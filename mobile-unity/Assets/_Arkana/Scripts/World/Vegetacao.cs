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

            /// <summary>Blob icosaedrico facetado com jitter — copa de arvore, rocha, moita. A cor desce em DEGRADE por vertice:
            /// escura embaixo (a massa faz sombra em si mesma), clara em cima (o sol). Com cor chapada a copa lia como pirulito
            /// e a rocha como bloco solto (fotos 14 e 15 de 12/09); o degrade custa zero triangulo.</summary>
            public void Blob(Vector3 centro, Vector3 escala, Color col, Sorteio rng, float jit)
            {
                var vs = new Vector3[IcoV.Length];
                for (int i = 0; i < IcoV.Length; i++)
                {
                    float f = 1f + rng.Faixa(-jit, jit * 1.2f);
                    Vector3 n = IcoV[i].normalized;
                    vs[i] = centro + new Vector3(n.x * f * escala.x, n.y * f * escala.y, n.z * f * escala.z);
                }
                Color baixo = Relevo.Escurecer(col, 0.38f), alto = Relevo.Clarear(col, 0.10f);   // KNOB: por foto
                Color Degrade(Vector3 p) => Color.Lerp(baixo, alto, Mathf.InverseLerp(centro.y - escala.y, centro.y + escala.y, p.y));
                for (int f = 0; f < IcoF.Length; f += 3)
                {
                    Vector3 a = vs[IcoF[f]], b = vs[IcoF[f + 1]], c = vs[IcoF[f + 2]];
                    TriCores(a, b, c, Degrade(a), Degrade(b), Degrade(c), (a + b + c) / 3f - centro);
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
    /// PEDREGULHO (onda 5B): a rocha da Meshy no lugar do blob marrom ("piramide de papelao" na foto 02), um
    /// GameObject por pedra no load, filho da celula; sem o .glb, volta o blob na malha da celula.
    /// </summary>
    public sealed class Vegetacao : MonoBehaviour
    {
        /// <summary>Moita e' cobertura de combate: some so' a 95 m. Arvore e rocha nunca somem (leem-se da queda).</summary>
        public const float CorteMoitas = 95f;
        const float RaioTronco = 0.38f, AlturaTronco = 3f;
        /// <summary>A rocha dos pedregulhos: a 18 (Emberstone Outcrop) SOLDADA e decimada a 1K tris. Sem ela, a de 3K dos
        /// rochedos do mar (Ruinas.RochaDoMar); sem as duas, o blob.</summary>
        public const string RochaDoPedregulho = "18-pedregulho";

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
        readonly List<Matrix4x4> rocVis = new List<Matrix4x4>();   // a matriz do MOLDE da Meshy, por rocha (vazia = blob)
        readonly List<Matrix4x4> moiM = new List<Matrix4x4>();
        readonly List<Color> moiTinta = new List<Color>();
        GameObject rocha;
        Bounds moldeRocha;
        static readonly Material[] matRocha = new Material[9];   // um por Bioma: 6 materiais para as 232 pedras (SRP Batcher)
        float relogio;

        public int ContarArvores() => arvPos.Count;
        public Vector3 PosArvore(int i) => arvPos[i];
        public bool EstaQueimada(int i) => i >= 0 && i < arvQueimada.Count && arvQueimada[i];
        public int ContarRochas() => rocM.Count;
        public int ContarMoitas() => moiM.Count;
        /// <summary>De onde saiu a pedra: RochaDoPedregulho, Ruinas.RochaDoMar ou "blob" (diag da foto).</summary>
        public string MoldeDasRochas { get; private set; }

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
            protoRocha = ProtoBlob(13, Vector3.zero, new Vector3(1f, 0.75f, 1f), Relevo.CorPedregulho, 0.3f);
            protoMoita = ProtoMoita();
            rocha = MoldeDaRocha(out moldeRocha);
            MoldeDasRochas = rocha != null ? rocha.name : "blob";
            arvPos.Clear(); arvM.Clear(); arvTinta.Clear(); arvQueimada.Clear(); arvColisor.Clear();
            rocM.Clear(); rocVis.Clear(); moiM.Clear(); moiTinta.Clear();
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
            rocM.AddRange(PlantioDasRochas(relevo));
            if (rocha == null) return;
            // sorteio proprio: a variacao da pedra nao mexe no lugar de nenhuma (o rng 51 segue o de sempre)
            var rng = new Sorteio(52);
            for (int i = 0; i < rocM.Count; i++) rocVis.Add(AssentarRocha(relevo, rocM[i], moldeRocha, rng));
        }

        /// <summary>
        /// Os pedregulhos da ilha, PURO por seed: a matriz do blob (centro em h + 0,1, giro em Y, escala (s, 0,75 s, s)).
        /// E' ela que manda no colisor (esfera de 0,8 s acima de s = 1,4) e na celula; a rocha da Meshy so' veste.
        /// </summary>
        public static List<Matrix4x4> PlantioDasRochas(Relevo relevo)
        {
            var lista = new List<Matrix4x4>();
            var rng = new Sorteio(51);
            float area = relevo.Escala * relevo.Escala;
            float L = relevo.RaioTerra;
            int tries = 0;
            while (lista.Count < (int)(58 * area) && tries < (int)(3200 * area))
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
                lista.Add(Matrix4x4.TRS(new Vector3(p.x, h + 0.1f, p.y), Quaternion.Euler(0f, rng.Faixa(0f, 360f), 0f),
                    new Vector3(s, s * 0.75f, s)));
            }
            return lista;
        }

        /// <summary>
        /// A ROCHA DA MESHY assentada no lugar do blob, PURA: a matriz do molde (`molde` = limites do .glb na origem) a partir
        /// da matriz do blob `rocha`. Ocupa a CAIXA do blob (largura 2 s, altura 1,5 x 0,75 s — a conta dos rochedos do mar),
        /// variando por eixo; a base deita 70% na encosta larga (+ ate' ~5 graus sorteados) e TODA a base (3x3 pontos) fica
        /// `afunda` abaixo do chao DESENHADO. Pousada pelo centro, a pedra da encosta mostra fresta sob a quina de baixo.
        /// </summary>
        public static Matrix4x4 AssentarRocha(Relevo relevo, Matrix4x4 rocha, Bounds molde, Sorteio rng)
        {
            Vector3 pos = rocha.GetColumn(3);
            float s = rocha.GetColumn(0).magnitude;
            // KNOB: variacao por eixo (+-15% na largura, +-20% na altura), afundamento e tombo, por foto
            var caixa = new Vector3(2f * s * rng.Faixa(0.85f, 1.15f), 1.5f * rocha.GetColumn(1).magnitude * rng.Faixa(0.8f, 1.2f),
                2f * s * rng.Faixa(0.85f, 1.15f));
            float afunda = caixa.y * rng.Faixa(0.15f, 0.35f);
            var tombo = new Vector3(rng.Faixa(-0.08f, 0.08f), 0f, rng.Faixa(-0.08f, 0.08f));

            // a encosta pela PEGADA inteira (+-s), nao pela normal do vertice, que pula nas cristas do pico
            float dx = ChaoDesenhado(relevo, pos.x - s, pos.z) - ChaoDesenhado(relevo, pos.x + s, pos.z);
            float dz = ChaoDesenhado(relevo, pos.x, pos.z - s) - ChaoDesenhado(relevo, pos.x, pos.z + s);
            Vector3 cima = (Vector3.Lerp(Vector3.up, new Vector3(dx, 2f * s, dz).normalized, 0.7f) + tombo).normalized;
            Vector3 lado = Vector3.Cross(cima, rocha.GetColumn(2)).normalized;   // o giro em Y do blob continua mandando
            Vector3 frente = Vector3.Cross(lado, cima);
            Matrix4x4 rs = new Matrix4x4(lado, cima, frente, new Vector4(0f, 0f, 0f, 1f))
                * Matrix4x4.Scale(new Vector3(caixa.x / molde.size.x, caixa.y / molde.size.y, caixa.z / molde.size.z));

            // o centro do molde cai no lugar do blob; a altura sai da base: o ponto menos enterrado fica `afunda` abaixo
            Vector3 c = rs.MultiplyPoint3x4(molde.center);
            float x0 = pos.x - c.x, z0 = pos.z - c.z, y0 = float.MaxValue;
            for (int i = 0; i < 3; i++)
                for (int k = 0; k < 3; k++)
                {
                    Vector3 o = rs.MultiplyPoint3x4(new Vector3(Mathf.Lerp(molde.min.x, molde.max.x, i * 0.5f), molde.min.y,
                        Mathf.Lerp(molde.min.z, molde.max.z, k * 0.5f)));
                    y0 = Mathf.Min(y0, ChaoDesenhado(relevo, x0 + o.x, z0 + o.z) - o.y);
                }
            return Matrix4x4.Translate(new Vector3(x0, y0 - afunda, z0)) * rs;
        }

        /// <summary>Altura do chao DESENHADO (a malha de Ilha.Quads, a divisao de triangulo da Ilha, que o GradeDoChao repete)
        /// sem montar a grade: 4 Altura() por consulta. Nos sitios das rochas a Altura() exata passa ate' 0,23 m da malha (medido).</summary>
        static float ChaoDesenhado(Relevo r, float x, float z)
        {
            int q = Ilha.Quads;
            float fx = Mathf.Clamp((x / r.Lado + 0.5f) * q, 0f, q - 1e-4f), fz = Mathf.Clamp((z / r.Lado + 0.5f) * q, 0f, q - 1e-4f);
            int ix = (int)fx, iz = (int)fz;
            float u = fx - ix, v = fz - iz;
            float xa = ((float)ix / q - 0.5f) * r.Lado, xb = ((float)(ix + 1) / q - 0.5f) * r.Lado;
            float za = ((float)iz / q - 0.5f) * r.Lado, zc = ((float)(iz + 1) / q - 0.5f) * r.Lado;
            float ha = r.Altura(xa, za), hb = r.Altura(xb, za), hc = r.Altura(xa, zc), hd = r.Altura(xb, zc);
            if (u + v <= 1f) return ha + (hb - ha) * u + (hc - ha) * v;
            return hd + (hc - hd) * (1f - u) + (hb - hd) * (1f - v);
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
                    if (rocha != null) Pedregulho(c.Go.transform, c.Rochas[j]);
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

        /// <summary>A rocha `i` da Meshy, filha da celula: nasce no load (nunca por evento), com o material do bioma dela.</summary>
        void Pedregulho(Transform celula, int i)
        {
            Matrix4x4 m = rocVis[i];
            GameObject go = Instantiate(rocha, celula);
            go.name = "Pedregulho";
            go.transform.SetPositionAndRotation(m.GetColumn(3), Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1)));
            go.transform.localScale = Vector3.Scale(go.transform.localScale,
                new Vector3(m.GetColumn(0).magnitude, m.GetColumn(1).magnitude, m.GetColumn(2).magnitude));
            Vector3 p = rocM[i].GetColumn(3);
            Bioma b = relevo.BiomaEm(p.x, p.z);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                Material tom = MaterialDaRocha(b, r.sharedMaterial);
                if (tom != null) r.sharedMaterial = tom;
                r.shadowCastingMode = ShadowCastingMode.On;   // o blob fazia sombra (malha da celula); a pedra continua fazendo
            }
        }

        /// <summary>
        /// O material da Meshy com o TOM DO BIOMA (multiplica a textura, que e' o basalto quase preto do kit: mediana 0,11)
        /// e fosco: o 0,5 de rugosidade do .glb deixa pedra com cara de plastico, o kit usa 0,25 de liso (KitCenario.Domado).
        /// Copia do importado — nunca altera o asset. KNOB: os tons, por foto.
        /// </summary>
        static Material MaterialDaRocha(Bioma b, Material original)
        {
            if (original == null) return null;
            // ponytail: cache por bioma, nao por original — um material por .glb (a Meshy entrega um); peca multi-material pediria a chave dupla
            if (matRocha[(int)b] != null) return matRocha[(int)b];
            var m = new Material(original) { name = "Pedregulho" + b };
            Color tom;
            switch (b)
            {
                case Bioma.Pico: tom = new Color(1.3f, 1.36f, 1.48f); break;     // cume frio e claro: pedra preta na tampa branca vira buraco
                case Bioma.Praia: case Bioma.Dunas: tom = new Color(1.5f, 1.36f, 1.12f); break;   // lavada de sol e de sal
                case Bioma.Campina: case Bioma.Floresta: tom = new Color(1.12f, 1.32f, 0.98f); break;   // musgo
                case Bioma.Ruinas: tom = new Color(1.36f, 1.3f, 1.2f); break;   // o cinza quente das colunas (CorRuina)
                default: tom = new Color(1.25f, 1.25f, 1.25f); break;             // encosta: o basalto do kit, um tom acima
            }
            if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", tom);   // glTFast
            else if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tom);
            if (m.HasProperty("roughnessFactor")) m.SetFloat("roughnessFactor", 0.75f);
            else if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            return matRocha[(int)b] = m;
        }

        /// <summary>O .glb da rocha e os limites dele na origem; null = nenhum dos dois em Resources (fica o blob).</summary>
        static GameObject MoldeDaRocha(out Bounds molde)
        {
            foreach (string nome in new[] { RochaDoPedregulho, Ruinas.RochaDoMar })
            {
                GameObject g = Resources.Load<GameObject>(nome);
                if (g == null) continue;
                molde = Ruinas.Limites(g);
                if (molde.size.x > 0.001f && molde.size.y > 0.001f && molde.size.z > 0.001f) return g;
            }
            molde = new Bounds();
            return null;
        }

        /// <summary>Arvores (ou tocos, se queimadas) + rochas-blob da celula numa malha so', em coordenada local.</summary>
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
            for (int j = 0; j < c.Rochas.Count && rocha == null; j++)
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
            // tronco escurecido e puxado pro cinza (o CorTronco puro, no sol do entardecer e com o pos saturando, saia laranja)
            // e copa em CACHO: a massa grande e tres menores em volta — duas pecas redondas liam como pirulito (foto 15 de
            // 12/09). ~90 triangulos por arvore.
            Color tronco = new Color(0.34f, 0.29f, 0.25f);   // casca escura: o sol quente (1; 0,86; 0,63 x 1,55) ja' doura
            b.Tronco(Vector3.zero, 0.32f, 0.22f, 3.35f, 5, Relevo.Escurecer(tronco, 0.2f), tronco);
            b.Blob(new Vector3(0f, 4.3f, 0f), new Vector3(1.8f, 1.3f, 1.8f), Relevo.CorFolhaA, rng, 0.20f);
            b.Blob(new Vector3(1.0f, 4.0f, 0.45f), new Vector3(1.05f, 0.85f, 1.05f), Relevo.CorFolhaA, rng, 0.18f);
            b.Blob(new Vector3(-0.85f, 4.1f, -0.6f), new Vector3(1.0f, 0.8f, 1.0f), Relevo.CorFolhaB, rng, 0.18f);
            b.Blob(new Vector3(0.25f, 5.2f, 0.15f), new Vector3(1.05f, 0.8f, 1.05f), Relevo.CorFolhaB, rng, 0.18f);
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
