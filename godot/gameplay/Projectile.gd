## Projetil de Fogo: TEMPO DE VIAGEM (GDD §4.1 — nada de hitscan).
## Esfera emissiva + trail GPU barato; no impacto, dano via Combat (ponto
## unico) + numero flutuante + burst de particulas.
class_name Projectile
extends Area3D

var dir := Vector3.FORWARD
var speed: float = float(Balance.FIRE.projectile_speed)
var travel_left: float = float(Balance.FIRE.range)
var dmg: float = float(Balance.FIRE.dmg)
var shooter: Node


static func launch(parent: Node, p_shooter: Node, from: Vector3, p_dir: Vector3) -> Projectile:
	var p := Projectile.new()
	p.shooter = p_shooter
	p.dir = p_dir.normalized()
	parent.add_child(p)
	p.global_position = from
	return p


func _ready() -> void:
	var col := CollisionShape3D.new()
	var sphere := SphereShape3D.new()
	sphere.radius = 0.25
	col.shape = sphere
	add_child(col)

	var mesh := MeshInstance3D.new()
	var ball := SphereMesh.new()
	ball.radius = 0.18
	ball.height = 0.36
	mesh.mesh = ball
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color(1.0, 0.55, 0.15)
	mat.emission_enabled = true
	mat.emission = Color(1.0, 0.45, 0.1)
	mat.emission_energy_multiplier = 2.5
	mesh.material_override = mat
	add_child(mesh)

	var trail := _make_particles(Color(1.0, 0.5, 0.1), 10, 0.3, false)
	add_child(trail)

	body_entered.connect(_on_hit)


func _physics_process(delta: float) -> void:
	var step := speed * delta
	position += dir * step
	travel_left -= step
	if travel_left <= 0.0:
		queue_free()


func _on_hit(body: Node3D) -> void:
	if body == shooter:
		return
	var hit_pos := global_position
	if Combat.deal(body, dmg, "fire"):
		_damage_number(int(round(dmg)), hit_pos)
	burst(get_parent(), hit_pos, Color(1.0, 0.5, 0.12), 16)
	queue_free()


func _damage_number(n: int, at: Vector3) -> void:
	var lbl := Label3D.new()
	lbl.text = str(n)
	lbl.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	lbl.no_depth_test = true
	lbl.modulate = Color(1.0, 0.82, 0.25)
	lbl.outline_size = 10
	lbl.font_size = 64
	lbl.pixel_size = 0.004
	get_parent().add_child(lbl)
	lbl.global_position = at + Vector3(0, 0.6, 0)
	var tw := lbl.create_tween()
	tw.tween_property(lbl, "position:y", lbl.position.y + 1.0, 0.6)
	tw.parallel().tween_property(lbl, "modulate:a", 0.0, 0.6)
	tw.tween_callback(lbl.queue_free)


## Burst one-shot reutilizado por impacto e morte de bot.
static func burst(parent: Node, pos: Vector3, color: Color, amount := 16) -> void:
	if parent == null or not is_instance_valid(parent):
		return
	var p := _make_particles(color, amount, 0.45, true)
	parent.add_child(p)
	p.global_position = pos
	p.emitting = true
	p.finished.connect(p.queue_free)


static func _make_particles(color: Color, amount: int, lifetime: float, one_shot: bool) -> GPUParticles3D:
	var p := GPUParticles3D.new()
	p.amount = amount
	p.lifetime = lifetime
	p.one_shot = one_shot
	p.explosiveness = 1.0 if one_shot else 0.0
	p.local_coords = false
	var pm := ParticleProcessMaterial.new()
	pm.direction = Vector3.UP
	pm.spread = 180.0
	pm.initial_velocity_min = 2.0 if one_shot else 0.1
	pm.initial_velocity_max = 5.0 if one_shot else 0.4
	pm.gravity = Vector3(0, -3.0, 0) if one_shot else Vector3.ZERO
	pm.color = color
	pm.scale_min = 0.6
	pm.scale_max = 1.0
	p.process_material = pm
	var quad := QuadMesh.new()
	quad.size = Vector2(0.14, 0.14)
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.vertex_color_use_as_albedo = true
	mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	quad.material = mat
	p.draw_pass_1 = quad
	return p
