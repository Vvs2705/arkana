## Projetil elemental: TEMPO DE VIAGEM (GDD §4.1 — nada de hitscan).
## Cor + FORMA distintas por elemento (GDD §10 — lei):
##   fire      -> esfera/gota flamejante (a original)
##   water     -> lamina/crescente d'agua achatada, arco leve na trajetoria
##   lightning -> dardo fino serrilhado, o mais rapido, rastro eletrico curto
##   earth     -> pedra facetada tombando devagar, a mais lenta/pesada, poeira
##   wind      -> espiral de laminas de ar semi-transparentes girando no eixo
## Numeros SO de Balance (spec()). No impacto: dano via Combat (ponto unico),
## numero flutuante, burst, knockback leve — e Bus.terrain_hit SEMPRE (a
## costura R19: o terreno reage, este script nunca muda o mundo).
class_name Projectile
extends Area3D

const WATER_ARC := 1.6   # m/s^2 de queda da agua ("arco leve") — KNOB local

var element := "fire"
var dir := Vector3.FORWARD
var speed: float = float(Balance.FIRE.projectile_speed)
var travel_left: float = float(Balance.FIRE.range)
var dmg: float = float(Balance.FIRE.dmg)
var shooter: Node
var _fall := 0.0
var _spin_node: Node3D   # terra tomba, vento gira — so' visual, hitbox parada
var _spin := Vector3.ZERO  # rad/s em euler local


## Perfil CRU do elemento (sem arma). Delega para Arma.base — fonte UNICA dos
## numeros por elemento desde a R21; este metodo fica porque Bot.gd e Player.gd
## ja' o chamam para cadencia e alcance.
static func spec(el: String) -> Dictionary:
	return Arma.base(el)


## Cor canonica por elemento (paleta GDD §10). UI e efeitos leem daqui.
static func tint(el: String) -> Color:
	match el:
		"water":
			return Color("2AA7FF")
		"lightning":
			return Color("F5D90A")
		"earth":
			return Color("A8763E")
		"wind":
			return Color("8FE8C9")
		_:
			return Color("FF5A2A")


static func launch(parent: Node, p_shooter: Node, from: Vector3, p_dir: Vector3, el := "fire") -> Projectile:
	var p := Projectile.new()
	p.shooter = p_shooter
	p.dir = p_dir.normalized()
	p.element = el
	## A ARMA ARCANA ENTRA AQUI (GDD §16.2 — R21). O spec sai do slot do
	## atirador: cajado voa mais longe e bate mais forte, manopla voa mais
	## rapido. Quem NAO tem ArmaSlot recebe o perfil cru do elemento (= varinha,
	## a linha de base) — nenhum pawn precisou ser editado para isto funcionar.
	var s: Dictionary = ArmaSlot.spec_de(p_shooter, el)
	p.speed = float(s.projectile_speed)
	p.travel_left = float(s.range)
	p.dmg = float(s.dmg)
	parent.add_child(p)
	p.global_position = from
	if absf(p.dir.y) < 0.99:
		p.look_at(from + p.dir)  # lamina e dardo apontam para onde voam
	return p


func _ready() -> void:
	var col := CollisionShape3D.new()
	var sphere := SphereShape3D.new()
	sphere.radius = 0.25  # hitbox igual para todos — a FORMA e' leitura visual
	col.shape = sphere
	add_child(col)
	match element:
		"water":
			_build_water()
		"lightning":
			_build_lightning()
		"earth":
			_build_earth()
		"wind":
			_build_wind()
		_:
			_build_fire()
	body_entered.connect(_on_hit)


func _build_fire() -> void:
	set_meta("shape", "sphere")
	var mesh := MeshInstance3D.new()
	var ball := SphereMesh.new()
	ball.radius = 0.18
	ball.height = 0.36
	mesh.mesh = ball
	mesh.material_override = _glow_mat(tint("fire"), 2.5)
	add_child(mesh)
	add_child(_make_particles(tint("fire"), 10, 0.3, false))


func _build_water() -> void:
	set_meta("shape", "blade")
	var mesh := MeshInstance3D.new()
	var lens := SphereMesh.new()
	lens.radius = 0.22
	lens.height = 0.44
	mesh.mesh = lens
	mesh.scale = Vector3(1.6, 0.28, 0.7)  # lente larga e achatada = lamina/crescente
	mesh.material_override = _glow_mat(tint("water"), 2.0)
	add_child(mesh)
	add_child(_make_particles(tint("water"), 10, 0.35, false))


func _build_lightning() -> void:
	set_meta("shape", "dart")
	var mat := _glow_mat(tint("lightning"), 3.5)
	for rot in [0.0, PI / 4.0]:  # duas laminas finas cruzadas a 45° = serrilhado
		var mesh := MeshInstance3D.new()
		var box := BoxMesh.new()
		box.size = Vector3(0.07, 0.07, 0.95)
		mesh.mesh = box
		mesh.rotation.z = rot
		mesh.material_override = mat
		add_child(mesh)
	add_child(_make_particles(tint("lightning"), 14, 0.18, false))  # rastro curto = eletrico


