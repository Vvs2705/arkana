using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Terrain;

namespace Arkana.Tests
{
    /// <summary>
    /// A LEITURA dos kits e do terreno reativo (cor + forma por estado, sumico pelo restante, teto de fogo no mobile,
    /// fio que treme sem sair das ancoras) e o MURO COMO COBERTURA: o tiro para no muro, nao acerta quem esta' atras,
    /// e o muro perde hp.
    /// </summary>
    public class VisualKitsTerrenoTests
    {
        private const float DT = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset();
            Arkana.Menu.Menu.PedidoDeTreino = false;
        }

        [TearDown]
        public void TearDown() { Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        [Test]
        public void TodoEstado_TemCorEForma_EDistintasEntreSi()
        {
            var estados = (EstadoCelula[])Enum.GetValues(typeof(EstadoCelula));
            foreach (EstadoCelula s in estados)
            {
                Assert.AreNotEqual(LeituraDoTerreno.SemCor, LeituraDoTerreno.CorDoEstado(s), s + " ficou sem cor");
                if (s != EstadoCelula.Normal)
                    Assert.AreNotEqual(FormaCelula.Nenhuma, LeituraDoTerreno.FormaDoEstado(s), s + " ficou sem forma (cor + FORMA, GDD §10)");
            }
            // entre os DESENHADOS (Normal nao se desenha): cor longe o bastante para ler, forma nunca repetida
            for (int i = 0; i < estados.Length; i++)
                for (int j = i + 1; j < estados.Length; j++)
                {
                    if (estados[i] == EstadoCelula.Normal || estados[j] == EstadoCelula.Normal) continue;
                    Color a = LeituraDoTerreno.CorDoEstado(estados[i]), b = LeituraDoTerreno.CorDoEstado(estados[j]);
                    float d = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                    Assert.Greater(d, 0.2f, estados[i] + " e " + estados[j] + " com cores quase iguais");
                    Assert.AreNotEqual(LeituraDoTerreno.FormaDoEstado(estados[i]), LeituraDoTerreno.FormaDoEstado(estados[j]),
                        estados[i] + " e " + estados[j] + " com a mesma forma: daltonico nao separa");
                }
            // o muro mostra quanto aguenta
            Assert.AreEqual(0, LeituraDoTerreno.EstagioDoMuro(Balance.Terrain.WallHp), "muro novo = inteiro");
            Assert.AreEqual(1, LeituraDoTerreno.EstagioDoMuro(Balance.Terrain.WallHp * 0.5f), "meio hp = rachado");
            Assert.AreEqual(2, LeituraDoTerreno.EstagioDoMuro(Balance.Terrain.WallHp * 0.2f), "pouco hp = em ruina");
        }

        [Test]
        public void Escala_SomeEmRestanteZero_EInteiraEnquantoVivo()
        {
            Assert.AreEqual(0f, LeituraDosKits.EscalaPorRestante(0f, 5f), "Restante 0 = some");
            Assert.AreEqual(0f, LeituraDosKits.EscalaPorRestante(-1f, 5f));
            Assert.AreEqual(0f, LeituraDosKits.EscalaPorRestante(float.NaN, 5f), "NaN nao desenha");
            Assert.AreEqual(1f, LeituraDosKits.EscalaPorRestante(5f, 5f), 1e-5f, "nasce inteira");
            Assert.AreEqual(1f, LeituraDosKits.EscalaPorRestante(4f, 5f), 1e-5f, "inteira ate' os ultimos 25%");
            Assert.GreaterOrEqual(LeituraDosKits.EscalaPorRestante(0.001f, 5f), LeituraDosKits.Piso, "vivo nunca some: o visual nao mente a regra");
            float antes = 0f;
            for (float r = 0f; r <= 5f; r += 0.05f)
            {
                float e = LeituraDosKits.EscalaPorRestante(r, 5f);
                Assert.GreaterOrEqual(e, antes - 1e-6f, "sumico monotono (r=" + r + ")");
                antes = e;
            }
        }

        [Test]
        public void TetoDeFogo_200Queimando_SoOsMaisPertoGanhamEmissor()
        {
            var vis = new List<CelulaVisivel>();
            for (int i = 0; i < 200; i++) vis.Add(new CelulaVisivel { Idx = i, Centro = new Vector3(i * 3f, 0f, 0f), Estado = EstadoCelula.Queimando });
            for (int i = 0; i < 50; i++) vis.Add(new CelulaVisivel { Idx = 1000 + i, Centro = new Vector3(300f, 0f, i), Estado = EstadoCelula.Carvao });   // colado no olho, mas nao queima
            var saida = new List<CelulaVisivel>();
            Vector3 olho = new Vector3(300f, 20f, 0f);   // no meio da fila de fogo
            int n = LeituraDoTerreno.EscolherEmissores(vis, EstadoCelula.Queimando, olho, LeituraDoTerreno.TetoDeFogo, saida);
            Assert.AreEqual(LeituraDoTerreno.TetoDeFogo, n, "200 queimando: so' o TETO ganha particula");
            Assert.AreEqual(n, saida.Count);
            var ids = new HashSet<int>();
            float pior = 0f;
            foreach (CelulaVisivel c in saida)
            {
                Assert.AreEqual(EstadoCelula.Queimando, c.Estado, "carvao nao pega emissor de fogo");
                ids.Add(c.Idx);
                pior = Mathf.Max(pior, Mathf.Abs(c.Centro.x - olho.x));
            }
            Assert.AreEqual(n, ids.Count, "sem celula repetida");
            for (int i = 0; i < 200; i++)
                if (!ids.Contains(i)) Assert.GreaterOrEqual(Mathf.Abs(vis[i].Centro.x - olho.x), pior, "quem ficou de fora esta' mais longe");
            Assert.AreEqual(10, LeituraDoTerreno.EscolherEmissores(vis.GetRange(0, 10), EstadoCelula.Queimando, olho, LeituraDoTerreno.TetoDeFogo, saida),
                "abaixo do teto, todas");
            Assert.AreEqual(0, LeituraDoTerreno.EscolherEmissores(null, EstadoCelula.Queimando, olho, LeituraDoTerreno.TetoDeFogo, saida), "sem terreno, nenhuma");
        }

        [Test]
        public void Fio_ComecaEmA_TerminaEmB_TremeSemSairDaLinha_EDeterministico()
        {
            Vector3 a = new Vector3(1f, 1f, 2f), b = new Vector3(7f, 1.5f, -3f);
            Vector3[] p = LeituraDosKits.PontosDoFio(a, b, 12, 3.7f);
            Assert.AreEqual(12, p.Length);
            Assert.AreEqual(a, p[0], "a ancora A e' onde a logica mede");
            Assert.AreEqual(b, p[11], "a ancora B e' onde a logica mede");
            Vector3[] q = LeituraDosKits.PontosDoFio(a, b, 12, 3.7f);
            for (int i = 0; i < 12; i++) Assert.AreEqual(p[i], q[i], "mesmo tempo = mesmo desenho");
            Vector3[] r = LeituraDosKits.PontosDoFio(a, b, 12, 4.2f);
            bool treme = false;
            for (int i = 1; i < 11; i++)
            {
                if ((p[i] - r[i]).sqrMagnitude > 1e-6f) treme = true;
                Assert.LessOrEqual(KitRunner.DistSegmento(p[i], a, b), LeituraDosKits.TremorFio * 1.5f, "treme sem fugir da linha que a logica mede");
            }
            Assert.IsTrue(treme, "o fio TREME com o tempo");
            Vector3[] dois = LeituraDosKits.PontosDoFio(a, b, 1, 0f);
            Assert.AreEqual(2, dois.Length, "no minimo as duas ancoras");
            Assert.AreEqual(a, dois[0]); Assert.AreEqual(b, dois[1]);
            Assert.AreSame(p, LeituraDosKits.PontosDoFio(a, b, 12, 5f, p), "reaproveita o buffer (zero lixo por frame)");
        }

        [Test]
        public void Muro_BloqueiaTiro_ParaNoMuro_NaoAcertaQuemEstaAtras_EPerdeHp()
        {
            // ilha de mentira 20x20 (celula 3 m), toda chao. Na fila z=10: atirador (8) | muro (10) | alvo (12)
            var t = new TerrenoReativo(new FakeRelevo(), 60f, 7, (cx, cz) => TipoCelula.Chao);
            var m = new Partida(new FakeRelevo()) { Terreno = t };
            m.Iniciar(9, 1, false);
            try
            {
                Vector3 pe = t.Centro(t.Idx(8, 10)), muro = t.Centro(t.Idx(10, 10)), atras = t.Centro(t.Idx(12, 10));
                var atirador = new FakeEntidade("player", pe, true);
                var alvo = new FakeEntidade("b1", atras);
                m.Registrar(atirador, null);
                m.Registrar(alvo, null);
                t.Reagir(Elemento.Terra, muro, false);
                int celula = t.CelulaEm(muro);
                Assert.IsTrue(t.BloqueiaTiro(muro), "pre-condicao: o muro subiu");
                var impactos = new List<Vector3>();
                Bus.TerrainHit += (el, pos, forte) => impactos.Add(pos);
                float vida0 = alvo.Vital.Hp + alvo.Vital.Escudo;

                Atirar(m, atirador, Pawn.ALTURA_MAO);
                Assert.AreEqual(0, m.Projeteis.Count, "o tiro morreu");
                Assert.AreEqual(vida0, alvo.Vital.Hp + alvo.Vital.Escudo, 1e-4f, "quem esta' ATRAS do muro nao apanha");
                Assert.AreEqual(1, impactos.Count, "um impacto so'");
                Assert.AreEqual(celula, t.CelulaEm(impactos[0]), "o tiro PAROU na celula do muro");
                Balance.PerfilElemento raio = Balance.Perfil(Elemento.Raio);
                Assert.AreEqual(Balance.Terrain.WallHp - raio.Dmg * raio.Estrutura, HpDoMuro(t, celula), 1e-3f, "o muro apanhou (dano x Estrutura)");

                // cobertura tem ALTURA: por cima do muro o tiro segue voando
                Projetil alto = Projetil.Lancar(atirador, pe + new Vector3(0.9f, Balance.Terrain.WallHeight + 1f, 0f), Vector3.right, Elemento.Raio);
                m.Registrar(alto);
                for (int i = 0; i < 60 && alto.Vivo && alto.Pos.x < muro.x + 3f; i++) m.Tick(DT);
                Assert.IsTrue(alto.Vivo, "o tiro alto passou por cima");
                Assert.Greater(alto.Pos.x, muro.x + 1.5f, "atravessou a coluna do muro");
                m.Projeteis.Clear();

                // controle (o teste nao e' vacuo): sem o muro, o MESMO tiro acerta quem estava atras
                t.Reset();
                Atirar(m, atirador, Pawn.ALTURA_MAO);
                Assert.Less(alvo.Vital.Hp + alvo.Vital.Escudo, vida0, "sem muro, o tiro acerta o alvo");
            }
            finally { m.Encerrar(); t.Desligar(); }
        }

        private static void Atirar(Partida m, IEntidade de, float altura)
        {
            Projetil p = Projetil.Lancar(de, de.Pos + Vector3.up * altura + Vector3.right * 0.9f, Vector3.right, Elemento.Raio);
            m.Registrar(p);
            for (int i = 0; i < 120 && m.Projeteis.Count > 0; i++) m.Tick(DT);
        }

        private static float HpDoMuro(TerrenoReativo t, int celula)
        {
            foreach (CelulaVisivel v in t.Visiveis) if (v.Idx == celula) return v.MuroHp;
            return 0f;
        }
    }
}
