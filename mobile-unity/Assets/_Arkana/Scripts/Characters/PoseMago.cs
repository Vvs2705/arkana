using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Characters
{
    /// <summary>Os clipes do contrato (Mage.gd: REQUIRED + OPTIONAL + os dois da agua + o agachado) e os da onda 15B:
    /// o pulo em duas fases (Pular = o ar; Pousar = a aterrissagem, o MESMO take do Regular_Jump a partir do pouso) e o
    /// andar para tras de quem recua mirando.</summary>
    public enum Clipe { Idle, Run, Cast, Cair, Planar, Pegar, Derrubado, Nadar, NadarParado, AndeAgachado, Pular, Pousar, AndarTras }

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

            // O AR DO PULO (sustentado ate' o chao): joelhos recolhidos, bracos abertos para o equilibrio. Quem sobe e' o
            // Pawn; a pose so' nao pode parecer parada.
            { Clipe.Pular, new Def { Dur = 0.8f, Laco = true,
                Raiz = new Trilha((0f, V(8, 0, 0))),
                Bob = new Trilha((0f, Y(-0.04f)), (0.4f, Y(-0.07f)), (0.8f, Y(-0.04f))),
                BE = new Trilha((0f, V(-35, 0, -30)), (0.4f, V(-48, 0, -40)), (0.8f, V(-35, 0, -30))),
                BD = new Trilha((0f, V(-35, 0, 30)), (0.4f, V(-48, 0, 40)), (0.8f, V(-35, 0, 30))),
                Cabeca = new Trilha((0f, V(-6, 0, 0))),
                PE = new Trilha((0f, V(-48, 0, -4)), (0.4f, V(-38, 0, -4)), (0.8f, V(-48, 0, -4))),
                PD = new Trilha((0f, V(-22, 0, 4)), (0.4f, V(-30, 0, 4)), (0.8f, V(-22, 0, 4))) } },

            // A ATERRISSAGEM (disparo unico, curta): absorve o impacto dobrando e volta ao repouso.
            { Clipe.Pousar, new Def { Dur = 0.5f, Laco = false,
                Bob = new Trilha((0f, Y(-0.04f)), (0.12f, Y(-0.18f)), (0.5f, Y(0))),
                Raiz = new Trilha((0f, V(6, 0, 0)), (0.12f, V(14, 0, 0)), (0.5f, Vector3.zero)),
                BE = new Trilha((0f, V(-35, 0, -30)), (0.12f, V(-20, 0, -22)), (0.5f, RepE)),
                BD = new Trilha((0f, V(-35, 0, 30)), (0.12f, V(-20, 0, 22)), (0.5f, RepD)),
                Cabeca = new Trilha((0f, V(-6, 0, 0)), (0.12f, V(8, 0, 0)), (0.5f, Vector3.zero)),
                PE = new Trilha((0f, V(-30, 0, -5)), (0.12f, V(-28, 0, -6)), (0.5f, Vector3.zero)),
                PD = new Trilha((0f, V(-26, 0, 5)), (0.12f, V(-28, 0, 6)), (0.5f, Vector3.zero)) } },

            // ANDAR PARA TRAS (recuando mirando): passo curto, tronco um pouco para tras, bracos soltos.
            { Clipe.AndarTras, new Def { Dur = 0.8f, Laco = true,
                Raiz = new Trilha((0f, V(-4, 0, 1.5f)), (0.4f, V(-4, 0, -1.5f)), (0.8f, V(-4, 0, 1.5f))),
                Bob = new Trilha((0f, Y(0)), (0.2f, Y(0.03f)), (0.4f, Y(0)), (0.6f, Y(0.03f)), (0.8f, Y(0))),
                BE = new Trilha((0f, V(12, 0, -8)), (0.4f, V(-12, 0, -8)), (0.8f, V(12, 0, -8))),
                BD = new Trilha((0f, V(-12, 0, 8)), (0.4f, V(12, 0, 8)), (0.8f, V(-12, 0, 8))),
                Cabeca = new Trilha((0f, V(4, 0, 0))),
                PE = new Trilha((0f, V(-22, 0, 0)), (0.4f, V(22, 0, 0)), (0.8f, V(-22, 0, 0))),
                PD = new Trilha((0f, V(22, 0, 0)), (0.4f, V(-22, 0, 0)), (0.8f, V(22, 0, 0))) } },
        };

        // ---------------------------------------------------------------- contrato

        public static float Duracao(Clipe c) => _defs[c].Dur;

        /// <summary>cast, pegar e pousar sao disparo unico (o cast_fired depende do fim); o resto e' estado sustentado —
        /// o Pular tambem (segura o ar ate' o chao), embora o take dele toque em ClampForever no externo.</summary>
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

        /// <summary>A mesma lei para o ANDAR PARA TRAS: o clipe de `comprimento` s anda BackStrideM por volta.</summary>
        public static float EscalaDeRecuo(float ms, float comprimento)
        {
            if (!(ms > 0f) || !(comprimento > 0f)) return Balance.Anim.ScaleMin;
            return Mathf.Clamp(ms * comprimento / Balance.Anim.BackStrideM, Balance.Anim.ScaleMin, Balance.Anim.BackScaleMax);
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
            // Combat_Stance ("Combate Ocioso" na biblioteca da Meshy) PRIMEIRO: o Idle_02 ("Parado 1") levanta o braco no
            // meio do laco — o elenco inteiro acenava junto na foto de 12/09
            { Clipe.Idle, new[] { "Combat_Stance", "idle", "Armature|Idle", "mixamo.com", "standing_idle", "breathing_idle",
                "idle_01", "idle_loop", "idle_02", "idle_03" } },
            { Clipe.Run, new[] { "run", "Running", "Armature|Run", "Armature|Running", "Run Forward",
                "running_forward", "locomotion_run", "sprint", "jog", "fast_run", "walk", "Walking" } },
            { Clipe.Cast, new[] { "cast", "Spellcast", "Spell Cast", "spell_cast", "casting", "magic_cast",
                "Attack", "attack1", "Standing 1H Magic Attack", "magic_attack", "shoot", "fireball",
                "Armature|Cast", "Armature|Attack", "mage_soell_cast", "mage_spell_cast", "Mage Spell Cast" } },
            { Clipe.Cair, new[] { "cair", "fall", "Fall1", "falling", "freefall", "free_fall", "skydive", "Skydiving", "air", "jump_loop" } },
            // o UUID e' o "Planar Arkana" (Texto para Movimento na conta da Meshy, 12/09): o FBX grava a take com o id.
            // NAO usar o "Planar horizontal v2" (01a040ba-...): mergulha de cabeca para baixo — o Diretor reprovou.
            { Clipe.Planar, new[] { "planar", "glide", "gliding", "parachute", "wingsuit", "hover", "Hovering", "flying",
                "01a093df-6ebc-763c-9c24-9f20123e366c" } },
            { Clipe.Pegar, new[] { "pegar", "pickup", "pick_up", "Picking Up", "grab", "interact", "Interacting", "loot", "crouch_pickup",
                "Collect_Object" } },
            { Clipe.Derrubado, new[] { "derrubado", "downed", "knocked", "Knocked Down", "knockdown", "crawl",
                "crawling", "wounded", "injured", "dying", "getting_up", "lying", "Prone_Reach_Help" } },
            { Clipe.Nadar, new[] { "nadar", "swim", "swimming", "swim_forward", "breaststroke", "freestyle" } },
            { Clipe.NadarParado, new[] { "nadar_parado", "swim_idle", "treading", "treading_water", "Water_Idle", "float", "floating" } },
            { Clipe.AndeAgachado, new[] { "ande_agachado", "crouch_walk", "crouch", "crouching", "sneak", "sneaking", "Crouched Walking",
                "Cautious_Crouch_Walk_Forward" } },
            // "Salto Regular" da biblioteca da Meshy (take Regular_Jump). NAO confundir com "jump_loop", que e' queda.
            { Clipe.Pular, new[] { "pular", "Regular_Jump", "jump", "jumping", "jump_up", "standing_jump" } },
            // a aterrissagem e' o PROPRIO take do pulo a partir do pouso (Mago.MontarExterno); o nome so' serve ao contrato
            { Clipe.Pousar, new[] { "pousar" } },
            // "Andar para tras" da biblioteca da Meshy (take Walk_Backward)
            { Clipe.AndarTras, new[] { "andar_tras", "Walk_Backward", "walking_backward", "walk_back", "backpedal",
                "Run_Backward", "running_backward" } },
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
            Clipe.Pular, Clipe.Pousar, Clipe.AndarTras,
        };

        /// <summary>Para onde um opcional cai num modelo externo que nao o tem (ANIM_FALLBACK do Godot).</summary>
        public static Clipe Substituto(Clipe c)
        {
            switch (c)
            {
                case Clipe.Pegar: return Clipe.Cast;
                case Clipe.Nadar: return Clipe.Run;        // meio submerso com a passada: era o que o jogo tinha
                case Clipe.AndeAgachado: return Clipe.Run;
                // os 19 magos sem o Regular_Jump: o ar SEGURA o passo no ar da corrida (o Mago congela uma copia dela). A
                // queda NAO serve: o Fall1 da Meshy e' de barriga para baixo (medido: cabeca 0,46 m a frente do quadril)
                case Clipe.Pular: return Clipe.Run;
                // sem o Walk_Backward: a corrida TOCADA AO CONTRARIO (o Mago inverte a velocidade)
                case Clipe.AndarTras: return Clipe.Run;
                default: return Clipe.Idle;   // Pousar sem o take: volta direto a passada
            }
        }

        // ---------------------------------------------------------------- pernas x mira (onda 15B)

        /// <summary>Para onde vao as pernas de quem se move MIRANDO: `Pernas` = graus do visual sobre a mira; `Tras` = recua
        /// (Walk_Backward). O corpo do Pawn fica na mira — so' o visual gira.</summary>
        public struct Passada
        {
            public float Pernas;
            public bool Tras;
        }

        /// <summary>
        /// `a` = graus da MIRA ate' o MOVIMENTO (DeltaAngle). Ate' RecuoGraus as pernas vao no rumo (corre de frente, o tronco
        /// torce para a mira); passou, RECUA: pernas opostas ao rumo. Histerese: quem ja' recua so' volta abaixo de
        /// RecuoGraus - RecuoBanda (senao o strafe diagonal troca de perna a cada tremida do stick).
        /// </summary>
        public static Passada PassadaMirando(float a, bool recuandoAntes)
        {
            a = float.IsNaN(a) ? 0f : Mathf.DeltaAngle(0f, a);
            float limiar = recuandoAntes ? Balance.Move.RecuoGraus - Balance.Anim.RecuoBanda : Balance.Move.RecuoGraus;
            bool tras = Mathf.Abs(a) > limiar;
            return new Passada { Tras = tras, Pernas = tras ? Mathf.DeltaAngle(0f, a + 180f) : a };
        }

        /// <summary>O tronco DESFAZ o giro das pernas ate' TorcaoMax: o peito volta para a mira (o resto fica de lado).</summary>
        public static float TorcaoDoTronco(float pernas) =>
            float.IsNaN(pernas) ? 0f : Mathf.Clamp(-Mathf.DeltaAngle(0f, pernas), -Balance.Anim.TorcaoMax, Balance.Anim.TorcaoMax);

        // ---------------------------------------------------------------- pulo em fases (onda 15B)

        /// <summary>Fracoes da agachada: abaixo de FaseTol o Hips esta' "em pe'" (o ar do clipe limpo e' um plato no
        /// repouso); FaseFundo marca a agachada do impulso.</summary>
        public const float FaseTol = 0.08f, FaseFundo = 0.3f;

        /// <summary>
        /// Onde o take do pulo DECOLA e POUSA, pela altura do Hips amostrada a cada `passo` s (clipe ja' LIMPO na importacao:
        /// nunca acima do 1o quadro, entao o ar e' um plato no repouso). Decolagem = o fim da agachada do impulso; pouso = o
        /// comeco da agachada da chegada. Sem agachada: o take inteiro e' ar. Agachada so' na segunda metade: e' a chegada.
        /// </summary>
        public static void FasesDoPulo(float[] alturas, float passo, out float decolagem, out float pouso)
        {
            decolagem = 0f;
            int n = alturas != null ? alturas.Length : 0;
            if (!(passo > 0f)) passo = 0f;
            pouso = Mathf.Max(n - 1, 0) * passo;
            if (n < 3 || passo == 0f) return;
            float h0 = alturas[0], fundo = 0f;
            for (int i = 1; i < n; i++) fundo = Mathf.Max(fundo, h0 - alturas[i]);
            if (!(fundo > 1e-6f)) return;
            float tol = fundo * FaseTol, desce = fundo * FaseFundo;
            int k = 1;
            while (k < n && h0 - alturas[k] < desce) k++;           // a primeira agachada funda
            int volta = k;
            while (volta < n && h0 - alturas[volta] > tol) volta++;  // levanta: DECOLOU
            if (volta >= n || k * 2 > n)
            {
                // nao levanta mais, ou a unica agachada e' a da chegada: sem impulso, pousa onde ela comeca
                int s = k;
                while (s > 0 && h0 - alturas[s - 1] > tol) s--;
                pouso = s * passo;
                return;
            }
            decolagem = volta * passo;
            int p = volta;
            while (p < n && h0 - alturas[p] <= tol) p++;             // o plato do ar ate' a agachada da chegada
            pouso = Mathf.Min(p, n - 1) * passo;
        }

        /// <summary>Velocidade do take no AR: o trecho decolagem..pouso cabe no voo previsto (2*Vy/g). Grampeada.</summary>
        public static float VelocidadeNoAr(float decolagem, float pouso, float voo) =>
            Mathf.Clamp((pouso - decolagem) / Mathf.Max(voo, 0.05f), 0.1f, 4f);

        /// <summary>
        /// TIRA O DESLOCAMENTO do Hips (importacao do pulo e do andar para tras): o corpo e' movido pela fisica do Pawn.
        /// `p` = posicoes locais por chave; `cima` = o alto do personagem no espaco do pai do Hips. O horizontal vira o do 1o
        /// quadro; o vertical nunca passa do 1o quadro (agachar pode; subir e andar, nao).
        /// </summary>
        public static Vector3[] SemDeslocamento(Vector3[] p, Vector3 cima)
        {
            if (p == null || p.Length == 0) return p;
            cima = cima.sqrMagnitude > 1e-12f ? cima.normalized : Vector3.up;
            float u0 = Vector3.Dot(p[0], cima);
            Vector3 plano = p[0] - cima * u0;
            var r = new Vector3[p.Length];
            for (int i = 0; i < p.Length; i++) r[i] = plano + cima * Mathf.Min(Vector3.Dot(p[i], cima), u0);
            return r;
        }

        /// <summary>Tangentes MONOTONAS (Fritsch-Carlson, caixa de 3): entre duas chaves a curva nunca passa delas — o plato
        /// do repouso continua plato e o grampo do SemDeslocamento vale tambem entre os quadros.</summary>
        public static float[] Tangentes(float[] t, float[] v)
        {
            int n = v.Length;
            var r = new float[n];
            for (int i = 1; i < n - 1; i++)
            {
                float a = (v[i] - v[i - 1]) / Mathf.Max(t[i] - t[i - 1], 1e-6f);
                float b = (v[i + 1] - v[i]) / Mathf.Max(t[i + 1] - t[i], 1e-6f);
                if (a * b <= 0f) continue;   // pico, vale ou plato: tangente zero
                r[i] = Mathf.Sign(a) * Mathf.Min(Mathf.Abs(a + b) * 0.5f, 3f * Mathf.Min(Mathf.Abs(a), Mathf.Abs(b)));
            }
            return r;
        }
    }
}
