// world/water.gdshader — agua estilizada animada (mar, lago, alagado)
// Dono: raia MUNDO. Ondas no vertice + bandas de cor no fragmento.
// unshaded: agua nao recebe sombra — barato e le'-se como cartoon.
// G2: reflexo FALSO do ceu (fresnel rasante puxa o dourado do entardecer,
// zero textura) + espuma de MARGEM via anel em COLOR.r (gravado pelo
// _disc_mesh) — nada de depth texture no mobile.
// G3: LOD de detalhe por distancia (aliasing de banda no horizonte + fill rate).
//
// G4 — TRES DEFEITOS MEDIDOS NO RENDER, todos com a mesma raiz: o padrao era
// UMA senoide de uma direcao so'.
//  1. LISTRA RETA. sin(x*a + z*b) desenha faixas paralelas infinitas. No lago
//     (36m, banda de 4,4m) davam 8 tarjas retas: o lago lia como toalha de
//     piquenique; no mar (520m) lia como papelao corrugado ate' o horizonte.
//     A 1a tentativa foi deslocar o dominio de um seno com o outro — e o
//     resultado, RENDERIZADO, foi um XADREZ de piscina: dois senos cruzados tem
//     eixo, e eixo sempre vira grade. A saida foi trocar a fonte do padrao por
//     value noise (mancha sem direcao) com um seno de marola por cima pesando 1/3.
//  2. LILAS. O fresnel era pow(...,2.5)*0.4: olhando o lago de cima (dot~0.35)
//     ele ainda injetava 14% de ceu dourado no azul do fundo, e dourado+azul da'
//     lilas. Agora pow 5 * 0.45: so' pinta no RASANTE, que e' onde reflexo existe.
//  3. AGUA SEM FUNDO. A cor nao dependia da profundidade — um disco de tom unico.
//     Agora o anel (`ring`, ja' gravado na malha) clareia a MARGEM: raso claro no
//     pe' do barranco, fundo escuro no meio. Custa 1 smoothstep e e' o que faz o
//     olho ler "isso tem profundidade".
// ORCAMENTO: 5 sin por pixel (era 3). O contraste de banda virou uniform: o MAR
// usa contraste baixo (e' fundo, nao pode competir com o combate). Se o fill rate
// do aparelho reclamar, o botao e' band_contrast=0 no mar.
shader_type spatial;
render_mode cull_back, depth_draw_opaque, unshaded;

uniform vec4 shallow_color : source_color = vec4(0.16, 0.65, 1.0, 1.0); // Agua GDD #2AA7FF
uniform vec4 deep_color : source_color = vec4(0.06, 0.30, 0.75, 1.0);
uniform vec4 sky_tint : source_color = vec4(0.95, 0.70, 0.42, 1.0); // horizonte entardecer
uniform float wave_height = 0.1;
uniform float alpha : hint_range(0.0, 1.0) = 0.88;
uniform float edge_foam = 0.0; // 1.0 nos discos (lago/alagado); 0 no mar
// Escala do padrao: a celula do value noise mede 1/(0.4*band_scale) metros.
// Mar 0.5 (celula de 5m, marola larga de fundo), lago 1.15 (2,2m), alagado 1.4.
uniform float band_scale = 1.0;
// Quanto a banda contrasta. Mar em 0.55: fundo de quadro nao pode gritar.
uniform float band_contrast : hint_range(0.0, 1.0) = 1.0;

varying vec3 wpos;
varying float ring;
varying float detail;   // 1 perto da camera, 0 no horizonte

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

