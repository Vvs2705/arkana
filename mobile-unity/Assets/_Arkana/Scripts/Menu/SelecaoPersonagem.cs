using System;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>
    /// Grade dos 20 magos (dados: Core.Kits). Retrato de Resources/Retratos/NN (512 px, importado depois de
    /// mobile-godot/godot/menu/art/NN.png); sem arquivo, um quadrado na cor do mago — a tela nunca fica com buraco.
    /// Mago sem kit implementado mostra "sem kit" (o botao da HUD nasce apagado para ele). Escolha persistida em PlayerPrefs.
    /// </summary>
    public sealed class SelecaoPersonagem : MonoBehaviour
    {
        public const string PrefEscolhido = "arkana.mago";
        public const string T_TITULO = Textos.SelTitulo, T_SEM_KIT = Textos.SelEmBreve, T_VOLTAR = Textos.Voltar;
        public event Action VoltarPedido;
        public event Action<string> Escolhido;

        /// <summary>Slug do mago escolhido (persistido). Padrao: o primeiro slug com kit implementado, senao o primeiro.</summary>
        public static string MagoEscolhido
        {
            get
            {
                string s = PlayerPrefs.GetString(PrefEscolhido, "");
                if (!string.IsNullOrEmpty(s) && Array.IndexOf(Kits.Slugs, s) >= 0) return s;
                return SlugPadrao();
            }
            set { PlayerPrefs.SetString(PrefEscolhido, value ?? ""); PlayerPrefs.Save(); }
        }

        public static string SlugPadrao()
        {
            foreach (var slug in Kits.Slugs) { var k = Kit(slug); if (k != null && k.Implementado) return slug; }
            return Kits.Slugs.Length > 0 ? Kits.Slugs[0] : "";
        }

        static Kits.KitDef Kit(string slug) { Kits.KitDef k; return Kits.Magos.TryGetValue(slug, out k) ? k : null; }

        /// <summary>Cor de fallback do retrato: determinista por slug (o mesmo mago tem a mesma cor em todo aparelho).</summary>
        public static Color CorDoMago(string slug)
        {
            int h = 0;
            foreach (char c in slug ?? "") h = h * 31 + c;
            float hue = ((h & 0x7fffffff) % 360) / 360f;
            return Color.HSVToRGB(hue, 0.45f, 0.55f);
        }

        public static Sprite Retrato(string slug)
        {
            if (string.IsNullOrEmpty(slug) || slug.Length < 2) return null;
            return Resources.Load<Sprite>("Retratos/" + slug.Substring(0, 2));
        }

        RectTransform _raiz;
        Image[] _molduras = new Image[0];
        string[] _slugs = new string[0];
        bool _seleciona;

        public static SelecaoPersonagem Criar(Transform pai, bool seleciona = true)
        {
            var go = new GameObject("Selecao", typeof(RectTransform), typeof(SelecaoPersonagem));
            go.transform.SetParent(pai, false);
            var s = go.GetComponent<SelecaoPersonagem>();
            s._raiz = (RectTransform)go.transform;
            s._seleciona = seleciona;
            AreaSegura.Esticar(s._raiz);
            s.Montar();
            return s;
        }

        void Montar()
        {
            Estilo.Fundo(_raiz);
            Margens m = AreaSegura.Atual();
            var conteudo = Formas.No(_raiz, "Conteudo");
            AreaSegura.EsticarDentro(conteudo, m, Dp.Px(Estilo.RespiroDp));
            float alt = Estilo.AlturaAlvo() + 8f;
            var titulo = Formas.Texto(conteudo, "Titulo", T_TITULO, 26f, Estilo.Ouro);
            titulo.rectTransform.anchorMin = new Vector2(0, 1); titulo.rectTransform.anchorMax = new Vector2(1, 1); titulo.rectTransform.pivot = new Vector2(0.5f, 1);
            titulo.rectTransform.anchoredPosition = Vector2.zero; titulo.rectTransform.sizeDelta = new Vector2(0, alt);
            var voltar = Estilo.Botao(conteudo, "BtnVoltar", T_VOLTAR, 150f, Estilo.AlvoDp, 16f);
            var vrt = (RectTransform)voltar.transform;
            vrt.anchorMin = new Vector2(0, 1); vrt.anchorMax = new Vector2(0, 1); vrt.pivot = new Vector2(0, 1); vrt.anchoredPosition = Vector2.zero;
            voltar.onClick.AddListener(() => VoltarPedido?.Invoke());

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(conteudo, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = Vector2.zero; srt.offsetMax = new Vector2(0, -alt - 4f);
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            scrollGo.GetComponent<Mask>().showMaskGraphic = false;
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            var grade = Formas.No(srt, "Grade");
            grade.anchorMin = new Vector2(0, 1); grade.anchorMax = new Vector2(1, 1); grade.pivot = new Vector2(0.5f, 1);
            var grid = grade.gameObject.AddComponent<GridLayoutGroup>();
            float cw = Dp.Px(120f), ch = Dp.Px(120f);
            grid.cellSize = new Vector2(cw, ch);
            grid.spacing = new Vector2(Dp.Px(8f), Dp.Px(8f));
            grid.childAlignment = TextAnchor.UpperCenter;
            grade.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = grade;

            _slugs = Kits.Slugs ?? new string[0];
            _molduras = new Image[_slugs.Length];
            string escolhido = MagoEscolhido;
            for (int i = 0; i < _slugs.Length; i++)
            {
                string slug = _slugs[i];
                var kit = Kit(slug);
                bool impl = kit != null && kit.Implementado;
                var card = Estilo.Moldura(grade, "Card" + slug, impl);
                _molduras[i] = card;
                var retrato = Formas.Imagem(card.transform, "Retrato", Retrato(slug), Color.white);
                if (retrato.sprite == null) { retrato.sprite = Formas.Quadrado(); retrato.color = CorDoMago(slug); }
                retrato.preserveAspect = true;
                retrato.rectTransform.anchorMin = new Vector2(0.1f, 0.35f); retrato.rectTransform.anchorMax = new Vector2(0.9f, 0.95f);
                retrato.rectTransform.offsetMin = Vector2.zero; retrato.rectTransform.offsetMax = Vector2.zero;
                var nome = Formas.Texto(card.transform, "Nome", kit != null ? kit.Nome : slug, 12f, Estilo.Texto);
                nome.rectTransform.anchorMin = new Vector2(0, 0.17f); nome.rectTransform.anchorMax = new Vector2(1, 0.34f); nome.rectTransform.offsetMin = Vector2.zero; nome.rectTransform.offsetMax = Vector2.zero;
                var sub = Formas.Texto(card.transform, "Sub", impl ? "" : T_SEM_KIT, 10f, Estilo.TextoFosco);
                sub.rectTransform.anchorMin = new Vector2(0, 0.02f); sub.rectTransform.anchorMax = new Vector2(1, 0.17f); sub.rectTransform.offsetMin = Vector2.zero; sub.rectTransform.offsetMax = Vector2.zero;
                if (_seleciona)
                {
                    var toque = Estilo.BotaoInvisivel(card.transform, "Toque");   // o card INTEIRO e' o alvo (>= 48dp)
                    string s = slug;
                    toque.onClick.AddListener(() => Escolher(s));
                }
            }
            Pintar(escolhido);
        }

        void Escolher(string slug)
        {
            MagoEscolhido = slug;
            Pintar(slug);
            Escolheu(slug);
        }

        void Escolheu(string slug) { Escolhido?.Invoke(slug); }

        void Pintar(string escolhido)
        {
            for (int i = 0; i < _molduras.Length; i++)
                _molduras[i].color = _seleciona && _slugs[i] == escolhido ? Estilo.Ouro : Formas.ComAlfa(Estilo.OuroFosco, 0.5f);
        }
    }
}
