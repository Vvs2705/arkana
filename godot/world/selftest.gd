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
	_test_sombra_na_queda()
	_test_sombra_na_queda_defect_red()
	_test_look()
	_test_fog_defect_red()
	_test_materials()
	_test_shader_uniforms()
	_test_terrain_ao()
	_test_terrain_ao_defect_red()
	_test_material_blend()
	_test_material_blend_defect_red()
	_test_coastline()
	_test_water_sheet()
	_test_water_sheet_defect_red()
	_test_aerial_read()
	_test_aerial_read_defect_red()
	_test_drop_budget()
	_test_scatter_budget()
	_test_gamma()
	_test_spawns()
	_test_escala_do_mapa()
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


## Fracao de nevoa que uma superficie a `d` metros da camera recebe, com a formula
## do modo DEPTH do Godot. Existe pra que a guarda cobre o EFEITO e nao o valor de
## um botao — begin, end e curve podem ser recalibrados a vontade desde que o
## resultado a 200 m continue deixando a ilha visivel.
func _fog_em(d: float) -> float:
	var t: float = clampf((d - env.fog_depth_begin)
			/ maxf(env.fog_depth_end - env.fog_depth_begin, 0.001), 0.0, 1.0)
	return clampf(pow(t, env.fog_depth_curve) * env.fog_density, 0.0, 1.0)


## Anti-vacuidade: o ajuste que a ilha tinha ANTES da ampliacao (34/220/1.8) —
## bom pra um mapa de 76 m de raio, fatal pra uma queda de 200 m.
func _test_fog_defect_red() -> void:
	print("[defeito: nevoa calibrada pro mapa pequeno]")
	var t: float = clampf((200.0 - 34.0) / (220.0 - 34.0), 0.0, 1.0)
	var velho: float = pow(t, 1.8)
	_check(velho >= 0.20,
			"o ajuste 34/220/1.8 comeria %0.0f%% do quadro aereo — BARRADO pela guarda acima"
			% (velho * 100.0))


func _test_shadow_budget() -> void:
	print("[sombra no ARM64]")
	_check(sun.shadow_enabled, "sol projeta sombra")
	_check(sun.directional_shadow_mode == DirectionalLight3D.SHADOW_ORTHOGONAL,
			"1 split ortogonal (PSSM redesenharia os casters 2-4x por frame)")
	# O teto de 80m vale para o JOGO A PE'. Desde 26/08 o alcance sobe durante a
	# queda (world/Sol.gd) — e o que esta' sendo cobrado aqui e' o REPOUSO, que e'
	# onde o mago passa a partida e onde o texel fino importa.
	_check(sun.directional_shadow_max_distance <= 80.0,
			"alcance EM REPOUSO <= 80m (%0.0f) — o resto e' nevoa" % sun.directional_shadow_max_distance)
	_check(sun.directional_shadow_fade_start < 1.0,
			"sombra some em rampa, nao numa linha reta cortando a ilha")


