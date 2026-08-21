## ARMAS ARCANAS — os DADOS (GDD §16.2). Sem nos, sem cena: registro puro +
## contas puras, testavel headless.
##
## A SEPARACAO DO GDD, escrita em codigo:
##   personagem = habilidades (passiva/tatica/suprema)  -> raia PERSONAGEM
##   ELEMENTO   = perfil do feitico (dano/mana/cadencia) -> core/Balance.gd
##   ARMA       = TIER do conjurador (multiplica o perfil) -> este arquivo
##
## Por que MULTIPLICADOR e nao numero absoluto: Balance e' o dono unico dos
## numeros do jogo (regra do projeto) e ja' tem os 5 elementos calibrados na
## regua de TTK. Se a arma trouxesse dano proprio existiriam DUAS fontes de
## verdade e o rebalanceamento viraria caca ao numero. Aqui a arma so' diz
## "quanto do perfil do elemento" — 15 numeros no total, todos KNOB.
##
## A VARINHA E' A LINHA DE BASE (tudo 1.0): "todo mundo cai do ceu com uma"
## (GDD §16.2). Logo, sem arma equipada o jogo se comporta exatamente como
## antes deste sistema existir — zero regressao.
class_name Arma

## Ordem de tier (usada pelo loot para decidir o que e' upgrade).
const TIERS := ["varinha", "cajado", "manopla"]

const ARMAS := {
	"varinha": {
		"nome": "Varinha",
		"raridade": "comum",
		## LINHA DE BASE — nao mexer sem recalibrar TODO o resto.
		"dmg": 1.0,
		"fire_rate": 1.0,        # atraso de conjuracao (MENOR = mais rapido)
		"projectile_speed": 1.0,
		"range": 1.0,
		"mana_cost": 1.0,
		"elementos": 1,          # quantos elementos porta ao mesmo tempo
		"runas": 1,              # slots de runa (GDD §16.3 — estrutura so')
		"suprema_bonus": 1.0,
	},
	"cajado": {
		"nome": "Cajado",
		"raridade": "raro",
		## "alcance e dano maiores, conjuracao mais lenta" (GDD §16.2).
		## KNOB: 1.55 de dano com 1.45 de atraso = ~7% de DPS a mais que a
		## varinha. O cajado NAO ganha por DPS bruto e sim por ALCANCE (1.7x =
		## 68m no fogo) — e' o rifle/sniper da tabela, quem acerta de longe.
		"dmg": 1.55,
		"fire_rate": 1.45,
		"projectile_speed": 1.25,
		"range": 1.7,
		"mana_cost": 1.3,
		"elementos": 1,
		"runas": 2,
		"suprema_bonus": 1.25,   # "canaliza supremas com bonus" — a raia de
		                         # habilidades le' daqui quando as supremas vierem
	},
	"manopla": {
		"nome": "Manopla",
		"raridade": "lendaria",
		## LENDARIA — so' em Baus Celestiais (GDD §16.2). "Estalar de dedos":
		## o atraso de conjuracao cai a 45% (KNOB — 0.27s vira 0.12s no fogo).
		## O LIMITADOR NAO E' O DANO, E' A MANA: ~8,5 de mana a cada 0,12s =
		## ~70/s de dreno contra 14/s de regeneracao. Da' ~1,9s de fogo continuo
		## e depois seca. Arma de RAJADA, nao de sustentacao. Se o playtest
		## disser que e' forte demais, o botao e' fire_rate (0.45 -> 0.60),
		## nunca o dano: e' a cadencia que vende a fantasia.
		"dmg": 1.25,
		## RETUNE 21/08 (estudo docs/DANO.md §2): com 0.45/0.95 a manopla
		## derrubava vida cheia em 0,65s — menos de um TERCO da faixa do GDD §5
		## — e o DPS SUSTENTADO dela (23,7-27,6) ficava ACIMA da varinha, ou
		## seja, era a melhor arma em rajada E em sustentacao. O limitador
		## declarado no comentario acima simplesmente nao existia. Com
		## 0.80/1.45 o TTK vai a 1,40-1,84s e o sustentado cai abaixo da
		## varinha — a fantasia do estalar de dedos e' vendida pela AUSENCIA
		## de tempo de preparo, nao pelo intervalo entre estalos.
		"fire_rate": 0.80,
		"projectile_speed": 1.15,
		"range": 1.15,
		"mana_cost": 1.45,
		"elementos": 2,          # 2 elementos SIMULTANEOS e FIXOS
		"runas": 3,
		"suprema_bonus": 1.1,
	},
}

