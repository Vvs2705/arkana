# world/_shot.gd — FERRAMENTA DE CALIBRAGEM da raia MUNDO. Nao entra no jogo:
# nada instancia esta cena, ela so' roda quando alguem pede.
#   Godot --path godot res://world/_shot.tscn --resolution 960x540
# (JANELA REAL, nao --headless: precisa de GPU pra renderizar de verdade.)
# Salva PNGs em user://shots e IMPRIME O ORCAMENTO MEDIDO (tris, draw calls,
# tempo de geracao). EXISTE PORQUE CALIBRAR NO ESCURO NAO FUNCIONA: todo defeito
# grande do G4 (grama que lia como cone de transito, lago listrado igual toalha,
# cratera vermelha no alagado, praia em anel perfeito) so' apareceu quando o
# quadro foi renderizado e OLHADO. Antes de mexer em cor ou densidade, rode isto,
# olhe, e so' entao decida.
#
# G5: as camaras deixaram de ser coordenadas cravadas e passaram a ser DERIVADAS
# dos centros de POI do Island.gd — ampliar a ilha ou mover um POI nao pode
# deixar a ferramenta de calibragem apontando pro vazio (foi o que aconteceu
# quando o SIZE mudou). E entrou a tomada que a fase nova exige: AEREO ALTO, a
# ~200m, que e' de onde o jogador escolhe onde pousar quando cai do ceu.
extends Node3D

var _isl: Node3D


func _ready() -> void:
	var t0 := Time.get_ticks_usec()
	_isl = (load("res://world/Island.tscn") as PackedScene).instantiate()
	add_child(_isl)
	var gen_ms := (Time.get_ticks_usec() - t0) / 1000.0
	var cam := Camera3D.new()
	cam.fov = 62.0
	# 4000 = o DEFAULT da Camera3D, que e' o que o jogo usa (ninguem mexe em `far`
	# em characters/ nem em gameplay/). Com os 400 herdados, o mar do outro lado do
	# mapa era cortado por plano de recorte e a ferramenta mentia sobre o horizonte.
	cam.far = 4000.0
	add_child(cam)
	cam.current = true
	var dir := "user://shots"
	DirAccess.make_dir_recursive_absolute(dir)
	print("=== ORCAMENTO ===")
	print("SIZE=%.0fm  geracao=%.0f ms" % [float(_isl.SIZE), gen_ms])
	_print_static_budget()
	var bus: Node = get_tree().root.get_node_or_null("Bus")
	for s in _shots():
		cam.position = s[1]
		cam.look_at(s[2], Vector3.UP)
		# A ferramenta fotografava um estado que o JOGO NUNCA TEM: o alcance da
		# sombra segue a altura do jogador (world/Sol.gd), e aqui ele ficava
		# sempre em repouso. Resultado: as tomadas aereas saiam sem uma sombra e
		# a calibragem mentia justo na fase que abre a partida. Avisar a altura
		# da CAMARA e' o que faz a foto valer como medida.
		if bus != null:
			bus.queda_altura.emit(maxf(s[1].y - float(_isl.height(s[1].x, s[1].z)), 0.0), 0.0)
		for i in 6:
			await get_tree().process_frame
		await RenderingServer.frame_post_draw
		var img := get_viewport().get_texture().get_image()
		img.save_png("%s/%s.png" % [dir, s[0]])
		print("shot %-10s draw_calls=%d  prims=%d" % [s[0],
				Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME),
				Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)])
	print("SHOTS EM ", ProjectSettings.globalize_path(dir))
	get_tree().quit()


## Custo que NAO depende de onde a camera esta': quantos nos desenhaveis existem
## (piso do draw call) e quantos triangulos o mundo carrega no total.
func _print_static_budget() -> void:
	var nodes := 0
	var tris := 0
	for n in _walk(_isl):
		nodes += 1
		var mesh: Mesh
		var mult := 1
		if n is MeshInstance3D:
			mesh = (n as MeshInstance3D).mesh
		else:
			var mm := (n as MultiMeshInstance3D).multimesh
			mesh = mm.mesh
			mult = mm.instance_count
		if mesh == null:
			continue
		for s in mesh.get_surface_count():
			tris += (mesh.surface_get_arrays(s)[Mesh.ARRAY_VERTEX] as PackedVector3Array).size() / 3 * mult
	print("nos desenhaveis=%d  triangulos no mundo=%d" % [nodes, tris])


func _walk(n: Node, out: Array = []) -> Array:
	if n is MeshInstance3D or (n is MultiMeshInstance3D and (n as MultiMeshInstance3D).multimesh != null):
		out.append(n)
	for c in n.get_children():
		_walk(c, out)
	return out


## Camaras DERIVADAS: cada POI se enquadra sozinho a partir do proprio centro e
## raio. Assim a ferramenta continua apontando certo depois de ampliar o mapa.
func _shots() -> Array:
	var s := float(_isl.SIZE)
	var out := [
		["valley", Vector3(0, 6.0, 18.0), Vector3(0, 2.0, -8.0)],
		["eye", Vector3(10, float(_isl.height(10, 34)) + 1.7, 34.0),
				Vector3(-40, float(_isl.height(-40, -40)) + 2.0, -40.0)],
	]
	for poi in [["lake", _isl.LAKE, _isl.LAKE_R], ["marsh", _isl.MARSH, _isl.MARSH_R],
			["forest", _isl.FOREST, _isl.FOREST_R], ["ruins", _isl.RUINS, _isl.RUINS_R],
			["peak", _isl.PEAK, _isl.PEAK_R], ["dunes", _isl.DUNES, _isl.DUNES_R],
			["bay", _isl.BAY_B, _isl.BAY_W + 10.0]]:
		var c: Vector2 = poi[1]
		var r: float = poi[2]
		var d := (r + 16.0)
		var eye := Vector3(c.x + d * 0.75, float(_isl.height(c.x + d * 0.75, c.y + d * 0.75)) + r * 0.55 + 5.0, c.y + d * 0.75)
		out.append([poi[0], eye, Vector3(c.x, float(_isl.height(c.x, c.y)) + 1.5, c.y)])
	# Panorama de meia-altura: le' a ilha inteira de vies (silhueta + camadas).
	out.append(["wide", Vector3(0, s * 0.30, s * 0.62), Vector3(0, 0.0, -s * 0.05)])
	# A TOMADA DA FASE NOVA: 200m de altura, o angulo da queda. Se um POI nao se
	# distingue AQUI, ele nao existe pro jogador que esta' escolhendo onde pousar.
	out.append(["drop200", Vector3(0, 200.0, 0.1), Vector3(0, 0, 0)])
	# E a mesma altura de vies, que e' como o paraquedas realmente chega.
	out.append(["drop_obl", Vector3(-s * 0.42, 200.0, s * 0.42), Vector3(0, 0, 0)])
	return out
