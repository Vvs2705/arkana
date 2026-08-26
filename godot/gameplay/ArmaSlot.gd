## O SLOT DE ARMA DO PAWN — acoplamento POR FORA (composicao).
## E' um Node3D filho do pawn. Player.gd/Pawn.gd/Bot.gd NAO precisam declarar
## nada: quem quiser saber a arma de alguem chama ArmaSlot.de(pawn), e quem
## nao tiver slot continua jogando com o perfil cru do elemento (varinha).
## Foi assim de proposito — a raia de personagens esta' nos mesmos arquivos, e
## sistema que exige edicao cruzada para BOOTAR e' sistema que trava a fase.
##
## Guarda: arma equipada, o par fixo da manopla e a lista de loots ao alcance.
## NAO tem _process: quem avisa "tem loot perto" e' o Area3D do proprio loot.
class_name ArmaSlot
extends Node3D

## Mao esquerda aproximada, em coordenadas locais do pawn. PENDENCIA declarada:
## quando characters/Mage.tscn expuser um BoneAttachment3D de mao, o modelo
## passa a pendurar la' e esta constante morre.
const MAO_OFFSET := Vector3(0.34, 1.16, 0.22)

## O GESTO DE PEGAR. Duracao da anim "pegar" do mago e o instante em que a mao
## ALCANCA o objeto (characters/Mage.gd, _anim_pegar). Sao numeros do MUNDO
## (segundos de animacao), nao do dedo — por isso nao vivem em dp.
const GESTO_S := 0.65
const GESTO_CONTATO_S := 0.24
const META_GESTO := "gesto_pegar_inicio"

## VAZIO = MAOS NUAS (26/08 — DIRECAO.md §1: "todos caem sem luva; achar a
## primeira e' a corrida de abertura"). Ate' entao todo slot nascia com varinha
## e ninguem jamais estava desarmado — a Lei das Luvas nao existia no codigo.
var arma_id := ""
var par: PackedStringArray = PackedStringArray()  # so' a manopla usa
## O ELEMENTO DA LUVA (26/08 — DIRECAO.md §1: "cada luva tem sua cor e seu
## elemento proprio"). Vazio = luva legada sem elemento (testes antigos): o
## disparo cai no elemento escolhido, como antes. A manopla ignora isto — o
## par fixo dela continua mandando.
var elemento := ""
## Pega sozinho quando o loot e' de tier ESTRITAMENTE melhor. E' o que faz o
## loop funcionar hoje, sem a HUD: ninguem recusa um upgrade. O prompt do Bus
## continua saindo para o botao "PEGAR" cobrir sidegrade/downgrade.
var auto_upgrade := true

var _candidatos: Array[Node] = []   # loots com o pawn dentro do raio
var _modelo: Node3D
var _alt := 0                       # alternancia dos 2 elementos da manopla


## Acha o slot de um pawn (ou null). Barato: pawn tem poucos filhos.
static func de(pawn: Node) -> ArmaSlot:
	if pawn == null or not is_instance_valid(pawn):
		return null
	for c in pawn.get_children():
		if c is ArmaSlot:
			return c as ArmaSlot
	return null


## O SPEC QUE O DISPARO USA. Sem slot -> perfil cru do elemento (= varinha).
## E' este fallback que garante "zero regressao" em quem ainda nao foi costurado.
static func spec_de(pawn: Node, el: String) -> Dictionary:
	var s := de(pawn)
	if s == null:
		return Arma.base(el)
	return s.spec(el)


static func arma_de(pawn: Node) -> String:
	var s := de(pawn)
	return "varinha" if s == null else s.arma_id


## A LEI DAS LUVAS (DIRECAO.md §1): sem luva NAO ha' ataque basico. Pawn SEM
## slot continua atacando (fiacao defensiva: pawn de teste e cena avulsa nao
## conhecem o sistema); pawn COM slot e maos nuas esta' desarmado de verdade.
## Habilidades (passiva/tatica/suprema) NAO passam por aqui — sao natas.
static func armado_de(pawn: Node) -> bool:
	var s := de(pawn)
	return s == null or s.arma_id != ""


