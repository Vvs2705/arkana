## Selftest headless da ZONA (o circulo que fecha). Rodar:
##   Godot --headless --path godot --script res://gameplay/selftest_zona.gd
##
## Arquivo SEPARADO de gameplay/selftest.gd de proposito: aquele e' compartilhado
## com outras raias e esta em edicao concorrente.
##
## Prova: as 5 fases cabem na duracao da partida, os raios encolhem como
## projetado e cada circulo esta CONTIDO no anterior, o dano cresce e NUNCA sai
## zero (o bug de docs/DANO.md §2.3), quem esta fora toma e quem esta dentro
## nao, o relogio das fases arma os tempos exatos da tabela, mesmo seed = mesmos
## centros (e seed diferente = centros diferentes), a HUD e avisada ANTES de
## fechar e a zona nao gasta um unico _process.
##
## NOTA: em modo --script os autoloads so' registram DEPOIS que este arquivo
## compila — por isso aqui NADA e' referenciado lexicalmente (Bus, Balance,
## Combat, Zona...): tudo chega via load()/get_node() no 1o frame.
extends SceneTree

var fails := 0
var _bus: Node
var _bal: Node
var _zona: GDScript
var _pawn: GDScript
var _dmg_full: Array = []   # damage_applied (novo, float)
var _avisos: Array = []
var _fechamentos: Array = []
var _zdano: Array = []
var _zestado: Array = []


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	_bus = root.get_node("Bus")
	_bal = root.get_node("Balance")
	_zona = load("res://gameplay/Zona.gd")
	_pawn = load("res://gameplay/Pawn.gd")
	_bus.damage_applied.connect(func(t: Node, a: float, e: String, s: Node, esc: bool) -> void:
		_dmg_full.append([t, a, e, s, esc]))
	_bus.zona_avisou.connect(func(f: int, c: Vector3, r: float, s: float) -> void:
		_avisos.append([f, c, r, s]))
	_bus.zona_fechando.connect(func(f: int, c: Vector3, r: float, d: float) -> void:
		_fechamentos.append([f, c, r, d]))
	_bus.zona_dano.connect(func(d: float, dps: float) -> void: _zdano.append([d, dps]))
	_bus.zona_estado.connect(func(dentro: bool) -> void: _zestado.append(dentro))

	_test_desenho()
	_test_determinismo()
	_test_relogio()
	_test_dano()
	print("")
	print("ZONA: %d falha(s)" % fails)
	quit(0 if fails == 0 else 1)


## A ZONA ESCALA COM O MAPA — e NINGUEM nasce fora dela.
##
## Este bloco existe por um defeito medido em 26/08: a ilha passou de 180 para
## 300 m e RAIO_INICIAL continuou 90, ou seja, menos de um terco do mapa. Seis
## dos catorze pontos de nascimento ficaram FORA do primeiro circulo e tomavam
## dano de tempestade no segundo zero da partida.
##
## O teste velho ("r >= 90") passou verde o tempo todo, porque cobrava um numero
## e nao uma RELACAO. Aqui a conta e' contra o mapa de verdade: se a ilha
## crescer de novo e a zona nao acompanhar, isto fica vermelho.
func _test_escala_do_mapa() -> void:
	print("[A zona acompanha o tamanho do mapa]")
	var cena := "res://world/Island.tscn"
	if not ResourceLoader.exists(cena):
		_check(false, "Island.tscn existe para medir a escala")
		return
	var ilha: Node3D = (load(cena) as PackedScene).instantiate()
	root.add_child(ilha)

	var lado := float(ilha.SIZE)
	var esc: float = _zona.escala_do_mapa(ilha)
	_check(is_equal_approx(esc, lado / float(_zona.ILHA_REF)),
		"escala sai do lado REAL da ilha (%.0fm / regua %.0fm = %.2fx)"
		% [lado, float(_zona.ILHA_REF), esc])
	_check(_zona.escala_do_mapa(null) == 1.0,
		"sem ilha, escala 1.0 — a zona roda sozinha em teste")

	var plano: Array = _zona.plano(ilha)
	var abertura: float = float(plano[0].raio) 		* (float(_zona.RAIO_INICIAL) / float(_zona.FASES[0].raio))
	_check(abertura >= lado * 0.5 * 0.98,
		"a abertura cobre meia-ilha DE VERDADE: %.0fm para um mapa de %.0fm"
		% [abertura, lado])

	# o que o defeito realmente causava: gente nascendo na tempestade
	var fora := 0
	var total := 0
	for n in get_nodes_in_group("spawn"):
		if not (n is Node3D):
			continue
		total += 1
		var p: Vector3 = (n as Node3D).global_position
		if Vector2(p.x - plano[0].centro.x, p.z - plano[0].centro.z).length() > abertura:
			fora += 1
	_check(total > 0, "ha' pontos de nascimento para conferir (%d)" % total)
	_check(fora == 0,
		"NENHUM nascimento cai fora do primeiro circulo (%d de %d fora)" % [fora, total])
	ilha.queue_free()


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)


