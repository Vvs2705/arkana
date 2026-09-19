using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Menu;

namespace Arkana.UI
{
    /// <summary>
    /// Logica PURA da HUD da DUPLA (contrato 17G): o texto do topo (DUPLAS n / BOTS n), a FAIXA DA SINTONIA (canalizando ->
    /// disparou | quebrada, o nome do combo nas duas cores dos elementos), a RECARGA da Sintonia do jogador com o "SINTONIA
    /// PRONTA" na BORDA de ficar pronta, e o "ESPECTANDO · nome". Tick(dt) e' o relogio; nada aqui decide jogo.
    /// </summary>
    public sealed class DuplaHudLogica
    {
        /// <summary>s: quanto a faixa segura o disparo e o "quebrada", a entrada, a saida e o carimbo do disparo (nasce em Pico
        /// e assenta em 1 em PuloS, a lingua da faixa do abate). Canalizacao sem desfecho some em Perdida s alem da duracao.
        /// KNOB por foto.</summary>
        public const float DisparoS = 1f, QuebradaS = 1.5f, EntraS = 0.08f, SaiS = 0.35f, PuloS = 0.2f, Pico = 1.3f, PerdidaS = 1f;
        /// <summary>s que o "SINTONIA PRONTA" fica e o esvaecer do fim.</summary>
        public const float ProntaS = 1.8f, ProntaSaiS = 0.5f;

        public enum Faixa { Nada, Canalizando, Disparou, Quebrada }

        public Faixa Estado { get; private set; }
        public ComboSintonia Combo { get; private set; }
        public float Agora { get; private set; }
        /// <summary>0..1 da recarga da Sintonia do jogador (1 = pronta).</summary>
        public float Recarga { get; private set; } = 1f;
        public bool Pronta { get; private set; } = true;

        float _desde = -99f, _dur = 1f, _prontaDesde = -99f;

        /// <summary>O topo: DUPLAS n (times vivos) na dupla, BOTS n no solo (duplas &lt; 0).</summary>
        public static string TextoTopo(int duplas, int bots) =>
            duplas >= 0 ? string.Format(Textos.HudDuplas, duplas) : string.Format(Textos.HudBots, bots);

        public static string TextoEspectando(string nome) => string.Format(Textos.Espectando, (nome ?? "").ToUpperInvariant());

        /// <summary>A faixa e' da canalizacao do TIME do jogador (a do inimigo, o mundo mostra: VisualDaSintonia).</summary>
        public static bool DaDupla(IEntidade jogador, IEntidade a, IEntidade b) =>
            jogador != null && (Combat.MesmoTime(jogador, a) || Combat.MesmoTime(jogador, b));

        /// <summary>Os dois elementos do combo (ordem do enum), perguntados a' regra da Sintonia: tabela nenhuma duplicada aqui.</summary>
        public static bool ElementosDe(ComboSintonia c, out Elemento a, out Elemento b)
        {
            Elemento[] t = Elementos.Todos;
            for (int i = 0; i < t.Length; i++)
                for (int j = i + 1; j < t.Length; j++)
                {
                    ComboSintonia? k = Sintonia.ComboDe(t[i], t[j]);
                    if (k.HasValue && k.Value == c) { a = t[i]; b = t[j]; return true; }
                }
            a = b = Elemento.Fogo;
            return false;
        }

        /// <summary>Onde o nome parte nas duas cores: no espaco mais perto do meio ("TORNADO | FLAMEJANTE"); palavra unica,
        /// no meio das letras ("ELETRO|CUSSÃO").</summary>
        public static int Corte(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return 0;
            int meio = nome.Length / 2, melhor = -1;
            for (int i = 0; i < nome.Length; i++)
                if (nome[i] == ' ' && (melhor < 0 || Mathf.Abs(i - meio) < Mathf.Abs(melhor - meio))) melhor = i;
            return melhor >= 0 ? melhor : meio;
        }

        /// <summary>Rich text: ate' o Corte na cor `a`, o resto na `b`.</summary>
        public static string Bicolor(string nome, Color a, Color b)
        {
            nome = nome ?? "";
            int k = Corte(nome);
            return "<color=" + Hex(a) + ">" + nome.Substring(0, k) + "</color><color=" + Hex(b) + ">" + nome.Substring(k) + "</color>";
        }

