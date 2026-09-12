using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>A logica PURA do VisualDaPartida: a malha da parede, o modelo e a coluna do loot, a queda do bau, o anel da zona.</summary>
    public class VisualPartidaTests
    {
        [SetUp]
        public void SetUp() { Bus.Reset(); }

        // ------------------------------------------------------------- parede da zona

        [Test]
        public void Parede_64Segmentos_ContagemCerta_DuasFaces_FechaAVolta()
        {
            const int n = 64;
            const float h = 220f;
            MalhaDados m = MalhaDaParede.Cilindro(n, h);
            int vs = (n + 1) * 2 * 2;   // (n+1) colunas (costura repetida) x pe'/topo x 2 faces
            Assert.AreEqual(vs, m.Vertices.Length);
            Assert.AreEqual(vs, m.Normais.Length);
            Assert.AreEqual(vs, m.Uvs.Length);
            Assert.AreEqual(n * 2 * 2 * 3, m.Triangulos.Length, "2 triangulos por quad, por face");
            foreach (int i in m.Triangulos) Assert.That(i >= 0 && i < vs, "indice dentro da malha (fora = erro de log no Unity)");

            int fora = 0, dentro = 0;
            for (int i = 0; i < vs; i++)
            {
                Vector3 p = m.Vertices[i], nrm = m.Normais[i];
                var radial = new Vector3(p.x, 0f, p.z);
                Assert.AreEqual(1f, radial.magnitude, 1e-4f, "raio 1: a casca escala pelo raio da zona");
                Assert.That(Mathf.Abs(p.y) < 1e-4f || Mathf.Abs(p.y - h) < 1e-3f, "so' pe' (0) e topo (220)");
                float d = Vector3.Dot(nrm, radial.normalized);
                Assert.AreEqual(1f, Mathf.Abs(d), 1e-4f, "normal horizontal, na direcao do raio");
                if (d > 0f) fora++; else dentro++;
            }
            Assert.AreEqual(vs / 2, fora, "metade dos vertices e' a face de fora");
            Assert.AreEqual(vs / 2, dentro, "metade e' a face de dentro");

            // cada triangulo gira para o lado da sua normal (frente do Unity) e a soma das areas da' a volta INTEIRA
            float areaFora = 0f, areaDentro = 0f;
            for (int t = 0; t < m.Triangulos.Length; t += 3)
            {
                int ia = m.Triangulos[t], ib = m.Triangulos[t + 1], ic = m.Triangulos[t + 2];
                Vector3 a = m.Vertices[ia], b = m.Vertices[ib], c = m.Vertices[ic];
                Vector3 g = Vector3.Cross(b - a, c - a);
                Assert.Greater(Vector3.Dot(g, m.Normais[ia]), 0f, "ordem do triangulo casa com a normal (senao um lado some)");
                Vector3 centro = (a + b + c) / 3f;
                if (Vector3.Dot(m.Normais[ia], new Vector3(centro.x, 0f, centro.z)) > 0f) areaFora += g.magnitude * 0.5f;
                else areaDentro += g.magnitude * 0.5f;
            }
            float esperada = n * 2f * Mathf.Sin(Mathf.PI / n) * h;   // prisma de 64 lados inscrito no raio 1
            Assert.AreEqual(esperada, areaFora, esperada * 1e-3f, "a face de fora fecha a volta (nenhum gomo faltando)");
            Assert.AreEqual(esperada, areaDentro, esperada * 1e-3f, "a de dentro tambem");

            // a costura: para cada vertice em u=0 ha' um gemeo na MESMA posicao e normal com u=1 (a textura rola sem rasgo)
            int costuras = 0;
            for (int i = 0; i < vs; i++)
            {
                if (m.Uvs[i].x != 0f) continue;
                bool achou = false;
                for (int j = 0; j < vs && !achou; j++)
                    achou = m.Uvs[j].x == 1f && m.Uvs[j].y == m.Uvs[i].y
                         && (m.Vertices[j] - m.Vertices[i]).sqrMagnitude < 1e-10f && m.Normais[j] == m.Normais[i];
                Assert.IsTrue(achou, "vertice " + i + " em u=0 sem gemeo em u=1");
                costuras++;
            }
            Assert.AreEqual(4, costuras, "pe' e topo, nas duas faces");
        }

        // ------------------------------------------------------------- loot

        [Test]
        public void Loot_TodoArmaIdDoRegistro_TemModelo_EOArquivoExiste()
        {
            Assert.AreEqual(LootVisual.ModeloDe(Arma.VARINHA), LootVisual.ModeloDe("inexistente"),
                "id desconhecido cai na luva comum, o mesmo fallback de Arma.Dados");
            Assert.AreEqual(LootVisual.ModeloDe(Arma.VARINHA), LootVisual.ModeloDe(null));
            var nomes = new HashSet<string>();
            foreach (string id in Arma.ARMAS.Keys)
            {
                string nome = LootVisual.ModeloDe(id);
                Assert.IsFalse(string.IsNullOrEmpty(nome), id);
                Assert.IsTrue(nomes.Add(nome), "cada luva tem o SEU modelo: " + nome);
            }
            // o nome tem que bater com um arquivo de Resources: nome errado vira primitiva CALADA na partida
            foreach (string nome in nomes)
            {
                string arq = Path.Combine(Application.dataPath, "_Arkana", "Resources", nome + ".glb");
                Assert.IsTrue(File.Exists(arq), "Resources.Load(\"" + nome + "\") nao acharia nada: " + arq);
            }
        }

        [Test]
        public void Coluna_SoNasRaridadesAltas_ECresceComOTier()
        {
            Assert.AreEqual(0f, LootVisual.AlturaDaColuna("comum"), "12 varinhas com farol viram ruido");
            Assert.AreEqual(0f, LootVisual.AlturaDaColuna(null));
            Assert.AreEqual(0f, LootVisual.AlturaDaColuna("inexistente"));
            float anterior = -1f;
            foreach (string id in Arma.TIERS)
            {
                float h = LootVisual.AlturaDaColuna(Arma.Raridade(id));
                Assert.AreEqual(Arma.Tier(id) >= 1, h > 0f, id + ": coluna so' do raro para cima");
                Assert.Greater(h, anterior, id + ": mais raro = farol mais alto (escolhe o rumo de longe)");
                anterior = h;
            }
        }

        // ------------------------------------------------------------- bau

        [Test]
        public void Bau_NaQueda_DesceMonotonico_EaseOut_ETerminaNoPouso()
        {
            var pouso = new Vector3(12f, 3.5f, -40f);
            Vector3 ini = BauVisual.PosNaQueda(pouso, 0f);
            Assert.AreEqual(pouso.y + BauCelestial.ALTURA_QUEDA, ini.y, 1e-3f, "o anuncio comeca la' em cima");
            float antes = float.PositiveInfinity;
            for (int i = 0; i <= 100; i++)
            {
                Vector3 p = BauVisual.PosNaQueda(pouso, i / 100f);
                Assert.AreEqual(pouso.x, p.x, "cai na vertical");
                Assert.AreEqual(pouso.z, p.z, "cai na vertical");
                Assert.Less(p.y, antes, "desce SEMPRE (t=" + i / 100f + ")");
                antes = p.y;
            }
            Assert.AreEqual(pouso, BauVisual.PosNaQueda(pouso, 1f), "termina no ponto de pouso");
            Assert.Less(BauVisual.PosNaQueda(pouso, 0.5f).y - pouso.y, BauCelestial.ALTURA_QUEDA * 0.5f,
                "ease-out: na metade do tempo ja' passou da metade (queda acelerada esconderia o bau)");
            Assert.AreEqual(pouso, BauVisual.PosNaQueda(pouso, 7f), "t alem de 1 nao fura o chao");
            Assert.AreEqual(ini, BauVisual.PosNaQueda(pouso, float.NaN), "NaN barrado: fica no alto, nunca NaN no transform");
        }

        // ------------------------------------------------------------- zona

        [Test]
        public void Zona_Anel_MostraOAvisadoNaEspera_EOMesmoAlvoNoFecha()
        {
            Zona.Circulo c;
            Assert.IsFalse(ZonaVisual.Proximo(null, out c), "treino: sem zona");
            var z = new Zona(null, Zona.SEED_ZONA);
            Assert.IsFalse(ZonaVisual.Proximo(z, out c), "inerte: nada no chao");
            z.LigarNoPouso();
            Assert.IsFalse(ZonaVisual.Proximo(z, out c), "abertura: a tempestade ainda nao existe");
            z.Tick(Zona.ABERTURA_S);
            Assert.AreEqual(Zona.Estado.Formando, z.EstadoAtual);
            Assert.IsFalse(ZonaVisual.Proximo(z, out c), "formando: anuncio, sem alvo");
            z.Tick(Zona.FORMACAO_S);
            Assert.AreEqual(Zona.Estado.Espera, z.EstadoAtual);
            Assert.IsTrue(ZonaVisual.Proximo(z, out c), "espera: o proximo circulo ja' esta' no chao");
            Assert.AreEqual(z.Plano[0].Centro, c.Centro);
            Assert.AreEqual(z.Plano[0].Raio, c.Raio);
            z.Tick(Zona.FASES[0].Espera);
            Assert.AreEqual(Zona.Estado.Fecha, z.EstadoAtual);
            Assert.AreEqual(1, z.FaseAtual, "no FECHA a fase ja' avancou");
            Assert.IsTrue(ZonaVisual.Proximo(z, out c), "fechando: o anel fica ate' a parede chegar");
            Assert.AreEqual(z.Plano[0].Centro, c.Centro, "o alvo de quem fecha e' o MESMO circulo avisado");
            Assert.AreEqual(z.Plano[0].Raio, c.Raio);
            z.ForcarCirculo(Zona.FASES.Length, Vector3.zero, 0f);
            Assert.IsFalse(ZonaVisual.Proximo(z, out c), "a tempestade tomou o mapa: nao ha' proximo");
        }
    }
}
