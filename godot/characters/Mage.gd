@tool
extends Node3D
## Mage — protagonista ARKANA (raia PERSONAGEM, R16). Visual PURO: malha
## procedural (SurfaceTool, zero binario) + AnimationPlayer. Sem fisica/input.
##
## Contrato (godot/ARQUITETURA.md):
##   play_anim("idle"|"run"|"cast")  — crossfade 0.15s, sem pulo
##   set_tint(Color)                 — recolore o manto (bots reusam o modelo)
## Para a raia de gameplay sincronizar o projetil com a anim `cast`:
##   sinal `cast_fired` (emitido pela propria anim no frame do disparo),
##   ou get_cast_fire_time() / get_anim_length("cast").

signal cast_fired

const CAST_FIRE_TIME := 0.22            # s dentro da anim `cast` em que a mao abre

const COL_ROBE := Color("1b2440")       # azul-noite arcano (GDD §10, clareado p/ leitura)
const COL_GOLD := Color("f0c75e")       # dourado arcano (GDD §10)
const COL_DARK := Color("07080f")       # vazio do capuz
const COL_SKIN := Color("e0b98d")

const SHADER := preload("res://characters/mage_toon.gdshader")

# ponytail: malhas compartilhadas entre instancias (player + 6 bots = 1 build)
static var _mesh_cache: Dictionary = {}

var _player: AnimationPlayer
var _tint_mats: Array[ShaderMaterial] = []
var _pending_tint: Variant = null
var _pending_anim := ""


func _ready() -> void:
	_build()
	if _pending_tint != null:
		set_tint(_pending_tint)
	if _pending_anim != "":
		play_anim(_pending_anim)
	else:
		_player.play("idle")


func play_anim(nome: String) -> void:
	if _player == null:
		_pending_anim = nome           # chamado antes de entrar na arvore
		return
	if _player.has_animation(nome):
		_player.play(nome, 0.15)
	else:
		push_warning("Mage: animacao desconhecida '%s'" % nome)


func set_tint(cor: Color) -> void:
	if _tint_mats.is_empty():
		_pending_tint = cor            # chamado antes de entrar na arvore
		return
	for m in _tint_mats:
		m.set_shader_parameter("tint", cor)
		m.set_shader_parameter("tint_mix", 0.85)


func get_cast_fire_time() -> float:
	return CAST_FIRE_TIME


func get_anim_length(nome: String) -> float:
	if _player != null and _player.has_animation(nome):
		return _player.get_animation(nome).length
	return 0.0


func _cast_fire() -> void:
	cast_fired.emit()


# ---------------------------------------------------------------- construcao

