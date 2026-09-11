using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Characters
{
    /// <summary>Os clipes do contrato (Mage.gd: REQUIRED + OPTIONAL + os dois da agua + o agachado).</summary>
    public enum Clipe { Idle, Run, Cast, Cair, Planar, Pegar, Derrubado, Nadar, NadarParado, AndeAgachado }

    /// <summary>
    /// PoseCorpo num instante. Euler em GRAUS, convencao Unity (+Z = frente do mago):
    ///   Raiz/Tronco/Cabeca: +X inclina/olha para BAIXO-frente; +Y vira para a direita; Z = rolagem.
    ///   Bracos/Pernas pendem em -Y: X NEGATIVO leva o membro para a FRENTE; Z abre para fora
    ///   (BracoD +Z, BracoE -Z).
    ///   BobY em metros do corpo de referencia (1,80 m); negativo = abaixo do repouso (agachado, caido).
    /// </summary>
    public struct PoseCorpo
    {
        public Vector3 Raiz, Cabeca, Tronco, BracoE, BracoD, PernaE, PernaD;
        public float BobY;
        /// <summary>Escala da mao direita: abre no disparo (1 = repouso).</summary>
        public float MaoD;
    }

    /// <summary>
    /// ANIMACAO PROCEDURAL, pura: clipe + tempo -> PoseCorpo. Reescrita de Mage.gd (_anim_*): os mesmos beats
    /// (o cast em tres tempos, o caido que levanta a cabeca uma vez por volta, o planar que ACHA o eixo),
    /// sem AnimationPlayer — o Mago aplica a PoseCorpo nos Transforms a cada frame.
    /// Sem alocacao por frame: as trilhas sao estaticas.
    /// </summary>
    public static class PoseMago
    {
        /// <summary>s dentro do cast em que a mao chega a frente (CAST_FIRE_TIME do Godot). E' aqui que nasce o projetil.</summary>
        public const float CastFireT = 0.22f;

        // ---------------------------------------------------------------- trilhas
        sealed class Trilha
        {
            readonly float[] _t; readonly Vector3[] _v;
            public Trilha(params (float t, Vector3 v)[] ks)
            {
                _t = new float[ks.Length]; _v = new Vector3[ks.Length];
                for (int i = 0; i < ks.Length; i++) { _t[i] = ks[i].t; _v[i] = ks[i].v; }
            }
            /// <summary>Smoothstep entre vizinhos; fora das pontas segura a ponta.</summary>
            public Vector3 Em(float t)
            {
                if (t <= _t[0]) return _v[0];
                int n = _t.Length;
                if (t >= _t[n - 1]) return _v[n - 1];
                int i = 1;
                while (_t[i] < t) i++;
                float u = Mathf.InverseLerp(_t[i - 1], _t[i], t);
                return Vector3.LerpUnclamped(_v[i - 1], _v[i], u * u * (3f - 2f * u));
            }
        }

        sealed class Def
        {
            public float Dur; public bool Laco;
            public Trilha Raiz = Zero, Cabeca = Zero, Tronco = Zero, BE = BracoRepE, BD = BracoRepD,
                PE = Zero, PD = Zero, Bob = Zero, MaoD = Um;
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Vector3 Y(float y) => new Vector3(0f, y, 0f);
        static readonly Trilha Zero = new Trilha((0f, Vector3.zero));
        static readonly Trilha Um = new Trilha((0f, Vector3.one));
        /// <summary>Braco em repouso: quase reto, um pouco aberto (0.04/0.14 rad do Godot).</summary>
        static readonly Vector3 RepE = V(-2.3f, 0f, -8f), RepD = V(-2.3f, 0f, 8f);
        static readonly Trilha BracoRepE = new Trilha((0f, RepE)), BracoRepD = new Trilha((0f, RepD));

        static readonly Dictionary<Clipe, Def> _defs = new Dictionary<Clipe, Def>
        {
            // Respiro. O peso vem do ATRASO: cabeca e bracos chegam ao pico DEPOIS do tronco.
            { Clipe.Idle, new Def { Dur = 2.4f, Laco = true,
                Raiz = new Trilha((0f, V(0, 0, 0.7f)), (1.2f, V(0, 0, -0.7f)), (2.4f, V(0, 0, 0.7f))),
                Bob = new Trilha((0f, Y(0)), (1.2f, Y(0.02f)), (2.4f, Y(0))),
                Tronco = new Trilha((0f, V(-1.1f, 0, 0)), (1.2f, V(-3.2f, 0, 0)), (2.4f, V(-1.1f, 0, 0))),
                Cabeca = new Trilha((0f, Vector3.zero), (1.45f, V(-2.3f, 0, 0)), (2.4f, Vector3.zero)),
                BE = new Trilha((0f, RepE), (1.35f, V(-5.2f, 0, -10.3f)), (2.4f, RepE)),
                BD = new Trilha((0f, RepD), (1.35f, V(-5.2f, 0, 10.3f)), (2.4f, RepD)) } },

            // Corrida: quadril CAI no contato, corpo rola, braco e perna opostos.
            { Clipe.Run, new Def { Dur = 0.6f, Laco = true,
                Raiz = new Trilha((0f, V(6.9f, 0, 2.6f)), (0.3f, V(6.9f, 0, -2.6f)), (0.6f, V(6.9f, 0, 2.6f))),
                Bob = new Trilha((0f, Y(0.005f)), (0.15f, Y(0.06f)), (0.3f, Y(0.005f)), (0.45f, Y(0.06f)), (0.6f, Y(0.005f))),
                BE = new Trilha((0f, V(-34, 0, -6.9f)), (0.3f, V(34, 0, -6.9f)), (0.6f, V(-34, 0, -6.9f))),
                BD = new Trilha((0f, V(34, 0, 6.9f)), (0.3f, V(-34, 0, 6.9f)), (0.6f, V(34, 0, 6.9f))),
                PE = new Trilha((0f, V(34, 0, 0)), (0.3f, V(-34, 0, 0)), (0.6f, V(34, 0, 0))),
                PD = new Trilha((0f, V(-34, 0, 0)), (0.3f, V(34, 0, 0)), (0.6f, V(-34, 0, 0))),
                Tronco = new Trilha((0f, V(-4.6f, -5.7f, 0)), (0.3f, V(-4.6f, 5.7f, 0)), (0.6f, V(-4.6f, -5.7f, 0))),
                Cabeca = new Trilha((0f, V(-5.7f, 3.4f, 0)), (0.3f, V(-5.7f, -3.4f, 0)), (0.6f, V(-5.7f, 3.4f, 0))) } },

            // Conjuracao em tres tempos: ANTECIPACAO (recua) -> DISPARO (0.22: braco ERGUE e estende a
            // FRENTE, mao abre) -> FOLLOW-THROUGH com o braco AINDA a frente. DIRECAO.md: "enquanto
            // dispara, o braco segue erguido; ao PARAR o braco abaixa" — quem abaixa e' o Mago, no
            // blend de volta ao idle/run, nao o clipe (e disparo continuo = cast reiniciado = braco no ar).
            { Clipe.Cast, new Def { Dur = 0.55f, Laco = false,
                BD = new Trilha((0f, RepD), (0.10f, V(35.5f, 0, 18.3f)), (CastFireT, V(-83, 0, 2.3f)),
                    (0.32f, V(-87, 0, 1.1f)), (0.42f, V(-78, 0, 3)), (0.55f, V(-75, 0, 4))),
                MaoD = new Trilha((0f, Vector3.one), (0.10f, Vector3.one * 0.78f), (0.24f, Vector3.one * 1.32f),
                    (0.42f, Vector3.one * 1.12f), (0.55f, Vector3.one)),
                BE = new Trilha((0f, RepE), (0.10f, V(-9.2f, 0, -13.8f)), (CastFireT, V(21.8f, 0, -11.5f)),
                    (0.40f, V(12.6f, 0, -10.3f)), (0.55f, RepE)),
                Tronco = new Trilha((0f, V(-1.1f, 0, 0)), (0.10f, V(-5.2f, 18.3f, 0)), (CastFireT, V(1.7f, -17.2f, 0)),
                    (0.36f, V(0, -12.6f, 0)), (0.55f, V(-1.1f, 0, 0))),
                Cabeca = new Trilha((0f, Vector3.zero), (0.10f, V(5.7f, 2.9f, 0)), (0.28f, V(-6.9f, -4, 0)), (0.55f, Vector3.zero)),
                Bob = new Trilha((0f, Y(0)), (0.10f, Y(0.012f)), (CastFireT, Y(-0.03f)), (0.55f, Y(0))) } },

            // QUEDA LIVRE: barriga para baixo, bracos abertos tremendo, joelhos dobrados — nao controla o ar.
            { Clipe.Cair, new Def { Dur = 1.1f, Laco = true,
                Raiz = new Trilha((0f, V(48.7f, 0, 5.7f)), (0.55f, V(52.7f, 0, -5.7f)), (1.1f, V(48.7f, 0, 5.7f))),
                Bob = new Trilha((0f, Y(0.04f)), (0.55f, Y(-0.02f)), (1.1f, Y(0.04f))),
                BE = new Trilha((0f, V(28.6f, 0, -71.6f)), (0.35f, V(20, 0, -83)), (0.75f, V(31.5f, 0, -74.5f)), (1.1f, V(28.6f, 0, -71.6f))),
                BD = new Trilha((0f, V(28.6f, 0, 71.6f)), (0.35f, V(31.5f, 0, 74.5f)), (0.75f, V(20, 0, 83)), (1.1f, V(28.6f, 0, 71.6f))),
                Cabeca = new Trilha((0f, V(-25.8f, 2.9f, 0)), (0.55f, V(-28.6f, -2.9f, 0)), (1.1f, V(-25.8f, 2.9f, 0))),
                PE = new Trilha((0f, V(25, 0, -8)), (0.55f, V(32, 0, -10)), (1.1f, V(25, 0, -8))),
                PD = new Trilha((0f, V(30, 0, 8)), (0.55f, V(22, 0, 10)), (1.1f, V(30, 0, 8))) } },

            // PLANEIO: o corpo ACHA o eixo — menos amplitude, bracos firmes, cabeca no horizonte, pernas juntas.
            { Clipe.Planar, new Def { Dur = 2.0f, Laco = true,
                Raiz = new Trilha((0f, V(60.2f, 0, 2)), (1.0f, V(62.5f, 0, -2)), (2.0f, V(60.2f, 0, 2))),
                Bob = new Trilha((0f, Y(0.02f)), (1.0f, Y(0.05f)), (2.0f, Y(0.02f))),
                BE = new Trilha((0f, V(10.3f, 0, -84.8f)), (1.0f, V(12.6f, 0, -87.1f)), (2.0f, V(10.3f, 0, -84.8f))),
                BD = new Trilha((0f, V(10.3f, 0, 84.8f)), (1.0f, V(12.6f, 0, 87.1f)), (2.0f, V(10.3f, 0, 84.8f))),
                Cabeca = new Trilha((0f, V(-12.6f, 0, 0))),
                PE = new Trilha((0f, V(8, 0, -3))), PD = new Trilha((0f, V(8, 0, 3))) } },

            // PEGAR / ABRIR: disparo unico e curto. Agacha, o braco direito alcanca, olha o que pega, volta.
            { Clipe.Pegar, new Def { Dur = 0.65f, Laco = false,
                Bob = new Trilha((0f, Y(0)), (0.22f, Y(-0.16f)), (0.42f, Y(-0.14f)), (0.65f, Y(0))),
                Raiz = new Trilha((0f, Vector3.zero), (0.22f, V(12.6f, 0, 0)), (0.42f, V(11.5f, 0, 0)), (0.65f, Vector3.zero)),
                BD = new Trilha((0f, RepD), (0.24f, V(-54.4f, 0, 17.2f)), (0.44f, V(-45.8f, 0, 12.6f)), (0.65f, RepD)),
                BE = new Trilha((0f, RepE), (0.24f, V(14.3f, 0, -17.2f)), (0.65f, RepE)),
                Cabeca = new Trilha((0f, Vector3.zero), (0.24f, V(24, 0, 0)), (0.65f, Vector3.zero)),
                PE = new Trilha((0f, Vector3.zero), (0.22f, V(-30, 0, -6)), (0.65f, Vector3.zero)),
                PD = new Trilha((0f, Vector3.zero), (0.22f, V(-30, 0, 6)), (0.65f, Vector3.zero)) } },

            // DERRUBADO (estado sustentado, em laco). Vocabulario 10+: caido, APOIADO num braco, respirando
            // pesado; a cabeca SOBE uma vez por volta — o corpo dizendo "ainda da' tempo". Lento de proposito.
            { Clipe.Derrubado, new Def { Dur = 3.4f, Laco = true,
                Bob = new Trilha((0f, Y(-0.55f)), (1.7f, Y(-0.522f)), (3.4f, Y(-0.55f))),
                Raiz = new Trilha((0f, V(-17, 0, 23)), (1.7f, V(-15.5f, 0, 21.8f)), (3.4f, V(-17, 0, 23))),
                Tronco = new Trilha((0f, V(26.4f, -4.6f, -5.7f)), (1.9f, V(29.8f, -2.9f, -5.2f)), (3.4f, V(26.4f, -4.6f, -5.7f))),
                BE = new Trilha((0f, V(17.2f, 0, -65.9f)), (1.7f, V(19.5f, 0, -64.2f)), (3.4f, V(17.2f, 0, -65.9f))),
                BD = new Trilha((0f, V(-31.5f, 0, 24)), (1.5f, V(-17.2f, 0, 35.5f)), (2.2f, V(-24, 0, 29.8f)), (3.4f, V(-31.5f, 0, 24))),
                Cabeca = new Trilha((0f, V(23, -7, 0)), (1.4f, V(-3.4f, 9.2f, 0)), (2.1f, V(-4.6f, 5.7f, 0)), (3.4f, V(23, -7, 0))),
                PE = new Trilha((0f, V(-40, 0, -10))), PD = new Trilha((0f, V(-60, 0, 15))) } },

            // NADAR do peito (DIRECAO.md §3): corpo baixo na agua, bracadas, pernada alternada.
            { Clipe.Nadar, new Def { Dur = 1.0f, Laco = true,
                Raiz = new Trilha((0f, V(70, 0, 3)), (0.5f, V(70, 0, -3)), (1.0f, V(70, 0, 3))),
                Bob = new Trilha((0f, Y(-0.35f)), (0.5f, Y(-0.32f)), (1.0f, Y(-0.35f))),
                BE = new Trilha((0f, V(-120, 0, -15)), (0.5f, V(-20, 0, -60)), (1.0f, V(-120, 0, -15))),
                BD = new Trilha((0f, V(-120, 0, 15)), (0.5f, V(-20, 0, 60)), (1.0f, V(-120, 0, 15))),
                Cabeca = new Trilha((0f, V(-45, -5, 0)), (0.5f, V(-45, 5, 0)), (1.0f, V(-45, -5, 0))),
                PE = new Trilha((0f, V(-20, 0, 0)), (0.5f, V(20, 0, 0)), (1.0f, V(-20, 0, 0))),
                PD = new Trilha((0f, V(20, 0, 0)), (0.5f, V(-20, 0, 0)), (1.0f, V(20, 0, 0))) } },

            // Boiando parado: em pe' na agua, bracos varrendo, pernas pedalando.
            { Clipe.NadarParado, new Def { Dur = 2.0f, Laco = true,
                Raiz = new Trilha((0f, V(5, 0, 0))),
                Bob = new Trilha((0f, Y(-0.35f)), (1.0f, Y(-0.31f)), (2.0f, Y(-0.35f))),
                BE = new Trilha((0f, V(-15, 0, -70)), (1.0f, V(-25, 0, -55)), (2.0f, V(-15, 0, -70))),
                BD = new Trilha((0f, V(-15, 0, 70)), (1.0f, V(-25, 0, 55)), (2.0f, V(-15, 0, 70))),
                Cabeca = new Trilha((0f, V(-5, 0, 0)), (1.0f, V(-8, 0, 0)), (2.0f, V(-5, 0, 0))),
                PE = new Trilha((0f, V(-15, 0, 0)), (1.0f, V(15, 0, 0)), (2.0f, V(-15, 0, 0))),
                PD = new Trilha((0f, V(15, 0, 0)), (1.0f, V(-15, 0, 0)), (2.0f, V(15, 0, 0))) } },

            // Andar agachado: baixo, inclinado, passo curto; a cabeca compensa para olhar a frente.
            { Clipe.AndeAgachado, new Def { Dur = 0.8f, Laco = true,
                Bob = new Trilha((0f, Y(-0.28f)), (0.2f, Y(-0.26f)), (0.4f, Y(-0.28f)), (0.6f, Y(-0.26f)), (0.8f, Y(-0.28f))),
                Raiz = new Trilha((0f, V(22, 0, 2)), (0.4f, V(22, 0, -2)), (0.8f, V(22, 0, 2))),
                Tronco = new Trilha((0f, V(10, -4, 0)), (0.4f, V(10, 4, 0)), (0.8f, V(10, -4, 0))),
                Cabeca = new Trilha((0f, V(-14, 0, 0))),
                BE = new Trilha((0f, V(-15, 0, -6)), (0.4f, V(15, 0, -6)), (0.8f, V(-15, 0, -6))),
                BD = new Trilha((0f, V(15, 0, 6)), (0.4f, V(-15, 0, 6)), (0.8f, V(15, 0, 6))),
                PE = new Trilha((0f, V(-2, 0, 0)), (0.4f, V(-46, 0, 0)), (0.8f, V(-2, 0, 0))),
                PD = new Trilha((0f, V(-46, 0, 0)), (0.4f, V(-2, 0, 0)), (0.8f, V(-46, 0, 0))) } },
        };

        // ---------------------------------------------------------------- contrato

        public static float Duracao(Clipe c) => _defs[c].Dur;

        /// <summary>cast e pegar sao disparo unico (o cast_fired depende do fim); o resto e' estado sustentado.</summary>
        public static bool Laco(Clipe c) => _defs[c].Laco;

        /// <summary>PoseCorpo no tempo t. Laco: t da' a volta; disparo unico: segura no ultimo quadro.</summary>
        public static PoseCorpo Pose(Clipe c, float t)
        {
            Def d = _defs[c];
            if (float.IsNaN(t)) t = 0f;
            t = d.Laco ? Mathf.Repeat(t, d.Dur) : Mathf.Clamp(t, 0f, d.Dur);
            return new PoseCorpo
            {
                Raiz = d.Raiz.Em(t), Cabeca = d.Cabeca.Em(t), Tronco = d.Tronco.Em(t),
                BracoE = d.BE.Em(t), BracoD = d.BD.Em(t), PernaE = d.PE.Em(t), PernaD = d.PD.Em(t),
                BobY = d.Bob.Em(t).y, MaoD = d.MaoD.Em(t).x,
            };
        }

        /// <summary>A mao direita esta' erguida a frente (o gesto de mira/disparo)?</summary>
        public static bool BracoDAFrente(PoseCorpo p) => p.BracoD.x < -45f;

        /// <summary>Mistura de poses (o crossfade entre clipes vive aqui, puro).</summary>
        public static PoseCorpo Lerp(PoseCorpo a, PoseCorpo b, float u)
        {
            u = Mathf.Clamp01(u);
            return new PoseCorpo
            {
                Raiz = Vector3.Lerp(a.Raiz, b.Raiz, u), Cabeca = Vector3.Lerp(a.Cabeca, b.Cabeca, u),
                Tronco = Vector3.Lerp(a.Tronco, b.Tronco, u),
                BracoE = Vector3.Lerp(a.BracoE, b.BracoE, u), BracoD = Vector3.Lerp(a.BracoD, b.BracoD, u),
                PernaE = Vector3.Lerp(a.PernaE, b.PernaE, u), PernaD = Vector3.Lerp(a.PernaD, b.PernaD, u),
                BobY = Mathf.Lerp(a.BobY, b.BobY, u), MaoD = Mathf.Lerp(a.MaoD, b.MaoD, u),
            };
        }

        /// <summary>
        /// A LEI DA PATINACAO (R20): speed_scale sai da velocidade real. A escala 1 percorre RunStrideM por
        /// ciclo; grampeada em ScaleMin..ScaleMax para nunca virar moonwalk nem tremedeira.
        /// </summary>
        public static float EscalaDeCorrida(float ms)
        {
            if (!(ms > 0f)) return Balance.Anim.ScaleMin;
            float msNaEscala1 = Balance.Anim.RunStrideM / Duracao(Clipe.Run);
            return Mathf.Clamp(ms / msNaEscala1, Balance.Anim.ScaleMin, Balance.Anim.ScaleMax);
        }

        /// <summary>HISTERESE Idle/Run: entra em run acima de Enter, so' volta a idle abaixo de Exit (senao pisca).</summary>
        public static bool Correndo(bool correndo, float ms)
        {
            return correndo ? !(ms < Balance.Move.RunAnimExit) : ms > Balance.Move.RunAnimEnter;
        }

        // ---------------------------------------------------------------- aliases

        /// <summary>
        /// Nomes que resolvem cada clipe: os do contrato, os do Mixamo e os da biblioteca da Meshy
        /// (ANIM_ALIASES do Mage.gd). "mage_soell_cast" NAO e' erro: e' como o clipe vem gravado no .glb.
        /// </summary>
        static readonly Dictionary<Clipe, string[]> _aliases = new Dictionary<Clipe, string[]>
        {
            { Clipe.Idle, new[] { "idle", "Armature|Idle", "mixamo.com", "standing_idle", "breathing_idle",
                "idle_01", "idle_loop", "idle_02", "idle_03" } },
            { Clipe.Run, new[] { "run", "Running", "Armature|Run", "Armature|Running", "Run Forward",
                "running_forward", "locomotion_run", "sprint", "jog", "fast_run", "walk", "Walking" } },
            { Clipe.Cast, new[] { "cast", "Spellcast", "Spell Cast", "spell_cast", "casting", "magic_cast",
                "Attack", "attack1", "Standing 1H Magic Attack", "magic_attack", "shoot", "fireball",
                "Armature|Cast", "Armature|Attack", "mage_soell_cast", "mage_spell_cast", "Mage Spell Cast" } },
            { Clipe.Cair, new[] { "cair", "fall", "falling", "freefall", "free_fall", "skydive", "Skydiving", "air", "jump_loop" } },
            { Clipe.Planar, new[] { "planar", "glide", "gliding", "parachute", "wingsuit", "hover", "Hovering", "flying" } },
            { Clipe.Pegar, new[] { "pegar", "pickup", "pick_up", "Picking Up", "grab", "interact", "Interacting", "loot", "crouch_pickup" } },
            { Clipe.Derrubado, new[] { "derrubado", "downed", "knocked", "Knocked Down", "knockdown", "crawl",
                "crawling", "wounded", "injured", "dying", "getting_up", "lying" } },
            { Clipe.Nadar, new[] { "nadar", "swim", "swimming", "swim_forward", "breaststroke", "freestyle" } },
            { Clipe.NadarParado, new[] { "nadar_parado", "swim_idle", "treading", "treading_water", "Water_Idle", "float", "floating" } },
            { Clipe.AndeAgachado, new[] { "ande_agachado", "crouch_walk", "crouch", "crouching", "sneak", "sneaking", "Crouched Walking" } },
        };

        static readonly Dictionary<string, Clipe> _porNome = MontaAliases();

        static Dictionary<string, Clipe> MontaAliases()
        {
            Dictionary<string, Clipe> d = new Dictionary<string, Clipe>();
            foreach (KeyValuePair<Clipe, string[]> kv in _aliases)
                foreach (string a in kv.Value) d[Normaliza(a)] = kv.Key;
            return d;
        }

        /// <summary>"Armature|Spell Cast" -> "spell_cast": tira o prefixo do rig, baixa a caixa, espaco/hifen viram '_'.</summary>
        public static string Normaliza(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return "";
            string s = nome;
            int pipe = s.LastIndexOf('|');
            if (pipe >= 0 && pipe < s.Length - 1) s = s.Substring(pipe + 1);
            s = s.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
            while (s.Contains("__")) s = s.Replace("__", "_");
            return s;
        }

        /// <summary>Nome (contrato, Mixamo ou Meshy) -> Clipe; null se nao mapeia.</summary>
        public static Clipe? Alias(string nome)
        {
            Clipe c;
            if (_porNome.TryGetValue(Normaliza(nome), out c)) return c;
            return null;
        }

        /// <summary>Todos os nomes reconhecidos (para teste e para casar com clipes de um modelo externo).</summary>
        public static IEnumerable<string> TodosOsAliases()
        {
            foreach (string[] arr in _aliases.Values) foreach (string a in arr) yield return a;
        }

        /// <summary>Aliases de UM clipe, na ordem de preferencia (o primeiro que o modelo tiver, ganha).</summary>
        public static string[] AliasesDe(Clipe c) => _aliases[c];

        public static readonly Clipe[] Todos =
        {
            Clipe.Idle, Clipe.Run, Clipe.Cast, Clipe.Cair, Clipe.Planar, Clipe.Pegar,
            Clipe.Derrubado, Clipe.Nadar, Clipe.NadarParado, Clipe.AndeAgachado,
        };

        /// <summary>Para onde um opcional cai num modelo externo que nao o tem (ANIM_FALLBACK do Godot).</summary>
        public static Clipe Substituto(Clipe c)
        {
            switch (c)
            {
                case Clipe.Pegar: return Clipe.Cast;
                case Clipe.Nadar: return Clipe.Run;        // meio submerso com a passada: era o que o jogo tinha
                case Clipe.AndeAgachado: return Clipe.Run;
                default: return Clipe.Idle;
            }
        }
    }
}
