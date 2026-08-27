// Toon HIBRIDO do Mage — leitura de combate (bandas) + volume (rim, specular, AO).
// Renderer MOBILE: nada de SCREEN_TEXTURE/DEPTH_TEXTURE (nao existem la);
// so' custom light(), que o mobile suporta. Zero textura — tudo por uniform/vertice.
//
// `tint`/`tint_mix`: os bots reusam o modelo com outra cor via Mage.set_tint().
// Os quatro "materiais" (tecido/metal/pele/energia) sao PRESETS destes uniforms,
// definidos em Mage.gd (MAT_*) — um shader so', sem branch por tipo.
shader_type spatial;
render_mode depth_draw_opaque, cull_back;

uniform vec3 albedo : source_color = vec3(0.8, 0.8, 0.8);
uniform vec3 tint : source_color = vec3(1.0, 1.0, 1.0);
uniform float tint_mix : hint_range(0.0, 1.0) = 0.0;
uniform vec3 emission_color : source_color = vec3(0.0, 0.0, 0.0);
uniform float emission_energy : hint_range(0.0, 8.0) = 0.0;

// Rim: destaca a silhueta contra o fundo (exigencia de leitura em combate).
// Modulado por NORMAL.y para parecer luz do CEU, e nao contorno chapado.
uniform vec3 rim_color : source_color = vec3(1.0, 1.0, 1.0);
uniform float rim_strength : hint_range(0.0, 2.0) = 0.30;
uniform float rim_power : hint_range(1.0, 8.0) = 3.0;

// Specular Blinn-Phong. spec_tight=1 corta em degrau (brilho de metal de desenho),
// spec_tight=0 deixa o lobo macio (tecido/pele).
uniform float spec_amount : hint_range(0.0, 1.0) = 0.06;
uniform float spec_power : hint_range(2.0, 128.0) = 12.0;
uniform float spec_tight : hint_range(0.0, 1.0) = 0.0;

// Meia-lambert: o lado escuro nao morre em preto (pele/tecido enrolam mais que metal).
uniform float shade_wrap : hint_range(0.0, 0.8) = 0.25;
// AO assado no vertice (COLOR.r), vindo do _lathe. 0 desliga (energia magica nao ocluia).
uniform float ao_strength : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	float ao = mix(1.0, COLOR.r, ao_strength);
	vec3 base = mix(albedo, tint, tint_mix) * ao;
	ALBEDO = base;
	// fresnel; ceu = 1 em cima, ~0.2 embaixo -> o rim nasce no ombro, nao no pe
	float fres = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), rim_power);
	float sky = clamp(NORMAL.y * 0.4 + 0.62, 0.0, 1.0);
	EMISSION = emission_color * emission_energy + rim_color * fres * rim_strength * sky;
}

void light() {
	vec3 n = normalize(NORMAL);
	float ndl = dot(n, normalize(LIGHT));
	float w = clamp((ndl + shade_wrap) / (1.0 + shade_wrap), 0.0, 1.0);
	// 4 degraus de borda SUAVE: mantem a leitura de toon e devolve o volume que
	// o ternario de 3 bandas duras achatava. Soma exatamente 1.0 na luz cheia.
	float band = 0.20
		+ 0.26 * smoothstep(0.10, 0.22, w)
		+ 0.30 * smoothstep(0.40, 0.52, w)
		+ 0.24 * smoothstep(0.70, 0.82, w);
	DIFFUSE_LIGHT += band * ATTENUATION * ALBEDO * LIGHT_COLOR / PI;

	vec3 h = normalize(normalize(LIGHT) + normalize(VIEW));
	float s = pow(max(dot(n, h), 0.0), spec_power);
	s = mix(s, smoothstep(0.25, 0.32, s), spec_tight);
	SPECULAR_LIGHT += s * spec_amount * ATTENUATION * LIGHT_COLOR;
}
