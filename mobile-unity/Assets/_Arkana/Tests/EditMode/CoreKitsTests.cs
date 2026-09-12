using System.Collections.Generic;
using NUnit.Framework;
using Arkana.Core;

namespace Arkana.Tests
{
    /// <summary>As leis de gameplay/selftest_kits.gd (_test_registro): os 20, sem mana, telegrafia 1-4 s, taticas 5-10 s.</summary>
    public class CoreKitsTests
    {
        /// <summary>Os implementados, lidos da FICHA: a lista cresce com os grupos dos 17 (12/09) e as leis valem para todos.</summary>
        private static IEnumerable<string> Implementados()
        {
            foreach (string s in Kits.Slugs) if (Kits.De(s).Implementado) yield return s;
        }

        [SetUp]
        public void SetUp() { Bus.Reset(); }

        [Test]
        public void Vinte_Slugs_NaOrdem()
        {
            Assert.AreEqual(20, Kits.Slugs.Length);
            Assert.AreEqual(20, Kits.Magos.Count);
            for (int i = 0; i < 20; i++)
            {
                string prefixo = (i + 1).ToString("00") + "-";
                Assert.IsTrue(Kits.Slugs[i].StartsWith(prefixo), Kits.Slugs[i] + " deveria comecar com " + prefixo);
                Assert.IsTrue(Kits.Magos.ContainsKey(Kits.Slugs[i]), "todo slug tem entrada em Magos");
                Assert.AreEqual(Kits.Slugs[i], Kits.De(Kits.Slugs[i]).Slug);
            }
            Assert.AreEqual("01-pyra", Kits.Slugs[0]);
            Assert.AreEqual("20-pip", Kits.Slugs[19]);
        }

        [Test]
        public void Implementados_SaoOsRegistrados_OsOutrosInertes()
        {
            foreach (string s in new[] { "01-pyra", "03-veu", "10-tessa" }) Assert.IsTrue(Kits.De(s).Implementado, s);
            foreach (string s in Kits.Slugs)
                Assert.AreEqual(Arkana.Gameplay.KitRunner.Registro.ContainsKey(s), Kits.De(s).Implementado, "ficha e registro andam juntos: " + s);
            foreach (string s in Kits.Slugs)
            {
                Kits.KitDef k = Kits.De(s);
                if (k.Implementado) continue;   // ainda sem grupo: o padrao inerte
                Assert.AreEqual(Kits.PadraoTaticaCd, k.TaticaCd, s);
                Assert.AreEqual(Kits.PadraoSupremaCarga, k.SupremaCarga, s);
                Assert.AreEqual(Kits.PadraoTelegrafia, k.Telegrafia, s);
            }
            Kits.KitDef nada = Kits.De("99-ninguem");
            Assert.IsFalse(nada.Implementado);
            Assert.IsNotNull(nada.Passiva, "mago nao implementado responde a ficha inteira");
            Assert.IsNotNull(nada.Tatica);
            Assert.IsNotNull(nada.Suprema);
        }

        [Test]
        public void Slug_Desconhecido_CaiNoPadrao()
        {
            Kits.KitDef k = Kits.De("nao-existe");
            Assert.IsNotNull(k);
            Assert.IsFalse(k.Implementado);
            Assert.AreEqual(Kits.PadraoTaticaCd, k.TaticaCd);
            Assert.IsNotNull(Kits.De(null));
        }

        [Test]
        public void Lei_4_2_NenhumKitTemCustoDeMana()
        {
            List<string> suja = new List<string>();
            foreach (string s in Kits.Slugs)
            {
                Kits.KitDef k = Kits.De(s);
                Dictionary<string, float>[] blocos = { k.Passiva, k.Tatica, k.Suprema };
                foreach (Dictionary<string, float> bloco in blocos)
                    foreach (string campo in bloco.Keys)
                    {
                        string c = campo.ToLowerInvariant();
                        if (c.Contains("mana") || c.Contains("custo")) suja.Add(s + "." + campo);
                    }
            }
            Assert.IsEmpty(suja, "§4.2: nenhum kit tem custo de MANA (so' cooldown): " + string.Join(", ", suja));
        }

        [Test]
        public void Lei_4_3_TelegrafiaEntre1e4s_NosImplementados()
        {
            Assert.AreEqual(1f, Kits.TelegrafiaMin);
            Assert.AreEqual(4f, Kits.TelegrafiaMax);
            foreach (string s in Implementados())
            {
                float t = Kits.De(s).Telegrafia;
                Assert.IsTrue(t >= Kits.TelegrafiaMin && t <= Kits.TelegrafiaMax, s + " avisa fora da faixa: " + t);
            }
        }

        [Test]
        public void Taticas_DosImplementados_Entre5e10s()
        {
            foreach (string s in Implementados())
            {
                float cd = Kits.De(s).TaticaCd;
                Assert.IsTrue(cd >= 5f && cd <= 10f, s + " tatica fora da faixa 5-10 s: " + cd);
            }
        }

        [Test]
        public void Numeros_DeKitsGd_Batem()
        {
            Assert.AreEqual(0.0015f, Kits.CargaPorDano);
            Kits.KitDef pyra = Kits.De("01-pyra");
            Assert.AreEqual(9f, pyra.TaticaCd);
            Assert.AreEqual(50f, pyra.SupremaCarga);
            Assert.AreEqual(8f, pyra.Tatica["comprimento"]);
            Assert.AreEqual(0.85f, pyra.Suprema["esfria_vel"]);
            Kits.KitDef veu = Kits.De("03-veu");
            Assert.AreEqual(6f, veu.TaticaCd);
            Assert.AreEqual(1.5f, veu.Tatica["duracao"]);
            Kits.KitDef tessa = Kits.De("10-tessa");
            Assert.AreEqual(6f, tessa.Tatica["max_fios"]);
            Assert.AreEqual(4f, tessa.Passiva["escudo_regen"]);
        }
    }
}
