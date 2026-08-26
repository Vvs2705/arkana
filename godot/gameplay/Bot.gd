## Bot: o mesmo Mage com tint, IA minima — vagar -> perseguir -> atacar com o
## MESMO projetil do player. Cada bot nasce com UM elemento fixo (cor+forma do
## projetil o distinguem de longe) e esquiva de vez em quando. Morre com feedback.
##
## PERCEPCAO E FFA (26/08, ordem do Diretor). Ate' hoje Main.gd cravava
## `b.target = player` no nascimento: os 6 sabiam onde o jogador estava desde o
## primeiro quadro, mesmo ele parado — "mesmo sem se mexer eles ja' me notam",
## nas palavras dele. Nao era IA agressiva; era ausencia de percepcao. Agora:
##   - FFA: TODOS contra todos. O alvo e' qualquer mago (grupo "magos"), nunca
##     "o player" por definicao.
##   - Ninguem nasce com alvo. O bot NOTA por quatro canais, cada um com
##     contra-jogada legivel:
##       VISTO    a < VISAO (12 m): perto assim nao ha' como nao ver.
##       OUVIDO   a < AUDICAO_PASSOS (18 m) SE o outro esta' EM MOVIMENTO —
##                passos denunciam; ficar parado esconde (disciplina paga).
##       DISPARO  a < AUDICAO_DISPARO (30 m): conjurar denuncia (Bus.disparo).
##       REVIDE   tomar dano ensina QUEM bateu (Bus.damage_applied) e o bot vai
##                atras da origem mesmo sem linha de visao.
##   - Alvo morre ou sai do ALCANCE DE MEMORIA -> o bot esquece e volta a vagar.
class_name Bot
extends Pawn

const VISAO := 12.0           # m — parado, so' e' notado a esta distancia
const AUDICAO_PASSOS := 18.0  # m — em movimento, os passos entregam
const AUDICAO_DISPARO := 30.0 # m — conjurar entrega mais longe ainda
const MEMORIA := 30.0         # m — alem disto o bot perde o rastro e esquece
const PERCEPCAO_S := 0.5      # s entre varreduras — 7 bots a 2 Hz custa nada
const CHASE_DIST := 20.0
const ATTACK_DIST := 12.0
const AIM_SPREAD := 0.12     # rad — KNOB: erro de mira do bot
const FIRE_RATE_MULT := 4.0  # KNOB: cadencia do bot vs player (calibrar no aparelho)
const DODGE_CHANCE := 0.3    # KNOB: chance por rolagem de esquivar em combate
const DODGE_ROLL_S := 1.4    # s entre rolagens — KNOB

var target: Pawn
var _wander_to := Vector3.ZERO
var _percebe_acc := 0.0
var _repick := 0.0
var _fire_cd := 1.5
var _cast := 0.0
var _dodge_roll := 0.0


func _ready() -> void:
	super()
	speed_factor = 0.8  # ponytail: bot mais lento que player; playtest calibra
	element = Balance.ELEMENTS[randi() % Balance.ELEMENTS.size()]  # fixo por bot
	# Os OUVIDOS do bot. Sinal desconectado sozinho quando o bot morre de vez
	# (queue_free) — nada a limpar na mao.
	Bus.disparo.connect(_ouvir_disparo)
	Bus.damage_applied.connect(_revidar)


func _physics_process(delta: float) -> void:
	apply_gravity(delta)
	_fire_cd = maxf(_fire_cd - delta, 0.0)
	_cast = maxf(_cast - delta, 0.0)
	_repick -= delta
	_percebe_acc += delta
	if _percebe_acc >= PERCEPCAO_S:
		_percebe_acc = 0.0
		_percebe()
	var alive_target := is_instance_valid(target) and target.hp > 0.0
	var dist := INF
	if alive_target:
		dist = global_position.distance_to(target.global_position)
	var dir := Vector3.ZERO
	if alive_target and dist < ATTACK_DIST:
		face_dir(_flat(target.global_position - global_position), delta)
		# Atordoado (conducao: raio em quem esta' molhado) nao conjura. O corpo
		# ja' e' travado pelo Pawn; o disparo tem que ser travado aqui.
		if _fire_cd <= 0.0 and stun_left <= 0.0 and Derrubado.pode_agir(self):
			_shoot()
	elif alive_target and dist < CHASE_DIST:
		dir = _flat(target.global_position - global_position)
	else:
		if _repick <= 0.0 or global_position.distance_to(_wander_to) < 1.5:
			_repick = randf_range(2.0, 5.0)
			_wander_to = global_position + Vector3(randf_range(-12, 12), 0, randf_range(-12, 12))
			# DESARMADO, a prioridade e' ACHAR LUVA (DIRECAO.md §7: os bots nas
			# leis do jogador). O vagar aponta para o loot mais perto; o
			# auto-upgrade do proprio loot equipa quando ele chega em cima.
			if not ArmaSlot.armado_de(self):
				var alvo_loot := _loot_mais_perto()
				if alvo_loot != null:
					_wander_to = alvo_loot.global_position
					_repick = 6.0
		dir = _flat(_wander_to - global_position)
	if alive_target and dist < CHASE_DIST:
		_maybe_dodge(delta)
	move_velocity(dir, delta)  # produto unico + dash + knockback (caminho do Pawn)
	move_and_slide()
	face_dir(dir, delta, 8.0)
	anim("cast" if _cast > 0.0 else locomotion_anim())  # pela velocidade REAL


