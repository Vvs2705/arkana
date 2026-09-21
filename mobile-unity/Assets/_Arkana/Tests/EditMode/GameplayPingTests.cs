using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>O PING DE SINTONIA (GDD §18.7, onda 18D): o jogador pede "COMBO?", o parceiro bot responde depois da reacao,
    /// prende o foco no alvo combinado e vai com o elemento que funde — ou recusa sem fingir. Tudo pela parte pura: a regra
    /// do ping (PingDeSintonia), a escolha do elemento (Dupla), o foco (PercepcaoBot/DecisaoBot), a faixa e o toque do anel
    /// (DuplaHudLogica) e a marca do pacto (MarcasLogica).</summary>
    public class GameplayPingTests
    {
        const int TIME_INIMIGO = 1;
        static readonly Elemento[] Fogo = { Elemento.Fogo }, Vento = { Elemento.Vento };
        FakeEntidade _jogador, _parceiro;
        PingDeSintonia _ping;
        List<IEntidade> _arena;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Combat.Reset();
            Derrubado.Reset();
            Efeitos.Reset();
            Sintonia.Reset();
            _jogador = new FakeEntidade("Eu", Vector3.zero, true);
            _parceiro = new FakeEntidade("Veu", new Vector3(2f, 0f, 0f));
            Combat.DefinirTime(_jogador, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(_parceiro, Combat.TIME_DO_PLAYER);
            _arena = new List<IEntidade> { _jogador, _parceiro };
            Derrubado.Arena = _arena;
            Derrubado.Instalar();   // com o outro de pe', o golpe fatal DERRUBA (e' o "cair" que acaba o pacto)
            _ping = new PingDeSintonia(_parceiro);
        }

        [TearDown]
        public void TearDown()
        {
            Sintonia.Reset();
            Derrubado.Reset();
            Combat.Reset();
            Bus.Reset();
        }

        FakeEntidade Inimigo(string nome, Vector3 pos, int time = TIME_INIMIGO)
        {
            var e = new FakeEntidade(nome, pos);
            Combat.DefinirTime(e, time);
            _arena.Add(e);
            return e;
        }

        static void Golpe(IEntidade e) => Combat.AplicarDano(e, 1000f, Elemento.Terra, null, false, true);

        static float Andando(IEntidade e) => 4f;

        /// <summary>Pede no `alvo` (passada a recarga do ping) e o parceiro responde com `meus`.</summary>
        void Aceitar(IEntidade alvo, Elemento[] meus)
        {
            _ping.Tick(PingDeSintonia.RECARGA_S);
            Assert.IsTrue(_ping.Propor(_jogador, alvo, Fogo), "o pedido sai");
            _ping.Tick(PingDeSintonia.ACEITE_S);
            _ping.Responder(meus);
        }

        [Test]
        public void Propor_SoEmDupla_ComSintoniaPronta_EOsDoisDePe()
        {
            FakeEntidade alvo = Inimigo("Pyra", new Vector3(0f, 0f, 10f));
            Assert.IsTrue(_ping.PodePropor(_jogador, Fogo), "dupla, os dois de pe', Sintonia pronta, armado: pode");
            Assert.IsFalse(_ping.PodePropor(alvo, Fogo), "so' o do MEU time pede (no solo ninguem e' parceiro de ninguem)");
            Assert.IsFalse(_ping.PodePropor(_jogador, null), "maos nuas nao pedem combo: nao ha' elemento");

            // a Sintonia do jogador em recarga (o custo sai no INICIO da canalizacao): o anel nao pede
            Sintonia.RegistrarImpacto(_jogador, Elemento.Fogo, alvo.Pos, alvo, 10f);
            Sintonia.RegistrarImpacto(_parceiro, Elemento.Vento, alvo.Pos, alvo, 10f);
            Assert.Greater(Sintonia.CooldownRestante(_jogador), 0f);
            Assert.IsFalse(_ping.PodePropor(_jogador, Fogo), "Sintonia em recarga nao pede combo");
            Assert.IsFalse(_ping.Propor(_jogador, alvo, Fogo));
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado);
            Sintonia.Reset();

            // anti-spam: um pedido a cada RECARGA_S
            Assert.IsTrue(_ping.Propor(_jogador, alvo, Fogo));
            Assert.AreEqual(PingDeSintonia.Fase.Proposto, _ping.Estado);
            Assert.IsFalse(_ping.Propor(_jogador, alvo, Fogo), "dois toques colados: o segundo nao sai");
            _ping.Tick(PingDeSintonia.RECARGA_S);
            Assert.IsTrue(_ping.Propor(_jogador, alvo, Fogo), "passada a recarga do ping, pede de novo");

            // o parceiro CAIDO nao responde: nem pede, e o pedido aberto morre
            Golpe(_parceiro);
            Assert.IsTrue(Derrubado.Esta(_parceiro));
            _ping.Tick(0.01f);
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado, "o pedido aberto morre com o parceiro no chao");
            _ping.Tick(PingDeSintonia.RECARGA_S);   // a recarga do ping nao pode ser o que barra: o parceiro caido e' que barra
            Assert.IsFalse(_ping.PodePropor(_jogador, Fogo), "parceiro caido: o anel nao pede");
        }

        [Test]
        public void Alvo_SobAMira_SenaoOUltimoAcertado()
        {
            FakeEntidade sob = Inimigo("Brok", new Vector3(0f, 0f, 12f)), ultimo = Inimigo("Tessa", new Vector3(8f, 0f, 8f), 2);
            Assert.AreSame(sob, PingDeSintonia.AlvoDoPing(_jogador, sob, ultimo), "quem esta' SOB A MIRA manda");
            Assert.AreSame(ultimo, PingDeSintonia.AlvoDoPing(_jogador, null, ultimo), "sem ninguem na mira, o ultimo acertado");
            Assert.AreSame(ultimo, PingDeSintonia.AlvoDoPing(_jogador, _parceiro, ultimo), "aliado sob a mira nao e' alvo");
            Assert.IsNull(PingDeSintonia.AlvoDoPing(_jogador, null, null));
            Assert.IsFalse(_ping.Propor(_jogador, null, Fogo), "sem alvo o pedido nao sai (a faixa diz MIRE NUM INIMIGO)");
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado);

            // o ultimo acertado vem das MARCAS (o acerto do jogador) e a morte o esquece
            var marcas = new MarcasLogica();
            marcas.Acertou(sob);
            marcas.Acertou(ultimo);
            Assert.AreSame(ultimo, marcas.UltimoAcertado, "o acerto mais recente");
            marcas.Esquecer(ultimo);
            Assert.IsNull(marcas.UltimoAcertado, "morreu: nao e' mais alvo de pedido");

            // caido nao e' alvo (o losango do caido ja' e' outro assunto): pula para o ultimo acertado
            FakeEntidade par = Inimigo("Gromm", new Vector3(3f, 0f, 12f));   // o parceiro do Brok: com ele de pe', o Brok CAI
            Golpe(sob);
            Assert.IsTrue(Derrubado.Esta(sob));
            Assert.AreSame(ultimo, PingDeSintonia.AlvoDoPing(_jogador, sob, ultimo), "caido sob a mira nao e' alvo");
            Assert.AreSame(par, PingDeSintonia.AlvoDoPing(_jogador, par, null));
        }

        [Test]
        public void Parceiro_AceitaDepoisDaReacao_EPrendeOFocoNoAlvo()
        {
            FakeEntidade alvo = Inimigo("Brok", new Vector3(0f, 0f, 35f));   // alem da MEMORIA (30) e da perseguicao (20)
            FakeEntidade perto = Inimigo("Tessa", new Vector3(3f, 0f, 0f), 2);
            Assert.IsTrue(_ping.Propor(_jogador, alvo, Fogo));
            Assert.IsFalse(_ping.Responde, "no mesmo quadro do toque, nada");
            _ping.Tick(PingDeSintonia.ACEITE_S - 0.1f);
            _ping.Responder(Vento);
            Assert.AreEqual(PingDeSintonia.Fase.Proposto, _ping.Estado, "antes da reacao o parceiro ainda nao respondeu");
            _ping.Tick(0.1f);
            Assert.IsTrue(_ping.Responde);
            _ping.Responder(Vento);
            Assert.AreEqual(PingDeSintonia.Fase.Aceito, _ping.Estado, "depois de ACEITE_S, aceita");
            Assert.IsTrue(_ping.Pactuado);
            Assert.AreSame(alvo, _ping.Alvo);

            // o FOCO: o pacto prende — acerto do jogador em outro, disparo perto, revide, a mira e a memoria nao o tiram
            var p = new PercepcaoBot(_parceiro) { Parceiro = _jogador };
            p.Ligar();
            p.Prender(_ping.Alvo);
            Assert.AreSame(alvo, p.Alvo);
            Bus.EmitDamageApplied(perto, 10f, Elemento.Raio, _jogador, false);
            Assert.AreSame(alvo, p.Alvo, "o jogador acertou outro: o pacto segura");
            Bus.EmitDisparo(perto, perto.Pos);
            Assert.AreSame(alvo, p.Alvo, "disparo mais perto nao o tira");
            p.Revidar(_parceiro, 10f, Elemento.Raio, perto, false);
            Assert.AreSame(alvo, p.Alvo, "nem o revide");
            p.MiraDoParceiro(perto);
            p.Varrer(_arena, Andando);
            Assert.AreSame(alvo, p.Alvo, "nem a varredura: a 35 m (alem da memoria) o alvo combinado continua");
            // solto, volta ao de sempre
            p.Prender(null);
            Bus.EmitDamageApplied(perto, 10f, Elemento.Raio, _jogador, false);
            Assert.AreSame(perto, p.Alvo, "sem pacto, o ultimo acertado do jogador volta a mandar");
            p.Desligar();

            // a DECISAO: o alvo do pacto se persegue a qualquer distancia (sem pacto, a 35 m ele fica com o jogador)
            var e = new DecisaoBot.Escolta { Segue = true, Parceiro = _jogador.Pos, Pacto = true };
            DecisaoBot.Saida s = new DecisaoBot(7).Decidir(0.1f, _parceiro.Pos, true, alvo, null, null, null, null, true, e);
            Assert.Greater(Vector3.Dot(s.Dir, (alvo.Pos - _parceiro.Pos).normalized), 0.99f, "no pacto, vai ate' o alvo");
            e.Pacto = false;
            s = new DecisaoBot(7).Decidir(0.1f, _parceiro.Pos, true, alvo, null, null, null, null, true, e);
            Assert.AreEqual(0f, s.Dir.magnitude, 1e-4f, "sem pacto, alem da perseguicao, fica na faixa do jogador");

            // a MARCA: pedido numa cor so', aceito nas DUAS
            IEntidade m; Elemento a, b; bool selado;
            Assert.IsTrue(MarcasLogica.MarcaDoPacto(_ping, out m, out a, out b, out selado));
            Assert.AreSame(alvo, m);
            Assert.IsTrue(selado);
            Assert.AreEqual(Elemento.Fogo, a, "metade do jogador");
            Assert.AreEqual(Elemento.Vento, b, "metade do parceiro");
            _ping.Tick(PingDeSintonia.RECARGA_S);
            _ping.Propor(_jogador, perto, Fogo);
            Assert.IsTrue(MarcasLogica.MarcaDoPacto(_ping, out m, out a, out b, out selado), "o pedido ja' marca o alvo");
            Assert.AreSame(perto, m, "pedido novo troca o alvo");
            Assert.IsFalse(selado);
            Assert.AreEqual(a, b, "pedido sem resposta: uma cor so' (a do jogador)");
            Assert.IsFalse(MarcasLogica.MarcaDoPacto(null, out m, out a, out b, out selado), "sem parceiro, sem marca");
        }

        [Test]
        public void Parceiro_EscolheOElementoQueFormaCombo()
        {
            Assert.AreEqual(Elemento.Vento, Dupla.ElementoDoCombo(Fogo, new[] { Elemento.Fogo, Elemento.Vento }),
                "manopla com o elemento do jogador: vai com o OUTRO");
            Assert.AreEqual(Elemento.Agua, Dupla.ElementoDoCombo(Fogo, new[] { Elemento.Agua, Elemento.Fogo }));
            Assert.AreEqual(Elemento.Agua, Dupla.ElementoDoCombo(new[] { Elemento.Fogo, Elemento.Vento }, new[] { Elemento.Vento, Elemento.Agua }),
                "jogador de manopla: o que funde com OS DOIS tiros dele");
            Assert.AreEqual(Elemento.Fogo, Dupla.ElementoDoCombo(new[] { Elemento.Fogo, Elemento.Vento }, Fogo),
                "sem um que funda com os dois, o que funde com algum");

            // o pacto anuncia o combo que VAI sair
            FakeEntidade alvo = Inimigo("Brok", new Vector3(0f, 0f, 10f));
            Aceitar(alvo, new[] { Elemento.Fogo, Elemento.Vento });
            Assert.AreEqual(Elemento.Vento, _ping.ElParceiro);
            Assert.AreEqual(ComboSintonia.TornadoFlamejante, _ping.Combo, "FOGO + VENTO: TORNADO FLAMEJANTE");
            // jogador de manopla: o par do nome e' o elemento DELE que casa com o do parceiro
            _ping.Tick(PingDeSintonia.RECARGA_S);
            _ping.Propor(_jogador, alvo, new[] { Elemento.Vento, Elemento.Terra });
            _ping.Tick(PingDeSintonia.ACEITE_S);
            _ping.Responder(Vento);
            Assert.AreEqual(Elemento.Terra, _ping.ElJogador);
            Assert.AreEqual(ComboSintonia.TempestadeDeAreia, _ping.Combo);

            // a MANOPLA de verdade (alterna tiro a tiro): alinhada, so' sai o elemento que funde
            var slot = new ArmaSlot(_parceiro);
            Assert.IsTrue(slot.Equipar(Arma.MANOPLA, new[] { Elemento.Fogo, Elemento.Vento }));
            for (int i = 0; i < 5; i++)
            {
                Dupla.Alinhar(slot, Elemento.Fogo, Elemento.Vento);
                Assert.AreEqual(Elemento.Vento, slot.ElementoDoDisparo(Elemento.Fogo), "tiro " + i + " do pacto: so' VENTO");
            }
            for (int i = 0; i < 3; i++)
            {
                Dupla.Alinhar(slot, Elemento.Fogo, Elemento.Fogo);
                Assert.AreEqual(Elemento.Fogo, slot.ElementoDoDisparo(Elemento.Fogo), "e o outro lado tambem");
            }
            // sem alinhar, a manopla segue alternando (o pacto nao mexe na arma de ninguem fora dele)
            Assert.AreNotEqual(slot.ElementoDoDisparo(Elemento.Fogo), slot.ElementoDoDisparo(Elemento.Fogo));
        }

        [Test]
        public void Parceiro_RecusaSemCombo_SemFingir()
        {
            FakeEntidade alvo = Inimigo("Brok", new Vector3(0f, 0f, 10f));
            Aceitar(alvo, Fogo);
            Assert.AreEqual(PingDeSintonia.Fase.Recusado, _ping.Estado, "so' o MESMO elemento: recusa");
            Assert.IsFalse(_ping.Pactuado);
            Assert.IsNull(_ping.Combo);
            IEntidade m; Elemento a, b; bool selado;
            Assert.IsFalse(MarcasLogica.MarcaDoPacto(_ping, out m, out a, out b, out selado), "recusou: o alvo nao fica marcado");
            _ping.Tick(PingDeSintonia.ACEITE_S);
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado, "a recusa passa (um SEM COMBO velho nao fica pendurado)");

            Aceitar(alvo, null);
            Assert.AreEqual(PingDeSintonia.Fase.Recusado, _ping.Estado, "maos nuas: recusa");
            // o parceiro sem a Sintonia (o que caiu num combo falhado segue pagando): recusa
            FakeEntidade outro = new FakeEntidade("Maris", new Vector3(1f, 0f, 1f));
            Combat.DefinirTime(outro, Combat.TIME_DO_PLAYER);
            Sintonia.RegistrarImpacto(outro, Elemento.Agua, alvo.Pos, alvo, 5f);
            Sintonia.RegistrarImpacto(_parceiro, Elemento.Terra, alvo.Pos, alvo, 5f);
            Assert.Greater(Sintonia.CooldownRestante(_parceiro), 0f);
            Assert.AreEqual(0f, Sintonia.CooldownRestante(_jogador));
            Aceitar(alvo, Vento);
            Assert.AreEqual(PingDeSintonia.Fase.Recusado, _ping.Estado, "parceiro em recarga: recusa");

            // a faixa: SEM COMBO
            var h = new DuplaHudLogica();
            h.Respondeu(null);
            Assert.AreEqual(DuplaHudLogica.Faixa.SemCombo, h.Estado);
        }

        [Test]
        public void Pacto_Expira_EAcabaComAlvoCaido_ParceiroCaido_OuASintoniaResolvida()
        {
            FakeEntidade alvo = Inimigo("Brok", new Vector3(0f, 0f, 10f));
            // pedido sem resposta morre em PROPOSTA_S
            Assert.IsTrue(_ping.Propor(_jogador, alvo, Fogo));
            _ping.Tick(PingDeSintonia.PROPOSTA_S - 0.1f);
            Assert.AreEqual(PingDeSintonia.Fase.Proposto, _ping.Estado);
            _ping.Tick(0.2f);
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado, "sem resposta, o pedido expira");

            // o pacto expira em PACTO_S
            Aceitar(alvo, Vento);
            _ping.Tick(PingDeSintonia.PACTO_S - 0.1f);
            Assert.IsTrue(_ping.Pactuado, "o pacto dura PACTO_S");
            _ping.Tick(0.2f);
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado, "e expira");

            // o alvo MORRE (sem parceiro dele de pe': morre, nao cai)
            Aceitar(alvo, Vento);
            Golpe(alvo);
            Assert.IsFalse(alvo.Vital.Viva);
            IEntidade m; Elemento a, b; bool selado;
            Assert.IsFalse(MarcasLogica.MarcaDoPacto(_ping, out m, out a, out b, out selado),
                "o losango sai no quadro em que o alvo cai (antes do relogio do parceiro andar)");
            _ping.Tick(0.01f);
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado, "o alvo caiu: o pacto acaba");

            // o alvo CAI (derrubado, com o parceiro dele de pe')
            FakeEntidade alvo2 = Inimigo("Tessa", new Vector3(0f, 0f, 14f), 2), dele = Inimigo("Gromm", new Vector3(4f, 0f, 14f), 2);
            Aceitar(alvo2, Vento);
            Golpe(alvo2);
            Assert.IsTrue(Derrubado.Esta(alvo2));
            _ping.Tick(0.01f);
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado, "derrubado tambem acaba");

            // a SINTONIA resolvida: canalizando o pacto segura; disparou, acaba
            Aceitar(dele, Vento);
            Assert.IsTrue(_ping.Pactuado);
            Sintonia.RegistrarImpacto(_jogador, Elemento.Fogo, dele.Pos, dele, 10f);
            Sintonia.RegistrarImpacto(_parceiro, Elemento.Vento, dele.Pos, dele, 10f);
            Assert.IsTrue(Sintonia.Canalizando(_parceiro));
            _ping.Tick(0.01f);
            Assert.IsTrue(_ping.Pactuado, "canalizando: o pacto segura ate' o combo sair");
            Sintonia.Tick(Balance.Sintonia.CanalizacaoS + 0.01f);
            Assert.IsFalse(Sintonia.Canalizando(_parceiro));
            _ping.Tick(0.01f);
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado, "a Sintonia disparou: o pacto cumpriu");
        }

        [Test]
        public void Pacto_AcabaComOJogadorCaido()
        {
            FakeEntidade alvo = Inimigo("Brok", new Vector3(0f, 0f, 10f));
            Aceitar(alvo, Vento);
            Golpe(_jogador);
            Assert.IsTrue(Derrubado.Esta(_jogador));
            _ping.Tick(0.01f);
            Assert.AreEqual(PingDeSintonia.Fase.Nada, _ping.Estado, "quem pediu caiu: nao ha' combo a fazer");
        }

        [Test]
        public void ToqueNoAnel_NaoRoubaOAtaqueNemOsVizinhos()
        {
            // a tela do teste (px = 1, com entalhe) e a do Poco F4 deitado (2400x1080 a 395 ppi)
            var telas = new[] { new Vector2(1600, 720), new Vector2(2400, 1080) };
            var margens = new[] { new Margens(80, 40, 90, 60), new Margens(0, 0, 0, 0) };
            var pxs = new[] { 1f, 395f / 160f };
            for (int i = 0; i < telas.Length; i++)
            {
                float px = pxs[i];
                HudLayout l = HudLayout.Calcular(telas[i], margens[i], px);
                Rect[] evitar = { l.Esquiva, l.Salto, l.Carrossel };
                Rect d = l.Disparo;
                Assert.IsFalse(DuplaHudLogica.NoAnel(d.center, d, evitar), "o meio do ataque e' do ATAQUE");
                Assert.IsFalse(DuplaHudLogica.NoAnel(new Vector2(d.xMin + 1f, d.yMin + 1f), d, evitar), "o canto do quadrado do ataque tambem");
                Assert.IsTrue(DuplaHudLogica.NoAnel(new Vector2(d.center.x, d.yMin - 10f * px), d, evitar), "logo abaixo do ataque: o anel");
                Assert.IsTrue(DuplaHudLogica.NoAnel(new Vector2(d.xMax + 10f * px, d.center.y), d, evitar), "logo a' direita: o anel");
                Assert.IsTrue(DuplaHudLogica.NoAnel(new Vector2(d.xMin - 5f * px, d.yMin - 5f * px), d, evitar), "na diagonal de baixo: o anel");
                Assert.IsFalse(DuplaHudLogica.NoAnel(new Vector2(l.Esquiva.xMax - 2f * px, l.Esquiva.center.y), d, evitar), "a borda da ESQUIVA e' da esquiva");
                Assert.IsFalse(DuplaHudLogica.NoAnel(new Vector2(l.Salto.xMin + 2f * px, l.Salto.yMin + 2f * px), d, evitar), "a do SALTO e' do salto");
                Assert.IsFalse(DuplaHudLogica.NoAnel(new Vector2(d.center.x, d.yMin - 60f * px), d, evitar), "longe demais nao e' anel");
                // varredura: nenhum ponto que o anel pega mora num botao; e o anel existe dos quatro lados que tem espaco
                int anel = 0;
                for (float x = d.center.x - 90f * px; x <= d.center.x + 90f * px; x += 3f * px)
                    for (float y = d.center.y - 90f * px; y <= d.center.y + 90f * px; y += 3f * px)
                    {
                        var p = new Vector2(x, y);
                        if (!DuplaHudLogica.NoAnel(p, d, evitar)) continue;
                        anel++;
                        foreach (Rect r in new[] { d, l.Esquiva, l.Salto, l.Carrossel })
                            Assert.IsFalse(r.Contains(p), "o anel pegou " + p + " dentro de um botao (" + telas[i] + ")");
                    }
                Assert.Greater(anel, 100, "o anel tem area de dedo");
            }
        }

        [Test]
        public void Faixa_ContaAConversa_SemPisarNaSintonia()
        {
            var h = new DuplaHudLogica();
            h.Pingou(Elemento.Fogo);
            Assert.AreEqual(DuplaHudLogica.Faixa.Combo, h.Estado, "COMBO?");
            Assert.AreEqual(Elemento.Fogo, h.ElPing);
            h.Tick(0.2f);
            Assert.Greater(h.Alfa, 0.9f, "o pedido aparece inteiro");
            h.Respondeu(ComboSintonia.TornadoFlamejante);
            Assert.AreEqual(DuplaHudLogica.Faixa.Aceito, h.Estado, "PARCEIRO: ACEITO");
            Assert.AreEqual(ComboSintonia.TornadoFlamejante, h.Combo, "→ o combo que vai sair");
            Assert.Greater(h.Escala, 1f, "o aceite carimba");
            h.Tick(DuplaHudLogica.RespostaS + 0.01f);
            Assert.AreEqual(DuplaHudLogica.Faixa.Nada, h.Estado, "a resposta sai da faixa (o pacto segue na marca)");
            h.SemAlvo();
            Assert.AreEqual(DuplaHudLogica.Faixa.SemAlvo, h.Estado, "MIRE NUM INIMIGO");
            // o pedido que morre sem resposta sai; a resposta nao
            h.Pingou(Elemento.Agua);
            h.Calar();
            Assert.AreEqual(DuplaHudLogica.Faixa.Nada, h.Estado);
            h.Respondeu(ComboSintonia.Lamacal);
            h.Calar();
            Assert.AreEqual(DuplaHudLogica.Faixa.Aceito, h.Estado, "calar nao apaga a resposta");
            // a Sintonia de verdade manda: o ping nao pisa na canalizacao
            h.Canalizar(ComboSintonia.ChuvaDeMagma, 1f);
            h.Pingou(Elemento.Fogo);
            h.Respondeu(null);
            h.SemAlvo();
            Assert.AreEqual(DuplaHudLogica.Faixa.Canalizando, h.Estado, "canalizando, o ping espera");
            Assert.AreEqual(ComboSintonia.ChuvaDeMagma, h.Combo);
        }

        /// <summary>O ACHADO DO APARELHO (Poco F4, 21/09, aparelho-19/tela-95s.png): com o parceiro AS COSTAS a marca azul era
        /// presa na borda de BAIXO, em cima da SUPREMA/TATICA. Presa, ela DESLIZA pelo contorno da caixa para fora de toda zona
        /// proibida (os controles e o topo do HudLayout, e a coluna da mira — PONTE A11), para qualquer direcao.</summary>
        [Test]
        public void MarcaDoParceiro_PresaNaBorda_NuncaSobreOsControlesNemAColunaDaMira()
        {
            var telas = new[] { new Vector2(1600, 720), new Vector2(2400, 1080) };
            var margens = new[] { new Margens(80, 40, 90, 60), new Margens(0, 0, 0, 0) };
            var pxs = new[] { 1f, 395f / 160f };
            for (int i = 0; i < telas.Length; i++)
            {
                Vector2 tela = telas[i];
                float px = pxs[i], aspecto = tela.x / tela.y;
                HudLayout l = HudLayout.Calcular(tela, margens[i], px);
                var zonas = new List<Rect>();
                MarcasLogica.ZonasProibidas(l, tela, px, zonas);
                Rect c = MarcasLogica.CaixaDaBorda;
                Rect caixa = Rect.MinMaxRect(c.xMin * tela.x, c.yMin * tela.y, c.xMax * tela.x, c.yMax * tela.y);
                Vector2 meia = new Vector2(58f, 25f) * 0.5f * px + Vector2.one * 16f * px;   // a placa do parceiro + a seta
                float meioX = tela.x * 0.5f, coluna = MarcasLogica.ColunaMiraDp * px * 0.5f;
                var controles = new[] { l.Joystick, l.AnelSintonia, l.Disparo, l.Esquiva, l.Tatica, l.Suprema, l.Salto, l.Minimapa, l.Pausa, l.Barras, l.Topo };
                int cruSobre = 0;
                for (float g = 0f; g < 360f; g += 5f)
                {
                    Vector2 longe = new Vector2(0.5f, 0.5f) + new Vector2(Mathf.Sin(g * Mathf.Deg2Rad), Mathf.Cos(g * Mathf.Deg2Rad)) * 100f;
                    Vector2 vp;
                    float ang;
                    Assert.IsTrue(MarcasLogica.NaBorda(longe, c, aspecto, out vp, out ang));
                    Vector2 cru = Vector2.Scale(vp, tela);
                    var rc = new Rect(cru - meia, 2f * meia);
                    foreach (Rect k in controles) if (rc.Overlaps(k)) { cruSobre++; break; }
                    Vector2 p = MarcasLogica.Deslizar(cru, caixa, meia, zonas, MarcasLogica.PassoBordaDp * px);
                    var r = new Rect(p - meia, 2f * meia);
                    foreach (Rect k in controles)
                        Assert.IsFalse(r.Overlaps(k), "a " + g + " graus a marca presa cai num botao/HUD (" + tela + ")");
                    Assert.IsFalse(r.xMax > meioX - coluna && r.xMin < meioX + coluna, "a " + g + " graus a marca presa cai na coluna da mira (" + tela + ")");
                    float naBorda = Mathf.Min(Mathf.Min(Mathf.Abs(p.x - caixa.xMin), Mathf.Abs(p.x - caixa.xMax)), Mathf.Min(Mathf.Abs(p.y - caixa.yMin), Mathf.Abs(p.y - caixa.yMax)));
                    Assert.Less(naBorda, 0.01f, "desliza PELO contorno (continua na borda)");
                }
                Assert.Greater(cruSobre, 0, "a borda crua (sem deslizar) caia em cima de botao: o defeito do aparelho (" + tela + ")");

                // o parceiro AS COSTAS e a' direita (a foto do aparelho): a projecao manda para baixo; a marca sai de la'
                Vector2 atras = MarcasLogica.ParaViewport(new Vector3(2f, 0f, 5f), Mathf.Tan(30f * Mathf.Deg2Rad), aspecto);
                Vector2 vpAtras;
                float angAtras;
                Assert.IsTrue(MarcasLogica.NaBorda(atras, c, aspecto, out vpAtras, out angAtras));
                Assert.AreEqual(c.yMin, vpAtras.y, 1e-4f, "as costas prendem na borda de BAIXO");
                Vector2 pa = MarcasLogica.Deslizar(Vector2.Scale(vpAtras, tela), caixa, meia, zonas, MarcasLogica.PassoBordaDp * px);
                Assert.IsTrue(MarcasLogica.Livre(pa, meia, zonas), "e desliza para um lugar livre");
                Assert.GreaterOrEqual(pa.x, tela.x * 0.5f, "do MESMO LADO: o parceiro a' direita nao aparece a' esquerda da mira (" + tela + ")");

                // no aparelho (Poco F4): o canto de cima-esquerda cai em cima dos abates -> o lugar livre mais perto e' DESCENDO a
                // borda esquerda (em cima so' ha' abates e a faixa da Sintonia)
                if (i == 1)
                {
                    Vector2 vpCanto;
                    MarcasLogica.NaBorda(new Vector2(-100f, 100.5f), c, aspecto, out vpCanto, out angAtras);
                    Vector2 canto = Vector2.Scale(vpCanto, tela);
                    Assert.IsFalse(MarcasLogica.Livre(canto, meia, zonas), "o canto de cima-esquerda esta' em cima dos abates");
                    Vector2 pc = MarcasLogica.Deslizar(canto, caixa, meia, zonas, MarcasLogica.PassoBordaDp * px);
                    Assert.AreEqual(caixa.xMin, pc.x, 0.01f, "desce pela borda ESQUERDA");
                    Assert.Less(pc.y, canto.y);
                }

                // o que ja' esta' livre nao mexe: o parceiro a' esquerda fica no meio da borda esquerda
                Vector2 vpEsq;
                MarcasLogica.NaBorda(new Vector2(-100f, 0.5f), c, aspecto, out vpEsq, out angAtras);
                Vector2 esq = Vector2.Scale(vpEsq, tela);
                Assert.AreEqual(esq, MarcasLogica.Deslizar(esq, caixa, meia, zonas, MarcasLogica.PassoBordaDp * px), "livre, fica onde a borda pos");
            }
        }
    }
}
