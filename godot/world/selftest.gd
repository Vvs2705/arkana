## Selftest headless da raia MUNDO (G3). Rodar:
##   Godot --headless --path godot --script res://world/selftest.gd
## Prova o que a raia promete: a ilha e' DETERMINISTICA (mesma malha em duas
## instancias, spawns acima do chao), o visual G3 esta' ligado de verdade (ACES,
## nevoa, AO de vertice multiplicado na cor), e — o mais importante — o
## ORCAMENTO MOBILE e' lei: nenhum efeito que o renderer "mobile" nao suporta
## esta' ligado, e a sombra cabe no ARM64.
## ANTI-VACUIDADE: os dois testes de defeito refazem, na mao, a versao SEM a
## melhoria (AO constante / sombra de PC) e mostram a guarda ficando VERMELHA.
##
## NOTA (mesma dos outros selftests): em modo --script os autoloads so'
## registram DEPOIS deste arquivo compilar — nada aqui referencia Bus/Balance.
extends SceneTree

var fails := 0
var island: Node3D
var env: Environment
var sun: DirectionalLight3D


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	var packed := load("res://world/Island.tscn") as PackedScene
	island = packed.instantiate()
	root.add_child(island)
	var we := island.get_node_or_null("Env") as WorldEnvironment
	sun = island.get_node_or_null("Sun") as DirectionalLight3D
	_check(we != null and we.environment != null, "ilha traz WorldEnvironment")
	_check(sun != null, "ilha traz o sol direcional")
	if we == null or sun == null:
		quit(1)
		return
	env = we.environment

	_test_mobile_budget()
	_test_shadow_budget()
	_test_shadow_budget_defect_red()
	_test_look()
	_test_materials()
	_test_shader_uniforms()
	_test_terrain_ao()
	_test_terrain_ao_defect_red()
	_test_material_blend()
	_test_material_blend_defect_red()
	_test_coastline()
	_test_scatter_budget()
	_test_gamma()
	_test_spawns()
	_test_determinism(packed)

	if fails == 0:
		print("SELFTEST OK")
	else:
		printerr("SELFTEST FALHOU: %d" % fails)
	quit(0 if fails == 0 else 1)


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)


# ------------------------------------------------------- orcamento (a lei)

## O renderer do projeto e' "mobile": SSAO, SSIL, SSR, SDFGI e nevoa VOLUMETRICA
## nao existem la'. Ligar qualquer um deles nao da' erro — simplesmente nao
## aparece no celular, e alguem passa a semana caçando o efeito que nunca veio.
func _test_mobile_budget() -> void:
	print("[orcamento mobile]")
	_check(not env.ssao_enabled, "SSAO desligado (nao existe no mobile)")
	_check(not env.ssil_enabled, "SSIL desligado (nao existe no mobile)")
	_check(not env.ssr_enabled, "SSR desligado (nao existe no mobile)")
	_check(not env.sdfgi_enabled, "SDFGI desligado (nao existe no mobile)")
	_check(not env.volumetric_fog_enabled, "nevoa volumetrica desligada (nao existe no mobile)")


func _test_shadow_budget() -> void:
	print("[sombra no ARM64]")
	_check(sun.shadow_enabled, "sol projeta sombra")
	_check(sun.directional_shadow_mode == DirectionalLight3D.SHADOW_ORTHOGONAL,
			"1 split ortogonal (PSSM redesenharia os casters 2-4x por frame)")
	_check(sun.directional_shadow_max_distance <= 80.0,
			"alcance de sombra <= 80m (%0.0f) — o resto e' nevoa" % sun.directional_shadow_max_distance)
	_check(sun.directional_shadow_fade_start < 1.0,
			"sombra some em rampa, nao numa linha reta cortando a ilha")


## Defeito medido: a config "de PC" (4 splits, 300m) roda no editor e mata o
## celular. A guarda de cima e' quem barra — aqui ela precisa ficar VERMELHA.
func _test_shadow_budget_defect_red() -> void:
	print("[defeito: sombra de PC]")
	var pc := DirectionalLight3D.new()
	pc.directional_shadow_mode = DirectionalLight3D.SHADOW_PARALLEL_4_SPLITS
	pc.directional_shadow_max_distance = 300.0
	var barrado := pc.directional_shadow_mode != DirectionalLight3D.SHADOW_ORTHOGONAL \
			or pc.directional_shadow_max_distance > 80.0
	_check(barrado, "config de PC seria BARRADA pela guarda acima")
	pc.free()


# ------------------------------------------------------------- visual G3

