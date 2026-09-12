using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// DERRUBADO E ELIMINADO na tela, pela partida de verdade (a Main cria o VisualDoAbate): um bot posto no chao, caido ->
    /// o anel acende; finalizado -> a alma estoura e o corpo AFUNDA e some — so' o visual, o Pawn fica na arena —; voltou a
    /// viver (o boneco do treino), o visual volta. Vermelho se o corpo nao sumir, se a entidade sumir junto, ou se o boneco
    /// ressuscitado ficar invisivel.
    /// </summary>
    public class AbateVisualTests
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
        public IEnumerator Caido_AcendeAnel_MorteEstouraEAfunda_SoOVisualSome()
        {
            Main main = _go.AddComponent<Main>();
            yield return null;
            Bus.EmitGameStartRequested();
            yield return null;
            var v = Object.FindFirstObjectByType<VisualDoAbate>();
            Assert.IsNotNull(v, "a partida tem quem desenhe o derrubado e o eliminado");

            // o bot sai do portao AINDA no castelo (a Queda so' aceita posicao ali) e pousa no ato num nascimento da ilha
            Bot bot = main.Bots[0];
            bot.enabled = false;   // parado: o teste e' do visual, nao da IA
            Vector3 n0 = Arkana.World.Ilha.Atual.Relevo.Nascimentos[0];
            main.Castelo.Saltar(bot.Pawn.gameObject);
            bot.Pawn.Aterrar(new Vector3(n0.x, -999f, n0.z));
            yield return null;
            Assert.AreEqual(0, v.CaidosNaTela, "de pe': nada no chao");

            new Derrubado(bot.Pawn).Cair(main.Player.Pawn);
            yield return null;
            Assert.AreEqual(1, v.CaidosNaTela, "caido: o anel acende");

            Combat.AplicarDano(bot.Pawn, 999f, Elemento.Fogo, main.Player.Pawn);   // ja' caido: a costura deixa a finalizacao passar
            Assert.IsFalse(bot.Pawn.Viva, "finalizado");
            yield return null;
            Assert.AreEqual(0, v.CaidosNaTela, "morto nao esta' caido");
            int n = 0;
            foreach (var ps in v.GetComponentsInChildren<ParticleSystem>()) n += ps.particleCount;
            Assert.Greater(n, 0, "a alma estoura");

            GameObject corpo = bot.Pawn.Visual.gameObject;
            float t = 0f;
            while (corpo.activeSelf && t < VisualDoAbate.CorpoFicaS + VisualDoAbate.CorpoAfundaS + 1f) { yield return null; t += Time.deltaTime; }
            Assert.IsFalse(corpo.activeSelf, "o corpo afundou e sumiu");
            Assert.IsTrue(bot.Pawn.gameObject.activeSelf, "so' o VISUAL some: o Pawn (entidade) fica");
            Assert.Contains(bot.Pawn, main.Partida.Arena, "e segue registrado na arena");

            bot.Pawn.Vital.Reset();   // o que a Partida faz com o boneco do treino
            yield return null;
            Assert.IsTrue(corpo.activeSelf, "voltou a viver: o visual volta");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
