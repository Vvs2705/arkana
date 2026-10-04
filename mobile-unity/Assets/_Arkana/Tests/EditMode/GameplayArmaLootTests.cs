using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>Armas, slot e loot: _test_armas, _test_loot, _test_elemento_na_luva, _test_loot_na_ilha_real.</summary>
    public class GameplayArmaLootTests
    {
        private List<object[]> _armados;
        private List<object[]> _prompts;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            _armados = new List<object[]>();
            _prompts = new List<object[]>();
            Bus.WeaponEquipped += (p, id, nome, rar, els) => _armados.Add(new object[] { p, id, nome, rar, els });
            Bus.LootPrompt += (nome, rar, perto) => _prompts.Add(new object[] { nome, rar, perto });
        }

        // ------------------------------------------------------------- registro

        [Test]
        public void Registro_TresTiers_VarinhaBase_CajadoAlcance_ManoplaEstalar()
        {
            Assert.AreEqual(3, Arma.ARMAS.Count);
            ArmaDef v = Arma.Dados(Arma.VARINHA), c = Arma.Dados(Arma.CAJADO), m = Arma.Dados(Arma.MANOPLA);
            Assert.AreEqual(1f, v.Dmg); Assert.AreEqual(1f, v.FireRate); Assert.AreEqual(1f, v.Range);
            Assert.AreEqual(1f, v.ManaCost); Assert.AreEqual(1f, v.ProjectileSpeed);
            Assert.AreEqual(1, v.Elementos); Assert.AreEqual("comum", v.Raridade);
            Assert.Greater(c.Dmg, v.Dmg, "cajado bate mais forte");
            Assert.Greater(c.Range, v.Range, "cajado alcanca mais longe");
            Assert.Greater(c.FireRate, v.FireRate, "cajado conjura MAIS LENTO");
            Assert.Greater(c.SupremaBonus, 1f);
            Assert.AreEqual("raro", c.Raridade);
            Assert.AreEqual("lendaria", m.Raridade);
            Assert.AreEqual(2, m.Elementos, "manopla porta 2 elementos");
            Assert.Less(m.FireRate, v.FireRate, "manopla conjura MAIS RAPIDO (estalar de dedos)");
            Assert.Less(m.FireRate, c.FireRate);
            Assert.AreEqual(1, v.Runas); Assert.AreEqual(2, c.Runas); Assert.AreEqual(3, m.Runas);
            Assert.Less(Arma.Tier(Arma.VARINHA), Arma.Tier(Arma.CAJADO));
            Assert.Less(Arma.Tier(Arma.CAJADO), Arma.Tier(Arma.MANOPLA));
            Assert.AreEqual(-1, Arma.Tier(""), "maos nuas = tier -1");
            var cores = new HashSet<Color>(); var formas = new HashSet<string>();
            foreach (string id in Arma.TIERS) { cores.Add(Arma.Cor(id)); formas.Add(Arma.Forma(id)); }
            Assert.AreEqual(3, cores.Count, "3 cores de raridade");
            Assert.AreEqual(3, formas.Count, "3 FORMAS de raridade (GDD 10: nunca so' cor)");
            foreach (Elemento[] par in Arma.PARES_MANOPLA)
            {
                Assert.AreEqual(2, par.Length, "todo par de manopla tem 2 elementos");
                Assert.AreNotEqual(par[0], par[1], "e distintos");
            }
            foreach (Elemento el in Elementos.Todos)
            {
                Balance.PerfilElemento b = Balance.Perfil(el);
                ArmaSpec sc = Arma.Spec(el, Arma.CAJADO);
                Assert.AreEqual(b.Dmg * c.Dmg, sc.Dmg, 0.001f, "spec = Balance x arma");
                Assert.AreEqual(b.Range * c.Range, sc.Range, 0.001f);
                ArmaSpec sv = Arma.Spec(el, Arma.VARINHA);
                Assert.AreEqual(b.Dmg, sv.Dmg, 0.001f, "varinha = identidade");
                Assert.AreEqual(b.FireRate, sv.FireRate, 0.001f);
            }
        }

        // ------------------------------------------------------------- a Lei das Luvas

        [Test]
        public void MaosNuas_NaoAtaca_CarrosselEscondido_EquiparAcende()
        {
            var pawn = new FakeEntidade("p", Vector3.zero, true);
            var slot = new ArmaSlot(pawn);
            Assert.AreEqual("", slot.ArmaId, "todo mundo nasce de MAOS NUAS");
            Assert.IsFalse(slot.PodeAtacar, "sem luva NAO ha' ataque basico");
            Assert.AreEqual(-1, slot.Tier, "a primeira varinha do chao E' upgrade");
            Assert.IsFalse(slot.CarrosselVisivel, "de maos nuas nao existe elemento na tela");

            Assert.IsTrue(slot.Equipar(Arma.VARINHA, null, Elemento.Agua));
            Assert.IsTrue(slot.PodeAtacar, "equipou: armado");
            Assert.AreEqual(1, _armados.Count);
            Assert.AreSame(pawn, _armados[0][0], "WeaponEquipped leva o pawn PRIMEIRO");
            Assert.AreEqual(Arma.VARINHA, _armados[0][1]);
            Assert.AreEqual("comum", _armados[0][3]);
            Assert.IsFalse(slot.CarrosselVisivel, "luva de elemento travado esconde o carrossel");
            Assert.IsFalse(slot.Equipar("machado"), "id desconhecido nao equipa");
            Assert.AreEqual(1, _armados.Count);

            var antes = slot.Spec(Elemento.Fogo);
            slot.Equipar(Arma.CAJADO, null, Elemento.Fogo);
            var depois = slot.Spec(Elemento.Fogo);
            Assert.Greater(depois.Dmg, antes.Dmg, "equipar cajado aumenta o dano");
            Assert.Greater(depois.Range, antes.Range);
            Assert.Greater(depois.FireRate, antes.FireRate, "e deixa a conjuracao mais lenta");
        }

        [Test]
        public void ElementoMoraNaLuva_PegarDeMaosNuasConsome_TrocarDeixaAVelha()
        {
            var pawn = new FakeEntidade("p", Vector3.zero, false);
            var slot = new ArmaSlot(pawn);
            var loot = new Loot(null);
            var luva = new LootItem(Arma.VARINHA, null, Elemento.Agua, Vector3.zero);
            loot.Itens.Add(luva);

            Assert.IsTrue(loot.Pegar(luva, slot));
            Assert.AreEqual(Elemento.Agua, slot.ElementoDaLuva, "pegar leva o ELEMENTO da luva junto");
            Assert.AreEqual(Elemento.Agua, slot.ElementoDoDisparo(Elemento.Fogo), "o disparo sai com o elemento DA LUVA");
            CollectionAssert.AreEqual(new[] { Elemento.Agua }, slot.Elementos(Elemento.Fogo));
            CollectionAssert.AreEqual(new[] { Elemento.Agua }, (Elemento[])_armados[0][4], "a HUD recebe o elemento travado");
            Assert.AreEqual(0, loot.Itens.Count, "de MAOS NUAS o loot e' CONSUMIDO: sem luva fantasma no chao");

            var outra = new LootItem(Arma.CAJADO, null, Elemento.Terra, Vector3.zero);
            loot.Itens.Add(outra);
            Assert.IsTrue(loot.Pegar(outra, slot));
            Assert.AreEqual(Arma.CAJADO, slot.ArmaId);
            Assert.AreEqual(Elemento.Terra, slot.ElementoDaLuva, "trocou: a nova impoe o elemento dela");
            Assert.AreEqual(1, loot.Itens.Count, "TROCA, nao consumo");
            Assert.AreEqual(Arma.VARINHA, outra.ArmaId, "a velha ficou no chao");
            Assert.AreEqual(Elemento.Agua, outra.ElementoDaLuva, "COM o elemento dela — o swap viaja completo");

            var igual = new LootItem(Arma.CAJADO, null, Elemento.Fogo, Vector3.zero);
            loot.Itens.Add(igual);
            _armados.Clear();
            Assert.IsFalse(loot.Pegar(igual, slot), "pegar a arma que JA' esta' equipada nao faz nada");
            Assert.AreEqual(0, _armados.Count, "e nao emite WeaponEquipped duplicado");
        }

        [Test]
        public void Manopla_DoisElementosFixos_AlternaTiroATiro_CarrosselNaoManda()
        {
            var slot = new ArmaSlot(new FakeEntidade("p", Vector3.zero, true));
            _armados.Clear();
            slot.Equipar(Arma.MANOPLA, new[] { Elemento.Fogo, Elemento.Vento });
            CollectionAssert.AreEqual(new[] { Elemento.Fogo, Elemento.Vento }, slot.Elementos(Elemento.Agua), "o par e' FIXO");
            Assert.AreEqual(2, ((Elemento[])_armados[0][4]).Length, "WeaponEquipped leva os 2 elementos");
            Elemento e1 = slot.ElementoDoDisparo(Elemento.Agua), e2 = slot.ElementoDoDisparo(Elemento.Agua);
            Assert.AreNotEqual(e1, e2, "alterna entre os 2 fixos");
            Assert.IsTrue(e1 == Elemento.Fogo || e1 == Elemento.Vento);
            Assert.IsTrue(e2 == Elemento.Fogo || e2 == Elemento.Vento);
            Assert.IsFalse(slot.CarrosselVisivel, "carrossel nao troca elemento de manopla");
            Assert.Less(slot.Spec(Elemento.Fogo).FireRate, Balance.Fogo.FireRate, "cast mais rapido que a linha de base");

            var loot = new Loot(null);
            LootItem bau = loot.BauCelestial(new Vector3(0f, 2f, 0f), 1);
            Assert.AreEqual(Arma.MANOPLA, bau.ArmaId, "BauCelestial() entrega manopla");
            CollectionAssert.AreEqual(Arma.PARES_MANOPLA[1], bau.Par, "com o par fixo do indice");
        }

        // ------------------------------------------------------------- distribuicao

        private static List<string> Assinatura(Loot l)
        {
            var saida = new List<string>();
            foreach (LootItem i in l.Itens) saida.Add(i.ArmaId + ":" + i.Pos.ToString("F3"));
            return saida;
        }

        [Test]
        public void Espalhar_Deterministico_16Loots_ManoplaNaoNasceNoChao_ElementosCiclam()
        {
            var a = new Loot(null); int na = a.Espalhar();
            var b = new Loot(null); int nb = b.Espalhar();
            Assert.AreEqual(Loot.QTD_VARINHA + Loot.QTD_CAJADO, na, "nasceram 12 varinhas + 4 cajados");
            Assert.AreEqual(na, nb);
            CollectionAssert.AreEqual(Assinatura(a), Assinatura(b), "MESMO SEED = MESMAS posicoes");
            var c = new Loot(null); c.Espalhar(999);
            CollectionAssert.AreNotEqual(Assinatura(a), Assinatura(c), "seed diferente = mapa diferente");

            int varinhas = 0, cajados = 0, manoplas = 0;
            var vistos = new HashSet<Elemento>();
            foreach (LootItem i in a.Itens)
            {
                if (i.ArmaId == Arma.VARINHA) varinhas++;
                if (i.ArmaId == Arma.CAJADO) cajados++;
                if (i.ArmaId == Arma.MANOPLA) manoplas++;
                if (i.ElementoDaLuva.HasValue) vistos.Add(i.ElementoDaLuva.Value);
            }
            Assert.AreEqual(Loot.QTD_VARINHA, varinhas);
            Assert.AreEqual(Loot.QTD_CAJADO, cajados, "cajados raros, 1 por POI");
            Assert.AreEqual(0, manoplas, "MANOPLA NAO nasce no chao (so' Bau Celestial)");
            Assert.GreaterOrEqual(vistos.Count, 3, "o loot cicla elementos — quem quer um, ANDA");
        }

        [Test]
        public void UmCajadoPorPoi_DentroDoRaioDoPoi_EAFracaoAcompanhaOMapa()
        {
            var pois = new[]
            {
                FakeRelevo.PoiDe("ruinas", new Vector2(120f, -140f), 40f, 264f),
                FakeRelevo.PoiDe("floresta", new Vector2(-130f, -120f), 80f, 264f),
                FakeRelevo.PoiDe("lago", new Vector2(150f, 60f), 48f, 264f),
                FakeRelevo.PoiDe("alagado", new Vector2(-140f, 120f), 44f, 264f),
            };
            var relevo = new FakeRelevo { Raio = 264f, PoisLista = pois, PousarFn = (x, z) => true };
            var loot = new Loot(relevo);
            loot.Espalhar();
            var cajados = new List<Vector2>();
            float maisLonge = 0f;
            foreach (LootItem i in loot.Itens)
            {
                var xz = new Vector2(i.Pos.x, i.Pos.z);
                maisLonge = Mathf.Max(maisLonge, xz.magnitude);
                if (i.ArmaId == Arma.CAJADO) cajados.Add(xz);
            }
            Assert.AreEqual(pois.Length, cajados.Count);
            foreach (Poi poi in pois)
            {
                float perto = float.PositiveInfinity; Vector2 meu = Vector2.zero;
                foreach (Vector2 cj in cajados)
                    if (Vector2.Distance(cj, poi.Centro) < perto) { perto = Vector2.Distance(cj, poi.Centro); meu = cj; }
                string dono = poi.Nome;
                foreach (Poi outro in pois)
                    if (Vector2.Distance(meu, outro.Centro) < Vector2.Distance(meu, poi.Centro)) dono = outro.Nome;
                Assert.AreEqual(poi.Nome, dono, "o cajado mais proximo do POI pertence a ELE");
                Assert.LessOrEqual(perto, poi.Raio, "cajado DENTRO do raio do POI " + poi.Nome);
            }
            Assert.Greater(maisLonge, 264f * 0.3f, "o loot alcanca a ilha inteira, em fracao do raio de terra");
        }

        [Test]
        public void NenhumLootNaAgua_QuemDecideOChaoEAIlha()
        {
            // metade oeste e' agua; o loot pede PodePousar e nunca cai la'
            var relevo = new FakeRelevo { PousarFn = (x, z) => x >= 0f };
            var loot = new Loot(relevo);
            int n = loot.Espalhar();
            Assert.Greater(n, 0);
            foreach (LootItem i in loot.Itens) Assert.GreaterOrEqual(i.Pos.x, 0f, "nenhum loot na agua");
        }

        // ------------------------------------------------------------- prompt e auto-upgrade

        [Test]
        public void LootPrompt_SoNaBorda_EntrouSaiu()
        {
            var loot = new Loot(null);
            loot.Itens.Add(new LootItem(Arma.VARINHA, null, Elemento.Agua, Vector3.zero));
            loot.Atualizar(new Vector3(20f, 0f, 0f));
            Assert.AreEqual(0, _prompts.Count, "longe: nada");
            loot.Atualizar(new Vector3(1f, 0f, 0f));
            Assert.AreEqual(1, _prompts.Count, "entrou no raio: prompt");
            Assert.IsTrue((bool)_prompts[0][2]);
            Assert.AreEqual("comum", _prompts[0][1]);
            loot.Atualizar(new Vector3(0.5f, 0f, 0f));
            loot.Atualizar(new Vector3(1.5f, 0f, 0f));
            Assert.AreEqual(1, _prompts.Count, "ficar dentro NAO repete (nao e' sinal por frame)");
            loot.Atualizar(new Vector3(20f, 0f, 0f));
            Assert.AreEqual(2, _prompts.Count, "saiu: prompt some");
            Assert.IsFalse((bool)_prompts[1][2]);
        }

        [Test]
        public void AutoUpgrade_BotPegaTierMelhor_ERecusaSidegrade()
        {
            var bot = new FakeEntidade("bot", Vector3.zero, false);
            var slot = new ArmaSlot(bot);
            var loot = new Loot(null);
            loot.Itens.Add(new LootItem(Arma.VARINHA, null, Elemento.Fogo, Vector3.zero));
            Assert.IsTrue(loot.TentarAutoUpgrade(slot), "desarmado: a primeira varinha e' upgrade");
            Assert.AreEqual(Arma.VARINHA, slot.ArmaId);
            loot.Itens.Add(new LootItem(Arma.VARINHA, null, Elemento.Agua, Vector3.zero));
            Assert.IsFalse(loot.TentarAutoUpgrade(slot), "mesmo tier: o prompt (TROCAR) decide, nao o pisar em cima");
            loot.Itens.Add(new LootItem(Arma.CAJADO, null, Elemento.Terra, Vector3.zero));
            Assert.IsTrue(loot.TentarAutoUpgrade(slot));
            Assert.AreEqual(Arma.CAJADO, slot.ArmaId, "tier melhor: pega");
            Assert.AreEqual(Loot.EMISSAO_CORPO, 0.35f, "constante de apresentacao registrada");
        }

        [Test]
        public void Manopla_CobraOElementoQueSai_EspiarNaoGira()
        {
            // 04/10: a manopla cobrava a mana do elemento do carrossel e atirava o do par
            var slot = new ArmaSlot(new FakeEntidade("p", Vector3.zero, true));
            slot.Equipar(Arma.MANOPLA, new[] { Elemento.Fogo, Elemento.Vento });
            Elemento previsto = slot.ProximoDisparo(Elemento.Agua);
            Assert.AreEqual(previsto, slot.ProximoDisparo(Elemento.Agua), "espiar nao gira a manopla");
            Assert.AreEqual(previsto, slot.ElementoDoDisparo(Elemento.Agua), "o tiro que sai e' o que foi cobrado");
            Assert.AreNotEqual(previsto, slot.ProximoDisparo(Elemento.Agua), "depois do tiro, o proximo e' o outro do par");
            var luva = new ArmaSlot(new FakeEntidade("q", Vector3.zero, true));
            luva.Equipar(Arma.CAJADO, null, Elemento.Terra);
            Assert.AreEqual(Elemento.Terra, luva.ProximoDisparo(Elemento.Agua), "luva comum: o elemento dela, nao o do carrossel");
        }
    }
}
