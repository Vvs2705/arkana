// world/mist.gdshader — nevoa baixa do alagado (G2). Um disco translucido
// com 2 ondas de ruido barato; o alpha morre na borda (anel em COLOR.r,
// gravado pelo _disc_mesh do Island.gd). Clima com COR, nunca falta de luz:
// a nevoa e' clara-esverdeada (Vento GDD §10), nao escurece nada.
shader_type spatial;
render_mode cull_disabled, depth_draw_never, unshaded;

uniform vec4 color : source_color = vec4(0.78, 0.94, 0.9, 0.16);

varying vec3 wpos;
varying float ring;

void vertex() {
	wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	ring = COLOR.r;
	VERTEX.y += sin(TIME * 0.5 + wpos.x * 0.3) * 0.06;
}

void fragment() {
	float n = sin(wpos.x * 0.35 + TIME * 0.14) * sin(wpos.z * 0.28 - TIME * 0.1)
			+ 0.5 * sin(wpos.x * 0.9 - TIME * 0.07 + wpos.z * 0.6);
	ALBEDO = color.rgb;
	ALPHA = color.a * (0.55 + 0.3 * n) * (1.0 - smoothstep(0.5, 1.0, ring));
}