        /// <summary>#RRGGBB sem o ColorUtility (o nativo nao roda no teste fora do Unity).</summary>
        public static string Hex(Color c)
        {
            Color32 k = c;
            return "#" + k.r.ToString("X2") + k.g.ToString("X2") + k.b.ToString("X2");
        }

        public void Canalizar(ComboSintonia c, float dur) { Combo = c; Estado = Faixa.Canalizando; _desde = Agora; _dur = Mathf.Max(dur, 0.01f); }
        public void Disparou(ComboSintonia c) { Combo = c; Estado = Faixa.Disparou; _desde = Agora; }
        public void Falhou(ComboSintonia c) { Combo = c; Estado = Faixa.Quebrada; _desde = Agora; }

        float Idade => Agora - _desde;
        float Segura => Estado == Faixa.Disparou ? DisparoS : Estado == Faixa.Quebrada ? QuebradaS : _dur + PerdidaS;

        /// <summary>0..1 do anel da canalizacao (enche em CanalizacaoS); o disparo o deixa cheio.</summary>
        public float Progresso => Estado == Faixa.Canalizando ? Mathf.Clamp01(Idade / _dur) : Estado == Faixa.Disparou ? 1f : 0f;

        public float Alfa
        {
            get
            {
                if (Estado == Faixa.Nada) return 0f;
                float saida = Estado == Faixa.Canalizando ? 1f : Mathf.Clamp01((Segura - Idade) / SaiS);
                return Mathf.Min(Mathf.Clamp01(Idade / EntraS), saida);
            }
        }

        /// <summary>O CARIMBO do disparo (pico -> 1, saida quadratica); fora do disparo, 1.</summary>
        public float Escala
        {
            get
            {
                if (Estado != Faixa.Disparou) return 1f;
                float f = Mathf.Clamp01(Idade / PuloS);
                return 1f + (Pico - 1f) * (1f - f) * (1f - f);
            }
        }

        /// <summary>A recarga do jogador (Sintonia.CooldownRestante). O PRONTA acende na BORDA de ficar pronta: nascer pronto
        /// (partida nova) nao pisca nada.</summary>
        public void Recarregar(float restante, float total)
        {
            bool pronta = !(restante > 0f);
            Recarga = total > 0f ? 1f - Mathf.Clamp01(restante / total) : 1f;
            if (pronta && !Pronta) _prontaDesde = Agora;
            Pronta = pronta;
        }

        public float ProntaAlfa
        {
            get
            {
                float i = Agora - _prontaDesde;
                if (i >= ProntaS) return 0f;
                return Mathf.Min(Mathf.Clamp01(i / EntraS), Mathf.Clamp01((ProntaS - i) / ProntaSaiS));
            }
        }

        /// <summary>Partida nova: faixa apagada, recarga cheia, nada piscando.</summary>
        public void Zerar()
        {
            Estado = Faixa.Nada;
            Recarga = 1f;
            Pronta = true;
            _prontaDesde = -99f;
        }

        public void Tick(float dt)
        {
            Agora += dt;
            if (Estado != Faixa.Nada && Idade >= Segura) Estado = Faixa.Nada;
        }
    }

    /// <summary>
    /// A casca da HUD da DUPLA, no idioma da HUD (Hud.Placa: fio de ouro + miolo escuro). (1) A FAIXA DA SINTONIA, no topo
    /// abaixo da faixa de aviso e acima da do abate: "SINTONIA" em ouro sobre o NOME DO COMBO nas duas cores, losangos nas
    /// cores dos dois elementos e o trilho que enche na canalizacao; o disparo carimba e esvaece; o falhou vira "SINTONIA
    /// QUEBRADA" em vermelho. So' a canalizacao do time do jogador. (2) O ANEL de recarga em volta do botao de ataque (fora
    /// do anel verde da mira) e o "SINTONIA PRONTA" acima da fileira da esquiva (so' em DUPLA, com o jogador de pe').
    /// (3) A placa ESPECTANDO · nome no rodape central. Tudo transitorio ou fora da coluna da mira (PONTE A11). Assina o Bus
    /// no construtor; Desligar solta.
    /// </summary>
    public sealed class HudDupla
    {
        const float CaptionDp = 10f, NomeDp = 18f, BarraDp = 3f, RaioDp = 9f;
        static readonly Color CorQuebrada = new Color(1f, 0.42f, 0.36f);   // o vermelho do X do kill feed
        static readonly Color CorApagada = new Color(0.55f, 0.55f, 0.6f);

