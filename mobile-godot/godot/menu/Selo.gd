## O SELO DE ARKANA — a marca do jogo, desenhada em codigo.
##
## MESMO desenho do buildSeal de roblox/src/client/Hud.luau: pentagono (raio
## 0.38 do lado), 5 gemas-losango nas pontas com as cores dos elementos (GDD
## par.10) e o "A" dourado no centro. As gemas pulsam devagar.
##
## POR QUE MORA NUM ARQUIVO SO', e nao mais dentro de menu/Menu.gd: o icone do
## aparelho e a splash de abertura precisam do selo em PNG, e quem gera esses
## PNG (menu/_marca.gd) roda como --script, ANTES dos autoloads existirem. Um
## preload de Menu.gd nessa hora quebra em "Identifier not found: Bus" — a
## mesma armadilha que o cabecalho de Menu.gd ja' descrevia. Aqui nao ha'
## dependencia de autoload nenhuma, entao o selo pode ser carregado de qualquer
## lugar: da tela de titulo, da ferramenta de marca ou de um teste.
##
## SEM class_name, pela mesma razao de Menu.gd: quem precisa, faz preload.
extends Control

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
