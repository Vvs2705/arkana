using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arkana.World
{
    /// <summary>
    /// A casca da ilha: monta a malha do terreno a partir de Relevo (132x132 quads, cor de vertice
    /// por bioma x AO, normais do campo de altura), o MeshCollider (colisao exata da malha, 34.848
    /// faces como o Godot media), a agua, a atmosfera (nevoa, ceu, ambiente) e o cenario: vegetacao,
    /// grama, ruinas e o kit esculpido. Um draw call para o chao.
    /// Fiacao defensiva: quem nao achar `Atual` usa um plano (AlturaDoChao devolve 0); shader Arkana
    /// ausente cai, calado, na cadeia de antes (MaterialPadrao).
    /// </summary>
    public sealed class Ilha : MonoBehaviour
    {
        public const int Quads = 132;

        /// <summary>
        /// NEVOA LINEAR (profundidade de vista). A guarda do Godot cobra o EFEITO, nao o botao: a 200 m (a
        /// altura da queda do Godot) a nevoa come menos de 20% do quadro — senao a escolha de pouso vira
        /// borrao caramelo; aqui o castelo voa a 320 m, entao o comeco foi mais longe. O FIM cabe antes do
        /// far da camera (1.000 m): la' o mar ja' e' cor de nevoa e encosta no ceu, que abaixo do
        /// horizonte e' a mesma cor. O combate perto fica limpo.
        /// </summary>
        public const float NevoaInicio = 180f, NevoaFim = 900f;
        public static readonly Color CorNevoa = new Color(0.93f, 0.76f, 0.55f);   // fog_light_color do Godot
        /// <summary>Far padrao da camera do jogo: a nevoa tem que fechar antes dele.</summary>
        public const float FarDaCamera = 1000f;

        /// <summary>
        /// O mar e' uma grade de 6 km que SEGUE a camera em passos de um quad: a borda fica a 3 km, alem do
        /// far em qualquer angulo e de qualquer altura. Fixo em 2,2 km, o castelo a 320 m via o fim do plano
        /// (foto do coordenador). Quad de 100 m: a onda de vertice so' existe a menos de 160 m da camera, e o
        /// que se ve' do mar e' o padrao do fragmento — o vertice nao precisa ser mais fino.
        /// </summary>
        const float LadoMar = 6000f;
        const int QuadsMar = 60;
        const float PassoMar = LadoMar / QuadsMar;

        public static Ilha Atual { get; private set; }

        [SerializeField] float escala = 2f;   // 2.0 = 600 m de lado, 264 m de raio de terra
        [SerializeField] int seed = 7;
        [SerializeField] bool comVegetacao = true;

        public Relevo Relevo { get; private set; }
        public Vegetacao Vegetacao { get; private set; }
        public Grama Grama { get; private set; }
        public Ruinas Ruinas { get; private set; }
        public KitCenario Kit { get; private set; }
        public Mesh MalhaDoTerreno { get; private set; }

        Transform mar;

        /// <summary>Altura do chao em (x, z); sem ilha na cena, plano em y = 0.</summary>
        public static float AlturaDoChao(float x, float z)
        {
            return Atual != null && Atual.Relevo != null ? Atual.Relevo.Altura(x, z) : 0f;
        }

        /// <summary>Superficie d'agua em (x, z) ou Relevo.Seco; sem ilha, tudo e' seco.</summary>
        public static float SuperficieDaAgua(float x, float z)
        {
            return Atual != null && Atual.Relevo != null ? Atual.Relevo.SuperficieDaAgua(x, z) : Relevo.Seco;
        }

        /// <summary>Fracao de nevoa numa superficie a `d` metros de profundidade de vista (a formula LINEAR do Unity).</summary>
        public static float NevoaEm(float d) => Mathf.Clamp01((d - NevoaInicio) / (NevoaFim - NevoaInicio));

        /// <summary>
        /// A NEVOA ACOMPANHA A ALTURA DA CAMERA. Licao G5 do Godot, reaprendida na foto de 11/09: com a nevoa fixa do
        /// chao, o mapa visto do castelo e da queda virava "um borrao caramelo" — e e' la' em cima que se escolhe onde
        /// pousar. No chao vale NevoaInicio/NevoaFim (perto limpo, longe com ar); a cada metro de altura o comeco anda
        /// 1 m e o fim 1,2 m. O fim nunca passa de 95% do far clip: o que o far corta ja' tem de estar na cor do ceu.
        /// </summary>
        public static void DistanciasDaNevoa(float alturaCamera, float farClip, out float inicio, out float fim)
        {
            float h = alturaCamera > 0f ? alturaCamera : 0f;   // NaN falha a comparacao e vira 0
            inicio = NevoaInicio + h;
            fim = NevoaFim + h * 1.2f;
            if (farClip > 0f) fim = Mathf.Min(fim, farClip * 0.95f);
            inicio = Mathf.Min(inicio, fim * 0.6f);
        }

        void Awake()
        {
            Montar();
        }

        void OnEnable() { RenderPipelineManager.beginCameraRendering += NevoaDaCamera; }

        void OnDisable() { RenderPipelineManager.beginCameraRendering -= NevoaDaCamera; }

        /// <summary>Por CAMERA que desenha (a do jogador, a do menu, a da foto aerea): cada uma com a nevoa da sua altura.</summary>
        static void NevoaDaCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam == null || !RenderSettings.fog) return;
            float inicio, fim;
            DistanciasDaNevoa(cam.transform.position.y, cam.farClipPlane, out inicio, out fim);
            RenderSettings.fogStartDistance = inicio;
            RenderSettings.fogEndDistance = fim;
        }

        void OnDestroy()
        {
            if (Atual == this) Atual = null;
        }

        public void Montar()
        {
            Atual = this;
            Relevo = new Relevo(escala, seed);
            Transform velho = transform.Find("Gerado");
            if (velho != null) Destroy(velho.gameObject);
            var gen = new GameObject("Gerado");
            gen.transform.SetParent(transform, false);
            MontarTerreno(gen.transform, MaterialTerreno());
            MontarAgua(gen.transform);
            Atmosfera();
            Pos = MontarPos(gen.transform);
            if (comVegetacao)
            {
                Vegetacao = Filho<Vegetacao>(gen.transform, "Vegetacao");
                Vegetacao.Montar(Relevo, MaterialToon());
                Grama = Filho<Grama>(gen.transform, "Grama");
                Grama.Montar(Relevo);
                Ruinas = Filho<Ruinas>(gen.transform, "Ruinas");
                Ruinas.Montar(Relevo, MaterialPedra());
                // o kit vem por ultimo: planta pelos POIs e foge do que as ruinas ja' ocupam
                Kit = Filho<KitCenario>(gen.transform, "KitCenario");
                Kit.Montar(Relevo, Ruinas.Pegadas);
            }
        }

        static T Filho<T>(Transform pai, string nome) where T : Component
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            return go.AddComponent<T>();
        }

        void LateUpdate()
        {
            if (mar == null) return;
            Camera c = Camera.main;
            if (c == null) return;
            Vector3 o = c.transform.position;
            // passo de um quad: a onda do vertice (posicao do mundo) nao escorrega sob a malha
            mar.position = new Vector3(Mathf.Round(o.x / PassoMar) * PassoMar, Relevo.AguaY, Mathf.Round(o.z / PassoMar) * PassoMar);
        }

        // ------------------------------------------------------------------ terreno

        void MontarTerreno(Transform parent, Material mat)
        {
            int n1 = Quads + 1;
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            var verts = new Vector3[n1 * n1];
            var norms = new Vector3[n1 * n1];
            var cols = new Color[n1 * n1];
            float lado = Relevo.Lado;
            for (int iz = 0; iz < n1; iz++)
            {
                for (int ix = 0; ix < n1; ix++)
                {
                    float x = ((float)ix / Quads - 0.5f) * lado;
                    float z = ((float)iz / Quads - 0.5f) * lado;
                    float h = Relevo.Altura(x, z);
                    Vector3 nrm = Relevo.Normal(x, z);
                    int i = iz * n1 + ix;
                    verts[i] = new Vector3(x, h, z);
                    norms[i] = nrm;
                    // A paleta e' sRGB; a cor de vertice chega no shader SEM conversao. Em espaco
                    // linear, gravar o hex cru deixa tudo claro e lavado (o defeito mais caro que o
                    // mundo Godot teve). O AO multiplica em LINEAR. O alfa e' o peso de rocha.
                    Color c = Relevo.Cor(x, z, h);
                    float a = c.a;
                    if (linear) c = c.linear;
                    float ao = Relevo.Ao(x, z, h, nrm.y);
                    cols[i] = new Color(c.r * ao, c.g * ao, c.b * ao, a);
                }
            }
            var tris = new int[Quads * Quads * 6];
            int t = 0;
            for (int iz = 0; iz < Quads; iz++)
            {
                for (int ix = 0; ix < Quads; ix++)
                {
                    int a = iz * n1 + ix, b = a + 1, c = a + n1, d = c + 1;
                    // frente para +Y (sentido horario visto de cima, no Unity). GradeDoChao repete ESTA divisao.
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = c; tris[t++] = d; tris[t++] = b;
                }
            }
            var mesh = new Mesh();
            mesh.name = "Terreno";
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.colors = cols;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            MalhaDoTerreno = mesh;

            var go = new GameObject("Terreno");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            // colisao exata da malha: corpo estatico, 34.848 faces (ok no mobile — medido no Godot)
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        // ------------------------------------------------------------------ agua

        void MontarAgua(Transform parent)
        {
            // Com o Arkana/Agua, a cor sai do material e o vertice carrega DADO (COLOR.r = o quanto e' raso).
            // Sem ele, a cadeia de antes: cor chapada no vertice.
            bool nova = ShaderArkana("ArkanaAgua", "Arkana/Agua") != null;
            // Mar: fundo de quadro, contraste baixo (ocupa meia tela e nao pode competir com o combate).
            Material mMar = nova ? MaterialAgua("Mar", 0x2f8fe0, 0x0d3576, 0.16f, 0f, 0.5f, 0.5f) : MaterialPadrao();
            // Lago e alagado sao POI: celula menor, contraste cheio, azul mais fundo (o claro virava lilas).
            Material mLago = nova ? MaterialAgua("Lago", 0x35b0f2, 0x0f4f9e, 0.07f, 1f, 1.15f, 1f) : MaterialPadrao();
            Material mBrejo = nova ? MaterialAgua("Alagado", 0x4ba589, 0x1e6b62, 0.04f, 1f, 1.4f, 0.85f) : MaterialPadrao();

            mar = Agua(parent, mMar, MalhaMar(nova ? new Color(0f, 0f, 0f, 1f) : CorCrua(0x2f8fe0)),
                new Vector3(0f, Relevo.AguaY, 0f), "Mar").transform;
            // RAIOS dos discos: >= a maior margem medida (faltar disco deixa buraco seco; sobrar some sob o terreno)
            Agua(parent, mLago, Disco(Relevo.Lago, Relevo.LagoY, Relevo.LagoDiscoR, 0.08f, 2.5f, nova ? (Color?)null : CorCrua(0x35b0f2)),
                new Vector3(Relevo.Lago.x, Relevo.LagoY, Relevo.Lago.y), "Lago");
            // o brejo e' raso por natureza: a referencia de fundo e' 0,6 m, nao 2,5
            Agua(parent, mBrejo, Disco(Relevo.Alagado, Relevo.AlagadoY, Relevo.AlagadoDiscoR, 0.12f, 0.6f, nova ? (Color?)null : CorCrua(0x4ba589)),
                new Vector3(Relevo.Alagado.x, Relevo.AlagadoY, Relevo.Alagado.y), "Alagado");

            // BRUMA do alagado: duas camadas em alturas e escalas diferentes (uma so' le' como adesivo)
            Material nevoa = MaterialNevoa();
            if (nevoa != null)
            {
                Agua(parent, nevoa, Anel(Relevo.AlagadoR + 3f, 0.14f), new Vector3(Relevo.Alagado.x, 1.05f, Relevo.Alagado.y), "Bruma");
                GameObject b2 = Agua(parent, nevoa, Anel(Relevo.AlagadoR - 2f, 0.18f),
                    new Vector3(Relevo.Alagado.x + 1.5f, 1.75f, Relevo.Alagado.y - 1f), "Bruma2");
                b2.transform.localRotation = Quaternion.Euler(0f, 109f, 0f);
            }
        }

        Color CorCrua(int hex)
        {
            Color c = Relevo.Hex(hex);
            return Linear() ? c.linear : c;
        }

        static GameObject Agua(Transform parent, Material mat, Mesh mesh, Vector3 pos, string nome)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;   // agua nao colide nem sombreia: o nado pergunta a Relevo
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>Grade plana de 6 km (61x61 vertices): o vertice precisa existir para a onda existir.</summary>
        static Mesh MalhaMar(Color cor)
        {
            int n1 = QuadsMar + 1;
            var v = new Vector3[n1 * n1];
            var c = new Color[n1 * n1];
            var n = new Vector3[n1 * n1];
            for (int iz = 0; iz < n1; iz++)
                for (int ix = 0; ix < n1; ix++)
                {
                    int i = iz * n1 + ix;
                    v[i] = new Vector3(((float)ix / QuadsMar - 0.5f) * LadoMar, 0f, ((float)iz / QuadsMar - 0.5f) * LadoMar);
                    c[i] = cor;
                    n[i] = Vector3.up;
                }
            var t = new int[QuadsMar * QuadsMar * 6];
            int k = 0;
            for (int iz = 0; iz < QuadsMar; iz++)
                for (int ix = 0; ix < QuadsMar; ix++)
                {
                    int a = iz * n1 + ix, b = a + 1, cc = a + n1, d = cc + 1;
                    t[k++] = a; t[k++] = cc; t[k++] = b;
                    t[k++] = cc; t[k++] = d; t[k++] = b;
                }
            var m = new Mesh { name = "Mar" };
            m.vertices = v;
            m.normals = n;
            m.colors = c;
            m.triangles = t;
            // o mar anda com a camera: bounds folgados para o frustum nunca cortar a borda que ainda aparece
            m.bounds = new Bounds(Vector3.zero, new Vector3(LadoMar, 20f, LadoMar));
            return m;
        }

        /// <summary>
        /// Disco d'agua em aneis finos. COLOR.r = o quanto o fundo esta' RASO ali, pela profundidade de
        /// verdade (cota da lamina - terreno) sobre `fundoRef`: 0 no fundo, 1 na margem — e acima de 1
        /// (terreno por cima da lamina) satura em 1. A espuma e o raso claro nascem na MARGEM REAL, nao
        /// no contorno do disco: foi o disco de borda dura que o coordenador fotografou do alto.
        /// O raio ainda e' deformado por angulo (contorno perfeito e' assinatura de script).
        /// `corFallback` != null: sem o shader novo, grava a cor chapada de antes.
        /// </summary>
        Mesh Disco(Vector2 centro, float lamina, float raio, float wobble, float fundoRef, Color? corFallback)
        {
            const int segs = 48, aneis = 12;
            var v = new List<Vector3>();
            var c = new List<Color>();
            var t = new List<int>();
            v.Add(Vector3.zero);
            c.Add(corFallback ?? Raso(centro, lamina, fundoRef));
            for (int k = 1; k <= aneis; k++)
            {
                float f = (float)k / aneis;
                for (int i = 0; i < segs; i++)
                {
                    float a = Mathf.PI * 2f * i / segs;
                    float w = 1f + wobble * (Mathf.Sin(a * 3f + 0.7f) * 0.6f + Mathf.Sin(a * 7f + 2.1f) * 0.4f);
                    var p = new Vector3(Mathf.Cos(a) * raio * w * f, 0f, Mathf.Sin(a) * raio * w * f);
                    v.Add(p);
                    c.Add(corFallback ?? Raso(centro + new Vector2(p.x, p.z), lamina, fundoRef));
                }
            }
            for (int i = 0; i < segs; i++)
            {
                int i1 = (i + 1) % segs;
                t.Add(0); t.Add(1 + i1); t.Add(1 + i);   // leque do centro, frente para +Y
            }
            for (int k = 1; k < aneis; k++)
            {
                int dentro = 1 + (k - 1) * segs, fora = 1 + k * segs;
                for (int i = 0; i < segs; i++)
                {
                    int i1 = (i + 1) % segs;
                    t.Add(dentro + i); t.Add(dentro + i1); t.Add(fora + i);
                    t.Add(dentro + i1); t.Add(fora + i1); t.Add(fora + i);
                }
            }
            var m = new Mesh { name = "Disco" };
            m.SetVertices(v);
            m.SetColors(c);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        Color Raso(Vector2 p, float lamina, float fundoRef)
        {
            float prof = lamina - Relevo.Altura(p.x, p.y);
            return new Color(1f - Mathf.Clamp01(prof / fundoRef), 0f, 0f, 1f);
        }

        /// <summary>Disco da bruma: COLOR.r = fracao do raio (o alfa morre na borda).</summary>
        static Mesh Anel(float raio, float wobble)
        {
            const int segs = 32;
            float[] aneis = { 0.3f, 0.55f, 0.8f, 1f };
            var v = new List<Vector3> { Vector3.zero };
            var c = new List<Color> { new Color(0f, 0f, 0f, 1f) };
            var t = new List<int>();
            for (int k = 0; k < aneis.Length; k++)
                for (int i = 0; i < segs; i++)
                {
                    float a = Mathf.PI * 2f * i / segs;
                    float w = 1f + wobble * (Mathf.Sin(a * 3f + 0.7f) * 0.6f + Mathf.Sin(a * 7f + 2.1f) * 0.4f);
                    v.Add(new Vector3(Mathf.Cos(a) * raio * w * aneis[k], 0f, Mathf.Sin(a) * raio * w * aneis[k]));
                    c.Add(new Color(aneis[k], 0f, 0f, 1f));
                }
            for (int i = 0; i < segs; i++)
            {
                int i1 = (i + 1) % segs;
                t.Add(0); t.Add(1 + i1); t.Add(1 + i);
            }
            for (int k = 1; k < aneis.Length; k++)
            {
                int dentro = 1 + (k - 1) * segs, fora = 1 + k * segs;
                for (int i = 0; i < segs; i++)
                {
                    int i1 = (i + 1) % segs;
                    t.Add(dentro + i); t.Add(dentro + i1); t.Add(fora + i);
                    t.Add(dentro + i1); t.Add(fora + i1); t.Add(fora + i);
                }
            }
            var m = new Mesh { name = "Bruma" };
            m.SetVertices(v);
            m.SetColors(c);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static bool Linear() => QualitySettings.activeColorSpace == ColorSpace.Linear;

        // ------------------------------------------------------------------ atmosfera

        /// <summary>
        /// Nevoa, ambiente e ceu do entardecer (Island.tscn do Godot). Ambiente em 3 cores: o toon le'
        /// unity_Ambient* direto, e o Trilight nao depende de assar probe nenhum.
        /// KNOB: as tres cores do ambiente sao gosto — calibrar por foto, nunca por teste.
        /// </summary>
        static void Atmosfera()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = CorNevoa;
            RenderSettings.fogStartDistance = NevoaInicio;
            RenderSettings.fogEndDistance = NevoaFim;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.47f, 0.62f);      // ceu frio: a sombra ganha cor, nao preto
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.48f, 0.42f);  // horizonte dourado do entardecer
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.27f, 0.24f);   // rebote do chao
            Material ceu = MaterialCeu();
            if (ceu != null) RenderSettings.skybox = ceu;
        }

        /// <summary>O volume global do pos (tonemapping, bloom, cor, vinheta). So' aparece na camera com LigarPos.</summary>
        public Volume Pos { get; private set; }

        /// <summary>
        /// POS-PROCESSAMENTO (passo D; o Diretor liberou em 12/09: "o FPS vai rodar legal"). Tonemapping NEUTRO segura o
        /// HDR do sol sem virar a paleta; bloom leve so' no que passa de 1 (disco do sol, brilho duro); cor um pouco mais
        /// viva e vinheta de cinema. Perfil criado em codigo: o UniversalRenderPipelineGlobalSettings deste projeto NAO
        /// remove variantes de pos nao usadas, entao isto vale no APK. KNOB: todos os numeros, por foto.
        /// </summary>
        static Volume MontarPos(Transform pai)
        {
            var go = new GameObject("Pos");
            go.transform.SetParent(pai, false);
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            p.name = "ArkanaPos";
            p.Add<Tonemapping>(true).mode.value = TonemappingMode.Neutral;
            Bloom b = p.Add<Bloom>(true);
            b.threshold.value = 1.1f;
            b.intensity.value = 0.35f;
            b.scatter.value = 0.6f;
            ColorAdjustments c = p.Add<ColorAdjustments>(true);
            c.postExposure.value = 0.15f;   // o neutro escurece o meio-tom: devolve um pouco
            c.contrast.value = 10f;
            c.saturation.value = 10f;
            Vignette vi = p.Add<Vignette>(true);
            vi.intensity.value = 0.2f;
            vi.smoothness.value = 0.45f;
            v.sharedProfile = p;
            return v;
        }

        /// <summary>A camera so' desenha o pos com o renderPostProcessing ligado (o padrao do URP e' desligado).</summary>
        public static void LigarPos(Camera c)
        {
            if (c == null) return;
            var d = c.GetUniversalAdditionalCameraData();
            d.renderPostProcessing = true;
        }

        public static bool PosLigado(Camera c) => c != null && c.TryGetComponent(out UniversalAdditionalCameraData d) && d.renderPostProcessing;

        // ------------------------------------------------------------------ materiais

        static Material material, matTerreno, matToon, matPedra, matCeu, matNevoa;
        static readonly Dictionary<string, Material> matAgua = new Dictionary<string, Material>();

        /// <summary>Shader proprio de Resources/ (entra no build por morar la'); Shader.Find so' de reserva. Null = cadeia antiga.</summary>
        static Shader ShaderArkana(string arquivo, string nome)
        {
            Shader s = Resources.Load<Shader>(arquivo);
            if (s == null) s = Shader.Find(nome);
            return s != null && s.isSupported ? s : null;
        }

        /// <summary>Toon padrao: arvores, moitas, rochas da vegetacao.</summary>
        public static Material MaterialToon()
        {
            if (matToon != null) return matToon;
            Shader s = ShaderArkana("ArkanaToon", "Arkana/Toon");
            if (s == null) return MaterialPadrao();
            matToon = new Material(s) { name = "ArkanaToon" };
            return matToon;
        }

        /// <summary>
        /// Chao: mais bandas e mais macias (superficie enorme e continua), mancha grande + oitava de perto e
        /// as duas texturas de detalhe a 0,55 — a textura QUEBRA a superficie, nao pinta o chao: a cor
        /// continua vindo do vertice, e e' ela que garante praia, lama, musgo e cume legiveis de 200 m.
        /// </summary>
        public static Material MaterialTerreno()
        {
            if (matTerreno != null) return matTerreno;
            Shader s = ShaderArkana("ArkanaToon", "Arkana/Toon");
            if (s == null) return MaterialPadrao();
            var m = new Material(s) { name = "ArkanaTerreno" };
            m.SetFloat("_Faixas", 4f);
            m.SetFloat("_FaixaMacia", 0.22f);
            m.SetFloat("_Mancha", 0.17f);
            m.SetFloat("_EscalaMancha", 0.19f);
            m.SetFloat("_Grao", 0.085f);
            m.SetFloat("_EscalaGrao", 0.65f);
            m.SetFloat("_Rim", 0.06f);   // chao nao tem silhueta
            Texture2D chao = Resources.Load<Texture2D>("detalhe-chao");
            Texture2D rocha = Resources.Load<Texture2D>("detalhe-rocha");
            if (chao != null && rocha != null)
            {
                m.SetTexture("_TexChao", chao);
                m.SetTexture("_TexRocha", rocha);
                m.SetFloat("_ForcaTextura", 0.55f);
                m.SetFloat("_EscalaTextura", 0.22f);   // 1/m: ladrilho de ~4,5 m
            }
            matTerreno = m;
            return m;
        }

        /// <summary>Pedra/ruina: sheen duro fraco no topo (a unica coisa que vende material duro num toon).</summary>
        public static Material MaterialPedra()
        {
            if (matPedra != null) return matPedra;
            Shader s = ShaderArkana("ArkanaToon", "Arkana/Toon");
            if (s == null) return MaterialPadrao();
            var m = new Material(s) { name = "ArkanaPedra" };
            m.SetFloat("_Brilho", 0.11f);
            m.SetFloat("_BrilhoDuro", 24f);
            m.SetFloat("_Mancha", 0.09f);
            m.SetFloat("_EscalaMancha", 1.6f);
            matPedra = m;
            return m;
        }

        static Material MaterialAgua(string nome, int rasa, int funda, float onda, float espuma, float banda, float contraste)
        {
            if (matAgua.TryGetValue(nome, out Material m) && m != null) return m;
            Shader s = ShaderArkana("ArkanaAgua", "Arkana/Agua");
            if (s == null) return MaterialPadrao();
            m = new Material(s) { name = "ArkanaAgua" + nome };
            m.SetColor("_Rasa", Relevo.Hex(rasa));   // propriedade Color: o Unity converte pro espaco ativo
            m.SetColor("_Funda", Relevo.Hex(funda));
            m.SetFloat("_Onda", onda);
            m.SetFloat("_Espuma", espuma);
            m.SetFloat("_EscalaBanda", banda);
            m.SetFloat("_Contraste", contraste);
            matAgua[nome] = m;
            return m;
        }

        static Material MaterialNevoa()
        {
            if (matNevoa != null) return matNevoa;
            Shader s = ShaderArkana("ArkanaNevoa", "Arkana/Nevoa");
            if (s == null) return null;   // bruma sem o shader dela nao existe: melhor nada que um disco opaco
            matNevoa = new Material(s) { name = "ArkanaNevoa" };
            return matNevoa;
        }

        static Material MaterialCeu()
        {
            if (matCeu != null) return matCeu;
            Shader s = ShaderArkana("ArkanaCeu", "Arkana/Ceu");
            if (s == null) return null;   // sem ceu proprio, fica o skybox da cena
            matCeu = new Material(s) { name = "ArkanaCeu" };
            return matCeu;
        }

        /// <summary>
        /// A cadeia ANTIGA (fallback): cor de vertice, sem textura. Os shaders URP Lit/Simple Lit IGNORAM
        /// cor de vertice; os de particula a leem (albedo x COLOR) e recebem luz e sombra.
        /// </summary>
        public static Material MaterialPadrao()
        {
            if (material != null) return material;
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Standard");
            material = new Material(s);
            material.name = "ArkanaMundo";
            return material;
        }
    }
}
