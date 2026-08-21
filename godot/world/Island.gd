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
const COL_SAND := Color("dcc9a0")
const COL_ROCK := Color("8e97ad")
const COL_MUD := Color("a8763e")     # Terra
const COL_TRUNK := Color("8a7259")   # clareado no G3: em LINEAR o 7a5230 antigo
                                    # virava um marrom-sangue que puxava a vista
const COL_LEAF_A := Color("3fa85c")
const COL_LEAF_B := Color("6fd177")
const COL_STONE := Color("b3bccf")
const COL_REED := Color("8fe8c9")    # Vento
# G4 — as cores que o chao NAO tinha. O defeito medido no render: a ilha inteira
# era UM verde so' (#58bd6d) com manchinhas de ruido; de longe le' como feltro.
# Campina de verdade tem faixa seca, faixa vicosa e chao de mata mais escuro.
const COL_GRASS_DRY := Color("a9c268")   # capim seco das encostas ao sol
const COL_GRASS_DEEP := Color("2f8a55")  # verde fundo das dobras
const COL_FOREST_FLOOR := Color("5f7742") # serrapilheira sob a copa
const COL_MOSS := Color("74a06a")        # musgo nas ruinas
const COL_PEBBLE := Color("8d8a84")

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
var _toon_terrain: ShaderMaterial   # chao: mais bandas + manchas de ruido
var _toon_stone: ShaderMaterial     # pedra/ruina: sheen duro no topo
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
	# 3 materiais, 1 SHADER — duplicate() nao recompila nada, so' troca uniforms
	# (uma mudanca de estado por draw call, nao um programa novo na GPU).
	# Chao: superficie enorme e continua, entao mais bandas + manchas de ruido —
	# e' o que tira a cara de "plastico pintado" sem custar textura.
	_toon_terrain = _toon.duplicate()
	_toon_terrain.set_shader_parameter("bands", 4.0)
	_toon_terrain.set_shader_parameter("band_soft", 0.22)
	_toon_terrain.set_shader_parameter("macro_noise", 0.11)
	_toon_terrain.set_shader_parameter("macro_scale", 0.19)
	_toon_terrain.set_shader_parameter("rim_strength", 0.06)  # chao nao tem silhueta
	# Pedra: sheen duro no topo do bloco = a unica coisa que vende "material duro"
	# num toon; a ruina para de parecer papelao recortado.
	_toon_stone = _toon.duplicate()
	# sheen 0.18 com corte 0.30-0.45 desenhava um OVALO branco chapado no topo de
	# cada bloco — de longe leem-se como adesivos. Menos forca, corte mais macio.
	_toon_stone.set_shader_parameter("sheen", 0.11)
	_toon_stone.set_shader_parameter("sheen_sharp", 24.0)
	_toon_stone.set_shader_parameter("macro_noise", 0.09)
	_toon_stone.set_shader_parameter("macro_scale", 1.6)
	# O MAR e' fundo de quadro: celula de ~4m e contraste BAIXO (0.5). Ele ocupa
	# meia tela em metade dos angulos — se ele gritar, o combate perde leitura.
	# O LAGO e o ALAGADO sao POI: celula de ~2m e contraste cheio, cor mais funda
	# (o azul claro de antes virava lilas pastel debaixo do ACES).
	# band_scale calibrado pra CELULA do value noise: tamanho = 1/(0.4*band_scale).
	# Mar 5m (marola larga de fundo), lago 2,2m, alagado 1,8m (agua parada, ondinha
	# curta). Contraste: mar 0.5 — ele ocupa meia tela e nao pode competir com o
	# combate; POIs em contraste cheio.
	_water_sea = _water_mat(Color("2f8fe0"), Color("0d3576"), 0.16, 0.0, 0.5, 0.5)
	_water_lake = _water_mat(Color("35b0f2"), Color("0f4f9e"), 0.07, 1.0, 1.15, 1.0)
	_water_marsh = _water_mat(Color("4ba589"), Color("1e6b62"), 0.04, 1.0, 1.4, 0.85)
	var grass_sh := load("res://world/grass.gdshader")
	_grass_mat = ShaderMaterial.new()
	_grass_mat.shader = grass_sh
	_flower_mat = ShaderMaterial.new()
	_flower_mat.shader = grass_sh
	_flower_mat.set_shader_parameter("sway", 0.05)
	_flower_mat.set_shader_parameter("blade_height", 0.34)  # flor e' baixa: o
	# vento tem que pegar a corola inteira, nao so' os 50% de cima da lamina
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
	_build_stacks(gen)
	_build_pebbles(gen)
	_build_bushes(gen)
	_build_grass(gen)
	_build_flowers(gen)
	_build_fireflies(gen)
	_build_motes(gen)
	_build_mist(gen)
	_snap_spawns()


# ---------------------------------------------------------------- relevo

