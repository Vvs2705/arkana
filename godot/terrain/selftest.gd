## Selftest headless da raia TERRENO (R19). Rodar:
##   Godot --headless --path godot --script res://terrain/selftest.gd
## Prova (GDD §14, o pilar): fogo espalha por ORCAMENTO (nunca chance/tique),
## agua apaga sem carbonizar, lago congela/descongela com colisao honesta,
## raio eletrifica so a agua conectada, muro nasce/expira/cai a tiro e NAO
## nasce em celula ocupada, vento paga do mesmo orcamento, hazard_at devolve
## o dps certo por estado, e o reset nao vaza estado entre partidas.
## ANTI-VACUIDADE: reintroduz DOIS defeitos medidos do projeto-mae e mostra
## a guarda correspondente ficando VERMELHA (chance-por-tique carboniza;
## muro sem checagem de ocupacao empareda).
##
## NOTA (mesma dos outros selftests): em modo --script os autoloads so'
## registram DEPOIS deste arquivo compilar — nada aqui referencia Bus/Balance
## lexicalmente; tudo chega via get_node()/load() no 1o frame.
extends SceneTree

# espelhos dos enums de terrain/TerrainSystem.gd
const M_GROUND := 1
const M_FUEL := 2
const M_LAKE := 3
const M_MARSH := 4
const S_NORMAL := 0
const S_BURNING := 1
const S_CHARRED := 2
const S_FROZEN := 3
const S_ZAP := 4
const S_WALL := 5

var fails := 0
var _bus: Node
var _t: Dictionary
var _changed: Array[String] = []
var sys: Node
var island: Node3D


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	_bus = root.get_node("Bus")
	_t = root.get_node("Balance").TERRAIN
	island = (load("res://world/Island.tscn") as PackedScene).instantiate()
	root.add_child(island)
	sys = island.get_node_or_null("TerrainSystem")
	_check(sys != null, "ilha monta o TerrainSystem")
	if sys == null:
		quit(1)
		return
	_bus.terrain_changed.connect(func(k: String, _p: Vector3) -> void: _changed.append(k))

	_test_grid()
	await _test_bus_wiring()
	_test_fire_budget()
	_test_fire_budget_defect_red()
	_test_water_douses()
	_test_charred_and_restart()
	_test_freeze()
	_test_electrify()
	_test_wall()
	_test_wall_defect_red()
	_test_wind_pays_budget()
	_test_hazard()

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


# ------------------------------------------------------------------ helpers

func _count_state(s: int) -> int:
	var n := 0
	for i in sys._state.size():
		if sys._state[i] == s:
			n += 1
	return n


func _count_mat(m: int) -> int:
	var n := 0
	for i in sys._mat.size():
		if sys._mat[i] == m:
			n += 1
	return n


func _spread_total() -> int:
	var s := 0
	for f in sys._fires.values():
		s += int(f.spread)
	return s


## Celula combustivel com a janela 5x5 mais cheia — o pior lugar p/ acender.
## Varrido, nao chumbado: o layout e' da ilha e pode mudar sem avisar o teste.
func _heart() -> int:
	var best := -1
	var best_n := -1
	var n: int = sys._n
	for cz in range(2, n - 2):
		for cx in range(2, n - 2):
			if sys._mat[cz * n + cx] != M_FUEL:
				continue
			var cnt := 0
			for dz in range(-2, 3):
				for dx in range(-2, 3):
					if sys._mat[(cz + dz) * n + (cx + dx)] == M_FUEL:
						cnt += 1
			if cnt > best_n:
				best_n = cnt
				best = cz * n + cx
	return best


## Mancha combustivel CONTIGUA (4-vizinhos) em volta do coracao.
func _patch_size(seed_idx: int) -> int:
	var seen := {seed_idx: true}
	var queue := [seed_idx]
	var head := 0
	while head < queue.size():
		var idx: int = queue[head]
		head += 1
		for nb in sys._neighbors(idx):
			if not seen.has(nb) and sys._mat[nb] == M_FUEL:
				seen[nb] = true
				queue.append(nb)
	return queue.size()


func _water_cell(m: int) -> int:
	for i in sys._mat.size():
		if sys._mat[i] == m:
			return i
	return -1


## Miolo do lago: celula d'agua cujos 4 vizinhos tambem sao agua (a conducao
## e o congelamento tem o que medir — pre-condicao nao-vazia).
func _lake_core() -> int:
	for i in sys._mat.size():
		if sys._mat[i] != M_LAKE:
			continue
		var ok := true
		for nb in sys._neighbors(i):
			if sys._mat[nb] != M_LAKE:
				ok = false
				break
		if ok:
			return i
	return _water_cell(M_LAKE)


