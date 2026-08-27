## O KIT DA ILHA FRATURADA — as pecas esculpidas que plantam o mapa.
##
## POR QUE ELE EXISTE (27/08/2026, ordem do Diretor: "quero um novo mapa, tao bom
## quanto o do Spellbreak"): gerar a ilha INTEIRA numa peca so' no Meshy deu um
## borrao cinza de 3,1 M de faces — a imagem aerea tem milhares de detalhes
## minusculos e o gerador faz media deles. Peca ISOLADA, com fundo neutro e
## poucas formas grandes, ele esculpe com precisao. O mapa se monta com PECAS.
##
## E' a mesma receita do Spellbreak: silhueta grande e legivel, repetida com
## rotacao e escala. De 200 m ninguem le' detalhe — le' forma e cor (GDD §10).
##
## ORCAMENTO, MEDIDO (27/08): cada peca sai do Meshy com ~3.000.000 de faces e
## textura 2K, ou seja 8-12 MB. Depois do Blender (decimate + textura 512) sao
## 6.000 faces e ~0,3 MB. O kit inteiro cabe em 6 MB.
##   * a PONTE-RAIZ ficou em 40.000 de proposito: a 6.000 ela se despedacava —
##     estrutura fina e torcida nao sobrevive ao collapse. Foi VISTO num render.
##   * os JUNCOS ficaram de fora: sao milhares de laminas soltas e o decimate
##     nao junta geometria desconexa (travou em 149.000). A grama procedural que
##     o Island ja' tem cobre esse papel melhor e mais barato.
##
## CADA PECA E' UM MULTIMESH: 1 draw call por TIPO, nao por copia. E' o que
## permite plantar centenas de rochas sem estourar o orcamento de draw call que
## o mapa de 600 m ja' mediu em 67.
class_name KitCenario
extends Node3D

const DIR := "res://world/modelos/kit"

## A MESHY NORMALIZA TUDO PARA ~1,9 UNIDADE. Sem escala, uma rocha de 6 m e um
## pedestal de 2 m chegam do mesmo tamanho. `altura_m` e' a altura REAL da peca
## no mundo, e e' o unico numero de design aqui: e' ele que decide se a rocha e'
## cobertura de agachar ou paredao de escalar.
##
## `n` e' quantas copias por ILHA DE REFERENCIA (raio 132 m). O plantio escala
## por AREA, entao o numero acompanha o mapa sozinho.
##
## `colide` — o que o corpo do jogador bate. Arvore e rocha sim; piso de runa e
## arco nao (o arco e' passagem, e uma capsula de colisao nele viraria parede).
const PECAS := [
	{"id": "17-rocha-basalto-modular", "altura_m": 15.0, "n": 11, "colide": true,
		"onde": "aberto", "gira": true, "escala_var": 0.35},
	{"id": "18-rocha-vulcanica-cobertura", "altura_m": 7.0, "n": 18, "colide": true,
		"onde": "aberto", "gira": true, "escala_var": 0.30},
	{"id": "19-arco-calcario-nymara", "altura_m": 14.0, "n": 4, "colide": false,
		"onde": "agua", "gira": true, "escala_var": 0.15},
	{"id": "20-ponte-raiz-aeris", "altura_m": 4.2, "n": 3, "colide": true,
		"onde": "floresta", "gira": true, "escala_var": 0.20},
	{"id": "21-pilar-condutor-fulgar", "altura_m": 28.0, "n": 5, "colide": true,
		"onde": "alto", "gira": true, "escala_var": 0.12},
	{"id": "28-pedestal-de-arma", "altura_m": 2.6, "n": 5, "colide": true,
		"onde": "poi", "gira": true, "escala_var": 0.0},
	## A ARVORE DOURADA FICOU DE FORA (27/08): as folhas dela sao milhares de
	## cascas FLUTUANTES, que nao compartilham vertice — nem soldando a 4 mm o
	## decimate desce de 140.000 faces. A 55 copias seriam 7,7 M de triangulos
	## sozinha. A carbonizada faz o mesmo papel de silhueta por 1.499.
	## Volta quando alguem reconstruir a copa como cartao, nao como casca.
	{"id": "30-arvore-carbonizada-renascendo", "altura_m": 12.0, "n": 12, "colide": true,
		"onde": "aberto", "gira": true, "escala_var": 0.30},
	{"id": "32-piso-runa-reativa", "altura_m": 0.6, "n": 7, "colide": false,
		"onde": "poi", "gira": true, "escala_var": 0.0},
]

## Raio da ilha de referencia para o qual os `n` acima foram escritos.
const RAIO_REF := 132.0
const SEED_KIT := 4801

