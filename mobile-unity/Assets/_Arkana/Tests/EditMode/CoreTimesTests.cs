using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// TIMES (onda 17, fase G4): "e' aliado?" e' UMA pergunta — Combat.MesmoTime — e EhPlayer so' diz "e' o humano".
    /// Aqui: o registro (e o que acontece sem ele), o anti-farm, o derrubado de bot com parceiro, o reerguer so' por
    /// aliado, o veredito por time (PONTE A6: a dupla vence junta, mesmo com o player fora; solo = ultimo em pe' de sempre)
    /// e um kit que olha o time do DONO (a bigorna do Brok), nao a flag do humano.
    /// </summary>
    public class CoreTimesTests
    {
        private Partida _m;
        private List<bool> _fins;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset(); Derrubado.Reset();
            Arkana.Menu.Menu.PedidoDeTreino = false;
            _fins = new List<bool>();
            Bus.MatchOver += v => _fins.Add(v);
            _m = null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_m != null) _m.Encerrar();
            Derrubado.Reset(); Efeitos.Reset(); Combat.Reset(); Bus.Reset();
        }

        private static FakeEntidade Bot(string nome, float x) => new FakeEntidade(nome, new Vector3(x, 0f, 0f));

        /// <summary>Tiro que mata: com parceiro de pe' o primeiro DERRUBA; o segundo finaliza. Para quando saiu.</summary>
        private static void Eliminar(IEntidade e)
        {
            for (int i = 0; i < 3 && e.Vital.Viva; i++) Combat.AplicarDano(e, 1000f, Elemento.Fogo, null, false, true);
        }

        private static void Golpe(IEntidade e) => Combat.AplicarDano(e, 1000f, Elemento.Fogo, null, false, true);

        /// <summary>Partida com o player e mais corpos registrados. O time vem DEPOIS do Iniciar (que chama Combat.Reset).</summary>
        private FakeEntidade NovaPartida(int bots, params IEntidade[] corpos)
        {
            if (_m != null) _m.Encerrar();
            _m = new Partida(new FakeRelevo());
            _m.Iniciar(7, bots, false);
            var p = new FakeEntidade("player", Vector3.zero, true);
            _m.Registrar(p, null);
            foreach (IEntidade c in corpos) _m.Registrar(c, null);
            return p;
        }

        // ---------------------------------------------------------------- A. o registro

        [Test]
        public void MesmoTime_Registrado_SemRegistro_PlayerSemRegistro_Null()
        {
            FakeEntidade a = Bot("a", 1f), b = Bot("b", 2f);
            var p = new FakeEntidade("p", Vector3.zero, true);
            var p2 = new FakeEntidade("p2", Vector3.zero, true);

            Assert.IsFalse(Combat.MesmoTime(a, b), "sem registro, cada bot e' um time so' dele (o FFA de hoje)");
            Assert.IsTrue(Combat.MesmoTime(a, a), "o proprio e' do proprio time");
            Assert.AreNotEqual(Combat.TimeDe(a), Combat.TimeDe(b));
            Assert.AreEqual(Combat.TimeDe(a), Combat.TimeDe(a), "o time avulso e' estavel");
            Assert.AreNotEqual(Combat.TIME_DO_PLAYER, Combat.TimeDe(a), "avulso nunca cai no time do player");
            Assert.AreEqual(Combat.TIME_DO_PLAYER, Combat.TimeDe(p), "player sem registro = TIME_DO_PLAYER");
            Assert.IsTrue(Combat.MesmoTime(p, p2), "compat: os fakes EhPlayer de hoje continuam um esquadrao");
            Assert.IsFalse(Combat.MesmoTime(p, a));

            Combat.DefinirTime(a, 3); Combat.DefinirTime(b, 3);
            Assert.IsTrue(Combat.MesmoTime(a, b), "registrados no mesmo time");
            Combat.DefinirTime(b, 4);
            Assert.IsFalse(Combat.MesmoTime(a, b), "re-registrar troca o time");
            Combat.DefinirTime(a, Combat.TIME_DO_PLAYER);
            Assert.IsTrue(Combat.MesmoTime(p, a), "o PARCEIRO bot registrado no time do player e' aliado dele");
            Combat.DefinirTime(p2, 5);
            Assert.IsFalse(Combat.MesmoTime(p, p2), "registrado manda mais que a flag EhPlayer");

            Assert.IsFalse(Combat.MesmoTime(null, a));
            Assert.IsFalse(Combat.MesmoTime(a, null));
            Assert.IsFalse(Combat.MesmoTime(null, null), "null nao e' aliado de ninguem");
        }

        [Test]
        public void Reset_LimpaORegistro()
        {
            var p = new FakeEntidade("p", Vector3.zero, true);
            FakeEntidade par = Bot("par", 1f);
            Combat.DefinirTime(par, Combat.TIME_DO_PLAYER);
            Assert.IsTrue(Combat.MesmoTime(p, par));
            Combat.Reset();
            Assert.IsFalse(Combat.MesmoTime(p, par), "partida nova nao herda o time da anterior");
        }

        [Test]
        public void Creditar_DanoEmAliadoRegistradoNaoConta_EmInimigoConta()
        {
            var p = new FakeEntidade("p", Vector3.zero, true);
            FakeEntidade par = Bot("par", 1f), e1 = Bot("e1", 5f), e2 = Bot("e2", 6f);
            Combat.DefinirTime(par, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(e1, 1); Combat.DefinirTime(e2, 1);

            Combat.AplicarDano(par, 10f, Elemento.Fogo, p);
            Assert.AreEqual(0f, p.Vital.DanoCausado, 1e-4f, "anti-farm: bater no parceiro nao evolui o escudo");
            Combat.AplicarDano(e2, 10f, Elemento.Fogo, e1);
            Assert.AreEqual(0f, e1.Vital.DanoCausado, 1e-4f, "nem na dupla inimiga");
            Combat.AplicarDano(e1, 10f, Elemento.Fogo, p);
            Assert.Greater(p.Vital.DanoCausado, 0f, "inimigo conta");
            Combat.AplicarDano(p, 10f, Elemento.Fogo, e1);
            Assert.Greater(e1.Vital.DanoCausado, 0f, "e o inimigo credita no player");
        }

        // ---------------------------------------------------------------- A. derrubado

        [Test]
        public void Derrubado_BotComParceiroDePeCai_SozinhoMorre()
        {
            FakeEntidade a = Bot("a", 0f), b = Bot("b", 40f), c = Bot("c", 3f);
            Combat.DefinirTime(a, 1); Combat.DefinirTime(b, 1); Combat.DefinirTime(c, 2);
            Derrubado.Arena = new List<IEntidade> { a, b, c };
            Derrubado.Instalar();
            var mortos = new List<IEntidade>();
            Bus.EntityDied += e => mortos.Add(e);

            Golpe(a);
            Assert.IsTrue(Derrubado.Esta(a), "bot com parceiro de pe' CAI");
            Assert.IsTrue(a.Vital.Viva);
            Assert.AreEqual(0, mortos.Count, "sem EntityDied");

            Golpe(c);
            Assert.IsFalse(Derrubado.Esta(c), "sozinho no time: o inimigo perto nao e' esquadrao dele");
            CollectionAssert.Contains(mortos, c, "morre como sempre morreu");

            Golpe(b);
            Assert.IsFalse(Derrubado.Esta(b), "o parceiro CAIDO nao segura ninguem: b morre");
            CollectionAssert.Contains(mortos, b);

            // defeito: o caido sozinho seguia esvaecendo 30 s esperando um resgate que nao existe mais — o time inteiro no
            // chao e' eliminado no proximo tique (regra dos BRs de esquadrao)
            Derrubado.De(a).Tique();
            Assert.IsFalse(a.Vital.Viva, "sem ninguem de pe' no time, o caido sai no proximo tique");
            CollectionAssert.Contains(mortos, a);
        }

        [Test]
        public void Reerguer_SoPorAliado()
        {
            FakeEntidade a = Bot("a", 0f), b = Bot("b", 40f), inimigo = Bot("inimigo", 1f);
            Combat.DefinirTime(a, 1); Combat.DefinirTime(b, 1); Combat.DefinirTime(inimigo, 2);
            Derrubado.Arena = new List<IEntidade> { a, b, inimigo };
            Derrubado.Instalar();
            Golpe(a);
            Derrubado d = Derrubado.De(a);
            Assert.IsNotNull(d);
            int n = Mathf.RoundToInt(Derrubado.REERGUER_S / Derrubado.TIQUE) + 2;
            for (int i = 0; i < n; i++) { Combat.TickDot(Derrubado.TIQUE); d.Tique(); }
            Assert.IsTrue(Derrubado.Esta(a), "o inimigo em cima NAO reergue");
            Assert.IsNull(d.Reanimador);

            b.Pos = new Vector3(1.5f, 0f, 0f);
            for (int i = 0; i < n && Derrubado.Esta(a); i++) { Combat.TickDot(Derrubado.TIQUE); d.Tique(); }
            Assert.IsFalse(Derrubado.Esta(a), "o parceiro chegou: reergueu");
            Assert.AreEqual(a.Vital.HpMax * Derrubado.VIDA_REERGUIDO, a.Vital.Hp, 0.01f);
        }

        // ---------------------------------------------------------------- B. veredito (PONTE A6)

        [Test]
        public void Dupla_VenceComOPlayerFora()
        {
            FakeEntidade par = Bot("par", 40f), e1 = Bot("e1", 80f), e2 = Bot("e2", -80f);
            FakeEntidade p = NovaPartida(3, par, e1, e2);
            Combat.DefinirTime(par, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(e1, 1); Combat.DefinirTime(e2, 1);

            Golpe(p);
            Assert.IsTrue(Derrubado.Esta(p), "o player com parceiro de pe' e' DERRUBADO");
            Assert.IsFalse(_m.PlayerFora, "derrubado ainda esta' em jogo");
            Eliminar(p);
            Assert.AreEqual(0, _fins.Count, "o player saiu mas o parceiro segue: a partida NAO acaba");
            Assert.IsTrue(_m.PlayerFora, "espectador");
            Assert.AreSame(par, _m.ParceiroVivo());

            Eliminar(e1);
            Assert.AreEqual(0, _fins.Count, "resta a outra dupla (e2)");
            Eliminar(e2);
            CollectionAssert.AreEqual(new[] { true }, _fins, "a dupla do player VENCE, com o player fora");
            Assert.AreEqual(1, _m.TimesVivos);
        }

        [Test]
        public void Dupla_DerrotaSoQuandoOTimeInteiroSai()
        {
            FakeEntidade par = Bot("par", 40f), e1 = Bot("e1", 80f), e2 = Bot("e2", -80f);
            FakeEntidade p = NovaPartida(3, par, e1, e2);
            Combat.DefinirTime(par, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(e1, 1); Combat.DefinirTime(e2, 1);

            Eliminar(p);
            Assert.AreEqual(0, _fins.Count, "a morte do player sozinha nao encerra mais");
            Eliminar(par);
            CollectionAssert.AreEqual(new[] { false }, _fins, "o time do player acabou: derrota");
            Assert.IsFalse(_m.PlayerFora, "sem time vivo nao ha' espectador");
        }

        [Test]
        public void Dupla_ParceiroCaiPrimeiro_OPlayerSegueEVenceSozinho()
        {
            FakeEntidade par = Bot("par", 40f), e1 = Bot("e1", 80f);
            FakeEntidade p = NovaPartida(2, par, e1);
            Combat.DefinirTime(par, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(e1, 1);

            Eliminar(par);
            Assert.AreEqual(0, _fins.Count, "o parceiro saiu, o player segue");
            Assert.IsNull(_m.ParceiroVivo());
            Assert.AreEqual(1, _m.BotsVivos, "o parceiro e' bot: a contagem desce");
            Eliminar(e1);
            CollectionAssert.AreEqual(new[] { true }, _fins);
            Assert.IsTrue(p.Vital.Viva);
        }

        [Test]
        public void Solo_IgualAHoje_UltimoEmPePelaContagem()
        {
            FakeEntidade b1 = Bot("b1", 40f), b2 = Bot("b2", -40f);
            FakeEntidade p = NovaPartida(2, b1);   // o b2 ainda nao foi registrado (a cena registra um por passo)
            Eliminar(b1);
            Assert.AreEqual(0, _fins.Count, "solo conta pelo `bots` do Iniciar: ainda falta um");
            Assert.IsFalse(Derrubado.Esta(b1), "bot solo nao cai: morre");
            _m.Registrar(b2, null);
            Eliminar(b2);
            CollectionAssert.AreEqual(new[] { true }, _fins, "ultimo em pe' = vitoria");

            _fins.Clear();
            FakeEntidade b3 = Bot("b3", 40f);
            FakeEntidade p2 = NovaPartida(1, b3);
            Golpe(p2);
            Assert.IsFalse(Derrubado.Esta(p2), "player solo nao cai");
            CollectionAssert.AreEqual(new[] { false }, _fins, "solo: a morte do player e' o fim, na hora");
            Assert.IsFalse(_m.PlayerFora);
            Assert.IsNull(_m.ParceiroVivo(), "solo nao tem parceiro");
        }

        [Test]
        public void TimesVivos_DerrubadoConta_EParceiroVivo()
        {
            FakeEntidade par = Bot("par", 40f), e1 = Bot("e1", 80f), e2 = Bot("e2", 90f), s = Bot("solo", -80f);
            FakeEntidade p = NovaPartida(4, par, e1, e2, s);
            Combat.DefinirTime(par, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(e1, 1); Combat.DefinirTime(e2, 1);
            Assert.AreEqual(3, _m.TimesVivos, "a dupla do player, a dupla 1 e o avulso");
            Assert.AreSame(par, _m.ParceiroVivo());

            Golpe(e1);
            Assert.IsTrue(Derrubado.Esta(e1));
            Assert.AreEqual(3, _m.TimesVivos, "derrubado ainda e' time vivo");
            Golpe(par);
            Assert.IsTrue(Derrubado.Esta(par));
            Assert.AreSame(par, _m.ParceiroVivo(), "o parceiro derrubado ainda e' o parceiro vivo");

            Eliminar(s);
            Assert.AreEqual(2, _m.TimesVivos);
            Eliminar(e1); Eliminar(e2);
            Assert.AreEqual(1, _m.TimesVivos);
            CollectionAssert.AreEqual(new[] { true }, _fins);
        }

        // ---------------------------------------------------------------- A. kit pelo time do DONO

        [Test]
        public void Brok_ForjaViva_EscudoNoAliadoDoDono_NuncaNoInimigoRegistrado()
        {
            var dono = new FakeConjurador("brok", Vector3.zero);
            var k = new KitRunner("13-brok", dono);
            var brok = (Brok)k.Impl;
            FakeEntidade parceiro = Bot("parceiro", 2f);                                  // bot: EhPlayer = false
            var inimigo = new FakeConjurador("inimigo", new Vector3(-3f, 0f, 0f), true);   // flag de humano, time 1
            Combat.DefinirTime(parceiro, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(inimigo, 1);
            dono.Arena.Add(dono); dono.Arena.Add(parceiro); dono.Arena.Add(inimigo);

            k.Tick(0.01f, 1000f);   // carga cheia pelo canal do dano
            Assert.IsTrue(k.UsarSuprema());
            parceiro.Vital.Escudo = 0f; inimigo.Vital.Escudo = 0f;
            float t = k.Dados.Telegrafia + 1f;
            for (float s = 0f; s < t; s += 0.1f) k.Tick(0.1f);
            Assert.IsNotNull(brok.BigornaAtiva);
            Assert.Greater(parceiro.Vital.Escudo, 3f, "o aliado do DONO (bot registrado no time dele) ganha escudo");
            Assert.AreEqual(0f, inimigo.Vital.Escudo, 1e-4f, "o inimigo registrado em outro time nao — mesmo com a flag EhPlayer");

            // o apoio do Grupo D (Sylva, Basalto, Noctus...) responde pelo mesmo Combat
            Assert.IsTrue(ApoioGrupoD.Aliado(dono, parceiro));
            Assert.IsTrue(ApoioGrupoD.Inimigo(dono, inimigo));
        }

        [Test]
        public void Projetil_AtravessaOAliado_AcertaOInimigo()
        {
            // defeito: `alvo != Atirador` como unico filtro — no modo dupla o tiro do player batia no parceiro (PONTE A4)
            var p = new FakeEntidade("player", Vector3.zero, true);
            var parceiro = Bot("parceiro", 2f);
            var inimigo = Bot("inimigo", 4f);
            Combat.DefinirTime(parceiro, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(inimigo, 1);
            Projetil tiro = Projetil.Lancar(p, new Vector3(0f, 1.1f, 0f), Vector3.right, Elemento.Fogo);
            float hp = parceiro.Vital.Hp + parceiro.Vital.Escudo, hpInimigo = inimigo.Vital.Hp + inimigo.Vital.Escudo;
            Assert.IsTrue(tiro.Tick(0.05f, _ => parceiro), "o tiro segue voando atraves do aliado");
            Assert.AreEqual(hp, parceiro.Vital.Hp + parceiro.Vital.Escudo, 1e-4f, "o parceiro nao toma o tiro");
            Assert.IsFalse(tiro.Tick(0.05f, _ => inimigo), "o inimigo para o tiro");
            Assert.Less(inimigo.Vital.Hp + inimigo.Vital.Escudo, hpInimigo, "o inimigo toma o tiro");
        }

    }
}
