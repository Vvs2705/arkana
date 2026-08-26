## Selftest headless da raia GAMEPLAY (R16, ampliado na R18/R19). Rodar:
##   Godot --headless --path godot --script res://gameplay/selftest.gd
## Prova: dano via Combat (reduz hp + sinal), NaN barrado, gesto R17, bot
## morre com hp 0, os 5 elementos com dano/custo/velocidade/cadencia/forma de
## Balance (terra a mais lenta, vento a maior cadencia), terrain_hit em TODO
## impacto (espiao no Bus), player_killed_bot na kill do player,
## dodge_performed so' na esquiva do player, esquiva da' i-frames (dano nao
## passa) e respeita cooldown, knockback decai, restart zera tudo (inclusive
## elemento e cooldown de esquiva) e o carrossel tem 5 slots >= 48dp.
## R21 (ARMAS ARCANAS, GDD 16.2): o registro de armas tem todos os campos, a
## varinha e' a linha de base, o cajado troca cadencia por alcance/dano, a
## manopla porta 2 elementos e conjura mais rapido, o loot nasce em quantidade
## e posicao DETERMINISTICAS (mesmo seed = mesmo mapa), manopla NAO nasce no
## chao (so' Bau Celestial) e equipar troca de verdade os atributos do ataque.
##
## NOTA: em modo --script os autoloads so' registram DEPOIS que este arquivo
## compila — por isso aqui NADA e' referenciado lexicalmente (Bus, Balance,
## Combat...): tudo chega via load()/get_node() no 1o frame.
extends SceneTree

var fails := 0
var _dmg_events: Array = []
var _dmg_full: Array = []
var _shields: Array = []
var _breaks: Array = []
var _died: Array = []
var _el_events: Array = []
var _thits: Array = []
var _kills: Array = []
var _dodges := 0
var _armados: Array = []
var _prompts: Array = []
var _bau_anuncios: Array = []
var _bau_pousos: Array = []
var _bau_prog: Array = []
var _bau_abertos: Array = []
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
	_bus.damage_applied.connect(func(t: Node, a: float, e: String, _s: Node, _x: bool) -> void:
		_dmg_events.append([t, a, e]))
	_bus.damage_applied.connect(func(t: Node, a: float, e: String, src: Node, esc: bool) -> void:
		_dmg_full.append([t, a, e, src, esc]))
	_bus.shield_changed.connect(func(ent: Node, sh: float, mx: float, lv: int) -> void:
		_shields.append([ent, sh, mx, lv]))
	_bus.shield_broken.connect(func(ent: Node) -> void: _breaks.append(ent))
	_bus.entity_died.connect(func(ent: Node) -> void: _died.append(ent))
	_bus.element_changed.connect(func(el: String) -> void: _el_events.append(el))
	_bus.terrain_hit.connect(func(el: String, pos: Vector3, strong: bool) -> void:
		_thits.append([el, pos, strong]))
	_bus.player_killed_bot.connect(func(bot_name: String) -> void: _kills.append(bot_name))
	_bus.dodge_performed.connect(func() -> void: _dodges += 1)
	_bus.weapon_equipped.connect(func(_pawn: Node, id: String, nome: String, rar: String,
			els: PackedStringArray) -> void: _armados.append([id, nome, rar, els]))
	_bus.loot_prompt.connect(func(nome: String, rar: String, perto: bool) -> void:
		_prompts.append([nome, rar, perto]))
	_bus.bau_anunciado.connect(func(pos: Vector3, segs: float) -> void:
		_bau_anuncios.append([pos, segs]))
	_bus.bau_pousou.connect(func(pos: Vector3) -> void: _bau_pousos.append(pos))
	_bus.bau_canalizando.connect(func(_pawn: Node, pr: float) -> void: _bau_prog.append(pr))
	_bus.bau_aberto.connect(func(por_player: bool, els: PackedStringArray) -> void:
		_bau_abertos.append([por_player, els]))
	_test_combat_damage()
	_test_combat_nan()
	_test_gesture()
	_test_bot_death()
	_test_bot_percepcao()
	_test_elements()
	_test_escudo()
	_test_evolucao()
	_test_elementos_no_alvo()
	_test_dot()
	_test_bus_impacto()
	_test_dodge()
	_test_armas()
	_test_loot()
	_test_bau()
	_test_dono_dos_sinais()
	_test_game_feel()
	_test_restart()
	_test_sinal_sem_zero()  # roda por ULTIMO: audita todos os eventos da sessao
	if fails == 0:
		print("SELFTEST OK")
	else:
		printerr("SELFTEST FALHOU: %d" % fails)
	quit(0 if fails == 0 else 1)


var _silent_fails := 0


func _check_silent(cond: bool, _name: String) -> void:
	if not cond:
		_silent_fails += 1


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)


## QUEM emitiu? `weapon_equipped` e `bau_canalizando` tinham consumidor mas nao
## diziam de quem eram. A HUD contornava comparando o arma_id com o slot do
## jogador: se um BOT equipasse a MESMA arma, o icone do jogador mudava. Sinal
## ambiguo obriga cada consumidor a adivinhar, e cada um adivinha diferente.
func _test_dono_dos_sinais() -> void:
	print("[Bus: quem emitiu? arma e bau carregam o dono]")
	var assinaturas := {}
	for sig in _bus.get_signal_list():
		var nomes: Array = []
		for arg in sig["args"]:
			nomes.append(String(arg["name"]))
		assinaturas[String(sig["name"])] = nomes
	for nome in ["weapon_equipped", "bau_canalizando"]:
		var args: Array = assinaturas.get(nome, [])
		_check(args.size() > 0 and String(args[0]) == "pawn",
			"%s abre com `pawn`: o sinal diz de QUEM e' (%s)" % [nome, str(args)])

	# O emissor real: o slot vive como filho do pawn, entao quem equipou e' o pai.
	var armados: Array = []
	_bus.weapon_equipped.connect(func(pawn: Node, id: String, _n: String, _r: String,
			_e: PackedStringArray) -> void: armados.append([pawn, id]))
	var dono := Node3D.new()
	root.add_child(dono)
	var slot: Node = load("res://gameplay/ArmaSlot.gd").new()
	dono.add_child(slot)
	slot.equipar("varinha")
	_check(armados.size() == 1 and armados[0][0] == dono,
		"weapon_equipped carrega o pawn que equipou, nao so' o id da arma")
	dono.queue_free()


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
	# Migrado do extinto `damage_dealt` em 25/08/2026. O que a checagem cobre nao
	# mudou: o VALOR do dano tem que chegar ao barramento. Mudou o sinal que o
	# leva — e o novo carrega float, entao 13.0 e nao 13.
	_check(_dmg_events.size() == 1 and is_equal_approx(float(_dmg_events[0][1]), 13.0),
		"Bus.damage_applied levou 13 ao barramento")
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


