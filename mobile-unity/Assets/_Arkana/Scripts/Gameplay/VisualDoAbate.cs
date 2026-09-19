using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Core;
using Arkana.Characters;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// DERRUBADO E ELIMINADO na leitura de jogo comercial — so' LEITURA, nada aqui decide jogo. Tres coisas:
    /// (1) DERRUBADO por ESTADO (Derrubado.De a cada quadro: nao ha' evento para perder, e o reerguer/morrer apaga sozinho):
    /// ANEL vermelho pulsando no chao (brilho macio + aro grosso + onda que abre) e, sobre a cabeca de quem nao e' o jogador,
    /// um LOSANGO que se le' de longe (cresce com a distancia: o tamanho de tela segura) com o ANEL DO TEMPO em volta — o
    /// esvaecimento drenando, ou o reerguer enchendo em verde. O ALIADO caido (o parceiro, na dupla) acende no AZUL-ALIADO
    /// (Dupla.CorAliado), nunca no vermelho do inimigo: de longe se sabe quem ir buscar. (2) ELIMINADO pelo Bus.EntityDied (uma vez): estouro de ALMA
    /// na cor do elemento do mago (IdentidadeMago) — coluna de luz curta, faiscas subindo, clarao e onda no chao.
    /// (3) O CORPO de bot fica um instante cinza, AFUNDA soltando cinza e some. Quem depende do corpo (loot nao cai do
    /// morto; kill feed leva o NOME; BotsVivos e' contador da Partida; a camera segue o jogador) nao depende do VISUAL: so'
    /// o Mago filho some, o Pawn (entidade, colisor, registro na arena) fica. Voltou a viver (boneco do treino), volta.
    /// Pool: um sistema de MUNDO por particula (Emit em rajada, as velhas ficam onde nasceram), 4 colunas em roda, caidos
    /// reaproveitados. Zero Instantiate por evento, zero lixo por quadro, zero luz dinamica. Nasce sob a arena (morre com
    /// ela). Os handlers do Bus nao lancam: excecao aqui cortaria os ouvintes seguintes.
    /// </summary>
    public sealed class VisualDoAbate : MonoBehaviour
    {
        // ------------------------------------------------------------------ KNOBS (por foto)
        /// <summary>m: raio de fora do aro vermelho (o caido deitado mede ~1,8 m) e a fracao do furo (0,78 = aro de 25 cm).</summary>
        const float AnelM = 1.15f, AroDentro = 0.78f;
        /// <summary>Hz do pulso do derrubado: o aro acende, a onda abre, o losango respira. Coracao de quem esta' caido.</summary>
        const float PulsoHz = 1.2f;
        /// <summary>HDR do aro, da onda e do losango: acima de 1 passa do limiar do bloom e ACENDE.</summary>
        const float BrilhoAnel = 2.4f, BrilhoMarca = 1.7f;
        /// <summary>m acima do topo da cabeca DE PE' (o caido deita; o losango fica onde a cabeca estaria) e o balanco dele.</summary>
        const float MarcaAcima = 0.75f, MarcaBalanco = 0.07f;
        /// <summary>m do losango de perto; longe ele cresce com a distancia (m por metro) para segurar o tamanho de tela.</summary>
        const float MarcaM = 0.7f, MarcaPorMetro = 0.045f;
        /// <summary>Raio do anel do tempo DENTRO do losango (fracao do quad: casa com o trilho escuro da textura) e a espessura.</summary>
        const float ArcoRaio = 0.43f, ArcoLargura = 0.075f;
        const int PontosArco = 48;
        /// <summary>s da coluna de alma; m de altura e de largura na base; m que a onda do chao abre.</summary>
        public const float ColunaS = 0.9f;
        const float ColunaM = 7f, ColunaLargura = 1.3f, OndaM = 3.2f;
        const int Faiscas = 44, Almas = 12, CinzasN = 24;
        /// <summary>s de corpo cinza parado antes de afundar; s afundando; m que afunda (o maior mago tem 2,3 m).</summary>
        public const float CorpoFicaS = 1.4f, CorpoAfundaS = 1.4f;
        const float CorpoAfundaM = 2.5f;

        static readonly Color CorCaido = new Color(1f, 0.16f, 0.12f);
        static readonly Color CorEsvaecer = new Color(1f, 0.66f, 0.55f);
        static readonly Color CorReerguer = new Color(0.45f, 1f, 0.6f);   // o mesmo verde do anel de reerguer da HUD

        static Material _mAnel, _mBrilho, _mMarca, _mCinza;
        static Mesh _aro, _quad;
        static Vector3[] _circulo;
        static int _idCor;

        Partida _partida;
        MaterialPropertyBlock _mpb;
        int _quadro;
        ParticleSystem _faiscas, _almas, _clarao, _cinzas;
        readonly Dictionary<Pawn, Caido> _caidos = new Dictionary<Pawn, Caido>();
        readonly Stack<Caido> _livres = new Stack<Caido>();
        readonly List<Pawn> _velhos = new List<Pawn>();   // reusada no Recolher: zero lixo por quadro
        readonly Coluna[] _colunas = new Coluna[4];
        int _proxima;
        readonly List<Corpo> _corpos = new List<Corpo>();

        sealed class Caido
        {
            public Transform Chao, Marca;
            public Renderer Aro, Onda, Brilho, Losango;
            public LineRenderer Arco;
            public int Quadro, Pontos;
            public float Desde;
            public bool Verde;
        }

        sealed class Coluna
        {
            public Transform Raiz;
            public LineRenderer Luz, Miolo;
            public Renderer Onda;
            public Vector3 Pe;
            public Color Cor;
            public float Resta;
        }

        struct Corpo { public Pawn P; public Transform V; public Vector3 Base; public float T; public bool Cinza, Sumiu; }

        /// <summary>Quantos caidos tem anel no chao AGORA (o teste e o diag da foto leem).</summary>
        public int CaidosNaTela => _caidos.Count;

        /// <summary>Nasce sob a arena (morre com ela). `partida` null = Partida.Atual a cada quadro.</summary>
        public static VisualDoAbate Criar(Transform arena, Partida partida)
        {
            var go = new GameObject("VisualDoAbate");
            if (arena != null) go.transform.SetParent(arena, false);
            var v = go.AddComponent<VisualDoAbate>();
            v._partida = partida;
            return v;
        }

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (_idCor == 0) _idCor = Shader.PropertyToID("_BaseColor");
            if (_mAnel == null)
            {
                _mAnel = MaterialVfx.Solido(Color.white, MaterialVfx.Mistura.Aditivo, true);
                _mBrilho = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Aditivo, true, MaterialVfx.PontoSuave());
                _mMarca = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, TexturaMarca());
                _mCinza = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.PontoSuave());
                _aro = AroGrosso();
                _quad = QuadVfx();
                _circulo = new Vector3[PontosArco + 1];   // do topo, horario (visto da camera: +X local = direita da tela)
                for (int i = 0; i <= PontosArco; i++)
                {
                    float a = Mathf.PI * 2f * i / PontosArco;
                    _circulo[i] = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * ArcoRaio;
                }
            }
            // FAISCAS: riscos rapidos num cone para CIMA (+Z do Novo = cima), empuxo que acelera a subida
            _faiscas = Estouro("Faiscas", new Vector2(0.55f, 1.15f), new Vector2(4f, 9.5f), new Vector2(0.05f, 0.12f), -0.2f, 16f, 0.35f, 200, null);
            var rf = _faiscas.GetComponent<ParticleSystemRenderer>();
            rf.renderMode = ParticleSystemRenderMode.Stretch;
            rf.velocityScale = 0.045f;
            rf.lengthScale = 1f;
            // ALMAS: motas macias e lentas que sobem abrindo (o que sobra da luz do mago)
            _almas = Estouro("Almas", new Vector2(1.1f, 2f), new Vector2(0.6f, 2f), new Vector2(0.16f, 0.34f), -0.12f, 40f, 0.45f, 96, null);
            // CLARAO: UMA particula grande de ponto que abre e some (o "flash" da morte)
            _clarao = Estouro("Clarao", new Vector2(0.22f, 0.22f), Vector2.zero, new Vector2(2.8f, 2.8f), 0f, 0f, 0f, 8, null);
            ParticleSystem.ShapeModule sc = _clarao.shape;
            sc.enabled = false;
            ParticleSystem.SizeOverLifetimeModule sz = _clarao.sizeOverLifetime;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.25f));
            // CINZAS do corpo que afunda: fumaca em ALFA (cinza nao brilha) que abre ao subir
            _cinzas = Estouro("Cinzas", new Vector2(1.2f, 2.2f), new Vector2(0.4f, 1.3f), new Vector2(0.18f, 0.42f), -0.06f, 50f, 0.5f, 120, _mCinza);
            ParticleSystem.SizeOverLifetimeModule sc2 = _cinzas.sizeOverLifetime;
            sc2.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.3f));
            for (int i = 0; i < _colunas.Length; i++) _colunas[i] = NovaColuna(i);
            for (int i = 0; i < 2; i++) _livres.Push(NovoCaido());   // ponytail: 2 prontos; o 3o caido simultaneo nasce na hora (e fica no pool)
            // tudo configurado PARADO; so' agora toca (taxa 0: nada nasce sozinho, o Emit e' o estouro)
            _faiscas.Play(); _almas.Play(); _clarao.Play(); _cinzas.Play();
        }

        void OnEnable() { Bus.EntityDied += AoMorrer; }
        void OnDisable() { Bus.EntityDied -= AoMorrer; }

        void LateUpdate()
        {
            _quadro++;
            float dt = Time.deltaTime;
            Camera cam = Camera.main;
            Partida p = _partida ?? Partida.Atual;
            if (p != null)
                for (int i = 0; i < p.Arena.Count; i++)
                {
                    Pawn pw = p.Arena[i] as Pawn;
                    if (pw == null || !pw.Viva) continue;
                    Derrubado d = Derrubado.De(pw);
                    if (d == null) continue;
                    Caido c;
                    if (!_caidos.TryGetValue(pw, out c)) { c = PegarCaido(); _caidos[pw] = c; }
                    c.Quadro = _quadro;
                    PintarCaido(c, pw, d, cam, Dupla.CorDoCaido(p.Player, pw, CorCaido));
                }
            Recolher();
            Colunas(dt);
            Corpos(dt);
        }

        // ------------------------------------------------------------------ derrubado

        void PintarCaido(Caido c, Pawn p, Derrubado d, Camera cam, Color cor)
        {
            Vector3 pe = p.Pos;
            c.Chao.SetPositionAndRotation(pe + Vector3.up * 0.05f, Deitar(pe));
            float ciclo = (Time.time - c.Desde) * PulsoHz;
            ciclo -= Mathf.Floor(ciclo);                                  // 0 -> 1 a cada batida
            float bate = 0.5f + 0.5f * Mathf.Cos(ciclo * 2f * Mathf.PI);   // 1 no comeco da batida
            Pintar(c.Aro, cor, BrilhoAnel * (0.6f + 0.4f * bate), 1f);
            c.Onda.transform.localScale = Vector3.one * (AnelM * (1f + 0.85f * ciclo));   // a onda ABRE e apaga
            Pintar(c.Onda, cor, BrilhoAnel, (1f - ciclo) * (1f - ciclo));
            Pintar(c.Brilho, cor, 0.9f, 0.35f + 0.25f * bate);
            // o losango e' para os OUTROS: o jogador caido ja' tem o painel da HUD (e ele tamparia a mira)
            bool marca = !p.EhPlayer && cam != null;
            if (c.Marca.gameObject.activeSelf != marca) c.Marca.gameObject.SetActive(marca);
            if (!marca) return;
            float alto = (p.Visual != null ? p.Visual.Altura : Mago.AlturaRef) + MarcaAcima + MarcaBalanco * Mathf.Sin(ciclo * 2f * Mathf.PI);
            Vector3 pos = pe + Vector3.up * alto;
            float s = Mathf.Max(MarcaM, Vector3.Distance(cam.transform.position, pos) * MarcaPorMetro) * (1f + 0.08f * bate);
            c.Marca.SetPositionAndRotation(pos, cam.transform.rotation);   // de frente para a camera: o arco e o quad no plano da tela
            c.Marca.localScale = new Vector3(s, s, s);
            Pintar(c.Losango, cor, BrilhoMarca, 1f);
            // o ANEL DO TEMPO: reerguendo enche em verde; senao drena com o esvaecimento
            bool verde = d.Progresso > 0f;
            float frac = verde ? d.Progresso : d.Esvaecimento;
            int n = frac > 0.001f ? 1 + Mathf.CeilToInt(frac * PontosArco) : 0;
            if (n != c.Pontos)
            {
                c.Pontos = n;
                c.Arco.positionCount = n;
                if (n > 0) c.Arco.SetPositions(_circulo);   // o que passa do positionCount e' ignorado: o arco sai do mesmo circulo
            }
            c.Arco.enabled = n > 1;
            if (verde != c.Verde || c.Arco.startColor.a <= 0f)
            {
                c.Verde = verde;
                c.Arco.startColor = c.Arco.endColor = verde ? CorReerguer : CorEsvaecer;
            }
            c.Arco.widthMultiplier = ArcoLargura * s;   // a largura e' de mundo: a escala do pai nao chega nela
        }

        /// <summary>Quem saiu do estado (reergueu, morreu, sumiu da arena) devolve o caido ao pool.</summary>
        void Recolher()
        {
            _velhos.Clear();
            foreach (KeyValuePair<Pawn, Caido> kv in _caidos) if (kv.Value.Quadro != _quadro) _velhos.Add(kv.Key);
            for (int i = 0; i < _velhos.Count; i++)
            {
                Caido c = _caidos[_velhos[i]];
                c.Chao.gameObject.SetActive(false);
                c.Marca.gameObject.SetActive(false);
                _livres.Push(c);
                _caidos.Remove(_velhos[i]);
            }
        }

        Caido PegarCaido()
        {
            Caido c = _livres.Count > 0 ? _livres.Pop() : NovoCaido();
            c.Chao.gameObject.SetActive(true);
            c.Desde = Time.time;   // o pulso de cada caido comeca no zero dele
            c.Pontos = -1;
            c.Arco.startColor = c.Arco.endColor = Color.clear;   // forca a cor no primeiro quadro
            return c;
        }

        Caido NovoCaido()
        {
            var c = new Caido();
            c.Chao = new GameObject("Caido").transform;
            c.Chao.SetParent(transform, false);
            c.Brilho = Peca(c.Chao, _quad, _mBrilho);
            c.Brilho.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // o quad XY deita no chao
            c.Brilho.transform.localScale = Vector3.one * (AnelM * 3.2f);
            c.Aro = Peca(c.Chao, _aro, _mAnel);
            c.Aro.transform.localPosition = new Vector3(0f, 0.02f, 0f);   // por cima do brilho
            c.Aro.transform.localScale = Vector3.one * AnelM;
            c.Onda = Peca(c.Chao, MalhaVfx.Anel(), _mAnel);
            // a marca NAO e' filha do chao: o chao deita na encosta, a marca fica de frente para a camera
            c.Marca = new GameObject("Marca").transform;
            c.Marca.SetParent(transform, false);
            c.Losango = Peca(c.Marca, _quad, _mMarca);
            var arco = new GameObject("AnelDoTempo");
            arco.transform.SetParent(c.Marca, false);
            c.Arco = arco.AddComponent<LineRenderer>();
            c.Arco.useWorldSpace = false;
            c.Arco.alignment = LineAlignment.TransformZ;   // Z da marca = frente da camera: o arco fica de cara para a tela
            c.Arco.numCapVertices = 2;
            c.Arco.sharedMaterial = MaterialVfx.DeLinha();
            c.Arco.shadowCastingMode = ShadowCastingMode.Off;
            c.Arco.receiveShadows = false;
            c.Arco.sortingOrder = 1;   // por cima do trilho escuro do losango (os dois sao transparentes no mesmo ponto)
            c.Chao.gameObject.SetActive(false);
            c.Marca.gameObject.SetActive(false);
            return c;
        }

        // ------------------------------------------------------------------ eliminado

        void AoMorrer(IEntidade e)
        {
            Pawn p = e as Pawn;
            if (p == null) return;
            Eliminar(p.Pos, IdentidadeMago.De(p.Slug).Elemento);
            // ponytail: o corpo do JOGADOR fica (a camera e a tela de FIM olham para ele); so' bot/boneco afunda
            if (p.EhPlayer || p.Visual == null) return;
            for (int i = 0; i < _corpos.Count; i++) if (_corpos[i].P == p) return;
            Transform v = p.Visual.transform;
            _corpos.Add(new Corpo { P = p, V = v, Base = v.localPosition });
        }

        /// <summary>O estouro de ALMA em `pe` (os pes de quem morreu), na cor do elemento. Publico: a foto chama direto.</summary>
        public void Eliminar(Vector3 pe, Elemento el)
        {
            Color cor = Projetil.Tint(el);
            Color claro = Color.Lerp(cor, Color.white, 0.55f);
            Coluna k = _colunas[_proxima];
            _proxima = (_proxima + 1) % _colunas.Length;   // a 5a morte no mesmo segundo rouba a coluna mais velha
            k.Pe = pe;
            k.Cor = cor;
            k.Resta = ColunaS;
            k.Raiz.SetPositionAndRotation(pe + Vector3.up * 0.06f, Deitar(pe));
            Mostrar(k, true);
            PintarColuna(k);   // ja' no lugar: um quadro com a coluna na origem da arena apareceria na foto
            Emitir(_faiscas, pe + Vector3.up * 0.15f, Faiscas, claro, cor);
            Emitir(_almas, pe + Vector3.up * 0.8f, Almas, claro, cor);
            Emitir(_clarao, pe + Vector3.up, 1, claro, claro);
        }

        void Colunas(float dt)
        {
            for (int i = 0; i < _colunas.Length; i++)
            {
                Coluna k = _colunas[i];
                if (k.Resta <= 0f) continue;
                k.Resta -= dt;
                if (k.Resta <= 0f) { Mostrar(k, false); continue; }
                PintarColuna(k);
            }
        }

        /// <summary>A coluna SOBE num piscar (0,18 s), abre na base e afina ao apagar; o miolo e' quase branco. A onda no chao
        /// abre rapido e some. Degrade de alfa para o alto: a luz vaza para o ceu, nao termina num corte.</summary>
        void PintarColuna(Coluna k)
        {
            float f = 1f - k.Resta / ColunaS;   // 0 -> 1
            float sob = 1f - Mathf.Clamp01(f / 0.18f);
            Vector3 topo = k.Pe + Vector3.up * (ColunaM * (1f - sob * sob));
            float some = 1f - f * f;
            float larg = ColunaLargura * Mathf.Clamp01(f / 0.06f) * (0.3f + 0.7f * some);
            k.Luz.SetPosition(0, k.Pe); k.Luz.SetPosition(1, topo);
            k.Miolo.SetPosition(0, k.Pe); k.Miolo.SetPosition(1, topo);
            k.Luz.widthMultiplier = larg;
            k.Miolo.widthMultiplier = larg * 0.35f;
            Color c = k.Cor;
            c.a = some; k.Luz.startColor = c;
            c.a = 0f; k.Luz.endColor = c;
            Color m = Color.Lerp(k.Cor, Color.white, 0.75f);
            m.a = some; k.Miolo.startColor = m;
            m.a = 0f; k.Miolo.endColor = m;
            float g = 1f - f;
            k.Onda.transform.localScale = Vector3.one * (0.4f + OndaM * (1f - g * g * g));
            Pintar(k.Onda, k.Cor, BrilhoAnel, g * g);
        }

        static void Mostrar(Coluna k, bool on) { k.Luz.enabled = on; k.Miolo.enabled = on; k.Onda.enabled = on; }

        Coluna NovaColuna(int i)
        {
            var k = new Coluna { Raiz = new GameObject("Coluna" + i).transform };
            k.Raiz.SetParent(transform, false);
            k.Luz = Feixe(k.Raiz, "Luz");
            k.Miolo = Feixe(k.Raiz, "Miolo");
            k.Onda = Peca(k.Raiz, MalhaVfx.Anel(), _mAnel);
            Mostrar(k, false);
            return k;
        }

        /// <summary>Feixe de MUNDO virado para a camera, com a faixa macia do DeLinha (HDR aditivo), largo na base.</summary>
        static LineRenderer Feixe(Transform pai, string nome)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            var l = go.AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.positionCount = 2;
            l.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
            l.numCapVertices = 0;
            l.alignment = LineAlignment.View;
            l.sharedMaterial = MaterialVfx.DeLinha();
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            return l;
        }

        // ------------------------------------------------------------------ o corpo

        /// <summary>
        /// O corpo de bot: CorpoFicaS cinza parado (o olho registra QUEM caiu), depois AFUNDA (entra rapido no chao) soltando
        /// cinza e, no fundo, o Mago filho DESLIGA (renderers, luva e animacao de uma vez). Voltou a viver: posicao e visual
        /// voltam. O morto some da lista so' se a arena o destruir — 13 entradas por partida, sem busca por quadro.
        /// </summary>
        void Corpos(float dt)
        {
            for (int i = _corpos.Count - 1; i >= 0; i--)
            {
                Corpo c = _corpos[i];
                if (c.P == null || c.V == null) { _corpos.RemoveAt(i); continue; }   // a arena morreu antes
                if (c.P.Viva)
                {
                    c.V.localPosition = c.Base;
                    if (!c.V.gameObject.activeSelf) c.V.gameObject.SetActive(true);
                    _corpos.RemoveAt(i);
                    continue;
                }
                if (c.Sumiu) continue;
                c.T += dt;
                float f = (c.T - CorpoFicaS) / CorpoAfundaS;
                if (f > 0f && !c.Cinza)
                {
                    c.Cinza = true;
                    Vector3 pe = c.P.Pos;
                    Emitir(_cinzas, pe + Vector3.up * 0.3f, CinzasN, new Color(0.26f, 0.25f, 0.29f, 0.85f), new Color(0.48f, 0.46f, 0.52f, 0.55f));
                    Color cor = Projetil.Tint(IdentidadeMago.De(c.P.Slug).Elemento);
                    Emitir(_almas, pe + Vector3.up * 0.6f, Almas / 2, Color.Lerp(cor, Color.white, 0.4f), cor);
                }
                if (f >= 1f) { c.V.gameObject.SetActive(false); c.Sumiu = true; }
                else if (f > 0f) c.V.localPosition = c.Base + Vector3.down * (CorpoAfundaM * f * f);
                _corpos[i] = c;
            }
        }

        // ------------------------------------------------------------------ utilidades

        static void Emitir(ParticleSystem ps, Vector3 pos, int n, Color a, Color b)
        {
            ParticleSystem.MainModule m = ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(a, b);
            ps.transform.position = pos;   // MUNDO: as particulas velhas ficam onde nasceram
            ps.Emit(n);
        }

        /// <summary>Cor HDR por objeto sem material novo. Alfa separado: Color * k multiplicaria o alfa (aditivo sairia k^2).</summary>
        void Pintar(Renderer r, Color c, float k, float a)
        {
            _mpb.Clear();
            _mpb.SetColor(_idCor, new Color(c.r * k, c.g * k, c.b * k, a));
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Estouro PARADO (taxa 0) de MUNDO pelo ParticulaVfx.Novo, emitindo num CONE para cima (+Z do Novo).</summary>
        ParticleSystem Estouro(string nome, Vector2 vida, Vector2 vel, Vector2 tam, float gravidade, float cone, float raio, int max, Material mat)
        {
            ParticleSystem ps = ParticulaVfx.Novo(transform, nome, Color.white, Color.white, 0f, vida, vel, tam, true, 0f, max);
            ParticleSystem.MainModule m = ps.main;
            m.gravityModifier = gravidade;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = cone;
            sh.radius = Mathf.Max(raio, 0.01f);
            sh.radiusThickness = 1f;
            if (mat != null) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        /// <summary>Chao de verdade (terreno) deita o disco na encosta; em cima de pedra/ruina (o corpo acima do terreno), reto.
        /// ponytail: disco rigido nao abraca relevo acidentado — decal projetado se o aro enterrar na foto.</summary>
        static Quaternion Deitar(Vector3 pe)
        {
            Relevo r = Ilha.Atual != null ? Ilha.Atual.Relevo : null;
            if (r == null || Mathf.Abs(pe.y - Ilha.AlturaDoChao(pe.x, pe.z)) > 0.3f) return Quaternion.identity;
            return Quaternion.FromToRotation(Vector3.up, r.Normal(pe.x, pe.z));
        }

        /// <summary>Filho com malha, sem colisor (a camera e a mira nao podem esbarrar em VFX) e sem sombra.</summary>
        static Renderer Peca(Transform pai, Mesh malha, Material mat)
        {
            var go = new GameObject("Peca");
            go.transform.SetParent(pai, false);
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        /// <summary>Coroa de raio 1 no plano XZ com furo AroDentro (o MalhaVfx.Anel e' de 10%: a 9 m da camera, rasante, vira fio).</summary>
        static Mesh AroGrosso()
        {
            const int lados = 48;
            var v = new Vector3[(lados + 1) * 2];
            var t = new int[lados * 6];
            for (int i = 0; i <= lados; i++)
            {
                float a = Mathf.PI * 2f * i / lados;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i * 2] = d * AroDentro;
                v[i * 2 + 1] = d;
                if (i == lados) break;
                int k = i * 6, b = i * 2;
                t[k] = b; t[k + 1] = b + 1; t[k + 2] = b + 3;
                t[k + 3] = b; t[k + 4] = b + 3; t[k + 5] = b + 2;
            }
            var m = new Mesh { name = "VfxAroAbate", vertices = v, triangles = t };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Quadrado 1x1 no plano XY COM uv e cor de vertice (o shader de particula le' os dois; o MalhaVfx.Quad nao tem uv).</summary>
        static Mesh QuadVfx()
        {
            var m = new Mesh { name = "VfxQuadAbate" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            m.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// O LOSANGO do caido, 128x128 gerado 1x (zero arquivo), na lingua das placas da HUD: ARO claro, miolo escuro, nucleo
        /// claro, halo curto para fora e o TRILHO escuro do anel do tempo em volta (o arco aceso corre em cima dele: o que ja'
        /// drenou le' como vazio). Branco/cinza: a cor HDR vem do _BaseColor (aro e nucleo acendem, o miolo fica vinho).
        /// </summary>
        static Texture2D TexturaMarca()
        {
            const int n = 128;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "MarcaCaido", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float d = (Mathf.Abs(dx) + Mathf.Abs(dy)) * 1.1f;   // o losango (1,1 = cabe dentro do trilho)
                    float dentro = Mathf.Clamp01((0.62f - d) / 0.03f);
                    float aro = Mathf.Clamp01(1f - Mathf.Abs(d - 0.54f) / 0.07f);
                    float nucleo = Mathf.Clamp01((0.2f - d) / 0.03f);
                    float claro = Mathf.Max(aro, nucleo);
                    float halo = d > 0.6f ? 0.35f * Mathf.Clamp01(1f - (d - 0.6f) / 0.16f) : 0f;
                    float trilho = Mathf.Clamp01(1f - Mathf.Abs(r - ArcoRaio * 2f) / 0.06f);
                    float a = Mathf.Max(Mathf.Max(dentro * 0.92f, halo * halo * 2.8f), trilho * 0.6f);
                    float v = dentro > 0f ? Mathf.Lerp(0.28f, 1f, claro) : (trilho > halo ? 0.15f : 1f);
                    byte b = (byte)(Mathf.Clamp01(v) * 255f);
                    px[y * n + x] = new Color32(b, b, b, (byte)(Mathf.Clamp01(a) * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }
    }
}
