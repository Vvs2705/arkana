using UnityEngine;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>
    /// O SELO DE ARKANA (GDD §10): pentagono dourado, 5 gemas-losango nas pontas com as cores dos elementos, "A" no
    /// centro. Desenhado em Texture2D por codigo: boot splash, icone e tela de titulo usam o MESMO desenho
    /// (uma verdade so'; quando o Diretor mudar o dourado, muda aqui).
    /// </summary>
    public static class Selo
    {
        static readonly Elemento[] Gemas = { Elemento.Fogo, Elemento.Agua, Elemento.Terra, Elemento.Vento, Elemento.Raio };

        /// <summary>Conta pura: pixels RGBA (linha 0 = embaixo, padrao Unity) de um selo de `lado` px, fundo transparente.</summary>
        public static Color32[] Pixels(int lado, float fase = 0f)
        {
            var px = new Color32[lado * lado];
            float c = lado * 0.5f;
            float r = lado * 0.38f;
            float g = lado * 0.05f;
            Color ouro = Estilo.Ouro, fosco = Estilo.OuroFosco;
            var pts = new Vector2[5];
            for (int i = 0; i < 5; i++)
            {
                float a = (-90f + i * 72f) * Mathf.Deg2Rad;
                pts[i] = new Vector2(c + Mathf.Cos(a) * r, c - Mathf.Sin(a) * r);   // y invertido: a ponta 0 fica em CIMA
            }
            float espessura = Mathf.Max(lado / 135f, 1f);
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Color cor = new Color(0, 0, 0, 0);
                    // anel externo tenue
                    float d = Vector2.Distance(p, new Vector2(c, c));
                    if (Mathf.Abs(d - r * 1.16f) < espessura * 0.75f) cor = Mistura(cor, fosco, 0.35f);
                    // arestas do pentagono
                    for (int i = 0; i < 5; i++)
                        if (DistSeg(p, pts[i], pts[(i + 1) % 5]) < espessura) cor = Mistura(cor, fosco, 0.7f);
                    // gemas (losangos) nas pontas
                    for (int i = 0; i < 5; i++)
                    {
                        float dx = Mathf.Abs(p.x - pts[i].x), dy = Mathf.Abs(p.y - pts[i].y);
                        if (dx + dy <= g)
                        {
                            float brilho = 0.55f + 0.25f * Mathf.Sin(fase * 2f + i * 1.3f);
                            cor = Mistura(cor, Estilo.CorCrua(Gemas[i]), brilho);
                            if (dx + dy > g - espessura) cor = Mistura(cor, ouro, 0.5f);
                        }
                    }
                    px[y * lado + x] = cor;
                }
            DesenharA(px, lado, ouro);
            return px;
        }

        /// <summary>O "A" central: duas hastes e a barra, em vetor (nao depende de fonte no boot).</summary>
        static void DesenharA(Color32[] px, int lado, Color ouro)
        {
            float c = lado * 0.5f;
            float alt = lado * 0.30f;
            float meia = lado * 0.11f;
            float esp = Mathf.Max(lado / 40f, 1.5f);
            Vector2 topo = new Vector2(c, c + alt * 0.55f);
            Vector2 esq = new Vector2(c - meia, c - alt * 0.45f);
            Vector2 dir = new Vector2(c + meia, c - alt * 0.45f);
            Vector2 b1 = Vector2.Lerp(esq, topo, 0.4f), b2 = Vector2.Lerp(dir, topo, 0.4f);
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (DistSeg(p, esq, topo) < esp || DistSeg(p, dir, topo) < esp || DistSeg(p, b1, b2) < esp * 0.8f)
                        px[y * lado + x] = Formas.ComAlfa(ouro, 0.92f);
                }
        }

        static Color Mistura(Color baseCor, Color nova, float alfa)
        {
            if (baseCor.a <= 0f) return Formas.ComAlfa(nova, alfa);
            return Color.Lerp(baseCor, Formas.ComAlfa(nova, Mathf.Max(alfa, baseCor.a)), alfa);
        }

        static float DistSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return Vector2.Distance(p, a + ab * t);
        }

        public static Texture2D Textura(int lado, float fase = 0f)
        {
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            tex.SetPixels32(Pixels(lado, fase));
            tex.Apply();
            return tex;
        }

        public static Sprite SpriteDe(int lado)
        {
            var t = Textura(lado);
            return Sprite.Create(t, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
