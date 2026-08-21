## CAMADA DE AVISOS DA HUD — tudo que APARECE e SOME por evento mora aqui:
## vinheta de dano, arcos direcionais, setas de bussola (zona/bau), a faixa de
## aviso, os badges de estado e o painel de DERRUBADO.
##
## POR QUE UM Control SO': sao oito coisas que piscam ao mesmo tempo numa
## partida com 6 bots. Uma por no' seria oito _process e oito redraws; aqui e'
## UM _process que so' redesenha quando alguma coisa mexeu, e um
## _draw() que decide a ordem de leitura de uma vez so'.
##
## A LEI DA PRIORIDADE (a tela vira sopa se tudo couber junto): a faixa de aviso
## tem UMA linha e cinco candidatos. Ganha o de MENOR numero de prioridade — e
## o que salva a vida do jogador tem numero menor. O resto nao encolhe: some.
##
## NADA AQUI DECIDE JOGO. Todo estado chega de fora (ui/Hud.gd, que observa o
## Bus); este no' so' desenha.
class_name HudAviso
extends Control

## Prioridade da faixa. Menor ganha. Ver "A LEI DA PRIORIDADE" no topo.
## DERRUBADO nao esta' na lista de proposito: ele nao disputa a faixa, ele a
## CALA (ver _draw_faixa) — quem esta' no chao nao tem o que fazer com um aviso
## de bau, e o painel proprio ja' diz o que importa.
const P_ZONA_FORA := 0    # esta' morrendo na tempestade AGORA
const P_TELEGRAFO := 1    # uma suprema foi anunciada (GDD §4.3, janela de fuga)
const P_ZONA := 2         # contagem da proxima parede
const P_BAU := 3          # oportunidade, nao ameaca — e' o primeiro a sumir

## Quatro badges e' o teto de leitura de relance no celular; o 5o estado existe
## no jogo mas nao na tela (ver relatorio da raia UI).
const MAX_BADGES := 4

var safe := Vector4.ZERO  # l, t, r, b em px de canvas — vem do Hud._layout
var player: Node = null   # Node3D com cam_yaw; duck-typed de proposito

var _faixa := {}          # prio -> {texto, cor, ate}  (ate <= 0 = ate' limpar)
var _arcos: Array = []    # {pos: Vector3, cor: Color, ate: float}
var _vinheta := 0.0
var _vinheta_cor := Color(0.9, 0.15, 0.15)
var _vinheta_ultima := -99.0
var _badges: Array[String] = []
var _bussola := {}        # chave -> {pos: Vector3, cor: Color}
var _caido := false
var _resgatando := false
var _esvaecimento := 0.0
var _reerguer := 0.0


func _ready() -> void:
	mouse_filter = MOUSE_FILTER_IGNORE
	anchor_right = 1.0
	anchor_bottom = 1.0


# ------------------------------------------------------------------ API

## Faixa de aviso. dur <= 0 fica ate' `limpar(prio)` (estado, nao evento).
func avisar(prio: int, texto: String, cor: Color, dur := 0.0) -> void:
	_faixa[prio] = {"texto": texto, "cor": cor, "ate": (_agora() + dur) if dur > 0.0 else 0.0}
	queue_redraw()


func limpar(prio: int) -> void:
	if _faixa.erase(prio):
		queue_redraw()


## Arco direcional de quem te acertou (docs/DANO.md §4.4): 60 graus na borda, no
## angulo do atacante RELATIVO AO YAW DA CAMERA — o jogador le a tela, nao o
## corpo. Guarda a posicao do MUNDO: girar a camera gira o arco junto.
func arco(de: Vector3, cor: Color) -> void:
	var f: Dictionary = Balance.FEEDBACK
	_arcos.append({"pos": de, "cor": cor, "ate": _agora() + float(f.arc_dur_s)})
	while _arcos.size() > int(f.arc_max):
		_arcos.pop_front()  # o 4o substitui o mais antigo
	queue_redraw()


