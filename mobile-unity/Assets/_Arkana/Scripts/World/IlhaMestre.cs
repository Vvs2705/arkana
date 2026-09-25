using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arkana.World
{
    /// <summary>
    /// A ilha do Documento Mestre (4.800 x 4.400 m), montada a partir de Resources/ilha-mestre-altura.bytes e
    /// ilha-mestre.json — os dois gerados por arte/tools/ilha_mestre.py, que MEDE a ilha (doc §18) e falha fora da meta.
    /// 16 blocos de UnityEngine.Terrain nativo (o Unity simplifica o que esta' longe e poda por bloco), 5 camadas CC0 por altura e
    /// declive, mar/lago/rio no shader de agua do jogo, sol da tarde vindo do SO (doc §8.2), arvores do kit como
    /// instancias do UnityEngine.Terrain e as 12 regioes em BLOCKOUT (volumes + pecas que o jogo ja' tem).
    /// Tudo nasce sob "_gerado" com DontSave: a cena guarda so' este componente; abrir a cena reconstroi.
    /// Convencao do doc: +X leste, +Y norte, +Z cima -> Unity (x, z, y).
    /// </summary>
    [ExecuteAlways]
    public sealed partial class IlhaMestre : MonoBehaviour
    {
        const int Blocos = 4, Res = 513, Alfa = 512;

        [Serializable] public class Ponto { public float x, y, z, meia; }
        [Serializable] public class Lago { public float cx, cy, a, b, nivel; }
        [Serializable] public class Ilhota { public float x, y, r, h; }
        [Serializable] public class Ponte { public float y, x0, x1, z, largura; }
        [Serializable] public class Regiao { public string id, nome; public float x, y, ex, ey, z; }
        [Serializable] public class No { public string id; public float x, y, z; }
        [Serializable] public class Rota { public string id; public float[] x, y; }
        [Serializable]
        public class Dados
        {
            public int amostras;
            public float x0, x1, y0, y1, fundo, faixa;
            public Lago lago;
            public Ilhota[] ilhotas;
            public Ponto[] rio;
            public float[] canion;
            public Ponte ponte;
            public Regiao[] regioes;
            public No[] nos;
            public Rota[] rotas;
        }

        public Dados D { get; private set; }
        /// <summary>Subterraneo (doc §7, R10-R12) de arte/tools/subterraneo.py; nulo se o JSON nao existir.</summary>
        public Sub S { get; private set; }
        UnityEngine.Terrain[,] terrenos;
        ushort[] alt;
        int n;
        Transform raiz;
        readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
        System.Random rnd;

        void OnEnable()
        {
            if (transform.Find("_gerado") == null) Construir();
        }

        [ContextMenu("Reconstruir")]
        public void Construir()
        {
            Transform velho = transform.Find("_gerado");
            if (velho != null) DestroyImmediate(velho.gameObject);
            if (!Carregar()) return;
            rnd = new System.Random(24092026);
            var go = new GameObject("_gerado") { hideFlags = HideFlags.DontSave };
            raiz = go.transform;
            raiz.SetParent(transform, false);
            Terrenos();
            Agua();
            Atmosfera();
            Regioes();
            Subterraneo();
            Cameras();
        }

        bool Carregar()
        {
            var bin = Resources.Load<TextAsset>("ilha-mestre-altura");
            var js = Resources.Load<TextAsset>("ilha-mestre");
            if (bin == null || js == null) { Debug.LogWarning("IlhaMestre: rode arte/tools/ilha_mestre.py"); return false; }
            D = JsonUtility.FromJson<Dados>(js.text);
            n = D.amostras;
            byte[] b = bin.bytes;
            if (b.Length != n * n * 2) { Debug.LogError($"IlhaMestre: {b.Length} bytes, esperado {n * n * 2}"); return false; }
            alt = new ushort[n * n];
            Buffer.BlockCopy(b, 0, alt, 0, b.Length);
            var sub = Resources.Load<TextAsset>("ilha-mestre-subterraneo");
            S = sub != null ? JsonUtility.FromJson<Sub>(sub.text) : null;
            return true;
        }

        // ------------------------------------------------------------------ amostragem (coordenadas do DOC)

        public float Altura(float x, float y)
        {
            // fora do mapa e' mar aberto. Grudar na borda fazia os 4 cabos (a 7 m da borda, agua rasa) pintarem
            // uma faixa "rasa" do mar inteiro alinhada aos eixos — a cruz clara (medido 25/09).
            if (x < D.x0 || x > D.x1 || y < D.y0 || y > D.y1) return D.fundo;
            float fx = Mathf.Clamp((x - D.x0) / (D.x1 - D.x0) * (n - 1), 0, n - 1.001f);
            float fy = Mathf.Clamp((y - D.y0) / (D.y1 - D.y0) * (n - 1), 0, n - 1.001f);
            int i = (int)fx, j = (int)fy;
            float u = fx - i, v = fy - j;
            float a = H(i, j), b = H(i + 1, j), c = H(i, j + 1), d = H(i + 1, j + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        float H(int i, int j) => D.fundo + alt[j * n + i] / 65535f * D.faixa;

        float Declive(float x, float y)
        {
            const float e = 3f;
            float gx = (Altura(x + e, y) - Altura(x - e, y)) / (2 * e);
            float gy = (Altura(x, y + e) - Altura(x, y - e)) / (2 * e);
            return Mathf.Atan(Mathf.Sqrt(gx * gx + gy * gy)) * Mathf.Rad2Deg;
        }

        static Vector3 U(float x, float y, float z) => new Vector3(x, z, y);

        float LagoQ(float x, float y)
        {
            float dx = (x - D.lago.cx) / D.lago.a, dy = (y - D.lago.cy) / D.lago.b;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ------------------------------------------------------------------ terreno

        void Terrenos()
        {
            float bx = (D.x1 - D.x0) / Blocos, by = (D.y1 - D.y0) / Blocos;
            TerrainLayer[] camadas = Camadas();
            Shader st = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            Material mt = st != null ? new Material(st) { name = "mestre_terreno" } : null;
            var t = new UnityEngine.Terrain[Blocos, Blocos];
            var arvores = Arvores(out TreePrototype[] protos);
            for (int bj = 0; bj < Blocos; bj++)
            for (int bi = 0; bi < Blocos; bi++)
            {
                var td = new TerrainData { heightmapResolution = Res };
                td.size = new Vector3(bx, D.faixa, by);
                var h = new float[Res, Res];
                for (int j = 0; j < Res; j++)
                for (int i = 0; i < Res; i++)
                    h[j, i] = alt[(bj * (Res - 1) + j) * n + bi * (Res - 1) + i] / 65535f;
                td.SetHeights(0, 0, h);
                td.alphamapResolution = Alfa;
                td.terrainLayers = camadas;
                td.SetAlphamaps(0, 0, Splat(D.x0 + bi * bx, D.y0 + bj * by, bx, by, camadas.Length));
                if (protos != null)
                {
                    td.treePrototypes = protos;
                    td.SetTreeInstances(ArvoresDoBloco(arvores, D.x0 + bi * bx, D.y0 + bj * by, bx, by).ToArray(), true);
                }
                GameObject go = UnityEngine.Terrain.CreateTerrainGameObject(td);
                go.name = $"TER_{bi}_{bj}";
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(raiz, false);
                go.transform.position = new Vector3(D.x0 + bi * bx, D.fundo, D.y0 + bj * by);
                UnityEngine.Terrain ter = go.GetComponent<UnityEngine.Terrain>();
                if (mt != null) ter.materialTemplate = mt;
                ter.drawInstanced = true;
                ter.heightmapPixelError = 4f;
                ter.basemapDistance = 1200f;
                ter.treeDistance = 5000f;
                ter.treeBillboardDistance = 5000f;   // sem impostor: as pecas do kit nao usam o shader de arvore do Terrain
                t[bi, bj] = ter;
            }
            terrenos = t;
            for (int bj = 0; bj < Blocos; bj++)
            for (int bi = 0; bi < Blocos; bi++)
                t[bi, bj].SetNeighbors(bi > 0 ? t[bi - 1, bj] : null, bj < Blocos - 1 ? t[bi, bj + 1] : null,
                                       bi < Blocos - 1 ? t[bi + 1, bj] : null, bj > 0 ? t[bi, bj - 1] : null);
        }

        static TerrainLayer[] Camadas()
        {
            string[] nomes = { "grama", "mata", "rocha", "areia", "terra" };
            float[] ladrilho = { 14f, 10f, 22f, 16f, 10f };
            var l = new TerrainLayer[nomes.Length];
            for (int k = 0; k < nomes.Length; k++)
                l[k] = new TerrainLayer
                {
                    name = "mestre_" + nomes[k],
                    diffuseTexture = Resources.Load<Texture2D>($"terreno-{nomes[k]}-cor"),
                    normalMapTexture = Resources.Load<Texture2D>($"terreno-{nomes[k]}-normal"),
                    tileSize = new Vector2(ladrilho[k], ladrilho[k]),
                    normalScale = 0.8f,
                };
            return l;
        }

        /// <summary>Pesos: 0 grama, 1 chao de mata, 2 rocha (declive), 3 areia (beira do mar e do lago), 4 terra (pisos das regioes).</summary>
        float[,,] Splat(float ox, float oy, float sx, float sy, int k)
        {
            var a = new float[Alfa, Alfa, k];
            Regiao flo = Array.Find(D.regioes, r => r.id == "R02");
            var w = new float[5];
            for (int j = 0; j < Alfa; j++)
            for (int i = 0; i < Alfa; i++)
            {
                Pesos(ox + (i + 0.5f) / Alfa * sx, oy + (j + 0.5f) / Alfa * sy, flo, w);
                for (int c = 0; c < 5 && c < k; c++) a[j, i, c] = w[c];
            }
            return a;
        }

        // cor media MEDIDA de cada camada (sRGB; arte/tools/texturas_terreno.py): grama, mata, rocha, areia, terra
        static readonly Color[] MediaCamada =
        {
            new Color(91 / 255f, 127 / 255f, 57 / 255f), new Color(77 / 255f, 69 / 255f, 43 / 255f), new Color(111 / 255f, 105 / 255f, 97 / 255f),
            new Color(195 / 255f, 179 / 255f, 141 / 255f), new Color(117 / 255f, 91 / 255f, 59 / 255f),
        };

        /// <summary>A cor do chao em (x, y) do doc: a mistura das 5 camadas do Terrain (minimapa, fotos, exportacao).</summary>
        public Color CorDoChao(float x, float y)
        {
            var w = new float[5];
            Pesos(x, y, Array.Find(D.regioes, r => r.id == "R02"), w);
            Color c = Color.black;
            for (int k = 0; k < 5; k++) c += MediaCamada[k] * w[k];
            return c;
        }

        /// <summary>Pesos das camadas (grama, mata, rocha, areia, terra; somam 1) em (x, y).</summary>
        void Pesos(float x, float y, Regiao flo, float[] w)
        {
            {
                float h = Altura(x, y), dec = Declive(x, y);
                float rocha = Mathf.InverseLerp(24f, 34f, dec) + Mathf.InverseLerp(430f, 520f, h);
                float areia = Mathf.InverseLerp(5f, 2.5f, h) + (LagoQ(x, y) < 1.12f ? Mathf.InverseLerp(110f, 106f, h) : 0f);
                float fx = (x - flo.x) / (flo.ex * 0.55f), fy = (y - flo.y) / (flo.ey * 0.55f);
                float mata = Mathf.InverseLerp(1.05f, 0.75f, Mathf.Sqrt(fx * fx + fy * fy)) +
                             Mathf.Clamp01(Mathf.PerlinNoise(x * 0.004f + 11f, y * 0.004f + 7f) * 2.2f - 1.25f);
                float terra = 0f;
                foreach (Regiao r in D.regioes)
                {
                    if (r.id == "R01" || r.id == "R02" || r.id == "R11" || r.id == "R10") continue;
                    // borda irregular: raio modulado por ruido (circulo perfeito denunciava o blockout)
                    float rr = Mathf.Min(r.ex, r.ey) * 0.28f * (0.7f + 0.6f * Mathf.PerlinNoise(x * 0.012f + r.x * 0.001f, y * 0.012f));
                    float rx = (x - r.x) / rr, ry = (y - r.y) / rr;
                    terra = Mathf.Max(terra, Mathf.InverseLerp(1.1f, 0.6f, Mathf.Sqrt(rx * rx + ry * ry)));
                }
                terra *= Mathf.Lerp(0.55f, 1f, Mathf.PerlinNoise(x * 0.05f, y * 0.05f));
                terra = Mathf.Max(terra, Mathf.InverseLerp(7f, 4f, DistRota(x, y)) * Mathf.Lerp(0.75f, 1f, Mathf.PerlinNoise(x * 0.2f, y * 0.2f)));
                rocha = Mathf.Clamp01(rocha);
                areia = Mathf.Clamp01(areia) * (1 - rocha);
                mata = Mathf.Clamp01(mata) * (1 - rocha) * (1 - areia);
                terra = Mathf.Clamp01(terra) * (1 - rocha) * (1 - areia);
                float grama = Mathf.Max(0f, 1f - rocha - areia - mata - terra);
                float s = grama + mata + rocha + areia + terra;
                w[0] = grama / s; w[1] = mata / s; w[2] = rocha / s; w[3] = areia / s; w[4] = terra / s;
            }
        }

        // ------------------------------------------------------------------ vegetacao (instancias do Terrain)

        struct Arv { public float x, y, esc; public int p; }

        List<Arv> Arvores(out TreePrototype[] protos)
        {
            string[] rec = { "35-arvore-copa", "36-pinheiro", "37-moita", "51-tronco-musgo" };
            float[] altura = { 22f, 20f, 2.2f, 1.2f };   // alvo em metros (doc §9 R02: arvores comuns 12-35 m)
            var lista = new List<TreePrototype>();
            var escala = new List<float>();
            foreach (string r in rec)
            {
                GameObject m = Molde(r, out float hMolde);
                if (m == null) continue;
                lista.Add(new TreePrototype { prefab = m });
                escala.Add(altura[Array.IndexOf(rec, r)] / Mathf.Max(hMolde, 0.01f));
            }
            protos = lista.Count > 0 ? lista.ToArray() : null;
            var arv = new List<Arv>();
            if (protos == null) return arv;
            Regiao flo = Array.Find(D.regioes, r => r.id == "R02");
            for (int k = 0; k < 60000 && arv.Count < 9000; k++)
            {
                float x = (float)(D.x0 + rnd.NextDouble() * (D.x1 - D.x0)), y = (float)(D.y0 + rnd.NextDouble() * (D.y1 - D.y0));
                float h = Altura(x, y);
                if (h < 6f || h > 470f || Declive(x, y) > 28f || LagoQ(x, y) < 1.15f || PertoDoRio(x, y, 30f) || EmPiso(x, y) || DistRota(x, y) < 9f) continue;
                float fx = (x - flo.x) / (flo.ex * 0.55f), fy = (y - flo.y) / (flo.ey * 0.55f);
                float densidade = Mathf.Sqrt(fx * fx + fy * fy) < 1f ? 0.9f : Mathf.Clamp01(Mathf.PerlinNoise(x * 0.004f + 11f, y * 0.004f + 7f) * 2.2f - 1.1f);
                if (rnd.NextDouble() > densidade) continue;
                int p = h > 300f ? 1 : (rnd.NextDouble() < 0.25 ? 2 : (rnd.NextDouble() < 0.1 ? 3 : 0));
                p = Mathf.Min(p, protos.Length - 1);
                arv.Add(new Arv { x = x, y = y, p = p, esc = escala[p] * (float)(0.75 + rnd.NextDouble() * 0.6) });
            }
            return arv;
        }

        List<TreeInstance> ArvoresDoBloco(List<Arv> arv, float ox, float oy, float sx, float sy)
        {
            var l = new List<TreeInstance>();
            foreach (Arv a in arv)
            {
                if (a.x < ox || a.x >= ox + sx || a.y < oy || a.y >= oy + sy) continue;
                l.Add(new TreeInstance
                {
                    position = new Vector3((a.x - ox) / sx, 0f, (a.y - oy) / sy),
                    prototypeIndex = a.p, widthScale = a.esc, heightScale = a.esc,
                    rotation = (float)(rnd.NextDouble() * Math.PI * 2), color = Color.white, lightmapColor = Color.white,
                });
            }
            return l;
        }

        /// <summary>O GLB do kit vira UMA malha na raiz (o UnityEngine.Terrain so' desenha arvore com MeshRenderer na raiz).</summary>
        GameObject Molde(string recurso, out float altura)
        {
            altura = 0f;
            var src = Resources.Load<GameObject>(recurso);
            if (src == null) return null;
            GameObject inst = Instantiate(src);
            var ci = new List<CombineInstance>();
            var ms = new List<Material>();
            Matrix4x4 w2l = inst.transform.worldToLocalMatrix;
            foreach (MeshFilter mf in inst.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mf.sharedMesh == null || mr == null) continue;
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                {
                    ci.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = w2l * mf.transform.localToWorldMatrix });
                    ms.Add(mr.sharedMaterials[Mathf.Min(s, mr.sharedMaterials.Length - 1)]);
                }
            }
            DestroyImmediate(inst);
            if (ci.Count == 0) return null;
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, name = "molde_" + recurso };
            mesh.CombineMeshes(ci.ToArray(), false, true);
            mesh.RecalculateBounds();
            altura = mesh.bounds.max.y;
            var go = new GameObject("molde_" + recurso) { hideFlags = HideFlags.HideAndDontSave };
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = ms.ToArray();
            go.transform.SetParent(raiz, false);
            go.transform.position = new Vector3(0, -10000, 0);
            return go;
        }

        bool PertoDoRio(float x, float y, float margem)
        {
            for (int k = 0; k + 1 < D.rio.Length; k++)
            {
                Ponto a = D.rio[k], b = D.rio[k + 1];
                Vector2 ab = new Vector2(b.x - a.x, b.y - a.y), ap = new Vector2(x - a.x, y - a.y);
                float u = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
                if ((ap - ab * u).magnitude < Mathf.Lerp(a.meia, b.meia, u) + margem) return true;
            }
            return false;
        }

        /// <summary>Distancia (m) ate' a rota de superficie mais proxima (doc §5).</summary>
        float DistRota(float x, float y)
        {
            float melhor = 1e9f;
            if (D.rotas == null) return melhor;
            foreach (Rota r in D.rotas)
                for (int k = 0; k + 1 < r.x.Length; k++)
                {
                    Vector2 a = new Vector2(r.x[k], r.y[k]), ab = new Vector2(r.x[k + 1], r.y[k + 1]) - a, ap = new Vector2(x, y) - a;
                    float u = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
                    melhor = Mathf.Min(melhor, (ap - ab * u).magnitude);
                }
            return melhor;
        }

        bool EmPiso(float x, float y)
        {
            foreach (Regiao r in D.regioes)
            {
                if (r.id == "R02" || r.id == "R11" || r.id == "R01") continue;
                float rr = Mathf.Min(r.ex, r.ey) * 0.3f;
                if ((x - r.x) * (x - r.x) + (y - r.y) * (y - r.y) < rr * rr) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ agua (shader do jogo)

        void Agua()
        {
            Material mar = Ilha.MaterialAgua("MestreMar", 0x2f8fe0, 0x0d3576, 0.16f, 0.6f, 0.5f, 0.6f);
            Material lago = Ilha.MaterialAgua("MestreLago", 0x35b0f2, 0x0f4f9e, 0.07f, 1f, 1.15f, 1f);
            Material rio = Ilha.MaterialAgua("MestreRio", 0x49c2e6, 0x1a6fa8, 0.10f, 1f, 1.3f, 1f);
            // mar: grade de 50 m sobre a ilha; COLOR.r = o quanto e' raso (o shader pinta turquesa e espuma na beira)
            Grade("Mar", mar, -6000f, -6000f, 12000f, 12000f, 60f, 0f, (x, y) => Mathf.InverseLerp(-14f, 0f, Altura(x, y)));
            Grade("Lago", lago, D.lago.cx - D.lago.a * 1.3f, D.lago.cy - D.lago.b * 1.3f, D.lago.a * 2.6f, D.lago.b * 2.6f, 10f,
                D.lago.nivel, (x, y) => Mathf.InverseLerp(D.lago.nivel - 10f, D.lago.nivel, Altura(x, y)));
            FitaDoRio(rio);
        }

        void Grade(string nome, Material m, float ox, float oy, float sx, float sy, float passo, float z, Func<float, float, float> raso)
        {
            int cx = Mathf.CeilToInt(sx / passo) + 1, cy = Mathf.CeilToInt(sy / passo) + 1;
            var v = new Vector3[cx * cy];
            var c = new Color[cx * cy];
            var tri = new int[(cx - 1) * (cy - 1) * 6];
            for (int j = 0; j < cy; j++)
            for (int i = 0; i < cx; i++)
            {
                float x = ox + i * passo, y = oy + j * passo;
                v[j * cx + i] = U(x - ox + passo * 2f, y - oy + passo * 2f, z);
                c[j * cx + i] = new Color(Mathf.Clamp01(raso(x, y)), 0f, 0f, 1f);
            }
            int t = 0;
            for (int j = 0; j < cy - 1; j++)
            for (int i = 0; i < cx - 1; i++)
            {
                int a = j * cx + i;
                tri[t++] = a; tri[t++] = a + cx; tri[t++] = a + 1;
                tri[t++] = a + 1; tri[t++] = a + cx; tri[t++] = a + cx + 1;
            }
            Malha(nome, m, v, c, tri).transform.position = U(ox - passo * 2f, oy - passo * 2f, 0f);
        }

        void FitaDoRio(Material m)
        {
            var v = new List<Vector3>();
            var c = new List<Color>();
            var tri = new List<int>();
            for (int k = 0; k + 1 < D.rio.Length; k++)
            {
                Ponto a = D.rio[k], b = D.rio[k + 1];
                Vector2 dir = new Vector2(b.x - a.x, b.y - a.y);
                float len = dir.magnitude;
                dir /= len;
                Vector2 nor = new Vector2(-dir.y, dir.x);
                int passos = Mathf.Max(1, Mathf.CeilToInt(len / 8f));
                for (int s = (k == 0 ? 0 : 1); s <= passos; s++)
                {
                    float u = s / (float)passos;
                    if (Mathf.Lerp(a.z, b.z, u) < 0.8f) break;   // a fita acaba na foz: dali e' mar
                    float x = a.x + dir.x * len * u, y = a.y + dir.y * len * u;
                    float z = Mathf.Lerp(a.z, b.z, u) + 0.15f, w = Mathf.Lerp(a.meia, b.meia, u) + 7f;
                    v.Add(U(x - nor.x * w, y - nor.y * w, z));
                    v.Add(U(x + nor.x * w, y + nor.y * w, z));
                    c.Add(new Color(0.55f, 0, 0, 1));
                    c.Add(new Color(0.55f, 0, 0, 1));
                }
            }
            for (int q = 0; q + 3 < v.Count; q += 2)
            {
                tri.Add(q); tri.Add(q + 1); tri.Add(q + 2);
                tri.Add(q + 2); tri.Add(q + 1); tri.Add(q + 3);
            }
            Malha("Rio", m, v.ToArray(), c.ToArray(), tri.ToArray());
        }

        GameObject Malha(string nome, Material m, Vector3[] v, Color[] c, int[] tri)
        {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, name = nome };
            mesh.vertices = v;
            mesh.colors = c;
            mesh.triangles = tri;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(nome) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // ------------------------------------------------------------------ atmosfera

        void Atmosfera()
        {
            // na cena Main ja' existe o sol do jogo (Sol): reaproveita a luz em vez de acender uma segunda
            Light l = RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional ? RenderSettings.sun : null;
            if (l == null)
            {
                var sol = new GameObject("Sol_Tarde_SO") { hideFlags = HideFlags.DontSave };
                sol.transform.SetParent(raiz, false);
                l = sol.AddComponent<Light>();
            }
            l.transform.rotation = Quaternion.Euler(36f, 45f, 0f);   // vem do sudoeste, vai para nordeste (doc §8.2)
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.92f, 0.80f);
            l.intensity = 1.7f;
            l.shadows = LightShadows.Soft;
            RenderSettings.sun = l;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.80f, 0.84f);
            // longe e leve (doc §8.2: "neblina so' como acabamento leve e distante"): a 1500 m ela lavava a vista de mapa
            RenderSettings.fogStartDistance = 4000f;
            RenderSettings.fogEndDistance = 18000f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.47f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.48f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.27f, 0.24f);
            Material ceu = Ilha.MaterialCeu();
            if (ceu != null) RenderSettings.skybox = ceu;
        }

        // ------------------------------------------------------------------ blockout das 12 regioes

        Material Mat(Color c)
        {
            if (mats.TryGetValue(c, out Material m) && m != null) return m;
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(s) { color = c, name = "blockout_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetFloat("_Smoothness", 0.15f);
            mats[c] = m;
            return m;
        }

        static readonly Color Pedra = new Color(0.72f, 0.66f, 0.55f), PedraEsc = new Color(0.42f, 0.40f, 0.38f),
            Madeira = new Color(0.45f, 0.30f, 0.17f), Telha = new Color(0.62f, 0.30f, 0.20f), Ferrugem = new Color(0.45f, 0.26f, 0.15f),
            Metal = new Color(0.30f, 0.31f, 0.32f), Concreto = new Color(0.58f, 0.58f, 0.54f), VerdeMil = new Color(0.33f, 0.38f, 0.28f),
            Lona = new Color(0.78f, 0.70f, 0.52f), Casca = new Color(0.36f, 0.25f, 0.16f), Folha = new Color(0.22f, 0.40f, 0.16f),
            Escuro = new Color(0.05f, 0.05f, 0.06f);

        Transform Grupo(string nome)
        {
            var g = new GameObject(nome) { hideFlags = HideFlags.DontSave };
            g.transform.SetParent(raiz, false);
            return g.transform;
        }

        /// <summary>Volume com a BASE no chao (z &lt; -1000 = amostra o terreno) — x,y do doc, dimensoes (largura x, prof. y, altura).</summary>
        GameObject Bloco(Transform pai, string nome, PrimitiveType tipo, float x, float y, float z, Vector3 dim, float rotY, Color cor)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            g.name = nome;
            g.hideFlags = HideFlags.DontSave;
            g.transform.SetParent(pai, false);
            float chao = z < -1000f ? Altura(x, y) : z;
            g.transform.localScale = tipo == PrimitiveType.Cylinder || tipo == PrimitiveType.Capsule
                ? new Vector3(dim.x, dim.z * 0.5f, dim.y) : new Vector3(dim.x, dim.z, dim.y);
            g.transform.position = U(x, y, chao + dim.z * 0.5f);
            g.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            g.GetComponent<Renderer>().sharedMaterial = Mat(cor);
            return g;
        }

        GameObject Peca(Transform pai, string recurso, float x, float y, float esc, float rotY, float z = -9999f)
        {
            var src = Resources.Load<GameObject>(recurso);
            if (src == null) return null;
            GameObject g = Instantiate(src, pai);
            g.hideFlags = HideFlags.DontSave;
            g.transform.localScale = Vector3.one * esc;
            g.transform.position = U(x, y, z < -1000f ? Altura(x, y) : z);
            g.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            return g;
        }

        Regiao R(string id) => Array.Find(D.regioes, r => r.id == id);
        float Rn(float a, float b) => (float)(a + rnd.NextDouble() * (b - a));

        Vector2 Espalhar(Regiao r, float raio)
        {
            double a = rnd.NextDouble() * Math.PI * 2, d = Math.Sqrt(rnd.NextDouble()) * raio;
            return new Vector2(r.x + (float)(Math.Cos(a) * d), r.y + (float)(Math.Sin(a) * d));
        }

        void Regioes()
        {
            // R01 lago: docas e santuario na ilhota principal
            Transform g = Grupo("ARKANA_R01_Lago");
            float[] ang = { 200f, 250f, 20f, 120f };
            foreach (float a in ang)
            {
                float rad = a * Mathf.Deg2Rad;
                float x = D.lago.cx + Mathf.Cos(rad) * D.lago.a * 0.97f, y = D.lago.cy + Mathf.Sin(rad) * D.lago.b * 0.97f;
                Bloco(g, "R01_Doca", PrimitiveType.Cube, x, y, D.lago.nivel + 0.4f, new Vector3(5f, 26f, 0.5f), -a + 90f, Madeira);
            }
            Ilhota ip = D.ilhotas[0];
            for (int k = 0; k < 6; k++)
                Peca(g, "38-coluna-ruina", ip.x + Mathf.Cos(k * 1.05f) * 11f, ip.y + Mathf.Sin(k * 1.05f) * 11f, 2.2f, k * 60f);
            Peca(g, "24-estatua-vigia", ip.x, ip.y, 2.5f, 180f);

            // R02 floresta: arvore principal oca (doc §4.2: base 280, 180-220 m, tronco 35-50 m) + gigantes secundarias
            g = Grupo("ARKANA_R02_Floresta");
            ArvoreGigante(g, -100f, 1300f, 44f, 205f);
            for (int k = 0; k < 8; k++)
            {
                Vector2 p = Espalhar(R("R02"), 520f);
                if (Declive(p.x, p.y) > 20f) continue;
                ArvoreGigante(g, p.x, p.y, Rn(8f, 12f), Rn(55f, 95f));
            }

            // R03 templo: terracos, torre central 45 m, alas com colunas, escadaria, estatuas
            g = Grupo("ARKANA_R03_Templo");
            Regiao t = R("R03");
            // tudo assentado no chao REAL (o gerador ja' faz o monte do santuario a 166 sobre o patio a 150)
            float zs = Altura(t.x + 10f, t.y + 10f);
            Bloco(g, "R03_Patio_Lajes", PrimitiveType.Cube, t.x, t.y, Altura(t.x - 30f, t.y) - 1.5f, new Vector3(200f, 160f, 2f), 0f, Pedra);
            Bloco(g, "R03_Terraco_Superior", PrimitiveType.Cube, t.x + 10f, t.y + 10f, zs - 1f, new Vector3(120f, 90f, 5f), 0f, Pedra);
            Bloco(g, "R03_Torre_Santuario", PrimitiveType.Cube, t.x + 10f, t.y + 10f, zs + 4f, new Vector3(20f, 20f, 45f), 0f, Pedra);
            Bloco(g, "R03_Torre_Coroa", PrimitiveType.Cube, t.x + 10f, t.y + 10f, zs + 49f, new Vector3(14f, 14f, 8f), 45f, PedraEsc);
            Bloco(g, "R03_Escadaria_Oeste", PrimitiveType.Cube, t.x - 55f, t.y + 10f, -9999f, new Vector3(18f, 30f, 6f), 0f, PedraEsc);
            for (int k = 0; k < 10; k++)
            {
                Peca(g, "38-coluna-ruina", t.x - 40f + k * 10f, t.y - 45f, 2.6f, 0f);
                Peca(g, "38-coluna-ruina", t.x - 40f + k * 10f, t.y + 65f, 2.6f, 0f);
            }
            Peca(g, "24-estatua-vigia", t.x - 50f, t.y - 5f, 3f, 90f);
            Peca(g, "24-estatua-vigia", t.x - 50f, t.y + 25f, 3f, 90f);
            Peca(g, "33-obelisco", t.x + 70f, t.y - 60f, 2.5f, 0f);
            Peca(g, "33-obelisco", t.x + 70f, t.y + 80f, 2.5f, 0f);

            // R04 vila: 30 casas em 3 faixas de terraco, praca com poco, torrinha arruinada
            g = Grupo("ARKANA_R04_Vila");
            Regiao v = R("R04");
            int casas = 0;
            string[] tipos = { "mestre-015-vila-casa-inteira", "mestre-109-vila-casa-grande", "mestre-103-vila-casa-terrea", "mestre-016-vila-casa-danificada" };
            var ocupadas = new List<Vector2>();
            for (int k = 0; k < 1500 && casas < 30; k++)
            {
                Vector2 p = Espalhar(v, 210f);
                if (Declive(p.x, p.y) > 10f || (p - new Vector2(v.x + 60f, v.y - 60f)).magnitude < 45f) continue;
                if (ocupadas.Exists(q => (q - p).magnitude < 20f)) continue;   // casas de 9-14 m nao se atropelam
                ocupadas.Add(p);
                float cw = Rn(10f, 16f), cd = 9f, cr = Rn(0f, 360f);
                if (Peca(g, tipos[casas % tipos.Length], p.x, p.y, 1f, cr, Altura(p.x, p.y) - 0.3f) == null)
                    Casa(g, p.x, p.y, cw, cd, cr, casas % 4 == 0);
                if (casas % 3 == 0)   // quintal: 3 modulos de cerca na frente da PORTA (a porta olha para -Y local)
                {
                    Vector2 fr = -new Vector2(Mathf.Sin(cr * Mathf.Deg2Rad), Mathf.Cos(cr * Mathf.Deg2Rad)), la = new Vector2(fr.y, -fr.x);
                    for (int m = -1; m <= 1; m++)
                    {
                        Vector2 c = p + fr * (cd * 0.5f + 6f) + la * (m * 3.2f + (m == 0 ? 4f : 0f));
                        Peca(g, "mestre-025-vila-cerca-madeira", c.x, c.y, 1f, cr);
                    }
                }
                casas++;
            }
            Bloco(g, "R04_Poco", PrimitiveType.Cylinder, v.x + 60f, v.y - 60f, -9999f, new Vector3(3f, 3f, 1.2f), 0f, PedraEsc);
            Bloco(g, "R04_Torrinha_Ruina", PrimitiveType.Cube, v.x - 120f, v.y + 90f, -9999f, new Vector3(7f, 7f, 18f), 12f, Pedra);

            // R05 ponte leste-oeste (corrigida) + travessia inferior a Z 80 ao sul
            g = Grupo("ARKANA_R05_Ponte");
            Ponte pt = D.ponte;
            float cxp = (pt.x0 + pt.x1) / 2f, comp = pt.x1 - pt.x0;
            // viaduto de arcos do kit (093): o topo do tabuleiro fica a 90 m da base da peca
            if (Peca(g, "mestre-093-ponte-de-arcos", cxp, pt.y, 1f, 0f, pt.z - 90f) == null)
            {
                Bloco(g, "R05_Ponte_Tabuleiro", PrimitiveType.Cube, cxp, pt.y, pt.z - 2f, new Vector3(comp, pt.largura, 2f), 0f, Pedra);
                foreach (float px in new[] { cxp - 75f, cxp, cxp + 75f })
                {
                    float fundo = Altura(px, pt.y);
                    Bloco(g, "R05_Pilar", PrimitiveType.Cube, px, pt.y, fundo, new Vector3(9f, 12f, pt.z - 2f - fundo), 0f, Pedra);
                }
            }
            Bloco(g, "R05_Travessia_Inferior_Z80", PrimitiveType.Cube, cxp, pt.y - 170f, 79f, new Vector3(comp * 0.8f, 8f, 1f), 0f, Madeira);

            // R06 acampamento: barracas, abrigos, fogueiras e as caixas do kit (027)
            g = Grupo("ARKANA_R06_Acampamento");
            Regiao a6 = R("R06");
            for (int k = 0; k < 10; k++)
            {
                float a = k * 36f * Mathf.Deg2Rad, d = Rn(18f, 26f);   // anel de barracas viradas para o centro
                Peca(g, "mestre-053-acampamento-barraca", a6.x + Mathf.Cos(a) * d, a6.y + Mathf.Sin(a) * d, 1f, -k * 36f + 90f);
            }
            for (int k = 0; k < 3; k++)
            {
                Vector2 p = Espalhar(a6, 22f);
                float r = Rn(0f, 180f);
                Peca(g, "mestre-058-acampamento-mesa", p.x, p.y, 1f, r);
                Vector2 o = new Vector2(Mathf.Cos(r * Mathf.Deg2Rad), -Mathf.Sin(r * Mathf.Deg2Rad)) * 0.9f;
                Peca(g, "mestre-055-acampamento-banco", p.x + o.y, p.y + o.x, 1f, r);
                Peca(g, "mestre-055-acampamento-banco", p.x - o.y, p.y - o.x, 1f, r);
            }
            for (int k = 0; k < 9; k++)
            {
                Vector2 p = Espalhar(a6, 30f);
                Peca(g, "mestre-028-acampamento-barril", p.x, p.y, 1f, Rn(0f, 360f));
            }
            for (int k = 0; k < 2; k++)
                Bloco(g, "R06_Abrigo", PrimitiveType.Cube, a6.x - 16f + k * 32f, a6.y + 34f, -9999f, new Vector3(12f, 8f, 4f), 20f * k, Madeira);
            for (int k = 0; k < 3; k++)
            {
                Vector2 p = Espalhar(a6, 10f);
                Bloco(g, "R06_Fogueira", PrimitiveType.Cylinder, p.x, p.y, -9999f, new Vector3(1.6f, 1.6f, 0.3f), 0f, Escuro);
            }
            for (int k = 0; k < 12; k++)
            {
                Vector2 p = Espalhar(a6, 32f);
                Peca(g, "mestre-027-acampamento-caixa-madeira", p.x, p.y, 1f, Rn(0f, 360f));
            }

            // R07 industria: galpoes, tanques, torre industrial de 75 m, guindastes
            g = Grupo("ARKANA_R07_Industria");
            Regiao r7 = R("R07");
            var galpoes = new List<Vector2>();
            for (int k = 0, tent = 0; k < 8 && tent < 2000; tent++)
            {
                Vector2 p = Espalhar(r7, 240f);
                if (Declive(p.x, p.y) > 12f || galpoes.Exists(q => (q - p).magnitude < 55f)) continue;
                galpoes.Add(p);
                float rot = Rn(-15f, 15f) + (k % 2) * 90f;
                if (Peca(g, "mestre-097-industrial-galpao", p.x, p.y, 1f, rot, Altura(p.x, p.y) - 0.5f) == null)
                    Bloco(g, "R07_Galpao", PrimitiveType.Cube, p.x, p.y, -9999f, new Vector3(40f, 24f, 12f), rot, Ferrugem);
                k++;
            }
            for (int k = 0; k < 4; k++)
                Peca(g, "mestre-064-industrial-tanque", r7.x + 90f + k * 18f, r7.y - 70f, 2.4f, k * 40f);
            for (int k = 0; k < 10; k++)   // linha de canos (escala 1,5: 6 m por modulo) ligando os tanques aos galpoes
                Peca(g, "mestre-062-industrial-cano-reto", r7.x + 100f - k * 9f, r7.y - 52f, 1.5f, 0f);
            Bloco(g, "R07_Torre_Industrial", PrimitiveType.Cube, r7.x - 20f, r7.y + 40f, -9999f, new Vector3(10f, 10f, 75f), 0f, Ferrugem);
            Bloco(g, "R07_Torre_Topo", PrimitiveType.Cylinder, r7.x - 20f, r7.y + 40f, Altura(r7.x - 20f, r7.y + 40f) + 75f, new Vector3(14f, 14f, 6f), 0f, Metal);
            for (int k = 0; k < 2; k++)
            {
                float x = r7.x + 60f - k * 150f, y = r7.y + 120f;
                Bloco(g, "R07_Guindaste", PrimitiveType.Cube, x, y, -9999f, new Vector3(3f, 3f, 40f), 0f, Ferrugem);
                Bloco(g, "R07_Guindaste_Lanca", PrimitiveType.Cube, x + 14f, y, Altura(x, y) + 38f, new Vector3(34f, 2f, 2f), 0f, Ferrugem);
            }

            // R08 base militar: comando, alojamentos, hangares, torres de vigia, heliponto de 28 m, cerca com brechas
            g = Grupo("ARKANA_R08_Base");
            Regiao b8 = R("R08");
            if (Peca(g, "mestre-079-militar-comando", b8.x, b8.y, 1f, 0f, Altura(b8.x, b8.y) - 0.3f) == null)
                Bloco(g, "R08_Comando", PrimitiveType.Cube, b8.x, b8.y, -9999f, new Vector3(30f, 20f, 10f), 0f, Concreto);
            for (int k = 0; k < 3; k++)
            {
                float ax = b8.x - 70f, ay = b8.y - 50f + k * 30f;
                if (Peca(g, "mestre-078-militar-alojamento", ax, ay, 1f, 0f, Altura(ax, ay) - 0.3f) == null)
                    Bloco(g, "R08_Alojamento", PrimitiveType.Cube, ax, ay, -9999f, new Vector3(32f, 11f, 6f), 0f, VerdeMil);
            }
            for (int k = 0; k < 2; k++)
            {
                GameObject h = Bloco(g, "R08_Hangar", PrimitiveType.Cylinder, b8.x + 80f, b8.y - 40f + k * 55f, -9999f, new Vector3(30f, 30f, 42f), 0f, VerdeMil);
                h.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                h.transform.position = U(b8.x + 80f, b8.y - 40f + k * 55f, Altura(b8.x + 80f, b8.y - 40f + k * 55f));
            }
            Bloco(g, "R08_Heliponto", PrimitiveType.Cylinder, b8.x - 10f, b8.y + 70f, -9999f, new Vector3(28f, 28f, 0.4f), 0f, Concreto);
            Bloco(g, "R08_Deposito", PrimitiveType.Cube, b8.x + 20f, b8.y - 80f, -9999f, new Vector3(20f, 15f, 6f), 0f, Concreto);
            for (int k = 0; k < 14; k++)   // barreiras em zigue-zague no portao e cobertura no patio (doc §3.2: 15-35 m)
            {
                float x = k < 6 ? b8.x - 60f + k * 6f : b8.x + Rn(-120f, 120f), y = k < 6 ? b8.y - 150f + (k % 2) * 4f : b8.y + Rn(-100f, 100f);
                Peca(g, "mestre-073-militar-barreira-concreto", x, y, 1f, k < 6 ? 0f : Rn(0f, 180f));
            }
            for (int k = 0; k < 12; k++)
            {
                Vector2 p = new Vector2(b8.x + 20f + Rn(-18f, 18f), b8.y - 65f + Rn(-6f, 6f));
                Peca(g, "mestre-074-militar-caixa-militar", p.x, p.y, 1f, Rn(0f, 90f));
            }
            float hx = 190f, hy = 150f;
            foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                if (Peca(g, "mestre-076-militar-torre-de-vigia", b8.x + c.x * hx, b8.y + c.y * hy, 1f, 45f) == null)
                    Bloco(g, "R08_Torre_Vigia", PrimitiveType.Cube, b8.x + c.x * hx, b8.y + c.y * hy, -9999f, new Vector3(4f, 4f, 15f), 0f, Metal);
            for (int k = 0; k < 24; k++)
            {
                if (k % 6 == 2) continue;   // brechas no perimetro (doc: pelo menos 3)
                float u = (k % 6 + 0.5f) / 6f, lado = k / 6;
                float x = lado < 2 ? b8.x - hx + u * 2 * hx : b8.x + (lado == 2 ? hx : -hx);
                float y = lado < 2 ? b8.y + (lado == 0 ? -hy : hy) : b8.y - hy + u * 2 * hy;
                Bloco(g, "R08_Cerca", PrimitiveType.Cube, x, y, -9999f, lado < 2 ? new Vector3(2 * hx / 6f, 0.3f, 2.5f) : new Vector3(0.3f, 2 * hy / 6f, 2.5f), 0f, Metal);
            }

            // R09 torre de observacao: base 16x16 no plato 430, plataforma a 478, topo 488
            g = Grupo("ARKANA_R09_Torre");
            Regiao r9 = R("R09");
            Bloco(g, "R09_Base", PrimitiveType.Cube, r9.x, r9.y, 430f, new Vector3(16f, 16f, 14f), 0f, PedraEsc);
            Bloco(g, "R09_Fuste", PrimitiveType.Cube, r9.x, r9.y, 444f, new Vector3(12f, 12f, 34f), 0f, Pedra);
            Bloco(g, "R09_Plataforma_478", PrimitiveType.Cube, r9.x, r9.y, 477f, new Vector3(18f, 18f, 1f), 0f, Madeira);
            Bloco(g, "R09_Sala_Observacao", PrimitiveType.Cube, r9.x, r9.y, 478f, new Vector3(9f, 9f, 6f), 0f, Pedra);
            Bloco(g, "R09_Topo_488", PrimitiveType.Cube, r9.x, r9.y, 484f, new Vector3(6f, 6f, 4f), 45f, Telha);

            // R10 entrada da caverna profunda (U01) + entradas de superficie da rede subterranea
            g = Grupo("ARKANA_R10_R11_Entradas");
            Peca(g, "19-arco-calcario-nymara", 850f, 950f, 6f, 200f, 220f);
            foreach (No u in D.nos)
            {
                if (u.id != "U01" && u.id != "U05" && u.id != "U06" && u.id != "U08" && u.id != "U13" && u.id != "U15") continue;
                Bloco(g, "Entrada_" + u.id, PrimitiveType.Cube, u.x, u.y, Altura(u.x, u.y) - 0.5f, new Vector3(9f, 6f, 7f), 0f, Escuro);
            }

            // pedras do kit espalhadas (cobertura solida, doc §3.2): encostas, costa e transicoes
            g = Grupo("ARKANA_20_Pedras");
            string[] pedras = { "17-rocha-basalto-modular", "18-pedregulho", "41-monolito-basalto", "47-rocha-costa" };
            for (int k = 0, feitas = 0; k < 20000 && feitas < 260; k++)
            {
                float x = Rn(D.x0, D.x1), y = Rn(D.y0, D.y1), h = Altura(x, y);
                if (h < 1f || LagoQ(x, y) < 1.05f || PertoDoRio(x, y, 5f) || EmPiso(x, y) || DistRota(x, y) < 8f) continue;
                // em grupos, nas encostas e na costa (cobertura legivel), nao salpicadas pela ilha toda
                if (Declive(x, y) < 12f && h > 6f && Mathf.PerlinNoise(x * 0.006f + 3f, y * 0.006f + 9f) < 0.62f) continue;
                string p = pedras[h < 6f ? 3 : rnd.Next(3)];
                Peca(g, p, x, y, Rn(2f, 5f), Rn(0f, 360f), h - 0.5f);
                feitas++;
            }
        }

        void ArvoreGigante(Transform g, float x, float y, float tronco, float altura)
        {
            float z = Altura(x, y);
            Bloco(g, "Arvore_Tronco", PrimitiveType.Cylinder, x, y, z, new Vector3(tronco, tronco, altura * 0.75f), 0f, Casca);
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3f + 0.3f;
                GameObject rz = Bloco(g, "Arvore_Raiz", PrimitiveType.Cylinder, x + Mathf.Cos(a) * tronco * 0.9f, y + Mathf.Sin(a) * tronco * 0.9f, z - 2f,
                    new Vector3(tronco * 0.22f, tronco * 0.22f, tronco * 1.6f), 0f, Casca);
                rz.transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), -1.2f, Mathf.Sin(a))) * Quaternion.Euler(90f, 0f, 0f);
            }
            for (int k = 0; k < 5; k++)
            {
                float a = k * 1.3f, d = k == 0 ? 0f : tronco * 1.2f;
                GameObject c = Bloco(g, "Arvore_Copa", PrimitiveType.Sphere, x + Mathf.Cos(a) * d, y + Mathf.Sin(a) * d,
                    z + altura * (0.72f + 0.05f * (k % 2)), new Vector3(tronco * 3.2f, tronco * 3.2f, altura * 0.3f), 0f, Folha);
                c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            }
            Bloco(g, "Arvore_Acesso_Oco", PrimitiveType.Cube, x, y - tronco * 0.5f, z, new Vector3(12f, 3f, 18f), 0f, Escuro);
        }

        void Casa(Transform g, float x, float y, float w, float d, float rot, bool arruinada)
        {
            float z = Altura(x, y);
            float h = arruinada ? 4f : Rn(5.5f, 8.5f);
            Bloco(g, "R04_Casa", PrimitiveType.Cube, x, y, z - 0.5f, new Vector3(w, d, h + 0.5f), rot, arruinada ? PedraEsc : Pedra);
            if (arruinada) return;
            GameObject telhado = Bloco(g, "R04_Telhado", PrimitiveType.Cube, x, y, z + h - d * 0.36f, new Vector3(w + 0.8f, d * 0.72f, d * 0.72f), rot, Telha);
            telhado.transform.rotation = Quaternion.Euler(0f, rot, 0f) * Quaternion.Euler(45f, 0f, 0f);
            telhado.transform.position = U(x, y, z + h);
        }

        // ------------------------------------------------------------------ cameras do doc §17.1

        void Cameras()
        {
            Camera principal = Camera.main;
            Cam("CAM_Mapa_Topo", U(0f, 0f, 4000f), Quaternion.Euler(90f, 0f, 0f), true);
            Cam("CAM_Mapa_Sudoeste", U(-3300f, -3300f, 1900f), Quaternion.LookRotation(U(0f, 150f, 150f) - U(-3300f, -3300f, 1900f)), false);
            Cam("CAM_Mapa_Nordeste", U(3300f, 3300f, 1900f), Quaternion.LookRotation(U(0f, 150f, 150f) - U(3300f, 3300f, 1900f)), false);
            if (principal != null) principal.farClipPlane = Mathf.Max(principal.farClipPlane, 15000f);
        }

        void Cam(string nome, Vector3 pos, Quaternion rot, bool orto)
        {
            var go = new GameObject(nome) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(raiz, false);
            go.transform.SetPositionAndRotation(pos, rot);
            Camera c = go.AddComponent<Camera>();
            c.enabled = false;
            c.farClipPlane = 15000f;
            c.nearClipPlane = 1f;
            c.orthographic = orto;
            c.orthographicSize = 2350f;
            c.fieldOfView = 40f;
        }
    }
}
