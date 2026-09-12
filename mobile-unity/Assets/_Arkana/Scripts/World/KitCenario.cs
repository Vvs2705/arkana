using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Characters;

namespace Arkana.World
{
    /// <summary>Onde uma peca do kit pode nascer. Cada valor e' uma REGRA DE DESIGN (KitCenario.gd), nao um filtro tecnico.</summary>
    public enum OndeNasce
    {
        /// <summary>Qualquer chao seco da ilha.</summary>
        Aberto,
        /// <summary>Na beira do lago (ate' 1,5 raio), sempre no SECO: arco de calcario que a agua esculpiu.</summary>
        Agua,
        /// <summary>Dentro da mata.</summary>
        Floresta,
        /// <summary>O ponto mais alto de 24 sorteios: marco alto em terreno alto e' o que se ve' da queda.</summary>
        Alto,
        /// <summary>Dentro de um POI qualquer.</summary>
        Poi,
    }

    /// <summary>Uma peca esculpida na Meshy. Os numeros de design sao do KitCenario.gd.</summary>
    public sealed class PecaDoKit
    {
        public readonly string Id;
        /// <summary>Altura REAL no mundo (m). A Meshy normaliza tudo pra ~1,9 unidade: este e' o unico numero que diz se e' cobertura ou paredao.</summary>
        public readonly float AlturaM;
        /// <summary>Copias na ilha de REFERENCIA (raio 132 m). Planta x AREA.</summary>
        public readonly int N;
        /// <summary>O corpo bate? Arco e piso de runa nao (o arco e' passagem: colisor nele viraria parede).</summary>
        public readonly bool Colide;
        public readonly OndeNasce Onde;
        /// <summary>Variacao de escala por copia (+-).</summary>
        public readonly float EscalaVar;
        /// <summary>Meia largura em planta na AlturaM (m), medida nos bounds dos .glb (proporcao da Meshy x AlturaM). Serve a folga, nao ao desenho.</summary>
        public readonly float RaioM;

        public PecaDoKit(string id, float alturaM, int n, bool colide, OndeNasce onde, float escalaVar, float raioM)
        {
            Id = id; AlturaM = alturaM; N = n; Colide = colide; Onde = onde; EscalaVar = escalaVar; RaioM = raioM;
        }

        /// <summary>Marco de navegacao (le'-se de longe) ou detalhe de chao (some cedo).</summary>
        public bool Marco => AlturaM >= 3f;
    }

    /// <summary>Uma copia plantada: onde (o pe' da peca ja' na cota), giro em Y (graus) e fator de escala (1 +- var).</summary>
    public struct Plantio
    {
        public Vector3 Pos;
        public float GiroGraus;
        public float Escala;
    }

    /// <summary>
    /// O PLANTIO do kit, PURO e deterministico por seed: onde cada peca nasce, quantas, com que giro.
    /// Regras: contagem x AREA; nunca na agua (o centro tem que ser pousavel); nunca em cima de um
    /// nascimento (folga = FolgaDoNascimento + meia largura da peca); copias da mesma peca afastadas
    /// 1,6 x altura (Godot); e, opcional, sem invadir o que ja' foi plantado (`ocupados`).
    /// </summary>
    public static class PlantioDoKit
    {
        public const float RaioRef = 132f;
        public const int SeedKit = 4801;
        /// <summary>Folga livre em volta de cada nascimento, alem da meia largura da peca (m): o mago nasce no aberto, nunca dentro da rocha.</summary>
        public const float FolgaDoNascimento = 6f;
        /// <summary>Corte por distancia (m, 3D ao centro da celula, como o visibility_range do Godot). 363k tris com 220/90 no Godot; 240/70 cabe.</summary>
        public const float CorteMarco = 240f, CorteChao = 70f;

