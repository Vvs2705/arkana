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
    /// maos nuas e com luva travada, botao de ataque desarmado, kill feed com o NOME do mago, faixa "ELIMINADO" que pula,
    /// relogio ("TREINO" no treino), numeros de dano com merge e hitmarker. Tick(dt) e' o relogio; nada aqui decide jogo.
    /// </summary>
    public sealed class HudLogica
    {
        /// <summary>Segundos que a confirmacao da arma fica na tela. Subir = poluicao; descer = pisca ilegivel.</summary>
        public const float ArmaRotuloS = 2.5f;
        public const float ArmaFadeS = 0.5f;
        public const float KillFeedS = 4f;
        public const int KillFeedMax = 4;   // a casca tem uma linha fixa por abate: mudar aqui muda as duas
        public const float HitmarkerS = 0.16f;
        /// <summary>Soma que conta como GOLPE GRANDE (cajado de terra, conducao, rajada somada): pula mais e esquenta para o branco.</summary>
        public const float NumeroGrande = 25f;
        /// <summary>O PULO do numero: nasce na escala de pico a cada golpe somado e assenta em 1 em PuloS. KNOB por foto.</summary>
        public const float PuloS = 0.12f, PuloPico = 1.45f, PuloPicoGrande = 1.8f;
        /// <summary>A FAIXA DO ABATE ("ELIMINADO: nome", centro da tela): quanto fica, entrada, saida e o PULO (nasce no pico e
        /// assenta em 1 em EliminadoPuloS). Abate seguido reescreve e pula de novo. KNOB por foto.</summary>
        public const float EliminadoS = 1.8f, EliminadoEntraS = 0.06f, EliminadoSaiS = 0.4f, EliminadoPuloS = 0.2f, EliminadoPico = 1.6f;

        /// <summary>`Golpe` = Agora do ultimo dano somado (o pulo recomeca nele).</summary>
        public sealed class Numero { public float Total; public float Nasceu; public float Ate; public float Golpe; public bool EmEscudo; public Elemento Elemento; }
        public sealed class Abate { public string Nome; public float Ate; }

        public string ArmaRotulo { get; private set; } = "";
        public bool Armado { get; private set; }
        public bool CarrosselVisivel { get; private set; }
        public float Hitmarker { get; private set; }
        public readonly List<Abate> KillFeed = new List<Abate>();
        /// <summary>Abates da partida inteira: o feed esquece em KillFeedS, a tela de fim nao.</summary>
        public int Abates { get; private set; }
        public float Agora { get; private set; }
        /// <summary>Quem o jogador acabou de eliminar ("" = ninguem): a faixa central le' daqui.</summary>
        public string Eliminado { get; private set; } = "";

        float _armaT = -1f;   // < 0 = sem rotulo na tela
        float _eliminadoDesde = -99f;
        readonly Dictionary<int, Numero> _numeros = new Dictionary<int, Numero>();
        readonly List<int> _mortos = new List<int>();   // reusadas no Tick: zero lixo por quadro
        readonly Predicate<Abate> _expirou;
        readonly float _mergeS, _vidaS;

        public HudLogica(float numMergeS, float numLifeS) { _mergeS = numMergeS; _vidaS = numLifeS; _expirou = a => Agora >= a.Ate; }

        /// <summary>Partida nova: maos nuas, sem elemento, sem rotulo, sem feed.</summary>
        public void MaosNuas()
        {
            Armado = false;
            CarrosselVisivel = false;
            ArmaRotulo = "";
            _armaT = -1f;
            KillFeed.Clear();
            Abates = 0;
            _numeros.Clear();
            Hitmarker = 0f;
            Eliminado = "";
            _eliminadoDesde = -99f;
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
            while (KillFeed.Count > KillFeedMax) KillFeed.RemoveAt(0);
            Abates++;
            Eliminado = nomeDoMago ?? "";
            _eliminadoDesde = Agora;
        }

        public bool EliminadoVisivel => Agora - _eliminadoDesde < EliminadoS;

        /// <summary>Entra num piscar, le' inteira e esvaece nos ultimos EliminadoSaiS.</summary>
        public float EliminadoAlfa
        {
            get
            {
                float i = Agora - _eliminadoDesde;
                if (i >= EliminadoS) return 0f;
                return Mathf.Min(Mathf.Clamp01(i / EliminadoEntraS), Mathf.Clamp01((EliminadoS - i) / EliminadoSaiS));
            }
        }

        /// <summary>O CARIMBO: nasce em EliminadoPico e assenta em 1 (saida quadratica, a mesma do numero de dano).</summary>
        public float EliminadoEscala
        {
            get
            {
                float f = Mathf.Clamp01((Agora - _eliminadoDesde) / EliminadoPuloS);
                return 1f + (EliminadoPico - 1f) * (1f - f) * (1f - f);
            }
        }

        /// <summary>Colocacao final: vencer = 1; cair com N bots de pe' = N + 1 (quem ainda esta' vivo ficou na frente).</summary>
        public static int Colocacao(bool vitoria, int botsVivos) => vitoria ? 1 : Mathf.Max(botsVivos, 0) + 1;

        public void Acertei() { Hitmarker = HitmarkerS; }

        /// <summary>Numero de dano por alvo: dentro de num_merge_s SOMA no mesmo numero (a manopla empilhava escada).</summary>
        public Numero RegistrarDano(int alvoId, float dano, Elemento el, bool emEscudo, out bool novo)
        {
            Numero n;
            if (_numeros.TryGetValue(alvoId, out n) && Agora < n.Ate)
            {
                n.Total += dano;
                n.Ate = Agora + _mergeS;
                n.Golpe = Agora;
                n.EmEscudo = emEscudo;
                n.Elemento = el;
                novo = false;
                return n;
            }
            n = new Numero { Total = dano, Nasceu = Agora, Ate = Agora + _mergeS, Golpe = Agora, EmEscudo = emEscudo, Elemento = el };
            _numeros[alvoId] = n;
            novo = true;
            return n;
        }

        /// <summary>0..1 de vida do numero (1 = acabou de nascer, 0 = morreu).</summary>
        public float VidaDoNumero(Numero n) => Mathf.Clamp01(1f - (Agora - n.Nasceu) / Mathf.Max(_vidaS, 0.01f));
        public int NumerosVivos => _numeros.Count;

        public static bool Grande(Numero n) => n.Total >= NumeroGrande;

        /// <summary>Escala do numero em `agora`: o pico no golpe, assenta em 1 em PuloS (saida quadratica: estala e pousa).</summary>
        public static float Pulo(Numero n, float agora)
        {
            float f = Mathf.Clamp01((agora - n.Golpe) / PuloS);
            float pico = Grande(n) ? PuloPicoGrande : PuloPico;
            return 1f + (pico - 1f) * (1f - f) * (1f - f);
        }

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
            KillFeed.RemoveAll(_expirou);
            _mortos.Clear();
            foreach (var kv in _numeros) if (VidaDoNumero(kv.Value) <= 0f) _mortos.Add(kv.Key);
            foreach (int k in _mortos) _numeros.Remove(k);
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
        public const string T_ELIMINADO = "<color=#FF6B5C>ELIMINADO:</color> {0}";   // o rotulo no vermelho do X do kill feed, o nome em branco
        public const string T_COLOCACAO = "COLOCAÇÃO", T_COLOCACAO_NUM = "#{0}", T_ABATES = "ABATES";   // idem (Textos.cs e' de outra frente hoje)
        public static IReadOnlyDictionary<string, string> Estados => Textos.HudEstados;
        static readonly Color CorZona = new Color(0.55f, 0.35f, 1f);
        // veredito: vermelho-escuro que ainda le' em 46dp sobre o miolo (~3,5:1; o #8B0000 "de verdade" some no preto);
        // a vitoria e' o Estilo.Ouro
        static readonly Color CorDerrota = new Color(0.78f, 0.17f, 0.15f);
        static readonly Color CorPerigo = new Color(1f, 0.52f, 0.46f);   // rotulo do ABANDONAR: a unica acao que custa a partida
        // a PLACA (fio + miolo) e' a das barras: relogio, kill feed, altimetro, carrossel e avisos falam o mesmo idioma.
        // Fio/icone sao propriedade (nao static readonly): a paleta do Estilo passa pelo ColorUtility, e inicializador
        // estatico de MonoBehaviour pode rodar dentro do AddComponent.
        static Color CorFio => Formas.ComAlfa(Estilo.OuroFosco, 0.95f);
        static Color CorIcone => Formas.ComAlfa(Estilo.Ouro, 0.95f);
        static readonly Color CorMiolo = new Color(0.03f, 0.04f, 0.07f, 0.8f);
        static readonly Color CorAbate = new Color(1f, 0.42f, 0.36f);

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
        Text _relogio, _bots, _fps, _armaRotulo, _altimetro;
        RectTransform _topo, _killFeed, _altimetroBox;
        Image _placaTopo;
        GameObject _iconeRelogio, _linhaBots;
        int _topoSeg = int.MinValue, _topoBots = int.MinValue;   // o que a placa do topo mostra: refaz so' quando muda
        AbateLinha[] _abates;
        HudAviso _aviso;
        Image _reticulo;
        Image[] _hitmarker;
        RectTransform _numeros;
        readonly Dictionary<HudLogica.Numero, Text> _labels = new Dictionary<HudLogica.Numero, Text>();
        readonly Stack<Text> _numerosLivres = new Stack<Text>();   // rotulo de dano que morreu volta aqui (nada de Destroy por golpe)
        readonly List<HudLogica.Numero> _numerosMortos = new List<HudLogica.Numero>();
        /// <summary>dp que o numero sobe na vida inteira (freando) e a fracao FINAL da vida em que ele some.</summary>
        const float NumSobeDp = 34f, NumSomeFrac = 0.4f;
        RectTransform _fim, _fimPlaca, _fimChipColocacao, _fimChipAbates;
        CanvasGroup _fimGrupo;
        Image _fimFio, _fimBrilho, _fimLosango;
        Text _fimTexto, _fimColocacao, _fimAbates;
        float _fimT = 1f;   // 0 -> 1 durante a entrada da placa; 1 = pousada
        bool _fimVitoria;
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
        // faixa do abate e vinheta do caido
        RectTransform _eliminado;
        CanvasGroup _eliminadoGrupo;
        Text _eliminadoTexto;
        string _eliminadoNome;   // o nome que a placa mediu por ultimo: remede so' quando muda
        Image _vinhetaCaido;
        static Sprite _spriteVinheta;
        /// <summary>dp: a faixa do abate ACIMA da mira (entre ela e a faixa de aviso do topo) e a altura da placa.</summary>
        const float EliminadoYDp = 86f, EliminadoAlturaDp = 36f;
        /// <summary>VINHETA DO CAIDO: alfa das bordas com o esvaecimento cheio -> quase apagado (a luz indo embora engrossa a
        /// borda) e o pulso em Hz. Sutil de proposito: a mira e o painel de DERRUBADO continuam lendo. KNOB por foto.</summary>
        const float VinhetaCaidoMin = 0.22f, VinhetaCaidoMax = 0.45f, VinhetaCaidoHz = 0.9f;   // 0,4-0,72 tomava a tela (foto 29)
        static readonly Color CorVinhetaCaido = new Color(0.78f, 0.05f, 0.04f);

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
            // vinheta do CAIDO: borda vermelha macia que pulsa e engrossa com o esvaecimento. Por BAIXO de tudo (controles e
            // textos por cima) e na tela CHEIA: e' a borda do vidro, nao a area segura.
            _vinhetaCaido = Formas.Imagem(_raiz, "VinhetaCaido", SpriteVinheta(), CorVinhetaCaido);
            AreaSegura.Esticar(_vinhetaCaido.rectTransform);
            _vinhetaCaido.enabled = false;

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
            // kill feed: uma linha fixa por abate (placa pequena + X + nome), nada de StringBuilder por frame
            _killFeed = Formas.No(_raiz, "KillFeed");
            _abates = new AbateLinha[HudLogica.KillFeedMax];
            for (int i = 0; i < _abates.Length; i++) _abates[i] = new AbateLinha(_killFeed, i);

            MontarTopo();

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
            // TELA CHEIA, nao area segura: o numero ancora no ponto de viewport da camera (o recuo do entalhe o deslocava)
            _numeros = Formas.No(_raiz, "Numeros");
            _numeros.anchorMin = Vector2.zero; _numeros.anchorMax = Vector2.one;
            _numeros.offsetMin = Vector2.zero; _numeros.offsetMax = Vector2.zero;
            MontarEliminado();   // depois dos numeros: o carimbo do abate fica por cima do "37" que o matou

            // altimetro (queda): a placa, seta de queda a esquerda e os metros em negrito (era retangulo chapado na foto 07)
            _altimetroBox = Formas.No(_raiz, "Altimetro");
            AreaSegura.Esticar(Placa(_altimetroBox, "Fundo", Dp.Px(8f)).rectTransform);
            var seta = Formas.Imagem(_altimetroBox, "Seta", Formas.Seta(), CorIcone);
            Fixar(seta.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(Dp.Px(17f), 0f), Vector2.one * Dp.Px(14f));
            seta.rectTransform.localRotation = Quaternion.Euler(0, 0, 180f);   // aponta para o chao
            _altimetro = Formas.Texto(_altimetroBox, "Texto", "", 18f, Color.white, TextAnchor.MiddleRight);
            _altimetro.fontStyle = FontStyle.Bold;
            AreaSegura.Esticar(_altimetro.rectTransform);
            _altimetro.rectTransform.offsetMax = new Vector2(-Dp.Px(12f), 0f);
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

        /// <summary>
        /// A PLACA da HUD — o idioma das barras: fio dourado fosco de cantos redondos + miolo escuro recuado 1dp. Devolve
        /// o fio (raiz): filhos novos desenham por cima do miolo; quem quer outra cor de fio pinta o .color (o aviso pinta
        /// na cor da prioridade). Usada pelo relogio, kill feed, altimetro, carrossel e avisos.
        /// </summary>
        internal static Image Placa(Transform pai, string nome, float raioPx)
        {
            float fio = Mathf.Max(Dp.Px(1f), 1f);
            var borda = Formas.Arredondada(pai, nome, CorFio, raioPx);
            var miolo = Formas.Arredondada(borda.transform, "Miolo", CorMiolo, Mathf.Max(raioPx - fio, 0.5f));
            AreaSegura.Esticar(miolo.rectTransform);
            miolo.rectTransform.offsetMin = new Vector2(fio, fio); miolo.rectTransform.offsetMax = new Vector2(-fio, -fio);
            return borda;
        }

        /// <summary>Ancora num PONTO do pai (ancora 0..1) com pivo, posicao e tamanho em px.</summary>
        static void Fixar(RectTransform rt, Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 tam)
        {
            rt.anchorMin = ancora; rt.anchorMax = ancora; rt.pivot = pivo;
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
        }

        const float TopoLinha1Dp = 32f, TopoLinha2Dp = 20f;   // 52dp = a altura do HudLayout.Topo

        /// <summary>
        /// Topo direito: relogio e BOTS numa PLACA (foto de 12/09: texto branco solto sumia no ceu claro). Linha 1: icone de
        /// relogio + tempo grande em negrito; fio; linha 2: icone de gente + BOTS. Encosta no canto e a largura segue o texto
        /// (PintarTopo mede so' quando ele muda). No TREINO vira uma linha so', sem icone.
        /// </summary>
        void MontarTopo()
        {
            var supDir = new Vector2(1f, 1f);
            var supEsq = new Vector2(0f, 1f);
            var meio = new Vector2(0.5f, 0.5f);
            _topo = Formas.No(_raiz, "Topo");
            _placaTopo = Placa(_topo, "Placa", Dp.Px(7f));
            Fixar(_placaTopo.rectTransform, supDir, supDir, Vector2.zero, new Vector2(Dp.Px(100f), Dp.Px(TopoLinha1Dp + TopoLinha2Dp)));
            var placa = _placaTopo.transform;
            // relogio de 14dp: aro + ponteiros nas 12h e 3h (retangulo sem sprite: nitido em qualquer dpi)
            var ir = Formas.No(placa, "IconeRelogio");
            Fixar(ir, supEsq, meio, new Vector2(Dp.Px(15f), -Dp.Px(TopoLinha1Dp * 0.5f)), Vector2.one * Dp.Px(14f));
            AreaSegura.Esticar(Formas.Imagem(ir, "Aro", Formas.Anel(0.72f), CorIcone).rectTransform);
            float haste = Mathf.Max(Dp.Px(1.6f), 1f);
            Fixar(Formas.Imagem(ir, "Minutos", null, CorIcone).rectTransform, meio, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(haste, Dp.Px(4.4f)));
            Fixar(Formas.Imagem(ir, "Horas", null, CorIcone).rectTransform, meio, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(Dp.Px(3.4f), haste));
            _iconeRelogio = ir.gameObject;
            _relogio = Formas.Texto(placa, "Relogio", "0:00", 22f, Color.white, TextAnchor.MiddleLeft);
            _relogio.fontStyle = FontStyle.Bold;
            Fixar(_relogio.rectTransform, supEsq, supEsq, Vector2.zero, new Vector2(Dp.Px(150f), Dp.Px(TopoLinha1Dp)));
            // linha 2 (some no treino): divisor, icone de gente (cabeca + meio disco de ombros), BOTS
            var lb = Formas.No(placa, "LinhaBots");
            AreaSegura.Esticar(lb);
            _linhaBots = lb.gameObject;
            var div = Formas.Imagem(lb, "Divisor", null, Formas.ComAlfa(Estilo.OuroFosco, 0.5f));
            div.rectTransform.anchorMin = supEsq; div.rectTransform.anchorMax = supDir;
            div.rectTransform.offsetMin = new Vector2(Dp.Px(6f), -Dp.Px(TopoLinha1Dp) - Mathf.Max(Dp.Px(1f), 1f));
            div.rectTransform.offsetMax = new Vector2(-Dp.Px(6f), -Dp.Px(TopoLinha1Dp));
            var ib = Formas.No(lb, "IconeBots");
            Fixar(ib, supEsq, meio, new Vector2(Dp.Px(15f), -Dp.Px(TopoLinha1Dp + TopoLinha2Dp * 0.5f)), Vector2.one * Dp.Px(11f));
            Fixar(Formas.Imagem(ib, "Cabeca", Formas.Disco(), CorIcone).rectTransform, meio, meio, new Vector2(0f, Dp.Px(3f)), Vector2.one * Dp.Px(5f));
            var ombros = Formas.Imagem(ib, "Ombros", Formas.Disco(), CorIcone);
            Fixar(ombros.rectTransform, meio, meio, new Vector2(0f, -Dp.Px(5.5f)), Vector2.one * Dp.Px(11f));
            ombros.type = Image.Type.Filled; ombros.fillMethod = Image.FillMethod.Vertical;
            ombros.fillOrigin = (int)Image.OriginVertical.Top; ombros.fillAmount = 0.5f;
            _bots = Formas.Texto(lb, "Bots", "", 13f, new Color(0.86f, 0.9f, 0.97f), TextAnchor.MiddleLeft);
            _bots.fontStyle = FontStyle.Bold;
            Fixar(_bots.rectTransform, supEsq, supEsq, new Vector2(0f, -Dp.Px(TopoLinha1Dp)), new Vector2(Dp.Px(150f), Dp.Px(TopoLinha2Dp)));
            // FPS (config): a esquerda da placa, na altura do relogio (PintarTopo acompanha a largura)
            _fps = Formas.Texto(_topo, "Fps", "", 12f, new Color(0.6f, 1f, 0.7f, 0.85f), TextAnchor.MiddleRight);
            Fixar(_fps.rectTransform, supDir, supDir, new Vector2(-Dp.Px(106f), 0f), new Vector2(Dp.Px(70f), Dp.Px(TopoLinha1Dp)));
            _fps.enabled = false;
        }

        /// <summary>Relogio e BOTS: texto e largura da placa so' mudam quando o SEGUNDO (ou a contagem) muda — era string.Format por frame.</summary>
        void PintarTopo()
        {
            int seg = _treino ? -1 : Mathf.Max(Mathf.CeilToInt(_restante), 0);
            int bots = _treino ? -1 : _botsVivos;
            if (seg == _topoSeg && bots == _topoBots) return;
            _topoSeg = seg; _topoBots = bots;
            _relogio.text = HudLogica.TextoRelogio(_restante, _treino, T_TREINO);
            _bots.text = _treino ? "" : string.Format(T_BOTS, _botsVivos);
            _iconeRelogio.SetActive(!_treino);
            _linhaBots.SetActive(!_treino);
            float x0 = Dp.Px(_treino ? 10f : 28f);   // sem icone, o texto centraliza na placa
            _relogio.rectTransform.anchoredPosition = new Vector2(x0, 0f);
            _bots.rectTransform.anchoredPosition = new Vector2(x0, -Dp.Px(TopoLinha1Dp));
            float w = Mathf.Ceil(x0 + Mathf.Max(_relogio.preferredWidth, _treino ? 0f : _bots.preferredWidth) + Dp.Px(10f));
            _placaTopo.rectTransform.sizeDelta = new Vector2(w, Dp.Px(_treino ? TopoLinha1Dp : TopoLinha1Dp + TopoLinha2Dp));
            _fps.rectTransform.anchoredPosition = new Vector2(-w - Dp.Px(6f), 0f);
        }

        /// <summary>
        /// Uma linha do kill feed: placa pequena, X vermelho e "Nome derrubado". Texto e largura so' sao refeitos quando o
        /// abate da linha muda; por frame so' o alfa (entra em EntraS deslizando da esquerda, esvaece nos ultimos SaiS).
        /// </summary>
        sealed class AbateLinha
        {
            const float AlturaDp = 16f, PassoDp = 18f;   // 4 linhas = 70dp: cabem nos 72dp do HudLayout.KillFeed
            const float EntraS = 0.18f, SaiS = 0.6f;
            readonly Image _placa;
            readonly Text _texto;
            readonly CanvasGroup _grupo;
            readonly float _y;
            HudLogica.Abate _abate;
            float _alfa = -1f, _dx = 1f;   // ultimo valor aplicado (1 = nunca): por frame so' toca se mudou

            public AbateLinha(RectTransform pai, int i)
            {
                var supEsq = new Vector2(0f, 1f);
                _y = -Dp.Px(PassoDp) * i;
                _placa = Placa(pai, "Abate" + i, Dp.Px(5f));
                Fixar(_placa.rectTransform, supEsq, supEsq, new Vector2(0f, _y), new Vector2(Dp.Px(120f), Dp.Px(AlturaDp)));
                _grupo = _placa.gameObject.AddComponent<CanvasGroup>();   // o fade pega placa, X e texto de uma vez
                var xis = Formas.Imagem(_placa.transform, "Xis", Formas.Xis(), CorAbate);
                Fixar(xis.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(Dp.Px(10f), 0f), Vector2.one * Dp.Px(12f));
                _texto = Formas.Texto(_placa.transform, "Texto", "", 11f, new Color(1f, 1f, 1f, 0.95f), TextAnchor.MiddleLeft);
                _texto.fontStyle = FontStyle.Bold;
                AreaSegura.Esticar(_texto.rectTransform);
                _texto.rectTransform.offsetMin = new Vector2(Dp.Px(18f), 0f);
                _placa.gameObject.SetActive(false);
            }

            public void Pintar(HudLogica.Abate ab, float agora, float larguraMax)
            {
                if (ab != _abate)
                {
                    _abate = ab;
                    _placa.gameObject.SetActive(ab != null);
                    if (ab != null)
                    {
                        _texto.text = string.Format(T_ABATE, ab.Nome);
                        float w = Mathf.Min(Mathf.Ceil(Dp.Px(26f) + _texto.preferredWidth), larguraMax);
                        _placa.rectTransform.sizeDelta = new Vector2(w, Dp.Px(AlturaDp));
                    }
                }
                if (ab == null) return;
                float idade = agora - (ab.Ate - HudLogica.KillFeedS);
                float a = Mathf.Min(Mathf.Clamp01(idade / EntraS), Mathf.Clamp01((ab.Ate - agora) / SaiS));
                if (a != _alfa) { _alfa = a; _grupo.alpha = a; }
                float e = 1f - Mathf.Clamp01(idade / EntraS);
                float dx = -Dp.Px(12f) * e * e;
                if (dx != _dx) { _dx = dx; _placa.rectTransform.anchoredPosition = new Vector2(dx, _y); }
            }
        }

        // ---------- faixa do ABATE e vinheta do CAIDO ----------

        /// <summary>
        /// A faixa do ABATE no centro, acima da mira: PLACA da HUD (fio de ouro + miolo escuro) com losangos de ouro nas pontas
        /// (a lingua da faixa de aviso), "ELIMINADO:" no vermelho do X do kill feed e o NOME em branco, e um brilho vermelho
        /// macio atras. Entra como CARIMBO (escala de pico -> 1, HudLogica.EliminadoEscala) e esvaece. Texto e largura so'
        /// sao refeitos quando o nome muda; por quadro, so' escala e alfa (CanvasGroup: placa, texto e brilho de uma vez).
        /// </summary>
        void MontarEliminado()
        {
            var meio = new Vector2(0.5f, 0.5f);
            _eliminado = Formas.No(_raiz, "Eliminado");
            Fixar(_eliminado, meio, meio, new Vector2(0f, Dp.Px(EliminadoYDp)), new Vector2(Dp.Px(200f), Dp.Px(EliminadoAlturaDp)));
            _eliminadoGrupo = _eliminado.gameObject.AddComponent<CanvasGroup>();
            _eliminadoGrupo.blocksRaycasts = false;   // o toque passa para o olhar livre atras
            var brilho = Formas.Imagem(_eliminado, "Brilho", Formas.Sombra(), Formas.ComAlfa(CorAbate, 0.32f));
            AreaSegura.Esticar(brilho.rectTransform);
            brilho.rectTransform.offsetMin = new Vector2(-Dp.Px(46f), -Dp.Px(26f));
            brilho.rectTransform.offsetMax = new Vector2(Dp.Px(46f), Dp.Px(26f));
            var placa = Placa(_eliminado, "Placa", Dp.Px(9f));
            AreaSegura.Esticar(placa.rectTransform);
            for (int i = 0; i < 2; i++)
            {
                var l = Formas.Imagem(placa.transform, "Losango" + i, Formas.Losango(), Estilo.Ouro);
                Fixar(l.rectTransform, new Vector2(i, 0.5f), meio, new Vector2((i == 0 ? 1f : -1f) * Dp.Px(13f), 0f), Vector2.one * Dp.Px(8f));
            }
            _eliminadoTexto = Formas.Texto(placa.transform, "Texto", "", 19f, Color.white);
            _eliminadoTexto.fontStyle = FontStyle.Bold;
            _eliminadoTexto.supportRichText = true;
            _eliminadoTexto.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(1.2f), -Dp.Px(1.2f));   // 1px some a 395 ppi
            AreaSegura.Esticar(_eliminadoTexto.rectTransform);
            _eliminado.gameObject.SetActive(false);
        }

        /// <summary>Nome novo na faixa: texto em MAIUSCULAS e a largura da placa segue o texto (1x por abate).</summary>
        void PintarEliminado()
        {
            _eliminadoNome = Logica.Eliminado;
            _eliminadoTexto.text = string.Format(T_ELIMINADO, _eliminadoNome.ToUpperInvariant());
            _eliminado.sizeDelta = new Vector2(Mathf.Ceil(_eliminadoTexto.preferredWidth + Dp.Px(58f)), Dp.Px(EliminadoAlturaDp));
        }

        /// <summary>
        /// A vinheta 64x64 gerada 1x (zero arquivo): centro VAZIO, borda cheia em superelipse (os cantos pesam mais, como a
        /// lente escurece), smoothstep sem degrau. Branca: a cor vem da Image; esticada na tela 20:9 as laterais ficam mais
        /// largas que o topo — e' onde o olho nao esta' lendo nada.
        /// </summary>
        internal static Sprite SpriteVinheta()
        {
            if (_spriteVinheta != null) return _spriteVinheta;
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "VinhetaCaido", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                    float d = Mathf.Sqrt(Mathf.Sqrt(dx * dx * dx * dx + dy * dy * dy * dy));
                    float a = Mathf.Clamp01((d - 0.78f) / 0.26f);   // borda mais fina: o miolo da tela fica limpo
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return _spriteVinheta = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------- tela de FIM (veredito) ----------
        const float FimL = 500f, FimA = 252f;   // dp da placa grande: cabe nos ~437dp de altura do Poco F4 deitado
        const float FimTituloY = 52f, FimOrnamentoY = 94f, FimChipsY = 108f, FimChipX = 72f;   // dp a partir do topo da placa
        const float FimEntraS = 0.35f;          // a placa POUSA: esvaece e encolhe de 1,12 para 1 (relogio sem escala)

        /// <summary>
        /// Tela de FIM no idioma da HUD (antes: texto solto numa coluna sobre veu chapado). Veu que escurece o jogo, PLACA
        /// grande de fio na cor do veredito; titulo com BRILHO atras (VITORIA em ouro, respirando; DERROTA em vermelho-escuro),
        /// ornamento, colocacao e abates em dois chips e os botoes-placa lado a lado. Cor/numeros sao do MostrarFim.
        /// </summary>
        void MontarFim()
        {
            var meio = new Vector2(0.5f, 0.5f);
            var sup = new Vector2(0.5f, 1f);
            var inf = new Vector2(0.5f, 0f);
            _fim = Formas.No(_raiz, "Fim");
            AreaSegura.Esticar(_fim);
            _fimGrupo = _fim.gameObject.AddComponent<CanvasGroup>();   // a entrada esvaece veu + placa de uma vez
            var veu = Formas.Imagem(_fim, "Veu", null, new Color(0.01f, 0.015f, 0.03f, 0.62f));
            AreaSegura.Esticar(veu.rectTransform);
            veu.raycastTarget = true;   // o toque nao vaza para o joystick/olhar atras
            _fimFio = Placa(_fim, "Placa", Dp.Px(14f));
            _fimPlaca = _fimFio.rectTransform;
            // a altura segue o botao (AlturaAlvo sobe em dpi baixo): o rodape nunca invade os chips
            Fixar(_fimPlaca, meio, meio, Vector2.zero, new Vector2(Dp.Px(FimL), Dp.Px(FimA - 50f) + Estilo.AlturaAlvo(50f)));
            // brilho: a sombra macia (disco que esvaece ate' a borda) esticada atras do titulo, na cor do veredito
            _fimBrilho = Formas.Imagem(_fimPlaca, "Brilho", Formas.Sombra(), Color.white);
            Fixar(_fimBrilho.rectTransform, sup, meio, new Vector2(0f, -Dp.Px(FimTituloY)), new Vector2(Dp.Px(420f), Dp.Px(118f)));
            _fimTexto = Formas.Texto(_fimPlaca, "Titulo", "", 46f, Color.white);
            _fimTexto.fontStyle = FontStyle.Bold;
            _fimTexto.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(1.5f), -Dp.Px(1.5f));   // 1px some num titulo de 46dp
            Fixar(_fimTexto.rectTransform, sup, meio, new Vector2(0f, -Dp.Px(FimTituloY)), new Vector2(Dp.Px(FimL), Dp.Px(64f)));
            _fimLosango = Ornamento(_fimPlaca, -Dp.Px(FimOrnamentoY), Dp.Px(320f));
            _fimColocacao = ChipFim(_fimPlaca, "ChipColocacao", T_COLOCACAO, out _fimChipColocacao);
            _fimAbates = ChipFim(_fimPlaca, "ChipAbates", T_ABATES, out _fimChipAbates);
            // botoes lado a lado no rodape da placa: JOGAR DE NOVO cheio (a acao que o dedo procura) e MENU
            const float bl = 210f, bm = 150f, vao = 14f, meia = (bl + vao + bm) * 0.5f;
            var de_novo = BotaoPlaca(_fimPlaca, "BtnJogarDeNovo", T_JOGAR_DE_NOVO, bl, Estilo.Ouro, true);
            Fixar((RectTransform)de_novo.transform, inf, inf, new Vector2(Dp.Px(bl * 0.5f - meia), Dp.Px(20f)), ((RectTransform)de_novo.transform).sizeDelta);
            de_novo.onClick.AddListener(() => ReiniciarPedido?.Invoke());
            var menu = BotaoPlaca(_fimPlaca, "BtnMenu", T_MENU, bm, Estilo.Texto, false);
            Fixar((RectTransform)menu.transform, inf, inf, new Vector2(Dp.Px(meia - bm * 0.5f), Dp.Px(20f)), ((RectTransform)menu.transform).sizeDelta);
            menu.onClick.AddListener(() => MenuPedido?.Invoke());
            _fim.gameObject.SetActive(false);
        }

        /// <summary>Chip do fim: placa pequena, numero grande em negrito em cima e o rotulo fosco embaixo. Devolve o numero.</summary>
        static Text ChipFim(RectTransform pai, string nome, string rotulo, out RectTransform chip)
        {
            var sup = new Vector2(0.5f, 1f);
            var inf = new Vector2(0.5f, 0f);
            chip = Placa(pai, nome, Dp.Px(8f)).rectTransform;
            Fixar(chip, sup, sup, Vector2.zero, new Vector2(Dp.Px(130f), Dp.Px(56f)));
            var n = Formas.Texto(chip, "Numero", "", 24f, Color.white);
            n.fontStyle = FontStyle.Bold;
            Fixar(n.rectTransform, sup, sup, new Vector2(0f, -Dp.Px(4f)), new Vector2(Dp.Px(130f), Dp.Px(30f)));
            var r = Formas.Texto(chip, "Rotulo", rotulo, 10f, Estilo.TextoFosco);
            r.fontStyle = FontStyle.Bold;
            Fixar(r.rectTransform, inf, inf, new Vector2(0f, Dp.Px(5f)), new Vector2(Dp.Px(130f), Dp.Px(16f)));
            return n;
        }

        /// <summary>Ornamento das placas grandes: fio fosco com um losango no meio. Devolve o losango (quem chama pinta).</summary>
        static Image Ornamento(RectTransform pai, float yPx, float larguraPx)
        {
            var meio = new Vector2(0.5f, 0.5f);
            var fio = Formas.Imagem(pai, "Ornamento", null, Formas.ComAlfa(Estilo.OuroFosco, 0.6f));
            Fixar(fio.rectTransform, new Vector2(0.5f, 1f), meio, new Vector2(0f, yPx), new Vector2(larguraPx, Mathf.Max(Dp.Px(1f), 1f)));
            var l = Formas.Imagem(fio.transform, "Losango", Formas.Losango(), Estilo.Ouro);
            Fixar(l.rectTransform, meio, meio, Vector2.zero, Vector2.one * Dp.Px(9f));
            return l;
        }

        /// <summary>
        /// Botao no idioma das placas (fim e pausa): fio + miolo arredondados, rotulo em negrito, alvo &gt;= 48dp — o no'
        /// INTEIRO pega o dedo. `cheio` = acao principal: miolo na `cor` em degrade (mais claro em cima) e rotulo escuro;
        /// senao miolo escuro e rotulo na `cor`. ColorTint sobre miolo BRANCO: a cor de cada estado E' a cor do miolo; sem
        /// fade (o dedo quer resposta no quadro do toque, e fade de 0,1 s deixava a foto pegar o miolo no meio do caminho).
        /// </summary>
        static Button BotaoPlaca(Transform pai, string nome, string texto, float larguraDp, Color cor, bool cheio)
        {
            var fio = Placa(pai, nome, Dp.Px(9f));
            fio.rectTransform.sizeDelta = new Vector2(Dp.Px(larguraDp), Estilo.AlturaAlvo(50f));
            fio.raycastTarget = true;
            if (cheio) fio.color = cor;
            var miolo = fio.transform.GetChild(0).GetComponent<Image>();   // a Placa nasce com o Miolo de primeiro filho
            if (cheio) miolo.sprite = Formas.Arredondado(true);   // mesma borda de 9-slice: so' ganha o volume
            miolo.color = Color.white;
            var b = fio.gameObject.AddComponent<Button>();
            b.targetGraphic = miolo;
            Color normal = cheio ? cor : CorMiolo;
            var c = b.colors;
            c.normalColor = normal; c.highlightedColor = normal; c.selectedColor = normal;   // toque nao tem hover; solto nao fica aceso
            c.pressedColor = cheio ? Formas.Escurecer(cor, 0.3f) : Color.Lerp(CorMiolo, cor, 0.3f);
            c.fadeDuration = 0f;
            b.colors = c;
            var t = Formas.Texto(fio.transform, "Rotulo", texto, 16f, cheio ? Estilo.NoiteFunda : cor);
            t.fontStyle = FontStyle.Bold;
            AreaSegura.Esticar(t.rectTransform);
            if (cheio) t.GetComponent<Shadow>().enabled = false;   // sombra preta sob letra escura so' borra
            return b;
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
            AreaSegura.NoRect(_topo, l.Topo);
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
            AreaSegura.NoRect(_killFeed, l.KillFeed);
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

        /// <summary>O veredito: cor (fio, titulo, brilho, losango), colocacao (so' fora do treino, que nao tem ranking) e abates.</summary>
        public void MostrarFim(bool vitoria)
        {
            _fimVitoria = vitoria;
            Color cor = vitoria ? Estilo.Ouro : CorDerrota;
            _fimTexto.text = vitoria ? T_VITORIA : T_DERROTA;
            _fimTexto.color = cor;
            _fimFio.color = cor;
            _fimLosango.color = cor;
            // brilho fraco de proposito: titulo e brilho tem a MESMA cor, e brilho forte vira borrao que come o contraste
            _fimBrilho.color = Formas.ComAlfa(cor, vitoria ? 0.34f : 0.18f);
            _fimBrilho.canvasRenderer.SetAlpha(1f);
            bool rank = !_treino;
            _fimChipColocacao.gameObject.SetActive(rank);
            _fimColocacao.text = string.Format(T_COLOCACAO_NUM, HudLogica.Colocacao(vitoria, _botsVivos));
            _fimColocacao.color = vitoria ? Estilo.Ouro : Color.white;
            _fimAbates.text = Logica.Abates.ToString();
            _fimChipColocacao.anchoredPosition = new Vector2(-Dp.Px(FimChipX), -Dp.Px(FimChipsY));
            _fimChipAbates.anchoredPosition = new Vector2(rank ? Dp.Px(FimChipX) : 0f, -Dp.Px(FimChipsY));
            _fimT = 0f;
            PintarFim(0f);   // ja' nasce transparente e grande: o primeiro quadro nao pisca a placa pousada
            _fim.gameObject.SetActive(true);
        }

        /// <summary>Por frame so' com a tela de fim no ar: a entrada (ate' pousar) e, na vitoria, o brilho respirando (alfa
        /// pelo CanvasRenderer: nao refaz malha). Pousada e sem vitoria, nao toca em nada.</summary>
        void PintarFim(float dt)
        {
            if (_fimT < 1f)
            {
                _fimT = Mathf.Min(_fimT + dt / FimEntraS, 1f);
                float e = 1f - (1f - _fimT) * (1f - _fimT);   // chega rapido, pousa devagar
                _fimGrupo.alpha = e;
                float s = Mathf.Lerp(1.12f, 1f, e);
                _fimPlaca.localScale = new Vector3(s, s, 1f);
            }
            if (_fimVitoria) _fimBrilho.canvasRenderer.SetAlpha(0.72f + 0.28f * Mathf.Sin(Time.unscaledTime * 0.9f * 2f * Mathf.PI));
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
        /// <summary>Morto nao esta' mais caido (a costura sai sem EntityReerguida): painel e vinheta saem, a tela de FIM assume.</summary>
        void OnMorreu(IEntidade e) { if (EhJogador(e)) Aviso.Derrubar(false); }
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
            Vector3 vp = cam.WorldToViewportPoint(mundo);
            if (vp.z < 0f) return;   // atras da camera projetaria no lugar errado
            lbl = _numerosLivres.Count > 0 ? _numerosLivres.Pop() : NovoNumero();
            lbl.gameObject.SetActive(true);
            lbl.canvasRenderer.SetAlpha(1f);
            lbl.rectTransform.localScale = Vector3.one;
            // ANCORA no viewport, nao pixel de tela: vale em qualquer resolucao (a foto de 2400x1080 sobre a tela de 640x480
            // do teste jogava o numero no joystick); a subida anda no anchoredPosition a partir daqui
            lbl.rectTransform.anchorMin = lbl.rectTransform.anchorMax = new Vector2(vp.x, vp.y);
            lbl.rectTransform.anchoredPosition = Vector2.zero;
            _labels[n] = lbl;
            PintarNumero(lbl, n);
        }

        /// <summary>Rotulo de dano novo (o pool devolve os velhos): NEGRITO, contorno escuro de 1,4 dp e sombra de 2 dp — o
        /// Shadow de 1 px do Formas.Texto some a 395 ppi, e o numero colorido se perdia no ceu, no fogo e no escudo branco.</summary>
        Text NovoNumero()
        {
            Text lbl = Formas.Texto(_numeros, "Num", "", 18f, Color.white);
            lbl.fontStyle = FontStyle.Bold;
            lbl.rectTransform.anchorMin = Vector2.zero; lbl.rectTransform.anchorMax = Vector2.zero;
            lbl.rectTransform.sizeDelta = new Vector2(Dp.Px(80f), Dp.Px(24f));
            lbl.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(2f), -Dp.Px(2f));   // ANTES do Outline (que tambem e' Shadow)
            var o = lbl.gameObject.AddComponent<Outline>();
            o.effectColor = Formas.ComAlfa(Estilo.NoiteFunda, 0.9f);
            o.effectDistance = new Vector2(Dp.Px(1.4f), -Dp.Px(1.4f));
            return lbl;
        }

        /// <summary>Texto, tamanho (Balance) e cor (elemento; escudo = branco-azulado). O golpe GRANDE esquenta para o branco.</summary>
        void PintarNumero(Text lbl, HudLogica.Numero n)
        {
            float escala = Mathf.Min((float)Balance.Feedback.NumScaleBase + (float)Balance.Feedback.NumScaleGain * n.Total / 25f, (float)Balance.Feedback.NumScaleMax);
            lbl.text = Mathf.RoundToInt(n.Total).ToString();
            lbl.fontSize = Mathf.Max(Mathf.RoundToInt(Dp.Px(18f) * escala), 11);
            Color c = n.EmEscudo ? Formas.Cor(Balance.Feedback.CorEscudo) : Estilo.CorElemento(n.Elemento);
            lbl.color = HudLogica.Grande(n) ? Color.Lerp(c, Color.white, 0.35f) : c;
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

        /// <summary>
        /// PAUSA no idioma da HUD (antes: a coluna de botoes do menu sobre um veu quase opaco). O veu ESCURECE o jogo
        /// congelado atras (ele segue visivel: o jogador ve' onde parou); PLACA central com titulo em ouro, ornamento e os
        /// botoes-placa empilhados: RETOMAR cheio (a acao que o dedo procura), CONFIGURACOES, e ABANDONAR com rotulo vermelho
        /// (a unica que custa a partida). Abre NA HORA, sem animacao: pausa e' reflexo. Nasce na primeira pausa.
        /// </summary>
        void MontarPausa()
        {
            var meio = new Vector2(0.5f, 0.5f);
            var sup = new Vector2(0.5f, 1f);
            const float L = 340f, bl = 280f;
            _pausaOverlay = Formas.No(_raiz, "Pausa");
            AreaSegura.Esticar(_pausaOverlay);
            var veu = Formas.Imagem(_pausaOverlay, "Veu", null, new Color(0.01f, 0.015f, 0.03f, 0.66f));
            AreaSegura.Esticar(veu.rectTransform);
            veu.raycastTarget = true;
            var placa = Placa(_pausaOverlay, "Painel", Dp.Px(14f)).rectTransform;
            var titulo = Formas.Texto(placa, "Titulo", T_PAUSA_TITULO, 30f, Estilo.Ouro);
            titulo.fontStyle = FontStyle.Bold;
            Fixar(titulo.rectTransform, sup, meio, new Vector2(0f, -Dp.Px(34f)), new Vector2(Dp.Px(L), Dp.Px(44f)));
            Ornamento(placa, -Dp.Px(62f), Dp.Px(200f));
            float y = Dp.Px(78f);   // topo do proximo botao, medido do topo da placa (a altura real vem do AlturaAlvo)
            Button Empilhar(string nome, string texto, Color cor, bool cheio)
            {
                var b = BotaoPlaca(placa, nome, texto, bl, cor, cheio);
                var rt = (RectTransform)b.transform;
                Fixar(rt, sup, sup, new Vector2(0f, -y), rt.sizeDelta);
                y += rt.sizeDelta.y + Dp.Px(10f);
                return b;
            }
            Empilhar("BtnRetomar", T_RETOMAR, Estilo.Ouro, true).onClick.AddListener(Retomar);
            Empilhar("BtnPausaConfig", T_CONFIG, Estilo.Texto, false).onClick.AddListener(() =>
            {
                var tela = Config.Criar(_pausaOverlay);
                tela.VoltarPedido += () => Destroy(tela.gameObject);
            });
            Empilhar("BtnAbandonar", T_ABANDONAR, CorPerigo, false).onClick.AddListener(() => { Retomar(); AbandonarPedido?.Invoke(); });
            Fixar(placa, meio, meio, Vector2.zero, new Vector2(Dp.Px(L), y - Dp.Px(10f) + Dp.Px(18f)));
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
            PintarTopo();
            if (_fim.gameObject.activeSelf) PintarFim(dt);
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
            // kill feed: linhas fixas, o texto so' e' refeito quando o abate da linha muda (era um StringBuilder por frame)
            float feedMax = _killFeed.sizeDelta.x;
            for (int i = 0; i < _abates.Length; i++) _abates[i].Pintar(i < Logica.KillFeed.Count ? Logica.KillFeed[i] : null, Logica.Agora, feedMax);
            // faixa do abate: o carimbo (escala) e o alfa so' enquanto ela esta' no ar
            bool elim = Logica.EliminadoVisivel;
            if (_eliminado.gameObject.activeSelf != elim) _eliminado.gameObject.SetActive(elim);
            if (elim)
            {
                if (_eliminadoNome != Logica.Eliminado) PintarEliminado();
                float s = Logica.EliminadoEscala;
                _eliminado.localScale = new Vector3(s, s, 1f);
                _eliminadoGrupo.alpha = Logica.EliminadoAlfa;
            }
            // vinheta do caido: pulsa devagar e engrossa conforme a luz esvaece (alfa pelo CanvasRenderer: nao refaz malha)
            bool caido = Aviso.Caido;
            if (_vinhetaCaido.enabled != caido) _vinhetaCaido.enabled = caido;
            if (caido)
            {
                float bate = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * VinhetaCaidoHz * 2f * Mathf.PI);
                _vinhetaCaido.canvasRenderer.SetAlpha(Mathf.Lerp(VinhetaCaidoMin, VinhetaCaidoMax, 1f - Aviso.Esvaecimento) * (0.8f + 0.2f * bate));
            }
            // numeros de dano: PULAM a cada golpe (HudLogica.Pulo), sobem FREANDO (rapido no nascimento, param no fim) e so'
            // somem na fracao final. Alfa pelo CanvasRenderer e pulo pela escala: nao refaz a malha do texto + contorno por quadro.
            float vidaS = Mathf.Max((float)Balance.Feedback.NumLifeS, 0.01f);
            _numerosMortos.Clear();
            foreach (var kv in _labels)
            {
                float vida = Logica.VidaDoNumero(kv.Key);
                Text lbl = kv.Value;
                if (vida <= 0f || lbl == null) { _numerosMortos.Add(kv.Key); continue; }
                float pulo = HudLogica.Pulo(kv.Key, Logica.Agora);
                lbl.rectTransform.localScale = new Vector3(pulo, pulo, 1f);
                lbl.canvasRenderer.SetAlpha(Mathf.Clamp01(vida / NumSomeFrac));
                lbl.rectTransform.anchoredPosition += new Vector2(0f, Dp.Px(NumSobeDp) * 2f * vida * dt / vidaS);   // integral de 2*vida = 1
            }
            foreach (var n in _numerosMortos)
            {
                Text lbl = _labels[n];
                _labels.Remove(n);
                if (lbl == null) continue;
                lbl.gameObject.SetActive(false);
                _numerosLivres.Push(lbl);
            }
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
