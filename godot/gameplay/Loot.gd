## LOOT NO CHAO — a arma arcana que se ve', se anda ate' e se pega (GDD §16.2).
##
## ORCAMENTO MOBILE (a regra que desenhou este arquivo): ZERO _process. Um loot
## nao pergunta todo frame "tem alguem perto?" — quem responde e' o Area3D dele,
## por sinal, no passo de fisica da engine. O unico movimento (girar/flutuar) e'
## um Tween em loop, que roda em C++ e nao volta pro GDScript. 16 loots no mapa
## custam 16 areas paradas.
##
## LEITURA A DISTANCIA — COR **+ FORMA** (GDD §10, lei): o feixe de luz e o
## marcador no chao mudam de COR *e* de FORMA por raridade (circulo/losango/
## triangulo), e a silhueta da arma tambem. Quem nao distingue cor le' a forma.
class_name Loot
extends Area3D

const RAIO_PEGAR := 2.2      # m — KNOB: raio em que o prompt aparece
const GIRO_S := 6.0          # s por volta — KNOB (lento; loot nao e' pirulito)
const FLUTUA_M := 0.16       # m de sobe-e-desce — KNOB
const ALTURA := 0.85         # m do chao ate' o centro do modelo
## KNOB — auto-iluminacao do CORPO do item no chao. Video do Diretor (26/08):
## em contra-luz o modelo Meshy vira VULTO PRETO — o feixe le', o item nao.
## Receita do Spellbreak (docs/referencias/SPELLBREAK.md, loot): item emissivo,
## que nao depende da luz da cena. Energia baixa de proposito: leitura a
## distancia, nao lanterna (o toon do projeto e' estilizado, nao neon).
const EMISSAO_CORPO := 0.35

## Centros dos POIs — ESPELHO de world/Island.gd (raia MUNDO). Duplicados de
## proposito: `world/` e' de outra raia e o loot nao pode depender do formato
## interno dela. Se a ilha mudar os POIs, muda estes 4 numeros.
const POIS := {
	"alagado": Vector2(-42, 36),
	"floresta": Vector2(-36, -40),
	"lago": Vector2(45, 18),
	"ruinas": Vector2(38, -44),
}

## QUANTOS NASCEM (KNOBs — a curva de poder da partida mora aqui):
##   varinha  comum e ESPALHADA: ninguem fica desarmado (GDD: todo mundo dropa
##            com uma; ate' a queda de paraquedas existir, o chao supre)
##   cajado   raro, 1 por POI: ir ao ponto de interesse tem que PAGAR
##   manopla  NAO NASCE NO CHAO. Lendaria = so' Bau Celestial (GDD §16.2).
##            O gancho e' bau_celestial() la' embaixo.
const QTD_VARINHA := 12
const QTD_CAJADO := 4
const SEED_LOOT := 1301       # seed fixo: mesma ilha, mesmo loot, sempre

var arma_id := "varinha"
## O elemento DESTA luva (viaja com ela no swap). DIRECAO.md §1.
var elemento := "fire"
var par: PackedStringArray = PackedStringArray()  # par fixo, so' manopla
var _pivo: Node3D


static func criar(p_arma_id: String, p_par := PackedStringArray(), p_elemento := "fire") -> Loot:
	var l := Loot.new()
	l.arma_id = p_arma_id
	l.par = p_par
	l.elemento = p_elemento
	return l


func _ready() -> void:
	# Grupo da BUSCA dos bots (Bot._loot_mais_perto): bot desarmado precisa
	# achar as luvas do chao sem varrer a arvore inteira.
	add_to_group("loot_arma")
	var col := CollisionShape3D.new()
	var sp := SphereShape3D.new()
	sp.radius = RAIO_PEGAR
	col.shape = sp
	add_child(col)
	monitoring = true
	_montar()
	body_entered.connect(_on_entrou)
	body_exited.connect(_on_saiu)


