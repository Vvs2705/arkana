using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Terrain;

namespace Arkana.Tests
{
    /// <summary>
    /// Os MESMOS invariantes de mobile-godot/godot/terrain/selftest.gd, numa ilha de mentira de 20x20 celulas:
    /// fogo por ORCAMENTO (uma rolagem por aresta, nunca por tique), agua apaga sem carbonizar, carvao nao barra
    /// tiro, gelo vira rota, raio eletrifica so' a agua CONECTADA, muro deterministico (907) que cai por hp e por
    /// prazo e nao empareda ninguem, lama 0.55, vento atica pagando do mesmo orcamento, DoT pelo Combat com fonte null.
    /// </summary>
    public class TerrainTests
    {
        // A ilha de mentira (20x20, celula de 3 m, lado 60 m): floresta em cima, dois lagos separados por terra embaixo.
        private const int N = 20;
        private const float LADO = 60f;
        private static readonly Vector2Int FLORESTA_MIN = new Vector2Int(2, 2), FLORESTA_MAX = new Vector2Int(17, 9);
        private static readonly Vector2Int LAGO_A_MIN = new Vector2Int(2, 12), LAGO_A_MAX = new Vector2Int(6, 16);
        private static readonly Vector2Int LAGO_B_MIN = new Vector2Int(10, 12), LAGO_B_MAX = new Vector2Int(14, 16);

        private List<string> _mudancas;

        private static TipoCelula Tipo(int cx, int cz)
        {
            if (Dentro(cx, cz, FLORESTA_MIN, FLORESTA_MAX)) return TipoCelula.Combustivel;
            if (Dentro(cx, cz, LAGO_A_MIN, LAGO_A_MAX) || Dentro(cx, cz, LAGO_B_MIN, LAGO_B_MAX)) return TipoCelula.Agua;
            return TipoCelula.Chao;
        }

        private static bool Dentro(int cx, int cz, Vector2Int a, Vector2Int b) => cx >= a.x && cx <= b.x && cz >= a.y && cz <= b.y;

        private static TerrenoReativo Novo(int seed = 7) => new TerrenoReativo(new FakeRelevo(), LADO, seed, Tipo);

        private static Vector3 Pos(TerrenoReativo t, int cx, int cz) => t.Centro(t.Idx(cx, cz));

        private static int Contar(TerrenoReativo t, EstadoCelula s)
        {
            int n = 0;
            for (int i = 0; i < N * N; i++) if (t.Estado(i) == s) n++;
            return n;
        }

        private static HashSet<int> Celulas(TerrenoReativo t, EstadoCelula s)
        {
            var c = new HashSet<int>();
            for (int i = 0; i < N * N; i++) if (t.Estado(i) == s) c.Add(i);
            return c;
        }

        [SetUp]
        public void SetUp()
        {
            Bus.Reset(); Combat.Reset(); Efeitos.Reset();
            _mudancas = new List<string>();
            Bus.TerrainChanged += (k, p) => _mudancas.Add(k);
        }

        [TearDown]
        public void TearDown() { Efeitos.Reset(); Combat.Reset(); Bus.Reset(); }

        [Test]
        public void Grade_TiposInjetados_ECelulaEm()
        {
            TerrenoReativo t = Novo();
            Assert.AreEqual(N, t.N);
            Assert.AreEqual(TipoCelula.Combustivel, t.Tipo(t.Idx(10, 5)));
            Assert.AreEqual(TipoCelula.Agua, t.Tipo(t.Idx(4, 14)));
            Assert.AreEqual(TipoCelula.Chao, t.Tipo(t.Idx(8, 14)), "terra entre os lagos");
            Assert.AreEqual(t.Idx(10, 5), t.CelulaEm(Pos(t, 10, 5)), "centro volta na propria celula");
            Assert.AreEqual(-1, t.CelulaEm(new Vector3(999f, 0f, 999f)), "fora do mapa = -1");
            Assert.AreEqual(0f, t.DpsEm(new Vector3(999f, 0f, 999f)), "fora do mapa nao machuca");
        }

