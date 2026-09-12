using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>A tempestade: invariantes de selftest_zona.gd, em C#.</summary>
    public class GameplayZonaTests
    {
        private List<float> _aberturas;
        private List<float[]> _formacoes;
        private List<object[]> _avisos, _fechamentos;
        private List<float[]> _zdano;
        private List<bool> _zestado;
        private List<object[]> _danos;

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Combat.Reset();
            _aberturas = new List<float>();
            _formacoes = new List<float[]>();
            _avisos = new List<object[]>();
            _fechamentos = new List<object[]>();
            _zdano = new List<float[]>();
            _zestado = new List<bool>();
            _danos = new List<object[]>();
            Bus.ZonaAbertura += s => _aberturas.Add(s);
            Bus.ZonaFormando += (r, d) => _formacoes.Add(new[] { r, d });
            Bus.ZonaAvisou += (f, c, r, s) => _avisos.Add(new object[] { f, c, r, s });
            Bus.ZonaFechando += (f, c, r, d) => _fechamentos.Add(new object[] { f, c, r, d });
            Bus.ZonaDano += (d, dps) => _zdano.Add(new[] { d, dps });
            Bus.ZonaEstado += dentro => _zestado.Add(dentro);
            Bus.DamageApplied += (t, a, e, s, esc) => _danos.Add(new object[] { t, a, e, s, esc });
        }

        // ------------------------------------------------ 1. o desenho das fases

        [Test]
        public void Desenho_AberturaMaisCincoFasesCabemNaPartidaEApertam()
        {
            float total = Zona.ABERTURA_S + Zona.FORMACAO_S;
            foreach (Zona.Fase f in Zona.FASES) total += f.Espera + f.Fecha;
            Assert.Less(total, Balance.Match.DurationS, "a tempestade INTEIRA cabe na partida");
            Assert.GreaterOrEqual(Balance.Match.DurationS - total, 30f, "sobra tempo para a QUEDA");
            Assert.AreEqual(70f, Zona.ABERTURA_S, "a abertura e' 1:10");
            float[] esperadas = { 60f, 50f, 40f, 30f };
            for (int i = 0; i < esperadas.Length; i++)
                Assert.AreEqual(esperadas[i], Zona.FASES[i].Espera, 0.001f, "janelas 1:00, 50, 40, 30");
            float frac = 1f, dps = 0f;
            foreach (Zona.Fase f in Zona.FASES)
            {
                Assert.Less(f.Frac, frac, "cada fase encolhe");
                Assert.Greater(f.Dps, dps, "o dano CRESCE a cada fase");
                frac = f.Frac; dps = f.Dps;
            }
            for (int i = 1; i < esperadas.Length; i++)
                Assert.Less(Zona.FASES[i].Espera, Zona.FASES[i - 1].Espera, "cada janela e' menor: a partida ACELERA");
            Zona.Fase ultima = Zona.FASES[Zona.FASES.Length - 1];
            Assert.AreEqual(0f, ultima.Frac, 0.0001f, "a ULTIMA fase fecha em ZERO");
            Assert.Greater(ultima.Dps * ultima.Fecha, Balance.Player.Hp, "o colapso resolve a partida");
            Assert.GreaterOrEqual(Zona.TICK_S, 0.25f, "tique proprio, nao por frame");
            Assert.GreaterOrEqual(Zona.FASES[0].Dps * Zona.TICK_S, 1f, "o MENOR tique ja' vale >= 1 — nunca int(0)");
        }

        // ------------------------------------------------ 1b. na queda nao ha' limite

        /// <summary>
        /// Visto na FOTO de 11/09: quem pousa no mar, fora do raio da ilha, recebia "VOLTE PARA A ZONA" nos 70 s de
        /// abertura — de uma tempestade que ainda nao existe (o Godot tinha o mesmo defeito). A HUD nao avisa de
        /// zona que nao ha'; quando ela se FORMA, o aviso vem, na borda.
        /// </summary>
        [Test]
        public void Abertura_ForaDoMapaNaoEAvisadoDeTempestadeQueNaoExiste()
        {
            var z = new Zona(null, 4242);
            var player = new FakeEntidade("p", new Vector3(200f, 0f, 0f), true);   // no mar, alem do raio do mapa
            var alvos = new List<IEntidade> { player };
            z.LigarNoPouso();

            for (int i = 0; i < 60; i++) z.Tick(1f, alvos);   // 60 dos 70 s de abertura
            Assert.IsFalse(z.Ativa, "ainda na abertura");
            CollectionAssert.DoesNotContain(_zestado, false, "sem tempestade, ninguem esta' FORA dela");

            for (int i = 0; i < 12; i++) z.Tick(1f, alvos);   // a abertura acaba: a parede se forma
            Assert.IsTrue(z.Ativa, "a tempestade existe");
            Assert.Contains(false, _zestado, "agora sim: fora da zona, o aviso vem");
            Assert.AreEqual(1, _zestado.FindAll(d => !d).Count, "na BORDA, uma vez");
        }

        [Test]
        public void Inerte_AteOPousoNaoHaCronometroNemDanoNemParede()
        {
            var z = new Zona(null, 4242);
            var player = new FakeEntidade("p", new Vector3(200f, 0f, 0f), true);  // FORA de qualquer circulo
            var alvos = new List<IEntidade> { player };

            Assert.IsFalse(z.Ligada, "recem-criada NAO esta' ligada");
            Assert.IsFalse(z.Ativa);
            Assert.AreEqual(0f, z.Restante, "nenhum cronometro correndo antes do pouso");
            Assert.AreEqual(0f, z.DpsAtual, "e nao doi");
            Assert.AreEqual(Zona.RAIO_MAPA_PADRAO, z.Raio, 0.001f, "o raio nasce na BORDA do mapa");
            Assert.IsTrue(z.Dentro(new Vector3(Zona.RAIO_MAPA_PADRAO - 1f, 0f, 0f)), "na beira do mapa esta' DENTRO");

            z.Tick(500f, alvos);   // meia partida de relogio sem pouso
            Assert.IsFalse(z.Ligada, "o tempo nao liga a zona: so' o pouso");
            Assert.AreEqual(0, _aberturas.Count + _formacoes.Count + _avisos.Count + _fechamentos.Count, "nenhum sinal");
            Assert.AreEqual(0, _danos.Count, "nenhum dano — nem para quem esta' longe");
            Assert.AreEqual(Balance.Player.Hp, player.Vital.Hp, 0.001f);
        }

        [Test]
        public void Abertura_PousoAbre70sDepoisFormaDepoisAnunciaO1oCirculo()
        {
            var z = new Zona(null, 4242);
            z.LigarNoPouso();
            Assert.AreEqual(1, _aberturas.Count);
            Assert.AreEqual(70f, _aberturas[0], 0.001f, "o pouso abre o cronometro de 1:10");
            Assert.AreEqual(70f, z.Restante, 0.001f, "relogio armado nos 70s");
            Assert.IsTrue(z.Ligada);
            Assert.IsFalse(z.Ativa, "durante a abertura a tempestade AINDA nao existe");
            Assert.AreEqual(0, _avisos.Count, "nenhum circulo anunciado na abertura");

            z.LigarNoPouso();
            Assert.AreEqual(1, _aberturas.Count, "LigarNoPouso de novo NAO reinicia (idempotente)");

            z.Tick(70f);
            Assert.AreEqual(1, _formacoes.Count, "acabado o 1:10, a tempestade SE FORMA");
            Assert.IsTrue(z.Ativa, "a tempestade passa a existir");
            Assert.AreEqual(Zona.RAIO_MAPA_PADRAO, _formacoes[0][0], 0.001f, "ela para na BORDA do mapa");
            Assert.AreEqual(Zona.FORMACAO_S, _formacoes[0][1], 0.001f);
            Assert.AreEqual(0f, z.DpsAtual, "formar NAO doi");
            Assert.AreEqual(0, _avisos.Count, "durante a formacao o 1o circulo ainda nao foi anunciado");
            Assert.Greater(z.Raio, Zona.RAIO_MAPA_PADRAO, "a parede comeca FORA do mapa e entra");

            z.Tick(Zona.FORMACAO_S);
            Assert.AreEqual(1, _avisos.Count, "formada e parada, o 1o circulo e' anunciado");
            Assert.AreEqual(Zona.FASES[0].Espera, (float)_avisos[0][3], 0.001f, "a janela e' a da fase 1");
            Assert.AreEqual(0, z.FaseAtual, "a fase 1 ainda nao FECHOU");
            Assert.AreEqual(Zona.RAIO_MAPA_PADRAO, z.Raio, 0.001f, "parada na costa");
        }

        // ------------------------------------------------ 1c. cada partida, circulos diferentes

        [Test]
        public void Sorteio_SeedPorPartidaDiferenteEMesmoSeedRepete()
        {
            var seeds = new HashSet<int>();
            var chaves = new HashSet<string>();
            for (int i = 0; i < 12; i++)
            {
                var z = new Zona(null);
                seeds.Add(z.SeedDaPartida);
                string chave = "";
                foreach (Zona.Circulo c in z.Plano) chave += c.Centro.x.ToString("F1") + "," + c.Centro.z.ToString("F1") + ";";
                chaves.Add(chave);
            }
            Assert.GreaterOrEqual(seeds.Count, 11, "12 partidas sortearam seeds distintos — o seed NAO e' fixo");
            Assert.GreaterOrEqual(chaves.Count, 11, "sequencias de circulos distintas: nao da' para decorar");

            var za = new Zona(null, 4242);
            var zb = new Zona(null, 4242);
            Assert.AreEqual(4242, za.SeedDaPartida, "o seed da partida fica GUARDADO (rede)");
            for (int i = 0; i < za.Plano.Length; i++)
                Assert.Less(Vector3.Distance(za.Plano[i].Centro, zb.Plano[i].Centro), 0.0001f, "MESMO seed = MESMA sequencia");
        }

        // ------------------------------------------------ 2. o determinismo e a lei do circulo contido

        [Test]
        public void Determinismo_MesmoSeedMesmosCentros_ContidosEDentroDoMapa()
        {
            Zona.Circulo[] a = Zona.Planejar(null);
            Zona.Circulo[] b = Zona.Planejar(null);
            Assert.AreEqual(Zona.FASES.Length, a.Length, "uma entrada por fase");
            for (int i = 0; i < a.Length; i++)
                Assert.Less(Vector3.Distance(a[i].Centro, b[i].Centro), 0.0001f, "MESMO SEED = centros identicos");
            Zona.Circulo[] c = Zona.Planejar(null, 9191);
            bool diferente = false;
            for (int i = 0; i < a.Length; i++) if (Vector3.Distance(a[i].Centro, c[i].Centro) > 0.001f) diferente = true;
            Assert.IsTrue(diferente, "seed diferente = centros diferentes");

            Vector3 pc = Vector3.zero;
            float pr = Zona.RAIO_MAPA_PADRAO;
            foreach (Zona.Circulo e in a)
            {
                Assert.LessOrEqual(Vector3.Distance(e.Centro, pc) + e.Raio, pr + 0.001f, "todo circulo novo esta' CONTIDO no anterior");
                Assert.LessOrEqual(new Vector2(e.Centro.x, e.Centro.z).magnitude + e.Raio, Zona.RAIO_MAPA_PADRAO + 0.001f, "nenhum circulo passa da borda");
                pc = e.Centro; pr = e.Raio;
            }
        }

        [Test]
        public void Fracao_MapaComODobroDoRaioDaCirculosComODobroDoRaio()
        {
            Zona.Circulo[] p1 = Zona.Planejar(new FakeRelevo { Raio = 132f }, 1234);
            Zona.Circulo[] p2 = Zona.Planejar(new FakeRelevo { Raio = 264f }, 1234);
            for (int i = 0; i < p1.Length; i++)
                Assert.AreEqual(p1[i].Raio * 2f, p2[i].Raio, 0.001f, "fracao nao envelhece: o dobro do mapa, o dobro do circulo");
            Assert.AreEqual(Zona.RAIO_MAPA_PADRAO, Zona.RaioDoMapa(null), "sem ilha, cai no padrao");
        }

        [Test]
        public void ProximoCentro_SempreEmChaoPousavel()
        {
            // metade oeste e' agua: nenhum centro pode cair la'
            var relevo = new FakeRelevo { PousarFn = (x, z) => x >= 0f };
            Zona.Circulo[] plano = Zona.Planejar(relevo, 7);
            foreach (Zona.Circulo c in plano)
                Assert.GreaterOrEqual(c.Centro.x, 0f, "o circulo nao pode ser um lago onde ninguem pisa");
        }

        // ------------------------------------------------ 3. o relogio das fases

        [Test]
        public void Relogio_AvisaAntesFechaNoTempoCertoFaseAFase_UltimaEmZero()
        {
            var z = new Zona(null, 4242);
            z.LigarNoPouso();
            z.Tick(Zona.ABERTURA_S);
            z.Tick(Zona.FORMACAO_S);
            Assert.AreEqual(1, _avisos.Count, "o 1o circulo DEPOIS da abertura");

            for (int i = 0; i < Zona.FASES.Length; i++)
            {
                Zona.Fase f = Zona.FASES[i];
                Assert.AreEqual(f.Espera, z.Restante, 0.001f, "fase " + (i + 1) + ": relogio armado na espera");
                Assert.IsFalse(z.Fechando, "durante a espera a parede esta' PARADA");
                int antes = _fechamentos.Count;
                z.Tick(f.Espera);
                Assert.IsTrue(z.Fechando, "fase " + (i + 1) + ": no tempo do aviso a parede ANDA");
                Assert.AreEqual(i + 1, z.FaseAtual, "contador de fase avancou");
                Assert.AreEqual(antes + 1, _fechamentos.Count, "ZonaFechando avisou a HUD");
                Assert.AreEqual(f.Fecha, (float)_fechamentos[_fechamentos.Count - 1][3], 0.001f, "o fechamento dura o projetado");
                Assert.AreEqual(f.Fecha, z.Restante, 0.001f, "relogio rearmado para o fechamento");
                float raioAntes = z.Raio;
                z.Tick(f.Fecha * 0.5f);
                Assert.Less(z.Raio, raioAntes, "no meio do fechamento a parede ja' andou");
                int avisosAntes = _avisos.Count;
                z.Tick(f.Fecha * 0.5f);
                Assert.AreEqual(z.Plano[i].Raio, z.Raio, 0.001f, "fase " + (i + 1) + ": o raio parou EXATO");
                Assert.Less(Vector3.Distance(z.Centro, z.Plano[i].Centro), 0.001f, "e o centro tambem");
                Assert.AreEqual(f.Dps, z.DpsAtual, 0.001f, "o dano da fase");
                if (i + 1 < Zona.FASES.Length)
                {
                    Assert.AreEqual(avisosAntes + 1, _avisos.Count, "o PROXIMO circulo ja' e' publico");
                    Assert.AreEqual(z.Plano[i + 1].Raio, (float)_avisos[_avisos.Count - 1][2], 0.001f, "o aviso leva o raio seguinte");
                }
                else Assert.AreEqual(avisosAntes, _avisos.Count, "ultima fase: nao ha' proximo circulo");
            }
            Assert.AreEqual(0f, z.Raio, 0.001f, "depois da ultima fase o raio e' ZERO: o mapa inteiro e' tempestade");
            Assert.IsFalse(z.Dentro(Vector3.zero), "ninguem esta' dentro de um circulo de raio zero");
            Assert.AreEqual(0f, z.Restante, "e a tempestade nao avanca mais");
        }

        // ------------------------------------------------ 4. o dano

        [Test]
        public void Dano_ForaSangraDentroNao_1xPorSegundo_EstadoSoNaBorda()
        {
            var z = new Zona(null, 4242);
            z.ForcarCirculo(1, Vector3.zero, 30f);
            var dentro = new FakeEntidade("player", new Vector3(5f, 1f, 0f), true);
            var fora = new FakeEntidade("bot", new Vector3(50f, 1f, 0f));
            var alvos = new List<IEntidade> { dentro, fora };

            Assert.IsTrue(z.Dentro(dentro.Pos));
            Assert.IsFalse(z.Dentro(fora.Pos));
            Assert.IsTrue(z.Dentro(new Vector3(30f, 40f, 0f)), "a zona e' CILINDRO: subir no plato nao tira ninguem");

            float hpDentro = dentro.Vital.Hp, hpFora = fora.Vital.Hp;
            z.Tique(alvos);
            float esperado = Zona.FASES[0].Dps * Zona.TICK_S;
            Assert.AreEqual(hpDentro, dentro.Vital.Hp, 0.001f, "quem esta' DENTRO nao perde vida");
            Assert.AreEqual(hpFora - esperado, fora.Vital.Hp, 0.001f, "quem esta' FORA perde dps x tique (ignora escudo)");
            Assert.AreEqual(1, _danos.Count, "UM sinal de dano por tique");
            Assert.IsNull(_danos[0][3], "a tempestade nao tem autor (ambiente)");
            Assert.Greater((float)_danos[0][1], 0f, "o dano-zero de 60Hz nao existe aqui");
            Assert.AreEqual(0, _zdano.Count, "bot fora NAO emite ZonaDano (so' o player)");
            Assert.AreEqual(0, _zestado.Count, "player dentro: nenhuma borda emitida");

            dentro.Pos = new Vector3(45f, 1f, 0f);
            z.Tique(alvos);
            Assert.AreEqual(1, _zestado.Count);
            Assert.IsFalse(_zestado[0], "atravessou a parede: ZonaEstado(false) UMA vez");
            Assert.AreEqual(1, _zdano.Count, "e o player levou o tique");
            z.Tique(alvos);
            Assert.AreEqual(1, _zestado.Count, "continuar fora NAO repete a borda");
            Assert.AreEqual(2, _zdano.Count, "mas o dano continua cobrando");
            dentro.Pos = new Vector3(0f, 1f, 0f);
            z.Tique(alvos);
            Assert.AreEqual(2, _zestado.Count);
            Assert.IsTrue(_zestado[1], "voltou para dentro: borda(true)");

            fora.Vital.Hp = 0f;
            int antes = _danos.Count;
            z.Tique(alvos);
            Assert.AreEqual(antes, _danos.Count, "pawn morto nao leva dano da zona");
        }

        [Test]
        public void Dano_TickAcumulaEDispara1xPorTICK_S()
        {
            var z = new Zona(null, 4242);
            z.ForcarCirculo(1, Vector3.zero, 30f);
            var fora = new FakeEntidade("bot", new Vector3(50f, 1f, 0f));
            var alvos = new List<IEntidade> { fora };
            z.Tick(0.5f, alvos);
            Assert.AreEqual(0, _danos.Count, "meio segundo: ainda sem tique");
            z.Tick(0.5f, alvos);
            Assert.AreEqual(1, _danos.Count, "1 s: um tique");
            for (int i = 0; i < 61; i++) z.Tick(1f / 60f, alvos);
            Assert.AreEqual(2, _danos.Count, "~60 frames = mais UM tique, nao 60");
        }
    }
}
