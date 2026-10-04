using NUnit.Framework;
using Arkana.Core;

namespace Arkana.Tests
{
    /// <summary>O ciclo Menu -> Partida -> Fim -> Menu, puro: transicoes pelo Bus, reinicio, abandono, MatchOver uma vez.</summary>
    public class FluxoDeJogoTests
    {
        FluxoDeJogo _f;
        int _montou, _desmontou;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            _montou = 0; _desmontou = 0;
            _f = new FluxoDeJogo(() => _montou++, () => _desmontou++);
            _f.Ligar();
        }

        [TearDown]
        public void TearDown() { _f.Desligar(); }

        [Test]
        public void NasceNoMenu_EOPedidoDoBusMontaUmaVez()
        {
            Assert.AreEqual(FluxoDeJogo.Estado.Menu, _f.Atual);
            Assert.AreEqual(0, _montou);
            Bus.EmitGameStartRequested();
            Assert.AreEqual(FluxoDeJogo.Estado.Partida, _f.Atual);
            Assert.AreEqual(1, _montou, "o pedido monta a arena");
            Bus.EmitGameStartRequested();
            Assert.AreEqual(1, _montou, "pedido durante a partida e' ignorado (toque duplo no JOGAR)");
            Assert.IsFalse(_f.PedirPartida());
        }

        [Test]
        public void MatchOver_LevaAoFim_UmaVezSo()
        {
            Bus.EmitMatchOver(true);
            Assert.AreEqual(FluxoDeJogo.Estado.Menu, _f.Atual, "MatchOver fora de partida nao conta");
            Assert.AreEqual(0, _f.Fins);
            Bus.EmitGameStartRequested();
            Bus.EmitMatchOver(true);
            Assert.AreEqual(FluxoDeJogo.Estado.Fim, _f.Atual);
            Assert.IsTrue(_f.Vitoria);
            Bus.EmitMatchOver(false);
            Assert.AreEqual(1, _f.Fins, "o segundo MatchOver nao conta");
            Assert.IsTrue(_f.Vitoria, "e nao reescreve o veredito");
            Assert.AreEqual(0, _desmontou, "no Fim a arena fica de pe' (a HUD mostra o veredito)");
        }

        [Test]
        public void Reiniciar_DesmontaEMontaDeNovo()
        {
            Assert.IsFalse(_f.Reiniciar(), "no menu nao ha' o que reiniciar");
            Bus.EmitGameStartRequested();
            Bus.EmitMatchOver(false);
            Assert.IsTrue(_f.Reiniciar());
            Assert.AreEqual(FluxoDeJogo.Estado.Partida, _f.Atual);
            Assert.AreEqual(1, _desmontou, "a arena velha sai");
            Assert.AreEqual(2, _montou, "e entra uma nova");
            Bus.EmitMatchOver(true);
            Assert.AreEqual(2, _f.Fins, "a partida nova pode acabar de novo");
        }

        [Test]
        public void Abandonar_NoMeioDaPartida_DesmontaEVoltaAoMenu()
        {
            Assert.IsFalse(_f.VoltarAoMenu(), "ja' esta' no menu");
            Bus.EmitGameStartRequested();
            _f.Tick(2f);
            Assert.AreEqual(2f, _f.TempoNoEstado, 1e-4f);
            Assert.IsTrue(_f.VoltarAoMenu());
            Assert.AreEqual(FluxoDeJogo.Estado.Menu, _f.Atual);
            Assert.AreEqual(1, _desmontou);
            Assert.AreEqual(0f, _f.TempoNoEstado, 1e-4f, "o relogio do estado zera");
            Bus.EmitGameStartRequested();
            Assert.AreEqual(2, _montou, "do menu da' para jogar de novo");
        }

        [Test]
        public void MenuDepoisDoFim_DesmontaUmaVez_EDesligarCalaOBus()
        {
            Bus.EmitGameStartRequested();
            Bus.EmitMatchOver(true);
            Assert.IsTrue(_f.VoltarAoMenu());
            Assert.AreEqual(1, _desmontou);
            _f.Desligar();
            Bus.EmitGameStartRequested();
            Assert.AreEqual(1, _montou, "desligado, o Bus nao move o fluxo");
            _f.Ligar(); _f.Ligar();
            Bus.EmitGameStartRequested();
            Assert.AreEqual(2, _montou, "religar duas vezes assina uma vez so'");
        }

        [Test]
        public void MedidorDeFps_LinhaDeCusto_LegivelNoLogcat_ContadorAusenteViraND()
        {
            // BLOCO H (04/10): a linha que o aparelho escreve por janela - onde o quadro gasta, nao so' quantos quadros
            string l = MedidorDeFps.Custo(12.34, 8.5, 21.0, 2, 512L * 1024 * 1024, 410, 95, 1250000);
            Assert.AreEqual("ARKANA CUSTO cpu_main=12.3ms cpu_render=8.5ms gpu=21.0ms gc=2 mem=512MB batches=410 setpass=95 tris=1250K", l);
            StringAssert.Contains("gpu=n/d", MedidorDeFps.Custo(5, 5, -1, 0, -1, -1, -1, -1), "driver sem tempo de GPU: n/d, nunca um numero falso");
            StringAssert.Contains("mem=n/d", MedidorDeFps.Custo(5, 5, 5, 0, -1, 1, 1, 1));
        }
    }
}
