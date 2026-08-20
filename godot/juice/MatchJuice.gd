## Camada de APRESENTACAO da partida (raia JUICE, R19). CanvasLayer
## autossuficiente: adicionada 'a cena, liga-se ao Bus sozinha e NADA depende
## dela — sem esta camada a partida roda identica (regra do core: OBSERVA,
## nunca decide jogo). Tudo aqui e' Control/gradiente barato; zero shader
## fullscreen por frame (regua mobile). Coluna central da mira: so' a contagem
## (transitoria, <=2.5s) e o "+1" (0.8s) pisam nela — nada persistente.
class_name MatchJuice
extends CanvasLayer

# ---------- knobs do Diretor ----------
const SHAKE := 1.0             # 0.0 desliga o tremor; 1.0 = amplitude cheia
const SHAKE_THRESHOLD := 15.0  # dano (queda de hp) acima disso treme
const VIGNETTE_ALPHA := 0.45   # pico do flash de dano — SUTIL, nunca opaco
const FEED_MAX := 3            # linhas do kill feed
const GOLD := Color(1.0, 0.82, 0.3)

var _countdown_lbl: Label
var _feed: VBoxContainer
var _vignette: Control
var _marks: Control
var _sting_gold: TextureRect
var _sting_gray: ColorRect
var _last_hp := -1.0
var _trauma := 0.0
var _cam: Camera3D
var _cam_prev := Vector2.ZERO
var _cd_tween: Tween
var _flash_tween: Tween
var _sting_tween: Tween


func _ready() -> void:
	# Acima do Hud (10): contagem e sting cobrem a tela por <1s e SOMEM —
	# a tela de fim do Hud "aparece por cima depois" porque o sting esvai.
	# Tudo com MOUSE_FILTER_IGNORE: nenhum toque para aqui.
	layer = 11
	_build_vignette()
	_build_feed()
	_build_marks()
	_build_countdown()
	_build_stings()
	Bus.match_started.connect(_on_match_started)
	Bus.match_over.connect(_on_match_over)
	Bus.health_changed.connect(_on_health)
	Bus.player_killed_bot.connect(_on_kill)
	set_process(false)  # _process so' vive enquanto ha' trauma


# ---------- 1. contagem de inicio (3 · 2 · 1 · LUTE!) ----------

func _on_match_started() -> void:
	# Restart limpa TUDO — estado que atravessa partida ja' vazou 3x no projeto.
	_last_hp = -1.0
	_trauma = 0.0
	_restore_cam()
	set_process(false)
	if _flash_tween:
		_flash_tween.kill()
	_vignette.modulate.a = 0.0
	if _sting_tween:
		_sting_tween.kill()
	_sting_gold.visible = false
	_sting_gray.visible = false
	for c in _feed.get_children():
		c.free()
	for c in _marks.get_children():
		c.free()
	_run_countdown()


func _run_countdown() -> void:
	if _cd_tween:
		_cd_tween.kill()
	_countdown_lbl.visible = true
	_cd_step("3")
	_cd_tween = create_tween()
	# 4 passos x 0.55s + fade 0.3s = 2.5s total (ritmo mobile)
	_pop(_cd_tween)
	for txt in ["2", "1", "LUTE!"]:
		_cd_tween.tween_callback(_cd_step.bind(txt))
		_pop(_cd_tween)
	_cd_tween.tween_property(_countdown_lbl, "modulate:a", 0.0, 0.3)
	_cd_tween.tween_callback(func() -> void: _countdown_lbl.visible = false)


func _cd_step(txt: String) -> void:
	_countdown_lbl.text = txt
	_countdown_lbl.modulate.a = 1.0
	_countdown_lbl.pivot_offset = _countdown_lbl.size / 2.0


func _pop(tw: Tween) -> void:
	tw.tween_property(_countdown_lbl, "scale", Vector2.ONE, 0.55)\
			.from(Vector2(1.6, 1.6)).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)


# ---------- 2+4. kill feed e marca "+1" ----------