func _build() -> void:
	for c in get_children():
		remove_child(c)
		c.queue_free()
	_tint_mats.clear()

	var navy := _mat(COL_ROBE)                       # manto/tunica/capuz/mangas
	var gold := _mat(COL_GOLD)                       # debruns, cinto, ombreiras
	var dark := _mat(COL_DARK)                       # interior do capuz
	var skin := _mat(COL_SKIN)                       # maos livres para conjurar
	var eye := _mat(COL_DARK, COL_GOLD, 3.0)         # olhos que brilham no vazio
	var gem := _mat(COL_GOLD, COL_GOLD, 1.2)         # emblema no peito
	_tint_mats.append(navy)

	var rig := Node3D.new()
	rig.name = "Rig"
	add_child(rig)
	var hips := Node3D.new()
	hips.name = "Hips"
	hips.position = Vector3(0, 0.92, 0)
	rig.add_child(hips)

	# Manto: sino facetado ate o chao, bainha em zigue-zague + debrum dourado
	var robe := _mi(hips, "Robe", _lathe("robe", [
		Vector3(-0.90, 0.46, 0), Vector3(-0.88, 0.45, 0), Vector3(-0.60, 0.385, 0),
		Vector3(-0.30, 0.30, 0), Vector3(0.0, 0.245, 0), Vector3(0.12, 0.22, 0),
	], 10, {"scallop": 0.10, "cap_bottom": true}), navy)
	_mi(robe, "RobeTrim", _lathe("trim", [
		Vector3(-0.87, 0.462, 0), Vector3(-0.78, 0.443, 0),
	], 10, {}), gold)

	# Tronco + cinto + gola + emblema
	var torso := _mi(hips, "Torso", _lathe("torso", [
		Vector3(0.0, 0.24, 0), Vector3(0.28, 0.27, 0), Vector3(0.42, 0.16, 0),
	], 8, {"cap_top": true}), navy, Vector3(0, 0.10, 0))
	_mi(torso, "Belt", _lathe("belt", [
		Vector3(0.0, 0.268, 0), Vector3(0.05, 0.262, 0), Vector3(0.08, 0.235, 0),
	], 8, {}), gold)
	_mi(torso, "Collar", _lathe("collar", [
		Vector3(0.0, 0.215, 0), Vector3(0.07, 0.165, 0),
	], 8, {}), gold, Vector3(0, 0.36, 0))
	_mi(torso, "Emblem", _disc("emblem", 0.05, 4), gem, Vector3(0, 0.26, -0.28))

	# Ombreiras douradas caidas para fora
	var pad := _lathe("pad", [
		Vector3(0.0, 0.175, 0), Vector3(0.06, 0.135, 0), Vector3(0.115, 0.05, 0),
	], 6, {"cap_top": true, "cap_bottom": true})
	_mi(torso, "PadL", pad, gold, Vector3(-0.30, 0.36, 0), Vector3(0, 0, 0.35))
	_mi(torso, "PadR", pad, gold, Vector3(0.30, 0.36, 0), Vector3(0, 0, -0.35))

	# Capuz: revolucao parcial (frente aberta), bico varrido para tras,
	# forro interno escuro e dois olhos dourados flutuando no vazio
	var head := _mi(torso, "Head", _lathe("hood", [
		Vector3(-0.06, 0.15, 0), Vector3(0.02, 0.20, 0), Vector3(0.10, 0.19, 0.01),
		Vector3(0.20, 0.155, 0.05), Vector3(0.28, 0.09, 0.10), Vector3(0.36, 0.02, 0.16),
	], 8, {"a0": deg_to_rad(325.0), "sweep": deg_to_rad(250.0), "cap_top": true}), navy,
		Vector3(0, 0.46, 0))
	_mi(head, "HoodInner", _lathe("hood_in", [
		Vector3(-0.06, 0.15, 0), Vector3(0.02, 0.20, 0), Vector3(0.10, 0.19, 0.01),
		Vector3(0.20, 0.155, 0.05), Vector3(0.28, 0.09, 0.10), Vector3(0.36, 0.02, 0.16),
	], 8, {"a0": deg_to_rad(325.0), "sweep": deg_to_rad(250.0), "rscale": 0.93,
		"inside": true}), dark)
	_mi(head, "EyeL", _disc("eye", 0.016, 4), eye, Vector3(-0.034, 0.07, -0.135))
	_mi(head, "EyeR", _disc("eye", 0.016, 4), eye, Vector3(0.034, 0.07, -0.135))

	# Bracos: manga sino + punho dourado + mao livre
	var sleeve := _lathe("sleeve", [
		Vector3(-0.44, 0.13, 0), Vector3(-0.40, 0.115, 0),
		Vector3(-0.20, 0.078, 0), Vector3(0.02, 0.09, 0),
	], 6, {"cap_bottom": true, "cap_top": true})
	var cuff := _lathe("cuff", [
		Vector3(-0.43, 0.138, 0), Vector3(-0.36, 0.122, 0),
	], 6, {})
	var hand := _lathe("hand", [
		Vector3(-0.10, 0.02, 0), Vector3(-0.07, 0.05, 0),
		Vector3(-0.03, 0.055, 0), Vector3(0.0, 0.035, 0),
	], 6, {"cap_bottom": true, "cap_top": true})
	var arm_l := _mi(torso, "ArmL", sleeve, navy,
		Vector3(-0.30, 0.38, 0), Vector3(0.04, 0, -0.14))
	_mi(arm_l, "CuffL", cuff, gold)
	_mi(arm_l, "HandL", hand, skin, Vector3(0, -0.46, 0))
	var arm_r := _mi(torso, "ArmR", sleeve, navy,
		Vector3(0.30, 0.38, 0), Vector3(0.04, 0, 0.14))
	_mi(arm_r, "CuffR", cuff, gold)
	_mi(arm_r, "HandR", hand, skin, Vector3(0, -0.46, 0))

	# Animacoes
	_player = AnimationPlayer.new()
	_player.name = "AnimationPlayer"
	add_child(_player)
	var lib := AnimationLibrary.new()
	lib.add_animation("idle", _anim_idle())
	lib.add_animation("run", _anim_run())
	lib.add_animation("cast", _anim_cast())
	_player.add_animation_library("", lib)