func height(x: float, z: float) -> float:
	var p := Vector2(x, z)
	var r := p.length()
	# COSTA IRREGULAR (G4). `fall` era funcao do raio PURO, entao a ilha era um
	# disco e a praia um anel de contorno perfeito — de cima lia como bolo. Um
	# raio "efetivo" deformado por ruido (~+-11%) cria enseada e ponta sem tocar
	# em mais nada: e' o mesmo custo (uma amostra de ruido) e a silhueta do mapa
	# deixa de ser um circulo. So' o RAIO DA COSTA usa isto; o vale central
	# continua no raio real (a arena precisa ser previsivel).
	var redge := r * (1.0 + 0.11 * _noise.get_noise_2d(x * 2.2 + 700.0, z * 2.2 - 300.0))
	var fall := 1.0 - smoothstep(52.0, 82.0, redge)
	var n := _noise.get_noise_2d(x, z) * 0.5 + 0.5
	var h := fall * (1.8 + 7.2 * n)               # colinas
	# ONDULACAO CURTA (G4). A unica oitava do relevo tinha 50m de comprimento de
	# onda: entre uma colina e outra o chao era um plano liso, e plano liso e' o
	# que faz o terreno parecer maquete por baixo de qualquer grama. Esta oitava
	# tem ~12m e +-0,5m — visivel a pe', mansa o bastante pra ninguem tropecar, e
	# bem acima do Nyquist da malha (quad de 1,8m). Morre na praia junto com `fall`.
	h += fall * _noise.get_noise_2d(x * 4.0 + 300.0, z * 4.0 - 120.0) * 0.5
	var valley := 1.0 - smoothstep(4.0, 30.0, r)  # vale central
	h -= 2.8 * valley
	if valley > 0.0:
		h = maxf(h, 1.05)                         # vale nunca afunda no mar
	# BARRANCO -> PRAIA (G4). A cava do lago descia de 9,4m a 25m do centro: 0,47m
	# de queda por metro andado. Numa rampa dessas a faixa de areia inteira cabe em
	# 1,5m de chao e a margem vira uma LINHA, por mais macia que a mistura de cor
	# seja. Alargando a cava (5,9m a 31m) a queda cai pela metade, a areia ganha
	# largura e o lago passa a ter beira que da' pra pisar — que e' o que o §14 quer
	# quando a agua congela e vira rota.
	h = lerpf(h, -2.4, 1.0 - smoothstep(LAKE_R * 0.3, LAKE_R + 16.0, p.distance_to(LAKE)))
	var dm := p.distance_to(MARSH)
	h = lerpf(h, 0.45, 0.92 * (1.0 - smoothstep(MARSH_R * 0.6, MARSH_R + 12.0, dm)))
	h -= 0.5 * (1.0 - smoothstep(4.0, 10.0, dm))  # poca central do alagado
	h = lerpf(h, 6.4, 1.0 - smoothstep(RUINS_R * 0.6, RUINS_R + 11.0, p.distance_to(RUINS)))
	h -= 3.2 * smoothstep(70.0, 90.0, redge)      # borda mergulha no mar
	return h


## COR DO CHAO (reescrita no G4). O que havia antes era uma escada de `if`: cada
## material entrava CHAPADO e a fronteira entre eles era uma linha de 1 vertice.
## No render de calibragem isso aparecia como (a) um anel laranja perfeito em volta
## do lago, (b) uma cratera vermelha de borda dura no alagado e (c) uma ilha de UM
## verde so'. Linha dura entre materiais e' o tell nº1 de terreno procedural.
## Agora todo material entra por PESO em rampa (smoothstep) sobre uma campina que
## ja' varia sozinha — e as rampas se somam, entao lugar nenhum tem fronteira.
## Custo: roda 10.201 vezes no load e ZERO em runtime (vai gravada no vertice).
func _vcolor(x: float, z: float, h: float, ny: float) -> Color:
	var p := Vector2(x, z)
	# CALIBRAGEM (medida, nao chutada): FastNoiseLite simplex quase nunca sai da
	# faixa 0,25-0,75 depois do *0.5+0.5. A primeira versao usava smoothstep(0.54,
	# 0.95) e portanto NUNCA disparava — o chao continuou de um verde so'. As
	# rampas abaixo vivem dentro da faixa REAL do ruido.
	# Escalas: `big` sai em ~33m (mancha de regiao) e `fine` em ~6m (granulado).
	var big := _noise.get_noise_2d(x * 1.5, z * 1.5) * 0.5 + 0.5
	var fine := _noise.get_noise_2d(x * 8.0 + 90.0, z * 8.0 - 40.0) * 0.5 + 0.5

	# 1) campina: verde por altitude + faixa seca no alto/exposto + fundo escuro
	var c := COL_GRASS_HI.lerp(COL_GRASS, clampf((h - 1.0) / 7.0, 0.0, 1.0))
	c = c.lerp(COL_GRASS_DRY, smoothstep(0.50, 0.72, big) * 0.7)
	c = c.lerp(COL_GRASS_DEEP, smoothstep(0.50, 0.28, big) * 0.6)
	c = c.lerp(c.darkened(0.14), fine * 0.5)

	# 2) POI floresta: chao de mata mais escuro e terroso — identidade do lugar de
	#    graca (o render mostrava a floresta com o MESMO verde da campina aberta).
	var wf := 1.0 - smoothstep(FOREST_R * 0.3, FOREST_R + 9.0, p.distance_to(FOREST))
	c = c.lerp(COL_FOREST_FLOOR, wf * (0.42 + 0.34 * fine))

	# 3) POI ruinas: piso de pedra FRIO tomado por musgo. A "luz propria" do POI
	#    nao precisa de uma luz — precisa de um chao com outro matiz.
	var wr := 1.0 - smoothstep(RUINS_R * 0.75, RUINS_R + 4.0, p.distance_to(RUINS))
	c = c.lerp(COL_ROCK.lerp(COL_MOSS, 0.1 + 0.45 * fine), wr * 0.92)

	# 4) POI alagado: lama entrando em rampa larga e SO' na cota baixa
	# Mesmo raciocinio da praia: das duas rampas que fecham a lama, a de COTA era a
	# estreita (1,4m). Na beira do brejo o terreno sobe rapido e a lama inteira
	# aparecia em dois vertices. Alargada para 2,8m de cota, a borda vira faixa.
	var wm := (1.0 - smoothstep(MARSH_R * 0.2, MARSH_R + 10.0, p.distance_to(MARSH))) 			* smoothstep(3.0, 0.2, h)
	var wmud := minf(wm * 1.5, 1.0)
	c = c.lerp(COL_MUD.darkened(0.2), wmud)

	# 5) rocha por INCLINACAO e por altitude, as duas em rampa
	var wrk := maxf(smoothstep(0.74, 0.60, ny), smoothstep(6.4, 8.4, h))
	c = c.lerp(COL_ROCK, wrk * 0.9)

	# 6) areia da praia, tambem em rampa; o alagado nao vira praia
	# Rampa LARGA (2,4m -> 0,5m de cota) E com o limiar puxado por ruido: sem o
	# ruido a linha da areia e' uma curva de nivel perfeita, que o olho le' como
	# contorno de mapa topografico. Com ele a praia sobe em ponta e recua em
	# enseada. A antiga rampa tinha 1,15m e desenhava um anel laranja fechado.
	# O supressor e' o peso da LAMA AO QUADRADO. Com (1-wm) linear o alagado, que
	# tambem e' cota baixa, levava ~38% de areia por cima da lama e o resultado era
	# um anel BEGE: o POI perdia a cara de brejo e virava outra praia. Ao quadrado,
	# lama e praia param de disputar o mesmo pixel.
	var beach := 3.2 + 1.0 * (_noise.get_noise_2d(x * 2.6 - 55.0, z * 2.6 + 210.0))
	var dry := (1.0 - wmud) * (1.0 - wmud)
	# DOIS degraus de praia, nao um. Grama e areia sao as duas cores mais distantes
	# da paleta: com uma rampa so', por mais macia que seja, ela precisa vencer toda
	# essa distancia dentro da faixa de cota em que a areia existe — e na beira do
	# lago essa faixa cabe em 2 vertices. Passando por um meio-termo (capim ralo em
	# areia) a mesma viagem se espalha pelo dobro de cota — ver o teste
	# [transicao de material] do selftest, que compara com a regra do G3.
	var meio := COL_SAND.lerp(c, 0.45)
	c = c.lerp(meio, smoothstep(beach, 0.9, h) * dry)
	c = c.lerp(COL_SAND, smoothstep(1.7, 0.15, h) * dry)
	# 7) faixa MOLHADA na linha d'agua: areia perto do nivel do mar escurece. E' o
	#    detalhe que separa "praia desenhada" de "praia molhada" e custa um lerp.
	return c.lerp(c.darkened(0.28), smoothstep(1.15, 0.0, h))


