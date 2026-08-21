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
	_check(sfx._streams.size() >= 47, "sintetizou a paleta inteira (%d sons)" % sfx._streams.size())
	## Os 4 buses do menu de Configuracoes (menu/Config.gd BUSES) + Ambient.
	## Slider sem bus = slider morto: isto e' o contrato com a raia de config.
	var buses_ok := true
	for b in ["Master", "Music", "Sfx", "Ui", "Ambient"]:
		if AudioServer.get_bus_index(b) == -1:
			buses_ok = false
	_check(buses_ok, "buses Master/Music/Sfx/Ui/Ambient existem (sliders das Configuracoes)")
	_check(AudioServer.get_bus_send(AudioServer.get_bus_index("Ambient")) == &"Sfx",
		"Ambient manda para Sfx (o slider Efeitos tambem baixa o vento)")

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
	## Acerto direto / em escudo / tique de DoT sao TRES leituras diferentes:
	## e' o sinal `damage_applied` que separa as tres (source e on_shield).
	_event(sfx, bus, "damage_applied", [null, 10.0, "fire", sfx, false], "hit")
	_event(sfx, bus, "damage_applied", [null, 10.0, "fire", sfx, true], "escudo_clank")
	_event(sfx, bus, "damage_applied", [null, 2.0, "fire", null, false], "st_queimar")
	_event(sfx, bus, "damage_applied", [null, 2.0, "terrain", null, false], "dot_terreno")
	_event(sfx, bus, "entity_died", [null], "baque")
	_event(sfx, bus, "player_killed_bot", ["Bot1"], "kill")
	_event(sfx, bus, "dodge_performed", [], "dodge")
	_event(sfx, bus, "element_changed", ["water"], "click_water", "ui")
	_event(sfx, bus, "game_start_requested", [], "ui_tick", "ui")
	_event(sfx, bus, "terrain_hit", ["lightning", Vector3.ZERO, false], "shot_lightning")
	_event(sfx, bus, "terrain_changed", ["fire_patch", Vector3.ZERO], "terrain_fire")
	_event(sfx, bus, "terrain_changed", ["ice_floor", Vector3.ZERO], "terrain_ice")
	_event(sfx, bus, "terrain_changed", ["zap_water", Vector3.ZERO], "terrain_zap")
	_event(sfx, bus, "terrain_changed", ["wall", Vector3.ZERO], "terrain_wall")
	_event(sfx, bus, "match_over", [true], "fanfare", "music")
	_event(sfx, bus, "match_over", [false], "defeat", "music")

	# --- R21: as mecanicas que entraram hoje ---
	_r21(sfx, bus)

	# --- ambiente liga na partida e desliga no fim ---
	_event(sfx, bus, "match_started", [], "sting_start", "music")
	_check(sfx._wind.playing and sfx._wind.stream != null, "vento ambiente tocando na partida")
	_check(sfx._wind.stream.loop_mode == AudioStreamWAV.LOOP_FORWARD, "vento e' loop sem fim")
	bus.emit_signal("match_over", true)
	_check(not sfx._wind.playing, "ambiente para no fim da partida")

	## --- guarda 1b: BUS MAGRO (sinal que o Sfx observa ainda nao existe) ---
	## E' o caso real: tres raias estao adicionando sinais AGORA. Sfx tem que
	## subir com um Bus que so' tem parte deles, sem derrubar o boot.
	bus.name = "BusEscondido"
	var magro := Node.new()
	magro.name = "Bus"
	magro.add_user_signal("match_started")  # so' UM dos ~25 que o Sfx observa
	root.add_child(magro)
	var enxuto: Node = (load("res://audio/Sfx.gd") as GDScript).new()
	enxuto.name = "SfxEnxuto"
	root.add_child(enxuto)
	_check(is_instance_valid(enxuto) and enxuto._connected,
		"Bus SEM os sinais novos: conecta o que existe e ignora o resto (boot vivo)")
	magro.emit_signal("match_started")
	_check(enxuto.get_meta("last_key") == "sting_start",
		"...e o sinal que EXISTE continua tocando")
	enxuto.free()
	magro.free()
	bus.name = "Bus"

	# --- guarda 2: gerador quebrado => silencio, jogo segue ---
	_check(sfx._wav(PackedFloat32Array()) == null, "buffer vazio (sintese quebrada) vira null")
	sfx._streams["hit"] = sfx._wav(PackedFloat32Array())  # injeta o gerador quebrado
	sfx.set_meta("last_key", "")
	sfx._throttle.clear()
	bus.emit_signal("damage_applied", null, 5.0, "fire", sfx, false)
	_check(sfx.get_meta("last_key") == "", "stream nulo: NAO toca, NAO crasha — partida segue")
	# kind de terreno desconhecido tambem nao pode explodir
	bus.emit_signal("terrain_changed", "sabor_novo_de_r20", Vector3.ZERO)
	_check(sfx.get_meta("last_key") == "", "kind desconhecido: silencio (melhor que timbre errado)")

	if fails == 0:
		print("SELFTEST OK")
	else:
		printerr("SELFTEST FALHOU: %d" % fails)
	quit(0 if fails == 0 else 1)


