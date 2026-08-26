## Prova headless da raia PERSONAGEM:
##   Godot --headless --path godot --script res://characters/selftest.gd
## Instancia em _initialize e checa em _process (o _ready da cena so' roda
## depois que o loop principal comeca).
extends SceneTree

const IDS := preload("res://characters/mage_identity.gd")
const VERT_BUDGET := 3400          # ficha dos personagens: ~3k por mago

var _mage: Node
var _frames := 0
var _fails := 0


func _initialize() -> void:
	var ps: PackedScene = load("res://characters/Mage.tscn")
	if ps == null:
		push_error("FALHOU — nao carregou res://characters/Mage.tscn")
		quit(1)
		return
	_mage = ps.instantiate()
	root.add_child(_mage)


func _process(_delta: float) -> bool:
	_frames += 1
	if _mage == null or _frames < 2:
		return _mage == null
	# _run_checks pode ABORTAR num erro de script; o contador vive fora dela para
	# que uma quebra no meio nao vire "OK — 0 falhas" (era o que acontecia antes).
	_fails = -1
	_run_checks()
	if _fails < 0:
		print("RESULTADO: ABORTOU no meio dos checks (veja o SCRIPT ERROR acima)")
		quit(1)
		return true
	print("RESULTADO: %s" % ("OK — 0 falhas" if _fails == 0 else "%d FALHAS" % _fails))
	quit(0 if _fails == 0 else 1)
	return true


func _run_checks() -> void:
	_fails = 0
	_check(_mage is Node3D, "raiz e' Node3D (contrato ARQUITETURA.md)")

	# ---- contrato com a raia de gameplay (play_anim / set_tint / cast_fired)
	var player: AnimationPlayer = _mage.get_node_or_null("AnimationPlayer")
	_check(player != null, "AnimationPlayer existe")
	for nome in ["idle", "run", "cast"]:
		_mage.play_anim(nome)
		var ok: bool = player != null and player.has_animation(nome)
		var dur: float = player.get_animation(nome).length if ok else 0.0
		_check(ok and dur > 0.0, "anim '%s' existe, duracao %.2fs > 0" % [nome, dur])
		_check(player != null and player.current_animation == nome,
			"play_anim('%s') ativa a animacao" % nome)

	var robe: MeshInstance3D = _mage.get_node("Rig/Hips/Robe")
	var antes: Variant = robe.material_override.get_shader_parameter("tint")
	_mage.set_tint(Color(1, 0.2, 0.2))
	var depois: Variant = robe.material_override.get_shader_parameter("tint")
	_check(depois == Color(1, 0.2, 0.2) and str(antes) != str(depois),
		"set_tint mudou o material do manto (%s -> %s)" % [antes, depois])

	_check(_mage.has_signal("cast_fired"), "sinal cast_fired existe (gatilho do projetil)")
	var fire: float = _mage.get_cast_fire_time()
	_check(fire > 0.0 and fire < _mage.get_anim_length("cast"),
		"cast_fire_time (%.2fs) dentro da anim cast (%.2fs)" % [fire, _mage.get_anim_length("cast")])

	# ---- pipeline .glb: pronto para assets reais, mas sem quebrar o fallback atual
	_check_model_pipeline()

	# ---- shading: os quatro materiais precisam responder a luz de forma DIFERENTE,
	# senao "tudo parece o mesmo material" (que era a queixa)
	_check_orcamento_triangulos()
	_check_materiais()

	# ---- AO assado no vertice: a barra do manto tem que ser mais escura que a cintura
	_check_ao(robe)

	# ---- identidade: todo mago do registro monta, respeita o porte e cabe no orcamento
	print("--- orcamento por mago (alvo < %d vertices) ---" % VERT_BUDGET)
	_check_mago("", "generico")
	for slug in IDS.slugs():
		_check_mago(slug, slug)


