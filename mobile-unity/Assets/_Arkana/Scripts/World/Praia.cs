using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arkana.World
{
    /// <summary>
    /// A PRAIA (onda 13C): a faixa de areia vazia da foto 33 (areia lisa, um tronco, uma pedrinha) ganha o que o mar deixa —
    /// barco naufragado com pedras e troncos em volta, troncos de deriva, rochas com estrela-do-mar (algumas com o pe' na agua)
    /// e capim de duna em manchas na parte alta da areia. Modelos da Meshy (45-48-*.glb, otimizar.py: em pe', base em y = 0).
    /// PLANTIO puro por seed (Plantio): so' em areia (Relevo.Solo.g), longe do lago e do brejo, fora dos nascimentos (quem colide),
    /// fora do que a Vegetacao ja' plantou (`ocupados`). DESENHO como a arvore da Meshy: RenderMeshInstanced por BLOCO de 60 m,
    /// um lote por peca x LOD, LOD1 alem de 40 m, capim some a 80 m, tronco a 150 m. Nenhum GameObject por touceira;
    /// o barco e' GameObject (1-2 na ilha) e troncos e rochas grandes ganham uma caixa de colisao cada (o tiro para neles).
    /// Sem o .glb a peca nao nasce; sem instancing (-nographics) nasce so' o colisor. Montada pela Vegetacao (a Ilha nao muda).
    /// </summary>
    public sealed class Praia : MonoBehaviour
    {
        public enum Peca { Barco, Tronco, Rocha, Capim }

        /// <summary>Os .glb em Resources, na ordem de Peca. O LOD1 e' o mesmo nome + "-lod1" (rocha e capim).</summary>
        public static readonly string[] Glb = { "45-barco-naufragado", "46-tronco-deriva", "47-rocha-costa", "48-capim-duna" };
        /// <summary>O tamanho de cada .glb (m: X comprido, Y altura, Z largo), medido nos bounds. Serve ao assentar e a' caixa de
        /// colisao, como o RaioM do kit. O tronco tem um galho erguido (a altura de 1,9 m e' dele; o tronco deitado tem ~0,6).</summary>
        public static readonly Vector3[] Tamanho =
        {
            new Vector3(5f, 2.65f, 2.37f), new Vector3(3.2f, 1.92f, 1.87f), new Vector3(2.2f, 1.48f, 1.71f), new Vector3(1.1f, 0.61f, 1.2f),
        };

        /// <summary>Uma peca da praia, PURA: a matriz leva o molde (centro da base na origem, +Y para cima) ao mundo.</summary>
        public struct PecaPlantada
        {
            public Peca Tipo;
            public Matrix4x4 M;
            public float Escala;
            public bool Colide;
            /// <summary>So' o barco: para onde fica o mar (XZ, unitario) — a foto olha o barco com a agua atras.</summary>
            public Vector2 Mar;
        }

        /// <summary>Quantas pecas a ilha de REFERENCIA (300 m) pede; planta x AREA. Na de 600 m: 20 troncos, 32 rochas, 32 manchas de
        /// capim (~5 touceiras cada). KNOB: por foto.</summary>
        public const int TroncosRef = 5, RochasRef = 8, ManchasRef = 8;
        /// <summary>A cota do centro do barco (m): na areia molhada, a agua a ~2-5 m. KNOB.</summary>
        public const float CotaDoBarco = 0.45f;
        /// <summary>Ate' onde a rocha pode ter o pe' no mar (cota do chao sob ela, m). Mais fundo, some na agua.</summary>
        public const float AguaRasa = -0.5f;
        /// <summary>Quanto das rochas soltas fica com o pe' no mar raso. KNOB.</summary>
        const float FracaoNoMar = 0.35f;
        /// <summary>O tronco de deriva fica na linha da mare' (cota do chao, m): acima da areia molhada, abaixo da campina.</summary>
        public const float TroncoMin = 0.2f, TroncoMax = 1.3f;
        /// <summary>O capim: da parte alta da praia a' transicao para a campina (cota, m) e so' onde o chao ainda e' areia.</summary>
        public const float CapimMin = 0.8f, CapimMax = 4.5f, AreiaDoCapim = 0.4f;
        /// <summary>Quanto o disco de areia da base do capim desce abaixo do chao (m, x escala): o .glb traz a base como disco. KNOB 5-8 cm.</summary>
        const float AfundaCapim = 0.07f;
        /// <summary>A partir de que escala tronco e rocha colidem (o seixo grande nao trava o jogador). KNOB.</summary>
        const float TroncoColide = 0.85f, RochaColide = 1f;
        /// <summary>Folga livre em volta de cada nascimento para quem colide (m, alem do raio da peca): o PlantioDoKit.FolgaDoNascimento.</summary>
        const float FolgaDoNascimento = 6f;

        /// <summary>Distancia 3D em que rocha e capim passam para o LOD1 (a da arvore) e cortes (m). KNOB: por foto e FPS.</summary>
        public const float DistanciaLod1 = 40f, CorteCapim = 80f, CorteTronco = 150f;
        const float PassoBloco = 60f;
        const float Periodo = 0.25f;
        /// <summary>A altura do CASCO do barco no .glb (m): a caixa de colisao para ai' — o mastro partido (2,65 m) nao vira parede.</summary>
        const float AlturaDoCasco = 1.3f;

        /// <summary>A TINTA de cada peca (multiplica a textura, sRGB). O barco e a rocha chegam quase pretos (madeira e basalto
        /// medianos ~0,15); o tronco de deriva chega branco e sob o sol quente viraria osso. KNOB: por foto.</summary>
        static readonly Color[] Tinta =
        {
            new Color(1.18f, 1.1f, 1f), new Color(0.9f, 0.88f, 0.84f), new Color(1.22f, 1.16f, 1.08f), new Color(1f, 1f, 0.94f),
        };

        // ---------------------------------------------------------------- plantio (puro)

        /// <summary>
        /// A praia da ilha, PURA por seed: 1-2 barcos na areia molhada (o 1o na costa das dunas, o outro longe dele) com 3 rochas e 2
        /// troncos compondo cada um, depois rochas, troncos e as manchas de capim soltos no anel da costa ate' a contagem x AREA.
        /// `ocupados` (x, z, raio em w: pedregulhos e arvores da Vegetacao) e' respeitado e recebe o que for plantado.
        /// </summary>
        public static List<PecaPlantada> Plantio(Relevo r, List<Vector4> ocupados = null)
        {
            var lista = new List<PecaPlantada>();
            if (r == null) return lista;
            if (ocupados == null) ocupados = new List<Vector4>();
            float area = r.Escala * r.Escala;
            Barcos(r, new Sorteio(131), ocupados, lista);

            // rochas soltas: um terco com o pe' no mar raso (a faixa de AguaRasa a 0 tem ~3 m: cota propria, senao quase nenhuma
            // cai la'), o resto na areia
            var rng = new Sorteio(132);
            int alvo = Mathf.RoundToInt(RochasRef * area), feitas = Contar(lista, Peca.Rocha), tentativas = 0;
            int alvoMar = feitas + Mathf.RoundToInt((alvo - feitas) * FracaoNoMar);
            while (feitas < alvo && tentativas++ < alvo * 900)
            {
                Vector2 p = NaCosta(rng, r);
                bool mar = feitas < alvoMar;
                float e = mar ? rng.Faixa(0.9f, 1.6f) : rng.Faixa(0.6f, 1.5f);
                if (!LugarDeRocha(r, p, mar)) continue;
                float raio = 0.55f * Tamanho[2].x * e;
                if (Invade(ocupados, p, raio + 2.5f) || (e >= RochaColide && PertoDeNascimento(r, p, FolgaDoNascimento + raio))) continue;
                lista.Add(Rocha(r, rng, p, e));
                ocupados.Add(new Vector4(p.x, p.y, 0f, raio));
                feitas++;
            }

            // troncos de deriva soltos, espalhados (4 m livres alem das pegadas)
            rng = new Sorteio(133);
            alvo = Mathf.RoundToInt(TroncosRef * area);
            feitas = Contar(lista, Peca.Tronco);
            tentativas = 0;
            while (feitas < alvo && tentativas++ < alvo * 600)
            {
                Vector2 p = NaCosta(rng, r);
                float e = rng.Faixa(0.75f, 1.2f);
                if (!LugarDeTronco(r, p)) continue;
                float raio = 0.5f * Tamanho[1].x * e;
                if (Invade(ocupados, p, raio + 4f) || (e >= TroncoColide && PertoDeNascimento(r, p, FolgaDoNascimento + raio))) continue;
                lista.Add(Tronco(r, rng, p, e));
                ocupados.Add(new Vector4(p.x, p.y, 0f, raio));
                feitas++;
            }

            // capim em MANCHAS: uma semente na areia alta e 3-8 touceiras em volta (sorteio parelho le' como plantacao)
            rng = new Sorteio(134);
            alvo = Mathf.RoundToInt(ManchasRef * area);
            int manchas = 0;
            tentativas = 0;
            while (manchas < alvo && tentativas++ < alvo * 600)
            {
                Vector2 s = NaCosta(rng, r);
                // o miolo das dunas fica ABERTO (o areal se le' de cima por ser liso); a franja e a praia ganham capim
                if (!LugarDeCapim(r, s) || Vector2.Distance(s, r.Dunas) < r.DunasR * 0.55f || Invade(ocupados, s, 1f)) continue;
                manchas++;
                int n = 3 + rng.Int(6);
                feitas = lista.Count;
                for (int k = 0; k < n * 3 && lista.Count - feitas < n; k++)
                {
                    float a = rng.Faixa(0f, Mathf.PI * 2f), d = Mathf.Sqrt(rng.Float()) * rng.Faixa(1.5f, 4f);
                    Vector2 p = s + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                    float e = rng.Faixa(0.7f, 1.35f);
                    // a touceira reserva meia pegada: vizinha encosta (a mancha fecha), nunca uma dentro da outra
                    if (!LugarDeCapim(r, p) || Invade(ocupados, p, 0.25f * e)) continue;
                    lista.Add(Capim(r, rng, p, e));
                    ocupados.Add(new Vector4(p.x, p.y, 0f, 0.25f * e));
                }
            }
            return lista;
        }

        static int Contar(List<PecaPlantada> l, Peca t)
        {
            int n = 0;
            for (int i = 0; i < l.Count; i++) if (l[i].Tipo == t) n++;
            return n;
        }

        /// <summary>
        /// O(s) BARCO(s): a cada 2 graus, de fora para dentro, o primeiro chao na CotaDoBarco; nota = largura da praia ali (ate' 18 m,
        /// da agua a' PraiaY). O 1o prefere a costa das dunas (o areal e' a praia do mapa), o 2o fica a mais de 150 m dele. Cada um
        /// deita de lado na areia molhada, meio enterrado, com a proa virada para o mar, e ganha 3 rochas e 2 troncos em volta.
        /// </summary>
        static void Barcos(Relevo r, Sorteio rng, List<Vector4> ocupados, List<PecaPlantada> lista)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(r.Escala), 1, 2);
            float eixoDunas = Mathf.Atan2(r.Dunas.y, r.Dunas.x) * Mathf.Rad2Deg;
            var feitos = new List<Vector2>();
            for (int b = 0; b < n; b++)
            {
                float melhor = float.MinValue;
                Vector2 c = Vector2.zero;
                for (int k = 0; k < 180; k++)
                {
                    float ang = k * 2f;
                    var d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                    float m = r.RaioTerra * 1.2f;
                    while (m > r.RaioTerra * 0.5f && r.Altura(d.x * m, d.y * m) < CotaDoBarco) m -= 1f;
                    Vector2 p = d * m;
                    if (!LongeDaAguaDoce(r, p) || r.SuperficieDaAgua(p.x, p.y) != Relevo.Seco || r.Solo(p.x, p.y, r.Altura(p.x, p.y)).g < 0.75f) continue;
                    if (PertoDeNascimento(r, p, FolgaDoNascimento + 3f) || Invade(ocupados, p, 3.5f)) continue;
                    bool longe = true;
                    foreach (Vector2 f in feitos) longe &= Vector2.Distance(f, p) > 150f;
                    if (!longe) continue;
                    float largura = 0f;
                    while (largura < 18f && r.Altura(p.x - d.x * largura, p.y - d.y * largura) < Relevo.PraiaY) largura += 1f;
                    float nota = largura - (b == 0 ? Mathf.Abs(Mathf.DeltaAngle(ang, eixoDunas)) * 0.4f : 0f);
                    if (largura >= 6f && nota > melhor) { melhor = nota; c = p; }
                }
                if (melhor == float.MinValue) return;
                feitos.Add(c);

                // o mar: ladeira abaixo pela pegada (a costa nao e' radial nas curvas)
                var mar = new Vector2(Vegetacao.ChaoDesenhado(r, c.x - 2.5f, c.y) - Vegetacao.ChaoDesenhado(r, c.x + 2.5f, c.y),
                    Vegetacao.ChaoDesenhado(r, c.x, c.y - 2.5f) - Vegetacao.ChaoDesenhado(r, c.x, c.y + 2.5f));
                mar = mar.sqrMagnitude > 1e-6f ? mar.normalized : c.normalized;
                var ao = new Vector2(-mar.y, mar.x) * (rng.Float() < 0.5f ? 1f : -1f);   // ao longo da costa
                float alfa = rng.Faixa(20f, 40f) * Mathf.Deg2Rad;
                Vector2 proa = ao * Mathf.Cos(alfa) + mar * Mathf.Sin(alfa);   // KNOB: a proa meio virada para o mar
                // o tombo: deitado de lado 10-18 graus (KNOB), a proa um pouco acima ou abaixo; meio enterrado
                var tombo = new Vector3(rng.Faixa(10f, 18f) * (rng.Float() < 0.5f ? 1f : -1f), 0f, rng.Faixa(-5f, 5f));
                lista.Add(new PecaPlantada
                {
                    Tipo = Peca.Barco, Escala = 1f, Colide = true, Mar = mar,
                    M = Assentar(r, Tamanho[0], c, Mathf.Atan2(-proa.y, proa.x) * Mathf.Rad2Deg, tombo, 1f, 0.6f, rng.Faixa(0.2f, 0.35f)),
                });

                // a cena em volta (no referencial proa x mar): pedra na proa com o pe' no mar, pedra atras da popa, pedra pequena do
                // lado da terra; um tronco ao longo da costa e outro na areia seca atras. Jitter de 0,8 m; lugar ruim, a peca nao vai
                Vector2 Em(float lp, float lm) => c + proa * lp + mar * lm + new Vector2(rng.Faixa(-0.8f, 0.8f), rng.Faixa(-0.8f, 0.8f));
                Vector2 q = Em(3.8f, 1.6f);
                if (LugarDeRocha(r, q, true) || LugarDeRocha(r, q, false)) Compor(r, lista, ocupados, Rocha(r, rng, q, rng.Faixa(0.9f, 1.3f)), q);
                q = Em(-4f, -0.6f);
                if (LugarDeRocha(r, q, true) || LugarDeRocha(r, q, false)) Compor(r, lista, ocupados, Rocha(r, rng, q, rng.Faixa(1f, 1.4f)), q);
                q = Em(1.2f, -3.2f);
                if (LugarDeRocha(r, q, false)) Compor(r, lista, ocupados, Rocha(r, rng, q, rng.Faixa(0.6f, 0.85f)), q);
                q = Em(6.5f, -1.2f);
                if (LugarDeTronco(r, q)) Compor(r, lista, ocupados, Tronco(r, rng, q, rng.Faixa(0.85f, 1.1f)), q);
                q = Em(-1f, -4.5f);
                if (LugarDeTronco(r, q)) Compor(r, lista, ocupados, Tronco(r, rng, q, rng.Faixa(0.8f, 1f)), q);
                ocupados.Add(new Vector4(c.x, c.y, 0f, 3f));   // o barco reserva o chao depois da cena dele (ela encosta nele)
            }
        }

        /// <summary>A peca da cena do barco entra se nao pisa no que ja' esta' plantado (60% da pegada: pedra encosta em pedra) e,
        /// se colide, fica fora da folga dos nascimentos, como as soltas.</summary>
        static void Compor(Relevo r, List<PecaPlantada> lista, List<Vector4> ocupados, PecaPlantada p, Vector2 onde)
        {
            float raio = 0.5f * Tamanho[(int)p.Tipo].x * p.Escala;
            if (Invade(ocupados, onde, 0.6f * raio) || (p.Colide && PertoDeNascimento(r, onde, FolgaDoNascimento + raio))) return;
            lista.Add(p);
            ocupados.Add(new Vector4(onde.x, onde.y, 0f, raio));
        }

        static PecaPlantada Rocha(Relevo r, Sorteio rng, Vector2 p, float e)
        {
            // o sorteio sai SEMPRE na mesma ordem: giro, tombo (2), afundar
            float giro = rng.Faixa(0f, 360f);
            var tombo = new Vector3(rng.Faixa(-5f, 5f), 0f, rng.Faixa(-5f, 5f));
            float afunda = Tamanho[2].y * e * rng.Faixa(0.1f, 0.22f);   // KNOB
            return new PecaPlantada { Tipo = Peca.Rocha, Escala = e, Colide = e >= RochaColide, M = Assentar(r, Tamanho[2], p, giro, tombo, e, 0.7f, afunda) };
        }

        static PecaPlantada Tronco(Relevo r, Sorteio rng, Vector2 p, float e)
        {
            float giro = rng.Faixa(0f, 360f);
            var tombo = new Vector3(rng.Faixa(-8f, 8f), 0f, rng.Faixa(-4f, 4f));
            // 30% meio enterrados (metade do tronco deitado na areia), o resto so' assentado. KNOB
            float afunda = e * (rng.Float() < 0.3f ? rng.Faixa(0.25f, 0.4f) : rng.Faixa(0.06f, 0.16f));
            return new PecaPlantada { Tipo = Peca.Tronco, Escala = e, Colide = e >= TroncoColide, M = Assentar(r, Tamanho[1], p, giro, tombo, e, 0.8f, afunda) };
        }

        static PecaPlantada Capim(Relevo r, Sorteio rng, Vector2 p, float e)
        {
            float giro = rng.Faixa(0f, 360f);
            var tombo = new Vector3(rng.Faixa(-4f, 4f), 0f, rng.Faixa(-4f, 4f));
            return new PecaPlantada { Tipo = Peca.Capim, Escala = e, M = Assentar(r, Tamanho[3], p, giro, tombo, e, 0.85f, AfundaCapim * e) };
        }

        /// <summary>Um ponto no anel da costa: a agua fica entre ~0,54 (o fundo da enseada) e ~1,02 do raio de terra.</summary>
        static Vector2 NaCosta(Sorteio rng, Relevo r)
        {
            float a = rng.Faixa(0f, Mathf.PI * 2f), d = r.RaioTerra * rng.Faixa(0.5f, 1.15f);
            return new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
        }

        /// <summary>A praia e' do MAR: longe da lamina do lago e da lama do brejo (a lama zera a ~1,3 raio do alagado).</summary>
        public static bool LongeDaAguaDoce(Relevo r, Vector2 p) =>
            Vector2.Distance(p, r.Lago) > r.LagoDiscoR + 12f && Vector2.Distance(p, r.Alagado) > r.AlagadoDiscoR + 16f;

        /// <summary>Rocha: na areia (seca, 0,1-1,6 m, chao de areia) ou com o pe' no mar raso (AguaRasa-0,05 m, alem do lago e do brejo).</summary>
        static bool LugarDeRocha(Relevo r, Vector2 p, bool mar)
        {
            float h = r.Altura(p.x, p.y);
            if (mar) return h >= AguaRasa && h < 0.05f && p.magnitude > r.RaioTerra * 0.5f && LongeDaAguaDoce(r, p);
            return h >= 0.1f && h <= 1.6f && LongeDaAguaDoce(r, p) && r.SuperficieDaAgua(p.x, p.y) == Relevo.Seco && r.Solo(p.x, p.y, h).g >= 0.7f;
        }

        /// <summary>Tronco: na linha da mare' (TroncoMin-TroncoMax), seco, areia, chao quase plano (tronco na ladeira rola).</summary>
        static bool LugarDeTronco(Relevo r, Vector2 p)
        {
            float h = r.Altura(p.x, p.y);
            return h >= TroncoMin && h <= TroncoMax && LongeDaAguaDoce(r, p) && r.SuperficieDaAgua(p.x, p.y) == Relevo.Seco
                   && r.NormalY(p.x, p.y) >= 0.9f && r.Solo(p.x, p.y, h).g >= 0.7f;
        }

        /// <summary>Capim: a areia alta (CapimMin-CapimMax, chao ainda de areia) e a franja das dunas, fora de ladeira.</summary>
        static bool LugarDeCapim(Relevo r, Vector2 p)
        {
            float h = r.Altura(p.x, p.y);
            return h >= CapimMin && h <= CapimMax && LongeDaAguaDoce(r, p) && r.SuperficieDaAgua(p.x, p.y) == Relevo.Seco
                   && r.NormalY(p.x, p.y) >= 0.75f && r.Solo(p.x, p.y, h).g >= AreiaDoCapim;
        }

        /// <summary>
        /// O molde de `tam` (centro da base na origem) em `p`: giro em Y, `tombo` (graus em X e Z do molde), a base deitada `encosta`
        /// na ladeira da pegada e TODA ela (3x3 pontos) `afunda` abaixo do chao DESENHADO — a conta do Vegetacao.AssentarRocha, sem o
        /// blob. Sem Quaternion.Euler/Matrix4x4.TRS (chamadas internas do Unity): roda puro na sonda, fora do editor.
        /// </summary>
        public static Matrix4x4 Assentar(Relevo r, Vector3 tam, Vector2 p, float giro, Vector3 tombo, float e, float encosta, float afunda)
        {
            float R = 0.5f * e * Mathf.Max(tam.x, tam.z);
            float dx = Vegetacao.ChaoDesenhado(r, p.x - R, p.y) - Vegetacao.ChaoDesenhado(r, p.x + R, p.y);
            float dz = Vegetacao.ChaoDesenhado(r, p.x, p.y - R) - Vegetacao.ChaoDesenhado(r, p.x, p.y + R);
            Vector3 n = Vector3.Lerp(Vector3.up, new Vector3(dx, 2f * R, dz).normalized, encosta).normalized;
            Quaternion q = Inclinar(n) * Giro(Vector3.up, giro) * Giro(Vector3.forward, tombo.z) * Giro(Vector3.right, tombo.x);
            Matrix4x4 rs = Matrix4x4.Rotate(q) * Matrix4x4.Scale(new Vector3(e, e, e));
            float y0 = float.MaxValue;
            for (int i = 0; i < 3; i++)
                for (int k = 0; k < 3; k++)
                {
                    Vector3 o = rs.MultiplyPoint3x4(new Vector3((i - 1) * 0.5f * tam.x, 0f, (k - 1) * 0.5f * tam.z));
                    y0 = Mathf.Min(y0, Vegetacao.ChaoDesenhado(r, p.x + o.x, p.y + o.z) - o.y);
                }
            return Matrix4x4.Translate(new Vector3(p.x, y0 - afunda, p.y)) * rs;
        }

        /// <summary>O giro que leva +Y a `n` (unitario).</summary>
        static Quaternion Inclinar(Vector3 n)
        {
            Vector3 eixo = Vector3.Cross(Vector3.up, n);
            float s = eixo.magnitude;
            return s < 1e-5f ? Quaternion.identity : Giro(eixo / s, Mathf.Atan2(s, n.y) * Mathf.Rad2Deg);
        }

        // ponytail: copia do Ruinas.Giro (privado la'); juntar quando alguem mexer nas Ruinas
        static Quaternion Giro(Vector3 eixo, float graus)
        {
            float a = graus * Mathf.Deg2Rad * 0.5f, s = Mathf.Sin(a);
            return new Quaternion(eixo.x * s, eixo.y * s, eixo.z * s, Mathf.Cos(a));
        }

        static bool PertoDeNascimento(Relevo r, Vector2 p, float raio)
        {
            foreach (Vector3 n in r.Nascimentos)
                if ((n.x - p.x) * (n.x - p.x) + (n.z - p.y) * (n.z - p.y) < raio * raio) return true;
            return false;
        }

        static bool Invade(List<Vector4> ocupados, Vector2 p, float raio)
        {
            for (int i = 0; i < ocupados.Count; i++)
            {
                float dx = ocupados[i].x - p.x, dz = ocupados[i].y - p.y, s = ocupados[i].w + raio;
                if (dx * dx + dz * dz < s * s) return true;
            }
            return false;
        }

        /// <summary>A peca em `olho` vai no LOD1? Distancia 3D, como a arvore.</summary>
        public static bool Lod1(Vector3 olho, Vector3 peca) => (olho - peca).sqrMagnitude > DistanciaLod1 * DistanciaLod1;

        /// <summary>O capim em `peca` visto de `olho` desenha? Distancia 3D: a grama corta na horizontal porque o shader dela achata o
        /// tufo no alto; o capim da Meshy nao achata, entao e' a distancia que o tira da queda e do castelo (1,3K tris cada).</summary>
        public static bool CapimVisivel(Vector3 olho, Vector3 peca) => (olho - peca).sqrMagnitude < CorteCapim * CorteCapim;

        // ---------------------------------------------------------------- casca

        sealed class Lote
        {
            public Matrix4x4[] M;
            public int N;
        }

        /// <summary>Bloco de desenho: a caixa que o frustum corta e um lote por peca instanciada x LOD ([(tipo - 1) * 2 + lod]).</summary>
        sealed class Bloco
        {
            public Bounds Caixa;
            public readonly List<int> Pecas = new List<int>();
            public readonly Lote[] Lotes = new Lote[6];
        }

        readonly List<PecaPlantada> pecas = new List<PecaPlantada>();
        readonly List<Matrix4x4> vis = new List<Matrix4x4>();   // a matriz com o molde, por peca (barco: a do GameObject)
        readonly List<Bloco> blocos = new List<Bloco>();
        readonly Mesh[] malha = new Mesh[6];                     // [(tipo - 1) * 2 + lod]; null = sem o .glb ou sem instancing
        readonly int[] tris = new int[6];
        static readonly Material[] mats = new Material[7];      // [(tipo - 1) * 2 + lod], 6 = o barco
        Camera olho;
        float relogio = Periodo;

        /// <summary>Quantas pecas do tipo nasceram (0 = sem o .glb).</summary>
        public int Contar(Peca t) => Contar(pecas, t);
        public PecaPlantada Plantada(int i) => pecas[i];
        public int Total => pecas.Count;
        /// <summary>O que carregou (diag da foto): os .glb e o LOD1 de cada um.</summary>
        public string Moldes { get; private set; } = "";
        /// <summary>Do ultimo corte (4 Hz): lotes mandados por quadro e triangulos ANTES do frustum (sem o barco).</summary>
        public int LotesEnviados { get; private set; }
        public int TrisEnviados { get; private set; }

        /// <summary>A camera do LOD e do corte; null = Camera.main. A Vegetacao.Olho repassa a da foto.</summary>
        public Camera Olho
        {
            get => olho;
            set { olho = value; relogio = Periodo; }
        }

        public void Montar(Relevo relevo, List<Vector4> ocupados)
        {
            pecas.Clear(); vis.Clear(); blocos.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            if (relevo == null) return;
            var prefab = new GameObject[4];
            var molde = new Matrix4x4[4];
            var tex = new Texture[7];
            Mesh barco = null;
            Moldes = "";
            for (int t = 0; t < 4; t++)
            {
                prefab[t] = Resources.Load<GameObject>(Glb[t]);
                Mesh m = Vegetacao.MalhaDoGlb(Glb[t], out Texture tx);
                if (prefab[t] == null || m == null) { prefab[t] = null; continue; }
                Bounds b = m.bounds;
                molde[t] = Matrix4x4.Translate(-new Vector3(b.center.x, b.min.y, b.center.z));
                Moldes += (Moldes.Length > 0 ? " + " : "") + Glb[t];
                if (t == 0) { barco = m; tex[6] = tx; continue; }
                if (!SystemInfo.supportsInstancing || tx == null) continue;   // -nographics: so' o colisor
                int k = (t - 1) * 2;
                malha[k] = m; tex[k] = tx;
                // o LOD1 e' outro .glb (outra UV, outra textura -> material proprio); sem ele, o LOD0 longe (custa triangulo, nao some)
                malha[k + 1] = Vegetacao.MalhaDoGlb(Glb[t] + "-lod1", out Texture tx1);
                if (malha[k + 1] != null && tx1 != null) { tex[k + 1] = tx1; Moldes += "(lod1)"; }
                else { malha[k + 1] = m; tex[k + 1] = tx; }
            }
            for (int k = 0; k < 7; k++)
            {
                if (tex[k] == null) continue;
                int t = k == 6 ? 0 : k / 2 + 1;
                if (Vegetacao.MaterialDaMeshy(ref mats[k], "Praia" + (Peca)t + (k & 1), tex[k], Tinta[t]) == null)
                {
                    if (k < 6) malha[k] = null;   // sem o Lit: a peca nao desenha (o colisor fica)
                }
            }
            for (int k = 0; k < 6; k++) tris[k] = malha[k] != null ? (int)(malha[k].GetIndexCount(0) / 3) : 0;

            foreach (PecaPlantada p in Plantio(relevo, ocupados))
            {
                int t = (int)p.Tipo;
                if (prefab[t] == null) continue;   // sem o .glb a peca nao nasce (nem colisor invisivel)
                pecas.Add(p);
                vis.Add(p.M * molde[t]);
                if (p.Tipo == Peca.Barco) Barco(prefab[0], p, barco.bounds, mats[6]);
                else if (p.Colide) Colisor(p);
            }
            MontarBlocos();
            relogio = Periodo;
        }

        /// <summary>O barco e' GameObject (1-2 na ilha): o .glb no molde, material fosco com a tinta, sombra, e a caixa do CASCO.</summary>
        void Barco(GameObject prefab, PecaPlantada p, Bounds b, Material mat)
        {
            var raiz = new GameObject("Barco");
            raiz.transform.SetParent(transform, false);
            raiz.transform.SetPositionAndRotation(p.M.GetColumn(3), Quaternion.LookRotation(p.M.GetColumn(2), p.M.GetColumn(1)));
            raiz.transform.localScale = Vector3.one * p.Escala;
            GameObject go = Instantiate(prefab, raiz.transform, false);
            go.name = Glb[0];
            go.transform.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (mat != null) r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            var bc = raiz.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, AlturaDoCasco * 0.5f, 0f);
            bc.size = new Vector3(Tamanho[0].x * 0.92f, AlturaDoCasco, Tamanho[0].z * 0.8f);
        }

        /// <summary>A caixa de colisao de tronco e rocha grande, girada com a peca: a rocha pela caixa dela (80%), o tronco pelo
        /// corpo deitado (0,5 m de altura: o galho erguido nao vira parede).</summary>
        void Colisor(PecaPlantada p)
        {
            var go = new GameObject(p.Tipo.ToString());
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(p.M.GetColumn(3), Quaternion.LookRotation(p.M.GetColumn(2), p.M.GetColumn(1)));
            go.transform.localScale = Vector3.one * p.Escala;
            var bc = go.AddComponent<BoxCollider>();
            Vector3 tam = Tamanho[(int)p.Tipo];
            if (p.Tipo == Peca.Rocha) { bc.center = new Vector3(0f, tam.y * 0.4f, 0f); bc.size = tam * 0.8f; }
            else { bc.center = new Vector3(0f, 0.25f, 0f); bc.size = new Vector3(tam.x * 0.9f, 0.5f, tam.z * 0.7f); }
        }

        /// <summary>Bina as pecas instanciadas em blocos de ~60 m e reserva os lotes (os dois LOD). Tudo alocado AQUI: o corte de
        /// 4 Hz so' copia matriz. ponytail: ilha na origem (a Vegetacao ja' supoe): matriz e caixa em coordenada de mundo.</summary>
        void MontarBlocos()
        {
            float meio = PassoBloco * 1000f;
            var porBloco = new Dictionary<long, Bloco>();
            for (int i = 0; i < pecas.Count; i++)
            {
                int t = (int)pecas[i].Tipo;
                if (t == 0 || malha[(t - 1) * 2] == null) continue;
                Vector3 p = pecas[i].M.GetColumn(3);
                long k = (long)Mathf.FloorToInt((p.x + meio) / PassoBloco) * 100000L + Mathf.FloorToInt((p.z + meio) / PassoBloco);
                float e = pecas[i].Escala, lado = 0.75f * e * Mathf.Max(Tamanho[t].x, Tamanho[t].z) + 0.5f;
                var caixa = new Bounds(p + Vector3.up * (0.5f * Tamanho[t].y * e), new Vector3(2f * lado, Tamanho[t].y * e + 1f, 2f * lado));
                if (!porBloco.TryGetValue(k, out Bloco b))
                {
                    porBloco[k] = b = new Bloco { Caixa = caixa };
                    blocos.Add(b);
                }
                b.Pecas.Add(i);
                b.Caixa.Encapsulate(caixa);
            }
            var n = new int[3];
            foreach (Bloco b in blocos)
            {
                System.Array.Clear(n, 0, 3);
                foreach (int i in b.Pecas) n[(int)pecas[i].Tipo - 1]++;
                for (int t = 0; t < 3; t++)
                {
                    if (n[t] == 0) continue;
                    b.Lotes[t * 2] = new Lote { M = new Matrix4x4[n[t]] };
                    b.Lotes[t * 2 + 1] = new Lote { M = new Matrix4x4[n[t]] };
                }
            }
        }

        /// <summary>4 Hz: cada peca no lote do LOD dela; capim alem do CorteCapim e tronco alem do CorteTronco nao vao.</summary>
        void Reclassificar(Vector3 o)
        {
            int lotes = 0, soma = 0;
            foreach (Bloco b in blocos)
            {
                foreach (Lote l in b.Lotes) if (l != null) l.N = 0;
                foreach (int i in b.Pecas)
                {
                    PecaPlantada p = pecas[i];
                    Vector3 pos = p.M.GetColumn(3);
                    if (p.Tipo == Peca.Capim && !CapimVisivel(o, pos)) continue;
                    if (p.Tipo == Peca.Tronco && (o - pos).sqrMagnitude > CorteTronco * CorteTronco) continue;
                    Lote l = b.Lotes[((int)p.Tipo - 1) * 2 + (Lod1(o, pos) ? 1 : 0)];
                    l.M[l.N++] = vis[i];
                }
                for (int k = 0; k < 6; k++)
                {
                    if (b.Lotes[k] == null || b.Lotes[k].N == 0) continue;
                    lotes++;
                    soma += b.Lotes[k].N * tris[k];
                }
            }
            LotesEnviados = lotes;
            TrisEnviados = soma;
        }

        void Update()
        {
            if (blocos.Count == 0) return;
            relogio += Time.deltaTime;
            if (relogio >= Periodo)
            {
                relogio = 0f;
                Camera cam = olho != null ? olho : Camera.main;
                Reclassificar(cam != null ? cam.transform.position : new Vector3(0f, 1e4f, 0f));   // sem camera: tudo longe
            }
            foreach (Bloco b in blocos)
                for (int k = 0; k < 6; k++)
                {
                    Lote l = b.Lotes[k];
                    if (l == null || l.N == 0) continue;
                    var rp = new RenderParams(mats[k])
                    {
                        worldBounds = b.Caixa,
                        shadowCastingMode = k >= 4 ? ShadowCastingMode.Off : ShadowCastingMode.On,   // capim nao paga sombra
                        receiveShadows = true,
                        lightProbeUsage = LightProbeUsage.Off,
                        layer = gameObject.layer,
                    };
                    Graphics.RenderMeshInstanced(rp, malha[k], 0, l.M, l.N);
                }
        }
    }
}
