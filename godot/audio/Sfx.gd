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
## R21 — MIXAGEM COM PRIORIDADE. Com 6 bots atirando, terreno queimando e o bau
## caindo, tudo ao mesmo tempo, o pool round-robin antigo roubava a voz do som
## que MAIS importava. Agora toda reproducao declara PRIORIDADE e um TETO DE
## REPETICAO (throttle em ms):
##   - som novo nunca rouba a voz de um som de prioridade MAIOR;
##   - som repetido dentro da janela do throttle simplesmente nao sai
##     (o terreno espirrava dezenas de eventos por segundo — o dano ja' foi
##      corrigido na raia gameplay, aqui e' o lado do som);
##   - o que TODO O MAPA precisa ouvir (telegrafia de suprema, estalo da
##     manopla, bau) e' P_CRITICA e NAO leva atenuacao por distancia.
##
## R21 — DISTANCIA. O projeto nao usa AudioStreamPlayer3D (a sintese vive em
## AudioStreamWAV mono e o mundo e' pequeno); o minimo viavel e' atenuar em dB
## pela distancia do PLAYER ate' o ponto do evento (_db_dist). Alem de SURDO_M
## o som nem e' gerado — e' o que impede 6 bots do outro lado da ilha de virar
## lama. Ver PEDIDOS no relatorio para o dia em que quisermos 3D de verdade.
extends Node

const SR := 22050
## 10 vozes: 1 player + 6 bots atirando ja' come 7 no pico; sobra p/ terreno,
## status e o que for critico. Mobile aguenta — sao WAVs prontos, nao sintese.
const POOL := 10

## PRIORIDADE (maior ganha a voz). Som novo NUNCA rouba voz de prioridade maior.
const P_AMBIENTE := 0   # cama sonora
const P_BAIXA := 1      # terreno, estados, ticks
const P_NORMAL := 2     # disparo, acerto
const P_ALTA := 3       # abate, escudo quebrado, arma equipada, pouso do bau
const P_CRITICA := 4    # telegrafia de suprema, estalo da manopla, bau

## Atenuacao por distancia: volume cheio ate' OUVIDO_M, -22 dB em SURDO_M,
## silencio total depois (nem ocupa voz).
const OUVIDO_M := 8.0
const SURDO_M := 110.0
const QUEDA_DB := 22.0

var _streams: Dictionary = {}
var _pool: Array[AudioStreamPlayer] = []
var _free_at: PackedInt64Array = PackedInt64Array()  # ms em que cada voz vaga
var _prio: PackedInt32Array = PackedInt32Array()
var _throttle: Dictionary = {}   # key -> ms do ultimo disparo
var _ui: AudioStreamPlayer       # bus Ui   (slider "Interface")
var _music: AudioStreamPlayer    # bus Music(slider "Musica")
var _evento: AudioStreamPlayer   # loop de evento longo (queda/canalizacao do bau)
var _wind: AudioStreamPlayer
var _birds: AudioStreamPlayer
var _bird_timer: Timer
var _connected := false
var _rng := RandomNumberGenerator.new()
var _last: AudioStreamPlayer     # espiao do selftest: ultima voz usada
var _player_ref: Node3D          # cache do player p/ distancia
var _manopla := false            # o player porta manopla? (estalo dos dedos)
var _tw: Tween                   # rampa de pitch do loop de evento
var _slot_ref: Node              # cache do slot de arma do player


func _ready() -> void:
	_rng.seed = 1337  # deterministico: mesmo timbre em todo aparelho
	_setup_buses()
	_setup_players()
	_synth_all()
	_connect_bus()
	set_meta("last_key", "")  # espiao p/ selftest headless (sem device de som)


# ---------------------------------------------------------------- buses
## Master ja' existe. Os NOMES sao contrato com menu/Config.gd (BUSES =
## {geral: Master, musica: Music, efeitos: Sfx, interface: Ui}) — os 4 sliders
## das Configuracoes so' funcionam se os 4 buses existirem, e quem os cria e'
## este autoload, que sobe ANTES da cena principal. Renomear aqui = slider morto.
## Ambient manda para Sfx (e nao para Master) de proposito: baixar "Efeitos"
## tem que baixar o vento tambem, senao o slider mente.
## Defaults conservadores — o Diretor testa em fone E em alto-falante de
## celular; melhor baixo que estourado.
func _setup_buses() -> void:
	for cfg: Array in [["Music", -6.0, "Master"], ["Sfx", -8.0, "Master"],
			["Ui", -10.0, "Master"], ["Ambient", -18.0, "Sfx"]]:
		if AudioServer.get_bus_index(cfg[0]) != -1:
			continue
		var i := AudioServer.bus_count
		AudioServer.add_bus(i)
		AudioServer.set_bus_name(i, cfg[0])
		AudioServer.set_bus_send(i, StringName(cfg[2]))
		AudioServer.set_bus_volume_db(i, cfg[1])


