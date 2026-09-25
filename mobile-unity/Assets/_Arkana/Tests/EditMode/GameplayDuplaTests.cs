using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>A DUPLA EM JOGO (onda 17D, contrato G): o cerebro do parceiro bot e das duplas inimigas, a montagem da
    /// partida (dupla x solo), a camera do espectador e a leitura da HUD — tudo pela parte pura, sem cena.</summary>
    public class GameplayDuplaTests
    {
        const int TIME_INIMIGO = 1;
        FakeEntidade _jogador, _parceiro;
        PercepcaoBot _p;
        List<IEntidade> _arena;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Combat.Reset();
            Derrubado.Reset();
            Efeitos.Reset();
            _jogador = new FakeEntidade("Eu", Vector3.zero, true);
            _parceiro = new FakeEntidade("Veu", new Vector3(2f, 0f, 0f));
            Combat.DefinirTime(_jogador, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(_parceiro, Combat.TIME_DO_PLAYER);
            _p = new PercepcaoBot(_parceiro) { Parceiro = _jogador };
            _p.Ligar();
            _arena = new List<IEntidade> { _jogador, _parceiro };
        }

        [TearDown]
        public void TearDown()
        {
            _p.Desligar();
            Bus.Reset();
            Combat.Reset();
            Derrubado.Reset();
        }

        FakeEntidade Inimigo(string nome, Vector3 pos, int time = TIME_INIMIGO)
        {
            var e = new FakeEntidade(nome, pos);
            Combat.DefinirTime(e, time);
            _arena.Add(e);
            return e;
        }

        static float Andando(IEntidade e) => 4f;   // todo mundo se mexendo: os passos entregam qualquer um no raio

        [Test]
        public void Parceiro_NuncaEscolheAliadoComoAlvo()
        {
            // o jogador colado, andando (passos) e atirando: nada disso vira presa
            _p.Varrer(_arena, Andando);
            Assert.IsNull(_p.Alvo, "o aliado colado e andando nao e' presa");
            Bus.EmitDisparo(_jogador, _jogador.Pos);
            Assert.IsNull(_p.Alvo, "o disparo do aliado nao denuncia ninguem");
            _p.Revidar(_parceiro, 10f, Elemento.Fogo, _jogador, false);
            Assert.IsNull(_p.Alvo, "dano de aliado (se escapar) nao ensina revide");
            _p.Focar(_jogador);
            Assert.IsNull(_p.Alvo, "foco num aliado e' recusado");

            // com um inimigo LONGE e o aliado perto, a varredura pega o inimigo
            FakeEntidade longe = Inimigo("Pyra", new Vector3(0f, 0f, 9f));
            _p.Varrer(_arena, Andando);
            Assert.AreSame(longe, _p.Alvo, "a presa e' o inimigo, mesmo mais longe que o aliado");
            Assert.IsFalse(Combat.MesmoTime(_parceiro, _p.Alvo));
        }

        [Test]
        public void Parceiro_FocaOAlvoDoJogador_OUltimoAcertadoEOSobAMira()
        {
            FakeEntidade perto = Inimigo("Brok", new Vector3(3f, 0f, 0f));
            FakeEntidade doJogador = Inimigo("Tessa", new Vector3(0f, 0f, 20f), 2);
            _p.Varrer(_arena, Andando);
            Assert.AreSame(perto, _p.Alvo, "sozinho ele pega o mais perto");
            // o JOGADOR acerta outro: o foco passa para ele (o mesmo alvo e' o que faz a Sintonia)
            Bus.EmitDamageApplied(doJogador, 12f, Elemento.Raio, _jogador, false);
            Assert.AreSame(doJogador, _p.Alvo, "o ultimo que o jogador acertou vira o alvo do parceiro");
            // o que o jogador tem SOB A MIRA so' vale para quem esta' sem alvo
            _p.MiraDoParceiro(perto);
            Assert.AreSame(doJogador, _p.Alvo, "com alvo vivo, a mira do jogador nao o tira do foco");
            _p.Esquecer();
            _p.MiraDoParceiro(perto);
            Assert.AreSame(perto, _p.Alvo, "sem alvo, mira no que o jogador mira");
            // um acerto de quem NAO e' o parceiro nao mexe no foco
            var outro = Inimigo("Gromm", new Vector3(5f, 0f, 5f), 3);
            Bus.EmitDamageApplied(doJogador, 5f, Elemento.Terra, outro, false);
            Assert.AreSame(perto, _p.Alvo, "so' o acerto do PARCEIRO dita o foco");
        }

        [Test]
        public void Parceiro_VoltaParaPertoAlemDe8m_SemCombate_EFicaDentroDaFaixa()
        {
            var d = new DecisaoBot(11);
            var e = new DecisaoBot.Escolta { Segue = true, Parceiro = new Vector3(10f, 0f, 0f) };
            DecisaoBot.Saida s = d.Decidir(0.1f, Vector3.zero, true, null, null, null, null, null, true, e);
            Assert.Greater(s.Dir.x, 0.99f, "a 10 m (alem de 8) volta para o jogador");
            e.Parceiro = new Vector3(7f, 0f, 0f);
            s = d.Decidir(0.1f, Vector3.zero, true, null, null, null, null, null, true, e);
            Assert.Greater(s.Dir.x, 0.99f, "voltando, so' para no meio da faixa (histerese)");
            e.Parceiro = new Vector3(5.5f, 0f, 0f);
            s = d.Decidir(0.1f, Vector3.zero, true, null, null, null, null, null, true, e);
            Assert.AreEqual(0f, s.Dir.magnitude, 1e-4f, "chegou na faixa: fica");
            e.Parceiro = new Vector3(7.5f, 0f, 0f);
            s = d.Decidir(0.1f, Vector3.zero, true, null, null, null, null, null, true, e);
            Assert.AreEqual(0f, s.Dir.magnitude, 1e-4f, "dentro de 4–8 m nao anda (quem manda o passo e' o jogador)");
            Assert.IsTrue(Dupla.Voltar(Dupla.ACOMPANHA_MAX + 0.1f, false));
            Assert.IsFalse(Dupla.Voltar(Dupla.ACOMPANHA_MAX - 0.1f, false));

            // EM COMBATE a faixa nao prende: persegue a presa
            FakeEntidade presa = Inimigo("Sylva", new Vector3(0f, 0f, 15f));
            e.Parceiro = new Vector3(-10f, 0f, 0f);
            s = new DecisaoBot(11).Decidir(0.1f, Vector3.zero, true, presa, null, null, null, null, true, e);
            Assert.Greater(s.Dir.z, 0.99f, "com presa no alcance de perseguicao, persegue");
            // sozinho (sem escolta) o bot armado sem alvo VAGA, nao fica parado: o default e' o de hoje
            s = new DecisaoBot(11).Decidir(0.1f, Vector3.zero, true, null, null, null, null, null, true);
            Assert.Greater(s.Dir.magnitude, 0.5f, "sem dupla, o vagar de sempre");
        }

        [Test]
        public void Parceiro_VaiAoAliadoDerrubado_EFicaPerto()
        {
            FakeEntidade caidoInimigo = Inimigo("Maris", new Vector3(3f, 0f, 0f));
            new Derrubado(caidoInimigo).Cair(null);
            Assert.IsNull(Dupla.AliadoCaido(_parceiro, _arena), "inimigo caido nao e' socorro");
            _jogador.Pos = new Vector3(0f, 0f, 15f);
            _parceiro.Pos = Vector3.zero;
            new Derrubado(_jogador).Cair(null);
            Assert.AreSame(_jogador, Dupla.AliadoCaido(_parceiro, _arena), "o jogador caido e' o socorro do parceiro");

            // o socorro manda mais que a presa: com um inimigo no alcance de perseguicao, ele vai ao caido
            FakeEntidade presa = Inimigo("Vex", new Vector3(15f, 0f, 0f));
            var d = new DecisaoBot(5);
            var e = new DecisaoBot.Escolta { Segue = true, Parceiro = _jogador.Pos, Socorrer = _jogador.Pos };
            DecisaoBot.Saida s = d.Decidir(0.1f, _parceiro.Pos, true, presa, null, null, null, null, true, e);
            Assert.Greater(s.Dir.z, 0.99f, "vai ate' o caido, nao atras da presa");
            // na BEIRA do raio do reerguer ainda chega mais (um passo de lado e o canal cai)
            Vector3 beira = _jogador.Pos - new Vector3(0f, 0f, Derrubado.RAIO_M * 0.95f);
            s = d.Decidir(0.1f, beira, true, null, null, null, null, null, true, e);
            Assert.Greater(s.Dir.z, 0.99f, "na beira do raio, entra mais");
            // chegou: FICA (o Derrubado canaliza por proximidade, dentro do raio)
            Vector3 perto = _jogador.Pos - new Vector3(0f, 0f, Dupla.SOCORRO_M * 0.5f);
            s = d.Decidir(0.1f, perto, true, null, null, null, null, null, true, e);
            Assert.AreEqual(0f, s.Dir.magnitude, 1e-4f, "junto do caido, para");
            Assert.Less(Dupla.SOCORRO_M, Derrubado.RAIO_M, "para DENTRO do raio do reerguer");
        }

        [Test]
        public void Parceiro_PrefereLuvaDeElementoDiferenteDoJogador()
        {
            var loot = new List<LootItem>
            {
                new LootItem(Arma.VARINHA, null, Elemento.Fogo, new Vector3(3f, 0f, 0f)),     // o elemento do jogador, mais perto
                new LootItem(Arma.VARINHA, null, Elemento.Agua, new Vector3(0f, 0f, 10f)),    // outro elemento, mais longe
            };
            Elemento[] doJogador = { Elemento.Fogo };
            Assert.AreSame(loot[1], Dupla.LuvaPreferida(Vector3.zero, loot, doJogador), "prefere a de outro elemento (faz Sintonia)");
            Assert.AreSame(loot[0], Dupla.LuvaPreferida(Vector3.zero, loot, null), "sem parceiro armado, a mais perto");
            // longe demais: arma de qualquer cor vale mais que maos nuas
            loot[1].Pos = new Vector3(0f, 0f, 3f + Dupla.LUVA_DESVIO_M + 5f);
            Assert.AreSame(loot[0], Dupla.LuvaPreferida(Vector3.zero, loot, doJogador), "nao atravessa o mapa pela cor");
            // a manopla que tem o elemento do jogador no par tambem e' "igual"
            var manopla = new LootItem(Arma.MANOPLA, new[] { Elemento.Raio, Elemento.Fogo }, null, new Vector3(1f, 0f, 0f));
            loot.Add(manopla);
            loot[1].Pos = new Vector3(0f, 0f, 10f);
            Assert.AreSame(loot[1], Dupla.LuvaPreferida(Vector3.zero, loot, doJogador), "par com o elemento dele conta como igual");
            // a decisao DESARMADA usa a preferencia
            var dec = new DecisaoBot(2);
            DecisaoBot.Saida s = dec.Decidir(0.1f, Vector3.zero, false, null, null, loot, null, null, true,
                new DecisaoBot.Escolta { Segue = true, Parceiro = new Vector3(20f, 0f, 0f), Evitar = doJogador });
            Assert.Greater(s.Dir.z, 0.99f, "desarmado, vai na luva de agua (nao na de fogo, nem atras do jogador)");
        }

        [Test]
        public void MontagemDupla_14CorposEm7Times_ParesNoMesmoNascimento_ParceiroNoFim()
        {
            Montagem.Vaga[] v = Montagem.Bots(true);
            int k = Balance.Match.TamanhoDoTime, times = Balance.Match.TimesInimigos + 1;
            Assert.AreEqual(k * Balance.Match.TimesInimigos + (k - 1), v.Length);
            Assert.AreEqual(k * times, Montagem.Corpos(true), "jogador + parceiros + times inimigos");
            var membros = new Dictionary<int, int> { { Combat.TIME_DO_PLAYER, 1 } };   // o jogador ja' esta' no time 0
            var nascimento = new Dictionary<int, int> { { Combat.TIME_DO_PLAYER, 0 } };
            for (int i = 0; i < v.Length; i++)
            {
                Assert.AreNotEqual(Montagem.SEM_TIME, v[i].Time, "na dupla todo corpo tem time");
                int n;
                membros[v[i].Time] = membros.TryGetValue(v[i].Time, out n) ? n + 1 : 1;
                if (nascimento.TryGetValue(v[i].Time, out n)) Assert.AreEqual(n, v[i].Nascimento, "a dupla nasce no MESMO ponto");
                else nascimento[v[i].Time] = v[i].Nascimento;
                if (v[i].Segue >= 0)
                {
                    Assert.Less(v[i].Segue, i, "quem segue nasce depois do lider");
                    Assert.AreEqual(v[i].Time, v[v[i].Segue].Time, "segue o proprio time");
                    Assert.AreNotEqual(0f, v[i].Lado, "e pousa AFASTADO, nao empilhado");
                }
            }
            Assert.AreEqual(times, membros.Count, times + " times");
            foreach (KeyValuePair<int, int> kv in membros) Assert.AreEqual(k, kv.Value, "time " + kv.Key + " tem " + k);
            Assert.AreEqual(times, new HashSet<int>(nascimento.Values).Count, "cada time no seu nascimento");
            Montagem.Vaga ultimo = v[v.Length - 1];
            Assert.IsTrue(ultimo.Parceiro, "o parceiro e' o ULTIMO bot (Main.Bots[0] continua inimigo)");
            Assert.AreEqual(Combat.TIME_DO_PLAYER, ultimo.Time);
            Assert.AreEqual(0, ultimo.Nascimento, "nasce onde o jogador nasce");
            Assert.IsFalse(v[0].Parceiro);
        }

        [Test]
        public void MontagemSolo_EAContaDeHoje()
        {
            Montagem.Vaga[] v = Montagem.Bots(false);
            Assert.AreEqual(Balance.Match.Bots, v.Length, "12 bots, como sempre");
            Assert.AreEqual(1 + Balance.Match.Bots, Montagem.Corpos(false));
            var nasc = new HashSet<int>();
            foreach (Montagem.Vaga x in v)
            {
                Assert.AreEqual(Montagem.SEM_TIME, x.Time, "solo nao registra time (FFA de hoje)");
                Assert.AreEqual(-1, x.Segue, "ninguem segue ninguem");
                Assert.IsFalse(x.Parceiro, "solo nao tem parceiro");
                Assert.AreNotEqual(0, x.Nascimento, "ninguem nasce no ponto do jogador");
                nasc.Add(x.Nascimento);
            }
            Assert.AreEqual(v.Length, nasc.Count, "cada bot no seu salto");
        }

        [Test]
        public void PlacaDoTopo_DuplasNaDupla_BotsNoSolo()
        {
            Assert.AreEqual("TRIOS 4", DuplaHudLogica.TextoTopo(4, 11));
            Assert.AreEqual("BOTS 11", DuplaHudLogica.TextoTopo(-1, 11));
            Assert.AreEqual("TRIOS 0", DuplaHudLogica.TextoTopo(0, 3), "zero times ainda e' modo em time");
        }

        [Test]
        public void AliadoCaido_GanhaMarcaAzul_InimigoCaidoOVermelho()
        {
            FakeEntidade inimigo = Inimigo("Pyra", new Vector3(5f, 0f, 0f));
            Color vermelho = Color.red;
            Assert.AreEqual(Dupla.CorAliado, Dupla.CorDoCaido(_jogador, _parceiro, vermelho), "o parceiro caido e' AZUL");
            Assert.AreEqual(vermelho, Dupla.CorDoCaido(_jogador, inimigo, vermelho), "o inimigo caido segue vermelho");
            Assert.AreEqual(vermelho, Dupla.CorDoCaido(_jogador, _jogador, vermelho), "o proprio jogador le' o perigo");
            Assert.AreEqual(vermelho, Dupla.CorDoCaido(null, _parceiro, vermelho), "sem jogador (foto/teste), ninguem e' aliado");
        }

        [Test]
        public void Marcas_AliadoNuncaGanhaMarcaDeInimigo_ESobAMiraEOInimigo()
        {
            var l = new MarcasLogica();
            Vector3 olho = new Vector3(0f, 0.9f, 0f);
            _parceiro.Pos = new Vector3(0f, 0f, 6f);                     // o parceiro BEM na mira
            FakeEntidade inimigo = Inimigo("Brok", new Vector3(0f, 0f, 14f));
            l.Acertou(_parceiro);                                          // nem acerto (se escapar) acende marca no aliado
            l.Atualizar(0.1f, _arena, _jogador, olho, Vector3.forward);
            foreach (MarcasLogica.Marca m in l.Visiveis) Assert.AreNotSame(_parceiro, m.Alvo, "aliado nao ganha marca de inimigo");
            Assert.AreSame(inimigo, l.SobAMira, "sob a mira: o inimigo atras do aliado");
            l.MedirParceiro(0.1f, _parceiro);
            Assert.AreSame(_parceiro, l.Parceiro, "o parceiro tem a marca DELE");
            _parceiro.Vital.Hp = 0f;
            l.MedirParceiro(0.1f, _parceiro);
            Assert.IsNull(l.Parceiro, "morto, sai");
        }

        [Test]
        public void MarcaDoParceiro_ForaDaVista_PresaNaBordaComASetaParaEle()
        {
            var caixa = Rect.MinMaxRect(0.2f, 0.26f, 0.8f, 0.8f);
            Vector2 pos; float ang;
            Assert.IsFalse(MarcasLogica.NaBorda(new Vector2(0.5f, 0.5f), caixa, 2.2f, out pos, out ang), "na vista: no lugar");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), pos);
            Assert.IsTrue(MarcasLogica.NaBorda(new Vector2(1.6f, 0.53f), caixa, 2.2f, out pos, out ang), "a direita, fora");
            Assert.AreEqual(0.8f, pos.x, 1e-4f, "presa na borda direita");
            Assert.Greater(ang, 1.3f, "a seta aponta para a direita (~90 graus)");
            // ATRAS da camera (z positivo no espaco dela): a projecao nao espelha — vai para baixo, do lado dele
            Vector2 vp = MarcasLogica.ParaViewport(new Vector3(-1f, 0f, 5f), 0.6f, 2.2f);
            MarcasLogica.NaBorda(vp, caixa, 2.2f, out pos, out ang);
            Assert.AreEqual(0.26f, pos.y, 1e-3f, "quem esta' as costas vai para a borda de baixo");
            Assert.Less(pos.x, 0.5f, "do lado esquerdo, onde ele esta'");
        }

        [Test]
        public void Espectador_ACameraSegueOParceiroSoComOJogadorFora()
        {
            Assert.AreSame(_jogador, CameraLogica.QuemSeguir(_jogador, false, _parceiro), "jogando: o jogador");
            Assert.AreSame(_parceiro, CameraLogica.QuemSeguir(_jogador, true, _parceiro), "fora: o parceiro");
            Assert.AreSame(_jogador, CameraLogica.QuemSeguir(_jogador, true, null), "sem parceiro vivo: fica no dono");
            Assert.AreEqual("ESPECTANDO · VEU", DuplaHudLogica.TextoEspectando("Veu"));
        }

        [Test]
        public void HudDaDupla_EntreAsFaixas_ForaDosControles_NaAreaSegura()
        {
            // a tela do teste (px = 1, com entalhe) e a do Poco F4 deitado (2400x1080 a 395 ppi = 437 dp: o caso apertado)
            var telas = new[] { new Vector2(1600, 720), new Vector2(2400, 1080) };
            var margens = new[] { new Margens(80, 40, 90, 60), new Margens(0, 0, 0, 0) };
            var pxs = new[] { 1f, 395f / 160f };
            for (int i = 0; i < telas.Length; i++)
            {
                Vector2 tela = telas[i]; Margens m = margens[i]; float px = pxs[i];
                HudLayout l = HudLayout.Calcular(tela, m, px);
                Assert.LessOrEqual(l.Sintonia.yMax, HudAviso.RectFaixa(tela, m, px).yMin, "a faixa da Sintonia mora ABAIXO da de aviso");
                Assert.GreaterOrEqual(l.Sintonia.yMin, tela.y * 0.5f + 104f * px - 0.5f, "e ACIMA da faixa do abate (meio + 68..104 dp)");
                Assert.AreEqual(tela.x * 0.5f, l.Sintonia.center.x, 1e-3f, "centrada");
                Assert.GreaterOrEqual(l.Sintonia.xMin, m.Esq); Assert.LessOrEqual(l.Sintonia.xMax, tela.x - m.Dir);
                foreach (Rect r in new[] { l.Esquiva, l.Tatica, l.Disparo, l.Carrossel, l.Salto })
                    Assert.IsFalse(l.SintoniaPronta.Overlaps(r), "o PRONTA nao cobre controle (" + tela + ")");
                Assert.IsFalse(l.AnelSintonia.Overlaps(l.Esquiva), "o anel nao encosta na esquiva");
                Assert.AreEqual(l.Disparo.center, l.AnelSintonia.center, "o anel abraca o ataque");
                // o anel e o SALTO sao redondos: a medida e' entre os centros
                Assert.Greater(Vector2.Distance(l.AnelSintonia.center, l.Salto.center), (l.AnelSintonia.width + l.Salto.width) * 0.5f, "o anel nao toca o SALTO");
                Assert.IsFalse(l.Espectando.Overlaps(l.Pegar), "o ESPECTANDO fica acima do PEGAR");
                Assert.GreaterOrEqual(l.Espectando.yMin, m.Baixo);
            }
        }

        [Test]
        public void FaixaDaSintonia_SoDaDupla_EOProntaSoNaBorda()
        {
            FakeEntidade a = Inimigo("A", Vector3.zero), b = Inimigo("B", Vector3.one);
            Assert.IsTrue(DuplaHudLogica.DaDupla(_jogador, _jogador, _parceiro));
            Assert.IsFalse(DuplaHudLogica.DaDupla(_jogador, a, b), "a Sintonia do inimigo nao entra na faixa");
            Elemento ea, eb;
            Assert.IsTrue(DuplaHudLogica.ElementosDe(ComboSintonia.TornadoFlamejante, out ea, out eb));
            Assert.AreEqual(Sintonia.ComboDe(ea, eb), ComboSintonia.TornadoFlamejante, "as duas cores sao as do proprio combo");
            Assert.AreNotEqual(ea, eb);
            Assert.AreEqual(7, DuplaHudLogica.Corte("TORNADO FLAMEJANTE"), "parte no espaco do meio");

            var l = new DuplaHudLogica();
            l.Recarregar(0f, 24f);
            l.Tick(0.1f);
            Assert.AreEqual(0f, l.ProntaAlfa, "nascer pronto nao pisca");
            l.Recarregar(12f, 24f);
            Assert.AreEqual(0.5f, l.Recarga, 1e-4f);
            l.Tick(0.1f);
            l.Recarregar(0f, 24f);
            l.Tick(0.1f);
            Assert.Greater(l.ProntaAlfa, 0f, "ficou pronta: SINTONIA PRONTA acende");
            l.Tick(DuplaHudLogica.ProntaS);
            Assert.AreEqual(0f, l.ProntaAlfa, "e apaga");

            l.Canalizar(ComboSintonia.Lamacal, 1f);
            l.Tick(0.5f);
            Assert.AreEqual(0.5f, l.Progresso, 1e-4f, "o trilho enche na canalizacao");
            l.Falhou(ComboSintonia.Lamacal);
            Assert.AreEqual(DuplaHudLogica.Faixa.Quebrada, l.Estado, "falhou: SINTONIA QUEBRADA");
            l.Tick(DuplaHudLogica.QuebradaS + 0.01f);
            Assert.AreEqual(DuplaHudLogica.Faixa.Nada, l.Estado, "e some");
        }
    }
}
