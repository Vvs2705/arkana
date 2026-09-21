using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Terrain;

namespace Arkana.Tests
{
    /// <summary>
    /// O GELO QUE SUSTENTA (GDD §14: agua sobre agua congela o lago e vira ROTA; o fogo derrete e quem esta' em cima cai). A
    /// logica ja' dizia Caminhavel e a tela desenhava a laje, mas o corpo nadava POR BAIXO dela. Aqui, puro: onde fica o
    /// colisor da celula (topo na lamina, pegada inteira), a regra do nadador (sobe, nunca fica preso), o pool que devolve
    /// quando derrete, a laje desenhada no pe' e o tiro que pousa no gelo. A casca (o BoxCollider de verdade) e' da foto 59.
    /// </summary>
    public class TerrainGeloTests
    {
        const float Lamina = 0.6f;   // Relevo.LagoY

        static Agua AguaDoLago() => new Agua(new FakeRelevo { AguaFn = (x, z) => Lamina, AlturaFn = (x, z) => -3f });

        [SetUp]
        public void SetUp() { Bus.Reset(); }

        [TearDown]
        public void TearDown() { Bus.Reset(); }

        [Test]
        public void Caixa_TopoNaLamina_PegadaDaCelulaInteira_AteOFundoDoNado()
        {
            float cs = Balance.Terrain.CellSize;
            var centro = new Vector3(12f, -2.3f, -7.5f);   // o y e' o FUNDO do lago: nao entra na caixa
            Bounds b = LeituraDoTerreno.CaixaDoGelo(centro, Lamina, cs);
            Assert.AreEqual(Lamina, b.max.y, 1e-5f, "o topo e' a lamina");
            Assert.AreEqual(12f, b.center.x, 1e-5f, "no centro da celula");
            Assert.AreEqual(-7.5f, b.center.z, 1e-5f);
            Assert.AreEqual(cs, b.size.x, 1e-5f, "a pegada da celula INTEIRA: vizinhas emendam sem fresta");
            Assert.AreEqual(cs, b.size.z, 1e-5f);
            Assert.AreEqual(Agua.PEITO, b.size.y, 1e-5f, "desce ate' o fundo do nado: ninguem passa por baixo");
            Agua agua = AguaDoLago();
            agua.Tick(0.1f, new Vector3(12f, b.max.y, -7.5f));
            Assert.IsFalse(agua.Nadando, "de pe' no topo, a Agua de verdade diz que NAO nada");
        }

        [Test]
        public void Nadador_NaHoraDoCongelamento_SobeParaOTopo_NuncaFicaPresoDentroOuEmbaixo()
        {
            Bounds b = LeituraDoTerreno.CaixaDoGelo(Vector3.zero, Lamina, Balance.Terrain.CellSize);
            var nadando = new Vector3(0.7f, Lamina - Agua.PEITO, -0.4f);   // Agua.Flutuar: o pe' 1,2 m abaixo da lamina
            Vector3 p = LeituraDoTerreno.PorEmCima(nadando, b);
            Assert.AreEqual(Lamina, p.y, 1e-5f, "o nadador sobe para o TOPO");
            Assert.AreEqual(0.7f, p.x, 1e-5f, "no mesmo lugar: nada de ser jogado para o lado");
            Assert.AreEqual(-0.4f, p.z, 1e-5f);
            Assert.AreEqual(Lamina, LeituraDoTerreno.PorEmCima(new Vector3(1f, Lamina - 0.5f, 1f), b).y, 1e-5f, "quem anda no raso tambem sobe");
            var emCima = new Vector3(0f, Lamina, 0f);
            Assert.AreEqual(emCima, LeituraDoTerreno.PorEmCima(emCima, b), "quem ja' esta' em cima fica");
            var noAr = new Vector3(0f, Lamina + 2f, 0f);
            Assert.AreEqual(noAr, LeituraDoTerreno.PorEmCima(noAr, b), "quem pula por cima nao e' puxado para baixo");

            Agua agua = AguaDoLago();
            agua.Tick(0.1f, nadando);
            Assert.IsTrue(agua.Nadando, "preparo: nadava");
            agua.Tick(0.1f, p);
            Assert.IsFalse(agua.Nadando, "no gelo, sai do nado");
            Assert.IsTrue(agua.Encharcado, "e sai encharcado, como de qualquer agua");
        }