## Vinheta de borda. O minimo entre dois disparos e' do Balance (anti-spam do
## DoT, docs/DANO.md §4.3): sem ele o terreno pisca 60x por segundo — era o
## defeito §2.3, um tique de 0,1 de dano por frame de fisica.
func pulsar(cor: Color, forca := 1.0) -> void:
	var agora := _agora()
	if agora - _vinheta_ultima < float(Balance.FEEDBACK.vignette_min_s):
		return
	_vinheta_ultima = agora
	_vinheta_cor = cor
	_vinheta = maxf(_vinheta, clampf(forca, 0.0, 1.0))
	queue_redraw()


## Badge de estado de kit. Nome sem rotulo em Textos.HUD_ESTADOS e' IGNORADO —
## e' o contrato do Bus: estado novo nao pode quebrar a HUD.
func estado(nome: String, ligado: bool) -> void:
	if not Textos.HUD_ESTADOS.has(nome):
		return
	if ligado:
		if not _badges.has(nome):
			_badges.append(nome)
	else:
		_badges.erase(nome)
	queue_redraw()


## Seta de bussola para um ponto do mundo (centro da zona, pouso do bau).
func bussola(chave: String, pos: Vector3, cor: Color, ligado := true) -> void:
	if ligado:
		_bussola[chave] = {"pos": pos, "cor": cor}
	else:
		_bussola.erase(chave)
	queue_redraw()


func derrubar(caido: bool) -> void:
	_caido = caido
	if not caido:
		_esvaecimento = 0.0
		_reerguer = 0.0
	queue_redraw()


func resgatar(on: bool) -> void:
	_resgatando = on
	queue_redraw()


func progresso(esvaecimento: float, reerguer: float) -> void:
	_esvaecimento = clampf(esvaecimento, 0.0, 1.0)
	_reerguer = clampf(reerguer, 0.0, 1.0)
	queue_redraw()


## Zera tudo — restart de partida nao pode herdar aviso da partida velha.
func zerar() -> void:
	_faixa.clear()
	_arcos.clear()
	_bussola.clear()
	_badges.clear()
	_vinheta = 0.0
	_vinheta_ultima = -99.0
	_caido = false
	_resgatando = false
	queue_redraw()


# ------------------------------------------------------------------ tique

func _process(delta: float) -> void:
	var mexeu := false
	if _vinheta > 0.0:
		_vinheta = maxf(_vinheta - delta * 2.2, 0.0)
		mexeu = true
	var agora := _agora()
	for prio: int in _faixa.keys():
		var ate: float = float(_faixa[prio].ate)
		if ate > 0.0 and agora >= ate:
			_faixa.erase(prio)
			mexeu = true
	var n := _arcos.size()
	_arcos = _arcos.filter(func(a: Dictionary) -> bool: return agora < float(a.ate))
	mexeu = mexeu or _arcos.size() != n
	# Seta e arco sao ANGULOS: giram com a camera mesmo sem evento novo.
	if not _arcos.is_empty() or (not _bussola.is_empty() and _tem_ancora()):
		mexeu = true
	if mexeu:
		queue_redraw()


func _agora() -> float:
	return float(Time.get_ticks_msec()) / 1000.0


func _tem_ancora() -> bool:
	return is_instance_valid(player) and player.get("cam_yaw") != null


## Angulo de tela (0 = 12h, cresce no sentido horario) de um ponto do mundo.
func _angulo(pos: Vector3) -> float:
	var origem: Vector3 = player.global_position
	var d := pos - origem
	return atan2(d.x, -d.z) - float(player.cam_yaw.rotation.y)


# ------------------------------------------------------------------ desenho

## Retangulos publicos porque o selftest cobra area segura neles (e porque o
## _draw precisa dos mesmos numeros — uma conta so', nao duas).
func rect_faixa() -> Rect2:
	var t := _tela()
	var alt := Dp.px(26.0)
	return Rect2(safe.x + Dp.px(8.0), safe.y + Dp.px(44.0),
			maxf(t.x - safe.x - safe.z - Dp.px(16.0), 1.0), alt)


func rect_badges() -> Rect2:
	var t := _tela()
	return Rect2(safe.x + Dp.px(16.0), safe.y + Dp.px(92.0),
			maxf(t.x * 0.5 - safe.x, 1.0), Dp.px(20.0))