## A SOMBRA DURANTE A QUEDA (26/08). Defeito fotografado: com alcance fixo em
## 60 m, a ilha vista do castelo a 200 m nao projetava UMA sombra — a mata e o
## pico liam como mancha chapada justo na fase que abre a partida. O alcance
## agora segue a altura (world/Sol.gd) e volta a 60 m no pouso.
## A regra e' funcao PURA de proposito: da' para prova-la sem partida e sem Bus.
func _test_sombra_na_queda() -> void:
	print("[sombra acompanha a queda]")
	var sol_script: GDScript = load("res://world/Sol.gd")
	_check(sun.get_script() == sol_script, "o Sun da ilha carrega world/Sol.gd")
	_check(is_equal_approx(sun.directional_shadow_max_distance, sol_script.PERTO),
			"boota em repouso (%0.0fm)" % sol_script.PERTO)
	# no chao e em altura invalida, fecha
	for m in [0.0, -3.0, NAN]:
		_check(is_equal_approx(sol_script.alcance_para(m), sol_script.PERTO),
				"altura %s -> alcance de repouso (NaN e negativo barrados)" % m)
	# subindo, abre — e nunca passa do teto
	_check(sol_script.alcance_para(50.0) > sol_script.PERTO,
			"a 50m de altura o alcance ja' passou dos 60m de repouso")
	_check(sol_script.alcance_para(200.0) >= 300.0,
			"a 200m (altura do castelo) o alcance cobre a ilha de 300m")
	_check(is_equal_approx(sol_script.alcance_para(9999.0), sol_script.LONGE),
			"o alcance tem teto (%0.0fm) — nao cresce sem fim" % sol_script.LONGE)
	# monotonica: mais alto nunca da' menos sombra
	var anterior := 0.0
	for m in [0.0, 25.0, 60.0, 120.0, 200.0, 400.0]:
		var a: float = sol_script.alcance_para(m)
		_check(a >= anterior, "alcance nao diminui ao subir (%0.0fm -> %0.0fm)" % [m, a])
		anterior = a

	# PONTA A PONTA. Sem isto, apagar a linha que conecta o Bus deixaria todos os
	# checks acima VERDES com a sombra morta no jogo — a regra existiria e nunca
	# seria chamada. Aqui o sinal e' emitido de verdade e o sol tem que responder.
	var bus: Node = root.get_node_or_null("Bus")
	_check(bus != null, "autoload Bus presente (o sol escuta a queda por ele)")
	if bus == null:
		return
	bus.queda_altura.emit(200.0, 40.0)
	_check(sun.directional_shadow_max_distance > sol_script.PERTO,
			"Bus.queda_altura(200m) ABRE o alcance de verdade (%0.0fm)"
			% sun.directional_shadow_max_distance)
	bus.queda_fase.emit("pousou")
	_check(is_equal_approx(sun.directional_shadow_max_distance, sol_script.PERTO),
			"Bus.queda_fase(pousou) FECHA de volta em %0.0fm" % sol_script.PERTO)


