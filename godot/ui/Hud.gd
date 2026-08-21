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
var fps_lbl: Label
var end_screen: Control
var end_lbl: Label
var _look_touch := -1
var _reticle: Control
var _hit_flash := 0.0
var _screen_grade: Control
var _bars_box: VBoxContainer
var _top_box: VBoxContainer
var _end_center: CenterContainer
var _numeros_dano := true
var _filtro: FiltroDaltonismo


func _ready() -> void:
	layer = 10
	_build_screen_grade()
	_build_bars()
	_build_top_right()
	_build_reticle()
	_build_sticks()
	_build_end()
	_layout()
	# Rotacao / split-screen / dobravel mudam a area segura E a escala do dp,
	# entao a HUD nao pode ser montada uma vez so' em _ready.
	get_tree().root.size_changed.connect(_layout)
	Bus.health_changed.connect(_on_hp)
	Bus.mana_changed.connect(_on_mana)
	Bus.damage_dealt.connect(_on_damage)
	# Contador de FPS, numeros de dano e daltonismo sao opcoes do jogador
	# (GDD par.12) e a HUD e' a dona delas — ver o grupo em menu/Config.gd.
	add_to_group("config_ouvintes")
	aplicar_config(Config.atual())


## Chega no boot e a cada mexida no menu. Nada aqui exige reiniciar.
func aplicar_config(cfg: Dictionary) -> void:
	var video: Dictionary = cfg.get("video", {})
	var jogo: Dictionary = cfg.get("jogo", {})
	fps_lbl.visible = bool(video.get("contador_fps", false))
	_numeros_dano = bool(jogo.get("numeros_dano", true))
	var modo := int(jogo.get("daltonismo", 0))
	# O passe de tela custa uma copia de framebuffer por quadro: so' nasce se
	# alguem precisar dele (ver ui/FiltroDaltonismo.gd).
	if modo != 0 and _filtro == null:
		_filtro = FiltroDaltonismo.new()
		add_child(_filtro)
	if _filtro != null:
		_filtro.modo = modo


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
	if fps_lbl.visible:
		fps_lbl.text = "%d FPS" % Engine.get_frames_per_second()


## Hitmarker sutil: dano em quem NAO e' o player = acerto do player.
## ponytail: bots so' atacam o player, entao a heuristica basta; se um dia
## bot atacar bot, o Bus precisa carregar o atirador.
func _on_damage(target: Node, amount: int, element: String) -> void:
	if not target.is_in_group("player"):
		_hit_flash = 0.16
		_reticle.queue_redraw()
		if _numeros_dano:
			_numero_dano(target, amount, element)


## Numero de dano flutuante (GDD par.12): nasce na cabeca do alvo projetada na
## tela, sobe e some em 0.7s. Cor do elemento — a mesma do projetil e do slot.
## ponytail: uma Label por acerto, sem pool. Com 6 bots num tiro sao poucas por
## segundo; se a partida virar 20 jogadores, ai' sim vira pool.
func _numero_dano(target: Node, amount: int, element: String) -> void:
	var cam := get_viewport().get_camera_3d()
	if cam == null or not (target is Node3D):
		return
	var mundo: Vector3 = (target as Node3D).global_position + Vector3(0, 1.7, 0)
	if cam.is_position_behind(mundo):
		return  # alvo atras da camera projetaria o numero no lugar errado
	var lbl := Label.new()
	lbl.text = str(amount)
	lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(18.0)), 11))
	lbl.add_theme_color_override("font_color", Projectile.tint(element))
	lbl.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.85))
	lbl.add_theme_constant_override("outline_size", maxi(int(Dp.px(3.0)), 2))
	lbl.position = cam.unproject_position(mundo)
	add_child(lbl)
	var t := create_tween().set_parallel()
	t.tween_property(lbl, "position:y", lbl.position.y - Dp.px(34.0), 0.7)
	t.tween_property(lbl, "modulate:a", 0.0, 0.7).set_ease(Tween.EASE_IN)
	t.chain().tween_callback(lbl.queue_free)


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


# ---------- posicionamento (area segura) ----------

## TODA margem de borda mora aqui — nada de offset cru espalhado na montagem.
## O APK R19 nascia errado porque a HUD encostava na borda FISICA da tela: no
## celular a borda esconde notch/camera (nas laterais, em paisagem) e barra de
## gestos (embaixo). Safe.* devolve quanto recuar em px de canvas.
##
## Os parametros existem para o selftest injetar margens falsas: headless nao
## tem notch (Safe.* = 0) e um teste sem inset nao provaria nada. Em jogo os
## defaults sao avaliados na chamada, entao _layout() usa o Safe de verdade.
func _layout(sl := Safe.left(), st := Safe.top(), sr := Safe.right(), sb := Safe.bottom()) -> void:
	var m := Dp.px(16.0)   # respiro de leitura (barras, timer)
	var g := Dp.px(24.0)   # respiro de dedo (controles)

	_bars_box.position = Vector2(sl + m, st + m)
	for b: ProgressBar in [hp_bar, mana_bar]:
		b.custom_minimum_size = Vector2(Dp.px(130.0), Dp.px(12.0))

	_top_box.offset_left = -sr - Dp.px(180.0)
	_top_box.offset_right = -sr - m
	_top_box.offset_top = st + m

	var js := Dp.px(150.0)
	joystick.offset_left = sl + g
	joystick.offset_right = joystick.offset_left + js
	joystick.offset_bottom = -(sb + g)
	joystick.offset_top = joystick.offset_bottom - js

	# Fogo sobe um pouco mais que g: e' onde o polegar direito descansa e a
	# barra de gestos rouba os toques rasantes na borda de baixo.
	var fb := Dp.px(88.0)
	fire_btn.offset_right = -(sr + g)
	fire_btn.offset_left = fire_btn.offset_right - fb
	fire_btn.offset_bottom = -(sb + Dp.px(40.0))
	fire_btn.offset_top = fire_btn.offset_bottom - fb

	var db := Dp.px(64.0)
	dodge_btn.offset_right = fire_btn.offset_left - Dp.px(12.0)
	dodge_btn.offset_left = dodge_btn.offset_right - db
	dodge_btn.offset_bottom = fire_btn.offset_bottom
	dodge_btn.offset_top = dodge_btn.offset_bottom - db

	var slot := Dp.px(52.0)
	carousel.offset_right = -(sr + g)
	carousel.offset_left = carousel.offset_right - slot * float(Balance.ELEMENTS.size())
	carousel.offset_bottom = fire_btn.offset_top - Dp.px(10.0)
	carousel.offset_top = carousel.offset_bottom - slot

	# O escurecido cobre a tela inteira (fica bonito sangrando ate' a borda);
	# so' o CONTEUDO respeita a area util.
	Safe.apply(_end_center, 16.0)


