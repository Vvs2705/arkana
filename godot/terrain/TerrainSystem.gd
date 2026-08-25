## terrain/TerrainSystem.gd — dono: raia TERRENO (R19). O PILAR (GDD §14).
## Grade logica de celulas (Balance.TERRAIN.cell_size) sobre a ilha procedural.
## O gameplay EMITE Bus.terrain_hit(element, pos, strong) no impacto do
## projétil; AQUI o mundo muda: fogo acende a floresta, agua apaga/congela o
## lago, raio eletrifica a agua conectada, terra ergue muro destrutivel, vento
## espalha o fogo PAGANDO do mesmo orcamento.
##
## FRONTEIRA (contrato que segurou 8 fases no projeto-mae): este sistema NUNCA
## aplica dano. Ele publica o perigo em hazard_at(pos) -> dps e quem decide
## vida e' o gameplay, no passo de fisica dele.
##
## FOGO POR ORCAMENTO (lei com MEDICAO — GDD §14, nota de 19/08): UMA rolagem
## por ARESTA (edge_chance) + fuel_budget por ignicao. Chance por tique =
## 99,67% acumulado = 380/380 celulas carbonizadas no projeto-mae. Para
## regular o incendio, mexa no ORCAMENTO — nunca na chance. O selftest
## reintroduz o defeito e prova que a guarda fica vermelha.
##
## MOBILE: mudancas visuais sao trocas de mesh/instancia baratas (MultiMesh de
## copas/tocos/carvao), particulas contidas (teto MAX_FIRE_FX) e UMA luz por
## frente de fogo — nunca luz por celula.
class_name TerrainSystem
extends Node3D

const TICK := 0.5              # s por passo da simulacao — KNOB
const MAX_FIRE_FX := 24        # chamas com particulas ao mesmo tempo — KNOB de aparelho
const MAX_SCORCH := 256        # marcas de carvao no chao (depois recicla as antigas)
const ZAP_FLOOD_LIMIT := 200   # guarda de perf do flood eletrico (nao e' balanceamento)
const R_SEED_STRONG := 1.5     # raio (celulas) de sementes/muro da magia forte: 3x3
const R_SPLASH := 1.5          # raio padrao de agua/vento/raio
const R_SPLASH_STRONG := 2.5
# cotas/raios dos discos d'agua — espelham world/Island.gd (_build_water)
const LAKE_SURFACE_Y := 0.6
const MARSH_SURFACE_Y := 0.55
## Fallback apenas: os valores REAIS sao lidos da ilha em _build_grid
## (Island.LAKE_DISC_R / MARSH_DISC_R / LAND_R). Ficam aqui para o sistema rodar
## num teste sem mundo. Em 26/08 estes numeros eram os UNICOS donos da verdade e
## envelheceram calados quando a ilha cresceu: o lago virou 29 m e o fogo
## continuou achando que a agua acabava aos 18.
const LAKE_DISC_R := 18.0
const MARSH_DISC_R := 15.0
const LAND_R_PADRAO := 76.0

enum { M_OUT, M_GROUND, M_FUEL, M_LAKE, M_MARSH }  # material: escrito 1x no build
enum { S_NORMAL, S_BURNING, S_CHARRED, S_FROZEN, S_ZAP, S_WALL }  # a magia mexe aqui

## SELFTEST ONLY — reintroduzem defeitos MEDIDOS para provar que as guardas do
## selftest reprovam (anti-vacuidade). NUNCA ligar em jogo.
var defect_chance_per_tick := false
var defect_wall_ignores_occupancy := false

static var instance: TerrainSystem

