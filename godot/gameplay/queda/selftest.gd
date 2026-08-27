## Selftest headless da raia A QUEDA. Rodar:
##   Godot --headless --path godot --script res://gameplay/queda/selftest.gd
##
## Prova, em ordem: a rota do castelo e' deterministica, cruza o mapa de fora a
## fora e ESCALA com o tamanho do terreno (nada de 180 cravado); o castelo
## publica a rota no Bus e nao gasta um frame de GDScript; as quatro fases saem
## na ordem do contrato; o mergulho ganha distancia e o planeio desce ~4x mais
## devagar; o pouso acontece na ALTURA DO TERRENO consultada no ponto de
## chegada (a ilha e' procedural — quem supoe y=0 enterra o mago); NENHUMA
## MAGIA sai durante a queda e nenhuma vaza no primeiro frame do chao; o
## controle volta inteiro ao jogador; e depois de pousar a maquina custa zero.
##
## NOTA: em modo --script os autoloads so' registram DEPOIS que este arquivo
## compila — por isso aqui NADA e' referenciado lexicalmente (Bus, Balance,
## Queda, Castelo, Player...): tudo chega via load()/get_node() no 1o frame.
extends SceneTree

const DT := 1.0 / 60.0

var fails := 0
var _bus: Node
var _bal: Node
var _queda: GDScript
var _castelo: GDScript
var _player_scr: GDScript
var _fases: Array = []       # queda_fase
var _alturas: Array = []     # queda_altura
var _rotas: Array = []       # castelo_rota
var _casts: Array = []       # spell_cast — tem que ficar VAZIO no ar
## Blocos que chegaram ao FIM. Erro de script no meio de um teste aborta a
## funcao inteira sem contar falha — e o selftest passaria em verde com metade
## das checagens nunca executadas (aconteceu na 1a rodada desta raia). Cada
## bloco se assina no fim; a assinatura que faltar VIRA FALHA.
var _blocos: Array = []
const BLOCOS := ["contrato", "rota", "castelo", "camera", "fases", "pouso", "sem_magia", "bots"]


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	_bus = root.get_node("Bus")
	_bal = root.get_node("Balance")
	_queda = load("res://gameplay/queda/Queda.gd")
	_castelo = load("res://world/Castelo.gd")
	_player_scr = load("res://gameplay/Player.gd")
	_bus.queda_fase.connect(func(f: String) -> void: _fases.append(f))
	_bus.queda_altura.connect(func(m: float, v: float) -> void: _alturas.append([m, v]))
	_bus.castelo_rota.connect(func(i: Vector3, f: Vector3, d: float) -> void:
		_rotas.append([i, f, d]))
	_bus.spell_cast.connect(func(el: String) -> void: _casts.append(el))
	## Script que nao compila devolve null aqui e TODA checagem abaixo viraria
	## erro silencioso. Sem os dois arquivos da raia nao ha' o que testar.
	_check(_queda != null, "gameplay/queda/Queda.gd compila")
	_check(_castelo != null, "world/Castelo.gd compila")
	if _queda == null or _castelo == null:
		printerr("RESULTADO: %d falha(s)" % maxi(fails, 1))
		quit(1)
		return

	_test_contrato()
	_test_rota()
	_test_castelo()
	_test_camera_castelo()
	_test_fases()
	_test_pouso_no_terreno()
	_test_sem_magia()
	_test_bots()
	print("[Cobertura]")
	for b in BLOCOS:
		_check(b in _blocos, "o bloco '%s' rodou ate' o fim" % b)
	print("")
	if fails == 0:
		print("RESULTADO: OK — 0 falhas")
	else:
		printerr("RESULTADO: %d falha(s)" % fails)
	quit(0 if fails == 0 else 1)


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)


## Ilha de mentira: SO' o contrato que a queda le' (a constante SIZE e o
## height()). Sem tocar em world/Island.gd, que outra raia esta' reescrevendo.
func _ilha(lado: float, corpo: String) -> Node3D:
	var scr := GDScript.new()
	scr.source_code = ("extends Node3D\nconst SIZE := %f\n" % lado) \
			+ "func height(x: float, z: float) -> float:\n\t" + corpo + "\n"
	scr.reload()
	var n := Node3D.new()
	n.set_script(scr)
	return n