func rect_derrubado() -> Rect2:
	var t := _tela()
	var w := Dp.px(240.0)
	return Rect2((t.x - w) / 2.0, t.y - safe.w - Dp.px(150.0), w, Dp.px(64.0))


func _tela() -> Vector2:
	return size if size.x > 1.0 else Safe.canvas()


func _draw() -> void:
	_draw_vinheta()
	_draw_arcos()
	_draw_bussola()
	_draw_faixa()
	_draw_badges()
	if _caido or _resgatando:
		_draw_derrubado()


## Vinheta: quatro tarjas nas bordas. NAO escurece o meio — a mira mora la'.
func _draw_vinheta() -> void:
	if _vinheta <= 0.0:
		return
	var t := _tela()
	var e := minf(t.x, t.y) * 0.13
	var c := Color(_vinheta_cor, 0.42 * _vinheta)
	draw_rect(Rect2(0, 0, t.x, e), c)          # ponytail: tarja chapada, sem
	draw_rect(Rect2(0, t.y - e, t.x, e), c)    # gradiente. Gradiente pede shader
	draw_rect(Rect2(0, 0, e, t.y), c)          # ou textura; se ficar duro demais
	draw_rect(Rect2(t.x - e, 0, e, t.y), c)    # no aparelho, ai' vira shader.


## Arcos de 60 graus na borda: de onde veio o dano (docs/DANO.md §4.4).
func _draw_arcos() -> void:
	if _arcos.is_empty() or not _tem_ancora():
		return
	var f: Dictionary = Balance.FEEDBACK
	var t := _tela()
	var c := t / 2.0
	var r := minf(t.x, t.y) * 0.42
	var meio := deg_to_rad(float(f.arc_deg)) / 2.0
	var agora := _agora()
	for a: Dictionary in _arcos:
		var vida: float = clampf((float(a.ate) - agora) / float(f.arc_dur_s), 0.0, 1.0)
		# -PI/2 leva o zero do draw_arc (3h) para as 12h, que e' o zero do _angulo.
		var ang: float = _angulo(a.pos) - PI / 2.0
		draw_arc(c, r, ang - meio, ang + meio, 24,
				Color(a.cor, 0.85 * vida), Dp.px(6.0), true)


## Seta de bussola: triangulo no anel, apontando para fora, na direcao do alvo.
func _draw_bussola() -> void:
	if _bussola.is_empty() or not _tem_ancora():
		return
	var t := _tela()
	var c := t / 2.0
	var r := minf(t.x, t.y) * 0.32
	var s := Dp.px(11.0)
	for chave: String in _bussola:
		var d: Dictionary = _bussola[chave]
		var ang := _angulo(d.pos)
		var dir := Vector2(sin(ang), -cos(ang))
		var p := c + dir * r
		var lado := dir.orthogonal()
		draw_colored_polygon(PackedVector2Array([
			p + dir * s, p - dir * s * 0.6 + lado * s * 0.75,
			p - dir * s * 0.6 - lado * s * 0.75,
		]), d.cor)


## QUEM GANHOU a faixa neste instante ("" = ninguem). Publico porque e' a
## regra de prioridade inteira num lugar so' — o _draw le daqui e o selftest
## cobra daqui, entao nao existe versao "de teste" divergente da desenhada.
func faixa_ativa() -> String:
	if _faixa.is_empty() or _caido:
		return ""  # no chao a faixa cala: o painel de DERRUBADO e' a unica leitura
	var prios := _faixa.keys()
	prios.sort()
	return str(_faixa[prios[0]].texto)  # menor prioridade GANHA; o resto some


func _draw_faixa() -> void:
	var texto := faixa_ativa()
	if texto == "":
		return
	var prios := _faixa.keys()
	prios.sort()
	var d: Dictionary = _faixa[prios[0]]
	var r := rect_faixa()
	var fs := maxi(int(Dp.px(17.0)), 11)
	var font := get_theme_default_font()
	draw_string_outline(font, Vector2(r.position.x, r.position.y + fs), texto,
			HORIZONTAL_ALIGNMENT_CENTER, r.size.x, fs, maxi(int(Dp.px(3.0)), 2),
			Color(0, 0, 0, 0.8))
	draw_string(font, Vector2(r.position.x, r.position.y + fs), texto,
			HORIZONTAL_ALIGNMENT_CENTER, r.size.x, fs, d.cor)


