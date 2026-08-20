## HUD minimo, FORA da coluna central (territorio da mira — licao do projeto):
## vida/mana sup-esq, timer/bots sup-dir, joystick e Fogo na metade de baixo.
## OBSERVA o jogo pelo Bus (regra do core); encaminha toque para o player.
class_name Hud
extends CanvasLayer

signal restart_pressed

var player: Player
var joystick: VirtualJoystick
var fire_btn: FireButton
var dodge_btn: DodgeButton
var carousel: ElementCarousel
var hp_bar: ProgressBar
var mana_bar: ProgressBar
var timer_lbl: Label
var bots_lbl: Label
var end_screen: Control
var end_lbl: Label
var _look_touch := -1
var _reticle: Control
var _hit_flash := 0.0


func _ready() -> void:
	layer = 10
	_build_bars()
	_build_top_right()
	_build_reticle()
	_build_sticks()
	_build_end()
	Bus.health_changed.connect(_on_hp)
	Bus.mana_changed.connect(_on_mana)
	Bus.damage_dealt.connect(_on_damage)


func bind_player(p: Player) -> void:
	player = p
	hp_bar.max_value = float(Balance.PLAYER.hp)
	hp_bar.value = p.hp
	mana_bar.max_value = float(Balance.PLAYER.mana_max)
	mana_bar.value = p.mana
	dodge_btn.cd_frac = p.dodge_cd_frac  # Callable morre com o player velho; is_valid() cobre


func update_match(time_left: float, bots: int) -> void:
	var t := maxi(int(ceilf(time_left)), 0)
	@warning_ignore("integer_division")
	timer_lbl.text = "%d:%02d" % [t / 60, t % 60]
	bots_lbl.text = "BOTS %d" % bots


func show_end(victory: bool) -> void:
	end_lbl.text = "VITORIA!" if victory else "DERROTA"
	end_lbl.add_theme_color_override("font_color",
			Color(0.35, 1.0, 0.45) if victory else Color(1.0, 0.35, 0.35))
	end_screen.visible = true


func hide_end() -> void:
	end_screen.visible = false


func _process(delta: float) -> void:
	if is_instance_valid(player):
		player.move_input = joystick.value
	if _hit_flash > 0.0:
		_hit_flash = maxf(_hit_flash - delta, 0.0)
		_reticle.queue_redraw()


## Hitmarker sutil: dano em quem NAO e' o player = acerto do player.
## ponytail: bots so' atacam o player, entao a heuristica basta; se um dia
## bot atacar bot, o Bus precisa carregar o atirador.
func _on_damage(target: Node, _amount: int, _element: String) -> void:
	if not target.is_in_group("player"):
		_hit_flash = 0.16
		_reticle.queue_redraw()


## Olhar livre: arrastar na metade direita FORA do botao de Fogo gira a camera.
func _input(e: InputEvent) -> void:
	if player == null or not is_instance_valid(player):
		return
	if e is InputEventScreenTouch:
		if e.pressed and _look_touch == -1 and _in_look_zone(e.position):
			_look_touch = e.index
		elif not e.pressed and e.index == _look_touch:
			_look_touch = -1
	elif e is InputEventScreenDrag and e.index == _look_touch:
		player.add_look(e.relative)


func _in_look_zone(pos: Vector2) -> bool:
	if end_screen.visible:
		return false
	var vp := get_viewport().get_visible_rect().size
	if pos.x < vp.x * 0.35:
		return false  # lado do joystick
	for c: Control in [fire_btn, dodge_btn, carousel]:
		if c.get_global_rect().has_point(pos):
			return false
	return true


func _on_hp(current: float, max_hp: float) -> void:
	hp_bar.max_value = max_hp
	hp_bar.value = current


func _on_mana(current: float, max_mana: float) -> void:
	mana_bar.max_value = max_mana
	mana_bar.value = current


# ---------- montagem ----------