## UM FRAME DE FISICA COMO O ENGINE FARIA: ele chama `_physics_process` SO' de
## quem esta' com o passo ligado. Reproduzir essa regra aqui e' o que torna o
## bloqueio de magia testavel — se a queda deixar de desligar o passo do Player,
## e' neste laco que o tiro sai.
func _frame(q: Node, p: Node) -> void:
	if p != null and is_instance_valid(p) and p.is_physics_processing():
		p._physics_process(DT)
	if q.is_physics_processing():
		q._physics_process(DT)


func _passo(q: Node, n: int) -> void:
	for _i in n:
		if not q.is_physics_processing():
			return
		_frame(q, q._player)


## A ABERTURA E' O CASTELO (ordem do Diretor, 26/08): "ele deve aparecer no
## ceu, voando, como uma ilha flutuante — e nao a imagem de como vemos ele por
## dentro; a camera fica mais distante". No trajeto o pivô larga o ombro e vai
## ao coracao do castelo com o braco recuado; no salto tudo volta ao ombro.
## Vermelho provado removendo o desvio do pivô e o recuo do braco.
func _test_camera_castelo() -> void:
	print("[Camera: a abertura enquadra o castelo de FORA, voando]")
	var arena := Node3D.new()
	root.add_child(arena)
	var ilha := _ilha(300.0, "return 0.0")
	arena.add_child(ilha)
	var p: CharacterBody3D = _player_scr.new()
	arena.add_child(p)
	var arm: SpringArm3D = p.cam_pitch.get_child(0) as SpringArm3D
	var len0 := arm.spring_length
	var mask0 := arm.collision_mask
	var q: Node = _queda.iniciar(arena, ilha, p)
	_frame(q, p)
	var pivo: Node3D = p.cam_yaw
	var castelo_pos: Vector3 = (q.castelo as Node3D).global_position
	# O mago viaja COLADO no castelo (PORTAO), entao "perto do castelo" seria
	# checagem vacua — o contrato e' LARGAR O OMBRO e subir ao coracao da rocha.
	_check(pivo.global_position.distance_to(
			p.global_position + Vector3(0, 1.85, 0)) > 5.0
			and pivo.global_position.y > castelo_pos.y,
			"no trajeto o pivô LARGA o ombro e sobe ao coracao do castelo")
	_check(arm.spring_length >= 60.0,
			"o braco recua para o plano geral (%.0fm)" % arm.spring_length)
	_check(arm.collision_mask == 0,
			"no ceu o braco nao colide (a propria rocha o encolheria)")
	# o salto devolve a camera ao ombro do mago, exatamente como era
	q.saltar()
	_frame(q, p)
	_check(is_equal_approx(arm.spring_length, len0),
			"no salto o braco volta ao ombro (%.2f)" % len0)
	_check(arm.collision_mask == mask0, "e a mascara de colisao volta")
	_check(pivo.global_position.distance_to(
			p.global_position + Vector3(0, 1.85, 0)) < 0.5,
			"no salto o pivô volta ao mago")
	arena.free()
	_blocos.append("camera")