func _setup_players() -> void:
	for i in POOL:
		var p := AudioStreamPlayer.new()
		p.bus = &"Sfx"
		add_child(p)
		_pool.append(p)
	_free_at.resize(POOL)
	_prio.resize(POOL)
	_ui = _novo_player(&"Ui", 0.0)
	_music = _novo_player(&"Music", 0.0)
	## Voz DEDICADA aos loops de evento (queda e canalizacao do bau): fora do
	## pool porque nao pode ser roubada nem roubar — ela toca por segundos.
	_evento = _novo_player(&"Sfx", -6.0)
	_wind = _novo_player(&"Ambient", 0.0)
	_birds = _novo_player(&"Ambient", -6.0)
	_bird_timer = Timer.new()
	_bird_timer.one_shot = true
	_bird_timer.timeout.connect(_on_bird_timer)
	add_child(_bird_timer)


func _novo_player(bus: StringName, vol: float) -> AudioStreamPlayer:
	var p := AudioStreamPlayer.new()
	p.bus = bus
	p.volume_db = vol
	add_child(p)
	return p


# ---------------------------------------------------------------- fiacao (defensiva)
func _connect_bus() -> void:
	var bus := get_node_or_null("/root/Bus")
	if bus == null:
		push_warning("Sfx: Bus ausente — audio mudo, jogo segue.")
		return
	## MIGRACAO DO DANO (Bus.gd §damage_applied): o sinal novo carrega QUEM
	## causou e SE bateu em escudo — da o "clank" de escudo, separa o tique de
	## DoT (source == null) do acerto direto e traz o alvo para calcular
	## distancia. O legado `damage_dealt` foi REMOVIDO em 25/08/2026: so' era
	## conectado no ramo else, que nunca rodava porque `damage_applied` sempre
	## existiu. Sinal sem ouvinte de producao e' contrato morto.
	_hook(bus, "damage_applied", _on_damage_applied)
	_hook(bus, "entity_died", _on_entity_died)
	_hook(bus, "player_killed_bot", _on_player_killed_bot)
	_hook(bus, "dodge_performed", _on_dodge)
	_hook(bus, "element_changed", _on_element_changed)
	_hook(bus, "match_started", _on_match_started)
	_hook(bus, "match_over", _on_match_over)
	_hook(bus, "terrain_hit", _on_terrain_hit)
	_hook(bus, "terrain_changed", _on_terrain_changed)
	_hook(bus, "game_start_requested", _on_game_start_requested)
	_hook(bus, "spell_cast", _on_spell_cast)
	# --- escudo evolutivo (GDD §5) ---
	_hook(bus, "shield_broken", _on_shield_broken)
	# --- armas arcanas (GDD §16.2) ---
	_hook(bus, "loot_prompt", _on_loot_prompt)
	_hook(bus, "weapon_equipped", _on_weapon_equipped)
	# --- habilidades (GDD §4) ---
	_hook(bus, "kit_telegraph", _on_kit_telegraph)
	_hook(bus, "kit_cooldown", _on_kit_cooldown)
	_hook(bus, "kit_state", _on_kit_state)
	# --- bau celestial (GDD §16.2) ---
	_hook(bus, "bau_anunciado", _on_bau_anunciado)
	_hook(bus, "bau_pousou", _on_bau_pousou)
	_hook(bus, "bau_canalizando", _on_bau_canalizando)
	_hook(bus, "bau_aberto", _on_bau_aberto)
	## Estados elementais (queimadura/congelamento/atordoamento): o Bus ganhou
	## `status_aplicado` nesta raia, mas quem EMITE e o Pawn (raia gameplay) —
	## ver PEDIDOS no relatorio. Enquanto ninguem emitir, isto e no-op.
	_hook(bus, "status_aplicado", _on_status_aplicado)
	_connected = true


## Sinal pode nao existir ainda (Bus e' do coordenador) — conecta so' se houver.
func _hook(bus: Node, sig: String, fn: Callable) -> void:
	if bus.has_signal(sig) and not bus.is_connected(sig, fn):
		bus.connect(sig, fn)


# ---------------------------------------------------------------- reproducao
## Toda reproducao passa aqui. Stream nulo/faltando (sintese quebrada) =>
## silencio e retorno — o jogo NUNCA percebe.
##   prio        : quem pode roubar voz de quem (P_*).
##   throttle_ms : teto de repeticao DESTA key. 0 = sem teto.
## Devolve se soou (o selftest le' isso; o jogo ignora).
func _play(key: String, vol_db := 0.0, prio := P_NORMAL, throttle_ms := 0) -> bool:
	var stream: AudioStreamWAV = _streams.get(key)
	if stream == null:
		return false
	var agora := Time.get_ticks_msec()
	if throttle_ms > 0:
		var ultimo: int = _throttle.get(key, -999999999)
		if agora - ultimo < throttle_ms:
			return false
		_throttle[key] = agora
	var i := _voz(agora, prio)
	if i < 0:
		return false  # tudo ocupado por som MAIS importante: cala e segue
	var p := _pool[i]
	p.stream = stream
	p.volume_db = vol_db
	p.play()
	_free_at[i] = agora + int(stream.get_length() * 1000.0)
	_prio[i] = prio
	_last = p
	set_meta("last_key", key)
	return true


