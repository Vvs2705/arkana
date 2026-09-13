using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// O que a raia MUNDO promete sobre a ilha (espelho do world/selftest.gd, sobre a classe PURA).
    /// Cada teste fica vermelho reintroduzindo o defeito que ele guarda.
    /// </summary>
    public class WorldRelevoTests
    {
        static Relevo Nova() => new Relevo(2f, 7);

        static readonly Vector2[] Amostras =
        {
            new Vector2(0, 0), new Vector2(37, -81), new Vector2(-120, 60), new Vector2(150, 60),
            new Vector2(24, 168), new Vector2(-200, 10), new Vector2(90, -190), new Vector2(-14, 233),
        };

        [Test]
        public void Altura_EhDeterministica_ParaOMesmoSeed()
        {
            var a = new Relevo(2f, 7);
            var b = new Relevo(2f, 7);
            foreach (var p in Amostras)
                Assert.AreEqual(a.Altura(p.x, p.y), b.Altura(p.x, p.y), 0f, "mesmo seed, mesma ilha em " + p);
        }

        [Test]
        public void Altura_MudaComSeedDiferente()
        {
            var a = new Relevo(2f, 7);
            var b = new Relevo(2f, 8);
            int diferentes = 0;
            foreach (var p in Amostras)
                if (Mathf.Abs(a.Altura(p.x, p.y) - b.Altura(p.x, p.y)) > 0.01f) diferentes++;
            Assert.Greater(diferentes, Amostras.Length / 2, "seed diferente tem que dar colinas diferentes");
        }

        [Test]
        public void RaioTerra_ELado_EscalamComEscala()
        {
            Assert.AreEqual(132f, new Relevo(1f).RaioTerra, 1e-3f);
            Assert.AreEqual(264f, new Relevo(2f).RaioTerra, 1e-3f);
            Assert.AreEqual(600f, new Relevo(2f).Lado, 1e-3f);
            Assert.AreEqual(2f * new Relevo(1f).Lago.x, new Relevo(2f).Lago.x, 1e-3f, "o centro do lago acompanha o knob");
        }

        [Test]
        public void Vertical_NaoEscala_PlatoNoveMetros_PicoAlto()
        {
            // Dobrar a altura junto tornaria toda ladeira insubivel: 9 m e ~26 m em QUALQUER escala.
            foreach (float esc in new[] { 1f, 2f })
            {
                var r = new Relevo(esc, 7);
                Assert.AreEqual(9f, r.Altura(r.Ruinas.x, r.Ruinas.y), 0.35f, "plato das ruinas na escala " + esc);
                float pico = r.Altura(r.Pico.x, r.Pico.y);
                Assert.IsTrue(pico > 20f && pico < 32f, "pico com " + pico + " m na escala " + esc);
                Assert.Greater(pico - r.Altura(0f, 0f), 15f, "o pico se ergue sobre o vale: silhueta, nao so' tinta");
            }
        }

        [Test]
        public void Pois_SaoSete_DentroDoRaioTerra_ComChaoPousavel()
        {
            var r = Nova();
            Assert.AreEqual(7, r.Pois.Length);
            var nomes = new HashSet<string>();
            foreach (var poi in r.Pois)
            {
                Assert.IsTrue(nomes.Add(poi.Nome), "nome repetido: " + poi.Nome);
                Assert.Less(poi.Centro.magnitude, r.RaioTerra, poi.Nome + " cabe no raio de terra");
                Vector3 p;
                Assert.IsTrue(r.PontoPousavelNoPoi(poi, out p), poi.Nome + " tem chao pousavel dentro dele");
                Assert.IsTrue(r.PodePousar(p.x, p.z), poi.Nome + ": o ponto achado e' pousavel");
                Assert.LessOrEqual(Vector2.Distance(poi.Centro, new Vector2(p.x, p.z)), poi.Raio * 2.5f + 0.01f);
            }
            // a ordem e' contrato (o loot cicla por indice): os 4 do Godot primeiro, alfabeticos
            Assert.AreEqual("alagado", r.Pois[0].Nome);
            Assert.AreEqual("floresta", r.Pois[1].Nome);
            Assert.AreEqual("lago", r.Pois[2].Nome);
            Assert.AreEqual("ruinas", r.Pois[3].Nome);
        }

        [Test]
        public void Pois_RaioEhFracaoDoRaioTerra()
        {
            var r1 = new Relevo(1f, 7);
            var r2 = new Relevo(2f, 7);
            for (int i = 0; i < r1.Pois.Length; i++)
            {
                Assert.AreEqual(r1.Pois[i].RaioFracao, r2.Pois[i].RaioFracao, 1e-6f, "a fracao nao muda com a escala");
                Assert.AreEqual(r1.Pois[i].Raio * 2f, r2.Pois[i].Raio, 1e-3f, "o raio em metros dobra com a escala");
                Assert.AreEqual(r2.Pois[i].RaioFracao * r2.RaioTerra, r2.Pois[i].Raio, 1e-3f);
                Assert.IsTrue(r1.Pois[i].RaioFracao > 0f && r1.Pois[i].RaioFracao < 1f);
            }
        }

        [Test]
        public void Nascimentos_PeloMenos14_Distintos_EmChaoSeco()
        {
            var r = Nova();
            // 1 jogador + 12 bots precisa de 13 DISTINTOS; 14 e' o teto estrutural documentado
            Assert.GreaterOrEqual(r.Nascimentos.Length, 14);
            for (int i = 0; i < r.Nascimentos.Length; i++)
            {
                Vector3 a = r.Nascimentos[i];
                Assert.IsTrue(r.PodePousar(a.x, a.z), "nascimento " + i + " em chao pousavel");
                Assert.AreEqual(Relevo.Seco, r.SuperficieDaAgua(a.x, a.z), "nascimento " + i + " fora d'agua");
                Assert.Greater(a.y, r.Altura(a.x, a.z), "nascimento " + i + " ACIMA do chao");
                for (int j = i + 1; j < r.Nascimentos.Length; j++)
                {
                    Vector3 b = r.Nascimentos[j];
                    float d = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                    Assert.GreaterOrEqual(d, Relevo.SeparacaoNascimentos, "nascimentos " + i + " e " + j + " valem um so'");
                }
            }
        }

        [Test]
        public void Nascimentos_CobremAIlha_NaoSoOMiolo()
        {
            var r = Nova();
            float maisLonge = 0f;
            foreach (var n in r.Nascimentos) maisLonge = Mathf.Max(maisLonge, new Vector2(n.x, n.z).magnitude);
            Assert.Greater(maisLonge, r.RaioTerra * 0.5f, "o nascimento mais distante passa de meia-terra");
        }

        [Test]
        public void PodePousar_FalsoNaAgua_ENaPraia()
        {
            var r = Nova();
            Assert.IsFalse(r.PodePousar(r.Lago.x, r.Lago.y), "centro do lago e' agua");
            Assert.IsFalse(r.PodePousar(r.Alagado.x, r.Alagado.y), "centro do alagado e' agua");
            Assert.IsFalse(r.PodePousar(r.RaioTerra + 60f, 0f), "mar aberto");
            Assert.AreEqual(Relevo.LagoY, r.SuperficieDaAgua(r.Lago.x, r.Lago.y), 1e-4f);
            Assert.AreEqual(Relevo.AguaY, r.SuperficieDaAgua(r.RaioTerra + 60f, 0f), 1e-4f);
            // areia seca abaixo da praia: chao SEM agua e com cota entre 0 e PraiaY — nao pousa
            int achou = 0;
            for (int k = 0; k < 64; k++)
            {
                float a = Mathf.PI * 2f * k / 64f;
                for (float d = r.RaioTerra + 40f; d > 40f; d -= 1f)
                {
                    float x = Mathf.Cos(a) * d, z = Mathf.Sin(a) * d;
                    float h = r.Altura(x, z);
                    if (h > 0.2f && h < Relevo.PraiaY && r.SuperficieDaAgua(x, z) == Relevo.Seco)
                    {
                        Assert.IsFalse(r.PodePousar(x, z), "praia nao e' chao de spawn");
                        achou++;
                        break;
                    }
                }
            }
            Assert.Greater(achou, 10, "a ilha tem praia em volta");
        }

        [Test]
        public void PodePousar_SemTetoDeAltura_PlatoEPico()
        {
            // DEFEITO (27/08): a janela 1,4-8,5 m cravada em tres arquivos proibia o plato das ruinas
            // (9,0 m) e o topo do pico. Reintroduzir o teto deixa este teste vermelho.
            var r = Nova();
            float ruinas = r.Altura(r.Ruinas.x, r.Ruinas.y);
            float pico = r.Altura(r.Pico.x, r.Pico.y);
            Assert.Greater(ruinas, 8.5f, "o plato passa do teto antigo");
            Assert.Greater(pico, 8.5f, "o pico passa do teto antigo");
            Assert.IsTrue(r.PodePousar(r.Ruinas.x, r.Ruinas.y), "pousa no plato das ruinas");
            Assert.IsTrue(r.PodePousar(r.Pico.x, r.Pico.y), "pousa no topo do pico");
        }

        [Test]
        public void Costa_EhIrregular()
        {
            // `fall` em funcao do raio puro faz a ilha virar um disco (variacao 0). Varre de fora
            // para dentro: a primeira terra e' a costa, bacia interna nenhuma engana.
            var r = Nova();
            float lo = 1e9f, hi = 0f;
            for (int k = 0; k < 64; k++)
            {
                float a = Mathf.PI * 2f * k / 64f;
                float dx = Mathf.Cos(a), dz = Mathf.Sin(a);
                float d = r.RaioTerra + 55f * r.Escala;
                while (d > 20f && r.Altura(dx * d, dz * d) <= 0f) d -= 0.5f;
                lo = Mathf.Min(lo, d);
                hi = Mathf.Max(hi, d);
            }
            Assert.Greater(hi - lo, 14f * r.Escala, "a linha d'agua varia entre enseada e ponta");
        }

        [Test]
        public void Ao_EscureceDobras_EDeixaChaoAbertoLimpo()
        {
            var r = Nova();
            float lo = 2f, hi = 0f;
            int n1 = Ilha.Quads + 1;
            for (int iz = 0; iz < n1; iz += 5)
                for (int ix = 0; ix < n1; ix += 5)
                {
                    float x = ((float)ix / Ilha.Quads - 0.5f) * r.Lado;
                    float z = ((float)iz / Ilha.Quads - 0.5f) * r.Lado;
                    float h = r.Altura(x, z);
                    float ao = r.Ao(x, z, h, r.NormalY(x, z));
                    lo = Mathf.Min(lo, ao);
                    hi = Mathf.Max(hi, ao);
                }
            Assert.Less(lo, 0.82f, "dobras e bacias escurecem de verdade (AO constante seria barrado)");
            Assert.Greater(hi, 0.98f, "chao aberto continua limpo");
        }

        static Color Tinta(Relevo r, Vector2 c, float raio)
        {
            float sr = 0f, sg = 0f, sb = 0f;
            int n = 0;
            for (float dx = -raio; dx <= raio; dx += raio / 6f)
                for (float dz = -raio; dz <= raio; dz += raio / 6f)
                {
                    if (dx * dx + dz * dz > raio * raio) continue;
                    float x = c.x + dx, z = c.y + dz;
                    float h = r.Altura(x, z);
                    Color col = r.Cor(x, z, h);
                    float ao = r.Ao(x, z, h, r.NormalY(x, z));
                    sr += col.r * ao; sg += col.g * ao; sb += col.b * ao;
                    n++;
                }
            return new Color(sr / n, sg / n, sb / n);
        }

        [Test]
        public void LeituraAerea_CadaPoiEhUmaManchaPropria()
        {
            // De 200 m nao existe detalhe: o que chega no olho e' a cor MEDIA de cada pedaco de chao.
            // Cada POI tem que ser distinto da campina E dos outros (0,10 na soma dos canais, sRGB).
            var r = Nova();
            var t = new Dictionary<string, Color>
            {
                { "campina", Tinta(r, Vector2.zero, 26f * r.Escala) },
                { "floresta", Tinta(r, r.Floresta, r.FlorestaR * 0.6f) },
                { "ruinas", Tinta(r, r.Ruinas, r.RuinasR * 0.6f) },
                { "alagado", Tinta(r, r.Alagado, r.AlagadoR * 0.5f) },
                { "dunas", Tinta(r, r.Dunas, r.DunasR * 0.5f) },
                { "pico", Tinta(r, r.Pico, r.PicoTopo) },
            };
            var nomes = new List<string>(t.Keys);
            for (int i = 0; i < nomes.Count; i++)
                for (int j = i + 1; j < nomes.Count; j++)
                {
                    Color a = t[nomes[i]], b = t[nomes[j]];
                    float d = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                    Assert.Greater(d, 0.10f, nomes[i] + "/" + nomes[j] + " viram a mesma mancha a 200 m");
                }
        }

        [Test]
        public void Bioma_RotulaOsCentros()
        {
            var r = Nova();
            Assert.AreEqual(Bioma.Campina, r.BiomaEm(0f, 0f));
            Assert.AreEqual(Bioma.Floresta, r.BiomaEm(r.Floresta.x, r.Floresta.y));
            Assert.AreEqual(Bioma.Ruinas, r.BiomaEm(r.Ruinas.x, r.Ruinas.y));
            Assert.AreEqual(Bioma.Agua, r.BiomaEm(r.Lago.x, r.Lago.y));
            Assert.AreEqual(Bioma.Agua, r.BiomaEm(r.Alagado.x, r.Alagado.y));
            Assert.AreEqual(Bioma.Pico, r.BiomaEm(r.Pico.x, r.Pico.y));
            Assert.AreEqual(Bioma.Dunas, r.BiomaEm(r.Dunas.x, r.Dunas.y));
            Assert.AreEqual(Bioma.Agua, r.BiomaEm(r.RaioTerra + 60f, 0f));
        }

        [Test]
        public void Cor_AlfaCarregaOPesoDeRocha()
        {
            // O shader troca detalhe-chao por detalhe-rocha pelo alfa: plato e vale sao chao (0), a escarpa do pico e' rocha.
            var r = Nova();
            Assert.Less(r.Cor(0f, 0f, r.Altura(0f, 0f)).a, 0.1f, "vale central nao e' rocha");
            float pior = 0f;
            for (int k = 0; k < 32; k++)
            {
                float a = Mathf.PI * 2f * k / 32f;
                float x = r.Pico.x + Mathf.Cos(a) * r.PicoR * 0.6f, z = r.Pico.y + Mathf.Sin(a) * r.PicoR * 0.6f;
                pior = Mathf.Max(pior, r.Cor(x, z, r.Altura(x, z)).a);
            }
            Assert.Greater(pior, 0.5f, "a escarpa do pico e' rocha em algum setor");
        }

        [Test]
        public void Cor_CumeDoPico_EhPedraQuente_ComMancha_NaoNeveLisa()
        {
            // DEFEITO (fotos 02/05/18 de 12/09): o cume saia branco-lilas e LISO (CorPico #dfe3ee a 0,72 por cima de tudo)
            // e o Diretor leu placeholder. Medido: antes r-b = -0,05 e spread 0,04; agora +0,06 e 0,12. O cume e' pedra
            // clara QUENTE com mancha de regiao — a de perto (fissura, liquen, estrato) e' do shader, fora do alcance daqui.
            var r = Nova();
            float sr = 0f, sb = 0f, lo = 9f, hi = 0f;
            int n = 0;
            for (float dx = -r.PicoTopo; dx <= r.PicoTopo; dx += 2f)
                for (float dz = -r.PicoTopo; dz <= r.PicoTopo; dz += 2f)
                {
                    float x = r.Pico.x + dx, z = r.Pico.y + dz, h = r.Altura(x, z);
                    if (h < Relevo.PicoH + 8f) continue;   // so' a tampa cheia
                    Color c = r.Cor(x, z, h);
                    sr += c.r; sb += c.b; n++;
                    float s = c.r + c.g + c.b;
                    lo = Mathf.Min(lo, s);
                    hi = Mathf.Max(hi, s);
                }
            Assert.Greater(n, 50, "a tampa do pico existe");
            Assert.Greater((sr - sb) / n, 0.03f, "cume quente (pedra e terra), nao neve azulada");
            Assert.Greater(hi - lo, 0.09f, "o cume tem mancha que se le' do alto, nao e' uma chapa lisa");
        }

        [Test]
        public void Solo_ComposicaoDoChao_PorBioma()
        {
            // O shader pinta cada bioma de perto (_Chao, onda 7B) pela composicao que a Ilha grava no UV0: r mata, g areia,
            // b pisado, a grama. Se a conta quebrar, a campina vira mata, a praia perde a areia molhada ou a terra batida some
            // dos nascimentos — calado, so' na foto.
            var r = Nova();
            System.Func<float, float, Color> solo = (x, z) => r.Solo(x, z, r.Altura(x, z));
            Color vale = solo(0f, 0f), mata = solo(r.Floresta.x, r.Floresta.y), duna = solo(r.Dunas.x, r.Dunas.y), pico = solo(r.Pico.x, r.Pico.y);
            Assert.Greater(vale.a, 0.8f, "o vale e' campina");
            Assert.Less(vale.r + vale.g, 0.2f, "o vale nao e' mata nem areia");
            Assert.Greater(mata.r, 0.8f, "o miolo da floresta e' chao de mata");
            Assert.Less(mata.a, 0.2f, "e nao campina");
            Assert.Greater(duna.g, 0.8f, "o areal e' areia");
            Assert.Less(pico.r + pico.g + pico.a, 0.3f, "o cume e' pedra: o solo vivo nao pinta la'");

            // a linha d'agua e' areia em volta da ilha (e' la' que o shader poe a areia molhada)
            int costa = 0, areia = 0;
            for (int k = 0; k < 48; k++)
            {
                float a = Mathf.PI * 2f * k / 48f, dx = Mathf.Cos(a), dz = Mathf.Sin(a);
                for (float d = r.RaioTerra + 50f; d > 40f; d -= 0.5f)
                {
                    float h = r.Altura(dx * d, dz * d);
                    if (h < 0.2f) continue;
                    if (h < 0.6f) { costa++; if (solo(dx * d, dz * d).g > 0.8f) areia++; }
                    break;
                }
            }
            Assert.Greater(costa, 30, "a costa foi achada");
            Assert.Greater(areia, costa * 0.8f, "a beira d'agua e' areia em quase toda a volta");

            // pisado: cheio em todo nascimento; zero na campina longe de todos eles e das ruinas
            foreach (Vector3 n in r.Nascimentos)
                Assert.Greater(solo(n.x, n.z).b, 0.95f, "o nascimento " + n + " e' chao pisado");
            bool achou = false;
            for (float x = -200f; x <= 200f && !achou; x += 8f)
                for (float z = -200f; z <= 200f && !achou; z += 8f)
                {
                    if (r.BiomaEm(x, z) != Bioma.Campina || Vector2.Distance(new Vector2(x, z), r.Ruinas) < r.RuinasR + 35f) continue;
                    bool longe = true;
                    foreach (Vector3 n in r.Nascimentos) if (Vector2.Distance(new Vector2(x, z), new Vector2(n.x, n.z)) < 45f) longe = false;
                    if (!longe) continue;
                    Assert.AreEqual(0f, solo(x, z).b, 1e-4f, "campina longe de nascimento e ruina nao e' pisada em " + x + "," + z);
                    achou = true;
                }
            Assert.IsTrue(achou, "a ilha tem campina longe de todo nascimento");

            // a composicao nunca passa de 1: cada camada que pinta cobre as de baixo na mesma fracao (a conta da cor)
            for (float x = -260f; x <= 260f; x += 13f)
                for (float z = -260f; z <= 260f; z += 13f)
                {
                    Color s = solo(x, z);
                    Assert.LessOrEqual(s.r + s.g + s.a, 1.0001f, "composicao estourou em " + x + "," + z);
                    Assert.GreaterOrEqual(Mathf.Min(Mathf.Min(s.r, s.g), Mathf.Min(s.b, s.a)), 0f, "peso negativo em " + x + "," + z);
                }
        }

        [Test]
        public void Ruinas_Praca_CorDoVerticeCabeNoCalcamento_EEhPedraQuente()
        {
            // A PRACA de pedra (onda 9B): o shader calca o plato ate' Relevo.BordaDaPraca do raio com cor PROPRIA (a lingua so' vai
            // para fora) e a cor do vertice e' so' a media dele (alto, minimapa, sem shader). Guarda o que so' a foto pegaria:
            // (1) o disco de cor acaba antes da borda do calcamento — senao sobra um anel de cinza liso em volta da praca; (2) o
            // miolo e' praca e pedra QUENTE (o CorRocha lilas de antes era o disco de plastico); (3) a Ilha entrega ao shader o
            // centro, o raio e a borda deste Relevo.
            var r = Nova();
            Vector4 praca = Ilha.PracaDasRuinas(r);
            Assert.AreEqual(r.Ruinas.x, praca.x, 1e-4f, "centro x da praca");
            Assert.AreEqual(r.Ruinas.y, praca.y, 1e-4f, "centro z da praca");
            Assert.AreEqual(r.RuinasR, praca.z, 1e-4f, "raio da praca");
            Assert.AreEqual(Relevo.BordaDaPraca, praca.w, 1e-6f, "borda do calcamento");
            float quente = 0f;
            for (int k = 0; k < 36; k++)
            {
                float ang = Mathf.PI * 2f * k / 36f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 b = r.Ruinas + dir * r.RuinasR * Relevo.BordaDaPraca;
                Color sb = r.Solo(b.x, b.y, r.Altura(b.x, b.y));
                Assert.Greater(sb.r + sb.g + sb.a, 0.97f, "na borda do calcamento a cor do vertice ja' e' o chao de volta, nao a praca, em " + b);
                Vector2 q = r.Ruinas + dir * r.RuinasR * 0.5f;
                float h = r.Altura(q.x, q.y);
                Assert.Less(r.Solo(q.x, q.y, h).a, 0.1f, "o miolo e' praca, nao campina, em " + q);
                Color c = r.Cor(q.x, q.y, h);
                quente += (c.r - c.b) / 36f;
            }
            Assert.Greater(quente, 0.03f, "a praca e' pedra quente, nao o lilas frio do disco liso");
        }

        [Test]
        public void Sorteio_EhDeterministico_EDiferePorSeed()
        {
            var a = new Sorteio(21);
            var b = new Sorteio(21);
            var c = new Sorteio(22);
            bool difere = false;
            for (int i = 0; i < 50; i++)
            {
                float va = a.Float(), vb = b.Float(), vc = c.Float();
                Assert.AreEqual(va, vb, 0f);
                Assert.IsTrue(va >= 0f && va < 1f);
                if (va != vc) difere = true;
            }
            Assert.IsTrue(difere);
            Assert.IsTrue(new Sorteio(0).Float() >= 0f, "seed zero nao mata o gerador");
        }
    }

    /// <summary>A sombra acompanha a queda (world/Sol.gd): abre no ar, fecha ao pousar, com teto.</summary>
    public class WorldSolTests
    {
        [Test]
        public void AlcancePara_NoChao_EEmAlturaInvalida_Fecha()
        {
            Assert.AreEqual(SombraDoSol.Perto, SombraDoSol.AlcancePara(0f), 1e-4f);
            Assert.AreEqual(SombraDoSol.Perto, SombraDoSol.AlcancePara(-3f), 1e-4f);
            Assert.AreEqual(SombraDoSol.Perto, SombraDoSol.AlcancePara(float.NaN), 1e-4f, "NaN barrado");
        }

        [Test]
        public void AlcancePara_Subindo_Abre_ComTeto_EMonotonico()
        {
            Assert.Greater(SombraDoSol.AlcancePara(50f), SombraDoSol.Perto);
            Assert.GreaterOrEqual(SombraDoSol.AlcancePara(200f), 300f, "a 200 m cobre a ilha");
            Assert.AreEqual(SombraDoSol.Longe, SombraDoSol.AlcancePara(9999f), 1e-4f, "tem teto");
            float anterior = 0f;
            foreach (float m in new[] { 0f, 25f, 60f, 120f, 200f, 400f })
            {
                float a = SombraDoSol.AlcancePara(m);
                Assert.GreaterOrEqual(a, anterior, "mais alto nunca da' menos sombra");
                anterior = a;
            }
            // DEFEITO: alcance cravado em 60 m nao cobre a altura do castelo
            Assert.Less(60f, 200f);
            Assert.Greater(SombraDoSol.AlcancePara(200f), 60f);
        }
    }
}
