using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Menu;

namespace Arkana.UI
{
    /// <summary>
    /// A CRONICA DA PARTIDA (GDD §18.6, Selo do Campeao): os numeros do cartao do fim, contados pelo Bus e ZERADOS no
    /// MatchStarted. Pura (sem cena): abates e dano do JOGADOR (so' em inimigo), Sintonias da DUPLA dele (a das duplas
    /// inimigas nao entra), os elementos que ele disparou (sem repetir, na ordem do 1o tiro), o tempo vivo (Tick; para na
    /// morte dele ou no veredito) e, NA HORA do veredito (Fechar), a colocacao por time e o parceiro. Observa, nunca decide;
    /// nada fica gravado nem sai do aparelho (Juramento §19.1). Assina o Bus no construtor; Desligar solta.
    /// </summary>
    public sealed class CronicaDaPartida
    {
        /// <summary>O jogador desta HUD (a Hud escreve no Vincular): a Sintonia "da dupla" e' a do time dele.</summary>
        public IEntidade Jogador;
        public int Abates { get; private set; }
        public float Dano { get; private set; }
        public int Sintonias { get; private set; }
        /// <summary>Os elementos que o jogador disparou (Bus.SpellCast), sem repetir, na ordem do 1o tiro de cada.</summary>
        public readonly List<Elemento> Usados = new List<Elemento>();
        public float TempoVivoS { get; private set; }
        /// <summary>Fechados no veredito: a colocacao, quantos TIMES a partida teve (0 = sem ranking) e o parceiro (null = solo).</summary>
        public int Colocacao { get; private set; }
        public int Times { get; private set; }
        public IEntidade Parceiro { get; private set; }
        public bool Dupla => Parceiro != null;

        bool _contando;
        readonly HashSet<int> _todos = new HashSet<int>(), _vivos = new HashSet<int>();

        public CronicaDaPartida()
        {
            Bus.MatchStarted += Zerar;
            Bus.MatchOver += AoAcabar;
            Bus.EntityDied += AoMorrer;
            Bus.PlayerKilledBot += AoAbater;
            Bus.DamageApplied += AoDanar;
            Bus.SpellCast += AoConjurar;
            Bus.SintoniaDisparou += AoSintonizar;
        }

        public void Desligar()
        {
            Bus.MatchStarted -= Zerar;
            Bus.MatchOver -= AoAcabar;
            Bus.EntityDied -= AoMorrer;
            Bus.PlayerKilledBot -= AoAbater;
            Bus.DamageApplied -= AoDanar;
            Bus.SpellCast -= AoConjurar;
            Bus.SintoniaDisparou -= AoSintonizar;
        }

        /// <summary>Partida nova: tudo a zero e o relogio do tempo vivo andando.</summary>
        public void Zerar()
        {
            Abates = 0; Dano = 0f; Sintonias = 0; TempoVivoS = 0f;
            Usados.Clear();
            Colocacao = 0; Times = 0; Parceiro = null;
            _contando = true;
        }

        /// <summary>O relogio do jogo (dt COM escala: a pausa nao conta).</summary>
        public void Tick(float dt) { if (_contando && dt > 0f) TempoVivoS += dt; }

        // os handlers do Bus nao lancam: excecao aqui cortaria os ouvintes seguintes
        void AoAcabar(bool vitoria) { _contando = false; }
        void AoMorrer(IEntidade e) { if (e != null && e.EhPlayer) _contando = false; }
        void AoAbater(string nome) { Abates++; }
        void AoDanar(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool emEscudo)
        {
            if (fonte != null && fonte.EhPlayer && alvo != null && !Combat.MesmoTime(alvo, fonte)) Dano += dano;
        }
        void AoConjurar(Elemento e) { if (!Usados.Contains(e)) Usados.Add(e); }
        void AoSintonizar(DisparoSintonia d) { if (DaDupla(d.A) || DaDupla(d.B)) Sintonias++; }
        bool DaDupla(IEntidade e) => e != null && (e.EhPlayer || Combat.MesmoTime(e, Jogador));

