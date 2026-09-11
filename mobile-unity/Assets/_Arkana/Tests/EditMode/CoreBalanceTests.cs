using NUnit.Framework;
using Arkana.Core;

namespace Arkana.Tests
{
    /// <summary>Os numeros de Balance.gd que o selftest cobra (5 perfis, escudo, DoT) e o produto unico de velocidade.</summary>
    public class CoreBalanceTests
    {
        private const float Eps = 0.0001f;

        [SetUp]
        public void SetUp() { Bus.Reset(); }

        [Test]
        public void Perfis_BatemComBalanceGd_DmgManaCadencia()
        {
            Balance.PerfilElemento f = Balance.Perfil(Elemento.Fogo);
            Assert.AreEqual(11f, f.Dmg, Eps); Assert.AreEqual(7.5f, f.ManaCost, Eps); Assert.AreEqual(0.26f, f.FireRate, Eps);
            Balance.PerfilElemento a = Balance.Perfil(Elemento.Agua);
            Assert.AreEqual(9.3f, a.Dmg, Eps); Assert.AreEqual(6.9f, a.ManaCost, Eps); Assert.AreEqual(0.28f, a.FireRate, Eps);
            Balance.PerfilElemento r = Balance.Perfil(Elemento.Raio);
            Assert.AreEqual(12.3f, r.Dmg, Eps); Assert.AreEqual(8.8f, r.ManaCost, Eps); Assert.AreEqual(0.32f, r.FireRate, Eps);
            Balance.PerfilElemento t = Balance.Perfil(Elemento.Terra);
            Assert.AreEqual(13.6f, t.Dmg, Eps); Assert.AreEqual(9.3f, t.ManaCost, Eps); Assert.AreEqual(0.35f, t.FireRate, Eps);
            Balance.PerfilElemento v = Balance.Perfil(Elemento.Vento);
            Assert.AreEqual(7.5f, v.Dmg, Eps); Assert.AreEqual(6.1f, v.ManaCost, Eps); Assert.AreEqual(0.23f, v.FireRate, Eps);
        }

        [Test]
        public void Perfis_FatoresAdimensionais_DaoPapelACadaElemento()
        {
            Assert.AreEqual(1.25f, Balance.Perfil(Elemento.Raio).Esc, Eps, "raio abre escudo");
            Assert.AreEqual(1.15f, Balance.Perfil(Elemento.Terra).Vida, Eps, "terra finaliza");
            Assert.AreEqual(0.75f, Balance.Perfil(Elemento.Vento).Esc, Eps, "vento nao quebra");
            Assert.AreEqual(2.0f, Balance.Perfil(Elemento.Vento).Empurrao, Eps, "vento tira do lugar");
            Assert.AreEqual(2.0f, Balance.Perfil(Elemento.Terra).Estrutura, Eps, "terra derruba cobertura");
            Assert.AreEqual(35f, Balance.Perfil(Elemento.Raio).ProjectileSpeed, Eps, "raio e' o mais rapido");
            Assert.AreEqual(18f, Balance.Perfil(Elemento.Terra).ProjectileSpeed, Eps, "terra e' o mais lento");
            // os 5 diferem entre si em dano
            foreach (Elemento x in Elementos.Todos)
                foreach (Elemento y in Elementos.Todos)
                    if (x != y) Assert.AreNotEqual(Balance.Perfil(x).Dmg, Balance.Perfil(y).Dmg, x + " e " + y + " com o mesmo dano");
        }

        [Test]
        public void Escudo_Tabela_E_Leis()
        {
            CollectionAssert.AreEqual(new float[] { 50f, 75f, 100f, 125f }, Balance.Escudo.Niveis);
            CollectionAssert.AreEqual(new float[] { 0f, 150f, 400f, 900f }, Balance.Escudo.Evoluir);
            Assert.AreEqual(4, Balance.Escudo.Cores.Length);
            Assert.IsTrue(Balance.Escudo.Transbordo, "transbordo e' lei");
            Assert.AreEqual(0f, Balance.Escudo.Regen, "escudo nao regenera sozinho");
        }