## PERCEPCAO E FFA (26/08 — DIRECAO.md §7). O defeito reintroduzivel: Main
## cravava b.target = player no nascimento e os 6 bots sabiam onde o jogador
## estava desde o 1o quadro, parado ou nao ("mesmo sem me mexer eles ja' me
## notam" — o Diretor). Agora o alvo NASCE nulo e so' aparece pelos sentidos.
## Provado em vermelho desligando _percebe e cravando o alvo de volta.
func _test_bot_percepcao() -> void:
	print("[Bot: percepcao — visto, ouvido, disparo, revide, FFA]")
	var _pawn_scr: GDScript = load("res://gameplay/Pawn.gd")
	var caçador: Node = _bot_scr.new()
	root.add_child(caçador)
	caçador.global_position = Vector3.ZERO
	caçador.set_physics_process(false)  # a varredura e' chamada na mao

	# nasce CEGO: ninguem lhe deu alvo
	_check(caçador.target == null, "bot nasce SEM alvo (nada de b.target = player)")

	# PARADO a 15 m: fora da visao (12) e sem passos — NAO e' notado.
	var quieto: Node = _pawn_scr.new()
	root.add_child(quieto)
	quieto.global_position = Vector3(15, 0, 0)
	quieto.velocity = Vector3.ZERO
	caçador._percebe()
	_check(caçador.target == null,
			"parado a 15m NAO e' notado — ficar imovel esconde (a contra-jogada)")

	# EM MOVIMENTO a 15 m: os passos denunciam (audicao 18).
	quieto.velocity = Vector3(4, 0, 0)
	caçador._percebe()
	_check(caçador.target == quieto, "andando a 15m os PASSOS entregam")

	# alvo morre -> esquece e volta a vagar
	quieto.hp = 0.0
	caçador._percebe()
	_check(caçador.target == null, "alvo morto e' esquecido")

	# VISTO: mesmo parado, a 8 m nao ha' como nao ver.
	var perto: Node = _pawn_scr.new()
	root.add_child(perto)
	perto.global_position = Vector3(8, 0, 0)
	perto.velocity = Vector3.ZERO
	caçador._percebe()
	_check(caçador.target == perto, "parado a 8m e' VISTO (dentro dos 12m)")

	# FFA: outro BOT tambem e' presa — nao existe "so' o player".
	caçador.target = null
	perto.global_position = Vector3(100, 0, 0)
	var rival: Node = _bot_scr.new()
	root.add_child(rival)
	rival.set_physics_process(false)
	rival.global_position = Vector3(6, 0, 0)
	caçador._percebe()
	_check(caçador.target == rival, "FFA: bot caça bot — todos contra todos")

	# DISPARO: conjurar a 25 m (alem da visao e dos passos) entrega a posicao.
	caçador.target = null
	rival.global_position = Vector3(100, 0, 0)
	var atirador: Node = _pawn_scr.new()
	root.add_child(atirador)
	atirador.global_position = Vector3(25, 0, 0)
	_bus.disparo.emit(atirador, atirador.global_position)
	_check(caçador.target == atirador, "conjurar DENUNCIA a 25m (Bus.disparo)")

	# REVIDE: tomar dano ensina QUEM bateu, mesmo fora de toda audicao.
	caçador.target = null
	var sniper: Node = _pawn_scr.new()
	root.add_child(sniper)
	sniper.global_position = Vector3(60, 0, 0)
	_bus.damage_applied.emit(caçador, 12.0, "fire", sniper, false)
	_check(caçador.target == sniper, "tomar dano ensina quem atacou (revide)")

	# o proprio tiro do bot NAO o assusta (disparo proprio ignorado)
	caçador.target = null
	_bus.disparo.emit(caçador, caçador.global_position)
	_check(caçador.target == null, "o proprio disparo nao vira alvo")

	for n in [caçador, quieto, perto, rival, atirador, sniper]:
		n.queue_free()


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


## ESCUDO DE MAGIA EVOLUTIVO (GDD 5). Ate' 21/08 o sistema nao existia em
## codigo: Balance nao tinha entrada, Pawn nao tinha campo e Combat ia direto no
## hp. Quatro kits do elenco (Corvomante, Tessa, Vitalis, Brok) dependiam de um
## sistema que nao estava la'.
func _test_escudo() -> void:
	print("[Escudo evolutivo: absorve antes da vida, transborda e nao perde dano]")
	var pawn_scr: GDScript = load("res://gameplay/Pawn.gd")
	var n1 := float(_bal.ESCUDO.niveis[0])
	var p: CharacterBody3D = pawn_scr.new()
	root.add_child(p)
	_check(is_equal_approx(float(p.shield), n1) and int(p.shield_level) == 1,
			"todo mago nasce com escudo N1 cheio (%.0f)" % n1)

	# --- o escudo come primeiro; a vida nao e' tocada enquanto ele durar
	_shields.clear()
	_breaks.clear()
	_dmg_full.clear()
	_check(is_equal_approx(_combat.deal(p, 20.0, "fire"), 20.0), "deal devolve o dano efetivo")
	_check(is_equal_approx(float(p.shield), n1 - 20.0), "escudo absorveu 20")
	_check(is_equal_approx(float(p.hp), float(_bal.PLAYER.hp)), "a VIDA nao foi tocada")
	_check(_shields.size() == 1 and is_equal_approx(float(_shields[0][1]), n1 - 20.0),
			"Bus.shield_changed avisa a HUD")
	_check(_breaks.is_empty(), "escudo em pe' nao anuncia quebra")
	_check(_dmg_full.size() == 1 and bool(_dmg_full[0][4]), "damage_applied marca on_shield")

	# --- TRANSBORDO: o excedente do MESMO tiro passa para a vida, sem perder nada
	var antes := float(p.shield) + float(p.hp)
	_check(is_equal_approx(_combat.deal(p, 40.0, "fire"), 40.0), "tiro que estoura o escudo")
	_check(is_zero_approx(float(p.shield)), "escudo zerou")
	_check(is_equal_approx(float(p.hp), float(_bal.PLAYER.hp) - 10.0),
			"os 10 que sobraram TRANSBORDARAM para a vida")
	_check(is_equal_approx(antes - (float(p.shield) + float(p.hp)), 40.0),
			"nenhum ponto de dano se perdeu na fronteira escudo/vida")
	_check(_breaks.size() == 1 and _breaks[0] == p, "Bus.shield_broken emitido na quebra")

	# --- os multiplicadores esc/vida sao o que da' papel diferente aos elementos
	var r: CharacterBody3D = pawn_scr.new()
	root.add_child(r)
	_combat.deal(r, 20.0, "lightning")
	_check(is_equal_approx(float(r.shield), n1 - 20.0 * float(_bal.LIGHTNING.esc)),
			"RAIO e' o abre-escudo: 1.25x em escudo (comeu %.1f)" % (20.0 * float(_bal.LIGHTNING.esc)))
	var w: CharacterBody3D = pawn_scr.new()
	root.add_child(w)
	_combat.deal(w, 20.0, "wind")
	_check(is_equal_approx(float(w.shield), n1 - 20.0 * float(_bal.WIND.esc)),
			"VENTO e' o pior contra escudo: 0.75x (comeu %.1f)" % (20.0 * float(_bal.WIND.esc)))
	var e: CharacterBody3D = pawn_scr.new()
	root.add_child(e)
	e.shield = 0.0
	_combat.deal(e, 20.0, "earth")
	_check(is_equal_approx(float(e.hp), float(_bal.PLAYER.hp) - 20.0 * float(_bal.EARTH.vida)),
			"TERRA e' o finalizador: 1.15x em vida")

	# transbordo com esc != 1.0: a sobra volta a dano CRU antes de virar vida
	var t: CharacterBody3D = pawn_scr.new()
	root.add_child(t)
	t.shield = 25.0
	_combat.deal(t, 50.0, "lightning")   # pede 62.5, come 25, sobram 30 crus
	_check(is_zero_approx(float(t.shield)) \
			and is_equal_approx(float(t.hp), float(_bal.PLAYER.hp) - 30.0),
			"transbordo converte a sobra de volta ao dano cru (esc 1.25)")
	for n: Node in [p, r, w, e, t]:
		n.queue_free()