# --------------------------------------------------- 1. o desenho das fases

func _test_desenho() -> void:
	print("[Desenho: as 5 fases cabem na partida e apertam de verdade]")
	var fases: Array = _zona.FASES
	var dur: float = float(_bal.MATCH.duration_s)
	var total := 0.0
	for f in fases:
		total += float(f.espera) + float(f.fecha)
	_check(total < dur, "a zona INTEIRA cabe na partida (%.0fs de %.0fs)" % [total, dur])
	_check(dur - total >= 10.0,
			"sobra duelo depois do ultimo circulo (%.0fs no circulo final)" % (dur - total))

	# raios: sempre menores, e o inicial cobre a ilha inteira (ninguem nasce fora)
	var r: float = float(_zona.RAIO_INICIAL)
	_check(r >= 90.0, "raio inicial de referencia cobre meia-ilha-REGUA: %.0fm" % r)
	_test_escala_do_mapa()

	r = float(_zona.RAIO_INICIAL)
	var ok_raio := true
	var ok_dps := true
	var dps := 0.0
	for f in fases:
		if float(f.raio) >= r:
			ok_raio = false
		r = float(f.raio)
		if float(f.dps) <= dps:
			ok_dps = false
		dps = float(f.dps)
	_check(ok_raio, "cada fase encolhe: 90 -> 62 -> 42 -> 24 -> 12 -> 4m")
	_check(ok_dps, "o dano CRESCE a cada fase (1.5 -> 18 dps)")
	_check(r > 0.0, "o circulo final tem area (%.0fm): o duelo tem chao" % r)

	# a fase final tem que MATAR quem fica fora dentro do tempo que sobra
	var resto: float = dur - total
	_check(dps * resto > float(_bal.PLAYER.hp),
			"a fase final resolve a partida: %.0f dps x %.0fs > %.0f de vida"
			% [dps, resto, float(_bal.PLAYER.hp)])

	# ANTI-BUG §2.3: o tique tem que ser grosso o bastante para nunca dar dano 0
	var tick: float = float(_zona.TICK_S)
	_check(tick >= 0.25, "o dano tem tique proprio de %.2fs (nao e' por frame)" % tick)
	_check(float(fases[0].dps) * tick >= 1.0,
			"o MENOR tique da zona ja' vale %.1f de dano — nunca int(0)"
			% (float(fases[0].dps) * tick))


# ------------------------------------------------------- 2. o determinismo

func _test_determinismo() -> void:
	print("[Determinismo: mesmo seed = mesma sequencia de circulos]")
	var a: Array = _zona.plano(null)
	var b: Array = _zona.plano(null)
	var iguais := a.size() == b.size()
	for i in a.size():
		if (a[i].centro as Vector3).distance_to(b[i].centro) > 0.0001:
			iguais = false
	_check(iguais, "MESMO SEED = os %d centros identicos" % a.size())
	var c: Array = _zona.plano(null, 9191)
	var diferente := false
	for i in a.size():
		if (a[i].centro as Vector3).distance_to(c[i].centro) > 0.001:
			diferente = true
	_check(diferente, "seed diferente = centros diferentes")
	_check(a.size() == (_zona.FASES as Array).size(), "o plano tem uma entrada por fase")

	## A LEI DO CIRCULO CONTIDO: se o circulo novo escapasse do velho, existiria
	## ponto seguro AGORA que fica fora depois — e planejar rotacao viraria sorte.
	var contido := true
	var pc := Vector3.ZERO
	var pr: float = float(_zona.RAIO_INICIAL)
	for e in a:
		if (e.centro as Vector3).distance_to(pc) + float(e.raio) > pr + 0.001:
			contido = false
		pc = e.centro
		pr = float(e.raio)
	_check(contido, "todo circulo novo esta' CONTIDO no anterior")

	## E o primeiro circulo nao pode sair da ilha (180m de lado = 90m de meia).
	var dentro_ilha := true
	for e in a:
		if Vector2((e.centro as Vector3).x, (e.centro as Vector3).z).length() + float(e.raio) > 90.001:
			dentro_ilha = false
	_check(dentro_ilha, "nenhum circulo passa da borda da ilha")


