## Sfx — raia AUDIO (R19). Autoload (o COORDENADOR adiciona no project.godot):
##   Sfx="*res://audio/Sfx.gd"
##
## TUDO procedural: PCM sintetizado UMA vez no _ready (AudioStreamWAV 16-bit
## mono 22050 Hz) — zero binario no repo, custo por frame = zero.
##
## REGRA HERDADA DO PROJETO-MAE: audio NUNCA decide jogo. Este no' so' OBSERVA
## o Bus (checagens defensivas em toda ponta); se ele faltar ou a sintese
## quebrar, a partida segue em silencio — nunca o contrario.
##
## Disparo por elemento HOJE toca no impacto (Bus.terrain_hit carrega o
## elemento). Para tocar no INSTANTE do disparo, pedimos ao coordenador o
## sinal `spell_cast(element)` no Bus — a fiacao abaixo ja' o observa se
## existir (has_signal), entao e' so' adicionar o sinal e emitir.
extends Node

const SR := 22050
const POOL := 6  # vozes simultaneas de SFX; suficiente p/ 1 player + 3 bots

var _streams: Dictionary = {}
var _pool: Array[AudioStreamPlayer] = []
var _pool_i := 0
var _wind: AudioStreamPlayer
var _birds: AudioStreamPlayer
var _bird_timer: Timer
var _connected := false
var _rng := RandomNumberGenerator.new()


func _ready() -> void:
	_rng.seed = 1337  # deterministico: mesmo timbre em todo aparelho
	_setup_buses()
	_setup_players()
	_synth_all()
	_connect_bus()
	set_meta("last_key", "")  # espiao p/ selftest headless (sem device de som)


# ---------------------------------------------------------------- buses
## Master ja' existe; criamos Sfx e Ambient com defaults conservadores
## (Diretor testa em fone E alto-falante de celular — melhor baixo que estourado).
func _setup_buses() -> void:
	for cfg: Array in [["Sfx", -8.0], ["Ambient", -18.0]]:
		if AudioServer.get_bus_index(cfg[0]) == -1:
			var i := AudioServer.bus_count
			AudioServer.add_bus(i)
			AudioServer.set_bus_name(i, cfg[0])
			AudioServer.set_bus_send(i, &"Master")
			AudioServer.set_bus_volume_db(i, cfg[1])


func _setup_players() -> void:
	for i in POOL:
		var p := AudioStreamPlayer.new()
		p.bus = &"Sfx"
		add_child(p)
		_pool.append(p)
	_wind = AudioStreamPlayer.new()
	_wind.bus = &"Ambient"
	add_child(_wind)
	_birds = AudioStreamPlayer.new()
	_birds.bus = &"Ambient"
	_birds.volume_db = -6.0
	add_child(_birds)
	_bird_timer = Timer.new()
	_bird_timer.one_shot = true
	_bird_timer.timeout.connect(_on_bird_timer)
	add_child(_bird_timer)


# ---------------------------------------------------------------- fiacao (defensiva)
func _connect_bus() -> void:
	var bus := get_node_or_null("/root/Bus")
	if bus == null:
		push_warning("Sfx: Bus ausente — audio mudo, jogo segue.")
		return
	_hook(bus, "damage_dealt", _on_damage_dealt)
	_hook(bus, "player_killed_bot", _on_player_killed_bot)
	_hook(bus, "dodge_performed", _on_dodge)
	_hook(bus, "element_changed", _on_element_changed)
	_hook(bus, "match_started", _on_match_started)
	_hook(bus, "match_over", _on_match_over)
	_hook(bus, "terrain_hit", _on_terrain_hit)
	_hook(bus, "terrain_changed", _on_terrain_changed)
	_hook(bus, "game_start_requested", _on_game_start_requested)
	_hook(bus, "spell_cast", _on_spell_cast)  # futuro: no-op ate o Bus ganhar o sinal
	_connected = true