## A PROGRESSAO DENTRO DA PARTIDA (GDD 5): o escudo evolui com o dano CAUSADO.
## Sem isso o mago do minuto 3 e' identico ao do minuto 0 e o BR vira deathmatch.
func _test_evolucao() -> void:
	print("[Escudo evolutivo: evolui com dano causado + anti-farm completo]")
	var pawn_scr: GDScript = load("res://gameplay/Pawn.gd")
	var niveis: Array = _bal.ESCUDO.niveis
	var passos: Array = _bal.ESCUDO.evoluir
	var atacante: CharacterBody3D = pawn_scr.new()
	root.add_child(atacante)
	var alvo: CharacterBody3D = pawn_scr.new()
	root.add_child(alvo)
	alvo.hp = 5000.0
	atacante.shield = 10.0  # sujo de proposito: evoluir NAO pode curar o que faltava

	_combat.deal(alvo, float(passos[1]) - 1.0, "fire", atacante)
	_check(int(atacante.shield_level) == 1, "1 de dano abaixo do passo: continua N1")
	_combat.deal(alvo, 1.0, "fire", atacante)
	_check(int(atacante.shield_level) == 2, "%.0f de dano causado sobe para N2" % float(passos[1]))
	_check(is_equal_approx(float(atacante.shield), 10.0 + float(niveis[1]) - float(niveis[0])),
			"subir de nivel entrega SO' a capacidade nova (nao regenera o que faltava)")
	_combat.deal(alvo, float(passos[3]) * 2.0, "fire", atacante)
	_check(int(atacante.shield_level) == 4, "dano de sobra nao passa do N4 (teto)")

	# --- ANTI-FARM (GDD 5 + DANO.md 3.9)
	var solo: CharacterBody3D = pawn_scr.new()
	root.add_child(solo)
	solo.hp = 5000.0
	_combat.deal(solo, 300.0, "fire", solo)
	_check(is_zero_approx(float(solo.dmg_dealt)), "dano em SI MESMO nao credita")
	var a1: CharacterBody3D = pawn_scr.new()
	var a2: CharacterBody3D = pawn_scr.new()
	root.add_child(a1)
	root.add_child(a2)
	a1.add_to_group("player")
	a2.add_to_group("player")
	a2.hp = 5000.0
	_combat.deal(a2, 300.0, "fire", a1)
	_check(is_zero_approx(float(a1.dmg_dealt)), "dano em ALIADO nao credita (dupla no canto)")
	var estrutura := _dummy(5000.0)   # tem hp, NAO tem shield_level: nao e' mago
	var b1: CharacterBody3D = pawn_scr.new()
	root.add_child(b1)
	_combat.deal(estrutura, 300.0, "fire", b1)
	_check(is_zero_approx(float(b1.dmg_dealt)),
			"dano em MURO/torreta/totem/casulo nao credita (checagem estrutural)")
	for n: Node in [atacante, alvo, solo, a1, a2, b1, estrutura]:
		n.queue_free()


## O ELEMENTO DEIXA DE SER COSMETICO (GDD 4.6/16.4 — DANO.md 3.3/3.4).
## O teste antigo (_test_elements) prova que os cinco DIFEREM; este prova que a
## diferenca IMPORTA para quem leva o tiro.
func _test_elementos_no_alvo() -> void:
	print("[Elementos no ALVO: efeito secundario + as reacoes elementais]")
	var pawn_scr: GDScript = load("res://gameplay/Pawn.gd")
	var ef: GDScript = load("res://gameplay/Efeitos.gd")
	var st: Dictionary = _bal.STATUS

	# --- FOGO queima
	var a: CharacterBody3D = pawn_scr.new()
	root.add_child(a)
	ef.aplicar(a, "fire", 10.0, null)
	_check(is_equal_approx(float(a.burn_dps), float(st.burn_dps)) \
			and is_equal_approx(float(a.burn_left), float(st.burn_dur)), "FOGO acende o alvo")
	# --- AGUA apaga (extincao) e molha
	ef.aplicar(a, "water", 10.0, null)
	_check(is_zero_approx(float(a.burn_left)) and float(a.wet_left) > 0.0,
			"AGUA em quem queima: EXTINCAO (apaga e molha)")
	_check(is_equal_approx(float(a.status_mult), float(st.wet_slow)), "molhado lentifica 10%")
	# --- AGUA em quem ja' esta molhado: HIPOTERMIA (lentidao dobrada)
	ef.aplicar(a, "water", 10.0, null)
	_check(is_equal_approx(float(a.status_mult), float(st.hypothermia_slow)),
			"AGUA em molhado: HIPOTERMIA (lentidao dobrada)")
	# --- RAIO em molhado: CONDUCAO (+50% e atordoamento curto)
	var m: float = ef.aplicar(a, "lightning", 10.0, null)
	_check(is_equal_approx(m, float(st.conduct_mult)), "RAIO em molhado: CONDUCAO +50% no impacto")
	_check(is_equal_approx(float(a.stun_left), float(st.conduct_stun)), "conducao atordoa 0.4s")
	# --- FOGO em molhado: VAPOR (nao acende, seca)
	var v: CharacterBody3D = pawn_scr.new()
	root.add_child(v)
	ef.aplicar(v, "water", 10.0, null)
	ef.aplicar(v, "fire", 10.0, null)
	_check(is_zero_approx(float(v.burn_left)) and is_zero_approx(float(v.wet_left)),
			"FOGO em molhado: VAPOR (nao acende e o molhado some)")
	# --- VENTO em quem queima: ATICAR (triangulo do fogo)
	var f: CharacterBody3D = pawn_scr.new()
	root.add_child(f)
	ef.aplicar(f, "fire", 10.0, null)
	var dur0 := float(f.burn_left)
	ef.aplicar(f, "wind", 10.0, null)
	_check(is_equal_approx(float(f.burn_dps), float(st.burn_fanned_dps)) \
			and is_equal_approx(float(f.burn_left), dur0 + float(st.burn_fanned_bonus)),
			"VENTO em quem queima: ATICAR (4 -> 7 dps, +2s)")
	# --- RAIO em alvo SECO nao conduz
	var seco: CharacterBody3D = pawn_scr.new()
	root.add_child(seco)
	_check(is_equal_approx(float(ef.aplicar(seco, "lightning", 10.0, null)), 1.0),
			"RAIO em alvo seco: sem conducao")
	_check(is_zero_approx(float(seco.stun_left)), "e sem atordoamento")
	# --- i-frame e' imunidade TOTAL: nem dano nem ESTADO passam
	seco.try_dodge(Vector3.FORWARD)
	ef.aplicar(seco, "fire", 10.0, null)
	_check(is_zero_approx(float(seco.burn_left)), "esquiva com i-frames tambem barra o ESTADO")

	# --- EXCLUSIVIDADE por categoria: nunca queimando E molhado ao mesmo tempo
	var x: CharacterBody3D = pawn_scr.new()
	root.add_child(x)
	var exclusivo := true
	for el: String in ["fire", "water", "fire", "wind", "water", "lightning", "fire"]:
		ef.aplicar(x, el, 10.0, null)
		if float(x.burn_left) > 0.0 and float(x.wet_left) > 0.0:
			exclusivo = false
	_check(exclusivo, "estado TERMICO e' exclusivo: a reacao substitui, nunca soma")
	# --- teto de atordoamento do kernel: NUNCA acima de stun_cap
	x.atordoar(5.0)
	_check(is_equal_approx(float(x.stun_left), float(st.stun_cap)),
			"atordoamento tem teto de %.2fs (kernel)" % float(st.stun_cap))

	# --- ARCO de conducao: o segundo molhado a menos de conduct_arc_m toma dano
	var arena := Node3D.new()
	root.add_child(arena)
	var p1: CharacterBody3D = pawn_scr.new()
	var p2: CharacterBody3D = pawn_scr.new()
	arena.add_child(p1)
	arena.add_child(p2)
	p2.global_position = Vector3(float(st.conduct_arc_m) - 1.0, 0, 0)
	ef.aplicar(p1, "water", 10.0, null)
	ef.aplicar(p2, "water", 10.0, null)
	var sh2 := float(p2.shield)
	ef.aplicar(p1, "lightning", 20.0, null)
	_check(float(p2.shield) < sh2, "conducao ARCA para outro molhado a menos de 4m")
	var p3: CharacterBody3D = pawn_scr.new()
	arena.add_child(p3)
	p3.global_position = Vector3(float(st.conduct_arc_m) + 5.0, 0, 0)
	ef.aplicar(p3, "water", 10.0, null)
	var sh3 := float(p3.shield)
	ef.aplicar(p1, "lightning", 20.0, null)
	_check(is_equal_approx(float(p3.shield), sh3), "molhado LONGE nao recebe o arco")

	# --- VENTO empurra o dobro; a pedra empurra pouco mais que o fogo
	_check(is_equal_approx(_combat.fator("wind", "empurrao"), 2.0),
			"vento empurra 2x (a identidade dele e' tirar do lugar)")
	_check(_combat.fator("wind", "empurrao") > _combat.fator("earth", "empurrao") \
			and _combat.fator("earth", "empurrao") > _combat.fator("fire", "empurrao"),
			"empurrao deixou de ser constante unica para os 5")
	_check(is_equal_approx(_combat.fator("terrain", "esc"), 1.0),
			"quem nao e' elemento (terreno/DoT) e' NEUTRO em esc/vida")

	# --- elemento nao muda estado de COISA (muro, arvore, loot)
	var coisa := StaticBody3D.new()
	root.add_child(coisa)
	_check(is_equal_approx(float(ef.aplicar(coisa, "lightning", 10.0, null)), 1.0),
			"elemento em estrutura nao reage aqui (quem reage e' o TerrainSystem)")
	for n: Node in [a, v, f, seco, x, arena, coisa]:
		n.queue_free()


