using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// O que a raia MUNDO-VISUAL promete sobre o cenario, nas classes PURAS (PlantioDoKit, PlantioDaGrama,
    /// GradeDoChao) — espelho do que o world/selftest.gd do Godot cobrava da ilha. Cada teste fica
    /// vermelho reintroduzindo o defeito que ele guarda (anotado em cada um).
    /// </summary>
    public class WorldVisualTests
    {
        static Relevo _relevo;
        static GradeDoChao _grade;
        static List<Tufo> _grama;

        static Relevo Relevo2 => _relevo ?? (_relevo = new Relevo(2f, 7));
        static GradeDoChao Grade => _grade ?? (_grade = new GradeDoChao(Relevo2));
        static List<Tufo> GramaDaIlha => _grama ?? (_grama = PlantioDaGrama.Grama(Grade));

        static float DistH(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ---------------------------------------------------------------- kit

        [Test]
        public void Kit_PlantioEhDeterministico_PorSeed()
        {
            // defeito: sorteio por System.Random() sem seed / por relogio -> duas ilhas iguais plantam diferente
            foreach (PecaDoKit peca in PlantioDoKit.Pecas)
            {
                List<Plantio> a = PlantioDoKit.Posicoes(Relevo2, PlantioDoKit.SeedKit, peca);
                List<Plantio> b = PlantioDoKit.Posicoes(new Relevo(2f, 7), PlantioDoKit.SeedKit, peca);
                Assert.AreEqual(a.Count, b.Count, peca.Id);
                for (int i = 0; i < a.Count; i++)
                {
                    Assert.AreEqual(a[i].Pos, b[i].Pos, peca.Id + " #" + i);
                    Assert.AreEqual(a[i].GiroGraus, b[i].GiroGraus, 0f, peca.Id + " #" + i);
                    Assert.AreEqual(a[i].Escala, b[i].Escala, 0f, peca.Id + " #" + i);
                }
            }
            List<Plantio> x = PlantioDoKit.Posicoes(Relevo2, PlantioDoKit.SeedKit, PlantioDoKit.Pecas[1]);
            List<Plantio> y = PlantioDoKit.Posicoes(Relevo2, PlantioDoKit.SeedKit + 1, PlantioDoKit.Pecas[1]);
            Assert.IsTrue(x.Count > 0 && y.Count > 0);
            Assert.AreNotEqual(x[0].Pos, y[0].Pos, "seed diferente, plantio diferente");
        }

        [Test]
        public void Kit_ContagemEscalaComAArea_DobrarOLadoQuadruplica()
        {
            // defeito: contagem cravada (sem x AREA) -> o mapa 4x maior recebe as MESMAS pecas e esvazia
            var r1 = new Relevo(1f, 7);
            int total1 = 0, total2 = 0;
            foreach (PecaDoKit peca in PlantioDoKit.Pecas)
            {
                Assert.AreEqual(4 * PlantioDoKit.Alvo(r1, peca), PlantioDoKit.Alvo(Relevo2, peca), peca.Id + ": alvo x AREA");
                if (peca.Onde != OndeNasce.Aberto) continue;
                int n1 = PlantioDoKit.Posicoes(r1, PlantioDoKit.SeedKit, peca).Count;
                int n2 = PlantioDoKit.Posicoes(Relevo2, PlantioDoKit.SeedKit, peca).Count;
                Assert.GreaterOrEqual(n1, Mathf.FloorToInt(PlantioDoKit.Alvo(r1, peca) * 0.9f), peca.Id + " na ilha de 300 m");
                Assert.GreaterOrEqual(n2, Mathf.FloorToInt(PlantioDoKit.Alvo(Relevo2, peca) * 0.9f), peca.Id + " na ilha de 600 m");
                total1 += n1;
                total2 += n2;
            }
            Assert.GreaterOrEqual(total2, total1 * 3.5f, "plantado de verdade (pecas do aberto): " + total1 + " -> " + total2);
        }

        [Test]
        public void Kit_NuncaNaAgua_NuncaEmCimaDeNascimento_PeNoChao()
        {
            // defeitos: tirar o PodePousar (o arco da beira do lago cai na agua); tirar a folga do nascimento
            // (a rocha de 15 m engole o ponto onde o mago surge); pousar pela origem (a malha boia na encosta)
            var ocupados = new List<Vector4>();
            int total = 0;
            foreach (PecaDoKit peca in PlantioDoKit.Pecas)
            {
                float livre = PlantioDoKit.RaioLivre(peca);
                foreach (Plantio pl in PlantioDoKit.Posicoes(Relevo2, PlantioDoKit.SeedKit, peca, ocupados))
                {
                    total++;
                    Assert.IsTrue(Relevo2.PodePousar(pl.Pos.x, pl.Pos.z), peca.Id + " na agua/praia em " + pl.Pos);
                    Assert.LessOrEqual(pl.Pos.y, Relevo2.Altura(pl.Pos.x, pl.Pos.z) + 1e-3f, peca.Id + " boiando em " + pl.Pos);
                    foreach (Vector3 n in Relevo2.Nascimentos)
                        Assert.GreaterOrEqual(DistH(pl.Pos, n), livre, peca.Id + " em cima do nascimento " + n);
                }
            }
            Assert.Greater(total, 100, "o kit planta de verdade na ilha de 600 m");
        }

        // ---------------------------------------------------------------- grama

        [Test]
        public void Grama_SoNasceEmChaoPousavel()
        {
            // defeito: confiar so' na grade (piso 1,05 m do Godot, ou a borda do disco d'agua que passa da
            // cava) -> tufo na praia e dentro do lago
            List<Tufo> g = GramaDaIlha;
            Assert.Greater(g.Count, 60000, "campina densa: " + g.Count + " tufos");
            foreach (Tufo t in g)
                Assert.IsTrue(Relevo2.PodePousar(t.Pos.x, t.Pos.z), "tufo fora do chao pousavel em " + t.Pos);
        }

        [Test]
        public void Grama_TufoPousaNaMalhaDesenhada()
        {
            // defeito: plantar pela Altura() exata -> nas dobras o tufo boia ou afunda ate' ~0,3 m no quad de 4,5 m
            foreach (Tufo t in GramaDaIlha.GetRange(0, 2000))
                Assert.AreEqual(Grade.AlturaNaMalha(t.Pos.x, t.Pos.z) - 0.06f, t.Pos.y, 1e-4f);
            // e nos vertices a grade E' o relevo (a malha passa por eles)
            float passo = Relevo2.Lado / Ilha.Quads;
            for (int i = 10; i < Ilha.Quads; i += 17)
            {
                float x = -Relevo2.Lado * 0.5f + i * passo, z = -Relevo2.Lado * 0.5f + (Ilha.Quads - i) * passo;
                Assert.AreEqual(Relevo2.Altura(x, z), Grade.AlturaNaMalha(x, z), 1e-3f);
            }
        }

        [Test]
        public void Grama_DensidadePorBioma()
        {
            // defeito: densidade parelha em todo bioma -> a mata, forrada de moita, vira gramado de campina
            var tufos = new Dictionary<Bioma, int>();
            foreach (Tufo t in GramaDaIlha)
            {
                Bioma b = Grade.BiomaEm(t.Pos.x, t.Pos.z);
                tufos[b] = (tufos.TryGetValue(b, out int n) ? n : 0) + 1;
            }
            var area = new Dictionary<Bioma, int>();
            float L = Relevo2.RaioTerra;
            for (float x = -L; x <= L; x += 3f)
                for (float z = -L; z <= L; z += 3f)
                {
                    Bioma b = Grade.BiomaEm(x, z);
                    area[b] = (area.TryGetValue(b, out int n) ? n : 0) + 1;
                }
            float Dens(Bioma b) => (tufos.TryGetValue(b, out int t) ? t : 0) / (float)Mathf.Max(1, area.TryGetValue(b, out int a) ? a : 0);
            Assert.AreEqual(0, tufos.TryGetValue(Bioma.Agua, out int agua) ? agua : 0, "grama na agua");
            Assert.AreEqual(0, tufos.TryGetValue(Bioma.Praia, out int praia) ? praia : 0, "grama na praia");
            Assert.AreEqual(0, tufos.TryGetValue(Bioma.Alagado, out int brejo) ? brejo : 0, "grama no brejo (la' e' junco)");
            float campina = Dens(Bioma.Campina), mata = Dens(Bioma.Floresta), dunas = Dens(Bioma.Dunas);
            Assert.Greater(mata, 0f, "a mata tem grama, so' menos");
            Assert.Greater(campina, mata * 1.4f, "campina " + campina + " x mata " + mata);
            Assert.Greater(campina, dunas * 4f, "campina " + campina + " x dunas " + dunas + ": o areal e' o campo ABERTO");
        }

        [Test]
        public void Grama_PlantioEhDeterministico()
        {
            List<Tufo> a = PlantioDaGrama.Flores(Grade);
            List<Tufo> b = PlantioDaGrama.Flores(Grade);
            Assert.Greater(a.Count, 200);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i += 37) Assert.AreEqual(a[i].Pos, b[i].Pos);
            Assert.AreNotEqual(PlantioDaGrama.Flores(Grade, 72)[0].Pos, a[0].Pos, "seed diferente, flor diferente");
        }

        // ---------------------------------------------------------------- culling

        [Test]
        public void Culling_EhHorizontal_CelulaNoAltoEmCimaDoJogadorContinuaVisivel()
        {
            // defeito (a licao da leva 6): distancia 3D -> a mesma celula a 1.000 m de altura sumiria, e o
            // corte passaria a depender da ALTURA em vez do chao em volta do jogador
            var jogador = new Vector3(120f, 3f, -40f);
            Assert.IsTrue(PlantioDaGrama.Visivel(jogador, new Vector3(120f, 1000f, -40f), PlantioDaGrama.CorteGrama));
            Assert.IsTrue(PlantioDaGrama.Visivel(jogador, new Vector3(120f + 79f, 0f, -40f), PlantioDaGrama.CorteGrama));
            Assert.IsFalse(PlantioDaGrama.Visivel(jogador, new Vector3(120f + 60f, 0f, -40f + 60f), PlantioDaGrama.CorteGrama),
                "85 m na horizontal: alem do corte de 80");
            Assert.IsFalse(PlantioDaGrama.Visivel(jogador, new Vector3(120f, 3f, -40f + 50f), PlantioDaGrama.CorteFlor),
                "flor e' pixel de cor: 50 m ja' corta");
        }

        [Test]
        public void Culling_FadeAcabaAntesDoCorte_NenhumTufoEmPeSomeDeUmaVez()
        {
            // defeito: fade do shader alem de (corte - meia diagonal) -> tufo ainda em pe' na quina da celula
            // cortada some de uma vez (pop). Com a grama da ilha de 600 m: 80 - 21 = ~59 m, o 56 do Godot.
            float passo = Relevo2.Lado / PlantioDaGrama.CelulasPorLado(Relevo2);
            Assert.AreEqual(30f, passo, 1e-3f, "a celula continua com ~30 m no mapa grande");
            foreach (float corte in new[] { PlantioDaGrama.CorteGrama, PlantioDaGrama.CorteFlor, PlantioDaGrama.CorteSeixo, PlantioDaGrama.CorteJunco })
            {
                float fim = PlantioDaGrama.FimDoFade(corte, passo);
                Assert.Greater(fim, PlantioDaGrama.RampaDoFade, "o fade de " + corte + " m tem rampa inteira");
                Assert.LessOrEqual(fim + passo * 0.70710678f, corte + 0.01f, "corte " + corte + ": fade acaba em " + fim);
            }
            float fimGrama = PlantioDaGrama.FimDoFade(PlantioDaGrama.CorteGrama, passo);
            Assert.IsTrue(fimGrama > 50f && fimGrama < 65f, "o colapso da grama fica onde o Godot mediu: " + fimGrama);
        }

        // ---------------------------------------------------------------- atmosfera e castelo

        [Test]
        public void Nevoa_DeixaAQuedaLegivel_EFechaAntesDoFar()
        {
            // defeitos: a nevoa calibrada pro mapa pequeno (34/220 no Godot) come o quadro aereo inteiro;
            // fim alem do far -> o mar para no far com cor de mar e o corte aparece contra o ceu
            Assert.AreEqual(0f, Ilha.NevoaEm(40f), 1e-4f, "o combate perto fica limpo");
            Assert.Less(Ilha.NevoaEm(200f), 0.2f, "a 200 m a nevoa come menos de 20%");
            Assert.Less(Ilha.NevoaEm(400f), 0.5f, "do castelo (320 m de altura) a ilha ainda se le'");
            Assert.AreEqual(1f, Ilha.NevoaEm(Ilha.FarDaCamera - 10f), 1e-4f, "no far o mar ja' e' cor de nevoa");
        }

        [Test]
        public void Castelo_EscalaPelaEnvergaduraDoGodot()
        {
            // defeito: carregar o modelo sem escala -> um .glb normalizado pela Meshy (~1,9) vira um tijolo no ceu
            Assert.AreEqual(1f, Castelo.EscalaDoModelo(new Vector3(35.8f, 52f, 25.75f)), 1e-3f, "o castelo.glb do Godot ja' tem 35,8 m");
            Assert.AreEqual(35.8f / 1.9f, Castelo.EscalaDoModelo(new Vector3(1.9f, 1.4f, 1.2f)), 1e-3f, "normalizado pela Meshy");
            Assert.AreEqual(1f, Castelo.EscalaDoModelo(Vector3.zero), 0f, "bounds vazio nao explode");
            Assert.AreEqual(1f, Castelo.EscalaDoModelo(new Vector3(float.NaN, 1f, float.NaN)), 0f, "NaN nao explode");
        }
    }
}
