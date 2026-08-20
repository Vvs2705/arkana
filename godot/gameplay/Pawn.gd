## Base de player e bots: hp, visual (Mage.tscn com FALLBACK — fiacao
## defensiva obrigatoria) e a VELOCIDADE como produto unico.
class_name Pawn
extends CharacterBody3D

const MAGE_SCENE := "res://characters/Mage.tscn"

var hp: float = float(Balance.PLAYER.hp)
var speed_factor := 1.0  # bots reduzem aqui, nunca no Balance
var terrain_mult := 1.0  # G2 (gelo/agua) escreve aqui
var status_mult := 1.0   # lentidao/buff escrevem aqui
var visual: Node3D
var _cur_anim := ""
var _gravity: float = float(ProjectSettings.get_setting("physics/3d/default_gravity", 9.8))


## Velocidade e' PRODUTO UNICO (regra provada) — ninguem escreve m/s direto.
func current_speed() -> float:
	return float(Balance.PLAYER.speed) * terrain_mult * status_mult * speed_factor


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
