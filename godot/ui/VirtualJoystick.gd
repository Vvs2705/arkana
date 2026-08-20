## Joystick virtual esquerdo (GDD §19.3). Toque cru via _input com indice
## proprio — convive com o botao de Fogo em multi-toque. Mouse emula toque
## pelo project.godot, entao teste no desktop vale.
class_name VirtualJoystick
extends Control

var value := Vector2.ZERO  # comprimento <= 1; y negativo = frente
var _touch := -1


func _ready() -> void:
	mouse_filter = MOUSE_FILTER_IGNORE


func _input(e: InputEvent) -> void:
	if e is InputEventScreenTouch:
		if e.pressed and _touch == -1 and get_global_rect().has_point(e.position):
			_touch = e.index
			_update(e.position)
		elif not e.pressed and e.index == _touch:
			_touch = -1
			value = Vector2.ZERO
			queue_redraw()
	elif e is InputEventScreenDrag and e.index == _touch:
		_update(e.position)


func _update(pos_global: Vector2) -> void:
	var local := pos_global - get_global_rect().position
	var radius := size.x / 2.0 * 0.75
	value = ((local - size / 2.0) / radius).limit_length(1.0)
	if value.length() < 0.12:
		value = Vector2.ZERO
	queue_redraw()


func _draw() -> void:
	var c := size / 2.0
	var r := size.x / 2.0 - Dp.px(2.0)
	draw_arc(c, r, 0, TAU, 48, Color(1, 1, 1, 0.35), Dp.px(2.0), true)
	draw_circle(c, r * 0.12, Color(1, 1, 1, 0.2))
	draw_circle(c + value * r * 0.6, Dp.px(22.0), Color(1, 1, 1, 0.45))