# ---------------------------------------------------- 3. o relogio das fases

func _test_relogio() -> void:
	print("[Relogio: avisa ANTES, fecha no tempo certo, fase a fase]")
	var mapa := Node3D.new()
	root.add_child(mapa)
	var z: Node3D = _zona.criar(mapa, null)
	var fases: Array = _zona.FASES

	_check(not z.is_processing(), "a zona NAO gasta _process (orcamento mobile)")
	_check(not z.is_physics_processing(), "nem _physics_process")
	_check(int(z.fase) == 0, "nasce antes da fase 1")
	_check(is_equal_approx(float(z.raio), float(_zona.RAIO_INICIAL)), "nasce com o raio inicial")
	_check(is_equal_approx(float(z.dps_atual()), 0.0),
			"antes da 1a fase a zona NAO doi (a partida abre com loot, nao sangrando)")
	_check(_avisos.size() == 1, "Bus.zona_avisou ja' publica o 1o circulo no boot")
	_check(is_equal_approx(float(_avisos[0][3]), float(fases[0].espera)),
			"o aviso leva os %.0fs de espera (contagem na HUD)" % float(fases[0].espera))

	## Percorre as 5 fases inteiras batendo os tempos da tabela contra o relogio.
	for i in fases.size():
		var f: Dictionary = fases[i]
		_check(is_equal_approx(float(z._timer.wait_time), float(f.espera)),
				"fase %d: relogio armado na espera (%.0fs)" % [i + 1, float(f.espera)])
		_check(not z.fechando, "fase %d: durante a espera a parede esta' PARADA" % (i + 1))
		var antes := _fechamentos.size()
		z._timer.timeout.emit()          # acabou a espera -> comeca a fechar
		_check(bool(z.fechando), "fase %d: no tempo do aviso a parede ANDA" % (i + 1))
		_check(int(z.fase) == i + 1, "fase %d: contador de fase avancou" % (i + 1))
		_check(_fechamentos.size() == antes + 1,
				"fase %d: Bus.zona_fechando avisou a HUD" % (i + 1))
		_check(is_equal_approx(float(_fechamentos[-1][3]), float(f.fecha)),
				"fase %d: o fechamento dura os %.0fs projetados" % [i + 1, float(f.fecha)])
		_check(is_equal_approx(float(z._timer.wait_time), float(f.fecha)),
				"fase %d: relogio rearmado para o fechamento" % (i + 1))
		var avisos_antes := _avisos.size()
		z._timer.timeout.emit()          # a parede chegou
		_check(is_equal_approx(float(z.raio), float(f.raio)),
				"fase %d: o raio parou EXATO em %.0fm" % [i + 1, float(f.raio)])
		_check(is_equal_approx(float(z.dps_atual()), float(f.dps)),
				"fase %d: o dano da fase e' %.1f dps" % [i + 1, float(f.dps)])
		if i + 1 < fases.size():
			_check(_avisos.size() == avisos_antes + 1,
					"fase %d: o PROXIMO circulo ja' e' publico (telegrafia)" % (i + 1))
			_check(is_equal_approx(float(_avisos[-1][2]), float(fases[i + 1].raio)),
					"fase %d: o aviso leva o raio do circulo seguinte" % (i + 1))
		else:
			_check(_avisos.size() == avisos_antes,
					"ultima fase: a tempestade nao avanca mais (nao ha' proximo circulo)")

	## Visual: procedural, zero binario (regra do projeto).
	_check(z._parede.mesh is CylinderMesh, "a parede e' malha PROCEDURAL (zero binario)")
	_check(is_equal_approx(z._parede.scale.x, float(z.raio)),
			"a parede acompanha o raio pelo setter (sem _process)")
	mapa.queue_free()


