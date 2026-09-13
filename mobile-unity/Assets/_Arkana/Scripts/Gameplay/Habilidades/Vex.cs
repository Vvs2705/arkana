using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO VEX (09) — Dominador. GDD §3, ficha em design/personagens/09-vex.md.
    ///   Passiva  Olhos do Miasma — inimigo DENTRO da nevoa dele ganha contorno verde (visual preso no alvo, so' para ele)
    ///   Tatica   Frascos         — 1 frasco em ARCO por uso (ate' 6 no chao): poca inerte que arma em 1s; inimigo que pisa
    ///                              detona uma nuvem de 3m por 4s — dano BAIXO + lentidao
    ///   Suprema  A Grande Obra   — nevoa de 12m por 12s: lenta + dano baixo no inimigo e SELA a cura de TODO MUNDO dentro
    /// OS LIMITADORES: a nevoa nega area, nao mata (DoT baixo, direto na vida pelo teto somado do Combat); um TIRO que para a
    /// `tiro_raio` da poca a detona longe de todos (todo impacto publica TerrainHit); VENTO dispersa e FOGO consome em 2s
    /// (§14, vale para nuvem e Grande Obra); o selo pega o time dele e ele mesmo; a Grande Obra acaba OFEGANTE (2s sem correr).
    /// </summary>
    public sealed class Vex : IHabilidade
    {
        public const string GRANDE_OBRA = "grande_obra";
        public const string OFEGANTE = "ofegante";
        /// <summary>s do tique de area (nuvens, gatilho das pocas) — o mesmo balde do DoT (Balance.Dot.Tick).</summary>
        public const float TICK = 0.25f;
        /// <summary>m de altura do arco do frasco (so' leitura: o pouso e' no ponto certo).</summary>
        public const float ARCO = 1.6f;

        readonly List<Frasco> _frascos = new List<Frasco>();
        readonly List<Nuvem> _nuvens = new List<Nuvem>();
        readonly Dictionary<IEntidade, EfeitoVisual> _contornos = new Dictionary<IEntidade, EfeitoVisual>();
        readonly Selo _selo = new Selo();
        Nuvem _obra;
        bool _avisando;
        Vector3 _centroAviso;

        public IReadOnlyList<Frasco> Frascos => _frascos;
        public IReadOnlyList<Nuvem> Nuvens => _nuvens;
        public Nuvem Obra => _obra;
        public Selo SeloDaObra => _selo;

        /// <summary>O time, pelo criterio do resto do jogo (Combat.Creditar, Derrubado): hoje o unico esquadrao e' o dos EhPlayer.</summary>
        public static bool MesmoTime(IEntidade a, IEntidade b) => a == b || (a != null && b != null && a.EhPlayer && b.EhPlayer);

        static float Plano(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        public void Tick(KitRunner k, float dt)
        {
            // o aviso no chao sai onde ele APERTOU (Bus.KitTelegraph leva Dono.Pos): a nevoa nasce la', nao 3s de caminhada depois
            if (k.Telegrafia > 0f && !_avisando) { _avisando = true; _centroAviso = k.Pos; }
            for (int i = _frascos.Count - 1; i >= 0; i--)
                if (!_frascos[i].Tick(k, this, dt)) _frascos.RemoveAt(i);
            for (int i = _nuvens.Count - 1; i >= 0; i--)
                if (!_nuvens[i].Tick(k, this, dt)) _nuvens.RemoveAt(i);
            if (_obra != null && !_obra.Tick(k, this, dt)) FimDaObra(k);
            _selo.Tick();
        }

        /// <summary>Frascos de Reagente: UM frasco por uso, em arco ate' `alcance` m na mira. O 7o leva o mais velho.</summary>
        public void Tatica(KitRunner k)
        {
            int max = (int)k.Dados.Tatica["max_frascos"];
            while (_frascos.Count >= max) { _frascos[0].Sumir(); _frascos.RemoveAt(0); }
            _frascos.Add(new Frasco(k));
        }

        /// <summary>A Grande Obra: so' depois dos 3s do fole inflando (o motor telegrafa).</summary>
        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            Vector3 c = _avisando ? _centroAviso : k.Pos;
            _avisando = false;
            _obra = new Nuvem(k, c, s["raio"], s["duracao"], s["dps"], s["lentidao"], true);
            k.AvisarEstado(GRANDE_OBRA, true);
        }

        /// <summary>O PRECO: o fole esvazia num suspiro comprido — 2s sem correr. Toda saida (tempo, vento, fogo) passa aqui.</summary>
        void FimDaObra(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            _obra = null;
            _selo.Soltar();
            k.AvisarEstado(GRANDE_OBRA, false);
            k.BuffVelocidade(s["ofegante_vel"], s["ofegante_dur"]);
            k.LigarEstado(OFEGANTE, s["ofegante_dur"]);
        }

        internal void NovaNuvem(KitRunner k, Vector3 centro)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            _nuvens.Add(new Nuvem(k, centro, t["nuvem_raio"], t["nuvem_dur"], t["dps"], t["lentidao"], false));
        }

        /// <summary>Olhos do Miasma: o contorno e' UM visual por alvo, renovado a cada tique enquanto ele esta' na nevoa.</summary>
        internal void Contornar(KitRunner k, IEntidade e)
        {
            EfeitoVisual v;
            if (_contornos.TryGetValue(e, out v) && v.Restante > 0f) { v.Restante = v.Duracao; return; }
            _contornos[e] = k.Visual("vex_contorno", e.Pos, e.Pos, 0.5f, k.Dados.Passiva["contorno_dur"], e);
        }

        /// <summary>O TIRO NO FRASCO e o §14: poca atingida detona onde o tiro parou (desperdicada) — ANTES das nuvens, para o
        /// vento que estourou a poca tambem dispersar a nuvem dela; depois vento/fogo nas nuvens e na Grande Obra.</summary>
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte)
        {
            float r = k.Dados.Tatica["tiro_raio"];
            for (int i = _frascos.Count - 1; i >= 0; i--)
            {
                Frasco f = _frascos[i];
                if (!f.Pousou || Plano(pos - f.Pos) > r) continue;
                f.Detonar(k, this);
                _frascos.RemoveAt(i);
            }
            for (int i = 0; i < _nuvens.Count; i++) _nuvens[i].Reagir(k, el, pos);
            if (_obra != null) _obra.Reagir(k, el, pos);
        }

        public void DanoRecebido(KitRunner k, float quanto) { }
        public void EstadoAcabou(KitRunner k, string nome) { }

        // ------------------------------------------------------------------ FRASCO
        /// <summary>Voa em arco `voo` s ate' o chao; pousado e' uma POCA inerte que arma em `arma` s e vive `poca_dur` s.</summary>
        public sealed class Frasco
        {
            public readonly Vector3 De, Para;
            public Vector3 Pos { get; private set; }
            public float Voo { get; private set; }
            public float Arma { get; private set; }
            public float Vida { get; private set; }
            public EfeitoVisual Visual { get; private set; }
            readonly float _vooTotal;
            float _acc;

            public bool Pousou => Voo <= 0f;
            public bool Armado => Pousou && Arma <= 0f;

            public Frasco(KitRunner k)
            {
                Dictionary<string, float> t = k.Dados.Tatica;
                De = k.Pos + Vector3.up * Pawn.ALTURA_MAO;
                Vector3 alvo = k.Pos + k.Mira() * t["alcance"];
                float chao = Arkana.World.Ilha.Atual != null ? Arkana.World.Ilha.AlturaDoChao(alvo.x, alvo.z) : k.Pos.y;
                Para = new Vector3(alvo.x, chao, alvo.z);
                Pos = De;
                _vooTotal = Voo = Mathf.Max(t["voo"], 0.01f);
                Arma = t["arma"];
                Vida = t["poca_dur"];
                Visual = k.Visual("vex_frasco", De, Para, 0.15f, Voo);
            }

            public bool Tick(KitRunner k, Vex vex, float dt)
            {
                if (!Pousou)
                {
                    Voo -= dt;
                    float f = 1f - Mathf.Max(Voo, 0f) / _vooTotal;
                    Pos = Vector3.Lerp(De, Para, f) + Vector3.up * (ARCO * 4f * f * (1f - f));
                    Visual.Pos = Pos;
                    if (Pousou)
                    {
                        Pos = Para;
                        Visual.Restante = 0f;
                        // quebra em caligrafia de circulo de transmutacao no chao (a casca le' "armado" pelo relogio do visual)
                        Visual = k.Visual("vex_poca", Para, Para, k.Dados.Tatica["gatilho"], Vida);
                    }
                    return true;
                }
                Vida -= dt;
                if (Vida <= 0f) { Sumir(); return false; }
                if (Arma > 0f) { Arma -= dt; return true; }   // INERTE: pisar ainda nao faz nada
                _acc += dt;
                if (_acc < TICK) return true;
                _acc = 0f;
                foreach (IEntidade e in k.AlvosPerto(Para, k.Dados.Tatica["gatilho"], k.Dono))
                    if (!MesmoTime(k.Dono, e)) { Detonar(k, vex); return false; }
                return true;
            }

            public void Detonar(KitRunner k, Vex vex)
            {
                Sumir();
                vex.NovaNuvem(k, Para);
            }

            public void Sumir() { Vida = 0f; Visual.Restante = 0f; }
        }

        // ------------------------------------------------------------------- NUVEM
        /// <summary>A nevoa (nuvem do frasco OU a Grande Obra): tique de area de 0,25s — DoT baixo + lentidao no inimigo; na
        /// Grande Obra, o SELO em todo mundo dentro. Vento dispersa, fogo consome em 2s.</summary>
        public sealed class Nuvem
        {
            public readonly Vector3 Centro;
            public readonly float Raio;
            public readonly bool EhObra;
            public float Restante { get; private set; }
            public bool Queimando { get; private set; }
            public readonly EfeitoVisual Visual;
            readonly float _dps, _lentidao;
            readonly Dictionary<string, float> _lim;
            float _acc;

            public Nuvem(KitRunner k, Vector3 centro, float raio, float dur, float dps, float lentidao, bool obra)
            {
                Centro = centro; Raio = raio; Restante = dur; _dps = dps; _lentidao = lentidao; EhObra = obra;
                _lim = obra ? k.Dados.Suprema : k.Dados.Tatica;
                _acc = TICK;   // o primeiro tique sai no estouro: quem pisou ja' sente
                Visual = k.Visual(obra ? "vex_obra" : "vex_nuvem", centro, centro, raio, dur);
            }

            public bool Dentro(Vector3 p) => Plano(p - Centro) <= Raio;

            public bool Tick(KitRunner k, Vex vex, float dt)
            {
                Restante -= dt;
                if (Restante <= 0f) { Visual.Restante = 0f; return false; }
                _acc += dt;
                if (_acc < TICK) return true;
                _acc = 0f;
                if (EhObra) vex._selo.Abrir();
                foreach (IEntidade e in k.AlvosPerto(Centro, Raio))
                {
                    if (EhObra) vex._selo.Selar(e);   // o selo vale para TODO MUNDO dentro — o time dele e ele mesmo
                    if (MesmoTime(k.Dono, e)) continue;
                    // `dono` como FONTE: o dano da nevoa credita a evolucao do escudo dele (GDD §5)
                    Combat.AplicarDot(e, _dps, TICK, "veneno", k.Dono);
                    Efeitos.Lentificar(e, _lentidao, TICK * 2f);
                    vex.Contornar(k, e);
                }
                if (EhObra) vex._selo.Fechar();
                return true;
            }

            /// <summary>§14: VENTO dispersa na hora; FOGO incendeia e consome em `fogo_consome` s (1x).</summary>
            public void Reagir(KitRunner k, Elemento el, Vector3 pos)
            {
                if (!Dentro(pos)) return;
                if (el == Elemento.Vento && _lim["vento_dispersa"] > 0f) Acabar();
                else if (el == Elemento.Fogo && !Queimando)
                {
                    Queimando = true;
                    Restante = Mathf.Min(Restante, _lim["fogo_consome"]);
                    Visual.Restante = Mathf.Min(Visual.Restante, Restante);
                    k.Visual("vex_fogo", Centro, Centro, Raio, Restante);
                }
            }

            public void Acabar() { Restante = 0f; Visual.Restante = 0f; }
        }

        // -------------------------------------------------------------------- SELO
        /// <summary>
        /// O SELO DE CURA (a Grande Obra; o Jardim da Vitalis usa o mesmo nos inimigos): selado NAO GANHA VIDA. Nao e' dano —
        /// e' a cura que nao entra: a vida que SUBIU desde o quadro anterior volta ao que era, sem Combat (nao credita, nao
        /// pisca, nao toca som). Derrubado e morto so' acompanham o valor (cair poe a reserva de esvaecimento na vida e o boneco
        /// renasce cheio: nada disso e' cura). A lista e' trocada a cada passada de area (Abrir/Selar/Fechar); Tick 1x por quadro.
        /// ponytail: DESFAZ depois do fato — cura e dano no mesmo quadro se compensam e a sobra da cura vaza; o selo de verdade
        /// e' um portao na Vitalidade.Curar (so' ela sabe o que e' cura).
        /// </summary>
        public sealed class Selo
        {
            readonly List<IEntidade> _quem = new List<IEntidade>();
            readonly List<float> _hp = new List<float>();
            readonly List<bool> _visto = new List<bool>();

            public int Selados => _quem.Count;
            public bool Selado(IEntidade e) => _quem.Contains(e);

            /// <summary>0 = nao conta (morto/caido): o proximo valor so' e' anotado, nunca desfeito.</summary>
            static float Rastro(IEntidade e) => e.Vital.Viva && !Derrubado.Esta(e) ? e.Vital.Hp : 0f;

            public void Abrir() { for (int i = 0; i < _visto.Count; i++) _visto[i] = false; }

            public void Selar(IEntidade e)
            {
                if (e == null || e.Vital == null) return;
                int i = _quem.IndexOf(e);
                if (i >= 0) { _visto[i] = true; return; }
                _quem.Add(e); _hp.Add(Rastro(e)); _visto.Add(true);
            }

            /// <summary>Quem nao foi Selar()-ado desde o Abrir() saiu da nevoa: sai do selo.</summary>
            public void Fechar()
            {
                for (int i = _quem.Count - 1; i >= 0; i--)
                    if (!_visto[i]) { _quem.RemoveAt(i); _hp.RemoveAt(i); _visto.RemoveAt(i); }
            }

            public void Tick()
            {
                for (int i = 0; i < _quem.Count; i++)
                {
                    IEntidade e = _quem[i];
                    Vitalidade v = e.Vital;
                    float agora = Rastro(e);
                    if (_hp[i] > 0f && agora > _hp[i])
                    {
                        v.Hp = _hp[i];
                        if (e.EhPlayer) Bus.EmitHealthChanged(v.Hp, v.HpMax);
                        agora = v.Hp;
                    }
                    _hp[i] = agora;
                }
            }

            public void Soltar() { _quem.Clear(); _hp.Clear(); _visto.Clear(); }
        }
    }
}