## Escolhe a voz: 1) qualquer uma ja' livre; 2) a de MENOR prioridade (empate:
## a que acaba primeiro). Recusa (-1) se a vitima for mais importante — e' esta
## linha que garante que a telegrafia da suprema nunca e' abafada por um tiro.
func _voz(agora: int, prio: int) -> int:
	var pior := -1
	for i in _pool.size():
		if _free_at[i] <= agora:
			return i
		if pior < 0 or _prio[i] < _prio[pior] \
				or (_prio[i] == _prio[pior] and _free_at[i] < _free_at[pior]):
			pior = i
	if pior < 0:
		return -1
	return pior if _prio[pior] <= prio else -1


## Mesma coisa, com ATENUACAO POR DISTANCIA do player ate' `pos`.
## Evento longe demais nao ocupa voz nenhuma.
func _play_at(key: String, pos: Vector3, vol_db := 0.0, prio := P_NORMAL,
		throttle_ms := 0) -> bool:
	var at: Variant = _db_dist(pos)
	if at == null:
		return false
	return _play(key, vol_db + float(at), prio, throttle_ms)


## dB a somar por distancia, ou null se e' longe demais para existir.
## Sem player em cena (menu, selftest) nao ha' ouvinte: 0 dB, toca cheio.
func _db_dist(pos: Vector3) -> Variant:
	var ouvinte := _ouvinte()
	if ouvinte == null:
		return 0.0
	var d := ouvinte.global_position.distance_to(pos)
	if d > SURDO_M:
		return null
	if d <= OUVIDO_M:
		return 0.0
	# curva log: -22 dB no limite audivel, suave perto, seca longe.
	return -QUEDA_DB * log(d / OUVIDO_M) / log(SURDO_M / OUVIDO_M)


func _ouvinte() -> Node3D:
	if is_instance_valid(_player_ref):
		return _player_ref
	var n := get_tree().get_first_node_in_group("player") if get_tree() != null else null
	_player_ref = n as Node3D
	return _player_ref


## Interface (bus Ui) e stings musicais (bus Music) tem voz propria: nao
## disputam com o combate e obedecem aos sliders certos das Configuracoes.
func _play_ui(key: String, vol_db := -6.0) -> void:
	var s: AudioStreamWAV = _streams.get(key)
	if s == null:
		return
	_ui.stream = s
	_ui.volume_db = vol_db
	_ui.play()
	set_meta("last_key", key)


func _play_music(key: String, vol_db := -2.0) -> void:
	var s: AudioStreamWAV = _streams.get(key)
	if s == null:
		return
	_music.stream = s
	_music.volume_db = vol_db
	_music.play()
	set_meta("last_key", key)


## Loop de evento longo (queda/canalizacao do bau). pitch = tensao subindo.
func _loop_evento(key: String, vol_db: float, pitch := 1.0) -> void:
	var s: AudioStreamWAV = _streams.get(key)
	if s == null:
		return
	if _evento.stream != s or not _evento.playing:
		_evento.stream = s
		_evento.play()
	_evento.volume_db = vol_db
	_evento.pitch_scale = pitch
	set_meta("last_key", key)


## API publica p/ o instante do disparo (gameplay pode chamar defensivamente,
## ou o futuro Bus.spell_cast chega aqui).
func play_shot(element: String, vol_db := -4.0) -> void:
	_play("shot_" + element, vol_db, P_NORMAL, 40)


# ---------------------------------------------------------------- handlers
## O ESTALO DA MANOPLA (GDD §16.2) — a assinatura sonora do jogo. Vem ANTES do
## timbre do elemento, sempre no mesmo timbre seco e agudo, para ser reconhecivel
## de longe e independente do elemento que sai: quem ouve o estalo sabe que tem
## manopla no mapa, e essa e' a informacao que aterroriza. P_CRITICA e sem
## throttle: numa rajada de manopla (~1 estalo a cada 0,22s) TODO estalo sai.
func _on_spell_cast(element: String) -> void:
	if _tem_manopla():
		_play("estalo_manopla", -1.0, P_CRITICA)
	play_shot(element)


## `weapon_equipped` nao diz QUEM equipou (ver PEDIDOS): um bot trocando de
## varinha limparia a flag do player. Entao a verdade e' lida do proprio pawn —
## qualquer filho que tenha `arma_id` serve, sem depender da classe ArmaSlot
## (audio nao acopla em gameplay). A flag fica so' como ultimo recurso, para
## contexto sem player em cena.
func _tem_manopla() -> bool:
	var p := _ouvinte()
	if p == null:
		return _manopla
	if not is_instance_valid(_slot_ref):
		_slot_ref = null
		for c in p.get_children():
			if "arma_id" in c:
				_slot_ref = c
				break
	if _slot_ref == null:
		return _manopla
	return str(_slot_ref.arma_id) == "manopla"


## Acerto COMPLETO (Bus.damage_applied). Tres leituras diferentes no ouvido:
##   escudo   -> "clank" metalico (bateu na casca, nao na pele)
##   DoT      -> tique de queimadura/terreno, quieto e throttled
##   direto   -> o "hit" seco de sempre
func _on_damage_applied(target: Node, _amount: float, element: String,
		source: Node, on_shield: bool) -> void:
	var pos := (target as Node3D).global_position if target is Node3D else Vector3.ZERO
	if source == null:
		## Tique de DoT: ja' vem em balde (Balance.DOT.tick) da raia gameplay,
		## mas 7 pawns queimando ao mesmo tempo ainda sao 7 sons — o throttle
		## corta para no maximo ~3/s no total.
		_play_at("st_queimar" if element == "fire" else "dot_terreno",
				pos, -13.0, P_BAIXA, 320)
		return
	if on_shield:
		_play_at("escudo_clank", pos, -7.0, P_NORMAL, 60)
	else:
		_play_at("hit", pos, -6.0, P_NORMAL, 60)


