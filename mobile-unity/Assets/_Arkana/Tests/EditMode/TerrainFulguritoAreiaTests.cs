using NUnit.Framework;
using Arkana.Terrain;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// FULGURITO — o que e' AREIA na IlhaMestre (a ilha do jogo hoje). Conselho Modo B ciclo 1, LP nº 2 / TD nº 2: a regra vivia
    /// so' na casca e ja' falhou uma vez em silencio com a suite verde (Fulgurito inerte na IlhaMestre, defeito S2 nº 1).
    /// Chao SECO entre Relevo.PraiaY (1,4 m) e FulguritoBehaviour.AreiaMaxY (3,75 m), bordas inclusas.
    /// Vistos VERMELHOS com o defeito plantado ('<' no teto e sem olhar a agua) — evidencia em production/qa/evidence/.
    /// </summary>
    public class TerrainFulguritoAreiaTests
    {
        [Test]
        public void ChaoSecoNaFaixaDePraia_EhAreia()
        {
            Assert.IsTrue(FulguritoBehaviour.AreiaDaMestre(2.5f, Relevo.Seco), "meio da faixa de praia, seco");
            Assert.IsTrue(FulguritoBehaviour.AreiaDaMestre(Relevo.PraiaY, Relevo.Seco), "borda de baixo inclusa");
            Assert.IsTrue(FulguritoBehaviour.AreiaDaMestre(FulguritoBehaviour.AreiaMaxY, Relevo.Seco), "borda de cima inclusa");
        }

        [Test]
        public void ChaoMolhado_NaoEhAreia()
        {
            Assert.IsFalse(FulguritoBehaviour.AreiaDaMestre(2.5f, 0f), "debaixo d'agua (mar) nao vitrifica");
            Assert.IsFalse(FulguritoBehaviour.AreiaDaMestre(2.5f, 3f), "beira do lago (superficie acima do chao) nao vitrifica");
        }

        [Test]
        public void ForaDaFaixaDeAltura_NaoEhAreia()
        {
            Assert.IsFalse(FulguritoBehaviour.AreiaDaMestre(FulguritoBehaviour.AreiaMaxY + 0.01f, Relevo.Seco), "acima de 3,75 m e' grama/rocha");
            Assert.IsFalse(FulguritoBehaviour.AreiaDaMestre(Relevo.PraiaY - 0.01f, Relevo.Seco), "abaixo de PraiaY nada de gameplay pousa");
            Assert.IsFalse(FulguritoBehaviour.AreiaDaMestre(float.NaN, Relevo.Seco), "altura quebrada nao vira areia");
        }
    }
}
