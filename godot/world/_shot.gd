# world/_shot.gd — FERRAMENTA DE CALIBRAGEM da raia MUNDO. Nao entra no jogo:
# nada instancia esta cena, ela so' roda quando alguem pede.
#   Godot --path godot res://world/_shot.tscn --resolution 960x540
# (JANELA REAL, nao --headless: precisa de GPU pra renderizar de verdade.)
# Salva 7 PNGs em user://shots — vale, floresta, lago, ruinas, alagado, aereo e
# uma tomada na altura do olho. EXISTE PORQUE CALIBRAR NO ESCURO NAO FUNCIONA:
# todo defeito grande do G4 (grama que lia como cone de transito, lago listrado
# igual toalha, cratera vermelha no alagado, praia em anel perfeito) so' apareceu
# quando o quadro foi renderizado e OLHADO. Antes de mexer em cor ou densidade,
# rode isto, olhe, e so' entao decida.
extends Node3D

const SHOTS := [
	# nome, posicao, alvo  (camera de 3a pessoa: ~4m de altura, olhando levemente pra baixo)
	["valley", Vector3(0, 6.0, 18.0), Vector3(0, 2.0, -8.0)],
	["forest", Vector3(-20, 8.0, -22.0), Vector3(-40, 3.0, -44.0)],
	["lake", Vector3(28, 7.0, 34.0), Vector3(46, 1.0, 18.0)],
	["ruins", Vector3(24, 11.0, -30.0), Vector3(40, 6.5, -46.0)],
	["marsh", Vector3(-26, 5.0, 20.0), Vector3(-44, 1.0, 36.0)],
	["wide", Vector3(0, 46.0, 96.0), Vector3(0, 0.0, -10.0)],
	["eye", Vector3(6, 3.2, 24.0), Vector3(-30, 4.0, -30.0)],
]

func _ready() -> void:
	var isl: Node3D = (load("res://world/Island.tscn") as PackedScene).instantiate()
	add_child(isl)
	var cam := Camera3D.new()
	cam.fov = 62.0
	cam.far = 400.0
	add_child(cam)
	cam.current = true
	var dir := "user://shots"
	DirAccess.make_dir_recursive_absolute(dir)
	for s in SHOTS:
		cam.position = s[1]
		cam.look_at(s[2], Vector3.UP)
		for i in 6:
			await get_tree().process_frame
		await RenderingServer.frame_post_draw
		var img := get_viewport().get_texture().get_image()
		img.save_png("%s/%s.png" % [dir, s[0]])
		print("shot ", s[0])
	print("SHOTS EM ", ProjectSettings.globalize_path(dir))
	get_tree().quit()
