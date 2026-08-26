## BOTAO DE ACAO REDONDO — Esquiva, Tatica, Suprema e PEGAR sao o MESMO widget.
## Nasceu como ui/DodgeButton.gd; quando entraram as habilidades (GDD §3) ficaram
## quatro botoes com o mesmo desenho, o mesmo toque e o mesmo cooldown, entao a
## classe virou uma so' em vez de quatro copias.
##
## O botao so' PEDE. Quem decide cooldown, mana e i-frames e' o gameplay
## (Pawn/KitRunner) — a UI nunca decide jogo (regra do core).
##
## Alvo de toque: quem monta e' que da' o tamanho (>= 48dp, cobrado no selftest).
class_name AcaoButton
extends Control

signal tocado

## Rotulo curto DENTRO do botao (Textos.*). Vazio = so' o disco.
var rotulo := ""
## Segunda linha, menor: o NOME da habilidade do mago (menu/Elenco.gd).
var subtitulo := ""
var cor := Color(0.25, 0.85, 0.95)
## FORMA da borda ("circulo" | "losango" | "triangulo" — Arma.RARIDADES).
## Vazio/desconhecida = o disco de sempre. E' a lei do GDD §10 dentro do botao:
## o PEGAR ja' mudava de COR por raridade, e cor sozinha nao e' leitura — quem
## nao distingue cor precisa ver que o contorno virou losango/triangulo.
var forma := ""
## 0..1 (0 = pronto). Callable porque o dono do numero e' o gameplay e ele troca
## de instancia a cada partida — is_valid() cobre o player velho.
var cd_frac := Callable()
## Segundos cheios do cooldown, para o numero no meio do botao. Chega pela BORDA
## (Bus.kit_cooldown), nunca por frame.
var total_s := 0.0
## true = botao de CARGA (a suprema, 26/08): em vez dos segundos que faltam,
## mostra a PORCENTAGEM 0->100% que o Diretor pediu ("contador de porcentagem
## iniciando em 0% ate' chegar em 100%"). O arco continua o mesmo.
var mostra_carga := false
## false = APAGADO: 17 dos 20 magos ainda nao tem kit implementado. O botao
## aparece (o jogador ve que a habilidade existe) mas nao emite toque.
var ativo := true

var _touch := -1
var _last_frac := -1.0


func _ready() -> void:
	mouse_filter = MOUSE_FILTER_IGNORE


func _input(e: InputEvent) -> void:
	if not visible:
		return  # botao escondido (PEGAR sem loot perto) nao rouba o toque
	if e is InputEventScreenTouch:
		if e.pressed and _touch == -1 and get_global_rect().has_point(e.position):
			_touch = e.index
			if ativo:
				tocado.emit()
			queue_redraw()
		elif not e.pressed and e.index == _touch:
			_touch = -1


func _process(_delta: float) -> void:
	if not visible:
		return
	var f := _frac()
	if not is_equal_approx(f, _last_frac):
		queue_redraw()  # anima o cooldown so' quando muda


func _frac() -> float:
	if cd_frac.is_valid():
		return clampf(float(cd_frac.call()), 0.0, 1.0)
	return 0.0


func _draw() -> void:
	var c := size / 2.0
	var r := minf(size.x, size.y) / 2.0 - Dp.px(3.0)
	var frac := _frac()
	_last_frac = frac
	var pronto := frac <= 0.0 and ativo
	draw_circle(c + Vector2(Dp.px(1.5), Dp.px(2.5)), r, Color(0, 0, 0, 0.20))
	var base := cor if ativo else Color(0.55, 0.55, 0.58)
	draw_circle(c, r, Color(base, 0.36 if pronto else 0.18))
	## O disco (alvo do dedo) continua REDONDO sempre; quem muda e' o contorno.
	## Mexer no alvo por raridade encolheria a area de toque do triangulo.
	var borda := Color(1, 1, 1, 0.56 if ativo else 0.28)
	var lados := contorno_lados(forma)
	if lados == 0:
		draw_arc(c, r, 0, TAU, 40, borda, Dp.px(1.6), true)
	else:
		draw_polyline(contorno(c, r, lados), borda, Dp.px(2.0), true)
	if frac > 0.0:
		draw_arc(c, r * 0.62, -PI / 2.0, -PI / 2.0 + TAU * frac, 32,
				Color(0, 0, 0, 0.55), Dp.px(10.0), true)
	var font := get_theme_default_font()
	var fs := maxi(int(Dp.px(11.0)), 8)
	var alfa := 0.90 if ativo else 0.55
	if rotulo != "":
		draw_string(font, Vector2(0, c.y + fs * 0.35), rotulo,
				HORIZONTAL_ALIGNMENT_CENTER, size.x, fs, Color(1, 1, 1, alfa))
	# Carga: a porcentagem QUE JA' ENCHEU (100 - falta). Cooldown: os segundos
	# que faltam — le mais rapido que o arco quando o cooldown e' longo.
	if frac > 0.0 and mostra_carga:
		var pct := int(floorf((1.0 - frac) * 100.0))
		draw_string(font, Vector2(0, c.y - fs * 0.9), str(pct) + "%",
				HORIZONTAL_ALIGNMENT_CENTER, size.x, fs, Color(1, 1, 1, 0.95))
	elif frac > 0.0 and total_s > 0.0:
		var s := int(ceilf(frac * total_s))
		draw_string(font, Vector2(0, c.y - fs * 0.9), str(s),
				HORIZONTAL_ALIGNMENT_CENTER, size.x, fs, Color(1, 1, 1, 0.95))
	if subtitulo != "":
		var sfs := maxi(int(Dp.px(8.0)), 7)
		draw_string(font, Vector2(-Dp.px(8.0), size.y + sfs * 1.1), subtitulo,
				HORIZONTAL_ALIGNMENT_CENTER, size.x + Dp.px(16.0), sfs,
				Color(1, 1, 1, 0.62 if ativo else 0.35))


## Quantos lados a forma da raridade tem (0 = circulo, o desenho padrao).
## Publica porque o selftest cobra daqui: assim nao existe uma versao "de
## teste" da regra divergente da desenhada.
static func contorno_lados(forma_: String) -> int:
	match forma_:
		"triangulo":
			return 3
		"losango":
			return 4
		_:
			return 0


## Poligono fechado inscrito no raio do botao, com um VERTICE PARA CIMA (e' o
## que faz o losango parecer losango e nao um quadrado).
static func contorno(centro: Vector2, raio: float, lados: int) -> PackedVector2Array:
	var pts := PackedVector2Array()
	for i in lados + 1:
		var a := -PI / 2.0 + TAU * float(i % lados) / float(lados)
		pts.append(centro + Vector2(cos(a), sin(a)) * raio)
	return pts