## "Derrubado" e' um BAQUE, nunca sofrimento (classificacao 10+).
func _on_entity_died(entity: Node) -> void:
	var pos := (entity as Node3D).global_position if entity is Node3D else Vector3.ZERO
	_play_at("baque", pos, -4.0, P_ALTA)


func _on_player_killed_bot(_bot_name: String) -> void:
	_play("kill", -3.0, P_ALTA)


func _on_dodge() -> void:
	_play("dodge", -8.0, P_NORMAL)


func _on_element_changed(element: String) -> void:
	if _streams.get("click_" + element) != null:
		_play_ui("click_" + element, -8.0)
	else:
		_play_ui("ui_tick", -10.0)


func _on_match_started() -> void:
	_manopla = false
	_player_ref = null  # cena nova, player novo
	_slot_ref = null
	_play_music("sting_start", -4.0)
	_ambient_start()


func _on_match_over(victory: bool) -> void:
	_ambient_stop()
	_play_music("fanfare" if victory else "defeat", -2.0)


func _on_game_start_requested() -> void:
	_play_ui("ui_tick", -10.0)


# ------------------------------------------------- escudo evolutivo (GDD §5)
## O escudo quebrando e' informacao TATICA: quem ouve sabe que o proximo tiro
## entra na vida. Vidro estilhacando, alto e curto, P_ALTA.
func _on_shield_broken(entity: Node) -> void:
	var pos := (entity as Node3D).global_position if entity is Node3D else Vector3.ZERO
	_play_at("escudo_quebra", pos, -2.0, P_ALTA)


# ------------------------------------------------- armas arcanas (GDD §16.2)
## Entrou no raio de um loot: blip discreto na Interface (o prompt e' da HUD).
func _on_loot_prompt(_nome: String, _raridade: String, perto: bool) -> void:
	if perto:
		_play_ui("loot_perto", -12.0)


## Equipou. A raridade sobe o timbre; a MANOPLA tem sonoridade propria (dourada)
## e ARMA o estalo dos dedos para os proximos disparos do player.
## ⚠️ `weapon_equipped` nao diz QUEM equipou (ver PEDIDOS): usamos a raridade,
## e o par de 2 elementos, como assinatura da manopla.
func _on_weapon_equipped(_pawn: Node, _arma_id: String, _nome: String, raridade: String,
		elementos: PackedStringArray) -> void:
	if raridade == "lendaria" or elementos.size() >= 2:
		_manopla = true
		_play("manopla_equipar", -1.0, P_CRITICA)
	else:
		_manopla = false
		# throttle: bots trocam de arma no chao; um encaixe por vez basta.
		_play("arma_equipar", -6.0 if raridade == "comum" else -4.0, P_ALTA, 250)


# ------------------------------------------------- habilidades (GDD §4)
## A LEI DO §4.3 NO AR: "se mata rapido, avisa antes". Este som e' a
## CONTRA-JOGADA, nao enfeite — P_CRITICA, sem throttle, sem atenuacao por
## distancia (suprema de longe tem que ser ouvida) e alto o bastante para
## atravessar 6 bots atirando. O rugido dura o que a telegrafia durar
## (Kits.TELEGRAFIA_MIN..MAX = 1..4s): esticamos por pitch_scale no player
## de evento, entao 1 buffer serve para toda a faixa.
func _on_kit_telegraph(_slug: String, _tipo: String, duracao: float,
		_pos := Vector3.ZERO) -> void:
	_play("telegrafia", 0.0, P_CRITICA)
	var d := clampf(duracao, 0.5, 6.0)
	_loop_evento("telegrafia_rugido", -3.0, clampf(1.6 / d, 0.35, 2.0))
	get_tree().create_timer(d).timeout.connect(_parar_evento)


## Borda de cooldown: restante > 0 = acabou de usar; 0 = ficou pronto.
## A suprema no instante do uso ja' tem a telegrafia — nao dobramos o som.
func _on_kit_cooldown(tipo: String, restante: float, _total: float) -> void:
	if restante <= 0.0:
		_play_ui("kit_pronto", -12.0)
	elif tipo == "tatica":
		_play("kit_tatica", -5.0, P_ALTA)


## Estados nomeados do kit. O sino da Veu "toca no mundo real" (habilidades/
## veu.gd) — e' o unico com timbre proprio; o resto e' um tique de liga/desliga,
## porque nome novo NAO pode exigir som novo (mesma regra da HUD).
func _on_kit_state(nome: String, ligado: bool) -> void:
	if nome == "sino_espectral":
		if ligado:
			_play("sino", -4.0, P_ALTA)
		return
	if nome == "escudo_quebrado":
		return  # ja' soa por shield_broken; dois sons seriam eco
	_play("estado_liga" if ligado else "estado_desliga", -14.0, P_BAIXA, 180)


