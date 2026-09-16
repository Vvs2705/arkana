using UnityEngine;
using UnityEngine.UI;
using Arkana.Characters;
using Arkana.Core;
using Arkana.Menu;
using ArkMenu = Arkana.Menu.Menu;

namespace Arkana.UI
{
    /// <summary>
    /// A LOGICA PURA da tela de carregamento: o progresso so' anda para a frente (a montagem avisa), a BARRA persegue o
    /// progresso sem nunca passar dele, a DICA troca a cada DicaS esvaecendo na virada, e a tela inteira esvaece em SomeS
    /// quando a montagem acaba. Relogio sem escala (a montagem roda com timeScale 0). Nada aqui monta partida.
    /// </summary>
    public sealed class CarregamentoLogica
    {
        /// <summary>KNOB por foto/aparelho: segundos de cada dica, a virada (esvaece e volta), a taxa com que a barra
        /// persegue o progresso (1/s: 8 = 95% de um salto em ~0,37 s) e o esvaecer final.</summary>
        public const float DicaS = 4.5f, ViradaS = 0.3f, Persegue = 8f, SomeS = 0.5f;

        public float Progresso { get; private set; }
        /// <summary>O que a barra MOSTRA: persegue o Progresso, nunca passa dele, nunca volta.</summary>
        public float Barra { get; private set; }
        public bool Pronto { get; private set; }
        public float Tempo { get; private set; }

        readonly int _dicas, _inicio;
        float _desdePronto;

        /// <summary>`inicio` = a primeira dica (o Main tira do seed: cada carregamento abre com outra, a foto com a mesma).</summary>
        public CarregamentoLogica(int dicas, int inicio)
        {
            _dicas = Mathf.Max(dicas, 1);
            _inicio = ((inicio % _dicas) + _dicas) % _dicas;
        }

        /// <summary>Progresso 0..1 da montagem. Voltar (ou NaN) nao conta; depois do Pronto, nada muda.</summary>
        public void Avancar(float p)
        {
            if (!Pronto && p > Progresso) Progresso = Mathf.Min(p, 1f);
        }

        /// <summary>A montagem acabou: 100% na hora e o esvaecer comeca.</summary>
        public void Terminar()
        {
            Progresso = 1f;
            Barra = 1f;
            Pronto = true;
        }

        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            Tempo += dt;
            Barra += (Progresso - Barra) * (1f - Mathf.Exp(-Persegue * dt));   // exponencial: fator < 1, nunca passa
            if (Pronto) _desdePronto += dt;
        }

        public int Porcento => Mathf.Clamp(Mathf.FloorToInt(Barra * 100f + 0.001f), 0, 100);

        public int Dica => (_inicio + (int)(Tempo / DicaS)) % _dicas;

        /// <summary>1 lendo; na virada esvaece ate' 0 e a proxima volta. A primeira ja' nasce inteira (a tela abre lendo).</summary>
        public float DicaAlfa
        {
            get
            {
                float f = Tempo % DicaS;
                float entra = Tempo < DicaS ? 1f : f / ViradaS;
                return Mathf.Clamp01(Mathf.Min(entra, (DicaS - f) / ViradaS));
            }
        }

        /// <summary>Alfa da tela inteira: 1 carregando; esvaece em SomeS depois do Pronto.</summary>
        public float Alfa => Pronto ? 1f - Mathf.Clamp01(_desdePronto / SomeS) : 1f;