        public readonly DuplaHudLogica Logica = new DuplaHudLogica();
        /// <summary>O jogador desta HUD (o filtro da faixa). A Hud escreve no Vincular.</summary>
        public IEntidade Jogador;

        readonly RectTransform _faixa, _placa, _barraTrilho;
        readonly CanvasGroup _faixaGrupo, _prontaGrupo;
        readonly Image _brilho, _losA, _losB, _barra, _anel, _anelTrilho;
        readonly Text _caption, _nome;
        readonly RectTransform _pronta, _espectando, _espectandoPlaca;
        readonly Text _espectandoTexto;
        DuplaHudLogica.Faixa _estadoPintado = (DuplaHudLogica.Faixa)(-1);
        ComboSintonia _comboPintado;
        IEntidade _espectado;
        float _larguraMax = 1f, _altura = 1f;

        public HudDupla(RectTransform raiz)
        {
            var meio = new Vector2(0.5f, 0.5f);
            // (1) a faixa
            _faixa = Formas.No(raiz, "Sintonia");
            _faixaGrupo = _faixa.gameObject.AddComponent<CanvasGroup>();
            _faixaGrupo.blocksRaycasts = false;
            _faixaGrupo.interactable = false;
            _brilho = Formas.Imagem(_faixa, "Brilho", Formas.Sombra(), Color.white);   // irmao ANTES da placa: desenha por baixo
            _brilho.rectTransform.anchorMin = _brilho.rectTransform.anchorMax = _brilho.rectTransform.pivot = meio;
            var fio = Hud.Placa(_faixa, "Placa", Dp.Px(RaioDp));
            _placa = fio.rectTransform;
            _placa.anchorMin = _placa.anchorMax = _placa.pivot = meio;
            _losA = Losango(_placa, 0f);
            _losB = Losango(_placa, 1f);
            _caption = Formas.Texto(_placa, "Caption", Textos.Sintonia, CaptionDp, Estilo.Ouro);
            _caption.fontStyle = FontStyle.Bold;
            Faixa(_caption.rectTransform, 1f, -Dp.Px(3f), Dp.Px(CaptionDp + 3f));   // 3..16 dp do topo da placa (42 dp)
            _nome = Formas.Texto(_placa, "Nome", "", NomeDp, Color.white);
            _nome.fontStyle = FontStyle.Bold;
            _nome.supportRichText = true;
            _nome.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(1.2f), -Dp.Px(1.2f));   // 1px some a 395 ppi
            Faixa(_nome.rectTransform, 1f, -Dp.Px(CaptionDp + 4f), Dp.Px(NomeDp + 4f));      // 14..36 dp; o trilho mora em 35..38
            _barraTrilho = Formas.No(_placa, "Trilho");
            _barraTrilho.anchorMin = new Vector2(0f, 0f); _barraTrilho.anchorMax = new Vector2(1f, 0f); _barraTrilho.pivot = new Vector2(0.5f, 0f);
            _barraTrilho.offsetMin = new Vector2(Dp.Px(22f), Dp.Px(4f)); _barraTrilho.offsetMax = new Vector2(-Dp.Px(22f), Dp.Px(4f + BarraDp));
            var trilho = Formas.Imagem(_barraTrilho, "Fundo", null, new Color(1f, 1f, 1f, 0.18f));
            AreaSegura.Esticar(trilho.rectTransform);
            _barra = Formas.Imagem(_barraTrilho, "Barra", null, Color.white);
            AreaSegura.Esticar(_barra.rectTransform);
            _faixa.gameObject.SetActive(false);

            // (2) o anel de recarga (o trilho fraco inteiro + o arco que enche) e o PRONTA
            _anelTrilho = Formas.Imagem(raiz, "AnelSintoniaTrilho", Formas.Anel(0.92f), new Color(1f, 1f, 1f, 0.14f));
            _anel = Formas.Imagem(raiz, "AnelSintonia", Formas.Anel(0.92f), Color.white);
            _anel.type = Image.Type.Filled;
            _anel.fillMethod = Image.FillMethod.Radial360;
            _anel.fillOrigin = (int)Image.Origin360.Top;
            _anel.fillClockwise = true;
            var pr = Hud.Placa(raiz, "SintoniaPronta", Dp.Px(7f));
            _pronta = pr.rectTransform;
            _prontaGrupo = pr.gameObject.AddComponent<CanvasGroup>();
            _prontaGrupo.blocksRaycasts = false;
            var prTexto = Formas.Texto(_pronta, "Texto", Textos.SintoniaPronta, 11f, Estilo.Ouro);
            prTexto.fontStyle = FontStyle.Bold;
            AreaSegura.Esticar(prTexto.rectTransform);

