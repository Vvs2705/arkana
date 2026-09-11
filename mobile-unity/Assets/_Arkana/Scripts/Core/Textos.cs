using System.Collections.Generic;

namespace Arkana.Core
{
    /// <summary>
    /// TEXTOS DE UI — PT-BR, num lugar so'. Espelho de mobile-godot/godot/core/Textos.gd.
    /// Nenhum texto solto no codigo: quem desenha tela LE daqui. Quando o EN entrar, este arquivo vira
    /// a origem do CSV sem que nenhuma tela mude. Formatos usam {0}/{1} (string.Format), nao %d/%s.
    /// </summary>
    public static class Textos
    {
        // ---------------------------------------------------------------- titulo
        public const string Marca = "ARKANA";
        public const string TituloSub = "Magos Battle Royale";
        public const string TituloToque = "TOQUE PARA COMEÇAR";

        // ---------------------------------------------------------------- menu principal
        public const string MenuJogar = "JOGAR";
        public const string MenuTreino = "TREINO";
        public const string HudPausa = "II";
        public const string PausaTitulo = "PAUSA";
        public const string PausaRetomar = "RETOMAR";
        public const string PausaAbandonar = "ABANDONAR PARTIDA";
        public const string MenuPersonagens = "PERSONAGENS";
        public const string MenuConfig = "CONFIGURAÇÕES";
        public const string MenuSair = "SAIR";
        public const string Voltar = "← VOLTAR";

        // ---------------------------------------------------------------- selecao de mago
        public const string SelTitulo = "MAGOS DE ARKANA";
        public const string SelEmBreve = "EM BREVE";
        public const string SelClasse = "Classe";
        public const string SelFuncao = "Função";
        public const string SelRaca = "Raça";
        public const string SelADefinir = "a definir";

        // ---------------------------------------------------------------- perfil do mago
        public const string PerfilHistoria = "HISTÓRIA";
        public const string PerfilKit = "KIT";
        public const string PerfilPassiva = "Passiva";
        public const string PerfilTatica = "Tática";
        public const string PerfilSuprema = "Suprema";
        public const string PerfilLimitadores = "LIMITADORES — o preço de cada poder";
        public const string PerfilFechar = "FECHAR";
        /// <summary>Proposital que seja feio e obvio: placeholder invisivel vira bug.</summary>
        public const string PerfilPendente = "[ texto em produção ]";

        // ---------------------------------------------------------------- configuracoes
        public const string CfgTitulo = "CONFIGURAÇÕES";
        public const string CfgAbaVideo = "Vídeo";
        public const string CfgAbaAudio = "Áudio";
        public const string CfgAbaControles = "Controles";
        public const string CfgAbaJogo = "Jogo";
        public const string CfgRestaurar = "RESTAURAR PADRÃO";

        public const string CfgTelaCheia = "Tela cheia";
        public const string CfgQualidade = "Qualidade";
        public const string CfgFpsLimite = "Limite de FPS";
        public const string CfgVsync = "VSync";
        public const string CfgContadorFps = "Contador de FPS";
        public static readonly string[] CfgQualidades = { "Baixa", "Média", "Alta" };
        public static readonly string[] CfgFpsOpcoes = { "30", "60", "120", "Ilimitado" };

        public const string CfgVolGeral = "Volume geral";
        public const string CfgVolMusica = "Música";
        public const string CfgVolEfeitos = "Efeitos";
        public const string CfgVolInterface = "Interface";

        public const string CfgSensibilidade = "Sensibilidade";
        public const string CfgSensMira = "Sensibilidade ao mirar";
        public const string CfgInverterY = "Inverter eixo Y";
        public const string CfgTeclasNota = "Remapeamento de teclas: só no PC (em breve).";
        /// <summary>Nota no lugar de botao morto (R21): a tela so' mostra opcao que muda o jogo.</summary>
        public const string CfgEsquemaNota = "Esquema de toque: hoje o jogo é todo Avançado — mira 100% manual. O Simples volta ao menu junto com a assistência de mira.";

        public const string CfgDaltonismo = "Modo daltonismo";
        public static readonly string[] CfgDaltonismos = { "Nenhum", "Protanopia", "Deuteranopia", "Tritanopia" };
        public const string CfgNumerosDano = "Números de dano";
        public const string CfgIdiomaNota = "Idioma: só PT-BR por enquanto.";
        public const string CfgComboNota = "Dicas de combo entram com a Conjuração Combinada (Sintonia).";

        // ---------------------------------------------------------------- bau celestial (GDD §16.2)
        public const string BauTitulo = "BAÚ CELESTIAL";
        public const string BauCaindo = "CAINDO EM {0}s";
        public const string BauPousou = "BAÚ NO CHÃO";
        public const string BauAbrindo = "ABRINDO...";
        public const string BauManopla = "MANOPLA: {0} + {1}";
        public const string BauPerdido = "A MANOPLA CAIU EM OUTRAS MÃOS";
        public const string BauCancelado = "CANALIZAÇÃO INTERROMPIDA";

