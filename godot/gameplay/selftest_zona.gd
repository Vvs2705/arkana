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
var _aberturas: Array = []
var _formacoes: Array = []


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
	_bus.zona_abertura.connect(func(segs: float) -> void: _aberturas.append(segs))
	_bus.zona_formando.connect(func(r: float, d: float) -> void: _formacoes.append([r, d]))
	_bus.zona_fechando.connect(func(f: int, c: Vector3, r: float, d: float) -> void:
		_fechamentos.append([f, c, r, d]))
	_bus.zona_dano.connect(func(d: float, dps: float) -> void: _zdano.append([d, dps]))
	_bus.zona_estado.connect(func(dentro: bool) -> void: _zestado.append(dentro))

	_test_desenho()
	_test_abertura()
	_test_sorteio_por_partida()
	_test_determinismo()
	_test_relogio()
	_test_dano()
	print("")
	print("ZONA: %d falha(s)" % fails)
	quit(0 if fails == 0 else 1)


## A ZONA ACOMPANHA O TAMANHO DO MAPA — e NINGUEM nasce fora dela.
##
## Este bloco existe por um defeito medido em 26/08: a ilha passou de 180 para
## 300 m e o raio inicial continuou 90, ou seja, menos de um terco do mapa. Seis
## dos catorze pontos de nascimento ficaram FORA do primeiro circulo e tomavam
## dano de tempestade no segundo zero da partida.
##
## Em 27/08 a causa foi arrancada: a tabela deixou de ser metro-com-regua e virou
## FRACAO do raio do mapa. Nao existe mais numero para envelhecer. Este teste
## agora prova a fiacao (a zona le' LAND_R da ilha entregue) e o efeito (ninguem
## nasce fora), que sao as duas coisas que o defeito realmente rompeu.
func _test_raio_do_mapa() -> void:
	print("[A zona le' o tamanho do mapa da ILHA, sem regua]")
	var cena := "res://world/Island.tscn"
	if not ResourceLoader.exists(cena):
		_check(false, "Island.tscn existe para medir")
		return
	var ilha: Node3D = (load(cena) as PackedScene).instantiate()
	root.add_child(ilha)

	var r_mapa: float = _zona.raio_do_mapa(ilha)
	_check(is_equal_approx(r_mapa, float(ilha.LAND_R)),
		"o raio do mapa E' o LAND_R da ilha (%.0fm), nao metade do lado (%.0fm)"
		% [float(ilha.LAND_R), float(ilha.SIZE) * 0.5])
	_check(_zona.raio_do_mapa(null) == float(_zona.RAIO_MAPA_PADRAO),
		"sem ilha, cai no padrao — a zona roda sozinha em teste")

	## A PROVA DE QUE A FRACAO NAO ENVELHECE: uma ilha FALSA com o dobro do raio
	## tem que produzir circulos com o dobro do raio, sem tocar em constante
	## nenhuma. Era exatamente isto que a regua de 180 m nao conseguia fazer.
	var falsa_scr := GDScript.new()
	falsa_scr.source_code = "extends Node3D
const LAND_R := 264.0
const SIZE := 600.0
"
	falsa_scr.reload()
	var falsa: Node3D = falsa_scr.new()
	root.add_child(falsa)
	var p1: Array = _zona.plano(ilha, 1234)
	var p2: Array = _zona.plano(falsa, 1234)
	var dobrou := true
	for i in p1.size():
		if not is_equal_approx(float(p2[i].raio), float(p1[i].raio) * 2.0):
			dobrou = false
	_check(dobrou, "mapa com o DOBRO do raio = todos os circulos com o dobro do raio")
	falsa.free()

	# o que o defeito realmente causava: gente nascendo na tempestade
	var borda: float = r_mapa
	var fora := 0
	var total := 0
	for n in get_nodes_in_group("spawn"):
		if not (n is Node3D):
			continue
		total += 1
		var p: Vector3 = (n as Node3D).global_position
		if Vector2(p.x, p.z).length() > borda:
			fora += 1
	_check(total > 0, "ha' pontos de nascimento para conferir (%d)" % total)
	_check(fora == 0,
		"NENHUM nascimento cai fora da borda do mapa (%d de %d fora)" % [fora, total])
	ilha.queue_free()


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)


# --------------------------------------------------- 1. o desenho das fases

