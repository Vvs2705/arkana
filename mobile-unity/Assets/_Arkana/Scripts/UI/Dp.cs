using UnityEngine;

namespace Arkana.UI
{
    /// <summary>
    /// dp -> px. Numero que o DEDO sente vive em dp (regra do projeto): fisico = dp * dpi/160.
    /// O Canvas da HUD usa escala 1 (px de tela = px de canvas), entao nao ha' segunda conversao.
    /// </summary>
    public static class Dp
    {
        public const float DpiPadrao = 160f;

        /// <summary>Override para teste/preview (0 = usa Screen.dpi).</summary>
        public static float DpiForcado = 0f;

        public static float Dpi
        {
            get
            {
                if (DpiForcado > 0f) return DpiForcado;
                float d = Screen.dpi;
                return d > 0f ? d : DpiPadrao;   // editor/headless devolve 0
            }
        }

        public static float Px(float dp) => PxCom(dp, Dpi);

        /// <summary>Conta pura (testavel): dp -> px numa dpi dada.</summary>
        public static float PxCom(float dp, float dpi) => dp * (dpi > 0f ? dpi : DpiPadrao) / DpiPadrao;

        /// <summary>Piso do alvo de toque (GDD §19.3).</summary>
        public const float AlvoMinimoDp = 48f;

        /// <summary>Deadzone da mira em px, a partir do numero em dp do Balance.</summary>
        public static float Deadzone => Px((float)Core.Balance.Touch.AimDeadzoneDp);
    }
}
