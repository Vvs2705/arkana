## HUD DE PARTIDA — FORA da coluna central (territorio da mira, licao do
## projeto). OBSERVA o jogo pelo Bus (regra do core) e encaminha toque para o
## player. Nada aqui decide jogo.
##
## MAPA DA TELA (paisagem):
##   sup-esq   vida / mana / ESCUDO evolutivo, e os badges de estado do kit
##   sup-dir   timer / bots / FPS
##   faixa     UMA linha de aviso no alto, centrada (zona, telegrafo, bau)
##   anel      arcos de dano direcional e setas de bussola, LONGE do reticulo
##   inf-esq   joystick
##   inf-dir   Fogo, Esquiva, TATICA, SUPREMA e o carrossel de elementos
##   inf-meio  PEGAR (so' aparece com loot ao alcance)
##
## A LEI DA PRIORIDADE VISUAL (6 bots + zona + bau + status = sopa): quem salva
## a vida do jogador ganha a tela. A faixa tem UMA linha e cinco candidatos —
## ver HudAviso.P_*; o painel de DERRUBADO cala a faixa inteira; badges tem teto
## de 4. O que perde nao encolhe: SOME.
##
## ⚠️ TATICA e SUPREMA pagam COOLDOWN, nunca mana (GDD §4.2). A barra de mana
## NAO pode reagir a elas — quem cobra e' o KitRunner, e a HUD so' le o frac.
class_name Hud
extends CanvasLayer

signal restart_pressed

## Segundos que a confirmacao da arma equipada fica na tela antes de apagar.
## 2.5s le' sem atrapalhar; SUBIR devolve a poluicao que o Diretor mandou
## tirar, DESCER vira pisca-pisca ilegivel no meio de uma briga.
const ARMA_LBL_S := 2.5

var player: Player
var joystick: VirtualJoystick
var fire_btn: FireButton
var dodge_btn: AcaoButton
var tatica_btn: AcaoButton
var suprema_btn: AcaoButton
var pegar_btn: AcaoButton
var carousel: ElementCarousel
var pausa_btn: AcaoButton
var _pausa_overlay: Control
var aviso: HudAviso
var hp_bar: ProgressBar
var mana_bar: ProgressBar
var escudo_bar: Control
var arma_lbl: Label
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
## Escudo evolutivo (GDD §5) — so' desenha depois que existe.
var _escudo := 0.0
var _escudo_max := 0.0
var _escudo_nivel := 0
var _escudo_quebrou := 0.0
## Numeros de dano ACUMULADOS por alvo (docs/DANO.md §4.2): id -> {lbl,total,ate}
var _nums := {}
## Casa dos numeros flutuantes. Existe para eles NAO virarem filhos soltos da
## HUD: com Label solta, todo codigo que varre os filhos (o selftest inclusive)
## passa a confundir numero de dano com rotulo de arma.
var _numeros: Control
## Contagens regressivas vivas: prio da faixa -> {ate, fmt, cor, s}
var _contagens := {}