## Disco 3x3 todo de CHAO livre — onde o muro pode subir inteiro.
func _wall_spot() -> int:
	var n: int = sys._n
	for cz in range(2, n - 2):
		for cx in range(2, n - 2):
			var ok := true
			for dz in range(-1, 2):
				for dx in range(-1, 2):
					var i := (cz + dz) * n + (cx + dx)
					if sys._mat[i] != M_GROUND or sys._state[i] != S_NORMAL:
						ok = false
			if ok:
				return cz * n + cx
	return -1


# -------------------------------------------------------------------- testes

func _test_grid() -> void:
	var fuel := _count_mat(M_FUEL)
	var lake := _count_mat(M_LAKE)
	var marsh := _count_mat(M_MARSH)
	var ground := _count_mat(M_GROUND)
	_check(fuel >= 40, "grade: floresta mapeada (%d celulas combustiveis)" % fuel)
	_check(lake >= 20, "grade: lago mapeado (%d celulas d'agua)" % lake)
	_check(marsh >= 1, "grade: poca do alagado mapeada (%d celulas)" % marsh)
	_check(ground >= 400, "grade: chao livre p/ muro (%d celulas)" % ground)
	_check(sys._cell_trees.size() >= 40, "grade: arvores em celulas (%d)" % sys._cell_trees.size())


func _test_bus_wiring() -> void:
	var pos: Vector3 = sys._cell_center(_heart())
	_bus.terrain_hit.emit("fire", pos, true)
	await process_frame  # o handler e' deferido (seguro p/ flush de fisica)
	_check(_count_state(S_BURNING) > 0, "Bus.terrain_hit acende via sinal (fiacao real)")
	_check("ignite" in _changed, "Bus.terrain_changed('ignite') emitido")
	sys.reset()


func _test_fire_budget() -> void:
	var h := _heart()
	var budget: int = int(_t.fuel_budget)
	var patch := _patch_size(h)
	_check(patch > 9 + budget,
			"pre-condicao: mancha contigua (%d) MAIOR que o teto (%d) — o orcamento morde" % [patch, 9 + budget])
	var ok_spread := true
	var ok_lit := true
	var worst := 0
	for run in 12:
		sys._apply_hit("fire", sys._cell_center(h), true)
		var seeds := _count_state(S_BURNING)
		for t in 64:
			sys._sim_tick(0.0)  # relogio parado: pior caso, nada se apaga sozinho
		var lit := _count_state(S_BURNING)
		if _spread_total() > budget:
			ok_spread = false
		if lit > seeds + budget:
			ok_lit = false
		worst = maxi(worst, lit)
		sys.reset()
	_check(ok_spread, "12 ignicoes: propagacao <= fuel_budget SEMPRE")
	_check(ok_lit, "12 ignicoes: acesas <= sementes + orcamento SEMPRE (pior caso %d)" % worst)


func _test_fire_budget_defect_red() -> void:
	# GUARDA VERMELHA 1 — reintroduz o defeito que carbonizou 380/380 celulas
	# no projeto-mae: chance por tique, sem orcamento. A MESMA guarda acima
	# tem de reprovar com ele ligado — senao ela e' vacua.
	var h := _heart()
	var budget: int = int(_t.fuel_budget)
	sys.defect_chance_per_tick = true
	sys._apply_hit("fire", sys._cell_center(h), true)
	var seeds := _count_state(S_BURNING)
	for t in 64:
		sys._sim_tick(0.0)
	var lit := _count_state(S_BURNING)
	sys.defect_chance_per_tick = false
	sys.reset()
	_check(lit > seeds + budget,
			"DEFEITO chance-por-tique deixa a guarda VERMELHA: %d acesas > teto %d (carbonizacao reproduzida)"
			% [lit, seeds + budget])


func _test_water_douses() -> void:
	var h := _heart()
	var pos: Vector3 = sys._cell_center(h)
	sys._apply_hit("fire", pos, false)
	_check(sys._state[h] == S_BURNING, "fogo fraco acende a celula do impacto")
	sys._apply_hit("water", pos, false)
	_check(sys._state[h] == S_NORMAL, "agua APAGA: celula volta ao normal")
	_check(_count_state(S_CHARRED) == 0, "apagar != queimar ate o fim: nada carboniza")
	_check(sys._fires.is_empty(), "frente sem chama viva fecha (sem vazamento)")
	_check("extinguish" in _changed, "terrain_changed('extinguish') emitido")
	sys.reset()


