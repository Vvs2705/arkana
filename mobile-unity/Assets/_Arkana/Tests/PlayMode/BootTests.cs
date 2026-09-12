using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// O BOOT de verdade, com cena: Main sozinho no mundo monta o menu, e o pedido de partida monta a arena inteira
    /// (ilha, castelo, jogador, 12 bots) e roda 3 s de frames sem erro de log. O treino nasce no chao, sem bots.
    /// </summary>
    public class BootTests
    {
        const float SEGUNDOS = 3f;
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
            if (_go != null) Object.Destroy(_go);   // Main.OnDestroy desmonta a arena e o que criou (ilha, sol, menu, sfx)
            yield return null;
        }

        static IEnumerator Rodar(float segundos)
        {
            float t = 0f;
            int frames = 0;
            while (t < segundos && frames < 600)
            {
                yield return null;
                t += Time.deltaTime;
                frames++;
            }
        }

        [UnityTest]
        public IEnumerator PartidaNormal_MontaArenaInteira_ERoda3sSemErro()
        {
            Main main = _go.AddComponent<Main>();
            yield return null;
            Assert.AreEqual(FluxoDeJogo.Estado.Menu, main.Fluxo.Atual, "boot cai no menu");
            Assert.IsNull(main.Partida, "sem pedido, sem partida");

            Bus.EmitGameStartRequested();   // o que o JOGAR do menu emite
            yield return Rodar(SEGUNDOS);

            Assert.AreEqual(FluxoDeJogo.Estado.Partida, main.Fluxo.Atual);
            Assert.IsNotNull(Ilha.Atual, "a ilha existe");
            Assert.IsNotNull(main.Partida);
            Assert.IsFalse(main.Partida.Treino, "pedido normal nao vira treino");
            Assert.IsTrue(main.Partida.Rodando);
            Assert.IsNotNull(main.Player, "jogador criado");
            Assert.IsTrue(main.Player.Pawn.Viva, "jogador vivo");
            Assert.AreEqual(Balance.Match.Bots, main.Bots.Count, "todos os bots criados");
            Assert.AreEqual(Balance.Match.Bots, main.Partida.BotsVivos);
            Assert.IsNotNull(main.Hud);
            Assert.IsNotNull(Camera.main, "a camera do jogador e' a MainCamera");
            // o volume sozinho nao muda nada na tela: a camera tem de pedir o pos
            Assert.IsNotNull(Ilha.Atual.Pos, "o volume do pos existe");
            Assert.IsTrue(Ilha.PosLigado(Camera.main), "a camera do jogador desenha o pos (bloom/tonemapping)");
            // A LICAO DE 11/09: o terreno reativo tinha 25 testes verdes e NENHUMA cena o criava. Sistema testado nao
            // prova sistema ligado — o boot cobra que a partida monta cada peca que o jogador precisa ver.
            Assert.IsNotNull(Arkana.Terrain.TerrenoReativoBehaviour.Atual, "o terreno reativo existe na partida");
            Assert.IsNotNull(Object.FindFirstObjectByType<Gameplay.VisualDaPartida>(), "loot, bau e tempestade tem quem desenhe");
            Assert.IsNotNull(Object.FindFirstObjectByType<Gameplay.VisualDosKits>(), "os kits tem quem desenhe");
            Assert.IsNotNull(Object.FindFirstObjectByType<Arkana.Terrain.VisualDoTerreno>(), "o terreno tem quem desenhe");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Treino_JogadorNoChao_SemBots_ComBonecos()
        {
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Rodar(1f);

            Assert.IsNotNull(main.Partida);
            Assert.IsTrue(main.Partida.Treino, "o pedido do menu vira treino");
            Assert.IsFalse(Arkana.Menu.Menu.PedidoDeTreino, "e e' consumido");
            Assert.IsFalse(main.Player.Pawn.Queda.NoAr, "no treino o jogador nasce no chao");
            Assert.IsTrue(main.Player.Pawn.PodeAgir, "e ja' pode agir");
            Assert.AreEqual(0, main.Bots.Count, "treino nao tem bots");
            Assert.AreEqual(Main.Bonecos, main.Partida.Bonecos.Count, "bonecos registrados");
            Assert.IsNull(main.Partida.Zona, "treino nao tem tempestade");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Treino_TaticaSaiNaMiraDaCamera_NaoNaFrenteDoCorpo()
        {
            // defeito (foto 17 de 12/09): o botao de tatica usava a frente do CORPO; parado, com a camera girada, a muralha
            // da Pyra nascia atras dela — fora da tela. Vermelho sem o YawAlvo do Player.Tatica.
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Rodar(1f);
            Gameplay.Pawn p = main.Player.Pawn;
            float yawCorpo = p.transform.eulerAngles.y * Mathf.Deg2Rad;
            main.Player.Camera.Logica.Yaw = yawCorpo + Mathf.PI;   // olhando para TRAS do corpo, sem andar
            yield return null;
            main.Player.Tatica();
            Assert.Greater(p.Runner.Visuais.Count, 0, "a tatica desenha alguma coisa");
            Gameplay.EfeitoVisual v = p.Runner.Visuais[p.Runner.Visuais.Count - 1];
            Vector3 mira = new Vector3(Mathf.Sin(yawCorpo + Mathf.PI), 0f, Mathf.Cos(yawCorpo + Mathf.PI));
            Vector3 centro = (v.Pos + v.Pos2) * 0.5f - p.Pos;
            centro.y = 0f;
            Assert.Greater(Vector3.Dot(centro, mira), 1f, v.Tipo + " saiu fora da mira da camera: " + centro);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Menu_VitrineMostraOMagoNoPico_ESomeNaPartida()
        {
            // o fundo do menu e' o mago escolhido no pico; o treino nasce LA' — o mago da vitrine nao pode sobrar na partida
            Main main = _go.AddComponent<Main>();
            yield return null;
            yield return null;
            Assert.IsNotNull(main.CameraDoMenu, "o menu tem camera");
            Assert.AreSame(main.CameraDoMenu, Camera.main, "no menu quem filma e' a camera do menu");
            var mago = Object.FindFirstObjectByType<Arkana.Characters.Mago>();
            Assert.IsNotNull(mago, "o mago da vitrine existe no menu");
            Vector2 pk = Ilha.Atual.Relevo.Pico;
            Vector3 p = mago.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(p.x, p.z), pk), 0.01f, "o mago esta' no pico");
            Assert.Less(Vector3.Distance(main.CameraDoMenu.transform.position, p), 8f, "a camera orbita perto dele");

            Bus.EmitGameStartRequested();
            yield return null;
            foreach (var m in Object.FindObjectsByType<Arkana.Characters.Mago>(FindObjectsSortMode.None))
                Assert.IsNotNull(m.GetComponentInParent<Gameplay.Pawn>(), "na partida so' ha' mago com corpo: o da vitrine sumiu");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MatchOver_VaiParaFim_EMenuDesmontaAArena()
        {
            Main main = _go.AddComponent<Main>();
            yield return null;
            Bus.EmitGameStartRequested();
            yield return Rodar(0.5f);
            main.Partida.Fim(false);
            yield return null;
            Assert.AreEqual(FluxoDeJogo.Estado.Fim, main.Fluxo.Atual, "MatchOver leva ao Fim");
            Assert.IsNotNull(main.Partida, "no Fim a arena fica de pe' (a HUD mostra o veredito)");

            Bus.EmitGameStartRequested();
            yield return null;
            Assert.AreEqual(FluxoDeJogo.Estado.Fim, main.Fluxo.Atual, "pedido fora do menu e' ignorado");

            main.Fluxo.VoltarAoMenu();
            yield return null;
            Assert.AreEqual(FluxoDeJogo.Estado.Menu, main.Fluxo.Atual);
            Assert.IsNull(main.Partida, "voltar ao menu desmonta a partida");
            Assert.IsNull(main.Player);
            Assert.AreEqual(0, GameObject.FindObjectsByType<Gameplay.Pawn>(FindObjectsSortMode.None).Length, "nenhum corpo sobra da arena velha");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