        public static readonly PecaDoKit[] Pecas =
        {
            new PecaDoKit("17-rocha-basalto-modular", 15f, 11, true, OndeNasce.Aberto, 0.35f, 11.5f),
            new PecaDoKit("18-rocha-vulcanica-cobertura", 7f, 18, true, OndeNasce.Aberto, 0.30f, 7.2f),
            new PecaDoKit("19-arco-calcario-nymara", 14f, 4, false, OndeNasce.Agua, 0.15f, 8.5f),
            new PecaDoKit("20-ponte-raiz-aeris", 4.2f, 3, true, OndeNasce.Floresta, 0.20f, 10f),
            new PecaDoKit("21-pilar-condutor-fulgar", 28f, 5, true, OndeNasce.Alto, 0.12f, 5.8f),
            new PecaDoKit("28-pedestal-de-arma", 2.6f, 5, true, OndeNasce.Poi, 0f, 1.1f),
            new PecaDoKit("30-arvore-carbonizada-renascendo", 12f, 12, true, OndeNasce.Aberto, 0.30f, 4.2f),
            new PecaDoKit("32-piso-runa-reativa", 0.6f, 7, false, OndeNasce.Poi, 0f, 3.3f),
        };

        /// <summary>Quantas copias a peca PEDE nesta ilha: N x (raio / raio de referencia)^2. Mapa 2x maior, 4x pecas.</summary>
        public static int Alvo(Relevo relevo, PecaDoKit peca)
        {
            float k = relevo.RaioTerra / RaioRef;
            return Mathf.RoundToInt(peca.N * k * k);
        }

        /// <summary>Raio horizontal em volta de um nascimento onde a peca nao pode ter o centro.</summary>
        public static float RaioLivre(PecaDoKit peca) => FolgaDoNascimento + peca.RaioM * (1f + peca.EscalaVar);

        public static List<Plantio> Posicoes(Relevo relevo, int seed, PecaDoKit peca)
        {
            return Posicoes(relevo, seed, peca, null);
        }

        /// <summary>`ocupados` (x, z, raio em w) acumula o que ja' foi plantado: peca de tipo diferente nao nasce dentro de outra.</summary>
        public static List<Plantio> Posicoes(Relevo relevo, int seed, PecaDoKit peca, List<Vector4> ocupados)
        {
            var lista = new List<Plantio>();
            if (relevo == null || peca == null) return lista;
            var rng = new Sorteio(Mistura(seed, peca.Id));
            int alvo = Alvo(relevo, peca);
            float espaco = peca.AlturaM * 1.6f;
            float livre = RaioLivre(peca);
            int tentativas = 0;
            while (lista.Count < alvo && tentativas < alvo * 40)
            {
                tentativas++;
                Vector2 p = Sortear(rng, relevo, peca.Onde);
                // o sorteio de escala e giro sai SEMPRE: a sequencia do rng nao depende de quem foi recusado antes
                float e = 1f + rng.Faixa(-1f, 1f) * peca.EscalaVar;
                float giro = rng.Float() * 360f;
                if (!relevo.PodePousar(p.x, p.y)) continue;
                if (PertoDeNascimento(relevo, p, livre)) continue;
                if (PertoDaLista(lista, p, espaco)) continue;
                float raio = peca.RaioM * e;
                if (ocupados != null && Invade(ocupados, p, raio)) continue;
                // pe' na MENOR cota da pegada, afundado 4% da altura: peca pousada exata mostra fresta na encosta
                float h = BaseNoChao(relevo, p, raio * 0.5f) - peca.AlturaM * e * 0.04f;
                lista.Add(new Plantio { Pos = new Vector3(p.x, h, p.y), GiroGraus = giro, Escala = e });
                if (ocupados != null) ocupados.Add(new Vector4(p.x, p.y, 0f, raio));
            }
            return lista;
        }

        static Vector2 Sortear(Sorteio rng, Relevo relevo, OndeNasce onde)
        {
            float a = rng.Float() * Mathf.PI * 2f;
            float u = Mathf.Sqrt(rng.Float());   // raiz: densidade parelha no disco
            switch (onde)
            {
                case OndeNasce.Poi:
                {
                    Poi poi = relevo.Pois[rng.Int(relevo.Pois.Length)];
                    return poi.Centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (u * poi.Raio);
                }
                case OndeNasce.Floresta:
                    return relevo.Floresta + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (u * relevo.FlorestaR);
                case OndeNasce.Agua:
                    return relevo.Lago + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (u * relevo.LagoR * 1.5f);
                case OndeNasce.Alto:
                {
                    Vector2 melhor = Vector2.zero;
                    float melhorH = -1e9f;
                    for (int i = 0; i < 24; i++)
                    {
                        float a2 = rng.Float() * Mathf.PI * 2f;
                        float d2 = Mathf.Sqrt(rng.Float()) * relevo.RaioTerra * 0.9f;
                        var q = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * d2;
                        float hq = relevo.Altura(q.x, q.y);
                        if (hq > melhorH) { melhorH = hq; melhor = q; }
                    }
                    return melhor;
                }
                default:
                    return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (u * relevo.RaioTerra * 0.92f);
            }
        }

