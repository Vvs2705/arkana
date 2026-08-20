@tool
## world/Island.gd — dono: raia MUNDO (R16, G0+G1).
## Ilha 100% PROCEDURAL (zero binario no repo): terreno ArrayMesh com relevo,
## os 4 POIs do GDD §14 em 3D (floresta / lago / ruinas / baixada-alagado),
## cel-shading via world/toon.gdshader, colisao trimesh + primitivas.
## DETERMINISMO: seeds fixos — a ilha e' sempre a MESMA (spawns confiaveis
## para a raia de gameplay; contrato em godot/ARQUITETURA.md).
## Distancias do MUNDO em METROS (regra da ponte: dp e' so' do dedo).
extends Node3D

const SIZE := 180.0          # m, lado do terreno (101x101 verts = 20k tris)
const QUADS := 100
const SEA_Y := 0.0

# POIs (GDD §14 reinterpretados) — centros XZ locais
const LAKE := Vector2(45, 18)
const LAKE_R := 17.0
const MARSH := Vector2(-42, 36)      # baixada/alagado
const MARSH_R := 16.0
const FOREST := Vector2(-36, -40)
const FOREST_R := 26.0
const RUINS := Vector2(38, -44)      # plato elevado
const RUINS_R := 14.0

# Paleta GDD §10 — saturada, "clima com COR, nunca falta de luz"
const COL_GRASS := Color("58bd6d")
const COL_GRASS_HI := Color("7ed687")
const COL_SAND := Color("e8cf8b")
const COL_ROCK := Color("8e97ad")
const COL_MUD := Color("a8763e")     # Terra
const COL_TRUNK := Color("7a5230")
const COL_LEAF_A := Color("3fa85c")
const COL_LEAF_B := Color("6fd177")
const COL_STONE := Color("b3bccf")
const COL_REED := Color("8fe8c9")    # Vento

# Icosaedro (winding CW = frente no Godot; lista classica CCW ja invertida)
const ICO_V: Array[Vector3] = [
	Vector3(-1, 1.618, 0), Vector3(1, 1.618, 0), Vector3(-1, -1.618, 0), Vector3(1, -1.618, 0),
	Vector3(0, -1, 1.618), Vector3(0, 1, 1.618), Vector3(0, -1, -1.618), Vector3(0, 1, -1.618),
	Vector3(1.618, 0, -1), Vector3(1.618, 0, 1), Vector3(-1.618, 0, -1), Vector3(-1.618, 0, 1),
]
const ICO_F := [
	[5, 11, 0], [1, 5, 0], [7, 1, 0], [10, 7, 0], [11, 10, 0],
	[9, 5, 1], [4, 11, 5], [2, 10, 11], [6, 7, 10], [8, 1, 7],
	[4, 9, 3], [2, 4, 3], [6, 2, 3], [8, 6, 3], [9, 8, 3],
	[5, 9, 4], [11, 4, 2], [10, 2, 6], [7, 6, 8], [1, 8, 9],
]

var _noise := FastNoiseLite.new()
# arvores: dados VIVOS p/ a raia TERRENO (GDD §14 — a copa some quando queima)
var _tree_xforms: Array[Transform3D] = []
var _tree_shapes: Array[CollisionShape3D] = []
var _forest_mm: MultiMesh
var _stump_mm: MultiMesh
var _burned_trees := {}  # i -> true (estado consultavel; RS nao le' em headless)
var _toon: ShaderMaterial
var _water_sea: ShaderMaterial
var _water_lake: ShaderMaterial
var _water_marsh: ShaderMaterial
var _grass_mat: ShaderMaterial
var _flower_mat: ShaderMaterial
var _mist_mat: ShaderMaterial


func _ready() -> void:
	_noise.seed = 7
	_noise.frequency = 0.02
	_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	_toon = ShaderMaterial.new()
	_toon.shader = load("res://world/toon.gdshader")
	_water_sea = _water_mat(Color("2f8fe0"), Color("103f8a"), 0.16, 0.0)
	_water_lake = _water_mat(Color("2aa7ff"), Color("1668c9"), 0.07, 1.0)
	_water_marsh = _water_mat(Color("55c9b0"), Color("1f7f8c"), 0.04, 1.0)
	var grass_sh := load("res://world/grass.gdshader")
	_grass_mat = ShaderMaterial.new()
	_grass_mat.shader = grass_sh
	_flower_mat = ShaderMaterial.new()
	_flower_mat.shader = grass_sh
	_flower_mat.set_shader_parameter("sway", 0.05)
	_mist_mat = ShaderMaterial.new()
	_mist_mat.shader = load("res://world/mist.gdshader")
	_build()
	if not Engine.is_editor_hint():
		_spawn_terrain()