// MEDIDO: dois senos cruzados — mesmo com deslocamento de dominio — desenham uma
// GRADE. A primeira tentativa do G4 trocou a listra reta por um xadrez de piscina,
// que e' pior: listra o olho perdoa, quadriculado ele nao. Padrao de agua precisa
// de algo SEM EIXO, e a fonte barata disso e' value noise (mancha irregular).
// Aqui: 1 value noise derivando devagar (a mancha) + 1 seno de marola direcional
// com 1/3 do peso (o "para onde a agua corre"). Custo: 5 sin/pixel — 1 a mais que
// a versao xadrez, 2 a mais que a listra original. Se o fill rate do aparelho
// reclamar, o botao e' band_contrast=0 no MAR (some o padrao, fica o gradiente).
float ripple(vec2 p, float t) {
	// dominio ROTACIONADO ~31 graus: value noise interpola numa grade, e grade
	// alinhada com os eixos do mundo desenha losangos alinhados com o mundo — a
	// agua ganhava um quadriculado sutil. Girar a amostragem custa 4 mults.
	vec2 q = vec2(p.x * 0.857 - p.y * 0.515, p.x * 0.515 + p.y * 0.857);
	float n = vnoise(q * 0.4 + vec2(t * 0.07, t * 0.045));
	float swell = sin(p.x * 0.62 + p.y * 0.28 + t * 0.8) * 0.5 + 0.5;
	return clamp(n * 0.68 + swell * 0.32, 0.0, 1.0);
}

void vertex() {
	wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	ring = COLOR.r;
	detail = 1.0 - smoothstep(55.0, 160.0, distance(wpos, INV_VIEW_MATRIX[3].xyz));
	VERTEX.y += (sin(TIME * 1.4 + wpos.x * 0.35 + wpos.z * 0.21) * wave_height
			+ cos(TIME * 0.9 + wpos.z * 0.44) * wave_height * 0.6) * detail;
}

void fragment() {
	float b = ripple(wpos.xz * band_scale, TIME);
	// 3 tons em banda dura — o mesmo vocabulario do toon.gdshader.
	// smoothstep e nao step: mesma banda de perto, sem o serrilhado que rasteja
	// quando a celula fica menor que o pixel.
	float band = smoothstep(0.38, 0.52, b) * 0.55 + smoothstep(0.62, 0.78, b) * 0.45;
	band = mix(0.5, band, detail * band_contrast);
	// margem RASA nos discos: `ring` e' 0 no centro e 1 na borda
	float shore = smoothstep(0.25, 1.0, ring) * edge_foam;
	vec3 col = mix(deep_color.rgb, shallow_color.rgb,
			clamp(band * 0.62 + shore * 0.42, 0.0, 1.0));
	// reflexo falso, so' no rasante (pow 5)
	float fr = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 5.0);
	col = mix(col, sky_tint.rgb, fr * 0.45);
	// Espuma de CRISTA tirada do proprio campo de onda: ela nasce onde a onda
	// esta' no pico, que e' onde ela existe.
	float foam = smoothstep(0.84, 0.96, b) * 0.5 * detail;
	// faixa de espuma na margem, respirando com o tempo — FINA (a antiga, de
	// 0.93 a 1.0 em branco puro, virava um aro de plastico em volta do lago)
	float wob = sin(TIME * 1.5 + wpos.x * 1.2 + wpos.z * 0.9) * 0.04;
	foam += smoothstep(0.965 + wob, 1.0 + wob, ring) * edge_foam;
	col = mix(col, mix(shallow_color.rgb, vec3(0.95, 0.99, 1.0), 0.6),
			clamp(foam, 0.0, 1.0) * 0.6);
	ALBEDO = col;
	// G5 — A BORDA FANTASMA. Diagnostico do render aereo: de 200 m aparecia um
	// LOSANGO nitido desenhado na agua, e ele nao vinha do mar — vinha de o mar ser
	// TRANSLUCIDO (alpha 0.88). Dentro do quadrado de SIZE metros existe terreno
	// submerso por baixo da lamina e os 12% que passam mostram o fundo; fora dele
	// nao existe malha nenhuma e os mesmos 12% mostram o ceu. O contorno do losango
	// era, literalmente, a BORDA DA MALHA DO TERRENO vista atraves da agua.
	// Ampliar o plano do mar ou o far da camera nao resolvia nada (foram testados):
	// o problema nunca foi onde o mar acaba, e sim que da' pra ver atraves dele.
	// `detail` ja' existe e ja' mede exatamente o que interessa (1 no pe' da camera,
	// 0 alem de 160 m). Perto o mar continua translucido — e' o que mostra a areia
	// molhada na beira; longe ele fecha e nao ha' o que vazar. Custo: 1 mix.
	ALPHA = mix(1.0, alpha, detail);
}