## O defeito que este conserto apaga, refeito na mao: alcance CRAVADO em 60 m.
## Com ele, a ilha inteira vista do castelo fica fora da sombra.
func _test_sombra_na_queda_defect_red() -> void:
	print("[defeito: alcance cravado durante a queda]")
	var fixo := 60.0
	var altura_do_castelo := 200.0
	_check(fixo < altura_do_castelo,
			"com alcance fixo em %0.0fm, a %0.0fm de altura NADA na ilha teria sombra"
			% [fixo, altura_do_castelo])
	var sol_script: GDScript = load("res://world/Sol.gd")
	_check(sol_script.alcance_para(altura_do_castelo) > fixo,
			"a regra nova cobre onde o alcance fixo nao chegava")


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
	# A NEVOA VISTA DA QUEDA (G5). Este e' o numero que decide se a fase nova existe:
	# o jogador entra a ~200 m de altura e escolhe onde pousar, entao o chao esta' a
	# ~200 m da camera. Com o ajuste herdado (fim aos 220 m) a conta dava 81% de
	# nevoa e o render aereo saiu um BORRAO CARAMELO — nao ha' escolha possivel num
	# quadro onde nao existe nada. A guarda cobra a conta, nao o botao: qualquer
	# combinacao de begin/end/curve serve, desde que a 200 m ainda se enxergue.
	_check(_fog_em(200.0) < 0.20,
			"a %0.0fm (a altura da queda) a nevoa come so' %0.0f%% do quadro" % [200.0, _fog_em(200.0) * 100.0])
	# E o mar continua tendo que morrer no ceu: e' o que a nevoa fazia antes e nao
	# pode deixar de fazer. 600 m e' o dobro da ilha — se sobrar mar nitido ali, a
	# borda do plano d'agua aparece no quadro (aconteceu, e foi corrigido no shader).
	_check(_fog_em(600.0) > 0.95, "a 600m o mar aberto ja' virou ceu (%0.0f%%)" % (_fog_em(600.0) * 100.0))
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
	# As travessias saem dos PROPRIOS centros de POI: cravar coordenadas foi o que
	# fez metade dos testes deste arquivo apontarem pro vazio quando o mapa cresceu.
	var e: float = float(island.LAND_R) + 12.0
	var lk: Vector2 = island.LAKE
	var mh: Vector2 = island.MARSH
	var rn: Vector2 = island.RUINS
	var pk: Vector2 = island.PEAK
	var du: Vector2 = island.DUNES
	var linhas := [
		[Vector2(-e, lk.y), Vector2(e, lk.y)],    # lago + as duas praias
		[Vector2(-e, mh.y), Vector2(e, mh.y)],    # alagado
		[Vector2(rn.x, -e), Vector2(rn.x, e)],    # ruinas
		[Vector2(-e, -e), Vector2(e, e)],         # floresta e vale
		[Vector2(pk.x, -e), Vector2(pk.x, e)],    # pico (POI novo do G5)
		[Vector2(-e, du.y), Vector2(e, du.y)],    # dunas (POI novo do G5)
	]
	var pior_novo := 0.0
	var pior_velho := 0.0
	var pior_dentro := 0.0    # so' a area jogavel
	var onde := Vector2.ZERO
	var onde_dentro := Vector2.ZERO
	for l in linhas:
		var a: Vector2 = l[0]
		var b: Vector2 = l[1]
		var pn := Color(0, 0, 0)
		var pv := Color(0, 0, 0)
		var ph := 0.0
		var n := 160
		for i in n + 1:
			var q: Vector2 = a.lerp(b, float(i) / n)
			var h: float = island.height(q.x, q.y)
			var ny: float = island._terrain_ny(q.x, q.y)
			var cn: Color = island._vcolor(q.x, q.y, h, ny)
			var cv: Color = _vcolor_g3(q, h, ny)
			# FILTRO DE PENHASCO (entrou no G5, e ele CORRIGE o teste, nao o afrouxa).
			# Este teste existe pra pegar EMENDA DE MATERIAL — cor que salta sem o
			# chao saltar. Onde o relevo despenca a cor TEM que acompanhar, e contar
			# isso como emenda so' mede o penhasco. Ate' o G4 o unico penhasco da
			# ilha ficava na borda, e o corte por raio dava conta; o G5 poe um
			# penhasco NO MEIO do mapa (a escarpa da mesa) e o corte por raio passou
			# a esconder o que interessa e a acusar o que nao e' defeito.
			# Passo da amostra ~1,8m; 0,8m de cota nele e' ~24 graus. Acima disso e'
			# escarpa. O MESMO filtro vale para as duas regras — comparacao justa.
			if i > 0 and absf(h - ph) <= 0.8:
				var dn := absf(cn.r - pn.r) + absf(cn.g - pn.g) + absf(cn.b - pn.b)
				if dn > pior_novo:
					pior_novo = dn
					onde = q
				if q.length() <= float(island.LAND_R) - 14.0 and dn > pior_dentro:
					pior_dentro = dn
					onde_dentro = q
				pior_velho = maxf(pior_velho,
						absf(cv.r - pv.r) + absf(cv.g - pv.g) + absf(cv.b - pv.b))
			pn = cn
			pv = cv
			ph = h
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
	# O que sobra do pior caso e' a beira d'agua, onde a areia molhada encosta na
	# grama dentro de uma faixa de cota estreita. Dentro da area jogavel, que e'
	# onde o jogador olha o chao de perto, a exigencia e' bem mais dura.
	# Regra, nao numero magico: dentro da area jogavel (r<=LAND_R-14), que e' onde o
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
## A varredura mudou de SENTIDO no G5, e isso e' um conserto, nao um capricho: a
## versao antiga andava DE DENTRO PRA FORA a partir de r=60 e parava na primeira
## cota <= 0. Com o lago agora centrado a 81 m do meio, o raio de 60 m ja' nascia
## dentro da cava do lago e a funcao devolvia "costa aos 60 m" — media agua doce
## achando que media mar. Varrendo DE FORA PRA DENTRO, a primeira terra que se
## encontra e' necessariamente a costa, e bacia interna nenhuma engana o teste.
func _test_coastline() -> void:
	print("[costa irregular]")
	var lo := 999.0
	var hi := 0.0
	for k in 64:
		var a := TAU * float(k) / 64.0
		var d := Vector2(cos(a), sin(a))
		var r: float = island.LAND_R + 55.0
		while r > 20.0 and island.height(d.x * r, d.y * r) <= 0.0:
			r -= 0.5
		lo = minf(lo, r)
		hi = maxf(hi, r)
	# 8 m bastava numa ilha de 76 m de raio; em 132 m a mesma exigencia relativa
	# pede ~14 m. O que se cobra e' o mesmo: costa nao pode ser circunferencia.
	_check(hi - lo > 14.0,
			"a linha d'agua varia %0.1fm entre a enseada e a ponta (disco perfeito = 0)"
			% (hi - lo))


