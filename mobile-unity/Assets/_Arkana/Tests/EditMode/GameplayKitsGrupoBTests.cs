using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Terrain;

namespace Arkana.Tests
{
    /// <summary>
    /// Os kits do GRUPO B (Vitalis, Ilusionista, Vex, Aelion) no padrao do GameplayKitsTests: FakeConjurador sem cena, o
    /// motor de verdade (KitRunner, Combat, Efeitos, Derrubado). Por mago: o efeito da tatica, o LIMITADOR que a paga, a
    /// telegrafia e o efeito da suprema, e a passiva. As leis gerais (sem mana, 1-4 s, 5-10 s) ja' valem pelo CoreKitsTests,
    /// que le' todo implementado.
    /// </summary>
    public class GameplayKitsGrupoBTests
    {
        private List<object[]> _teles, _states, _thits;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset(); Derrubado.Reset();
            _teles = new List<object[]>(); _states = new List<object[]>(); _thits = new List<object[]>();
            Bus.KitTelegraph += (s, t, d, p) => _teles.Add(new object[] { s, t, d });
            Bus.KitState += (n, on) => _states.Add(new object[] { n, on });
            Bus.TerrainHit += (e, p, s) => _thits.Add(new object[] { e, p, s });
        }

        [TearDown]
        public void TearDown() { Derrubado.Reset(); Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        // ---------------------------------------------------------------- apoio

        /// <summary>O laco da partida em miniatura: a janela de DoT fecha (Combat.TickDot) e o runner anda.</summary>
        private static void Andar(KitRunner k, float total, float dt = 0.05f)
        {
            int n = Mathf.CeilToInt(total / dt - 1e-3f);
            for (int i = 0; i < n; i++) { Combat.TickDot(dt); k.Tick(dt); }
        }

        /// <summary>Enche a carga na hora pelo canal do DANO (1000 x CargaPorDano = 150%).</summary>
        private static void Carregar(KitRunner k) => k.Tick(0.01f, 1000f);

        private bool TemEstado(string nome, bool ligado) => _states.Exists(e => (string)e[0] == nome && (bool)e[1] == ligado);

        private static float Ehp(IEntidade e) => e.Vital.Hp + e.Vital.Escudo;

        /// <summary>A ficha com a tatica sem espera (varios usos no mesmo teste).</summary>
        private static Kits.KitDef Rapida(string slug)
        {
            Kits.KitDef d = Kits.De(slug);
            return new Kits.KitDef
            {
                Slug = d.Slug, Nome = d.Nome, Implementado = d.Implementado, TaticaCd = 0.05f,
                SupremaCarga = d.SupremaCarga, Telegrafia = d.Telegrafia, Passiva = d.Passiva, Tatica = d.Tatica, Suprema = d.Suprema,
            };
        }

        /// <summary>Esquadrao de dois (hoje so' os EhPlayer): quem cai e' DERRUBADO, nao morre.</summary>
        private static void Esquadrao(List<IEntidade> arena)
        {
            Derrubado.Instalar();
            Derrubado.Arena = arena;
        }

        // ================================================================ VITALIS

        [Test]
        public void Vitalis_VaiLumen_Cura8PorSegundoPor12s_EQualquerDanoDissipa()
        {
            var p = new FakeConjurador("vitalis", Vector3.zero);
            var k = new KitRunner("07-vitalis", p, Rapida("07-vitalis"));
            var vit = (Vitalis)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            float mana0 = p.Mana;
            p.Vital.Hp = 1f;
            k.Tick(0.05f);   // o runner le' a vida baixada como dano: o tique a absorve ANTES da cura existir
            Assert.IsTrue(k.UsarTatica());
            Assert.IsTrue(vit.Curando && TemEstado(Vitalis.LUMEN_CURA, true), "a Lumen foi curar (a HUD sabe)");
            Andar(k, 1f);
            Assert.AreEqual(1f + t["cura"], p.Vital.Hp, 0.2f, "Vai Lumen: 8 hp/s (SOLO: na propria Vitalis)");
            Andar(k, t["duracao"]);
            Assert.AreEqual(1f + t["cura"] * t["duracao"], p.Vital.Hp, 0.3f, "cura por 12s e PARA");
            Assert.IsFalse(vit.Curando);
            Assert.AreEqual(mana0, p.Mana, "a tatica nao toca a mana");

            // o PRECO: qualquer dano dissipa a Lumen — a cura para no ato
            p.Vital.Hp = 30f;
            k.Tick(0.1f);
            Assert.IsTrue(k.UsarTatica());
            Andar(k, 0.5f);
            Combat.AplicarDano(p, 1f, Elemento.Fogo, null);   // no escudo: ate' 1 ponto conta
            k.Tick(0.05f);
            Assert.IsFalse(vit.Curando, "qualquer dano DISSIPA a Lumen");
            Assert.IsTrue(TemEstado(Vitalis.LUMEN_CURA, false), "a HUD recebe o desligamento");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "vitalis_susto"), "ela volta assustada (visivel)");
            float hp = p.Vital.Hp;
            Andar(k, 1f);
            Assert.AreEqual(hp, p.Vital.Hp, 0.001f, "dissipada, nao cura mais");
        }

        [Test]
        public void Vitalis_JardimDaAurora_TelegrafaCuraDentroSelaInimigo_EApagaALumen()
        {
            var p = new FakeConjurador("vitalis", Vector3.zero);
            var k = new KitRunner("07-vitalis", p);
            var vit = (Vitalis)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var dentro = new FakeEntidade("boneco", new Vector3(3f, 0f, 0f));
            var fora = new FakeEntidade("longe", new Vector3(20f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(dentro); p.Arena.Add(fora);
            p.Vital.Hp = 50f; dentro.Vital.Hp = 50f; fora.Vital.Hp = 50f;

            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            Assert.AreEqual(k.Dados.Telegrafia, (float)_teles[0][2], 0.001f, "o aviso leva 1,5s");
            Andar(k, k.Dados.Telegrafia - 0.2f);
            Assert.IsNull(vit.JardimAtivo, "durante o aviso NADA floresce");
            Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "nem cura");
            Andar(k, 0.3f);
            Assert.IsNotNull(vit.JardimAtivo, "o Jardim floresce DEPOIS do aviso");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "vitalis_jardim" && Mathf.Approximately(v.Raio, s["raio"])), "o circulo de 6m e' visivel");
            Assert.IsTrue(TemEstado(Vitalis.JARDIM, true));

            float hp = p.Vital.Hp;
            Andar(k, 1f);
            Assert.AreEqual(hp + s["cura"], p.Vital.Hp, 0.3f, "dentro do Jardim ela regenera 8 hp/s");

            // O SELO: inimigo dentro NAO recebe cura nenhuma; fora cura normal
            dentro.Vital.Curar(20f); fora.Vital.Curar(20f);
            k.Tick(0.05f);
            Assert.AreEqual(50f, dentro.Vital.Hp, 0.001f, "inimigo DENTRO: a cura nao entra");
            Assert.AreEqual(70f, fora.Vital.Hp, 0.001f, "fora do Jardim a cura vale");
            dentro.Pos = new Vector3(10f, 0f, 0f);
            Andar(k, 0.3f);
            dentro.Vital.Curar(10f);
            k.Tick(0.05f);
            Assert.AreEqual(60f, dentro.Vital.Hp, 0.001f, "saiu do Jardim: o selo solta");

            // o PRECO: murcha e a Lumen volta APAGADA — 3s sem passiva
            for (int i = 0; i < 400 && vit.JardimAtivo != null; i++) k.Tick(0.05f);
            Assert.IsNull(vit.JardimAtivo, "10s e acabou");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "vitalis_murcha"), "as flores murcham em petalas (visivel)");
            Assert.IsTrue(k.EstadoAtivo(Vitalis.LUMEN_APAGADA) && !vit.LumenEmCasa(k), "Lumen apagada: sem passiva");
            Andar(k, s["apagada_dur"] + 0.1f);
            Assert.IsTrue(vit.LumenEmCasa(k), "3s e a Lumen volta");
        }

        [Test]
        public void Vitalis_MaosLivres_LumenReergueOCaido_SemEscudo_SoComALumenEmCasa()
        {
            var p = new FakeConjurador("vitalis", Vector3.zero);
            var k = new KitRunner("07-vitalis", p);
            var vit = (Vitalis)k.Impl;
            var aliado = new FakeConjurador("aliada", new Vector3(5f, 0f, 0f));   // longe do raio de 2,4m do resgate a pe'
            p.Arena.Add(p); p.Arena.Add(aliado);
            Esquadrao(p.Arena);
            Combat.AplicarDano(aliado, 500f, Elemento.Fogo, null);
            Assert.IsTrue(Derrubado.Esta(aliado), "com esquadrao o aliado CAI");

            // a Lumen FORA (curando) = sem passiva: ninguem reergue
            Assert.IsTrue(k.UsarTatica());
            Andar(k, 7f);
            Assert.IsTrue(Derrubado.Esta(aliado), "Lumen longe: a Vitalis fica SEM a passiva");
            Assert.IsNull(vit.Reerguendo);

            Combat.AplicarDano(p, 1f, Elemento.Fogo, null);   // dissipa a cura: a Lumen volta
            k.Tick(0.05f);
            Assert.IsTrue(vit.LumenEmCasa(k));
            Andar(k, 1f);
            Assert.AreSame(aliado, vit.Reerguendo, "a Lumen voa ate' o caido");
            Assert.IsTrue(Derrubado.Esta(aliado), "reerguer leva 6s");
            Andar(k, k.Dados.Passiva["reerguer_s"]);
            Assert.IsFalse(Derrubado.Esta(aliado), "a Lumen reergueu enquanto a Vitalis seguia de pe'");
            Assert.AreEqual(0f, aliado.Vital.Escudo, 0.001f, "Lumen reerguendo NAO gera escudo");
            Assert.AreEqual(Balance.Player.Hp * Derrubado.VIDA_REERGUIDO, aliado.Vital.Hp, 0.01f);
        }

        // ============================================================ ILUSIONISTA

        [Test]
        public void Ilusionista_Espelho_DevolveA30PorCento_SoPelaFrente_EQuebraNa3a()
        {
            var p = new FakeConjurador("ilusionista", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("08-ilusionista", p, Rapida("08-ilusionista"));
            var ilu = (Ilusionista)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            var bot = new FakeConjurador("bot", new Vector3(0f, 0f, 12f), false);
            p.Arena.Add(p); p.Arena.Add(bot);
            var arena = new List<Projetil>();
            int absorvidos = 0;
            k.ProjeteisVivos = () => arena;
            k.AoAbsorver = pr => absorvidos++;

            Assert.IsTrue(k.UsarTatica());
            Assert.IsNotNull(ilu.EspelhoAtivo, "o espelho de corpo inteiro nasce na mira");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "ilusionista_espelho"));
            Projetil vem = Projetil.Lancar(bot, new Vector3(0f, 1.4f, 2f), Vector3.back, Elemento.Fogo);
            Projetil costas = Projetil.Lancar(bot, new Vector3(0f, 1.4f, 1.6f), Vector3.forward, Elemento.Fogo);
            arena.Add(vem); arena.Add(costas);
            k.Tick(0.02f);
            Assert.IsFalse(arena.Contains(vem), "o tiro que VEM pela frente e' pego pelo vidro");
            Assert.IsTrue(arena.Contains(costas), "o que passa pelas costas NAO e' devolvido");
            Assert.AreEqual(1, absorvidos);
            Assert.AreEqual(1, ilu.Devolvidos.Count, "e volta como reflexo");
            float ehp = Ehp(bot);
            Andar(k, 0.1f, 0.02f);
            Assert.AreEqual(ehp, Ehp(bot), 0.001f, "o reflexo VIAJA (tempo de viagem, §4.1)");
            Andar(k, 0.6f, 0.02f);
            Assert.AreEqual(vem.Dano * t["fracao_dano"], ehp - Ehp(bot), 0.01f, "devolve a quem atirou com 30% do dano");
            Assert.Greater(p.Vital.DanoCausado, 0f, "o dano devolvido e' DELE (credita o escudo)");

            // o PRECO: quebra na 3a devolucao (vidro alto: a HUD/sfx recebem)
            arena.Remove(costas);
            arena.Add(Projetil.Lancar(bot, new Vector3(0.3f, 1.4f, 2f), Vector3.back, Elemento.Raio));
            arena.Add(Projetil.Lancar(bot, new Vector3(-0.3f, 1.4f, 1.8f), Vector3.back, Elemento.Agua));
            k.Tick(0.02f);
            Assert.IsNull(ilu.EspelhoAtivo, "a 3a devolucao QUEBRA o espelho");
            Assert.IsTrue(TemEstado(Ilusionista.ESPELHO_QUEBRADO, true), "o vidro quebrando se ouve");
            Projetil quarto = Projetil.Lancar(bot, new Vector3(0f, 1.4f, 2f), Vector3.back, Elemento.Fogo);
            arena.Add(quarto);
            k.Tick(0.02f);
            Assert.IsTrue(arena.Contains(quarto), "quebrado, nao devolve mais nada");

            // e dura so' 2s
            k.Tick(0.1f);
            Assert.IsTrue(k.UsarTatica());
            Andar(k, t["duracao"] + 0.1f);
            Assert.IsNull(ilu.EspelhoAtivo, "fixo por 2s");
        }

        [Test]
        public void Ilusionista_BaileDeEspelhos_ReflexosEspelhados_InvisivelQueQuebraAoConjurar_EARevelacao()
        {
            var p = new FakeConjurador("ilusionista", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("08-ilusionista", p);
            var ilu = (Ilusionista)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var arena = new List<Projetil>();
            k.ProjeteisVivos = () => arena;
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            Assert.AreEqual(k.Dados.Telegrafia, (float)_teles[0][2], 0.001f, "o acorde de vidro: 1,8s de aviso");
            Andar(k, k.Dados.Telegrafia - 0.2f);
            Assert.IsNull(ilu.BaileAtivo, "nada no aviso");
            Assert.IsFalse(ilu.Invisivel);
            Andar(k, 0.25f);
            Ilusionista.Baile b = ilu.BaileAtivo;
            Assert.IsNotNull(b, "o Baile comeca DEPOIS do aviso");
            Assert.AreEqual((int)s["reflexos"], k.Visuais.Count(v => v.Tipo == "ilusionista_reflexo"), "5 reflexos na cena");
            Assert.IsTrue(ilu.Invisivel && TemEstado(Ilusionista.INVISIVEL, true), "e ele some");
            Assert.IsTrue(p.Estados.ContainsKey(Ilusionista.INVISIVEL), "o CORPO recebe o estado invisivel");
            Assert.AreEqual(s["raio"], Vector3.Distance(b.Pos[0], Vector3.zero), 0.001f, "a roda a 3m dele");

            // ESPELHAM o passo: de lado segue, na direcao do reflexo INVERTE (o passo trocado)
            p.Pos = new Vector3(1f, 0f, 0f);
            k.Tick(0.01f);
            Assert.AreEqual(new Vector3(1f, 0f, 3f), b.Pos[0], "passo de lado: o reflexo da frente vai junto");
            p.Pos = new Vector3(0f, 0f, 1f);
            k.Tick(0.01f);
            Assert.AreEqual(new Vector3(0f, 0f, 2f), b.Pos[0], "passo na direcao dele: o reflexo VEM ao encontro (invertido)");

            // reflexo nao fere quem esta' nele; tiro QUEBRA o reflexo e o flash ofusca so' de perto
            var parado = new FakeEntidade("parado", b.Pos[4]);
            var perto = new FakeConjurador("perto", b.Pos[2] + new Vector3(2f, 0f, 0f), false);
            var longe = new FakeConjurador("longe", b.Pos[3] + new Vector3(20f, 0f, 0f), false);
            p.Arena.Add(p); p.Arena.Add(parado); p.Arena.Add(perto); p.Arena.Add(longe);
            float ehp = Ehp(parado);
            arena.Add(Projetil.Lancar(perto, b.Pos[2] + Vector3.up, Vector3.left, Elemento.Fogo));
            arena.Add(Projetil.Lancar(longe, b.Pos[3] + Vector3.up, Vector3.left, Elemento.Fogo));
            k.Tick(0.01f);
            Assert.AreEqual(0, arena.Count, "o tiro morre no reflexo");
            Assert.IsFalse(b.Inteiro[2] || b.Inteiro[3], "e o reflexo QUEBRA");
            Assert.IsTrue(perto.Estados.ContainsKey("silencio"), "quem quebrou DE PERTO fica ofuscado (sem conjurar)");
            Assert.AreEqual(s["ofusca"], perto.Estados["silencio"], 0.001f, "por 0,5s");
            Assert.IsFalse(longe.Estados.ContainsKey("silencio"), "de longe nao paga nada");
            Andar(k, 0.5f);
            Assert.AreEqual(ehp, Ehp(parado), 0.001f, "reflexo NAO causa dano");

            // invisivel QUEBRA ao conjurar
            Assert.IsTrue(ilu.Invisivel, "2,5s de invisivel ainda correndo");
            k.NotificarAtaque();
            k.Tick(0.05f);
            Assert.IsFalse(ilu.Invisivel, "atacou: aparece");
            Assert.IsTrue(TemEstado(Ilusionista.INVISIVEL, false));

            // no fim quebram em sequencia e a ULTIMA nota revela onde ele esta'
            Andar(k, s["duracao"] + s["reflexos"] * Ilusionista.NOTA + 0.2f);
            Assert.IsNull(ilu.BaileAtivo, "o Baile acaba");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == Ilusionista.REVELADO && v.Alvo == p), "a posicao REAL dele revelada");
            Assert.IsTrue(TemEstado(Ilusionista.REVELADO, true), "e a HUD mostra REVELADO");
        }

        [Test]
        public void Ilusionista_TruqueDeFuga_DerrubadoSomeEmCacosEDeixaOReflexoCaido_SoloMorre()
        {
            var p = new FakeConjurador("ilusionista", Vector3.zero);
            var k = new KitRunner("08-ilusionista", p);
            var ilu = (Ilusionista)k.Impl;
            Dictionary<string, float> pas = k.Dados.Passiva;
            p.Arena.Add(p); p.Arena.Add(new FakeConjurador("aliado", new Vector3(30f, 0f, 0f)));
            Esquadrao(p.Arena);
            k.Tick(0.05f);
            Assert.IsFalse(ilu.Invisivel);
            Combat.AplicarDano(p, 500f, Elemento.Fogo, null);
            Assert.IsTrue(Derrubado.Esta(p));
            k.Tick(0.05f);
            Assert.IsTrue(ilu.Invisivel, "derrubado: quebra em cacos e SOME");
            Assert.AreEqual(pas["invisivel"], p.Estados[Ilusionista.INVISIVEL], 0.001f, "por 3s");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "ilusionista_caido"), "o reflexo CAIDO fica no lugar");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "ilusionista_cacos"), "os cacos de luz");
            Andar(k, pas["invisivel"] + 0.1f);
            Assert.IsFalse(ilu.Invisivel, "3s e volta a ser visto");

            // SOLO: sem esquadrao ninguem cai — morre, e a passiva nao tem vez
            Derrubado.Reset();
            var solo = new FakeConjurador("solo", Vector3.zero);
            var k2 = new KitRunner("08-ilusionista", solo);
            solo.Arena.Add(solo);
            Esquadrao(solo.Arena);
            Combat.AplicarDano(solo, 500f, Elemento.Fogo, null);
            k2.Tick(0.05f);
            Assert.IsFalse(solo.Vital.Viva, "solo morre direto");
            Assert.IsFalse(((Ilusionista)k2.Impl).Invisivel);
        }

        // ==================================================================== VEX

        [Test]
        public void Vex_Frascos_ArcoPocaInerteArmaEm1s_PisarDetonaNuvemBaixaELenta_Ate6()
        {
            var p = new FakeConjurador("vex", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("09-vex", p, Rapida("09-vex"));
            var vex = (Vex)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            Assert.IsTrue(k.UsarTatica());
            Vex.Frasco f = vex.Frascos[0];
            Assert.IsFalse(f.Pousou, "o frasco VOA em arco (tempo de viagem)");
            k.Tick(t["voo"] * 0.5f);
            Assert.Greater(f.Pos.y, Pawn.ALTURA_MAO, "no meio do arco ele sobe");
            Andar(k, t["voo"]);
            Assert.IsTrue(f.Pousou, "pousa");
            Assert.AreEqual(new Vector3(0f, 0f, t["alcance"]), f.Para, "a 8m na mira");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "vex_poca"), "vira poca no chao");

            var inimigo = new FakeEntidade("bot", f.Para);
            var aliado = new FakeConjurador("aliado", f.Para + new Vector3(1f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(inimigo); p.Arena.Add(aliado);
            Andar(k, 0.3f);
            Assert.AreEqual(0, vex.Nuvens.Count, "INERTE: pisar antes de armar nao faz nada");
            Andar(k, t["arma"]);
            Assert.AreEqual(1, vex.Nuvens.Count, "armada, o inimigo pisou: a nuvem estoura");
            Assert.AreEqual(0, vex.Frascos.Count, "o frasco foi gasto");
            float hp = inimigo.Vital.Hp, esc = inimigo.Vital.Escudo, hpAliado = aliado.Vital.Hp;
            Andar(k, 1f);
            float dano = hp - inimigo.Vital.Hp;
            Assert.Greater(dano, 0f, "a nuvem fere");
            Assert.LessOrEqual(dano, t["dps"] * 1.3f, "dano BAIXO (nega area, nao mata)");
            Assert.AreEqual(esc, inimigo.Vital.Escudo, 0.001f, "e' DoT: direto na vida");
            Assert.AreEqual(t["lentidao"], Efeitos.De(inimigo).StatusMult, 0.001f, "e deixa lento");
            Assert.AreEqual(hpAliado, aliado.Vital.Hp, 0.001f, "aliado dentro da nuvem nao apanha");
            Assert.AreEqual(1f, Efeitos.De(aliado).StatusMult, 0.001f, "nem fica lento");
            Assert.Greater(p.Vital.DanoCausado, 0f, "o dano da nuvem credita o escudo dele");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "vex_contorno" && v.Alvo == inimigo), "Olhos do Miasma: contorno verde em quem esta' na nevoa");
            Assert.IsFalse(k.Visuais.Any(v => v.Tipo == "vex_contorno" && v.Alvo == aliado), "so' em inimigo");
            Andar(k, t["nuvem_dur"]);
            Assert.AreEqual(0, vex.Nuvens.Count, "a nuvem dura 4s");

            p.Arena.Remove(inimigo);
            for (int i = 0; i < (int)t["max_frascos"] + 2; i++) { k.Tick(0.1f); Assert.IsTrue(k.UsarTatica()); }
            Assert.AreEqual((int)t["max_frascos"], vex.Frascos.Count, "no maximo 6 frascos (o 7o leva o mais velho)");
        }

        [Test]
        public void Vex_Limitadores_TiroNaPocaDetonaLonge_AguaNada_VentoDispersa_FogoConsomeEm2s()
        {
            var p = new FakeConjurador("vex", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("09-vex", p, Rapida("09-vex"));
            var vex = (Vex)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            k.UsarTatica();
            Andar(k, t["voo"] + 0.05f);
            Vector3 poca = vex.Frascos[0].Para;
            Bus.EmitTerrainHit(Elemento.Terra, poca + new Vector3(0.6f, 0f, 0f), false);
            Assert.AreEqual(0, vex.Frascos.Count, "um TIRO na poca a detona a distancia");
            Assert.AreEqual(1, vex.Nuvens.Count, "(desperdicada: ninguem dentro)");
            Vex.Nuvem n = vex.Nuvens[0];
            float antes = n.Restante;
            Bus.EmitTerrainHit(Elemento.Agua, n.Centro, false);
            Assert.AreEqual(antes, n.Restante, 0.001f, "agua nao faz nada na nevoa");
            Bus.EmitTerrainHit(Elemento.Vento, n.Centro + new Vector3(2f, 0f, 0f), false);
            k.Tick(0.05f);
            Assert.AreEqual(0, vex.Nuvens.Count, "VENTO dispersa (§14)");

            k.UsarTatica();
            Andar(k, t["voo"] + 0.05f);
            Bus.EmitTerrainHit(Elemento.Fogo, vex.Frascos[0].Para, false);
            Assert.AreEqual(1, vex.Nuvens.Count, "o tiro de fogo detona a poca...");
            Assert.IsTrue(vex.Nuvens[0].Queimando, "...e INCENDEIA a nuvem");
            Assert.LessOrEqual(vex.Nuvens[0].Restante, t["fogo_consome"], "fogo consome em 2s");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "vex_fogo"), "o fogo na nevoa se ve'");
            Andar(k, t["fogo_consome"] + 0.1f);
            Assert.AreEqual(0, vex.Nuvens.Count, "queimou em 2s (sem fogo seriam 4s)");
        }

        [Test]
        public void Vex_GrandeObra_Telegrafa3s_NevoaLentaSelaACuraDeTodos_EAcabaOfegante()
        {
            var p = new FakeConjurador("vex", Vector3.zero);
            var k = new KitRunner("09-vex", p);
            var vex = (Vex)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var aliado = new FakeConjurador("aliado", new Vector3(4f, 0f, 0f));
            var inimigo = new FakeEntidade("bot", new Vector3(6f, 0f, 0f));
            var fora = new FakeEntidade("longe", new Vector3(30f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(aliado); p.Arena.Add(inimigo); p.Arena.Add(fora);
            foreach (IEntidade e in p.Arena) e.Vital.Hp = 50f;

            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            Assert.AreEqual(s["raio"], LeituraDosKits.RaioDoAviso("09-vex"), 0.001f, "o anel do aviso tem o tamanho da nevoa");
            Andar(k, k.Dados.Telegrafia - 0.2f);
            Assert.IsNull(vex.Obra, "3s de fole inflando: nada ainda");
            Andar(k, 0.3f);
            Assert.IsNotNull(vex.Obra, "a Grande Obra DEPOIS do aviso");
            Assert.IsTrue(TemEstado(Vex.GRANDE_OBRA, true));
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "vex_obra" && Mathf.Approximately(v.Raio, s["raio"])), "a nevoa de 12m se ve'");
            Assert.AreEqual(s["lentidao"], Efeitos.De(inimigo).StatusMult, 0.001f, "desacelera o inimigo");
            Assert.AreEqual(1f, Efeitos.De(aliado).StatusMult, 0.001f, "o aliado nao fica lento");

            // o SELO vale para TODO MUNDO dentro — ele, o aliado e o inimigo; fora, a cura vale
            float hpInimigo = inimigo.Vital.Hp;
            p.Vital.Curar(10f); aliado.Vital.Curar(10f); inimigo.Vital.Curar(10f); fora.Vital.Curar(10f);
            k.Tick(0.02f);
            Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "nem ELE se cura na Grande Obra (dois gumes)");
            Assert.AreEqual(50f, aliado.Vital.Hp, 0.001f, "nem o time dele");
            Assert.LessOrEqual(inimigo.Vital.Hp, hpInimigo + 0.001f, "nem o inimigo");
            Assert.AreEqual(60f, fora.Vital.Hp, 0.001f, "fora da nevoa a cura vale");

            // o PRECO: acaba ofegante — 2s sem correr
            Andar(k, s["duracao"]);
            Assert.IsNull(vex.Obra, "12s e acabou");
            Assert.IsTrue(k.EstadoAtivo(Vex.OFEGANTE) && TemEstado(Vex.OFEGANTE, true), "o fole esvazia");
            Assert.AreEqual(s["ofegante_vel"], Efeitos.De(p).StatusMult, 0.001f, "2s sem correr");
            p.Vital.Curar(10f);
            k.Tick(0.02f);
            Assert.AreEqual(60f, p.Vital.Hp, 0.001f, "acabou a nevoa, o selo solta");
        }

        // ================================================================= AELION

        [Test]
        public void Aelion_Flecha_CarregaLentaEBrilhando_VoaRapida_DePertoRendeMenos_EMarcaAlem40m()
        {
            var p = new FakeConjurador("aelion", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("11-aelion", p, Rapida("11-aelion"));
            var ae = (Aelion)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            var longe = new FakeEntidade("longe", new Vector3(0f, 0f, 45f));
            longe.Vital.Escudo = 0f;
            p.Arena.Add(p); p.Arena.Add(longe);
            Assert.IsTrue(k.UsarTatica());
            Assert.IsTrue(ae.Carregando && TemEstado(Aelion.CARREGANDO, true), "puxa a corda (brilho: a HUD e a casca sabem)");
            Assert.AreEqual(t["carga_vel"], Efeitos.De(p).StatusMult, 0.001f, "o PRECO: 40% mais lento carregando");
            Andar(k, t["carga"] - 0.1f, 0.02f);
            Assert.AreEqual(0, ae.Flechas.Count, "carregando nao ha' flecha");
            Andar(k, 0.12f, 0.02f);
            Assert.AreEqual(1, ae.Flechas.Count, "cheia em 1,5s ela SAI sozinha");
            Assert.IsTrue(TemEstado(Aelion.CARREGANDO, false));
            Andar(k, 0.3f, 0.02f);
            Assert.AreEqual(100f, longe.Vital.Hp, 0.001f, "voa: 45m nao sao instantaneos (§4.1)");
            Andar(k, 0.5f, 0.02f);
            Assert.AreEqual(100f - t["dano"], longe.Vital.Hp, 0.01f, "de longe, dano cheio");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "aelion_estilhaco"), "estilhaco de vidro estelar no impacto");
            Assert.IsTrue(_thits.Exists(e => (Elemento)e[0] == Aelion.EL), "o impacto publica no terreno (§14)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "aelion_marca" && v.Alvo == longe), "Olhar do Crepusculo: acerto a +40m MARCA");

            // de PERTO rende menos (encostar e' a contra-jogada)
            var perto = new FakeEntidade("perto", new Vector3(0f, 0f, 5f));
            perto.Vital.Escudo = 0f;
            p.Arena.Remove(longe); p.Arena.Add(perto);
            k.Tick(0.1f);
            Assert.IsTrue(k.UsarTatica());
            Andar(k, t["carga"] + 0.3f, 0.02f);
            float dano = 100f - perto.Vital.Hp;
            Assert.Greater(dano, 0f, "acertou de perto");
            Assert.Less(dano, t["dano"] * 0.7f, "de perto a flecha rende pouco");
            Assert.GreaterOrEqual(dano, t["dano"] * t["dano_perto"] - 0.01f, "mas nunca menos que a metade");
            Assert.IsFalse(k.Visuais.Any(v => v.Tipo == "aelion_marca" && v.Alvo == perto), "de perto nao marca");
        }

        [Test]
        public void Aelion_Flecha_AtravessaUmMuroFino_ParaNoSegundo()
        {
            var m = new Partida(new FakeRelevo());
            try
            {
                m.Iniciar(5, 0, true, Vector3.zero);
                var t = new TerrenoReativo(new FakeRelevo(), 60f, 7, (cx, cz) => TipoCelula.Chao);
                m.Terreno = t;
                t.Reagir(Elemento.Terra, new Vector3(0f, 0f, 10f), false);
                t.Reagir(Elemento.Terra, new Vector3(0f, 0f, 22f), false);
                Assert.IsTrue(t.BloqueiaTiro(new Vector3(0f, 1.4f, 10f)) && t.BloqueiaTiro(new Vector3(0f, 1.4f, 22f)), "dois muros finos na linha");

                var p = new FakeConjurador("aelion", Vector3.zero) { DirecaoDaMira = Vector3.forward };
                var k = new KitRunner("11-aelion", p, Rapida("11-aelion"));
                var ae = (Aelion)k.Impl;
                var entre = new FakeEntidade("entre", new Vector3(0f, 0f, 16f));
                var atras = new FakeEntidade("atras", new Vector3(0f, 0f, 30f));
                p.Arena.Add(p); p.Arena.Add(entre); p.Arena.Add(atras);
                float ehpEntre = Ehp(entre);
                k.UsarTatica();
                Andar(k, k.Dados.Tatica["carga"] + 0.5f, 0.02f);
                Assert.Less(Ehp(entre), ehpEntre, "a flecha ATRAVESSA o 1o muro e acerta quem esta' atras dele");

                entre.Pos = new Vector3(15f, 0f, 16f);   // sai da linha
                k.Tick(0.1f);
                Assert.IsTrue(k.UsarTatica());
                Andar(k, k.Dados.Tatica["carga"] + 0.1f, 0.02f);
                Aelion.Flecha f = ae.Flechas[ae.Flechas.Count - 1];
                Andar(k, 0.5f, 0.02f);
                Assert.IsFalse(f.Voando, "o 2o muro PARA a flecha");
                Assert.AreEqual(1, f.Muros, "atravessou so' um");
                Assert.That(f.Pos.z, Is.InRange(21f, 23f), "parou no 2o muro");
                Assert.AreEqual(100f + 50f, Ehp(atras), 0.001f, "quem esta' atras dos dois muros esta' salvo");
            }
            finally { m.Encerrar(); }
        }

        [Test]
        public void Aelion_Chuva_Telegrafa3sNaFaixa_TresOndasSoNaFaixa_EArcoEsfria()
        {
            var p = new FakeConjurador("aelion", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("11-aelion", p);
            var ae = (Aelion)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var naFaixa = new FakeEntidade("naFaixa", new Vector3(0f, 0f, 15f));
            var aoLado = new FakeEntidade("aoLado", new Vector3(3f, 0f, 15f));      // largura 3: meia faixa de 1,5m
            var colado = new FakeEntidade("colado", new Vector3(0f, 0f, 2f));       // a faixa comeca a 4m dele
            foreach (FakeEntidade e in new[] { naFaixa, aoLado, colado }) { e.Vital.Escudo = 0f; p.Arena.Add(e); }
            p.Arena.Add(p);

            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            Assert.AreEqual(k.Dados.Telegrafia, (float)_teles[0][2], 0.001f, "3s: o risco dourado ao ceu");
            k.Tick(0.05f);
            EfeitoVisual faixa = k.Visuais.FirstOrDefault(v => v.Tipo == "aelion_aviso");
            Assert.IsNotNull(faixa, "a faixa estreita AVISADA no chao");
            Assert.AreEqual(new Vector3(0f, 0f, s["inicio"]), faixa.Pos);
            Assert.AreEqual(new Vector3(0f, 0f, s["inicio"] + s["comprimento"]), faixa.Pos2);
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "aelion_risco"), "e a flecha ao ceu que todos veem");
            p.DirecaoDaMira = Vector3.right;   // virar depois de avisar nao move a faixa
            Andar(k, k.Dados.Telegrafia - 0.3f);
            Assert.IsNull(ae.ChuvaAtiva, "nada cai durante o aviso");
            Assert.AreEqual(100f, naFaixa.Vital.Hp, 0.001f);
            Andar(k, 0.3f);
            Assert.AreEqual(100f - s["dano"], naFaixa.Vital.Hp, 0.01f, "a 1a onda cai na faixa avisada");
            Andar(k, s["intervalo"] * (s["ondas"] - 1f) + 0.05f);
            Assert.AreEqual(100f - s["dano"] * s["ondas"], naFaixa.Vital.Hp, 0.01f, "3 ondas");
            Assert.AreEqual(100f, aoLado.Vital.Hp, 0.001f, "a faixa e' ESTREITA: um passo de lado salva");
            Assert.AreEqual(100f, colado.Vital.Hp, 0.001f, "colado nele nao pega (encostar e' o counter)");
            Assert.IsNull(ae.ChuvaAtiva, "acabou");
            Assert.IsTrue(k.EstadoAtivo(Aelion.ARCO_FRIO) && TemEstado(Aelion.ARCO_FRIO, true), "o arco esfria");
            Assert.IsFalse(k.ProntoTatica, "o PRECO: 2s sem a tatica");
            Assert.Greater(k.TaticaCd, s["esfria_dur"] - 0.2f);
        }

        [Test]
        public void Aelion_OlharDoCrepusculo_SoAcertoDeleAlemDe40m_Marca3s()
        {
            var p = new FakeConjurador("aelion", Vector3.zero);
            var k = new KitRunner("11-aelion", p);
            Dictionary<string, float> pas = k.Dados.Passiva;
            var longe = new FakeEntidade("longe", new Vector3(0f, 0f, 50f));
            var perto = new FakeEntidade("perto", new Vector3(0f, 0f, 30f));
            var outro = new FakeEntidade("outro", new Vector3(0f, 0f, 60f));
            k.Tick(0.05f);
            Combat.AplicarDano(longe, 5f, Elemento.Fogo, p);
            Combat.AplicarDano(perto, 5f, Elemento.Fogo, p);
            Combat.AplicarDano(outro, 5f, Elemento.Fogo, perto);   // dano de OUTRO a +40m dele nao conta
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "aelion_marca" && v.Alvo == longe), "acerto dele a 50m MARCA (ataque basico tambem)");
            Assert.IsFalse(k.Visuais.Any(v => v.Tipo == "aelion_marca" && v.Alvo == perto), "a 30m nao");
            Assert.IsFalse(k.Visuais.Any(v => v.Tipo == "aelion_marca" && v.Alvo == outro), "so' o acerto DELE");
            Andar(k, pas["marca_dur"] + 0.1f);
            Assert.IsFalse(k.Visuais.Any(v => v.Tipo == "aelion_marca"), "a marca dura 3s");
        }
    }
}