func _montar() -> void:
	if is_instance_valid(_pivo):
		_pivo.queue_free()
	_pivo = Node3D.new()
	_pivo.position.y = ALTURA
	var m := ArmaSlot.modelo(arma_id, elemento)  # mesmo modelo da mao: o que
	m.position.y = -0.3                          # voce ve' e' o que equipa
	m.scale = Vector3.ONE * 2.6  # luva e' pequena; no chao ela AMPLIA para ler de longe
	_emissao_corpo(m)
	_pivo.add_child(m)
	add_child(_pivo)
	var cor := Arma.cor(arma_id)
	_feixe(cor)
	_marcador(cor)
	## Giro + flutuacao em Tween (engine-side): custo de GDScript por frame = 0.
	var tw := create_tween().set_loops()
	tw.tween_property(_pivo, "rotation:y", TAU, GIRO_S).from(0.0)
	var tf := create_tween().set_loops()
	tf.tween_property(_pivo, "position:y", ALTURA + FLUTUA_M, 1.3) \
			.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tf.tween_property(_pivo, "position:y", ALTURA, 1.3) \
			.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)


## O CORPO do item acende na cor do ELEMENTO da luva (EMISSAO_CORPO acima).
## So' materiais SOMBREADOS (= os do .glb do Meshy): o fallback procedural ja'
## e' UNSHADED emissivo (ArmaSlot.mat_brilho) e nao precisa de ajuda.
## TRES armadilhas que este bloco desarma:
## 1. o material do .glb e' COMPARTILHADO entre instancias — mexer nele acende
##    a luva NA MAO de todo mundo. Por isso: duplicate() + override DE
##    SUPERFICIE, que vive no no' desta instancia e morre com ela (ao pegar,
##    a mao instancia modelo novo do .glb intacto — nada viaja).
## 2. o Meshy importa com emission_enabled=true mas emission=PRETO + textura
##    emissiva quase toda preta (medido 26/08): "ja e' emissivo" e' mentira.
## 3. a textura emissiva preta engoliria a cor — por isso ela e' anulada e
##    fica so' o banho chapado da cor do elemento.
func _emissao_corpo(raiz: Node) -> void:
	var cor := Projectile.tint(elemento)
	var pilha: Array = [raiz]
	while not pilha.is_empty():
		var n: Node = pilha.pop_back()
		for c in n.get_children():
			pilha.append(c)
		if not (n is MeshInstance3D):
			continue
		var mi := n as MeshInstance3D
		if mi.mesh == null:
			continue
		for s in mi.mesh.get_surface_count():
			var m: Material = mi.get_active_material(s)
			if not (m is StandardMaterial3D):
				continue
			var sm := m as StandardMaterial3D
			if sm.shading_mode == BaseMaterial3D.SHADING_MODE_UNSHADED:
				continue  # procedural: ja' se ilumina sozinho
			var dup := sm.duplicate() as StandardMaterial3D
			dup.emission_enabled = true
			dup.emission = cor
			dup.emission_texture = null
			dup.emission_energy_multiplier = EMISSAO_CORPO
			mi.set_surface_override_material(s, dup)


## Feixe vertical: o que faz o loot ser VISTO de longe. Altura por raridade —
## a lendaria e' quase o dobro da comum, entao da' pra escolher o rumo de longe.
func _feixe(cor: Color) -> void:
	var m := MeshInstance3D.new()
	var cyl := CylinderMesh.new()
	cyl.top_radius = 0.10
	cyl.bottom_radius = 0.22
	cyl.height = float(Arma.RARIDADES[Arma.raridade(arma_id)].feixe)
	cyl.radial_segments = 6
	m.mesh = cyl
	m.position.y = cyl.height * 0.5
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.cull_mode = BaseMaterial3D.CULL_DISABLED
	mat.albedo_color = Color(cor, 0.22)
	mat.emission_enabled = true
	mat.emission = cor
	mat.emission_energy_multiplier = 1.6
	m.material_override = mat
	m.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(m)


## Marcador no chao: a FORMA da raridade (GDD §10). Primitivas achatadas —
## circulo (torus), losango (caixa girada 45°), triangulo (prisma).
func _marcador(cor: Color) -> void:
	var m := MeshInstance3D.new()
	match Arma.forma(arma_id):
		"losango":
			var bx := BoxMesh.new()
			bx.size = Vector3(0.95, 0.03, 0.95)
			m.mesh = bx
			m.rotation.y = PI / 4.0
		"triangulo":
			var pr := PrismMesh.new()
			pr.size = Vector3(1.25, 0.03, 1.25)
			m.mesh = pr
		_:
			var to := TorusMesh.new()
			to.inner_radius = 0.46
			to.outer_radius = 0.60
			to.rings = 12
			m.mesh = to
	m.position.y = 0.06
	m.material_override = ArmaSlot.mat_brilho(cor, 1.4)
	m.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(m)


