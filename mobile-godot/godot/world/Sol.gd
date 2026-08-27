## O SOL DA ILHA — e o alcance da sombra, que deixou de ser um numero fixo.
##
## O PROBLEMA MEDIDO (26/08): `directional_shadow_max_distance` valia 60 m. Esse
## numero foi calibrado quando a ilha tinha 180 m e o jogador nascia no chao.
## Depois disso a ilha virou 300 m e a partida passou a COMECAR NO AR, a ~200 m
## de altura. Resultado: durante a queda inteira — a fase mais bonita da partida,
## e a primeira coisa que o jogador ve' — TODA a ilha esta' alem dos 60 m e nao
## projeta uma sombra. A mata, o pico e as ruinas leem como mancha de cor chapada
## vista de cima. Fotografado com world/_shot.gd e comparado lado a lado.
##
## POR QUE NAO E' SO' AUMENTAR O NUMERO: o atlas de sombra tem 2048 px e cobre o
## alcance inteiro. A 60 m o texel sai ~6 cm — bom para o pe' do mago e o tronco
## ao lado. A 400 m sai ~20 cm, e a sombra PERTO, onde o jogador passa 95% da
## partida, vira borrao. Trocar o chao pelo ceu seria pagar o quadro bonito com o
## quadro que importa.
##
## POR QUE NAO E' PSSM: dois ou mais splits redesenham os casters uma vez POR
## SPLIT, todo frame. A raia MUNDO ja' mediu e recusou isso no ARM64, e a guarda
## em world/selftest.gd continua barrando. Nada aqui reabre essa decisao.
##
## O QUE ESTE SCRIPT FAZ: um split so', com o alcance seguindo a ALTURA do
## jogador. No ar o alcance abre e cobre a ilha; ao pousar, fecha em 60 m e o
## texel volta a 6 cm. Custa uma conta por quadro de altimetro (4 Hz) e nenhum
## draw call novo. A altura ja' chega de graca: Bus.queda_altura existe desde a
## queda, emitida SO' pelo jogador (Queda._altimetro, guardada por `e_player`).
##
## Trocar altura por alcance e' uma FUNCAO PURA (`alcance_para`) de proposito: o
## selftest prova a regra sem precisar de partida, de Bus e de aparelho.
extends DirectionalLight3D

## Alcance em repouso, com o mago no chao. E' o valor que a guarda de orcamento
## do selftest cobra (<= 80 m) — o teto de 80 continua valendo para o JOGO A PE'.
const PERTO := 60.0

## Teto no ar. 380 m cobre a ilha de 300 m em diagonal com folga; passar disso
## so' aumentaria o texel sem ninguem ver mais sombra.
const LONGE := 380.0

## Metros de alcance por metro de altura. Com 2.0, a 190 m de queda o alcance ja'
## bate o teto — a sombra esta' inteira antes de o jogador comecar a escolher
## onde pousar, que e' quando ele de fato olha para o mapa.
const POR_METRO := 2.0


func _ready() -> void:
	directional_shadow_max_distance = PERTO
	# FIACAO DEFENSIVA (regra do projeto): a ilha boota sozinha, sem partida e
	# sem autoload. Em --script os autoloads so' registram depois deste arquivo
	# compilar, por isso o Bus vem por get_node e nunca pelo identificador.
	var bus: Node = get_tree().root.get_node_or_null("Bus")
	if bus == null:
		return
	bus.queda_altura.connect(_ao_mudar_altura)
	bus.queda_fase.connect(_ao_mudar_fase)


## Altura acima do solo -> alcance de sombra. Pura: sem estado, sem no', sem Bus.
static func alcance_para(metros: float) -> float:
	# `not (x > 0)` barra NaN, zero e negativo de uma vez — mesmo idioma de
	# Combat.deal. Sem isso um NaN vindo do altimetro passaria pelo clamp e
	# deixaria o alcance da sombra indefinido pelo resto da partida.
	if not (metros > 0.0):
		return PERTO
	return clampf(metros * POR_METRO, PERTO, LONGE)


func _ao_mudar_altura(metros: float, _velocidade: float) -> void:
	directional_shadow_max_distance = alcance_para(metros)


func _ao_mudar_fase(fase: String) -> void:
	# Pousou = o jogador vai andar. Fecha o alcance mesmo que o ultimo altimetro
	# tenha chegado com altura residual: quem manda no chao e' a fase, nao a
	# ultima amostra.
	if fase == "pousou":
		directional_shadow_max_distance = PERTO
