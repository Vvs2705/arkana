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

        [Test]
        public void Kit_Altar_ComoAFicha_BraseiroNoCentro_PentagonoComDoisTombados_PlataformasFrenteAFrente()
        {
            // defeitos: plantar o altar pelo sorteio solto (o pentagono some); nenhum obelisco tombado (a historia "o mundo
            // esqueceu a Sintonia" some); plataformas do mesmo lado; altar fora do vale ou dentro d'agua
            var ocupados = new List<Vector4>();
            List<Plantio>[] a = PlantioDoKit.Altar(Relevo2, ocupados);
            Assert.AreEqual(1, a[0].Count, "um braseiro");
            Assert.AreEqual(5, a[1].Count, "cinco obeliscos");
            Assert.AreEqual(2, a[2].Count, "duas plataformas");
            Vector3 c = a[0][0].Pos;
            int tombados = 0;
            foreach (Plantio o in a[1])
            {
                if (o.TomboGraus != 0f) { tombados++; Assert.Greater(DistH(o.Pos, c), PlantioDoKit.RaioDoAltar + 0.5f, "tombado cai para FORA"); }
                else Assert.AreEqual(PlantioDoKit.RaioDoAltar, DistH(o.Pos, c), 0.01f, "de pe' na ponta do pentagono");
            }
            Assert.AreEqual(2, tombados, "dois obeliscos mortos");
            Vector3 u = a[2][0].Pos - c, v = a[2][1].Pos - c;
            Assert.AreEqual(DistH(a[2][0].Pos, c), DistH(a[2][1].Pos, c), 0.01f);
            Assert.Less(u.x * v.x + u.z * v.z, 0f, "frente a frente, o braseiro no meio");
            foreach (List<Plantio> l in a)
                foreach (Plantio p in l) Assert.IsTrue(Relevo2.PodePousar(p.Pos.x, p.Pos.z), "altar fora do chao pousavel em " + p.Pos);
            Assert.Less(new Vector2(c.x, c.z).magnitude, Relevo2.RaioTerra * 0.25f, "no VALE, perto do centro: " + c);
            Assert.AreEqual(1, ocupados.Count, "o altar reserva o chao dele para o plantio solto");
        }

        [Test]
        public void Kit_PecasDasRuinas_NoAnelDoPlato_DeFrenteParaOCentro()
        {
            // defeitos: sortear as pecas das ruinas no mapa inteiro (a estatua-vigia no meio da duna); giro sorteado
            // (a estatua de costas para o circulo que ela vigia)
            int n = 0;
            foreach (PecaDoKit peca in PlantioDoKit.Pecas)
            {
                if (peca.Onde != OndeNasce.Ruinas) continue;
                foreach (Plantio pl in PlantioDoKit.Posicoes(Relevo2, PlantioDoKit.SeedKit, peca))
                {
                    n++;
                    Vector2 d = Relevo2.Ruinas - new Vector2(pl.Pos.x, pl.Pos.z);
                    Assert.That(d.magnitude, Is.InRange(Relevo2.RuinasR * 0.74f, Relevo2.RuinasR * 1.36f), peca.Id);
                    Assert.AreEqual(0f, Mathf.DeltaAngle(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, pl.GiroGraus), 0.01f, peca.Id + " de costas para o centro");
                }
            }
            Assert.Greater(n, 8, "arcos, colunas-braseiro e estatuas nas ruinas");
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

        // ---------------------------------------------------------------- pedregulhos (onda 5B)

        [Test]
        public void Pedregulho_RochaDaMeshyAssentaNaMalha_DentroDaCaixaDoBlob()
        {
            // defeitos: assentar pelo centro (na encosta de 0,6 a quina de baixo boia: fresta sob a pedra); pela Altura() exata
            // (ate' 0,23 m acima da malha nas cristas); escalar so' pelo molde (pedra gigante, ou seixo onde o colisor de 0,8 s
            // promete obstaculo); afundar demais (a pedra some no morro)
            var molde = new Bounds(new Vector3(0.003f, 0.2365f, 0f), new Vector3(0.952f, 0.473f, 1f));   // o 18-pedregulho.glb no Unity
            List<Matrix4x4> rochas = Vegetacao.PlantioDasRochas(Relevo2);
            Assert.AreEqual(232, rochas.Count, "58 x AREA na ilha de 600 m");
            var rng = new Sorteio(52);
            foreach (Matrix4x4 r in rochas)
            {
                Matrix4x4 m = Vegetacao.AssentarRocha(Relevo2, r, molde, rng);
                Vector3 pos = r.GetColumn(3);
                float s = r.GetColumn(0).magnitude, altura = m.GetColumn(1).magnitude * molde.size.y;
                for (int i = 0; i < 3; i++)
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 b = m.MultiplyPoint3x4(new Vector3(Mathf.Lerp(molde.min.x, molde.max.x, i * 0.5f), molde.min.y,
                            Mathf.Lerp(molde.min.z, molde.max.z, k * 0.5f)));
                        Assert.LessOrEqual(b.y, Grade.AlturaNaMalha(b.x, b.z) - 0.1f * altura, "base boiando na pedra de " + pos);
                    }
                Vector3 topo = m.MultiplyPoint3x4(new Vector3(molde.center.x, molde.max.y, molde.center.z));
                Assert.Greater(topo.y - Grade.AlturaNaMalha(topo.x, topo.z), 0.15f * altura, "pedra engolida pelo chao em " + pos);
                Assert.That(m.GetColumn(0).magnitude * molde.size.x / (2f * s), Is.InRange(0.84f, 1.16f), "largura fora do blob em " + pos);
                Assert.That(altura / (1.125f * s), Is.InRange(0.79f, 1.21f), "altura fora do blob em " + pos);
            }
        }

        [Test]
        public void Seixo_ATampaDoPicoTemSeixo()
        {
            // defeito: o teto de 13 m do seixo (e o de 12,5 da grama) deixava lisa a tampa do pico, onde o treino acontece
            int cume = 0;
            foreach (Tufo t in PlantioDaGrama.Seixos(Grade))
                if (t.Pos.y > Relevo.PicoH + 4f) cume++;
            Assert.Greater(cume, 800, "seixos na tampa do pico: " + cume);
        }

        // ---------------------------------------------------------------- ruinas da Meshy (onda 10A)

        [Test]
        public void Ruinas_PedraDaMeshy_NoLugarDeSempre_PeNaMalha_DentroDaPegada()
        {
            // defeitos: sorteio a mais no rng 31 (a coluna muda de altura e giro); pegada nova, maior ou fora do lugar (o KitCenario,
            // que planta pelas pegadas, muda a ilha inteira); a Meshy maior que a pegada (arco do kit dentro da coluna); pe' pela
            // Altura() exata ou so' pelo centro (fresta sob o plinto na encosta); enterrar demais (muralha engolida); coluna achatada
            // ate' o sy do prisma (capitel panqueca); bloco de cima da ponta boiando ou atravessado no de baixo
            var coluna = new Bounds(new Vector3(0.00228f, 2.1f, -0.00203f), new Vector3(1.36748f, 4.2f, 1.37316f));   // o 38-coluna-ruina.glb no Unity
            var bloco = new Bounds(new Vector3(0.00248f, 0.69253f, 0.00461f), new Vector3(1.8f, 1.38506f, 0.88905f));   // o 39-bloco-ruina.glb
            var pegadas = new List<Vector4>();
            List<Ruinas.PedraDaRuina> pedras = Ruinas.Plantio(Relevo2, coluna, bloco, pegadas);

            // o registro de ANTES da Meshy (o prisma de 12/09, medido): contagem, pegadas e o sorteio 31 nao se mexem
            var n = new int[4];
            foreach (Ruinas.PedraDaRuina p in pedras) n[(int)p.Tipo]++;
            Assert.AreEqual(new[] { 22, 2, 1, 34 }, n, "colunas em pe', caidas, toco do altar, blocos");
            Assert.AreEqual(53, pegadas.Count, "22 colunas + 2 caidas + 29 trechos de muralha");
            Assert.Less(Vector4.Distance(new Vector4(152f, -146f, 0f, 0.9f), pegadas[0]), 1e-3f, "pegada 0: " + pegadas[0]);
            Assert.Less(Vector4.Distance(new Vector4(152.153961f, -130.9f, 0f, 2.31f), pegadas[2]), 1e-3f, "a coluna caida: " + pegadas[2]);
            Assert.Less(Vector4.Distance(new Vector4(166.720078f, -115.119705f, 0f, 1.3f), pegadas[52]), 1e-3f, "a ponta da muralha leste: " + pegadas[52]);
            Assert.AreEqual(0.92689544f, pedras[0].Proc.GetColumn(1).magnitude, 1e-4f, "altura sorteada da coluna 0");
            Assert.AreEqual(0.5804657f, pedras[23].Proc.GetColumn(1).magnitude, 1e-4f, "altura sorteada da ultima coluna");
            Vector4 z = pedras[55].Proc.GetColumn(2);
            Assert.AreEqual(-71.5932f, Mathf.Atan2(z.x, z.z) * Mathf.Rad2Deg, 0.01f, "giro sorteado do ultimo bloco da muralha");

            // a Meshy: dentro da pegada DELA (o altar, no miolo que o KitCenario ja' reserva), a base inteira abaixo da malha
            // desenhada, parte a mostra; o bloco de cima sentado no de baixo
            Vector3 Pe(Vector4 q) => new Vector3(q.x, 0f, q.y);
            Vector2 c = Relevo2.Ruinas;
            int emCima = 0;
            float topoAnterior = 0f;
            foreach (Ruinas.PedraDaRuina p in pedras)
            {
                Bounds m = p.Tipo == Ruinas.TipoDePedra.Bloco ? bloco : coluna;
                var pts = new Vector3[27];
                float baixo = float.MaxValue, alto = float.MinValue;
                for (int i = 0; i < 27; i++)
                {
                    pts[i] = p.Meshy.MultiplyPoint3x4(m.min + Vector3.Scale(m.size, new Vector3(i % 3, i / 3 % 3, i / 9) * 0.5f));
                    baixo = Mathf.Min(baixo, pts[i].y);
                    alto = Mathf.Max(alto, pts[i].y);
                }
                Vector3 centro = p.Meshy.MultiplyPoint3x4(m.center);
                var dona = new Vector4(c.x, c.y, 0f, Relevo2.RuinasR * 0.7f);
                if (DistH(centro, Pe(dona)) > 6f)
                {
                    foreach (Vector4 q in pegadas) if (DistH(centro, Pe(q)) < DistH(centro, Pe(dona))) dona = q;
                    Assert.Less(DistH(centro, Pe(dona)), 0.01f, p.Tipo + " fora do centro da pegada em " + centro);
                }
                foreach (Vector3 o in pts) Assert.LessOrEqual(DistH(o, Pe(dona)), dona.w, p.Tipo + " maior que a pegada em " + centro);

                float chao = Grade.AlturaNaMalha(centro.x, centro.z);
                if (baixo > chao + 0.3f)
                {
                    emCima++;
                    Assert.That(topoAnterior - baixo, Is.InRange(0.05f, 0.3f), "bloco de cima boiando ou atravessado em " + centro);
                }
                else
                {
                    foreach (Vector3 o in pts)
                        if (o.y < baixo + 0.01f) Assert.LessOrEqual(o.y, Grade.AlturaNaMalha(o.x, o.z) - 0.05f, p.Tipo + " com fresta sob a base em " + centro);
                    Assert.Greater(alto - chao, 0.6f * (alto - baixo), p.Tipo + " engolida pelo chao em " + centro);
                    if (p.Tipo == Ruinas.TipoDePedra.Coluna || p.Tipo == Ruinas.TipoDePedra.Toco)
                        Assert.That(alto - baixo, Is.InRange(2.8f, 4.4f), "coluna achatada ou esticada em " + centro);
                }
                topoAnterior = alto;
            }
            Assert.AreEqual(4, emCima, "a 2a camada das 3 pontas da muralha e o bloco de cima do altar");
        }

        // ---------------------------------------------------------------- arvores da Meshy (onda 6B)

        [Test]
        public void Arvore_DaMeshy_NoLugarDeSempre_PinheiroSoNaMata_PeNaMalha_LodPelaDistancia3D()
        {
            // defeitos: sorteio a mais no rng 21 (a mata inteira muda de lugar e o terreno reativo queima outra arvore); pinheiro
            // na campina ou mata sem pinheiro; pe' pela Altura() exata (raiz boiando nas dobras da malha) ou enterrado; inclinacao
            // sem teto (mata bebada); a Meshy esticada; LOD pela distancia HORIZONTAL (do castelo, a 320 m, a mata toda em LOD0)
            List<Vegetacao.ArvorePlantada> a = Vegetacao.PlantioDasArvores(Relevo2);
            Assert.AreEqual(615, a.Count, "150 + 26 por AREA: a mata cheia e as avulsas na ilha de 600 m");
            // o registro de ANTES da Meshy (a procedural de 12/09, medido): PosArvore(i) nao se mexe
            Assert.Less(Vector3.Distance(new Vector3(-103.2437f, 7.026678f, -106.0411f), a[0].Pos), 1e-3f, "arvore 0 em " + a[0].Pos);
            Assert.Less(Vector3.Distance(new Vector3(-194.3382f, 4.247117f, -104.7702f), a[510].Pos), 1e-3f, "ultima da mata em " + a[510].Pos);
            Assert.Less(Vector3.Distance(new Vector3(50.42117f, 4.697743f, 15.64644f), a[614].Pos), 1e-3f, "ultima avulsa em " + a[614].Pos);

            var floresta = new Vector3(Relevo2.Floresta.x, 0f, Relevo2.Floresta.y);
            int mata = 0, pinheiros = 0;
            var tintas = new int[Vegetacao.Variantes];
            foreach (Vegetacao.ArvorePlantada t in a)
            {
                bool naMata = DistH(t.Pos, floresta) <= Relevo2.FlorestaR;
                if (naMata) mata++;
                if (t.Especie == Vegetacao.Especie.Pinheiro)
                {
                    pinheiros++;
                    Assert.IsTrue(naMata, "pinheiro fora da mata em " + t.Pos);
                }
                tintas[t.Variante]++;
                Vector3 pe = t.Meshy.GetColumn(3), cima = t.Meshy.GetColumn(1), lado = t.Meshy.GetColumn(0);
                Assert.Less(DistH(pe, t.Pos), 1e-3f, "a Meshy fora do registro em " + t.Pos);
                float chao = Grade.AlturaNaMalha(pe.x, pe.z);
                Assert.LessOrEqual(pe.y, chao - 0.05f, "pe' boiando em " + t.Pos);
                Assert.Greater(pe.y, chao - 1f, "arvore enterrada em " + t.Pos);
                Assert.LessOrEqual(Vector3.Angle(cima, Vector3.up), Vegetacao.Inclinacao * 1.42f, "arvore bebada em " + t.Pos);
                Assert.That(lado.magnitude, Is.InRange(0.84f, 1.31f), "escala em " + t.Pos);
                Assert.That(cima.magnitude / lado.magnitude, Is.InRange(0.94f, 1.11f), "a Meshy esticada em " + t.Pos);
            }
            Assert.AreEqual(511, mata, "a mata fechada");
            Assert.That(pinheiros / (float)mata, Is.InRange(0.55f, 0.75f), "pinheiros na mata: " + pinheiros + " de " + mata);
            foreach (int n in tintas) Assert.Greater(n, a.Count / 5, "as tres tintas aparecem: " + string.Join("/", tintas));

            Vector3 p = a[0].Pos;
            Assert.IsFalse(Vegetacao.Lod1(p + new Vector3(Vegetacao.DistanciaLod1 - 1f, 1.8f, 0f), p), "perto: LOD0");
            Assert.IsTrue(Vegetacao.Lod1(p + new Vector3(0f, 1.8f, Vegetacao.DistanciaLod1 + 1f), p), "longe: LOD1");
            Assert.IsTrue(Vegetacao.Lod1(p + new Vector3(0f, 320f, 0f), p), "do castelo, bem em cima da mata: LOD1");
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