func _mat(albedo: Color, emission := Color.BLACK, energy := 0.0) -> ShaderMaterial:
	var m := ShaderMaterial.new()
	m.shader = SHADER
	m.set_shader_parameter("albedo", albedo)
	m.set_shader_parameter("emission_color", emission)
	m.set_shader_parameter("emission_energy", energy)
	return m


func _mi(parent: Node, nm: String, mesh: Mesh, mat: Material,
		pos := Vector3.ZERO, rot := Vector3.ZERO) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = nm
	mi.mesh = mesh
	mi.material_override = mat
	mi.position = pos
	mi.rotation = rot
	parent.add_child(mi)
	return mi


## Revolve um perfil (x=altura, y=raio, z=recuo em Z do anel) em torno de Y.
## Nao-indexado + generate_normals() = flat shading facetado.
## opts: cap_top/cap_bottom (bool), scallop (bainha zigue-zague no anel 0),
##       a0/sweep (revolucao parcial, ex.: capuz aberto na frente),
##       rscale (escala de raio), inside (inverte faces — forro interno).
static func _lathe(key: String, profile: Array, sides: int, opts: Dictionary) -> ArrayMesh:
	if _mesh_cache.has(key):
		return _mesh_cache[key]
	var a0: float = opts.get("a0", 0.0)
	var sweep: float = opts.get("sweep", TAU)
	var wrap := is_equal_approx(sweep, TAU)
	var inside: bool = opts.get("inside", false)
	var rscale: float = opts.get("rscale", 1.0)
	var scallop: float = opts.get("scallop", 0.0)
	var cols := sides if wrap else sides + 1

	var rings: Array = []
	for i in profile.size():
		var p: Vector3 = profile[i]
		var ring := PackedVector3Array()
		for j in cols:
			var a := a0 + sweep * float(j) / float(sides)
			var r := p.y * rscale
			var y := p.x
			if scallop > 0.0 and i == 0 and j % 2 == 1:
				r *= 1.0 - scallop
				y += scallop * 0.30
			ring.append(Vector3(cos(a) * r, y, sin(a) * r + p.z))
		rings.append(ring)

	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in rings.size() - 1:
		var lo: PackedVector3Array = rings[i]
		var hi: PackedVector3Array = rings[i + 1]
		for j in sides:
			var jn := (j + 1) % cols if wrap else j + 1
			_tri(st, lo[j], lo[jn], hi[jn], inside)
			_tri(st, lo[j], hi[jn], hi[j], inside)
	if opts.get("cap_bottom", false):
		var p0: Vector3 = profile[0]
		var c0 := Vector3(0.0, p0.x, p0.z)
		var r0: PackedVector3Array = rings[0]
		for j in sides:
			var jn := (j + 1) % cols if wrap else j + 1
			_tri(st, c0, r0[jn], r0[j], inside)
	if opts.get("cap_top", false):
		var pt: Vector3 = profile[profile.size() - 1]
		var ct := Vector3(0.0, pt.x, pt.z)
		var rt: PackedVector3Array = rings[rings.size() - 1]
		for j in sides:
			var jn := (j + 1) % cols if wrap else j + 1
			_tri(st, ct, rt[j], rt[jn], inside)
	st.generate_normals()
	var mesh := st.commit()
	_mesh_cache[key] = mesh
	return mesh


