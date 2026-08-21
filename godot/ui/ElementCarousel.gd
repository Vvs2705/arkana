## Carrossel de elementos — acima do botao de disparo (GDD §19.3).
## Cada slot >= 48dp, cor + FORMA no icone (GDD §10 — lei): fogo = gota,
## agua = crescente, raio = zigzag, terra = pedra facetada, vento = espiral.
## 5 slots em UMA fileira (5 x 52dp = 260dp, cabe folgado no canto direito).
## OBSERVA a selecao pelo Bus (feedback imediato vem do Bus.element_changed
## que o Player emite); o toque so' PEDE.
class_name ElementCarousel
extends Control

signal chosen(element: String)

var selected := "fire"


func _ready() -> void:
	mouse_filter = MOUSE_FILTER_IGNORE
	Bus.element_changed.connect(func(el: String) -> void:
		selected = el
		queue_redraw())


func _input(e: InputEvent) -> void:
	if e is InputEventScreenTouch and e.pressed and get_global_rect().has_point(e.position):
		var els: Array = Balance.ELEMENTS
		var local: float = e.position.x - get_global_rect().position.x
		var i := clampi(int(local / (size.x / els.size())), 0, els.size() - 1)
		chosen.emit(str(els[i]))


func _draw() -> void:
	var els: Array = Balance.ELEMENTS
	var slot := size.x / els.size()
	for i in els.size():
		var el := str(els[i])
		var c := Vector2(slot * (i + 0.5), size.y / 2.0)
		var r := minf(slot, size.y) / 2.0 - Dp.px(4.0)
		var col: Color = Projectile.tint(el)
		draw_circle(c + Vector2(Dp.px(1.5), Dp.px(2.0)), r, Color(0, 0, 0, 0.16))
		draw_circle(c, r, Color(col.darkened(0.12), 0.18 if el != selected else 0.38))
		if el == selected:
			draw_arc(c, r + Dp.px(2.0), 0, TAU, 40, Color.WHITE, Dp.px(2.2), true)
		_icon(el, c, r * 0.55, col)


## FORMA por elemento — nunca so' cor (daltonismo, GDD §10).
func _icon(el: String, c: Vector2, r: float, col: Color) -> void:
	match el:
		"water":  # crescente: arco grosso aberto
			draw_arc(c, r, PI * 0.65, PI * 1.85, 24, col, r * 0.55, true)
		"lightning":  # zigzag/raio
			var pts := PackedVector2Array([
				c + Vector2(r * 0.35, -r), c + Vector2(-r * 0.35, r * 0.15),
				c + Vector2(r * 0.15, r * 0.15), c + Vector2(-r * 0.35, r),
			])
			draw_polyline(pts, col, maxf(r * 0.3, 1.0), true)
		"earth":  # pedra facetada: poligono irregular cheio
			draw_colored_polygon(PackedVector2Array([
				c + Vector2(-r * 0.9, -r * 0.1), c + Vector2(-r * 0.35, -r * 0.9),
				c + Vector2(r * 0.55, -r * 0.75), c + Vector2(r * 0.9, r * 0.2),
				c + Vector2(r * 0.3, r * 0.9), c + Vector2(-r * 0.6, r * 0.7),
			]), col)
		"wind":  # espiral de ar: linha que enrola para o centro
			var spiral := PackedVector2Array()
			for k in 20:
				var t := float(k) / 19.0
				spiral.append(c + Vector2.from_angle(t * TAU * 1.6 - PI * 0.5) * r * (1.0 - t * 0.85))
			draw_polyline(spiral, col, maxf(r * 0.28, 1.0), true)
		_:  # gota/esfera flamejante: circulo + bico
			draw_circle(c + Vector2(0, r * 0.25), r * 0.75, col)
			draw_colored_polygon(PackedVector2Array([
				c + Vector2(0, -r), c + Vector2(-r * 0.5, r * 0.1), c + Vector2(r * 0.5, r * 0.1),
			]), col)
