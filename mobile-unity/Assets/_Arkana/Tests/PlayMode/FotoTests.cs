using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Arkana.Core;

namespace Arkana.Tests
{
    /// <summary>
    /// FOTOS do jogo rodando — a regua que o Godot deixou (characters/_shot_mago.gd, world/_shot.gd): julgar pelo QUADRO,
    /// nao so' pelo numero. Um teste verde nao diz se o mago saiu rosa, se a ilha ficou preta ou se a HUD saiu da tela.
    /// So' rodam com GPU: `powershell -File mobile-unity\foto.ps1` (sem -nographics). No portao (-nographics) sao IGNORADAS.
    /// Saida: mobile-unity/Logs/fotos/*.png (fora do git), na proporcao do Poco F4 (20:9).
    /// </summary>
    public class FotoTests
    {
        const int L = 2400, A = 1080;
        GameObject _go;

        static string Pasta => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "fotos"));

        /// <summary>Poco F4: 6,67" a 2400x1080 ~ 395 ppi. Sem isto o editor responde dpi 0 -> 160 e todo botao sai 2,5x menor.</summary>
        const float DpiDoAparelho = 395f;
        const int SeedDasFotos = 3103;

        /// <summary>Vira a camera do jogador para o centro da ilha: a foto mostra o mapa, nao a pedra em que ele encostou.</summary>
        static void OlharParaOCentro(Main main)
        {
            Vector3 p = main.Player.Pawn.Pos;
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(-p.x, -p.z);
            main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
        }

        static float CameraLogica_PitchPadrao() => Arkana.Gameplay.CameraLogica.PITCH_PADRAO;

        /// <summary>Junto de cada foto do jogador, o que a camera e o corpo estao tocando (Logs/fotos/diag.txt).
        /// Existe porque um quadro estranho sem dado vira palpite — e dois palpites ja' tinham errado.</summary>
        static void Diagnostico(Main main, string nome)
        {
            if (main == null || main.Player == null || main.Player.Pawn == null) return;
            var sb = new System.Text.StringBuilder();
            Vector3 p = main.Player.Pawn.Pos;
            Camera cam = main.Player.Camera.Cam;
            Vector3 c = cam.transform.position;
            sb.AppendLine(nome + ": pawn=" + p.ToString("F2") + " fase=" + main.Player.Pawn.Queda.Fase
                + " terreno=" + Arkana.World.Ilha.AlturaDoChao(p.x, p.z).ToString("F2")
                + " cam=" + c.ToString("F2") + " dist=" + Vector3.Distance(c, p + Vector3.up * 1.8f).ToString("F2"));
            foreach (Collider k in Physics.OverlapSphere(c, 0.3f)) sb.AppendLine("  camera dentro do colisor: " + Caminho(k.transform) + " (" + k.GetType().Name + ")");
            foreach (Collider k in Physics.OverlapSphere(p + Vector3.up, 0.7f)) sb.AppendLine("  corpo toca: " + Caminho(k.transform) + " (" + k.GetType().Name + ")");
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && r.bounds.Contains(c)) sb.AppendLine("  camera dentro dos bounds de: " + Caminho(r.transform));
            int visiveis = 0;
            foreach (Renderer r in main.Player.Pawn.GetComponentsInChildren<Renderer>()) if (r.enabled) visiveis++;
            sb.AppendLine("  renderers do mago ligados: " + visiveis);
            float terreno = Arkana.World.Ilha.AlturaDoChao(p.x, p.z);
            sb.AppendLine("  sonda Topo(x,z) = " + Arkana.Gameplay.ChaoComObstaculos.Topo(p.x, p.z, terreno).ToString("F2"));
            bool verso = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            RaycastHit[] hs = Physics.RaycastAll(new Vector3(p.x, terreno + 60f, p.z), Vector3.down, 61f, ~0, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = verso;
            foreach (RaycastHit h in hs) sb.AppendLine("  raio de cima acerta: " + Caminho(h.collider.transform) + " y=" + h.point.y.ToString("F2") + " convex=" + ((h.collider as MeshCollider) != null ? ((MeshCollider)h.collider).convex.ToString() : "-"));
            Collider[] emVolta = Physics.OverlapSphere(new Vector3(p.x, terreno + 3f, p.z), 6f);
            foreach (Collider k in emVolta) if (k is MeshCollider) sb.AppendLine("  colisor perto: " + Caminho(k.transform) + " bounds=" + k.bounds.min.ToString("F1") + ".." + k.bounds.max.ToString("F1") + " convex=" + ((MeshCollider)k).convex + " mesh=" + (((MeshCollider)k).sharedMesh != null ? ((MeshCollider)k).sharedMesh.vertexCount.ToString() : "null"));
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        static string Caminho(Transform t)
        {
            string s = t.name;
            for (Transform x = t.parent; x != null; x = x.parent) s = x.name + "/" + s;
            return s;
        }

        [SetUp]
        public void SetUp()
        {
            Arkana.Menu.Menu.PedidoDeTreino = false;
            Arkana.UI.Dp.DpiForcado = DpiDoAparelho;
            Main.SeedForcado = SeedDasFotos;   // a mesma partida em toda rodada: foto de hoje se compara com a de ontem
            _go = new GameObject("Main");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Arkana.Menu.Menu.PedidoDeTreino = false;
            Arkana.UI.Dp.DpiForcado = 0f;
            Main.SeedForcado = 0;
            if (_go != null) Object.Destroy(_go);
            yield return null;
        }

        static void ExigirGpu()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("sem GPU (-nographics): foto so' pelo foto.ps1");
        }

        static IEnumerator Esperar(float segundos)
        {
            float t = 0f;
            int frames = 0;
            // teto de quadros proporcional ao pedido: o teto fixo de 3000 cortava a espera de 76 s em ~51 s (a 59 fps)
            int teto = Mathf.CeilToInt(segundos * 400f) + 100;
            while (t < segundos && frames < teto) { yield return null; t += Time.deltaTime; frames++; }
        }

        /// <summary>Renderiza `cam` num RenderTexture (com a UI de tela, se pedida) e grava o PNG.</summary>
        static void Foto(Camera cam, string nome, bool comUi)
        {
            Assert.IsNotNull(cam, "sem camera para a foto " + nome);
            var rt = new RenderTexture(L, A, 24, RenderTextureFormat.ARGB32);
            RenderTexture antes = cam.targetTexture;
            cam.targetTexture = rt;   // ANTES do layout: a HUD mede o alvo da camera (Hud.TamanhoDaTela)
            // A UI e' ScreenSpaceOverlay (nao entra em RenderTexture): durante a foto ela vira ScreenSpaceCamera desta camera.
            Canvas[] canvases = comUi ? Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None) : new Canvas[0];
            var modos = new RenderMode[canvases.Length];
            var cams = new Camera[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modos[i] = canvases[i].renderMode;
                cams[i] = canvases[i].worldCamera;
                if (!canvases[i].isRootCanvas || canvases[i].renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = cam.nearClipPlane + 0.05f;
            }
            Arkana.UI.Hud[] huds = comUi ? Object.FindObjectsByType<Arkana.UI.Hud>(FindObjectsSortMode.None) : new Arkana.UI.Hud[0];
            for (int i = 0; i < huds.Length; i++) huds[i].Layout();
            Canvas.ForceUpdateCanvases();

            cam.Render();
            cam.targetTexture = antes;

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null) continue;
                canvases[i].renderMode = modos[i];
                canvases[i].worldCamera = cams[i];
            }
            for (int i = 0; i < huds.Length; i++) if (huds[i] != null) huds[i].Layout();

            RenderTexture.active = rt;
            var tex = new Texture2D(L, A, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, L, A), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Pasta);
            File.WriteAllBytes(Path.Combine(Pasta, nome + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        static Camera CameraTemporaria(string nome, Vector3 pos, Vector3 olhar, Color fundo)
        {
            var go = new GameObject(nome);
            var c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.Skybox;
            c.backgroundColor = fundo;
            c.fieldOfView = 50f;
            c.nearClipPlane = 0.3f;
            c.farClipPlane = 4000f;
            c.enabled = false;   // so' renderiza quando a foto pede
            go.transform.position = pos;
            go.transform.LookAt(olhar);
            return c;
        }

        [UnityTest]
        public IEnumerator Foto_Menu()
        {
            ExigirGpu();
            _go.AddComponent<Main>();
            yield return Esperar(0.5f);
            Camera c = CameraTemporaria("CamFotoMenu", new Vector3(0f, 3f, -5f), Vector3.zero, Arkana.Menu.Estilo.Noite);
            c.clearFlags = CameraClearFlags.SolidColor;
            Foto(c, "01-menu", true);
            Object.Destroy(c.gameObject);
        }

        [UnityTest]
        public IEnumerator Foto_Treino_JogadorEIlha()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Foto(main.Player.Camera.Cam, "02-treino-jogador", true);
            Diagnostico(main, "02-treino-jogador");

            Vector3 p = main.Player.Pawn.Pos;
            Camera perto = CameraTemporaria("CamFotoPerto", p + new Vector3(2.2f, 1.6f, 3.2f), p + Vector3.up * 1.0f, Color.gray);
            Foto(perto, "03-mago-de-perto", false);
            Object.Destroy(perto.gameObject);

            Camera aerea = CameraTemporaria("CamFotoAerea", new Vector3(0f, 420f, -430f), new Vector3(0f, 0f, 20f), Color.gray);
            Foto(aerea, "04-ilha-aerea", false);
            Object.Destroy(aerea.gameObject);

            Camera rasante = CameraTemporaria("CamFotoRasante", p + new Vector3(-18f, 9f, -22f), p + new Vector3(10f, 0f, 30f), Color.gray);
            Foto(rasante, "05-chao-rasante", false);
            Object.Destroy(rasante.gameObject);
        }

        [UnityTest]
        public IEnumerator Foto_Partida_CasteloQuedaPouso()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "partida sem jogador");
            Foto(main.Player.Camera.Cam, "06-castelo", true);

            // salta quando o castelo esta' sobre a ilha (no comeco da rota ele ainda esta' no mar)
            float t = 0f;
            while (main.Castelo != null && main.Castelo.Progresso < 0.42f && t < 15f) { yield return null; t += Time.deltaTime; }
            main.Player.Saltar();
            yield return Esperar(2.5f);
            Foto(main.Player.Camera.Cam, "07-queda", true);

            t = 0f;
            while (main.Player != null && main.Player.Pawn.Queda.NoAr && t < 20f) { yield return null; t += Time.deltaTime; }
            OlharParaOCentro(main);
            yield return Esperar(1.5f);
            Foto(main.Player.Camera.Cam, "08-pouso", true);
            Diagnostico(main, "08-pouso");

            // 70 s de abertura + formacao: a tempestade existe, o bau ja' anunciou, o loot esta' no chao
            yield return Esperar(76f);
            Camera aerea = CameraTemporaria("CamFotoZona", new Vector3(0f, 380f, -380f), Vector3.zero, Color.gray);
            Foto(aerea, "09-zona-aerea", false);
            Object.Destroy(aerea.gameObject);
            if (main.Player != null && main.Player.Camera != null && main.Player.Pawn != null)
            {
                OlharParaOCentro(main);
                yield return Esperar(0.5f);
                Foto(main.Player.Camera.Cam, "10-jogador-na-zona", true);
                Diagnostico(main, "10-jogador-na-zona");
            }
        }
    }
}