## DANO AO LONGO DO TEMPO — as duas leis (DANO.md 3.5) + o fim do dano a 60Hz.
func _test_dot() -> void:
	print("[DoT: teto de dps, ignora escudo e tique de %.2fs]" % float(_bal.DOT.tick))
	var pawn_scr: GDScript = load("res://gameplay/Pawn.gd")
	var tick := float(_bal.DOT.tick)

	# --- LEI 1: DoT ignora escudo e vai direto na vida
	var p: CharacterBody3D = pawn_scr.new()
	root.add_child(p)
	var sh0 := float(p.shield)
	p.acender(float(_bal.STATUS.burn_dps), 10.0)
	p.status_step(tick)
	_check(is_equal_approx(float(p.shield), sh0), "queimadura NAO toca o escudo")
	_check(is_equal_approx(float(p.hp), float(_bal.PLAYER.hp) - float(_bal.STATUS.burn_dps) * tick),
			"queimadura vai direto na VIDA (%.1f dps)" % float(_bal.STATUS.burn_dps))

	# --- o tique: nada e' aplicado antes de fechar DOT.tick (fim do dano a 60Hz)
	var q: CharacterBody3D = pawn_scr.new()
	root.add_child(q)
	q.acender(float(_bal.STATUS.burn_dps), 10.0)
	q.status_step(tick * 0.4)
	_check(is_equal_approx(float(q.hp), float(_bal.PLAYER.hp)), "antes do tique nao aplica nada")
	q.status_step(tick * 0.6)
	_check(float(q.hp) < float(_bal.PLAYER.hp), "no tique aplica o balde inteiro, de uma vez")
	_check(is_equal_approx(float(_bal.PLAYER.hp) - float(q.hp), float(_bal.STATUS.burn_dps) * tick),
			"o balde acumula EXATO (nao perde nem inventa dano fracionario)")

	# --- LEI 2: teto de dps somado, nao importa quantas fontes
	var t: CharacterBody3D = pawn_scr.new()
	root.add_child(t)
	t.acender(999.0, 10.0)   # fonte absurda: o teto tem que segurar
	t.status_step(tick)
	_check(is_equal_approx(float(_bal.PLAYER.hp) - float(t.hp), float(_bal.DOT.teto_dps) * tick),
			"teto de %.0f dps somados segura qualquer combinacao de fontes" % float(_bal.DOT.teto_dps))

	# --- a queimadura EXPIRA (nao e' dano eterno) e nao acumula em pilha
	var d: CharacterBody3D = pawn_scr.new()
	root.add_child(d)
	d.acender(float(_bal.STATUS.burn_dps), float(_bal.STATUS.burn_dur))
	d.acender(float(_bal.STATUS.burn_dps), float(_bal.STATUS.burn_dur))
	_check(is_equal_approx(float(d.burn_left), float(_bal.STATUS.burn_dur)),
			"queimadura REFRESCA a duracao, nunca empilha")
	d.status_step(float(_bal.STATUS.burn_dur) + 1.0)
	_check(is_zero_approx(float(d.burn_left)) and is_zero_approx(float(d.burn_dps)),
			"queimadura expira sozinha")
	_check(d.estado() == "", "sem estado ativo o icone da HUD fica vazio")

	# --- atordoado o corpo nao obedece ao joystick
	var s: CharacterBody3D = pawn_scr.new()
	root.add_child(s)
	s.atordoar(0.5)
	s.move_velocity(Vector3.RIGHT, 1.0 / 60.0)
	_check(is_zero_approx(float(s.velocity.x)), "atordoado nao anda")
	s.move_velocity(Vector3.RIGHT, 1.0)
	s.move_velocity(Vector3.RIGHT, 1.0 / 60.0)
	_check(float(s.velocity.x) > 0.0, "passado o atordoamento volta a andar")
	for n: Node in [p, q, t, d, s]:
		n.queue_free()


