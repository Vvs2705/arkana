using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Menu;

namespace Arkana.UI
{
    /// <summary>
    /// Logica PURA do aviso "PAGINA DO GRIMORIO": fila (duas paginas no mesmo golpe entram uma depois da outra), o relogio
    /// de cada uma (entra, fica, esvaece) e o RETANGULO na tela. Nada aqui decide jogo.
    /// ONDE (PONTE A11): a coluna da mira e' proibida e os botoes tambem. O aviso mora a' ESQUERDA, no vao entre o kill feed
    /// (que desce das barras) e o joystick — medido do proprio HudLayout, entao anda junto se a HUD mudar. Transitorio.
    /// </summary>
    public sealed class AvisoGrimorioLogica
    {
        /// <summary>KNOB por foto/aparelho: entrada (desliza + acende), leitura e saida, em s.</summary>
        public const float EntraS = 0.3f, FicaS = 3.4f, SaiS = 0.6f;
        public const float Total = EntraS + FicaS + SaiS;
        /// <summary>KNOB: a placa em dp (cabe "Retorno Contestavel" a 17 dp ao lado do selo de 44 dp).</summary>
        public const float LarguraDp = 280f, AlturaDp = 64f;

        readonly Queue<string> _fila = new Queue<string>();
        /// <summary>A pagina na tela agora (null = nada).</summary>
        public string Atual { get; private set; }
        /// <summary>s desde que a Atual entrou.</summary>
        public float T { get; private set; }

        public void Enfileirar(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            _fila.Enqueue(id);
            if (Atual == null) Proximo();
        }

        public void Tick(float dt)
        {
            if (Atual == null || !(dt > 0f)) return;
            T += dt;
            if (T >= Total) Proximo();
        }

        void Proximo() { Atual = _fila.Count > 0 ? _fila.Dequeue() : null; T = 0f; }

        /// <summary>0..1: acende na entrada, cheio na leitura, apaga na saida.</summary>
        public float Alfa => Atual == null ? 0f : Mathf.Min(Mathf.Clamp01(T / EntraS), Mathf.Clamp01((Total - T) / SaiS));

        /// <summary>0..1 do deslize de entrada (saida cubica: chega rapido e pousa).</summary>
        public float Entrada { get { float f = Atual == null ? 0f : Mathf.Clamp01(T / EntraS); return 1f - (1f - f) * (1f - f) * (1f - f); } }

        /// <summary>A COLUNA DA MIRA (PONTE A11): o terco do meio da tela, onde a mira e o alvo moram em terceira pessoa.</summary>
        public static Rect ColunaDaMira(Vector2 tela) => new Rect(tela.x / 3f, 0f, tela.x / 3f, tela.y);

        /// <summary>
        /// Px de tela (origem inferior esquerda). Alinhado a' esquerda com o kill feed; centrado no vao entre ele e o
        /// joystick. Tela baixa demais para o vao: encosta em cima do joystick (sobre o kill feed, que tambem e' transitorio).
        /// Tela estreita (16:9): a placa encolhe para parar 8 dp antes da coluna da mira (o nome encolhe junto, bestFit).
        /// </summary>
        public static Rect Retangulo(Vector2 tela, Margens m, float px)
        {
            HudLayout l = HudLayout.DaConfig(tela, m, px);
            float w = Mathf.Min(LarguraDp * px, ColunaDaMira(tela).xMin - 8f * px - l.KillFeed.xMin), h = AlturaDp * px;
            float yc = Mathf.Max((l.Joystick.yMax + l.KillFeed.yMin) * 0.5f, l.Joystick.yMax + 4f * px + h * 0.5f);
            return new Rect(l.KillFeed.xMin, yc - h * 0.5f, w, h);
        }
    }

    /// <summary>
    /// Casca do aviso: placa da HUD com fio de OURO, o selo com o icone da pagina (o MESMO do livro, IconeDaPagina) e um
    /// brilho que nasce e apaga, "PAGINA DO GRIMORIO" em ouro espacado e o nome da pagina em ouro claro. Desliza da
    /// esquerda. Filha do canvas da HUD (nasce e morre com ela; a foto a converte junto). Ticka tambem o relogio do Grimorio.
    /// Posicao por ANCORA no meio da altura (o deslocamento do HudLayout em relacao ao meio quase nao depende da tela). A
    /// medida da tela e' a do canvas, como o Hud.TamanhoDaTela; a foto (que troca o alvo da camera) pinta com Pintar(tela).
    /// </summary>
    public sealed class AvisoGrimorio : MonoBehaviour
    {
        public const string T_AVISO = Textos.GrimorioAviso;
        /// <summary>KNOB por foto: o selo, o deslize de entrada e o pico do brilho.</summary>
        const float SeloDp = 44f, DeslizeDp = 28f, BrilhoPico = 0.8f;

        public AvisoGrimorioLogica Logica { get; private set; }
        public Grimorio Grimorio { get; private set; }

        RectTransform _raiz, _caixa, _selo, _icone;
        CanvasGroup _grupo;
        Image _brilho;
        Text _nome;
        string _mostrando;
        Vector2 _tela;
        Rect _r;

        public static AvisoGrimorio Criar(Transform hud, Grimorio g)
        {
            var go = new GameObject("AvisoGrimorio", typeof(RectTransform), typeof(AvisoGrimorio));
            go.transform.SetParent(hud, false);
            var a = go.GetComponent<AvisoGrimorio>();
            a._raiz = (RectTransform)go.transform;
            AreaSegura.Esticar(a._raiz);
            a.Logica = new AvisoGrimorioLogica();
            a.Grimorio = g;
            if (g != null) g.Acendeu += a.Logica.Enfileirar;
            a.Montar();
            return a;
        }

