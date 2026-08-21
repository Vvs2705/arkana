## Botao de Fogo — REVISADO NA R17 pelo veredito do Diretor no aparelho:
## clicar DISPARA na hora; SEGURAR mantem disparando enquanto houver mana;
## arrastar direciona a magia. ANEL verde aceso = disparando.
class_name FireButton
extends Control

signal fired                    # um pedido de disparo (o Player barra por mana)
signal aim_state(aiming: bool)  # o player gira para a mira enquanto true
signal aim_delta(rel: Vector2)  # arrasto -> camera/reticulo acompanham

var gesture := FireGesture.new()
var _touch := -1
var _color := Projectile.tint("fire")
var _label: String = Textos.HUD_ELEMENTOS.fire


func _ready() -> void:
	mouse_filter = MOUSE_FILTER_IGNORE
	gesture.repeat_ms = int(float(Balance.FIRE.fire_rate) * 1000.0)
	# deadzone e' dp -> px de CANVAS, e essa conversao muda quando a janela
	# muda (rotacao, dobravel). Recalcula junto com o tamanho do botao.
	resized.connect(_tune_deadzone)
	_tune_deadzone()
	Bus.element_changed.connect(_on_element)  # UI OBSERVA; quem decide e' o Player


func _tune_deadzone() -> void:
	gesture.deadzone_px = Dp.px(float(Balance.TOUCH.aim_deadzone_dp))


## O disparo continuo (R17) vale para os 3 elementos: cadencia/cor/rotulo
## seguem o elemento ativo — numeros de Balance via Projectile.spec.
func _on_element(el: String) -> void:
	gesture.repeat_ms = int(float(Projectile.spec(el).fire_rate) * 1000.0)
	_color = Projectile.tint(el)
	_label = str(Textos.HUD_ELEMENTOS.get(el, Textos.HUD_ELEMENTOS.fire))
	queue_redraw()


func _input(e: InputEvent) -> void:
	if e is InputEventScreenTouch:
		if e.pressed and _touch == -1 and get_global_rect().has_point(e.position):
			_touch = e.index
			if gesture.press(e.position, Time.get_ticks_msec()) == FireGesture.Result.FIRE:
				fired.emit()  # clicar JA' dispara — pedido do Diretor no aparelho
			queue_redraw()
		elif not e.pressed and e.index == _touch:
			_touch = -1
			gesture.release(Time.get_ticks_msec())
			aim_state.emit(false)
			queue_redraw()
	elif e is InputEventScreenDrag and e.index == _touch:
		gesture.drag(e.position)
		if gesture.has_aimed():
			aim_state.emit(true)
			aim_delta.emit(e.relative)
		queue_redraw()


func _process(_delta: float) -> void:
	if _touch != -1:
		# SEGURAR = auto-fogo por cadencia. A mana quem barra e' o Player
		# (autoridade unica de custo) — aqui so' se PEDE.
		if gesture.poll(Time.get_ticks_msec()) == FireGesture.Result.FIRE:
			fired.emit()
		queue_redraw()


func _draw() -> void:
	var c := size / 2.0
	var r := minf(size.x, size.y) / 2.0 - Dp.px(4.0)
	draw_circle(c + Vector2(Dp.px(2.0), Dp.px(3.0)), r, Color(0, 0, 0, 0.22))
	draw_circle(c, r, Color(_color.darkened(0.35), 0.38))
	draw_arc(c, r, 0, TAU, 40, Color(1, 1, 1, 0.62), Dp.px(1.8), true)
	var fs := maxi(int(Dp.px(13.0)), 8)
	draw_string(get_theme_default_font(), Vector2(0, c.y + fs * 0.35), _label,
			HORIZONTAL_ALIGNMENT_CENTER, size.x, fs, Color(1, 1, 0.92, 0.92))
	if gesture.visual(Time.get_ticks_msec()) == FireGesture.Visual.RING:
		draw_arc(c, r + Dp.px(5.0), 0, TAU, 48, Color(0.2, 1.0, 0.4), Dp.px(3.5), true)
