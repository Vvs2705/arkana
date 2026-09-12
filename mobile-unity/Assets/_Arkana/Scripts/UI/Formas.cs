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
        /// <summary>Anel de espessura escolhida (`interno` = raio do furo, 0..0.98): botoes de acao finos, disparo grosso.</summary>
        public static Sprite Anel(float interno) => Get("anel-" + Mathf.RoundToInt(interno * 100f), (x, y) => Dentro(x, y, 0.98f) && !Dentro(x, y, interno), 128);
        /// <summary>So' o CONTORNO do losango/triangulo (raridade no botao PEGAR: forma no aro, nao um bloco cheio).</summary>
        public static Sprite LosangoAnel() => Get("losango-anel", (x, y) => Mathf.Abs(x) + Mathf.Abs(y) < 0.95f && Mathf.Abs(x) + Mathf.Abs(y) >= 0.8f, 128);
        public static Sprite TrianguloAnel() => Get("triangulo-anel", (x, y) => Tri(x, y, 1f) && !Tri(x, y, 0.8f), 128);
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
        static bool Tri(float x, float y, float s) => y > -0.8f * s && Mathf.Abs(x) < (0.8f * s - y) * 0.6f;
        /// <summary>Smoothstep de verdade (o Mathf.SmoothStep do Unity interpola entre from/to, nao e' o do shader).</summary>
        static float Liso(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3f - 2f * t); }

        // ---------- sprites SUAVES (alfa continuo): o que deixa a HUD com cara de jogo e nao de placeholder ----------

        /// <summary>Halo desenhado num quadrado HaloEscala x o botao: brilho no aro, some para dentro e esvaece para fora.</summary>
        public const float HaloEscala = 1.5f;
        /// <summary>Raio (texels) dos cantos do Arredondado = borda do 9-slice.</summary>
        public const float ArredondadoRaio = 12f;

        /// <summary>
        /// Halo em volta de um botao redondo (Image com lado * HaloEscala, mesmo centro): em PRETO separa o botao da
        /// areia/ceu claro (foto de 12/09: disco claro sumia no chao); na COR da acao e' o brilho da suprema pronta.
        /// </summary>
        public static Sprite Halo() => GetSuave("halo", 64, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y), d = r - 1f / HaloEscala;
            float a = Mathf.Exp(-d * d / (d < 0f ? 0.006f : 0.03f));   // cai rapido para dentro, espalha para fora
            return new Color(1f, 1f, 1f, a * (1f - Liso(0.85f, 1f, r)));   // zera antes da borda do quadrado
        });

        /// <summary>Sombra macia (disco que esvaece ate' a borda): o miolo do joystick "descola" do chao.</summary>
        public static Sprite Sombra() => GetSuave("sombra", 64, (x, y) => new Color(1f, 1f, 1f, 1f - Liso(0.3f, 1f, Mathf.Sqrt(x * x + y * y))));

        /// <summary>Disco com volume: branco em cima, cinza embaixo (a Image tinge). 1 texel de antialias na borda.</summary>
        public static Sprite DiscoDegrade() => GetSuave("disco-degrade", 64, (x, y) =>
        {
            float v = Mathf.Lerp(0.62f, 1f, (y + 1f) * 0.5f);
            return new Color(v, v, v, Mathf.Clamp01((0.98f - Mathf.Sqrt(x * x + y * y)) * 32f));
        });

        /// <summary>
        /// Retangulo de cantos redondos para Image.Type.Sliced (moldura, trilho e preenchimento das barras).
        /// `degrade`: branco em cima, cinza embaixo — o preenchimento ganha volume (mais claro em cima) sem shader.
        /// </summary>
        public static Sprite Arredondado(bool degrade = false) => GetSuave(degrade ? "arred-degrade" : "arred", 32, (x, y) =>
        {
            const float r = ArredondadoRaio / 16f;   // 32 texels = 2 unidades: 12 texels = 0,75
            float qx = Mathf.Max(Mathf.Abs(x) - (1f - r), 0f), qy = Mathf.Max(Mathf.Abs(y) - (1f - r), 0f);
            float a = Mathf.Clamp01((r - Mathf.Sqrt(qx * qx + qy * qy)) * 16f + 0.5f);
            float v = degrade ? Mathf.Lerp(0.6f, 1f, (y + 1f) * 0.5f) : 1f;
            return new Color(v, v, v, a);
        }, ArredondadoRaio);

        /// <summary>Image 9-slice de cantos redondos com raio `raioPx` NA TELA (o multiplicador escala a borda do sprite).</summary>
        public static Image Arredondada(Transform pai, string nome, Color cor, float raioPx, bool degrade = false)
        {
            var img = Imagem(pai, nome, Arredondado(degrade), cor);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ArredondadoRaio / Mathf.Max(raioPx, 0.5f);
            return img;
        }

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

        /// <summary>
        /// Degrade vertical de ALFA (branco; a cor vem da Image). `escuroEmCima`: alfa cheio na borda de cima.
        /// Existe para a grade de leitura da HUD: retangulo chapado deixava FAIXA de borda dura atravessando o
        /// mundo (vista na foto de 11/09). Smoothstep para nao sobrar degrau.
        /// </summary>
        public static Sprite Degrade(bool escuroEmCima)
        {
            string chave = escuroEmCima ? "degrade-cima" : "degrade-baixo";
            Sprite s;
            if (_cache.TryGetValue(chave, out s) && s != null) return s;
            const int H = 64;
            var tex = new Texture2D(4, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[4 * H];
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)(H - 1);                 // 0 embaixo, 1 em cima
                float a = escuroEmCima ? t : 1f - t;
                a = a * a * (3f - 2f * a);
                byte b = (byte)Mathf.RoundToInt(a * 255f);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, b);
            }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 4, H), new Vector2(0.5f, 0.5f), 100f);
            _cache[chave] = s;
            return s;
        }

        /// <summary>`lado` maior so' para traco FINO (aros de 3-4dp): a 64 texels ele sai em contas quando ampliado 5x.</summary>
        static Sprite Get(string chave, Func<float, float, bool> f, int lado = Lado)
        {
            Sprite s;
            if (_cache.TryGetValue(chave, out s) && s != null) return s;
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    // 4 amostras por pixel: antialias barato nas bordas
                    int n = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float fx = ((x + 0.25f + sx * 0.5f) / lado) * 2f - 1f;
                            float fy = ((y + 0.25f + sy * 0.5f) / lado) * 2f - 1f;
                            if (f(fx, fy)) n++;
                        }
                    px[y * lado + x] = new Color32(255, 255, 255, (byte)(n * 63));
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f);
            _cache[chave] = s;
            return s;
        }

        // ponytail: uma textura por sprite (cada uma quebra o lote do uGUI); juntar num atlas se o profiler da HUD reclamar de draw call
        /// <summary>Gerador de cor + alfa CONTINUOS por pixel (1 amostra; a borda suave vem da propria funcao). `borda` &gt; 0 = 9-slice.</summary>
        static Sprite GetSuave(string chave, int lado, Func<float, float, Color> f, float borda = 0f)
        {
            Sprite s;
            if (_cache.TryGetValue(chave, out s) && s != null) return s;
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                    px[y * lado + x] = f(((x + 0.5f) / lado) * 2f - 1f, ((y + 0.5f) / lado) * 2f - 1f);
            tex.SetPixels32(px);
            tex.Apply();
            var r = new Rect(0, 0, lado, lado);
            s = borda > 0f
                ? Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(borda, borda, borda, borda))
                : Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100f);
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