        static bool PertoDeNascimento(Relevo relevo, Vector2 p, float raio)
        {
            Vector3[] ns = relevo.Nascimentos;
            for (int i = 0; i < ns.Length; i++)
            {
                float dx = ns[i].x - p.x, dz = ns[i].z - p.y;
                if (dx * dx + dz * dz < raio * raio) return true;
            }
            return false;
        }

        static bool PertoDaLista(List<Plantio> lista, Vector2 p, float d)
        {
            for (int i = 0; i < lista.Count; i++)
            {
                float dx = lista[i].Pos.x - p.x, dz = lista[i].Pos.z - p.y;
                if (dx * dx + dz * dz < d * d) return true;
            }
            return false;
        }

        static bool Invade(List<Vector4> ocupados, Vector2 p, float raio)
        {
            for (int i = 0; i < ocupados.Count; i++)
            {
                float dx = ocupados[i].x - p.x, dz = ocupados[i].y - p.y, r = ocupados[i].w + raio;
                if (dx * dx + dz * dz < r * r) return true;
            }
            return false;
        }

        static float BaseNoChao(Relevo relevo, Vector2 p, float r)
        {
            float h = relevo.Altura(p.x, p.y);
            h = Mathf.Min(h, relevo.Altura(p.x + r, p.y));
            h = Mathf.Min(h, relevo.Altura(p.x - r, p.y));
            h = Mathf.Min(h, relevo.Altura(p.x, p.y + r));
            return Mathf.Min(h, relevo.Altura(p.x, p.y - r));
        }

        /// <summary>Seed por peca, estavel entre runtimes (string.GetHashCode nao e': o .NET o embaralha por processo).</summary>
        static int Mistura(int seed, string id)
        {
            unchecked
            {
                uint h = 2166136261u;
                for (int i = 0; i < id.Length; i++) h = (h ^ id[i]) * 16777619u;
                return (int)(h ^ ((uint)seed * 2654435761u));
            }
        }
    }

    /// <summary>
    /// A casca do kit: carrega cada .glb de Resources, mede a peca (Renderer.bounds) e escala pela altura
    /// real, instancia nas posicoes do PlantioDoKit, doma o metal da Meshy e junta tudo por StaticBatching.
    /// COLISAO: a forma real (MeshCollider) quando a malha e' legivel; senao a caixa INTEIRA dos bounds.
    /// A caixa encolhida 15% (a opcao "barata" da 1a versao) deixava a camera entrar na peca (foto de 11/09).
    /// CORTE por celula de 55 m (4 Hz): marco a 240 m, detalhe de chao a 70 m (KitCenario.gd).
    /// Sem o .glb, a peca nao nasce — a ilha boota igual (fiacao defensiva).
    /// </summary>
    public sealed class KitCenario : MonoBehaviour
    {
        const float PassoCelula = 55f;

        sealed class Celula
        {
            public Vector3 Centro;
            public readonly List<Renderer> Marcos = new List<Renderer>();
            public readonly List<Renderer> Chao = new List<Renderer>();
            public bool MarcosLigados = true, ChaoLigado = true;
        }

        readonly Dictionary<long, Celula> celulas = new Dictionary<long, Celula>();
        readonly Dictionary<string, int> porPeca = new Dictionary<string, int>();
        float relogio;

        public int Total { get; private set; }
        public int Contar(string id) => porPeca.TryGetValue(id, out int n) ? n : 0;