## Sinal pode nao existir ainda (Bus e' do coordenador) — conecta so' se houver.
func _hook(bus: Node, sig: String, fn: Callable) -> void:
	if bus.has_signal(sig) and not bus.is_connected(sig, fn):
		bus.connect(sig, fn)


# ---------------------------------------------------------------- reproducao
## Toda reproducao passa aqui. Stream nulo/faltando (sintese quebrada) =>
## silencio e retorno — o jogo NUNCA percebe.
func _play(key: String, vol_db := 0.0) -> void:
	var stream: AudioStream = _streams.get(key)
	if stream == null:
		return
	var p := _pool[_pool_i]
	_pool_i = (_pool_i + 1) % _pool.size()
	p.stream = stream
	p.volume_db = vol_db
	p.play()
	set_meta("last_key", key)


## API publica p/ o instante do disparo (gameplay pode chamar defensivamente,
## ou o futuro Bus.spell_cast chega aqui).
func play_shot(element: String, vol_db := -4.0) -> void:
	_play("shot_" + element, vol_db)


# ---------------------------------------------------------------- handlers
func _on_spell_cast(element: String) -> void:
	play_shot(element)


func _on_damage_dealt(_target: Node, _amount: int, _element: String) -> void:
	_play("hit", -6.0)


func _on_player_killed_bot(_bot_name: String) -> void:
	_play("kill", -3.0)


func _on_dodge() -> void:
	_play("dodge", -8.0)


func _on_element_changed(element: String) -> void:
	if _streams.get("click_" + element) != null:
		_play("click_" + element, -8.0)
	else:
		_play("ui_tick", -10.0)


func _on_match_started() -> void:
	_play("sting_start", -4.0)
	_ambient_start()


func _on_match_over(victory: bool) -> void:
	_ambient_stop()
	_play("fanfare" if victory else "defeat", -2.0)


func _on_game_start_requested() -> void:
	_play("ui_tick", -10.0)


## Impacto elemental no terreno = a identidade sonora do disparo hoje.
func _on_terrain_hit(element: String, _pos: Vector3, strong: bool) -> void:
	play_shot(element, -3.0 if strong else -6.0)


## Os nomes de `kind` sao da raia gameplay/mundo — casamos por substring p/
## nao quebrar com renome. Desconhecido = silencio (melhor que timbre errado).
func _on_terrain_changed(kind: String, _pos: Vector3) -> void:
	var k := kind.to_lower()
	if k.contains("fire") or k.contains("fogo") or k.contains("burn"):
		_play("terrain_fire", -4.0)
	elif k.contains("ice") or k.contains("gelo") or k.contains("frost"):
		_play("terrain_ice", -4.0)
	elif k.contains("zap") or k.contains("elec") or k.contains("shock") or k.contains("raio"):
		_play("terrain_zap", -6.0)
	elif k.contains("wall") or k.contains("muro") or k.contains("stone") \
			or k.contains("rock") or k.contains("earth") or k.contains("terra"):
		_play("terrain_wall", -4.0)


# ---------------------------------------------------------------- ambiente
func _ambient_start() -> void:
	var wind: AudioStream = _streams.get("ambient_wind")
	if wind != null:
		_wind.stream = wind
		_wind.volume_db = -4.0
		_wind.play()
	if _streams.get("bird") != null:
		_bird_timer.start(_rng.randf_range(4.0, 10.0))


func _ambient_stop() -> void:
	_wind.stop()
	_bird_timer.stop()
	_birds.stop()


func _on_bird_timer() -> void:
	var chirp: AudioStream = _streams.get("bird")
	if chirp != null:
		_birds.stream = chirp
		_birds.play()
	_bird_timer.start(_rng.randf_range(6.0, 16.0))  # esparso, entardecer


