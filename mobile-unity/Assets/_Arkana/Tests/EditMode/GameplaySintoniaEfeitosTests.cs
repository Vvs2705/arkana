using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Terrain;

namespace Arkana.Tests
{
    /// <summary>Os 10 combos da Sintonia NO MUNDO (GDD §9/§14, PONTE A4): golpe so' no inimigo, o que fica, counters e terreno.</summary>
    public class GameplaySintoniaEfeitosTests
    {
        private const float DT = 0.05f;
        private static readonly Vector3 P = Vector3.zero;

        private Partida _m;
        private FakeEntidade _a, _b;
        private List<object[]> _danos;
        private List<KeyValuePair<IEntidade, Vector3>> _empurroes;
        private Action<IEntidade, Vector3> _empurrarOriginal;
        private readonly List<TerrenoReativo> _terrenos = new List<TerrenoReativo>();

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset(); SintoniaEfeitos.Reset();
            Arkana.Menu.Menu.PedidoDeTreino = false;
            _m = new Partida(new FakeRelevo());
            _m.Iniciar(1, 0, true);
            SintoniaEfeitos.Reset();
            SintoniaEfeitos.Instalar();
            _danos = new List<object[]>();
            _empurroes = new List<KeyValuePair<IEntidade, Vector3>>();
            Bus.DamageApplied += (t, q, el, f, esc) => _danos.Add(new object[] { t, q, el, f });
            _empurrarOriginal = SintoniaEfeitos.Empurrar;
            SintoniaEfeitos.Empurrar = (e, v) => _empurroes.Add(new KeyValuePair<IEntidade, Vector3>(e, v));
            // a dupla atira de longe (30 m): o combo cai no ponto P
            _a = Registrar("a", new Vector3(-30f, 0f, 0f), 1);
            _b = Registrar("b", new Vector3(-30f, 0f, 4f), 1);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (TerrenoReativo t in _terrenos) t.Desligar();
            _terrenos.Clear();
            SintoniaEfeitos.Empurrar = _empurrarOriginal;
            _m.Encerrar();
            SintoniaEfeitos.Reset(); Efeitos.Reset(); Combat.Reset(); Bus.Reset();
        }

        private FakeEntidade Registrar(string nome, Vector3 pos, int time)
        {
            var e = new FakeEntidade(nome, pos);
            _m.Registrar(e, null);
            Combat.DefinirTime(e, time);
            return e;
        }

        private DisparoSintonia D(ComboSintonia c, IEntidade alvo = null)
        {
            Elemento x, y;
            SintoniaEfeitos.Par(c, out x, out y);
            return new DisparoSintonia { Combo = c, A = _a, B = _b, ElA = x, ElB = y, Ponto = P, Alvo = alvo, Dano = 40f };
        }

        /// <summary>O que a Partida.Tick faz com a Sintonia e os estados (sem projetil, zona nem terreno andando).</summary>
        private void Tick(float s)
        {
            for (float t = 0f; t < s - 1e-4f; t += DT)
            {
                SintoniaEfeitos.Tick(DT);
                foreach (IEntidade e in _m.Arena) if (e.Vital.Viva) Efeitos.Tick(e, DT);
            }
        }

        private TerrenoReativo Terreno(TipoCelula tipo)
        {
            var t = new TerrenoReativo(new FakeRelevo(), 90f, 7, (cx, cz) => tipo);
            _terrenos.Add(t);
            _m.Terreno = t;
            return t;
        }

        private static readonly ComboSintonia[] Todos = (ComboSintonia[])Enum.GetValues(typeof(ComboSintonia));

