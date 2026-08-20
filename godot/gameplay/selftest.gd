## Selftest headless da raia GAMEPLAY (R16, ampliado na R18/R19). Rodar:
##   Godot --headless --path godot --script res://gameplay/selftest.gd
## Prova: dano via Combat (reduz hp + sinal), NaN barrado, gesto R17, bot
## morre com hp 0, os 5 elementos com dano/custo/velocidade/cadencia/forma de
## Balance (terra a mais lenta, vento a maior cadencia), terrain_hit em TODO
## impacto (espiao no Bus), player_killed_bot na kill do player,
## dodge_performed so' na esquiva do player, esquiva da' i-frames (dano nao
## passa) e respeita cooldown, knockback decai, restart zera tudo (inclusive
## elemento e cooldown de esquiva) e o carrossel tem 5 slots >= 48dp.
##
## NOTA: em modo --script os autoloads so' registram DEPOIS que este arquivo
## compila — por isso aqui NADA e' referenciado lexicalmente (Bus, Balance,
## Combat...): tudo chega via load()/get_node() no 1o frame.
extends SceneTree

var fails := 0
var _dmg_events: Array = []
var _died: Array = []
var _el_events: Array = []
var _thits: Array = []
var _kills: Array = []
var _dodges := 0
var _bus: Node
var _bal: Node
var _combat: GDScript
var _fg: GDScript
var _bot_scr: GDScript


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	_bus = root.get_node("Bus")
	_bal = root.get_node("Balance")
	_combat = load("res://gameplay/Combat.gd")
	_fg = load("res://gameplay/FireGesture.gd")
	_bot_scr = load("res://gameplay/Bot.gd")
	_bus.damage_dealt.connect(func(t: Node, a: int, e: String) -> void:
		_dmg_events.append([t, a, e]))
	_bus.entity_died.connect(func(ent: Node) -> void: _died.append(ent))
	_bus.element_changed.connect(func(el: String) -> void: _el_events.append(el))
	_bus.terrain_hit.connect(func(el: String, pos: Vector3, strong: bool) -> void:
		_thits.append([el, pos, strong]))
	_bus.player_killed_bot.connect(func(bot_name: String) -> void: _kills.append(bot_name))
	_bus.dodge_performed.connect(func() -> void: _dodges += 1)
	_test_combat_damage()
	_test_combat_nan()
	_test_gesture()
	_test_bot_death()
	_test_elements()
	_test_bus_impacto()
	_test_dodge()
	_test_restart()
	if fails == 0:
		print("SELFTEST OK")
	else:
		printerr("SELFTEST FALHOU: %d" % fails)
	quit(0 if fails == 0 else 1)


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)


func _dummy(hp0: float) -> Node:
	var s := GDScript.new()
	s.source_code = "extends Node\nvar hp := 0.0"
	s.reload()
	var n: Node = s.new()
	n.hp = hp0
	root.add_child(n)
	return n


func _test_combat_damage() -> void:
	print("[Combat: dano]")
	var d := _dummy(50.0)
	_dmg_events.clear()
	_check(_combat.deal(d, 13.0, "fire"), "Combat.deal aplica dano")
	_check(is_equal_approx(float(d.hp), 37.0), "hp reduziu 50 -> 37")
	_check(_dmg_events.size() == 1 and _dmg_events[0][1] == 13, "Bus.damage_dealt emitiu 13")
	d.queue_free()


func _test_combat_nan() -> void:
	print("[Combat: guarda NaN]")
	var d := _dummy(50.0)
	_dmg_events.clear()
	_check(not _combat.deal(d, NAN), "NaN barrado")
	_check(not _combat.deal(d, -5.0), "negativo barrado")
	_check(not _combat.deal(d, 0.0), "zero barrado")
	_check(is_equal_approx(float(d.hp), 50.0), "hp intacto")
	_check(_dmg_events.is_empty(), "nenhum sinal emitido")
	d.queue_free()


