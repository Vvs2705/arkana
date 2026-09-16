using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Characters;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// O CORPO da onda 15B, puro (queixa do Diretor, 16/09: pulo em Idle, moonwalk mirando para tras, pernas congeladas
    /// atirando): o angulo mira x movimento -> pernas/tronco/recuo/velocidade; as fases do pulo pela altura do Hips; o
    /// deslocamento tirado do take; a reserva de quem nao tem os takes novos. Os numeros das curvas sao os MEDIDOS no
    /// 01-pyra.fbx (Blender, 16/09; 24 quadros/s; x lado, y frente, z alto).
    /// </summary>
    public class CharactersCorpoTests
    {
        const float Q = 1f / 24f;

        /// <summary>Regular_Jump do 01-pyra.fbx, Hips (x, y, z) por quadro: agacha ate' 0,778, sobe ate' 1,442, agacha de novo.</summary>
        static readonly float[] PuloX = { -0.012f, -0.015f, -0.011f, -0.004f, 0.004f, 0.011f, 0.015f, 0.010f, -0.002f, -0.015f, -0.029f,
            -0.042f, -0.056f, -0.061f, -0.056f, -0.046f, -0.039f, -0.040f, -0.042f, -0.046f, -0.048f, -0.049f, -0.049f, -0.049f, -0.049f,
            -0.051f, -0.055f, -0.056f, -0.053f, -0.045f, -0.036f, -0.029f, -0.024f, -0.021f, -0.018f, -0.016f, -0.016f, -0.017f, -0.019f,
            -0.020f, -0.021f, -0.019f, -0.018f, -0.016f, -0.015f, -0.013f };
        static readonly float[] PuloY = { -0.034f, -0.033f, -0.030f, -0.026f, -0.022f, -0.018f, -0.015f, -0.014f, -0.015f, -0.015f, -0.016f,
            -0.016f, -0.016f, -0.013f, -0.007f, 0.000f, 0.007f, 0.015f, 0.024f, 0.032f, 0.034f, 0.024f, 0.008f, -0.009f, -0.025f,
            -0.042f, -0.059f, -0.073f, -0.076f, -0.073f, -0.067f, -0.062f, -0.057f, -0.054f, -0.050f, -0.047f, -0.044f, -0.043f, -0.042f,
            -0.041f, -0.040f, -0.039f, -0.038f, -0.037f, -0.036f, -0.035f };
        static readonly float[] PuloZ = { 1.015f, 1.010f, 0.983f, 0.931f, 0.872f, 0.815f, 0.778f, 0.783f, 0.823f, 0.868f, 0.913f,
            0.957f, 1.007f, 1.094f, 1.198f, 1.305f, 1.388f, 1.418f, 1.432f, 1.442f, 1.438f, 1.410f, 1.372f, 1.333f, 1.280f,
            1.187f, 1.075f, 0.970f, 0.901f, 0.855f, 0.813f, 0.780f, 0.778f, 0.799f, 0.825f, 0.852f, 0.881f, 0.913f, 0.946f,
            0.974f, 0.991f, 0.997f, 1.000f, 1.004f, 1.007f, 1.011f };
        /// <summary>Walk_Backward do 01-pyra.fbx: anda 0,90 m em y (para tras) em 22 quadros.</summary>
        static readonly float[] TrasY = { -0.047f, -0.010f, 0.026f, 0.064f, 0.104f, 0.150f, 0.198f, 0.248f, 0.294f, 0.335f, 0.375f,
            0.413f, 0.451f, 0.490f, 0.528f, 0.569f, 0.613f, 0.661f, 0.712f, 0.762f, 0.809f, 0.851f };
        static readonly float[] TrasZ = { 1.015f, 1.011f, 1.005f, 0.997f, 0.989f, 0.988f, 0.990f, 0.994f, 0.999f, 1.004f, 1.009f,
            1.012f, 1.012f, 1.008f, 1.001f, 0.995f, 0.991f, 0.991f, 0.993f, 0.997f, 1.002f, 1.009f };

        static readonly Vector3 CimaZ = new Vector3(0f, 0f, 1f);

        static Vector3[] Pulo()
        {
            var p = new Vector3[PuloZ.Length];
            for (int i = 0; i < p.Length; i++) p[i] = new Vector3(PuloX[i], PuloY[i], PuloZ[i]);
            return p;
        }

        static float[] Tempos(int n)
        {
            var t = new float[n];
            for (int i = 0; i < n; i++) t[i] = i * Q;
            return t;
        }

        /// <summary>Hermite cubica (a mesma do AnimationCurve sem peso) em `t`.</summary>
        static float Hermite(float[] t, float[] v, float[] m, float x)
        {
            int i = 0;
            while (i < t.Length - 2 && x > t[i + 1]) i++;
            float d = t[i + 1] - t[i], s = Mathf.Clamp01((x - t[i]) / d), s2 = s * s, s3 = s2 * s;
            return (2 * s3 - 3 * s2 + 1) * v[i] + (s3 - 2 * s2 + s) * d * m[i] + (-2 * s3 + 3 * s2) * v[i + 1] + (s3 - s2) * d * m[i + 1];
        }

        /// <summary>O take limpo, amostrado como o Mago amostra (60 Hz), com as tangentes que o importador grava.</summary>
        static float[] PuloLimpoA60Hz()
        {
            Vector3[] limpo = PoseMago.SemDeslocamento(Pulo(), CimaZ);
            var z = new float[limpo.Length];
            for (int i = 0; i < z.Length; i++) z[i] = limpo[i].z;
            float[] t = Tempos(z.Length), m = PoseMago.Tangentes(t, z);
            float fim = t[t.Length - 1];
            var h = new float[Mathf.CeilToInt(fim * 60f) + 1];
            for (int i = 0; i < h.Length; i++) h[i] = Hermite(t, z, m, Mathf.Min(i / 60f, fim));
            return h;
        }

        // ------------------------------------------------------------------ pernas x mira

        [Test]
        public void Mirando_AteRecuoGraus_CorreDeFrente_ETroncoVoltaParaAMira()
        {
            PoseMago.Passada p = PoseMago.PassadaMirando(0f, false);
            Assert.IsFalse(p.Tras);
            Assert.AreEqual(0f, p.Pernas, 1e-4f);
            p = PoseMago.PassadaMirando(90f, false);   // stick para a direita, mira a frente
            Assert.IsFalse(p.Tras, "strafe e' corrida de lado, nao recuo");
            Assert.AreEqual(90f, p.Pernas, 1e-3f, "as pernas vao no rumo");
            Assert.AreEqual(-Balance.Anim.TorcaoMax, PoseMago.TorcaoDoTronco(p.Pernas), 1e-3f, "o tronco desfaz ate' o teto");
            p = PoseMago.PassadaMirando(-60f, false);
            Assert.AreEqual(-60f, p.Pernas, 1e-3f);
            Assert.AreEqual(60f, PoseMago.TorcaoDoTronco(p.Pernas), 1e-3f, "dentro do teto o peito volta INTEIRO para a mira");
            Assert.AreEqual(90f, PoseMago.PassadaMirando(450f, false).Pernas, 1e-3f, "angulo fora de +-180 normaliza");
            Assert.AreEqual(0f, PoseMago.PassadaMirando(float.NaN, false).Pernas);
            Assert.AreEqual(0f, PoseMago.TorcaoDoTronco(float.NaN));
        }

        [Test]
        public void Mirando_ParaTras_Recua_PernasContraORumo_SemMoonwalk()
        {
            PoseMago.Passada p = PoseMago.PassadaMirando(180f, false);   // o caso da foto 46 (dif=180)
            Assert.IsTrue(p.Tras, "correr para tras mirando e' RECUAR");
            Assert.AreEqual(0f, p.Pernas, 1e-3f, "recuando reto as pernas ficam de frente para a mira");
            p = PoseMago.PassadaMirando(Balance.Move.RecuoGraus + 5f, false);
            Assert.IsTrue(p.Tras);
            Assert.AreEqual(Balance.Move.RecuoGraus + 5f - 180f, p.Pernas, 1e-3f, "pernas OPOSTAS ao rumo");
            Assert.AreEqual(180f - Balance.Move.RecuoGraus - 5f, PoseMago.TorcaoDoTronco(p.Pernas), 1e-3f);
            p = PoseMago.PassadaMirando(-150f, false);
            Assert.IsTrue(p.Tras);
            Assert.AreEqual(30f, p.Pernas, 1e-3f);
            // em todo o circulo as pernas ficam a menos de RecuoGraus da mira (nunca de costas para ela)
            for (float a = -180f; a <= 180f; a += 5f)
                Assert.LessOrEqual(Mathf.Abs(PoseMago.PassadaMirando(a, false).Pernas), Balance.Move.RecuoGraus + 1e-3f, "a=" + a);
        }

        [Test]
        public void Recuo_TemHisterese_NaoTrocaDePernaNaDiagonal()
        {
            float entre = Balance.Move.RecuoGraus - Balance.Anim.RecuoBanda * 0.5f;
            Assert.IsFalse(PoseMago.PassadaMirando(entre, false).Tras, "de frente, no meio da banda, segue de frente");
            Assert.IsTrue(PoseMago.PassadaMirando(entre, true).Tras, "recuando, no meio da banda, segue recuando");
            Assert.IsFalse(PoseMago.PassadaMirando(Balance.Move.RecuoGraus - Balance.Anim.RecuoBanda - 1f, true).Tras, "abaixo da banda volta");
            Assert.IsFalse(PoseMago.PassadaMirando(Balance.Move.RecuoGraus - 1f, false).Tras);
            Assert.IsTrue(PoseMago.PassadaMirando(Balance.Move.RecuoGraus + 1f, false).Tras);
        }

        [Test]
        public void RecuarMirando_EMaisLento_EmRampa()
        {
            Assert.AreEqual(1f, Locomocao.FatorDeRecuo(0f));
            Assert.AreEqual(1f, Locomocao.FatorDeRecuo(-80f), "strafe nao perde velocidade");
            Assert.AreEqual(Balance.Move.BackpedalMult, Locomocao.FatorDeRecuo(180f), 1e-5f);
            Assert.AreEqual(Balance.Move.BackpedalMult, Locomocao.FatorDeRecuo(-150f), 1e-5f, "para tras pela esquerda tambem");
            Assert.AreEqual(Balance.Move.BackpedalMult, Locomocao.FatorDeRecuo(540f), 1e-5f);
            Assert.AreEqual((1f + Balance.Move.BackpedalMult) * 0.5f, Locomocao.FatorDeRecuo(Balance.Move.RecuoGraus), 1e-4f, "meio da rampa");
            float antes = 1f;
            for (float a = 0f; a <= 180f; a += 3f)
            {
                float f = Locomocao.FatorDeRecuo(a);
                Assert.LessOrEqual(f, antes + 1e-6f, "sem degrau que sobe: a=" + a);
                Assert.Less(antes - f, 0.1f, "sem degrau: a=" + a);
                antes = f;
            }
            Assert.AreEqual(1f, Locomocao.FatorDeRecuo(float.NaN));
            Assert.Less(Balance.Move.BackpedalMult, 1f);
            Assert.Greater(Balance.Move.BackpedalMult, Velocidade.Piso);
        }

        [Test]
        public void AndarParaTras_CadenciaPelaVelocidade_ComTetoProprio()
        {
            const float len = 0.875f;   // Walk_Backward medido
            float ms1 = Balance.Anim.BackStrideM / len;
            Assert.AreEqual(1f, PoseMago.EscalaDeRecuo(ms1, len), 1e-4f, "escala 1 anda BackStrideM por volta");
            Assert.Greater(PoseMago.EscalaDeRecuo(ms1 * 1.5f, len), PoseMago.EscalaDeRecuo(ms1, len));
            float recuoCheio = Balance.Player.Speed * Balance.Move.BackpedalMult;
            Assert.AreEqual(Balance.Anim.BackScaleMax, PoseMago.EscalaDeRecuo(recuoCheio, len), 1e-4f, "recuando cheio satura no teto do recuo");
            Assert.AreEqual(Balance.Anim.ScaleMin, PoseMago.EscalaDeRecuo(0f, len));
            Assert.AreEqual(Balance.Anim.ScaleMin, PoseMago.EscalaDeRecuo(float.NaN, len));
            Assert.AreEqual(Balance.Anim.ScaleMin, PoseMago.EscalaDeRecuo(3f, 0f));
            Assert.Greater(Balance.Anim.BackScaleMax, Balance.Anim.ScaleMax, "o andar precisa de mais cadencia que a corrida");
        }

        // ------------------------------------------------------------------ contrato dos takes novos

        [Test]
        public void TakesNovos_ResolvemPeloNomeDaMeshy_ComReserva()
        {
            Assert.AreEqual(Clipe.Pular, PoseMago.Alias("Regular_Jump"));
            Assert.AreEqual(Clipe.Pular, PoseMago.Alias("Armature|Regular_Jump"));
            Assert.AreEqual(Clipe.Pular, PoseMago.Alias("pular"));
            Assert.AreEqual(Clipe.Pousar, PoseMago.Alias("pousar"));
            Assert.AreEqual(Clipe.AndarTras, PoseMago.Alias("Walk_Backward"));
            Assert.AreEqual(Clipe.AndarTras, PoseMago.Alias("Armature|Walk_Backward"));
            Assert.AreEqual(Clipe.AndarTras, PoseMago.Alias("andar_tras"));
            Assert.AreEqual(Clipe.Cair, PoseMago.Alias("jump_loop"), "jump_loop continua sendo queda");
            Assert.AreEqual(Clipe.Run, PoseMago.Alias("Walking"), "andar de frente continua sendo a passada");
            Assert.IsTrue(PoseMago.Laco(Clipe.Pular), "o ar e' sustentado ate' o chao");
            Assert.IsTrue(PoseMago.Laco(Clipe.AndarTras));
            Assert.IsFalse(PoseMago.Laco(Clipe.Pousar), "a aterrissagem acaba sozinha");
            Assert.AreEqual(Clipe.Run, PoseMago.Substituto(Clipe.Pular), "sem o take: o passo no ar da corrida, nao o Idle (nem a queda de barriga)");
            Assert.That(Balance.Anim.PuloReservaT, Is.InRange(0f, 0.625f), "o quadro segurado mora dentro do Running (0,625 s)");
            Assert.AreEqual(Clipe.Run, PoseMago.Substituto(Clipe.AndarTras), "sem o take: a corrida ao contrario");
            Assert.AreEqual(Clipe.Idle, PoseMago.Substituto(Clipe.Pousar));
            CollectionAssert.Contains(PoseMago.Todos, Clipe.Pular);
            CollectionAssert.Contains(PoseMago.Todos, Clipe.Pousar);
            CollectionAssert.Contains(PoseMago.Todos, Clipe.AndarTras);
        }

        // ------------------------------------------------------------------ o take sem deslocamento

        [Test]
        public void SemDeslocamento_OPuloSoAgacha_NuncaSobeNemAnda()
        {
            Vector3[] p = Pulo(), r = PoseMago.SemDeslocamento(p, CimaZ);
            Assert.AreEqual(p.Length, r.Length);
            float fundo = float.PositiveInfinity;
            for (int i = 0; i < r.Length; i++)
            {
                Assert.AreEqual(p[0].x, r[i].x, 1e-6f, "anda de lado no quadro " + i);
                Assert.AreEqual(p[0].y, r[i].y, 1e-6f, "anda para a frente no quadro " + i);
                Assert.LessOrEqual(r[i].z, p[0].z + 1e-6f, "SOBE no quadro " + i);
                Assert.AreEqual(Mathf.Min(p[i].z, p[0].z), r[i].z, 1e-6f, "a agachada tem de ficar no quadro " + i);
                fundo = Mathf.Min(fundo, r[i].z);
            }
            Assert.AreEqual(0.778f, fundo, 1e-4f, "a agachada do impulso fica inteira");
        }

        [Test]
        public void SemDeslocamento_OAndarParaTrasFicaNoLugar_EComOAltoGirado()
        {
            var p = new Vector3[TrasY.Length];
            for (int i = 0; i < p.Length; i++) p[i] = new Vector3(0.012f, TrasY[i], TrasZ[i]);
            Vector3[] r = PoseMago.SemDeslocamento(p, CimaZ);
            for (int i = 0; i < r.Length; i++)
            {
                Assert.AreEqual(p[0].y, r[i].y, 1e-6f, "o andar para tras ainda anda no quadro " + i);
                Assert.AreEqual(p[i].z, r[i].z, 1e-6f, "o balanco do andar (abaixo do 1o quadro) fica");
            }
            // o alto em qualquer direcao (a Armature da Meshy vem girada): a regra e' no eixo, nao no "y"
            Vector3 cima = new Vector3(0f, 0.6f, 0.8f);
            var q = new[] { new Vector3(0.1f, 0.5f, 0.3f), new Vector3(0.4f, 1.5f, 1.2f), new Vector3(-0.2f, 0.1f, -0.3f) };
            Vector3[] s = PoseMago.SemDeslocamento(q, cima * 3f);
            float u0 = Vector3.Dot(q[0], cima);
            for (int i = 0; i < s.Length; i++)
            {
                float u = Vector3.Dot(s[i], cima);
                Assert.AreEqual(Mathf.Min(Vector3.Dot(q[i], cima), u0), u, 1e-5f);
                Vector3 h = (s[i] - cima * u) - (q[0] - cima * u0);
                Assert.Less(h.magnitude, 1e-5f, "o plano do chao fica o do 1o quadro");
            }
            Assert.IsNull(PoseMago.SemDeslocamento(null, cima));
            Assert.AreEqual(0, PoseMago.SemDeslocamento(new Vector3[0], cima).Length);
        }

        [Test]
        public void Tangentes_NuncaPassamDasChaves_OPlatoFicaPlato()
        {
            Vector3[] limpo = PoseMago.SemDeslocamento(Pulo(), CimaZ);
            var z = new float[limpo.Length];
            for (int i = 0; i < z.Length; i++) z[i] = limpo[i].z;
            ConfereMonotono(Tempos(z.Length), z, "pulo limpo");
            // subida ingreme e depois quase plana: a media das inclinacoes passaria da chave
            float[] v = { 0f, 10f, 10.1f, 10.1f, 4f, 3.9f };
            ConfereMonotono(Tempos(v.Length), v, "ingreme->plano");
            float[] m = PoseMago.Tangentes(Tempos(v.Length), v);
            Assert.AreEqual(0f, m[0]);
            Assert.AreEqual(0f, m[m.Length - 1]);
            Assert.AreEqual(0f, m[2], "pico/plato: tangente zero");
        }

        static void ConfereMonotono(float[] t, float[] v, string nome)
        {
            float[] m = PoseMago.Tangentes(t, v);
            for (int i = 0; i + 1 < t.Length; i++)
            {
                float lo = Mathf.Min(v[i], v[i + 1]) - 1e-6f, hi = Mathf.Max(v[i], v[i + 1]) + 1e-6f;
                for (int k = 1; k < 10; k++)
                {
                    float x = Hermite(t, v, m, Mathf.Lerp(t[i], t[i + 1], k / 10f));
                    Assert.That(x, Is.InRange(lo, hi), nome + ": a curva passa das chaves entre " + i + " e " + (i + 1));
                }
            }
        }

        // ------------------------------------------------------------------ as fases do pulo

        [Test]
        public void FasesDoPulo_DoTakeMedido_DecolaNoFimDaAgachada_PousaNaChegada()
        {
            PoseMago.FasesDoPulo(PuloLimpoA60Hz(), 1f / 60f, out float dec, out float pouso);
            // medido pelos dedos dos pes no Blender: saem do chao entre 0,50 e 0,54 s, voltam em 1,125 s
            Assert.That(dec, Is.InRange(0.45f, 0.56f), "decolagem");
            Assert.That(pouso, Is.InRange(1.05f, 1.16f), "pouso");
            float voo = Locomocao.TempoDeVoo(Balance.Player.JumpV);
            Assert.AreEqual(1.06f, voo, 0.01f, "o pulo do jogo fica ~1,06 s no ar");
            float v = PoseMago.VelocidadeNoAr(dec, pouso, voo);
            Assert.That(v, Is.InRange(0.45f, 0.7f), "o ar do take (0,6 s) esticado no voo do jogo");
            Assert.AreEqual(voo, (pouso - dec) / v, 1e-3f, "o trecho do ar cabe exatamente no voo");
            Assert.Less(PoseMago.VelocidadeNoAr(dec, pouso, Locomocao.TempoDeVoo(Balance.Player.JumpV * Mathf.Sqrt(1.5f))), v,
                "a mola do Fizz voa mais: o ar toca mais devagar");
        }

        [Test]
        public void FasesDoPulo_CasosDeBorda()
        {
            float[] reto = { 1f, 1f, 1f, 1f };
            PoseMago.FasesDoPulo(reto, 0.1f, out float d, out float p);
            Assert.AreEqual(0f, d, "sem agachada o take inteiro e' ar");
            Assert.AreEqual(0.3f, p, 1e-5f);
            // so' a agachada da chegada (take que comeca no ar)
            float[] chegada = { 1f, 1f, 1f, 1f, 1f, 1f, 0.9f, 0.7f, 0.8f, 1f };
            PoseMago.FasesDoPulo(chegada, 0.1f, out d, out p);
            Assert.AreEqual(0f, d);
            Assert.AreEqual(0.6f, p, 1e-5f, "pousa onde a agachada comeca");
            // o ar com ruido (a reducao de chaves do importador tremendo milimetros abaixo do repouso) segue sendo UM ar
            float[] ruido = { 1f, 0.9f, 0.7f, 0.9f, 0.999f, 0.998f, 0.999f, 0.998f, 0.8f, 0.7f, 1f };
            PoseMago.FasesDoPulo(ruido, 0.1f, out d, out p);
            Assert.AreEqual(0.4f, d, 1e-5f, "decola onde o ruido comeca");
            Assert.AreEqual(0.8f, p, 1e-5f, "pousa na agachada, nao no primeiro milimetro");
            PoseMago.FasesDoPulo(null, 0.1f, out d, out p);
            Assert.AreEqual(0f, d); Assert.AreEqual(0f, p);
            PoseMago.FasesDoPulo(reto, float.NaN, out d, out p);
            Assert.AreEqual(0f, d); Assert.AreEqual(0f, p);
            Assert.AreEqual(4f, PoseMago.VelocidadeNoAr(0f, 1f, 0f), "voo zero nao divide por zero");
            Assert.AreEqual(0.1f, PoseMago.VelocidadeNoAr(0f, 0.01f, 5f));
        }

        // ------------------------------------------------------------------ o ar do corpo

        /// <summary>Simula o corpo num chao em y=0 (e um degrau/abismo em `chaoDepois`) com a MESMA ordem do Pawn: Tick,
        /// mover, chao, AtualizarAr. Devolve as poses do ar que apareceram.</summary>
        static string Simular(Locomocao l, float segundos, float chaoDepois, bool pular, out float y)
        {
            const float dt = 1f / 60f;
            float mana = 100f;
            y = 0f;
            bool noChao = true;
            if (pular) Assert.IsTrue(l.Pular(noChao, false));
            var vistas = new System.Text.StringBuilder();
            for (float t = 0f; t < segundos; t += dt)
            {
                Vector3 d = l.Tick(dt, Vector3.zero, noChao, false, Balance.Player.Speed, false, false, ref mana);
                float chao = t > 0.001f ? chaoDepois : 0f;
                bool decolando = pular && t < dt * 0.5f;   // o isGrounded do CharacterController ainda diz chao
                y += d.y;
                if (y < chao) y = chao;
                noChao = decolando || (y <= chao + 1e-4f && l.Vy <= 0f);
                l.AtualizarAr(dt, noChao, false);
                string a = noChao ? null : l.AnimNoAr();
                string marca = a == null ? "." : a == "pular" ? "p" : a == "cair" ? "c" : "?";
                if (vistas.Length == 0 || vistas[vistas.Length - 1] != marca[0]) vistas.Append(marca);
            }
            return vistas.ToString();
        }

        [Test]
        public void Pulo_NoArEPulo_DoInicioAoFim_SemPiscarQueda_EPousa()
        {
            var l = new Locomocao();
            string vistas = Simular(l, 0.5f, 0f, true, out float y);
            Assert.IsTrue(l.Pulando, "no meio do pulo: " + vistas);
            Assert.AreEqual("p", vistas.Substring(vistas.Length - 1), "o ar e' o SALTO: " + vistas);
            Assert.Greater(y, 1f, "subiu");
            l = new Locomocao();
            vistas = Simular(l, 1.4f, 0f, true, out y);
            Assert.AreEqual(".p.", vistas, "decola (o 1o quadro ainda e' chao), fica em PULO ate' o chao, sem 'cair' na descida");
            Assert.IsFalse(l.Pulando);
            Assert.Greater(l.PousoS, 0f, "tocou o chao: a aterrissagem armou");
            Assert.LessOrEqual(l.PousoS, Locomocao.POUSO_S);
        }

        [Test]
        public void Pulo_DoBarranco_ViraQueda_DepoisDoVooPrevisto()
        {
            var l = new Locomocao();
            string vistas = Simular(l, 1.3f, -50f, true, out _);
            Assert.AreEqual(".p", vistas, "ate' o voo previsto + folga ainda e' pulo: " + vistas);
            vistas = Simular(l = new Locomocao(), 2.2f, -50f, true, out _);
            Assert.AreEqual(".pc", vistas, "passou do voo previsto caindo: QUEDA");
            Assert.IsTrue(l.QuedaLonga);
        }

        [Test]
        public void SemPulo_DegrauNaoEQueda_QuedaLongaE()
        {
            var l = new Locomocao();
            string vistas = Simular(l, 1f, -0.3f, false, out _);
            Assert.IsFalse(vistas.Contains("c"), "degrau de 30 cm nao pisca queda: " + vistas);
            Assert.AreEqual(0f, l.PousoS, "degrau nao tem aterrissagem");
            vistas = Simular(l = new Locomocao(), 1.45f, -8f, false, out _);   // toca o chao em ~1,2 s
            Assert.AreEqual(".c.", vistas, "8 m de queda: passada, QUEDA, chao");
            Assert.Greater(l.PousoS, 0f, "queda longa pousa com a aterrissagem");
            Assert.IsNull(new Locomocao().AnimNoAr(), "sem pulo nem queda, o ar segue a passada");
        }

        [Test]
        public void Pulo_OChaoDoQuadroDaDecolagemNaoCancela_ETeleporteLimpa()
        {
            var l = new Locomocao();
            Assert.IsTrue(l.Pular(true, false));
            float mana = 0f;
            l.Tick(1f / 60f, Vector3.zero, true, false, 7.5f, false, false, ref mana);
            l.AtualizarAr(1f / 60f, true, false);   // o isGrounded ainda e' o de antes
            Assert.IsTrue(l.Pulando, "o pulo nao pode morrer no quadro em que decola");
            Assert.AreEqual(0f, l.PousoS, "nem armar aterrissagem");
            l.AtualizarAr(1f / 60f, false, false);
            l.Parar();
            Assert.IsFalse(l.Pulando, "teleporte no ar: nada de aterrissar no destino");
            l.AtualizarAr(1f / 60f, true, false);
            Assert.AreEqual(0f, l.PousoS);
            Assert.IsTrue(l.Pular(true, false));
            l.AtualizarAr(1f / 60f, false, false);
            l.AtualizarAr(1f / 60f, false, true);   // caiu na agua
            Assert.IsFalse(l.Pulando);
            Assert.AreEqual(0f, l.PousoS, "nadando nao aterrissa");
            Assert.AreEqual(0f, Locomocao.TempoDeVoo(-1f));
        }
    }
}
