using NUnit.Framework;
using UnityEngine;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// A COSTA DO MAR (onda 13A): a textura que a Ilha assa UMA vez pela profundidade do fundo e o ArkanaAgua le' no mar (faixa
    /// rasa turquesa, espuma na linha d'agua). Pura: Relevo -> texels. Fica vermelho invertendo o degrade, deixando o mar aberto
    /// raso (o mar inteiro turquesa), desalinhando o texel do uv do shader ou deixando o load caro.
    /// </summary>
    public class WorldCostaTests
    {
        static Relevo _r;
        static Relevo R => _r ?? (_r = new Relevo(2f, 7));

        /// <summary>Praia aberta: longe do lago (~22 graus), do alagado (~139) e da boca do fiorde (~160-200), onde o fundo sobe de novo.</summary>
        static readonly float[] Rumos = { 0f, 60f, 90f, 120f, 240f, 270f, 300f, 330f };

        /// <summary>A linha d'agua de FORA no rumo (varre de fora para dentro: o lago e o fiorde nao contam).</summary>
        static float Beira(float ang)
        {
            float m = 420f;
            while (m > 0f && R.Altura(Mathf.Cos(ang) * m, Mathf.Sin(ang) * m) < Relevo.AguaY) m -= 0.25f;
            return m;
        }

        /// <summary>O que o SHADER le' em (x, z): bilinear com texel no centro, clamp na borda, decodificado em q = prof/CostaFundo
        /// (q = 1 - costa x ret.w): < 0 na terra, 0 na linha d'agua, 1 no mar fundo.</summary>
        static float QNoShader(Color32[] px, int n, Vector4 ret, float x, float z)
        {
            float u = Mathf.Clamp((x - ret.x) * ret.z * n - 0.5f, 0f, n - 1f), v = Mathf.Clamp((z - ret.y) * ret.z * n - 0.5f, 0f, n - 1f);
            int i = Mathf.Min((int)u, n - 2), j = Mathf.Min((int)v, n - 2);
            float fu = u - i, fv = v - j;
            float a = Mathf.Lerp(px[j * n + i].r, px[j * n + i + 1].r, fu), b = Mathf.Lerp(px[(j + 1) * n + i].r, px[(j + 1) * n + i + 1].r, fu);
            return 1f - Mathf.Lerp(a, b, fv) / 255f * ret.w;
        }

        [Test]
        public void Costa_TerraEUm_MarAbertoEZero_LinhaDaguaNoMeio()
        {
            Assert.AreEqual(1f, Ilha.Costa(R, 0f, 0f), 1e-6f, "o vale (terra seca) e' terra");
            Assert.AreEqual(1f, Ilha.Costa(R, R.Pico.x, R.Pico.y), 1e-6f, "o pico e' terra");
            for (int k = 0; k < 36; k++)
            {
                float a = k * 10f * Mathf.Deg2Rad;
                Assert.AreEqual(0f, Ilha.Costa(R, Mathf.Cos(a) * 340f, Mathf.Sin(a) * 340f), 1e-6f,
                    "mar aberto e' FUNDO no rumo " + k * 10 + " (CostaFundo abaixo do plato de 3,2 m, senao o mar inteiro sai turquesa)");
            }
            float linha = 1f / (1f + Ilha.CostaSeco);
            foreach (float g in Rumos)
            {
                float a = g * Mathf.Deg2Rad, m = Beira(a);
                Assert.AreEqual(linha, Ilha.Costa(R, Mathf.Cos(a) * m, Mathf.Sin(a) * m), 0.03f, "a linha d'agua vale 1/(1 + CostaSeco) no rumo " + g);
            }
        }

        [Test]
        public void Costa_DesceSemVoltarAoSairDaPraia_EAcabaFundoPerto()
        {
            foreach (float g in Rumos)
            {
                float a = g * Mathf.Deg2Rad, m = Beira(a);
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float antes = Ilha.Costa(R, d.x * m, d.y * m), fundoEm = -1f;
                for (float e = 0.5f; e <= 80f; e += 0.5f)
                {
                    float c = Ilha.Costa(R, d.x * (m + e), d.y * (m + e));
                    Assert.LessOrEqual(c, antes + 1e-5f, "o raso nao volta a subir ao se afastar da praia (rumo " + g + ", " + e + " m)");
                    if (fundoEm < 0f && c <= 0f) fundoEm = e;
                    antes = c;
                }
                Assert.Greater(Ilha.Costa(R, d.x * (m + 3f), d.y * (m + 3f)), 0.5f, "a 3 m da beira ainda e' raso (rumo " + g + ")");
                Assert.That(fundoEm, Is.InRange(10f, 60f), "a faixa rasa acaba a 10-60 m da linha d'agua (rumo " + g + ")");
            }
        }

        [Test]
        public void AssarCosta_TexelNoCentro_AnelDeForaFundo_FaixaFina_Barato()
        {
            int n = Ilha.CostaTexels;
            Color32[] px = null;
            Vector4 ret = default;
            long melhor = long.MaxValue;
            for (int k = 0; k < 3; k++)   // o melhor de 3: um soluco da maquina nao reprova
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                px = Ilha.AssarCosta(R, n, out ret);
                melhor = System.Math.Min(melhor, sw.ElapsedMilliseconds);
            }
            Assert.Less(melhor, 60, "assar a costa levou " + melhor + " ms (roda no load do celular)");
            Assert.AreEqual(n * n, px.Length);
            float lado = 1f / ret.z;
            Assert.AreEqual(ret.x, ret.y, 1e-6f, "quadrado centrado");
            Assert.LessOrEqual(ret.x, -0.5f * R.Lado, "o retangulo cobre a malha do terreno");
            Assert.GreaterOrEqual(ret.x + lado, 0.5f * R.Lado, "o retangulo cobre a malha do terreno");
            Assert.AreEqual(1f + Ilha.CostaSeco, ret.w, 1e-6f, "o shader decodifica pelo _CostaRet.w: o numero mora so' no C#");

            // o texel (i, j) e' a Costa no CENTRO dele (a convencao do bilinear): o uv do shader acha o mesmo lugar. (252, 0) fica
            // na linha d'agua, onde meio texel de erro ja' da' mais de 10 niveis.
            foreach (var p in new[] { new Vector2(0f, 0f), new Vector2(252f, 0f), new Vector2(-100f, 262f), new Vector2(170f, -210f) })
            {
                int i = Mathf.FloorToInt((p.x - ret.x) * ret.z * n), j = Mathf.FloorToInt((p.y - ret.y) * ret.z * n);
                float cx = ret.x + (i + 0.5f) * lado / n, cz = ret.y + (j + 0.5f) * lado / n;
                Assert.AreEqual(Mathf.RoundToInt(Ilha.Costa(R, cx, cz) * 255f), px[j * n + i].r, 1, "texel de " + p);
            }
            Assert.AreEqual(255, px[(n / 2) * n + n / 2].r, "o miolo da ilha e' terra");

            // de ponta a ponta, como o shader le': a espuma nasce NA linha d'agua (q 0), a terra fica abaixo de 0 e o mar alem do
            // retangulo (o resto do mar de 6 km) e' fundo pelo clamp
            foreach (float g in Rumos)
            {
                float a = g * Mathf.Deg2Rad, m = Beira(a);
                Assert.AreEqual(0f, QNoShader(px, n, ret, Mathf.Cos(a) * m, Mathf.Sin(a) * m), 0.03f, "q na linha d'agua, rumo " + g);
            }
            Assert.Less(QNoShader(px, n, ret, 0f, 0f), 0f, "a terra le' q < 0");
            Assert.AreEqual(1f, QNoShader(px, n, ret, 2500f, -1800f), 1e-6f, "fora do retangulo = mar fundo, como antes");

            for (int k = 0; k < n; k++)
            {
                Assert.AreEqual(0, px[k].r, "o anel de fora (sul) e' mar fundo: o clamp o estende pelo mar de 6 km");
                Assert.AreEqual(0, px[(n - 1) * n + k].r, "o anel de fora (norte) e' mar fundo");
                Assert.AreEqual(0, px[k * n].r, "o anel de fora (oeste) e' mar fundo");
                Assert.AreEqual(0, px[k * n + n - 1].r, "o anel de fora (leste) e' mar fundo");
            }
            int raso = 0, linhaDagua = Mathf.RoundToInt(255f / (1f + Ilha.CostaSeco));
            foreach (Color32 c in px) if (c.r > 0 && c.r < linhaDagua) raso++;
            float f = (float)raso / px.Length;
            Assert.That(f, Is.InRange(0.03f, 0.25f), "a agua rasa e' um anel em volta da ilha, nao o mar todo (" + (f * 100f).ToString("F1") + "% dos texels)");
        }
    }
}