        /// <summary>
        /// O veredito, lido da ARENA naquele instante: vencer = #1; senao #(times de OUTRO lado com alguem vivo + 1) — o time
        /// do jogador nunca conta contra ele (o fim por tempo o pega vivo). Times = todos os que entraram (vivos ou nao):
        /// 7 duplas na dupla; no solo cada mago e' um time (Combat.TimeDe) e da' os 13 de sempre. Arena null = sem ranking.
        /// </summary>
        public void Fechar(bool vitoria, IList<IEntidade> arena)
        {
            _contando = false;
            Parceiro = null;
            _todos.Clear(); _vivos.Clear();
            int meu = Combat.TimeDe(Jogador);
            if (arena != null)
                for (int i = 0; i < arena.Count; i++)
                {
                    IEntidade e = arena[i];
                    if (e == null) continue;
                    int t = Combat.TimeDe(e);
                    _todos.Add(t);
                    if (t == meu) { if (e != Jogador && Parceiro == null) Parceiro = e; }
                    else if (e.Vital != null && e.Vital.Viva) _vivos.Add(t);
                }
            Times = _todos.Count;
            Colocacao = HudLogica.Colocacao(vitoria, _vivos.Count);
        }

        /// <summary>"#4 DE 7 DUPLAS" / "#4 DE 13 MAGOS"; "" sem ranking. `corDoNumero` (#RRGGBB) pinta so' o "#4".</summary>
        public string TextoColocacao(string corDoNumero = null)
        {
            if (Times <= 0) return "";
            string n = string.Format(Textos.SeloPosicao, Colocacao);
            if (corDoNumero != null) n = "<color=" + corDoNumero + ">" + n + "</color>";
            return string.Format(Dupla ? Textos.SeloColocacaoDupla : Textos.SeloColocacaoSolo, n, Times);
        }

        public static string Titulo(bool vitoria, bool dupla) => vitoria ? (dupla ? Textos.SeloCampeoes : Textos.SeloCampeao) : Textos.HudDerrota;

        /// <summary>A linha pequena sobre o titulo: de onde o veredito veio.</summary>
        public static string Chamada(bool vitoria, bool dupla, bool treino) =>
            treino ? Textos.MenuTreino : vitoria ? (dupla ? Textos.SeloUltimaDupla : Textos.SeloUltimoMago) : Textos.SeloFimDaPartida;

        /// <summary>"4:05" (segundos inteiros, para baixo: 4:05,9 ainda e' 4:05).</summary>
        public static string TextoTempo(float s)
        {
            int t = Mathf.Max(Mathf.FloorToInt(s), 0);
            return string.Format(Textos.SeloTempo, t / 60, t % 60);
        }

        /// <summary>"1.235": inteiro com ponto de milhar (PT-BR) sem depender da cultura do aparelho.</summary>
        public static string TextoDano(float d) => Mathf.RoundToInt(Mathf.Max(d, 0f)).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');
    }