## Distancia em que a peca some. Nao e' gosto: e' o mesmo corte por celula que a
## grama usa. Rocha e arvore precisam ser vistas de longe (sao marco de
## navegacao); piso e pedestal nao.
## MEDIDO E APERTADO (27/08). Com 220/90 o kit desenhava 363.926 tris e 114 draw
## calls NA TELA — mais que a ilha inteira (184.967 / 67) e o triplo do APK que
## ja' roda no aparelho. Com 140/70 cai para o orcamento abaixo.
## KNOB: subir devolve marco visivel de mais longe e custa quadro; descer faz a
## rocha aparecer na cara do jogador (pop-in), que e' pior que nao ter rocha.
const VIS_MARCO := 240.0
const VIS_CHAO := 70.0

var _plantadas := 0


## Planta o kit sobre a ilha. `island` precisa responder height/pode_pousar/pois
## — o mesmo contrato que Loot e Zona ja' usam.
static func plantar(parent: Node3D, island: Node, p_seed := SEED_KIT) -> KitCenario:
	var k := KitCenario.new()
	k.name = "KitCenario"
	parent.add_child(k)
	k._montar(island, p_seed)
	return k


func total() -> int:
	return _plantadas


func _montar(island: Node, p_seed: int) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = p_seed
	var raio := RAIO_REF
	if island != null and is_instance_valid(island) and island.has_method("raio_terra"):
		raio = float(island.raio_terra())
	# a densidade e' por AREA: mapa 2x maior recebe 4x as pecas
	var area := (raio / RAIO_REF) * (raio / RAIO_REF)

	for peca: Dictionary in PECAS:
		var cena := "%s/%s.glb" % [DIR, str(peca.id)]
		if not ResourceLoader.exists(cena):
			continue
		var malha := _malha_de(cena)
		if malha == null:
			continue
		var alvo := int(round(float(peca.n) * area))
		var xf: Array[Transform3D] = []
		var base := _escala_base(malha, float(peca.altura_m))
		var tentativas := 0
		while xf.size() < alvo and tentativas < alvo * 40:
			tentativas += 1
			var p: Variant = _sortear(rng, island, raio, str(peca.onde))
			if p == null:
				continue
			var pos: Vector2 = p
			var perto := false
			for t in xf:
				if Vector2(t.origin.x, t.origin.z).distance_to(pos) < float(peca.altura_m) * 1.6:
					perto = true
					break
			if perto:
				continue
			var h := 0.0
			if island != null and island.has_method("height"):
				h = float(island.height(pos.x, pos.y))
			var e: float = base * (1.0 + rng.randf_range(-1.0, 1.0) * float(peca.escala_var))
			var giro: float = rng.randf() * TAU if bool(peca.gira) else 0.0
			var b := Basis(Vector3.UP, giro).scaled(Vector3(e, e, e))
			# afunda um dedo no chao: peca pousada exata mostra fresta na encosta
			xf.append(Transform3D(b, Vector3(pos.x, h - float(peca.altura_m) * 0.04, pos.y)))
		if xf.is_empty():
			continue
		_plantadas += xf.size()
		var vis: float = VIS_MARCO if float(peca.altura_m) >= 3.0 else VIS_CHAO
		_multimesh_grade(malha, xf, str(peca.id), vis, raio)
		if bool(peca.colide):
			_colisao(xf, float(peca.altura_m))


## A malha da peca, achatada num Mesh so'. O .glb da Meshy vem com um Node3D
## raiz e uma MeshInstance3D dentro; MultiMesh precisa do Mesh puro.
func _malha_de(cena: String) -> Mesh:
	var ps: PackedScene = load(cena)
	if ps == null:
		return null
	var n: Node = ps.instantiate()
	var achado: Mesh = null
	var pilha: Array[Node] = [n]
	while not pilha.is_empty():
		var c: Node = pilha.pop_back()
		for f in c.get_children():
			pilha.append(f)
		if c is MeshInstance3D and (c as MeshInstance3D).mesh != null:
			achado = (c as MeshInstance3D).mesh
			break
	## O GRAMPO DO METAL, a licao do mago preto (core/Pbr.gd). Os modelos da
	## Meshy chegam com metallic = 1,00 e metal e' quase todo REFLEXO: sem
	## reflection probe (nao ha' — nao cabe no orcamento mobile) o renderer
	## devolve BREU. VISTO em 27/08 num render de dentro do jogo: as rochas do
	## kit sairam pretas contra a campina, exatamente como a Pyra saiu em 26/08.
	## Domar ANTES de o Mesh ir para o MultiMesh, porque dali em diante o
	## material e' compartilhado por todas as copias.
	Pbr.domar(n)
	n.free()
	return achado


## De quanto multiplicar a peca para ela ter `altura_m` metros no mundo.
func _escala_base(m: Mesh, altura_m: float) -> float:
	var caixa := m.get_aabb()
	var alto := maxf(caixa.size.y, 0.001)
	return altura_m / alto