## OS BOTS CAEM JUNTO — pela MESMA lei do jogador.
## Ate' 25/08/2026 os seis nasciam no chao enquanto o jogador caia do castelo:
## a partida abria com o jogador no ar e o mapa ja' povoado.
func _test_bots() -> void:
	print("[Os bots caem do castelo, sob a mesma lei]")
	var ilha := _ilha(300.0, "return 4.0")
	root.add_child(ilha)
	var arena := Node3D.new()
	root.add_child(arena)

	var player: Node3D = _player_scr.new()
	arena.add_child(player)
	var q: Node = _queda.iniciar(arena, ilha, player)
	_check(q != null, "a queda do jogador montou")

	var bots: Array = []
	for i in 6:
		var b := CharacterBody3D.new()
		b.set_script(_player_scr)
		arena.add_child(b)
		bots.append(b)
	var quedas: Array = _queda.iniciar_bots(bots, ilha, q.castelo)
	_check(quedas.size() == 6, "os 6 bots receberam queda (%d)" % quedas.size())

	var no_ar := 0
	for qb in quedas:
		if str(qb.fase) == "no_castelo":
			no_ar += 1
	_check(no_ar == 6, "os 6 comecam NO AR, nao no chao (%d)" % no_ar)

	var travados := 0
	for b in bots:
		if not b.is_physics_processing():
			travados += 1
	_check(travados == 6,
		"os 6 estao com o passo de fisica DESLIGADO: e' o que barra magia no ar (%d)" % travados)

	_casts.clear()
	## Roda a partida inteira: cada bot viaja, salta na hora sorteada, cai e pousa.
	for _i in 3600:
		for qb in quedas:
			if qb.is_physics_processing():
				var alvo: Node = qb._player
				if alvo != null and is_instance_valid(alvo) and alvo.is_physics_processing():
					alvo._physics_process(DT)
				qb._physics_process(DT)

	var pousados := 0
	var fora_do_chao := 0
	for i in quedas.size():
		var qb: Node = quedas[i]
		if str(qb.fase) == "pousou":
			pousados += 1
			var pos: Vector3 = (bots[i] as Node3D).global_position
			if absf(pos.y - 4.0) > 0.2:
				fora_do_chao += 1
	_check(pousados == 6, "os 6 POUSARAM (%d)" % pousados)
	_check(fora_do_chao == 0, "os 6 pousaram NO TERRENO (%d fora)" % fora_do_chao)
	_check(_casts.is_empty(),
		"NENHUM bot conjurou durante a queda (%d disparos)" % _casts.size())

	var religados := 0
	for b in bots:
		if b.is_physics_processing():
			religados += 1
	_check(religados == 6, "os 6 tiveram o passo de fisica DEVOLVIDO ao pousar (%d)" % religados)

	## DETERMINISMO: o mesmo seed sorteia os mesmos instantes de salto. Sem isto
	## dois testes do mesmo cenario dariam pousos diferentes e o portao viraria
	## moeda.
	var t1: Array = []
	for qb in quedas:
		t1.append(qb._t_salto)
	var bots2: Array = []
	for i in 6:
		var b2 := CharacterBody3D.new()
		b2.set_script(_player_scr)
		arena.add_child(b2)
		bots2.append(b2)
	var quedas2: Array = _queda.iniciar_bots(bots2, ilha, q.castelo)
	var iguais := 0
	for i in quedas2.size():
		if is_equal_approx(float(quedas2[i]._t_salto), float(t1[i])):
			iguais += 1
	_check(iguais == 6, "mesmo seed, mesmos instantes de salto (%d/6)" % iguais)

	arena.queue_free()
	ilha.queue_free()
	_blocos.append("bots")


# ------------------------------------------------------- 1. o contrato do Bus

func _test_contrato() -> void:
	print("[Contrato: os tres sinais que UI e audio observam]")
	_check(_bus.has_signal("queda_fase"), "Bus.queda_fase existe")
	_check(_bus.has_signal("queda_altura"), "Bus.queda_altura existe")
	_check(_bus.has_signal("castelo_rota"), "Bus.castelo_rota existe")
	## A FIACAO DEFENSIVA que o Main vai escrever: se esta raia sumir, o
	## `ResourceLoader.exists` devolve false e a partida boota sem queda.
	_check(ResourceLoader.exists("res://gameplay/queda/Queda.gd"),
			"o caminho que o Main testa antes de carregar existe")
	_blocos.append("contrato")


# --------------------------------------------------- 2. a rota (pura, sem mundo)

