using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Menu;

namespace Arkana.UI
{
    /// <summary>
    /// Logica PURA do que a HUD mostra e que nao e' "aviso": rotulo da arma que SOME em 2,5 s, carrossel escondido de
    /// maos nuas e com luva travada, botao de ataque desarmado, kill feed com o NOME do mago, relogio ("TREINO" no
    /// treino), numeros de dano com merge e hitmarker. Tick(dt) e' o relogio; nada aqui decide jogo.
    /// </summary>
    public sealed class HudLogica
    {
        /// <summary>Segundos que a confirmacao da arma fica na tela. Subir = poluicao; descer = pisca ilegivel.</summary>
        public const float ArmaRotuloS = 2.5f;
        public const float ArmaFadeS = 0.5f;
        public const float KillFeedS = 4f;
        public const float HitmarkerS = 0.16f;

        public sealed class Numero { public float Total; public float Nasceu; public float Ate; public bool EmEscudo; public Elemento Elemento; }
        public sealed class Abate { public string Nome; public float Ate; }

        public string ArmaRotulo { get; private set; } = "";
        public bool Armado { get; private set; }
        public bool CarrosselVisivel { get; private set; }
        public float Hitmarker { get; private set; }
        public readonly List<Abate> KillFeed = new List<Abate>();
        public float Agora { get; private set; }

        float _armaT = -1f;   // < 0 = sem rotulo na tela
        readonly Dictionary<int, Numero> _numeros = new Dictionary<int, Numero>();
        readonly float _mergeS, _vidaS;

        public HudLogica(float numMergeS, float numLifeS) { _mergeS = numMergeS; _vidaS = numLifeS; }

        /// <summary>Partida nova: maos nuas, sem elemento, sem rotulo, sem feed.</summary>
        public void MaosNuas()
        {
            Armado = false;
            CarrosselVisivel = false;
            ArmaRotulo = "";
            _armaT = -1f;
            KillFeed.Clear();
            _numeros.Clear();
            Hitmarker = 0f;
        }

        /// <summary>Equipar ACENDE o ataque; o rotulo (ja' formatado: nome + ELEMENTO) aparece e depois some.
        /// Luva com elemento travado esconde o carrossel; luva/arma sem elemento o devolve.</summary>
        public void Equipar(string rotulo, Elemento[] elementos)
        {
            Armado = true;
            ArmaRotulo = rotulo ?? "";
            _armaT = 0f;
            CarrosselVisivel = elementos == null || elementos.Length == 0;
        }

        /// <summary>1 enquanto le, esvaece ate' 0 depois de ArmaRotuloS.</summary>
        public float ArmaRotuloAlfa
        {
            get
            {
                if (_armaT < 0f) return 0f;
                if (_armaT <= ArmaRotuloS) return 1f;
                return Mathf.Clamp01(1f - (_armaT - ArmaRotuloS) / ArmaFadeS);
            }
        }
        public bool ArmaRotuloVisivel => ArmaRotuloAlfa > 0f;

        public void Abater(string nomeDoMago)
        {
            KillFeed.Add(new Abate { Nome = nomeDoMago, Ate = Agora + KillFeedS });
            while (KillFeed.Count > 4) KillFeed.RemoveAt(0);
        }

        public void Acertei() { Hitmarker = HitmarkerS; }

        /// <summary>Numero de dano por alvo: dentro de num_merge_s SOMA no mesmo numero (a manopla empilhava escada).</summary>
        public Numero RegistrarDano(int alvoId, float dano, Elemento el, bool emEscudo, out bool novo)
        {
            Numero n;
            if (_numeros.TryGetValue(alvoId, out n) && Agora < n.Ate)
            {
                n.Total += dano;
                n.Ate = Agora + _mergeS;
                n.EmEscudo = emEscudo;
                n.Elemento = el;
                novo = false;
                return n;
            }
            n = new Numero { Total = dano, Nasceu = Agora, Ate = Agora + _mergeS, EmEscudo = emEscudo, Elemento = el };
            _numeros[alvoId] = n;
            novo = true;
            return n;
        }

        /// <summary>0..1 de vida do numero (1 = acabou de nascer, 0 = morreu).</summary>
        public float VidaDoNumero(Numero n) => Mathf.Clamp01(1f - (Agora - n.Nasceu) / Mathf.Max(_vidaS, 0.01f));
        public int NumerosVivos => _numeros.Count;

        public static string TextoRelogio(float restanteS, bool treino, string rotuloTreino)
        {
            if (treino) return rotuloTreino;
            int t = Mathf.Max(Mathf.CeilToInt(restanteS), 0);
            return string.Format("{0}:{1:00}", t / 60, t % 60);
        }

        public void Tick(float dt)
        {
            Agora += dt;
            if (_armaT >= 0f)
            {
                _armaT += dt;
                if (_armaT > ArmaRotuloS + ArmaFadeS) _armaT = -1f;
            }
            if (Hitmarker > 0f) Hitmarker = Mathf.Max(Hitmarker - dt, 0f);
            KillFeed.RemoveAll(a => Agora >= a.Ate);
            var mortos = new List<int>();
            foreach (var kv in _numeros) if (VidaDoNumero(kv.Value) <= 0f) mortos.Add(kv.Key);
            foreach (int k in mortos) _numeros.Remove(k);
        }
    }

    /// <summary>
    /// Layout PURO da HUD em px de tela (origem inferior esquerda): toda margem de borda passa pela area segura.
    /// Existe fora do MonoBehaviour para o teste cobrar "pausa DENTRO da tela", alvos &gt;= 48dp e fileira sem sobreposicao.
    /// </summary>
    public struct HudLayout
    {
        public Rect Barras, Topo, Pausa, Joystick, Disparo, Esquiva, Tatica, Suprema, Salto, Carrossel, ArmaRotulo, Pegar, Altimetro, KillFeed;

        public static HudLayout Calcular(Vector2 tela, Margens m, float px)
        {
            var l = new HudLayout();
            float mm = 16f * px;    // respiro de leitura
            float g = 24f * px;     // respiro de dedo
            l.Barras = new Rect(m.Esq + mm, tela.y - m.Topo - mm - 56f * px, 190f * px, 56f * px);
            l.Topo = new Rect(tela.x - m.Dir - mm - 180f * px, tela.y - m.Topo - mm - 52f * px, 180f * px, 52f * px);
            // pausa: canto superior direito, ABAIXO do relogio, dentro da tela (o teste cobra o retangulo)
            float pz = 44f * px;
            l.Pausa = new Rect(tela.x - m.Dir - g - pz, tela.y - m.Topo - g - 58f * px - pz, pz, pz);
            float js = 150f * px;
            l.Joystick = new Rect(m.Esq + g, m.Baixo + g, js, js);
            float fb = 88f * px;
            l.Disparo = new Rect(tela.x - m.Dir - g - fb, m.Baixo + 40f * px, fb, fb);
            float db = 64f * px;
            float dir = l.Disparo.xMin;
            l.Esquiva = new Rect(dir - 12f * px - db, l.Disparo.yMin, db, db); dir = l.Esquiva.xMin;
            l.Tatica = new Rect(dir - 12f * px - db, l.Disparo.yMin, db, db); dir = l.Tatica.xMin;
            l.Suprema = new Rect(dir - 12f * px - db, l.Disparo.yMin, db, db);
            l.Salto = new Rect(tela.x - m.Dir - g - db, l.Disparo.yMax + 10f * px, db, db);
            float slot = 52f * px;
            float nEl = Elementos.Todos.Length;
            l.Carrossel = new Rect(l.Salto.xMin - 10f * px - slot * nEl, l.Disparo.yMax + 10f * px, slot * nEl, slot);
            l.ArmaRotulo = new Rect(tela.x - m.Dir - 320f * px, l.Carrossel.yMax + 4f * px, 320f * px - g, 16f * px);
            float pb = 64f * px;
            l.Pegar = new Rect(tela.x / 2f - pb / 2f, m.Baixo + 40f * px, pb, pb);
            l.Altimetro = new Rect(tela.x - m.Dir - mm - 120f * px, tela.y / 2f - 20f * px, 120f * px, 40f * px);
            l.KillFeed = new Rect(m.Esq + mm, l.Barras.yMin - 8f * px - 72f * px, 260f * px, 72f * px);
            return l;
        }
    }

