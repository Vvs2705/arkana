using System.Collections.Generic;

namespace Arkana.Core
{
    /// <summary>
    /// TEXTOS DE UI — PT-BR, num lugar so'. Espelho de mobile-godot/godot/core/Textos.gd.
    /// Nenhum texto solto no codigo: quem desenha tela LE daqui. Quando o EN entrar, este arquivo vira
    /// a origem do CSV sem que nenhuma tela mude. Formatos usam {0}/{1} (string.Format), nao %d/%s.
    /// </summary>
    public static partial class Textos
    {
        // ---------------------------------------------------------------- titulo
        public const string Marca = "ARKANA";
        public const string TituloSub = "Magos Battle Royale";
        public const string TituloToque = "TOQUE PARA COMEÇAR";

        // ---------------------------------------------------------------- tela de carregamento (GDD §11)
        public const string CarregaPartida = "PREPARANDO A PARTIDA";
        public const string CarregaTreino = "PREPARANDO O TREINO";
        public const string CarregaDica = "DICA";
        public const string CarregaPct = "{0}%";
        /// <summary>O passo que a montagem esta' dando agora (a linha fina sobre a barra): a barra anda de verdade.</summary>
        public const string CarregaMagos = "Convocando os magos";
        public const string CarregaArena = "Erguendo a arena";
        public const string CarregaCastelo = "O castelo se aproxima";
        public const string CarregaTerreno = "Despertando o terreno";
        public const string CarregaPronto = "Tudo pronto";
        /// <summary>
        /// DICAS da tela de carregamento: "as dicas ensinam a matriz de combos de graca" (GDD §11). Cada uma cabe em duas
        /// linhas da placa. So' o que o jogo FAZ hoje (GDD §14 e o que o Kits/Balance cobram) — dica que mente ensina errado.
        /// </summary>
        public static readonly string[] Dicas =
        {
            "Água conduz Raio — cuidado onde pisa.",
            "Fogo na grama e nas árvores se espalha, e a mata queimada vira carvão: a cobertura some.",
            "Sintonia: acerte o mesmo alvo que o parceiro com OUTRO elemento, quase juntos — as duas magias se fundem num combo.",
            "Água congela o lago por 10 s e vira ponte — mas o Fogo derrete, e quem está em cima cai.",
            "Raio na água eletrocuta todo mundo que está nela. Saia da água antes do raio.",
            "Terra ergue um muro de pedra de 60 de vida: cobertura na hora, que qualquer dano derruba.",
            "Água em chão de terra vira lamaçal e deixa todos lentos. O Fogo seca a lama.",
            "Vento é faca de dois gumes: espalha o fogo, mas também dissipa névoa e gás.",
            "Cada elemento tem a sua forma: círculo é Fogo, gota é Água, raio é Raio, quadrado é Terra, espiral é Vento.",
            "Kits não gastam mana: a tática volta em segundos e a suprema carrega com o tempo e com o dano que você causa.",
            "A suprema não carrega no ar: pouse cedo para chegar à luta com ela pronta.",
            "Toda suprema é avisada: um anel acende no chão e enche até ela sair. Viu o anel, saia de perto.",
            "O escudo evolui com o dano que você causa: branco, azul, roxo e dourado. Ele não se regenera sozinho.",
            "A Manopla lendária só sai do Baú Celestial. Canalize perto dele: sair do raio cancela.",
            "A tempestade arcana fecha a ilha em fases. Fora da zona, a vida escorre a cada segundo.",
            "No ar, toque o salto de novo para abrir ou fechar o planeio.",
            "Em dupla, com a Sintonia pronta, toque no anel em volta do ataque: o parceiro aceita o combo no inimigo da sua mira.",
        };

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
        /// <summary>Cartao do mago escolhido (a vitrine do Elenco): abas, barras, o tempo do kit e o botao.</summary>
        public const string SelTodos = "TODOS";
        public const string SelConfirmar = "CONFIRMAR";
        public const string SelAlcance = "ALCANCE";
        public const string SelPorte = "PORTE";
        public const string SelAltura = "{0} m";
        public const string SelRecarga = "recarga {0} s";
        public const string SelCarga = "carga {0} s";
        /// <summary>A escala de ALCANCE das fichas, do mais curto ao mais longo: a barra do cartao enche por aqui.</summary>
        public static readonly string[] SelAlcances = { "Muito curto", "Curto", "Curto-médio", "Médio", "Longo", "Muito longo" };

