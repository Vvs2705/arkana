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
    /// ARVORE DA MESHY (onda 6B): o cacho de 4 blobs lia como "brinquedo de blocos" ao lado da pedra texturizada. Copa
    /// larga (campina, avulsas e 1/3 da mata) e pinheiro (2/3 da mata), desenhados como a Grama: RenderMeshInstanced por
    /// BLOCO de 60 m, um lote por especie x tinta x LOD, nenhum GameObject por arvore, LOD por arvore a 4 Hz. Os lugares,
    /// o colisor do tronco e o registro (PosArvore) sao os de sempre. Queimada: o toco procedural na malha da celula.
    /// MOITA DA MESHY (onda 6B): a mesma receita por celula; perto do olho (AlcanceMoitaMeshy) a de 1,2K, dali ao CorteMoitas a de 428 tris.
    /// Sem os .glb (ou sem instancing, no -nographics), volta a procedural na malha da celula.
    /// </summary>
    public sealed class Vegetacao : MonoBehaviour
    {
        /// <summary>Moita e' cobertura de combate: some so' a 95 m. Arvore e rocha nunca somem (leem-se da queda).</summary>
        public const float CorteMoitas = 95f;
        const float RaioTronco = 0.38f, AlturaTronco = 3f;
        /// <summary>A rocha dos pedregulhos: a 18 (Emberstone Outcrop) SOLDADA e decimada a 1K tris. Sem ela, a de 3K dos
        /// rochedos do mar (Ruinas.RochaDoMar); sem as duas, o blob.</summary>
        public const string RochaDoPedregulho = "18-pedregulho";

        /// <summary>As arvores da Meshy em Resources (1,5K tris; o LOD1 e' o mesmo nome + "-lod1", ~600 tris).</summary>
        public const string ArvoreCopa = "35-arvore-copa", ArvorePinheiro = "36-pinheiro";
        /// <summary>Distancia 3D do olho a' arvore em que ela passa para o LOD1 (m). Nunca some: le'-se da queda.</summary>
        public const float DistanciaLod1 = 40f;   // KNOB: 35-45 m, por foto e FPS
        /// <summary>Quanto da mata fechada e' pinheiro; o resto e' copa larga. A previa de 12/09 leu como mata com 2 de 3.</summary>
        public const float PinheirosNaMata = 0.65f;   // KNOB
        /// <summary>Tintas por especie (base, sol, sombra). Cada uma e' um material e um lote a mais por bloco.</summary>
        public const int Variantes = 3;
        /// <summary>Inclinacao maxima da arvore da Meshy (graus, em X e em Z): tudo a prumo le' como plantacao.</summary>
        public const float Inclinacao = 4f;   // KNOB
        /// <summary>Quanto o pe' da Meshy desce abaixo do chao mais baixo da pegada (m): raiz boiando na encosta le' como defeito.</summary>
        const float AfundaArvore = 0.08f;
        /// <summary>Lado do BLOCO de desenho da Meshy (m). Com a celula de 30 m, o voo via ~250 lotes; com 60, ~metade.</summary>
        const float PassoBloco = 60f;   // KNOB
        const float Periodo = 0.25f;    // 4 Hz: corte da moita e LOD da arvore

        /// <summary>
        /// A TINTA da Meshy (multiplica a textura, sRGB), por especie x variante (base, sol, sombra). O pinheiro chega TEAL
        /// puro (folha media 0,05/0,32/0,33): na sombra fria do entardecer virava arvore de natal azul (previa de 12/09); o
        /// azul cai ~30% e ele vira verde-abeto. A copa (oliva 0,31/0,43/0,20) ganha um pouco de verde. KNOB: por foto.
        /// </summary>
        static readonly Color[,] TintaMeshy =
        {
            { new Color(1f, 1.06f, 0.92f), new Color(1.1f, 1.12f, 0.84f), new Color(0.88f, 0.98f, 0.94f) },     // copa
            { new Color(1.14f, 1f, 0.72f), new Color(1.24f, 1.06f, 0.68f), new Color(1.02f, 0.94f, 0.8f) },      // pinheiro
        };

        /// <summary>A moita da Meshy (1,2K tris, 2,2 x 1,6 m). O LOD1 ("-lod1", 428 tris) e' OUTRO remesh do site: a decimacao
        /// no Blender parava em ~1.150 tris (a malha soldada tem 385 vertices e UV em retalhos — 3,2K vertices na GPU).</summary>
        public const string MoitaMeshy = "37-moita";
        /// <summary>
        /// Ate' onde a moita e' a da Meshy de 1,2K (m, do olho ao centro da celula); dali ate' o CorteMoitas, a de 428 tris
        /// (sem ela, a procedural). A de 1,2K ate' 95 m custaria ~400K tris na tela da mata (sonda de 12/09); ate' 45 m, ~80 moitas.
        /// </summary>
        public const float AlcanceMoitaMeshy = 45f;
        /// <summary>A moita da Meshy chega verde-limao (folha 0,27/0,49/0,12): a tinta aprofunda o verde. Campina, mata. KNOB.</summary>
        static readonly Color[] TintaMoita = { new Color(0.74f, 0.86f, 1.04f), new Color(0.64f, 0.78f, 1.06f) };

        public enum Especie { Copa, Pinheiro }

        /// <summary>Uma arvore do plantio, PURA: onde ela esta' (o registro que o terreno reativo e o colisor usam), a procedural
        /// de reserva e a da Meshy (pe' na malha desenhada, giro com inclinacao, escala — sem o molde do .glb).</summary>
        public struct ArvorePlantada
        {
            public Vector3 Pos;
            public Matrix4x4 Proc;
            public Color Tinta;
            public Especie Especie;
            public int Variante;
            public Matrix4x4 Meshy;
        }

        /// <summary>Um lote instanciado: as matrizes de UMA malha (especie x LOD) com UMA tinta, num bloco. So' o N muda (4 Hz).</summary>
        sealed class Lote
        {
            public Matrix4x4[] M;
            public int N;
        }

        /// <summary>Bloco de desenho da Meshy: a caixa que o frustum corta e um lote por especie x variante x LOD.</summary>
        sealed class Bloco
        {
            public Bounds Caixa;
            public readonly List<int> Arvores = new List<int>();
            public readonly Lote[] Lotes = new Lote[2 * Variantes * 2];
        }

        sealed class Celula
        {
            public GameObject Go;
            public MeshFilter Mf;
            public MeshRenderer Mr;
            public Vector3 Centro;
            public readonly List<int> Arvores = new List<int>();
            public readonly List<int> Rochas = new List<int>();
            public readonly List<int> Moitas = new List<int>();
            public Renderer MoitasR;
            public bool MoitasLigadas = true;
            // a moita da Meshy da celula (fixa: sem LOD, sem fogo) e se o olho esta' perto o bastante para ela
            public Lote[] MoitasMeshy;
            public Bounds CaixaMoitas;
            public bool MoitasPerto, MoitasMedio;   // Meshy de perto (LOD0) / Meshy leve ate' o CorteMoitas (LOD1)
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
        float relogio = Periodo;

        // a arvore da Meshy: especie, tinta e matriz (ja' com o molde) por arvore; malha [especie * 2 + lod]; null = procedural
        readonly List<Especie> arvEsp = new List<Especie>();
        readonly List<int> arvVar = new List<int>();
        readonly List<Matrix4x4> arvVis = new List<Matrix4x4>();
        readonly List<Bloco> blocos = new List<Bloco>();
        readonly Matrix4x4[] moldeArvore = new Matrix4x4[2];
        readonly int[] trisArvore = new int[4];
        Mesh[] malhaArvore;
        static readonly Material[] matArvore = new Material[2 * Variantes];
        Camera cameraDoLod;
        // a moita da Meshy: matriz (com o molde) e tinta (0 campina, 1 mata) por moita; null = so' a procedural
        readonly List<Matrix4x4> moiVis = new List<Matrix4x4>();
        readonly List<int> moiVar = new List<int>();
        Mesh malhaMoita, malhaMoitaLod1;
        Matrix4x4 moldeMoita;
        static readonly Material[] matMoita = new Material[2], matMoitaLod1 = new Material[2];

        public int ContarArvores() => arvPos.Count;
        public Vector3 PosArvore(int i) => arvPos[i];
        public bool EstaQueimada(int i) => i >= 0 && i < arvQueimada.Count && arvQueimada[i];
        public Especie EspecieDe(int i) => arvEsp[i];
        public int ContarRochas() => rocM.Count;
        public int ContarMoitas() => moiM.Count;
        /// <summary>De onde saiu a pedra: RochaDoPedregulho, Ruinas.RochaDoMar ou "blob" (diag da foto).</summary>
        public string MoldeDasRochas { get; private set; }
        /// <summary>De onde saiu a arvore: "35-arvore-copa + 36-pinheiro" ou "procedural" (diag da foto).</summary>
        public string MoldeDasArvores { get; private set; }
        /// <summary>De onde saiu a moita perto do olho: MoitaMeshy ou "procedural" (diag da foto).</summary>
        public string MoldeDasMoitas { get; private set; }
        /// <summary>Do ultimo corte (4 Hz), para o diag: lotes mandados por quadro e triangulos ANTES do frustum, e arvores em LOD0.</summary>
        public int LotesEnviados { get; private set; }
        public int TrisEnviados { get; private set; }
        public int ArvoresPerto { get; private set; }

        /// <summary>A camera que manda no LOD da arvore e no corte da moita; null = Camera.main. A foto aponta a dela.</summary>
        public Camera Olho
        {
            get => cameraDoLod;
            set { cameraDoLod = value; relogio = Periodo; }
        }

        /// <summary>A arvore em `arvore` vista de `olho` vai no LOD1? Distancia 3D: do castelo (320 m) a mata inteira e' LOD1.</summary>
        public static bool Lod1(Vector3 olho, Vector3 arvore) => (olho - arvore).sqrMagnitude > DistanciaLod1 * DistanciaLod1;

        /// <summary>Fogo consumiu a arvore `i`: a copa SOME, sobra o toco e o tronco deixa de colidir. `false` restaura (restart).</summary>
        public void MarcarQueimada(int i, bool queimada = true)
        {
            if (i < 0 || i >= arvPos.Count || arvQueimada[i] == queimada) return;
            arvQueimada[i] = queimada;
            if (arvColisor[i] != null) arvColisor[i].enabled = !queimada;
            RemontarCelula(CelulaDe(arvPos[i]));
            relogio = Periodo;   // a arvore da Meshy sai (ou volta) dos lotes no proximo quadro, junto com o toco
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
            malhaArvore = MalhasDasArvores(moldeArvore, trisArvore);
            MoldeDasArvores = malhaArvore != null ? ArvoreCopa + " + " + ArvorePinheiro : "procedural";
            malhaMoita = MalhaDaMoita(out moldeMoita, out malhaMoitaLod1);
            MoldeDasMoitas = malhaMoita != null ? MoitaMeshy + (malhaMoitaLod1 != null ? " + lod1" : "") : "procedural";
            arvPos.Clear(); arvM.Clear(); arvTinta.Clear(); arvQueimada.Clear(); arvColisor.Clear();
            arvEsp.Clear(); arvVar.Clear(); arvVis.Clear(); blocos.Clear();
            rocM.Clear(); rocVis.Clear(); moiM.Clear(); moiTinta.Clear(); moiVis.Clear(); moiVar.Clear();
            PlantarArvores();
            PlantarRochas();
            PlantarMoitas();
            MontarCelulas();
            MontarBlocos();
            relogio = Periodo;   // o primeiro Update ja' enche os lotes
        }

        // ---------------------------------------------------------------- scatter

        void PlantarArvores()
        {
            foreach (ArvorePlantada a in PlantioDasArvores(relevo))
            {
                arvPos.Add(a.Pos);
                arvM.Add(a.Proc);
                arvTinta.Add(a.Tinta);
                arvEsp.Add(a.Especie);
                arvVar.Add(a.Variante);
                arvVis.Add(a.Meshy * moldeArvore[(int)a.Especie]);
                arvQueimada.Add(false);
                arvColisor.Add(null);
            }
        }

        /// <summary>
        /// As arvores da ilha, PURO por seed: a mata fechada (150 x AREA, 4,4 m entre troncos) e as avulsas (26 x AREA). O
        /// sorteio 21 e' o de sempre — a Meshy nao mudou o lugar de nenhuma arvore; especie e inclinacao saem do 22.
        /// </summary>
        public static List<ArvorePlantada> PlantioDasArvores(Relevo relevo)
        {
            var lista = new List<ArvorePlantada>();
            var rng = new Sorteio(21);
            var meshy = new Sorteio(22);
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
                lista.Add(Arvore(relevo, rng, meshy, p, h, true));
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
                lista.Add(Arvore(relevo, rng, meshy, p, h, false));
                extra++;
            }
            return lista;
        }

        static ArvorePlantada Arvore(Relevo relevo, Sorteio rng, Sorteio meshy, Vector2 p, float h, bool mata)
        {
            // os quatro sorteios do 21, na ordem de sempre: escala, giro, esticao, tinta
            float s = rng.Faixa(0.75f, 1.16f);
            var pos = new Vector3(p.x, h - 0.1f, p.y);
            float giro = rng.Faixa(0f, 360f);
            float sy = rng.Faixa(0.95f, 1.22f);
            // tinta por arvore: quebra a repeticao (umas puxam pro amarelo-sol, outras pro verde-frio)
            float v = rng.Faixa(-0.15f, 0.15f);

            // a Meshy: os tres sorteios do 22 saem SEMPRE (a sequencia nao depende da especie)
            bool pinheiro = meshy.Float() < PinheirosNaMata && mata;
            float tx = meshy.Faixa(-Inclinacao, Inclinacao), tz = meshy.Faixa(-Inclinacao, Inclinacao);
            // a escala da procedural vira a da especie (a copa sozinha na campina abre mais) e o esticao cai para 0,95-1,1: a
            // Meshy esticada 22% deforma. KNOB: as faixas, por foto (copa 5,15 m x 6,5 m; pinheiro 8 m x 3,3 m no .glb)
            float k = Mathf.InverseLerp(0.75f, 1.16f, s);
            float e = pinheiro ? Mathf.Lerp(0.85f, 1.15f, k) : mata ? Mathf.Lerp(0.9f, 1.15f, k) : Mathf.Lerp(1f, 1.3f, k);
            float ey = e * Mathf.Lerp(0.95f, 1.1f, Mathf.InverseLerp(0.95f, 1.22f, sy));
            // o pe' no chao DESENHADO mais baixo da pegada (a raiz da copa abre ~2 m; o pinheiro e' tronco fino)
            float pe = PeNoChao(relevo, p, (pinheiro ? 0.5f : 1.3f) * e);
            return new ArvorePlantada
            {
                Pos = pos,
                Proc = Matrix4x4.TRS(pos, Quaternion.Euler(0f, giro, 0f), new Vector3(s, sy * s, s)),
                Tinta = new Color(1f + v * 1.5f, 1f + v * 0.5f, 1f - v * 0.9f),
                Especie = pinheiro ? Especie.Pinheiro : Especie.Copa,
                Variante = v > 0.05f ? 1 : v < -0.05f ? 2 : 0,   // a mesma leitura da tinta: sol / sombra
                Meshy = Matrix4x4.TRS(new Vector3(p.x, pe - AfundaArvore, p.y), Quaternion.Euler(tx, giro, tz), new Vector3(e, ey, e)),
            };
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

        /// <summary>O chao desenhado mais baixo entre `p` e os quatro pontos a `raio` dele: onde assenta a base de quem tem pegada.</summary>
        static float PeNoChao(Relevo r, Vector2 p, float raio)
        {
            float a = Mathf.Min(ChaoDesenhado(r, p.x, p.y), ChaoDesenhado(r, p.x + raio, p.y));
            float b = Mathf.Min(ChaoDesenhado(r, p.x - raio, p.y), ChaoDesenhado(r, p.x, p.y + raio));
            return Mathf.Min(Mathf.Min(a, b), ChaoDesenhado(r, p.x, p.y - raio));
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
                var giro = Quaternion.Euler(0f, rng.Faixa(0f, 360f), 0f);
                float sy = rng.Faixa(0.7f, 1.15f);
                moiM.Add(Matrix4x4.TRS(new Vector3(p.x, h - 0.12f, p.y), giro, new Vector3(sc, sy * sc, sc)));
                float v = rng.Faixa(-0.14f, 0.14f);
                moiTinta.Add(new Color(1f - v * 0.5f, 1f + v, 1f + v * 0.7f));
                if (malhaMoita == null) continue;
                // a da Meshy do TAMANHO da procedural (a cobertura nao muda: ~1,4 m de largura em sc = 1) e pe' no chao mais
                // baixo da pegada: moita boiando na encosta le' como defeito
                float e = 0.65f * sc;
                moiVis.Add(Matrix4x4.TRS(new Vector3(p.x, PeNoChao(relevo, p, 0.7f * e) - 0.1f, p.y), giro, new Vector3(e, sy * e, e)) * moldeMoita);
                moiVar.Add(relevo.BiomaEm(p.x, p.y) == Bioma.Floresta ? 1 : 0);
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
                c.Mr = c.Go.AddComponent<MeshRenderer>();
                c.Mr.sharedMaterial = mat;
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
                    if (malhaMoita != null) MoitasDaMeshy(c);
                }
                RemontarCelula(k);
            }
        }

        /// <summary>Os lotes da moita da Meshy da celula, um por tinta, cheios de uma vez (moita nao queima nem troca de LOD).</summary>
        void MoitasDaMeshy(Celula c)
        {
            c.MoitasMeshy = new Lote[2];
            for (int j = 0; j < c.Moitas.Count; j++)
            {
                int i = c.Moitas[j];
                Lote l = c.MoitasMeshy[moiVar[i]];
                if (l == null)
                {
                    int n = 0;
                    for (int q = 0; q < c.Moitas.Count; q++) if (moiVar[c.Moitas[q]] == moiVar[i]) n++;
                    l = c.MoitasMeshy[moiVar[i]] = new Lote { M = new Matrix4x4[n] };
                }
                l.M[l.N++] = moiVis[i];
                Vector3 p = moiVis[i].GetColumn(3);
                if (j == 0) c.CaixaMoitas = new Bounds(p, Vector3.zero);
                else c.CaixaMoitas.Encapsulate(p);
            }
            c.CaixaMoitas.SetMinMax(c.CaixaMoitas.min - new Vector3(1.5f, 0.5f, 1.5f), c.CaixaMoitas.max + new Vector3(1.5f, 2.5f, 1.5f));
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

        /// <summary>Tocos (arvore queimada), a arvore procedural (sem a Meshy) e as rochas-blob da celula numa malha so', em
        /// coordenada local. Celula sem nada disso (Meshy nas arvores e nas pedras) desliga o renderer.</summary>
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
                else if (malhaArvore == null)
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
            c.Mr.enabled = buf.V.Count > 0;
            if (velha != null) Destroy(velha);
        }

        static bool Linear() => QualitySettings.activeColorSpace == ColorSpace.Linear;

        // ---------------------------------------------------------------- arvore da Meshy

        /// <summary>
        /// As malhas das duas arvores da Meshy ([especie * 2 + lod]), o molde e os triangulos de cada uma e os materiais das
        /// variantes; null = falta .glb, textura, instancing (-nographics) ou o Lit: a ilha fica com a procedural.
        /// Sem o LOD1, longe fica o LOD0 (custa triangulo, nao some).
        /// </summary>
        static Mesh[] MalhasDasArvores(Matrix4x4[] molde, int[] tris)
        {
            if (!SystemInfo.supportsInstancing) return null;
            var m = new Mesh[4];
            string[] nomes = { ArvoreCopa, ArvorePinheiro };
            for (int e = 0; e < 2; e++)
            {
                m[e * 2] = MalhaDoGlb(nomes[e], out Texture tex);
                if (m[e * 2] == null || tex == null) return null;
                m[e * 2 + 1] = MalhaDoGlb(nomes[e] + "-lod1", out _);
                if (m[e * 2 + 1] == null) m[e * 2 + 1] = m[e * 2];
                // o LOD1 e' a mesma arvore decimada (mesmo pivo, mesma UV): um molde e uma textura servem aos dois
                molde[e] = PeDoTronco(m[e * 2]);
                for (int v = 0; v < Variantes; v++)
                {
                    int k = e * Variantes + v;
                    if (MaterialDaMeshy(ref matArvore[k], "Arvore" + (Especie)e + v, tex, TintaMeshy[e, v]) == null) return null;
                }
            }
            for (int i = 0; i < 4; i++) tris[i] = (int)(m[i].GetIndexCount(0) / 3);
            return m;
        }

        /// <summary>A moita da Meshy, os materiais das duas tintas e o molde (base em y = 0, centro da pegada na origem);
        /// null = falta o .glb, a textura, o instancing ou o Lit (fica a procedural em toda distancia).</summary>
        static Mesh MalhaDaMoita(out Matrix4x4 molde, out Mesh lod1)
        {
            molde = Matrix4x4.identity;
            lod1 = null;
            if (!SystemInfo.supportsInstancing) return null;
            Mesh m = MalhaDoGlb(MoitaMeshy, out Texture tex);
            if (m == null || tex == null) return null;
            for (int v = 0; v < matMoita.Length; v++)
                if (MaterialDaMeshy(ref matMoita[v], "Moita" + v, tex, TintaMoita[v]) == null) return null;
            // o LOD1 e' OUTRO remesh do site (428 tris): outra UV, outra textura -> materiais proprios; falhou, fica a procedural longe
            lod1 = MalhaDoGlb(MoitaMeshy + "-lod1", out Texture tex1);
            for (int v = 0; lod1 != null && v < matMoitaLod1.Length; v++)
                if (tex1 == null || MaterialDaMeshy(ref matMoitaLod1[v], "MoitaLod1" + v, tex1, TintaMoita[v]) == null) lod1 = null;
            Bounds b = m.bounds;
            molde = Matrix4x4.Translate(-new Vector3(b.center.x, b.min.y, b.center.z));
            return m;
        }

        /// <summary>A malha e a textura de cor do .glb em Resources. ponytail: a Meshy entrega UM no' sem transformacao
        /// (conferido nos quatro .glb); peca com hierarquia pediria a matriz do no' junto.</summary>
        static Mesh MalhaDoGlb(string nome, out Texture tex)
        {
            tex = null;
            GameObject g = Resources.Load<GameObject>(nome);
            MeshFilter mf = g != null ? g.GetComponentInChildren<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null) return null;
            Renderer r = mf.GetComponent<Renderer>();
            Material o = r != null ? r.sharedMaterial : null;
            if (o != null) tex = o.HasProperty("baseColorTexture") ? o.GetTexture("baseColorTexture") : o.mainTexture;
            return mf.sharedMesh;
        }

        /// <summary>
        /// O MOLDE: leva o pe' do TRONCO para a origem, onde mora o colisor (capsula de 0,38 m no PosArvore). A copa da Meshy
        /// tem o tronco torcido ~0,6 m fora do pivo do .glb (medido nos vertices de 8 a 35% da altura); a base vai a y = 0.
        /// Malha ilegivel: so' a base.
        /// </summary>
        static Matrix4x4 PeDoTronco(Mesh m)
        {
            Bounds b = m.bounds;
            var pe = new Vector3(0f, b.min.y, 0f);
            if (m.isReadable)
            {
                Vector3[] vs = m.vertices;   // uma vez por especie, no load
                float y0 = b.min.y + b.size.y * 0.08f, y1 = b.min.y + b.size.y * 0.35f, sx = 0f, sz = 0f;
                int n = 0;
                for (int i = 0; i < vs.Length; i++)
                    if (vs[i].y >= y0 && vs[i].y < y1) { sx += vs[i].x; sz += vs[i].z; n++; }
                if (n > 0) { pe.x = sx / n; pe.z = sz / n; }
            }
            return Matrix4x4.Translate(-pe);
        }

        /// <summary>
        /// URP Lit com a textura da Meshy x a TINTA da variante, fosco e dos DOIS lados: a folha da Meshy e' casca aberta
        /// (~100 arestas de borda e ~90 nao-manifold por arvore, medido no Blender). Instanciado: com "Strip Unused" a variante
        /// de instancing so' vai pro APK se algum material-ASSET a pede — o ArkanaArvoreInstancing (Lit + GPU Instancing, como
        /// o ArkanaGramaInstancing da grama); sem ele, o Lit dos Always Included Shaders. Copia: nunca altera o asset.
        /// </summary>
        static Material MaterialDaMeshy(ref Material cache, string nome, Texture tex, Color tinta)
        {
            if (cache != null) return cache;
            Material molde = Resources.Load<Material>("ArkanaArvoreInstancing");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (molde != null && molde.shader != lit) molde = null;   // referencia quebrada (meta regenerado): ignora
            if (lit == null) return null;
            var m = molde != null ? new Material(molde) : new Material(lit);
            m.name = nome;
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", tinta);
            m.SetFloat("_Cull", 0f);          // 0 = Off: frente e verso (a sombra tambem)
            m.SetFloat("_Smoothness", 0.12f); // folha fosca: o 0,5 do Lit vira plastico
            m.SetFloat("_Metallic", 0f);
            m.enableInstancing = true;        // o RenderMeshInstanced exige
            return cache = m;
        }

        /// <summary>
        /// Bina as arvores da Meshy em blocos de ~60 m e reserva os lotes (capacidade = arvores daquela especie e tinta no
        /// bloco, nos dois LOD). Tudo alocado AQUI: o corte de 4 Hz so' copia matriz e o quadro so' desenha.
        /// ponytail: ilha na origem (o PosArvore e o terreno reativo ja' supoem): matriz e caixa vao em coordenada de mundo.
        /// </summary>
        void MontarBlocos()
        {
            if (malhaArvore == null) return;
            int lado = Mathf.Max(1, Mathf.RoundToInt(relevo.Lado / PassoBloco));
            float passoB = relevo.Lado / lado, meio = relevo.Lado * 0.5f;
            var porBloco = new Dictionary<int, Bloco>();
            for (int i = 0; i < arvPos.Count; i++)
            {
                Vector3 p = arvPos[i];
                int k = Mathf.Clamp((int)((p.z + meio) / passoB), 0, lado - 1) * lado + Mathf.Clamp((int)((p.x + meio) / passoB), 0, lado - 1);
                if (!porBloco.TryGetValue(k, out Bloco b))
                {
                    porBloco[k] = b = new Bloco { Caixa = new Bounds(p, Vector3.zero) };
                    blocos.Add(b);
                }
                b.Arvores.Add(i);
                b.Caixa.Encapsulate(p);
            }
            // a caixa do frustum: a arvore mais larga e mais alta na escala maxima (1,3 x 1,1), + o molde e a inclinacao
            Vector3 t = Vector3.Max(malhaArvore[0].bounds.size, malhaArvore[2].bounds.size) * 1.45f;
            var folga = new Vector3(Mathf.Max(t.x, t.z) * 0.5f + 1f, 1f, Mathf.Max(t.x, t.z) * 0.5f + 1f);
            var n = new int[2 * Variantes];
            foreach (Bloco b in blocos)
            {
                System.Array.Clear(n, 0, n.Length);
                foreach (int i in b.Arvores) n[(int)arvEsp[i] * Variantes + arvVar[i]]++;
                for (int v = 0; v < n.Length; v++)
                {
                    if (n[v] == 0) continue;
                    b.Lotes[v * 2] = new Lote { M = new Matrix4x4[n[v]] };
                    b.Lotes[v * 2 + 1] = new Lote { M = new Matrix4x4[n[v]] };
                }
                b.Caixa.SetMinMax(b.Caixa.min - folga, b.Caixa.max + new Vector3(folga.x, t.y + 1f, folga.z));
            }
        }

        /// <summary>
        /// Enche os lotes pelo LOD de CADA arvore (4 Hz): LOD0 perto, LOD1 alem de DistanciaLod1. Queimada nao entra: quem a
        /// desenha e' o toco da celula. Anota o custo do quadro para o diag.
        /// </summary>
        void Reclassificar(Vector3 olhoDoLod)
        {
            int lotes = 0, tris = 0, perto = 0;
            for (int j = 0; j < blocos.Count; j++)
            {
                Bloco b = blocos[j];
                for (int k = 0; k < b.Lotes.Length; k++) if (b.Lotes[k] != null) b.Lotes[k].N = 0;
                for (int a = 0; a < b.Arvores.Count; a++)
                {
                    int i = b.Arvores[a];
                    if (arvQueimada[i]) continue;
                    bool longe = Lod1(olhoDoLod, arvPos[i]);
                    if (!longe) perto++;
                    Lote l = b.Lotes[((int)arvEsp[i] * Variantes + arvVar[i]) * 2 + (longe ? 1 : 0)];
                    l.M[l.N++] = arvVis[i];
                }
                for (int k = 0; k < b.Lotes.Length; k++)
                {
                    if (b.Lotes[k] == null || b.Lotes[k].N == 0) continue;
                    lotes++;
                    tris += b.Lotes[k].N * trisArvore[(k >> 1) / Variantes * 2 + (k & 1)];
                }
            }
            LotesEnviados = lotes;
            TrisEnviados = tris;
            ArvoresPerto = perto;
        }

        /// <summary>Um RenderMeshInstanced por lote cheio, todo quadro (como a Grama). O frustum de cada camera — e de cada
        /// cascata da sombra — corta o lote pela caixa do bloco.</summary>
        void Desenhar()
        {
            for (int j = 0; j < blocos.Count; j++)
            {
                Bloco b = blocos[j];
                for (int k = 0; k < b.Lotes.Length; k++)
                {
                    Lote l = b.Lotes[k];
                    if (l == null || l.N == 0) continue;
                    var rp = new RenderParams(matArvore[k >> 1])
                    {
                        worldBounds = b.Caixa,
                        shadowCastingMode = ShadowCastingMode.On,   // a arvore fazia sombra (malha da celula); continua fazendo
                        receiveShadows = true,
                        lightProbeUsage = LightProbeUsage.Off,      // Off = o probe AMBIENTE da cena (o Trilight da Ilha)
                        layer = gameObject.layer,
                    };
                    Graphics.RenderMeshInstanced(rp, malhaArvore[(k >> 1) / Variantes * 2 + (k & 1)], 0, l.M, l.N);
                }
            }
        }

        /// <summary>
        /// 4 Hz: o corte da MOITA (distancia horizontal, uma por celula) e o LOD da arvore da Meshy (uma por arvore). Todo
        /// quadro: os lotes. A rocha fica com o frustum (bounds da propria celula).
        /// </summary>
        void Update()
        {
            relogio += Time.deltaTime;
            if (relogio >= Periodo && cels != null)
            {
                relogio = 0f;
                Camera cam = cameraDoLod != null ? cameraDoLod : Camera.main;
                if (cam != null) CortarMoitas(cam.transform.position);
                // sem camera nenhuma, tudo em LOD1: desenha, nunca some
                if (malhaArvore != null) Reclassificar(cam != null ? cam.transform.position : new Vector3(0f, 1e4f, 0f));
            }
            if (malhaArvore != null) Desenhar();
            if (malhaMoita != null && cels != null) DesenharMoitas();
        }

        /// <summary>A moita da celula: da Meshy ate' AlcanceMoitaMeshy, a procedural dali ao CorteMoitas, nada alem.</summary>
        void CortarMoitas(Vector3 olho)
        {
            float corte2 = CorteMoitas * CorteMoitas, perto2 = AlcanceMoitaMeshy * AlcanceMoitaMeshy;
            for (int k = 0; k < cels.Length; k++)
            {
                Celula c = cels[k];
                if (c == null || c.MoitasR == null) continue;
                Vector3 centro = transform.TransformPoint(c.Centro);
                float d2 = (olho - centro).sqrMagnitude;
                c.MoitasPerto = c.MoitasMeshy != null && d2 < perto2;
                c.MoitasMedio = c.MoitasMeshy != null && malhaMoitaLod1 != null && !c.MoitasPerto && d2 < corte2;
                bool liga = d2 < corte2 && !c.MoitasPerto && !c.MoitasMedio;
                if (liga != c.MoitasLigadas)
                {
                    c.MoitasLigadas = liga;
                    c.MoitasR.enabled = liga;
                }
            }
        }

        /// <summary>A moita da Meshy das celulas perto: um lote por tinta, sem sombra (moita nao pagava sombra e continua nao pagando).</summary>
        void DesenharMoitas()
        {
            for (int k = 0; k < cels.Length; k++)
            {
                Celula c = cels[k];
                if (c == null || !(c.MoitasPerto || c.MoitasMedio)) continue;
                Mesh malha = c.MoitasPerto ? malhaMoita : malhaMoitaLod1;
                Material[] mats = c.MoitasPerto ? matMoita : matMoitaLod1;
                for (int v = 0; v < c.MoitasMeshy.Length; v++)
                {
                    Lote l = c.MoitasMeshy[v];
                    if (l == null) continue;
                    var rp = new RenderParams(mats[v])
                    {
                        worldBounds = c.CaixaMoitas,
                        shadowCastingMode = ShadowCastingMode.Off,
                        receiveShadows = true,
                        lightProbeUsage = LightProbeUsage.Off,
                        layer = gameObject.layer,
                    };
                    Graphics.RenderMeshInstanced(rp, malha, 0, l.M, l.N);
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
