## CENA PRINCIPAL — a partida jogavel de 3 min (G1: jogo, nao sandbox).
## COMO A PARTIDA ACABA (revisto ao entrar a Zona): o fim de verdade e' ULTIMO
## EM PE — o player morre (derrota) ou os 6 bots morrem (vitoria). O cronometro
## de Balance.MATCH.duration_s (180s) deixou de ser "o fim" e virou REDE DE
## SEGURANCA: a zona fecha o ultimo circulo aos 165s com 18 dps em quem estiver
## fora, entao em condicoes normais alguem cai antes dos 180s. Se ninguem cair,
## o timeout continua encerrando como derrota. Nao mexi na regra do timeout de
## proposito: mudar o veredito do tempo esgotado e' decisao de Diretor, nao de
## raia (registrado no relatorio).
## Fiacao defensiva: Island/Mage carregam com fallback; esta cena BOOTA
## sozinha. Tudo por-partida vive sob "Arena" — restart = arena nova, limpa
## (estado que atravessa partida ja' vazou 3x no projeto).
extends Node3D

const ISLAND_SCENE := "res://world/Island.tscn"
## A QUEDA — a abertura de battle royale. Carregada por CAMINHO e nao por
## preload: se a raia da queda nao existir, a partida abre no chao como antes.
const QUEDA_SCRIPT := "res://gameplay/queda/Queda.gd"
const PLAYER_MAGE := "01-pyra"
# Magos ja' cadastrados em characters/mage_identity.gd (o script nao tem
# class_name; carrega igual ao Mage.gd). Cresce sozinho conforme a raia
# PERSONAGEM preenche as fichas — nada aqui muda.
const MAGE_IDS := preload("res://characters/mage_identity.gd")

const BOT_TINTS: Array[Color] = [
	Color(0.9, 0.3, 0.3), Color(0.3, 0.7, 0.9), Color(0.4, 0.85, 0.4),
	Color(0.95, 0.8, 0.3), Color(0.75, 0.4, 0.9), Color(0.95, 0.55, 0.25),
]

enum { PLAYING, OVER }

var match_state := PLAYING
var time_left: float = float(Balance.MATCH.duration_s)
var bots_alive := 0
var arena: Node3D
var island: Node3D
var player: Player
var zona: Zona
var hud: Hud
var _spawns: Array[Vector3] = []


func _ready() -> void:
	island = _load_island()
	add_child(island)
	_collect_spawns()
	hud = Hud.new()
	add_child(hud)
	hud.restart_pressed.connect(_build_match)
	Bus.entity_died.connect(_on_entity_died)
	## Costura R19: o juice observa o Bus — entra ANTES da 1a partida para
	## nao perder o match_started do boot. Defensivo: sem a raia, nada quebra.
	if ResourceLoader.exists("res://juice/MatchJuice.tscn"):
		add_child((load("res://juice/MatchJuice.tscn") as PackedScene).instantiate())
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
	player.set_mage(PLAYER_MAGE)
	_dar_arma(player)
	for i in bots_alive:
		var b := Bot.new()
		# SEM b.target = player (26/08): a partida e' FFA e o alvo nasce da
		# PERCEPCAO do bot (Bot._percebe) — visto, ouvido, disparo, revide.
		# Cravar o player aqui era o defeito "mesmo sem me mexer eles me notam".
		arena.add_child(b)
		_dar_arma(b)
		b.global_position = pts[i + 1]
		# Identidade de mago manda; tint so' entra em quem ainda nao tem ficha
		# no registro (senao sobrescreveria a paleta — raia PERSONAGEM, R20).
		var fichas: Array = MAGE_IDS.slugs().filter(func(s: String) -> bool: return s != PLAYER_MAGE)
		var slug: String = str(fichas[i % fichas.size()]) if not fichas.is_empty() else ""
		if slug != "":
			b.set_mage(slug)
		else:
			b.set_tint(BOT_TINTS[i % BOT_TINTS.size()])
	## LOOT DA PARTIDA (GDD §16.2). Nasce sob a Arena: restart = loot novo e
	## intacto, sem estado atravessando partida. Deterministico (seed fixo).
	Loot.espalhar(arena, island)
	## BAU CELESTIAL (GDD §16.2) — o evento que abre a UNICA porta da manopla.
	## Tambem sob a Arena: o Timer dele para junto no fim da partida e some no
	## restart (nenhum bau de partida velha caindo na partida nova).
	BauCelestial.agendar(arena, island)
	## A TEMPESTADE ARCANA — o circulo que fecha. Sem ela a partida acabava so'
	## por cronometro: um deathmatch com timer, nao um battle royale. Tambem
	## nasce sob a Arena (os dois Timers dela param no fim da partida e somem no
	## restart) e e' DETERMINISTICA pelo proprio seed — mesma sequencia de
	## circulos toda partida, como manda o contrato do projeto.
	zona = Zona.criar(arena, island)
	## A QUEDA DO CASTELO — a partida comeca no ar, nao no chao. O castelo cruza
	## o mapa, o jogador salta quando quiser, cai, plana e pousa. Ordem do
	## Diretor: durante a queda o mago NAO tem poder nenhum, so' o corpo.
	##
	## Vem DEPOIS de tudo (loot, bau, zona) de proposito: quando o jogador pousa,
	## o mundo ja' esta' montado e ele cai num mapa vivo, nao num vazio que se
	## preenche embaixo dele. Nasce sob a Arena, entao o restart limpa junto.
	##
	## Fiacao defensiva: sem o arquivo, nada acontece e a partida abre no chao.
	## Os BOTS continuam nascendo no chao — faze-los cair e' raia de IA e mexeria
	## em Bot.gd; esta' registrado como pendencia, nao como esquecimento.
	if ResourceLoader.exists(QUEDA_SCRIPT):
		var qs: GDScript = load(QUEDA_SCRIPT)
		if qs != null:
			var q = qs.iniciar(arena, island, player)
			## OS BOTS CAEM JUNTO, pela mesma lei: passo de fisica desligado ate'
			## pousar, entao nenhum deles conjura no ar. Compartilham o castelo
			## do jogador — ha' UM castelo no ceu, nao sete.
			if q != null:
				var lista: Array = []
				for n in arena.get_children():
					if n is Bot:
						lista.append(n)
				qs.iniciar_bots(lista, island, q.castelo)
	hud.bind_player(player)
	hud.hide_end()
	hud.update_match(time_left, bots_alive)
	Bus.match_started.emit()


## Toda queda do ceu comeca com uma VARINHA (GDD §16.2). O slot e' um no' filho
## — Player.gd/Bot.gd/Pawn.gd nao sabem que ele existe (composicao); quem
## precisa da arma de alguem chama ArmaSlot.de(pawn).
## Da' o SLOT — nunca mais a arma (26/08, DIRECAO.md §1: todos caem de maos
## nuas; achar a primeira luva e' a corrida de abertura da partida).
func _dar_arma(pawn: Pawn) -> void:
	var slot := ArmaSlot.new()
	slot.name = "ArmaSlot"
	# Decisao no 5 do Diretor: o PLAYER equipa apertando PEGAR — nada de troca
	# automatica por pisar em cima ("trocar a luva boa sem querer e' roubo").
	# O bot mantem o auto: o "apertar" dele e' implicito.
	slot.auto_upgrade = pawn is Bot
	pawn.add_child(slot)


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