func _ready() -> void:
	layer = 10
	_build_screen_grade()
	_build_bars()
	_build_top_right()
	_build_reticle()
	_build_aviso()
	_numeros = Control.new()
	_numeros.anchor_right = 1.0
	_numeros.anchor_bottom = 1.0
	_numeros.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_numeros)
	_build_sticks()
	_build_end()
	_layout()
	# Rotacao / split-screen / dobravel mudam a area segura E a escala do dp,
	# entao a HUD nao pode ser montada uma vez so' em _ready.
	get_tree().root.size_changed.connect(_layout)
	Bus.health_changed.connect(_on_hp)
	Bus.mana_changed.connect(_on_mana)
	## O SINAL DE DANO COMPLETO (docs/DANO.md §C1). `damage_dealt` NAO e' mais
	## ouvido aqui: ele nao carrega quem atirou, e sem isso o hitmarker era uma
	## heuristica que mentia em bot-vs-bot e o arco direcional era impossivel.
	Bus.damage_applied.connect(_on_damage)
	Bus.shield_changed.connect(_on_escudo)
	Bus.shield_broken.connect(_on_escudo_quebrou)
	Bus.kit_bound.connect(_on_kit_bound)
	Bus.kit_cooldown.connect(_on_kit_cooldown)
	Bus.kit_telegraph.connect(_on_kit_telegraph)
	Bus.kit_state.connect(func(nome: String, ligado: bool) -> void: aviso.estado(nome, ligado))
	Bus.zona_avisou.connect(_on_zona_avisou)
	Bus.zona_fechando.connect(_on_zona_fechando)
	Bus.zona_dano.connect(_on_zona_dano)
	Bus.zona_estado.connect(_on_zona_estado)
	Bus.bau_anunciado.connect(_on_bau_anunciado)
	Bus.bau_pousou.connect(_on_bau_pousou)
	Bus.bau_canalizando.connect(_on_bau_canalizando)
	Bus.bau_aberto.connect(_on_bau_aberto)
	Bus.loot_prompt.connect(_on_loot_prompt)
	Bus.weapon_equipped.connect(_on_arma)
	Bus.entity_derrubada.connect(_on_derrubada)
	Bus.entity_reerguida.connect(_on_reerguida)
	Bus.derrubado_progresso.connect(_on_derrubado_progresso)
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
	## MAOS NUAS NAO TEM ELEMENTO (ordem do Diretor, 26/08): o carrossel dos 5
	## so' faria sentido se o jogador pudesse escolher — e nao pode, o elemento
	## mora na LUVA. Antes ele nascia visivel e o jogador escolhia um elemento
	## que o disparo ignorava. O botao de ataque nasce APAGADO; equipar acende.
	carousel.visible = false
	fire_btn.desarmado = true
	player = p
	hp_bar.max_value = float(Balance.PLAYER.hp)
	hp_bar.value = p.hp
	mana_bar.max_value = float(Balance.PLAYER.mana_max)
	mana_bar.value = p.mana
	dodge_btn.cd_frac = p.dodge_cd_frac  # Callable morre com o player velho; is_valid() cobre
	# Cooldown de habilidade e' CONTINUO e o Bus so' emite na BORDA: o valor por
	# frame vem daqui (KitRunner.frac_*), como manda o contrato do sinal.
	tatica_btn.cd_frac = func() -> float:
		var k := KitRunner.de(player)
		return k.frac_tatica() if k != null else 0.0
	suprema_btn.cd_frac = func() -> float:
		var k := KitRunner.de(player)
		return k.frac_suprema() if k != null else 0.0
	# Partida nova nao herda aviso, badge nem escudo da partida velha.
	aviso.player = p
	aviso.zerar()
	_contagens.clear()
	_escudo = 0.0
	_escudo_max = 0.0
	_escudo_nivel = 0
	escudo_bar.get_parent().visible = false
	pegar_btn.visible = false
	arma_lbl.text = ""
	arma_lbl.modulate = Color.WHITE  # partida nova nao herda o fade da anterior


func update_match(time_left: float, bots: int) -> void:
	var t := maxi(int(ceilf(time_left)), 0)
	@warning_ignore("integer_division")
	timer_lbl.text = "%d:%02d" % [t / 60, t % 60]
	bots_lbl.text = Textos.HUD_BOTS % bots


## No TREINO nao existe relogio nem contagem que importe — mostrar "3:00" e
## "BOTS 0" era mentira de partida (pendencia do video do Diretor, 26/08).
## O canto diz o que a cena e': TREINO.
func update_treino() -> void:
	timer_lbl.text = Textos.MENU_TREINO
	bots_lbl.text = ""


func show_end(victory: bool) -> void:
	end_lbl.text = Textos.HUD_VITORIA if victory else Textos.HUD_DERROTA
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
	if _escudo_quebrou > 0.0:
		_escudo_quebrou = maxf(_escudo_quebrou - delta, 0.0)
		escudo_bar.queue_redraw()
	if not _contagens.is_empty():
		_tick_contagens()
	if fps_lbl.visible:
		fps_lbl.text = Textos.HUD_FPS % Engine.get_frames_per_second()


# ---------- dano (docs/DANO.md §4) ----------

## O acerto COMPLETO. Tres leituras diferentes saem do mesmo sinal:
##   fui EU quem acertou  -> hitmarker + numero flutuante
##   acertaram em MIM     -> vinheta + arco direcional de quem atirou
##   bot em bot           -> nada (a heuristica velha acendia o hitmarker aqui)
func _on_damage(target: Node, amount: float, element: String, source: Node,
		on_shield: bool) -> void:
	if not is_instance_valid(player):
		return
	if source == player and target != player:
		_hit_flash = 0.16
		_reticle.queue_redraw()
		if _numeros_dano:
			_numero_dano(target, amount, element, on_shield)
	if target == player:
		aviso.pulsar(Projectile.tint(element), clampf(amount / 25.0, 0.35, 1.0))
		# Terreno/DoT nao tem direcao (docs/DANO.md §4.4): sem fonte, sem arco.
		if source != null and is_instance_valid(source) and source is Node3D:
			aviso.arco((source as Node3D).global_position, Projectile.tint(element))


