using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Arkana.Audio;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.UI;
using Arkana.Terrain;
using Arkana.World;
using ArkMenu = Arkana.Menu.Menu;
using ArkSelecao = Arkana.Menu.SelecaoPersonagem;
using ArkVitrine = Arkana.Menu.VitrineDoMenu;

namespace Arkana
{
    /// <summary>
    /// O CICLO DE VIDA, puro (porte do que Main.gd + Menu.gd decidiam): Menu -> Partida -> Fim -> Menu.
    /// Transicoes vem do Bus (GameStartRequested, MatchOver) ou de pedidos da HUD (reiniciar, menu, abandonar).
    /// A casca injeta `montar`/`desmontar`; aqui so' se decide QUANDO. Testavel sem cena.
    /// </summary>
    public sealed class FluxoDeJogo
    {
        public enum Estado { Menu, Partida, Fim }

        public Estado Atual { get; private set; } = Estado.Menu;
        public int Montagens { get; private set; }
        public int Desmontagens { get; private set; }
        public int Fins { get; private set; }
        public bool Vitoria { get; private set; }
        public float TempoNoEstado { get; private set; }

        readonly Action _montar, _desmontar;
        bool _ligado;

        public FluxoDeJogo(Action montar, Action desmontar) { _montar = montar; _desmontar = desmontar; }

        /// <summary>Assina o Bus UMA vez (repetir nao duplica). Bus.Reset() tambem cala; por isso a casca nao reseta o Bus.</summary>
        public void Ligar()
        {
            if (_ligado) return;
            _ligado = true;
            Bus.GameStartRequested += AoPedirPartida;
            Bus.MatchOver += AoAcabar;
        }

        public void Desligar()
        {
            if (!_ligado) return;
            _ligado = false;
            Bus.GameStartRequested -= AoPedirPartida;
            Bus.MatchOver -= AoAcabar;
        }

        void AoPedirPartida() { PedirPartida(); }
        void AoAcabar(bool vitoria) { Acabou(vitoria); }

        /// <summary>Menu -> Partida. Fora do menu o pedido e' ignorado (toque duplo no JOGAR nao monta duas arenas).</summary>
        public bool PedirPartida()
        {
            if (Atual != Estado.Menu) return false;
            Montar();
            return true;
        }

        /// <summary>Partida -> Fim, UMA vez: o segundo MatchOver (ou um fora de partida) nao conta.</summary>
        public bool Acabou(bool vitoria)
        {
            if (Atual != Estado.Partida) return false;
            Atual = Estado.Fim;
            TempoNoEstado = 0f;
            Vitoria = vitoria;
            Fins++;
            return true;
        }

        /// <summary>Jogar de novo: arena velha fora, arena nova. Vale no fim e no meio (abandonar e recomecar).</summary>
        public bool Reiniciar()
        {
            if (Atual == Estado.Menu) return false;
            Desmontar();
            Montar();
            return true;
        }

        /// <summary>Abandono (Partida) ou "menu" na tela de fim: desmonta e volta.</summary>
        public bool VoltarAoMenu()
        {
            if (Atual == Estado.Menu) return false;
            Desmontar();
            Atual = Estado.Menu;
            TempoNoEstado = 0f;
            return true;
        }

        public void Tick(float dt) { if (dt > 0f) TempoNoEstado += dt; }

        void Montar()
        {
            Atual = Estado.Partida;
            TempoNoEstado = 0f;
            Montagens++;
            _montar?.Invoke();
        }

        void Desmontar()
        {
            Desmontagens++;
            _desmontar?.Invoke();
        }
    }

    /// <summary>
    /// A casca fina da cena Main: garante EventSystem, cria Sfx, Menu, Ilha, Sol e a camera do menu (com a VitrineDoMenu) no
    /// boot, e a cada partida monta a arena (HUD, Castelo, Player, Bots/bonecos, Partida) e a desmonta no fim/abandono.
    /// Tudo por-partida vive sob "Arena" (restart = arena nova, limpa — estado que atravessa partida ja' vazou 3x no Godot).
    /// Fiacao defensiva: peca ausente vira aviso, nunca excecao. Nao chama Bus.Reset(): HUD/Sfx/Fluxo ja' assinaram.
    /// </summary>
    public sealed class Main : MonoBehaviour
    {
        public const string NomeArena = "Arena";
        public const int Bonecos = 2;

