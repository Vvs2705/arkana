using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA TESSA (10) — Dominadora. GDD §3, ficha em design/personagens/10-tessa.md; espelho de habilidades/tessa.gd.
    ///   Passiva  Compasso Runico — regenera escudo de 1 em 1 ponto; escudo QUEBRADO da' +15% vel por 2s
    ///   Tatica   Fio do Tear     — fio de raio entre 2 pontos (ate' 6): tocar = dano + lentidao + revela
    ///   Suprema  Tear-Mae        — absorve projetil inimigo e TECE escudo (ela + aliados perto)
    /// OS LIMITADORES: o fio cai com 1 golpe em qualquer ancora (qualquer TerrainHit a 1,5m); zumbe a 5m; agua no
    /// chao CONDUZ o raio do fio para TODOS (o toque publica TerrainHit(Raio) e o §14 nao pergunta o time); o
    /// Tear nao absorve curtissimo alcance (zona morta 2m) e e' 1 por vez.
    /// O escudo e' o do KERNEL (Vitalidade): aqui so' se REGENERA/TECE pela porta KitRunner.RegenerarEscudo.
    /// </summary>
    public sealed class Tessa : IHabilidade
    {
        public const string ESCUDO_QUEBRADO = "escudo_quebrado";
        public const string FIO_ZUMBIDO = "fio_zumbido";

        private readonly List<Fio> _fios = new List<Fio>();
        private Tear _tear;
        private bool _tinhaEscudo = true;
        private float _regenAcc;

        public IReadOnlyList<Fio> Fios => _fios;
        public Tear TearAtivo => _tear != null && _tear.Vivo ? _tear : null;

        public void Tick(KitRunner k, float dt)
        {
            Dictionary<string, float> p = k.Dados.Passiva;
            // Quebra ANTES da regeneracao: regenerar primeiro devolveria uma casca e a quebra nunca seria vista.
            bool vivo = k.Dono.Vital.Escudo > 0f;
            if (_tinhaEscudo && !vivo)
            {
                k.BuffVelocidade(p["quebra_buff_vel"], p["quebra_buff_dur"]);
                k.LigarEstado(ESCUDO_QUEBRADO, p["quebra_buff_dur"]);
            }
            _tinhaEscudo = vivo;
            // De 1 em 1 ponto, nao por frame: no maximo 4 ShieldChanged por segundo, mesmo total.
            _regenAcc += p["escudo_regen"] * dt;
            if (_regenAcc + 0.0001f >= 1f)
            {
                float inteiro = Mathf.Floor(_regenAcc + 0.0001f);
                KitRunner.RegenerarEscudo(k.Dono, inteiro);
                _regenAcc -= inteiro;
            }
            for (int i = _fios.Count - 1; i >= 0; i--)
                if (!_fios[i].Tick(k, dt)) _fios.RemoveAt(i);
            if (_tear != null && !_tear.Tick(k, dt)) _tear = null;
        }

        /// <summary>Fio do Tear: as DUAS ancoras nascem juntas — debaixo dos pes e a `comprimento` m na mira (um toque).</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            while (_fios.Count >= (int)t["max_fios"])   // o 7o fio apaga o mais velho
            {
                _fios[0].Derrubar();
                _fios.RemoveAt(0);
            }
            Vector3 a = k.Pos;
            _fios.Add(new Fio(k, a, a + k.Mira() * t["comprimento"]));
        }

        /// <summary>Tear-Mae — max. 1 por vez.</summary>
        public void Suprema(KitRunner k)
        {
            if (_tear != null && _tear.Vivo) return;
            _tear = new Tear(k);
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void DanoRecebido(KitRunner k, float quanto) { }

        /// <summary>UM golpe em qualquer ancora derruba o fio: TODO impacto de projetil emite TerrainHit.</summary>
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte)
        {
            float r = k.Dados.Tatica["ancora_raio"];
            for (int i = _fios.Count - 1; i >= 0; i--)
            {
                Fio f = _fios[i];
                if (Vector3.Distance(pos, f.A) <= r || Vector3.Distance(pos, f.B) <= r)
                {
                    f.Derrubar();
                    _fios.RemoveAt(i);
                }
            }
        }

        // --------------------------------------------------------------------- FIO
        /// <summary>Segmento de raio entre 2 ancoras. Tique proprio de 0,1s.</summary>
        public sealed class Fio
        {
            public const float TICK = 0.1f;
            public readonly Vector3 A, B;
            public float Duracao { get; private set; }
            public readonly EfeitoVisual Visual;
            private readonly IEntidade _dona;
            private readonly Dictionary<string, float> _t;
            private readonly Dictionary<IEntidade, float> _rearme = new Dictionary<IEntidade, float>();
            private readonly List<IEntidade> _vencidos = new List<IEntidade>();
            private float _acc;
            private bool _zumbindo;

            public Vector3 Centro => (A + B) * 0.5f;
            public bool Vivo => Duracao > 0f;

            public Fio(KitRunner k, Vector3 a, Vector3 b)
            {
                A = a; B = b; _dona = k.Dono; _t = k.Dados.Tatica;
                Duracao = _t["duracao"];
                Visual = k.Visual("fio", a, b, _t["raio_toque"], Duracao);
            }

            public void Derrubar() { Duracao = 0f; Visual.Restante = 0f; }

            public bool Tick(KitRunner k, float dt)
            {
                Duracao -= dt;
                if (Duracao <= 0f) { Visual.Restante = 0f; return false; }
                _vencidos.Clear();
                foreach (KeyValuePair<IEntidade, float> kv in new List<KeyValuePair<IEntidade, float>>(_rearme))
                {
                    float r = kv.Value - dt;
                    if (r <= 0f) _vencidos.Add(kv.Key); else _rearme[kv.Key] = r;
                }
                foreach (IEntidade e in _vencidos) _rearme.Remove(e);
                _acc += dt;
                if (_acc < TICK) return true;
                _acc = 0f;
                float zumbido = _t["zumbido_dist"];
                float alcance = Vector3.Distance(A, B) * 0.5f + zumbido;
                bool perto = false;
                foreach (IEntidade alvo in k.AlvosPerto(Centro, alcance, _dona))
                {
                    float d = KitRunner.DistSegmento(alvo.Pos, A, B);
                    if (d <= zumbido) perto = true;   // o fio se DENUNCIA a 5m (brilho + zumbido)
                    if (d > _t["raio_toque"] || _rearme.ContainsKey(alvo)) continue;
                    Tocar(k, alvo);
                }
                if (perto != _zumbindo)
                {
                    _zumbindo = perto;
                    Bus.EmitKitState(FIO_ZUMBIDO, perto);
                }
                return true;
            }

            private void Tocar(KitRunner k, IEntidade alvo)
            {
                // `dona` como FONTE: dano de habilidade credita a evolucao do escudo dela (GDD §5).
                if (Combat.AplicarDano(alvo, _t["dano"], Elemento.Raio, _dona) <= 0f) return;
                _rearme[alvo] = _t["rearme"];
                Efeitos.Lentificar(alvo, _t["lentidao"], _t["lentidao_dur"]);
                k.Visual("revelado", alvo.Pos, alvo.Pos, 0.5f, _t["revela_dur"], alvo);
                // AGUA CONDUZ (§14): o kit NAO decide o mundo — publica o raio no ponto do toque.
                Bus.EmitTerrainHit(Elemento.Raio, alvo.Pos, false);
            }
        }

        // ---------------------------------------------------------------- TEAR-MAE
        /// <summary>Tear runico: engole projetil inimigo no raio (fora da zona morta) e tece escudo para os aliados perto.</summary>
        public sealed class Tear
        {
            public const float TICK = 0.1f;
            public readonly Vector3 Pos;
            public float Duracao { get; private set; }
            public readonly EfeitoVisual Visual;
            public int Absorvidos { get; private set; }
            private readonly IEntidade _dona;
            private readonly Dictionary<string, float> _s;
            private float _acc;

            public bool Vivo => Duracao > 0f;

            public Tear(KitRunner k)
            {
                Pos = k.Pos; _dona = k.Dono; _s = k.Dados.Suprema;
                Duracao = _s["duracao"];
                Visual = k.Visual("tear", Pos, Pos, _s["raio"], Duracao);
            }

            public bool Tick(KitRunner k, float dt)
            {
                Duracao -= dt;
                if (Duracao <= 0f) { Visual.Restante = 0f; return false; }
                _acc += dt;
                if (_acc < TICK) return true;
                _acc = 0f;
                IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
                if (vivos == null) return true;
                Vector3 centro = _dona.Pos;   // o tear acompanha a dona (o escudo e' dela e dos aliados perto)
                for (int i = vivos.Count - 1; i >= 0; i--)
                {
                    Projetil p = vivos[i];
                    if (p == null || !p.Vivo || Combat.MesmoTime(_dona, p.Atirador)) continue;   // so' tiro INIMIGO
                    float d = Vector3.Distance(p.Pos, centro);
                    if (d > _s["raio"] || d < _s["zona_morta"]) continue;   // ZONA MORTA: nada de corpo a corpo
                    vivos.RemoveAt(i);   // absorvido: sai da arena sem impacto (nem terreno, nem dano)
                    Absorvidos++;
                    k.AoAbsorver?.Invoke(p);
                    Tecer(k, _s["escudo_por_projetil"], centro);
                }
                return true;
            }

            private void Tecer(KitRunner k, float quanto, Vector3 centro)
            {
                KitRunner.RegenerarEscudo(_dona, quanto);
                foreach (IEntidade a in k.AlvosPerto(centro, _s["raio_aliado"], _dona))
                    if (Combat.MesmoTime(_dona, a)) KitRunner.RegenerarEscudo(a, quanto);
            }
        }
    }
}
