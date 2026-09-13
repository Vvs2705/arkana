using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// Os kits do GRUPO A (02 Ceifadora, 04 Corvus, 05 Corvomante, 06 Olho-de-Eter), pelo mesmo molde do
    /// GameplayKitsTests (FakeConjurador, sem cena): para cada mago o EFEITO da tatica, o LIMITADOR que a paga, a
    /// TELEGRAFIA + o efeito da suprema e a PASSIVA. As leis gerais (5-10 s, 1-4 s, sem mana) sao do CoreKitsTests.
    /// </summary>
    public class GameplayKitsGrupoATests
    {
        private List<object[]> _states;
        private int _disparos;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset();
            KitRunner.DpsDoTerreno = null;
            _states = new List<object[]>();
            _disparos = 0;
            Bus.KitState += (n, on) => _states.Add(new object[] { n, on });
            Bus.Disparo += (p, pos) => _disparos++;
        }

        [TearDown]
        public void TearDown() { Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        private static void Andar(KitRunner k, float total, float dt = 0.1f)
        {
            int n = Mathf.CeilToInt(total / dt);
            for (int i = 0; i < n; i++) k.Tick(dt);
        }

        /// <summary>Enche a carga na hora pelo canal do DANO (1000 x CargaPorDano = 150%).</summary>
        private static void Carregar(KitRunner k) => k.Tick(0.01f, 1000f);

        private bool TemEstado(string nome, bool ligado) => _states.Any(e => (string)e[0] == nome && (bool)e[1] == ligado);

        private static float Ehp(IEntidade e) => e.Vital.Hp + e.Vital.Escudo;

        /// <summary>A Travessia move corpos pela casca; no teste o "corpo" e' o Pos dos fakes.</summary>
        private static bool MoverFake(IEntidade e, Vector3 p)
        {
            var c = e as FakeConjurador;
            if (c != null) { c.Pos = p; return true; }
            var f = e as FakeEntidade;
            if (f != null) { f.Pos = p; return true; }
            return false;
        }

        // ================================================================== 02 CEIFADORA

        [Test]
        public void Ceifadora_MaoDoVazio_AvisaAntes_AgarraOPrimeiro_ElaAindaConjura_EAEsquivaCorta()
        {
            var c = new FakeConjurador("ceifa", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("02-ceifadora", c);
            var ceifa = (Ceifadora)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            var alvo = new FakeConjurador("bot", new Vector3(0.8f, 0f, 9f), false);   // perto da linha de mira, a 9m
            var vizinho = new FakeEntidade("vizinho", new Vector3(0.8f, 0f, 9f + t["raio"] + 0.5f));
            c.Arena.Add(c); c.Arena.Add(alvo); c.Arena.Add(vizinho);
            float mana0 = c.Mana;
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual(mana0, c.Mana, "a tatica NAO consome mana");
            Assert.AreEqual(alvo.Pos, ceifa.MaoAtiva.Ponto, "o ponto e' o inimigo na linha de mira (ate' 12m)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "ceifadora_marca"), "a rachadura + o circulo AVISAM antes");
            Andar(k, t["atraso"] - 0.15f);
            Assert.AreEqual(1f, Efeitos.De(alvo).StatusMult, "durante o aviso ninguem e' preso (a mao viaja)");
            Andar(k, 0.2f);
            Assert.AreSame(alvo, ceifa.MaoAtiva.Preso, "a mao AGARRA o primeiro na area");
            Assert.AreEqual(Velocidade.Piso, Efeitos.De(alvo).StatusMult, 1e-4f, "prende a POSICAO");
            Assert.AreEqual(t["agarra"], Efeitos.De(alvo).SlowLeft, 0.01f, "por 1,2s");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "ceifadora_mao" && v.Alvo == alvo), "a mao se ve' presa nele");
            Assert.IsFalse(alvo.Estados.ContainsKey("silencio"), "o agarrado AINDA CONJURA (prende, nao executa)");
            Assert.AreEqual(0f, Efeitos.De(alvo).StunLeft, "e nao atordoa");
            Assert.AreEqual(1f, Efeitos.De(vizinho).StatusMult, "so' o primeiro: o vizinho fora da area pequena fica livre");
            // o LIMITADOR: cortavel com 1 golpe — hoje a esquiva do agarrado arranca a mao
            Efeitos.De(alvo).IframesLeft = 0.12f;
            k.Tick(0.05f);
            Assert.AreEqual(1f, Efeitos.De(alvo).StatusMult, "cortou: solto na hora");
            Assert.IsNull(ceifa.MaoAtiva);
        }

        [Test]
        public void Ceifadora_Travessia_AvisaAntes_Atravessa60m_SaiReveladaESemConjurar_ORasgoAbertoPuxaQuemToca()
        {
            System.Func<IEntidade, Vector3, bool> velho = Ceifadora.Teleportar;
            try
            {
                Ceifadora.Teleportar = MoverFake;
                var c = new FakeConjurador("ceifa", Vector3.zero) { DirecaoDaMira = Vector3.forward };
                var k = new KitRunner("02-ceifadora", c);
                Dictionary<string, float> s = k.Dados.Suprema;
                var inimigo = new FakeConjurador("bot", new Vector3(10f, 0f, 0f), false);
                c.Arena.Add(c); c.Arena.Add(inimigo);
                Carregar(k);
                Assert.IsTrue(k.UsarSuprema());
                Andar(k, k.Dados.Telegrafia - 0.2f);
                Assert.AreEqual(Vector3.zero, c.Pos, "durante o aviso ela NAO sai do lugar");
                Andar(k, 0.3f);
                Assert.AreEqual(s["alcance"], c.Pos.z, 0.01f, "atravessou 60m em linha reta, na hora");
                Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "ceifadora_rasgo"), "o rasgo se ve'");
                Assert.IsTrue(k.EstadoAtivo(Ceifadora.REVELADO) && TemEstado("revelado", true), "sai REVELADA (o chip da HUD)");
                Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "revelado" && v.Alvo == c), "o feixe sobre ela");
                Assert.IsFalse(k.PodeConjurar, "1s SEM CONJURAR (vertigem do Vazio)");
                // o rasgo fica ABERTO: quem tocar a entrada vem atras — inimigo inclusive
                inimigo.Pos = new Vector3(0.5f, 0f, 0.3f);
                k.Tick(0.1f);
                Assert.Greater(inimigo.Pos.z, s["alcance"] - 3f, "o inimigo que tocou a entrada atravessou atras dela");
                Assert.IsTrue(inimigo.Estados.ContainsKey("silencio"), "e sai sem conjurar tambem");
                Andar(k, s["silencio"] + 0.1f);
                Assert.IsTrue(k.PodeConjurar, "o silencio tem prazo");
                Andar(k, s["aberto"]);
                Assert.IsNull(((Ceifadora)k.Impl).RasgoAberto, "o rasgo cicatriza em 3s");
                var tarde = new FakeEntidade("tarde", new Vector3(0.2f, 0f, 0f));
                c.Arena.Add(tarde);
                k.Tick(0.1f);
                Assert.AreEqual(0f, tarde.Pos.z, 1e-4f, "rasgo fechado nao puxa ninguem");
                Andar(k, s["revelado"]);
                Assert.IsFalse(k.EstadoAtivo(Ceifadora.REVELADO), "o revelado expira sozinho");
            }
            finally { Ceifadora.Teleportar = velho; }
        }

        [Test]
        public void Ceifadora_Ecos_QuemCaiNoRaioDeixaOReplayDoPassado_ForaDoRaioNao_SomeEm60s()
        {
            var c = new FakeConjurador("ceifa", Vector3.zero);
            var k = new KitRunner("02-ceifadora", c);
            var ceifa = (Ceifadora)k.Impl;
            Dictionary<string, float> p = k.Dados.Passiva;
            var vitima = new FakeEntidade("vitima", new Vector3(5f, 0f, 0f));
            var fugiu = new FakeEntidade("fugiu", new Vector3(-5f, 0f, 0f));
            c.Arena.Add(c); c.Arena.Add(vitima); c.Arena.Add(fugiu);
            for (int i = 0; i < 12; i++) { vitima.Pos += new Vector3(0f, 0f, 0.5f); k.Tick(p["eco_amostra"]); }   // 3s de luta
            fugiu.Pos = new Vector3(-(p["eco_raio"] + 30f), 0f, 0f);   // saiu do raio e demorou a cair
            for (int i = 0; i < 3; i++) { vitima.Pos += new Vector3(0f, 0f, 0.5f); k.Tick(p["eco_amostra"]); }
            Assert.AreEqual(0, ceifa.Ecos.Count, "vivo nao deixa eco");
            Vector3 queda = vitima.Pos;
            vitima.Vital.Hp = 0f;
            fugiu.Vital.Hp = 0f;
            k.Tick(0.05f);
            Assert.AreEqual(1, ceifa.Ecos.Count, "quem caiu NO RAIO deixa o eco; quem caiu longe, nao");
            Ceifadora.Eco eco = ceifa.Ecos[0];
            Assert.AreEqual("ceifadora_eco", eco.Visual.Tipo);
            Assert.AreEqual(queda, eco.Visual.Pos, "o eco fica onde ele caiu");
            Assert.AreEqual(p["eco_janela"], eco.Visual.Duracao, 1e-4f, "e dura 60s");
            Assert.Less(eco.Caminho[0].z, queda.z - 4f, "o replay comeca ~3s ANTES da queda (passado, nunca o agora)");
            Assert.AreEqual(eco.Caminho[0], eco.Em(0f), "o vulto roda do passado...");
            Assert.AreEqual(queda.z, eco.Em(p["eco_replay"] - 0.001f).z, 0.05f, "...ate' a queda, em laco");
            Andar(k, p["eco_janela"] + 0.5f, 0.5f);
            Assert.AreEqual(0, ceifa.Ecos.Count, "o eco some depois de 60s");
        }

        // ================================================================== 04 CORVUS

        [Test]
        public void Corvus_Uivo_SoAcendeQuemAnda_ParadoEscapa_EOUivoDenunciaEle()
        {
            var c = new FakeConjurador("corvus", Vector3.zero);
            var k = new KitRunner("04-corvus", c);
            var corvus = (Corvus)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            var andando = new FakeEntidade("andando", new Vector3(10f, 0f, 0f));
            var parado = new FakeEntidade("parado", new Vector3(-10f, 0f, 0f));
            var fora = new FakeEntidade("fora", new Vector3(t["raio"] + 5f, 0f, 0f));
            c.Arena.AddRange(new IEntidade[] { c, andando, parado, fora });
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual(1, _disparos, "o uivo DENUNCIA ele: os bots escutam como um disparo");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "corvus_uivo" && v.Raio == t["raio"]), "o anel do uivo (forma, nao so' som)");
            for (int i = 0; i < 5; i++)
            {
                andando.Pos += new Vector3(0f, 0f, 0.5f);
                fora.Pos += new Vector3(0f, 0f, 0.5f);
                k.Tick(0.1f);
            }
            Assert.IsTrue(corvus.Acesos.Any(a => a.Alvo == andando), "quem ANDA no raio fica com o cheiro aceso");
            Assert.IsFalse(corvus.Acesos.Any(a => a.Alvo == parado), "PARADO esconde do uivo (disciplina)");
            Assert.IsFalse(corvus.Acesos.Any(a => a.Alvo == fora), "fora dos 25m ninguem ouve");
            EfeitoVisual cheiro = k.Visuais.First(v => v.Tipo == "corvus_cheiro" && v.Alvo == andando);
            Assert.Greater(cheiro.Raio, 0f, "andando: o contorno acende");
            k.Tick(0.1f);
            Assert.AreEqual(0f, cheiro.Raio, "parou: o contorno apaga (so' ENQUANTO se move)");
            andando.Pos += new Vector3(0f, 0f, 0.5f);
            k.Tick(0.1f);
            Assert.Greater(cheiro.Raio, 0f, "voltou a andar: acende de novo");
            Andar(k, t["aceso_dur"]);
            Assert.IsFalse(k.Visuais.Any(v => v.Tipo == "corvus_cheiro"), "o cheiro aceso dura 4s");
        }

        [Test]
        public void Corvus_MundoDeCheiros_FitaDoInimigo_SoOPassado()
        {
            var c = new FakeConjurador("corvus", Vector3.zero);
            var k = new KitRunner("04-corvus", c);
            var corvus = (Corvus)k.Impl;
            Dictionary<string, float> p = k.Dados.Passiva;
            var presa = new FakeEntidade("presa", new Vector3(5f, 0f, 0f));
            var longe = new FakeEntidade("longe", new Vector3(p["trilha_raio"] + 10f, 0f, 0f));
            c.Arena.AddRange(new IEntidade[] { c, presa, longe });
            for (int i = 0; i < 6; i++) { presa.Pos += new Vector3(0f, 0f, 1f); k.Tick(p["trilha_amostra"]); }
            Assert.IsTrue(corvus.Fareja(presa), "fareja quem passa no raio");
            Assert.IsFalse(corvus.Fareja(longe), "so' no raio");
            EfeitoVisual fita = k.Visuais.First(v => v.Tipo == "corvus_trilha" && v.Alvo == presa);
            Assert.AreEqual(p["trilha_janela"], fita.Duracao, "a fita cobre 60s");
            var pts = new Vector3[64];
            int n = corvus.PontosDaTrilha(presa, p["trilha_atraso"], p["trilha_janela"], pts);
            Assert.GreaterOrEqual(n, 3, "a fita tem os passos de antes");
            Assert.Less(pts[n - 1].z, presa.Pos.z - 1.5f, "a ponta fica 2s ATRAS: passado, nunca o agora");
        }

        [Test]
        public void Corvus_Lobisomem_AvisaComUivo_CorreRasgaECuraAoAbater_SemMagias_EDesmanchaOfegante()
        {
            var c = new FakeConjurador("corvus", Vector3.zero);
            var k = new KitRunner("04-corvus", c);
            Dictionary<string, float> s = k.Dados.Suprema;
            var presa = new FakeEntidade("presa", new Vector3(0f, 0f, 1.5f));
            c.Arena.Add(c); c.Arena.Add(presa);
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            k.Tick(0.1f);
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "corvus_uivo" && v.Raio == s["uivo_raio"]), "o uivo de transformacao e' ouvido a 60m");
            Assert.AreEqual(1, _disparos, "e os bots escutam (a telegrafia com som)");
            float ehp = Ehp(presa);
            Andar(k, k.Dados.Telegrafia - 0.3f);
            Assert.IsFalse(k.EstadoAtivo(Corvus.FERA), "durante o aviso nada muda");
            Assert.AreEqual(ehp, Ehp(presa), "nem garra");
            Andar(k, 0.3f);
            Assert.IsTrue(k.EstadoAtivo(Corvus.FERA), "a fera sai DEPOIS do aviso");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "corvus_penas"), "o corpo estoura em penas");
            Assert.AreEqual(s["buff_vel"], Efeitos.De(c).StatusMult, 0.001f, "corre 40% mais");
            Assert.IsFalse(k.PodeConjurar, "SEM MAGIAS: nem tiro...");
            Assert.IsFalse(k.UsarTatica(), "...nem tatica");
            Andar(k, 0.4f);
            Assert.Less(Ehp(presa), ehp, "as garras rasgam quem encosta");
            Assert.Greater(c.Vital.DanoCausado, 0f, "a garra credita o escudo (fonte = ele)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "corvus_garra" && v.Alvo == presa), "o rasgo se ve' na presa");
            c.Vital.Hp = 50f;
            presa.Vital.Escudo = 0f;
            presa.Vital.Hp = 1f;
            Andar(k, s["garra_cadencia"] + 0.1f);
            Assert.IsFalse(presa.Vital.Viva);
            Assert.AreEqual(50f + s["cura_abate"], c.Vital.Hp, 0.01f, "cura ao abater");
            Andar(k, s["duracao"]);
            Assert.IsFalse(k.EstadoAtivo(Corvus.FERA), "duracao fixa de 30s");
            Assert.IsTrue(k.EstadoAtivo(Corvus.OFEGANTE), "desmancha ofegante");
            Assert.AreEqual(s["ofegante_vel"], Efeitos.De(c).StatusMult, 0.001f, "2s lento");
            Assert.IsFalse(k.PodeConjurar, "e ainda sem conjurar");
            Andar(k, s["ofegante_dur"] + 0.2f);
            Assert.IsTrue(k.PodeConjurar, "o preco tem prazo");
        }

        // ================================================================== 05 CORVOMANTE

        [Test]
        public void Corvomante_VooDoOlho_CorpoParadoESemConjurar_OCorvoMarcaQuemVe_BateAsas_E60DeVida()
        {
            var c = new FakeConjurador("corvomante", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("05-corvomante", c);
            var cm = (Corvomante)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            Dictionary<string, float> p = k.Dados.Passiva;
            var longe = new FakeEntidade("longe", new Vector3(0f, 0f, 40f));   // longe do corpo, no caminho do corvo
            var lado = new FakeEntidade("lado", new Vector3(p["visao"] + 5f, 0f, 20f));   // perto do voo, mas a 25m de lado
            c.Arena.AddRange(new IEntidade[] { c, longe, lado });
            Andar(k, 0.5f);
            Assert.IsFalse(cm.Marcado(longe), "corvo no ombro: a passiva nao marca ninguem");
            Assert.IsTrue(k.UsarTatica());
            Assert.AreEqual(Velocidade.Piso, Efeitos.De(c).StatusMult, 1e-4f, "o CORPO fica parado");
            Assert.IsFalse(k.PodeConjurar, "e indefeso: sem conjurar");
            Andar(k, 2.5f);
            Assert.Greater(cm.CorvoEmVoo.Pos.z, 20f, "o corvo voa na mira");
            Assert.IsTrue(cm.Marcado(longe), "o corvo MARCA quem ve' (a passiva)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "corvomante_marca" && v.Alvo == longe));
            Assert.IsFalse(cm.Marcado(lado), "so' a 20m dele");
            Assert.GreaterOrEqual(_disparos, 2, "bate asas audivelmente: os bots ouvem o CORVO");
            // o LIMITADOR: 60 de vida — tiro inimigo que passa por ele doi e e' engolido
            var atirador = new FakeEntidade("bot", new Vector3(30f, 0f, 30f));
            var tiros = new List<Projetil>();
            Projetil um = Projetil.Lancar(atirador, cm.CorvoEmVoo.Pos, Vector3.left, Elemento.Fogo);
            int precisa = Mathf.CeilToInt(t["corvo_vida"] / um.Dano);
            tiros.Add(um);
            for (int i = 1; i < precisa - 1; i++) tiros.Add(Projetil.Lancar(atirador, cm.CorvoEmVoo.Pos, Vector3.left, Elemento.Fogo));
            k.ProjeteisVivos = () => tiros;
            k.Tick(0.01f);
            Assert.IsNotNull(cm.CorvoEmVoo, "um tiro a menos que a vida: o corvo aguenta");
            tiros.Add(Projetil.Lancar(atirador, cm.CorvoEmVoo.Pos, Vector3.left, Elemento.Fogo));
            k.Tick(0.01f);
            Assert.IsNull(cm.CorvoEmVoo, "60 de vida: o corvo cai");
            Assert.AreEqual(0, tiros.Count, "e engoliu os tiros que o acertaram");
            Assert.IsFalse(k.PodeConjurar, "abatido, o corpo segue parado ate' o fim do voo");
            Andar(k, t["duracao"]);
            Assert.IsTrue(k.PodeConjurar, "o voo tem prazo: 4s");
        }

        [Test]
        public void Corvomante_Grasnido_EspiralNoAviso_NoPontoDoAviso_SoNoEscudoDoInimigo_EOCorvoExausto()
        {
            var c = new FakeConjurador("corvomante", Vector3.zero);
            var k = new KitRunner("05-corvomante", c);
            var cm = (Corvomante)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var cheio = new FakeEntidade("cheio", new Vector3(4f, 0f, 0f));
            var fraco = new FakeEntidade("fraco", new Vector3(-4f, 0f, 0f));
            fraco.Vital.Escudo = 20f;
            var aliado = new FakeConjurador("aliado", new Vector3(0f, 0f, 4f));
            var fora = new FakeEntidade("fora", new Vector3(s["raio"] + 3f, 0f, 0f));
            c.Arena.AddRange(new IEntidade[] { c, cheio, fraco, aliado, fora });
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            Andar(k, k.Dados.Telegrafia - 0.2f);
            Assert.IsTrue(cm.CorvoFora, "no aviso o corvo sobe em espiral");
            Assert.AreEqual(50f, cheio.Vital.Escudo, "durante o aviso nada acontece");
            c.Pos = new Vector3(0f, 0f, -30f);   // ele saiu andando: o pulso vale no ponto do AVISO
            Andar(k, 0.3f);
            Assert.AreEqual(0f, cheio.Vital.Escudo, 0.01f, "50 no escudo");
            Assert.AreEqual(100f, cheio.Vital.Hp, 0.01f, "SO' no escudo: nada vaza para a vida");
            Assert.AreEqual(0f, fraco.Vital.Escudo, 0.01f, "escudo de 20 perde 20...");
            Assert.AreEqual(100f, fraco.Vital.Hp, 0.01f, "...e a vida fica");
            Assert.AreEqual(50f, aliado.Vital.Escudo, "aliado (esquadrao do player) nao apanha");
            Assert.AreEqual(50f, fora.Vital.Escudo, "fora do raio nao apanha");
            Assert.AreEqual(50f, c.Vital.Escudo, "nem ele");
            Assert.Greater(c.Vital.DanoCausado, 0f, "o pulso credita a evolucao (fonte = ele)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "corvomante_grasnido"), "o anel de penas de luz");
            Assert.IsFalse(cm.CorvoFora, "o corvo desceu");
            Assert.IsTrue(k.EstadoAtivo(Corvomante.EXAUSTO), "o corvo despenca exausto");
            Assert.IsFalse(k.UsarTatica(), "5s sem tatica");
            Andar(k, s["exausto_dur"] + 0.2f);
            Assert.IsTrue(k.UsarTatica(), "descansou, voa de novo");
        }

        // ================================================================== 06 OLHO-DE-ETER

        [Test]
        public void OlhoDeEter_PoDeEter_SoQuemConjurou_Ate40m_SomeEm5s()
        {
            var c = new FakeConjurador("olho", Vector3.zero);
            var k = new KitRunner("06-olho-de-eter", c);
            var olho = (OlhoDeEter)k.Impl;
            Dictionary<string, float> p = k.Dados.Passiva;
            var atirou = new FakeEntidade("atirou", new Vector3(20f, 0f, 0f));
            var quieto = new FakeEntidade("quieto", new Vector3(10f, 0f, 0f));
            var longe = new FakeEntidade("longe", new Vector3(p["raio"] + 10f, 0f, 0f));
            var tiros = new List<Projetil>
            {
                Projetil.Lancar(atirou, atirou.Pos, Vector3.left, Elemento.Fogo),
                Projetil.Lancar(longe, longe.Pos, Vector3.left, Elemento.Fogo),
                Projetil.Lancar(c, c.Pos, Vector3.right, Elemento.Fogo),
            };
            k.ProjeteisVivos = () => tiros;
            k.Tick(0.15f);
            Assert.IsTrue(olho.TemPo(atirou), "quem CONJUROU carrega o po de eter");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "olho_po" && v.Alvo == atirou));
            Assert.IsFalse(olho.TemPo(quieto), "quem segura a magia nao aparece (a contra-jogada)");
            Assert.IsFalse(olho.TemPo(longe), "so' ate' 40m");
            Assert.IsFalse(olho.TemPo(c), "o proprio tiro nao conta");
            tiros.Clear();
            Andar(k, p["janela"] + 0.2f);
            Assert.IsFalse(olho.TemPo(atirou), "o po some 5s depois da ultima conjuracao");
        }

        [Test]
        public void OlhoDeEter_Enxame_AtrasoDe1_4s_TunelEstreito_InterrompeERevela_EFogoLimpa()
        {
            var c = new FakeConjurador("olho", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("06-olho-de-eter", c);
            var olho = (OlhoDeEter)k.Impl;
            Dictionary<string, float> t = k.Dados.Tatica;
            var noTunel = new FakeConjurador("bot", new Vector3(0.8f, 0f, 15f), false);
            var fora = new FakeConjurador("bot2", new Vector3(t["tunel"] + 1.5f, 0f, 10f), false);
            c.Arena.AddRange(new IEntidade[] { c, noTunel, fora });
            Assert.IsTrue(k.UsarTatica());
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "olho_enxame"), "o tunel aparece no toque (o aviso)");
            Andar(k, t["atraso"] - 0.2f);
            Assert.IsFalse(noTunel.Estados.ContainsKey("silencio"), "1,4s de atraso: ninguem tocado ainda");
            Andar(k, 0.2f + 15f / t["vel"] + 0.2f);
            Assert.IsTrue(noTunel.Estados.ContainsKey("silencio"), "tocado: a conjuracao e' INTERROMPIDA");
            Assert.AreEqual(t["interrompe"], noTunel.Estados["silencio"], 1e-4f);
            Assert.IsTrue(olho.Marcado(noTunel), "e fica revelado (mariposas pousadas)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "olho_mariposas" && v.Alvo == noTunel && v.Duracao == t["revela_dur"]), "por 6s");
            Assert.IsFalse(fora.Estados.ContainsKey("silencio"), "tunel estreito: fora do eixo escapa");
            Assert.IsFalse(olho.Marcado(fora));
            Bus.EmitTerrainHit(Elemento.Agua, noTunel.Pos, false);
            Assert.IsTrue(olho.Marcado(noTunel), "agua nao queima mariposa");
            Bus.EmitTerrainHit(Elemento.Fogo, noTunel.Pos + new Vector3(1f, 0f, 0f), false);
            Assert.IsFalse(olho.Marcado(noTunel), "FOGO perto queima as mariposas e limpa a marca (§14)");
        }

        [Test]
        public void OlhoDeEter_Crisalida_CasuloNoAviso_EclodeERevelaTodosNoRaio_Cego1s_EDestruidoNaoEclode()
        {
            var c = new FakeConjurador("olho", Vector3.zero) { DirecaoDaMira = Vector3.forward };
            var k = new KitRunner("06-olho-de-eter", c);
            var olho = (OlhoDeEter)k.Impl;
            Dictionary<string, float> s = k.Dados.Suprema;
            var a = new FakeEntidade("a", new Vector3(20f, 0f, 0f));
            var b = new FakeEntidade("b", new Vector3(-10f, 0f, 15f));
            var fora = new FakeEntidade("fora", new Vector3(0f, 0f, s["raio"] + 3f));
            c.Arena.AddRange(new IEntidade[] { c, a, b, fora });
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            k.Tick(0.1f);
            Assert.IsNotNull(olho.CasuloPlantado, "o casulo nasce NO AVISO (e' ele que pulsa)");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "olho_casulo"));
            Assert.IsFalse(olho.Marcado(a), "antes de eclodir, nada");
            Andar(k, k.Dados.Telegrafia);
            Assert.IsTrue(olho.Marcado(a) && olho.Marcado(b), "eclodiu: mariposas em TODO inimigo no raio");
            Assert.IsFalse(olho.Marcado(fora), "so' no raio");
            Assert.IsTrue(k.Visuais.Any(v => v.Tipo == "olho_eclosao"), "a explosao de po dourado");
            Assert.IsTrue(k.EstadoAtivo(OlhoDeEter.CEGO), "e ele fica 1s cego");
            var tiros = new List<Projetil> { Projetil.Lancar(a, a.Pos, Vector3.left, Elemento.Fogo) };
            k.ProjeteisVivos = () => tiros;
            k.Tick(0.2f);
            Assert.IsFalse(olho.TemPo(a), "cego: a passiva nao sente");
            Andar(k, s["cego_dur"] + 0.2f);
            Assert.IsTrue(olho.TemPo(a), "a traducao volta");

            // o LIMITADOR: o casulo e' destrutivel ANTES de eclodir (60 de vida)
            tiros.Clear();
            var d = new FakeEntidade("d", new Vector3(5f, 0f, 5f));
            c.Arena.Add(d);
            Carregar(k);
            Assert.IsTrue(k.UsarSuprema());
            k.Tick(0.1f);
            Vector3 casulo = olho.CasuloPlantado.Pos;
            Bus.EmitTerrainHit(Elemento.Terra, casulo, false);
            Assert.IsTrue(olho.CasuloPlantado.Inteiro, "um impacto so' racha");
            for (int i = 0; i < 3; i++) Bus.EmitTerrainHit(Elemento.Terra, casulo, false);
            Assert.IsFalse(olho.CasuloPlantado.Inteiro, "60 de vida: quebrou");
            Andar(k, k.Dados.Telegrafia);
            Assert.IsFalse(olho.Marcado(d), "casulo destruido: nada eclode (o preco)");
        }
    }
}
