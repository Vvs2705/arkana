using System.Collections.Generic;
using NUnit.Framework;
using Arkana.Audio;

namespace Arkana.Tests
{
    /// <summary>Sintese: 48 timbres nomeados, todos finitos em [-1,1], desconhecido = null; mixagem: throttle, teto, prioridade.</summary>
    public class AudioSinteseTests
    {
        [Test]
        public void PaletaTem48TimbresNomeados()
        {
            Assert.AreEqual(48, Sintese.Nomes.Length);
            Assert.AreEqual(48, new HashSet<string>(Sintese.Nomes).Count, "nomes unicos");
        }

        [Test]
        public void TodoTimbreEFinitoEmMenosUmUm()
        {
            foreach (var nome in Sintese.Nomes)
            {
                var b = Sintese.Gerar(nome);
                Assert.IsNotNull(b, nome);
                Assert.Greater(b.Length, 0, nome);
                float pico = 0f;
                foreach (float v in b)
                {
                    Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v), nome + " tem amostra nao finita");
                    Assert.IsTrue(v >= -1f && v <= 1f, nome + " fora de [-1,1]");
                    if (System.Math.Abs(v) > pico) pico = System.Math.Abs(v);
                }
                Assert.Greater(pico, 0.01f, nome + " e' silencio");
            }
        }

        [Test]
        public void TimbreDesconhecidoESilencio()
        {
            Assert.IsNull(Sintese.Gerar("sabor_novo_de_r20"));
            Assert.IsNull(Sintese.Gerar(""));
            Assert.IsNull(Sintese.Gerar(null));
            Assert.IsNull(Sfx.TimbreDoTerreno("sabor_novo"), "kind desconhecido: silencio, nunca timbre errado");
            Assert.AreEqual("terrain_fire", Sfx.TimbreDoTerreno("burn"));
            Assert.AreEqual("terrain_zap", Sfx.TimbreDoTerreno("electric"));
        }

        [Test]
        public void CincoDisparosSaoPcmDistintos()
        {
            var shots = new[] { "shot_fire", "shot_water", "shot_lightning", "shot_earth", "shot_wind" };
            for (int i = 0; i < shots.Length; i++)
                for (int j = i + 1; j < shots.Length; j++)
                    Assert.IsFalse(Iguais(Sintese.Gerar(shots[i]), Sintese.Gerar(shots[j])), shots[i] + " == " + shots[j]);
        }

        [Test]
        public void SinteseEDeterminista()
        {
            Assert.IsTrue(Iguais(Sintese.Gerar("terrain_fire"), Sintese.Gerar("terrain_fire")), "mesmo timbre em todo aparelho");
            Assert.IsTrue(Iguais(Sintese.Gerar("st_burn"), Sintese.Gerar("st_queimar")), "st_burn e' o apelido de st_queimar");
        }

        [Test]
        public void LoopsSaoOsQuatroDeEvento()
        {
            Assert.IsTrue(Sintese.EhLoop("ambient_wind") && Sintese.EhLoop("bau_queda") && Sintese.EhLoop("bau_canal") && Sintese.EhLoop("telegrafia_rugido"));
            Assert.IsFalse(Sintese.EhLoop("hit"));
        }

        static bool Iguais(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        // ---------- mixagem ----------
        [Test]
        public void ThrottleBarraRepeticaoNaJanela()
        {
            var v = new Vozes(4);
            Assert.GreaterOrEqual(v.Tocar("hit", 1000, Vozes.P_NORMAL, 500, 90), 0);
            Assert.AreEqual(-1, v.Tocar("hit", 1200, Vozes.P_NORMAL, 500, 90), "2o disparo da mesma key na janela NAO soa");
            Assert.GreaterOrEqual(v.Tocar("hit", 1600, Vozes.P_NORMAL, 500, 90), 0);
        }

        [Test]
        public void PoolCheioRecusaOMenosImportanteEAceitaOCritico()
        {
            var v = new Vozes(3);
            for (int i = 0; i < 3; i++) Assert.GreaterOrEqual(v.Tocar("terrain_fire", 0, Vozes.P_NORMAL, 0, 1000), 0);
            Assert.AreEqual(-1, v.Tocar("terrain_ice", 10, Vozes.P_BAIXA, 0, 1000), "pool cheio: som menos importante e' recusado");
            int voz = v.Tocar("telegrafia", 10, Vozes.P_CRITICA, 0, 1000);
            Assert.GreaterOrEqual(voz, 0, "a TELEGRAFIA entra assim mesmo");
            for (int i = 0; i < 3; i++) v.Tocar("shot_fire", 20, Vozes.P_NORMAL, 0, 300);
            Assert.AreEqual(Vozes.P_CRITICA, v.PrioDe(voz), "e nao e' roubada por um tiro que vem depois");
            Assert.GreaterOrEqual(v.Tocar("hit", 2000, Vozes.P_BAIXA, 0, 10), 0, "vozes vencidas voltam a servir");
        }

        [Test]
        public void DistanciaAtenuaEmDbELongeDemaisNemToca()
        {
            Assert.AreEqual(0f, Vozes.DbDistancia(4f).Value, 1e-4f);
            float? meio = Vozes.DbDistancia(60f);
            Assert.IsTrue(meio.HasValue && meio.Value < -5f);
            Assert.AreEqual(-Vozes.QUEDA_DB, Vozes.DbDistancia(Vozes.SURDO_M).Value, 1e-3f);
            Assert.IsFalse(Vozes.DbDistancia(400f).HasValue, "fora do alcance: nem ocupa voz");
            Assert.AreEqual(1f, Vozes.DbParaGanho(0f), 1e-4f);
            Assert.AreEqual(0.5f, Vozes.DbParaGanho(-6.0206f), 1e-3f);
        }
    }
}