        /// <summary>
        /// A FICHA DE TELA de cada mago, resumida de design/personagens/NN-*.md (a mesma fonte que os kits seguem):
        /// { titulo, papel (classe ou raca · funcao), alcance (um de SelAlcances), tatica, descricao, suprema, descricao }.
        /// Descricao curta: cabe em 2 linhas do cartao. `{chave}` = numero do bloco Tatica/Suprema do Kits DAQUELE mago —
        /// o Elenco troca pelo valor e o texto acompanha o balanceamento (kit novo pode trocar o numero escrito pela chave).
        /// ponytail: o alcance de 02/03/04/07/08 foi lido do kit (a ficha deles nao tem a linha Alcance); o Diretor corrige aqui.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> SelFichas = new Dictionary<string, string[]>
        {
            { "01-pyra", new[] { "A Chama de Guerra", "Vanguarda · Ataque", "Médio",
                "Muralha de Brasas", "Risca {comprimento} m de fogo por {duracao} s: quem cruza queima e sai aceso.",
                "Braço Livre", "Solta o braço de chama por {duracao} s: lança-chamas em leque e dash em fogo." } },
            { "02-ceifadora", new[] { "A Voz do Vazio", "Vanguarda · Perseguição", "Curto-médio",
                "Mão do Vazio", "Uma mão de sombra irrompe a até 12 m e agarra o inimigo por 1,2 s.",
                "Travessia", "Rasga o Vazio até 60 m à frente: ela e o grupo atravessam juntos." } },
            { "03-veu", new[] { "A Andarilha", "Errante · Fuga", "Curto",
                "Atravessar", "{duracao} s no plano espectral: invulnerável, veloz, passa paredes de {parede_max} m.",
                "Maré Espectral", "Por {duracao} s, ela e quem estiver a {raio} m entram juntos no plano espectral." } },
            { "04-corvus", new[] { "O Caçador", "Errante · Perseguição", "Curto",
                "Uivo de Caça", "Uivo de 25 m: quem se move fica com o cheiro aceso por 4 s.",
                "Forma de Lobisomem", "30 s como besta: 40% mais rápido, cura ao abater e vê todos os rastros." } },
            { "05-corvomante", new[] { "O Olho Distante", "Vidente · Longa distância", "Longo",
                "Voo do Olho", "Voa com o corvo para vasculhar, ou o pousa num aliado como sentinela.",
                "Grasnido do Fim", "Pulso antimagia: 50 de dano em escudo e destrói armadilhas na área." } },
            { "06-olho-de-eter", new[] { "O Observador", "Vidente · Longa distância", "Longo",
                "Enxame Perscrutador", "Enxame em linha: interrompe conjuração e cura, e revela por 6 s.",
                "Crisálida", "Casulo que eclode em 2 s: mariposas pousam nos inimigos e os revelam." } },
            { "07-vitalis", new[] { "A Mão que Cura", "Guardião · Suporte / Vida", "Médio",
                "Vai, Lúmen", "Envia Lúmen a um aliado a até 30 m: cura 8 de vida/s por 12 s.",
                "Jardim da Aurora", "Círculo de luz por 10 s: aliados curam 8/s; inimigos não curam nada." } },
            { "08-ilusionista", new[] { "O Espelho", "Guardião · Engano", "Médio",
                "Espelho de Mão", "Espelho por 2 s que devolve até 3 projéteis mágicos a quem atirou.",
                "Baile de Espelhos", "5 reflexos imitam cada passo dele, e ele some por 2,5 s." } },
            { "09-vex", new[] { "O Alquimista da Peste", "Dominador · Área", "Curto",
                "Frascos de Reagente", "Até 6 frascos viram poças: quem pisa detona nuvem de dano e lentidão.",
                "A Grande Obra", "Névoa enorme por 12 s: desacelera e ninguém dentro usa cura ou poção." } },
            { "10-tessa", new[] { "A Tecelã de Raios", "Dominadora · Defesa", "Curto",
                "Fio do Tear", "Fio de raio de {comprimento} m (até {max_fios}): quem toca leva dano, fica lento e revelado.",
                "Tear-Mãe", "Tear de {raio} m por {duracao} s: absorve projéteis e os tece em escudo do grupo." } },
            { "11-aelion", new[] { "O Arco do Crepúsculo", "Alto Elfo · Longa distância", "Muito longo",
                "Flecha de Éter", "Flecha muito rápida que fura 1 obstáculo fino; carregada, bate mais.",
                "Chuva do Crepúsculo", "Dispara ao céu: 3 s depois, flechas caem numa linha longa à frente." } },
            { "12-umbra", new[] { "A Lâmina da Noite", "Drow · Perseguição", "Curto",
                "Véu Umbrio", "2,5 s em penumbra, quase invisível; o 1º golpe saindo dela bate +50%.",
                "Dança das Sombras", "Por 6 s, cada esquiva teleporta 8 m e deixa uma sombra que explode." } },
            { "13-brok", new[] { "O Ferreiro de Runas", "Anão · Proteção", "Curto-médio",
                "Runa-Escudo", "Ergue uma muralha rúnica curva de 4 m que segura projéteis por 6 s.",
                "Forja Viva", "Bigorna-totem por 12 s: aliados perto regeneram escudo e ganham runa." } },
            { "14-gromm", new[] { "O Xamã da Tempestade", "Orc · Suporte / Vida", "Médio",
                "Totem das Chuvas", "Totem de chuva que cura 6 de vida/s por 8 s e apaga o fogo em volta.",
                "Espírito do Trovão", "Um bisão-espírito cruza o campo empurrando inimigos e derrubando muros." } },
            { "15-maris", new[] { "A Voz das Marés", "Nereida · Controle", "Longo",
                "Onda Prisão", "Esfera lenta que ergue uma coluna d'água e suspende o inimigo 1,2 s.",
                "Maré Cheia", "Inunda uma área por 10 s: aliados deslizam, inimigos afundam lentos." } },
            { "16-fizz", new[] { "O Artífice de Bolso", "Gnomo · Armadilhas", "Longo",
                "Torreta Faísca", "Mini-torreta que atira faíscas em quem entra no cone; até 2 ativas.",
                "MEGABOBINA", "Em 2 s, a bobina gigante fulmina o inimigo marcado mais próximo." } },
            { "17-sylva", new[] { "A Filha da Floresta", "Dríade · Suporte / Vida", "Médio",
                "Broto Guardião", "Broto que cura 5 de vida/s por 6 s; plantado na grama, vira moita.",
                "Coração da Mata", "Raízes por 8 s: aliados curam vida e escudo; inimigos são agarrados." } },
            { "18-basalto", new[] { "O Desperto", "Golem · Tanque", "Muito curto",
                "Punho Sísmico", "Onda de pedra em cone que empurra e, na terra, ergue 3 pedras.",
                "Monólito", "6 s como torre: +60% de resistência e 15% do dano volta em estilhaços." } },
            { "19-noctus", new[] { "O Sedento de Éter", "Vampiro · Perseguição", "Curto-médio",
                "Mordida do Vazio", "Investida de 6 m que drena 20 de mana do alvo e o marca.",
                "Forma de Névoa", "4 s de névoa: imune a projéteis e atravessa inimigos, mas não conjura." } },
            { "20-pip", new[] { "A Centelha Selvagem", "Fada · Perseguição", "Curto",
                "Zip-Zag", "Três dashes em zigue-zague; quem ela atravessa leva uma faísca.",
                "Supercélula", "Uma nuvem de tempestade a segue por 8 s, raiando o inimigo mais perto." } },
        };

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
        public const string CfgComboNota = "Sintonia: acerte o mesmo alvo que o parceiro, com outro elemento, quase juntos — as duas magias se fundem.";

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

