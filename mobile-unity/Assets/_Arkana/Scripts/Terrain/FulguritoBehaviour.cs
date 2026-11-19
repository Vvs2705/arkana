using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Terrain
{
    /// <summary>
    /// A casca do FULGURITO (ADR-0001): liga a classe pura a Ilha.Atual (areia = Bioma Dunas/Praia na ilha procedural; faixa de praia seca na IlhaMestre), ticka e DESENHA os
    /// vidros vivos — feixe de laminas de vidro de areia fundida (verde-palido translucido, base ambar de crosta queimada),
    /// pool de GlassMax objetos criado 1x, zero alocacao por quadro. Sem ilha na cena: plano sem areia (nada acontece).
    /// </summary>
    public sealed class FulguritoBehaviour : MonoBehaviour
    {
        [SerializeField] float ladoSemIlha = 180f;

        public static FulguritoBehaviour Atual { get; private set; }
        public Fulgurito Logica { get; private set; }

        static Mesh _laminas, _crosta;
        static Material _mVidro, _mCrosta;
        readonly List<Transform> _pool = new List<Transform>();

        /// <summary>Monta a logica (ja' ligada no Bus) e o pool. Chamado no Start; o teste de foto pode chamar antes.</summary>
        public void Montar()
        {
            if (Logica != null) return;
            Ilha ilha = Ilha.Atual;
            Relevo r = ilha != null ? ilha.Relevo : null;
            RelevoMestre mestre = ilha != null ? ilha.Mestre : null;
            if (r != null)
                Logica = new Fulgurito(r.Lado, (x, z) => { Bioma b = r.BiomaEm(x, z); return b == Bioma.Dunas || b == Bioma.Praia; });
            else if (mestre != null)
                Logica = new Fulgurito(mestre.Lado, (x, z) => AreiaDaMestre(mestre, x, z));   // a ilha do jogo hoje (Main.UsarIlhaMestre)
            else Logica = new Fulgurito(ladoSemIlha, null);
            Bus.MatchStarted += Logica.Reset;
            Atual = this;
            if (_laminas == null) { _laminas = Laminas(); _crosta = Crosta(); }
            if (_mVidro == null) _mVidro = MaterialVfx.Solido(new Color(0.72f, 0.93f, 0.84f, 0.62f), MaterialVfx.Mistura.Alfa, true);
            if (_mCrosta == null) _mCrosta = MaterialVfx.Solido(new Color(0.55f, 0.36f, 0.18f, 1f), MaterialVfx.Mistura.Opaco);
            for (int i = 0; i < Balance.Terrain.GlassMax; i++)
            {
                var go = new GameObject("Fulgurito" + i);
                go.transform.SetParent(transform, false);
                Parte(go.transform, _laminas, _mVidro);
                Parte(go.transform, _crosta, _mCrosta);
                go.SetActive(false);
                _pool.Add(go.transform);
            }
        }

        /// <summary>Areia da IlhaMestre: chao SECO na faixa de praia (PraiaY..AreiaMaxY). A camada "areia" do splat dela pesa mais
        /// que metade abaixo de ~3,75 m (IlhaMestre.Pesos: InverseLerp(5, 2.5, h)); a areia da beira do lago fica de fora.</summary>
        public const float AreiaMaxY = 3.75f;

        public static bool AreiaDaMestre(RelevoMestre m, float x, float z)
        {
            if (m.SuperficieDaAgua(x, z) != Relevo.Seco) return false;
            float h = m.Altura(x, z);
            return h >= Relevo.PraiaY && h <= AreiaMaxY;
        }

        static void Parte(Transform pai, Mesh m, Material mat)
        {
            var go = new GameObject(m.name);
            go.transform.SetParent(pai, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        void Start() => Montar();

        void Update()
        {
            if (Logica == null) return;
            Logica.Tick(Time.deltaTime);
            IReadOnlyList<VidroVisivel> vis = Logica.Visiveis;
            for (int i = 0; i < _pool.Count; i++)
            {
                Transform t = _pool[i];
                bool liga = i < vis.Count;
                if (t.gameObject.activeSelf != liga) t.gameObject.SetActive(liga);
                if (!liga) continue;
                VidroVisivel v = vis[i];
                Vector3 c = v.Centro;
                c.y = Ilha.AlturaDoChao(c.x, c.z) - 0.05f;
                float giro = (v.Semente % 360u);
                // nasce subindo (0,25 s) e afina nos ultimos 0,6 s: o jogador le' "vai sumir"
                float vida = Balance.Terrain.GlassDuration - v.Restante;
                float s = Mathf.Clamp01(vida / 0.25f) * Mathf.Clamp01(v.Restante / 0.6f);
                t.SetPositionAndRotation(c, Quaternion.Euler(0f, giro, 0f));
                t.localScale = new Vector3(1f, Mathf.Max(s, 0.02f), 1f);
            }
        }

        void OnDestroy()
        {
            if (Atual == this) Atual = null;
            if (Logica == null) return;
            Bus.MatchStarted -= Logica.Reset;
            Logica.Desligar();
        }

        // ------------------------------------------------------------ malhas (1x)

        /// <summary>Sete laminas finas inclinadas (prisma triangular afinando na ponta) — o feixe de vidro que o raio funde,
        /// ate' GlassHeight. Deterministico (semente fixa): a mesma forma em toda montagem.</summary>
        static Mesh Laminas()
        {
            var rng = new System.Random(Fulgurito.Seed);
            var v = new List<Vector3>();
            var t = new List<int>();
            float h = Balance.Terrain.GlassHeight, cel = Balance.Terrain.CellSize;
            for (int k = 0; k < 7; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = (float)rng.NextDouble() * cel * 0.3f;
                var b = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                float alt = h * (0.55f + 0.45f * (float)rng.NextDouble());
                float larg = 0.25f + 0.25f * (float)rng.NextDouble();
                var incl = new Vector3((float)rng.NextDouble() - 0.5f, 0f, (float)rng.NextDouble() - 0.5f) * 0.7f;
                Vector3 topo = b + incl + Vector3.up * alt;
                int o = v.Count;
                for (int j = 0; j < 3; j++)
                {
                    float aj = a + j * Mathf.PI * 2f / 3f;
                    v.Add(b + new Vector3(Mathf.Cos(aj), 0f, Mathf.Sin(aj)) * larg);
                }
                v.Add(topo);
                t.AddRange(new[] { o, o + 3, o + 1, o + 1, o + 3, o + 2, o + 2, o + 3, o });
            }
            var m = new Mesh { name = "FulguritoLaminas" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A crosta de areia queimada no pe' do vidro: um hexagono baixo da largura da celula.</summary>
        static Mesh Crosta()
        {
            var v = new List<Vector3> { new Vector3(0f, 0.12f, 0f) };
            var t = new List<int>();
            float r = Balance.Terrain.CellSize * 0.48f;
            for (int j = 0; j < 6; j++)
            {
                float a = j * Mathf.PI / 3f;
                v.Add(new Vector3(Mathf.Cos(a) * r, 0.02f, Mathf.Sin(a) * r));
            }
            for (int j = 0; j < 6; j++) t.AddRange(new[] { 0, 1 + (j + 1) % 6, 1 + j });
            var m = new Mesh { name = "FulguritoCrosta" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }
}