## Numero de dano flutuante (GDD par.12): nasce na cabeca do alvo projetada na
## tela, sobe e some. Cor do elemento em VIDA, branco-azulado em ESCUDO, e o
## TAMANHO sai do dano (docs/DANO.md §4.2 — 30 fica maior que 8).
##
## ACUMULACAO: dois acertos no mesmo alvo dentro de `num_merge_s` somam NO MESMO
## label. Sem isso a manopla (0,21s de cadencia) empilha 5 numeros por segundo e
## a tela vira escada ilegivel — e' a queixa do Diretor.
func _numero_dano(target: Node, amount: float, element: String, on_shield: bool) -> void:
	var cam := get_viewport().get_camera_3d()
	if cam == null or not (target is Node3D):
		return
	var f: Dictionary = Balance.FEEDBACK
	var agora := float(Time.get_ticks_msec()) / 1000.0
	var id := target.get_instance_id()
	var e: Dictionary = _nums.get(id, {})
	if not e.is_empty() and is_instance_valid(e.lbl) and agora < float(e.ate):
		e.total = float(e.total) + amount
		e.ate = agora + float(f.num_merge_s)
		_pintar_numero(e.lbl, float(e.total), element, on_shield)
		return
	var mundo: Vector3 = (target as Node3D).global_position + Vector3(0, 1.7, 0)
	if cam.is_position_behind(mundo):
		return  # alvo atras da camera projetaria o numero no lugar errado
	var lbl := Label.new()
	_pintar_numero(lbl, amount, element, on_shield)
	lbl.position = cam.unproject_position(mundo)
	_numeros.add_child(lbl)
	_nums[id] = {"lbl": lbl, "total": amount, "ate": agora + float(f.num_merge_s)}
	var vida := float(f.num_life_s)
	var t := create_tween().set_parallel()
	t.tween_property(lbl, "position:y", lbl.position.y - Dp.px(34.0), vida)
	t.tween_property(lbl, "modulate:a", 0.0, vida).set_ease(Tween.EASE_IN)
	t.chain().tween_callback(lbl.queue_free)


func _pintar_numero(lbl: Label, total: float, element: String, on_shield: bool) -> void:
	var f: Dictionary = Balance.FEEDBACK
	lbl.text = str(int(round(total)))
	var escala := minf(float(f.num_scale_base) + float(f.num_scale_gain) * total / 25.0,
			float(f.num_scale_max))
	lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(18.0) * escala), 11))
	lbl.add_theme_color_override("font_color",
			Color(str(f.cor_escudo)) if on_shield else Projectile.tint(element))
	lbl.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.85))
	lbl.add_theme_constant_override("outline_size", maxi(int(Dp.px(3.0)), 2))


# ---------- escudo evolutivo (GDD §5) ----------

func _on_escudo(entity: Node, shield: float, shield_max: float, level: int) -> void:
	if entity != player:
		return  # 6 bots escudados na barra do jogador nao e' HUD, e' ruido
	_escudo = shield
	_escudo_max = shield_max
	_escudo_nivel = level
	escudo_bar.get_parent().visible = shield_max > 0.0
	escudo_bar.queue_redraw()


func _on_escudo_quebrou(entity: Node) -> void:
	if entity != player:
		return
	_escudo_quebrou = 0.35  # branco piscando na barra (docs/DANO.md §4.1)
	escudo_bar.queue_redraw()


## Barra SEGMENTADA POR NIVEL: um traco por nivel conquistado. E' o unico
## indicador de progressao DENTRO da partida — o nivel importa mais que os
## pontos, entao a segmentacao (forma) vem antes da cor (GDD §10).
func _draw_escudo() -> void:
	var cores: Array = Balance.ESCUDO.cores
	var n := maxi(_escudo_nivel, 1)
	var cor := Color(str(cores[clampi(n - 1, 0, cores.size() - 1)]))
	if _escudo_quebrou > 0.0:
		cor = Color.WHITE
	var r := Rect2(Vector2.ZERO, escudo_bar.size)
	escudo_bar.draw_rect(r, Color(0, 0, 0, 0.45))
	var frac: float = _escudo / maxf(_escudo_max, 0.001)
	escudo_bar.draw_rect(Rect2(r.position, Vector2(r.size.x * clampf(frac, 0.0, 1.0), r.size.y)),
			cor)
	for i in range(1, n):
		var x := r.size.x * float(i) / float(n)
		escudo_bar.draw_line(Vector2(x, 0), Vector2(x, r.size.y), Color(0, 0, 0, 0.8),
				maxf(Dp.px(1.5), 1.0))


# ---------- habilidades (GDD §3 / §4) ----------

## Quem entrou em campo e se ele TEM kit. 17 dos 20 magos ainda nao tem: o botao
## aparece APAGADO, porque esconde-lo faria o jogador achar que o mago nao tem
## habilidade — e ele tem, na ficha, so' nao no codigo ainda.
func _on_kit_bound(slug: String, implementado: bool) -> void:
	for par: Array in [[tatica_btn, "tatica", Textos.HUD_TATICA],
			[suprema_btn, "suprema", Textos.HUD_SUPREMA]]:
		var b: AcaoButton = par[0]
		b.ativo = implementado
		b.rotulo = str(par[2])
		b.subtitulo = _nome_habilidade(slug, str(par[1])) if implementado \
				else Textos.HUD_KIT_EM_BREVE
		b.total_s = 0.0
		b.queue_redraw()


