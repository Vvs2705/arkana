## O CASTELO VOADOR — o transporte que abre a partida (GDD §18.2 e o Degrau 3:
## "castelo voador, queda de vassoura, zona fechando, escudo evolutivo").
## Ele cruza o mapa numa rota RETA e PUBLICA; o jogador viaja pendurado no
## portao e escolhe a hora de saltar. Quem manda na queda em si e'
## gameplay/queda/Queda.gd — este arquivo so' sabe duas coisas: voar em linha
## reta e ser legivel de longe.
##
## ORCAMENTO MOBILE (a mesma lei da Zona): ZERO _process. A travessia inteira
## e' UM Tween sobre `position` — engine-side, nenhuma linha de GDScript por
## frame. No fim da rota o castelo se apaga sozinho.
##
## DETERMINISTICO: a rota sai de um seed proprio (nao o do Loot nem o da Zona),
## entao mexer no loot nao muda por onde o castelo passa.
class_name Castelo
extends Node3D

## Cena opcional: se a raia de ARTE quiser trocar o castelo procedural por um
## modelado, basta ela existir com este script na raiz. Ausente, o jogo boota
## igual (fiacao defensiva, regra 1 do projeto).
const CENA := "res://world/Castelo.tscn"

## Altura de voo, em metros acima do nivel do mar da ilha. E' o TETO da queda:
## 320m com queda de 55 m/s e planeio de 12 m/s dao ~10s de ar — a janela que
## o celular aguenta com o dedo na tela sem virar tempo morto (o Fortnite gasta
## ~15s num aparelho que nao esta' na mao de ninguem no onibus).
## SUBIR = mais leitura de mapa e mais alcance horizontal, queda mais lenta;
## DESCER = o salto vira formalidade, nao da' tempo de escolher destino.
const ALTURA := 320.0

## Segundos de ponta a ponta. E' a JANELA DE DECISAO do jogador, nao a
## velocidade do castelo: quem nao saltar ate' o fim e' empurrado (Queda._viajar).
## NAO escala com o tamanho do mapa DE PROPOSITO — mapa maior so' quer dizer
## castelo mais rapido, com a mesma janela de 22s para ler o terreno e decidir.
## AUMENTAR = abertura arrastada, todo mundo pousa junto no fim da rota;
## DIMINUIR = nao da' tempo de ler o mapa e a escolha vira dado.
const DURACAO := 22.0

## Ponta da rota como fracao do lado do mapa. 0.62 x 180m = 111m do centro,
## ou seja, o castelo ENTRA de fora da ilha e SAI pelo outro lado — a rota
## cobre o mapa inteiro em vez de comecar em cima dele.
const MARGEM := 0.62

## Desvio lateral maximo da rota, em fracao do lado do mapa. Sem ele toda
## partida abriria com o castelo passando exatamente pelo centro, e o vale
## central seria sempre a queda obvia. AUMENTAR = rotas rasantes na borda, com
## metade do mapa longe demais; DIMINUIR = todo mundo cai no mesmo lugar.
const DESVIO := 0.18

## Seed proprio: mexer no loot (Loot.gd) ou nos circulos (Zona.gd) nao pode
## mudar por onde o castelo passa.
const SEED_ROTA := 3103

## So' o FALLBACK de quando nao ha' ilha para consultar (selftest, cena solta).
## O numero de verdade e' Island.SIZE, lido em runtime — a ilha esta' crescendo
## nesta mesma fase e cravar 180 aqui quebraria a rota em silencio.
const LADO_PADRAO := 180.0

var plano: Dictionary = {}   # {inicio: Vector3, fim: Vector3, duracao: float}


## Lado do terreno em metros, LIDO da constante de quem esta' no mapa. Nao ha'
## `class_name Island` para referenciar (nem deve haver acoplamento), entao a
## constante sai do proprio script do no'. Ilha ausente ou sem SIZE: LADO_PADRAO.
static func lado(island: Node) -> float:
	if island != null and is_instance_valid(island):
		var scr: Script = island.get_script()
		if scr is GDScript:
			var consts: Dictionary = (scr as GDScript).get_script_constant_map()
			if consts.has("SIZE"):
				return float(consts["SIZE"])
	return LADO_PADRAO