    /// <summary>
    /// O CARTAO DO CAMPEAO (GDD §18.6) — a tela de fim que da' vontade de mostrar para alguem: enquadrado e legivel para que a
    /// captura de tela do celular JA' seja o card (compartilhar de verdade fica para depois: FileProvider no manifesto).
    /// A placa do botao de menu (PlacaBotao, 9-slice de borda grossa) em tres colunas: o RETRATO do mago (busto, moldura de
    /// ouro) com o nome e "COM parceiro"; a CRONICA (de onde veio, titulo, colocacao por time, abates · dano · Sintonias ·
    /// tempo vivo, e as 5 gemas com as usadas acesas); e o SELO DE ARKANA com a marca embaixo. Vitoria: o Selo e' CARIMBADO
    /// (desce grande e girado, bate, solta a onda de brilho e treme o cartao) e a aura do JOGAR respira em volta. Derrota: o
    /// mesmo cartao sobrio, prata, com o selo APAGADO no lugar (o que faltou). Nada aqui da' vantagem (Juramento §19.1).
    /// CANVAS PROPRIO acima de todos (Ordem) e, no ar, o canvas da HUD da partida DESLIGADO: barras, topo, bussola, minimapa,
    /// feed, faixas, marcas de alvo e a HUD da dupla moram todos nele — nada de combate aparece por cima nem atraves do
    /// cartao (foto 56 da integracao: a marca do parceiro atras do DANO, o ELIMINADO atras da colocacao).
    /// </summary>
    public sealed class SeloDoCampeao
    {
        /// <summary>dp do cartao, do vao ate' os botoes e da altura deles: cabe nos ~437 dp do Poco F4 deitado.</summary>
        public const float CartaoL = 800f, CartaoA = 272f, VaoDp = 16f, BotaoDp = 52f, AlturaDp = CartaoA + VaoDp + BotaoDp;
        /// <summary>Respiro minimo ate' a borda da area segura; tela menor ENCOLHE o conjunto (nunca corta).</summary>
        public const float FolgaDp = 12f;
        /// <summary>KNOB por foto: a borda do 9-slice da placa (a do botao e' 16 dp: o cartao grande pede moldura mais grossa).</summary>
        public const float BordaDp = 26f;
        /// <summary>O conjunto POUSA: esvaece e encolhe de 1,06 para 1 (relogio sem escala).</summary>
        public const float EntraS = 0.35f;
        /// <summary>O CARIMBO. KNOB por foto: espera o cartao pousar, desce, bate, a onda e o tremor (s); escala e giro no
        /// comeco e no fim; quanto afunda na batida; o tremor do cartao (dp).</summary>
        public const float CarimboAtrasoS = 0.3f, DesceS = 0.22f, BateS = 0.14f, OndaS = 0.6f, TremorS = 0.28f;
        public const float CarimboPico = 2.6f, GiroInicio = -34f, GiroFim = -12f, Afunda = 0.1f, TremorDp = 5f;
        /// <summary>Colunas (dp a partir do canto superior esquerdo do cartao): retrato, cronica e selo.</summary>
        const float RetratoX = 124f, TextoX = 435f, TextoL = 360f, SeloX = 706f, SeloY = 100f, SeloDp = 172f;
        const int SeloPx = 384;   // ~420 px na tela do Poco F4: o assado basta sem mipmap
        /// <summary>Acima de TODO canvas do jogo: menu 0, HUD 10 (Hud.OrdemDoCanvas), carregamento 50.</summary>
        public const int Ordem = 60;
        /// <summary>O veu que apaga o mundo. O uGUI mistura em espaco LINEAR: 0,76 deixava ~52% do brilho aos olhos (a foto
        /// 56 parecia sem veu); 0,9 deixa ~35%. O cartao e' o quadro, o jogo atras e' so' contexto. KNOB por foto.</summary>
        public const float VeuAlfa = 0.9f;
        static readonly Color CorSeloApagado = new Color(0.62f, 0.62f, 0.68f, 0.16f);
        static readonly Color OuroDesce = new Color(0.86f, 0.72f, 0.45f, 1f), PrataDesce = new Color(0.78f, 0.8f, 0.88f, 1f);
        static Sprite _spriteSelo;

        public readonly Button JogarDeNovo, Voltar;
        /// <summary>Foto: &gt;= 0 congela o relogio do cartao nesse instante (s). Negativo = o relogio.</summary>
        public float Congelar = -1f;
        public bool Visivel => _raiz.gameObject.activeSelf;

        readonly Canvas _partida;
        readonly RectTransform _raiz, _conteudo, _cartao;
        readonly CanvasGroup _grupo;
        readonly Image _aura, _moldura, _brilho, _onda, _selo;
        readonly RawImage _foto;
        readonly Text _nome, _parceiro, _chamada, _titulo, _colocacao;
        readonly Letreiro _tituloLetreiro;
        readonly Text[] _num = new Text[4], _rot = new Text[4];
        readonly Image[] _div = new Image[3], _aros, _formas;
        readonly Logo _marca;
        float _t, _escala = 1f;
        bool _vitoria;