## O ORCAMENTO E' EM TRIANGULO. Ate' 25/08 o auditor cobrava VERTICE num teto de
## 20.000 e reprovava os DOIS modelos reais do jogo estando ambos por volta de
## 15,4 mil triangulos: costura de UV duplica vertice sem criar triangulo. Estes
## checks provam as tres coisas que o conserto precisa garantir.
func _check_orcamento_triangulos() -> void:
	print("[Orcamento geometrico medido em TRIANGULO]")
	var MA := load("res://characters/model_audit.gd")
	var Mage := load("res://characters/Mage.gd")
	_check(int(MA.MAX_TRIANGLES) == 20000 and int(MA.MIN_TRIANGLES) == 12000,
		"faixa aprovada e' 12k..20k triangulos, num lugar so'")

	# 1) contagem correta em superficie NAO indexada: 3 vertices = 1 triangulo
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in 30:
		st.add_vertex(Vector3(i, 0, 0))
		st.add_vertex(Vector3(i, 1, 0))
		st.add_vertex(Vector3(i, 0, 1))
	var solta: ArrayMesh = st.commit()
	var arr: Array = solta.surface_get_arrays(0)
	var nv := (arr[Mesh.ARRAY_VERTEX] as PackedVector3Array).size()
	_check(Mage._triangulos_da_superficie(solta, 0, arr, nv) == 30,
		"superficie solta: 90 vertices contam 30 triangulos")

	# 2) INDEXADA: o triangulo sai dos INDICES, nao dos vertices reaproveitados.
	# E' exatamente o caso que fazia a conta vertices/3 mentir.
	var st2 := SurfaceTool.new()
	st2.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in 4:
		st2.add_vertex(Vector3(i % 2, i / 2, 0))
	st2.add_index(0); st2.add_index(1); st2.add_index(2)
	st2.add_index(1); st2.add_index(2); st2.add_index(3)
	var idx: ArrayMesh = st2.commit()
	var arr2: Array = idx.surface_get_arrays(0)
	var nv2 := (arr2[Mesh.ARRAY_VERTEX] as PackedVector3Array).size()
	_check(Mage._triangulos_da_superficie(idx, 0, arr2, nv2) == 2,
		"superficie indexada: 4 vertices e 6 indices contam 2 triangulos")

	# 3) O PORTAO PEGA um modelo acima do teto. Sem este check, subir MAX para
	# 20k so' teria trocado um numero errado por outro sem ninguem notar.
	var st3 := SurfaceTool.new()
	st3.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in 20001:
		st3.add_vertex(Vector3(i, 0, 0))
		st3.add_vertex(Vector3(i, 1, 0))
		st3.add_vertex(Vector3(i, 0, 1))
	var gordo: ArrayMesh = st3.commit()
	var arr3: Array = gordo.surface_get_arrays(0)
	var nv3 := (arr3[Mesh.ARRAY_VERTEX] as PackedVector3Array).size()
	var tris_gordo: int = Mage._triangulos_da_superficie(gordo, 0, arr3, nv3)
	_check(tris_gordo > int(MA.MAX_TRIANGLES),
		"malha de %d triangulos ESTOURA o teto de %d" % [tris_gordo, int(MA.MAX_TRIANGLES)])

	# 4) os modelos reais: dentro da faixa, e o relatorio traz o campo
	for slug in ["01-pyra", "13-brok"]:
		var arq := "res://characters/modelos/%s.glb" % slug.split("-", true, 1)[1]
		if not ResourceLoader.exists(arq):
			continue
		var m: Node3D = load("res://characters/Mage.tscn").instantiate()
		m.mage_id = slug
		root.add_child(m)
		var r: Dictionary = m.get_model_report()
		var t := int(r.get("triangles", 0))
		_check(t >= int(MA.MIN_TRIANGLES) and t <= int(MA.MAX_TRIANGLES),
			"%s: %d triangulos dentro da faixa (vertices %d, que NAO e' portao)"
			% [slug, t, int(r["vertices"])])
		m.queue_free()


func _check_materiais() -> void:
	var Mage := load("res://characters/Mage.gd")
	var mat: Dictionary = Mage.MAT
	for k in ["cloth", "metal", "skin", "energy"]:
		_check(mat.has(k), "preset de material '%s' existe" % k)
	_check(mat["metal"]["spec_amount"] > mat["cloth"]["spec_amount"] * 4.0,
		"metal brilha muito mais que tecido (%.2f vs %.2f)"
		% [mat["metal"]["spec_amount"], mat["cloth"]["spec_amount"]])
	_check(mat["skin"]["shade_wrap"] > mat["metal"]["shade_wrap"],
		"pele enrola a luz mais que metal (%.2f vs %.2f)"
		% [mat["skin"]["shade_wrap"], mat["metal"]["shade_wrap"]])
	_check(mat["energy"]["ao_strength"] == 0.0, "energia magica ignora o AO")


