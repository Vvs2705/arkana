## TELA DE CONFIGURACOES — spec do GDD par.12 (4 abas + Restaurar padrao por
## aba + persistencia em settings.json carregado no boot).
##
## DUAS METADES, de proposito:
##   1) ESTATICA (atual/carregar/salvar/aplicar/mesclar) — nao precisa de tela
##      nem de arvore. E o contrato que o resto do jogo usa:
##          if Config.atual().jogo.numeros_dano: ...
##      Chame Config.aplicar(Config.carregar()) UMA vez no boot (o Menu ja faz);
##      Config.atual() se vira sozinho se ninguem chamou (partida rodada direto).
##   2) A TELA — monta os controles a partir do mesmo dicionario.
##
## O QUE ESTA TELA APLICA SOZINHA (nao tem outro dono): volume dos buses, limite
## de FPS, VSync, tela cheia e o preset de QUALIDADE (escala de render, MSAA e
## atlas de sombra — tudo por API de runtime, nada exige reiniciar).
## O QUE TEM DONO: sensibilidade/inverter Y (Player), contador de FPS, numeros
## de dano e daltonismo (HUD). Esses recebem a config nova pelo grupo
## "config_ouvintes" — quem quiser participar faz:
##     add_to_group("config_ouvintes")
##     func aplicar_config(cfg: Dictionary) -> void: ...
## e recebe a chamada no boot e a CADA mexida no menu, na hora.
##
## R21: o que nao tem como ligar hoje NAO fica no menu. Opcao que grava no disco
## e nao muda nada e' pior que opcao nenhuma (veredito do Diretor no aparelho).
## Sairam da tela e do PADRAO: Idioma (so' existe PT-BR — core/Textos.gd e' const),
## Dicas de combo (a Sintonia do GDD par.9 nao existe ainda) e Esquema de toque
## (Simples pede assistencia de mira, que e' da raia gameplay). Cada uma virou
## uma NOTA na propria aba, e volta junto com o sistema que a torna real.
##
## Tudo em px de canvas via Dp/Safe; nenhum numero de dedo em px cru.
class_name Config
extends Control

signal voltar_pedido

const CAMINHO := "user://settings.json"

## Padrao de fabrica. Numero de opcao = INDICE na lista de Textos.CFG_*.
const PADRAO := {
	"video": {
		"tela_cheia": true,
		"qualidade": 1,       # Textos.CFG_QUALIDADES -> Média
		"fps_limite": 1,      # Textos.CFG_FPS_OPCOES -> 60
		"vsync": true,
		"contador_fps": false,
	},
	"audio": {
		"geral": 80.0, "musica": 70.0, "efeitos": 85.0, "interface": 70.0,
	},
	"controles": {
		"sensibilidade": 3.0,  # 0.1-10.0; 3.0 = a mira que o jogo sempre teve
		"sens_mira": 1.0,      # multiplicador enquanto arrasta a partir do Fogo
		"inverter_y": false,
	},
	"jogo": {
		"daltonismo": 0,      # Textos.CFG_DALTONISMOS -> Nenhum
		"numeros_dano": true,
	},
}

## PRESETS DE QUALIDADE — so' entra aqui o que EXISTE e vale a pena no renderer
## mobile do Godot 4.4 e liga em RUNTIME (sem project.godot, sem reiniciar):
## escala de render 3D, MSAA e o atlas da sombra direcional.
## NAO entra o que o mobile simplesmente nao tem: SSAO, SSIL, SSR, SDFGI e
## nevoa volumetrica. Oferecer botao morto foi exatamente o bug desta tela.
## Media = o que o project.godot ja' fazia (MSAA 2x, sombra 2048): quem nunca
## mexeu no menu nao ve diferenca nenhuma.
const QUALIDADE := [
	{"escala": 0.7, "msaa": Viewport.MSAA_DISABLED, "sombra": 1024},
	{"escala": 1.0, "msaa": Viewport.MSAA_2X, "sombra": 2048},
	{"escala": 1.0, "msaa": Viewport.MSAA_4X, "sombra": 4096},
]

## Volume (0-100) -> bus de audio. Music e Ui ainda nao existem em audio/Sfx.gd
## (so Master, Sfx e Ambient): o valor fica salvo e passa a valer sozinho no dia
## em que a raia AUDIO criar o bus. Nada a mudar aqui.
const BUSES := {"geral": "Master", "musica": "Music", "efeitos": "Sfx", "interface": "Ui"}

var _cfg: Dictionary = {}
var _abas: TabContainer

## A config EM VIGOR, viva enquanto o processo vive (static var sobrevive a
## troca de cena — e' o que faz o menu falar com a partida sem autoload).
static var _atual: Dictionary = {}


# ---------------------------------------------------------------- persistencia