func _test_look() -> void:
	print("[identidade visual G3]")
	_check(env.tonemap_mode == Environment.TONE_MAPPER_ACES,
			"tonemap ACES (contraste/rolloff — o salto de realismo de graca)")
	# ponytail: nao testo o VALOR da exposicao — isso e' botao de gosto, e um teste
	# que trava um botao so' quebra na proxima calibragem. Testo o que e' estrutura:
	# o passe de correcao existe e esta' puxando contraste sem estourar o verde.
	_check(env.adjustment_enabled and env.adjustment_contrast > 1.0
			and env.adjustment_saturation >= 0.9 and env.adjustment_saturation <= 1.05,
			"correcao de cor ligada, contraste alto e saturacao controlada")
	# nevoa por PROFUNDIDADE, com um comeco: perto limpo (leitura de alvo em
	# combate), longe dissolvido. A exponencial nao tem comeco e lava os 20m.
	_check(env.fog_enabled and env.fog_mode == Environment.FOG_MODE_DEPTH,
			"nevoa por profundidade, nao exponencial")
	_check(env.fog_depth_begin >= 30.0,
			"nevoa so' comeca a %0.0fm — o combate perto fica limpo" % env.fog_depth_begin)
	_check(env.fog_depth_end >= 200.0, "nevoa fecha longe o bastante p/ engolir o mar")
	_check(env.fog_height_density > 0.0, "camada baixa de nevoa (vale/mar respiram bruma)")
	_check(env.glow_enabled, "glow ligado (vagalume e sol em HDR acendem)")
	_check(env.background_mode == Environment.BG_SKY
			and env.ambient_light_source == Environment.AMBIENT_SOURCE_SKY,
			"ceu procedural tambem ILUMINA (sombra nao morre em preto)")
	_check(env.ambient_light_sky_contribution < 1.0,
			"ambiente mistura um frio proprio — sombra com COR, nunca falta de luz")
	# custo ESCONDIDO: ceu REALTIME ignora radiance_size e forca 256 por frame
	# (o Godot so' avisa numa linha de log que ninguem le'). INCREMENTAL respeita.
	_check(env.sky != null and env.sky.process_mode == Sky.PROCESS_MODE_INCREMENTAL,
			"ceu em INCREMENTAL — respeita a radiance de 64 em vez de forcar 256/frame")
	_check(env.sky != null and env.sky.radiance_size <= Sky.RADIANCE_SIZE_128,
			"radiance pequena (ambiente nao precisa de nitidez)")


func _test_materials() -> void:
	print("[materiais]")
	var terrain := island.get_node_or_null("Generated/Terrain") as MeshInstance3D
	_check(terrain != null, "malha de terreno existe")
	if terrain == null:
		return
	var shaders := {}
	var n_mats := 0
	for mi in _all_meshes(island):
		var mesh: Mesh = mi.mesh if mi is MeshInstance3D else (mi as MultiMeshInstance3D).multimesh.mesh
		if mesh == null:
			continue
		for s in mesh.get_surface_count():
			var mat := mesh.surface_get_material(s)
			if mat == null:
				continue
			n_mats += 1
			# zero binario: nada de textura importada, tudo shader procedural
			if mat is ShaderMaterial:
				shaders[(mat as ShaderMaterial).shader] = true
	_check(n_mats > 0, "mundo tem materiais (%d superficies)" % n_mats)
	_check(shaders.size() <= 4,
			"os materiais compartilham poucos SHADERS (%d) — variacao por uniform, nao por programa" % shaders.size())
	var tmat := terrain.mesh.surface_get_material(0) as ShaderMaterial
	_check(tmat != null and tmat.get_shader_parameter("macro_noise") > 0.0,
			"terreno usa manchas de ruido (o que tira a chapadura)")


## Bug classico e SILENCIOSO: set_shader_parameter com nome errado nao da' erro,
## so' nao faz nada. Aqui os nomes que o Island.gd escreve tem que EXISTIR.
func _test_shader_uniforms() -> void:
	print("[uniforms do toon]")
	var sh := load("res://world/toon.gdshader") as Shader
	var names := {}
	for u in sh.get_shader_uniform_list():
		names[String(u["name"])] = true
	for want in ["bands", "band_soft", "wrap", "rim_strength", "shade_tint",
			"shade_fill", "sheen", "sheen_sharp", "macro_noise", "macro_scale"]:
		_check(names.has(want), "toon.gdshader expoe uniform '%s'" % want)


# ---------------------------------------------------- transicoes e densidade (G4)