func _on_kit_cooldown(tipo: String, _restante: float, total: float) -> void:
	var b: AcaoButton = tatica_btn if tipo == "tatica" else suprema_btn
	b.total_s = total  # o SEGUNDO no botao; o 0..1 continuo vem do frac_*
	b.queue_redraw()


## A LEI DO §4.3 na tela: "se mata rapido, avisa antes". Vale para a suprema de
## QUALQUER conjurador — se a do inimigo fosse muda, nao existiria contra-jogada.
func _on_kit_telegraph(slug: String, _tipo: String, duracao: float, _pos: Vector3) -> void:
	var nome := _nome_habilidade(slug, "suprema")
	aviso.avisar(HudAviso.P_TELEGRAFO, Textos.HUD_TELEGRAFO % nome,
			Color(1.0, 0.75, 0.25), maxf(duracao, 0.6))


## O NOME da habilidade e' DADO do elenco (menu/Elenco.gd), nao texto de UI:
## "Muralha de Brasas — linha de fogo baixo..." vira "Muralha de Brasas".
static func _nome_habilidade(slug: String, campo: String) -> String:
	for m: Dictionary in Elenco.MAGOS:
		if str(m.get("slug", "")) == slug:
			return str(m.get(campo, "")).split("—")[0].strip_edges()
	return ""


# ---------- zona / tempestade arcana ----------

func _on_zona_avisou(_fase: int, centro: Vector3, _raio: float, segundos: float) -> void:
	aviso.bussola("zona", centro, Zona.COR)
	_contar(HudAviso.P_ZONA, segundos, Textos.ZONA_AVISO, Zona.COR)


func _on_zona_fechando(_fase: int, centro: Vector3, _raio: float, duracao: float) -> void:
	aviso.bussola("zona", centro, Zona.COR)
	_contagens.erase(HudAviso.P_ZONA)
	aviso.avisar(HudAviso.P_ZONA, Textos.ZONA_FECHANDO, Zona.COR, duracao)


## O tique da tempestade (1x por segundo, nunca por frame). A vinheta e o dps
## juntos respondem "quanto vai doer se eu nao correr".
func _on_zona_dano(_dano: float, dps: float) -> void:
	aviso.pulsar(Zona.COR, 0.9)
	aviso.avisar(HudAviso.P_ZONA_FORA,
			Textos.ZONA_FORA + Textos.HUD_SEP + Textos.ZONA_DPS % int(round(dps)),
			Color(1.0, 0.45, 0.45))


func _on_zona_estado(dentro: bool) -> void:
	if dentro:
		aviso.limpar(HudAviso.P_ZONA_FORA)
	else:
		aviso.avisar(HudAviso.P_ZONA_FORA, Textos.ZONA_FORA, Color(1.0, 0.45, 0.45))
		var z := _zona()
		if z != null:
			aviso.bussola("zona", z.centro, Zona.COR)  # a seta e' a saida


## O centro/raio CONTINUOS nao tem sinal de proposito: a HUD le a Zona direto.
## Defensivo porque o selftest monta a HUD sem partida em volta.
func _zona() -> Zona:
	var m := get_parent()
	if m != null and "zona" in m:
		var z: Variant = m.get("zona")
		if z is Zona and is_instance_valid(z):
			return z as Zona
	return null


# ---------- bau celestial (GDD §16.2) ----------

func _on_bau_anunciado(pos: Vector3, segundos: float) -> void:
	aviso.bussola("bau", pos, _cor_lendaria())
	_contar(HudAviso.P_BAU, segundos,
			Textos.BAU_TITULO + Textos.HUD_SEP + Textos.BAU_CAINDO, _cor_lendaria())


func _on_bau_pousou(pos: Vector3) -> void:
	aviso.bussola("bau", pos, _cor_lendaria())
	_contagens.erase(HudAviso.P_BAU)
	aviso.avisar(HudAviso.P_BAU, Textos.BAU_POUSOU, _cor_lendaria())


## 0.0 = INTERROMPEU (saiu do raio, caiu, morreu). O numero continua na faixa
## (o quanto falta), mas o ESTADO virou anel no meio de baixo: a faixa e' texto
## e texto que troca nao le' como perda. Regra do projeto — cancelar e' estado
## de 1a classe, e estado se le' em COR + FORMA (ver HudAviso.canalizar).
func _on_bau_canalizando(pawn: Node, progresso: float) -> void:
	if pawn != null and pawn != player:
		return  # canalizacao de bot nao acende a barra do jogador
	if progresso <= 0.0:
		## INTERROMPEU. A faixa volta ao texto de "da' pra abrir", mas quem diz
		## que o progresso MORREU e' o anel virando X (cor + forma): so' trocar
		## a frase la' em cima lia como "nao aconteceu nada".
		aviso.cancelar()
		aviso.avisar(HudAviso.P_BAU, Textos.BAU_POUSOU, _cor_lendaria())
		return
	aviso.canalizar(progresso, _cor_lendaria())
	aviso.avisar(HudAviso.P_BAU,
			Textos.BAU_ABRINDO + Textos.HUD_SEP + "%d%%" % int(progresso * 100.0),
			_cor_lendaria())


