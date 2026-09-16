using NUnit.Framework;
using UnityEngine;
using Arkana.Menu;

namespace Arkana.Tests
{
    /// <summary>
    /// A PLACA dos botoes de menu, PURA (Color32[], sem Unity na frente): o lado bate com o 9-slice, o canto sai
    /// TRANSPARENTE (o chanfro cortou), a moldura clara existe entre o contorno e o miolo, o miolo nao tem buraco, o
    /// secundario e' escuro e translucido, o principal e' ouro opaco e a luz vem de CIMA nos dois. A aura brilha so'
    /// por fora. Vermelho se o chanfro sumir, se a moldura virar fundo, se o degrade inverter ou se a aura invadir a
    /// placa (ela e' filha do botao: se pintar o miolo, cobre a placa inteira — foi assim que a oliva nasceu).
    /// </summary>
    public class MenuBotaoTests
    {
        static Color32 Em(Color32[] px, int lado, int x, int y) => px[y * lado + x];
        static float Luz(Color32 c) => (c.r * 0.30f + c.g * 0.59f + c.b * 0.11f) / 255f;

        [Test]
        public void Placa_CantoChanfrado_MolduraClara_MioloComVolume()
        {
            foreach (PlacaBotao.Tipo tipo in new[] { PlacaBotao.Tipo.Escura, PlacaBotao.Tipo.Cheia })
            {
                int lado = PlacaBotao.Lado(tipo);
                Color32[] px = PlacaBotao.Pixels(tipo);
                Assert.AreEqual(2 * PlacaBotao.Borda + PlacaBotao.Miolo, lado, "9-slice: borda + miolo + borda");
                Assert.AreEqual(lado * lado, px.Length, "a placa e' quadrada de lado " + lado);

                Assert.AreEqual(0, Em(px, lado, 1, 1).a, "canto de baixo-esquerda sem chanfro (" + tipo + ")");
                Assert.AreEqual(0, Em(px, lado, lado - 2, lado - 2).a, "canto de cima-direita sem chanfro (" + tipo + ")");
                Assert.Greater(Em(px, lado, lado / 2, 1).a, 200, "o meio da borda de baixo e' placa, nao buraco");

                int meio = lado / 2;
                float mioloLuz = Luz(Em(px, lado, meio, meio));
                bool moldura = false;
                for (int x = 1; x < (int)PlacaBotao.Vao; x++)
                {
                    Color32 c = Em(px, lado, x, meio);
                    moldura |= c.a > 200 && Luz(c) > mioloLuz + 0.15f;
                }
                Assert.IsTrue(moldura, "sem moldura clara na borda (" + tipo + ")");

                for (int x = (int)PlacaBotao.Vao + 2; x < lado - (int)PlacaBotao.Vao - 2; x++)
                    Assert.Greater(Em(px, lado, x, meio).a, 100, "buraco no miolo em x=" + x + " (" + tipo + ")");

                // linha 0 = EMBAIXO (padrao Unity): a luz vem de cima
                Assert.Greater(Luz(Em(px, lado, meio, lado - 6)), Luz(Em(px, lado, meio, 5)) + 0.05f, "o volume inverteu (" + tipo + ")");
            }

            int l = PlacaBotao.Lado(PlacaBotao.Tipo.Escura);
            Color32 escuro = Em(PlacaBotao.Pixels(PlacaBotao.Tipo.Escura), l, l / 2, l / 2);
            Assert.Less(Luz(escuro), 0.25f, "o miolo do secundario e' quase preto");
            Assert.Less(escuro.a, 250, "e translucido: o por do sol passa por tras");
            Color32 cheio = Em(PlacaBotao.Pixels(PlacaBotao.Tipo.Cheia), l, l / 2, l / 2);
            Assert.Greater(Luz(cheio), 0.5f, "o JOGAR e' ouro batido");
            Assert.AreEqual(255, cheio.a, "e opaco");
        }

        [Test]
        public void Aura_BrilhaSoPorFora()
        {
            int lado = PlacaBotao.Lado(PlacaBotao.Tipo.Aura);
            Color32[] px = PlacaBotao.Pixels(PlacaBotao.Tipo.Aura);
            Assert.AreEqual(2 * (PlacaBotao.Margem + PlacaBotao.Borda) + PlacaBotao.Miolo, lado, "a aura tem a margem do brilho");
            Assert.AreEqual(lado * lado, px.Length);
            int meio = lado / 2;
            Assert.AreEqual(0, Em(px, lado, meio, meio).a, "a aura e' vazada: nao cobre a placa");
            Assert.AreEqual(0, Em(px, lado, PlacaBotao.Margem + 4, meio).a, "nem um texel dentro da placa");
            Assert.Greater(Em(px, lado, PlacaBotao.Margem - 2, meio).a, 30, "o brilho encosta na borda da placa");
            Assert.Less(Em(px, lado, 0, meio).a, 40, "e esvaece antes da borda da textura");
        }
    }
}
