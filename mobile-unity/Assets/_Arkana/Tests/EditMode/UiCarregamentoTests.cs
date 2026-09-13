using NUnit.Framework;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>
    /// A tela de carregamento, pura: a barra so' anda para a frente e nunca passa do progresso real, a dica troca no tempo
    /// (e esvaece na virada), e a tela some depois que a montagem acaba — e so' depois. Vermelho se a barra voltar, se o
    /// NaN de uma carga quebrar o progresso, se a dica travar ou se a tela sumir antes do Terminar.
    /// </summary>
    public class UiCarregamentoTests
    {
        [Test]
        public void Barra_SoAnda_ADicaTroca_ETelaSomeSoDepoisDoFim()
        {
            var l = new CarregamentoLogica(3, 5);   // 5 fora da faixa: a primeira dica e' a 2
            Assert.AreEqual(2, l.Dica);
            Assert.AreEqual(1f, l.DicaAlfa, 1e-4f, "a primeira dica ja' nasce lendo");
            Assert.AreEqual(0, l.Porcento);

            float barra = 0f;
            float[] cargas = { 0.1f, 0.05f, float.NaN, 0.4f, 0.3f, 0.8f, 0.75f };
            foreach (float c in cargas)
            {
                l.Avancar(c);
                for (int i = 0; i < 5; i++)
                {
                    l.Tick(0.05f);
                    Assert.GreaterOrEqual(l.Barra, barra, "a barra nunca volta");
                    Assert.LessOrEqual(l.Barra, l.Progresso + 1e-5f, "nem passa do que carregou de verdade");
                    barra = l.Barra;
                }
            }
            Assert.AreEqual(0.8f, l.Progresso, 1e-5f, "carga que volta (ou NaN) nao conta");
            Assert.Greater(l.Barra, 0.5f, "a barra persegue o progresso");
            Assert.IsFalse(l.Pronto);
            Assert.AreEqual(1f, l.Alfa, "carregando, a tela esta' inteira");

            // a dica troca a cada DicaS e esvaece na virada
            while (l.Tempo < CarregamentoLogica.DicaS - CarregamentoLogica.ViradaS * 0.5f) l.Tick(0.05f);
            Assert.AreEqual(2, l.Dica, "ainda a primeira");
            Assert.Less(l.DicaAlfa, 1f, "esvaecendo na virada");
            while (l.Tempo < CarregamentoLogica.DicaS + 0.01f) l.Tick(0.01f);
            Assert.AreEqual(0, l.Dica, "trocou e deu a volta na lista");
            while (l.Tempo < CarregamentoLogica.DicaS * 2f + 0.01f) l.Tick(0.05f);
            Assert.AreEqual(1, l.Dica);

            // a tela so' some depois do fim: o tempo sozinho nao a tira
            for (int i = 0; i < 200; i++) l.Tick(0.05f);
            Assert.IsFalse(l.Sumiu, "sem Terminar a tela fica");
            l.Terminar();
            Assert.AreEqual(100, l.Porcento, "acabou: 100% na hora");
            l.Avancar(0.2f);
            Assert.AreEqual(1f, l.Progresso, "depois do fim nada muda");
            Assert.AreEqual(1f, l.Alfa, "o esvaecer comeca no proximo quadro");
            l.Tick(CarregamentoLogica.SomeS * 0.5f);
            Assert.AreEqual(0.5f, l.Alfa, 1e-4f, "esvaecendo");
            Assert.IsFalse(l.Sumiu);
            l.Tick(CarregamentoLogica.SomeS);
            Assert.IsTrue(l.Sumiu, "esvaeceu: a partida esta' na tela");
            Assert.AreEqual(0f, l.Alfa);
        }
    }
}
