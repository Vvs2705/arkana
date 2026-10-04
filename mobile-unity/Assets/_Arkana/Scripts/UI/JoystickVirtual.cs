using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Arkana.Core;

namespace Arkana.UI
{
    /// <summary>
    /// Logica PURA do joystick (GDD §19.3). Entrada: deslocamento do dedo em px a partir do centro e o raio util.
    /// Saida: direcao com comprimento 0..1 ja' com deadzone REESCALADA e curva aplicada.
    /// Deadzone e' UMA fonte de verdade (Balance.Move.StickDeadzone): reescalar a partir da borda da zona morta
    /// e' o que impede o degrau — cruzou a zona, sai de 0, nao de 12% da velocidade.
    /// </summary>
    public sealed class JoystickLogica
    {
        public float Deadzone;
        public float Curva;

        public JoystickLogica(float deadzone, float curva) { Deadzone = deadzone; Curva = curva; }

        public Vector2 Direcao(Vector2 deslocPx, float raioPx)
        {
            if (raioPx <= 0f) return Vector2.zero;
            Vector2 v = deslocPx / raioPx;
            float len = v.magnitude;
            if (len > 1f) { v /= len; len = 1f; }
            if (len < Deadzone || len <= 0f) return Vector2.zero;
            float util = (len - Deadzone) / (1f - Deadzone);           // 0 na borda da zona morta, 1 na borda do anel
            float mag = Mathf.Pow(Mathf.Clamp01(util), Curva <= 0f ? 1f : Curva);
            return v / len * mag;
        }
    }

    /// <summary>
    /// Casca uGUI: base + miolo, arrasto pelo EventSystem (multi-toque: cada ponteiro tem seu id). Visual: base ESCURA
    /// translucida com aro sutil (os dois circulos brancos chapados liam placeholder na foto de 12/09) e miolo com volume
    /// (degrade) e sombra macia; o aro acende enquanto o dedo segura.
    /// </summary>
    public sealed class JoystickVirtual : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        static readonly Color AroSolto = new Color(0.82f, 0.88f, 1f, 0.34f);
        static readonly Color AroPreso = new Color(0.9f, 0.94f, 1f, 0.62f);

        public Vector2 Direcao { get; private set; }
        public bool Segurando => _ponteiro != int.MinValue;

        JoystickLogica _logica;
        RectTransform _rt;
        RectTransform _bolinha;
        Image _aro;
        int _ponteiro = int.MinValue;

        public static JoystickVirtual Criar(Transform pai, float ladoPx)
        {
            var go = new GameObject("Joystick", typeof(RectTransform), typeof(Image), typeof(JoystickVirtual));
            go.transform.SetParent(pai, false);
            var fundo = go.GetComponent<Image>();
            fundo.sprite = Formas.Disco();
            fundo.color = new Color(0.03f, 0.04f, 0.08f, 0.3f);
            fundo.raycastTarget = true;   // o alvo de toque e' o disco inteiro
            var j = go.GetComponent<JoystickVirtual>();
            j._rt = (RectTransform)go.transform;
            j._rt.sizeDelta = new Vector2(ladoPx, ladoPx);
            var sombra = Formas.Imagem(go.transform, "Sombra", Formas.Halo(), new Color(0, 0, 0, 0.3f));
            sombra.rectTransform.sizeDelta = new Vector2(ladoPx, ladoPx) * Formas.HaloEscala;
            j._aro = Formas.Imagem(go.transform, "Anel", Formas.Anel(0.93f), AroSolto);
            AreaSegura.Esticar(j._aro.rectTransform);
            float dz = (float)Balance.Move.StickDeadzone;
            var zona = Formas.Imagem(go.transform, "ZonaMorta", Formas.Disco(), new Color(1, 1, 1, 0.14f));
            zona.rectTransform.sizeDelta = new Vector2(ladoPx * dz, ladoPx * dz);
            // miolo = no' vazio que anda; dentro dele a sombra vem ANTES (uGUI desenha na ordem dos irmaos)
            j._bolinha = Formas.No(go.transform, "Bolinha");
            j._bolinha.sizeDelta = new Vector2(Dp.Px(44f), Dp.Px(44f));
            var sombraMiolo = Formas.Imagem(j._bolinha, "Sombra", Formas.Sombra(), new Color(0, 0, 0, 0.5f));
            sombraMiolo.rectTransform.sizeDelta = new Vector2(Dp.Px(58f), Dp.Px(58f));
            sombraMiolo.rectTransform.anchoredPosition = new Vector2(0, -Dp.Px(3f));
            var miolo = Formas.Imagem(j._bolinha, "Miolo", Formas.DiscoDegrade(), new Color(0.86f, 0.9f, 1f, 0.88f));
            AreaSegura.Esticar(miolo.rectTransform);
            var borda = Formas.Imagem(j._bolinha, "Borda", Formas.Anel(0.88f), new Color(0.3f, 0.34f, 0.45f, 0.55f));
            AreaSegura.Esticar(borda.rectTransform);
            j._logica = new JoystickLogica(dz, (float)Balance.Move.StickCurve);
            return j;
        }

        float Raio => _rt.rect.width * 0.5f * 0.75f;

        public void OnPointerDown(PointerEventData e)
        {
            if (_ponteiro != int.MinValue) return;
            _ponteiro = e.pointerId;
            _aro.color = AroPreso;
            Atualizar(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _ponteiro) return;
            Atualizar(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _ponteiro) return;
            Soltar();
        }

        /// <summary>Desligado com o dedo em cima (eliminado, espectador), o "soltar" nunca chega: sem isto o analogico voltava
        /// travado na ultima direcao depois de revivido.</summary>
        void OnDisable() { if (_ponteiro != int.MinValue) Soltar(); }

        void Soltar()
        {
            _ponteiro = int.MinValue;
            Direcao = Vector2.zero;
            _aro.color = AroSolto;
            if (_bolinha != null) _bolinha.anchoredPosition = Vector2.zero;
        }

        void Atualizar(PointerEventData e)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out local);
            Direcao = _logica.Direcao(local, Raio);
            if (_bolinha != null) _bolinha.anchoredPosition = Vector2.ClampMagnitude(local, Raio) * 0.6f;
        }
    }
}