## Onde a peca pode nascer. Cada `onde` e' uma REGRA DE DESIGN, nao um filtro
## tecnico: e' o que faz o mapa ter regioes com cara propria em vez de ruido
## espalhado por igual.
func _sortear(rng: RandomNumberGenerator, island: Node, raio: float, onde: String) -> Variant:
	var pois: Dictionary = {}
	if island != null and island.has_method("pois"):
		pois = island.pois()
	var p := Vector2.ZERO
	match onde:
		"poi", "floresta", "agua":
			if pois.is_empty():
				return null
			var nomes: Array = pois.keys()
			var alvo: String = str(nomes[rng.randi() % nomes.size()])
			if onde == "floresta":
				alvo = "floresta" if pois.has("floresta") else alvo
			elif onde == "agua":
				alvo = "lago" if pois.has("lago") else alvo
			var poi: Dictionary = pois[alvo]
			var c: Vector2 = poi.centro
			var r := float(poi.raio)
			var a := rng.randf() * TAU
			var d := sqrt(rng.randf()) * r * (1.5 if onde == "agua" else 1.0)
			p = c + Vector2(cos(a), sin(a)) * d
		"alto":
			# marco alto vai para terreno alto: e' o que se ve' da queda
			var melhor := Vector2.ZERO
			var melhor_h := -1e9
			for _i in 24:
				var a2 := rng.randf() * TAU
				var d2 := sqrt(rng.randf()) * raio * 0.9
				var q := Vector2(cos(a2), sin(a2)) * d2
				var hq := float(island.height(q.x, q.y)) if island != null and island.has_method("height") else 0.0
				if hq > melhor_h:
					melhor_h = hq
					melhor = q
			p = melhor
		_:
			var a3 := rng.randf() * TAU
			var d3 := sqrt(rng.randf()) * raio * 0.92
			p = Vector2(cos(a3), sin(a3)) * d3
	if island != null and island.has_method("pode_pousar") and not island.pode_pousar(p.x, p.y):
		return null
	return p


## EM GRADE, e cada celula NO SEU LUGAR. Nao e' detalhe: `visibility_range` mede
## a distancia da camera ate' a ORIGEM DO NO'. Um MultiMesh unico com as
## transformadas do mundo assadas fica em (0,0,0), e o corte vira tudo-ou-nada —
## foi exatamente o defeito medido na grama em 27/08, e eu o repeti aqui na
## primeira versao: 5,1 M de triangulos desenhando sempre.
## A celula tem ~55 m, um pouco menor que o corte mais curto (90 m).
func _multimesh_grade(m: Mesh, xf: Array[Transform3D], nm: String, vis: float,
		raio: float) -> void:
	var passo := 55.0
	var cel := {}
	for t in xf:
		var k := Vector2i(int(floor(t.origin.x / passo)), int(floor(t.origin.z / passo)))
		if not cel.has(k):
			cel[k] = [] as Array[Transform3D]
		cel[k].append(t)
	for k: Vector2i in cel:
		var centro := Vector3((float(k.x) + 0.5) * passo, 0.0, (float(k.y) + 0.5) * passo)
		var lista: Array[Transform3D] = cel[k]
		var mm := MultiMesh.new()
		mm.transform_format = MultiMesh.TRANSFORM_3D
		mm.mesh = m
		mm.instance_count = lista.size()
		for i in lista.size():
			var t: Transform3D = lista[i]
			t.origin -= centro          # instancia em coordenada LOCAL da celula
			mm.set_instance_transform(i, t)
		var mmi := MultiMeshInstance3D.new()
		mmi.name = "%s_%d_%d" % [nm, k.x, k.y]
		mmi.multimesh = mm
		mmi.position = centro
		mmi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		if vis > 0.0:
			mmi.visibility_range_end = vis
			mmi.visibility_range_end_margin = vis * 0.10
		add_child(mmi)


## COLISAO POR CAPSULA, nao trimesh. Uma peca de 6.000 faces vira 6.000 faces de
## colisao, e o kit inteiro estouraria o custo de fisica sozinho. A capsula
## acerta o que o jogo precisa (nao atravessar a rocha) por uma fracao do preco —
## e e' o mesmo padrao que as arvores do Island ja' usam.
func _colisao(xf: Array[Transform3D], altura_m: float) -> void:
	var corpo := StaticBody3D.new()
	corpo.name = "Colisao"
	add_child(corpo)
	for t in xf:
		var cs := CollisionShape3D.new()
		var forma := CylinderShape3D.new()
		forma.height = altura_m
		forma.radius = maxf(altura_m * 0.28, 0.5)
		cs.shape = forma
		cs.position = t.origin + Vector3(0, altura_m * 0.5, 0)
		corpo.add_child(cs)