## AO DE VERTICE (G3) — o substituto barato do SSAO, que NAO existe no renderer
## mobile. Duas contas, ambas no load (custo de runtime: ZERO):
##  * cavidade: se a media da vizinhanca de 2,5m esta' ACIMA deste ponto, o ponto
##    e' fundo de dobra -> escurece. E' o que da' fundo de vale, pe' de barranco e
##    a cava do alagado sem uma unica textura.
##  * encosta: quanto mais deitada a normal, menos ceu ela ve' -> escurece um fio.
## Sem isso o terreno le' chapado mesmo com a luz certa (era o defeito de G2).
## DUAS escalas porque o relevo tem duas: 2,5m pega a CRISTA/dobra (borda do
## lago, degrau do plato das ruinas) e 9m pega a BACIA (o vale central, a cava do
## alagado). Medido nesta ilha: a 2,5m a concavidade vai a 0,39m e a 9m vai a
## 2,2m — com um so' raio o AO sairia com 5% de amplitude, ou seja, invisivel.
## O corte da encosta usa a faixa REAL de normais do terreno (ny nunca cai de
## 0,73 aqui); a faixa "de manual" 0,3-0,9 nao encostaria em nada.
func _terrain_ao(x: float, z: float, h: float, ny: float) -> float:
	var e := 2.5
	var w := 9.0
	var near := (height(x - e, z) + height(x + e, z) + height(x, z - e) + height(x, z + e)) * 0.25 - h
	var wide := (height(x - w, z) + height(x + w, z) + height(x, z - w) + height(x, z + w)) * 0.25 - h
	var cavity := clampf(1.0 - maxf(near, 0.0) * 0.45 - maxf(wide, 0.0) * 0.07, 0.66, 1.0)
	return cavity * lerpf(0.80, 1.0, smoothstep(0.70, 0.97, ny))


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
			var c := _lin(_vcolor(x, z, h, nrm.y))
			var ao := _terrain_ao(x, z, h, nrm.y)   # AO multiplica em LINEAR
			cols[i] = Color(c.r * ao, c.g * ao, c.b * ao)
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
	mesh.surface_set_material(0, _toon_terrain)
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

func _water_mat(shallow: Color, deep: Color, wave: float, edge: float,
		band := 1.0, contrast := 1.0) -> ShaderMaterial:
	var m := ShaderMaterial.new()
	m.shader = load("res://world/water.gdshader")
	m.set_shader_parameter("shallow_color", shallow)
	m.set_shader_parameter("deep_color", deep)
	m.set_shader_parameter("wave_height", wave)
	m.set_shader_parameter("edge_foam", edge)
	m.set_shader_parameter("band_scale", band)
	m.set_shader_parameter("band_contrast", contrast)
	return m


func _build_water(parent: Node3D) -> void:
	var sea := PlaneMesh.new()
	sea.size = Vector2(520, 520)
	sea.subdivide_width = 40
	sea.subdivide_depth = 40
	sea.material = _water_sea
	_add_mesh(parent, sea, Transform3D(Basis.IDENTITY, Vector3(0, SEA_Y, 0)), "Sea", false)
	_add_mesh(parent, _disc_mesh(18.0, _water_lake, 0.08),
			Transform3D(Basis.IDENTITY, Vector3(LAKE.x, 0.6, LAKE.y)), "LakeWater", false)
	_add_mesh(parent, _disc_mesh(15.0, _water_marsh, 0.12),
			Transform3D(Basis.IDENTITY, Vector3(MARSH.x, 0.55, MARSH.y)), "MarshWater", false)


