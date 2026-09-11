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
    /// em Partida.Tick. E' o IConjurador dos kits: entrega mira, chao, mana (que o kit LE') e os alvos da arena.
    /// </summary>
    public class Pawn : MonoBehaviour, IConjurador
    {
        public const float ALTURA_MAO = 1.4f;
        public const float SAIDA_TIRO = 0.9f;
        public const float GESTO_CAST_S = 0.3f;
        public static readonly Color TINT_MORTO = new Color(0.25f, 0.25f, 0.3f);

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
        private bool _querFlutuar, _morto, _visivel;
        private Color _tint = Color.white;
        private GameObject _luva;
        // relogios dos estados que so' o corpo resolve (IConjurador.AplicarEstado)
        private float _intangivelS, _silencioS;
        private float _danoCausadoAntes;
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

        // ---------------------------------------------------------------- IConjurador
        /// <summary>Mirando (player) e' o yaw da camera; senao o corpo (EncararDir do bot) ou a frente.</summary>
        public Vector3 DirecaoDaMira => YawAlvo.HasValue
            ? new Vector3(Mathf.Sin(YawAlvo.Value), 0f, Mathf.Cos(YawAlvo.Value))
            : (EncararDir.sqrMagnitude > 0.0001f ? EncararDir : transform.forward);

        public Func<Vector3, float, IEntidade[]> AlvosNoRaio => _buscador;

        /// <summary>"intangivel" = i-frames pelo relogio (a mascara de colisao fisica fica para depois: o Acerto da Partida
        /// ja' respeita i-frames); "silencio" = sem conjurar. Renova para o maior prazo, nunca encurta.</summary>
        public void AplicarEstado(string nome, float dur)
        {
            if (nome == "intangivel") _intangivelS = Mathf.Max(_intangivelS, dur);
            else if (nome == "silencio") _silencioS = Mathf.Max(_silencioS, dur);
        }

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

        /// <summary>Um corpo novo, invisivel, no castelo (`Embarcar`) ou no chao (`Aterrar`).</summary>
        public static Pawn Criar(Transform pai, string slug, bool ehPlayer)
        {
            var go = new GameObject(Kits.De(slug).Nome);
            if (pai != null) go.transform.SetParent(pai, false);
            var cc = go.AddComponent<CharacterController>();
            cc.height = Partida.ALTURA_CORPO; cc.radius = Partida.RAIO_CORPO;
            cc.center = new Vector3(0f, Partida.ALTURA_CORPO * 0.5f, 0f);
            cc.slopeLimit = 50f; cc.stepOffset = 0.4f;
            var p = go.AddComponent<Pawn>();
            p.Montar(slug, ehPlayer);
            return p;
        }

        private void Montar(string slug, bool ehPlayer)
        {
            Slug = slug ?? "";
            EhPlayer = ehPlayer;
            _cc = GetComponent<CharacterController>();
            Vital = new Vitalidade(Balance.Player.Hp);
            Slot = new ArmaSlot(this) { AutoUpgrade = !ehPlayer };   // decisao 5: o player equipa apertando PEGAR
            Loc = new Locomocao();
            IRelevo relevo = Ilha.Atual != null ? Ilha.Atual.Relevo : null;
            Agua = new Agua(relevo);
            Queda = new Queda(relevo, transform.position, ehPlayer);
            // o kit nasce com o corpo: KitBound sai daqui (so' player) — a HUD precisa existir ANTES do Pawn
            Runner = new KitRunner(Slug, this) { Slot = Slot, AoLancar = RegistrarProjetil, ProjeteisVivos = ProjeteisDaArena };
            Visual = Mago.Criar(transform, Slug);   // fiacao defensiva mora no Mago (modelo ausente -> procedural)
            Visivel(false);
        }

        void OnEnable()
        {
            Bus.WeaponEquipped += AoEquipar;
        }

        void OnDisable()
        {
            Bus.WeaponEquipped -= AoEquipar;
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
            if (!Viva) { if (!_morto) Morrer(); return; }
            if (_morto) Reviver();   // boneco de treino que a Partida resetou

            if (Mana < Balance.Player.ManaMax) EscreverMana(Mathf.Min(Mana + Balance.Player.ManaRegen * dt, Balance.Player.ManaMax));
            _fireCd = Mathf.Max(_fireCd - dt, 0f);
            _gestoS = Mathf.Max(_gestoS - dt, 0f);

            Agua.Tick(dt, transform.position);
            Efeitos.EstadoAlvo est = Efeitos.De(this);
            float velMax = VelocidadeMaxima(Agua.Fator, FatorDoTerrenoReativo(transform.position), est.StatusMult, Derrubado.FatorVelocidade(this));
            _dir = Locomocao.DirDoStick(Stick, YawCam);
            bool podeFlutuar = _querFlutuar && Derrubado.PodeAgir(this);
            float mana = Mana;
            Vector3 desloc = Loc.Tick(dt, _dir, NoChao, Agua.Nadando, velMax, est.StunLeft > 0f, podeFlutuar, ref mana);
            if (mana != Mana) EscreverMana(mana);
            // o corpo e' a fonte UNICA dos i-frames: esquiva e intangivel (o kit pede por AplicarEstado, nunca escreve aqui)
            est.IframesLeft = Mathf.Max(Loc.IframesLeft, _intangivelS);
            if (Agua.Nadando) desloc.y = Agua.Flutuar(transform.position, dt).y - transform.position.y;

            if (_cc != null && _cc.enabled) { _cc.Move(desloc); NoChao = _cc.isGrounded; }
            else { transform.position += desloc; NoChao = false; }
            // rede de seguranca: a verdade do chao e' a ilha (sem colisor, ou atravessou a malha)
            Vector3 pos = transform.position;
            float chao = Ilha.AlturaDoChao(pos.x, pos.z);
            if (pos.y < chao) { transform.position = new Vector3(pos.x, chao, pos.z); NoChao = true; }

            Virar(dt);
            Loc.AtualizarBank(transform.eulerAngles.y * Mathf.Deg2Rad, dt);
            if (Visual != null) Visual.transform.localRotation = Quaternion.Euler(0f, 0f, -Loc.Bank * Mathf.Rad2Deg);
            Tocar(_gestoS > 0f ? _gesto : AnimDeLocomocao());
        }

        /// <summary>Relogios do kit e dos estados do corpo. Roda ate' no ar (a carga da suprema ja' se barra por NoChao).</summary>
        private void TickKit(float dt)
        {
            _intangivelS = Mathf.Max(_intangivelS - dt, 0f);
            _silencioS = Mathf.Max(_silencioS - dt, 0f);
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

        /// <summary>A UNICA fonte da animacao: derrubado > nadando > ar > run/idle com histerese.</summary>
        public string AnimDeLocomocao()
        {
            if (Derrubado.Esta(this)) return "derrubado";
            if (Agua.Nadando) return Agua.Locomocao(VelocidadeHorizontal);
            if (!NoChao && (Loc.Flutuando || Loc.Vy < -Locomocao.QUEDA_ANIM_V)) return Loc.Flutuando ? "planar" : "cair";
            return Loc.AnimNoChao();
        }

        private void Tocar(string clipe)
        {
            if (Visual == null) return;
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
            // o spec sai do elemento QUE VAI SAIR (senao a mana cobrada e' de um elemento e o tiro de outro)
            ArmaSpec spec = Slot.Spec(Slot.ElementoDaLuva ?? Elemento);
            if (Mana < spec.ManaCost) return false;
            Elemento el = Slot.ElementoDoDisparo(Elemento);
            EscreverMana(Mana - spec.ManaCost);
            _fireCd = spec.FireRate * CadenciaMult;
            Vector3 origem = transform.position + Vector3.up * ALTURA_MAO + dir * SAIDA_TIRO;
            Projetil p = Projetil.Lancar(this, origem, dir, el, Slot);
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
        public bool UsarTatica() => PodeAgir && PodeConjurar && Runner != null && Runner.UsarTatica();
        public bool UsarSuprema() => PodeAgir && PodeConjurar && Runner != null && Runner.UsarSuprema();

        public bool Pular() => PodeAgir && Loc.Pular(NoChao, Agua.Nadando);

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
            Tocar("idle");
            if (Visual != null) Visual.SetTint(TINT_MORTO);
        }

        private void Reviver()
        {
            _morto = false;
            if (Visual != null) Visual.SetTint(_tint);
        }
    }
}