        [Test]
        public void Dot_Status_E_Player()
        {
            Assert.AreEqual(12f, Balance.Dot.TetoDps);
            Assert.IsTrue(Balance.Dot.IgnoraEscudo);
            Assert.AreEqual(0.25f, Balance.Dot.Tick, Eps);
            Assert.AreEqual(0.8f, Balance.Status.StunCap, Eps, "teto do kernel — NUNCA subir");
            Assert.AreEqual(100f, Balance.Player.Hp);
            Assert.AreEqual(16f, Balance.Player.ManaRegen);
            Assert.AreEqual(480f, Balance.Match.DurationS);
            Assert.AreEqual(12, Balance.Match.Bots);
            Assert.AreEqual(16, Balance.Terrain.FuelBudget);
        }

        [Test]
        public void Vitalidade_NasceComN1Cheio_E_ResetVolta()
        {
            Vitalidade v = new Vitalidade(Balance.Player.Hp);
            Assert.AreEqual(100f, v.Hp);
            Assert.AreEqual(50f, v.Escudo);
            Assert.AreEqual(50f, v.EscudoMax);
            Assert.AreEqual(1, v.Nivel);
            Assert.IsTrue(v.Viva);
            v.Hp = 0f; v.Escudo = 0f; v.DanoCausado = 999f; v.Evoluir();
            v.Reset();
            Assert.AreEqual(100f, v.Hp);
            Assert.AreEqual(50f, v.Escudo);
            Assert.AreEqual(1, v.Nivel);
            Assert.AreEqual(0f, v.DanoCausado);
        }

        [Test]
        public void Vitalidade_Curar_GrampeiaNoMax_E_BarraNaN()
        {
            Vitalidade v = new Vitalidade(100f);
            v.Hp = 90f;
            v.Curar(50f);
            Assert.AreEqual(100f, v.Hp);
            v.Hp = 90f;
            v.Curar(float.NaN);
            v.Curar(-5f);
            Assert.AreEqual(90f, v.Hp);
        }

        [Test]
        public void Velocidade_ProdutoUnico_NuncaDaZero()
        {
            float vmax = Balance.Player.Speed;
            Assert.AreEqual(vmax, Velocidade.Produto(vmax, 1f, 1f, 1f), Eps, "em terra seca o produto nao muda");
            Assert.AreEqual(vmax * Balance.Terrain.MudSlow, Velocidade.Produto(vmax, Balance.Terrain.MudSlow, 1f, 1f), Eps, "lama e' 0.55");
            Assert.Greater(Velocidade.Produto(vmax, 0f, 1f, 1f), 0f, "fator 0 vira piso, nunca trava o mago");
            Assert.Greater(Velocidade.Produto(vmax, 1f, -1f, 1f), 0f, "fator negativo vira piso");
            Assert.Greater(Velocidade.Produto(vmax, 1f, 1f, float.NaN), 0f, "NaN vira piso");
            Assert.IsFalse(float.IsNaN(Velocidade.Produto(float.NaN, float.NaN, float.NaN, float.NaN)));
            Assert.Greater(Velocidade.Produto(0f, 0f, 0f, 0f), 0f);
            Assert.AreEqual(vmax * Velocidade.Piso, Velocidade.Produto(vmax, 0f, 1f, 1f), Eps);
        }

        [Test]
        public void Textos_ArmaRotulo_NomeMaisElemento()
        {
            Assert.AreEqual("Luva", Textos.ArmaRotulo("Luva", new string[0]), "vazio devolve so' o nome");
            Assert.AreEqual("Luva", Textos.ArmaRotulo("Luva", new[] { "xyz" }), "desconhecido nao pendura separador");
            Assert.AreEqual("Luva · FOGO", Textos.ArmaRotulo("Luva", new[] { "fire" }));
            Assert.AreEqual("Manopla · ÁGUA + RAIO", Textos.ArmaRotulo("Manopla", new[] { Elemento.Agua, Elemento.Raio }));
        }
    }
}