# ------------------------------------------------- bau celestial (GDD §16.2)
## "Todo o mapa percebe" — SEM atenuacao por distancia, de proposito. O gongo
## anuncia, e o assobio da queda fica no ar ate' o pouso, subindo de tom.
func _on_bau_anunciado(_pos: Vector3, segundos: float) -> void:
	_play("bau_anuncio", 0.0, P_CRITICA)
	_loop_evento("bau_queda", -8.0, 0.8)
	_tw = get_tree().create_tween()
	_tw.tween_property(_evento, "pitch_scale", 1.7, maxf(segundos, 0.1))


func _on_bau_pousou(_pos: Vector3) -> void:
	_parar_evento()
	_play("bau_pouso", -1.0, P_CRITICA)


## Chega a CADA frame de fisica enquanto o player canaliza (BauCelestial._process)
## — por isso NAO gera stream nova: so' mexe no pitch de um loop que ja' toca.
func _on_bau_canalizando(_pawn: Node, progresso: float) -> void:
	if progresso <= 0.0:
		_parar_evento()
		return
	_loop_evento("bau_canal", -8.0, 0.9 + 0.55 * clampf(progresso, 0.0, 1.0))


func _on_bau_aberto(por_player: bool, _elementos: PackedStringArray) -> void:
	_parar_evento()
	_play("bau_aberto", 0.0, P_CRITICA)
	if por_player:
		_manopla = true  # o bau e' a UNICA porta da manopla (GDD §16.2)


func _parar_evento() -> void:
	if _tw != null and _tw.is_valid():
		_tw.kill()  # senao a rampa de pitch da queda continua mexendo no loop
	_evento.stop()
	_evento.pitch_scale = 1.0


# ------------------------------------------------- estados elementais
## queimando / molhado / congelado / atordoado — um timbre por estado, curto e
## quieto: e' leitura de estado, nao evento. (Sinal ainda sem emissor: PEDIDOS.)
func _on_status_aplicado(alvo: Node, nome: String) -> void:
	var pos := (alvo as Node3D).global_position if alvo is Node3D else Vector3.ZERO
	var k := "st_" + nome
	if _streams.get(k) == null:
		return  # estado novo sem timbre: silencio, nunca timbre errado
	_play_at(k, pos, -9.0, P_BAIXA, 200)


## Impacto elemental no terreno = a identidade sonora do disparo hoje.
## Agora ATENUADO: 6 bots acertando o outro lado da ilha nao entra na mixagem.
func _on_terrain_hit(element: String, pos: Vector3, strong: bool) -> void:
	_play_at("shot_" + element, pos, -3.0 if strong else -6.0, P_NORMAL, 40)


## Os nomes de `kind` sao da raia gameplay/mundo — casamos por substring p/
## nao quebrar com renome. Desconhecido = silencio (melhor que timbre errado).
## THROTTLE DE 150ms POR TIMBRE: o alastramento do fogo cospe uma celula por
## vez e virava serra eletrica; agora sai um crepitar legivel por rajada.
func _on_terrain_changed(kind: String, pos: Vector3) -> void:
	var k := kind.to_lower()
	if k.contains("fire") or k.contains("fogo") or k.contains("burn"):
		_play_at("terrain_fire", pos, -4.0, P_BAIXA, 150)
	elif k.contains("ice") or k.contains("gelo") or k.contains("frost"):
		_play_at("terrain_ice", pos, -4.0, P_BAIXA, 150)
	elif k.contains("zap") or k.contains("elec") or k.contains("shock") or k.contains("raio"):
		_play_at("terrain_zap", pos, -6.0, P_BAIXA, 150)
	elif k.contains("wall") or k.contains("muro") or k.contains("stone") \
			or k.contains("rock") or k.contains("earth") or k.contains("terra"):
		_play_at("terrain_wall", pos, -4.0, P_BAIXA, 150)


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
	_parar_evento()  # partida acabou com bau no ar? o loop nao pode sobreviver


func _on_bird_timer() -> void:
	var chirp: AudioStream = _streams.get("bird")
	if chirp != null:
		_birds.stream = chirp
		_birds.play()
	_bird_timer.start(_rng.randf_range(6.0, 16.0))  # esparso, entardecer


