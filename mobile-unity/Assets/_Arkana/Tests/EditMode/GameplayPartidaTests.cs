using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>O veredito, o relogio, o treino e os projeteis em voo (Main.gd + selftest _test_treino), em C#.</summary>
    public class GameplayPartidaTests
    {
        private const float DT = 1f / 60f;
        private Partida _m;
        private List<bool> _fins;
        private int _inicios;
        private List<string> _abates;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Arkana.Menu.Menu.PedidoDeTreino = false;
            _fins = new List<bool>(); _abates = new List<string>(); _inicios = 0;
            Bus.MatchOver += v => _fins.Add(v);
            Bus.MatchStarted += () => _inicios++;
            Bus.PlayerKilledBot += n => _abates.Add(n);
            _m = new Partida(new FakeRelevo());
        }

        [TearDown]
        public void TearDown() { _m.Encerrar(); }

        private FakeEntidade Registrar(string nome, Vector3 pos, bool player = false)
        {
            var e = new FakeEntidade(nome, pos, player);
            _m.Registrar(e, new ArmaSlot(e));
            return e;
        }

        private static void Matar(IEntidade e, IEntidade por = null)
        {
            Combat.AplicarDano(e, 1000f, Elemento.Fogo, por, false, true);
        }

        [Test]
        public void Vitoria_AoFicarSozinho_MatchOverUmaVez()
        {
            _m.Iniciar(11, 2, false);
            Assert.AreEqual(1, _inicios, "MatchStarted no inicio");
            Assert.AreSame(_m, Partida.Atual);
            FakeEntidade p = Registrar("player", Vector3.zero, true);
            FakeEntidade b1 = Registrar("b1", new Vector3(30f, 0f, 0f));
            FakeEntidade b2 = Registrar("b2", new Vector3(-30f, 0f, 0f));
            Assert.AreEqual(2, _m.BotsVivos);
            Matar(b1, p);
            Assert.AreEqual(1, _m.BotsVivos);
            Assert.AreEqual(0, _fins.Count, "com um bot vivo a partida segue");
            Matar(b2, p);
            Assert.AreEqual(0, _m.BotsVivos);
            Assert.AreEqual(1, _fins.Count);
            Assert.IsTrue(_fins[0], "ultimo em pe' = vitoria");
            Assert.IsTrue(_m.Acabou);
            _m.Fim(false);
            _m.Tick(DT);
            Assert.AreEqual(1, _fins.Count, "MatchOver so' UMA vez");
        }

        [Test]
        public void Derrota_QuandoOPlayerMorre_EPeloRelogio()
        {
            _m.Iniciar(11, 1, false);
            FakeEntidade p = Registrar("player", Vector3.zero, true);
            Registrar("b1", new Vector3(30f, 0f, 0f));
            Matar(p);
            Assert.AreEqual(1, _fins.Count);
            Assert.IsFalse(_fins[0], "player morto = derrota");

            var m2 = new Partida(new FakeRelevo());
            m2.Iniciar(12, 1, false);
            Registrar("player2", Vector3.zero, true);
            Assert.AreEqual(Balance.Match.DurationS, m2.Restante, 1e-3f);
            m2.Tick(Balance.Match.DurationS + 1f);
            Assert.AreEqual(0f, m2.Restante, 1e-4f);
            Assert.AreEqual(2, _fins.Count);
            Assert.IsFalse(_fins[1], "tempo esgotado e' a rede de seguranca: derrota");
            m2.Encerrar();
        }

        [Test]
        public void Treino_SemZona_SemRelogio_TresLuvasNoSpawn_BonecoRegenera()
        {
            _m.Iniciar(5, 3, true, new Vector3(10f, 0f, 10f));
            Assert.IsTrue(_m.Treino);
            Assert.IsNull(_m.Zona, "treino nao tem tempestade");
            Assert.IsNull(_m.Bau);
            Assert.AreEqual(0, _m.BotsVivos, "bonecos nao contam vitoria");
            Assert.AreEqual(3, _m.Loot.Itens.Count, "as tres luvas em fila");
            var ids = new HashSet<string>();
            foreach (LootItem l in _m.Loot.Itens) { ids.Add(l.ArmaId); Assert.Less(Vector3.Distance(l.Pos, new Vector3(10f, 0f, 10f)), 8f, "a passos do spawn"); }
            Assert.IsTrue(ids.Contains(Arma.VARINHA) && ids.Contains(Arma.CAJADO) && ids.Contains(Arma.MANOPLA));
            Assert.AreEqual(Partida.SUPREMA_TREINO_S, _m.SupremaCargaS(50f), "suprema em 5 s");

            FakeEntidade p = Registrar("player", new Vector3(10f, 0f, 10f), true);
            var boneco = new FakeEntidade("boneco", new Vector3(18f, 0f, 8f));
            _m.RegistrarBoneco(boneco);
            float antes = _m.Restante;
            _m.Tick(10f);
            Assert.AreEqual(antes, _m.Restante, 1e-4f, "treino nao tem relogio");
            Combat.AplicarDano(boneco, 40f, Elemento.Fogo, p, false, true);
            float ferido = boneco.Vital.Hp;
            _m.Tick(1f);
            Assert.Greater(boneco.Vital.Hp, ferido, "o boneco regenera");
            Matar(boneco, p);
            Assert.AreEqual(0, _fins.Count, "matar boneco nao fecha a partida");
            _m.Tick(DT);
            Assert.IsTrue(boneco.Vital.Viva, "o boneco volta");
        }

        [Test]
        public void PedidoDeTreino_EConsumido_NaoHerdaParaAProxima()
        {
            Arkana.Menu.Menu.PedidoDeTreino = true;
            _m.Iniciar(1, 3, false);
            Assert.IsTrue(_m.Treino, "o pedido do menu vira treino");
            Assert.IsFalse(Arkana.Menu.Menu.PedidoDeTreino, "e e' ZERADO ao consumir");
            _m.Iniciar(2, 3, false);
            Assert.IsFalse(_m.Treino, "a proxima partida nasce normal");
            Assert.IsNotNull(_m.Zona);
        }

        [Test]
        public void Projetil_RegistradoVoaEAcerta_MasNaoEmIframes()
        {
            _m.Iniciar(9, 1, false);
            FakeEntidade p = Registrar("player", Vector3.zero, true);
            FakeEntidade b = Registrar("b1", new Vector3(6f, 0f, 0f));
            ArmaSlot slot = _m.SlotDe(p);
            slot.Equipar(Arma.VARINHA, null, Elemento.Fogo);
            Projetil tiro = Projetil.Lancar(p, new Vector3(0.9f, Pawn.ALTURA_MAO, 0f), Vector3.right, Elemento.Raio, slot);   // Raio: sem status no alvo seco
            _m.Registrar(tiro);
            float hp0 = b.Vital.Hp + b.Vital.Escudo;
            for (int i = 0; i < 60 && _m.Projeteis.Count > 0; i++) _m.Tick(DT);
            Assert.AreEqual(0, _m.Projeteis.Count, "o projetil morreu no impacto");
            Assert.Less(b.Vital.Hp + b.Vital.Escudo, hp0, "o alvo tomou o dano");

            Efeitos.De(b).IframesLeft = 1f;
            Projetil tiro2 = Projetil.Lancar(p, new Vector3(0.9f, Pawn.ALTURA_MAO, 0f), Vector3.right, Elemento.Raio, slot);
            _m.Registrar(tiro2);
            float hp1 = b.Vital.Hp + b.Vital.Escudo;
            for (int i = 0; i < 20; i++) _m.Tick(0.01f);
            Assert.AreEqual(hp1, b.Vital.Hp + b.Vital.Escudo, 1e-4f, "em i-frames o tiro atravessa");
        }

        [Test]
        public void ZonaLigaNoPouso_EDerrubadoInstalado()
        {
            _m.Iniciar(3, 1, false);
            Assert.IsFalse(_m.Zona.Ligada, "a zona nasce inerte");
            Bus.EmitQuedaFase(Queda.POUSOU);
            Assert.IsTrue(_m.Zona.Ligada, "o pouso liga a tempestade");
            Assert.IsNotNull(Combat.InterceptarMorte, "Derrubado instalado no ponto unico");
            Assert.AreSame(_m.Arena, Derrubado.Arena);
        }

        [Test]
        public void KillFeed_MortePorDoT_CreditaOUltimoAtacante_SemDuplicar()
        {
            _m.Iniciar(4, 1, false);
            FakeEntidade p = Registrar("player", Vector3.zero, true);
            FakeEntidade b = Registrar("Vex", new Vector3(5f, 0f, 0f));
            Combat.AplicarDano(b, 20f, Elemento.Fogo, p, false, true);
            b.Vital.Hp = 3f;
            Combat.AplicarDot(b, 100f, 0.25f, "burn", null);   // a queimadura fecha a conta, fonte null
            Assert.AreEqual(1, _abates.Count, "abate por DoT do player entra no feed");
            Assert.AreEqual("Vex", _abates[0]);

            var m2 = new Partida(new FakeRelevo());
            m2.Iniciar(6, 1, false);
            FakeEntidade p2 = Registrar("p2", Vector3.zero, true);
            FakeEntidade b2 = new FakeEntidade("Umbra", new Vector3(5f, 0f, 0f));
            m2.Registrar(b2, new ArmaSlot(b2));
            Matar(b2, p2);   // tiro direto: quem emite e' o Projetil, nao a Partida
            Assert.AreEqual(1, _abates.Count, "morte direta nao duplica o feed");
            m2.Encerrar();
        }
    }
}