## Disco/losango plano voltado para -Z (frente do personagem).
static func _disc(key: String, r: float, sides: int) -> ArrayMesh:
	if _mesh_cache.has(key):
		return _mesh_cache[key]
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for j in sides:
		var a := TAU * float(j) / float(sides)
		var an := TAU * float(j + 1) / float(sides)
		st.add_vertex(Vector3.ZERO)
		st.add_vertex(Vector3(cos(a) * r, sin(a) * r, 0.0))
		st.add_vertex(Vector3(cos(an) * r, sin(an) * r, 0.0))
	st.generate_normals()
	var mesh := st.commit()
	_mesh_cache[key] = mesh
	return mesh


static func _tri(st: SurfaceTool, a: Vector3, b: Vector3, c: Vector3, flip: bool) -> void:
	if flip:
		st.add_vertex(a)
		st.add_vertex(c)
		st.add_vertex(b)
	else:
		st.add_vertex(a)
		st.add_vertex(b)
		st.add_vertex(c)


# ----------------------------------------------------------------- animacoes

const _P := "Rig/Hips/Torso/"          # prefixo dos membros


func _new_anim(length: float, looped: bool) -> Animation:
	var a := Animation.new()
	a.length = length
	a.loop_mode = Animation.LOOP_LINEAR if looped else Animation.LOOP_NONE
	return a


func _tr(a: Animation, path: String, keys: Array) -> void:
	var t := a.add_track(Animation.TYPE_VALUE)
	a.track_set_path(t, NodePath(path))
	a.track_set_interpolation_type(t, Animation.INTERPOLATION_CUBIC)
	for k in keys:
		a.track_insert_key(t, k[0], k[1])


func _anim_idle() -> Animation:
	var a := _new_anim(2.4, true)
	_tr(a, "Rig:position", [[0.0, Vector3.ZERO], [1.2, Vector3(0, 0.02, 0)], [2.4, Vector3.ZERO]])
	_tr(a, "Rig:rotation", [[0.0, Vector3.ZERO]])
	_tr(a, _P.trim_suffix("/") + ":rotation",
		[[0.0, Vector3(0.02, 0, 0)], [1.2, Vector3(0.055, 0, 0)], [2.4, Vector3(0.02, 0, 0)]])
	_tr(a, "Rig/Hips/Robe:rotation",
		[[0.0, Vector3.ZERO], [1.2, Vector3(0.035, 0, 0.02)], [2.4, Vector3.ZERO]])
	_tr(a, "Rig/Hips/Robe:scale",
		[[0.0, Vector3.ONE], [1.2, Vector3(1.02, 1.0, 1.02)], [2.4, Vector3.ONE]])
	_tr(a, _P + "Head:rotation",
		[[0.0, Vector3.ZERO], [1.2, Vector3(0.04, 0, 0)], [2.4, Vector3.ZERO]])
	_tr(a, _P + "ArmL:rotation",
		[[0.0, Vector3(0.04, 0, -0.14)], [1.2, Vector3(0.09, 0, -0.18)], [2.4, Vector3(0.04, 0, -0.14)]])
	_tr(a, _P + "ArmR:rotation",
		[[0.0, Vector3(0.04, 0, 0.14)], [1.2, Vector3(0.09, 0, 0.18)], [2.4, Vector3(0.04, 0, 0.14)]])
	_tr(a, _P + "ArmR/HandR:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmL/HandL:scale", [[0.0, Vector3.ONE]])
	return a


