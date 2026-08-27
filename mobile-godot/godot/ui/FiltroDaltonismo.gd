## FILTRO DE DALTONISMO — a opcao "Modo daltonismo" do GDD par.12 virando pixel.
##
## POR QUE UM FILTRO DE TELA: a lei do projeto (GDD par.10) e' "forma, nunca so'
## cor" — o carrossel ja' desenha gota/crescente/zigzag/pedra/espiral. Mas forma
## resolve ICONE, nao resolve a arena: fogo laranja contra terra ocre, sangue na
## barra de vida, aliado x inimigo. Para isso so' um passe de tela resolve.
##
## O QUE ELE FAZ: daltonizacao (nao simulacao). Simula como o jogador VE, mede o
## que se perdeu no caminho, e devolve essa diferenca nos canais que ele AINDA
## enxerga. Duas cores que colapsavam numa so' voltam a se separar.
##
## CUSTO — o teto conhecido: hint_screen_texture forca uma copia do framebuffer
## por quadro. No renderer mobile isso nao e' de graca. Por isso a HUD/Config so'
## CRIAM este no' quando o modo != Nenhum: quem nao usa paga zero.
class_name FiltroDaltonismo
extends CanvasLayer

## Indices da lista Textos.CFG_DALTONISMOS: 0 Nenhum, 1 Prot, 2 Deut, 3 Trit.
var modo := 0:
	set(v):
		modo = v
		if _rect != null:
			_rect.material.set_shader_parameter("modo", v)
			_rect.visible = v != 0

var _rect: ColorRect

const CODIGO := "
shader_type canvas_item;
render_mode unshaded;

uniform int modo = 0;
uniform sampler2D tela : hint_screen_texture, filter_nearest;

void fragment() {
	vec3 c = texture(tela, SCREEN_UV).rgb;
	// sim = como o olho daltonico ve; desvio = para onde jogar o erro.
	mat3 sim = mat3(1.0);
	mat3 desvio = mat3(0.0);
	if (modo == 1) {  // protanopia (sem cone L)
		sim = mat3(vec3(0.152286, 0.114503, -0.003882),
		           vec3(1.052583, 0.786281, -0.048116),
		           vec3(-0.204868, 0.099216, 1.051998));
		desvio = mat3(vec3(0.0, 0.7, 0.7), vec3(0.0, 1.0, 0.0), vec3(0.0, 0.0, 1.0));
	} else if (modo == 2) {  // deuteranopia (sem cone M)
		sim = mat3(vec3(0.367322, 0.280085, -0.011820),
		           vec3(0.860646, 0.672501, 0.042940),
		           vec3(-0.227968, 0.047413, 0.968881));
		desvio = mat3(vec3(0.0, 0.7, 0.7), vec3(0.0, 1.0, 0.0), vec3(0.0, 0.0, 1.0));
	} else if (modo == 3) {  // tritanopia (sem cone S)
		sim = mat3(vec3(1.255528, -0.078411, 0.004733),
		           vec3(-0.076749, 0.930809, 0.691367),
		           vec3(-0.178779, 0.147602, 0.303900));
		desvio = mat3(vec3(1.0, 0.0, 0.0), vec3(0.0, 1.0, 0.0), vec3(0.7, 0.7, 0.0));
	}
	vec3 err = c - sim * c;
	COLOR = vec4(clamp(c + desvio * err, vec3(0.0), vec3(1.0)), 1.0);
}
"


func _ready() -> void:
	# Acima da HUD (layer 10) e do menu (layer 0): corrigir a tela pela metade
	# seria pior que nao corrigir.
	layer = 50
	var sh := Shader.new()
	sh.code = CODIGO
	var mat := ShaderMaterial.new()
	mat.shader = sh
	_rect = ColorRect.new()
	_rect.name = "Passe"
	_rect.material = mat
	_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE  # nao rouba um toque sequer
	_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_rect.anchor_right = 1.0
	_rect.anchor_bottom = 1.0
	add_child(_rect)
	modo = modo  # dispara o setter agora que o ColorRect existe
	add_to_group("config_ouvintes")


## Contrato do grupo "config_ouvintes" (ver menu/Config.gd): trocar o modo no
## menu vale na hora, sem reiniciar.
func aplicar_config(cfg: Dictionary) -> void:
	modo = int(cfg.get("jogo", {}).get("daltonismo", 0))
