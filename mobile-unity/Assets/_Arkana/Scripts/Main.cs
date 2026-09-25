using System;
using System.Collections;
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
    /// QUEM NASCE NA PARTIDA, puro (o teste conta sem cena). SOLO = o de hoje: Balance.Match.Bots bots, cada um sozinho
    /// (ninguem registrado: o Combat da' a cada um o seu time) e cada um no seu salto. DUPLA (contrato 17G): as
    /// Balance.Match.DuplasInimigas duplas (times 1..n; as duas pontas no MESMO nascimento — o mesmo salto do castelo — e o
    /// 2o pousa AO LADO, afastado) e, por ULTIMO, o PARCEIRO do jogador (time 0, o nascimento do jogador): 14 corpos em 7
    /// times. O parceiro vai no fim da lista: Main.Bots[0] continua sendo um inimigo (os testes de cena usam o 1o bot como alvo).
    /// </summary>
    public static class Montagem
    {
        public struct Vaga
        {
            /// <summary>Time registrado no Combat; SEM_TIME = nao registra (o solo de hoje).</summary>
            public int Time;
            /// <summary>Quem tem o mesmo nascimento salta junto; 0 = o do jogador.</summary>
            public int Nascimento;
            /// <summary>Indice da vaga que esta' SEGUE (a outra ponta da dupla inimiga); -1 = ninguem.</summary>
            public int Segue;
            /// <summary>O parceiro do jogador: segue o jogador.</summary>
            public bool Parceiro;
            /// <summary>Lado do afastamento de quem segue (+1/-1).</summary>
            public float Lado;
        }

        public const int SEM_TIME = -1;

        /// <summary>Os bots da partida, na ordem de criacao.</summary>
        public static Vaga[] Bots(bool dupla) => dupla ? Duplas(Balance.Match.DuplasInimigas) : Solo(Balance.Match.Bots);

        /// <summary>Corpos na arena, o jogador incluso.</summary>
        public static int Corpos(bool dupla) => 1 + Bots(dupla).Length;

        public static Vaga[] Solo(int bots)
        {
            var v = new Vaga[Mathf.Max(bots, 0)];
            for (int i = 0; i < v.Length; i++) v[i] = new Vaga { Time = SEM_TIME, Nascimento = i + 1, Segue = -1 };
            return v;
        }

        public static Vaga[] Duplas(int duplas)
        {
            int n = Mathf.Max(duplas, 0);
            var v = new Vaga[n * 2 + 1];
            for (int t = 0; t < n; t++)
            {
                v[t * 2] = new Vaga { Time = t + 1, Nascimento = t + 1, Segue = -1 };
                v[t * 2 + 1] = new Vaga { Time = t + 1, Nascimento = t + 1, Segue = t * 2, Lado = (t % 2 == 0) ? 1f : -1f };
            }
            v[n * 2] = new Vaga { Time = Combat.TIME_DO_PLAYER, Nascimento = 0, Segue = -1, Parceiro = true, Lado = 1f };
            return v;
        }
    }

    /// <summary>
    /// A casca fina da cena Main: garante EventSystem, cria Sfx, Menu, Ilha, Sol e a camera do menu (com a VitrineDoMenu) no
    /// boot, e a cada partida monta a arena (HUD, Castelo, Player, Bots/bonecos, Partida) e a desmonta no fim/abandono.
    /// Tudo por-partida vive sob "Arena" (restart = arena nova, limpa — estado que atravessa partida ja' vazou 3x no Godot).
    /// A MONTAGEM e' em FATIAS (passo E da ORDEM: "sem quadro acima de 100 ms"): a TelaDeCarregamento cobre a tela no toque
    /// do JOGAR, os modelos carregam fora do quadro e a arena sobe em passos por quadro com o relogio do jogo parado; no fim
    /// a tela esvaece e a partida comeca como antes.
    /// Fiacao defensiva: peca ausente vira aviso, nunca excecao. Nao chama Bus.Reset(): HUD/Sfx/Fluxo ja' assinaram.
    /// </summary>
    public sealed class Main : MonoBehaviour
    {
        public const string NomeArena = "Arena";
        public const int Bonecos = 2;
        /// <summary>
        /// KNOB: ms de trabalho da montagem por quadro. Estourou, o proximo passo fica para o proximo quadro — o pior quadro e'
        /// este orcamento + o passo mais caro. Menor = tela mais lisa e carregamento mais longo (cada quadro a mais custa o
        /// desenho da tela e a espera do vsync).
        /// </summary>
        public const float OrcamentoMs = 12f;

        /// <summary>0 = sorteia a cada partida (o jogo). > 0 = partida repetivel (foto.ps1 e testes). Nunca ligado no APK.</summary>
        public static int SeedForcado = 0;

        /// <summary>A partida esta' sendo montada: a tela de carregamento na frente, o relogio do jogo parado (timeScale 0: o
        /// castelo nao parte, ninguem salta, a zona nao conta) e a Partida sem Tick (arena pela metade nao da' veredito).</summary>
        public bool Carregando { get; private set; }
        /// <summary>A tela de carregamento viva; esvaece e se destroi sozinha depois da montagem (== null de novo).</summary>
        public TelaDeCarregamento Tela { get; private set; }
        /// <summary>A MEDICAO da ultima montagem (sem log: o teste grava): ms de cada passo e da pre-carga (parede), quantos
        /// quadros a montagem levou, o pior deles e a duracao inteira, do toque ao fim.</summary>
        public readonly List<KeyValuePair<string, float>> TemposDaMontagem = new List<KeyValuePair<string, float>>();
        public float PiorQuadroMs { get; private set; }
        public int QuadrosDaMontagem { get; private set; }
        public float DuracaoDaMontagemMs { get; private set; }

        public FluxoDeJogo Fluxo { get; private set; }
        public Partida Partida { get; private set; }
        public Player Player { get; private set; }
        public readonly List<Bot> Bots = new List<Bot>();
        /// <summary>O parceiro bot do jogador (modo DUPLA; null no solo e no treino). Tambem esta' em Bots (o ultimo).</summary>
        public Bot Parceiro { get; private set; }
        /// <summary>Esta partida e' em DUPLA (Menu.ModoDupla fora do treino), lido no toque do JOGAR.</summary>
        public bool Dupla { get; private set; }
        public Hud Hud { get; private set; }
        /// <summary>O Grimorio de Descobertas da partida (18C): ouve o Bus, acende pagina e grava UM inteiro no aparelho.</summary>
        public Grimorio Grimorio { get; private set; }
        public ArkMenu Menu { get; private set; }
        public Sfx Sfx { get; private set; }
        public Castelo Castelo { get; private set; }
        /// <summary>A camera do menu (a "Main Camera" da cena ou, sem cena, a que o boot criou): filma a VitrineDoMenu.</summary>
        public Camera CameraDoMenu => _camMenu;

        Transform _arena;
        Camera _camMenu;
        ArkVitrine _vitrine;
        Coroutine _montagem;
        RenderTexture _arte;
        ThreadPriority _prioridade;
        readonly System.Diagnostics.Stopwatch _quadro = new System.Diagnostics.Stopwatch();
        readonly System.Diagnostics.Stopwatch _parede = new System.Diagnostics.Stopwatch();
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
            if (_arte != null) { _arte.Release(); Destroy(_arte); _arte = null; }
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
            if (Partida == null || Carregando) return;   // montando: a partida so' anda quando a arena estiver inteira
            Partida.Tick(dt);
            if (Hud != null && !Partida.Treino) AtualizarHud();
            // o parceiro mira o que o jogador tem SOB A MIRA enquanto nao tem alvo (o 1o acerto do jogador ja' o foca pelo Bus)
            if (Parceiro != null && Hud != null) Parceiro.Percepcao.MiraDoParceiro(Hud.Marcas.Logica.SobAMira);
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
        /// <summary>A partida acontece na ilha do Documento Mestre (25/09/2026). false = a ilha procedural de 600 m.</summary>
        public const bool UsarIlhaMestre = true;

        Ilha GarantirIlha()
        {
            Ilha ilha = Ilha.Atual;
            if (ilha == null)
            {
                if (UsarIlhaMestre && FindFirstObjectByType<IlhaMestre>() == null)
                    _meus.Add(new GameObject("IlhaMestre", typeof(IlhaMestre)));   // OnEnable -> Construir(), antes da Ilha
                ilha = new GameObject("Ilha").AddComponent<Ilha>();   // Awake -> Montar()
                _meus.Add(ilha.gameObject);
            }
            if (FindFirstObjectByType<Sol>() == null) _meus.Add(new GameObject("Sol", typeof(Sol)));
            return ilha;
        }

        // ---------------------------------------------------------------- arena

        /// <summary>
        /// O toque do JOGAR/TREINO (via FluxoDeJogo). Sincrono so' o que a tela precisa: fotografar a vitrine, apagar o menu,
        /// cobrir com a tela e parar o relogio. O resto e' a corrotina Montando, em fatias. ANTES (ate' 12/09) tudo isto — 13
        /// Resources.Load de mago de ~10 MB + 2 texturas de 2K cada, HUD, castelo, 12 bots, terreno e 5 visuais — cabia num
        /// quadro so': a tela congelava no menu, sem retorno, ate' a partida aparecer.
        /// </summary>
        void Montar()
        {
            _quadro.Restart();   // o quadro do toque ja' conta: foto da vitrine + tela
            _parede.Restart();
            bool treino = ArkMenu.PedidoDeTreino;   // lido ANTES: Partida.Iniciar consome e zera
            Dupla = !treino && ArkMenu.ModoDupla;   // o treino e' sozinho com os bonecos (o combo se aprende la' com o parceiro na partida)
            string slug = ArkSelecao.MagoEscolhido;
            // sorteado POR PARTIDA; Partida/Castelo/Zona/Loot guardam para a rede. SeedForcado > 0 so' para foto/teste:
            // sem ele cada foto pousa num lugar diferente e duas rodadas nao se comparam. A primeira dica tambem sai dele.
            int seed = SeedForcado > 0 ? SeedForcado : new System.Random().Next(1, int.MaxValue);
            Texture arte = FotografarVitrine();   // ANTES de a vitrine desligar: o mago escolhido em guarda no pico
            MostrarMenu(false);
            Tela = TelaDeCarregamento.Criar(arte, slug, treino, seed);
            Carregando = true;
            Time.timeScale = 0f;
            // KNOB: a integracao dos carregamentos assincronos pode usar ate' 50 ms por quadro (o padrao BelowNormal, 4 ms):
            // atras da tela ninguem joga, carregar rapido vale mais que 60 FPS. Normal (10 ms) se a tela engasgar no aparelho.
            _prioridade = Application.backgroundLoadingPriority;
            Application.backgroundLoadingPriority = ThreadPriority.High;
            _montagem = StartCoroutine(Montando(slug, treino, seed));
        }

        /// <summary>A montagem em fatias: 1) a PRE-CARGA dos modelos fora do quadro (Resources.LoadAsync), com a barra no
        /// progresso real; 2) os PASSOS do Montar de antes, na mesma ordem, quantos couberem no OrcamentoMs de cada quadro;
        /// 3) o AQUECER atras da tela (HUD e camera acesos um quadro: shader e textura sobem ainda cobertos). No fim o relogio
        /// volta e a tela esvaece.</summary>
        IEnumerator Montando(string slug, bool treino, int seed)
        {
            TemposDaMontagem.Clear();
            PiorQuadroMs = 0f;
            QuadrosDaMontagem = 0;
            yield return null;   // a tela desenha o primeiro quadro (0%) antes de qualquer peso
            FecharQuadro();

            IRelevo relevo = GarantirIlha().Chao;   // a do boot, quase sempre
            _arena = new GameObject(NomeArena).transform;
            Montagem.Vaga[] vagas = Montagem.Bots(Dupla);
            Vector3[] nasc = Nascimentos(relevo, seed, 1 + vagas.Length);
            int corpos = treino ? Bonecos : vagas.Length;

            // 1. O PESO: cada mago e' um .fbx de ~10 MB com duas texturas de 2K; o castelo, o bau e as luvas tambem vem de
            //    Resources. A pre-carga os le' e descomprime no carregador do Unity, fora do quadro; o Resources.Load do
            //    Mago/Castelo/loot depois so' acha na memoria. As requisicoes ficam vivas (a lista) ate' o fim da montagem.
            var cargas = new List<ResourceRequest>();
            foreach (string nome in Modelos(slug, corpos, treino)) cargas.Add(Resources.LoadAsync<GameObject>(nome));
            List<Passo> passos = Passos(slug, treino, seed, relevo, nasc, corpos, vagas);
            float total = cargas.Count + passos.Count;
            var relogio = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                float soma = 0f;
                bool todas = true;
                for (int i = 0; i < cargas.Count; i++) { soma += cargas[i].isDone ? 1f : cargas[i].progress; todas &= cargas[i].isDone; }
                Avancar(soma / total, Textos.CarregaMagos);
                if (todas) break;
                yield return null;
                FecharQuadro();
            }
            TemposDaMontagem.Add(new KeyValuePair<string, float>("pre-carga", (float)relogio.Elapsed.TotalMilliseconds));

            // 2 e 3. Os passos: quadro que estourou o orcamento fecha antes do proximo; passo "sozinho" fecha logo depois
            for (int i = 0; i < passos.Count; i++)
            {
                if (_quadro.Elapsed.TotalMilliseconds >= OrcamentoMs) { yield return null; FecharQuadro(); }
                Avancar((cargas.Count + i) / total, passos[i].Rotulo);
                double antes = _quadro.Elapsed.TotalMilliseconds;
                passos[i].Fazer();
                Time.timeScale = 0f;   // o Hud.Vincular chama Retomar (timeScale 1): o relogio continua esperando a tela
                TemposDaMontagem.Add(new KeyValuePair<string, float>(passos[i].Nome, (float)(_quadro.Elapsed.TotalMilliseconds - antes)));
                if (passos[i].Sozinho) { yield return null; FecharQuadro(); }
            }

            cargas.Clear();
            _montagem = null;
            Carregando = false;
            Time.timeScale = 1f;   // a partida comeca AQUI, com a tela esvaecendo por cima
            Application.backgroundLoadingPriority = _prioridade;
            DuracaoDaMontagemMs = (float)_parede.Elapsed.TotalMilliseconds;
            if (Tela != null) Tela.Terminar();
        }

        void Avancar(float progresso, string passo) { if (Tela != null) Tela.Avancar(progresso, passo); }

        /// <summary>Fecha a medicao de um quadro da montagem: de uma retomada da corrotina a outra = o quadro inteiro (o
        /// desenho da tela e os Start que cairam nele inclusos).</summary>
        void FecharQuadro()
        {
            float ms = (float)_quadro.Elapsed.TotalMilliseconds;
            if (ms > PiorQuadroMs) PiorQuadroMs = ms;
            QuadrosDaMontagem++;
            _quadro.Restart();
        }

        /// <summary>Um passo da montagem: o que faz, o nome na medicao, o rotulo na tela e se o quadro fecha logo depois
        /// (`sozinho`: quem trabalha no Start — castelo, terreno reativo, voo — pesa no comeco do quadro SEGUINTE, fora do
        /// orcamento; nada mais entra nele).</summary>
        struct Passo
        {
            public readonly string Nome, Rotulo;
            public readonly Action Fazer;
            public readonly bool Sozinho;
            public Passo(string nome, string rotulo, Action fazer, bool sozinho = false) { Nome = nome; Rotulo = rotulo; Fazer = fazer; Sozinho = sozinho; }
        }

        /// <summary>
        /// O Montar de antes, na MESMA ordem, picado. A ordem e' contrato: a HUD antes do Pawn e do Iniciar (assina MatchStarted e
        /// KitBound no Criar); o Vincular antes do Start do castelo (o Mapa.Zerar apagaria a rota que o Start emite); os bots
        /// antes do Embarcar do jogador; o Voo por ultimo, com todo corpo na arena. A HUD e a camera do jogador nascem
        /// APAGADAS (nada de Update com arena pela metade, nada de desenhar o mundo atras da tela) e acendem no aquecer.
        /// </summary>
        List<Passo> Passos(string slug, bool treino, int seed, IRelevo relevo, Vector3[] nasc, int corpos, Montagem.Vaga[] vagas)
        {
            var p = new List<Passo>();
            p.Add(new Passo("hud", Textos.CarregaArena, () =>
            {
                Hud = Hud.Criar();
                Hud.gameObject.SetActive(false);
                Hud.ReiniciarPedido += () => Fluxo.Reiniciar();
                Hud.MenuPedido += VoltarAoMenu;
                Hud.AbandonarPedido += VoltarAoMenu;
                // O GRIMORIO (18C) liga ANTES do Partida.Iniciar (ouve o MatchStarted); o aviso e' filho da HUD (nasce e morre
                // com ela, fora da coluna da mira) e ticka o relogio dele.
                Grimorio = new Grimorio();
                Grimorio.Ligar();
                AvisoGrimorio.Criar(Hud.transform, Grimorio);
            }));
            p.Add(new Passo("jogador", Textos.CarregaMagos, () =>
            {
                Player = Player.Criar(_arena, slug);
                Player.Camera.Cam.enabled = false;
                Partida = new Partida(relevo);
                Partida.Iniciar(seed, treino ? 0 : vagas.Length, treino, nasc[0]);
                Partida.Registrar(Player.Pawn, Player.Pawn.Slot);
                if (Dupla) Combat.DefinirTime(Player.Pawn, Combat.TIME_DO_PLAYER);   // DEPOIS do Iniciar (o Combat.Reset limpa os times)
                Hud.Vincular(Player.Pawn);
            }));
            p.Add(new Passo("loot", Textos.CarregaArena, () => VisualDaPartida.Criar(_arena, Partida)));   // loot, bau e tempestade na tela
            if (treino)
            {
                p.Add(new Passo("pouso", Textos.CarregaArena, () => Player.Pawn.Aterrar(nasc[0])));
                for (int i = 0; i < corpos; i++)
                {
                    int k = i;
                    p.Add(new Passo("boneco" + (k + 1), Textos.CarregaMagos, () =>
                    {
                        // boneco = Pawn cru (nao age, nao persegue) que a Partida regenera: apanhar sem culpa e' o servico dele
                        Pawn d = Pawn.Criar(_arena, OutroSlug(slug, k), false);
                        d.name = "Boneco" + (k + 1);
                        d.Aterrar(nasc[0] + new Vector3(8f + k * 3f, 0f, -2f));
                        Partida.RegistrarBoneco(d);
                    }));
                }
            }
            else
            {
                p.Add(new Passo("castelo", Textos.CarregaCastelo, () => Castelo = Castelo.Criar(_arena, relevo, seed), true));
                for (int i = 0; i < corpos; i++)
                {
                    int k = i;
                    p.Add(new Passo("bot" + (k + 1), Textos.CarregaMagos, () =>
                    {
                        Bot b = Bot.Criar(_arena, OutroSlug(slug, k), seed + k + 1);
                        b.Embarcar(Castelo);   // todos caem do mesmo castelo, cada bot no seu instante sorteado (a dupla, junto)
                        Partida.Registrar(b.Pawn, b.Pawn.Slot);
                        Parear(b, vagas[k]);
                        Bots.Add(b);
                    }));
                }
            }
            p.Add(new Passo("ligar", Textos.CarregaArena, () =>
            {
                if (!treino) Player.Embarcar(Castelo);
                if (Partida.Treino) Hud.ModoTreino(); else AtualizarHud();
                Player.Ligar(Hud);
                Sfx.PosOuvinte = PosDoJogador;
            }));
            // O TERRENO REATIVO (GDD §14). Ate' 11/09 ele so' existia nos testes: nenhuma cena o criava, e fogo/gelo/muro
            // nunca aconteciam na partida. Nasce sob a arena (morre com ela); a Partida ticka pelo TerrenoReativoBehaviour.Atual.
            // Sozinho: o Start dele monta a grade da ilha e registra as 615 arvores.
            p.Add(new Passo("terreno", Textos.CarregaTerreno, () =>
            {
                _arena.gameObject.AddComponent<TerrenoReativoBehaviour>();
                VisualDoTerreno.Criar(_arena);        // fogo, carvao, gelo, eletrico, lama, muro
            }, true));
            p.Add(new Passo("kits", Textos.CarregaTerreno, () => VisualDosKits.Criar(_arena, Partida)));       // muralha, fio, poca, eco, tear + o aviso da suprema
            p.Add(new Passo("impacto", Textos.CarregaTerreno, () =>
            {
                VisualDoImpacto.Criar(_arena, Partida);    // estouro, piscada no corpo, bolha no escudo
                VisualDaSintonia.Criar(_arena, Partida);   // a canalizacao da dupla, os 10 combos e o "falhou" (17C; os dois modos)
            }));
            p.Add(new Passo("abate", Textos.CarregaTerreno, () => VisualDoAbate.Criar(_arena, Partida)));      // derrubado e eliminado
            p.Add(new Passo("voo", Textos.CarregaCastelo, () => VisualDoVoo.Criar(_arena, Partida, Castelo), true));   // castelo vivo, rastro, vento, estalo
            // AQUECER atras da tela: a HUD e a camera do jogador acendem um quadro ANTES de a tela sair — o primeiro desenho do
            // mundo visto do castelo (ou do chao do treino), com o que ele custa, cai ainda coberto.
            p.Add(new Passo("aquecer", Textos.CarregaPronto, () =>
            {
                Hud.gameObject.SetActive(true);
                Player.Camera.Cam.enabled = true;
            }, true));
            return p;
        }

        /// <summary>O que a partida vai instanciar de Resources — o mago do jogador, o de cada bot/boneco (o mesmo OutroSlug
        /// dos passos), o castelo, o bau e as tres luvas — nos caminhos do Mago.TentarModeloExterno, Castelo.MontarVisual,
        /// BauVisual e LootVisual. Caminho que nao existir so' termina com asset nulo (sem log).</summary>
        static List<string> Modelos(string slug, int corpos, bool treino)
        {
            var m = new List<string> { "magos/" + slug };
            for (int i = 0; i < corpos; i++) m.Add("magos/" + OutroSlug(slug, i));
            if (!treino) m.Add("castelo");
            m.Add(BauVisual.MODELO);
            foreach (string id in Arma.TIERS) m.Add(LootVisual.ModeloDe(id));
            return m;
        }

        /// <summary>
        /// A ARTE da tela de carregamento: o quadro da vitrine (o mago escolhido em guarda no pico, o por do sol atras) num
        /// RenderTexture, no toque do JOGAR — uma renderizacao a mais, uma vez. Sem a vitrine na tela (jogar de novo) reusa a
        /// ultima; sem GPU (portao -nographics), null. KNOB: no tamanho da tela; tela pequena (a janela 640x480 do editor)
        /// sai no 2400x1080 do Poco F4 — o da foto.
        /// </summary>
        Texture FotografarVitrine()
        {
            if (_camMenu == null || !_camMenu.enabled || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return _arte;
            bool pequena = Screen.width < 1280;
            int w = pequena ? 2400 : Screen.width, h = pequena ? 1080 : Screen.height;
            if (_arte != null && (_arte.width != w || _arte.height != h)) { _arte.Release(); Destroy(_arte); _arte = null; }
            if (_arte == null) _arte = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "ArteDoCarregamento" };
            RenderTexture antes = _camMenu.targetTexture;
            _camMenu.targetTexture = _arte;
            _camMenu.Render();
            _camMenu.targetTexture = antes;
            return _arte;
        }

        void Desmontar()
        {
            if (_montagem != null) { StopCoroutine(_montagem); _montagem = null; }   // abandono/teste no meio da montagem
            if (Carregando) { Carregando = false; Application.backgroundLoadingPriority = _prioridade; }
            if (Tela != null) { Destroy(Tela.gameObject); Tela = null; }
            if (Partida != null) { Partida.Encerrar(); Partida = null; }
            if (Grimorio != null) { Grimorio.Desligar(); Grimorio = null; }
            if (Sfx != null) Sfx.PosOuvinte = null;
            if (Hud != null) { Destroy(Hud.gameObject); Hud = null; }
            if (Player != null && Player.Camera != null) Destroy(Player.Camera.gameObject);   // a camera nao e' filha da arena
            if (_arena != null) { Destroy(_arena.gameObject); _arena = null; }
            for (int i = 0; i < _tiros.Count; i++) if (_tiros[i] != null) Destroy(_tiros[i]);
            _tiros.Clear();
            _donoDoTiro.Clear();
            Player = null; Castelo = null; Bots.Clear(); Parceiro = null;
            Time.timeScale = 1f;   // a pausa da HUD nao pode atravessar partida
        }

        /// <summary>O topo da HUD: BOTS n no solo, DUPLAS n (times vivos) na dupla.</summary>
        void AtualizarHud() => Hud.AtualizarPartida(Partida.Restante, Partida.BotsVivos, Dupla ? Partida.TimesVivos : -1);

        /// <summary>Registra o time e forma a dupla da vaga (solo: nada — o de hoje). A outra ponta ja' existe: nasceu antes.</summary>
        void Parear(Bot b, Montagem.Vaga v)
        {
            if (v.Time != Montagem.SEM_TIME) Combat.DefinirTime(b.Pawn, v.Time);
            if (v.Parceiro)
            {
                b.Parear(Player.Pawn, true, v.Lado);   // salta quando o jogador salta, pousa ao lado dele, foca o alvo dele
                Parceiro = b;
            }
            else if (v.Segue >= 0 && v.Segue < Bots.Count)
            {
                Bot lider = Bots[v.Segue];
                b.Parear(lider.Pawn, true, v.Lado);
                lider.Parear(b.Pawn, false);   // o lider nao segue, mas foca o alvo do outro e o socorre
            }
        }

        void VoltarAoMenu()
        {
            if (Fluxo.VoltarAoMenu()) MostrarMenu(true);
        }

        Vector3 PosDoJogador()
        {
            IEntidade visto = Partida != null && Partida.PlayerFora ? Partida.ParceiroVivo() : null;   // espectador: ouve onde a camera esta'
            if (visto != null) return visto.Pos;
            if (Player != null && Player.Pawn != null) return Player.Pawn.Pos;
            Camera c = Camera.main;
            return c != null ? c.transform.position : Vector3.zero;
        }

        /// <summary>[0] = jogador (sorteado pelo seed), o resto para os bots. Sem ilha: centro + anel de 30 m (Main.gd) com
        /// `total` pontos (os corpos da partida).</summary>
        static Vector3[] Nascimentos(IRelevo relevo, int seed, int total)
        {
            Vector3[] n = relevo is Relevo r ? r.Nascimentos : relevo is RelevoMestre rm ? rm.Nascimentos : null;
            if (n == null || n.Length < 2)
            {
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