func _on_bau_aberto(por_player: bool, elementos: PackedStringArray) -> void:
	aviso.canalizar_fim()  # terminou: o anel some sem marca de interrupcao
	aviso.bussola("bau", Vector3.ZERO, Color.WHITE, false)
	_contagens.erase(HudAviso.P_BAU)
	var txt := Textos.BAU_PERDIDO
	if por_player and elementos.size() >= 2:
		txt = Textos.BAU_MANOPLA % [_el(elementos[0]), _el(elementos[1])]
	aviso.avisar(HudAviso.P_BAU, txt, _cor_lendaria(), 5.0)


func _cor_lendaria() -> Color:
	return Arma.RARIDADES["lendaria"].cor


func _el(id: String) -> String:
	return str(Textos.HUD_ELEMENTOS.get(id, id))


# ---------- loot e arma arcana ----------

## "TEM COISA AQUI, E E' ISTO": o botao PEGAR so' existe com loot ao alcance
## (botao sem funcao na tela e' poluicao), e carrega as tres leituras do item —
## NOME embaixo, COR e FORMA da raridade no contorno (GDD §10: cor sozinha nao
## e' leitura). Raridade que a HUD nao conhece cai no disco branco de sempre.
func _on_loot_prompt(nome: String, raridade: String, perto: bool) -> void:
	pegar_btn.visible = perto
	# PEGAR de maos nuas, TROCAR com luva na mao (decisao no 5, 26/08): o
	# rotulo avisa que a acao tem custo — trocar deixa a sua no chao.
	pegar_btn.rotulo = Textos.LOOT_TROCAR if ArmaSlot.armado_de(player) \
			else Textos.LOOT_PEGAR
	if perto:
		var r: Dictionary = Arma.RARIDADES.get(raridade,
				{"cor": Color.WHITE, "forma": ""})
		pegar_btn.subtitulo = nome
		pegar_btn.cor = r.cor
		pegar_btn.forma = str(r.get("forma", ""))
		pegar_btn.queue_redraw()


func _on_arma(pawn: Node, _arma_id: String, nome: String, raridade: String,
		elementos: PackedStringArray) -> void:
	# O sinal DIZ de quem e' (core/Bus.gd). Antes isto comparava o arma_id com
	# o slot do jogador, e um BOT com a MESMA arma mudava o icone dele.
	if pawn != player or not is_instance_valid(player):
		return  # arma de bot nao entra no icone do jogador
	# UMA funcao monta o rotulo (Textos.arma_rotulo): o botao de PEGAR do chao
	# e esta confirmacao dizem a MESMA coisa da MESMA forma — nome + elemento.
	arma_lbl.text = Textos.arma_rotulo(nome, elementos)
	arma_lbl.add_theme_color_override("font_color",
			Arma.RARIDADES.get(raridade, {"cor": Color.WHITE}).cor)
	## O "PEGUEI": o rotulo PISCA. Sem isso a unica confirmacao de que a troca
	## aconteceu era um texto mudando num canto — no meio de uma briga ninguem
	## ve'. O pulso dura menos que o gesto do mago, entao os dois se somam.
	## SEM ARMA NAO HA' ATAQUE (DIRECAO.md par.1): equipar e' o que ACENDE o
	## botao de disparo, com o elemento da luva. Ordem do Diretor (26/08): "na
	## parte de ataques nao aparece nada ate' que equipe alguma arma de verdade"
	## — como em todo battle royale.
	fire_btn.desarmado = false
	## E o rotulo agora SOME: antes ficava pendurado a partida inteira ("pode
	## apagar este texto da manopla para ser visto de cima" — Diretor, 26/08).
	## Confirmacao e' EVENTO, nao painel: quem quer saber o que tem na mao olha
	## o botao de ataque, que diz o elemento. Pulso -> espera -> apaga.
	arma_lbl.modulate = Color(2.2, 2.2, 2.2)
	var tw := create_tween()
	tw.tween_property(arma_lbl, "modulate", Color.WHITE, 0.45)
	tw.tween_interval(ARMA_LBL_S)
	tw.tween_property(arma_lbl, "modulate:a", 0.0, 0.5)
	# A LUVA TRAVA O ELEMENTO (26/08 — DIRECAO.md §1): com elemento na luva o
	# carrossel virava MENTIRA — mostrava VENTO e o tiro saia FOGO (visto no
	# video do Diretor). Com o elemento travado ele some; volta de maos nuas.
	carousel.visible = elementos.is_empty() or str(elementos[0]) == ""


