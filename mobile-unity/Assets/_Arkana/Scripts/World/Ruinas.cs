using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arkana.World
{
    /// <summary>
    /// As RUINAS por codigo (Island.gd _build_ruins): colunas num circulo, duas muralhas quebradas e o
    /// altar no plato de 9 m, mais os ROCHEDOS NO MAR (_build_stacks), que dao escala ao horizonte.
    ///
    /// ONDE DIFERE DO GODOT: la' o circulo tinha 13 m cravados e a muralha comecava a (-12,-16) — o
    /// proprio Godot mediu que, quando o plato cresceu, a construcao nao foi junto e o POI virou "campina
    /// com pedras". Aqui o layout e' FRACAO do raio das ruinas e a CONTAGEM cresce com a escala: o
    /// tamanho de cada pedra fica (uma coluna tem 4,2 m em qualquer mapa), a densidade tambem.
    ///
    /// PEDRA DA MESHY (onda 10A): a coluna canelada de arenito e o bloco de muralha rachado (38/39-*.glb) no lugar dos
    /// prismas bege lisos, que liam como maquete ao lado dos arcos e estatuas texturizados. Lugar, giro e altura sorteados
    /// sao os de sempre (Plantio, PURO; a variacao da Meshy tem sorteio proprio) e as Pegadas tambem: cada peca cabe na
    /// dela. Pe' na malha desenhada, afundado um pouco. Sem o .glb (ou malha nao legivel), volta o prisma.
    ///
    /// Custo: as pecas da Meshy (ilha de 600 m: 24 colunas + o toco, 34 blocos = ~65K tris) viram UMA malha por .glb
    /// (Mesh.CombineMeshes, no load): 2 draw calls (+ sombra), nenhum GameObject por pedra, nenhum Update. Colisao = MeshCollider
    /// da propria malha (estatica, exata: a Queda pousa no topo da coluna, o corpo bate na muralha). Rochedo nao colide.
    /// </summary>
    public sealed class Ruinas : MonoBehaviour
    {
        const float AltColuna = 4.2f;

        /// <summary>As pecas da Meshy em Resources (onda 10A, site + otimizar): coluna canelada com capitel quebrado e musgo no pe'
        /// (1.500 tris, 1,37 x 4,2 m) e bloco de muralha de arenito rachado com runa (800 tris, 1,8 x 1,39 x 0,89 m).</summary>
        public const string ColunaDaMeshy = "38-coluna-ruina", BlocoDaMeshy = "39-bloco-ruina";
        /// <summary>O bloco da Meshy na muralha, fator por eixo sobre o .glb (+-5% por pedra): 2,1 x 1,33 x 0,95 m, com 18-30% da
        /// altura enterrada fica ~1 m a mostra, a meia-altura do prisma de 2,2 x 0,9 x 1,0. Esticado ate' a caixa do prisma, as
        /// pedras achatariam 50%. KNOB: por foto.</summary>
        public static readonly Vector3 EscalaBloco = new Vector3(1.17f, 0.96f, 1.07f);
        /// <summary>Quanto o pe' da coluna desce abaixo do chao do centro (m): pousada exata mostra fresta. KNOB.</summary>
        const float AfundaPe = 0.1f;
        /// <summary>O minimo que o ponto mais baixo da base fica abaixo da menor cota da pegada (m): na encosta, sem fresta.</summary>
        const float Folga = 0.06f;
        /// <summary>Quanto o bloco de cima (ponta da muralha, altar) entra no de baixo (m): o topo rachado nao e' plano. KNOB.</summary>
        const float Encaixe = 0.12f;

        public int Colunas { get; private set; }
        public int Blocos { get; private set; }
        public int Rochedos { get; private set; }
        /// <summary>O chao que as ruinas ocupam (x, z, raio em w), por coluna e bloco: o KitCenario nao planta em cima da muralha.</summary>
        public readonly List<Vector4> Pegadas = new List<Vector4>();

        public enum TipoDePedra { Coluna, Caida, Toco, Bloco }

        /// <summary>Uma pedra das ruinas, PURA: o prisma de sempre (lugar, giro e altura sorteados), a Meshy assentada e a tinta.</summary>
        public struct PedraDaRuina
        {
            public TipoDePedra Tipo;
            public Matrix4x4 Proc, Meshy;
            public Color Tinta;
        }

        public void Montar(Relevo relevo, Material pedra)
        {
            Pegadas.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject c = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
            }
            Colunas = Blocos = Rochedos = 0;
            if (relevo == null) return;
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            MontarRuinas(relevo, pedra, linear);
            MontarRochedos(relevo, pedra, linear);
        }

        void MontarRuinas(Relevo r, Material pedra, bool linear)
        {
            Mesh mc = MalhaDaMeshy(ColunaDaMeshy, out Material matC), mb = MalhaDaMeshy(BlocoDaMeshy, out Material matB);
            var cubo = new Bounds(Vector3.up * 0.5f, Vector3.one);   // sem o .glb a matriz da Meshy sai, mas volta o prisma
            List<PedraDaRuina> pedras = Plantio(r, mc != null ? mc.bounds : cubo, mb != null ? mb.bounds : cubo, Pegadas);
            var colProto = ProtoColuna();
            var blocoProto = ProtoBloco();
            var b = new MalhaProc.Construtor();
            var colunas = new List<PedraDaRuina>();
            var blocos = new List<PedraDaRuina>();
            foreach (PedraDaRuina p in pedras)
            {
                bool bloco = p.Tipo == TipoDePedra.Bloco;
                if (bloco) Blocos++;
                else if (p.Tipo == TipoDePedra.Coluna) Colunas++;
                if ((bloco ? mb : mc) != null) (bloco ? blocos : colunas).Add(p);
                else b.Adicionar(bloco ? blocoProto : colProto, p.Proc, p.Tinta);
            }
            if (b.V.Count > 0) Filho("Ruinas", b.ParaMesh("Ruinas", linear), pedra, ShadowCastingMode.On, true);
            // ponytail: uma malha por .glb para as ruinas inteiras (caixa de ~80 m): perto delas, cada uma das 4 cascatas de sombra
            // (50 m) desenha tudo, ~4 x 65K tris. Se o FPS medido cair ali: juntar por celula de ~25 m, ou tirar a sombra do bloco
            if (colunas.Count > 0) Filho("Colunas", Juntar("Colunas", mc, colunas), matC, ShadowCastingMode.On, true);
            if (blocos.Count > 0) Filho("Blocos", Juntar("Blocos", mb, blocos), matB, ShadowCastingMode.On, true);
        }

        /// <summary>
        /// As pedras das ruinas, PURO por seed: o layout do Godot em FRACAO do raio (a contagem cresce com a escala), as Pegadas
        /// em `pegadas` e a matriz da Meshy de cada pedra a partir dos limites do .glb (`coluna`, `bloco`). O sorteio 31 e' o de
        /// sempre (giro, altura e tinta do prisma nao mudam: o KitCenario planta pelas pegadas); a variacao da Meshy vem do 32.
        /// </summary>
        public static List<PedraDaRuina> Plantio(Relevo r, Bounds coluna, Bounds bloco, List<Vector4> pegadas)
        {
            var lista = new List<PedraDaRuina>();
            var rng = new Sorteio(31);
            var meshy = new Sorteio(32);
            float k = r.RuinasR / 20f;                 // o layout do Godot foi escrito para RUINS_R = 20 m
            int nCol = Mathf.Max(4, Mathf.RoundToInt(12 * r.Escala));
            float raioCirc = 13f * k;
            Vector2 c0 = r.Ruinas;

            for (int i = 0; i < nCol; i++)
            {
                float ang = Mathf.PI * 2f * i / nCol;
                var p = new Vector2(c0.x + Mathf.Cos(ang) * raioCirc, c0.y + Mathf.Sin(ang) * raioCirc);
                float h = r.Altura(p.x, p.y);
                Color tinta = Desgaste(rng, -0.13f, 0.09f);
                if (i % 12 == 2)
                {
                    // coluna CAIDA, deitada apontando pra fora do circulo (a ruina conta uma historia): o eixo da coluna (+Y)
                    // vira o "fora". O prisma tomba 84,6 graus como no Godot; a Meshy deita a 90, uma face do plinto no chao,
                    // girada no proprio eixo (o capitel quebrado nao cai sempre igual), no meio da pegada
                    var fora = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Quaternion rumo = Giro(Vector3.up, Mathf.Atan2(fora.x, fora.z) * Mathf.Rad2Deg);
                    var pos = new Vector3(p.x + fora.x * 2.1f, h + 0.8f, p.y + fora.z * 2.1f);
                    var meio = new Vector2(pos.x + fora.x * AltColuna * 0.5f, pos.z + fora.z * AltColuna * 0.5f);
                    Quaternion deitada = rumo * Giro(Vector3.right, 90f) * Giro(Vector3.up, 90f * meshy.Int(4));
                    lista.Add(new PedraDaRuina
                    {
                        Tipo = TipoDePedra.Caida,
                        Proc = Trs(pos, rumo * Giro(Vector3.right, 84.6f), Vector3.one),
                        Meshy = Pousar(r, coluna, Matrix4x4.Rotate(deitada) * Matrix4x4.Scale(Vector3.one * meshy.Faixa(0.92f, 1f)), meio, AfundaPe),
                        Tinta = tinta,
                    });
                    pegadas.Add(new Vector4(meio.x, meio.y, 0f, AltColuna * 0.55f));
                    continue;
                }
                pegadas.Add(new Vector4(p.x, p.y, 0f, 0.9f));
                float sy = rng.Faixa(0.35f, 1.05f), giro = rng.Faixa(0f, 360f);
                lista.Add(new PedraDaRuina
                {
                    Tipo = TipoDePedra.Coluna,
                    Proc = Trs(new Vector3(p.x, h, p.y), Giro(Vector3.up, giro), new Vector3(1f, sy, 1f)),
                    Meshy = ColunaEmPe(r, coluna, p, giro, sy, meshy.Faixa(0.84f, 0.92f)),
                    Tinta = tinta,
                });
            }

            // duas muralhas QUEBRADAS: lacuna a cada 4 blocos, pontas com 2 camadas
            Vector2[] inicio = { new Vector2(-12f, -16f) * k, new Vector2(15f, -6f) * k };
            Vector2[] dir = { new Vector2(1f, 0.18f).normalized, new Vector2(0.25f, 1f).normalized };
            int[] nBase = { 10, 9 };
            for (int w = 0; w < 2; w++)
            {
                int n = Mathf.RoundToInt(nBase[w] * r.Escala);
                float yaw = -Mathf.Atan2(dir[w].y, dir[w].x) * Mathf.Rad2Deg;
                for (int j = 0; j < n; j++)
                {
                    if (j % 4 == 3) continue;
                    Vector2 p = c0 + inicio[w] + dir[w] * (j * 2.6f);
                    float h = r.Altura(p.x, p.y);
                    int camadas = (j == 0 || j == n - 1) ? 2 : 1;
                    float topo = float.NaN;
                    for (int l = 0; l < camadas; l++)
                    {
                        float giro = yaw + rng.Faixa(-7f, 7f);
                        Matrix4x4 m = Bloco(r, bloco, p, giro, Vector3.one, topo, meshy);
                        topo = m.MultiplyPoint3x4(bloco.max).y;
                        lista.Add(new PedraDaRuina
                        {
                            Tipo = TipoDePedra.Bloco,
                            Proc = Trs(new Vector3(p.x, h + 0.45f + l * 0.92f, p.y), Giro(Vector3.up, giro), Vector3.one),
                            Meshy = m,
                            Tinta = Desgaste(rng, -0.16f, 0.08f),
                        });
                    }
                    pegadas.Add(new Vector4(p.x, p.y, 0f, 1.3f));
                }
            }

            // altar central: 2 blocos (o de cima mais estreito, girado) + toco de coluna
            float hc = r.Altura(c0.x, c0.y);
            var cima = new Vector3(0.7f, 1f, 0.7f);
            Matrix4x4 a0 = Bloco(r, bloco, c0, 23f, Vector3.one, float.NaN, meshy);
            Matrix4x4 a1 = Bloco(r, bloco, c0 + new Vector2(0.3f, -0.2f), 63f, cima, a0.MultiplyPoint3x4(bloco.max).y, meshy);
            lista.Add(new PedraDaRuina { Tipo = TipoDePedra.Bloco, Proc = Trs(new Vector3(c0.x, hc + 0.45f, c0.y), Giro(Vector3.up, 23f), Vector3.one), Meshy = a0, Tinta = Color.white });
            lista.Add(new PedraDaRuina { Tipo = TipoDePedra.Bloco, Proc = Trs(new Vector3(c0.x + 0.3f, hc + 1.35f, c0.y - 0.2f), Giro(Vector3.up, 63f), cima), Meshy = a1, Tinta = Color.white });
            var toco = new Vector2(c0.x - 3.4f, c0.y + 3f);
            lista.Add(new PedraDaRuina
            {
                Tipo = TipoDePedra.Toco,
                Proc = Trs(new Vector3(toco.x, hc, toco.y), Quaternion.identity, new Vector3(1f, 0.28f, 1f)),
                Meshy = ColunaEmPe(r, coluna, toco, 0f, 0.28f, 0.88f),
                Tinta = Color.white,
            });
            return lista;
        }

        /// <summary>A coluna da Meshy em pe' no lugar do prisma: largura `e` (o plinto cabe na pegada de 0,9 m ate' 0,92) e a
        /// altura sorteada de sempre (sy 0,35-1,05) contida em 0,8-1,12 x `e` — achatada a 0,35, como o prisma, o capitel vira
        /// panqueca: a coluna curta fica com 2,8 m. KNOB: a faixa, por foto.</summary>
        static Matrix4x4 ColunaEmPe(Relevo r, Bounds molde, Vector2 p, float giro, float sy, float e)
        {
            float ey = e * Mathf.Lerp(0.8f, 1.12f, Mathf.InverseLerp(0.35f, 1.05f, sy));
            return Pousar(r, molde, Matrix4x4.Rotate(Giro(Vector3.up, giro)) * Matrix4x4.Scale(new Vector3(e, ey, e)), p, AfundaPe);
        }

        /// <summary>O bloco da Meshy (EscalaBloco x `fator`, +-5% por eixo) em `p`: no chao, com 18-30% da altura enterrada; com
        /// `apoio` (o topo do bloco de baixo), sentado nele, `Encaixe` para dentro. KNOB: faixas, por foto.</summary>
        static Matrix4x4 Bloco(Relevo r, Bounds molde, Vector2 p, float giro, Vector3 fator, float apoio, Sorteio meshy)
        {
            var esc = new Vector3(EscalaBloco.x * fator.x * meshy.Faixa(0.95f, 1.05f), EscalaBloco.y * fator.y * meshy.Faixa(0.95f, 1.05f),
                EscalaBloco.z * fator.z * meshy.Faixa(0.95f, 1.05f));
            float afunda = molde.size.y * esc.y * meshy.Faixa(0.18f, 0.3f);
            Matrix4x4 m = Pousar(r, molde, Matrix4x4.Rotate(Giro(Vector3.up, giro)) * Matrix4x4.Scale(esc), p, afunda);
            if (!float.IsNaN(apoio)) m = Matrix4x4.Translate(Vector3.up * (apoio - Encaixe - m.MultiplyPoint3x4(molde.min).y)) * m;
            return m;
        }

        /// <summary>
        /// O molde (`rs`: giro e escala, sem translacao) com o centro da caixa em `onde`, o fundo `afunda` abaixo da malha desenhada
        /// no centro e NUNCA acima da menor cota sob a pegada (27 pontos da caixa, `Folga` para dentro): na encosta a quina de baixo
        /// nao boia e so' o lado de cima enterra mais. Serve em pe' e deitada (a coluna caida); o AssentarRocha faz o mesmo pela base.
        /// </summary>
        static Matrix4x4 Pousar(Relevo r, Bounds molde, Matrix4x4 rs, Vector2 onde, float afunda)
        {
            Vector3 c = rs.MultiplyPoint3x4(molde.center);
            float x0 = onde.x - c.x, z0 = onde.y - c.z, fundo = float.MaxValue, chao = float.MaxValue;
            for (int i = 0; i < 27; i++)
            {
                Vector3 o = rs.MultiplyPoint3x4(molde.min + Vector3.Scale(molde.size, new Vector3(i % 3, i / 3 % 3, i / 9) * 0.5f));
                fundo = Mathf.Min(fundo, o.y);
                chao = Mathf.Min(chao, ChaoDesenhado(r, x0 + o.x, z0 + o.z));
            }
            float pe = Mathf.Min(ChaoDesenhado(r, onde.x, onde.y) - afunda, chao - Folga);
            return Matrix4x4.Translate(new Vector3(x0, pe - fundo, z0)) * rs;
        }

        /// <summary>Altura do chao DESENHADO (a malha de Ilha.Quads, a divisao de triangulo da Ilha, que o GradeDoChao repete).</summary>
        // ponytail: copia do Vegetacao.ChaoDesenhado (privado la'); juntar os dois num lugar so' quando alguem mexer no Vegetacao
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

        /// <summary>Giro de `graus` em volta do `eixo` unitario e o TRS, na conta: Quaternion.Euler e Matrix4x4.TRS sao chamadas
        /// internas do Unity, e sem elas o Plantio roda puro fora do editor (a sonda da onda 10A, como a do AssentarRocha).</summary>
        static Quaternion Giro(Vector3 eixo, float graus)
        {
            float a = graus * Mathf.Deg2Rad * 0.5f, s = Mathf.Sin(a);
            return new Quaternion(eixo.x * s, eixo.y * s, eixo.z * s, Mathf.Cos(a));
        }

        static Matrix4x4 Trs(Vector3 p, Quaternion q, Vector3 s) => Matrix4x4.Translate(p) * Matrix4x4.Rotate(q) * Matrix4x4.Scale(s);

        /// <summary>A malha e o material do .glb `nome` (copia fosca: o 0,5 de rugosidade da Meshy e' pedra de plastico); null = sem o
        /// .glb, ou malha nao legivel (o CombineMeshes no aparelho exige legivel; o glTFast a deixa assim, a fiacao e' defensiva).</summary>
        static Mesh MalhaDaMeshy(string nome, out Material mat)
        {
            mat = null;
            GameObject g = Resources.Load<GameObject>(nome);
            // ponytail: um no' sem transformacao e um material (a Meshy entrega assim); no' com giro pediria o localToWorld dele
            MeshFilter mf = g != null ? g.GetComponentInChildren<MeshFilter>() : null;
            MeshRenderer mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
            if (mr == null || mr.sharedMaterial == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) return null;
            mat = new Material(mr.sharedMaterial) { name = nome + " (ruina)" };   // copia: nunca altera o asset importado
            if (mat.HasProperty("roughnessFactor")) mat.SetFloat("roughnessFactor", 0.75f);   // glTFast; KNOB: por foto
            else if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            return mf.sharedMesh;
        }

        /// <summary>As pedras de UM .glb numa malha so' (um draw call), a tinta de cada uma na cor do vertice: o shader do glTFast
        /// multiplica a base pela cor do vertice (linear, como o ParaMesh grava a do prisma).</summary>
        static Mesh Juntar(string nome, Mesh molde, List<PedraDaRuina> pedras)
        {
            int n = molde.vertexCount;
            var inst = new CombineInstance[pedras.Count];
            var cores = new Color[n * pedras.Count];
            for (int i = 0; i < pedras.Count; i++)
            {
                inst[i] = new CombineInstance { mesh = molde, transform = pedras[i].Meshy };
                Color t = pedras[i].Tinta.linear;
                for (int v = 0; v < n; v++) cores[i * n + v] = t;
            }
            var malha = new Mesh { name = nome };
            if (cores.Length > 65000) malha.indexFormat = IndexFormat.UInt32;   // 25 colunas x 2.954 vertices
            malha.CombineMeshes(inst, true, true);
            malha.colors = cores;
            return malha;
        }

        /// <summary>Nome em Resources da rocha dos rochedos: a rocha vulcanica do kit (Emberstone Outcrop) em 3K, remesh do site.</summary>
        public const string RochaDoMar = "18-rocha-mar";

        /// <summary>
        /// Rochedos num anel alem da praia, dentro da nevoa e fora da sombra: cenario de fundo, nao rota. Desde 12/09 cada
        /// um e' a ROCHA do kit em 3K (22 x 3K tris): o bloco facetado lia como placeholder do castelo e da queda. Sem o
        /// .glb, volta o bloco (fiacao defensiva). Os sorteios sao os mesmos: o anel nao muda de lugar.
        /// </summary>
        void MontarRochedos(Relevo r, Material pedra, bool linear)
        {
            var rng = new Sorteio(111);
            GameObject rocha = Resources.Load<GameObject>(RochaDoMar);
            Bounds molde = rocha != null ? Limites(rocha) : new Bounds();
            if (!(molde.size.y > 0.001f)) rocha = null;
            var proto = new MalhaProc.Construtor();
            proto.Blob(Vector3.zero, new Vector3(1f, 0.75f, 1f), Relevo.CorPedregulho, new Sorteio(13), 0.3f);
            var b = new MalhaProc.Construtor();
            Transform raiz = null;
            if (rocha != null) { raiz = new GameObject("Rochedos").transform; raiz.SetParent(transform, false); }
            const int n = 22;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * 2f * i / n + rng.Faixa(-0.12f, 0.12f);
                // o anel acompanha a ilha: 26-100 m alem da terra na ilha de 132 m, como FRACAO do raio
                float d = rng.Faixa(r.RaioTerra * 1.2f, r.RaioTerra * 1.76f);
                float sc = rng.Faixa(4f, 12f);
                var pos = new Vector3(Mathf.Cos(a) * d, rng.Faixa(-2.6f, -0.6f), Mathf.Sin(a) * d);
                var esc = new Vector3(sc, sc * rng.Faixa(0.8f, 1.9f), sc);
                var giro = Quaternion.Euler(0f, rng.Faixa(0f, 360f), 0f);
                Color tinta = Desgaste(rng, -0.1f, 0.1f);
                if (rocha == null) { b.Adicionar(proto, Matrix4x4.TRS(pos, giro, esc), tinta); continue; }
                // o bloco tinha 2 x sc de largura e 1,5 x sc x (0,8..1,9) de altura em volta do centro: a rocha ocupa a
                // mesma caixa, com o pe' afundado no mar
                float largura = 2f * esc.x, altura = 1.5f * esc.y;
                float lx = Mathf.Max(molde.size.x, molde.size.z);
                var go = Instantiate(rocha, Vector3.zero, giro, raiz);
                go.transform.localScale = new Vector3(largura / lx, altura / molde.size.y, largura / lx);
                go.transform.position = pos + Vector3.down * (altura * 0.5f + molde.min.y * go.transform.localScale.y);
                foreach (Renderer rr in go.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = ShadowCastingMode.Off;
            }
            Rochedos = n;
            if (rocha == null) Filho("Rochedos", b.ParaMesh("Rochedos", linear), pedra, ShadowCastingMode.Off);
        }

        /// <summary>Limites do molde na origem, sem giro (o que o glb traz de hierarquia conta junto). Os pedregulhos da Vegetacao medem por aqui.</summary>
        internal static Bounds Limites(GameObject prefab)
        {
            GameObject tmp = Instantiate(prefab);
            Renderer[] rs = tmp.GetComponentsInChildren<Renderer>();
            var b = new Bounds();
            for (int i = 0; i < rs.Length; i++) { if (i == 0) b = rs[i].bounds; else b.Encapsulate(rs[i].bounds); }
            tmp.SetActive(false);
            if (Application.isPlaying) Destroy(tmp); else DestroyImmediate(tmp);
            return b;
        }

        GameObject Filho(string nome, Mesh malha, Material mat, ShadowCastingMode sombra, bool colide = false)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = sombra;
            if (colide) go.AddComponent<MeshCollider>().sharedMesh = malha;
            return go;
        }

        /// <summary>Pedra que envelheceu junto nunca envelhece IGUAL: tinta por peca.</summary>
        static Color Desgaste(Sorteio rng, float min, float max)
        {
            float v = rng.Faixa(min, max);
            return new Color(1f + v * 0.8f, 1f + v * 0.9f, 1f + v, 1f);
        }

        /// <summary>Coluna de 4,2 m: a UNICA silhueta vertical construida do mapa (compete com arvore e com a mesa do pico).</summary>
        static MalhaProc.Construtor ProtoColuna()
        {
            var b = new MalhaProc.Construtor();
            b.Tronco(Vector3.zero, 0.72f, 0.6f, AltColuna, 6, Relevo.CorRuina);
            b.Tampa(new Vector3(0f, AltColuna, 0f), 0.6f, 6, Relevo.CorRuina);
            return b;
        }

        static MalhaProc.Construtor ProtoBloco()
        {
            var b = new MalhaProc.Construtor();
            b.Caixa(Vector3.zero, new Vector3(1.1f, 0.45f, 0.5f), Relevo.Escurecer(Relevo.CorRuina, 0.1f));
            return b;
        }
    }
}