## TERRENO REATIVO (R19): a ilha monta o proprio sistema de reacao — assim o
## restart do Main (que so remonta a Arena) nao precisa saber que ele existe.
## Fiacao defensiva: a raia TERRENO pode faltar e a ilha boota do mesmo jeito.
func _spawn_terrain() -> void:
	var scr: Variant = load("res://terrain/TerrainSystem.gd")
	if scr is GDScript:
		var t: Node = (scr as GDScript).new()
		t.name = "TerrainSystem"
		add_child(t)


func _build() -> void:
	var old := get_node_or_null("Generated")
	if old:
		old.free()
	var gen := Node3D.new()
	gen.name = "Generated"
	add_child(gen)
	_build_terrain(gen)
	_build_water(gen)
	_build_forest(gen)
	_build_ruins(gen)
	_build_marsh(gen)
	_build_rocks(gen)
	_build_grass(gen)
	_build_flowers(gen)
	_build_fireflies(gen)
	_build_mist(gen)
	_snap_spawns()


# ---------------------------------------------------------------- relevo

func height(x: float, z: float) -> float:
	var p := Vector2(x, z)
	var r := p.length()
	var fall := 1.0 - smoothstep(52.0, 82.0, r)
	var n := _noise.get_noise_2d(x, z) * 0.5 + 0.5
	var h := fall * (1.8 + 7.2 * n)               # colinas
	var valley := 1.0 - smoothstep(4.0, 30.0, r)  # vale central
	h -= 2.8 * valley
	if valley > 0.0:
		h = maxf(h, 1.05)                         # vale nunca afunda no mar
	h = lerpf(h, -2.4, 1.0 - smoothstep(LAKE_R * 0.55, LAKE_R + 8.0, p.distance_to(LAKE)))
	var dm := p.distance_to(MARSH)
	h = lerpf(h, 0.45, 0.92 * (1.0 - smoothstep(MARSH_R * 0.6, MARSH_R + 12.0, dm)))
	h -= 0.5 * (1.0 - smoothstep(4.0, 10.0, dm))  # poca central do alagado
	h = lerpf(h, 6.4, 1.0 - smoothstep(RUINS_R * 0.6, RUINS_R + 11.0, p.distance_to(RUINS)))
	h -= 3.2 * smoothstep(70.0, 90.0, r)          # borda mergulha no mar
	return h


func _vcolor(x: float, z: float, h: float, ny: float) -> Color:
	var p := Vector2(x, z)
	if p.distance_to(MARSH) < MARSH_R + 4.0:
		return COL_MUD.lerp(COL_GRASS, clampf((h - 0.8) * 0.9, 0.0, 1.0))
	if h < 0.95:
		return COL_SAND
	if p.distance_to(RUINS) < RUINS_R * 0.8:
		return COL_ROCK.lerp(COL_GRASS, 0.35)     # piso de pedra tomado pelo mato
	if ny < 0.62 or h > 7.6:
		return COL_ROCK
	var g := COL_GRASS_HI.lerp(COL_GRASS, clampf((h - 1.0) / 7.0, 0.0, 1.0))
	var v := _noise.get_noise_2d(x * 3.0, z * 3.0) * 0.05
	return Color(g.r + v, g.g + v, g.b + v)


func _build_terrain(parent: Node3D) -> void:
	var n1 := QUADS + 1
	var verts := PackedVector3Array()
	var norms := PackedVector3Array()
	var cols := PackedColorArray()
	var idx := PackedInt32Array()
	verts.resize(n1 * n1)
	norms.resize(n1 * n1)
	cols.resize(n1 * n1)
	var e := 0.6
	for iz in n1:
		for ix in n1:
			var x := (float(ix) / QUADS - 0.5) * SIZE
			var z := (float(iz) / QUADS - 0.5) * SIZE
			var h := height(x, z)
			var nrm := Vector3(
				height(x - e, z) - height(x + e, z),
				2.0 * e,
				height(x, z - e) - height(x, z + e)).normalized()
			var i := iz * n1 + ix
			verts[i] = Vector3(x, h, z)
			norms[i] = nrm
			cols[i] = _vcolor(x, z, h, nrm.y)
	for iz in QUADS:
		for ix in QUADS:
			var a := iz * n1 + ix
			var b := a + 1
			var c := a + n1
			var d := c + 1
			idx.append_array(PackedInt32Array([a, b, c, b, d, c]))
	var arr := []
	arr.resize(Mesh.ARRAY_MAX)
	arr[Mesh.ARRAY_VERTEX] = verts
	arr[Mesh.ARRAY_NORMAL] = norms
	arr[Mesh.ARRAY_COLOR] = cols
	arr[Mesh.ARRAY_INDEX] = idx
	var mesh := ArrayMesh.new()
	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arr)
	mesh.surface_set_material(0, _toon)
	var mi := MeshInstance3D.new()
	mi.name = "Terrain"
	mi.mesh = mesh
	parent.add_child(mi)
	# colisao: trimesh estatico (20k tris — ok p/ corpo estatico mobile)
	var body := StaticBody3D.new()
	var shape := CollisionShape3D.new()
	shape.shape = mesh.create_trimesh_shape()
	body.add_child(shape)
	mi.add_child(body)


