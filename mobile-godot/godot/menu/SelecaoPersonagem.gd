## TELA DE SELECAO DE PERSONAGEM — duas vistas num Control so:
##   GRADE  : os 20 magos em cards (retrato + nome + funcao + classe)
##   PERFIL : toque num card e abre a ficha (historia breve, kit, limitadores)
##
## NAO SABE NADA sobre os magos: tudo vem de menu/Elenco.gd (contrato la em
## cima daquele arquivo). Trocar/costurar texto de personagem nunca mexe aqui.
##
## Mobile-first (celular deitado): o numero de colunas nasce da largura UTIL
## que o pai entrega — o pai (Menu.gd) ja descontou a area segura do Android.
## Nenhuma margem em px cru; alvo de toque = o card inteiro.
class_name SelecaoPersonagem
extends Control

signal voltar_pedido

## Card largo o bastante para 5 colunas encherem uma tela 20:9 (base do
## project.godot: 1600x720) sem sobrar corredor vazio nas laterais.
const LARG_CARD := 240.0
const ALT_CARD := 210.0
const ALT_RETRATO := 118.0
const SEP := 10

var _grade: Control
var _perfil: PanelContainer
var _grid: GridContainer


func _ready() -> void:
	_grade = _montar_grade()
	_perfil = _montar_perfil()
	resized.connect(_ajustar_colunas)
	_ajustar_colunas()


# ---------------------------------------------------------------- grade

func _montar_grade() -> Control:
	var raiz := Control.new()
	raiz.name = "Grade"
	raiz.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(raiz)
	raiz.set_anchors_preset(Control.PRESET_FULL_RECT)

	var alt_topo := Estilo.altura_alvo() + 8.0
	var topo := Control.new()
	topo.mouse_filter = Control.MOUSE_FILTER_IGNORE
	raiz.add_child(topo)
	topo.set_anchors_preset(Control.PRESET_TOP_WIDE)
	topo.offset_bottom = alt_topo
	var titulo := Estilo.rotulo(Textos.SEL_TITULO, 30, Estilo.OURO)
	titulo.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	titulo.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	topo.add_child(titulo)
	titulo.set_anchors_preset(Control.PRESET_FULL_RECT)
	var voltar := Estilo.botao(Textos.VOLTAR, "BtnVoltar", Estilo.ALVO_DP, 170.0, 22)
	voltar.pressed.connect(func() -> void: voltar_pedido.emit())
	topo.add_child(voltar)

	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	raiz.add_child(scroll)
	scroll.set_anchors_preset(Control.PRESET_FULL_RECT)
	scroll.offset_top = alt_topo + 4.0
	var centro := CenterContainer.new()
	centro.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(centro)
	_grid = GridContainer.new()
	_grid.columns = 5
	_grid.add_theme_constant_override("h_separation", SEP)
	_grid.add_theme_constant_override("v_separation", SEP)
	centro.add_child(_grid)
	for d in Elenco.MAGOS:
		_grid.add_child(_card(d))
	return raiz


## Quantos cards cabem na largura util (o pai ja tirou a area segura).
func _ajustar_colunas() -> void:
	if _grid == null:
		return
	_grid.columns = clampi(int(size.x / (LARG_CARD + SEP)), 2, 5)


func _card(d: Dictionary) -> PanelContainer:
	var temporada := Elenco.e_temporada(d)
	var card := PanelContainer.new()
	card.name = "Card%02d" % int(d.id)
	card.add_to_group("elenco_card")
	card.custom_minimum_size = Vector2(LARG_CARD, ALT_CARD)
	card.add_theme_stylebox_override("panel", Estilo.moldura(not temporada))

	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 2)
	v.mouse_filter = Control.MOUSE_FILTER_IGNORE
	card.add_child(v)
	v.add_child(_retrato(d, ALT_RETRATO, temporada))
	v.add_child(_centrado(Elenco.nome_cheio(d), 15, Estilo.TEXTO))
	v.add_child(_centrado(str(d.funcao), 13, Estilo.TEXTO_FOSCO))
	var classe := str(d.classe)
	v.add_child(_centrado(classe if classe != "" else Textos.SEL_A_DEFINIR,
		13, Color(Estilo.OURO, 0.75)))
	if temporada:
		var breve := _centrado(Textos.SEL_EM_BREVE, 13, Estilo.OURO)
		breve.name = "EmBreve"
		v.add_child(breve)

	# o card INTEIRO e o alvo de toque (bem acima de 48dp)
	var toque := Estilo.botao_invisivel("Toque")
	toque.pressed.connect(func() -> void: _abrir_perfil(d))
	card.add_child(toque)
	return card


func _centrado(texto: String, tam: int, cor: Color) -> Label:
	var l := Estilo.rotulo(texto, tam, cor)
	l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	return l


## Retrato do Diretor (personagens/NN/arte/concept.png -> menu/art/NN.png).
## Se o arquivo ainda nao existir, entra a silhueta — a tela nunca fica com
## buraco e nenhum codigo muda quando a arte chega.
func _retrato(d: Dictionary, altura: float, temporada: bool) -> Control:
	var caminho := str(d.caminho_arte)
	if ResourceLoader.exists(caminho):
		var tr := TextureRect.new()
		tr.texture = load(caminho)
		tr.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		tr.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		tr.custom_minimum_size = Vector2(0, altura)
		tr.mouse_filter = Control.MOUSE_FILTER_IGNORE
		return tr
	var sil := Silhueta.new()
	sil.cor = Color("#232A4A") if temporada else Color("#333D66")
	sil.custom_minimum_size = Vector2(0, altura)
	return sil


