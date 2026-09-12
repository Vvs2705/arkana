using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    // =================================================================================== LEITURA (pura)

    /// <summary>
    /// A LEITURA dos kits, PURA (o EditMode chama sem cena): quanto um efeito ainda aparece, o tremor do fio e o raio
    /// do aviso da suprema. A casca (VisualDosKits) so' aplica isto aos objetos.
    /// </summary>
    public static class LeituraDosKits
    {
        /// <summary>Fracao FINAL da vida em que o efeito vai sumindo; antes disso aparece inteiro.</summary>
        public const float FracaoDoSumico = 0.25f;
        /// <summary>Piso enquanto VIVO: efeito que ainda age nunca fica invisivel (o visual nao mente a regra).</summary>
        public const float Piso = 0.3f;
        /// <summary>m de tremor no MEIO do fio (as pontas ficam cravadas nas ancoras).</summary>
        public const float TremorFio = 0.18f;
        /// <summary>Raio do aviso quando a suprema nao tem "raio" na ficha (o Braco Livre e' leque, nao area).</summary>
        public const float RaioAvisoPadrao = 3.5f;

        /// <summary>0 = acabou (some). Vivo: 1 ate' os ultimos 25% da vida, depois desce ate' o Piso. NaN/negativo = 0.</summary>
        public static float EscalaPorRestante(float restante, float duracao)
        {
            if (!(restante > 0f)) return 0f;
            if (!(duracao > 0f)) return 1f;
            float f = Mathf.Clamp01(restante / (duracao * FracaoDoSumico));
            return Piso + (1f - Piso) * f;
        }

        /// <summary>
        /// Pontos do fio de `a` a `b` com tremor DETERMINISTICO (mesmo tempo = mesmo desenho: replay e teste batem).
        /// As pontas sao EXATAMENTE a e b — a ancora e' o ponto fraco e tem de estar onde a logica mede. Tremor maximo
        /// no meio, zero nas pontas. `saida` e' reaproveitada se tiver o tamanho certo (zero lixo por frame).
        /// </summary>
        public static Vector3[] PontosDoFio(Vector3 a, Vector3 b, int n, float tempo, Vector3[] saida = null)
        {
            n = Mathf.Max(n, 2);
            if (saida == null || saida.Length != n) saida = new Vector3[n];
            Vector3 d = b - a;
            Vector3 lado = Vector3.Cross(d, Vector3.up);
            lado = lado.sqrMagnitude > 1e-6f ? lado.normalized : Vector3.right;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float env = Mathf.Sin(t * Mathf.PI) * TremorFio;
                // soma de senos com fase por ponto: barato, sem Random, e o "passo de anime" vem de quem chama
                float s1 = Mathf.Sin(tempo * 31f + i * 2.3f) * 0.6f + Mathf.Sin(tempo * 17f + i * 5.1f) * 0.4f;
                float s2 = Mathf.Sin(tempo * 23f + i * 3.7f);
                saida[i] = a + d * t + lado * (s1 * env) + Vector3.up * (s2 * env);
            }
            saida[0] = a;
            saida[n - 1] = b;
            return saida;
        }

        /// <summary>Area do aviso: o "raio" da suprema na ficha (Mare, Tear-Mae); sem ele, o padrao.</summary>
        public static float RaioDoAviso(string slug)
        {
            Kits.KitDef k = Kits.De(slug);
            float r;
            return k.Suprema != null && k.Suprema.TryGetValue("raio", out r) && r > 0f ? r : RaioAvisoPadrao;
        }
    }

    // ============================================================================ FERRAMENTAS DE VFX

    /// <summary>
    /// Materiais de VFX por codigo (zero asset). Shaders SO' da lista Build.ShadersDoCodigo (URP Unlit e
    /// Particles/Unlit): fora dela o Shader.Find devolve null no aparelho. A transparencia e' escrita a mao como o
    /// BaseShaderGUI do URP 17 faz (_Surface/_Blend/_SrcBlend/_DstBlend/_ZWrite + keyword + fila 3000) — material
    /// criado em runtime nao passa pelo inspector que faria isso. Instancing ligado: o terreno desenha por lote.
    /// </summary>
    public static class MaterialVfx
    {
        public enum Mistura { Opaco, Alfa, Aditivo }

        public const string Unlit = "Universal Render Pipeline/Unlit";
        public const string ParticulaUnlit = "Universal Render Pipeline/Particles/Unlit";
        static Material _particula, _linha;
        static Texture2D _ponto, _faixa;

        /// <summary>Cor chapada (Unlit: nao le' cor de vertice). Null so' sem URP no build.</summary>
        public static Material Solido(Color cor, Mistura m, bool duploLado = false) => Novo(Unlit, cor, m, duploLado, null);

        /// <summary>Particulas, fios e malhas com cor de vertice: aditivo com ponto macio. UM para todos (a cor vem do vertice).
        /// Cor base ACIMA de 1 (HDR): o miolo passa do limiar do bloom do pos (Ilha.MontarPos) e a magia ACENDE em vez de
        /// so' pintar. KNOB: 1,8 por foto (17/18-kit).</summary>
        public const float BrilhoHdr = 1.8f;

        public static Material DeParticula()
        {
            // alfa fica 1: Color.white * k multiplicaria o alfa junto, e no aditivo (SrcAlpha) o brilho sairia k^2
            if (_particula == null) _particula = Novo(ParticulaUnlit, new Color(BrilhoHdr, BrilhoHdr, BrilhoHdr, 1f), Mistura.Aditivo, true, PontoSuave());
            return _particula;
        }

        /// <summary>Fios e feixes (LineRenderer): o mesmo aditivo HDR do DeParticula, mas com FAIXA macia — alfa so' na
        /// LARGURA. O ponto do DeParticula esticado no comprimento apagava as pontas: o fio de 6 m acendia so' no meio e
        /// sumia justo nas ancoras (o ponto fraco que tem de aparecer).</summary>
        public static Material DeLinha()
        {
            if (_linha == null) _linha = Novo(ParticulaUnlit, new Color(BrilhoHdr, BrilhoHdr, BrilhoHdr, 1f), Mistura.Aditivo, true, FaixaSuave());
            return _linha;
        }

        /// <summary>Cor de vertice (degrade da cunha de fogo) sem textura, em alfa.</summary>
        public static Material CorDeVertice(Mistura m) => Novo(ParticulaUnlit, Color.white, m, true, null);

        public static Material Novo(string shader, Color cor, Mistura m, bool duploLado, Texture tex)
        {
            Shader s = Shader.Find(shader);
            if (s == null) s = Shader.Find(Unlit);
            if (s == null) return null;
            var mat = new Material(s) { name = "Vfx", enableInstancing = true };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", cor);
            if (tex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (m != Mistura.Opaco)
            {
                BlendMode dst = m == Mistura.Aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha;
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", m == Mistura.Aditivo ? 2f : 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)dst);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)dst);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetShaderPassEnabled("DepthOnly", false);
                mat.SetShaderPassEnabled("ShadowCaster", false);
            }
            if (duploLado) mat.SetFloat("_Cull", 0f);
            return mat;
        }

        /// <summary>Ponto de luz macio 32x32 gerado 1x (zero arquivo): particula quadrada chapada nao le' como fogo.</summary>
        public static Texture2D PontoSuave()
        {
            if (_ponto != null) return _ponto;
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "PontoSuave", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            _ponto = t;
            return t;
        }

        /// <summary>Faixa 4x32 (zero arquivo): alfa so' no v (a largura da linha), cheio no u (o comprimento). Miolo
        /// duro, borda macia — o mesmo perfil do PontoSuave, sem apagar nas pontas.</summary>
        public static Texture2D FaixaSuave()
        {
            if (_faixa != null) return _faixa;
            const int n = 32;
            var t = new Texture2D(4, n, TextureFormat.RGBA32, false) { name = "FaixaSuave", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[4 * n];
            for (int y = 0; y < n; y++)
            {
                float a = Mathf.Clamp01(1f - Mathf.Abs((y + 0.5f) / n * 2f - 1f));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return _faixa = t;
        }
    }

    /// <summary>Malhas de VFX geradas 1x (zero arquivo) pelo mesmo Construtor facetado da ilha.</summary>
    public static class MalhaVfx
    {
        static Mesh _quad, _disco, _anel, _cunha;
        static readonly Dictionary<PrimitiveType, Mesh> _prims = new Dictionary<PrimitiveType, Mesh>();

        /// <summary>Quadrado 1x1 no plano XZ, centro na origem, virado para cima.</summary>
        public static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var b = new MalhaProc.Construtor();
            Vector3 p0 = new Vector3(-0.5f, 0f, -0.5f), p1 = new Vector3(0.5f, 0f, -0.5f);
            Vector3 p2 = new Vector3(0.5f, 0f, 0.5f), p3 = new Vector3(-0.5f, 0f, 0.5f);
            b.Tri(p0, p1, p2, Color.white, Vector3.up);
            b.Tri(p0, p2, p3, Color.white, Vector3.up);
            return _quad = b.ParaMesh("VfxQuad");
        }

        /// <summary>Disco de raio 1 no plano XZ.</summary>
        public static Mesh Disco()
        {
            if (_disco != null) return _disco;
            var b = new MalhaProc.Construtor();
            b.Tampa(Vector3.zero, 1f, 20, Color.white);
            return _disco = b.ParaMesh("VfxDisco");
        }

        /// <summary>Coroa de raio 1 (borda do aviso), 10% de espessura.</summary>
        public static Mesh Anel()
        {
            if (_anel != null) return _anel;
            const int lados = 40;
            const float r0 = 0.9f;
            var b = new MalhaProc.Construtor();
            for (int i = 0; i < lados; i++)
            {
                float a0 = Mathf.PI * 2f * i / lados, a1 = Mathf.PI * 2f * (i + 1) / lados;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                b.Tri(d0 * r0, d0, d1, Color.white, Vector3.up);
                b.Tri(d0 * r0, d1, d1 * r0, Color.white, Vector3.up);
            }
            return _anel = b.ParaMesh("VfxAnel");
        }

        /// <summary>
        /// Cunha (prisma de topo pontudo): base de largura 1 em y=0, crista em y=1, comprida em Z (-0.5..0.5). Silhueta
        /// de CHAMA, nao de caixa ("retangulo laranja" foi a queixa do Diretor em 26/08). Degrade na cor de vertice:
        /// base laranja-avermelhada, crista amarela.
        /// </summary>
        public static Mesh Cunha()
        {
            if (_cunha != null) return _cunha;
            var b = new MalhaProc.Construtor();
            Vector3 e0 = new Vector3(-0.5f, 0f, -0.5f), d0 = new Vector3(0.5f, 0f, -0.5f), t0 = new Vector3(0f, 1f, -0.5f);
            Vector3 e1 = new Vector3(-0.5f, 0f, 0.5f), d1 = new Vector3(0.5f, 0f, 0.5f), t1 = new Vector3(0f, 1f, 0.5f);
            Vector3 esq = new Vector3(-1f, 0.5f, 0f), dir = new Vector3(1f, 0.5f, 0f);
            b.Tri(e0, e1, t1, Color.white, esq); b.Tri(e0, t1, t0, Color.white, esq);
            b.Tri(d0, t0, t1, Color.white, dir); b.Tri(d0, t1, d1, Color.white, dir);
            b.Tri(e0, t0, d0, Color.white, Vector3.back);
            b.Tri(e1, d1, t1, Color.white, Vector3.forward);
            Mesh m = b.ParaMesh("VfxCunha");
            PintarPorAltura(m, new Color(0.95f, 0.25f, 0.05f), new Color(1f, 0.85f, 0.25f));
            return _cunha = m;
        }

        /// <summary>Degrade vertical na cor de vertice (os shaders de particula leem). sRGB -> linear como a Ilha faz.</summary>
        public static void PintarPorAltura(Mesh m, Color baixo, Color alto)
        {
            Vector3[] v = m.vertices;
            Bounds bb = m.bounds;
            float h = Mathf.Max(bb.size.y, 1e-4f);
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            var c = new Color[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                Color k = Color.Lerp(baixo, alto, (v[i].y - bb.min.y) / h);
                c[i] = linear ? k.linear : k;
            }
            m.colors = c;
        }

        /// <summary>A malha de uma primitiva do Unity (esfera, capsula, cilindro) sem o GameObject nem o colisor.</summary>
        public static Mesh Primitiva(PrimitiveType tipo)
        {
            Mesh m;
            if (_prims.TryGetValue(tipo, out m) && m != null) return m;
            GameObject tmp = GameObject.CreatePrimitive(tipo);
            tmp.SetActive(false);   // o colisor dela nao pode existir nem por um frame (a camera e a mira fazem raycast)
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.Destroy(tmp);
            _prims[tipo] = m;
            return m;
        }
    }

    /// <summary>Emissores de particula por codigo (zero asset), em pe' (+Z local = cima do pai).</summary>
    public static class ParticulaVfx
    {
        /// <summary>
        /// Emissor PARADO — quem usa da' Play. Caixa de emissao: x e y no chao, z na vertical (sh.scale). NUNCA toca em
        /// main.duration: com o sistema tocando o Unity loga erro (e o PlayMode reprova log).
        /// `aleatorio` 0 = sobe reto, 1 = espirra para todo lado (faisca).
        /// </summary>
        public static ParticleSystem Novo(Transform pai, string nome, Color a, Color b, float taxa, Vector2 vida,
            Vector2 vel, Vector2 tam, bool mundo, float aleatorio = 0f, int max = 48)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(vida.x, vida.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(vel.x, vel.y);
            main.startSize = new ParticleSystem.MinMaxCurve(tam.x, tam.y);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = 0f;
            main.maxParticles = max;
            main.simulationSpace = mundo ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = taxa;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = Vector3.one;
            sh.randomDirectionAmount = aleatorio;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.05f));   // nasce cheia, morre um fiapo
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialVfx.DeParticula();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        /// <summary>Chama: amarelo -> laranja, sobe ~1,5 m.</summary>
        public static ParticleSystem Fogo(Transform pai, string nome, float taxa, bool mundo) =>
            Novo(pai, nome, new Color(1f, 0.85f, 0.3f), new Color(1f, 0.36f, 0.1f), taxa,
                new Vector2(0.45f, 0.85f), new Vector2(1.2f, 2.6f), new Vector2(0.25f, 0.8f), mundo);
    }

    // ================================================================================== A CASCA

    /// <summary>
    /// DESENHA os kits (GDD §3/§4): junta os `Runner.Visuais` de todos os pawns da arena e mantem UM objeto por
    /// EfeitoVisual enquanto ele vive (chave = a propria instancia: nada pula de alvo quando a lista encolhe), com
    /// pool por tipo. Tipo sem desenho nao quebra (fiacao defensiva). Tambem desenha o AVISO da suprema no chao
    /// (Bus.KitTelegraph, GDD §4.3: "se mata rapido, avisa antes") — um anel que se ENCHE ate' o efeito sair.
    /// Opacidade/escala seguem LeituraDosKits.EscalaPorRestante. Zero luz dinamica: o projeto ilumina com COR.
    /// </summary>
    public sealed class VisualDosKits : MonoBehaviour
    {
        const int PontosFio = 12;
        /// <summary>O fio corre na cintura: a logica mede nos pes, o olho le' no corpo.</summary>
        const float AlturaFio = 1f;
        /// <summary>Muralha BAIXA de proposito: "bloqueia visao rasante", nao a visao inteira (Godot).</summary>
        const float AlturaNucleo = 1.1f;
        /// <summary>O revelado se le' de longe: e' informacao para o time.</summary>
        const float AlturaFeixe = 14f;
        /// <summary>Centro da cupula do Tear acima dos pes da dona (a borda no chao sai do corte da esfera).</summary>
        const float AlturaTear = 1.2f;
        /// <summary>VFX em passos de 12 fps (look de anime, SPELLBREAK.md §2.2), nunca lerp continuo.</summary>
        const float PassoAnime = 1f / 12f;
        static readonly float[] Flicker = { 1f, 0.8f, 0.95f, 0.75f, 0.9f };
        static readonly float[] Cintila = { 0.1f, 0.17f, 0.12f, 0.2f, 0.14f };
        /// <summary>O braco de chama "pulsa no ritmo do coracao dela" (ficha 01-pyra): tum-TUM e pausa, em passos de anime.
        /// 8 passos = 0,67 s, 90 bpm de quem esta' lutando. Multiplica a TAXA: a chama engrossa e afina no compasso.</summary>
        static readonly float[] Batimento = { 1.5f, 0.85f, 1.3f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f };
        /// <summary>m — altura do vulto do eco (a Veu e' "silhueta pequena").</summary>
        const float AlturaEco = 1.7f, LarguraEco = 0.8f;

        static readonly Color CorFogo = new Color32(0xFF, 0x5A, 0x2A, 255);
        static readonly Color CorRaio = new Color32(0xF5, 0xD9, 0x0A, 255);   // Tessa e' Raio: paleta GDD §10
        static readonly Color CorEspectro = new Color(0.55f, 0.75f, 1f, 0.35f);
        static readonly Color CorTear = new Color(0.9f, 0.8f, 0.2f, 1f);
        static readonly Color CorAviso = new Color(1f, 0.18f, 0.3f, 1f);
        static int _idCor;
        static Material _mNucleo, _mBrasa, _mFantasma, _mTear, _mFeixe, _mAviso;
        static Texture2D _silhueta;

        Partida _partida;
        MaterialPropertyBlock _mpb;
        int _quadro;
        readonly Dictionary<EfeitoVisual, Item> _vivos = new Dictionary<EfeitoVisual, Item>();
        readonly Dictionary<string, Stack<Item>> _livres = new Dictionary<string, Stack<Item>>();
        readonly List<EfeitoVisual> _mortos = new List<EfeitoVisual>();
        readonly List<Aviso> _avisos = new List<Aviso>();
        readonly Dictionary<Pawn, Braco> _bracos = new Dictionary<Pawn, Braco>();

        sealed class Item
        {
            public string Tipo;
            public GameObject Go;
            public Renderer R, R2;
            public ParticleSystem Ps, Ps2;
            public LineRenderer Linha;
            public Vector3[] Pontos;
            public int Quadro;
        }

        sealed class Aviso
        {
            public Vector3 Pos;
            public float Raio, Duracao, Restante;
            public Item It;
        }

        /// <summary>Os ossos do braco de chama de UMA Pyra + os dois efeitos dela (chave estavel no pool: o mesmo Item quadro
        /// a quadro). Restante 1 fixo: ninguem desconta, quem liga e desliga e' o estado do kit.</summary>
        sealed class Braco
        {
            public Transform Ombro, Mao;
            public readonly EfeitoVisual Livre = new EfeitoVisual("braco_livre", Vector3.zero, Vector3.zero, 0f, 1f);
            public readonly EfeitoVisual Fornalha = new EfeitoVisual("fornalha", Vector3.zero, Vector3.zero, 0f, 1f);
        }

        /// <summary>Nasce sob a arena (morre com ela). `partida` null = Partida.Atual a cada frame.</summary>
        public static VisualDosKits Criar(Transform arena, Partida partida)
        {
            var go = new GameObject("VisualDosKits");
            if (arena != null) go.transform.SetParent(arena, false);
            var v = go.AddComponent<VisualDosKits>();
            v._partida = partida;
            return v;
        }

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (_idCor == 0) _idCor = Shader.PropertyToID("_BaseColor");
            if (_mNucleo == null)
            {
                _mNucleo = MaterialVfx.CorDeVertice(MaterialVfx.Mistura.Alfa);
                _mBrasa = MaterialVfx.Solido(CorFogo, MaterialVfx.Mistura.Alfa);
                float k = MaterialVfx.BrilhoHdr;
                _mFantasma = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, new Color(k, k, k, 1f), MaterialVfx.Mistura.Aditivo, true, Silhueta());
                _mTear = MaterialVfx.Solido(CorTear, MaterialVfx.Mistura.Aditivo, true);   // dos dois lados: a camera fica DENTRO da bolha
                _mFeixe = MaterialVfx.Solido(CorRaio, MaterialVfx.Mistura.Aditivo, true);
                _mAviso = MaterialVfx.Solido(CorAviso, MaterialVfx.Mistura.Alfa, true);
            }
        }

        void OnEnable() { Bus.KitTelegraph += AoAvisar; }
        void OnDisable() { Bus.KitTelegraph -= AoAvisar; }

        void LateUpdate()
        {
            _quadro++;
            Partida p = _partida ?? Partida.Atual;
            if (p != null)
                for (int i = 0; i < p.Arena.Count; i++)
                {
                    Pawn dono = p.Arena[i] as Pawn;
                    if (dono == null || dono.Runner == null) continue;
                    IReadOnlyList<EfeitoVisual> vs = dono.Runner.Visuais;
                    for (int j = 0; j < vs.Count; j++) Desenhar(vs[j], dono);
                    if (dono.Runner.Impl is Pyra) BracoDaPyra(dono);
                }
            Recolher();
            Avisos(Time.deltaTime);
        }

        // ------------------------------------------------------------------ efeitos

        void Desenhar(EfeitoVisual v, Pawn dono)
        {
            if (v == null || v.Tipo == null || !(v.Restante > 0f)) return;
            Item it;
            if (!_vivos.TryGetValue(v, out it)) { it = Pegar(v.Tipo); _vivos[v] = it; }
            it.Quadro = _quadro;
            float e = LeituraDosKits.EscalaPorRestante(v.Restante, v.Duracao);
            switch (v.Tipo)
            {
                case "muralha": Muralha(it, v, e); break;
                case "poca": Poca(it, v, e); break;
                case "fio": Fio(it, v, e); break;
                case "eco": Eco(it, v, e); break;
                case "tear": Tear(it, v, e, dono); break;
                case "aceso": Aceso(it, v, e); break;
                case "revelado": Revelado(it, v, e); break;
                case "braco_livre": Chama(it, v, 110f, 36f); break;
                case "fornalha": Chama(it, v, 18f, 0f); break;
            }
        }

        /// <summary>
        /// O BRACO DE CHAMA da Pyra (ficha 01-pyra: o ESQUERDO, chama viva presa na manopla). No Braco Livre ele pega fogo
        /// inteiro; com a passiva acesa (o +vel de pisar fogo, o unico buff > 1 no kit dela) ou na telegrafia da suprema (o
        /// "brilho crescente" da ficha) solta um fio de brasas. Nao e' EfeitoVisual do kit (a regra nao muda): e' LEITURA do
        /// estado, desenhada pelo mesmo pool — o quadro em que o estado some, o Recolher devolve e a chama morre sozinha.
        /// Morta nao arde: o relogio do kit para na morte e o estado ficaria.
        /// </summary>
        void BracoDaPyra(Pawn dono)
        {
            if (!dono.Viva) return;
            bool livre = dono.Runner.EstadoAtivo(Pyra.BRACO_LIVRE);
            if (!livre && !(Efeitos.De(dono).StatusMult > 1f || dono.Runner.Telegrafia > 0f)) return;
            Braco b;
            if (!_bracos.TryGetValue(dono, out b)) { b = AcharBraco(dono.Visual); _bracos[dono] = b; }
            if (b.Ombro == null || b.Mao == null) return;   // sem osso (ou o modelo morreu): nao desenha, nao quebra
            EfeitoVisual v = livre ? b.Livre : b.Fornalha;
            v.Pos = b.Ombro.position;
            v.Pos2 = b.Mao.position;
            Desenhar(v, dono);
        }

        /// <summary>Ombro e mao ESQUERDOS por nome, 1x por pawn: Meshy/Mixamo ("LeftArm", "LeftHand" — o 01-pyra.fbx tem os
        /// dois) ou o procedural ("BracoE" e a "Mao" dele). Nao achou fica no cache sem osso: a busca aloca, nao se repete.</summary>
        static Braco AcharBraco(Component mago)
        {
            var b = new Braco();
            if (mago == null) return b;
            foreach (Transform t in mago.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant();
                if (n.EndsWith("leftarm") || n == "bracoe") b.Ombro = t;
                else if (n.EndsWith("lefthand")) b.Mao = t;
            }
            if (b.Mao == null && b.Ombro != null) b.Mao = b.Ombro.Find("Mao");
            return b;
        }

        /// <summary>Chama no braco (Pos = ombro, Pos2 = punho): a caixa corre do ombro a um palmo alem do punho (o punho
        /// fechado tambem arde) e a raiz segue o osso a cada quadro. Direcao ALEATORIA + gravidade negativa: o osso gira, o
        /// fogo sobe para o ceu. MUNDO: o braco varre e a chama fica no rastro. A taxa bate no compasso do coracao.</summary>
        void Chama(Item it, EfeitoVisual v, float taxa, float taxaFaisca)
        {
            Vector3 d = (v.Pos2 - v.Pos) * 1.2f;
            Transform t = it.Go.transform;
            t.position = v.Pos + d * 0.5f;
            if (d.sqrMagnitude > 1e-6f) t.rotation = Quaternion.FromToRotation(Vector3.forward, d);
            float bate = Batimento[Passo() % Batimento.Length];
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.scale = new Vector3(0.16f, d.magnitude, 0.16f);   // y da caixa = -z da raiz = o braco (o Novo gira -90 em X)
            ParticleSystem.EmissionModule em = it.Ps.emission;
            em.rateOverTimeMultiplier = taxa * bate;
            if (it.Ps2 == null) return;
            ParticleSystem.ShapeModule sh2 = it.Ps2.shape;
            sh2.scale = sh.scale;
            ParticleSystem.EmissionModule em2 = it.Ps2.emission;
            em2.rateOverTimeMultiplier = taxaFaisca * bate;
        }

        /// <summary>Segmento de chamas: cunha emissiva no chao + chamas subindo (espaco LOCAL: o vento empurra e a chama vai junto).</summary>
        void Muralha(Item it, EfeitoVisual v, float e)
        {
            Vector3 a = NoChao(v.Pos), b = NoChao(v.Pos2);
            Vector3 d = b - a;
            d.y = 0f;
            float comp = Mathf.Max(d.magnitude, v.Raio * 2f);
            Transform t = it.Go.transform;
            t.position = (a + b) * 0.5f;
            if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d, Vector3.up);
            float f = Flicker[Passo() % Flicker.Length];
            it.R.transform.localScale = new Vector3(v.Raio * 2f, AlturaNucleo * Mathf.Lerp(0.5f, 1f, e) * f, comp);
            Pintar(it.R, Color.white, 0.6f * e);   // o nucleo e' o calor no pe' da muralha; quem desenha o fogo sao as chamas
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.scale = new Vector3(v.Raio * 2f, comp, 0.1f);
            ParticleSystem.EmissionModule em = it.Ps.emission;
            em.rateOverTimeMultiplier = comp * 22f * e;
            ParticleSystem.ShapeModule sh2 = it.Ps2.shape;
            sh2.scale = sh.scale;
            ParticleSystem.EmissionModule em2 = it.Ps2.emission;
            em2.rateOverTimeMultiplier = comp * 7f * e;
        }

        /// <summary>Poca do dash: brasa no chao piscando em passos + chamas BAIXAS em cima. Escala so' no disco: a raiz
        /// escalada levaria a caixa das chamas junto.</summary>
        void Poca(Item it, EfeitoVisual v, float e)
        {
            float r = Mathf.Max(v.Raio, 0.1f);
            it.Go.transform.SetPositionAndRotation(NoChao(v.Pos) + Vector3.up * 0.07f, Inclinacao(v.Pos));
            it.R.transform.localScale = Vector3.one * r;
            Pintar(it.R, CorFogo, 0.55f * e * Flicker[Passo() % Flicker.Length]);   // o disco e' a AREA; quem desenha o fogo sao as chamas
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.scale = new Vector3(r * 1.4f, r * 1.4f, 0.1f);   // quadrado dentro do disco (canto a 0,99 r)
            ParticleSystem.EmissionModule em = it.Ps.emission;
            em.rateOverTimeMultiplier = r * 12f * e;
        }

        /// <summary>Fio do Tear: linha de raio com tremor (a espessura treme junto) entre as duas ANCORAS — o ponto fraco:
        /// acendem em HDR e pulsam no proprio compasso — com faiscas espirrando ao longo do fio.</summary>
        void Fio(Item it, EfeitoVisual v, float e)
        {
            Vector3 a = NoChao(v.Pos) + Vector3.up * AlturaFio, b = NoChao(v.Pos2) + Vector3.up * AlturaFio;
            int passo = Passo();
            it.Pontos = LeituraDosKits.PontosDoFio(a, b, PontosFio, passo * PassoAnime, it.Pontos);
            it.Linha.positionCount = PontosFio;
            it.Linha.SetPositions(it.Pontos);
            it.Linha.widthMultiplier = 0.2f * Flicker[passo % Flicker.Length];
            Color c = CorRaio;
            c.a = e;
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            // a raiz corre no fio (so' a caixa das faiscas le' isso: a linha e as ancoras sao de mundo)
            Vector3 d = b - a;
            Transform t = it.Go.transform;
            t.position = (a + b) * 0.5f;
            if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d, Vector3.up);
            float pulso = Flicker[(passo + 2) % Flicker.Length];   // defasado do fio: a ancora bate no compasso dela
            it.R.transform.position = a;
            it.R2.transform.position = b;
            it.R.transform.localScale = it.R2.transform.localScale = Vector3.one * (0.42f * pulso);
            Pintar(it.R, Hdr(CorRaio, 2f * pulso), e);
            Pintar(it.R2, Hdr(CorRaio, 2f * pulso), e);
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.scale = new Vector3(0.12f, d.magnitude, 0.12f);
            ParticleSystem.EmissionModule em = it.Ps.emission;
            em.rateOverTimeMultiplier = d.magnitude * 16f * e;
        }

        /// <summary>Eco da Veu: "Veu de vidro" parada na ENTRADA — o vulto (so' o contorno aceso) tremeluz no meio da neblina
        /// espectral que sobe devagar; some encolhendo para dentro dela (pe' fixo no chao, largura e altura juntas).</summary>
        void Eco(Item it, EfeitoVisual v, float e)
        {
            Vector3 pe = NoChao(v.Pos);
            it.Go.transform.position = pe + Vector3.up * 0.9f;
            it.Linha.SetPosition(0, pe);
            it.Linha.SetPosition(1, pe + Vector3.up * (AlturaEco * e));
            it.Linha.widthMultiplier = LarguraEco * e;
            Color c = CorEspectro;
            c.a = 0.8f * e * Flicker[Passo() % Flicker.Length];   // KNOB por foto: 0,8 com o HDR 1,8 do material
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            ParticleSystem.EmissionModule em = it.Ps.emission;
            em.rateOverTimeMultiplier = 22f * e;
            ParticleSystem.EmissionModule em2 = it.Ps2.emission;
            em2.rateOverTimeMultiplier = 10f * e;
        }

        /// <summary>
        /// Tear-Mae: cupula cintilante do raio de absorcao + BORDA acesa onde a esfera corta o chao + TRAMA de faiscas na
        /// casca, girando com a raiz em passos (o tear "gira tecendo"). Acompanha a DONA, como a logica (Tear.Tick mede
        /// em _dona.Pos). ponytail: a manta de luz que VOA para os aliados (ficha) espera um evento de absorcao na casca.
        /// </summary>
        void Tear(Item it, EfeitoVisual v, float e, Pawn dono)
        {
            Vector3 p = dono != null ? dono.Pos : v.Pos;
            int passo = Passo();
            Transform t = it.Go.transform;
            t.SetPositionAndRotation(p + Vector3.up * AlturaTear, Quaternion.Euler(0f, passo % 72 * 5f, 0f));   // 60 graus/s
            it.R.transform.localScale = Vector3.one * (v.Raio * 2f);
            Pintar(it.R, CorTear, Cintila[passo % Cintila.Length] * e);
            Vector3 chao = NoChao(p);
            float h = p.y + AlturaTear - chao.y;
            it.R2.transform.SetPositionAndRotation(chao + Vector3.up * 0.06f, Inclinacao(p));   // mundo: nao gira com a raiz
            it.R2.transform.localScale = Vector3.one * Mathf.Sqrt(Mathf.Max(v.Raio * v.Raio - h * h, 0.01f));
            Pintar(it.R2, Hdr(CorRaio, 1.6f), e * Flicker[passo % Flicker.Length]);
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.radius = v.Raio;
            ParticleSystem.EmissionModule em = it.Ps.emission;
            em.rateOverTimeMultiplier = 180f * e;
        }

        /// <summary>Aceso: fogo preso ao alvo, em espaco de MUNDO (o rastro fica — e' informacao para os outros).</summary>
        void Aceso(Item it, EfeitoVisual v, float e)
        {
            it.Go.transform.position = PosDoAlvo(v) + Vector3.up * 0.9f;
            ParticleSystem.EmissionModule em = it.Ps.emission;
            em.rateOverTimeMultiplier = 22f * e;
        }

        /// <summary>Revelado: feixe de luz sobre o alvo tocado pelo fio (largo nele, some no ceu) + motas subindo por dentro.
        /// O brilho (e, flicker) vai pelo _BaseColor da linha: o degrade do feixe fica fixo, sem Gradient novo por quadro.</summary>
        void Revelado(Item it, EfeitoVisual v, float e)
        {
            Vector3 p = PosDoAlvo(v);
            it.Go.transform.position = p;
            it.Linha.SetPosition(0, p);
            it.Linha.SetPosition(1, p + Vector3.up * AlturaFeixe);
            Pintar(it.Linha, Hdr(Color.white, MaterialVfx.BrilhoHdr), 0.8f * e * Flicker[Passo() % Flicker.Length]);
            ParticleSystem.EmissionModule em = it.Ps.emission;
            em.rateOverTimeMultiplier = 16f * e;
        }

        /// <summary>Quem sumiu da lista volta ao pool. Particula PARA de emitir (a chama morre sozinha, sem piscar).</summary>
        void Recolher()
        {
            _mortos.Clear();
            foreach (KeyValuePair<EfeitoVisual, Item> kv in _vivos) if (kv.Value.Quadro != _quadro) _mortos.Add(kv.Key);
            for (int i = 0; i < _mortos.Count; i++)
            {
                Devolver(_vivos[_mortos[i]]);
                _vivos.Remove(_mortos[i]);
            }
        }

        // ------------------------------------------------------------------ aviso da suprema

        void AoAvisar(string slug, string tipo, float duracao, Vector3 pos)
        {
            if (!(duracao > 0f)) return;
            _avisos.Add(new Aviso { Pos = pos, Raio = LeituraDosKits.RaioDoAviso(slug), Duracao = duracao, Restante = duracao, It = Pegar("aviso") });
        }

        /// <summary>Anel fixo do tamanho da area + miolo que ENCHE com o tempo: cheio = o efeito sai (contagem sem numero).</summary>
        void Avisos(float dt)
        {
            for (int i = _avisos.Count - 1; i >= 0; i--)
            {
                Aviso a = _avisos[i];
                a.Restante -= dt;
                if (a.Restante <= 0f) { Devolver(a.It); _avisos.RemoveAt(i); continue; }
                float prog = 1f - a.Restante / a.Duracao;
                Transform t = a.It.Go.transform;
                t.SetPositionAndRotation(NoChao(a.Pos) + Vector3.up * 0.12f, Inclinacao(a.Pos));
                a.It.R.transform.localScale = Vector3.one * a.Raio;
                a.It.R2.transform.localScale = Vector3.one * Mathf.Max(a.Raio * prog, 0.05f);
                Pintar(a.It.R, CorAviso, (Passo() & 1) == 0 ? 0.95f : 0.6f);   // a borda PULSA: le-se de canto de olho
                Pintar(a.It.R2, CorAviso, 0.35f);
            }
        }

        // ------------------------------------------------------------------ pool

        Item Pegar(string tipo)
        {
            Stack<Item> s;
            Item it = _livres.TryGetValue(tipo, out s) && s.Count > 0 ? s.Pop() : Novo(tipo);
            Mostrar(it, true);
            return it;
        }

        void Devolver(Item it)
        {
            Mostrar(it, false);
            Stack<Item> s;
            if (!_livres.TryGetValue(it.Tipo, out s)) { s = new Stack<Item>(); _livres[it.Tipo] = s; }
            s.Push(it);
        }

        static void Mostrar(Item it, bool on)
        {
            if (it.R != null) it.R.enabled = on;
            if (it.R2 != null) it.R2.enabled = on;
            if (it.Linha != null) it.Linha.enabled = on;
            if (it.Ps == null) return;
            if (on) it.Ps.Play(true);
            else it.Ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Item Novo(string tipo)
        {
            var it = new Item { Tipo = tipo, Go = new GameObject("Vfx_" + tipo) };
            Transform raiz = it.Go.transform;
            raiz.SetParent(transform, false);
            switch (tipo)
            {
                case "muralha":
                    it.R = Peca(raiz, MalhaVfx.Cunha(), _mNucleo);
                    // PAREDE DE FOGO, nao barra de lava (foto 17 de 12/09): chama grande e densa (o teto padrao de 48
                    // particulas deixava 8 m de muralha com meia duzia de pontos) + faisca miuda que sobe alto e espirra.
                    // As faiscas sao FILHAS das chamas: o Play(true)/Stop(true) do pool liga as duas.
                    it.Ps = ParticulaVfx.Novo(raiz, "Chamas", new Color(1f, 0.85f, 0.3f), new Color(1f, 0.32f, 0.08f), 30f,
                        new Vector2(0.5f, 1f), new Vector2(1.4f, 3f), new Vector2(0.6f, 1.3f), false, 0.08f, 260);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Faiscas", new Color(1f, 0.92f, 0.55f), new Color(1f, 0.5f, 0.15f), 10f,
                        new Vector2(0.8f, 1.6f), new Vector2(2f, 4.5f), new Vector2(0.06f, 0.14f), false, 0.35f, 120);
                    it.Ps2.transform.localRotation = Quaternion.identity;   // ja' herda o -90 das chamas
                    break;
                case "poca":
                    it.R = Peca(raiz, MalhaVfx.Disco(), _mBrasa);
                    // chamas BAIXAS (o preset de fogo, mais lento e com teto menor: o dash solta 3-4 pocas encavaladas).
                    // MUNDO: a poca e' parada, e a chama que sobra ao recolher morre onde estava.
                    it.Ps = ParticulaVfx.Fogo(raiz, "Chamas", 20f, true);
                    ParticleSystem.MainModule mp = it.Ps.main;
                    mp.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
                    mp.maxParticles = 40;
                    break;
                case "fio":
                    it.Linha = Linha(it.Go, 0.2f);
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mFeixe);
                    it.R2 = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mFeixe);
                    // faisca ELETRICA: miuda, rapida, curta e para todo lado (aleatorio 1), na caixa esticada no fio.
                    // MUNDO: o fio nao anda.
                    it.Ps = ParticulaVfx.Novo(raiz, "Faiscas", new Color(1f, 1f, 0.85f), CorRaio, 60f,
                        new Vector2(0.12f, 0.3f), new Vector2(1.5f, 4f), new Vector2(0.06f, 0.14f), true, 1f, 60);
                    break;
                case "eco":
                    // a capsula lia PILULA azul (foto 19 de 12/09): no lugar, um CARTAO virado para a camera (linha de VISTA:
                    // nao tem face, nao le' caixa de angulo nenhum) com a SILHUETA de capuz e manto so' no contorno — borda
                    // acesa, miolo vazado: o fresnel que o Unlit nao tem, pintado na textura. A NEBLINA e' a leitura principal:
                    // vaza do corpo (abre ao subir) + almas miudas (filhas: o Play(true)/Stop(true) do pool liga as duas).
                    // Alfa baixo: aditivo grande estoura o bloom. MUNDO: o eco e' parado. Caixa = o corpo em pe' (0,2-1,6 m).
                    it.Linha = Linha(it.Go, LarguraEco);
                    it.Linha.sharedMaterial = _mFantasma;
                    it.Linha.numCapVertices = 0;   // a ponta redonda esticaria o texel da borda alem do pe' e do capuz
                    it.Ps = ParticulaVfx.Novo(raiz, "Neblina", new Color(0.91f, 0.9f, 0.94f, 0.25f), new Color(0.55f, 0.75f, 1f, 0.18f), 22f,
                        new Vector2(1f, 1.8f), new Vector2(0.15f, 0.5f), new Vector2(0.5f, 1.1f), true, 0.25f, 56);
                    ParticleSystem.SizeOverLifetimeModule sze = it.Ps.sizeOverLifetime;
                    sze.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
                    ParticleSystem.ShapeModule she = it.Ps.shape;
                    she.scale = new Vector3(0.6f, 0.6f, 1.4f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Almas", new Color(0.9f, 0.95f, 1f), new Color(0.55f, 0.75f, 1f), 10f,
                        new Vector2(0.8f, 1.6f), new Vector2(0.3f, 0.9f), new Vector2(0.05f, 0.12f), true, 0.4f, 24);
                    it.Ps2.transform.localRotation = Quaternion.identity;   // ja' herda o -90 da neblina
                    ParticleSystem.ShapeModule sha = it.Ps2.shape;
                    sha.scale = she.scale;
                    break;
                case "tear":
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mTear);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);   // a BORDA, no chao
                    // TRAMA: faisca so' na CASCA da cupula (hemisferio, espessura 0), escorrendo para dentro (absorve).
                    // LOCAL: a cupula anda com a dona e gira em passos — a trama vai junto.
                    it.Ps = ParticulaVfx.Novo(raiz, "Trama", new Color(1f, 0.97f, 0.7f), CorRaio, 180f,
                        new Vector2(0.5f, 1.1f), new Vector2(-0.6f, -0.1f), new Vector2(0.1f, 0.26f), false, 0f, 200);
                    ParticleSystem.ShapeModule sht = it.Ps.shape;
                    sht.shapeType = ParticleSystemShapeType.Hemisphere;   // +Z local = cima (o Novo gira -90 em X)
                    sht.radiusThickness = 0f;
                    break;
                case "aceso":
                    it.Ps = ParticulaVfx.Fogo(raiz, "Aceso", 22f, true);
                    ParticleSystem.ShapeModule sh = it.Ps.shape;
                    sh.scale = new Vector3(0.5f, 0.5f, 1.2f);
                    break;
                case "revelado":
                    // FEIXE: faixa aditiva virada para a camera, larga no alvo e sumindo no ceu (o cilindro chapado lia
                    // placeholder) + motas subindo por dentro. LOCAL: o alvo corre e a coluna vai junto, reta.
                    it.Linha = Linha(it.Go, 0.7f);
                    it.Linha.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
                    var gf = new Gradient();
                    gf.SetKeys(new[] { new GradientColorKey(CorRaio, 0f), new GradientColorKey(CorRaio, 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
                    it.Linha.colorGradient = gf;
                    it.Ps = ParticulaVfx.Novo(raiz, "Motas", new Color(1f, 1f, 0.8f), CorRaio, 16f,
                        new Vector2(0.8f, 1.8f), new Vector2(3f, 7f), new Vector2(0.08f, 0.2f), false, 0.04f, 48);
                    ParticleSystem.ShapeModule shr = it.Ps.shape;
                    shr.scale = new Vector3(0.3f, 0.3f, 0.4f);
                    break;
                case "aviso":
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mAviso);
                    it.R2 = Peca(raiz, MalhaVfx.Disco(), _mAviso);
                    it.R.transform.localPosition = new Vector3(0f, 0.02f, 0f);   // a borda por cima do miolo
                    break;
                case "braco_livre":
                    // CHAMA TOTAL (a manopla destravou): lambidas curtas e densas do tamanho do braco (as da muralha sao de
                    // parede) + faisca miuda espirrando para todo lado. Filhas: o Play(true)/Stop(true) do pool liga as duas.
                    it.Ps = ParticulaVfx.Novo(raiz, "Chamas", new Color(1f, 0.85f, 0.3f), new Color(1f, 0.32f, 0.08f), 110f,
                        new Vector2(0.25f, 0.5f), new Vector2(0.1f, 0.4f), new Vector2(0.14f, 0.34f), true, 1f, 90);
                    Subir(it.Ps, 0.7f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Faiscas", new Color(1f, 0.92f, 0.55f), new Color(1f, 0.5f, 0.15f), 36f,
                        new Vector2(0.35f, 0.8f), new Vector2(0.8f, 2.2f), new Vector2(0.03f, 0.07f), true, 1f, 50);
                    it.Ps2.transform.localRotation = Quaternion.identity;   // ja' herda o -90 das chamas
                    Subir(it.Ps2, 0.3f);
                    break;
                case "fornalha":
                    // FIO DE BRASAS (passiva acesa): poucas, miudas e lentas — ela corre e o fio fica para tras.
                    it.Ps = ParticulaVfx.Novo(raiz, "Brasas", new Color(1f, 0.8f, 0.35f), CorFogo, 18f,
                        new Vector2(0.5f, 1f), new Vector2(0.05f, 0.3f), new Vector2(0.04f, 0.09f), true, 1f, 24);
                    Subir(it.Ps, 0.15f);
                    break;
            }
            return it;
        }

        /// <summary>Empuxo para CIMA do mundo (gravidade negativa, fracao de g): a chama sobe qualquer que seja o giro do osso.</summary>
        static void Subir(ParticleSystem ps, float empuxo)
        {
            ParticleSystem.MainModule m = ps.main;
            m.gravityModifier = -empuxo;
        }

        /// <summary>
        /// O vulto do eco (64 no comprimento x 32 na largura, gerado 1x, zero arquivo): capuz em gota, ombro, manto abrindo na
        /// barra e o pe' DISSOLVENDO na neblina. Alfa so' no CONTORNO (miolo 10%, borda cheia, brilho curto para fora): borda
        /// acesa e meio vazado sao o que le' "fantasma" e nao "plastico". u = do pe' ao capuz (o comprimento da linha,
        /// textureMode Stretch), v = a largura. ponytail: silhueta generica de capuz — o vulto DELA pede o BakeMesh do modelo.
        /// </summary>
        static Texture2D Silhueta()
        {
            if (_silhueta != null) return _silhueta;
            const int nu = 64, nv = 32;
            var t = new Texture2D(nu, nv, TextureFormat.RGBA32, false) { name = "SilhuetaEco", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[nu * nv];
            for (int x = 0; x < nu; x++)
            {
                float h = (x + 0.5f) / nu, w = Mathf.Max(MeiaLargura(h), 1e-3f), pe = Mathf.Clamp01(h / 0.3f);
                for (int y = 0; y < nv; y++)
                {
                    float d = Mathf.Abs((y + 0.5f) / nv * 2f - 1f) / w;   // 0 no eixo, 1 no contorno
                    float a = d <= 1f ? 0.1f + 0.9f * d * d * d * d : Mathf.Clamp01(1f - (d - 1f) * 5f);
                    px[y * nu + x] = new Color32(255, 255, 255, (byte)(a * pe * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return _silhueta = t;
        }

        /// <summary>Meia largura do vulto (1 = a borda do cartao) na altura h (0 = pe', 1 = bico do capuz).</summary>
        static float MeiaLargura(float h)
        {
            if (h < 0.45f) return Mathf.Lerp(0.55f, 0.46f, h / 0.45f);            // manto: abre na barra
            if (h < 0.68f) return Mathf.Lerp(0.46f, 0.6f, (h - 0.45f) / 0.23f);   // tronco ate' os ombros
            if (h < 0.76f) return Mathf.Lerp(0.6f, 0.34f, (h - 0.68f) / 0.08f);   // ombro caindo no pescoco
            float c = (h - 0.76f) / 0.24f;                                         // o CAPUZ: gota com bico no alto
            return 0.4f * Mathf.Sqrt(Mathf.Max(0f, 1f - c * c)) * (1f - 0.35f * c);
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

        /// <summary>Linha aditiva de MUNDO virada para a camera, com a faixa macia (MaterialVfx.DeLinha).</summary>
        static LineRenderer Linha(GameObject go, float largura)
        {
            var l = go.AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.widthMultiplier = largura;
            l.numCapVertices = 2;
            l.alignment = LineAlignment.View;
            l.sharedMaterial = MaterialVfx.DeLinha();
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            return l;
        }

        // ------------------------------------------------------------------ utilidades

        /// <summary>Cor por objeto sem material novo (MaterialPropertyBlock): so' o alfa muda de efeito para efeito.</summary>
        void Pintar(Renderer r, Color c, float alfa)
        {
            if (r == null) return;
            c.a *= alfa;
            _mpb.SetColor(_idCor, c);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Cor acima de 1 (acende no bloom) com o alfa intacto: Color * k multiplicaria o alfa e o aditivo sairia k^2.</summary>
        static Color Hdr(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        static int Passo() => (int)(Time.time / PassoAnime);

        static Vector3 NoChao(Vector3 p) => new Vector3(p.x, Ilha.AlturaDoChao(p.x, p.z), p.z);

        /// <summary>Disco no chao deita na encosta (normal do relevo no centro); sem ilha, reto.
        /// ponytail: disco rigido nao abraca relevo acidentado — decal projetado se o aviso de 6 m enterrar de verdade.</summary>
        static Quaternion Inclinacao(Vector3 p)
        {
            Relevo r = Ilha.Atual != null ? Ilha.Atual.Relevo : null;
            return r != null ? Quaternion.FromToRotation(Vector3.up, r.Normal(p.x, p.z)) : Quaternion.identity;
        }

        /// <summary>O alvo vivo na cena; destruido (fim de arena) ou ausente cai no ponto gravado.</summary>
        static Vector3 PosDoAlvo(EfeitoVisual v)
        {
            if (v.Alvo == null) return v.Pos;
            UnityEngine.Object o = v.Alvo as UnityEngine.Object;
            if (!ReferenceEquals(o, null) && o == null) return v.Pos;
            return v.Alvo.Pos;
        }
    }
}
