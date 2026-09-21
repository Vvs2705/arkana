using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Menu;

namespace Arkana.Gameplay
{
    /// <summary>
    /// GRIMORIO DE DESCOBERTAS (GDD §18.3, PONTE A18): 12 paginas, cada uma uma INTERACAO do jogo que o jogador achou
    /// sozinho — 6 de terreno, 6 de dupla e desfecho. Progressao por EXPERIMENTACAO, nao por poder.
    ///
    /// REGRA DURA (Juramento §19.1): nenhuma pagina da' vantagem. Por CONSTRUCAO, nao por promessa: esta classe so' OUVE o
    /// Bus e grava UM inteiro no aparelho — nao escreve em Vitalidade, Balance, Combat, Partida nem em pawn nenhum (o teste
    /// roda as 12 descobertas e compara o mundo antes/depois). Nada sai do celular.
    ///
    /// Uma pagina acende UMA vez na vida: `PrefChave` guarda a mascara (bit i = Paginas[i]). A ordem so' CRESCE no fim —
    /// bit gravado nunca troca de dono; formato novo = chave nova ("grimorio.v2").
    ///
    /// DUAS TROCAS em relacao ao Roblox (Grimoire.luau), porque a interacao de la' nao existe aqui hoje:
    ///  - "Ponte de Gelo" (atravessar a agua congelada): no celular o gelo e' so' placa e corte de conducao — o corpo nada
    ///    por baixo (Pawn/Agua leem o relevo, nao o TerrenoReativo). Vira LAGO CONGELADO: congelar a agua com o seu tiro.
    ///  - "Fumacar o Esconderijo" (queimar grama alta que escondia alguem): nao ha' esconderijo em grama no celular. Vira
    ///    VENTO NO FOGO: o vento espalha a chama (TerrenoReativo.AplicarVento) — o "ops, alimentou" do GDD §18.3.
    ///
    /// Casca: Main cria por partida (Ligar/Desligar); o AvisoGrimorio ticka (o relogio do "no vermelho" e da janela do corte).
    /// </summary>
    public sealed class Grimorio
    {
        // ---------------------------------------------------------------- catalogo (ORDEM CANONICA: so' cresce no fim)
        public const string LagoCongelado = "lago_congelado", Conducao = "conducao", FogoApagado = "fogo_apagado",
            MuroDePedra = "muro_de_pedra", Lamacal = "lamacal", VentoNoFogo = "vento_no_fogo";
        public const string Sintonia = "sintonia", PactoQuebrado = "pacto_quebrado", Retorno = "retorno",
            MapaArma = "o_mapa_e_arma", NoVermelho = "no_vermelho", EscudoCoroado = "escudo_coroado";

        /// <summary>As 12, na ordem do livro (as DeTerreno primeiras sao as de terreno). So' ids: o catalogo nao TEM onde
        /// pendurar numero de jogo.</summary>
        public static readonly string[] Paginas =
        {
            LagoCongelado, Conducao, FogoApagado, MuroDePedra, Lamacal, VentoNoFogo,
            Sintonia, PactoQuebrado, Retorno, MapaArma, NoVermelho, EscudoCoroado,
        };
        public const int DeTerreno = 6;
        public const int Todas = (1 << 12) - 1;

        // ---------------------------------------------------------------- persistencia
        public const string PrefChave = "grimorio.v1";
        /// <summary>Onde mora (o padrao do ConfigLogica.Store): PlayerPrefs no jogo; o teste e a foto trocam por PrefsMemoria —
        /// nada grava no aparelho de quem roda.</summary>
        public static IPrefs Store = new PrefsPlayer();

        /// <summary>A mascara gravada (o menu le' daqui). Save adulterado/ilegivel = livro vazio, nunca excecao.</summary>
        public static int MascaraSalva()
        {
            try { return Store.LerInt(PrefChave, 0) & Todas; }
            catch (Exception) { return 0; }
        }