        public bool Sumiu => Pronto && _desdePronto >= SomeS;
    }

    /// <summary>
    /// A TELA DE CARREGAMENTO (GDD §11: key art, logo, barra fina dourada + %, dica rotativa no rodape), no idioma da HUD.
    /// Fundo = o ULTIMO QUADRO DA VITRINE (o mago escolhido em guarda no pico, o por do sol atras), que o Main fotografa no
    /// toque do JOGAR, aproximando devagar; a esquerda o que se prepara, o NOME, o titulo, o elemento e o papel dele; embaixo
    /// a placa da DICA, o passo da montagem, o % e a barra com brilho na ponta e um reflexo correndo no dourado. O selo gira
    /// no canto: a tela esta' viva mesmo quando um passo pesa. Canvas proprio por cima de tudo (a HUD nasce atras, apagada) e
    /// bloqueia o toque. O Main so' diz quanto andou (Avancar) e quando acabou (Terminar): ela esvaece e se destroi sozinha.
    /// </summary>
    public sealed class TelaDeCarregamento : MonoBehaviour
    {
        /// <summary>Acima do Menu (0) e da HUD (10).</summary>
        public const int Ordem = 50;
        /// <summary>KNOB por foto: quanto a arte aproxima (fracao) e em quantos segundos, o veu que a escurece, o giro do selo
        /// (graus/s) e a volta do reflexo que corre na barra (s).</summary>
        public const float ArteZoom = 0.06f, ArteZoomS = 10f, ArteVeu = 0.2f, SeloGiro = 24f, ReflexoS = 1.6f;

        public CarregamentoLogica Logica { get; private set; }

        static readonly Vector2 Meio = new Vector2(0.5f, 0.5f), SupEsq = new Vector2(0f, 1f);
        static Sprite _seloSprite;

        CanvasGroup _grupo;
        RawImage _arte;
        RectTransform _selo, _fill, _brilho, _reflexo;
        Image _brilhoImg;
        Text _passo, _pct, _dica;
        int _pctVisto = -1, _dicaVista = -1;
        string[] _dicas;

        /// <summary>`arte` null (sem GPU) = noite com o selo grande e fosco. `dica` = a primeira dica (qualquer inteiro).</summary>
        public static TelaDeCarregamento Criar(Texture arte, string slug, bool treino, int dica)
        {
            var canvas = Formas.CanvasTelaCheia("Carregamento", Ordem);
            var t = canvas.gameObject.AddComponent<TelaDeCarregamento>();
            t.Montar((RectTransform)canvas.transform, arte, slug, treino, dica);
            return t;
        }

        /// <summary>Quanto a montagem andou (0..1) e o passo que ela esta' dando (null = o mesmo).</summary>
        public void Avancar(float progresso, string passo)
        {
            Logica.Avancar(progresso);
            if (passo != null && _passo.text != passo) _passo.text = passo;
        }

        /// <summary>A partida esta' montada: 100% e o esvaecer. O toque passa para a partida ja' no primeiro quadro.</summary>
        public void Terminar()
        {
            Logica.Terminar();
            _passo.text = Textos.CarregaPronto;
            _grupo.blocksRaycasts = false;
            Pintar();
        }

        void Montar(RectTransform raiz, Texture arte, string slug, bool treino, int dica)
        {
            _dicas = Textos.Dicas;
            Logica = new CarregamentoLogica(_dicas.Length, dica);
            _grupo = gameObject.AddComponent<CanvasGroup>();

            // FUNDO: a noite (bloqueia o toque: nada vaza para a HUD que monta atras) e a ARTE esticada na tela — o Main
            // fotografa no tamanho da tela, entao nao entorta
            Estilo.Fundo(raiz).color = Estilo.NoiteFunda;
            _arte = new GameObject("Arte", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _arte.transform.SetParent(raiz, false);
            _arte.raycastTarget = false;
            AreaSegura.Esticar(_arte.rectTransform);
            if (arte != null)
            {
                _arte.texture = arte;
                float v = 1f - ArteVeu;
                _arte.color = new Color(v, v, v, 1f);
            }
            else
            {
                _arte.enabled = false;   // sem a foto (portao sem GPU): o selo grande e fosco onde estaria o mago
                var fosco = Formas.Imagem(raiz, "SeloFundo", SpriteDoSelo(), Formas.ComAlfa(Color.white, 0.12f));
                Fixar(fosco.rectTransform, new Vector2(0.72f, 0.56f), Meio, Vector2.zero, Vector2.one * Dp.Px(300f));
            }

            // LEITURA sobre a neve e o ceu do pico (o mesmo remedio do titulo do menu): halo escuro macio atras do nome e o
            // degrade do rodape atras da dica e da barra. KNOB: alfas e tamanhos, por foto.
            var rodape = Formas.Imagem(raiz, "SombraBaixo", Formas.Degrade(false), Formas.ComAlfa(Estilo.NoiteFunda, 0.85f));
            rodape.rectTransform.anchorMin = Vector2.zero; rodape.rectTransform.anchorMax = new Vector2(1f, 0.42f);
            rodape.rectTransform.offsetMin = Vector2.zero; rodape.rectTransform.offsetMax = Vector2.zero;
            var halo = Formas.Imagem(raiz, "SombraNome", Formas.Sombra(), Formas.ComAlfa(Estilo.NoiteFunda, 0.6f));
            Fixar(halo.rectTransform, new Vector2(0.18f, 0.6f), Meio, Vector2.zero, new Vector2(Dp.Px(560f), Dp.Px(300f)));

            var c = Formas.No(raiz, "Conteudo");
            AreaSegura.EsticarDentro(c, AreaSegura.Atual(), Dp.Px(Estilo.RespiroDp + 8f));
            MontarTopo(c);
            MontarMago(c, slug, treino);
            MontarRodape(c);
            Pintar();
        }

        /// <summary>O selo girando devagar (a tela esta' viva) e a marca ARKANA do titulo.</summary>
        void MontarTopo(RectTransform c)
        {
            _selo = Formas.Imagem(c, "Selo", SpriteDoSelo(), Color.white).rectTransform;
            Fixar(_selo, SupEsq, Meio, new Vector2(Dp.Px(20f), -Dp.Px(20f)), Vector2.one * Dp.Px(40f));
            var marca = ArkMenu.Wordmark(c, 140f, false).rectTransform;   // parada: quem corre aqui e' o reflexo da barra
            Fixar(marca, SupEsq, new Vector2(0f, 0.5f), new Vector2(Dp.Px(50f), -Dp.Px(20f)), marca.sizeDelta);   // na linha do centro do selo
        }

        /// <summary>O ESCOLHIDO, a esquerda (o mago da foto fica no terco direito): o que se prepara, NOME, titulo, o fio das
        /// placas grandes e a gema do elemento (cor + FORMA, GDD §10) com o nome dele e o papel.</summary>
        void MontarMago(RectTransform c, string slug, bool treino)
        {
            var b = Formas.No(c, "Mago");
            Fixar(b, new Vector2(0f, 0.6f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(Dp.Px(460f), Dp.Px(150f)));
            var modo = Linha(b, "Modo", treino ? Textos.CarregaTreino : Textos.CarregaPartida, 0f, 18f, 12f, Formas.ComAlfa(Estilo.Ouro, 0.9f));
            modo.fontStyle = FontStyle.Bold;
            var nome = Linha(b, "Nome", Elenco.Nome(slug).ToUpperInvariant(), 18f, 58f, 46f, Estilo.Ouro);
            nome.fontStyle = FontStyle.Bold;
            nome.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(2f), -Dp.Px(2f));   // 1px some num nome de 46dp
            var titulo = Linha(b, "Titulo", Elenco.Titulo(slug), 78f, 24f, 18f, Estilo.Texto);
            titulo.fontStyle = FontStyle.Italic;
            var fio = Formas.Imagem(b, "Fio", null, Formas.ComAlfa(Estilo.OuroFosco, 0.7f));
            Fixar(fio.rectTransform, SupEsq, SupEsq, new Vector2(0f, -Dp.Px(110f)), new Vector2(Dp.Px(260f), Mathf.Max(Dp.Px(1f), 1f)));
            var losango = Formas.Imagem(fio.transform, "Losango", Formas.Losango(), Estilo.Ouro);
            Fixar(losango.rectTransform, new Vector2(0f, 0.5f), Meio, Vector2.zero, Vector2.one * Dp.Px(9f));
            Elemento el = IdentidadeMago.De(slug).Elemento;
            Color cor = Estilo.CorElemento(el);
            var gema = Formas.Imagem(b, "Gema", Formas.DoElemento(el), cor);
            Fixar(gema.rectTransform, SupEsq, new Vector2(0f, 0.5f), new Vector2(0f, -Dp.Px(128f)), Vector2.one * Dp.Px(14f));
            string papel = Elenco.Papel(slug);
            var linha = Linha(b, "Papel", "<b><color=#" + ColorUtility.ToHtmlStringRGB(cor) + ">" + Elenco.NomeDo(el) + "</color></b>"
                + (string.IsNullOrEmpty(papel) ? "" : Textos.HudSep + papel), 119f, 18f, 13f, Estilo.Texto);
            linha.rectTransform.offsetMin += new Vector2(Dp.Px(20f), 0f);   // a gema a' esquerda
        }

        /// <summary>A placa da DICA (idioma da HUD: fio de ouro fosco + miolo escuro), o passo, o % e a barra: a placa das
        /// barras da HUD com o preenchimento dourado em degrade, o brilho pulsando na ponta e o reflexo correndo por dentro.</summary>
        void MontarRodape(RectTransform c)
        {
            float h = Dp.Px(7f), baixo = Dp.Px(4f);
            float fio = Mathf.Max(Dp.Px(1f), 1f), folga = Mathf.Max(Dp.Px(1.5f), 1f);
            var placa = Hud.Placa(c, "Barra", h * 0.5f).rectTransform;
            placa.anchorMin = Vector2.zero; placa.anchorMax = new Vector2(1f, 0f); placa.pivot = new Vector2(0.5f, 0f);
            placa.offsetMin = new Vector2(0f, baixo); placa.offsetMax = new Vector2(0f, baixo + h);
            var trilho = Formas.No(placa, "Trilho");
            AreaSegura.Esticar(trilho);
            trilho.offsetMin = new Vector2(fio + folga, fio + folga); trilho.offsetMax = -trilho.offsetMin;
            float raio = Mathf.Max(h * 0.5f - fio - folga, 1f);
            var fill = Formas.Arredondada(trilho, "Fill", Estilo.Ouro, raio, true);
            fill.gameObject.AddComponent<RectMask2D>();   // o reflexo corre DENTRO do dourado (corte por retangulo, sem stencil)
            _fill = fill.rectTransform;
            _fill.anchorMin = Vector2.zero; _fill.anchorMax = new Vector2(0f, 1f);
            _fill.offsetMin = Vector2.zero; _fill.offsetMax = Vector2.zero;
            _reflexo = Formas.Imagem(fill.transform, "Reflexo", Formas.Sombra(), new Color(1f, 0.98f, 0.9f, 0.6f)).rectTransform;
            Fixar(_reflexo, new Vector2(0f, 0.5f), Meio, Vector2.zero, new Vector2(Dp.Px(120f), h * 3f));
            _brilhoImg = Formas.Imagem(trilho, "Brilho", Formas.Sombra(), Formas.ComAlfa(Estilo.Ouro, 0.9f));
            _brilho = _brilhoImg.rectTransform;
            Fixar(_brilho, new Vector2(0f, 0.5f), Meio, Vector2.zero, new Vector2(Dp.Px(46f), Dp.Px(28f)));

            float yTexto = baixo + h + Dp.Px(5f);
            _passo = Formas.Texto(c, "Passo", "", 12f, Formas.ComAlfa(Estilo.Texto, 0.85f), TextAnchor.LowerLeft);
            Fixar(_passo.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, yTexto), new Vector2(Dp.Px(420f), Dp.Px(20f)));
            _pct = Formas.Texto(c, "Porcento", "", 18f, Estilo.Ouro, TextAnchor.LowerRight);
            _pct.fontStyle = FontStyle.Bold;
            Fixar(_pct.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, yTexto - Dp.Px(2f)), new Vector2(Dp.Px(90f), Dp.Px(26f)));

            // a DICA: 2/3 da largura (anchor, nao Screen: a foto troca o tamanho do canvas), losango + DICA + fio + o texto
            var dica = Hud.Placa(c, "Dica", Dp.Px(10f)).rectTransform;
            dica.anchorMin = new Vector2(0.17f, 0f); dica.anchorMax = new Vector2(0.83f, 0f); dica.pivot = new Vector2(0.5f, 0f);
            float yDica = yTexto + Dp.Px(30f);
            dica.offsetMin = new Vector2(0f, yDica); dica.offsetMax = new Vector2(0f, yDica + Dp.Px(50f));
            var los = Formas.Imagem(dica, "Losango", Formas.Losango(), Estilo.Ouro);
            Fixar(los.rectTransform, new Vector2(0f, 0.5f), Meio, new Vector2(Dp.Px(18f), 0f), Vector2.one * Dp.Px(10f));
            var rotulo = Formas.Texto(dica, "Rotulo", Textos.CarregaDica, 12f, Estilo.Ouro, TextAnchor.MiddleLeft);
            rotulo.fontStyle = FontStyle.Bold;
            Fixar(rotulo.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(Dp.Px(30f), 0f), new Vector2(Dp.Px(44f), Dp.Px(20f)));
            var sep = Formas.Imagem(dica, "Separador", null, Formas.ComAlfa(Estilo.OuroFosco, 0.6f));
            Fixar(sep.rectTransform, new Vector2(0f, 0.5f), Meio, new Vector2(Dp.Px(78f), 0f), new Vector2(fio, Dp.Px(28f)));
            _dica = Formas.Texto(dica, "Texto", "", 14f, Estilo.Texto, TextAnchor.MiddleLeft);
            _dica.horizontalOverflow = HorizontalWrapMode.Wrap; _dica.verticalOverflow = VerticalWrapMode.Truncate;
            _dica.resizeTextForBestFit = true; _dica.resizeTextMaxSize = _dica.fontSize;
            _dica.resizeTextMinSize = Mathf.Max(8, Mathf.RoundToInt(_dica.fontSize * 0.75f));   // dica comprida encolhe, nao vaza
            AreaSegura.Esticar(_dica.rectTransform);
            _dica.rectTransform.offsetMin = new Vector2(Dp.Px(90f), Dp.Px(4f)); _dica.rectTransform.offsetMax = new Vector2(-Dp.Px(16f), -Dp.Px(4f));
        }

        void Update()
        {
            Logica.Tick(Time.unscaledDeltaTime);   // a montagem roda com timeScale 0: a tela anda no relogio de verdade
            Pintar();
            if (Logica.Sumiu) Destroy(gameObject);
        }

        void Pintar()
        {
            float t = Logica.Tempo, b = Logica.Barra;
            _fill.anchorMax = new Vector2(b, 1f);
            _brilho.anchorMin = _brilho.anchorMax = new Vector2(b, 0.5f);
            _brilhoImg.enabled = b > 0.002f;
            _brilhoImg.canvasRenderer.SetAlpha(0.7f + 0.3f * Mathf.Sin(t * 4.5f));   // pulsa: alfa pelo CanvasRenderer, sem refazer malha
            float w = _fill.rect.width, rw = _reflexo.sizeDelta.x;
            _reflexo.anchoredPosition = new Vector2(Mathf.Lerp(-rw, w + rw, Mathf.Repeat(t, ReflexoS) / ReflexoS), 0f);
            if (Logica.Porcento != _pctVisto) { _pctVisto = Logica.Porcento; _pct.text = string.Format(Textos.CarregaPct, _pctVisto); }
            if (Logica.Dica != _dicaVista) { _dicaVista = Logica.Dica; _dica.text = _dicas.Length > 0 ? _dicas[_dicaVista % _dicas.Length] : ""; }
            _dica.canvasRenderer.SetAlpha(Logica.DicaAlfa);
            _selo.localRotation = Quaternion.Euler(0f, 0f, -t * SeloGiro);
            if (_arte.enabled)
            {
                float z = 1f + ArteZoom * Mathf.SmoothStep(0f, 1f, t / ArteZoomS);
                _arte.rectTransform.localScale = new Vector3(z, z, 1f);
            }
            _grupo.alpha = Logica.Alfa;
        }

        /// <summary>Um selo de 128 px por processo (o do titulo e' de 256): o do canto e o fosco do fundo sem arte.</summary>
        static Sprite SpriteDoSelo()
        {
            if (_seloSprite == null) _seloSprite = Selo.SpriteDe(128);
            return _seloSprite;
        }

        /// <summary>Texto de uma linha no topo do bloco (`y` do topo e altura `h` em dp), com o contorno do titulo do menu.</summary>
        static Text Linha(RectTransform pai, string nome, string texto, float yDp, float hDp, float tamDp, Color cor)
        {
            var t = Formas.Texto(pai, nome, texto, tamDp, cor, TextAnchor.MiddleLeft);
            var rt = t.rectTransform;
            rt.anchorMin = SupEsq; rt.anchorMax = Vector2.one; rt.pivot = SupEsq;
            rt.offsetMin = new Vector2(0f, -Dp.Px(yDp + hDp)); rt.offsetMax = new Vector2(0f, -Dp.Px(yDp));
            ArkMenu.Contorno(t);
            return t;
        }

        /// <summary>Ancora num PONTO do pai (0..1) com pivo, posicao e tamanho em px.</summary>
        static void Fixar(RectTransform rt, Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 tam)
        {
            rt.anchorMin = ancora; rt.anchorMax = ancora; rt.pivot = pivo;
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
        }
    }
}
