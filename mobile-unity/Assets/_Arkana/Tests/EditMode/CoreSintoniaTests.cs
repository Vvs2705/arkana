using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// A SINTONIA (GDD §9 + PONTE A5/A15): a matriz dos 10 pares, o funil (aliado, outro conjurador, elemento diferente,
    /// janela, raio), a canalizacao, o custo cobrado no INICIO e o reembolso so' para quem segue de pe'.
    /// </summary>
    public class CoreSintoniaTests
    {
        private const float Eps = 0.001f;
        private const int DUPLA = 1;
        private FakeEntidade _a, _b;
        private List<string> _log;   // a ORDEM de tudo: canal, efeito, disparou, falhou
        private List<(ComboSintonia c, IEntidade a, IEntidade b, Vector3 p, float dur)> _canais;
        private List<DisparoSintonia> _efeitos, _disparos;
        private List<(ComboSintonia c, IEntidade a, IEntidade b, Vector3 p)> _falhas;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Combat.Reset();
            Derrubado.Reset();
            Sintonia.Reset();
            _a = new FakeEntidade("a", Vector3.zero);
            _b = new FakeEntidade("b", new Vector3(3f, 0f, 0f));
            Combat.DefinirTime(_a, DUPLA);
            Combat.DefinirTime(_b, DUPLA);
            _log = new List<string>();
            _canais = new List<(ComboSintonia, IEntidade, IEntidade, Vector3, float)>();
            _efeitos = new List<DisparoSintonia>();
            _disparos = new List<DisparoSintonia>();
            _falhas = new List<(ComboSintonia, IEntidade, IEntidade, Vector3)>();
            Bus.SintoniaCanalizando += (c, a, b, p, d) => { _log.Add("canal"); _canais.Add((c, a, b, p, d)); };
            Bus.SintoniaDisparou += d => { _log.Add("disparou"); _disparos.Add(d); };
            Bus.SintoniaFalhou += (c, a, b, p) => { _log.Add("falhou"); _falhas.Add((c, a, b, p)); };
            Sintonia.Efeito = d => { _log.Add("efeito"); _efeitos.Add(d); };
        }

        [TearDown]
        public void TearDown() { Sintonia.Reset(); Derrubado.Reset(); Combat.Reset(); Bus.Reset(); }

        private static void Impacto(IEntidade quem, Elemento el, Vector3 p, IEntidade alvo = null, float dano = 10f) =>
            Sintonia.RegistrarImpacto(quem, el, p, alvo, dano);

        /// <summary>A dupla abre um Tornado Flamejante no chao perto da origem.</summary>
        private void AbrirTornado()
        {
            Impacto(_a, Elemento.Fogo, new Vector3(1f, 0f, 0f));
            Impacto(_b, Elemento.Vento, new Vector3(2f, 0f, 0f));
            Assert.AreEqual(1, _canais.Count, "a dupla canaliza");
        }

        [Test]
        public void Matriz_DezPares_Comutativos_E_MesmoElementoNaoFunde()
        {
            var tabela = new (Elemento x, Elemento y, ComboSintonia c)[]
            {
                (Elemento.Fogo, Elemento.Vento, ComboSintonia.TornadoFlamejante),
                (Elemento.Fogo, Elemento.Terra, ComboSintonia.ChuvaDeMagma),
                (Elemento.Fogo, Elemento.Raio, ComboSintonia.ExplosaoDePlasma),
                (Elemento.Fogo, Elemento.Agua, ComboSintonia.CortinaDeVapor),
                (Elemento.Agua, Elemento.Raio, ComboSintonia.Eletrocussao),
                (Elemento.Agua, Elemento.Terra, ComboSintonia.Lamacal),
                (Elemento.Agua, Elemento.Vento, ComboSintonia.TempestadeTorrencial),
                (Elemento.Terra, Elemento.Vento, ComboSintonia.TempestadeDeAreia),
                (Elemento.Terra, Elemento.Raio, ComboSintonia.CristaisCarregados),
                (Elemento.Vento, Elemento.Raio, ComboSintonia.NuvemTempestuosa),
            };
            foreach (var t in tabela)
            {
                Assert.AreEqual(t.c, Sintonia.ComboDe(t.x, t.y), t.x + "+" + t.y);
                Assert.AreEqual(t.c, Sintonia.ComboDe(t.y, t.x), t.y + "+" + t.x + ": a ordem nao importa");
            }
            foreach (Elemento e in Elementos.Todos) Assert.IsNull(Sintonia.ComboDe(e, e), e + " com " + e + " nao funde");

            var nomes = new HashSet<string>();
            foreach (ComboSintonia c in Enum.GetValues(typeof(ComboSintonia)))
            {
                Assert.Greater(Balance.Sintonia.Raio(c), 0f, c + ": raio");
                Assert.Greater(Balance.Sintonia.Duracao(c), 0f, c + ": duracao");
                Assert.IsTrue(nomes.Add(Textos.ComboNome(c)), c + ": nome proprio");
            }
            Assert.AreEqual(10, nomes.Count);
            Assert.AreEqual("EXPLOSÃO DE PLASMA", Textos.ComboNome(ComboSintonia.ExplosaoDePlasma));
            Assert.Greater(Balance.Sintonia.Mult, 1f, "mais forte que a soma das partes");
            Assert.Greater(Balance.Sintonia.CooldownS, Balance.Sintonia.JanelaS, "a recarga e' o anti-spam");
        }

        [Test]
        public void Aliados_ElementosDiferentes_NaJanela_E_NoRaio_Canalizam_E_DisparamUmaVez()
        {
            var alvo = new FakeEntidade("alvo", new Vector3(10f, 0f, 4f));   // sem time: inimigo de todo mundo
            Impacto(_a, Elemento.Fogo, new Vector3(8f, 0f, 4f), null, 11f);   // no chao, sem par: fica pendente
            Assert.AreEqual(0, _log.Count, "um impacto sozinho nao faz nada");
            Sintonia.Tick(Balance.Sintonia.JanelaS - 0.5f);   // 1,0 s: exato em binario
            Impacto(_b, Elemento.Vento, alvo.Pos + Vector3.up * 1.2f, alvo, 7.5f);   // no corpo: conta no pe' do alvo

            Assert.AreEqual(1, _canais.Count);
            var ch = _canais[0];
            Assert.AreEqual(ComboSintonia.TornadoFlamejante, ch.c);
            Assert.AreSame(_a, ch.a, "A = o do 1o impacto");
            Assert.AreSame(_b, ch.b);
            Assert.AreEqual(9f, ch.p.x, Eps, "centro = media dos dois impactos");
            Assert.AreEqual(0f, ch.p.y, Eps, "no chao");
            Assert.AreEqual(4f, ch.p.z, Eps);
            Assert.AreEqual(Balance.Sintonia.CanalizacaoS, ch.dur, Eps);
            Assert.IsTrue(Sintonia.Canalizando(_a) && Sintonia.Canalizando(_b));
            Assert.AreEqual(Balance.Sintonia.CooldownS, Sintonia.CooldownRestante(_a), Eps, "custo cobrado no INICIO");
            Assert.AreEqual(Balance.Sintonia.CooldownS, Sintonia.CooldownRestante(_b), Eps, "nos DOIS");

            Sintonia.Tick(Balance.Sintonia.CanalizacaoS - 0.25f);
            Assert.AreEqual(0, _efeitos.Count, "canalizando ainda");
            Sintonia.Tick(0.25f);
            CollectionAssert.AreEqual(new[] { "canal", "efeito", "disparou" }, _log, "efeito UMA vez, e so' depois o evento");
            DisparoSintonia d = _disparos[0];
            Assert.AreEqual(ComboSintonia.TornadoFlamejante, d.Combo);
            Assert.AreSame(_a, d.A);
            Assert.AreSame(_b, d.B);
            Assert.AreEqual(Elemento.Fogo, d.ElA);
            Assert.AreEqual(Elemento.Vento, d.ElB);
            Assert.AreSame(alvo, d.Alvo, "o alvo do 2o impacto");
            Assert.AreEqual((11f + 7.5f) * Balance.Sintonia.Mult, d.Dano, Eps, "soma x Mult");
            Assert.AreEqual(ch.p, d.Ponto);
            Assert.AreEqual(d.Dano, _efeitos[0].Dano, Eps, "o Efeito recebe o mesmo disparo");
            Assert.IsFalse(Sintonia.Canalizando(_a) || Sintonia.Canalizando(_b));

            Sintonia.Tick(5f);
            Assert.AreEqual(1, _efeitos.Count, "o canal saiu: nada de repetir");
            Assert.AreEqual(1, _disparos.Count);
            Assert.AreEqual(Balance.Sintonia.CooldownS - Balance.Sintonia.CanalizacaoS - 5f, Sintonia.CooldownRestante(_a), Eps, "a recarga corre no relogio do Tick");
        }

        [Test]
        public void ForaDaJanela_NaoCanaliza()
        {
            Impacto(_a, Elemento.Fogo, Vector3.zero);
            Sintonia.Tick(Balance.Sintonia.JanelaS + 0.1f);
            Impacto(_b, Elemento.Vento, Vector3.zero);
            Assert.AreEqual(0, _canais.Count, "o 1o impacto ja' expirou");
            Assert.AreEqual(0f, Sintonia.CooldownRestante(_a));
        }

        [Test]
        public void Longe_NaoCanaliza_MasAlturaNaoSepara()
        {
            Impacto(_a, Elemento.Fogo, Vector3.zero);
            Impacto(_b, Elemento.Vento, new Vector3(0f, 0f, Balance.Sintonia.RaioAlvoM + 0.5f));
            Assert.AreEqual(0, _canais.Count, "dois alvos longe nao sao 'mesmo alvo'");
            Assert.AreEqual(0f, Sintonia.CooldownRestante(_a) + Sintonia.CooldownRestante(_b));

            Sintonia.Reset();
            Impacto(_a, Elemento.Fogo, Vector3.zero);
            Impacto(_b, Elemento.Vento, new Vector3(1f, 20f, 0f));   // topo do morro/muro em cima do mesmo ponto
            Assert.AreEqual(1, _canais.Count, "a distancia e' no plano");
        }

        [Test]
        public void MesmoElemento_NaoFunde_E_NaoCobra()
        {
            Impacto(_a, Elemento.Fogo, Vector3.zero);
            Impacto(_b, Elemento.Fogo, Vector3.zero);
            Assert.AreEqual(0, _canais.Count);
            Assert.AreEqual(0f, Sintonia.CooldownRestante(_a) + Sintonia.CooldownRestante(_b));
        }

        [Test]
        public void Inimigos_NaoEntramNoCombo()
        {
            var inimigo = new FakeEntidade("inimigo", new Vector3(1f, 0f, 0f));
            Combat.DefinirTime(inimigo, DUPLA + 1);
            var semTime = new FakeEntidade("solo", new Vector3(1f, 0f, 1f));   // FFA: cada um sozinho
            Impacto(_a, Elemento.Fogo, Vector3.zero);
            Impacto(inimigo, Elemento.Vento, Vector3.zero);
            Impacto(semTime, Elemento.Terra, Vector3.zero);
            Assert.AreEqual(0, _canais.Count, "inimigo perto conjurando outro elemento nao funde");
            Assert.AreEqual(0f, Sintonia.CooldownRestante(_a) + Sintonia.CooldownRestante(inimigo), "nem queima recarga de ninguem");
        }

        [Test]
        public void MesmoConjurador_NaoCombinaConsigo()
        {
            Impacto(_a, Elemento.Fogo, Vector3.zero);
            Impacto(_a, Elemento.Vento, Vector3.zero);   // manopla de dois elementos
            Assert.AreEqual(0, _canais.Count);
        }

        [Test]
        public void DoisPendentes_DoMesmoConjurador_ValeOMaisRecente()
        {
            Impacto(_a, Elemento.Fogo, Vector3.zero);
            Sintonia.Tick(0.2f);
            Impacto(_a, Elemento.Fogo, new Vector3(2f, 0f, 0f));
            Impacto(_b, Elemento.Vento, new Vector3(4f, 0f, 0f));
            Assert.AreEqual(1, _canais.Count);
            Assert.AreEqual(3f, _canais[0].p.x, Eps, "par com o impacto mais recente");
        }

        [Test]
        public void Derrubado_NoMeio_Falha_E_ReembolsaSoQuemSegueDePe()
        {
            AbrirTornado();
            new Derrubado(_b).Cair(null);
            Sintonia.Tick(0.1f);
            Assert.AreEqual(1, _falhas.Count, "caiu no meio: FALHOU");
            Assert.AreEqual(ComboSintonia.TornadoFlamejante, _falhas[0].c);
            Assert.AreSame(_a, _falhas[0].a);
            Assert.AreSame(_b, _falhas[0].b);
            Assert.AreEqual(_canais[0].p, _falhas[0].p);
            Assert.AreEqual(0f, Sintonia.CooldownRestante(_a), "quem continua de pe' nao paga (PONTE A5)");
            Assert.AreEqual(Balance.Sintonia.CooldownS - 0.1f, Sintonia.CooldownRestante(_b), Eps, "quem caiu segue pagando");
            Assert.IsFalse(Sintonia.Canalizando(_a) || Sintonia.Canalizando(_b));
            Sintonia.Tick(Balance.Sintonia.CanalizacaoS);
            Assert.AreEqual(0, _efeitos.Count + _disparos.Count, "canal que falhou nunca dispara");
        }

        [Test]
        public void Morto_NoMeio_Falha_E_ReembolsaSoQuemSegueDePe()
        {
            AbrirTornado();
            _a.Vital.Hp = 0f;
            Sintonia.Tick(0.1f);
            Assert.AreEqual(1, _falhas.Count);
            Assert.AreEqual(0f, Sintonia.CooldownRestante(_b), "o parceiro vivo recebe de volta");
            Assert.Greater(Sintonia.CooldownRestante(_a), 0f, "o morto nao ganha reembolso");
            Assert.AreEqual(0, _efeitos.Count);
        }

        [Test]
        public void EmRecarga_OuCanalizando_NaoAbreCombo()
        {
            var c = new FakeEntidade("c", new Vector3(0f, 0f, 2f));
            Combat.DefinirTime(c, DUPLA);   // trio: o terceiro ajuda a provar que o bloqueio e' do conjurador
            AbrirTornado();
            Impacto(c, Elemento.Terra, Vector3.zero);
            Impacto(_a, Elemento.Raio, Vector3.zero);
            Assert.AreEqual(1, _canais.Count, "canalizando nao abre outro");

            Sintonia.Tick(Balance.Sintonia.CanalizacaoS);
            Assert.AreEqual(1, _disparos.Count);
            Impacto(c, Elemento.Terra, Vector3.zero);
            Impacto(_a, Elemento.Raio, Vector3.zero);
            Impacto(_b, Elemento.Agua, Vector3.zero);
            Assert.AreEqual(1, _canais.Count, "em recarga nao abre");

            Sintonia.Tick(Balance.Sintonia.CooldownS);
            Impacto(_a, Elemento.Raio, Vector3.zero);
            Impacto(c, Elemento.Terra, Vector3.zero);
            Assert.AreEqual(2, _canais.Count, "a recarga acabou: combina de novo");
            Assert.AreEqual(ComboSintonia.CristaisCarregados, _canais[1].c);
        }

        [Test]
        public void Reset_LimpaTudo()
        {
            AbrirTornado();
            Sintonia.Reset();
            Assert.IsFalse(Sintonia.Canalizando(_a) || Sintonia.Canalizando(_b));
            Assert.AreEqual(0f, Sintonia.CooldownRestante(_a) + Sintonia.CooldownRestante(_b), "recarga nao atravessa partida");
            Assert.IsNull(Sintonia.Efeito, "o gancho sai junto (a 17C reinstala no Iniciar)");
            Sintonia.Tick(Balance.Sintonia.CanalizacaoS * 2f);
            Assert.AreEqual(0, _disparos.Count + _falhas.Count, "canal apagado nao dispara nem falha");

            Impacto(_a, Elemento.Fogo, Vector3.zero);
            Sintonia.Reset();
            Impacto(_b, Elemento.Vento, Vector3.zero);
            Assert.AreEqual(1, _canais.Count, "pendente apagado: so' o canal de antes do Reset");
        }

        [Test]
        public void DuplaInimiga_DeBots_FazOProprioCombo()
        {
            var player = new FakeEntidade("player", new Vector3(1f, 0f, 1f), true);
            Combat.DefinirTime(player, Combat.TIME_DO_PLAYER);
            var x = new FakeEntidade("x", new Vector3(20f, 0f, 0f));
            var y = new FakeEntidade("y", new Vector3(22f, 0f, 0f));
            Combat.DefinirTime(x, 2);
            Combat.DefinirTime(y, 2);
            Impacto(x, Elemento.Agua, player.Pos + Vector3.up, player);
            Impacto(y, Elemento.Raio, player.Pos + Vector3.up, player);
            Assert.AreEqual(1, _canais.Count, "a regra nao sabe quem e' humano");
            Assert.AreEqual(ComboSintonia.Eletrocussao, _canais[0].c);
            Sintonia.Tick(Balance.Sintonia.CanalizacaoS);
            Assert.AreEqual(1, _disparos.Count);
            Assert.AreSame(player, _disparos[0].Alvo);
            Assert.AreSame(x, _disparos[0].A);
        }

        [Test]
        public void Projetil_Impacto_NoChao_E_NoCorpo_AlimentaASintonia()
        {
            var alvo = new FakeEntidade("alvo", new Vector3(2f, 0f, 0f));
            Projetil noChao = Projetil.Lancar(_a, new Vector3(0f, 0f, 0f), Vector3.right, Elemento.Fogo);
            Projetil noCorpo = Projetil.Lancar(_b, new Vector3(1.5f, 1.1f, 0f), Vector3.right, Elemento.Raio);
            noChao.Impacto(null);
            Assert.AreEqual(0, _canais.Count);
            noCorpo.Impacto(alvo);
            Assert.AreEqual(1, _canais.Count, "os dois caminhos do impacto chamam a Sintonia");
            Assert.AreEqual(ComboSintonia.ExplosaoDePlasma, _canais[0].c);
            Sintonia.Tick(Balance.Sintonia.CanalizacaoS);
            Assert.AreEqual((noChao.Dano + noCorpo.Dano) * Balance.Sintonia.Mult, _disparos[0].Dano, Eps, "o dano de cada tiro");
            Assert.AreSame(alvo, _disparos[0].Alvo);
        }
    }
}