func _test_charred_and_restart() -> void:
	var tcell := -1
	for idx in sys._cell_trees:
		tcell = idx
		break
	_check(tcell >= 0, "pre-condicao: existe celula com arvore")
	var ti: int = sys._cell_trees[tcell][0]
	sys._apply_hit("fire", sys._cell_center(tcell), false)
	_check(sys._state[tcell] == S_BURNING, "arvore acende")
	sys._sim_tick(float(_t.burn_duration) + 0.1)
	_check(sys._state[tcell] == S_CHARRED, "apos burn_duration vira CARVAO")
	_check("charred" in _changed, "terrain_changed('charred') emitido")
	_check(island._tree_shapes[ti].disabled, "cobertura some DE VERDADE: tronco deixa de colidir")
	# a troca de mesh (copa escondida + toco) vive no RenderingServer, que em
	# headless nao devolve leitura — o estado consultavel da ilha e' a prova
	_check(island.is_tree_burned(ti), "ilha marcou a copa como consumida (mesh trocado)")
	sys.reset()  # a porta da partida nova
	_check(sys._state[tcell] == S_NORMAL, "restart limpa o carvao")
	_check(not island.is_tree_burned(ti) and not island._tree_shapes[ti].disabled,
			"restart devolve a floresta (nada vaza entre partidas)")


func _test_freeze() -> void:
	var pos: Vector3 = sys._cell_center(_lake_core())
	sys._apply_hit("water", pos, true)
	var frozen := _count_state(S_FROZEN)
	_check(frozen > 0, "agua no lago CONGELA a superficie (%d celulas)" % frozen)
	_check(sys._ice.size() == frozen, "cada celula congelada tem gelo (mesh + colisao)")
	if not sys._ice.is_empty():
		var pair: Array = sys._ice.values()[0]
		_check(pair[1] is CollisionShape3D and (pair[1] as Node).get_parent() is StaticBody3D,
				"gelo e' ROTA: colisao real em StaticBody")
		_check((pair[1] as Node3D).global_position.y > 0.5,
				"a lamina fica NA SUPERFICIE (nao no fundo do lago)")
	_check(sys.hazard_at(pos) == 0.0, "gelo nao machuca (hazard 0)")
	_check("freeze" in _changed, "terrain_changed('freeze') emitido")
	sys._sim_tick(float(_t.freeze_duration) + 0.5)
	_check(_count_state(S_FROZEN) == 0 and sys._ice.is_empty(),
			"derreteu: colisao some LIMPA (quem estava em cima cai)")
	_check("melt" in _changed, "terrain_changed('melt') emitido")
	sys.reset()


func _test_electrify() -> void:
	var core := _lake_core()
	var pos: Vector3 = sys._cell_center(core)
	sys._apply_hit("lightning", pos, false)
	var zapped := _count_state(S_ZAP)
	_check(zapped >= 3, "raio ELETRIFICA a agua conectada (%d celulas)" % zapped)
	_check(sys.hazard_at(pos) == float(_t.electrify_dps), "hazard_at = electrify_dps na agua eletrificada")
	var marsh := _water_cell(M_MARSH)
	_check(marsh >= 0 and sys._state[marsh] != S_ZAP,
			"a conducao NAO atravessa terra (lago != alagado)")
	_check("electrify" in _changed, "terrain_changed('electrify') emitido")
	sys._sim_tick(float(_t.electrify_duration) + 0.5)
	_check(sys.hazard_at(pos) == 0.0, "eletrificacao expira")
	sys.reset()
	# gelo ISOLA: congela a area do impacto (r 2.5 > r 1.5 do raio) — o raio
	# cai no gelo e nao encontra liquido para conduzir
	sys._apply_hit("water", pos, true)
	_check(sys._state[core] == S_FROZEN, "pre-condicao: o miolo congelou")
	sys._apply_hit("lightning", pos, false)
	_check(_count_state(S_ZAP) == 0, "gelo isola: raio no gelo nao conduz")
	sys.reset()


