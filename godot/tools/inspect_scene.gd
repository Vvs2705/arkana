extends SceneTree


func _initialize() -> void:
	var path := "res://characters/modelos/pyra.glb"
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--scene="):
			path = arg.trim_prefix("--scene=")

	var packed: PackedScene = load(path)
	if packed == null:
		push_error("Nao carregou %s" % path)
		quit(1)
		return
	var inst := packed.instantiate()
	root.add_child(inst)
	print("SCENE %s" % path)
	_dump(inst, 0)
	quit(0)


func _dump(n: Node, depth: int) -> void:
	var pad := "  ".repeat(depth)
	print("%s%s <%s>" % [pad, n.name, n.get_class()])
	if n is AnimationPlayer:
		var p := n as AnimationPlayer
		print("%s  animations=%s" % [pad, p.get_animation_list()])
		for a in p.get_animation_list():
			print("%s  - %s len=%.3f tracks=%d" % [
				pad, a, p.get_animation(a).length, p.get_animation(a).get_track_count()
			])
	if n is MeshInstance3D and (n as MeshInstance3D).mesh != null:
		var mi := n as MeshInstance3D
		var verts := 0
		for s in mi.mesh.get_surface_count():
			verts += (mi.mesh.surface_get_arrays(s)[Mesh.ARRAY_VERTEX] as PackedVector3Array).size()
		print("%s  mesh surfaces=%d vertices=%d" % [pad, mi.mesh.get_surface_count(), verts])
	if n is Skeleton3D:
		print("%s  bones=%d" % [pad, (n as Skeleton3D).get_bone_count()])
	for c in n.get_children():
		_dump(c, depth + 1)
