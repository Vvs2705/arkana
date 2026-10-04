using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Characters;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// OS VERBOS DE MOTOR que os kits pediram (onda 12A), um teste cada, sem cena: o pouso seguro do Teleportar, o dash do
    /// Impulso, o invisivel na percepcao do bot, o fator de pulo e a vida base da ficha. O kit que USA cada verbo e' provado
    /// no teste do grupo dele (FakeConjurador anota o pedido); aqui, o que o corpo faz com o pedido.
    /// </summary>
    public class GameplayMotorTests
    {
        private const float DT = 1f / 60f;

        [SetUp]
        public void SetUp() { Bus.Reset(); Combat.Reset(); Efeitos.Reset(); }

        [TearDown]
        public void TearDown() { Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        [Test]
        public void Teleporte_PousoSeguro_NemNoMar_NemEmCimaDaCopa_NemDentroDoMorro()
        {
            // a arvore em (0, 10): o colisor do tronco (0,38 m) sobe 3 m DENTRO da copa. O pouso de antes (o topo da Queda)
            // punha a Ceifadora la' em cima
            Func<float, float, float, float> tronco = (x, z, h) => new Vector2(x, z - 10f).magnitude < 0.38f ? h + 3f : h;
            var ilha = new FakeRelevo();
            Vector3 p = Pawn.PousoSeguro(ilha, Vector3.zero, new Vector3(0f, 0f, 10f), tronco);
            Assert.AreEqual(0f, p.y, 1e-4f, "no chao, nunca em cima da copa");
            Assert.Greater(new Vector2(p.x, p.z - 10f).magnitude, 0.38f + Pawn.PEGADA - 1e-3f, "com o corpo inteiro fora do tronco");
            Assert.Greater(p.z, 8.5f, "recuando o minimo pela linha");

            // mar a partir de z = 20: a Travessia de 60 m nao cospe ninguem na agua
            ilha.PousarFn = (x, z) => z < 20f;
            p = Pawn.PousoSeguro(ilha, Vector3.zero, new Vector3(0f, 0f, 60f), null);
            Assert.That(p.z, Is.InRange(18.5f, 20f), "recua ate' o chao seco");

            // encosta subindo para +z: o pe' vai na cota MAIS ALTA sob o corpo — nada dele fica dentro do morro
            ilha.PousarFn = null;
            ilha.AlturaFn = (x, z) => Mathf.Max(z - 5f, 0f);
            p = Pawn.PousoSeguro(ilha, Vector3.zero, new Vector3(0f, 0f, 10f), null);
            Assert.AreEqual(10f, p.z, 1e-3f, "chao livre: pousa no ponto pedido");
            Assert.GreaterOrEqual(p.y, ilha.Altura(0f, 10f + Pawn.PEGADA) - 1e-3f, "a pegada inteira acima do chao da encosta");

            // nada livre na linha inteira: fica onde esta'
            Vector3 de = new Vector3(1f, 2f, 3f);
            Assert.AreEqual(de, Pawn.PousoSeguro(new FakeRelevo(), de, new Vector3(1f, 0f, 20f), (x, z, h) => h + 1f), "sem pouso seguro, nao sai do lugar");
        }

        [Test]
        public void Impulso_ChegaNaDistanciaNoTempoPedido_NaRota_EParaSemODeslizeDoEmpurrao()
        {
            const float m = 6f, dur = 0.4f;
            var l = new Locomocao();
            float mana = 100f;
            Assert.IsTrue(l.Impulso(Vector3.forward, m, dur));
            Vector3 d = Vector3.zero;
            float t = 0f, noMeio = -1f;
            // o stick empurra para o LADO o tempo todo: no dash o corpo segue a rota do kit
            while (t < dur - 1e-5f)
            {
                d += l.Tick(DT, Vector3.right, true, false, Balance.Player.Speed, false, false, ref mana);
                t += DT;
                if (noMeio < 0f && t >= 0.1f - 1e-5f) noMeio = d.z;
            }
            Assert.AreEqual(m, d.z, m * 0.01f, "6 m em 0,4 s, como pedido");
            Assert.AreEqual(0f, d.x, 1e-3f, "o stick nao soma por cima: a rota telegrafada e' a do corpo");
            Assert.AreEqual(Locomocao.PercorridoNoDash(m, dur, 0.1f), noMeio, 0.02f, "a rota da logica (Investida) anda JUNTO do corpo");
            Assert.AreEqual(0f, l.ImpulsoLeft, 1e-5f, "acabou no tempo pedido");
            // soltou: emenda na corrida e freia como ela — sem a cauda de 1 s do empurrao
            for (int i = 0; i < 12; i++) d += l.Tick(DT, Vector3.zero, true, false, Balance.Player.Speed, false, false, ref mana);
            Assert.AreEqual(0f, l.VelocidadeHorizontal, 1e-3f, "0,2 s depois ja' parou");
            Assert.Less(d.z, m + 0.8f, "e o que sobra e' so' o freio da corrida");
        }

        [Test]
        public void Invisivel_OBotNaoVeNemOuveOsPassos_MasOuveODisparo_EColadoVeOBrilho()
        {
            var eu = new FakeEntidade("cacador", Vector3.zero);
            var bot = new PercepcaoBot(eu);
            bot.Ligar();
            var ilu = new FakeEntidade("ilusionista", new Vector3(6f, 0f, 0f));
            var magos = new List<IEntidade> { eu, ilu };
            bool oculto = true;
            Func<IEntidade, bool> ocultoDe = e => oculto && e == ilu;
            Func<IEntidade, float> andando = e => 4f;
            bot.Varrer(magos, andando, ocultoDe);
            Assert.IsNull(bot.Alvo, "invisivel a 6 m, andando: nem visto (12 m) nem ouvido (18 m)");
            Bus.EmitDisparo(ilu, ilu.Pos);
            Assert.AreSame(ilu, bot.Alvo, "conjurar DENUNCIA: o disparo continua sendo ouvido");
            bot.Varrer(magos, andando, ocultoDe);
            Assert.IsNull(bot.Alvo, "sumiu da vista: o bot larga o alvo...");
            Assert.AreEqual(ilu.Pos, bot.ConsumirPista().Value, "...e vai ate' onde o viu por ultimo");
            Combat.AplicarDano(eu, 5f, Elemento.Fogo, ilu);
            Assert.AreSame(ilu, bot.Alvo, "acertar ensina quem bateu (o revide nao olha)");
            bot.Esquecer();
            ilu.Pos = new Vector3(PercepcaoBot.VISAO_OCULTO - 0.5f, 0f, 0f);
            bot.Varrer(magos, andando, ocultoDe);
            Assert.AreSame(ilu, bot.Alvo, "colado, o brilho entrega (ficha 08: shimmer de perto)");
            bot.Esquecer();
            ilu.Pos = new Vector3(6f, 0f, 0f);
            oculto = false;
            bot.Varrer(magos, e => 0f, ocultoDe);
            Assert.AreSame(ilu, bot.Alvo, "apareceu: parado a 6 m e' visto de novo");
            bot.Desligar();
        }

        [Test]
        public void Pulo_FatorDeAltura_AMolaDoFizzSobe1_5xMaisAlto()
        {
            float normal = Topo(1f), mola = Topo(1.5f);
            Assert.AreEqual(1.5f, mola / normal, 0.03f, "h = v^2/2g: a mola multiplica a ALTURA, nao a velocidade");
            Assert.IsFalse(new Locomocao().Pular(false, false, 1.5f), "com mola ou sem, so' do chao");
        }

        private static float Topo(float fator)
        {
            var l = new Locomocao();
            Assert.IsTrue(l.Pular(true, false, fator));
            float y = 0f, topo = 0f, mana = 100f;
            for (int i = 0; i < 600 && y > -0.01f; i++)
            {
                y += l.Tick(DT, Vector3.zero, false, false, Balance.Player.Speed, false, false, ref mana).y;
                topo = Mathf.Max(topo, y);
            }
            return topo;
        }

        [Test]
        public void VidaBase_DaFicha_APipNasceCom55_ORestoNaRegua()
        {
            Assert.AreEqual(55f, IdentidadeMago.De("20-pip").VidaBase, "ficha 20: '55 de vida base (a mais fragil do elenco)'");
            foreach (string s in Kits.Slugs)
                if (s != "20-pip") Assert.AreEqual(Balance.Player.Hp, IdentidadeMago.De(s).VidaBase, s);
            Assert.AreEqual(Balance.Player.Hp, IdentidadeMago.De("99-ninguem").VidaBase, "slug sem ficha: a regua");
            Vitalidade v = Pawn.VidaDe("20-pip");
            Assert.AreEqual(55f, v.HpMax, "o corpo da Pip nasce com 55...");
            Assert.AreEqual(55f, v.Hp, "...cheio");
            Assert.AreEqual(Balance.Player.Hp, Pawn.VidaDe("01-pyra").HpMax);
        }

        [Test]
        public void Atirar_TiroManaECadencia_ExatamenteUmaVez_NoCorpoDeVerdade()
        {
            // GATE (04/10): fire/mana/cooldown UMA vez, no Pawn.Atirar real (antes so' o caminho unico garantia, sem teste)
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;   // o Mago instancia materiais no EditMode
            Pawn p = Pawn.Criar(null, "01-pyra", false);
            try
            {
                p.Aterrar(Vector3.zero);
                Assert.IsTrue(p.Slot.Equipar(Arma.VARINHA, null, Elemento.Fogo));
                int disparos = 0;
                Bus.Disparo += (a, o) => { if (ReferenceEquals(a, p)) disparos++; };
                ArmaSpec spec = p.Slot.Spec(Elemento.Fogo);
                float mana0 = p.Mana;
                Assert.IsTrue(p.Atirar(Vector3.forward), "o 1o toque dispara");
                Assert.IsFalse(p.Atirar(Vector3.forward), "o 2o toque no MESMO quadro: a cadencia barra");
                Assert.AreEqual(1, disparos, "um projetil so'");
                Assert.AreEqual(mana0 - spec.ManaCost, p.Mana, 1e-4f, "a mana foi cobrada uma vez");
                p.Mana = 0f;
                p.Tick(spec.FireRate * p.CadenciaMult + 0.01f);
                p.Mana = spec.ManaCost * 0.5f;
                Assert.IsFalse(p.Atirar(Vector3.forward), "sem mana para o tiro: nao sai, nem cobra");
                Assert.AreEqual(spec.ManaCost * 0.5f, p.Mana, 1e-4f, "recusado nao cobra");
                Assert.AreEqual(1, disparos);
                p.Mana = Balance.Player.ManaMax;
                Assert.IsTrue(p.Atirar(Vector3.forward), "passada a cadencia, com mana, sai o 2o");
                Assert.AreEqual(2, disparos);
            }
            finally { UnityEngine.Object.DestroyImmediate(p.gameObject); }
        }
    }
}
