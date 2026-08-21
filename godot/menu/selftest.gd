## Selftest headless da raia APRESENTACAO. Rodar:
##   Godot --headless --path godot --script res://menu/selftest.gd
##
## Prova, sem abrir janela:
##   1. a cena instancia e os 4 botoes do menu tem alvo >= 48dp;
##   2. NENHUMA tela encosta na borda crua — todas passam por Safe (o bug do
##      APK R19: conteudo sob o notch e sob a barra de gestos);
##   3. a ficha dos 20 magos cumpre o contrato de menu/Elenco.gd (campo faltando
##      quebra aqui, nao no aparelho do Diretor);
##   4. a selecao lista os 20, marca EM BREVE so nos 11-20, abre e fecha o perfil;
##   5. Configuracoes tem as 4 abas do GDD par.12, um "Restaurar padrao" por aba,
##      e settings.json sobrevive a ida e volta ao disco (inclusive corrompido);
##   6. JOGAR emite Bus.game_start_requested SEM trocar de cena.
##
## NOTA (mesma do gameplay/selftest.gd): em modo --script os autoloads so'
## registram DEPOIS deste arquivo compilar — Bus chega via get_node no 1o frame.
extends SceneTree

const CFG_TESTE := "user://selftest_settings.json"

var fails := 0
var _sinal := 0


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	var ps: PackedScene = load("res://menu/Menu.tscn")
	if ps == null:
		printerr("FALHOU — nao carregou res://menu/Menu.tscn")
		quit(1)
		return
	var menu: Control = ps.instantiate()
	root.add_child(menu)
	_check(menu is Control, "cena instancia com raiz Control")

	_teste_botoes(menu)
	_teste_area_segura(menu)
	_teste_ficha_elenco()
	_teste_selecao(menu)
	_teste_config(menu)
	_teste_persistencia()
	_teste_bus(menu)

	if fails == 0:
		print("SELFTEST OK")
	else:
		printerr("SELFTEST FALHOU: %d" % fails)
	quit(0 if fails == 0 else 1)


# --- 4 botoes de menu, alvo >= 48dp (mesma conversao do jogo: ui/Dp.gd) ------
func _teste_botoes(menu: Control) -> void:
	var min48 := maxf(Dp.px(Estilo.ALVO_DP), Estilo.ALVO_DP) - 0.5
	for nome in ["BtnJogar", "BtnPersonagens", "BtnConfig", "BtnSair"]:
		var b: Button = menu.find_child(nome, true, false)
		_check(b != null, "botao %s existe" % nome)
		if b != null:
			_check(b.custom_minimum_size.y >= min48,
				"%s >= 48dp (%.0fpx >= %.0fpx)" % [nome, b.custom_minimum_size.y, min48])


# --- area segura: nenhuma tela de conteudo encosta na borda crua ------------
func _teste_area_segura(menu: Control) -> void:
	var respiro := Dp.px(12.0)  # Menu.RESPIRO_DP (Menu.gd nao tem class_name — ver la)
	for nome in ["Titulo", "MenuPrincipal", "Selecao", "Config"]:
		var t: Control = menu.find_child(nome, false, false)
		_check(t != null, "tela %s existe" % nome)
		if t == null:
			continue
		_check(is_equal_approx(t.offset_left, Safe.left() + respiro)
			and is_equal_approx(t.offset_top, Safe.top() + respiro)
			and is_equal_approx(t.offset_right, -Safe.right() - respiro)
			and is_equal_approx(t.offset_bottom, -Safe.bottom() - respiro),
			"%s encostada por Safe + respiro (nao em px cru)" % nome)
		_check(t.offset_left > 0.0 and t.offset_right < 0.0,
			"%s nao cola na borda da tela" % nome)


# --- a ficha dos 20 magos cumpre o contrato de Elenco.gd --------------------
func _teste_ficha_elenco() -> void:
	_check(Elenco.MAGOS.size() == 20, "ficha lista 20 magos (achou %d)" % Elenco.MAGOS.size())
	var ids := {}
	var completos := true
	for d: Dictionary in Elenco.MAGOS:
		ids[d.get("id", 0)] = true
		for c: String in Elenco.CAMPOS:
			if not d.has(c):
				completos = false
				printerr("    mago %s sem o campo '%s'" % [d.get("nome", "?"), c])
	_check(completos, "todo mago tem todos os campos do contrato")
	_check(ids.size() == 20 and ids.has(1) and ids.has(20), "ids 1..20 unicos")
	# placeholder no lugar de campo vazio — a tela nunca mostra vazio
	_check(Elenco.campo({"historia_breve": ""}, "historia_breve") == Textos.PERFIL_PENDENTE,
		"campo vazio vira Textos.PERFIL_PENDENTE")


