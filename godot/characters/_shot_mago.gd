# characters/_shot_mago.gd — FERRAMENTA DE CALIBRAGEM da raia PERSONAGEM.
# Nao entra no jogo: nada instancia esta cena, ela so' roda quando alguem pede.
#   Godot --path godot res://characters/_shot_mago.tscn --resolution 900x1200 -- --mage=13-brok
# (JANELA REAL, nao --headless: precisa de GPU pra renderizar de verdade — a
# mesma licao do world/_shot.gd.)
#
# EXISTE PORQUE: model_audit.gd responde "cabe no orcamento?" com numeros, e
# nao responde "parece um personagem do Arkana?". Modelo gerado por IA erra
# justamente onde numero nao ve — escala, peca de assinatura que sumiu, textura
# lavada, membro deformado pelo rig. Isso so' aparece quando o quadro e' OLHADO.
#
# Salva PNGs em user://shots_mago: um por angulo em repouso, mais um por
# animacao obrigatoria (idle/run/cast) na pose media dela.
extends Node3D

const ANGULOS := [
	# nome, angulo em graus ao redor do personagem
	["frente", 0.0],
	["tres_quartos", 35.0],
	["lado", 90.0],
	["costas", 180.0],
]

var _slug := "01-pyra"


func _ready() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--mage="):
			_slug = arg.trim_prefix("--mage=")

	# Luz e ceu do jogo: julgar o modelo com a iluminacao errada engana.
	var sol := DirectionalLight3D.new()
	sol.rotation_degrees = Vector3(-42, -35, 0)
	sol.light_energy = 1.15
	sol.shadow_enabled = true
	add_child(sol)

	var amb := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("#0B1026")   # azul-noite arcano (GDD 10)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("#2B3A6B")
	env.ambient_light_energy = 0.9
	amb.environment = env
	add_child(amb)

	var chao := MeshInstance3D.new()
	var plano := PlaneMesh.new()
	plano.size = Vector2(12, 12)
	chao.mesh = plano
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color("#1A2140")
	chao.material_override = mat
	add_child(chao)

	var mago: Node3D = (load("res://characters/Mage.tscn") as PackedScene).instantiate()
	mago.mage_id = _slug
	add_child(mago)
	for i in 8:
		await get_tree().process_frame

	var rel: Dictionary = mago.get_model_report() if mago.has_method("get_model_report") else {}
	print("modelo: ", rel.get("source", "?"), "  vertices: ", rel.get("vertices", "?"))

	# Altura real da malha: enquadrar por numero fixo mente quando o personagem
	# tem 1,40m (anao) ou 2,30m (golem).
	var alt := _altura_visivel(mago)
	print("altura medida: %.2f m" % alt)

	var cam := Camera3D.new()
	cam.fov = 45.0
	add_child(cam)
	cam.current = true

	var dir := "user://shots_mago"
	DirAccess.make_dir_recursive_absolute(dir)
	var raio := maxf(alt * 2.1, 1.6)
	var alvo := Vector3(0, alt * 0.55, 0)

	for a in ANGULOS:
		var rad := deg_to_rad(float(a[1]))
		cam.position = Vector3(sin(rad) * raio, alt * 0.75, cos(rad) * raio)
		cam.look_at(alvo, Vector3.UP)
		await _renderiza("%s/%s_%s.png" % [dir, _slug, a[0]])
		print("shot ", a[0])

	# Uma pose por animacao obrigatoria, sempre de tres-quartos (o angulo que
	# mais denuncia deformacao de rig no ombro e no joelho).
	var rad34 := deg_to_rad(35.0)
	cam.position = Vector3(sin(rad34) * raio, alt * 0.75, cos(rad34) * raio)
	cam.look_at(alvo, Vector3.UP)
	for nome in ["idle", "run", "cast"]:
		if mago.has_method("play_anim"):
			mago.play_anim(nome)
			for i in 14:
				await get_tree().process_frame
		await _renderiza("%s/%s_anim_%s.png" % [dir, _slug, nome])
		print("shot anim ", nome)

	print("SHOTS EM ", ProjectSettings.globalize_path(dir))
	get_tree().quit()


func _renderiza(caminho: String) -> void:
	for i in 4:
		await get_tree().process_frame
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(caminho)


## Altura visivel = AABB unida de todas as malhas. Usar o valor da ficha aqui
## esconderia justamente o defeito que queremos pegar (modelo na escala errada).
func _altura_visivel(no: Node) -> float:
	var topo := 0.0
	for m in _malhas(no):
		var aabb: AABB = m.get_aabb()
		topo = maxf(topo, (m.global_transform * aabb).end.y)
	return topo if topo > 0.05 else 1.7


func _malhas(no: Node) -> Array:
	var fora: Array = []
	if no is MeshInstance3D:
		fora.append(no)
	for f in no.get_children():
		fora.append_array(_malhas(f))
	return fora