## Disco em aneis; COLOR.r = fracao do raio (0 centro, 1 borda) — os shaders
## de agua/nevoa usam isso p/ espuma de margem e fade de borda (G2).
## `wobble` deforma o RAIO por angulo (duas harmonicas fixas — deterministico e
## sem ruido nenhum). O render de calibragem entregou o defeito: o lago era uma
## ELIPSE perfeita com um anel de areia perfeito em volta. Contorno perfeito e'
## a assinatura de "isso foi carimbado por um script". 8% de deformacao ja' basta
## pro olho parar de fechar a elipse.
func _disc_mesh(radius: float, mat: Material, wobble := 0.0) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var segs := 32
	var rings := [0.3, 0.55, 0.8, 1.0]
	for i in segs:
		var a0 := TAU * i / segs
		var a1 := TAU * (i + 1) / segs
		var w0 := 1.0 + wobble * (sin(a0 * 3.0 + 0.7) * 0.6 + sin(a0 * 7.0 + 2.1) * 0.4)
		var w1 := 1.0 + wobble * (sin(a1 * 3.0 + 0.7) * 0.6 + sin(a1 * 7.0 + 2.1) * 0.4)
		var r0: float = rings[0] * radius
		st.set_color(Color(0, 0, 0))
		st.add_vertex(Vector3.ZERO)
		st.set_color(Color(rings[0], 0, 0))
		st.add_vertex(Vector3(cos(a0) * r0 * w0, 0, sin(a0) * r0 * w0))
		st.add_vertex(Vector3(cos(a1) * r0 * w1, 0, sin(a1) * r0 * w1))
		for k in rings.size() - 1:
			var ri: float = rings[k] * radius
			var ro: float = rings[k + 1] * radius
			var ci := Color(rings[k], 0, 0)
			var co := Color(rings[k + 1], 0, 0)
			var i0 := Vector3(cos(a0) * ri * w0, 0, sin(a0) * ri * w0)
			var o0 := Vector3(cos(a0) * ro * w0, 0, sin(a0) * ro * w0)
			var o1 := Vector3(cos(a1) * ro * w1, 0, sin(a1) * ro * w1)
			var i1 := Vector3(cos(a1) * ri * w1, 0, sin(a1) * ri * w1)
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
	while xf.size() < 58 and tries < 1600:
		tries += 1
		var p := FOREST + Vector2(rng.randf_range(-1, 1), rng.randf_range(-1, 1)) * FOREST_R
		if p.distance_to(FOREST) > FOREST_R:
			continue
		var h := height(p.x, p.y)
		if h < 1.2 or h > 7.4:
			continue
		var ok := true
		for t in xf:
			if Vector2(t.origin.x, t.origin.z).distance_to(p) < 4.8:
				ok = false
				break
		if ok:
			xf.append(_tree_xform(rng, p, h))
	# arvores avulsas fora da mata fechada
	tries = 0
	var extra := 0
	while extra < 10 and tries < 900:
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
	# TINTA POR ARVORE (G4). No render as 67 arvores eram clones exatos — a mata
	# lia como carimbo repetido. Cor por instancia e' de graca (o MultiMesh ja'
	# carrega o buffer) e quebra a repeticao mais do que escala nova quebraria:
	# umas puxam pro amarelo-sol, outras pro verde-frio de dentro da mata.
	var tcol := PackedColorArray()
	for i in xf.size():
		var v := rng.randf_range(-0.15, 0.15)
		tcol.append(Color(1.0 + v * 1.5, 1.0 + v * 0.5, 1.0 - v * 0.9))
	_forest_mm = _multimesh(parent, _tree_mesh(), xf, "Forest", true, tcol).multimesh
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
	var s := rng.randf_range(0.75, 1.16)
	var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)) \
			.scaled(Vector3(s, rng.randf_range(0.95, 1.22) * s, s))
	return Transform3D(b, Vector3(p.x, h - 0.1, p.y))


func _tree_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var rng := RandomNumberGenerator.new()
	rng.seed = 11
	_frustum(st, Vector3.ZERO, 0.32, 0.22, 3.35, 5, COL_TRUNK, COL_TRUNK.lightened(0.12))
	_blob(st, Vector3(0, 4.35, 0), Vector3(1.9, 1.35, 1.9), COL_LEAF_A, rng, 0.20)
	_blob(st, Vector3(0.62, 5.05, 0.35), Vector3(1.08, 0.82, 1.08), COL_LEAF_B, rng, 0.18)
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

	# desgaste por peca: pedra que envelheceu junto nunca envelhece IGUAL
	var ccol := PackedColorArray()
	for i in cols.size():
		var v := rng.randf_range(-0.13, 0.09)
		ccol.append(Color(1.0 + v * 0.8, 1.0 + v * 0.9, 1.0 + v))
	var bcol := PackedColorArray()
	for i in blks.size():
		var v := rng.randf_range(-0.16, 0.08)
		bcol.append(Color(1.0 + v * 0.8, 1.0 + v * 0.9, 1.0 + v))
	_multimesh(parent, _column_mesh(), cols, "RuinColumns", true, ccol)
	_multimesh(parent, _block_mesh(), blks, "RuinBlocks", true, bcol)
	parent.add_child(body)


