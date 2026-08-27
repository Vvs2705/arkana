## NUMEROS DAS HABILIDADES — o "Balance" da raia HABILIDADES (GDD §3 e §4).
## Dono: raia GAMEPLAY/HABILIDADES. core/Balance.gd continua sendo do
## COORDENADOR: nada daqui entra la', e nada de la' e' copiado para ca'.
## Rebalancear kit = editar AQUI, nunca cacar constante dentro do efeito.
##
## AS DUAS LEIS DO GDD QUE ESTE ARQUIVO CARREGA NO FORMATO:
##  1. ECONOMIA (§4.2, revista pelo Diretor em 26/08 — docs/DIRECAO.md §4) —
##     ataque paga MANA; tatica paga COOLDOWN; a suprema paga CARGA: enche de
##     0% a 100% com o tempo E com dano causado, e so' dispara cheia. Por isso
##     NAO existe campo "mana"/"custo" em nenhuma entrada abaixo: nao e'
##     esquecimento, e' a lei. O selftest reprova se aparecer.
##  2. TELEGRAFIA (§4.3) — "se mata rapido, avisa antes": toda suprema tem
##     "telegrafia" entre 1s e 4s de som+visual ANTES do efeito. O selftest
##     reprova telegrafia fora dessa faixa em qualquer mago implementado.
##
## OS ⚖️ LIMITADORES SAO PARTE DO KIT, NAO CORTE (ordem do Diretor). Cada
## numero de limitador mora no MESMO bloco do poder que ele paga — ler o kit
## e' ler o preco junto.
##
## COMO CADASTRAR UM MAGO NOVO:
##   1) preencha a entrada dele aqui (os numeros) com "implementado": true;
##   2) crie gameplay/habilidades/<nome>.gd com os 3 estaticos do contrato
##      (tick / tatica / suprema — ver KitRunner.IMPL);
##   3) registre o script em KitRunner.IMPL com o mesmo slug.
##   Nada mais no jogo muda: Main, Pawn, Bot e HUD nao sabem quem existe.
class_name Kits

## Piso e teto da telegrafia da suprema (GDD §4.3). O KitRunner GRAMPEIA a
## telegrafia nesta faixa em runtime: um dado ruim nao consegue burlar a lei.
const TELEGRAFIA_MIN := 1.0
const TELEGRAFIA_MAX := 4.0

## Quanto da carga da suprema cada ponto de DANO CAUSADO vale (modelo Apex,
## ordem do Diretor 26/08: "faca exatamente igual para facilitar a logica").
## 0.0015 = 100 de dano adianta 15% da barra. AUMENTAR: agressao vira spam de
## suprema. DIMINUIR: esconder-se carrega quase tao rapido quanto lutar — e o
## ponto do modelo e' exatamente premiar quem luta.
const CARGA_POR_DANO := 0.0015

## Campos que TODO mago tem. Quem nao declara herda daqui — inclusive os 17
## ainda nao implementados, que ficam declarados e inertes.
const PADRAO := {
	"nome": "?",
	"implementado": false,
	## Faixa oficial 5-10s (DIRECAO.md §4): agressao no teto, utilidade no piso.
	"tatica_cd": 7.0,      # s — KNOB
	## Segundos ate' a carga passiva encher 100% (dano causado ACELERA — ver
	## CARGA_POR_DANO). Era "suprema_cd" de 100s: cooldown de suprema morreu em 26/08.
	"suprema_carga": 45.0, # s — KNOB
	"telegrafia": 1.5,     # s de aviso da suprema (GDD §4.3)
	"passiva": {},
	"tatica": {},
	"suprema": {},
}