func _on_kill(bot_name: String) -> void:
	var l := Label.new()
	l.text = "Voce derrubou %s" % bot_name
	l.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	l.add_theme_font_size_override("font_size", maxi(int(Dp.px(13.0)), 9))
	l.add_theme_color_override("font_color", Color(1, 1, 1, 0.95))
	_feed.add_child(l)
	while _feed.get_child_count() > FEED_MAX:
		_feed.get_child(0).free()
	var tw := l.create_tween()
	tw.tween_interval(2.0)
	tw.tween_property(l, "modulate:a", 0.0, 1.0)  # vida total da linha: 3s
	tw.tween_callback(l.queue_free)
	_spawn_mark()


func _spawn_mark() -> void:
	var l := Label.new()
	l.text = "+1"
	l.add_theme_font_size_override("font_size", maxi(int(Dp.px(26.0)), 16))
	l.add_theme_color_override("font_color", GOLD)
	l.add_theme_color_override("font_outline_color", Color(0.25, 0.15, 0, 0.9))
	l.add_theme_constant_override("outline_size", maxi(int(Dp.px(3.0)), 2))
	_marks.add_child(l)
	l.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	l.position.y -= _marks.size.y * 0.28  # centro-baixo, acima dos controles
	var tw := l.create_tween().set_parallel()
	tw.tween_property(l, "position:y", -Dp.px(70.0), 0.8)\
			.as_relative().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(l, "modulate:a", 0.0, 0.8).set_ease(Tween.EASE_IN)
	tw.chain().tween_callback(l.queue_free)


# ---------- 3+6. flash de dano e screen shake ----------

func _on_health(current: float, _max_hp: float) -> void:
	if is_nan(current):
		return  # licao da ponte: NaN nao entra em conta de apresentacao
	var prev := _last_hp
	_last_hp = current
	if prev < 0.0 or current >= prev:
		return  # primeira leitura ou vida SUBINDO: nada de flash
	if _flash_tween:
		_flash_tween.kill()
	_vignette.modulate.a = VIGNETTE_ALPHA
	_flash_tween = create_tween()
	_flash_tween.tween_property(_vignette, "modulate:a", 0.0, 0.15)
	if prev - current > SHAKE_THRESHOLD and SHAKE > 0.0:
		_trauma = minf(_trauma + 0.6, 1.0)
		set_process(true)


func _process(delta: float) -> void:
	_trauma = maxf(_trauma - delta * 1.8, 0.0)
	var cam := get_viewport().get_camera_3d()
	if cam != _cam:
		_restore_cam()
		_cam = cam
		if _cam:
			_cam_prev = Vector2(_cam.h_offset, _cam.v_offset)
	if _cam:
		# ponytail: amplitude em METROS ~= 6px a 5m de alvo em 720p; o px exato
		# depende da profundidade — nao vale a conta por frame. So' offsets de
		# camera (apresentacao); nenhum estado de jogo e' tocado.
		var a := _trauma * _trauma * 0.06 * SHAKE
		_cam.h_offset = _cam_prev.x + randf_range(-a, a)
		_cam.v_offset = _cam_prev.y + randf_range(-a, a)
	if _trauma <= 0.0:
		_restore_cam()
		set_process(false)


func _restore_cam() -> void:
	if is_instance_valid(_cam):
		_cam.h_offset = _cam_prev.x
		_cam.v_offset = _cam_prev.y
	_cam = null


# ---------- 5. sting de fim (antes da tela do Hud, que fica por cima depois) ----------

func _on_match_over(victory: bool) -> void:
	if _sting_tween:
		_sting_tween.kill()
	_sting_gold.visible = false
	_sting_gray.visible = false
	_sting_tween = create_tween()
	if victory:
		# veu dourado SUBINDO por 0.8s, depois esvai — revela a tela do Hud
		_sting_gold.visible = true
		_sting_gold.modulate.a = 1.0
		_sting_gold.anchor_top = 1.0
		_sting_tween.tween_property(_sting_gold, "anchor_top", 0.0, 0.45)\
				.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
		_sting_tween.tween_property(_sting_gold, "modulate:a", 0.0, 0.35)
		_sting_tween.tween_callback(func() -> void: _sting_gold.visible = false)
	else:
		# "dessaturacao" barata: veu CINZA medio lava a cor sem escurecer
		# (licao: nunca apagar a luz da tela) e sem shader fullscreen.
		_sting_gray.visible = true
		_sting_gray.modulate.a = 0.0
		_sting_tween.tween_property(_sting_gray, "modulate:a", 0.5, 0.25)
		_sting_tween.tween_property(_sting_gray, "modulate:a", 0.0, 0.55)
		_sting_tween.tween_callback(func() -> void: _sting_gray.visible = false)