func _column_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	_frustum(st, Vector3.ZERO, 0.58, 0.5, 3.0, 6, COL_STONE)
	# tampa do topo
	st.set_color(_lin(COL_STONE))
	for i in 6:
		var a0 := TAU * i / 6.0
		var a1 := TAU * (i + 1) / 6.0
		st.add_vertex(Vector3(0, 3.0, 0))
		st.add_vertex(Vector3(cos(a0) * 0.5, 3.0, sin(a0) * 0.5))
		st.add_vertex(Vector3(cos(a1) * 0.5, 3.0, sin(a1) * 0.5))
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon_stone)
	return m


func _block_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	_box(st, Vector3(1.1, 0.45, 0.5), COL_STONE.darkened(0.1))
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon_stone)
	return m


# ---------------------------------------------------------------- baixada

func _build_marsh(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 41
	var xf: Array[Transform3D] = []
	var cols := PackedColorArray()
	var tries := 0
	while xf.size() < 80 and tries < 1600:
		tries += 1
		var p := MARSH + Vector2(rng.randf_range(-1, 1), rng.randf_range(-1, 1)) * (MARSH_R - 1.0)
		if p.distance_to(MARSH) > MARSH_R - 1.0:
			continue
		var h := height(p.x, p.y)
		if h < 0.25 or h > 0.85:
			continue
		var s := rng.randf_range(0.6, 1.45)
		# tombo de ate' 12 graus: junco em pe' PERFEITO vira grade de cerca
		var b := Basis.from_euler(Vector3(rng.randf_range(-0.21, 0.21),
				rng.randf_range(0.0, TAU), rng.randf_range(-0.21, 0.21))) \
				.scaled(Vector3(s, s * rng.randf_range(0.75, 1.35), s))
		xf.append(Transform3D(b, Vector3(p.x, h - 0.05, p.y)))
		var v := rng.randf_range(-0.22, 0.16)
		cols.append(Color(1.0 + v * 0.6, 1.0 + v, 1.0 + v * 0.8))
	_multimesh(parent, _reed_mesh(), xf, "Reeds", false, cols)


## Junco AFUNILADO. Antes eram dois retangulos de largura constante — no render
## viravam tiras de papel mentol espetadas na lama. Base larga, ponta quase nula
## e um leve arco: a silhueta passa a ser vegetal.
func _reed_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	# O #8FE8C9 puro (Vento, GDD §10) num junco INTEIRO virava uma lasca de mentol
	# fluorescente no render — a cor do elemento vale como ACENTO, nao como corpo.
	var body := COL_REED.lerp(Color("2f6b4f"), 0.62)
	var tip := COL_REED.lerp(Color("3f7d63"), 0.3)
	for k in 3:
		var ang := TAU * float(k) / 3.0
		var d := Vector3(cos(ang), 0, sin(ang))
		var lean := Vector3(cos(ang + 1.1), 0, sin(ang + 1.1)) * 0.22
		var wb := d * 0.22
		var wt := d * 0.045
		var top := lean + Vector3(0, 1.35 + 0.22 * float(k), 0)
		st.set_color(_lin(body))
		st.add_vertex(-wb)
		st.add_vertex(wb)
		st.set_color(_lin(tip))
		st.add_vertex(top + wt)
		st.set_color(_lin(body))
		st.add_vertex(-wb)
		st.set_color(_lin(tip))
		st.add_vertex(top + wt)
		st.add_vertex(top - wt)
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
	m.surface_set_material(0, _toon_stone)
	return m


# ---------------------------------------------------------------- vida (G2)

## sRGB -> LINEAR. O DEFEITO MAIS CARO que este mundo tinha e nao sabia: os hex
## da paleta (GDD §10) sao sRGB, mas COLOR de vertice chega no shader SEM
## conversao (so' BaseMaterial3D tem o vertex_color_is_srgb; shader nao tem).
## Resultado: #58bd6d entrava como albedo LINEAR 0.84 de verde — quase o dobro do
## que o hex significa. Tudo saia claro, lavado e dessaturado, e a agua era a
## unica coisa com cor certa no quadro porque uniform `source_color` o Godot ja'
## converte sozinho. Converter aqui custa ZERO em runtime (roda no load) —
## converter no shader custaria 3 pow por pixel.
func _lin(c: Color) -> Color:
	return c.srgb_to_linear()


## Normal.y aproximada do terreno (mesmo esquema do _build_terrain).
func _terrain_ny(x: float, z: float) -> float:
	var e := 0.6
	return Vector3(height(x - e, z) - height(x + e, z), 2.0 * e,
			height(x, z - e) - height(x, z + e)).normalized().y


## Onde tufo/moita/pedrinha pode nascer: campina aberta, fora d'agua e fora de
## ladeira. Uma funcao so' porque tres scatters diferentes fazem a MESMA pergunta.
func _open_ground(p: Vector2, hmin := 1.05, hmax := 7.3, nymin := 0.66) -> float:
	if p.length() > 74.0 or p.distance_to(LAKE) < LAKE_R + 2.0 \
			or p.distance_to(MARSH) < MARSH_R:
		return -1.0
	var h := height(p.x, p.y)
	if h < hmin or h > hmax or _terrain_ny(p.x, p.y) < nymin:
		return -1.0
	return h


## GRAMA EM MOITA (G4). O render de calibragem mostrou o defeito de frente: 3000
## tufos com sorteio UNIFORME leem como cones de transito num campo de golfe — o
## olho acha a grade antes de achar o mato. Mato nasce em moita, e' isso que da'
## a mancha irregular que o cerebro le' como "campo". Aqui 80% dos tufos caem em
## volta de uma das ~210 sementes e o resto fica esparso.
## E o que importa pro ORCAMENTO: a grama sai em GRADE de MultiMeshes (6x6).
## Antes era UM MultiMesh com a AABB da ilha inteira — nunca era cortado pelo
## frustum, logo TODOS os 3.000 tufos passavam pelo vertex shader todo frame,
## inclusive os que estavam atras da camera. Fatiado em 36 celulas de 30m, uma
## camera de 3a pessoa com fade em 46m enxerga 6-9 celulas.
## A CONTA (e' ela que autoriza a densidade): antes 3.000 x 12 verts = 36k por
## frame, SEMPRE. Agora 11.000 tufos x 18 verts, mas so' ~2.500-3.300 instancias
## caem no frustum -> 45k-60k por frame. ~1,5x o custo de vertice por 3,7x de
## densidade. Se o aparelho reclamar, o botao e' a CONTAGEM (11000 -> 7000) e
## depois o fade_start do grass.gdshader — nao a grade, que so' economiza.
func _build_grass(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 61
	var seeds: Array[Vector2] = []
	var tries := 0
	while seeds.size() < 430 and tries < 6000:
		tries += 1
		var sp := Vector2(rng.randf_range(-72, 72), rng.randf_range(-72, 72))
		if _open_ground(sp) >= 0.0:
			seeds.append(sp)
	var xf: Array[Transform3D] = []
	var cols := PackedColorArray()
	tries = 0
	while xf.size() < 11000 and tries < 60000:
		tries += 1
		var p: Vector2
		if not seeds.is_empty() and rng.randf() < 0.88:
			var a := rng.randf_range(0.0, TAU)
			# raiz quadrada = densidade parelha dentro da moita (sem ela vira alvo
			# de tiro ao prato: um caroco no centro e nada na beirada).
			# Raio CURTO (1,2-3,0m): com moita de 4,6m os 20 tufos dela se diluiam
			# em 66m2 e a moita nao existia — so' havia chuvisco outra vez.
			var r := sqrt(rng.randf()) * rng.randf_range(1.2, 3.0)
			p = seeds[rng.randi_range(0, seeds.size() - 1)] + Vector2(cos(a), sin(a)) * r
		else:
			p = Vector2(rng.randf_range(-74, 74), rng.randf_range(-74, 74))
		var h := _open_ground(p)
		if h < 0.0:
			continue
		var sc := rng.randf_range(0.6, 1.12)
		var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)) \
				.scaled(Vector3(sc, rng.randf_range(0.75, 1.3) * sc, sc))
		xf.append(Transform3D(b, Vector3(p.x, h - 0.06, p.y)))
		var v := rng.randf_range(-0.14, 0.14)
		cols.append(Color(1.0 + v * 0.9, 1.0 + v, 1.0 + v * 0.5))
	_multimesh_grid(parent, _grass_mesh(), xf, cols, "Grass", 6)