        /// <summary>`partida` = o canvas da HUD de combate, que o cartao apaga enquanto esta' no ar.</summary>
        public SeloDoCampeao(Canvas partida, string jogarDeNovo, string menu)
        {
            var meio = new Vector2(0.5f, 0.5f);
            var sup = new Vector2(0.5f, 1f);
            var inf = new Vector2(0.5f, 0f);
            _partida = partida;
            _raiz = (RectTransform)Formas.CanvasTelaCheia("Fim", Ordem).transform;
            _grupo = _raiz.gameObject.AddComponent<CanvasGroup>();   // a entrada esvaece veu + cartao de uma vez
            var veu = Formas.Imagem(_raiz, "Veu", null, new Color(0.01f, 0.015f, 0.03f, VeuAlfa));
            AreaSegura.Esticar(veu.rectTransform);
            veu.raycastTarget = true;   // o toque nao vaza para o joystick/olhar atras
            _conteudo = Formas.No(_raiz, "Conteudo");
            Hud.Fixar(_conteudo, meio, meio, Vector2.zero, new Vector2(Dp.Px(CartaoL), Dp.Px(CartaoA + VaoDp) + Estilo.AlturaAlvo(BotaoDp)));

            // o cartao: um FUNDO escuro solido (o miolo da placa e' translucido, feito para o botao sobre o mundo — no cartao o
            // mundo vazava atraves dele), recuado meia borda para ficar dentro do chanfro; por cima a placa do botao de menu
            // com a borda do 9-slice mais grossa; a aura (so' na vitoria) e' transparente por dentro
            _cartao = Formas.No(_conteudo, "Cartao");
            Hud.Fixar(_cartao, sup, sup, Vector2.zero, new Vector2(Dp.Px(CartaoL), Dp.Px(CartaoA)));
            var fundo = Formas.Imagem(_cartao, "Fundo", null, Formas.ComAlfa(Estilo.NoiteFunda, 0.94f));
            AreaSegura.Esticar(fundo.rectTransform);
            float recuo = Dp.Px(BordaDp * 0.5f);
            fundo.rectTransform.offsetMin = Vector2.one * recuo; fundo.rectTransform.offsetMax = -Vector2.one * recuo;
            var placa = Formas.Imagem(_cartao, "Placa", null, Color.white);
            PlacaBotao.Vestir(placa, PlacaBotao.Tipo.Escura);
            placa.pixelsPerUnitMultiplier = PlacaBotao.Borda / Mathf.Max(Dp.Px(BordaDp), 0.5f);
            AreaSegura.Esticar(placa.rectTransform);
            _aura = Formas.Imagem(_cartao, "Aura", null, Color.white);
            PlacaBotao.Vestir(_aura, PlacaBotao.Tipo.Aura);
            _aura.pixelsPerUnitMultiplier = placa.pixelsPerUnitMultiplier;
            AreaSegura.Esticar(_aura.rectTransform);
            float folga = Dp.Px(BordaDp) * PlacaBotao.Margem / PlacaBotao.Borda;
            _aura.rectTransform.offsetMin = -Vector2.one * folga;
            _aura.rectTransform.offsetMax = Vector2.one * folga;
            // o brilho e a onda do selo POR BAIXO do texto: a onda passa por tras da cronica sem lavar os numeros
            _brilho = Formas.Imagem(_cartao, "BrilhoDoSelo", Formas.Sombra(), Formas.ComAlfa(Estilo.Ouro, 0.55f));
            Em(_brilho.rectTransform, SeloX, SeloY, 260f, 260f);
            _onda = Formas.Imagem(_cartao, "Onda", Formas.Anel(0.93f), Estilo.OuroClaro);
            Em(_onda.rectTransform, SeloX, SeloY, 150f, 150f);

            // (1) o RETRATO: busto do Resources/Retratos na moldura, degrade no pe', nome e o parceiro embaixo
            _moldura = Formas.Arredondada(_cartao, "Retrato", Estilo.OuroFosco, Dp.Px(8f));
            Em(_moldura.rectTransform, RetratoX, 118f, 196f, 196f);
            _foto = new GameObject("Foto", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _foto.transform.SetParent(_moldura.transform, false);
            _foto.raycastTarget = false;
            AreaSegura.Esticar(_foto.rectTransform);
            float filete = Dp.Px(3f);
            _foto.rectTransform.offsetMin = new Vector2(filete, filete); _foto.rectTransform.offsetMax = -_foto.rectTransform.offsetMin;
            var pe = Formas.Imagem(_foto.transform, "Sombra", Formas.Degrade(false), Formas.ComAlfa(Estilo.NoiteFunda, 0.8f));
            pe.rectTransform.anchorMin = Vector2.zero; pe.rectTransform.anchorMax = new Vector2(1f, 0.32f);
            pe.rectTransform.offsetMin = Vector2.zero; pe.rectTransform.offsetMax = Vector2.zero;
            _nome = Rotulo(_cartao, "Nome", 20f, Estilo.OuroClaro, 0.12f, false);
            Em(_nome.rectTransform, RetratoX, 236f, 210f, 26f);
            _parceiro = Rotulo(_cartao, "Parceiro", 11f, Estilo.TextoFosco, 0.2f, false);
            Em(_parceiro.rectTransform, RetratoX, 257f, 210f, 16f);

            // (2) a CRONICA: chamada, titulo, colocacao, ornamento, os numeros e as gemas
            _chamada = Rotulo(_cartao, "Chamada", 12f, Estilo.Ouro, 0.3f, false);
            Em(_chamada.rectTransform, TextoX, 30f, TextoL, 18f);
            _titulo = Rotulo(_cartao, "Titulo", 30f, Estilo.Ouro, 0.05f, true);
            _tituloLetreiro = _titulo.GetComponent<Letreiro>();
            Em(_titulo.rectTransform, TextoX, 62f, TextoL, 42f);
            _colocacao = Rotulo(_cartao, "Colocacao", 18f, Estilo.Texto, 0.1f, false);
            _colocacao.supportRichText = true;
            Em(_colocacao.rectTransform, TextoX, 95f, TextoL, 24f);
            var fio = Formas.Imagem(_cartao, "Ornamento", null, Formas.ComAlfa(Estilo.OuroFosco, 0.6f));
            Em(fio.rectTransform, TextoX, 117f, 240f, 1f);
            var losango = Formas.Imagem(fio.transform, "Losango", Formas.Losango(), Estilo.Ouro);
            Hud.Fixar(losango.rectTransform, meio, meio, Vector2.zero, Vector2.one * Dp.Px(9f));
            string[] rotulos = { Textos.SeloAbates, Textos.SeloDano, Textos.SeloSintonias, Textos.SeloTempoVivo };
            for (int i = 0; i < 4; i++)
            {
                _num[i] = Rotulo(_cartao, "Num" + i, 26f, Color.white, 0.04f, false);
                _rot[i] = Rotulo(_cartao, "Rot" + i, 10f, Estilo.TextoFosco, 0.18f, false);
                _rot[i].text = rotulos[i];
                if (i > 0) _div[i - 1] = Formas.Imagem(_cartao, "Div" + i, null, Formas.ComAlfa(Estilo.OuroFosco, 0.55f));
            }
            var elRotulo = Rotulo(_cartao, "Elementos", 10f, Estilo.TextoFosco, 0.25f, false);
            elRotulo.text = Textos.SeloElementos;
            Em(elRotulo.rectTransform, TextoX, 199f, TextoL, 14f);
            Elemento[] todos = Elementos.Todos;
            _aros = new Image[todos.Length];
            _formas = new Image[todos.Length];
            const float gema = 30f, passo = gema + 12f;
            for (int i = 0; i < todos.Length; i++)
            {
                var disco = Formas.Imagem(_cartao, "Gema" + i, Formas.Disco(), Formas.ComAlfa(Estilo.NoiteFunda, 0.8f));
                Em(disco.rectTransform, TextoX + passo * (i - (todos.Length - 1) * 0.5f), 229f, gema, gema);
                _aros[i] = Formas.Imagem(disco.transform, "Aro", Formas.Anel(0.86f), Color.white);
                AreaSegura.Esticar(_aros[i].rectTransform);
                _formas[i] = Formas.Imagem(disco.transform, "Forma", Formas.DoElemento(todos[i]), Color.white);
                AreaSegura.Esticar(_formas[i].rectTransform);
                _formas[i].rectTransform.offsetMin = Vector2.one * Dp.Px(7f); _formas[i].rectTransform.offsetMax = -Vector2.one * Dp.Px(7f);
            }

            // (3) o SELO, por cima de tudo (desce SOBRE o cartao), e a marca ARKANA embaixo (a captura sempre diz de que jogo e')
            if (_spriteSelo == null) _spriteSelo = Selo.SpriteDe(SeloPx);   // 1x por processo, no carregamento (nao no veredito)
            _selo = Formas.Imagem(_cartao, "Selo", _spriteSelo, Color.white);
            Em(_selo.rectTransform, SeloX, SeloY, SeloDp, SeloDp);
            _marca = Arkana.Menu.Menu.Wordmark(_cartao, 128f, false);
            Em((RectTransform)_marca.transform, SeloX, 222f, 128f, 128f / Logo.Aspecto);

            // os botoes FORA do cartao, embaixo: JOGAR DE NOVO cheio (a acao que o dedo procura) e MENU
            const float bl = 250f, bm = 170f, vao = 16f, meia = (bl + vao + bm) * 0.5f;
            JogarDeNovo = Estilo.Botao(_conteudo, "BtnJogarDeNovo", jogarDeNovo, bl, BotaoDp, 17f, true);
            var rj = (RectTransform)JogarDeNovo.transform;
            Hud.Fixar(rj, inf, inf, new Vector2(Dp.Px(bl * 0.5f - meia), 0f), rj.sizeDelta);
            Voltar = Estilo.Botao(_conteudo, "BtnMenu", menu, bm, BotaoDp, 17f, false);
            var rv = (RectTransform)Voltar.transform;
            Hud.Fixar(rv, inf, inf, new Vector2(Dp.Px(meia - bm * 0.5f), 0f), rv.sizeDelta);
            _raiz.gameObject.SetActive(false);
        }

        /// <summary>O veredito no cartao. `slug` = o mago jogado (retrato e nome); a cronica ja' FECHADA (Fechar).</summary>
        public void Mostrar(bool vitoria, bool treino, CronicaDaPartida c, string slug)
        {
            _vitoria = vitoria;
            _t = 0f;
            bool dupla = c.Dupla;
            // cabecalho: ouro na vitoria, prata na derrota
            _chamada.text = CronicaDaPartida.Chamada(vitoria, dupla, treino);
            _chamada.color = vitoria ? Estilo.Ouro : Estilo.TextoFosco;
            Caber(_titulo, CronicaDaPartida.Titulo(vitoria, dupla), 30f, TextoL, _tituloLetreiro.Espaco);
            _titulo.color = vitoria ? Estilo.Ouro : Estilo.Texto;
            _tituloLetreiro.Baixo = vitoria ? OuroDesce : PrataDesce;
            _titulo.SetVerticesDirty();
            _colocacao.text = treino ? "" : c.TextoColocacao(Hex(vitoria ? Estilo.Ouro : Color.white));
            _colocacao.color = vitoria ? Estilo.Texto : Estilo.TextoFosco;
            // o retrato (o mesmo busto da selecao; sem arquivo, o quadrado da cor do mago)
            Sprite sp = SelecaoPersonagem.Retrato(slug);
            _foto.texture = sp != null ? sp.texture : null;
            _foto.uvRect = sp != null ? SelecaoPersonagem.Busto(sp.texture.width, sp.texture.height) : new Rect(0f, 0f, 1f, 1f);
            _foto.color = sp != null ? Color.white : SelecaoPersonagem.CorDoMago(slug);
            if (!vitoria) _foto.color *= new Color(0.8f, 0.8f, 0.85f, 1f);   // sobrio: um tom abaixo
            _moldura.color = vitoria ? Estilo.Ouro : Estilo.OuroFosco;
            Caber(_nome, Elenco.Nome(slug).ToUpperInvariant(), 20f, 210f, 0.12f);
            _nome.color = vitoria ? Estilo.OuroClaro : Estilo.Texto;
            _parceiro.text = dupla ? string.Format(Textos.SeloComParceiro, (c.Parceiro.Nome ?? "").ToUpperInvariant()) : "";
            // os numeros: SINTONIAS so' existe com parceiro (3 colunas no solo)
            _num[0].text = c.Abates.ToString();
            _num[1].text = CronicaDaPartida.TextoDano(c.Dano);
            _num[2].text = c.Sintonias.ToString();
            _num[3].text = CronicaDaPartida.TextoTempo(c.TempoVivoS);
            int n = dupla ? 4 : 3, k = 0;
            float w = TextoL / n;
            for (int i = 0; i < 4; i++)
            {
                bool mostra = i != 2 || dupla;
                _num[i].gameObject.SetActive(mostra);
                _rot[i].gameObject.SetActive(mostra);
                if (!mostra) continue;
                _num[i].color = vitoria ? Estilo.OuroClaro : Color.white;
                float cx = TextoX - TextoL * 0.5f + w * (k + 0.5f);
                Em(_num[i].rectTransform, cx, 146f, w, 30f);
                Em(_rot[i].rectTransform, cx, 170f, w, 14f);
                if (k > 0) Em(_div[k - 1].rectTransform, cx - w * 0.5f, 156f, 1f, 40f);
                k++;
            }
            _div[2].gameObject.SetActive(dupla);
            // as gemas: a usada acende na cor do elemento (daltonismo incluso), a outra fica cinza
            Elemento[] todos = Elementos.Todos;
            for (int i = 0; i < todos.Length; i++)
            {
                bool usou = c.Usados.Contains(todos[i]);
                Color cor = Estilo.CorElemento(todos[i]);
                _aros[i].color = usou ? cor : Formas.ComAlfa(Estilo.TextoFosco, 0.22f);
                _formas[i].color = usou ? cor : Formas.ComAlfa(Estilo.TextoFosco, 0.3f);
            }
            // o selo: carimbado na vitoria, apagado (parado, sem brilho) na derrota
            _selo.color = vitoria ? Color.white : CorSeloApagado;
            _selo.rectTransform.localScale = Vector3.one;
            _selo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, GiroFim);
            _brilho.gameObject.SetActive(vitoria);
            _onda.gameObject.SetActive(vitoria);
            _aura.gameObject.SetActive(vitoria);
            _marca.Brilho = vitoria;
            _marca.SetVerticesDirty();
            _cartao.anchoredPosition = Vector2.zero;
            if (_partida != null) _partida.enabled = false;   // a HUD de combate inteira sai (tudo dela mora nesse canvas)
            _raiz.gameObject.SetActive(true);
            Pintar(0f);   // ja' nasce transparente e grande, o selo no alto: o primeiro quadro nao pisca nada pousado
        }

