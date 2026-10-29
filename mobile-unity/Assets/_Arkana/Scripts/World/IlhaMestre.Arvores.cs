using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arkana.World
{
    /// <summary>
    /// AS ARVORES DA ILHA, INSTANCIADAS (29/10/2026). Como instancias do UnityEngine.Terrain cada arvore era um draw (o SRP
    /// Batcher barateia o setpass, nao o draw): ~500 batches de perto no treino e ~4.400 no chao. Agora e' a receita do
    /// Vegetacao da ilha antiga: RenderMeshInstanced por BLOCO de PassoBloco m, um lote por especie x LOD, nenhum GameObject
    /// por arvore. LOD por arvore a 4 Hz pelo tamanho na tela (a mesma conta do LODGroup: altura / distancia x lodBias):
    /// LOD0 acima de LodLonge, o "-lod1" ate' LodCorte, nada abaixo. Material: o Lit instanciado do Vegetacao
    /// (ArkanaArvoreInstancing) com a textura do proprio .glb — o shader do importador nao leva a variante de instancing
    /// para o APK. Sem colisor, como era no Terrain.
    /// </summary>
    public sealed partial class IlhaMestre
    {
        const float PassoBloco = 200f;   // KNOB: bloco maior = menos draws, corte de frustum mais grosso
        const float PeriodoLod = 0.25f;
        const int MaxPorChamada = 1023;  // limite classico do instancing; lote maior sai em fatias

        sealed class LoteArv { public Matrix4x4[] M; public int N; }

        sealed class BlocoArv
        {
            public Bounds Caixa;
            public float Topo;   // a arvore mais alta do bloco (m): alem do corte dela, o bloco inteiro some
            public readonly List<int> Idx = new List<int>();
            public LoteArv[] Lotes;   // especie x LOD (2)
        }

        // por especie: [0] = LOD0, [1] = LOD1 (nulo se nao ha' "-lod1": a especie fica em LOD0 ate' o corte)
        Mesh[,] arvMalha;
        Material[,] arvMat;
        int[,] arvTris;
        // por arvore
        Matrix4x4[] arvM;
        Vector3[] arvPos;
        float[] arvAlt;
        int[] arvEsp;
        readonly List<BlocoArv> blocosArv = new List<BlocoArv>();
        float relogioLod = PeriodoLod;

        /// <summary>Arvores plantadas na ilha (o teste do boot e o export leem daqui).</summary>
        public int ArvoresPlantadas => arvPos != null ? arvPos.Length : 0;
        /// <summary>Especies carregadas e quantas tem o LOD1 de verdade.</summary>
        public int EspeciesDeArvore => arvMalha != null ? arvMalha.GetLength(0) : 0;
        public int EspeciesComLod1 { get; private set; }
        /// <summary>O ultimo corte (4 Hz): lotes que vao para a GPU, arvores neles e os tris somados.</summary>
        public int LotesDeArvore { get; private set; }
        public int ArvoresNaTela { get; private set; }
        public int TrisDeArvore { get; private set; }

        /// <summary>A arvore i para o export (nome do recurso, posicao de mundo, giro em radianos, escala).</summary>
        public (string recurso, Vector3 pos, float giro, float escala) Arvore(int i) =>
            (recursoArv[arvEsp[i]], arvPos[i], arvGiro[i], arvEsc[i]);
        string[] recursoArv;
        float[] arvGiro, arvEsc;

        /// <summary>Carrega as especies (LOD0 + "-lod1") e planta a lista do Arvores() em blocos. Chamado no Terrenos().</summary>
        void PlantarArvores(List<Arv> lista, string[] recursos, float[] alturaMolde)
        {
            int ne = recursos.Length;
            recursoArv = recursos;
            blocosArv.Clear();   // o "Reconstruir" do editor planta de novo
            arvMalha = new Mesh[ne, 2];
            arvMat = new Material[ne, 2];
            arvTris = new int[ne, 2];
            EspeciesComLod1 = 0;
            for (int e = 0; e < ne; e++)
            {
                for (int l = 0; l < 2; l++)
                {
                    Mesh m = MalhaDoMolde(recursos[e] + (l == 0 ? "" : "-lod1"), out Material[] ms);
                    if (m == null) continue;
                    Texture tex = null;
                    if (ms.Length > 0 && ms[0] != null)
                        tex = ms[0].HasProperty("baseColorTexture") ? ms[0].GetTexture("baseColorTexture") : ms[0].mainTexture;
                    Material cache = null;
                    Material mat = Vegetacao.MaterialDaMeshy(ref cache, "mestre_arvore_" + e + "_" + l, tex, Color.white);
                    if (mat == null) continue;
                    arvMalha[e, l] = m;
                    arvMat[e, l] = mat;
                    arvTris[e, l] = (int)(m.GetIndexCount(0) / 3);
                }
                if (arvMalha[e, 1] != null) EspeciesComLod1++;
            }

            int n0 = lista.Count;
            arvM = new Matrix4x4[n0];
            arvPos = new Vector3[n0];
            arvAlt = new float[n0];
            arvEsp = new int[n0];
            arvGiro = new float[n0];
            arvEsc = new float[n0];
            var porBloco = new Dictionary<int, BlocoArv>();
            int lado = Mathf.CeilToInt(Mathf.Max(D.x1 - D.x0, D.y1 - D.y0) / PassoBloco);
            for (int i = 0; i < n0; i++)
            {
                Arv a = lista[i];
                var p = U(a.x, a.y, Altura(a.x, a.y));
                float giro = (float)(rnd.NextDouble() * Mathf.PI * 2);
                arvPos[i] = p;
                arvEsp[i] = a.p;
                arvGiro[i] = giro;
                arvEsc[i] = a.esc;
                arvAlt[i] = alturaMolde[a.p] * a.esc;
                arvM[i] = Matrix4x4.TRS(p, Quaternion.Euler(0f, giro * Mathf.Rad2Deg, 0f), Vector3.one * a.esc);
                int k = (int)((a.y - D.y0) / PassoBloco) * lado + (int)((a.x - D.x0) / PassoBloco);
                if (!porBloco.TryGetValue(k, out BlocoArv b))
                {
                    porBloco[k] = b = new BlocoArv { Caixa = new Bounds(p, Vector3.zero), Lotes = new LoteArv[ne * 2] };
                    blocosArv.Add(b);
                }
                b.Idx.Add(i);
                b.Caixa.Encapsulate(p);
            }
            var cont = new int[ne];
            foreach (BlocoArv b in blocosArv)
            {
                System.Array.Clear(cont, 0, ne);
                float topo = 0f, largura = 0f;
                foreach (int i in b.Idx)
                {
                    cont[arvEsp[i]]++;
                    topo = Mathf.Max(topo, arvAlt[i]);
                    Mesh m0 = arvMalha[arvEsp[i], 0];
                    if (m0 != null) largura = Mathf.Max(largura, Mathf.Max(m0.bounds.size.x, m0.bounds.size.z) * arvEsc[i]);
                }
                for (int e = 0; e < ne; e++)
                {
                    if (cont[e] == 0) continue;
                    b.Lotes[e * 2] = new LoteArv { M = new Matrix4x4[cont[e]] };
                    b.Lotes[e * 2 + 1] = new LoteArv { M = new Matrix4x4[cont[e]] };
                }
                b.Topo = topo;
                var folga = new Vector3(largura * 0.5f + 1f, 1f, largura * 0.5f + 1f);
                b.Caixa.SetMinMax(b.Caixa.min - folga, b.Caixa.max + new Vector3(folga.x, topo + 1f, folga.z));
            }
        }

        /// <summary>
        /// 4 Hz: o LOD de cada arvore pela fracao da tela que ela ocupa (altura / (2 d tan(fov/2)) x lodBias, como o LODGroup).
        /// Bloco inteiro alem do corte da maior arvore dele nem olha as arvores.
        /// </summary>
        void ReclassificarArvores(Camera cam)
        {
            Vector3 olho = cam.transform.position;
            float k = QualitySettings.lodBias / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            // tamanho na tela = alt * k / d  ->  LOD0 se d < alt * k / LodLonge; some se d > alt * k / LodCorte
            float k0 = k / LodLonge, k1 = k / LodCorte;
            int lotes = 0, naTela = 0, tris = 0;
            foreach (BlocoArv b in blocosArv)
            {
                for (int j = 0; j < b.Lotes.Length; j++) if (b.Lotes[j] != null) b.Lotes[j].N = 0;
                float alcance = b.Topo * k1;
                if (b.Caixa.SqrDistance(olho) > alcance * alcance) continue;
                foreach (int i in b.Idx)
                {
                    float d = Vector3.Distance(olho, arvPos[i]), a = arvAlt[i];
                    if (d > a * k1) continue;
                    int e = arvEsp[i];
                    int l = d > a * k0 && arvMalha[e, 1] != null ? 1 : 0;
                    if (arvMalha[e, l] == null) continue;
                    LoteArv lote = b.Lotes[e * 2 + l];
                    lote.M[lote.N++] = arvM[i];
                }
                for (int j = 0; j < b.Lotes.Length; j++)
                {
                    LoteArv lote = b.Lotes[j];
                    if (lote == null || lote.N == 0) continue;
                    lotes += (lote.N + MaxPorChamada - 1) / MaxPorChamada;
                    naTela += lote.N;
                    tris += lote.N * arvTris[j >> 1, j & 1];
                }
            }
            LotesDeArvore = lotes;
            ArvoresNaTela = naTela;
            TrisDeArvore = tris;
        }

        void DesenharArvores()
        {
            foreach (BlocoArv b in blocosArv)
            for (int j = 0; j < b.Lotes.Length; j++)
            {
                LoteArv lote = b.Lotes[j];
                if (lote == null || lote.N == 0) continue;
                var rp = new RenderParams(arvMat[j >> 1, j & 1])
                {
                    worldBounds = b.Caixa,
                    shadowCastingMode = ShadowCastingMode.On,   // a do Terrain fazia sombra; continua fazendo
                    receiveShadows = true,
                    lightProbeUsage = LightProbeUsage.Off,
                    layer = gameObject.layer,
                };
                for (int ini = 0; ini < lote.N; ini += MaxPorChamada)
                    Graphics.RenderMeshInstanced(rp, arvMalha[j >> 1, j & 1], 0, lote.M, Mathf.Min(MaxPorChamada, lote.N - ini), ini);
            }
        }

        void Update()
        {
            if (blocosArv.Count == 0 || !SystemInfo.supportsInstancing) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            relogioLod += Time.deltaTime;
            if (relogioLod >= PeriodoLod || !Application.isPlaying)
            {
                relogioLod = 0f;
                ReclassificarArvores(cam);
            }
            DesenharArvores();
        }
    }
}