# ------------------------------------------------------------- 4. o dano

func _test_dano() -> void:
	print("[Dano: fora sangra, dentro nao, e nenhum sinal de zero]")
	var mapa := Node3D.new()
	root.add_child(mapa)
	var z: Node3D = _zona.criar(mapa, null)
	# cenario controlado: fase 1 (1.5 dps), circulo de 30m no centro
	z.fase = 1
	z.centro = Vector3.ZERO
	z.raio = 30.0

	var dentro: CharacterBody3D = _pawn.new()
	dentro.add_to_group("player")
	mapa.add_child(dentro)
	dentro.global_position = Vector3(5, 1, 0)
	var fora: CharacterBody3D = _pawn.new()
	mapa.add_child(fora)
	fora.global_position = Vector3(50, 1, 0)

	_check(z.dentro(dentro.global_position), "quem esta' a 5m do centro esta' DENTRO")
	_check(not z.dentro(fora.global_position), "quem esta' a 50m esta' FORA")
	_check(z.dentro(Vector3(30, 40, 0)),
			"a zona e' CILINDRO: subir no plato nao tira ninguem do circulo")

	var hp_dentro: float = float(dentro.hp)
	var hp_fora: float = float(fora.hp)
	_dmg_full.clear()
	_zdano.clear()
	_zestado.clear()
	z._tique()

	var esperado: float = float(_zona.FASES[0].dps) * float(_zona.TICK_S)
	_check(is_equal_approx(float(dentro.hp), hp_dentro), "quem esta' DENTRO nao perde vida")
	_check(is_equal_approx(float(fora.hp), hp_fora - esperado),
			"quem esta' FORA perde %.1f no tique (%.1f dps x %.0fs)"
			% [esperado, float(_zona.FASES[0].dps), float(_zona.TICK_S)])
	_check(_dmg_full.size() == 1, "UM sinal de dano por tique, nao um por frame")
	_check(_dmg_full[0][3] == null, "a tempestade nao tem autor (source null: e' ambiente)")
	_check(str(_dmg_full[0][2]) == "zona", "o dano sai etiquetado como 'zona'")
	# Migrado do extinto `damage_dealt` em 25/08/2026. A cobertura e' a mesma e
	# ela importa: a zona nao pode emitir dano ZERO a 60 Hz — era isso que o
	# arredondamento para int do sinal antigo mascarava.
	_check(float(_dmg_full[0][1]) > 0.0,
			"o dano do tique e' %.2f (> 0): o dano-zero de 60Hz nao existe aqui"
			% float(_dmg_full[0][1]))

	## O tique do BOT nao pode acender a vinheta do player.
	_check(_zdano.is_empty(), "bot fora da zona NAO emite zona_dano (so' o player)")
	_check(_zestado.is_empty(), "player dentro: nenhuma borda emitida")

	## Agora o PLAYER sai. Borda: um sinal na saida, nenhum enquanto fica fora.
	dentro.global_position = Vector3(45, 1, 0)
	z._tique()
	_check(_zestado.size() == 1 and _zestado[0] == false,
			"o player atravessou a parede: Bus.zona_estado(false) UMA vez")
	_check(_zdano.size() == 1, "e o player levou o tique da tempestade")
	z._tique()
	_check(_zestado.size() == 1, "continuar fora NAO repete a borda (nao e' sinal por frame)")
	_check(_zdano.size() == 2, "mas o dano continua cobrando, tique a tique")
	dentro.global_position = Vector3(0, 1, 0)
	z._tique()
	_check(_zestado.size() == 2 and _zestado[1] == true, "voltou para dentro: borda(true)")

	## Alvo morto sai da varredura sem sinal duplicado.
	fora.hp = 0.0
	var antes := _dmg_full.size()
	z._tique()
	_check(_dmg_full.size() == antes, "pawn morto nao leva dano da zona")
	mapa.queue_free()