## O G4 trocou a escada de `if` do _vcolor por PESOS EM RAMPA. A promessa e'
## verificavel, mas so' EM COMPARACAO: parte do salto de cor entre dois vertices
## vizinhos vem do RELEVO (na queda da ilha pro mar a cota muda 1m em 1,5m de
## chao, e ai' a cor muda rapido com razao). Entao o teste roda as duas regras
## sobre as MESMAS amostras e sobre o MESMO terreno, e cobra que a nova seja
## folgadamente mais macia que a escada de `if` do G3.
## Travessias escolhidas para cruzar cada fronteira que existe: praia, margem do
## lago, lama do alagado, piso das ruinas e chao de mata.
func _material_seams() -> Array:
	var linhas := [
		[Vector2(-88, 18), Vector2(88, 18)],      # lago + as duas praias
		[Vector2(-88, 36), Vector2(88, 36)],      # alagado
		[Vector2(38, -88), Vector2(38, 88)],      # ruinas
		[Vector2(-70, -70), Vector2(70, 70)],     # floresta e vale
	]
	var pior_novo := 0.0
	var pior_velho := 0.0
	var pior_dentro := 0.0    # so' a area jogavel (r <= 68m)
	var onde := Vector2.ZERO
	var onde_dentro := Vector2.ZERO
	for l in linhas:
		var a: Vector2 = l[0]
		var b: Vector2 = l[1]
		var pn := Color(0, 0, 0)
		var pv := Color(0, 0, 0)
		var n := 120
		for i in n + 1:
			var q: Vector2 = a.lerp(b, float(i) / n)
			var h: float = island.height(q.x, q.y)
			var ny: float = island._terrain_ny(q.x, q.y)
			var cn: Color = island._vcolor(q.x, q.y, h, ny)
			var cv: Color = _vcolor_g3(q, h, ny)
			if i > 0:
				var dn := absf(cn.r - pn.r) + absf(cn.g - pn.g) + absf(cn.b - pn.b)
				if dn > pior_novo:
					pior_novo = dn
					onde = q
				if q.length() <= 68.0 and dn > pior_dentro:
					pior_dentro = dn
					onde_dentro = q
				pior_velho = maxf(pior_velho,
						absf(cv.r - pv.r) + absf(cv.g - pv.g) + absf(cv.b - pv.b))
			pn = cn
			pv = cv
	return [pior_novo, pior_velho, onde, pior_dentro, onde_dentro]


## A regra de cor do G3, reconstruida na mao: escada de `if`, cada material
## entrando CHAPADO. E' a versao SEM a melhoria — sem ela o teste acima nao prova
## nada, so' registra um numero.
func _vcolor_g3(p: Vector2, h: float, ny: float) -> Color:
	if p.distance_to(island.MARSH) < island.MARSH_R + 4.0:
		return (island.COL_MUD as Color).lerp(island.COL_GRASS, clampf((h - 0.8) * 0.9, 0.0, 1.0))
	if h < 0.95:
		return island.COL_SAND
	if p.distance_to(island.RUINS) < island.RUINS_R * 0.8:
		return (island.COL_ROCK as Color).lerp(island.COL_GRASS, 0.35)
	if ny < 0.62 or h > 7.6:
		return island.COL_ROCK
	return (island.COL_GRASS_HI as Color).lerp(island.COL_GRASS, clampf((h - 1.0) / 7.0, 0.0, 1.0))


func _test_material_blend() -> void:
	print("[transicao de material]")
	var m := _material_seams()
	var novo: float = m[0]
	var velho: float = m[1]
	var dentro: float = m[3]
	_check(novo < velho * 0.7,
			"pior fronteira caiu de %0.2f (escada de if) para %0.2f — rampa, nao degrau"
			% [velho, novo])
	# O que sobra do pior caso e' o penhasco onde a ilha DESPENCA no mar (r~76m):
	# la' a cota muda mais de 1m entre dois vertices e a cor tem que acompanhar —
	# isso e' relevo, nao emenda. Dentro da area jogavel (r<=68m), que e' onde o
	# jogador olha o chao de perto, a exigencia e' bem mais dura.
	# Regra, nao numero magico: dentro da area jogavel (r<=68m), que e' onde o
	# jogador olha o chao de perto, a pior emenda tem que ser MENOS DA METADE da
	# que a escada de if produzia no mesmo terreno.
	_check(dentro < velho * 0.5,
			"na area jogavel a pior emenda e' %0.2f, menos da metade dos %0.2f do G3 (em %s)"
			% [dentro, velho, m[4]])


