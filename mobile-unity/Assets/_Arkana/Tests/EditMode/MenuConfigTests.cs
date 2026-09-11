using NUnit.Framework;
using UnityEngine;
using Arkana.Menu;

namespace Arkana.Tests
{
    /// <summary>Config: defaults, tipo persistido (o store guarda o TIPO, nunca valor duplicado), daltonismo; Selo desenhado.</summary>
    public class MenuConfigTests
    {
        [Test]
        public void DefaultsDoGdd12()
        {
            var c = new ConfigLogica();
            Assert.IsTrue(c.Bool(ConfigLogica.K_TELA_CHEIA));
            Assert.AreEqual(1, c.Int(ConfigLogica.K_QUALIDADE));
            Assert.AreEqual(1, c.Int(ConfigLogica.K_FPS_LIMITE));
            Assert.IsFalse(c.Bool(ConfigLogica.K_CONTADOR_FPS));
            Assert.AreEqual(80f, c.Float(ConfigLogica.K_VOL_GERAL), 1e-4f);
            Assert.AreEqual(3f, c.Float(ConfigLogica.K_SENSIBILIDADE), 1e-4f);
            Assert.AreEqual(0, c.Int(ConfigLogica.K_DALTONISMO));
            Assert.IsTrue(c.Bool(ConfigLogica.K_NUMEROS_DANO));
            Assert.AreEqual(0, c.Int(ConfigLogica.K_LAYOUT));
            Assert.AreEqual(1f, c.Float(ConfigLogica.K_ESCALA_BOTOES), 1e-4f);
        }

        [Test]
        public void TipoErradoCaiNoPadrao()
        {
            var c = new ConfigLogica();
            Assert.IsFalse(c.Set(ConfigLogica.K_VOL_GERAL, "alto demais"));
            Assert.AreEqual(80f, c.Float(ConfigLogica.K_VOL_GERAL), 1e-4f);
            Assert.IsFalse(c.Set(ConfigLogica.K_VSYNC, 1));
            Assert.IsTrue(c.Bool(ConfigLogica.K_VSYNC));
            Assert.IsFalse(c.Set("inexistente.x", 1), "chave fantasma nao entra");
            Assert.IsTrue(c.Set(ConfigLogica.K_VOL_GERAL, 42));
            Assert.AreEqual(42f, c.Float(ConfigLogica.K_VOL_GERAL), 1e-4f, "int em chave float e' promovido");
            Assert.IsTrue(c.Set(ConfigLogica.K_DALTONISMO, 3));
            Assert.AreEqual(3, c.Int(ConfigLogica.K_DALTONISMO));
        }

        [Test]
        public void PersistenciaGuardaOTipoNuncaOValorDuplicado()
        {
            var c = new ConfigLogica();
            c.Set(ConfigLogica.K_VOL_GERAL, 42f);
            c.Set(ConfigLogica.K_NUMEROS_DANO, false);
            c.Set(ConfigLogica.K_DALTONISMO, 2);
            var p = new PrefsMemoria();
            c.Salvar(p);
            Assert.AreEqual(1, p.Salvos);
            Assert.IsTrue(p.Floats.ContainsKey(ConfigLogica.K_VOL_GERAL), "float vai como float");
            Assert.IsFalse(p.Ints.ContainsKey(ConfigLogica.K_VOL_GERAL), "e nao em dobro");
            Assert.AreEqual(0, p.LerInt(ConfigLogica.K_NUMEROS_DANO, 9), "bool vai como int 0/1");
            Assert.AreEqual(2, p.LerInt(ConfigLogica.K_DALTONISMO, 9));
            foreach (var k in ConfigLogica.Padrao.Keys)
                Assert.IsFalse(p.Ints.ContainsKey(k) && p.Floats.ContainsKey(k), k + " gravada em dois tipos");

            var lido = new ConfigLogica();
            lido.Carregar(p);
            Assert.AreEqual(42f, lido.Float(ConfigLogica.K_VOL_GERAL), 1e-4f);
            Assert.IsFalse(lido.Bool(ConfigLogica.K_NUMEROS_DANO));
            Assert.AreEqual(2, lido.Int(ConfigLogica.K_DALTONISMO));
            Assert.IsTrue(lido.Bool(ConfigLogica.K_VSYNC), "o que nao foi mexido continua no padrao");

            // store com o TIPO errado para a chave (arquivo de versao velha): cai no padrao
            var sujo = new PrefsMemoria();
            sujo.GravarInt(ConfigLogica.K_VOL_GERAL, 7);
            var c2 = new ConfigLogica();
            c2.Carregar(sujo);
            Assert.AreEqual(80f, c2.Float(ConfigLogica.K_VOL_GERAL), 1e-4f);
        }

        [Test]
        public void RestaurarPadraoPorSecao()
        {
            var c = new ConfigLogica();
            c.Set(ConfigLogica.K_VOL_GERAL, 7f);
            c.Set(ConfigLogica.K_DALTONISMO, 1);
            c.Restaurar("audio");
            Assert.AreEqual(80f, c.Float(ConfigLogica.K_VOL_GERAL), 1e-4f);
            Assert.AreEqual(1, c.Int(ConfigLogica.K_DALTONISMO), "restaurar audio nao mexe em jogo");
            Assert.AreEqual("jogo", ConfigLogica.SecaoDe(ConfigLogica.K_DALTONISMO));
            Assert.AreEqual(4, ConfigLogica.Secoes.Length);
        }

        [Test]
        public void DaltonismoTemOs4ModosDoGdd()
        {
            Assert.AreEqual(4, Arkana.UI.FiltroDaltonismo.Nomes.Length);
            Assert.AreEqual("Nenhum", Arkana.UI.FiltroDaltonismo.Nomes[0]);
            Assert.AreEqual("Tritanopia", Arkana.UI.FiltroDaltonismo.Nomes[3]);
        }

        [Test]
        public void SeloDesenhaPentagonoEGemas()
        {
            int lado = 96;
            var px = Selo.Pixels(lado);
            Assert.AreEqual(lado * lado, px.Length);
            int opacos = 0; int ouro = 0;
            var ouroRef = (Color32)Estilo.Ouro;
            foreach (var p in px)
            {
                if (p.a > 0) opacos++;
                if (p.a > 0 && Mathf.Abs(p.r - ouroRef.r) < 40 && Mathf.Abs(p.g - ouroRef.g) < 40 && Mathf.Abs(p.b - ouroRef.b) < 60) ouro++;
            }
            Assert.Greater(opacos, lado * lado / 40, "o selo nao e' uma textura vazia");
            Assert.Less(opacos, lado * lado / 2, "e nao e' um quadrado cheio");
            Assert.Greater(ouro, 10, "tem o dourado do A e do contorno");
            // determinista: icone e splash usam o MESMO desenho
            var px2 = Selo.Pixels(lado);
            for (int i = 0; i < px.Length; i += 97) Assert.AreEqual(px[i].a, px2[i].a);
        }

        [Test]
        public void CorDoMagoEDeterminista()
        {
            Assert.AreEqual(SelecaoPersonagem.CorDoMago("01-pyra"), SelecaoPersonagem.CorDoMago("01-pyra"));
            Assert.AreNotEqual(SelecaoPersonagem.CorDoMago("01-pyra"), SelecaoPersonagem.CorDoMago("02-ceifadora"));
        }
    }
}