        void OnDestroy() { if (Grimorio != null && Logica != null) Grimorio.Acendeu -= Logica.Enfileirar; }

        void Montar()
        {
            _caixa = Formas.No(_raiz, "Caixa");
            _caixa.anchorMin = _caixa.anchorMax = _caixa.pivot = new Vector2(0f, 0.5f);
            _grupo = _caixa.gameObject.AddComponent<CanvasGroup>();
            _grupo.blocksRaycasts = false; _grupo.interactable = false;   // aviso nao pega toque: o dedo passa para o jogo
            var placa = Hud.Placa(_caixa, "Placa", Dp.Px(12f));
            placa.color = Formas.ComAlfa(Estilo.Ouro, 0.95f);
            AreaSegura.Esticar(placa.rectTransform);
            float selo = Dp.Px(SeloDp), folga = Dp.Px(10f);
            _selo = Formas.No(_caixa, "Selo");
            _selo.anchorMin = _selo.anchorMax = _selo.pivot = new Vector2(0f, 0.5f);
            _selo.anchoredPosition = new Vector2(folga, 0); _selo.sizeDelta = new Vector2(selo, selo);
            _brilho = Formas.Imagem(_selo, "Brilho", Formas.Halo(), Formas.ComAlfa(Estilo.Ouro, 0f));
            _brilho.rectTransform.sizeDelta = new Vector2(selo * Formas.HaloEscala * 1.3f, selo * Formas.HaloEscala * 1.3f);
            AreaSegura.Esticar(Formas.Imagem(_selo, "Disco", Formas.Disco(), Estilo.NoiteFunda).rectTransform);
            AreaSegura.Esticar(Formas.Imagem(_selo, "Aro", Formas.Anel(0.86f), Estilo.Ouro).rectTransform);
            float x0 = folga + selo + Dp.Px(10f);
            var rotulo = Formas.Texto(_caixa, "Rotulo", T_AVISO, 10.5f, Estilo.Ouro, TextAnchor.LowerLeft);
            rotulo.fontStyle = FontStyle.Bold;
            rotulo.GetComponent<Shadow>().enabled = false;   // o Letreiro vem PRIMEIRO (a regra do Estilo.Botao)
            rotulo.gameObject.AddComponent<Letreiro>().Baixo = new Color(1f, 1f, 1f, 0f);   // so' o espacamento
            // o Letreiro abre a palavra 12% em volta do meio dela: +8 dp de folga para a 1a letra nao encostar no selo
            rotulo.rectTransform.anchorMin = new Vector2(0, 0.52f); rotulo.rectTransform.anchorMax = new Vector2(1, 0.92f);
            rotulo.rectTransform.offsetMin = new Vector2(x0 + Dp.Px(8f), 0); rotulo.rectTransform.offsetMax = new Vector2(-folga, 0);
            _nome = Formas.Texto(_caixa, "Nome", "", 17f, Estilo.OuroClaro, TextAnchor.UpperLeft);
            _nome.fontStyle = FontStyle.Bold;
            _nome.horizontalOverflow = HorizontalWrapMode.Wrap; _nome.verticalOverflow = VerticalWrapMode.Truncate;
            _nome.resizeTextForBestFit = true; _nome.resizeTextMaxSize = _nome.fontSize; _nome.resizeTextMinSize = Mathf.Max(Mathf.RoundToInt(Dp.Px(12f)), 8);
            Arkana.Menu.Menu.Contorno(_nome);
            _nome.rectTransform.anchorMin = new Vector2(0, 0.08f); _nome.rectTransform.anchorMax = new Vector2(1, 0.54f);
            _nome.rectTransform.offsetMin = new Vector2(x0, 0); _nome.rectTransform.offsetMax = new Vector2(-folga, 0);
            _caixa.gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;   // com escala: a pausa congela o aviso e o relogio do grimorio
            if (Grimorio != null) Grimorio.Tick(dt);
            Logica.Tick(dt);
            Pintar(TamanhoDaTela());
        }

        /// <summary>O mesmo do Hud: canvas numa camera (a foto) = o alvo dela; senao, a tela.</summary>
        Vector2 TamanhoDaTela()
        {
            Canvas c = GetComponentInParent<Canvas>();
            c = c != null ? c.rootCanvas : null;
            if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera != null) return c.worldCamera.pixelRect.size;
            return new Vector2(Screen.width, Screen.height);
        }

        /// <summary>Publico para a foto pintar no tamanho do QUADRO (o batchmode tem Screen de 640x480) sem esperar um quadro.</summary>
        public void Pintar(Vector2 tela)
        {
            string atual = Logica.Atual;
            if (atual != _mostrando)
            {
                _mostrando = atual;
                _caixa.gameObject.SetActive(atual != null);
                if (atual == null) return;
                if (_icone != null) Destroy(_icone.gameObject);
                _icone = IconeDaPagina.Desenhar(_selo, atual, true, Dp.Px(SeloDp) * 0.62f);
                _nome.text = TelaGrimorio.Rotulo(atual, true)[0];
            }
            if (atual == null) return;
            if (tela != _tela) { _tela = tela; _r = AvisoGrimorioLogica.Retangulo(tela, AreaSegura.Atual(), Dp.Px(1f)); }
            _caixa.sizeDelta = _r.size;
            _caixa.anchoredPosition = new Vector2(_r.xMin - (1f - Logica.Entrada) * Dp.Px(DeslizeDp), _r.center.y - tela.y * 0.5f);
            _grupo.alpha = Logica.Alfa;
            // o BRILHO do selo: nasce no pico e apaga em ~1,2 s (a pagina "acendendo")
            _brilho.color = Formas.ComAlfa(Estilo.Ouro, BrilhoPico * (1f - Mathf.Clamp01(Logica.T / 1.2f)) + 0.2f);
        }
    }
}