func _check_model_pipeline() -> void:
	var Mage := load("res://characters/Mage.gd")
	_check(Mage.MODEL_DIR == "res://characters/modelos",
		"modelos game-ready moram em res://characters/modelos")
	_check(Mage.model_path_for("01-pyra") == "res://characters/modelos/pyra.glb",
		"slug 01-pyra resolve para pyra.glb")
	_check(_mage.has_method("get_model_source"), "Mage expõe get_model_source()")
	_check(_mage.get_model_source() == "procedural",
		"sem .glb importado, Mage cai no procedural")
	_check_imported_model_contract()
	_check_real_pyra_glb()


func _check_imported_model_contract() -> void:
	var ok_path := "user://arkana_external_model_test.tscn"
	_save_external_model_test_scene(ok_path, 0.55, {
		"idle": "Idle",
		"run": "Armature|Running",
		"cast": "Spell Cast",
	})
	var m: Node3D = load("res://characters/Mage.tscn").instantiate()
	m.mage_id = "01-pyra"
	m.imported_model_path_override = ok_path
	root.add_child(m)

	var player: AnimationPlayer = m.get_node_or_null("ExternalModel/AnimationPlayer")
	_check(m.get_model_source() == "external:%s" % ok_path,
		"modelo externo valido substitui o procedural")
	_check(player != null, "modelo externo pode ter AnimationPlayer aninhado")
	m.play_anim("idle")
	_check(player != null and player.current_animation == "Idle",
		"alias externo Idle satisfaz contrato idle")
	m.play_anim("run")
	_check(player != null and player.current_animation == "Armature|Running",
		"alias externo Armature|Running satisfaz contrato run")
	_check(m.get_anim_length("cast") > m.get_cast_fire_time(),
		"alias externo Spell Cast aceita cast maior que o tempo de disparo")
	var report: Dictionary = m.get_model_report()
	_check(report["vertices"] > 0 and report["surface_count"] > 0,
		"relatorio tecnico mede vertices e superficies do externo")
	_check(report["material_slots"] > 0, "relatorio tecnico mede materiais do externo")
	_check(report["bones"] == 1, "relatorio tecnico mede ossos do externo")
	_check(report["animations"]["cast"] == "Spell Cast",
		"relatorio tecnico registra alias real da animacao")
	var body: MeshInstance3D = m.get_node("ExternalModel/Body")
	var before: Material = body.material_override
	m.set_tint(Color(0.9, 0.1, 0.1))
	var after: Material = body.material_override
	_check(before == after and after is StandardMaterial3D \
		and (after as StandardMaterial3D).albedo_color == Color(0.9, 0.1, 0.1),
		"set_tint recolore material duplicado do externo")
	var fired := []
	m.cast_fired.connect(func(): fired.append(true))
	# PEGAR NAO E TIRO. Modelo externo sem a opcional "pegar" cai no substituto
	# "cast"; se o gatilho olhasse o nome FINAL em vez do PEDIDO, apanhar um
	# item do chao emitiria cast_fired. Achado pela raia de interacao em 26/08.
	m.play_anim("pegar")
	player.seek(m.get_cast_fire_time() + 0.01, true)
	m._process(0.01)
	_check(fired.is_empty(), "play_anim('pegar') caindo em 'cast' NAO emite cast_fired")
	m.play_anim("cast")
	player.seek(m.get_cast_fire_time() + 0.01, true)
	m._process(0.01)
	_check(fired.size() == 1, "modelo externo emite cast_fired por adaptador")
	m.play_anim("cast")
	m.play_anim("run")
	player.seek(m.get_cast_fire_time() + 0.01, true)
	m._process(0.01)
	_check(fired.size() == 1, "interromper cast externo com run nao emite cast_fired")
	m.queue_free()

	var short_path := "user://arkana_external_model_short_cast_test.tscn"
	_save_external_model_test_scene(short_path, 0.10, {
		"idle": "Idle",
		"run": "Running",
		"cast": "Spell Cast",
	})
	var bad: Node3D = load("res://characters/Mage.tscn").instantiate()
	bad.mage_id = "01-pyra"
	bad.imported_model_path_override = short_path
	root.add_child(bad)
	_check(bad.get_model_source() == "procedural",
		"modelo externo com cast curto cai no procedural")
	bad.queue_free()


