using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Terrain;

namespace Arkana.Tests
{
    /// <summary>
    /// FULGURITO (GDD §16.4; ata docs/conselho/2026-11-19-fulgurito.md; ADR docs/architecture/adr-0001-fulgurito.md).
    /// Ilha de mentira de 20x20 celulas (3 m, lado 60 m): AREIA na metade de cima (cz >= 10), chao comum embaixo.
    /// Cada teste foi visto VERMELHO contra a implementacao com defeitos plantados (evidencia em production/qa/evidence/).
    /// </summary>
    public class TerrainFulguritoTests
    {
        private const float LADO = 60f;

        // areia = z >= 0 (metade de cima da grade); o centro da celula decide
        private static Fulgurito Novo() => new Fulgurito(LADO, (x, z) => z >= 0f);

        private static Vector3 Pos(Fulgurito f, int cx, int cz) => f.Centro(cz * f.N + cx);

        [SetUp]
        public void SetUp() { Bus.Reset(); }

        [TearDown]
        public void TearDown() { Bus.Reset(); }

        [Test]
        public void RaioNaAreia_FundeVidroQueTapaAVisao()
        {
            Fulgurito f = Novo();
            Vector3 p = Pos(f, 5, 15);
            Bus.EmitTerrainHit(Elemento.Raio, p, false);
            Assert.AreEqual(1, f.Ativos, "raio na areia funde UM vidro");
            Assert.IsTrue(f.TemVidro(p));
            Assert.IsTrue(f.BloqueiaVisao(p), "o vidro e' cobertura: tapa a visao");
            Assert.AreEqual(1, f.Visiveis.Count, "a cena recebe o vidro para desenhar");
        }

        [Test]
        public void RaioForaDaAreia_NaoFundeNada()
        {
            Fulgurito f = Novo();
            Bus.EmitTerrainHit(Elemento.Raio, Pos(f, 5, 3), false);
            Bus.EmitTerrainHit(Elemento.Raio, new Vector3(999f, 0f, 999f), true);
            Assert.AreEqual(0, f.Ativos, "chao comum e fora do mapa nao viram vidro");
        }

        [Test]
        public void OutroElementoNaAreia_NaoFundeVidro()
        {
            Fulgurito f = Novo();
            foreach (Elemento e in new[] { Elemento.Fogo, Elemento.Agua, Elemento.Terra, Elemento.Vento })
                Bus.EmitTerrainHit(e, Pos(f, 5, 15), true);
            Assert.AreEqual(0, f.Ativos, "so' o RAIO funde a areia (a ciencia do fulgurito)");
        }

        [Test]
        public void QualquerImpactoNoVidro_EstilhacaComUmTiro()
        {
            Fulgurito f = Novo();
            Vector3 p = Pos(f, 5, 15);
            Bus.EmitTerrainHit(Elemento.Raio, p, false);
            Bus.EmitTerrainHit(Elemento.Fogo, p, false);
            Assert.IsFalse(f.TemVidro(p), "1 tiro destroi");
            Assert.IsFalse(f.BloqueiaVisao(p), "estilhacado nao tapa mais nada");
            Assert.AreEqual(1, f.Estilhacados);

            Bus.EmitTerrainHit(Elemento.Raio, p, false);   // funde de novo
            f.Tick(Balance.Terrain.GlassNascer + 0.01f);    // ja' nasceu (ciclo 2: vidro NASCENDO nao quebra com Raio)
            Bus.EmitTerrainHit(Elemento.Raio, p, false);   // o raio TAMBEM estilhaca — e nao refunde no mesmo impacto
            Assert.IsFalse(f.TemVidro(p), "raio no vidro ja' nascido estilhaca em vez de renovar");
        }