    /// <summary>
    /// HUD de partida — OBSERVA o Bus e encaminha toque. Nada aqui decide jogo. Montada por codigo (zero prefab).
    /// Mapa (paisagem): sup-esq vida/mana/escudo + kill feed; sup-dir relogio/bots/FPS + PAUSA; faixa de aviso no alto;
    /// inf-esq joystick; inf-dir Fogo, Esquiva, TATICA, SUPREMA, SALTO e o carrossel; inf-meio PEGAR.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        // ---------- textos: TUDO de Core.Textos (nenhum texto solto). Os dois ultimos ainda nao existem la'. ----------
        public const string T_VIDA = Textos.HudVida, T_MANA = Textos.HudMana, T_ESCUDO = Textos.HudEscudo, T_BOTS = Textos.HudBots, T_FPS = Textos.HudFps;
        public const string T_TREINO = Textos.MenuTreino, T_VITORIA = Textos.HudVitoria, T_DERROTA = Textos.HudDerrota, T_JOGAR_DE_NOVO = Textos.HudJogarDeNovo, T_MENU = Textos.HudMenu;
        public const string T_ESQUIVA = Textos.HudEsquiva, T_TATICA = Textos.HudTatica, T_SUPREMA = Textos.HudSuprema, T_SALTO = Textos.HudSalto, T_PAUSA = Textos.HudPausa, T_KIT_EM_BREVE = Textos.HudKitEmBreve;
        public const string T_TELEGRAFO = Textos.HudTelegrafo, T_SEP = Textos.HudSep;
        public const string T_ZONA_ABERTURA = Textos.ZonaAbertura, T_ZONA_FORMANDO = Textos.ZonaFormando;
        public const string T_ZONA_AVISO = Textos.ZonaAviso, T_ZONA_FECHANDO = Textos.ZonaFechando, T_ZONA_FORA = Textos.ZonaFora, T_ZONA_DPS = Textos.ZonaDps;
        public const string T_BAU = Textos.BauTitulo, T_BAU_CAINDO = Textos.BauCaindo, T_BAU_POUSOU = Textos.BauPousou, T_BAU_ABRINDO = Textos.BauAbrindo, T_BAU_MANOPLA = Textos.BauManopla, T_BAU_PERDIDO = Textos.BauPerdido;
        public const string T_PEGAR = Textos.LootPegar, T_TROCAR = Textos.LootTrocar;
        public const string T_PAUSA_TITULO = Textos.PausaTitulo, T_RETOMAR = Textos.PausaRetomar, T_CONFIG = Textos.MenuConfig, T_ABANDONAR = Textos.PausaAbandonar;
        public const string T_ALTITUDE = "{0} m";            // ponytail: mover para Textos quando o CORE quiser
        public const string T_ABATE = "{0} derrubado";
        public static IReadOnlyDictionary<string, string> Estados => Textos.HudEstados;
        static readonly Color CorZona = new Color(0.55f, 0.35f, 1f);

        /// <summary>Raridade -> cor + FORMA do contorno (GDD §10). Desconhecida = branco redondo.</summary>
        public static void Raridade(string r, out Color cor, out string forma)
        {
            switch ((r ?? "").ToLowerInvariant())
            {
                case "raro": case "rara": cor = Formas.Hex("#4C8CFF"); forma = "losango"; break;
                case "epico": case "epica": cor = Formas.Hex("#9B5CE6"); forma = "losango"; break;
                case "lendaria": case "lendario": cor = Estilo.Ouro; forma = "triangulo"; break;
                case "comum": cor = Formas.Hex("#DDE3F0"); forma = ""; break;
                default: cor = Color.white; forma = ""; break;
            }
        }

        // ---------- API para a raia CENA ----------
        public HudLogica Logica { get; private set; }
        public AvisoLogica Aviso { get; private set; }
        public JoystickVirtual Joystick { get; private set; }
        public BotaoDisparo Disparo { get; private set; }
        public BotaoAcao Esquiva { get; private set; }
        public BotaoAcao Tatica { get; private set; }
        public BotaoAcao Suprema { get; private set; }
        public BotaoAcao Salto { get; private set; }
        public BotaoAcao Pegar { get; private set; }
        public BotaoAcao Pausa { get; private set; }
        public CarrosselElementos Carrossel { get; private set; }
        public IEntidade Jogador { get; private set; }
        public bool Pausado { get; private set; }
        public bool SegurandoSalto => Salto != null && Salto.Segurando;

        public event Action<Vector2> OlharDelta;
        public event Action<Elemento> ElementoEscolhido;
        public event Action ReiniciarPedido;
        public event Action MenuPedido;
        public event Action AbandonarPedido;

        Canvas _canvas;
        RectTransform _raiz;
        RectTransform _barras;
        BarraHud _hp, _mana, _escudo;
        Arkana.Gameplay.Pawn _pawn;
        Image[] _escudoSegs = new Image[0];
        Text _relogio, _bots, _fps, _armaRotulo, _altimetro, _killFeed;
        RectTransform _altimetroBox;
        HudAviso _aviso;
        Image _reticulo;
        Image[] _hitmarker;
        RectTransform _numeros;
        readonly Dictionary<HudLogica.Numero, Text> _labels = new Dictionary<HudLogica.Numero, Text>();
        RectTransform _fim;
        Text _fimTexto;
        RectTransform _pausaOverlay;
        RectTransform _olhar;
        Vector2 _telaAtual;
        bool _treino;
        float _restante;
        int _botsVivos;
        float _escudoQuebrou;
        float _escudoVal, _escudoMax;
        int _escudoNivel;
        bool _numerosDano = true;
        bool _mostraFps;

        public static Hud Criar()
        {
            var canvas = Formas.CanvasTelaCheia("HUD", 10);
            var hud = canvas.gameObject.AddComponent<Hud>();
            hud._canvas = canvas;
            hud.Montar();
            return hud;
        }

