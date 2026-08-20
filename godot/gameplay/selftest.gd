## Selftest headless da raia GAMEPLAY (R16, ampliado na R18). Rodar:
##   Godot --headless --path godot --script res://gameplay/selftest.gd
## Prova: dano via Combat (reduz hp + sinal), NaN barrado, gesto R17, bot
## morre com hp 0, elementos mudam dano/custo/forma, esquiva da' i-frames
## (dano nao passa) e respeita cooldown, knockback decai, restart zera tudo
## (inclusive elemento e cooldown de esquiva).
##
## NOTA: em modo --script os autoloads so' registram DEPOIS que este arquivo
## compila — por isso aqui NADA e' referenciado lexicalmente (Bus, Balance,
## Combat...): tudo chega via load()/get_node() no 1o frame.
extends SceneTree

var fails := 0
var _dmg_events: Array = []
var _died: Array = []
var _el_events: Array = []
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
	_test_combat_damage()
	_test_combat_nan()
	_test_gesture()
	_test_bot_death()
	_test_elements()
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
	print("[Elementos: dano/custo/velocidade/forma por elemento]")
	var proj: GDScript = load("res://gameplay/Projectile.gd")
	var sf: Dictionary = proj.spec("fire")
	var sw: Dictionary = proj.spec("water")
	var sl: Dictionary = proj.spec("lightning")
	_check(sf == _bal.FIRE and sw == _bal.WATER and sl == _bal.LIGHTNING,
			"spec espelha Balance (nada solto)")
	_check(sf.dmg != sw.dmg and sw.dmg != sl.dmg and sf.dmg != sl.dmg,
			"dano difere por elemento")
	_check(sf.mana_cost != sw.mana_cost and sw.mana_cost != sl.mana_cost,
			"custo de mana difere por elemento")
	_check(float(sl.projectile_speed) > float(sf.projectile_speed)
			and float(sl.projectile_speed) > float(sw.projectile_speed),
			"raio e' o mais rapido (Balance.LIGHTNING)")
	var shapes := {}
	for el in ["fire", "water", "lightning"]:
		var p: Node = proj.launch(root, null, Vector3.ZERO, Vector3.FORWARD, el)
		shapes[el] = str(p.get_meta("shape", "?"))
		_check(is_equal_approx(float(p.dmg), float(proj.spec(el).dmg)),
				el + ": projetil carrega o dano do Balance")
		p.queue_free()
	_check(shapes["fire"] != shapes["water"] and shapes["water"] != shapes["lightning"]
			and shapes["fire"] != shapes["lightning"],
			"FORMA distinta por elemento (GDD §10)")


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