var _island                     # world/Island.gd (duck-typed: script @tool)
var _n := 60                    # celulas por lado
var _cs := 3.0                  # m por celula (Balance.TERRAIN.cell_size)
var _half := 90.0
var _wall_h := 3.5
var _mat := PackedByteArray()
var _state := PackedByteArray()
var _cell_trees := {}           # idx -> Array de indices de arvore da ilha
var _remaining := {}            # idx -> s restantes do estado (so celulas ativas)
var _fires := {}                # id -> {budget, seeds, spread, alive} (frentes vivas)
var _fire_of := {}              # idx aceso -> id da frente que o acendeu
var _pending := {}              # idx -> ainda deve a sua UNICA rolagem por aresta
var _burning := {}              # idx -> true (vento e centroide da luz leem daqui)
var _wall_hp := {}              # idx -> hp restante do muro
var _ice := {}                  # idx -> [MeshInstance3D, CollisionShape3D]
var _walls := {}                # idx -> [MeshInstance3D, CollisionShape3D]
var _fire_fx := {}              # idx -> CPUParticles3D
var _next_fire := 0
var _acc := 0.0
var _scorch_i := 0

var _ice_body: StaticBody3D
var _wall_body: StaticBody3D
var _fire_light: OmniLight3D
var _zap: MeshInstance3D
var _scorch_mm: MultiMesh
var _ice_mesh: BoxMesh
var _ice_shape: BoxShape3D
var _wall_mesh: BoxMesh
var _wall_shape: BoxShape3D
var _flame_quad: QuadMesh


func _ready() -> void:
	_island = get_parent()
	instance = self
	add_to_group("terrain")
	_cs = float(Balance.TERRAIN.cell_size)
	_wall_h = float(Balance.TERRAIN.wall_height)
	_half = float(_island.SIZE) * 0.5
	_n = int(round(float(_island.SIZE) / _cs))
	_build_grid()
	_build_nodes()
	Bus.terrain_hit.connect(_on_terrain_hit)
	# Estado NOVO nao atravessa partida (vazou 3x no projeto-mae). O Main
	# emite match_started ao remontar a arena — esta e' a porta da limpeza.
	Bus.match_started.connect(reset)


func _exit_tree() -> void:
	if instance == self:
		instance = null


func _physics_process(delta: float) -> void:
	_acc += delta
	while _acc >= TICK:
		_acc -= TICK
		_sim_tick(TICK)


# ------------------------------------------------------------- API publica

## O CONTRATO com o gameplay (coordenador fia no Pawn): dps do perigo no
## ponto. Queimando = burn_dps, eletrificado = electrify_dps, resto = 0.
## Este modulo NUNCA aplica o dano — quem le e decide e' o gameplay.
func hazard_at(pos: Vector3) -> float:
	var idx := _cell_at(pos)
	if idx < 0:
		return 0.0
	match int(_state[idx]):
		S_BURNING:
			return float(Balance.TERRAIN.burn_dps)
		S_ZAP:
			return float(Balance.TERRAIN.electrify_dps)
	return 0.0


## Fiacao defensiva p/ o Pawn: 0.0 se o sistema nao montou (ilha fallback).
static func dps_at(pos: Vector3) -> float:
	if instance != null and is_instance_valid(instance):
		return instance.hazard_at(pos)
	return 0.0


## Zera TODO o estado transitorio e restaura a floresta. Chamado no
## match_started (o Main remonta a arena) e pelo selftest entre blocos.
func reset() -> void:
	for idx in _walls:
		_free_pair(_walls[idx])
	_walls.clear()
	for idx in _ice:
		_free_pair(_ice[idx])
	_ice.clear()
	for idx in _fire_fx:
		_fire_fx[idx].queue_free()
	_fire_fx.clear()
	_state.fill(0)
	_remaining.clear()
	_fires.clear()
	_fire_of.clear()
	_pending.clear()
	_burning.clear()
	_wall_hp.clear()
	_acc = 0.0
	for i in int(_island.tree_count()):
		_island.set_tree_burned(i, false)
	var hid := Transform3D(Basis.IDENTITY.scaled(Vector3.ONE * 0.001), Vector3(0, -60, 0))
	for i in MAX_SCORCH:
		_scorch_mm.set_instance_transform(i, hid)
	_scorch_i = 0
	_fire_light.visible = false
	_zap.visible = false