        /// <summary>0 = sorteia a cada partida (o jogo). > 0 = partida repetivel (foto.ps1 e testes). Nunca ligado no APK.</summary>
        public static int SeedForcado = 0;

        public FluxoDeJogo Fluxo { get; private set; }
        public Partida Partida { get; private set; }
        public Player Player { get; private set; }
        public readonly List<Bot> Bots = new List<Bot>();
        public Hud Hud { get; private set; }
        public ArkMenu Menu { get; private set; }
        public Sfx Sfx { get; private set; }
        public Castelo Castelo { get; private set; }
        /// <summary>A camera do menu (a "Main Camera" da cena ou, sem cena, a que o boot criou): filma a VitrineDoMenu.</summary>
        public Camera CameraDoMenu => _camMenu;

        Transform _arena;
        Camera _camMenu;
        ArkVitrine _vitrine;
        readonly List<GameObject> _meus = new List<GameObject>();      // o que este boot criou fora da arena (some com ele)
        readonly List<GameObject> _tiros = new List<GameObject>();
        /// <summary>Qual projetil cada objeto do pool desenhou no quadro anterior: trocou de dono, o rastro recomeca (senao risca a tela).</summary>
        readonly List<Projetil> _donoDoTiro = new List<Projetil>();
        static readonly Dictionary<Projetil.Forma, Mesh> _malhas = new Dictionary<Projetil.Forma, Mesh>();
        static readonly Dictionary<Elemento, Material> _materiais = new Dictionary<Elemento, Material>();

        void Awake()
        {
            GarantirEventSystem();
            if (GetComponent<AudioListener>() == null) gameObject.AddComponent<AudioListener>();   // o unico ouvinte da cena (menu e partida)
            Sfx = Sfx.Criar();
            _meus.Add(Sfx.gameObject);
#if !UNITY_EDITOR
            gameObject.AddComponent<MedidorDeFps>();   // so' no aparelho: FPS no logcat (a medicao que faltava desde 25/08)
#endif
            Menu = ArkMenu.Criar();   // ANTES da ilha: o Criar aplica a Config (nivel de qualidade) que a ilha e o sol encontram
            _meus.Add(Menu.gameObject);
            GarantirIlha();   // ja' no boot: o fundo do menu e' o mago no pico (a partida reaproveita a mesma ilha)
            // A camera do menu: a da cena ou, sem cena (PlayMode), uma propria. Na partida ela desliga e a do Player vira a
            // Camera.main (Grama/Vegetacao/Kit cortam por ela); no menu ela e' a Camera.main e o corte mede o pico.
            GameObject cam = GameObject.Find("Main Camera");
            _camMenu = cam != null ? cam.GetComponent<Camera>() : null;
            if (_camMenu == null)
            {
                var go = new GameObject("CameraMenu");
                go.tag = "MainCamera";
                _camMenu = go.AddComponent<Camera>();   // sem AudioListener: o unico e' o do Main
                _meus.Add(go);
            }
            _camMenu.clearFlags = CameraClearFlags.Skybox;   // o ceu do por do sol da ilha, nao mais a cor chapada da noite
            _camMenu.fieldOfView = 40f;
            _camMenu.nearClipPlane = 0.1f;
            _camMenu.farClipPlane = Ilha.FarDaCamera;
            Ilha.LigarPos(_camMenu);   // o mesmo pos da partida (bloom, tonemapping)
            _camMenu.enabled = true;
            _vitrine = _camMenu.gameObject.AddComponent<ArkVitrine>();
            Fluxo = new FluxoDeJogo(Montar, Desmontar);
            Fluxo.Ligar();
            _pedidoAdb = PartidaPeloAdb.Pedido();   // teste sem dedo (MIUI recusa toque pelo adb); null = jogo normal
            _fpsAdb = PartidaPeloAdb.FpsPedido();
        }

        int _fpsAdb;

        string _pedidoAdb;
        float _relogioAdb;
        bool _saltoAdb;