        [Test]
        public void Bus_TerrainHitAcende_EDesligarSolta()
        {
            TerrenoReativo t = Novo();
            Bus.EmitTerrainHit(Elemento.Fogo, Pos(t, 10, 5), false);
            Assert.AreEqual(EstadoCelula.Queimando, t.Estado(t.Idx(10, 5)), "Bus.TerrainHit acende via sinal (fiacao real)");
            Assert.Contains("burn", _mudancas);
            t.Desligar();
            Bus.EmitTerrainHit(Elemento.Fogo, Pos(t, 12, 5), false);
            Assert.AreEqual(EstadoCelula.Normal, t.Estado(t.Idx(12, 5)), "desligado nao reage");
        }

        [Test]
        public void Fogo_PropagaNoMaximoAteOrcamento_E12Seeds()
        {
            int orc = Balance.Terrain.FuelBudget;
            for (int seed = 1; seed <= 12; seed++)
            {
                TerrenoReativo t = Novo(seed);
                t.Reagir(Elemento.Fogo, Pos(t, 10, 5), true);
                int sementes = t.Queimando;
                Assert.AreEqual(9, sementes, "fogo forte planta 3x3 sementes de graca");
                for (int i = 0; i < 64; i++) t.Simular(0f);   // relogio parado: pior caso, nada se apaga sozinho
                Assert.LessOrEqual(t.TotalEspalhado, orc, "propagacao <= FuelBudget SEMPRE (seed " + seed + ")");
                Assert.LessOrEqual(t.Queimando, sementes + orc, "acesas <= sementes + orcamento (seed " + seed + ")");
                t.Desligar();
            }
        }

        [Test]
        public void Fogo_UmaRolagemPorAresta_MesmaSeedMesmaMancha_ERelogioNaoReRola()
        {
            TerrenoReativo a = Novo(907), b = Novo(907);
            a.Reagir(Elemento.Fogo, Pos(a, 10, 5), true);
            b.Reagir(Elemento.Fogo, Pos(b, 10, 5), true);
            for (int i = 0; i < 64; i++) { a.Simular(0f); b.Simular(0f); }
            Assert.IsTrue(Celulas(a, EstadoCelula.Queimando).SetEquals(Celulas(b, EstadoCelula.Queimando)), "mesma seed = a MESMA mancha");
            Assert.AreEqual(a.Rolagens, b.Rolagens);
            Assert.Greater(a.Rolagens, 0);
            int rolagens = a.Rolagens, acesas = a.Queimando;
            Assert.LessOrEqual(rolagens, 4 * acesas, "no maximo 4 arestas por celula acesa");
            for (int i = 0; i < 200; i++) a.Simular(0f);
            Assert.AreEqual(rolagens, a.Rolagens, "o relogio NAO re-rola: uma rolagem por aresta e nunca mais (chance por tique carbonizou 380/380)");
            Assert.AreEqual(acesas, a.Queimando);
            // A PROVA DE "UMA POR ARESTA": com a seed 6 a semente fraca rola as 4 arestas, PERDE as 4 e o fogo MORRE
            // na semente com o orcamento intacto. Chance por tique re-rolaria ate' sentenciar a floresta inteira.
            TerrenoReativo morre = Novo(6);
            morre.Reagir(Elemento.Fogo, Pos(morre, 10, 5), false);
            for (int i = 0; i < 64; i++) morre.Simular(0f);
            Assert.AreEqual(4, morre.Rolagens, "a semente rolou as 4 arestas UMA vez");
            Assert.AreEqual(0, morre.TotalEspalhado, "perdeu as 4: nada espalhou");
            Assert.AreEqual(1, morre.Queimando, "o fogo pode MORRER antes de gastar o orcamento (a frente nao e' sentenca)");
            morre.Desligar();
            TerrenoReativo c = Novo(908);
            c.Reagir(Elemento.Fogo, Pos(c, 10, 5), true);
            for (int i = 0; i < 64; i++) c.Simular(0f);
            Assert.IsFalse(Celulas(a, EstadoCelula.Queimando).SetEquals(Celulas(c, EstadoCelula.Queimando)), "seed diferente = mancha diferente");
            a.Desligar(); b.Desligar(); c.Desligar();
        }