# ------------------------------------------------------------------ grade

func _build_grid() -> void:
	_mat.resize(_n * _n)
	_state.resize(_n * _n)
	var lake: Vector2 = _island.LAKE
	var marsh: Vector2 = _island.MARSH
	var forest: Vector2 = _island.FOREST
	var forest_r: float = _island.FOREST_R
	# Os raios saem da ILHA que foi entregue, nunca das constantes locais: e' o
	# que impede o terreno de envelhecer quando o mapa muda de tamanho.
	var lake_r: float = float(_island.LAKE_DISC_R) if "LAKE_DISC_R" in _island else LAKE_DISC_R
	var marsh_r: float = float(_island.MARSH_DISC_R) if "MARSH_DISC_R" in _island else MARSH_DISC_R
	var land_r: float = float(_island.LAND_R) if "LAND_R" in _island else LAND_R_PADRAO
	for cz in _n:
		for cx in _n:
			var idx := cz * _n + cx
			var x := (cx + 0.5) * _cs - _half
			var z := (cz + 0.5) * _cs - _half
			var p := Vector2(x, z)
			var h: float = _island.height(x, z)
			var m := M_OUT
			if p.distance_to(lake) < lake_r and h < LAKE_SURFACE_Y - 0.1:
				m = M_LAKE
			elif p.distance_to(marsh) < marsh_r and h < MARSH_SURFACE_Y - 0.1:
				m = M_MARSH
			elif h > 0.9 and p.length() < land_r:
				# a FLORESTA inteira e' combustivel (arvores + sub-bosque):
				# a mancha CONTIGUA e' o que faz o ORCAMENTO ser a lei que
				# regula o incendio (celulas so-arvore seriam ilhas soltas)
				m = M_FUEL if p.distance_to(forest) < forest_r else M_GROUND
			_mat[idx] = m
	for i in int(_island.tree_count()):
		var idx := _cell_at(_island.tree_pos(i))
		if idx >= 0:
			_mat[idx] = M_FUEL
			if not _cell_trees.has(idx):
				_cell_trees[idx] = []
			_cell_trees[idx].append(i)


func _cell_at(pos: Vector3) -> int:
	var l := to_local(pos)
	var cx := int(floor((l.x + _half) / _cs))
	var cz := int(floor((l.z + _half) / _cs))
	if cx < 0 or cz < 0 or cx >= _n or cz >= _n:
		return -1
	return cz * _n + cx


func _cell_center(idx: int) -> Vector3:
	var cx := idx % _n
	var cz := int(idx / float(_n))
	var x := (cx + 0.5) * _cs - _half
	var z := (cz + 0.5) * _cs - _half
	return to_global(Vector3(x, float(_island.height(x, z)), z))


func _disc(c: int, r: float) -> PackedInt32Array:
	var out := PackedInt32Array()
	var cx := c % _n
	var cz := int(c / float(_n))
	var ri := int(ceil(r))
	for dz in range(-ri, ri + 1):
		for dx in range(-ri, ri + 1):
			if dx * dx + dz * dz <= r * r:
				var x := cx + dx
				var z := cz + dz
				if x >= 0 and z >= 0 and x < _n and z < _n:
					out.append(z * _n + x)
	return out


func _neighbors(c: int) -> PackedInt32Array:
	var out := PackedInt32Array()
	var cx := c % _n
	if cx > 0:
		out.append(c - 1)
	if cx < _n - 1:
		out.append(c + 1)
	if c >= _n:
		out.append(c - _n)
	if c < _n * (_n - 1):
		out.append(c + _n)
	return out


func _is_water(idx: int) -> bool:
	return _mat[idx] == M_LAKE or _mat[idx] == M_MARSH


func _is_liquid(idx: int) -> bool:
	return _is_water(idx) and _state[idx] != S_FROZEN


func _flammable(idx: int) -> bool:
	return _mat[idx] == M_FUEL and _state[idx] == S_NORMAL


func _emit(kind: String, idx: int) -> void:
	Bus.terrain_changed.emit(kind, _cell_center(idx))


