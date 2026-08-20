## Prova headless da raia PERSONAGEM (R16):
##   Godot --headless --path godot --script res://characters/selftest.gd
## Instancia em _initialize e checa em _process (o _ready da cena so' roda
## depois que o loop principal comeca).
extends SceneTree

var _mage: Node
var _frames := 0


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
	var fails := _run_checks()
	print("RESULTADO: %s" % ("OK — 0 falhas" if fails == 0 else "%d FALHAS" % fails))
	quit(0 if fails == 0 else 1)
	return true


func _run_checks() -> int:
	var fails := 0
	fails += _check(_mage is Node3D, "raiz e' Node3D (contrato ARQUITETURA.md)")

	var player: AnimationPlayer = _mage.get_node_or_null("AnimationPlayer")
	fails += _check(player != null, "AnimationPlayer existe")
	for nome in ["idle", "run", "cast"]:
		_mage.play_anim(nome)
		var ok: bool = player != null and player.has_animation(nome)
		var dur: float = player.get_animation(nome).length if ok else 0.0
		fails += _check(ok and dur > 0.0, "anim '%s' existe, duracao %.2fs > 0" % [nome, dur])
		fails += _check(player != null and player.current_animation == nome,
			"play_anim('%s') ativa a animacao" % nome)

	var robe: MeshInstance3D = _mage.get_node("Rig/Hips/Robe")
	var antes: Variant = robe.material_override.get_shader_parameter("tint")
	_mage.set_tint(Color(1, 0.2, 0.2))
	var depois: Variant = robe.material_override.get_shader_parameter("tint")
	fails += _check(depois == Color(1, 0.2, 0.2) and str(antes) != str(depois),
		"set_tint mudou o material do manto (%s -> %s)" % [antes, depois])

	fails += _check(_mage.has_signal("cast_fired"), "sinal cast_fired existe (gatilho do projetil)")
	var fire: float = _mage.get_cast_fire_time()
	fails += _check(fire > 0.0 and fire < _mage.get_anim_length("cast"),
		"cast_fire_time (%.2fs) dentro da anim cast (%.2fs)" % [fire, _mage.get_anim_length("cast")])

	var verts := 0
	for mi in _mage.find_children("*", "MeshInstance3D", true, false):
		for s in mi.mesh.get_surface_count():
			verts += (mi.mesh.surface_get_arrays(s)[Mesh.ARRAY_VERTEX] as PackedVector3Array).size()
	print("Vertices totais: %d (~%d tris) — alvo < 8000 (mobile)" % [verts, verts / 3])
	fails += _check(verts > 0 and verts < 8000, "orcamento de vertices")
	return fails


func _check(ok: bool, msg: String) -> int:
	print(("PASS   " if ok else "FALHOU ") + msg)
	return 0 if ok else 1
