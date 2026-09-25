using NUnit.Framework;
using UnityEngine;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>A rota do castelo: deterministica por seed, atravessa a ilha, alcance de queda compativel.</summary>
    public class WorldCasteloTests
    {
        static readonly Relevo Mapa = new Relevo(2f, 7);

        [Test]
        public void Rota_EhDeterministica_PorSeed()
        {
            var a = new RotaDoCastelo(3103, Mapa);
            var b = new RotaDoCastelo(3103, Mapa);
            Assert.AreEqual(a.Inicio, b.Inicio);
            Assert.AreEqual(a.Fim, b.Fim);
            Assert.AreEqual(3103, a.Seed, "o seed fica guardado para a rede");
        }

        [Test]
        public void Rota_MudaComSeed()
        {
            // DEFEITO (27/08): SEED_ROTA cravado = o castelo cruzava a ilha pela MESMA linha em toda partida.
            var a = new RotaDoCastelo(1, Mapa);
            var b = new RotaDoCastelo(2, Mapa);
            Assert.Greater(Vector3.Distance(a.Inicio, b.Inicio), 1f);
        }

        [Test]
        public void Rota_AtravessaAIlha_DeForaAFora_PertoDoCentro()
        {
            for (int s = 0; s < 40; s++)
            {
                var r = new RotaDoCastelo(s * 7919 + 1, Mapa);
                float ini = new Vector2(r.Inicio.x, r.Inicio.z).magnitude;
                float fim = new Vector2(r.Fim.x, r.Fim.z).magnitude;
                Assert.Greater(ini, Mapa.RaioTerra, "entra de fora da ilha (seed " + r.Seed + ")");
                Assert.Greater(fim, Mapa.RaioTerra, "sai pelo outro lado (seed " + r.Seed + ")");
                Assert.Less(r.DistanciaAoCentro, Mapa.RaioTerra * 0.5f, "passa a menos de meia-terra do centro");
                Assert.AreEqual(r.Altura, r.Inicio.y, 1e-3f);
                Assert.AreEqual(r.Altura, r.Fim.y, 1e-3f);
            }
        }

        [Test]
        public void Rota_NemSempreCruzaOCentro()
        {
            // Sem DESVIO toda partida abriria com o castelo em cima do vale — a queda obvia sempre a mesma.
            float maior = 0f;
            for (int s = 0; s < 40; s++) maior = Mathf.Max(maior, new RotaDoCastelo(s * 7919 + 1, Mapa).DistanciaAoCentro);
            Assert.Greater(maior, 20f, "alguma rota passa longe do centro");
        }

        [Test]
        public void Rota_EscalaComOMapa_DuracaoNao()
        {
            var pequena = new RotaDoCastelo(3103, new Relevo(1f, 7));
            var grande = new RotaDoCastelo(3103, new Relevo(2f, 7));
            float lp = (pequena.Fim - pequena.Inicio).magnitude;
            float lg = (grande.Fim - grande.Inicio).magnitude;
            Assert.AreEqual(lp * 2f, lg, 0.01f, "a rota dobra com o lado");
            Assert.AreEqual(pequena.Velocidade * 2f, grande.Velocidade, 0.01f, "mapa maior = castelo mais rapido, mesma janela de 22 s");
            Assert.AreEqual(34f, grande.Velocidade, 1f, "a 600 m o castelo anda ~34 m/s");
        }

        [Test]
        public void PosicaoEm_Interpola_ComClamp()
        {
            var r = new RotaDoCastelo(3103, Mapa);
            Assert.AreEqual(r.Inicio, r.PosicaoEm(0f));
            Assert.AreEqual(r.Fim, r.PosicaoEm(1f));
            Assert.AreEqual(r.Fim, r.PosicaoEm(1.5f));
            Assert.AreEqual((r.Inicio + r.Fim) * 0.5f, r.PosicaoEm(0.5f));
        }

        [Test]
        public void Queda_AlcanceCompativelComOGodot()
        {
            // Queda.gd: 55 m/s terminal, 40 m/s2, planeio a 60 m sobre o chao (12 m/s desce, 16 anda),
            // freio 90. Pousando a 5 m: ~185 m de alcance horizontal em ~9,5 s de ar.
            float segundos;
            var rota = new RotaDoCastelo(3103, Mapa);
            float alcance = rota.AlcanceHorizontalDaQueda(5f, out segundos);
            Assert.IsTrue(alcance > 170f && alcance < 200f, "alcance " + alcance + " m");
            Assert.IsTrue(segundos > 9f && segundos < 10.5f, "tempo no ar " + segundos + " s");
            // O desvio lateral maximo da rota (18% do lado = 108 m) cabe no alcance: do ponto mais
            // proximo da rota, o vale central e' sempre alcancavel.
            Assert.Less(RotaDoCastelo.Desvio * Mapa.Lado, alcance);
            // E a mesa do pico (26 m) tem MENOS ar que o vale, nao mais: alcance cai, nunca sobe.
            float s2;
            Assert.Less(rota.AlcanceHorizontalDaQueda(26f, out s2), alcance);
        }

        [Test]
        public void Rota_SemIlha_UsaAIlhaPadrao()
        {
            var r = new RotaDoCastelo(3103, null);
            Assert.AreEqual(600f, r.Lado, 1e-3f, "fiacao defensiva: sem ilha, a de 600 m");
        }
    }
}
