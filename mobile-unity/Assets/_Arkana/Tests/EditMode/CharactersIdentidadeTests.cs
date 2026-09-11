using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Characters;

namespace Arkana.Tests
{
    /// <summary>Os 20 magos: altura da ficha, tres cores validas, nome do Kits, silhueta; desconhecido cai no generico.</summary>
    public class CharactersIdentidadeTests
    {
        [Test]
        public void Os20SlugsDoKitsTemIdentidade()
        {
            Assert.AreEqual(20, Kits.Slugs.Length);
            foreach (string slug in Kits.Slugs)
            {
                IdentidadeMago id = IdentidadeMago.De(slug);
                Assert.AreEqual(slug, id.Slug);
                Assert.AreEqual(Kits.De(slug).Nome, id.Nome, slug);
                Assert.IsFalse(string.IsNullOrEmpty(id.Nome), slug);
            }
        }

        [Test]
        public void AlturaVemDaFichaEmMetros()
        {
            // As fichas vao de Pip (0,60 m, voa) e Fizz (0,95 m) a Basalto (2,30 m): o porte e' leitura de raca.
            foreach (string slug in Kits.Slugs)
            {
                float h = IdentidadeMago.De(slug).AlturaM;
                Assert.IsTrue(h >= 0.5f && h <= 2.5f, slug + " altura " + h);
            }
            Assert.AreEqual(1.78f, IdentidadeMago.De("01-pyra").AlturaM, 1e-4f);
            Assert.AreEqual(1.40f, IdentidadeMago.De("13-brok").AlturaM, 1e-4f);
            Assert.AreEqual(2.30f, IdentidadeMago.De("18-basalto").AlturaM, 1e-4f);
            Assert.AreEqual(0.60f, IdentidadeMago.De("20-pip").AlturaM, 1e-4f);
            Assert.Greater(IdentidadeMago.De("20-pip").FlutuaM, 1f, "Pip nunca pousa");
            Assert.AreEqual(0f, IdentidadeMago.De("13-brok").FlutuaM);
        }

        [Test]
        public void TresCoresValidasPorMagoEPrimariaDiferenteDaMarca()
        {
            foreach (string slug in Kits.Slugs)
            {
                IdentidadeMago id = IdentidadeMago.De(slug);
                Assert.IsTrue(IdentidadeMago.HexValido(id.CorPrimaria), slug + " primaria");
                Assert.IsTrue(IdentidadeMago.HexValido(id.CorSecundaria), slug + " secundaria");
                Assert.IsTrue(IdentidadeMago.HexValido(id.CorMarca), slug + " marca");
                Assert.AreNotEqual(Color.magenta, IdentidadeMago.Cor(id.CorPrimaria), slug);
                Assert.AreNotEqual(id.CorPrimaria.ToUpperInvariant(), id.CorMarca.ToUpperInvariant(), slug + ": a marca tem que se destacar do manto");
            }
        }

        [Test]
        public void CorHexConverteEInvalidoGrita()
        {
            Color c = IdentidadeMago.Cor("#FF5A2A");
            Assert.AreEqual(1f, c.r, 1e-3f);
            Assert.AreEqual(0x5A / 255f, c.g, 1e-3f);
            Assert.AreEqual(0x2A / 255f, c.b, 1e-3f);
            Assert.AreEqual(Color.magenta, IdentidadeMago.Cor("zzz"));
            Assert.AreEqual(Color.magenta, IdentidadeMago.Cor(null));
            Assert.IsFalse(IdentidadeMago.HexValido("FF5A2"));
        }

        [Test]
        public void SlugDesconhecidoCaiNoGenericoSemLancar()
        {
            IdentidadeMago g = IdentidadeMago.De("99-ninguem");
            Assert.AreEqual(IdentidadeMago.AlturaRef, g.AlturaM);
            Assert.IsTrue(IdentidadeMago.HexValido(g.CorPrimaria));
            Assert.DoesNotThrow(() => IdentidadeMago.De(null));
            Assert.AreEqual(20, IdentidadeMago.Todas().Length);
        }

        [Test]
        public void SilhuetaELarguraLeemARaca()
        {
            Assert.AreEqual(Silhueta.Robusto, IdentidadeMago.De("13-brok").Silhueta, "largo como uma porta");
            Assert.AreEqual(Silhueta.Flutuante, IdentidadeMago.De("20-pip").Silhueta);
            Assert.AreEqual(Silhueta.Alto, IdentidadeMago.De("18-basalto").Silhueta);
            Assert.Greater(IdentidadeMago.Largura(Silhueta.Robusto), IdentidadeMago.Largura(Silhueta.Esguio));
            HashSet<Silhueta> vistas = new HashSet<Silhueta>();
            foreach (string slug in Kits.Slugs) vistas.Add(IdentidadeMago.De(slug).Silhueta);
            Assert.GreaterOrEqual(vistas.Count, 4, "o elenco nao e' um clone so'");
        }

        [Test]
        public void ElementoAfimCasaComAFicha()
        {
            Assert.AreEqual(Elemento.Fogo, IdentidadeMago.De("01-pyra").Elemento);
            Assert.AreEqual(Elemento.Agua, IdentidadeMago.De("15-maris").Elemento);
            Assert.AreEqual(Elemento.Raio, IdentidadeMago.De("10-tessa").Elemento);
            Assert.AreEqual(Elemento.Terra, IdentidadeMago.De("18-basalto").Elemento);
        }
    }
}