# --- selecao: 20 cards, EM BREVE so nas temporadas, perfil abre e fecha -----
func _teste_selecao(menu: Control) -> void:
	var cards := get_nodes_in_group("elenco_card")
	_check(cards.size() == 20, "selecao lista 20 cards (achou %d)" % cards.size())
	var c01: Node = menu.find_child("Card01", true, false)
	var c11: Node = menu.find_child("Card11", true, false)
	var c20: Node = menu.find_child("Card20", true, false)
	_check(c01 != null and c01.find_child("EmBreve", true, false) == null,
		"card 01 (lancamento) SEM selo EM BREVE")
	_check(c11 != null and c11.find_child("EmBreve", true, false) != null,
		"card 11 (temporada) COM selo EM BREVE")
	_check(c20 != null and c20.find_child("EmBreve", true, false) != null,
		"card 20 (temporada) COM selo EM BREVE")

	var perfil: Control = menu.find_child("Perfil", true, false)
	_check(perfil != null and not perfil.visible, "perfil comeca fechado")
	if c01 == null or perfil == null:
		return
	var toque: Button = c01.find_child("Toque", true, false)
	_check(toque != null, "card e alvo de toque inteiro")
	if toque == null:
		return
	toque.pressed.emit()
	_check(perfil.visible, "tocar no card abre o perfil")
	var nome: Label = perfil.find_child("PerfilNome", true, false)
	_check(nome != null and nome.text.begins_with("Pyra"),
		"perfil mostra o mago tocado (%s)" % (nome.text if nome != null else "sem nome"))
	var fechar: Button = perfil.find_child("BtnFecharPerfil", true, false)
	_check(fechar != null, "perfil tem FECHAR")
	if fechar != null:
		fechar.pressed.emit()
		_check(not perfil.visible, "FECHAR volta para a grade")


