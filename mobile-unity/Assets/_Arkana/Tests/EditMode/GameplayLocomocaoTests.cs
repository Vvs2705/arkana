using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>Game feel, esquiva, salto e flutuar (selftest.gd _test_game_feel/_test_dodge/_test_salto_e_flutuar), em C#.</summary>
    public class GameplayLocomocaoTests
    {
        private const float DT = 1f / 60f;
        private static readonly Vector3 DIR = Vector3.right;

        [SetUp]
        public void SetUp() { Bus.Reset(); }

        private static Vector3 Passo(Locomocao l, Vector3 dir, float dt = DT, bool noChao = true, bool flutuar = false)
        {
            float mana = 100f;
            return l.Tick(dt, dir, noChao, false, Balance.Player.Speed, false, flutuar, ref mana);
        }

        [Test]
        public void Aceleracao_ConvergeEmSpeedSobreAccel()
        {
            var l = new Locomocao();
            Passo(l, DIR);
            Assert.Greater(l.VelocidadeHorizontal, 0f);
            Assert.Less(l.VelocidadeHorizontal, Balance.Player.Speed * 0.5f, "1 frame NAO chega na velocidade cheia");
            float t = DT;
            while (l.VelocidadeHorizontal < Balance.Player.Speed - 0.001f && t < 3f) { Passo(l, DIR); t += DT; }
            Assert.AreEqual(Balance.Player.Speed, l.VelocidadeHorizontal, 0.001f);
            Assert.AreEqual(Balance.Player.Speed / Balance.Move.Accel, t, DT * 1.5f, "tempo ate a velocidade cheia = speed/accel");
        }

        [Test]
        public void Freio_ParaEmSpeedSobreBrake_SemInverter()
        {
            var l = new Locomocao();
            for (int i = 0; i < 120; i++) Passo(l, DIR);
            float t = 0f;
            while (l.VelocidadeHorizontal > 0.001f && t < 3f)
            {
                Passo(l, Vector3.zero); t += DT;
                Assert.GreaterOrEqual(l.VelH.x, -0.001f, "o freio nao passa do ponto");
            }
            Assert.AreEqual(Balance.Player.Speed / Balance.Move.Brake, t, DT * 1.5f);
        }

        [Test]
        public void NoAr_AceleraMenos()
        {
            var chao = new Locomocao(); var ar = new Locomocao();
            Passo(chao, DIR, DT, true);
            Passo(ar, DIR, DT, false);
            Assert.Less(ar.VelocidadeHorizontal, chao.VelocidadeHorizontal);
        }

        [Test]
        public void Stick12PorCento_NaoDa12PorCentoDaVelocidade()
        {
            // a curva mora no joystick (fonte unica); a locomocao so' multiplica — sem degrau na borda da zona morta
            var j = new JoystickLogica(Balance.Move.StickDeadzone, Balance.Move.StickCurve);
            Vector2 s12 = j.Direcao(new Vector2(12f, 0f), 100f);
            var l = new Locomocao();
            for (int i = 0; i < 120; i++) Passo(l, Locomocao.DirDoStick(s12, 0f));
            Assert.AreEqual(0f, l.VelocidadeHorizontal, 1e-4f, "12% de stick = zona morta = parado");
            Vector2 s50 = j.Direcao(new Vector2(50f, 0f), 100f);
            var l2 = new Locomocao();
            for (int i = 0; i < 120; i++) Passo(l2, Locomocao.DirDoStick(s50, 0f));
            Assert.Greater(l2.VelocidadeHorizontal, 0f);
            Assert.Less(l2.VelocidadeHorizontal, Balance.Player.Speed * 0.5f, "curva > 1: metade do curso e' MENOS que metade da velocidade");
        }

        [Test]
        public void Dodge_PercorreDistance_IndependenteDoBurst_ComIframesECooldown()
        {
            var l = new Locomocao();
            Assert.IsTrue(l.DodgePronto);
            Assert.IsTrue(l.Dodge(Vector3.forward));
            Assert.Greater(l.IframesLeft, 0f, "i-frames no inicio do dash");
            Assert.IsFalse(l.Dodge(Vector3.forward), "cooldown barra esquiva dupla");
            float z = 0f;
            const float dt = 0.005f;
            for (float t = 0f; t < Balance.Dodge.Duration - 1e-6f; t += dt) z += Passo(l, Vector3.zero, dt).z;
            Assert.AreEqual(Balance.Dodge.Distance, z, Balance.Dodge.Distance * 0.02f, "o dash percorre Distance mesmo com a rampa de arranque");
            Assert.Greater(Locomocao.DashSpeedAt(0f), Locomocao.DashSpeedAt(1f), "arranque no primeiro instante");
            Assert.Less(l.DodgeLeft, 1e-3f, "o dash acabou");
            Assert.Greater(new Vector2(l.VelH.x, l.VelH.z).magnitude, Balance.Player.Speed * 0.9f, "exit_momentum: sai correndo");
            Passo(l, Vector3.zero, Balance.Dodge.Iframes + 0.05f);
            Assert.AreEqual(0f, l.IframesLeft, "i-frames acabam");
            Assert.IsFalse(l.DodgePronto);
            Passo(l, Vector3.zero, Balance.Dodge.Cooldown + 0.1f);
            Assert.IsTrue(l.DodgePronto, "cooldown expira e libera");
        }

        [Test]
        public void Knockback_EntraNaVelocidade_EDecaiSozinho()
        {
            var l = new Locomocao();
            l.Empurrar(new Vector3(3f, 0f, 0f));
            Passo(l, Vector3.zero);
            Assert.Greater(l.Vel.x, 0f);
            Passo(l, Vector3.zero, 10f);
            Assert.AreEqual(0f, l.Vel.x, 1e-4f);
        }

        [Test]
        public void Pulo_SoDoChao_ESobeUns1m4()
        {
            var l = new Locomocao();
            Assert.IsFalse(l.Pular(false, false), "no ar o pulo e' NEGADO");
            Assert.IsFalse(l.Pular(true, true), "nadando nao se pula");
            Assert.IsTrue(l.Pular(true, false));
            float y = 0f, topo = 0f;
            for (int i = 0; i < 600 && (y > -0.01f); i++) { y += Passo(l, Vector3.zero, DT, false).y; topo = Mathf.Max(topo, y); }
            Assert.AreEqual(1.4f, topo, 0.15f, "salto arcano: ~1,4 m (passa cerca, nao vira plataforma)");
        }

        [Test]
        public void Flutuar_SeguraAQueda_ConsomeMana_ERespeitaDurMax()
        {
            var l = new Locomocao();
            for (int i = 0; i < 10; i++) Passo(l, Vector3.zero, DT, false);
            Assert.Less(l.Vy, -1f, "caindo, a gravidade acelera");

            var f = new Locomocao();
            float mana = 40f;
            for (int i = 0; i < 30; i++) f.Tick(DT, Vector3.zero, false, false, Balance.Player.Speed, false, true, ref mana);
            Assert.IsTrue(f.Flutuando);
            Assert.AreEqual(-Balance.Flutuar.DescV, f.Vy, 1e-4f, "descida constante e lenta; NAO sobe");
            Assert.Less(mana, 40f - 5f, "flutuar CONSOME mana");

            mana = 0f;
            f.Tick(DT, Vector3.zero, false, false, Balance.Player.Speed, false, true, ref mana);
            Assert.IsFalse(f.Flutuando, "mana no fim: a magia larga o corpo na hora");

            var t = new Locomocao();
            mana = 1000f;
            for (int i = 0; i < 200; i++) t.Tick(Balance.Flutuar.DurMax / 100f, Vector3.zero, false, false, Balance.Player.Speed, false, true, ref mana);
            Assert.IsFalse(t.Flutuando, "passado o teto a flutuacao ACABA");
            Assert.AreEqual(0f, t.FlutuaRestante, 1e-4f);
            Passo(t, Vector3.zero, DT, true);
            Assert.AreEqual(Balance.Flutuar.DurMax, t.FlutuaRestante, 1e-4f, "o chao devolve a flutuacao");
        }

        [Test]
        public void Histerese_RunIdle_NaoPisca()
        {
            var l = new Locomocao();
            Assert.AreEqual("idle", l.AnimNoChao());
            l.Vel = new Vector3(Balance.Move.RunAnimEnter + 0.1f, 0f, 0f);
            Assert.AreEqual("run", l.AnimNoChao());
            l.Vel = new Vector3((Balance.Move.RunAnimEnter + Balance.Move.RunAnimExit) * 0.5f, 0f, 0f);
            Assert.AreEqual("run", l.AnimNoChao(), "entre Exit e Enter continua run");
            l.Vel = new Vector3(Balance.Move.RunAnimExit - 0.1f, 0f, 0f);
            Assert.AreEqual("idle", l.AnimNoChao());
        }

        [Test]
        public void VelocidadeMaxima_EProdutoUnico()
        {
            Assert.AreEqual(Balance.Player.Speed * 0.55f * 0.9f * 0.35f, Locomocao.Speed(0.55f, 0.9f, 0.35f), 1e-4f);
            var l = new Locomocao();
            float mana = 100f;
            for (int i = 0; i < 120; i++) l.Tick(DT, DIR, true, false, Locomocao.Speed(0.55f, 1f, 1f), false, false, ref mana);
            Assert.AreEqual(Balance.Player.Speed * 0.55f, l.VelocidadeHorizontal, 1e-3f, "nadando a 55%");
        }

        [Test]
        public void Atordoado_OCorpoNaoObedece()
        {
            var l = new Locomocao();
            float mana = 100f;
            for (int i = 0; i < 60; i++) l.Tick(DT, DIR, true, false, Balance.Player.Speed, true, false, ref mana);
            Assert.AreEqual(0f, l.VelocidadeHorizontal, 1e-4f);
        }
    }
}
