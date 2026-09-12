using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arkana.World
{
    /// <summary>
    /// A malha do terreno como a GPU a desenha, PURA: alturas nos (Quads+1)^2 vertices e a MESMA
    /// divisao de triangulo da Ilha. Existe por dois motivos: (1) o tufo pousa na superficie DESENHADA
    /// — a Altura() exata passa ate' ~0,3 m acima/abaixo do quad de 4,5 m nas dobras, e tufo boiando ou
    /// enterrado le' como defeito; (2) bioma e inclinacao por no', calculados UMA vez: perguntar BiomaEm()
    /// por candidato custaria ~3 milhoes de Altura() no load do celular.
    /// </summary>
    public sealed class GradeDoChao
    {
        public readonly Relevo Relevo;
        public readonly int Quads;
        public readonly float Lado;
        readonly int n1;
        readonly float[] h;
        readonly float[] ny;
        readonly Bioma[] bioma;

        public GradeDoChao(Relevo relevo, int quads = Ilha.Quads)
        {
            Relevo = relevo;
            Quads = Mathf.Max(1, quads);
            Lado = relevo.Lado;
            n1 = Quads + 1;
            h = new float[n1 * n1];
            ny = new float[n1 * n1];
            bioma = new Bioma[n1 * n1];
            for (int iz = 0; iz < n1; iz++)
            {
                for (int ix = 0; ix < n1; ix++)
                {
                    float x = ((float)ix / Quads - 0.5f) * Lado;   // a mesma conta da Ilha.MontarTerreno
                    float z = ((float)iz / Quads - 0.5f) * Lado;
                    int i = iz * n1 + ix;
                    h[i] = relevo.Altura(x, z);
                    ny[i] = relevo.NormalY(x, z);
                    bioma[i] = relevo.BiomaEm(x, z);
                }
            }
        }

        void Celula(float x, float z, out int ix, out int iz, out float u, out float v)
        {
            float fx = Mathf.Clamp((x / Lado + 0.5f) * Quads, 0f, Quads - 1e-4f);
            float fz = Mathf.Clamp((z / Lado + 0.5f) * Quads, 0f, Quads - 1e-4f);
            ix = (int)fx;
            iz = (int)fz;
            u = fx - ix;
            v = fz - iz;
        }

        /// <summary>Altura NA MALHA: triangulos (a,c,b) e (c,d,b), exatamente como a Ilha os escreve.</summary>
        public float AlturaNaMalha(float x, float z)
        {
            Celula(x, z, out int ix, out int iz, out float u, out float v);
            int a = iz * n1 + ix, b = a + 1, c = a + n1, d = c + 1;
            if (u + v <= 1f) return h[a] + (h[b] - h[a]) * u + (h[c] - h[a]) * v;
            return h[d] + (h[c] - h[d]) * (1f - u) + (h[b] - h[d]) * (1f - v);
        }

        int NoMaisPerto(float x, float z)
        {
            int ix = Mathf.Clamp(Mathf.RoundToInt((x / Lado + 0.5f) * Quads), 0, Quads);
            int iz = Mathf.Clamp(Mathf.RoundToInt((z / Lado + 0.5f) * Quads), 0, Quads);
            return iz * n1 + ix;
        }

        public Bioma BiomaEm(float x, float z) => bioma[NoMaisPerto(x, z)];
        public float NormalY(float x, float z) => ny[NoMaisPerto(x, z)];
    }

    /// <summary>Uma instancia de chao: posicao, giro (Euler em GRAUS), escala e tinta (multiplicador, ou cor se a camada diz).</summary>
    public struct Tufo
    {
        public Vector3 Pos;
        public Vector3 Giro;
        public Vector3 Escala;
        public Color Tinta;
    }

    /// <summary>
    /// ONDE nasce cada coisa de chao — grama, flor, junco, seixo —, PURO e deterministico por seed.
    /// Os numeros sao do Island.gd (contagem x AREA, moita de grama, corte por celula); o que e' novo:
    /// a grama so' nasce em chao POUSAVEL (o Godot aceitava 1,05 m, abaixo da praia) e a densidade
    /// varia por BIOMA (a campina e' a referencia; a mata tem chao de serrapilheira e moitas).
    /// </summary>
    public static class PlantioDaGrama
    {
        /// <summary>Corte da CELULA, na horizontal (m). Numeros do Godot: flor e' pixel de cor, seixo some cedo, grama e junco a 80.</summary>
        public const float CorteGrama = 80f, CorteFlor = 45f, CorteSeixo = 55f, CorteJunco = 80f;
        /// <summary>Largura da rampa do colapso no shader (Godot: 42 -> 56 m).</summary>
        public const float RampaDoFade = 14f;

        /// <summary>A grade escala com o mapa: a celula continua com ~30 m (6-9 no frustum de 3a pessoa).</summary>
        public static int CelulasPorLado(Relevo r) => Mathf.Max(1, (int)(10 * r.Escala));

        /// <summary>
        /// O fade TEM que acabar antes do corte menos meia diagonal da celula: um tufo na quina de uma
        /// celula cortada fica a (corte - meia diagonal) do olho e ja' tem que estar achatado. Sem isso,
        /// tufo em pe' some de uma vez (pop). Com a grama: 80 - 21 = 59 m, o 56 do Godot.
        /// </summary>
        public static float FimDoFade(float corte, float passoCelula) => corte - passoCelula * 0.70710678f;

        /// <summary>
        /// VISIVEL = distancia HORIZONTAL do olho ao centro da celula menor que o corte. A licao da leva 6:
        /// com as celulas todas na origem o corte so' funcionava na vertical (tudo-ou-nada). A altura fica
        /// fora de proposito — quem apaga a grama no alto e' o colapso 3D do shader.
        /// </summary>
        public static bool Visivel(Vector3 olho, Vector3 centroCelula, float corte)
        {
            float dx = olho.x - centroCelula.x, dz = olho.z - centroCelula.z;
            return dx * dx + dz * dz < corte * corte;
        }

        /// <summary>Fracao da densidade da campina aceita em cada bioma. Agua, praia e brejo: zero (o brejo tem junco).</summary>
        public static float Densidade(Bioma b)
        {
            switch (b)
            {
                case Bioma.Campina: return 1f;
                case Bioma.Ruinas: return 0.6f;    // so' a borda: o miolo e' piso de pedra (excluido abaixo)
                case Bioma.Floresta: return 0.5f;  // serrapilheira sob a copa; quem enche o chao da mata e' a moita
                case Bioma.Pico: return 0.4f;      // cume batido de vento
                case Bioma.Rocha: return 0.25f;
                case Bioma.Dunas: return 0.15f;    // o areal e' o campo ABERTO do mapa: capim ralo so' na franja
                default: return 0f;
            }
        }

        static float Area(Relevo r) => r.Escala * r.Escala;

        /// <summary>Campina aberta do Godot (_open_ground), com o piso na PRAIA. -1 = nao.</summary>
        static float ChaoAberto(GradeDoChao g, Vector2 p, float hmin, float hmax, float nymin)
        {
            Relevo r = g.Relevo;
            if (p.magnitude > r.RaioTerra - 6f || Vector2.Distance(p, r.Lago) < r.LagoR + 2f
                || Vector2.Distance(p, r.Alagado) < r.AlagadoR) return -1f;
            // o areal e o piso das ruinas sao POI que se le' de cima por serem LISOS
            if (Vector2.Distance(p, r.Dunas) < r.DunasR * 0.85f) return -1f;
            if (Vector2.Distance(p, r.Ruinas) < r.RuinasR * 0.8f) return -1f;
            float h = g.AlturaNaMalha(p.x, p.y);
            if (h < hmin || h > hmax || g.NormalY(p.x, p.y) < nymin) return -1f;
            return h;
        }

        /// <summary>
        /// GRAMA EM MOITA: 88% dos tufos caem em volta de uma semente (raio curto 1,2-3,0 m) — sorteio
        /// uniforme le' como cones num campo de golfe. 30.000 tufos por ilha de referencia, x AREA.
        /// </summary>
        public static List<Tufo> Grama(GradeDoChao g, int seed = 61)
        {
            Relevo r = g.Relevo;
            var rng = new Sorteio(seed);
            float area = Area(r), L = r.RaioTerra;
            var sementes = new List<Vector2>();
            int tentativas = 0;
            while (sementes.Count < (int)(1180 * area) && tentativas < (int)(16000 * area))
            {
                tentativas++;
                var sp = new Vector2(rng.Faixa(-L, L), rng.Faixa(-L, L));
                if (ChaoAberto(g, sp, Relevo.PraiaY, 12.5f, 0.66f) >= 0f) sementes.Add(sp);
            }
            var lista = new List<Tufo>((int)(30000 * area));
            tentativas = 0;
            while (lista.Count < (int)(30000 * area) && tentativas < (int)(150000 * area))
            {
                tentativas++;
                Vector2 p;
                if (sementes.Count > 0 && rng.Float() < 0.88f)
                {
                    float a = rng.Faixa(0f, Mathf.PI * 2f);
                    // raiz quadrada = densidade parelha dentro da moita (sem ela vira alvo de tiro ao prato)
                    float rr = Mathf.Sqrt(rng.Float()) * rng.Faixa(1.2f, 3.0f);
                    p = sementes[rng.Int(sementes.Count)] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                }
                else p = new Vector2(rng.Faixa(-L, L), rng.Faixa(-L, L));
                float h = ChaoAberto(g, p, Relevo.PraiaY, 12.5f, 0.66f);
                if (h < 0f) continue;
                if (rng.Float() >= Densidade(g.BiomaEm(p.x, p.y))) continue;
                // contrato duro: grama so' em chao onde se POUSA (a pergunta exata, nao a da grade)
                if (!r.PodePousar(p.x, p.y)) continue;
                float sc = rng.Faixa(0.6f, 1.12f);
                float v = rng.Faixa(-0.14f, 0.14f);
                lista.Add(new Tufo
                {
                    Pos = new Vector3(p.x, h - 0.06f, p.y),
                    Giro = new Vector3(0f, rng.Faixa(0f, 360f), 0f),
                    Escala = new Vector3(sc, rng.Faixa(0.75f, 1.3f) * sc, sc),
                    Tinta = new Color(1f + v * 0.9f, 1f + v, 1f + v * 0.5f, 1f),
                });
            }
            return lista;
        }

        /// <summary>Petalas na paleta GDD §10 (sRGB): dourado arcano, Vento, rosa, branco.</summary>
        public static readonly Color[] Petalas =
        {
            Relevo.Hex(0xf0c75e), Relevo.Hex(0x8fe8c9), Relevo.Hex(0xff8ab3), Relevo.Hex(0xf5f0ff),
        };

        /// <summary>Flores so' em volta dos POIs de vida (lago, mata, ruinas) — flor espalhada por igual vira confete.</summary>
        public static List<Tufo> Flores(GradeDoChao g, int seed = 71)
        {
            Relevo r = g.Relevo;
            var rng = new Sorteio(seed);
            float area = Area(r), L = r.RaioTerra;
            var lista = new List<Tufo>();
            int tentativas = 0;
            while (lista.Count < (int)(480 * area) && tentativas < (int)(9000 * area))
            {
                tentativas++;
                var p = new Vector2(rng.Faixa(-L, L), rng.Faixa(-L, L));
                bool pertoDePoi = Vector2.Distance(p, r.Lago) < r.LagoR + 9f
                                  || Vector2.Distance(p, r.Floresta) < r.FlorestaR
                                  || Vector2.Distance(p, r.Ruinas) < r.RuinasR + 6f;
                if (!pertoDePoi || Vector2.Distance(p, r.Lago) < r.LagoR + 1f
                    || Vector2.Distance(p, r.Alagado) < r.AlagadoR || Vector2.Distance(p, r.Ruinas) < r.RuinasR * 0.8f)
                    continue;
                float h = g.AlturaNaMalha(p.x, p.y);
                if (h > 11f || g.NormalY(p.x, p.y) < 0.7f || !r.PodePousar(p.x, p.y)) continue;
                float s = rng.Faixa(0.8f, 1.2f);
                lista.Add(new Tufo
                {
                    Pos = new Vector3(p.x, h - 0.03f, p.y),
                    Giro = new Vector3(0f, rng.Faixa(0f, 360f), 0f),
                    Escala = new Vector3(s, s, s),
                    Tinta = Petalas[rng.Int(Petalas.Length)],
                });
            }
            return lista;
        }

        /// <summary>Junco: so' no alagado, na cota da lama (0,25-0,85 m), tombado ate' 12 graus — junco em pe' perfeito vira cerca.</summary>
        public static List<Tufo> Juncos(GradeDoChao g, int seed = 41)
        {
            Relevo r = g.Relevo;
            var rng = new Sorteio(seed);
            float area = Area(r), raio = r.AlagadoR - 1f;
            var lista = new List<Tufo>();
            int tentativas = 0;
            while (lista.Count < (int)(150 * area) && tentativas < (int)(3600 * area))
            {
                tentativas++;
                Vector2 p = r.Alagado + new Vector2(rng.Faixa(-1f, 1f), rng.Faixa(-1f, 1f)) * raio;
                if (Vector2.Distance(p, r.Alagado) > raio) continue;
                float h = g.AlturaNaMalha(p.x, p.y);
                if (h < 0.25f || h > 0.85f) continue;
                float s = rng.Faixa(0.6f, 1.45f);
                const float tombo = 12f;
                var giro = new Vector3(rng.Faixa(-tombo, tombo), rng.Faixa(0f, 360f), rng.Faixa(-tombo, tombo));
                float v = rng.Faixa(-0.22f, 0.16f);
                lista.Add(new Tufo
                {
                    Pos = new Vector3(p.x, h - 0.05f, p.y),
                    Giro = giro,
                    Escala = new Vector3(s, s * rng.Faixa(0.75f, 1.35f), s),
                    Tinta = new Color(1f + v * 0.6f, 1f + v, 1f + v * 0.8f, 1f),
                });
            }
            return lista;
        }

        /// <summary>Seixo: o degrau de escala entre a lamina (0,5 m) e o rochedo (2 m) — sem ele o chao parece maquete.
        /// O CUME do pico ganha leva propria (onda 5B): acima de 12,5 m nao nasce grama e acima de 13 nao nascia seixo, e e'
        /// la' que o treino acontece — a tampa lisa de 9 mil m2 lia como gesso (fotos 02 e 18).</summary>
        public static List<Tufo> Seixos(GradeDoChao g, int seed = 81)
        {
            Relevo r = g.Relevo;
            var rng = new Sorteio(seed);
            float area = Area(r), L = r.RaioTerra;
            var lista = new List<Tufo>();
            int tentativas = 0;
            while (lista.Count < (int)(380 * area) && tentativas < (int)(11000 * area))
            {
                tentativas++;
                var p = new Vector2(rng.Faixa(-L, L), rng.Faixa(-L, L));
                if (p.magnitude > L || Vector2.Distance(p, r.Lago) < r.LagoR - 1f) continue;
                float h = g.AlturaNaMalha(p.x, p.y);
                if (h < 0.15f || h > 13f) continue;
                lista.Add(Seixo(rng, p, h, 0.09f, 0.26f));
            }
            // o cume: no disco do pico, so' na TAMPA (o corte do Bioma.Pico), maiores para ler da camera de 3a pessoa
            int alvo = lista.Count + (int)(250 * area);   // KNOB: ~1 seixo a cada 3 m, por foto
            tentativas = 0;
            while (lista.Count < alvo && tentativas < (int)(4000 * area))
            {
                tentativas++;
                Vector2 p = r.Pico + new Vector2(rng.Faixa(-1f, 1f), rng.Faixa(-1f, 1f)) * r.PicoR;
                float h = g.AlturaNaMalha(p.x, p.y);
                if (h < Relevo.PicoH + 4f || g.NormalY(p.x, p.y) < 0.8f) continue;
                Tufo s = Seixo(rng, p, h, 0.1f, 0.34f);
                // basalto, da familia da rocha da Meshy ao lado: o cinza claro sumia na tampa clara e lia como lasca de papel (foto 28)
                s.Tinta = new Color(s.Tinta.r * 0.5f, s.Tinta.g * 0.48f, s.Tinta.b * 0.46f, 1f);
                lista.Add(s);
            }
            return lista;
        }

        static Tufo Seixo(Sorteio rng, Vector2 p, float h, float min, float max)
        {
            float sc = rng.Faixa(min, max);
            var giro = new Vector3(rng.Faixa(-23f, 23f), rng.Faixa(0f, 360f), rng.Faixa(-23f, 23f));
            float v = rng.Faixa(-0.18f, 0.18f);
            return new Tufo
            {
                Pos = new Vector3(p.x, h + sc * 0.15f, p.y),
                Giro = giro,
                Escala = new Vector3(sc * rng.Faixa(1f, 1.8f), sc * 0.6f, sc),
                Tinta = new Color(1f + v, 1f + v * 0.9f, 1f + v * 0.7f, 1f),
            };
        }
    }

    /// <summary>
    /// A casca: bina os tufos em celulas (~30 m, grade que escala com o mapa) e desenha cada celula VISIVEL
    /// com Graphics.RenderMeshInstanced — uma chamada por lote de ate' 1023, sem GameObject por tufo.
    /// CUSTO REAL: o Unity ainda parte o lote no teto de instancias do backend (250 no Vulkan mobile), entao
    /// ~10 mil tufos no frustum viram ~40 draw calls instanciados baratos de CPU. KNOB: a contagem do
    /// PlantioDaGrama.Grama ou o CorteGrama. Culling em duas etapas: a celula alem do corte HORIZONTAL nem
    /// vai pra GPU (4 Hz, uma distancia por celula); a que vai, o frustum corta pelos bounds dela. Olho
    /// acima do colapso (queda, castelo) = nenhum tufo: todos ja' estariam achatados pelo shader.
    /// </summary>
    public sealed class Grama : MonoBehaviour
    {
        const int MaxLote = 1023;   // teto da API por chamada: lote explicito, sem depender de quem divide

        sealed class Lote
        {
            public Matrix4x4[] M;
            public int N;
            public MaterialPropertyBlock Mpb;
        }

        sealed class Celula
        {
            public Vector3 Centro;
            public Bounds Caixa;
            public readonly List<Lote> Lotes = new List<Lote>();
        }

        sealed class Camada
        {
            public string Nome;
            public Mesh Malha;
            public Material Mat;
            public float Corte;
            public int Total;
            public readonly List<Celula> Cels = new List<Celula>();
            public readonly List<Celula> Visiveis = new List<Celula>();
        }

        static readonly int IdTinta = Shader.PropertyToID("_Tinta");

        readonly List<Camada> camadas = new List<Camada>();
        float tetoDoColapso;   // o maior FimDoFade entre as camadas
        float topoDoChao;      // o tufo mais alto da ilha
        float relogio;

        /// <summary>Quantas instancias a camada tem ("Grama", "Flores", "Juncos", "Seixos"); 0 se nao montou.</summary>
        public int Contar(string nome)
        {
            for (int i = 0; i < camadas.Count; i++) if (camadas[i].Nome == nome) return camadas[i].Total;
            return 0;
        }

        public void Montar(Relevo relevo)
        {
            camadas.Clear();
            // Fiacao defensiva: sem instancing (headless -nographics) ou sem o shader, a ilha fica sem grama, calada.
            if (relevo == null || !SystemInfo.supportsInstancing) return;
            Shader s = Resources.Load<Shader>("ArkanaGrama");
            if (s == null) s = Shader.Find("Arkana/Grama");
            if (s == null || !s.isSupported) return;
            // O material-ASSET com "Enable GPU Instancing" existe para o BUILD: com Instancing Variants em
            // "Strip Unused", variante instanciada de shader que so' material de runtime usa sai do APK, e a
            // grama desenharia todos os tufos no mesmo lugar. Referencia quebrada (meta regenerado) = ignora.
            Material molde = Resources.Load<Material>("ArkanaGramaInstancing");
            if (molde != null && molde.shader != s) molde = null;

            var grade = new GradeDoChao(relevo);
            int lado = PlantioDaGrama.CelulasPorLado(relevo);
            float passo = relevo.Lado / lado;
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            topoDoChao = -1e9f;

            Adicionar("Grama", MalhaTufo(linear), Mat(s, molde, "Grama", 0.09f, 0.7f, 0.8f, PlantioDaGrama.CorteGrama, passo),
                PlantioDaGrama.Grama(grade), PlantioDaGrama.CorteGrama, lado, passo, relevo.Lado, false, linear);
            // flor e' baixa: o vento tem que pegar a corola inteira, nao so' a metade de cima
            Adicionar("Flores", MalhaFlor(linear), Mat(s, molde, "Flores", 0.05f, 0.34f, 0.8f, PlantioDaGrama.CorteFlor, passo),
                PlantioDaGrama.Flores(grade), PlantioDaGrama.CorteFlor, lado, passo, relevo.Lado, true, linear);
            Adicionar("Juncos", MalhaJunco(linear), Mat(s, molde, "Juncos", 0.07f, 1.5f, 0.6f, PlantioDaGrama.CorteJunco, passo),
                PlantioDaGrama.Juncos(grade), PlantioDaGrama.CorteJunco, lado, passo, relevo.Lado, false, linear);
            // seixo nao balanca e guarda mais da normal: e' pedra, nao folha
            Adicionar("Seixos", MalhaSeixo(linear), Mat(s, molde, "Seixos", 0f, 1f, 0.3f, PlantioDaGrama.CorteSeixo, passo),
                PlantioDaGrama.Seixos(grade), PlantioDaGrama.CorteSeixo, lado, passo, relevo.Lado, false, linear);

            tetoDoColapso = 0f;
            for (int i = 0; i < camadas.Count; i++)
                tetoDoColapso = Mathf.Max(tetoDoColapso, PlantioDaGrama.FimDoFade(camadas[i].Corte, passo));
        }

        static Material Mat(Shader s, Material molde, string nome, float vento, float lamina, float achatar, float corte, float passo)
        {
            var m = (molde != null ? new Material(molde) : new Material(s));
            m.name = "Arkana" + nome;
            m.enableInstancing = true;
            float fim = PlantioDaGrama.FimDoFade(corte, passo);
            m.SetFloat("_Vento", vento);
            m.SetFloat("_AlturaLamina", lamina);
            m.SetFloat("_Achatar", achatar);
            m.SetFloat("_FadeFim", fim);
            m.SetFloat("_FadeInicio", Mathf.Max(0f, fim - PlantioDaGrama.RampaDoFade));
            return m;
        }

        void Adicionar(string nome, Mesh malha, Material mat, List<Tufo> tufos, float corte, int lado, float passo,
                       float ladoIlha, bool tintaEhCor, bool linear)
        {
            var cam = new Camada { Nome = nome, Malha = malha, Mat = mat, Corte = corte, Total = tufos.Count };
            var porCelula = new Dictionary<int, List<int>>();
            float meio = ladoIlha * 0.5f;
            for (int i = 0; i < tufos.Count; i++)
            {
                Vector3 p = tufos[i].Pos;
                int cx = Mathf.Clamp((int)((p.x + meio) / passo), 0, lado - 1);
                int cz = Mathf.Clamp((int)((p.z + meio) / passo), 0, lado - 1);
                int k = cz * lado + cx;
                if (!porCelula.TryGetValue(k, out List<int> l)) porCelula[k] = l = new List<int>();
                l.Add(i);
                topoDoChao = Mathf.Max(topoDoChao, p.y);
            }
            foreach (KeyValuePair<int, List<int>> kv in porCelula)
            {
                var cel = new Celula
                {
                    Centro = new Vector3((kv.Key % lado + 0.5f) * passo - meio, 0f, (kv.Key / lado + 0.5f) * passo - meio),
                };
                bool primeiro = true;
                List<int> idx = kv.Value;
                for (int ini = 0; ini < idx.Count; ini += MaxLote)
                {
                    int n = Mathf.Min(MaxLote, idx.Count - ini);
                    var lote = new Lote { M = new Matrix4x4[n], N = n, Mpb = new MaterialPropertyBlock() };
                    var tintas = new Vector4[n];
                    for (int j = 0; j < n; j++)
                    {
                        Tufo t = tufos[idx[ini + j]];
                        lote.M[j] = Matrix4x4.TRS(t.Pos, Quaternion.Euler(t.Giro), t.Escala);
                        // multiplicador fica cru (o Godot tambem); cor de petala e' sRGB e vira linear
                        Color c = tintaEhCor && linear ? t.Tinta.linear : t.Tinta;
                        tintas[j] = new Vector4(c.r, c.g, c.b, 1f);
                        if (primeiro) { cel.Caixa = new Bounds(t.Pos, Vector3.zero); primeiro = false; }
                        else cel.Caixa.Encapsulate(t.Pos);
                    }
                    lote.Mpb.SetVectorArray(IdTinta, tintas);
                    cel.Lotes.Add(lote);
                }
                cel.Caixa.Expand(8f);   // +-4 m: o junco mais alto passa de 3,5 m acima do pe'; o vento balanca
                cam.Cels.Add(cel);
            }
            camadas.Add(cam);
        }

        void Update()
        {
            if (camadas.Count == 0) return;
            relogio -= Time.deltaTime;
            if (relogio <= 0f)
            {
                relogio = 0.25f;
                Camera c = Camera.main;
                Reclassificar(c != null ? c.transform.position : Vector3.zero, c != null);
            }
            for (int i = 0; i < camadas.Count; i++)
            {
                Camada cam = camadas[i];
                for (int j = 0; j < cam.Visiveis.Count; j++)
                {
                    Celula cel = cam.Visiveis[j];
                    for (int k = 0; k < cel.Lotes.Count; k++)
                    {
                        Lote l = cel.Lotes[k];
                        var rp = new RenderParams(cam.Mat)
                        {
                            worldBounds = cel.Caixa,
                            matProps = l.Mpb,
                            shadowCastingMode = ShadowCastingMode.Off,   // grama nao paga sombra
                            receiveShadows = true,
                            lightProbeUsage = LightProbeUsage.Off,
                            layer = gameObject.layer,
                        };
                        Graphics.RenderMeshInstanced(rp, cam.Malha, 0, l.M, l.N);
                    }
                }
            }
        }

        void Reclassificar(Vector3 olho, bool temOlho)
        {
            // do alto (castelo, queda) todo tufo ja' estaria achatado pelo colapso 3D: nem manda pra GPU
            bool alto = !temOlho || olho.y - topoDoChao > tetoDoColapso;
            for (int i = 0; i < camadas.Count; i++)
            {
                Camada cam = camadas[i];
                cam.Visiveis.Clear();
                if (alto) continue;
                for (int j = 0; j < cam.Cels.Count; j++)
                    if (PlantioDaGrama.Visivel(olho, cam.Cels[j].Centro, cam.Corte)) cam.Visiveis.Add(cam.Cels[j]);
            }
        }

        // ------------------------------------------------------------ malhas (Island.gd)

        /// <summary>Lamina de 4 vertices com gradiente pe' -> ponta (a oclusao do pe' sai de graca).</summary>
        static void Lamina(MalhaProc.Construtor b, Vector3 b0, Vector3 b1, Vector3 t1, Vector3 t0, Color pe, Color ponta)
        {
            int i = b.V.Count;
            b.V.Add(b0); b.V.Add(b1); b.V.Add(t1); b.V.Add(t0);
            b.C.Add(pe); b.C.Add(pe); b.C.Add(ponta); b.C.Add(ponta);
            b.T.Add(i); b.T.Add(i + 1); b.T.Add(i + 2);
            b.T.Add(i); b.T.Add(i + 2); b.T.Add(i + 3);
        }

        /// <summary>Tufo de 3 laminas finas e tortas; a ponta encosta no tom do chao (ponta clara vira objeto, nao textura).
        /// A ponta segue a MEDIA da campina (Relevo.CorGrama*, onda 7B): com o chao mais fundo, a ponta de antes (0,42/0,71/0,37)
        /// virava pingo de limao por cima dele.</summary>
        static Mesh MalhaTufo(bool linear)
        {
            var b = new MalhaProc.Construtor();
            var rng = new Sorteio(91);
            Color pe = new Color(0.10f, 0.24f, 0.12f), ponta = new Color(0.39f, 0.62f, 0.36f);
            for (int k = 0; k < 3; k++)
            {
                float a = Mathf.PI * 2f * k / 3f + 0.4f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var lado = new Vector3(-d.z, 0f, d.x);
                const float w = 0.055f;
                float alt = rng.Faixa(0.26f, 0.44f);
                Vector3 inclina = d * rng.Faixa(0.05f, 0.16f);
                Vector3 raiz = d * 0.05f;
                Lamina(b, raiz - lado * w, raiz + lado * w,
                    raiz + inclina + lado * w * 0.2f + Vector3.up * alt, raiz + inclina - lado * w * 0.2f + Vector3.up * alt, pe, ponta);
            }
            return b.ParaMesh("Tufo", linear);
        }

        /// <summary>Dois quads cruzados: caule verde embaixo, cabeca BRANCA em cima — a tinta da instancia da' a petala.</summary>
        static Mesh MalhaFlor(bool linear)
        {
            var b = new MalhaProc.Construtor();
            Color caule = new Color(0.35f, 0.55f, 0.3f);
            for (int k = 0; k < 2; k++)
            {
                float a = k * Mathf.PI * 0.5f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Lamina(b, -d * 0.09f, d * 0.09f, d * 0.14f + Vector3.up * 0.34f, -d * 0.14f + Vector3.up * 0.34f, caule, Color.white);
            }
            return b.ParaMesh("Flor", linear);
        }

        /// <summary>Junco AFUNILADO com arco leve; o #8FE8C9 (Vento) puro num junco inteiro vira mentol fluorescente: e' acento, nao corpo.</summary>
        static Mesh MalhaJunco(bool linear)
        {
            var b = new MalhaProc.Construtor();
            Color vento = Relevo.Hex(0x8fe8c9);
            Color corpo = Color.Lerp(vento, Relevo.Hex(0x2f6b4f), 0.62f), ponta = Color.Lerp(vento, Relevo.Hex(0x3f7d63), 0.3f);
            for (int k = 0; k < 3; k++)
            {
                float a = Mathf.PI * 2f * k / 3f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 inclina = new Vector3(Mathf.Cos(a + 1.1f), 0f, Mathf.Sin(a + 1.1f)) * 0.22f;
                Vector3 topo = inclina + Vector3.up * (1.35f + 0.22f * k);
                Vector3 wb = d * 0.22f, wt = d * 0.045f;
                Lamina(b, -wb, wb, topo + wt, topo - wt, corpo, ponta);
            }
            return b.ParaMesh("Junco", linear);
        }

        static Mesh MalhaSeixo(bool linear)
        {
            var b = new MalhaProc.Construtor();
            b.Blob(Vector3.zero, new Vector3(1f, 0.7f, 1f), Relevo.CorSeixo, new Sorteio(17), 0.34f);
            return b.ParaMesh("Seixo", linear);
        }
    }
}