func _test_rota() -> void:
	print("[Rota: deterministica, cruza o mapa inteiro e ESCALA com o terreno]")
	var lado := 180.0
	var a: Dictionary = _castelo.rota(lado)
	var b: Dictionary = _castelo.rota(lado)
	_check((a.inicio as Vector3).is_equal_approx(b.inicio)
			and (a.fim as Vector3).is_equal_approx(b.fim),
			"MESMO SEED = mesma rota (nada sorteado na hora)")
	var c: Dictionary = _castelo.rota(lado, 9191)
	_check((a.inicio as Vector3).distance_to(c.inicio) > 1.0,
			"seed diferente = rota diferente")

	var ini: Vector3 = a.inicio
	var fim: Vector3 = a.fim
	var alt: float = float(_castelo.ALTURA)
	_check(is_equal_approx(ini.y, alt) and is_equal_approx(fim.y, alt),
			"o castelo voa a %.0fm nas duas pontas" % alt)
	_check(Vector2(ini.x, ini.z).length() > lado * 0.5
			and Vector2(fim.x, fim.z).length() > lado * 0.5,
			"as duas pontas caem FORA da ilha: o castelo entra e sai do mapa")
	# distancia HORIZONTAL do centro da ilha ate' a reta da rota (a altura nao
	# entra): e' o que garante que a rota corta o mapa em vez de raspar a borda.
	var i2 := Vector2(ini.x, ini.z)
	var f2 := Vector2(fim.x, fim.z)
	var dir := (f2 - i2).normalized()
	var perp: float = absf((-i2).cross(dir))
	_check(perp <= lado * float(_castelo.DESVIO) + 0.001,
			"a rota passa a %.0fm do centro (<= %.0fm de desvio): corta o mapa"
			% [perp, lado * float(_castelo.DESVIO)])

	## O ANTI-CRAVAMENTO: a ilha VAI CRESCER nesta fase. Rota de um mapa maior
	## tem que ser maior — se alguem cravar 180 aqui, este teste cai.
	var grande: Dictionary = _castelo.rota(300.0)
	_check((grande.inicio as Vector3).distance_to(grande.fim)
			> (ini.distance_to(fim)) * 1.5,
			"mapa de 300m gera rota bem maior que a de 180m (nada cravado)")
	_check(is_equal_approx(float(grande.duracao), float(a.duracao)),
			"a DURACAO nao muda com o mapa: a janela de decisao e' a mesma")

	## De onde sai o tamanho do mapa: da constante SIZE de quem esta' no mapa.
	var ilha := _ilha(300.0, "return 0.0")
	_check(is_equal_approx(float(_castelo.lado(ilha)), 300.0),
			"lado() LE' Island.SIZE do no' da ilha (300m no dublê)")
	_check(is_equal_approx(float(_castelo.lado(null)), float(_castelo.LADO_PADRAO)),
			"sem ilha cai no LADO_PADRAO (fiacao defensiva)")
	var pelado := Node3D.new()
	_check(float(_castelo.lado(pelado)) == float(_castelo.LADO_PADRAO),
			"no' sem SIZE tambem cai no padrao, sem quebrar")
	pelado.free()
	ilha.free()

	## A abertura tem que caber na partida com folga pra jogar.
	var dur: float = float(a.duracao)
	_check(dur > 0.0 and dur < float(_bal.MATCH.duration_s) * 0.25,
			"a rota (%.0fs) cabe folgada nos %.0fs de partida"
			% [dur, float(_bal.MATCH.duration_s)])
	_blocos.append("rota")


# ------------------------------------------------------- 3. o castelo no mundo

func _test_castelo() -> void:
	print("[Castelo: publica a rota, nasce no comeco dela e nao gasta frame]")
	var arena := Node3D.new()
	root.add_child(arena)
	var ilha := _ilha(240.0, "return 3.0")
	_rotas.clear()
	var cast: Node3D = _castelo.criar(arena, ilha)
	_check(_rotas.size() == 1, "Bus.castelo_rota sai UMA vez (a HUD desenha a linha)")
	var esperada: Dictionary = _castelo.rota(240.0)
	_check((_rotas[0][0] as Vector3).is_equal_approx(esperada.inicio),
			"o sinal leva a rota do mapa DESTA partida (lado 240 do dublê)")
	_check(cast.global_position.is_equal_approx(esperada.inicio),
			"o castelo nasce no comeco da rota")
	_check(not cast.is_processing() and not cast.is_physics_processing(),
			"o castelo NAO gasta _process (a travessia e' Tween, engine-side)")
	# A LEI MUDOU em 26/08 (DIRECAO §10): o castelo de jogo e' o modelo do
	# Meshy decimado ("Aetherstone Citadel", 30k tris). Este teste cobrava
	# "zero binario" — que era a lei ate' os concepts serem aprovados. Agora:
	# com o arquivo presente, o filho e' ModeloCastelo; o PROCEDURAL continua
	# vivo como fallback e e' provado logo abaixo, chamando-o direto.
	var malhas := 0
	var pilha: Array = [cast]
	while not pilha.is_empty():
		var f0: Node = pilha.pop_back()
		for c0 in f0.get_children():
			pilha.append(c0)
		if f0 is MeshInstance3D:
			malhas += 1
	if ResourceLoader.exists(str(cast.MODELO)):
		_check(cast.get_node_or_null("ModeloCastelo") != null,
				"com o .glb no lugar, o castelo VESTE o modelo do Meshy")
		_check(malhas >= 1, "o modelo traz malha de verdade (%d)" % malhas)
	else:
		_check(malhas >= 4 and malhas <= 12,
				"sem o .glb, a silhueta procedural segura (%d malhas)" % malhas)
	# o fallback NUNCA pode morrer: um castelo cru monta as primitivas mesmo
	# com o modelo existindo no projeto.
	var cru: Node3D = _castelo.new()
	arena.add_child(cru)
	cru._montar_fallback()
	var malhas_fb := 0
	for f1 in cru.get_children():
		if f1 is MeshInstance3D:
			malhas_fb += 1
	_check(malhas_fb >= 4, "o fallback procedural continua vivo (%d malhas)" % malhas_fb)
	cru.free()
	ilha.free()
	arena.queue_free()
	_blocos.append("castelo")


