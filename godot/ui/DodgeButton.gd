## Botao de Esquiva (Balance.DODGE) — alvo >= 48dp, cooldown visivel no
## proprio botao (arco escuro que encolhe ate' liberar). So' PEDE a esquiva;
## quem decide (cooldown, i-frames) e' o Pawn.
class_name DodgeButton
extends Control

signal dodged

var cd_frac := Callable()  # devolve 0..1 (0 = pronta); a HUD liga no Player
var _touch := -1
var _last_frac := -1.0


func _ready() -> void:
	mouse_filter = MOUSE_FILTER_IGNORE


func _input(e: InputEvent) -> void:
	if e is InputEventScreenTouch:
		if e.pressed and _touch == -1 and get_global_rect().has_point(e.position):
			_touch = e.index
			dodged.emit()
			queue_redraw()
		elif not e.pressed and e.index == _touch:
			_touch = -1


func _process(_delta: float) -> void:
	var f := _frac()
	if not is_equal_approx(f, _last_frac):
		queue_redraw()  # anima o cooldown so' quando muda


func _frac() -> float:
	if cd_frac.is_valid():
		return clampf(float(cd_frac.call()), 0.0, 1.0)
	return 0.0


func _draw() -> void:
	var c := size / 2.0
	var r := minf(size.x, size.y) / 2.0 - Dp.px(3.0)
	var frac := _frac()
	_last_frac = frac
	var ready_now := frac <= 0.0
	draw_circle(c, r, Color(0.25, 0.85, 0.95, 0.55 if ready_now else 0.22))
	draw_arc(c, r, 0, TAU, 40, Color(1, 1, 1, 0.5), Dp.px(1.5), true)
	if frac > 0.0:
		draw_arc(c, r * 0.62, -PI / 2.0, -PI / 2.0 + TAU * frac, 32,
				Color(0, 0, 0, 0.55), Dp.px(10.0), true)
	var fs := maxi(int(Dp.px(11.0)), 8)
	draw_string(get_theme_default_font(), Vector2(0, c.y + fs * 0.35), "ESQV",
			HORIZONTAL_ALIGNMENT_CENTER, size.x, fs, Color(1, 1, 1, 0.9))
