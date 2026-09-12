using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>Um conjurador sem cena: o minimo do IConjurador + o que a arena responde.</summary>
    public sealed class FakeConjurador : IConjurador
    {
        public string Nome { get; set; }
        public Vector3 Pos { get; set; }
        public bool EhPlayer { get; set; }
        public Vitalidade Vital { get; set; }
        public float Mana { get; set; } = 50f;
        public Vector3 DirecaoDaMira { get; set; } = Vector3.forward;
        public bool NoChao { get; set; } = true;
        /// <summary>Estados que o corpo recebeu (nome -> dur).</summary>
        public readonly Dictionary<string, float> Estados = new Dictionary<string, float>();
        /// <summary>A arena: quem AlvosNoRaio devolve.</summary>
        public List<IEntidade> Arena = new List<IEntidade>();

        public FakeConjurador(string nome, Vector3 pos, bool ehPlayer = true)
        {
            Nome = nome; Pos = pos; EhPlayer = ehPlayer;
            Vital = new Vitalidade(Balance.Player.Hp);
        }

        public void AplicarEstado(string nome, float dur) => Estados[nome] = dur;
        public Func<Vector3, float, IEntidade[]> AlvosNoRaio => (p, r) => Arena.ToArray();
    }

    /// <summary>
    /// Os MESMOS invariantes de mobile-godot/godot/gameplay/selftest_kits.gd: §4.2 economia (tatica = cooldown,
    /// suprema = carga, nada de mana), §4.3 telegrafia grampeada ANTES do efeito, e os limitadores dos 3 kits.
    /// </summary>
    public class GameplayKitsTests
    {
        private List<object[]> _cds, _teles, _states, _bounds, _thits;
        private int _disparos;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset();
            KitRunner.DpsDoTerreno = null;
            _cds = new List<object[]>(); _teles = new List<object[]>(); _states = new List<object[]>();
            _bounds = new List<object[]>(); _thits = new List<object[]>(); _disparos = 0;
            Bus.KitCooldown += (t, r, tot) => _cds.Add(new object[] { t, r, tot });
            Bus.KitTelegraph += (s, t, d, p) => _teles.Add(new object[] { s, t, d });
            Bus.KitState += (n, on) => _states.Add(new object[] { n, on });
            Bus.KitBound += (s, i) => _bounds.Add(new object[] { s, i });
            Bus.TerrainHit += (e, p, s) => _thits.Add(new object[] { e, p, s });
            Bus.Disparo += (p, pos) => _disparos++;
        }

        [TearDown]
        public void TearDown() { KitRunner.DpsDoTerreno = null; Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        // ---------------------------------------------------------------- apoio

        private static void Andar(KitRunner k, float total, float dt = 0.1f)
        {
            int n = Mathf.CeilToInt(total / dt);
            for (int i = 0; i < n; i++) k.Tick(dt);
        }

        /// <summary>Enche a carga na hora pelo canal do DANO (1000 x CargaPorDano = 150%).</summary>
        private static void Carregar(KitRunner k) => k.Tick(0.01f, 1000f);

        private bool TemEstado(string nome, bool ligado)
        {
            foreach (object[] e in _states) if ((string)e[0] == nome && (bool)e[1] == ligado) return true;
            return false;
        }

        private int Cds(string tipo, Func<float, bool> restante)
        {
            int n = 0;
            foreach (object[] e in _cds) if ((string)e[0] == tipo && restante((float)e[1])) n++;
            return n;
        }

        // ---------------------------------------------------------------- testes

        [Test]
        public void Registro_TresImplementados_SlugSemKit_KitBoundFalse_EUsarNaoFazNada()
        {
            Assert.AreEqual(3, KitRunner.Registro.Count);
            foreach (string s in new[] { "01-pyra", "03-veu", "10-tessa" })
                Assert.IsTrue(KitRunner.Registro.ContainsKey(s) && Kits.De(s).Implementado, "registrado e marcado: " + s);
            var vex = new FakeConjurador("vex", Vector3.zero);
            var k = new KitRunner("09-vex", vex);
            Assert.AreEqual(1, _bounds.Count);
            Assert.AreEqual("09-vex", _bounds[0][0]); Assert.IsFalse((bool)_bounds[0][1], "KitBound(slug, false): botao apagado");
            Assert.IsNull(k.Impl);
            Assert.IsFalse(k.ProntoTatica); Assert.IsFalse(k.UsarTatica(), "mago sem kit nao tem tatica");
            Carregar(k);
            Assert.IsFalse(k.UsarSuprema(), "mago sem kit nao tem suprema");
            Assert.IsTrue(k.PodeConjurar, "mago sem kit ATACA normalmente");
            Assert.AreEqual(0f, k.CargaSuprema, "sem kit a barra nem enche (botao apagado)");
            Assert.AreEqual(0, _cds.Count, "e a HUD nao recebe cooldown de botao que nao existe"); Assert.AreEqual(0, _teles.Count);
        }

        [Test]
        public void Economia_TaticaPagaCooldown_NuncaMana_EAvisaSoNaBorda()
        {
            var p = new FakeConjurador("pyra", Vector3.zero);
            var k = new KitRunner("01-pyra", p);
            Assert.IsTrue((bool)_bounds[0][1], "KitBound anuncia o mago COM kit");
            float mana0 = p.Mana;
            Assert.IsTrue(k.ProntoTatica, "tatica pronta no spawn");
            Assert.AreEqual(0f, k.CargaSuprema, "suprema COMECA em 0%");
            Assert.AreEqual(1f, k.FracSuprema, 0.001f);
            Assert.IsTrue(k.UsarTatica(), "tatica dispara");
            Assert.AreEqual(mana0, p.Mana, "TATICA NAO consome mana");
            Assert.AreEqual(1, Cds("tatica", r => r > 0f), "KitCooldown avisa no uso");
            Assert.AreEqual(k.Dados.TaticaCd, (float)_cds[0][2], 0.001f, "total = TaticaCd do mago");
            Assert.IsFalse(k.UsarTatica(), "cooldown BLOQUEIA o reuso");
            Assert.AreEqual(1f, k.FracTatica, 0.001f);
            Andar(k, k.Dados.TaticaCd * 0.5f);
            Assert.That(k.FracTatica, Is.InRange(0.4f, 0.6f), "o cooldown CONTA");
            Assert.IsFalse(k.ProntoTatica);
            Assert.AreEqual(0, Cds("tatica", r => r <= 0f), "nada de aviso por frame no meio do cooldown");
            Andar(k, k.Dados.TaticaCd * 0.5f + 0.2f);
            Assert.IsTrue(k.ProntoTatica, "volta quando o cooldown zera (respeita TaticaCd)");
            Assert.AreEqual(1, Cds("tatica", r => r <= 0f), "KitCooldown 'pronta' UMA vez: so' na BORDA");
            Andar(k, 3f);
            Assert.AreEqual(1, Cds("tatica", r => r <= 0f), "pronta continua sem repetir");
        }

        [Test]
        public void Suprema_SoDisparaA100_CargaSobeComTempoEComDano_UsarZera()
        {
            var p = new FakeConjurador("pyra", Vector3.zero);
            var k = new KitRunner("01-pyra", p);
            Assert.IsFalse(k.ProntoSuprema); Assert.IsFalse(k.UsarSuprema(), "a 0% e' recusada");
            float cargaS = k.Dados.SupremaCarga;
            Andar(k, cargaS * 0.5f);
            Assert.That(k.CargaSuprema, Is.InRange(0.45f, 0.55f), "o TEMPO enche: ~50% na metade");
            Assert.IsFalse(k.UsarSuprema(), "abaixo de 100% NAO dispara");
            float antes = k.CargaSuprema;
            k.Tick(0.01f, 100f);
            Assert.Greater(k.CargaSuprema, antes + 0.10f, "100 de dano causado adianta 15% (CargaPorDano)");
            Assert.AreEqual(0, Cds("suprema", r => true), "sem aviso antes de encher");
            Andar(k, cargaS);
            Assert.AreEqual(1f, k.CargaSuprema, 0.0001f);
            Assert.AreEqual(1, Cds("suprema", r => r <= 0f), "a borda dos 100% avisa UMA vez");
            float mana0 = p.Mana;
            Assert.IsTrue(k.UsarSuprema(), "a 100% dispara");
            Assert.AreEqual(mana0, p.Mana, "SUPREMA NAO consome mana");
            Assert.AreEqual(0f, k.CargaSuprema, "usar GASTA a carga inteira");
            Assert.IsFalse(k.UsarSuprema(), "vazia de novo, bloqueada de novo");
            Assert.AreEqual(1, Cds("suprema", r => r > 0f));
            Assert.AreEqual(cargaS, (float)_cds[_cds.Count - 1][2], 0.001f, "leva os segundos de carga para a HUD");
        }

        [Test]
        public void Suprema_NoTreino_EncheEm5s_ARegraDaPartidaChegaNoRunner()
        {
            // defeito (foto 18 de 12/09): Partida.SupremaCargaS tinha teste proprio, mas o runner lia Dados.SupremaCarga
            // direto — no treino a Pyra esperava os 50 s de partida para ver a suprema
            var m = new Partida(new FakeRelevo());
            try
            {
                m.Iniciar(5, 3, true, Vector3.zero);
                var k = new KitRunner("01-pyra", new FakeConjurador("pyra", Vector3.zero));
                Assert.AreEqual(Partida.SUPREMA_TREINO_S, k.SupremaCargaS, 1e-4f);
                Andar(k, Partida.SUPREMA_TREINO_S + 0.1f);
                Assert.IsTrue(k.ProntoSuprema, "cheia em 5 s no treino: carga=" + k.CargaSuprema);
            }
            finally { m.Encerrar(); }
        }

        [Test]
        public void Suprema_NaoCarregaNoAr_NemPorTempoNemPorDano()
        {
            var p = new FakeConjurador("pyra", Vector3.zero) { NoChao = false };
            var k = new KitRunner("01-pyra", p);
            Andar(k, 5f);
            Assert.AreEqual(0f, k.CargaSuprema, "no ar o TEMPO nao enche");
            k.Tick(0.1f, 100f);
            Assert.AreEqual(0f, k.CargaSuprema, "no ar nem DANO CAUSADO enche (o ponto unico barra os dois canais)");
            p.NoChao = true;
            Andar(k, 1f);
            Assert.Greater(k.CargaSuprema, 0f, "pousou: a carga volta a andar");
        }

        [Test]
        public void Telegrafia_AvisaAntesDoEfeito_EGrampeadaEm1a4()
        {
            var p = new FakeConjurador("pyra", Vector3.zero);
            var k = new KitRunner("01-pyra", p);
            Carregar(k);
            float mana0 = p.Mana;
            Assert.IsTrue(k.UsarSuprema());
            Assert.Greater(k.Telegrafia, 0f, "existe fase de telegrafia");
            Assert.IsFalse(k.EstadoAtivo(Pyra.BRACO_LIVRE), "o EFEITO nao acontece no toque");
            Assert.AreEqual(1, _teles.Count);
            Assert.AreEqual("suprema", _teles[0][1]);
            Assert.AreEqual(k.Dados.Telegrafia, (float)_teles[0][2], 0.001f, "KitTelegraph leva a duracao do aviso");
            Andar(k, k.Dados.Telegrafia - 0.2f);
            Assert.IsFalse(k.EstadoAtivo(Pyra.BRACO_LIVRE), "durante o aviso NADA acontece");
            Assert.AreEqual(0, _disparos, "o lanca-chamas nao cuspiu nada durante o aviso");
            Andar(k, 0.4f);
            Assert.IsTrue(k.EstadoAtivo(Pyra.BRACO_LIVRE), "o efeito comeca DEPOIS do aviso");
            Andar(k, 1f);
            Assert.GreaterOrEqual(_disparos, (int)k.Dados.Suprema["leque_tiros"], "o leque continuo dispara em leque");
            Assert.AreEqual(mana0, p.Mana, "o leque da suprema tambem NAO custa mana");

            // ANTI-VACUIDADE: ficha ruim nao burla a lei — o motor grampeia na faixa do GDD.
            Kits.KitDef ruim = Copia(Kits.De("01-pyra")); ruim.Telegrafia = 0f;
            var k2 = new KitRunner("01-pyra", new FakeConjurador("p2", Vector3.zero), ruim);
            Carregar(k2); k2.UsarSuprema();
            Assert.GreaterOrEqual(k2.Telegrafia, Kits.TelegrafiaMin, "telegrafia 0 grampeada no piso de 1s");
            Kits.KitDef longa = Copia(Kits.De("01-pyra")); longa.Telegrafia = 10f;
            var k3 = new KitRunner("01-pyra", new FakeConjurador("p3", Vector3.zero), longa);
            Carregar(k3); k3.UsarSuprema();
            Assert.LessOrEqual(k3.Telegrafia, Kits.TelegrafiaMax, "telegrafia 10 grampeada no teto de 4s");
            Assert.AreEqual(Kits.TelegrafiaMax, (float)_teles[_teles.Count - 1][2], 0.001f, "o Bus recebe o valor grampeado");
        }

        private static Kits.KitDef Copia(Kits.KitDef d)
        {
            return new Kits.KitDef
            {
                Slug = d.Slug, Nome = d.Nome, Implementado = d.Implementado, TaticaCd = d.TaticaCd,
                SupremaCarga = d.SupremaCarga, Telegrafia = d.Telegrafia, Passiva = d.Passiva, Tatica = d.Tatica, Suprema = d.Suprema,
            };
        }

        [Test]
        public void Pyra_Muralha_FereQuemAtravessa_NuncaADona_CreditaEscudo_EPassivaAcende()
        {
            var p = new FakeConjurador("pyra", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("01-pyra", p);
            var pyra = (Pyra)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            k.UsarTatica();
            Assert.AreEqual(1, pyra.BrasasVivas.Count, "a tatica ergue a muralha");
            Pyra.Brasas muro = pyra.BrasasVivas[0];
            Assert.AreEqual(t["comprimento"], Vector3.Distance(muro.A, muro.B), 0.001f, "muralha de 8m");
            Assert.AreEqual(1, k.Visuais.Count); Assert.AreEqual("muralha", k.Visuais[0].Tipo); Assert.IsTrue(k.Visuais[0].Segmento);
            var vitima = new FakeEntidade("v", muro.Centro);
            p.Arena.Add(p); p.Arena.Add(vitima);
            float ehp0 = vitima.Vital.Hp + vitima.Vital.Escudo;
            p.Pos = muro.Centro;   // a Pyra DENTRO da propria muralha
            float ehpPyra = p.Vital.Hp + p.Vital.Escudo;
            Andar(k, 0.2f);
            Assert.Less(vitima.Vital.Hp + vitima.Vital.Escudo, ehp0, "quem atravessa a muralha leva dano");
            Assert.AreEqual(ehpPyra, p.Vital.Hp + p.Vital.Escudo, 0.001f, "a muralha NAO fere a propria Pyra");
            Assert.Greater(p.Vital.DanoCausado, 0f, "o dano da tatica CREDITA a evolucao do escudo (fonte = a Pyra)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "aceso" && v.Alvo == vitima), "quem atravessa sai ACESO (rastro visivel)");
            Assert.AreEqual(k.Dados.Passiva["buff_vel"], Efeitos.De(p).StatusMult, 0.001f, "passiva: +10% atravessando chamas");
            Assert.AreEqual(0, _thits.Count, "a MURALHA nao acende o terreno (queimaria a ilha a cada 9s)");
        }

        [Test]
        public void Pyra_Limitadores_AguaApagaEMolha_VentoEmpurra3m()
        {
            var p = new FakeConjurador("pyra", Vector3.zero);
            var k = new KitRunner("01-pyra", p);
            var pyra = (Pyra)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            k.UsarTatica();
            Vector3 centro = pyra.BrasasVivas[0].Centro;
            float cdAntes = k.TaticaCd;
            Bus.EmitTerrainHit(Elemento.Agua, centro, false);
            Assert.AreEqual(0, pyra.BrasasVivas.Count, "agua APAGA a muralha (§14)");
            Assert.AreEqual(cdAntes + t["molhado_cd_extra"], k.TaticaCd, 0.001f, "braco molhado: +1s na recarga");
            Assert.IsTrue(TemEstado(Pyra.BRACO_MOLHADO, true), "a HUD fica sabendo do braco molhado");
            k.Tick(0.1f);
            Assert.AreEqual(0, k.Visuais.Count, "a muralha apagada some da cena");
            Andar(k, t["molhado_cd_extra"] + k.Dados.TaticaCd + 0.2f);
            Assert.IsTrue(k.UsarTatica());
            Pyra.Brasas muro2 = pyra.BrasasVivas[0];
            Vector3 antes = muro2.Centro;
            Bus.EmitTerrainHit(Elemento.Vento, antes + new Vector3(0f, 0f, 2f), false);
            Assert.AreEqual(t["vento_empurra"], Vector3.Distance(antes, muro2.Centro), 0.001f, "vento empurra a muralha 3m (§14)");
            Assert.AreEqual(muro2.A, muro2.Visual.Pos, "o visual acompanha a muralha empurrada");
            Bus.EmitTerrainHit(Elemento.Fogo, antes, false);
            Assert.AreEqual(1, pyra.BrasasVivas.Count, "fogo nao apaga nem empurra");
        }

        [Test]
        public void Pyra_BracoFrio_ExpiraSozinho_EPassivaDevolveFogoDoChao()
        {
            var p = new FakeConjurador("pyra", Vector3.zero);
            var k = new KitRunner("01-pyra", p);
            Dictionary<string, float> s = k.Dados.Suprema;
            Carregar(k);
            k.UsarSuprema();
            Andar(k, k.Dados.Telegrafia + s["duracao"] + 0.2f);
            Assert.IsFalse(k.EstadoAtivo(Pyra.BRACO_LIVRE), "a suprema tem duracao FIXA (6s)");
            Assert.GreaterOrEqual(k.TaticaCd, s["esfria_dur"] - 0.3f, "braco frio: 4s sem tatica");
            Assert.IsFalse(k.ProntoTatica, "tatica realmente bloqueada com o braco frio");
            Assert.IsTrue(k.EstadoAtivo(Pyra.BRACO_FRIO), "braco_frio esta' no RELOGIO de estados");
            Assert.AreEqual(s["esfria_vel"], Efeitos.De(p).StatusMult, 0.001f, "braco frio: -15% de velocidade");
            Andar(k, s["esfria_dur"] + 0.2f);
            Assert.IsFalse(k.EstadoAtivo(Pyra.BRACO_FRIO), "braco_frio EXPIRA sozinho");
            Assert.IsTrue(TemEstado(Pyra.BRACO_FRIO, false), "a HUD recebe o DESLIGAMENTO — o chip nao fica preso");

            // Coracao de Fornalha: fogo NO CHAO (TerrenoReativo.DpsEm) e' devolvido no mesmo tique.
            KitRunner.DpsDoTerreno = pos => Balance.Terrain.BurnDps;
            p.Vital.Hp = 50f;
            k.Tick(0.5f);
            Assert.AreEqual(50f + Balance.Terrain.BurnDps * 0.5f, p.Vital.Hp, 0.001f, "passiva: o fogo do chao nao fica nela");
            Assert.AreEqual(k.Dados.Passiva["buff_vel"], Efeitos.De(p).StatusMult, 0.001f, "e reacende o braco (+10%)");
            KitRunner.DpsDoTerreno = pos => Balance.Terrain.ElectrifyDps;
            p.Vital.Hp = 50f;
            k.Tick(0.5f);
            Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "chao ELETRIFICADO continua doendo nela (so' fogo e' imune)");
        }

        [Test]
        public void Veu_Entrelinha_Atravessar_Mare_EOsPrecos()
        {
            var p = new FakeConjurador("veu", Vector3.zero);
            var k = new KitRunner("03-veu", p);
            var veu = (Veu)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            k.Tick(0.1f);
            Assert.IsFalse(veu.Desfocada, "recem-caida nao desfoca (quietude se conquista)");
            Andar(k, k.Dados.Passiva["quietude"] + 0.2f);
            Assert.IsTrue(veu.Desfocada, "Entrelinha: desfoca com 4s de quietude");
            Assert.IsTrue(TemEstado(Veu.DESFOCADA, true));
            Combat.AplicarDano(p, 0.4f, Elemento.Fogo, null);
            k.Tick(0.1f);
            Assert.IsFalse(veu.Desfocada, "QUALQUER dano quebra a Entrelinha (ate' 0,4)");
            Assert.LessOrEqual(k.DesdeDano, 0.1f, "o relogio de dano zerou");
            Assert.IsTrue(TemEstado(Veu.DESFOCADA, false));
            Andar(k, k.Dados.Passiva["quietude"] + 0.2f);
            Assert.IsTrue(veu.Desfocada);
            k.NotificarAtaque(); k.Tick(0.1f);
            Assert.IsFalse(veu.Desfocada, "atacar tambem quebra");

            float mana0 = p.Mana;
            Assert.IsTrue(k.UsarTatica(), "Atravessar dispara");
            Assert.AreEqual(mana0, p.Mana, "Atravessar NAO consome mana");
            Assert.IsTrue(k.EstadoAtivo(Veu.ATIVACAO) && !k.EstadoAtivo(Veu.ESPECTRAL), "a tatica tambem avisa (0,8s)");
            Andar(k, t["ativacao"] + 0.05f);
            Assert.IsTrue(k.EstadoAtivo(Veu.ESPECTRAL), "entra no plano espectral depois da ativacao");
            Assert.IsTrue(p.Estados.ContainsKey(Veu.INTANGIVEL), "intangivel: o corpo resolve a colisao");
            Assert.Greater(Efeitos.De(p).IframesLeft, 0f, "invulneravel no plano espectral");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "eco"), "o ECO fica visivel na entrada");
            Andar(k, t["duracao"] + 0.05f);
            Assert.IsFalse(k.EstadoAtivo(Veu.ESPECTRAL));
            Assert.IsFalse(k.PodeConjurar, "sai do Atravessar SEM CONJURAR (vale para o ataque)");
            Assert.IsFalse(k.ProntoTatica, "e a tatica tambem, no silencio");
            Andar(k, t["silencio_saida"] + 0.05f);
            Assert.IsTrue(k.PodeConjurar, "o silencio tem prazo: 1s e volta");

            // Mare: o grupo (ela + aliado EhPlayer a 6m) entra junto, NINGUEM conjura, o sino toca no mundo real.
            var aliado = new FakeConjurador("aliada", new Vector3(2f, 0f, 0f));
            var inimigo = new FakeConjurador("bot", new Vector3(3f, 0f, 0f), false);
            p.Arena.Add(p); p.Arena.Add(aliado); p.Arena.Add(inimigo);
            _states.Clear();
            Dictionary<string, float> s = k.Dados.Suprema;
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            Andar(k, k.Dados.Telegrafia + 0.05f);
            Assert.IsTrue(k.EstadoAtivo(Veu.MARE), "a Mare comeca depois do aviso");
            Assert.IsFalse(k.PodeConjurar, "na Mare NINGUEM conjura");
            Assert.IsTrue(TemEstado(Veu.SINO, true), "o SINO toca no mundo real (counter sonoro)");
            Assert.IsTrue(aliado.Estados.ContainsKey(Veu.INTANGIVEL) && aliado.Estados.ContainsKey("silencio"), "o aliado vai junto e tambem nao conjura");
            Assert.AreEqual(s["buff_vel"], Efeitos.De(aliado).StatusMult, 0.001f, "o aliado fica veloz");
            Assert.IsFalse(inimigo.Estados.ContainsKey(Veu.INTANGIVEL), "inimigo perto NAO entra");
            Andar(k, s["duracao"] + 0.05f);
            Assert.IsTrue(TemEstado(Veu.SINO, false), "o sino para junto com a Mare");
        }

        [Test]
        public void Tessa_Compasso_Fio_TearMae_EOsPrecos()
        {
            var p = new FakeConjurador("tessa", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            Kits.KitDef rapida = Copia(Kits.De("10-tessa")); rapida.TaticaCd = 0.05f;   // o fio dura 20s: com 7s de cd os velhos expirariam antes do 7o
            var k = new KitRunner("10-tessa", p, rapida);
            var tessa = (Tessa)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            Dictionary<string, float> pas = k.Dados.Passiva;
            p.Vital.Escudo = 10f;
            Andar(k, 1f);
            Assert.AreEqual(10f + pas["escudo_regen"], p.Vital.Escudo, 0.001f, "Compasso Runico: regenera 4 de escudo por segundo");
            p.Vital.Escudo = p.Vital.EscudoMax;
            Andar(k, 2f);
            Assert.AreEqual(p.Vital.EscudoMax, p.Vital.Escudo, 0.001f, "a regeneracao respeita o teto do nivel");
            _states.Clear();
            p.Vital.Escudo = 0f;
            k.Tick(0.05f);
            Assert.IsTrue(TemEstado(Tessa.ESCUDO_QUEBRADO, true), "a HUD sabe que o escudo quebrou");
            Assert.AreEqual(pas["quebra_buff_vel"], Efeitos.De(p).StatusMult, 0.001f, "+15% quando o escudo quebra");

            for (int i = 0; i < (int)t["max_fios"] + 2; i++) { k.Tick(0.1f); Assert.IsTrue(k.UsarTatica(), "fio " + i); }
            Assert.AreEqual((int)t["max_fios"], tessa.Fios.Count, "no maximo 6 fios (o 7o apaga o 1o)");
            Tessa.Fio fio = tessa.Fios[0];
            Assert.AreEqual(t["comprimento"], Vector3.Distance(fio.A, fio.B), 0.001f, "o fio liga 2 pontos a 6m");
                        var vitima = new FakeEntidade("v", fio.Centro);
            p.Arena.Add(p); p.Arena.Add(vitima);
            float ehp = vitima.Vital.Hp + vitima.Vital.Escudo;
            p.Vital.DanoCausado = 0f; _thits.Clear(); _states.Clear();
            Andar(k, 0.2f);
            Assert.AreEqual((int)t["max_fios"], k.Visuais.Count(v => v.Tipo == "fio"), "a cena ve' 6 fios, nao 8");
            Assert.Less(vitima.Vital.Hp + vitima.Vital.Escudo, ehp, "tocar o fio doi");
            Assert.Greater(p.Vital.DanoCausado, 0f, "o dano do fio CREDITA a evolucao do escudo dela");
            Assert.IsTrue(_thits.Exists(e => (Elemento)e[0] == Elemento.Raio), "agua conduz: o toque publica RAIO no terreno (§14)");
            Assert.AreEqual(t["lentidao"], Efeitos.De(vitima).StatusMult, 0.001f, "o fio deixa lento (pela porta unica)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "revelado" && v.Alvo == vitima), "o fio REVELA quem tocou");
            Assert.IsTrue(TemEstado(Tessa.FIO_ZUMBIDO, true), "o fio ZUMBE a 5m (se denuncia)");
            Bus.EmitTerrainHit(Elemento.Fogo, fio.A, false);
            Assert.AreEqual(0, tessa.Fios.Count, "UM golpe na ancora derruba o fio");

            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            Andar(k, k.Dados.Telegrafia + 0.05f);
            Tessa.Tear tear = tessa.TearAtivo;
            Assert.IsNotNull(tear, "o Tear-Mae abre depois do aviso");
            Dictionary<string, float> s = k.Dados.Suprema;
            var atirador = new FakeEntidade("bot", new Vector3(20f, 0f, 0f));
            Projetil longe = Projetil.Lancar(atirador, new Vector3(4f, 1f, 0f), Vector3.left, Elemento.Fogo);
            Projetil colado = Projetil.Lancar(atirador, new Vector3(1f, 1f, 0f), Vector3.left, Elemento.Fogo);
            Projetil dela = Projetil.Lancar(p, new Vector3(4f, 1f, 0f), Vector3.right, Elemento.Raio);
            var arena = new List<Projetil> { longe, colado, dela };
            k.ProjeteisVivos = () => arena;
            int absorvidos = 0; k.AoAbsorver = pr => absorvidos++;
            p.Vital.Escudo = 0f;
            Andar(k, 0.2f);
            Assert.IsFalse(arena.Contains(longe), "o Tear engole projetil inimigo no alcance");
            Assert.IsTrue(arena.Contains(colado), "zona morta de 2m: nao engole curtissimo alcance");
            Assert.IsTrue(arena.Contains(dela), "nao engole o proprio tiro");
            Assert.AreEqual(1, absorvidos);
            Assert.That(p.Vital.Escudo, Is.InRange(s["escudo_por_projetil"], s["escudo_por_projetil"] + 1f), "o absorvido vira ESCUDO tecido (no escudo do kernel; +1 de folga do Compasso)");
            Carregar(k); k.UsarSuprema();
            Andar(k, k.Dados.Telegrafia + 0.05f);
            Assert.AreSame(tear, tessa.TearAtivo, "maximo 1 Tear-Mae por vez");
        }

        [Test]
        public void Utilitarios_DistSegmento_ERegenerarEscudoGrampeado()
        {
            Assert.AreEqual(2f, KitRunner.DistSegmento(new Vector3(0, 0, 2), new Vector3(-5, 0, 0), new Vector3(5, 0, 0)), 0.001f);
            Assert.AreEqual(4f, KitRunner.DistSegmento(new Vector3(9, 0, 0), new Vector3(-5, 0, 0), new Vector3(5, 0, 0)), 0.001f);
            var e = new FakeEntidade("e", Vector3.zero);
            e.Vital.Escudo = 45f;
            int avisos = 0; Bus.ShieldChanged += (a, s, m, n) => avisos++;
            Assert.AreEqual(e.Vital.EscudoMax, KitRunner.RegenerarEscudo(e, 100f), 0.001f, "regenerar grampeia no teto");
            Assert.AreEqual(1, avisos, "e avisa a HUD");
        }
    }
}