## O que o jogo deve obedecer AGORA. Le do disco na primeira chamada, entao
## rodar a partida direto (sem passar pelo menu) tambem respeita settings.json.
static func atual() -> Dictionary:
	if _atual.is_empty():
		_atual = carregar()
	return _atual

static func carregar(caminho := CAMINHO) -> Dictionary:
	var padrao: Dictionary = PADRAO.duplicate(true)
	if not FileAccess.file_exists(caminho):
		return padrao
	var f := FileAccess.open(caminho, FileAccess.READ)
	if f == null:
		push_warning("Config: nao consegui ler %s — usando o padrao." % caminho)
		return padrao
	var dados: Variant = JSON.parse_string(f.get_as_text())
	if typeof(dados) != TYPE_DICTIONARY:
		push_warning("Config: %s corrompido — usando o padrao." % caminho)
		return padrao
	return mesclar(padrao, dados)


static func salvar(cfg: Dictionary, caminho := CAMINHO) -> bool:
	var f := FileAccess.open(caminho, FileAccess.WRITE)
	if f == null:
		push_warning("Config: nao consegui escrever %s." % caminho)
		return false
	f.store_string(JSON.stringify(cfg, "\t"))
	return true


## O arquivo em disco e ENTRADA NAO CONFIAVEL (editado a mao, corrompido, de
## uma versao mais velha). So passa o que existe no PADRAO e com o tipo certo;
## o resto cai no padrao em vez de derrubar o boot.
static func mesclar(padrao: Dictionary, salvo: Dictionary) -> Dictionary:
	for secao: String in padrao.keys():
		if typeof(salvo.get(secao)) != TYPE_DICTIONARY:
			continue
		var s: Dictionary = salvo[secao]
		for chave: String in padrao[secao].keys():
			if not s.has(chave):
				continue
			var pv: Variant = padrao[secao][chave]
			var v: Variant = s[chave]
			if pv is bool:
				if v is bool:
					padrao[secao][chave] = v
			elif pv is String:
				if v is String:
					padrao[secao][chave] = v
			elif v is int or v is float:  # JSON devolve todo numero como float
				padrao[secao][chave] = int(v) if pv is int else float(v)
	return padrao


## Poe a config de pe: aplica o que nao tem dono (audio/video) e ENTREGA o resto
## para quem tem (grupo "config_ouvintes"). Seguro no boot e em headless.
static func aplicar(cfg: Dictionary) -> void:
	_atual = cfg
	var audio: Dictionary = cfg.get("audio", {})
	for chave: String in BUSES.keys():
		var i := AudioServer.get_bus_index(BUSES[chave])
		if i == -1:
			continue  # bus ainda nao existe: valor fica guardado
		var v := float(audio.get(chave, 100.0))
		AudioServer.set_bus_mute(i, v <= 0.0)
		AudioServer.set_bus_volume_db(i, linear_to_db(maxf(v, 1.0) / 100.0))
	var video: Dictionary = cfg.get("video", {})
	Engine.max_fps = [30, 60, 120, 0][clampi(int(video.get("fps_limite", 1)), 0, 3)]

	# QUALIDADE: escala de render e MSAA sao do Viewport raiz (a HUD nao e'
	# afetada — canvas_items escala em separado); o atlas da sombra direcional e'
	# global no RenderingServer, entao da' pra mexer sem tocar em world/.
	var q: Dictionary = QUALIDADE[clampi(int(video.get("qualidade", 1)), 0, QUALIDADE.size() - 1)]
	RenderingServer.directional_shadow_atlas_set_size(int(q["sombra"]), true)
	var loop := Engine.get_main_loop()
	if loop is SceneTree:
		var vp: Viewport = (loop as SceneTree).root
		vp.scaling_3d_mode = Viewport.SCALING_3D_MODE_BILINEAR
		vp.scaling_3d_scale = float(q["escala"])
		vp.msaa_3d = q["msaa"]
		# Aplicacao IMEDIATA: HUD e Player recebem a config nova no mesmo frame.
		(loop as SceneTree).call_group("config_ouvintes", "aplicar_config", cfg)

	if DisplayServer.get_name() == "headless":
		return
	DisplayServer.window_set_vsync_mode(
		DisplayServer.VSYNC_ENABLED if bool(video.get("vsync", true))
		else DisplayServer.VSYNC_DISABLED)
	DisplayServer.window_set_mode(
		DisplayServer.WINDOW_MODE_FULLSCREEN if bool(video.get("tela_cheia", true))
		else DisplayServer.WINDOW_MODE_WINDOWED)


# ---------------------------------------------------------------- tela

func _ready() -> void:
	_cfg = carregar()
	# CanvasLayer NAO herda o `visible` do Control pai: sem isto o filtro de
	# daltonismo continuaria pintando o menu depois de sair de Configuracoes.
	visibility_changed.connect(_sincronizar_filtro)
	_montar()