# ================================================================ SINTESE
## ~24s de PCM total a 22050 Hz (48 sons, ~1,0 MB) — gerado uma vez no boot e
## guardado em _streams. Custo por frame depois disso = ZERO: em partida so'
## atribuimos streams prontas a players.
## ponytail: MEDIDO em 309 ms de CPU no desktop (~1s num celular mediano), tudo
## dentro do _ready do autoload, ou seja, antes do menu pintar. Se o Diretor
## sentir o boot travar, o botao e' sintetizar aqui so' o subconjunto de UI e
## chamar o resto por call_deferred no primeiro frame ocioso do menu — a paleta
## incompleta ja' degrada em silencio (_play engole key faltando).
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
	# ---- R21: mecanicas novas ----
	_streams["estalo_manopla"] = _wav(_gen_estalo())
	_streams["manopla_equipar"] = _wav(_gen_manopla_equipar())
	_streams["arma_equipar"] = _wav(_gen_arma_equipar())
	_streams["loot_perto"] = _wav(_gen_loot_perto())
	_streams["telegrafia"] = _wav(_gen_telegrafia())
	_streams["telegrafia_rugido"] = _wav(_gen_rugido(), true)
	_streams["kit_tatica"] = _wav(_gen_kit_tatica())
	_streams["kit_pronto"] = _wav(_gen_kit_pronto())
	_streams["sino"] = _wav(_gen_sino())
	_streams["estado_liga"] = _wav(_gen_click(880.0))
	_streams["estado_desliga"] = _wav(_gen_click(587.0))
	_streams["bau_anuncio"] = _wav(_gen_bau_anuncio())
	_streams["bau_queda"] = _wav(_gen_bau_queda(), true)
	_streams["bau_pouso"] = _wav(_gen_bau_pouso())
	_streams["bau_canal"] = _wav(_gen_bau_canal(), true)
	_streams["bau_aberto"] = _wav(_gen_bau_aberto())
	_streams["escudo_clank"] = _wav(_gen_escudo_clank())
	_streams["escudo_quebra"] = _wav(_gen_escudo_quebra())
	_streams["baque"] = _wav(_gen_baque())
	_streams["dot_terreno"] = _wav(_gen_dot_terreno())
	# estados elementais: a key e' "st_" + nome do estado (Pawn.estado()).
	_streams["st_queimar"] = _wav(_gen_st_queimar())
	_streams["st_burn"] = _streams["st_queimar"]
	_streams["st_wet"] = _wav(_gen_st_wet())
	_streams["st_frost"] = _wav(_gen_st_frost())
	_streams["st_stun"] = _wav(_gen_st_stun())


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


# ================================================================ R21: SINTESE
# Timbres das mecanicas que entraram hoje. Mesma regra de sempre: PCM gerado
# uma vez no boot, zero arquivo de audio no repo.

## Costura de loop: recebe n+fade amostras e mistura a cauda no comeco, para o
## loop nao estalar na emenda. Mesma tecnica do vento ambiente.
func _loop_seam(raw: PackedFloat32Array, n: int, fade: int) -> PackedFloat32Array:
	var b := raw.slice(0, n)
	for i in fade:
		var a := float(i) / float(fade)
		b[i] = b[i] * a + raw[n + i] * (1.0 - a)
	return b


# ---- A ASSINATURA DO JOGO: o estalar de dedos da manopla (GDD §16.2) ----
## Estalo SECO: 8ms de estouro de banda larga (o "crack" da pele), um pop de
## corpo brevissimo e uma cauda dourada de dois harmonicos que sustenta ~0,2s.
## E a cauda que faz o estalo VIAJAR: o transiente da o susto, o brilho diz
## "manopla" mesmo a distancia. Timbre FIXO, independente do elemento — quem
## ouve nao precisa ver para saber o que tem no mapa.
func _gen_estalo() -> PackedFloat32Array:
	var b := _buf(0.30)
	_add_noise(b, 0.0, 0.008, 9000.0, 4000.0, 1.0, "decay")   # o crack
	_add_tone(b, 0.0, 0.025, 2100.0, 850.0, 0.55)             # corpo do dedo
	_add_tone(b, 0.006, 0.24, 2093.0, 2093.0, 0.20, 0.30)     # cauda dourada
	_add_tone(b, 0.006, 0.20, 3136.0, 3136.0, 0.12)           # brilho (5a)
	return b


func _gen_manopla_equipar() -> PackedFloat32Array:  # fecho metalico + acorde de ouro
	var b := _buf(1.05)
	_add_noise(b, 0.0, 0.010, 9000.0, 4000.0, 0.9, "decay")   # o estalo, de novo
	_add_tone(b, 0.0, 0.09, 420.0, 260.0, 0.5, 0.5)           # a luva fechando
	for n: Array in [[0.10, 523.0], [0.22, 659.0], [0.34, 784.0], [0.46, 1047.0]]:
		_add_tone(b, n[0], 0.55, n[1], n[1], 0.26, 0.35)
	_add_noise(b, 0.10, 0.85, 5200.0, 2600.0, 0.10, "hump")   # po de ouro
	return b


func _gen_arma_equipar() -> PackedFloat32Array:  # "chunk" de encaixe
	var b := _buf(0.38)
	_add_noise(b, 0.0, 0.05, 2400.0, 700.0, 0.55, "decay")
	_add_tone(b, 0.0, 0.10, 300.0, 170.0, 0.6)
	_add_tone(b, 0.05, 0.28, 660.0, 655.0, 0.20, 0.3)  # anel curto do cabo
	return b


func _gen_loot_perto() -> PackedFloat32Array:  # blip discreto de prompt
	var b := _buf(0.14)
	_add_tone(b, 0.0, 0.06, 880.0, 880.0, 0.35)
	_add_tone(b, 0.05, 0.08, 1174.0, 1174.0, 0.30)
	return b


