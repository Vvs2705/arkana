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

    /// <summary>Casca uGUI: anel + bolinha, arrasto pelo EventSystem (multi-toque: cada ponteiro tem seu id).</summary>
    public sealed class JoystickVirtual : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Direcao { get; private set; }
        public bool Segurando => _ponteiro != int.MinValue;

        JoystickLogica _logica;
        RectTransform _rt;
        RectTransform _bolinha;
        int _ponteiro = int.MinValue;

        public static JoystickVirtual Criar(Transform pai, float ladoPx)
        {
            var go = new GameObject("Joystick", typeof(RectTransform), typeof(Image), typeof(JoystickVirtual));
            go.transform.SetParent(pai, false);
            var fundo = go.GetComponent<Image>();
            fundo.sprite = Formas.Disco();
            fundo.color = new Color(1, 1, 1, 0.08f);
            fundo.raycastTarget = true;   // o alvo de toque e' o disco inteiro
            var j = go.GetComponent<JoystickVirtual>();
            j._rt = (RectTransform)go.transform;
            j._rt.sizeDelta = new Vector2(ladoPx, ladoPx);
            var anel = Formas.Imagem(go.transform, "Anel", Formas.Anel(), new Color(1, 1, 1, 0.35f));
            AreaSegura.Esticar(anel.rectTransform);
            float dz = (float)Balance.Move.StickDeadzone;
            var zona = Formas.Imagem(go.transform, "ZonaMorta", Formas.Disco(), new Color(1, 1, 1, 0.2f));
            zona.rectTransform.sizeDelta = new Vector2(ladoPx * dz, ladoPx * dz);
            var bola = Formas.Imagem(go.transform, "Bolinha", Formas.Disco(), new Color(1, 1, 1, 0.45f));
            bola.rectTransform.sizeDelta = new Vector2(Dp.Px(44f), Dp.Px(44f));
            j._bolinha = bola.rectTransform;
            j._logica = new JoystickLogica(dz, (float)Balance.Move.StickCurve);
            return j;
        }

        float Raio => _rt.rect.width * 0.5f * 0.75f;

        public void OnPointerDown(PointerEventData e)
        {
            if (_ponteiro != int.MinValue) return;
            _ponteiro = e.pointerId;
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
            _ponteiro = int.MinValue;
            Direcao = Vector2.zero;
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
