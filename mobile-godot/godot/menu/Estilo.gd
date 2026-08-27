## PELE DAS TELAS DE MENU — paleta do GDD par.10 + os 3 widgets que as tres
## telas (Menu, Selecao, Config) compartilham. Existe para o botao do menu, o
## botao da selecao e o botao de config serem O MESMO botao: quando o Diretor
## mudar o dourado, muda num arquivo.
##
## Tudo procedural (StyleBoxFlat), zero binario — regra da raia.
## Toda altura de alvo de toque passa por Dp.px (ui/Dp.gd).
class_name Estilo

const NOITE_FUNDA := Color("#05070F")
const NOITE := Color("#0B1026")
const PAINEL := Color("#0E1430")
const PAINEL_ALTO := Color("#101838")
const OURO := Color("#F0C75E")
const OURO_FOSCO := Color("#8A7336")
const RAIO := Color("#F5D90A")  # o "K" da wordmark (GDD par.10)
const TEXTO := Color("#E8E6F0")
const TEXTO_FOSCO := Color("#9A97AD")

## Menor alvo que o dedo pode tocar (GDD par.19.3). Em dp, sempre.
const ALVO_DP := 48.0


static func rotulo(texto: String, tam: int, cor: Color) -> Label:
	var l := Label.new()
	l.text = texto
	l.add_theme_font_size_override("font_size", tam)
	l.add_theme_color_override("font_color", cor)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l


## Paragrafo que quebra linha sozinho (perfil de mago, notas de config).
static func paragrafo(texto: String, tam: int, cor: Color) -> Label:
	var l := rotulo(texto, tam, cor)
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	return l


## Botao de menu. min_dp vira px de canvas via Dp; nunca abaixo do proprio
## valor em px (tela pequena com dpi baixo nao pode encolher o alvo).
static func botao(texto: String, nome: String, min_dp := ALVO_DP,
		largura := 340.0, fonte := 28) -> Button:
	var b := Button.new()
	b.name = nome
	b.text = texto
	b.focus_mode = Control.FOCUS_NONE
	b.custom_minimum_size = Vector2(largura, altura_alvo(min_dp))
	b.add_theme_font_size_override("font_size", fonte)
	b.add_theme_color_override("font_color", OURO)
	b.add_theme_color_override("font_hover_color", Color("#FFE9A8"))
	b.add_theme_color_override("font_pressed_color", NOITE_FUNDA)
	var normal := StyleBoxFlat.new()
	normal.bg_color = PAINEL_ALTO
	normal.border_color = Color(OURO_FOSCO, 0.8)
	normal.set_border_width_all(2)
	normal.set_corner_radius_all(12)
	b.add_theme_stylebox_override("normal", normal)
	var hover: StyleBoxFlat = normal.duplicate()
	hover.border_color = OURO
	b.add_theme_stylebox_override("hover", hover)
	var pressed: StyleBoxFlat = normal.duplicate()
	pressed.bg_color = OURO
	pressed.border_color = OURO
	b.add_theme_stylebox_override("pressed", pressed)
	return b


## Botao invisivel que cobre outro no' (card, tela de titulo): o alvo de toque
## e' a area inteira, sem desenhar nada por cima da arte.
static func botao_invisivel(nome: String) -> Button:
	var b := Button.new()
	b.name = nome
	b.flat = true
	b.focus_mode = Control.FOCUS_NONE
	for st in ["normal", "hover", "pressed", "focus"]:
		b.add_theme_stylebox_override(st, StyleBoxEmpty.new())
	return b


static func moldura(forte := true) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = PAINEL
	sb.border_color = Color(OURO_FOSCO, 0.6 if forte else 0.35)
	sb.set_border_width_all(1)
	sb.set_corner_radius_all(10)
	sb.set_content_margin_all(8)
	return sb


static func altura_alvo(dp := ALVO_DP) -> float:
	return maxf(Dp.px(dp), dp)