func _test_desenho() -> void:
	print("[Desenho: abertura + 5 fases cabem na partida e apertam de verdade]")
	var fases: Array = _zona.FASES
	var dur: float = float(_bal.MATCH.duration_s)
	var total: float = float(_zona.ABERTURA_S) + float(_zona.FORMACAO_S)
	for f in fases:
		total += float(f.espera) + float(f.fecha)
	_check(total < dur, "a tempestade INTEIRA cabe na partida (%.0fs de %.0fs)" % [total, dur])
	_check(dur - total >= 30.0,
			"e sobra tempo para a QUEDA, que corre no mesmo relogio (%.0fs)" % (dur - total))

	## OS TEMPOS QUE O DIRETOR MANDOU (27/08): 1:10 de abertura, depois 1:00, 50,
	## 40 e 30. Cravados como teste porque foram ordem, nao estimativa.
	_check(is_equal_approx(float(_zona.ABERTURA_S), 70.0),
			"a abertura e' 1:10 — o mapa fica INTEIRO aberto por 70s depois do pouso")
	var esperadas := [60.0, 50.0, 40.0, 30.0]
	var ok_janelas := true
	for i in esperadas.size():
		if not is_equal_approx(float(fases[i].espera), float(esperadas[i])):
			ok_janelas = false
	_check(ok_janelas, "as janelas seguem a ordem: 1:00, 50, 40, 30")

	## A JANELA ENCOLHE. E' o que faz a partida acelerar em vez de ter dois
	## ritmos (Fortnite: 2:30, 1:50, 1:30, 1:00, 45, 30, 20, 10 — ver
	## docs/referencias/ZONA-BATTLE-ROYALE.md).
	var ok_encolhe := true
	for i in range(1, esperadas.size()):
		if float(fases[i].espera) >= float(fases[i - 1].espera):
			ok_encolhe = false
	_check(ok_encolhe, "cada janela e' menor que a anterior: a partida ACELERA")

	_test_raio_do_mapa()

	var frac := 1.0
	var ok_frac := true
	var ok_dps := true
	var dps := 0.0
	for f in fases:
		if float(f.frac) >= frac:
			ok_frac = false
		frac = float(f.frac)
		if float(f.dps) <= dps:
			ok_dps = false
		dps = float(f.dps)
	_check(ok_frac, "cada fase encolhe: 1.00 -> .62 -> .42 -> .26 -> .13 -> 0 do mapa")
	_check(ok_dps, "o dano CRESCE a cada fase (%.1f -> %.0f dps)"
			% [float(fases[0].dps), dps])

	## O COLAPSO. Ordem do Diretor: "ate' todo o mapa ser tomado pela tempestade
	## de dano para forcar finalizar a batalha". E' o que os tres referenciais
	## fazem (PUBG fase 9 -> 0 m, Apex ring 6 -> 0,05 m, Fortnite -> 1 jogador).
	## Sem isso a partida podia terminar por CRONOMETRO com dois vivos.
	_check(is_equal_approx(float(fases[-1].frac), 0.0),
			"a ULTIMA fase fecha em ZERO: o mapa inteiro vira tempestade")
	var mortal: float = float(fases[-1].dps) * float(fases[-1].fecha)
	_check(mortal > float(_bal.PLAYER.hp),
			"o colapso resolve a partida: %.0f dps x %.0fs = %.0f > %.0f de vida"
			% [float(fases[-1].dps), float(fases[-1].fecha), mortal, float(_bal.PLAYER.hp)])

	# ANTI-BUG §2.3: o tique tem que ser grosso o bastante para nunca dar dano 0
	var tick: float = float(_zona.TICK_S)
	_check(tick >= 0.25, "o dano tem tique proprio de %.2fs (nao e' por frame)" % tick)
	_check(float(fases[0].dps) * tick >= 1.0,
			"o MENOR tique da zona ja' vale %.1f de dano — nunca int(0)"
			% (float(fases[0].dps) * tick))


# --------------------------------------------- 1b. NA QUEDA NAO HA' LIMITE

