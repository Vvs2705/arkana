using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>HUD pura: rotulo da arma some em 2,5 s; maos nuas escondem o carrossel; kill feed com nome; faixa do abate que
    /// carimba e some; TREINO no relogio;
    /// numeros com merge; prioridade da faixa; cooldown interpolado; pausa DENTRO da tela; alvos &gt;= 48dp; daltonismo;
    /// minimapa e bussola sem cobrir nada, mundo -&gt; mapa, bussola que gira com a camera e a ilha pintada.</summary>
    public class UiHudLogicaTests
    {
        HudLogica _h;

        [SetUp]
        public void SetUp() { _h = new HudLogica(0.35f, 0.60f); _h.MaosNuas(); }

        [Test]
        public void RotuloDaArmaSomeEm2_5s()
        {
            _h.Equipar("Luva Comum · ÁGUA", new[] { Elemento.Agua });
            Assert.IsTrue(_h.ArmaRotuloVisivel);
            Assert.AreEqual(1f, _h.ArmaRotuloAlfa, 1e-4f);
            _h.Tick(2.4f);
            Assert.AreEqual(1f, _h.ArmaRotuloAlfa, 1e-4f, "ate' 2,5 s le' inteiro");
            _h.Tick(0.35f);
            Assert.Less(_h.ArmaRotuloAlfa, 1f);
            Assert.Greater(_h.ArmaRotuloAlfa, 0f);
            _h.Tick(0.5f);
            Assert.IsFalse(_h.ArmaRotuloVisivel, "depois do fade o rotulo SOME");
            Assert.AreEqual(HudLogica.ArmaRotuloS, 2.5f, 1e-4f);
        }

        [Test]
        public void MaosNuasEscondemCarrosselEDesarmam()
        {
            Assert.IsFalse(_h.CarrosselVisivel);
            Assert.IsFalse(_h.Armado);
            _h.Equipar("Luva · ÁGUA", new[] { Elemento.Agua });
            Assert.IsTrue(_h.Armado, "equipar ACENDE o ataque");
            Assert.IsFalse(_h.CarrosselVisivel, "luva de elemento travado segue sem carrossel");
            _h.Equipar("Cajado", new Elemento[0]);
            Assert.IsTrue(_h.CarrosselVisivel, "arma sem elemento devolve o carrossel");
            _h.MaosNuas();
            Assert.IsFalse(_h.CarrosselVisivel);
            Assert.AreEqual("", _h.ArmaRotulo);
        }

        [Test]
        public void KillFeedUsaONomeDoMago()
        {
            _h.Abater("Pyra");
            Assert.AreEqual(1, _h.KillFeed.Count);
            Assert.AreEqual("Pyra", _h.KillFeed[0].Nome);
            _h.Tick(HudLogica.KillFeedS + 0.01f);
            Assert.AreEqual(0, _h.KillFeed.Count, "o feed expira");
        }

        [Test]
        public void TelaDeFimContaAbatesEColocacao()
        {
            _h.Abater("Pyra"); _h.Abater("Veu");
            _h.Tick(HudLogica.KillFeedS + 0.01f);
            Assert.AreEqual(2, _h.Abates, "o feed esquece; a tela de fim nao");
            _h.MaosNuas();
            Assert.AreEqual(0, _h.Abates, "partida nova zera");
            Assert.AreEqual(1, HudLogica.Colocacao(true, 5), "vencer e' #1");
            Assert.AreEqual(6, HudLogica.Colocacao(false, 5), "cair com 5 de pe' = #6");
            Assert.AreEqual(1, HudLogica.Colocacao(false, -1), "contagem suja nao vira #0");
        }

        [Test]
        public void RelogioMostraTreinoNoTreino()
        {
            Assert.AreEqual("TREINO", HudLogica.TextoRelogio(125f, true, "TREINO"));
            Assert.AreEqual("2:05", HudLogica.TextoRelogio(125f, false, "TREINO"));
            Assert.AreEqual("0:00", HudLogica.TextoRelogio(-3f, false, "TREINO"));
        }

        [Test]
        public void NumerosDeDanoSomamDentroDoMerge()
        {
            bool novo;
            var n1 = _h.RegistrarDano(7, 37f, Elemento.Fogo, false, out novo);
            Assert.IsTrue(novo);
            var n2 = _h.RegistrarDano(7, 13f, Elemento.Fogo, false, out novo);
            Assert.IsFalse(novo, "acerto seguido SOMA no mesmo numero");
            Assert.AreSame(n1, n2);
            Assert.AreEqual(50f, n2.Total, 1e-4f);
            _h.Tick(0.4f);
            _h.RegistrarDano(7, 5f, Elemento.Fogo, false, out novo);
            Assert.IsTrue(novo, "passou a janela: numero novo");
            _h.RegistrarDano(8, 5f, Elemento.Fogo, false, out novo);
            Assert.IsTrue(novo, "alvo diferente = numero diferente");
        }

        [Test]
        public void NumeroPulaNoGolpeEAssenta()
        {
            bool novo;
            var n = _h.RegistrarDano(7, 10f, Elemento.Fogo, false, out novo);
            Assert.AreEqual(HudLogica.PuloPico, HudLogica.Pulo(n, _h.Agora), 1e-4f, "nasce no pico");
            _h.Tick(HudLogica.PuloS);
            Assert.AreEqual(1f, HudLogica.Pulo(n, _h.Agora), 1e-4f, "assenta em 1");
            Assert.IsFalse(HudLogica.Grande(n));
            _h.RegistrarDano(7, 20f, Elemento.Fogo, false, out novo);   // dentro do merge: soma 30
            Assert.IsFalse(novo);
            Assert.IsTrue(HudLogica.Grande(n), "a soma passou do limiar: golpe grande");
            Assert.AreEqual(HudLogica.PuloPicoGrande, HudLogica.Pulo(n, _h.Agora), 1e-4f, "o golpe somado pula de novo, e o grande pula mais");
        }

        [Test]
        public void FaixaDoAbateCarimbaLeESome()
        {
            Assert.IsFalse(_h.EliminadoVisivel, "sem abate, sem faixa");
            _h.Abater("Pyra");
            Assert.IsTrue(_h.EliminadoVisivel);
            Assert.AreEqual("Pyra", _h.Eliminado);
            Assert.AreEqual(HudLogica.EliminadoPico, _h.EliminadoEscala, 1e-4f, "nasce no pico (o carimbo)");
            _h.Tick(HudLogica.EliminadoPuloS);
            Assert.AreEqual(1f, _h.EliminadoEscala, 1e-4f, "assenta em 1");
            Assert.AreEqual(1f, _h.EliminadoAlfa, 1e-4f, "no meio le' inteira");
            _h.Tick(HudLogica.EliminadoS - HudLogica.EliminadoPuloS - HudLogica.EliminadoSaiS * 0.5f);
            Assert.Greater(_h.EliminadoAlfa, 0f);
            Assert.Less(_h.EliminadoAlfa, 1f, "esvaece no fim");
            _h.Abater("Tessa");
            Assert.AreEqual("Tessa", _h.Eliminado, "abate seguido reescreve");
            Assert.AreEqual(HudLogica.EliminadoPico, _h.EliminadoEscala, 1e-4f, "e carimba de novo");
            _h.Tick(HudLogica.EliminadoS + 0.01f);
            Assert.IsFalse(_h.EliminadoVisivel, "some sozinha");
            Assert.AreEqual(0f, _h.EliminadoAlfa);
            _h.Abater("Veu");
            _h.MaosNuas();
            Assert.IsFalse(_h.EliminadoVisivel, "partida nova nao herda a faixa");
        }

        [Test]
        public void HitmarkerEsvaece()
        {
            _h.Acertei();
            Assert.Greater(_h.Hitmarker, 0f);
            _h.Tick(HudLogica.HitmarkerS + 0.01f);
            Assert.AreEqual(0f, _h.Hitmarker);
        }

        // ---------- avisos ----------
        AvisoLogica Aviso() => new AvisoLogica(0.12f, 1.4f, 3, new[] { "braco_livre", "revelado" });

        [Test]
        public void FaixaMostraSoQuemSalvaAVida()
        {
            var a = Aviso();
            a.Avisar(AvisoLogica.P_BAU, "BAU", Color.white);
            a.Avisar(AvisoLogica.P_ZONA, "PAREDE", Color.white);
            a.Avisar(AvisoLogica.P_TELEGRAFO, "SUPREMA", Color.white);
            a.Avisar(AvisoLogica.P_ZONA_FORA, "VOLTE PARA A ZONA", Color.white);
            Assert.AreEqual("VOLTE PARA A ZONA", a.FaixaAtiva());
            a.Limpar(AvisoLogica.P_ZONA_FORA);
            Assert.AreEqual("SUPREMA", a.FaixaAtiva());
            a.Derrubar(true);
            Assert.AreEqual("", a.FaixaAtiva(), "no chao a faixa CALA");
            a.Derrubar(false);
            Assert.AreEqual("SUPREMA", a.FaixaAtiva());
        }

        [Test]
        public void FaixaComDuracaoExpira()
        {
            var a = Aviso();
            a.Avisar(AvisoLogica.P_BAU, "X", Color.white, 1f);
            a.Tick(0.5f);
            Assert.AreEqual("X", a.FaixaAtiva());
            a.Tick(0.6f);
            Assert.AreEqual("", a.FaixaAtiva());
        }

        [Test]
        public void ContagemReescreveSoQuandoOSegundoMuda()
        {
            var a = Aviso();
            a.Contar(AvisoLogica.P_ZONA, 12f, "AVANÇA EM {0}s", Color.white);
            Assert.AreEqual("AVANÇA EM 12s", a.FaixaAtiva());
            a.Tick(0.5f);
            Assert.AreEqual("AVANÇA EM 12s", a.FaixaAtiva());
            a.Tick(0.6f);
            Assert.AreEqual("AVANÇA EM 11s", a.FaixaAtiva());
            a.Tick(12f);
            Assert.AreEqual("", a.FaixaAtiva(), "contagem zerada some da faixa");
        }

        [Test]
        public void EstadoDesconhecidoEIgnorado()
        {
            var a = Aviso();
            a.Estado("braco_livre", true);
            Assert.Contains("braco_livre", a.Badges);
            a.Estado("um_estado_que_ninguem_desenhou", true);
            Assert.AreEqual(1, a.Badges.Count);
            a.Estado("braco_livre", false);
            Assert.AreEqual(0, a.Badges.Count);
        }

        [Test]
        public void VinhetaTemIntervaloMinimoEArcosTemTeto()
        {
            var a = Aviso();
            Assert.IsTrue(a.Pulsar(Color.red, 0.5f));
            Assert.IsFalse(a.Pulsar(Color.red, 1f), "tiques colados nao repicam");
            Assert.AreEqual(0.5f, a.Vinheta, 1e-4f);
            a.Tick(0.13f);
            Assert.IsTrue(a.Pulsar(Color.red, 1f));
            for (int i = 0; i < 6; i++) a.MarcarArco(new Vector3(i, 0, 0), Color.red);
            Assert.AreEqual(3, a.Arcos.Count, "o 4o substitui o mais antigo");
            Assert.AreEqual(3f, a.Arcos[0].Pos.x, 1e-4f);
            a.Tick(1.5f);
            Assert.AreEqual(0, a.Arcos.Count);
        }

        [Test]
        public void CancelarCanalizacaoEEstadoDePrimeiraClasse()
        {
            var a = Aviso();
            a.Cancelar();
            Assert.AreEqual(0f, a.Cancelado, "nao havia canalizacao: nao existe cancelamento do nada");
            a.Canalizar(0.45f);
            Assert.AreEqual(0.45f, a.Canal, 1e-4f);
            a.Cancelar();
            Assert.Less(a.Canal, 0f);
            Assert.Greater(a.Cancelado, 0f, "interromper NAO some caladinho: fica a marca");
            a.Tick(AvisoLogica.CANCELADO_S + 0.01f);
            Assert.AreEqual(0f, a.Cancelado, "a marca apaga sozinha");
            a.Canalizar(0.9f);
            a.CanalizarFim();
            Assert.Less(a.Canal, 0f);
            Assert.AreEqual(0f, a.Cancelado, "terminar BEM fecha o anel sem marca");
        }

        // ---------- botao de acao ----------
        [Test]
        public void CooldownInterpolaEntreBordas()
        {
            var b = new BotaoAcaoLogica();
            b.Cooldown(8f, 8f);
            Assert.AreEqual(1f, b.Frac, 1e-4f);
            Assert.AreEqual("8", b.TextoCentral());
            b.Tick(4f);
            Assert.AreEqual(0.5f, b.Frac, 1e-4f, "entre bordas o 0..1 e' interpolado");
            Assert.AreEqual("4", b.TextoCentral());
            b.MostraCarga = true;
            Assert.AreEqual("50%", b.TextoCentral(), "a suprema mostra a PORCENTAGEM que ja' encheu");
            b.Tick(5f);
            Assert.AreEqual(0f, b.Frac);
            Assert.AreEqual("", b.TextoCentral());
        }

        [Test]
        public void BotaoApagadoNaoPede()
        {
            var b = new BotaoAcaoLogica();
            b.Ativo = false;
            Assert.IsFalse(b.Tocar(), "17 dos 20 magos sem kit: o toque nao vaza");
            b.Ativo = true;
            Assert.IsTrue(b.Tocar());
            b.Cooldown(3f, 3f);
            Assert.IsFalse(b.Tocar(), "em cooldown nao pede");
        }

        // ---------- layout e area segura ----------
        [Test]
        public void PausaFicaDentroDaTelaEAlvosTem48dp()
        {
            Vector2 tela = new Vector2(1600, 720);
            var m = new Margens(80, 40, 90, 60);
            float px = 1f;
            var l = HudLayout.Calcular(tela, m, px);
            Assert.GreaterOrEqual(l.Pausa.yMin, 0f);
            Assert.LessOrEqual(l.Pausa.yMax, tela.y - m.Topo, "PAUSA de fato NA TELA, abaixo do relogio");
            Assert.LessOrEqual(l.Pausa.xMax, tela.x - m.Dir);
            Assert.Less(l.Pausa.yMax, l.Topo.yMin + 0.01f, "abaixo do relogio");
            foreach (var r in new[] { l.Joystick, l.Disparo, l.Esquiva, l.Tatica, l.Suprema, l.Salto, l.Pegar })
                Assert.GreaterOrEqual(Mathf.Min(r.width, r.height) / px, 48f, "alvo de toque >= 48dp");
            Assert.GreaterOrEqual(l.Carrossel.width / Elementos.Todos.Length / px, 48f, "slot do carrossel >= 48dp");
            Assert.GreaterOrEqual(l.Joystick.xMin, m.Esq + 8f, "joystick recua do notch esquerdo");
            Assert.LessOrEqual(l.Disparo.xMax, tela.x - m.Dir - 8f, "Fogo recua do notch direito");
            foreach (var r in new[] { l.Joystick, l.Disparo, l.Esquiva, l.Tatica, l.Suprema, l.Pegar })
                Assert.GreaterOrEqual(r.yMin, m.Baixo + 8f, "recua da barra de gestos");
            Assert.LessOrEqual(l.Suprema.xMax, l.Tatica.xMin); Assert.LessOrEqual(l.Tatica.xMax, l.Esquiva.xMin); Assert.LessOrEqual(l.Esquiva.xMax, l.Disparo.xMin);
            Assert.GreaterOrEqual(l.Carrossel.yMin, l.Disparo.yMax, "carrossel ACIMA do Fogo");
            Assert.IsFalse(l.Pegar.Overlaps(l.Suprema), "PEGAR nao briga com a fileira");
            var l2 = HudLayout.Calcular(tela, m, px);
            Assert.AreEqual(l.Suprema.xMin, l2.Suprema.xMin, 1e-4f, "layout repetido e' estavel");
        }

        [Test]
        public void MinimapaEBussolaNaoCobremNada()
        {
            // a tela do teste (px = 1, com entalhe) e a do Poco F4 deitado (2400x1080 a 395 ppi = 437dp: o caso apertado)
            var telas = new[] { new Vector2(1600, 720), new Vector2(2400, 1080) };
            var margens = new[] { new Margens(80, 40, 90, 60), new Margens(0, 0, 0, 0) };
            var pxs = new[] { 1f, 395f / 160f };
            for (int i = 0; i < telas.Length; i++)
            {
                Vector2 tela = telas[i]; Margens m = margens[i]; float px = pxs[i];
                var l = HudLayout.Calcular(tela, m, px);
                Rect mapa = l.Minimapa;
                Assert.LessOrEqual(mapa.yMax, l.Topo.yMin, "minimapa ABAIXO do relogio");
                Assert.LessOrEqual(mapa.xMax, tela.x - m.Dir, "minimapa dentro da area segura");
                Assert.AreEqual(mapa.width, mapa.height, 1e-3f, "quadrado");
                Assert.GreaterOrEqual(mapa.width / px, HudLayout.MinimapaMinDp - 1e-3f, "e' alvo de toque (abre o mapa grande)");
                foreach (var r in new[] { l.Pausa, l.Topo, l.Altimetro, l.ArmaRotulo, l.Salto, l.Carrossel, l.Disparo })
                    Assert.IsFalse(mapa.Overlaps(r), "o minimapa nao cobre nada da coluna direita (" + tela + ")");
                Assert.Less(l.Pausa.yMax, l.Topo.yMin + 0.01f, "a pausa segue abaixo do relogio");
                Assert.GreaterOrEqual(l.Pausa.xMin, m.Esq);
                Assert.GreaterOrEqual(l.Bussola.yMin, HudAviso.RectFaixa(tela, m, px).yMax, "a bussola mora ACIMA da faixa de aviso");
                Assert.LessOrEqual(l.Bussola.yMax, tela.y - m.Topo);
                Assert.AreEqual(tela.x * 0.5f, l.Bussola.center.x, 1e-3f, "bussola centrada na mira");
                Assert.IsFalse(l.Bussola.Overlaps(l.Barras) || l.Bussola.Overlaps(l.Topo));
                Assert.GreaterOrEqual(l.MapaGrande.xMin, m.Esq); Assert.LessOrEqual(l.MapaGrande.xMax, tela.x - m.Dir);
                Assert.GreaterOrEqual(l.MapaGrande.yMin, m.Baixo); Assert.LessOrEqual(l.MapaGrande.yMax, tela.y - m.Topo, "mapa grande na area segura");
            }
        }

        // ---------- mapa e bussola ----------
        [Test]
        public void MapaLevaMundoAJanelaEBussolaGiraComACamera()
        {
            // mundo -> janela: o jogador no meio, norte (+z) EM CIMA, leste (+x) a direita
            Vector3 eu = new Vector3(40f, 3f, -20f);
            float escala = 240f / MapaLogica.VistaLocalM;   // janela de 240 px: 1 px por metro
            Assert.AreEqual(Vector2.zero, MapaLogica.NaJanela(eu, eu, escala), "o jogador no meio da janela");
            Vector2 zona = MapaLogica.NaJanela(new Vector3(100f, 0f, 60f), eu, escala);
            Assert.AreEqual(60f, zona.x, 1e-3f, "60 m a leste = 60 px a direita");
            Assert.AreEqual(80f, zona.y, 1e-3f, "80 m ao norte = 80 px acima");
            // a textura: origem da ilha no meio; a janela uv do minimapa tem vista/extensao de lado
            float ext = 660f;
            Assert.AreEqual(new Vector2(0.5f, 0.5f), MapaLogica.Uv(Vector3.zero, ext));
            Assert.AreEqual(1f, MapaLogica.Uv(new Vector3(330f, 0f, 330f), ext).y, 1e-4f, "o canto norte da textura");
            Rect uv = MapaLogica.UvDaJanela(eu, MapaLogica.VistaLocalM, ext);
            Assert.AreEqual(MapaLogica.VistaLocalM / ext, uv.width, 1e-4f);
            Assert.AreEqual(MapaLogica.Uv(eu, ext).x, uv.center.x, 1e-4f, "a janela segue o jogador");
            // zoom: ilha inteira (castelo) = centro da ilha; perto (no chao) = o jogador
            Assert.AreEqual(0f, MapaLogica.CentroDaJanela(eu, ext, ext).magnitude, 1e-4f);
            Assert.AreEqual(eu.x, MapaLogica.CentroDaJanela(eu, MapaLogica.VistaLocalM, ext).x, 1e-3f);
            // rumo e bussola: olhando o norte, o L esta' na borda direita; olhando o leste, no meio; o N cruza o zero
            Assert.AreEqual(0f, MapaLogica.Rumo(Vector3.zero, new Vector3(0f, 0f, 10f)), 1e-3f);
            Assert.AreEqual(90f, MapaLogica.Rumo(Vector3.zero, new Vector3(10f, 0f, 0f)), 1e-3f);
            Assert.AreEqual(1f, MapaLogica.NaBussola(90f, 0f), 1e-4f);
            Assert.AreEqual(0f, MapaLogica.NaBussola(90f, 90f), 1e-4f);
            Assert.AreEqual(10f / MapaLogica.MeiaBussola, MapaLogica.NaBussola(0f, 350f), 1e-4f, "o N 10 graus a direita, cruzando o zero");
            Assert.AreEqual(1f, MapaLogica.AlfaNaBussola(0f), 1e-4f);
            Assert.AreEqual(0f, MapaLogica.AlfaNaBussola(1f), 1e-4f, "a marca some na borda, nao corta seco");
            // a seta: o sprite aponta para cima; yaw 90 (leste) = seta para a DIREITA na tela
            float giro = MapaLogica.GiroDaSeta(90f) * Mathf.Deg2Rad;
            Assert.AreEqual(1f, -Mathf.Sin(giro), 1e-4f, "yaw 90 = leste = seta para a direita");
            Assert.AreEqual(0f, Mathf.Cos(giro), 1e-4f);
            // o bau 30 m a sudoeste: embaixo e a esquerda no mapa; olhando o norte, a bussola o poe ATRAS (fora da faixa)
            Vector3 bau = eu + new Vector3(-30f, 0f, -30f);
            Vector2 pb = MapaLogica.NaJanela(bau, eu, escala);
            Assert.Less(pb.x, 0f); Assert.Less(pb.y, 0f);
            Assert.AreEqual(225f, Mathf.Repeat(MapaLogica.Rumo(eu, bau), 360f), 1e-3f);
            Assert.Greater(Mathf.Abs(MapaLogica.NaBussola(MapaLogica.Rumo(eu, bau), 0f)), 1f);
        }

        [Test]
        public void MapaDaIlhaPintaMarChaoLagoESombra()
        {
            // o sombreado: plano = 1; encosta virada para o NOROESTE (sobe para leste/sul) acende, a de costas apaga
            Assert.AreEqual(1f, MapaLogica.Sombra(0f, 0f), 1e-4f);
            Assert.Greater(MapaLogica.Sombra(0.8f, -0.8f), 1f, "virada para a luz");
            Assert.Less(MapaLogica.Sombra(-0.8f, 0.8f), 1f, "de costas para a luz");
            var r = new Arkana.World.Relevo();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Color32[] px = MapaLogica.PintarIlha(r, MapaLogica.TexturaLado);
            long ms = sw.ElapsedMilliseconds;
            int n = MapaLogica.TexturaLado;
            float ext = MapaLogica.Extensao(r);
            Color32 Em(float x, float z)
            {
                Vector2 uv = MapaLogica.Uv(new Vector3(x, 0f, z), ext);
                return px[Mathf.FloorToInt(uv.y * n) * n + Mathf.FloorToInt(uv.x * n)];
            }
            Color32 mar = px[0], vale = Em(0f, 0f), lago = Em(r.Lago.x, r.Lago.y), mata = Em(r.Floresta.x, r.Floresta.y);
            Assert.IsTrue(mar.b > mar.g && mar.b > mar.r, "o canto da textura e' mar: azul");
            Assert.IsTrue(vale.g > vale.b && vale.g > vale.r, "o vale e' campina: verde");
            Assert.IsTrue(lago.b > lago.r && lago.b > lago.g, "o lago e' agua");
            Assert.Less(mata.g + mata.r, vale.g + vale.r, "a floresta e' copa ESCURA, nao campina");
            // ponytail: a pintura roda numa Task (Minimapa.Textura); a thread principal so' sobe ~1 MB uma vez. Teto frouxo de
            // regressao: se ele estourar, o mapa demora a aparecer — baixar TexturaLado para 256 corta 4x.
            Assert.Less(ms, 4000, "pintura de " + n + "x" + n + " levou " + ms + " ms");
        }

        [Test]
        public void AreaSeguraViraMargens()
        {
            var m = AreaSegura.Calcular(new Rect(80, 60, 1430, 620), new Vector2(1600, 720));
            Assert.AreEqual(80f, m.Esq); Assert.AreEqual(60f, m.Baixo); Assert.AreEqual(90f, m.Dir); Assert.AreEqual(40f, m.Topo);
            var zero = AreaSegura.Calcular(new Rect(0, 0, 1600, 720), new Vector2(1600, 720));
            Assert.AreEqual(0f, zero.Esq + zero.Topo + zero.Dir + zero.Baixo);
        }

        [Test]
        public void AvisosDesenhadosAMaoRespeitamAreaSegura()
        {
            Vector2 tela = new Vector2(1600, 720); var m = new Margens(80, 40, 90, 60);
            var f = HudAviso.RectFaixa(tela, m, 1f);
            Assert.GreaterOrEqual(f.xMin, m.Esq + 8f); Assert.LessOrEqual(f.yMax, tela.y - m.Topo - 8f); Assert.LessOrEqual(f.xMax, tela.x - m.Dir);
            Assert.LessOrEqual(HudAviso.RectDerrubado(tela, m, 1f).yMax, tela.y - m.Topo);
            Assert.GreaterOrEqual(HudAviso.RectDerrubado(tela, m, 1f).yMin, m.Baixo);
            Assert.GreaterOrEqual(HudAviso.RectCanalizar(tela, m, 1f).yMin, m.Baixo);
        }

        [Test]
        public void CarrosselMapeiaToqueParaSlot()
        {
            Assert.AreEqual(0, CarrosselElementos.SlotDe(5f, 260f, 5));
            Assert.AreEqual(2, CarrosselElementos.SlotDe(130f, 260f, 5));
            Assert.AreEqual(4, CarrosselElementos.SlotDe(999f, 260f, 5));
        }

        [Test]
        public void DaltonismoRemapeiaSemSairDaFaixa()
        {
            Color vermelho = new Color(1f, 0f, 0f);
            Assert.AreEqual(vermelho, FiltroDaltonismo.Corrigir(vermelho, 0), "modo Nenhum e' identidade");
            for (int modo = 1; modo <= 3; modo++)
            {
                Color c = FiltroDaltonismo.Corrigir(vermelho, modo);
                Assert.IsTrue(c.r >= 0f && c.r <= 1f && c.g >= 0f && c.g <= 1f && c.b >= 0f && c.b <= 1f, "modo " + modo + " dentro de [0,1]");
            }
            Color deut = FiltroDaltonismo.Corrigir(vermelho, 2);
            Assert.AreNotEqual(vermelho, deut, "deuteranopia devolve o erro nos canais que sobram");
            Assert.Greater(deut.b, 0f);
        }

        // ------------------------------------------------------------------ 04/10: HUD configuravel (BLOCO G)

        [Test]
        public void Layout_Espelhado_JoystickADireita_FogoAEsquerda_RespeitandoOEntalhe()
        {
            float px = 395f / 160f;   // Poco F4 deitado
            var tela = new Vector2(2400, 1080);
            var m = new Margens { Esq = 80f, Dir = 0f, Topo = 0f, Baixo = 0f };   // entalhe da camera a esquerda
            HudLayout p = HudLayout.Calcular(tela, m, px), e = HudLayout.Calcular(tela, m, px, 1f, true);
            Assert.Less(p.Joystick.center.x, tela.x / 2f);
            Assert.Greater(e.Joystick.center.x, tela.x / 2f, "canhoto: joystick a direita");
            Assert.Less(e.Disparo.center.x, tela.x / 2f, "e o Fogo a esquerda");
            Assert.AreEqual(tela.x - m.Dir - p.Disparo.xMax, e.Disparo.xMin - m.Esq, 0.01f, "a mesma folga da borda, contada do entalhe");
            Assert.AreEqual(p.Minimapa, e.Minimapa, "so' os controles trocam de lado");
            Assert.AreEqual(p.Barras, e.Barras);
        }

        [Test]
        public void Layout_EscalaDosBotoes_CresceSemSobrepor_EEncolheSemPassarDe48dp()
        {
            float px = 395f / 160f;
            var tela = new Vector2(2400, 1080);
            var m = new Margens();
            HudLayout n = HudLayout.Calcular(tela, m, px), g = HudLayout.Calcular(tela, m, px, 1.4f), p = HudLayout.Calcular(tela, m, px, 0.8f);
            Assert.Greater(g.Disparo.width, n.Disparo.width, "1,4: maior");
            Assert.Less(p.Disparo.width, n.Disparo.width, "0,8: menor");
            Assert.AreEqual(n.Disparo, HudLayout.Calcular(tela, m, px, 1f).Disparo, "o padrao nao muda");
            foreach (HudLayout l in new[] { g, p, HudLayout.Calcular(tela, m, px, 1.4f, true) })
            {
                Rect[] toque = { l.Disparo, l.Esquiva, l.Tatica, l.Suprema, l.Pegar, l.Salto, l.Joystick, l.Carrossel };
                for (int i = 0; i < toque.Length; i++)
                {
                    Assert.GreaterOrEqual(Mathf.Min(toque[i].width, toque[i].height), Dp.AlvoMinimoDp * px - 0.01f, "alvo de toque >= 48 dp");
                    Assert.IsTrue(toque[i].xMin >= -0.01f && toque[i].xMax <= tela.x + 0.01f, "dentro da tela");
                    for (int j = i + 1; j < toque.Length; j++) Assert.IsFalse(toque[i].Overlaps(toque[j]), "sem sobrepor: " + i + " x " + j);
                }
            }
        }
    }
}
