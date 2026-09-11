using NUnit.Framework;
using UnityEngine;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>O gesto unico (GDD §19.3): mira-solta dispara; tap curto dispara; voltar ao centro CANCELA e o estado diz.</summary>
    public class UiGestoTests
    {
        GestoLogica _g;
        int _disparos, _cancelamentos;
        Vector2 _dir;

        [SetUp]
        public void SetUp()
        {
            _g = new GestoLogica(20f, 220);
            _disparos = 0; _cancelamentos = 0; _dir = Vector2.zero;
            _g.Disparou += d => { _disparos++; _dir = d; };
            _g.Cancelou += () => _cancelamentos++;
        }

        [Test]
        public void MirarESoltarDispara()
        {
            _g.Pressionar(new Vector2(100, 100), 0);
            _g.Arrastar(new Vector2(160, 100));
            Assert.AreEqual(EstadoGesto.Mirando, _g.Estado);
            Assert.IsTrue(_g.MostraAnel, "anel aceso = soltar dispara");
            _g.Soltar(900);
            Assert.AreEqual(1, _disparos);
            Assert.AreEqual(0, _cancelamentos);
            Assert.AreEqual(1f, _dir.x, 1e-4f);
            Assert.AreEqual(EstadoGesto.Ocioso, _g.Estado);
        }

        [Test]
        public void TapCurtoDisparaParaOndeACameraOlha()
        {
            _g.Pressionar(Vector2.zero, 1000);
            _g.Soltar(1000 + 220);
            Assert.AreEqual(1, _disparos);
            Assert.AreEqual(Vector2.zero, _dir, "zero = direcao da camera");
        }

        [Test]
        public void SegurarParadoAlemDoTapNaoDispara()
        {
            _g.Pressionar(Vector2.zero, 0);
            _g.Soltar(221);
            Assert.AreEqual(0, _disparos);
            Assert.AreEqual(1, _cancelamentos);
        }

        [Test]
        public void VoltarAoCentroCancelaEOEstadoDizCancelando()
        {
            _g.Pressionar(new Vector2(100, 100), 0);
            _g.Arrastar(new Vector2(170, 100));
            _g.Arrastar(new Vector2(105, 100));   // voltou para dentro da deadzone
            Assert.AreEqual(EstadoGesto.Cancelando, _g.Estado);
            Assert.IsTrue(_g.MostraXis, "o botao MOSTRA que soltar cancela");
            Assert.IsFalse(_g.MostraAnel);
            _g.Soltar(500);
            Assert.AreEqual(0, _disparos);
            Assert.AreEqual(1, _cancelamentos);
        }

        [Test]
        public void SairDoCentroDeNovoVoltaAMirar()
        {
            _g.Pressionar(Vector2.zero, 0);
            _g.Arrastar(new Vector2(50, 0));
            _g.Arrastar(new Vector2(3, 0));
            Assert.AreEqual(EstadoGesto.Cancelando, _g.Estado);
            _g.Arrastar(new Vector2(0, -60));
            Assert.AreEqual(EstadoGesto.Mirando, _g.Estado);
            _g.Soltar(700);
            Assert.AreEqual(1, _disparos);
            Assert.AreEqual(-1f, _dir.y, 1e-4f);
        }

        [Test]
        public void DeadzoneEmDpRespeitada()
        {
            // 14 dp a 320 dpi = 28 px: 20 px de arrasto NAO mira (vira tap), 30 px mira.
            float dz = Dp.PxCom(14f, 320f);
            Assert.AreEqual(28f, dz, 1e-4f);
            var g = new GestoLogica(dz, 220);
            int mirou = 0; Vector2 dir = new Vector2(9, 9);
            g.MiraMudou += d => mirou++;
            g.Disparou += d => dir = d;
            g.Pressionar(Vector2.zero, 0);
            g.Arrastar(new Vector2(20, 0));
            Assert.AreEqual(0, mirou);
            g.Soltar(100);
            Assert.AreEqual(Vector2.zero, dir, "abaixo da deadzone e' tap: dispara para a camera");
            g.Pressionar(Vector2.zero, 0);
            g.Arrastar(new Vector2(30, 0));
            Assert.AreEqual(1, mirou);
        }

        [Test]
        public void ArrastarSemPressionarNaoFazNada()
        {
            _g.Arrastar(new Vector2(500, 0));
            _g.Soltar(10);
            Assert.AreEqual(EstadoGesto.Ocioso, _g.Estado);
            Assert.AreEqual(0, _disparos + _cancelamentos);
        }

        [Test]
        public void DpFallbackParaDpiZero()
        {
            Assert.AreEqual(14f, Dp.PxCom(14f, 0f), 1e-4f, "dpi 0 (editor) cai em 160");
        }
    }
}