        // MAPA E BUSSOLA. Rumos de 45 em 45 graus a partir do norte (L = leste, O = oeste: o PT-BR, nao o E/W).
        public static readonly string[] MapaRumos = { "N", "NE", "L", "SE", "S", "SO", "O", "NO" };
        public const string MapaFechar = "TOQUE PARA FECHAR";
        /// <summary>Nome de cada POI no mapa grande (chave = Poi.Nome do Relevo). POI sem nome aqui fica sem rotulo.</summary>
        public static readonly IReadOnlyDictionary<string, string> MapaPois = new Dictionary<string, string>
        {
            { "alagado", "ALAGADO" }, { "floresta", "FLORESTA" }, { "lago", "LAGO" }, { "ruinas", "RUÍNAS" },
            { "dunas", "DUNAS" }, { "pico", "PICO" }, { "vale", "VALE" },
        };

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
            // os 17 kits dos grupos A-D (12/09): o preco e o poder que o jogador precisa LER no chip
            { "corvus_fera", "FORMA DE FERA" },
            { "corvus_ofegante", "OFEGANTE" },
            { "corvomante_voo", "VOO DO OLHO" },
            { "corvomante_exausto", "CORVO EXAUSTO" },
            { "olho_cego", "OLHO CEGO" },
            { "lumen_cura", "LÚMEN CURANDO" },
            { "lumen_apagada", "LÚMEN APAGADA" },
            { "jardim", "JARDIM DA AURORA" },
            { "espelho", "ESPELHO" },
            { "espelho_quebrado", "ESPELHO QUEBRADO" },
            { "invisivel", "INVISÍVEL" },
            { "baile", "BAILE DE ESPELHOS" },
            { "grande_obra", "A GRANDE OBRA" },
            { "ofegante", "OFEGANTE" },
            { "carregando", "CARREGANDO" },
            { "arco_frio", "ARCO FRIO" },
            { "umbra_veu", "VÉU UMBRIO" },
            { "umbra_danca", "DANÇA DAS SOMBRAS" },
            { "brok_runas_apagadas", "RUNAS APAGADAS" },
            { "gromm_cansado", "CANSADO" },
            { "maris_encharcada", "ENCHARCADA" },
            { "encharcada", "ENCHARCADA" },
            { "molas_travadas", "MOLAS TRAVADAS" },
            { "seiva_gasta", "SEIVA GASTA" },
            { "monolito", "MONÓLITO" },
            { "pernas_duras", "PERNAS DURAS" },
            { "nevoa", "NÉVOA" },
            { "faminto", "FAMINTO" },
            { "supercelula", "SUPERCÉLULA" },
        };