# ---------------------------------------------------------------- pegar

func _on_entrou(body: Node3D) -> void:
	var slot := ArmaSlot.de(body)
	if slot == null:
		return  # chao, muro, arvore: o Area3D ve' tudo, so' pawn interessa
	slot.registrar(self, true)
	_prompt(body, true)
	## AUTO-UPGRADE: ninguem recusa arma melhor. E' o que deixa o loop rodar
	## HOJE, sem o botao da HUD existir, e serve player e bot pelo MESMO
	## caminho. Sidegrade/downgrade continuam dependendo do prompt.
	if slot.auto_upgrade and Arma.tier(arma_id) > slot.tier():
		pegar(slot)


func _on_saiu(body: Node3D) -> void:
	var slot := ArmaSlot.de(body)
	if slot == null:
		return
	slot.registrar(self, false)
	_prompt(body, false)


## A HUD OBSERVA (nunca decide). So' o player emite: prompt de bot e' ruido.
func _prompt(body: Node, dentro: bool) -> void:
	if body == null or not is_instance_valid(body) or not body.is_in_group("player"):
		return
	var d := Arma.dados(arma_id)
	## O QUE ESTA' NO CHAO, POR EXTENSO: nome + elemento (ordem do Diretor,
	## 26/08). O elemento e' o que essa luva IMPOE ao disparo — quem passa por
	## cima decide com a informacao completa, e nao "Luva Comum" (qual delas?).
	## A manopla mostra o PAR; luva comum/conjurador, o elemento unico.
	var els := par if not par.is_empty() else PackedStringArray([elemento])
	Bus.loot_prompt.emit(Textos.arma_rotulo(str(d.nome), els),
			str(d.raridade), dentro)


## TROCA (nao consome): a arma velha fica no chao no lugar desta. E' o padrao
## de BR — trocar tem custo de decisao, e o mapa nao esvazia sozinho.
## Devolve false se ja' e' a mesma arma (nada acontece, sem sinal duplicado).
##
## O ATO (R22): esta e' a porta UNICA do "pegar" — botao da HUD, auto-upgrade e
## Bau Celestial passam todos por aqui, entao e' aqui que o gesto nasce. O
## ESTADO nao espera animacao (dedo que aperta tem que responder no mesmo
## frame); quem se sincroniza com a mao e' o EFEITO: a faisca sai em
## GESTO_CONTATO_S, o instante em que o braco do mago alcanca o chao.
func pegar(slot: ArmaSlot) -> bool:
	if slot == null or not is_instance_valid(slot) or slot.arma_id == arma_id:
		return false
	var velha := slot.arma_id
	var velho_par := slot.par
	var velho_el := slot.elemento
	var cor := Arma.cor(arma_id)  # cor do que esta' sendo PEGO (o swap vem abaixo)
	slot.equipar(arma_id, par, elemento)
	_prompt(slot.get_parent(), false)  # o prompt some: ja' pegou
	ArmaSlot.gesto(slot.get_parent())
	_faisca(cor)
	# MAOS NUAS nao viram loot: com todos caindo desarmados (DIRECAO.md §1) o
	# swap antigo deixaria uma "arma vazia" fantasma no chao — cada primeira
	# pegada da partida criaria uma. O loot e' CONSUMIDO; a troca de verdade
	# (arma por arma) continua deixando a velha no lugar, padrao de BR.
	if velha == "":
		queue_free()
		return true
	arma_id = velha
	par = velho_par
	elemento = velho_el
	_montar()
	return true


## O "peguei" no MUNDO (o da tela e' da HUD): um estouro curto na cor da
## raridade, no tempo do contato da mao. Adiado por SceneTreeTimer porque o
## relogio da engine e' o unico que nao custa um _process neste no'.
func _faisca(cor: Color) -> void:
	var pai := get_parent()
	var pos := global_position + Vector3(0, ALTURA, 0)
	var tree := get_tree()
	if tree == null:
		Projectile.burst(pai, pos, cor, 14)  # sem arvore, sem relogio: sai agora
		return
	tree.create_timer(ArmaSlot.GESTO_CONTATO_S).timeout.connect(
			func() -> void: Projectile.burst(pai, pos, cor, 14))