## O GARGALO DO ESTUDO (DANO.md 2.3/2.4): dano fracionario virava 0 sessenta
## vezes por segundo, e o sinal nao dizia QUEM causou nem SE bateu em escudo.
func _test_sinal_sem_zero() -> void:
	print("[Bus: damage_applied com source + on_shield, e NENHUM evento com 0]")
	var zerado := 0
	for ev: Array in _dmg_full:
		if not (float(ev[1]) > 0.0):
			zerado += 1
	_check(_dmg_full.size() > 0 and zerado == 0,
			"nenhum dos %d eventos de dano saiu com amount 0" % _dmg_full.size())
	var legado_zero := 0
	for ev: Array in _dmg_events:
		if int(ev[1]) <= 0:
			legado_zero += 1
	_check(legado_zero == 0, "o sinal legado tambem nunca sai com 0 (troco de fracao)")
	var pawn_scr: GDScript = load("res://gameplay/Pawn.gd")
	var src: CharacterBody3D = pawn_scr.new()
	root.add_child(src)
	var alvo: CharacterBody3D = pawn_scr.new()
	root.add_child(alvo)
	_dmg_full.clear()
	_combat.deal(alvo, 10.0, "fire", src)
	_check(_dmg_full.size() == 1 and _dmg_full[0][3] == src,
			"damage_applied carrega a FONTE (destrava indicador direcional e hitmarker)")
	_check(bool(_dmg_full[0][4]), "on_shield=true quando o escudo absorveu")
	alvo.shield = 0.0
	_dmg_full.clear()
	_combat.deal(alvo, 10.0, "fire", src)
	_check(not bool(_dmg_full[0][4]), "on_shield=false quando o acerto foi na vida")
	_check(_dmg_full[0][1] is float, "amount e' FLOAT (era int(round()) e virava 0 no terreno)")
	_dmg_full.clear()
	alvo.acender(float(_bal.STATUS.burn_dps), 5.0)
	alvo.status_step(float(_bal.DOT.tick))
	_check(_dmg_full.size() == 1 and _dmg_full[0][3] == null,
			"DoT/terreno sai com source null (nao tem direcao — nao gera arco na HUD)")
	src.queue_free()
	alvo.queue_free()


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
	p.shield = 0.0  # este bloco mede VIDA; o escudo tem bloco proprio abaixo
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


## R20 GAME FEEL: o Diretor pediu "movimentacao e AGILIDADE". O que da' pra
## provar sem o dedo no aparelho: a velocidade ACELERA (nao e' mais
## interruptor), o freio para em tempo previsivel sem passar do ponto, o dash
## percorre a distancia configurada mesmo com a curva de arranque, a animacao
## escala com a velocidade e a histerese nao deixa a anim piscar.
func _test_game_feel() -> void:
	print("[Game feel: aceleracao · freio · dash com arranque · anim x velocidade]")
	var pawn_scr: GDScript = load("res://gameplay/Pawn.gd")
	var player_scr: GDScript = load("res://gameplay/Player.gd")
	var vmax := float(_bal.PLAYER.speed)
	var alvo := Vector3.RIGHT * vmax
	var dt := 1.0 / 60.0

	# --- aceleracao: nao e' instantanea, mas CONVERGE pro maximo
	var v: Vector3 = pawn_scr.accel_step(Vector3.ZERO, alvo, dt, true)
	_check(v.length() > 0.0 and v.length() < vmax * 0.5,
			"1 frame NAO chega na velocidade cheia (fim do interruptor)")
	var t := dt
	while v.length() < vmax - 0.001 and t < 3.0:
		v = pawn_scr.accel_step(v, alvo, dt, true)
		t += dt
	_check(is_equal_approx(v.length(), vmax), "aceleracao converge pra velocidade maxima")
	var t_esperado := vmax / float(_bal.MOVE.accel)
	_check(absf(t - t_esperado) <= dt * 1.5,
			"tempo ate a velocidade cheia = speed/accel (%.3fs)" % t_esperado)
	_check(pawn_scr.accel_step(Vector3.ZERO, alvo, dt, false).length() 			< pawn_scr.accel_step(Vector3.ZERO, alvo, dt, true).length(),
			"no ar acelera menos (air_control)")

	# --- freio: para em tempo previsivel e NAO passa do ponto (nao inverte)
	v = alvo
	t = 0.0
	while v.length() > 0.0 and t < 3.0:
		v = pawn_scr.accel_step(v, Vector3.ZERO, dt, true)
		t += dt
		_check_silent(v.x >= 0.0, "freio nunca inverte o sentido")
	_check(v == Vector3.ZERO, "freio zera a velocidade")
	_check(absf(t - vmax / float(_bal.MOVE.brake)) <= dt * 1.5,
			"tempo de frenagem = speed/brake (%.3fs)" % (vmax / float(_bal.MOVE.brake)))
	_check(_silent_fails == 0, "freio nunca passa do ponto (sem overshoot)")
	# inverter o sentido usa a taxa de curva, mais alta que a de arranque
	_check((alvo - pawn_scr.accel_step(alvo, -alvo, dt, true)).length() 			> (alvo - pawn_scr.accel_step(alvo, Vector3.ZERO, dt, true)).length(),
			"inverter o sentido responde mais rapido que so' frear (turn_accel)")

	# --- dash: arranque na frente, mas a distancia continua a do Balance
	var media := float(_bal.DODGE.distance) / float(_bal.DODGE.duration)
	_check(pawn_scr.dash_speed_at(0.0) > media * 1.5, "dash comeca MUITO mais rapido que a media")
	_check(pawn_scr.dash_speed_at(1.0) < pawn_scr.dash_speed_at(0.0), "e desacelera ate o fim")
	_check(pawn_scr.dash_speed_at(1.0) > 0.0, "burst nao estoura (velocidade final > 0)")
	var d: CharacterBody3D = pawn_scr.new()
	root.add_child(d)
	d.try_dodge(Vector3.RIGHT)
	var dist := 0.0
	var passo := float(_bal.DODGE.duration) / 18.0
	for i in 18:
		d.move_velocity(Vector3.ZERO, passo)
		dist += d.velocity.x * passo
	_check(absf(dist - float(_bal.DODGE.distance)) < float(_bal.DODGE.distance) * 0.02,
			"dash percorre a distancia do Balance (%.2fm, medido %.2fm)"
			% [float(_bal.DODGE.distance), dist])
	_check(d._move_vel.length() > 0.0, "sai do dash com momentum (nao para seco)")

	# --- animacao proporcional a velocidade (fim da patinacao)
	var ref: float = d._run_reference_speed()
	_check(ref > 0.0, "referencia de cadencia sai da duracao do ciclo de 'run'")
	if ref > 0.0:
		_check(is_equal_approx(ref, float(_bal.ANIM.run_stride_m) / float(d.visual.get_anim_length("run"))),
				"referencia = run_stride_m / ciclo da anim")
		d._run_anim = true
		d.velocity = Vector3(ref * 0.8, 0, 0)
		d.anim("run")
		var e1: float = d._anim_speed
		d.velocity = Vector3(ref * 1.6, 0, 0)
		d.anim("run")
		var e2: float = d._anim_speed
		_check(is_equal_approx(e1, 0.8) and is_equal_approx(e2, 1.6),
				"speed_scale = velocidade / referencia (%.2f e %.2f)" % [e1, e2])
		d.velocity = Vector3(ref * 99.0, 0, 0)
		d.anim("run")
		_check(is_equal_approx(d._anim_speed, float(_bal.ANIM.scale_max)), "speed_scale tem teto")
		d.anim("cast")
		_check(is_equal_approx(d._anim_speed, 1.0), "em 'cast' o speed_scale volta a 1.0 (o disparo nao pode sair do tempo)")

	# --- histerese: entre os dois limiares a animacao NAO pisca
	d._run_anim = false
	var meio := (float(_bal.MOVE.run_anim_enter) + float(_bal.MOVE.run_anim_exit)) * 0.5
	d.velocity = Vector3(meio, 0, 0)
	_check(d.locomotion_anim() == "idle", "velocidade no meio dos limiares: continua idle")
	d.velocity = Vector3(float(_bal.MOVE.run_anim_enter) + 0.5, 0, 0)
	_check(d.locomotion_anim() == "run", "acima do limiar de entrada: vira run")
	d.velocity = Vector3(meio, 0, 0)
	_check(d.locomotion_anim() == "run", "de volta ao meio: SEGUE run (nao pisca)")
	d.velocity = Vector3(float(_bal.MOVE.run_anim_exit) - 0.1, 0, 0)
	_check(d.locomotion_anim() == "idle", "abaixo do limiar de saida: volta a idle")
	d.queue_free()

	# --- joystick: zona morta reescalada + curva com curso fino no centro
	var dz := float(_bal.MOVE.stick_deadzone)
	_check(player_scr.stick_magnitude(Vector2(dz, 0)) == 0.0, "dentro da zona morta = parado")
	_check(is_equal_approx(player_scr.stick_magnitude(Vector2(1, 0)), 1.0),
			"joystick no talo = velocidade cheia")
	var logo_apos: float = player_scr.stick_magnitude(Vector2(dz + 0.001, 0))
	_check(logo_apos < 0.02, "logo depois da zona morta sai do ZERO (sem degrau)")
	var m1: float = player_scr.stick_magnitude(Vector2(0.5, 0))
	var m2: float = player_scr.stick_magnitude(Vector2(0.75, 0))
	_check(m1 < 0.5 and m2 > m1, "curva > 1: sobra curso fino no centro (da' pra andar devagar)")