        // ---------------------------------------------------------------- sintonia e dupla (GDD §9, onda 17)
        public const string Sintonia = "SINTONIA";
        public const string SintoniaQuebrada = "SINTONIA QUEBRADA";
        public const string SintoniaPronta = "SINTONIA PRONTA";
        public const string HudDuplas = "TRIOS {0}";
        public const string Espectando = "ESPECTANDO · {0}";
        public const string Parceiro = "PARCEIRO";
        public const string ModoRotulo = "MODO";
        public const string ModoDupla = "TRIO";
        public const string ModoSolo = "SOLO";
        public const string DuplaEliminada = "TRIO ELIMINADO";

        /// <summary>O nome do combo na tela (faixa da canalizacao e disparo), ja' com acento.</summary>
        public static string ComboNome(ComboSintonia c)
        {
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante: return "TORNADO FLAMEJANTE";
                case ComboSintonia.ChuvaDeMagma: return "CHUVA DE MAGMA";
                case ComboSintonia.ExplosaoDePlasma: return "EXPLOSÃO DE PLASMA";
                case ComboSintonia.CortinaDeVapor: return "CORTINA DE VAPOR";
                case ComboSintonia.Eletrocussao: return "ELETROCUSSÃO";
                case ComboSintonia.Lamacal: return "LAMAÇAL";
                case ComboSintonia.TempestadeTorrencial: return "TEMPESTADE TORRENCIAL";
                case ComboSintonia.TempestadeDeAreia: return "TEMPESTADE DE AREIA";
                case ComboSintonia.CristaisCarregados: return "CRISTAIS CARREGADOS";
                case ComboSintonia.NuvemTempestuosa: return "NUVEM TEMPESTUOSA";
            }
            return "?";
        }

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