func _sincronizar_filtro() -> void:
	var f := get_node_or_null("Filtro")
	if f != null:
		f.visible = is_visible_in_tree()


func _montar() -> void:
	for c in get_children():
		remove_child(c)  # sem remover, o remontar duplica nomes (@Abas@2)
		c.queue_free()

	var topo := Control.new()
	topo.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(topo)
	topo.set_anchors_preset(Control.PRESET_TOP_WIDE)
	topo.offset_bottom = Estilo.altura_alvo() + 8.0
	var titulo := Estilo.rotulo(Textos.CFG_TITULO, 30, Estilo.OURO)
	titulo.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	titulo.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	topo.add_child(titulo)
	titulo.set_anchors_preset(Control.PRESET_FULL_RECT)
	var voltar := Estilo.botao(Textos.VOLTAR, "BtnVoltarConfig", Estilo.ALVO_DP, 170.0, 22)
	voltar.pressed.connect(func() -> void:
		salvar(_cfg)
		voltar_pedido.emit())
	topo.add_child(voltar)

	_abas = TabContainer.new()
	_abas.name = "Abas"
	add_child(_abas)
	_abas.set_anchors_preset(Control.PRESET_FULL_RECT)
	_abas.offset_top = topo.offset_bottom + 4.0
	# a barra de abas tambem e alvo de dedo (GDD par.19.3)
	_abas.get_tab_bar().custom_minimum_size.y = Estilo.altura_alvo()

	_aba_video(_aba(Textos.CFG_ABA_VIDEO))
	_aba_audio(_aba(Textos.CFG_ABA_AUDIO))
	_aba_controles(_aba(Textos.CFG_ABA_CONTROLES))
	_aba_jogo(_aba(Textos.CFG_ABA_JOGO))

	# PREVIEW do daltonismo NA PROPRIA TELA: escolher o modo e nao ver nada
	# mudar e' o mesmo bug de novo. O filtro se inscreve sozinho no grupo.
	var filtro := FiltroDaltonismo.new()
	filtro.name = "Filtro"
	filtro.modo = int(_cfg["jogo"]["daltonismo"])
	add_child(filtro)
	_sincronizar_filtro()


## Cria a aba (scroll, porque tela de celular deitada e baixa) e ja pendura o
## "Restaurar padrao" DELA no rodape — a spec pede um por aba.
func _aba(nome: String) -> VBoxContainer:
	var scroll := ScrollContainer.new()
	scroll.name = nome
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_abas.add_child(scroll)
	var v := VBoxContainer.new()
	v.name = "Lista"
	v.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	v.add_theme_constant_override("separation", 6)
	scroll.add_child(v)
	return v


func _rodape(v: VBoxContainer, secao: String) -> void:
	var b := Estilo.botao(Textos.CFG_RESTAURAR, "BtnRestaurar_" + secao,
		Estilo.ALVO_DP, 260.0, 20)
	b.pressed.connect(func() -> void: _restaurar(secao))
	var caixa := HBoxContainer.new()
	caixa.alignment = BoxContainer.ALIGNMENT_END
	caixa.add_child(b)
	v.add_child(caixa)


func _restaurar(secao: String) -> void:
	_cfg[secao] = PADRAO[secao].duplicate(true)
	_gravar()
	var aba := _abas.current_tab
	_montar()  # remontar e mais barato (e menos bug) que sincronizar 5 widgets
	_abas.current_tab = aba


## Controle discreto (toque unico): aplica e grava na hora.
func _gravar() -> void:
	aplicar(_cfg)
	salvar(_cfg)


# ---------------------------------------------------------------- as 4 abas

func _aba_video(v: VBoxContainer) -> void:
	_opcao(v, "video", "tela_cheia", Textos.CFG_TELA_CHEIA)
	_opcao(v, "video", "qualidade", Textos.CFG_QUALIDADE, Textos.CFG_QUALIDADES)
	_opcao(v, "video", "fps_limite", Textos.CFG_FPS_LIMITE, Textos.CFG_FPS_OPCOES)
	_opcao(v, "video", "vsync", Textos.CFG_VSYNC)
	_opcao(v, "video", "contador_fps", Textos.CFG_CONTADOR_FPS)
	# ponytail: Resolucao (GDD par.12) e opcao de PC — no Android a janela e a
	# tela. Entra junto com o remapeamento de teclas quando o alvo for desktop.
	_rodape(v, "video")


func _aba_audio(v: VBoxContainer) -> void:
	_slider(v, "audio", "geral", Textos.CFG_VOL_GERAL, 0.0, 100.0, 1.0)
	_slider(v, "audio", "musica", Textos.CFG_VOL_MUSICA, 0.0, 100.0, 1.0)
	_slider(v, "audio", "efeitos", Textos.CFG_VOL_EFEITOS, 0.0, 100.0, 1.0)
	_slider(v, "audio", "interface", Textos.CFG_VOL_INTERFACE, 0.0, 100.0, 1.0)
	_rodape(v, "audio")