        public static int Indice(string id) => Array.IndexOf(Paginas, id);
        public static bool AcesaEm(int mascara, string id) { int i = Indice(id); return i >= 0 && (mascara & (1 << i)) != 0; }
        public static int Contar(int mascara) { int n = 0; for (int m = mascara & Todas; m != 0; m &= m - 1) n++; return n; }

        // ---------------------------------------------------------------- KNOBs da DESCOBERTA (nao sao balanceamento)
        /// <summary>m (no plano) entre o impacto do tiro e a celula que mudou: splash de agua (1,5 cel) + o vizinho que o vento
        /// acende + meia diagonal, a 3 m por celula ~ 9,6 m. O raio do lago eletrificado passa longe: basta a 1a celula.</summary>
        public const float AlcanceTerrenoM = 10f;
        /// <summary>"No vermelho": fracao de vida quando a zona morde, e quanto tempo de pe' depois (o Roblox: 0,25 e 10 s).</summary>
        public const float VermelhoFracao = 0.25f, VermelhoSobreviveS = 10f;
        /// <summary>s: o golpe do jogador que conta como o corte de uma Sintonia inimiga (o Roblox: 4 s).</summary>
        public const float CorteS = 4f;
        public const int EscudoNivel = 3;
        /// <summary>Interacoes de terreno DISTINTAS numa partida para "O Mapa e' Arma" (contam mesmo ja' acesas).</summary>
        public const int TerrenoParaMapa = 3;

        // ---------------------------------------------------------------- estado
        public int Mascara { get; private set; }
        public int Acesas => Contar(Mascara);
        public bool Acesa(string id) => AcesaEm(Mascara, id);
        /// <summary>A pagina ACABOU de acender (uma vez na vida): o aviso em partida ouve.</summary>
        public event Action<string> Acendeu;

        /// <summary>Os tiros em voo (o autor do terreno sai daqui). Padrao: a partida em curso; o teste injeta.</summary>
        public Func<IList<Projetil>> Projeteis = () => Partida.Atual != null ? Partida.Atual.Projeteis : null;

        struct Golpe { public IEntidade Fonte; public float Em; }
        readonly Dictionary<IEntidade, Golpe> _ultimoGolpe = new Dictionary<IEntidade, Golpe>();
        readonly HashSet<Vector2Int> _queimando = new HashSet<Vector2Int>();
        readonly HashSet<string> _terrenoNaPartida = new HashSet<string>();
        float _agora;
        float _vidaFrac = 1f;
        float _vermelhoDesde = -1f;
        bool _caido;   // o jogador esta' derrubado (a vida dele e' a reserva de esvaecer, nao "de pe'")
        bool _ligado;

        public Grimorio() { Mascara = MascaraSalva(); }

        /// <summary>O relogio (janela do corte, "no vermelho"). Quem ticka e' a casca, 1x por frame.</summary>
        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            _agora += dt;
            if (_vermelhoDesde >= 0f && _agora - _vermelhoDesde >= VermelhoSobreviveS)
            {
                _vermelhoDesde = -1f;
                Descobrir(NoVermelho);
            }
        }

        public void Ligar()
        {
            if (_ligado) return;
            _ligado = true;
            Bus.MatchStarted += AoComecar;
            Bus.MatchOver += AoAcabar;
            Bus.TerrainChanged += AoMudarTerreno;
            Bus.SintoniaDisparou += AoDispararSintonia;
            Bus.SintoniaFalhou += AoFalharSintonia;
            Bus.EntityReerguida += AoReerguer;
            Bus.DamageApplied += AoDanar;
            Bus.HealthChanged += AoMudarVida;
            Bus.ZonaDano += AoMorderZona;
            Bus.EntityDerrubada += AoCair;
            Bus.EntityDied += AoMorrer;
        }

