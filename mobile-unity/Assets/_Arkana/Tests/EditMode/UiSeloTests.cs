using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>O SELO DO CAMPEAO (onda 18B, GDD §18.6) pela parte pura: a cronica conta pelo Bus o que o cartao mostra (abates e
    /// dano do jogador, Sintonias da DUPLA dele, elementos sem repetir, tempo vivo), zera no MatchStarted e fecha a colocacao
    /// por time (dupla e solo); o carimbo desce, bate e assenta; o cartao cabe na area segura do Poco F4 sem encolher.</summary>
    public class UiSeloTests
    {
        const int TIME_INIMIGO = 1, TIME_OUTRO = 2;
        FakeEntidade _eu, _par, _inimigoA, _inimigoB;
        CronicaDaPartida _c;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Combat.Reset();
            _eu = new FakeEntidade("Pyra", Vector3.zero, true);
            _par = new FakeEntidade("Gromm", new Vector3(2f, 0f, 0f));
            _inimigoA = new FakeEntidade("Vex", new Vector3(20f, 0f, 0f));
            _inimigoB = new FakeEntidade("Maris", new Vector3(22f, 0f, 0f));
            Combat.DefinirTime(_eu, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(_par, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(_inimigoA, TIME_INIMIGO);
            Combat.DefinirTime(_inimigoB, TIME_INIMIGO);
            _c = new CronicaDaPartida { Jogador = _eu };
            Bus.EmitMatchStarted();
        }

        [TearDown]
        public void TearDown()
        {
            _c.Desligar();
            Bus.Reset();
            Combat.Reset();
        }

        static DisparoSintonia Combo(IEntidade a, IEntidade b) =>
            new DisparoSintonia { Combo = ComboSintonia.TornadoFlamejante, A = a, B = b, ElA = Elemento.Fogo, ElB = Elemento.Vento };

        static void Matar(IEntidade e) { e.Vital.Hp = 0f; }

        [Test]
        public void Conta_AbatesEDanoDoJogador_SoEmInimigo()
        {
            Bus.EmitPlayerKilledBot("Vex");
            Bus.EmitPlayerKilledBot("Maris");
            Assert.AreEqual(2, _c.Abates, "cada abate do jogador conta");
            Bus.EmitDamageApplied(_inimigoA, 30f, Elemento.Fogo, _eu, false);
            Bus.EmitDamageApplied(_inimigoB, 12.5f, Elemento.Raio, _eu, true);   // no escudo tambem e' dano causado
            Bus.EmitDamageApplied(_eu, 50f, Elemento.Fogo, _inimigoA, false);    // o que ele levou
            Bus.EmitDamageApplied(_inimigoA, 20f, Elemento.Terra, _par, false);   // o do parceiro e' do parceiro
            Bus.EmitDamageApplied(_inimigoA, 9f, Elemento.Fogo, null, false);     // terreno/DoT sem dono
            Bus.EmitDamageApplied(_par, 7f, Elemento.Fogo, _eu, false);           // no aliado nao e' feito de guerra
            Assert.AreEqual(42.5f, _c.Dano, 1e-4f, "so' o dano do jogador em inimigo");
        }

        [Test]
        public void Sintonias_SoDaDupla_ADasDuplasInimigasNaoEntra()
        {
            Bus.EmitSintoniaDisparou(Combo(_eu, _par));
            Bus.EmitSintoniaDisparou(Combo(_par, _eu));   // o parceiro abriu: da dupla do mesmo jeito
            Bus.EmitSintoniaDisparou(Combo(_inimigoA, _inimigoB));
            Assert.AreEqual(2, _c.Sintonias, "a Sintonia da dupla inimiga nao entra na cronica do jogador");
        }

        [Test]
        public void Elementos_SemRepetir_NaOrdemDoPrimeiroTiro()
        {
            Bus.EmitSpellCast(Elemento.Fogo);
            Bus.EmitSpellCast(Elemento.Raio);
            Bus.EmitSpellCast(Elemento.Fogo);
            Bus.EmitSpellCast(Elemento.Agua);
            Bus.EmitSpellCast(Elemento.Raio);
            CollectionAssert.AreEqual(new[] { Elemento.Fogo, Elemento.Raio, Elemento.Agua }, _c.Usados);
        }

        [Test]
        public void TempoVivo_AndaComOJogo_ParaNaMorteDeleENoVeredito()
        {
            _c.Tick(10f);
            Bus.EmitEntityDied(_inimigoA);
            _c.Tick(5f);
            Assert.AreEqual(15f, _c.TempoVivoS, 1e-4f, "a morte de outro nao para o relogio");
            Bus.EmitEntityDied(_eu);
            _c.Tick(20f);
            Assert.AreEqual(15f, _c.TempoVivoS, 1e-4f, "eliminado, o tempo vivo para");
            Bus.EmitMatchStarted();
            _c.Tick(3f);
            Bus.EmitMatchOver(true);
            _c.Tick(4f);
            Assert.AreEqual(3f, _c.TempoVivoS, 1e-4f, "o veredito para o relogio");
            Assert.AreEqual("4:05", CronicaDaPartida.TextoTempo(245.9f), "segundos para baixo");
            Assert.AreEqual("0:07", CronicaDaPartida.TextoTempo(7f));
            Assert.AreEqual("1.235", CronicaDaPartida.TextoDano(1234.6f), "ponto de milhar em qualquer cultura");
        }

        [Test]
        public void MatchStarted_ZeraTudo()
        {
            Bus.EmitPlayerKilledBot("Vex");
            Bus.EmitDamageApplied(_inimigoA, 30f, Elemento.Fogo, _eu, false);
            Bus.EmitSintoniaDisparou(Combo(_eu, _par));
            Bus.EmitSpellCast(Elemento.Terra);
            _c.Tick(30f);
            _c.Fechar(false, new List<IEntidade> { _eu, _par, _inimigoA, _inimigoB });
            Assert.IsTrue(_c.Dupla);
            Bus.EmitMatchStarted();
            Assert.AreEqual(0, _c.Abates);
            Assert.AreEqual(0f, _c.Dano);
            Assert.AreEqual(0, _c.Sintonias);
            Assert.AreEqual(0, _c.Usados.Count);
            Assert.AreEqual(0f, _c.TempoVivoS);
            Assert.AreEqual(0, _c.Times, "a colocacao da partida velha sai");
            Assert.IsNull(_c.Parceiro);
            _c.Tick(2f);
            Assert.AreEqual(2f, _c.TempoVivoS, 1e-4f, "o relogio volta a andar (o Fechar o tinha parado)");
        }

        [Test]
        public void Colocacao_PorDupla_ContaSoOsTimesDoOutroLadoDePe()
        {
            // 4 duplas: a do jogador, a A (inteira fora), a B (um de pe') e a C (inteira)
            var b1 = new FakeEntidade("B1", Vector3.zero); var b2 = new FakeEntidade("B2", Vector3.zero);
            var c1 = new FakeEntidade("C1", Vector3.zero); var c2 = new FakeEntidade("C2", Vector3.zero);
            Combat.DefinirTime(b1, TIME_OUTRO); Combat.DefinirTime(b2, TIME_OUTRO);
            Combat.DefinirTime(c1, 3); Combat.DefinirTime(c2, 3);
            var arena = new List<IEntidade> { _eu, _par, _inimigoA, _inimigoB, b1, b2, c1, c2 };
            Matar(_inimigoA); Matar(_inimigoB); Matar(b1);
            // fim por tempo: o jogador de pe' — o time dele nao conta contra ele
            _c.Fechar(false, arena);
            Assert.AreEqual(3, _c.Colocacao, "B e C de pe' = #3");
            Assert.AreEqual(4, _c.Times, "4 duplas na partida, vivas ou nao");
            Assert.AreSame(_par, _c.Parceiro, "o parceiro sai da arena (mesmo time, nao o jogador)");
            Assert.AreEqual("#3 DE 4 TRIOS", _c.TextoColocacao());
            Assert.AreEqual("<color=#FFFFFF>#3</color> DE 4 TRIOS", _c.TextoColocacao("#FFFFFF"), "so' o numero ganha a cor");
            // o time do jogador cai inteiro: a mesma conta
            Matar(_eu); Matar(_par);
            _c.Fechar(false, arena);
            Assert.AreEqual(3, _c.Colocacao);
            _c.Fechar(true, arena);
            Assert.AreEqual(1, _c.Colocacao, "vencer e' #1");
            Assert.AreEqual(CronicaDaPartida.Titulo(true, true), Textos.SeloCampeoes, "a dupla vence junta: o plural");
            Assert.AreEqual(CronicaDaPartida.Chamada(true, true, false), Textos.SeloUltimaDupla);
            Assert.AreEqual(CronicaDaPartida.Titulo(false, true), Textos.HudDerrota);
        }

        [Test]
        public void Colocacao_Solo_CadaMagoEUmTime_SemParceiro()
        {
            Combat.Reset();   // o FFA de sempre: ninguem registrado, cada bot sozinho (o jogador e' o time 0)
            var bots = new List<IEntidade>();
            var arena = new List<IEntidade> { _eu };
            for (int i = 0; i < 4; i++) { var b = new FakeEntidade("Bot" + i, Vector3.zero); bots.Add(b); arena.Add(b); }
            Matar(bots[0]); Matar(bots[1]); Matar(_eu);
            _c.Fechar(false, arena);
            Assert.IsNull(_c.Parceiro, "solo nao tem parceiro");
            Assert.IsFalse(_c.Dupla);
            Assert.AreEqual(3, _c.Colocacao, "2 bots de pe' = #3");
            Assert.AreEqual(5, _c.Times, "5 magos");
            Assert.AreEqual("#3 DE 5 MAGOS", _c.TextoColocacao());
            Assert.AreEqual(CronicaDaPartida.Titulo(true, false), Textos.SeloCampeao);
            _c.Fechar(false, null);
            Assert.AreEqual("", _c.TextoColocacao(), "sem arena, sem ranking (o cartao esconde a linha)");
        }

        [Test]
        public void Carimbo_DesceBateSoltaAOndaEAssenta()
        {
            float escala, alfa, giro, onda, tremor;
            SeloDoCampeao.Carimbo(0f, out escala, out alfa, out giro, out onda, out tremor);
            Assert.AreEqual(0f, alfa, "antes de o cartao pousar, o selo nao aparece");
            Assert.AreEqual(SeloDoCampeao.CarimboPico, escala, 1e-4f);
            float desce = SeloDoCampeao.CarimboAtrasoS + SeloDoCampeao.DesceS * 0.5f;
            SeloDoCampeao.Carimbo(desce, out escala, out alfa, out giro, out onda, out tremor);
            Assert.Greater(escala, 1.5f, "na metade da descida ainda esta' grande (acelera: cai, nao flutua)");
            Assert.Less(escala, SeloDoCampeao.CarimboPico);
            Assert.Less(onda, 0f, "sem onda antes da batida");
            float batida = SeloDoCampeao.CarimboAtrasoS + SeloDoCampeao.DesceS;
            SeloDoCampeao.Carimbo(batida + SeloDoCampeao.BateS * 0.5f, out escala, out alfa, out giro, out onda, out tremor);
            Assert.Less(escala, 0.95f, "na batida o selo AFUNDA");
            Assert.AreEqual(1f, alfa);
            Assert.Greater(onda, 0f, "a onda de brilho sai da batida");
            SeloDoCampeao.Carimbo(batida + SeloDoCampeao.OndaS * 0.5f, out escala, out alfa, out giro, out onda, out tremor);
            Assert.AreEqual(0.5f, onda, 1e-4f);
            SeloDoCampeao.Carimbo(5f, out escala, out alfa, out giro, out onda, out tremor);
            Assert.AreEqual(1f, escala, 1e-4f, "assentado");
            Assert.AreEqual(SeloDoCampeao.GiroFim, giro, 1e-4f);
            Assert.Less(onda, 0f, "a onda acabou");
            Assert.AreEqual(0f, tremor, 1e-4f, "o cartao parou de tremer");
        }

        /// <summary>Foto 56 da integracao: a HUD de combate vazava pelo cartao. O cartao mora num canvas ACIMA de todos e o veu
        /// escurece de verdade na mistura LINEAR do uGUI (o alfa 0,76 deixava ~52% do brilho aos olhos).</summary>
        [Test]
        public void Cartao_AcimaDeTodoCanvas_EVeuQueApagaNaMisturaLinear()
        {
            Assert.Greater(SeloDoCampeao.Ordem, Hud.OrdemDoCanvas, "acima da HUD (marcas de alvo e HUD da dupla moram nela)");
            Assert.Greater(SeloDoCampeao.Ordem, TelaDeCarregamento.Ordem, "acima de todo canvas do jogo");
            // um branco da HUD sob o veu, como o olho ve' (sRGB ~ gama 2,2 sobre o que sobra em linear)
            float aosOlhos = Mathf.Pow(1f - SeloDoCampeao.VeuAlfa, 1f / 2.2f);
            Assert.LessOrEqual(aosOlhos, 0.4f, "o veu apaga o resto: no maximo ~40% do brilho");
        }

        [Test]
        public void Cartao_CabeNoPocoF4DeitadoSemEncolher_EEncolheNaTelaEstreita()
        {
            float poco = Dp.PxCom(1f, 395f);
            Assert.AreEqual(1f, SeloDoCampeao.Escala(new Vector2(2400f, 1080f), new Margens(0f, 0f, 0f, 0f), poco), 1e-4f);
            Assert.AreEqual(1f, SeloDoCampeao.Escala(new Vector2(2400f, 1080f), new Margens(100f, 0f, 0f, 0f), poco), 1e-4f, "o furo da camera na lateral nao encolhe");
            // 16:9 de 440 dpi: 1920 px = ~698 dp < 800 dp do cartao — encolhe e cabe inteiro com a folga
            float px = Dp.PxCom(1f, 440f);
            float s = SeloDoCampeao.Escala(new Vector2(1920f, 1080f), new Margens(60f, 0f, 0f, 0f), px);
            Assert.Less(s, 1f);
            Assert.LessOrEqual((SeloDoCampeao.CartaoL + 2f * SeloDoCampeao.FolgaDp) * px * s, 1920f - 60f + 0.01f, "cabe na largura segura");
            Assert.LessOrEqual((SeloDoCampeao.AlturaDp + 2f * SeloDoCampeao.FolgaDp) * px * s, 1080f + 0.01f, "e na altura");
            Assert.Greater(SeloDoCampeao.AlturaDp * poco, 0f);
            Assert.LessOrEqual((SeloDoCampeao.AlturaDp + 2f * SeloDoCampeao.FolgaDp) * poco, 1080f, "no Poco F4 cartao + botoes cabem nos ~437 dp");
        }
    }
}