# ---------------------------------------------------------------- perfil

func _montar_perfil() -> PanelContainer:
	var p := PanelContainer.new()
	p.name = "Perfil"
	p.visible = false
	var sb := Estilo.moldura()
	sb.bg_color = Estilo.NOITE
	sb.set_content_margin_all(12)
	p.add_theme_stylebox_override("panel", sb)
	add_child(p)
	p.set_anchors_preset(Control.PRESET_FULL_RECT)
	return p


func _abrir_perfil(d: Dictionary) -> void:
	for c in _perfil.get_children():
		_perfil.remove_child(c)
		c.queue_free()

	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 8)
	_perfil.add_child(v)

	var cab := HBoxContainer.new()
	cab.add_theme_constant_override("separation", 12)
	v.add_child(cab)
	var fechar := Estilo.botao(Textos.PERFIL_FECHAR, "BtnFecharPerfil",
		Estilo.ALVO_DP, 170.0, 22)
	fechar.pressed.connect(_fechar_perfil)
	cab.add_child(fechar)
	var nome := Estilo.rotulo(Elenco.nome_cheio(d), 28, Estilo.OURO)
	nome.name = "PerfilNome"
	nome.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	nome.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	cab.add_child(nome)

	var corpo := HBoxContainer.new()
	corpo.size_flags_vertical = Control.SIZE_EXPAND_FILL
	corpo.add_theme_constant_override("separation", 14)
	v.add_child(corpo)

	var col := VBoxContainer.new()
	col.custom_minimum_size.x = 240.0
	corpo.add_child(col)
	col.add_child(_retrato(d, 240.0, Elenco.e_temporada(d)))
	col.add_child(_centrado("%s: %s" % [Textos.SEL_RACA, d.raca], 15, Estilo.TEXTO_FOSCO))
	var classe := str(d.classe)
	col.add_child(_centrado("%s: %s" % [Textos.SEL_CLASSE,
		classe if classe != "" else Textos.SEL_A_DEFINIR], 15, Estilo.TEXTO_FOSCO))
	col.add_child(_centrado("%s: %s" % [Textos.SEL_FUNCAO, d.funcao], 15, Estilo.TEXTO_FOSCO))
	if Elenco.e_temporada(d):
		col.add_child(_centrado(Textos.SEL_EM_BREVE, 16, Estilo.OURO))

	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	corpo.add_child(scroll)
	var txt := VBoxContainer.new()
	txt.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	txt.add_theme_constant_override("separation", 4)
	scroll.add_child(txt)

	_secao(txt, Textos.PERFIL_HISTORIA)
	txt.add_child(Estilo.paragrafo(Elenco.campo(d, "historia_breve"), 17, Estilo.TEXTO))
	_secao(txt, Textos.PERFIL_KIT)
	_habilidade(txt, Textos.PERFIL_PASSIVA, Elenco.campo(d, "passiva"))
	_habilidade(txt, Textos.PERFIL_TATICA, Elenco.campo(d, "tatica"))
	_habilidade(txt, Textos.PERFIL_SUPREMA, Elenco.campo(d, "suprema"))
	_secao(txt, Textos.PERFIL_LIMITADORES)
	txt.add_child(Estilo.paragrafo(Elenco.campo(d, "limitadores"), 16, Estilo.TEXTO_FOSCO))

	_grade.visible = false
	_perfil.visible = true


func _fechar_perfil() -> void:
	_perfil.visible = false
	_grade.visible = true


func _secao(v: VBoxContainer, titulo: String) -> void:
	var esp := Control.new()
	esp.custom_minimum_size = Vector2(0, 8)
	v.add_child(esp)
	v.add_child(Estilo.rotulo(titulo, 18, Estilo.OURO))


func _habilidade(v: VBoxContainer, rotulo: String, texto: String) -> void:
	v.add_child(Estilo.paragrafo("%s — %s" % [rotulo, texto], 17, Estilo.TEXTO))


# ---------------------------------------------------------------- desenho

## Silhueta placeholder de mago (chapeu + cabeca + manto + cajado), usada ate a
## arte do Diretor existir em res://menu/art/NN.png. Veio do Menu.gd (R18) junto
## com a vitrine — o desenho e o mesmo.
class Silhueta extends Control:
	var cor := Color("#333D66")

	func _init() -> void:
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		var w := size.x
		var h := size.y
		var cx := w * 0.5
		# manto
		draw_colored_polygon(PackedVector2Array([
			Vector2(cx - w * 0.10, h * 0.45), Vector2(cx + w * 0.10, h * 0.45),
			Vector2(cx + w * 0.20, h * 0.97), Vector2(cx - w * 0.20, h * 0.97),
		]), cor)
		# cabeca
		draw_circle(Vector2(cx, h * 0.37), h * 0.11, cor)
		# chapeu: aba + cone
		draw_rect(Rect2(cx - w * 0.16, h * 0.27, w * 0.32, h * 0.05), cor)
		draw_colored_polygon(PackedVector2Array([
			Vector2(cx + w * 0.02, h * 0.03),
			Vector2(cx + w * 0.10, h * 0.28), Vector2(cx - w * 0.10, h * 0.28),
		]), cor)
		# cajado com orbe
		draw_line(Vector2(cx + w * 0.20, h * 0.30), Vector2(cx + w * 0.24, h * 0.95),
			cor, 3.0, true)
		draw_circle(Vector2(cx + w * 0.195, h * 0.26), w * 0.035, Color("#8A7336"))