        [Test]
        public void CadaCombo_DanoSoNoInimigo_ComAFonteA()
        {
            FakeEntidade aliado = Registrar("aliado", P + new Vector3(1f, 0f, 0f), 1);
            FakeEntidade inimigo = Registrar("inimigo", P + new Vector3(-1f, 0f, 0f), 2);
            FakeEntidade longe = Registrar("longe", P + new Vector3(40f, 0f, 0f), 2);
            FakeEntidade esquivando = Registrar("esquivando", P + new Vector3(0f, 0f, 1f), 2);
            Assert.AreEqual(10, Todos.Length, "os 10 pares do GDD §9");
            foreach (ComboSintonia c in Todos)
            {
                SintoniaEfeitos.Reset(); Efeitos.Reset();
                foreach (FakeEntidade e in new[] { _a, _b, aliado, inimigo, longe, esquivando }) e.Vital.Reset();
                Efeitos.De(esquivando).IframesLeft = 0.1f;
                _danos.Clear(); _empurroes.Clear();
                SintoniaEfeitos.Disparar(D(c, inimigo));

                Assert.IsTrue(_danos.Exists(x => x[0] == inimigo && x[3] == _a), c + ": o inimigo no raio apanha, com a fonte A");
                Assert.IsFalse(_danos.Exists(x => x[0] == esquivando), c + ": a esquiva e' imunidade TOTAL");
                foreach (object[] x in _danos)
                {
                    Assert.AreSame(_a, x[3], c + ": todo dano do combo tem a fonte A");
                    Assert.IsFalse(x[0] == aliado || x[0] == _a || x[0] == _b, c + ": aliado nao apanha do combo (A4)");
                    Assert.AreNotSame(longe, x[0], c + ": fora do raio nao apanha");
                }
                Assert.AreEqual(aliado.Vital.HpMax, aliado.Vital.Hp, c + ": vida do aliado intacta");
                Assert.AreEqual(Balance.Escudo.Niveis[0], aliado.Vital.Escudo, 1e-4f, c + ": escudo do aliado intacto");
                Assert.IsFalse(_empurroes.Exists(x => x.Key == aliado), c + ": aliado nao e' empurrado");
                Assert.AreEqual(0f, Efeitos.De(aliado).StunLeft, c + ": aliado nao e' atordoado");
            }
        }

        [Test]
        public void Instalar_LigaOGanchoDaSintonia_ResetLimpaOQueFicou()
        {
            Sintonia.Efeito = null;
            SintoniaEfeitos.Instalar();
            Assert.IsNotNull(Sintonia.Efeito, "Instalar liga Sintonia.Efeito");
            Sintonia.Efeito(D(ComboSintonia.Lamacal));
            Assert.AreEqual(1, SintoniaEfeitos.Ativos.Count, "o gancho instalado E' o Disparar: a lama ficou");
            Sintonia.Efeito(D(ComboSintonia.CristaisCarregados));
            Assert.AreEqual(1 + SintoniaEfeitos.Cristais, SintoniaEfeitos.Ativos.Count, "as minas ficaram");
            SintoniaEfeitos.Reset();
            Assert.AreEqual(0, SintoniaEfeitos.Ativos.Count, "Reset limpa os persistentes");
            SintoniaEfeitos.Disparar(D(ComboSintonia.ChuvaDeMagma));
            Bus.EmitTerrainHit(Elemento.Agua, P, false);
            Tick(DT);
            Assert.AreEqual(1, SintoniaEfeitos.Ativos.Count, "Reset solta o ouvido do Bus: sem Instalar, a agua nao chega");
        }

        [Test]
        public void Tornado_AndaAteOInimigo_ESugaSoOInimigo()
        {
            FakeEntidade inimigo = Registrar("inimigo", P + new Vector3(6f, 0f, 0f), 2);
            FakeEntidade aliado = Registrar("aliado", P + new Vector3(-3f, 0f, 0f), 1);
            SintoniaEfeitos.Disparar(D(ComboSintonia.TornadoFlamejante, inimigo));
            Assert.AreEqual(1, SintoniaEfeitos.Ativos.Count);
            SintoniaEfeitos.Persistente t = SintoniaEfeitos.Ativos[0];
            Tick(1f);
            Assert.AreEqual(SintoniaEfeitos.TornadoVel * 1f, t.Pos.x, 0.15f, "ANDA para o inimigo, na velocidade dele");
            Assert.IsTrue(_empurroes.Exists(x => x.Key == inimigo), "SUGA o inimigo no raio");
            foreach (KeyValuePair<IEntidade, Vector3> x in _empurroes)
            {
                Assert.AreNotSame(aliado, x.Key, "o parceiro nao e' sugado");
                Assert.Less(x.Value.x, 0f, "o puxao aponta PARA o funil");
                Assert.LessOrEqual(x.Value.magnitude, SintoniaEfeitos.PuxaoM + 1e-4f);
            }
            Assert.AreEqual(0f, Efeitos.De(inimigo).BurnLeft, "longe do funil: puxado, nao queimado");
            inimigo.Pos = t.Pos + new Vector3(0.5f, 0f, 0f);
            Tick(0.3f);
            Assert.Greater(Efeitos.De(inimigo).BurnLeft, 0f, "no funil pega fogo");
            Assert.AreEqual(0f, Efeitos.De(aliado).BurnLeft);
        }

