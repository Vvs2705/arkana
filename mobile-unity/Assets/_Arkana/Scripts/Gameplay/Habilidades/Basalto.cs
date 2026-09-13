using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO BASALTO (18) — curta distancia / tanque. Ficha em design/personagens/18-basalto.md; tempos no DIRECAO.md §4 e §6.
    ///   Passiva  Pele de Montanha — tiro que bate pelas COSTAS fere 20% menos (as placas)
    ///   Tatica   Punho Sismico    — onda de pedra em cone CURTO que viaja do punho: dano + empurrao; em TERRA ergue 3 pedras
    ///   Suprema  Monolito         — 6 s de torre: -60% de dano, marca quem esta' perto e devolve 15% do que leva em estilhacos
    ///                               num cone a frente
    /// OS LIMITADORES SAO PARTE DO KIT: raio o ATORDOA 0,4 s a mais (pedra runica conduz); o punho e' curtissimo (kitar de longe
    /// e' a contra-jogada); no Monolito NAO anda nem conjura (ancora, nao imortalidade) e ao acabar as pernas endurecem 2 s
    /// (sem correr). Na telegrafia ele AFUNDA meio metro: parado em cima do aviso.
    /// A reducao e' DEVOLUCAO no mesmo tique (o molde do Coracao de Fornalha) — golpe letal mata antes da devolucao.
    /// ponytail: a imunidade a lentidao de terreno (lama) da ficha nao entrou — o fator de terreno e' do Pawn, fora do kit.
    /// </summary>
    public sealed class Basalto : IHabilidade
    {
        public const string MONOLITO = "monolito";
        public const string PERNAS_DURAS = "pernas_duras";

        private Onda _onda;
        private Vector3 _tiroDir;
        private Elemento _tiroEl;
        private float _tiroJanela, _pulsoAcc, _marcaAcc, _reflexo;

        public Onda OndaAtiva => _onda;
        /// <summary>Dano guardado para os proximos estilhacos (15% do recebido no Monolito).</summary>
        public float Reflexo => _reflexo;

        public void Tick(KitRunner k, float dt)
        {
            Dictionary<string, float> p = k.Dados.Passiva, s = k.Dados.Suprema;
            // quem VAI bater: o projetil inimigo mais perto chegando (direcao e elemento que o DanoRecebido le'). Por indice.
            Projetil tiro = ApoioGrupoD.TiroChegando(k, p["tiro_perto"]);
            if (tiro != null) { _tiroDir = tiro.Dir; _tiroEl = tiro.ElementoDoTiro; _tiroJanela = p["tiro_janela"]; }
            else _tiroJanela = Mathf.Max(_tiroJanela - dt, 0f);
            // afundando no aviso e no Monolito: ancora. Reaplica todo tique — lentidao alheia nao o solta.
            if (k.Telegrafia > 0f || k.EstadoAtivo(MONOLITO)) k.BuffVelocidade(s["imovel"], 0.2f);
            if (_onda != null && !_onda.Tick(k, dt)) _onda = null;
            if (!k.EstadoAtivo(MONOLITO)) return;
            _pulsoAcc += dt;
            _marcaAcc += dt;
            if (_pulsoAcc >= s["pulso"]) { _pulsoAcc = 0f; Estilhacos(k); }
            if (_marcaAcc >= s["marca_periodo"]) { _marcaAcc = 0f; Marcar(k); }
        }

        /// <summary>Punho Sismico: a onda sai do punho na mira.</summary>
        public void Tatica(KitRunner k) => _onda = new Onda(k, k.Pos, k.Mira());

        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            float dur = s["duracao"];
            k.LigarEstado(MONOLITO, dur);
            k.Silenciar(dur);   // nao conjura outra coisa (o ataque incluso)
            k.BuffVelocidade(s["imovel"], 0.2f);
            k.Visual("basalto_monolito", k.Pos, k.Pos, 1f, dur, k.Dono);
            _reflexo = 0f;
            _pulsoAcc = 0f;
            _marcaAcc = s["marca_periodo"];   // marca ja' no 1o tique
        }

        /// <summary>O preco do Monolito: as placas soltam numa avalanche e as pernas endurecem (2 s sem correr).</summary>
        public void EstadoAcabou(KitRunner k, string nome)
        {
            if (nome != MONOLITO) return;
            Dictionary<string, float> s = k.Dados.Suprema;
            Estilhacos(k);   // o que sobrou volta antes das placas cairem
            k.BuffVelocidade(s["endurece_vel"], s["endurece_dur"]);
            k.LigarEstado(PERNAS_DURAS, s["endurece_dur"]);
            k.Visual("basalto_avalanche", k.Pos, k.Pos, 1.2f, 1.5f);
        }

        /// <summary>Pele de Montanha + Monolito sobre o golpe que ACABOU de entrar (o motor mediu a queda de vida+escudo).</summary>
        public void DanoRecebido(KitRunner k, float quanto)
        {
            Dictionary<string, float> p = k.Dados.Passiva, s = k.Dados.Suprema;
            bool deTiro = _tiroJanela > 0f;
            _tiroJanela = 0f;   // um golpe por tiro: o proximo dano (DoT, zona) nao herda esta direcao
            float fica = 1f;    // fracao do dano que FICA nele
            // o tiro anda NA DIRECAO em que ele olha = bateu nas COSTAS: as placas seguram
            if (deTiro && Vector3.Dot(ApoioGrupoD.Plano(_tiroDir).normalized, k.Mira()) > p["costas_cos"])
            {
                fica *= 1f - p["costas_reducao"];
                k.Visual("basalto_estilhaco", k.Pos, k.Pos - k.Mira(), 0.35f, 0.4f);   // lasca nas costas: a placa segurou
            }
            if (k.EstadoAtivo(MONOLITO))
            {
                fica *= 1f - s["reducao"];
                _reflexo += quanto * s["reflexo"];
            }
            if (fica < 1f) ApoioGrupoD.Devolver(k, k.Dono, quanto * (1f - fica));
            // o PRECO da pedra runica: raio o atordoa 0,4 s A MAIS (soma ao que ja' houver; o kernel grampeia no teto)
            if (deTiro && _tiroEl == Elemento.Raio) Efeitos.Atordoar(k.Dono, Efeitos.De(k.Dono).StunLeft + p["raio_atordoa"]);
        }

        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        /// <summary>Estilhacos: o dano guardado sai inteiro em CADA inimigo do cone a frente (area de proposito: e' reflexo).</summary>
        private void Estilhacos(KitRunner k)
        {
            if (!(_reflexo > 0f)) return;
            Dictionary<string, float> s = k.Dados.Suprema;
            Vector3 d = k.Mira();
            foreach (IEntidade e in k.AlvosPerto(k.Pos, s["raio"], k.Dono))
                if (ApoioGrupoD.Inimigo(k.Dono, e) && ApoioGrupoD.NoCone(k.Pos, d, e.Pos, s["cone_graus"]))
                    Combat.AplicarDano(e, _reflexo, Elemento.Terra, k.Dono);
            k.Visual("basalto_estilhaco", k.Pos, k.Pos + d * s["raio"], 1f, 0.5f);
            _reflexo = 0f;
        }

        /// <summary>"Provoca e marca": bot nao le' provocacao (ponytail: a mira dos bots e' deles), entao a marca e' a LUZ do
        /// revelado sobre cada inimigo perto — informacao para todos.</summary>
        private void Marcar(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            foreach (IEntidade e in k.AlvosPerto(k.Pos, s["raio"], k.Dono))
                if (ApoioGrupoD.Inimigo(k.Dono, e)) k.Visual("revelado", e.Pos, e.Pos, 0.5f, s["marca_periodo"] + 0.15f, e);
        }

        // ------------------------------------------------------------------ ONDA
        /// <summary>
        /// A onda do Punho: frente que VIAJA do punho a `vel` m/s ate' o alcance, num cone. Quem a frente alcanca leva dano e
        /// empurrao, uma vez. Chegou no fim: publica TERRA em 3 pontos do arco — o §14 ergue muro so' em chao livre (em agua,
        /// em cima de alguem ou onde ja' ha' coisa, nao nasce nada). O visual e' escrito a cada tique (frente e largura).
        /// </summary>
        public sealed class Onda
        {
            public const float TICK = 0.05f;
            public const float Sobra = 0.25f;
            public readonly Vector3 Origem, Dir;
            public readonly EfeitoVisual Visual;
            public float Idade { get; private set; }
            private readonly Dictionary<string, float> _t;
            private readonly List<IEntidade> _atingidos = new List<IEntidade>();
            private float _acc;
            private bool _pedras;

            public float Frente => Mathf.Min(Idade * _t["vel"], _t["alcance"]);
            public IReadOnlyList<IEntidade> Atingidos => _atingidos;

            public Onda(KitRunner k, Vector3 origem, Vector3 dir)
            {
                _t = k.Dados.Tatica;
                Origem = origem;
                Dir = dir;
                Visual = k.Visual("basalto_onda", origem, origem, 0.3f, _t["alcance"] / Mathf.Max(_t["vel"], 0.1f) + Sobra);
            }

            public bool Tick(KitRunner k, float dt)
            {
                Idade += dt;
                float f = Frente, alcance = _t["alcance"];
                // Pos2 = a frente; Raio = a meia-largura dela (o cone abre com a distancia)
                Visual.Pos2 = Origem + Dir * f;
                Visual.Raio = Mathf.Max(f * Mathf.Tan(_t["cone_graus"] * Mathf.Deg2Rad), 0.3f);
                _acc += dt;
                if (_acc >= TICK || f >= alcance)
                {
                    _acc = 0f;
                    foreach (IEntidade e in k.AlvosPerto(Origem, f + 0.5f, k.Dono))
                    {
                        if (_atingidos.Contains(e) || !ApoioGrupoD.Inimigo(k.Dono, e)) continue;
                        if (!ApoioGrupoD.NoCone(Origem, Dir, e.Pos, _t["cone_graus"])) continue;
                        _atingidos.Add(e);
                        Combat.AplicarDano(e, _t["dano"], Elemento.Terra, k.Dono);
                        ApoioGrupoD.Empurrar(e, e.Pos - Origem, _t["empurrao"]);
                    }
                }
                if (f < alcance) return true;
                if (!_pedras) { _pedras = true; Pedras(); }
                return Idade < alcance / Mathf.Max(_t["vel"], 0.1f) + Sobra;
            }

            private void Pedras()
            {
                int n = Mathf.Max((int)_t["pedras"], 0);
                for (int i = 0; i < n; i++)
                {
                    float a = n <= 1 ? 0f : Mathf.Lerp(-_t["cone_graus"], _t["cone_graus"], i / (float)(n - 1));
                    Bus.EmitTerrainHit(Elemento.Terra, Origem + ApoioGrupoD.Girar(Dir, a) * _t["alcance"], false);
                }
            }
        }
    }
}
