## Selftest headless da raia JUICE (R19). Rodar:
##   Godot --headless --path godot --script res://juice/selftest.gd
## Prova: a camada instancia sozinha e cada sinal do Bus acende o overlay
## certo; flash NAO dispara com vida subindo; kill feed limita a 3; restart
## limpa tudo; e a partida (gameplay/Main) boota SEM a camada — nada depende
## dela.
##
## NOTA (padrao das outras raias): em --script os autoloads registram DEPOIS
## desta compilacao — nada de referencia lexical a Bus/Balance aqui.
extends SceneTree

var fails := 0


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	var bus: Node = root.get_node("Bus")
	var j: CanvasLayer = (load("res://juice/MatchJuice.tscn") as PackedScene).instantiate()
	root.add_child(j)

	print("[contagem de inicio]")
	bus.match_started.emit()
	_check(j._countdown_lbl.visible, "match_started acende a contagem")
	_check(str(j._countdown_lbl.text) == "3", "contagem abre no 3")

	print("[kill feed + marca +1]")
	for i in 5:
		bus.player_killed_bot.emit("Bot%d" % i)
	_check(j._feed.get_child_count() == 3, "kill feed limita a 3 linhas")
	_check("Bot2" in str((j._feed.get_child(0) as Label).text), "linha mais velha e' o 3o abate")
	_check("Bot4" in str((j._feed.get_child(2) as Label).text), "feed usa o nome que o sinal traz")
	_check("Voce derrubou" in str((j._feed.get_child(2) as Label).text), "texto do feed")
	_check(j._marks.get_child_count() == 5, "um +1 por abate")

	print("[flash de dano: so' com vida CAINDO]")
	bus.health_changed.emit(100.0, 100.0)  # primeira leitura: baseline
	_check(j._vignette.modulate.a == 0.0, "primeira leitura nao acende vinheta")
	bus.health_changed.emit(90.0, 100.0)   # queda de 10
	_check(j._vignette.modulate.a > 0.0, "vida caindo acende a vinheta")
	_check(is_zero_approx(j._trauma), "queda fraca (<=15) NAO gera trauma")
	j._vignette.modulate.a = 0.0
	bus.health_changed.emit(100.0, 100.0)  # vida SUBIU
	_check(j._vignette.modulate.a == 0.0, "vida SUBINDO nao acende vinheta")
	bus.health_changed.emit(NAN, 100.0)
	_check(j._vignette.modulate.a == 0.0, "NaN barrado na apresentacao")

	print("[screen shake: trauma so' com dano forte]")
	bus.health_changed.emit(100.0, 100.0)  # re-baseline pos-NaN
	bus.health_changed.emit(80.0, 100.0)   # queda de 20 > 15
	_check(j._trauma > 0.0, "dano forte (>15) gera trauma")
	_check(j._trauma <= 1.0, "trauma tem teto 1.0")
	_check(j.is_processing(), "_process liga so' enquanto ha' trauma")

	print("[sting de fim de partida]")
	bus.match_over.emit(true)
	_check(j._sting_gold.visible, "vitoria acende o veu dourado")
	_check(not j._sting_gray.visible, "vitoria NAO acende o cinza")
	bus.match_over.emit(false)
	_check(j._sting_gray.visible, "derrota acende o veu cinza")
	_check(not j._sting_gold.visible, "derrota NAO acende o dourado")

	print("[restart limpa tudo]")
	bus.match_started.emit()
	_check(j._feed.get_child_count() == 0, "restart limpa o kill feed")
	_check(j._marks.get_child_count() == 0, "restart limpa os +1")
	_check(not j._sting_gray.visible and not j._sting_gold.visible, "restart esconde stings")
	_check(is_zero_approx(j._trauma), "restart zera o trauma")
	_check(j._vignette.modulate.a == 0.0, "restart apaga a vinheta")
	_check(j._countdown_lbl.visible, "restart reabre a contagem")
	j.free()

	print("[camada AUSENTE: a partida boota sem juice/]")
	var main: Node3D = (load("res://gameplay/Main.gd") as GDScript).new()
	root.add_child(main)  # _ready monta ilha, HUD e a 1a partida — sem MatchJuice
	_check(is_instance_valid(main.arena), "gameplay/Main monta partida SEM a camada")
	_check(is_instance_valid(main.hud), "HUD vive sem a camada")
	main.queue_free()

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