        [Test]
        public void Vento_Atica_EPagaDoMesmoOrcamento_AteExatamenteFuelBudget()
        {
            TerrenoReativo t = Novo();
            Vector3 p = Pos(t, 10, 5);
            int orc = Balance.Terrain.FuelBudget;
            t.Reagir(Elemento.Fogo, p, false);
            Assert.AreEqual(1, t.Queimando, "1 semente no coracao da floresta");
            t.Reagir(Elemento.Vento, p, false);
            Assert.Greater(t.Queimando, 1, "vento ESPALHA o fogo na hora (sem rolagem)");
            for (int k = 0; k < 20; k++) { t.Reagir(Elemento.Vento, p, true); t.Simular(0f); t.Simular(0f); }
            Assert.AreEqual(orc, t.TotalEspalhado, "vento PAGA do FuelBudget: a frente para EXATAMENTE no orcamento");
            Assert.AreEqual(1 + orc, t.Queimando, "vento nao e' carbonizador infinito");
            t.Reagir(Elemento.Vento, p, true);
            Assert.AreEqual(1 + orc, t.Queimando, "orcamento zerado: nem o vento espalha");
            t.Desligar();
        }

        [Test]
        public void Agua_Apaga_SemCarvao_EFrenteFecha()
        {
            TerrenoReativo t = Novo();
            Vector3 p = Pos(t, 10, 5);
            t.Reagir(Elemento.Fogo, p, false);
            Assert.AreEqual(EstadoCelula.Queimando, t.EstadoEm(p), "fogo fraco acende a celula do impacto");
            _mudancas.Clear();
            t.Reagir(Elemento.Agua, p, false);
            Assert.AreEqual(EstadoCelula.Normal, t.EstadoEm(p), "agua APAGA");
            Assert.AreEqual(0, Contar(t, EstadoCelula.Carvao), "apagar != queimar ate' o fim: nada carboniza");
            Assert.AreEqual(0, t.Queimando);
            Assert.Contains("clear", _mudancas);
            t.Simular(0f);
            Assert.AreEqual(0, t.Rolagens, "celula apagada nao rola aresta (frente fechou)");
            t.Desligar();
        }

        [Test]
        public void Carvao_AposBurnDuration_ArvoreSome_NaoBarraTiro_EResetDevolve()
        {
            TerrenoReativo t = Novo();
            var queimadas = new List<KeyValuePair<int, bool>>();
            t.AoQueimarArvore = (i, q) => queimadas.Add(new KeyValuePair<int, bool>(i, q));
            Vector3 p = Pos(t, 8, 14);   // terra entre os lagos: a arvore e' que faz a celula virar combustivel
            Assert.AreEqual(TipoCelula.Chao, t.Tipo(t.CelulaEm(p)));
            t.RegistrarArvore(3, p);
            Assert.AreEqual(TipoCelula.Combustivel, t.Tipo(t.CelulaEm(p)), "arvore = combustivel");
            t.Reagir(Elemento.Fogo, p, false);
            Assert.AreEqual(EstadoCelula.Queimando, t.EstadoEm(p), "arvore acende");
            Assert.AreEqual(Balance.Terrain.BurnDps, t.DpsEm(p), "queimando = BurnDps");
            t.Simular(Balance.Terrain.BurnDuration + 0.1f);
            Assert.AreEqual(EstadoCelula.Carvao, t.EstadoEm(p), "apos BurnDuration vira CARVAO");
            Assert.Contains("ash", _mudancas);
            Assert.AreEqual(1, queimadas.Count); Assert.AreEqual(3, queimadas[0].Key); Assert.IsTrue(queimadas[0].Value, "a ilha marca a copa como consumida");
            Assert.IsFalse(t.BloqueiaTiro(p), "carvao NAO barra tiro: a cobertura sumiu de verdade");
            Assert.IsTrue(t.Caminhavel(p));
            Assert.AreEqual(0f, t.DpsEm(p), "carvao nao machuca");
            t.Simular(100f);
            Assert.AreEqual(EstadoCelula.Carvao, t.EstadoEm(p), "carvao e' permanente na partida");
            t.Reagir(Elemento.Fogo, p, false);
            Assert.AreEqual(EstadoCelula.Carvao, t.EstadoEm(p), "carvao nao reacende");
            t.Reset();
            Assert.AreEqual(EstadoCelula.Normal, t.EstadoEm(p), "restart limpa o carvao");
            Assert.AreEqual(2, queimadas.Count); Assert.IsFalse(queimadas[1].Value, "restart devolve a floresta");
            Assert.AreEqual(0, t.Visiveis.Count);
            t.Desligar();
        }