# ---- A LEI DO §4.3: toda suprema AVISA antes ----
## O aviso e feito para NAO poder ser confundido com nada: um par de tons
## dissonantes (tritono La/Mib) — o intervalo de alarme — sobre um metal grave
## que DESCE, e uma varredura de ruido que SOBE por cima. Dissonancia + duas
## direcoes ao mesmo tempo = o ouvido nao ignora. Fica no topo da mixagem por
## PRIORIDADE, nao por volume bruto.
func _gen_telegrafia() -> PackedFloat32Array:
	var b := _buf(1.15)
	_add_tone(b, 0.0, 1.10, 165.0, 96.0, 0.60, 0.85)   # metal grave descendo
	_add_tone(b, 0.02, 0.95, 440.0, 415.0, 0.30, 0.5)  # La
	_add_tone(b, 0.02, 0.95, 622.0, 587.0, 0.30, 0.5)  # Mib — o tritono
	_add_noise(b, 0.0, 1.10, 400.0, 4200.0, 0.35, "rise")
	return b


## Rugido sustentado que preenche a janela de telegrafia (1..4s). Loop de 1,6s
## esticado por pitch_scale — 1 buffer cobre a faixa inteira do Kits.
func _gen_rugido() -> PackedFloat32Array:
	var n := int(1.6 * SR)
	var fade := int(0.25 * SR)
	var raw := PackedFloat32Array()
	raw.resize(n + fade)
	var lp := 0.0
	var ph := 0.0
	for i in raw.size():
		var t := float(i) / SR
		var k := minf(1.0, TAU * 320.0 / SR)
		lp += k * ((_rng.randf() * 2.0 - 1.0) - lp)
		ph += TAU * 82.0 / SR
		var trem := 0.75 + 0.25 * sin(TAU * 6.0 * t)  # o pulsar da ameaca
		raw[i] = (lp * 0.75 + sin(ph) * 0.35) * trem
	return _loop_seam(raw, n, fade)


func _gen_kit_tatica() -> PackedFloat32Array:  # conjuracao rapida
	var b := _buf(0.36)
	_add_noise(b, 0.0, 0.22, 600.0, 3000.0, 0.40, "hump")
	_add_tone(b, 0.02, 0.30, 392.0, 587.0, 0.28, 0.35)
	return b


func _gen_kit_pronto() -> PackedFloat32Array:  # "voltou": duas notas subindo
	var b := _buf(0.22)
	_add_tone(b, 0.0, 0.08, 784.0, 784.0, 0.30)
	_add_tone(b, 0.07, 0.14, 1047.0, 1047.0, 0.28, 0.25)
	return b


## O sino espectral da Veu — "toca no mundo real". Parciais INARMONICAS (nao
## sao multiplos inteiros): e o que faz um sino soar oco e assombrado em vez
## de virar orgao.
func _gen_sino() -> PackedFloat32Array:
	var b := _buf(1.00)
	for p: Array in [[440.0, 0.30], [1056.0, 0.18], [1496.0, 0.12],
			[2024.0, 0.08], [2860.0, 0.05]]:
		_add_tone(b, 0.0, 0.98, p[0], p[0] * 0.995, p[1])
	_add_noise(b, 0.0, 0.02, 7000.0, 3000.0, 0.35, "decay")  # a batida do badalo
	return b


# ---- BAU CELESTIAL: evento que o mapa INTEIRO ouve (GDD §16.2) ----
## Gongo grave de parciais inarmonicas + um coro de quintas por cima. Toca sem
## atenuacao por distancia de proposito: o anuncio E a informacao.
func _gen_bau_anuncio() -> PackedFloat32Array:
	var b := _buf(1.70)
	_add_noise(b, 0.0, 0.03, 6000.0, 1500.0, 0.5, "decay")   # a batida
	for p: Array in [[65.0, 0.55], [98.0, 0.30], [147.0, 0.22],
			[233.0, 0.14], [311.0, 0.10]]:
		_add_tone(b, 0.0, 1.65, p[0], p[0] * 0.998, p[1], 0.2)
	_add_tone(b, 0.25, 1.30, 392.0, 392.0, 0.14, 0.3)        # coro
	_add_tone(b, 0.25, 1.30, 588.0, 588.0, 0.11)
	return b


## Assobio da queda: loop de 1,2s cujo PITCH sobe (tween 0,8 -> 1,7 nos 6s da
## QUEDA_S). Objeto pesado se aproximando — a rampa faz o trabalho, o buffer e
## barato.
func _gen_bau_queda() -> PackedFloat32Array:
	var n := int(1.2 * SR)
	var fade := int(0.2 * SR)
	var raw := PackedFloat32Array()
	raw.resize(n + fade)
	var lp := 0.0
	var ph := 0.0
	for i in raw.size():
		var t := float(i) / SR
		var k := minf(1.0, TAU * 1400.0 / SR)
		lp += k * ((_rng.randf() * 2.0 - 1.0) - lp)
		ph += TAU * (520.0 + 40.0 * sin(TAU * 3.0 * t)) / SR  # vibrato do assobio
		raw[i] = lp * 0.35 + sin(ph) * 0.40
	return _loop_seam(raw, n, fade)


func _gen_bau_pouso() -> PackedFloat32Array:  # baque pesado + brilho magico
	var b := _buf(1.00)
	_add_tone(b, 0.0, 0.45, 62.0, 24.0, 1.0)                 # o impacto
	_add_noise(b, 0.0, 0.28, 1800.0, 300.0, 0.65, "decay")   # terra e cascalho
	_add_noise(b, 0.06, 0.90, 4800.0, 2200.0, 0.14, "hump")  # o feixe acendendo
	_add_tone(b, 0.10, 0.70, 784.0, 784.0, 0.14, 0.3)
	return b