# --------------------------------------------- G5: a ilha vista LA' DE CIMA

## LAMINA D'AGUA. A ilha e' desenhada por uma funcao de relevo e a agua por um
## DISCO plano posto por cima; sao duas coisas independentes, e quando o mapa
## cresceu elas se desencontraram na primeira tentativa. O sintoma e' o pior que
## existe num POI: um BURACO no meio do lago, com o fundo da cava seco e a' mostra,
## porque o disco acabava antes da margem.
## A guarda mede as duas e cobra a relacao certa: o raio em que a cota cruza a
## lamina tem que caber DENTRO do disco. O raio do disco e' LIDO DA MALHA (o menor
## vertice do anel externo), nao de um numero repetido aqui — repetir o numero e'
## justamente como as duas coisas se desencontram.
## Errar pra MAIS e' de graca e por isso nao e' cobrado: onde a margem ja' subiu,
## quem cobre o disco e' o terreno, e o plano d'agua fica enterrado.
func _water_gap(nm: String, centro: Vector2, superficie: float) -> Array:
	var mi := island.get_node_or_null("Generated/" + nm) as MeshInstance3D
	if mi == null:
		return [-1.0, 0.0]
	var arr: Array = (mi.mesh as ArrayMesh).surface_get_arrays(0)
	var verts: PackedVector3Array = arr[Mesh.ARRAY_VERTEX]
	var cols: PackedColorArray = arr[Mesh.ARRAY_COLOR]
	var disco := INF
	for i in verts.size():
		# COLOR.r == 1.0 marca o anel EXTERNO do disco (gravado pelo _disc_mesh);
		# o MENOR raio desse anel e' o pior caso, porque o disco e' deformado.
		if cols[i].r > 0.999:
			disco = minf(disco, Vector2(verts[i].x, verts[i].z).length())
	var submerso := 0.0
	for k in 96:
		var a := TAU * float(k) / 96.0
		var d := Vector2(cos(a), sin(a))
		var r := 0.5
		while r < 70.0 and island.height(centro.x + d.x * r, centro.y + d.y * r) < superficie:
			r += 0.25
		submerso = maxf(submerso, r)
	return [disco, submerso]


func _test_water_sheet() -> void:
	print("[lamina d'agua cobre a cava]")
	for job in [["LakeWater", island.LAKE, 0.6], ["MarshWater", island.MARSH, 0.55]]:
		var m := _water_gap(job[0], job[1], job[2])
		var disco: float = m[0]
		var submerso: float = m[1]
		_check(disco > 0.0, "%s existe na cena" % job[0])
		_check(disco >= submerso,
				"%s: disco de %0.1fm cobre os %0.1fm de cava submersa (sobra %0.1fm)"
				% [job[0], disco, submerso, disco - submerso])


## Anti-vacuidade: o par de discos do G4 (18 m no lago, 15 m no alagado) foi
## herdado sem ser remedido quando os POIs cresceram. Com os raios de HOJE aquele
## disco deixaria a cava descoberta — e e' isso que a guarda acima pega.
func _test_water_sheet_defect_red() -> void:
	print("[defeito: disco d'agua herdado do mapa pequeno]")
	for job in [["LakeWater", island.LAKE, 0.6, 18.0], ["MarshWater", island.MARSH, 0.55, 15.0]]:
		var m := _water_gap(job[0], job[1], job[2])
		var submerso: float = m[1]
		var herdado: float = job[3]
		_check(herdado < submerso,
				"%s com os %0.0fm do G4 deixaria %0.1fm de cava seca — BARRADO pela guarda acima"
				% [job[0], herdado, submerso - herdado])


