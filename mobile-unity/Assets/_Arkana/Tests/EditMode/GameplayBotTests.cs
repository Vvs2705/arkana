using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>Percepcao (selftest.gd _test_bot_percepcao), Lei das Luvas no bot e decisao, em C#.</summary>
    public class GameplayBotTests
    {
        private FakeEntidade _eu;
        private PercepcaoBot _p;
        private Dictionary<IEntidade, float> _vel;
        private List<IEntidade> _magos;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Efeitos.Reset();
            _eu = new FakeEntidade("cacador", Vector3.zero);
            _p = new PercepcaoBot(_eu);
            _p.Ligar();
            _vel = new Dictionary<IEntidade, float>();
            _magos = new List<IEntidade> { _eu };
        }

        private float VelDe(IEntidade e) { float v; return _vel.TryGetValue(e, out v) ? v : 0f; }
        private FakeEntidade Mago(string nome, float x, float vel = 0f)
        {
            var m = new FakeEntidade(nome, new Vector3(x, 0f, 0f));
            _magos.Add(m); _vel[m] = vel;
            return m;
        }

        [Test]
        public void NasceCego_ParadoA15mNaoENotado_AndandoOsPassosEntregam()
        {
            Assert.IsNull(_p.Alvo, "bot nasce SEM alvo");
            FakeEntidade quieto = Mago("quieto", 15f);
            _p.Varrer(_magos, VelDe);
            Assert.IsNull(_p.Alvo, "parado a 15m NAO e' notado — ficar imovel esconde");
            _vel[quieto] = 4f;
            _p.Varrer(_magos, VelDe);
            Assert.AreSame(quieto, _p.Alvo, "andando a 15m os PASSOS entregam");
            quieto.Vital.Hp = 0f;
            _p.Varrer(_magos, VelDe);
            Assert.IsNull(_p.Alvo, "alvo morto e' esquecido");
        }

        [Test]
        public void Visto_ParadoA8m_EFFA_BotCacaBot()
        {
            FakeEntidade perto = Mago("perto", 8f);
            _p.Varrer(_magos, VelDe);
            Assert.AreSame(perto, _p.Alvo, "parado a 8m e' VISTO");
            _p.Esquecer();
            perto.Pos = new Vector3(100f, 0f, 0f);
            FakeEntidade rival = Mago("rival-bot", 6f);
            _p.Varrer(_magos, VelDe);
            Assert.AreSame(rival, _p.Alvo, "FFA: bot caca bot");
        }

        [Test]
        public void OuvidoA15m_EsquecidoAlemDaMemoria()
        {
            FakeEntidade a = Mago("a", 15f, 4f);
            _p.Varrer(_magos, VelDe);
            Assert.AreSame(a, _p.Alvo);
            a.Pos = new Vector3(PercepcaoBot.MEMORIA + 5f, 0f, 0f);
            _p.Varrer(_magos, VelDe);
            Assert.IsNull(_p.Alvo, "alem da memoria o rastro se perde");
        }

        [Test]
        public void Disparo_DenunciaA25m_ViaBus_MasNaoA35_NemOProprio()
        {
            FakeEntidade atirador = Mago("atirador", 25f);
            Bus.EmitDisparo(atirador, atirador.Pos);
            Assert.AreSame(atirador, _p.Alvo, "conjurar DENUNCIA a 25m");
            Assert.AreEqual(atirador.Pos, _p.ConsumirPista().Value, "anda ate' onde ouviu");
            _p.Esquecer();
            FakeEntidade longe = Mago("longe", 35f);
            Bus.EmitDisparo(longe, longe.Pos);
            Assert.IsNull(_p.Alvo, "a 35m nao se ouve o disparo");
            Bus.EmitDisparo(_eu, _eu.Pos);
            Assert.IsNull(_p.Alvo, "o proprio disparo nao vira alvo");
        }

        [Test]
        public void Disparo_NaoRoubaBrigaMaisPerta()
        {
            FakeEntidade perto = Mago("perto", 5f);
            _p.Varrer(_magos, VelDe);
            FakeEntidade longe = Mago("longe", 20f);
            Bus.EmitDisparo(longe, longe.Pos);
            Assert.AreSame(perto, _p.Alvo, "um tiro longe nao rouba a atencao de uma briga em curso");
        }

        [Test]
        public void Revide_TomarDanoEnsinaQuemBateu_MesmoA60m()
        {
            FakeEntidade sniper = Mago("sniper", 60f);
            Combat.AplicarDano(_eu, 12f, Elemento.Fogo, sniper);
            Assert.AreSame(sniper, _p.Alvo, "tomar dano ensina quem atacou");
        }

        [Test]
        public void Desligar_CalaOsOuvidos()
        {
            _p.Desligar();
            FakeEntidade atirador = Mago("atirador", 10f);
            Bus.EmitDisparo(atirador, atirador.Pos);
            Assert.IsNull(_p.Alvo);
        }

        [Test]
        public void Tick_VarreSoACadaPercepcaoS()
        {
            Mago("perto", 5f);
            _p.Tick(PercepcaoBot.PERCEPCAO_S * 0.5f, _magos, VelDe);
            Assert.IsNull(_p.Alvo);
            _p.Tick(PercepcaoBot.PERCEPCAO_S * 0.5f, _magos, VelDe);
            Assert.IsNotNull(_p.Alvo);
        }

        // ---------------------------------------------------------------- decisao

        [Test]
        public void Desarmado_EscolheALuvaMaisProxima_ENaoAtira()
        {
            var loot = new List<LootItem>
            {
                new LootItem(Arma.CAJADO, null, Elemento.Raio, new Vector3(30f, 0f, 0f)),
                new LootItem(Arma.VARINHA, null, Elemento.Fogo, new Vector3(0f, 0f, 9f)),
            };
            Assert.AreSame(loot[1], DecisaoBot.LuvaMaisPerto(Vector3.zero, loot));
            var d = new DecisaoBot(7);
            FakeEntidade presa = Mago("presa", 5f);
            DecisaoBot.Saida s = d.Decidir(0.1f, Vector3.zero, false, presa, null, loot, null, null, true);
            Assert.IsFalse(s.Atirar, "sem luva NAO ha' ataque basico (Lei das Luvas vale para o bot)");
            Assert.Greater(s.Dir.z, 0.99f, "desarmado, o vagar aponta para a luva mais perto");
        }

        [Test]
        public void Armado_PersegueEAtiraComErro_Determinista()
        {
            FakeEntidade presa = Mago("presa", 15f);
            var d = new DecisaoBot(3);
            DecisaoBot.Saida s = d.Decidir(0.1f, Vector3.zero, true, presa, null, null, null, null, true);
            Assert.IsFalse(s.Atirar, "a 15m persegue (ATTACK_DIST 12)");
            Assert.Greater(s.Dir.x, 0.99f);
            presa.Pos = new Vector3(8f, 0f, 0f);
            s = d.Decidir(0.1f, Vector3.zero, true, presa, null, null, null, null, true);
            Assert.IsTrue(s.Atirar, "a 8m atira");
            Assert.AreEqual(0f, s.Dir.magnitude, 1e-4f, "atirando, para");
            Assert.Greater(s.Encarar.x, 0.99f);
            float ang = Vector3.Angle(Vector3.right, new Vector3(s.DirTiro.x, 0f, s.DirTiro.z)) * Mathf.Deg2Rad;
            Assert.LessOrEqual(ang, DecisaoBot.AIM_SPREAD + 1e-3f, "erro de mira dentro do spread");
            var d2 = new DecisaoBot(3);
            d2.Decidir(0.1f, Vector3.zero, true, presa, null, null, null, null, true);
            Assert.AreEqual(d.SaltoEm, d2.SaltoEm, "mesmo seed, mesmo relogio de salto");
            Assert.GreaterOrEqual(d.SaltoEm, DecisaoBot.SALTO_MIN);
            Assert.LessOrEqual(d.SaltoEm, DecisaoBot.SALTO_MAX);
        }

        [Test]
        public void Zona_PuxaParaOCentro_AcimaDaPresa()
        {
            var zona = new Zona(new FakeRelevo(), 1);
            zona.ForcarCirculo(2, Vector3.zero, 20f);
            var d = new DecisaoBot(1);
            FakeEntidade presa = Mago("presa", 45f);
            Vector3 pos = new Vector3(40f, 0f, 0f);
            DecisaoBot.Saida s = d.Decidir(0.1f, pos, true, presa, null, null, zona, null, true);
            Assert.Less(s.Dir.x, -0.99f, "fora da margem, corre para o centro e nao atras da presa");
        }

        [Test]
        public void AutoUpgrade_SoTierMaior()
        {
            var bot = new FakeEntidade("bot", Vector3.zero);
            var slot = new ArmaSlot(bot) { AutoUpgrade = true };
            var loot = new Loot(null);
            loot.Itens.Add(new LootItem(Arma.CAJADO, null, Elemento.Raio, Vector3.zero));
            Assert.IsTrue(loot.TentarAutoUpgrade(slot), "maos nuas: qualquer luva e' upgrade");
            Assert.AreEqual(Arma.CAJADO, slot.ArmaId);
            loot.Itens.Clear();
            loot.Itens.Add(new LootItem(Arma.VARINHA, null, Elemento.Fogo, Vector3.zero));
            Assert.IsFalse(loot.TentarAutoUpgrade(slot), "tier menor nao troca");
            Assert.AreEqual(Arma.CAJADO, slot.ArmaId);
            loot.Itens.Add(new LootItem(Arma.MANOPLA, (Elemento[])Arma.PARES_MANOPLA[0].Clone(), null, Vector3.zero));
            Assert.IsTrue(loot.TentarAutoUpgrade(slot), "tier maior troca");
            Assert.AreEqual(Arma.MANOPLA, slot.ArmaId);
        }
    }
}