## A ORDEM DO DIRETOR, 27/08: "mapa todo aberto ate' que todos descam no mapa,
## contamos um cronometro de 1:10 para comecar o circulo se formar em torno do
## mapa, ele para, cronometro de mais 1:00...".
##
## Antes a zona nascia LIGADA no _ready: enquanto o mago ainda estava no castelo,
## a fase 1 ja' estava contando, e quem pousasse tarde pousava com a parede em
## movimento. E' a mesma classe de defeito da suprema que carregava no ar
## (corrigida em 26/08): a partida contando antes do pe' no chao.
##
## VERMELHO PROVADO: chamando `_avisar()` no _ready, ou tirando o guarda de
## `iniciar()`, este bloco cai.
func _test_abertura() -> void:
	print("[Na queda nao ha' limite: 1:10 de mapa aberto antes da tempestade]")
	var mapa := Node3D.new()
	root.add_child(mapa)
	_aberturas.clear()
	_formacoes.clear()
	_avisos.clear()
	var z: Node3D = _zona.criar(mapa, null)

	# --- INERTE: a zona existe e nao faz nada
	_check(not z.ativa(), "recem-criada a zona NAO esta' ativa")
	_check(not z._parede.visible, "a parede esta' INVISIVEL — nao ha' tempestade no ceu")
	_check(z._timer.is_stopped(), "nenhum cronometro correndo antes do pouso")
	_check(is_equal_approx(float(z.dps_atual()), 0.0), "e nao doi")
	_check(_avisos.is_empty(), "nenhum circulo foi anunciado ainda")
	_check(is_equal_approx(float(z.raio), float(z._raio_da_borda())),
			"o raio nasce na BORDA do mapa: `dentro()` responde SIM para todo mundo")
	_check(z.dentro(Vector3(float(z._raio_da_borda()) - 1.0, 0, 0)),
			"quem esta' na beira do mapa durante a queda esta' DENTRO")

	# --- POUSOU: comeca o 1:10
	z.iniciar()
	_check(_aberturas.size() == 1 and is_equal_approx(float(_aberturas[0]), 70.0),
			"o pouso abre o cronometro de 1:10 (Bus.zona_abertura)")
	_check(is_equal_approx(float(z._timer.wait_time), 70.0), "relogio armado nos 70s")
	_check(not z.ativa(), "durante a abertura a tempestade AINDA nao existe")
	_check(_avisos.is_empty(), "e nenhum circulo e' anunciado durante a abertura")
	_check(is_equal_approx(float(z.restante()), float(z._timer.time_left)),
			"restante() entrega o cronometro para a HUD sem espelhar Timer")

	z.iniciar()
	_check(_aberturas.size() == 1, "iniciar() de novo NAO reinicia (idempotente)")

	# --- FORMA: a parede aparece em torno do mapa e PARA
	z._timer.timeout.emit()
	_check(_formacoes.size() == 1, "acabado o 1:10, a tempestade SE FORMA")
	_check(z._parede.visible, "agora a parede e' visivel")
	_check(z.ativa(), "e a tempestade passa a existir")
	_check(is_equal_approx(float(_formacoes[0][0]), float(z._raio_da_borda())),
			"ela para na BORDA do mapa (%.0fm) — nao tira chao de ninguem"
			% float(z._raio_da_borda()))
	_check(is_equal_approx(float(z.dps_atual()), 0.0),
			"formar NAO doi: e' anuncio, nao punicao")
	_check(_avisos.is_empty(), "durante a formacao o 1o circulo ainda nao foi sorteado")

	# --- PARA, e SO' ENTAO comeca a janela de 1:00
	z._timer.timeout.emit()
	_check(_avisos.size() == 1, "formada e parada, o 1o circulo e' anunciado")
	_check(is_equal_approx(float(_avisos[0][3]), float(_zona.FASES[0].espera)),
			"e a janela e' a da fase 1 (%.0fs)" % float(_zona.FASES[0].espera))
	_check(int(z.fase) == 0, "a fase 1 ainda nao FECHOU — a parede esta' parada")
	mapa.queue_free()


# ------------------------------- 1c. CADA PARTIDA, CIRCULOS DIFERENTES