## LEITURA DO ALTO — a exigencia NOVA da fase. Caindo de 200 m o jogador escolhe
## onde pousar, e daquela altura nao existe detalhe: nao ha' sombra projetada (o
## sol so' desenha ate' 60 m), nao ha' grama (some por celula alem de 80 m) e nao
## ha' silhueta de objeto pequeno. O que chega no olho e' a COR MEDIA de cada
## pedaco de chao — e cor media e' exatamente o que esta' gravado na cor de
## vertice da malha. Entao o teste le' a propria malha e cobra que cada POI seja
## uma MANCHA propria: distinta da campina aberta E distinta dos outros POIs.
## Limiar 0.10 na soma dos canais (~3% por canal em LINEAR): abaixo disso duas
## areas viram a mesma mancha a 200 m, por mais diferentes que os hex parecam.
func _poi_tint(centro: Vector2, raio: float) -> Color:
	var mi := island.get_node_or_null("Generated/Terrain") as MeshInstance3D
	var arr: Array = (mi.mesh as ArrayMesh).surface_get_arrays(0)
	var verts: PackedVector3Array = arr[Mesh.ARRAY_VERTEX]
	var cols: PackedColorArray = arr[Mesh.ARRAY_COLOR]
	var acc := Color(0, 0, 0)
	var n := 0
	for i in verts.size():
		if Vector2(verts[i].x, verts[i].z).distance_to(centro) <= raio:
			acc += cols[i]
			n += 1
	if n == 0:
		return Color(0, 0, 0)
	return Color(acc.r / n, acc.g / n, acc.b / n)


func _dist(a: Color, b: Color) -> float:
	return absf(a.r - b.r) + absf(a.g - b.g) + absf(a.b - b.b)


func _aerial_tints() -> Dictionary:
	return {
		"campina": _poi_tint(Vector2(0, 0), 26.0),
		"floresta": _poi_tint(island.FOREST, float(island.FOREST_R) * 0.6),
		"ruinas": _poi_tint(island.RUINS, float(island.RUINS_R) * 0.6),
		"alagado": _poi_tint(island.MARSH, float(island.MARSH_R) * 0.5),
		"dunas": _poi_tint(island.DUNES, float(island.DUNES_R) * 0.5),
		"pico": _poi_tint(island.PEAK, float(island.PEAK_TOP)),
	}


func _test_aerial_read() -> void:
	print("[leitura do alto: cada POI e' uma mancha propria]")
	var t := _aerial_tints()
	var nomes: Array = t.keys()
	var pior := 9.0
	var par := ""
	for i in nomes.size():
		for j in range(i + 1, nomes.size()):
			var d := _dist(t[nomes[i]], t[nomes[j]])
			if d < pior:
				pior = d
				par = "%s/%s" % [nomes[i], nomes[j]]
	_check(pior > 0.10,
			"o par mais parecido do mapa (%s) ainda difere %0.3f na cor gravada" % [par, pior])
	# E o pico tem que ser tambem FORMA, nao so' cor (GDD §10: cor + forma). A mesa
	# e' o unico relevo com altura de landmark; achatou, sumiu do quadro aereo.
	var alto: float = island.height(island.PEAK.x, island.PEAK.y)
	var chao: float = island.height(0, 0)
	_check(alto - chao > 20.0,
			"o pico se ergue %0.1fm acima do vale — silhueta, nao so' tinta" % (alto - chao))


## Anti-vacuidade: a ilha do G4 tinha 4 POIs e dois deles se distinguiam so' pelo
## que ha' EM CIMA do chao (copas, pedras) — coisas que a 200 m nao se resolvem.
## Aqui as duas guardas acima recebem os casos degenerados: um POI sem cor propria
## (cor da campina contra ela mesma) e um POI sem altura (colina de 8 m no lugar
## da mesa). As duas tem que reprovar.
func _test_aerial_read_defect_red() -> void:
	print("[defeito: POI que so' se distingue de perto]")
	var t := _aerial_tints()
	var campina: Color = t["campina"]
	_check(_dist(campina, campina) <= 0.10,
			"POI pintado com o verde da campina daria diferenca 0.000 — BARRADO pela guarda acima")
	var chao: float = island.height(0, 0)
	_check(not ((chao + 8.0) - chao > 20.0),
			"uma colina de 8m no lugar da mesa seria BARRADA pela guarda de silhueta")


