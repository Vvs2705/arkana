using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Menu;

namespace Arkana.Tests
{
    /// <summary>
    /// A MARCA desenhada em codigo (Logo), pura: os glifos viram poligonos validos (sem NaN, sem area zero), as 6 letras de
    /// ARKANA estao la' em ordem e so' o K tem o raio; a caixa tem proporcao de marca (larga e baixa); a malha do Graphic e'
    /// de triangulos validos com a caixa da tinta cabendo no retangulo sem entortar; e a arte assada tem face dentro de cada
    /// letra, o amarelo do raio so' no K e a borda transparente (nada cortado). Vermelho se sumir uma letra, se as letras se
    /// empilharem, se a malha entortar ou se o assado sair vazio/do avesso.
    /// </summary>
    public class UiLogoTests
    {
        static int Letras => Textos.Marca.Length;

        static Rect CaixaDaLetra(int l)
        {
            Rect r = Rect.zero; bool tem = false;
            foreach (Logo.Peca p in Logo.Pecas)
            {
                if (p.Letra != l) continue;
                r = tem ? Rect.MinMaxRect(Mathf.Min(r.xMin, p.Caixa.xMin), Mathf.Min(r.yMin, p.Caixa.yMin), Mathf.Max(r.xMax, p.Caixa.xMax), Mathf.Max(r.yMax, p.Caixa.yMax)) : p.Caixa;
                tem = true;
            }
            Assert.IsTrue(tem, "a letra " + l + " (" + Textos.Marca[l] + ") sumiu da marca");
            return r;
        }

        [Test]
        public void Glifos_PoligonosValidos_AsSeisLetrasEmOrdem_SoOKTemRaio()
        {
            Assert.AreEqual(6, Letras, "ARKANA");
            foreach (Logo.Peca p in Logo.Pecas)
            {
                Assert.IsNotNull(p.Contornos);
                Assert.Greater(p.Contornos.Length, 0);
                foreach (Vector2[] c in p.Contornos)
                {
                    Assert.GreaterOrEqual(c.Length, 3, "contorno com menos de 3 pontos");
                    float area = 0f;
                    for (int i = 0, j = c.Length - 1; i < c.Length; j = i++)
                    {
                        Assert.IsFalse(float.IsNaN(c[i].x) || float.IsNaN(c[i].y) || float.IsInfinity(c[i].x) || float.IsInfinity(c[i].y), "ponto NaN/inf");
                        area += (c[j].x - c[i].x) * (c[j].y + c[i].y);
                    }
                    Assert.Greater(Mathf.Abs(area) * 0.5f, 4f, "poligono degenerado (area ~0) na letra " + p.Letra);
                }
                if (p.Raio) Assert.AreEqual(2, p.Letra, "o raio e' so' a perna do K (letra 2)");
            }
            bool kTemRaio = false;
            foreach (Logo.Peca p in Logo.Pecas) kTemRaio |= p.Raio;
            Assert.IsTrue(kTemRaio, "o K perdeu o raio (GDD §10)");

            float xAntes = float.MinValue;
            for (int l = 0; l < Letras; l++)
            {
                Rect r = CaixaDaLetra(l);
                Assert.That(r.height, Is.InRange(98f, 135f), "altura de caixa da letra " + Textos.Marca[l]);
                Assert.That(r.width, Is.InRange(60f, 115f), "largura da letra " + Textos.Marca[l]);
                Assert.Greater(r.center.x, xAntes + 70f, "as letras andam para a direita, sem se empilhar");
                xAntes = r.center.x;
            }
            Assert.That(Logo.Aspecto, Is.InRange(3.8f, 5.2f), "marca larga e baixa (o titulo tem 300 dp de largura)");
            Rect cx = Logo.Caixa;
            Assert.That(cx.height, Is.InRange(110f, 160f), "caixa da tinta: letra + lanca + contorno");
        }

