using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// OS KITS DO GRUPO D (16 Fizz, 17 Sylva, 18 Basalto, 19 Noctus, 20 Pip) pelo FakeConjurador, sem cena: para cada mago o
    /// efeito da tatica, o limitador que a paga, a telegrafia + o efeito da suprema e a passiva. As leis gerais (§4.2 sem
    /// mana, §4.3 telegrafia) moram em CoreKitsTests/GameplayKitsTests; aqui so' o que e' de cada kit.
    /// O corpo do fake NAO anda: a logica mede na rota (o lance), e o teste move o fake a mao quando precisa.
    /// </summary>
    public class GameplayKitsGrupoDTests
    {
        private List<object[]> _thits, _teles;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset();
            KitRunner.DpsDoTerreno = null;
            Sylva.EhGrama = Sylva.GramaDaIlha;
            _thits = new List<object[]>();
            _teles = new List<object[]>();
            Bus.TerrainHit += (e, p, s) => _thits.Add(new object[] { e, p, s });
            Bus.KitTelegraph += (s, t, d, p) => _teles.Add(new object[] { s, t, d });
        }

        [TearDown]
        public void TearDown() { KitRunner.DpsDoTerreno = null; Sylva.EhGrama = Sylva.GramaDaIlha; Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        // ---------------------------------------------------------------- apoio

        /// <summary>Anda o relogio como a Partida: janela de DoT + o kit.</summary>
        private static void Andar(KitRunner k, float total, float dt = 0.05f)
        {
            int n = Mathf.CeilToInt(total / dt - 1e-4f);
            for (int i = 0; i < n; i++) { Combat.TickDot(dt); k.Tick(dt); }
        }

        private static void Carregar(KitRunner k) => k.Tick(0.01f, 1000f);

        private static float Ehp(IEntidade e) => e.Vital.Hp + e.Vital.Escudo;

        private static Kits.KitDef ComCd(string slug, float cd)
        {
            Kits.KitDef d = Kits.De(slug);
            return new Kits.KitDef
            {
                Slug = d.Slug, Nome = d.Nome, Implementado = d.Implementado, TaticaCd = cd,
                SupremaCarga = d.SupremaCarga, Telegrafia = d.Telegrafia, Passiva = d.Passiva, Tatica = d.Tatica, Suprema = d.Suprema,
            };
        }

        private static FakeConjurador Bot(string nome, Vector3 pos) => new FakeConjurador(nome, pos, false);

        /// <summary>A suprema: carrega, usa, e prova que o EFEITO nao sai no toque (a telegrafia do motor, na faixa 1-4 s).</summary>
        private void UsarSupremaTelegrafada(KitRunner k)
        {
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema(), k.Slug + ": a 100% a suprema dispara");
            Assert.AreEqual(1, _teles.Count, k.Slug + ": KitTelegraph avisa");
            Assert.That(k.Telegrafia, Is.InRange(Kits.TelegrafiaMin, Kits.TelegrafiaMax), k.Slug + ": aviso de 1 a 4 s");
        }

        // ---------------------------------------------------------------- as leis, por mago

        [Test]
        public void GrupoD_Registrados_NaFaixa_ENuncaPagamMana()
        {
            foreach (string s in new[] { "16-fizz", "17-sylva", "18-basalto", "19-noctus", "20-pip" })
            {
                Kits.KitDef d = Kits.De(s);
                Assert.IsTrue(d.Implementado && KitRunner.Registro.ContainsKey(s), s + " registrado e marcado");
                Assert.That(d.TaticaCd, Is.InRange(5f, 10f), s + " tatica 5-10 s");
                Assert.That(d.SupremaCarga, Is.InRange(40f, 55f), s + " carga 40-55 s");
                Assert.That(d.Telegrafia, Is.InRange(1f, 4f), s + " telegrafia 1-4 s");
                var p = new FakeConjurador(s, Vector3.zero);
                p.Arena.Add(p);
                var k = new KitRunner(s, p);
                Assert.IsNotNull(k.Impl, s + " tem kit");
                float mana0 = p.Mana;
                Assert.IsTrue(k.UsarTatica(), s + " tatica dispara");
                Assert.AreEqual(mana0, p.Mana, s + ": a TATICA nao paga mana");
                Carregar(k);
                Assert.IsTrue(k.UsarSuprema(), s + " suprema dispara");
                Assert.AreEqual(mana0, p.Mana, s + ": a SUPREMA nao paga mana");
                Andar(k, 10f);
                Assert.GreaterOrEqual(p.Mana, mana0, s + ": nada no kit GASTA a mana dela (so' o Noctus ganha)");
            }
        }

        // ---------------------------------------------------------------- 16 FIZZ

        [Test]
        public void Fizz_Torreta_ConeFixo_Fraca_Max2_EQuebra()
        {
            var p = new FakeConjurador("fizz", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("16-fizz", p, ComCd("16-fizz", 0.05f));
            var fizz = (Fizz)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual(1, fizz.Torretas.Count, "a tatica finca a torreta");
            Fizz.Torreta tor = fizz.Torretas[0];
            Assert.AreEqual(t["dist"], tor.Pos.z, 0.001f, "fincada na mira");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "fizz_torreta"), "a cena desenha a torreta");
            // um no cone (longe) e um FORA do cone mas mais perto: o cone fixo manda, nao a distancia
            var noCone = Bot("noCone", new Vector3(0f, 0f, 9f));
            var fora = Bot("fora", new Vector3(3f, 0f, 1.5f));
            p.Arena.Add(p); p.Arena.Add(noCone); p.Arena.Add(fora);
            float ehpCone = Ehp(noCone), ehpFora = Ehp(fora);
            Andar(k, 0.2f);
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "fizz_faisca"), "a faisca VIAJA desenhada (nada nasce no alvo)");
            Andar(k, 1.8f);
            Assert.Less(Ehp(noCone), ehpCone, "quem entra no cone leva faisca");
            Assert.AreEqual(ehpFora, Ehp(fora), 0.001f, "cone FIXO: ao lado da torreta ninguem leva (flanqueavel)");
            Assert.LessOrEqual(ehpCone - Ehp(noCone), t["dano"] * 1.25f * 3f, "faisca FRACA: zoneia, nao mata (<= 3 faiscas em 2 s)");
            Assert.Greater(p.Vital.DanoCausado, 0f, "o dano da torreta credita o Fizz (escudo e carga)");

            p.Pos = new Vector3(-6f, 0f, 0f);
            Assert.IsTrue(k.UsarTatica()); Andar(k, 0.1f);
            p.Pos = new Vector3(6f, 0f, 0f);
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual((int)t["max_torretas"], fizz.Torretas.Count, "no maximo 2 torretas: a 3a recolhe a mais velha");
            Assert.IsFalse(fizz.Torretas.Contains(tor), "a mais velha saiu");

            // quebra: 40 de vida — tiro INIMIGO que toca o corpo e golpe no chao perto dela
            Fizz.Torreta alvo = fizz.Torretas[0];
            var arena = new List<Projetil> { Projetil.Lancar(noCone, alvo.Pos + new Vector3(0f, 0.6f, 0.2f), Vector3.back, Elemento.Fogo) };
            k.ProjeteisVivos = () => arena;
            float vida0 = alvo.Vida;
            k.Tick(0.02f);
            Assert.AreEqual(0, arena.Count, "o tiro que TOCA a torreta para nela (engolido)");
            Assert.Less(alvo.Vida, vida0, "e fere a torreta");
            Bus.EmitTerrainHit(Elemento.Terra, alvo.Pos, false);
            Bus.EmitTerrainHit(Elemento.Terra, alvo.Pos, false);
            k.Tick(0.02f);
            Assert.IsFalse(fizz.Torretas.Contains(alvo), "40 de vida: quebrou e saiu");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "fizz_sucata"), "quebrada vira sucata na tela");
        }

        [Test]
        public void Fizz_Megabobina_ArmaNaTelegrafia_UmRaioSo_MolasTravam_EQuebrada_NaoDispara()
        {
            var p = new FakeConjurador("fizz", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("16-fizz", p);
            var fizz = (Fizz)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var perto = Bot("perto", new Vector3(0f, 0f, 8f));
            var longe = Bot("longe", new Vector3(0f, 0f, 14f));
            p.Arena.Add(p); p.Arena.Add(perto); p.Arena.Add(longe);
            Assert.AreEqual(k.Dados.Passiva["pulo_mult"], fizz.FatorDoPulo(k), "Mola: pulo 50% mais alto");
            UsarSupremaTelegrafada(k);
            Andar(k, 0.2f);
            Assert.IsNotNull(fizz.BobinaArmada, "a bobina se ARMA na telegrafia (destrutivel)");
            Assert.AreSame(perto, fizz.BobinaArmada.Alvo, "o laser marca o inimigo MAIS PERTO");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "fizz_bobina") && k.Visuais.Any(v => v.Tipo == "fizz_mira"));
            float ehpPerto = Ehp(perto), ehpLonge = Ehp(longe);
            Andar(k, k.Dados.Telegrafia - 0.4f);
            Assert.AreEqual(ehpPerto, Ehp(perto), 0.001f, "durante o aviso NADA acontece");
            Andar(k, 1f);
            Assert.GreaterOrEqual(ehpPerto - Ehp(perto), s["dano"], "UM raio devastador no mais perto");
            Assert.AreEqual(ehpLonge, Ehp(longe), 0.001f, "um alvo so', nunca area");
            Assert.IsTrue(_thits.Count(h => (Elemento)h[0] == Elemento.Raio) >= 2, "o raio publica Raio no trajeto (desliga construcoes)");
            Assert.IsTrue(k.EstadoAtivo(Fizz.MOLAS_TRAVADAS), "o preco: molas travadas");
            Assert.AreEqual(1f, fizz.FatorDoPulo(k), "sem pulo alto com as molas travadas");
            Andar(k, s["trava_dur"] + 0.1f);
            Assert.AreEqual(k.Dados.Passiva["pulo_mult"], fizz.FatorDoPulo(k), "as molas destravam sozinhas");

            // a mola e' de ALTURA (h = v^2/2g): so' no quadro da decolagem
            float vy = ApoioGrupoD.VyComMola(Balance.Player.JumpV, 1.5f);
            Assert.AreEqual(1.5f, vy * vy / (Balance.Player.JumpV * Balance.Player.JumpV), 0.001f, "o pulo sobe 1,5x mais alto");
            Assert.AreEqual(3f, ApoioGrupoD.VyComMola(3f, 1.5f), "no meio do ar a mola nao mexe");

            // QUEBRADA na carga: 60 de vida, sem raio e sem o preco
            _teles.Clear(); _thits.Clear();
            var p2 = new FakeConjurador("fizz2", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k2 = new KitRunner("16-fizz", p2);
            var fizz2 = (Fizz)k2.Impl;
            var alvo2 = Bot("alvo2", new Vector3(0f, 0f, 6f));
            p2.Arena.Add(p2); p2.Arena.Add(alvo2);
            UsarSupremaTelegrafada(k2);
            Andar(k2, 0.2f);
            Vector3 bobina = fizz2.BobinaArmada.Pos;
            for (int i = 0; i < 3; i++) Bus.EmitTerrainHit(Elemento.Terra, bobina, false);   // 3 x 27,2 > 60
            Assert.IsFalse(fizz2.BobinaArmada.Viva, "a bobina QUEBRA na carga");
            float ehp2 = Ehp(alvo2);
            Andar(k2, k2.Dados.Telegrafia + 1f);
            Assert.AreEqual(ehp2, Ehp(alvo2), 0.001f, "quebrada: a carga foi embora sem raio");
            Assert.IsFalse(k2.EstadoAtivo(Fizz.MOLAS_TRAVADAS), "sem disparo, sem o preco");
        }

        // ---------------------------------------------------------------- 17 SYLVA

        [Test]
        public void Sylva_Broto_Cura_Queima_Moita_EVinculoTransfere()
        {
            var p = new FakeConjurador("sylva", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("17-sylva", p, ComCd("17-sylva", 0.05f));
            var sylva = (Sylva)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            p.Arena.Add(p);
            p.Vital.Hp = 50f;
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual(1, sylva.Brotos.Count, "a tatica planta o broto");
            Andar(k, t["florescer"] - 0.1f);
            Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "fechado nao cura: floresce em 1 s");
            Andar(k, t["duracao"] + 0.3f);
            Assert.That(p.Vital.Hp, Is.InRange(50f + t["cura"] * t["duracao"] - 1.5f, 50f + t["cura"] * t["duracao"] + 0.5f),
                "o polen cura 5 hp/s por 6 s (ela inclusa: SOLO)");
            Assert.AreEqual(0, sylva.Brotos.Count, "o broto murcha no fim");

            // FOGO queima o broto na hora (o §14 contra ela)
            p.Vital.Hp = 50f;
            Assert.IsTrue(k.UsarTatica());
            Bus.EmitTerrainHit(Elemento.Fogo, sylva.Brotos[0].Pos, false);
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "sylva_queima"), "a queima aparece na hora");
            Andar(k, 2f);
            Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "queimado nao cura");
            // chao em chamas debaixo dele tambem
            KitRunner.DpsDoTerreno = pos => Balance.Terrain.BurnDps;
            Assert.IsTrue(k.UsarTatica());
            k.Tick(0.05f);
            Assert.AreEqual(0, sylva.Brotos.Count, "chao em chamas queima o broto");
            KitRunner.DpsDoTerreno = null;

            // GRAMA: moita de cobertura; fora dela, nao
            Sylva.EhGrama = pos => true;
            Assert.IsTrue(k.UsarTatica());
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "sylva_moita"), "em grama o broto vira moita");
            Sylva.EhGrama = pos => false;
            int moitas = k.Visuais.Count(v => v.Tipo == "sylva_moita");
            Andar(k, 0.1f);
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual(moitas, k.Visuais.Count(v => v.Tipo == "sylva_moita"), "fora da grama, moita nenhuma");

            // SEIVA COMPARTILHADA: 20% do dano do aliado VEM para ela (transfere, nao reduz). Runner limpo: broto nenhum curando.
            Assert.IsNull(sylva.Vinculado, "SOLO: ninguem para vincular");
            p = new FakeConjurador("sylva2", Vector3.zero);
            k = new KitRunner("17-sylva", p);
            sylva = (Sylva)k.Impl;
            var aliada = new FakeConjurador("aliada", new Vector3(3f, 0f, 0f));
            var bot = Bot("bot", new Vector3(30f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(aliada); p.Arena.Add(bot);
            Andar(k, 0.3f);
            Assert.AreSame(aliada, sylva.Vinculado, "vincula o aliado perto");
            float ehpA = Ehp(aliada), ehpS = Ehp(p);
            Combat.AplicarDano(aliada, 50f, Elemento.Fogo, bot);
            k.Tick(0.05f);
            float f = k.Dados.Passiva["vinculo_frac"];
            Assert.AreEqual(ehpA - 50f * (1f - f), Ehp(aliada), 0.01f, "o aliado fica so' com 80%");
            Assert.AreEqual(ehpS - 50f * f, Ehp(p), 0.01f, "e os 20% sao DELA (dano real)");
        }

        [Test]
        public void Sylva_Coracao_Telegrafado_RegeneraVidaEEscudo_AgarraQuemCruza_ESeivaGasta()
        {
            var p = new FakeConjurador("sylva", Vector3.zero);
            var k = new KitRunner("17-sylva", p);
            var sylva = (Sylva)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var dentro = Bot("dentro", new Vector3(3f, 0f, 0f));
            var aliada = new FakeConjurador("aliada", new Vector3(11f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(dentro); p.Arena.Add(aliada);
            UsarSupremaTelegrafada(k);
            Andar(k, k.Dados.Telegrafia - 0.2f);
            Assert.IsNull(sylva.CoracaoAtivo, "durante o aviso as raizes nao agem");
            Assert.AreEqual(0f, Efeitos.De(dentro).StunLeft, "ninguem agarrado no aviso");
            Andar(k, 0.3f);
            Assert.IsNotNull(sylva.CoracaoAtivo, "o Coracao abre depois do aviso");
            Assert.Greater(Efeitos.De(dentro).StunLeft, 0f, "inimigo dentro e' AGARRADO");
            Assert.LessOrEqual(Efeitos.De(dentro).StunLeft, Balance.Status.StunCap, "no teto do kernel");
            p.Vital.Hp = 50f; p.Vital.Escudo = 0f;
            Andar(k, 1f);
            Assert.That(p.Vital.Hp, Is.InRange(50f + s["cura"] * 0.7f, 50f + s["cura"] * 1.3f), "ela regenera VIDA");
            Assert.That(p.Vital.Escudo, Is.InRange(s["escudo"] * 0.7f, s["escudo"] * 1.3f), "e ESCUDO");
            Assert.AreEqual(0f, Efeitos.De(aliada).StunLeft, "aliado de fora ainda nao cruzou");
            aliada.Pos = new Vector3(5f, 0f, 0f);
            Andar(k, 0.3f);
            Assert.Greater(Efeitos.De(aliada).StunLeft, 0f, "o aliado que CRUZA a borda tambem e' agarrado (dois gumes)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "sylva_agarra" && v.Alvo == aliada), "as raizes aparecem nele");
            Andar(k, s["duracao"]);
            Assert.IsNull(sylva.CoracaoAtivo, "8 s e acaba");
            Assert.IsTrue(k.EstadoAtivo(Sylva.SEIVA_GASTA), "o preco: a seiva foi gasta");
            aliada.Pos = new Vector3(3f, 0f, 0f);
            Andar(k, 0.3f);
            Assert.IsNull(sylva.Vinculado, "seiva gasta: sem vinculo");
            Andar(k, s["seiva_dur"]);
            Assert.IsFalse(k.EstadoAtivo(Sylva.SEIVA_GASTA), "a seiva volta sozinha");
            Assert.AreSame(aliada, sylva.Vinculado, "e o vinculo volta com ela");
        }

        // ---------------------------------------------------------------- 18 BASALTO

        [Test]
        public void Basalto_Punho_OndaViaja_ConeCurto_ErguePedras_EPeleDeMontanha()
        {
            var p = new FakeConjurador("basalto", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("18-basalto", p);
            Dictionary<string, float> t = k.Dados.Tatica;
            var frente = Bot("frente", new Vector3(0f, 0f, 3f));
            var lado = Bot("lado", new Vector3(-3f, 0f, 0.5f));
            var longe = Bot("longe", new Vector3(0f, 0f, 8f));
            p.Arena.Add(p); p.Arena.Add(frente); p.Arena.Add(lado); p.Arena.Add(longe);
            float e0 = Ehp(frente), eLado = Ehp(lado), eLonge = Ehp(longe);
            Assert.IsTrue(k.UsarTatica());
            Andar(k, 0.1f);
            Assert.AreEqual(e0, Ehp(frente), 0.001f, "a onda VIAJA: aos 0,1 s ainda nao chegou a 3 m");
            Andar(k, 0.4f);
            Assert.GreaterOrEqual(e0 - Ehp(frente), t["dano"] - 0.01f, "a frente alcanca: dano decente");
            Assert.AreEqual(eLado, Ehp(lado), 0.001f, "fora do cone, nada");
            Assert.AreEqual(eLonge, Ehp(longe), 0.001f, "alcance CURTISSIMO: a 8 m, nada (kitar e' a contra-jogada)");
            Assert.AreEqual((int)t["pedras"], _thits.Count(h => (Elemento)h[0] == Elemento.Terra), "em TERRA: 3 pedras (o §14 ergue)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "basalto_onda"), "a onda aparece");

            // PELE DE MONTANHA: tiro pelas COSTAS fere 20% menos; de frente, inteiro
            var arena = new List<Projetil>();
            k.ProjeteisVivos = () => arena;
            p.Vital.Hp = p.Vital.HpMax; p.Vital.Escudo = 0f;
            arena.Add(Projetil.Lancar(frente, new Vector3(0f, 1f, -2f), Vector3.forward, Elemento.Fogo));   // vem de tras
            k.Tick(0.02f);
            arena.Clear();
            Combat.AplicarDano(p, 20f, Elemento.Fogo, frente);
            k.Tick(0.02f);
            Assert.AreEqual(p.Vital.HpMax - 20f * (1f - k.Dados.Passiva["costas_reducao"]), p.Vital.Hp, 0.01f, "pelas costas: as placas seguram 20%");
            p.Vital.Hp = p.Vital.HpMax;
            k.Tick(0.02f);
            arena.Add(Projetil.Lancar(frente, new Vector3(0f, 1f, 2f), Vector3.back, Elemento.Fogo));        // vem de frente
            k.Tick(0.02f);
            arena.Clear();
            Combat.AplicarDano(p, 20f, Elemento.Fogo, frente);
            k.Tick(0.02f);
            Assert.AreEqual(p.Vital.HpMax - 20f, p.Vital.Hp, 0.01f, "de frente: inteiro");
            // o PRECO: raio o atordoa 0,4 s a mais
            p.Vital.Hp = p.Vital.HpMax;
            arena.Add(Projetil.Lancar(frente, new Vector3(0f, 1f, 2f), Vector3.back, Elemento.Raio));
            k.Tick(0.02f);
            arena.Clear();
            Combat.AplicarDano(p, 10f, Elemento.Raio, frente);
            k.Tick(0.02f);
            Assert.GreaterOrEqual(Efeitos.De(p).StunLeft, k.Dados.Passiva["raio_atordoa"] - 0.05f, "raio atordoa a pedra runica");
        }

        [Test]
        public void Basalto_Monolito_Telegrafado_Reduz60_Reflete_Marca_NaoAnda_EEndurece()
        {
            var p = new FakeConjurador("basalto", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("18-basalto", p);
            Dictionary<string, float> s = k.Dados.Suprema;
            var bot = Bot("bot", new Vector3(0f, 0f, 4f));
            p.Arena.Add(p); p.Arena.Add(bot);
            UsarSupremaTelegrafada(k);
            k.Tick(0.05f);
            Assert.AreEqual(s["imovel"], Efeitos.De(p).StatusMult, 0.001f, "afunda no aviso: parado");
            Assert.IsFalse(k.EstadoAtivo(Basalto.MONOLITO), "o efeito nao sai no toque");
            Andar(k, k.Dados.Telegrafia + 0.05f);
            Assert.IsTrue(k.EstadoAtivo(Basalto.MONOLITO), "vira torre depois do aviso");
            Assert.IsFalse(k.PodeConjurar, "no Monolito nao conjura (ancora)");
            Efeitos.Lentificar(p, 0.9f, 3f);   // lentidao alheia nao o solta
            k.Tick(0.05f);
            Assert.AreEqual(s["imovel"], Efeitos.De(p).StatusMult, 0.001f, "nao anda");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "revelado" && v.Alvo == bot), "marca quem esta' perto");
            float eBot = Ehp(bot), eBas = Ehp(p);
            Combat.AplicarDano(p, 50f, Elemento.Fogo, bot);
            k.Tick(0.05f);
            Assert.AreEqual(eBas - 50f * (1f - s["reducao"]), Ehp(p), 0.01f, "-60% de dano");
            Andar(k, s["pulso"] + 0.05f);
            Assert.AreEqual(50f * s["reflexo"], eBot - Ehp(bot), 0.01f, "reflete 15% em estilhacos no cone");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "basalto_estilhaco"), "os estilhacos aparecem");
            Andar(k, s["duracao"]);
            Assert.IsFalse(k.EstadoAtivo(Basalto.MONOLITO), "6 s e acaba");
            Assert.IsTrue(k.EstadoAtivo(Basalto.PERNAS_DURAS), "o preco: pernas endurecidas");
            Assert.AreEqual(s["endurece_vel"], Efeitos.De(p).StatusMult, 0.001f, "2 s sem correr");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "basalto_avalanche"), "as placas caem em avalanche");
        }

        // ---------------------------------------------------------------- 19 NOCTUS

        [Test]
        public void Noctus_Mordida_Viaja_DrenaEter_Marca_EErrarSilencia()
        {
            var p = new FakeConjurador("noctus", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("19-noctus", p, ComCd("19-noctus", 0.05f));
            var noc = (Noctus)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            var bot = Bot("bot", new Vector3(0.5f, 0f, 4f));
            bot.Mana = 50f;
            p.Mana = 30f;
            p.Arena.Add(p); p.Arena.Add(bot);
            Assert.IsTrue(k.UsarTatica());
            Andar(k, 0.1f);
            Assert.AreEqual(50f, bot.Mana, 0.001f, "a investida VIAJA: aos 0,1 s nao chegou a 4 m");
            Andar(k, 0.8f);
            Assert.AreEqual(50f - t["dreno"], bot.Mana, 0.001f, "drena 20 de eter do alvo");
            Assert.AreEqual(30f + t["dreno"], p.Mana, 0.001f, "e o eter vai para ele");
            Assert.AreSame(bot, noc.Marcado, "e o MARCA");
            Assert.IsTrue(k.PodeConjurar, "acertou: sem castigo");
            Assert.AreEqual(bot.Vital.HpMax, bot.Vital.Hp, "drena ETER, nunca vida");
            // o proximo disparo do marcado custa +50%
            bot.Mana -= 10f;
            k.Tick(0.05f);
            Assert.AreEqual(30f - 10f - 10f * t["marca_extra"], bot.Mana, 0.01f, "o disparo marcado custa +50%");
            Assert.IsNull(noc.Marcado, "a marca paga uma vez e sai");

            // ERROU: 6 m secos, 1,5 s sem conjurar
            bot.Pos = new Vector3(20f, 0f, 0f);
            Andar(k, 0.1f);
            Assert.IsTrue(k.UsarTatica());
            Andar(k, ApoioGrupoD.DuracaoDoImpulso(t["alcance"]) + 0.1f);
            Assert.IsFalse(k.PodeConjurar, "errou a investida: vulneravel, sem conjurar");
            Andar(k, t["erro_dur"] + 0.1f);
            Assert.IsTrue(k.PodeConjurar, "o castigo tem prazo");
        }

        [Test]
        public void Noctus_SedeDeEter_Abate_ENevoa_Telegrafada_Intangivel_Lentidao_Faminto()
        {
            var p = new FakeConjurador("noctus", Vector3.zero);
            var k = new KitRunner("19-noctus", p);
            Dictionary<string, float> pas = k.Dados.Passiva, s = k.Dados.Suprema;
            var bot = Bot("bot", new Vector3(0.6f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(bot);
            k.Tick(0.05f);
            p.Mana = 40f;
            p.Vital.DanoCausado += 100f;
            k.Tick(0.05f);
            Assert.AreEqual(40f + 100f * pas["conversao"], p.Mana, 0.01f, "15% do dano causado volta como ETER");
            p.Vital.Hp = 50f;
            bot.Vital.Hp = 5f; bot.Vital.Escudo = 0f;
            Combat.AplicarDano(bot, 30f, Elemento.Vento, p);
            Assert.AreEqual(50f + pas["abate_vida"], p.Vital.Hp, 0.01f, "abate devolve 25 de vida");
            bot.Vital.Reset();

            UsarSupremaTelegrafada(k);
            Andar(k, k.Dados.Telegrafia - 0.2f);
            Assert.IsFalse(k.EstadoAtivo(Noctus.NEVOA), "a nevoa nao sai no toque");
            Andar(k, 0.4f);
            Assert.IsTrue(k.EstadoAtivo(Noctus.NEVOA), "vira nevoa depois do aviso");
            Assert.IsTrue(p.Estados.ContainsKey("intangivel"), "intangivel: o corpo resolve (projetil atravessa)");
            Assert.Greater(Efeitos.De(p).IframesLeft, 0f);
            Assert.IsFalse(k.PodeConjurar, "a nevoa nao conjura");
            Assert.AreEqual(s["vel_dia"], Efeitos.De(p).StatusMult, 0.001f, "a luz do dia o freia");
            Assert.AreEqual(s["lentidao"], Efeitos.De(bot).StatusMult, 0.001f, "quem ele atravessa fica lento");
            Andar(k, 0.6f);
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "noctus_rastro"), "o rastro mostra o caminho");
            Andar(k, s["duracao"]);
            Assert.IsTrue(k.EstadoAtivo(Noctus.FAMINTO), "o preco: recondensa FAMINTO");
            float m = p.Mana = 10f;
            p.Vital.DanoCausado += 100f;
            k.Tick(0.05f);
            Assert.AreEqual(m, p.Mana, 0.001f, "faminto: a passiva nao rende");
            Andar(k, s["faminto_dur"] + 0.1f);
            p.Vital.DanoCausado += 100f;
            k.Tick(0.05f);
            Assert.Greater(p.Mana, m, "e volta a render");
        }

        // ---------------------------------------------------------------- 20 PIP

        [Test]
        public void Pip_ZipZag_RotaTelegrafada_FaiscaSaltaParaOMaisPerto_EDefinhaParada()
        {
            var p = new FakeConjurador("pip", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("20-pip", p);
            var pip = (Pip)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica, pas = k.Dados.Passiva;
            var cruzado = Bot("cruzado", new Vector3(0.9f, 0f, 2f));   // no 1o dash (zig a +35 graus)
            var vizinho = Bot("vizinho", new Vector3(3.9f, 0f, 2f));   // fora da rota, perto do cruzado
            p.Arena.Add(p); p.Arena.Add(cruzado); p.Arena.Add(vizinho);
            float eC = Ehp(cruzado), eV = Ehp(vizinho);
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual((int)t["dashes"], k.Visuais.Count(v => v.Tipo == "pip_rota"), "a ROTA inteira aparece no toque");
            Assert.AreEqual(t["dash_dist"], Vector3.Distance(pip.Rota(0), pip.Rota(1)), 0.001f);
            Assert.Greater(Vector3.Dot(pip.Rota(1) - pip.Rota(0), Vector3.right), 0f, "zig para um lado...");
            Assert.Less(Vector3.Dot(pip.Rota(2) - pip.Rota(1), Vector3.right), 0f, "...zag para o outro");
            Andar(k, 0.7f);
            Assert.Less(Ehp(vizinho), eV, "a faisca SALTA do atravessado para o inimigo mais perto dele");
            Assert.AreEqual(eC, Ehp(cruzado), 0.001f, "e nao volta no atravessado");
            Andar(k, t["intervalo"] * t["dashes"]);
            Assert.AreEqual(-1, pip.DashAtual, "tres dashes e acabou");

            // NUNCA POUSAR: parada 2 s, definha 2 hp/s; andou, zera
            p.Vital.Hp = 60f;
            p.Pos = new Vector3(40f, 0f, 40f);
            Andar(k, pas["parada_s"] - 0.2f);
            Assert.AreEqual(60f, p.Vital.Hp, 0.001f, "antes dos 2 s parada, nada");
            Andar(k, 1.2f);
            Assert.That(p.Vital.Hp, Is.InRange(60f - pas["definha"] * 1.4f, 60f - pas["definha"] * 0.6f), "parada: definha 2 hp/s");
            float hp = p.Vital.Hp;
            p.Pos += new Vector3(1f, 0f, 0f);
            Andar(k, 1f);
            Assert.AreEqual(hp, p.Vital.Hp, 0.001f, "andou: o relogio da parada zera");
            bool sino = false;
            for (int i = 0; i < 30; i++) { k.Tick(0.05f); sino |= k.Visuais.Any(v => v.Tipo == "pip_sino"); }
            Assert.IsTrue(sino, "o SINO denuncia, com onda visivel");
            // voa: a agua eletrificada do chao nao a alcanca (so' ela: fogo no chao doi)
            KitRunner.DpsDoTerreno = pos => Balance.Terrain.ElectrifyDps;
            p.Pos += new Vector3(1f, 0f, 0f);
            p.Vital.Hp = 50f;
            k.Tick(0.5f);
            Assert.AreEqual(50f + Balance.Terrain.ElectrifyDps * 0.5f, p.Vital.Hp, 0.001f, "agua eletrificada: devolvida");
            KitRunner.DpsDoTerreno = pos => Balance.Terrain.BurnDps;
            p.Pos += new Vector3(1f, 0f, 0f);
            p.Vital.Hp = 50f;
            k.Tick(0.5f);
            Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "fogo no chao nao e' devolvido");
        }

        [Test]
        public void Pip_Supercelula_Telegrafada_AtiraNoMaisPerto_ForaDoRaioNao_EChoveNela()
        {
            var p = new FakeConjurador("pip", Vector3.zero);
            var k = new KitRunner("20-pip", p);
            Dictionary<string, float> s = k.Dados.Suprema;
            var perto = Bot("perto", new Vector3(0f, 0f, 5f));
            var medio = Bot("medio", new Vector3(0f, 0f, 9f));
            var fora = Bot("fora", new Vector3(0f, 0f, s["raio"] + 3f));
            p.Arena.Add(p); p.Arena.Add(perto); p.Arena.Add(medio); p.Arena.Add(fora);
            float eP = Ehp(perto), eM = Ehp(medio), eF = Ehp(fora);
            UsarSupremaTelegrafada(k);
            Andar(k, k.Dados.Telegrafia - 0.2f);
            Assert.IsFalse(k.EstadoAtivo(Pip.SUPERCELULA), "a nuvem nao sai no toque");
            Andar(k, 0.3f);
            Assert.IsTrue(k.EstadoAtivo(Pip.SUPERCELULA), "a nuvem nasce depois do aviso");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "pip_nuvem" && v.Alvo == p), "e a SEGUE");
            Andar(k, 2f);
            Assert.Less(Ehp(perto), eP, "atira no mais PERTO dela");
            Assert.LessOrEqual(eP - Ehp(perto), s["dano"] * 1.25f * (2f / s["cadencia"] + 1f), "raio FRACO");
            Assert.AreEqual(eM, Ehp(medio), 0.001f, "prioriza o mais perto (a distancia e' a contra-jogada)");
            Assert.AreEqual(eF, Ehp(fora), 0.001f, "fora do raio, nada");
            Andar(k, s["duracao"] - 1.6f);
            Assert.IsFalse(k.EstadoAtivo(Pip.SUPERCELULA), "8 s e acaba");
            Assert.IsTrue(k.EstadoAtivo(Pip.ENCHARCADA), "o preco: a nuvem chove nela");
            Assert.Greater(Efeitos.De(p).WetLeft, 0f, "encharcada: molhada CONDUZ raio");
            Assert.AreEqual(s["chuva_vel"], Efeitos.De(p).StatusMult, 0.001f, "e lenta");
        }
    }
}