        [Test]
        public void Pool_UmPorCelula_DevolveQuandoDerrete_EReusaSemCriar()
        {
            int criados = 0;
            var pool = new PoolDeCelulas<object>(() => { criados++; return new object(); });
            var soltos = new List<object>();
            bool novo;
            object a = pool.Pegar(10, 1, out novo);
            Assert.IsTrue(novo, "celula que acabou de congelar: posicione");
            object b = pool.Pegar(11, 1, out novo);
            pool.Soltar(1, soltos.Add);
            Assert.AreEqual(2, pool.Vivos);
            Assert.AreEqual(0, soltos.Count, "as duas congeladas neste quadro: nada sai");

            Assert.AreSame(a, pool.Pegar(10, 2, out novo));
            Assert.IsFalse(novo, "a mesma celula ainda congelada: o MESMO colisor, sem reposicionar");
            pool.Soltar(2, soltos.Add);   // a 11 derreteu (saiu de Visiveis)
            Assert.AreEqual(1, soltos.Count);
            Assert.AreSame(b, soltos[0], "o colisor da celula que DERRETEU sai");
            Assert.AreEqual(1, pool.Vivos);
            Assert.AreEqual(1, pool.Livres);

            object c = pool.Pegar(30, 3, out novo);
            Assert.IsTrue(novo);
            Assert.AreSame(b, c, "gelo novo reusa o do pool");
            Assert.AreEqual(2, criados, "nada nasce alem do maior numero de celulas juntas");
            pool.Soltar(3, soltos.Add);   // a 10 nao veio no quadro 3: derreteu
            Assert.AreSame(a, soltos[1]);
            Assert.AreEqual(1, pool.Vivos);

            Assert.AreSame(a, pool.Pegar(10, 4, out novo));
            Assert.IsTrue(novo, "recongelou depois de derreter: colisor NOVO (reposiciona e tira quem esta' dentro)");
        }

        [Test]
        public void Laje_Fresca_TopoNoColisor_Derretendo_AfundaSobALamina()
        {
            float fresca = LeituraDoTerreno.BaseDoGelo + LeituraDoTerreno.EspessuraDoGelo(Balance.Terrain.FreezeDuration);
            Assert.That(fresca, Is.InRange(0f, 0.1f), "fresca, o topo desenhado encosta no colisor: o pe' pisa NO gelo, nao 35 cm dentro");
            Assert.Greater(LeituraDoTerreno.EspessuraDoGelo(0.1f), 0f, "afina, mas nao some enquanto a logica segura");
            Assert.Less(LeituraDoTerreno.BaseDoGelo + LeituraDoTerreno.EspessuraDoGelo(0.1f), 0f, "no fim a laje afunda sob a lamina: a rota avisa que vai sumir");
        }

        [Test]
        public void Tiro_PousaNoGelo_NaLaminaOuAbaixo_PorCimaSegue_AguaLiquidaNaoPara()
        {
            var t = new TerrenoReativo(new FakeRelevo { AlturaFn = (x, z) => -3f }, 30f, 7, (cx, cz) => TipoCelula.Agua);
            try
            {
                Vector3 c = t.Centro(t.Idx(5, 5));
                Vector3 abaixo = new Vector3(c.x, Lamina - 0.2f, c.z);
                Assert.IsFalse(LeituraDoTerreno.TiroNoGelo(t, abaixo, Lamina), "agua LIQUIDA nao para o tiro (quem para e' o fundo)");
                t.Reagir(Elemento.Agua, c, false);
                Assert.AreEqual(EstadoCelula.Congelado, t.EstadoEm(c), "preparo: congelou");
                Assert.IsTrue(LeituraDoTerreno.TiroNoGelo(t, abaixo, Lamina), "abaixo da lamina, no gelo: para ali");
                Assert.IsTrue(LeituraDoTerreno.TiroNoGelo(t, new Vector3(c.x, Lamina, c.z), Lamina), "na lamina: pousa");
                Assert.IsFalse(LeituraDoTerreno.TiroNoGelo(t, new Vector3(c.x, Lamina + 0.3f, c.z), Lamina), "por cima do gelo o tiro segue voando");
                Assert.IsFalse(LeituraDoTerreno.TiroNoGelo(null, abaixo, Lamina), "sem terreno, nada");
                t.Reagir(Elemento.Fogo, c, false);
                Assert.IsFalse(LeituraDoTerreno.TiroNoGelo(t, abaixo, Lamina), "derreteu: volta a ser agua");
            }
            finally { t.Desligar(); }
        }
    }
}
