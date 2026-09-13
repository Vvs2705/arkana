using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// ONDA 13C — o que a praia e o cume prometem, nas classes PURAS (Praia.Plantio, PlantioDaGrama.SeixosDoCume). Cada teste fica
    /// vermelho reintroduzindo o defeito que ele guarda (anotado em cada um). Sem chamada interna do Unity: rodam tambem na sonda.
    /// </summary>
    public class WorldPraiaTests
    {
        static Relevo _relevo;
        static GradeDoChao _grade;
        static List<Praia.PecaPlantada> _praia;

        static Relevo Relevo2 => _relevo ?? (_relevo = new Relevo(2f, 7));
        static GradeDoChao Grade => _grade ?? (_grade = new GradeDoChao(Relevo2));
        static List<Praia.PecaPlantada> PraiaDaIlha => _praia ?? (_praia = Praia.Plantio(Relevo2));

        static Vector3 Pe(Praia.PecaPlantada p) => p.M.GetColumn(3);

        static float DistH(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static List<Praia.PecaPlantada> Do(Praia.Peca t) => PraiaDaIlha.FindAll(p => p.Tipo == t);

        [Test]
        public void Praia_SoNaAreiaOuNoMarRaso_NuncaNoLagoNemNoBrejo()
        {
            // defeitos: plantar pelo Bioma.Campina/pela cota so' (capim no gramado, tronco no morro); tirar o LongeDaAguaDoce
            // (rocha com o pe' na lamina do lago, tronco na lama do brejo); rocha do mar alem da agua rasa (some)
            Relevo r = Relevo2;
            foreach (Praia.PecaPlantada p in PraiaDaIlha)
            {
                Vector3 c = Pe(p);
                float h = r.Altura(c.x, c.z), areia = r.Solo(c.x, c.z, h).g;
                var xz = new Vector2(c.x, c.z);
                Assert.Greater(Vector2.Distance(xz, r.Lago), r.LagoDiscoR, p.Tipo + " no lago em " + c);
                Assert.Greater(Vector2.Distance(xz, r.Alagado), r.AlagadoDiscoR, p.Tipo + " no brejo em " + c);
                switch (p.Tipo)
                {
                    case Praia.Peca.Barco:
                        Assert.AreEqual(Bioma.Praia, r.BiomaEm(c.x, c.z), "barco fora da praia em " + c);
                        break;
                    case Praia.Peca.Tronco:
                        Assert.That(h, Is.InRange(Praia.TroncoMin, Praia.TroncoMax), "tronco fora da linha da mare' em " + c);
                        Assert.GreaterOrEqual(areia, 0.7f, "tronco fora da areia em " + c);
                        break;
                    case Praia.Peca.Rocha:
                        Assert.GreaterOrEqual(h, Praia.AguaRasa, "rocha afogada em " + c);
                        Assert.IsTrue(h < 0.05f || areia >= 0.7f, "rocha fora da areia e fora do mar em " + c + " (areia " + areia + ")");
                        break;
                    case Praia.Peca.Capim:
                        Assert.That(h, Is.InRange(Praia.CapimMin, Praia.CapimMax), "capim fora da areia alta em " + c);
                        Assert.GreaterOrEqual(areia, Praia.AreiaDoCapim, "capim no gramado em " + c);
                        Assert.AreEqual(Relevo.Seco, r.SuperficieDaAgua(c.x, c.z), "capim na agua em " + c);
                        break;
                }
            }
        }

        [Test]
        public void Praia_ContagensDaIlhaDe600m()
        {
            // defeitos: contagem cravada sem x AREA, sorteio que desiste cedo (a praia volta a ficar vazia), nenhuma rocha no mar
            Assert.That(Do(Praia.Peca.Barco).Count, Is.InRange(1, 2), "barcos");
            Assert.That(Do(Praia.Peca.Tronco).Count, Is.InRange(15, 25), "troncos");
            List<Praia.PecaPlantada> rochas = Do(Praia.Peca.Rocha);
            Assert.That(rochas.Count, Is.InRange(25, 40), "rochas");
            int noMar = rochas.FindAll(p => Relevo2.Altura(Pe(p).x, Pe(p).z) < 0f).Count;
            Assert.GreaterOrEqual(noMar, rochas.Count / 5, "rochas com o pe' no mar: " + noMar);
            Assert.That(Do(Praia.Peca.Capim).Count, Is.InRange(120, 200), "touceiras de capim");
            Assert.AreEqual(1, Praia.Plantio(new Relevo(1f, 7)).FindAll(p => p.Tipo == Praia.Peca.Barco).Count, "a ilha de 300 m tem um barco");
        }

        [Test]
        public void Praia_DeterministicaPorSeed()
        {
            // defeito: System.Random / relogio -> duas partidas com a mesma seed com praias diferentes
            List<Praia.PecaPlantada> a = PraiaDaIlha, b = Praia.Plantio(new Relevo(2f, 7));
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Tipo, b[i].Tipo, "#" + i);
                Assert.IsTrue(a[i].M == b[i].M, "#" + i + " em " + Pe(a[i]));
            }
            List<Praia.PecaPlantada> c = Praia.Plantio(new Relevo(2f, 8));
            Assert.IsTrue(c.Count != a.Count || !(c[c.Count - 1].M == a[a.Count - 1].M), "outra ilha, outra praia");
        }

        [Test]
        public void Praia_BarcoNaBeiraDAgua_ComPedrasETroncosEmVolta()
        {
            // defeitos: barco solto no meio da areia seca (a agua longe da foto); barco sem a cena em volta; barco de pe' (nao tombado)
            foreach (Praia.PecaPlantada b in Do(Praia.Peca.Barco))
            {
                Vector3 c = Pe(b);
                bool agua = false;
                for (float d = 0f; d <= 12f && !agua; d += 0.5f) agua = Relevo2.Altura(c.x + b.Mar.x * d, c.z + b.Mar.y * d) < 0f;
                Assert.IsTrue(agua, "o mar a menos de 12 m do barco em " + c);
                int rochas = 0, troncos = 0;
                foreach (Praia.PecaPlantada p in PraiaDaIlha)
                {
                    if (DistH(Pe(p), c) > 8f) continue;
                    if (p.Tipo == Praia.Peca.Rocha) rochas++;
                    if (p.Tipo == Praia.Peca.Tronco) troncos++;
                }
                Assert.GreaterOrEqual(rochas, 2, "pedras em volta do barco em " + c);
                Assert.GreaterOrEqual(troncos, 1, "tronco em volta do barco em " + c);
                float tombo = Vector3.Angle(b.M.GetColumn(1), Vector3.up);
                Assert.That(tombo, Is.InRange(6f, 30f), "o barco deita de lado na areia em " + c);
            }
        }

        [Test]
        public void Praia_PecaAssentada_BaseAbaixoDaMalha_ParteAMostra()
        {
            // defeitos: assentar pelo centro (na ladeira a quina de baixo boia), pela Altura() exata (ate' 0,23 m fora da malha
            // desenhada), afundar pela altura inteira (a rocha some na areia), capim com o disco de areia boiando
            foreach (Praia.PecaPlantada p in PraiaDaIlha)
            {
                Vector3 tam = Praia.Tamanho[(int)p.Tipo];
                for (int i = 0; i < 3; i++)
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 o = p.M.MultiplyPoint3x4(new Vector3((i - 1) * 0.5f * tam.x, 0f, (k - 1) * 0.5f * tam.z));
                        Assert.LessOrEqual(o.y, Grade.AlturaNaMalha(o.x, o.z) - 0.03f, p.Tipo + " com a base boiando em " + Pe(p));
                    }
                Vector3 topo = p.M.MultiplyPoint3x4(new Vector3(0f, tam.y, 0f));
                float chao = Mathf.Max(Grade.AlturaNaMalha(topo.x, topo.z), Relevo.AguaY);
                Assert.Greater(topo.y - chao, 0.25f * tam.y * p.Escala, p.Tipo + " engolido pelo chao (ou pela agua) em " + Pe(p));
            }
        }

        [Test]
        public void Praia_QuemColideFicaLongeDosNascimentos()
        {
            // defeito: tirar a folga -> o mago nasce dentro do barco ou de uma rocha da praia das dunas
            foreach (Praia.PecaPlantada p in PraiaDaIlha)
            {
                if (!p.Colide) continue;
                foreach (Vector3 n in Relevo2.Nascimentos)
                    Assert.GreaterOrEqual(DistH(Pe(p), n), 6f + 0.5f * Praia.Tamanho[(int)p.Tipo].x * p.Escala, p.Tipo + " em cima do nascimento " + n);
            }
            Assert.Greater(PraiaDaIlha.FindAll(p => p.Colide).Count, 20, "barco, troncos e rochas grandes colidem");
            Assert.AreEqual(0, Do(Praia.Peca.Capim).FindAll(p => p.Colide).Count, "capim nao colide");
        }

        [Test]
        public void Praia_CapimEmManchas_NaoEspalhadoPorIgual()
        {
            // defeito: sortear touceira solta no anel inteiro -> 160 pontinhos a ~20 m um do outro (plantacao, nao duna)
            List<Praia.PecaPlantada> c = Do(Praia.Peca.Capim);
            float soma = 0f;
            foreach (Praia.PecaPlantada a in c)
            {
                float perto = float.MaxValue;
                foreach (Praia.PecaPlantada b in c)
                    if (!(a.M == b.M)) perto = Mathf.Min(perto, DistH(Pe(a), Pe(b)));
                soma += perto;
                Assert.Greater(perto, 0.3f, "duas touceiras uma dentro da outra em " + Pe(a));
            }
            Assert.Less(soma / c.Count, 2.5f, "vizinho mais perto medio do capim");
        }

        [Test]
        public void Praia_LodECortePelaDistancia3D()
        {
            // defeitos: LOD ou corte pela horizontal (do castelo, bem em cima da praia, o capim de 1,3K tris inteiro em LOD0 e
            // desenhado: o capim da Meshy nao tem o colapso do shader da grama)
            var p = new Vector3(40f, 0.5f, -268f);
            Assert.IsFalse(Praia.Lod1(p + new Vector3(Praia.DistanciaLod1 - 1f, 1.8f, 0f), p), "perto: LOD0");
            Assert.IsTrue(Praia.Lod1(p + new Vector3(0f, 320f, 0f), p), "do castelo: LOD1");
            Assert.IsTrue(Praia.CapimVisivel(p + new Vector3(0f, 1.8f, Praia.CorteCapim - 2f), p), "a 78 m ainda desenha");
            Assert.IsFalse(Praia.CapimVisivel(p + new Vector3(60f, 0f, 60f), p), "85 m: cortado");
            Assert.IsFalse(Praia.CapimVisivel(p + new Vector3(0f, 120f, 0f), p), "da queda, bem em cima: cortado");
        }

        // ---------------------------------------------------------------- o cume

        [Test]
        public void SeixoDoCume_SoNaTampaDoPico_EscalaGiroEAssentado()
        {
            // defeitos: tirar o corte da tampa (seixo da Meshy na encosta e na campina); escala sem a faixa (lasca de 2 m); tombo
            // livre (laje de pe'); pousar pelo centro (na ladeira a laje boia)
            List<Tufo> s = PlantioDaGrama.SeixosDoCume(Grade);
            Assert.That(s.Count, Is.InRange(250, 500), "seixos da Meshy no cume");
            foreach (Tufo t in s)
            {
                Assert.GreaterOrEqual(Grade.AlturaNaMalha(t.Pos.x, t.Pos.z), Relevo.PicoH + 4f, "seixo fora da tampa em " + t.Pos);
                // o quadrado do sorteio (+-PicoR): a rampa subivel passa do raio e ainda e' tampa acima de 22 m
                Assert.LessOrEqual(Vector2.Distance(new Vector2(t.Pos.x, t.Pos.z), Relevo2.Pico), Relevo2.PicoR * 1.5f, "seixo fora do pico em " + t.Pos);
                Assert.That(t.Escala.x, Is.InRange(PlantioDaGrama.SeixoCumeMin, PlantioDaGrama.SeixoCumeMax));
                Assert.AreEqual(t.Escala.x, t.Escala.y, 1e-5f, "a Meshy nao estica");
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(0f, t.Giro.x)), 6f);
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(0f, t.Giro.z)), 6f);
                float r = 0.28f * t.Escala.x;
                foreach (Vector2 d in new[] { Vector2.zero, Vector2.right, Vector2.left, Vector2.up, Vector2.down })
                    Assert.Less(t.Pos.y, Grade.AlturaNaMalha(t.Pos.x + d.x * r, t.Pos.z + d.y * r), "seixo boiando em " + t.Pos);
            }
            List<Tufo> b = PlantioDaGrama.SeixosDoCume(Grade);
            for (int i = 0; i < s.Count; i += 23) Assert.AreEqual(s[i].Pos, b[i].Pos, "deterministico #" + i);
        }

        [Test]
        public void SeixoDoCume_ALevaProceduralDoCumeSai_ADaIlhaNaoMuda()
        {
            // defeitos: as duas levas no cume (a lasca clara chapada continua la', por baixo da Meshy); o sorteio da ilha mexido
            // (os seixos da praia e da campina mudam de lugar)
            List<Tufo> com = PlantioDaGrama.Seixos(Grade), sem = PlantioDaGrama.Seixos(Grade, 81, false);
            Assert.Greater(sem.Count, 1000, "a leva da ilha continua");
            Assert.Less(sem.Count, com.Count);
            for (int i = 0; i < sem.Count; i++)
            {
                Assert.AreEqual(com[i].Pos, sem[i].Pos, "a leva da ilha mudou no #" + i);
                Assert.LessOrEqual(sem[i].Pos.y, 13.5f, "seixo procedural no cume em " + sem[i].Pos);
            }
        }
    }
}