const MAGOS := {
	# ---------------------------------------------------------- IMPLEMENTADOS
	"01-pyra": {
		"nome": "Pyra",
		"implementado": true,
		"tatica_cd": 9.0,           # teto da faixa: a Muralha AGRIDE (dano+area)
		"suprema_carga": 50.0,      # dano puro carrega devagar (DIRECAO.md §4)
		"telegrafia": 1.4,          # rugido + brilho antes do Braco Livre
		## Coracao de Fornalha: fogo NO CHAO nao a fere e reacende o braco.
		"passiva": {
			"buff_vel": 1.10,       # +10% (GDD §3)
			"buff_dur": 2.0,        # s ao atravessar chamas
		},
		## Muralha de Brasas: linha de fogo baixo de 8m por 5s.
		"tatica": {
			"comprimento": 8.0,     # m (GDD §3)
			"duracao": 5.0,         # s
			"espessura": 1.0,       # m de meia-largura da faixa que queima
			"dano": 18.0,           # "dano moderado" por travessia — KNOB
			"rearme": 1.0,          # s ate a mesma pessoa poder levar de novo
			"aceso_dur": 2.0,       # s de rastro visivel em quem atravessou
			## ⚖️ agua/gelo APAGAM a muralha e molham o braco:
			"molhado_cd_extra": 1.0,  # +1s na recarga da tatica (GDD §3)
			"apaga_raio": 3.0,        # m do impacto de agua que apaga
			## ⚖️ vento EMPURRA a muralha 3m (§14):
			"vento_empurra": 3.0,
			"vento_raio": 4.0,
		},
		## Braco Livre: 6s de leque continuo + dash que deixa fogo.
		"suprema": {
			"duracao": 6.0,
			"leque_tiros": 3,
			"leque_graus": 17.0,    # abertura de cada lado — "leque CURTO"
			"cadencia": 0.16,       # s entre rajadas — KNOB
			"fogo_dash_raio": 1.6,  # m da poca que o dash deixa
			"fogo_dash_dur": 3.0,   # s
			"fogo_dash_dano": 8.0,
			## ⚖️ ao acabar, o braco ESFRIA (GDD §3):
			"esfria_dur": 4.0,      # s sem tatica
			"esfria_vel": 0.85,     # −15% de velocidade
		},
	},
	"03-veu": {
		"nome": "Véu",
		"implementado": true,
		"tatica_cd": 6.0,           # piso da faixa: Atravessar e' mobilidade pura
		"suprema_carga": 40.0,      # utilidade de grupo enche mais rapido
		"telegrafia": 1.6,          # o SINO espectral (counter sonoro, GDD §3)
		## Entrelinha: desfoca depois de 4s parada de briga.
		"passiva": {
			"quietude": 4.0,        # s sem atacar/tomar dano
			"dist_desfoque": 20.0,  # m — so' desfoca para quem olha de longe
		},
		## Atravessar: 0,8s de ativacao + 1,5s no plano espectral.
		"tatica": {
			"ativacao": 0.8,        # s (GDD §3) — o aviso da tatica
			"duracao": 1.5,         # s intangivel/invulneravel
			"buff_vel": 1.35,       # "mais rapida"
			"parede_max": 2.0,      # m de parede fina que ela atravessa
			## ⚖️ o preco:
			"eco_dur": 2.0,         # s do eco visivel deixado na ENTRADA
			"silencio_saida": 1.0,  # s sem conjurar ao sair (GDD §3)
		},
		## Mare Espectral: 5s de plano espectral para o grupo.
		"suprema": {
			"duracao": 5.0,
			"raio": 6.0,            # m no cast (quem entra junto)
			"buff_vel": 1.25,
			## ⚖️ NINGUEM conjura na Mare, e o sino toca no mundo real:
			"silencio": 5.0,        # = duracao inteira
			"sino": true,
		},
	},
	"10-tessa": {
		"nome": "Tessa",
		"implementado": true,
		"tatica_cd": 7.0,           # baixo de proposito: ela TECE varios fios
		"suprema_carga": 45.0,      # defesa que absorve projeteis: meio da regua
		"telegrafia": 1.8,          # o tear rune girando antes de abrir
		## Compasso Runico: o marca-passo regenera escudo. A CAPACIDADE nao mora
		## aqui: o escudo evolutivo e' do kernel (Balance.ESCUDO / shield_max).
		"passiva": {
			"escudo_regen": 4.0,    # por s — "lentamente"
			"quebra_buff_vel": 1.15,  # +15% quando o escudo QUEBRA
			"quebra_buff_dur": 2.0,   # s
		},
		## Fio do Tear: fio de raio entre 2 pontos.
		"tatica": {
			"comprimento": 6.0,     # m entre as duas ancoras
			"duracao": 20.0,        # s de vida do fio
			"max_fios": 6,          # o 7o apaga o mais velho (GDD §3)
			"raio_toque": 0.8,      # m do fio que conta como toque
			"dano": 14.0,
			"rearme": 0.8,          # s entre dois toques do mesmo alvo
			"lentidao": 0.65,       # fator de velocidade
			"lentidao_dur": 1.5,    # s
			"revela_dur": 4.0,      # s
			## ⚖️ o preco:
			"zumbido_dist": 5.0,    # m em que o fio se denuncia (som+brilho)
			"ancora_raio": 1.5,     # m: UM golpe perto da ancora derruba o fio
		},
		## Tear-Mae: absorve projetil inimigo e TECE escudo.
		"suprema": {
			"duracao": 8.0,
			"raio": 6.0,            # m de alcance da absorcao
			"escudo_por_projetil": 12.0,
			"raio_aliado": 8.0,     # m para quem o escudo tecido vai
			## ⚖️ o preco:
			"zona_morta": 2.0,      # m: NAO absorve curtissimo alcance/corpo a corpo
			"max_simultaneo": 1,    # so' 1 tear por vez
		},
	},
	# -------------------------------------------- DECLARADOS, NAO IMPLEMENTADOS
	# Ficha completa (texto) em menu/Elenco.gd e GDD §3. O sistema roda sem
	# eles: KitRunner sem implementacao = mago sem tatica/suprema, e a HUD
	# recebe kit_bound(slug, false) para desenhar o botao apagado.
	"02-ceifadora": {"nome": "Ceifadora"},
	"04-corvus": {"nome": "Corvus"},
	"05-corvomante": {"nome": "Corvomante"},
	"06-olho-de-eter": {"nome": "Olho-de-Éter"},
	"07-vitalis": {"nome": "Vitalis"},
	"08-ilusionista": {"nome": "Ilusionista"},
	"09-vex": {"nome": "Vex"},
	"11-aelion": {"nome": "Aelion"},
	"12-umbra": {"nome": "Umbra"},
	"13-brok": {"nome": "Brok"},
	"14-gromm": {"nome": "Gromm"},
	"15-maris": {"nome": "Maris"},
	"16-fizz": {"nome": "Fizz"},
	"17-sylva": {"nome": "Sylva"},
	"18-basalto": {"nome": "Basalto"},
	"19-noctus": {"nome": "Noctus"},
	"20-pip": {"nome": "Pip"},
}


## Ficha COMPLETA de um mago (PADRAO + o que ele declara). Slug desconhecido
## devolve o padrao inerte — fiacao defensiva: kit que nao existe nao quebra
## partida, so' nao tem habilidade.
static func de(slug: String) -> Dictionary:
	var d: Dictionary = PADRAO.duplicate(true)
	d.merge(MAGOS.get(slug, {}), true)
	return d


static func implementado(slug: String) -> bool:
	return bool(de(slug).implementado)


static func slugs() -> Array:
	return MAGOS.keys()
