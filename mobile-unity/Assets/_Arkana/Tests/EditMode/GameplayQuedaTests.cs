using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>A queda (gameplay/queda/selftest.gd) e a agua (selftest.gd _test_agua), em C#.</summary>
    public class GameplayQuedaTests
    {
        private const float DT = 1f / 60f;
        private List<string> _fases;
        private List<float[]> _alturas;
        private List<Elemento> _casts;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            _fases = new List<string>();
            _alturas = new List<float[]>();
            _casts = new List<Elemento>();
            Bus.QuedaFase += f => _fases.Add(f);
            Bus.QuedaAltura += (m, v) => _alturas.Add(new[] { m, v });
            Bus.SpellCast += e => _casts.Add(e);
        }

        /// <summary>Roda ate' pousar; devolve os segundos no ar.</summary>
        private static float CairAteOChao(Queda q, int maxPassos = 20000)
        {
            float t = 0f;
            for (int i = 0; i < maxPassos && q.NoAr; i++) { q.Tick(DT); t += DT; }
            return t;
        }

        [Test]
        public void Fases_SaemNaOrdemDoContrato_ESemMagiaNoAr()
        {
            var q = new Queda(new FakeRelevo(), new Vector3(0f, Queda.ALTURA_CASTELO, 0f));
            Assert.AreEqual(Queda.NO_CASTELO, q.Fase);
            Assert.IsTrue(q.NoAr);
            Assert.IsFalse(q.PodeConjurar, "no castelo nao se conjura");
            Vector3 antes = q.Pos;
            q.Tick(1f);
            Assert.AreEqual(antes, q.Pos, "no castelo o corpo nao cai: viaja pendurado");

            Assert.IsTrue(q.Saltar());
            Assert.AreEqual(Queda.CAINDO, q.Fase);
            Assert.IsFalse(q.Saltar(), "saltar duas vezes nao emite duas");
            bool conjurouNoAr = false;
            float t = 0f;
            for (int i = 0; i < 20000 && q.NoAr; i++)
            {
                q.Tick(DT); t += DT;
                if (q.NoAr && q.PodeConjurar) conjurouNoAr = true;
            }
            Assert.IsFalse(conjurouNoAr, "NENHUMA magia no ar");
            Assert.AreEqual(Queda.POUSOU, q.Fase);
            Assert.IsTrue(q.PodeConjurar, "no chao o poder volta");
            CollectionAssert.AreEqual(new[] { Queda.NO_CASTELO, Queda.CAINDO, Queda.PLANANDO, Queda.POUSOU }, _fases,
                "as quatro fases, na ordem, uma vez cada");
            Assert.AreEqual(0, _casts.Count, "nenhum SpellCast durante a queda");
            Assert.AreEqual(0f, _alturas[_alturas.Count - 1][0], "no pouso a HUD apaga o altimetro (0, 0)");
            Assert.AreEqual(0f, _alturas[_alturas.Count - 1][1]);
            // os numeros documentados: ~9,4 s de ar a partir de 120 m
            Assert.Greater(t, 8f, "tempo de ar documentado (~9,4 s)");
            Assert.Less(t, 11f);
        }

        [Test]
        public void Alcance_ComOJoystickCravadoChegaA180m_EMergulhoGanhaDistancia()
        {
            var q = new Queda(new FakeRelevo(), new Vector3(0f, Queda.ALTURA_CASTELO, 0f));
            q.Direcao = new Vector3(1f, 0f, 0f);
            q.Saltar();
            float vyMax = 0f, vyFim = 0f;
            for (int i = 0; i < 20000 && q.NoAr; i++)
            {
                q.Tick(DT);
                if (q.Fase == Queda.CAINDO) vyMax = Mathf.Max(vyMax, q.Vy);
                if (q.Fase == Queda.PLANANDO) vyFim = q.Vy;
            }
            Assert.Greater(q.Pos.x, 150f, "alcance documentado (~180 m)");
            Assert.Less(q.Pos.x, 210f);
            Assert.Greater(vyMax, Queda.VEL_PLANEIO * 3f, "o mergulho e' RAPIDO (~49 m/s ao chegar nos 90 m)");
            Assert.Greater(vyMax / vyFim, 4f, "o planeio desce ~5x mais devagar que a queda");
        }

        [Test]
        public void Pouso_NaAlturaDoTerrenoConsultadaNoPontoDeChegada()
        {
            var relevo = new FakeRelevo { AlturaFn = (x, z) => 7f };
            var q = new Queda(relevo, new Vector3(0f, 7f + Queda.ALTURA_CASTELO, 0f));
            q.Saltar();
            CairAteOChao(q);
            Assert.AreEqual(Queda.POUSOU, q.Fase);
            Assert.AreEqual(7f, q.Pos.y, 0.01f, "pousa NO terreno, nao em y = 0 (a ilha e' procedural)");
            Assert.AreEqual(0f, q.Altura, 0.01f);
            Assert.AreEqual(0f, q.Vy);
        }

        [Test]
        public void Altimetro_A10Hz_NuncaPorFrame_EBotNaoFalaComOBus()
        {
            var q = new Queda(new FakeRelevo(), new Vector3(0f, Queda.ALTURA_CASTELO, 0f));
            q.Saltar();
            _alturas.Clear();
            for (int i = 0; i < 60; i++) q.Tick(DT);
            Assert.GreaterOrEqual(_alturas.Count, 9, "~10 sinais em 1 s");
            Assert.LessOrEqual(_alturas.Count, 11, "nunca 60 por segundo");
            Assert.Greater(_alturas[0][1], 0f, "velocidade sai POSITIVA = descendo");

            _fases.Clear(); _alturas.Clear();
            var bot = new Queda(new FakeRelevo(), new Vector3(0f, Queda.ALTURA_CASTELO, 0f), false);
            bot.Saltar();
            CairAteOChao(bot);
            Assert.AreEqual(Queda.POUSOU, bot.Fase, "o bot cai pela MESMA lei");
            Assert.AreEqual(0, _fases.Count + _alturas.Count, "mas nao mexe na HUD do jogador");
        }

        [Test]
        public void AlternarPlanar_AbreCedoOuMergulhaAteOChao()
        {
            var q = new Queda(new FakeRelevo(), new Vector3(0f, Queda.ALTURA_CASTELO, 0f));
            Assert.IsFalse(q.AlternarPlanar(), "no castelo nao ha' o que alternar");
            q.Saltar();
            Assert.IsTrue(q.AlternarPlanar());
            Assert.AreEqual(Queda.PLANANDO, q.Fase, "abriu cedo, acima da altura automatica");
            Assert.IsTrue(q.AlternarPlanar());
            Assert.AreEqual(Queda.CAINDO, q.Fase, "fechou: mergulho");
            for (int i = 0; i < 20000 && q.NoAr; i++)
            {
                q.Tick(DT);
                Assert.AreNotEqual(Queda.PLANANDO, q.Fase, "mergulho manual nao reabre sozinho");
            }
            Assert.AreEqual(Queda.POUSOU, q.Fase, "e pousa do mesmo jeito: queda nao causa dano");
            Assert.IsFalse(q.AlternarPlanar(), "no chao nao ha' o que alternar");
        }

        // ------------------------------------------------------------- a agua

        [Test]
        public void Agua_NadarDoPeito_FlutuarESairEncharcado()
        {
            // lamina em y = 2 dentro de um disco de 10 m; seco fora
            var relevo = new FakeRelevo { AguaFn = (x, z) => Mathf.Abs(x) <= 10f ? 2f : Relevo.Seco };
            var a = new Agua(relevo);

            a.Tick(0.1f, new Vector3(50f, 0f, 0f));
            Assert.IsFalse(a.Nadando);
            Assert.AreEqual(1f, a.Fator, 0.001f, "em terra seca o produto nao muda");

            a.Tick(0.1f, new Vector3(0f, 0f, 0f));   // 2 m de agua > peito 1,2
            Assert.IsTrue(a.Nadando, "peito coberto -> modo NADAR");
            Assert.AreEqual(Agua.NADO_MULT, a.Fator, 0.001f, "nadar e' 55% (produto unico)");
            Assert.AreEqual("nadar_parado", a.Locomocao(0f));
            Assert.AreEqual("nadar", a.Locomocao(3f));
            Vector3 p = new Vector3(0f, -2f, 0f);
            for (int i = 0; i < 30; i++) p = a.Flutuar(p, 0.05f);
            Assert.AreEqual(2f - Agua.PEITO, p.y, 0.15f, "o corpo FLUTUA com a lamina no peito");

            a.Tick(0.1f, new Vector3(0f, 1.5f, 0f));  // 0,5 m: poca no tornozelo
            Assert.IsFalse(a.Nadando, "poca no tornozelo NAO e' nado");
            Assert.IsTrue(a.Encharcado);
            Assert.AreEqual(Agua.MOLHADO_MULT, a.Fator, 0.001f, "saiu ENCHARCADO: 80%");
            Assert.AreEqual(new Vector3(0f, 5f, 0f), a.Flutuar(new Vector3(0f, 5f, 0f), 0.1f), "fora d'agua nada flutua");

            for (int i = 0; i < 40; i++) a.Tick(0.1f, new Vector3(50f, 0f, 0f));
            Assert.AreEqual(1f, a.Fator, 0.001f, "a roupa seca em MOLHADO_S e a velocidade volta");

            var seca = new Agua(null);
            seca.Tick(0.1f, Vector3.zero);
            Assert.AreEqual(1f, seca.Fator, "sem ilha tudo vale 1.0 e nada quebra");
        }
    }
}