# ---------- derrubado (GDD §3.7/§3.8) ----------

func _on_derrubada(entity: Node, _causador: Node) -> void:
	if entity == player:
		aviso.derrubar(true)


func _on_reerguida(entity: Node, por: Node) -> void:
	if entity == player:
		aviso.derrubar(false)
	if por == player:
		aviso.resgatar(false)


## 4 Hz e SO' quando o player esta' envolvido: caido (a barra de esvaecimento e'
## dele) ou resgatando (o anel e' do aliado no chao).
func _on_derrubado_progresso(entity: Node, esvaecimento: float, reerguer: float) -> void:
	if entity != player:
		aviso.resgatar(true)
	aviso.progresso(esvaecimento, reerguer)


# ---------- contagens regressivas ----------

## Uma contagem por prioridade de faixa. Reescreve o texto so' quando o SEGUNDO
## inteiro muda — nao a cada frame.
func _contar(prio: int, segundos: float, fmt: String, cor: Color) -> void:
	_contagens[prio] = {
		"ate": float(Time.get_ticks_msec()) / 1000.0 + segundos,
		"fmt": fmt, "cor": cor, "s": -1,
	}


func _tick_contagens() -> void:
	var agora := float(Time.get_ticks_msec()) / 1000.0
	for prio: int in _contagens.keys():
		var c: Dictionary = _contagens[prio]
		var s := int(ceilf(float(c.ate) - agora))
		if s <= 0:
			_contagens.erase(prio)
			aviso.limpar(prio)
		elif s != int(c.s):
			c.s = s
			aviso.avisar(prio, str(c.fmt) % s, c.cor)


## Olhar livre: arrastar na metade direita FORA dos botoes gira a camera.
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
	for c: Control in [fire_btn, dodge_btn, tatica_btn, suprema_btn, pegar_btn, carousel]:
		if c.visible and c.get_global_rect().has_point(pos):
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
	for b: Control in [hp_bar, mana_bar, escudo_bar]:
		b.custom_minimum_size = Vector2(Dp.px(130.0), Dp.px(12.0))
	# A faixa e os badges desenham a mao: recebem o recorte, nao offsets.
	aviso.safe = Vector4(sl, st, sr, sb)

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

	# Esquiva, TATICA e SUPREMA na MESMA fileira, a esquerda do Fogo: sao os
	# tres botoes que o polegar direito alcanca sem sair do lugar. 64dp cada
	# (>= 48dp) — 3 x 64 + Fogo = 316dp, sobra tela ate' a zona do joystick.
	var db := Dp.px(64.0)
	var dir := fire_btn.offset_left
	for b: AcaoButton in [dodge_btn, tatica_btn, suprema_btn]:
		b.offset_right = dir - Dp.px(12.0)
		b.offset_left = b.offset_right - db
		b.offset_bottom = fire_btn.offset_bottom
		b.offset_top = b.offset_bottom - db
		dir = b.offset_left

	var slot := Dp.px(52.0)
	# pausa: canto superior direito, abaixo do relogio da partida
	pausa_btn.offset_right = -(sr + g)
	pausa_btn.offset_left = pausa_btn.offset_right - Dp.px(44.0)
	pausa_btn.offset_top = st + g + Dp.px(58.0)
	pausa_btn.offset_bottom = pausa_btn.offset_top + Dp.px(44.0)
	carousel.offset_right = -(sr + g)
	carousel.offset_left = carousel.offset_right - slot * float(Balance.ELEMENTS.size())
	carousel.offset_bottom = fire_btn.offset_top - Dp.px(10.0)
	carousel.offset_top = carousel.offset_bottom - slot

	# Nome da arma equipada logo ACIMA do carrossel: mesma coluna, mesma leitura.
	arma_lbl.offset_right = -(sr + g)
	arma_lbl.offset_left = -sr - Dp.px(320.0)
	arma_lbl.offset_bottom = carousel.offset_top - Dp.px(4.0)
	arma_lbl.offset_top = arma_lbl.offset_bottom - Dp.px(16.0)

	# PEGAR no rodape, centrado: e' a UNICA coisa da HUD no meio, e fica na
	# faixa de baixo (longe do reticulo, que mora no meio da ALTURA).
	# 64dp = o mesmo alvo da fileira do polegar (bem acima dos 48dp de piso).
	# Era 56dp; subiu junto com a leitura nova (contorno da raridade + nome),
	# e porque interagir agora e' um ATO, nao um clique de inventario.
	var pb := Dp.px(64.0)
	pegar_btn.offset_left = -pb / 2.0
	pegar_btn.offset_right = pb / 2.0
	pegar_btn.offset_bottom = -(sb + Dp.px(40.0))
	pegar_btn.offset_top = pegar_btn.offset_bottom - pb

	# O escurecido cobre a tela inteira (fica bonito sangrando ate' a borda);
	# so' o CONTEUDO respeita a area util.
	Safe.apply(_end_center, 16.0)