        [Test]
        public void Nuvem_PersegueOAlvo_DescarregaComAFonteA_EAcabaEm6s()
        {
            Assert.AreEqual(6f, Balance.Sintonia.Duracao(ComboSintonia.NuvemTempestuosa), 1e-4f, "GDD §9: persegue por 6 s");
            FakeEntidade alvo = Registrar("alvo", P, 2);
            SintoniaEfeitos.Disparar(D(ComboSintonia.NuvemTempestuosa, alvo));
            Assert.AreEqual(1, SintoniaEfeitos.Ativos.Count);
            SintoniaEfeitos.Persistente n = SintoniaEfeitos.Ativos[0];
            alvo.Pos = P + new Vector3(10f, 0f, 0f);   // correu para fora do raio (5 m)
            _danos.Clear();
            Tick(2f);
            Assert.Less((n.Pos - alvo.Pos).magnitude, 0.2f, "a nuvem PERSEGUE o alvo");
            Assert.IsTrue(_danos.Exists(x => x[0] == alvo && x[3] == _a && (Elemento)x[2] == Elemento.Raio), "descarga de raio no alvo, fonte A");
            Tick(6f - 2f - 0.1f);
            Assert.AreEqual(1, SintoniaEfeitos.Ativos.Count, "viva ate' os 6 s");
            Tick(0.2f);
            Assert.AreEqual(0, SintoniaEfeitos.Ativos.Count, "acaba em 6 s");
        }

        [Test]
        public void Lamacal_LentificaTodoMundoNaLama_ENaoDobraComATerraEncharcada()
        {
            FakeEntidade inimigo = Registrar("inimigo", P, 2);
            FakeEntidade aliado = Registrar("aliado", P + new Vector3(2f, 0f, 0f), 1);
            FakeEntidade fora = Registrar("fora", P + new Vector3(30f, 0f, 0f), 2);
            SintoniaEfeitos.Disparar(D(ComboSintonia.Lamacal));
            Tick(0.3f);
            Assert.AreEqual(Balance.Terrain.MudSlow, Efeitos.De(inimigo).StatusMult, 1e-4f, "lamacal lentifica");
            Assert.AreEqual(Balance.Terrain.MudSlow, Efeitos.De(aliado).StatusMult, 1e-4f, "a lama e' CHAO: pega a dupla tambem (§14)");
            Assert.AreEqual(1f, Efeitos.De(fora).StatusMult, 1e-4f, "fora da lama corre");
            inimigo.Pos = fora.Pos;
            Tick(1f);
            Assert.AreEqual(1f, Efeitos.De(inimigo).StatusMult, 1e-4f, "saiu da lama: volta a correr");

            // com terreno: a agua da receita vira LAMA na celula — o fator de terreno ja' cobra, o status nao dobra
            SintoniaEfeitos.Reset(); Efeitos.Reset();
            SintoniaEfeitos.Instalar();
            TerrenoReativo t = Terreno(TipoCelula.Chao);
            inimigo.Pos = P + new Vector3(Balance.Sintonia.Raio(ComboSintonia.Lamacal) * 0.85f, 0f, 0f);
            SintoniaEfeitos.Disparar(D(ComboSintonia.Lamacal));
            Tick(0.3f);
            Assert.AreEqual(Balance.Terrain.MudSlow, t.FatorTerreno(inimigo.Pos), 1e-4f, "a receita encharcou ate' a borda do raio");
            Assert.AreEqual(Balance.Terrain.MudSlow, t.FatorTerreno(inimigo.Pos) * Efeitos.De(inimigo).StatusMult, 1e-4f,
                "UMA lentidao so' (o Roblox: nao dobrar a poca)");
        }

