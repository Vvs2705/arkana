## TEXTOS DE UI — PT-BR, num lugar so'. Regra do projeto: nenhum texto solto no
## codigo. Quem desenha tela LE daqui; quem revisa copy mexe SO' aqui.
##
## Por que const e nao um .csv de traducao: hoje o jogo e' PT-BR (GDD par.12
## lista PT-BR/EN como opcao de Jogo, sem prazo). Um sistema de locale agora
## seria peso morto — quando o EN entrar, este arquivo vira o CSV de origem
## sem que nenhuma tela mude (todas ja' passam por Textos.*).
class_name Textos

# ---------------------------------------------------------------- titulo
## A marca. O wordmark desenha UMA Label por letra (GDD par.10).
const MARCA := "ARKANA"
const TITULO_SUB := "Magos Battle Royale"
const TITULO_TOQUE := "TOQUE PARA COMEÇAR"

# ---------------------------------------------------------------- menu principal
const MENU_JOGAR := "JOGAR"
const MENU_PERSONAGENS := "PERSONAGENS"
const MENU_CONFIG := "CONFIGURAÇÕES"
const MENU_SAIR := "SAIR"
const VOLTAR := "← VOLTAR"

# ---------------------------------------------------------------- selecao de mago
const SEL_TITULO := "MAGOS DE ARKANA"
const SEL_EM_BREVE := "EM BREVE"
const SEL_CLASSE := "Classe"
const SEL_FUNCAO := "Função"
const SEL_RACA := "Raça"
const SEL_A_DEFINIR := "a definir"

# ---------------------------------------------------------------- perfil do mago
const PERFIL_HISTORIA := "HISTÓRIA"
const PERFIL_KIT := "KIT"
const PERFIL_PASSIVA := "Passiva"
const PERFIL_TATICA := "Tática"
const PERFIL_SUPREMA := "Suprema"
const PERFIL_LIMITADORES := "LIMITADORES — o preço de cada poder"
const PERFIL_FECHAR := "FECHAR"
## Aparece em todo campo de texto que a raia de roteiro ainda nao entregou.
## E' proposital que seja feio e obvio: placeholder invisivel vira bug.
const PERFIL_PENDENTE := "[ texto em produção ]"

# ---------------------------------------------------------------- configuracoes
const CFG_TITULO := "CONFIGURAÇÕES"
const CFG_ABA_VIDEO := "Vídeo"
const CFG_ABA_AUDIO := "Áudio"
const CFG_ABA_CONTROLES := "Controles"
const CFG_ABA_JOGO := "Jogo"
const CFG_RESTAURAR := "RESTAURAR PADRÃO"

const CFG_TELA_CHEIA := "Tela cheia"
const CFG_QUALIDADE := "Qualidade"
const CFG_FPS_LIMITE := "Limite de FPS"
const CFG_VSYNC := "VSync"
const CFG_CONTADOR_FPS := "Contador de FPS"
const CFG_QUALIDADES: Array[String] = ["Baixa", "Média", "Alta"]
const CFG_FPS_OPCOES: Array[String] = ["30", "60", "120", "Ilimitado"]

const CFG_VOL_GERAL := "Volume geral"
const CFG_VOL_MUSICA := "Música"
const CFG_VOL_EFEITOS := "Efeitos"
const CFG_VOL_INTERFACE := "Interface"

const CFG_SENSIBILIDADE := "Sensibilidade"
const CFG_SENS_MIRA := "Sensibilidade ao mirar"
const CFG_INVERTER_Y := "Inverter eixo Y"
## Remapeamento de teclas (GDD par.12) e' tela de PC; o alvo agora e' celular.
const CFG_TECLAS_NOTA := "Remapeamento de teclas: só no PC (em breve)."
## NOTAS NO LUGAR DE BOTAO MORTO (R21): a tela so' mostra opcao que muda o jogo.
## Cada nota abaixo substitui um seletor que gravava no disco e nao fazia nada.
const CFG_ESQUEMA_NOTA := "Esquema de toque: hoje o jogo é todo Avançado — mira 100% manual. O Simples volta ao menu junto com a assistência de mira."

