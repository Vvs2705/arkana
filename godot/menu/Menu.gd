## PORTA DE ENTRADA do jogo (raia APRESENTACAO, R18). Tres telas num Control:
## Titulo (Selo + wordmark + "toque para comecar") -> Menu (JOGAR/PERSONAGENS/
## SAIR) -> Elenco (vitrine dos 20 magos). Tudo PROCEDURAL (zero binario):
## Selo em _draw (mesmo desenho do roblox/src/client/Hud.luau buildSeal),
## gradiente via GradientTexture2D, particulas CPUParticles2D.
## Paleta GDD par.10: azul-noite #0B1026 + dourado #F0C75E.
## Boota SOZINHA e aguenta ser recarregada (nenhum estado fora de _ready).
extends Control

const NIGHT := Color("#0B1026")
const NIGHT_DEEP := Color("#05070F")
const GOLD := Color("#F0C75E")
const GOLD_DIM := Color("#8A7336")
const RAIO := Color("#F5D90A")  # o "K" da wordmark leva o amarelo do Raio (GDD par.10)
const TEXT_COL := Color("#E8E6F0")
const TEXT_DIM := Color("#9A97AD")

const MAIN_SCENE := "res://gameplay/Main.tscn"

## ---------------------------------------------------------------- ARTE DO DIRETOR
## O retrato de cada mago entra AQUI: quando a arte de personagens/NN-*/arte/
## for aprovada, exporte/copie como res://menu/art/NN.png (ex.: art/01.png)
## e o card troca a silhueta placeholder pelo retrato AUTOMATICAMENTE
## (ver _card: ResourceLoader.exists). Nenhum codigo a mudar.
const ART_DIR := "res://menu/art"

## Elenco de personagens/00-LEIA.md (so leitura). 01-10 = lancamento;
## 11-20 = temporadas (ganham selo EM BREVE na vitrine).
const ELENCO: Array[Dictionary] = [
	{"n": 1, "nome": "Pyra, a Chama de Guerra", "info": "Ataque / média dist."},
	{"n": 2, "nome": "Ceifadora, a Voz do Vazio", "info": "Ataque / Perseguição"},
	{"n": 3, "nome": "Véu, a Andarilha", "info": "Perseguição / fuga"},
	{"n": 4, "nome": "Corvus, o Caçador", "info": "Ataque / Perseguição"},
	{"n": 5, "nome": "Corvomante, o Olho Distante", "info": "Longa distância / recon"},
	{"n": 6, "nome": "Olho-de-Éter, o Observador", "info": "Longa distância / recon"},
	{"n": 7, "nome": "Vitalis, a Mão que Cura", "info": "Suporte / Vida"},
	{"n": 8, "nome": "Ilusionista, o Espelho", "info": "Suporte / engano"},
	{"n": 9, "nome": "Vex, o Alquimista da Peste", "info": "Curta dist. / área"},
	{"n": 10, "nome": "Tessa, a Tecelã de Raios", "info": "Curta dist. / defesa"},
	{"n": 11, "nome": "Aelion, o Arco do Crepúsculo", "info": "Alto Elfo · Longa distância"},
	{"n": 12, "nome": "Umbra, a Lâmina da Noite", "info": "Elfa Negra · Ataque / Perseguição"},
	{"n": 13, "nome": "Brok, o Ferreiro de Runas", "info": "Anão · Suporte / Proteção"},
	{"n": 14, "nome": "Gromm, o Xamã da Tempestade", "info": "Orc · Suporte / Vida"},
	{"n": 15, "nome": "Maris, a Voz das Marés", "info": "Nereida · Longa distância"},
	{"n": 16, "nome": "Fizz, o Artífice de Bolso", "info": "Gnomo · Longa distância"},
	{"n": 17, "nome": "Sylva, a Filha da Floresta", "info": "Dríade · Suporte / Vida"},
	{"n": 18, "nome": "Basalto, o Desperto", "info": "Golem rúnico · Curta distância"},
	{"n": 19, "nome": "Noctus, o Sedento de Éter", "info": "Vampiro arcano · Ataque / Perseguição"},
	{"n": 20, "nome": "Pip, a Centelha Selvagem", "info": "Fada · Ataque / Perseguição"},
]

## O selftest desliga isto para provar o sinal sem trocar de cena.
var scene_switch_enabled := true

