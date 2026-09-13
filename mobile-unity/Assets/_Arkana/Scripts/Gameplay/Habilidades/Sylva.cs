using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA SYLVA (17) — suporte / vida. Ficha em design/personagens/17-sylva.md; tempos no DIRECAO.md §4 e §6.
    ///   Passiva  Seiva Compartilhada — vinculo com o ALIADO mais perto: 20% do dano que ele leva vem para ela (transfere)
    ///   Tatica   Broto Guardiao      — floresce em 1 s e o polen cura 5 hp/s por 6 s (ela inclusa); em GRAMA vira moita
    ///   Suprema  Coracao da Mata     — 8 s de raizes douradas: aliado dentro regenera VIDA e ESCUDO; quem CRUZA a borda
    ///                                  e' agarrado 0,8 s (o teto de atordoamento do kernel)
    /// OS LIMITADORES SAO PARTE DO KIT: o vinculo transfere dano REAL (pode mata-la); o broto QUEIMA na hora com fogo (golpe de
    /// fogo perto ou chao em chamas debaixo); as raizes agarram tambem o ALIADO que vem de fora (dois gumes espacial); ao
    /// acabar a seiva foi gasta — 3 s sem vinculo (a pele perde o verde).
    /// ponytail: SOLO hoje — sem esquadrao o vinculo nao tem com quem se ligar e a passiva nao age; a leitura dela na tela e' o
    /// cabelo-estacao (flor -> outono pela vida), desenhado pela casca.
    /// </summary>
    public sealed class Sylva : IHabilidade
    {
        public const string SEIVA_GASTA = "seiva_gasta";

        /// <summary>Casca: o ponto e' GRAMA (a Campina da ilha)? Sem ilha, nunca. O teste troca (e o SetUp devolve).</summary>
        public static Func<Vector3, bool> EhGrama = GramaDaIlha;

        public static bool GramaDaIlha(Vector3 p) =>
            Ilha.Atual != null && Ilha.Atual.Relevo != null && Ilha.Atual.Relevo.BiomaEm(p.x, p.z) == Bioma.Campina;

        private readonly List<Broto> _brotos = new List<Broto>();
        private Coracao _coracao;
        private Vector3 _centroAviso;
        private bool _avisando;
        private IEntidade _vinculo;
        private float _ehpVinculo, _buscaAcc = 1f;

        public IReadOnlyList<Broto> Brotos => _brotos;
        public Coracao CoracaoAtivo => _coracao;
        /// <summary>O aliado vinculado agora (null = ninguem; SOLO, sempre). A casca desenha o fio de seiva ate' ele.</summary>
        public IEntidade Vinculado => _vinculo;

        public void Tick(KitRunner k, float dt)
        {
            // o Coracao nasce onde o AVISO nasceu (o anel do chao), nao onde ela estiver quando o aviso acaba
            if (k.Telegrafia > 0f && !_avisando) { _avisando = true; _centroAviso = k.Pos; }
            Vinculo(k, dt);
            for (int i = _brotos.Count - 1; i >= 0; i--)
                if (!_brotos[i].Tick(k, dt)) _brotos.RemoveAt(i);
            if (_coracao != null && !_coracao.Tick(k, dt))
            {
                _coracao = null;
                k.LigarEstado(SEIVA_GASTA, k.Dados.Suprema["seiva_dur"]);
            }
        }

        /// <summary>Seiva Compartilhada: a queda de vida+escudo do vinculado desde o tique anterior e' dividida — ela devolve a
        /// fracao a ele e a toma para si pelo ponto unico (fonte null: nao credita ninguem).</summary>
        private void Vinculo(KitRunner k, float dt)
        {
            Dictionary<string, float> p = k.Dados.Passiva;
            if (k.EstadoAtivo(SEIVA_GASTA)) { _vinculo = null; return; }
            _buscaAcc += dt;
            if (_buscaAcc >= 0.25f)
            {
                _buscaAcc = 0f;
                IEntidade novo = AliadoMaisProximo(k, p["vinculo_raio"]);
                if (novo != _vinculo) { _vinculo = novo; if (novo != null) _ehpVinculo = Ehp(novo); }
            }
            if (_vinculo == null) return;
            float ehp = Ehp(_vinculo);
            if (ehp < _ehpVinculo - 0.0001f)
            {
                float q = (_ehpVinculo - ehp) * p["vinculo_frac"];
                ApoioGrupoD.Devolver(k, _vinculo, q);
                // Fogo e' a REGUA neutra (1x no escudo, 1x na vida): a fatia chega nela do tamanho que saiu dele
                Combat.AplicarDano(k.Dono, q, Elemento.Fogo, null);
            }
            _ehpVinculo = Ehp(_vinculo);
        }

        private static IEntidade AliadoMaisProximo(KitRunner k, float raio)
        {
            IEntidade melhor = null;
            float d2 = float.MaxValue;
            foreach (IEntidade e in k.AlvosPerto(k.Pos, raio, k.Dono))
            {
                if (!ApoioGrupoD.Aliado(k.Dono, e)) continue;
                float d = (e.Pos - k.Pos).sqrMagnitude;
                if (d < d2) { d2 = d; melhor = e; }
            }
            return melhor;
        }

        private static float Ehp(IEntidade e) => e.Vital.Hp + e.Vital.Escudo;

        /// <summary>Broto Guardiao: plantado a `dist` m na mira. Em GRAMA tambem levanta uma moita de cobertura.</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            Vector3 p = k.Pos + k.Mira() * t["dist"];
            _brotos.Add(new Broto(k, p));
            // ponytail: a moita e' cobertura de VISTA (esconde o corpo); tiro atravessa — bloquear pede celula "moita" no §14
            if (EhGrama != null && EhGrama(p)) k.Visual("sylva_moita", p, p, t["moita_raio"], t["moita_dur"]);
        }

        /// <summary>Coracao da Mata: no centro do aviso.</summary>
        public void Suprema(KitRunner k)
        {
            Vector3 c = _avisando ? _centroAviso : k.Pos;
            _avisando = false;
            _coracao = new Coracao(k, c);
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void DanoRecebido(KitRunner k, float quanto) { }

        /// <summary>O broto QUEIMA na hora com fogo perto (o §14 contra ela).</summary>
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte)
        {
            if (el != Elemento.Fogo) return;
            float r = k.Dados.Tatica["queima_raio"];
            for (int i = 0; i < _brotos.Count; i++)
                if (Vector3.Distance(pos, _brotos[i].Pos) <= r) _brotos[i].Queimar(k);
        }

        // ------------------------------------------------------------------ BROTO
        /// <summary>Broto: fecha `florescer` s (nao cura), depois o polen cura a cada 0,25 s quem esta' no raio (ela e aliados).</summary>
        public sealed class Broto
        {
            public const float TICK = 0.25f;
            public readonly Vector3 Pos;
            public readonly EfeitoVisual Visual;
            public float Idade { get; private set; }
            public bool Queimado { get; private set; }
            private readonly Dictionary<string, float> _t;
            private float _acc;

            public bool Aberto => Idade >= _t["florescer"];
            public bool Vivo => !Queimado && Idade < _t["florescer"] + _t["duracao"];

            public Broto(KitRunner k, Vector3 pos)
            {
                _t = k.Dados.Tatica;
                Pos = pos;
                Visual = k.Visual("sylva_broto", pos, pos, _t["raio"], _t["florescer"] + _t["duracao"]);
            }

            public bool Tick(KitRunner k, float dt)
            {
                Idade += dt;
                // chao em CHAMAS debaixo dele (o fogo do mundo e' o do TerrenoReativo): queima
                if (KitRunner.DpsDoTerreno != null && Mathf.Approximately(KitRunner.DpsDoTerreno(Pos), Balance.Terrain.BurnDps)) Queimar(k);
                if (!Vivo) { Visual.Restante = 0f; return false; }
                if (!Aberto) return true;
                _acc += dt;
                if (_acc < TICK) return true;
                float q = _t["cura"] * _acc;
                _acc = 0f;
                foreach (IEntidade e in k.AlvosPerto(Pos, _t["raio"]))
                    if (e == k.Dono || ApoioGrupoD.Aliado(k.Dono, e)) ApoioGrupoD.Curar(k, e, q);
                return true;
            }

            public void Queimar(KitRunner k)
            {
                if (Queimado) return;
                Queimado = true;
                Visual.Restante = 0f;
                k.Visual("sylva_queima", Pos, Pos, 0.8f, 1f);
            }
        }

        // ------------------------------------------------------------------ CORACAO
        /// <summary>A area das raizes. Tique de 0,25 s (no maximo 4 ShieldChanged por segundo, o passo do Compasso da Tessa).
        /// Agarrar e' na BORDA (fora -> dentro), com rearme: ninguem fica preso em laco na linha.</summary>
        public sealed class Coracao
        {
            public const float TICK = 0.25f;
            public readonly Vector3 Centro;
            public readonly EfeitoVisual Visual;
            public float Restante { get; private set; }
            private readonly Dictionary<string, float> _s;
            private readonly HashSet<IEntidade> _dentro = new HashSet<IEntidade>();
            private readonly Dictionary<IEntidade, float> _agarradoEm = new Dictionary<IEntidade, float>();
            private float _acc, _relogio;

            public Coracao(KitRunner k, Vector3 centro)
            {
                _s = k.Dados.Suprema;
                Centro = centro;
                Restante = _s["duracao"];
                Visual = k.Visual("sylva_raizes", centro, centro, _s["raio"], Restante);
                // quem ja' esta' dentro: aliado (e ela) so' fica; INIMIGO e' agarrado — as raizes brotam debaixo dele
                foreach (IEntidade e in k.AlvosPerto(centro, _s["raio"]))
                {
                    _dentro.Add(e);
                    if (ApoioGrupoD.Inimigo(k.Dono, e)) Agarrar(k, e);
                }
            }

            public bool Tick(KitRunner k, float dt)
            {
                Restante -= dt;
                _relogio += dt;
                if (Restante <= 0f) { Visual.Restante = 0f; return false; }
                _acc += dt;
                if (_acc < TICK) return true;
                float passo = _acc;
                _acc = 0f;
                List<IEntidade> agora = k.AlvosPerto(Centro, _s["raio"]);
                foreach (IEntidade e in agora)
                {
                    if (e == k.Dono || ApoioGrupoD.Aliado(k.Dono, e))
                    {
                        ApoioGrupoD.Curar(k, e, _s["cura"] * passo);
                        KitRunner.RegenerarEscudo(e, _s["escudo"] * passo);
                    }
                    // quem CRUZA a borda e' agarrado — o aliado que vinha de fora tambem (dois gumes); ela nunca
                    if (e != k.Dono && !_dentro.Contains(e)) Agarrar(k, e);
                }
                _dentro.Clear();
                foreach (IEntidade e in agora) _dentro.Add(e);
                return true;
            }

            private void Agarrar(KitRunner k, IEntidade e)
            {
                float quando;
                if (_agarradoEm.TryGetValue(e, out quando) && _relogio - quando < _s["rearme"]) return;
                _agarradoEm[e] = _relogio;
                Efeitos.Atordoar(e, _s["agarra"]);   // o kernel grampeia no StunCap
                k.Visual("sylva_agarra", e.Pos, e.Pos, 0.7f, _s["agarra"], e);
            }
        }
    }
}