# ---------------------------------------------------------------- agua

func _water_mat(shallow: Color, deep: Color, wave: float, edge: float) -> ShaderMaterial:
	var m := ShaderMaterial.new()
	m.shader = load("res://world/water.gdshader")
	m.set_shader_parameter("shallow_color", shallow)
	m.set_shader_parameter("deep_color", deep)
	m.set_shader_parameter("wave_height", wave)
	m.set_shader_parameter("edge_foam", edge)
	return m


func _build_water(parent: Node3D) -> void:
	var sea := PlaneMesh.new()
	sea.size = Vector2(520, 520)
	sea.subdivide_width = 40
	sea.subdivide_depth = 40
	sea.material = _water_sea
	_add_mesh(parent, sea, Transform3D(Basis.IDENTITY, Vector3(0, SEA_Y, 0)), "Sea", false)
	_add_mesh(parent, _disc_mesh(18.0, _water_lake),
			Transform3D(Basis.IDENTITY, Vector3(LAKE.x, 0.6, LAKE.y)), "LakeWater", false)
	_add_mesh(parent, _disc_mesh(15.0, _water_marsh),
			Transform3D(Basis.IDENTITY, Vector3(MARSH.x, 0.55, MARSH.y)), "MarshWater", false)


## Disco em aneis; COLOR.r = fracao do raio (0 centro, 1 borda) — os shaders
## de agua/nevoa usam isso p/ espuma de margem e fade de borda (G2).
func _disc_mesh(radius: float, mat: Material) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var segs := 24
	var rings := [0.3, 0.55, 0.8, 1.0]
	for i in segs:
		var a0 := TAU * i / segs
		var a1 := TAU * (i + 1) / segs
		var r0: float = rings[0] * radius
		st.set_color(Color(0, 0, 0))
		st.add_vertex(Vector3.ZERO)
		st.set_color(Color(rings[0], 0, 0))
		st.add_vertex(Vector3(cos(a0) * r0, 0, sin(a0) * r0))
		st.add_vertex(Vector3(cos(a1) * r0, 0, sin(a1) * r0))
		for k in rings.size() - 1:
			var ri: float = rings[k] * radius
			var ro: float = rings[k + 1] * radius
			var ci := Color(rings[k], 0, 0)
			var co := Color(rings[k + 1], 0, 0)
			var i0 := Vector3(cos(a0) * ri, 0, sin(a0) * ri)
			var o0 := Vector3(cos(a0) * ro, 0, sin(a0) * ro)
			var o1 := Vector3(cos(a1) * ro, 0, sin(a1) * ro)
			var i1 := Vector3(cos(a1) * ri, 0, sin(a1) * ri)
			# mesma ordem do _quad(i0,o0,o1,i1), com cor por anel
			st.set_color(ci)
			st.add_vertex(i0)
			st.set_color(co)
			st.add_vertex(o0)
			st.add_vertex(o1)
			st.set_color(ci)
			st.add_vertex(i0)
			st.set_color(co)
			st.add_vertex(o1)
			st.set_color(ci)
			st.add_vertex(i1)
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, mat)
	return m


# ---------------------------------------------------------------- floresta

