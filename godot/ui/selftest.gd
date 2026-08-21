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
var jogador: Node  # Player de verdade; ver a armadilha de tipo no topo


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
	# O sinal de dano completo carrega a FONTE do acerto: sem um player DE
	# VERDADE em campo nao da' para provar hitmarker, arco direcional nem
	# cooldown de habilidade. Sem anotar o tipo, pela armadilha do topo.
	jogador = (load("res://gameplay/Player.gd") as GDScript).new()
	root.add_child(jogador)
	hud.bind_player(jogador)
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
			["Tatica", hud.tatica_btn], ["Suprema", hud.suprema_btn],
			["arma equipada", hud.arma_lbl], ["carrossel", hud.carousel]]:
		var c: Control = par[1]
		_check(c.offset_right <= -(SR + folga),
			"%s recua do notch direito (%.0f <= %.0f)" % [par[0], c.offset_right, -(SR + folga)])
	for par: Array in [["joystick", hud.joystick], ["Fogo", hud.fire_btn],
			["Esquiva", hud.dodge_btn], ["Tatica", hud.tatica_btn],
			["Suprema", hud.suprema_btn], ["PEGAR", hud.pegar_btn]]:
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
	var antes := Vector2(hud.suprema_btn.offset_left, hud.carousel.offset_top)
	hud._layout(SL, ST, SR, SB)
	_check(is_equal_approx(antes.x, hud.suprema_btn.offset_left)
			and is_equal_approx(antes.y, hud.carousel.offset_top),
		"layout repetido e' estavel (nao acumula deriva)")

	# --- alvos de toque: 48dp e' o minimo confortavel no Android ---
	var alvos := {
		"joystick": _lado(hud.joystick),
		"Fogo": _lado(hud.fire_btn),
		"Esquiva": _lado(hud.dodge_btn),
		"Tatica": _lado(hud.tatica_btn),
		"Suprema": _lado(hud.suprema_btn),
		"PEGAR": _lado(hud.pegar_btn),
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

	_check(hud.suprema_btn.offset_right <= hud.tatica_btn.offset_left
			and hud.tatica_btn.offset_right <= hud.dodge_btn.offset_left
			and hud.dodge_btn.offset_right <= hud.fire_btn.offset_left,
		"Suprema/Tatica/Esquiva/Fogo em fileira, sem sobreposicao")

	_teste_config_hud(hud)
	_teste_config_player()
	_teste_sistemas(hud)

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
	cam.current = true   # depois do player: a camera do teste e' a que vale
	var alvo := Node3D.new()  # NAO esta no grupo "player": conta como acerto nosso
	root.add_child(alvo)
	alvo.global_position = Vector3(0, 0, -6)  # a frente da camera
	var bus: Node = root.get_node_or_null("Bus")
	cfg.jogo.numeros_dano = false
	Config.aplicar(cfg)
	bus.damage_applied.emit(alvo, 37.0, "fire", jogador, false)
	_check(_labels(hud).is_empty(), "'Numeros de dano' DESLIGADO nao cria numero")
	cfg.jogo.numeros_dano = true
	Config.aplicar(cfg)
	bus.damage_applied.emit(alvo, 37.0, "fire", jogador, false)
	var nums := _labels(hud)
	_check(nums.size() == 1 and nums[0].text == "37",
		"'Numeros de dano' LIGADO faz o dano flutuar na tela (achou %d)" % nums.size())

	# ACUMULACAO (docs/DANO.md §4.2): dois acertos no mesmo alvo dentro de
	# num_merge_s somam NO MESMO label, senao a manopla empilha escada ilegivel.
	bus.damage_applied.emit(alvo, 13.0, "fire", jogador, false)
	nums = _labels(hud)
	_check(nums.size() == 1 and nums[0].text == "50",
		"acerto seguido SOMA no mesmo numero (%d label(s), '%s')"
		% [nums.size(), "" if nums.is_empty() else nums[0].text])

	# O BUG QUE O SINAL NOVO CONSERTA: bot batendo em bot nao e' acerto MEU.
	# A heuristica velha ("alvo nao e' o player, logo fui eu") acendia aqui.
	var outro := Node3D.new()
	root.add_child(outro)
	hud._hit_flash = 0.0
	bus.damage_applied.emit(alvo, 20.0, "fire", outro, false)
	_check(is_zero_approx(hud._hit_flash) and _labels(hud).size() == 1,
		"bot-vs-bot NAO acende hitmarker nem numero (fim da heuristica ponytail)")
	outro.queue_free()

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


## Numeros de dano vivem numa casa propria (Hud._numeros): assim o rotulo da
## arma equipada, que tambem e' Label, nao entra na conta.
func _labels(hud: CanvasLayer) -> Array[Label]:
	var out: Array[Label] = []
	for c in hud._numeros.get_children():
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


# --- OS SEIS SISTEMAS NOVOS NA TELA (raia UI) --------------------------------
## O defeito que criou esta bateria: SEIS sistemas de gameplay emitiam sinal e
## NENHUM aparecia na tela. Entao aqui nao se cobra presenca de no' — cobra-se
## EFEITO: o elemento nasce quando o sinal chega, some quando deve, o alvo de
## toque tem 48dp, o desenho a mao respeita a area segura, e sinal que a HUD
## nao conhece NAO pode derrubar a partida.
##
## Sem anotar os tipos de gameplay (ArmaSlot) nem HudAviso: dar nome ao tipo o
## pendura na COMPILACAO deste arquivo e em modo --script os autoloads ainda
## nao existem — mesma armadilha ja' anotada no topo.
func _teste_sistemas(hud: CanvasLayer) -> void:
	var bus: Node = root.get_node_or_null("Bus")
	var balance: Node = root.get_node_or_null("Balance")
	var aviso: Control = hud.aviso
	hud._layout(SL, ST, SR, SB)  # Config.aplicar pode ter reposto o Safe real
	var folga := Dp.px(8.0)
	var cv: Vector2 = aviso._tela()

	# --- area segura do que e' desenhado A MAO (nao tem offset para inspecionar)
	for par: Array in [["faixa de aviso", aviso.rect_faixa()],
			["badges de estado", aviso.rect_badges()]]:
		var r: Rect2 = par[1]
		_check(r.position.x >= SL + folga and r.position.y >= ST + folga
				and r.end.x <= cv.x - SR,
			"%s dentro da area util (x %.0f, y %.0f, fim %.0f)"
			% [par[0], r.position.x, r.position.y, r.end.x])
	_check(aviso.rect_derrubado().end.y <= cv.y - SB,
		"painel de DERRUBADO acima da barra de gestos")

	# ---------------------------------------------------------- 1. HABILIDADES
	_check(not hud.tatica_btn.ativo and not hud.suprema_btn.ativo,
		"botoes de habilidade nascem APAGADOS (nenhum kit acoplado ainda)")
	bus.kit_bound.emit("01-pyra", true)
	_check(hud.tatica_btn.ativo and hud.tatica_btn.subtitulo == "Muralha de Brasas",
		"kit implementado ACENDE a tatica com o nome da ficha ('%s')"
		% hud.tatica_btn.subtitulo)
	_check(hud.suprema_btn.subtitulo == "Braço Livre",
		"suprema mostra o nome da ficha do mago ('%s')" % hud.suprema_btn.subtitulo)
	# 17 dos 20 magos ainda nao tem kit: o botao FICA, apagado — esconde-lo faria
	# o jogador achar que o mago nao tem habilidade, e ele tem (na ficha).
	bus.kit_bound.emit("06-olho-de-eter", false)
	_check(not hud.tatica_btn.ativo
			and hud.tatica_btn.subtitulo == Textos.HUD_KIT_EM_BREVE,
		"mago SEM kit implementado deixa o botao apagado e marcado '%s'"
		% Textos.HUD_KIT_EM_BREVE)
	bus.kit_bound.emit("mago-que-nao-existe", true)
	_check(hud.tatica_btn.subtitulo == "", "slug fora do elenco nao quebra o botao")
	bus.kit_cooldown.emit("suprema", 40.0, 60.0)
	_check(is_equal_approx(hud.suprema_btn.total_s, 60.0),
		"kit_cooldown (borda) entrega o total de segundos ao botao")

	# O botao apagado nao pode PEDIR habilidade: com 17 magos sem kit, um toque
	# que vaza chamaria request_tatica() num KitRunner sem implementacao.
	var b: Control = (load("res://ui/AcaoButton.gd") as GDScript).new()
	root.add_child(b)
	b.position = Vector2.ZERO
	b.size = Vector2(Dp.px(64.0), Dp.px(64.0))
	var pedidos := [0]
	b.tocado.connect(func() -> void: pedidos[0] += 1)
	b.ativo = false
	b._input(_toque(b.size / 2.0, true))
	b._input(_toque(b.size / 2.0, false))
	_check(pedidos[0] == 0, "botao APAGADO nao pede habilidade nenhuma")
	b.ativo = true
	b._input(_toque(b.size / 2.0, true))
	_check(pedidos[0] == 1, "botao aceso pede a habilidade UMA vez por toque")
	b.visible = false
	b._input(_toque(b.size / 2.0, false))
	b._input(_toque(b.size / 2.0, true))
	_check(pedidos[0] == 1, "botao ESCONDIDO (PEGAR sem loot) nao rouba o toque")
	b.queue_free()

	# GDD §4.2: tatica e suprema pagam COOLDOWN, NUNCA mana.
	var mana_antes: float = hud.mana_bar.value
	bus.kit_bound.emit("01-pyra", true)
	bus.kit_cooldown.emit("tatica", 8.0, 8.0)
	bus.kit_telegraph.emit("01-pyra", "suprema", 1.2, Vector3.ZERO)
	bus.kit_state.emit("braco_livre", true)
	_check(is_equal_approx(hud.mana_bar.value, mana_antes),
		"habilidade NAO mexe na barra de mana (GDD §4.2 — cooldown, nunca mana)")
	_check(aviso.faixa_ativa() == Textos.HUD_TELEGRAFO % "Braço Livre",
		"telegrafo da suprema aparece na faixa ('%s')" % aviso.faixa_ativa())
	_check(aviso._badges.has("braco_livre"), "kit_state ACENDE o badge de estado")
	bus.kit_state.emit("braco_livre", false)
	_check(not aviso._badges.has("braco_livre"), "kit_state desligado APAGA o badge")
	bus.kit_state.emit("um_estado_que_ninguem_desenhou", true)
	_check(aviso._badges.is_empty(),
		"estado desconhecido e' IGNORADO (nao vira badge, nao quebra a HUD)")
	aviso.limpar(aviso.P_TELEGRAFO)

	# ------------------------------------------------------ 2. ESCUDO EVOLUTIVO
	var linha_esc: Control = hud.escudo_bar.get_parent()
	_check(not linha_esc.visible,
		"barra de escudo nasce escondida (ainda nao existe escudo)")
	bus.shield_changed.emit(jogador, 60.0, 75.0, 2)
	_check(linha_esc.visible and hud._escudo_nivel == 2
			and is_equal_approx(hud._escudo, 60.0),
		"shield_changed MOSTRA a barra segmentada no nivel 2")
	var outro := Node3D.new()
	root.add_child(outro)
	bus.shield_changed.emit(outro, 10.0, 50.0, 1)
	_check(hud._escudo_nivel == 2 and is_equal_approx(hud._escudo, 60.0),
		"escudo de BOT nao mexe na barra do jogador")
	bus.shield_broken.emit(jogador)
	_check(hud._escudo_quebrou > 0.0, "shield_broken faz a barra de escudo piscar")

	# ----------------------------------------------------------------- 3. ZONA
	bus.zona_avisou.emit(1, Vector3(20, 0, 0), 60.0, 12.0)
	_check(aviso._bussola.has("zona"), "zona_avisou poe a seta do centro na bussola")
	hud._tick_contagens()
	_check(aviso.faixa_ativa() == Textos.ZONA_AVISO % 12,
		"contagem regressiva da tempestade na faixa ('%s')" % aviso.faixa_ativa())
	bus.zona_fechando.emit(1, Vector3(20, 0, 0), 60.0, 8.0)
	_check(aviso.faixa_ativa() == Textos.ZONA_FECHANDO,
		"zona_fechando troca a contagem pelo aviso de parede andando")
	bus.zona_estado.emit(false)
	_check(aviso.faixa_ativa() == Textos.ZONA_FORA,
		"fora da zona: 'VOLTE PARA A ZONA' ganha a faixa")
	bus.zona_dano.emit(6.0, 18.0)
	_check(aviso._vinheta > 0.0 and aviso.faixa_ativa().ends_with(Textos.ZONA_DPS % 18),
		"tique da tempestade acende vinheta e diz o dps ('%s')" % aviso.faixa_ativa())
	bus.zona_estado.emit(true)
	_check(aviso.faixa_ativa() != Textos.ZONA_FORA,
		"voltar para dentro APAGA o aviso de morte na tempestade")

	# -------------------------------------------------------- 4. BAU CELESTIAL
	aviso.limpar(aviso.P_ZONA)
	hud._contagens.clear()
	bus.bau_anunciado.emit(Vector3(30, 0, -10), 6.0)
	_check(aviso._bussola.has("bau"), "bau_anunciado marca o ponto de pouso na bussola")
	hud._tick_contagens()
	_check(aviso.faixa_ativa().begins_with(Textos.BAU_TITULO),
		"contagem da queda do bau na faixa ('%s')" % aviso.faixa_ativa())
	bus.bau_pousou.emit(Vector3(30, 0, -10))
	_check(aviso.faixa_ativa() == Textos.BAU_POUSOU, "bau_pousou: da' pra abrir agora")
	bus.bau_canalizando.emit(0.5)
	_check(aviso.faixa_ativa().begins_with(Textos.BAU_ABRINDO),
		"canalizar mostra o progresso ('%s')" % aviso.faixa_ativa())
	bus.bau_canalizando.emit(0.0)
	_check(aviso.faixa_ativa() == Textos.BAU_POUSOU,
		"cancelar a canalizacao (saiu do raio) volta para 'BAU NO CHAO'")
	bus.bau_aberto.emit(true, PackedStringArray(["fire", "water"]))
	_check(not aviso._bussola.has("bau")
			and aviso.faixa_ativa() == Textos.BAU_MANOPLA % ["FOGO", "ÁGUA"],
		"bau aberto POR MIM anuncia o par da manopla e apaga o marcador ('%s')"
		% aviso.faixa_ativa())
	bus.bau_aberto.emit(false, PackedStringArray())
	_check(aviso.faixa_ativa() == Textos.BAU_PERDIDO,
		"bau aberto por outro (sem elementos no sinal) nao quebra: manopla perdida")

	# ---------------------------------------------------------- 5. LOOT E ARMA
	_check(not hud.pegar_btn.visible, "botao PEGAR nasce escondido")
	bus.loot_prompt.emit("Cajado", "raro", true)
	_check(hud.pegar_btn.visible and hud.pegar_btn.subtitulo == "Cajado",
		"loot ao alcance MOSTRA o PEGAR com o nome do item")
	bus.loot_prompt.emit("Cajado", "raro", false)
	_check(not hud.pegar_btn.visible, "sair do raio ESCONDE o botao PEGAR")
	bus.loot_prompt.emit("Coisa", "raridade_que_nao_existe", true)
	_check(hud.pegar_btn.visible, "raridade desconhecida nao quebra o prompt de loot")
	_check(hud.arma_lbl.text == "", "rotulo da arma nasce vazio")
	var slot: Node = (load("res://gameplay/ArmaSlot.gd") as GDScript).new()
	slot.name = "ArmaSlot"
	jogador.add_child(slot)
	slot.equipar("cajado")
	_check(hud.arma_lbl.text.begins_with("Cajado"),
		"equipar publica a arma do jogador na HUD ('%s')" % hud.arma_lbl.text)
	var minha: String = hud.arma_lbl.text
	bus.weapon_equipped.emit("manopla", "Manopla", "lendaria",
			PackedStringArray(["fire", "earth"]))
	_check(hud.arma_lbl.text == minha, "arma de BOT nao sequestra o rotulo do jogador")

	# -------------------------- prioridade: quem salva a vida GANHA a faixa ----
	bus.bau_pousou.emit(Vector3(5, 0, 5))
	bus.zona_fechando.emit(2, Vector3.ZERO, 40.0, 9.0)
	bus.kit_telegraph.emit("01-pyra", "suprema", 2.0, Vector3.ZERO)
	bus.zona_estado.emit(false)
	_check(aviso.faixa_ativa() == Textos.ZONA_FORA,
		"com bau + parede + suprema juntos, a faixa mostra SO' a ameaca de morte")

	# ----------------------------------------------------------- 6. DERRUBADO
	bus.entity_derrubada.emit(jogador, outro)
	_check(aviso._caido, "entity_derrubada do jogador abre o painel de DERRUBADO")
	_check(aviso.faixa_ativa() == "",
		"no chao a faixa CALA: o painel de DERRUBADO e' a unica leitura")
	bus.derrubado_progresso.emit(jogador, 0.5, 0.0)
	_check(is_equal_approx(aviso._esvaecimento, 0.5),
		"barra de esvaecimento segue o progresso (x ESVAECER_S = os segundos)")
	bus.entity_reerguida.emit(jogador, outro)
	_check(not aviso._caido and aviso.faixa_ativa() != "",
		"reerguido FECHA o painel e devolve a faixa")
	bus.entity_derrubada.emit(outro, jogador)
	_check(not aviso._caido, "aliado derrubado nao abre o painel do JOGADOR")
	bus.derrubado_progresso.emit(outro, 0.9, 0.4)
	_check(aviso._resgatando and is_equal_approx(aviso._reerguer, 0.4),
		"resgatar aliado desenha o anel de reerguer")
	bus.entity_reerguida.emit(outro, jogador)
	_check(not aviso._resgatando, "aliado de pe' apaga o anel de resgate")

	# ------------------------------------------- 7. DANO: DE ONDE ELE VEIO ----
	aviso.zerar()
	outro.global_position = Vector3(6, 0, 0)
	bus.damage_applied.emit(jogador, 12.0, "lightning", outro, false)
	_check(aviso._arcos.size() == 1 and aviso._vinheta > 0.0,
		"levar dano acende vinheta + arco direcional de quem atirou")
	var v1: float = aviso._vinheta
	bus.damage_applied.emit(jogador, 40.0, "fire", outro, false)
	_check(is_equal_approx(aviso._vinheta, v1),
		"vinheta tem intervalo minimo (docs/DANO.md §4.3): tiques colados nao repicam")
	aviso.zerar()
	bus.damage_applied.emit(jogador, 3.0, "terrain", null, false)
	_check(aviso._arcos.is_empty() and aviso._vinheta > 0.0,
		"terreno/DoT tem vinheta mas NAO tem direcao — sem fonte, sem arco (§4.4)")
	for i in 6:
		bus.damage_applied.emit(jogador, 5.0, "fire", outro, false)
	_check(aviso._arcos.size() <= int(balance.FEEDBACK.arc_max),
		"arcos direcionais tem teto de %d — o 4o substitui o mais antigo"
		% int(balance.FEEDBACK.arc_max))

	outro.queue_free()


func _toque(pos: Vector2, pressed: bool) -> InputEventScreenTouch:
	var e := InputEventScreenTouch.new()
	e.position = pos
	e.pressed = pressed
	return e


func _lado(c: Control) -> float:
	return minf(c.offset_right - c.offset_left, c.offset_bottom - c.offset_top)


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)