func _build_bars() -> void:
	var m := Dp.px(16.0)
	var box := VBoxContainer.new()
	box.position = Vector2(m, m)
	box.add_theme_constant_override("separation", int(Dp.px(4.0)))
	hp_bar = _bar(Color(0.85, 0.2, 0.2))
	mana_bar = _bar(Color(0.2, 0.45, 0.95))
	box.add_child(_bar_row("VIDA", hp_bar))
	box.add_child(_bar_row("MANA", mana_bar))
	add_child(box)


func _bar(color: Color) -> ProgressBar:
	var b := ProgressBar.new()
	b.show_percentage = false
	b.custom_minimum_size = Vector2(Dp.px(130.0), Dp.px(12.0))
	var fill := StyleBoxFlat.new()
	fill.bg_color = color
	fill.set_corner_radius_all(int(Dp.px(4.0)))
	var bg := StyleBoxFlat.new()
	bg.bg_color = Color(0, 0, 0, 0.45)
	bg.set_corner_radius_all(int(Dp.px(4.0)))
	b.add_theme_stylebox_override("fill", fill)
	b.add_theme_stylebox_override("background", bg)
	return b


func _bar_row(text: String, bar: ProgressBar) -> HBoxContainer:
	var row := HBoxContainer.new()
	var lbl := Label.new()
	lbl.text = text
	lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(11.0)), 8))
	row.add_child(lbl)
	row.add_child(bar)
	return row


func _build_top_right() -> void:
	var m := Dp.px(16.0)
	var box := VBoxContainer.new()
	box.anchor_left = 1.0
	box.anchor_right = 1.0
	box.offset_left = -Dp.px(180.0)
	box.offset_right = -m
	box.offset_top = m
	timer_lbl = Label.new()
	timer_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	timer_lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(20.0)), 12))
	bots_lbl = Label.new()
	bots_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	bots_lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(13.0)), 9))
	box.add_child(timer_lbl)
	box.add_child(bots_lbl)
	add_child(box)


func _build_reticle() -> void:
	var ret := Control.new()
	ret.anchor_right = 1.0
	ret.anchor_bottom = 1.0
	ret.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ret.draw.connect(func() -> void:
		var c := ret.size / 2.0
		ret.draw_arc(c, Dp.px(6.0), 0, TAU, 24, Color(1, 1, 1, 0.8), Dp.px(1.2), true)
		ret.draw_circle(c, Dp.px(1.4), Color(1, 1, 1, 0.9))
		if _hit_flash > 0.0:  # hitmarker: 4 tracinhos diagonais que somem rapido
			var a := _hit_flash / 0.16
			var i := Dp.px(8.0)
			var o := Dp.px(13.0)
			for s in [Vector2(1, 1), Vector2(-1, 1), Vector2(1, -1), Vector2(-1, -1)]:
				ret.draw_line(c + s * i, c + s * o, Color(1, 1, 1, 0.85 * a), Dp.px(1.6), true))
	_reticle = ret
	add_child(ret)