# ---------------------------------------------------------------- distribuicao

## Espalha o loot da partida. DETERMINISTICO: mesmo seed = mesmas posicoes e
## mesmas armas, sempre (spawns confiaveis sao contrato do projeto). Devolve
## quantos nasceram.
## `island` so' precisa responder height(x, z) — se nao responder (ilha de
## fallback), tudo nasce em y = 1.0 e o jogo continua de pe'.
static func espalhar(parent: Node3D, island: Node, p_seed := SEED_LOOT) -> int:
	var rng := RandomNumberGenerator.new()
	rng.seed = p_seed
	var n := 0
	# varinhas: aneis largos cobrindo a ilha inteira — nunca ficar sem arma
	for i in QTD_VARINHA:
		var ang := TAU * float(i) / float(QTD_VARINHA) + rng.randf_range(-0.22, 0.22)
		var raio := rng.randf_range(14.0, 62.0)
		# O ELEMENTO CICLA pelos 5 (deterministico pelo indice): a ilha inteira
		# oferece variedade — quem quer um elemento especifico tem que ANDAR.
		if _por(parent, island, rng, "varinha", Vector2(cos(ang), sin(ang)) * raio,
				Balance.ELEMENTS[i % Balance.ELEMENTS.size()]):
			n += 1
	# cajados: 1 por POI — ir ao ponto de interesse tem que pagar.
	# A ordem das chaves de um Dictionary const e' a de declaracao (por isso
	# elas estao em ordem alfabetica la' em cima): determinismo sem sort.
	var nomes: Array = POIS.keys()
	for i in mini(QTD_CAJADO, nomes.size()):
		var c: Vector2 = POIS[nomes[i]]
		var a := rng.randf() * TAU
		if _por(parent, island, rng, "cajado",
				c + Vector2(cos(a), sin(a)) * rng.randf_range(6.0, 15.0),
				Balance.ELEMENTS[(i + 2) % Balance.ELEMENTS.size()]):
			n += 1
	return n


## Tenta pousar um loot perto de `alvo`. Rejeita agua, praia e mar pela ALTURA
## do terreno (o lago fica em -2.4 e o alagado em 0.45 — o corte em 1.4 exclui
## os dois sem o loot precisar conhecer o formato da ilha).
static func _por(parent: Node3D, island: Node, rng: RandomNumberGenerator,
		arma_id_: String, alvo: Vector2, elemento_ := "fire") -> bool:
	## A busca ABRE a cada tentativa (4m -> 37m). Sem isso o cajado do lago e o
	## do alagado simplesmente NAO nasciam: os dois POIs sao agua, e um jitter
	## fixo de 9m nunca alcancava a margem. Abrindo em espiral o loot pousa na
	## BEIRA do POI — que e' onde ele devia estar mesmo.
	for tentativa in 12:
		var busca := 4.0 + float(tentativa) * 3.0
		var p := alvo if tentativa == 0 else alvo + Vector2(
				rng.randf_range(-busca, busca), rng.randf_range(-busca, busca))
		if p.length() > 70.0:
			continue
		var h := 1.0
		if island != null and is_instance_valid(island) and island.has_method("height"):
			h = float(island.height(p.x, p.y))
			if h < 1.4 or h > 8.5:
				continue
		var l := criar(arma_id_, PackedStringArray(), elemento_)
		parent.add_child(l)
		l.global_position = Vector3(p.x, h, p.y)
		return true
	return false


## GANCHO DO BAU CELESTIAL (GDD §16.2) — a UNICA porta da manopla. Quem chama
## e' gameplay/BauCelestial.gd, no instante em que a canalizacao completa:
##   Loot.bau_celestial(arena, pos_do_bau, indice_do_par)
## O par de elementos e' FIXO por manopla e escolhido pelo INDICE (determinismo:
## o mesmo bau da' a mesma manopla para todo mundo na partida).
static func bau_celestial(parent: Node3D, pos: Vector3, indice := 0) -> Loot:
	var par_ := PackedStringArray(Arma.PARES_MANOPLA[indice % Arma.PARES_MANOPLA.size()])
	var l := criar("manopla", par_)
	parent.add_child(l)
	l.global_position = pos
	return l
