using UnityEngine;

namespace Arkana
{
    /// <summary>
    /// A MEDICAO QUE FALTAVA DESDE 25/08: FPS no aparelho, escrito no logcat a cada JANELA segundos
    /// ("ARKANA FPS media=58,7 min=41,2 quadros=294 dt_max=31,0ms"). Le-se com `adb logcat -s Unity`.
    /// Media = quadros / tempo; min = o pior segundo da janela; dt_max = o pior quadro (engasgo).
    /// Fora do editor e dos testes: a linha e' um Debug.Log e o BootTests reprova log inesperado.
    /// </summary>
    public sealed class MedidorDeFps : MonoBehaviour
    {
        public const float JANELA = 5f;

        float _t, _tSeg, _dtMax, _piorSeg = float.MaxValue;
        int _quadros, _quadrosSeg;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt; _tSeg += dt; _quadros++; _quadrosSeg++;
            if (dt > _dtMax) _dtMax = dt;
            if (_tSeg >= 1f)
            {
                float fpsSeg = _quadrosSeg / _tSeg;
                if (fpsSeg < _piorSeg) _piorSeg = fpsSeg;
                _tSeg = 0f; _quadrosSeg = 0;
            }
            if (_t < JANELA) return;
            Debug.Log(Linha(_quadros / _t, _piorSeg == float.MaxValue ? _quadros / _t : _piorSeg, _quadros, _dtMax));
            _t = 0f; _quadros = 0; _dtMax = 0f; _piorSeg = float.MaxValue;
        }

        public static string Linha(float media, float min, int quadros, float dtMax) =>
            string.Format("ARKANA FPS media={0:F1} min={1:F1} quadros={2} dt_max={3:F1}ms", media, min, quadros, dtMax * 1000f);
    }
}
