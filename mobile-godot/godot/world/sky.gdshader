// world/sky.gdshader — ceu do entardecer arcano (dono: raia MUNDO, G2)
// Gradiente + SOL visivel com halo (HDR -> o glow acende) + nuvens
// estilizadas 2 tons com borda dura (mesmo vocabulario do toon.gdshader).
// Zero textura; radiance do Sky em 64 (barato p/ ambiente no mobile).
shader_type sky;

uniform vec3 top_color : source_color = vec3(0.13, 0.2, 0.46);
uniform vec3 horizon_color : source_color = vec3(0.96, 0.73, 0.42);
uniform vec3 cloud_color : source_color = vec3(1.0, 0.86, 0.72);
uniform vec3 cloud_shadow : source_color = vec3(0.58, 0.48, 0.66);

float vhash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float vnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(vhash(i), vhash(i + vec2(1, 0)), f.x),
		mix(vhash(i + vec2(0, 1)), vhash(i + vec2(1, 1)), f.x), f.y);
}

void sky() {
	float up = clamp(EYEDIR.y, 0.0, 1.0);
	vec3 col = mix(horizon_color, top_color, pow(up, 0.55));
	// sol: disco duro + halo largo dourado
	float sund = max(dot(EYEDIR, LIGHT0_DIRECTION), 0.0);
	col += vec3(1.0, 0.9, 0.7) * step(0.9993, sund) * 2.5;
	col += vec3(1.0, 0.75, 0.45) * pow(sund, 9.0) * 0.4;
	// nuvens: projecao no plano, 2 oitavas, corte duro = cartoon
	if (EYEDIR.y > 0.02) {
		vec2 uv = EYEDIR.xz / (EYEDIR.y + 0.18) * 1.6 + vec2(TIME * 0.008, 0.0);
		float n = vnoise(uv) * 0.65 + vnoise(uv * 2.7 + 13.0) * 0.35;
		float m = smoothstep(0.57, 0.61, n);
		float core = smoothstep(0.63, 0.8, n);
		vec3 cl = mix(cloud_color, cloud_shadow, core * 0.55);
		float fade = smoothstep(0.02, 0.14, EYEDIR.y);
		col = mix(col, cl, m * fade * 0.85);
	}
	COLOR = col;
}
