// world/toon.gdshader — cel-shading HIBRIDO mobile (dono: raia MUNDO, G3)
// Continua toon (bandas legiveis em combate, GDD §10) mas deixou de ser CHAPADO:
//   1. wrap (half-lambert) antes de bandar -> o lado escuro tem forma, nao e' um
//      recorte preto; a leitura de volume aparece sem custo nenhum.
//   2. degrau com borda MACIA -> mesma silhueta de banda, sem serrilha piscando
//      no celular (aliasing de step() e' o que mais denuncia "jogo barato").
//   3. sombra com COR FRIA (regra da ponte: clima com COR, nunca falta de luz).
//   4. rim dos DOIS lados: quente no sol, frio na sombra — o mago nunca some
//      contra o fundo, que e' requisito de combate, nao capricho.
//   5. sheen banda-dura opcional (pedra/ruina) e manchas de ruido opcionais
//      (terreno) — ambos DESLIGADOS por padrao, so' o material que precisa paga.
// ORCAMENTO: sem textura, sem depth/screen texture, sem specular ambiente
// (specular_disabled continua ligado — o brilho e' fake, somado no difuso).
// ponytail: sem outline (inverted hull dobraria draw calls no mobile);
// a banda + rim ja vendem o toon. Outline entra se o aparelho sobrar fps.
shader_type spatial;
render_mode cull_back, depth_draw_opaque, specular_disabled;

uniform float bands : hint_range(2.0, 6.0) = 3.0;
uniform float band_soft : hint_range(0.001, 0.4) = 0.12;   // largura do degrau
uniform float wrap : hint_range(0.0, 0.6) = 0.26;          // half-lambert
uniform float rim_strength : hint_range(0.0, 1.0) = 0.22;
uniform vec4 rim_color : source_color = vec4(1.0, 0.85, 0.55, 1.0); // dourado #F0C75E
uniform vec4 shade_tint : source_color = vec4(0.40, 0.50, 0.82, 1.0); // azul-noite #0B1026 aberto
uniform float shade_fill : hint_range(0.0, 0.4) = 0.11;    // quanto de frio entra na sombra
uniform float sheen : hint_range(0.0, 1.0) = 0.0;          // brilho duro (pedra/agua)
uniform float sheen_sharp : hint_range(4.0, 64.0) = 22.0;
uniform float macro_noise : hint_range(0.0, 0.5) = 0.0;    // manchas (terreno)
uniform float macro_scale = 0.35;
// OITAVA DE PERTO (27/08). O chao tinha SO' a mancha grande (macro_scale 0.19 =
// onda de ~5 m), e a 2 m do olho isso e' uma superficie lisa: o render de dentro
// do jogo mostrou campina de plastico. Esta oitava tem ~1,5 m e amplitude
// pequena — nao desenha nada que se note, quebra a chapadura que se nota.
// Custa UM hash a mais, e so' para quem liga (branch em uniform).
uniform float detail_noise : hint_range(0.0, 0.4) = 0.0;
uniform float detail_scale = 0.65;
// TEXTURA DE CHAO (27/08). O shader era "sem textura" por orcamento, e por isso
// o chao lia como PLASTICO: cor de vertice + uma oitava de hash nao fazem
// superficie. UMA amostra de textura em cinza, LADRILHADA em espaco de mundo
// (nao precisa de UV no terreno), resolve o que tres oitavas de ruido nao
// resolviam — e custa uma amostra, nao tres hashes.
//   * cinza, nao colorida: a COR continua vindo do vertice, entao toda a logica
//     de bioma (praia, lama, rocha, cume) segue valendo sem tocar em nada.
//   * duas texturas (chao e rocha) misturadas pelo MESMO peso de rocha que a
//     cor de vertice usa, passado em COLOR.a pelo gerador do terreno.
uniform sampler2D tex_chao : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D tex_rocha : source_color, filter_linear_mipmap, repeat_enable;
uniform float tex_forca : hint_range(0.0, 1.0) = 0.0;
uniform float tex_escala = 0.22;   // 1/m: 0,22 = ladrilho de ~4,5 m

varying vec3 wpos;

