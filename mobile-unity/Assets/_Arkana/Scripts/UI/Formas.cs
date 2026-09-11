using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;

namespace Arkana.UI
{
    /// <summary>
    /// Sprites gerados por codigo (zero asset): disco, anel, X, e a FORMA de cada elemento (GDD §10:
    /// cor + forma, nunca so' cor). Tudo em cache — cada textura nasce uma vez por processo.
    /// Tambem a fabrica de uGUI que toda tela repete (Canvas, Image, Text com a fonte embutida).
    /// </summary>
    public static class Formas
    {
        const int Lado = 64;
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Disco() => Get("disco", (x, y) => Dentro(x, y, 0.98f));
        public static Sprite Anel() => Get("anel", (x, y) => Dentro(x, y, 0.98f) && !Dentro(x, y, 0.84f));
        public static Sprite Xis() => Get("xis", (x, y) => Mathf.Abs(Mathf.Abs(x) - Mathf.Abs(y)) < 0.14f && Mathf.Abs(x) < 0.7f);
        public static Sprite Quadrado() => Get("quadrado", (x, y) => Mathf.Abs(x) < 0.95f && Mathf.Abs(y) < 0.95f);
        public static Sprite Triangulo() => Get("triangulo", (x, y) => y > -0.8f && Mathf.Abs(x) < (0.8f - y) * 0.6f);
        public static Sprite Losango() => Get("losango", (x, y) => Mathf.Abs(x) + Mathf.Abs(y) < 0.95f);
        /// <summary>Seta apontando para cima (bussola: gira por rotacao do RectTransform).</summary>
        public static Sprite Seta() => Get("seta", (x, y) => y > -0.9f && Mathf.Abs(x) < (0.9f - y) * 0.45f);

        /// <summary>Circulo (fogo), gota (agua), raio, quadrado (terra), espiral (vento).</summary>
        public static Sprite DoElemento(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fogo: return Get("el_fogo", (x, y) => Dentro(x, y, 0.7f));
                case Elemento.Agua: return Get("el_agua", (x, y) => Dentro(x, y + 0.25f, 0.5f) || (y > -0.25f && y < 0.85f && Mathf.Abs(x) < (0.85f - y) * 0.45f));
                case Elemento.Raio: return Get("el_raio", Raio);
                case Elemento.Terra: return Get("el_terra", (x, y) => Mathf.Abs(x) < 0.62f && Mathf.Abs(y) < 0.62f);
                default: return Get("el_vento", Espiral);
            }
        }

        static bool Dentro(float x, float y, float r) => x * x + y * y <= r * r;

        static bool Raio(float x, float y)
        {
            // zigue-zague: dois segmentos diagonais grossos
            float a = y > 0f ? x - (0.35f - y * 0.7f) : x - (-0.35f - y * 0.7f);
            return Mathf.Abs(a) < 0.16f && Mathf.Abs(y) < 0.85f;
        }

        static bool Espiral(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 0.9f || r < 0.08f) return false;
            float ang = Mathf.Atan2(y, x);
            if (ang < 0f) ang += Mathf.PI * 2f;
            // r cresce com o angulo (2,2 voltas); traco largo o bastante para ler a 48dp
            for (int volta = 0; volta < 3; volta++)
            {
                float alvo = (ang + volta * Mathf.PI * 2f) / (Mathf.PI * 2f * 2.2f) * 0.9f;
                if (Mathf.Abs(r - alvo) < 0.09f) return true;
            }
            return false;
        }

        static Sprite Get(string chave, Func<float, float, bool> f)
        {
            Sprite s;
            if (_cache.TryGetValue(chave, out s) && s != null) return s;
            var tex = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[Lado * Lado];
            for (int y = 0; y < Lado; y++)
                for (int x = 0; x < Lado; x++)
                {
                    // 4 amostras por pixel: antialias barato nas bordas
                    int n = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float fx = ((x + 0.25f + sx * 0.5f) / Lado) * 2f - 1f;
                            float fy = ((y + 0.25f + sy * 0.5f) / Lado) * 2f - 1f;
                            if (f(fx, fy)) n++;
                        }
                    px[y * Lado + x] = new Color32(255, 255, 255, (byte)(n * 63));
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, Lado, Lado), new Vector2(0.5f, 0.5f), 100f);
            _cache[chave] = s;
            return s;
        }

        // ---------- fabrica de uGUI (o que toda tela repete) ----------

        static Font _fonte;

        /// <summary>Fonte embutida do Unity: zero asset no repo.</summary>
        public static Font Fonte()
        {
            if (_fonte == null) _fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _fonte;
        }

        public static RectTransform No(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        public static Image Imagem(Transform pai, string nome, Sprite sprite, Color cor)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pai, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = cor;
            img.raycastTarget = false;
            return img;
        }

        public static Text Texto(Transform pai, string nome, string texto, float tamDp, Color cor, TextAnchor ancora = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(pai, false);
            var t = go.GetComponent<Text>();
            t.font = Fonte();
            t.text = texto;
            t.fontSize = Mathf.Max(Mathf.RoundToInt(Dp.Px(tamDp)), 8);
            t.color = cor;
            t.alignment = ancora;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.8f);
            sh.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        public static Canvas CanvasTelaCheia(string nome, int ordem)
        {
            var go = new GameObject(nome, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = ordem;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; // px de canvas = px de tela (Dp e AreaSegura contam com isso)
            sc.scaleFactor = 1f;
            return c;
        }

        /// <summary>Cor -> versao escurecida (mesma leitura do Godot Color.darkened).</summary>
        public static Color Escurecer(Color c, float k) => new Color(c.r * (1f - k), c.g * (1f - k), c.b * (1f - k), c.a);

        public static Color ComAlfa(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>Le "RRGGBB"/"#RRGGBB"; falha vira branco (nunca derruba a HUD).</summary>
        public static Color Hex(string hex)
        {
            Color c;
            if (string.IsNullOrEmpty(hex)) return Color.white;
            string h = hex.StartsWith("#") ? hex : "#" + hex;
            return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.white;
        }

        /// <summary>Sobrecargas para o Balance poder guardar cor como hex OU Color sem quebrar a compilacao daqui.</summary>
        public static Color Cor(string hex) => Hex(hex);
        public static Color Cor(Color c) => c;
        public static Color Cor(Color32 c) => c;
    }
}