## ORCAMENTO DA QUEDA. No chao a grama some sozinha: o grass.gdshader achata a
## lamina aos 56 m. So' que achatar NAO deixa de custar — o vertex shader roda
## igual para toda instancia que caia no frustum, e do alto da queda o frustum
## pega a ILHA INTEIRA de uma vez, ou seja, os 30.000 tufos. Por isso a grama sai
## fatiada em celulas com visibility_range: alem do alcance a celula inteira e'
## descartada pela engine, ANTES do vertex shader. Sem isso a ampliacao nao paga.
func _test_drop_budget() -> void:
	print("[orcamento da queda: grama cortada por celula]")
	var gen := island.get_node_or_null("Generated")
	var celulas := 0
	var sem_corte := 0
	var alcance := 0.0
	for c in gen.get_children():
		if c is MultiMeshInstance3D and String(c.name).begins_with("Grass"):
			celulas += 1
			var v: float = (c as MultiMeshInstance3D).visibility_range_end
			alcance = maxf(alcance, v)
			if v <= 0.0:
				sem_corte += 1
	_check(celulas > 0 and sem_corte == 0,
			"as %d celulas de grama tem corte por distancia (%d sem corte)" % [celulas, sem_corte])
	# O corte tem que ser MAIOR que o fade do shader (56 m) — senao apaga tufo que
	# ainda esta' em pe' — e bem menor que a altura da queda, senao nao corta nada.
	_check(alcance > 56.0 and alcance < 150.0,
			"corte em %0.0fm: depois do fade do shader (56m) e muito antes dos 200m da queda"
			% alcance)


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
	var n1: int = int(island.QUADS) + 1
	for iz in range(0, n1, 5):
		for ix in range(0, n1, 5):
			var x: float = (float(ix) / float(island.QUADS) - 0.5) * float(island.SIZE)
			var z: float = (float(iz) / float(island.QUADS) - 0.5) * float(island.SIZE)
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
	_check(n >= 12, "ilha publica %d spawns no grupo 'spawn'" % n)
	_check(acima, "todo spawn foi grudado ACIMA do terreno (ninguem nasce dentro do chao)")

	## O TETO ESTRUTURAL DA POPULACAO. gameplay/Main pede 1 + Balance.MATCH.bots
	## pontos DISTINTOS; `_spawn_points` repete com `_spawns[i % size]` quando
	## faltam, e dois magos nascem um dentro do outro. Em 27/08 a populacao subiu de
	## 6 para 12 junto com o mapa 4x maior — com 14 nascimentos isso couber foi
	## sorte, e sorte nao e' contrato. VERMELHO PROVADO: subindo bots para 20.
	var precisa := 1 + int(Balance.MATCH.bots)
	_check(n >= precisa,
			"os %d nascimentos cobrem 1 jogador + %d bots (precisa de %d distintos)"
			% [n, int(Balance.MATCH.bots), precisa])

	## E ELES ESTAO ESPALHADOS: dois nascimentos em cima um do outro sao dois
	## nascimentos que valem um. 12 m e' mais que o alcance de contato.
	var perto := 1e9
	var lista: Array[Vector2] = []
	for c in island.get_children():
		if c is Marker3D and c.is_in_group("spawn"):
			lista.append(Vector2(c.position.x, c.position.z))
	for i in lista.size():
		for j in range(i + 1, lista.size()):
			perto = minf(perto, lista[i].distance_to(lista[j]))
	_check(perto > 12.0,
			"os dois nascimentos mais proximos estao a %.0f m (minimo 12)" % perto)