        public void Desligar()
        {
            if (!_ligado) return;
            _ligado = false;
            Bus.MatchStarted -= AoComecar;
            Bus.MatchOver -= AoAcabar;
            Bus.TerrainChanged -= AoMudarTerreno;
            Bus.SintoniaDisparou -= AoDispararSintonia;
            Bus.SintoniaFalhou -= AoFalharSintonia;
            Bus.EntityReerguida -= AoReerguer;
            Bus.DamageApplied -= AoDanar;
            Bus.HealthChanged -= AoMudarVida;
            Bus.ZonaDano -= AoMorderZona;
            Bus.EntityDerrubada -= AoCair;
            Bus.EntityDied -= AoMorrer;
        }

        // ---------------------------------------------------------------- a regra pura do terreno

        /// <summary>
        /// Espelho do TerrenoReativo.Reagir: a mudanca `tipo` (o nome do Bus.TerrainChanged) causada por um tiro de `el`
        /// acende qual pagina? `queimava` = a celula estava em chamas (so' assim "clear" com agua e' APAGAR — muro que cai
        /// tambem vira "clear"). Se a quimica do TerrenoReativo mudar, esta tabela muda junto (o teste e' onde aparece).
        /// </summary>
        public static string PaginaDoTerreno(string tipo, bool queimava, Elemento el)
        {
            switch (tipo)
            {
                case "ice": return el == Elemento.Agua ? LagoCongelado : null;
                case "electric": return el == Elemento.Raio ? Conducao : null;
                case "clear": return el == Elemento.Agua && queimava ? FogoApagado : null;
                case "wall": return el == Elemento.Terra ? MuroDePedra : null;
                case "mud": return el == Elemento.Agua ? Lamacal : null;
                case "burn": return el == Elemento.Vento ? VentoNoFogo : null;
            }
            return null;
        }

