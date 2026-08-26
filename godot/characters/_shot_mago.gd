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
# animacao do CONTRATO que o mago realmente tenha (as opcionais sao puladas
# quando o modelo nao traz o clipe — foto de fallback nomeada como a animacao
# de verdade seria uma mentira arquivada em PNG).
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

	# REGUA DE 1 METRO. A leitura de escala pela API mentiu (get_scale() dizia
	# 0.01 num modelo que renderiza no tamanho certo). Cubo de tamanho conhecido
	# ao lado responde "o anao esta baixo?" sem depender de nenhuma API.
	var regua := MeshInstance3D.new()
	var cubo := BoxMesh.new()
	cubo.size = Vector3.ONE
	regua.mesh = cubo
	var mr := StandardMaterial3D.new()
	mr.albedo_color = Color("#F0C75E")
	regua.material_override = mr
	regua.position = Vector3(1.1, 0.5, 0)
	add_child(regua)

	var mago: Node3D = (load("res://characters/Mage.tscn") as PackedScene).instantiate()
	mago.mage_id = _slug
	add_child(mago)
	for i in 8:
		await get_tree().process_frame

	var rel: Dictionary = mago.get_model_report() if mago.has_method("get_model_report") else {}
	print("modelo: ", rel.get("source", "?"), "  vertices: ", rel.get("vertices", "?"))

	# Altura real da malha: enquadrar por numero fixo mente quando o personagem
	# tem 1,40m (anao) ou 2,30m (golem).
	var alt := maxf(_altura_visivel(mago), 1.0)   # piso: a regua tem 1 m
	print("altura medida: %.2f m" % alt)

	var cam := Camera3D.new()
	cam.fov = 45.0
	add_child(cam)
	cam.current = true

	var dir := "user://shots_mago"
	DirAccess.make_dir_recursive_absolute(dir)
	var raio := maxf(alt * 2.1, 1.6)   # se alt=0 (medicao falhou) o piso segura
	var alvo := Vector3(0, alt * 0.55, 0)

	for a in ANGULOS:
		var rad := deg_to_rad(float(a[1]))
		cam.position = Vector3(sin(rad) * raio, alt * 0.75, -cos(rad) * raio)
		cam.look_at(alvo, Vector3.UP)
		await _renderiza("%s/%s_%s.png" % [dir, _slug, a[0]])
		print("shot ", a[0])

	# Uma pose por animacao obrigatoria, sempre de tres-quartos (o angulo que
	# mais denuncia deformacao de rig no ombro e no joelho).
	var rad34 := deg_to_rad(35.0)
	cam.position = Vector3(sin(rad34) * raio, alt * 0.75, -cos(rad34) * raio)
	cam.look_at(alvo, Vector3.UP)
	# TODAS as do contrato, nao so' as tres obrigatorias. A lista estava cravada
	# em idle/run/cast e por isso a ferramenta nunca mostrou `cair`, `planar`,
	# `pegar` nem `derrubado` — animacoes escritas a mao, em quadros-chave, que
	# NINGUEM tinha olhado renderizadas. Calibrar no escuro nao funciona: e' a
	# mesma licao que world/_shot.gd carrega no cabecalho.
	# As opcionais so' entram se o mago TIVER o clipe (has_anim), senao a foto
	# seria do fallback com o nome da animacao que nao existe — uma mentira
	# arquivada em PNG.
	var mage_scr: GDScript = load("res://characters/Mage.gd")
	var lista: Array = ["idle", "run", "cast"] + Array(mage_scr.OPTIONAL_ANIMS)
	for nome in lista:
		if not mago.has_method("play_anim"):
			continue
		if nome in mage_scr.OPTIONAL_ANIMS and mago.has_method("has_anim") 				and not mago.has_anim(nome):
			print("pula anim %s — este mago nao tem o clipe" % nome)
			continue
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
## NAO tem fallback silencioso: um numero plausivel inventado no lugar de uma
## medicao que falhou e' pior que erro nenhum — foi assim que 1.70 apareceu para
## a Pyra E para o Brok, escondendo que um deles podia estar na escala errada.
## Altura do personagem MONTADO, em metros.
##
## Media a MAIOR MALHA ISOLADA ate' 26/08, e isso so' funciona quando o corpo e'
## uma malha so'. No mago PROCEDURAL o corpo tem 18 primitivas, a maior delas e'
## o manto, e a ferramenta respondia 1,02 m para um mago de 1,83 m — o
## enquadramento saia perto demais e CORTAVA A CABECA nas fotos de animacao.
## Descoberto tentando julgar a pose de `derrubado`: eu nao conseguia dizer se o
## mago estava caido ou de pe' porque a propria referencia estava errada.
##
## SAO DOIS CASOS, e nao ha' formula unica — tentei uma e ela quebrou o outro:
##
##   MALHA SKINADA (todo modelo da Meshy): quem desenha e' o ESQUELETO. O
##   `global_transform` do MeshInstance3D nao participa do desenho e nao
##   significa nada — medido em 26/08, multiplicar a caixa por ele devolveu
##   0,018 m para a Pyra, que renderiza em 1,78. Aqui vale a caixa do RECURSO,
##   crua: o rig da Meshy ja' assa a altura da ficha nela (Brok deu 1,40, que e'
##   exatamente a ficha dele, conferido no olho com uma regua de 1 m).
##
##   MALHA COMUM (mago procedural): a caixa do recurso e' so' daquela peca. Aqui
##   vale a UNIAO das caixas levadas para o espaco do personagem.
##
## `basis.get_scale()` continua PROIBIDO nos dois casos: num .glb da Meshy ele
## devolveu (0.01, 0.01, 0.01) para um modelo do tamanho certo — base espelhada
## do glTF faz a decomposicao mentir. Nada aqui decompoe base nenhuma.
func _altura_visivel(no: Node) -> float:
	var malhas := _malhas(no)
	var raiz := no as Node3D
	var topo_skin := 0.0
	var caixa := AABB()
	var tem_comum := false
	var n_skin := 0
	for m in malhas:
		if m.mesh == null:
			continue
		if _e_skinada(m):
			n_skin += 1
			topo_skin = maxf(topo_skin, m.mesh.get_aabb().size.y)
			continue
		var local: AABB = m.get_aabb()
		if raiz != null:
			local = (raiz.global_transform.affine_inverse() * m.global_transform) * local
		caixa = local if not tem_comum else caixa.merge(local)
		tem_comum = true
	var topo := maxf(topo_skin, caixa.size.y if tem_comum else 0.0)
	print("  malhas medidas: %d (%d skinadas)  topo: %.3f m" % [malhas.size(), n_skin, topo])
	if topo <= 0.05:
		push_warning("_shot_mago: nao consegui medir a altura (%d malhas)" % malhas.size())
	return topo


## Skinada = desenhada pelo esqueleto. Duas perguntas porque o glTF pode trazer
## so' uma das duas: o recurso de skin, ou o caminho para o Skeleton3D.
func _e_skinada(m: MeshInstance3D) -> bool:
	if m.skin != null:
		return true
	return m.skeleton != NodePath() and m.get_node_or_null(m.skeleton) is Skeleton3D


func _malhas(no: Node) -> Array:
	var fora: Array = []
	if no is MeshInstance3D:
		fora.append(no)
	for f in no.get_children():
		fora.append_array(_malhas(f))
	return fora
