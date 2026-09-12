// Arkana/Ceu — skybox do entardecer arcano (RenderSettings.skybox). Releitura do sky.gdshader:
// gradiente horizonte->topo, DISCO do sol duro + halo largo dourado (a direcao vem da luz principal
// que o URP ja' publica em _MainLightPosition), nuvens cartoon de 2 tons com borda dura e, novo aqui,
// a FAIXA DO HORIZONTE puxada pra cor da nevoa: o mar que a nevoa engole encosta no ceu sem costura.
// Zero textura.
Shader "Arkana/Ceu"
{
    Properties
    {
        _Topo("Topo", Color) = (0.13, 0.2, 0.46, 1)
        _Horizonte("Horizonte", Color) = (0.96, 0.73, 0.42, 1)
        _Nuvem("Nuvem", Color) = (1, 0.86, 0.72, 1)
        _NuvemSombra("Sombra da nuvem", Color) = (0.58, 0.48, 0.66, 1)
        _NevoaNoHorizonte("Nevoa no horizonte", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Topo;
                float4 _Horizonte;
                float4 _Nuvem;
                float4 _NuvemSombra;
                float _NevoaNoHorizonte;
            CBUFFER_END

            struct Atributos
            {
                float4 positionOS : POSITION;
            };

            struct Variantes
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            float H21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Ruido(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(H21(i), H21(i + float2(1.0, 0.0)), f.x),
                            lerp(H21(i + float2(0.0, 1.0)), H21(i + float2(1.0, 1.0)), f.x), f.y);
            }

            Variantes Vert(Atributos v)
            {
                Variantes o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;   // a malha do skybox e' centrada na camera: vertice = direcao
                return o;
            }

            float4 Frag(Variantes i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = saturate(d.y);
                float3 col = lerp(_Horizonte.rgb, _Topo.rgb, pow(up, 0.55));

                // sol: disco duro + halo largo (em HDR o disco estoura; em LDR vira branco)
                float sund = max(dot(d, _MainLightPosition.xyz), 0.0);
                col += float3(1.0, 0.9, 0.7) * step(0.9993, sund) * 2.5;
                col += float3(1.0, 0.75, 0.45) * pow(sund, 9.0) * 0.4;

                // nuvens: projecao no plano, 2 oitavas, corte duro = cartoon
                if (d.y > 0.02)
                {
                    float2 uv = d.xz / (d.y + 0.18) * 1.6 + float2(_Time.y * 0.008, 0.0);
                    float n = Ruido(uv) * 0.65 + Ruido(uv * 2.7 + 13.0) * 0.35;
                    float m = smoothstep(0.57, 0.61, n);
                    float nucleo = smoothstep(0.63, 0.8, n);
                    float3 cl = lerp(_Nuvem.rgb, _NuvemSombra.rgb, nucleo * 0.55);
                    col = lerp(col, cl, m * smoothstep(0.02, 0.14, d.y) * 0.85);
                }

                // faixa do horizonte na cor da nevoa: o mar nevoado encosta no ceu sem linha. ABAIXO do
                // horizonte nao existe "chao" de skybox: e' o mar alem do far, que a nevoa ja' engoliu —
                // do castelo (320 m) o mar acaba no far a ~18 graus abaixo da linha do horizonte.
                float faixa = (1.0 - smoothstep(0.0, 0.12, abs(d.y))) * _NevoaNoHorizonte;
                float abaixo = 1.0 - smoothstep(-0.03, 0.0, d.y);
                col = lerp(col, unity_FogColor.rgb, max(faixa, abaixo));
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Skybox/Procedural"
}