        /// <summary>
        /// ponytail: AUTORIA pelo tiro que esta' MORRENDO agora. O TerrainChanged nao traz autor, mas e' SINCRONO dentro do
        /// Projetil.Impacto (TerrainHit -> TerrenoReativo.Reagir -> SetEstado -> TerrainChanged), e o projetil so' sai da
        /// Partida.Projeteis depois: quem esta' morto (!Vivo) ali, perto da celula, com o elemento certo, e' o autor exato.
        /// Teto: kit e combo da Sintonia mudam terreno sem projetil — nao acendem pagina de terreno; um tiro morto fora do
        /// laco da Partida (peca de kit que o para) espera 1 quadro na lista. Sobe com Bus.TerrainChanged ganhar o autor.
        /// </summary>
        void AoMudarTerreno(string tipo, Vector3 pos)
        {
            var celula = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));   // centros a 3 m: unico por celula
            bool queimava = _queimando.Remove(celula);
            if (tipo == "burn") _queimando.Add(celula);
            IList<Projetil> tiros = Projeteis != null ? Projeteis() : null;
            if (tiros == null) return;
            for (int i = 0; i < tiros.Count; i++)
            {
                Projetil p = tiros[i];
                if (p == null || p.Vivo || p.Atirador == null || !p.Atirador.EhPlayer) continue;
                if (new Vector2(p.Pos.x - pos.x, p.Pos.z - pos.z).sqrMagnitude > AlcanceTerrenoM * AlcanceTerrenoM) continue;
                string pag = PaginaDoTerreno(tipo, queimava, p.ElementoDoTiro);
                if (pag == null) continue;
                _terrenoNaPartida.Add(pag);   // conta para "O Mapa e' Arma" mesmo com a pagina ja' acesa
                Descobrir(pag);
                return;
            }
        }

        // ---------------------------------------------------------------- dupla e desfecho

        /// <summary>Os DOIS conjuraram: se o jogador e' um deles, ele descobriu (a dupla dele e' o outro).</summary>
        void AoDispararSintonia(DisparoSintonia d)
        {
            if ((d.A != null && d.A.EhPlayer) || (d.B != null && d.B.EhPlayer)) Descobrir(Sintonia);
        }

        /// <summary>A Sintonia INIMIGA falhou (um dos dois caiu/morreu na canalizacao) e o ultimo golpe num deles, ha' menos
        /// de CorteS, foi do jogador. "Voce leu e cortou": o golpe do parceiro bot nao e' descoberta do jogador.</summary>
        void AoFalharSintonia(ComboSintonia c, IEntidade a, IEntidade b, Vector3 ponto)
        {
            if (CortadoPeloJogador(a) || CortadoPeloJogador(b)) Descobrir(PactoQuebrado);
        }

        bool CortadoPeloJogador(IEntidade x)
        {
            Golpe g;
            return x != null && _ultimoGolpe.TryGetValue(x, out g) && g.Fonte.EhPlayer && !Combat.MesmoTime(x, g.Fonte)
                && _agora - g.Em <= CorteS;
        }

        /// <summary>O JOGADOR reergueu alguem (o parceiro). Ser reerguido nao e' descoberta (so' volta a ficar de pe').</summary>
        void AoReerguer(IEntidade e, IEntidade por)
        {
            if (e != null && e.EhPlayer) _caido = false;
            if (por != null && por.EhPlayer && e != por) Descobrir(Retorno);
        }

        /// <summary>O golpe que ZERA a vida (derruba ou elimina) de um INIMIGO, dado pelo jogador com o escudo ja' no nivel 3+.
        /// Tambem guarda o ultimo golpe de cada alvo (o corte da Sintonia).</summary>
        void AoDanar(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool noEscudo)
        {
            if (alvo == null || fonte == null || fonte == alvo) return;
            _ultimoGolpe[alvo] = new Golpe { Fonte = fonte, Em = _agora };
            if (fonte.EhPlayer && alvo.Vital != null && !alvo.Vital.Viva && !Combat.MesmoTime(alvo, fonte)
                    && fonte.Vital != null && fonte.Vital.Nivel >= EscudoNivel)
                Descobrir(EscudoCoroado);
        }

        void AoMudarVida(float hp, float max) { _vidaFrac = max > 0f ? hp / max : 1f; }   // so' o do jogador

        /// <summary>A zona mordeu o jogador (o ZonaDano e' so' dele, DEPOIS do HealthChanged do mesmo golpe) DE PE' e ja' no
        /// vermelho: o relogio dos VermelhoSobreviveS comeca. A mordida que derruba (vida 0) nao conta; cair ou morrer zera.</summary>
        void AoMorderZona(float dano, float dps)
        {
            if (_vermelhoDesde < 0f && !_caido && _vidaFrac > 0f && _vidaFrac <= VermelhoFracao) _vermelhoDesde = _agora;
        }

        void AoCair(IEntidade e, IEntidade causador) { if (e != null && e.EhPlayer) { _caido = true; _vermelhoDesde = -1f; } }
        void AoMorrer(IEntidade e) { if (e != null && e.EhPlayer) _vermelhoDesde = -1f; }

        void AoComecar()
        {
            _ultimoGolpe.Clear(); _queimando.Clear(); _terrenoNaPartida.Clear();
            _vidaFrac = 1f; _vermelhoDesde = -1f; _caido = false;
        }

        /// <summary>O time do jogador venceu (com ele de pe' ou nao) depois de TerrenoParaMapa interacoes de terreno dele.</summary>
        void AoAcabar(bool vitoria)
        {
            if (vitoria && _terrenoNaPartida.Count >= TerrenoParaMapa) Descobrir(MapaArma);
        }

        // ---------------------------------------------------------------- a descoberta

        /// <summary>Acende UMA vez na vida: grava a mascara e avisa. Ja' acesa = nada (nem aviso).</summary>
        void Descobrir(string id)
        {
            int i = Indice(id);
            if (i < 0 || (Mascara & (1 << i)) != 0) return;
            Mascara |= 1 << i;
            try { Store.GravarInt(PrefChave, Mascara); Store.Salvar(); }
            catch (Exception) { }   // sem disco a pagina vale pela sessao; o jogo nunca cai por causa de save
            Acendeu?.Invoke(id);
        }
    }
}