## Os pares FIXOS da manopla (GDD §16.2: "2 elementos fixos por manopla, sem
## troca — poder alto, flexibilidade menor"). O portador NAO escolhe: a manopla
## que ele achou e' que manda. Determinismo: o loot sorteia o indice com seed.
## ⚠️ NOTA LEGAL REGISTRADA NO GDD: estalar de dedos + luva e' MECANICA (livre).
## O simbolo/desenho da luva do personagem de FMA nunca entra aqui nem na arte.
const PARES_MANOPLA := [
	["fire", "wind"],        # o par do GDD: fogo alimentado por vento
	["water", "lightning"],  # conducao (GDD §14): molha e eletrocuta
	["earth", "fire"],       # muro e brasa
]

## RARIDADE = COR **+ FORMA** (GDD §10 — lei do projeto; daltonico tem que ler
## a raridade sem depender de cor). A forma vale para o marcador no chao E para
## a silhueta do modelo.
const RARIDADES := {
	"comum": {"cor": Color("BFD4E8"), "forma": "circulo", "feixe": 1.9},
	"raro": {"cor": Color("6C8CFF"), "forma": "losango", "feixe": 2.6},
	"lendaria": {"cor": Color("FFC53D"), "forma": "triangulo", "feixe": 3.6},
}


## Perfil CRU do elemento (o que Balance define). Fonte unica — Projectile.spec
## delega para ca'; ninguem mais faz esse match.
static func base(el: String) -> Dictionary:
	match el:
		"water":
			return Balance.WATER
		"lightning":
			return Balance.LIGHTNING
		"earth":
			return Balance.EARTH
		"wind":
			return Balance.WIND
		_:
			return Balance.FIRE


static func existe(arma_id: String) -> bool:
	return ARMAS.has(arma_id)


static func dados(arma_id: String) -> Dictionary:
	return ARMAS.get(arma_id, ARMAS["varinha"])


## Posicao na escada de poder. -1 para id desconhecido.
static func tier(arma_id: String) -> int:
	return TIERS.find(arma_id)


static func raridade(arma_id: String) -> String:
	return str(dados(arma_id).raridade)


static func cor(arma_id: String) -> Color:
	return RARIDADES[raridade(arma_id)].cor


static func forma(arma_id: String) -> String:
	return str(RARIDADES[raridade(arma_id)].forma)


## O NUMERO QUE O DISPARO USA: perfil do elemento x tier da arma.
## Devolve as MESMAS chaves de Balance.FIRE — quem consome (Player, Bot,
## Projectile) nao precisa saber que existe arma.
static func spec(el: String, arma_id := "varinha") -> Dictionary:
	var b := base(el)
	var a := dados(arma_id)
	return {
		"dmg": float(b.dmg) * float(a.dmg),
		"mana_cost": float(b.mana_cost) * float(a.mana_cost),
		"fire_rate": float(b.fire_rate) * float(a.fire_rate),
		"projectile_speed": float(b.projectile_speed) * float(a.projectile_speed),
		"range": float(b.range) * float(a.range),
	}


## Elementos que a arma porta. Varinha/cajado: o que o carrossel escolheu.
## Manopla: o par FIXO dela, o carrossel nao manda.
static func elementos_de(arma_id: String, par: PackedStringArray,
		escolhido: String) -> PackedStringArray:
	if int(dados(arma_id).elementos) > 1 and par.size() >= 2:
		return par
	return PackedStringArray([escolhido])
