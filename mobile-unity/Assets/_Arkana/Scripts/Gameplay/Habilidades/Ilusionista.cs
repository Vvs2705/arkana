using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO ILUSIONISTA (08) — Guardiao do engano. GDD §3, ficha em design/personagens/08-ilusionista.md.
    ///   Passiva  Truque de Fuga    — DERRUBADO, quebra em cacos: invisivel 3s e um reflexo caido fica no lugar
    ///   Tatica   Espelho de Mao    — espelho fixo 2s a 1,5m na mira: DEVOLVE projetil inimigo a quem atirou, com 30% do dano
    ///   Suprema  Baile de Espelhos — 5 reflexos que ESPELHAM o passo dele por 6s + 2,5s invisivel
    /// OS LIMITADORES: o espelho so' pega PROJETIL que chega pela FRENTE (area e corpo a corpo passam) e QUEBRA na 3a
    /// devolucao (vidro alto); reflexo nao fere e anda INVERTIDO; invisivel QUEBRA ao conjurar (ataque ou tatica); o flash do
    /// reflexo quebrado so' ofusca (0,5s sem conjurar) quem estava a `flash_dist` m; no fim do Baile a ultima nota REVELA ele 1s.
    /// Invisivel e' do CORPO (IConjurador.AplicarEstado("invisivel")): a percepcao do bot deixa de ve'-lo (so' o brilho colado;
    /// o disparo continua denunciando) e o kit o APAGA quando ele aparece. Para o dono, a casca apaga o modelo e poe o
    /// vidro tremendo no lugar (VisualDosKits.GrupoB).
    /// SOLO: ninguem cai (Derrubado.SOLO_DERRUBA) — a passiva espera o esquadrao.
    /// </summary>
    public sealed class Ilusionista : IHabilidade
    {
        public const string ESPELHO = "espelho";
        public const string ESPELHO_QUEBRADO = "espelho_quebrado";
        /// <summary>O estado do corpo E o chip da HUD (o mesmo nome).</summary>
        public const string INVISIVEL = Pawn.INVISIVEL;
        public const string BAILE = "baile";
        public const string REVELADO = "revelado";
        /// <summary>s do estouro de cacos (sumir, voltar, quebrar).</summary>
        public const float CACOS = 0.6f;
        /// <summary>s entre as notas do fim do Baile (os reflexos quebram em sequencia).</summary>
        public const float NOTA = 0.12f;

        Espelho _espelho;
        Baile _baile;
        readonly List<Devolvido> _devolvidos = new List<Devolvido>();
        float _invisivel, _invisT;
        bool _caido;

        public bool Invisivel => _invisivel > 0f;
        public Espelho EspelhoAtivo => _espelho;
        public Baile BaileAtivo => _baile;
        public IReadOnlyList<Devolvido> Devolvidos => _devolvidos;

        static float Plano(Vector3 v) => new Vector2(v.x, v.z).magnitude;
        static Vector3 Chao(Vector3 v) => new Vector3(v.x, 0f, v.z);

        public void Tick(KitRunner k, float dt)
        {
            bool caido = Derrubado.Esta(k.Dono);
            if (caido && !_caido) TruqueDeFuga(k);
            _caido = caido;
            if (_invisivel > 0f)
            {
                _invisT += dt;
                _invisivel -= dt;
                // conjurar QUEBRA: o ataque zera o DesdeAtaque do runner — se ele ficou menor que o tempo sumido, atacou sumido
                if (_invisivel <= 0f || k.DesdeAtaque + 0.001f < _invisT) Aparecer(k);
            }
            if (_espelho != null && !_espelho.Tick(k, this, dt)) { _espelho = null; k.AvisarEstado(ESPELHO, false); }
            for (int i = _devolvidos.Count - 1; i >= 0; i--)
                if (!_devolvidos[i].Tick(k, dt)) _devolvidos.RemoveAt(i);
            if (_baile != null && !_baile.Tick(k, dt)) { _baile = null; k.AvisarEstado(BAILE, false); }
        }

        /// <summary>Espelho de Mao: saca do colete com floreio. A tatica e' conjurar: quebra a invisibilidade.</summary>
        public void Tatica(KitRunner k)
        {
            if (Invisivel) Aparecer(k);
            if (_espelho != null) _espelho.Sumir();
            _espelho = new Espelho(k);
            k.AvisarEstado(ESPELHO, true);
        }

        /// <summary>Baile de Espelhos: so' depois do acorde de vidro (o motor telegrafa).</summary>
        public void Suprema(KitRunner k)
        {
            _baile = new Baile(k);
            FicarInvisivel(k, k.Dados.Suprema["invisivel"]);
            k.AvisarEstado(BAILE, true);
        }

        /// <summary>Truque de Fuga: na BORDA do derrubado — some em cacos e deixa o reflexo CAIDO onde caiu.</summary>
        void TruqueDeFuga(KitRunner k)
        {
            Dictionary<string, float> p = k.Dados.Passiva;
            FicarInvisivel(k, p["invisivel"]);
            k.Visual("ilusionista_caido", k.Pos, k.Pos + k.Mira(), 0.5f, p["reflexo_caido"]);
        }

        void FicarInvisivel(KitRunner k, float dur)
        {
            if (_invisivel <= 0f) k.AvisarEstado(INVISIVEL, true);
            _invisivel = Mathf.Max(_invisivel, dur);
            _invisT = 0f;
            k.Dono.AplicarEstado(INVISIVEL, dur);
            k.Visual("ilusionista_cacos", k.Pos, k.Pos, 1f, CACOS);
        }

        /// <summary>Volta a ser visto: recompoe em cacos de luz e o corpo volta para a visao dos bots.</summary>
        void Aparecer(KitRunner k)
        {
            _invisivel = 0f;
            k.Dono.AplicarEstado(INVISIVEL, 0f);
            k.Visual("ilusionista_cacos", k.Pos, k.Pos, 1f, CACOS);
            k.AvisarEstado(INVISIVEL, false);
        }

        internal void Devolver(KitRunner k, Projetil p, float fracao) => _devolvidos.Add(new Devolvido(k, p, fracao));

        public void DanoRecebido(KitRunner k, float quanto) { }
        public void EstadoAcabou(KitRunner k, string nome) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        // ----------------------------------------------------------------- ESPELHO
        /// <summary>Segmento A-B de pe', de frente para a mira (Normal). Pega o projetil inimigo que VEM pela frente e esta' a
        /// `captura` m do vidro (a faixa cobre o passo de um tiro a 30 fps).</summary>
        public sealed class Espelho
        {
            /// <summary>m de altura do vidro (a logica mede o tiro nessa faixa acima dos pes).</summary>
            public const float ALTURA = 2.1f;
            public readonly Vector3 Centro, Normal, A, B;
            public int Devolucoes { get; private set; }
            public float Duracao { get; private set; }
            public readonly EfeitoVisual Visual;
            readonly Dictionary<string, float> _t;
            readonly IEntidade _dono;

            public Espelho(KitRunner k)
            {
                _t = k.Dados.Tatica;
                _dono = k.Dono;
                Normal = k.Mira();
                Centro = k.Pos + Normal * _t["distancia"];
                Vector3 lado = Vector3.Cross(Normal, Vector3.up).normalized * (_t["largura"] * 0.5f);
                A = Centro - lado;
                B = Centro + lado;
                Duracao = _t["duracao"];
                Visual = k.Visual("ilusionista_espelho", A, B, _t["captura"], Duracao);
            }

            public void Sumir() { Duracao = 0f; Visual.Restante = 0f; }

            /// <summary>Por QUADRO (tiro rapido nao espera tique de area). false = acabou (tempo ou a 3a devolucao).</summary>
            public bool Tick(KitRunner k, Ilusionista ilu, float dt)
            {
                Duracao -= dt;
                if (Duracao <= 0f) { Visual.Restante = 0f; return false; }
                IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
                if (vivos == null) return true;
                float cap = _t["captura"];
                for (int i = vivos.Count - 1; i >= 0; i--)
                {
                    Projetil p = vivos[i];
                    if (p == null || !p.Vivo || Vex.MesmoTime(_dono, p.Atirador)) continue;   // o proprio e o do time passam
                    if (Vector3.Dot(p.Dir, Normal) >= 0f) continue;   // pelas costas ou de raspao: passa
                    float dy = p.Pos.y - Centro.y;
                    if (dy < -Projetil.RAIO_HITBOX || dy > ALTURA + Projetil.RAIO_HITBOX) continue;
                    if (KitRunner.DistSegmento(Chao(p.Pos), Chao(A), Chao(B)) > cap) continue;
                    vivos.RemoveAt(i);   // pego pelo vidro: sai da arena sem impacto
                    k.AoAbsorver?.Invoke(p);
                    ilu.Devolver(k, p, _t["fracao_dano"]);
                    Devolucoes++;
                    k.Visual("ilusionista_brilho", p.Pos, p.Pos, 0.5f, 0.3f);   // o acorde de cristal
                    if (Devolucoes < (int)_t["devolucoes"]) continue;
                    // o PRECO: a 3a devolucao QUEBRA o espelho — vidro alto, todos sabem
                    Sumir();
                    k.Visual("ilusionista_cacos", Centro + Vector3.up * 1f, Centro, 1.2f, CACOS);
                    k.LigarEstado(ESPELHO_QUEBRADO, 0.5f);
                    return false;
                }
                return true;
            }
        }

        // --------------------------------------------------------------- DEVOLVIDO
        /// <summary>O tiro que o espelho devolve: mesmo elemento e velocidade, `fracao` do dano, NA DIRECAO de quem atirou (a
        /// posicao dele na hora — se andou, erra). Tiro de kit: voa aqui e acerta o primeiro corpo do trajeto.</summary>
        public sealed class Devolvido
        {
            public Vector3 Pos { get; private set; }
            public readonly Vector3 Dir;
            public readonly Elemento El;
            public readonly float Dano;
            public readonly EfeitoVisual Visual;
            readonly float _vel;
            readonly IEntidade _dono;
            float _alcance;

            public Devolvido(KitRunner k, Projetil p, float fracao)
            {
                _dono = k.Dono;
                El = p.ElementoDoTiro;
                Dano = p.Dano * fracao;
                _vel = Mathf.Max(p.Velocidade, 1f);
                Pos = p.Pos;
                Vector3 d = p.Atirador != null ? p.Atirador.Pos + Vector3.up * 1.2f - Pos : -p.Dir;
                Dir = d.sqrMagnitude > 0.0001f ? d.normalized : -p.Dir;
                _alcance = d.magnitude + 6f;   // passa um pouco de quem atirou e some
                Visual = k.Visual("ilusionista_devolvido", Pos, Pos - Dir, 0.2f, _alcance / _vel + 0.1f);
            }

            public bool Tick(KitRunner k, float dt)
            {
                float passo = _vel * dt;
                Vector3 de = Pos;
                Pos += Dir * passo;
                _alcance -= passo;
                Visual.Pos = Pos;
                Visual.Pos2 = Pos - Dir * 1.4f;
                IEntidade alvo = Aelion.NoTrajeto(k, de, Pos, _dono);
                if (alvo != null)
                {
                    // o elemento do tiro ORIGINAL continua valendo (molhado + raio conduz, fogo acende) — so' o dano e' 30%
                    float mult = Efeitos.Aplicar(alvo, El, Dano, _dono);
                    Combat.AplicarDano(alvo, Dano * mult, El, _dono);
                    Bus.EmitTerrainHit(El, Pos, false);
                    Visual.Restante = 0f;
                    return false;
                }
                if (_alcance > 0f) return true;
                Visual.Restante = 0f;
                return false;
            }
        }

        // -------------------------------------------------------------------- BAILE
        /// <summary>
        /// A roda de reflexos em volta de onde ele conjurou. Cada reflexo e' o mundo visto num ESPELHO posto entre ele e o
        /// reflexo: o passo na direcao do reflexo INVERTE, o de lado segue — o passo trocado que o observador atento nota.
        /// Tiro inimigo que pega num reflexo morre nele e o QUEBRA; o clarao ofusca quem atirou de perto. No fim quebram em
        /// sequencia (uma NOTA cada) e a ultima nota e' a posicao REAL dele, revelada.
        /// </summary>
        public sealed class Baile
        {
            public readonly Vector3 Centro;
            public readonly Vector3[] Normais, Base, Pos;
            public readonly bool[] Inteiro;
            public readonly EfeitoVisual[] Visuais;
            public float Duracao { get; private set; }
            public bool Revelou { get; private set; }
            readonly Dictionary<string, float> _s;
            readonly IEntidade _dono;
            float _fim;
            int _proxima;

            public int Inteiros { get { int n = 0; for (int i = 0; i < Inteiro.Length; i++) if (Inteiro[i]) n++; return n; } }

            public Baile(KitRunner k)
            {
                _s = k.Dados.Suprema;
                _dono = k.Dono;
                int n = Mathf.Max((int)_s["reflexos"], 1);
                Centro = k.Pos;
                Duracao = _s["duracao"];
                Normais = new Vector3[n]; Base = new Vector3[n]; Pos = new Vector3[n]; Inteiro = new bool[n];
                Visuais = new EfeitoVisual[n];
                Vector3 f = k.Mira();
                for (int i = 0; i < n; i++)
                {
                    // o 1o reflexo na mira: a roda abre de frente para quem ele encara
                    float a = Mathf.PI * 2f * i / n, c = Mathf.Cos(a), s = Mathf.Sin(a);
                    Vector3 d = new Vector3(f.x * c + f.z * s, 0f, -f.x * s + f.z * c);
                    Normais[i] = d;
                    Base[i] = Pos[i] = Centro + d * _s["raio"];
                    Inteiro[i] = true;
                    Visuais[i] = k.Visual("ilusionista_reflexo", Pos[i], Pos[i] + d, 0.5f, Duracao + n * NOTA + 0.1f);
                }
            }

            public bool Tick(KitRunner k, float dt)
            {
                if (Duracao > 0f)
                {
                    Duracao -= dt;
                    Espelhar();
                    Tiros(k);
                    return true;
                }
                // o fim em NOTAS: um reflexo por NOTA; a ultima revela onde ele esta'
                _fim += dt;
                while (_fim >= NOTA && _proxima < Inteiro.Length)
                {
                    _fim -= NOTA;
                    if (Inteiro[_proxima]) Quebrar(k, _proxima, null);
                    _proxima++;
                }
                if (_proxima < Inteiro.Length) return true;
                float r = _s["revela_fim"];
                k.Visual(REVELADO, _dono.Pos, _dono.Pos, 0.5f, r, _dono);
                k.LigarEstado(REVELADO, r);
                Revelou = true;
                return false;
            }

            void Espelhar()
            {
                Vector3 delta = Chao(_dono.Pos - Centro);
                for (int i = 0; i < Pos.Length; i++)
                {
                    if (!Inteiro[i]) continue;
                    Vector3 n = Normais[i];
                    Pos[i] = Base[i] + delta - 2f * Vector3.Dot(delta, n) * n;
                    Visuais[i].Pos = Pos[i];
                    Visuais[i].Pos2 = Pos[i] + n;
                }
            }

            void Tiros(KitRunner k)
            {
                IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
                if (vivos == null) return;
                float r = Partida.RAIO_CORPO + Projetil.RAIO_HITBOX;
                for (int j = vivos.Count - 1; j >= 0; j--)
                {
                    Projetil p = vivos[j];
                    if (p == null || !p.Vivo || Vex.MesmoTime(_dono, p.Atirador)) continue;
                    for (int i = 0; i < Pos.Length; i++)
                    {
                        if (!Inteiro[i]) continue;
                        float dy = p.Pos.y - Pos[i].y;
                        if (dy < -Projetil.RAIO_HITBOX || dy > Partida.ALTURA_CORPO + Projetil.RAIO_HITBOX) continue;
                        if (Plano(p.Pos - Pos[i]) > r) continue;
                        vivos.RemoveAt(j);   // o tiro morre no reflexo, como num corpo
                        k.AoAbsorver?.Invoke(p);
                        Quebrar(k, i, p.Atirador);
                        break;
                    }
                }
            }

            /// <summary>Quebra o reflexo `i`. `quem` = quem quebrou: o FLASH so' ofusca (sem conjurar) a curtissima distancia.</summary>
            public void Quebrar(KitRunner k, int i, IEntidade quem)
            {
                Inteiro[i] = false;
                Visuais[i].Restante = 0f;
                k.Visual("ilusionista_cacos", Pos[i] + Vector3.up * 1f, Pos[i], 1f, CACOS);
                IConjurador c = quem as IConjurador;
                if (c != null && Plano(quem.Pos - Pos[i]) <= _s["flash_dist"]) c.AplicarEstado("silencio", _s["ofusca"]);
            }
        }
    }
}
