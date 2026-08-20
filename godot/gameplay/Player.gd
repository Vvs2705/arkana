## Player: CharacterBody3D + camera 3a pessoa sobre o ombro (SpringArm com
## colisao). A HUD alimenta move_input/add_look/request_fire — o player nao
## le Input direto (toque e' territorio da UI).
class_name Player
extends Pawn

var mana: float = float(Balance.PLAYER.mana_max)
var move_input := Vector2.ZERO   # joystick: x direita, y baixo
var look_sens := 0.008           # rad por px de canvas — KNOB (calibrar no aparelho)
var cam_yaw: Node3D
var cam_pitch: Node3D
var cam: Camera3D
var _fire_cd := 0.0
var _cast := 0.0
var _want_fire := false
var _aiming := false


func _ready() -> void:
	super()
	add_to_group("player")
	_build_camera()


func _build_camera() -> void:
	cam_yaw = Node3D.new()
	cam_pitch = Node3D.new()
	var arm := SpringArm3D.new()
	arm.spring_length = 2.6
	arm.position = Vector3(0.55, 0.0, 0.0)  # sobre o ombro direito (regua Spellbreak)
	arm.add_excluded_object(get_rid())
	cam = Camera3D.new()
	cam.current = true
	arm.add_child(cam)
	cam_pitch.add_child(arm)
	cam_pitch.rotation.x = -0.15
	cam_yaw.add_child(cam_pitch)
	add_child(cam_yaw)
	cam_yaw.top_level = true  # a camera NAO herda o giro do corpo


func set_aiming(on: bool) -> void:
	_aiming = on


func add_look(rel: Vector2) -> void:
	if hp <= 0.0:
		return
	cam_yaw.rotation.y -= rel.x * look_sens
	cam_pitch.rotation.x = clampf(cam_pitch.rotation.x - rel.y * look_sens, -1.1, 0.5)


func request_fire() -> void:
	_want_fire = true  # consumido no _physics_process (raycast precisa do passo de fisica)


func _physics_process(delta: float) -> void:
	cam_yaw.global_position = global_position + Vector3(0, 1.5, 0)
	_fire_cd = maxf(_fire_cd - delta, 0.0)
	_cast = maxf(_cast - delta, 0.0)
	if mana < float(Balance.PLAYER.mana_max):
		mana = minf(mana + float(Balance.PLAYER.mana_regen) * delta, float(Balance.PLAYER.mana_max))
		Bus.mana_changed.emit(mana, float(Balance.PLAYER.mana_max))
	apply_gravity(delta)
	var dir := _move_dir()
	velocity.x = dir.x * current_speed()
	velocity.z = dir.z * current_speed()
	move_and_slide()
	if _aiming:
		rotation.y = lerp_angle(rotation.y, cam_yaw.rotation.y, 12.0 * delta)
	else:
		face_dir(dir, delta)
	if _want_fire:
		_want_fire = false
		_try_fire()
	anim("cast" if _cast > 0.0 else ("run" if dir.length_squared() > 0.01 else "idle"))


func _move_dir() -> Vector3:
	if move_input.length_squared() < 0.01:
		return Vector3.ZERO
	var f := -cam_yaw.global_transform.basis.z
	f.y = 0.0
	f = f.normalized()
	var r := cam_yaw.global_transform.basis.x
	r.y = 0.0
	r = r.normalized()
	return (f * -move_input.y + r * move_input.x).limit_length(1.0)


func _try_fire() -> void:
	if _fire_cd > 0.0 or mana < float(Balance.FIRE.mana_cost):
		return
	mana -= float(Balance.FIRE.mana_cost)
	Bus.mana_changed.emit(mana, float(Balance.PLAYER.mana_max))
	_fire_cd = float(Balance.FIRE.fire_rate)
	_cast = 0.3
	rotation.y = cam_yaw.rotation.y  # o personagem gira para a mira ao disparar
	var from := global_position + Vector3(0, 1.4, 0)
	var aim_dir := (_aim_point() - from).normalized()
	Projectile.launch(get_parent(), self, from + aim_dir * 0.9, aim_dir)
	anim("cast")


## Ponto que o reticulo (centro da camera) esta' olhando.
func _aim_point() -> Vector3:
	var cam_from := cam.global_position
	var cam_dir := -cam.global_transform.basis.z
	var to := cam_from + cam_dir * float(Balance.FIRE.range)
	var q := PhysicsRayQueryParameters3D.create(cam_from, to)
	q.exclude = [get_rid()]
	var hit := get_world_3d().direct_space_state.intersect_ray(q)
	if hit.is_empty():
		return to
	return hit["position"]


func die() -> void:
	move_input = Vector2.ZERO
	anim("idle")
	set_tint(Color(0.25, 0.25, 0.3))
	set_physics_process(false)
