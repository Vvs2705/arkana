using NUnit.Framework;
using UnityEngine;
using Arkana.Gameplay;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>
    /// A camera NUNCA termina dentro de uma parede (foto de 11/09, seed 3103: mago encostado numa rocha, ombro da
    /// camera dentro dela, tela preta). O antes (so' o SphereCast do olho) deixava a camera a 0,2 m do ombro, dentro do cubo.
    /// </summary>
    public class CameraColisaoTests
    {
        static GameObject Parede(Vector3 centro, Vector3 tamanho)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = centro;
            go.transform.localScale = tamanho;
            Physics.SyncTransforms();
            return go;
        }

        static bool DentroDeAlgo(Vector3 p) => Physics.OverlapSphere(p, CameraLogica.RAIO_COLISAO).Length > 0;

        [Test]
        public void OmbroDentroDaParede_ACameraRecuaParaOCentro()
        {
            // parede colada a direita do mago: o ombro (0,78 m a direita) cai dentro dela
            GameObject parede = Parede(new Vector3(2f, 2f, 0f), new Vector3(3f, 4f, 8f));
            try
            {
                Vector3 centro = new Vector3(0f, CameraLogica.ALTURA_PIVO, 0f);
                Vector3 olho = centro + new Vector3(CameraLogica.OMBRO_X, CameraLogica.OMBRO_Y, 0f);   // x=0,78: dentro (parede de 0,5 a 3,5)
                Vector3 desejada = olho - Vector3.forward * CameraLogica.BRACO;
                Assert.IsTrue(DentroDeAlgo(olho), "o cenario: o ombro esta' dentro da parede");

                // o ANTES: so' o SphereCast a partir do olho. Comecando dentro, o Unity reporta a parede a distancia ZERO;
                // o codigo antigo grampeava em 0,2 m e deixava a camera a 0,2 m do ombro — dentro da parede.
                Assert.IsTrue(DentroDeAlgo(olho - Vector3.forward * 0.2f), "o defeito, documentado: 0,2 m atras do ombro ainda e' parede");

                Vector3 pos = CameraTerceiraPessoa.Colidir(centro, olho, desejada, null);
                Assert.IsFalse(DentroDeAlgo(pos), "a camera termina FORA de qualquer colisor");
                Assert.Less(pos.x, 0.5f - CameraLogica.RAIO_COLISAO + 0.01f, "recuou do ombro para o lado livre");
            }
            finally { Object.DestroyImmediate(parede); }
        }

        [Test]
        public void ParedeAtras_ACameraEncostaNela()
        {
            GameObject parede = Parede(new Vector3(0f, 2f, -2.5f), new Vector3(8f, 4f, 1f));   // de z=-3 a z=-2
            try
            {
                Vector3 centro = new Vector3(0f, CameraLogica.ALTURA_PIVO, 0f);
                Vector3 olho = centro + new Vector3(CameraLogica.OMBRO_X, CameraLogica.OMBRO_Y, 0f);
                Vector3 desejada = olho - Vector3.forward * CameraLogica.BRACO;   // z=-4,15: atras da parede
                Vector3 pos = CameraTerceiraPessoa.Colidir(centro, olho, desejada, null);
                Assert.IsFalse(DentroDeAlgo(pos));
                Assert.Greater(pos.z, -2f, "parou antes da parede");
                Assert.Less(pos.z, -1f, "mas usou o braco que tinha");
            }
            finally { Object.DestroyImmediate(parede); }
        }

        [Test]
        public void EmCanto_ACameraSobeEOlhaDeCima()
        {
            // parede a direita E atras: atras so' sobra 0,6 m de braco -> canto
            GameObject lado = Parede(new Vector3(2f, 2f, 0f), new Vector3(3f, 4f, 8f));
            GameObject atras = Parede(new Vector3(0f, 2f, -1.4f), new Vector3(8f, 4f, 1f));   // de z=-1,9 a z=-0,9
            try
            {
                Vector3 centro = new Vector3(0f, CameraLogica.ALTURA_PIVO, 0f);
                Vector3 olho = centro + new Vector3(CameraLogica.OMBRO_X, CameraLogica.OMBRO_Y, 0f);
                Vector3 desejada = olho - Vector3.forward * CameraLogica.BRACO;
                bool deCima;
                Vector3 pos = CameraTerceiraPessoa.Colidir(centro, olho, desejada, null, out deCima);
                Assert.IsTrue(deCima, "em canto, a camera vai para cima");
                Assert.IsFalse(DentroDeAlgo(pos), "e termina fora de tudo");
                Assert.Greater(pos.y, centro.y + 0.5f, "acima da cabeca");
                Assert.Greater(Vector3.Distance(pos, centro), CameraLogica.BRACO_CURTO * 0.5f, "com algum braco");
                Assert.IsFalse(Physics.Linecast(pos, centro), "e ve' a cabeca do mago");
            }
            finally { Object.DestroyImmediate(lado); Object.DestroyImmediate(atras); }
        }

        [Test]
        public void SemParede_ACameraFicaNoBraco()
        {
            Vector3 centro = new Vector3(0f, CameraLogica.ALTURA_PIVO, 0f);
            Vector3 olho = centro + new Vector3(CameraLogica.OMBRO_X, CameraLogica.OMBRO_Y, 0f);
            Vector3 desejada = olho - Vector3.forward * CameraLogica.BRACO;
            Assert.AreEqual(desejada, CameraTerceiraPessoa.Colidir(centro, olho, desejada, null));
        }

        // ------------------------------------------------------------------ 04/10: a mira converge (BLOCO E)

        [Test]
        public void Mira_TiroDaMaoVaiAoPontoDoRaio_EIgnoraOQueEstaEntreCameraEMao()
        {
            // camera no ombro direito, atras; mao no eixo do corpo. O raio acha o alvo; o tiro da MAO aponta para o mesmo
            // ponto (o arrasto saia paralelo a camera: ~1,9 m curto e 0,78 m a esquerda). Pilar colado na lente nao conta.
            Vector3 mao = new Vector3(0f, 1.4f, 0f);
            Vector3 olho = new Vector3(CameraLogica.OMBRO_X, CameraLogica.ALTURA_PIVO + CameraLogica.OMBRO_Y, -CameraLogica.BRACO);
            GameObject alvo = Parede(new Vector3(0.5f, 1.5f, 20f), Vector3.one);
            GameObject pilar = Parede(new Vector3(0.78f, 2f, -2.5f), new Vector3(0.3f, 4f, 0.3f));
            try
            {
                Vector3 dir = (alvo.transform.position - olho).normalized;
                Vector3 p = Player.PontoDoRaio(olho, dir, 40f, mao, null);
                Assert.Greater(p.z, 19f, "o pilar entre a camera e a mao nao rouba a mira");
                Assert.Less(Vector3.Distance(p, alvo.transform.position), 0.9f, "o raio acha o alvo");
                Assert.Less(Vector3.Angle(p - mao, alvo.transform.position - mao), 2f, "o tiro da mao vai para onde o reticulo olha");
            }
            finally { Object.DestroyImmediate(alvo); Object.DestroyImmediate(pilar); }
        }

        [Test]
        public void Olhar_EmDp_OMesmoCmGiraIgualEmQualquerTela()
        {
            float antes = Dp.DpiForcado;
            try
            {
                Dp.DpiForcado = 160f; Vector2 a = Player.EmDp(new Vector2(160f / 2.54f, 0f));   // 1 cm a 160 dpi
                Dp.DpiForcado = 395f; Vector2 b = Player.EmDp(new Vector2(395f / 2.54f, 0f));   // 1 cm no Poco F4 (~395 ppi)
                Assert.AreEqual(a.x, b.x, 1e-3f, "1 cm de dedo = o mesmo giro (em px o Poco girava 2,5x)");
            }
            finally { Dp.DpiForcado = antes; }
        }
    }
}