func _build_forest(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 21
	var xf: Array[Transform3D] = []
	var tries := 0
	# ponytail: rejeicao O(n^2) — n=94, roda uma vez no load, irrelevante
	while xf.size() < 78 and tries < 1600:
		tries += 1
		var p := FOREST + Vector2(rng.randf_range(-1, 1), rng.randf_range(-1, 1)) * FOREST_R
		if p.distance_to(FOREST) > FOREST_R:
			continue
		var h := height(p.x, p.y)
		if h < 1.2 or h > 7.4:
			continue
		var ok := true
		for t in xf:
			if Vector2(t.origin.x, t.origin.z).distance_to(p) < 3.4:
				ok = false
				break
		if ok:
			xf.append(_tree_xform(rng, p, h))
	# arvores avulsas fora da mata fechada
	tries = 0
	var extra := 0
	while extra < 16 and tries < 900:
		tries += 1
		var p := Vector2(rng.randf_range(-62, 62), rng.randf_range(-62, 62))
		if p.length() > 62 or p.distance_to(FOREST) < FOREST_R \
				or p.distance_to(LAKE) < LAKE_R + 6 or p.distance_to(MARSH) < MARSH_R + 4 \
				or p.distance_to(RUINS) < RUINS_R + 4:
			continue
		var h := height(p.x, p.y)
		if h < 1.2 or h > 7.0:
			continue
		xf.append(_tree_xform(rng, p, h))
		extra += 1
	_tree_xforms = xf
	_forest_mm = _multimesh(parent, _tree_mesh(), xf, "Forest").multimesh
	# tocos de carvao (GDD §14: a copa some DE VERDADE, sobra o toco) — mesmo
	# truque de MultiMesh: todos pre-alocados escondidos, 1 draw call sempre.
	var hid := _hidden_xf()
	var stumps: Array[Transform3D] = []
	stumps.resize(xf.size())
	stumps.fill(hid)
	_stump_mm = _multimesh(parent, _stump_mesh(), stumps, "Stumps").multimesh
	# colisao: tronco = cilindro por arvore (a raia TERRENO desliga ao queimar)
	var body := StaticBody3D.new()
	body.name = "TreeColliders"
	var cyl := CylinderShape3D.new()
	cyl.radius = 0.38
	cyl.height = 3.0
	_tree_shapes.clear()
	for t in xf:
		var cs := CollisionShape3D.new()
		cs.shape = cyl
		cs.position = t.origin + Vector3(0, 1.5, 0)
		body.add_child(cs)
		_tree_shapes.append(cs)
	parent.add_child(body)


func _tree_xform(rng: RandomNumberGenerator, p: Vector2, h: float) -> Transform3D:
	var s := rng.randf_range(0.8, 1.35)
	var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)) \
			.scaled(Vector3(s, rng.randf_range(0.9, 1.15) * s, s))
	return Transform3D(b, Vector3(p.x, h - 0.1, p.y))


func _tree_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var rng := RandomNumberGenerator.new()
	rng.seed = 11
	_frustum(st, Vector3.ZERO, 0.34, 0.24, 2.8, 5, COL_TRUNK)
	_blob(st, Vector3(0, 3.6, 0), Vector3(2.3, 1.9, 2.3), COL_LEAF_A, rng, 0.16)
	_blob(st, Vector3(0.7, 4.6, 0.4), Vector3(1.3, 1.1, 1.3), COL_LEAF_B, rng, 0.16)
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon)
	return m


# ---------------------------------------------------------------- ruinas

