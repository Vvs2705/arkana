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

            const int cw = 480, ch = 640, colunas = 5;
            var folha = new Texture2D(cw * colunas, ch * 2, TextureFormat.RGB24, false);
            var rt = new RenderTexture(cw, ch, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            Arkana.Characters.Clipe[] todos = Arkana.Characters.PoseMago.Todos;
            for (int i = 0; i < todos.Length; i++)
            {
                m.Play(todos[i]);
                bool unico = !Arkana.Characters.PoseMago.Laco(todos[i]);
                yield return Esperar(unico ? 0.35f : 0.8f);   // disparo unico: foto no meio, antes de voltar ao idle
                cam.Render();
                RenderTexture.active = rt;
                folha.ReadPixels(new Rect(0, 0, cw, ch), (i % colunas) * cw, (1 - i / colunas) * ch);
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

            // DERRUBADO: o treino ja' levantou o boneco; ele cai pela porta do estado e um golpe drena o anel do tempo
            yield return Esperar(0.5f);
            new Gameplay.Derrubado(alvo).Cair(eu);
            Combat.AplicarDano(alvo, 35f, Elemento.Terra, eu);
            yield return Esperar(1.2f);
            Gameplay.Derrubado dd = Gameplay.Derrubado.De(alvo);
            Foto(main.Player.Camera.Cam, "29-derrubado", true);
            sb.AppendLine("29-derrubado: caido=" + (dd != null) + " esvaecimento=" + (dd != null ? dd.Esvaecimento.ToString("F2") : "-")
                + " aneis=" + vis.CaidosNaTela + " clipe=" + alvo.Clipe);

            // O JOGADOR DERRUBADO: a vinheta, o painel da HUD e o anel sob ele; o golpe tira luz e engrossa a vinheta
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
    }
}
