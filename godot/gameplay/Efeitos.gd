## EFEITOS SECUNDARIOS E REACOES ELEMENTAIS **NO ALVO** (GDD §4.6, §14, §16.4 —
## docs/DANO.md §3.3/§3.4).
##
## O que este arquivo conserta: ate' aqui o pedra-papel-tesoura dos elementos
## existia SO' no terreno (TerrainSystem). Agua num inimigo em chamas nao fazia
## nada; raio num inimigo molhado nao fazia nada. Escolher fogo ou gelo nao
## mudava nada para quem levava o tiro — o carrossel era um seletor de cor.
##
## A REGRA QUE IMPEDE ISSO DE VIRAR SOPA: todo estado do alvo e' EXCLUSIVO por
## categoria — UM termico (queimando OU molhado, nunca os dois) e UM de
## movimento. A reacao sempre SUBSTITUI, nunca soma. Previsivel e' aprendivel.
##
## Fronteira: aqui so' se mexe no ESTADO DO ALVO. Quem muda o MUNDO e' o
## TerrainSystem, por Bus.terrain_hit. Quem aplica dano e' o Combat.
class_name Efeitos


## Resolve o elemento chegando no alvo e devolve o MULTIPLICADOR DE IMPACTO
## (conducao e' +50% no mesmo tiro, entao tem que ser resolvida ANTES do dano).
## `dano` e' o dano cru do projetil — so' o arco de conducao usa.
static func aplicar(alvo: Node, el: String, dano: float, fonte: Node) -> float:
	## So' MAGO tem estado. Muro, arvore, loot e chao nao reagem por aqui — a
	## reacao do MUNDO e' do TerrainSystem, e o 2.0x antiestrutura do terra vive
	## la' (Balance.EARTH.estrutura), nao aqui.
	if not is_instance_valid(alvo) or not alvo.has_method("molhar"):
		return 1.0
	## i-frames sao imunidade TOTAL (Balance.DODGE.iframes): quem esquivou nao
	## leva dano NEM estado. Sem esta guarda o dano era barrado no Combat mas a
	## queimadura passava — e esquivar deixaria de ser a jogada que e'.
	if float(alvo.iframes_left) > 0.0 or float(alvo.hp) <= 0.0:
		return 1.0
	var s: Dictionary = Balance.STATUS
	var molhado: bool = float(alvo.wet_left) > 0.0
	var queimando: bool = float(alvo.burn_left) > 0.0
	match el:
		"fire":
			if molhado:
				## VAPOR: fogo em quem esta' molhado NAO acende — evapora a agua
				## e limpa o estado. Nuvem/ofuscamento e' feedback, raia de UI.
				alvo.secar()
			else:
				alvo.acender(float(s.burn_dps), float(s.burn_dur))
		"water":
			## EXTINCAO: a agua apaga a queimadura de graca, de proposito — a
			## contra-jogada tem que ser barata para o fogo poder doer.
			## HIPOTERMIA (GDD §16.4): agua em quem JA' estava molhado dobra a
			## lentidao (-20% em vez de -10%).
			alvo.molhar(float(s.wet_dur),
					float(s.hypothermia_slow) if molhado else float(s.wet_slow))
		"lightning":
			## CONDUCAO: so' em alvo MOLHADO. +50% no impacto, atordoamento
			## curto e um arco para outro molhado perto. E' o pagamento da agua,
			## que sozinha e' o menor dano do jogo.
			if molhado:
				alvo.atordoar(float(s.conduct_stun))
				_arco(alvo, dano * float(s.conduct_mult) * float(s.conduct_arc_mult), fonte)
				return float(s.conduct_mult)
		"wind":
			## ATICAR (triangulo do fogo, GDD §16.4): o vento sopra a brasa.
			## Dois gumes — o vento do INIMIGO tambem te ajuda a queimar.
			if queimando:
				alvo.acender(float(s.burn_fanned_dps),
						float(alvo.burn_left) + float(s.burn_fanned_bonus))
	return 1.0


## UM arco por acerto, para o molhado mais proximo dentro de conduct_arc_m.
## Sem cadeia: encadear atordoamento e dano vira controle infinito, e o teto de
## atordoamento do kernel (0.8s) existe justamente para isso nunca acontecer.
static func _arco(de: Node, dano: float, fonte: Node) -> void:
	var pai := de.get_parent()
	if pai == null or not (de is Node3D):
		return
	var origem: Vector3 = (de as Node3D).global_position
	var r2: float = float(Balance.STATUS.conduct_arc_m) * float(Balance.STATUS.conduct_arc_m)
	var perto: Node = null
	var d2 := r2
	for outro in pai.get_children():
		if outro == de or not (outro is Node3D) or not outro.has_method("molhar"):
			continue
		if float(outro.wet_left) <= 0.0 or float(outro.hp) <= 0.0:
			continue
		var dd: float = (outro as Node3D).global_position.distance_squared_to(origem)
		if dd <= d2:
			d2 = dd
			perto = outro
	if perto != null:
		Combat.deal(perto, dano, "lightning", fonte)