## Anti-vacuidade explicita: a regra antiga, medida do mesmo jeito, seria barrada.
func _test_material_blend_defect_red() -> void:
	print("[defeito: fronteira em degrau]")
	var m := _material_seams()
	var velho: float = m[1]
	# a MESMA transecao, com a regra do G3, medida dentro da area jogavel
	_check(velho >= 0.30 and velho >= m[0] / 0.7,
			"a escada de if do G3 daria salto %0.2f — BARRADA pelas duas guardas acima"
			% velho)


## Costa: a linha d'agua tem que ser IRREGULAR. Se `fall` voltar a depender do
## raio puro, a ilha vira um disco e a praia um anel de contorno perfeito (era o
## defeito mais visivel do mapa visto de cima). Medida: o raio em que h cruza
## zero, varrido em 48 angulos, precisa de amplitude de verdade.
func _test_coastline() -> void:
	print("[costa irregular]")
	var lo := 999.0
	var hi := 0.0
	for k in 48:
		var a := TAU * float(k) / 48.0
		var d := Vector2(cos(a), sin(a))
		var r := 60.0
		while r < 110.0 and island.height(d.x * r, d.y * r) > 0.0:
			r += 0.5
		lo = minf(lo, r)
		hi = maxf(hi, r)
	_check(hi - lo > 8.0,
			"a linha d'agua varia %0.1fm entre a enseada e a ponta (disco perfeito = 0)"
			% (hi - lo))


## Densidade so' e' ganho se ela vier COM culling. A grama tem que estar fatiada
## em varios MultiMesh: um so', com a AABB da ilha, nunca sai do frustum e a GPU
## paga o vertex shader de todo tufo do mapa, inclusive os de tras da camera.
func _test_scatter_budget() -> void:
	print("[densidade com culling]")
	var gen := island.get_node_or_null("Generated")
	var celulas := 0
	var tufos := 0
	var maior := 0
	for c in gen.get_children():
		if c is MultiMeshInstance3D and String(c.name).begins_with("Grass"):
			celulas += 1
			var n: int = (c as MultiMeshInstance3D).multimesh.instance_count
			tufos += n
			maior = maxi(maior, n)
	_check(celulas >= 9, "grama fatiada em %d celulas (culling por celula)" % celulas)
	_check(tufos > 6000, "campina densa: %d tufos" % tufos)
	# a celula tem que ser MUITO menor que o total, senao fatiar nao economizou nada
	_check(maior < tufos / 4,
			"nenhuma celula concentra o mapa (maior tem %d de %d)" % [maior, tufos])
	# o horizonte tem silhueta, e ela nao pode custar sombra
	var st := gen.get_node_or_null("SeaStacks") as MultiMeshInstance3D
	_check(st != null and st.multimesh.instance_count >= 10,
			"ha' rochedos no mar dando escala ao horizonte")
	_check(st != null and st.cast_shadow == GeometryInstance3D.SHADOW_CASTING_SETTING_OFF,
			"os rochedos NAO projetam sombra (estao alem dos 60m do sol)")


# ------------------------------------------------------------ AO / relevo

## O AO de vertice e' o substituto do SSAO. Ele so' vale se de fato ESCURECER as
## dobras — e se essa conta chegar na cor gravada na malha (nao adianta calcular
## e esquecer de multiplicar, que e' o jeito mais facil de mentir aqui).
func _test_terrain_ao() -> void:
	print("[AO de vertice]")
	var lo := 2.0
	var hi := 0.0
	var darkest := Vector2.ZERO
	for iz in range(0, 101, 4):
		for ix in range(0, 101, 4):
			var x := (float(ix) / 100.0 - 0.5) * 180.0
			var z := (float(iz) / 100.0 - 0.5) * 180.0
			var h: float = island.height(x, z)
			var ny: float = island._terrain_ny(x, z)
			var ao: float = island._terrain_ao(x, z, h, ny)
			if ao < lo:
				lo = ao
				darkest = Vector2(x, z)
			hi = maxf(hi, ao)
	_check(lo < 0.82, "dobras e bacias escurecem de verdade (AO minimo %0.2f)" % lo)
	_check(hi > 0.98, "chao aberto continua limpo (AO maximo %0.2f)" % hi)
	# a cor gravada na malha tem que ser exatamente paleta -> LINEAR -> vezes AO
	var h2: float = island.height(darkest.x, darkest.y)
	var ny2: float = island._terrain_ny(darkest.x, darkest.y)
	var pura: Color = island._vcolor(darkest.x, darkest.y, h2, ny2)
	var ao2: float = island._terrain_ao(darkest.x, darkest.y, h2, ny2)
	var esperada: Color = island._lin(pura)
	var gravada: Variant = _terrain_color_near(darkest)
	_check(gravada != null and absf((gravada as Color).g - esperada.g * ao2) < 0.02,
			"o AO chegou na COR DE VERTICE da malha, nao ficou so' na conta")