            // (3) espectando: a placa no meio do retangulo, na largura do texto
            _espectando = Formas.No(raiz, "Espectando");
            var ep = Hud.Placa(_espectando, "Placa", Dp.Px(9f));
            ep.color = Dupla.CorAliado;
            _espectandoPlaca = ep.rectTransform;
            _espectandoPlaca.anchorMin = _espectandoPlaca.anchorMax = _espectandoPlaca.pivot = meio;
            _espectandoTexto = Formas.Texto(_espectandoPlaca, "Texto", "", 15f, Color.white);
            _espectandoTexto.fontStyle = FontStyle.Bold;
            AreaSegura.Esticar(_espectandoTexto.rectTransform);
            _espectando.gameObject.SetActive(false);
            _anel.enabled = _anelTrilho.enabled = false;
            _pronta.gameObject.SetActive(false);

            Bus.SintoniaCanalizando += AoCanalizar;
            Bus.SintoniaDisparou += AoDisparar;
            Bus.SintoniaFalhou += AoFalhar;
        }

        public void Desligar()
        {
            Bus.SintoniaCanalizando -= AoCanalizar;
            Bus.SintoniaDisparou -= AoDisparar;
            Bus.SintoniaFalhou -= AoFalhar;
        }

        /// <summary>Partida nova: faixa apagada, nada herdado.</summary>
        public void Vincular(IEntidade jogador)
        {
            Jogador = jogador;
            Logica.Zerar();
            _estadoPintado = (DuplaHudLogica.Faixa)(-1);
            _espectado = null;
            _espectando.gameObject.SetActive(false);
        }

        // os handlers do Bus nao lancam: excecao aqui cortaria os ouvintes seguintes
        void AoCanalizar(ComboSintonia c, IEntidade a, IEntidade b, Vector3 p, float dur) { if (DuplaHudLogica.DaDupla(Jogador, a, b)) Logica.Canalizar(c, dur); }
        void AoDisparar(DisparoSintonia d) { if (DuplaHudLogica.DaDupla(Jogador, d.A, d.B)) Logica.Disparou(d.Combo); }
        void AoFalhar(ComboSintonia c, IEntidade a, IEntidade b, Vector3 p) { if (DuplaHudLogica.DaDupla(Jogador, a, b)) Logica.Falhou(c); }

        /// <summary>Os retangulos do HudLayout (px de tela, area segura ja' contada).</summary>
        public void Layout(HudLayout l)
        {
            AreaSegura.NoRect(_faixa, l.Sintonia);
            _larguraMax = l.Sintonia.width;
            _altura = l.Sintonia.height;
            _estadoPintado = (DuplaHudLogica.Faixa)(-1);   // remede a placa na largura nova
            AreaSegura.NoRect(_anelTrilho.rectTransform, l.AnelSintonia);
            AreaSegura.NoRect(_anel.rectTransform, l.AnelSintonia);
            AreaSegura.NoRect(_pronta, l.SintoniaPronta);
            AreaSegura.NoRect(_espectando, l.Espectando);
        }

