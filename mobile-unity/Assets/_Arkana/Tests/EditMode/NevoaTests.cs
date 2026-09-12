using NUnit.Framework;
using UnityEngine;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// A nevoa por altura (Ilha.DistanciasDaNevoa). A licao G5 do Godot: visto da queda, o chao a 200 m nao pode estar
    /// engolido — e' la' de cima que se escolhe onde pousar. Foto de 11/09: com a nevoa fixa, a ilha sumia no caramelo.
    /// </summary>
    public class NevoaTests
    {
        static float Fracao(float d, float inicio, float fim) => Mathf.Clamp01((d - inicio) / (fim - inicio));

        [Test]
        public void NoChao_ValeANevoaDoChao()
        {
            float i, f;
            Ilha.DistanciasDaNevoa(1.8f, 1000f, out i, out f);
            Assert.AreEqual(Ilha.NevoaInicio, i, 2f, "o olho do mago a 1,8 m ve' a nevoa do chao");
            Assert.Less(Fracao(200f, i, f), 0.2f, "a 200 m do chao, ar leve");
        }

        [Test]
        public void DaQueda_OChaoLaEmbaixoLeLimpo()
        {
            float i, f;
            Ilha.DistanciasDaNevoa(200f, 1000f, out i, out f);
            Assert.AreEqual(0f, Fracao(200f, i, f), 1e-4f, "a 200 m de altura, o chao logo abaixo sem nevoa");
            Ilha.DistanciasDaNevoa(320f, 1000f, out i, out f);
            Assert.Less(Fracao(450f, i, f), 0.1f, "do castelo, o centro da ilha a ~450 m continua legivel");
        }

        [Test]
        public void SobeComAAltura_ENuncaPassaDoFarClip()
        {
            float antesI = -1f, antesF = -1f;
            for (float h = 0f; h <= 1000f; h += 50f)
            {
                float i, f;
                Ilha.DistanciasDaNevoa(h, 1000f, out i, out f);
                Assert.Greater(f, i, "fim depois do comeco em h=" + h);
                Assert.LessOrEqual(f, 950f + 1e-3f, "o fim respeita 95% do far clip em h=" + h);
                Assert.GreaterOrEqual(i, antesI - 1e-3f, "o comeco nunca volta ao subir");
                Assert.GreaterOrEqual(f, antesF - 1e-3f, "o fim nunca volta ao subir");
                antesI = i; antesF = f;
            }
        }

        [Test]
        public void AlturaNegativaOuNaN_VaiParaOChao()
        {
            float i0, f0, i1, f1, i2, f2;
            Ilha.DistanciasDaNevoa(0f, 1000f, out i0, out f0);
            Ilha.DistanciasDaNevoa(-30f, 1000f, out i1, out f1);
            Ilha.DistanciasDaNevoa(float.NaN, 1000f, out i2, out f2);
            Assert.AreEqual(i0, i1); Assert.AreEqual(f0, f1);
            Assert.AreEqual(i0, i2); Assert.AreEqual(f0, f2);
        }
    }
}
