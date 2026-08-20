## Bot: o mesmo Mage com tint, IA minima — vagar -> perseguir (<20m) ->
## atacar (<12m) com o MESMO projetil do player. Morre com feedback.
class_name Bot
extends Pawn

const CHASE_DIST := 20.0
const ATTACK_DIST := 12.0
const AIM_SPREAD := 0.12     # rad — KNOB: erro de mira do bot
const FIRE_RATE_MULT := 4.0  # KNOB: cadencia do bot vs player (calibrar no aparelho)

var target: Pawn
var _wander_to := Vector3.ZERO
var _repick := 0.0
var _fire_cd := 1.5
var _cast := 0.0


func _ready() -> void:
	super()
	speed_factor = 0.8  # ponytail: bot mais lento que player; playtest calibra


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
		if _fire_cd <= 0.0:
			_shoot()
	elif alive_target and dist < CHASE_DIST:
		dir = _flat(target.global_position - global_position)
	else:
		if _repick <= 0.0 or global_position.distance_to(_wander_to) < 1.5:
			_repick = randf_range(2.0, 5.0)
			_wander_to = global_position + Vector3(randf_range(-12, 12), 0, randf_range(-12, 12))
		dir = _flat(_wander_to - global_position)
	velocity.x = dir.x * current_speed()
	velocity.z = dir.z * current_speed()
	move_and_slide()
	face_dir(dir, delta, 8.0)
	anim("cast" if _cast > 0.0 else ("run" if dir.length_squared() > 0.01 else "idle"))


func _flat(v: Vector3) -> Vector3:
	v.y = 0.0
	if v.length_squared() < 0.0001:
		return Vector3.ZERO
	return v.normalized()


func _shoot() -> void:
	_fire_cd = float(Balance.FIRE.fire_rate) * FIRE_RATE_MULT
	_cast = 0.3
	var from := global_position + Vector3(0, 1.4, 0)
	var to := target.global_position + Vector3(0, 1.2, 0)
	var dir := (to - from).normalized().rotated(Vector3.UP, randf_range(-AIM_SPREAD, AIM_SPREAD))
	Projectile.launch(get_parent(), self, from + dir * 0.9, dir)
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
