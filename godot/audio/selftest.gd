## Selftest headless da raia AUDIO (R19). Rodar:
##   Godot --headless --path godot --script res://audio/selftest.gd
## Prova: Sfx instancia; cada evento do Bus dispara o player certo (espiao:
## meta "last_key" + stream atribuido ao pool — headless nao tem device de
## som, entao validamos play()/stream, nao "soou"); os 5 timbres de disparo
## sao PCM DISTINTOS; Bus ausente e sintese quebrada NAO derrubam nada.
##
## NOTA (mesma do menu/selftest.gd): em modo --script os autoloads so'
## registram DEPOIS deste arquivo compilar — Bus chega via get_node no 1o frame.
extends SceneTree

var fails := 0


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	var bus: Node = root.get_node_or_null("Bus")
	if bus == null:  # rede de seguranca caso o modo --script mude
		bus = (load("res://core/Bus.gd") as GDScript).new()
		bus.name = "Bus"
		root.add_child(bus)

	# --- guarda 1: Sfx SEM Bus instancia, avisa e nao conecta (jogo segue) ---
	bus.name = "BusEscondido"
	var orfao: Node = (load("res://audio/Sfx.gd") as GDScript).new()
	orfao.name = "SfxOrfao"
	root.add_child(orfao)
	_check(is_instance_valid(orfao) and not orfao._connected,
		"sem Bus: instancia sem crash e nao conecta")
	orfao.free()
	bus.name = "Bus"

	# --- instancia normal ---
	var sfx: Node = (load("res://audio/Sfx.gd") as GDScript).new()
	sfx.name = "Sfx"
	root.add_child(sfx)
	_check(sfx._connected, "conectou ao Bus")
	_check(sfx._streams.size() >= 21, "sintetizou a paleta inteira (%d sons)" % sfx._streams.size())
	_check(AudioServer.get_bus_index("Sfx") != -1 and AudioServer.get_bus_index("Ambient") != -1,
		"buses Sfx e Ambient criados no AudioServer")

	# --- 5 timbres de disparo DISTINTOS (identidade sonora = acessibilidade) ---
	var shots := ["shot_fire", "shot_water", "shot_lightning", "shot_earth", "shot_wind"]
	var todos := true
	for k in shots:
		if sfx._streams.get(k) == null:
			todos = false
	_check(todos, "5 disparos elementais existem")
	var distintos := true
	for i in shots.size():
		for j in range(i + 1, shots.size()):
			if sfx._streams[shots[i]].data == sfx._streams[shots[j]].data:
				distintos = false
	_check(distintos, "5 disparos sao PCM DISTINTOS (par a par)")

	# --- cada evento do Bus -> player certo ---
	_event(sfx, bus, "damage_dealt", [null, 10, "fire"], "hit")
	_event(sfx, bus, "player_killed_bot", ["Bot1"], "kill")
	_event(sfx, bus, "dodge_performed", [], "dodge")
	_event(sfx, bus, "element_changed", ["water"], "click_water")
	_event(sfx, bus, "game_start_requested", [], "ui_tick")
	_event(sfx, bus, "terrain_hit", ["lightning", Vector3.ZERO, false], "shot_lightning")
	_event(sfx, bus, "terrain_changed", ["fire_patch", Vector3.ZERO], "terrain_fire")
	_event(sfx, bus, "terrain_changed", ["ice_floor", Vector3.ZERO], "terrain_ice")
	_event(sfx, bus, "terrain_changed", ["zap_water", Vector3.ZERO], "terrain_zap")
	_event(sfx, bus, "terrain_changed", ["wall", Vector3.ZERO], "terrain_wall")
	_event(sfx, bus, "match_over", [true], "fanfare")
	_event(sfx, bus, "match_over", [false], "defeat")

	# --- ambiente liga na partida e desliga no fim ---
	_event(sfx, bus, "match_started", [], "sting_start")
	_check(sfx._wind.playing and sfx._wind.stream != null, "vento ambiente tocando na partida")
	_check(sfx._wind.stream.loop_mode == AudioStreamWAV.LOOP_FORWARD, "vento e' loop sem fim")
	bus.emit_signal("match_over", true)
	_check(not sfx._wind.playing, "ambiente para no fim da partida")

	# --- guarda 2: gerador quebrado => silencio, jogo segue ---
	_check(sfx._wav(PackedFloat32Array()) == null, "buffer vazio (sintese quebrada) vira null")
	sfx._streams["hit"] = sfx._wav(PackedFloat32Array())  # injeta o gerador quebrado
	sfx.set_meta("last_key", "")
	bus.emit_signal("damage_dealt", null, 5, "fire")
	_check(sfx.get_meta("last_key") == "", "stream nulo: NAO toca, NAO crasha — partida segue")
	# kind de terreno desconhecido tambem nao pode explodir
	bus.emit_signal("terrain_changed", "sabor_novo_de_r20", Vector3.ZERO)
	_check(sfx.get_meta("last_key") == "", "kind desconhecido: silencio (melhor que timbre errado)")

	if fails == 0:
		print("SELFTEST OK")
	else:
		printerr("SELFTEST FALHOU: %d" % fails)
	quit(0 if fails == 0 else 1)


## Espiao: emite o sinal e confere que o ULTIMO player do pool recebeu o
## stream esperado e chamou play() (headless: valida atribuicao, nao audicao).
func _event(sfx: Node, bus: Node, sig: String, args: Array, expected: String) -> void:
	sfx.set_meta("last_key", "")
	bus.callv("emit_signal", [sig] + args)
	var p: AudioStreamPlayer = sfx._pool[(sfx._pool_i - 1 + sfx._pool.size()) % sfx._pool.size()]
	var ok: bool = sfx.get_meta("last_key") == expected \
		and p.stream == sfx._streams.get(expected) and p.playing
	_check(ok, "%s -> %s" % [sig, expected])


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)
