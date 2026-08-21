## Selftest headless da raia UI/HUD (R19.2 — area segura). Rodar:
##   Godot --headless --path godot --script res://ui/selftest.gd
## Prova: a HUD monta; NENHUM elemento de borda ignora o notch/barra de gestos;
## refazer o layout nao acumula deriva; a HUD reage a rotacao/resize; os alvos
## de toque tem >= 48dp; e a base do viewport continua com cara de celular.
##
## POR QUE MARGEM INJETADA: headless (e desktop) nao tem notch — Safe.* devolve
## zero e um teste sem inset passaria mesmo com a HUD colada na borda. Entao o
## teste chama Hud._layout(l, t, r, b) com margens falsas e cobra o recuo.
##
## NOTA (mesma do menu/selftest.gd): em modo --script os autoloads so' registram
## DEPOIS deste arquivo compilar — por isso a Hud entra por load() em runtime.
extends SceneTree

const SL := 80.0  # notch a esquerda (paisagem)
const ST := 40.0
const SR := 90.0  # notch a direita
const SB := 60.0  # barra de gestos

var fails := 0


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	_check(root.get_node_or_null("Bus") != null, "autoload Bus presente")
	var balance: Node = root.get_node_or_null("Balance")
	_check(balance != null, "autoload Balance presente")
	if balance == null:
		quit(1)
		return
	var n_els: int = balance.ELEMENTS.size()

	var hud: CanvasLayer = (load("res://ui/Hud.gd") as GDScript).new()
	root.add_child(hud)
	_check(hud.joystick != null and hud.fire_btn != null and hud.dodge_btn != null
			and hud.carousel != null, "HUD monta joystick, Fogo, Esquiva e carrossel")

	# --- rotacao/split-screen: o layout NAO pode ser so' de _ready ---
	_check(root.size_changed.is_connected(hud._layout),
		"HUD refaz o layout no size_changed (rotacao/resize)")

	# --- margens de borda com notch injetado ---
	hud._layout(SL, ST, SR, SB)
	var folga := Dp.px(8.0)  # alem do notch, um respiro minimo de dedo/leitura

	_check(hud._bars_box.position.x >= SL + folga and hud._bars_box.position.y >= ST + folga,
		"barras vida/mana dentro da area util (%.0f, %.0f)"
		% [hud._bars_box.position.x, hud._bars_box.position.y])
	_check(hud._top_box.offset_right <= -(SR + folga) and hud._top_box.offset_top >= ST + folga,
		"timer/bots dentro da area util (dir %.0f, topo %.0f)"
		% [hud._top_box.offset_right, hud._top_box.offset_top])
	_check(hud.joystick.offset_left >= SL + folga,
		"joystick recua do notch esquerdo (%.0f >= %.0f)" % [hud.joystick.offset_left, SL + folga])

	for par: Array in [["Fogo", hud.fire_btn], ["Esquiva", hud.dodge_btn],
			["carrossel", hud.carousel]]:
		var c: Control = par[1]
		_check(c.offset_right <= -(SR + folga),
			"%s recua do notch direito (%.0f <= %.0f)" % [par[0], c.offset_right, -(SR + folga)])
	for par: Array in [["joystick", hud.joystick], ["Fogo", hud.fire_btn],
			["Esquiva", hud.dodge_btn]]:
		var c: Control = par[1]
		_check(c.offset_bottom <= -(SB + folga),
			"%s recua da barra de gestos (%.0f <= %.0f)"
			% [par[0], c.offset_bottom, -(SB + folga)])

	_check(hud.carousel.offset_bottom <= hud.fire_btn.offset_top,
		"carrossel fica ACIMA do botao de Fogo (sem sobreposicao)")
	# Fim de partida: aqui o recuo vem do Safe.apply() de verdade (0 em headless),
	# entao o que se cobra e' o respiro — cai para 0 se alguem tirar o apply.
	_check(hud._end_center.offset_left >= Dp.px(16.0) - 0.01
			and hud._end_center.offset_right <= -Dp.px(16.0) + 0.01,
		"tela de fim encostada dentro da area util")

	# --- refazer o layout nao pode derivar (Esquiva/carrossel saem do Fogo) ---
	var antes := Vector2(hud.dodge_btn.offset_left, hud.carousel.offset_top)
	hud._layout(SL, ST, SR, SB)
	_check(is_equal_approx(antes.x, hud.dodge_btn.offset_left)
			and is_equal_approx(antes.y, hud.carousel.offset_top),
		"layout repetido e' estavel (nao acumula deriva)")

	# --- alvos de toque: 48dp e' o minimo confortavel no Android ---
	var alvos := {
		"joystick": _lado(hud.joystick),
		"Fogo": _lado(hud.fire_btn),
		"Esquiva": _lado(hud.dodge_btn),
		"slot do carrossel": minf(
			(hud.carousel.offset_right - hud.carousel.offset_left) / float(n_els),
			hud.carousel.offset_bottom - hud.carousel.offset_top),
	}
	for nome: String in alvos:
		var d: float = float(alvos[nome]) / Dp.px(1.0)
		_check(d >= 48.0, "%s >= 48dp (%.0fdp)" % [nome, d])
	for b: Button in hud.end_screen.find_children("", "Button", true, false):
		var d := b.custom_minimum_size.y / Dp.px(1.0)
		_check(d >= 48.0, "botao '%s' >= 48dp (%.0fdp)" % [b.text, d])

	_teste_config_hud(hud)
	_teste_config_player()

	# --- base do viewport: com aspect "expand" o canvas CRESCE no eixo que
	# sobra, e Safe.gd/Dp.gd convertem dividindo por esta base. Base 16:9 num
	# celular 20:9 = margem horizontal ~20% curta, e o notch volta a comer a HUD.
	var bw := float(ProjectSettings.get_setting("display/window/size/viewport_width"))
	var bh := float(ProjectSettings.get_setting("display/window/size/viewport_height"))
	_check(bw / bh >= 1.9, "base do viewport e' de celular (%.2f:1 >= 1.90:1)" % (bw / bh))

	if fails == 0:
		print("SELFTEST OK")
	else:
		printerr("SELFTEST FALHOU: %d" % fails)
	quit(0 if fails == 0 else 1)