# ---------------------------------------------------------------- reacoes

func _on_terrain_hit(element: String, pos: Vector3, strong: bool) -> void:
	# deferido: o sinal chega do flush de fisica (body_entered do projetil) e
	# aqui nascem/morrem StaticBodies — mexer nisso durante o flush e' erro.
	call_deferred("_apply_hit", element, pos, strong)


## Reacao a um impacto. O selftest chama direto (sincrono).
func _apply_hit(element: String, pos: Vector3, strong: bool) -> void:
	var c := _cell_at(pos)
	if c < 0:
		return
	if _state[c] == S_WALL:
		# muro e' cobertura DESTRUTIVEL: qualquer elemento o danifica
		_damage_wall(c, _element_dmg(element))
		return
	match element:
		"fire":
			_apply_fire(c, R_SEED_STRONG if strong else 0.0)
		"water":
			_apply_water(c, R_SPLASH_STRONG if strong else R_SPLASH)
		"lightning":
			_apply_lightning(c, R_SPLASH)
		"earth":
			_apply_earth(c, strong)
		"wind":
			_apply_wind(c, R_SPLASH)


## FOGO: acende a cobertura (abre uma FRENTE com orcamento proprio) e derrete
## o gelo (contra-jogada do §14). O raio da magia planta SEMENTES de graca —
## o orcamento cobre a PROPAGACAO, nao o impacto.
func _apply_fire(c: int, r: float) -> void:
	var fire := {}
	for idx in _disc(c, r):
		if _state[idx] == S_FROZEN:
			_set_state(idx, S_NORMAL, 0.0)
			_emit("melt", idx)
		elif _flammable(idx):
			if fire.is_empty():
				fire = _new_fire()
			_ignite(idx, fire, false)
	if not fire.is_empty():
		_emit("ignite", c)


## AGUA: apaga o fogo (apagar != queimar ate o fim — nada carboniza) e
## congela a superficie do lago: vira ROTA por freeze_duration.
func _apply_water(c: int, r: float) -> void:
	var doused := false
	var froze := false
	for idx in _disc(c, r):
		if _state[idx] == S_BURNING:
			_set_state(idx, S_NORMAL, 0.0)
			doused = true
		elif _is_water(idx) and (_state[idx] == S_NORMAL or _state[idx] == S_ZAP):
			# congelar tambem CORTA a conducao eletrica
			_set_state(idx, S_FROZEN, float(Balance.TERRAIN.freeze_duration))
			froze = true
	if doused:
		_emit("extinguish", c)
	if froze:
		_emit("freeze", c)


## RAIO: eletrifica TODA a agua conectada (flood 4-vizinhos). Gelo e terra
## isolam — o flood so atravessa agua liquida.
func _apply_lightning(c: int, r: float) -> void:
	var source := -1
	if _is_liquid(c):
		source = c
	else:
		for idx in _disc(c, r):
			if _is_liquid(idx):
				source = idx
				break
	if source < 0:
		return
	var dur := float(Balance.TERRAIN.electrify_duration)
	var src_pos := _cell_center(source)
	var far := 0.0
	var queue := PackedInt32Array([source])
	var seen := {source: true}
	var head := 0
	while head < queue.size() and head < ZAP_FLOOD_LIMIT:
		var idx := queue[head]
		head += 1
		_set_state(idx, S_ZAP, dur)  # refresca se ja estava
		var cc := _cell_center(idx)
		far = maxf(far, Vector2(cc.x - src_pos.x, cc.z - src_pos.z).length())
		for nb in _neighbors(idx):
			if not seen.has(nb) and _is_liquid(nb):
				seen[nb] = true
				queue.append(nb)
	# visual: UMA lamina emissiva sobre a area (nunca luz nem fx por celula)
	var wy := LAKE_SURFACE_Y if _mat[source] == M_LAKE else MARSH_SURFACE_Y
	_zap.visible = true
	_zap.global_position = Vector3(src_pos.x, wy + 0.12, src_pos.z)
	var rad := far + _cs
	_zap.scale = Vector3(rad, 1.0, rad)
	_emit("electrify", source)