func _anim_run() -> Animation:
	var a := _new_anim(0.6, true)
	_tr(a, "Rig:rotation", [[0.0, Vector3(-0.12, 0, 0)]])       # inclinado para frente
	_tr(a, "Rig:position", [
		[0.0, Vector3(0, 0.02, 0)], [0.15, Vector3(0, 0.055, 0)], [0.3, Vector3(0, 0.02, 0)],
		[0.45, Vector3(0, 0.055, 0)], [0.6, Vector3(0, 0.02, 0)]])
	_tr(a, _P + "ArmL:rotation", [
		[0.0, Vector3(0.6, 0, -0.12)], [0.3, Vector3(-0.6, 0, -0.12)], [0.6, Vector3(0.6, 0, -0.12)]])
	_tr(a, _P + "ArmR:rotation", [
		[0.0, Vector3(-0.6, 0, 0.12)], [0.3, Vector3(0.6, 0, 0.12)], [0.6, Vector3(-0.6, 0, 0.12)]])
	_tr(a, _P.trim_suffix("/") + ":rotation", [
		[0.0, Vector3(0.08, 0.10, 0)], [0.3, Vector3(0.08, -0.10, 0)], [0.6, Vector3(0.08, 0.10, 0)]])
	_tr(a, _P + "Head:rotation", [
		[0.0, Vector3(0.10, -0.06, 0)], [0.3, Vector3(0.10, 0.06, 0)], [0.6, Vector3(0.10, -0.06, 0)]])
	_tr(a, "Rig/Hips/Robe:rotation", [
		[0.0, Vector3(-0.20, 0, 0)], [0.15, Vector3(-0.14, 0, 0.02)], [0.3, Vector3(-0.20, 0, 0)],
		[0.45, Vector3(-0.14, 0, -0.02)], [0.6, Vector3(-0.20, 0, 0)]])
	_tr(a, "Rig/Hips/Robe:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmR/HandR:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmL/HandL:scale", [[0.0, Vector3.ONE]])
	return a


func _anim_cast() -> Animation:
	var a := _new_anim(0.55, false)
	# braco direito: recua, dispara para frente, mao abre; volta ao neutro
	_tr(a, _P + "ArmR:rotation", [
		[0.0, Vector3(0.04, 0, 0.14)], [0.10, Vector3(-0.55, 0, 0.30)],
		[CAST_FIRE_TIME, Vector3(1.45, 0, 0.04)], [0.40, Vector3(1.30, 0, 0.06)],
		[0.55, Vector3(0.04, 0, 0.14)]])
	_tr(a, _P + "ArmR/HandR:scale", [
		[0.0, Vector3.ONE], [0.10, Vector3(0.8, 0.8, 0.8)],
		[0.24, Vector3(1.3, 1.3, 1.3)], [0.42, Vector3(1.15, 1.15, 1.15)],
		[0.55, Vector3.ONE]])
	_tr(a, _P + "ArmL:rotation", [
		[0.0, Vector3(0.04, 0, -0.14)], [CAST_FIRE_TIME, Vector3(-0.35, 0, -0.20)],
		[0.55, Vector3(0.04, 0, -0.14)]])
	_tr(a, _P.trim_suffix("/") + ":rotation", [
		[0.0, Vector3(0.02, 0, 0)], [0.10, Vector3(0.02, -0.30, 0)],
		[CAST_FIRE_TIME, Vector3(0.0, 0.28, 0)], [0.55, Vector3(0.02, 0, 0)]])
	_tr(a, _P + "Head:rotation", [
		[0.0, Vector3.ZERO], [CAST_FIRE_TIME, Vector3(0.10, 0.06, 0)], [0.55, Vector3.ZERO]])
	_tr(a, "Rig:position", [
		[0.0, Vector3.ZERO], [CAST_FIRE_TIME, Vector3(0, -0.02, 0)], [0.55, Vector3.ZERO]])
	_tr(a, "Rig:rotation", [[0.0, Vector3.ZERO]])
	_tr(a, "Rig/Hips/Robe:rotation", [
		[0.0, Vector3.ZERO], [CAST_FIRE_TIME, Vector3(-0.08, 0, 0)], [0.55, Vector3.ZERO]])
	# marcador do disparo: a propria anim avisa a raia de gameplay
	var t := a.add_track(Animation.TYPE_METHOD)
	a.track_set_path(t, NodePath("."))
	a.track_insert_key(t, CAST_FIRE_TIME, {"method": "_cast_fire", "args": []})
	return a
