## PORTA DE ENTRADA do jogo (raia APRESENTACAO). Quatro telas num Control:
## Titulo (Selo + wordmark + "toque para comecar") -> Menu (JOGAR / PERSONAGENS
## / CONFIGURACOES / SAIR) -> Selecao de mago (menu/SelecaoPersonagem.gd) e
## Configuracoes (menu/Config.gd).
##
## Tudo PROCEDURAL (zero binario): Selo em _draw (mesmo desenho do
## roblox/src/client/Hud.luau buildSeal), gradiente via GradientTexture2D,
## particulas CPUParticles2D. Paleta e cores: menu/Estilo.gd (GDD par.10).
## Texto: core/Textos.gd — nenhuma string solta aqui.
##
## AREA SEGURA (o bug do APK R19): o FUNDO sangra a tela inteira de proposito;
## TODO conteudo vive dentro de telas encostadas por Safe.apply (ui/Safe.gd) e
## reencostadas a cada resize — girar o aparelho muda o notch, e o layout
## acompanha. Nenhuma margem em px cru neste arquivo.
##
## Boota SOZINHA e aguenta ser recarregada (nenhum estado fora de _ready).
##
## SEM class_name DE PROPOSITO: com class_name este script compila no scan de
## classes globais, ANTES dos autoloads existirem, e o `Bus` de _on_jogar vira
## "Identifier not found" no --script do selftest.
extends Control

const MAIN_SCENE := "res://gameplay/Main.tscn"

## Respiro entre a area segura do sistema e o conteudo, em dp.
const RESPIRO_DP := 12.0

## O selftest desliga isto para provar o sinal sem trocar de cena.
var scene_switch_enabled := true

var _telas: Array[Control] = []
var _tela_titulo: Control
var _tela_menu: Control
var _tela_selecao: Control
var _tela_config: Control
var _screen: Control
var _veu: ColorRect        # fade de saida — nada de tela branca seca
var _tap: Label
var _particulas: CPUParticles2D
var _starting := false
var _phase := 0.0


func _ready() -> void:
	# settings.json e lido e aplicado UMA vez, no boot (GDD par.12).
	Config.aplicar(Config.carregar())

	_build_fundo()
	_tela_titulo = _build_titulo()
	_tela_menu = _build_menu()
	_tela_selecao = _build_selecao()
	_tela_config = _build_config()
	_veu = ColorRect.new()
	_veu.color = Estilo.NOITE_FUNDA
	_veu.visible = false
	_veu.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(_veu)
	_veu.set_anchors_preset(Control.PRESET_FULL_RECT)  # o fade cobre ate o notch
	_tela_menu.visible = false
	_tela_selecao.visible = false
	_tela_config.visible = false
	_screen = _tela_titulo
	resized.connect(_on_resized)
	_on_resized()


func _process(delta: float) -> void:
	_phase += delta
	if _tap != null and _tap.is_visible_in_tree():
		_tap.modulate.a = 0.5 + 0.4 * sin(_phase * 3.0)


func _on_resized() -> void:
	_layout_particulas()
	_encostar_telas()


## A UNICA fonte de margem de borda do menu. Girar o aparelho, abrir a barra de
## gestos ou mudar de aparelho passa por aqui.
func _encostar_telas() -> void:
	for t in _telas:
		Safe.apply(t, RESPIRO_DP)


# ---------------------------------------------------------------- fundo vivo

func _build_fundo() -> void:
	# gradiente vertical azul-noite (procedural, sem textura em disco)
	add_child(_grad_rect(
		[Estilo.NOITE_FUNDA, Estilo.NOITE, Color("#131B3E")], [0.0, 0.55, 1.0],
		Vector2(0.5, 0.0), Vector2(0.5, 1.0), GradientTexture2D.FILL_LINEAR))
	# halo dourado sutil atras do centro da tela
	add_child(_grad_rect(
		[Color(Estilo.OURO, 0.10), Color(Estilo.OURO, 0.0)], [0.0, 1.0],
		Vector2(0.5, 0.42), Vector2(0.5, 0.0), GradientTexture2D.FILL_RADIAL))
	# particulas ambar subindo (motes arcanos)
	_particulas = CPUParticles2D.new()
	_particulas.amount = 36
	_particulas.lifetime = 9.0
	_particulas.preprocess = 6.0
	_particulas.emission_shape = CPUParticles2D.EMISSION_SHAPE_RECTANGLE
	_particulas.direction = Vector2(0, -1)
	_particulas.spread = 12.0
	_particulas.gravity = Vector2(0, -14)
	_particulas.initial_velocity_min = 18.0
	_particulas.initial_velocity_max = 46.0
	_particulas.scale_amount_min = 1.2
	_particulas.scale_amount_max = 3.2
	var ramp := Gradient.new()
	ramp.offsets = PackedFloat32Array([0.0, 0.2, 1.0])
	ramp.colors = PackedColorArray([
		Color(Estilo.OURO, 0.0), Color(Estilo.OURO, 0.5), Color(Estilo.OURO, 0.0)])
	_particulas.color_ramp = ramp
	add_child(_particulas)


