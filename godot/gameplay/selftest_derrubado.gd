## Selftest headless da raia DERRUBADO/REERGUER. Rodar:
##   Godot --headless --path godot --script res://gameplay/selftest_derrubado.gd
##
## Prova, sem aparelho e sem esperar 30s de relogio real (o tique e' chamado a
## mao; o Timer so' precisa ter a cadencia certa, e isso tambem e' verificado):
##   1. vida a zero com esquadrao ENTRA em derrubado — nao morre;
##   2. solo (e bot, que nao tem esquadrao) morre como sempre morreu;
##   3. derrubado nao conjura/nao usa habilidade, rasteja e perde o escudo;
##   4. o esvaecimento mata no tempo previsto — no tique 120 (30s), nao no 119;
##   5. reerguer devolve vida PARCIAL e ZERO escudo, preservando o NIVEL;
##   6. interromper faz o progresso DECAIR simetrico (nem zera, nem congela);
##   7. finalizacao por dano no chao mata na hora;
##   8. Bus.entity_died sai UMA vez por entidade, em toda a vida do estado;
##   9. os ganchos dos kits: Jardim da Aurora (50% mais rapido) e Lumen
##      (proxy do grupo "reanimador" reergue sem corpo).
##
## NOTA: em modo --script os autoloads so' registram DEPOIS que este arquivo
## compila — por isso aqui NADA e' referenciado lexicalmente (Bus, Balance,
## Combat, Derrubado): tudo chega via load()/get_node() no 1o frame.
extends SceneTree

var fails := 0
var _bus: Node
var _bal: Node
var _combat: GDScript
var _der: GDScript
var _pawn_scr: GDScript
var _died: Array = []
var _caidos: Array = []
var _levantados: Array = []
var _prog: Array = []


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	_bus = root.get_node("Bus")
	_bal = root.get_node("Balance")
	_combat = load("res://gameplay/Combat.gd")
	_der = load("res://gameplay/Derrubado.gd")
	_pawn_scr = load("res://gameplay/Pawn.gd")
	_bus.entity_died.connect(func(e: Node) -> void: _died.append(e))
	_bus.entity_derrubada.connect(func(e: Node, c: Node) -> void: _caidos.append([e, c]))
	_bus.entity_reerguida.connect(func(e: Node, p: Node) -> void: _levantados.append([e, p]))
	_bus.derrubado_progresso.connect(func(e: Node, esv: float, r: float) -> void:
		_prog.append([e, esv, r]))
	_test_cai_em_vez_de_morrer()
	_test_solo_morre()
	_test_esvaecimento()
	_test_reerguer()
	_test_interrupcao()
	_test_finalizacao()
	_test_ganchos_dos_kits()
	print("selftest derrubado: %s (%d falhas)" % ["OK" if fails == 0 else "FALHOU", fails])
	quit(1 if fails > 0 else 0)


# --------------------------------------------------------------------- testes

## 1+3: a vida zera e o mago CAI. Rasteja, perde o escudo, nao age, e o Bus
## NAO anuncia morte.
func _test_cai_em_vez_de_morrer() -> void:
	var a := _arena()
	var p := _pawn(a, Vector3.ZERO)
	var amigo := _pawn(a, Vector3(50, 0, 0))   # longe: nao resgata neste teste
	var vel := float(p.speed_factor)
	p.shield = 50.0
	p.hp = 0.0                                  # o Combat acabou de zerar
	var antes := _died.size()
	_check(bool(_der.interceptar(p, amigo)), "vida a zero: a costura manda NAO morrer")
	_check(bool(_der.esta(p)), "entrou no estado DERRUBADO")
	_check(float(p.hp) > 0.0, "reserva de esvaecimento no lugar da vida zerada")
	_check(_died.size() == antes, "entity_died NAO sai ao cair")
	_check(_caidos.size() == 1 and _caidos[0][0] == p and _caidos[0][1] == amigo,
			"Bus.entity_derrubada com o causador certo")
	_check(is_zero_approx(float(p.shield)), "derrubado fica SEM escudo")
	_check(is_equal_approx(float(p.speed_factor), vel * float(_der.RASTEJO)),
			"rastejar: velocidade cai pelo produto unico")
	_check(not bool(_der.pode_agir(p)), "derrubado nao conjura e nao usa habilidade")
	_check(bool(_der.pode_agir(amigo)), "quem esta' de pe' continua agindo")
	_check(not _prog.is_empty() and _prog[0][0] == p,
			"HUD avisada no instante da queda (derrubado_progresso)")
	a.queue_free()


