// world/water.gdshader — agua estilizada animada (mar, lago, alagado)
// Dono: raia MUNDO. Ondas no vertice + bandas de cor no fragmento.
// unshaded: agua nao recebe sombra — barato e le'-se como cartoon.
// G2: reflexo FALSO do ceu (fresnel rasante puxa o dourado do entardecer,
// zero textura) + espuma de MARGEM via anel em COLOR.r (gravado pelo
// _disc_mesh) — nada de depth texture no mobile.
shader_type spatial;
render_mode cull_back, depth_draw_opaque, unshaded;

uniform vec4 shallow_color : source_color = vec4(0.16, 0.65, 1.0, 1.0); // Agua GDD #2AA7FF
uniform vec4 deep_color : source_color = vec4(0.06, 0.30, 0.75, 1.0);
uniform vec4 sky_tint : source_color = vec4(0.95, 0.70, 0.42, 1.0); // horizonte entardecer
uniform float wave_height = 0.1;
uniform float alpha : hint_range(0.0, 1.0) = 0.88;
uniform float edge_foam = 0.0; // 1.0 nos discos (lago/alagado); 0 no mar

varying vec3 wpos;
varying float ring;

void vertex() {
	wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	ring = COLOR.r;
	VERTEX.y += sin(TIME * 1.4 + wpos.x * 0.35 + wpos.z * 0.21) * wave_height
			+ cos(TIME * 0.9 + wpos.z * 0.44) * wave_height * 0.6;
}

void fragment() {
	// 3 tons em banda dura — o mesmo vocabulario do toon.gdshader
	float b1 = sin(wpos.x * 0.55 + wpos.z * 0.35 + TIME * 0.7) * 0.5 + 0.5;
	float band = step(0.5, b1) * 0.5 + step(0.82, b1) * 0.5;
	vec3 col = mix(deep_color.rgb, shallow_color.rgb, band);
	// reflexo falso: quanto mais rasante o olhar, mais o ceu dourado entra
	float fr = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 2.5);
	col = mix(col, sky_tint.rgb, fr * 0.4);
	// crista de espuma esparsa
	float foam = step(0.965, sin(wpos.x * 0.9 - TIME * 1.1) * sin(wpos.z * 0.8 + TIME * 0.8) * 0.5 + 0.5);
	// banda de espuma na margem, respirando com o tempo
	float wob = sin(TIME * 1.5 + wpos.x * 1.2 + wpos.z * 0.9) * 0.05;
	foam += smoothstep(0.84 + wob, 0.97 + wob, ring) * edge_foam;
	col = mix(col, vec3(0.93, 0.98, 1.0), clamp(foam, 0.0, 1.0) * 0.8);
	ALBEDO = col;
	ALPHA = alpha;
}