## Tufo de 3 LAMINAS finas e tortas (era 2 quads largos cruzados — geometria de
## PIRAMIDE, e piramide e' exatamente o que aparecia no render). Fina, com queda
## pro lado e alturas diferentes: a silhueta vira mato.
## A ponta deixou de ser (0.5,0.86,0.45): esse verde era ~40% mais claro que o
## chao em LINEAR, entao cada tufo virava um OBJETO claro sobre o gramado em vez
## de virar TEXTURA do gramado. Agora a ponta encosta no tom do chao.
func _grass_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var rng := RandomNumberGenerator.new()
	rng.seed = 91
	var base := _lin(Color(0.10, 0.24, 0.12))   # pe' escuro = oclusao de graca
	var tip := _lin(Color(0.42, 0.71, 0.37))
	for k in 3:
		var a := TAU * float(k) / 3.0 + 0.4
		var d := Vector3(cos(a), 0, sin(a))
		var side := Vector3(-d.z, 0, d.x)
		var w := 0.085
		var hgt := rng.randf_range(0.44, 0.74)
		var lean := d * rng.randf_range(0.05, 0.16)
		var root := d * 0.05
		var b0 := root - side * w
		var b1 := root + side * w
		var t1 := root + lean + side * w * 0.2 + Vector3(0, hgt, 0)
		var t0 := root + lean - side * w * 0.2 + Vector3(0, hgt, 0)
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


## DETRITO DE CHAO (G4): cascalho e seixo. O chao aberto nao tinha NADA entre a
## lamina de grama (0,5m) e o rochedo (2m) — e' esse buraco de escala que faz
## terreno procedural parecer maquete. 300 seixos, sem sombra e sem colisao.
func _build_pebbles(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 81
	var xf: Array[Transform3D] = []
	var cols := PackedColorArray()
	var tries := 0
	while xf.size() < 190 and tries < 5000:
		tries += 1
		var p := Vector2(rng.randf_range(-76, 76), rng.randf_range(-76, 76))
		if p.length() > 76.0 or p.distance_to(LAKE) < LAKE_R - 1.0:
			continue
		var h := height(p.x, p.y)
		if h < 0.15 or h > 8.2:
			continue
		var sc := rng.randf_range(0.09, 0.26)
		var b := Basis.from_euler(Vector3(rng.randf_range(-0.4, 0.4),
				rng.randf_range(0.0, TAU), rng.randf_range(-0.4, 0.4))) \
				.scaled(Vector3(sc * rng.randf_range(1.0, 1.8), sc * 0.6, sc))
		xf.append(Transform3D(b, Vector3(p.x, h + sc * 0.15, p.y)))
		var v := rng.randf_range(-0.18, 0.18)
		cols.append(Color(1.0 + v, 1.0 + v * 0.9, 1.0 + v * 0.7))
	_multimesh(parent, _pebble_mesh(), xf, "Pebbles", false, cols)


func _pebble_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var rng := RandomNumberGenerator.new()
	rng.seed = 17
	_blob(st, Vector3.ZERO, Vector3(1, 0.7, 1), COL_PEBBLE, rng, 0.34)
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon_stone)
	return m