## 2: BR SOLO. Sem esquadrao nao ha' quem reerga — entao nao ha' estado.
func _test_solo_morre() -> void:
	var a := _arena()
	var solo := _pawn(a, Vector3.ZERO)
	var bot := _pawn(a, Vector3(2, 0, 0), false)  # sem grupo "player" = sem esquadrao
	solo.hp = 0.0
	_check(not bool(_der.interceptar(solo, null)), "player solo: morre, nao cai")
	_check(not bool(_der.esta(solo)), "nenhum estado criado no solo")
	_check(not bool(_der.pode_cair(bot)), "bot nao tem esquadrao: morre como sempre morreu")
	a.queue_free()


## 4+8: o esvaecimento mata no tempo previsto e entity_died sai UMA vez.
func _test_esvaecimento() -> void:
	var a := _arena()
	var p := _pawn(a, Vector3.ZERO)
	_pawn(a, Vector3(50, 0, 0))                 # esquadrao existe, mas longe
	p.hp = 0.0
	_der.interceptar(p, null)
	var d: Node = _der.de(p)
	_check(is_equal_approx(float(d._timer.wait_time), float(_der.TIQUE))
			and not d._timer.is_stopped(), "relogio proprio (Timer) na cadencia do tique")
	d._timer.stop()                             # daqui pra frente o teste conduz
	var n := int(float(_der.ESVAECER_S) / float(_der.TIQUE))
	var antes := _died.size()
	for i in n - 1:
		d._tique()
	_check(bool(_der.esta(p)) and _died.size() == antes,
			"a %.2fs ainda esvaecendo (nao morreu cedo)" % (float(_der.ESVAECER_S) - float(_der.TIQUE)))
	_check(d.esvaecimento() < 0.05 and d.esvaecimento() > 0.0,
			"a HUD ve' o esvaecimento chegando ao fim")
	d._tique()
	_check(_died.size() == antes + 1 and _died[-1] == p,
			"aos %.0fs a morte de verdade sai UMA vez" % float(_der.ESVAECER_S))
	_check(not bool(_der.esta(p)), "estado desfeito na morte")
	_check(is_equal_approx(float(p.speed_factor), 1.0), "rastejo devolvido")
	a.queue_free()


## 5: quem reergue devolve vida PARCIAL e ZERO escudo (GDD §3).
func _test_reerguer() -> void:
	var a := _arena()
	var caido := _pawn(a, Vector3.ZERO)
	var medico := _pawn(a, Vector3(1.5, 0, 0))
	caido.shield_level = 3
	caido.shield = 100.0
	caido.hp = 0.0
	_der.interceptar(caido, null)
	var d: Node = _der.de(caido)
	d._timer.stop()
	var n := int(float(_der.REERGUER_S) / float(_der.TIQUE))
	var antes := _died.size()
	for i in n - 1:
		d._tique()
	_check(bool(_der.esta(caido)), "ainda caido antes do fim da canalizacao")
	_check(medico.is_in_group("reanimando"),
			"quem canaliza fica marcado (a costura do Player le' este grupo)")
	_check(d.progresso() > 0.9, "progresso do resgate quase cheio")
	d._tique()
	_check(not bool(_der.esta(caido)), "reerguido")
	_check(is_equal_approx(float(caido.hp), float(_bal.PLAYER.hp) * float(_der.VIDA_REERGUIDO)),
			"volta com vida PARCIAL (%d%%)" % int(float(_der.VIDA_REERGUIDO) * 100.0))
	_check(is_zero_approx(float(caido.shield)), "reerguer nao gera escudo (GDD §3)")
	_check(int(caido.shield_level) == 3, "o NIVEL do escudo sobrevive a queda")
	_check(not medico.is_in_group("reanimando"), "marcador do reanimador limpo")
	_check(_levantados.size() == 1 and _levantados[0][0] == caido and _levantados[0][1] == medico,
			"Bus.entity_reerguida com quem resgatou")
	_check(_died.size() == antes, "reerguer nao emite entity_died")
	_check(is_equal_approx(float(caido.speed_factor), 1.0), "volta a andar de pe'")
	a.queue_free()


## 6: INTERROMPER. O progresso decai na mesma velocidade com que subiu.
func _test_interrupcao() -> void:
	var a := _arena()
	var caido := _pawn(a, Vector3.ZERO)
	var medico := _pawn(a, Vector3(1.5, 0, 0))
	caido.hp = 0.0
	_der.interceptar(caido, null)
	var d: Node = _der.de(caido)
	d._timer.stop()
	for i in 8:
		d._tique()
	var meio: float = d.progresso()
	_check(meio > 0.0, "canalizando: progresso subiu")
	medico.global_position = Vector3(50, 0, 0)   # saiu do raio
	d._tique()
	_check(not medico.is_in_group("reanimando"), "saiu do raio: para de canalizar")
	_check(d.progresso() < meio, "progresso DECAI (nao congela)")
	for i in 8:
		d._tique()
	_check(is_zero_approx(d.progresso()),
			"decai simetrico: 8 tiques pra subir, 8 pra zerar")
	_check(bool(_der.esta(caido)), "interrompido continua caido")
	a.queue_free()