func _test_gesture() -> void:
	print("[FireGesture: clicar dispara · segurar auto-fogo · arrastar mira (R17)]")
	var g: RefCounted = _fg.new()
	g.deadzone_px = 20.0
	g.repeat_ms = 270
	var res: Dictionary = _fg.Result
	var vis: Dictionary = _fg.Visual
	# clicar DISPARA na hora — o veredito do Diretor no aparelho
	_check(g.press(Vector2.ZERO, 0) == res.FIRE, "clicar dispara imediatamente")
	_check(g.visual(10) == vis.RING, "anel aceso enquanto o dedo esta' no botao")
	# segurar = um disparo por cadencia, nunca por frame
	_check(g.poll(100) == res.NONE, "antes da cadencia NAO repete")
	_check(g.poll(270) == res.FIRE, "na cadencia repete o disparo")
	_check(g.poll(300) == res.NONE, "um por cadencia, nao um por frame")
	_check(g.poll(540) == res.FIRE, "segue repetindo enquanto segura")
	# arrastar direciona SEM interromper o fogo
	g.drag(Vector2(80, 0))
	_check(g.has_aimed(), "arrasto alem da deadzone vira mira")
	_check(g.poll(810) == res.FIRE, "mirando continua disparando")
	# soltar para o fogo
	g.release(900)
	_check(g.poll(1200) == res.NONE, "soltou = parou")
	_check(g.visual(1200) == vis.NONE, "anel apaga ao soltar")
	# a MANA nao mora na maquina: quem barra e' o Player (autoridade de custo).
	# O gate disso e' o teste de mana do Player logo abaixo no fluxo da partida.


func _test_bot_death() -> void:
	print("[Bot: morte]")
	var b: Node = _bot_scr.new()
	root.add_child(b)
	_died.clear()
	_check(_combat.deal(b, 9999.0), "dano letal aplicado")
	_check(float(b.hp) == 0.0, "bot com hp 0")
	_check(_died.size() == 1 and _died[0] == b, "Bus.entity_died emitido")
	_check(not b.is_physics_processing(), "bot morto para de agir")
	_check(not _combat.deal(b, 10.0), "morto nao toma dano de novo")


func _test_elements() -> void:
	print("[Elementos: os 5 — dano/custo/velocidade/cadencia/forma de Balance]")
	var proj: GDScript = load("res://gameplay/Projectile.gd")
	var els: Array = _bal.ELEMENTS
	_check(els.size() == 5, "ELEMENTS tem os 5 (fire/water/lightning/earth/wind)")
	var mirror := {"fire": _bal.FIRE, "water": _bal.WATER, "lightning": _bal.LIGHTNING,
			"earth": _bal.EARTH, "wind": _bal.WIND}
	var ok_mirror := true
	var dmgs := {}
	var costs := {}
	var slowest := ""
	var fastest := ""
	var quickest := ""
	var min_sp := INF
	var max_sp := -INF
	var min_fr := INF
	for el in els:
		var s: Dictionary = proj.spec(str(el))
		if s != mirror[el]:
			ok_mirror = false
		dmgs[float(s.dmg)] = true
		costs[float(s.mana_cost)] = true
		if float(s.projectile_speed) < min_sp:
			min_sp = float(s.projectile_speed)
			slowest = str(el)
		if float(s.projectile_speed) > max_sp:
			max_sp = float(s.projectile_speed)
			fastest = str(el)
		if float(s.fire_rate) < min_fr:
			min_fr = float(s.fire_rate)
			quickest = str(el)
	_check(ok_mirror, "spec espelha Balance nos 5 (nada solto)")
	_check(dmgs.size() == 5, "dano difere nos 5")
	_check(costs.size() == 5, "custo de mana difere nos 5")
	_check(slowest == "earth", "terra e' a mais lenta e pesada (Balance.EARTH)")
	_check(fastest == "lightning", "raio segue o mais rapido (Balance.LIGHTNING)")
	_check(quickest == "wind", "vento tem a maior cadencia (menor fire_rate)")
	var shapes := {}
	for el in els:
		var p: Node = proj.launch(root, null, Vector3.ZERO, Vector3.FORWARD, str(el))
		shapes[str(p.get_meta("shape", "?"))] = true
		_check(is_equal_approx(float(p.dmg), float(proj.spec(str(el)).dmg)),
				str(el) + ": projetil carrega o dano do Balance")
		p.queue_free()
	_check(shapes.size() == 5, "FORMA distinta nos 5 (GDD §10)")