## TERRA: ergue muro de pedra — cobertura destrutivel com hp e prazo.
## MURO NAO NASCE EM CELULA OCUPADA (anti-griefing provado no projeto-mae:
## emparedar gente por wall_duration e' botao de deletar, nao jogada).
func _apply_earth(c: int, strong: bool) -> void:
	var cells := _disc(c, R_SEED_STRONG) if strong else PackedInt32Array([c])
	var occupied: Variant = null  # resolvido preguicosamente (consulta a arvore)
	var up := 0
	for idx in cells:
		if _state[idx] != S_NORMAL:
			continue
		var m := int(_mat[idx])
		# so em chao firme; sob arvore em pe (ou fora da ilha) nao cabe muro
		if m != M_GROUND and not (m == M_FUEL and not _cell_trees.has(idx)):
			continue
		if not defect_wall_ignores_occupancy:
			if occupied == null:
				occupied = _occupied_cells()
			if occupied.has(idx):
				continue  # ninguem e' emparedado na propria celula
		_wall_hp[idx] = float(Balance.TERRAIN.wall_hp)
		_set_state(idx, S_WALL, float(Balance.TERRAIN.wall_duration))
		up += 1
	if up > 0:
		_emit("wall_up", c)


## VENTO em fogo: espalha SEM rolagem — mas PAGA do MESMO fuel_budget da
## frente. Sem isso o vento seria o buraco por onde a carbonizacao volta.
func _apply_wind(c: int, r: float) -> void:
	var spread := false
	for idx in _disc(c, r):
		if _state[idx] != S_BURNING:
			continue
		var f: Dictionary = _fires.get(_fire_of.get(idx, -1), {})
		if f.is_empty():
			continue
		for nb in _neighbors(idx):
			if _ignite(nb, f, true):
				spread = true
	if spread:
		_emit("wind_spread", c)


## Quem esta DE PE em cada celula. ponytail: celula do CENTRO do corpo — quem
## esta na divisa pode ver o muro subir encostado (o motor empurra, nao
## enterra). Se doer no playtest, marcar tambem a celula do canto mais proximo.
func _occupied_cells() -> Dictionary:
	var out := {}
	for b in get_tree().root.find_children("*", "CharacterBody3D", true, false):
		var idx := _cell_at((b as Node3D).global_position)
		if idx >= 0:
			out[idx] = true
	return out


func _damage_wall(idx: int, dmg: float) -> void:
	if _state[idx] != S_WALL:
		return
	_wall_hp[idx] = float(_wall_hp.get(idx, 0.0)) - dmg
	if float(_wall_hp[idx]) <= 0.0:
		_set_state(idx, S_NORMAL, 0.0)
		_emit("wall_down", idx)


func _element_dmg(el: String) -> float:
	match el:
		"water":
			return float(Balance.WATER.dmg)
		"lightning":
			return float(Balance.LIGHTNING.dmg)
		"earth":
			return float(Balance.EARTH.dmg)
		"wind":
			return float(Balance.WIND.dmg)
		_:
			return float(Balance.FIRE.dmg)


# ----------------------------------------------------------- fogo (a lei)

func _new_fire() -> Dictionary:
	_next_fire += 1
	var f := {
		"id": _next_fire,
		"budget": int(Balance.TERRAIN.fuel_budget),
		"seeds": 0,
		"spread": 0,
		"alive": 0,
	}
	_fires[_next_fire] = f
	return f


