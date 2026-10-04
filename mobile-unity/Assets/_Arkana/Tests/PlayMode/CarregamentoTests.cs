using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Arkana.Core;

namespace Arkana.Tests
{
    /// <summary>
    /// A MONTAGEM EM FATIAS com a partida de verdade (passo E da ORDEM): o JOGAR cobre a tela no MESMO quadro e para o relogio;
    /// a arena sobe em varios quadros com a barra so' andando para a frente; no fim o relogio volta, a HUD e a camera do
    /// jogador acendem e a tela esvaece e some. E' tambem a MEDICAO: cada passo, a pre-carga, os quadros e o pior deles vao
    /// para o resultado do teste e para Logs/carregamento-*.txt (o editor sem GPU nao e' o aparelho: o numero que decide
    /// e' o do Poco F4; este mostra ONDE pesa). Vermelho se a montagem voltar a ser um quadro so', se a partida andar com a
    /// arena pela metade, ou se a tela nao sair.
    /// </summary>
    public class CarregamentoTests
    {
        GameObject _go;

        [SetUp]
        public void SetUp()
        {
            Arkana.Menu.Menu.PedidoDeTreino = false;
            _go = new GameObject("Main");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Arkana.Menu.Menu.PedidoDeTreino = false;
            if (_go != null) Object.Destroy(_go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Jogar_MontaEmFatiasAtrasDaTela_EATelaSomeQuandoAPartidaComeca([Values(false, true)] bool treino)
        {
            LogsDoBoot.Montagem();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = treino;
            Bus.EmitGameStartRequested();
            Assert.IsTrue(main.Carregando, "o toque abre o carregamento no mesmo quadro");
            Assert.IsFalse(main.Tela == null, "com a tela na frente");
            Assert.AreEqual(0f, Time.timeScale, "o relogio do jogo espera a montagem");

            int quadros = 0;
            float antes = 0f;
            while (main.Carregando && quadros < 900)
            {
                float p = main.Tela.Logica.Progresso;
                Assert.GreaterOrEqual(p, antes, "a barra so' anda para a frente");
                antes = p;
                if (main.Partida != null) Assert.IsTrue(main.Partida.Rodando && !main.Partida.Acabou, "arena pela metade nao da' veredito");
                yield return null;
                quadros++;
            }
            Assert.IsFalse(main.Carregando, "a montagem acabou");
            Assert.Greater(main.QuadrosDaMontagem, 2, "em fatias: a montagem nao cabe mais num quadro so'");
            Assert.AreEqual(1f, Time.timeScale, "o relogio voltou");
            Assert.IsNotNull(main.Partida);
            Assert.AreEqual(treino, main.Partida.Treino);
            Assert.IsTrue(main.Partida.Rodando);
            Assert.AreEqual(treino ? 0 : Montagem.Bots(main.Dupla).Length, main.Bots.Count, "todos os corpos criados");
            Assert.IsTrue(main.Hud.gameObject.activeSelf, "a HUD acende no fim");
            Assert.AreSame(main.Player.Camera.Cam, Camera.main, "e quem filma e' a camera do jogador");
            Assert.AreEqual(1f, main.Tela.Logica.Progresso, "100% na tela");

            float t = 0f;
            while (!(main.Tela == null) && t < 3f) { yield return null; t += Time.unscaledDeltaTime; }
            Assert.IsTrue(main.Tela == null, "a tela esvaece e some quando a partida comeca");
            Assert.AreEqual(FluxoDeJogo.Estado.Partida, main.Fluxo.Atual);

            var sb = new StringBuilder();
            sb.AppendLine((treino ? "TREINO" : "PARTIDA") + ": " + main.QuadrosDaMontagem + " quadros, pior "
                + main.PiorQuadroMs.ToString("F1") + " ms, do toque ao fim " + main.DuracaoDaMontagemMs.ToString("F0") + " ms (orcamento "
                + Main.OrcamentoMs + " ms/quadro)");
            float soma = 0f;
            foreach (var kv in main.TemposDaMontagem)
            {
                sb.AppendLine("  " + kv.Key + ": " + kv.Value.ToString("F1") + " ms");
                if (kv.Key != "pre-carga") soma += kv.Value;
            }
            sb.AppendLine("  soma dos passos (fora a pre-carga): " + soma.ToString("F1") + " ms");
            TestContext.WriteLine(sb.ToString());
            string pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            Directory.CreateDirectory(pasta);
            File.WriteAllText(Path.Combine(pasta, "carregamento-" + (treino ? "treino" : "partida") + ".txt"), sb.ToString());
            LogAssert.NoUnexpectedReceived();
        }
    }
}