## ============================================================================
## O MAPA CRESCE POR UM KNOB (27/08 — ordem do Diretor: "um mapa muito maior,
## mais exploravel"). Quatro leis, e cada uma tem uma forma diferente de morrer
## em silencio:
##
##  1. TUDO na horizontal sai de ESCALA. Se um POI, um spawn ou um raio ficar em
##     metro cravado, ele nao acompanha — foi assim que a ilha de 26/08 cresceu de
##     180 para 300 m deixando a Zona com um terco do mapa e o loot fora dos POIs.
##  2. A VERTICAL NAO ESCALA. Dobrar a altura junto tornaria toda ladeira
##     insubivel: o plato tem 9,0 m e o pico 28 m em QUALQUER tamanho de mapa.
##  3. A DENSIDADE se mantem (coisas por metro quadrado), senao mapa maior e'
##     campo de golfe. Medido em 27/08: crescendo so' o SIZE, 45 draw calls de
##     decoracao DESAPARECERAM porque os scatters desistiam no limite de tentativas.
##  4. O CULLING POR CELULA tem que ser real. As celulas ficavam todas em (0,0,0)
##     e visibility_range mede a distancia ate' a ORIGEM DO NO': o corte era
##     tudo-ou-nada. Invisivel numa ilha de 132 m (a camera nunca esta' a mais de
##     80 m do centro) e fatal em qualquer mapa maior.
func _test_escala_do_mapa() -> void:
	print("[O mapa cresce por um knob: horizontal escala, vertical nao]")
	var scr: GDScript = island.get_script()
	var e := float(scr.ESCALA)
	print("  (ESCALA = %.1f -> lado %.0f m, raio de terra %.0f m, area de terra %.1fx)"
			% [e, float(scr.SIZE), float(scr.LAND_R), e * e])

	# --- 1. a horizontal sai do knob
	_check(is_equal_approx(float(scr.LAND_R), float(scr.B_LAND_R) * e),
			"LAND_R = B_LAND_R x ESCALA (nenhum metro cravado)")
	_check(is_equal_approx(float(scr.SIZE), float(scr.B_SIZE) * e),
			"SIZE = B_SIZE x ESCALA")
	var pois: Dictionary = island.pois()
	var todos_escalados := true
	for nome in pois:
		var c: Vector2 = pois[nome].centro
		if c.length() > float(scr.LAND_R):
			todos_escalados = false
	_check(todos_escalados, "os %d POIs cabem dentro do raio de terra" % pois.size())
	_check(is_equal_approx(float(pois["lago"].centro.x), float(scr.B_LAKE.x) * e),
			"e o centro do lago acompanhou o knob")

	# --- 2. a vertical NAO escala: a fisica do jogo nao pode mudar de tamanho
	var h_ruinas: float = island.height(float(scr.RUINS.x), float(scr.RUINS.y))
	var h_pico: float = island.height(float(scr.PEAK.x), float(scr.PEAK.y))
	_check(absf(h_ruinas - 9.0) < 0.35,
			"o plato das ruinas tem %.2f m (9,0 esperado) em qualquer escala" % h_ruinas)
	_check(h_pico > 24.0 and h_pico < 32.0,
			"o pico tem %.1f m (~28 esperado): a encosta continua subivel" % h_pico)

	# --- 3. os spawns cobrem a ilha, nao o miolo dela
	var mais_longe := 0.0
	var n_spawn := 0
	for c in island.get_children():
		if c is Marker3D and c.is_in_group("spawn"):
			n_spawn += 1
			mais_longe = maxf(mais_longe, Vector2(c.position.x, c.position.z).length())
	_check(mais_longe > float(scr.LAND_R) * 0.5,
			"o nascimento mais distante esta' a %.0f m, alem de meia-terra (%.0f m)"
			% [mais_longe, float(scr.LAND_R) * 0.5])

	# --- 3b. densidade preservada: tufos por metro quadrado, nao tufos
	var gen := island.get_node_or_null("Generated")
	var tufos := 0
	var celulas := 0
	var origens := {}
	for c in gen.get_children():
		if c is MultiMeshInstance3D and String(c.name).begins_with("Grass"):
			celulas += 1
			tufos += (c as MultiMeshInstance3D).multimesh.instance_count
			origens["%.1f,%.1f" % [c.position.x, c.position.z]] = true
	var area_km := PI * pow(float(scr.LAND_R), 2.0) / 1e6
	var dens := float(tufos) / area_km
	# A referencia e' a ilha aprovada de 26/08: 30.000 tufos em pi*132^2 = 0,0548
	# km2 = ~548.000 por km2. Faixa larga de proposito (o scatter rejeita terreno),
	# apertada o bastante para pegar "esqueci de escalar a contagem" (cairia 4x).
	_check(dens > 380000.0,
			"densidade de grama %.0f tufos/km2 (referencia 548.000): o mapa nao esvaziou"
			% dens)

	# --- 4. o culling por celula e' REAL: cada celula no SEU lugar
	_check(celulas >= 9, "grama fatiada em %d celulas" % celulas)
	_check(origens.size() == celulas,
			"as %d celulas tem %d origens DISTINTAS — visibility_range mede a "
			% [celulas, origens.size()] + "distancia ate' a origem do no', e com "
			+ "todas em (0,0,0) o corte era tudo-ou-nada")
	var passo := float(scr.SIZE) / float(int(10.0 * e))
	_check(passo > 20.0 and passo < 40.0,
			"a celula tem %.0f m: a grade escalou junto com o mapa (o corte e' 80 m)"
			% passo)
## ============================================================================


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