func _build_sticks() -> void:
	var m := Dp.px(24.0)
	var js := Dp.px(150.0)
	joystick = VirtualJoystick.new()
	joystick.anchor_top = 1.0
	joystick.anchor_bottom = 1.0
	joystick.offset_left = m
	joystick.offset_right = m + js
	joystick.offset_top = -(m + js)
	joystick.offset_bottom = -m
	add_child(joystick)

	var fb := Dp.px(88.0)  # >= 48dp (alvo de toque)
	fire_btn = FireButton.new()
	fire_btn.anchor_left = 1.0
	fire_btn.anchor_right = 1.0
	fire_btn.anchor_top = 1.0
	fire_btn.anchor_bottom = 1.0
	fire_btn.offset_left = -(m + fb)
	fire_btn.offset_right = -m
	fire_btn.offset_top = -(Dp.px(40.0) + fb)
	fire_btn.offset_bottom = -Dp.px(40.0)
	add_child(fire_btn)
	fire_btn.fired.connect(func() -> void:
		if is_instance_valid(player):
			player.request_fire())
	fire_btn.aim_state.connect(func(a: bool) -> void:
		if is_instance_valid(player):
			player.set_aiming(a))
	fire_btn.aim_delta.connect(func(rel: Vector2) -> void:
		if is_instance_valid(player):
			player.add_look(rel))

	# Esquiva a' esquerda do disparo — alvo >= 48dp, cooldown no proprio botao.
	var db := Dp.px(64.0)
	dodge_btn = DodgeButton.new()
	dodge_btn.anchor_left = 1.0
	dodge_btn.anchor_right = 1.0
	dodge_btn.anchor_top = 1.0
	dodge_btn.anchor_bottom = 1.0
	dodge_btn.offset_right = -(m + fb + Dp.px(12.0))
	dodge_btn.offset_left = dodge_btn.offset_right - db
	dodge_btn.offset_bottom = -Dp.px(40.0)
	dodge_btn.offset_top = dodge_btn.offset_bottom - db
	add_child(dodge_btn)
	dodge_btn.dodged.connect(func() -> void:
		if is_instance_valid(player):
			player.request_dodge())

	# Carrossel de elementos ACIMA do botao de disparo (GDD §19.3), slots 52dp.
	var slot := Dp.px(52.0)
	carousel = ElementCarousel.new()
	carousel.anchor_left = 1.0
	carousel.anchor_right = 1.0
	carousel.anchor_top = 1.0
	carousel.anchor_bottom = 1.0
	carousel.offset_right = -m
	carousel.offset_left = -m - slot * float(Balance.ELEMENTS.size())
	carousel.offset_bottom = -(Dp.px(40.0) + fb + Dp.px(10.0))
	carousel.offset_top = carousel.offset_bottom - slot
	add_child(carousel)
	carousel.chosen.connect(func(el: String) -> void:
		if is_instance_valid(player):
			player.set_element(el))


func _build_end() -> void:
	end_screen = Control.new()
	end_screen.anchor_right = 1.0
	end_screen.anchor_bottom = 1.0
	end_screen.visible = false
	var dim := ColorRect.new()
	dim.color = Color(0, 0, 0, 0.55)
	dim.anchor_right = 1.0
	dim.anchor_bottom = 1.0
	end_screen.add_child(dim)
	var center := CenterContainer.new()
	center.anchor_right = 1.0
	center.anchor_bottom = 1.0
	end_screen.add_child(center)
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", int(Dp.px(16.0)))
	box.alignment = BoxContainer.ALIGNMENT_CENTER
	end_lbl = Label.new()
	end_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	end_lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(30.0)), 18))
	box.add_child(end_lbl)
	var btn := Button.new()
	btn.text = "JOGAR DE NOVO"
	btn.custom_minimum_size = Vector2(Dp.px(200.0), Dp.px(56.0))  # >= 48dp
	btn.add_theme_font_size_override("font_size", maxi(int(Dp.px(16.0)), 10))
	btn.pressed.connect(func() -> void: restart_pressed.emit())
	box.add_child(btn)
	# VOLTAR AO MENU (costura da R18, coordenador): o menu nao guarda estado e
	# reconstroi tudo em _ready, entao a troca de cena e' segura. Fiacao direta,
	# como o JOGAR do menu — defensiva se a cena sumir.
	var menu_btn := Button.new()
	menu_btn.text = "MENU"
	menu_btn.custom_minimum_size = Vector2(Dp.px(200.0), Dp.px(56.0))
	menu_btn.add_theme_font_size_override("font_size", maxi(int(Dp.px(16.0)), 10))
	menu_btn.pressed.connect(func() -> void:
		if ResourceLoader.exists("res://menu/Menu.tscn"):
			get_tree().change_scene_to_file("res://menu/Menu.tscn")
	)
	box.add_child(menu_btn)
	center.add_child(box)
	add_child(end_screen)