func _check_real_pyra_glb() -> void:
	var path := "res://characters/modelos/pyra.glb"
	if not ResourceLoader.exists(path):
		_check(true, "pyra.glb ainda ausente; teste real pulado")
		return
	var m: Node3D = load("res://characters/Mage.tscn").instantiate()
	m.mage_id = "01-pyra"
	root.add_child(m)
	var report: Dictionary = m.get_model_report()
	_check(m.get_model_source() == "external:%s" % path,
		"01-pyra carrega pyra.glb real")
	_check(report["vertices"] > 0 and report["surface_count"] > 0,
		"pyra.glb real reporta geometria")
	_check(report["animations"].has("idle") and report["animations"].has("run") \
		and report["animations"].has("cast"),
		"pyra.glb real resolve idle/run/cast por aliases")
	_check(m.get_anim_length("cast") > m.get_cast_fire_time(),
		"pyra.glb real tem cast sincronizavel")

	# METAL DOMADO (video do Diretor, 26/08: a personagem anda como SILHUETA
	# PRETA pela ilha). O PBR da Meshy traz metallic alto, e metal e' quase todo
	# reflexo: sem reflection probe o renderer mobile devolve breu. O Mage
	# grampeia metallic <= 0.2 e roughness >= 0.45 na importacao. VERMELHO se
	# alguem remover o _domar_pbr.
	var pior_metal := 0.0
	var pilha: Array = [m]
	while not pilha.is_empty():
		var n: Node = pilha.pop_back()
		for c in n.get_children():
			pilha.append(c)
		if n is MeshInstance3D and (n as MeshInstance3D).mesh != null:
			var mi := n as MeshInstance3D
			for s in mi.mesh.get_surface_count():
				var mat := mi.mesh.surface_get_material(s)
				if mat is StandardMaterial3D:
					pior_metal = maxf(pior_metal, (mat as StandardMaterial3D).metallic)
	_check(pior_metal <= 0.21,
		"metallic grampeado p/ mobile sem probe (pior: %.2f)" % pior_metal)

	# O modelo do glTF olha para +Z; o jogo anda para -Z. Sem a meia-volta o
	# personagem corre de costas e patina — foi o que o Diretor viu no aparelho
	# em 25/08. Este teste fica VERMELHO se alguem tirar a correcao do Mage.gd.
	# LACO: o importador glTF traz tudo com LOOP_NONE. Sem o conserto, "run"
	# toca uns tres passos, congela e o corpo desliza — a patinacao relatada
	# pelo Diretor em 25/08. "cast" NAO pode repetir: e' disparo unico e o
	# cast_fired depende do fim dela. Este teste fica VERMELHO se o laco sumir.
	var ap: AnimationPlayer = m.get_animation_player_externo()
	if ap != null:
		var nomes: Dictionary = report["animations"]
		for contrato in ["idle", "run"]:
			var a := ap.get_animation(nomes[contrato])
			_check(a != null and a.loop_mode == Animation.LOOP_LINEAR,
				"anim externa '%s' repete em laco" % contrato)
		var ac := ap.get_animation(nomes["cast"])
		_check(ac != null and ac.loop_mode != Animation.LOOP_LINEAR,
			"anim externa 'cast' NAO repete (e' disparo unico)")

	var externo := m.get_node_or_null("ExternalModel")
	_check(externo != null, "modelo externo entra como no' 'ExternalModel'")
	if externo != null:
		var giro: float = absf(wrapf((externo as Node3D).rotation.y, -PI, PI))
		_check(absf(giro - PI) < 0.01,
			"modelo externo recebe a meia-volta (+Z do glTF -> -Z do jogo)")
	m.queue_free()


func _save_external_model_test_scene(path: String, cast_len: float, names: Dictionary) -> void:
	var scene_root := Node3D.new()
	scene_root.name = "ImportedMage"

	var mesh := MeshInstance3D.new()
	mesh.name = "Body"
	mesh.mesh = BoxMesh.new()
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.2, 0.4, 0.8)
	mesh.material_override = mat
	scene_root.add_child(mesh)
	mesh.owner = scene_root

	var skeleton := Skeleton3D.new()
	skeleton.name = "Skeleton3D"
	skeleton.add_bone("Root")
	scene_root.add_child(skeleton)
	skeleton.owner = scene_root

	var player := AnimationPlayer.new()
	player.name = "AnimationPlayer"
	var lib := AnimationLibrary.new()
	lib.add_animation(names["idle"], _test_anim(0.4, true))
	lib.add_animation(names["run"], _test_anim(0.4, true))
	lib.add_animation(names["cast"], _test_anim(cast_len, false))
	player.add_animation_library("", lib)
	scene_root.add_child(player)
	player.owner = scene_root

	var packed := PackedScene.new()
	var err := packed.pack(scene_root)
	_check(err == OK, "mock de modelo externo empacota")
	err = ResourceSaver.save(packed, path)
	_check(err == OK, "mock de modelo externo salva em %s" % path)
	scene_root.free()