func armado() -> bool:
	return arma_id != ""


func _ready() -> void:
	_montar_visual()


func spec(el: String) -> Dictionary:
	return Arma.spec(el, arma_id)


func dados() -> Dictionary:
	return Arma.dados(arma_id)


func elementos(escolhido: String) -> PackedStringArray:
	if int(Arma.dados(arma_id).elementos) <= 1 and elemento != "":
		return PackedStringArray([elemento])
	return Arma.elementos_de(arma_id, par, escolhido)


## Elemento DESTE disparo. Varinha/cajado: o do carrossel. Manopla: alterna
## entre os 2 fixos, tiro a tiro — e' assim que "porta 2 elementos
## simultaneos" vira coisa que o dedo sente, sem inventar UI nova.
func elemento_do_disparo(escolhido: String) -> String:
	var els := elementos(escolhido)
	if els.size() >= 2:
		_alt = (_alt + 1) % els.size()
		return els[_alt]
	# A LUVA MANDA (26/08): quem tem elemento proprio dispara ELE, nao o do
	# carrossel. Luva sem elemento (legado/teste) segue o escolhido.
	return elemento if elemento != "" else escolhido


## Troca a arma ativa. p_par so' importa para a manopla (par fixo, sem troca);
## p_elemento e' o elemento DA LUVA (fica nela, viaja com ela no swap).
func equipar(id: String, p_par: PackedStringArray = PackedStringArray(),
		p_elemento := "") -> void:
	if not Arma.existe(id):
		return
	arma_id = id
	par = p_par
	elemento = p_elemento
	_alt = 0
	_montar_visual()
	var d := dados()
	# O dono e o PAI: o slot vive pendurado no pawn (ver ArmaSlot.de()).
	Bus.weapon_equipped.emit(get_parent(), id, str(d.nome), str(d.raridade), elementos(""))


func tier() -> int:
	## Maos nuas = tier -1: QUALQUER luva do chao e' upgrade. Sem isto,
	## Arma.tier("") caia no fallback varinha (tier 0) e o auto-upgrade do bot
	## recusava a primeira varinha da vida dele — bot desarmado para sempre.
	if arma_id == "":
		return -1
	return Arma.tier(arma_id)


# ---------------------------------------------------------------- loot

func registrar(loot: Node, dentro: bool) -> void:
	if dentro:
		if not _candidatos.has(loot):
			_candidatos.append(loot)
	else:
		_candidatos.erase(loot)


## Pega o loot mais proximo ao alcance. Devolve true se equipou algo.
## Chamada pela HUD (botao PEGAR) e pelo auto-upgrade do proprio loot.
func pegar() -> bool:
	var melhor: Node = null
	var d2 := INF
	for c in _candidatos:
		if not is_instance_valid(c):
			continue
		var dd: float = global_position.distance_squared_to((c as Node3D).global_position)
		if dd < d2:
			d2 = dd
			melhor = c
	if melhor == null:
		return false
	return bool(melhor.pegar(self))


# ---------------------------------------------------------------- o gesto