        void Montar()
        {
            _raiz = (RectTransform)_canvas.transform;
            Logica = new HudLogica((float)Balance.Feedback.NumMergeS, (float)Balance.Feedback.NumLifeS);
            Aviso = new AvisoLogica((float)Balance.Feedback.VignetteMinS, (float)Balance.Feedback.ArcDurS, (int)Balance.Feedback.ArcMax, Estados.Keys);

            // grade de tela: escurece topo e rodape (leitura), o meio fica livre. EM DEGRADE: o retangulo chapado do
            // Godot deixava duas faixas de borda dura cortando o mundo (foto de 11/09). Pico maior, media parecida.
            var gTopo = Formas.Imagem(_raiz, "GradeTopo", Formas.Degrade(true), new Color(0.02f, 0.025f, 0.04f, 0.34f));
            gTopo.raycastTarget = false;
            gTopo.rectTransform.anchorMin = new Vector2(0, 0.81f); gTopo.rectTransform.anchorMax = Vector2.one; gTopo.rectTransform.offsetMin = Vector2.zero; gTopo.rectTransform.offsetMax = Vector2.zero;
            var gBaixo = Formas.Imagem(_raiz, "GradeBaixo", Formas.Degrade(false), new Color(0.02f, 0.025f, 0.04f, 0.26f));
            gBaixo.raycastTarget = false;
            gBaixo.rectTransform.anchorMin = Vector2.zero; gBaixo.rectTransform.anchorMax = new Vector2(1, 0.28f); gBaixo.rectTransform.offsetMin = Vector2.zero; gBaixo.rectTransform.offsetMax = Vector2.zero;

            // olhar livre: metade direita, ATRAS dos botoes (irmao anterior = raycast por baixo)
            _olhar = Formas.No(_raiz, "Olhar");
            var olharImg = _olhar.gameObject.AddComponent<Image>();
            olharImg.color = new Color(0, 0, 0, 0.001f);
            olharImg.raycastTarget = true;
            _olhar.gameObject.AddComponent<OlharArrasto>().Delta = d => OlharDelta?.Invoke(d);

            // barras (vida manda: mais alta; mana e escudo finas embaixo — cabem nos 56dp do HudLayout.Barras)
            _barras = Formas.No(_raiz, "Barras");
            _hp = new BarraHud(_barras, T_VIDA, new Color(0.88f, 0.22f, 0.2f), 0f, 18f, 10f);
            _mana = new BarraHud(_barras, T_MANA, new Color(0.22f, 0.5f, 1f), 22f, 13f, 8.5f);
            _escudo = new BarraHud(_barras, T_ESCUDO, Color.white, 39f, 13f, 8.5f);
            _escudo.Linha.gameObject.SetActive(false);
            _killFeed = Formas.Texto(_raiz, "KillFeed", "", 11f, new Color(1, 1, 1, 0.9f), TextAnchor.UpperLeft);

            // topo direito
            var topo = Formas.No(_raiz, "Topo");
            _relogio = Formas.Texto(topo, "Relogio", "0:00", 20f, Color.white, TextAnchor.UpperRight);
            _bots = Formas.Texto(topo, "Bots", "", 13f, Color.white, TextAnchor.UpperRight);
            _fps = Formas.Texto(topo, "Fps", "", 12f, new Color(0.6f, 1f, 0.7f, 0.85f), TextAnchor.UpperRight);
            _relogio.rectTransform.anchorMin = new Vector2(0, 1); _relogio.rectTransform.anchorMax = new Vector2(1, 1); _relogio.rectTransform.pivot = new Vector2(1, 1);
            _relogio.rectTransform.anchoredPosition = Vector2.zero; _relogio.rectTransform.sizeDelta = new Vector2(0, Dp.Px(24f));
            _bots.rectTransform.anchorMin = new Vector2(0, 1); _bots.rectTransform.anchorMax = new Vector2(1, 1); _bots.rectTransform.pivot = new Vector2(1, 1);
            _bots.rectTransform.anchoredPosition = new Vector2(0, -Dp.Px(24f)); _bots.rectTransform.sizeDelta = new Vector2(0, Dp.Px(16f));
            _fps.rectTransform.anchorMin = new Vector2(0, 1); _fps.rectTransform.anchorMax = new Vector2(1, 1); _fps.rectTransform.pivot = new Vector2(1, 1);
            _fps.rectTransform.anchoredPosition = new Vector2(0, -Dp.Px(40f)); _fps.rectTransform.sizeDelta = new Vector2(0, Dp.Px(14f));
            _fps.enabled = false;

            // reticulo + hitmarker (SEM area segura: marca o centro da camera)
            _reticulo = Formas.Imagem(_raiz, "Reticulo", Formas.Anel(), new Color(1, 1, 1, 0.8f));
            _reticulo.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); _reticulo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _reticulo.rectTransform.sizeDelta = new Vector2(Dp.Px(12f), Dp.Px(12f));
            var ponto = Formas.Imagem(_reticulo.transform, "Ponto", Formas.Disco(), new Color(1, 1, 1, 0.9f));
            ponto.rectTransform.sizeDelta = new Vector2(Dp.Px(3f), Dp.Px(3f));
            _hitmarker = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var h = Formas.Imagem(_reticulo.transform, "Hit" + i, null, Color.white);
                h.rectTransform.sizeDelta = new Vector2(Dp.Px(1.6f), Dp.Px(7f));
                float ang = 45f + i * 90f;
                h.rectTransform.localRotation = Quaternion.Euler(0, 0, ang);
                h.rectTransform.anchoredPosition = new Vector2(-Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad)) * Dp.Px(10.5f);
                h.enabled = false;
                _hitmarker[i] = h;
            }

            _aviso = HudAviso.Criar(_raiz, Aviso);
            _aviso.Angulo = AnguloDe;
            _aviso.RotuloEstado = s => { string r; return Estados.TryGetValue(s, out r) ? r : s; };
            _aviso.TituloDerrubado = Textos.DerrubadoVoce; _aviso.TituloAliado = Textos.DerrubadoAliado;
            _aviso.TextoEsvaecendo = Textos.DerrubadoEsvaecendo; _aviso.TextoReerguendo = Textos.DerrubadoReerguendo;
            _numeros = Formas.No(_raiz, "Numeros");
            AreaSegura.Esticar(_numeros);

            // altimetro (queda)
            _altimetroBox = Formas.No(_raiz, "Altimetro");
            var altFundo = Formas.Imagem(_altimetroBox, "Fundo", null, new Color(0, 0, 0, 0.35f));
            AreaSegura.Esticar(altFundo.rectTransform);
            _altimetro = Formas.Texto(_altimetroBox, "Texto", "", 16f, Color.white, TextAnchor.MiddleRight);
            AreaSegura.Esticar(_altimetro.rectTransform);
            _altimetroBox.gameObject.SetActive(false);

            // controles
            Joystick = JoystickVirtual.Criar(_raiz, Dp.Px(150f));
            Disparo = BotaoDisparo.Criar(_raiz, Dp.Px(88f));
            Esquiva = BotaoAcao.Criar(_raiz, "Esquiva", T_ESQUIVA, new Color(0.25f, 0.85f, 0.95f), Dp.Px(64f));
            Tatica = BotaoAcao.Criar(_raiz, "Tatica", T_TATICA, new Color(0.55f, 0.80f, 1f), Dp.Px(64f));
            Suprema = BotaoAcao.Criar(_raiz, "Suprema", T_SUPREMA, Estilo.Ouro, Dp.Px(64f));   // o ouro da paleta, o mesmo do fio das barras
            Suprema.Logica.MostraCarga = true;    // a suprema mostra 0->100% (carga, nao cooldown)
            Tatica.Ativo(false); Suprema.Ativo(false);   // ate' o KitBound dizer que o mago tem kit
            Salto = BotaoAcao.Criar(_raiz, "Salto", T_SALTO, new Color(0.80f, 0.86f, 1f), Dp.Px(64f));
            Pegar = BotaoAcao.Criar(_raiz, "Pegar", T_PEGAR, new Color(0.85f, 0.90f, 1f), Dp.Px(64f));
            Pegar.gameObject.SetActive(false);
            Pausa = BotaoAcao.Criar(_raiz, "Pausa", T_PAUSA, new Color(0.75f, 0.78f, 0.9f), Dp.Px(44f));
            Pausa.Tocado += AbrirPausa;
            Carrossel = CarrosselElementos.Criar(_raiz, Dp.Px(52f));
            Carrossel.Escolheu += e => ElementoEscolhido?.Invoke(e);
            Carrossel.Visivel(false);
            _armaRotulo = Formas.Texto(_raiz, "ArmaRotulo", "", 11f, Color.white, TextAnchor.MiddleRight);

            MontarFim();
            Layout();
            Assinar();
            AplicarConfig(ConfigLogica.Atual);
            ConfigLogica.Mudou += AplicarConfig;
            Logica.MaosNuas();
            Disparo.Desarmar();
        }

        /// <summary>
        /// Uma barra (vida/mana/escudo). A foto de 12/09 mostrava retangulo chapado com o rotulo solto ao lado; agora:
        /// moldura escura de fio dourado e cantos redondos, trilho, RASTRO de dano (faixa clara que segura o valor velho e
        /// desce devagar ate' o atual — o olho le' QUANTO o golpe tirou) e preenchimento em degrade (mais claro em cima),
        /// com ROTULO a esquerda e atual/max a direita DENTRO da barra. Texto so' e' refeito quando o inteiro muda.
        /// </summary>
        sealed class BarraHud
        {
            const float LarguraDp = 190f;
            const float RastroEsperaS = 0.35f;   // segura o valor velho antes de descer
            const float RastroVel = 0.8f;        // fracao da barra por segundo na descida
            static readonly Color CorRastro = new Color(1f, 0.93f, 0.8f, 0.85f);

            public readonly RectTransform Linha;
            public readonly RectTransform Trilho;   // o escudo pendura os separadores de nivel aqui
            public readonly Image Fill;
            readonly Image _rastro;
            readonly Text _numero;
            float _frac = 1f, _rastroF = 1f, _espera;
            int _cur = int.MinValue, _max = int.MinValue;

            public BarraHud(RectTransform pai, string rotulo, Color cor, float yDp, float alturaDp, float fonteDp)
            {
                float h = Dp.Px(alturaDp);
                float fio = Mathf.Max(Dp.Px(1f), 1f), folga = Mathf.Max(Dp.Px(2f), 2f);
                float raio = Mathf.Min(Dp.Px(5f), h * 0.5f);
                Linha = Formas.No(pai, "Linha" + rotulo);
                Linha.anchorMin = new Vector2(0, 1); Linha.anchorMax = new Vector2(0, 1); Linha.pivot = new Vector2(0, 1);
                Linha.sizeDelta = new Vector2(Dp.Px(LarguraDp), h);
                Linha.anchoredPosition = new Vector2(0, -Dp.Px(yDp));
                var borda = Formas.Arredondada(Linha, "Borda", Formas.ComAlfa(Estilo.OuroFosco, 0.95f), raio);
                AreaSegura.Esticar(borda.rectTransform);
                var fundo = Formas.Arredondada(Linha, "Fundo", new Color(0.03f, 0.04f, 0.07f, 0.8f), raio - fio);
                Recuar(fundo.rectTransform, fio, fio);
                Trilho = Formas.No(Linha, "Trilho");
                Recuar(Trilho, folga, folga);
                float raioDentro = Mathf.Max(raio - folga, 1f);
                _rastro = Formas.Arredondada(Trilho, "Rastro", CorRastro, raioDentro);
                AreaSegura.Esticar(_rastro.rectTransform);
                Fill = Formas.Arredondada(Trilho, "Fill", cor, raioDentro, true);
                AreaSegura.Esticar(Fill.rectTransform);
                var t = Formas.Texto(Linha, "Rotulo", rotulo, fonteDp, new Color(1f, 1f, 1f, 0.95f), TextAnchor.MiddleLeft);
                t.fontStyle = FontStyle.Bold;
                Contornar(t);
                Recuar(t.rectTransform, Dp.Px(6f), 0f);
                _numero = Formas.Texto(Linha, "Numero", "", fonteDp, new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleRight);
                Contornar(_numero);
                Recuar(_numero.rectTransform, Dp.Px(6f), 0f);
            }

            /// <summary>Valor novo. Caiu = o rastro segura e depois desce; subiu (cura) = sem rastro.</summary>
            public void Valor(float cur, float max)
            {
                float f = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
                if (f < _frac - 0.0001f) _espera = RastroEsperaS;
                _frac = f;
                if (_rastroF < f) _rastroF = f;
                Ancorar(Fill, f);
                Ancorar(_rastro, _rastroF);
                int c = Mathf.Max(Mathf.CeilToInt(cur - 0.001f), 0), m = Mathf.Max(Mathf.RoundToInt(max), 0);
                if (c == _cur && m == _max) return;
                _cur = c; _max = m;
                _numero.text = m > 0 ? c + "/" + m : "";
            }

            /// <summary>Partida nova: o valor sem rastro (nao houve golpe).</summary>
            public void Encher(float cur, float max)
            {
                Valor(cur, max);
                _rastroF = _frac; _espera = 0f;
                Ancorar(_rastro, _rastroF);
            }

            /// <summary>Por frame: parado (rastro == valor) nao toca em nada.</summary>
            public void Tick(float dt)
            {
                if (_rastroF <= _frac) return;
                if (_espera > 0f) { _espera -= dt; return; }
                _rastroF = Mathf.Max(_rastroF - RastroVel * dt, _frac);
                Ancorar(_rastro, _rastroF);
            }

            static void Ancorar(Image img, float f)
            {
                img.enabled = f > 0.001f;
                img.rectTransform.anchorMax = new Vector2(f, 1f);
            }

            static void Recuar(RectTransform rt, float x, float y)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(x, y); rt.offsetMax = new Vector2(-x, -y);
            }

            /// <summary>Contorno escuro: o texto le' em cima do vermelho, do azul E do escudo branco/dourado.</summary>
            static void Contornar(Text t)
            {
                var o = t.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.55f);
                o.effectDistance = new Vector2(1f, -1f);
            }
        }

        void MontarFim()
        {
            _fim = Formas.No(_raiz, "Fim");
            AreaSegura.Esticar(_fim);
            var veu = Formas.Imagem(_fim, "Veu", null, new Color(0, 0, 0, 0.55f));
            AreaSegura.Esticar(veu.rectTransform);
            veu.raycastTarget = true;
            var col = Estilo.Coluna(_fim, "Centro", 16f);
            _fimTexto = Formas.Texto(col.transform, "Titulo", "", 30f, Color.white);
            Estilo.Tamanho(_fimTexto, Dp.Px(320f), Dp.Px(44f));
            var de_novo = Estilo.Botao(col.transform, "BtnJogarDeNovo", T_JOGAR_DE_NOVO, 200f, 56f, 16f);
            Estilo.Tamanho(de_novo, Dp.Px(200f), Estilo.AlturaAlvo(56f));
            de_novo.onClick.AddListener(() => ReiniciarPedido?.Invoke());
            var menu = Estilo.Botao(col.transform, "BtnMenu", T_MENU, 200f, 56f, 16f);
            Estilo.Tamanho(menu, Dp.Px(200f), Estilo.AlturaAlvo(56f));
            menu.onClick.AddListener(() => MenuPedido?.Invoke());
            _fim.gameObject.SetActive(false);
        }

        /// <summary>
        /// O que a HUD cobre. No aparelho (Overlay) e' a tela. Na FOTO (foto.ps1: ScreenSpaceCamera num RenderTexture)
        /// e' o alvo da camera — sem isto a HUD saia montada para a janela do editor (640x480) e amontoada num canto.
        /// </summary>
        Vector2 TamanhoDaTela()
        {
            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera && _canvas.worldCamera != null)
                return _canvas.worldCamera.pixelRect.size;
            return new Vector2(Screen.width, Screen.height);
        }

        /// <summary>TODA margem de borda mora aqui (area segura). Refeito quando a tela muda (rotacao, dobravel).</summary>
        public void Layout()
        {
            Vector2 tela = TamanhoDaTela();
            _telaAtual = tela;
            Margens m = AreaSegura.Atual();
            HudLayout l = HudLayout.Calcular(tela, m, Dp.Px(1f));
            AreaSegura.NoRect(_barras, l.Barras);
            AreaSegura.NoRect((RectTransform)_relogio.transform.parent, l.Topo);
            AreaSegura.NoRect((RectTransform)Pausa.transform, l.Pausa);
            AreaSegura.NoRect((RectTransform)Joystick.transform, l.Joystick);
            AreaSegura.NoRect((RectTransform)Disparo.transform, l.Disparo);
            AreaSegura.NoRect((RectTransform)Esquiva.transform, l.Esquiva);
            AreaSegura.NoRect((RectTransform)Tatica.transform, l.Tatica);
            AreaSegura.NoRect((RectTransform)Suprema.transform, l.Suprema);
            AreaSegura.NoRect((RectTransform)Salto.transform, l.Salto);
            AreaSegura.NoRect((RectTransform)Carrossel.transform, l.Carrossel);
            AreaSegura.NoRect(_armaRotulo.rectTransform, l.ArmaRotulo);
            AreaSegura.NoRect((RectTransform)Pegar.transform, l.Pegar);
            AreaSegura.NoRect(_altimetroBox, l.Altimetro);
            AreaSegura.NoRect(_killFeed.rectTransform, l.KillFeed);
            // olhar livre: da fronteira do joystick (35%) ate' a borda direita
            _olhar.anchorMin = new Vector2(0.35f, 0); _olhar.anchorMax = Vector2.one; _olhar.offsetMin = Vector2.zero; _olhar.offsetMax = Vector2.zero;
            _aviso.Layout(tela, m);
        }

        // ---------- Bus ----------
        void Assinar()
        {
            Bus.HealthChanged += OnHp;
            Bus.ManaChanged += OnMana;
            Bus.DamageApplied += OnDano;
            Bus.ShieldChanged += OnEscudo;
            Bus.ShieldBroken += OnEscudoQuebrou;
            Bus.EntityDied += OnMorreu;
            Bus.PlayerKilledBot += OnAbate;
            Bus.KitBound += OnKitBound;
            Bus.KitCooldown += OnKitCooldown;
            Bus.KitTelegraph += OnKitTelegraph;
            Bus.KitState += OnKitState;
            Bus.ZonaAbertura += OnZonaAbertura;
            Bus.ZonaFormando += OnZonaFormando;
            Bus.ZonaAvisou += OnZonaAvisou;
            Bus.ZonaFechando += OnZonaFechando;
            Bus.ZonaDano += OnZonaDano;
            Bus.ZonaEstado += OnZonaEstado;
            Bus.BauAnunciado += OnBauAnunciado;
            Bus.BauPousou += OnBauPousou;
            Bus.BauCanalizando += OnBauCanalizando;
            Bus.BauAberto += OnBauAberto;
            Bus.LootPrompt += OnLoot;
            Bus.WeaponEquipped += OnArma;
            Bus.ElementChanged += OnElemento;
            Bus.EntityDerrubada += OnDerrubada;
            Bus.EntityReerguida += OnReerguida;
            Bus.DerrubadoProgresso += OnDerrubadoProgresso;
            Bus.QuedaFase += OnQuedaFase;
            Bus.QuedaAltura += OnQuedaAltura;
            Bus.MatchStarted += OnMatchStarted;
            Bus.MatchOver += OnMatchOver;
        }

        void OnDestroy()
        {
            Bus.HealthChanged -= OnHp; Bus.ManaChanged -= OnMana; Bus.DamageApplied -= OnDano; Bus.ShieldChanged -= OnEscudo;
            Bus.ShieldBroken -= OnEscudoQuebrou; Bus.EntityDied -= OnMorreu; Bus.PlayerKilledBot -= OnAbate; Bus.KitBound -= OnKitBound;
            Bus.KitCooldown -= OnKitCooldown; Bus.KitTelegraph -= OnKitTelegraph; Bus.KitState -= OnKitState; Bus.ZonaAbertura -= OnZonaAbertura;
            Bus.ZonaFormando -= OnZonaFormando; Bus.ZonaAvisou -= OnZonaAvisou; Bus.ZonaFechando -= OnZonaFechando; Bus.ZonaDano -= OnZonaDano;
            Bus.ZonaEstado -= OnZonaEstado; Bus.BauAnunciado -= OnBauAnunciado; Bus.BauPousou -= OnBauPousou; Bus.BauCanalizando -= OnBauCanalizando;
            Bus.BauAberto -= OnBauAberto; Bus.LootPrompt -= OnLoot; Bus.WeaponEquipped -= OnArma; Bus.ElementChanged -= OnElemento;
            Bus.EntityDerrubada -= OnDerrubada; Bus.EntityReerguida -= OnReerguida; Bus.DerrubadoProgresso -= OnDerrubadoProgresso;
            Bus.QuedaFase -= OnQuedaFase; Bus.QuedaAltura -= OnQuedaAltura; Bus.MatchStarted -= OnMatchStarted; Bus.MatchOver -= OnMatchOver;
            ConfigLogica.Mudou -= AplicarConfig;
            if (Pausado) Time.timeScale = 1f;
        }

        /// <summary>Partida nova: maos nuas (carrossel escondido, ataque apagado), nada herdado da partida velha.</summary>
        public void Vincular(IEntidade jogador)
        {
            Jogador = jogador;
            Logica.MaosNuas();
            Aviso.Zerar();
            Disparo.Desarmar();
            Carrossel.Visivel(false);
            Pegar.gameObject.SetActive(false);
            _armaRotulo.text = "";
            // O KitBound sai no Pawn.Montar, que pode acontecer ANTES desta HUD existir (ordem da cena).
            // Sem isto os botoes de tatica/suprema ficariam apagados para sempre — costura de 11/09/2026.
            var pawn = jogador as Arkana.Gameplay.Pawn;
            _pawn = pawn;
            if (pawn != null && pawn.Runner != null) OnKitBound(pawn.Runner.Slug, pawn.Runner.Impl != null);
            // As barras nascem com o que o corpo TEM: nenhum evento sai no spawn, e sem isto o atual/max ficava vazio ate'
            // o primeiro golpe e o escudo N1 com que todo mago cai (GDD §5) nem aparecia.
            Vitalidade vit = jogador != null ? jogador.Vital : null;
            _escudoVal = vit != null ? vit.Escudo : 0f; _escudoMax = vit != null ? vit.EscudoMax : 0f; _escudoNivel = vit != null ? vit.Nivel : 0;
            if (vit != null) _hp.Encher(vit.Hp, vit.HpMax);
            _mana.Encher(pawn != null ? pawn.Mana : Balance.Player.ManaMax, Balance.Player.ManaMax);
            _escudo.Encher(_escudoVal, _escudoMax);
            _escudo.Linha.gameObject.SetActive(_escudoMax > 0f);
            if (_escudoMax > 0f) PintarEscudo();
            _fim.gameObject.SetActive(false);
            _restante = (float)Balance.Match.DurationS;
            _botsVivos = (int)Balance.Match.Bots;
            _treino = false;
            foreach (var kv in _labels) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _labels.Clear();
            Retomar();
        }

        /// <summary>A cena chama por frame (ou quando muda): relogio e bots vivos nao passam pelo Bus.</summary>
        public void AtualizarPartida(float restanteS, int botsVivos) { _restante = restanteS; _botsVivos = botsVivos; _treino = false; }
        /// <summary>No TREINO nao existe relogio nem contagem que importe: o canto diz o que a cena e'.</summary>
        public void ModoTreino() { _treino = true; }

        public void MostrarFim(bool vitoria)
        {
            _fimTexto.text = vitoria ? T_VITORIA : T_DERROTA;
            _fimTexto.color = vitoria ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.35f, 0.35f);
            _fim.gameObject.SetActive(true);
        }

        void AplicarConfig(ConfigLogica cfg)
        {
            _mostraFps = cfg.Bool(ConfigLogica.K_CONTADOR_FPS);
            _numerosDano = cfg.Bool(ConfigLogica.K_NUMEROS_DANO);
            _fps.enabled = _mostraFps;
        }

        bool EhJogador(IEntidade e) => e != null && (e == Jogador || e.EhPlayer);

        void OnHp(float cur, float max) { _hp.Valor(cur, max); }
        void OnMana(float cur, float max) { _mana.Valor(cur, max); }
        void OnMatchStarted() { _fim.gameObject.SetActive(false); }
        void OnMatchOver(bool vitoria) { MostrarFim(vitoria); }
        void OnAbate(string nome) { Logica.Abater(nome); }
        void OnMorreu(IEntidade e) { }
        void OnElemento(Elemento e)
        {
            Carrossel.Selecionar(e);
            if (Logica.Armado) Disparo.Armar(Estilo.CorElemento(e), Elementos.Nome(e));
        }

        /// <summary>O acerto completo: fui EU -> hitmarker + numero; acertaram EM MIM -> vinheta + arco; bot em bot -> nada.</summary>
        void OnDano(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool emEscudo)
        {
            if (EhJogador(fonte) && !EhJogador(alvo))
            {
                Logica.Acertei();
                if (_numerosDano && alvo != null) NumeroDano(alvo, dano, el, emEscudo);
            }
            if (EhJogador(alvo))
            {
                Aviso.Pulsar(Estilo.CorElemento(el), Mathf.Clamp(dano / 25f, 0.35f, 1f));
                if (fonte != null) Aviso.MarcarArco(fonte.Pos, Estilo.CorElemento(el));   // terreno/DoT: sem fonte, sem direcao
            }
        }

        void NumeroDano(IEntidade alvo, float dano, Elemento el, bool emEscudo)
        {
            bool novo;
            var n = Logica.RegistrarDano(alvo.GetHashCode(), dano, el, emEscudo, out novo);
            Text lbl;
            if (!novo && _labels.TryGetValue(n, out lbl) && lbl != null) { PintarNumero(lbl, n); return; }
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 mundo = alvo.Pos + Vector3.up * 1.7f;
            Vector3 sp = cam.WorldToScreenPoint(mundo);
            if (sp.z < 0f) return;   // atras da camera projetaria no lugar errado
            lbl = Formas.Texto(_numeros, "Num", "", 18f, Color.white);
            lbl.rectTransform.anchorMin = Vector2.zero; lbl.rectTransform.anchorMax = Vector2.zero;
            lbl.rectTransform.sizeDelta = new Vector2(Dp.Px(80f), Dp.Px(24f));
            lbl.rectTransform.anchoredPosition = new Vector2(sp.x, sp.y);
            _labels[n] = lbl;
            PintarNumero(lbl, n);
        }

        void PintarNumero(Text lbl, HudLogica.Numero n)
        {
            float escala = Mathf.Min((float)Balance.Feedback.NumScaleBase + (float)Balance.Feedback.NumScaleGain * n.Total / 25f, (float)Balance.Feedback.NumScaleMax);
            lbl.text = Mathf.RoundToInt(n.Total).ToString();
            lbl.fontSize = Mathf.Max(Mathf.RoundToInt(Dp.Px(18f) * escala), 11);
            lbl.color = n.EmEscudo ? Formas.Cor(Balance.Feedback.CorEscudo) : Estilo.CorElemento(n.Elemento);
        }

        void OnEscudo(IEntidade e, float escudo, float max, int nivel)
        {
            if (!EhJogador(e)) return;   // 6 bots escudados na barra do jogador e' ruido
            _escudoVal = escudo; _escudoMax = max; _escudoNivel = nivel;
            _escudo.Linha.gameObject.SetActive(max > 0f);
            PintarEscudo();
        }

        void OnEscudoQuebrou(IEntidade e) { if (EhJogador(e)) { _escudoQuebrou = 0.35f; PintarEscudo(); } }

        /// <summary>Barra SEGMENTADA por nivel (forma antes da cor, GDD §10) na cor do nivel (Balance.Escudo.Cores).</summary>
        void PintarEscudo()
        {
            int n = Mathf.Max(_escudoNivel, 1);
            var cores = Balance.Escudo.Cores;
            Color cor = Formas.Cor(cores[Mathf.Clamp(n - 1, 0, cores.Length - 1)]);
            if (_escudoQuebrou > 0f) cor = Color.white;
            _escudo.Fill.color = cor;
            _escudo.Valor(_escudoVal, _escudoMax);
            if (_escudoSegs.Length != n - 1)
            {
                foreach (var s in _escudoSegs) if (s != null) Destroy(s.gameObject);
                _escudoSegs = new Image[Mathf.Max(n - 1, 0)];
                for (int i = 1; i < n; i++)
                {
                    // separador de nivel ancorado em i/n do trilho: acompanha a barra em qualquer dpi
                    var seg = Formas.Imagem(_escudo.Trilho, "Seg" + i, null, new Color(0, 0, 0, 0.6f));
                    seg.rectTransform.anchorMin = new Vector2(i / (float)n, 0f); seg.rectTransform.anchorMax = new Vector2(i / (float)n, 1f);
                    seg.rectTransform.sizeDelta = new Vector2(Mathf.Max(Dp.Px(1.5f), 1f), 0f);
                    seg.rectTransform.anchoredPosition = Vector2.zero;
                    _escudoSegs[i - 1] = seg;
                }
            }
        }

        /// <summary>Kit implementado ACENDE tatica/suprema com o nome do mago; sem kit fica APAGADO e marcado "EM BREVE".</summary>
        void OnKitBound(string slug, bool implementado)
        {
            KitDefLite kit = KitDefLite.De(slug);
            Tatica.Ativo(implementado); Suprema.Ativo(implementado);
            Tatica.Rotulo(T_TATICA); Suprema.Rotulo(T_SUPREMA);
            string sub = implementado ? kit.Nome : T_KIT_EM_BREVE;
            Tatica.Subtitulo(sub); Suprema.Subtitulo(sub);
            Tatica.Logica.Cooldown(0f, 0f); Suprema.Logica.Cooldown(0f, 0f);
        }

        void OnKitCooldown(string tipo, float restante, float total)
        {
            var b = tipo == "tatica" ? Tatica : Suprema;
            b.Logica.Cooldown(restante, total);
        }

        /// <summary>A lei do §4.3: "se mata rapido, avisa antes" — vale para a suprema de QUALQUER conjurador.</summary>
        void OnKitTelegraph(string slug, string tipo, float duracao, Vector3 pos)
        {
            Aviso.Avisar(AvisoLogica.P_TELEGRAFO, string.Format(T_TELEGRAFO, KitDefLite.De(slug).Nome), new Color(1f, 0.75f, 0.25f), Mathf.Max(duracao, 0.6f));
        }

        void OnKitState(string nome, bool ligado) { Aviso.Estado(nome, ligado); }

        void OnZonaAbertura(float s) { Aviso.Contar(AvisoLogica.P_ZONA, s, T_ZONA_ABERTURA, CorZona); }
        void OnZonaFormando(float raio, float dur) { Aviso.PararContagem(AvisoLogica.P_ZONA); Aviso.Avisar(AvisoLogica.P_ZONA, T_ZONA_FORMANDO, CorZona, dur); }
        void OnZonaAvisou(int fase, Vector3 centro, float raio, float s) { Aviso.SetBussola("zona", centro, CorZona); Aviso.Contar(AvisoLogica.P_ZONA, s, T_ZONA_AVISO, CorZona); }
        void OnZonaFechando(int fase, Vector3 centro, float raio, float dur) { Aviso.SetBussola("zona", centro, CorZona); Aviso.PararContagem(AvisoLogica.P_ZONA); Aviso.Avisar(AvisoLogica.P_ZONA, T_ZONA_FECHANDO, CorZona, dur); }
        void OnZonaDano(float dano, float dps)
        {
            Aviso.Pulsar(CorZona, 0.9f);
            Aviso.Avisar(AvisoLogica.P_ZONA_FORA, T_ZONA_FORA + T_SEP + string.Format(T_ZONA_DPS, Mathf.RoundToInt(dps)), new Color(1f, 0.45f, 0.45f));
        }
        void OnZonaEstado(bool dentro)
        {
            if (dentro) Aviso.Limpar(AvisoLogica.P_ZONA_FORA);
            else Aviso.Avisar(AvisoLogica.P_ZONA_FORA, T_ZONA_FORA, new Color(1f, 0.45f, 0.45f));
        }

        void OnBauAnunciado(Vector3 pos, float s) { Aviso.SetBussola("bau", pos, Estilo.Ouro); Aviso.Contar(AvisoLogica.P_BAU, s, T_BAU + T_SEP + T_BAU_CAINDO, Estilo.Ouro); }
        void OnBauPousou(Vector3 pos) { Aviso.SetBussola("bau", pos, Estilo.Ouro); Aviso.PararContagem(AvisoLogica.P_BAU); Aviso.Avisar(AvisoLogica.P_BAU, T_BAU_POUSOU, Estilo.Ouro); }
        void OnBauCanalizando(IEntidade pawn, float prog)
        {
            if (pawn != null && !EhJogador(pawn)) return;
            if (prog <= 0f) { Aviso.Cancelar(); Aviso.Avisar(AvisoLogica.P_BAU, T_BAU_POUSOU, Estilo.Ouro); return; }
            Aviso.Canalizar(prog);
            _aviso.CorCanal(Estilo.Ouro);
            Aviso.Avisar(AvisoLogica.P_BAU, T_BAU_ABRINDO + T_SEP + Mathf.RoundToInt(prog * 100f) + "%", Estilo.Ouro);
        }
        void OnBauAberto(bool porPlayer, Elemento[] els)
        {
            Aviso.CanalizarFim();
            Aviso.SetBussola("bau", Vector3.zero, Color.white, false);
            Aviso.PararContagem(AvisoLogica.P_BAU);
            string txt = porPlayer && els != null && els.Length >= 2 ? string.Format(T_BAU_MANOPLA, Elementos.Nome(els[0]), Elementos.Nome(els[1])) : T_BAU_PERDIDO;
            Aviso.Avisar(AvisoLogica.P_BAU, txt, Estilo.Ouro, 5f);
        }

        /// <summary>PEGAR de maos nuas, TROCAR com luva na mao; nome embaixo, cor + FORMA da raridade no contorno.</summary>
        void OnLoot(string nome, string raridade, bool perto)
        {
            Pegar.gameObject.SetActive(perto);
            Pegar.Rotulo(Logica.Armado ? T_TROCAR : T_PEGAR);
            if (!perto) return;
            Color cor; string forma;
            Raridade(raridade, out cor, out forma);
            Pegar.Subtitulo(nome);
            Pegar.Cor(cor);
            Pegar.Forma(forma);
        }

        void OnArma(IEntidade pawn, string armaId, string nome, string raridade, Elemento[] els)
        {
            if (!EhJogador(pawn)) return;   // arma de bot nao entra no icone do jogador
            string rotulo = Textos.ArmaRotulo(nome, els);
            Logica.Equipar(rotulo, els);
            Color cor; string forma;
            Raridade(raridade, out cor, out forma);
            _armaRotulo.text = rotulo;
            _armaRotulo.color = cor;
            Elemento el = els != null && els.Length > 0 ? els[0] : Carrossel.Selecionado;
            Disparo.Armar(Estilo.CorElemento(el), Elementos.Nome(el));
            Carrossel.Visivel(Logica.CarrosselVisivel);
        }

        void OnDerrubada(IEntidade e, IEntidade causador) { if (EhJogador(e)) Aviso.Derrubar(true); }
        void OnReerguida(IEntidade e, IEntidade por) { if (EhJogador(e)) Aviso.Derrubar(false); if (EhJogador(por)) Aviso.Resgatando = false; }
        void OnDerrubadoProgresso(IEntidade e, float esv, float reer) { if (!EhJogador(e)) Aviso.Resgatando = true; Aviso.Progresso(esv, reer); }

        void OnQuedaFase(string fase) { _altimetroBox.gameObject.SetActive(fase == "caindo" || fase == "planando"); }
        void OnQuedaAltura(float metros, float vel) { _altimetro.text = string.Format(T_ALTITUDE, Mathf.RoundToInt(metros)); }

        // ---------- pausa (GDD §12: Retomar / Configuracoes / Abandonar) ----------
        void AbrirPausa()
        {
            if (_pausaOverlay == null) MontarPausa();
            Pausado = true;
            Time.timeScale = 0f;
            _pausaOverlay.gameObject.SetActive(true);
        }

        public void Retomar()
        {
            Pausado = false;
            Time.timeScale = 1f;
            if (_pausaOverlay != null) _pausaOverlay.gameObject.SetActive(false);
        }

        void MontarPausa()
        {
            _pausaOverlay = Formas.No(_raiz, "Pausa");
            AreaSegura.Esticar(_pausaOverlay);
            var veu = Formas.Imagem(_pausaOverlay, "Veu", null, new Color(0.02f, 0.03f, 0.06f, 0.82f));
            AreaSegura.Esticar(veu.rectTransform);
            veu.raycastTarget = true;
            var col = Estilo.Coluna(_pausaOverlay, "Centro", 14f);
            var titulo = Formas.Texto(col.transform, "Titulo", T_PAUSA_TITULO, 34f, Estilo.Ouro);
            Estilo.Tamanho(titulo, Dp.Px(300f), Dp.Px(48f));
            var retomar = Estilo.Botao(col.transform, "BtnRetomar", T_RETOMAR, 300f, 52f);
            Estilo.Tamanho(retomar, Dp.Px(300f), Estilo.AlturaAlvo(52f));
            retomar.onClick.AddListener(Retomar);
            var cfg = Estilo.Botao(col.transform, "BtnPausaConfig", T_CONFIG, 300f, 52f);
            Estilo.Tamanho(cfg, Dp.Px(300f), Estilo.AlturaAlvo(52f));
            cfg.onClick.AddListener(() =>
            {
                var tela = Config.Criar(_pausaOverlay);
                tela.VoltarPedido += () => Destroy(tela.gameObject);
            });
            var sair = Estilo.Botao(col.transform, "BtnAbandonar", T_ABANDONAR, 300f, 52f);
            Estilo.Tamanho(sair, Dp.Px(300f), Estilo.AlturaAlvo(52f));
            sair.onClick.AddListener(() => { Retomar(); AbandonarPedido?.Invoke(); });
        }

        /// <summary>Angulo de tela (rad; 0 = frente, horario) de um ponto do mundo, relativo ao YAW DA CAMERA.</summary>
        float AnguloDe(Vector3 pos)
        {
            var cam = Camera.main;
            Vector3 origem = Jogador != null ? Jogador.Pos : (cam != null ? cam.transform.position : Vector3.zero);
            Vector3 d = pos - origem;
            float yaw = cam != null ? cam.transform.eulerAngles.y * Mathf.Deg2Rad : 0f;
            return Mathf.Atan2(d.x, d.z) - yaw;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Logica.Tick(dt);
            if (Screen.width != (int)_telaAtual.x || Screen.height != (int)_telaAtual.y) Layout();
            // relogio / bots / fps
            _relogio.text = HudLogica.TextoRelogio(_restante, _treino, T_TREINO);
            _bots.text = _treino ? "" : string.Format(T_BOTS, _botsVivos);
            if (_mostraFps) _fps.text = string.Format(T_FPS, Mathf.RoundToInt(1f / Mathf.Max(dt, 0.0001f)));
            // rotulo da arma: pulso -> espera -> apaga
            _armaRotulo.enabled = Logica.ArmaRotuloVisivel;
            _armaRotulo.color = Formas.ComAlfa(_armaRotulo.color, Logica.ArmaRotuloAlfa);
            // hitmarker
            float a = Logica.Hitmarker / HudLogica.HitmarkerS;
            for (int i = 0; i < 4; i++) { _hitmarker[i].enabled = a > 0f; _hitmarker[i].color = new Color(1, 1, 1, 0.85f * a); }
            // escudo piscando
            if (_escudoQuebrou > 0f) { _escudoQuebrou = Mathf.Max(_escudoQuebrou - dt, 0f); PintarEscudo(); }
            // rastro de dano das barras (parado nao toca em nada)
            _hp.Tick(dt); _mana.Tick(dt); _escudo.Tick(dt);
            // tatica/suprema LIDAS do runner (FracTatica/FracSuprema existem para a HUD): a carga da suprema nao e' linear
            // (para no ar, acelera com dano) e nasce VAZIA — so' com a borda do Bus o botao dizia PRONTA no spawn, e o
            // brilho de "pronta" mentiria. Leitura, nao decisao: quem nega o toque continua sendo o KitRunner.
            var kit = _pawn != null ? _pawn.Runner : null;
            if (kit != null && kit.Impl != null)
            {
                Tatica.Logica.Cooldown(kit.TaticaCd, kit.Dados.TaticaCd);
                float carga = kit.SupremaCargaS;
                Suprema.Logica.Cooldown(kit.FracSuprema * carga, carga);
            }
            // kill feed
            var sb = new System.Text.StringBuilder();
            foreach (var ab in Logica.KillFeed) sb.AppendLine(string.Format(T_ABATE, ab.Nome));
            _killFeed.text = sb.ToString();
            // numeros de dano: sobem e somem
            var mortos = new List<HudLogica.Numero>();
            foreach (var kv in _labels)
            {
                float vida = Logica.VidaDoNumero(kv.Key);
                if (vida <= 0f || kv.Value == null) { mortos.Add(kv.Key); continue; }
                kv.Value.color = Formas.ComAlfa(kv.Value.color, vida);
                kv.Value.rectTransform.anchoredPosition += new Vector2(0, Dp.Px(34f) * dt / Mathf.Max((float)Balance.Feedback.NumLifeS, 0.01f));
            }
            foreach (var n in mortos) { if (_labels[n] != null) Destroy(_labels[n].gameObject); _labels.Remove(n); }
        }

        /// <summary>Leitura defensiva do Kits: slug fora do elenco nao quebra o botao.</summary>
        struct KitDefLite
        {
            public string Nome; public bool Implementado;
            public static KitDefLite De(string slug)
            {
                Kits.KitDef k;
                if (!string.IsNullOrEmpty(slug) && Kits.Magos != null && Kits.Magos.TryGetValue(slug, out k) && k != null)
                    return new KitDefLite { Nome = k.Nome, Implementado = k.Implementado };
                return new KitDefLite { Nome = "", Implementado = false };
            }
        }
    }

    /// <summary>Olhar livre: arrastar na metade direita FORA dos botoes gira a camera (os botoes ficam por cima no raycast).</summary>
    public sealed class OlharArrasto : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<Vector2> Delta;
        int _ponteiro = int.MinValue;
        Vector2 _ultimo;
        public void OnPointerDown(PointerEventData e) { if (_ponteiro == int.MinValue) { _ponteiro = e.pointerId; _ultimo = e.position; } }
        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _ponteiro) return;
            Delta?.Invoke(e.position - _ultimo);
            _ultimo = e.position;
        }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId == _ponteiro) _ponteiro = int.MinValue; }
    }
}