        void OnDestroy()
        {
            if (Fluxo != null) Fluxo.Desligar();
            Desmontar();
            for (int i = 0; i < _meus.Count; i++) if (_meus[i] != null) Destroy(_meus[i]);
            _meus.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Fluxo.Tick(dt);
            if (_pedidoAdb != null && Fluxo.Atual == FluxoDeJogo.Estado.Menu)
            {
                _relogioAdb += dt;
                if (_relogioAdb >= 1.5f)   // o menu chega a desenhar um quadro: a foto do aparelho mostra o boot
                {
                    ArkMenu.PedidoDeTreino = _pedidoAdb == "treino";
                    _pedidoAdb = null;
                    _saltoAdb = true;
                    Bus.EmitGameStartRequested();
                    if (_fpsAdb != 0) Application.targetFrameRate = _fpsAdb;   // depois da Config (que roda no Criar do Menu)
                }
            }
            // o jogador automatico salta no meio da rota (a 1a rodada no aparelho caiu no mar: o castelo empurra no FIM da rota, fora da ilha)
            if (_saltoAdb && Castelo != null && Player != null && Castelo.Progresso >= PartidaPeloAdb.SALTO_EM)
            {
                _saltoAdb = false;
                Player.Saltar();
            }
            if (Partida == null) return;
            Partida.Tick(dt);
            if (Hud != null && !Partida.Treino) Hud.AtualizarPartida(Partida.Restante, Partida.BotsVivos);
            DesenharTiros();
        }

        // ---------------------------------------------------------------- boot

        /// <summary>A HUD (uGUI) precisa de UM EventSystem com o modulo do Input System — uma vez, nunca dois.</summary>
        void GarantirEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            _meus.Add(go);
        }

        void MostrarMenu(bool on)
        {
            if (Menu != null)
            {
                Menu.gameObject.SetActive(on);
                if (on) Menu.Rearmar();
            }
            if (_camMenu != null) _camMenu.enabled = on;
            if (_vitrine != null) _vitrine.enabled = on;   // desligada, o mago do pico some (o treino nasce la')
        }

        /// <summary>Ilha e Sol SE FALTAREM (a cena so' tem Main + luz + camera). Nascem no boot e atravessam partidas.
        /// Sem log: o BootTests reprova qualquer log inesperado.</summary>
        Ilha GarantirIlha()
        {
            Ilha ilha = Ilha.Atual;
            if (ilha == null)
            {
                ilha = new GameObject("Ilha").AddComponent<Ilha>();   // Awake -> Montar()
                _meus.Add(ilha.gameObject);
            }
            if (FindFirstObjectByType<Sol>() == null) _meus.Add(new GameObject("Sol", typeof(Sol)));
            return ilha;
        }

        // ---------------------------------------------------------------- arena

        void Montar()
        {
            MostrarMenu(false);
            Relevo relevo = GarantirIlha().Relevo;   // a do boot, quase sempre

            _arena = new GameObject(NomeArena).transform;
            // sorteado POR PARTIDA; Partida/Castelo/Zona/Loot guardam para a rede. SeedForcado > 0 so' para foto/teste:
            // sem ele cada foto pousa num lugar diferente e duas rodadas nao se comparam.
            int seed = SeedForcado > 0 ? SeedForcado : new System.Random().Next(1, int.MaxValue);
            bool treino = ArkMenu.PedidoDeTreino;                    // lido ANTES: Partida.Iniciar consome e zera
            Vector3[] nasc = Nascimentos(relevo, seed);

            Hud = Hud.Criar();   // antes de Iniciar: a HUD assina MatchStarted no Criar
            Hud.ReiniciarPedido += () => Fluxo.Reiniciar();
            Hud.MenuPedido += VoltarAoMenu;
            Hud.AbandonarPedido += VoltarAoMenu;

            string slug = ArkSelecao.MagoEscolhido;
            Player = Player.Criar(_arena, slug);
            Partida = new Partida(relevo);
            Partida.Iniciar(seed, Balance.Match.Bots, treino, nasc[0]);
            Partida.Registrar(Player.Pawn, Player.Pawn.Slot);
            VisualDaPartida.Criar(_arena, Partida);   // loot, bau e tempestade na tela (a HUD so' anunciava)

            if (Partida.Treino)
            {
                Player.Pawn.Aterrar(nasc[0]);
                for (int i = 0; i < Bonecos; i++)
                {
                    // boneco = Pawn cru (nao age, nao persegue) que a Partida regenera: apanhar sem culpa e' o servico dele
                    Pawn d = Pawn.Criar(_arena, OutroSlug(slug, i), false);
                    d.name = "Boneco" + (i + 1);
                    d.Aterrar(nasc[0] + new Vector3(8f + i * 3f, 0f, -2f));
                    Partida.RegistrarBoneco(d);
                }
            }
            else
            {
                Castelo = Castelo.Criar(_arena, relevo, seed);
                for (int i = 0; i < Balance.Match.Bots; i++)
                {
                    Bot b = Bot.Criar(_arena, OutroSlug(slug, i), seed + i + 1);
                    b.Embarcar(Castelo);   // todos caem do mesmo castelo, cada bot no seu instante sorteado
                    Partida.Registrar(b.Pawn, b.Pawn.Slot);
                    Bots.Add(b);
                }
                Player.Embarcar(Castelo);
            }

            Hud.Vincular(Player.Pawn);
            if (Partida.Treino) Hud.ModoTreino(); else Hud.AtualizarPartida(Partida.Restante, Partida.BotsVivos);
            Player.Ligar(Hud);
            Sfx.PosOuvinte = PosDoJogador;
            // O TERRENO REATIVO (GDD §14). Ate' 11/09 ele so' existia nos testes: nenhuma cena o criava, e fogo/gelo/muro
            // nunca aconteciam na partida. Nasce sob a arena (morre com ela); a Partida ticka pelo TerrenoReativoBehaviour.Atual.
            _arena.gameObject.AddComponent<TerrenoReativoBehaviour>();
            VisualDoTerreno.Criar(_arena);            // fogo, carvao, gelo, eletrico, lama, muro
            VisualDosKits.Criar(_arena, Partida);     // muralha, fio, poca, eco, tear + o aviso da suprema no chao
        }