## BAU CELESTIAL (GDD 16.2) — o evento de mundo que abre a UNICA porta da
## manopla. Sem ele a arma lendaria existe no registro e e' inalcancavel em
## partida. Prova o ciclo inteiro sem esperar 51 segundos: o relogio do bau e'
## um Timer de verdade, entao o teste so' bate nele.
func _test_bau() -> void:
	print("[Bau Celestial: queda telegrafada, canalizacao e manopla (GDD 16.2)]")
	var bau_scr: GDScript = load("res://gameplay/BauCelestial.gd")
	var arma: GDScript = load("res://gameplay/Arma.gd")
	var slot_scr: GDScript = load("res://gameplay/ArmaSlot.gd")
	var loot_scr: GDScript = load("res://gameplay/Loot.gd")
	var pawn_scr: GDScript = load("res://gameplay/Pawn.gd")

	# --- QUANDO CAI: relogio FIXO (nao sorteado), dentro da partida e com briga
	# sobrando depois. Se alguem subir ANUNCIO_S sem pensar, quebra aqui.
	var pousa: float = float(bau_scr.ANUNCIO_S) + float(bau_scr.QUEDA_S)
	_check(pousa < float(_bal.MATCH.duration_s), "o bau pousa DENTRO da partida (%.0fs de %.0fs)"
			% [pousa, float(_bal.MATCH.duration_s)])
	_check(float(_bal.MATCH.duration_s) - pousa > 60.0,
			"sobra mais de 60s de briga pela manopla depois do pouso")
	_check(float(bau_scr.QUEDA_S) >= 3.0, "a queda e' TELEGRAFADA (>= 3s brilhando no ceu)")
	_check(float(bau_scr.CANALIZAR_S) > 0.0, "abrir CANALIZA (a janela de contra-ataque)")

	# --- DETERMINISMO: mesmo seed = mesmo ponto de pouso E mesmo par de elementos
	var mapa := Node3D.new()
	root.add_child(mapa)
	var a: Node3D = bau_scr.agendar(mapa, null)
	var b: Node3D = bau_scr.agendar(mapa, null)
	_check(a.global_position.distance_to(b.global_position) < 0.001,
			"MESMO SEED = mesmo ponto de pouso")
	_check(Array(a.elementos()) == Array(b.elementos()), "MESMO SEED = mesmo par de elementos")
	var c: Node3D = bau_scr.agendar(mapa, null, 4242)
	_check(a.global_position.distance_to(c.global_position) > 0.001,
			"seed diferente = ponto de pouso diferente")
	var raio := Vector2(a.global_position.x, a.global_position.z).length()
	_check(raio >= float(bau_scr.RAIO_MIN) - 0.01 and raio <= float(bau_scr.RAIO_MAX) + 0.01,
			"pousa em campo ABERTO: anel de %.0f a %.0fm do centro (medido %.1fm)"
			% [float(bau_scr.RAIO_MIN), float(bau_scr.RAIO_MAX), raio])
	b.queue_free()

	# --- A QUEDA: o relogio do proprio bau leva ESPERANDO -> CAINDO -> POUSADO
	_bau_anuncios.clear()
	_bau_pousos.clear()
	_check(int(a.fase) == int(a.ESPERANDO), "nasce esperando (nada no ceu ainda)")
	_check(is_equal_approx(float(a._timer.wait_time), float(bau_scr.ANUNCIO_S)),
			"relogio armado em ANUNCIO_S (%.0fs)" % float(bau_scr.ANUNCIO_S))
	a._timer.timeout.emit()
	_check(int(a.fase) == int(a.CAINDO), "no tempo do anuncio o bau COMECA A CAIR")
	_check(_bau_anuncios.size() == 1, "Bus.bau_anunciado avisa a HUD da queda")
	_check((_bau_anuncios[0][0] as Vector3).distance_to(a.global_position) < 0.001,
			"o anuncio leva o ponto EXATO de pouso (marcador na bussola)")
	_check(is_equal_approx(float(_bau_anuncios[0][1]), float(bau_scr.QUEDA_S)),
			"e quantos segundos faltam para pousar")
	_check(not a.monitoring, "bau NO AR nao pode ser aberto")
	_check(is_equal_approx(float(a._timer.wait_time), float(bau_scr.QUEDA_S)),
			"relogio rearmado para o pouso")
	a._timer.timeout.emit()
	_check(int(a.fase) == int(a.POUSADO), "pousou no tempo esperado")
	_check(a.monitoring, "pousado: agora da pra abrir")
	_check(_bau_pousos.size() == 1, "Bus.bau_pousou emitido")

	# --- CANALIZAR: so' anda com alguem em cima, e sair CANCELA
	var p: CharacterBody3D = pawn_scr.new()
	p.add_to_group("player")
	mapa.add_child(p)
	var slot: Node3D = slot_scr.new()
	p.add_child(slot)
	p.global_position = a.global_position
	c._entrou(p)
	_check(not c.is_processing(), "bau que ainda NAO pousou nao canaliza")
	c.queue_free()
	_bau_prog.clear()
	_check(not a.is_processing(), "bau vazio custa ZERO por frame (sem _process)")
	a._entrou(p)
	_check(a.is_processing(), "com alguem em cima, a canalizacao liga")
	a._process(float(bau_scr.CANALIZAR_S) * 0.5)
	_check(int(a.fase) == int(a.POUSADO), "meia canalizacao NAO abre (da tempo de reagir)")
	_check(_bau_prog.size() == 1 and float(_bau_prog[0]) > 0.4 and float(_bau_prog[0]) < 0.6,
			"Bus.bau_canalizando leva o progresso do player (%.2f)" % float(_bau_prog[0]))
	a._saiu(p)
	_check(not a.is_processing() and is_zero_approx(float(a._progresso)),
			"sair do raio CANCELA a canalizacao (e desliga o _process)")
	_check(float(_bau_prog[-1]) == 0.0, "e a HUD e' avisada do cancelamento")

	# --- ABRIR: a manopla com os 2 elementos FIXOS, equipada na hora
	_bau_abertos.clear()
	a._entrou(p)
	a._process(float(bau_scr.CANALIZAR_S) + 0.01)
	_check(int(a.fase) == int(a.ABERTO), "canalizacao completa ABRE o bau")
	_check(str(slot.arma_id) == "manopla", "abrir equipa a MANOPLA (a unica porta dela)")
	_check(slot.par.size() == 2, "a manopla tem EXATAMENTE 2 elementos")
	_check(Array(slot.par) == Array(a.elementos()),
			"e sao os do bau: %s (deterministico pelo seed)" % str(Array(a.elementos())))
	var validos := true
	for el in slot.par:
		if not (str(el) in _bal.ELEMENTS):
			validos = false
	_check(validos, "os 2 elementos existem em Balance")
	var fr: float = float(slot.spec("fire").fire_rate)
	_check(fr < float(_bal.FIRE.fire_rate), "equipada, conjura mais rapido que a linha de base")
	_check(is_equal_approx(fr, float(_bal.FIRE.fire_rate) * float(arma.dados("manopla").fire_rate)),
			"cadencia = Balance x multiplicador da MANOPLA (estalar de dedos)")
	var e1: String = slot.elemento_do_disparo("water")
	var e2: String = slot.elemento_do_disparo("water")
	_check(e1 != e2 and (e1 in Array(slot.par)) and (e2 in Array(slot.par)),
			"alterna entre os 2 fixos: o carrossel nao troca elemento de manopla")
	_check(_bau_abertos.size() == 1 and bool(_bau_abertos[0][0]),
			"Bus.bau_aberto avisa que a manopla foi para o PLAYER")
	_check(Array(_bau_abertos[0][1]) == Array(a.elementos()),
			"e leva os 2 elementos para a HUD mostrar")
	# a arma velha fica no chao no lugar do bau (mesmo padrao BR do loot)
	var velhas := 0
	for n in mapa.get_children():
		if n.get_script() == loot_scr and str(n.arma_id) == "varinha":
			velhas += 1
	_check(velhas == 1, "a varinha trocada ficou no chao onde o bau abriu")

	p.queue_free()
	mapa.queue_free()