# ---------- montagem ----------

func _build_bars() -> void:
	_bars_box = VBoxContainer.new()
	_bars_box.add_theme_constant_override("separation", int(Dp.px(4.0)))
	hp_bar = _bar(Color(0.85, 0.2, 0.2))
	mana_bar = _bar(Color(0.2, 0.45, 0.95))
	escudo_bar = Control.new()
	escudo_bar.draw.connect(_draw_escudo)
	_bars_box.add_child(_bar_row(Textos.HUD_VIDA, hp_bar))
	_bars_box.add_child(_bar_row(Textos.HUD_MANA, mana_bar))
	var linha := _bar_row(Textos.HUD_ESCUDO, escudo_bar)
	linha.visible = false  # so' aparece quando o escudo existe (GDD §5)
	_bars_box.add_child(linha)
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


func _bar_row(text: String, bar: Control) -> HBoxContainer:
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


func _build_aviso() -> void:
	aviso = HudAviso.new()
	add_child(aviso)


## So' cria e liga os sinais — tamanho e margem sao do _layout (area segura).
## Alvos de toque em dp: joystick 150, Fogo 88, acoes 64, PEGAR 64, slot 52.
func _build_sticks() -> void:
	joystick = VirtualJoystick.new()
	joystick.anchor_top = 1.0
	joystick.anchor_bottom = 1.0
	add_child(joystick)

	fire_btn = FireButton.new()
	_canto(fire_btn)
	fire_btn.fired.connect(func() -> void:
		if is_instance_valid(player):
			player.request_fire())
	fire_btn.aim_state.connect(func(a: bool) -> void:
		if is_instance_valid(player):
			player.set_aiming(a))
	fire_btn.aim_delta.connect(func(rel: Vector2) -> void:
		if is_instance_valid(player):
			player.add_look(rel))

	# Esquiva / TATICA / SUPREMA: mesmo widget, mesmo desenho de cooldown.
	# Os tres so' PEDEM — quem decide e' o Pawn (esquiva) e o KitRunner (kit).
	dodge_btn = _acao(Textos.HUD_ESQUIVA, Color(0.25, 0.85, 0.95),
			func() -> void: player.request_dodge())
	tatica_btn = _acao(Textos.HUD_TATICA, Color(0.55, 0.80, 1.00),
			func() -> void: player.request_tatica())
	suprema_btn = _acao(Textos.HUD_SUPREMA, Color(1.00, 0.72, 0.30),
			func() -> void: player.request_suprema())
	tatica_btn.ativo = false   # ate' o kit_bound dizer que o mago tem kit
	suprema_btn.ativo = false
	# A suprema mostra PORCENTAGEM 0->100% (26/08) — e' carga, nao cooldown.
	suprema_btn.mostra_carga = true

	# PEGAR (GDD §16.2): nasce escondido e so' aparece com loot ao alcance.
	pegar_btn = _acao(Textos.LOOT_PEGAR, Color(0.85, 0.90, 1.00),
			func() -> void: player.request_pegar())
	pegar_btn.anchor_left = 0.5
	pegar_btn.anchor_right = 0.5
	pegar_btn.visible = false

	arma_lbl = Label.new()
	arma_lbl.anchor_left = 1.0
	arma_lbl.anchor_right = 1.0
	arma_lbl.anchor_top = 1.0
	arma_lbl.anchor_bottom = 1.0
	arma_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	arma_lbl.add_theme_font_size_override("font_size", maxi(int(Dp.px(11.0)), 8))
	add_child(arma_lbl)

	# Carrossel de elementos ACIMA do botao de disparo (GDD §19.3). 5 slots de
	# 52dp (>= 48dp) em UMA fileira: 260dp cabem no canto direito sem invadir a
	# zona do joystick (35% da esquerda) nem a de olhar — 2 fileiras so' se um
	# 6o elemento entrar.
	# PAUSA (pedido do Diretor 26/08 + GDD §12): icone no canto superior
	# direito abre RETOMAR / CONFIGURACOES / ABANDONAR dentro da partida.
	# process_mode ALWAYS: com a arvore pausada, so' eles continuam ouvindo.
	pausa_btn = _acao(Textos.HUD_PAUSA, Color(0.75, 0.78, 0.90),
			func() -> void: _abrir_pausa())
	pausa_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	# _canto ancora no RODAPE (fileira do polegar); a pausa mora no TOPO. Sem
	# re-ancorar, o offset_top do _layout contava da borda de BAIXO e o botao
	# nascia ~58dp ABAIXO da tela — invisivel no aparelho (2o video, 26/08).
	pausa_btn.anchor_top = 0.0
	pausa_btn.anchor_bottom = 0.0
	carousel = ElementCarousel.new()
	_canto(carousel)
	carousel.chosen.connect(func(el: String) -> void:
		if is_instance_valid(player):
			player.set_element(el))


