using System.Collections.Generic;
using UnityEngine;

namespace Arkana.World
{
    /// <summary>
    /// A BASE DE ROCHA da ilha flutuante (Diretor, 25/09: sem mar — a ilha paira no ceu). Um "cone" irregular pendurado
    /// sob a costa: o anel de cima segue a borda REAL da terra (o ultimo chao acima de 0,5 m em cada direcao a partir do
    /// centro), com um beiral curto, e afina ate' uma ponta a ~700 m abaixo, com saliencias de rocha. Sem colisor: quem
    /// passa da borda cai no vazio (RelevoMestre.VazioY). Material de rocha do proprio Terrain, dos dois lados.
    /// Vetavel: profundidade, forma e cor.
    /// </summary>
    public sealed partial class IlhaMestre
    {
        const int BaseAngulos = 256, BaseAneis = 16;
        const float BaseFundo = 700f;

        void BaseRochosa()
        {
            // borda: em cada direcao, o raio da terra mais distante (passo de 8 m ate' 3,5 km)
            var borda = new float[BaseAngulos];
            for (int a = 0; a < BaseAngulos; a++)
            {
                float th = 2f * Mathf.PI * a / BaseAngulos, cx = Mathf.Cos(th), cy = Mathf.Sin(th), ultimo = 200f;
                for (float r = 200f; r < 3500f; r += 8f)
                    if (Altura(cx * r, cy * r) >= 0.5f) ultimo = r;
                borda[a] = ultimo + 3f;
            }
            // suaviza o anel (um cabo fino de 1 amostra virava espinho)
            var suave = new float[BaseAngulos];
            for (int a = 0; a < BaseAngulos; a++)
                suave[a] = (borda[(a + BaseAngulos - 1) % BaseAngulos] + 2f * borda[a] + borda[(a + 1) % BaseAngulos]) * 0.25f;

            var v = new List<Vector3>((BaseAneis + 1) * (BaseAngulos + 1) + 1);
            var uv = new List<Vector2>(v.Capacity);
            var f = new List<int>(BaseAneis * BaseAngulos * 6 + BaseAngulos * 3);
            float perimetro = 0f;
            for (int a = 0; a < BaseAngulos; a++) perimetro += suave[a] * 2f * Mathf.PI / BaseAngulos;
            for (int k = 0; k <= BaseAneis; k++)
            {
                float t = (float)k / BaseAneis;
                // perfil: beiral (sai 2 %) nos primeiros 3 %, depois afina com curva — cone de "raiz" de ilha
                float forma = t < 0.03f ? 1f + 0.02f * (t / 0.03f) : 1.02f * Mathf.Pow(1f - (t - 0.03f) / 0.97f, 1.35f);
                float z = t < 0.03f ? 0.3f - 6f * (t / 0.03f) : -6f - (BaseFundo - 6f) * Mathf.Pow((t - 0.03f) / 0.97f, 0.85f);
                for (int a = 0; a <= BaseAngulos; a++)
                {
                    int ai = a % BaseAngulos;
                    float th = 2f * Mathf.PI * a / BaseAngulos;
                    // rocha irregular: ondas de 3 escalas, mais fortes no meio da altura (nada de ruido na borda de cima)
                    float ruido = t < 0.03f ? 0f : Mathf.Sin(t * Mathf.PI) *
                        (0.09f * Mathf.Sin(5f * th + 9f * t + 1.3f) + 0.06f * Mathf.Sin(13f * th + 17f * t) + 0.035f * Mathf.Sin(31f * th - 23f * t + 4f));
                    float r = suave[ai] * forma * (1f + ruido);
                    v.Add(U(Mathf.Cos(th) * r, Mathf.Sin(th) * r, z + (t < 0.03f ? 0f : ruido * 60f)));
                    uv.Add(new Vector2(perimetro * a / BaseAngulos / 25f, z / 25f));
                }
            }
            int W = BaseAngulos + 1;
            for (int k = 0; k < BaseAneis; k++)
            for (int a = 0; a < BaseAngulos; a++)
            {
                int a0 = k * W + a, a1 = a0 + 1, b0 = a0 + W, b1 = b0 + 1;
                f.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
            }
            var m = new Mesh { name = "base_rochosa", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(f, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();

            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(s) { name = "mestre_base_rochosa" };
            Texture2D cor = Resources.Load<Texture2D>("terreno-rocha-cor"), nor = Resources.Load<Texture2D>("terreno-rocha-normal");
            if (cor != null) mat.SetTexture("_BaseMap", cor);
            if (nor != null) { mat.SetTexture("_BumpMap", nor); mat.EnableKeyword("_NORMALMAP"); }
            mat.SetColor("_BaseColor", new Color(0.62f, 0.56f, 0.50f));   // a rocha do Terrain puxada para o terroso da falesia
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Cull", 0f);   // dos dois lados: por uma enseada ve'-se a parede de dentro, nao o ceu
            var go = new GameObject("Base_Rochosa") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