        public void Montar(Relevo relevo, int seed = PlantioDoKit.SeedKit)
        {
            celulas.Clear();
            porPeca.Clear();
            Total = 0;
            if (relevo == null) return;
            var ocupados = new List<Vector4>();
            // o miolo das ruinas (colunas e muros, Ruinas.cs) fica de fora; a folga dos nascimentos o plantio ja' guarda
            ocupados.Add(new Vector4(relevo.Ruinas.x, relevo.Ruinas.y, 0f, relevo.RuinasR * 0.7f));
            bool legivel = true;
            foreach (PecaDoKit peca in PlantioDoKit.Pecas)
            {
                GameObject prefab = Resources.Load<GameObject>(peca.Id);
                if (prefab == null) continue;
                List<Plantio> pontos = PlantioDoKit.Posicoes(relevo, seed, peca, ocupados);
                if (pontos.Count == 0) continue;
                if (!Medir(prefab, out float alturaMalha, out float peMalha)) continue;
                float escalaBase = peca.AlturaM / alturaMalha;
                Material domado = null;
                int n = 0;
                for (int i = 0; i < pontos.Count; i++)
                {
                    Plantio pl = pontos[i];
                    float s = escalaBase * pl.Escala;
                    // o pe' da malha (bounds.min.y) vai pra cota do plantio
                    Vector3 pos = pl.Pos - Vector3.up * (peMalha * s);
                    GameObject go = Instantiate(prefab, pos, Quaternion.Euler(0f, pl.GiroGraus, 0f), transform);
                    go.name = peca.Id;
                    go.transform.localScale = go.transform.localScale * s;
                    Renderer[] rs = go.GetComponentsInChildren<Renderer>();
                    for (int r = 0; r < rs.Length; r++)
                    {
                        // ponytail: um material por peca (a Meshy entrega um); peca multi-material pediria um domado por slot
                        if (domado == null && rs[r].sharedMaterial != null) domado = Domado(rs[r].sharedMaterial);
                        if (domado != null) rs[r].sharedMaterial = domado;
                        rs[r].shadowCastingMode = ShadowCastingMode.Off;   // kit nao paga sombra (Godot)
                        Registrar(rs[r], pos, peca.Marco);
                    }
                    MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
                    for (int m = 0; m < mfs.Length; m++)
                    {
                        Mesh malha = mfs[m].sharedMesh;
                        if (malha == null) continue;
                        if (!malha.isReadable) legivel = false;
                        if (!peca.Colide) continue;
                        // A FORMA REAL quando a malha e' legivel. A caixa encolhida (e o cilindro do Godot, ainda menor)
                        // deixava corpo e CAMERA entrarem na sobra da malha: a foto de 11/09 (seed 3103) pousou o jogador
                        // dentro de uma arvore carbonizada e a tela virou o avesso da peca. Malha estatica no PhysX e'
                        // barata; o custo que o Godot temia era da fisica dele. Sem malha legivel (build), a caixa INTEIRA:
                        // parede invisivel na borda da rocha e' melhor que camera dentro dela.
                        if (malha.isReadable)
                        {
                            // A MALHA REAL (12/09). O casco convexo existia porque a malha DECIMADA tinha triangulo com a
                            // volta trocada e o raio da Queda atravessava o topo (diag de 11/09). O kit agora vem do REMESH
                            // do site (fechado, volta coerente) e a sonda da Queda acerta o verso de qualquer jeito
                            // (queriesHitBackfaces). E o casco de 10K triangulos estoura o limite de 255 faces do PhysX
                            // ("partial hull", aviso que reprova o BootTests). Ganho de brinde: a ponte-raiz tem vao.
                            var mc = mfs[m].gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = malha;
                            continue;
                        }
                        Bounds b = malha.bounds;   // espaco da malha: a caixa gira e escala com a peca
                        BoxCollider bc = mfs[m].gameObject.AddComponent<BoxCollider>();
                        bc.center = b.center;
                        bc.size = b.size;
                    }
                    n++;
                }
                porPeca[peca.Id] = n;
                Total += n;
            }
            // Um lote por material: 8 materiais, as copias viram poucos draw calls. Malha nao legivel nao
            // combina (o Unity reclamaria no log): a peca desenha solta, mas desenha.
            if (Total > 0 && legivel) StaticBatchingUtility.Combine(gameObject);
        }