        public void Esconder()
        {
            _raiz.gameObject.SetActive(false);
            if (_partida != null) _partida.enabled = true;
        }

        /// <summary>O canvas do cartao e' raiz propria: morre com a HUD (Hud.OnDestroy).</summary>
        public void Destruir() { if (_raiz != null) Object.Destroy(_raiz.gameObject); }

        /// <summary>A tela mudou (rotacao, foto): o conjunto no centro da area segura, encolhido se nao couber — ja' neste
        /// quadro (a foto troca o alvo da camera e renderiza em seguida, sem Update no meio).</summary>
        public void Layout(Vector2 tela, Margens m)
        {
            _escala = Escala(tela, m, Dp.Px(1f));
            _conteudo.anchoredPosition = new Vector2((m.Esq - m.Dir) * 0.5f, (m.Baixo - m.Topo) * 0.5f);
            if (Visivel) Pintar(0f);
        }

        /// <summary>Conta pura: a escala que faz cartao + botoes + folga caberem na area segura (1 = tamanho de projeto).</summary>
        public static float Escala(Vector2 tela, Margens m, float px)
        {
            float w = (tela.x - m.Esq - m.Dir) / (px * (CartaoL + 2f * FolgaDp));
            float h = (tela.y - m.Topo - m.Baixo) / (px * (AlturaDp + 2f * FolgaDp));
            return Mathf.Clamp(Mathf.Min(w, h), 0.1f, 1f);
        }

