using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>Bau Celestial (_test_bau) e Derrubado (selftest_derrubado.gd), em C#.</summary>
    public class GameplayBauDerrubadoTests
    {
        private List<object[]> _anuncios;
        private List<Vector3> _pousos;
        private List<float> _prog;
        private List<object[]> _abertos;
        private List<IEntidade> _died;
        private List<IEntidade[]> _caidos, _levantados;
        private List<object[]> _derrubadoProg;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Combat.Reset();
            Derrubado.Reset();
            Derrubado.Instalar();
            _anuncios = new List<object[]>(); _pousos = new List<Vector3>(); _prog = new List<float>();
            _abertos = new List<object[]>(); _died = new List<IEntidade>();
            _caidos = new List<IEntidade[]>(); _levantados = new List<IEntidade[]>(); _derrubadoProg = new List<object[]>();
            Bus.BauAnunciado += (p, s) => _anuncios.Add(new object[] { p, s });
            Bus.BauPousou += p => _pousos.Add(p);
            Bus.BauCanalizando += (p, f) => _prog.Add(f);
            Bus.BauAberto += (pl, els) => _abertos.Add(new object[] { pl, els });
            Bus.EntityDied += e => _died.Add(e);
            Bus.EntityDerrubada += (e, c) => _caidos.Add(new[] { e, c });
            Bus.EntityReerguida += (e, p) => _levantados.Add(new[] { e, p });
            Bus.DerrubadoProgresso += (e, esv, r) => _derrubadoProg.Add(new object[] { e, esv, r });
        }

        [TearDown]
        public void TearDown() { Derrubado.Reset(); Combat.Reset(); Bus.Reset(); }

        // ======================================================================= BAU

        private static BauCelestial NovoBau(IRelevo relevo, Loot loot, Dictionary<IEntidade, ArmaSlot> slots, int seed = Loot.SEED_LOOT)
        {
            return new BauCelestial(relevo, loot, e => { ArmaSlot s; return slots != null && slots.TryGetValue(e, out s) ? s : null; }, seed);
        }

        [Test]
        public void Bau_RelogioFixoDentroDaPartida_DeterminismoEAnelEmFracao()
        {
            float pousa = BauCelestial.ANUNCIO_S + BauCelestial.QUEDA_S;
            Assert.Less(pousa, Balance.Match.DurationS, "o bau pousa DENTRO da partida");
            Assert.Greater(Balance.Match.DurationS - pousa, 60f, "sobra briga pela manopla");
            Assert.GreaterOrEqual(BauCelestial.QUEDA_S, 3f, "a queda e' TELEGRAFADA");
            Assert.Greater(BauCelestial.CANALIZAR_S, 0f, "abrir CANALIZA");

            BauCelestial a = NovoBau(null, null, null), b = NovoBau(null, null, null);
            Assert.Less(Vector3.Distance(a.Pos, b.Pos), 0.001f, "MESMO SEED = mesmo ponto de pouso");
            CollectionAssert.AreEqual(a.Elementos, b.Elementos, "MESMO SEED = mesmo par");
            BauCelestial c = NovoBau(null, null, null, 4242);
            Assert.Greater(Vector3.Distance(a.Pos, c.Pos), 0.001f, "seed diferente = ponto diferente");
            float rMin = BauCelestial.RAIO_MIN_F * BauCelestial.RAIO_TERRA_PADRAO;
            float rMax = BauCelestial.RAIO_MAX_F * BauCelestial.RAIO_TERRA_PADRAO;
            float raio = new Vector2(a.Pos.x, a.Pos.z).magnitude;
            Assert.GreaterOrEqual(raio, rMin - 0.01f, "pousa em campo ABERTO (anel)");
            Assert.LessOrEqual(raio, rMax + 0.01f);
            BauCelestial g = NovoBau(new FakeRelevo { Raio = 264f }, null, null);
            Assert.AreEqual(raio * 2f, new Vector2(g.Pos.x, g.Pos.z).magnitude, 0.01f, "mapa com o dobro do raio: o anel dobra junto (fracao)");
            BauCelestial seco = NovoBau(new FakeRelevo { PousarFn = (x, z) => x >= 0f }, null, null, 3);
            Assert.GreaterOrEqual(seco.Pos.x, 0f, "pousa em chao POUSAVEL (nao no lago)");
        }

        [Test]
        public void Bau_EsperandoCaindoPousado_NoRelogio()
        {
            BauCelestial a = NovoBau(null, null, null);
            var p = new FakeEntidade("p", a.Pos, true);
            var todos = new List<IEntidade> { p };
            Assert.AreEqual(BauCelestial.Fase.Esperando, a.FaseAtual, "nasce esperando");
            Assert.AreEqual(BauCelestial.ANUNCIO_S, a.Restante, 0.001f);
            a.Tick(1f, todos);
            Assert.AreEqual(0f, a.Progresso, "bau que ainda NAO pousou nao canaliza");
            a.Tick(BauCelestial.ANUNCIO_S - 1f, todos);
            Assert.AreEqual(BauCelestial.Fase.Caindo, a.FaseAtual, "no tempo do anuncio COMECA A CAIR");
            Assert.AreEqual(1, _anuncios.Count, "BauAnunciado avisa a HUD");
            Assert.Less(Vector3.Distance((Vector3)_anuncios[0][0], a.Pos), 0.001f, "o anuncio leva o ponto EXATO");
            Assert.AreEqual(BauCelestial.QUEDA_S, (float)_anuncios[0][1], 0.001f);
            Assert.IsFalse(a.PodeAbrir, "bau NO AR nao pode ser aberto");
            a.Tick(BauCelestial.QUEDA_S, todos);
            Assert.AreEqual(BauCelestial.Fase.Pousado, a.FaseAtual, "pousou no tempo esperado");
            Assert.IsTrue(a.PodeAbrir);
            Assert.AreEqual(1, _pousos.Count, "BauPousou emitido");
        }

        [Test]
        public void Bau_CanalizaSoComAlguemEmCima_SairCancela_100PorCentoAbreEEquipaAManopla()
        {
            var loot = new Loot(null);
            var p = new FakeEntidade("p", Vector3.zero, true);
            var slot = new ArmaSlot(p);
            slot.Equipar(Arma.VARINHA, null, Elemento.Fogo);  // o teste e' sobre a TROCA no bau
            var slots = new Dictionary<IEntidade, ArmaSlot> { { p, slot } };
            BauCelestial a = NovoBau(null, loot, slots);
            p.Pos = a.Pos;
            var todos = new List<IEntidade> { p };
            a.Tick(BauCelestial.ANUNCIO_S, todos);
            a.Tick(BauCelestial.QUEDA_S, todos);
            Assert.AreEqual(BauCelestial.Fase.Pousado, a.FaseAtual);
            _prog.Clear();

            a.Tick(BauCelestial.CANALIZAR_S * 0.5f, todos);
            Assert.AreEqual(BauCelestial.Fase.Pousado, a.FaseAtual, "meia canalizacao NAO abre");
            Assert.AreEqual(1, _prog.Count);
            Assert.Greater(_prog[0], 0.4f); Assert.Less(_prog[0], 0.6f);

            p.Pos = a.Pos + new Vector3(10f, 0f, 0f);
            a.Tick(0.1f, todos);
            Assert.AreEqual(0f, a.Progresso, "sair do raio CANCELA (volta do ZERO)");
            Assert.AreEqual(0f, _prog[_prog.Count - 1], "e a HUD e' avisada do cancelamento");
            int n = _prog.Count;
            a.Tick(0.1f, todos);
            Assert.AreEqual(n, _prog.Count, "cancelamento sai na BORDA, uma vez");

            // derrubado em cima do bau NAO abre bau
            p.Pos = a.Pos;
            Derrubado.Arena = todos;
            new Derrubado(p).Cair(null);
            a.Tick(1f, todos);
            Assert.AreEqual(0f, a.Progresso, "caido nao canaliza");
            Derrubado.Reerguer(p);

            a.Tick(BauCelestial.CANALIZAR_S + 0.01f, todos);
            Assert.AreEqual(BauCelestial.Fase.Aberto, a.FaseAtual, "canalizacao completa ABRE");
            Assert.AreEqual(Arma.MANOPLA, slot.ArmaId, "abrir equipa a MANOPLA (a unica porta dela)");
            Assert.AreEqual(2, slot.Par.Length, "EXATAMENTE 2 elementos");
            CollectionAssert.AreEqual(a.Elementos, slot.Par, "os do bau (deterministico pelo seed)");
            Assert.AreEqual(1, _abertos.Count);
            Assert.IsTrue((bool)_abertos[0][0], "a manopla foi para o PLAYER");
            CollectionAssert.AreEqual(a.Elementos, (Elemento[])_abertos[0][1], "e leva os 2 elementos para a HUD");
            int velhas = 0;
            foreach (LootItem i in loot.Itens) if (i.ArmaId == Arma.VARINHA) velhas++;
            Assert.AreEqual(1, velhas, "a varinha trocada ficou no chao onde o bau abriu");
            a.Tick(1f, todos);
            Assert.AreEqual(1, _abertos.Count, "aberto nao abre de novo");
        }

        // ================================================================= DERRUBADO

        private static FakeEntidade Pawn(List<IEntidade> arena, Vector3 pos, bool esquadrao = true)
        {
            var p = new FakeEntidade("p" + arena.Count, pos, esquadrao);
            arena.Add(p);
            return p;
        }

        private static List<IEntidade> Arena() { var a = new List<IEntidade>(); Derrubado.Arena = a; return a; }

        /// <summary>Um tique como o loop faria: a janela de DoT do Combat renova (TickDot) e o relogio do caido anda.</summary>
        private static void Tique(Derrubado d) { Combat.TickDot(Derrubado.TIQUE); d.Tique(); }

        [Test]
        public void Derrubado_VidaZeroComEsquadraoCaiEmVezDeMorrer()
        {
            List<IEntidade> a = Arena();
            FakeEntidade p = Pawn(a, Vector3.zero);
            FakeEntidade amigo = Pawn(a, new Vector3(50f, 0f, 0f));
            p.Vital.Hp = 0f;
            Assert.IsTrue(Derrubado.Interceptar(p, amigo), "vida a zero: a costura manda NAO morrer");
            Assert.IsTrue(Derrubado.Esta(p), "entrou em DERRUBADO");
            Assert.Greater(p.Vital.Hp, 0f, "reserva de esvaecimento no lugar da vida zerada");
            Assert.AreEqual(0, _died.Count, "EntityDied NAO sai ao cair");
            Assert.AreEqual(1, _caidos.Count);
            Assert.AreSame(p, _caidos[0][0]); Assert.AreSame(amigo, _caidos[0][1]);
            Assert.AreEqual(0f, p.Vital.Escudo, 0.001f, "derrubado fica SEM escudo");
            Assert.AreEqual(Derrubado.RASTEJO, Derrubado.FatorVelocidade(p), 0.001f, "rastejar pelo produto unico");
            Assert.IsFalse(Derrubado.PodeAgir(p), "derrubado nao conjura");
            Assert.IsTrue(Derrubado.PodeAgir(amigo));
            Assert.Greater(_derrubadoProg.Count, 0, "HUD avisada no instante da queda");
            Assert.AreSame(p, _derrubadoProg[0][0]);
        }

        [Test]
        public void Derrubado_ACosturaNoCombat_VidaZeradaPeloDanoCaiSemEntityDied()
        {
            List<IEntidade> a = Arena();
            FakeEntidade p = Pawn(a, Vector3.zero);
            Pawn(a, new Vector3(3f, 0f, 0f));
            FakeEntidade algoz = Pawn(a, new Vector3(10f, 0f, 0f), false);
            Assert.Greater(Combat.AplicarDano(p, 999f, Elemento.Fogo, algoz), 0f);
            Assert.IsTrue(Derrubado.Esta(p), "o Combat pergunta ao Derrubado antes de matar");
            Assert.AreEqual(0, _died.Count, "e nao emite EntityDied");
            Assert.AreSame(algoz, Derrubado.De(p).Causador, "o causador e' a fonte do dano");
            Assert.AreEqual(Balance.Player.Hp, p.Vital.Hp, 0.001f, "reserva de esvaecimento cheia");
        }

        [Test]
        public void Derrubado_SoloEBotMorremComoSempre()
        {
            List<IEntidade> a = Arena();
            FakeEntidade solo = Pawn(a, Vector3.zero);
            FakeEntidade bot = Pawn(a, new Vector3(2f, 0f, 0f), false);
            solo.Vital.Hp = 0f;
            Assert.IsFalse(Derrubado.Interceptar(solo, null), "player solo: morre, nao cai");
            Assert.IsFalse(Derrubado.Esta(solo));
            Assert.IsFalse(Derrubado.PodeCair(bot), "bot nao tem esquadrao");
        }

        [Test]
        public void Derrubado_EsvaeceEmESVAECER_S_EMorreUmaVez()
        {
            List<IEntidade> a = Arena();
            FakeEntidade p = Pawn(a, Vector3.zero);
            Pawn(a, new Vector3(50f, 0f, 0f));
            p.Vital.Hp = 0f;
            Derrubado.Interceptar(p, null);
            Derrubado d = Derrubado.De(p);
            Assert.AreEqual(1f, d.Esvaecimento, 0.001f, "1.0 = acabou de cair");
            int n = Mathf.RoundToInt(Derrubado.ESVAECER_S / Derrubado.TIQUE);
            for (int i = 0; i < n - 1; i++) Tique(d);
            Assert.IsTrue(Derrubado.Esta(p), "a 29,75 s ainda esvaecendo");
            Assert.AreEqual(0, _died.Count, "nao morreu cedo");
            Assert.Greater(d.Esvaecimento, 0f); Assert.Less(d.Esvaecimento, 0.05f, "a HUD ve' o fim chegando");
            Tique(d);
            Assert.AreEqual(1, _died.Count, "aos 30 s a morte de verdade sai UMA vez");
            Assert.AreSame(p, _died[0]);
            Assert.IsFalse(Derrubado.Esta(p), "estado desfeito na morte");
            Assert.AreEqual(1f, Derrubado.FatorVelocidade(p), "rastejo devolvido");
        }

        [Test]
        public void Derrubado_TickAcumulaA4Hz()
        {
            List<IEntidade> a = Arena();
            FakeEntidade p = Pawn(a, Vector3.zero);
            Pawn(a, new Vector3(50f, 0f, 0f));
            p.Vital.Hp = 0f;
            Derrubado.Interceptar(p, null);
            Derrubado d = Derrubado.De(p);
            float antes = p.Vital.Hp;
            for (int i = 0; i < 6; i++) d.Tick(1f / 60f);   // 0,1 s: nenhum tique
            Assert.AreEqual(antes, p.Vital.Hp, 0.001f, "sem tique antes de 0,25 s");
            Combat.TickDot(0.5f);
            d.Tick(0.4f);                                   // 0,5 s acumulados: 2 tiques
            Assert.AreEqual(antes - 2f * Derrubado.DanoTique, p.Vital.Hp, 0.01f, "4 Hz: dois tiques em meio segundo");
        }

        [Test]
        public void Derrubado_ReerguerDevolveVidaParcialZeroEscudoENivelFica()
        {
            List<IEntidade> a = Arena();
            FakeEntidade caido = Pawn(a, Vector3.zero);
            FakeEntidade medico = Pawn(a, new Vector3(1.5f, 0f, 0f));
            caido.Vital.DanoCausado = Balance.Escudo.Evoluir[2];
            Assert.IsTrue(caido.Vital.Evoluir());
            Assert.AreEqual(3, caido.Vital.Nivel);
            caido.Vital.Escudo = 100f;
            caido.Vital.Hp = 0f;
            Derrubado.Interceptar(caido, null);
            Derrubado d = Derrubado.De(caido);
            int n = Mathf.RoundToInt(Derrubado.REERGUER_S / Derrubado.TIQUE);
            for (int i = 0; i < n - 1; i++) Tique(d);
            Assert.IsTrue(Derrubado.Esta(caido), "ainda caido antes do fim");
            Assert.AreSame(medico, d.Reanimador, "quem canaliza fica marcado");
            Assert.Greater(d.Progresso, 0.9f);
            Tique(d);
            Assert.IsFalse(Derrubado.Esta(caido), "reerguido");
            Assert.AreEqual(Balance.Player.Hp * Derrubado.VIDA_REERGUIDO, caido.Vital.Hp, 0.001f, "vida PARCIAL");
            Assert.AreEqual(0f, caido.Vital.Escudo, 0.001f, "reerguer nao gera escudo");
            Assert.AreEqual(3, caido.Vital.Nivel, "o NIVEL sobrevive");
            Assert.AreEqual(1, _levantados.Count);
            Assert.AreSame(caido, _levantados[0][0]); Assert.AreSame(medico, _levantados[0][1]);
            Assert.AreEqual(0, _died.Count, "reerguer nao emite EntityDied");
            Assert.AreEqual(1f, Derrubado.FatorVelocidade(caido), "volta a andar de pe'");
            Assert.AreEqual(0, _died.Count);
        }

        [Test]
        public void Derrubado_InterromperDecaiSimetrico()
        {
            List<IEntidade> a = Arena();
            FakeEntidade caido = Pawn(a, Vector3.zero);
            FakeEntidade medico = Pawn(a, new Vector3(1.5f, 0f, 0f));
            caido.Vital.Hp = 0f;
            Derrubado.Interceptar(caido, null);
            Derrubado d = Derrubado.De(caido);
            for (int i = 0; i < 8; i++) Tique(d);
            float meio = d.Progresso;
            Assert.Greater(meio, 0f);
            medico.Pos = new Vector3(50f, 0f, 0f);
            Tique(d);
            Assert.IsNull(d.Reanimador, "saiu do raio: para de canalizar");
            Assert.Less(d.Progresso, meio, "DECAI (nao congela)");
            for (int i = 0; i < 8; i++) Tique(d);
            Assert.AreEqual(0f, d.Progresso, 0.001f, "8 pra subir, 8 pra zerar");
            Assert.IsTrue(Derrubado.Esta(caido), "interrompido continua caido");
        }

        [Test]
        public void Derrubado_FinalizacaoNoChaoMataNaHora_UmaVez()
        {
            List<IEntidade> a = Arena();
            FakeEntidade caido = Pawn(a, Vector3.zero);
            Pawn(a, new Vector3(3f, 0f, 0f));   // aliado de pe: sem esquadrao ninguem CAI, morre (SOLO_DERRUBA=false)
            FakeEntidade algoz = Pawn(a, new Vector3(50f, 0f, 0f), false);
            caido.Vital.Hp = 0f;
            Assert.IsTrue(Derrubado.Interceptar(caido, null), "com esquadrao, cai em vez de morrer");
            Assert.IsFalse(Derrubado.Interceptar(caido, algoz), "ja' caido: a costura deixa a finalizacao passar");
            Assert.Greater(Combat.AplicarDano(caido, 999f, Elemento.Fogo, algoz), 0f, "dano ENTRA no derrubado");
            Assert.AreEqual(1, _died.Count, "finalizado: EntityDied UMA vez");
            Assert.AreSame(caido, _died[0]);
            Assert.IsFalse(Derrubado.Esta(caido), "estado desfeito na finalizacao");
        }

        [Test]
        public void Derrubado_GanchosDosKits_JardimDaAuroraELumen()
        {
            List<IEntidade> a = Arena();
            FakeEntidade caido = Pawn(a, Vector3.zero);
            Pawn(a, new Vector3(1.5f, 0f, 0f));
            caido.Vital.Hp = 0f;
            Derrubado.Interceptar(caido, null);
            Derrubado d = Derrubado.De(caido);
            Derrubado.Acelerar(caido, 1.5f, 10f);
            int n = Mathf.RoundToInt(Derrubado.REERGUER_S / (Derrubado.TIQUE * 1.5f));
            for (int i = 0; i < n - 1; i++) Tique(d);
            Assert.IsTrue(Derrubado.Esta(caido), "Jardim: ainda nao no penultimo tique");
            Tique(d);
            Assert.IsFalse(Derrubado.Esta(caido), "Jardim da Aurora: 50% mais rapido");

            Derrubado.Reset();
            List<IEntidade> b = Arena();
            FakeEntidade ferido = Pawn(b, Vector3.zero);
            var lumen = new FakeEntidade("lumen", new Vector3(1f, 0f, 0f), true) { Vital = null };  // PROXY sem corpo
            b.Add(lumen);
            Derrubado.Reanimadores.Add(lumen);
            ferido.Vital.Hp = 0f;
            Assert.IsTrue(Derrubado.Interceptar(ferido, null), "com a Lumen por perto ja' HA' esquadrao");
            Derrubado d2 = Derrubado.De(ferido);
            for (int i = 0; i < Mathf.RoundToInt(Derrubado.REERGUER_S / Derrubado.TIQUE); i++) Tique(d2);
            Assert.IsFalse(Derrubado.Esta(ferido), "Maos Livres: a Lumen reergue sozinha");
            Assert.AreSame(lumen, _levantados[_levantados.Count - 1][1], "o credito vai para a Lumen");
        }
    }
}
