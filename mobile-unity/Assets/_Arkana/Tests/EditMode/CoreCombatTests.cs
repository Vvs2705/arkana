using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Tests
{
    /// <summary>Os invariantes de gameplay/selftest.gd (dano, NaN, escudo, evolucao, DoT), em C#.</summary>
    public class CoreCombatTests
    {
        private sealed class FakeEntidade : IEntidade
        {
            public string Nome { get; set; }
            public Vector3 Pos { get; set; }
            public bool EhPlayer { get; set; }
            public Vitalidade Vital { get; set; }

            public FakeEntidade(string nome, float hp = 100f, bool player = false)
            {
                Nome = nome;
                EhPlayer = player;
                Vital = new Vitalidade(hp);
            }
        }

        private const float Eps = 0.001f;
        private List<float> _danos;
        private List<bool> _emEscudo;
        private List<IEntidade> _mortos;
        private List<IEntidade> _quebras;
        private List<int> _niveis;      // ShieldChanged: level
        private List<IEntidade> _quem;  // ShieldChanged: entity

        [SetUp]
        public void SetUp()
        {
            Bus.Reset();
            Combat.Reset();
            _danos = new List<float>();
            _emEscudo = new List<bool>();
            _mortos = new List<IEntidade>();
            _quebras = new List<IEntidade>();
            _niveis = new List<int>();
            _quem = new List<IEntidade>();
            Bus.DamageApplied += (t, a, e, s, esc) => { _danos.Add(a); _emEscudo.Add(esc); };
            Bus.EntityDied += e => _mortos.Add(e);
            Bus.ShieldBroken += e => _quebras.Add(e);
            Bus.ShieldChanged += (e, sh, mx, lv) => { _quem.Add(e); _niveis.Add(lv); };
        }

        [Test]
        public void NaN_Zero_E_Negativo_NaoAplicam_E_NaoEmitem()
        {
            FakeEntidade d = new FakeEntidade("d");
            Assert.AreEqual(0f, Combat.AplicarDano(d, float.NaN, Elemento.Fogo, null));
            Assert.AreEqual(0f, Combat.AplicarDano(d, -5f, Elemento.Fogo, null));
            Assert.AreEqual(0f, Combat.AplicarDano(d, 0f, Elemento.Fogo, null));
            Assert.AreEqual(100f, d.Vital.Hp, Eps, "hp intacto");
            Assert.AreEqual(Balance.Escudo.Niveis[0], d.Vital.Escudo, Eps, "escudo intacto");
            Assert.AreEqual(0, _danos.Count, "nenhum sinal emitido");
            Assert.AreEqual(0, _quem.Count);
        }

        [Test]
        public void Dano_ChegaAoBarramento_ComValorEfetivo()
        {
            FakeEntidade d = new FakeEntidade("d");
            d.Vital.Escudo = 0f;
            Assert.AreEqual(13f, Combat.AplicarDano(d, 13f, Elemento.Fogo, null), Eps);
            Assert.AreEqual(87f, d.Vital.Hp, Eps);
            Assert.AreEqual(1, _danos.Count);
            Assert.AreEqual(13f, _danos[0], Eps);
            Assert.IsFalse(_emEscudo[0]);
        }

        [Test]
        public void Escudo_AbsorveAntesDaVida_E_Transborda()
        {
            FakeEntidade p = new FakeEntidade("p");
            float n1 = Balance.Escudo.Niveis[0];
            Assert.AreEqual(n1, p.Vital.Escudo, Eps, "nasce com N1 cheio");
            Assert.AreEqual(1, p.Vital.Nivel);

            Assert.AreEqual(20f, Combat.AplicarDano(p, 20f, Elemento.Fogo, null), Eps, "devolve o efetivo");
            Assert.AreEqual(n1 - 20f, p.Vital.Escudo, Eps, "escudo absorveu 20");
            Assert.AreEqual(100f, p.Vital.Hp, Eps, "a VIDA nao foi tocada");
            Assert.AreEqual(1, _quem.Count, "ShieldChanged avisa a HUD");
            Assert.AreEqual(0, _quebras.Count, "escudo em pe' nao anuncia quebra");
            Assert.IsTrue(_emEscudo[0], "DamageApplied marca onShield");

            float antes = p.Vital.Escudo + p.Vital.Hp;
            Assert.AreEqual(40f, Combat.AplicarDano(p, 40f, Elemento.Fogo, null), Eps, "tiro que estoura o escudo");
            Assert.AreEqual(0f, p.Vital.Escudo, Eps, "escudo zerou");
            Assert.AreEqual(90f, p.Vital.Hp, Eps, "os 10 que sobraram TRANSBORDARAM");
            Assert.AreEqual(40f, antes - (p.Vital.Escudo + p.Vital.Hp), Eps, "nenhum ponto se perdeu na fronteira");
            Assert.AreEqual(1, _quebras.Count, "ShieldBroken na quebra");
            Assert.AreSame(p, _quebras[0]);
        }

        [Test]
        public void Multiplicadores_DoElemento_EscudoEVida()
        {
            float n1 = Balance.Escudo.Niveis[0];
            FakeEntidade r = new FakeEntidade("raio");
            Combat.AplicarDano(r, 20f, Elemento.Raio, null);
            Assert.AreEqual(n1 - 20f * 1.25f, r.Vital.Escudo, Eps, "RAIO 1.25x em escudo");

            FakeEntidade w = new FakeEntidade("vento");
            Combat.AplicarDano(w, 20f, Elemento.Vento, null);
            Assert.AreEqual(n1 - 20f * 0.75f, w.Vital.Escudo, Eps, "VENTO 0.75x em escudo");

            FakeEntidade e = new FakeEntidade("terra");
            e.Vital.Escudo = 0f;
            Combat.AplicarDano(e, 20f, Elemento.Terra, null);
            Assert.AreEqual(100f - 20f * 1.15f, e.Vital.Hp, Eps, "TERRA 1.15x em vida");

            // transbordo com esc != 1: a sobra volta a dano CRU antes de virar vida
            FakeEntidade t = new FakeEntidade("t");
            t.Vital.Escudo = 25f;
            Combat.AplicarDano(t, 50f, Elemento.Raio, null);   // pede 62.5, come 25, sobram 30 crus
            Assert.AreEqual(0f, t.Vital.Escudo, Eps);
            Assert.AreEqual(70f, t.Vital.Hp, Eps, "sobra convertida ao dano cru (esc 1.25)");
        }

        [Test]
        public void ShieldBroken_SaiUmaVez()
        {
            FakeEntidade p = new FakeEntidade("p");
            Combat.AplicarDano(p, 50f, Elemento.Fogo, null);
            Assert.AreEqual(0f, p.Vital.Escudo, Eps);
            Assert.AreEqual(1, _quebras.Count);
            Combat.AplicarDano(p, 10f, Elemento.Fogo, null);
            Assert.AreEqual(1, _quebras.Count, "escudo ja' zerado nao anuncia de novo");
            Assert.AreEqual(90f, p.Vital.Hp, Eps);
        }

        [Test]
        public void EntityDied_UmaVez_E_MortoNaoRecebeMais()
        {
            FakeEntidade b = new FakeEntidade("bot");
            Assert.Greater(Combat.AplicarDano(b, 9999f, Elemento.Fogo, null), 0f, "dano letal aplicado");
            Assert.AreEqual(0f, b.Vital.Hp);
            Assert.IsFalse(b.Vital.Viva);
            Assert.AreEqual(1, _mortos.Count);
            Assert.AreSame(b, _mortos[0]);
            int eventos = _danos.Count;
            Assert.AreEqual(0f, Combat.AplicarDano(b, 10f, Elemento.Fogo, null), "morto nao toma dano de novo");
            Assert.AreEqual(1, _mortos.Count, "EntityDied so' uma vez");
            Assert.AreEqual(eventos, _danos.Count, "morto nao emite DamageApplied");
            Assert.AreEqual(0f, Combat.AplicarDot(b, 10f, 0.25f, "burn", null), "morto nao toma DoT");
        }

        [Test]
        public void Efetivo_NaoInflaOverkill()
        {
            FakeEntidade b = new FakeEntidade("bot");
            b.Vital.Escudo = 0f;
            b.Vital.Hp = 10f;
            FakeEntidade fonte = new FakeEntidade("f");
            Assert.AreEqual(10f, Combat.AplicarDano(b, 200f, Elemento.Fogo, fonte), Eps);
            Assert.AreEqual(10f, fonte.Vital.DanoCausado, Eps, "credita o que a barra perdeu, nao os 200");
        }

        [Test]
        public void Derrubado_Intercepta_A_Morte()
        {
            FakeEntidade b = new FakeEntidade("bot");
            Combat.InterceptarMorte = (alvo, fonte) => true;
            Combat.AplicarDano(b, 9999f, Elemento.Fogo, null);
            Assert.AreEqual(0f, b.Vital.Hp);
            Assert.AreEqual(0, _mortos.Count, "interceptado: cai, nao morre");
        }

        [Test]
        public void Fonte_AcumulaDanoCausado_E_EvoluiComTransbordoDeCapacidade()
        {
            float[] passos = Balance.Escudo.Evoluir;
            float[] niveis = Balance.Escudo.Niveis;
            FakeEntidade atacante = new FakeEntidade("a");
            FakeEntidade alvo = new FakeEntidade("alvo", 5000f);
            alvo.Vital.Escudo = 0f;
            atacante.Vital.Escudo = 10f;   // sujo de proposito: evoluir NAO pode curar o que faltava

            Combat.AplicarDano(alvo, passos[1] - 1f, Elemento.Fogo, atacante);
            Assert.AreEqual(1, atacante.Vital.Nivel, "1 abaixo do passo: continua N1");
            Combat.AplicarDano(alvo, 1f, Elemento.Fogo, atacante);
            Assert.AreEqual(2, atacante.Vital.Nivel, "150 de dano causado sobe para N2");
            Assert.AreEqual(niveis[1], atacante.Vital.EscudoMax, Eps);
            Assert.AreEqual(10f + niveis[1] - niveis[0], atacante.Vital.Escudo, Eps, "entrega SO' a capacidade nova");
            Assert.IsTrue(_quem.Contains(atacante), "ShieldChanged da FONTE ao evoluir");
            Assert.AreEqual(2, _niveis[_quem.IndexOf(atacante)]);

            Combat.AplicarDano(alvo, passos[2] - passos[1], Elemento.Fogo, atacante);
            Assert.AreEqual(3, atacante.Vital.Nivel, "400 -> N3");
            Combat.AplicarDano(alvo, passos[3] * 2f, Elemento.Fogo, atacante);
            Assert.AreEqual(4, atacante.Vital.Nivel, "dano de sobra nao passa do N4");
            Assert.AreEqual(niveis[3], atacante.Vital.EscudoMax, Eps);
        }

        [Test]
        public void AntiFarm_NaoCredita_SiMesmo_Nem_Aliado()
        {
            FakeEntidade solo = new FakeEntidade("solo", 5000f);
            Combat.AplicarDano(solo, 300f, Elemento.Fogo, solo);
            Assert.AreEqual(0f, solo.Vital.DanoCausado, "dano em SI MESMO nao credita");

            FakeEntidade a1 = new FakeEntidade("a1", 100f, true);
            FakeEntidade a2 = new FakeEntidade("a2", 5000f, true);
            Combat.AplicarDano(a2, 300f, Elemento.Fogo, a1);
            Assert.AreEqual(0f, a1.Vital.DanoCausado, "dano em ALIADO nao credita");
        }

        [Test]
        public void Dot_IgnoraEscudo_E_VaiDiretoNaVida()
        {
            FakeEntidade p = new FakeEntidade("p");
            float sh0 = p.Vital.Escudo;
            float tick = Balance.Dot.Tick;
            float ef = Combat.AplicarDot(p, Balance.Status.BurnDps, tick, "burn", null);
            Assert.AreEqual(Balance.Status.BurnDps * tick, ef, Eps);
            Assert.AreEqual(sh0, p.Vital.Escudo, Eps, "queimadura NAO toca o escudo");
            Assert.AreEqual(100f - Balance.Status.BurnDps * tick, p.Vital.Hp, Eps, "vai direto na VIDA");
            Assert.AreEqual(1, _danos.Count);
            Assert.IsFalse(_emEscudo[0]);
        }

        [Test]
        public void Dot_RespeitaTetoSomado_E_RenovaNoTique()
        {
            FakeEntidade t = new FakeEntidade("t");
            float tick = Balance.Dot.Tick;
            float teto = Balance.Dot.TetoDps * tick;   // 3.0 por janela
            Assert.AreEqual(2.5f, Combat.AplicarDot(t, 10f, tick, "burn", null), Eps, "1a fonte passa inteira");
            Assert.AreEqual(0.5f, Combat.AplicarDot(t, 10f, tick, "terrain", null), Eps, "2a fonte so' o que sobra do teto");
            Assert.AreEqual(0f, Combat.AplicarDot(t, 999f, tick, "electric", null), "3a fonte: teto cheio, nada");
            Assert.AreEqual(100f - teto, t.Vital.Hp, Eps, "12 dps somados, nao importa quantas fontes");

            Combat.TickDot(tick);
            Assert.AreEqual(teto, Combat.AplicarDot(t, 999f, tick, "burn", null), Eps, "janela nova: teto renovado, fonte absurda segurada");
            Assert.AreEqual(100f - 2f * teto, t.Vital.Hp, Eps);

            foreach (float a in _danos) Assert.Greater(a, 0f, "nenhum evento de dano sai com amount 0");
        }

        [Test]
        public void Dot_NaN_E_Zero_NaoAplicam()
        {
            FakeEntidade t = new FakeEntidade("t");
            Assert.AreEqual(0f, Combat.AplicarDot(t, float.NaN, 0.25f, "burn", null));
            Assert.AreEqual(0f, Combat.AplicarDot(t, 4f, 0f, "burn", null));
            Assert.AreEqual(0f, Combat.AplicarDot(t, 4f, float.NaN, "burn", null));
            Assert.AreEqual(100f, t.Vital.Hp, Eps);
            Assert.AreEqual(0, _danos.Count);
        }

        [Test]
        public void Empurrao_MultiplicaPeloElemento()
        {
            Assert.AreEqual(Balance.Combate.Knockback * 2.0f, Combat.Empurrao(Elemento.Vento), Eps);
            Assert.AreEqual(Balance.Combate.Knockback * 1.2f, Combat.Empurrao(Elemento.Terra), Eps);
            Assert.AreEqual(Balance.Combate.Knockback, Combat.Empurrao(Elemento.Fogo), Eps);
        }
    }
}
