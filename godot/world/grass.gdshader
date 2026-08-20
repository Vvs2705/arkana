// world/grass.gdshader — tufos de grama e flores em MultiMesh (G2).
// Milhares de instancias por 1 draw call; vento no vertice; colapso por
// distancia (LOD gratis: longe a lamina afunda e some do fill rate).
// unshaded: o gradiente base->ponta ja vem baked na cor de vertice,
// multiplicado pela tinta por-instancia (variacao de tom da campina).
shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, unshaded;

uniform float sway = 0.09;
uniform float fade_start = 46.0;
uniform float fade_end = 60.0;

void vertex() {
	float t = clamp(VERTEX.y / 0.55, 0.0, 1.0);
	vec3 iw = MODEL_MATRIX[3].xyz; // posicao da instancia no mundo
	float d = distance(iw, INV_VIEW_MATRIX[3].xyz);
	VERTEX.y *= 1.0 - smoothstep(fade_start, fade_end, d);
	VERTEX.x += sin(TIME * 1.9 + iw.x * 0.8 + iw.z * 0.6) * sway * t;
	VERTEX.z += cos(TIME * 1.3 + iw.x * 0.5) * sway * 0.6 * t;
}

void fragment() {
	ALBEDO = COLOR.rgb;
	ROUGHNESS = 1.0;
}
