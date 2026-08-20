## Selftest headless da raia GAMEPLAY (R16). Rodar:
##   Godot --headless --path godot --script res://gameplay/selftest.gd
## Prova: dano via Combat (reduz hp + sinal), NaN barrado, cancelamento do
## gesto NAO dispara (maquina pura), bot morre com hp 0, restart zera tudo.
##
## NOTA: em modo --script os autoloads so' registram DEPOIS que este arquivo
## compila — por isso aqui NADA e' referenciado lexicalmente (Bus, Balance,
## Combat...): tudo chega via load()/get_node() no 1o frame.
extends SceneTree

var fails := 0
var _dmg_events: Array = []
var _died: Array = []
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
	_test_combat_damage()
	_test_combat_nan()
	_test_gesture()
	_test_bot_death()
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
	print("[FireGesture: maquina pura do gesto unico]")
	var g: RefCounted = _fg.new()
	g.deadzone_px = 20.0
	g.tap_max_ms = 220
	var res: Dictionary = _fg.Result
	var vis: Dictionary = _fg.Visual
	# toque curto dispara na direcao da camera
	g.press(Vector2.ZERO, 0)
	_check(g.release(100) == res.TAP, "tap curto = TAP (dispara)")
	# arrastar para fora e soltar = FIRE, mostrando ANEL
	g.press(Vector2.ZERO, 0)
	g.drag(Vector2(80, 0))
	_check(g.visual(50) == vis.RING, "fora da deadzone mostra ANEL")
	_check(g.release(600) == res.FIRE, "soltar fora = FIRE")
	# voltar ao centro = CANCEL, mostrando X — e NAO dispara
	g.press(Vector2.ZERO, 0)
	g.drag(Vector2(80, 0))
	g.drag(Vector2(4, 0))
	_check(g.visual(600) == vis.CROSS, "de volta ao centro mostra X")
	_check(g.release(600) == res.CANCEL, "cancelamento NAO dispara")
	# segurar parado alem do tap tambem cancela
	g.press(Vector2.ZERO, 0)
	_check(g.release(500) == res.CANCEL, "segurar parado cancela")


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
	main._build_match()
	_check(arena1.is_queued_for_deletion(), "arena velha descartada")
	_check(main.arena != arena1 and is_instance_valid(main.arena), "arena nova montada")
	_check(_count_bots(main.arena) == n_bots, "bots de volta a %d" % n_bots)
	_check(main.bots_alive == n_bots, "contador de bots zerado")
	_check(is_equal_approx(float(main.player.hp), float(_bal.PLAYER.hp)), "hp do player cheio")
	_check(is_equal_approx(float(main.player.mana), float(_bal.PLAYER.mana_max)), "mana cheia")
	_check(is_equal_approx(float(main.time_left), float(_bal.MATCH.duration_s)), "timer resetado")
	_check(int(main.match_state) == int(main.PLAYING), "estado PLAYING")
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