func _grad_rect(cores: Array, offs: Array, de: Vector2, ate: Vector2, fill: int) -> TextureRect:
	var g := Gradient.new()
	g.offsets = PackedFloat32Array(offs)
	g.colors = PackedColorArray(cores)
	var tex := GradientTexture2D.new()
	tex.gradient = g
	tex.fill = fill
	tex.fill_from = de
	tex.fill_to = ate
	var tr := TextureRect.new()
	tr.texture = tex
	tr.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	tr.stretch_mode = TextureRect.STRETCH_SCALE
	tr.mouse_filter = Control.MOUSE_FILTER_IGNORE
	# de proposito FORA da area segura: fundo sangra, conteudo nao.
	tr.set_anchors_preset(Control.PRESET_FULL_RECT)
	return tr


func _layout_particulas() -> void:
	if _particulas == null:
		return
	_particulas.position = Vector2(size.x * 0.5, size.y + 12.0)
	_particulas.emission_rect_extents = Vector2(size.x * 0.5, 10.0)


# ---------------------------------------------------------------- tela titulo

func _build_titulo() -> Control:
	var t := _tela("Titulo")

	# toque em QUALQUER lugar avanca (alvo maximo possivel > 48dp)
	var tap_btn := Estilo.botao_invisivel("TapTitulo")
	t.add_child(tap_btn)
	tap_btn.set_anchors_preset(Control.PRESET_FULL_RECT)
	tap_btn.pressed.connect(func() -> void: _go(_tela_menu))

	var cc := CenterContainer.new()
	cc.mouse_filter = Control.MOUSE_FILTER_IGNORE
	t.add_child(cc)
	cc.set_anchors_preset(Control.PRESET_FULL_RECT)
	cc.offset_bottom = -60.0  # sobe o bloco: respiro p/ o "toque para comecar"
	var v := VBoxContainer.new()
	v.alignment = BoxContainer.ALIGNMENT_CENTER
	v.add_theme_constant_override("separation", 6)
	v.mouse_filter = Control.MOUSE_FILTER_IGNORE
	cc.add_child(v)

	var selo := Seal.new()
	selo.custom_minimum_size = Vector2(270, 270)
	selo.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	v.add_child(selo)
	v.add_child(_wordmark(88, 16))
	var sub := Estilo.rotulo(Textos.TITULO_SUB, 26, Estilo.TEXTO_FOSCO)
	sub.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	v.add_child(sub)

	_tap = Estilo.rotulo(Textos.TITULO_TOQUE, 24, Estilo.OURO)
	_tap.add_theme_constant_override("outline_size", 2)
	_tap.add_theme_color_override("font_outline_color", Color(Estilo.NOITE_FUNDA, 0.8))
	t.add_child(_tap)
	_tap.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	_tap.offset_top -= 56.0
	_tap.offset_bottom -= 56.0
	return t


## Wordmark ARKANA: uma Label por letra num HBox = espacamento largo com a
## fonte padrao (sem binario). Outline dourado engrossa o traco (peso).
func _wordmark(tam: int, sep: int) -> HBoxContainer:
	var box := HBoxContainer.new()
	box.add_theme_constant_override("separation", sep)
	box.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	box.mouse_filter = Control.MOUSE_FILTER_IGNORE
	for ch in Textos.MARCA:
		var cor := Estilo.RAIO if ch == "K" else Estilo.OURO
		var l := Estilo.rotulo(ch, tam, cor)
		l.add_theme_constant_override("outline_size", maxi(2, tam / 22))
		l.add_theme_color_override("font_outline_color", Color(cor, 0.9))
		l.add_theme_color_override("font_shadow_color", Color(Estilo.NOITE_FUNDA, 0.85))
		l.add_theme_constant_override("shadow_offset_x", 0)
		l.add_theme_constant_override("shadow_offset_y", maxi(2, tam / 24))
		box.add_child(l)
	return box


# ---------------------------------------------------------------- menu principal

func _build_menu() -> Control:
	var t := _tela("MenuPrincipal")
	var cc := CenterContainer.new()
	cc.mouse_filter = Control.MOUSE_FILTER_IGNORE
	t.add_child(cc)
	cc.set_anchors_preset(Control.PRESET_FULL_RECT)
	var v := VBoxContainer.new()
	v.alignment = BoxContainer.ALIGNMENT_CENTER
	v.add_theme_constant_override("separation", 12)
	cc.add_child(v)
	v.add_child(_wordmark(44, 8))
	var esp := Control.new()
	esp.custom_minimum_size = Vector2(0, 8)
	v.add_child(esp)

	var jogar := Estilo.botao(Textos.MENU_JOGAR, "BtnJogar", 56.0)
	jogar.pressed.connect(_on_jogar)
	v.add_child(jogar)
	var pers := Estilo.botao(Textos.MENU_PERSONAGENS, "BtnPersonagens", 56.0)
	pers.pressed.connect(func() -> void: _go(_tela_selecao))
	v.add_child(pers)
	var cfg := Estilo.botao(Textos.MENU_CONFIG, "BtnConfig", 56.0)
	cfg.pressed.connect(func() -> void: _go(_tela_config))
	v.add_child(cfg)
	var sair := Estilo.botao(Textos.MENU_SAIR, "BtnSair", 56.0)
	sair.pressed.connect(func() -> void: get_tree().quit())
	v.add_child(sair)
	return t