## Zumbido da canalizacao (3s). Loop curto; o pitch sobe com o PROGRESSO, entao
## a barra da HUD e o ouvido contam a mesma historia sem custo por frame.
func _gen_bau_canal() -> PackedFloat32Array:
	var n := int(1.0 * SR)
	var fade := int(0.15 * SR)
	var raw := PackedFloat32Array()
	raw.resize(n + fade)
	var p1 := 0.0
	var p2 := 0.0
	for i in raw.size():
		var t := float(i) / SR
		p1 += TAU * 160.0 / SR
		p2 += TAU * 240.0 / SR
		var trem := 0.7 + 0.3 * sin(TAU * 7.0 * t)
		raw[i] = (sin(p1) * 0.45 + sin(p2) * 0.28 + sin(p1 * 4.0) * 0.06) * trem
	return _loop_seam(raw, n, fade)


func _gen_bau_aberto() -> PackedFloat32Array:  # acorde maior + explosao dourada
	var b := _buf(1.30)
	_add_noise(b, 0.0, 0.35, 900.0, 5000.0, 0.55, "decay")
	for n: Array in [[523.0, 0.26], [659.0, 0.24], [784.0, 0.24], [1047.0, 0.20]]:
		_add_tone(b, 0.0, 1.25, n[0], n[0], n[1], 0.35)
	_add_noise(b, 0.05, 1.10, 6000.0, 3000.0, 0.10, "hump")
	return b


# ---- escudo evolutivo (GDD §5) ----
func _gen_escudo_clank() -> PackedFloat32Array:  # bateu na CASCA, nao na pele
	var b := _buf(0.16)
	_add_noise(b, 0.0, 0.03, 5000.0, 2000.0, 0.45, "decay")
	_add_tone(b, 0.0, 0.14, 920.0, 900.0, 0.35, 0.5)
	_add_tone(b, 0.0, 0.10, 1380.0, 1360.0, 0.20)  # 5a = metal
	return b


## O escudo QUEBRANDO e informacao tatica: o proximo tiro entra na vida.
## Estilhaco de vidro — estouro brilhante que desce + 4 cacos agudos.
func _gen_escudo_quebra() -> PackedFloat32Array:
	var b := _buf(0.60)
	_add_noise(b, 0.0, 0.30, 9000.0, 1800.0, 0.85, "decay")
	_add_tone(b, 0.0, 0.08, 700.0, 300.0, 0.45)  # a casca cedendo
	var f := [2400.0, 3100.0, 1900.0, 2750.0]
	for i in 4:
		_add_tone(b, 0.04 + i * 0.06, 0.16, f[i], f[i] * 0.7, 0.16)
	return b


# ---- estados e derrubada ----
## "Derrubado" e um BAQUE, nao sofrimento (classificacao 10+): corpo no chao,
## sem voz nenhuma.
func _gen_baque() -> PackedFloat32Array:
	var b := _buf(0.40)
	_add_tone(b, 0.0, 0.28, 88.0, 33.0, 0.85)
	_add_noise(b, 0.0, 0.16, 600.0, 180.0, 0.45, "decay")
	return b


func _gen_dot_terreno() -> PackedFloat32Array:  # tique surdo do chao machucando
	var b := _buf(0.16)
	_add_noise(b, 0.0, 0.16, 520.0, 190.0, 0.40, "decay")
	return b


func _gen_st_queimar() -> PackedFloat32Array:  # brasa curta
	var b := _buf(0.22)
	var energy := 0.0
	for i in b.size():
		if _rng.randf() < 0.006:
			energy = _rng.randf_range(0.5, 1.0)
		b[i] = (_rng.randf() * 2.0 - 1.0) * energy * 0.55 * (1.0 - float(i) / float(b.size()))
		energy *= 0.985
	return b


func _gen_st_wet() -> PackedFloat32Array:  # encharcou
	var b := _buf(0.28)
	_add_noise(b, 0.0, 0.20, 3000.0, 600.0, 0.45, "hump")
	_add_tone(b, 0.0, 0.16, 520.0, 190.0, 0.30, 0.2)
	return b


func _gen_st_frost() -> PackedFloat32Array:  # cristalizando (sobe e trava)
	var b := _buf(0.42)
	_add_noise(b, 0.0, 0.30, 1200.0, 3600.0, 0.35, "rise")
	_add_tone(b, 0.10, 0.32, 1760.0, 1866.0, 0.20, 0.3)
	_add_tone(b, 0.16, 0.26, 2637.0, 2700.0, 0.12)
	return b


## Atordoado: tom BAMBO. A oscilacao de ~11 Hz e o que o ouvido le como
## "perdi o controle" — sem grito, sem agonia.
func _gen_st_stun() -> PackedFloat32Array:
	var b := _buf(0.48)
	var ph := 0.0
	for i in b.size():
		var t := float(i) / SR
		var u := float(i) / float(b.size())
		ph += TAU * lerpf(400.0, 250.0, u) / SR
		var am := 0.55 + 0.45 * sin(TAU * 11.0 * t)
		b[i] = sin(ph) * 0.35 * am * pow(1.0 - u, 1.2)
	return b
