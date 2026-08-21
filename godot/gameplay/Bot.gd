## Bot: o mesmo Mage com tint, IA minima — vagar -> perseguir (<20m) ->
## atacar (<12m) com o MESMO projetil do player. Cada bot nasce com UM elemento
## fixo (cor+forma do projetil o distinguem de longe) e esquiva de vez em
## quando. Morre com feedback.
class_name Bot
extends Pawn

const CHASE_DIST := 20.0
const ATTACK_DIST := 12.0
const AIM_SPREAD := 0.12     # rad — KNOB: erro de mira do bot
const FIRE_RATE_MULT := 4.0  # KNOB: cadencia do bot vs player (calibrar no aparelho)
const DODGE_CHANCE := 0.3    # KNOB: chance por rolagem de esquivar em combate
const DODGE_ROLL_S := 1.4    # s entre rolagens — KNOB

var target: Pawn
var _wander_to := Vector3.ZERO
var _repick := 0.0
var _fire_cd := 1.5
var _cast := 0.0
var _dodge_roll := 0.0


func _ready() -> void:
	super()
	speed_factor = 0.8  # ponytail: bot mais lento que player; playtest calibra
	element = Balance.ELEMENTS[randi() % Balance.ELEMENTS.size()]  # fixo por bot


func _physics_process(delta: float) -> void:
	apply_gravity(delta)
	_fire_cd = maxf(_fire_cd - delta, 0.0)
	_cast = maxf(_cast - delta, 0.0)
	_repick -= delta
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
		dir = _flat(_wander_to - global_position)
	if alive_target and dist < CHASE_DIST:
		_maybe_dodge(delta)
	move_velocity(dir, delta)  # produto unico + dash + knockback (caminho do Pawn)
	move_and_slide()
	face_dir(dir, delta, 8.0)
	anim("cast" if _cast > 0.0 else locomotion_anim())  # pela velocidade REAL


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
	_fire_cd = float(ArmaSlot.spec_de(self, element).fire_rate) * FIRE_RATE_MULT
	_cast = 0.3
	var from := global_position + Vector3(0, 1.4, 0)
	var to := target.global_position + Vector3(0, 1.2, 0)
	var dir := (to - from).normalized().rotated(Vector3.UP, randf_range(-AIM_SPREAD, AIM_SPREAD))
	Projectile.launch(get_parent(), self, from + dir * 0.9, dir, element)
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