## ARMAS ARCANAS (GDD 16.2) — o registro e' DADO, entao da' pra provar inteiro
## sem cena, sem fisica e sem frame.
func _test_armas() -> void:
	print("[Armas arcanas: registro (GDD 16.2)]")
	var arma: GDScript = load("res://gameplay/Arma.gd")
	var campos := ["nome", "raridade", "dmg", "fire_rate", "projectile_speed",
			"range", "mana_cost", "elementos", "runas", "suprema_bonus"]
	_check(arma.ARMAS.size() == 3, "3 armas: varinha, cajado, manopla")
	var completo := true
	for id: String in ["varinha", "cajado", "manopla"]:
		if not arma.ARMAS.has(id):
			completo = false
			continue
		for c: String in campos:
			if not (arma.ARMAS[id] as Dictionary).has(c):
				completo = false
	_check(completo, "toda arma tem os %d campos do 16.2" % campos.size())

	var v: Dictionary = arma.dados("varinha")
	var c: Dictionary = arma.dados("cajado")
	var m: Dictionary = arma.dados("manopla")
	# VARINHA = linha de base: sem ela em 1.0 nao existe "zero regressao"
	var base_ok := true
	for k: String in ["dmg", "fire_rate", "projectile_speed", "range", "mana_cost"]:
		if not is_equal_approx(float(v[k]), 1.0):
			base_ok = false
	_check(base_ok, "varinha e' a LINHA DE BASE (multiplicadores 1.0)")
	_check(int(v.elementos) == 1 and str(v.raridade) == "comum", "varinha: comum, 1 elemento")

	# CAJADO: rifle/sniper — paga cadencia por alcance e dano
	_check(float(c.dmg) > float(v.dmg), "cajado bate mais forte que a varinha")
	_check(float(c.range) > float(v.range), "cajado alcanca mais longe")
	_check(float(c.fire_rate) > float(v.fire_rate), "cajado conjura MAIS LENTO (atraso maior)")
	_check(float(c.suprema_bonus) > 1.0, "cajado canaliza supremas com bonus")
	_check(str(c.raridade) == "raro", "cajado e' raro")

	# MANOPLA: lendaria, 2 elementos, estalar de dedos
	_check(str(m.raridade) == "lendaria", "manopla e' LENDARIA")
	_check(int(m.elementos) == 2, "manopla porta 2 elementos simultaneos")
	_check(float(m.fire_rate) < float(v.fire_rate), "manopla conjura MAIS RAPIDO (estalar de dedos)")
	_check(float(m.fire_rate) < float(c.fire_rate), "manopla e' o cast mais rapido dos 3")

	# slots de runa por raridade (GDD 16.3 — estrutura pronta, runas depois)
	_check(int(v.runas) == 1 and int(c.runas) == 2 and int(m.runas) == 3,
			"slots de runa 1/2/3 (GDD 16.3)")

	# tier ordena a escada de poder (o loot decide upgrade por aqui)
	_check(arma.tier("varinha") < arma.tier("cajado") \
			and arma.tier("cajado") < arma.tier("manopla"), "tier: varinha < cajado < manopla")

	# RARIDADE = COR + FORMA (GDD 10): as duas dimensoes tem que ser distintas
	var cores := {}
	var formas := {}
	for id: String in ["varinha", "cajado", "manopla"]:
		cores[arma.cor(id)] = true
		formas[arma.forma(id)] = true
	_check(cores.size() == 3, "3 cores de raridade distintas")
	_check(formas.size() == 3, "3 FORMAS de raridade distintas (GDD 10: nunca so' cor)")

	# pares FIXOS da manopla: 2 elementos, e elementos que Balance conhece
	var pares: Array = arma.PARES_MANOPLA
	var pares_ok := not pares.is_empty()
	for par: Array in pares:
		if par.size() != 2 or not (par[0] in _bal.ELEMENTS) or not (par[1] in _bal.ELEMENTS):
			pares_ok = false
	_check(pares_ok, "todo par de manopla tem 2 elementos validos de Balance")

	# spec = perfil do elemento x tier da arma, nos 5 elementos
	var spec_ok := true
	for el in _bal.ELEMENTS:
		var b: Dictionary = arma.base(str(el))
		var sc: Dictionary = arma.spec(str(el), "cajado")
		if not is_equal_approx(float(sc.dmg), float(b.dmg) * float(c.dmg)) \
				or not is_equal_approx(float(sc.range), float(b.range) * float(c.range)):
			spec_ok = false
		if arma.spec(str(el), "varinha") != {"dmg": float(b.dmg),
				"mana_cost": float(b.mana_cost), "fire_rate": float(b.fire_rate),
				"projectile_speed": float(b.projectile_speed), "range": float(b.range)}:
			spec_ok = false
	_check(spec_ok, "spec = Balance x arma nos 5 elementos (varinha = identidade)")