## A costura R19: gameplay EMITE, terreno reage. Impacto em QUALQUER corpo
## anuncia terrain_hit; kill do player anuncia player_killed_bot.
func _test_bus_impacto() -> void:
	print("[Bus: terrain_hit em todo impacto · player_killed_bot na kill]")
	var proj: GDScript = load("res://gameplay/Projectile.gd")
	# impacto em superficie do mundo (corpo estatico, sem hp): anuncia mesmo assim
	var wall := StaticBody3D.new()
	root.add_child(wall)
	_thits.clear()
	var p: Area3D = proj.launch(root, null, Vector3(1, 2, 3), Vector3.FORWARD, "earth")
	p._on_hit(wall)
	_check(_thits.size() == 1, "impacto no mundo emite terrain_hit")
	_check(_thits[0][0] == "earth", "terrain_hit carrega o elemento")
	_check((_thits[0][1] as Vector3).distance_to(Vector3(1, 2, 3)) < 0.01,
			"terrain_hit carrega a posicao do impacto")
	_check(_thits[0][2] == false, "strong=false por ora (taticas R20+)")
	# impacto em pawn TAMBEM anuncia + kill do player emite player_killed_bot
	var b: Node = _bot_scr.new()
	root.add_child(b)
	var shooter := Node3D.new()
	shooter.add_to_group("player")
	root.add_child(shooter)
	_thits.clear()
	_kills.clear()
	var p2: Area3D = proj.launch(root, shooter, Vector3.ZERO, Vector3.FORWARD, "fire")
	p2.dmg = 9999.0
	p2._on_hit(b)
	_check(_thits.size() == 1, "impacto em pawn tambem emite terrain_hit")
	_check(_kills == [str(b.name)], "kill do player emite player_killed_bot")
	# kill de bot em bot NAO e' kill do player
	var b2: Node = _bot_scr.new()
	root.add_child(b2)
	var bot_shooter: Node = _bot_scr.new()
	root.add_child(bot_shooter)
	_kills.clear()
	var p3: Area3D = proj.launch(root, bot_shooter, Vector3.ZERO, Vector3.FORWARD, "fire")
	p3.dmg = 9999.0
	p3._on_hit(b2)
	_check(_kills.is_empty(), "kill de bot por bot NAO emite player_killed_bot")
	wall.queue_free()
	shooter.queue_free()
	bot_shooter.queue_free()


func _test_dodge() -> void:
	print("[Esquiva: i-frames + cooldown + knockback]")
	var pawn_scr: GDScript = load("res://gameplay/Pawn.gd")
	var p: CharacterBody3D = pawn_scr.new()
	root.add_child(p)
	_check(p.dodge_ready(), "esquiva pronta no spawn")
	_check(p.try_dodge(Vector3.FORWARD), "esquiva dispara")
	_check(float(p.iframes_left) > 0.0, "i-frames ativos no inicio do dash")
	var hp0 := float(p.hp)
	_check(not _combat.deal(p, 10.0), "dano durante i-frame NAO passa")
	_check(is_equal_approx(float(p.hp), hp0), "hp intacto no i-frame")
	_check(not p.try_dodge(Vector3.FORWARD), "cooldown barra esquiva dupla")
	p.move_velocity(Vector3.ZERO, float(_bal.DODGE.iframes) + 0.05)  # relogio anda
	_check(_combat.deal(p, 10.0), "apos i-frames o dano volta a passar")
	_check(is_equal_approx(float(p.hp), hp0 - 10.0), "hp reduziu apos i-frames")
	p.move_velocity(Vector3.ZERO, float(_bal.DODGE.cooldown) + 0.1)
	_check(p.dodge_ready(), "cooldown expira e libera")
	p.apply_knockback(Vector3(3, 0, 0))
	p.move_velocity(Vector3.ZERO, 0.016)
	_check(float(p.velocity.x) > 0.0, "knockback entra na velocidade")
	p.move_velocity(Vector3.ZERO, 10.0)
	_check(is_zero_approx(float(p.velocity.x)), "knockback decai a zero sozinho")
	# dodge_performed: feedback global SO da esquiva do PLAYER
	var pl: CharacterBody3D = pawn_scr.new()
	root.add_child(pl)
	pl.add_to_group("player")
	_dodges = 0
	pl.try_dodge(Vector3.FORWARD)
	_check(_dodges == 1, "esquiva do player emite dodge_performed")
	var npc: CharacterBody3D = pawn_scr.new()
	root.add_child(npc)
	npc.try_dodge(Vector3.FORWARD)
	_check(_dodges == 1, "esquiva de bot NAO emite (feedback e' do player)")
	pl.queue_free()
	npc.queue_free()
	p.queue_free()


