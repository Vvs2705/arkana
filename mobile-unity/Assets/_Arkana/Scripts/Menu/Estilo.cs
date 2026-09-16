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
        public static readonly Color OuroClaro = Formas.Hex("#FFE9A6");   // o rotulo dos botoes (o Ouro chapado sumia na areia)
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

        /// <summary>
        /// Botao de menu: a PLACA do PlacaBotao (cantos chanfrados, contorno escuro, moldura dourada em degrade, fio
        /// interno e miolo escuro com volume) + gema de losango nas duas laterais e o rotulo em ouro claro com
        /// espacamento e contorno. Alvo >= 48dp. `principal` = o JOGAR: ouro cheio, letra escura e a aura respirando.
        /// Devolve o Button (Text filho "Rotulo": o Config le o rotulo por GetComponentInChildren).
        /// A borda NAO e' mais um filho esticado: filho desenha por cima do pai e cobria a placa (a oliva da foto 45).
        /// </summary>
        public static Button Botao(Transform pai, string nome, string texto, float larguraDp = 300f, float alturaDp = BotaoDp, float fonteDp = 18f, bool principal = false)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(Dp.Px(larguraDp), AlturaAlvo(alturaDp));
            var placa = go.GetComponent<Image>();
            PlacaBotao.Vestir(placa, principal ? PlacaBotao.Tipo.Cheia : PlacaBotao.Tipo.Escura);
            var b = go.GetComponent<Button>();
            b.targetGraphic = placa;
            var cores = b.colors;
            cores.normalColor = cores.highlightedColor = cores.selectedColor = Color.white;   // toque nao tem hover; solto nao fica aceso
            cores.pressedColor = new Color(0.66f, 0.64f, 0.68f);   // pressionado ESCURECE a placa (e o BotaoMenu afunda)
            cores.fadeDuration = 0f;
            b.colors = cores;
            var dedo = go.AddComponent<BotaoMenu>();
            if (principal) dedo.Aura = PlacaBotao.Aura(go.transform);
            Gema(go.transform, "GemaEsq", 0f);
            Gema(go.transform, "GemaDir", 1f);
            var t = Formas.Texto(go.transform, "Rotulo", texto, fonteDp, principal ? NoiteFunda : OuroClaro);
            t.fontStyle = FontStyle.Bold;
            t.GetComponent<Shadow>().enabled = false;   // o Letreiro tem de vir PRIMEIRO: sombra e contorno copiam a malha depois
            var letreiro = t.gameObject.AddComponent<Letreiro>();
            if (principal) letreiro.Baixo = new Color(1f, 1f, 1f, 0f);   // letra escura no ouro nao leva degrade
            else Arkana.Menu.Menu.Contorno(t);                           // o mesmo contorno do titulo: le' sobre a areia
            AreaSegura.Esticar(t.rectTransform);
            return b;
        }

        /// <summary>A gema de losango na lateral da placa: a escura por baixo (contorno) e o ouro por cima — o mesmo
        /// detalhe do losango da tela de carregamento.</summary>
        static void Gema(Transform pai, string nome, float ancoraX)
        {
            var escura = Formas.Imagem(pai, nome, Formas.Losango(), PlacaBotao.CorGema);
            FixarGema(escura.rectTransform, ancoraX, Dp.Px(PlacaBotao.GemaDp));
            var ouro = Formas.Imagem(escura.transform, "Ouro", Formas.Losango(), Ouro);
            FixarGema(ouro.rectTransform, 0.5f, Dp.Px(PlacaBotao.GemaOuroDp));
        }

        static void FixarGema(RectTransform rt, float ancoraX, float lado)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ancoraX, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(lado, lado);
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