# --- as opcoes do menu tem EFEITO na HUD (bug do APK R21: menu so' estetico) --
## Nao basta a opcao existir na tela: aqui se cobra o EFEITO. Cada bloco muda a
## config, chama Config.aplicar (o mesmo caminho do menu real) e olha a HUD.
func _teste_config_hud(hud: CanvasLayer) -> void:
	var cfg: Dictionary = Config.PADRAO.duplicate(true)
	_check(hud.is_in_group("config_ouvintes"), "HUD escuta mudanca de configuracao")

	# contador de FPS
	_check(hud.fps_lbl != null and not hud.fps_lbl.visible, "contador de FPS nasce desligado")
	cfg.video.contador_fps = true
	Config.aplicar(cfg)
	_check(hud.fps_lbl.visible, "ligar 'Contador de FPS' MOSTRA o contador na HUD")
	cfg.video.contador_fps = false
	Config.aplicar(cfg)
	_check(not hud.fps_lbl.visible, "desligar 'Contador de FPS' esconde o contador")

	# qualidade: escala de render / MSAA / atlas de sombra, tudo em runtime
	var vp: Viewport = root
	cfg.video.qualidade = 0
	Config.aplicar(cfg)
	var baixa := Vector2(vp.scaling_3d_scale, float(vp.msaa_3d))
	cfg.video.qualidade = 2
	Config.aplicar(cfg)
	_check(baixa.x < vp.scaling_3d_scale and baixa.y < float(vp.msaa_3d),
		"'Qualidade' muda escala de render (%.2f -> %.2f) e MSAA (%d -> %d)"
		% [baixa.x, vp.scaling_3d_scale, int(baixa.y), int(vp.msaa_3d)])
	cfg.video.qualidade = 1
	Config.aplicar(cfg)
	_check(is_equal_approx(vp.scaling_3d_scale, 1.0)
			and int(vp.msaa_3d) == int(Viewport.MSAA_2X),
		"'Media' e' exatamente o que o project.godot ja' fazia (sem surpresa)")

	# numeros de dano: precisa de camera 3D para projetar o alvo na tela
	var cam := Camera3D.new()
	root.add_child(cam)
	cam.current = true
	var alvo := Node3D.new()  # NAO esta no grupo "player": conta como acerto nosso
	root.add_child(alvo)
	alvo.global_position = Vector3(0, 0, -6)  # a frente da camera
	var bus: Node = root.get_node_or_null("Bus")
	cfg.jogo.numeros_dano = false
	Config.aplicar(cfg)
	bus.damage_dealt.emit(alvo, 37, "fire")
	_check(_labels(hud).is_empty(), "'Numeros de dano' DESLIGADO nao cria numero")
	cfg.jogo.numeros_dano = true
	Config.aplicar(cfg)
	bus.damage_dealt.emit(alvo, 37, "fire")
	var nums := _labels(hud)
	_check(nums.size() == 1 and nums[0].text == "37",
		"'Numeros de dano' LIGADO faz o dano flutuar na tela (achou %d)" % nums.size())

	# daltonismo: o passe de tela so' EXISTE quando alguem precisa dele
	_check(hud._filtro == null, "daltonismo Nenhum nao cria passe de tela (custo zero)")
	cfg.jogo.daltonismo = 2
	Config.aplicar(cfg)
	_check(hud._filtro != null and hud._filtro.modo == 2,
		"escolher Deuteranopia liga o filtro de tela na hora")
	cfg.jogo.daltonismo = 0
	Config.aplicar(cfg)
	_check(hud._filtro.modo == 0 and not hud._filtro._rect.visible,
		"voltar para Nenhum desliga o filtro")

	cam.queue_free()
	alvo.queue_free()
	Config.aplicar(Config.PADRAO.duplicate(true))