# --- configuracoes: as 4 abas do GDD par.12 + restaurar padrao por aba ------
func _teste_config(menu: Control) -> void:
	var abas: TabContainer = menu.find_child("Abas", true, false)
	_check(abas != null, "Configuracoes tem TabContainer")
	if abas == null:
		return
	# Daqui pra baixo o teste MEXE em controles de verdade, e controle mexido
	# grava settings.json na hora. O arquivo do dev sai da frente antes do
	# primeiro toque e volta no fim — teste nao reconfigura a maquina de ninguem.
	var backup := ""
	if FileAccess.file_exists(Config.CAMINHO):
		backup = FileAccess.open(Config.CAMINHO, FileAccess.READ).get_as_text()

	_check(abas.get_tab_count() == 4, "4 abas (achou %d)" % abas.get_tab_count())
	for secao in ["video", "audio", "controles", "jogo"]:
		_check(menu.find_child("BtnRestaurar_" + secao, true, false) != null,
			"aba %s tem RESTAURAR PADRAO" % secao)
	_check(menu.find_child("audio_geral", true, false) is HSlider, "slider de volume geral")
	_check(menu.find_child("jogo_daltonismo", true, false) is OptionButton, "modo daltonismo")

	# R21 — A LEI DESTA TELA: nao existe controle sem consumidor. Se alguem
	# devolver um seletor bonito que so' grava no disco, cai aqui e nao no
	# aparelho do Diretor. "Ligada" = tem quem leia a chave fora do Config.
	for orfa in ["jogo_idioma", "jogo_dicas_combo", "controles_esquema"]:
		_check(menu.find_child(orfa, true, false) == null,
			"'%s' fora da tela (opcao sem efeito nao volta)" % orfa)
	for chave in ["idioma", "dicas_combo", "esquema"]:
		var secao: String = "controles" if chave == "esquema" else "jogo"
		_check(not Config.PADRAO[secao].has(chave),
			"'%s' tambem saiu do settings.json (nada de chave fantasma)" % chave)
	# ...e o jogador ficou sabendo por que a opcao sumiu, na propria aba.
	var notas := 0
	for l: Label in menu.find_children("", "Label", true, false):
		if l.text in [Textos.CFG_ESQUEMA_NOTA, Textos.CFG_IDIOMA_NOTA, Textos.CFG_COMBO_NOTA]:
			notas += 1
	_check(notas == 3, "cada opcao removida virou NOTA na aba (achou %d de 3)" % notas)

	# Preview do daltonismo na propria tela de Configuracoes.
	var filtro: CanvasLayer = menu.find_child("Filtro", true, false)
	_check(filtro is FiltroDaltonismo, "Configuracoes tem o passe de daltonismo")
	if filtro != null:
		_check(filtro.is_in_group("config_ouvintes"), "o passe escuta a mudanca do menu")
		var ob: OptionButton = menu.find_child("jogo_daltonismo", true, false)
		ob.item_selected.emit(3)
		_check(filtro.modo == 3, "escolher Tritanopia muda a tela NA HORA (sem reiniciar)")
		ob.item_selected.emit(0)
		_check(filtro.modo == 0, "voltar para Nenhum desliga o passe")
		# CanvasLayer nao herda visible do Control pai — sem isto o filtro
		# pintaria o menu inteiro depois de sair de Configuracoes.
		_check(not filtro.visible, "com Configuracoes fechada, o passe nao pinta o menu")

	# Aplicacao IMEDIATA das opcoes de video: mexeu, valeu (GDD par.12).
	var cfg: Dictionary = Config.PADRAO.duplicate(true)
	cfg.video.qualidade = 0
	cfg.video.fps_limite = 0
	Config.aplicar(cfg)
	_check(Engine.max_fps == 30 and is_equal_approx(root.scaling_3d_scale, 0.7),
		"limite de FPS e qualidade valem no ato (max_fps=%d, escala=%.2f)"
		% [Engine.max_fps, root.scaling_3d_scale])
	Config.aplicar(Config.PADRAO.duplicate(true))

	# "Restaurar padrao" remonta a tela inteira: e o caminho mais fragil daqui
	# (nomes duplicados, aba perdida).
	var s: HSlider = menu.find_child("audio_geral", true, false)
	abas.current_tab = 1
	s.value = 7.0
	var botao: Button = menu.find_child("BtnRestaurar_audio", true, false)
	botao.pressed.emit()
	var s2: HSlider = menu.find_child("audio_geral", true, false)
	_check(s2 != null and is_equal_approx(s2.value, float(Config.PADRAO.audio.geral)),
		"RESTAURAR PADRAO devolve o valor de fabrica")
	var abas2: TabContainer = menu.find_child("Abas", true, false)
	_check(abas2 != null and abas2.get_tab_count() == 4 and abas2.current_tab == 1,
		"remontar nao duplica no' nem perde a aba aberta")
	if backup != "":
		FileAccess.open(Config.CAMINHO, FileAccess.WRITE).store_string(backup)
	else:
		DirAccess.remove_absolute(Config.CAMINHO)


# --- settings.json: ida e volta ao disco, e resistente a lixo ---------------
func _teste_persistencia() -> void:
	var cfg: Dictionary = Config.PADRAO.duplicate(true)
	cfg.audio.geral = 42.0
	cfg.jogo.numeros_dano = false
	_check(Config.salvar(cfg, CFG_TESTE), "settings.json grava")
	var lido := Config.carregar(CFG_TESTE)
	_check(is_equal_approx(float(lido.audio.geral), 42.0)
		and lido.jogo.numeros_dano == false, "settings.json volta igual do disco")
	DirAccess.remove_absolute(CFG_TESTE)
	_check(is_equal_approx(float(Config.carregar(CFG_TESTE).audio.geral),
		float(Config.PADRAO.audio.geral)), "sem arquivo, cai no padrao")
	# arquivo editado a mao / de versao antiga nao pode derrubar o boot
	var sujo := Config.mesclar(Config.PADRAO.duplicate(true),
		{"audio": {"geral": "alto demais"}, "video": 7, "inexistente": {"x": 1}})
	_check(is_equal_approx(float(sujo.audio.geral), float(Config.PADRAO.audio.geral))
		and sujo.video.vsync == Config.PADRAO.video.vsync,
		"valor de tipo errado no arquivo cai no padrao")


# --- JOGAR emite o sinal do contrato (core/Bus.gd) --------------------------
func _teste_bus(menu: Control) -> void:
	var bus: Node = root.get_node_or_null("Bus")
	_check(bus != null, "autoload Bus presente")
	if bus == null:
		return
	menu.scene_switch_enabled = false  # prova o sinal sem trocar de cena
	bus.game_start_requested.connect(func() -> void: _sinal += 1)
	var jogar: Button = menu.find_child("BtnJogar", true, false)
	if jogar != null:
		jogar.pressed.emit()
	_check(_sinal == 1, "JOGAR emite Bus.game_start_requested (1x)")


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)