        [Test]
        public void Agua_CongelaOLago_ViraCaminhavel_CortaConducao_EDerrete()
        {
            TerrenoReativo t = Novo();
            Vector3 p = Pos(t, 4, 14);
            Assert.IsFalse(t.Caminhavel(p), "agua liquida nao se anda");
            t.Reagir(Elemento.Agua, p, true);
            int gelo = Contar(t, EstadoCelula.Congelado);
            Assert.Greater(gelo, 0, "agua no lago CONGELA a superficie");
            Assert.IsTrue(t.Caminhavel(p), "gelo e' ROTA");
            Assert.AreEqual(0f, t.DpsEm(p), "gelo nao machuca");
            Assert.Contains("ice", _mudancas);
            Assert.AreEqual(gelo, t.Visiveis.Count, "cada celula congelada esta' na lista da cena");
            Assert.AreEqual(EstadoCelula.Congelado, t.Visiveis[0].Estado);
            t.Reagir(Elemento.Raio, p, false);
            Assert.AreEqual(0, Contar(t, EstadoCelula.Eletrificado), "gelo ISOLA: raio no gelo nao conduz");
            t.Simular(Balance.Terrain.FreezeDuration + 0.5f);
            Assert.AreEqual(0, Contar(t, EstadoCelula.Congelado), "derreteu");
            Assert.IsFalse(t.Caminhavel(p), "a rota some LIMPA (quem estava em cima cai)");
            t.Reagir(Elemento.Agua, p, false);
            t.Reagir(Elemento.Fogo, p, false);
            Assert.AreEqual(EstadoCelula.Normal, t.EstadoEm(p), "fogo DERRETE o gelo (contra-jogada)");
            t.Desligar();
        }

        [Test]
        public void Raio_EletrificaAAguaConectadaInteira_ENaoASeparada()
        {
            TerrenoReativo t = Novo();
            Vector3 p = Pos(t, 4, 14);
            t.Reagir(Elemento.Raio, p, false);
            HashSet<int> zap = Celulas(t, EstadoCelula.Eletrificado);
            Assert.AreEqual(25, zap.Count, "o lago A INTEIRO eletrifica (flood 4-vizinhos)");
            for (int cx = LAGO_A_MIN.x; cx <= LAGO_A_MAX.x; cx++)
                for (int cz = LAGO_A_MIN.y; cz <= LAGO_A_MAX.y; cz++)
                    Assert.IsTrue(zap.Contains(t.Idx(cx, cz)));
            Assert.AreEqual(EstadoCelula.Normal, t.Estado(t.Idx(12, 14)), "a conducao NAO atravessa terra: o lago B nao eletrifica");
            Assert.AreEqual(Balance.Terrain.ElectrifyDps, t.DpsEm(Pos(t, 6, 16)), "DpsEm = ElectrifyDps na ponta mais longe");
            Assert.Contains("electric", _mudancas);
            t.Simular(Balance.Terrain.ElectrifyDuration + 0.5f);
            Assert.AreEqual(0f, t.DpsEm(p), "eletrificacao expira");
            t.Reagir(Elemento.Raio, Pos(t, 8, 14), false);
            Assert.AreEqual(0, Contar(t, EstadoCelula.Eletrificado), "raio na terra entre os lagos (a 2 celulas) nao acha agua no splash");
            t.Reagir(Elemento.Raio, Pos(t, 7, 14), false);
            Assert.AreEqual(25, Contar(t, EstadoCelula.Eletrificado), "raio na margem (a 1 celula) conduz para o lago inteiro");
            t.Desligar();
        }