        /// <summary>
        /// Conta pura do CARIMBO em `t` (s desde o Mostrar): depois do atraso o selo DESCE (grande, transparente e girado ->
        /// no lugar, ACELERANDO: cai, nao flutua), BATE (afunda e volta) e solta a ONDA (0..1; &lt; 0 = sem onda) e o TREMOR
        /// do cartao (-1..1, some em TremorS).
        /// </summary>
        public static void Carimbo(float t, out float escala, out float alfa, out float giro, out float onda, out float tremor)
        {
            float d = t - CarimboAtrasoS;
            onda = -1f; tremor = 0f;
            if (d < DesceS)
            {
                float f = Mathf.Clamp01(d / DesceS), e = f * f;
                escala = Mathf.Lerp(CarimboPico, 1f, e);
                alfa = Mathf.Clamp01(f / 0.35f);
                giro = Mathf.Lerp(GiroInicio, GiroFim, e);
                return;
            }
            float b = d - DesceS;   // desde a batida
            escala = 1f - Afunda * Mathf.Sin(Mathf.PI * Mathf.Clamp01(b / BateS));
            alfa = 1f;
            giro = GiroFim;
            if (b < OndaS) onda = b / OndaS;
            float k = Mathf.Clamp01(1f - b / TremorS);
            tremor = k * k * Mathf.Sin(b * 70f);
        }

