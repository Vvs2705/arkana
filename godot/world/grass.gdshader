// world/grass.gdshader — tufos de grama e flores em MultiMesh (G2).
// Milhares de instancias por 1 draw call; vento no vertice; colapso por
// distancia (LOD gratis: longe a lamina afunda e some do fill rate).
// O gradiente base->ponta vem baked na cor de vertice, multiplicado pela tinta
// por-instancia (variacao de tom da campina).
//
// G4 — NORMAL ACHATADA PRA CIMA. Este shader NAO tem light() proprio: usa o
// difuso padrao. Com a normal real de cada lamina, as 3 faces de um tufo pegavam
// 3 brilhos diferentes e o tufo lia como um OBJETO facetado plantado no chao —
// no render de calibragem a campina virou um campo de cones de transito. Grama
// tem que ler como TEXTURA do chao, e chao aponta pra cima. Puxando 80% da
// normal pro UP, o tufo recebe a mesma luz que o terreno embaixo dele e se funde.
// De quebra o vento deixa de cintilar (a normal parava de girar com a lamina).
// Custo: um mix no vertex shader. Bonus: a lamina fina de agora tem 0,17m de
// largura, entao o serrilhado da silhueta tambem some.
shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, shadows_disabled;

uniform float sway = 0.09;
uniform float fade_start = 42.0;
uniform float fade_end = 56.0;
uniform float blade_height = 0.7;

void vertex() {
	float t = clamp(VERTEX.y / blade_height, 0.0, 1.0);
	vec3 iw = MODEL_MATRIX[3].xyz; // posicao da instancia no mundo
	float d = distance(iw, INV_VIEW_MATRIX[3].xyz);
	VERTEX.y *= 1.0 - smoothstep(fade_start, fade_end, d);
	VERTEX.x += sin(TIME * 1.9 + iw.x * 0.8 + iw.z * 0.6) * sway * t;
	VERTEX.z += cos(TIME * 1.3 + iw.x * 0.5) * sway * 0.6 * t;
	NORMAL = normalize(mix(NORMAL, vec3(0.0, 1.0, 0.0), 0.8));
}

void fragment() {
	ALBEDO = COLOR.rgb;
	ROUGHNESS = 1.0;
}