func _aba_controles(v: VBoxContainer) -> void:
	_slider(v, "controles", "sensibilidade", Textos.CFG_SENSIBILIDADE, 0.1, 10.0, 0.1)
	_slider(v, "controles", "sens_mira", Textos.CFG_SENS_MIRA, 0.1, 3.0, 0.05)
	_opcao(v, "controles", "inverter_y", Textos.CFG_INVERTER_Y)
	v.add_child(Estilo.paragrafo(Textos.CFG_ESQUEMA_NOTA, 16, Estilo.TEXTO_FOSCO))
	v.add_child(Estilo.paragrafo(Textos.CFG_TECLAS_NOTA, 16, Estilo.TEXTO_FOSCO))
	_rodape(v, "controles")


func _aba_jogo(v: VBoxContainer) -> void:
	_opcao(v, "jogo", "daltonismo", Textos.CFG_DALTONISMO, Textos.CFG_DALTONISMOS)
	_opcao(v, "jogo", "numeros_dano", Textos.CFG_NUMEROS_DANO)
	v.add_child(Estilo.paragrafo(Textos.CFG_IDIOMA_NOTA, 16, Estilo.TEXTO_FOSCO))
	v.add_child(Estilo.paragrafo(Textos.CFG_COMBO_NOTA, 16, Estilo.TEXTO_FOSCO))
	_rodape(v, "jogo")


# ---------------------------------------------------------------- widgets

## Linha padrao: rotulo a esquerda, controle a direita, altura >= 48dp.
func _linha(v: VBoxContainer, rotulo: String) -> HBoxContainer:
	var h := HBoxContainer.new()
	h.custom_minimum_size.y = Estilo.altura_alvo()
	h.add_theme_constant_override("separation", 12)
	var l := Estilo.rotulo(rotulo, 20, Estilo.TEXTO)
	l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	h.add_child(l)
	v.add_child(h)
	return h


## Sem lista de opcoes = liga/desliga (CheckButton). Com lista = OptionButton.
func _opcao(v: VBoxContainer, secao: String, chave: String, rotulo: String,
		opcoes: Array[String] = []) -> Control:
	var h := _linha(v, rotulo)
	if opcoes.is_empty():
		var cb := CheckButton.new()
		cb.name = "%s_%s" % [secao, chave]
		cb.focus_mode = Control.FOCUS_NONE
		cb.custom_minimum_size.y = Estilo.altura_alvo()
		cb.button_pressed = bool(_cfg[secao][chave])
		cb.toggled.connect(func(on: bool) -> void:
			_cfg[secao][chave] = on
			_gravar())
		h.add_child(cb)
		return cb
	var ob := OptionButton.new()
	ob.name = "%s_%s" % [secao, chave]
	ob.focus_mode = Control.FOCUS_NONE
	ob.custom_minimum_size = Vector2(220.0, Estilo.altura_alvo())
	ob.add_theme_font_size_override("font_size", 20)
	for o in opcoes:
		ob.add_item(o)
	# indice vindo do disco pode estar fora da lista (versao antiga do arquivo)
	ob.selected = clampi(int(_cfg[secao][chave]), 0, opcoes.size() - 1)
	ob.item_selected.connect(func(i: int) -> void:
		_cfg[secao][chave] = i
		_gravar())
	h.add_child(ob)
	return ob


func _slider(v: VBoxContainer, secao: String, chave: String, rotulo: String,
		minimo: float, maximo: float, passo: float) -> HSlider:
	var h := _linha(v, rotulo)
	var s := HSlider.new()
	s.name = "%s_%s" % [secao, chave]
	s.min_value = minimo
	s.max_value = maximo
	s.step = passo
	s.value = clampf(float(_cfg[secao][chave]), minimo, maximo)
	s.focus_mode = Control.FOCUS_NONE
	# a faixa inteira e alta: o dedo acerta o cursor sem mirar (GDD par.19.3)
	s.custom_minimum_size = Vector2(320.0, Estilo.altura_alvo())
	s.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	h.add_child(s)
	var casas := 0 if passo >= 1.0 else 2
	var num := Estilo.rotulo(String.num(s.value, casas), 20, Estilo.OURO)
	num.custom_minimum_size.x = 70.0
	num.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	num.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	h.add_child(num)
	s.value_changed.connect(func(val: float) -> void:
		_cfg[secao][chave] = val
		num.text = String.num(val, casas)
		aplicar(_cfg))  # so aplica: gravar a cada pixel do arrasto castiga a flash
	s.drag_ended.connect(func(mudou: bool) -> void:
		if mudou:
			salvar(_cfg))
	return s
