// Toon do Mage — bandas duras + rim, coerente com a regua Spellbreak (PRISMA §1).
// `tint`/`tint_mix`: os bots reusam o modelo com outra cor via Mage.set_tint().
shader_type spatial;
render_mode depth_draw_opaque, cull_back;

uniform vec3 albedo : source_color = vec3(0.8, 0.8, 0.8);
uniform vec3 tint : source_color = vec3(1.0, 1.0, 1.0);
uniform float tint_mix : hint_range(0.0, 1.0) = 0.0;
uniform vec3 emission_color : source_color = vec3(0.0, 0.0, 0.0);
uniform float emission_energy : hint_range(0.0, 8.0) = 0.0;
uniform float rim_strength : hint_range(0.0, 1.0) = 0.18;

void fragment() {
	vec3 base = mix(albedo, tint, tint_mix);
	ALBEDO = base;
	float rim = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 3.0);
	EMISSION = emission_color * emission_energy + base * rim * rim_strength;
}

void light() {
	float ndl = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
	// 3 bandas duras: sombra / meia-luz / luz
	float band = ndl > 0.55 ? 1.0 : (ndl > 0.18 ? 0.62 : 0.34);
	DIFFUSE_LIGHT += band * ALBEDO * LIGHT_COLOR / PI;
}
