## FERRAMENTA DE ATELIE (nao entra no APK — ver exclude_filter). Renderiza a
## MARCA do Arkana em PNG a partir do Selo que ja' existe em menu/Selo.gd.
##
## Por que existe: o icone do aparelho e a splash de abertura precisam ser
## arquivo binario — o Android e o motor nao sabem chamar um _draw(). Sem esta
## ferramenta alguem desenharia o selo DE NOVO num editor de imagem, e no dia
## em que o Diretor mudasse o dourado existiriam duas verdades divergindo em
## silencio. Aqui o desenho continua morando num lugar so': menu/Selo.gd.
##
## Uso (precisa de video — NAO roda em --headless, o driver dummy devolve
## textura vazia; isso e' esperado e a ferramenta avisa em vez de salvar lixo):
##     godot --path godot --script menu/_marca.gd
##
## Saida: menu/art/marca/*.png
extends SceneTree

const SELO := preload("res://menu/Selo.gd")
const DESTINO := "res://menu/art/marca"

## nome -> [lado em px, pinta o fundo?, quanto do lado o selo ocupa]
##
## A TERCEIRA COLUNA existe por causa do icone ADAPTATIVO do Android: a frente
## e' de 432 px mas o sistema aplica uma mascara (circulo, squircle, gota — cada
## fabricante a sua) e SO' o miolo de ~66% e' garantido. Um selo desenhado ate' a
## borda perde as gemas de cima e de baixo em metade dos aparelhos. Com 0.56 o
## selo inteiro, incluindo o anel externo em raio 1.16, cabe dentro da zona
## segura em qualquer mascara. O icone legado e a splash nao tem mascara: 1.0.
const PECAS := {
	"fundo-432": [432, true, 0.0],    # fundo do adaptativo: so' o gradiente
	"selo-432": [432, false, 0.64],   # frente do adaptativo: dentro da mascara
	"icone-192": [192, true, 1.0],    # icone legado: opaco, sem mascara
	"splash-512": [512, false, 1.0],  # abertura: fundo vem de boot_splash/bg_color
}

var _pendentes: Array = []
var _quadros := 0
var _vp: SubViewport
var _atual := ""
var _falhou := false


func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(DESTINO)
	_pendentes = PECAS.keys()
	_proxima()


func _proxima() -> void:
	if _pendentes.is_empty():
		return
	_atual = _pendentes.pop_front()
	var lado: int = PECAS[_atual][0]
	var opaco: bool = PECAS[_atual][1]
	var escala: float = PECAS[_atual][2]

	if _vp:
		_vp.queue_free()
	_vp = SubViewport.new()
	_vp.size = Vector2i(lado, lado)
	_vp.transparent_bg = not opaco
	_vp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(_vp)

	if opaco:
		# Mesmo gradiente do fundo do menu (Menu._build_fundo), para o icone no
		# aparelho e a tela de titulo serem visivelmente a mesma marca.
		var g := Gradient.new()
		g.offsets = PackedFloat32Array([0.0, 0.55, 1.0])
		g.colors = PackedColorArray([
			Estilo.NOITE_FUNDA, Estilo.NOITE, Color("#131B3E")])
		var tex := GradientTexture2D.new()
		tex.gradient = g
		tex.fill_from = Vector2(0.5, 0.0)
		tex.fill_to = Vector2(0.5, 1.0)
		tex.width = lado
		tex.height = lado
		var fundo := TextureRect.new()
		fundo.texture = tex
		fundo.size = Vector2(lado, lado)
		_vp.add_child(fundo)

	if escala > 0.0:
		var box := int(round(lado * escala))
		var selo: Control = SELO.new()
		selo.size = Vector2(box, box)
		selo.position = Vector2((lado - box) * 0.5, (lado - box) * 0.5)
		# O "A" nasce com fonte 64 num selo de 270 px (Menu._build_titulo). Manter a
		# PROPORCAO, e nao o numero, e' o que faz todas as pecas terem a mesma letra.
		var a := selo.get_child(0) as Label
		a.add_theme_font_size_override("font_size", int(round(box * 64.0 / 270.0)))
		a.add_theme_constant_override("outline_size", maxi(2, int(box * 3.0 / 270.0)))
		_vp.add_child(selo)

	_quadros = 0


func _process(_delta: float) -> bool:
	if _atual == "":
		return true
	_quadros += 1
	# 3 quadros: um para o SubViewport existir, um para o _draw do Selo entrar,
	# um para a textura do alvo estar de fato escrita. Salvar antes disso devolve
	# imagem preta — foi assim que a primeira versao "funcionou" sem desenhar nada.
	if _quadros < 3:
		return false

	# Sem esta guarda o script TRAVA: em --headless get_texture() volta nulo, o
	# _process morre no meio a cada quadro e nunca chega a devolver true. Medido:
	# 2 minutos de laco silencioso ate' o shell matar o processo.
	var tex := _vp.get_texture()
	var img: Image = tex.get_image() if tex else null
	if img == null or img.get_width() == 0:
		return _falhar("ERRO: sem textura em %s — rode COM video (sem --headless)." % _atual)
	# fundo-432 e' um gradiente liso de proposito — a checagem de "nada foi
	# desenhado" so' faz sentido nas pecas que tem selo.
	if PECAS[_atual][2] > 0.0 and _vazia(img):
		printerr("ERRO: textura vazia em %s." % _atual)
		printerr("      Rodou com --headless? O driver dummy nao rasteriza nada.")
		return _falhar("      Rode com video: godot --path godot --script menu/_marca.gd")

	var caminho := "%s/%s.png" % [DESTINO, _atual]
	var erro := img.save_png(caminho)
	if erro != OK:
		return _falhar("ERRO ao salvar %s (codigo %d)" % [caminho, erro])
	print("  %s  %dx%d" % [caminho, img.get_width(), img.get_height()])

	if _pendentes.is_empty():
		_atual = ""
		print("MARCA: %d pecas geradas." % PECAS.size())
		return true
	_proxima()
	return false


## Uma imagem toda transparente OU toda de uma cor so' e' o sintoma de render
## que nao aconteceu. Amostra a grade inteira, por onde o pentagono passa.
## Nao se aplica a pecas sem selo (ver chamada).
func _vazia(img: Image) -> bool:
	var vistas := {}
	var passo := maxi(1, img.get_width() / 24)
	for x in range(0, img.get_width(), passo):
		for y in range(0, img.get_height(), passo):
			vistas[img.get_pixel(x, y).to_rgba32()] = true
			if vistas.size() > 2:
				return false
	return true



## Sai com codigo 1 de VERDADE. Nao mover para _initialize: quit() de la' e'
## engolido e o shell recebe 0 — o portao passaria com a marca quebrada.
func _falhar(motivo: String) -> bool:
	printerr(motivo)
	_falhou = true
	quit(1)
	return true