        [Test]
        public void ZonasDeChao_PegamTodoMundo_ComoOTerreno()
        {
            // A4: o que vira CHAO (lava, vapor, areia) pega a dupla tambem, e sem autor (dano de ambiente, fonte nula)
            foreach (ComboSintonia c in new[] { ComboSintonia.ChuvaDeMagma, ComboSintonia.CortinaDeVapor, ComboSintonia.TempestadeDeAreia })
            {
                SintoniaEfeitos.Reset(); Efeitos.Reset(); Combat.Reset();
                FakeEntidade aliado = Registrar("aliado-" + c, P + new Vector3(1f, 0f, 0f), 1);
                FakeEntidade inimigo = Registrar("inimigo-" + c, P + new Vector3(-1f, 0f, 0f), 2);
                Combat.DefinirTime(_a, 1); Combat.DefinirTime(_b, 1);
                SintoniaEfeitos.Disparar(D(c));
                _danos.Clear();
                Tick(1f);
                Combat.TickDot(1f);
                Assert.IsTrue(_danos.Exists(x => x[0] == aliado && x[3] == null), c + ": o chao pega o parceiro (fonte nula)");
                Assert.IsTrue(_danos.Exists(x => x[0] == inimigo && x[3] == null), c + ": e o inimigo");
                aliado.Pos = inimigo.Pos = new Vector3(0f, 0f, 60f);   // fora da proxima zona
            }
        }

        [Test]
        public void Eletrocussao_AtordoaOInimigo_PeloTetoDoKernel()
        {
            FakeEntidade inimigo = Registrar("inimigo", P, 2);
            FakeEntidade aliado = Registrar("aliado", P + new Vector3(1f, 0f, 0f), 1);
            Efeitos.Atordoar(inimigo, 0.5f);   // ja' vinha atordoado: nao SOMA (acima de 0,8 s o jogador perde o controle)
            SintoniaEfeitos.Disparar(D(ComboSintonia.Eletrocussao));
            Assert.AreEqual(SintoniaEfeitos.StunEletro, Efeitos.De(inimigo).StunLeft, 1e-4f, "atordoa 0,7 s");
            Assert.LessOrEqual(Efeitos.De(inimigo).StunLeft, Balance.Status.StunCap, "o teto do kernel vale");
            Assert.AreEqual(0f, Efeitos.De(aliado).StunLeft, "o parceiro na poca nao e' atordoado pelo combo");
            Assert.AreEqual(0, SintoniaEfeitos.Ativos.Count, "golpe: o eletrico que fica e' o do terreno");
        }

        [Test]
        public void Cristais_SaoMinas_AtordoamAoPisarEAoQuebrar()
        {
            FakeEntidade inimigo = Registrar("inimigo", P, 2);
            FakeEntidade outro = Registrar("outro", P + new Vector3(0f, 0f, -40f), 2);
            FakeEntidade aliado = Registrar("aliado", P + new Vector3(0f, 0f, 40f), 1);
            SintoniaEfeitos.Disparar(D(ComboSintonia.CristaisCarregados, inimigo));
            Assert.AreEqual(SintoniaEfeitos.Cristais, SintoniaEfeitos.Ativos.Count, "as minas nascem");
            Tick(0.3f);
            Assert.AreEqual(0f, Efeitos.De(inimigo).StunLeft, "o estouro do combo nao atordoa: quem atordoa e' a MINA");
            Assert.AreEqual(SintoniaEfeitos.Cristais, SintoniaEfeitos.Ativos.Count, "ninguem pisou");

            // o PARCEIRO passa pela mina sem disparar; o inimigo pisa e ela estoura
            SintoniaEfeitos.Persistente m0 = SintoniaEfeitos.Ativos[0];
            aliado.Pos = m0.Pos;
            Tick(0.3f);
            Assert.AreEqual(SintoniaEfeitos.Cristais, SintoniaEfeitos.Ativos.Count, "aliado nao dispara a mina");
            inimigo.Pos = m0.Pos + new Vector3(0.5f, 0f, 0f);
            Tick(0.3f);
            Assert.IsTrue(m0.Quebrou, "pisou: quebrou");
            Assert.Greater(Efeitos.De(inimigo).StunLeft, 0f, "atordoa ao quebrar");
            Assert.AreEqual(0f, Efeitos.De(aliado).StunLeft, "o estouro nao atordoa o parceiro");
            Assert.AreEqual(SintoniaEfeitos.Cristais - 1, SintoniaEfeitos.Ativos.Count);

            // qualquer golpe (TerrainHit) quebra outra mina e atordoa o inimigo do lado
            SintoniaEfeitos.Persistente m1 = SintoniaEfeitos.Ativos[0];
            outro.Pos = m1.Pos + new Vector3(2f, 0f, 0f);   // fora do gatilho, dentro do estouro
            Bus.EmitTerrainHit(Elemento.Fogo, m1.Pos, false);
            Assert.IsTrue(m1.Quebrou, "um tiro na mina a quebra");
            Assert.Greater(Efeitos.De(outro).StunLeft, 0f, "e o estouro atordoa quem estava perto");
            Assert.LessOrEqual(Efeitos.De(outro).StunLeft, Balance.Status.StunCap);
            Tick(DT);
            Assert.AreEqual(SintoniaEfeitos.Cristais - 2, SintoniaEfeitos.Ativos.Count);
        }