func _test_wall() -> void:
	var c := _wall_spot()
	_check(c >= 0, "pre-condicao: ha disco de chao livre p/ muro")
	var pos: Vector3 = sys._cell_center(c)
	sys._apply_hit("earth", pos, true)
	var walls := _count_state(S_WALL)
	_check(walls >= 5, "terra FORTE ergue muro em area (%d celulas)" % walls)
	_check(sys._walls.size() == walls, "cada celula de muro tem mesh + colisao")
	if not sys._walls.is_empty():
		var pair: Array = sys._walls.values()[0]
		_check((pair[1] as Node).get_parent() is StaticBody3D, "muro e' cobertura: colisao real")
	_check(sys.hazard_at(pos) == 0.0, "muro nao machuca (hazard 0)")
	_check("wall_up" in _changed, "terrain_changed('wall_up') emitido")
	# projeteis derrubam: terrain_hit de QUALQUER elemento danifica o muro
	var fire_dmg: float = float(root.get_node("Balance").FIRE.dmg)
	var expect := int(ceil(float(_t.wall_hp) / fire_dmg))
	var hits := 0
	while sys._state[c] == S_WALL and hits < expect + 3:
		sys._apply_hit("fire", pos, false)
		hits += 1
	_check(sys._state[c] != S_WALL, "projeteis DERRUBAM o muro (cobertura destrutivel)")
	_check(hits == expect, "muro caiu no golpe certo (%d de %d)" % [hits, expect])
	_check("wall_down" in _changed, "terrain_changed('wall_down') emitido")
	sys.reset()
	# expira sozinho
	sys._apply_hit("earth", pos, false)
	_check(sys._state[c] == S_WALL, "terra fraca ergue muro na celula do impacto")
	sys._sim_tick(float(_t.wall_duration) + 0.5)
	_check(sys._state[c] == S_NORMAL and sys._walls.is_empty(), "muro expira e a colisao some")
	sys.reset()
	# ANTI-GRIEFING: um corpo DE PE na celula — o muro nao nasce nela
	var body := CharacterBody3D.new()
	root.add_child(body)
	body.global_position = pos
	sys._apply_hit("earth", pos, true)
	_check(sys._state[c] != S_WALL, "muro NAO nasce na celula ocupada (anti-griefing provado)")
	_check(_count_state(S_WALL) >= 4, "a protecao nao vira nerf: o resto do disco sobe")
	sys.reset()
	body.queue_free()


func _test_wall_defect_red() -> void:
	# GUARDA VERMELHA 2 — sem a checagem de ocupacao o muro EMPAREDA: a
	# guarda acima tem de reprovar com o defeito ligado (nao e' vacua).
	var c := _wall_spot()
	var pos: Vector3 = sys._cell_center(c)
	var body := CharacterBody3D.new()
	root.add_child(body)
	body.global_position = pos
	sys.defect_wall_ignores_occupancy = true
	sys._apply_hit("earth", pos, true)
	_check(sys._state[c] == S_WALL,
			"DEFEITO muro-sem-checagem deixa a guarda VERMELHA: muro nasceu EM CIMA do corpo")
	sys.defect_wall_ignores_occupancy = false
	sys.reset()
	body.queue_free()


func _test_wind_pays_budget() -> void:
	var h := _heart()
	var pos: Vector3 = sys._cell_center(h)
	var budget: int = int(_t.fuel_budget)
	sys._apply_hit("fire", pos, false)
	var seeds := _count_state(S_BURNING)
	_check(seeds == 1, "pre-condicao: 1 semente no coracao da floresta")
	sys._apply_hit("wind", pos, false)
	var after_wind := _count_state(S_BURNING)
	_check(after_wind > seeds, "vento ESPALHA o fogo na hora (%d -> %d)" % [seeds, after_wind])
	# sopra ate cansar: se o vento nao pagasse, passaria do teto
	for k in 20:
		sys._apply_hit("wind", pos, true)
		for t in 2:
			sys._sim_tick(0.0)
	var lit := _count_state(S_BURNING)
	var spread := _spread_total()
	_check(spread <= budget, "vento PAGA do fuel_budget (%d <= %d)" % [spread, budget])
	_check(lit <= seeds + budget,
			"vento nao e' carbonizador infinito (%d <= %d)" % [lit, seeds + budget])
	sys.reset()


func _test_hazard() -> void:
	var h := _heart()
	var pos: Vector3 = sys._cell_center(h)
	_check(sys.hazard_at(pos) == 0.0, "hazard_at: normal = 0")
	sys._apply_hit("fire", pos, false)
	_check(sys.hazard_at(pos) == float(_t.burn_dps), "hazard_at: queimando = burn_dps")
	_check(sys.hazard_at(Vector3(999, 0, 999)) == 0.0, "hazard_at: fora do mapa = 0")
	# o contrato estatico que o coordenador fia no Pawn (fiacao defensiva)
	var scr: GDScript = sys.get_script()
	_check(float(scr.dps_at(pos)) == float(_t.burn_dps),
			"TerrainSystem.dps_at (contrato do Pawn) devolve o mesmo dps")
	sys.reset()
	_check(sys.hazard_at(pos) == 0.0, "reset zera o perigo")