# ================================================================ SINTESE
## ~9s de PCM total a 22050 Hz — gerado uma vez, guardado em _streams.
func _synth_all() -> void:
	_streams["shot_fire"] = _wav(_gen_shot_fire())
	_streams["shot_water"] = _wav(_gen_shot_water())
	_streams["shot_lightning"] = _wav(_gen_shot_lightning())
	_streams["shot_earth"] = _wav(_gen_shot_earth())
	_streams["shot_wind"] = _wav(_gen_shot_wind())
	_streams["hit"] = _wav(_gen_hit())
	_streams["kill"] = _wav(_gen_kill())
	_streams["dodge"] = _wav(_gen_dodge())
	# clique tonal na nota do elemento (identidade + acessibilidade)
	for e: Array in [["fire", 262.0], ["water", 330.0], ["lightning", 523.0],
			["earth", 196.0], ["wind", 440.0]]:
		_streams["click_" + e[0]] = _wav(_gen_click(e[1]))
	_streams["terrain_fire"] = _wav(_gen_terrain_fire())
	_streams["terrain_ice"] = _wav(_gen_terrain_ice())
	_streams["terrain_zap"] = _wav(_gen_terrain_zap())
	_streams["terrain_wall"] = _wav(_gen_terrain_wall())
	_streams["sting_start"] = _wav(_gen_sting_start())
	_streams["fanfare"] = _wav(_gen_fanfare())
	_streams["defeat"] = _wav(_gen_defeat())
	_streams["ui_tick"] = _wav(_gen_ui_tick())
	_streams["ambient_wind"] = _wav(_gen_wind_loop(), true)
	_streams["bird"] = _wav(_gen_bird())


## PCM float [-1,1] -> AudioStreamWAV. Buffer vazio (gerador quebrado) => null
## — e _play engole null em silencio. GUARDA: sintese nunca derruba o jogo.
func _wav(samples: PackedFloat32Array, loop := false) -> AudioStreamWAV:
	if samples.is_empty():
		return null
	var bytes := PackedByteArray()
	bytes.resize(samples.size() * 2)
	for i in samples.size():
		bytes.encode_s16(i * 2, int(clampf(samples[i], -1.0, 1.0) * 32000.0))
	var w := AudioStreamWAV.new()
	w.format = AudioStreamWAV.FORMAT_16_BITS
	w.mix_rate = SR
	w.stereo = false
	w.data = bytes
	if loop:
		w.loop_mode = AudioStreamWAV.LOOP_FORWARD
		w.loop_begin = 0
		w.loop_end = samples.size()
	return w


func _buf(dur: float) -> PackedFloat32Array:
	var b := PackedFloat32Array()
	b.resize(int(dur * SR))
	return b


## Seno com varredura f0->f1 + 2o harmonico opcional; ataque 3ms, decay exp.
func _add_tone(buf: PackedFloat32Array, start: float, dur: float,
		f0: float, f1: float, amp: float, h2 := 0.0) -> void:
	var s := int(start * SR)
	var n := int(dur * SR)
	var ph := 0.0
	for i in n:
		var idx := s + i
		if idx >= buf.size():
			break
		var t := float(i) / float(n)
		ph += TAU * lerpf(f0, f1, t) / SR
		var env := minf(t * float(n) / (0.003 * SR), 1.0) * pow(1.0 - t, 1.6)
		buf[idx] += (sin(ph) + h2 * sin(2.0 * ph)) * amp * env


## Ruido por lowpass 1-polo com corte varrendo fc0->fc1; env: decay/hump/rise.
func _add_noise(buf: PackedFloat32Array, start: float, dur: float,
		fc0: float, fc1: float, amp: float, env := "decay") -> void:
	var s := int(start * SR)
	var n := int(dur * SR)
	var lp := 0.0
	for i in n:
		var idx := s + i
		if idx >= buf.size():
			break
		var t := float(i) / float(n)
		var k := minf(1.0, TAU * lerpf(fc0, fc1, t) / SR)
		lp += k * ((_rng.randf() * 2.0 - 1.0) - lp)
		var e: float
		match env:
			"hump":
				e = sin(PI * t)
			"rise":
				e = t * t
			_:
				e = pow(1.0 - t, 2.0)
		buf[idx] += lp * amp * e