func _test_restart() -> void:
	print("[Main: restart limpo]")
	var main: Node3D = (load("res://gameplay/Main.gd") as GDScript).new()
	root.add_child(main)  # _ready monta ilha(fallback), HUD e a 1a partida
	var arena1: Node3D = main.arena
	var n_bots := int(_bal.MATCH.bots)
	_check(is_instance_valid(arena1), "partida 1 montada")
	_check(_count_bots(arena1) == n_bots, "%d bots na partida 1" % n_bots)
	# suja o estado: dano no player, mata um bot, gasta tempo e mana
	_combat.deal(main.player, 30.0)
	_combat.deal(_first_bot(arena1), 9999.0)
	main.player.mana = 1.0
	main.time_left = 3.0
	_check(main.bots_alive == n_bots - 1, "morte de bot contada")
	_check(float(main.player.hp) < float(_bal.PLAYER.hp), "player danificado (pre-condicao)")
	# suja tambem elemento e esquiva — estado que NAO pode atravessar partida
	_el_events.clear()
	main.player.set_element("lightning")
	_check(str(main.player.element) == "lightning", "troca de elemento aplica")
	_check(_el_events == ["lightning"], "Bus.element_changed emitido na troca")
	_check(int(main.hud.fire_btn.gesture.repeat_ms) == int(float(_bal.LIGHTNING.fire_rate) * 1000.0),
			"cadencia do disparo continuo segue o elemento")
	_check(str(main.hud.carousel.selected) == "lightning", "carrossel destaca o elemento")
	main.player.try_dodge(Vector3.FORWARD)
	_check(not main.player.dodge_ready(), "esquiva em cooldown (pre-condicao)")
	main._build_match()
	_check(arena1.is_queued_for_deletion(), "arena velha descartada")
	_check(main.arena != arena1 and is_instance_valid(main.arena), "arena nova montada")
	_check(_count_bots(main.arena) == n_bots, "bots de volta a %d" % n_bots)
	_check(main.bots_alive == n_bots, "contador de bots zerado")
	_check(is_equal_approx(float(main.player.hp), float(_bal.PLAYER.hp)), "hp do player cheio")
	_check(is_equal_approx(float(main.player.mana), float(_bal.PLAYER.mana_max)), "mana cheia")
	_check(is_equal_approx(float(main.time_left), float(_bal.MATCH.duration_s)), "timer resetado")
	_check(int(main.match_state) == int(main.PLAYING), "estado PLAYING")
	_check(str(main.player.element) == "fire", "restart volta ao elemento padrao")
	_check(main.player.dodge_ready(), "restart zera cooldown de esquiva")
	_check(str(main.hud.carousel.selected) == "fire", "carrossel resetado no restart")
	_check(int(main.hud.fire_btn.gesture.repeat_ms) == int(float(_bal.FIRE.fire_rate) * 1000.0),
			"cadencia de disparo resetada")
	# carrossel com 5 slots, cada um >= 48dp (anchors direita: largura = offsets)
	var dp: GDScript = load("res://ui/Dp.gd")
	var car: Control = main.hud.carousel
	var slot_w := (car.offset_right - car.offset_left) / float(_bal.ELEMENTS.size())
	_check(slot_w >= dp.px(48.0) - 0.01, "carrossel: 5 slots com largura >= 48dp")
	_check((car.offset_bottom - car.offset_top) >= dp.px(48.0) - 0.01,
			"carrossel: altura >= 48dp")
	main.queue_free()


func _count_bots(arena: Node3D) -> int:
	var n := 0
	for c in arena.get_children():
		if c.get_script() == _bot_scr:
			n += 1
	return n


func _first_bot(arena: Node3D) -> Node:
	for c in arena.get_children():
		if c.get_script() == _bot_scr:
			return c
	return null