## Acende `idx` na frente `f`. `charged` = veio da PROPAGACAO (rolagem de
## aresta ou rajada de vento): SO ela paga combustivel. As sementes do impacto
## sao de graca — senao uma magia de area gastaria o orcamento no proprio
## impacto e nada propagaria.
func _ignite(idx: int, f: Dictionary, charged: bool) -> bool:
	if not _flammable(idx):
		return false
	if charged:
		if int(f.budget) <= 0:
			return false  # teto duro: a frente para mesmo cercada de floresta
		f.budget = int(f.budget) - 1
		f.spread = int(f.spread) + 1
	else:
		f.seeds = int(f.seeds) + 1
	f.alive = int(f.alive) + 1
	_fire_of[idx] = f.id
	_pending[idx] = true  # fara a sua UNICA rolagem por aresta no proximo tique
	_set_state(idx, S_BURNING, float(Balance.TERRAIN.burn_duration))
	return true


## Celula deixou de queimar (apagou, expirou): devolve a vaga a frente; a
## ultima chama fecha o incendio. Ponto UNICO — todo caminho passa por ca.
func _leave_fire(idx: int) -> void:
	_pending.erase(idx)
	var id: int = _fire_of.get(idx, -1)
	_fire_of.erase(idx)
	var f: Dictionary = _fires.get(id, {})
	if f.is_empty():
		return
	f.alive = int(f.alive) - 1
	if int(f.alive) <= 0:
		_fires.erase(id)


# -------------------------------------------------------------- simulacao

## Um passo. `dt = 0` = relogio parado (pior caso do selftest: NADA expira;
## quem segura a frente e' SO o orcamento).
func _sim_tick(dt: float) -> void:
	var snap := _burning.keys()
	for idx in snap:
		if _state[idx] != S_BURNING:
			continue
		if defect_chance_per_tick:
			# DEFEITO MEDIDO do projeto-mae (30%/vizinho/tique = 99,67%
			# acumulado = mapa carbonizado): re-rola CADA aresta a CADA tique
			# e ignora o orcamento. Existe SO p/ o selftest provar o vermelho.
			var f0: Dictionary = _fires.get(_fire_of.get(idx, -1), {})
			for nb in _neighbors(idx):
				if not f0.is_empty() and _flammable(nb) \
						and randf() < float(Balance.TERRAIN.edge_chance):
					f0.spread = int(f0.spread) + 1
					f0.alive = int(f0.alive) + 1
					_fire_of[nb] = f0.id
					_set_state(nb, S_BURNING, float(Balance.TERRAIN.burn_duration))
		elif _pending.has(idx):
			# UMA ROLAGEM POR ARESTA, e nunca mais — a frente avanca um anel
			# por tique em vez de sentenciar a floresta inteira.
			_pending.erase(idx)
			var f: Dictionary = _fires.get(_fire_of.get(idx, -1), {})
			if not f.is_empty():
				for nb in _neighbors(idx):
					if int(f.budget) > 0 and _flammable(nb) \
							and randf() < float(Balance.TERRAIN.edge_chance):
						_ignite(nb, f, true)
	if dt > 0.0:
		for idx in _remaining.keys():
			if not _remaining.has(idx):
				continue
			_remaining[idx] = float(_remaining[idx]) - dt
			if float(_remaining[idx]) <= 0.0:
				_expire(idx)
	_update_front_fx()


func _expire(idx: int) -> void:
	match int(_state[idx]):
		S_BURNING:
			# queimou ate o fim: CARVAO permanente — a cobertura SOME de verdade
			_set_state(idx, S_CHARRED, 0.0)
			_emit("charred", idx)
		S_FROZEN:
			# derreteu: a colisao some LIMPA (quem estava em cima cai — honesto)
			_set_state(idx, S_NORMAL, 0.0)
			_emit("melt", idx)
		S_WALL:
			_set_state(idx, S_NORMAL, 0.0)
			_emit("wall_down", idx)
		_:
			_set_state(idx, S_NORMAL, 0.0)