# ---- disparos (5 timbres distintos = identidade sonora por elemento) ----
func _gen_shot_fire() -> PackedFloat32Array:  # sopro grave
	var b := _buf(0.30)
	_add_noise(b, 0.0, 0.30, 1200.0, 250.0, 0.9, "decay")
	_add_tone(b, 0.0, 0.30, 95.0, 55.0, 0.35)
	return b


func _gen_shot_water() -> PackedFloat32Array:  # gota/splash
	var b := _buf(0.24)
	_add_tone(b, 0.0, 0.13, 260.0, 940.0, 0.5, 0.25)  # "bloop" subindo
	_add_noise(b, 0.09, 0.15, 2600.0, 900.0, 0.3, "decay")  # respingo
	return b


func _gen_shot_lightning() -> PackedFloat32Array:  # zap curto
	var b := _buf(0.14)
	_add_noise(b, 0.0, 0.14, 6500.0, 2500.0, 0.55, "decay")
	_add_tone(b, 0.0, 0.12, 1750.0, 1400.0, 0.3, 0.6)
	_add_tone(b, 0.0, 0.08, 2600.0, 2300.0, 0.18)
	return b


func _gen_shot_earth() -> PackedFloat32Array:  # impacto de pedra
	var b := _buf(0.28)
	_add_tone(b, 0.0, 0.26, 105.0, 42.0, 0.7)
	_add_noise(b, 0.0, 0.07, 900.0, 400.0, 0.5, "decay")  # estalo do granito
	return b


func _gen_shot_wind() -> PackedFloat32Array:  # whoosh agudo
	var b := _buf(0.35)
	_add_noise(b, 0.0, 0.35, 700.0, 3600.0, 0.55, "hump")
	return b


# ---- combate ----
func _gen_hit() -> PackedFloat32Array:  # seco
	var b := _buf(0.09)
	_add_noise(b, 0.0, 0.09, 2200.0, 800.0, 0.6, "decay")
	_add_tone(b, 0.0, 0.07, 240.0, 180.0, 0.4)
	return b


func _gen_kill() -> PackedFloat32Array:  # punch + nota de recompensa
	var b := _buf(0.45)
	_add_tone(b, 0.0, 0.12, 90.0, 45.0, 0.8)  # punch
	_add_noise(b, 0.0, 0.05, 1500.0, 600.0, 0.4, "decay")
	_add_tone(b, 0.10, 0.16, 659.0, 659.0, 0.3, 0.35)  # E5
	_add_tone(b, 0.20, 0.25, 880.0, 880.0, 0.32, 0.35)  # A5 — a recompensa
	return b


func _gen_dodge() -> PackedFloat32Array:  # whoosh suave
	var b := _buf(0.25)
	_add_noise(b, 0.0, 0.25, 400.0, 1900.0, 0.45, "hump")
	return b


func _gen_click(freq: float) -> PackedFloat32Array:  # clique tonal do elemento
	var b := _buf(0.09)
	_add_tone(b, 0.0, 0.09, freq, freq, 0.5, 0.3)
	return b


# ---- terreno reativo ----
func _gen_terrain_fire() -> PackedFloat32Array:  # crepitar
	var b := _buf(0.60)
	var energy := 0.0
	for i in b.size():
		if _rng.randf() < 0.0025:
			energy = _rng.randf_range(0.5, 1.0)
		var fade := 1.0 - float(i) / float(b.size())
		b[i] = (_rng.randf() * 2.0 - 1.0) * energy * 0.7 * fade
		energy *= 0.990
	_add_noise(b, 0.0, 0.60, 300.0, 200.0, 0.15, "hump")  # cama do fogo
	return b