## MOITA (G4): o degrau de escala que faltava entre grama e arvore, e o que enche
## o chao vazio da floresta no render. 130 arbustos, concentrados na mata e na
## borda dela. Sem colisao de proposito: arbusto que trava o passo e' bug de
## movimentacao disfarcado de cenario.
func _build_bushes(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 101
	var xf: Array[Transform3D] = []
	var cols := PackedColorArray()
	var tries := 0
	while xf.size() < 130 and tries < 4000:
		tries += 1
		var p: Vector2
		if rng.randf() < 0.72:
			var a := rng.randf_range(0.0, TAU)
			p = FOREST + Vector2(cos(a), sin(a)) * (sqrt(rng.randf()) * (FOREST_R + 7.0))
		else:
			p = Vector2(rng.randf_range(-70, 70), rng.randf_range(-70, 70))
		var h := _open_ground(p, 1.15, 7.4, 0.7)
		if h < 0.0:
			continue
		var sc := rng.randf_range(0.62, 1.35)
		var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)) \
				.scaled(Vector3(sc, rng.randf_range(0.7, 1.15) * sc, sc))
		xf.append(Transform3D(b, Vector3(p.x, h - 0.12, p.y)))
		var v := rng.randf_range(-0.14, 0.14)
		cols.append(Color(1.0 - v * 0.5, 1.0 + v, 1.0 + v * 0.7))
	_multimesh(parent, _bush_mesh(), xf, "Bushes", false, cols)


func _bush_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var rng := RandomNumberGenerator.new()
	rng.seed = 23
	_blob(st, Vector3(0, 0.42, 0), Vector3(0.72, 0.5, 0.72), COL_LEAF_A.darkened(0.18), rng, 0.28)
	_blob(st, Vector3(0.34, 0.62, -0.2), Vector3(0.42, 0.34, 0.42), COL_LEAF_B.darkened(0.1), rng, 0.26)
	st.generate_normals()
	var m := st.commit()
	m.surface_set_material(0, _toon)
	return m


## ROCHEDOS NO MAR (G4). O horizonte era uma linha reta de agua listrada: sem
## nada la' fora o jogador nao tem como medir o tamanho da ilha nem a distancia,
## e a leitura de CAMADAS (perto / meio / longe) nunca fecha. 18 pedras grandes
## num anel de 92-140m, fora do alcance da sombra (60m) e sem colisao nenhuma.
## 1 draw call, ~700 tris, e o mapa ganha borda.
func _build_stacks(parent: Node3D) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 111
	var xf: Array[Transform3D] = []
	var cols := PackedColorArray()
	for i in 18:
		var a := TAU * float(i) / 18.0 + rng.randf_range(-0.14, 0.14)
		var r := rng.randf_range(92.0, 140.0)
		var sc := rng.randf_range(3.2, 9.5)
		var b := Basis(Vector3.UP, rng.randf_range(0.0, TAU)) \
				.scaled(Vector3(sc, sc * rng.randf_range(0.8, 1.9), sc))
		xf.append(Transform3D(b, Vector3(cos(a) * r, rng.randf_range(-2.6, -0.6), sin(a) * r)))
		var v := rng.randf_range(-0.1, 0.1)
		cols.append(Color(1.0 + v * 0.6, 1.0 + v * 0.8, 1.0 + v))
	_multimesh(parent, _rock_mesh(), xf, "SeaStacks", false, cols)


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
		cols.append(_lin(petals[rng.randi_range(0, petals.size() - 1)]))
	_multimesh(parent, _flower_mesh(), xf, "Flowers", false, cols)


func _flower_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var stem := _lin(Color(0.35, 0.55, 0.3))
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
	p.scale_amount_min = 0.13
	p.scale_amount_max = 0.22
	# ESFERA baixa, nao billboard quadrado. Medido a olho no render: o quad de
	# 6cm com albedo HDR passa pelo glow e o borrao herda a FORMA da fonte — o
	# vagalume virava um cubinho branco de ~15px flutuando na copa. Uma esfera de
	# 6 lados x 3 aneis (36 tris x 24 particulas = ~900 tris, nada) e' redonda de
	# qualquer angulo, dispensa a conta de billboard e o glow vira halo redondo.
	var ball := SphereMesh.new()
	ball.radius = 0.5
	ball.height = 1.0
	ball.radial_segments = 6
	ball.rings = 3
	var m := StandardMaterial3D.new()
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.vertex_color_use_as_albedo = true
	m.albedo_color = Color(2.3, 1.9, 0.7)   # dourado arcano em HDR
	ball.material = m
	p.mesh = ball
	p.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	p.position = Vector3(FOREST.x, height(FOREST.x, FOREST.y) + 2.6, FOREST.y)
	parent.add_child(p)


