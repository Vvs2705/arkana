using NUnit.Framework;
using UnityEngine;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>Joystick: deadzone REESCALADA (sem degrau) e curva de resposta.</summary>
    public class UiJoystickTests
    {
        readonly JoystickLogica _j = new JoystickLogica(0.12f, 1.4f);
        const float R = 100f;

        [Test]
        public void DentroDaDeadzoneEZero()
        {
            Assert.AreEqual(Vector2.zero, _j.Direcao(new Vector2(11f, 0), R));
            Assert.AreEqual(Vector2.zero, _j.Direcao(Vector2.zero, R));
        }

        [Test]
        public void LogoAposADeadzoneNaoSaiA12PorCento()
        {
            // O degrau: 12% de curso nao pode virar 12% de velocidade — reescala a partir da borda da zona morta.
            Vector2 v = _j.Direcao(new Vector2(13f, 0), R);
            Assert.Greater(v.magnitude, 0f);
            Assert.Less(v.magnitude, 0.05f, "saiu da zona morta: quase parado, nao a 12%");
        }

        [Test]
        public void CursoCheioEUm()
        {
            Assert.AreEqual(1f, _j.Direcao(new Vector2(R, 0), R).magnitude, 1e-4f);
            Assert.AreEqual(1f, _j.Direcao(new Vector2(R * 3f, 0), R).magnitude, 1e-4f, "alem do anel satura em 1");
        }

        [Test]
        public void CurvaDaCursoFinoPertoDoCentro()
        {
            // meio do curso util: linear daria 0.5; curva 1.4 da' 0.5^1.4 ~ 0.379
            float meio = (0.12f + 1f) / 2f * R;
            float mag = _j.Direcao(new Vector2(meio, 0), R).magnitude;
            Assert.AreEqual(Mathf.Pow(0.5f, 1.4f), mag, 1e-3f);
            Assert.Less(mag, 0.5f);
            var linear = new JoystickLogica(0.12f, 1f);
            Assert.AreEqual(0.5f, linear.Direcao(new Vector2(meio, 0), R).magnitude, 1e-3f);
        }

        [Test]
        public void DirecaoEPreservada()
        {
            Vector2 v = _j.Direcao(new Vector2(0, -80f), R);
            Assert.AreEqual(0f, v.x, 1e-4f);
            Assert.Less(v.y, 0f);
        }

        [Test]
        public void RaioZeroNaoExplode()
        {
            Assert.AreEqual(Vector2.zero, _j.Direcao(new Vector2(10, 10), 0f));
        }
    }
}