## A ORDEM DO DIRETOR, 27/08: "a ideia de nao ser fixo o fechamento dos circulos
## e' para ninguem montar estrategias para ficar fixo no mesmo lugar e sobreviver
## sempre como finalistas".
##
## O DEFEITO QUE ISTO MATA: SEED_ZONA era fixo em 2707. Toda partida que o jogo
## jamais rodou teve os MESMOS cinco circulos nos MESMOS lugares. Decorar o mapa
## uma vez ganhava para sempre — e num battle royale isso nao e' desequilibrio,
## e' o fim do genero.
##
## O determinismo NAO se perde: ele mudou de escopo. Era determinismo ENTRE
## partidas; agora e' DENTRO da partida (um seed gera o plano inteiro de uma
## vez), que e' o unico escopo de que o teste e a rede precisam.
##
## VERMELHO PROVADO: voltando `p_seed := SEED_ZONA` na assinatura de `criar`,
## as duas primeiras linhas daqui caem.
func _test_sorteio_por_partida() -> void:
	print("[Cada partida, circulos diferentes: ninguem decora o mapa]")
	var mapa := Node3D.new()
	root.add_child(mapa)
	var seeds := {}
	var centros := {}
	for _i in 12:
		var z: Node3D = _zona.criar(mapa, null)
		seeds[int(z.seed_da_partida)] = true
		var chave := ""
		for e in z._plano:
			chave += "%.1f,%.1f;" % [(e.centro as Vector3).x, (e.centro as Vector3).z]
		centros[chave] = true
		z.free()
	_check(seeds.size() >= 11,
			"12 partidas sortearam %d seeds distintos — o seed NAO e' fixo" % seeds.size())
	_check(centros.size() >= 11,
			"e %d sequencias de circulos distintas: nao da' para decorar" % centros.size())

	## Mas passando o seed a mao a sequencia se REPETE — e' o que o teste e a
	## rede precisam (o servidor sorteia, manda o numero, todos desenham igual).
	var za: Node3D = _zona.criar(mapa, null, 4242)
	var zb: Node3D = _zona.criar(mapa, null, 4242)
	var iguais := true
	for i in (za._plano as Array).size():
		if (za._plano[i].centro as Vector3).distance_to(zb._plano[i].centro) > 0.0001:
			iguais = false
	_check(int(za.seed_da_partida) == 4242, "o seed da partida fica GUARDADO (rede)")
	_check(iguais, "MESMO seed passado a mao = MESMA sequencia (determinismo na partida)")
	za.free()
	zb.free()
	mapa.queue_free()


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
	var pr: float = float(_zona.RAIO_MAPA_PADRAO)
	for e in a:
		if (e.centro as Vector3).distance_to(pc) + float(e.raio) > pr + 0.001:
			contido = false
		pc = e.centro
		pr = float(e.raio)
	_check(contido, "todo circulo novo esta' CONTIDO no anterior")

	## E nenhum circulo pode passar da borda do mapa.
	var borda: float = float(_zona.RAIO_MAPA_PADRAO)
	var dentro_ilha := true
	for e in a:
		if Vector2((e.centro as Vector3).x, (e.centro as Vector3).z).length() + float(e.raio) > borda + 0.001:
			dentro_ilha = false
	_check(dentro_ilha, "nenhum circulo passa da borda do mapa (%.0fm)" % borda)


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
	_check(is_equal_approx(float(z.raio), float(z._raio_da_borda())), "nasce na borda do mapa")
	_check(is_equal_approx(float(z.dps_atual()), 0.0),
			"antes da 1a fase a zona NAO doi (a partida abre com loot, nao sangrando)")
	## ATRAVESSA A ABERTURA: pouso -> 1:10 -> formacao -> 1o aviso. Coberto em
	## detalhe por _test_abertura; aqui e' so' o caminho ate' a fase 1.
	_avisos.clear()
	z.iniciar()
	z._timer.timeout.emit()   # fim do 1:10 -> a tempestade se forma
	z._timer.timeout.emit()   # formada e parada -> anuncia o 1o circulo
	_check(_avisos.size() == 1, "Bus.zona_avisou publica o 1o circulo DEPOIS da abertura")
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
		var r_alvo: float = float(z._plano[i].raio)
		_check(is_equal_approx(float(z.raio), r_alvo),
				"fase %d: o raio parou EXATO em %.1fm (%.0f%% do mapa)"
				% [i + 1, r_alvo, float(f.frac) * 100.0])
		_check(is_equal_approx(float(z.dps_atual()), float(f.dps)),
				"fase %d: o dano da fase e' %.1f dps" % [i + 1, float(f.dps)])
		if i + 1 < fases.size():
			_check(_avisos.size() == avisos_antes + 1,
					"fase %d: o PROXIMO circulo ja' e' publico (telegrafia)" % (i + 1))
			_check(is_equal_approx(float(_avisos[-1][2]), float(z._plano[i + 1].raio)),
					"fase %d: o aviso leva o raio do circulo seguinte" % (i + 1))
		else:
			_check(_avisos.size() == avisos_antes,
					"ultima fase: a tempestade nao avanca mais (nao ha' proximo circulo)")

	## Visual: procedural, zero binario (regra do projeto).
	_check(z._parede.mesh is CylinderMesh, "a parede e' malha PROCEDURAL (zero binario)")
	_check(is_equal_approx(z._parede.scale.x, maxf(float(z.raio), 0.05)),
			"a parede acompanha o raio pelo setter (sem _process)")
	## O COLAPSO CHEGOU A ZERO e a parede nao degenerou.
	_check(is_equal_approx(float(z.raio), 0.0),
			"depois da ultima fase o raio e' ZERO: o mapa inteiro e' tempestade")
	_check(z._parede.scale.x > 0.0, "e a escala da parede nao virou zero (malha valida)")
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
