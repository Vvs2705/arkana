using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>
    /// Paleta do GDD §10 + os widgets que Menu, Selecao, Config e a pausa compartilham (o MESMO botao em todo lugar).
    /// Tudo procedural; toda altura de alvo passa por Dp. Cores de elemento passam pelo FiltroDaltonismo.
    /// </summary>
    public static class Estilo
    {
        public static readonly Color NoiteFunda = Formas.Hex("#05070F");
        public static readonly Color Noite = Formas.Hex("#0B1026");
        public static readonly Color Painel = Formas.Hex("#0E1430");
        public static readonly Color PainelAlto = Formas.Hex("#101838");
        public static readonly Color Ouro = Formas.Hex("#F0C75E");
        public static readonly Color OuroFosco = Formas.Hex("#8A7336");
        public static readonly Color Raio = Formas.Hex("#F5D90A");   // o "K" da wordmark
        public static readonly Color Texto = Formas.Hex("#E8E6F0");
        public static readonly Color TextoFosco = Formas.Hex("#9A97AD");

        public const float AlvoDp = 48f;
        public const float BotaoDp = 56f;
        public const float RespiroDp = 12f;

        /// <summary>Cor CRUA de cada elemento (as gemas do Selo, GDD §10).</summary>
        public static Color CorCrua(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fogo: return Formas.Hex("#FF5A2A");
                case Elemento.Agua: return Formas.Hex("#2AA7FF");
                case Elemento.Raio: return Formas.Hex("#F5D90A");
                case Elemento.Terra: return Formas.Hex("#A8763E");
                default: return Formas.Hex("#8FE8C9");
            }
        }

        /// <summary>Cor do elemento ja' passada pelo modo daltonismo em vigor — a UI pinta SEMPRE por aqui.</summary>
        public static Color CorElemento(Elemento e) => FiltroDaltonismo.Aplicar(CorCrua(e));

        /// <summary>Altura de alvo em px: nunca abaixo do proprio valor em px (dpi baixo nao encolhe o alvo).</summary>
        public static float AlturaAlvo(float dp = AlvoDp) => Mathf.Max(Dp.Px(dp), dp);

        public static Text Rotulo(Transform pai, string texto, float tamDp, Color cor, TextAnchor ancora = TextAnchor.MiddleCenter)
        {
            var t = Formas.Texto(pai, "Rotulo", texto, tamDp, cor, ancora);
            return t;
        }

        /// <summary>Paragrafo que quebra linha (perfil do mago, notas de config).</summary>
        public static Text Paragrafo(Transform pai, string texto, float tamDp, Color cor)
        {
            var t = Formas.Texto(pai, "Paragrafo", texto, tamDp, cor, TextAnchor.UpperLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        /// <summary>Botao de menu: painel escuro, borda dourada, alvo >= 48dp. Devolve o Button (Text filho "Rotulo").</summary>
        public static Button Botao(Transform pai, string nome, string texto, float larguraDp = 300f, float alturaDp = BotaoDp, float fonteDp = 18f)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(Dp.Px(larguraDp), AlturaAlvo(alturaDp));
            var img = go.GetComponent<Image>();
            img.color = PainelAlto;
            var borda = Formas.Imagem(go.transform, "Borda", null, Formas.ComAlfa(OuroFosco, 0.8f));
            AreaSegura.Esticar(borda.rectTransform);
            borda.transform.SetAsFirstSibling();
            borda.rectTransform.offsetMin = new Vector2(-2, -2);
            borda.rectTransform.offsetMax = new Vector2(2, 2);
            var b = go.GetComponent<Button>();
            var cores = b.colors;
            cores.normalColor = Color.white;
            cores.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            cores.pressedColor = Ouro;
            b.colors = cores;
            var t = Formas.Texto(go.transform, "Rotulo", texto, fonteDp, Ouro);
            AreaSegura.Esticar(t.rectTransform);
            return b;
        }

        /// <summary>Botao invisivel que cobre outro no' (card, tela de titulo): o alvo e' a area inteira.</summary>
        public static Button BotaoInvisivel(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            AreaSegura.Esticar((RectTransform)go.transform);
            return go.GetComponent<Button>();
        }

        /// <summary>Painel escuro com moldura dourada (fundo de card/perfil).</summary>
        public static Image Moldura(Transform pai, string nome, bool forte = true)
        {
            var borda = Formas.Imagem(pai, nome, null, Formas.ComAlfa(OuroFosco, forte ? 0.6f : 0.35f));
            var miolo = Formas.Imagem(borda.transform, "Miolo", null, Painel);
            AreaSegura.Esticar(miolo.rectTransform);
            miolo.rectTransform.offsetMin = new Vector2(1, 1);
            miolo.rectTransform.offsetMax = new Vector2(-1, -1);
            return borda;
        }

        /// <summary>Fundo azul-noite de tela cheia (sangra ate' o notch de proposito).</summary>
        public static Image Fundo(Transform pai)
        {
            var f = Formas.Imagem(pai, "Fundo", null, Noite);
            AreaSegura.Esticar(f.rectTransform);
            f.raycastTarget = true;   // bloqueia o toque de vazar para o jogo atras
            return f;
        }

        /// <summary>Coluna vertical centrada com espacamento em dp.</summary>
        public static VerticalLayoutGroup Coluna(Transform pai, string nome, float espacoDp)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(pai, false);
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.spacing = Dp.Px(espacoDp);
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = false; v.childControlHeight = false;
            v.childForceExpandWidth = false; v.childForceExpandHeight = false;
            var f = go.GetComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            f.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            return v;
        }

        public static LayoutElement Tamanho(Component c, float larguraPx, float alturaPx)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = larguraPx; le.preferredHeight = alturaPx;
            le.minWidth = larguraPx; le.minHeight = alturaPx;
            ((RectTransform)c.transform).sizeDelta = new Vector2(larguraPx, alturaPx);
            return le;
        }
    }
}