func _build_ruins(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 31
	var cols: Array[Transform3D] = []
	var blks: Array[Transform3D] = []
	var body := StaticBody3D.new()
	body.name = "RuinColliders"

	# circulo de 8 colunas quebradas + 1 caida
	for i in 8:
		var ang := TAU * i / 8.0
		var p := Vector2(RUINS.x + cos(ang) * 8.0, RUINS.y + sin(ang) * 8.0)
		var h := height(p.x, p.y)
		if i == 2:
			# coluna caida, deitada apontando pra fora do circulo
			var bf := Basis.from_euler(Vector3(0, -ang, PI * 0.47))
			cols.append(Transform3D(bf, Vector3(p.x + cos(ang) * 1.5, h + 0.6, p.y + sin(ang) * 1.5)))
			continue
		var sy := rng.randf_range(0.35, 1.05)
		var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)).scaled(Vector3(1, sy, 1))
		cols.append(Transform3D(b, Vector3(p.x, h, p.y)))
		var cs := CollisionShape3D.new()
		var cshape := CylinderShape3D.new()
		cshape.radius = 0.62
		cshape.height = 3.0 * sy
		cs.shape = cshape
		cs.position = Vector3(p.x, h + 1.5 * sy, p.y)
		body.add_child(cs)

	# 2 muros quebrados de blocos, com lacunas e camadas caidas
	var walls := [
		{"start": Vector2(RUINS.x - 8.0, RUINS.y - 11.0), "dir": Vector2(1, 0.18).normalized(), "n": 7},
		{"start": Vector2(RUINS.x + 10.5, RUINS.y - 4.0), "dir": Vector2(0.25, 1).normalized(), "n": 6},
	]
	var blk_shape := BoxShape3D.new()
	blk_shape.size = Vector3(2.2, 0.9, 1.0)
	for w in walls:
		var dirv: Vector2 = w["dir"]
		var yaw := -atan2(dirv.y, dirv.x)
		for j in w["n"]:
			if j == 3:
				continue  # lacuna — muro QUEBRADO
			var p: Vector2 = w["start"] + dirv * (j * 2.3)
			var h := height(p.x, p.y)
			var layers := 2 if (j == 0 or j == int(w["n"]) - 1) else 1
			for l in layers:
				var jy := yaw + rng.randf_range(-0.12, 0.12)
				var t := Transform3D(Basis(Vector3.UP, jy), Vector3(p.x, h + 0.45 + l * 0.92, p.y))
				blks.append(t)
				var cs := CollisionShape3D.new()
				cs.shape = blk_shape
				cs.transform = t
				body.add_child(cs)
	# altar central: 2 blocos + toco de coluna
	var hc := height(RUINS.x, RUINS.y)
	blks.append(Transform3D(Basis(Vector3.UP, 0.4), Vector3(RUINS.x, hc + 0.45, RUINS.y)))
	blks.append(Transform3D(Basis(Vector3.UP, 1.1).scaled(Vector3(0.7, 1, 0.7)),
			Vector3(RUINS.x + 0.3, hc + 1.35, RUINS.y - 0.2)))
	cols.append(Transform3D(Basis.IDENTITY.scaled(Vector3(1, 0.28, 1)),
			Vector3(RUINS.x - 2.6, hc, RUINS.y + 2.2)))

	_multimesh(parent, _column_mesh(), cols, "RuinColumns")
	_multimesh(parent, _block_mesh(), blks, "RuinBlocks")
	parent.add_child(body)


func _column_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	_frustum(st, Vector3.ZERO, 0.58, 0.5, 3.0, 6, COL_STONE)
	# tampa do topo
	st.set_color(COL_STONE)
	for i in 6:
		var a0 := TAU * i / 6.0
		var a1 := TAU * (i + 1) / 6.0
		st.add_vertex(Vector3(0, 3.0, 0))
		st.add_vertex(Vector3(cos(a0) * 0.5, 3.0, sin(a0) * 0.5))
		st.add_vertex(Vector3(cos(a1) * 0.5, 3.0, sin(a1) * 0.5))
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon)
	return m


func _block_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	_box(st, Vector3(1.1, 0.45, 0.5), COL_STONE.darkened(0.1))
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon)
	return m


# ---------------------------------------------------------------- baixada

func _build_marsh(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 41
	var xf: Array[Transform3D] = []
	var tries := 0
	while xf.size() < 45 and tries < 700:
		tries += 1
		var p := MARSH + Vector2(rng.randf_range(-1, 1), rng.randf_range(-1, 1)) * (MARSH_R - 1.0)
		if p.distance_to(MARSH) > MARSH_R - 1.0:
			continue
		var h := height(p.x, p.y)
		if h < 0.25 or h > 0.85:
			continue
		var s := rng.randf_range(0.7, 1.3)
		var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)).scaled(Vector3(s, s, s))
		xf.append(Transform3D(b, Vector3(p.x, h - 0.05, p.y)))
	_multimesh(parent, _reed_mesh(), xf, "Reeds", false)


func _reed_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for k in 2:
		var ang := k * PI * 0.5
		var d := Vector3(cos(ang), 0, sin(ang)) * 0.28
		var top := Vector3(0, 1.5, 0)
		_quad(st, -d, d, d + top, -d + top, COL_REED)
		_quad(st, d, -d, -d + top, d + top, COL_REED)  # verso
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon)
	return m


# ---------------------------------------------------------------- rochas