## O ATO DE PEGAR (pedido do Diretor: "gesto de pegar o item, abrir o bau").
## Um lugar so' para os dois atos — item do chao (Loot.pegar) e canalizacao do
## Bau Celestial — porque e' o MESMO movimento: agacha, estende o braco, volta.
## A animacao mora no visual do mago; aqui esta' a UNICA chamada dela no jogo.
##
## POR QUE NAO `pawn.anim("pegar")`, que seria o caminho obvio: o Player
## reescreve a animacao TODO frame (`anim(locomotion_anim())` no fim do
## _physics_process) e Pawn.anim() so' repassa quando o NOME muda. Pedindo por
## la', o gesto seria apagado no frame seguinte — duraria 16ms. Tocando direto
## no visual, o Pawn continua achando que esta' em "idle"/"run" e nao repete a
## chamada: o gesto roda inteiro. No fim devolvemos o corpo para a pose de
## locomocao, senao o mago congela agachado (a anim e' de disparo unico).
##
## Devolve false quando o pawn nao tem visual com animacao (fallback
## procedural, pawn de teste): fiacao defensiva, o ato acontece do mesmo jeito.
static func gesto(pawn: Node) -> bool:
	var vis := _visual_animado(pawn)
	if vis == null:
		return false
	## Carimbo do gesto MAIS NOVO. Sem ele o fim de um gesto apagaria o
	## seguinte — e' o caso do bau, que repete o movimento a cada GESTO_S.
	var inicio := Time.get_ticks_msec()
	pawn.set_meta(META_GESTO, inicio)
	vis.call("play_anim", "pegar")
	var tree := vis.get_tree()
	if tree == null:
		return true  # fora da arvore nao ha' relogio; a pose volta no proximo anim()
	tree.create_timer(GESTO_S).timeout.connect(func() -> void: _voltar(pawn, inicio))
	return true


## Devolve o corpo para a pose que o gameplay ja' acha que esta' tocando.
static func _voltar(pawn: Node, inicio: int) -> void:
	var vis := _visual_animado(pawn)
	if vis == null:
		return
	if int(pawn.get_meta(META_GESTO, 0)) != inicio:
		return  # um gesto mais novo comecou: quem manda e' ele
	var nome := "idle"
	if pawn.has_method("locomotion_anim"):
		nome = str(pawn.call("locomotion_anim"))
	vis.call("play_anim", nome)


## O visual do pawn, se ele existir e souber animar. Duck-typing de proposito:
## este arquivo NAO conhece Pawn nem Mage — acopla por fora, como o resto do
## slot (e e' o que deixa bot, player e pawn de teste passarem pelo mesmo lugar).
static func _visual_animado(pawn: Node) -> Node:
	if pawn == null or not is_instance_valid(pawn):
		return null
	var v: Variant = pawn.get("visual")
	if not (v is Node) or not is_instance_valid(v as Node) \
			or not (v as Node).has_method("play_anim"):
		return null
	return v as Node


# ---------------------------------------------------------------- visual

func _montar_visual() -> void:
	if is_instance_valid(_modelo):
		_modelo.queue_free()
	if arma_id == "":
		return  # maos nuas: nenhum modelo na mao — a ausencia E' a leitura
	_modelo = modelo(arma_id, elemento)
	_modelo.position = MAO_OFFSET
	add_child(_modelo)


## MODELO PROCEDURAL da arma (zero binario — regra do projeto). Primitivas da
## engine, nada de malha em disco.
##
## 26/08 (queixa do Diretor no teste: "ainda temos varinhas e bastoes na tela,
## precisa mudar tudo para luvas"): os tres tiers viraram LUVAS — punho, dorso,
## dedos. A COR vem do ELEMENTO da luva (DIRECAO.md §1: cor+elemento proprios);
## a FORMA diz o tier: punho curto (comum), punho longo com nos de cristal
## (conjurador), placas grossas com 2 gemas (manopla — o brilho alto continua
## sendo o tell do GDD §16.2). Modelo definitivo vem do Meshy (DIRECAO §10,
## peca no 2); este e' o procedural que segura a leitura ate' la'.
static func modelo(arma_id_: String, elemento_ := "") -> Node3D:
	var n := Node3D.new()
	n.name = "ModeloArma"
	var c := Projectile.tint(elemento_) if elemento_ != "" else Arma.cor(arma_id_)
	match arma_id_:
		"cajado":
			_luva(n, c, 1)
		"manopla":
			_luva(n, c, 2)
		_:
			_luva(n, c, 0)
	return n


