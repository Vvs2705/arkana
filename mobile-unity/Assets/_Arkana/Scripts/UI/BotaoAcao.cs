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

        /// <summary>Texto do meio: "%" da carga (suprema) ou segundos que faltam (tatica); "" quando pronto.</summary>
        public string TextoCentral()
        {
            float f = Frac;
            if (f <= 0f) return "";
            if (MostraCarga) return Mathf.FloorToInt((1f - f) * 100f) + "%";
            return Mathf.CeilToInt(f * _total).ToString();
        }

        /// <summary>Toque: so' passa se ativo E pronto. Devolve se o pedido saiu.</summary>
        public bool Tocar()
        {
            return Ativo && Frac <= 0f;
        }
    }

    /// <summary>Casca uGUI: Esquiva, Tatica, Suprema, Salto, Pegar e Pausa sao o MESMO widget.</summary>
    public sealed class BotaoAcao : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public BotaoAcaoLogica Logica { get; private set; } = new BotaoAcaoLogica();
        public event Action Tocado;
        public bool Segurando { get; private set; }

        Image _disco;
        Image _contorno;
        Image _arco;
        Text _rotulo;
        Text _centro;
        Text _subtitulo;
        Color _cor;

        public static BotaoAcao Criar(Transform pai, string nome, string rotulo, Color cor, float ladoPx)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(BotaoAcao));
            go.transform.SetParent(pai, false);
            var b = go.GetComponent<BotaoAcao>();
            ((RectTransform)go.transform).sizeDelta = new Vector2(ladoPx, ladoPx);
            b._cor = cor;
            b._disco = go.GetComponent<Image>();
            b._disco.sprite = Formas.Disco();
            b._disco.raycastTarget = true;
            b._contorno = Formas.Imagem(go.transform, "Contorno", Formas.Anel(), new Color(1, 1, 1, 0.56f));
            AreaSegura.Esticar(b._contorno.rectTransform);
            // arco de cooldown: Image radial (Filled) — nao precisa de shader
            b._arco = Formas.Imagem(go.transform, "Arco", Formas.Disco(), new Color(0, 0, 0, 0.55f));
            b._arco.rectTransform.sizeDelta = new Vector2(ladoPx * 0.7f, ladoPx * 0.7f);
            b._arco.type = Image.Type.Filled;
            b._arco.fillMethod = Image.FillMethod.Radial360;
            b._arco.fillOrigin = (int)Image.Origin360.Top;
            b._arco.fillClockwise = true;
            b._arco.fillAmount = 0f;
            b._rotulo = Formas.Texto(go.transform, "Rotulo", rotulo, 11f, Color.white);
            AreaSegura.Esticar(b._rotulo.rectTransform);
            b._centro = Formas.Texto(go.transform, "Centro", "", 11f, Color.white);
            b._centro.rectTransform.anchoredPosition = new Vector2(0, ladoPx * 0.22f);
            b._subtitulo = Formas.Texto(go.transform, "Subtitulo", "", 8f, new Color(1, 1, 1, 0.62f));
            b._subtitulo.rectTransform.anchoredPosition = new Vector2(0, -ladoPx * 0.5f - Dp.Px(8f));
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
                case "losango": _contorno.sprite = Formas.Losango(); break;
                case "triangulo": _contorno.sprite = Formas.Triangulo(); break;
                default: _contorno.sprite = Formas.Anel(); break;
            }
            // disco continua REDONDO (alvo do dedo); so' o contorno muda
            _contorno.type = Image.Type.Simple;
        }

        public void Ativo(bool on) { Logica.Ativo = on; Pintar(); }

        void Update()
        {
            Logica.Tick(Time.deltaTime);
            Pintar();
        }

        void Pintar()
        {
            bool ativo = Logica.Ativo;
            Color baseCor = ativo ? _cor : new Color(0.55f, 0.55f, 0.58f);
            _disco.color = Formas.ComAlfa(baseCor, Logica.Pronto ? 0.36f : 0.18f);
            _contorno.color = new Color(1, 1, 1, ativo ? 0.56f : 0.28f);
            float f = Logica.Frac;
            _arco.enabled = f > 0f;
            _arco.fillAmount = f;
            _centro.text = Logica.TextoCentral();
            _rotulo.color = new Color(1, 1, 1, ativo ? 0.9f : 0.55f);
            _subtitulo.color = new Color(1, 1, 1, ativo ? 0.62f : 0.35f);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!gameObject.activeInHierarchy) return;
            Segurando = true;
            if (Logica.Tocar()) Tocado?.Invoke();
        }

        public void OnPointerUp(PointerEventData e) { Segurando = false; }
    }
}