# ------------------------------------------------------- 4. as quatro fases

func _test_fases() -> void:
	print("[Fases: no_castelo -> caindo -> planando -> pousou, na ordem]")
	var arena := Node3D.new()
	root.add_child(arena)
	var ilha := _ilha(180.0, "return 12.0")   # planalto de 12m: pouso NAO e' y=0
	arena.add_child(ilha)
	var p: CharacterBody3D = _player_scr.new()
	arena.add_child(p)
	_fases.clear()
	_alturas.clear()
	var q: Node = _queda.iniciar(arena, ilha, p)

	_check(q != null and str(q.fase) == "no_castelo", "nasce em no_castelo")
	_check(_fases == ["no_castelo"], "e ja' publica a fase no Bus")
	_check(not p.is_physics_processing(),
			"o Player para de andar sozinho: quem conduz o corpo e' a queda")
	# O CASTELO SEM BONECOS (decisao no 14 — DIRECAO.md §2). Provado em
	# vermelho apagando o visible=false do _ready: os dois checks caem.
	_check(not p.visible, "no castelo o corpo e' INVISIVEL — o ceu mostra um objeto, nao gente")

	## 1. NO CASTELO — colado no castelo, e um toque salta.
	## No FRAME ZERO, antes de qualquer passo: senao o mago passa o primeiro
	## frame no spawn (no chao) e um salto imediato pousaria na hora.
	_check(p.global_position.is_equal_approx(
			q.castelo.global_position + Vector3(q.PORTAO)),
			"ja' nasce pendurado no castelo, sem esperar um frame")
	q.castelo.global_position = Vector3(10, 320, -20)
	_passo(q, 1)
	_check(p.global_position.is_equal_approx(Vector3(10, 320, -20) + Vector3(q.PORTAO)),
			"o mago viaja pendurado no portao do castelo")
	_check(str(q.fase) == "no_castelo", "olhar e esperar NAO salta")
	_check(not p.visible, "viajando, continua invisivel")
	q.saltar()
	_check(p.visible, "ao ACIONAR o salto o mago SURGE (do nada, e nao e' problema — o Diretor)")
	# devolve o MUNDO INTEIRO ao estado de antes do salto de prova: a fase, a
	# visibilidade E o rastro no Bus — saltar() publicou "caindo" e sem apagar
	# esse eco as checagens de ordem-das-fases la' embaixo contam fase a mais.
	q.fase = "no_castelo"
	p.visible = false
	_fases.resize(1)
	p.request_fire()                       # o toque que a HUD ja' tem hoje
	_passo(q, 1)
	_check(str(q.fase) == "caindo", "UM TOQUE SALTA")
	_check(not bool(p.get("_want_fire")),
			"o toque do salto e' consumido: nao vira disparo depois")
	_check(_fases == ["no_castelo", "caindo"], "a fase nova saiu no Bus, uma vez")

	## 2. CAINDO — acelera ate' a terminal, nao teleporta pra ela.
	var y0 := p.global_position.y
	_passo(q, 1)
	var v1 := (y0 - p.global_position.y) / DT
	_check(v1 > 0.0 and v1 < float(q.VEL_QUEDA) * 0.5,
			"o 1o frame NAO ja' cai a %.0f m/s (tem rampa: %.1f m/s)"
			% [float(q.VEL_QUEDA), v1])
	_passo(q, 180)                          # 3s: passou da rampa
	var y1 := p.global_position.y
	_passo(q, 60)
	var v_queda := (y1 - p.global_position.y)
	_check(absf(v_queda - float(q.VEL_QUEDA)) < 1.0,
			"a queda livre estabiliza em %.0f m/s (medido %.1f)"
			% [float(q.VEL_QUEDA), v_queda])

	## 3. PLANANDO — a troca acontece na ALTURA, e desce muito mais devagar.
	var alt_troca := -1.0
	while str(q.fase) == "caindo" and q.is_physics_processing():
		q._physics_process(DT)
		if str(q.fase) == "planando":
			alt_troca = float(q.altura())
	_check(str(q.fase) == "planando", "abaixo do limiar o corpo acha o eixo")
	_check(alt_troca <= float(q.ALTURA_PLANEIO)
			and alt_troca > float(q.ALTURA_PLANEIO) - float(q.VEL_QUEDA) * DT * 2.0,
			"a virada acontece a %.1fm do CHAO (limiar %.0fm)"
			% [alt_troca, float(q.ALTURA_PLANEIO)])
	_check(_fases == ["no_castelo", "caindo", "planando"], "planando saiu no Bus")
	_passo(q, 45)                            # passa o freio
	var y2 := p.global_position.y
	_passo(q, 30)
	var v_planeio := (y2 - p.global_position.y) * 2.0
	_check(absf(v_planeio - float(q.VEL_PLANEIO)) < 1.0,
			"planando desce %.0f m/s (medido %.1f)" % [float(q.VEL_PLANEIO), v_planeio])
	_check(v_planeio < v_queda * 0.5,
			"o planeio e' MUITO mais lento que a queda (%.0fx)" % (v_queda / v_planeio))

	## 4. POUSOU — o controle volta inteiro.
	_passo(q, 600)
	_check(str(q.fase) == "pousou", "toca o chao e a queda acaba")
	_check(_fases == ["no_castelo", "caindo", "planando", "pousou"],
			"as 4 fases do contrato sairam na ordem, uma vez cada")
	_check(is_equal_approx(p.global_position.y, 12.0),
			"pousou EM CIMA do terreno de 12m (y=%.2f), nao no nivel do mar"
			% p.global_position.y)
	_check(p.is_physics_processing(), "o Player volta a andar por conta propria")
	_check(p.velocity.is_equal_approx(Vector3.ZERO), "chega no chao parado, sem herdar queda")
	_check(not q.is_physics_processing(),
			"depois do pouso a maquina da queda custa ZERO por frame")

	## O ALTIMETRO: fluido pra HUD, mas nem de longe um sinal por frame.
	_check(_alturas.size() > 5, "o altimetro alimentou a HUD durante a queda")
	_check(_alturas.size() < 900 / 6,
			"e sai a ~%.0f Hz, nao por frame (%d sinais)"
			% [float(q.HZ_ALTIMETRO), _alturas.size()])
	_check(float(_alturas[-1][0]) == 0.0 and float(_alturas[-1][1]) == 0.0,
			"o ultimo sinal zera o altimetro no pouso")
	var so_positivo := true
	for a in _alturas:
		if float(a[0]) < 0.0:
			so_positivo = false
	_check(so_positivo, "o altimetro nunca reporta altura negativa")
	arena.queue_free()
	_blocos.append("fases")