        [Test]
        public void Counters_OElementoCertoDesfazAZona()
        {
            var contra = new Dictionary<ComboSintonia, Elemento>
            {
                { ComboSintonia.TornadoFlamejante, Elemento.Agua }, { ComboSintonia.ChuvaDeMagma, Elemento.Agua },
                { ComboSintonia.CortinaDeVapor, Elemento.Vento }, { ComboSintonia.Lamacal, Elemento.Fogo },
                { ComboSintonia.TempestadeDeAreia, Elemento.Agua }, { ComboSintonia.NuvemTempestuosa, Elemento.Terra },
            };
            foreach (KeyValuePair<ComboSintonia, Elemento> kv in contra)
            {
                SintoniaEfeitos.Reset();
                SintoniaEfeitos.Instalar();
                SintoniaEfeitos.Disparar(D(kv.Key));
                Assert.AreEqual(1, SintoniaEfeitos.Ativos.Count, kv.Key + " fica");
                foreach (Elemento el in Elementos.Todos)
                {
                    if (el == kv.Value) continue;
                    Bus.EmitTerrainHit(el, P, false);
                }
                Tick(DT);
                Assert.AreEqual(1, SintoniaEfeitos.Ativos.Count, kv.Key + ": so' o counter desfaz");
                Bus.EmitTerrainHit(kv.Value, P + new Vector3(2f, 0f, 0f), false);
                Tick(DT);
                Assert.AreEqual(0, SintoniaEfeitos.Ativos.Count, kv.Key + ": " + kv.Value + " desfaz");
            }
            // GDD §9, o exemplo do proprio texto: "Torrencial apaga Magma"
            SintoniaEfeitos.Reset();
            SintoniaEfeitos.Instalar();
            SintoniaEfeitos.Disparar(D(ComboSintonia.ChuvaDeMagma));
            SintoniaEfeitos.Disparar(D(ComboSintonia.TempestadeTorrencial));
            Tick(DT);
            Assert.AreEqual(0, SintoniaEfeitos.Ativos.Count, "Torrencial apaga Magma");
        }

        [Test]
        public void Receita_OrdemEAusenciasDoRoblox_EFogoDeUmaFrenteSo()
        {
            // Eletrocussao NAO molha: a agua congelaria o lago e mataria a conducao
            TerrenoReativo lago = Terreno(TipoCelula.Agua);
            SintoniaEfeitos.Disparar(D(ComboSintonia.Eletrocussao));
            Assert.AreEqual(EstadoCelula.Eletrificado, lago.EstadoEm(P), "a poca inteira eletriza");
            lago.Desligar();

            // Tornado NAO venta: a chama recem-nascida fica acesa
            TerrenoReativo mata = Terreno(TipoCelula.Combustivel);
            SintoniaEfeitos.Disparar(D(ComboSintonia.TornadoFlamejante));
            Assert.AreEqual(EstadoCelula.Queimando, mata.EstadoEm(P), "o tornado incendeia");
            // Vapor so' molha: vapor e' fogo APAGADO
            SintoniaEfeitos.Disparar(D(ComboSintonia.CortinaDeVapor));
            Assert.AreEqual(EstadoCelula.Normal, mata.EstadoEm(P), "o vapor apaga o que queimava");
            mata.Reset();

            // O FOGO do combo e' UMA ignicao: um orcamento de combustivel so' (§14), por maior que seja o raio
            SintoniaEfeitos.Disparar(D(ComboSintonia.ChuvaDeMagma));
            for (int i = 0; i < 60; i++) mata.Simular(0f);   // relogio parado: so' o orcamento segura a frente
            Assert.Greater(mata.Queimando, 0, "o magma acende a mata");
            Assert.LessOrEqual(mata.Queimando, 9 + Balance.Terrain.FuelBudget, "3x3 de semente + UM orcamento");
        }
    }
}
