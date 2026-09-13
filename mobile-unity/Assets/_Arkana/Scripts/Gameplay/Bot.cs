using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// OS SENTIDOS DO BOT, puros (Bot.gd, ordem do Diretor 26/08). Nasce CEGO. Quatro canais, cada um com
    /// contra-jogada legivel: VISTO (&lt;12 m), OUVIDO (&lt;18 m so' quem se MOVE — ficar parado esconde),
    /// DISPARO (&lt;30 m, Bus.Disparo — conjurar denuncia), REVIDE (tomar dano ensina quem bateu, sem limite).
    /// FFA: qualquer mago e' presa. Memoria curta: alvo morto ou alem de MEMORIA e' esquecido.
    /// So' procura alvo NOVO quando esta' sem nenhum (senao vira pinball entre passantes).
    /// OCULTO (invisivel, penumbra parada — o corpo decide): a visao so' o pega colado (VISAO_OCULTO, o brilho de perto) e os
    /// passos nao o localizam; o alvo que some e' largado e o bot vai ate' onde o viu. DISPARO e REVIDE continuam valendo.
    /// </summary>
    public sealed class PercepcaoBot
    {
        public const float VISAO = 12f, AUDICAO_PASSOS = 18f, AUDICAO_DISPARO = 30f, MEMORIA = 30f;
        /// <summary>m em que o oculto ainda se ve' (o brilho do invisivel, o vulto da penumbra colado) — KNOB.</summary>
        public const float VISAO_OCULTO = 2.5f;
        /// <summary>s entre varreduras — 12 bots a 2 Hz custa nada.</summary>
        public const float PERCEPCAO_S = 0.5f;
        /// <summary>m/s acima do qual os passos entregam.</summary>
        public const float PASSOS_V = 1f;

        public IEntidade Alvo { get; private set; }
        /// <summary>Para onde ir atras de um som (a decisao consome e zera).</summary>
        public Vector3? Pista { get; private set; }

        private readonly IEntidade _eu;
        private float _acc;
        private bool _ligado;

        public PercepcaoBot(IEntidade eu) { _eu = eu; }

        /// <summary>Liga os ouvidos no Bus (e desliga com Desligar; Bus.Reset tambem os cala).</summary>
        public void Ligar()
        {
            if (_ligado) return;
            _ligado = true;
            Bus.Disparo += OuvirDisparo;
            Bus.DamageApplied += Revidar;
        }

        public void Desligar()
        {
            _ligado = false;
            Bus.Disparo -= OuvirDisparo;
            Bus.DamageApplied -= Revidar;
        }

        public void Esquecer() { Alvo = null; }
        public Vector3? ConsumirPista() { Vector3? p = Pista; Pista = null; return p; }

        public bool AlvoVivo => Alvo != null && Alvo.Vital != null && Alvo.Vital.Viva;

        /// <summary>Relogio: varre a cada PERCEPCAO_S. `velocidadeDe` = m/s horizontal de cada mago (passos); `ocultoDe` = a
        /// visao nao o pega (null = ninguem oculto).</summary>
        public void Tick(float dt, IList<IEntidade> magos, Func<IEntidade, float> velocidadeDe, Func<IEntidade, bool> ocultoDe = null)
        {
            _acc += dt;
            if (_acc < PERCEPCAO_S) return;
            _acc = 0f;
            Varrer(magos, velocidadeDe, ocultoDe);
        }

        public void Varrer(IList<IEntidade> magos, Func<IEntidade, float> velocidadeDe, Func<IEntidade, bool> ocultoDe = null)
        {
            if (Alvo != null && (!AlvoVivo || Dist(Alvo.Pos) > MEMORIA)) Alvo = null;
            // sumiu da vista: larga o alvo e vai ate' onde o viu por ultimo (disparo e revide o devolvem)
            if (Alvo != null && ocultoDe != null && ocultoDe(Alvo) && Dist(Alvo.Pos) >= VISAO_OCULTO) { Pista = Alvo.Pos; Alvo = null; }
            if (Alvo != null || magos == null) return;
            IEntidade melhor = null;
            float melhorD = float.PositiveInfinity;
            for (int i = 0; i < magos.Count; i++)
            {
                IEntidade p = magos[i];
                if (p == null || p == _eu || p.Vital == null || !p.Vital.Viva) continue;
                float d = Dist(p.Pos);
                if (d >= melhorD) continue;
                bool oculto = ocultoDe != null && ocultoDe(p);
                bool visto = d < (oculto ? VISAO_OCULTO : VISAO);
                bool ouvido = !oculto && d < AUDICAO_PASSOS && velocidadeDe != null && velocidadeDe(p) > PASSOS_V;
                if (visto || ouvido) { melhor = p; melhorD = d; }
            }
            if (melhor != null) Alvo = melhor;
        }

        /// <summary>Conjurou perto = entregou a posicao. So' LARGA o alvo atual se o disparo veio de MAIS PERTO.</summary>
        public void OuvirDisparo(IEntidade quem, Vector3 pos)
        {
            if (quem == null || quem == _eu || !Vivo) return;
            float d = Dist(pos);
            if (d >= AUDICAO_DISPARO) return;
            if (AlvoVivo && d >= Dist(Alvo.Pos)) return;
            Alvo = quem;
            Pista = pos;
        }

        /// <summary>Tomou dano = aprendeu quem bateu, mesmo fora de toda audicao.</summary>
        public void Revidar(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool escudo)
        {
            if (alvo != _eu || fonte == null || fonte == _eu || !Vivo) return;
            Alvo = fonte;
            Pista = fonte.Pos;
        }

        private bool Vivo => _eu != null && _eu.Vital != null && _eu.Vital.Viva;
        private float Dist(Vector3 p) => Vector3.Distance(_eu.Pos, p);
    }

    /// <summary>
    /// A DECISAO DO BOT, pura e determinista por seed: desarmado CACA a luva mais proxima (a Lei das Luvas vale
    /// para ele); armado persegue, encara e atira com erro; foge da zona; loot no caminho e' do auto-upgrade
    /// (Loot.TentarAutoUpgrade, na casca); salta do castelo num instante sorteado da rota.
    /// </summary>
    public sealed class DecisaoBot
    {
        public const float CHASE_DIST = 20f, ATTACK_DIST = 12f;
        /// <summary>rad — KNOB: erro de mira do bot.</summary>
        public const float AIM_SPREAD = 0.12f;
        /// <summary>KNOB: cadencia do bot vs player.</summary>
        public const float FIRE_RATE_MULT = 4f;
        public const float DODGE_CHANCE = 0.3f, DODGE_ROLL_S = 1.4f;
        /// <summary>Fracao da rota do castelo em que o bot pode saltar (nem na ponta de entrada, nem em fila no fim).</summary>
        public const float SALTO_MIN = 0.18f, SALTO_MAX = 0.86f;
        public const float CHEGOU_M = 1.5f;

        public struct Saida
        {
            public Vector3 Dir;          // intencao de movimento (0..1)
            public Vector3 Encarar;      // zero = encara o movimento
            public bool Atirar;
            public Vector3 DirTiro;
            public bool Esquivar;
            public Vector3 DirEsquiva;
        }

        /// <summary>Instante (fracao da rota) em que salta do castelo.</summary>
        public readonly float SaltoEm;
        public Vector3 Destino { get; private set; }

        private readonly System.Random _rng;
        private float _repick, _dodgeRoll;

        public DecisaoBot(int seed)
        {
            _rng = new System.Random(seed);
            SaltoEm = Faixa(SALTO_MIN, SALTO_MAX);
        }

        /// <summary>Destino imposto de fora (zona, bau, som): sobrepoe o vagar e segura o repique.</summary>
        public void IrPara(Vector3 pos) { Destino = pos; _repick = 4f; }

        /// <summary>A luva mais proxima no chao. Sem teto: um bot desarmado atravessa o mapa por uma arma.</summary>
        public static LootItem LuvaMaisPerto(Vector3 pos, IList<LootItem> itens)
        {
            LootItem melhor = null;
            float d2 = float.PositiveInfinity;
            if (itens == null) return null;
            for (int i = 0; i < itens.Count; i++)
            {
                float dd = (itens[i].Pos - pos).sqrMagnitude;
                if (dd < d2) { d2 = dd; melhor = itens[i]; }
            }
            return melhor;
        }

        public Saida Decidir(float dt, Vector3 pos, bool armado, IEntidade alvo, Vector3? pista, IList<LootItem> loot, Zona zona, Vector3? bau, bool podeAtirar)
        {
            _repick -= dt;
            _dodgeRoll -= dt;
            var s = new Saida();
            if (pista.HasValue) IrPara(pista.Value);
            bool vivo = alvo != null && alvo.Vital != null && alvo.Vital.Viva;
            float dist = vivo ? Vector3.Distance(pos, alvo.Pos) : float.PositiveInfinity;
            bool foraDaZona = zona != null && zona.DevePuxar(pos);
            if (foraDaZona) IrPara(zona.Centro);   // a tempestade manda mais que a presa
            else if (bau.HasValue && Vector3.Distance(pos, bau.Value) < BauCelestial.ATRAIR_BOT_M) IrPara(bau.Value);   // o farol atrai

            if (armado && vivo && dist < ATTACK_DIST)
            {
                s.Encarar = Plano(alvo.Pos - pos);
                if (podeAtirar)
                {
                    s.Atirar = true;
                    Vector3 de = pos + Vector3.up * Pawn.ALTURA_MAO, para = alvo.Pos + Vector3.up * 1.2f;
                    s.DirTiro = GirarY((para - de).normalized, Faixa(-AIM_SPREAD, AIM_SPREAD));
                }
                if (foraDaZona) s.Dir = Plano(Destino - pos);
            }
            else if (armado && vivo && dist < CHASE_DIST && !foraDaZona)
            {
                s.Dir = Plano(alvo.Pos - pos);
            }
            else
            {
                if (_repick <= 0f || Vector3.Distance(pos, Destino) < CHEGOU_M)
                {
                    _repick = Faixa(2f, 5f);
                    Destino = pos + new Vector3(Faixa(-12f, 12f), 0f, Faixa(-12f, 12f));
                    if (!armado)
                    {
                        LootItem l = LuvaMaisPerto(pos, loot);   // DESARMADO, a prioridade e' ACHAR LUVA
                        if (l != null) { Destino = l.Pos; _repick = 6f; }
                    }
                }
                s.Dir = Plano(Destino - pos);
            }
            if (armado && vivo && dist < CHASE_DIST && _dodgeRoll <= 0f)
            {
                _dodgeRoll = DODGE_ROLL_S;
                if (_rng.NextDouble() < DODGE_CHANCE)
                {
                    s.Esquivar = true;
                    s.DirEsquiva = Vector3.Cross(Plano(alvo.Pos - pos), Vector3.up) * (_rng.NextDouble() < 0.5 ? 1f : -1f);
                }
            }
            return s;
        }

        private float Faixa(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        /// <summary>Gira em torno de Y por `rad` (trig pura: a decisao nao encosta em Quaternion nativo).</summary>
        private static Vector3 GirarY(Vector3 v, float rad)
        {
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.0001f ? Vector3.zero : v.normalized;
        }
    }

    /// <summary>Casca: percepcao + decisao sobre um Pawn. Nome = Kits.De(slug).Nome; tint so' em slug sem ficha.</summary>
    public sealed class Bot : MonoBehaviour
    {
        public static readonly Color[] TINTS =
        {
            new Color(0.9f, 0.3f, 0.3f), new Color(0.3f, 0.7f, 0.9f), new Color(0.4f, 0.85f, 0.4f),
            new Color(0.95f, 0.8f, 0.3f), new Color(0.75f, 0.4f, 0.9f), new Color(0.95f, 0.55f, 0.25f),
        };

        public Pawn Pawn { get; private set; }
        public PercepcaoBot Percepcao { get; private set; }
        public DecisaoBot Decisao { get; private set; }
        private Castelo _castelo;

        public static Bot Criar(Transform pai, string slug, int seed)
        {
            Pawn pawn = Pawn.Criar(pai, slug, false);
            pawn.CadenciaMult = DecisaoBot.FIRE_RATE_MULT;
            var b = pawn.gameObject.AddComponent<Bot>();
            b.Pawn = pawn;
            b.Percepcao = new PercepcaoBot(pawn);
            b.Decisao = new DecisaoBot(seed);
            b.Percepcao.Ligar();
            Kits.KitDef kit = Kits.De(slug);
            if (!Kits.Magos.ContainsKey(kit.Slug)) pawn.SetTint(TINTS[((seed % TINTS.Length) + TINTS.Length) % TINTS.Length]);
            return b;
        }

        public void Embarcar(Castelo castelo) { _castelo = castelo; Pawn.Embarcar(castelo); }

        void OnEnable() { if (Percepcao != null) Percepcao.Ligar(); }
        void OnDisable() { if (Percepcao != null) Percepcao.Desligar(); }

        void Update()
        {
            if (Pawn == null || !Pawn.Viva) return;
            if (Pawn.Queda.NoAr)
            {
                // salta pelo relogio da rota (nao tem dedo); sem castelo, cai de onde esta'
                if (Pawn.Queda.Fase == Queda.NO_CASTELO && (_castelo == null || _castelo.Progresso >= Decisao.SaltoEm)) Pawn.Saltar();
                return;
            }
            Partida m = Partida.Atual;
            IList<IEntidade> arena = m != null ? m.Arena : null;
            Percepcao.Tick(Time.deltaTime, arena, VelocidadeDe, OcultoDe);
            IList<LootItem> loot = m != null && m.Loot != null ? m.Loot.Itens : null;
            if (m != null && m.Loot != null) m.Loot.TentarAutoUpgrade(Pawn.Slot);   // loot no caminho: so' tier maior
            Vector3? bau = m != null && m.Bau != null && m.Bau.PodeAbrir ? m.Bau.Pos : (Vector3?)null;
            DecisaoBot.Saida s = Decisao.Decidir(Time.deltaTime, Pawn.Pos, Pawn.Slot.Armado, Percepcao.Alvo, Percepcao.ConsumirPista(),
                loot, m != null ? m.Zona : null, bau, Pawn.PodeAgir);
            Pawn.YawCam = 0f;
            Pawn.Stick = new Vector2(s.Dir.x, s.Dir.z);
            Pawn.EncararDir = s.Encarar;
            if (s.Esquivar) Pawn.Dodge(s.DirEsquiva);
            if (s.Atirar) Pawn.Atirar(s.DirTiro);
        }

        /// <summary>O que os passos entregam: nada de quem anda sem som (Passo de Veludo).</summary>
        private static float VelocidadeDe(IEntidade e)
        {
            var p = e as Pawn;
            return p != null && !p.SemPassos ? p.VelocidadeHorizontal : 0f;
        }

        private static bool OcultoDe(IEntidade e)
        {
            var p = e as Pawn;
            return p != null && p.Oculto;
        }
    }
}
