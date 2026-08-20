## Selftest headless da raia APRESENTACAO (R18). Rodar:
##   Godot --headless --path godot --script res://menu/selftest.gd
## Prova: a cena instancia; JOGAR/PERSONAGENS/SAIR existem com >=48dp;
## a vitrine lista os 20 magos (EM BREVE so nos 11-20); JOGAR emite
## Bus.game_start_requested SEM trocar de cena (scene_switch_enabled=false).
##
## NOTA (mesma do gameplay/selftest.gd): em modo --script os autoloads so'
## registram DEPOIS deste arquivo compilar — Bus chega via get_node no 1o frame.
extends SceneTree

var fails := 0
var _sinal := 0


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	var ps: PackedScene = load("res://menu/Menu.tscn")
	if ps == null:
		printerr("FALHOU — nao carregou res://menu/Menu.tscn")
		quit(1)
		return
	var menu: Control = ps.instantiate()
	root.add_child(menu)
	_check(menu is Control, "cena instancia com raiz Control")

	# --- 3 botoes, alvo >= 48dp (mesma conversao do jogo: ui/Dp.gd) ---
	var min48 := maxf(Dp.px(48.0), 48.0) - 0.5
	for nome in ["BtnJogar", "BtnPersonagens", "BtnSair"]:
		var b: Button = menu.find_child(nome, true, false)
		_check(b != null, "botao %s existe" % nome)
		if b != null:
			_check(b.custom_minimum_size.y >= min48,
				"%s >= 48dp (%.0fpx >= %.0fpx)" % [nome, b.custom_minimum_size.y, min48])

	# --- vitrine: 20 cards; EM BREVE marca so as temporadas (11-20) ---
	var cards := get_nodes_in_group("elenco_card")
	_check(cards.size() == 20, "vitrine lista 20 cards (achou %d)" % cards.size())
	var c01: Node = menu.find_child("Card01", true, false)
	var c11: Node = menu.find_child("Card11", true, false)
	var c20: Node = menu.find_child("Card20", true, false)
	_check(c01 != null and c01.find_child("EmBreve", true, false) == null,
		"card 01 (lancamento) SEM selo EM BREVE")
	_check(c11 != null and c11.find_child("EmBreve", true, false) != null,
		"card 11 (temporada) COM selo EM BREVE")
	_check(c20 != null and c20.find_child("EmBreve", true, false) != null,
		"card 20 (temporada) COM selo EM BREVE")

	# --- JOGAR emite o sinal do contrato (Bus.gd) ---
	var bus: Node = root.get_node_or_null("Bus")
	_check(bus != null, "autoload Bus presente")
	if bus != null:
		menu.scene_switch_enabled = false  # prova o sinal sem trocar de cena
		bus.game_start_requested.connect(func() -> void: _sinal += 1)
		var jogar: Button = menu.find_child("BtnJogar", true, false)
		if jogar != null:
			jogar.pressed.emit()
		_check(_sinal == 1, "JOGAR emite Bus.game_start_requested (1x)")

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