# ---------- montagem ----------

func _build_bars() -> void:
	_bars_box = VBoxContainer.new()
	_bars_box.add_theme_constant_override("separation", int(Dp.px(4.0)))
	hp_bar = _bar(Color(0.85, 0.2, 0.2))
	mana_bar = _bar(Color(0.2, 0.45, 0.95))
	_bars_box.add_child(_bar_row("VIDA", hp_bar))
	_bars_box.add_child(_bar_row("MANA", mana_bar))
	add_child(_bars_box)


func _build_screen_grade() -> void:
	var grade := Control.new()
	grade.anchor_right = 1.0
	grade.anchor_bottom = 1.0
	grade.mouse_filter = Control.MOUSE_FILTER_IGNORE
	grade.draw.connect(func() -> void:
		var h := grade.size.y
		var w := grade.size.x
		grade.draw_rect(Rect2(0, 0, w, h * 0.19), Color(0.02, 0.025, 0.04, 0.22))
		grade.draw_rect(Rect2(0, h * 0.72, w, h * 0.28), Color(0.02, 0.025, 0.04, 0.16))
	)
	_screen_grade = grade
	add_child(grade)


func _bar(color: Color) -> ProgressBar:
	var b := ProgressBar.new()
	b.show_percentage = false  # tamanho vem do _layout (muda com a janela)
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
	_top_box = VBoxContainer.new()
	_top_box.anchor_left = 1.0
	_top_box.anchor_right = 1.0
	timer_lbl = Label.new()
	timer_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	timer_lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(20.0)), 12))
	bots_lbl = Label.new()
	bots_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	bots_lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(13.0)), 9))
	# Contador de FPS logo abaixo do placar: sup-dir ja' e' a coluna de numeros,
	# e continua FORA da coluna central (territorio da mira). Ligado no menu.
	fps_lbl = Label.new()
	fps_lbl.name = "ContadorFps"
	fps_lbl.visible = false
	fps_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	fps_lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(12.0)), 8))
	fps_lbl.add_theme_color_override("font_color", Color(0.6, 1.0, 0.7, 0.85))
	_top_box.add_child(timer_lbl)
	_top_box.add_child(bots_lbl)
	_top_box.add_child(fps_lbl)
	add_child(_top_box)


## SEM area segura de proposito: a mira marca o centro da CAMERA. Recuar do
## notch aqui separaria o reticulo de onde a magia sai.
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


## So' cria e liga os sinais — tamanho e margem sao do _layout (area segura).
## Alvos de toque em dp: joystick 150, Fogo 88, Esquiva 64, slot 52 (min 48dp).
func _build_sticks() -> void:
	joystick = VirtualJoystick.new()
	joystick.anchor_top = 1.0
	joystick.anchor_bottom = 1.0
	add_child(joystick)

	fire_btn = FireButton.new()
	fire_btn.anchor_left = 1.0
	fire_btn.anchor_right = 1.0
	fire_btn.anchor_top = 1.0
	fire_btn.anchor_bottom = 1.0
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

	# Esquiva a' esquerda do disparo — cooldown desenhado no proprio botao.
	dodge_btn = DodgeButton.new()
	dodge_btn.anchor_left = 1.0
	dodge_btn.anchor_right = 1.0
	dodge_btn.anchor_top = 1.0
	dodge_btn.anchor_bottom = 1.0
	add_child(dodge_btn)
	dodge_btn.dodged.connect(func() -> void:
		if is_instance_valid(player):
			player.request_dodge())

	# Carrossel de elementos ACIMA do botao de disparo (GDD §19.3). 5 slots de
	# 52dp (>= 48dp) em UMA fileira: 260dp cabem no canto direito sem invadir a
	# zona do joystick (35% da esquerda) nem a de olhar — 2 fileiras so' se um
	# 6o elemento entrar.
	carousel = ElementCarousel.new()
	carousel.anchor_left = 1.0
	carousel.anchor_right = 1.0
	carousel.anchor_top = 1.0
	carousel.anchor_bottom = 1.0
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
	_end_center = CenterContainer.new()
	_end_center.anchor_right = 1.0
	_end_center.anchor_bottom = 1.0
	end_screen.add_child(_end_center)
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
	_end_center.add_child(box)
	add_child(end_screen)