        /// <summary>Por quadro com o cartao no ar (dt SEM escala): a entrada e, na vitoria, o carimbo, a onda, o brilho do selo
        /// e a aura (alfa pelo CanvasRenderer: nao refaz malha).</summary>
        public void Pintar(float dt)
        {
            _t += dt;
            float t = Congelar >= 0f ? Congelar : _t;
            float e = Mathf.Clamp01(t / EntraS);
            e = 1f - (1f - e) * (1f - e);   // chega rapido, pousa devagar
            _grupo.alpha = e;
            float s = _escala * Mathf.Lerp(1.06f, 1f, e);
            _conteudo.localScale = new Vector3(s, s, 1f);
            if (!_vitoria) return;
            float escala, alfa, giro, onda, tremor;
            Carimbo(t, out escala, out alfa, out giro, out onda, out tremor);
            RectTransform rs = _selo.rectTransform;
            rs.localScale = new Vector3(escala, escala, 1f);
            rs.localRotation = Quaternion.Euler(0f, 0f, giro);
            _selo.canvasRenderer.SetAlpha(alfa);
            _onda.enabled = onda >= 0f;
            if (onda >= 0f)
            {
                float o = 1f - (1f - onda) * (1f - onda), so = Mathf.Lerp(0.7f, 2.2f, o);
                _onda.rectTransform.localScale = new Vector3(so, so, 1f);
                _onda.canvasRenderer.SetAlpha(0.9f * (1f - onda) * (1f - onda));
            }
            float b = t - CarimboAtrasoS - DesceS;   // desde a batida: o brilho estoura e assenta respirando; a aura acende junto
            float respira = 0.5f + 0.5f * Mathf.Sin(t * 0.9f * 2f * Mathf.PI);
            _brilho.canvasRenderer.SetAlpha(b < 0f ? 0f : Mathf.Lerp(1f, 0.4f + 0.15f * respira, Mathf.Clamp01(b / 0.5f)));
            _aura.canvasRenderer.SetAlpha(Mathf.Lerp(BotaoMenu.AuraMin, BotaoMenu.AuraMax, respira) * Mathf.Clamp01(b / 0.3f));
            _cartao.anchoredPosition = new Vector2(tremor * Dp.Px(TremorDp), 0f);
        }

