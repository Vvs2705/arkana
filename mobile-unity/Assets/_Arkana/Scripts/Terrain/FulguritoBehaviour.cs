using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Terrain
{
    /// <summary>
    /// A casca do FULGURITO (ADR-0001): liga a classe pura a Ilha.Atual (areia = Bioma Dunas/Praia na ilha procedural; faixa de praia seca
    /// na IlhaMestre), ticka e DESENHA os vidros vivos. Visual do ciclo 2 (veto do CD no Modo B ciclo 1: "le' como gelo e nao tapa"):
    /// feixe de TUBOS rugosos e retorcidos de areia fundida — verde-garrafa/oliva acinzentado e ambar-fume, base queimada escura subindo
    /// pela peca, opacidade ~0,95 (tapa um mago de pe'), mais denso na altura do torso — e uma CROSTA irregular de areia vitrificada que
    /// acompanha o declive e se dissolve na areia (borda com alfa 0, sem hexagono duro).
    /// Pool de GlassMax objetos criado 1x; zero alocacao por quadro (a crosta so' e' reassentada quando o vidro do slot muda).
    /// Sem ilha na cena: plano sem areia (nada acontece).
    /// </summary>
    public sealed class FulguritoBehaviour : MonoBehaviour
    {
        [SerializeField] float ladoSemIlha = 180f;

        public static FulguritoBehaviour Atual { get; private set; }
        public Fulgurito Logica { get; private set; }

        /// <summary>Variantes de feixe (forma + paleta); o vidro escolhe pela Semente — duas praias nao repetem o mesmo desenho.</summary>
        public const int Variantes = 3;
        const int CrostaSegs = 12;

        static Mesh[] _feixes;
        static Material _mVidro;
        static Vector3[] _crostaBase;   // xz local da crosta (y = alfa do anel: 0 miolo, 1 meio, 2 borda)
        static Color[] _crostaCor;
        static int[] _crostaTris;

        sealed class Slot
        {
            public Transform Raiz, Feixe;
            public MeshFilter FiltroFeixe;
            public Mesh Crosta;
            public Vector3[] Verts;
            public int Idx = -1;
            public float Y;
        }
        readonly List<Slot> _pool = new List<Slot>();

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
            if (_feixes == null)
            {
                _feixes = new Mesh[Variantes];
                for (int k = 0; k < Variantes; k++) _feixes[k] = Feixe(k);
                MontarCrostaBase();
            }
            if (_mVidro == null) _mVidro = MaterialVfx.CorDeVertice(MaterialVfx.Mistura.Alfa);   // Particles/Unlit (lista do build): le' cor de vertice
            for (int i = 0; i < Balance.Terrain.GlassMax; i++)
            {
                var go = new GameObject("Fulgurito" + i);
                go.transform.SetParent(transform, false);
                var s = new Slot { Raiz = go.transform, Verts = new Vector3[_crostaBase.Length] };
                s.Feixe = Parte(go.transform, _feixes[0], "Feixe", out s.FiltroFeixe);
                s.Crosta = new Mesh { name = "FulguritoCrosta" + i };
                s.Crosta.MarkDynamic();
                s.Crosta.vertices = _crostaBase;
                s.Crosta.colors = _crostaCor;
                s.Crosta.triangles = _crostaTris;
                Parte(go.transform, s.Crosta, "Crosta", out _);
                go.SetActive(false);
                _pool.Add(s);
            }
        }

        /// <summary>Areia da IlhaMestre: chao SECO na faixa de praia (PraiaY..AreiaMaxY). A camada "areia" do splat dela pesa mais
        /// que metade abaixo de ~3,75 m (IlhaMestre.Pesos: InverseLerp(5, 2.5, h)); a areia da beira do lago fica de fora.</summary>
        public const float AreiaMaxY = 3.75f;

        public static bool AreiaDaMestre(RelevoMestre m, float x, float z) => AreiaDaMestre(m.Altura(x, z), m.SuperficieDaAgua(x, z));

        /// <summary>A REGRA, pura (EditMode: TerrainFulguritoAreiaTests): altura do chao e superficie da agua no ponto.</summary>
        public static bool AreiaDaMestre(float altura, float superficieDaAgua)
        {
            if (superficieDaAgua != Relevo.Seco) return false;   // mar ou lago por cima: molhado nao vitrifica
            return altura >= Relevo.PraiaY && altura <= AreiaMaxY;   // NaN cai fora (comparacao falsa)
        }

        static Transform Parte(Transform pai, Mesh m, string nome, out MeshFilter mf)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _mVidro;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        void Start() => Montar();

        void Update()
        {
            if (Logica == null) return;
            Logica.Tick(Time.deltaTime);
            IReadOnlyList<VidroVisivel> vis = Logica.Visiveis;
            for (int i = 0; i < _pool.Count; i++)
            {
                Slot s = _pool[i];
                bool liga = i < vis.Count;
                if (s.Raiz.gameObject.activeSelf != liga) s.Raiz.gameObject.SetActive(liga);
                if (!liga) { s.Idx = -1; continue; }
                VidroVisivel v = vis[i];
                if (s.Idx != v.Idx) Assentar(s, v);   // so' quando o slot troca de vidro: relevo amostrado 1x (nao 24x por quadro)
                // nasce subindo (GlassNascer) e afina nos ultimos 0,6 s: o jogador le' "vai sumir"
                float vida = Balance.Terrain.GlassDuration - v.Restante;
                float k = Mathf.Clamp01(vida / Balance.Terrain.GlassNascer) * Mathf.Clamp01(v.Restante / 0.6f);
                s.Feixe.localScale = new Vector3(1f, Mathf.Max(k, 0.02f), 1f);
            }
        }

        /// <summary>Poe o slot no vidro novo: altura do chao, giro e variante pela semente, e a crosta deitada no relevo real.</summary>
        void Assentar(Slot s, VidroVisivel v)
        {
            s.Idx = v.Idx;
            Vector3 c = v.Centro;
            s.Y = Ilha.AlturaDoChao(c.x, c.z);
            c.y = s.Y - 0.08f;
            Quaternion giro = Quaternion.Euler(0f, v.Semente % 360u, 0f);
            s.Raiz.SetPositionAndRotation(c, giro);
            s.FiltroFeixe.sharedMesh = _feixes[(int)((v.Semente >> 9) % (uint)Variantes)];
            for (int j = 0; j < _crostaBase.Length; j++)
            {
                Vector3 b = _crostaBase[j];
                Vector3 w = c + giro * new Vector3(b.x, 0f, b.z);
                s.Verts[j] = new Vector3(b.x, Ilha.AlturaDoChao(w.x, w.z) - c.y + 0.03f + (b.y < 0.5f ? 0.03f : 0f), b.z);
            }
            s.Crosta.vertices = s.Verts;   // array do slot (sem alocar); acompanha o declive celula a celula
            s.Crosta.RecalculateBounds();
        }

        void OnDestroy()
        {
            if (Atual == this) Atual = null;
            for (int i = 0; i < _pool.Count; i++) if (_pool[i].Crosta != null) Destroy(_pool[i].Crosta);
            if (Logica == null) return;
            Bus.MatchStarted -= Logica.Reset;
            Logica.Desligar();
        }

        // ------------------------------------------------------------ malhas (1x)

        // paletas de areia fundida (fulgurito real: vidro de silica impura — verde-garrafa, oliva, ambar-fume; nunca ciano)
        static readonly Color Queimado = new Color(0.09f, 0.07f, 0.05f);
        static readonly Color[][] Paletas =
        {
            new[] { new Color(0.20f, 0.28f, 0.09f), new Color(0.27f, 0.31f, 0.15f), new Color(0.42f, 0.27f, 0.07f) },   // oliva + ambar
            new[] { new Color(0.11f, 0.23f, 0.08f), new Color(0.19f, 0.27f, 0.11f), new Color(0.22f, 0.24f, 0.14f) },   // verde-garrafa
            new[] { new Color(0.45f, 0.26f, 0.06f), new Color(0.36f, 0.22f, 0.08f), new Color(0.24f, 0.29f, 0.12f) },   // ambar-fume
        };

        /// <summary>Feixe de TUBOS rugosos (o fulgurito real e' um tubo de areia fundida): 9 pecas de 5 lados em 3 segmentos, raio que
        /// afina ate' uma ponta ROMBUDA (tampa, sem bico), eixo que entorta e gira, raio de cada anel com ruido. Mais largas e juntas
        /// entre 0,6 e 1,7 m (o torso de um mago de pe'). Alfa 0,95 no corpo: tapa. ~35 tris por peca.</summary>
        static Mesh Feixe(int variante)
        {
            var rng = new System.Random(Fulgurito.Seed + 101 * variante);
            float R() => (float)rng.NextDouble();
            Color[] pal = Paletas[variante % Paletas.Length];
            var v = new List<Vector3>();
            var cor = new List<Color>();
            var t = new List<int>();
            float h = Balance.Terrain.GlassHeight, cel = Balance.Terrain.CellSize;
            const int lados = 5, niveis = 4;
            for (int k = 0; k < 9; k++)
            {
                float a = R() * Mathf.PI * 2f, d = Mathf.Sqrt(R()) * cel * 0.38f;
                var b = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                float alt = h * (k < 3 ? 0.85f + 0.15f * R() : 0.45f + 0.45f * R());   // 3 altas cobrem a cabeca
                float r0 = 0.24f + 0.2f * R();
                var incl = new Vector3(R() - 0.5f, 0f, R() - 0.5f) * 0.9f;
                var torce = new Vector3(R() - 0.5f, 0f, R() - 0.5f) * 0.5f;
                Color corpo = pal[rng.Next(pal.Length)];
                float giro0 = R() * Mathf.PI * 2f;
                int o = v.Count;
                for (int n = 0; n < niveis; n++)
                {
                    float f = n / (float)(niveis - 1);
                    Vector3 eixo = b + incl * f + torce * Mathf.Sin(f * Mathf.PI) + Vector3.up * (alt * f);
                    float raio = r0 * Mathf.Lerp(1f, 0.45f, f) * (n == 1 ? 1.15f : 1f);   // barriga no torso
                    Color cn = Color.Lerp(Queimado, corpo, Mathf.SmoothStep(0f, 1f, f * 1.15f));
                    cn = Color.Lerp(cn, Color.white, 0.05f * f);   // ponta um pouco mais clara (a luz atravessa a ponta)
                    cn.a = n == 0 ? 1f : 0.95f;
                    for (int j = 0; j < lados; j++)
                    {
                        float aj = giro0 + f * 1.1f + j * Mathf.PI * 2f / lados;
                        float rj = raio * (0.75f + 0.5f * R());
                        v.Add(eixo + new Vector3(Mathf.Cos(aj), 0f, Mathf.Sin(aj)) * rj);
                        cor.Add(cn);
                    }
                }
                for (int n = 0; n < niveis - 1; n++)
                    for (int j = 0; j < lados; j++)
                    {
                        int a0 = o + n * lados + j, a1 = o + n * lados + (j + 1) % lados;
                        int b0 = a0 + lados, b1 = a1 + lados;
                        t.Add(a0); t.Add(b0); t.Add(a1);
                        t.Add(a1); t.Add(b0); t.Add(b1);
                    }
                int topo = o + (niveis - 1) * lados;
                for (int j = 1; j < lados - 1; j++) { t.Add(topo); t.Add(topo + j + 1); t.Add(topo + j); }   // tampa: ponta rombuda
            }
            var m = new Mesh { name = "FulguritoFeixe" + variante };
            m.SetVertices(v); m.SetColors(cor); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A crosta: mancha irregular de areia queimada/vitrificada (miolo escuro e brilhoso -> anel areia-tostada -> borda
        /// cor de areia com alfa 0, que se dissolve no chao). O y de cada vertice e' o do relevo (Assentar).</summary>
        static void MontarCrostaBase()
        {
            var rng = new System.Random(Fulgurito.Seed + 7);
            float R = Balance.Terrain.CellSize * 0.62f;
            int n = 1 + 2 * CrostaSegs;
            _crostaBase = new Vector3[n];
            _crostaCor = new Color[n];
            _crostaBase[0] = Vector3.zero;
            _crostaCor[0] = new Color(0.20f, 0.17f, 0.12f, 0.92f);
            for (int j = 0; j < CrostaSegs; j++)
            {
                float a = j * Mathf.PI * 2f / CrostaSegs;
                float ri = R * (0.38f + 0.18f * (float)rng.NextDouble()), re = R * (0.8f + 0.4f * (float)rng.NextDouble());
                _crostaBase[1 + j] = new Vector3(Mathf.Cos(a) * ri, 1f, Mathf.Sin(a) * ri);
                _crostaCor[1 + j] = new Color(0.36f, 0.29f, 0.19f, 0.8f);
                _crostaBase[1 + CrostaSegs + j] = new Vector3(Mathf.Cos(a) * re, 2f, Mathf.Sin(a) * re);
                _crostaCor[1 + CrostaSegs + j] = new Color(0.62f, 0.52f, 0.36f, 0f);
            }
            var t = new List<int>();
            for (int j = 0; j < CrostaSegs; j++)
            {
                int i0 = 1 + j, i1 = 1 + (j + 1) % CrostaSegs, e0 = i0 + CrostaSegs, e1 = i1 + CrostaSegs;
                t.Add(0); t.Add(i1); t.Add(i0);
                t.Add(i0); t.Add(i1); t.Add(e0);
                t.Add(e0); t.Add(i1); t.Add(e1);
            }
            _crostaTris = t.ToArray();
        }
    }
}
