using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arkana.UI
{
    /// <summary>
    /// Logica PURA do cooldown de um botao: o Bus so' emite na BORDA (KitCooldown restante/total),
    /// e a fracao 0..1 e' interpolada por Tick(dt) entre bordas. MostraCarga = suprema (0->100%).
    /// Apagado (Ativo=false) nao pede nada: 17 dos 20 magos ainda nao tem kit.
    /// </summary>
    public sealed class BotaoAcaoLogica
    {
        public bool Ativo = true;
        public bool MostraCarga;
        float _restante;
        float _total;

        public float Total => _total;

        public void Cooldown(float restante, float total)
        {
            _total = Mathf.Max(total, 0f);
            _restante = Mathf.Clamp(restante, 0f, _total);
        }

        public void Tick(float dt)
        {
            if (_restante > 0f) _restante = Mathf.Max(_restante - dt, 0f);
        }

        /// <summary>0 = pronto; 1 = acabou de entrar em cooldown.</summary>
        public float Frac => _total > 0f ? Mathf.Clamp01(_restante / _total) : 0f;
        public bool Pronto => Frac <= 0f && Ativo;

        /// <summary>O numero do meio (-1 = pronto): % ja' carregado (suprema) ou segundos que faltam (tatica).
        /// A casca so' refaz o texto quando ELE muda — zero string nova por frame.</summary>
        public int NumeroCentral
        {
            get
            {
                float f = Frac;
                if (f <= 0f) return -1;
                return MostraCarga ? Mathf.FloorToInt((1f - f) * 100f) : Mathf.CeilToInt(f * _total);
            }
        }

        /// <summary>Texto do meio: "%" da carga (suprema) ou segundos que faltam (tatica); "" quando pronto.</summary>
        public string TextoCentral()
        {
            int n = NumeroCentral;
            if (n < 0) return "";
            return MostraCarga ? n + "%" : n.ToString();
        }

        /// <summary>Toque: so' passa se ativo E pronto. Devolve se o pedido saiu.</summary>
        public bool Tocar()
        {
            return Ativo && Frac <= 0f;
        }
    }

    /// <summary>
    /// Casca uGUI: Esquiva, Tatica, Suprema, Salto, Pegar e Pausa sao o MESMO widget. A foto de 12/09 lia "placeholder"
    /// (disco claro translucido sumindo na areia): agora disco ESCURO, ANEL na cor da acao, halo escuro que descola do
    /// chao claro, cooldown = pizza escura radial + o aro reacendendo no sentido horario, e a suprema PRONTA pulsando.
    /// O alvo de toque continua o retangulo do proprio no' (ladoPx): halo e brilho sao so' tinta, nao pegam dedo.
    /// </summary>
    public sealed class BotaoAcao : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        static readonly Color Fundo = new Color(0.03f, 0.04f, 0.08f);
        static readonly Color Apagado = new Color(0.5f, 0.52f, 0.58f);
        const float AnelInterno = 0.87f;   // aro fino (~3,5dp num botao de 64dp)
        const float PulsoHz = 0.9f;        // suprema pronta: respira devagar (pisca-pisca rapido e' alarme, nao convite)
        const float EstouroS = 0.6f;       // o flash da virada para 100%

        public BotaoAcaoLogica Logica { get; private set; } = new BotaoAcaoLogica();
        public event Action Tocado;
        public bool Segurando { get; private set; }

        Image _disco;
        Image _brilho;
        Image _arco;
        Image _contorno;
        Image _progresso;
        Text _rotulo;
        Text _centro;
        Text _subtitulo;
        Color _cor;
        float _lado;
        int _numero = int.MinValue;   // ultimo numero escrito no centro: o texto so' e' refeito quando ELE muda
        int _estado = -1;             // 0 pronto, 1 cooldown: o rotulo so' troca de lugar/tamanho na virada
        bool _pulsavaAntes;
        float _estouro;               // 1 -> 0 depois que a suprema vira PRONTA

        public static BotaoAcao Criar(Transform pai, string nome, string rotulo, Color cor, float ladoPx)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(BotaoAcao));
            go.transform.SetParent(pai, false);
            var b = go.GetComponent<BotaoAcao>();
            ((RectTransform)go.transform).sizeDelta = new Vector2(ladoPx, ladoPx);
            b._cor = cor;
            b._lado = ladoPx;
            b._disco = go.GetComponent<Image>();
            b._disco.sprite = Formas.Disco();
            b._disco.raycastTarget = true;
            Vector2 halo = new Vector2(ladoPx, ladoPx) * Formas.HaloEscala;
            var sombra = Formas.Imagem(go.transform, "Sombra", Formas.Halo(), new Color(0, 0, 0, 0.45f));
            sombra.rectTransform.sizeDelta = halo;
            b._brilho = Formas.Imagem(go.transform, "Brilho", Formas.Halo(), cor);
            b._brilho.rectTransform.sizeDelta = halo;
            b._brilho.enabled = false;
            // cooldown: pizza escura (Filled radial, sem shader) — a parte clara cresce no sentido horario a partir das 12h
            b._arco = Formas.Imagem(go.transform, "Arco", Formas.Disco(), new Color(0, 0, 0, 0.5f));
            b._arco.rectTransform.sizeDelta = new Vector2(ladoPx, ladoPx) * AnelInterno;
            b._arco.type = Image.Type.Filled;
            b._arco.fillMethod = Image.FillMethod.Radial360;
            b._arco.fillOrigin = (int)Image.Origin360.Top;
            b._arco.fillClockwise = false;
            b._arco.fillAmount = 0f;
            b._contorno = Formas.Imagem(go.transform, "Contorno", Formas.Anel(AnelInterno), cor);
            AreaSegura.Esticar(b._contorno.rectTransform);
            // o aro REACENDE na cor da acao conforme o cooldown anda (o Contorno fica de trilho apagado por baixo)
            b._progresso = Formas.Imagem(go.transform, "Progresso", Formas.Anel(AnelInterno), cor);
            AreaSegura.Esticar(b._progresso.rectTransform);
            b._progresso.type = Image.Type.Filled;
            b._progresso.fillMethod = Image.FillMethod.Radial360;
            b._progresso.fillOrigin = (int)Image.Origin360.Top;
            b._progresso.fillClockwise = true;
            b._progresso.enabled = false;
            // 9,5dp negrito: "SUPREMA" cabe DENTRO do aro (a 11dp vazava por cima dele na foto)
            b._rotulo = Formas.Texto(go.transform, "Rotulo", rotulo, 9.5f, Color.white);
            b._rotulo.fontStyle = FontStyle.Bold;
            AreaSegura.Esticar(b._rotulo.rectTransform);
            b._centro = Formas.Texto(go.transform, "Centro", "", 15f, Color.white);
            b._centro.fontStyle = FontStyle.Bold;
            AreaSegura.Esticar(b._centro.rectTransform);
            b._centro.rectTransform.anchoredPosition = new Vector2(0, ladoPx * 0.1f);
            b._subtitulo = Formas.Texto(go.transform, "Subtitulo", "", 8.5f, new Color(0.88f, 0.9f, 0.98f, 0.72f));
            b._subtitulo.rectTransform.anchoredPosition = new Vector2(0, -ladoPx * 0.5f - Dp.Px(9f));
            b.Pintar();
            return b;
        }

        public void Rotulo(string r) { _rotulo.text = r; }
        public void Subtitulo(string s) { _subtitulo.text = s; Pintar(); }
        public void Cor(Color c) { _cor = c; Pintar(); }

        /// <summary>FORMA do contorno por raridade ("circulo"|"losango"|"triangulo"): GDD §10 dentro do botao.</summary>
        public void Forma(string forma)
        {
            switch (forma)
            {
                case "losango": _contorno.sprite = Formas.LosangoAnel(); break;
                case "triangulo": _contorno.sprite = Formas.TrianguloAnel(); break;
                default: _contorno.sprite = Formas.Anel(AnelInterno); break;
            }
            // disco continua REDONDO (alvo do dedo); so' o contorno muda
            _contorno.type = Image.Type.Simple;
        }

        public void Ativo(bool on) { Logica.Ativo = on; Pintar(); }

        void Update()
        {
            Logica.Tick(Time.deltaTime);
            if (_estouro > 0f) _estouro = Mathf.Max(_estouro - Time.unscaledDeltaTime / EstouroS, 0f);
            Pintar();
        }

        /// <summary>Por frame, mas sem lixo: setter de cor/fill do uGUI ignora valor igual, e texto so' quando o numero muda.</summary>
        void Pintar()
        {
            bool ativo = Logica.Ativo;
            float f = Logica.Frac;
            bool emCd = ativo && f > 0f;
            Color c = ativo ? _cor : Apagado;
            _disco.color = Formas.ComAlfa(Color.Lerp(Fundo, c, Segurando ? 0.34f : 0.16f), ativo ? 0.68f : 0.5f);
            _contorno.color = Formas.ComAlfa(c, !ativo ? 0.4f : emCd ? 0.26f : 1f);
            _progresso.enabled = emCd;
            _progresso.color = c;
            _progresso.fillAmount = 1f - f;
            _arco.enabled = emCd;
            _arco.fillAmount = f;
            int n = emCd ? Logica.NumeroCentral : -1;
            if (n != _numero) { _numero = n; _centro.text = n < 0 ? "" : Logica.TextoCentral(); }
            int estado = emCd ? 1 : 0;
            if (estado != _estado)
            {
                _estado = estado;
                // pronto: o NOME no meio; em cooldown o NUMERO manda e o nome desce, menor
                _rotulo.fontSize = Mathf.Max(Mathf.RoundToInt(Dp.Px(emCd ? 7.5f : 9.5f)), 8);
                _rotulo.rectTransform.anchoredPosition = new Vector2(0, emCd ? -_lado * 0.22f : 0f);
            }
            _rotulo.color = new Color(1, 1, 1, !ativo ? 0.45f : emCd ? 0.62f : 0.95f);
            _subtitulo.color = new Color(0.88f, 0.9f, 0.98f, ativo ? 0.72f : 0.38f);
            // suprema PRONTA: estouro na virada + respiracao enquanto espera o dedo (alfa pelo CanvasRenderer: nao refaz malha)
            bool pulsa = Logica.MostraCarga && Logica.Pronto;
            if (pulsa && !_pulsavaAntes) _estouro = 1f;
            _pulsavaAntes = pulsa;
            _brilho.enabled = pulsa;
            if (!pulsa) return;
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulsoHz * 2f * Mathf.PI);
            _brilho.color = c;
            _brilho.canvasRenderer.SetAlpha(Mathf.Clamp01(0.45f + 0.4f * p + 0.5f * _estouro));
            float s = 1f + 0.06f * p + 0.25f * _estouro;
            _brilho.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!gameObject.activeInHierarchy) return;
            Segurando = true;
            if (Logica.Tocar()) Tocado?.Invoke();
        }

        public void OnPointerUp(PointerEventData e) { Segurando = false; }

        /// <summary>Desligado com o dedo em cima, o soltar nao vem: o SALTO ficava "segurando" e o corpo flutuava sozinho.</summary>
        void OnDisable() { Segurando = false; }
    }
}
