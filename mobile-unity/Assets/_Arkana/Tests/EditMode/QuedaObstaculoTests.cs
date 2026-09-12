using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// O POUSO NO TOPO (foto de 11/09, seed 3103): a Queda so' conhecia o terreno e o mago pousava DENTRO da pedra;
    /// a camera ficava no avesso da peca. Com o ChaoComObstaculos a Queda enxerga o topo do que estiver no caminho.
    /// Prova em vermelho: com o FakeRelevo cru (o antes), o pouso sai em y = 0, dentro da pedra.
    /// </summary>
    public class QuedaObstaculoTests
    {
        [SetUp] public void SetUp() { Bus.Reset(); }

        static GameObject Pedra()
        {
            var pedra = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedra.transform.position = new Vector3(0f, 2f, 0f);
            pedra.transform.localScale = new Vector3(4f, 4f, 4f);   // topo em y = 4
            Physics.SyncTransforms();
            return pedra;
        }

        static Queda CairDe(IRelevo chao, Vector3 de)
        {
            var q = new Queda(chao, de, false);
            q.Saltar();
            for (int i = 0; i < 5000 && q.NoAr; i++) q.Tick(1f / 60f);
            return q;
        }

        [Test]
        public void EmCimaDaPedra_OChaoEOTopoDela()
        {
            GameObject pedra = Pedra();
            try
            {
                var chao = new ChaoComObstaculos(new FakeRelevo());
                Assert.AreEqual(4f, chao.Altura(0f, 0f), 0.01f, "em cima da pedra, o chao e' o topo dela");
                Assert.AreEqual(0f, chao.Altura(10f, 0f), 0.01f, "fora dela, o terreno");
            }
            finally { Object.DestroyImmediate(pedra); }
        }

        [Test]
        public void AQueda_PousaNoTopo_NaoDentro()
        {
            GameObject pedra = Pedra();
            try
            {
                Queda antes = CairDe(new FakeRelevo(), new Vector3(0f, 120f, 0f));
                Assert.AreEqual(0f, antes.Pos.y, 0.05f, "o defeito, documentado: o terreno nu pousa dentro da pedra");

                Queda q = CairDe(new ChaoComObstaculos(new FakeRelevo()), new Vector3(0f, 120f, 0f));
                Assert.IsFalse(q.NoAr, "pousou");
                Assert.AreEqual(4f, q.Pos.y, 0.05f, "pousa NO TOPO da pedra");
            }
            finally { Object.DestroyImmediate(pedra); }
        }

        [Test]
        public void CorpoDeMago_NaoEChao()
        {
            var mago = new GameObject("Mago");
            try
            {
                mago.AddComponent<CharacterController>();   // capsula de 2 m em volta da origem: topo em y = 1
                Physics.SyncTransforms();
                Assert.AreEqual(0f, new ChaoComObstaculos(new FakeRelevo()).Altura(0f, 0f), 0.01f, "ninguem pousa na cabeca do outro");
            }
            finally { Object.DestroyImmediate(mago); }
        }
    }
}