## Escreve o estado, agenda a expiracao e sincroniza visual/colisao.
## Ponto UNICO de mudanca — e' o que garante que nada vaza ao trocar de estado.
func _set_state(idx: int, s: int, duration: float) -> void:
	var old := int(_state[idx])
	if old == S_BURNING and s != S_BURNING:
		_leave_fire(idx)
		_burning.erase(idx)
		_fx_off(idx)
	if old == S_FROZEN and s != S_FROZEN:
		_ice_off(idx)
	if old == S_WALL and s != S_WALL:
		_wall_off(idx)
		_wall_hp.erase(idx)
	_state[idx] = s
	if duration > 0.0:
		_remaining[idx] = duration
	else:
		_remaining.erase(idx)
	match s:
		S_BURNING:
			_burning[idx] = true
			_fx_on(idx)
		S_FROZEN:
			_ice_on(idx)
		S_WALL:
			_wall_on(idx)
		S_CHARRED:
			_char_cell(idx)


# ------------------------------------------------------ visual (mobile-barato)

func _build_nodes() -> void:
	_ice_body = StaticBody3D.new()
	_ice_body.name = "IceBody"
	add_child(_ice_body)
	_wall_body = StaticBody3D.new()
	_wall_body.name = "WallBody"
	add_child(_wall_body)

	_ice_mesh = BoxMesh.new()
	_ice_mesh.size = Vector3(_cs, 0.5, _cs)
	var im := StandardMaterial3D.new()
	im.albedo_color = Color(0.72, 0.9, 1.0)
	im.emission_enabled = true
	im.emission = Color(0.35, 0.55, 0.75)
	im.emission_energy_multiplier = 0.25
	_ice_mesh.material = im
	_ice_shape = BoxShape3D.new()
	_ice_shape.size = _ice_mesh.size

	_wall_mesh = BoxMesh.new()
	_wall_mesh.size = Vector3(_cs * 0.96, _wall_h, _cs * 0.96)
	var wm := StandardMaterial3D.new()
	wm.albedo_color = Color(0.52, 0.55, 0.62)
	_wall_mesh.material = wm
	_wall_shape = BoxShape3D.new()
	_wall_shape.size = _wall_mesh.size

	# carvao no chao: 1 MultiMesh pre-alocado = 1 draw call, recicla as antigas
	var plane := PlaneMesh.new()
	plane.size = Vector2(_cs * 0.92, _cs * 0.92)
	var sm := StandardMaterial3D.new()
	sm.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	sm.albedo_color = Color(0.1, 0.08, 0.07)
	plane.material = sm
	_scorch_mm = MultiMesh.new()
	_scorch_mm.transform_format = MultiMesh.TRANSFORM_3D
	_scorch_mm.mesh = plane
	_scorch_mm.instance_count = MAX_SCORCH
	var hid := Transform3D(Basis.IDENTITY.scaled(Vector3.ONE * 0.001), Vector3(0, -60, 0))
	for i in MAX_SCORCH:
		_scorch_mm.set_instance_transform(i, hid)
	var smi := MultiMeshInstance3D.new()
	smi.name = "Scorch"
	smi.multimesh = _scorch_mm
	smi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(smi)

	# UMA luz por FRENTE de fogo (teto da regra mobile) — nunca por celula
	_fire_light = OmniLight3D.new()
	_fire_light.light_color = Color(1.0, 0.6, 0.3)
	_fire_light.omni_range = 11.0
	_fire_light.light_energy = 1.5
	_fire_light.shadow_enabled = false
	_fire_light.visible = false
	add_child(_fire_light)

	# lamina eletrica: 1 disco emissivo por descarga (visual, nao gameplay)
	var cyl := CylinderMesh.new()
	cyl.top_radius = 1.0
	cyl.bottom_radius = 1.0
	cyl.height = 0.06
	var zm := StandardMaterial3D.new()
	zm.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	zm.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	zm.albedo_color = Color(1.0, 0.92, 0.3, 0.45)
	cyl.material = zm
	_zap = MeshInstance3D.new()
	_zap.name = "Zap"
	_zap.mesh = cyl
	_zap.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_zap.visible = false
	add_child(_zap)

	# chama: quad HDR billboard — o glow do Environment ja acende (sem luz)
	_flame_quad = QuadMesh.new()
	_flame_quad.size = Vector2(0.5, 0.5)
	var fm := StandardMaterial3D.new()
	fm.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	fm.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	fm.albedo_color = Color(2.6, 1.2, 0.35)
	_flame_quad.material = fm


