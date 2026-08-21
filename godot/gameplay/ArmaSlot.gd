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

var arma_id := "varinha"
var par: PackedStringArray = PackedStringArray()  # so' a manopla usa
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


func _ready() -> void:
	_montar_visual()


func spec(el: String) -> Dictionary:
	return Arma.spec(el, arma_id)


func dados() -> Dictionary:
	return Arma.dados(arma_id)


func elementos(escolhido: String) -> PackedStringArray:
	return Arma.elementos_de(arma_id, par, escolhido)


## Elemento DESTE disparo. Varinha/cajado: o do carrossel. Manopla: alterna
## entre os 2 fixos, tiro a tiro — e' assim que "porta 2 elementos
## simultaneos" vira coisa que o dedo sente, sem inventar UI nova.
func elemento_do_disparo(escolhido: String) -> String:
	var els := elementos(escolhido)
	if els.size() < 2:
		return escolhido
	_alt = (_alt + 1) % els.size()
	return els[_alt]


## Troca a arma ativa. p_par so' importa para a manopla (par fixo, sem troca).
func equipar(id: String, p_par: PackedStringArray = PackedStringArray()) -> void:
	if not Arma.existe(id):
		return
	arma_id = id
	par = p_par
	_alt = 0
	_montar_visual()
	var d := dados()
	Bus.weapon_equipped.emit(id, str(d.nome), str(d.raridade), elementos(""))


func tier() -> int:
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


# ---------------------------------------------------------------- visual

func _montar_visual() -> void:
	if is_instance_valid(_modelo):
		_modelo.queue_free()
	_modelo = modelo(arma_id)
	_modelo.position = MAO_OFFSET
	add_child(_modelo)


## MODELO PROCEDURAL da arma (zero binario — regra do projeto). Primitivas da
## engine, nada de malha em disco. COR + FORMA (GDD §10): a silhueta sozinha
## ja' diz o tier, mesmo em preto e branco.
##   varinha -> bastao curto e fino com ponta redonda
##   cajado  -> bastao longo com cristal facetado no topo
##   manopla -> punho fechado + 3 dedos, e o BRILHO NAS MAOS que o GDD exige
##              como contrapartida do poder (denuncia o portador)
static func modelo(arma_id_: String) -> Node3D:
	var n := Node3D.new()
	n.name = "ModeloArma"
	var c := Arma.cor(arma_id_)
	match arma_id_:
		"cajado":
			_haste(n, 0.045, 1.55, Color("6b5136"))
			var cristal := MeshInstance3D.new()
			var pr := PrismMesh.new()
			pr.size = Vector3(0.26, 0.34, 0.26)
			cristal.mesh = pr
			cristal.position.y = 0.92
			cristal.material_override = mat_brilho(c, 3.0)
			n.add_child(cristal)
		"manopla":
			var punho := MeshInstance3D.new()
			var bx := BoxMesh.new()
			bx.size = Vector3(0.19, 0.20, 0.15)
			punho.mesh = bx
			punho.material_override = mat_brilho(c, 4.0)  # brilho ALTO: o tell do GDD
			n.add_child(punho)
			for i in 3:  # 3 dedos: silhueta de punho, sem copiar simbolo nenhum
				var dedo := MeshInstance3D.new()
				var d := BoxMesh.new()
				d.size = Vector3(0.05, 0.055, 0.13)
				dedo.mesh = d
				dedo.position = Vector3(-0.06 + 0.06 * float(i), 0.13, 0.01)
				dedo.material_override = mat_brilho(c, 4.0)
				n.add_child(dedo)
		_:
			_haste(n, 0.032, 0.72, Color("5c4a3a"))
			var ponta := MeshInstance3D.new()
			var sp := SphereMesh.new()
			sp.radius = 0.075
			sp.height = 0.15
			ponta.mesh = sp
			ponta.position.y = 0.40
			ponta.material_override = mat_brilho(c, 2.2)
			n.add_child(ponta)
	return n


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