## Numeros de dano sao Labels soltas na HUD (as outras vivem dentro de caixas).
func _labels(hud: CanvasLayer) -> Array[Label]:
	var out: Array[Label] = []
	for c in hud.get_children():
		if c is Label:
			out.append(c as Label)
	return out


# --- sensibilidade / mira / inverter Y chegam ate' a camera do Player --------
## Sem anotar o tipo `Player` de proposito: dar nome ao tipo pendura Player.gd
## (que fala com o autoload Bus) na COMPILACAO deste arquivo, e em modo --script
## os autoloads ainda nao existem — mesma armadilha ja' anotada no topo.
func _teste_config_player() -> void:
	var script: GDScript = load("res://gameplay/Player.gd")
	var p: Node = script.new()
	var cfg: Dictionary = Config.PADRAO.duplicate(true)
	p.aplicar_config(cfg)
	var padrao: float = p.look_sens
	_check(is_equal_approx(padrao, 0.008),
		"slider no padrao (3.0) = a mira que o jogo sempre teve (%.4f)" % padrao)
	cfg.controles.sensibilidade = 6.0
	p.aplicar_config(cfg)
	_check(is_equal_approx(p.look_sens, padrao * 2.0),
		"dobrar a Sensibilidade dobra o rad/px do Player (%.4f)" % p.look_sens)
	cfg.controles.sens_mira = 0.5
	cfg.controles.inverter_y = true
	p.aplicar_config(cfg)
	_check(is_equal_approx(p.sens_mira, 0.5), "'Sensibilidade ao mirar' chega no Player")
	_check(is_equal_approx(p.inverter_y, -1.0), "'Inverter eixo Y' vira o sinal do pitch")
	# valor de arquivo adulterado nao pode virar mira insana
	cfg.controles.sensibilidade = 9999.0
	p.aplicar_config(cfg)
	_check(p.look_sens <= float(script.SENS_BASE) * 10.0 + 0.0001,
		"sensibilidade fora da faixa e' presa no maximo do slider")
	p.free()


func _lado(c: Control) -> float:
	return minf(c.offset_right - c.offset_left, c.offset_bottom - c.offset_top)


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)
