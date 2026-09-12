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
    /// Custo: tudo numa malha por grupo (1 draw call as ruinas, 1 os rochedos). Colisao das ruinas =
    /// MeshCollider da propria malha (~1.000 faces estaticas, exata, 1 componente); rochedo nao colide.
    /// </summary>
    public sealed class Ruinas : MonoBehaviour
    {
        const float AltColuna = 4.2f;

        public int Colunas { get; private set; }
        public int Blocos { get; private set; }
        public int Rochedos { get; private set; }
        /// <summary>O chao que as ruinas ocupam (x, z, raio em w), por coluna e bloco: o KitCenario nao planta em cima da muralha.</summary>
        public readonly List<Vector4> Pegadas = new List<Vector4>();

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
            var rng = new Sorteio(31);
            float k = r.RuinasR / 20f;                 // o layout do Godot foi escrito para RUINS_R = 20 m
            int nCol = Mathf.Max(4, Mathf.RoundToInt(12 * r.Escala));
            float raioCirc = 13f * k;
            var colProto = ProtoColuna();
            var blocoProto = ProtoBloco();
            var b = new MalhaProc.Construtor();
            Vector2 c0 = r.Ruinas;

            for (int i = 0; i < nCol; i++)
            {
                float ang = Mathf.PI * 2f * i / nCol;
                var p = new Vector2(c0.x + Mathf.Cos(ang) * raioCirc, c0.y + Mathf.Sin(ang) * raioCirc);
                float h = r.Altura(p.x, p.y);
                Color tinta = Desgaste(rng, -0.13f, 0.09f);
                if (i % 12 == 2)
                {
                    // coluna CAIDA, deitada apontando pra fora do circulo (a ruina conta uma historia):
                    // o eixo da coluna (+Y) vira o +Z do LookRotation, tombado 84,6 graus como no Godot
                    var fora = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    var q = Quaternion.LookRotation(fora, Vector3.up) * Quaternion.Euler(84.6f, 0f, 0f);
                    var pos = new Vector3(p.x + fora.x * 2.1f, h + 0.8f, p.y + fora.z * 2.1f);
                    b.Adicionar(colProto, Matrix4x4.TRS(pos, q, Vector3.one), tinta);
                    Pegadas.Add(new Vector4(pos.x + fora.x * AltColuna * 0.5f, pos.z + fora.z * AltColuna * 0.5f, 0f, AltColuna * 0.55f));
                    continue;
                }
                Pegadas.Add(new Vector4(p.x, p.y, 0f, 0.9f));
                float sy = rng.Faixa(0.35f, 1.05f);
                b.Adicionar(colProto, Matrix4x4.TRS(new Vector3(p.x, h, p.y),
                    Quaternion.Euler(0f, rng.Faixa(0f, 360f), 0f), new Vector3(1f, sy, 1f)), tinta);
                Colunas++;
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
                    for (int l = 0; l < camadas; l++)
                    {
                        var q = Quaternion.Euler(0f, yaw + rng.Faixa(-7f, 7f), 0f);
                        b.Adicionar(blocoProto, Matrix4x4.TRS(new Vector3(p.x, h + 0.45f + l * 0.92f, p.y), q, Vector3.one),
                            Desgaste(rng, -0.16f, 0.08f));
                        Blocos++;
                    }
                    Pegadas.Add(new Vector4(p.x, p.y, 0f, 1.3f));
                }
            }

            // altar central: 2 blocos + toco de coluna
            float hc = r.Altura(c0.x, c0.y);
            b.Adicionar(blocoProto, Matrix4x4.TRS(new Vector3(c0.x, hc + 0.45f, c0.y), Quaternion.Euler(0f, 23f, 0f), Vector3.one), Color.white);
            b.Adicionar(blocoProto, Matrix4x4.TRS(new Vector3(c0.x + 0.3f, hc + 1.35f, c0.y - 0.2f), Quaternion.Euler(0f, 63f, 0f),
                new Vector3(0.7f, 1f, 0.7f)), Color.white);
            b.Adicionar(colProto, Matrix4x4.TRS(new Vector3(c0.x - 3.4f, hc, c0.y + 3f), Quaternion.identity, new Vector3(1f, 0.28f, 1f)), Color.white);
            Blocos += 2;

            Mesh malha = b.ParaMesh("Ruinas", linear);
            GameObject go = Filho("Ruinas", malha, pedra, ShadowCastingMode.On);
            go.AddComponent<MeshCollider>().sharedMesh = malha;
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

        GameObject Filho(string nome, Mesh malha, Material mat, ShadowCastingMode sombra)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = sombra;
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