## LOOT: nascer no mapa, pegar, equipar e TROCAR O ATAQUE.
func _test_loot() -> void:
	print("[Loot: achar no mapa, pegar e equipar (GDD 16.2)]")
	var arma: GDScript = load("res://gameplay/Arma.gd")
	var slot_scr: GDScript = load("res://gameplay/ArmaSlot.gd")
	var loot_scr: GDScript = load("res://gameplay/Loot.gd")
	var proj: GDScript = load("res://gameplay/Projectile.gd")

	# --- slot: sem arma = varinha; e o fallback de quem NAO tem slot
	var pawn := Node3D.new()
	root.add_child(pawn)
	var slot: Node3D = slot_scr.new()
	pawn.add_child(slot)
	_check(str(slot.arma_id) == "varinha", "todo mundo comeca de varinha")
	_check(slot_scr.de(pawn) == slot, "ArmaSlot.de acha o slot do pawn")
	var sem := Node3D.new()
	root.add_child(sem)
	_check(slot_scr.spec_de(sem, "fire") == arma.base("fire"),
			"pawn SEM slot cai no perfil cru do elemento (zero regressao)")

	# --- equipar TROCA os atributos do ataque
	var antes: Dictionary = slot.spec("fire")
	_armados.clear()
	slot.equipar("cajado")
	var depois: Dictionary = slot.spec("fire")
	_check(float(depois.dmg) > float(antes.dmg), "equipar cajado aumenta o dano do ataque")
	_check(float(depois.range) > float(antes.range), "equipar cajado aumenta o alcance")
	_check(float(depois.fire_rate) > float(antes.fire_rate),
			"equipar cajado deixa a conjuracao mais lenta")
	_check(_armados.size() == 1 and str(_armados[0][0]) == "cajado" \
			and str(_armados[0][2]) == "raro", "Bus.weapon_equipped avisa a HUD (id + raridade)")

	# --- o PROJETIL le' a arma do atirador (sem editar Player/Bot/Pawn)
	var pc: Node = proj.launch(root, pawn, Vector3.ZERO, Vector3.FORWARD, "fire")
	_check(is_equal_approx(float(pc.dmg), float(arma.spec("fire", "cajado").dmg)),
			"projetil do cajado carrega o dano DA ARMA")
	_check(is_equal_approx(float(pc.travel_left), float(arma.spec("fire", "cajado").range)),
			"projetil do cajado voa o alcance DA ARMA")
	pc.queue_free()
	var pv: Node = proj.launch(root, sem, Vector3.ZERO, Vector3.FORWARD, "fire")
	_check(is_equal_approx(float(pv.dmg), float(_bal.FIRE.dmg)),
			"sem arma o projetil segue o Balance puro")
	pv.queue_free()

	# --- MANOPLA: 2 elementos fixos + cast quase instantaneo
	_armados.clear()
	slot.equipar("manopla", PackedStringArray(["fire", "wind"]))
	_check(slot.elementos("water").size() == 2, "manopla porta 2 elementos")
	_check(Array(slot.elementos("water")) == ["fire", "wind"],
			"o par e' FIXO: o carrossel nao troca elemento de manopla")
	_check(_armados.size() == 1 and Array(_armados[0][3]) == ["fire", "wind"],
			"Bus.weapon_equipped leva os 2 elementos para a HUD")
	var e1: String = slot.elemento_do_disparo("water")
	var e2: String = slot.elemento_do_disparo("water")
	_check(e1 != e2 and (e1 in ["fire", "wind"]) and (e2 in ["fire", "wind"]),
			"manopla alterna entre os 2 elementos tiro a tiro")
	_check(float(slot.spec("fire").fire_rate) < float(_bal.FIRE.fire_rate),
			"manopla: cast mais rapido que a linha de base (estalar de dedos)")
	slot.equipar("varinha")
	_check(Array(slot.elementos("water")) == ["water"], "varinha usa o elemento do carrossel")

	# --- DISTRIBUICAO deterministica
	var mapa_a := Node3D.new()
	root.add_child(mapa_a)
	var n_a: int = loot_scr.espalhar(mapa_a, null)
	var mapa_b := Node3D.new()
	root.add_child(mapa_b)
	var n_b: int = loot_scr.espalhar(mapa_b, null)
	var total: int = int(loot_scr.QTD_VARINHA) + int(loot_scr.QTD_CAJADO)
	_check(n_a == total, "nasceram %d loots (%d varinhas + %d cajados)" \
			% [total, loot_scr.QTD_VARINHA, loot_scr.QTD_CAJADO])
	_check(n_a == n_b, "mesma quantidade nas duas rodadas")
	_check(_pos_loot(mapa_a) == _pos_loot(mapa_b), "MESMO SEED = MESMAS posicoes (determinismo)")
	var mapa_c := Node3D.new()
	root.add_child(mapa_c)
	loot_scr.espalhar(mapa_c, null, 999)
	_check(_pos_loot(mapa_a) != _pos_loot(mapa_c), "seed diferente = mapa de loot diferente")

	var tipos := {}
	for l in mapa_a.get_children():
		tipos[str(l.arma_id)] = int(tipos.get(str(l.arma_id), 0)) + 1
	_check(int(tipos.get("varinha", 0)) == int(loot_scr.QTD_VARINHA),
			"varinhas comuns e espalhadas")
	_check(int(tipos.get("cajado", 0)) == int(loot_scr.QTD_CAJADO), "cajados raros, 1 por POI")
	_check(not tipos.has("manopla"),
			"MANOPLA NAO nasce no chao (so' Bau Celestial — GDD 16.2)")

	# --- o gancho do Bau Celestial e' a UNICA porta da manopla
	var bau: Node = loot_scr.bau_celestial(mapa_a, Vector3(0, 2, 0), 0)
	_check(str(bau.arma_id) == "manopla", "bau_celestial() entrega manopla")
	_check(bau.par.size() == 2, "a manopla do bau ja' vem com o par fixo")

	# --- PEGAR troca a arma e deixa a velha no chao (padrao BR)
	var chao: Node = loot_scr.criar("cajado")
	mapa_a.add_child(chao)
	_check(bool(chao.pegar(slot)), "pegar equipa o cajado")
	_check(str(slot.arma_id) == "cajado", "slot agora tem cajado")
	_check(str(chao.arma_id) == "varinha", "a arma velha ficou no chao (troca, nao consumo)")
	# guarda: loot igual ao que ja' esta' na mao nao dispara troca nem sinal
	var igual: Node = loot_scr.criar("cajado")
	mapa_a.add_child(igual)
	_armados.clear()
	_check(not bool(igual.pegar(slot)), "pegar a arma que JA esta equipada nao faz nada")
	_check(_armados.is_empty(), "e nao emite Bus.weapon_equipped duplicado")

	pawn.queue_free()
	sem.queue_free()
	mapa_a.queue_free()
	mapa_b.queue_free()
	mapa_c.queue_free()


## Assinatura do mapa de loot: o que o determinismo tem que repetir.
func _pos_loot(mapa: Node3D) -> Array:
	var out: Array = []
	for l in mapa.get_children():
		out.append([str(l.arma_id), (l as Node3D).global_position.snapped(Vector3.ONE * 0.001)])
	return out


func _test_restart() -> void:
	print("[Main: restart limpo]")
	var main: Node3D = (load("res://gameplay/Main.gd") as GDScript).new()
	root.add_child(main)  # _ready monta ilha(fallback), HUD e a 1a partida
	var arena1: Node3D = main.arena
	var n_bots := int(_bal.MATCH.bots)
	_check(is_instance_valid(arena1), "partida 1 montada")
	_check(_count_bots(arena1) == n_bots, "%d bots na partida 1" % n_bots)
	_check(main.player.visual != null and main.player.visual.has_method("get_model_source") \
		and str(main.player.visual.get_model_source()).begins_with("external:res://characters/modelos/pyra.glb"),
		"player inicia como Pyra GLB")
	# suja o estado: dano no player, mata um bot, gasta tempo e mana.
	# 90 e' de proposito: 50 comem o escudo N1 e 40 TRANSBORDAM para a vida.
	_combat.deal(main.player, 90.0)
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
	_check(is_equal_approx(float(main.player.shield), float(_bal.ESCUDO.niveis[0])) 			and int(main.player.shield_level) == 1 and is_zero_approx(float(main.player.dmg_dealt)),
			"restart devolve escudo N1 cheio e zera o dano causado")
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
