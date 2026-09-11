using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>HUD pura: rotulo da arma some em 2,5 s; maos nuas escondem o carrossel; kill feed com nome; TREINO no relogio;
    /// numeros com merge; prioridade da faixa; cooldown interpolado; pausa DENTRO da tela; alvos &gt;= 48dp; daltonismo.</summary>
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
    }
}