## A varredura dos sentidos. So' roda a PERCEPCAO_S — e so' procura alvo NOVO
## quando esta' sem nenhum: bot que ja' cacava alguem nao fica trocando de
## presa a cada passante (senao vira pinball entre alvos).
func _percebe() -> void:
	if target != null and (not is_instance_valid(target) or target.hp <= 0.0
			or global_position.distance_to(target.global_position) > MEMORIA):
		target = null
	if target != null:
		return
	var melhor: Pawn = null
	var melhor_d := INF
	for n in get_tree().get_nodes_in_group("magos"):
		if n == self or not (n is Pawn):
			continue
		var p := n as Pawn
		if p.hp <= 0.0 or not is_instance_valid(p):
			continue
		var d := global_position.distance_to(p.global_position)
		if d >= melhor_d:
			continue
		var visto := d < VISAO
		var ouvido := d < AUDICAO_PASSOS and p.horizontal_speed() > 1.0
		if visto or ouvido:
			melhor = p
			melhor_d = d
	if melhor != null:
		target = melhor


## Conjurou perto = entregou a posicao. So' LARGA o alvo atual se o disparo
## veio de MAIS PERTO — um tiro longe nao rouba a atencao de uma briga em curso.
func _ouvir_disparo(quem: Node, pos: Vector3) -> void:
	if quem == self or hp <= 0.0 or not (quem is Pawn):
		return
	var d := global_position.distance_to(pos)
	if d >= AUDICAO_DISPARO:
		return
	if target != null and is_instance_valid(target) and target.hp > 0.0 			and d >= global_position.distance_to(target.global_position):
		return
	target = quem as Pawn
	ir_para(pos)  # anda ate' onde ouviu, mesmo sem ver ninguem


## Tomou dano = aprendeu quem bateu. Revida mesmo alem da audicao: levar tiro
## ensina a direcao — e' assim em todo jogo de acao, e e' a ordem do Diretor.
func _revidar(alvo: Node, _amount: float, _el: String, fonte: Node, _esc: bool) -> void:
	if alvo != self or hp <= 0.0 or fonte == self or not (fonte is Pawn):
		return
	if not is_instance_valid(fonte):
		return
	target = fonte as Pawn
	ir_para((fonte as Pawn).global_position)


## A luva mais proxima no chao (grupo "loot_arma"). Sem teto de distancia:
## um bot desarmado atravessa o mapa por uma arma — e' o que um jogador faria.
func _loot_mais_perto() -> Node3D:
	var melhor: Node3D = null
	var d2 := INF
	for n in get_tree().get_nodes_in_group("loot_arma"):
		if not (n is Node3D) or not is_instance_valid(n):
			continue
		var dd := global_position.distance_squared_to((n as Node3D).global_position)
		if dd < d2:
			d2 = dd
			melhor = n
	return melhor


func _flat(v: Vector3) -> Vector3:
	v.y = 0.0
	if v.length_squared() < 0.0001:
		return Vector3.ZERO
	return v.normalized()


## Esquiva ocasional: rolagem por DODGE_ROLL_S; se passar, dash lateral.
func _maybe_dodge(delta: float) -> void:
	_dodge_roll -= delta
	if _dodge_roll > 0.0:
		return
	_dodge_roll = DODGE_ROLL_S
	if randf() < DODGE_CHANCE:
		var side := _flat(target.global_position - global_position).cross(Vector3.UP)
		try_dodge(side * (1.0 if randf() < 0.5 else -1.0))


func _shoot() -> void:
	# A Lei das Luvas vale para o bot IGUAL ("todas as logicas do jogo
	# precisam ser aplicadas aos bots" — o Diretor, 26/08).
	if not ArmaSlot.armado_de(self):
		return
	# O elemento e' o da LUVA equipada (DIRECAO.md §1) — o sorteado no _ready
	# virou so' fallback de pawn sem slot.
	var slot := ArmaSlot.de(self)
	var el: String = element if slot == null else slot.elemento_do_disparo(element)
	_fire_cd = float(ArmaSlot.spec_de(self, el).fire_rate) * FIRE_RATE_MULT
	_cast = 0.3
	var from := global_position + Vector3(0, 1.4, 0)
	var to := target.global_position + Vector3(0, 1.2, 0)
	var dir := (to - from).normalized().rotated(Vector3.UP, randf_range(-AIM_SPREAD, AIM_SPREAD))
	Projectile.launch(get_parent(), self, from + dir * 0.9, dir, el)
	if visual != null and visual.has_method("play_anim"):
		visual.play_anim("cast")  # retrigger por tiro — mesmo motivo do Player
	anim("cast")


func die() -> void:
	set_physics_process(false)
	anim("idle")
	if is_inside_tree():
		Projectile.burst(get_parent(), global_position + Vector3(0, 1.2, 0), Color(0.9, 0.3, 0.9), 24)
		var tw := create_tween()
		tw.tween_property(visual, "scale", Vector3.ONE * 0.05, 0.45)
		tw.tween_callback(queue_free)
	else:
		queue_free()

## Destino imposto de FORA (zona fechando, Bau Celestial caindo). Sobrepoe o
## vagar e segura o repique, senao o bot sorteia outro destino em 2-5s e volta
## a morrer na tempestade. Pedido por DUAS raias (Zona e BauCelestial), que ja'
## chamam isto de forma defensiva com has_method().
func ir_para(pos: Vector3) -> void:
	_wander_to = pos
	_repick = 4.0