        [Test]
        public void Muro_Deterministico907_BloqueiaTiro_CaiPorHp_EPorDuracao()
        {
            TerrenoReativo t = Novo();
            Vector3 p = Pos(t, 10, 18);
            int c = t.Idx(10, 18);
            t.Reagir(Elemento.Terra, p, true);
            Assert.AreEqual(9, Contar(t, EstadoCelula.Muro), "terra FORTE ergue muro 3x3");
            Assert.IsTrue(t.BloqueiaTiro(p), "muro e' cobertura");
            Assert.IsFalse(t.Caminhavel(p));
            Assert.AreEqual(0f, t.DpsEm(p), "muro nao machuca");
            Assert.Contains("wall", _mudancas);
            CelulaVisivel v = null;
            foreach (CelulaVisivel x in t.Visiveis) if (x.Idx == c) v = x;
            Assert.IsNotNull(v); Assert.AreEqual(Balance.Terrain.WallHp, v.MuroHp, 0.001f);
            Assert.AreEqual(TerrenoReativo.SementeDoMuro(c), v.Semente, "a pedra nasce da seed 907 + celula");
            TerrenoReativo outro = Novo(12345);
            outro.Reagir(Elemento.Terra, p, true);
            foreach (CelulaVisivel x in outro.Visiveis) if (x.Idx == c) Assert.AreEqual(v.Semente, x.Semente, "2 montagens = o MESMO muro, seja qual for a seed da partida");
            outro.Desligar();
            // Cai por hp: dano x Estrutura do elemento (terra 2.0 racha mais rapido que fogo 1.0)
            Balance.PerfilElemento terra = Balance.Perfil(Elemento.Terra);
            int esperado = Mathf.CeilToInt(Balance.Terrain.WallHp / (terra.Dmg * terra.Estrutura));
            int golpes = 0;
            while (t.Estado(c) == EstadoCelula.Muro && golpes < esperado + 3) { t.Reagir(Elemento.Terra, p, false); golpes++; }
            Assert.AreEqual(esperado, golpes, "muro caiu no golpe certo (hp / (dano x estrutura))");
            Assert.IsFalse(t.BloqueiaTiro(p), "projeteis DERRUBAM o muro");
            Assert.AreEqual(8, Contar(t, EstadoCelula.Muro), "so' a celula atingida caiu");
            Balance.PerfilElemento fogo = Balance.Perfil(Elemento.Fogo);
            Assert.Greater(Mathf.CeilToInt(Balance.Terrain.WallHp / (fogo.Dmg * fogo.Estrutura)), esperado, "fogo precisa de mais golpes que terra");
            t.Reset();
            t.Reagir(Elemento.Terra, p, false);
            Assert.AreEqual(1, Contar(t, EstadoCelula.Muro), "terra fraca ergue muro so' na celula do impacto");
            t.Simular(Balance.Terrain.WallDuration + 0.5f);
            Assert.AreEqual(0, Contar(t, EstadoCelula.Muro), "muro expira sozinho");
            Assert.AreEqual(0, t.Visiveis.Count);
            t.Desligar();
        }

        [Test]
        public void Muro_NaoNasceEmCelulaOcupada_NemNaAgua()
        {
            TerrenoReativo t = Novo();
            Vector3 p = Pos(t, 10, 18);
            var corpo = new FakeEntidade("corpo", p);
            t.Tick(0.01f, new List<IEntidade> { corpo });   // quem esta' de pe' e' o que o ultimo Tick viu
            t.Reagir(Elemento.Terra, p, true);
            Assert.AreNotEqual(EstadoCelula.Muro, t.EstadoEm(p), "muro NAO nasce na celula ocupada (anti-griefing)");
            Assert.AreEqual(8, Contar(t, EstadoCelula.Muro), "a protecao nao vira nerf: o resto do disco sobe");
            t.Reagir(Elemento.Terra, Pos(t, 4, 14), true);
            Assert.AreEqual(8, Contar(t, EstadoCelula.Muro), "na agua nao cabe muro");
            t.Desligar();
        }