func _draw_badges() -> void:
	if _badges.is_empty():
		return
	var r := rect_badges()
	var fs := maxi(int(Dp.px(10.0)), 8)
	var font := get_theme_default_font()
	var x := r.position.x
	for i in mini(_badges.size(), MAX_BADGES):
		var txt: String = str(Textos.HUD_ESTADOS[_badges[i]])
		var w: float = font.get_string_size(txt, HORIZONTAL_ALIGNMENT_LEFT, -1, fs).x
		var chip := Rect2(x, r.position.y, w + Dp.px(12.0), r.size.y)
		draw_rect(chip, Color(0.05, 0.06, 0.09, 0.55))
		draw_rect(chip, Color(0.75, 0.85, 1.0, 0.45), false, Dp.px(1.0))
		draw_string(font, Vector2(x + Dp.px(6.0), r.position.y + r.size.y * 0.75), txt,
				HORIZONTAL_ALIGNMENT_LEFT, -1, fs, Color(0.92, 0.96, 1.0, 0.95))
		x = chip.end.x + Dp.px(6.0)


## Painel de DERRUBADO. Barra de ESVAECIMENTO (o tempo que resta) e ANEL de
## resgate. Aqui a regra "nada na coluna central" cai de proposito: quem esta'
## no chao nao esta' mirando, e o unico numero que importa e' esse.
func _draw_derrubado() -> void:
	var r := rect_derrubado()
	var font := get_theme_default_font()
	var fs := maxi(int(Dp.px(13.0)), 9)
	var titulo := Textos.DERRUBADO_VOCE if _caido else Textos.DERRUBADO_ALIADO
	draw_string_outline(font, Vector2(r.position.x, r.position.y), titulo,
			HORIZONTAL_ALIGNMENT_CENTER, r.size.x, fs, maxi(int(Dp.px(3.0)), 2),
			Color(0, 0, 0, 0.8))
	draw_string(font, Vector2(r.position.x, r.position.y), titulo,
			HORIZONTAL_ALIGNMENT_CENTER, r.size.x, fs, Color(1.0, 0.45, 0.35))
	if _caido:
		var alt := Dp.px(10.0)
		var trilho := Rect2(r.position.x, r.position.y + Dp.px(10.0), r.size.x, alt)
		draw_rect(trilho, Color(0, 0, 0, 0.5))
		draw_rect(Rect2(trilho.position, Vector2(trilho.size.x * _esvaecimento, alt)),
				Color(1.0, 0.55, 0.30, 0.9))
		var segs := int(ceilf(_esvaecimento * float(Derrubado.ESVAECER_S)))
		draw_string(font, Vector2(r.position.x, r.position.y + Dp.px(34.0)),
				Textos.DERRUBADO_ESVAECENDO % segs,
				HORIZONTAL_ALIGNMENT_CENTER, r.size.x, maxi(int(Dp.px(11.0)), 8),
				Color(1, 1, 1, 0.85))
	if _reerguer > 0.0:
		var c := Vector2(r.position.x + r.size.x / 2.0, r.end.y + Dp.px(22.0))
		var raio := Dp.px(18.0)
		draw_arc(c, raio, 0, TAU, 32, Color(1, 1, 1, 0.22), Dp.px(3.0), true)
		draw_arc(c, raio, -PI / 2.0, -PI / 2.0 + TAU * _reerguer, 32,
				Color(0.45, 1.0, 0.6), Dp.px(3.4), true)
		draw_string(font, Vector2(c.x - Dp.px(60.0), c.y + raio + Dp.px(14.0)),
				Textos.DERRUBADO_REERGUENDO, HORIZONTAL_ALIGNMENT_CENTER,
				Dp.px(120.0), maxi(int(Dp.px(10.0)), 8), Color(0.7, 1.0, 0.8, 0.9))
