using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Terrain;

namespace Arkana.Tests
{
    /// <summary>
    /// A COSTURA entre Pawn, KitRunner, Partida e TerrenoReativo, sem cena: o kit anuncia o mago (so' do player),
    /// a suprema vazia nao dispara, a Partida entrega os alvos vivos no raio (sem o proprio, se pedido), a lama
    /// entra no produto unico de velocidade e o DoT do chao sai UMA vez, pelo relogio da Partida.
    /// </summary>
    public class GameplayCosturaTests
    {
        private List<object[]> _bounds;
        private int _teles;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset(); Derrubado.Reset();
            Arkana.Menu.Menu.PedidoDeTreino = false;
            _bounds = new List<object[]>(); _teles = 0;
            Bus.KitBound += (s, i) => _bounds.Add(new object[] { s, i });
            Bus.KitTelegraph += (s, t, d, p) => _teles++;
        }

        [TearDown]
        public void TearDown() { Derrubado.Reset(); Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        [Test]
        public void KitRunner_PawnFakeComPyra_RecebeKitBoundTrue_EBotNaoEmite()
        {
            var p = new FakeConjurador("pyra", Vector3.zero);
            var k = new KitRunner("01-pyra", p);
            Assert.AreEqual(1, _bounds.Count, "KitBound sai no bind");
            Assert.AreEqual("01-pyra", _bounds[0][0]);
            Assert.IsTrue((bool)_bounds[0][1], "Pyra tem kit implementado: botoes acesos");
            Assert.IsNotNull(k.Impl);

            var bot = new FakeConjurador("bot-pyra", Vector3.zero, false);
            var kb = new KitRunner("01-pyra", bot);
            Assert.AreEqual(1, _bounds.Count, "bot NAO polui a HUD com KitBound");
            k.Desligar(); kb.Desligar();
        }

        [Test]
        public void Suprema_AntesDeCheia_NaoFazNada()
        {
            var p = new FakeConjurador("pyra", Vector3.zero);
            var k = new KitRunner("01-pyra", p);
            k.Tick(0.5f);
            Assert.Greater(k.CargaSuprema, 0f); Assert.Less(k.CargaSuprema, 1f);
            float carga = k.CargaSuprema;
            Assert.IsFalse(k.UsarSuprema(), "abaixo de 100% e' recusada");
            Assert.AreEqual(carga, k.CargaSuprema, 1e-6f, "e a carga fica como estava");
            Assert.AreEqual(0f, k.Telegrafia, "nada agendado");
            Assert.AreEqual(0, _teles, "nada avisado");
            k.Desligar();
        }

        [Test]
        public void Partida_AlvosNoRaio_SoVivosDentroDoRaio_ENuncaOProprioSePedido()
        {
            var m = new Partida(new FakeRelevo());
            m.Iniciar(1, 3, false);
            var eu = new FakeEntidade("eu", Vector3.zero);
            var perto = new FakeEntidade("perto", new Vector3(3f, 0f, 0f));
            var morto = new FakeEntidade("morto", new Vector3(2f, 0f, 0f));
            var longe = new FakeEntidade("longe", new Vector3(50f, 0f, 0f));
            morto.Vital.Hp = 0f;
            foreach (IEntidade e in new IEntidade[] { eu, perto, morto, longe }) m.Registrar(e, null);

            IEntidade[] r = m.AlvosNoRaio(eu.Pos, 10f, eu);
            Assert.AreEqual(1, r.Length, "so' quem esta' vivo e dentro do raio");
            Assert.AreSame(perto, r[0]);
            Assert.IsFalse(System.Array.IndexOf(r, eu) >= 0, "nunca o proprio");
            Assert.IsFalse(System.Array.IndexOf(r, morto) >= 0, "morto nao e' alvo");
            Assert.IsFalse(System.Array.IndexOf(r, longe) >= 0, "fora do raio nao entra");

            IEntidade[] todos = m.AlvosNoRaio(eu.Pos, 10f);
            Assert.AreEqual(2, todos.Length, "sem `excluir` o pawn vem junto (IConjurador: o kit exclui a dona)");
            m.Encerrar();
        }

        [Test]
        public void Lama_EntraNoProdutoUnicoDeVelocidade()
        {
            var t = new TerrenoReativo(new FakeRelevo(), 60f, 7, (cx, cz) => TipoCelula.Chao);
            Vector3 c = t.Centro(t.Idx(10, 10));
            Vector3 seco = t.Centro(t.Idx(2, 2));
            t.Reagir(Elemento.Agua, c, false);   // agua no chao de terra = lamacal
            Assert.AreEqual(Balance.Terrain.MudSlow, t.FatorTerreno(c), 1e-4f, "lama = 0.55");
            Assert.AreEqual(1f, t.FatorTerreno(seco), 1e-4f);

            Assert.AreEqual(Balance.Player.Speed * Balance.Terrain.MudSlow, Pawn.VelocidadeMaxima(1f, t.FatorTerreno(c), 1f, 1f), 1e-4f, "a lama entra no produto");
            Assert.AreEqual(Balance.Player.Speed, Pawn.VelocidadeMaxima(1f, t.FatorTerreno(seco), 1f, 1f), 1e-4f, "seco = base");
            Assert.AreEqual(Balance.Player.Speed * Agua.NADO_MULT * Balance.Terrain.MudSlow, Pawn.VelocidadeMaxima(Agua.NADO_MULT, Balance.Terrain.MudSlow, 1f, 1f), 1e-4f, "agua e lama multiplicam (dois fatores de terreno)");
            Assert.AreEqual(Balance.Player.Speed * Balance.Terrain.MudSlow * 0.9f * Derrubado.RASTEJO, Pawn.VelocidadeMaxima(1f, Balance.Terrain.MudSlow, 0.9f, Derrubado.RASTEJO), 1e-4f, "status e postura seguem no produto");
            t.Desligar();
        }

        [Test]
        public void Partida_TickaOTerreno_EOFogoDoChaoCobraPeloPontoUnico()
        {
            var t = new TerrenoReativo(new FakeRelevo(), 60f, 7, (cx, cz) => TipoCelula.Combustivel);
            var m = new Partida(new FakeRelevo()) { Terreno = t };
            m.Iniciar(2, 1, false);
            Vector3 c = t.Centro(t.Idx(10, 10));
            var v = new FakeEntidade("v", c);
            m.Registrar(v, null);
            t.Reagir(Elemento.Fogo, c, false);
            Assert.AreEqual(EstadoCelula.Queimando, t.EstadoEm(c));
            float hp0 = v.Vital.Hp;
            m.Tick(Balance.Dot.Tick + 0.05f);
            Assert.Less(v.Vital.Hp, hp0, "a Partida ticka o terreno: quem pisa fogo paga DoT (direto na vida)");
            m.Encerrar(); t.Desligar();
        }
    }
}