func _build_rocks(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 51
	var xf: Array[Transform3D] = []
	var body := StaticBody3D.new()
	body.name = "RockColliders"
	var tries := 0
	while xf.size() < 26 and tries < 1200:
		tries += 1
		var p := Vector2(rng.randf_range(-70, 70), rng.randf_range(-70, 70))
		if p.length() > 72 or p.distance_to(LAKE) < LAKE_R + 3 or p.distance_to(MARSH) < MARSH_R:
			continue
		var h := height(p.x, p.y)
		var hill := h > 4.5
		var beach := h > 0.25 and h < 1.0
		if not (hill or beach):
			continue
		var s := rng.randf_range(0.5, 2.2)
		var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)).scaled(Vector3(s, s * 0.75, s))
		xf.append(Transform3D(b, Vector3(p.x, h + 0.1, p.y)))
		if s > 1.4:
			var cs := CollisionShape3D.new()
			var sph := SphereShape3D.new()
			sph.radius = s * 0.8
			cs.shape = sph
			cs.position = Vector3(p.x, h + s * 0.3, p.y)
			body.add_child(cs)
	_multimesh(parent, _rock_mesh(), xf, "Rocks")
	parent.add_child(body)


func _rock_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var rng := RandomNumberGenerator.new()
	rng.seed = 13
	_blob(st, Vector3.ZERO, Vector3(1, 0.75, 1), COL_ROCK, rng, 0.3)
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon)
	return m


# ---------------------------------------------------------------- vida (G2)

## Normal.y aproximada do terreno (mesmo esquema do _build_terrain).
func _terrain_ny(x: float, z: float) -> float:
	var e := 0.6
	return Vector3(height(x - e, z) - height(x + e, z), 2.0 * e,
			height(x, z - e) - height(x, z + e)).normalized().y


func _build_grass(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 61
	var xf: Array[Transform3D] = []
	var cols := PackedColorArray()
	var tries := 0
	# ponytail: scatter por rejeicao, roda 1x no load (<100ms); sem checagem
	# de espacamento — tufos sobrepostos leem como moita, nao como bug
	while xf.size() < 3000 and tries < 12000:
		tries += 1
		var p := Vector2(rng.randf_range(-74, 74), rng.randf_range(-74, 74))
		if p.length() > 74 or p.distance_to(LAKE) < LAKE_R + 2 or p.distance_to(MARSH) < MARSH_R:
			continue
		var h := height(p.x, p.y)
		if h < 1.05 or h > 7.3 or _terrain_ny(p.x, p.y) < 0.66:
			continue
		var s := rng.randf_range(0.7, 1.4)
		var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)) \
				.scaled(Vector3(s, rng.randf_range(0.8, 1.2) * s, s))
		xf.append(Transform3D(b, Vector3(p.x, h - 0.06, p.y)))
		var v := rng.randf_range(-0.12, 0.12)
		cols.append(Color(1.0 + v * 1.6, 1.0 + v, 1.0 + v * 0.4))
	_multimesh(parent, _grass_mesh(), xf, "Grass", false, cols)


func _grass_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var base := Color(0.14, 0.34, 0.18)   # pe' escuro = oclusao de graca
	var tip := Color(0.5, 0.86, 0.45)
	for k in 2:
		var ang := k * PI * 0.5
		var d := Vector3(cos(ang), 0, sin(ang))
		var b0 := -d * 0.3
		var b1 := d * 0.3
		var t1 := d * 0.11 + Vector3(0, 0.55, 0)
		var t0 := -d * 0.11 + Vector3(0, 0.55, 0)
		st.set_color(base)
		st.add_vertex(b0)
		st.add_vertex(b1)
		st.set_color(tip)
		st.add_vertex(t1)
		st.set_color(base)
		st.add_vertex(b0)
		st.set_color(tip)
		st.add_vertex(t1)
		st.add_vertex(t0)
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _grass_mat)
	return m