## R21 — as mecanicas que entraram hoje: armas arcanas, bau celestial,
## habilidades (telegrafia), escudo e estados. Mais as tres garantias de
## MIXAGEM que o Diretor pediu: throttle, teto de vozes e prioridade.
func _r21(sfx: Node, bus: Node) -> void:
	# --- armas arcanas (GDD §16.2) ---
	_event(sfx, bus, "loot_prompt", ["Cajado", "raro", true], "loot_perto", "ui")
	_event(sfx, bus, "weapon_equipped",
		["cajado", "Cajado", "raro", PackedStringArray(["fire"])], "arma_equipar")

	# --- O ESTALO DA MANOPLA: so' sai depois que o player TEM a manopla ---
	sfx._manopla = false
	sfx.set_meta("last_key", "")
	sfx._throttle.clear()
	bus.emit_signal("spell_cast", "fire")
	_check(sfx.get_meta("last_key") == "shot_fire",
		"sem manopla: disparo normal, sem estalo")
	_event(sfx, bus, "weapon_equipped",
		["manopla", "Manopla", "lendaria", PackedStringArray(["fire", "wind"])],
		"manopla_equipar")
	_check(sfx._manopla, "equipar manopla ARMA o estalo dos dedos")
	sfx._throttle.clear()
	sfx.set_meta("last_key", "")
	var estalou := false
	bus.emit_signal("spell_cast", "fire")
	for p: AudioStreamPlayer in sfx._pool:
		if p.stream == sfx._streams["estalo_manopla"] and p.playing:
			estalou = true
	_check(estalou and sfx.get_meta("last_key") == "shot_fire",
		"com manopla: ESTALO + disparo, nos dois (assinatura do GDD §16.2)")

	# --- A LEI DO §4.3: toda suprema avisa antes, alto ---
	_event(sfx, bus, "kit_telegraph", ["pyra", "suprema", 2.0, Vector3.ZERO], "telegrafia_rugido", "evento")
	var teve_aviso := false
	for p: AudioStreamPlayer in sfx._pool:
		if p.stream == sfx._streams["telegrafia"] and p.playing:
			teve_aviso = true
	_check(teve_aviso, "suprema TELEGRAFADA: aviso alto sai antes do efeito")
	_event(sfx, bus, "kit_cooldown", ["tatica", 8.0, 8.0], "kit_tatica")
	_event(sfx, bus, "kit_cooldown", ["suprema", 0.0, 60.0], "kit_pronto", "ui")
	_event(sfx, bus, "kit_state", ["sino_espectral", true], "sino")

	# --- escudo evolutivo (GDD §5) ---
	_event(sfx, bus, "shield_broken", [null], "escudo_quebra")

	# --- bau celestial: o evento que o mapa inteiro ouve ---
	_event(sfx, bus, "bau_anunciado", [Vector3(40, 0, 40), 6.0], "bau_queda", "evento")
	var teve_gongo := false
	for p: AudioStreamPlayer in sfx._pool:
		if p.stream == sfx._streams["bau_anuncio"] and p.playing:
			teve_gongo = true
	_check(teve_gongo, "bau anunciado: gongo + assobio da queda no ar")
	_event(sfx, bus, "bau_pousou", [Vector3(40, 0, 40)], "bau_pouso")
	_check(not sfx._evento.playing, "pouso corta o assobio da queda")
	_event(sfx, bus, "bau_canalizando", [0.5], "bau_canal", "evento")
	var pitch_meio: float = sfx._evento.pitch_scale
	bus.emit_signal("bau_canalizando", 1.0)
	_check(sfx._evento.pitch_scale > pitch_meio,
		"canalizacao: o zumbido SOBE com a barra (sem stream nova por frame)")
	bus.emit_signal("bau_canalizando", 0.0)
	_check(not sfx._evento.playing, "cancelar a canalizacao corta o zumbido")
	_event(sfx, bus, "bau_aberto", [true, PackedStringArray(["fire", "wind"])], "bau_aberto")

	# --- estados elementais (sinal novo, ainda sem emissor no gameplay) ---
	_check(bus.has_signal("status_aplicado"), "Bus tem o sinal status_aplicado")
	for st: Array in [["burn", "st_burn"], ["wet", "st_wet"], ["frost", "st_frost"],
			["stun", "st_stun"]]:
		_event(sfx, bus, "status_aplicado", [null, st[0]], st[1])
	sfx.set_meta("last_key", "")
	bus.emit_signal("status_aplicado", null, "estado_que_ninguem_inventou_ainda")
	_check(sfx.get_meta("last_key") == "", "estado sem timbre: silencio, nunca timbre errado")

	_mixagem(sfx)
	_distancia(sfx)