        void Desmontar()
        {
            if (Partida != null) { Partida.Encerrar(); Partida = null; }
            if (Sfx != null) Sfx.PosOuvinte = null;
            if (Hud != null) { Destroy(Hud.gameObject); Hud = null; }
            if (Player != null && Player.Camera != null) Destroy(Player.Camera.gameObject);   // a camera nao e' filha da arena
            if (_arena != null) { Destroy(_arena.gameObject); _arena = null; }
            for (int i = 0; i < _tiros.Count; i++) if (_tiros[i] != null) Destroy(_tiros[i]);
            _tiros.Clear();
            _donoDoTiro.Clear();
            Player = null; Castelo = null; Bots.Clear();
            Time.timeScale = 1f;   // a pausa da HUD nao pode atravessar partida
        }

        void VoltarAoMenu()
        {
            if (Fluxo.VoltarAoMenu()) MostrarMenu(true);
        }

        Vector3 PosDoJogador()
        {
            if (Player != null && Player.Pawn != null) return Player.Pawn.Pos;
            Camera c = Camera.main;
            return c != null ? c.transform.position : Vector3.zero;
        }

        /// <summary>[0] = jogador (sorteado pelo seed), o resto para os bots. Sem ilha: centro + anel de 30 m (Main.gd).</summary>
        static Vector3[] Nascimentos(Relevo relevo, int seed)
        {
            Vector3[] n = relevo != null ? relevo.Nascimentos : null;
            if (n == null || n.Length < 2)
            {
                int total = 1 + Balance.Match.Bots;
                var pts = new Vector3[total];
                pts[0] = new Vector3(0f, 1f, 0f);
                for (int i = 1; i < total; i++)
                {
                    float a = Mathf.PI * 2f * (i - 1) / (total - 1);
                    pts[i] = new Vector3(Mathf.Cos(a) * 30f, 1f, Mathf.Sin(a) * 30f);
                }
                return pts;
            }
            var lista = new List<Vector3>(n);
            int k = ((seed % lista.Count) + lista.Count) % lista.Count;
            Vector3 meu = lista[k];
            lista.RemoveAt(k);
            lista.Insert(0, meu);
            return lista.ToArray();
        }

        /// <summary>i-esimo slug do elenco que NAO e' o do jogador (bots e bonecos nunca repetem o mago do jogador).</summary>
        static string OutroSlug(string doJogador, int i)
        {
            var outros = new List<string>();
            foreach (string s in Kits.Slugs) if (s != doJogador) outros.Add(s);
            if (outros.Count == 0) return doJogador ?? "";
            return outros[((i % outros.Count) + outros.Count) % outros.Count];
        }

        // ---------------------------------------------------------------- tiros (pool de primitivas)