var _tela_titulo: Control
var _tela_menu: Control
var _tela_elenco: Control
var _screen: Control
var _veu: ColorRect        # fade de saida — nada de tela branca seca
var _tap: Label
var _particulas: CPUParticles2D
var _starting := false
var _phase := 0.0


func _ready() -> void:
	_build_fundo()
	_tela_titulo = _build_titulo()
	_tela_menu = _build_menu()
	_tela_elenco = _build_elenco()
	_veu = ColorRect.new()
	_veu.color = NIGHT_DEEP
	_veu.visible = false
	_veu.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(_veu)
	_veu.set_anchors_preset(Control.PRESET_FULL_RECT)
	_tela_menu.visible = false
	_tela_elenco.visible = false
	_screen = _tela_titulo
	resized.connect(_layout_particulas)
	_layout_particulas()


func _process(delta: float) -> void:
	_phase += delta
	if _tap != null and _tap.is_visible_in_tree():
		_tap.modulate.a = 0.5 + 0.4 * sin(_phase * 3.0)


# ---------------------------------------------------------------- fundo vivo

func _build_fundo() -> void:
	# gradiente vertical azul-noite (procedural, sem textura em disco)
	add_child(_grad_rect(
		[NIGHT_DEEP, NIGHT, Color("#131B3E")], [0.0, 0.55, 1.0],
		Vector2(0.5, 0.0), Vector2(0.5, 1.0), GradientTexture2D.FILL_LINEAR))
	# halo dourado sutil atras do centro da tela
	add_child(_grad_rect(
		[Color(GOLD, 0.10), Color(GOLD, 0.0)], [0.0, 1.0],
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
	ramp.colors = PackedColorArray([Color(GOLD, 0.0), Color(GOLD, 0.5), Color(GOLD, 0.0)])
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
	var tap_btn := Button.new()
	tap_btn.flat = true
	tap_btn.focus_mode = Control.FOCUS_NONE
	for st in ["normal", "hover", "pressed", "focus"]:
		tap_btn.add_theme_stylebox_override(st, StyleBoxEmpty.new())
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
	var sub := _label("Magos Battle Royale", 26, TEXT_DIM)
	sub.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	v.add_child(sub)

	_tap = _label("TOQUE PARA COMEÇAR", 24, GOLD)
	_tap.add_theme_constant_override("outline_size", 2)
	_tap.add_theme_color_override("font_outline_color", Color(NIGHT_DEEP, 0.8))
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
	for ch in "ARKANA":
		var l := _label(ch, tam, RAIO if ch == "K" else GOLD)
		l.add_theme_constant_override("outline_size", maxi(2, tam / 22))
		l.add_theme_color_override("font_outline_color",
			Color(RAIO if ch == "K" else GOLD, 0.9))
		l.add_theme_color_override("font_shadow_color", Color(NIGHT_DEEP, 0.85))
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
	v.add_theme_constant_override("separation", 16)
	cc.add_child(v)
	v.add_child(_wordmark(44, 8))
	var esp := Control.new()
	esp.custom_minimum_size = Vector2(0, 10)
	v.add_child(esp)

	var jogar := _btn("JOGAR", "BtnJogar", 56.0)
	jogar.pressed.connect(_on_jogar)
	v.add_child(jogar)
	var pers := _btn("PERSONAGENS", "BtnPersonagens", 56.0)
	pers.pressed.connect(func() -> void: _go(_tela_elenco))
	v.add_child(pers)
	var sair := _btn("SAIR", "BtnSair", 56.0)
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


# ---------------------------------------------------------------- vitrine do elenco

func _build_elenco() -> Control:
	var t := _tela("Elenco")

	var topo := Control.new()
	topo.mouse_filter = Control.MOUSE_FILTER_IGNORE
	t.add_child(topo)
	topo.set_anchors_preset(Control.PRESET_TOP_WIDE)
	topo.offset_bottom = 72.0
	var titulo := _label("ELENCO — 20 MAGOS", 30, GOLD)
	titulo.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	titulo.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	topo.add_child(titulo)
	titulo.set_anchors_preset(Control.PRESET_FULL_RECT)
	var voltar := _btn("← VOLTAR", "BtnVoltar", 48.0)
	voltar.custom_minimum_size.x = 170.0
	voltar.add_theme_font_size_override("font_size", 22)
	voltar.pressed.connect(func() -> void: _go(_tela_menu))
	topo.add_child(voltar)
	voltar.position = Vector2(16, 10)

	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	t.add_child(scroll)
	scroll.set_anchors_preset(Control.PRESET_FULL_RECT)
	scroll.offset_top = 76.0
	scroll.offset_left = 16.0
	scroll.offset_right = -16.0
	scroll.offset_bottom = -12.0
	var wrap := CenterContainer.new()
	wrap.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(wrap)
	var grid := GridContainer.new()
	grid.columns = 5
	grid.add_theme_constant_override("h_separation", 12)
	grid.add_theme_constant_override("v_separation", 12)
	wrap.add_child(grid)
	for d in ELENCO:
		grid.add_child(_card(d))
	return t


func _card(d: Dictionary) -> PanelContainer:
	var n: int = d.n
	var temporada := n >= 11
	var card := PanelContainer.new()
	card.name = "Card%02d" % n
	card.add_to_group("elenco_card")
	card.custom_minimum_size = Vector2(228, 204)
	var sb := StyleBoxFlat.new()
	sb.bg_color = Color("#0E1430")
	sb.border_color = Color(GOLD_DIM, 0.35 if temporada else 0.6)
	sb.set_border_width_all(1)
	sb.set_corner_radius_all(10)
	sb.set_content_margin_all(8)
	card.add_theme_stylebox_override("panel", sb)

	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 4)
	card.add_child(v)

	# --- retrato: AQUI a arte do Diretor substitui a silhueta (ver ART_DIR) ---
	var art_path := "%s/%02d.png" % [ART_DIR, n]
	if ResourceLoader.exists(art_path):
		var tr := TextureRect.new()
		tr.texture = load(art_path)
		tr.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		tr.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		tr.custom_minimum_size = Vector2(0, 104)
		v.add_child(tr)
	else:
		var sil := Silhueta.new()
		sil.cor = Color("#232A4A") if temporada else Color("#333D66")
		sil.custom_minimum_size = Vector2(0, 104)
		v.add_child(sil)

	var nome := _label("%02d · %s" % [n, d.nome], 15, TEXT_COL)
	nome.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	nome.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	v.add_child(nome)
	var info := _label(d.info, 13, TEXT_DIM)
	info.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	info.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	v.add_child(info)
	if temporada:
		var breve := _label("EM BREVE", 13, GOLD)
		breve.name = "EmBreve"
		breve.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		v.add_child(breve)
	return card


# ---------------------------------------------------------------- helpers

func _tela(nome: String) -> Control:
	var t := Control.new()
	t.name = nome
	t.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(t)
	t.set_anchors_preset(Control.PRESET_FULL_RECT)
	return t


func _label(texto: String, tam: int, cor: Color) -> Label:
	var l := Label.new()
	l.text = texto
	l.add_theme_font_size_override("font_size", tam)
	l.add_theme_color_override("font_color", cor)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l


## Botao grande de menu. min_dp vira px de canvas via Dp (regra do projeto:
## o que o dedo sente vive em dp); nunca abaixo do proprio valor em px.
func _btn(texto: String, nome: String, min_dp: float) -> Button:
	var b := Button.new()
	b.name = nome
	b.text = texto
	b.focus_mode = Control.FOCUS_NONE
	b.custom_minimum_size = Vector2(340, maxf(Dp.px(min_dp), min_dp))
	b.add_theme_font_size_override("font_size", 28)
	b.add_theme_color_override("font_color", GOLD)
	b.add_theme_color_override("font_hover_color", Color("#FFE9A8"))
	b.add_theme_color_override("font_pressed_color", NIGHT_DEEP)
	var normal := StyleBoxFlat.new()
	normal.bg_color = Color("#101838")
	normal.border_color = Color(GOLD_DIM, 0.8)
	normal.set_border_width_all(2)
	normal.set_corner_radius_all(12)
	b.add_theme_stylebox_override("normal", normal)
	var hover: StyleBoxFlat = normal.duplicate()
	hover.border_color = GOLD
	b.add_theme_stylebox_override("hover", hover)
	var pressed: StyleBoxFlat = normal.duplicate()
	pressed.bg_color = GOLD
	pressed.border_color = GOLD
	b.add_theme_stylebox_override("pressed", pressed)
	return b


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


## Silhueta placeholder de mago (chapeu + cabeca + manto + cajado) ate' a
## arte do Diretor chegar em res://menu/art/NN.png.
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
		draw_line(Vector2(cx + w * 0.20, h * 0.30), Vector2(cx + w * 0.24, h * 0.95), cor, 3.0, true)
		draw_circle(Vector2(cx + w * 0.195, h * 0.26), w * 0.035, Color("#8A7336"))
