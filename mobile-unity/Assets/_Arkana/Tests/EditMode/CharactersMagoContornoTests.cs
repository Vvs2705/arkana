using NUnit.Framework;
using UnityEngine;
using Arkana.Characters;

namespace Arkana.Tests
{
    /// <summary>
    /// O CONTORNO DE LUZ (onda 9A) mora no Arkana/Mago, e os 20 FBX nascem URP Lit no ImportacaoArkana: o Mago troca o
    /// shader na hora de vestir. A troca e' por NOME de propriedade — renomear _BaseMap no shader deixaria o elenco inteiro
    /// sem textura com o portao verde. E a tinta de bot, o cinza de morto e a piscada escrevem _BaseColor.
    /// </summary>
    public class CharactersMagoContornoTests
    {
        [Test]
        public void MagoExterno_VesteOToonComContorno_SemPerderTexturaNemTinta()
        {
            string slug = null;
            foreach (string s in CharactersMagoExternoTests.SlugsExternos()) { slug = s; break; }
            Assume.That(slug, Is.Not.Null, "nenhum FBX em Resources/magos");
            Shader toon = Resources.Load<Shader>("ArkanaMago");
            Assert.IsNotNull(toon, "o ArkanaMago saiu de Resources");
            Assume.That(toon.isSupported, "Arkana/Mago nao roda neste dispositivo grafico: o mago fica no URP Lit do import");

            // o Mago instancia materiais (r.materials: o tint de um bot nao pinta o player); no EditMode isso loga erro
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Mago m = Mago.Criar(null, slug);
            try
            {
                int n = 0;
                foreach (Renderer r in m.GetComponentsInChildren<Renderer>())
                    foreach (Material mat in r.sharedMaterials)
                    {
                        n++;
                        Assert.AreEqual("Arkana/Mago", mat.shader.name, r.name + ": o FBX nao vestiu o toon com contorno");
                        Assert.IsNotNull(mat.GetTexture("_BaseMap"), r.name + ": a textura da Meshy nao veio na troca de shader");
                        Assert.IsNotNull(mat.GetTexture("_BumpMap"), r.name + ": o normal da Meshy nao veio na troca de shader");
                        Assert.Greater(mat.GetColor("_EmissionColor").maxColorComponent, 0f, r.name + ": o preenchimento sumiu");
                    }
                Assert.Greater(n, 0, "o FBX nao tem material");

                m.SetTint(Arkana.Gameplay.Pawn.TINT_MORTO);
                foreach (Renderer r in m.GetComponentsInChildren<Renderer>())
                    foreach (Material mat in r.sharedMaterials)
                    {
                        // tolerancia: a cor ida e volta sRGB <-> linear do projeto Linear erra no 7o digito (0,25 != 0,25 exato)
                        Color c = mat.GetColor("_BaseColor"), t = Arkana.Gameplay.Pawn.TINT_MORTO;
                        Assert.IsTrue(Mathf.Abs(c.r - t.r) < 1e-3f && Mathf.Abs(c.g - t.g) < 1e-3f && Mathf.Abs(c.b - t.b) < 1e-3f,
                            r.name + ": o cinza de morto nao pinta (" + c + ")");
                    }
            }
            finally { Object.DestroyImmediate(m.gameObject); }
        }
    }
}
