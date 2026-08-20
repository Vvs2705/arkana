// world/water.gdshader — agua estilizada animada (mar, lago, alagado)
// Dono: raia MUNDO. Ondas no vertice + bandas de cor no fragmento.
// unshaded: agua nao recebe sombra — barato e le'-se como cartoon.
shader_type spatial;
render_mode cull_back, depth_draw_opaque, unshaded;

uniform vec4 shallow_color : source_color = vec4(0.16, 0.65, 1.0, 1.0); // Agua GDD #2AA7FF
uniform vec4 deep_color : source_color = vec4(0.06, 0.30, 0.75, 1.0);
uniform float wave_height = 0.1;
uniform float alpha : hint_range(0.0, 1.0) = 0.88;

varying vec3 wpos;

void vertex() {
	wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	VERTEX.y += sin(TIME * 1.4 + wpos.x * 0.35 + wpos.z * 0.21) * wave_height
			+ cos(TIME * 0.9 + wpos.z * 0.44) * wave_height * 0.6;
}

void fragment() {
	// 3 tons em banda dura — o mesmo vocabulario do toon.gdshader
	float b1 = sin(wpos.x * 0.55 + wpos.z * 0.35 + TIME * 0.7) * 0.5 + 0.5;
	float band = step(0.5, b1) * 0.5 + step(0.82, b1) * 0.5;
	vec3 col = mix(deep_color.rgb, shallow_color.rgb, band);
	// crista de espuma esparsa
	float foam = step(0.965, sin(wpos.x * 0.9 - TIME * 1.1) * sin(wpos.z * 0.8 + TIME * 0.8) * 0.5 + 0.5);
	col = mix(col, vec3(0.92, 0.98, 1.0), foam * 0.7);
	ALBEDO = col;
	ALPHA = alpha;
}