## A rota inteira, de uma vez e SEM mundo — e' o que torna o determinismo
## testavel sem rodar partida (`Castelo.rota(180.0, seed)`).
static func rota(lado_m: float, p_seed := SEED_ROTA) -> Dictionary:
	var rng := RandomNumberGenerator.new()
	rng.seed = p_seed
	var ang := rng.randf() * TAU
	var dir := Vector3(cos(ang), 0.0, sin(ang))
	var lat := Vector3(-dir.z, 0.0, dir.x) * rng.randf_range(-DESVIO, DESVIO) * lado_m
	var alcance := lado_m * MARGEM
	var alto := Vector3(0.0, ALTURA, 0.0)
	return {
		"inicio": lat - dir * alcance + alto,
		"fim": lat + dir * alcance + alto,
		"duracao": DURACAO,
	}


## Poe um castelo em rota sobre `parent` (a Arena da partida: restart = castelo
## novo, sem nada atravessando partida).
static func criar(parent: Node3D, island: Node, p_seed := SEED_ROTA) -> Castelo:
	var c := _instanciar()
	c.name = "Castelo"
	c.plano = rota(lado(island), p_seed)
	parent.add_child(c)
	return c


static func _instanciar() -> Castelo:
	if ResourceLoader.exists(CENA):
		var packed: Variant = load(CENA)
		if packed is PackedScene:
			var n: Node = (packed as PackedScene).instantiate()
			if n is Castelo:
				return n as Castelo
			n.free()
	return Castelo.new()


func _ready() -> void:
	if plano.is_empty():
		plano = rota(LADO_PADRAO)
	if get_child_count() == 0:
		_montar_visual()   # cena de arte, se existir, ja' trouxe a malha dela
	var ini: Vector3 = plano.inicio
	var fim: Vector3 = plano.fim
	position = ini
	if ini.distance_to(fim) > 0.001:
		look_at_from_position(ini, fim, Vector3.UP)  # a proa aponta pra rota
	## A HUD/minimapa desenham a linha por onde da' pra saltar. UI OBSERVA.
	Bus.castelo_rota.emit(ini, fim, float(plano.duracao))
	var tw := create_tween()
	tw.tween_property(self, "position", fim, float(plano.duracao))
	tw.tween_callback(queue_free)  # saiu do mapa: some (nada parado no ceu)


# ------------------------------------------------------------------ visual

## PROCEDURAL, zero binario (regra do projeto). O castelo e' visto a 300m de
## distancia e por ~30s por partida: 6 malhas sem sombra bastam para ler
## "castelo" na silhueta, e sombra a 320m so' custaria atlas.
func _montar_visual() -> void:
	var pedra := StandardMaterial3D.new()
	pedra.albedo_color = Color(0.42, 0.40, 0.47)
	var telhado := StandardMaterial3D.new()
	telhado.albedo_color = Color(0.35, 0.22, 0.42)
	telhado.emission_enabled = true
	telhado.emission = Color("b06cff")          # violeta arcano (paleta GDD §10)
	telhado.emission_energy_multiplier = 0.6
	# base: a rocha flutuante, larga em cima e em ponta embaixo
	# de ponta-cabeca: e' a silhueta de rocha flutuante, larga em cima e em bico
	_peca(_prisma(30.0, 14.0, 22.0), Vector3(0, -7, 0), pedra, PI)
	_peca(_caixa(26.0, 5.0, 18.0), Vector3(0, 2.5, 0), pedra)
	# tres torres — a silhueta que faz o jogador reconhecer o castelo de longe
	for p in [Vector3(-9, 9, -6), Vector3(9, 9, -6), Vector3(0, 11, 6)]:
		_peca(_cilindro(3.0, 3.0, 13.0), p, pedra)
		_peca(_cilindro(0.0, 4.2, 6.0), p + Vector3(0, 9.5, 0), telhado)


func _peca(m: Mesh, pos: Vector3, mat: Material, giro_z := 0.0) -> void:
	var mi := MeshInstance3D.new()
	mi.mesh = m
	mi.position = pos
	mi.rotation.z = giro_z
	mi.material_override = mat
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(mi)


func _caixa(x: float, y: float, z: float) -> BoxMesh:
	var b := BoxMesh.new()
	b.size = Vector3(x, y, z)
	return b


func _prisma(x: float, y: float, z: float) -> PrismMesh:
	var p := PrismMesh.new()
	p.size = Vector3(x, y, z)
	p.left_to_right = 0.5
	return p


func _cilindro(topo: float, base: float, h: float) -> CylinderMesh:
	var c := CylinderMesh.new()
	c.top_radius = topo
	c.bottom_radius = base
	c.height = h
	c.radial_segments = 10   # a 300m ninguem conta lado de torre
	c.rings = 1
	return c
