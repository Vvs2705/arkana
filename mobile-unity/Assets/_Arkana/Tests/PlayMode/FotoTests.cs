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
    }
}
