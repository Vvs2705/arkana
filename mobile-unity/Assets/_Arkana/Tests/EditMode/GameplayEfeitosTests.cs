using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>Efeitos no alvo (_test_elementos_no_alvo) e o projetil (_test_loot: o projetil le' a arma).</summary>
    public class GameplayEfeitosTests
    {
        private List<object[]> _status;
        private List<object[]> _danos;
        private List<object[]> _disparos;
        private List<Elemento> _casts;
        private List<object[]> _terrain;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Combat.Reset();
            Efeitos.Reset();
            _status = new List<object[]>(); _danos = new List<object[]>(); _disparos = new List<object[]>();
            _casts = new List<Elemento>(); _terrain = new List<object[]>();
            Bus.StatusAplicado += (a, n) => _status.Add(new object[] { a, n });
            Bus.DamageApplied += (t, a, e, s, esc) => _danos.Add(new object[] { t, a, e, s, esc });
            Bus.Disparo += (p, pos) => _disparos.Add(new object[] { p, pos });
            Bus.SpellCast += e => _casts.Add(e);
            Bus.TerrainHit += (e, p, s) => _terrain.Add(new object[] { e, p, s });
        }

        [TearDown]
        public void TearDown() { Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        [Test]
        public void Reacoes_FogoAcende_AguaExtingueEMolha_Hipotermia_Conducao_Vapor_Aticar()
        {
            var a = new FakeEntidade("a", Vector3.zero);
            Efeitos.Aplicar(a, Elemento.Fogo, 10f, null);
            Efeitos.EstadoAlvo s = Efeitos.De(a);
            Assert.AreEqual(Balance.Status.BurnDps, s.BurnDps, 0.001f, "FOGO acende");
            Assert.AreEqual(Balance.Status.BurnDur, s.BurnLeft, 0.001f);
            Assert.AreEqual(1, _status.Count); Assert.AreEqual("burn", _status[0][1]);
            Efeitos.Aplicar(a, Elemento.Fogo, 10f, null);
            Assert.AreEqual(1, _status.Count, "StatusAplicado so' na BORDA: acender de novo nao repete");

            Efeitos.Aplicar(a, Elemento.Agua, 10f, null);
            Assert.AreEqual(0f, s.BurnLeft, "AGUA em quem queima: EXTINCAO");
            Assert.Greater(s.WetLeft, 0f, "e molha");
            Assert.AreEqual(Balance.Status.WetSlow, s.StatusMult, 0.001f, "molhado lentifica 10%");
            Efeitos.Aplicar(a, Elemento.Agua, 10f, null);
            Assert.AreEqual(Balance.Status.HypothermiaSlow, s.StatusMult, 0.001f, "AGUA em molhado: HIPOTERMIA");

            float m = Efeitos.Aplicar(a, Elemento.Raio, 10f, null);
            Assert.AreEqual(Balance.Status.ConductMult, m, 0.001f, "RAIO em molhado: CONDUCAO +50%");
            Assert.AreEqual(Balance.Status.ConductStun, s.StunLeft, 0.001f, "conducao atordoa 0.4s");

            var v = new FakeEntidade("v", Vector3.zero);
            Efeitos.Aplicar(v, Elemento.Agua, 10f, null);
            Efeitos.Aplicar(v, Elemento.Fogo, 10f, null);
            Assert.AreEqual(0f, Efeitos.De(v).BurnLeft, "FOGO em molhado: VAPOR (nao acende)");
            Assert.AreEqual(0f, Efeitos.De(v).WetLeft, "e o molhado some");

            var f = new FakeEntidade("f", Vector3.zero);
            Efeitos.Aplicar(f, Elemento.Fogo, 10f, null);
            float dur0 = Efeitos.De(f).BurnLeft;
            Efeitos.Aplicar(f, Elemento.Vento, 10f, null);
            Assert.AreEqual(Balance.Status.BurnFannedDps, Efeitos.De(f).BurnDps, 0.001f, "VENTO em quem queima: ATICAR");
            Assert.AreEqual(dur0 + Balance.Status.BurnFannedBonus, Efeitos.De(f).BurnLeft, 0.001f, "+2s");

            var seco = new FakeEntidade("seco", Vector3.zero);
            Assert.AreEqual(1f, Efeitos.Aplicar(seco, Elemento.Raio, 10f, null), 0.001f, "RAIO em seco: sem conducao");
            Assert.AreEqual(0f, Efeitos.De(seco).StunLeft, "e sem atordoamento");
            Efeitos.De(seco).IframesLeft = 0.1f;
            Efeitos.Aplicar(seco, Elemento.Fogo, 10f, null);
            Assert.AreEqual(0f, Efeitos.De(seco).BurnLeft, "i-frames barram tambem o ESTADO");
        }

        [Test]
        public void Exclusividade_NuncaQueimandoEMolhado_StunNuncaPassaDoTeto()
        {
            var x = new FakeEntidade("x", Vector3.zero);
            foreach (Elemento el in new[] { Elemento.Fogo, Elemento.Agua, Elemento.Fogo, Elemento.Vento, Elemento.Agua, Elemento.Raio, Elemento.Fogo })
            {
                Efeitos.Aplicar(x, el, 10f, null);
                Efeitos.EstadoAlvo s = Efeitos.De(x);
                Assert.IsFalse(s.BurnLeft > 0f && s.WetLeft > 0f, "estado TERMICO e' exclusivo");
            }
            Efeitos.Atordoar(x, 5f);
            Assert.AreEqual(Balance.Status.StunCap, Efeitos.De(x).StunLeft, 0.001f, "atordoamento tem teto de 0.8s");
            Efeitos.Atordoar(x, 0.1f);
            Assert.AreEqual(Balance.Status.StunCap, Efeitos.De(x).StunLeft, 0.001f, "e um stun menor nao encurta o que ja' vale");
            var coisa = new FakeEntidade("muro", Vector3.zero) { Vital = null };
            Assert.AreEqual(1f, Efeitos.Aplicar(coisa, Elemento.Raio, 10f, null), "elemento em estrutura nao reage aqui");
        }

        [Test]
        public void Arco_ConducaoPulaParaOutroMolhadoPerto_NaoParaLonge()
        {
            var p1 = new FakeEntidade("p1", Vector3.zero);
            var p2 = new FakeEntidade("p2", new Vector3(Balance.Status.ConductArcM - 1f, 0f, 0f));
            var p3 = new FakeEntidade("p3", new Vector3(Balance.Status.ConductArcM + 5f, 0f, 0f));
            var todos = new List<IEntidade> { p1, p2, p3 };
            Efeitos.Aplicar(p1, Elemento.Agua, 10f, null);
            Efeitos.Aplicar(p2, Elemento.Agua, 10f, null);
            Efeitos.Aplicar(p3, Elemento.Agua, 10f, null);
            float sh2 = p2.Vital.Escudo, sh3 = p3.Vital.Escudo;
            Efeitos.Aplicar(p1, Elemento.Raio, 20f, null, todos);
            Assert.Less(p2.Vital.Escudo, sh2, "conducao ARCA para outro molhado a menos de 4m");
            Assert.AreEqual(sh3, p3.Vital.Escudo, 0.001f, "molhado LONGE nao recebe o arco");
        }

        [Test]
        public void Tick_QueimaduraDoiPeloPontoUnicoEApagaNoTempo()
        {
            var a = new FakeEntidade("a", Vector3.zero);
            Efeitos.Aplicar(a, Elemento.Fogo, 10f, null);
            float hp = a.Vital.Hp;
            for (int i = 0; i < 60; i++) { Combat.TickDot(1f / 60f); Efeitos.Tick(a, 1f / 60f); }
            Assert.Less(a.Vital.Hp, hp, "a queimadura DOI (DoT direto na vida)");
            Assert.AreEqual(hp - Balance.Status.BurnDps, a.Vital.Hp, 0.5f, "no ritmo do dps (4/s), em tiques de 0,25 s");
            Assert.AreEqual(Balance.Escudo.Niveis[0], a.Vital.Escudo, 0.001f, "DoT ignora o escudo");
            for (int i = 0; i < 180; i++) { Combat.TickDot(1f / 60f); Efeitos.Tick(a, 1f / 60f); }
            Assert.AreEqual(0f, Efeitos.De(a).BurnLeft, "apaga em BurnDur");
            Assert.AreEqual(0f, Efeitos.De(a).BurnDps);
            Assert.AreEqual("", Efeitos.De(a).Nome());
        }

        // ------------------------------------------------------------- projetil

        [Test]
        public void Projetil_LancarEOPontoUnico_LeAArmaDoAtirador_AcertoDelegado()
        {
            var bot = new FakeEntidade("bot", Vector3.zero);
            var slot = new ArmaSlot(bot);
            slot.Equipar(Arma.CAJADO, null, Elemento.Fogo);
            Projetil pc = Projetil.Lancar(bot, Vector3.zero, Vector3.forward, Elemento.Fogo, slot);
            Assert.AreEqual(Arma.Spec(Elemento.Fogo, Arma.CAJADO).Dmg, pc.Dano, 0.001f, "carrega o dano DA ARMA");
            Assert.AreEqual(Arma.Spec(Elemento.Fogo, Arma.CAJADO).Range, pc.AlcanceRestante, 0.001f, "voa o alcance DA ARMA");
            Assert.AreEqual(1, _disparos.Count, "TODO tiro passa por Disparo");
            Assert.AreSame(bot, _disparos[0][0]);
            Assert.AreEqual(0, _casts.Count, "SpellCast e' so' do player");

            var player = new FakeEntidade("player", Vector3.zero, true);
            Projetil pv = Projetil.Lancar(player, Vector3.zero, Vector3.forward, Elemento.Fogo);
            Assert.AreEqual(Balance.Fogo.Dmg, pv.Dano, 0.001f, "sem arma segue o Balance puro");
            Assert.AreEqual(1, _casts.Count);
            Assert.AreEqual(Projetil.Forma.Esfera, pv.FormaDoTiro);
            Assert.AreEqual(Projetil.Forma.Dardo, Projetil.FormaDe(Elemento.Raio), "cor + FORMA por elemento");

            // voa o alcance e morre; nunca acerta o proprio atirador
            int passos = 0;
            while (pv.Tick(1f / 60f, pos => player) && passos < 100000) passos++;
            Assert.AreEqual(0, _danos.Count, "o atirador nao se acerta");
            Assert.IsFalse(pv.Vivo);
            Assert.AreEqual(Balance.Fogo.Range, pv.Pos.z, 1f, "morreu no fim do alcance");

            // acerto delegado: a casca resolve a fisica, o projetil resolve o jogo
            var alvo = new FakeEntidade("alvo", new Vector3(0f, 0f, 5f));
            Projetil ph = Projetil.Lancar(player, Vector3.zero, Vector3.forward, Elemento.Terra);
            _danos.Clear(); _terrain.Clear();
            passos = 0;
            while (ph.Tick(1f / 60f, pos => Vector3.Distance(pos, alvo.Pos) <= 0.5f ? alvo : null) && passos < 1000) passos++;
            Assert.IsFalse(ph.Vivo, "acertou: o projetil acabou");
            Assert.AreEqual(1, _danos.Count, "dano SO' pelo Combat, uma vez");
            Assert.AreSame(alvo, _danos[0][0]);
            Assert.AreEqual(Elemento.Terra, _danos[0][2]);
            Assert.AreEqual(1, _terrain.Count, "TerrainHit SEMPRE: o terreno decide se reage");

            Projetil pa = Projetil.Lancar(player, Vector3.zero, Vector3.forward, Elemento.Agua);
            for (int i = 0; i < 30; i++) pa.Tick(1f / 60f);
            Assert.Less(pa.Pos.y, 0f, "a agua tem arco leve");
            Assert.AreEqual(Balance.Combate.Knockback * Balance.Vento.Empurrao, Projetil.Lancar(bot, Vector3.zero, Vector3.forward, Elemento.Vento, null).Empurrao(true), 0.001f, "vento empurra 2x no chao");
            Assert.AreEqual(Balance.Combate.Knockback, Projetil.Lancar(bot, Vector3.zero, Vector3.forward, Elemento.Vento, null).Empurrao(false), 0.001f, "nada de juggle no ar");
        }
    }
}