        /// <summary>Cor + FORMA por elemento (GDD §10) sobre Partida.Projeteis; a fisica e' toda da Partida.</summary>
        void DesenharTiros()
        {
            List<Projetil> lista = Partida.Projeteis;
            while (_tiros.Count < lista.Count) { _tiros.Add(NovoTiro()); _donoDoTiro.Add(null); }
            for (int i = 0; i < _tiros.Count; i++)
            {
                GameObject go = _tiros[i];
                if (go == null) continue;
                if (i >= lista.Count) { if (go.activeSelf) go.SetActive(false); _donoDoTiro[i] = null; continue; }
                Projetil p = lista[i];
                Vestir(go, p.FormaDoTiro, p.ElementoDoTiro);
                go.transform.position = p.Pos;
                if (_donoDoTiro[i] != p)
                {
                    // o pool casa por INDICE: quando um tiro morre, os de tras trocam de objeto — o rastro recomeca aqui
                    _donoDoTiro[i] = p;
                    var tr = go.GetComponent<TrailRenderer>();
                    Color c = Projetil.Tint(p.ElementoDoTiro);
                    tr.startColor = c;
                    tr.endColor = new Color(c.r, c.g, c.b, 0f);
                    tr.Clear();
                }
                if (p.Dir.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(p.Dir, Vector3.up);
                if (!go.activeSelf) go.SetActive(true);
            }
        }

        GameObject NovoTiro()
        {
            var go = new GameObject("Tiro", typeof(MeshFilter), typeof(MeshRenderer));   // sem colisor: a camera e a mira nao podem esbarrar no tiro
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // RASTRO: bola chapada voando le' como placeholder (foto 18 de 12/09); o fio aditivo (HDR) da o movimento e acende no bloom
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.18f;
            tr.minVertexDistance = 0.12f;
            tr.widthMultiplier = 0.32f;
            tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            tr.numCapVertices = 2;
            tr.alignment = LineAlignment.View;
            tr.sharedMaterial = MaterialVfx.DeLinha();   // faixa macia na LARGURA: o ponto esticado apagava as pontas do rastro
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            go.SetActive(false);
            return go;
        }

        static void Vestir(GameObject go, Projetil.Forma forma, Elemento el)
        {
            var mf = go.GetComponent<MeshFilter>();
            Mesh m = MalhaDe(forma);
            if (mf.sharedMesh != m) mf.sharedMesh = m;
            var mr = go.GetComponent<MeshRenderer>();
            Material mat = MaterialDe(el);
            if (mr.sharedMaterial != mat) mr.sharedMaterial = mat;
            go.transform.localScale = EscalaDe(forma);
        }

        // ponytail: leitura de silhueta com primitivas; malha de verdade por elemento quando a arte chegar
        static Vector3 EscalaDe(Projetil.Forma f)
        {
            switch (f)
            {
                case Projetil.Forma.Lamina: return new Vector3(0.5f, 0.08f, 0.7f);
                case Projetil.Forma.Dardo: return new Vector3(0.12f, 0.12f, 1.1f);
                case Projetil.Forma.Pedra: return new Vector3(0.45f, 0.45f, 0.45f);
                case Projetil.Forma.Espiral: return new Vector3(0.35f, 0.35f, 0.9f);
                default: return new Vector3(0.5f, 0.5f, 0.5f);
            }
        }

        static Mesh MalhaDe(Projetil.Forma f)
        {
            Mesh m;
            if (_malhas.TryGetValue(f, out m) && m != null) return m;
            PrimitiveType tipo = f == Projetil.Forma.Lamina || f == Projetil.Forma.Dardo || f == Projetil.Forma.Pedra ? PrimitiveType.Cube : PrimitiveType.Sphere;
            GameObject tmp = GameObject.CreatePrimitive(tipo);
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            _malhas[f] = m;
            return m;
        }

        static Material MaterialDe(Elemento el)
        {
            Material mat;
            if (_materiais.TryGetValue(el, out mat) && mat != null) return mat;
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Standard");
            mat = s != null ? new Material(s) : Ilha.MaterialPadrao();
            mat.name = "Tiro_" + Elementos.Id(el);
            // [MainColor] do URP: color escreve _BaseColor. Acima de 1 (HDR): o miolo do tiro passa do limiar do bloom
            // e vira ORBE aceso, nao bola de plastico. Alfa 1 (Color * k multiplicaria o alfa junto). KNOB: com 2,2 o fogo
            // saturava o vermelho e virava AMARELO — a cor do Raio da Tessa (foto 18 de 12/09); 1,5 segura o matiz.
            Color t = Projetil.Tint(el);
            mat.color = new Color(t.r * 1.5f, t.g * 1.5f, t.b * 1.5f, 1f);
            _materiais[el] = mat;
            return mat;
        }
    }
}