# ------------------------------------- 5. o pouso consulta o terreno DE VERDADE

func _test_pouso_no_terreno() -> void:
	print("[Pouso: height(x,z) do ponto de CHEGADA — a ilha e' procedural]")
	## Rampa: o chao sobe 0,2m por metro de X. Se o pouso usasse a altura do
	## ponto de SALTO (ou zero), o mago terminaria enterrado ou boiando.
	var arena := Node3D.new()
	root.add_child(arena)
	var ilha := _ilha(180.0, "return x * 0.2")
	arena.add_child(ilha)
	var p: CharacterBody3D = _player_scr.new()
	arena.add_child(p)
	var q: Node = _queda.iniciar(arena, ilha, p)
	q.castelo.global_position = Vector3(0, 300, 0)
	_passo(q, 1)
	q.saltar()
	p.move_input = Vector2(1, 0)     # deriva no +X: o chao sobe enquanto ele cai
	_passo(q, 3000)
	_check(str(q.fase) == "pousou", "pousou na rampa")
	_check(p.global_position.x > 40.0,
			"o joystick levou o mago %.0fm de lado no ar (da' pra escolher onde cair)"
			% p.global_position.x)
	_check(absf(p.global_position.y - p.global_position.x * 0.2) < 0.2,
			"y do pouso = height(x,z) EM x=%.0f (y=%.1f, terreno=%.1f)"
			% [p.global_position.x, p.global_position.y, p.global_position.x * 0.2])

	## FORA DO QUADRADO DO TERRENO NAO HA' COLISAO: quem pousa la' cai para
	## sempre no instante em que a fisica volta. O dedo ficou colado no +X a
	## queda inteira; o pouso tem que parar na borda do mapa (180/2 = 90m).
	_check(p.global_position.x <= 90.0 + 0.001,
			"o pouso nao passa da borda do terreno (x=%.1f, limite 90m)"
			% p.global_position.x)

	## Sem dedo no joystick, cai reto — o deslocamento e' DECISAO, nao deriva.
	var p2: CharacterBody3D = _player_scr.new()
	arena.add_child(p2)
	var q2: Node = _queda.iniciar(arena, ilha, p2)
	q2.castelo.global_position = Vector3(0, 300, 0)
	_passo(q2, 1)
	q2.saltar()
	_passo(q2, 3000)
	_check(Vector2(p2.global_position.x, p2.global_position.z).length() < 0.01,
			"sem input o mago cai a prumo (nenhuma deriva de brinde)")

	## ALCANCE PURO, num mapa grande demais para a trava importar: e' o numero
	## que decide se a rota do castelo significa alguma coisa. Se a queda so'
	## alcancasse 40m, saltar cedo ou tarde daria no mesmo.
	var largo := _ilha(2000.0, "return 0.0")
	arena.add_child(largo)
	var p3: CharacterBody3D = _player_scr.new()
	arena.add_child(p3)
	var q3: Node = _queda.iniciar(arena, largo, p3)
	q3.castelo.global_position = Vector3(0, 320, 0)
	_passo(q3, 1)
	q3.saltar()
	p3.move_input = Vector2(1, 0)
	_passo(q3, 3000)
	var alcance: float = Vector2(p3.global_position.x, p3.global_position.z).length()
	_check(alcance >= 150.0,
			"de %.0fm de altura a queda cobre %.0fm no horizontal (>= 150m)"
			% [float(_castelo.ALTURA), alcance])
	arena.queue_free()
	_blocos.append("pouso")


