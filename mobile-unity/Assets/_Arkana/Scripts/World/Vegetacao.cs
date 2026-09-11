using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arkana.World
{
    /// <summary>
    /// Malhas procedurais por codigo (zero asset): tronco de cone facetado, blob icosaedrico,
    /// caixa. Cor por vertice; sombreamento facetado (vertices nao compartilhados).
    /// A frente de cada triangulo e' decidida por `paraFora`, nao por ordem de escrita — o Godot
    /// e o Unity discordam de winding e de mao, e isso e' o que sobrevive a' troca.
    /// </summary>
    public static class MalhaProc
    {
        static readonly Vector3[] IcoV =
        {
            new Vector3(-1, 1.618f, 0), new Vector3(1, 1.618f, 0), new Vector3(-1, -1.618f, 0), new Vector3(1, -1.618f, 0),
            new Vector3(0, -1, 1.618f), new Vector3(0, 1, 1.618f), new Vector3(0, -1, -1.618f), new Vector3(0, 1, -1.618f),
            new Vector3(1.618f, 0, -1), new Vector3(1.618f, 0, 1), new Vector3(-1.618f, 0, -1), new Vector3(-1.618f, 0, 1),
        };
        static readonly int[] IcoF =
        {
            5, 11, 0, 1, 5, 0, 7, 1, 0, 10, 7, 0, 11, 10, 0,
            9, 5, 1, 4, 11, 5, 2, 10, 11, 6, 7, 10, 8, 1, 7,
            4, 9, 3, 2, 4, 3, 6, 2, 3, 8, 6, 3, 9, 8, 3,
            5, 9, 4, 11, 4, 2, 10, 2, 6, 7, 6, 8, 1, 8, 9,
        };

        public sealed class Construtor
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Color> C = new List<Color>();
            public readonly List<int> T = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Color col, Vector3 paraFora)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), paraFora) < 0f)
                {
                    Vector3 t = b; b = c; c = t;
                }
                int i = V.Count;
                V.Add(a); V.Add(b); V.Add(c);
                C.Add(col); C.Add(col); C.Add(col);
                T.Add(i); T.Add(i + 1); T.Add(i + 2);
            }

            /// <summary>Tronco de cone (so' as laterais), base em `pe`, escurece no pe' (sombra de contato de graca).</summary>
            public void Tronco(Vector3 pe, float r0, float r1, float h, int lados, Color col)
            {
                Tronco(pe, r0, r1, h, lados, col, col);
            }

            public void Tronco(Vector3 pe, float r0, float r1, float h, int lados, Color col, Color topo)
            {
                Color cbot = Relevo.Escurecer(col, 0.22f);
                for (int i = 0; i < lados; i++)
                {
                    float a0 = Mathf.PI * 2f * i / lados;
                    float a1 = Mathf.PI * 2f * (i + 1) / lados;
                    Vector3 lo0 = pe + new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0);
                    Vector3 lo1 = pe + new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0);
                    Vector3 hi1 = pe + new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1);
                    Vector3 hi0 = pe + new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1);
                    float am = (a0 + a1) * 0.5f;
                    Vector3 fora = new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am));
                    // gradiente por vertice: cor do pe' embaixo, cor do topo em cima
                    TriCores(lo0, lo1, hi1, cbot, cbot, topo, fora);
                    TriCores(lo0, hi1, hi0, cbot, topo, topo, fora);
                }
            }

            void TriCores(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc, Vector3 paraFora)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), paraFora) < 0f)
                {
                    Vector3 t = b; b = c; c = t;
                    Color tc = cb; cb = cc; cc = tc;
                }
                int i = V.Count;
                V.Add(a); V.Add(b); V.Add(c);
                C.Add(ca); C.Add(cb); C.Add(cc);
                T.Add(i); T.Add(i + 1); T.Add(i + 2);
            }

            /// <summary>Tampa circular horizontal em `centro`, virada para cima.</summary>
            public void Tampa(Vector3 centro, float r, int lados, Color col)
            {
                for (int i = 0; i < lados; i++)
                {
                    float a0 = Mathf.PI * 2f * i / lados;
                    float a1 = Mathf.PI * 2f * (i + 1) / lados;
                    Tri(centro,
                        centro + new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r),
                        centro + new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r), col, Vector3.up);
                }
            }

            /// <summary>Blob icosaedrico facetado com jitter — copa de arvore, rocha, moita.</summary>
            public void Blob(Vector3 centro, Vector3 escala, Color col, Sorteio rng, float jit)
            {
                var vs = new Vector3[IcoV.Length];
                for (int i = 0; i < IcoV.Length; i++)
                {
                    float f = 1f + rng.Faixa(-jit, jit * 1.2f);
                    Vector3 n = IcoV[i].normalized;
                    vs[i] = centro + new Vector3(n.x * f * escala.x, n.y * f * escala.y, n.z * f * escala.z);
                }
                for (int f = 0; f < IcoF.Length; f += 3)
                {
                    Vector3 a = vs[IcoF[f]], b = vs[IcoF[f + 1]], c = vs[IcoF[f + 2]];
                    Tri(a, b, c, col, (a + b + c) / 3f - centro);
                }
            }

            /// <summary>Caixa facetada centrada em `centro`, meias-dimensoes `meia`.</summary>
            public void Caixa(Vector3 centro, Vector3 meia, Color col)
            {
                Vector3[] eixos = { Vector3.right, Vector3.up, Vector3.forward };
                for (int e = 0; e < 3; e++)
                {
                    Vector3 n = eixos[e], u = eixos[(e + 1) % 3], v = eixos[(e + 2) % 3];
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 fora = n * s;
                        Vector3 c0 = centro + Vector3.Scale(fora, meia);
                        Vector3 du = Vector3.Scale(u, meia), dv = Vector3.Scale(v, meia);
                        Vector3 p0 = c0 - du - dv, p1 = c0 + du - dv, p2 = c0 + du + dv, p3 = c0 - du + dv;
                        Tri(p0, p1, p2, col, fora);
                        Tri(p0, p2, p3, col, fora);
                    }
                }
            }

            /// <summary>Copia `proto` transformado por `m`, cor multiplicada por `tinta`.</summary>
            public void Adicionar(Construtor proto, Matrix4x4 m, Color tinta)
            {
                int baseIdx = V.Count;
                for (int i = 0; i < proto.V.Count; i++)
                {
                    V.Add(m.MultiplyPoint3x4(proto.V[i]));
                    Color c = proto.C[i];
                    C.Add(new Color(c.r * tinta.r, c.g * tinta.g, c.b * tinta.b, c.a));
                }
                for (int i = 0; i < proto.T.Count; i++) T.Add(baseIdx + proto.T[i]);
            }

            public void Limpar()
            {
                V.Clear(); C.Clear(); T.Clear();
            }

            /// <summary>Malha facetada (normais recalculadas por face, vertices nao compartilhados). `linear` converte a cor sRGB para linear.</summary>
            public Mesh ParaMesh(string nome, bool linear = false)
            {
                var mesh = new Mesh();
                mesh.name = nome;
                if (V.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(V);
                if (linear)
                {
                    var lc = new List<Color>(C.Count);
                    for (int i = 0; i < C.Count; i++)
                    {
                        Color l = C[i].linear;
                        l.a = C[i].a;
                        lc.Add(l);
                    }
                    mesh.SetColors(lc);
                }
                else mesh.SetColors(C);
                mesh.SetTriangles(T, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }

    /// <summary>
    /// Arvores, rochas e moitas procedurais, por CELULA: cada celula e' um GameObject no SEU
    /// lugar (instancias em coordenada local) — a licao da leva 6: com todas as celulas na
    /// origem, o corte por distancia era tudo-ou-nada e so' funcionava na vertical. Aqui o
    /// frustum corta pelo bounds da celula e a distancia corta a moita a 95 m, na horizontal.
    /// Arvore e' dado VIVO: o terreno reativo queima a floresta (GDD §14) por MarcarQueimada(i).
    /// </summary>
    public sealed class Vegetacao : MonoBehaviour
    {
        /// <summary>Moita e' cobertura de combate: some so' a 95 m. Arvore e rocha nunca somem (leem-se da queda).</summary>
        public const float CorteMoitas = 95f;
        const float RaioTronco = 0.38f, AlturaTronco = 3f;

        sealed class Celula
        {
            public GameObject Go;
            public MeshFilter Mf;
            public Vector3 Centro;
            public readonly List<int> Arvores = new List<int>();
            public readonly List<int> Rochas = new List<int>();
            public readonly List<int> Moitas = new List<int>();
            public Renderer MoitasR;
            public bool MoitasLigadas = true;
        }

        Relevo relevo;
        Material mat;
        int celulas;
        float passo;
        Celula[] cels;
        readonly MalhaProc.Construtor buf = new MalhaProc.Construtor();
        MalhaProc.Construtor protoArvore, protoToco, protoRocha, protoMoita;

        readonly List<Vector3> arvPos = new List<Vector3>();
        readonly List<Matrix4x4> arvM = new List<Matrix4x4>();
        readonly List<Color> arvTinta = new List<Color>();
        readonly List<bool> arvQueimada = new List<bool>();
        readonly List<CapsuleCollider> arvColisor = new List<CapsuleCollider>();
        readonly List<Matrix4x4> rocM = new List<Matrix4x4>();
        readonly List<Matrix4x4> moiM = new List<Matrix4x4>();
        readonly List<Color> moiTinta = new List<Color>();
        float relogio;

        public int ContarArvores() => arvPos.Count;
        public Vector3 PosArvore(int i) => arvPos[i];
        public bool EstaQueimada(int i) => i >= 0 && i < arvQueimada.Count && arvQueimada[i];
        public int ContarRochas() => rocM.Count;
        public int ContarMoitas() => moiM.Count;

        /// <summary>Fogo consumiu a arvore `i`: a copa SOME, sobra o toco e o tronco deixa de colidir. `false` restaura (restart).</summary>
        public void MarcarQueimada(int i, bool queimada = true)
        {
            if (i < 0 || i >= arvPos.Count || arvQueimada[i] == queimada) return;
            arvQueimada[i] = queimada;
            if (arvColisor[i] != null) arvColisor[i].enabled = !queimada;
            RemontarCelula(CelulaDe(arvPos[i]));
        }

        public void Montar(Relevo relevo, Material mat)
        {
            this.relevo = relevo;
            this.mat = mat;
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            protoArvore = ProtoArvore();
            protoToco = ProtoToco();
            protoRocha = ProtoBlob(13, Vector3.zero, new Vector3(1f, 0.75f, 1f), Relevo.CorRocha, 0.3f);
            protoMoita = ProtoMoita();
            arvPos.Clear(); arvM.Clear(); arvTinta.Clear(); arvQueimada.Clear(); arvColisor.Clear();
            rocM.Clear(); moiM.Clear(); moiTinta.Clear();
            PlantarArvores();
            PlantarRochas();
            PlantarMoitas();
            MontarCelulas();
        }

        // ---------------------------------------------------------------- scatter

        void PlantarArvores()
        {
            var rng = new Sorteio(21);
            float area = relevo.Escala * relevo.Escala;   // densidade constante: contagem x area
            var xz = new List<Vector2>();
            int tries = 0;
            // ponytail: rejeicao O(n^2), roda uma vez no load
            while (xz.Count < (int)(150 * area) && tries < (int)(4200 * area))
            {
                tries++;
                Vector2 p = relevo.Floresta + new Vector2(rng.Faixa(-1f, 1f), rng.Faixa(-1f, 1f)) * relevo.FlorestaR;
                if (Vector2.Distance(p, relevo.Floresta) > relevo.FlorestaR) continue;
                float h = relevo.Altura(p.x, p.y);
                if (h < 1.2f || h > 7.4f) continue;
                bool ok = true;
                for (int j = 0; j < xz.Count; j++)
                    if (Vector2.Distance(xz[j], p) < 4.4f) { ok = false; break; }
                if (!ok) continue;
                xz.Add(p);
                Arvore(rng, p, h);
            }
            // arvores avulsas fora da mata fechada
            tries = 0;
            int extra = 0;
            float L = relevo.RaioTerra;
            while (extra < (int)(26 * area) && tries < (int)(2600 * area))
            {
                tries++;
                Vector2 p = new Vector2(rng.Faixa(-L, L), rng.Faixa(-L, L));
                if (p.magnitude > L * 0.84f || Vector2.Distance(p, relevo.Floresta) < relevo.FlorestaR
                    || Vector2.Distance(p, relevo.Lago) < relevo.LagoR + 6f || Vector2.Distance(p, relevo.Alagado) < relevo.AlagadoR + 4f
                    || Vector2.Distance(p, relevo.Ruinas) < relevo.RuinasR + 4f || Vector2.Distance(p, relevo.Dunas) < relevo.DunasR + 6f)
                    continue;
                float h = relevo.Altura(p.x, p.y);
                if (h < 1.2f || h > 9.5f) continue;
                Arvore(rng, p, h);
                extra++;
            }
        }

        void Arvore(Sorteio rng, Vector2 p, float h)
        {
            float s = rng.Faixa(0.75f, 1.16f);
            var pos = new Vector3(p.x, h - 0.1f, p.y);
            var rot = Quaternion.Euler(0f, rng.Faixa(0f, 360f), 0f);
            var esc = new Vector3(s, rng.Faixa(0.95f, 1.22f) * s, s);
            arvPos.Add(pos);
            arvM.Add(Matrix4x4.TRS(pos, rot, esc));
            // tinta por arvore: quebra a repeticao (umas puxam pro amarelo-sol, outras pro verde-frio)
            float v = rng.Faixa(-0.15f, 0.15f);
            arvTinta.Add(new Color(1f + v * 1.5f, 1f + v * 0.5f, 1f - v * 0.9f));
            arvQueimada.Add(false);
            arvColisor.Add(null);
        }

        void PlantarRochas()
        {
            var rng = new Sorteio(51);
            float area = relevo.Escala * relevo.Escala;
            float L = relevo.RaioTerra;
            int tries = 0;
            while (rocM.Count < (int)(58 * area) && tries < (int)(3200 * area))
            {
                tries++;
                Vector2 p = new Vector2(rng.Faixa(-L, L), rng.Faixa(-L, L));
                if (p.magnitude > L - 4f || Vector2.Distance(p, relevo.Lago) < relevo.LagoR + 3f
                    || Vector2.Distance(p, relevo.Alagado) < relevo.AlagadoR) continue;
                float h = relevo.Altura(p.x, p.y);
                bool morro = h > 7f;                       // encosta alta e cume: silhueta recortada
                bool praia = h > 0.25f && h < 1f;
                if (!(morro || praia)) continue;
                float s = rng.Faixa(0.5f, 2.2f);
                rocM.Add(Matrix4x4.TRS(new Vector3(p.x, h + 0.1f, p.y), Quaternion.Euler(0f, rng.Faixa(0f, 360f), 0f),
                    new Vector3(s, s * 0.75f, s)));
            }
        }

        void PlantarMoitas()
        {
            var rng = new Sorteio(101);
            float area = relevo.Escala * relevo.Escala;
            float L = relevo.RaioTerra;
            int tries = 0;
            while (moiM.Count < (int)(260 * area) && tries < (int)(9000 * area))
            {
                tries++;
                Vector2 p;
                if (rng.Float() < 0.72f)
                {
                    float a = rng.Faixa(0f, Mathf.PI * 2f);
                    p = relevo.Floresta + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (Mathf.Sqrt(rng.Float()) * (relevo.FlorestaR + 9f));
                }
                else p = new Vector2(rng.Faixa(-L, L), rng.Faixa(-L, L));
                float h = ChaoAberto(p, 1.15f, 12f, 0.7f);
                if (h < 0f) continue;
                float sc = rng.Faixa(0.62f, 1.35f);
                moiM.Add(Matrix4x4.TRS(new Vector3(p.x, h - 0.12f, p.y), Quaternion.Euler(0f, rng.Faixa(0f, 360f), 0f),
                    new Vector3(sc, rng.Faixa(0.7f, 1.15f) * sc, sc)));
                float v = rng.Faixa(-0.14f, 0.14f);
                moiTinta.Add(new Color(1f - v * 0.5f, 1f + v, 1f + v * 0.7f));
            }
        }

        /// <summary>Campina aberta, fora d'agua, fora de ladeira, fora do areal e do piso das ruinas. -1 = nao.</summary>
        float ChaoAberto(Vector2 p, float hmin, float hmax, float nymin)
        {
            if (p.magnitude > relevo.RaioTerra - 6f || Vector2.Distance(p, relevo.Lago) < relevo.LagoR + 2f
                || Vector2.Distance(p, relevo.Alagado) < relevo.AlagadoR) return -1f;
            if (Vector2.Distance(p, relevo.Dunas) < relevo.DunasR * 0.85f) return -1f;
            if (Vector2.Distance(p, relevo.Ruinas) < relevo.RuinasR * 0.8f) return -1f;
            float h = relevo.Altura(p.x, p.y);
            if (h < hmin || h > hmax || relevo.NormalY(p.x, p.y) < nymin) return -1f;
            return h;
        }

        // ---------------------------------------------------------------- celulas

        int CelulaDe(Vector3 p)
        {
            float meio = relevo.Lado * 0.5f;
            int cx = Mathf.Clamp((int)((p.x + meio) / passo), 0, celulas - 1);
            int cz = Mathf.Clamp((int)((p.z + meio) / passo), 0, celulas - 1);
            return cz * celulas + cx;
        }

        void MontarCelulas()
        {
            // A grade escala com o mapa para a celula continuar com ~30 m (6-9 no frustum de 3a pessoa).
            celulas = Mathf.Max(1, (int)(10 * relevo.Escala));
            passo = relevo.Lado / celulas;
            float meio = relevo.Lado * 0.5f;
            cels = new Celula[celulas * celulas];
            for (int i = 0; i < arvPos.Count; i++) Cel(CelulaDe(arvPos[i])).Arvores.Add(i);
            for (int i = 0; i < rocM.Count; i++) Cel(CelulaDe(rocM[i].GetColumn(3))).Rochas.Add(i);
            for (int i = 0; i < moiM.Count; i++) Cel(CelulaDe(moiM[i].GetColumn(3))).Moitas.Add(i);
            for (int k = 0; k < cels.Length; k++)
            {
                Celula c = cels[k];
                if (c == null) continue;
                c.Centro = new Vector3((k % celulas + 0.5f) * passo - meio, 0f, (k / celulas + 0.5f) * passo - meio);
                c.Go = new GameObject("Celula" + k.ToString("00"));
                c.Go.transform.SetParent(transform, false);
                c.Go.transform.localPosition = c.Centro;      // CADA CELULA NO SEU LUGAR
                c.Mf = c.Go.AddComponent<MeshFilter>();
                var mr = c.Go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                // colisao: tronco = capsula por arvore; rocha grande = esfera. Todas no GameObject da celula.
                for (int j = 0; j < c.Arvores.Count; j++)
                {
                    int i = c.Arvores[j];
                    var cc = c.Go.AddComponent<CapsuleCollider>();
                    cc.radius = RaioTronco;
                    cc.height = AlturaTronco;
                    cc.direction = 1;
                    cc.center = arvPos[i] - c.Centro + new Vector3(0f, AlturaTronco * 0.5f, 0f);
                    arvColisor[i] = cc;
                }
                for (int j = 0; j < c.Rochas.Count; j++)
                {
                    Matrix4x4 m = rocM[c.Rochas[j]];
                    float s = m.GetColumn(0).magnitude;
                    if (s <= 1.4f) continue;
                    var sc = c.Go.AddComponent<SphereCollider>();
                    sc.radius = s * 0.8f;
                    Vector3 p = m.GetColumn(3);
                    sc.center = p - c.Centro + new Vector3(0f, s * 0.2f, 0f);
                }
                if (c.Moitas.Count > 0)
                {
                    var mgo = new GameObject("Moitas");
                    mgo.transform.SetParent(c.Go.transform, false);
                    var mmf = mgo.AddComponent<MeshFilter>();
                    var mmr = mgo.AddComponent<MeshRenderer>();
                    mmr.sharedMaterial = mat;
                    mmr.shadowCastingMode = ShadowCastingMode.Off;   // moita nao paga sombra
                    c.MoitasR = mmr;
                    buf.Limpar();
                    for (int j = 0; j < c.Moitas.Count; j++)
                    {
                        int i = c.Moitas[j];
                        Matrix4x4 m = moiM[i];
                        m.SetColumn(3, m.GetColumn(3) - new Vector4(c.Centro.x, c.Centro.y, c.Centro.z, 0f));
                        buf.Adicionar(protoMoita, m, moiTinta[i]);
                    }
                    mmf.sharedMesh = buf.ParaMesh("Moitas" + k, Linear());
                }
                RemontarCelula(k);
            }
        }

        Celula Cel(int k)
        {
            if (cels[k] == null) cels[k] = new Celula();
            return cels[k];
        }

        /// <summary>Arvores (ou tocos, se queimadas) + rochas da celula numa malha so', em coordenada local.</summary>
        void RemontarCelula(int k)
        {
            Celula c = cels != null && k >= 0 && k < cels.Length ? cels[k] : null;
            if (c == null || c.Mf == null) return;
            buf.Limpar();
            Vector4 off = new Vector4(c.Centro.x, c.Centro.y, c.Centro.z, 0f);
            for (int j = 0; j < c.Arvores.Count; j++)
            {
                int i = c.Arvores[j];
                if (arvQueimada[i])
                {
                    // toco de carvao no pe' da arvore, giro proprio por indice
                    var m = Matrix4x4.TRS(arvPos[i] - c.Centro, Quaternion.Euler(0f, i * 137.5f, 0f), Vector3.one);
                    buf.Adicionar(protoToco, m, Color.white);
                }
                else
                {
                    Matrix4x4 m = arvM[i];
                    m.SetColumn(3, m.GetColumn(3) - off);
                    buf.Adicionar(protoArvore, m, arvTinta[i]);
                }
            }
            for (int j = 0; j < c.Rochas.Count; j++)
            {
                Matrix4x4 m = rocM[c.Rochas[j]];
                m.SetColumn(3, m.GetColumn(3) - off);
                buf.Adicionar(protoRocha, m, Color.white);
            }
            Mesh velha = c.Mf.sharedMesh;
            c.Mf.sharedMesh = buf.ParaMesh("Celula" + k, Linear());
            if (velha != null) Destroy(velha);
        }

        static bool Linear() => QualitySettings.activeColorSpace == ColorSpace.Linear;

        /// <summary>
        /// Corte por distancia da MOITA, na horizontal, por celula. 4 Hz: uma distancia por celula,
        /// nada por instancia. Arvore e rocha ficam com o frustum (bounds da propria celula).
        /// </summary>
        void Update()
        {
            relogio += Time.deltaTime;
            if (relogio < 0.25f || cels == null) return;
            relogio = 0f;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 olho = cam.transform.position;
            float corte2 = CorteMoitas * CorteMoitas;
            for (int k = 0; k < cels.Length; k++)
            {
                Celula c = cels[k];
                if (c == null || c.MoitasR == null) continue;
                Vector3 centro = transform.TransformPoint(c.Centro);
                bool liga = (olho - centro).sqrMagnitude < corte2;
                if (liga != c.MoitasLigadas)
                {
                    c.MoitasLigadas = liga;
                    c.MoitasR.enabled = liga;
                }
            }
        }

        // ---------------------------------------------------------------- prototipos

        static MalhaProc.Construtor ProtoArvore()
        {
            var b = new MalhaProc.Construtor();
            var rng = new Sorteio(11);
            b.Tronco(Vector3.zero, 0.32f, 0.22f, 3.35f, 5, Relevo.CorTronco, Relevo.Clarear(Relevo.CorTronco, 0.12f));
            b.Blob(new Vector3(0f, 4.35f, 0f), new Vector3(1.9f, 1.35f, 1.9f), Relevo.CorFolhaA, rng, 0.20f);
            b.Blob(new Vector3(0.62f, 5.05f, 0.35f), new Vector3(1.08f, 0.82f, 1.08f), Relevo.CorFolhaB, rng, 0.18f);
            return b;
        }

        static MalhaProc.Construtor ProtoToco()
        {
            var b = new MalhaProc.Construtor();
            Color carvao = new Color(0.13f, 0.11f, 0.10f);
            b.Tronco(Vector3.zero, 0.34f, 0.2f, 0.9f, 5, carvao);
            b.Tampa(new Vector3(0f, 0.9f, 0f), 0.2f, 5, new Color(0.24f, 0.14f, 0.09f));   // miolo de brasa apagada
            return b;
        }

        static MalhaProc.Construtor ProtoBlob(int seed, Vector3 centro, Vector3 escala, Color col, float jit)
        {
            var b = new MalhaProc.Construtor();
            b.Blob(centro, escala, col, new Sorteio(seed), jit);
            return b;
        }

        static MalhaProc.Construtor ProtoMoita()
        {
            var b = new MalhaProc.Construtor();
            var rng = new Sorteio(23);
            b.Blob(new Vector3(0f, 0.42f, 0f), new Vector3(0.72f, 0.5f, 0.72f), Relevo.Escurecer(Relevo.CorFolhaA, 0.18f), rng, 0.28f);
            b.Blob(new Vector3(0.34f, 0.62f, -0.2f), new Vector3(0.42f, 0.34f, 0.42f), Relevo.Escurecer(Relevo.CorFolhaB, 0.1f), rng, 0.26f);
            return b;
        }
    }
}
