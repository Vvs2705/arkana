using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Menu;
using Arkana.Terrain;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>
    /// O GRIMORIO DE DESCOBERTAS (onda 18C): cada pagina acende pelo evento certo e SO' pelo autor certo (o jogador; a dupla
    /// quando a pagina e' da dupla), acende UMA vez na vida, persiste e volta, e o JURAMENTO (§19.1): as 12 descobertas nao
    /// mexem em Vitalidade nem em Balance. O terreno vai pelo CAMINHO REAL: Projetil.Impacto -> TerrainHit -> TerrenoReativo
    /// -> TerrainChanged, com o tiro na lista de "em voo" como a Partida faz (e fora dela depois do impacto).
    /// </summary>
    public class GameplayGrimorioTests
    {
        // a ilha de mentira do TerrainTests (20x20 celulas de 3 m): mata em cima, dois lagos separados por terra embaixo
        const float LADO = 60f;
        static bool Dentro(int cx, int cz, int x0, int z0, int x1, int z1) => cx >= x0 && cx <= x1 && cz >= z0 && cz <= z1;
        static TipoCelula Tipo(int cx, int cz)
        {
            if (Dentro(cx, cz, 2, 2, 17, 9)) return TipoCelula.Combustivel;
            if (Dentro(cx, cz, 2, 12, 6, 16) || Dentro(cx, cz, 10, 12, 14, 16)) return TipoCelula.Agua;
            return TipoCelula.Chao;
        }

        IPrefs _storeAntes;
        PrefsMemoria _store;
        Grimorio _g;
        List<Projetil> _tiros;
        List<string> _acendeu;
        TerrenoReativo _t;
        FakeEntidade _eu, _parceiro, _inimigoA, _inimigoB;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset(); Arkana.Core.Sintonia.Reset();
            _storeAntes = Grimorio.Store;
            _store = new PrefsMemoria();
            Grimorio.Store = _store;   // NUNCA o PlayerPrefs de quem roda o teste
            _tiros = new List<Projetil>();
            _acendeu = new List<string>();
            _g = NovoGrimorio();
            _t = new TerrenoReativo(new FakeRelevo(), LADO, 7, Tipo);
            _eu = new FakeEntidade("Eu", Vector3.zero, true);
            _parceiro = new FakeEntidade("Parceiro", Vector3.zero);
            _inimigoA = new FakeEntidade("InimigoA", Vector3.zero);
            _inimigoB = new FakeEntidade("InimigoB", Vector3.zero);
            Combat.DefinirTime(_eu, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(_parceiro, Combat.TIME_DO_PLAYER);
            Combat.DefinirTime(_inimigoA, 1);
            Combat.DefinirTime(_inimigoB, 1);
            Bus.EmitMatchStarted();
        }

        [TearDown]
        public void TearDown() { Grimorio.Store = _storeAntes; Bus.Reset(); Combat.Reset(); }

        Grimorio NovoGrimorio()
        {
            var g = new Grimorio { Projeteis = () => _tiros };
            g.Acendeu += id => _acendeu.Add(id);
            g.Ligar();
            return g;
        }

        Vector3 Celula(int cx, int cz) => _t.Centro(_t.Idx(cx, cz));

        /// <summary>Um tiro que morre em (cx, cz): entra na lista de "em voo", impacta no chao, sai — o que a Partida.Tick faz.</summary>
        Projetil Atirar(IEntidade quem, Elemento el, int cx, int cz)
        {
            Projetil p = Projetil.Lancar(quem, Celula(cx, cz) + Vector3.up * 0.2f, Vector3.down, el);
            _tiros.Add(p);
            p.Impacto(null);
            _tiros.Remove(p);
            return p;
        }

        static DisparoSintonia Combo(IEntidade a, IEntidade b) =>
            new DisparoSintonia { Combo = ComboSintonia.TornadoFlamejante, A = a, B = b, ElA = Elemento.Fogo, ElB = Elemento.Vento };

        // ------------------------------------------------------------------ catalogo

        [Test]
        public void Catalogo_12Paginas_SeisDeTerreno_IdsUnicos_TodasComNomeEFrase()
        {
            Assert.AreEqual(12, Grimorio.Paginas.Length);
            Assert.AreEqual(12, Grimorio.Paginas.Distinct().Count(), "id repetido no catalogo");
            Assert.AreEqual((1 << Grimorio.Paginas.Length) - 1, Grimorio.Todas);
            Assert.AreEqual(6, Grimorio.DeTerreno);
            Assert.GreaterOrEqual(Grimorio.DeTerreno, Grimorio.TerrenoParaMapa, "O Mapa e' Arma pede mais interacoes do que existem");
            foreach (string id in Grimorio.Paginas)
            {
                string[] t;
                Assert.IsTrue(Textos.GrimorioPagina.TryGetValue(id, out t), "sem texto: " + id);
                Assert.AreEqual(2, t.Length);
                Assert.IsFalse(string.IsNullOrEmpty(t[0]) || string.IsNullOrEmpty(t[1]), "nome e frase: " + id);
            }
            Assert.AreEqual(12, Textos.GrimorioPagina.Count, "texto de pagina que nao existe no catalogo");
        }

        [Test]
        public void PaginaDoTerreno_EspelhaAQuimica_ETaboaDaAutoria()
        {
            Assert.AreEqual(Grimorio.LagoCongelado, Grimorio.PaginaDoTerreno("ice", false, Elemento.Agua));
            Assert.AreEqual(Grimorio.Conducao, Grimorio.PaginaDoTerreno("electric", false, Elemento.Raio));
            Assert.AreEqual(Grimorio.FogoApagado, Grimorio.PaginaDoTerreno("clear", true, Elemento.Agua));
            Assert.IsNull(Grimorio.PaginaDoTerreno("clear", false, Elemento.Agua), "muro que cai tambem e' clear: so' apagar FOGO conta");
            Assert.AreEqual(Grimorio.MuroDePedra, Grimorio.PaginaDoTerreno("wall", false, Elemento.Terra));
            Assert.AreEqual(Grimorio.Lamacal, Grimorio.PaginaDoTerreno("mud", false, Elemento.Agua));
            Assert.AreEqual(Grimorio.VentoNoFogo, Grimorio.PaginaDoTerreno("burn", false, Elemento.Vento));
            Assert.IsNull(Grimorio.PaginaDoTerreno("burn", false, Elemento.Fogo), "acender a mata com fogo nao e' descoberta");
            Assert.IsNull(Grimorio.PaginaDoTerreno("ash", false, Elemento.Fogo));
            Assert.IsNull(Grimorio.PaginaDoTerreno("wall", false, Elemento.Agua), "elemento errado nao e' autor");
            Assert.IsNull(Grimorio.PaginaDoTerreno("ice", false, Elemento.Raio));
        }

        // ------------------------------------------------------------------ terreno: pelo caminho real

        [Test]
        public void Terreno_TiroDoJogador_AcendeAsSeisPeloCaminhoReal()
        {
            Atirar(_eu, Elemento.Agua, 4, 14);                  // o lago A congela
            Assert.IsTrue(_g.Acesa(Grimorio.LagoCongelado), "agua na agua");
            Atirar(_eu, Elemento.Raio, 12, 14);                 // o lago B (liquido) eletrifica
            Assert.IsTrue(_g.Acesa(Grimorio.Conducao), "raio na agua");
            Atirar(_eu, Elemento.Terra, 18, 18);
            Assert.IsTrue(_g.Acesa(Grimorio.MuroDePedra), "terra ergue muro");
            Assert.AreEqual(EstadoCelula.Muro, _t.Estado(_t.Idx(18, 18)));
            Atirar(_eu, Elemento.Agua, 8, 18);
            Assert.IsTrue(_g.Acesa(Grimorio.Lamacal), "agua na terra");
            Atirar(_inimigoA, Elemento.Fogo, 5, 5);             // o fogo e' de OUTRO: o vento do jogador e' que espalha
            Assert.AreEqual(EstadoCelula.Queimando, _t.Estado(_t.Idx(5, 5)));
            Atirar(_eu, Elemento.Vento, 5, 5);
            Assert.IsTrue(_g.Acesa(Grimorio.VentoNoFogo), "o vento espalhou a chama");
            Atirar(_inimigoA, Elemento.Fogo, 13, 5);
            Atirar(_eu, Elemento.Agua, 13, 5);
            Assert.AreEqual(EstadoCelula.Normal, _t.Estado(_t.Idx(13, 5)), "apagou sem virar carvao");
            Assert.IsTrue(_g.Acesa(Grimorio.FogoApagado), "agua no fogo");
            CollectionAssert.AreEquivalent(Grimorio.Paginas.Take(Grimorio.DeTerreno).ToArray(), _acendeu.ToArray(), "as seis, uma vez cada");
        }

        [Test]
        public void Terreno_TiroDeOutro_NaoAcende_NemDoParceiro()
        {
            foreach (IEntidade quem in new IEntidade[] { _inimigoA, _parceiro })
            {
                _t.Desligar();
                _t = new TerrenoReativo(new FakeRelevo(), LADO, 7, Tipo);   // ilha virgem: cada um muda o terreno de verdade
                Atirar(quem, Elemento.Agua, 4, 14);
                Atirar(quem, Elemento.Raio, 12, 14);
                Atirar(quem, Elemento.Terra, 18, 18);
                Atirar(quem, Elemento.Agua, 8, 18);
                Atirar(quem, Elemento.Fogo, 5, 5);
                Atirar(quem, Elemento.Vento, 5, 5);
            }
            CollectionAssert.IsEmpty(_acendeu, "a descoberta e' do JOGADOR: o tiro do parceiro bot nao ensina nada a ele");
        }

        [Test]
        public void Terreno_TiroDoJogadorAindaEmVoo_OuLonge_NaoLevaOCredito()
        {
            // um tiro de agua do jogador AINDA VOANDO bem em cima do lago que o inimigo congela: o autor e' o inimigo
            Projetil voando = Projetil.Lancar(_eu, Celula(4, 14) + Vector3.up, Vector3.forward, Elemento.Agua);
            _tiros.Add(voando);
            Atirar(_inimigoA, Elemento.Agua, 4, 14);
            Assert.IsFalse(_g.Acesa(Grimorio.LagoCongelado), "tiro vivo nao e' o autor");
            _tiros.Clear();
            // um tiro MORTO do jogador esquecido na lista, a ~30 m: o muro do inimigo nao e' dele
            Projetil longe = Projetil.Lancar(_eu, Celula(2, 18), Vector3.down, Elemento.Terra);
            longe.Impacto(null);
            _tiros.Add(longe);
            Atirar(_inimigoA, Elemento.Terra, 16, 12);
            Assert.AreEqual(EstadoCelula.Muro, _t.Estado(_t.Idx(16, 12)));
            Assert.IsFalse(_g.Acesa(Grimorio.MuroDePedra), "longe demais da celula que mudou");
        }

        [Test]
        public void Terreno_AguaDerrubandoMuro_NaoEFogoApagado()
        {
            Atirar(_inimigoA, Elemento.Terra, 18, 18);
            for (int i = 0; i < 60 && _t.Estado(_t.Idx(18, 18)) == EstadoCelula.Muro; i++) Atirar(_eu, Elemento.Agua, 18, 18);
            Assert.AreEqual(EstadoCelula.Normal, _t.Estado(_t.Idx(18, 18)), "o muro caiu na agua");
            Assert.IsFalse(_g.Acesa(Grimorio.FogoApagado), "clear sem chama nao e' apagar fogo");
        }

        // ------------------------------------------------------------------ dupla e desfecho

        [Test]
        public void Sintonia_ComOJogadorEmQualquerPonta_SoDelaNao()
        {
            Bus.EmitSintoniaDisparou(Combo(_inimigoA, _inimigoB));
            Assert.IsFalse(_g.Acesa(Grimorio.Sintonia), "combo inimigo nao e' descoberta minha");
            Bus.EmitSintoniaDisparou(Combo(_parceiro, _eu));
            Assert.IsTrue(_g.Acesa(Grimorio.Sintonia), "o jogador na 2a ponta tambem conjurou");
        }

        [Test]
        public void PactoQuebrado_OGolpeDoJogadorCortaAInimiga_NaJanela()
        {
            Bus.EmitDamageApplied(_inimigoA, 10f, Elemento.Fogo, _parceiro, false);
            Bus.EmitSintoniaFalhou(ComboSintonia.Eletrocussao, _inimigoA, _inimigoB, Vector3.zero);
            Assert.IsFalse(_g.Acesa(Grimorio.PactoQuebrado), "o corte do parceiro bot nao e' do jogador");
            Bus.EmitDamageApplied(_inimigoB, 10f, Elemento.Fogo, _eu, false);
            _g.Tick(Grimorio.CorteS + 0.5f);
            Bus.EmitSintoniaFalhou(ComboSintonia.Eletrocussao, _inimigoA, _inimigoB, Vector3.zero);
            Assert.IsFalse(_g.Acesa(Grimorio.PactoQuebrado), "golpe velho demais nao cortou nada");
            Bus.EmitDamageApplied(_parceiro, 10f, Elemento.Fogo, _eu, false);   // (sem fogo amigo no jogo; aqui, a prova do MesmoTime)
            Bus.EmitSintoniaFalhou(ComboSintonia.Eletrocussao, _parceiro, _eu, Vector3.zero);
            Assert.IsFalse(_g.Acesa(Grimorio.PactoQuebrado), "a propria Sintonia falhar nao e' corte");
            Bus.EmitDamageApplied(_inimigoB, 10f, Elemento.Fogo, _eu, false);
            _g.Tick(Grimorio.CorteS - 0.5f);
            Bus.EmitSintoniaFalhou(ComboSintonia.Eletrocussao, _inimigoA, _inimigoB, Vector3.zero);
            Assert.IsTrue(_g.Acesa(Grimorio.PactoQuebrado), "o jogador bateu num dos dois ha' menos de CorteS");
        }

        [Test]
        public void Retorno_SoQuandoOJogadorReergue()
        {
            Bus.EmitEntityReerguida(_eu, _parceiro);
            Assert.IsFalse(_g.Acesa(Grimorio.Retorno), "ser reerguido nao e' descoberta");
            Bus.EmitEntityReerguida(_parceiro, null);
            Assert.IsFalse(_g.Acesa(Grimorio.Retorno), "kit/roteiro (por = null)");
            Bus.EmitEntityReerguida(_parceiro, _eu);
            Assert.IsTrue(_g.Acesa(Grimorio.Retorno));
        }

        [Test]
        public void EscudoCoroado_GolpeQueZeraOInimigo_ComEscudoNivel3()
        {
            _inimigoA.Vital.Hp = 0f;
            _parceiro.Vital.Hp = 0f;
            _eu.Vital.DanoCausado = Balance.Escudo.Evoluir[1];
            _eu.Vital.Evoluir();
            Assert.AreEqual(2, _eu.Vital.Nivel);
            Bus.EmitDamageApplied(_inimigoA, 10f, Elemento.Fogo, _eu, false);
            Assert.IsFalse(_g.Acesa(Grimorio.EscudoCoroado), "nivel 2 ainda nao");
            _eu.Vital.DanoCausado = Balance.Escudo.Evoluir[2];
            _eu.Vital.Evoluir();
            Assert.AreEqual(3, _eu.Vital.Nivel);
            Bus.EmitDamageApplied(_inimigoB, 10f, Elemento.Fogo, _eu, false);
            Assert.IsFalse(_g.Acesa(Grimorio.EscudoCoroado), "golpe que NAO zera a vida");
            Bus.EmitDamageApplied(_parceiro, 10f, Elemento.Fogo, _eu, false);
            Assert.IsFalse(_g.Acesa(Grimorio.EscudoCoroado), "aliado nao conta");
            Bus.EmitDamageApplied(_inimigoA, 10f, Elemento.Fogo, _inimigoB, false);
            Assert.IsFalse(_g.Acesa(Grimorio.EscudoCoroado), "o golpe e' de outro");
            Bus.EmitDamageApplied(_inimigoA, 10f, Elemento.Fogo, _eu, false);
            Assert.IsTrue(_g.Acesa(Grimorio.EscudoCoroado));
        }

        [Test]
        public void NoVermelho_AZonaMordeAbaixoDe25_E10sDePe()
        {
            Bus.EmitHealthChanged(30f, 100f);
            Bus.EmitZonaDano(1f, 1f);
            _g.Tick(Grimorio.VermelhoSobreviveS + 1f);
            Assert.IsFalse(_g.Acesa(Grimorio.NoVermelho), "30% nao e' o vermelho");
            Bus.EmitHealthChanged(0f, 100f);                    // a mordida que derruba
            Bus.EmitEntityDerrubada(_eu, null);
            Bus.EmitZonaDano(1f, 1f);
            Bus.EmitHealthChanged(20f, 100f);                   // caido, a reserva de esvaecer baixa: a zona morde, mas nao e' "de pe'"
            Bus.EmitZonaDano(1f, 1f);
            _g.Tick(Grimorio.VermelhoSobreviveS + 1f);
            Assert.IsFalse(_g.Acesa(Grimorio.NoVermelho), "caido nao continuou de pe'");
            Bus.EmitEntityReerguida(_eu, _parceiro);
            Bus.EmitHealthChanged(20f, 100f);
            Bus.EmitZonaDano(1f, 1f);
            _g.Tick(Grimorio.VermelhoSobreviveS * 0.5f);
            Bus.EmitEntityDerrubada(_eu, _inimigoA);            // caiu antes dos 10 s
            _g.Tick(Grimorio.VermelhoSobreviveS);
            Assert.IsFalse(_g.Acesa(Grimorio.NoVermelho), "cair zera o relogio");
            Bus.EmitEntityReerguida(_eu, _parceiro);
            Bus.EmitHealthChanged(20f, 100f);
            Bus.EmitZonaDano(1f, 1f);
            _g.Tick(Grimorio.VermelhoSobreviveS - 0.1f);
            Assert.IsFalse(_g.Acesa(Grimorio.NoVermelho), "ainda nao deu o tempo");
            _g.Tick(0.2f);
            Assert.IsTrue(_g.Acesa(Grimorio.NoVermelho));
        }

        [Test]
        public void MapaArma_VitoriaDepoisDeTresInteracoesDeTerreno_NaMesmaPartida_MesmoJaAcesas()
        {
            _store.GravarInt(Grimorio.PrefChave, 0b111111);    // as 6 de terreno ja' descobertas em outra vida
            _g.Desligar();
            _g = NovoGrimorio();
            Atirar(_eu, Elemento.Agua, 4, 14);
            Atirar(_eu, Elemento.Terra, 18, 18);
            Bus.EmitMatchOver(true);
            Assert.IsFalse(_g.Acesa(Grimorio.MapaArma), "duas nao bastam");
            Bus.EmitMatchStarted();                             // partida nova: a conta recomeca
            Atirar(_eu, Elemento.Raio, 12, 14);
            Atirar(_eu, Elemento.Agua, 8, 18);
            Atirar(_eu, Elemento.Agua, 8, 1);                   // lamacal de novo: distinta nao, repetida
            Bus.EmitMatchOver(true);
            Assert.IsFalse(_g.Acesa(Grimorio.MapaArma), "conta interacoes DISTINTAS da partida (e a anterior nao vale)");
            Atirar(_eu, Elemento.Terra, 1, 18);
            Bus.EmitMatchOver(false);
            Assert.IsFalse(_g.Acesa(Grimorio.MapaArma), "derrota nao e' o mapa como arma");
            Bus.EmitMatchOver(true);
            Assert.IsTrue(_g.Acesa(Grimorio.MapaArma));
            CollectionAssert.AreEqual(new[] { Grimorio.MapaArma }, _acendeu.ToArray(), "as de terreno ja' estavam acesas: nada de aviso repetido");
        }

        // ------------------------------------------------------------------ uma vez na vida, persiste, volta

        [Test]
        public void AcendeUmaVez_GravaNoStore_EVoltaNoGrimorioNovo()
        {
            Bus.EmitSintoniaDisparou(Combo(_eu, _parceiro));
            Bus.EmitSintoniaDisparou(Combo(_eu, _parceiro));
            Bus.EmitEntityReerguida(_parceiro, _eu);
            Bus.EmitEntityReerguida(_parceiro, _eu);
            CollectionAssert.AreEqual(new[] { Grimorio.Sintonia, Grimorio.Retorno }, _acendeu.ToArray(), "cada uma UMA vez");
            Assert.AreEqual(2, _store.Salvos, "grava so' quando acende");
            int esperado = (1 << Grimorio.Indice(Grimorio.Sintonia)) | (1 << Grimorio.Indice(Grimorio.Retorno));
            Assert.AreEqual(esperado, _store.Ints[Grimorio.PrefChave], "uma chave, um inteiro: bit i = Paginas[i]");
            _g.Desligar();
            _acendeu.Clear();
            var outro = NovoGrimorio();                          // "reabriu o jogo"
            Assert.AreEqual(2, outro.Acesas);
            Assert.IsTrue(outro.Acesa(Grimorio.Sintonia) && outro.Acesa(Grimorio.Retorno));
            Bus.EmitSintoniaDisparou(Combo(_eu, _parceiro));
            CollectionAssert.IsEmpty(_acendeu, "ja' acesa na vida passada: sem aviso de novo");
            Assert.AreEqual(2, Grimorio.Contar(Grimorio.MascaraSalva()));
        }

        [Test]
        public void SaveAdulterado_VivePelaMascara_EDesligadoNaoOuve()
        {
            _store.GravarInt(Grimorio.PrefChave, -1);           // todos os bits: so' os 12 do catalogo valem
            Assert.AreEqual(Grimorio.Todas, Grimorio.MascaraSalva());
            Assert.AreEqual(12, Grimorio.Contar(Grimorio.MascaraSalva()));
            _store.GravarFloat(Grimorio.PrefChave, 3.5f);       // tipo errado: livro vazio, sem excecao
            Assert.AreEqual(0, Grimorio.MascaraSalva());
            _g.Desligar();
            Bus.EmitEntityReerguida(_parceiro, _eu);
            Assert.IsFalse(_g.Acesa(Grimorio.Retorno), "desligado nao ouve o Bus");
        }

        // ------------------------------------------------------------------ o juramento (§19.1)

        static string Retrato(params IEntidade[] es) =>
            string.Join("|", es.Select(e => e.Nome + ":" + e.Vital.Hp + "/" + e.Vital.HpMax + "/" + e.Vital.Escudo + "/" + e.Vital.EscudoMax
                + "/" + e.Vital.Nivel + "/" + e.Vital.DanoCausado));

        /// <summary>Todo campo estatico do Balance (e dos blocos dele), arrays por valor: se alguma pagina escrever la', muda.</summary>
        static string RetratoDoBalance()
        {
            var sb = new System.Text.StringBuilder();
            var tipos = new List<Type> { typeof(Balance) };
            tipos.AddRange(typeof(Balance).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic));
            foreach (Type t in tipos)
                foreach (FieldInfo f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    object v = f.GetValue(null);
                    var arr = v as Array;
                    sb.Append(t.Name).Append('.').Append(f.Name).Append('=')
                        .Append(arr != null ? string.Join(",", arr.Cast<object>().Select(x => Convert.ToString(x))) : Convert.ToString(v)).Append(';');
                }
            return sb.ToString();
        }

        [Test]
        public void Juramento_AsDozeDescobertas_NaoMexemEmVidaEscudoNemBalance()
        {
            _inimigoA.Vital.Hp = 0f;
            _eu.Vital.DanoCausado = Balance.Escudo.Evoluir[2];
            _eu.Vital.Evoluir();
            string vidas = Retrato(_eu, _parceiro, _inimigoA, _inimigoB), balance = RetratoDoBalance();

            Atirar(_eu, Elemento.Agua, 4, 14);
            Atirar(_eu, Elemento.Raio, 12, 14);
            Atirar(_eu, Elemento.Terra, 18, 18);
            Atirar(_eu, Elemento.Agua, 8, 18);
            Atirar(_inimigoB, Elemento.Fogo, 5, 5);
            Atirar(_eu, Elemento.Vento, 5, 5);
            Atirar(_inimigoB, Elemento.Fogo, 13, 5);
            Atirar(_eu, Elemento.Agua, 13, 5);
            Bus.EmitSintoniaDisparou(Combo(_eu, _parceiro));
            Bus.EmitDamageApplied(_inimigoA, 10f, Elemento.Fogo, _eu, false);   // zera o A com escudo 3 (e corta a Sintonia dele)
            Bus.EmitSintoniaFalhou(ComboSintonia.Lamacal, _inimigoA, _inimigoB, Vector3.zero);
            Bus.EmitEntityReerguida(_parceiro, _eu);
            Bus.EmitHealthChanged(20f, 100f);
            Bus.EmitZonaDano(1f, 1f);
            _g.Tick(Grimorio.VermelhoSobreviveS + 0.1f);
            Bus.EmitMatchOver(true);

            Assert.AreEqual(12, _g.Acesas, "as doze acenderam: " + string.Join(",", _acendeu));
            Assert.AreEqual(vidas, Retrato(_eu, _parceiro, _inimigoA, _inimigoB), "o grimorio escreveu em vida/escudo de alguem");
            Assert.AreEqual(balance, RetratoDoBalance(), "o grimorio escreveu no Balance");
            // e o catalogo nao TEM onde pendurar poder: so' ids (string) — nenhum campo de pagina e' numero de jogo
            Assert.AreEqual(typeof(string[]), typeof(Grimorio).GetField("Paginas").FieldType);
        }

        // ------------------------------------------------------------------ o aviso e o livro (logica pura)

        [Test]
        public void Aviso_Fila_UmaDepoisDaOutra_ComEntradaELeituraESaida()
        {
            var a = new AvisoGrimorioLogica();
            Assert.AreEqual(0f, a.Alfa);
            a.Enfileirar(Grimorio.Sintonia);
            a.Enfileirar(Grimorio.EscudoCoroado);
            Assert.AreEqual(Grimorio.Sintonia, a.Atual);
            Assert.AreEqual(0f, a.Alfa, 1e-4f, "nasce apagado e acende");
            a.Tick(AvisoGrimorioLogica.EntraS);
            Assert.AreEqual(1f, a.Alfa, 1e-4f);
            Assert.AreEqual(1f, a.Entrada, 1e-4f);
            a.Tick(AvisoGrimorioLogica.FicaS + AvisoGrimorioLogica.SaiS * 0.5f);
            Assert.AreEqual(0.5f, a.Alfa, 1e-3f, "esvaece na saida");
            a.Tick(AvisoGrimorioLogica.SaiS);
            Assert.AreEqual(Grimorio.EscudoCoroado, a.Atual, "a segunda entra quando a primeira sai");
            a.Tick(AvisoGrimorioLogica.Total + 0.01f);
            Assert.IsNull(a.Atual);
            Assert.AreEqual(0f, a.Alfa);
        }

        /// <summary>Poco F4 (o aparelho), 16:9 estreito, 1440p denso, 720p ralo e um 360 dp de altura (o vao nao cabe: encosta no joystick).</summary>
        static readonly Vector2[] Telas = { new Vector2(2400, 1080), new Vector2(1920, 1080), new Vector2(3200, 1440), new Vector2(1600, 720), new Vector2(1280, 720) };
        static readonly float[] Dpis = { 395f, 420f, 515f, 280f, 320f };

        [Test]
        public void Aviso_ForaDaColunaDaMira_ForaDosBotoes_NaAreaSegura()
        {
            for (int i = 0; i < Telas.Length; i++)
                foreach (var m in new[] { new Margens(0, 0, 0, 0), new Margens(96f, 0, 96f, 24f) })
                {
                    Vector2 tela = Telas[i];
                    float px = Dp.PxCom(1f, Dpis[i]);
                    Rect r = AvisoGrimorioLogica.Retangulo(tela, m, px);
                    HudLayout l = HudLayout.Calcular(tela, m, px);
                    string onde = tela + " @" + Dpis[i] + " margem " + m.Esq;
                    Assert.IsTrue(r.xMin >= m.Esq && r.yMin >= m.Baixo && r.xMax <= tela.x - m.Dir && r.yMax <= tela.y - m.Topo, "fora da area segura " + onde);
                    Assert.IsFalse(r.Overlaps(AvisoGrimorioLogica.ColunaDaMira(tela)), "na coluna da mira " + onde);
                    // o PERMANENTE (controles e placas) nunca; o TRANSITORIO (kill feed, faixa da Sintonia, espectador) nao se cobre
                    // no aparelho do Diretor — em tela de 360 dp de altura a propria HUD ja' nao tem vao para todos
                    foreach (var b in new[] { l.Joystick, l.Disparo, l.Esquiva, l.Tatica, l.Suprema, l.Salto, l.Carrossel, l.Pegar, l.Barras, l.Topo, l.Pausa, l.Minimapa })
                        Assert.IsFalse(r.Overlaps(b), "cobre controle/placa da HUD " + b + " " + onde);
                    if (i == 0)
                        foreach (var b in new[] { l.KillFeed, l.Sintonia, l.Espectando })
                            Assert.IsFalse(r.Overlaps(b), "no Poco F4 cabe no vao, sem cobrir aviso transitorio " + b);
                    Assert.GreaterOrEqual(r.height, 48f * px, "le' de relance");
                }
        }

        [Test]
        public void Livro_ApagadaNuncaEntregaNomeNemFrase_EContaSeteDeDoze()
        {
            foreach (string id in Grimorio.Paginas)
            {
                string[] apagada = TelaGrimorio.Rotulo(id, false), acesa = TelaGrimorio.Rotulo(id, true);
                Assert.AreEqual(Textos.GrimorioTrancada, apagada[0]);
                Assert.AreEqual("", apagada[1]);
                Assert.AreEqual(Textos.GrimorioPagina[id][0], acesa[0]);
                Assert.AreEqual(Textos.GrimorioPagina[id][1], acesa[1]);
            }
            Assert.AreEqual("7/12", TelaGrimorio.Contagem(0b1010_1101_0101));
            Assert.AreEqual("0/12", TelaGrimorio.Contagem(0));
            Assert.AreEqual("12/12", TelaGrimorio.Contagem(-1));
        }
    }
}