        [Test]
        public void Malha_TriangulosValidos_ACaixaCabeSemEntortar_EOReflexoPorCima()
        {
            var r = new Rect(-150f, -50f, 300f, 100f);   // mais alto que a proporcao: a marca cabe pela largura
            var v = new List<UIVertex>();
            var t = new List<int>();
            Logo.Preencher(v, t, r, new Color32(255, 255, 255, 255), -1f);
            Assert.AreEqual(4, v.Count, "parada: so' o quad da arte");
            Vector2 min, max;
            Triangulos(v, t, out min, out max);
            int altArte, cel, alt;
            Rect mo = Logo.Moldura(Logo.LarguraTextura, out altArte, out cel, out alt);
            Assert.AreEqual(mo.width / mo.height, (max.x - min.x) / (max.y - min.y), 1e-3f, "o quad tem a proporcao da arte (nao entorta)");
            float k = (max.x - min.x) / mo.width;   // px por unidade
            Assert.AreEqual(r.width, Logo.Caixa.width * k, 0.05f, "a caixa da tinta ocupa a largura toda do retangulo");
            Assert.LessOrEqual(Logo.Caixa.height * k, r.height + 0.05f, "e cabe na altura");
            Vector2 centroDaTinta = (min + max) * 0.5f + (Logo.Caixa.center - mo.center) * k;
            Assert.AreEqual(r.center.x, centroDaTinta.x, 0.05f, "a tinta centrada no retangulo");
            Assert.AreEqual(r.center.y, centroDaTinta.y, 0.05f);

            Logo.Preencher(v, t, r, new Color32(255, 255, 255, 255), 0.5f);
            Assert.GreaterOrEqual(v.Count, 4 + 14, "com a fase, o reflexo (7 pares de vertices) entra por cima do quad");
            Triangulos(v, t, out min, out max);
            float vArte = altArte / (float)alt;
            for (int i = 4; i < 4 + 14; i++)
            {
                Assert.LessOrEqual(v[i].color.a, Mathf.CeilToInt(Logo.FaixaAlfa * 255f), "reflexo e' veu, nao tinta chapada");
                Assert.GreaterOrEqual(v[i].uv0.y, vArte - 1e-4f, "o reflexo amostra a mascara da face, nao a arte");
            }
        }

        /// <summary>Indices na faixa, triangulos com area (nada degenerado), vertices finitos; devolve a caixa do quad da arte.</summary>
        static void Triangulos(List<UIVertex> v, List<int> t, out Vector2 min, out Vector2 max)
        {
            Assert.Greater(t.Count, 0);
            Assert.AreEqual(0, t.Count % 3);
            foreach (int i in t) Assert.That(i, Is.InRange(0, v.Count - 1), "indice fora da malha");
            foreach (UIVertex u in v)
                Assert.IsFalse(float.IsNaN(u.position.x) || float.IsNaN(u.position.y) || float.IsInfinity(u.position.x) || float.IsInfinity(u.position.y), "vertice NaN");
            for (int i = 0; i < t.Count; i += 3)
            {
                Vector3 a = v[t[i]].position, b = v[t[i + 1]].position, c = v[t[i + 2]].position;
                float area = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
                Assert.Greater(area, 1e-3f, "triangulo degenerado");
            }
            min = Vector2.Min(v[0].position, v[2].position);
            max = Vector2.Max(v[0].position, v[2].position);
        }

        [Test]
        public void Arte_FaceEmCadaLetra_ORaioSoNoK_EBordaTransparente()
        {
            const int W = 320;
            int alt, altArte, cel, alt2;
            Color32[] px = Logo.Pixels(W, out alt, out altArte);
            Rect mo = Logo.Moldura(W, out altArte, out cel, out alt2);
            Assert.AreEqual(alt, alt2);
            Assert.AreEqual(W * alt, px.Length);
            for (int x = 0; x < W; x++)
            {
                Assert.AreEqual(0, px[x].a, "linha de baixo da arte transparente (a sombra coube)");
                Assert.AreEqual(0, px[(altArte - 1) * W + x].a, "linha de cima transparente (a lanca coube)");
            }
            for (int y = 0; y < altArte; y++)
            {
                Assert.AreEqual(0, px[y * W].a, "coluna da esquerda transparente");
                Assert.AreEqual(0, px[y * W + W - 1].a, "coluna da direita transparente");
            }
            float s = W / mo.width;
            for (int l = 0; l < Letras; l++)
            {
                Rect r = CaixaDaLetra(l);
                int x0 = Mathf.RoundToInt((r.xMin - mo.xMin) * s), x1 = Mathf.RoundToInt((r.xMax - mo.xMin) * s);
                int y0 = Mathf.RoundToInt((0f - mo.yMin) * s), y1 = Mathf.RoundToInt((Logo.Altura - mo.yMin) * s);
                int face = 0, raio = 0, total = (x1 - x0) * (y1 - y0);
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        if (px[(y + altArte) * W + x].a > 128) face++;   // a mascara da face
                        Color32 c = px[y * W + x];
                        if (c.a > 200 && c.b < 60 && c.r > 180 && c.g > 0.84f * c.r) raio++;   // o amarelo do Raio (#F5D90A)
                    }
                Assert.Greater(face, total * 0.18f, "a letra " + Textos.Marca[l] + " tem face dourada");
                Assert.Less(face, total * 0.85f, "e contraforma (nao e' um bloco)");
                if (l == 2) Assert.Greater(raio, total * 0.02f, "o K tem a perna em raio amarelo");
                else Assert.LessOrEqual(raio, total * 0.005f, "o amarelo do raio e' so' do K (" + Textos.Marca[l] + ")");
            }
        }
    }
}
