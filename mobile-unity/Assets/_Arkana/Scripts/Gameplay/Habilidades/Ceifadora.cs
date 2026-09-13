using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA CEIFADORA (02) — Vanguarda. GDD §3, ficha em design/personagens/02-ceifadora.md, DIRECAO.md §6.
    ///   Passiva  Ecos dos Caidos — quem morre a ate' 40m deixa 60s o replay dos seus ultimos 3s (so' ela ve')
    ///   Tatica   Mao do Vazio    — ponto a ate' 12m: a rachadura corre ate' la' (0,5s) e a mao AGARRA o 1o inimigo 1,2s
    ///   Suprema  Travessia       — rasgo reto ate' 60m: ela atravessa na hora e a ENTRADA fica aberta 3s
    /// OS LIMITADORES SAO PARTE DO KIT: a mao AVISA (rachadura + circulo por 0,5s) e o agarrado AINDA CONJURA (prende
    /// posicao, nunca atordoa); a mao e' cortavel com 1 golpe; o rasgo fica ABERTO 3s e QUEM tocar a entrada atravessa
    /// atras (inimigo inclusive); quem atravessa sai REVELADO 4s e 1s SEM CONJURAR; o eco e' passado, nunca o agora.
    /// So' ela ve' o eco de graca: e' Visual do runner dela (bot nao tem kit).
    /// </summary>
    public sealed class Ceifadora : IHabilidade
    {
        /// <summary>O chip que a HUD ja' sabe desenhar (Textos.HudEstados): quem sai do Vazio sai revelado.</summary>
        public const string REVELADO = "revelado";
        /// <summary>s entre duas conferidas da entrada do rasgo aberto.</summary>
        const float TOQUE_S = 0.1f;

        /// <summary>A mao em curso: Atraso > 0 = a rachadura ainda corre; Preso != null = agarrando.</summary>
        public sealed class Mao
        {
            public Vector3 Ponto;
            public float Atraso;
            public IEntidade Preso;
            public EfeitoVisual Visual;
            public int Cortes;
            public bool Esquivava;
        }

        /// <summary>O rasgo aberto: entrada A (onde ela estava), saida B (onde ela saiu).</summary>
        public sealed class Rasgo
        {
            public Vector3 A, B;
            public float Aberto, Acc;
            public EfeitoVisual Visual;
            public readonly List<IEntidade> Atravessaram = new List<IEntidade>();
        }

        /// <summary>Um eco: o caminho dos ultimos segundos de quem caiu, em laco. O desenho pergunta Em(t).</summary>
        public sealed class Eco
        {
            public EfeitoVisual Visual;
            public Vector3[] Caminho;
            public float Replay;

            /// <summary>Onde o vulto esta' `t` segundos depois de nascer (o replay roda em laco de `Replay` s).</summary>
            public Vector3 Em(float t)
            {
                int n = Caminho.Length;
                if (n < 2) return Caminho[0];
                float f = Mathf.Repeat(t, Replay) / Mathf.Max(Replay, 0.001f) * (n - 1);
                int i = Mathf.Min((int)f, n - 2);
                return Vector3.Lerp(Caminho[i], Caminho[i + 1], f - i);
            }
        }

        /// <summary>Anel das ultimas posicoes de UM vivo no raio (a fita que vira eco se ele cair).</summary>
        private sealed class Rastro
        {
            public readonly Vector3[] Pts;
            public int N, Prox, Amostra;
            public Rastro(int n) { Pts = new Vector3[n]; }
            public void Por(Vector3 p) { Pts[Prox] = p; Prox = (Prox + 1) % Pts.Length; if (N < Pts.Length) N++; }
            /// <summary>i = 0 e' a mais velha.</summary>
            public Vector3 Em(int i) => Pts[(Prox - N + i + Pts.Length) % Pts.Length];
        }

        private Mao _mao;
        private Rasgo _rasgo;
        private readonly Dictionary<IEntidade, Rastro> _rastros = new Dictionary<IEntidade, Rastro>();
        private readonly List<IEntidade> _caidos = new List<IEntidade>();
        private readonly List<Eco> _ecos = new List<Eco>();
        private float _amostraAcc;
        private int _amostra;

        public Mao MaoAtiva => _mao;
        public Rasgo RasgoAberto => _rasgo;
        public IReadOnlyList<Eco> Ecos => _ecos;

        /// <summary>O eco que o desenho recebeu (a lista e' curta: um por morte recente).</summary>
        public Eco EcoDe(EfeitoVisual v)
        {
            for (int i = 0; i < _ecos.Count; i++) if (_ecos[i].Visual == v) return _ecos[i];
            return null;
        }

        public void Tick(KitRunner k, float dt)
        {
            Passiva(k, dt);
            TickMao(k, dt);
            TickRasgo(k, dt);
        }

        // ------------------------------------------------------------------ passiva

        /// <summary>
        /// Ecos dos Caidos. A MORTE se confere todo quadro (o boneco do treino levanta no quadro seguinte da Partida); a
        /// AMOSTRA de posicao, a cada `eco_amostra` s. Morreu no raio (visto na ultima amostra) = eco na posicao da queda.
        /// </summary>
        private void Passiva(KitRunner k, float dt)
        {
            Dictionary<string, float> p = k.Dados.Passiva;
            _caidos.Clear();
            foreach (KeyValuePair<IEntidade, Rastro> kv in _rastros)
                if (kv.Key.Vital == null || !kv.Key.Vital.Viva) _caidos.Add(kv.Key);
            for (int i = 0; i < _caidos.Count; i++)
            {
                Rastro r = _rastros[_caidos[i]];
                _rastros.Remove(_caidos[i]);
                if (r.N >= 2 && _amostra - r.Amostra <= 1) NovoEco(k, r, p);
            }
            for (int i = _ecos.Count - 1; i >= 0; i--) if (_ecos[i].Visual.Restante <= 0f) _ecos.RemoveAt(i);

            _amostraAcc += dt;
            if (_amostraAcc < p["eco_amostra"]) return;
            _amostraAcc = 0f;
            _amostra++;
            int n = Mathf.Max(2, Mathf.RoundToInt(p["eco_replay"] / p["eco_amostra"]));
            foreach (IEntidade e in k.AlvosPerto(k.Pos, p["eco_raio"], k.Dono))
            {
                Rastro r;
                if (!_rastros.TryGetValue(e, out r)) { r = new Rastro(n); _rastros[e] = r; }
                r.Por(e.Pos);
                r.Amostra = _amostra;
            }
        }

        private void NovoEco(KitRunner k, Rastro r, Dictionary<string, float> p)
        {
            var caminho = new Vector3[r.N];   // uma alocacao por MORTE, nunca por quadro
            for (int i = 0; i < r.N; i++) caminho[i] = r.Em(i);
            EfeitoVisual v = k.Visual("ceifadora_eco", caminho[r.N - 1], caminho[0], 0.4f, p["eco_janela"]);
            _ecos.Add(new Eco { Visual = v, Caminho = caminho, Replay = p["eco_replay"] });
        }

        // ------------------------------------------------------------------ tatica

        /// <summary>
        /// Mao do Vazio. "Marca um ponto a ate' 12m": o toque so' da' DIRECAO, entao o inimigo mais perto da linha de mira
        /// (ate' `mira_lado` m de lado) vira o ponto; ninguem na linha = o alcance cheio. A mao NAO nasce no alvo: a
        /// rachadura corre ate' o ponto por `atraso` s (DIRECAO §5: a magia viaja e a area avisa) — quem ve' sai dela.
        /// </summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            Vector3 dir = k.Mira();
            Vector3 ponto = k.Pos + dir * t["alcance"];
            float melhor = float.MaxValue;
            foreach (IEntidade e in k.AlvosPerto(k.Pos, t["alcance"], k.Dono))
            {
                if (!Inimigo(k.Dono, e)) continue;
                Vector3 d = e.Pos - k.Pos;
                d.y = 0f;
                float frente = Vector3.Dot(d, dir);
                if (frente <= 0f || frente >= melhor || (d - dir * frente).magnitude > t["mira_lado"]) continue;
                melhor = frente;
                ponto = e.Pos;
            }
            if (_mao != null && _mao.Preso != null) Soltar(_mao);
            _mao = new Mao { Ponto = ponto, Atraso = t["atraso"], Cortes = Mathf.Max(1, (int)t["golpes_corte"]) };
            _mao.Visual = k.Visual("ceifadora_marca", k.Pos, ponto, t["raio"], t["atraso"]);
        }

        private void TickMao(KitRunner k, float dt)
        {
            if (_mao == null) return;
            Dictionary<string, float> t = k.Dados.Tatica;
            if (_mao.Atraso > 0f)
            {
                _mao.Atraso -= dt;
                if (_mao.Atraso <= 0f) Irromper(k, t);
                return;
            }
            IEntidade p = _mao.Preso;
            if (p == null || _mao.Visual.Restante <= 0f || p.Vital == null || !p.Vital.Viva) { _mao = null; return; }
            // CORTAVEL COM 1 GOLPE. ponytail: o jogo ainda nao tem corpo a corpo; o "golpe" de hoje e' a ESQUIVA do agarrado
            // (a borda dos i-frames arranca a mao). Quando houver golpe corpo a corpo, ele entra aqui, no mesmo contador.
            bool esquiva = Efeitos.De(p).IframesLeft > 0f;
            if (esquiva && !_mao.Esquivava && --_mao.Cortes <= 0) { Soltar(_mao); _mao = null; return; }
            _mao.Esquivava = esquiva;
        }

        /// <summary>A mao irrompe: agarra o inimigo mais perto do centro na area. Ninguem = a mao fecha no ar e volta.</summary>
        private void Irromper(KitRunner k, Dictionary<string, float> t)
        {
            float r2 = t["raio"] * t["raio"], melhor = float.MaxValue;
            IEntidade preso = null;
            foreach (IEntidade e in k.AlvosPerto(_mao.Ponto, t["raio"] + 3f, k.Dono))
            {
                // em esquiva e' imune a TUDO (Efeitos: i-frames = nem estado)
                if (!Inimigo(k.Dono, e) || Efeitos.De(e).IframesLeft > 0f) continue;
                float d = Plano(e.Pos, _mao.Ponto);
                if (d <= r2 && d < melhor) { melhor = d; preso = e; }
            }
            if (preso == null)
            {
                k.Visual("ceifadora_mao", _mao.Ponto, _mao.Ponto, t["raio"], 0.6f);
                _mao = null;
                return;
            }
            _mao.Preso = preso;
            // PRENDE POSICAO pelo produto unico (o piso de velocidade), NUNCA atordoa: o agarrado ainda conjura (a lei da ficha)
            Efeitos.Lentificar(preso, Velocidade.Piso, t["agarra"]);
            _mao.Visual = k.Visual("ceifadora_mao", _mao.Ponto, _mao.Ponto, t["raio"], t["agarra"], preso);
        }

        /// <summary>Desfaz SO' a prisao da mao (lentidao de outra fonte que a substituiu fica).</summary>
        private static void Soltar(Mao m)
        {
            if (Efeitos.De(m.Preso).StatusMult <= Velocidade.Piso + 1e-4f) Efeitos.Lentificar(m.Preso, 1f, 0f);
            m.Visual.Restante = 0f;
        }

        // ------------------------------------------------------------------ suprema

        /// <summary>Travessia: so' quando a telegrafia acaba. Ela sai do outro lado na hora (o pouso seguro do corpo: nunca no
        /// mar, dentro do morro ou em cima de copa); a entrada fica aberta.</summary>
        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            Vector3 a = k.Pos;
            if (!k.Dono.Teleportar(a + k.Mira() * s["alcance"])) return;   // no ar: o Vazio nao abre
            _rasgo = new Rasgo { A = a, B = k.Pos, Aberto = s["aberto"] };
            _rasgo.Visual = k.Visual("ceifadora_rasgo", a, _rasgo.B, s["raio_toque"], s["aberto"]);
            _rasgo.Atravessaram.Add(k.Dono);
            Revelar(k, k.Dono, s);
        }

        /// <summary>
        /// O RASGO ABERTO: quem tocar a ENTRADA em `aberto` s atravessa atras e sai revelado. ponytail: SOLO, "ela e
        /// aliados" = qualquer um que tocar — e' tambem o limitador ("inimigos podem entrar atras"); com esquadrao a regra
        /// nao muda (o rasgo e' do Vazio, nao dela), so' o aliado passa a existir.
        /// </summary>
        private void TickRasgo(KitRunner k, float dt)
        {
            if (_rasgo == null) return;
            _rasgo.Aberto -= dt;
            if (_rasgo.Aberto <= 0f) { _rasgo = null; return; }
            _rasgo.Acc += dt;
            if (_rasgo.Acc < TOQUE_S) return;   // a busca na arena aloca: 10x por segundo, nunca por quadro
            _rasgo.Acc = 0f;
            Dictionary<string, float> s = k.Dados.Suprema;
            float r2 = s["raio_toque"] * s["raio_toque"];
            Vector3 lado = Vector3.Cross(Vector3.up, _rasgo.B - _rasgo.A).normalized;
            foreach (IEntidade e in k.AlvosPerto(_rasgo.A, s["raio_toque"] + 3f, k.Dono))
            {
                if (_rasgo.Atravessaram.Contains(e) || Plano(e.Pos, _rasgo.A) > r2) continue;
                // cada um sai um passo ao lado do anterior: ninguem nasce dentro de ninguem. So' CORPO atravessa.
                IConjurador c = e as IConjurador;
                if (c == null || !c.Teleportar(_rasgo.B + lado * (1.2f * _rasgo.Atravessaram.Count))) continue;
                _rasgo.Atravessaram.Add(e);
                Revelar(k, e, s);
            }
        }

        /// <summary>Quem sai do Vazio: REVELADO (feixe para todos + chip) e 1s SEM CONJURAR (vertigem do Vazio).</summary>
        private static void Revelar(KitRunner k, IEntidade quem, Dictionary<string, float> s)
        {
            k.Visual("revelado", quem.Pos, quem.Pos, 0.5f, s["revelado"], quem);
            if (quem == k.Dono)
            {
                k.LigarEstado(REVELADO, s["revelado"]);
                k.Silenciar(s["silencio"]);
                return;
            }
            IConjurador c = quem as IConjurador;
            if (c != null) c.AplicarEstado("silencio", s["silencio"]);
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void DanoRecebido(KitRunner k, float quanto) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        // ------------------------------------------------------------------ apoio

        /// <summary>Time: hoje o unico esquadrao e' o do player (Combat.Creditar). Bot contra bot sao inimigos.</summary>
        private static bool Inimigo(IEntidade eu, IEntidade e) => e != null && e != eu && !(eu.EhPlayer && e.EhPlayer);

        private static float Plano(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
