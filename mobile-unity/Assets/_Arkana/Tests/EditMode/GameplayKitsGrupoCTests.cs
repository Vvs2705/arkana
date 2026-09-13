using System;
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
    /// O GRUPO C (Umbra, Brok, Gromm, Maris) no padrao do GameplayKitsTests: por mago, o efeito da tatica, o limitador que a
    /// paga, a telegrafia + o efeito da suprema, e a passiva. As leis gerais (sem mana, 1-4 s, 5-10 s) ja' valem para eles
    /// pelo CoreKitsTests (a lista de implementados le' a ficha).
    /// </summary>
    public class GameplayKitsGrupoCTests
    {
        // os padroes das portas de casca, guardados antes de qualquer teste trocar
        private static readonly Action<IConjurador, float> _esquivaPadrao = Umbra.RecarregarEsquiva;
        private static readonly Action<IEntidade, Vector3> _empurraoPadrao = Gromm.EmpurrarCorpo;

        private List<object[]> _states, _thits;
        private List<object[]> _bounds;
        private int _disparos;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset();
            KitRunner.DpsDoTerreno = null;
            Umbra.RecarregarEsquiva = _esquivaPadrao;
            Gromm.EmpurrarCorpo = _empurraoPadrao;
            _states = new List<object[]>(); _thits = new List<object[]>(); _bounds = new List<object[]>(); _disparos = 0;
            Bus.KitState += (n, on) => _states.Add(new object[] { n, on });
            Bus.TerrainHit += (e, p, s) => _thits.Add(new object[] { e, p, s });
            Bus.KitBound += (s, i) => _bounds.Add(new object[] { s, i });
            Bus.Disparo += (p, pos) => _disparos++;
        }

        [TearDown]
        public void TearDown()
        {
            KitRunner.DpsDoTerreno = null;
            Umbra.RecarregarEsquiva = _esquivaPadrao;
            Gromm.EmpurrarCorpo = _empurraoPadrao;
            Efeitos.Reset(); Combat.Reset(); Bus.Reset();
        }

        // ---------------------------------------------------------------- apoio

        private static void Andar(KitRunner k, float total, float dt = 0.1f)
        {
            int n = Mathf.CeilToInt(total / dt);
            for (int i = 0; i < n; i++) k.Tick(dt);
        }

        /// <summary>Enche a carga na hora pelo canal do DANO (1000 x CargaPorDano = 150%).</summary>
        private static void Carregar(KitRunner k) => k.Tick(0.01f, 1000f);

        /// <summary>A Partida em miniatura: os tiros voam (sem corpo para acertar) e o kit mede depois de cada passo.</summary>
        private static void Voar(KitRunner k, List<Projetil> arena, float segundos, float dt = 1f / 60f)
        {
            int n = Mathf.CeilToInt(segundos / dt);
            for (int i = 0; i < n; i++)
            {
                for (int j = arena.Count - 1; j >= 0; j--) if (!arena[j].Tick(dt)) arena.RemoveAt(j);
                k.Tick(dt);
            }
        }

        private bool TemEstado(string nome, bool ligado)
        {
            foreach (object[] e in _states) if ((string)e[0] == nome && (bool)e[1] == ligado) return true;
            return false;
        }

        private bool AguaEm(Vector3 perto, float raio)
        {
            foreach (object[] e in _thits)
                if ((Elemento)e[0] == Elemento.Agua && Vector3.Distance((Vector3)e[1], perto) <= raio) return true;
            return false;
        }

        private static float Ehp(IEntidade e) => e.Vital.Hp + e.Vital.Escudo;

        [Test]
        public void GrupoC_QuatroRegistrados_ComKitNoBotao()
        {
            foreach (string s in new[] { "12-umbra", "13-brok", "14-gromm", "15-maris" })
            {
                Assert.IsTrue(Kits.De(s).Implementado, s);
                Assert.IsTrue(KitRunner.Registro.ContainsKey(s), s);
                _bounds.Clear();
                var k = new KitRunner(s, new FakeConjurador(s, Vector3.zero));
                Assert.IsNotNull(k.Impl, s + ": o player recebe o kit");
                Assert.IsTrue((bool)_bounds[0][1], s + ": KitBound(slug, true) acende os botoes");
                Assert.IsNull(new KitRunner(s, new FakeConjurador("bot", Vector3.zero, false)).Impl, s + ": bot NAO usa kit");
            }
        }

        // =============================================================================== UMBRA

        [Test]
        public void Umbra_VeuUmbrio_OPrimeiroGolpeSaindoDoVeuDa50Porcento_SoUmaVez()
        {
            var p = new FakeConjurador("umbra", Vector3.zero);
            var k = new KitRunner("12-umbra", p);
            var umbra = (Umbra)k.Impl;
            var alvo = new FakeEntidade("bot", new Vector3(3f, 0f, 0f));
            alvo.Vital.Escudo = 0f;
            Assert.IsTrue(k.UsarTatica(), "o veu dispara");
            Assert.IsTrue(umbra.NoVeu && umbra.BonusArmado, "penumbra com o golpe armado");
            Assert.IsTrue(TemEstado(Umbra.VEU, true), "a HUD/o som ouvem o veu");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == Umbra.VEU && v.Alvo == p), "a penumbra se VE no corpo dela");
            Combat.AplicarDano(alvo, 20f, Elemento.Fogo, p);
            Assert.AreEqual(100f - 20f * (1f + k.Dados.Tatica["bonus_dano"]), alvo.Vital.Hp, 0.001f, "o 1o golpe saindo do veu: +50%");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "umbra_corte" && v.Alvo == alvo), "a lamina violeta risca o alvo");
            Assert.IsFalse(umbra.NoVeu, "o golpe saiu: o veu acabou");
            Combat.AplicarDano(alvo, 20f, Elemento.Fogo, p);
            Assert.AreEqual(100f - 30f - 20f, alvo.Vital.Hp, 0.001f, "so' o PRIMEIRO golpe leva o bonus");
            var outro = new FakeEntidade("outro", Vector3.one);
            Combat.AplicarDano(alvo, 10f, Elemento.Fogo, outro);
            Assert.AreEqual(40f, alvo.Vital.Hp, 0.001f, "dano de outra fonte nao passa pelo veu dela");
        }

        [Test]
        public void Umbra_VeuUmbrio_ConjurarQuebra_VeuTemPrazo_EOBonusTemJanelaCurta()
        {
            var p = new FakeConjurador("umbra", Vector3.zero);
            var k = new KitRunner("12-umbra", p);
            var umbra = (Umbra)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            Assert.IsTrue(k.UsarTatica());
            Andar(k, t["duracao"] + 0.05f);
            Assert.IsFalse(umbra.NoVeu, "o veu tem prazo (2,5 s)");
            Assert.IsTrue(TemEstado(Umbra.VEU, false), "e a HUD recebe o desligamento");
            Assert.IsTrue(umbra.BonusArmado, "o tiro que saiu no fim do veu ainda leva o bonus (janela)");
            Andar(k, t["janela_bonus"] + 0.05f);
            Assert.IsFalse(umbra.BonusArmado, "a janela e' curta: passou, o bonus some");
            var alvo = new FakeEntidade("bot", new Vector3(3f, 0f, 0f));
            alvo.Vital.Escudo = 0f;
            Combat.AplicarDano(alvo, 20f, Elemento.Fogo, p);
            Assert.AreEqual(80f, alvo.Vital.Hp, 0.001f, "sem veu, sem bonus");

            Andar(k, k.Dados.TaticaCd);
            Assert.IsTrue(k.UsarTatica());
            k.Tick(0.2f);
            Assert.IsTrue(umbra.NoVeu);
            k.NotificarAtaque();   // CONJUROU (o ataque basico)
            k.Tick(0.05f);
            Assert.IsFalse(umbra.NoVeu, "o veu QUEBRA ao conjurar (o limitador)");
        }

        [Test]
        public void Umbra_DancaDasSombras_AvisaAntes_CadaEsquivaDeixaIscaQueExplodeFraco_EFimCegaDeLuz()
        {
            var p = new FakeConjurador("umbra", Vector3.zero);
            var k = new KitRunner("12-umbra", p);
            var umbra = (Umbra)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var perto = new FakeEntidade("perto", new Vector3(1f, 0f, 0f));
            var longe = new FakeEntidade("longe", new Vector3(s["sombra_raio"] + 1.5f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(perto); p.Arena.Add(longe);
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            Assert.Greater(k.Telegrafia, 0f, "a suprema AVISA (a luz sugada)");
            k.DashIniciou = true;
            k.Tick(0.1f);
            Assert.IsFalse(k.EstadoAtivo(Umbra.DANCA), "durante o aviso NADA acontece");
            Assert.AreEqual(0, umbra.Sombras.Count, "esquivar no aviso nao deixa isca");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "umbra_suga"), "o aviso se VE: a luz sugada");
            Andar(k, k.Dados.Telegrafia);
            Assert.IsTrue(k.EstadoAtivo(Umbra.DANCA), "a danca comeca DEPOIS do aviso");

            int disparos = _disparos;
            k.DashIniciou = true;
            k.Tick(0.01f);
            Assert.AreEqual(1, umbra.Sombras.Count, "a esquiva deixa a sombra-isca");
            Assert.AreEqual(Vector3.zero, umbra.Sombras[0].Pos, "ONDE ELA ESTAVA");
            Assert.AreEqual(disparos + 1, _disparos, "o salto SUSSURRA (os bots ouvem)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "umbra_tinta"), "o corte de tinta preta");
            p.Pos = new Vector3(0f, 0f, 5f);   // a esquiva levou o corpo
            float ehpPerto = Ehp(perto), ehpLonge = Ehp(longe), ehpEla = Ehp(p);
            Andar(k, s["sombra_espera"] + 0.1f);
            Assert.AreEqual(0, umbra.Sombras.Count, "a isca estourou");
            Assert.Less(Ehp(perto), ehpPerto, "quem esta' colado na isca apanha");
            Assert.LessOrEqual(ehpPerto - Ehp(perto), s["sombra_dano"], "e apanha FRACO (assedio, nao nuke)");
            Assert.AreEqual(ehpLonge, Ehp(longe), 0.001f, "0 de dano a quem esta' longe");
            Assert.AreEqual(ehpEla, Ehp(p), 0.001f, "a isca nao fere a dona");

            for (int i = 0; i < 100 && k.EstadoAtivo(Umbra.DANCA); i++) k.Tick(0.1f);
            Assert.IsFalse(k.EstadoAtivo(Umbra.DANCA), "6 s e acabou");
            Assert.IsFalse(k.PodeConjurar, "o PRECO: 1 s cega de LUZ, sem conjurar");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "umbra_luz"), "o clarao se ve'");
            Andar(k, s["cega_dur"] + 0.1f);
            Assert.IsTrue(k.PodeConjurar, "a cegueira tem prazo");
        }

        [Test]
        public void Umbra_PassoDeVeludo_AbateDevolve30PorcentoDaEsquiva_SoAbateDela()
        {
            var p = new FakeConjurador("umbra", Vector3.zero);
            var k = new KitRunner("12-umbra", p);
            var umbra = (Umbra)k.Impl;
            IConjurador quem = null; float fracao = 0f; int n = 0;
            Umbra.RecarregarEsquiva = (c, f) => { quem = c; fracao = f; n++; };
            k.Tick(0.01f);
            var ferido = new FakeEntidade("ferido", Vector3.one);
            Combat.AplicarDano(ferido, 20f, Elemento.Fogo, p);
            Assert.AreEqual(0, n, "ferir nao e' abater");
            var alheio = new FakeEntidade("alheio", Vector3.one);
            Combat.AplicarDano(alheio, 500f, Elemento.Fogo, new FakeEntidade("outro", Vector3.zero));
            Assert.AreEqual(0, n, "abate de OUTRO nao recarrega a esquiva dela");
            var vitima = new FakeEntidade("vitima", Vector3.one);
            Combat.AplicarDano(vitima, 500f, Elemento.Fogo, p);
            Assert.AreEqual(1, n, "o abate dela recarrega a esquiva");
            Assert.AreSame(p, quem);
            Assert.AreEqual(k.Dados.Passiva["esquiva_por_abate"], fracao, 0.001f, "30% do cooldown da esquiva");
            Assert.AreEqual(1, umbra.Abates);
        }

        // =============================================================================== BROK

        [Test]
        public void Brok_RunaEscudo_BloqueiaTiroInimigo_NaoOProprio_EGastaAVida()
        {
            var p = new FakeConjurador("brok", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("13-brok", p);
            var brok = (Brok)k.Impl;
            var arena = new List<Projetil>();
            k.ProjeteisVivos = () => arena;
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual(1, brok.Muralhas.Count, "a martelada ergue a muralha");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "brok_muralha"), "a muralha se VE");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "brok_martelada"), "faisca de forja no cast");
            Brok.Muralha muro = brok.Muralhas[0];
            var bot = new FakeEntidade("bot", new Vector3(0f, 0f, 20f));
            Projetil inimigo = Projetil.Lancar(bot, new Vector3(0f, 1.4f, 10f), Vector3.back, Elemento.Fogo);
            Projetil dele = Projetil.Lancar(p, new Vector3(0f, 1.4f, 0.5f), Vector3.forward, Elemento.Fogo);
            arena.Add(inimigo); arena.Add(dele);
            float vida0 = muro.Vida;
            _thits.Clear();
            Voar(k, arena, 0.5f);
            Assert.IsFalse(inimigo.Vivo, "o tiro inimigo morre na muralha");
            Assert.Less(inimigo.Pos.z, muro.Raio + 1f, "morreu NA muralha (a 2,5 m), nao antes");
            Assert.Greater(inimigo.Pos.z, muro.Raio - 1f, "e nao passou dela");
            Assert.IsTrue(dele.Vivo, "o tiro do proprio Brok atravessa");
            Assert.AreEqual(1, muro.Bloqueados);
            Assert.AreEqual(vida0 - inimigo.Dano * Balance.Perfil(Elemento.Fogo).Estrutura, muro.Vida, 0.001f, "o tiro gasta a vida propria");
            Assert.IsTrue(_thits.Exists(e => (Elemento)e[0] == Elemento.Fogo), "o impacto acontece na muralha (terreno e estouro reagem ali)");
            Andar(k, k.Dados.Tatica["duracao"]);
            Assert.AreEqual(0, brok.Muralhas.Count, "6 s e a muralha cai");
        }

        [Test]
        public void Brok_RunaEscudo_Limitadores_FlancoEPorCimaPassam_EVidaPropriaQuebra()
        {
            var p = new FakeConjurador("brok", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("13-brok", p);
            var brok = (Brok)k.Impl;
            var arena = new List<Projetil>();
            k.ProjeteisVivos = () => arena;
            k.UsarTatica();
            var bot = new FakeEntidade("bot", new Vector3(20f, 0f, 20f));
            Projetil flanco = Projetil.Lancar(bot, new Vector3(10f, 1.4f, 0f), Vector3.left, Elemento.Fogo);
            Projetil alto = Projetil.Lancar(bot, new Vector3(0f, 3.5f, 10f), Vector3.back, Elemento.Fogo);
            arena.Add(flanco); arena.Add(alto);
            Voar(k, arena, 0.45f);
            Assert.IsTrue(flanco.Vivo, "pelo FLANCO passa (so' a frente e' coberta)");
            Assert.IsTrue(alto.Vivo, "POR CIMA passa");
            Assert.AreEqual(0, brok.Muralhas[0].Bloqueados);

            Brok.Muralha muro = brok.Muralhas[0];
            Projetil molde = Projetil.Lancar(bot, Vector3.zero, Vector3.back, Elemento.Terra);
            int precisa = Mathf.CeilToInt(muro.Vida / (molde.Dano * Balance.Perfil(Elemento.Terra).Estrutura));
            for (int i = 1; i <= precisa; i++)
            {
                Projetil pedra = Projetil.Lancar(bot, new Vector3(0f, 1.4f, 10f), Vector3.back, Elemento.Terra);
                arena.Add(pedra);
                Voar(k, arena, 0.6f);
                Assert.IsFalse(pedra.Vivo, "a pedra " + i + " bateu");
                if (i < precisa) Assert.AreEqual(1, brok.Muralhas.Count, "aguenta ate' a vida acabar (" + i + ")");
            }
            Assert.AreEqual(0, brok.Muralhas.Count, "VIDA PROPRIA: terra (antiestrutura) derruba em " + precisa + " pedras");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "brok_estilhaco"), "quebrou em estilhacos");
        }

        [Test]
        public void Brok_ForjaViva_AvisaAntes_EscudoNuncaVida_RunaDevolve_BarulhentaDestrutivel_EApagaAsRunas()
        {
            var p = new FakeConjurador("brok", Vector3.zero);
            var k = new KitRunner("13-brok", p);
            var brok = (Brok)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var aliado = new FakeConjurador("aliado", new Vector3(2f, 0f, 0f));
            var inimigo = new FakeEntidade("bot", new Vector3(-3f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(aliado); p.Arena.Add(inimigo);
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            p.Vital.Escudo = 10f; p.Vital.Hp = 50f; aliado.Vital.Escudo = 0f; aliado.Vital.Hp = 50f; inimigo.Vital.Escudo = 0f;
            k.Tick(0.1f);
            Assert.IsNull(brok.BigornaAtiva, "durante o aviso NADA (a bigorna ainda esta' erguida)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "brok_erguida" && v.Alvo == p), "o aviso se VE: bigorna acima da cabeca");
            p.Pos = new Vector3(1f, 0f, 0f);
            int d0 = _disparos;
            Andar(k, k.Dados.Telegrafia);
            Assert.IsNotNull(brok.BigornaAtiva, "a bigorna desce DEPOIS do aviso");
            Assert.AreEqual(Vector3.zero, brok.BigornaAtiva.Pos, "no CENTRO do anel do aviso, nao onde ele andou");
            Assert.AreEqual(s["runa"], brok.Runa, 0.001f, "runa pessoal de 25");
            Assert.Greater(_disparos, d0, "o baque do plantio denuncia a posicao");

            float esc0 = p.Vital.Escudo;
            Andar(k, 1f);
            Assert.Greater(p.Vital.Escudo, esc0 + 3f, "regenera ESCUDO no raio");
            Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "NUNCA vida");
            Assert.Greater(aliado.Vital.Escudo, 3f, "o aliado no raio tambem");
            Assert.AreEqual(50f, aliado.Vital.Hp, 0.001f);
            Assert.AreEqual(0f, inimigo.Vital.Escudo, 0.001f, "inimigo no raio nao ganha nada");

            float esc1 = p.Vital.Escudo;
            Combat.AplicarDano(p, 10f, Elemento.Fogo, inimigo);
            k.Tick(0.01f);
            Assert.AreEqual(esc1, p.Vital.Escudo, 0.01f, "a RUNA devolve no mesmo tique o que o Combat cobrou");
            Assert.AreEqual(s["runa"] - 10f, brok.Runa, 0.01f, "e gasta o que devolveu");
            Combat.AplicarDano(p, 60f, Elemento.Fogo, inimigo);
            k.Tick(0.01f);
            Assert.AreEqual(0f, brok.Runa, 0.001f, "devolve ate' 25, nao mais");
            k.Tick(0.01f);
            Assert.IsFalse(k.Visuais.Any(v => v.Tipo == "brok_runa"), "runa gasta some do corpo");

            int d1 = _disparos;
            Andar(k, s["clang"] * 2f + 0.1f);
            Assert.GreaterOrEqual(_disparos - d1, 2, "BARULHENTA: cada clang e' um Disparo que os bots ouvem");

            var arena = new List<Projetil>();
            k.ProjeteisVivos = () => arena;
            float vida0 = brok.BigornaAtiva.Vida;
            Projetil tiro = Projetil.Lancar(inimigo, new Vector3(-5f, Brok.Bigorna.ALTURA, 0f), Vector3.right, Elemento.Fogo);
            arena.Add(tiro);
            Voar(k, arena, 0.3f);
            Assert.IsFalse(tiro.Vivo, "o tiro bate na bigorna");
            Assert.AreEqual(vida0 - tiro.Dano, brok.BigornaAtiva.Vida, 0.01f, "a bigorna e' destrutivel (vida propria)");

            Andar(k, brok.BigornaAtiva.Duracao + 0.05f);
            Assert.IsNull(brok.BigornaAtiva, "12 s e ela racha");
            Assert.IsTrue(k.EstadoAtivo(Brok.RUNAS_APAGADAS), "o PRECO: as runas do martelo apagam");
            Assert.IsFalse(k.ProntoTatica, "sem tatica");
            Assert.GreaterOrEqual(k.TaticaCd, s["runas_apagadas"] - 0.2f, "por 3 s");
        }

        [Test]
        public void Brok_TemperaEterna_DevolveQuinzePorcentoDoDanoDoChao_SoDoChao()
        {
            var p = new FakeConjurador("brok", Vector3.zero);
            var k = new KitRunner("13-brok", p);
            var brok = (Brok)k.Impl;
            float r = k.Dados.Passiva["reducao_terreno"];
            KitRunner.DpsDoTerreno = pos => Balance.Terrain.BurnDps;
            p.Vital.Hp = 50f;
            k.Tick(0.5f);
            Assert.AreEqual(50f + Balance.Terrain.BurnDps * 0.5f * r, p.Vital.Hp, 0.001f, "fogo no chao: 15% volta");
            Assert.IsTrue(brok.Temperando);
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "brok_tempera"), "o metal aterrando se VE nos pes");
            KitRunner.DpsDoTerreno = pos => Balance.Terrain.ElectrifyDps;
            p.Vital.Hp = 50f;
            k.Tick(0.5f);
            Assert.AreEqual(50f + Balance.Terrain.ElectrifyDps * 0.5f * r, p.Vital.Hp, 0.001f, "chao eletrico tambem e' terreno");
            KitRunner.DpsDoTerreno = pos => 0f;
            p.Vital.Hp = 50f;
            k.Tick(0.5f);
            Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "fora do chao que doi, nada");
            Assert.IsFalse(brok.Temperando);
        }

        // =============================================================================== GROMM

        [Test]
        public void Gromm_TotemDasChuvas_CuraOTimeNoRaio_ApagaFogo_InclusiveOTaticoAliado_EOTotemCai()
        {
            var terreno = new TerrenoReativo(null, 60f, 7, (cx, cz) => TipoCelula.Combustivel);
            try
            {
                var p = new FakeConjurador("gromm", Vector3.zero) { DirecaoDaMira = Vector3.forward };
                var k = new KitRunner("14-gromm", p);
                var gromm = (Gromm)k.Impl;
                Dictionary<string, float> t = k.Dados.Tatica;
                Vector3 alvoDoTotem = Vector3.forward * t["distancia"];
                terreno.Reagir(Elemento.Fogo, alvoDoTotem, false);
                Assert.AreEqual(EstadoCelula.Queimando, terreno.EstadoEm(alvoDoTotem), "preparo: o chao do totem pega fogo");
                var pyra = new KitRunner("01-pyra", new FakeConjurador("pyra", new Vector3(0f, 0f, -2f)) { DirecaoDaMira = Vector3.forward });
                pyra.UsarTatica();
                Assert.AreEqual(1, ((Pyra)pyra.Impl).BrasasVivas.Count, "preparo: a muralha de brasas ALIADA esta' acesa");
                var inimigo = new FakeEntidade("bot", alvoDoTotem + Vector3.right);
                var fora = new FakeConjurador("fora", new Vector3(t["raio"] + 5f, 0f, 0f));
                p.Arena.Add(p); p.Arena.Add(inimigo); p.Arena.Add(fora);
                p.Vital.Hp = 50f; inimigo.Vital.Hp = 50f; fora.Vital.Hp = 50f;

                Assert.IsTrue(k.UsarTatica());
                Assert.AreEqual(1, gromm.Totens.Count, "planta o totem");
                Assert.AreEqual(alvoDoTotem, gromm.Totens[0].Pos, "um passo a frente, na mira");
                Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "gromm_totem"), "o totem e a chuva se VEEM");
                Assert.IsTrue(AguaEm(alvoDoTotem, 0.01f), "a chuva e' AGUA no terreno");
                Assert.AreNotEqual(EstadoCelula.Queimando, terreno.EstadoEm(alvoDoTotem), "a chuva APAGA o fogo (§14)");
                Assert.AreEqual(0, ((Pyra)pyra.Impl).BrasasVivas.Count, "e apaga o fogo TATICO aliado (dois gumes)");

                Andar(k, 1f);
                Assert.That(p.Vital.Hp, Is.InRange(50f + t["cura"] * 0.7f, 50f + t["cura"] * 1.05f), "chuva curativa: 6 hp/s no raio");
                Assert.AreEqual(50f, inimigo.Vital.Hp, 0.001f, "inimigo na chuva nao cura");
                Assert.AreEqual(50f, fora.Vital.Hp, 0.001f, "aliado fora do raio nao cura");

                var arena = new List<Projetil>();
                k.ProjeteisVivos = () => arena;
                Projetil molde = Projetil.Lancar(inimigo, Vector3.zero, Vector3.left, Elemento.Terra);
                int precisa = Mathf.CeilToInt(t["vida"] / (molde.Dano * Balance.Perfil(Elemento.Terra).Estrutura));
                for (int i = 1; i <= precisa; i++)
                {
                    Projetil pedra = Projetil.Lancar(inimigo, alvoDoTotem + new Vector3(6f, Gromm.Totem.ALTURA, 0f), Vector3.left, Elemento.Terra);
                    arena.Add(pedra);
                    Voar(k, arena, 0.5f);
                    Assert.IsFalse(pedra.Vivo, "a pedra " + i + " bate no totem");
                }
                Assert.AreEqual(0, gromm.Totens.Count, "o totem tem 80 de vida: e' o alvo obvio");
                float hp = p.Vital.Hp;
                Andar(k, 1f);
                Assert.AreEqual(hp, p.Vital.Hp, 0.001f, "totem derrubado, chuva acabou");
            }
            finally { terreno.Desligar(); }
        }

        [Test]
        public void Gromm_SangueDaEstepe_CurarUmAliadoDevolve30PorcentoDoQueEntrou()
        {
            var p = new FakeConjurador("gromm", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("14-gromm", p);
            Dictionary<string, float> t = k.Dados.Tatica;
            var aliado = new FakeConjurador("aliado", Vector3.forward * t["distancia"]);
            p.Arena.Add(p); p.Arena.Add(aliado);
            k.UsarTatica();
            p.Pos = new Vector3(t["raio"] + 10f, 0f, 0f);   // o Gromm FORA da chuva: so' a passiva o cura
            p.Vital.Hp = 50f; aliado.Vital.Hp = 50f;
            Andar(k, 1f);
            float curado = aliado.Vital.Hp - 50f;
            Assert.Greater(curado, 3f, "a chuva cura o aliado");
            Assert.AreEqual(50f + curado * k.Dados.Passiva["cura_propria"], p.Vital.Hp, 0.01f, "e 30% do que ENTROU volta para o Gromm");
            aliado.Vital.Hp = aliado.Vital.HpMax;
            float hp = p.Vital.Hp;
            Andar(k, 1f);
            Assert.AreEqual(hp, p.Vital.Hp, 0.001f, "aliado cheio nao cura: 30% de nada e' nada");
        }

        [Test]
        public void Gromm_EspiritoDoTrovao_AvisaNaLinha_EmpurraSoInimigo_DerrubaMuro_RastroAcelera_EOPrecoCansa()
        {
            var m = new Partida(new FakeRelevo());
            try
            {
                m.Iniciar(3, 0, true, Vector3.zero);
                var terreno = new TerrenoReativo(null, 120f, 7, (cx, cz) => TipoCelula.Chao);
                m.Terreno = terreno;
                Vector3 muro = new Vector3(0f, 0f, 10f);
                terreno.Reagir(Elemento.Terra, muro, false);
                Assert.IsTrue(terreno.BloqueiaTiro(muro), "preparo: muro de pedra na linha");
                var empurroes = new List<KeyValuePair<IEntidade, Vector3>>();
                Gromm.EmpurrarCorpo = (e, v) => empurroes.Add(new KeyValuePair<IEntidade, Vector3>(e, v));

                var p = new FakeConjurador("gromm", Vector3.zero) { DirecaoDaMira = Vector3.forward };
                var k = new KitRunner("14-gromm", p);
                var gromm = (Gromm)k.Impl;
                Dictionary<string, float> s = k.Dados.Suprema;
                var inimigo = new FakeEntidade("bot", new Vector3(0.8f, 0f, 6f));
                var ladoFora = new FakeEntidade("fora", new Vector3(s["largura"] + 3f, 0f, 6f));
                var aliado = new FakeConjurador("aliado", new Vector3(0.5f, 0f, 4f));
                p.Arena.Add(p); p.Arena.Add(inimigo); p.Arena.Add(ladoFora); p.Arena.Add(aliado);
                Carregar(k);
                Assert.IsTrue(k.UsarSuprema());
                k.Tick(0.1f);
                EfeitoVisual linha = k.Visuais.FirstOrDefault(v => v.Tipo == "gromm_linha");
                Assert.IsNotNull(linha, "o aviso se VE: a linha que treme no chao");
                Assert.AreEqual(s["comprimento"], Vector3.Distance(linha.Pos, linha.Pos2), 0.01f, "a linha inteira do bisao");
                p.DirecaoDaMira = Vector3.right;   // virou no meio do aviso
                Assert.IsNull(gromm.BisaoAtivo, "durante o aviso NADA");
                Assert.AreEqual(0, empurroes.Count);
                Andar(k, k.Dados.Telegrafia);
                Assert.IsNotNull(gromm.BisaoAtivo, "o bisao sai DEPOIS do aviso");
                Assert.AreEqual(Vector3.forward, gromm.BisaoAtivo.Dir, "na linha AVISADA, nao na mira nova");

                float ehp = Ehp(inimigo);
                Andar(k, 1f);
                Assert.AreEqual(1, empurroes.Count(e => e.Key == inimigo), "empurra o inimigo UMA vez");
                Vector3 ve = empurroes.First(e => e.Key == inimigo).Value;
                Assert.Greater(ve.z, 0f, "para a frente da corrida");
                Assert.Greater(ve.x, 0f, "e para fora da linha");
                Assert.AreEqual(s["empurrao"], ve.magnitude, 0.01f);
                Assert.IsFalse(empurroes.Any(e => e.Key == ladoFora), "fora da largura nao leva");
                Assert.IsFalse(empurroes.Any(e => e.Key == aliado), "aliado nao e' empurrado");
                Assert.LessOrEqual(ehp - Ehp(inimigo), s["dano"] * 1.3f, "dano irrelevante: desloca, nao mata");
                Assert.IsFalse(terreno.BloqueiaTiro(muro), "derruba o muro de pedra");
                Assert.IsFalse(terreno.BloqueiaTiro(muro + Vector3.forward * 3f) || terreno.BloqueiaTiro(muro + Vector3.forward * 6f),
                    "e nao ergue muro novo no chao limpo (Terra em chao limpo ERGUE muro)");

                Andar(k, 1.2f);
                Assert.IsFalse(gromm.BisaoAtivo.Correndo, "30 m e o bisao dissolve");
                Assert.IsTrue(k.EstadoAtivo(Gromm.CANSADO), "o PRECO: levanta com o peso dos anos");
                Assert.AreEqual(s["cansado_vel"], Efeitos.De(p).StatusMult, 0.001f, "2 s lento, mesmo em cima do proprio rastro");
                Assert.AreEqual(s["rastro_vel"], Efeitos.De(aliado).StatusMult, 0.001f, "o rastro acelera o aliado");
                Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "gromm_rastro"), "o rastro se VE");
                Andar(k, s["cansado_dur"] + 0.2f);
                Assert.AreEqual(s["rastro_vel"], Efeitos.De(p).StatusMult, 0.001f, "descansado, o rastro acelera ele tambem");
            }
            finally { m.Encerrar(); }
        }

        // =============================================================================== MARIS

        [Test]
        public void Maris_OndaPrisao_EsferaLentaPrende_MolhaESuspendeMasNaoSilencia_EsquivaFura()
        {
            var p = new FakeConjurador("maris", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("15-maris", p);
            var maris = (Maris)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            var inimigo = new FakeEntidade("bot", new Vector3(0f, 0f, 6f));
            p.Arena.Add(p); p.Arena.Add(inimigo);
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual(1, maris.Esferas.Count, "a esfera sai");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "maris_esfera"), "e se VE (visivel = esquivavel)");
            Andar(k, 0.3f);
            Assert.AreEqual(1, maris.Esferas.Count, "LENTA: a 6 m ainda nao chegou em 0,3 s");
            Assert.AreEqual(1f, Efeitos.De(inimigo).StatusMult, 0.001f);
            Andar(k, 0.4f);
            Assert.AreEqual(0, maris.Esferas.Count, "tocou: vira coluna");
            Assert.AreEqual(Velocidade.Piso, Efeitos.De(inimigo).StatusMult, 0.001f, "SUSPENSO: nao anda");
            Assert.AreEqual(t["suspensao"], Efeitos.De(inimigo).SlowLeft, 0.001f, "por 1,2 s");
            Assert.Greater(Efeitos.De(inimigo).WetLeft, 0f, "a coluna MOLHA (o raio conduz nele)");
            Assert.AreEqual(0f, Efeitos.De(inimigo).StunLeft, 0.001f, "NAO e' atordoar: ele ainda conjura (o limitador)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "maris_coluna" && v.Alvo == inimigo), "a coluna se VE no preso");
            Assert.IsTrue(AguaEm(inimigo.Pos, 0.01f), "a agua cai no chao (§14)");

            Efeitos.Reset();
            Andar(k, k.Dados.TaticaCd);
            var esquivando = new FakeEntidade("esquiva", new Vector3(0f, 0f, 3f));
            var aliado = new FakeConjurador("aliado", new Vector3(0f, 0f, 5f));
            var depois = new FakeEntidade("depois", new Vector3(0f, 0f, 8f));
            p.Arena.Clear(); p.Arena.Add(p); p.Arena.Add(esquivando); p.Arena.Add(aliado); p.Arena.Add(depois);
            Efeitos.De(esquivando).IframesLeft = 10f;
            Assert.IsTrue(k.UsarTatica());
            Andar(k, 1.2f);
            Assert.AreEqual(1f, Efeitos.De(esquivando).StatusMult, 0.001f, "a ESQUIVA fura a esfera (i-frames)");
            Assert.AreEqual(1f, Efeitos.De(aliado).StatusMult, 0.001f, "aliado nao e' preso");
            Assert.AreEqual(Velocidade.Piso, Efeitos.De(depois).StatusMult, 0.001f, "quem vinha atras e' pego");

            Andar(k, k.Dados.TaticaCd);
            p.DirecaoDaMira = Vector3.left;
            p.Arena.Clear();
            _thits.Clear();
            Assert.IsTrue(k.UsarTatica());
            Andar(k, t["alcance"] / t["velocidade"] + 0.3f);
            Assert.AreEqual(0, maris.Esferas.Count, "sem ninguem, estoura no fim do alcance");
            Assert.IsTrue(AguaEm(new Vector3(-t["alcance"] - 0.9f, t["altura"], 0f), 0.5f), "e a agua cai la' (vira poca)");
        }

        [Test]
        public void Maris_MareCheia_AvisaAntes_TimeDesliza_InimigoAfunda_TodosMolhados_RaioConduzParaTodos_EEncharca()
        {
            var p = new FakeConjurador("maris", Vector3.zero);
            var k = new KitRunner("15-maris", p);
            var maris = (Maris)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var aliado = new FakeConjurador("aliado", new Vector3(3f, 0f, 0f));
            var inimigo = new FakeEntidade("bot", new Vector3(-3f, 0f, 0f));
            var preso = new FakeEntidade("preso", new Vector3(0f, 0f, 3f));
            var longe = new FakeEntidade("longe", new Vector3(s["raio"] + 6f, 0f, 0f));
            p.Arena.Add(p); p.Arena.Add(aliado); p.Arena.Add(inimigo); p.Arena.Add(preso); p.Arena.Add(longe);
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            k.Tick(0.1f);
            Assert.IsNull(maris.MareAtiva, "durante o aviso NADA");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "maris_recuo"), "o aviso se VE: a agua recua");
            p.Pos = new Vector3(5f, 0f, 0f);
            Andar(k, k.Dados.Telegrafia);
            Assert.IsNotNull(maris.MareAtiva, "a mare chega DEPOIS do aviso");
            Assert.AreEqual(Vector3.zero, maris.MareAtiva.Centro, "no CENTRO do anel do aviso");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "maris_mare"), "a mare se VE");

            Efeitos.Lentificar(preso, Velocidade.Piso, 5f);   // uma prisao mais forte que a Mare
            Andar(k, 0.5f);
            Assert.AreEqual(s["buff_vel"], Efeitos.De(aliado).StatusMult, 0.001f, "o time DESLIZA");
            Assert.AreEqual(s["buff_vel"], Efeitos.De(p).StatusMult, 0.001f, "ela tambem");
            Assert.AreEqual(s["lentidao"], Efeitos.De(inimigo).StatusMult, 0.001f, "o inimigo AFUNDA (-20%)");
            Assert.AreEqual(Velocidade.Piso, Efeitos.De(preso).StatusMult, 0.001f, "a Mare nao solta quem esta' preso");
            Assert.IsTrue(Efeitos.De(aliado).WetLeft > 0f && Efeitos.De(inimigo).WetLeft > 0f && Efeitos.De(p).WetLeft > 0f && Efeitos.De(preso).WetLeft > 0f,
                "TODO MUNDO na agua fica molhado");
            Assert.AreEqual(1f, Efeitos.De(longe).StatusMult, 0.001f, "fora da area, nada");
            Assert.AreEqual(0f, Efeitos.De(longe).WetLeft, 0.001f);
            Assert.IsTrue(maris.SobreAgua, "a propria Mare e' agua para a Mare Viva");

            Bus.EmitTerrainHit(Elemento.Raio, new Vector3(s["raio"] + 10f, 0f, 0f), false);
            Assert.AreEqual(0f, maris.MareAtiva.Choque, 0.001f, "raio LONGE da agua nao conduz");
            float ela = Ehp(p), dele = Ehp(aliado), inim = Ehp(inimigo), fora = Ehp(longe);
            Bus.EmitTerrainHit(Elemento.Raio, new Vector3(-2f, 0f, 0f), false);
            Assert.Greater(maris.MareAtiva.Choque, 0f, "raio DENTRO eletrifica a area inteira");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "maris_choque"), "o choque se VE");
            Andar(k, 0.6f);
            Assert.Less(Ehp(inimigo), inim, "o inimigo na agua leva o choque");
            Assert.Less(Ehp(p), ela, "ELA TAMBEM (dois gumes maximo)");
            Assert.Less(Ehp(aliado), dele, "e o time dela");
            Assert.AreEqual(fora, Ehp(longe), 0.001f, "fora da agua nao");

            for (int i = 0; i < 150 && maris.MareAtiva != null; i++) k.Tick(0.1f);
            Assert.IsNull(maris.MareAtiva, "10 s e a agua drena");
            Assert.IsTrue(k.EstadoAtivo(Maris.ENCHARCADA), "o PRECO: roupa encharcada");
            Assert.AreEqual(s["encharcada_vel"], Efeitos.De(p).StatusMult, 0.001f, "2 s lenta");
        }

        [Test]
        public void Maris_MareViva_RegeneraSobreAguaLiquidaOuPoca_NuncaNoSecoNemNoGelo()
        {
            var m = new Partida(new FakeRelevo());
            try
            {
                m.Iniciar(3, 0, true, Vector3.zero);
                // a oeste (x < 0) e' lago; a leste, chao
                var terreno = new TerrenoReativo(null, 60f, 7, (cx, cz) => cx < 10 ? TipoCelula.Agua : TipoCelula.Chao);
                m.Terreno = terreno;
                var lago = new Vector3(-7.5f, 0f, 1.5f);
                var seco = new Vector3(7.5f, 0f, 1.5f);
                var p = new FakeConjurador("maris", lago);
                var k = new KitRunner("15-maris", p);
                var maris = (Maris)k.Impl;
                float regen = k.Dados.Passiva["regen"];
                p.Vital.Hp = 50f;
                Andar(k, 1f);
                Assert.That(p.Vital.Hp, Is.InRange(50f + regen * 0.75f, 50f + regen * 1.05f), "sobre o lago: 3 hp/s");
                Assert.IsTrue(maris.SobreAgua);
                Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "maris_mare_viva"), "a cura sobre a agua se VE");

                p.Pos = seco; p.Vital.Hp = 50f;
                Andar(k, 1f);
                Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "no seco, nada");
                Assert.IsFalse(maris.SobreAgua);

                terreno.Reagir(Elemento.Agua, seco, false);   // agua no chao de terra: POCA (lama)
                p.Vital.Hp = 50f;
                Andar(k, 1f);
                Assert.Greater(p.Vital.Hp, 50f + regen * 0.7f, "a poca conta como agua");

                terreno.Reagir(Elemento.Agua, lago, false);   // agua no lago: CONGELA (gelo e' rota, nao agua)
                Assert.AreEqual(EstadoCelula.Congelado, terreno.EstadoEm(lago), "preparo: o lago congelou");
                p.Pos = lago; p.Vital.Hp = 50f;
                Andar(k, 1f);
                Assert.AreEqual(50f, p.Vital.Hp, 0.001f, "no gelo, nada");
                terreno.Desligar();
            }
            finally { m.Encerrar(); }
        }
    }
}
