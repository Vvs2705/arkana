using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arkana.World
{
    /// <summary>
    /// A casca da ilha: monta a malha do terreno a partir de Relevo (132x132 quads, cor de vertice
    /// por bioma x AO, normais do campo de altura), o MeshCollider (colisao exata da malha, 34.848
    /// faces como o Godot media), os planos d'agua e a vegetacao. Um draw call para o chao.
    /// Fiacao defensiva: quem nao achar `Atual` usa um plano (AlturaDoChao devolve 0).
    /// </summary>
    public sealed class Ilha : MonoBehaviour
    {
        public const int Quads = 132;

        public static Ilha Atual { get; private set; }

        [SerializeField] float escala = 2f;   // 2.0 = 600 m de lado, 264 m de raio de terra
        [SerializeField] int seed = 7;
        [SerializeField] bool comVegetacao = true;

        public Relevo Relevo { get; private set; }
        public Vegetacao Vegetacao { get; private set; }
        public Mesh MalhaDoTerreno { get; private set; }

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

        void Awake()
        {
            Montar();
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
            Material mat = MaterialPadrao();
            MontarTerreno(gen.transform, mat);
            MontarAgua(gen.transform, mat);
            if (comVegetacao)
            {
                var vgo = new GameObject("Vegetacao");
                vgo.transform.SetParent(gen.transform, false);
                Vegetacao = vgo.AddComponent<Vegetacao>();
                Vegetacao.Montar(Relevo, mat);
            }
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
                    // frente para +Y (sentido horario visto de cima, no Unity)
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

        void MontarAgua(Transform parent, Material mat)
        {
            // O mar e' fundo de quadro: um quad de 2200 m (borda a 1.100 m, alem de onde a nevoa fecha).
            // TODO agua: shader de agua (water.gdshader) — hoje e' cor chapada opaca, sem onda nem espuma.
            var mar = new MalhaProc.Construtor();
            mar.Caixa(new Vector3(0f, Relevo.AguaY - 0.5f, 0f), new Vector3(1100f, 0.5f, 1100f), Relevo.Hex(0x2f8fe0));
            Agua(parent, mat, mar.ParaMesh("Mar", Linear()), Vector3.zero, "Mar");
            Agua(parent, mat, Disco(Relevo.LagoDiscoR, Relevo.Hex(0x35b0f2), 0.08f),
                new Vector3(Relevo.Lago.x, Relevo.LagoY, Relevo.Lago.y), "Lago");
            Agua(parent, mat, Disco(Relevo.AlagadoDiscoR, Relevo.Hex(0x4ba589), 0.12f),
                new Vector3(Relevo.Alagado.x, Relevo.AlagadoY, Relevo.Alagado.y), "Alagado");
        }

        static void Agua(Transform parent, Material mat, Mesh mesh, Vector3 pos, string nome)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;   // agua nao colide nem sombreia: o nado pergunta a Relevo
        }

        /// <summary>Disco em leque com o raio deformado por angulo: contorno perfeito e' assinatura de script.</summary>
        static Mesh Disco(float raio, Color cor, float wobble)
        {
            var b = new MalhaProc.Construtor();
            const int segs = 32;
            for (int i = 0; i < segs; i++)
            {
                float a0 = Mathf.PI * 2f * i / segs, a1 = Mathf.PI * 2f * (i + 1) / segs;
                float w0 = 1f + wobble * (Mathf.Sin(a0 * 3f + 0.7f) * 0.6f + Mathf.Sin(a0 * 7f + 2.1f) * 0.4f);
                float w1 = 1f + wobble * (Mathf.Sin(a1 * 3f + 0.7f) * 0.6f + Mathf.Sin(a1 * 7f + 2.1f) * 0.4f);
                b.Tri(Vector3.zero,
                    new Vector3(Mathf.Cos(a0) * raio * w0, 0f, Mathf.Sin(a0) * raio * w0),
                    new Vector3(Mathf.Cos(a1) * raio * w1, 0f, Mathf.Sin(a1) * raio * w1), cor, Vector3.up);
            }
            return b.ParaMesh("Disco", Linear());
        }

        static bool Linear() => QualitySettings.activeColorSpace == ColorSpace.Linear;

        // ------------------------------------------------------------------ material

        static Material material;

        /// <summary>
        /// UM material para chao, agua, vegetacao e castelo: cor de vertice, sem textura.
        /// Os shaders URP Lit/Simple Lit IGNORAM cor de vertice; os de particula a leem
        /// (albedo x COLOR) e recebem luz e sombra — e' o que da' um draw call por malha
        /// com a cor de bioma gravada no vertice.
        /// TODO toon: cel-shading + detalhe de chao em espaco de mundo (toon.gdshader): bandas
        /// macias com wrap, sombra com cor fria, rim dos dois lados, e as duas texturas
        /// arte/cenario/texturas/detalhe-chao.png e detalhe-rocha.png misturadas pelo peso de
        /// rocha que ja' vai no alfa do vertice (tex_forca 0.55, tex_escala 0.22).
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