        /// <summary>Instancia na origem, sem giro, e le' a altura e o pe' pelos Renderer.bounds (o que o glb traz de hierarquia conta junto).</summary>
        static bool Medir(GameObject prefab, out float altura, out float pe)
        {
            altura = 0f;
            pe = 0f;
            GameObject tmp = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Renderer[] rs = tmp.GetComponentsInChildren<Renderer>();
            bool tem = false;
            Bounds b = new Bounds();
            for (int i = 0; i < rs.Length; i++)
            {
                if (!tem) { b = rs[i].bounds; tem = true; }
                else b.Encapsulate(rs[i].bounds);
            }
            tmp.SetActive(false);   // o Destroy so' vale no fim do quadro: desligado, o molde nao aparece na origem
            if (Application.isPlaying) Destroy(tmp); else DestroyImmediate(tmp);
            if (!tem || !(b.size.y > 0.001f)) return false;
            altura = b.size.y;
            pe = b.min.y;
            return true;
        }

        /// <summary>
        /// O GRAMPO DO METAL (a licao do mago preto): a Meshy entrega metallicFactor 1,0 e metal sem probe
        /// vira breu. Copia, nao altera o material importado (em Play no editor isso sujaria o asset).
        /// </summary>
        static Material Domado(Material original)
        {
            // DOIS LADOS (foto de 11/09): a malha decimada da Meshy tem triangulo com a volta trocada, e o glTFast so'
            // desenha os dois lados quando o .glb pede — a peca saia ESTILHACADA, com o chao aparecendo por dentro.
            // URP Lit com Render Face = Both e a textura base do glTF. Sem o Lit, cai na copia do original (como antes).
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null)
            {
                var d = new Material(lit) { name = original.name + " (domado)" };
                Texture tex = original.HasProperty("baseColorTexture") ? original.GetTexture("baseColorTexture") : original.mainTexture;
                Color cor = original.HasProperty("baseColorFactor") ? original.GetColor("baseColorFactor") : Color.white;
                d.SetTexture("_BaseMap", tex);
                d.SetColor("_BaseColor", cor);
                d.SetFloat("_Cull", 0f);   // 0 = Off: frente e verso
                d.SetFloat("_Smoothness", 0.25f);   // pedra fosca; o padrao 0,5 do Lit deixa rocha com cara de plastico
                d.doubleSidedGI = true;
                d.enableInstancing = true;
                MaterialMago.Domar(d);     // metal <= 0,2 e liso <= 0,55: a licao do mago preto
                return d;
            }
            var m = new Material(original) { name = original.name + " (domado)" };
            if (m.HasProperty("metallicFactor")) m.SetFloat("metallicFactor", Mathf.Min(m.GetFloat("metallicFactor"), MaterialMago.MetallicMax));
            if (m.HasProperty("roughnessFactor")) m.SetFloat("roughnessFactor", Mathf.Max(m.GetFloat("roughnessFactor"), 1f - MaterialMago.SmoothnessMax));
            MaterialMago.Domar(m);   // _Metallic/_Smoothness, se o importador usou o URP Lit
            return m;
        }

        void Registrar(Renderer r, Vector3 pos, bool marco)
        {
            int cx = Mathf.FloorToInt(pos.x / PassoCelula), cz = Mathf.FloorToInt(pos.z / PassoCelula);
            long k = ((long)cx << 32) ^ (uint)cz;
            if (!celulas.TryGetValue(k, out Celula c))
            {
                c = new Celula { Centro = new Vector3((cx + 0.5f) * PassoCelula, pos.y, (cz + 0.5f) * PassoCelula) };
                celulas[k] = c;
            }
            (marco ? c.Marcos : c.Chao).Add(r);
        }

        void Update()
        {
            if (celulas.Count == 0) return;
            relogio -= Time.deltaTime;
            if (relogio > 0f) return;
            relogio = 0.25f;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 olho = cam.transform.position;
            foreach (Celula c in celulas.Values)
            {
                float d2 = (olho - c.Centro).sqrMagnitude;
                bool marcos = d2 < PlantioDoKit.CorteMarco * PlantioDoKit.CorteMarco;
                bool chao = d2 < PlantioDoKit.CorteChao * PlantioDoKit.CorteChao;
                if (marcos != c.MarcosLigados) { c.MarcosLigados = marcos; Ligar(c.Marcos, marcos); }
                if (chao != c.ChaoLigado) { c.ChaoLigado = chao; Ligar(c.Chao, chao); }
            }
        }

        static void Ligar(List<Renderer> rs, bool on)
        {
            for (int i = 0; i < rs.Count; i++) if (rs[i] != null) rs[i].enabled = on;
        }
    }
}
