using System;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>
    /// Porta de entrada: Titulo (Selo + wordmark + "toque para comecar") -> Menu (JOGAR / TREINO / ELENCO / CONFIGURACOES)
    /// -> Elenco e Configuracoes. Tudo por codigo. JOGAR emite Bus.GameStartRequested; TREINO liga PedidoDeTreino (estatico:
    /// sobrevive a troca de cena, e quem consome zera) e pede a partida do mesmo jeito.
    /// </summary>
    public sealed class Menu : MonoBehaviour
    {
        public const string T_MARCA = Textos.Marca, T_SUB = Textos.TituloSub, T_TOQUE = Textos.TituloToque;
        public const string T_JOGAR = Textos.MenuJogar, T_TREINO = Textos.MenuTreino, T_CONFIG = Textos.MenuConfig, T_SAIR = Textos.MenuSair;
        public const string T_ELENCO = "ELENCO";   // pedido do coordenador (Textos so' tem "PERSONAGENS"); ponytail: unificar no Core

        /// <summary>O treino e' a MESMA cena da partida com um pedido diferente. A cena le e ZERA ao consumir.</summary>
        public static bool PedidoDeTreino;

        public event Action JogarPedido;   // alem do Bus, para a cena que quiser fiacao direta

        Canvas _canvas;
        RectTransform _raiz;
        RectTransform _titulo, _menu, _elenco, _config;
        RectTransform _atual;
        Text _tap;
        float _fase;
        bool _iniciando;

        public static Menu Criar()
        {
            var canvas = Formas.CanvasTelaCheia("Menu", 0);
            var m = canvas.gameObject.AddComponent<Menu>();
            m._canvas = canvas;
            m.Montar();
            return m;
        }

        void Montar()
        {
            _raiz = (RectTransform)_canvas.transform;
            ConfigLogica.Aplicar(ConfigLogica.Atual);   // settings lidos e aplicados UMA vez, no boot
            // FUNDO TRANSLUCIDO: atras do Titulo e do menu principal passa o 3D (VitrineDoMenu, o mago no pico). Continua
            // bloqueando o toque. Elenco e Configuracoes pintam o PROPRIO Estilo.Fundo opaco por cima (muito texto) — nao ha'
            // o que trocar no Ir. O degrade de baixo segura a leitura do "toque para comecar" sobre a neve.
            // KNOB: os dois alfas, por foto.
            Estilo.Fundo(_raiz).color = Formas.ComAlfa(Estilo.Noite, 0.35f);
            var sombra = Formas.Imagem(_raiz, "SombraBaixo", Formas.Degrade(false), Formas.ComAlfa(Estilo.Noite, 0.55f));
            sombra.rectTransform.anchorMin = Vector2.zero; sombra.rectTransform.anchorMax = new Vector2(1f, 0.4f);
            sombra.rectTransform.offsetMin = Vector2.zero; sombra.rectTransform.offsetMax = Vector2.zero;
            _titulo = MontarTitulo();
            _menu = MontarMenu();
            _elenco = null;
            _config = null;
            _menu.gameObject.SetActive(false);
            _atual = _titulo;
        }

        RectTransform Tela(string nome)
        {
            var t = Formas.No(_raiz, nome);
            AreaSegura.EsticarDentro(t, AreaSegura.Atual(), Dp.Px(Estilo.RespiroDp));
            return t;
        }

        RectTransform MontarTitulo()
        {
            var t = Tela("Titulo");
            var tap = Estilo.BotaoInvisivel(t, "TapTitulo");   // toque em QUALQUER lugar avanca
            tap.onClick.AddListener(() => Ir(_menu));
            var col = Estilo.Coluna(t, "Centro", 6f);
            ((RectTransform)col.transform).anchoredPosition = new Vector2(0, Dp.Px(20f));
            var selo = Formas.Imagem(col.transform, "Selo", Selo.SpriteDe(256), Color.white);
            Estilo.Tamanho(selo, Dp.Px(150f), Dp.Px(150f));
            var marca = Wordmark(col.transform, 44f, 8f);
            Estilo.Tamanho(marca, Dp.Px(300f), Dp.Px(52f));
            var sub = Formas.Texto(col.transform, "Sub", T_SUB, 16f, Estilo.TextoFosco);
            Estilo.Tamanho(sub, Dp.Px(300f), Dp.Px(22f));
            _tap = Formas.Texto(t, "Toque", T_TOQUE, 14f, Estilo.Ouro);
            _tap.rectTransform.anchorMin = new Vector2(0.5f, 0); _tap.rectTransform.anchorMax = new Vector2(0.5f, 0); _tap.rectTransform.pivot = new Vector2(0.5f, 0);
            _tap.rectTransform.anchoredPosition = new Vector2(0, Dp.Px(28f)); _tap.rectTransform.sizeDelta = new Vector2(Dp.Px(300f), Dp.Px(20f));
            return t;
        }

        /// <summary>Wordmark ARKANA: uma letra por Text (espacamento largo), o K na cor do raio (GDD §10).</summary>
        HorizontalLayoutGroup Wordmark(Transform pai, float tamDp, float sepDp)
        {
            var go = new GameObject("Wordmark", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(pai, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = Dp.Px(sepDp); h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            foreach (char ch in T_MARCA)
            {
                var l = Formas.Texto(go.transform, "L" + ch, ch.ToString(), tamDp, ch == 'K' ? Estilo.Raio : Estilo.Ouro);
                Estilo.Tamanho(l, Dp.Px(tamDp * 0.8f), Dp.Px(tamDp * 1.2f));
            }
            return h;
        }

        RectTransform MontarMenu()
        {
            var t = Tela("MenuPrincipal");
            var col = Estilo.Coluna(t, "Centro", 12f);
            var marca = Wordmark(col.transform, 26f, 4f);
            Estilo.Tamanho(marca, Dp.Px(200f), Dp.Px(34f));
            Botao(col.transform, "BtnJogar", T_JOGAR, OnJogar);
            Botao(col.transform, "BtnTreino", T_TREINO, () => { PedidoDeTreino = true; OnJogar(); });
            Botao(col.transform, "BtnElenco", T_ELENCO, () => { if (_elenco == null) _elenco = MontarElenco(); Ir(_elenco); });
            Botao(col.transform, "BtnConfig", T_CONFIG, () => { if (_config == null) _config = MontarConfig(); Ir(_config); });
            Botao(col.transform, "BtnSair", T_SAIR, () => Application.Quit());
            return t;
        }

        Button Botao(Transform pai, string nome, string texto, Action acao)
        {
            var b = Estilo.Botao(pai, nome, texto, 260f, Estilo.BotaoDp, 18f);
            Estilo.Tamanho(b, Dp.Px(260f), Estilo.AlturaAlvo(Estilo.BotaoDp));
            b.onClick.AddListener(() => acao());
            return b;
        }

        RectTransform MontarElenco()
        {
            var t = Formas.No(_raiz, "Elenco");
            AreaSegura.Esticar(t);
            var s = Elenco.Criar(t);
            s.VoltarPedido += () => Ir(_menu);
            return t;
        }

        RectTransform MontarConfig()
        {
            var t = Formas.No(_raiz, "Config");
            AreaSegura.Esticar(t);
            var c = Config.Criar(t);
            c.VoltarPedido += () => Ir(_menu);
            return t;
        }

        void OnJogar()
        {
            if (_iniciando) return;
            _iniciando = true;
            Bus.EmitGameStartRequested();   // contrato oficial: o menu PEDE a partida pelo Bus
            JogarPedido?.Invoke();
        }

        /// <summary>Permite pedir de novo (a cena voltou ao menu sem destruir este objeto).</summary>
        public void Rearmar() { _iniciando = false; }

        void Ir(RectTransform alvo)
        {
            if (alvo == null || alvo == _atual) return;
            if (_atual != null) _atual.gameObject.SetActive(false);
            _atual = alvo;
            alvo.gameObject.SetActive(true);
        }

        void Update()
        {
            _fase += Time.unscaledDeltaTime;
            if (_tap != null && _tap.gameObject.activeInHierarchy)
                _tap.color = Formas.ComAlfa(Estilo.Ouro, 0.5f + 0.4f * Mathf.Sin(_fase * 3f));
        }
    }
}
