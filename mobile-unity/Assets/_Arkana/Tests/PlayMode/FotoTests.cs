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
            Arkana.World.Ilha.LigarPos(c);   // a foto mostra o quadro que o jogador ve', com o pos
            go.transform.position = pos;
            go.transform.LookAt(olhar);
            return c;
        }

        [UnityTest]
        public IEnumerator Foto_Menu()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return Esperar(1.2f);   // a vitrine poe o mago no pico e o corte por distancia (4 Hz) acorda
            Assert.IsNotNull(main.CameraDoMenu, "o menu tem camera (a vitrine 3D do fundo)");
            Foto(main.CameraDoMenu, "01-menu", true);
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

        /// <summary>
        /// FOLHA DE CLIPES do mago externo: os 10 clipes lado a lado, no cenario do jogo. Existe porque o "Planar horizontal
        /// v2" da Meshy mergulhava de cabeca para baixo e so' o Diretor viu (12/09) — clipe torto tem de aparecer AQUI antes.
        /// </summary>
        static System.Collections.Generic.IEnumerable<string> SlugsExternos()
        {
            foreach (string s in Arkana.Core.Kits.Slugs)
                if (Resources.Load<GameObject>("magos/" + s) != null) yield return s;
        }

        /// <summary>O ELENCO lado a lado, parado, na luz do jogo: o quadro que mostra ao Diretor o que ja' entrou.</summary>
        [UnityTest]
        public IEnumerator Foto_Elenco_Todos()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");

            var slugs = new System.Collections.Generic.List<string>(SlugsExternos());
            Assume.That(slugs.Count, Is.GreaterThan(0), "nenhum FBX em Resources/magos");
            Vector3 c = main.Player.Pawn.Pos + new Vector3(14f, 0f, 0f);
            const float passo = 1.5f;
            int colunas = Mathf.Min(10, slugs.Count);   // 20 numa fila so' viram formigas: duas filas de 10
            var magos = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < slugs.Count; i++)
            {
                int col = i % colunas, fila = i / colunas;
                Vector3 p = c + new Vector3((col - (colunas - 1) * 0.5f + fila * 0.5f) * passo, 0f, -fila * 2.6f);
                p.y = Arkana.World.Ilha.AlturaDoChao(p.x, p.z);
                var m = Arkana.Characters.Mago.Criar(null, slugs[i]);
                m.transform.position = p;
                magos.Add(m.gameObject);
            }
            yield return Esperar(0.6f);
            float largura = colunas * passo;
            Camera cam = CameraTemporaria("CamFotoElenco", c + new Vector3(0f, 2.6f, 1.2f + largura * 0.95f), c + new Vector3(0f, 0.9f, -1.3f), Color.gray);
            Foto(cam, "12-elenco", false);
            Object.Destroy(cam.gameObject);
            foreach (GameObject g in magos) Object.Destroy(g);
        }

        /// <summary>As pecas da OFICINA no lugar delas: o Altar de Sintonia no vale, o anel das ruinas, a torre arcana.</summary>
        [UnityTest]
        public IEnumerator Foto_Kit_AltarRuinasTorre()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Assert.IsNotNull(ilha.Kit, "sem kit");
            // o corte por distancia mede a camera do JOGADOR (no pico): liga tudo para a camera da foto
            foreach (Renderer r in ilha.Kit.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            var sb = new System.Text.StringBuilder();

            GameObject bras = GameObject.Find("27-braseiro-elemental");
            Assert.IsNotNull(bras, "o altar nao nasceu");
            Vector3 c = bras.transform.position;
            sb.AppendLine("13-altar: braseiro=" + c.ToString("F1") + " obeliscos=" + ilha.Kit.Contar("33-obelisco") + " plataformas=" + ilha.Kit.Contar("34-plataforma-sintonia"));
            Camera cam = CameraTemporaria("CamFotoAltar", c + new Vector3(6.5f, 3.4f, -7.5f), c + Vector3.up * 0.6f, Color.gray);
            Foto(cam, "13-altar", false);
            Object.Destroy(cam.gameObject);

            Vector2 ru = ilha.Relevo.Ruinas;
            float rr = ilha.Relevo.RuinasR;
            Vector3 centro = new Vector3(ru.x, Arkana.World.Ilha.AlturaDoChao(ru.x, ru.y), ru.y);
            sb.AppendLine("14-ruinas: centro=" + centro.ToString("F1") + " arcos=" + ilha.Kit.Contar("22-arco-partido") + " colunas=" + ilha.Kit.Contar("23-coluna-braseiro") + " estatuas=" + ilha.Kit.Contar("24-estatua-vigia"));
            cam = CameraTemporaria("CamFotoRuinas", centro + new Vector3(-rr * 1.1f, 16f, rr * 1.1f), centro + Vector3.up * 2f, Color.gray);
            Foto(cam, "14-ruinas", false);
            Object.Destroy(cam.gameObject);

            GameObject torre = GameObject.Find("26-torre-arcana");
            Assert.IsNotNull(torre, "a torre nao nasceu");
            Vector3 t = torre.transform.position;
            sb.AppendLine("15-torre: pos=" + t.ToString("F1") + " torres=" + ilha.Kit.Contar("26-torre-arcana"));
            cam = CameraTemporaria("CamFotoTorre", t + new Vector3(20f, 5f, -20f), t + Vector3.up * 9f, Color.gray);
            Foto(cam, "15-torre", false);
            Object.Destroy(cam.gameObject);

            // o BAU e as tres LUVAS de perto, pelo mesmo caminho da partida (VisualDaPartida.Modelo): e' a distancia em
            // que o jogador canaliza o bau e pega a luva — buraco de decimacao aparece aqui ou nao aparece
            Vector3 b0 = main.Player.Pawn.Pos + new Vector3(4f, 0f, 0f);
            b0.y = Arkana.World.Ilha.AlturaDoChao(b0.x, b0.z);
            var pivos = new System.Collections.Generic.List<GameObject>();
            var pb = new GameObject("FotoBau"); pb.transform.position = b0; pivos.Add(pb);
            Gameplay.VisualDaPartida.Modelo(Gameplay.BauVisual.MODELO, pb.transform, Gameplay.BauVisual.TAMANHO, true);
            string[] luvas = Gameplay.Arma.TIERS;
            for (int i = 0; i < luvas.Length; i++)
            {
                var pl = new GameObject("FotoLuva" + i);
                pl.transform.position = b0 + new Vector3(-0.2f + i * 0.75f, 1.0f, -1.3f);
                pivos.Add(pl);
                Gameplay.VisualDaPartida.Modelo(Gameplay.LootVisual.ModeloDe(luvas[i]), pl.transform, Gameplay.LootVisual.TAMANHO_LUVA, false);
            }
            yield return null;
            cam = CameraTemporaria("CamFotoBau", b0 + new Vector3(0.4f, 1.5f, -3.4f), b0 + new Vector3(0.3f, 0.6f, -0.4f), Color.gray);
            cam.fieldOfView = 40f;
            Foto(cam, "16-bau-luvas", false);
            Object.Destroy(cam.gameObject);
            foreach (GameObject g in pivos) Object.Destroy(g);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        /// <summary>O KIT da Pyra em acao (tatica e suprema) de lado: o VFX de assinatura se julga pelo quadro, com o bloom.</summary>
        [UnityTest]
        public IEnumerator Foto_Kit_PyraTaticaSuprema()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(Gameplay.Partida.SUPREMA_TREINO_S + 1f);   // no treino a suprema enche em 5 s
            Assert.IsNotNull(main.Player, "treino sem jogador");
            // o kit sai na MIRA (yaw da camera do jogador): vira para o centro da ilha (longe das colunas do loot do
            // treino, que ficam no nascimento) e fotografa pela propria camera do jogador — o quadro que ele ve'
            OlharParaOCentro(main);
            yield return Esperar(0.3f);
            main.Player.Tatica();
            yield return Esperar(0.9f);   // a chama sobe 1,4-3 m/s: aos 0,3 s ela ainda estava no chao
            Foto(main.Player.Camera.Cam, "17-kit-tatica", true);
            // o que o kit escreveu e o que a casca desenhou (um quadro sem efeito, sem dado, vira palpite)
            var sb = new System.Text.StringBuilder("17-kit-tatica: pawn=" + main.Player.Pawn.Pos.ToString("F1")
                + " cam=" + main.Player.Camera.Cam.transform.position.ToString("F1") + " visuais=" + main.Player.Pawn.Runner.Visuais.Count + "\n");
            foreach (var v in main.Player.Pawn.Runner.Visuais) sb.AppendLine("  visual " + v.Tipo + " " + v.Pos.ToString("F1") + " -> " + v.Pos2.ToString("F1") + " restante=" + v.Restante.ToString("F2"));
            var vk = Object.FindFirstObjectByType<Gameplay.VisualDosKits>();
            if (vk != null)
                foreach (Renderer r in vk.GetComponentsInChildren<Renderer>(true))
                    sb.AppendLine("  desenho " + Caminho(r.transform) + " ativo=" + r.gameObject.activeInHierarchy + " ligado=" + r.enabled
                        + " bounds=" + r.bounds.center.ToString("F1") + " tam=" + r.bounds.size.ToString("F1") + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name : "NULL"));
            else sb.AppendLine("  SEM VisualDosKits");
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            yield return Esperar(1.2f);
            var runner = main.Player.Pawn.Runner;
            string antes = "18-kit-suprema: carga=" + runner.CargaSuprema.ToString("F2") + " pronto=" + runner.ProntoSuprema;
            main.Player.Suprema();
            yield return Esperar(2f);   // a suprema e' TELEGRAFADA: o braco livre so' sai quando o aviso no chao enche
            Foto(main.Player.Camera.Cam, "18-kit-suprema", true);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), antes + " visuais depois=" + runner.Visuais.Count + "\n");
        }

        /// <summary>A ESCOLHA DE MAGO sobre a vitrine 3D: a grade a' esquerda, o mago tocado troca NA HORA no pico a' direita.</summary>
        [UnityTest]
        public IEnumerator Foto_Elenco_Escolha()
        {
            ExigirGpu();
            string antes = PlayerPrefs.GetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, "");
            try
            {
                Main main = _go.AddComponent<Main>();
                yield return null;
                Tocar("TapTitulo");
                yield return null;
                Tocar("BtnElenco");
                yield return null;
                Tocar("Card03-veu/Toque");
                yield return null;
                var mago = Object.FindFirstObjectByType<Arkana.Characters.Mago>();
                Assert.IsNotNull(mago, "a vitrine tem mago");
                Assert.AreEqual("Mago 03-veu", mago.name, "o toque no retrato troca o mago da vitrine na hora");
                yield return Esperar(1.2f);
                Foto(main.CameraDoMenu, "21-elenco", true);
            }
            finally
            {
                PlayerPrefs.SetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, antes);
            }
        }

        static void Tocar(string caminho)
        {
            GameObject go = GameObject.Find(caminho);
            Assert.IsNotNull(go, "sem " + caminho + " na tela");
            go.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }

        /// <summary>Os outros dois kits implementados (Veu e Tessa) em acao, pelo mesmo roteiro da Pyra: tatica e suprema no treino.</summary>
        [UnityTest]
        public IEnumerator Foto_Kit_VeuTessa([Values("03-veu", "10-tessa")] string slug)
        {
            ExigirGpu();
            // a escolha do mago mora nos PlayerPrefs (persiste no editor): guarda e devolve, a foto nao troca o mago de ninguem
            string antes = PlayerPrefs.GetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, "");
            try
            {
                Arkana.Menu.SelecaoPersonagem.MagoEscolhido = slug;
                Main main = _go.AddComponent<Main>();
                yield return null;
                Arkana.Menu.Menu.PedidoDeTreino = true;
                Bus.EmitGameStartRequested();
                yield return Esperar(Gameplay.Partida.SUPREMA_TREINO_S + 1f);
                Assert.IsNotNull(main.Player, "treino sem jogador");
                OlharParaOCentro(main);
                yield return Esperar(0.3f);
                main.Player.Tatica();
                yield return Esperar(0.9f);
                Foto(main.Player.Camera.Cam, "19-kit-" + slug + "-tatica", true);
                yield return Esperar(1.2f);
                main.Player.Suprema();
                yield return Esperar(2f);
                Foto(main.Player.Camera.Cam, "20-kit-" + slug + "-suprema", true);
                var sb = new System.Text.StringBuilder("19/20-kit-" + slug + ": visuais=" + main.Player.Pawn.Runner.Visuais.Count + "\n");
                foreach (var v in main.Player.Pawn.Runner.Visuais) sb.AppendLine("  visual " + v.Tipo + " " + v.Pos.ToString("F1") + " restante=" + v.Restante.ToString("F2"));
                File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            }
            finally
            {
                PlayerPrefs.SetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, antes);
            }
        }

        [UnityTest]
        public IEnumerator Foto_Elenco_ClipesDoMago([ValueSource(nameof(SlugsExternos))] string slug)
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");

            Vector3 p = main.Player.Pawn.Pos + new Vector3(12f, 0f, 0f);
            p.y = Arkana.World.Ilha.AlturaDoChao(p.x, p.z) + 1.2f;   // no ar: queda, planeio e nado sao horizontais
            var m = Arkana.Characters.Mago.Criar(null, slug);
            m.transform.position = p;
            Camera cam = CameraTemporaria("CamFotoClipes", p + new Vector3(0f, 1.1f, 4.2f), p + Vector3.up * 0.8f, Color.gray);

            const int cw = 480, ch = 640, colunas = 6;
            Arkana.Characters.Clipe[] todos = Arkana.Characters.PoseMago.Todos;
            int linhas = (todos.Length + colunas - 1) / colunas;   // a folha cresce com o contrato (onda 15: pular, pousar, andar para tras)
            var folha = new Texture2D(cw * colunas, ch * linhas, TextureFormat.RGB24, false);
            var rt = new RenderTexture(cw, ch, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            for (int i = 0; i < todos.Length; i++)
            {
                m.Play(todos[i]);
                bool unico = !Arkana.Characters.PoseMago.Laco(todos[i]);
                yield return Esperar(unico ? 0.35f : 0.8f);   // disparo unico: foto no meio, antes de voltar ao idle
                cam.Render();
                RenderTexture.active = rt;
                folha.ReadPixels(new Rect(0, 0, cw, ch), (i % colunas) * cw, (linhas - 1 - i / colunas) * ch);
                RenderTexture.active = null;
            }
            folha.Apply();
            Directory.CreateDirectory(Pasta);
            File.WriteAllBytes(Path.Combine(Pasta, "11-clipes-" + slug + ".png"), folha.EncodeToPNG());
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), "11-clipes-" + slug + ": fonte=" + m.Fonte
                + " ordem=" + string.Join(",", todos) + "\n");
            cam.targetTexture = null;
            Object.Destroy(folha);
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(cam.gameObject);
            Object.Destroy(m.gameObject);
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

        /// <summary>
        /// As telas que fecham a partida, no idioma da HUD: PAUSA (veu sobre o jogo congelado + placa central) e o FIM nos dois
        /// vereditos (placa grande, titulo com brilho, colocacao e abates, botoes-placa). Partida NORMAL: o treino nao tem
        /// colocacao e a placa sairia sem o chip. Tudo pelo caminho do jogo: o II pelo toque, o veredito por Partida.Fim ->
        /// Bus.MatchOver, e a segunda partida pelo JOGAR DE NOVO (arena nova, mesmo seed).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Telas_FimEPausa()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "partida sem jogador");
            Assert.IsNotNull(main.Hud, "partida sem HUD");

            // PAUSA pelo caminho do dedo: o II e' um BotaoAcao (OnPointerDown -> Tocado -> AbrirPausa)
            var toque = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            main.Hud.Pausa.OnPointerDown(toque);
            main.Hud.Pausa.OnPointerUp(toque);
            Assert.IsTrue(main.Hud.Pausado, "o II pausa");
            Assert.AreEqual(0f, Time.timeScale, "a pausa congela o jogo");
            yield return null;   // timeScale 0: o quadro passa, o relogio do jogo nao (a pausa abre sem animacao)
            Foto(main.Player.Camera.Cam, "23-pausa", true);
            Tocar("BtnRetomar");
            Assert.IsFalse(main.Hud.Pausado, "RETOMAR solta a pausa");
            Assert.AreEqual(1f, Time.timeScale, "e devolve o relogio");

            // DERROTA com a arena cheia: 12 bots de pe' = colocacao #13
            main.Partida.Fim(false);
            yield return Esperar(0.8f);   // a placa pousa em 0,35 s (relogio sem escala)
            Assert.AreEqual(FluxoDeJogo.Estado.Fim, main.Fluxo.Atual, "MatchOver leva ao Fim");
            Foto(main.Player.Camera.Cam, "22-fim-derrota", true);

            // VITORIA: JOGAR DE NOVO pelo botao da placa e o outro veredito (#1, titulo em ouro respirando)
            Tocar("BtnJogarDeNovo");
            yield return Esperar(2f);
            Assert.AreEqual(FluxoDeJogo.Estado.Partida, main.Fluxo.Atual, "JOGAR DE NOVO monta partida nova");
            main.Partida.Fim(true);
            yield return Esperar(0.8f);
            Foto(main.Player.Camera.Cam, "22-fim-vitoria", true);
        }

        /// <summary>
        /// O PESO do acerto: um raio do jogador no boneco (escudo N1 -> bolha, numero de dano na HUD, estouro onde o tiro
        /// para) e os cinco estouros lado a lado no chao, na ordem do enum. Foto sem Esperar depois do acerto: a piscada
        /// dura 0,08 s.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Combate_Impacto()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");

            var alvo = (Gameplay.Pawn)main.Partida.Bonecos[0];
            Vector3 d = alvo.Pos - main.Player.Pawn.Pos; d.y = 0f;
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(d.x, d.z);
            yield return Esperar(0.3f);
            main.Partida.Registrar(Gameplay.Projetil.Lancar(main.Player.Pawn, alvo.Pos + Vector3.up * 1.1f - d.normalized * 1.5f, d.normalized, Elemento.Raio));
            float t = 0f;
            while (main.Partida.Projeteis.Count > 0 && t < 3f) { yield return null; t += Time.deltaTime; }
            yield return null;
            Foto(main.Player.Camera.Cam, "24-impacto", true);

            var vi = Object.FindFirstObjectByType<Gameplay.VisualDoImpacto>();
            Assert.IsNotNull(vi, "o acerto tem quem desenhe");
            float yaw = main.Player.Camera.Logica.Yaw;
            Vector3 frente = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)), lado = new Vector3(frente.z, 0f, -frente.x);
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = main.Player.Pawn.Pos + frente * 6f + lado * ((i - 2) * 1.8f);
                p.y = Arkana.World.Ilha.AlturaDoChao(p.x, p.z) + 0.3f;
                vi.Estourar((Elemento)i, p);
            }
            yield return Esperar(0.12f);   // abriu: a 0,05 s os cinco ainda eram cinco pontos brancos
            Foto(main.Player.Camera.Cam, "25-estouros", true);
            int n = 0;
            foreach (var ps in vi.GetComponentsInChildren<ParticleSystem>()) n += ps.particleCount;
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), "25-estouros: particulas vivas=" + n + "\n");
            Assert.Greater(n, 0, "os estouros nascem");
        }

        /// <summary>
        /// Os PEDREGULHOS da Meshy (onda 5B) no lugar dos blobs marrons: o quadro do treino (a "piramide de papelao" da foto
        /// 02), o grupo de pedras mais cheio perto do jogador com a camera baixa (onde pedra boiando ou estilhacada aparece ou
        /// nao aparece) e o chao do cume do pico com os seixos novos.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Pedras_TreinoPertoECume()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Assert.IsNotNull(ilha.Vegetacao, "sem vegetacao");

            // 1) o quadro do jogador, o mesmo da foto 02
            Foto(main.Player.Camera.Cam, "27-pedras-treino", true);
            Diagnostico(main, "27-pedras-treino");

            // 2) o grupo mais cheio perto do jogador: vizinhos a 12 m valem 25 m de distancia cada
            var pedras = new System.Collections.Generic.List<Renderer>();
            foreach (Transform t in ilha.Vegetacao.GetComponentsInChildren<Transform>())
            {
                if (t.name != "Pedregulho") continue;
                Renderer r = t.GetComponentInChildren<Renderer>();   // o glTFast poe o renderer na raiz ou num filho
                if (r != null) pedras.Add(r);
            }
            var sb = new System.Text.StringBuilder("27-pedras: molde=" + ilha.Vegetacao.MoldeDasRochas + " pedregulhos=" + pedras.Count
                + " rochas=" + ilha.Vegetacao.ContarRochas() + " seixos=" + (ilha.Grama != null ? ilha.Grama.Contar("Seixos") : 0) + "\n");
            Assert.Greater(pedras.Count, 0, "nenhum pedregulho da Meshy (molde " + ilha.Vegetacao.MoldeDasRochas + ")");
            Vector3 p = main.Player.Pawn.Pos;
            Renderer melhor = null;
            float nota = float.MinValue;
            foreach (Renderer a in pedras)
            {
                int viz = 0;
                foreach (Renderer b in pedras) if ((a.bounds.center - b.bounds.center).sqrMagnitude < 144f) viz++;
                float n = viz * 25f - Vector3.Distance(a.bounds.center, p);
                if (n > nota) { nota = n; melhor = a; }
            }
            Bounds lim = melhor.bounds;
            MeshFilter mf = melhor.GetComponent<MeshFilter>();
            sb.AppendLine("  grupo: centro=" + lim.center.ToString("F1") + " tam=" + lim.size.ToString("F1") + " dist ao jogador="
                + Vector3.Distance(lim.center, p).ToString("F0") + " tris do molde=" + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.GetIndexCount(0) / 3 : 0)
                + " mat=" + (melhor.sharedMaterial != null ? melhor.sharedMaterial.name + " / " + melhor.sharedMaterial.shader.name : "NULL")
                + " sombra=" + melhor.shadowCastingMode);
            // do lado do centro da ilha olhando para fora: a pedra recorta contra o ceu e o mar
            Vector3 praCentro = new Vector3(-lim.center.x, 0f, -lim.center.z).normalized;
            if (praCentro.sqrMagnitude < 0.5f) praCentro = Vector3.right;
            Vector3 olho = lim.center + praCentro * Mathf.Max(6f, lim.extents.magnitude * 3f);
            olho.y = Mathf.Max(Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 1.8f, lim.center.y + 0.6f);
            Camera cam = CameraTemporaria("CamFotoPedras", olho, lim.center, Color.gray);
            Foto(cam, "27-pedras-perto", false);
            Object.Destroy(cam.gameObject);

            // 3) o chao do cume: os seixos quebram a tampa lisa (a camera do ombro, um pouco mais baixa e mais perto)
            cam = CameraTemporaria("CamFotoCume", p + new Vector3(-3f, 1.9f, -3.5f), p + new Vector3(2f, 0f, 4f), Color.gray);
            Foto(cam, "27-pedras-cume", false);
            Object.Destroy(cam.gameObject);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        /// <summary>
        /// SOMBRA E PROFUNDIDADE (onda 5C): a sombra do mago pela camera do jogador no treino, o pe' dele de perto (o mago
        /// tem de PISAR na sombra, sem vao) e as ruinas com sombra e SSAO. As tres cameras olham a ~60 graus do sol, com a
        /// sombra caindo para a DIREITA do quadro: inteira na foto, longe do joystick e sem se esconder atras do mago.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Sombra_PertoPeCenario()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);   // e da' tempo de o editor compilar as variantes novas (cascata + macia)
            Assert.IsNotNull(main.Player, "treino sem jogador");

            var sol = Object.FindFirstObjectByType<Arkana.World.Sol>();
            Assert.IsNotNull(sol, "sem sol");
            Vector3 l = sol.transform.forward; l.y = 0f; l.Normalize();                    // para onde a sombra cai
            Vector3 v = (l * 0.5f + new Vector3(-l.z, 0f, l.x) * 0.87f).normalized;        // olhar a 60 graus do sol

            main.Player.Camera.Logica.Yaw = Mathf.Atan2(v.x, v.z);
            main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
            yield return Esperar(1f);   // a camera assenta no yaw novo
            Foto(main.Player.Camera.Cam, "28-sombra-perto", true);
            Diagnostico(main, "28-sombra-perto");

            Vector3 p = main.Player.Pawn.Pos;
            Camera pe = CameraTemporaria("CamFotoSombraPe", p - v * 3.2f + Vector3.up * 1.3f, p + v * 0.6f + Vector3.up * 0.3f, Color.gray);
            Foto(pe, "28-sombra-pe", false);
            Object.Destroy(pe.gameObject);

            // o ARCO das ruinas (5 m: sombra de ~9 m com o sol a 30 graus) a 16 m, dentro da 3a cascata; sem arco, o centro
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            if (ilha.Kit != null)
                foreach (Renderer r in ilha.Kit.GetComponentsInChildren<Renderer>(true)) r.enabled = true;   // o corte mede a camera do jogador
            GameObject arco = GameObject.Find("22-arco-partido");
            Vector2 ru = ilha.Relevo.Ruinas;
            Vector3 alvo = arco != null ? arco.transform.position : new Vector3(ru.x, Arkana.World.Ilha.AlturaDoChao(ru.x, ru.y), ru.y);
            Vector3 c = alvo - v * 16f;
            c.y = Arkana.World.Ilha.AlturaDoChao(c.x, c.z) + 5f;
            Camera cen = CameraTemporaria("CamFotoSombraCenario", c, alvo + Vector3.up * 1.5f, Color.gray);
            Foto(cen, "28-sombra-cenario", false);
            Object.Destroy(cen.gameObject);

            Light luz = sol.GetComponent<Light>();
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), "28-sombra: " + Arkana.World.Sol.Estado()
                + " alcance=" + QualitySettings.shadowDistance.ToString("F0") + "m luz=" + luz.shadows + " forca=" + luz.shadowStrength.ToString("F2")
                + " queda-da-sombra=" + l.ToString("F2") + " pawn=" + p.ToString("F1")
                + " cenario: " + (arco != null ? "arco" : "centro das ruinas (sem arco)") + " alvo=" + alvo.ToString("F1") + " cam=" + c.ToString("F1") + "\n");
        }

        /// <summary>
        /// DERRUBADO E ELIMINADO no TREINO: deterministico (sem castelo, sem pouso por hora — a versao na partida normal
        /// dependia de onde o castelo estava e uma rodada pousou os dois no convés, a 262 m). O boneco morre de um tiro de
        /// verdade (Projetil -> Combat -> EntityDied + PlayerKilledBot) e o treino o levanta; depois cai pela porta do estado
        /// (bot solo nao cai pela regra: sem esquadrao). O corpo que AFUNDA fica com o AbateVisualTests (partida normal).
        /// 29-eliminado: coluna de alma, faiscas e a faixa ELIMINADO. 29-derrubado: anel vermelho, losango e o anel do tempo.
        /// 29-jogador-derrubado: a vinheta nas bordas, o painel da HUD e o anel sob o proprio jogador.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Combate_DerrubadoEliminado()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Gameplay.Pawn eu = main.Player.Pawn;
            var alvo = (Gameplay.Pawn)main.Partida.Bonecos[0];
            Vector3 d = alvo.Pos - eu.Pos; d.y = 0f;
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(d.x, d.z);
            main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
            yield return Esperar(0.3f);
            var vis = Object.FindFirstObjectByType<Gameplay.VisualDoAbate>();
            Assert.IsNotNull(vis, "o abate tem quem desenhe");
            var sb = new System.Text.StringBuilder();

            // ELIMINADO: escudo zerado (senao o escudo N1 do boneco segura o tiro) e a vida no fim; o tiro de fogo fecha
            alvo.Vital.Escudo = 0f;
            alvo.Vital.Hp = 3f;
            main.Partida.Registrar(Gameplay.Projetil.Lancar(eu, alvo.Pos + Vector3.up * 1.1f - d.normalized * 1.5f, d.normalized, Elemento.Fogo));
            float t = 0f;
            while (main.Hud.Logica.Eliminado.Length == 0 && t < 2f) { yield return null; t += Time.deltaTime; }
            yield return Esperar(0.2f);   // a coluna ja' subiu, as faiscas no ar, a faixa assentando
            Foto(main.Player.Camera.Cam, "29-eliminado", true);
            int part = 0;
            foreach (var ps in vis.GetComponentsInChildren<ParticleSystem>()) part += ps.particleCount;
            sb.AppendLine("29-eliminado: alvo=" + alvo.Nome + " faixa='" + main.Hud.Logica.Eliminado + "' visivel=" + main.Hud.Logica.EliminadoVisivel
                + " particulas=" + part + " t=" + t.ToString("F2"));
            Assert.AreEqual(alvo.Nome, main.Hud.Logica.Eliminado, "a faixa diz QUEM caiu");

            // DERRUBADO: o treino ja' levantou o boneco; ele cai pela porta do estado e um golpe drena o anel do tempo. Caido SEM
            // ninguem de pe' no time sai no proximo tique (onda 17): o boneco e o jogador ganham um aliado de pe' (outro boneco)
            yield return Esperar(0.5f);
            Combat.DefinirTime(alvo, 7);
            Combat.DefinirTime(main.Partida.Bonecos[1], 7);
            new Gameplay.Derrubado(alvo).Cair(eu);
            Combat.AplicarDano(alvo, 35f, Elemento.Terra, eu);
            yield return Esperar(1.2f);
            Gameplay.Derrubado dd = Gameplay.Derrubado.De(alvo);
            Foto(main.Player.Camera.Cam, "29-derrubado", true);
            sb.AppendLine("29-derrubado: caido=" + (dd != null) + " esvaecimento=" + (dd != null ? dd.Esvaecimento.ToString("F2") : "-")
                + " aneis=" + vis.CaidosNaTela + " clipe=" + alvo.Clipe);

            // O JOGADOR DERRUBADO: a vinheta, o painel da HUD e o anel sob ele; o golpe tira luz e engrossa a vinheta. O treino so'
            // tem 2 bonecos: o de pe' passa para o time do jogador (e o caido levanta, senao sairia sem aliado de pe')
            Gameplay.Derrubado.Reerguer(alvo);
            Combat.DefinirTime(main.Partida.Bonecos[1], Combat.TIME_DO_PLAYER);
            new Gameplay.Derrubado(eu).Cair(null);
            Combat.AplicarDano(eu, 45f, Elemento.Terra, null);   // terra: sem queimadura, a vinheta da foto e' so' a do caido
            yield return Esperar(1f);
            Foto(main.Player.Camera.Cam, "29-jogador-derrubado", true);
            sb.AppendLine("29-jogador-derrubado: caido=" + main.Hud.Aviso.Caido + " esvaecimento=" + main.Hud.Aviso.Esvaecimento.ToString("F2") + " aneis=" + vis.CaidosNaTela);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.IsNotNull(dd, "o boneco caiu");
        }

        /// <summary>
        /// O CHAO VIVO do pico (onda 5A): de perto, onde o treino nasce e se pousa (pedra gasta, terra quente e liquen; fissura
        /// e seixo), e a ENCOSTA mais ingreme do pico de frente (estrato). Camera fixa: a foto de hoje se compara com a de ontem.
        /// Guarda junto o isolamento do shader: so' o material do TERRENO liga o chao de pedra (arvore e ruina usam o mesmo).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Chao_PicoEEncosta()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Assert.AreEqual(1f, Arkana.World.Ilha.MaterialTerreno().GetFloat("_Chao"), "o terreno liga o chao de pedra (sem shader Arkana, cai aqui)");
            Assert.AreEqual(0f, Arkana.World.Ilha.MaterialToon().GetFloat("_Chao"), "arvore e moita nao viram pedra");
            Assert.AreEqual(0f, Arkana.World.Ilha.MaterialPedra().GetFloat("_Chao"), "ruina nao vira chao");
            var r = ilha.Relevo;
            var sb = new System.Text.StringBuilder();

            // PICO de perto: atras e ao lado do mago, 3,2 m acima do chao, olhando para o centro da ilha — o planalto na
            // frente, a rampa descendo para a grama ao fundo
            Vector3 p = main.Player.Pawn.Pos;
            Vector3 frente = new Vector3(-p.x, 0f, -p.z).normalized, lado = new Vector3(frente.z, 0f, -frente.x);
            Vector3 c = p - frente * 4f + lado * 2.2f;
            c.y = Arkana.World.Ilha.AlturaDoChao(c.x, c.z) + 3.2f;
            Camera cam = CameraTemporaria("CamFotoChaoPico", c, p + frente * 7f, Color.gray);
            Foto(cam, "26-chao-pico", false);
            Object.Destroy(cam.gameObject);
            float hp = r.Altura(p.x, p.z);
            sb.AppendLine("26-chao-pico: pawn=" + p.ToString("F1") + " cam=" + c.ToString("F1") + " bioma=" + r.BiomaEm(p.x, p.z)
                + " alfa(rocha)=" + r.Cor(p.x, p.z, hp).a.ToString("F2") + " cor=" + r.Cor(p.x, p.z, hp).ToString("F2"));

            // ENCOSTA: o ponto mais ingreme do anel do pico com peso de rocha, visto de baixo e de frente para o declive
            Vector3 q = Vector3.zero;
            float ny = 2f;
            for (float rr = 0.3f; rr <= 0.8f; rr += 0.05f)
                for (int k = 0; k < 48; k++)
                {
                    float a = Mathf.PI * 2f * k / 48f;
                    float x = r.Pico.x + Mathf.Cos(a) * r.PicoR * rr, z = r.Pico.y + Mathf.Sin(a) * r.PicoR * rr;
                    float h = r.Altura(x, z), v = r.NormalY(x, z);
                    if (v < ny && r.Cor(x, z, h).a > 0.8f) { ny = v; q = new Vector3(x, h, z); }
                }
            Assert.Less(ny, 0.95f, "o pico tem encosta de rocha de verdade");
            Vector3 nq = r.Normal(q.x, q.z);
            Vector3 desce = new Vector3(nq.x, 0f, nq.z).normalized;   // a normal aponta morro abaixo
            Vector3 e = q + desce * 13f;
            e.y = Mathf.Max(Mathf.Max(Arkana.World.Ilha.AlturaDoChao(e.x, e.z), 0f) + 3f, q.y - 1.5f);
            cam = CameraTemporaria("CamFotoChaoEncosta", e, q + Vector3.up * 1.5f, Color.gray);
            Foto(cam, "26-chao-encosta", false);
            Object.Destroy(cam.gameObject);
            sb.AppendLine("26-chao-encosta: ponto=" + q.ToString("F1") + " ny=" + ny.ToString("F3") + " cam=" + e.ToString("F1"));
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        /// <summary>
        /// ONDA 6A — o MAPA. 30-minimapa-castelo: no castelo o minimapa mostra a ilha INTEIRA com a rota tracejada e a seta no
        /// portao. 30-minimapa: depois do pouso, com a tempestade formada — a janela local em volta do jogador, a borda violeta
        /// da zona, o proximo circulo branco, o bau e a BUSSOLA no topo central. 30-mapa-grande: o toque no minimapa (o
        /// caminho do dedo) abre a ilha inteira com os nomes dos POIs, sem pausar. 30-mapa-grande-fechando: a zona cravada
        /// na fase 1 pelo ForcarCirculo (depuracao da Zona) — a tinta roxa fora dela e os dois circulos no mapa aberto.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Mapa_MinimapaBussolaEMapaGrande()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);   // o castelo emitiu a rota (Start) e a ilha foi pintada na Task (~0,5 s)
            Assert.IsNotNull(main.Player, "partida sem jogador");
            Assert.IsNotNull(main.Hud.Mapa, "a HUD tem mapa");
            Assert.IsTrue(Arkana.UI.Minimapa.IlhaPintada, "a textura da ilha subiu (a Task da pintura terminou)");
            Foto(main.Player.Camera.Cam, "30-minimapa-castelo", true);

            // o roteiro do 06-08: salta com o castelo sobre a ilha e espera o pouso
            float t = 0f;
            while (main.Castelo != null && main.Castelo.Progresso < 0.42f && t < 15f) { yield return null; t += Time.deltaTime; }
            main.Player.Saltar();
            t = 0f;
            while (main.Player != null && main.Player.Pawn.Queda.NoAr && t < 20f) { yield return null; t += Time.deltaTime; }
            // 70 s de abertura + 10 s de formacao: a tempestade existe e o proximo circulo esta' anunciado; o bau caiu aos 51 s
            yield return Esperar(Gameplay.Zona.ABERTURA_S + Gameplay.Zona.FORMACAO_S + 2f);
            OlharParaOCentro(main);
            yield return Esperar(1f);   // o zoom do minimapa ja' mergulhou na janela local e a camera assentou
            Foto(main.Player.Camera.Cam, "30-minimapa", true);
            Diagnostico(main, "30-minimapa");
            var sb = new System.Text.StringBuilder();
            Gameplay.Zona z = main.Partida.Zona;
            Gameplay.BauCelestial bau = main.Partida.Bau;
            Gameplay.Zona.Circulo prox;
            bool temProx = Gameplay.ZonaVisual.Proximo(z, out prox);
            sb.AppendLine("30-minimapa: zona=" + z.EstadoAtual + " fase=" + z.FaseAtual + " centro=" + z.Centro.ToString("F0") + " raio=" + z.Raio.ToString("F0")
                + " proximo=" + (temProx ? prox.Centro.ToString("F0") + " r=" + prox.Raio.ToString("F0") : "-")
                + " bau=" + (bau != null ? bau.FaseAtual + " " + bau.Pos.ToString("F0") : "-")
                + " jogador=" + main.Player.Pawn.Pos.ToString("F0") + " yaw=" + (main.Player.Camera.Logica.Yaw * Mathf.Rad2Deg).ToString("F0"));

            Tocar("Minimapa");   // o caminho do dedo: o Button do minimapa
            Assert.IsTrue(main.Hud.Mapa.Aberto, "tocar no minimapa abre o mapa grande");
            Assert.AreEqual(1f, Time.timeScale, "o mapa grande NAO pausa o jogo");
            yield return null;
            Foto(main.Player.Camera.Cam, "30-mapa-grande", true);

            // a zona cravada na fase 1 (Plano[0]) parada em espera: o proximo passa a ser o Plano[1]
            z.ForcarCirculo(1, z.Plano[0].Centro, z.Plano[0].Raio);
            yield return null;
            Foto(main.Player.Camera.Cam, "30-mapa-grande-fechando", true);
            sb.AppendLine("30-mapa-grande-fechando: zona=" + z.EstadoAtual + " fase=" + z.FaseAtual + " raio=" + z.Raio.ToString("F0")
                + " proximo=" + z.Plano[1].Centro.ToString("F0") + " r=" + z.Plano[1].Raio.ToString("F0") + " aberto=" + main.Hud.Mapa.Aberto);
            Tocar("PlacaDoMapa");   // tocar de novo (no mapa aberto) fecha
            Assert.IsFalse(main.Hud.Mapa.Aberto, "tocar de novo fecha o mapa grande");
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        /// <summary>
        /// AS ARVORES DA MESHY (onda 6B) no lugar do cacho de blobs: a copa mais sozinha da campina a ~15 m (grama, ceu e a
        /// moita da Meshy perto), a borda da mata fechada (pinheiro + copa, a moita da Meshy no chao) e a ilha do alto como a
        /// 04 (a mata do SW toda em LOD1). O jogador e' levado (Aterrar) para perto de cada camera: a grama e o kit cortam pela
        /// camera DELE; o LOD da arvore e a moita, pela camera da foto (Vegetacao.Olho). As tres olham a ~60 graus do sol.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Arvores_CampinaMataAerea()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Arkana.World.Vegetacao veg = ilha.Vegetacao;
            Assert.IsNotNull(veg, "sem vegetacao");
            Arkana.World.Relevo r = ilha.Relevo;
            var sol = Object.FindFirstObjectByType<Arkana.World.Sol>();
            Vector3 l = sol != null ? sol.transform.forward : new Vector3(0.6f, -0.5f, 0.6f);
            l.y = 0f;
            l.Normalize();
            Vector3 v = (l * 0.5f + new Vector3(-l.z, 0f, l.x) * 0.87f).normalized;   // olhar a 60 graus do sol: luz de lado, volume
            int n = veg.ContarArvores(), pinheiros = 0;
            for (int i = 0; i < n; i++) if (veg.EspecieDe(i) == Arkana.World.Vegetacao.Especie.Pinheiro) pinheiros++;
            var sb = new System.Text.StringBuilder("31-arvores: molde=" + veg.MoldeDasArvores + " moita-perto=" + veg.MoldeDasMoitas
                + " arvores=" + n + " pinheiros=" + pinheiros + " copas=" + (n - pinheiros) + " moitas=" + veg.ContarMoitas() + "\n");

            // 1) CAMPINA: a copa em campina com menos vizinhas a 20 m, camera a 15 m em chao seco e no nivel dela
            int melhor = -1, vizMelhor = 0;
            float nota = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (veg.EspecieDe(i) != Arkana.World.Vegetacao.Especie.Copa) continue;
                Vector3 a = veg.PosArvore(i);
                if (r.BiomaEm(a.x, a.z) != Arkana.World.Bioma.Campina) continue;
                Vector3 c = a - v * 15f;
                if (!r.PodePousar(c.x, c.z)) continue;
                int viz = 0;
                for (int j = 0; j < n; j++)
                {
                    Vector3 b = veg.PosArvore(j) - a;
                    b.y = 0f;
                    if (j != i && b.sqrMagnitude < 400f) viz++;
                }
                float s = viz * 10f + Mathf.Abs(r.Altura(c.x, c.z) - a.y);
                if (s < nota) { nota = s; melhor = i; vizMelhor = viz; }
            }
            Assert.GreaterOrEqual(melhor, 0, "nenhuma copa na campina com chao seco a 15 m");
            Vector3 arv = veg.PosArvore(melhor);
            Vector3 olho = arv - v * 15f;
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 1.9f;
            main.Player.Pawn.Aterrar(olho - v * 3f);   // atras da camera: fora do quadro
            yield return Esperar(1.2f);
            Camera cam = CameraTemporaria("CamFotoArvoreCampina", olho, arv + Vector3.up * 3.4f, Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "31-arvores-campina", false);
            sb.AppendLine("  31-arvores-campina: arvore #" + melhor + " em " + arv.ToString("F1") + " vizinhas<20m=" + vizMelhor
                + " cam=" + olho.ToString("F1") + " lotes=" + veg.LotesEnviados + " tris-enviados=" + veg.TrisEnviados + " LOD0=" + veg.ArvoresPerto);
            Object.Destroy(cam.gameObject);

            // 2) MATA: a borda da mata fechada (2 m fora do disco), olhando 25 m para dentro, sem tronco colado na lente
            Vector2 f = r.Floresta;
            float R = r.FlorestaR, melhorDot = -2f;
            Vector3 borda = Vector3.zero, dentro = Vector3.zero;
            for (int k = 0; k < 48; k++)
            {
                float ang = Mathf.PI * 2f * k / 48f;
                var d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 c = new Vector3(f.x, 0f, f.y) + d * (R + 2f);
                if (!r.PodePousar(c.x, c.z) || r.Altura(c.x, c.z) < 1.3f) continue;
                float perto = float.MaxValue;
                for (int j = 0; j < n; j++)
                {
                    Vector3 b = veg.PosArvore(j) - c;
                    b.y = 0f;
                    perto = Mathf.Min(perto, b.magnitude);
                }
                if (perto < 3.5f) continue;
                float dot = Vector3.Dot(-d, v);   // o sol de lado-costas do fotografo
                if (dot > melhorDot) { melhorDot = dot; borda = c; dentro = new Vector3(f.x, 0f, f.y) + d * (R - 25f); }
            }
            Assert.Greater(melhorDot, -2f, "nenhuma borda da mata com chao seco");
            borda.y = Arkana.World.Ilha.AlturaDoChao(borda.x, borda.z) + 1.9f;
            dentro.y = Arkana.World.Ilha.AlturaDoChao(dentro.x, dentro.z) + 3f;
            Vector3 fora = new Vector3(borda.x - f.x, 0f, borda.z - f.y).normalized;
            main.Player.Pawn.Aterrar(borda + fora * 3f);
            yield return Esperar(1.2f);
            cam = CameraTemporaria("CamFotoArvoreMata", borda, dentro, Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "31-arvores-mata", false);
            sb.AppendLine("  31-arvores-mata: cam=" + borda.ToString("F1") + " alvo=" + dentro.ToString("F1") + " sol-de-costas=" + melhorDot.ToString("F2")
                + " lotes=" + veg.LotesEnviados + " tris-enviados=" + veg.TrisEnviados + " LOD0=" + veg.ArvoresPerto);
            Object.Destroy(cam.gameObject);

            // 3) A ILHA DO ALTO, o mesmo quadro da 04: a mata do SW em LOD1 (o custo do castelo e da queda)
            cam = CameraTemporaria("CamFotoArvoreAerea", new Vector3(0f, 420f, -430f), new Vector3(0f, 0f, 20f), Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "31-ilha-aerea", false);
            sb.AppendLine("  31-ilha-aerea: lotes=" + veg.LotesEnviados + " tris-enviados=" + veg.TrisEnviados + " LOD0=" + veg.ArvoresPerto);
            Object.Destroy(cam.gameObject);
            veg.Olho = null;
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.AreNotEqual("procedural", veg.MoldeDasArvores, "as arvores da Meshy nao carregaram (faltam os .glb em Resources?)");
        }


        /// <summary>
        /// ONDA 7C — o VOO como espetaculo. 34-castelo-vivo: camera de LADO, a ~40 m do casco (60 m do eixo: o castelo tem
        /// 35,8 m de envergadura e 52 de altura, mais perto a torre sai do quadro), 10 m abaixo do eixo e mirando 18 m atras
        /// dele, com o FOV do jogador (62): o castelo num terco, a esteira dourada e as brasas cruzando o resto, as runas
        /// girando sob a base (elipse vista de cima) e as rochas em orbita. Depois o roteiro do 06-08
        /// (salta com o castelo sobre a ilha): 34-queda pela camera do jogador em queda livre (vento na camera, rastro fino nas
        /// maos e pes), 34-planeio (as fitas nas maos e o brilho) e 34-pouso no instante do pouso (anel de poeira, onda e
        /// faiscas: 0,12 s depois, o anel ja' abriu ~1,5 m).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Voo_CasteloQuedaPlaneioPouso()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Bus.EmitGameStartRequested();
            yield return Esperar(3f);   // a esteira (2,4 s) e as brasas (2,4-4 s) enchem o rastro
            Assert.IsNotNull(main.Player, "partida sem jogador");
            Assert.IsNotNull(main.Castelo, "partida sem castelo");
            var voo = Object.FindFirstObjectByType<Gameplay.VisualDoVoo>();
            Assert.IsNotNull(voo, "o voo tem quem desenhe");
            Arkana.World.Castelo cas = main.Castelo;
            Vector3 c = cas.transform.position, dir = cas.Rota.Direcao;
            Vector3 lado = Vector3.Cross(Vector3.up, dir).normalized;
            Camera cam = CameraTemporaria("CamFotoCasteloVivo", c + lado * 60f - dir * 4f + Vector3.down * 10f, c - dir * 18f + Vector3.down * 6f, Color.gray);
            cam.fieldOfView = 62f;
            Foto(cam, "34-castelo-vivo", false);
            Object.Destroy(cam.gameObject);
            var sb = new System.Text.StringBuilder("34-castelo-vivo: castelo=" + c.ToString("F0") + " progresso=" + cas.Progresso.ToString("F2")
                + " vivo=" + voo.CasteloVivo + " rastro=" + voo.RastroDoCastelo + " rochas=" + voo.Rochas + " brasas=" + voo.Brasas + "\n");

            float t = 0f;
            while (main.Castelo != null && main.Castelo.Progresso < 0.42f && t < 15f) { yield return null; t += Time.deltaTime; }
            main.Player.Saltar();
            yield return Esperar(2.5f);   // terminal (55 m/s) desde 1,4 s: o vento no maximo
            Gameplay.Pawn eu = main.Player.Pawn;
            Foto(main.Player.Camera.Cam, "34-queda", true);
            sb.AppendLine("34-queda: fase=" + eu.Queda.Fase + " vy=" + eu.Queda.Vy.ToString("F1") + " altura=" + eu.Queda.Altura.ToString("F0")
                + " vento=" + voo.Vento + " rastro=" + voo.RastroAceso(eu));

            t = 0f;
            while (eu.Queda.NoAr && eu.Queda.Fase != Gameplay.Queda.PLANANDO && t < 15f) { yield return null; t += Time.deltaTime; }
            yield return Esperar(0.7f);   // as fitas crescem (0,9 s de vida) e o brilho sai das maos
            Foto(main.Player.Camera.Cam, "34-planeio", true);
            sb.AppendLine("34-planeio: fase=" + eu.Queda.Fase + " vy=" + eu.Queda.Vy.ToString("F1") + " vento=" + voo.Vento + " rastro=" + voo.RastroAceso(eu));

            t = 0f;
            while (eu.Queda.NoAr && t < 20f) { yield return null; t += Time.deltaTime; }
            yield return Esperar(0.12f);
            Foto(main.Player.Camera.Cam, "34-pouso", true);
            sb.AppendLine("34-pouso: pousos=" + voo.Pousos + " ultimo=" + voo.UltimoPouso.ToString("F1") + " pawn=" + eu.Pos.ToString("F1"));
            Diagnostico(main, "34-pouso");
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.Greater(voo.Pousos, 0, "o pouso estalou");
        }

        // ONDA 7A — colar em FotoTests.cs, depois de Foto_Chao_PicoEEncosta. Rodar: .\foto.ps1 "Foto_Agua"

        /// <summary>
        /// ONDA 7A — a AGUA de longe e de perto. 32-lago-alto: 200 m acima da lamina, olhando para o sul (para o sol), o lago
        /// a' esquerda e o ALAGADO a' direita — a agua listrada das fotos 06/07 era o alagado: o xadrez era a bruma (seno x seno
        /// sem LOD) e o miolo azul de borda dura era o MAR desenhado por cima da poca. 32-lago-perto: a camera de 3a pessoa (a
        /// altura e o pitch do jogo) a 5 m da margem do lago, de frente para o sol: onda, espuma de margem e o caminho de
        /// faiscas. Camera fixa: a foto de hoje se compara com a de ontem.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Agua_LagoAltoEPerto()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            var r = ilha.Relevo;
            var sol = Object.FindFirstObjectByType<Arkana.World.Sol>();
            Assert.IsNotNull(sol, "sem sol");
            var sb = new System.Text.StringBuilder();

            // o MAR nao desenha sob o lago e o alagado (senao a ordem dos transparentes o poe por cima, vista do alto)
            GameObject mar = GameObject.Find("Ilha/Gerado/Mar");
            Assert.IsNotNull(mar, "sem mar");
            Material mm = mar.GetComponent<Renderer>().sharedMaterial;
            if (mm.shader.name == "Arkana/Agua")
            {
                Vector4 s0 = mm.GetVector("_SemMar0"), s1 = mm.GetVector("_SemMar1");
                Assert.AreEqual(r.LagoDiscoR, s0.z, 0.01f, "o mar abre o disco do lago");
                Assert.AreEqual(r.AlagadoDiscoR, s1.z, 0.01f, "o mar abre o disco do alagado");
                sb.AppendLine("32-agua: mar=" + mm.name + " semMar0=" + s0.ToString("F1") + " semMar1=" + s1.ToString("F1")
                    + " reflexo=" + mm.GetFloat("_Reflexo").ToString("F2") + " brilho=" + mm.GetFloat("_Brilho").ToString("F2"));
            }
            else sb.AppendLine("32-agua: SEM Arkana/Agua (cadeia antiga): " + mm.shader.name);

            // 1) DO ALTO: a 200 m, os dois POIs d'agua no quadro (o lago a' esquerda, o alagado a' direita)
            Vector2 meio = (r.Lago + r.Alagado) * 0.5f;
            Vector3 c = new Vector3(meio.x, Arkana.World.Relevo.LagoY + 200f, meio.y + 240f);
            const float pitchAlto = 27f * Mathf.Deg2Rad;
            Camera cam = CameraTemporaria("CamFotoLagoAlto", c, c + new Vector3(0f, -Mathf.Sin(pitchAlto), -Mathf.Cos(pitchAlto)) * 100f, Color.gray);
            Foto(cam, "32-lago-alto", false);
            cam.aspect = (float)L / A;   // a do PNG (fora da foto a camera volta ao aspecto da janela)
            Vector3 pl = cam.WorldToViewportPoint(new Vector3(r.Lago.x, Arkana.World.Relevo.LagoY, r.Lago.y));
            Vector3 pa = cam.WorldToViewportPoint(new Vector3(r.Alagado.x, Arkana.World.Relevo.AlagadoY, r.Alagado.y));
            sb.AppendLine("32-lago-alto: cam=" + c.ToString("F1") + " lago no png=(" + (pl.x * L).ToString("F0") + ", " + ((1f - pl.y) * A).ToString("F0") + ") a " + pl.z.ToString("F0")
                + " m, alagado=(" + (pa.x * L).ToString("F0") + ", " + ((1f - pa.y) * A).ToString("F0") + ") a " + pa.z.ToString("F0") + " m");
            Object.Destroy(cam.gameObject);

            // 2) DE PERTO: de costas para a sombra (= de frente para o sol), 5 m alem da margem REAL do lago nessa direcao
            Vector3 l = sol.transform.forward; l.y = 0f; l.Normalize();
            Vector2 d = new Vector2(l.x, l.z);
            float margem = 0f;
            while (margem < r.LagoDiscoR && r.Altura(r.Lago.x + d.x * margem, r.Lago.y + d.y * margem) < Arkana.World.Relevo.LagoY) margem += 0.25f;
            Vector2 pe = r.Lago + d * (margem + 5f);
            c = new Vector3(pe.x, r.Altura(pe.x, pe.y) + 3.6f, pe.y);   // pivo 1,85 + ombro + braco 4,15 a 17 graus
            float pitch = CameraLogica_PitchPadrao();
            Vector3 dir = new Vector3(-d.x * Mathf.Cos(pitch), -Mathf.Sin(pitch), -d.y * Mathf.Cos(pitch));
            cam = CameraTemporaria("CamFotoLagoPerto", c, c + dir * 10f, Color.gray);
            Foto(cam, "32-lago-perto", false);
            // o que houver entre a camera e o ponto da lamina no centro do quadro (a agua nao tem colisor)
            Vector3 naAgua = c + dir * ((c.y - Arkana.World.Relevo.LagoY) / Mathf.Sin(pitch));
            bool tapa = Physics.Linecast(c, naAgua, out RaycastHit h);
            sb.AppendLine("32-lago-perto: margem a " + margem.ToString("F1") + " m do centro, cam=" + c.ToString("F1") + " pitch=" + (pitch * Mathf.Rad2Deg).ToString("F0")
                + " centro do quadro na lamina a " + Vector3.Distance(c, naAgua).ToString("F1") + " m, no caminho: " + (tapa ? Caminho(h.collider.transform) + " a " + h.distance.ToString("F1") + " m" : "nada"));
            Object.Destroy(cam.gameObject);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        /// <summary>
        /// Leva o CORPO do jogador a (x, z) com o CharacterController desligado no meio. Existe porque o Pawn.Aterrar, depois do
        /// pouso, devolve o corpo para Queda.Pos (a Queda ja' pousou e ignora Posicionar). A grama e o kit cortam pela camera
        /// DELE: sem o corpo perto, a foto de outro canto da ilha sai sem tufo nenhum.
        /// </summary>
        static void LevarJogador(Main main, Vector3 p)
        {
            var cc = main.Player.Pawn.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            main.Player.Pawn.transform.position = new Vector3(p.x, Arkana.World.Ilha.AlturaDoChao(p.x, p.z) + 0.05f, p.z);
            if (cc != null) cc.enabled = true;
        }

        /// <summary>
        /// A CAMPINA VIVA (onda 7B): o _Chao do terreno saiu do pico para o resto da ilha. 33-campina: a camera do JOGADOR, baixa,
        /// no nascimento de grama pura mais perto do centro (manchas fria/quente, trecho de terra batida, trilha, trevo e flor).
        /// 33-mata-borda: o chao entrando na mata (lingua de musgo e humus, folha caida). 33-praia: a costa sul, onde a duna desce
        /// ao mar (areia molhada nitida, fio de sal, duna clara). 33-ilha-aerea: o quadro da 04. Treino: deterministico.
        /// Guarda junto o contrato: a malha do terreno leva a composicao do solo no UV0 e so' o material do terreno liga o _Chao.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Chao_CampinaMataPraiaAerea()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Arkana.World.Relevo r = ilha.Relevo;
            Assert.AreEqual(1f, Arkana.World.Ilha.MaterialTerreno().GetFloat("_Chao"), "o terreno liga o chao vivo (sem shader Arkana, cai aqui)");
            Assert.AreEqual(0f, Arkana.World.Ilha.MaterialToon().GetFloat("_Chao"), "arvore e moita nao viram chao");
            Mesh malha = ilha.MalhaDoTerreno;
            var uv = new System.Collections.Generic.List<Vector4>();
            malha.GetUVs(0, uv);
            Assert.AreEqual(malha.vertexCount, uv.Count, "a malha do terreno leva a composicao do solo no UV0");
            int meio = malha.vertexCount / 2;
            Vector3 vm = malha.vertices[meio];
            Color sm = r.Solo(vm.x, vm.z, vm.y);
            Assert.AreEqual(sm.a, uv[meio].w, 1e-4f, "o UV0 e' o Relevo.Solo do vertice (grama)");
            Assert.AreEqual(sm.b, uv[meio].z, 1e-4f, "o UV0 e' o Relevo.Solo do vertice (pisado)");
            var sb = new System.Text.StringBuilder();
            Arkana.World.Vegetacao veg = ilha.Vegetacao;

            // 1) CAMPINA: o nascimento de grama pura mais perto do centro, a camera do jogador baixa olhando para o miolo da ilha
            Vector3 nasc = Vector3.zero;
            float melhor = float.MaxValue;
            foreach (Vector3 n in r.Nascimentos)
            {
                if (r.BiomaEm(n.x, n.z) != Arkana.World.Bioma.Campina || r.Solo(n.x, n.z, r.Altura(n.x, n.z)).a < 0.95f) continue;
                float d = new Vector2(n.x, n.z).magnitude;
                if (d < melhor) { melhor = d; nasc = n; }
            }
            Assert.Less(melhor, float.MaxValue, "nenhum nascimento em campina pura");
            LevarJogador(main, nasc);
            OlharParaOCentro(main);
            main.Player.Camera.Logica.Pitch = 0.2f;   // mais rasante que o padrao (0,30): a mancha recua ate' o pico
            yield return Esperar(1.5f);               // a camera assenta e a grama reclassifica (4 Hz) em volta dele
            Foto(main.Player.Camera.Cam, "33-campina", false);
            Diagnostico(main, "33-campina");
            Color s = r.Solo(nasc.x, nasc.z, r.Altura(nasc.x, nasc.z));
            sb.AppendLine("33-campina: nascimento=" + nasc.ToString("F1") + " solo(mata,areia,pisado,grama)=" + s.ToString("F2"));

            // 2) MATA: a borda (metade da regiao da mata, ~57 m do centro), do lado mais livre de tronco; camera 4 m acima do chao,
            //    fora da borda, olhando o chao que entra na mata. O corpo fica 4 m atras da camera, fora do quadro.
            Vector2 f = r.Floresta;
            Vector3 borda = Vector3.zero, fora = Vector3.zero;
            float folga = -1f;
            for (int k = 0; k < 48; k++)
            {
                float ang = Mathf.PI * 2f * k / 48f;
                var d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 b = new Vector3(f.x, 0f, f.y) + d * 57f, c = b + d * 7f;
                if (!r.PodePousar(c.x, c.z) || !r.PodePousar(b.x, b.z)) continue;
                float livre = 99f;
                if (veg != null)
                    for (int j = 0; j < veg.ContarArvores(); j++)
                    {
                        Vector3 t = veg.PosArvore(j) - c;
                        t.y = 0f;
                        livre = Mathf.Min(livre, t.magnitude);
                    }
                if (livre > folga) { folga = livre; borda = b; fora = d; }
            }
            Assert.Greater(folga, 0f, "nenhuma borda da mata em chao seco");
            Vector3 olho = borda + fora * 7f;
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 4f;
            Vector3 alvo = borda - fora * 6f;
            alvo.y = Arkana.World.Ilha.AlturaDoChao(alvo.x, alvo.z);
            LevarJogador(main, olho + fora * 4f);
            yield return Esperar(1.2f);
            Camera cam = CameraTemporaria("CamFotoMataBorda", olho, alvo, Color.gray);
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "33-mata-borda", false);
            Object.Destroy(cam.gameObject);
            sb.AppendLine("33-mata-borda: borda=" + borda.ToString("F1") + " cam=" + olho.ToString("F1") + " tronco-mais-perto=" + folga.ToString("F1")
                + " solo-na-borda=" + r.Solo(borda.x, borda.z, r.Altura(borda.x, borda.z)).ToString("F2"));

            // 3) PRAIA: a costa sul, onde as dunas descem ao mar. Camera de pe' na areia seca (1,4 m de cota), 16 graus a oeste do
            //    eixo das dunas, olhando a beira d'agua 4 graus a leste dele: mar a direita, areia molhada, duna a esquerda.
            float eixo = Mathf.Atan2(r.Dunas.y, r.Dunas.x);
            Vector3 PontoDaCosta(float ang, float cota)
            {
                var d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                float m = r.Dunas.magnitude;
                while (m < r.RaioTerra + 60f && r.Altura(d.x * m, d.z * m) > cota) m += 0.5f;
                return new Vector3(d.x * m, cota, d.z * m);
            }
            olho = PontoDaCosta(eixo - 16f * Mathf.Deg2Rad, 1.4f);
            alvo = PontoDaCosta(eixo + 4f * Mathf.Deg2Rad, 0.3f);
            Vector3 praTras = (olho - alvo);
            praTras.y = 0f;
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 1.9f;
            LevarJogador(main, olho + praTras.normalized * 4f);
            yield return Esperar(1.2f);
            cam = CameraTemporaria("CamFotoPraia", olho, alvo, Color.gray);
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "33-praia", false);
            Object.Destroy(cam.gameObject);
            sb.AppendLine("33-praia: cam=" + olho.ToString("F1") + " alvo=" + alvo.ToString("F1")
                + " solo-na-agua=" + r.Solo(alvo.x, alvo.z, r.Altura(alvo.x, alvo.z)).ToString("F2"));

            // 4) A ILHA DO ALTO, o quadro da 04
            cam = CameraTemporaria("CamFotoChaoAerea", new Vector3(0f, 420f, -430f), new Vector3(0f, 0f, 20f), Color.gray);
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "33-ilha-aerea", false);
            Object.Destroy(cam.gameObject);
            if (veg != null) veg.Olho = null;
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }


        /// <summary>
        /// O ALVO LE' DE LONGE (onda 8A), no TREINO: uma rajada de tres fogos de verdade no boneco com o escudo em 12/50 — o
        /// primeiro morde o escudo, o segundo o estoura e transborda, o terceiro entra na vida. 35-alvo: a marca em cima dele
        /// (nome, escudo vazio com o fantasma do que tinha, vida com o RASTRO claro do golpe). 35-alvo-dois: o outro boneco
        /// longe (~22 m), SOB A MIRA e sem acerto: duas marcas, a de longe menor e com o fio no ouro vivo.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Combate_Alvo()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Gameplay.Pawn eu = main.Player.Pawn;
            var alvo = (Gameplay.Pawn)main.Partida.Bonecos[0];
            var longe = (Gameplay.Pawn)main.Partida.Bonecos[1];
            Vector3 d = alvo.Pos - eu.Pos; d.y = 0f;
            Vector3 dir = d.normalized, lado = new Vector3(dir.z, 0f, -dir.x);
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(d.x, d.z);
            main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
            longe.Aterrar(eu.Pos + dir * 22f + lado * 4f);   // o segundo nasce 3 m atras do primeiro: vai para longe, a direita
            alvo.Vital.Escudo = 12f;
            yield return Esperar(1.5f);   // a camera assenta e o rastro do escudo mexido a mao desce inteiro (0,35 + 0,76/0,8 s)

            // a RAJADA: Projetil -> Combat -> DamageApplied, 0,12 s entre os tiros (golpe seguido segura o rastro inteiro)
            for (int i = 0; i < 3; i++)
            {
                main.Partida.Registrar(Gameplay.Projetil.Lancar(eu, alvo.Pos + Vector3.up * 1.1f - dir * 1.5f, dir, Elemento.Fogo));
                yield return Esperar(0.12f);
            }
            float t = 0f;
            while (main.Partida.Projeteis.Count > 0 && t < 2f) { yield return null; t += Time.deltaTime; }
            yield return null;
            Foto(main.Player.Camera.Cam, "35-alvo", true);
            Arkana.UI.MarcasLogica marcas = main.Hud.Marcas.Logica;
            var sb = new System.Text.StringBuilder();
            Arkana.UI.MarcasLogica.Marca doAlvo = null;
            foreach (Arkana.UI.MarcasLogica.Marca m in marcas.Visiveis)
            {
                if (m.Alvo == (IEntidade)alvo) doAlvo = m;
                sb.AppendLine("35-alvo: " + m.Alvo.Nome + " dist=" + m.Dist.ToString("F1") + " mira=" + m.NaMira + " alfa=" + m.Alfa.ToString("F2")
                    + " vida=" + m.Vida.Frac.ToString("F2") + "/" + m.Vida.Fantasma.ToString("F2")
                    + " escudo=" + m.Escudo.Frac.ToString("F2") + "/" + m.Escudo.Fantasma.ToString("F2") + " nivel=" + m.Nivel);
            }
            float rastroVida = doAlvo != null ? doAlvo.Vida.Fantasma - doAlvo.Vida.Frac : 0f;
            float rastroEscudo = doAlvo != null ? doAlvo.Escudo.Fantasma - doAlvo.Escudo.Frac : 0f;

            // 35-alvo-dois: a mira no de longe, sem tiro. O raio da camera passa pelo OMBRO (CameraLogica.Posicionar): mirar
            // do ombro ao meio do corpo poe o corpo sob a mira. O ombro depende do yaw: duas passadas convergem.
            var cl = main.Player.Camera.Logica;
            Vector3 meio = longe.Pos + Vector3.up * 0.9f;
            for (int i = 0; i < 2; i++)
            {
                Vector3 ombro = eu.Pos + Vector3.up * (Gameplay.CameraLogica.ALTURA_PIVO + Gameplay.CameraLogica.OMBRO_Y) + cl.Direita * Gameplay.CameraLogica.OMBRO_X;
                Vector3 v = meio - ombro;
                cl.Yaw = Mathf.Atan2(v.x, v.z);
                cl.Pitch = Mathf.Atan2(-v.y, new Vector2(v.x, v.z).magnitude);
            }
            yield return Esperar(0.3f);
            Foto(main.Player.Camera.Cam, "35-alvo-dois", true);
            bool longeNaMira = false;
            foreach (Arkana.UI.MarcasLogica.Marca m in marcas.Visiveis)
            {
                if (m.Alvo == (IEntidade)longe) longeNaMira = m.NaMira;
                sb.AppendLine("35-alvo-dois: " + m.Alvo.Nome + " dist=" + m.Dist.ToString("F1") + " mira=" + m.NaMira + " alfa=" + m.Alfa.ToString("F2")
                    + " vida=" + m.Vida.Frac.ToString("F2") + "/" + m.Vida.Fantasma.ToString("F2"));
            }
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());

            Assert.IsNotNull(doAlvo, "o boneco acertado tem marca");
            Assert.Greater(rastroVida, 0.05f, "a vida mostra o rastro do golpe");
            Assert.Greater(rastroEscudo, 0.05f, "o escudo estourado mostra o fantasma do que tinha");
            Assert.AreEqual(2, marcas.Visiveis.Count, "duas marcas: a do acerto e a da mira");
            Assert.AreSame(alvo, marcas.Visiveis[0].Alvo, "a mais perto primeiro");
            Assert.IsTrue(longeNaMira, "o de longe esta' sob a mira (sem acerto)");
        }

        // ---- COLAR em FotoTests.cs logo depois de Foto_Elenco_Escolha (antes do `static void Tocar`). Onda 9C.

        /// <summary>A ESCOLHA DE MAGO com cara de jogo (onda 9C): cartao rico, CONFIRMAR dourado, disco de luz no pe' do
        /// escolhido (38-elenco); depois a aba RAIO ligada e a Tessa escolhida (38-elenco-filtro); o CONFIRMAR sai com ela.</summary>
        [UnityTest]
        public IEnumerator Foto_Elenco_Cartao()
        {
            ExigirGpu();
            string antes = PlayerPrefs.GetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, "");
            try
            {
                Main main = _go.AddComponent<Main>();
                yield return null;
                Tocar("TapTitulo");
                yield return null;
                Tocar("BtnElenco");
                yield return null;
                Tocar("Card01-pyra/Toque");
                yield return null;
                var mago = Object.FindFirstObjectByType<Arkana.Characters.Mago>();
                Assert.IsNotNull(mago, "a vitrine tem mago");
                Assert.AreEqual("Mago 01-pyra", mago.name, "o toque no retrato troca o mago da vitrine na hora");
                Assert.IsNotNull(GameObject.Find("DiscoDoEscolhido"), "o escolhido tem o disco de luz no pe'");
                yield return Esperar(1.2f);   // a camera desliza para o vao entre o painel e o cartao
                Foto(main.CameraDoMenu, "38-elenco", true);

                Tocar("AbaRaio");
                yield return null;
                Assert.IsNull(GameObject.Find("Card01-pyra"), "a aba RAIO esconde quem nao e' de raio");
                Tocar("Card10-tessa/Toque");
                yield return Esperar(1.2f);
                Foto(main.CameraDoMenu, "38-elenco-filtro", true);

                Tocar("BtnConfirmar");
                yield return null;
                Assert.AreEqual("10-tessa", Arkana.Menu.SelecaoPersonagem.MagoEscolhido, "o CONFIRMAR sai com o escolhido");
                Assert.IsNull(GameObject.Find("Selecao"), "e volta ao menu");
            }
            finally
            {
                PlayerPrefs.SetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, antes);
            }
        }

        // ---- onda 9A: colar dentro da classe FotoTests (antes do "}" final da classe). Rodar: .\foto.ps1 "Foto_Mago_Contorno"

        /// <summary>
        /// O CONTORNO DE LUZ (onda 9A) no TREINO, com a camera do jogador olhando PARA o sol: o lado do mago que ela ve' e' o
        /// escuro — a silhueta das fotos 03 e 11. 36-mago-contorno: o jogador a 4 m (contorno discreto) e os dois bonecos a
        /// ~12 e ~25 m (contorno cheio: tem de ler no celular). 36-mago-contorno-perto: o boneco 1 de frente, com o sol atras.
        /// 36-elenco: cinco magos lado a lado — os mais escuros do elenco e a Vitalis (roupa branca: o contorno nao pode
        /// estourar no bloom). Os "-antes" sao o MESMO quadro com o URP Lit que o import fazia (troca por nome de propriedade).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Mago_Contorno()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var sol = Object.FindFirstObjectByType<Arkana.World.Sol>();
            Assert.IsNotNull(sol, "sem sol");
            Vector3 v = -sol.transform.forward; v.y = 0f; v.Normalize();   // para onde o SOL esta': a camera olha para la'
            Vector3 lado = new Vector3(v.z, 0f, -v.x);

            Gameplay.Pawn eu = main.Player.Pawn;
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(v.x, v.z);
            main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
            eu.EncararDir = v;   // de costas para a camera, de frente para o sol
            var b1 = (Gameplay.Pawn)main.Partida.Bonecos[0];
            var b2 = (Gameplay.Pawn)main.Partida.Bonecos[1];
            b1.Aterrar(eu.Pos + v * 12f - lado * 3.5f);
            b2.Aterrar(eu.Pos + v * 25f + lado * 5f);
            b1.EncararDir = -v;  // de frente para a camera: o lado escuro e' o que se ve'
            b2.EncararDir = -v;
            yield return Esperar(1.5f);   // a camera assenta no yaw novo e os corpos giram
            Foto(main.Player.Camera.Cam, "36-mago-contorno", true);
            Diagnostico(main, "36-mago-contorno");

            Vector3 p1 = b1.Pos;
            Camera perto = CameraTemporaria("CamFotoContornoPerto", p1 - v * 3.2f + lado * 1.1f + Vector3.up * 1.5f, p1 + Vector3.up * 1.0f, Color.gray);
            Foto(perto, "36-mago-contorno-perto", false);

            // o ELENCO: cinco a ~6,5 m, de frente para a camera, com o sol atras deles
            string[] cinco = { "04-corvus", "12-umbra", "07-vitalis", "19-noctus", "02-ceifadora" };
            Vector3 c = eu.Pos + lado * 14f;
            var magos = new System.Collections.Generic.List<GameObject>();
            var sb = new System.Text.StringBuilder("36-mago-contorno: sol=" + v.ToString("F2") + " b1=" + b1.Pos.ToString("F1") + " (" + Vector3.Distance(b1.Pos, eu.Pos).ToString("F0")
                + " m) b2=" + b2.Pos.ToString("F1") + " (" + Vector3.Distance(b2.Pos, eu.Pos).ToString("F0") + " m)\n");
            for (int i = 0; i < cinco.Length; i++)
            {
                if (Resources.Load<GameObject>("magos/" + cinco[i]) == null) continue;
                Vector3 p = c + lado * ((i - 2) * 1.5f);
                p.y = Arkana.World.Ilha.AlturaDoChao(p.x, p.z);
                var m = Arkana.Characters.Mago.Criar(null, cinco[i]);
                m.transform.position = p;
                m.transform.rotation = Quaternion.LookRotation(-v);
                magos.Add(m.gameObject);
            }
            yield return Esperar(0.6f);
            Camera elenco = CameraTemporaria("CamFotoContornoElenco", c - v * 6.5f + Vector3.up * 1.6f, c + Vector3.up * 0.9f, Color.gray);
            Foto(elenco, "36-elenco", false);
            foreach (var mg in Object.FindObjectsByType<Arkana.Characters.Mago>(FindObjectsSortMode.None))
            {
                Renderer r = mg.GetComponentInChildren<Renderer>();
                sb.AppendLine("  " + mg.name + " fonte=" + mg.Fonte + " shader=" + (r != null && r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-")
                    + " dist-camera-jogador=" + Vector3.Distance(mg.transform.position, main.Player.Camera.Cam.transform.position).ToString("F1"));
            }

            // ANTES: os mesmos quadros com o URP Lit do import (o que o jogo tinha ate' a onda 9A)
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            int voltaram = 0;
            foreach (var mg in Object.FindObjectsByType<Arkana.Characters.Mago>(FindObjectsSortMode.None))
                foreach (Renderer r in mg.GetComponentsInChildren<Renderer>())
                    foreach (Material mt in r.sharedMaterials)
                        if (lit != null && mt != null && mt.shader.name == "Arkana/Mago" && mt.GetTexture("_BaseMap") != null)
                        {
                            mt.shader = lit;
                            mt.EnableKeyword("_NORMALMAP");   // como o ImportacaoArkana grava
                            mt.EnableKeyword("_EMISSION");
                            voltaram++;
                        }
            sb.AppendLine("  antes: " + voltaram + " materiais de volta ao URP Lit");
            yield return Esperar(1f);   // o editor compila a variante do Lit (sem isto o quadro pode sair no ciano de espera)
            Foto(main.Player.Camera.Cam, "36-mago-contorno-antes", true);
            Foto(perto, "36-mago-contorno-perto-antes", false);
            Foto(elenco, "36-elenco-antes", false);
            Object.Destroy(perto.gameObject);
            Object.Destroy(elenco.gameObject);
            foreach (GameObject g in magos) Object.Destroy(g);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.Greater(voltaram, 0, "nenhum material do mago estava no Arkana/Mago");
        }

        /// <summary>
        /// A PRACA DAS RUINAS (onda 9B): o plato das ruinas deixa de ser o disco cinza liso. 37-ruinas-chao: a camera do JOGADOR no
        /// piso, a 0,45 do raio, olhando para o arco da Meshy do anel (lajes, junta, musgo entrando, laje que falta, as colunas de
        /// Ruinas.cs e a borda em lingua ao fundo). 37-ruinas-alto: ~150 m sobre as ruinas, inclinada (o medalhao no meio, a borda
        /// roida pela campina). Treino: deterministico. Guarda junto o contrato: o terreno leva o _Ruinas do Relevo, a ruina nao.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Chao_Ruinas()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Arkana.World.Relevo r = ilha.Relevo;
            Vector4 praca = Arkana.World.Ilha.MaterialTerreno().GetVector("_Ruinas");
            Assert.AreEqual(r.RuinasR, praca.z, 1e-3f, "o terreno leva o _Ruinas do Relevo (sem ele, nada de praca)");
            Assert.AreEqual(0f, Arkana.World.Ilha.MaterialPedra().GetVector("_Ruinas").z, 1e-6f, "a pedra lavrada (colunas, muralha) nao vira praca");
            var sb = new System.Text.StringBuilder();

            // 1) NO PISO: o arco partido da Meshy (o primeiro do anel); sem kit, o noroeste (o lado do miolo da ilha)
            Vector2 c = r.Ruinas;
            Vector2 dir = new Vector2(-0.62f, 0.78f).normalized;
            Vector3 arco = Vector3.zero;
            if (ilha.Kit != null)
                foreach (Transform t in ilha.Kit.GetComponentsInChildren<Transform>(true))
                    if (t.name == "22-arco-partido") { arco = t.position; dir = (new Vector2(arco.x, arco.z) - c).normalized; break; }
            Vector2 p2 = c + dir * r.RuinasR * 0.45f;
            LevarJogador(main, new Vector3(p2.x, 0f, p2.y));
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(dir.x, dir.y);
            main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
            yield return Esperar(1.5f);   // a camera assenta; grama e kit reclassificam (4 Hz) em volta dele
            Foto(main.Player.Camera.Cam, "37-ruinas-chao", false);
            Diagnostico(main, "37-ruinas-chao");
            Vector3 pe = main.Player.Pawn.Pos;
            sb.AppendLine("37-ruinas-chao: pawn=" + pe.ToString("F1") + " arco=" + arco.ToString("F1") + " raio=" + (new Vector2(pe.x, pe.z) - c).magnitude.ToString("F1")
                + "/" + r.RuinasR.ToString("F0") + " bioma=" + r.BiomaEm(pe.x, pe.z) + " solo=" + r.Solo(pe.x, pe.z, r.Altura(pe.x, pe.z)).ToString("F2")
                + " _Ruinas=" + praca.ToString("F2"));

            // 2) DO ALTO: 150 m acima do plato, 60 m ao sul, olhando o centro (~68 graus para baixo). O corte do kit e da grama mede o
            //    jogador (no plato): liga o kit inteiro e da' o olho da vegetacao para esta camera.
            float hc = Arkana.World.Ilha.AlturaDoChao(c.x, c.y);
            Camera cam = CameraTemporaria("CamFotoRuinasAlto", new Vector3(c.x, hc + 150f, c.y - 60f), new Vector3(c.x, hc, c.y), Color.gray);
            if (ilha.Kit != null) foreach (Renderer rr in ilha.Kit.GetComponentsInChildren<Renderer>(true)) rr.enabled = true;
            Arkana.World.Vegetacao veg = ilha.Vegetacao;
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "37-ruinas-alto", false);
            Object.Destroy(cam.gameObject);
            if (veg != null) veg.Olho = null;
            sb.AppendLine("37-ruinas-alto: cam=" + new Vector3(c.x, hc + 150f, c.y - 60f).ToString("F1") + " centro=" + new Vector3(c.x, hc, c.y).ToString("F1"));
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }


        /// <summary>
        /// Os kits do GRUPO A (Ceifadora, Corvus, Corvomante, Olho-de-Eter) em acao no treino, pelo roteiro da Veu/Tessa: tatica
        /// e suprema pela camera do jogador. O boneco 1 ANDA em circulo (Pawn cru obedece o Stick): o Corvus so' fareja e so'
        /// acende quem se mexe. A mira vai no boneco (a mao agarra, o corvo passa por cima, o enxame o atravessa); a Travessia
        /// vai para o centro da ilha e a foto OLHA PARA TRAS (o rasgo fica atras dela). Cada quadro tem o seu diag.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Kit_GrupoA([Values("02-ceifadora", "04-corvus", "05-corvomante", "06-olho-de-eter")] string slug)
        {
            ExigirGpu();
            string antes = PlayerPrefs.GetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, "");
            try
            {
                Arkana.Menu.SelecaoPersonagem.MagoEscolhido = slug;
                Main main = _go.AddComponent<Main>();
                yield return null;
                Arkana.Menu.Menu.PedidoDeTreino = true;
                Bus.EmitGameStartRequested();
                yield return Esperar(0.5f);
                Assert.IsNotNull(main.Player, "treino sem jogador");
                Gameplay.Pawn eu = main.Player.Pawn;
                Gameplay.Pawn boneco = main.Partida.Bonecos.Count > 0 ? main.Partida.Bonecos[0] as Gameplay.Pawn : null;
                Assert.IsNotNull(boneco, "treino sem boneco");
                yield return GrupoAAndando(boneco, Gameplay.Partida.SUPREMA_TREINO_S + 1f);   // a suprema enche em 5 s no treino

                GrupoAMirar(main, boneco.Pos);
                yield return GrupoAAndando(boneco, 0.3f);
                main.Player.Tatica();
                // o quadro de cada tatica: o uivo aos 0,45 s (o anel ainda aceso), o enxame depois do atraso de 1,4 s
                float tTatica = slug == "04-corvus" ? 0.45f : slug == "06-olho-de-eter" ? 1.9f : 0.9f;
                yield return GrupoAAndando(boneco, tTatica);
                Foto(main.Player.Camera.Cam, "39-kit-" + slug + "-tatica", true);
                GrupoADiag(main, "39-kit-" + slug + "-tatica");

                // o Voo do Olho deixa o corpo sem conjurar 4 s: espera a suprema ficar PRONTA de verdade
                float t = 0f;
                while (!eu.Runner.ProntoSuprema && t < 8f) { boneco.Stick = GrupoAStick(Time.time); yield return null; t += Time.deltaTime; }
                yield return GrupoAAndando(boneco, 0.3f);
                if (slug == "02-ceifadora") OlharParaOCentro(main); else GrupoAMirar(main, boneco.Pos);
                yield return GrupoAAndando(boneco, 0.3f);
                string carga = "carga=" + eu.Runner.CargaSuprema.ToString("F2") + " pronto=" + eu.Runner.ProntoSuprema;
                main.Player.Suprema();
                // a suprema e' TELEGRAFADA (2-2,5 s): o quadro sai logo depois do efeito
                float tSuprema = slug == "05-corvomante" ? 2.8f : slug == "04-corvus" ? 2.4f : 2.3f;
                yield return GrupoAAndando(boneco, tSuprema);
                if (slug == "02-ceifadora")
                {
                    var ceifa = eu.Runner.Impl as Gameplay.Ceifadora;
                    if (ceifa != null && ceifa.RasgoAberto != null) GrupoAMirar(main, ceifa.RasgoAberto.A);   // olha o rasgo
                    yield return GrupoAAndando(boneco, 0.35f);
                }
                Foto(main.Player.Camera.Cam, "39-kit-" + slug + "-suprema", true);
                GrupoADiag(main, "39-kit-" + slug + "-suprema " + carga);
            }
            finally
            {
                PlayerPrefs.SetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, antes);
            }
        }

        /// <summary>Circulo de ~4 m (0,5 de stick, 1 rad/s): o boneco se MEXE sem sair do quadro.</summary>
        static Vector2 GrupoAStick(float t) => new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * 0.5f;

        static IEnumerator GrupoAAndando(Gameplay.Pawn boneco, float segundos)
        {
            float t = 0f;
            int frames = 0, teto = Mathf.CeilToInt(segundos * 400f) + 100;
            while (t < segundos && frames < teto)
            {
                if (boneco != null) boneco.Stick = GrupoAStick(Time.time);
                yield return null;
                t += Time.deltaTime;
                frames++;
            }
        }

        /// <summary>Vira a camera do jogador para `alvo` (o kit sai na MIRA: a camera e' a mira).</summary>
        static void GrupoAMirar(Main main, Vector3 alvo)
        {
            Vector3 d = alvo - main.Player.Pawn.Pos;
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(d.x, d.z);
            main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
        }

        /// <summary>O que o kit escreveu (Visuais) e o que a casca desenhou (renderers do VisualDosKits) no quadro.</summary>
        static void GrupoADiag(Main main, string nome)
        {
            var runner = main.Player.Pawn.Runner;
            var sb = new System.Text.StringBuilder(nome + ": pawn=" + main.Player.Pawn.Pos.ToString("F1")
                + " cam=" + main.Player.Camera.Cam.transform.position.ToString("F1") + " telegrafia=" + runner.Telegrafia.ToString("F2")
                + " silencio=" + runner.Silencio.ToString("F2") + " escala=" + (main.Player.Pawn.Visual != null ? main.Player.Pawn.Visual.transform.localScale.x.ToString("F2") : "-")
                + " visuais=" + runner.Visuais.Count + "\n");
            foreach (var v in runner.Visuais)
                sb.AppendLine("  visual " + v.Tipo + " " + v.Pos.ToString("F1") + " -> " + v.Pos2.ToString("F1") + " raio=" + v.Raio.ToString("F1")
                    + " restante=" + v.Restante.ToString("F2") + (v.Alvo != null ? " alvo=" + v.Alvo.Nome : ""));
            var vk = Object.FindFirstObjectByType<Gameplay.VisualDosKits>();
            if (vk != null)
            {
                foreach (Renderer r in vk.GetComponentsInChildren<Renderer>(true))
                    if (r.enabled && r.gameObject.name.Length > 0)
                        sb.AppendLine("  desenho " + Caminho(r.transform) + " bounds=" + r.bounds.center.ToString("F1") + " tam=" + r.bounds.size.ToString("F1")
                            + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name : "NULL"));
                foreach (ParticleSystem ps in vk.GetComponentsInChildren<ParticleSystem>(false))
                    if (ps.isPlaying && ps.particleCount > 0) sb.AppendLine("  particula " + Caminho(ps.transform) + " n=" + ps.particleCount);
            }
            else sb.AppendLine("  SEM VisualDosKits");
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        /// <summary>
        /// Os kits do GRUPO D (Fizz, Sylva, Basalto, Noctus, Pip) em acao, no roteiro da Veu/Tessa (treino, tatica e suprema) —
        /// mas de FRENTE para o Boneco1: torreta, bobina, mordida, punho, zigue-zague e nuvem precisam de alguem para acertar.
        /// A Pip mira 35 graus de lado: o 1o dash (zig) atravessa o boneco e a faisca salta para o Boneco2 (3 m ao lado).
        /// Esperas por kit (KNOB por foto): o quadro cai no meio do efeito, nao antes nem depois dele.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Kit_GrupoD([Values("16-fizz", "17-sylva", "18-basalto", "19-noctus", "20-pip")] string slug)
        {
            ExigirGpu();
            string antes = PlayerPrefs.GetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, "");
            try
            {
                Arkana.Menu.SelecaoPersonagem.MagoEscolhido = slug;
                Main main = _go.AddComponent<Main>();
                yield return null;
                Arkana.Menu.Menu.PedidoDeTreino = true;
                Bus.EmitGameStartRequested();
                yield return Esperar(Gameplay.Partida.SUPREMA_TREINO_S + 1f);
                Assert.IsNotNull(main.Player, "treino sem jogador");
                GameObject boneco = GameObject.Find("Boneco1");
                Assert.IsNotNull(boneco, "o treino tem boneco");
                Vector3 b = boneco.transform.position, p0 = main.Player.Pawn.Pos;
                Vector3 dir = new Vector3(b.x - p0.x, 0f, b.z - p0.z).normalized;
                bool pip = slug == "20-pip";
                LevarJogador(main, b - dir * (pip ? 3f : 4f));
                Vector3 mira = pip ? Gameplay.ApoioGrupoD.Girar(dir, -Kits.De(slug).Tatica["zig_graus"]) : dir;
                main.Player.Camera.Logica.Yaw = Mathf.Atan2(mira.x, mira.z);
                main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
                float esperaT = 0.9f, esperaS = 2f;
                switch (slug)
                {
                    case "16-fizz": esperaT = 1.1f; esperaS = 2.15f; break;     // a 2a faisca no ar; o raio da bobina no lampejo
                    case "17-sylva": esperaT = 1.4f; esperaS = 2.6f; break;     // o broto aberto com polen; as raizes cheias
                    case "18-basalto": esperaT = 0.5f; esperaS = 2.3f; break;   // a crista + as pedras; as placas de pe'
                    case "19-noctus": esperaT = 0.6f; esperaS = 2f; break;      // o fio de eter no boneco; a nevoa
                    case "20-pip": esperaT = 0.65f; esperaS = 2.2f; break;      // a faisca saltando; o 2o raio da nuvem
                }
                yield return Esperar(0.3f);
                main.Player.Tatica();
                yield return Esperar(esperaT);
                Foto(main.Player.Camera.Cam, "39-kit-" + slug + "-tatica", true);
                DiagKitGrupoD(main, "39-kit-" + slug + "-tatica", b);
                yield return Esperar(1.2f);
                Gameplay.KitRunner runner = main.Player.Pawn.Runner;
                string pre = "39-kit-" + slug + "-suprema: carga=" + runner.CargaSuprema.ToString("F2") + " pronto=" + runner.ProntoSuprema + "\n";
                main.Player.Suprema();
                yield return Esperar(esperaS);   // a suprema e' TELEGRAFADA: o efeito so' sai quando o aviso no chao enche
                Foto(main.Player.Camera.Cam, "39-kit-" + slug + "-suprema", true);
                File.AppendAllText(Path.Combine(Pasta, "diag.txt"), pre);
                DiagKitGrupoD(main, "39-kit-" + slug + "-suprema", b);
            }
            finally
            {
                PlayerPrefs.SetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, antes);
            }
        }

        /// <summary>O que o kit escreveu (visuais, com alvo) e o que a casca desenhou (renderers LIGADOS): quadro sem dado vira palpite.</summary>
        static void DiagKitGrupoD(Main main, string nome, Vector3 boneco)
        {
            Gameplay.Pawn pawn = main.Player.Pawn;
            var sb = new System.Text.StringBuilder(nome + ": pawn=" + pawn.Pos.ToString("F1") + " boneco=" + boneco.ToString("F1")
                + " hp=" + pawn.Vital.Hp.ToString("F0") + " mana=" + pawn.Mana.ToString("F0") + " visuais=" + pawn.Runner.Visuais.Count + "\n");
            foreach (Gameplay.EfeitoVisual v in pawn.Runner.Visuais)
                sb.AppendLine("  visual " + v.Tipo + " " + v.Pos.ToString("F1") + " -> " + v.Pos2.ToString("F1") + " restante=" + v.Restante.ToString("F2")
                    + (v.Alvo != null ? " alvo=" + v.Alvo.Nome : ""));
            var vk = Object.FindFirstObjectByType<Gameplay.VisualDosKits>();
            if (vk == null) sb.AppendLine("  SEM VisualDosKits");
            else
                foreach (Renderer r in vk.GetComponentsInChildren<Renderer>(true))
                    if (r.enabled && r.gameObject.activeInHierarchy)
                        sb.AppendLine("  desenho " + Caminho(r.transform) + " bounds=" + r.bounds.center.ToString("F1") + " tam=" + r.bounds.size.ToString("F1")
                            + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name : "NULL"));
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        /// <summary>
        /// O GRUPO C (Umbra, Brok, Gromm, Maris) em acao, pelo roteiro da Veu/Tessa: tatica e suprema no treino, pela camera do
        /// jogador. A Danca da Umbra so' aparece na ESQUIVA (a isca fica onde ela estava): a foto esquiva depois do aviso.
        /// O diag leva o que o kit escreveu (visuais) e o que a casca desenhou (renderers ligados e particulas vivas).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Kit_GrupoC([Values("12-umbra", "13-brok", "14-gromm", "15-maris")] string slug)
        {
            ExigirGpu();
            string antes = PlayerPrefs.GetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, "");
            try
            {
                Arkana.Menu.SelecaoPersonagem.MagoEscolhido = slug;
                Main main = _go.AddComponent<Main>();
                yield return null;
                Arkana.Menu.Menu.PedidoDeTreino = true;
                Bus.EmitGameStartRequested();
                yield return Esperar(Gameplay.Partida.SUPREMA_TREINO_S + 1f);
                Assert.IsNotNull(main.Player, "treino sem jogador");
                Assert.IsNotNull(main.Player.Pawn.Runner.Impl, slug + " sem kit: o registro do Grupo C nao entrou");
                OlharParaOCentro(main);
                yield return Esperar(0.3f);
                main.Player.Tatica();
                yield return Esperar(0.9f);
                Foto(main.Player.Camera.Cam, "39-kit-" + slug + "-tatica", true);
                DiagKitC(main, "39-kit-" + slug + "-tatica");
                yield return Esperar(1.2f);
                var runner = main.Player.Pawn.Runner;
                string carga = "39-kit-" + slug + "-suprema: carga=" + runner.CargaSuprema.ToString("F2") + " pronto=" + runner.ProntoSuprema + "\n";
                main.Player.Suprema();
                yield return Esperar(runner.Dados.Telegrafia + 0.15f);   // a suprema e' TELEGRAFADA: o efeito sai quando o aviso enche
                if (slug == "12-umbra") main.Player.Pawn.Dodge();
                yield return Esperar(slug == "14-gromm" ? 0.7f : slug == "15-maris" ? 0.6f : 0.3f);
                Foto(main.Player.Camera.Cam, "39-kit-" + slug + "-suprema", true);
                File.AppendAllText(Path.Combine(Pasta, "diag.txt"), carga);
                DiagKitC(main, "39-kit-" + slug + "-suprema");
            }
            finally
            {
                PlayerPrefs.SetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, antes);
            }
        }

        static void DiagKitC(Main main, string nome)
        {
            var runner = main.Player.Pawn.Runner;
            var sb = new System.Text.StringBuilder(nome + ": pawn=" + main.Player.Pawn.Pos.ToString("F1")
                + " cam=" + main.Player.Camera.Cam.transform.position.ToString("F1") + " tele=" + runner.Telegrafia.ToString("F2")
                + " visuais=" + runner.Visuais.Count + "\n");
            foreach (var v in runner.Visuais)
                sb.AppendLine("  visual " + v.Tipo + " " + v.Pos.ToString("F1") + " -> " + v.Pos2.ToString("F1") + " raio=" + v.Raio.ToString("F1") + " restante=" + v.Restante.ToString("F2"));
            var vk = Object.FindFirstObjectByType<Gameplay.VisualDosKits>();
            if (vk == null) sb.AppendLine("  SEM VisualDosKits");
            else
            {
                foreach (Renderer r in vk.GetComponentsInChildren<Renderer>(true))
                    if (r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer))
                        sb.AppendLine("  desenho " + Caminho(r.transform) + " bounds=" + r.bounds.center.ToString("F1") + " tam=" + r.bounds.size.ToString("F1")
                            + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name : "NULL"));
                foreach (ParticleSystem ps in vk.GetComponentsInChildren<ParticleSystem>(true))
                    if (ps.particleCount > 0) sb.AppendLine("  particulas " + Caminho(ps.transform) + " vivas=" + ps.particleCount);
            }
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        /// <summary>
        /// Os kits do GRUPO B (Vitalis, Ilusionista, Vex, Aelion) em acao no treino, pelo roteiro do Foto_Kit_VeuTessa: tatica
        /// e suprema pela camera do jogador. A espera e' a de cada mago: a flecha do Aelion so' sai cheia em 1,5s, a poca do Vex
        /// so' acende armada (1s depois de pousar), e a suprema fotografa DEPOIS do aviso (1,5 a 3s) com o efeito ja' aberto.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Kit_GrupoB([Values("07-vitalis", "08-ilusionista", "09-vex", "11-aelion")] string slug)
        {
            ExigirGpu();
            // a escolha do mago mora nos PlayerPrefs (persiste no editor): guarda e devolve, a foto nao troca o mago de ninguem
            string antes = PlayerPrefs.GetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, "");
            try
            {
                Arkana.Menu.SelecaoPersonagem.MagoEscolhido = slug;
                Main main = _go.AddComponent<Main>();
                yield return null;
                Arkana.Menu.Menu.PedidoDeTreino = true;
                Bus.EmitGameStartRequested();
                yield return Esperar(Gameplay.Partida.SUPREMA_TREINO_S + 1f);   // no treino a suprema enche em 5 s
                Assert.IsNotNull(main.Player, "treino sem jogador");
                Assert.AreEqual(slug, main.Player.Pawn.Slug, "o treino entrou com o mago escolhido");
                Gameplay.KitRunner runner = main.Player.Pawn.Runner;
                Assert.IsNotNull(runner.Impl, "o kit do " + slug + " nao registrou (a HUD apagaria os botoes)");
                OlharParaOCentro(main);
                yield return Esperar(0.3f);
                var sb = new System.Text.StringBuilder();

                main.Player.Tatica();
                float espera = slug == "11-aelion" ? Kits.De(slug).Tatica["carga"] + 0.15f : slug == "09-vex" ? 1.7f : 0.9f;
                yield return Esperar(espera);
                Foto(main.Player.Camera.Cam, "39-kit-" + slug + "-tatica", true);
                DiagKitGrupoB(sb, "39-kit-" + slug + "-tatica", main);

                yield return Esperar(1.2f);
                string carga = " (carga=" + runner.CargaSuprema.ToString("F2") + " pronto=" + runner.ProntoSuprema + ")";
                main.Player.Suprema();
                float depois = slug == "09-vex" ? 1.5f : slug == "11-aelion" ? 0.3f : slug == "07-vitalis" ? 0.8f : 0.7f;
                yield return Esperar(Kits.De(slug).Telegrafia + depois);
                Foto(main.Player.Camera.Cam, "39-kit-" + slug + "-suprema", true);
                DiagKitGrupoB(sb, "39-kit-" + slug + "-suprema" + carga, main);
                Directory.CreateDirectory(Pasta);
                File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            }
            finally
            {
                PlayerPrefs.SetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, antes);
            }
        }

        /// <summary>O que o kit escreveu e o que a casca desenhou na hora da foto (um quadro sem efeito, sem dado, vira palpite):
        /// os visuais do runner, os desenhos LIGADOS do VisualDosKits por tipo e quantos renderers do mago estao acesos (o
        /// Ilusionista invisivel tem de dar 0).</summary>
        static void DiagKitGrupoB(System.Text.StringBuilder sb, string nome, Main main)
        {
            Gameplay.Pawn pawn = main.Player.Pawn;
            Gameplay.KitRunner runner = pawn.Runner;
            int ligados = 0;
            foreach (Renderer r in pawn.GetComponentsInChildren<Renderer>()) if (r.enabled) ligados++;
            sb.AppendLine(nome + ": pawn=" + pawn.Pos.ToString("F1") + " cam=" + main.Player.Camera.Cam.transform.position.ToString("F1")
                + " telegrafia=" + runner.Telegrafia.ToString("F2") + " visuais=" + runner.Visuais.Count + " renderers do mago ligados=" + ligados);
            foreach (Gameplay.EfeitoVisual v in runner.Visuais)
                sb.AppendLine("  visual " + v.Tipo + " " + v.Pos.ToString("F1") + " -> " + v.Pos2.ToString("F1") + " raio=" + v.Raio.ToString("F1") + " restante=" + v.Restante.ToString("F2"));
            var vk = Object.FindFirstObjectByType<Gameplay.VisualDosKits>();
            if (vk == null) { sb.AppendLine("  SEM VisualDosKits"); return; }
            foreach (Transform t in vk.transform)
            {
                int on = 0, total = 0;
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) { total++; if (r.enabled && r.gameObject.activeInHierarchy) on++; }
                if (on > 0) sb.AppendLine("  desenho " + t.name + " " + t.position.ToString("F1") + " renderers " + on + "/" + total);
            }
        }

        // ONDA 10A — colar em FotoTests.cs, depois de Foto_Chao_Ruinas. Rodar: .\foto.ps1 "Foto_Ruinas_PedraDaMeshy"

        /// <summary>
        /// A PEDRA DAS RUINAS (onda 10A): colunas, colunas caidas, as duas muralhas e o altar de Ruinas.cs viram a coluna canelada
        /// e o bloco rachado da Meshy (38/39-*.glb) no lugar dos prismas bege lisos da foto 37. 40-ruinas-perto: camera temporaria
        /// a 2,2 m do chao, 9 m de lado para a coluna caida (as de pe' do anel atras dela, a muralha leste ao fundo). 40-ruinas-alto:
        /// ~28 m sobre o noroeste do plato (o quadro da 14, mais alto), olhando o centro: o anel, as caidas, as duas muralhas, o altar.
        /// Treino: deterministico. Guarda junto o contrato: a Meshy entrou (uma malha por .glb, com colisor) e o prisma nao sobrou.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Ruinas_PedraDaMeshy()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Arkana.World.Ruinas ru = ilha.Ruinas;
            Assert.IsNotNull(ru, "sem ruinas");
            var sb = new System.Text.StringBuilder("40-ruinas: colunas=" + ru.Colunas + " blocos=" + ru.Blocos + " pegadas=" + ru.Pegadas.Count);
            foreach (string nome in new[] { "Colunas", "Blocos" })
            {
                Transform t = ru.transform.Find(nome);
                Assert.IsNotNull(t, "a malha " + nome + " da Meshy nao entrou (sem o .glb volta o prisma)");
                Assert.IsNotNull(t.GetComponent<MeshCollider>(), nome + " sem colisor: a Queda e o corpo atravessam");
                Mesh m = t.GetComponent<MeshFilter>().sharedMesh;
                Material mat = t.GetComponent<MeshRenderer>().sharedMaterial;
                sb.Append(" | " + nome + ": tris=" + m.GetIndexCount(0) / 3 + " vert=" + m.vertexCount + " bounds=" + m.bounds.size.ToString("F0")
                    + " mat=" + (mat != null ? mat.name + " / " + mat.shader.name : "NULL"));
            }
            Assert.IsNull(ru.transform.Find("Ruinas"), "sobrou prisma procedural junto com a Meshy");
            sb.AppendLine();

            // 1) PERTO: a coluna caida DE LADO (fuste canelado, capitel quebrado no chao) a 9 m, olho de gente, o anel de pe' e a
            //    muralha leste atras. O anel do kit (arco, estatua) cai perto dali: o olho vai para o primeiro ponto livre, de 30 em 30
            //    graus em volta da caida. Depois o jogador vai para la': o corte do kit e da grama mede a camera DELE
            Arkana.World.Relevo r = ilha.Relevo;
            Vector2 c = r.Ruinas;
            Vector4 caida = ru.Pegadas[0];
            foreach (Vector4 q in ru.Pegadas) if (q.w > caida.w) caida = q;   // a pegada larga e' a da coluna caida
            Vector2 alvo = new Vector2(caida.x, caida.y), dir = (alvo - c).normalized, lado = new Vector2(-dir.y, dir.x);
            Vector3 olho = Vector3.zero;
            for (int k = 0; k < 12; k++)
            {
                float a = k * Mathf.PI / 6f;
                Vector2 o = alvo + (lado * Mathf.Cos(a) + dir * Mathf.Sin(a)) * 9f;
                olho = new Vector3(o.x, Arkana.World.Ilha.AlturaDoChao(o.x, o.y) + 2.2f, o.y);
                if (Physics.OverlapSphere(olho, 1.5f, ~0, QueryTriggerInteraction.Ignore).Length == 0) break;
            }
            LevarJogador(main, olho);
            if (ilha.Kit != null) foreach (Renderer rr in ilha.Kit.GetComponentsInChildren<Renderer>(true)) rr.enabled = true;
            yield return Esperar(1f);   // grama e kit reclassificam (4 Hz) em volta dele
            float ha = Arkana.World.Ilha.AlturaDoChao(alvo.x, alvo.y);
            Camera cam = CameraTemporaria("CamFotoRuinasPerto", olho, new Vector3(alvo.x, ha + 1f, alvo.y), Color.gray);
            Arkana.World.Vegetacao veg = ilha.Vegetacao;
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "40-ruinas-perto", false);
            Object.Destroy(cam.gameObject);
            sb.AppendLine("40-ruinas-perto: olho=" + olho.ToString("F1") + " caida=" + caida.ToString("F1"));

            // 2) DO ALTO: 28 m sobre o noroeste do plato (a um raio do centro em x e em z), olhando o centro
            float hc = Arkana.World.Ilha.AlturaDoChao(c.x, c.y), raio = r.RuinasR;
            var de = new Vector3(c.x - raio, hc + 28f, c.y + raio);
            cam = CameraTemporaria("CamFotoRuinasAlto40", de, new Vector3(c.x + 4f, hc, c.y - 4f), Color.gray);
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "40-ruinas-alto", false);
            Object.Destroy(cam.gameObject);
            if (veg != null) veg.Olho = null;
            sb.AppendLine("40-ruinas-alto: cam=" + de.ToString("F1") + " centro=" + new Vector3(c.x, hc, c.y).ToString("F1"));
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }
        /// <summary>
        /// ONDA 11 — as PECAS DA MESHY no lugar das primitivas, pela camera do jogador no treino (roteiro do Foto_Kit_GrupoD):
        /// 41-pecas-basalto (os muros de terra que o Punho ergue + o Monolito), 41-pecas-fizz-torreta, 41-pecas-fizz-bobina
        /// (armada, laser no boneco), 41-pecas-fizz-sucata (a bobina tombada depois do raio) e 41-pecas-brok (a Runa-Escudo ja'
        /// fria). O jogador fica DE LADO do boneco (muro nao nasce em celula ocupada) e recua depois de fincar a torreta e a bobina
        /// (a peca a 1,2-1,5 m ficaria atras do corpo dele). O diag lista cada peca com o MATERIAL: "(meshy)" = veio do .glb;
        /// ArkanaMundo / Vfx = caiu na primitiva. Esperas: KNOB por foto (o quadro cai com a peca inteira de pe').
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Pecas_Onda11([Values("18-basalto", "16-fizz", "13-brok")] string slug)
        {
            ExigirGpu();
            string antes = PlayerPrefs.GetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, "");
            try
            {
                Arkana.Menu.SelecaoPersonagem.MagoEscolhido = slug;
                Main main = _go.AddComponent<Main>();
                yield return null;
                Arkana.Menu.Menu.PedidoDeTreino = true;
                Bus.EmitGameStartRequested();
                yield return Esperar(Gameplay.Partida.SUPREMA_TREINO_S + 1f);
                Assert.IsNotNull(main.Player, "treino sem jogador");
                Assert.IsNotNull(main.Player.Pawn.Runner.Impl, slug + " sem kit");
                GameObject boneco = GameObject.Find("Boneco1");
                Assert.IsNotNull(boneco, "o treino tem boneco");
                Vector3 b = boneco.transform.position, p0 = main.Player.Pawn.Pos;
                Vector3 dir = new Vector3(b.x - p0.x, 0f, b.z - p0.z).normalized, lado = Vector3.Cross(Vector3.up, dir);
                // o Fizz mira PERTO do boneco (o cone da torreta e o laser da bobina precisam de alguem); Basalto e Brok, ao lado
                Vector3 pe = slug == "16-fizz" ? b - dir * 5f + lado * 1.5f : b - dir * 5f + lado * 6f;
                LevarJogador(main, pe);
                main.Player.Camera.Logica.Yaw = Mathf.Atan2(dir.x, dir.z);
                main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
                yield return Esperar(0.3f);
                Gameplay.KitRunner runner = main.Player.Pawn.Runner;
                switch (slug)
                {
                    case "18-basalto":
                        main.Player.Tatica();                           // o Punho: a onda chega aos 5 m e ergue 3 muros
                        yield return Esperar(1f);
                        main.Player.Suprema();
                        yield return Esperar(runner.Dados.Telegrafia + 0.6f);   // o Monolito ja' de pe' (sobe em 0,3 s)
                        Foto(main.Player.Camera.Cam, "41-pecas-basalto", true);
                        DiagPecasOnda11(main, "41-pecas-basalto");
                        break;
                    case "16-fizz":
                        main.Player.Tatica();
                        yield return Esperar(0.4f);
                        LevarJogador(main, pe - dir * 2.5f);              // recua: a torreta fica a ~4 m, inteira no quadro
                        yield return Esperar(0.8f);
                        Foto(main.Player.Camera.Cam, "41-pecas-fizz-torreta", true);
                        DiagPecasOnda11(main, "41-pecas-fizz-torreta");
                        yield return Esperar(1f);
                        Vector3 aqui = main.Player.Pawn.Pos;
                        main.Player.Suprema();
                        yield return Esperar(0.3f);
                        LevarJogador(main, aqui - dir * 2.5f);            // a bobina nasce a 1,2 m: recua de novo
                        yield return Esperar(1f);                        // armada (sobe em 0,75 s), coroa carregando, laser
                        Foto(main.Player.Camera.Cam, "41-pecas-fizz-bobina", true);
                        DiagPecasOnda11(main, "41-pecas-fizz-bobina");
                        yield return Esperar(runner.Dados.Telegrafia - 1.3f + 0.6f);   // disparou: a sucata tombada
                        Foto(main.Player.Camera.Cam, "41-pecas-fizz-sucata", true);
                        DiagPecasOnda11(main, "41-pecas-fizz-sucata");
                        break;
                    case "13-brok":
                        main.Player.Tatica();
                        yield return Esperar(1.6f);                      // ja' esfriou (1,4 s): a madeira e o ferro, o aro azul
                        Foto(main.Player.Camera.Cam, "41-pecas-brok", true);
                        DiagPecasOnda11(main, "41-pecas-brok");
                        break;
                }
            }
            finally
            {
                PlayerPrefs.SetString(Arkana.Menu.SelecaoPersonagem.PrefEscolhido, antes);
            }
        }

        /// <summary>As pecas LIGADAS dos kits e dos muros no quadro, com o material (o nome diz se veio do .glb).</summary>
        static void DiagPecasOnda11(Main main, string nome)
        {
            var sb = new System.Text.StringBuilder(nome + ": pawn=" + main.Player.Pawn.Pos.ToString("F1")
                + " cam=" + main.Player.Camera.Cam.transform.position.ToString("F1") + "\n");
            foreach (Gameplay.EfeitoVisual v in main.Player.Pawn.Runner.Visuais)
                sb.AppendLine("  visual " + v.Tipo + " " + v.Pos.ToString("F1") + " -> " + v.Pos2.ToString("F1") + " raio=" + v.Raio.ToString("F1")
                    + " restante=" + v.Restante.ToString("F2"));
            var raizes = new Component[] { Object.FindFirstObjectByType<Gameplay.VisualDosKits>(), Object.FindFirstObjectByType<Arkana.Terrain.VisualDoTerreno>() };
            foreach (Component raiz in raizes)
            {
                if (raiz == null) { sb.AppendLine("  (sem uma das cascas)"); continue; }
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                    if (r.enabled && !(r is ParticleSystemRenderer) && !(r is LineRenderer))
                        sb.AppendLine("  peca " + Caminho(r.transform) + " bounds=" + r.bounds.center.ToString("F1") + " tam=" + r.bounds.size.ToString("F1")
                            + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.name + " [" + r.sharedMaterial.shader.name + "]" : "NULL"));
            }
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }

        // ONDA 12B — colar em FotoTests.cs, dentro da classe (o onda12-carregar-compila.sh cola no fim). Rodar: .\foto.ps1 "Foto_Carregando"

        /// <summary>
        /// A TELA DE CARREGAMENTO (onda 12B) no MEIO da montagem da partida normal (12 bots: a mais longa). A arte e' o quadro da
        /// vitrine (o mago escolhido em guarda no pico, o por do sol atras) que o Main fotografa no toque do JOGAR; a esquerda
        /// PREPARANDO A PARTIDA, o nome, o titulo e a gema do elemento com o papel; embaixo a placa da DICA, o passo, o % e a
        /// barra dourada com o brilho na ponta; no canto o selo girando. A foto sai pela camera do menu (desligada: so' renderiza
        /// quando pedida) com a UI por cima — a tela cobre o mundo inteiro, entao a camera nao importa. Guarda junto o contrato:
        /// a montagem ainda nao acabou quando a foto sai, o relogio do jogo esta' parado e a HUD ainda nao acendeu.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Carregando_TelaNoMeioDaMontagem()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return Esperar(1.2f);   // a vitrine poe o mago no pico e o corte por distancia acorda: a arte e' esse quadro
            Bus.EmitGameStartRequested();
            int n = 0;
            while (main.Carregando && main.Tela.Logica.Barra < 0.4f && n < 900) { yield return null; n++; }
            Assert.IsTrue(main.Carregando, "a montagem acabou antes da foto: nao sobrou meio para fotografar");
            Assert.AreEqual(0f, Time.timeScale, "carregando, o relogio do jogo espera");
            Assert.IsTrue(main.Hud == null || !main.Hud.gameObject.activeSelf, "a HUD monta apagada atras da tela");
            Foto(main.CameraDoMenu, "42-carregando", true);
        }

        // ONDA 13A — colar no FIM da classe FotoTests (antes do "}" que fecha a classe). Rodar: .\foto.ps1 "Foto_Costa"

        /// <summary>
        /// A COSTA DO MAR (onda 13A): a faixa rasa turquesa abracando a ilha, a espuma branca na linha d'agua que RESPIRA (a frente
        /// vai e volta, ~6,7 s por onda) e a linha rala ao largo entrando — tudo do `_Costa` que a Ilha assa e so' o mar le'.
        /// 42-costa-aerea: a altura da queda (200 m), 220 m ao largo da praia das dunas, olhando a costa. 42-costa-praia e
        /// 42-costa-praia-b (3 s depois: a espuma andou): a altura da camera do jogo na areia seca, olhando o mar na diagonal da praia.
        /// 42-costa-enseada: 70 m atras do fundo do fiorde, olhando a boca (raso nas duas margens, mar aberto la' fora).
        /// Treino: deterministico. Guarda junto o contrato: o mar le' a costa; o lago e o alagado nao (neles nada muda).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Costa_AereaPraiaEnseada()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Arkana.World.Relevo r = ilha.Relevo;
            Arkana.World.Vegetacao veg = ilha.Vegetacao;
            var sb = new System.Text.StringBuilder();

            // o contrato: so' o MAR recebe a costa assada
            GameObject mar = GameObject.Find("Ilha/Gerado/Mar");
            Assert.IsNotNull(mar, "sem mar");
            Material mm = mar.GetComponent<Renderer>().sharedMaterial;
            if (mm.shader.name == "Arkana/Agua")
            {
                Texture tc = mm.GetTexture("_Costa");
                Assert.IsNotNull(tc, "o mar le' a costa assada (_Costa)");
                Assert.AreEqual(Arkana.World.Ilha.CostaTexels, tc.width);
                Vector4 ret = mm.GetVector("_CostaRet");
                Assert.Greater(ret.z, 0f, "_CostaRet sem escala: o mar inteiro amostraria um texel so'");
                Assert.LessOrEqual(ret.x, -0.5f * r.Lado, "o retangulo da costa cobre a ilha");
                Assert.AreEqual(1f + Arkana.World.Ilha.CostaSeco, ret.w, 1e-5f, "o shader decodifica a costa pelo _CostaRet.w");
                foreach (string poca in new[] { "Lago", "Alagado" })
                {
                    GameObject g = GameObject.Find("Ilha/Gerado/" + poca);
                    Assert.IsNotNull(g, "sem " + poca);
                    Assert.IsNull(g.GetComponent<Renderer>().sharedMaterial.GetTexture("_Costa"), poca + " nao le' a costa: nada muda nele");
                }
                sb.AppendLine("42-costa: textura " + tc.width + "x" + tc.height + " ret=" + ret.ToString("F5") + " turquesa=" + mm.GetColor("_Turquesa")
                    + " raso=" + mm.GetFloat("_CostaRaso").ToString("F2") + " espuma=" + mm.GetFloat("_CostaEspuma").ToString("F2") + " vel=" + mm.GetFloat("_CostaVel").ToString("F2"));
            }
            else sb.AppendLine("42-costa: SEM Arkana/Agua (cadeia antiga): " + mm.shader.name);

            // a linha de cota `cota` de FORA no rumo `ang` (varre de fora para dentro: o lago e o fiorde nao contam); cota < 0 = no mar
            Vector3 Beira(float ang, float cota)
            {
                var d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                float m = r.RaioTerra + 160f;
                while (m > 0f && r.Altura(d.x * m, d.z * m) < cota) m -= 0.25f;
                return new Vector3(d.x * m, 0f, d.z * m);
            }
            void Diag(string nome, Vector3 de, Vector3 para)
            {
                bool tapa = Physics.Linecast(de, para, out RaycastHit h);
                sb.AppendLine(nome + ": cam=" + de.ToString("F1") + " alvo=" + para.ToString("F1") + " costa no alvo=" + Arkana.World.Ilha.Costa(r, para.x, para.z).ToString("F2")
                    + " no caminho: " + (tapa ? Caminho(h.collider.transform) + " a " + h.distance.ToString("F1") + " m" : "nada"));
            }
            float eixo = Mathf.Atan2(r.Dunas.y, r.Dunas.x);   // a praia das dunas (sul)
            Vector3 fora = new Vector3(Mathf.Cos(eixo), 0f, Mathf.Sin(eixo));

            // 1) DO ALTO: 200 m acima do mar, 220 m ao largo da praia, olhando 30 m terra adentro (~42 graus para baixo)
            Vector3 praia = Beira(eixo, 0f);
            Vector3 olho = praia + fora * 220f + Vector3.up * 200f, alvo = praia - fora * 30f;
            Camera cam = CameraTemporaria("CamFotoCostaAerea", olho, alvo, Color.gray);
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "42-costa-aerea", false);
            Object.Destroy(cam.gameObject);
            Diag("42-costa-aerea", olho, alvo);

            // 2) NA PRAIA: a altura da camera do jogo (3,6 m) sobre a areia seca de 1 m de cota, olhando o mar na DIAGONAL da praia
            //    (60% ao largo, 80% ao longo), 14 graus para baixo: a espuma da beira embaixo, o turquesa no meio, o fundo e o horizonte
            //    em cima. Duas fotos, 3 s entre elas: a frente da espuma e a linha ao largo andaram.
            Vector3 pe = Beira(eixo - 3f * Mathf.Deg2Rad, 1f);
            olho = new Vector3(pe.x, Arkana.World.Ilha.AlturaDoChao(pe.x, pe.z) + 3.6f, pe.z);
            Vector3 aoLongo = new Vector3(-fora.z, 0f, fora.x);
            Vector3 dir = (fora * 0.6f + aoLongo * 0.8f).normalized;
            const float pitch = 14f * Mathf.Deg2Rad;
            alvo = olho + dir * (olho.y / Mathf.Tan(pitch)) + Vector3.down * olho.y;   // onde o centro do quadro toca a lamina (y 0)
            LevarJogador(main, olho - dir * 4f);   // o corpo atras da camera: a grama e o kit cortam pela camera DELE
            yield return Esperar(1.2f);
            cam = CameraTemporaria("CamFotoCostaPraia", olho, alvo, Color.gray);
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "42-costa-praia", false);
            yield return Esperar(3f);
            Foto(cam, "42-costa-praia-b", false);
            Object.Destroy(cam.gameObject);
            Diag("42-costa-praia", olho, alvo);

            // 3) O FIORDE: 70 m de altura, 40 m atras do fundo dele, olhando o meio do canal e a boca. As pontas sao o BEnseadaA/B do
            //    Relevo (privados, metros-base x Escala)
            Vector2 fundo = new Vector2(-84f, -4f) * r.Escala, boca = new Vector2(-142f, 30f) * r.Escala;
            Vector2 eixoF = (boca - fundo).normalized, meio = (fundo + boca) * 0.5f;
            olho = new Vector3(fundo.x - eixoF.x * 40f, 70f, fundo.y - eixoF.y * 40f);
            alvo = new Vector3(meio.x, 0f, meio.y);
            cam = CameraTemporaria("CamFotoCostaEnseada", olho, alvo, Color.gray);
            if (veg != null) veg.Olho = cam;
            yield return null;
            Foto(cam, "42-costa-enseada", false);
            Object.Destroy(cam.gameObject);
            if (veg != null) veg.Olho = null;
            Diag("42-costa-enseada", olho, alvo);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }


        // ONDA 13B — colar em FotoTests.cs, no fim da classe. Rodar: .\foto.ps1 "Foto_Ceu"

        /// <summary>Direcao pelo azimute (graus, 0 = +z, positivo gira para +x) e pela elevacao (graus, positivo = para cima).</summary>
        static Vector3 DirecaoCeu(float azimute, float elevacao)
        {
            float a = azimute * Mathf.Deg2Rad, e = elevacao * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(a) * Mathf.Cos(e));
        }

        /// <summary>
        /// O CEU (onda 13B): a banda de cumulos em couve-flor no horizonte (base reta, 3 faixas toon, borda de luz dourada do
        /// lado do sol) e os cirros finos no alto, no lugar das manchas bege de borda mole das fotos 34/40. Do chao do treino,
        /// olho a 3,6 m (a altura da camera do jogo): 43-ceu-horizonte olhando o sol (15 graus a' esquerda dele, 12 para cima:
        /// o sol, o halo e os cumulos CONTRA a luz — silhueta lilas com a borda de prata), 43-ceu-contra de costas para o sol (6
        /// para cima: os cumulos acesos, creme no topo e rosa na base) e 43-ceu-alto a 45 graus, de lado para o sol (os cirros
        /// e o degrade lilas -> azul-violeta). Treino: deterministico. Guarda junto o contrato: o skybox e' o Arkana/Ceu (se o
        /// shader nao compilar a cadeia antiga assume calada) e as propriedades que o Ilha.cs e os KNOBs usam existem.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Ceu_HorizonteContraAlto()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(1.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Material ceu = RenderSettings.skybox;
            Assert.IsNotNull(ceu, "sem skybox");
            Assert.AreEqual("Arkana/Ceu", ceu.shader.name, "o ceu nao e' o Arkana/Ceu (o shader nao compilou e a cadeia antiga assumiu?)");
            foreach (string p in new[] { "_Topo", "_Meio", "_Horizonte", "_Nuvem", "_NuvemMeio", "_NuvemSombra", "_NuvemRim", "_Cirro",
                                         "_NevoaNoHorizonte", "_NevoaNaNuvem", "_Cobertura", "_AlturaBanda", "_TamanhoNuvem", "_Vento", "_RimForca", "_CirroForca" })
                Assert.IsTrue(ceu.HasProperty(p), "o ceu perdeu a propriedade " + p);
            var sol = Object.FindFirstObjectByType<Arkana.World.Sol>();
            Assert.IsNotNull(sol, "sem sol");
            Vector3 paraSol = -sol.transform.forward;
            float azSol = Mathf.Atan2(paraSol.x, paraSol.z) * Mathf.Rad2Deg;
            float elSol = Mathf.Asin(Mathf.Clamp(paraSol.y, -1f, 1f)) * Mathf.Rad2Deg;

            Vector3 olho = main.Player.Pawn.Pos + Vector3.up * 3.6f;
            var tomadas = new[] { ("43-ceu-horizonte", azSol - 15f, 12f), ("43-ceu-contra", azSol + 180f, 6f), ("43-ceu-alto", azSol + 120f, 45f) };
            foreach (var (nome, az, el) in tomadas)
            {
                Camera cam = CameraTemporaria("CamFotoCeu", olho, olho + DirecaoCeu(az, el) * 100f, Color.gray);
                Foto(cam, nome, false);
                Object.Destroy(cam.gameObject);
            }
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), "43-ceu: skybox=" + ceu.name + " / " + ceu.shader.name + " suportado=" + ceu.shader.isSupported
                + " sol az=" + azSol.ToString("F0") + " el=" + elSol.ToString("F0") + " olho=" + olho.ToString("F1")
                + " cobertura=" + ceu.GetFloat("_Cobertura").ToString("F2") + " vento=" + ceu.GetFloat("_Vento").ToString("F3") + "\n");
        }

        /// <summary>
        /// 43-ceu-castelo: o MESMO quadro da 34-castelo-vivo (partida, seed das fotos, 3 s de voo), para comparar lado a lado
        /// o ceu de manchas de antes com a banda de cumulos atras do castelo e os cirros por cima.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Ceu_Castelo()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Bus.EmitGameStartRequested();
            yield return Esperar(3f);
            Assert.IsNotNull(main.Castelo, "partida sem castelo");
            Arkana.World.Castelo cas = main.Castelo;
            Vector3 c = cas.transform.position, dir = cas.Rota.Direcao;
            Vector3 lado = Vector3.Cross(Vector3.up, dir).normalized;
            Camera cam = CameraTemporaria("CamFotoCeuCastelo", c + lado * 60f - dir * 4f + Vector3.down * 10f, c - dir * 18f + Vector3.down * 6f, Color.gray);
            cam.fieldOfView = 62f;
            Foto(cam, "43-ceu-castelo", false);
            Object.Destroy(cam.gameObject);
        }

        // ONDA 13C — colar em FotoTests.cs, dentro da classe (no fim, antes do fecha-chave). Rodar: .\foto.ps1 "Foto_Praia"

        /// <summary>
        /// A PRAIA deixou de ser areia vazia (foto 33: um tronco e uma pedrinha) e o CUME perdeu as lascas claras chapadas (fotos 27 e
        /// 41). 44-cume-seixos: o quadro da 27-pedras-treino (a camera do jogador 2,5 s depois do treino comecar, com a HUD) com o
        /// seixo de basalto da Meshy no chao. 44-praia-barco: o barco naufragado da costa das dunas a 8 m, do lado da terra, a agua
        /// atras (o angulo sem pedra nem tronco na frente e com o sol mais nas costas). 44-praia-costa: o MESMO quadro da 33-praia (antes x depois): troncos, rochas, capim
        /// e o barco no fundo. 44-praia-alto: a faixa de praia do barco a ~60 m de altura. O jogador e' levado para perto de cada
        /// camera (a grama e o kit cortam pela camera DELE); o LOD e o corte da praia, pela camera da foto (Vegetacao.Olho).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Praia_BarcoCostaAltoECume()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Arkana.World.Vegetacao veg = ilha.Vegetacao;
            Assert.IsNotNull(veg, "sem vegetacao");
            Arkana.World.Praia praia = veg.Praia;
            Assert.IsNotNull(praia, "a vegetacao nao montou a praia");
            Arkana.World.Relevo r = ilha.Relevo;
            int cume = ilha.Grama != null ? ilha.Grama.Contar("SeixosCume") : 0;
            var sb = new System.Text.StringBuilder("44-praia: moldes=" + praia.Moldes + " barcos=" + praia.Contar(Arkana.World.Praia.Peca.Barco)
                + " troncos=" + praia.Contar(Arkana.World.Praia.Peca.Tronco) + " rochas=" + praia.Contar(Arkana.World.Praia.Peca.Rocha)
                + " capim=" + praia.Contar(Arkana.World.Praia.Peca.Capim) + " seixos-cume=" + cume
                + " seixos=" + (ilha.Grama != null ? ilha.Grama.Contar("Seixos") : 0) + "\n");

            // 1) O CUME: o quadro da 27-pedras-treino, antes de mexer no jogador
            Foto(main.Player.Camera.Cam, "44-cume-seixos", true);
            Diagnostico(main, "44-cume-seixos");

            // 2) O BARCO de perto: o primeiro (costa das dunas), a 8 m do lado da terra, olhando o casco com o mar atras
            int ib = -1;
            for (int i = 0; i < praia.Total && ib < 0; i++) if (praia.Plantada(i).Tipo == Arkana.World.Praia.Peca.Barco) ib = i;
            Assert.GreaterOrEqual(ib, 0, "nenhum barco (falta o 45-barco-naufragado.glb em Resources?)");
            Arkana.World.Praia.PecaPlantada barco = praia.Plantada(ib);
            Vector3 b = barco.M.GetColumn(3);
            var mar = new Vector3(barco.Mar.x, 0f, barco.Mar.y);
            var ao = new Vector3(-mar.z, 0f, mar.x);
            var sol = Object.FindFirstObjectByType<Arkana.World.Sol>();
            Vector3 l = sol != null ? sol.transform.forward : new Vector3(0.6f, -0.5f, 0.6f);
            l.y = 0f;
            l.Normalize();
            // do lado da terra, a 8 m, de -60 a +60 graus: o angulo sem pedra nem tronco na frente do casco (a cena do barco encosta
            // nele) e com o sol mais nas costas do fotografo
            Vector3 olho = b - mar * 8f;
            float nota = float.MinValue;
            for (int k = -4; k <= 4; k++)
            {
                float a = k * 15f * Mathf.Deg2Rad;
                Vector3 d = -mar * Mathf.Cos(a) + ao * Mathf.Sin(a), c = b + d * 8f;
                if (Arkana.World.Ilha.SuperficieDaAgua(c.x, c.z) != Arkana.World.Relevo.Seco) continue;
                float n = Vector3.Dot(-d, l);
                for (int i = 0; i < praia.Total; i++)
                {
                    Arkana.World.Praia.PecaPlantada q = praia.Plantada(i);
                    if (q.Tipo == Arkana.World.Praia.Peca.Barco || q.Tipo == Arkana.World.Praia.Peca.Capim) continue;
                    Vector3 w = (Vector3)q.M.GetColumn(3) - c, ab = b - c;
                    w.y = ab.y = 0f;
                    float t = Mathf.Clamp01(Vector3.Dot(w, ab) / ab.sqrMagnitude);
                    if (t > 0.1f && t < 0.9f && (w - ab * t).magnitude < 1.2f + q.Escala) n -= 10f;
                }
                if (n > nota) { nota = n; olho = c; }
            }
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 1.7f;
            Vector3 ver = b - olho;   // o sentido camera -> barco: o corpo fica 4 m atras da camera
            ver.y = 0f;
            ver.Normalize();
            LevarJogador(main, olho - ver * 4f);
            yield return Esperar(1.2f);
            Camera cam = CameraTemporaria("CamFotoBarco", olho, b + Vector3.up * 0.9f, Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "44-praia-barco", false);
            sb.AppendLine("  44-praia-barco: barco em " + b.ToString("F1") + " mar=" + mar.ToString("F2") + " cam=" + olho.ToString("F1") + " nota=" + nota.ToString("F2")
                + " dist=" + Vector3.Distance(olho, b).ToString("F1") + " sol-nas-costas=" + Vector3.Dot((b - olho).normalized, l).ToString("F2")
                + " lotes=" + praia.LotesEnviados + " tris-enviados=" + praia.TrisEnviados);
            foreach (Collider k in Physics.OverlapSphere(b, 3f)) sb.AppendLine("    colisor no barco: " + Caminho(k.transform) + " (" + k.GetType().Name + ")");
            Object.Destroy(cam.gameObject);

            // 3) A COSTA: o quadro da 33-praia (a costa sul, onde as dunas descem ao mar), para comparar antes e depois
            float eixo = Mathf.Atan2(r.Dunas.y, r.Dunas.x);
            Vector3 PontoDaCosta(float ang, float cota)
            {
                var d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                float m = r.Dunas.magnitude;
                while (m < r.RaioTerra + 60f && r.Altura(d.x * m, d.z * m) > cota) m += 0.5f;
                return new Vector3(d.x * m, cota, d.z * m);
            }
            olho = PontoDaCosta(eixo - 16f * Mathf.Deg2Rad, 1.4f);
            Vector3 alvo = PontoDaCosta(eixo + 4f * Mathf.Deg2Rad, 0.3f);
            Vector3 praTras = olho - alvo;
            praTras.y = 0f;
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 1.9f;
            LevarJogador(main, olho + praTras.normalized * 4f);
            yield return Esperar(1.2f);
            cam = CameraTemporaria("CamFotoCosta", olho, alvo, Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "44-praia-costa", false);
            sb.AppendLine("  44-praia-costa: cam=" + olho.ToString("F1") + " alvo=" + alvo.ToString("F1") + " barco a " + Vector3.Distance(olho, b).ToString("F0")
                + " m lotes=" + praia.LotesEnviados + " tris-enviados=" + praia.TrisEnviados);
            Object.Destroy(cam.gameObject);

            // 4) DO ALTO: 60 m sobre a areia, 45 m para dentro da terra, olhando a beira d'agua do barco (a faixa atravessa o quadro)
            olho = b - mar * 45f;
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 60f;
            LevarJogador(main, b - mar * 18f);
            yield return Esperar(1.2f);
            cam = CameraTemporaria("CamFotoPraiaAlto", olho, b + mar * 8f, Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "44-praia-alto", false);
            sb.AppendLine("  44-praia-alto: cam=" + olho.ToString("F1") + " lotes=" + praia.LotesEnviados + " tris-enviados=" + praia.TrisEnviados);
            Object.Destroy(cam.gameObject);
            veg.Olho = null;
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.Greater(praia.Contar(Arkana.World.Praia.Peca.Capim), 0, "o capim da Meshy nao nasceu (falta o 48-capim-duna.glb?)");
            Assert.Greater(cume, 0, "o seixo do cume da Meshy nao carregou (falta o 49-seixos-cume.glb?)");
        }


        /// <summary>
        /// DIAGNOSTICO DO CORPO (queixa do Diretor, 16/09): a luva na mao "cobre a camera", o pulo e a corrida para tras.
        /// 46-diag-luva*: a luva comum equipada (camera do jogador e de perto) + a escala do osso da mao no diag.
        /// 46-diag-pulo-N: o pulo visto de lado, quadro a quadro. 46-diag-tras-N: stick para tras solto e mirando.
        /// O Player e' desligado para o teste escrever a intencao direto no Pawn (a HUD reescreveria o stick).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Diag_LuvaPuloTras()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Gameplay.Pawn eu = main.Player.Pawn;
            var sb = new System.Text.StringBuilder("46-diag: fonte=" + eu.Visual.Fonte + "\n");

            // 1) A LUVA
            eu.Slot.Equipar(Gameplay.Arma.VARINHA);
            yield return Esperar(0.4f);
            Transform mao = eu.Visual.MaoDireita;
            sb.AppendLine("  mao=" + Caminho(mao) + " lossy=" + mao.lossyScale.ToString("F3") + " rig=" + eu.Visual.transform.lossyScale.ToString("F3"));
            foreach (Renderer r in mao.GetComponentsInChildren<Renderer>())
                sb.AppendLine("    " + r.name + " bounds=" + r.bounds.size.ToString("F2") + " centro=" + r.bounds.center.ToString("F1"));
            Foto(main.Player.Camera.Cam, "46-diag-luva", true);
            Vector3 p = eu.Pos;
            Camera cl = CameraTemporaria("CamDiagLuva", p + eu.transform.forward * 3f + eu.transform.right * 1.5f + Vector3.up * 1.6f, p + Vector3.up * 1.1f, Color.gray);
            Foto(cl, "46-diag-luva-perto", false);
            Object.Destroy(cl.gameObject);

            // 2) O PULO, de lado
            main.Player.enabled = false;
            eu.Stick = Vector2.zero;
            eu.YawAlvo = null;
            yield return Esperar(0.5f);
            p = eu.Pos;
            Vector3 lado = eu.transform.right;
            Assert.IsTrue(eu.Pular(), "nao pulou");
            float t = 0f;
            int n = 0;
            float[] marcas = { 0.08f, 0.25f, 0.45f, 0.65f, 0.8f, 0.95f, 1.15f };
            while (n < marcas.Length && t < 3f)
            {
                yield return null;
                t += Time.deltaTime;
                if (t < marcas[n]) continue;
                Camera cp = CameraTemporaria("CamDiagPulo", p + lado * 5f + Vector3.up * 1.4f, p + Vector3.up * 1.4f, Color.gray);
                Foto(cp, "46-diag-pulo-" + n, false);
                Object.Destroy(cp.gameObject);
                sb.AppendLine("  pulo t=" + t.ToString("F2") + " clipe=" + eu.Clipe + " visual=" + eu.Visual.ClipeAtual + " vy=" + eu.Loc.Vy.ToString("F2")
                    + " noChao=" + eu.NoChao + " y=" + (eu.Pos.y - p.y).ToString("F2"));
                n++;
            }
            yield return Esperar(1f);

            // 3) PARA TRAS: solto (o corpo vira?) e mirando (o corpo encara a camera)
            foreach (bool mirando in new[] { false, true })
            {
                float yaw = main.Player.Camera.Logica.Yaw;
                eu.YawCam = yaw;
                eu.YawAlvo = mirando ? yaw : (float?)null;
                eu.transform.rotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);
                eu.Stick = new Vector2(0f, -1f);
                t = 0f;
                n = 0;
                float[] tras = { 0.1f, 0.25f, 0.5f, 1.0f };
                while (n < tras.Length && t < 3f)
                {
                    yield return null;
                    t += Time.deltaTime;
                    if (t < tras[n]) continue;
                    string nome = "46-diag-tras-" + (mirando ? "mirando-" : "solto-") + n;
                    Foto(main.Player.Camera.Cam, nome, true);
                    Vector3 q = eu.Pos;
                    Camera ct = CameraTemporaria("CamDiagTras", q + eu.transform.right * 4f + Vector3.up * 1.3f, q + Vector3.up * 1.0f, Color.gray);
                    Foto(ct, nome + "-lado", false);
                    Object.Destroy(ct.gameObject);
                    float yawCorpo = eu.transform.eulerAngles.y;
                    Vector3 v = eu.Loc.Vel;
                    float yawVel = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                    sb.AppendLine("  " + nome + " t=" + t.ToString("F2") + " clipe=" + eu.Clipe + " vel=" + eu.VelocidadeHorizontal.ToString("F2")
                        + " yawCorpo=" + yawCorpo.ToString("F0") + " yawVel=" + yawVel.ToString("F0") + " yawCam=" + (yaw * Mathf.Rad2Deg).ToString("F0")
                        + " dif=" + Mathf.DeltaAngle(yawCorpo, yawVel).ToString("F0"));
                    n++;
                }
                eu.Stick = Vector2.zero;
                yield return Esperar(0.8f);
            }
            main.Player.enabled = true;
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
        }


        /// <summary>
        /// A LUVA NA MAO (onda 15A): o soquete cancela a escala 100 do osso da Meshy e a luva da mao e' o .glb do chao.
        /// 47-luva-jogador: camera do jogador com HUD — a tela NAO pode estar coberta (mede a fracao da tela que a luva pega).
        /// 47-luva-perto: a 1,5 m da mao direita. 47-luva-disparo: perto, no meio do gesto de conjurar (braco a frente).
        /// 47-luva-manopla: perto, a manopla com as duas gemas acesas. Escala e bounds medidos vao para o diag.txt.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Luva_NaMao()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Gameplay.Pawn eu = main.Player.Pawn;
            Transform mao = eu.Visual.MaoDireita;
            var sb = new System.Text.StringBuilder("47-luva: fonte=" + eu.Visual.Fonte + " mao=" + Caminho(mao) + "\n");

            // 1) A LUVA COMUM pela camera do jogador: tela livre
            eu.Slot.Equipar(Gameplay.Arma.VARINHA);
            yield return Esperar(0.4f);
            float tela = DiagLuva(sb, "47-luva-jogador", mao, Gameplay.Arma.VARINHA, main.Player.Camera.Cam);
            Foto(main.Player.Camera.Cam, "47-luva-jogador", true);
            Diagnostico(main, "47-luva-jogador");

            // 2) DE PERTO, parado
            Camera cp = CameraDaMao(eu, mao, Gameplay.Arma.VARINHA);
            DiagLuva(sb, "47-luva-perto", mao, Gameplay.Arma.VARINHA, cp);
            Foto(cp, "47-luva-perto", false);
            Object.Destroy(cp.gameObject);

            // 3) NO MEIO DO GESTO: o braco chega a frente no CastFireT
            main.Player.DisparoRapido();
            float t = 0f;
            while (t < Arkana.Characters.PoseMago.CastFireT) { yield return null; t += Time.deltaTime; }
            cp = CameraDaMao(eu, mao, Gameplay.Arma.VARINHA);
            DiagLuva(sb, "47-luva-disparo", mao, Gameplay.Arma.VARINHA, cp);
            sb.AppendLine("    clipe=" + eu.Clipe + " visual=" + eu.Visual.ClipeAtual + " t=" + t.ToString("F2"));
            Foto(cp, "47-luva-disparo", false);
            Object.Destroy(cp.gameObject);
            yield return Esperar(1f);

            // 4) A MANOPLA, de perto: as duas gemas nas cores do par
            eu.Slot.Equipar(Gameplay.Arma.MANOPLA, new[] { Elemento.Fogo, Elemento.Vento });
            yield return Esperar(0.4f);
            cp = CameraDaMao(eu, mao, Gameplay.Arma.MANOPLA);
            DiagLuva(sb, "47-luva-manopla", mao, Gameplay.Arma.MANOPLA, cp);
            Foto(cp, "47-luva-manopla", false);
            Object.Destroy(cp.gameObject);

            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.Less(tela, 0.15f, "a luva cobre a tela do jogador\n" + sb);
        }

        /// <summary>Camera a 1,5 m da luva, do lado de fora e um pouco a frente do corpo, olhando o centro dela.</summary>
        static Camera CameraDaMao(Gameplay.Pawn eu, Transform mao, string arma)
        {
            Transform luva = mao.Find("Luva " + arma);
            Assert.IsNotNull(luva, "a luva " + arma + " nao esta' na mao");
            Vector3 alvo = LimitesDaLuva(luva).center;
            Vector3 dir = (eu.transform.right * 0.8f + eu.transform.forward * 1f + Vector3.up * 0.35f).normalized;
            return CameraTemporaria("CamLuva", alvo + dir * 1.5f, alvo, Color.gray);
        }

        static Bounds LimitesDaLuva(Transform luva)
        {
            Renderer[] rs = luva.GetComponentsInChildren<Renderer>();
            Assert.Greater(rs.Length, 0, "luva sem renderer");
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        /// <summary>Escala, bounds e a fracao da tela que a caixa da luva pega nesta camera (0..1). Devolve a fracao.</summary>
        static float DiagLuva(System.Text.StringBuilder sb, string nome, Transform mao, string arma, Camera cam)
        {
            Transform luva = mao.Find("Luva " + arma);
            Assert.IsNotNull(luva, "a luva " + arma + " nao esta' na mao");
            Bounds b = LimitesDaLuva(luva);
            float x0 = 1f, y0 = 1f, x1 = 0f, y1 = 0f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = new Vector3((i & 1) != 0 ? b.max.x : b.min.x, (i & 2) != 0 ? b.max.y : b.min.y, (i & 4) != 0 ? b.max.z : b.min.z);
                Vector3 v = cam.WorldToViewportPoint(c);
                if (v.z <= 0f) { x0 = y0 = 0f; x1 = y1 = 1f; break; }   // canto atras da camera: conta a tela toda
                x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y);
            }
            float tela = Mathf.Max(0f, Mathf.Min(x1, 1f) - Mathf.Max(x0, 0f)) * Mathf.Max(0f, Mathf.Min(y1, 1f) - Mathf.Max(y0, 0f));
            sb.AppendLine("  " + nome + ": maoLossy=" + mao.lossyScale.ToString("F3") + " soqueteLossy=" + luva.lossyScale.ToString("F4")
                + " rig=" + mao.GetComponentInParent<Arkana.Characters.Mago>().transform.Find("Rig").lossyScale.ToString("F4")
                + " luva tam=" + b.size.ToString("F3") + " centro=" + b.center.ToString("F2")
                + " centro-osso=" + Vector3.Distance(b.center, mao.position).ToString("F3")
                + " dist-camera=" + Vector3.Distance(b.center, cam.transform.position).ToString("F2")
                + " tela=" + (tela * 100f).ToString("F1") + "%"
                + " dedos(mundo)=" + luva.up.ToString("F2") + " dorso(mundo)=" + luva.forward.ToString("F2"));
            return tela;
        }


        /// <summary>
        /// O CORPO DA ONDA 15B (queixa do Diretor, 16/09: pulo em Idle, moonwalk mirando para tras, pernas congeladas atirando).
        /// 48-pulo-N: o pulo em fases visto de lado, quadro a quadro (decolagem, subida, topo, descida, pouso, aterrissagem, de pe').
        /// 48-tras-mirando-N (+ -lado): stick para tras mirando a frente — Walk_Backward, pernas contra o rumo, sem moonwalk.
        /// 48-lado-mirando-N (+ -lado): stick para a direita mirando a frente — pernas no rumo, tronco torcido para a mira.
        /// 48-atirando-correndo-N: de lado, correndo solto e disparando — o cast so' no tronco, as pernas correm.
        /// O Player e' desligado e a intencao vai direto no Pawn. A luva so' entra no tiro (a de antes tapava a camera de lado).
        /// O diag (Logs/fotos/diag.txt) diz o take, o tempo, a velocidade do estado, a torcao, as pernas e os angulos.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Corpo_PuloRecuoTiro()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Gameplay.Pawn eu = main.Player.Pawn;
            var sb = new System.Text.StringBuilder("48-corpo: fonte=" + eu.Visual.Fonte + " fases(decolagem,pouso)=" + eu.Visual.FasesDoPulo.ToString("F3")
                + " takePulo=" + eu.Visual.TemClipe("Regular_Jump") + " takeTras=" + eu.Visual.TemClipe("Walk_Backward") + "\n");
            main.Player.enabled = false;
            eu.Stick = Vector2.zero;
            eu.YawAlvo = null;
            eu.EncararDir = Vector3.zero;
            yield return Esperar(0.6f);

            // 1) O PULO, de lado
            Vector3 p = eu.Pos;
            Vector3 lado = eu.transform.right;
            Assert.IsTrue(eu.Pular(), "nao pulou");
            float t = 0f;
            int n = 0;
            bool idleNoAr = false, viuPulo = false, viuPouso = false;
            float[] marcas = { 0.05f, 0.25f, 0.5f, 0.8f, 1.02f, 1.2f, 1.6f };
            while (n < marcas.Length && t < 3f)
            {
                yield return null;
                t += Time.deltaTime;
                if (!eu.NoChao && eu.Clipe == "idle") idleNoAr = true;
                viuPulo |= eu.Clipe == "pular";
                viuPouso |= eu.Clipe == "pousar";
                if (t < marcas[n]) continue;
                Camera cp = CameraTemporaria("CamPulo15", p + lado * 5f + Vector3.up * 1.4f, p + Vector3.up * 1.2f, Color.gray);
                Foto(cp, "48-pulo-" + n, false);
                Object.Destroy(cp.gameObject);
                sb.AppendLine("  48-pulo-" + n + " t=" + t.ToString("F2") + " clipe=" + eu.Clipe + " " + eu.Visual.Estado()
                    + " vy=" + eu.Loc.Vy.ToString("F2") + " noChao=" + eu.NoChao + " y=" + (eu.Pos.y - p.y).ToString("F2")
                    + " noArS=" + eu.Loc.NoArS.ToString("F2") + " pousoS=" + eu.Loc.PousoS.ToString("F2"));
                n++;
            }
            yield return Esperar(0.6f);

            // 2) RECUANDO MIRANDO: stick para tras, mira a frente (camera do jogador + de lado, pela direita)
            var viu = new System.Collections.Generic.List<string>();
            yield return AndarCorpo15(main, eu, sb, viu, "48-tras-mirando", new Vector2(0f, -1f), true, false,
                new[] { 0.3f, 0.55f, 0.8f, 1.05f }, new Vector3(4f, 1.3f, 0f), true);
            bool recuou = viu.Contains("andar_tras");
            // 3) DE LADO MIRANDO: stick para a direita, mira a frente (camera do jogador + da frente-esquerda: pernas de perfil)
            viu.Clear();
            yield return AndarCorpo15(main, eu, sb, viu, "48-lado-mirando", new Vector2(1f, 0f), true, false,
                new[] { 0.3f, 0.6f, 0.9f }, new Vector3(-3f, 1.3f, 3f), true);
            bool strafeCorre = viu.Contains("run") && !viu.Contains("andar_tras");
            // 4) ATIRANDO CORRENDO (solto), de lado
            eu.Slot.Equipar(Gameplay.Arma.VARINHA);
            yield return Esperar(0.4f);
            viu.Clear();
            yield return AndarCorpo15(main, eu, sb, viu, "48-atirando-correndo", new Vector2(0f, 1f), false, true,
                new[] { 0.4f, 0.65f, 0.9f }, new Vector3(4f, 1.3f, 0f), false);
            bool castNoTronco = viu.Contains("castTronco");

            main.Player.enabled = true;
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.IsFalse(idleNoAr, "o pulo voltou a ficar em Idle no ar");
            Assert.IsTrue(viuPulo, "o pulo nao tocou 'pular'");
            Assert.IsTrue(viuPouso, "parado, o pulo nao aterrissou ('pousar')");
            Assert.IsTrue(recuou, "stick para tras mirando nao recuou (andar_tras)");
            Assert.IsTrue(strafeCorre, "stick de lado mirando tinha de ser corrida de lado, nao recuo");
            Assert.IsTrue(castNoTronco, "correndo, o cast nao foi para o tronco");
        }

        /// <summary>Anda `stick` (mirando a frente ou solto; atirando para a frente do corpo a cada 0,28 s) e fotografa nas
        /// `marcas`: a camera do jogador (se `comJogador`, como `nome-N`, e a de fora como `nome-N-lado`) ou so' a de fora
        /// (`nome-N`), posta em `olhoLocal` no referencial do corpo. `viu` junta os clipes (e "castTronco") das marcas.</summary>
        static IEnumerator AndarCorpo15(Main main, Gameplay.Pawn eu, System.Text.StringBuilder sb, System.Collections.Generic.List<string> viu,
            string nome, Vector2 stick, bool mirando, bool atirando, float[] marcas, Vector3 olhoLocal, bool comJogador)
        {
            float yaw = main.Player.Camera.Logica.Yaw;
            eu.YawCam = yaw;
            eu.YawAlvo = mirando ? yaw : (float?)null;
            eu.transform.rotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);
            eu.Stick = stick;
            float t = 0f, proximoTiro = 0f;
            int n = 0;
            while (n < marcas.Length && t < 3f)
            {
                yield return null;
                t += Time.deltaTime;
                if (atirando && t >= proximoTiro && eu.Atirar(eu.transform.forward)) proximoTiro = t + 0.28f;
                if (t < marcas[n]) continue;
                string foto = nome + "-" + n;
                if (comJogador) Foto(main.Player.Camera.Cam, foto, true);
                Vector3 q = eu.Pos;
                Camera cl = CameraTemporaria("CamCorpo15", q + eu.transform.TransformDirection(olhoLocal), q + Vector3.up * 1.0f, Color.gray);
                Foto(cl, comJogador ? foto + "-lado" : foto, false);
                Object.Destroy(cl.gameObject);
                float yawCorpo = eu.transform.eulerAngles.y;
                Vector3 v = eu.Loc.Vel;
                float yawVel = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                viu.Add(eu.Clipe);
                if (eu.Visual.CastNoTronco) viu.Add("castTronco");
                sb.AppendLine("  " + foto + " t=" + t.ToString("F2") + " clipe=" + eu.Clipe + " " + eu.Visual.Estado()
                    + " castNoTronco=" + eu.Visual.CastNoTronco + " vel=" + eu.VelocidadeHorizontal.ToString("F2")
                    + " yawCorpo=" + yawCorpo.ToString("F0") + " yawVel=" + yawVel.ToString("F0") + " dif=" + Mathf.DeltaAngle(yawCorpo, yawVel).ToString("F0")
                    + " pernas=" + eu.PernasYaw.ToString("F0") + " recuando=" + eu.Recuando);
                n++;
            }
            eu.Stick = Vector2.zero;
            eu.YawAlvo = null;
            yield return Esperar(0.8f);
        }


        /// <summary>
        /// DIAGNOSTICO DO LOGO (onda 14B saiu invisivel no titulo, no menu e no carregamento): um Canvas proprio com o Logo, uma
        /// RawImage com a MESMA textura do Logo e um Image de controle, lado a lado. 49-diag-logo.png + o estado do Graphic no diag.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Diag_Logo()
        {
            ExigirGpu();
            var cam = CameraTemporaria("CamDiagLogo", new Vector3(0f, 0f, -10f), Vector3.zero, Color.gray);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.3f, 0.4f);
            var cgo = new GameObject("CanvasDiagLogo", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sb = new System.Text.StringBuilder("49-diag-logo:\n");

            var lgo = new GameObject("LogoDiag", typeof(RectTransform), typeof(Arkana.Menu.Logo));
            lgo.transform.SetParent(cgo.transform, false);
            var logo = lgo.GetComponent<Arkana.Menu.Logo>();
            var lrt = (RectTransform)lgo.transform;
            lrt.anchoredPosition = new Vector2(-300f, 150f);
            lrt.sizeDelta = new Vector2(500f, 500f / Arkana.Menu.Logo.Aspecto);

            var rgo = new GameObject("RawDiag", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            rgo.transform.SetParent(cgo.transform, false);
            var raw = rgo.GetComponent<UnityEngine.UI.RawImage>();
            raw.texture = logo.mainTexture;
            var rrt = (RectTransform)rgo.transform;
            rrt.anchoredPosition = new Vector2(300f, 150f);
            rrt.sizeDelta = new Vector2(500f, 300f);

            var igo = new GameObject("ImagemControle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            igo.transform.SetParent(cgo.transform, false);
            igo.GetComponent<UnityEngine.UI.Image>().color = Color.magenta;
            var irt = (RectTransform)igo.transform;
            irt.anchoredPosition = new Vector2(0f, -250f);
            irt.sizeDelta = new Vector2(200f, 60f);

            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            Texture t = logo.mainTexture;
            var cr = logo.canvasRenderer;
            Material crm = cr.materialCount > 0 ? cr.GetMaterial() : null;
            sb.AppendLine("  logo ativo=" + logo.IsActive() + " enabled=" + logo.enabled + " rect=" + lrt.rect + " cor=" + logo.color
                + " tex=" + (t != null ? t.name + " " + t.width + "x" + t.height : "null")
                + " material=" + (logo.materialForRendering != null ? logo.materialForRendering.name + " / " + logo.materialForRendering.shader.name : "null")
                + " cr.material=" + (crm != null ? crm.name : "nenhum") + " cr.materiais=" + cr.materialCount
                + " cr.textura=" + (crm != null && crm.mainTexture != null ? crm.mainTexture.name : "?")
                + " cr.alpha=" + cr.GetAlpha() + " cr.cull=" + cr.cull + " cr.hasMoved=" + cr.hasMoved + " depth=" + logo.depth
                + " absoluteDepth=" + cr.absoluteDepth);
            var v = new System.Collections.Generic.List<UIVertex>();
            var ti = new System.Collections.Generic.List<int>();
            Arkana.Menu.Logo.Preencher(v, ti, logo.GetPixelAdjustedRect(), logo.color, -1f);
            sb.Append("  malha: vertices=" + v.Count + " indices=" + ti.Count);
            foreach (UIVertex u in v) sb.Append(" [" + u.position.ToString("F0") + " uv" + u.uv0.ToString("F2") + " a" + u.color.a + "]");
            sb.AppendLine();
            var tex2 = t as Texture2D;
            if (tex2 != null) sb.AppendLine("  textura legivel=" + tex2.isReadable + " formato=" + tex2.format + " mips=" + tex2.mipmapCount + " hide=" + tex2.hideFlags);
            sb.AppendLine("  raw ativo=" + raw.IsActive() + " rect=" + rrt.rect + " tex=" + (raw.texture != null ? raw.texture.name : "null"));

            Foto(cam, "49-diag-logo", true);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Object.Destroy(cgo);
            Object.Destroy(cam.gameObject);
        }

        // ONDA 14B — colar em FotoTests.cs, dentro da classe (o onda14-logo-compila.sh cola no fim). Rodar: .\foto.ps1 "Foto_Logo"

        /// <summary>
        /// A MARCA NOVA (onda 14B): ARKANA com letras proprias desenhadas em codigo (Logo) — ouro em degrade com bisel,
        /// contorno escuro grosso, espessura em bronze, sombra macia, o A em ponta de lanca com a barra em losango e a perna do
        /// K em raio amarelo com halo azul-eletrico. 45-logo-titulo: a tela de titulo (o selo, a marca de 300 dp, o subtitulo)
        /// com o reflexo congelado sobre o A do meio e a cintilancia na ponta dele; 45-logo-menu: o menu com a marca de 200 dp
        /// sobre os botoes, parada. Guarda o contrato: a marca velha (uma letra por Text) saiu, as duas marcas sao Logo na
        /// proporcao do desenho, e a do menu fica acima do JOGAR.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Logo_TituloEMenu()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return Esperar(1.2f);   // a vitrine poe o mago no pico (o mesmo fundo do 01-menu)
            var marcas = Object.FindObjectsByType<Arkana.Menu.Logo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(2, marcas.Length, "titulo e menu tem a marca desenhada (o menu nasce apagado)");
            Assert.IsNull(GameObject.Find("LK"), "a marca velha (uma letra por Text) saiu");
            foreach (Arkana.Menu.Logo m in marcas)
            {
                Rect r = m.rectTransform.rect;
                Assert.AreEqual(Arkana.Menu.Logo.Aspecto, r.width / r.height, 0.02f, "a caixa da marca na proporcao do desenho");
                m.Fase = m.transform.parent.parent.name == "Titulo" ? 0.56f : 1f;   // titulo: o reflexo sobre o A do meio; menu: parado
            }
            yield return null;
            Foto(main.CameraDoMenu, "45-logo-titulo", true);

            Tocar("TapTitulo");
            yield return null;
            GameObject marcaMenu = GameObject.Find("MenuPrincipal/Centro/Wordmark");
            Assert.IsNotNull(marcaMenu, "o menu mostra a marca");
            Assert.Greater(marcaMenu.transform.position.y, GameObject.Find("BtnJogar").transform.position.y, "a marca acima do JOGAR");
            Foto(main.CameraDoMenu, "45-logo-menu", true);
        }


        /// <summary>
        /// TIRO POR TOQUE com o Player LIGADO (a 48-atirando desliga o Player): a camera olha 120 graus para o lado do corpo e o
        /// jogador atira sem segurar a mira. Antes da onda 15 o Player largava a mira no quadro seguinte e o corpo nunca virava
        /// para o reticulo; agora ele segura MIRA_APOS_TIRO_S e o corpo vira. 52-tiro-toque-0/1 (de lado) + os yaws no diag.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Diag_TiroPorToque()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Gameplay.Pawn eu = main.Player.Pawn;
            eu.Slot.Equipar(Gameplay.Arma.VARINHA);
            yield return Esperar(0.5f);
            float yawCorpo0 = eu.transform.eulerAngles.y;
            main.Player.Camera.Logica.Yaw = (yawCorpo0 + 120f) * Mathf.Deg2Rad;
            main.Player.Camera.Logica.Pitch = CameraLogica_PitchPadrao();
            yield return null;
            Vector3 p = eu.Pos;
            Vector3 lado = eu.transform.right;
            var sb = new System.Text.StringBuilder("52-tiro-toque: yawCorpo0=" + yawCorpo0.ToString("F0") + " yawCam=" + (main.Player.Camera.Logica.Yaw * Mathf.Rad2Deg).ToString("F0") + "\n");
            main.Player.DisparoRapido();
            float t = 0f;
            int n = 0;
            float[] marcas = { 0.15f, 0.45f };
            while (n < marcas.Length && t < 2f)
            {
                yield return null;
                t += Time.deltaTime;
                if (t < marcas[n]) continue;
                Camera c = CameraTemporaria("CamDiagTiro", p + lado * 4f + Vector3.up * 1.4f, p + Vector3.up * 1.1f, Color.gray);
                Foto(c, "52-tiro-toque-" + n, false);
                Object.Destroy(c.gameObject);
                float yawCorpo = eu.transform.eulerAngles.y, yawCam = main.Player.Camera.Logica.Yaw * Mathf.Rad2Deg;
                sb.AppendLine("  52-tiro-toque-" + n + " t=" + t.ToString("F2") + " clipe=" + eu.Clipe + " yawCorpo=" + yawCorpo.ToString("F0")
                    + " yawCam=" + yawCam.ToString("F0") + " dif=" + Mathf.DeltaAngle(yawCorpo, yawCam).ToString("F0") + " yawAlvo=" + (eu.YawAlvo.HasValue ? "sim" : "nao"));
                n++;
            }
            float difFinal = Mathf.Abs(Mathf.DeltaAngle(eu.transform.eulerAngles.y, main.Player.Camera.Logica.Yaw * Mathf.Rad2Deg));
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.Less(difFinal, 30f, "o corpo nao virou para o reticulo depois do tiro por toque (a mira foi largada cedo)");
        }

        // ONDA 16A — colar em FotoTests.cs, no FIM da classe (depois do Foto_Logo_TituloEMenu). Rodar: .\foto.ps1 "Foto_Botoes"

        /// <summary>
        /// OS BOTOES DO MENU (onda 16A): a foto 45 lia "placeholder" — retangulo chapado cor de oliva (a borda dourada
        /// era FILHA e o uGUI desenha filho por cima do pai, cobrindo a placa). Agora cada botao e' a PLACA assada em
        /// codigo: cantos chanfrados, contorno escuro, moldura dourada em degrade, fio interno, miolo escuro
        /// translucido com luz em cima, gema de losango nas duas laterais e o rotulo em ouro claro espacado com
        /// contorno. 50-menu-botoes: o menu inteiro (o JOGAR maior, em ouro cheio com letra escura e a aura respirando;
        /// os quatro secundarios escuros). 50-menu-pressionado: o JOGAR com o dedo em cima (escurece e afunda).
        /// 50-config: a tela secundaria que usa o MESMO botao (VOLTAR, as opcoes e o RESTAURAR PADRAO).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Botoes_MenuEConfig()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return Esperar(1.2f);   // a vitrine poe o mago no pico (o mesmo fundo do 45-logo-menu)
            Tocar("TapTitulo");
            yield return null;
            GameObject jogar = GameObject.Find("BtnJogar"), sair = GameObject.Find("BtnSair");
            Assert.IsNotNull(jogar, "o menu abriu sem o JOGAR");
            Assert.IsNotNull(sair, "o menu abriu sem o SAIR");
            var placa = jogar.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(placa.sprite, "a placa e' um sprite assado, nao um retangulo chapado");
            Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced, placa.type, "a placa e' 9-slice");
            Assert.IsNull(jogar.transform.Find("Borda"), "a borda-filha que cobria a placa (a oliva da foto 45) saiu");
            Assert.IsNotNull(jogar.transform.Find("GemaEsq"), "a gema de losango na lateral");
            Assert.Greater(((RectTransform)jogar.transform).rect.width, ((RectTransform)sair.transform).rect.width, "o JOGAR e' o principal: maior");
            Assert.IsNotNull(jogar.transform.Find("Aura"), "o principal respira");
            Assert.IsNull(sair.transform.Find("Aura"), "os secundarios nao");
            yield return null;
            Foto(main.CameraDoMenu, "50-menu-botoes", true);

            // PRESSIONADO pelo caminho do dedo: o Button escurece a placa (fadeDuration 0) e o BotaoMenu afunda
            var toque = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            UnityEngine.EventSystems.ExecuteEvents.Execute(jogar, toque, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            yield return null;
            Assert.Less(jogar.transform.localScale.x, 1f, "o JOGAR afunda no toque");
            Assert.Less(placa.canvasRenderer.GetColor().r, 0.9f, "e escurece");
            Foto(main.CameraDoMenu, "50-menu-pressionado", true);
            UnityEngine.EventSystems.ExecuteEvents.Execute(jogar, toque, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.AreEqual(1f, jogar.transform.localScale.x, 1e-3f, "e volta ao soltar");

            // a tela secundaria: o MESMO Estilo.Botao no VOLTAR, nas opcoes e no RESTAURAR PADRAO
            Tocar("BtnConfig");
            yield return null;
            Assert.IsNotNull(GameObject.Find("BtnVoltarConfig"), "as Configuracoes abriram");
            Foto(main.CameraDoMenu, "50-config", true);
        }


        /// <summary>
        /// O CHAO DA MATA (onda 14A). 51-mata-chao: a camera a 1,6 m dentro da mata, a 5 m do tronco caido mais acompanhado
        /// (cogumelos e samambaias a menos de 6 m dele), do lado em que o sol fica mais nas costas do fotografo. 51-mata-cristal: o
        /// cristal mais perto do miolo da mata, a 4 m, visto do lado da sombra propria (a camera olha contra a luz: o brilho que se
        /// ve' e' a emissao). 51-mata-alto: a mata a ~50 m de altura, do lado de dentro da ilha. O jogador vai para perto de cada
        /// camera (a grama e o kit cortam pela camera DELE); o LOD e o corte da mata, pela camera da foto (Vegetacao.Olho).
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Mata_ChaoCristalEAlto()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2.5f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            var ilha = Arkana.World.Ilha.Atual;
            Assert.IsNotNull(ilha, "sem ilha");
            Arkana.World.Vegetacao veg = ilha.Vegetacao;
            Assert.IsNotNull(veg, "sem vegetacao");
            Arkana.World.Mata mata = veg.Mata;
            Assert.IsNotNull(mata, "a vegetacao nao montou a mata");
            Arkana.World.Relevo r = ilha.Relevo;
            var sb = new System.Text.StringBuilder("51-mata: moldes=" + mata.Moldes + " cristais=" + mata.Contar(Arkana.World.Mata.Peca.Cristal)
                + " troncos=" + mata.Contar(Arkana.World.Mata.Peca.Tronco) + " cogumelos=" + mata.Contar(Arkana.World.Mata.Peca.Cogumelo)
                + " samambaias=" + mata.Contar(Arkana.World.Mata.Peca.Samambaia) + "\n");
            // o KIT real (monta depois, a mata foge dele por PegadasDoKit, uma copia do sorteio): quem encosta em tronco ou cristal
            if (ilha.Kit != null)
                foreach (Transform k in ilha.Kit.GetComponentsInChildren<Transform>())
                {
                    if (k.parent != ilha.Kit.transform) continue;
                    Arkana.World.PecaDoKit pk = System.Array.Find(Arkana.World.PlantioDoKit.Pecas, x => x.Id == k.name);
                    if (pk == null) continue;
                    for (int i = 0; i < mata.Total; i++)
                    {
                        Arkana.World.Mata.PecaPlantada q = mata.Plantada(i);
                        Vector3 w = (Vector3)q.M.GetColumn(3) - k.position;
                        w.y = 0f;
                        if (q.Colide && w.magnitude < pk.RaioM * 0.8f)
                            sb.AppendLine("  KIT x MATA: " + k.name + " em " + k.position.ToString("F0") + " encosta no " + q.Tipo + " a " + w.magnitude.ToString("F1") + " m");
                    }
                }
            var sol = Object.FindFirstObjectByType<Arkana.World.Sol>();
            Vector3 l = sol != null ? sol.transform.forward : new Vector3(0.6f, -0.5f, 0.6f);
            l.y = 0f;
            l.Normalize();

            // 1) O CHAO: o tronco caido com mais cogumelos (valem 3) e samambaias a menos de 6 m; a camera a 5 m do lado dele
            //    (o eixo do tronco e' o X do molde; o lado, o Z), no lado com o sol mais nas costas, a 1,6 m, olhando o meio dele
            int it = -1, melhor = -1;
            for (int i = 0; i < mata.Total; i++)
            {
                Arkana.World.Mata.PecaPlantada p = mata.Plantada(i);
                if (p.Tipo != Arkana.World.Mata.Peca.Tronco) continue;
                Vector3 c = p.M.GetColumn(3);
                int n = 0;
                for (int j = 0; j < mata.Total; j++)
                {
                    Arkana.World.Mata.PecaPlantada q = mata.Plantada(j);
                    if (q.Tipo == Arkana.World.Mata.Peca.Tronco || q.Tipo == Arkana.World.Mata.Peca.Cristal) continue;
                    Vector3 w = (Vector3)q.M.GetColumn(3) - c;
                    w.y = 0f;
                    if (w.magnitude < 6f) n += q.Tipo == Arkana.World.Mata.Peca.Cogumelo ? 3 : 1;
                }
                if (n > melhor) { melhor = n; it = i; }
            }
            Assert.GreaterOrEqual(it, 0, "nenhum tronco caido (falta o 51-tronco-musgo.glb em Resources?)");
            Arkana.World.Mata.PecaPlantada tronco = mata.Plantada(it);
            Vector3 t = tronco.M.GetColumn(3);
            Vector3 lado = tronco.M.GetColumn(2);
            lado.y = 0f;
            lado.Normalize();
            if (Vector3.Dot(lado, l) > 0f) lado = -lado;   // a camera fica de onde a luz vem: sol nas costas
            Vector3 olho = t + lado * 5f;
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 1.6f;
            LevarJogador(main, olho + lado * 4f);   // o corpo 4 m atras da camera
            yield return Esperar(1.2f);
            Camera cam = CameraTemporaria("CamFotoMataChao", olho, t + Vector3.up * 0.5f, Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "51-mata-chao", false);
            sb.AppendLine("  51-mata-chao: tronco em " + t.ToString("F1") + " vizinhos=" + melhor + " cam=" + olho.ToString("F1")
                + " sol-nas-costas=" + Vector3.Dot(-lado, l).ToString("F2") + " lotes=" + mata.LotesEnviados + " tris-enviados=" + mata.TrisEnviados
                + " (arvores: lotes=" + veg.LotesEnviados + " tris=" + veg.TrisEnviados + ")");
            foreach (Collider k in Physics.OverlapSphere(t, 2f)) sb.AppendLine("    colisor no tronco: " + Caminho(k.transform) + " (" + k.GetType().Name + ")");
            Object.Destroy(cam.gameObject);

            // 2) O CRISTAL: o mais perto do miolo da mata (a sombra mais fechada), a 4 m, a 1,4 m do chao, visto do lado em que a
            //    luz NAO bate (a camera olha contra a luz): o que brilha e' a emissao, nao o sol
            int ic = -1;
            float dc = float.MaxValue;
            for (int i = 0; i < mata.Total; i++)
            {
                Arkana.World.Mata.PecaPlantada p = mata.Plantada(i);
                if (p.Tipo != Arkana.World.Mata.Peca.Cristal) continue;
                Vector3 c = p.M.GetColumn(3);
                float d = Vector2.Distance(new Vector2(c.x, c.z), r.Floresta);
                if (d < dc) { dc = d; ic = i; }
            }
            Assert.GreaterOrEqual(ic, 0, "nenhum cristal (falta o 50-cristal-arcano.glb em Resources?)");
            Arkana.World.Mata.PecaPlantada cristal = mata.Plantada(ic);
            Vector3 cr = cristal.M.GetColumn(3);
            olho = cr + l * 4f;
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 1.4f;
            LevarJogador(main, olho + l * 4f);
            yield return Esperar(1.2f);
            cam = CameraTemporaria("CamFotoMataCristal", olho, cr + Vector3.up * (0.6f * cristal.Escala), Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "51-mata-cristal", false);
            sb.AppendLine("  51-mata-cristal: cristal em " + cr.ToString("F1") + " escala=" + cristal.Escala.ToString("F2") + " a " + dc.ToString("F0")
                + " m do miolo, cam=" + olho.ToString("F1") + " lotes=" + mata.LotesEnviados + " tris-enviados=" + mata.TrisEnviados);
            foreach (Collider k in Physics.OverlapSphere(cr, 1f)) sb.AppendLine("    colisor no cristal: " + Caminho(k.transform) + " (" + k.GetType().Name + ")");
            Object.Destroy(cam.gameObject);

            // 3) DO ALTO: 50 m sobre a mata, 35 m para o lado de dentro da ilha, olhando o miolo (a mata atravessa o quadro)
            var miolo = new Vector3(r.Floresta.x, Arkana.World.Ilha.AlturaDoChao(r.Floresta.x, r.Floresta.y), r.Floresta.y);
            Vector3 dentro = -new Vector3(r.Floresta.x, 0f, r.Floresta.y).normalized;
            olho = miolo + dentro * 35f;
            olho.y = Arkana.World.Ilha.AlturaDoChao(olho.x, olho.z) + 50f;
            LevarJogador(main, miolo + dentro * 12f);
            yield return Esperar(1.2f);
            cam = CameraTemporaria("CamFotoMataAlto", olho, miolo, Color.gray);
            veg.Olho = cam;
            yield return null;
            Foto(cam, "51-mata-alto", false);
            sb.AppendLine("  51-mata-alto: cam=" + olho.ToString("F1") + " lotes=" + mata.LotesEnviados + " tris-enviados=" + mata.TrisEnviados
                + " (arvores: lotes=" + veg.LotesEnviados + " tris=" + veg.TrisEnviados + ")");
            Object.Destroy(cam.gameObject);
            veg.Olho = null;
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.Greater(mata.Contar(Arkana.World.Mata.Peca.Cogumelo), 0, "o cogumelo da Meshy nao nasceu (falta o 52-cogumelos.glb?)");
            Assert.Greater(mata.Contar(Arkana.World.Mata.Peca.Samambaia), 0, "a samambaia da Meshy nao nasceu (falta o 53-samambaia.glb?)");
        }



        /// <summary>
        /// SINTONIA NO MUNDO (onda 17C): os 10 combos disparados no treino, numa FOLHA 5x2 (53-sintonia-combos: um quadro por
        /// combo, na ordem do enum, a mesma camera de lado e do alto) + a CANALIZACAO pela camera do jogador (fio nas duas cores
        /// do player ao parceiro passando pelo ponto, anel enchendo) + o FALHOU (anel rachando). A dupla e' o player e o Boneco1
        /// (mesmo time); o alvo e' o Boneco2. Chama SintoniaEfeitos.Disparar e os eventos do Bus DIRETO: QUANDO fundir e' da
        /// Core.Sintonia (teste proprio); aqui se julga a FORMA de cada combo no quadro — tem de se reconhecer de longe.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Sintonia_OsDezCombos()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Assert.GreaterOrEqual(main.Partida.Bonecos.Count, 2, "o treino tem os dois bonecos");

            void Levar(Gameplay.Pawn corpo, Vector3 p)
            {
                var cc = corpo.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                corpo.transform.position = new Vector3(p.x, Arkana.World.Ilha.AlturaDoChao(p.x, p.z) + 0.05f, p.z);
                if (cc != null) cc.enabled = true;
            }

            Gameplay.Pawn eu = main.Player.Pawn;
            var parceiro = (Gameplay.Pawn)main.Partida.Bonecos[0];
            var alvo = (Gameplay.Pawn)main.Partida.Bonecos[1];
            Combat.DefinirTime(eu, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(parceiro, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(alvo, 1);
            var vis = Object.FindFirstObjectByType<Gameplay.VisualDaSintonia>();
            bool criado = vis == null;   // a Main da 17D cria; sem ela, a foto cria o seu
            if (criado) vis = Gameplay.VisualDaSintonia.Criar(null, main.Partida);

            // o ponto do combo a 14 m a frente do jogador; o parceiro a direita, os dois de frente para o alvo
            Vector3 frente = alvo.Pos - eu.Pos; frente.y = 0f; frente.Normalize();
            Vector3 lado = new Vector3(frente.z, 0f, -frente.x);
            Vector3 ponto = eu.Pos + frente * 14f;
            ponto.y = Arkana.World.Ilha.AlturaDoChao(ponto.x, ponto.z);
            Levar(parceiro, eu.Pos + frente * 3f + lado * 6f);
            Levar(alvo, ponto);
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(frente.x, frente.z);
            Arkana.Terrain.TerrenoReativo terreno = Arkana.Terrain.TerrenoReativoBehaviour.Atual != null ? Arkana.Terrain.TerrenoReativoBehaviour.Atual.Terreno : null;
            yield return Esperar(0.4f);

            // 53-sintonia-canalizacao: a camera DO JOGADOR (com a HUD) no meio da canalizacao
            Bus.EmitSintoniaCanalizando(ComboSintonia.ExplosaoDePlasma, eu, parceiro, ponto, Balance.Sintonia.CanalizacaoS);
            yield return Esperar(Balance.Sintonia.CanalizacaoS * 0.6f);
            Foto(main.Player.Camera.Cam, "53-sintonia-canalizacao", true);
            Bus.EmitSintoniaFalhou(ComboSintonia.ExplosaoDePlasma, eu, parceiro, ponto);
            yield return Esperar(0.2f);
            Foto(main.Player.Camera.Cam, "53-sintonia-falhou", true);
            yield return Esperar(0.6f);

            // a FOLHA: a mesma camera de lado e do alto (a area inteira do maior combo, 12 m, cabe no quadro)
            Camera cam = CameraTemporaria("CamFotoSintonia", ponto - frente * 16f - lado * 9f + Vector3.up * 11f, ponto + Vector3.up * 1.5f, Color.gray);
            const int cw = 640, ch = 400, colunas = 5;
            var combos = (ComboSintonia[])System.Enum.GetValues(typeof(ComboSintonia));
            int linhas = (combos.Length + colunas - 1) / colunas;
            var folha = new Texture2D(cw * colunas, ch * linhas, TextureFormat.RGB24, false);
            var rt = new RenderTexture(cw, ch, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var sb = new System.Text.StringBuilder("53-sintonia-combos:");
            for (int i = 0; i < combos.Length; i++)
            {
                ComboSintonia c = combos[i];
                Gameplay.SintoniaEfeitos.Reset();
                if (terreno != null) terreno.Reset();   // o fogo/lama do quadro anterior nao suja o proximo
                Levar(alvo, ponto);
                yield return Esperar(1.2f);             // o estouro anterior termina de cair
                Elemento x, y;
                Gameplay.SintoniaEfeitos.Par(c, out x, out y);
                var d = new DisparoSintonia { Combo = c, A = eu, B = parceiro, ElA = x, ElB = y, Ponto = ponto, Alvo = alvo, Dano = 20f };
                Gameplay.SintoniaEfeitos.Disparar(d);
                Bus.EmitSintoniaDisparou(d);
                // cada forma no seu auge: o plasma estufando, o raio da nuvem aceso, o funil e a parede de areia ja' cheios
                float auge;
                switch (c)
                {
                    case ComboSintonia.ExplosaoDePlasma: auge = 0.18f; break;
                    case ComboSintonia.NuvemTempestuosa: auge = 0.12f; break;
                    case ComboSintonia.Eletrocussao: auge = 0.3f; break;
                    case ComboSintonia.ChuvaDeMagma: auge = 0.35f; break;
                    case ComboSintonia.TempestadeTorrencial: auge = 0.5f; break;
                    case ComboSintonia.CristaisCarregados: auge = 0.5f; break;
                    case ComboSintonia.TempestadeDeAreia: auge = 1.6f; break;
                    default: auge = 1.1f; break;
                }
                yield return Esperar(auge);
                cam.Render();
                RenderTexture.active = rt;
                folha.ReadPixels(new Rect(0, 0, cw, ch), (i % colunas) * cw, (linhas - 1 - i / colunas) * ch);
                RenderTexture.active = null;
                int particulas = 0;
                foreach (var ps in vis.GetComponentsInChildren<ParticleSystem>()) particulas += ps.particleCount;
                sb.Append(" " + c + "(ativos=" + Gameplay.SintoniaEfeitos.Ativos.Count + ",particulas=" + particulas + ")");
                Assert.Greater(particulas + Gameplay.SintoniaEfeitos.Ativos.Count, 0, c + ": o combo aparece");
            }
            folha.Apply();
            Directory.CreateDirectory(Pasta);
            File.WriteAllBytes(Path.Combine(Pasta, "53-sintonia-combos.png"), folha.EncodeToPNG());
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString() + "\n");
            Gameplay.SintoniaEfeitos.Reset();
            cam.targetTexture = null;
            Object.Destroy(folha);
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(cam.gameObject);
            if (criado) Object.Destroy(vis.gameObject);
        }

        // ---- COLAR em FotoTests.cs dentro da classe (antes do ultimo `}` dela). Onda 17D (Dupla em jogo).

        /// <summary>
        /// A DUPLA EM JOGO (onda 17D). 54-dupla-menu: o seletor MODO: DUPLA | SOLO colado a' esquerda do JOGAR (DUPLA aceso).
        /// 54-dupla-parceiro: a partida NORMAL em dupla, os dois no chao — o parceiro a' frente com a marca AZUL (nome + vida),
        /// DUPLAS 7 no topo, o ponto azul no minimapa, o anel da Sintonia em volta do ataque e o SINTONIA PRONTA (a borda
        /// forcada pela logica). 54-dupla-sintonia: a faixa da canalizacao (SINTONIA + TORNADO FLAMEJANTE nas duas cores, o
        /// trilho na metade), disparada DIRETO pelo Bus (a regra da 17B nao e' o assunto da foto). 54-dupla-quebrada: o falhou.
        /// 54-dupla-espectador: o jogador eliminado (derrubado e finalizado) com o time vivo — a camera no parceiro e a placa
        /// ESPECTANDO · nome, sem os controles do corpo. O parceiro fica PARADO (bot desligado): a foto e' da leitura, nao da IA.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Dupla_ParceiroESintonia()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return Esperar(1.2f);   // a vitrine poe o mago no pico
            Arkana.Menu.Menu.ModoDupla = true;   // o padrao, sem depender do PlayerPrefs do editor (a foto nao grava)
            Tocar("TapTitulo");
            yield return null;
            Assert.IsNotNull(GameObject.Find("BtnModoDupla"), "o menu tem o seletor MODO (DUPLA)");
            Assert.IsNotNull(GameObject.Find("BtnModoSolo"), "e o SOLO");
            Foto(main.CameraDoMenu, "54-dupla-menu", true);

            Bus.EmitGameStartRequested();
            for (int n = 0; main.Carregando && n < 2000; n++) yield return null;
            yield return Esperar(1f);   // a tela de carregamento esvaece
            Assert.IsTrue(main.Dupla, "JOGAR com o modo DUPLA monta a partida em dupla");
            Assert.IsNotNull(main.Parceiro, "o parceiro bot nasceu");
            Assert.AreEqual(14, main.Partida.Arena.Count, "jogador + parceiro + 6 duplas");
            Assert.AreEqual(7, main.Partida.TimesVivos, "7 times");
            Assert.IsTrue(Combat.MesmoTime(main.Player.Pawn, main.Parceiro.Pawn), "o parceiro e' do time do jogador");

            // os dois no chao, lado a lado: saem do castelo pela porta (a Queda so' aceita posicao la') e pousam no ato
            Gameplay.Pawn eu = main.Player.Pawn, par = main.Parceiro.Pawn;
            main.Parceiro.enabled = false;
            Vector3 n0 = Arkana.World.Ilha.Atual.Relevo.Nascimentos[0];
            main.Castelo.Saltar(eu.gameObject);
            eu.Aterrar(new Vector3(n0.x, -999f, n0.z));
            yield return null;
            OlharParaOCentro(main);
            float yaw = main.Player.Camera.Logica.Yaw;
            Vector3 frente = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)), lado = new Vector3(frente.z, 0f, -frente.x);
            main.Castelo.Saltar(par.gameObject);
            Vector3 pp = eu.Pos + frente * 6f + lado * 2.5f;
            par.Aterrar(new Vector3(pp.x, -999f, pp.z));
            main.Hud.Dupla.Logica.Recarregar(12f, Balance.Sintonia.CooldownS);   // a BORDA: no proximo quadro a recarga real (0) acende o PRONTA
            yield return Esperar(1.2f);
            Foto(main.Player.Camera.Cam, "54-dupla-parceiro", true);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("54-dupla-parceiro: duplas=" + main.Hud.Duplas + " parceiro=" + par.Nome + " dist=" + Vector3.Distance(eu.Pos, par.Pos).ToString("F1")
                + " marca=" + (main.Hud.Marcas.Logica.Parceiro == (IEntidade)par) + " pronta=" + main.Hud.Dupla.Logica.ProntaAlfa.ToString("F2")
                + " recarga=" + main.Hud.Dupla.Logica.Recarga.ToString("F2"));
            Assert.AreEqual(7, main.Hud.Duplas, "o topo conta DUPLAS");
            Assert.AreSame(par, main.Hud.Marcas.Logica.Parceiro, "o parceiro tem a marca azul");

            // a FAIXA DA SINTONIA: canalizando (o trilho enche em CanalizacaoS), depois o falhou
            Vector3 ponto = eu.Pos + frente * 10f;
            Bus.EmitSintoniaCanalizando(ComboSintonia.TornadoFlamejante, eu, par, ponto, Balance.Sintonia.CanalizacaoS);
            yield return Esperar(Balance.Sintonia.CanalizacaoS * 0.5f);
            Foto(main.Player.Camera.Cam, "54-dupla-sintonia", true);
            sb.AppendLine("54-dupla-sintonia: estado=" + main.Hud.Dupla.Logica.Estado + " progresso=" + main.Hud.Dupla.Logica.Progresso.ToString("F2")
                + " alfa=" + main.Hud.Dupla.Logica.Alfa.ToString("F2"));
            Assert.AreEqual(Arkana.UI.DuplaHudLogica.Faixa.Canalizando, main.Hud.Dupla.Logica.Estado, "a faixa acende na canalizacao da dupla");
            Bus.EmitSintoniaFalhou(ComboSintonia.TornadoFlamejante, eu, par, ponto);
            yield return Esperar(0.3f);
            Foto(main.Player.Camera.Cam, "54-dupla-quebrada", true);
            Assert.AreEqual(Arkana.UI.DuplaHudLogica.Faixa.Quebrada, main.Hud.Dupla.Logica.Estado, "o falhou vira SINTONIA QUEBRADA");
            yield return Esperar(Arkana.UI.DuplaHudLogica.QuebradaS);

            // o ESPECTADOR: com o parceiro de pe' o jogador CAI; o segundo golpe o finaliza — o time segue vivo
            Combat.AplicarDano(eu, 9999f, Elemento.Terra, null);
            Combat.AplicarDano(eu, 9999f, Elemento.Terra, null);
            yield return Esperar(1.5f);   // a camera assenta nas costas do parceiro
            Foto(main.Player.Camera.Cam, "54-dupla-espectador", true);
            sb.AppendLine("54-dupla-espectador: vivo=" + eu.Viva + " fora=" + main.Partida.PlayerFora + " seguindo=" + (main.Player.Camera.Seguindo != null ? main.Player.Camera.Seguindo.Nome : "-")
                + " fluxo=" + main.Fluxo.Atual);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.IsFalse(eu.Viva, "o jogador foi eliminado");
            Assert.IsTrue(main.Partida.PlayerFora, "com o parceiro vivo, o jogador fica de FORA (nao e' derrota)");
            Assert.AreEqual(FluxoDeJogo.Estado.Partida, main.Fluxo.Atual, "a partida segue");
            Assert.AreSame(par, main.Player.Camera.Seguindo, "a camera segue o parceiro");
        }


        // ---- COLAR em FotoTests.cs dentro da classe (antes do ultimo `}` dela). Onda 18A (acabamento visual da Sintonia).

        /// <summary>
        /// ACABAMENTO DA SINTONIA (onda 18A). 55-sintonia-combos: a folha 5x2 dos 10 combos (ordem do enum) agora com o alvo numa
        /// LADEIRA — o ponto seco de maior desnivel perto do treino, sem penhasco — para provar que a zona SEGUE o relevo (nada de
        /// meia pizza, nada de arco branco enterrado), cada quadro no MOMENTO DE MAIOR LEITURA do combo, camera de 3/4 vinda de
        /// baixo do morro. 55-sintonia-perto: a poca de magma com os meteoros caindo, pela camera do JOGADOR (com a HUD).
        /// 55-sintonia-canalizacao / 55-sintonia-falhou: o aviso deitado na ladeira enchendo e depois rachando. A dupla e' o
        /// player e o Boneco1 (mesmo time), o alvo o Boneco2; SintoniaEfeitos.Disparar + Bus direto (QUANDO fundir e' da
        /// Core.Sintonia, com teste proprio; aqui se julga a FORMA). Entre quadros tudo e' zerado (o vapor de 5 s nao suja o
        /// proximo). diag.txt: a ladeira escolhida, o desnivel que a malha da zona venceu e as particulas de cada quadro.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Sintonia_Acabamento()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            yield return Esperar(2f);
            Assert.IsNotNull(main.Player, "treino sem jogador");
            Assert.GreaterOrEqual(main.Partida.Bonecos.Count, 2, "o treino tem os dois bonecos");

            void Levar(Gameplay.Pawn corpo, Vector3 p)
            {
                var cc = corpo.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                corpo.transform.position = new Vector3(p.x, Arkana.World.Ilha.AlturaDoChao(p.x, p.z) + 0.05f, p.z);
                if (cc != null) cc.enabled = true;
            }

            Gameplay.Pawn eu = main.Player.Pawn;
            var parceiro = (Gameplay.Pawn)main.Partida.Bonecos[0];
            var alvo = (Gameplay.Pawn)main.Partida.Bonecos[1];
            Combat.DefinirTime(eu, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(parceiro, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(alvo, 1);
            var vis = Object.FindFirstObjectByType<Gameplay.VisualDaSintonia>();
            bool criado = vis == null;   // a Main cria; sem ela, a foto cria o seu
            if (criado) vis = Gameplay.VisualDaSintonia.Criar(null, main.Partida);
            Arkana.Terrain.TerrenoReativo terreno = Arkana.Terrain.TerrenoReativoBehaviour.Atual != null ? Arkana.Terrain.TerrenoReativoBehaviour.Atual.Terreno : null;

            // a LADEIRA: perto do jogador, o ponto seco com o maior desnivel num diametro de 18 m (sem penhasco: normal.y > 0,72)
            Arkana.World.Relevo rel = Arkana.World.Ilha.Atual.Relevo;
            Vector3 ponto = eu.Pos, sobe = Vector3.forward;
            float melhor = -1f, normalY = 1f;
            for (float d = 10f; d <= 60f; d += 4f)
                for (int k = 0; k < 24; k++)
                {
                    float a = k * Mathf.PI / 12f;
                    float x = eu.Pos.x + Mathf.Cos(a) * d, z = eu.Pos.z + Mathf.Sin(a) * d;
                    if (Arkana.World.Ilha.SuperficieDaAgua(x, z) > rel.Altura(x, z) - 0.5f) continue;
                    Vector3 n = rel.Normal(x, z);
                    if (n.y < 0.72f) continue;
                    var g = new Vector3(-n.x, 0f, -n.z);   // a normal pende para BAIXO do morro: sobe-se pelo contrario
                    if (g.sqrMagnitude < 1e-6f) continue;
                    g.Normalize();
                    float desnivel = rel.Altura(x + g.x * 9f, z + g.z * 9f) - rel.Altura(x - g.x * 9f, z - g.z * 9f);
                    if (desnivel <= melhor) continue;
                    melhor = desnivel; normalY = n.y; sobe = g;
                    ponto = new Vector3(x, rel.Altura(x, z), z);
                }
            Vector3 lado = new Vector3(sobe.z, 0f, -sobe.x);
            Levar(alvo, ponto);
            Levar(eu, ponto - sobe * 11f - lado * 2f);        // a dupla ABAIXO na ladeira, o fio sobe o morro
            Levar(parceiro, ponto - sobe * 9f + lado * 6f);
            main.Player.Camera.Logica.Yaw = Mathf.Atan2(sobe.x, sobe.z);
            var sb = new System.Text.StringBuilder("55-sintonia: ladeira em " + ponto.ToString("F1") + " desnivel(18 m)=" + melhor.ToString("F2")
                + " normalY=" + normalY.ToString("F2") + "\n");
            Assert.Greater(melhor, 2f, "nao achei ladeira perto do treino: a foto nao prova o relevo");
            yield return Esperar(0.6f);

            // o AVISO deitado na ladeira (camera do jogador) enchendo, e o FALHOU rachando
            Bus.EmitSintoniaCanalizando(ComboSintonia.ChuvaDeMagma, eu, parceiro, ponto, Balance.Sintonia.CanalizacaoS);
            yield return Esperar(Balance.Sintonia.CanalizacaoS * 0.6f);
            Foto(main.Player.Camera.Cam, "55-sintonia-canalizacao", true);
            Bus.EmitSintoniaFalhou(ComboSintonia.ChuvaDeMagma, eu, parceiro, ponto);
            yield return Esperar(0.22f);
            Foto(main.Player.Camera.Cam, "55-sintonia-falhou", true);
            yield return Esperar(0.6f);

            // a FOLHA: camera de 3/4, vinda de baixo do morro e de lado (a ladeira aparece de perfil e a zona de frente)
            Camera cam = CameraTemporaria("CamFotoSintonia18", ponto - sobe * 13f - lado * 11f + Vector3.up * 10f, ponto + Vector3.up * 1.5f, Color.gray);
            const int cw = 640, ch = 400, colunas = 5;
            var combos = (ComboSintonia[])System.Enum.GetValues(typeof(ComboSintonia));
            int linhas = (combos.Length + colunas - 1) / colunas;
            var folha = new Texture2D(cw * colunas, ch * linhas, TextureFormat.RGB24, false);
            var rt = new RenderTexture(cw, ch, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            float desnivelDaZona = 0f;
            for (int i = 0; i < combos.Length; i++)
            {
                ComboSintonia c = combos[i];
                Gameplay.SintoniaEfeitos.Reset();
                if (terreno != null) terreno.Reset();   // o fogo/lama do quadro anterior nao suja o proximo
                Levar(alvo, ponto);
                yield return Esperar(0.7f);             // o que ficou esmaece e volta ao pool
                foreach (var ps in vis.GetComponentsInChildren<ParticleSystem>()) ps.Clear(true);
                yield return Esperar(0.4f);
                Elemento x, y;
                Gameplay.SintoniaEfeitos.Par(c, out x, out y);
                var d = new DisparoSintonia { Combo = c, A = eu, B = parceiro, ElA = x, ElB = y, Ponto = ponto, Alvo = alvo, Dano = 20f };
                Gameplay.SintoniaEfeitos.Disparar(d);
                Bus.EmitSintoniaDisparou(d);
                // cada forma no seu AUGE: o funil ja' alto, meteoros no ar sobre a poca, o clarao do plasma, a nuvem de vapor
                // cheia, a estrela acesa, a lama ondulando, a chuva no meio, a parede fechada, o cacho brotado, a 2a descarga
                float auge;
                switch (c)
                {
                    case ComboSintonia.TornadoFlamejante: auge = 1.3f; break;
                    case ComboSintonia.ChuvaDeMagma: auge = 0.75f; break;
                    case ComboSintonia.ExplosaoDePlasma: auge = 0.1f; break;
                    case ComboSintonia.CortinaDeVapor: auge = 1.8f; break;
                    case ComboSintonia.Eletrocussao: auge = 0.35f; break;
                    case ComboSintonia.Lamacal: auge = 1.4f; break;
                    case ComboSintonia.TempestadeTorrencial: auge = 0.8f; break;
                    case ComboSintonia.TempestadeDeAreia: auge = 1.8f; break;
                    case ComboSintonia.CristaisCarregados: auge = 0.9f; break;
                    default: auge = 1.08f; break;   // nuvem: a 2a descarga (NuvemPulsoS = 1 s) com a nuvem ja' formada
                }
                yield return Esperar(auge);
                cam.Render();
                RenderTexture.active = rt;
                folha.ReadPixels(new Rect(0, 0, cw, ch), (i % colunas) * cw, (linhas - 1 - i / colunas) * ch);
                RenderTexture.active = null;
                int particulas = 0, zonas = 0;
                float dy = 0f;
                foreach (var ps in vis.GetComponentsInChildren<ParticleSystem>()) particulas += ps.particleCount;
                foreach (var mr in vis.GetComponentsInChildren<MeshRenderer>())
                    if (mr.enabled && mr.name == "Chao") { zonas++; dy = Mathf.Max(dy, mr.bounds.size.y); }
                if (c == ComboSintonia.ChuvaDeMagma) desnivelDaZona = dy;
                sb.Append(" " + c + "(ativos=" + Gameplay.SintoniaEfeitos.Ativos.Count + ",particulas=" + particulas + ",zonas=" + zonas
                    + ",dyZona=" + dy.ToString("F2") + ")");
                Assert.Greater(particulas + Gameplay.SintoniaEfeitos.Ativos.Count, 0, c + ": o combo aparece");
            }
            folha.Apply();
            Directory.CreateDirectory(Pasta);
            File.WriteAllBytes(Path.Combine(Pasta, "55-sintonia-combos.png"), folha.EncodeToPNG());
            Assert.Greater(desnivelDaZona, 1f, "a poca de magma nao acompanhou a ladeira (malha plana?)");

            // 55-sintonia-perto: a poca de magma pela camera do JOGADOR, com meteoros no ar e estourando no chao
            Gameplay.SintoniaEfeitos.Reset();
            if (terreno != null) terreno.Reset();
            Levar(alvo, ponto);
            yield return Esperar(0.7f);
            foreach (var ps in vis.GetComponentsInChildren<ParticleSystem>()) ps.Clear(true);
            yield return Esperar(0.3f);
            var dm = new DisparoSintonia { Combo = ComboSintonia.ChuvaDeMagma, A = eu, B = parceiro, ElA = Elemento.Fogo, ElB = Elemento.Terra, Ponto = ponto, Alvo = alvo, Dano = 20f };
            Gameplay.SintoniaEfeitos.Disparar(dm);
            Bus.EmitSintoniaDisparou(dm);
            yield return Esperar(1f);
            Foto(main.Player.Camera.Cam, "55-sintonia-perto", true);
            sb.Append("\n55-sintonia-perto: dyZonaMagma=" + desnivelDaZona.ToString("F2") + "\n");
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());

            Gameplay.SintoniaEfeitos.Reset();
            cam.targetTexture = null;
            Object.Destroy(folha);
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(cam.gameObject);
            if (criado) Object.Destroy(vis.gameObject);
        }

        // ---- COLAR em FotoTests.cs dentro da classe (antes do ultimo `}` dela). Onda 18B (Selo do Campeao).

        /// <summary>
        /// O SELO DO CAMPEAO (onda 18B, GDD §18.6): o cartao do fim, em DUPLA. A cronica chega pelo Bus, o mesmo caminho do jogo
        /// (abates, dano do jogador, Sintonias da dupla, elementos disparados); o tempo vivo avanca direto na cronica.
        /// 56-selo-vitoria-carimbo: o relogio do cartao CONGELADO logo depois da batida (o selo afundando, a onda de brilho
        /// saindo, o cartao tremendo). 56-selo-vitoria: assentado (a aura respirando em volta). 56-selo-derrota: a partida
        /// seguinte — 3 duplas inimigas saem, depois a do jogador (o parceiro cai e e' finalizado, o jogador em seguida): o
        /// veredito sai sozinho, #4 DE 7 DUPLAS, o cartao sobrio com o selo apagado.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Selo_CartaoDoCampeao()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return Esperar(1.2f);   // a vitrine poe o mago no pico
            Arkana.Menu.Menu.ModoDupla = true;   // o padrao, sem depender do PlayerPrefs do editor
            Bus.EmitGameStartRequested();
            for (int n = 0; main.Carregando && n < 2000; n++) yield return null;
            yield return Esperar(1f);   // a tela de carregamento esvaece
            Assert.IsTrue(main.Dupla, "a partida e' em dupla");
            Gameplay.Pawn eu = main.Player.Pawn, par = main.Parceiro.Pawn;
            IEntidade inimigo = null;
            foreach (IEntidade e in main.Partida.Arena) if (!Combat.MesmoTime(e, eu)) { inimigo = e; break; }

            // VITORIA: a cronica de uma partida boa (3 abates, 1.284 de dano, 2 Sintonias, FOGO/VENTO/RAIO, 6:52 vivo)
            Bus.EmitSpellCast(Elemento.Fogo); Bus.EmitSpellCast(Elemento.Vento); Bus.EmitSpellCast(Elemento.Raio);
            Bus.EmitPlayerKilledBot("Vex"); Bus.EmitPlayerKilledBot("Maris"); Bus.EmitPlayerKilledBot("Brok");
            Bus.EmitDamageApplied(inimigo, 1284f, Elemento.Fogo, eu, false);
            Bus.EmitSintoniaDisparou(new DisparoSintonia { Combo = ComboSintonia.TornadoFlamejante, A = eu, B = par, ElA = Elemento.Fogo, ElB = Elemento.Vento, Ponto = eu.Pos });
            Bus.EmitSintoniaDisparou(new DisparoSintonia { Combo = ComboSintonia.ExplosaoDePlasma, A = par, B = eu, ElA = Elemento.Fogo, ElB = Elemento.Raio, Ponto = eu.Pos });
            main.Hud.Cronica.Tick(412f);
            yield return Esperar(1.9f);   // a faixa do abate e a da Sintonia saem da frente
            main.Partida.Fim(true);
            main.Hud.Cartao.Congelar = Arkana.UI.SeloDoCampeao.CarimboAtrasoS + Arkana.UI.SeloDoCampeao.DesceS + 0.07f;
            yield return null;
            Foto(main.Player.Camera.Cam, "56-selo-vitoria-carimbo", true);
            main.Hud.Cartao.Congelar = -1f;
            yield return Esperar(1.6f);
            Foto(main.Player.Camera.Cam, "56-selo-vitoria", true);
            var cr = main.Hud.Cronica;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("56-selo-vitoria: colocacao=" + cr.Colocacao + "/" + cr.Times + " parceiro=" + (cr.Parceiro != null ? cr.Parceiro.Nome : "-")
                + " abates=" + cr.Abates + " dano=" + cr.Dano + " sintonias=" + cr.Sintonias + " elementos=" + string.Join(",", cr.Usados)
                + " tempo=" + Arkana.UI.CronicaDaPartida.TextoTempo(cr.TempoVivoS) + " fluxo=" + main.Fluxo.Atual);
            Assert.AreEqual(FluxoDeJogo.Estado.Fim, main.Fluxo.Atual, "MatchOver leva ao Fim");
            Assert.IsTrue(main.Hud.Cartao.Visivel, "o cartao no ar");
            Assert.IsFalse(main.Hud.GetComponent<Canvas>().enabled, "com o cartao no ar a HUD de combate inteira sai (nada vaza por ele)");
            Assert.AreEqual(1, cr.Colocacao);
            Assert.AreEqual(7, cr.Times, "7 duplas");
            Assert.AreSame(par, cr.Parceiro, "o cartao diz COM o parceiro");
            Assert.AreEqual(3, cr.Abates);
            Assert.AreEqual(2, cr.Sintonias);
            Assert.AreEqual(3, cr.Usados.Count);

            // DERROTA pela partida seguinte: JOGAR DE NOVO pelo botao do cartao
            Tocar("BtnJogarDeNovo");
            yield return null;
            for (int n = 0; main.Carregando && n < 2000; n++) yield return null;
            yield return Esperar(1f);
            Assert.AreEqual(FluxoDeJogo.Estado.Partida, main.Fluxo.Atual, "JOGAR DE NOVO monta partida nova");
            Assert.IsFalse(main.Hud.Cartao.Visivel, "a partida nova nasce sem cartao");
            Assert.IsTrue(main.Hud.GetComponent<Canvas>().enabled, "e com a HUD de combate de volta");
            eu = main.Player.Pawn; par = main.Parceiro.Pawn;
            inimigo = null;
            foreach (IEntidade e in main.Partida.Arena) if (!Combat.MesmoTime(e, eu)) { inimigo = e; break; }
            Bus.EmitSpellCast(Elemento.Agua); Bus.EmitSpellCast(Elemento.Terra);
            Bus.EmitPlayerKilledBot("Sylva");
            Bus.EmitDamageApplied(inimigo, 537f, Elemento.Agua, eu, false);
            Bus.EmitSintoniaDisparou(new DisparoSintonia { Combo = ComboSintonia.Lamacal, A = par, B = eu, ElA = Elemento.Terra, ElB = Elemento.Agua, Ponto = eu.Pos });
            main.Hud.Cronica.Tick(187f);
            yield return Esperar(1.9f);
            // 3 duplas inimigas saem e depois a do jogador (o parceiro primeiro: o jogador cai por ultimo e o veredito sai
            // sozinho). Golpe de sobra em quem ja' morreu e' nada (Combat); 3 por corpo cobrem escudo, derrubado e finalizacao.
            var times = new System.Collections.Generic.List<int>();
            foreach (IEntidade e in main.Partida.Arena) { int t = Combat.TimeDe(e); if (t != Combat.TIME_DO_PLAYER && !times.Contains(t)) times.Add(t); }
            var alvos = new System.Collections.Generic.List<IEntidade>();
            foreach (IEntidade e in main.Partida.Arena) { int k = times.IndexOf(Combat.TimeDe(e)); if (k >= 0 && k < 3) alvos.Add(e); }
            alvos.Add(par);
            alvos.Add(eu);
            foreach (IEntidade e in alvos)
                for (int g = 0; g < 3; g++) Combat.AplicarDano(e, 9999f, Elemento.Terra, null);
            yield return Esperar(0.9f);   // o cartao pousa em 0,35 s (relogio sem escala)
            Foto(main.Player.Camera.Cam, "56-selo-derrota", true);
            cr = main.Hud.Cronica;
            sb.AppendLine("56-selo-derrota: colocacao=" + cr.Colocacao + "/" + cr.Times + " parceiro=" + (cr.Parceiro != null ? cr.Parceiro.Nome : "-")
                + " abates=" + cr.Abates + " dano=" + cr.Dano + " sintonias=" + cr.Sintonias + " elementos=" + string.Join(",", cr.Usados)
                + " tempo=" + Arkana.UI.CronicaDaPartida.TextoTempo(cr.TempoVivoS) + " fluxo=" + main.Fluxo.Atual + " timesVivos=" + main.Partida.TimesVivos);
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.AreEqual(FluxoDeJogo.Estado.Fim, main.Fluxo.Atual, "a dupla do jogador inteira fora = derrota");
            Assert.AreEqual(4, cr.Colocacao, "3 duplas de pe' = #4");
            Assert.AreEqual(7, cr.Times);
            Assert.AreSame(par, cr.Parceiro);
        }


        // ---- COLAR em FotoTests.cs dentro da classe (antes do ultimo `}` dela). Onda 18C (Grimorio de Descobertas).

        /// <summary>
        /// O GRIMORIO DE DESCOBERTAS (onda 18C). 57-grimorio-menu: o menu com ELENCO | GRIMORIO na MESMA linha (a coluna nao
        /// cresce: continua cabendo nos 437 dp do Poco F4 deitado). 57-grimorio-tela: o livro aberto — 7 de 12 acesas (placa com
        /// fio de ouro, o selo com o icone em cor, nome e frase) e 5 apagadas (a silhueta do icone e "???"), o contador 7/12.
        /// 57-grimorio-aviso: no treino, um tiro de TERRA do jogador morre no chao a' frente dele e o resto e' o caminho real
        /// (Projetil.Impacto -> TerrainHit -> TerrenoReativo -> TerrainChanged -> Grimorio): o muro sobe, a pagina MURO DE PEDRA
        /// acende e o aviso desliza a' esquerda, fora da coluna da mira e dos botoes. O save da foto e' de MEMORIA
        /// (Grimorio.Store): nenhuma pagina fica gravada no PlayerPrefs de quem roda.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Grimorio_TelaEPagina()
        {
            ExigirGpu();
            Arkana.Menu.IPrefs storeAntes = Arkana.Gameplay.Grimorio.Store;
            var memoria = new Arkana.Menu.PrefsMemoria();
            // 7 de 12; o MURO DE PEDRA fica apagado — e' ele que acende no aviso
            string[] acesas = { Arkana.Gameplay.Grimorio.LagoCongelado, Arkana.Gameplay.Grimorio.Conducao, Arkana.Gameplay.Grimorio.VentoNoFogo,
                Arkana.Gameplay.Grimorio.Sintonia, Arkana.Gameplay.Grimorio.Retorno, Arkana.Gameplay.Grimorio.NoVermelho, Arkana.Gameplay.Grimorio.EscudoCoroado };
            int mascara = 0;
            foreach (string id in acesas) mascara |= 1 << Arkana.Gameplay.Grimorio.Indice(id);
            memoria.GravarInt(Arkana.Gameplay.Grimorio.PrefChave, mascara);
            Arkana.Gameplay.Grimorio.Store = memoria;
            try
            {
                Main main = _go.AddComponent<Main>();
                yield return Esperar(1.2f);   // a vitrine poe o mago no pico
                Tocar("TapTitulo");
                yield return null;
                GameObject elenco = GameObject.Find("BtnElenco"), grimorio = GameObject.Find("BtnGrimorio");
                Assert.IsNotNull(grimorio, "o menu tem o GRIMORIO");
                Assert.IsNotNull(elenco, "e o ELENCO continua");
                Assert.AreSame(elenco.transform.parent, grimorio.transform.parent, "ELENCO e GRIMORIO na MESMA linha: a coluna nao cresce");
                Foto(main.CameraDoMenu, "57-grimorio-menu", true);

                Tocar("BtnGrimorio");
                yield return null;
                Assert.IsNotNull(GameObject.Find("BtnVoltarGrimorio"), "o livro abriu");
                GameObject numero = GameObject.Find("Contador/Numero");
                Assert.IsNotNull(numero, "o contador");
                Assert.AreEqual("7/12", numero.GetComponent<UnityEngine.UI.Text>().text);
                Assert.IsNotNull(GameObject.Find("Pagina_" + Arkana.Gameplay.Grimorio.Sintonia + "/Frase"), "acesa tem a frase");
                Assert.IsNull(GameObject.Find("Pagina_" + Arkana.Gameplay.Grimorio.MuroDePedra + "/Frase"), "apagada nao entrega nada");
                Foto(main.CameraDoMenu, "57-grimorio-tela", true);
                Tocar("BtnVoltarGrimorio");
                yield return null;

                // o AVISO em partida: o treino (jogador no chao, sem zona)
                Arkana.Menu.Menu.PedidoDeTreino = true;
                Bus.EmitGameStartRequested();
                for (int n = 0; main.Carregando && n < 2000; n++) yield return null;
                yield return Esperar(1f);   // a tela de carregamento esvaece
                Assert.IsNotNull(main.Grimorio, "a partida liga o grimorio");
                Arkana.Terrain.TerrenoReativo terreno = Arkana.Terrain.TerrenoReativoBehaviour.Atual != null ? Arkana.Terrain.TerrenoReativoBehaviour.Atual.Terreno : null;
                Assert.IsNotNull(terreno, "o treino tem terreno reativo");
                // a celula de CHAO livre (sem muro, sem ninguem em pe' nela) mais perto de 7 m a' frente da camera
                Gameplay.Pawn eu = main.Player.Pawn;
                Vector3 frente = main.Player.Camera.Cam.transform.forward; frente.y = 0f; frente.Normalize();
                Vector3 lado = new Vector3(frente.z, 0f, -frente.x), mira = eu.Pos + frente * 7f;
                int melhor = -1;
                float melhorD = float.MaxValue;
                for (float d = 4f; d <= 14f; d += 1f)
                    for (float l = -8f; l <= 8f; l += 1f)
                    {
                        int idx = terreno.CelulaEm(eu.Pos + frente * d + lado * l);
                        if (idx < 0 || terreno.Tipo(idx) != Arkana.Terrain.TipoCelula.Chao || terreno.Estado(idx) != Arkana.Terrain.EstadoCelula.Normal) continue;
                        bool ocupada = false;
                        foreach (IEntidade e in main.Partida.Arena) if (e != null && terreno.CelulaEm(e.Pos) == idx) ocupada = true;
                        float dd = (terreno.Centro(idx) - mira).sqrMagnitude;
                        if (!ocupada && dd < melhorD) { melhorD = dd; melhor = idx; }
                    }
                Assert.GreaterOrEqual(melhor, 0, "sem chao livre na frente do treino");
                Vector3 alvo = terreno.Centro(melhor);
                alvo.y = Arkana.World.Ilha.AlturaDoChao(alvo.x, alvo.z);
                var tiro = Gameplay.Projetil.Lancar(eu, alvo + Vector3.up * 0.3f, Vector3.down, Elemento.Terra);
                main.Partida.Registrar(tiro);
                tiro.Impacto(null);   // morre no chao: dali em diante e' o caminho do jogo (a Partida o tira da lista no proximo quadro)
                Assert.AreEqual(Arkana.Terrain.EstadoCelula.Muro, terreno.Estado(melhor), "o muro subiu");
                Assert.IsTrue(main.Grimorio.Acesa(Arkana.Gameplay.Grimorio.MuroDePedra), "a pagina acendeu pelo tiro DO JOGADOR");
                yield return Esperar(0.6f);   // o aviso entrou (0,3 s) e o brilho ainda esta' no selo
                var aviso = Object.FindFirstObjectByType<Arkana.UI.AvisoGrimorio>();
                Assert.IsNotNull(aviso, "o aviso e' filho da HUD");
                Assert.AreEqual(Arkana.Gameplay.Grimorio.MuroDePedra, aviso.Logica.Atual, "o aviso mostra a pagina que acendeu");
                aviso.Pintar(new Vector2(L, A));   // no tamanho do QUADRO (o batchmode tem Screen de 640x480); a HUD o Foto() refaz
                Foto(main.Player.Camera.Cam, "57-grimorio-aviso", true);
                File.AppendAllText(Path.Combine(Pasta, "diag.txt"), "57-grimorio-aviso: celula=" + melhor + " dist=" + Vector3.Distance(eu.Pos, alvo).ToString("F1")
                    + " alfa=" + aviso.Logica.Alfa.ToString("F2") + " acesas=" + main.Grimorio.Acesas + " salvo(memoria)="
                    + Arkana.Gameplay.Grimorio.Contar(memoria.Ints[Arkana.Gameplay.Grimorio.PrefChave]) + "\n");
                Assert.AreEqual(8, Arkana.Gameplay.Grimorio.Contar(memoria.Ints[Arkana.Gameplay.Grimorio.PrefChave]), "gravou na MEMORIA da foto, nao no aparelho");
            }
            finally
            {
                Arkana.Gameplay.Grimorio.Store = storeAntes;   // nada da foto fica no PlayerPrefs
            }
        }

        // ---- COLAR em FotoTests.cs dentro da classe (antes do ultimo `}` dela). Onda 18D — Ping de Sintonia.

        /// <summary>
        /// O PING DE SINTONIA (onda 18D, GDD §18.7). A partida NORMAL em dupla, os dois no chao; o jogador de luva de FOGO, o
        /// parceiro de MANOPLA Fogo+Vento; um inimigo a ~12 m, SOB A MIRA. 58-ping-proposto: o toque no ANEL da Sintonia
        /// (pelo EventSystem, no objeto do anel: o caminho do dedo) pede o combo — a faixa diz SINTONIA / COMBO? na cor do
        /// fogo e o inimigo ganha o LOSANGO do pedido (uma cor so', pulsando). 58-ping-aceito: a resposta do parceiro (as duas
        /// chamadas que o Bot.Update faz, dadas a mao: o bot fica PARADO, a foto e' da leitura, nao da IA) — PARCEIRO: ACEITO /
        /// → TORNADO FLAMEJANTE nas duas cores e o losango SELADO nas duas cores (fogo | vento). 58-ping-parceiro-atras (o
        /// achado do aparelho de 21/09): a camera virada 180 graus, o parceiro AS COSTAS — a marca azul presa na borda fica
        /// FORA dos botoes (suprema, tatica, esquiva, ataque, salto, joystick) e da coluna da mira. Depois das fotos o bot e'
        /// ligado dois quadros: o Update de verdade prende o foco no alvo combinado.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Ping_ComboProposto()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return Esperar(1.2f);
            Arkana.Menu.Menu.ModoDupla = true;
            Tocar("TapTitulo");
            yield return null;
            Bus.EmitGameStartRequested();
            for (int n = 0; main.Carregando && n < 2000; n++) yield return null;
            yield return Esperar(1f);   // a tela de carregamento esvaece
            Assert.IsTrue(main.Dupla, "JOGAR com o modo DUPLA monta a partida em dupla");
            Assert.IsNotNull(main.Parceiro, "o parceiro bot nasceu");

            // os dois no chao, lado a lado (o roteiro da 54-dupla); o parceiro PARADO
            Gameplay.Pawn eu = main.Player.Pawn, par = main.Parceiro.Pawn;
            main.Parceiro.enabled = false;
            Vector3 n0 = Arkana.World.Ilha.Atual.Relevo.Nascimentos[0];
            main.Castelo.Saltar(eu.gameObject);
            eu.Aterrar(new Vector3(n0.x, -999f, n0.z));
            yield return null;
            OlharParaOCentro(main);
            float yaw = main.Player.Camera.Logica.Yaw;
            Vector3 frente = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)), lado = new Vector3(frente.z, 0f, -frente.x);
            main.Castelo.Saltar(par.gameObject);
            Vector3 pp = eu.Pos + frente * 5f + lado * 3f;
            par.Aterrar(new Vector3(pp.x, -999f, pp.z));
            // um INIMIGO parado a ~12 m, a' esquerda do parceiro
            Gameplay.Bot inimigoBot = null;
            foreach (Gameplay.Bot b in main.Bots) if (!Combat.MesmoTime(b.Pawn, eu)) { inimigoBot = b; break; }
            Assert.IsNotNull(inimigoBot, "ha' inimigo na partida");
            inimigoBot.enabled = false;
            Gameplay.Pawn inimigo = inimigoBot.Pawn;
            main.Castelo.Saltar(inimigo.gameObject);
            Vector3 pi = eu.Pos + frente * 12f - lado * 1.5f;
            inimigo.Aterrar(new Vector3(pi.x, -999f, pi.z));
            // as luvas: o jogador de FOGO; o parceiro de MANOPLA com o fogo dele e o vento (tem de ESCOLHER o que funde)
            eu.Slot.Equipar(Gameplay.Arma.VARINHA, null, Elemento.Fogo);
            par.Slot.Equipar(Gameplay.Arma.MANOPLA, new[] { Elemento.Fogo, Elemento.Vento });
            yield return Esperar(0.6f);   // pousam e a camera assenta

            // a MIRA no inimigo: o raio da camera passa pelo OMBRO (as duas passadas da 35-alvo-dois)
            var cl = main.Player.Camera.Logica;
            Vector3 meio = inimigo.Pos + Vector3.up * 0.9f;
            for (int i = 0; i < 2; i++)
            {
                Vector3 ombro = eu.Pos + Vector3.up * (Gameplay.CameraLogica.ALTURA_PIVO + Gameplay.CameraLogica.OMBRO_Y) + cl.Direita * Gameplay.CameraLogica.OMBRO_X;
                Vector3 v = meio - ombro;
                cl.Yaw = Mathf.Atan2(v.x, v.z);
                cl.Pitch = Mathf.Atan2(-v.y, new Vector2(v.x, v.z).magnitude);
            }
            yield return Esperar(0.4f);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("58-ping: sobAMira=" + (main.Hud.Marcas.Logica.SobAMira != null ? main.Hud.Marcas.Logica.SobAMira.Nome : "-")
                + " inimigo=" + inimigo.Nome + " dist=" + Vector3.Distance(eu.Pos, inimigo.Pos).ToString("F1")
                + " parceiro=" + par.Nome + " anelPronto=" + main.Hud.Dupla.Logica.Pronta);
            Assert.AreSame(inimigo, main.Hud.Marcas.Logica.SobAMira, "o inimigo esta' SOB A MIRA");

            // O TOQUE NO ANEL: o pressionar no objeto do anel (o que o EventSystem entrega ao dedo que cai na coroa)
            GameObject toque = GameObject.Find("ToqueAnelSintonia");
            Assert.IsNotNull(toque, "o anel da Sintonia tem alvo de toque");
            var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            UnityEngine.EventSystems.ExecuteEvents.Execute(toque, ev, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            Gameplay.PingDeSintonia ping = main.Parceiro.Ping;
            Assert.AreEqual(Gameplay.PingDeSintonia.Fase.Proposto, ping.Estado, "o toque no anel pediu o combo");
            Assert.AreSame(inimigo, ping.Alvo, "no inimigo sob a mira");
            Assert.AreEqual(0, main.Partida.Projeteis.Count, "o toque no anel nao disparou o ataque");
            yield return Esperar(0.3f);
            Foto(main.Player.Camera.Cam, "58-ping-proposto", true);
            sb.AppendLine("58-ping-proposto: ping=" + ping.Estado + " faixa=" + main.Hud.Dupla.Logica.Estado + " alfa=" + main.Hud.Dupla.Logica.Alfa.ToString("F2"));
            Assert.AreEqual(Arkana.UI.DuplaHudLogica.Faixa.Combo, main.Hud.Dupla.Logica.Estado, "a faixa diz COMBO?");

            // A RESPOSTA: as duas chamadas do Bot.Update (o relogio da reacao e a resposta com a luva dele)
            ping.Tick(Gameplay.PingDeSintonia.ACEITE_S);
            ping.Responder(Gameplay.Dupla.ElementosDe(par));
            Assert.AreEqual(Gameplay.PingDeSintonia.Fase.Aceito, ping.Estado, "o parceiro aceitou");
            Assert.AreEqual(Elemento.Vento, ping.ElParceiro, "a manopla vai com o VENTO (o fogo nao funde com fogo)");
            Assert.AreEqual(ComboSintonia.TornadoFlamejante, ping.Combo);
            yield return Esperar(0.35f);   // o carimbo do aceite assenta
            Foto(main.Player.Camera.Cam, "58-ping-aceito", true);
            sb.AppendLine("58-ping-aceito: ping=" + ping.Estado + " elParceiro=" + ping.ElParceiro + " combo=" + ping.Combo
                + " faixa=" + main.Hud.Dupla.Logica.Estado + " escala=" + main.Hud.Dupla.Logica.Escala.ToString("F2"));
            Assert.AreEqual(Arkana.UI.DuplaHudLogica.Faixa.Aceito, main.Hud.Dupla.Logica.Estado, "a faixa diz PARCEIRO: ACEITO");

            // o ACHADO DO APARELHO (21/09): o parceiro AS COSTAS prendia a marca azul na borda de baixo, em cima da SUPREMA.
            // A camera vira 180 graus: a marca presa desliza para fora dos botoes e da coluna da mira
            cl.Yaw += Mathf.PI;
            cl.Pitch = CameraLogica_PitchPadrao();
            yield return Esperar(0.6f);
            Foto(main.Player.Camera.Cam, "58-ping-parceiro-atras", true);
            var marca = GameObject.Find("MarcaParceiro");
            sb.AppendLine("58-ping-parceiro-atras: marca=" + (marca != null && marca.activeInHierarchy)
                + " ancora=" + (marca != null ? ((RectTransform)marca.transform).anchorMin.ToString("F3") : "-") + " (viewport, na tela do editor)");

            // o bot de verdade: dois quadros de Update e o foco esta' preso no alvo combinado
            main.Parceiro.enabled = true;
            yield return null;
            yield return null;
            sb.AppendLine("58-ping-bot: preso=" + (main.Parceiro.Percepcao.Preso != null ? main.Parceiro.Percepcao.Preso.Nome : "-")
                + " alvo=" + (main.Parceiro.Percepcao.Alvo != null ? main.Parceiro.Percepcao.Alvo.Nome : "-"));
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), sb.ToString());
            Assert.AreSame(inimigo, main.Parceiro.Percepcao.Preso, "o Update do parceiro prendeu o foco no alvo do pacto");
            Assert.AreSame(inimigo, main.Parceiro.Percepcao.Alvo);
        }


        // ---- COLAR em FotoTests.cs dentro da classe (antes do ultimo `}` dela). Onda 18E — o gelo que sustenta.

        /// <summary>
        /// O GELO QUE SUSTENTA (onda 18E, GDD §14: agua sobre agua congela o lago e vira ROTA; o fogo derrete e quem esta' em cima
        /// cai). Ate' aqui a logica dizia Caminhavel e a tela desenhava a laje, mas o corpo nadava POR BAIXO dela. No treino: o
        /// jogador NADANDO na celula mais funda do lago; um TerrainHit de AGUA ali (o caminho do impacto, sem projetil: nenhuma
        /// pagina do Grimorio acende) congela o 3x3 — o nadador SOBE para o topo (a regra do nadador) e fica de pe' na lamina.
        /// 59-gelo-em-cima: a camera do jogador, ele de pe' na laje. Depois um TerrainHit de FOGO derrete a celula dele: o
        /// colisor sai, ele cai e volta a NADAR. 59-gelo-derreteu: ele nadando no buraco da laje.
        /// </summary>
        [UnityTest]
        public IEnumerator Foto_Gelo_OMagoAndaPorCima()
        {
            ExigirGpu();
            Main main = _go.AddComponent<Main>();
            yield return null;
            Arkana.Menu.Menu.PedidoDeTreino = true;
            Bus.EmitGameStartRequested();
            for (int n = 0; main.Carregando && n < 2000; n++) yield return null;
            yield return Esperar(1f);   // a tela de carregamento esvaece
            Assert.IsNotNull(Arkana.World.Ilha.Atual, "sem ilha");
            Arkana.Terrain.TerrenoReativo terreno = Arkana.Terrain.TerrenoReativoBehaviour.Atual != null ? Arkana.Terrain.TerrenoReativoBehaviour.Atual.Terreno : null;
            Assert.IsNotNull(terreno, "o treino tem terreno reativo");

            // a celula de AGUA mais funda do LAGO (a lamina do lago, nao o mar nem o brejo): ali se nada de verdade
            float lamina = Arkana.World.Relevo.LagoY;
            int melhor = -1;
            float fundo = float.MaxValue;
            for (int idx = 0; idx < terreno.N * terreno.N; idx++)
            {
                if (terreno.Tipo(idx) != Arkana.Terrain.TipoCelula.Agua) continue;
                Vector3 c = terreno.Centro(idx);
                if (Arkana.World.Ilha.SuperficieDaAgua(c.x, c.z) != lamina) continue;
                if (c.y < fundo) { fundo = c.y; melhor = idx; }
            }
            Assert.GreaterOrEqual(melhor, 0, "sem lago na ilha");
            Vector3 centro = terreno.Centro(melhor);
            Assert.Greater(lamina - centro.y, Gameplay.Agua.PEITO + 0.3f, "preparo: a celula e' funda (nado de verdade)");

            // 1) NADANDO: o corpo no fundo da celula; a Agua o faz boiar com o peito na lamina
            Gameplay.Pawn eu = main.Player.Pawn;
            LevarJogador(main, centro);
            yield return Esperar(1.5f);
            Assert.IsTrue(eu.Agua.Nadando, "preparo: o jogador nada no lago");
            Assert.AreEqual(Arkana.Terrain.EstadoCelula.Normal, terreno.Estado(melhor));

            // 2) CONGELA com ele dentro: sobe para o topo e fica DE PE' na lamina
            Bus.EmitTerrainHit(Elemento.Agua, centro, false);
            Assert.AreEqual(Arkana.Terrain.EstadoCelula.Congelado, terreno.Estado(melhor), "agua sobre agua congelou");
            yield return Esperar(1f);
            Vector3 p = eu.Pos;
            Assert.AreEqual(melhor, terreno.CelulaEm(p), "na mesma celula: subiu, nao foi jogado para o lado");
            Assert.IsFalse(eu.Agua.Nadando, "em cima do gelo ninguem nada");
            Assert.AreEqual(lamina, p.y, 0.15f, "de pe' NA lamina (o topo do colisor), nao por baixo");
            Assert.IsTrue(eu.NoChao, "o gelo SUSTENTA: o CharacterController esta' apoiado");
            float topo = Gameplay.ChaoComObstaculos.Topo(p.x, p.z, Arkana.World.Ilha.AlturaDoChao(p.x, p.z));
            Assert.AreEqual(lamina, topo, 0.05f, "a Queda e o pouso veem o gelo como chao (ChaoComObstaculos)");
            OlharParaOCentro(main);
            yield return Esperar(0.3f);   // a camera assenta no rumo novo
            Foto(main.Player.Camera.Cam, "59-gelo-em-cima", true);
            string diag = "59-gelo-em-cima: celula=" + melhor + " fundo=" + centro.y.ToString("F2") + " lamina=" + lamina.ToString("F2")
                + " pawn=" + p.ToString("F2") + " nadando=" + eu.Agua.Nadando + " noChao=" + eu.NoChao + " topo=" + topo.ToString("F2") + "\n";

            // 3) O FOGO derrete a celula dele: o colisor sai, ele cai e volta a NADAR
            Bus.EmitTerrainHit(Elemento.Fogo, centro, false);
            Assert.AreEqual(Arkana.Terrain.EstadoCelula.Normal, terreno.Estado(melhor), "o fogo derreteu");
            yield return Esperar(1.5f);
            Assert.IsTrue(eu.Agua.Nadando, "quem estava em cima caiu na agua e nada, como sempre");
            Assert.Less(eu.Pos.y, lamina - 0.5f, "afundou ate' o peito");
            Foto(main.Player.Camera.Cam, "59-gelo-derreteu", true);
            diag += "59-gelo-derreteu: pawn=" + eu.Pos.ToString("F2") + " nadando=" + eu.Agua.Nadando + "\n";
            Directory.CreateDirectory(Pasta);
            File.AppendAllText(Path.Combine(Pasta, "diag.txt"), diag);
        }

    }
}