## MIXAGEM: throttle, teto de vozes e prioridade. Sem isto, 6 bots + terreno +
## bau viram lama e a telegrafia da suprema morre embaixo de um tiro.
func _mixagem(sfx: Node) -> void:
	# --- throttle: repeticao dentro da janela nao sai ---
	sfx._throttle.clear()
	_reset_vozes(sfx)
	var t1: bool = sfx._play("hit", 0.0, sfx.P_NORMAL, 500)
	var t2: bool = sfx._play("hit", 0.0, sfx.P_NORMAL, 500)
	_check(t1 and not t2, "throttle: 2o disparo da mesma key na janela NAO soa")

	# --- teto de vozes: enche o pool e confere que o teto vale ---
	sfx._throttle.clear()
	_reset_vozes(sfx)
	var soaram := 0
	for i in sfx.POOL:
		if sfx._play("terrain_fire", 0.0, sfx.P_NORMAL):
			soaram += 1
	_check(soaram == sfx.POOL, "teto de vozes: o pool inteiro (%d) toca" % sfx.POOL)
	_check(not sfx._play("terrain_ice", 0.0, sfx.P_BAIXA),
		"pool cheio: som MENOS importante e' recusado (nao vira lama)")

	# --- prioridade: o critico SEMPRE entra, mesmo com tudo ocupado ---
	_check(sfx._play("telegrafia", 0.0, sfx.P_CRITICA),
		"pool cheio: a TELEGRAFIA da suprema entra assim mesmo (GDD §4.3)")
	## ...e depois de entrar, ela nao pode ser roubada por um tiro qualquer.
	for i in sfx.POOL:
		sfx._play("shot_fire", 0.0, sfx.P_NORMAL)
	var sobreviveu := false
	for p: AudioStreamPlayer in sfx._pool:
		if p.stream == sfx._streams["telegrafia"]:
			sobreviveu = true
	_check(sobreviveu, "telegrafia NAO e' abafada por disparos que vem depois")
	_reset_vozes(sfx)


## Libera todas as vozes (o teste nao pode depender do relogio real).
func _reset_vozes(sfx: Node) -> void:
	for i in sfx.POOL:
		sfx._free_at[i] = 0
		sfx._prio[i] = 0
		sfx._pool[i].stop()
		sfx._pool[i].stream = null


## DISTANCIA: som de longe soa de longe, e longe demais nem ocupa voz.
func _distancia(sfx: Node) -> void:
	var ouvinte := Node3D.new()
	ouvinte.add_to_group("player")
	root.add_child(ouvinte)
	sfx._player_ref = null
	_check(sfx._db_dist(Vector3(0, 0, 4.0)) == 0.0, "perto: volume cheio (0 dB)")
	var meio: Variant = sfx._db_dist(Vector3(0, 0, 60.0))
	_check(meio != null and float(meio) < -5.0, "longe: atenuado (%s dB)" % str(meio))
	_check(sfx._db_dist(Vector3(0, 0, 400.0)) == null,
		"longe demais: nem gera som (nao ocupa voz)")
	sfx._throttle.clear()
	_reset_vozes(sfx)
	_check(not sfx._play_at("hit", Vector3(0, 0, 400.0), 0.0),
		"evento fora do alcance NAO toca")

	## Com player em cena, a manopla e' lida do SLOT do pawn, nao da flag —
	## um bot trocando de arma emite weapon_equipped e limparia a flag.
	var sc := GDScript.new()
	sc.source_code = "extends Node\nvar arma_id := \"varinha\"\n"
	sc.reload()
	var slot: Node = sc.new()
	ouvinte.add_child(slot)
	sfx._slot_ref = null
	sfx._manopla = true  # flag mentindo de proposito
	_check(not sfx._tem_manopla(), "estalo le' a arma do PAWN, nao a flag global")
	slot.set("arma_id", "manopla")
	_check(sfx._tem_manopla(), "...e reconhece a manopla no punho do player")

	ouvinte.free()
	sfx._player_ref = null
	sfx._slot_ref = null
	sfx._manopla = false


## Espiao: emite o sinal e confere que a voz escolhida recebeu o stream
## esperado e chamou play() (headless: valida atribuicao, nao audicao).
## `_last` e' preenchido por Sfx._play; `_ui`/`_music` tem voz propria.
func _event(sfx: Node, bus: Node, sig: String, args: Array, expected: String,
		onde := "pool") -> void:
	sfx.set_meta("last_key", "")
	## O teste dispara ~50 eventos no MESMO milissegundo — no jogo eles estao
	## a segundos de distancia. Sem zerar throttle e vozes, a propria mixagem
	## (que tem teste proprio em _mixagem) recusaria o evento sob exame.
	sfx._throttle.clear()
	_reset_vozes(sfx)
	bus.callv("emit_signal", [sig] + args)
	var p: AudioStreamPlayer = sfx._last
	if onde == "ui":
		p = sfx._ui
	elif onde == "music":
		p = sfx._music
	elif onde == "evento":
		p = sfx._evento
	var ok: bool = sfx.get_meta("last_key") == expected \
		and p != null and p.stream == sfx._streams.get(expected) and p.playing
	_check(ok, "%s -> %s" % [sig, expected])


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)