        /// <summary>Por quadro (dt sem escala). `dupla` = partida em dupla (o anel so' existe nela); `espectado` = o parceiro
        /// que a camera segue com o jogador fora (null = jogando).</summary>
        public void Pintar(float dt, bool dupla, IEntidade espectado)
        {
            Logica.Tick(dt);
            // (1) faixa: texto, cores e largura so' quando o estado/combo muda; por quadro, alfa, escala e o trilho
            bool faixa = Logica.Estado != DuplaHudLogica.Faixa.Nada;
            if (_faixa.gameObject.activeSelf != faixa) _faixa.gameObject.SetActive(faixa);
            if (faixa)
            {
                if (Logica.Estado != _estadoPintado || Logica.Combo != _comboPintado) PintarFaixa();
                _faixaGrupo.alpha = Logica.Alfa;
                float s = Logica.Escala;
                _placa.localScale = new Vector3(s, s, 1f);
                _barra.rectTransform.anchorMax = new Vector2(Logica.Progresso, 1f);
            }
            // (2) anel: a recarga do jogador, so' em dupla e de pe'
            bool vivo = Jogador != null && Jogador.Vital != null && Jogador.Vital.Viva;
            bool anel = dupla && vivo && espectado == null;
            if (anel)
            {
                Logica.Recarregar(Sintonia.CooldownRestante(Jogador), Balance.Sintonia.CooldownS);
                _anel.fillAmount = Logica.Recarga;
                _anel.color = Logica.Pronta ? Formas.ComAlfa(Estilo.Ouro, 0.9f) : new Color(1f, 1f, 1f, 0.6f);
            }
            if (_anel.enabled != anel) { _anel.enabled = anel; _anelTrilho.enabled = anel; }
            float pa = anel ? Logica.ProntaAlfa : 0f;
            if (_pronta.gameObject.activeSelf != pa > 0f) _pronta.gameObject.SetActive(pa > 0f);
            if (pa > 0f) _prontaGrupo.alpha = pa;
            // (3) espectando: o texto so' quando o seguido muda
            if (espectado != _espectado)
            {
                _espectado = espectado;
                _espectando.gameObject.SetActive(espectado != null);
                if (espectado != null)
                {
                    _espectandoTexto.text = DuplaHudLogica.TextoEspectando(espectado.Nome);
                    _espectandoPlaca.sizeDelta = new Vector2(Mathf.Ceil(_espectandoTexto.preferredWidth + Dp.Px(36f)), _espectando.sizeDelta.y);
                }
            }
        }

        /// <summary>Estado novo na faixa: o texto (o nome em duas cores, ou QUEBRADA), os losangos, o brilho e a largura.</summary>
        void PintarFaixa()
        {
            _estadoPintado = Logica.Estado;
            _comboPintado = Logica.Combo;
            Elemento ea, eb;
            DuplaHudLogica.ElementosDe(Logica.Combo, out ea, out eb);
            Color a = Estilo.CorElemento(ea), b = Estilo.CorElemento(eb);
            string nome = Textos.ComboNome(Logica.Combo);
            bool quebrada = Logica.Estado == DuplaHudLogica.Faixa.Quebrada;
            if (quebrada)
            {
                _caption.text = nome;
                _caption.color = Estilo.TextoFosco;
                _nome.text = Textos.SintoniaQuebrada;
                _nome.color = CorQuebrada;
                _losA.color = _losB.color = CorApagada;
                _brilho.color = Formas.ComAlfa(CorQuebrada, 0.22f);
            }
            else
            {
                _caption.text = Textos.Sintonia;
                _caption.color = Estilo.Ouro;
                _nome.text = DuplaHudLogica.Bicolor(nome, a, b);
                _nome.color = Color.white;
                _losA.color = a;
                _losB.color = b;
                _brilho.color = Formas.ComAlfa(Color.Lerp(a, b, 0.5f), 0.3f);
                _barra.color = a;
            }
            _barraTrilho.gameObject.SetActive(!quebrada);
            float w = Mathf.Min(Mathf.Ceil(_nome.preferredWidth + Dp.Px(64f)), _larguraMax);
            w = Mathf.Max(w, Dp.Px(180f));
            _placa.sizeDelta = new Vector2(w, _altura);
            _brilho.rectTransform.sizeDelta = new Vector2(w + Dp.Px(80f), _altura + Dp.Px(44f));
        }

        static Image Losango(RectTransform placa, float ancoraX)
        {
            var l = Formas.Imagem(placa, "Losango" + ancoraX, Formas.Losango(), Estilo.Ouro);
            RectTransform rt = l.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(ancoraX, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((ancoraX < 0.5f ? 1f : -1f) * Dp.Px(12f), 0f);
            rt.sizeDelta = Vector2.one * Dp.Px(9f);
            return l;
        }

        /// <summary>Faixa horizontal no topo do pai: `y` px a partir de cima (negativo desce), `h` px de altura.</summary>
        static void Faixa(RectTransform rt, float ancoraY, float y, float h)
        {
            rt.anchorMin = new Vector2(0f, ancoraY); rt.anchorMax = new Vector2(1f, ancoraY); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, h);
        }
    }
}
