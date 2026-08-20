## CENA PRINCIPAL — a partida jogavel de 3 min (G1: jogo, nao sandbox).
## Fiacao defensiva: Island/Mage carregam com fallback; esta cena BOOTA
## sozinha. Tudo por-partida vive sob "Arena" — restart = arena nova, limpa
## (estado que atravessa partida ja' vazou 3x no projeto).
extends Node3D

const ISLAND_SCENE := "res://world/Island.tscn"
const BOT_TINTS: Array[Color] = [
	Color(0.9, 0.3, 0.3), Color(0.3, 0.7, 0.9), Color(0.4, 0.85, 0.4),
	Color(0.95, 0.8, 0.3), Color(0.75, 0.4, 0.9), Color(0.95, 0.55, 0.25),
]

enum { PLAYING, OVER }

var match_state := PLAYING
var time_left: float = float(Balance.MATCH.duration_s)
var bots_alive := 0
var arena: Node3D
var player: Player
var hud: Hud
var _spawns: Array[Vector3] = []


func _ready() -> void:
	add_child(_load_island())
	_collect_spawns()
	hud = Hud.new()
	add_child(hud)
	hud.restart_pressed.connect(_build_match)
	Bus.entity_died.connect(_on_entity_died)
	_build_match()


func _process(delta: float) -> void:
	if match_state != PLAYING:
		return
	time_left -= delta
	if time_left <= 0.0:
		time_left = 0.0
		_end_match(false)
	hud.update_match(time_left, bots_alive)


## Monta (ou REMONTA do zero) uma partida. Chamado no boot e no "jogar de novo".
func _build_match() -> void:
	if is_instance_valid(arena):
		arena.process_mode = Node.PROCESS_MODE_DISABLED
		arena.queue_free()
	arena = Node3D.new()
	arena.name = "Arena"
	add_child(arena)
	match_state = PLAYING
	time_left = float(Balance.MATCH.duration_s)
	bots_alive = int(Balance.MATCH.bots)
	var pts := _spawn_points(1 + bots_alive)
	player = Player.new()
	arena.add_child(player)
	player.global_position = pts[0]
	for i in bots_alive:
		var b := Bot.new()
		b.target = player
		arena.add_child(b)
		b.global_position = pts[i + 1]
		b.set_tint(BOT_TINTS[i % BOT_TINTS.size()])
	hud.bind_player(player)
	hud.hide_end()
	hud.update_match(time_left, bots_alive)
	Bus.match_started.emit()


func _on_entity_died(entity: Node) -> void:
	if match_state != PLAYING:
		return
	if entity is Bot:
		bots_alive -= 1
		if bots_alive <= 0:
			_end_match(true)
	elif entity.is_in_group("player"):
		_end_match(false)


func _end_match(victory: bool) -> void:
	match_state = OVER
	arena.process_mode = Node.PROCESS_MODE_DISABLED
	hud.show_end(victory)
	Bus.match_over.emit(victory)


# ---------- mundo ----------

func _load_island() -> Node3D:
	if ResourceLoader.exists(ISLAND_SCENE):
		var packed: Variant = load(ISLAND_SCENE)
		if packed is PackedScene:
			var n: Node = (packed as PackedScene).instantiate()
			if n is Node3D:
				return n
			n.free()
	return _fallback_island()


func _fallback_island() -> Node3D:
	var n := Node3D.new()
	var ground := StaticBody3D.new()
	var col := CollisionShape3D.new()
	col.shape = WorldBoundaryShape3D.new()
	ground.add_child(col)
	var mesh := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(200, 200)
	mesh.mesh = plane
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.3, 0.55, 0.3)
	mesh.material_override = mat
	ground.add_child(mesh)
	n.add_child(ground)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-50, -30, 0)
	sun.shadow_enabled = true
	n.add_child(sun)
	var we := WorldEnvironment.new()
	var env := Environment.new()
	var sky := Sky.new()
	sky.sky_material = ProceduralSkyMaterial.new()
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
	we.environment = env
	n.add_child(we)
	return n


func _collect_spawns() -> void:
	_spawns.clear()
	for m in get_tree().get_nodes_in_group("spawn"):
		if m is Node3D:
			_spawns.append((m as Node3D).global_position)


func _spawn_points(count: int) -> Array[Vector3]:
	var pts: Array[Vector3] = []
	if _spawns.is_empty():
		# fallback: player no centro, bots em anel de 30m (fora do raio de
		# perseguicao — a partida abre com cacada, nao com mob no spawn)
		pts.append(Vector3(0, 1.0, 0))
		for i in range(count - 1):
			var a := TAU * float(i) / float(count - 1)
			pts.append(Vector3(cos(a) * 30.0, 1.0, sin(a) * 30.0))
	else:
		for i in range(count):
			pts.append(_spawns[i % _spawns.size()] + Vector3(randf_range(-1, 1), 0.5, randf_range(-1, 1)))
	return pts