## DUAS camadas de bruma no alagado, em alturas e escalas diferentes. Uma camada
## so' le' como um adesivo transparente; duas, com deriva diferente, leem como AR.
## E' o unico lugar do mapa que ganha profundidade volumetrica de verdade — e sai
## por 2 discos transparentes pequenos, nao pela nevoa volumetrica (que nao existe
## no renderer mobile).
## POLEN NA CAMPINA (G4). O vale central e' onde a partida comeca e onde a
## camera passa mais tempo, e era o pedaco mais VAZIO do quadro: chao, ceu, nada
## no meio. Meia duzia de motas douradas subindo devagar poe uma camada entre o
## jogador e o horizonte — e' o truque mais barato de profundidade que existe.
## ponytail: CPUParticles como os vagalumes (driver Android previsivel), 20
## particulas, sem sombra, sem colisao. Se pesar, e' a PRIMEIRA coisa a cair.
func _build_motes(parent: Node3D) -> void:
	var p := CPUParticles3D.new()
	p.name = "Pollen"
	p.amount = 20
	p.lifetime = 11.0
	p.preprocess = 11.0
	p.randomness = 0.6
	p.emission_shape = CPUParticles3D.EMISSION_SHAPE_BOX
	p.emission_box_extents = Vector3(26.0, 3.0, 26.0)
	p.spread = 60.0
	p.gravity = Vector3(0.05, 0.09, 0.0)     # sobem no ar quente do entardecer
	p.initial_velocity_min = 0.1
	p.initial_velocity_max = 0.4
	p.scale_amount_min = 0.05
	p.scale_amount_max = 0.09
	var ball := SphereMesh.new()
	ball.radius = 0.5
	ball.height = 1.0
	ball.radial_segments = 5
	ball.rings = 3
	var m := StandardMaterial3D.new()
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.albedo_color = Color(1.9, 1.7, 1.2)    # HDR fraco: pega o glow sem virar bola
	ball.material = m
	p.mesh = ball
	p.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	p.position = Vector3(0, height(0, 0) + 3.4, 0)
	parent.add_child(p)


func _build_mist(parent: Node3D) -> void:
	_add_mesh(parent, _disc_mesh(MARSH_R + 3.0, _mist_mat, 0.14),
			Transform3D(Basis.IDENTITY, Vector3(MARSH.x, 1.05, MARSH.y)), "MarshMist", false)
	_add_mesh(parent, _disc_mesh(MARSH_R - 2.0, _mist_mat, 0.18),
			Transform3D(Basis(Vector3.UP, 1.9), Vector3(MARSH.x + 1.5, 1.75, MARSH.y - 1.0)),
			"MarshMist2", false)


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


## MultiMesh em GRADE. Existe por um motivo so': AABB. Um MultiMesh unico que
## cobre a ilha inteira NUNCA e' cortado pelo frustum — a GPU paga o vertex shader
## de TODA instancia, inclusive as que estao atras da camera. Fatiar o mesmo
## conjunto em celulas devolve o culling. Nao muda um pixel; muda o custo.
func _multimesh_grid(parent: Node3D, mesh: Mesh, xforms: Array[Transform3D],
		colors: PackedColorArray, nm: String, cells: int) -> void:
	var half := SIZE * 0.5
	var bx: Array[Array] = []
	var bc: Array[PackedColorArray] = []
	for i in cells * cells:
		bx.append([])
		bc.append(PackedColorArray())
	for i in xforms.size():
		var cx := clampi(int((xforms[i].origin.x + half) / SIZE * float(cells)), 0, cells - 1)
		var cz := clampi(int((xforms[i].origin.z + half) / SIZE * float(cells)), 0, cells - 1)
		var k := cz * cells + cx
		bx[k].append(xforms[i])
		if i < colors.size():
			bc[k].append(colors[i])
	for k in bx.size():
		if bx[k].is_empty():
			continue
		var tf: Array[Transform3D] = []
		tf.assign(bx[k])
		_multimesh(parent, mesh, tf, "%s%02d" % [nm, k], false, bc[k])


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
	st.set_color(_lin(Color(0.24, 0.14, 0.09)))
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
	st.set_color(_lin(col))
	st.add_vertex(p0)
	st.add_vertex(p1)
	st.add_vertex(p2)
	st.add_vertex(p0)
	st.add_vertex(p2)
	st.add_vertex(p3)


## Tronco de cone facetado (so' as laterais), base em `base`, frente pra fora.
## `top` (opcional) faz o tronco escurecer no PE'. O tronco era um retangulo de
## cor unica: sem sombra de contato e sem gradiente ele le' como papelao recortado
## de perto, que era exatamente a cara da floresta no render.
func _frustum(st: SurfaceTool, base: Vector3, r0: float, r1: float, h: float,
		sides: int, col: Color, top := Color(0, 0, 0, 0)) -> void:
	var cbot := col.darkened(0.22)
	var ctop := col if top.a == 0.0 else top
	for i in sides:
		var a0 := TAU * i / sides
		var a1 := TAU * (i + 1) / sides
		var lo0 := base + Vector3(cos(a0) * r0, 0, sin(a0) * r0)
		var lo1 := base + Vector3(cos(a1) * r0, 0, sin(a1) * r0)
		var hi1 := base + Vector3(cos(a1) * r1, h, sin(a1) * r1)
		var hi0 := base + Vector3(cos(a0) * r1, h, sin(a0) * r1)
		# gradiente por vertice: mesmo numero de tris, custo zero
		st.set_color(_lin(cbot))
		st.add_vertex(lo0)
		st.add_vertex(lo1)
		st.set_color(_lin(ctop))
		st.add_vertex(hi1)
		st.set_color(_lin(cbot))
		st.add_vertex(lo0)
		st.set_color(_lin(ctop))
		st.add_vertex(hi1)
		st.add_vertex(hi0)


## Blob icosaedrico facetado com jitter — copa de arvore / rocha.
func _blob(st: SurfaceTool, center: Vector3, scl: Vector3, col: Color, rng: RandomNumberGenerator, jit: float) -> void:
	var vs: Array[Vector3] = []
	for v in ICO_V:
		vs.append(center + v.normalized() * (1.0 + rng.randf_range(-jit, jit * 1.2)) * scl)
	st.set_color(_lin(col))
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