func _test_anim(length: float, looped: bool) -> Animation:
	var a := Animation.new()
	a.length = length
	a.loop_mode = Animation.LOOP_LINEAR if looped else Animation.LOOP_NONE
	return a


func _check_ao(robe: MeshInstance3D) -> void:
	var arrays: Array = robe.mesh.surface_get_arrays(0)
	var vs: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
	var cs: Variant = arrays[Mesh.ARRAY_COLOR]
	if cs == null:
		_check(false, "manto tem cor de vertice (AO assado)")
		return
	var baixo := 1.0
	var alto := 0.0
	for i in vs.size():
		if vs[i].y < -0.85:
			baixo = min(baixo, (cs as PackedColorArray)[i].r)
		elif vs[i].y > 0.10:
			alto = max(alto, (cs as PackedColorArray)[i].r)
	_check(baixo < alto - 0.15,
		"AO de vertice: barra do manto (%.2f) mais escura que a cintura (%.2f)" % [baixo, alto])


## Monta um mago do registro e valida paleta, porte e orcamento de vertices.
func _check_mago(slug: String, rotulo: String) -> void:
	var m: Node3D = load("res://characters/Mage.tscn").instantiate()
	m.mage_id = slug
	m.prefer_imported_model = false
	root.add_child(m)
	var idf: Dictionary = m.get_mage_sheet()

	var verts := 0
	for mi in m.find_children("*", "MeshInstance3D", true, false):
		for s in mi.mesh.get_surface_count():
			verts += (mi.mesh.surface_get_arrays(s)[Mesh.ARRAY_VERTEX] as PackedVector3Array).size()

	# porte: o Rig escala pela altura da ficha (o anao 1,40 e o golem 2,30 tem
	# que MEDIR diferente, senao a silhueta por raca nao existe) e escala
	# UNIFORME — nao-uniforme aqui cisalha ombreira/braco/capuz rotacionados.
	var rig: Node3D = m.get_node("Rig")
	var esperado: float = float(idf["height"]) / IDS.REF_HEIGHT
	var porte_ok := is_equal_approx(rig.scale.y, esperado) \
		and is_equal_approx(rig.scale.x, esperado) \
		and is_equal_approx(rig.scale.z, esperado)

	var manto: MeshInstance3D = m.get_node("Rig/Hips/Robe")
	# a largura da raca vive no manto (e nao no Rig) — e' o que evita o cisalhamento
	var largura_ok := is_equal_approx(manto.scale.x, float(idf["bulk"]))
	var cor_ok: bool = manto.material_override.get_shader_parameter("albedo") \
		== Color(idf["robe"])

	print("  %-14s %4d vert · altura %.2fm · bulk %.2f · manto #%s"
		% [rotulo, verts, idf["height"], idf["bulk"], idf["robe"]])
	_check(verts > 0 and verts < VERT_BUDGET, "%s: orcamento de vertices (%d)" % [rotulo, verts])
	_check(porte_ok, "%s: altura aplicada, Rig com escala UNIFORME (%.2f)" % [rotulo, rig.scale.y])
	_check(largura_ok, "%s: largura da raca no manto (%.2f)" % [rotulo, manto.scale.x])
	_check(cor_ok, "%s: paleta da ficha aplicada no manto" % rotulo)
	_check_sem_cisalhamento(m, rotulo)
	m.queue_free()


## Escala nao-uniforme num PAI cisalha todo filho rotacionado (foi o que torceu
## as ombreiras do anao). Cisalhamento = colunas da base deixam de ser
## perpendiculares — barato de detectar e vale para qualquer mago futuro.
func _check_sem_cisalhamento(m: Node3D, rotulo: String) -> void:
	var pior := 0.0
	var culpado := ""
	for mi in m.find_children("*", "MeshInstance3D", true, false):
		var b: Basis = (mi as MeshInstance3D).global_basis
		var x := b.x.normalized()
		var y := b.y.normalized()
		var z := b.z.normalized()
		var d: float = max(absf(x.dot(y)), max(absf(x.dot(z)), absf(y.dot(z))))
		if d > pior:
			pior = d
			culpado = String(mi.name)
	_check(pior < 0.01, "%s: sem cisalhamento (pior %.4f em %s)" % [rotulo, pior, culpado])


func _check(ok: bool, msg: String) -> void:
	print(("PASS   " if ok else "FALHOU ") + msg)
	if not ok:
		_fails += 1
