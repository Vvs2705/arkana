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
    /// sobrevive a troca de cena, e quem consome zera) e pede a partida do mesmo jeito. Colado no JOGAR, o seletor MODO:
    /// DUPLA | SOLO (ModoDupla, lembrado no PlayerPrefs).
    /// </summary>
    public sealed class Menu : MonoBehaviour
    {
        public const string T_MARCA = Textos.Marca, T_SUB = Textos.TituloSub, T_TOQUE = Textos.TituloToque;
        public const string T_JOGAR = Textos.MenuJogar, T_TREINO = Textos.MenuTreino, T_CONFIG = Textos.MenuConfig, T_SAIR = Textos.MenuSair;
        public const string T_ELENCO = "ELENCO";   // pedido do coordenador (Textos so' tem "PERSONAGENS"); ponytail: unificar no Core

        /// <summary>O treino e' a MESMA cena da partida com um pedido diferente. A cena le e ZERA ao consumir.</summary>
        public static bool PedidoDeTreino;

        /// <summary>
        /// O MODO da partida (contrato 17G): DUPLA = o jogador + um PARCEIRO bot contra 6 duplas (a Sintonia, GDD §9, so' existe
        /// com aliado); SOLO = o FFA de sempre. O Main le' no toque do JOGAR (o treino e' sempre sozinho). Lido do PlayerPrefs
        /// no boot do menu; o seletor escreve e GRAVA; teste escreve direto (nao grava).
        /// </summary>
        public static bool ModoDupla = ModoPadrao;
        /// <summary>PADRAO DUPLA: decisao do coordenador (onda 17), VETAVEL pelo Diretor — trocar aqui volta o jogo ao solo.</summary>
        public const bool ModoPadrao = true;
        public const string PrefModo = "partida.modo_dupla";
        public const string T_MODO = Textos.ModoRotulo, T_DUPLA = Textos.ModoDupla, T_SOLO = Textos.ModoSolo;

        public event Action JogarPedido;   // alem do Bus, para a cena que quiser fiacao direta

        Canvas _canvas;
        RectTransform _raiz;
        RectTransform _titulo, _menu, _elenco, _config;
        RectTransform _atual;
        Text _tap;
        CanvasGroup _modoDupla, _modoSolo;
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
            ModoDupla = PlayerPrefs.GetInt(PrefModo, ModoPadrao ? 1 : 0) != 0;
            // FUNDO TRANSLUCIDO: atras do Titulo, do menu principal e do Elenco passa o 3D (VitrineDoMenu, o mago no pico).
            // Continua bloqueando o toque. O Elenco nao pinta fundo (painel a esquerda, o mago escolhido a direita); so' as
            // Configuracoes pintam o PROPRIO Estilo.Fundo opaco por cima (muito texto) — nao ha' o que trocar no Ir. O degrade
            // de baixo segura a leitura do "toque para comecar" sobre a neve.
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
            // LEITURA SOBRE A NEVE (foto 01-menu de 12/09: o subtitulo cinza e o "toque" sumiam no branco do pico): um halo
            // escuro MACIO atras da coluna e outro atras do "toque" (Formas.Sombra esvaece ate' a borda: sem retangulo, e o
            // mago no terco direito fica de fora) + contorno escuro no texto. KNOB: alfas e tamanhos, por foto.
            var halo = Formas.Imagem(t, "SombraCentro", Formas.Sombra(), Formas.ComAlfa(Estilo.NoiteFunda, 0.5f));
            halo.rectTransform.sizeDelta = new Vector2(Dp.Px(420f), Dp.Px(360f));   // 0,28-0,72 da tela no 20:9: o mago (0,64+) quase nao escurece
            halo.rectTransform.anchoredPosition = new Vector2(0, -Dp.Px(10f));   // centrado entre a wordmark e o subtitulo
            var col = Estilo.Coluna(t, "Centro", 6f);
            ((RectTransform)col.transform).anchoredPosition = new Vector2(0, Dp.Px(20f));
            var selo = Formas.Imagem(col.transform, "Selo", Selo.SpriteDe(256), Color.white);
            Estilo.Tamanho(selo, Dp.Px(150f), Dp.Px(150f));
            Wordmark(col.transform, 300f, true);
            var sub = Formas.Texto(col.transform, "Sub", T_SUB, 16f, Estilo.Texto);   // Texto, nao TextoFosco: o fosco sumia na neve
            Contorno(sub);
            Estilo.Tamanho(sub, Dp.Px(300f), Dp.Px(22f));
            var haloTap = Formas.Imagem(t, "SombraToque", Formas.Sombra(), Formas.ComAlfa(Estilo.NoiteFunda, 0.55f));
            haloTap.rectTransform.anchorMin = new Vector2(0.5f, 0); haloTap.rectTransform.anchorMax = new Vector2(0.5f, 0);
            haloTap.rectTransform.anchoredPosition = new Vector2(0, Dp.Px(38f)); haloTap.rectTransform.sizeDelta = new Vector2(Dp.Px(380f), Dp.Px(64f));
            _tap = Formas.Texto(t, "Toque", T_TOQUE, 14f, Estilo.Ouro);
            Contorno(_tap);
            _tap.rectTransform.anchorMin = new Vector2(0.5f, 0); _tap.rectTransform.anchorMax = new Vector2(0.5f, 0); _tap.rectTransform.pivot = new Vector2(0.5f, 0);
            _tap.rectTransform.anchoredPosition = new Vector2(0, Dp.Px(28f)); _tap.rectTransform.sizeDelta = new Vector2(Dp.Px(300f), Dp.Px(20f));
            return t;
        }

        /// <summary>Contorno escuro de 1,2 dp: o Shadow de 1 px do Formas.Texto some a 395 ppi; o Outline le' sobre a neve e
        /// sobre a noite. Acompanha o alfa do texto (useGraphicAlpha): o "toque" pulsa com contorno e tudo.
        /// A tela de carregamento usa o mesmo (o nome do mago sobre o ceu do pico).</summary>
        internal static void Contorno(Text t)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = Formas.ComAlfa(Estilo.NoiteFunda, 0.9f);
            o.effectDistance = new Vector2(Dp.Px(1.2f), -Dp.Px(1.2f));
        }

        /// <summary>A marca ARKANA: o Logo (letras proprias desenhadas em codigo; o K em raio, GDD §10). `larguraDp` = a largura
        /// da tinta, a altura sai da proporcao; `brilho` = o reflexo correndo. Titulo, menu e a tela de carregamento: a MESMA
        /// marca.</summary>
        internal static Logo Wordmark(Transform pai, float larguraDp, bool brilho)
        {
            var go = new GameObject("Wordmark", typeof(RectTransform), typeof(Logo));
            go.transform.SetParent(pai, false);
            var logo = go.GetComponent<Logo>();
            logo.raycastTarget = false;   // o toque do titulo passa (o TapTitulo cobre a tela por baixo)
            logo.Brilho = brilho;
            Estilo.Tamanho(logo, Dp.Px(larguraDp), Dp.Px(larguraDp / Logo.Aspecto));
            return logo;
        }

        RectTransform MontarMenu()
        {
            var t = Tela("MenuPrincipal");
            var col = Estilo.Coluna(t, "Centro", 12f);
            Wordmark(col.transform, 200f, true);
            // o JOGAR numa LINHA do tamanho dele: o seletor de MODO pendura a' esquerda, FORA da coluna (mais uma fileira
            // passava da altura do Poco F4 deitado, 437 dp)
            var linha = Formas.No(col.transform, "LinhaJogar");
            Estilo.Tamanho(linha, Dp.Px(JogarLarguraDp), Estilo.AlturaAlvo(JogarAlturaDp));
            Botao(linha, "BtnJogar", T_JOGAR, OnJogar, true);
            MontarModo(linha);
            Botao(col.transform, "BtnTreino", T_TREINO, () => { PedidoDeTreino = true; OnJogar(); });
            Botao(col.transform, "BtnElenco", T_ELENCO, () => { if (_elenco == null) _elenco = MontarElenco(); Ir(_elenco); });
            Botao(col.transform, "BtnConfig", T_CONFIG, () => { if (_config == null) _config = MontarConfig(); Ir(_config); });
            Botao(col.transform, "BtnSair", T_SAIR, () => Application.Quit());
            return t;
        }

        /// <summary>KNOB por foto: o JOGAR e' o PRINCIPAL (maior, ouro cheio, aura respirando); os outros, secundarios.</summary>
        const float JogarLarguraDp = 300f, JogarAlturaDp = 66f, JogarFonteDp = 24f;
        const float ItemLarguraDp = 264f, ItemAlturaDp = 52f, ItemFonteDp = 18f;

        Button Botao(Transform pai, string nome, string texto, Action acao, bool principal = false)
        {
            float larg = principal ? JogarLarguraDp : ItemLarguraDp, alt = principal ? JogarAlturaDp : ItemAlturaDp;
            var b = Estilo.Botao(pai, nome, texto, larg, alt, principal ? JogarFonteDp : ItemFonteDp, principal);
            Estilo.Tamanho(b, Dp.Px(larg), Estilo.AlturaAlvo(alt));
            b.onClick.AddListener(() => acao());
            return b;
        }

        /// <summary>KNOB por foto: as duas placas do seletor, o vao entre elas, a distancia ate' o JOGAR e o alfa do apagado.</summary>
        const float ModoLarguraDp = 112f, ModoAlturaDp = 48f, ModoFonteDp = 16f, ModoVaoDp = 8f, ModoAoLadoDp = 16f, ModoApagado = 0.4f;

        /// <summary>
        /// O seletor MODO: rotulo "MODO" em ouro e as duas placas do menu (Estilo.Botao) lado a lado, colado a' esquerda do
        /// JOGAR e na altura dele. O escolhido fica ACESO e o outro APAGADO (alfa): escolha le' de relance, sem cor nova.
        /// </summary>
        void MontarModo(RectTransform linha)
        {
            float w = 2f * Dp.Px(ModoLarguraDp) + Dp.Px(ModoVaoDp), h = Estilo.AlturaAlvo(ModoAlturaDp);
            var grupo = Formas.No(linha, "Modo");
            grupo.anchorMin = grupo.anchorMax = new Vector2(0f, 0.5f);
            grupo.pivot = new Vector2(1f, 0.5f);
            grupo.anchoredPosition = new Vector2(-Dp.Px(ModoAoLadoDp), 0f);
            grupo.sizeDelta = new Vector2(w, Estilo.AlturaAlvo(JogarAlturaDp));
            var rotulo = Formas.Texto(grupo, "RotuloModo", T_MODO, 11f, Estilo.Ouro);
            rotulo.fontStyle = FontStyle.Bold;
            Contorno(rotulo);
            rotulo.rectTransform.anchorMin = new Vector2(0f, 1f); rotulo.rectTransform.anchorMax = Vector2.one;
            rotulo.rectTransform.pivot = new Vector2(0.5f, 1f);
            rotulo.rectTransform.anchoredPosition = Vector2.zero;
            rotulo.rectTransform.sizeDelta = new Vector2(0f, Dp.Px(14f));
            _modoDupla = BotaoModo(grupo, "BtnModoDupla", T_DUPLA, 0f, true);
            _modoSolo = BotaoModo(grupo, "BtnModoSolo", T_SOLO, 1f, false);
            PintarModo();
        }

        CanvasGroup BotaoModo(RectTransform grupo, string nome, string texto, float ancoraX, bool dupla)
        {
            var b = Estilo.Botao(grupo, nome, texto, ModoLarguraDp, ModoAlturaDp, ModoFonteDp);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ancoraX, 0f);
            rt.anchoredPosition = Vector2.zero;
            b.onClick.AddListener(() => EscolherModo(dupla));
            return b.gameObject.AddComponent<CanvasGroup>();
        }

        void EscolherModo(bool dupla)
        {
            ModoDupla = dupla;
            PlayerPrefs.SetInt(PrefModo, dupla ? 1 : 0);
            PlayerPrefs.Save();
            PintarModo();
        }

        void PintarModo()
        {
            if (_modoDupla == null) return;
            _modoDupla.alpha = ModoDupla ? 1f : ModoApagado;
            _modoSolo.alpha = ModoDupla ? ModoApagado : 1f;
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
        public void Rearmar() { _iniciando = false; PintarModo(); }   // o teste pode ter trocado o ModoDupla por fora

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
                _tap.color = Formas.ComAlfa(Estilo.Ouro, 0.6f + 0.35f * Mathf.Sin(_fase * 3f));   // piso 0,25: nunca some de vez
        }
    }
}