func _build_flowers(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 71
	# petalas na paleta GDD §10: dourado arcano, Vento, rosa, branco
	var petals: Array[Color] = [Color("f0c75e"), Color("8fe8c9"), Color("ff8ab3"), Color("f5f0ff")]
	var xf: Array[Transform3D] = []
	var cols := PackedColorArray()
	var tries := 0
	while xf.size() < 240 and tries < 4000:
		tries += 1
		var p := Vector2(rng.randf_range(-72, 72), rng.randf_range(-72, 72))
		var near_poi: bool = p.distance_to(LAKE) < LAKE_R + 9 \
				or p.distance_to(FOREST) < FOREST_R or p.distance_to(RUINS) < RUINS_R + 6
		if not near_poi or p.distance_to(LAKE) < LAKE_R + 1 or p.distance_to(MARSH) < MARSH_R:
			continue
		var h := height(p.x, p.y)
		if h < 1.05 or h > 7.2 or _terrain_ny(p.x, p.y) < 0.7:
			continue
		var s := rng.randf_range(0.8, 1.2)
		xf.append(Transform3D(Basis(Vector3.UP, rng.randf_range(0.0, TAU)).scaled(Vector3(s, s, s)),
				Vector3(p.x, h - 0.03, p.y)))
		cols.append(petals[rng.randi_range(0, petals.size() - 1)])
	_multimesh(parent, _flower_mesh(), xf, "Flowers", false, cols)


func _flower_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var stem := Color(0.35, 0.55, 0.3)
	var head := Color(1, 1, 1)   # a tinta da instancia da' a cor da petala
	for k in 2:
		var ang := k * PI * 0.5
		var d := Vector3(cos(ang), 0, sin(ang))
		var b0 := -d * 0.09
		var b1 := d * 0.09
		var t1 := d * 0.14 + Vector3(0, 0.34, 0)
		var t0 := -d * 0.14 + Vector3(0, 0.34, 0)
		st.set_color(stem)
		st.add_vertex(b0)
		st.add_vertex(b1)
		st.set_color(head)
		st.add_vertex(t1)
		st.set_color(stem)
		st.add_vertex(b0)
		st.set_color(head)
		st.add_vertex(t1)
		st.add_vertex(t0)
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _flower_mat)
	return m


func _build_fireflies(parent: Node3D) -> void:
	# vagalumes da floresta no entardecer — HDR + glow do Environment acende
	# ponytail: CPUParticles (24 particulas) — mais previsivel que GPUParticles
	# na zoologia de drivers Android; 1 draw call
	var p := CPUParticles3D.new()
	p.name = "Fireflies"
	p.amount = 24
	p.lifetime = 8.0
	p.preprocess = 8.0
	p.randomness = 0.5
	p.emission_shape = CPUParticles3D.EMISSION_SHAPE_BOX
	p.emission_box_extents = Vector3(FOREST_R * 0.8, 2.2, FOREST_R * 0.8)
	p.spread = 180.0
	p.gravity = Vector3.ZERO
	p.initial_velocity_min = 0.25
	p.initial_velocity_max = 0.7
	p.scale_amount_min = 0.06
	p.scale_amount_max = 0.11
	var quad := QuadMesh.new()
	quad.size = Vector2.ONE
	var m := StandardMaterial3D.new()
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	m.vertex_color_use_as_albedo = true
	m.albedo_color = Color(2.3, 1.9, 0.7)   # dourado arcano em HDR
	quad.material = m
	p.mesh = quad
	p.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	p.position = Vector3(FOREST.x, height(FOREST.x, FOREST.y) + 2.6, FOREST.y)
	parent.add_child(p)


func _build_mist(parent: Node3D) -> void:
	_add_mesh(parent, _disc_mesh(MARSH_R + 3.0, _mist_mat),
			Transform3D(Basis.IDENTITY, Vector3(MARSH.x, 1.15, MARSH.y)), "MarshMist", false)


# ---------------------------------------------------------------- helpers

func _snap_spawns() -> void:
	for c in get_children():
		if c is Marker3D and c.is_in_group("spawn"):
			c.position.y = height(c.position.x, c.position.z) + 0.3


func _add_mesh(parent: Node3D, mesh: Mesh, t: Transform3D, nm: String, shadows := true) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = nm
	mi.mesh = mesh
	mi.transform = t
	if not shadows:
		mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(mi)
	return mi


func _multimesh(parent: Node3D, mesh: Mesh, xforms: Array[Transform3D], nm: String,
		shadows := true, colors := PackedColorArray()) -> MultiMeshInstance3D:
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.use_colors = colors.size() > 0
	mm.mesh = mesh
	mm.instance_count = xforms.size()
	for i in xforms.size():
		mm.set_instance_transform(i, xforms[i])
		if mm.use_colors:
			mm.set_instance_color(i, colors[i])
	var mmi := MultiMeshInstance3D.new()
	mmi.name = nm
	mmi.multimesh = mm
	if not shadows:
		mmi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(mmi)
	return mmi


# ------------------------------------------- API da raia TERRENO (GDD §14)

func tree_count() -> int:
	return _tree_xforms.size()


func tree_pos(i: int) -> Vector3:
	return _tree_xforms[i].origin