const CFG_DALTONISMO := "Modo daltonismo"
const CFG_DALTONISMOS: Array[String] = ["Nenhum", "Protanopia", "Deuteranopia", "Tritanopia"]
const CFG_NUMEROS_DANO := "Números de dano"
const CFG_IDIOMA_NOTA := "Idioma: só PT-BR por enquanto."
const CFG_COMBO_NOTA := "Dicas de combo entram com a Conjuração Combinada (Sintonia)."

## BAU CELESTIAL (evento de mundo, GDD §16.2 — a unica fonte da MANOPLA).
## Nao confundir com a suprema antiga da Vitalis: desde 20/08 a suprema dela
## e' "Jardim da Aurora"; o bau nao pertence a personagem nenhum.
const BAU_TITULO := "BAÚ CELESTIAL"
const BAU_CAINDO := "CAINDO EM %ds"
const BAU_POUSOU := "BAÚ NO CHÃO"
const BAU_ABRINDO := "ABRINDO..."
const BAU_MANOPLA := "MANOPLA: %s + %s"
const BAU_PERDIDO := "A MANOPLA CAIU EM OUTRAS MÃOS"

# ---------------------------------------------------------------- HUD de partida
## Rotulos curtos: eles vivem ao lado de barras finas e de botoes de 48dp, entao
## palavra comprida quebra o layout no celular. ESQV e' abreviacao de proposito.
const HUD_VIDA := "VIDA"
const HUD_MANA := "MANA"
const HUD_ESCUDO := "ESCUDO"
const HUD_BOTS := "BOTS %d"
const HUD_FPS := "%d FPS"
const HUD_VITORIA := "VITÓRIA!"
const HUD_DERROTA := "DERROTA"
const HUD_JOGAR_DE_NOVO := "JOGAR DE NOVO"
const HUD_MENU := "MENU"
const HUD_ESQUIVA := "ESQV"
## Rotulo do botao de Fogo por elemento — o botao segue o carrossel (R17).
const HUD_ELEMENTOS := {
	"fire": "FOGO", "water": "ÁGUA", "lightning": "RAIO",
	"earth": "TERRA", "wind": "VENTO",
}

## HABILIDADES (GDD §3/§4.2 — pagam COOLDOWN, nunca mana).
## O NOME de cada habilidade sai de menu/Elenco.gd (dado, nao texto de UI);
## estes dois sao o rotulo generico de quem ainda nao tem nome em ficha.
const HUD_TATICA := "TÁTICA"
const HUD_SUPREMA := "SUPREMA"
## Mago sem kit implementado (17 dos 20): botao apagado, mesmo verbo da tela de
## selecao para nao inventar vocabulario novo — ver SEL_EM_BREVE.
const HUD_KIT_EM_BREVE := SEL_EM_BREVE
## Telegrafo da suprema (GDD §4.3 — "se mata rapido, avisa antes").
const HUD_TELEGRAFO := "SUPREMA: %s"

## ZONA / TEMPESTADE ARCANA — o que salva a vida do jogador vem primeiro.
const ZONA_AVISO := "A TEMPESTADE AVANÇA EM %ds"
const ZONA_FECHANDO := "A TEMPESTADE ESTÁ AVANÇANDO"
const ZONA_FORA := "VOLTE PARA A ZONA"
const ZONA_DPS := "%d/s"

## DERRUBADO (vocabulario 10+: DERRUBADO e ESVAECER, nunca mutilacao).
const DERRUBADO_VOCE := "VOCÊ FOI DERRUBADO"
const DERRUBADO_ESVAECENDO := "ESVAECENDO %ds"
const DERRUBADO_REERGUENDO := "REERGUENDO"
const DERRUBADO_ALIADO := "ALIADO DERRUBADO"

## LOOT E ARMA ARCANA (GDD §16.2).
const LOOT_PEGAR := "PEGAR"
const ARMA_PAR := "%s + %s"