# ------------------------------------------------ 6. NENHUMA MAGIA NO AR

func _test_sem_magia() -> void:
	print("[Sem magia no ar: ordem do Diretor — so' o corpo]")
	var arena := Node3D.new()
	root.add_child(arena)
	var ilha := _ilha(180.0, "return 0.0")
	arena.add_child(ilha)
	var p: CharacterBody3D = _player_scr.new()
	arena.add_child(p)
	var q: Node = _queda.iniciar(arena, ilha, p)
	q.castelo.global_position = Vector3(0, 300, 0)
	_passo(q, 1)
	q.saltar()
	_check(_queda.no_ar(p), "Queda.no_ar() acusa o mago no ar (gancho pra quem quiser)")

	var mana0: float = float(p.mana)
	_casts.clear()
	## O jogador martela TODOS os botoes durante a queda inteira.
	for _i in 120:
		p.request_fire()
		p.request_dodge()
		p.request_tatica()
		p.request_suprema()
		_frame(q, p)
	_check(_casts.is_empty(), "nenhum Bus.spell_cast durante a queda")
	_check(is_equal_approx(float(p.mana), mana0),
			"nenhuma mana gasta no ar (%.0f de %.0f)" % [p.mana, mana0])
	## Projectile e' um Area3D solto na arena (Projectile.launch usa o pai do
	## atirador). Se um tiro tivesse saido, ele estaria aqui.
	var projeteis := 0
	for n in arena.get_children():
		if n is Area3D:
			projeteis += 1
	_check(projeteis == 0, "nenhum projetil nasceu na arena durante a queda")
	_check(float(p.get("_dodge_cd")) <= 0.0, "nem esquiva: o dash tambem e' poder")

	## E o botao apertado no ar NAO pode disparar no primeiro frame do chao.
	p.request_fire()
	p.request_suprema()
	_passo(q, 3000)
	_check(str(q.fase) == "pousou", "pousou")
	_check(not bool(p.get("_want_fire")) and not bool(p.get("_want_suprema")),
			"o que ficou engatilhado no ar MORRE no pouso (nada vaza pro chao)")
	_check(_casts.is_empty(), "e nenhuma magia saiu em toda a queda")
	_check(not _queda.no_ar(p), "no chao, Queda.no_ar() volta a ser false")
	arena.queue_free()
	_blocos.append("sem_magia")