        /// <summary>Texto do cartao no idioma do botao de menu: negrito, ESPACADO (Letreiro — antes do contorno: o Outline copia
        /// a malha ja' espacada) e contorno escuro de 1,2 dp. `degrade` = o ouro descendo dentro de cada letra.</summary>
        static Text Rotulo(Transform pai, string nome, float tamDp, Color cor, float espaco, bool degrade)
        {
            var t = Formas.Texto(pai, nome, "", tamDp, cor);
            t.fontStyle = FontStyle.Bold;
            t.GetComponent<Shadow>().enabled = false;
            var l = t.gameObject.AddComponent<Letreiro>();
            l.Espaco = espaco;
            if (!degrade) l.Baixo = new Color(1f, 1f, 1f, 0f);
            Arkana.Menu.Menu.Contorno(t);
            return t;
        }

        /// <summary>O texto no tamanho de projeto, encolhendo ate' caber em `larguraDp` com o espacamento (titulo no plural,
        /// nome de mago comprido).</summary>
        static void Caber(Text t, string texto, float tamDp, float larguraDp, float espaco)
        {
            t.text = texto;
            t.fontSize = Mathf.Max(Mathf.RoundToInt(Dp.Px(tamDp)), 8);
            float max = Dp.Px(larguraDp) / (1f + espaco);
            while (t.fontSize > 8 && t.preferredWidth > max) t.fontSize--;
        }

        /// <summary>No cartao: centro (cx, cy) em dp a partir do canto superior esquerdo, tamanho em dp.</summary>
        static void Em(RectTransform rt, float cx, float cy, float w, float h) =>
            Hud.Fixar(rt, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(Dp.Px(cx), -Dp.Px(cy)), new Vector2(Mathf.Max(Dp.Px(w), 1f), Dp.Px(h)));

        /// <summary>#RRGGBB sem o ColorUtility (o nativo nao roda no teste fora do Unity).</summary>
        static string Hex(Color c)
        {
            Color32 k = c;
            return "#" + k.r.ToString("X2") + k.g.ToString("X2") + k.b.ToString("X2");
        }
    }
}