float h21(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float vnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(h21(i), h21(i + vec2(1, 0)), f.x),
		mix(h21(i + vec2(0, 1)), h21(i + vec2(1, 1)), f.x), f.y);
}

void vertex() {
	wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec3 col = COLOR.rgb;
	// Manchas grandes de terreno. 1 oitava so': a chapadura morre com muito
	// pouco, e a 2a oitava custaria o dobro de hash no fill rate do celular.
	// Branch em UNIFORM = coerente em todo o warp; quem nao usa nao paga.
	if (macro_noise > 0.0) {
		float n = vnoise(wpos.xz * macro_scale);
		// A mancha muda o MATIZ (mais seca/amarela), nao so' o brilho. Medido a
		// olho: so' escurecer desenhava borroes cinzentos que leem como sujeira
		// no chao; puxar pro amarelo le' como capim seco e some no conjunto.
		col = mix(col, col * vec3(1.06, 0.88, 0.62), n * macro_noise * 2.0);
	}
	if (detail_noise > 0.0) {
		// Duas amostras defasadas: uma so' desenha um xadrez visivel quando a
		// camera fica rente ao chao (o filtro do vnoise e' quadrado).
		float d = vnoise(wpos.xz * detail_scale) * 0.65
				+ vnoise(wpos.xz * detail_scale * 2.7 + 31.0) * 0.35;
		// So' BRILHO, nao matiz: matiz nesta escala vira sujeira. O terreno ja'
		// tem a variacao de cor na cor de vertice.
		col *= mix(1.0 - detail_noise, 1.0 + detail_noise, d);
	}
	if (tex_forca > 0.0) {
		// Duas escalas do MESMO ladrilho, defasadas: mata a repeticao visivel do
		// ladrilho de 4,5 m, que a 200 m de vista aparece como xadrez.
		vec2 uv = wpos.xz * tex_escala;
		float g = texture(tex_chao, uv).r * 0.65
				+ texture(tex_chao, uv * 0.37 + 0.5).r * 0.35;
		float r = texture(tex_rocha, uv * 1.7).r;
		// COLOR.a carrega o peso de rocha que o terreno ja' calculou por
		// inclinacao ampla e altura. Uma fonte para a cor e para a textura.
		float m = mix(g, r, COLOR.a);
		col *= mix(1.0, m * 1.35, tex_forca);
	}
	ALBEDO = col;
	ROUGHNESS = 1.0;
}

void light() {
	// wrap: o terminador desliza pro lado escuro em vez de cortar em seco
	float ndl = dot(NORMAL, LIGHT);
	float d = clamp((ndl + wrap) / (1.0 + wrap), 0.0, 1.0) * ATTENUATION;
	// degrau macio: floor() da a banda, smoothstep(fract) tira o serrilhado
	float steps = max(bands - 1.0, 1.0);
	float s = d * steps;
	float band = clamp((floor(s) + smoothstep(0.0, band_soft, fract(s))) / steps, 0.0, 1.0);

	DIFFUSE_LIGHT += band * LIGHT_COLOR * ALBEDO / PI;
	// preenchimento frio na parte NAO iluminada: a sombra ganha cor propria em
	// vez de ficar refem so' do ambiente do ceu (que a achata em cinza).
	DIFFUSE_LIGHT += (1.0 - band) * shade_fill * shade_tint.rgb * ALBEDO;

	// rim dos dois lados — quente onde bate sol, frio na sombra
	float fres = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 3.0);
	vec3 rc = mix(shade_tint.rgb, rim_color.rgb, band);
	DIFFUSE_LIGHT += fres * rim_strength * rc * (0.3 + 0.7 * band);

	// sheen: highlight de borda dura, somado no DIFUSO de proposito (nao acende
	// reflexo de ambiente, que no mobile custaria leitura de cubemap por pixel)
	if (sheen > 0.0) {
		float sp = pow(max(dot(NORMAL, normalize(LIGHT + VIEW)), 0.0), sheen_sharp);
		DIFFUSE_LIGHT += smoothstep(0.3, 0.45, sp) * sheen * LIGHT_COLOR * ATTENUATION;
	}
}