        // ---------------------------------------------------------------- HUD de partida
        // Rotulos curtos: vivem ao lado de barras finas e botoes de 48dp. ESQV e' abreviacao de proposito.
        public const string HudSep = " · ";
        public const string HudVida = "VIDA";
        public const string HudMana = "MANA";
        public const string HudEscudo = "ESCUDO";
        public const string HudSalto = "SALTO";
        public const string HudBots = "BOTS {0}";
        public const string HudFps = "{0} FPS";
        public const string HudVitoria = "VITÓRIA!";
        public const string HudDerrota = "DERROTA";
        public const string HudJogarDeNovo = "JOGAR DE NOVO";
        public const string HudMenu = "MENU";
        public const string HudEsquiva = "ESQV";
        /// <summary>Rotulo do botao de fogo por elemento (id do Godot -> nome).</summary>
        public static readonly IReadOnlyDictionary<string, string> HudElementos = new Dictionary<string, string>
        {
            { "fire", "FOGO" }, { "water", "ÁGUA" }, { "lightning", "RAIO" },
            { "earth", "TERRA" }, { "wind", "VENTO" },
        };

        // HABILIDADES (pagam COOLDOWN, nunca mana). O nome de cada uma sai do Elenco; estes sao o generico.
        public const string HudTatica = "TÁTICA";
        public const string HudSuprema = "SUPREMA";
        public const string HudKitEmBreve = SelEmBreve;
        public const string HudTelegrafo = "SUPREMA: {0}";

        // ZONA / TEMPESTADE ARCANA — o que salva a vida do jogador vem primeiro.
        public const string ZonaAbertura = "MAPA ABERTO · TEMPESTADE EM {0}s";
        public const string ZonaFormando = "A TEMPESTADE CERCA A ILHA";
        public const string ZonaAviso = "A TEMPESTADE AVANÇA EM {0}s";
        public const string ZonaFechando = "A TEMPESTADE ESTÁ AVANÇANDO";
        public const string ZonaFora = "VOLTE PARA A ZONA";
        public const string ZonaDps = "{0}/s";

        // DERRUBADO (vocabulario 10+: DERRUBADO e ESVAECER, nunca mutilacao).
        public const string DerrubadoVoce = "VOCÊ FOI DERRUBADO";
        public const string DerrubadoEsvaecendo = "ESVAECENDO {0}s";
        public const string DerrubadoReerguendo = "REERGUENDO";
        public const string DerrubadoAliado = "ALIADO DERRUBADO";

        // LOOT E ARMA ARCANA (GDD §16.2). Com luva na mao o botao muda de nome: pegar e trocar sao atos diferentes.
        public const string LootPegar = "PEGAR";
        public const string LootTrocar = "TROCAR";
        public const string LootPegou = "PEGOU: {0}";
        public const string ArmaPar = "{0} + {1}";

        /// <summary>ESTADOS DE KIT (Bus.KitState). Nome que NAO esta' aqui nao vira badge — a HUD ignora o que nao sabe desenhar.</summary>
        public static readonly IReadOnlyDictionary<string, string> HudEstados = new Dictionary<string, string>
        {
            { "braco_livre", "BRAÇO LIVRE" },
            { "braco_frio", "BRAÇO FRIO" },
            { "braco_molhado", "BRAÇO MOLHADO" },
            { "desfocada", "DESFOCADA" },
            { "silencio", "SEM CONJURAR" },
            { "sino_espectral", "SINO ESPECTRAL" },
            { "escudo_quebrado", "ESCUDO QUEBRADO" },
            { "revelado", "REVELADO" },
            { "fio_zumbido", "FIO ZUMBINDO" },
        };

        /// <summary>
        /// O ROTULO DE UMA ARMA NA TELA: nome + o elemento que ela impoe ao disparo (ordem do Diretor, 26/08).
        /// Uma funcao so' porque duas telas mostram isto (PEGAR/TROCAR e a confirmacao de quem equipou).
        /// Vazio/desconhecido devolve so' o nome — nunca "Luva · " pendurado.
        /// </summary>
        public static string ArmaRotulo(string nome, string[] els)
        {
            List<string> limpos = new List<string>();
            if (els != null)
            {
                foreach (string e in els)
                {
                    string rotulo;
                    if (!string.IsNullOrEmpty(e) && HudElementos.TryGetValue(e, out rotulo)) limpos.Add(rotulo);
                }
            }
            if (limpos.Count == 0) return nome;
            if (limpos.Count == 1) return nome + HudSep + limpos[0];
            return nome + HudSep + string.Format(ArmaPar, limpos[0], limpos[1]);
        }

        /// <summary>Mesma coisa, a partir do Elemento[] que o Bus.WeaponEquipped carrega.</summary>
        public static string ArmaRotulo(string nome, Elemento[] els)
        {
            if (els == null) return nome;
            string[] ids = new string[els.Length];
            for (int i = 0; i < els.Length; i++) ids[i] = Elementos.Id(els[i]);
            return ArmaRotulo(nome, ids);
        }
    }
}