## 7+8: FINALIZACAO no chao — e nada de morrer duas vezes.
func _test_finalizacao() -> void:
	var a := _arena()
	var caido := _pawn(a, Vector3.ZERO)
	var algoz := _pawn(a, Vector3(50, 0, 0))
	caido.hp = 0.0
	_der.interceptar(caido, null)
	var d: Node = _der.de(caido)
	d._timer.stop()
	var antes := _died.size()
	_check(not bool(_der.interceptar(caido, algoz)),
			"ja' caido: a costura deixa a finalizacao passar (nao derruba de novo)")
	_check(_combat.deal(caido, 999.0, "fire", algoz, false) > 0.0,
			"dano ENTRA no derrubado (da' pra finalizar)")
	_check(_died.size() == antes + 1 and _died[-1] == caido,
			"finalizado: entity_died UMA vez")
	_check(not bool(_der.esta(caido)), "estado desfeito na finalizacao")
	a.queue_free()


## 9: os dois ganchos que a raia de habilidades vai usar.
func _test_ganchos_dos_kits() -> void:
	# Jardim da Aurora (§3.7): reerguer 50% mais rapido = 16 tiques em vez de 24.
	var a := _arena()
	var caido := _pawn(a, Vector3.ZERO)
	_pawn(a, Vector3(1.5, 0, 0))
	caido.hp = 0.0
	_der.interceptar(caido, null)
	var d: Node = _der.de(caido)
	d._timer.stop()
	_der.acelerar(caido, 1.5, 10.0)
	var n := int(float(_der.REERGUER_S) / (float(_der.TIQUE) * 1.5))
	for i in n - 1:
		d._tique()
	_check(bool(_der.esta(caido)), "Jardim: ainda nao levantou no penultimo tique")
	d._tique()
	_check(not bool(_der.esta(caido)), "Jardim da Aurora: resgate 50% mais rapido")
	a.queue_free()

	# Maos Livres (§3.7): a Lumen e' um PROXY sem corpo — reergue sozinha.
	var b := _arena()
	var ferido := _pawn(b, Vector3.ZERO)
	var lumen := Node3D.new()
	b.add_child(lumen)
	lumen.add_to_group("player")        # esquadrao da Vitalis
	lumen.add_to_group("reanimador")    # o proxy que reergue sem ter vida
	lumen.global_position = Vector3(1.0, 0, 0)
	ferido.hp = 0.0
	_check(bool(_der.interceptar(ferido, null)),
			"com a Lumen por perto ja' HA' esquadrao: cai em vez de morrer")
	var d2: Node = _der.de(ferido)
	d2._timer.stop()
	for i in int(float(_der.REERGUER_S) / float(_der.TIQUE)):
		d2._tique()
	_check(not bool(_der.esta(ferido)), "Maos Livres: a Lumen reergue sozinha")
	_check(_levantados[-1][1] == lumen, "o credito do resgate vai para a Lumen")
	# Truque de Fuga (§3.8): o REFLEXO caido nao e' um Pawn — e' um no' com hp.
	var reflexo := Node3D.new()
	reflexo.set_script(_reflexo_scr())
	b.add_child(reflexo)
	_check(_der.derrubar(reflexo, null) != null,
			"Truque de Fuga: da' pra derrubar o reflexo (no' sem escudo e sem corpo)")
	_check(bool(_der.esta(reflexo)), "o reflexo fica caido no lugar")
	b.queue_free()


# ------------------------------------------------------------------ utilidades

func _arena() -> Node3D:
	var a := Node3D.new()
	root.add_child(a)
	return a


func _pawn(a: Node3D, pos: Vector3, esquadrao := true) -> Node:
	var p: Node = _pawn_scr.new()
	a.add_child(p)
	if esquadrao:
		p.add_to_group("player")
	p.global_position = pos
	return p


## O reflexo do Ilusionista em miniatura: um no' que tem `hp` e nada mais.
func _reflexo_scr() -> GDScript:
	var s := GDScript.new()
	s.source_code = "extends Node3D\nvar hp := 0.0\n"
	s.reload()
	return s


func _check(ok: bool, msg: String) -> void:
	if not ok:
		fails += 1
		printerr("FALHOU: %s" % msg)
	else:
		print("ok: %s" % msg)