func _build_earth() -> void:
	set_meta("shape", "rock")
	_spin_node = Node3D.new()
	var mat := _glow_mat(tint("earth"), 1.1)  # brilho baixo = pedra fosca, nao gema
	for rot: Vector3 in [Vector3.ZERO, Vector3(0.7, 0.5, 0.9)]:
		var mesh := MeshInstance3D.new()
		var box := BoxMesh.new()
		box.size = Vector3(0.30, 0.26, 0.30)
		mesh.mesh = box
		mesh.rotation = rot  # dois blocos cruzados = silhueta facetada
		mesh.material_override = mat
		_spin_node.add_child(mesh)
	add_child(_spin_node)
	_spin = Vector3(2.4, 3.2, 0.0)  # tomba DEVAGAR — leitura de peso
	add_child(_make_particles(tint("earth"), 8, 0.4, false))  # poeira no voo


func _build_wind() -> void:
	set_meta("shape", "spiral")
	var mat := _glow_mat(tint("wind"), 1.8)
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.albedo_color = Color(tint("wind"), 0.55)  # lamina de ar semi-transparente
	_spin_node = Node3D.new()
	for i in 3:  # 3 laminas torcidas ao redor do eixo do voo = helice/espiral
		var mesh := MeshInstance3D.new()
		var box := BoxMesh.new()
		box.size = Vector3(0.52, 0.05, 0.22)
		mesh.mesh = box
		mesh.rotation.z = TAU * float(i) / 3.0
		mesh.rotation.y = 0.5  # torcao: le como espiral, nao cruz
		mesh.material_override = mat
		_spin_node.add_child(mesh)
	add_child(_spin_node)
	_spin = Vector3(0.0, 0.0, 13.0)  # gira RAPIDO no eixo do voo
	add_child(_make_particles(tint("wind"), 12, 0.45, false))  # rastro de vento


func _glow_mat(color: Color, energy: float) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = color
	mat.emission_enabled = true
	mat.emission = color
	mat.emission_energy_multiplier = energy
	return mat


func _physics_process(delta: float) -> void:
	var step := speed * delta
	position += dir * step
	if element == "water":
		_fall += WATER_ARC * delta
		position.y -= _fall * delta
	if _spin_node:
		_spin_node.rotation += _spin * delta  # pedra tomba / espiral gira
	travel_left -= step
	if travel_left <= 0.0:
		queue_free()


func _on_hit(body: Node3D) -> void:
	if body == shooter:
		return
	var hit_pos := global_position
	var em_escudo: bool = ("shield" in body) and float(body.shield) > 0.0
	## O ELEMENTO DEIXA DE SER COSMETICO: a reacao no ALVO e resolvida ANTES do
	## dano porque a conducao (raio em quem esta molhado) multiplica o impacto
	## deste mesmo tiro. Efeitos NUNCA aplica dano direto — quem aplica e o Combat.
	var mult := Efeitos.aplicar(body, element, dmg, shooter)
	var efetivo := Combat.deal(body, dmg * mult, element, shooter)
	if efetivo > 0.0:
		_damage_number(efetivo, hit_pos, em_escudo)
		if body.has_method("apply_knockback"):
			body.apply_knockback(dir * _empurrao(body))
		# Kill do PLAYER anuncia no Bus — kill feed e audio observam (raias paralelas).
		if float(body.get("hp")) <= 0.0 and not body.is_in_group("player") \
				and shooter != null and is_instance_valid(shooter) and shooter.is_in_group("player"):
			Bus.player_killed_bot.emit(str(body.name))
	# A COSTURA DO TERRENO (R19): TODO impacto anuncia — chao, arvore, muro,
	# pawn. O terreno decide se e como reage; este script NUNCA muda o mundo.
	# strong=false por ora — taticas carregadas vem depois (R20+).
	Bus.terrain_hit.emit(element, hit_pos, false)
	burst(get_parent(), hit_pos, tint(element), 16)
	queue_free()


## EMPURRAO = base do Balance x fator do elemento (docs/DANO.md §3.3). Antes era
## uma constante local de 2.2 igual para os cinco: o vento, cuja identidade
## inteira e empurrar, empurrava tanto quanto a pedra.
func _empurrao(body: Node) -> float:
	var f := Combat.fator(element, "empurrao")
	## NADA DE JUGGLE: o empurrao DOBRADO so vale em quem esta no chao, senao o
	## vento vira controle infinito no ar.
	if f > 1.0 and body.has_method("is_on_floor") and not body.is_on_floor():
		f = 1.0
	return float(Balance.COMBATE.knockback) * f


## Numero flutuante. COR + FORMA (GDD §10): acerto em ESCUDO sai branco-azulado,
## acerto em VIDA sai na cor do elemento — antes era dourado fixo para tudo, e o
## jogador nao tinha como ler "bati no escudo" x "bati na vida". O TAMANHO sai do
## dano (docs/DANO.md §4.2): um acerto de 30 fica visivelmente maior que um de 8.
func _damage_number(n: float, at: Vector3, em_escudo: bool) -> void:
	var f: Dictionary = Balance.FEEDBACK
	var lbl := Label3D.new()
	lbl.text = str(int(round(n)))
	lbl.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	lbl.no_depth_test = true
	lbl.modulate = Color(str(f.cor_escudo)) if em_escudo else tint(element)
	lbl.outline_size = 10
	lbl.font_size = int(64.0 * minf(float(f.num_scale_base)
			+ float(f.num_scale_gain) * n / 25.0, float(f.num_scale_max)))
	lbl.pixel_size = 0.004
	get_parent().add_child(lbl)
	lbl.global_position = at + Vector3(0, 0.6, 0)
	var vida := float(f.num_life_s)
	var tw := lbl.create_tween()
	tw.tween_property(lbl, "position:y", lbl.position.y + 1.0, vida)
	tw.parallel().tween_property(lbl, "modulate:a", 0.0, vida)
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