func _gen_terrain_ice() -> PackedFloat32Array:  # cristal
	var b := _buf(0.45)
	_add_tone(b, 0.0, 0.45, 1568.0, 1568.0, 0.22, 0.4)
	_add_tone(b, 0.0, 0.40, 1573.0, 1573.0, 0.18)  # batimento = brilho
	_add_tone(b, 0.04, 0.35, 2093.0, 2093.0, 0.15)
	return b


func _gen_terrain_zap() -> PackedFloat32Array:  # zumbido (raio na agua)
	var b := _buf(0.50)
	var ph := 0.0
	for i in b.size():
		var t := float(i) / SR
		ph += TAU * 110.0 / SR
		var am := 0.6 + 0.4 * sin(TAU * 9.0 * t)
		var fade := pow(1.0 - float(i) / float(b.size()), 0.8)
		b[i] = signf(sin(ph)) * 0.22 * am * fade
	_add_tone(b, 0.0, 0.45, 880.0, 870.0, 0.1)
	return b


func _gen_terrain_wall() -> PackedFloat32Array:  # pedra subindo
	var b := _buf(0.45)
	_add_noise(b, 0.0, 0.40, 100.0, 320.0, 1.0, "rise")
	_add_tone(b, 0.0, 0.42, 50.0, 115.0, 0.4)
	_add_noise(b, 0.38, 0.07, 700.0, 300.0, 0.5, "decay")  # trava no topo
	return b


# ---- UI / partida ----
func _gen_sting_start() -> PackedFloat32Array:
	var b := _buf(0.55)
	_add_tone(b, 0.0, 0.15, 440.0, 440.0, 0.35, 0.3)
	_add_tone(b, 0.14, 0.40, 659.0, 659.0, 0.4, 0.35)
	return b


func _gen_fanfare() -> PackedFloat32Array:  # 3 notas douradas (C-E-G)
	var b := _buf(0.95)
	_add_tone(b, 0.0, 0.20, 523.0, 523.0, 0.35, 0.4)
	_add_tone(b, 0.18, 0.20, 659.0, 659.0, 0.35, 0.4)
	_add_tone(b, 0.36, 0.58, 784.0, 784.0, 0.4, 0.45)
	_add_tone(b, 0.36, 0.58, 1568.0, 1568.0, 0.12)  # brilho de ouro
	return b


func _gen_defeat() -> PackedFloat32Array:  # 2 notas graves descendo
	var b := _buf(0.85)
	_add_tone(b, 0.0, 0.35, 220.0, 220.0, 0.4, 0.2)
	_add_tone(b, 0.32, 0.52, 175.0, 170.0, 0.42, 0.2)
	return b


func _gen_ui_tick() -> PackedFloat32Array:
	var b := _buf(0.05)
	_add_tone(b, 0.0, 0.05, 1200.0, 1100.0, 0.4)
	return b


# ---- ambiente ----
func _gen_wind_loop() -> PackedFloat32Array:  # 2s, costura por crossfade
	var dur := 2.0
	var fade := int(0.25 * SR)
	var n := int(dur * SR)
	var raw := PackedFloat32Array()
	raw.resize(n + fade)
	var lp := 0.0
	for i in raw.size():
		var t := float(i) / SR
		var fc := 260.0 + 140.0 * sin(TAU * t / dur)  # respiracao: 1 ciclo/loop
		var k := minf(1.0, TAU * fc / SR)
		lp += k * ((_rng.randf() * 2.0 - 1.0) - lp)
		raw[i] = lp * 0.9
	var b := raw.slice(0, n)
	for i in fade:  # fim ~ comeco => loop sem clique
		var a := float(i) / float(fade)
		b[i] = b[i] * a + raw[n + i] * (1.0 - a)
	return b


func _gen_bird() -> PackedFloat32Array:  # passaro esparso do entardecer
	var b := _buf(0.32)
	_add_tone(b, 0.0, 0.06, 3400.0, 2500.0, 0.22)
	_add_tone(b, 0.12, 0.05, 3000.0, 2300.0, 0.18)
	_add_tone(b, 0.20, 0.07, 3600.0, 2700.0, 0.2)
	return b
