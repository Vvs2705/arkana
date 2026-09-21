// Arkana/SintoniaZona — o CHAO e o CORPO dos combos da Sintonia (onda 18A): lava, lama, agua eletrica, a sombra da nuvem, o
// funil do tornado, a onda de choque e o aviso da canalizacao. Unlit transparente PRE-MULTIPLICADO: numa passada so' a crosta
// ESCURECE o chao (alfa) e o veio ACENDE (cor acima de 1 -> bloom do pos). Tudo por propriedade — o C# pinta cada zona por
// MaterialPropertyBlock — e ZERO keyword alem do fog: o "Strip Unused" nao tem variante para descartar. Entra no APK pelo
// material-asset Resources/ArkanaSintoniaZona.mat (teste EditMode confere).
// Dado no vertice (VisualDaSintonia escreve): COLOR.r = raio relativo (0 centro .. 1 borda; no funil, a altura 0..1),
// COLOR.a = alfa radial (a borda esmaece), UV0 = metros no plano (o ruido tem tamanho de MUNDO, nao de malha).
// Custo: 2 fbm de 3 oitavas (hash sem seno: o seno de argumento grande perde bits na GPU do celular) + bolha + anel.
Shader "Arkana/SintoniaZona"
{
    Properties
    {
        _Cor("Base (alfa = cobertura)", Color) = (0.1, 0.035, 0.02, 0.95)
        [HDR] _Brilho("Brilho: veio, onda, bolha, anel", Color) = (4.5, 1.6, 0.3, 1)
        _Escala("Ruido: frequencia (1/m)", Float) = 0.32
        _Rolagem("Ruido: rolagem xy e zw (m/s)", Vector) = (0.04, 0.015, -0.02, 0.03)
        _Veio("Veio: quanto, finura, manchas, miolo quente", Vector) = (1, 5, 0.9, 0.9)
        _Anel("Anel: raio, largura, forca, racha", Vector) = (0, 0.05, 0, 0)
        _Onda("Onda: quanto, frequencia, velocidade, bolhas", Vector) = (0, 0, 0, 0)
        _Enche("Enche: raio cheio, borda roida", Vector) = (2, 0.45, 0, 0)
        _Periodo("Ruido: periodo em x (funil; 0 = sem)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "SintoniaZona"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Cor;
                float4 _Brilho;
                float _Escala;
                float4 _Rolagem;
                float4 _Veio;
                float4 _Anel;
                float4 _Onda;
                float4 _Enche;
                float _Periodo;
            CBUFFER_END

            struct Atributos
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Variantes
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 dado : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fog : TEXCOORD3;
            };

            Variantes Vert(Atributos v)
            {
                Variantes o;
                float3 pw = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(pw);
                o.positionWS = pw;
                o.uv = v.uv;
                o.dado = float2(v.color.r, v.color.a);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // hash sem seno (Hoskins)
            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // ruido de valor; `per` > 0 fecha o x num periodo (o funil da a volta sem costura)
            float Ruido(float2 p, float per)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float x0 = i.x, x1 = i.x + 1.0;
                if (per > 0.0) { x0 -= per * floor(x0 / per); x1 -= per * floor(x1 / per); }
                float a = Hash(float2(x0, i.y)), b = Hash(float2(x1, i.y));
                float c = Hash(float2(x0, i.y + 1.0)), d = Hash(float2(x1, i.y + 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p, float per)
            {
                return (Ruido(p, per) * 0.5 + Ruido(p * 2.0, per * 2.0) * 0.25 + Ruido(p * 4.0, per * 4.0) * 0.125) / 0.875;
            }

            float4 Frag(Variantes i) : SV_Target
            {
                float t = _Time.y;
                float rad = i.dado.x;
                float fade = i.dado.y;
                float2 p = i.uv * _Escala;
                float n1 = Fbm(p + t * _Rolagem.xy, _Periodo);
                float n2 = Fbm(p + float2(17.3, 5.1) + t * _Rolagem.zw, _Periodo);

                // a borda ROIDA: o ruido come a borda (circulo de compasso le' como adesivo colado no chao)
                float e0 = (1.0 - n2) * _Enche.y;
                float m = smoothstep(e0, e0 + 0.3, fade);
                // o miolo que ENCHE (a canalizacao conta o tempo sem numero); 2 = cheio
                float cheio = 1.0 - smoothstep(_Enche.x - 0.03, _Enche.x + 0.03, rad);
                float cobre = m * cheio;

                // BASE: cor x manchas (crosta, lama, vapor), alfa = cobertura; pre-multiplicada
                float manchas = lerp(1.0, 0.35 + 1.3 * n2, _Veio.z);
                float a = saturate(_Cor.a * cobre);
                float3 cor = _Cor.rgb * manchas * a;

                // VEIO: a crista do ruido (onde n1 cruza 0,5) vira linha fina que acende; miolo quente onde n2 e' alto. pow com expoente
                // >= 1: pow(0,0) = exp2(0 x -inf) = NaN na GPU, e NaN vira quadrado preto no bloom
                float crista = saturate(1.0 - abs(n1 * 2.0 - 1.0));
                float veio = pow(crista, max(_Veio.y, 1.0)) * _Veio.x + smoothstep(0.62, 0.9, n2) * _Veio.w;
                // ONDAS que correm para fora (no funil: faixas que SOBEM)
                float onda = pow(saturate(sin(rad * _Onda.y - t * _Onda.z)), 6.0) * _Onda.x;
                // BOLHAS / RESPINGOS: celulas de ~0,9 m, cada uma estoura um anel que abre e apaga no seu tempo
                float2 q = i.uv * 1.1;
                float2 ci = floor(q);
                float h = Hash(ci + 11.7);
                float fase = frac(t * 0.7 + h * 7.0);
                float2 centro = float2(Hash(ci + 3.1), Hash(ci + 7.7)) * 0.5 + 0.25;
                float d = length(frac(q) - centro);
                float bolha = (1.0 - smoothstep(0.0, 0.05, abs(d - fase * 0.3))) * (1.0 - fase) * step(0.35, h) * _Onda.w;
                // ANEL: onda de choque / borda do aviso; `racha` abre quatro fendas (o "falhou")
                float x = (rad - _Anel.x) / max(_Anel.y, 0.001);
                float anel = exp(-x * x) * _Anel.z;
                float fenda = abs(frac(atan2(i.uv.y, i.uv.x + 1e-4) * 0.63661977 + 0.5) - 0.5);   // 4 fendas (2/pi); +1e-4: atan2(0,0) e' NaN no centro
                anel *= smoothstep(_Anel.w * 0.2, _Anel.w * 0.2 + 0.03, fenda + 1.0 - step(0.001, _Anel.w));

                float3 col = cor + _Brilho.rgb * ((veio + onda + bolha) * cobre + anel);
                float alfa = saturate(a + anel * 0.45);
                // nevoa: a base vai para a cor da nevoa x alfa (pre-multiplicado); o brilho apaga na distancia
                float fogF = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fog);
                col = MixFogColor(col, unity_FogColor.rgb * alfa, fogF);
                return float4(col, alfa);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