func _on_jogar() -> void:
	if _starting:
		return
	# Contrato oficial: o menu PEDE a partida pelo Bus (core/Bus.gd).
	Bus.game_start_requested.emit()
	if not scene_switch_enabled:
		return
	# Fiacao direta defensiva: mesmo sem ninguem ouvindo o Bus ainda,
	# o botao leva a partida. Se a cena da raia gameplay sumir, fica no menu.
	if not ResourceLoader.exists(MAIN_SCENE):
		push_warning("Menu: %s não existe — permanecendo no menu." % MAIN_SCENE)
		return
	_starting = true
	_veu.modulate.a = 0.0
	_veu.visible = true
	var tw := create_tween()
	tw.tween_property(_veu, "modulate:a", 1.0, 0.25)
	tw.tween_callback(func() -> void: get_tree().change_scene_to_file(MAIN_SCENE))


# ---------------------------------------------------------------- telas filhas

func _build_selecao() -> Control:
	var t := _tela("Selecao")
	var s := SelecaoPersonagem.new()
	s.name = "SelecaoPersonagem"
	t.add_child(s)
	s.set_anchors_preset(Control.PRESET_FULL_RECT)
	s.voltar_pedido.connect(func() -> void: _go(_tela_menu))
	return t


func _build_config() -> Control:
	var t := _tela("Config")
	var c := Config.new()
	c.name = "Configuracoes"
	t.add_child(c)
	c.set_anchors_preset(Control.PRESET_FULL_RECT)
	c.voltar_pedido.connect(func() -> void: _go(_tela_menu))
	return t


# ---------------------------------------------------------------- helpers

## Tela = Control de tela cheia ENCOSTADO na area segura (_encostar_telas).
func _tela(nome: String) -> Control:
	var t := Control.new()
	t.name = nome
	t.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(t)
	t.set_anchors_preset(Control.PRESET_FULL_RECT)
	_telas.append(t)
	return t


## Fade curto entre telas (o fundo fica — nunca ha' quadro vazio).
func _go(alvo: Control) -> void:
	if alvo == _screen or alvo == null:
		return
	var de := _screen
	_screen = alvo
	var tw := create_tween()
	tw.tween_property(de, "modulate:a", 0.0, 0.16)
	tw.tween_callback(func() -> void:
		de.visible = false
		de.modulate.a = 1.0
		alvo.modulate.a = 0.0
		alvo.visible = true)
	tw.tween_property(alvo, "modulate:a", 1.0, 0.16)


# ---------------------------------------------------------------- desenhos

## O Selo de Arkana — MESMO desenho do buildSeal do roblox/src/client/Hud.luau:
## pentagono (raio 0.38 do lado), 5 gemas-losango nas pontas (cores dos
## elementos, GDD par.10), "A" dourado no centro. Gemas pulsam devagar.
class Seal extends Control:
	const GEMS: Array[Color] = [
		Color("#FF5A2A"),  # Fogo
		Color("#2AA7FF"),  # Água
		Color("#A8763E"),  # Terra
		Color("#8FE8C9"),  # Vento
		Color("#F5D90A"),  # Raio
	]
	var _phase := 0.0

	func _init() -> void:
		mouse_filter = Control.MOUSE_FILTER_IGNORE
		var a := Label.new()
		a.text = "A"
		a.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		a.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		a.add_theme_font_size_override("font_size", 64)
		a.add_theme_color_override("font_color", Color("#F0C75E"))
		a.add_theme_constant_override("outline_size", 3)
		a.add_theme_color_override("font_outline_color", Color("#F0C75E", 0.6))
		a.modulate.a = 0.9
		add_child(a)
		a.set_anchors_preset(Control.PRESET_FULL_RECT)

	func _process(delta: float) -> void:
		_phase += delta
		queue_redraw()

	func _draw() -> void:
		var c := size * 0.5
		var r := minf(size.x, size.y) * 0.38
		var pts := PackedVector2Array()
		for i in 5:
			pts.append(c + Vector2.from_angle(deg_to_rad(-90 + i * 72)) * r)
		draw_arc(c, r * 1.16, 0.0, TAU, 64, Color("#8A7336", 0.35), 1.5, true)
		for i in 5:
			draw_line(pts[i], pts[(i + 1) % 5], Color("#8A7336", 0.7), 2.0, true)
		var g := minf(size.x, size.y) * 0.05
		for i in 5:
			var brilho := 0.55 + 0.25 * sin(_phase * 2.0 + i * 1.3)
			var p := pts[i]
			var quad := PackedVector2Array([
				p + Vector2(0, -g), p + Vector2(g, 0),
				p + Vector2(0, g), p + Vector2(-g, 0),
			])
			draw_colored_polygon(quad, Color(GEMS[i], brilho))
			var contorno := quad.duplicate()
			contorno.append(quad[0])
			draw_polyline(contorno, Color("#F0C75E", 0.5), 1.0, true)
