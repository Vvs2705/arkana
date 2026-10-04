using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.World;
using Arkana.Characters;
using Arkana.Terrain;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O CORPO de player e bot (porte de Pawn.gd): CharacterController + Vitalidade + ArmaSlot + Queda + Locomocao +
    /// Agua + mana + KitRunner, com o visual do Mago. Recebe INTENCAO (Stick/YawCam/EncararDir e os verbos) — nao le' Input.
    /// `AnimDeLocomocao()` e' a UNICA fonte da animacao (estado sustentado se resolve aqui, nao driblando);
    /// gesto transitorio ("cast", "pegar") volta por relogio. Nasce INVISIVEL ate' `Saltar()` (decisao 14: o castelo
    /// viaja sem bonecos). Combat.TickDot, Efeitos.Tick e TerrenoReativo.Tick NAO sao daqui: rodam uma vez por frame
    /// em Partida.Tick. E' o IConjurador dos kits: entrega mira, chao, mana (que o kit LE') e os alvos da arena, e resolve os
    /// verbos de corpo que eles pedem (Teleportar, Impulso, pulo com mola, invisivel/penumbra para a percepcao do bot).
    /// </summary>
    public class Pawn : MonoBehaviour, IConjurador
    {
        public const float ALTURA_MAO = 1.4f;
        public const float SAIDA_TIRO = 0.9f;
        public const float GESTO_CAST_S = 0.3f;
        /// <summary>s que o gesto da tatica/suprema segura a pose pedida (o Mecanim segura o tronco por Balance.Anim.KitSeguraS).</summary>
        public const float GESTO_KIT_S = 0.6f;
        public static readonly Color TINT_MORTO = new Color(0.25f, 0.25f, 0.3f);
        /// <summary>Os estados de corpo que a percepcao do bot le' (IConjurador.AplicarEstado).</summary>
        public const string INVISIVEL = "invisivel", PENUMBRA = "penumbra", SEM_PASSOS = "sem_passos";
        /// <summary>m do centro ate' onde o pouso do teleporte confere o chao (o corpo + folga): pilar, tronco e borda de
        /// rocha a menos disso tiram o lugar.</summary>
        public const float PEGADA = Partida.RAIO_CORPO + 0.15f;
        /// <summary>m entre as tentativas do pouso seguro, recuando pela linha.</summary>
        const float PASSO_POUSO = 1f;

        // ---------------------------------------------------------------- IEntidade
        public string Nome => name;
        public Vector3 Pos => transform.position;
        public bool EhPlayer { get; private set; }
        public Vitalidade Vital { get; private set; }

        // ---------------------------------------------------------------- estado
        public string Slug { get; private set; } = "";
        public ArmaSlot Slot { get; private set; }
        public Queda Queda { get; private set; }
        public Locomocao Loc { get; private set; }
        public Agua Agua { get; private set; }
        public Mago Visual { get; private set; }
        /// <summary>O motor do kit deste mago (um por pawn; bots tambem tem, mas so' o player fala com a HUD).</summary>
        public KitRunner Runner { get; private set; }
        /// <summary>Set publico pelo IConjurador; o kit LE' e nunca escreve (GDD §4.2). Por dentro, EscreverMana (avisa a HUD).</summary>
        public float Mana { get; set; } = Balance.Player.ManaMax;
        /// <summary>Elemento do carrossel (fallback da luva legada sem elemento).</summary>
        public Elemento Elemento = Elemento.Fogo;
        /// <summary>Cadencia relativa ao player (o bot atira mais devagar). KNOB do bot.</summary>
        public float CadenciaMult = 1f;
        public bool NoChao { get; private set; }
        public string Clipe { get; private set; } = "";
        /// <summary>Quantas vezes mais ALTO o pulo sobe: a mola do Fizz escreve (IConjurador). 1 = normal.</summary>
        public float FatorDePulo { get; set; } = 1f;
        public int Pulos { get; private set; }

        // ---------------------------------------------------------------- intencao (a casca escreve)
        /// <summary>x direita, y frente; magnitude 0..1 ja' moldada pelo joystick.</summary>
        public Vector2 Stick;
        /// <summary>Yaw (rad) que da' o sentido do stick (camera do player; 0 no bot, que manda direcao de mundo).</summary>
        public float YawCam;
        /// <summary>Se tem valor, o corpo gira para este yaw (rad) — mirando. Senao encara EncararDir ou o movimento.</summary>
        public float? YawAlvo;
        /// <summary>Direcao (mundo) para encarar sem se mover (bot atirando). Zero = ignora.</summary>
        public Vector3 EncararDir;

        private CharacterController _cc;
        private Castelo _castelo;
        private Vector3 _dir;
        private float _fireCd, _gestoS;
        private string _gesto = "";
        private bool _querFlutuar, _morto, _visivel, _sumido;
        private Color _tint = Color.white;
        private GameObject _luva;
        // relogios dos estados que so' o corpo resolve (IConjurador.AplicarEstado)
        private float _intangivelS, _silencioS, _invisivelS, _penumbraS, _semPassosS;
        private float _danoCausadoAntes;
        // pernas x mira (onda 15B): o giro do visual sobre o corpo e se esta' recuando (histerese)
        private float _pernasYaw;
        private bool _recuando;
        // a busca de alvos e' um servico da Partida em curso; sem partida, ninguem por perto (o kit trata null)
        private static readonly Func<Vector3, float, IEntidade[]> _buscador = BuscarAlvos;

        public float VelocidadeHorizontal => Loc != null ? Loc.VelocidadeHorizontal : 0f;
        public bool Viva => Vital != null && Vital.Viva;
        /// <summary>Age quem esta' vivo, no chao (nada de magia no ar) e nao derrubado.</summary>
        public bool PodeAgir => Viva && Queda.PodeConjurar && Derrubado.PodeAgir(this);
        /// <summary>Esquiva ou plano espectral (intangivel): imunidade total pelo relogio.</summary>
        public bool Invulneravel => Loc.IframesLeft > 0f || _intangivelS > 0f;
        /// <summary>Silencio do proprio kit (Veu saindo do Atravessar) ou imposto de fora (aliado na Mare) barra ATE' o ataque.</summary>
        public bool PodeConjurar => _silencioS <= 0f && (Runner == null || Runner.PodeConjurar);
        public Vector3 Frente => transform.forward;
        /// <summary>A VISAO do bot nao o pega: invisivel, ou em penumbra PARADO (andando e' um vulto — o limitador da Umbra).</summary>
        public bool Oculto => Viva && (_invisivelS > 0f || (_penumbraS > 0f && VelocidadeHorizontal <= PercepcaoBot.PASSOS_V));
        /// <summary>Os PASSOS nao entregam (Passo de Veludo). O oculto a percepcao ja' nao localiza por passo.</summary>
        public bool SemPassos => _semPassosS > 0f;
        public Vector3 DirecaoDaEsquiva => Loc != null ? Loc.DirEsquiva : Vector3.zero;
        /// <summary>O corpo encara uma mira (player mirando, bot encarando o alvo) em vez do proprio rumo.</summary>
        public bool Mirando => YawAlvo.HasValue || EncararDir.sqrMagnitude > 0.0001f;
        /// <summary>Graus que o VISUAL gira sobre o corpo para as pernas acharem o rumo (0 = de frente para a mira).</summary>
        public float PernasYaw => _pernasYaw;
        /// <summary>Anda para tras mirando (Walk_Backward; BackpedalMult na velocidade).</summary>
        public bool Recuando => _recuando;

        // ---------------------------------------------------------------- IConjurador
        /// <summary>Mirando (player) e' o yaw da camera; senao o corpo (EncararDir do bot) ou a frente.</summary>
        public Vector3 DirecaoDaMira => YawAlvo.HasValue
            ? new Vector3(Mathf.Sin(YawAlvo.Value), 0f, Mathf.Cos(YawAlvo.Value))
            : (EncararDir.sqrMagnitude > 0.0001f ? EncararDir : transform.forward);

        public Func<Vector3, float, IEntidade[]> AlvosNoRaio => _buscador;

        /// <summary>"intangivel" = i-frames pelo relogio (a mascara de colisao fisica fica para depois: o Acerto da Partida
        /// ja' respeita i-frames); "silencio" = sem conjurar; "invisivel"/"penumbra"/"sem_passos" = o que a percepcao do bot
        /// le' (Oculto, SemPassos). Renova para o maior prazo, nunca encurta; dur 0 apaga.</summary>
        public void AplicarEstado(string nome, float dur)
        {
            switch (nome)
            {
                case "intangivel": _intangivelS = Renovar(_intangivelS, dur); break;
                case "silencio": _silencioS = Renovar(_silencioS, dur); break;
                case INVISIVEL: _invisivelS = Renovar(_invisivelS, dur); break;
                case PENUMBRA: _penumbraS = Renovar(_penumbraS, dur); break;
                case SEM_PASSOS: _semPassosS = Renovar(_semPassosS, dur); break;
            }
        }

        private static float Renovar(float agora, float dur) => dur > 0f ? Mathf.Max(agora, dur) : 0f;

        private static IEntidade[] BuscarAlvos(Vector3 pos, float raio) =>
            Partida.Atual != null ? Partida.Atual.AlvosNoRaio(pos, raio) : null;

        private static void RegistrarProjetil(Projetil p) { if (Partida.Atual != null) Partida.Atual.Registrar(p); }
        private static IList<Projetil> ProjeteisDaArena() => Partida.Atual != null ? Partida.Atual.Projeteis : null;

        /// <summary>O produto unico do pawn, PURO: agua e chao reativo (lama) sao os dois fatores de terreno, multiplicados.</summary>
        public static float VelocidadeMaxima(float agua, float terrenoReativo, float status, float postura) =>
            Locomocao.Speed(agua * terrenoReativo, status, postura);

        /// <summary>Lama = MudSlow; sem terreno na cena, 1. O DoT do chao NAO e' daqui (TerrenoReativo.Tick, via Partida).</summary>
        private static float FatorDoTerrenoReativo(Vector3 pos)
        {
            TerrenoReativoBehaviour t = TerrenoReativoBehaviour.Atual;
            return t != null && t.Terreno != null ? t.Terreno.FatorTerreno(pos) : 1f;
        }

        // ---------------------------------------------------------------- criacao

        /// <summary>A vida com que o corpo de `slug` nasce: a da ficha (IdentidadeMago.VidaBase — a Pip tem 55), player ou bot.</summary>
        public static Vitalidade VidaDe(string slug) => new Vitalidade(IdentidadeMago.De(slug).VidaBase);

        /// <summary>Um corpo novo, invisivel, no castelo (`Embarcar`) ou no chao (`Aterrar`).</summary>
        public static Pawn Criar(Transform pai, string slug, bool ehPlayer)
        {
            var go = new GameObject(Kits.De(slug).Nome);
            if (pai != null) go.transform.SetParent(pai, false);
            var cc = go.AddComponent<CharacterController>();
            cc.height = Partida.ALTURA_CORPO; cc.radius = Partida.RAIO_CORPO;
            cc.center = new Vector3(0f, Partida.ALTURA_CORPO * 0.5f, 0f);
            cc.slopeLimit = Balance.Move.InclinacaoMax; cc.stepOffset = 0.4f;   // a mesma rampa que a cola da Locomocao acompanha
            var p = go.AddComponent<Pawn>();
            p.Montar(slug, ehPlayer);
            return p;
        }

        private void Montar(string slug, bool ehPlayer)
        {
            Slug = slug ?? "";
            EhPlayer = ehPlayer;
            _cc = GetComponent<CharacterController>();
            Vital = VidaDe(Slug);
            Slot = new ArmaSlot(this) { AutoUpgrade = !ehPlayer };   // decisao 5: o player equipa apertando PEGAR
            Loc = new Locomocao();
            IRelevo relevo = Ilha.Atual != null ? Ilha.Atual.Chao : null;
            Agua = new Agua(relevo);
            // a Queda enxerga o topo das pedras/arvores/ruinas, nao so' o terreno (senao pousa DENTRO delas — foto de 11/09)
            Queda = new Queda(relevo != null ? new ChaoComObstaculos(relevo) : null, transform.position, ehPlayer);
            // o kit nasce com o corpo: KitBound sai daqui (so' player) — a HUD precisa existir ANTES do Pawn
            Runner = new KitRunner(Slug, this) { Slot = Slot, AoLancar = RegistrarProjetil, ProjeteisVivos = ProjeteisDaArena };
            Visual = Mago.Criar(transform, Slug);   // fiacao defensiva mora no Mago (modelo ausente -> procedural)
            Visivel(false);
        }

        void OnEnable()
        {
            Bus.WeaponEquipped += AoEquipar;
            Bus.DamageApplied += AoDano;
        }

        void OnDisable()
        {
            Bus.WeaponEquipped -= AoEquipar;
            Bus.DamageApplied -= AoDano;
        }

        /// <summary>Golpe recebido: o tronco acusa o lado de onde veio (so' no Mecanim; o flash do corpo segue no VisualDoImpacto).</summary>
        private void AoDano(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool emEscudo)
        {
            if (!ReferenceEquals(alvo, this) || Visual == null || !(dano > 0f)) return;
            Vector3 de = fonte != null ? fonte.Pos - transform.position : transform.forward;
            Visual.Golpe(transform.InverseTransformDirection(de));
        }

        void OnDestroy()
        {
            if (Runner != null) Runner.Desligar();
        }

        /// <summary>Viaja pendurado no portao, invisivel, ate' Saltar().</summary>
        public void Embarcar(Castelo castelo)
        {
            _castelo = castelo;
            if (castelo != null) castelo.Embarcar(gameObject);
        }

        /// <summary>Salta do castelo (ou cai de onde esta'). Na BORDA. O corpo SURGE aqui.</summary>
        public bool Saltar()
        {
            if (Queda.Fase != Queda.NO_CASTELO) return false;
            Vector3 p = _castelo != null ? _castelo.Saltar(gameObject) : transform.position;
            _castelo = null;
            transform.position = p;
            Queda.Posicionar(p);
            Queda.Saltar();
            Visivel(true);
            return true;
        }

        /// <summary>Nasce no chao (treino / sem castelo): pousa no ato e ja' pode agir.</summary>
        public void Aterrar(Vector3 pos)
        {
            float chao = Ilha.AlturaDoChao(pos.x, pos.z);
            transform.position = new Vector3(pos.x, Mathf.Max(pos.y, chao), pos.z);
            Queda.Posicionar(transform.position);
            Queda.Saltar();
            Queda.Tick(1f / 60f);   // Altura <= ALTURA_POUSO: pousa no primeiro passo
            transform.position = Queda.Pos;
            _castelo = null;
            Visivel(true);
        }

        public void Planar() { Queda.AlternarPlanar(); }

        public void SetTint(Color c)
        {
            _tint = c;
            if (Visual != null) Visual.SetTint(c);
        }

        private void Visivel(bool on)
        {
            _visivel = on;
            Renderer[] rs = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++) rs[i].enabled = on;
        }

        /// <summary>Quem NAO e' o dono nao ve' o oculto: a camera e' do player, entao o corpo de BOT some inteiro (luva junto;
        /// morto volta a aparecer para o abate). O do player fica: a leitura e' do kit (vidro do Ilusionista, penumbra da
        /// Umbra) — o dono sabe onde esta'. So' na BORDA (a busca de renderers aloca).
        /// ponytail: bot nao usa kit, entao hoje nenhum corpo de bot fica oculto; o brilho de perto (ficha 08) pede VFX no dia.</summary>
        private void Sumir()
        {
            bool some = !EhPlayer && Oculto;
            if (some == _sumido) return;
            _sumido = some;
            Visivel(!some);
        }

        // ---------------------------------------------------------------- o frame

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            TickKit(dt);
            if (Queda.NoAr) { TickNoAr(dt); return; }
            Sumir();
            if (!Viva) { if (!_morto) Morrer(); return; }
            if (_morto) Reviver();   // boneco de treino que a Partida resetou

            if (Mana < Balance.Player.ManaMax) EscreverMana(Mathf.Min(Mana + Balance.Player.ManaRegen * dt, Balance.Player.ManaMax));
            _fireCd = Mathf.Max(_fireCd - dt, 0f);
            _gestoS = Mathf.Max(_gestoS - dt, 0f);

            Agua.Tick(dt, transform.position);
            Efeitos.EstadoAlvo est = Efeitos.De(this);
            _dir = Locomocao.DirDoStick(Stick, YawCam);
            // recuar mirando e' mais lento: entra no produto como postura (Balance.Move.BackpedalMult)
            float recuo = Mirando && _dir.sqrMagnitude > 0.0001f
                ? Locomocao.FatorDeRecuo(Mathf.DeltaAngle(YawDaMira(), Mathf.Atan2(_dir.x, _dir.z) * Mathf.Rad2Deg)) : 1f;
            float velMax = VelocidadeMaxima(Agua.Fator, FatorDoTerrenoReativo(transform.position), est.StatusMult, Derrubado.FatorVelocidade(this) * recuo);
            bool podeFlutuar = _querFlutuar && Derrubado.PodeAgir(this);
            float mana = Mana;
            Vector3 desloc = Loc.Tick(dt, _dir, NoChao, Agua.Nadando, velMax, est.StunLeft > 0f, podeFlutuar, ref mana);
            if (mana != Mana) EscreverMana(mana);
            // o corpo e' a fonte UNICA dos i-frames: esquiva e intangivel (o kit pede por AplicarEstado, nunca escreve aqui)
            est.IframesLeft = Mathf.Max(Loc.IframesLeft, _intangivelS);
            if (Agua.Nadando) desloc.y = Agua.Flutuar(transform.position, dt).y - transform.position.y;

            if (_cc != null && _cc.enabled)
            {
                Vector3 antes = transform.position;
                _cc.Move(desloc);
                NoChao = _cc.isGrounded;
                Loc.Real(transform.position - antes, dt);   // contra a parede a passada para: nada de correr parado
            }
            else { transform.position += desloc; NoChao = false; }
            // rede de seguranca: a verdade do chao e' a ilha (sem colisor, ou atravessou a malha)
            Vector3 pos = transform.position;
            float chao = Ilha.AlturaDoChao(pos.x, pos.z);
            // (dentro de caverna/tunel da IlhaMestre o chao verdadeiro e' a casca, abaixo da superficie: a rede nao puxa)
            if (pos.y < chao && !Ilha.Subterraneo(pos))
            {
                transform.position = new Vector3(pos.x, chao, pos.z); NoChao = true;
                Physics.SyncTransforms();   // sem isto o proximo Move do CharacterController parte da pose antiga
            }
            if (NoChao && !Ilha.NoVazio(pos)) UltimoChaoSeguro = pos;
            if (Viva && Ilha.NoVazio(transform.position))   // caiu da borda da ilha flutuante: morre, e o corpo volta a beira
            {
                Combat.MorrerNoVazio(this);
                if (_cc != null) _cc.enabled = false;
                transform.position = UltimoChaoSeguro;
                Physics.SyncTransforms();
            }
            Loc.AtualizarAr(dt, NoChao, Agua.Nadando);
            if (NoChao && Loc.PuloGuardadoS > 0f) Pular();   // o SALTO tocado pouco antes de pousar sai agora (jump buffer)

            Virar(dt);
            Loc.AtualizarBank(transform.eulerAngles.y * Mathf.Deg2Rad, dt);
            Pernas(dt);
            Tocar(_gestoS > 0f ? _gesto : AnimDeLocomocao());
        }

        /// <summary>Yaw (graus) que o corpo encara mirando: a camera (player) ou o alvo (bot).</summary>
        private float YawDaMira() => YawAlvo.HasValue ? YawAlvo.Value * Mathf.Rad2Deg : Mathf.Atan2(EncararDir.x, EncararDir.z) * Mathf.Rad2Deg;

        /// <summary>
        /// PERNAS x MIRA (onda 15B). Mirando, o corpo (transform) fica na mira — e' a verdade do jogo — e so' o VISUAL gira as
        /// pernas para o rumo real (ou contra ele, recuando); o Mago desfaz o giro no tronco. Solto, o corpo ja' vira para o
        /// rumo e as pernas voltam a zero. O banking de sempre continua por fora, no referencial do corpo.
        /// </summary>
        private void Pernas(float dt)
        {
            if (Visual == null) return;
            if (Visual.Mecanim)
            {
                // a biblioteca do Mixamo tem strafe e recuo de verdade (blend pela velocidade no corpo): sem girar o visual
                _pernasYaw = 0f; _recuando = false; Visual.Torcao = 0f;
                Visual.transform.localRotation = Quaternion.Euler(0f, 0f, -Loc.Bank * Mathf.Rad2Deg);
                return;
            }
            float alvo = 0f;
            Vector3 v = Loc.Vel;
            bool andando = Mirando && !Agua.Nadando && !Derrubado.Esta(this) && new Vector2(v.x, v.z).magnitude > Balance.Move.RunAnimExit;
            if (andando)
            {
                PoseMago.Passada p = PoseMago.PassadaMirando(
                    Mathf.DeltaAngle(transform.eulerAngles.y, Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg), _recuando);
                _recuando = p.Tras;
                alvo = p.Pernas;
            }
            else _recuando = false;
            // Lerp comum (nao LerpAngle): o alvo mora em +-105, e trocar de lado passa por ZERO — o quadril gira pela frente
            _pernasYaw = Mathf.Lerp(_pernasYaw, alvo, 1f - Mathf.Exp(-Balance.Anim.PernasRate * dt));
            Visual.transform.localRotation = Quaternion.Euler(0f, 0f, -Loc.Bank * Mathf.Rad2Deg) * Quaternion.Euler(0f, _pernasYaw, 0f);
            Visual.Torcao = PoseMago.TorcaoDoTronco(_pernasYaw);
        }

        /// <summary>Relogios do kit e dos estados do corpo. Roda ate' no ar (a carga da suprema ja' se barra por NoChao).</summary>
        private void TickKit(float dt)
        {
            _intangivelS = Mathf.Max(_intangivelS - dt, 0f);
            _silencioS = Mathf.Max(_silencioS - dt, 0f);
            _invisivelS = Mathf.Max(_invisivelS - dt, 0f);
            _penumbraS = Mathf.Max(_penumbraS - dt, 0f);
            _semPassosS = Mathf.Max(_semPassosS - dt, 0f);
            if (Runner == null || Vital == null) return;
            float causado = Vital.DanoCausado;
            Runner.Tick(dt, causado - _danoCausadoAntes);   // o canal Apex da carga e' o DELTA do dano causado
            _danoCausadoAntes = causado;
        }

        /// <summary>No castelo o portao carrega o corpo; caindo, a Queda escreve a posicao.</summary>
        private void TickNoAr(float dt)
        {
            if (Queda.Fase == Queda.NO_CASTELO)
            {
                if (_castelo == null || !_castelo.Embarcado(gameObject)) { Saltar(); return; }   // castelo saiu do mapa (ou me empurrou): ninguem fica preso
                Queda.Posicionar(transform.position);
                return;
            }
            Queda.Direcao = Locomocao.DirDoStick(Stick, YawCam);
            Queda.Tick(dt);
            transform.position = Queda.Pos;
            if (!Queda.NoAr) { NoChao = true; Loc.Vy = 0f; }
            Virar(dt);
            Tocar(Queda.Planando ? "planar" : "cair");
        }

        /// <summary>Giro do corpo. Parado gira mais rapido (pivo); mirando segue a camera.</summary>
        private void Virar(float dt)
        {
            float pivo = Mathf.Lerp(1f + Balance.Move.TurnPivotBonus, 1f, Loc.Frac);
            float yaw = transform.eulerAngles.y;
            if (YawAlvo.HasValue)
                yaw = Mathf.LerpAngle(yaw, YawAlvo.Value * Mathf.Rad2Deg, Mathf.Min(Balance.Move.TurnRateAim * pivo * dt, 1f));
            else
            {
                Vector3 d = EncararDir.sqrMagnitude > 0.0001f ? EncararDir : _dir;
                d.y = 0f;
                if (d.sqrMagnitude < 0.0001f) return;
                yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, Mathf.Min(Balance.Move.TurnRateFree * pivo * dt, 1f));
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>A UNICA fonte da animacao: derrubado > nadando > ar (planar, queda longa, pulo) > run/andar_tras com
        /// histerese > aterrissagem parada > idle. Degrau sem pulo segue a passada.</summary>
        public string AnimDeLocomocao()
        {
            if (Derrubado.Esta(this)) return "derrubado";
            if (Agua.Nadando) return Agua.Locomocao(VelocidadeHorizontal);
            if (!NoChao)
            {
                string ar = Loc.AnimNoAr();
                if (ar != null) return ar;
            }
            string chao = Loc.AnimNoChao();
            if (chao == "run") return _recuando ? "andar_tras" : chao;   // correndo, o pouso some na passada
            return Loc.PousoS > 0f ? "pousar" : chao;
        }

        private void Tocar(string clipe)
        {
            if (Visual == null) return;
            Visual.VelocidadeLocal = transform.InverseTransformDirection(Loc.Vel);   // a passada do Mecanim: frente, tras e lados
            if (clipe != Clipe) { Clipe = clipe; Visual.Play(clipe); }
            Visual.SetVelocidade(VelocidadeHorizontal);   // fim da patinacao: o Mago cadencia a passada pela velocidade real
        }

        /// <summary>Gesto transitorio (cast, pegar): sobrepoe a locomocao por `s` e volta sozinho.</summary>
        public void Gesto(string clipe, float s)
        {
            _gesto = clipe; _gestoS = s;
            if (Visual != null) { Clipe = clipe; Visual.Play(clipe); }   // retrigger POR gesto (fogo continuo reinicia o braco)
        }

        private void EscreverMana(float v)
        {
            Mana = v;
            if (EhPlayer) Bus.EmitManaChanged(Mana, Balance.Player.ManaMax);
        }

        // ---------------------------------------------------------------- verbos

        /// <summary>O ATAQUE BASICO: so' com luva, no chao, de pe', com mana e cadencia. Dano so' pelo projetil.</summary>
        public bool Atirar(Vector3 dir)
        {
            if (!PodeAgir || !PodeConjurar || !Slot.PodeAtacar || _fireCd > 0f) return false;
            if (Efeitos.De(this).StunLeft > 0f) return false;   // atordoado nao conjura
            dir.y = Mathf.Clamp(dir.y, -1f, 1f);
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            dir.Normalize();
            // o spec sai do elemento QUE VAI SAIR (senao a mana cobrada e' de um elemento e o tiro de outro): a manopla so'
            // gira depois de pagar
            Elemento el = Slot.ProximoDisparo(Elemento);
            ArmaSpec spec = Slot.Spec(el);
            if (Mana < spec.ManaCost) return false;
            Slot.ElementoDoDisparo(Elemento);
            EscreverMana(Mana - spec.ManaCost);
            _fireCd = spec.FireRate * CadenciaMult;
            Vector3 mao = transform.position + Vector3.up * ALTURA_MAO;
            Vector3 origem = Projetil.Saida(mao, dir, SAIDA_TIRO, Partida.Livre(mao, dir, SAIDA_TIRO), out bool colado);
            Projetil p = Projetil.Lancar(this, origem, dir, el, Slot);
            if (colado) p.Impacto(null);   // encostado na parede o tiro bate NELA (e a Partida descarta o morto), nao nasce do outro lado
            RegistrarProjetil(p);   // o mesmo caminho do tiro de kit (KitRunner.AoLancar)
            if (Runner != null) Runner.NotificarAtaque();   // atacar quebra a Entrelinha da Veu
            Gesto("cast", GESTO_CAST_S);
            return true;
        }

        /// <summary>Esquiva na direcao do movimento (parado: para onde olha).</summary>
        public bool Dodge() => Dodge(_dir.sqrMagnitude > 0.0001f ? _dir : transform.forward);

        public bool Dodge(Vector3 dir)
        {
            if (!PodeAgir || !Loc.Dodge(dir)) return false;
            if (Runner != null) Runner.DashIniciou = true;   // o Braco Livre da Pyra deixa fogo no dash
            if (EhPlayer) Bus.EmitDodgePerformed();   // feedback global so' da esquiva do player
            return true;
        }

        /// <summary>TATICA / SUPREMA: quem decide (cooldown, carga, telegrafia) e' o KitRunner; aqui so' o corpo pode agir.</summary>
        public bool UsarTatica() => Kit(PodeAgir && PodeConjurar && Runner != null && Runner.UsarTatica(), "tatica");
        public bool UsarSuprema() => Kit(PodeAgir && PodeConjurar && Runner != null && Runner.UsarSuprema(), "suprema");

        /// <summary>A tatica e a suprema MOVEM O CORPO (antes so' o tiro basico pedia gesto — achado de 24/09): magia de duas
        /// maos no Mecanim, o cast de sempre no legado. Quem decide e' o KitRunner; o gesto so' mostra.</summary>
        private bool Kit(bool usou, string gesto)
        {
            if (usou) Gesto(gesto, GESTO_KIT_S);
            return usou;
        }

        /// <summary>Do chao, na altura do FatorDePulo (a mola do Fizz). Conta a decolagem: o kit ve' pela borda de Pulos.</summary>
        public bool Pular()
        {
            if (!PodeAgir || !Loc.Pular(NoChao, Agua.Nadando, FatorDePulo)) return false;
            Pulos++;
            if (Visual != null) Visual.VooS = Loc.VooS;   // o ar do take cabe no voo desta decolagem (a mola do Fizz voa mais)
            return true;
        }

        /// <summary>TELEPORTE (Travessia, Danca): surge no POUSO SEGURO ate' `destino` e avisa o CharacterController. No ar
        /// (castelo, queda) ou morto, recusa. Dash e empurrao ficam para tras (Locomocao.Parar).</summary>
        public bool Teleportar(Vector3 destino)
        {
            if (!Viva || Queda.NoAr) return false;
            IRelevo relevo = Ilha.Atual != null ? Ilha.Atual.Chao : null;
            transform.position = PousoSeguro(relevo, transform.position, destino, ChaoComObstaculos.Topo);
            Physics.SyncTransforms();   // o CharacterController le' a pose nova no proximo Move
            Loc.Parar();
            NoChao = true;
            return true;
        }

        /// <summary>DASH de kit pela Locomocao (a mesma rampa da esquiva, sem o deslize do empurrao).</summary>
        public bool Impulso(Vector3 dir, float metros, float dur) => Viva && Loc.Impulso(dir, metros, dur);

        /// <summary>
        /// O POUSO SEGURO, puro: de `destino` recua pela linha ate' `de`, de metro em metro, ate' achar chao SECO (nunca o mar)
        /// e LIVRE — nada solido acima do terreno no centro nem na PEGADA do corpo (`topo` = ChaoComObstaculos.Topo). Assim
        /// ninguem surge em cima de copa (o colisor do tronco sobe 3 m DENTRO da copa), pilar ou muro, nem com meio corpo
        /// dentro de rocha. Pousa na cota MAIS ALTA do chao sob a pegada: na encosta, nada do corpo fica dentro do morro.
        /// Nada livre na linha inteira = fica onde esta'. Sem relevo, plano seco em y = 0; `topo` null = sem obstaculos.
        /// </summary>
        public static Vector3 PousoSeguro(IRelevo relevo, Vector3 de, Vector3 destino, Func<float, float, float, float> topo)
        {
            Vector3 p = new Vector3(destino.x, 0f, destino.z), alvo = new Vector3(de.x, 0f, de.z);
            int n = Mathf.CeilToInt(Vector3.Distance(p, alvo) / PASSO_POUSO);
            for (int i = 0; i <= n; i++, p = Vector3.MoveTowards(p, alvo, PASSO_POUSO))
            {
                if (relevo != null && !relevo.PodePousar(p.x, p.z)) continue;
                float y;
                if (Livre(relevo, topo, p.x, p.z, out y)) return new Vector3(p.x, y, p.z);
            }
            return de;
        }

        /// <summary>O centro e 8 pontos na borda da PEGADA sem nada solido acima do chao. `y` = a cota mais alta entre eles.</summary>
        private static bool Livre(IRelevo relevo, Func<float, float, float, float> topo, float x, float z, out float y)
        {
            y = float.NegativeInfinity;
            for (int k = 0; k <= 8; k++)
            {
                float a = k * Mathf.PI * 0.25f, r = k == 0 ? 0f : PEGADA;
                float px = x + Mathf.Cos(a) * r, pz = z + Mathf.Sin(a) * r;
                float h = relevo != null ? relevo.Altura(px, pz) : 0f;
                if (topo != null && topo(px, pz, h) > h) return false;
                y = Mathf.Max(y, h);
            }
            return true;
        }

        /// <summary>Segurar o salto no ar. Quem cobra a mana e' a Locomocao no Tick.</summary>
        public void Flutuar(bool on) { _querFlutuar = on; }

        /// <summary>PEGAR = TROCAR (a velha fica no chao). Porta unica: Loot.Pegar.</summary>
        public bool Pegar()
        {
            if (!PodeAgir || Partida.Atual == null || Partida.Atual.Loot == null) return false;
            bool ok = Partida.Atual.Loot.Pegar(Slot);
            if (ok) Gesto("pegar", ArmaSlot.GESTO_S);
            return ok;
        }

        public bool Trocar() => Pegar();

        public void Empurrar(Vector3 v) { Loc.Empurrar(v); }

        private void AoEquipar(IEntidade pawn, string armaId, string nome, string raridade, Elemento[] els)
        {
            if (!ReferenceEquals(pawn, this) || Visual == null) return;
            if (_luva != null) Destroy(_luva);
            _luva = LuvaVisual.Criar(Visual.MaoDireita, armaId, els);
            if (!_visivel) Visivel(false);   // a luva nasce escondida junto com o corpo no castelo
        }

        private void Morrer()
        {
            _morto = true;
            Stick = Vector2.zero; EncararDir = Vector3.zero; YawAlvo = null;
            Tocar("derrubado");   // deitado: o VisualDoAbate afunda o corpo, e em pe' ele descia como elevador
            _pernasYaw = 0f; _recuando = false;
            if (Visual != null)
            {
                Visual.transform.localRotation = Quaternion.identity;   // caido de pernas tortas nao: o corpo deita inteiro
                Visual.Torcao = 0f;
                Visual.SetTint(TINT_MORTO);
            }
            // o corpo afunda e some (VisualDoAbate): o colisor em pe' viraria parede invisivel para o passo, a camera e a mira
            if (_cc != null) _cc.enabled = false;
        }

        /// <summary>Ultimo chao seguro (fora do vazio): o corpo de quem cai da borda volta para ca', para o time poder revive-lo.</summary>
        public Vector3 UltimoChaoSeguro { get; private set; }

        /// <summary>RENASCER (Partida.Reviver): o corpo volta para `pos`, com mana cheia e SO' a luva base do elemento do mago
        /// (sem escudo — a Partida ja' zerou; sem manopla dupla). O Reviver() (colisor, tinta) roda quando Viva volta.</summary>
        public void Renascer(Vector3 pos)
        {
            if (_cc != null) _cc.enabled = false;
            Aterrar(pos);
            Loc.Parar();
            EscreverMana(Balance.Player.ManaMax);
            Slot.Equipar(Arma.VARINHA, null, IdentidadeMago.De(Slug).Elemento);
            if (_morto) Reviver();
            Physics.SyncTransforms();
        }

        private void Reviver()
        {
            _morto = false;
            if (_cc != null) _cc.enabled = true;
            if (Visual != null) Visual.SetTint(_tint);
        }
    }
}