        /// <summary>Conselho Modo B ciclo 1, LP nº 1: a suprema do Fizz emite Raio FORTE a cada 3 m (1 celula) no MESMO quadro.
        /// O 2º ponto caia no vidro recem-fundido e o estilhacava (vidro/buraco alternado). Agora o vidro NASCENDO ignora Raio
        /// e o disco do ponto seguinte continua fundindo o que falta: uma faixa continua.</summary>
        [Test]
        public void RaioForteEmSequencia_NaoEstilhacaOProprioVidro()
        {
            Fulgurito f = Novo();
            for (int i = 0; i < 3; i++) Bus.EmitTerrainHit(Elemento.Raio, Pos(f, 4 + i, 15), true);   // 3 m entre si, mesmo quadro
            Assert.AreEqual(0, f.Estilhacados, "a propria rajada nao quebra o vidro que acabou de fundir");
            Assert.AreEqual(15, f.Ativos, "faixa continua: disco 3x3 + 3 + 3 colunas novas");
            for (int cx = 3; cx <= 7; cx++)
                for (int cz = 14; cz <= 16; cz++)
                    Assert.IsTrue(f.TemVidro(Pos(f, cx, cz)), "buraco na faixa em (" + cx + "," + cz + ")");
        }

        [Test]
        public void VidroNascendo_OutroElementoAindaEstilhaca()
        {
            Fulgurito f = Novo();
            Vector3 p = Pos(f, 5, 15);
            Bus.EmitTerrainHit(Elemento.Raio, p, false);
            Bus.EmitTerrainHit(Elemento.Fogo, p, false);   // mesmo quadro: o vidro ainda sobe, mas nao e' escudo
            Assert.IsFalse(f.TemVidro(p), "so' o Raio poupa o vidro nascendo; qualquer outro impacto quebra (1 tiro destroi)");
            Assert.AreEqual(1, f.Estilhacados);
        }

        [Test]
        public void VidroSemImpacto_SomeDepoisDeGlassDuration()
        {
            Fulgurito f = Novo();
            Vector3 p = Pos(f, 5, 15);
            Bus.EmitTerrainHit(Elemento.Raio, p, false);
            f.Tick(Balance.Terrain.GlassDuration - 0.1f);
            Assert.IsTrue(f.TemVidro(p), "ainda de pe' antes do prazo");
            f.Tick(0.2f);
            Assert.IsFalse(f.TemVidro(p), "o vidro expira sozinho");
            Assert.AreEqual(0, f.Visiveis.Count);
            f.Tick(float.NaN);   // relogio quebrado nao ressuscita nem quebra nada
            Assert.AreEqual(0, f.Ativos);
        }

        [Test]
        public void MuitosRaios_NuncaPassamDoTetoGlassMax()
        {
            Fulgurito f = Novo();
            for (int cz = 10; cz < 20; cz++)
                for (int cx = 0; cx < 20; cx++)
                    Bus.EmitTerrainHit(Elemento.Raio, Pos(f, cx, cz), false);
            Assert.AreEqual(Balance.Terrain.GlassMax, f.Ativos, "200 celulas de areia, mas o teto de orcamento manda");
        }

        [Test]
        public void RaioForte_FundeDisco3x3SoNaAreia()
        {
            Fulgurito f = Novo();
            Bus.EmitTerrainHit(Elemento.Raio, Pos(f, 5, 15), true);
            Assert.AreEqual(9, f.Ativos, "forte = disco RaioForte (3x3) inteiro de areia");

            Bus.Reset();
            Fulgurito g = Novo();
            Bus.EmitTerrainHit(Elemento.Raio, Pos(g, 5, 10), true);   // borda: a linha cz=9 e' chao comum
            Assert.AreEqual(6, g.Ativos, "a parte do disco fora da areia nao vira vidro");
        }

        [Test]
        public void DesligarEReset_SoltamOBusEZeramOEstado()
        {
            Fulgurito f = Novo();
            Bus.EmitTerrainHit(Elemento.Raio, Pos(f, 5, 15), false);
            f.Reset();
            Assert.AreEqual(0, f.Ativos, "Reset limpa os vidros");
            Assert.AreEqual(0, f.Fundidos);
            f.Desligar();
            Bus.EmitTerrainHit(Elemento.Raio, Pos(f, 6, 15), false);
            Assert.AreEqual(0, f.Ativos, "desligado nao ouve mais o Bus");
        }
    }
}
