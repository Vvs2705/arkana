using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// O VOO na tela, pela partida de verdade (a Main cria o VisualDoVoo): o castelo ganha runas e rochas (filhas dele) e
    /// solta o rastro; o jogador ainda no castelo nao risca nada; saltou -> rastro nas maos e pes e vento na camera; pousou
    /// -> estalo nos pes DELE, rastro apagando, vento parado; castelo foi embora -> o rastro dele para. Vermelho se a borda
    /// do pouso nao disparar, se o rastro seguir aceso no chao, ou se o castelo sumir e o rastro dele nao.
    /// </summary>
    public class VooVisualTests
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
            Time.timeScale = 1f;
            if (_go != null) Object.Destroy(_go);
            yield return null;
        }

        static IEnumerator Esperar(float segundos)
        {
            float t = 0f;
            int frames = 0, teto = Mathf.CeilToInt(segundos * 400f) + 100;
            while (t < segundos && frames < teto) { yield return null; t += Time.deltaTime; frames++; }
        }

        /// <summary>A partida monta em FATIAS atras da tela de carregamento (onda 12B): espera a arena inteira (teto de quadros).</summary>
        static IEnumerator AteMontar(Main main)
        {
            for (int n = 0; main.Carregando && n < 900; n++) yield return null;
        }

        [UnityTest]
        public IEnumerator Voo_CasteloVivo_RastroEVentoNaQueda_EstaloNoPouso_SomeComOCastelo()
        {
            Main main = _go.AddComponent<Main>();
            yield return null;
            Bus.EmitGameStartRequested();
            yield return AteMontar(main);
            yield return null;
            yield return null;   // o Start do castelo monta o modelo; no LateUpdate seguinte o voo pendura runas e rochas
            var v = Object.FindFirstObjectByType<VisualDoVoo>();
            Assert.IsNotNull(v, "a partida tem quem desenhe o voo");
            Assert.IsTrue(v.CasteloVivo, "o castelo ganhou runas, rochas e rastro");
            Assert.IsNotNull(main.Castelo.transform.Find("VooRunas"), "as runas sao FILHAS do castelo (somem com ele)");
            Assert.Greater(v.Rochas, 0, "rochas em orbita");
            Assert.IsTrue(v.RastroDoCastelo, "o castelo risca o ceu por onde passa");
            Pawn eu = main.Player.Pawn;
            Assert.IsFalse(v.RastroAceso(eu), "no castelo o jogador (invisivel) nao risca nada");
            Assert.AreEqual(0, v.Vento, "sem queda, sem vento");

            // a rota do castelo COMECA sobre o vazio da ilha flutuante (25/09): saltar ali e' morrer no vazio, sem pouso.
            // Espera (tempo acelerado so' nesta espera) o castelo passar sobre chao seco — o jogador salta como um humano.
            Time.timeScale = 10f;
            for (float w = 0f; w < 15f && !Arkana.World.Ilha.Atual.Chao.PodePousar(main.Castelo.transform.position.x, main.Castelo.transform.position.z); w += Time.unscaledDeltaTime)
                yield return null;
            Time.timeScale = 1f;
            Assert.IsTrue(Arkana.World.Ilha.Atual.Chao.PodePousar(main.Castelo.transform.position.x, main.Castelo.transform.position.z), "o castelo passou sobre a ilha");
            main.Player.Saltar();
            yield return Esperar(1.5f);
            Assert.AreEqual(Queda.CAINDO, eu.Queda.Fase, "ainda em queda livre");
            Assert.IsTrue(v.RastroAceso(eu), "caindo: rastro nas maos e pes");
            Assert.Greater(v.Vento, 0, "caindo: vento passando pela camera");

            float t = 0f;
            while (eu.Queda.NoAr && t < 25f) { yield return null; t += Time.deltaTime; }
            Assert.IsFalse(eu.Queda.NoAr, "pousou");
            yield return null;   // o LateUpdate do quadro do pouso le' a borda
            Vector3 d = v.UltimoPouso - eu.Pos;
            d.y = 0f;   // no mar o estalo sobe para a lamina
            Assert.Less(d.magnitude, 1f, "o estalo foi nos pes do jogador");
            Assert.IsFalse(v.RastroAceso(eu), "no chao o rastro apaga");
            yield return Esperar(0.6f);
            Assert.AreEqual(0, v.Vento, "no chao o vento para");

            if (main.Castelo != null) Object.Destroy(main.Castelo.gameObject);   // o fim da rota, adiantado
            yield return null;
            yield return null;
            Assert.IsFalse(v.CasteloVivo, "o castelo foi embora");
            Assert.IsFalse(v.RastroDoCastelo, "e o rastro dele para (o que ja' saiu apaga sozinho)");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