# ---------- montagem (tudo Control barato, mouse_filter IGNORE) ----------

func _build_vignette() -> void:
	_vignette = Control.new()
	_vignette.set_anchors_preset(Control.PRESET_FULL_RECT)
	_vignette.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_vignette.modulate.a = 0.0
	# 4 gradientes nas BORDAS (64x64 gerados uma vez, esticados) — nada de
	# retangulo opaco fullscreen; o centro da tela fica 100% limpo.
	_vignette.add_child(_edge(0.0, 0.0, 1.0, 0.16, Vector2(0.5, 0), Vector2(0.5, 1)))
	_vignette.add_child(_edge(0.0, 0.84, 1.0, 1.0, Vector2(0.5, 1), Vector2(0.5, 0)))
	_vignette.add_child(_edge(0.0, 0.0, 0.12, 1.0, Vector2(0, 0.5), Vector2(1, 0.5)))
	_vignette.add_child(_edge(0.88, 0.0, 1.0, 1.0, Vector2(1, 0.5), Vector2(0, 0.5)))
	add_child(_vignette)


func _edge(al: float, at: float, ar: float, ab: float,
		from: Vector2, to: Vector2) -> TextureRect:
	var g := Gradient.new()
	g.set_color(0, Color(0.9, 0.12, 0.08, 1.0))
	g.set_color(1, Color(0.9, 0.12, 0.08, 0.0))
	var t := GradientTexture2D.new()
	t.gradient = g
	t.fill_from = from
	t.fill_to = to
	var r := TextureRect.new()
	r.texture = t
	r.stretch_mode = TextureRect.STRETCH_SCALE
	r.mouse_filter = Control.MOUSE_FILTER_IGNORE
	r.anchor_left = al
	r.anchor_top = at
	r.anchor_right = ar
	r.anchor_bottom = ab
	return r


func _build_feed() -> void:
	# Sup-direito, ABAIXO do timer/bots do Hud (~60dp de topo)
	_feed = VBoxContainer.new()
	_feed.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_feed.anchor_left = 1.0
	_feed.anchor_right = 1.0
	_feed.offset_left = -Dp.px(260.0)
	_feed.offset_right = -Dp.px(16.0)
	_feed.offset_top = Dp.px(64.0)
	_feed.add_theme_constant_override("separation", int(Dp.px(2.0)))
	add_child(_feed)


func _build_marks() -> void:
	_marks = Control.new()
	_marks.set_anchors_preset(Control.PRESET_FULL_RECT)
	_marks.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_marks)


func _build_countdown() -> void:
	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_countdown_lbl = Label.new()
	_countdown_lbl.visible = false
	_countdown_lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(56.0)), 32))
	_countdown_lbl.add_theme_color_override("font_color", GOLD)
	_countdown_lbl.add_theme_color_override("font_outline_color", Color(0.25, 0.15, 0, 0.9))
	_countdown_lbl.add_theme_constant_override("outline_size", maxi(int(Dp.px(4.0)), 3))
	center.add_child(_countdown_lbl)
	add_child(center)


func _build_stings() -> void:
	var g := Gradient.new()
	g.set_color(0, Color(GOLD.r, GOLD.g, GOLD.b, 0.55))
	g.set_color(1, Color(GOLD.r, GOLD.g, GOLD.b, 0.0))
	var t := GradientTexture2D.new()
	t.gradient = g
	t.fill_from = Vector2(0.5, 1)  # dourado embaixo, esvai para cima
	t.fill_to = Vector2(0.5, 0)
	_sting_gold = TextureRect.new()
	_sting_gold.texture = t
	_sting_gold.stretch_mode = TextureRect.STRETCH_SCALE
	_sting_gold.set_anchors_preset(Control.PRESET_FULL_RECT)
	_sting_gold.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_sting_gold.visible = false
	add_child(_sting_gold)

	_sting_gray = ColorRect.new()
	_sting_gray.color = Color(0.55, 0.55, 0.55, 0.9)
	_sting_gray.set_anchors_preset(Control.PRESET_FULL_RECT)
	_sting_gray.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_sting_gray.visible = false
	add_child(_sting_gray)