func _fx_on(idx: int) -> void:
	if _fire_fx.size() >= MAX_FIRE_FX or _fire_fx.has(idx):
		return  # teto de aparelho: alem dele a celula queima sem particula
	var p := CPUParticles3D.new()
	p.amount = 10
	p.lifetime = 0.7
	p.randomness = 0.4
	p.direction = Vector3.UP
	p.spread = 20.0
	p.gravity = Vector3.ZERO
	p.initial_velocity_min = 1.2
	p.initial_velocity_max = 2.6
	p.scale_amount_min = 0.5
	p.scale_amount_max = 1.0
	p.emission_shape = CPUParticles3D.EMISSION_SHAPE_BOX
	p.emission_box_extents = Vector3(_cs * 0.3, 0.2, _cs * 0.3)
	p.mesh = _flame_quad
	p.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(p)
	p.position = to_local(_cell_center(idx)) + Vector3(0, 0.6, 0)
	_fire_fx[idx] = p


func _fx_off(idx: int) -> void:
	var p = _fire_fx.get(idx)
	if p != null:
		p.queue_free()
		_fire_fx.erase(idx)


func _ice_on(idx: int) -> void:
	if _ice.has(idx):
		return
	var c := _cell_center(idx)
	var wy := LAKE_SURFACE_Y if _mat[idx] == M_LAKE else MARSH_SURFACE_Y
	var pos := Vector3(c.x, wy + 0.1, c.z)
	var mi := MeshInstance3D.new()
	mi.mesh = _ice_mesh
	add_child(mi)
	mi.global_position = pos
	var shp := CollisionShape3D.new()
	shp.shape = _ice_shape
	_ice_body.add_child(shp)
	shp.global_position = pos
	_ice[idx] = [mi, shp]


func _ice_off(idx: int) -> void:
	var pair = _ice.get(idx)
	if pair != null:
		_free_pair(pair)
		_ice.erase(idx)


func _wall_on(idx: int) -> void:
	if _walls.has(idx):
		return
	var c := _cell_center(idx)
	var pos := Vector3(c.x, c.y + _wall_h * 0.5, c.z)
	var mi := MeshInstance3D.new()
	mi.mesh = _wall_mesh
	add_child(mi)
	mi.global_position = pos
	var shp := CollisionShape3D.new()
	shp.shape = _wall_shape
	_wall_body.add_child(shp)
	shp.global_position = pos
	_walls[idx] = [mi, shp]


func _wall_off(idx: int) -> void:
	var pair = _walls.get(idx)
	if pair != null:
		_free_pair(pair)
		_walls.erase(idx)


func _char_cell(idx: int) -> void:
	# a cobertura DESAPARECE de verdade: copa fora do mesh, toco de carvao,
	# tronco sem colisao (linha de tiro e caminho abertos) + mancha no chao
	for t in _cell_trees.get(idx, []):
		_island.set_tree_burned(int(t), true)
	_scorch_mm.set_instance_transform(_scorch_i % MAX_SCORCH,
			Transform3D(Basis(Vector3.UP, randf() * TAU), _cell_center(idx) + Vector3(0, 0.07, 0)))
	_scorch_i += 1


func _update_front_fx() -> void:
	if _burning.is_empty():
		_fire_light.visible = false
	else:
		var c := Vector3.ZERO
		for idx in _burning:
			c += _cell_center(idx)
		c /= float(_burning.size())
		_fire_light.visible = true
		_fire_light.global_position = c + Vector3(0, 2.2, 0)
	if _zap.visible:
		var any := false
		for idx in _remaining:
			if _state[idx] == S_ZAP:
				any = true
				break
		_zap.visible = any


func _free_pair(pair: Array) -> void:
	for n in pair:
		if is_instance_valid(n):
			n.queue_free()
