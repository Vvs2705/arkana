using NUnit.Framework;
using UnityEngine;
using Arkana.Terrain;

namespace Arkana.Tests
{
    /// <summary>
    /// As PECAS DA MESHY da onda 11 (muro de terra, monolito, torreta, bobina, escudo): o muro sobe, assenta e afunda sem
    /// pular; sem o .glb a casca cai na primitiva sem quebrar; com o .glb, a peca tem o pe' no chao (o desenho a poe em
    /// y = 0) e as pecas dividem UM material por .glb.
    /// </summary>
    public class VisualPecasMeshyTests
    {
        [Test]
        public void Muro_SobeAssentaECai_SemPular()
        {
            Assert.AreEqual(0f, LeituraDoTerreno.FracaoDoMuro(0f, -1f), 1e-5f, "nasce enterrado");
            Assert.AreEqual(1f, LeituraDoTerreno.FracaoDoMuro(LeituraDoTerreno.SubidaDoMuro, -1f), 1e-5f, "de pe' ao fim da subida");
            Assert.AreEqual(1f, LeituraDoTerreno.FracaoDoMuro(9f, -1f), 1e-5f, "de pe' fica de pe'");
            Assert.AreEqual(1f, LeituraDoTerreno.FracaoDoMuro(9f, 0f), 1e-5f, "o quadro da queda ainda e' o muro inteiro");
            Assert.AreEqual(0f, LeituraDoTerreno.FracaoDoMuro(9f, LeituraDoTerreno.QuedaDoMuro), 1e-5f, "afundou inteiro: volta ao pool");
            float antes = -1f;
            for (float t = 0f; t <= LeituraDoTerreno.SubidaDoMuro + 0.05f; t += 0.01f)
            {
                float f = LeituraDoTerreno.FracaoDoMuro(t, -1f);
                Assert.GreaterOrEqual(f, antes - 1e-6f, "sobe sem voltar (t=" + t + ")");
                antes = f;
            }
            antes = 2f;
            for (float t = 0f; t <= LeituraDoTerreno.QuedaDoMuro + 0.05f; t += 0.01f)
            {
                float f = LeituraDoTerreno.FracaoDoMuro(5f, t);
                Assert.LessOrEqual(f, antes + 1e-6f, "cai sem voltar (t=" + t + ")");
                antes = f;
            }
            Assert.LessOrEqual(LeituraDoTerreno.FracaoDoMuro(0.1f, 0.05f), LeituraDoTerreno.FracaoDoMuro(0.1f, -1f) + 1e-6f,
                "caiu no MEIO da subida: desce dali, nunca pula para cima");
        }

        [Test]
        public void PecaDaMeshy_SemGlb_DevolveFalsoSemQuebrar()
        {
            Mesh m;
            Material mat;
            Assert.IsFalse(PecaDaMeshy.Carregar("nao-existe-onda11", out m, out mat), "sem o .glb: quem chama poe a primitiva");
            Assert.IsNull(m);
            Assert.IsNull(mat);
        }

        [Test]
        public void PecasDaOnda11_PeNoChao_EUmMaterialPorGlb(
            [Values("40-muro-terra", "41-monolito-basalto", "42-torreta-fizz", "43-bobina-fizz", "44-escudo-brok")] string nome)
        {
            if (Resources.Load<GameObject>(nome) == null) Assert.Ignore(nome + ".glb fora de Resources: a casca usa a primitiva");
            Mesh m, m2;
            Material mat, mat2;
            Assert.IsTrue(PecaDaMeshy.Carregar(nome, out m, out mat), nome + " nao carregou (malha ou URP Lit)");
            Assert.AreEqual(0f, m.bounds.min.y, 0.02f, nome + ": o pe' tem de estar em y = 0 (o desenho poe a peca no chao)");
            Assert.Greater(m.bounds.size.y, 1f, nome + " sem altura");
            Assert.IsNotNull(mat.GetTexture("_BaseMap"), nome + " sem a textura da Meshy: sairia branco");
            Assert.IsTrue(PecaDaMeshy.Carregar(nome, out m2, out mat2));
            Assert.AreSame(mat, mat2, "um material por .glb: todo muro divide o mesmo (lote do SRP)");
        }
    }
}