## Fogo consumiu a arvore `i`: a copa SOME do mesh (instancia escondida),
## sobra o toco de carvao e o tronco DEIXA de colidir — queimar a floresta
## abre caminho e linha de tiro (e' a jogada do §14). `burned=false` restaura
## (porta do restart: estado nao atravessa partida).
func set_tree_burned(i: int, burned: bool) -> void:
	if _forest_mm == null or i < 0 or i >= _tree_xforms.size():
		return
	if burned:
		_burned_trees[i] = true
	else:
		_burned_trees.erase(i)
	var hid := _hidden_xf()
	_forest_mm.set_instance_transform(i, hid if burned else _tree_xforms[i])
	var stump := Transform3D(Basis(Vector3.UP, float(i) * 2.39996), _tree_xforms[i].origin)
	_stump_mm.set_instance_transform(i, stump if burned else hid)
	if i < _tree_shapes.size():
		_tree_shapes[i].disabled = burned


func is_tree_burned(i: int) -> bool:
	return _burned_trees.has(i)


## MultiMesh nao tem visibilidade por instancia: esconder = escala ~0 no fundo.
func _hidden_xf() -> Transform3D:
	return Transform3D(Basis.IDENTITY.scaled(Vector3.ONE * 0.001), Vector3(0, -60, 0))


func _stump_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var char_col := Color(0.13, 0.11, 0.1)
	_frustum(st, Vector3.ZERO, 0.34, 0.2, 0.9, 5, char_col)
	# tampa com miolo de brasa apagada (le' como carvao, nao como buraco)
	st.set_color(Color(0.24, 0.14, 0.09))
	for i in 5:
		var a0 := TAU * i / 5.0
		var a1 := TAU * (i + 1) / 5.0
		st.add_vertex(Vector3(0, 0.9, 0))
		st.add_vertex(Vector3(cos(a0) * 0.2, 0.9, sin(a0) * 0.2))
		st.add_vertex(Vector3(cos(a1) * 0.2, 0.9, sin(a1) * 0.2))
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon)
	return m


## Quad com winding CW (frente Godot): tris (p0,p1,p2) e (p0,p2,p3).
func _quad(st: SurfaceTool, p0: Vector3, p1: Vector3, p2: Vector3, p3: Vector3, col: Color) -> void:
	st.set_color(col)
	st.add_vertex(p0)
	st.add_vertex(p1)
	st.add_vertex(p2)
	st.add_vertex(p0)
	st.add_vertex(p2)
	st.add_vertex(p3)


## Tronco de cone facetado (so' as laterais), base em `base`, frente pra fora.
func _frustum(st: SurfaceTool, base: Vector3, r0: float, r1: float, h: float, sides: int, col: Color) -> void:
	for i in sides:
		var a0 := TAU * i / sides
		var a1 := TAU * (i + 1) / sides
		_quad(st,
			base + Vector3(cos(a0) * r0, 0, sin(a0) * r0),
			base + Vector3(cos(a1) * r0, 0, sin(a1) * r0),
			base + Vector3(cos(a1) * r1, h, sin(a1) * r1),
			base + Vector3(cos(a0) * r1, h, sin(a0) * r1), col)


## Blob icosaedrico facetado com jitter — copa de arvore / rocha.
func _blob(st: SurfaceTool, center: Vector3, scl: Vector3, col: Color, rng: RandomNumberGenerator, jit: float) -> void:
	var vs: Array[Vector3] = []
	for v in ICO_V:
		vs.append(center + v.normalized() * (1.0 + rng.randf_range(-jit, jit * 1.2)) * scl)
	st.set_color(col)
	for f in ICO_F:
		st.add_vertex(vs[f[0]])
		st.add_vertex(vs[f[1]])
		st.add_vertex(vs[f[2]])


## Caixa facetada centrada na origem, half-extents `he`, winding CW-frente.
func _box(st: SurfaceTool, he: Vector3, col: Color) -> void:
	var a := Vector3(-he.x, -he.y, he.z)
	var b := Vector3(he.x, -he.y, he.z)
	var c := Vector3(he.x, he.y, he.z)
	var d := Vector3(-he.x, he.y, he.z)
	var e2 := Vector3(-he.x, -he.y, -he.z)
	var f := Vector3(he.x, -he.y, -he.z)
	var g := Vector3(he.x, he.y, -he.z)
	var hh := Vector3(-he.x, he.y, -he.z)
	_quad(st, d, c, b, a, col)      # +Z
	_quad(st, g, hh, e2, f, col)    # -Z
	_quad(st, c, g, f, b, col)      # +X
	_quad(st, hh, d, a, e2, col)    # -X
	_quad(st, hh, g, c, d, col)     # +Y
	_quad(st, a, b, f, e2, col)     # -Y