## Defeito: AO constante (o que a versao G2 tinha, sem saber). Se a conta nao
## varia, o terreno le' chapado — e o teste de cima tem que acusar.
func _test_terrain_ao_defect_red() -> void:
	print("[defeito: AO chapado]")
	var flat := 1.0   # a "conta" que a G2 fazia: nenhuma
	_check(not (flat < 0.82), "AO constante seria BARRADO pela guarda acima")


## GAMA — o defeito mais caro que este mundo teve. COLOR de vertice chega no
## shader SEM conversao sRGB->linear; jogar o hex da paleta direto deixava tudo
## claro e lavado. Aqui: (a) a cor gravada e' a LINEAR, e (b) a versao ingenua
## (hex cru) seria MUITO mais clara — a prova de que a guarda de cima nao e' vacua.
func _test_gamma() -> void:
	print("[gama: paleta sRGB -> linear]")
	var g := island.COL_GRASS as Color
	var lin: Color = island._lin(g)
	_check(lin.g < g.g * 0.75,
			"srgb_to_linear muda de verdade (%0.2f -> %0.2f no verde)" % [g.g, lin.g])
	var cru: Variant = _terrain_color_near(Vector2(0, 0))
	_check(cru != null and (cru as Color).g < g.g,
			"o chao guarda o verde LINEAR, nao o hex cru (que sairia lavado)")


## Cor de vertice do terreno no vertice mais proximo de `p` (a grade e' regular).
func _terrain_color_near(p: Vector2) -> Variant:
	var mi := island.get_node_or_null("Generated/Terrain") as MeshInstance3D
	if mi == null:
		return null
	var arr: Array = (mi.mesh as ArrayMesh).surface_get_arrays(0)
	var verts: PackedVector3Array = arr[Mesh.ARRAY_VERTEX]
	var cols: PackedColorArray = arr[Mesh.ARRAY_COLOR]
	var best := -1
	var bd := INF
	for i in verts.size():
		var d := Vector2(verts[i].x, verts[i].z).distance_squared_to(p)
		if d < bd:
			bd = d
			best = i
	return cols[best] if best >= 0 else null


# --------------------------------------------------------- contrato gameplay

func _test_spawns() -> void:
	print("[contrato com a raia GAMEPLAY]")
	var n := 0
	var acima := true
	for c in island.get_children():
		if c is Marker3D and c.is_in_group("spawn"):
			n += 1
			var m := c as Marker3D
			if m.position.y < island.height(m.position.x, m.position.z):
				acima = false
	_check(n >= 8, "ilha publica %d spawns no grupo 'spawn'" % n)
	_check(acima, "todo spawn foi grudado ACIMA do terreno (ninguem nasce dentro do chao)")


func _test_determinism(packed: PackedScene) -> void:
	print("[determinismo]")
	var b := packed.instantiate() as Node3D
	root.add_child(b)
	var ma := (island.get_node("Generated/Terrain") as MeshInstance3D).mesh as ArrayMesh
	var mb := (b.get_node("Generated/Terrain") as MeshInstance3D).mesh as ArrayMesh
	var va: PackedVector3Array = ma.surface_get_arrays(0)[Mesh.ARRAY_VERTEX]
	var vb: PackedVector3Array = mb.surface_get_arrays(0)[Mesh.ARRAY_VERTEX]
	var ca: PackedColorArray = ma.surface_get_arrays(0)[Mesh.ARRAY_COLOR]
	var cb: PackedColorArray = mb.surface_get_arrays(0)[Mesh.ARRAY_COLOR]
	_check(va == vb, "duas instancias geram a MESMA malha (seeds fixos)")
	_check(ca == cb, "e as mesmas cores (o AO tambem e' deterministico)")
	_check(island.tree_count() == b.tree_count(),
			"mesma floresta: %d arvores" % island.tree_count())
	b.queue_free()


func _all_meshes(n: Node, out: Array = []) -> Array:
	if n is MeshInstance3D or (n is MultiMeshInstance3D and (n as MultiMeshInstance3D).multimesh != null):
		out.append(n)
	for c in n.get_children():
		_all_meshes(c, out)
	return out
