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

## O pedido de TREINO (decisao no 19 — DIRECAO.md §8). O menu escreve aqui e o
## _ready consome: static porque precisa sobreviver a troca de cena, consumida
## porque partida normal nenhuma pode herdar o treino por engano.
static var proximo_treino := false
var modo_treino := false


func _ready() -> void:
	modo_treino = proximo_treino
	proximo_treino = false
	island = _load_island()
	add_child(island)
	_collect_spawns()
	hud = Hud.new()
	add_child(hud)
	hud.restart_pressed.connect(_build_match)
	Bus.entity_died.connect(_on_entity_died)
	## O POUSO LIGA A TEMPESTADE (27/08). Conectado UMA vez, aqui, e nao a cada
	## partida: reconectar por partida acumularia N conexoes no mesmo sinal.
	Bus.queda_fase.connect(_ao_mudar_queda)
	## Costura R19: o juice observa o Bus — entra ANTES da 1a partida para
	## nao perder o match_started do boot. Defensivo: sem a raia, nada quebra.
	if ResourceLoader.exists("res://juice/MatchJuice.tscn"):
		add_child((load("res://juice/MatchJuice.tscn") as PackedScene).instantiate())
	_build_match()


func _process(delta: float) -> void:
	if match_state != PLAYING:
		return
	if modo_treino:
		return  # treino nao tem relogio: fica o tempo que quiser
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
	if modo_treino:
		bots_alive = 0  # bonecos de treino nao contam vitoria — nascem em _montar_treino
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
		# O BATISMO (2o video do Diretor, 26/08): o kill feed mostra body.name —
		# sem nome, o Godot autogera "@CharacterBody3D@N" e o interno vazava
		# na tela ("Voce derrubou @CharacterBody3D@1718").
		b.name = str(Kits.de(slug).nome) if slug != "" else "Bot %d" % (i + 1)
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
	## No TREINO nao ha' zona: ninguem aprende habilidade correndo da tempestade.
	##
	## Ela nasce INERTE (sem parede, sem dano, sem cronometro) e SO' LIGA quando
	## o jogador pousa — ordem do Diretor de 27/08: "mapa todo aberto ate' que
	## todos descam". Quem liga e' _ao_pousar(), fiado no Bus.queda_fase.
	## Sem a camada de queda o pouso nunca chega, entao a zona liga aqui mesmo
	## (senao a partida rodaria sem tempestade nenhuma).
	zona = null if modo_treino else Zona.criar(arena, island)
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
	if modo_treino:
		_montar_treino()
	elif ResourceLoader.exists(QUEDA_SCRIPT):
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
			else:
				_ligar_zona()   # a queda existe mas nao subiu: nao ficar sem zona
	elif zona != null:
		## SEM A CAMADA DE QUEDA a partida abre no chao e o sinal de pouso nunca
		## chega. Ligar aqui e' o que impede uma partida inteira sem tempestade.
		_ligar_zona()
	hud.bind_player(player)
	hud.hide_end()
	if modo_treino:
		hud.update_treino()   # o canto diz "TREINO", nao um 3:00 que nao corre
	else:
		hud.update_match(time_left, bots_alive)
	Bus.match_started.emit()


## Toda queda do ceu comeca com uma VARINHA (GDD §16.2). O slot e' um no' filho
## — Player.gd/Bot.gd/Pawn.gd nao sabem que ele existe (composicao); quem
## precisa da arma de alguem chama ArmaSlot.de(pawn).
## O LOBBY DE TREINO (DIRECAO.md §8): sem zona, sem relogio, sem queda — o
## jogador nasce no chao com as TRES luvas expostas a passos do spawn, dois
## bonecos que regeneram para apanhar, e a suprema enchendo em 5s em vez de
## 40-50 (testar suprema esperando 50s nao e' treino, e' fila).
## A TEMPESTADE COMECA A CONTAR NO CHAO, nao no ceu. Ordem do Diretor (27/08):
## "mapa todo aberto ate' que todos descam no mapa, contamos um cronometro de
## 1:10 para comecar o circulo se formar". Enquanto o mago cai, `zona.iniciar()`
## nao foi chamada: sem parede, sem dano, sem cronometro.
##
## E' a MESMA lei que a suprema ja' segue (KitRunner: `if Queda.no_ar(pawn):
## return`) — nada da partida conta antes do pe' no chao.
func _ao_mudar_queda(fase: String) -> void:
	if fase == "pousou":
		_ligar_zona()


## Idempotente pelos dois lados: `iniciar()` ignora chamada repetida, e aqui a
## zona pode nem existir (treino). Isso deixa as tres portas de entrada — pouso,
## queda ausente, queda que falhou — chamarem sem se coordenar.
func _ligar_zona() -> void:
	if zona != null and is_instance_valid(zona):
		zona.iniciar()


func _montar_treino() -> void:
	# as tres luvas, em fila, a 3-5m do jogador (a manopla com par fixo de
	# exemplo — no jogo real ela so' nasce no Bau Celestial)
	var base := player.global_position
	var fila := [["varinha", PackedStringArray(), "fire"],
			["cajado", PackedStringArray(), "lightning"],
			["manopla", PackedStringArray(["fire", "wind"]), "fire"]]
	for i in fila.size():
		var l: Loot = Loot.criar(str(fila[i][0]), fila[i][1], str(fila[i][2]))
		arena.add_child(l)
		var pos := base + Vector3(3.0 + float(i) * 1.6, 0.0, 2.5)
		pos.y = _altura(pos) + 0.4
		l.global_position = pos
	# dois bonecos: Pawn cru (nao age, nao persegue) que REGENERA — apanhar
	# sem culpa e' o servico deles. Grupo proprio para o selftest achar.
	for i in 2:
		var d := Pawn.new()
		d.name = "Boneco%d" % i
		arena.add_child(d)
		d.add_to_group("boneco_treino")
		var pos := base + Vector3(8.0 + float(i) * 3.0, 0.0, -2.0)
		pos.y = _altura(pos) + 1.0
		d.global_position = pos
		var t := Timer.new()
		t.wait_time = 1.0
		t.autostart = true
		d.add_child(t)
		t.timeout.connect(func() -> void:
			if is_instance_valid(d) and d.hp > 0.0:
				d.hp = minf(d.hp + 15.0, 100.0))
	# suprema em 5s: o dado e' por-instancia (Kits.de ja' duplica), entao isto
	# nao vaza para partidas reais.
	var k := KitRunner.de(player)
	if k != null:
		k.dados["suprema_carga"] = 5.0


func _altura(pos: Vector3) -> float:
	if island != null and island.has_method("height"):
		return float(island.call("height", pos.x, pos.z))
	return 0.0


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
	# O ultimo decremento precisa chegar a HUD AQUI: o _process para no OVER
	# e a vitoria congelava "BOTS 1" na tela (2o video do Diretor, 26/08).
	hud.update_match(time_left, bots_alive)
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
