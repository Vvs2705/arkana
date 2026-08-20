// world/toon.gdshader — cel-shading mobile (dono: raia MUNDO, R16/G1)
// Bandas DURAS de luz + rim dourado. ALBEDO vem da COR DE VERTICE que o
// Island.gd pinta (paleta GDD §10) — zero textura, zero binario.
// ponytail: sem outline (inverted hull dobraria draw calls no mobile);
// a banda + rim ja vendem o toon. Outline entra se o aparelho sobrar fps.
shader_type spatial;
render_mode cull_back, depth_draw_opaque, specular_disabled;

uniform float bands : hint_range(2.0, 6.0) = 3.0;
uniform float rim_strength : hint_range(0.0, 1.0) = 0.22;
uniform vec4 rim_color : source_color = vec4(1.0, 0.85, 0.55, 1.0); // dourado arcano #F0C75E

void fragment() {
	ALBEDO = COLOR.rgb;
	ROUGHNESS = 1.0;
}

void light() {
	float ndl = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
	float band = clamp(floor(ndl * bands) / (bands - 1.0), 0.0, 1.0);
	DIFFUSE_LIGHT += band * LIGHT_COLOR * ALBEDO / PI;
	// rim so' no lado iluminado — silhueta quente de fim de tarde
	float rim = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 3.0);
	DIFFUSE_LIGHT += rim * rim_strength * band * rim_color.rgb;
}
