using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Terrain;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO AELION (11) — o sniper. Ficha (proposta) em design/personagens/11-aelion.md; tempos da DIRECAO.md §4.
    ///   Passiva  Olhar do Crepusculo — acerto a mais de 40m MARCA o alvo 3s (so' para ele — nao vira wallhack de esquadrao)
    ///   Tatica   Flecha de Eter      — puxa a corda 1,5s e a flecha sai sozinha: MUITO rapida, atravessa 1 muro fino
    ///   Suprema  Chuva do Crepusculo — flecha ao ceu (o risco que todos veem, 3s de aviso); 3 ondas de flechas numa faixa
    ///                                  estreita de 30m a' frente, marcada no chao
    /// OS LIMITADORES: puxando a corda fica 40% mais lento e BRILHANDO; de perto a flecha rende a metade (cheia so' a 20m) e a
    /// faixa da chuva comeca a 4m dele — encostar e' a contra-jogada; a faixa e' avisada no chao; ao acabar a chuva o arco
    /// esfria — 2s sem a tatica.
    /// A flecha e' TIRO DE KIT (dano e velocidade proprios): voa aqui, com tempo de viagem (§4.1), e o acerto sai pelo Combat.
    /// O eter viaja como RAIO no §14 (o elemento mais rapido: abre escudo, conduz na agua). ponytail: se "eter" virar
    /// elemento, troca so' o EL.
    /// </summary>
    public sealed class Aelion : IHabilidade
    {
        public const string CARREGANDO = "carregando";
        public const string ARCO_FRIO = "arco_frio";
        public const Elemento EL = Elemento.Raio;

        readonly List<Flecha> _flechas = new List<Flecha>();
        readonly Dictionary<IEntidade, EfeitoVisual> _marcas = new Dictionary<IEntidade, EfeitoVisual>();
        Chuva _chuva;
        float _carga;
        bool _mirando, _ouvindo;
        Vector3 _inicio, _dir;
        KitRunner _k;

        public bool Carregando => _carga > 0f;
        /// <summary>0..1 da corda puxada (a casca acende o brilho por aqui).</summary>
        public float FracCarga(KitRunner k) => _carga > 0f ? 1f - _carga / Mathf.Max(k.Dados.Tatica["carga"], 0.001f) : 0f;
        public IReadOnlyList<Flecha> Flechas => _flechas;
        public Chuva ChuvaAtiva => _chuva;

        static float Plano(Vector3 v) => new Vector2(v.x, v.z).magnitude;
        static Vector3 Chao(Vector3 v) => new Vector3(v.x, 0f, v.z);

        public void Tick(KitRunner k, float dt)
        {
            Ouvir(k);
            // a faixa e o risco ao ceu saem no APERTO (a mira de quando ele avisou), nao quando a chuva cai
            if (k.Telegrafia > 0f && !_mirando) Mirar(k);
            if (_carga > 0f)
            {
                _carga -= dt;
                if (_carga <= 0f) Soltar(k);
            }
            for (int i = _flechas.Count - 1; i >= 0; i--)
                if (!_flechas[i].Tick(k, dt)) _flechas.RemoveAt(i);
            if (_chuva != null && !_chuva.Tick(k, dt)) FimDaChuva(k);
        }

        /// <summary>Flecha de Eter: PUXA a corda (o preco: 40% mais lento e o brilho). Sai sozinha cheia em `carga` s.
        /// ponytail: "segurar e soltar antes" pede um SOLTAR no botao da HUD (IHabilidade.TaticaSolta) — hoje o toque carrega
        /// e a flecha sai cheia.</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            _carga = t["carga"];
            k.BuffVelocidade(t["carga_vel"], t["carga"]);
            k.AvisarEstado(CARREGANDO, true);
        }

        void Soltar(KitRunner k)
        {
            _carga = 0f;
            k.AvisarEstado(CARREGANDO, false);
            Vector3 d = k.Mira();
            _flechas.Add(new Flecha(k, k.Pos + Vector3.up * Pawn.ALTURA_MAO + d * Pawn.SAIDA_TIRO, d));
        }

        /// <summary>Chuva do Crepusculo: cai na faixa AVISADA (a mira do aperto).</summary>
        public void Suprema(KitRunner k)
        {
            if (!_mirando) Mirar(k);
            _mirando = false;
            _chuva = new Chuva(k, _inicio, _dir);
        }

        /// <summary>O aviso (§5 da DIRECAO: toda area avisa no chao, para TODOS): a faixa estreita e a flecha ao ceu.</summary>
        void Mirar(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            _mirando = true;
            _dir = k.Mira();
            _inicio = k.Pos + _dir * s["inicio"];
            float t = Mathf.Max(k.Telegrafia, 0.1f);
            k.Visual("aelion_aviso", _inicio, _inicio + _dir * s["comprimento"], s["largura"] * 0.5f, t);
            k.Visual("aelion_risco", k.Pos, k.Pos, 0.2f, t);
        }

        /// <summary>O PRECO: o arco esfria vertendo po' de estrela — 2s sem a tatica.</summary>
        void FimDaChuva(KitRunner k)
        {
            float s = k.Dados.Suprema["esfria_dur"];
            _chuva = null;
            k.ForcarCdTatica(s);
            k.LigarEstado(ARCO_FRIO, s);
        }

        // ------------------------------------------------------------- a passiva
        /// <summary>O acerto que conta e' QUALQUER dano dele (ataque basico, flecha, chuva): o Bus e' o unico lugar que sabe
        /// quem atingiu quem. Assina no 1o tique (o runner ainda nao existia no construtor) e SOLTA sozinho quando o corpo
        /// some com a arena (o Bus nao e' resetado entre partidas).</summary>
        void Ouvir(KitRunner k)
        {
            if (_ouvindo) return;
            _ouvindo = true;
            _k = k;
            Bus.DamageApplied += AoDanar;
        }

        void AoDanar(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool escudo)
        {
            UnityEngine.Object corpo = _k.Dono as UnityEngine.Object;
            if (!ReferenceEquals(corpo, null) && corpo == null) { Bus.DamageApplied -= AoDanar; _ouvindo = false; return; }
            if (!ReferenceEquals(fonte, _k.Dono) || alvo == null || alvo == fonte) return;
            Dictionary<string, float> p = _k.Dados.Passiva;
            if (Plano(alvo.Pos - _k.Pos) <= p["dist_marca"]) return;
            EfeitoVisual v;
            if (_marcas.TryGetValue(alvo, out v) && v.Restante > 0f) { v.Restante = v.Duracao; return; }
            _marcas[alvo] = _k.Visual("aelion_marca", alvo.Pos, alvo.Pos, 0.45f, p["marca_dur"], alvo);
        }

        public void DanoRecebido(KitRunner k, float quanto) { }
        public void EstadoAcabou(KitRunner k, string nome) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        /// <summary>
        /// O primeiro corpo VIVO no trajeto de..ate' (a capsula do Partida.Acerto: raio do corpo + do tiro, altura do corpo),
        /// sem i-frames e sem `dono`. Mede o SEGMENTO do passo: tiro de kit rapido nao atravessa ninguem entre dois quadros.
        /// E' o acerto de todo tiro de kit do Grupo B (a flecha, o tiro devolvido pelo Espelho do Ilusionista).
        /// </summary>
        public static IEntidade NoTrajeto(KitRunner k, Vector3 de, Vector3 ate, IEntidade dono)
        {
            float r = Partida.RAIO_CORPO + Projetil.RAIO_HITBOX;
            Vector3 a = Chao(de), b = Chao(ate);
            IEntidade melhor = null;
            float melhorD = float.MaxValue;
            foreach (IEntidade e in k.AlvosPerto((de + ate) * 0.5f, Vector3.Distance(de, ate) * 0.5f + r + Partida.ALTURA_CORPO, dono))
            {
                if (Efeitos.De(e).IframesLeft > 0f) continue;   // a esquiva e o plano espectral sao imunidade TOTAL
                float dy = ate.y - e.Pos.y;
                if (dy < -Projetil.RAIO_HITBOX || dy > Partida.ALTURA_CORPO + Projetil.RAIO_HITBOX) continue;
                Vector3 p = Chao(e.Pos);
                if (KitRunner.DistSegmento(p, a, b) > r) continue;
                float d = (p - a).sqrMagnitude;
                if (d < melhorD) { melhorD = d; melhor = e; }
            }
            return melhor;
        }

        /// <summary>Muro de pe' no ponto (a regra do Partida.NoMuro: celula de muro E abaixo do topo). Sem terreno, nada.</summary>
        static bool NoMuro(Vector3 pos)
        {
            TerrenoReativo t = Partida.Atual != null && Partida.Atual.Terreno != null ? Partida.Atual.Terreno
                : TerrenoReativoBehaviour.Atual != null ? TerrenoReativoBehaviour.Atual.Terreno : null;
            return t != null && t.BloqueiaTiro(pos) && pos.y <= t.Centro(t.CelulaEm(pos)).y + Balance.Terrain.WallHeight;
        }

        // ------------------------------------------------------------------ FLECHA
        /// <summary>Flecha de Eter em voo. O visual e' o RISCO inteiro (da corda a' ponta) e fica `RASTRO` s no ar depois.</summary>
        public sealed class Flecha
        {
            public const float RASTRO = 0.5f;
            public readonly Vector3 Origem, Dir;
            public Vector3 Pos { get; private set; }
            public bool Voando { get; private set; } = true;
            /// <summary>Quantos muros ja' ATRAVESSOU.</summary>
            public int Muros { get; private set; }
            public readonly EfeitoVisual Visual;
            readonly Dictionary<string, float> _t;
            readonly IEntidade _dono;
            float _alcance, _noMuro;
            bool _dentro;

            public Flecha(KitRunner k, Vector3 origem, Vector3 dir)
            {
                _t = k.Dados.Tatica;
                _dono = k.Dono;
                Origem = Pos = origem;
                Dir = dir;
                _alcance = _t["alcance"];
                Visual = k.Visual("aelion_flecha", origem, origem, 0.06f, RASTRO);
            }

            /// <summary>false = acabou (parou e o rastro apagou).</summary>
            public bool Tick(KitRunner k, float dt)
            {
                if (!Voando) return Visual.Restante > 0f;
                Visual.Restante = RASTRO;   // voando, o risco fica aceso inteiro
                float passo = Mathf.Min(_t["velocidade"] * dt, _alcance);
                Vector3 de = Pos;
                Pos += Dir * passo;
                _alcance -= passo;
                Visual.Pos2 = Pos;
                IEntidade alvo = NoTrajeto(k, de, Pos, _dono);
                if (alvo != null) { Acertar(k, alvo); return true; }
                if (Muro(passo)) { Parar(k); return true; }
                if (Arkana.World.Ilha.Atual != null && Pos.y < Arkana.World.Ilha.AlturaDoChao(Pos.x, Pos.z)) { Parar(k); return true; }
                if (_alcance <= 0f) Voando = false;
                return true;
            }

            /// <summary>Atravessa `atravessa` muro(s) FINO(s): o 2o para, e muro mais grosso que `parede_max` tambem.</summary>
            bool Muro(float passo)
            {
                bool dentro = NoMuro(Pos);
                if (dentro && !_dentro) { Muros++; _noMuro = 0f; }
                if (dentro) _noMuro += passo;
                _dentro = dentro;
                if (!dentro) return false;
                if (Muros > (int)_t["atravessa"] || _noMuro > _t["parede_max"]) { Muros--; return true; }
                return false;
            }

            /// <summary>O acerto: de PERTO rende `dano_perto` do dano, cheio a partir de `dist_cheia` m (encostar e' o counter).</summary>
            void Acertar(KitRunner k, IEntidade alvo)
            {
                float f = Mathf.Lerp(_t["dano_perto"], 1f, Mathf.Clamp01(Plano(alvo.Pos - Origem) / Mathf.Max(_t["dist_cheia"], 0.01f)));
                float dano = _t["dano"] * f;
                float mult = Efeitos.Aplicar(alvo, EL, dano, _dono);
                Combat.AplicarDano(alvo, dano * mult, EL, _dono);   // `dono` como FONTE: credita a evolucao e acende a passiva
                Parar(k);
            }

            /// <summary>Para onde bateu: estilhaco de vidro estelar + TerrainHit (o terreno decide; o muro apanha).</summary>
            void Parar(KitRunner k)
            {
                Voando = false;
                Visual.Pos2 = Pos;
                k.Visual("aelion_estilhaco", Pos, Pos, 0.5f, 0.5f);
                Bus.EmitTerrainHit(EL, Pos, false);
            }
        }

        // ------------------------------------------------------------------- CHUVA
        /// <summary>A faixa A-B de meia largura `largura`/2: `ondas` ondas a cada `intervalo` s, a 1a no fim do aviso.</summary>
        public sealed class Chuva
        {
            public readonly Vector3 A, B;
            public readonly float MeiaLargura;
            public int Ondas { get; private set; }
            public readonly EfeitoVisual Visual;
            readonly Dictionary<string, float> _s;
            float _acc;

            public Chuva(KitRunner k, Vector3 inicio, Vector3 dir)
            {
                _s = k.Dados.Suprema;
                A = inicio;
                B = inicio + dir * _s["comprimento"];
                MeiaLargura = _s["largura"] * 0.5f;
                _acc = _s["intervalo"];
                Visual = k.Visual("aelion_chuva", A, B, MeiaLargura, _s["ondas"] * _s["intervalo"] + 0.3f);
            }

            public bool Dentro(Vector3 p) => KitRunner.DistSegmento(Chao(p), Chao(A), Chao(B)) <= MeiaLargura;

            public bool Tick(KitRunner k, float dt)
            {
                _acc += dt;
                if (_acc < _s["intervalo"]) return true;
                _acc = 0f;
                Ondas++;
                foreach (IEntidade e in k.AlvosPerto((A + B) * 0.5f, Vector3.Distance(A, B) * 0.5f + MeiaLargura + 1f, k.Dono))
                    if (!Vex.MesmoTime(k.Dono, e) && Dentro(e.Pos)) Combat.AplicarDano(e, _s["dano"], EL, k.Dono);
                return Ondas < (int)_s["ondas"];
            }
        }
    }
}