        [Test]
        public void Lama_Fator055_VentoNaoAfeta_FogoSeca_EExpira()
        {
            TerrenoReativo t = Novo();
            Vector3 p = Pos(t, 8, 18);
            Assert.AreEqual(1f, t.FatorTerreno(p), 0.001f);
            t.Reagir(Elemento.Agua, p, false);
            Assert.AreEqual(EstadoCelula.Lama, t.EstadoEm(p), "agua no chao de terra = LAMACAL");
            Assert.AreEqual(Balance.Terrain.MudSlow, t.FatorTerreno(p), 0.001f, "lama = 0.55 no produto de velocidade");
            Assert.AreEqual(0f, t.DpsEm(p), "lama nao machuca");
            Assert.IsTrue(t.Caminhavel(p));
            Assert.Contains("mud", _mudancas);
            t.Reagir(Elemento.Vento, p, false);
            Assert.AreEqual(EstadoCelula.Lama, t.EstadoEm(p), "vento nao afeta a lama");
            t.Reagir(Elemento.Fogo, p, false);
            Assert.AreEqual(EstadoCelula.Normal, t.EstadoEm(p), "fogo SECA a lama");
            Assert.AreEqual(1f, t.FatorTerreno(p), 0.001f);
            t.Reagir(Elemento.Agua, p, false);
            t.Simular(Balance.Terrain.MudDuration + 0.5f);
            Assert.AreEqual(EstadoCelula.Normal, t.EstadoEm(p), "a lama seca sozinha");
            t.Reagir(Elemento.Agua, Pos(t, 10, 5), false);
            Assert.AreNotEqual(EstadoCelula.Lama, t.EstadoEm(Pos(t, 10, 5)), "floresta nao vira lama");
            t.Desligar();
        }

        [Test]
        public void DoTAmbiental_PeloCombat_FonteNull_DiretoNaVida_NoBaldeDeDotTick()
        {
            TerrenoReativo t = Novo();
            Vector3 fogo = Pos(t, 10, 5), agua = Pos(t, 4, 14);
            t.Reagir(Elemento.Fogo, fogo, false);
            t.Reagir(Elemento.Raio, agua, false);
            var noFogo = new FakeEntidade("a", fogo);
            var naAgua = new FakeEntidade("b", agua);
            var seco = new FakeEntidade("c", Pos(t, 8, 18));
            var danos = new List<object[]>();
            Bus.DamageApplied += (alvo, quanto, el, fonte, esc) => danos.Add(new object[] { alvo, quanto, el, fonte, esc });
            var alvos = new List<IEntidade> { noFogo, naAgua, seco };
            t.Tick(0.1f, alvos);
            Assert.AreEqual(0, danos.Count, "nada de dano por frame: o balde e' Balance.Dot.Tick");
            t.Tick(0.15f, alvos);
            Assert.AreEqual(2, danos.Count, "um dano por alvo em perigo, no fechamento do balde");
            foreach (object[] d in danos)
            {
                Assert.IsNull(d[3], "fonte NULL = ambiente (nao credita escudo a ninguem)");
                Assert.IsFalse((bool)d[4], "DoT vai DIRETO na vida (o escudo protege contra magia, nao contra estar em chamas)");
            }
            Assert.AreEqual(100f - Balance.Terrain.BurnDps * 0.25f, noFogo.Vital.Hp, 0.01f, "fogo cobra BurnDps x 0.25s");
            Assert.AreEqual(100f - Balance.Terrain.ElectrifyDps * 0.25f, naAgua.Vital.Hp, 0.01f, "agua eletrificada cobra ElectrifyDps x 0.25s");
            Assert.AreEqual(noFogo.Vital.EscudoMax, noFogo.Vital.Escudo, 0.001f, "escudo intacto");
            Assert.AreEqual(100f, seco.Vital.Hp, "quem esta' em chao normal nao paga nada");
            t.Desligar();
        }

        [Test]
        public void TerrainChanged_SoNaBorda()
        {
            TerrenoReativo t = Novo();
            Vector3 agua = Pos(t, 4, 14);
            t.Reagir(Elemento.Raio, agua, false);
            int eletricos = _mudancas.FindAll(m => m == "electric").Count;
            Assert.AreEqual(25, eletricos, "uma borda por celula que MUDOU");
            t.Reagir(Elemento.Raio, agua, false);
            Assert.AreEqual(eletricos, _mudancas.FindAll(m => m == "electric").Count, "refrescar o mesmo estado NAO repete o sinal");
            t.Desligar();
        }
    }
}
