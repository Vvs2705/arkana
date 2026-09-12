using System;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Characters;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>
    /// A ESCOLHA DE MAGO com cara de jogo, no idioma da HUD (Hud.Placa: fio de ouro fosco + miolo escuro):
    /// a ESQUERDA o painel com VOLTAR discreto, titulo, abas de elemento (TODOS/FOGO/.../VENTO) e a grade dos 20 retratos;
    /// no MEIO o escolhido em 3D sobre o disco de luz do elemento (a VitrineDoMenu desliza para o vao enquanto Aberta);
    /// a DIREITA o CARTAO (Elenco: nome, titulo, papel, barras de alcance e porte, tatica e suprema com nome, descricao e o
    /// tempo do kit) e o CONFIRMAR dourado. Tocar no retrato ESCOLHE na hora (persistido em PlayerPrefs, avisa MagoTrocou);
    /// CONFIRMAR volta ao menu com ele. Retrato de Resources/Retratos/NN (corpo inteiro, 512 px): a celula mostra o BUSTO
    /// (RawImage.uvRect, sem mascara); sem arquivo, o quadrado na cor do mago. Mago sem kit sai apagado e o tempo diz EM BREVE.
    /// Sem fundo proprio: o do Menu (translucido) ja' bloqueia o toque.
    /// </summary>
    public sealed class SelecaoPersonagem : MonoBehaviour
    {
        public const string PrefEscolhido = "arkana.mago";
        public const string T_TITULO = Textos.SelTitulo, T_VOLTAR = Textos.Voltar, T_CONFIRMAR = Textos.SelConfirmar;
        /// <summary>KNOB (por foto): painel e inicio do cartao em fracao da largura util — o mago da vitrine fica no vao
        /// entre os dois (VitrineDoMenu.OlharLadoElenco). Celula, vao e aba em dp: cabecalho + abas + 4 linhas de 5 enchem os
        /// ~413 dp de altura do Poco F4 (a foto 21 sobrava um vazio embaixo da grade). Busto = fracao de cima do retrato.</summary>
        public const float PainelFracao = 0.42f, CartaoDe = 0.72f, CelulaDp = 66f, VaoDp = 6f, AbaDp = 44f, BustoFracao = 0.5f;
        public event Action VoltarPedido;
        public event Action<string> Escolhido;
        /// <summary>Toda troca do MagoEscolhido (toque no Elenco, teste): a VitrineDoMenu troca o 3D NESTE quadro.</summary>
        public static event Action MagoTrocou;
        /// <summary>O Elenco esta' na tela: a VitrineDoMenu desliza o mago para o vao entre o painel e o cartao.</summary>
        public static bool Aberta { get; private set; }

        /// <summary>Miolo e fio das placas da HUD (Hud.CorMiolo/CorFio sao privados: os mesmos valores).</summary>
        static readonly Color Miolo = new Color(0.03f, 0.04f, 0.07f, 0.8f);
        static Color Fio => Formas.ComAlfa(Estilo.OuroFosco, 0.95f);
        static readonly Vector2 Meio = new Vector2(0.5f, 0.5f), SupEsq = new Vector2(0f, 1f);

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
        Text[] _nomes = new Text[0];
        string[] _slugs = new string[0];
        bool _seleciona;
        int _filtro = Elenco.FiltroTodos;
        Image[] _abaFio = new Image[0], _abaMiolo = new Image[0];
        Text[] _abaTexto = new Text[0];
        ScrollRect _scroll;
        Text _nome, _titulo, _papel, _elemento, _alcance, _porte;
        Image _gema, _pilula, _alcanceFill, _porteFill;
        LinhaKit _tatica, _suprema;

        void OnEnable() { Aberta = true; }

        void OnDisable() { Aberta = false; }

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
            float pad = Dp.Px(8f), alt = Estilo.AlturaAlvo(), vao = Dp.Px(VaoDp), aba = Dp.Px(AbaDp);

            // ---- PAINEL da esquerda (placa da HUD, translucida: o pico aparece por tras): VOLTAR + titulo, abas, grade
            var painel = Hud.Placa(conteudo, "Painel", Dp.Px(14f)).rectTransform;
            painel.anchorMin = Vector2.zero; painel.anchorMax = new Vector2(PainelFracao, 1f);
            painel.offsetMin = Vector2.zero; painel.offsetMax = Vector2.zero;
            var voltar = BotaoPlaca(painel, "BtnVoltar", T_VOLTAR, 13f, Estilo.Texto, false);   // discreto: fio fosco, miolo escuro
            var vrt = (RectTransform)voltar.transform;
            Fixar(vrt, SupEsq, SupEsq, new Vector2(pad, -pad), new Vector2(Dp.Px(96f), alt));
            voltar.onClick.AddListener(() => VoltarPedido?.Invoke());
            var titulo = Formas.Texto(painel, "Titulo", T_TITULO, 17f, Estilo.Ouro, TextAnchor.MiddleLeft);
            titulo.fontStyle = FontStyle.Bold;
            var trt = titulo.rectTransform;
            trt.anchorMin = SupEsq; trt.anchorMax = Vector2.one; trt.pivot = SupEsq;
            trt.offsetMin = new Vector2(pad + vrt.sizeDelta.x + Dp.Px(12f), -pad - alt); trt.offsetMax = new Vector2(-pad, -pad);
            float yAbas = pad + alt + vao;
            MontarAbas(painel, pad, yAbas, aba);

            // RectMask2D corta por retangulo (sem stencil); a Image quase invisivel e' o alvo do arrasto no vao entre retratos
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scrollGo.transform.SetParent(painel, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(pad, pad); srt.offsetMax = new Vector2(-pad, -(yAbas + aba + vao));
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            _scroll = scrollGo.GetComponent<ScrollRect>();
            _scroll.horizontal = false;
            var grade = Formas.No(srt, "Grade");
            grade.anchorMin = SupEsq; grade.anchorMax = Vector2.one; grade.pivot = new Vector2(0.5f, 1);
            grade.sizeDelta = Vector2.zero;   // largura = a do scroll (o 100 px padrao do RectTransform abria uma coluna cortada)
            var grid = grade.gameObject.AddComponent<GridLayoutGroup>();
            float cel = Dp.Px(CelulaDp);
            int folga = Mathf.CeilToInt(cel * 0.05f);   // o escolhido cresce 8%: a folga impede o RectMask2D de comer a borda
            grid.cellSize = new Vector2(cel, cel);
            grid.spacing = new Vector2(vao, vao);
            grid.padding = new RectOffset(folga, folga, folga, folga);
            grid.childAlignment = TextAnchor.UpperCenter;
            grade.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = grade;

            _slugs = Kits.Slugs ?? new string[0];
            _bordas = new Image[_slugs.Length];
            _nomes = new Text[_slugs.Length];
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
                _nomes[i] = nome;
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
                    var toque = Estilo.BotaoInvisivel(borda.transform, "Toque");   // a celula INTEIRA e' o alvo (66dp >= 48dp)
                    string s = slug;
                    toque.onClick.AddListener(() => Escolher(s));
                }
            }
            MontarCartao(conteudo);
            PintarAbas();
            Pintar(MagoEscolhido);
        }

        /// <summary>As abas TODOS/FOGO/.../VENTO em fracoes iguais da largura: placa da HUD, gema + nome do elemento. A acesa
        /// ganha fio de ouro e miolo aquecido (PintarAbas); o toque filtra a grade (Elenco.NoFiltro).</summary>
        void MontarAbas(RectTransform painel, float pad, float y, float h)
        {
            int n = Elementos.Todos.Length + 1;
            _abaFio = new Image[n]; _abaMiolo = new Image[n]; _abaTexto = new Text[n];
            float meioVao = Dp.Px(2.5f);
            var linha = Formas.No(painel, "Abas");
            linha.anchorMin = SupEsq; linha.anchorMax = Vector2.one; linha.pivot = new Vector2(0.5f, 1f);
            linha.offsetMin = new Vector2(pad, -y - h); linha.offsetMax = new Vector2(-pad, -y);
            for (int i = 0; i < n; i++)
            {
                bool todos = i == 0;
                Elemento el = todos ? Elemento.Fogo : Elementos.Todos[i - 1];
                int filtro = todos ? Elenco.FiltroTodos : (int)el;
                var fio = Hud.Placa(linha, todos ? "AbaTodos" : "Aba" + el, Dp.Px(8f));
                fio.raycastTarget = true;   // a aba INTEIRA pega o dedo
                var rt = fio.rectTransform;
                rt.anchorMin = new Vector2((float)i / n, 0f); rt.anchorMax = new Vector2((i + 1f) / n, 1f);
                rt.offsetMin = new Vector2(todos ? 0f : meioVao, 0f); rt.offsetMax = new Vector2(i == n - 1 ? 0f : -meioVao, 0f);
                // Transition None ANTES de qualquer cor: o ColorTint gravaria o tom no CanvasRenderer e escureceria o miolo
                var b = fio.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Filtrar(filtro));
                var t = Formas.Texto(fio.transform, "Rotulo", todos ? Textos.SelTodos : Elenco.NomeDo(el), 10.5f, Estilo.TextoFosco);
                t.fontStyle = FontStyle.Bold;
                Caber(t, 0.7f);
                AreaSegura.Esticar(t.rectTransform);
                if (!todos)
                {
                    t.rectTransform.offsetMin = new Vector2(Dp.Px(17f), 0f);   // a gema a' esquerda do nome
                    var g = Formas.Imagem(fio.transform, "Gema", Formas.DoElemento(el), Estilo.CorElemento(el));
                    Fixar(g.rectTransform, new Vector2(0f, 0.5f), Meio, new Vector2(Dp.Px(12f), 0f), Vector2.one * Dp.Px(12f));
                }
                _abaFio[i] = fio;
                _abaMiolo[i] = fio.transform.GetChild(0).GetComponent<Image>();   // a Placa nasce com o Miolo de primeiro filho
                _abaTexto[i] = t;
            }
        }

        /// <summary>
        /// O CARTAO do escolhido, na borda direita, ao lado do mago em 3D. Em cima: pilula do elemento, NOME grande, titulo,
        /// papel e as barras (alcance da ficha, porte pela altura). Embaixo, de baixo para cima: CONFIRMAR dourado, suprema,
        /// tatica e o ornamento das placas grandes. O vao do meio absorve a altura de cada tela.
        /// </summary>
        void MontarCartao(RectTransform conteudo)
        {
            var c = Hud.Placa(conteudo, "Cartao", Dp.Px(14f)).rectTransform;
            c.anchorMin = new Vector2(CartaoDe, 0f); c.anchorMax = Vector2.one;
            c.offsetMin = Vector2.zero; c.offsetMax = Vector2.zero;
            float x = Dp.Px(14f);
            _pilula = Formas.Arredondada(c, "Pilula", Color.white, Dp.Px(9f));
            Fixar(_pilula.rectTransform, SupEsq, SupEsq, new Vector2(x, -Dp.Px(12f)), new Vector2(Dp.Px(80f), Dp.Px(18f)));
            _gema = Formas.Imagem(_pilula.transform, "Gema", null, Color.white);
            Fixar(_gema.rectTransform, new Vector2(0f, 0.5f), Meio, new Vector2(Dp.Px(11f), 0f), Vector2.one * Dp.Px(11f));
            _elemento = Formas.Texto(_pilula.transform, "Elemento", "", 10.5f, Color.white, TextAnchor.MiddleLeft);
            _elemento.fontStyle = FontStyle.Bold;
            AreaSegura.Esticar(_elemento.rectTransform);
            _elemento.rectTransform.offsetMin = new Vector2(Dp.Px(21f), 0f);
            _nome = Rotulo(c, "Nome", x, Dp.Px(34f), Dp.Px(36f), 28f, Estilo.Ouro);
            _nome.fontStyle = FontStyle.Bold;
            _nome.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(1.2f), -Dp.Px(1.2f));   // 1px some a 395 ppi
            _titulo = Rotulo(c, "TituloMago", x, Dp.Px(70f), Dp.Px(20f), 14f, Estilo.Texto);
            _titulo.fontStyle = FontStyle.Italic;
            _papel = Rotulo(c, "Papel", x, Dp.Px(90f), Dp.Px(16f), 11f, Estilo.TextoFosco);
            _alcanceFill = Barra(c, "Alcance", Textos.SelAlcance, Dp.Px(116f), out _alcance);
            _porteFill = Barra(c, "Porte", Textos.SelPorte, Dp.Px(138f), out _porte);

            float baixo = Dp.Px(14f);
            if (_seleciona)
            {
                var ok = BotaoPlaca(c, "BtnConfirmar", T_CONFIRMAR, 17f, Estilo.Ouro, true);
                var ort = (RectTransform)ok.transform;
                float h = Estilo.AlturaAlvo(50f);
                ort.anchorMin = Vector2.zero; ort.anchorMax = new Vector2(1f, 0f); ort.pivot = new Vector2(0.5f, 0f);
                ort.offsetMin = new Vector2(x, baixo); ort.offsetMax = new Vector2(-x, baixo + h);
                ok.onClick.AddListener(() => VoltarPedido?.Invoke());   // a escolha ja' foi gravada no toque do retrato
                baixo += h + Dp.Px(12f);
            }
            float bloco = Dp.Px(64f);
            _suprema = new LinhaKit(c, "Suprema", Textos.HudSuprema, baixo, bloco);
            _tatica = new LinhaKit(c, "Tatica", Textos.HudTatica, baixo + bloco + Dp.Px(8f), bloco);
            Ornamento(c, baixo + 2f * bloco + Dp.Px(16f), x);
        }

        /// <summary>Uma habilidade no cartao: icone (disco com aro na cor e a forma), rotulo TATICA/SUPREMA, o tempo do kit
        /// a' direita, o nome em negrito e a descricao da ficha em 2 linhas. Ancorada pelo pe' do cartao.</summary>
        sealed class LinhaKit
        {
            readonly Image _aro, _forma;
            readonly Text _rotulo, _tempo, _nome, _desc;

            public LinhaKit(RectTransform cartao, string nome, string rotulo, float yBaixo, float h)
            {
                var b = Formas.No(cartao, nome);
                b.anchorMin = Vector2.zero; b.anchorMax = new Vector2(1f, 0f); b.pivot = new Vector2(0.5f, 0f);
                b.offsetMin = new Vector2(0f, yBaixo); b.offsetMax = new Vector2(0f, yBaixo + h);
                float x = Dp.Px(14f), icone = Dp.Px(30f), tx = x + icone + Dp.Px(8f);
                var disco = Formas.Imagem(b, "Icone", Formas.DiscoDegrade(), new Color(0.13f, 0.14f, 0.22f, 1f));
                Fixar(disco.rectTransform, SupEsq, SupEsq, new Vector2(x, -Dp.Px(2f)), Vector2.one * icone);
                _aro = Formas.Imagem(disco.transform, "Aro", Formas.Anel(0.8f), Color.white);
                AreaSegura.Esticar(_aro.rectTransform);
                _forma = Formas.Imagem(disco.transform, "Forma", null, Color.white);
                Fixar(_forma.rectTransform, Meio, Meio, Vector2.zero, Vector2.one * Dp.Px(14f));
                _rotulo = Rotulo(b, "Rotulo", tx, 0f, Dp.Px(13f), 9.5f, Color.white);
                _rotulo.text = rotulo;
                _rotulo.fontStyle = FontStyle.Bold;
                _tempo = Rotulo(b, "Tempo", tx, 0f, Dp.Px(13f), 10f, Estilo.TextoFosco);
                _tempo.alignment = TextAnchor.MiddleRight;
                _nome = Rotulo(b, "Nome", tx, Dp.Px(13f), Dp.Px(19f), 14f, Estilo.Texto);
                _nome.fontStyle = FontStyle.Bold;
                _desc = Rotulo(b, "Desc", tx, Dp.Px(33f), Dp.Px(30f), 10.5f, Formas.ComAlfa(Estilo.Texto, 0.8f));
                _desc.alignment = TextAnchor.UpperLeft;
                Caber(_desc, 0.75f);
            }

            /// <summary>`h` = Elenco.Habilidade: { nome, descricao, tempo }.</summary>
            public void Pintar(string[] h, Color cor, Sprite forma)
            {
                _aro.color = cor; _forma.sprite = forma; _forma.color = cor; _rotulo.color = cor;
                _nome.text = h[0]; _desc.text = h[1]; _tempo.text = h[2];
            }
        }

        /// <summary>Barra de atributo: rotulo fosco, trilho escuro, preenchimento em degrade (a cor vem do Encher) e o valor
        /// a' direita. Devolve o preenchimento: o Encher mexe na ANCORA (largura = fracao), o 9-slice nao entorta.</summary>
        static Image Barra(RectTransform c, string nome, string rotulo, float y, out Text valor)
        {
            float x = Dp.Px(14f), h = Dp.Px(16f), trilhoH = Dp.Px(6f);
            var r = Rotulo(c, nome + "Rotulo", x, y, h, 9.5f, Estilo.TextoFosco);
            r.text = rotulo;
            r.fontStyle = FontStyle.Bold;
            valor = Rotulo(c, nome + "Valor", x, y, h, 10.5f, Estilo.Texto);
            valor.alignment = TextAnchor.MiddleRight;
            var trilho = Formas.Arredondada(c, nome, new Color(0f, 0f, 0f, 0.5f), trilhoH * 0.5f);
            var t = trilho.rectTransform;
            float topo = y + (h - trilhoH) * 0.5f;
            t.anchorMin = SupEsq; t.anchorMax = Vector2.one; t.pivot = SupEsq;
            t.offsetMin = new Vector2(x + Dp.Px(58f), -topo - trilhoH); t.offsetMax = new Vector2(-x - Dp.Px(66f), -topo);
            var fill = Formas.Arredondada(trilho.transform, "Fill", Color.white, trilhoH * 0.5f, true);
            AreaSegura.Esticar(fill.rectTransform);
            return fill;
        }

        static void Encher(Image fill, float v, Color cor)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp(v, 0.08f, 1f), 1f);   // piso: o menor (Pip) ainda le' barra
            fill.color = cor;
        }

        /// <summary>O ornamento das placas grandes da HUD (fio fosco com losango no meio), a `yBaixo` do pe' do cartao.</summary>
        static void Ornamento(RectTransform c, float yBaixo, float x)
        {
            var fio = Formas.Imagem(c, "Ornamento", null, Formas.ComAlfa(Estilo.OuroFosco, 0.6f));
            var rt = fio.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f); rt.pivot = Meio;
            rt.offsetMin = new Vector2(x, yBaixo); rt.offsetMax = new Vector2(-x, yBaixo + Mathf.Max(Dp.Px(1f), 1f));
            var l = Formas.Imagem(fio.transform, "Losango", Formas.Losango(), Estilo.Ouro);
            Fixar(l.rectTransform, Meio, Meio, Vector2.zero, Vector2.one * Dp.Px(9f));
        }

        /// <summary>
        /// O botao das placas da HUD (fim e pausa): fio + miolo arredondados, rotulo em negrito, o no' INTEIRO pega o dedo.
        /// `cheio` = acao principal: miolo em degrade na `cor`, rotulo escuro; senao miolo escuro e rotulo na `cor`.
        /// ponytail: copia do Hud.BotaoPlaca (privado); tornar o de la' internal e apagar esta.
        /// </summary>
        static Button BotaoPlaca(Transform pai, string nome, string texto, float fonteDp, Color cor, bool cheio)
        {
            var fio = Hud.Placa(pai, nome, Dp.Px(9f));
            fio.raycastTarget = true;
            if (cheio) fio.color = cor;
            var miolo = fio.transform.GetChild(0).GetComponent<Image>();   // a Placa nasce com o Miolo de primeiro filho
            if (cheio) miolo.sprite = Formas.Arredondado(true);   // mesma borda de 9-slice: so' ganha o volume
            miolo.color = Color.white;
            var b = fio.gameObject.AddComponent<Button>();
            b.targetGraphic = miolo;
            Color normal = cheio ? cor : Miolo;
            var c = b.colors;
            c.normalColor = normal; c.highlightedColor = normal; c.selectedColor = normal;   // toque nao tem hover
            c.pressedColor = cheio ? Formas.Escurecer(cor, 0.3f) : Color.Lerp(Miolo, cor, 0.3f);
            c.fadeDuration = 0f;
            b.colors = c;
            var t = Formas.Texto(fio.transform, "Rotulo", texto, fonteDp, cheio ? Estilo.NoiteFunda : cor);
            t.fontStyle = FontStyle.Bold;
            AreaSegura.Esticar(t.rectTransform);
            if (cheio) t.GetComponent<Shadow>().enabled = false;   // sombra preta sob letra escura so' borra
            return b;
        }

        /// <summary>Texto que pode quebrar/encolher: nome comprido ou tela estreita encolhe a letra em vez de vazar a borda.</summary>
        static void Caber(Text t, float piso)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true; t.resizeTextMaxSize = t.fontSize;
            t.resizeTextMinSize = Mathf.Max(8, Mathf.RoundToInt(t.fontSize * piso));
        }

        /// <summary>Texto de uma linha no topo do pai (`x` da esquerda, `y` do topo, altura `h`, em px; a direita recua 14dp).</summary>
        static Text Rotulo(RectTransform pai, string nome, float x, float y, float h, float tamDp, Color cor)
        {
            var t = Formas.Texto(pai, nome, "", tamDp, cor, TextAnchor.MiddleLeft);
            Caber(t, 0.6f);
            var rt = t.rectTransform;
            rt.anchorMin = SupEsq; rt.anchorMax = Vector2.one; rt.pivot = SupEsq;
            rt.offsetMin = new Vector2(x, -y - h); rt.offsetMax = new Vector2(-Dp.Px(14f), -y);
            return t;
        }

        /// <summary>Ancora num PONTO do pai (0..1) com pivo, posicao e tamanho em px.</summary>
        static void Fixar(RectTransform rt, Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 tam)
        {
            rt.anchorMin = ancora; rt.anchorMax = ancora; rt.pivot = pivo;
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
        }

        void Escolher(string slug)
        {
            MagoEscolhido = slug;   // avisa MagoTrocou: o 3D troca neste quadro
            Pintar(slug);
            Escolheu(slug);
        }

        void Escolheu(string slug) { Escolhido?.Invoke(slug); }

        /// <summary>A aba filtra a grade: o GridLayoutGroup so' arruma os ativos, entao a grade fecha sem buraco.</summary>
        void Filtrar(int filtro)
        {
            _filtro = filtro;
            for (int i = 0; i < _slugs.Length; i++) _bordas[i].gameObject.SetActive(Elenco.NoFiltro(_slugs[i], filtro));
            PintarAbas();
            if (_scroll != null) { _scroll.StopMovement(); _scroll.content.anchoredPosition = Vector2.zero; }   // aba nova, do topo
        }

        void PintarAbas()
        {
            for (int i = 0; i < _abaFio.Length; i++)
            {
                bool acesa = (i == 0 ? Elenco.FiltroTodos : (int)Elementos.Todos[i - 1]) == _filtro;
                _abaFio[i].color = acesa ? Estilo.Ouro : Fio;
                _abaMiolo[i].color = acesa ? Color.Lerp(Miolo, Estilo.Ouro, 0.22f) : Miolo;
                _abaTexto[i].color = acesa ? Estilo.Ouro : Estilo.TextoFosco;
            }
        }

        void Pintar(string escolhido)
        {
            for (int i = 0; i < _bordas.Length; i++)
            {
                bool este = _seleciona && _slugs[i] == escolhido;
                _bordas[i].color = este ? Estilo.Ouro : Formas.ComAlfa(Estilo.OuroFosco, 0.45f);
                _bordas[i].rectTransform.localScale = Vector3.one * (este ? 1.08f : 1f);   // KNOB: o escolhido salta da grade
                _nomes[i].color = este ? Estilo.Ouro : Estilo.Texto;
            }
            if (_nome == null) return;
            Elemento el = IdentidadeMago.De(escolhido).Elemento;
            Color cor = Estilo.CorElemento(el);
            _nome.text = Elenco.Nome(escolhido).ToUpperInvariant();
            _titulo.text = Elenco.Titulo(escolhido);
            _papel.text = Elenco.Papel(escolhido);
            _pilula.color = Formas.ComAlfa(cor, 0.2f);
            _gema.sprite = Formas.DoElemento(el); _gema.color = cor;
            _elemento.text = Elenco.NomeDo(el); _elemento.color = cor;
            Encher(_alcanceFill, Elenco.Alcance(escolhido), cor);
            _alcance.text = Elenco.AlcanceRotulo(escolhido);
            Encher(_porteFill, Elenco.Porte(escolhido), cor);
            _porte.text = Elenco.Altura(escolhido);
            _tatica.Pintar(Elenco.Habilidade(escolhido, false), cor, Formas.DoElemento(el));
            _suprema.Pintar(Elenco.Habilidade(escolhido, true), Estilo.Ouro, Formas.Losango());
        }
    }
}