## A luva em primitivas: punho (cilindro), dorso (caixa), 4 dedos + polegar.
## `tier` muda silhueta e brilho — nunca so' a cor (GDD §10).
static func _luva(parent: Node3D, cor: Color, tier: int) -> void:
	var couro := Color("3a3230")
	var brilho: float = [1.6, 2.4, 4.0][tier]
	# punho/cano — mais longo por tier
	var cuff := MeshInstance3D.new()
	var cy := CylinderMesh.new()
	cy.top_radius = 0.055
	cy.bottom_radius = 0.062 + 0.012 * float(tier)
	cy.height = 0.10 + 0.05 * float(tier)
	cuff.mesh = cy
	cuff.material_override = mat_brilho(cor.darkened(0.45), brilho * 0.3)
	parent.add_child(cuff)
	# dorso da mao
	var dorso := MeshInstance3D.new()
	var bx := BoxMesh.new()
	bx.size = Vector3(0.085, 0.10, 0.045 + 0.012 * float(tier))
	dorso.mesh = bx
	dorso.position.y = 0.10
	dorso.material_override = mat_brilho(couro.lerp(cor, 0.25), brilho * 0.4)
	parent.add_child(dorso)
	# 4 dedos
	for i in 4:
		var dedo := MeshInstance3D.new()
		var d := BoxMesh.new()
		d.size = Vector3(0.018, 0.055, 0.02)
		dedo.mesh = d
		dedo.position = Vector3(-0.030 + 0.020 * float(i), 0.175, 0.0)
		dedo.material_override = mat_brilho(couro.lerp(cor, 0.2), brilho * 0.35)
		parent.add_child(dedo)
	# polegar
	var pol := MeshInstance3D.new()
	var pd := BoxMesh.new()
	pd.size = Vector3(0.02, 0.045, 0.02)
	pol.mesh = pd
	pol.position = Vector3(0.052, 0.115, 0.008)
	pol.rotation.z = -0.5
	pol.material_override = mat_brilho(couro.lerp(cor, 0.2), brilho * 0.35)
	parent.add_child(pol)
	# a RUNA no dorso: a cor do elemento acesa — e' o que se le' de longe
	var runa := MeshInstance3D.new()
	var rp := PrismMesh.new()
	rp.size = Vector3(0.045, 0.045, 0.012)
	runa.mesh = rp
	runa.position = Vector3(0, 0.105, 0.034 + 0.008 * float(tier))
	runa.rotation.x = PI / 2.0
	runa.material_override = mat_brilho(cor, brilho)
	parent.add_child(runa)
	if tier >= 1:
		# nos de cristal do conjurador (e da manopla) sobre os dedos
		for i in 4:
			var no_ := MeshInstance3D.new()
			var np := PrismMesh.new()
			np.size = Vector3(0.016, 0.02, 0.016)
			no_.mesh = np
			no_.position = Vector3(-0.030 + 0.020 * float(i), 0.21, 0.0)
			no_.material_override = mat_brilho(cor, brilho)
			parent.add_child(no_)
	if tier >= 2:
		# as 2 gemas da manopla: os DOIS elementos, visiveis (o portador se
		# denuncia — limitador do GDD §16.2)
		for i in 2:
			var gema := MeshInstance3D.new()
			var g := SphereMesh.new()
			g.radius = 0.022
			g.height = 0.04
			gema.mesh = g
			gema.position = Vector3(-0.022 + 0.045 * float(i), 0.06, 0.035)
			gema.material_override = mat_brilho(cor, 5.0)
			parent.add_child(gema)


static func _haste(parent: Node3D, raio: float, alt: float, cor: Color) -> void:
	var m := MeshInstance3D.new()
	var cyl := CylinderMesh.new()
	cyl.top_radius = raio
	cyl.bottom_radius = raio * 1.25  # afina para cima: le' como cabo torneado
	cyl.height = alt
	cyl.radial_segments = 6          # mobile: 6 lados bastam nesse tamanho
	m.mesh = cyl
	var mat := StandardMaterial3D.new()
	mat.albedo_color = cor
	m.material_override = mat
	parent.add_child(m)


## Material de brilho compartilhado (o loot no chao usa o mesmo).
static func mat_brilho(cor: Color, energia: float) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = cor
	mat.emission_enabled = true
	mat.emission = cor
	mat.emission_energy_multiplier = energia
	return mat
