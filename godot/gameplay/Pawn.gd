## Base de player e bots: hp, visual (Mage.tscn com FALLBACK — fiacao
## defensiva obrigatoria) e a VELOCIDADE como produto unico.
class_name Pawn
extends CharacterBody3D

const MAGE_SCENE := "res://characters/Mage.tscn"
const KNOCK_DECAY := 9.0  # m/s^2 — KNOB local; pedir entrada em Balance se sobreviver ao playtest

var hp: float = float(Balance.PLAYER.hp)
var element := "fire"    # padrao do carrossel (Balance.ELEMENTS)
var speed_factor := 1.0  # bots reduzem aqui, nunca no Balance
var terrain_mult := 1.0  # G2 (gelo/agua) escreve aqui
var status_mult := 1.0   # lentidao/buff escrevem aqui
var iframes_left := 0.0  # esquiva: Combat.deal barra dano enquanto > 0
var knockback := Vector3.ZERO
var visual: Node3D
var _dodge_left := 0.0
var _dodge_cd := 0.0
var _dodge_dir := Vector3.ZERO
var _cur_anim := ""
var _gravity: float = float(ProjectSettings.get_setting("physics/3d/default_gravity", 9.8))


## Velocidade e' PRODUTO UNICO (regra provada) — ninguem escreve m/s direto.
func current_speed() -> float:
	return float(Balance.PLAYER.speed) * terrain_mult * status_mult * speed_factor


## Caminho UNICO da velocidade horizontal: produto unico + dash + knockback.
## Player e Bot passam por aqui — ninguem escreve velocity.x/z por fora.
func move_velocity(dir: Vector3, delta: float) -> void:
	_dodge_cd = maxf(_dodge_cd - delta, 0.0)
	iframes_left = maxf(iframes_left - delta, 0.0)
	knockback = knockback.move_toward(Vector3.ZERO, KNOCK_DECAY * delta)
	if _dodge_left > 0.0:
		_dodge_left -= delta
		var v := float(Balance.DODGE.distance) / float(Balance.DODGE.duration)
		velocity.x = _dodge_dir.x * v
		velocity.z = _dodge_dir.z * v
		return
	velocity.x = dir.x * current_speed() + knockback.x
	velocity.z = dir.z * current_speed() + knockback.z


func dodge_ready() -> bool:
	return _dodge_cd <= 0.0 and hp > 0.0


## Fracao 0..1 do cooldown de esquiva (1 = acabou de usar). A UI so' LE.
func dodge_cd_frac() -> float:
	return _dodge_cd / float(Balance.DODGE.cooldown)


## Dash na direcao do movimento (parado: para onde olha) com i-frames curtos.
## Todos os numeros vem de Balance.DODGE.
func try_dodge(dir: Vector3) -> bool:
	if not dodge_ready():
		return false
	dir.y = 0.0
	if dir.length_squared() < 0.0001:
		dir = -global_transform.basis.z
		dir.y = 0.0
	_dodge_dir = dir.normalized()
	_dodge_left = float(Balance.DODGE.duration)
	_dodge_cd = float(Balance.DODGE.cooldown)
	iframes_left = float(Balance.DODGE.iframes)
	return true


## Empurrao transitorio (feedback de acerto) — decai sozinho em move_velocity.
func apply_knockback(v: Vector3) -> void:
	knockback += v


func _ready() -> void:
	var col := CollisionShape3D.new()
	var capsule := CapsuleShape3D.new()
	capsule.height = 1.8
	capsule.radius = 0.35
	col.shape = capsule
	col.position.y = 0.9
	add_child(col)
	visual = _build_visual()
	add_child(visual)


func _build_visual() -> Node3D:
	# A raia PERSONAGEM pode terminar depois — esta cena BOOTA de qualquer jeito.
	if ResourceLoader.exists(MAGE_SCENE):
		var packed: Variant = load(MAGE_SCENE)
		if packed is PackedScene:
			var v: Node = (packed as PackedScene).instantiate()
			if v is Node3D:
				return v
			v.free()
	return _fallback_mage()


func _fallback_mage() -> Node3D:
	var root := Node3D.new()
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.45, 0.35, 0.75)
	var body := MeshInstance3D.new()
	var cap := CapsuleMesh.new()
	cap.height = 1.2
	cap.radius = 0.3
	body.mesh = cap
	body.position.y = 1.2
	body.material_override = mat
	var manto := MeshInstance3D.new()
	var cyl := CylinderMesh.new()
	cyl.top_radius = 0.25
	cyl.bottom_radius = 0.55
	cyl.height = 1.0
	manto.mesh = cyl
	manto.position.y = 0.5
	manto.material_override = mat
	root.add_child(body)
	root.add_child(manto)
	root.set_meta("fallback_mat", mat)
	return root


func anim(anim_name: String) -> void:
	if anim_name == _cur_anim:
		return
	_cur_anim = anim_name
	if visual and visual.has_method("play_anim"):
		visual.play_anim(anim_name)


func set_tint(c: Color) -> void:
	if visual == null:
		return
	if visual.has_method("set_tint"):
		visual.set_tint(c)
	elif visual.has_meta("fallback_mat"):
		(visual.get_meta("fallback_mat") as StandardMaterial3D).albedo_color = c


func apply_gravity(delta: float) -> void:
	if not is_on_floor():
		velocity.y -= _gravity * delta


## Vira o corpo para a direcao dir (XZ). Forward do node e' -Z.
func face_dir(dir: Vector3, delta: float, turn_speed := 10.0) -> void:
	if dir.length_squared() < 0.0001:
		return
	rotation.y = lerp_angle(rotation.y, atan2(-dir.x, -dir.z), turn_speed * delta)


func die() -> void:
	pass  # subclasses dao o feedback
