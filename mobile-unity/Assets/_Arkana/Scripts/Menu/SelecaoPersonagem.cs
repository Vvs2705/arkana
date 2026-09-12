using System;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Characters;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>
    /// A ESCOLHA DE MAGO como vitrine: painel escuro translucido a ESQUERDA com a grade dos 20 retratos (dados: Core.Kits)
    /// e o lado direito LIVRE — ali a VitrineDoMenu mostra o escolhido em 3D no pico, com o CARTAO dele ao lado (nome,
    /// elemento, porte, kit). Sem fundo proprio: o do Menu (translucido) ja' bloqueia o toque.
    /// Retrato de Resources/Retratos/NN (corpo inteiro, 512 px): a celula mostra o BUSTO (RawImage.uvRect, sem mascara);
    /// sem arquivo, o quadrado na cor do mago — a tela nunca fica com buraco. Mago sem kit sai apagado e o cartao diz
    /// "KIT EM BREVE" (o botao da HUD nasce apagado para ele). Escolha persistida em PlayerPrefs; toda troca avisa MagoTrocou.
    /// </summary>
    public sealed class SelecaoPersonagem : MonoBehaviour
    {
        public const string PrefEscolhido = "arkana.mago";
        public const string T_TITULO = Textos.SelTitulo, T_SEM_KIT = Textos.SelEmBreve, T_VOLTAR = Textos.Voltar;
        /// <summary>KNOB (por foto): painel e inicio do cartao em fracao da largura util (o mago da vitrine cai em ~0,64-0,77
        /// da tela no 20:9), celula em dp (5 colunas x 4 linhas cabem no Poco F4 sem rolar), busto = fracao de cima do retrato.</summary>
        public const float PainelFracao = 0.45f, CartaoDe = 0.79f, CelulaDp = 68f, BustoFracao = 0.5f;
        public event Action VoltarPedido;
        public event Action<string> Escolhido;
        /// <summary>Toda troca do MagoEscolhido (toque no Elenco, teste): a VitrineDoMenu troca o 3D NESTE quadro.</summary>
        public static event Action MagoTrocou;

        /// <summary>Slug do mago escolhido (persistido). Padrao: o primeiro slug com kit implementado, senao o primeiro.</summary>
        public static string MagoEscolhido
        {
            get
            {
                string s = PlayerPrefs.GetString(PrefEscolhido, "");
                if (!string.IsNullOrEmpty(s) && Array.IndexOf(Kits.Slugs, s) >= 0) return s;
                return SlugPadrao();
            }
            set { PlayerPrefs.SetString(PrefEscolhido, value ?? ""); PlayerPrefs.Save(); MagoTrocou?.Invoke(); }
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

        /// <summary>O BUSTO numa celula quadrada: a fracao de cima do retrato, centrada, na proporcao da textura (conta pura).</summary>
        public static Rect Busto(int largura, int altura)
        {
            if (largura <= 0 || altura <= 0) return new Rect(0f, 0f, 1f, 1f);
            float v = BustoFracao, u = v * altura / largura;
            if (u > 1f) { u = 1f; v = (float)largura / altura; }   // retrato estreito demais: largura inteira
            return new Rect((1f - u) * 0.5f, 1f - v, u, v);
        }

        RectTransform _raiz;
        Image[] _bordas = new Image[0];
        string[] _slugs = new string[0];
        bool _seleciona;
        Text _nome, _porte, _tatica, _suprema;
        Image _gema, _faixa;

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
            var conteudo = Formas.No(_raiz, "Conteudo");
            AreaSegura.EsticarDentro(conteudo, AreaSegura.Atual(), Dp.Px(Estilo.RespiroDp));
            float pad = Dp.Px(10f), alt = Estilo.AlturaAlvo();

            // ---- PAINEL da esquerda (translucido: o pico aparece por tras), cabecalho VOLTAR + titulo, e a grade
            var painel = Formas.Arredondada(conteudo, "Painel", Formas.ComAlfa(Estilo.NoiteFunda, 0.78f), Dp.Px(14f)).rectTransform;
            painel.anchorMin = Vector2.zero; painel.anchorMax = new Vector2(PainelFracao, 1f);
            painel.offsetMin = Vector2.zero; painel.offsetMax = Vector2.zero;
            var voltar = Estilo.Botao(painel, "BtnVoltar", T_VOLTAR, 130f, Estilo.AlvoDp, 16f);
            var vrt = (RectTransform)voltar.transform;
            vrt.anchorMin = vrt.anchorMax = vrt.pivot = new Vector2(0, 1); vrt.anchoredPosition = new Vector2(pad, -pad);
            voltar.onClick.AddListener(() => VoltarPedido?.Invoke());
            var titulo = Formas.Texto(painel, "Titulo", T_TITULO, 17f, Estilo.Ouro, TextAnchor.MiddleLeft);
            var trt = titulo.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0, 1);
            trt.offsetMin = new Vector2(pad * 2f + vrt.sizeDelta.x, -pad - alt); trt.offsetMax = new Vector2(-pad, -pad);

            // RectMask2D corta por retangulo (sem stencil); a Image quase invisivel e' o alvo do arrasto no vao entre retratos
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scrollGo.transform.SetParent(painel, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(pad, pad); srt.offsetMax = new Vector2(-pad, -(pad * 2f + alt));
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            var grade = Formas.No(srt, "Grade");
            grade.anchorMin = new Vector2(0, 1); grade.anchorMax = new Vector2(1, 1); grade.pivot = new Vector2(0.5f, 1);
            grade.sizeDelta = Vector2.zero;   // largura = a do scroll (o 100 px padrao do RectTransform abria uma coluna cortada)
            var grid = grade.gameObject.AddComponent<GridLayoutGroup>();
            float cel = Dp.Px(CelulaDp);
            int folga = Mathf.CeilToInt(cel * 0.05f);   // o escolhido cresce 8%: a folga impede o RectMask2D de comer a borda
            grid.cellSize = new Vector2(cel, cel);
            grid.spacing = new Vector2(Dp.Px(8f), Dp.Px(8f));
            grid.padding = new RectOffset(folga, folga, folga, folga);
            grid.childAlignment = TextAnchor.UpperCenter;
            grade.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = grade;

            _slugs = Kits.Slugs ?? new string[0];
            _bordas = new Image[_slugs.Length];
            float filete = Dp.Px(3f);
            for (int i = 0; i < _slugs.Length; i++)
            {
                string slug = _slugs[i];
                Kits.KitDef kit = Kits.De(slug);
                // a borda e' a placa inteira, o retrato opaco a cobre por dentro: sobra o filete (Ouro no escolhido)
                var borda = Formas.Arredondada(grade, "Card" + slug, Estilo.OuroFosco, Dp.Px(6f));
                _bordas[i] = borda;
                var foto = new GameObject("Retrato", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                foto.transform.SetParent(borda.transform, false);
                foto.raycastTarget = false;
                AreaSegura.Esticar(foto.rectTransform);
                foto.rectTransform.offsetMin = new Vector2(filete, filete); foto.rectTransform.offsetMax = -foto.rectTransform.offsetMin;
                Sprite sp = Retrato(slug);
                if (sp != null) { foto.texture = sp.texture; foto.uvRect = Busto(sp.texture.width, sp.texture.height); }
                else foto.color = CorDoMago(slug);   // RawImage sem textura = branco tingido: o quadrado da cor do mago
                if (!kit.Implementado) foto.color *= new Color(0.7f, 0.7f, 0.75f, 1f);   // KNOB: sem kit sai apagado (mas escolhivel)

                // nome sobre um degrade escuro no pe' do retrato; a gema (cor + FORMA do elemento, GDD §10) no canto de cima
                var sombra = Formas.Imagem(foto.transform, "Sombra", Formas.Degrade(false), Formas.ComAlfa(Estilo.NoiteFunda, 0.9f));
                sombra.rectTransform.anchorMin = Vector2.zero; sombra.rectTransform.anchorMax = new Vector2(1f, 0.45f);
                sombra.rectTransform.offsetMin = Vector2.zero; sombra.rectTransform.offsetMax = Vector2.zero;
                var nome = Formas.Texto(foto.transform, "Nome", kit.Nome, 9.5f, Estilo.Texto);
                nome.rectTransform.anchorMin = Vector2.zero; nome.rectTransform.anchorMax = new Vector2(1f, 0.28f);
                nome.rectTransform.offsetMin = Vector2.zero; nome.rectTransform.offsetMax = Vector2.zero;
                Elemento el = IdentidadeMago.De(slug).Elemento;
                var disco = Formas.Imagem(foto.transform, "Gema", Formas.Disco(), Formas.ComAlfa(Estilo.NoiteFunda, 0.75f));
                var drt = disco.rectTransform;
                drt.anchorMin = drt.anchorMax = drt.pivot = Vector2.one;
                drt.sizeDelta = Vector2.one * Dp.Px(16f); drt.anchoredPosition = -Vector2.one * Dp.Px(2f);
                var forma = Formas.Imagem(disco.transform, "Forma", Formas.DoElemento(el), Estilo.CorElemento(el));
                AreaSegura.Esticar(forma.rectTransform);
                forma.rectTransform.offsetMin = Vector2.one * Dp.Px(2.5f); forma.rectTransform.offsetMax = -forma.rectTransform.offsetMin;

                if (_seleciona)
                {
                    var toque = Estilo.BotaoInvisivel(borda.transform, "Toque");   // a celula INTEIRA e' o alvo (68dp >= 48dp)
                    string s = slug;
                    toque.onClick.AddListener(() => Escolher(s));
                }
            }
            MontarCartao(conteudo);
            Pintar(MagoEscolhido);
        }

        /// <summary>O CARTAO do escolhido, na borda direita, ao lado do mago em 3D (a vitrine o poe no terco direito:
        /// VitrineDoMenu.OlharLado). Faixa na cor do elemento, nome, elemento · porte, filete e o kit em numeros.</summary>
        void MontarCartao(RectTransform conteudo)
        {
            var c = Formas.Arredondada(conteudo, "Cartao", Formas.ComAlfa(Estilo.NoiteFunda, 0.72f), Dp.Px(12f)).rectTransform;
            c.anchorMin = new Vector2(CartaoDe, 0.5f); c.anchorMax = new Vector2(1f, 0.5f); c.pivot = new Vector2(1f, 0.5f);
            c.sizeDelta = new Vector2(0f, Dp.Px(122f)); c.anchoredPosition = Vector2.zero;
            _faixa = Formas.Imagem(c, "Faixa", null, Color.white);
            var f = _faixa.rectTransform;
            f.anchorMin = Vector2.zero; f.anchorMax = new Vector2(0f, 1f); f.pivot = new Vector2(0f, 0.5f);
            f.sizeDelta = new Vector2(Dp.Px(3f), -Dp.Px(24f)); f.anchoredPosition = new Vector2(Dp.Px(6f), 0f);
            float x = Dp.Px(16f);
            _nome = Linha(c, "Nome", x, Dp.Px(10f), Dp.Px(30f), 22f, Estilo.Ouro);
            _gema = Formas.Imagem(c, "Gema", null, Color.white);
            var g = _gema.rectTransform;
            g.anchorMin = g.anchorMax = g.pivot = new Vector2(0f, 1f);
            g.sizeDelta = Vector2.one * Dp.Px(14f); g.anchoredPosition = new Vector2(x, -Dp.Px(46f));
            _porte = Linha(c, "Porte", x + Dp.Px(20f), Dp.Px(44f), Dp.Px(18f), 11f, Estilo.Texto);
            var sep = Formas.Imagem(c, "Filete", null, Formas.ComAlfa(Estilo.OuroFosco, 0.6f)).rectTransform;
            sep.anchorMin = new Vector2(0f, 1f); sep.anchorMax = Vector2.one; sep.pivot = new Vector2(0f, 1f);
            sep.offsetMin = new Vector2(x, -Dp.Px(68f) - Mathf.Max(1f, Dp.Px(1f))); sep.offsetMax = new Vector2(-Dp.Px(12f), -Dp.Px(68f));
            _tatica = Linha(c, "Tatica", x, Dp.Px(74f), Dp.Px(18f), 12f, Estilo.Texto);
            _suprema = Linha(c, "Suprema", x, Dp.Px(94f), Dp.Px(18f), 12f, Estilo.Texto);
        }

        /// <summary>Texto de uma linha no topo do cartao (`x` da esquerda, `y` do topo, altura `h`, em px). Best fit: nome
        /// comprido ou tela estreita encolhe a letra em vez de vazar a borda.</summary>
        static Text Linha(RectTransform pai, string nome, float x, float y, float h, float tamDp, Color cor)
        {
            var t = Formas.Texto(pai, nome, "", tamDp, cor, TextAnchor.MiddleLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true; t.resizeTextMaxSize = t.fontSize; t.resizeTextMinSize = Mathf.Max(8, Mathf.RoundToInt(t.fontSize * 0.6f));
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(x, -y - h); rt.offsetMax = new Vector2(-Dp.Px(10f), -y);
            return t;
        }

        void Escolher(string slug)
        {
            MagoEscolhido = slug;   // avisa MagoTrocou: o 3D troca neste quadro
            Pintar(slug);
            Escolheu(slug);
        }

        void Escolheu(string slug) { Escolhido?.Invoke(slug); }

        void Pintar(string escolhido)
        {
            for (int i = 0; i < _bordas.Length; i++)
            {
                bool este = _seleciona && _slugs[i] == escolhido;
                _bordas[i].color = este ? Estilo.Ouro : Formas.ComAlfa(Estilo.OuroFosco, 0.45f);
                _bordas[i].rectTransform.localScale = Vector3.one * (este ? 1.08f : 1f);   // KNOB: o escolhido salta da grade
            }
            if (_nome == null) return;
            Elemento el = IdentidadeMago.De(escolhido).Elemento;
            Color cor = Estilo.CorElemento(el);
            _nome.text = Elenco.Nome(escolhido);
            _porte.text = Elenco.Porte(escolhido);
            _gema.sprite = Formas.DoElemento(el); _gema.color = cor;
            _faixa.color = cor;
            string[] kit = Elenco.Kit(escolhido);
            _tatica.text = kit[0]; _suprema.text = kit[1];
            _tatica.color = Kits.De(escolhido).Implementado ? Estilo.Texto : Estilo.TextoFosco;
        }
    }
}