func _acao(rotulo: String, cor: Color, acao: Callable) -> AcaoButton:
	var b := AcaoButton.new()
	b.rotulo = rotulo
	b.cor = cor
	_canto(b)
	b.tocado.connect(func() -> void:
		if is_instance_valid(player):
			acao.call())
	return b


## Canto inferior direito — a ancora de todo controle de polegar.
func _canto(c: Control) -> void:
	c.anchor_left = 1.0
	c.anchor_right = 1.0
	c.anchor_top = 1.0
	c.anchor_bottom = 1.0
	add_child(c)


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
	btn.text = Textos.HUD_JOGAR_DE_NOVO
	btn.custom_minimum_size = Vector2(Dp.px(200.0), Dp.px(56.0))  # >= 48dp
	btn.add_theme_font_size_override("font_size", maxi(int(Dp.px(16.0)), 10))
	btn.pressed.connect(func() -> void: restart_pressed.emit())
	box.add_child(btn)
	# VOLTAR AO MENU (costura da R18, coordenador): o menu nao guarda estado e
	# reconstroi tudo em _ready, entao a troca de cena e' segura. Fiacao direta,
	# como o JOGAR do menu — defensiva se a cena sumir.
	var menu_btn := Button.new()
	menu_btn.text = Textos.HUD_MENU
	menu_btn.custom_minimum_size = Vector2(Dp.px(200.0), Dp.px(56.0))
	menu_btn.add_theme_font_size_override("font_size", maxi(int(Dp.px(16.0)), 10))
	menu_btn.pressed.connect(func() -> void:
		if ResourceLoader.exists("res://menu/Menu.tscn"):
			get_tree().change_scene_to_file("res://menu/Menu.tscn")
	)
	box.add_child(menu_btn)
	_end_center.add_child(box)
	add_child(end_screen)


# ---------------------------------------------------------------- pausa

## A PAUSA DE PARTIDA (GDD §12: Retomar / Configuracoes / Abandonar). Montada
## UMA vez, escondida; get_tree().paused congela o mundo e so' o que e' ALWAYS
## continua ouvindo o dedo. As Configuracoes sao o MESMO menu/Config.gd do menu
## principal — uma tela, dois lugares, zero divergencia.
func _abrir_pausa() -> void:
	if _pausa_overlay == null:
		_montar_pausa()
	get_tree().paused = true
	_pausa_overlay.visible = true


func _retomar() -> void:
	get_tree().paused = false
	if _pausa_overlay != null:
		_pausa_overlay.visible = false


func _abandonar() -> void:
	get_tree().paused = false
	get_tree().change_scene_to_file("res://menu/Menu.tscn")


func _montar_pausa() -> void:
	_pausa_overlay = Control.new()
	_pausa_overlay.name = "Pausa"
	_pausa_overlay.process_mode = Node.PROCESS_MODE_ALWAYS
	_pausa_overlay.visible = false
	add_child(_pausa_overlay)
	_pausa_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	var veu := ColorRect.new()
	veu.color = Color(0.02, 0.03, 0.06, 0.82)
	_pausa_overlay.add_child(veu)
	veu.set_anchors_preset(Control.PRESET_FULL_RECT)
	var cc := CenterContainer.new()
	_pausa_overlay.add_child(cc)
	cc.set_anchors_preset(Control.PRESET_FULL_RECT)
	var v := VBoxContainer.new()
	v.alignment = BoxContainer.ALIGNMENT_CENTER
	v.add_theme_constant_override("separation", 14)
	cc.add_child(v)
	var titulo := Estilo.rotulo(Textos.PAUSA_TITULO, 34, Estilo.OURO)
	titulo.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	v.add_child(titulo)
	var retomar := Estilo.botao(Textos.PAUSA_RETOMAR, "BtnRetomar", 52.0)
	retomar.pressed.connect(_retomar)
	v.add_child(retomar)
	var cfg := Estilo.botao(Textos.MENU_CONFIG, "BtnPausaConfig", 52.0)
	cfg.pressed.connect(_abrir_config_na_pausa)
	v.add_child(cfg)
	var sair := Estilo.botao(Textos.PAUSA_ABANDONAR, "BtnAbandonar", 52.0)
	sair.pressed.connect(_abandonar)
	v.add_child(sair)


func _abrir_config_na_pausa() -> void:
	var Cfg := load("res://menu/Config.gd") as GDScript
	if Cfg == null:
		return  # fiacao defensiva: sem a tela, a pausa continua funcionando
	var c: Control = Cfg.new()
	c.name = "ConfigNaPausa"
	_pausa_overlay.add_child(c)
	c.set_anchors_preset(Control.PRESET_FULL_RECT)
	c.voltar_pedido.connect(func() -> void: c.queue_free())
