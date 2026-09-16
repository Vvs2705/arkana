using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>
    /// A PLACA dos botoes de menu (onda 16A). A foto 45 lia "placeholder": retangulo chapado cor de oliva — a Borda
    /// dourada era FILHA do botao e o uGUI desenha o filho POR CIMA do pai, entao ela cobria a placa inteira. Agora a
    /// placa e' UM sprite assado por codigo (campo de distancia, o mesmo espirito do Selo e do Logo) e vestido em
    /// 9-slice: cantos CHANFRADOS, contorno escuro, moldura dourada em degrade, vao, fio interno fino e o miolo escuro
    /// translucido com volume (luz em cima, sombra embaixo) — o idioma das placas da HUD e da tela de carregamento.
    /// Tres sabores: Escura (secundario), Cheia (o JOGAR, ouro batido com letra escura) e Aura (o brilho que respira
    /// por FORA do JOGAR; transparente por dentro, entao pode ser filha sem cobrir nada).
    /// </summary>
    public static class PlacaBotao
    {
        public enum Tipo { Escura, Cheia, Aura }

        /// <summary>Borda do 9-slice e miolo, em texels (1 texel ~ 1 px no Poco F4); margem = a folga da aura.</summary>
        public const int Borda = 40, Miolo = 8, Margem = 18;
        /// <summary>KNOB por foto: a borda do 9-slice NA TELA (o chanfro e a moldura escalam junto).</summary>
        public const float BordaDp = 16f;
        /// <summary>KNOB por foto (texels, medidos da borda para dentro): canto cortado, contorno escuro, moldura
        /// dourada, o vao escuro e o fio interno.</summary>
        public const float Chanfro = 19f, Contorno = 1.8f, Moldura0 = 1.8f, Moldura1 = 4.4f, Vao = 7.2f, Fio1 = 8.4f;
        /// <summary>KNOB: o tamanho da gema de losango nas duas laterais (a escura por baixo, o ouro por cima).</summary>
        public const float GemaDp = 13f, GemaOuroDp = 8f;

        // KNOB da paleta (hex direto: o assado e' conta pura). O ouro e' o mesmo do Logo/Estilo (GDD §10).
        static readonly Color Face0 = H(0xFFF6D2), Face1 = H(0xF0C75E), Face2 = H(0xD48E2C);
        static readonly Color CorContorno = H(0x1A0C05, 0.96f);
        static readonly Color FundoTopo = H(0x1E1A26, 0.80f), FundoBaixo = H(0x090810, 0.90f);
        static readonly Color CheioTopo = H(0xFFE9A2), CheioMeio = H(0xE9B23F), CheioBaixo = H(0x8A4A0E);
        static readonly Color CheioRim = H(0xFFF7DA), CheioFio = H(0x6E3C11, 0.55f);
        static readonly Color Luz = H(0xFFE6B0), Quente = H(0xC98A3A), LuzCheia = H(0xFFFDF0), EscuroCheia = H(0x5A3208);
        public static readonly Color CorAura = H(0xFFCA5A);
        public static readonly Color CorGema = H(0x1A0C05, 0.95f);

        static readonly Dictionary<Tipo, Sprite> _cache = new Dictionary<Tipo, Sprite>();

        public static int Lado(Tipo t) => 2 * ((t == Tipo.Aura ? Margem : 0) + Borda) + Miolo;

        /// <summary>
        /// Conta pura: os pixels RGBA da placa (linha 0 embaixo, padrao Unity). `d` = distancia para DENTRO da borda
        /// do retangulo chanfrado; cada faixa de `d` e' uma camada, pintada de dentro para fora.
        /// </summary>
        public static Color32[] Pixels(Tipo tipo)
        {
            int margem = tipo == Tipo.Aura ? Margem : 0;
            int lado = Lado(tipo);
            float h = lado * 0.5f - margem;
            var px = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
            {
                float t = 1f - (y + 0.5f) / lado;   // 0 no TOPO, 1 embaixo: a luz vem de cima
                Color fundo = tipo == Tipo.Cheia ? Degrade(t, CheioTopo, CheioMeio, CheioBaixo) : Degrade(t, FundoTopo, FundoTopo, FundoBaixo);
                Color moldura = tipo == Tipo.Cheia ? CheioRim : Degrade(t, Face0, Face1, Face2);
                Color fio = tipo == Tipo.Cheia ? CheioFio : Formas.ComAlfa(Face1, 0.45f);
                float sobe = tipo == Tipo.Cheia ? Quad(Mathf.Clamp01((0.30f - t) / 0.30f)) * 0.55f : Quad(Mathf.Clamp01((0.38f - t) / 0.38f)) * 0.16f;
                float desce = tipo == Tipo.Cheia ? Quad(Mathf.Clamp01((t - 0.74f) / 0.26f)) * 0.45f : Quad(Mathf.Clamp01((t - 0.70f) / 0.30f)) * 0.10f;
                for (int x = 0; x < lado; x++)
                {
                    float ax = Mathf.Abs(x + 0.5f - lado * 0.5f), ay = Mathf.Abs(y + 0.5f - lado * 0.5f);
                    float qx = ax - h, qy = ay - h;
                    float caixa = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float d = -Mathf.Max(caixa, (ax + ay - (2f * h - Chanfro)) * 0.70710678f);   // dentro > 0
                    if (tipo == Tipo.Aura)
                    {
                        float fora = Mathf.Max(-d, 0f) / (Margem * 0.55f);
                        px[y * lado + x] = new Color32(B(CorAura.r), B(CorAura.g), B(CorAura.b), B(Mathf.Exp(-fora * fora) * Mathf.Clamp01(-d + 0.5f) * 0.55f));
                        continue;
                    }
                    float dentro = Faixa(d, Vao, 1e6f);
                    Color acc = Sobre(new Color(0f, 0f, 0f, 0f), fundo, dentro);
                    acc = Sobre(acc, tipo == Tipo.Cheia ? LuzCheia : Luz, dentro * sobe);
                    acc = Sobre(acc, tipo == Tipo.Cheia ? EscuroCheia : Quente, dentro * desce);
                    acc = Sobre(acc, fio, Faixa(d, Vao, Fio1));
                    acc = Sobre(acc, fundo, Faixa(d, Moldura1, Vao));
                    acc = Sobre(acc, moldura, Faixa(d, Moldura0, Moldura1));
                    acc = Sobre(acc, CorContorno, Faixa(d, 0f, Contorno));
                    px[y * lado + x] = Reta(acc);
                }
            }
            return px;
        }

        /// <summary>Uma textura por sabor, por processo (o cache do Formas nao serve: aqui a borda do 9-slice varia).</summary>
        public static Sprite SpriteDe(Tipo tipo)
        {
            Sprite s;
            if (_cache.TryGetValue(tipo, out s) && s != null) return s;
            int lado = Lado(tipo);
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(Pixels(tipo));
            tex.Apply();
            float b = Borda + (tipo == Tipo.Aura ? Margem : 0);
            s = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            _cache[tipo] = s;
            return s;
        }

        /// <summary>Veste uma Image existente com a placa em 9-slice (a borda em texels vira BordaDp na tela).</summary>
        public static void Vestir(Image img, Tipo tipo)
        {
            img.sprite = SpriteDe(tipo);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.pixelsPerUnitMultiplier = Borda / Mathf.Max(Dp.Px(BordaDp), 0.5f);
        }

        /// <summary>A aura do botao principal: filha esticada PARA FORA da placa (transparente por dentro).</summary>
        public static Image Aura(Transform pai)
        {
            var img = Formas.Imagem(pai, "Aura", null, Color.white);
            Vestir(img, Tipo.Aura);
            AreaSegura.Esticar(img.rectTransform);
            float folga = Dp.Px(BordaDp) * Margem / Borda;
            img.rectTransform.offsetMin = new Vector2(-folga, -folga);
            img.rectTransform.offsetMax = new Vector2(folga, folga);
            return img;
        }

        static float Quad(float v) => v * v;
        static float Faixa(float d, float a, float b) => Mathf.Clamp01(d - a + 0.5f) * Mathf.Clamp01(b - d + 0.5f);
        static Color Degrade(float t, Color c0, Color c1, Color c2) => t < 0.55f ? Color.Lerp(c0, c1, t / 0.55f) : Color.Lerp(c1, c2, (t - 0.55f) / 0.45f);

        /// <summary>`c` por cima de `acc` com alfa `a` (o alfa da propria cor entra junto); acumulado PREmultiplicado.</summary>
        static Color Sobre(Color acc, Color c, float a)
        {
            a *= c.a;
            return new Color(c.r * a + acc.r * (1f - a), c.g * a + acc.g * (1f - a), c.b * a + acc.b * (1f - a), a + acc.a * (1f - a));
        }

        static Color32 Reta(Color p)
        {
            if (p.a <= 1e-4f) return new Color32(0, 0, 0, 0);
            return new Color32(B(p.r / p.a), B(p.g / p.a), B(p.b / p.a), B(p.a));
        }

        static byte B(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
        static Color H(uint rgb, float a = 1f) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);
    }

    /// <summary>
    /// O DEDO no botao de menu: afunda (encolhe) enquanto segura — o ColorTint do Button ja' escurece a placa — e, no
    /// botao principal, a aura RESPIRA (alfa pelo CanvasRenderer, sem refazer malha). Relogio sem escala: as
    /// Configuracoes tambem abrem na pausa, com timeScale 0.
    /// </summary>
    public sealed class BotaoMenu : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>KNOB por foto: quanto afunda no toque, a respiracao da aura (Hz) e os dois extremos do alfa dela.</summary>
        public const float Afunda = 0.97f, RespiraHz = 0.4f, AuraMin = 0.45f, AuraMax = 1f;

        /// <summary>A aura do principal (null nos secundarios: nao respira nada).</summary>
        public Image Aura;

        bool _preso;
        float _fase;

        public void OnPointerDown(PointerEventData e) { _preso = true; transform.localScale = new Vector3(Afunda, Afunda, 1f); }
        public void OnPointerUp(PointerEventData e) { _preso = false; transform.localScale = Vector3.one; }
        void OnDisable() { _preso = false; transform.localScale = Vector3.one; }

        void Update()
        {
            if (Aura == null) return;
            _fase += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(AuraMin, AuraMax, 0.5f + 0.5f * Mathf.Sin(_fase * RespiraHz * 2f * Mathf.PI));
            Aura.canvasRenderer.SetAlpha(_preso ? AuraMin : a);
        }
    }

    /// <summary>
    /// O ROTULO do botao: espacamento entre as letras (a fonte legada nao tem tracking) e o degrade do ouro dentro de
    /// cada letra. Cada glifo e' um quad de 4 vertices: ele anda para os lados na PROPORCAO da distancia ao centro da
    /// linha — assim o efeito sobrevive a' copia que o Outline/Shadow fazem depois (nao depende do indice do caractere).
    /// </summary>
    public sealed class Letreiro : BaseMeshEffect
    {
        /// <summary>KNOB: folga entre as letras, em fracao da largura da palavra (0,12 = 12% mais larga).</summary>
        public float Espaco = 0.12f;
        /// <summary>Multiplicador da linha de BAIXO de cada letra (o ouro descendo). Alfa 0 = sem degrade.</summary>
        public Color Baixo = new Color(0.86f, 0.72f, 0.45f, 1f);

        public override void ModifyMesh(VertexHelper vh)
        {
            int n = vh.currentVertCount;
            if (!IsActive() || n < 4 || n % 4 != 0) return;   // 4 vertices por glifo; fora disso, nao mexe
            var v = new UIVertex();
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < n; i++) { vh.PopulateUIVertex(ref v, i); min = Mathf.Min(min, v.position.x); max = Mathf.Max(max, v.position.x); }
            float meio = (min + max) * 0.5f;
            for (int q = 0; q < n; q += 4)
            {
                float cx = 0f, cy = 0f;
                for (int i = 0; i < 4; i++) { vh.PopulateUIVertex(ref v, q + i); cx += v.position.x * 0.25f; cy += v.position.y * 0.25f; }
                float dx = (cx - meio) * Espaco;
                for (int i = 0; i < 4; i++)
                {
                    vh.PopulateUIVertex(ref v, q + i);
                    v.position.x += dx;
                    if (Baixo.a > 0f && v.position.y < cy)
                        v.color = new Color32((byte)(v.color.r * Baixo.r), (byte)(v.color.g * Baixo.g), (byte)(v.color.b * Baixo.b), v.color.a);
                    vh.SetUIVertex(v, q + i);
                }
            }
        }
    }
}
