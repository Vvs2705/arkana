// Arkana/Toon — cel-shading HIBRIDO do mundo (terreno, arvores, rochas, ruinas). Releitura do
// mobile-godot/godot/world/toon.gdshader, nao traducao:
//   * cor de VERTICE e' a cor (bioma x AO ja' multiplicados pela Ilha); o alfa e' o PESO DE ROCHA.
//   * wrap antes de bandar (o lado escuro tem forma) + degrau MACIO (sem serrilha piscando no celular).
//   * sombra com COR FRIA (clima com cor, nunca falta de luz) e rim dos dois lados.
//   * textura de detalhe CINZA em espaco de MUNDO: tira a cara de plastico sem UV no terreno.
// Tudo que e' opcional (mancha, grao, textura, sheen) e' branch em UNIFORM: coerente no warp, quem
// nao liga nao paga. Ambiente pelas 3 cores que o URP escreve por camera (unity_Ambient*), nao pelo
// probe SH: nao depende de o probe ser recalculado em runtime.
Shader "Arkana/Toon"
{
    Properties
    {
        _Faixas("Faixas", Range(2, 6)) = 3
        _FaixaMacia("Borda da faixa", Range(0.001, 0.4)) = 0.12
        _Wrap("Wrap (half-lambert)", Range(0, 0.6)) = 0.26
        _Rim("Rim", Range(0, 1)) = 0.22
        _CorRim("Cor do rim (sol)", Color) = (1, 0.85, 0.55, 1)
        _CorSombra("Cor da sombra (frio)", Color) = (0.40, 0.50, 0.82, 1)
        _Frio("Frio na sombra", Range(0, 0.4)) = 0.11
        _Brilho("Sheen duro (pedra)", Range(0, 1)) = 0
        _BrilhoDuro("Aperto do sheen", Range(4, 64)) = 22
        _Mancha("Mancha grande", Range(0, 0.5)) = 0
        _EscalaMancha("Escala da mancha (1/m)", Float) = 0.35
        _Grao("Oitava de perto", Range(0, 0.4)) = 0
        _EscalaGrao("Escala do grao (1/m)", Float) = 0.65
        [NoScaleOffset] _TexChao("Detalhe do chao (cinza)", 2D) = "white" {}
        [NoScaleOffset] _TexRocha("Detalhe da rocha (cinza)", 2D) = "white" {}
        _ForcaTextura("Forca da textura", Range(0, 1)) = 0
        _EscalaTextura("Escala da textura (1/m)", Float) = 0.22
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // O MESMO cbuffer em todos os passes: e' o que mantem o shader compativel com o SRP Batcher.
        CBUFFER_START(UnityPerMaterial)
            float _Faixas;
            float _FaixaMacia;
            float _Wrap;
            float _Rim;
            float4 _CorRim;
            float4 _CorSombra;
            float _Frio;
            float _Brilho;
            float _BrilhoDuro;
            float _Mancha;
            float _EscalaMancha;
            float _Grao;
            float _EscalaGrao;
            float _ForcaTextura;
            float _EscalaTextura;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_TexChao);
            SAMPLER(sampler_TexChao);
            TEXTURE2D(_TexRocha);
            SAMPLER(sampler_TexRocha);

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variantes
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                float fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // hash + value noise (1 hash por canto). Hash SEM seno (Hoskins): o frac(sin(x)*43758) do Godot, com as
            // coordenadas de mundo (+-300 m), dava hash diferente pro mesmo canto visto de duas celulas = costura reta.
            float H21(float2 p)
            {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Ruido(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(H21(i), H21(i + float2(1.0, 0.0)), f.x),
                            lerp(H21(i + float2(0.0, 1.0)), H21(i + float2(1.0, 1.0)), f.x), f.y);
            }

            // ambiente em 3 cores (ceu/horizonte/chao) — o URP escreve unity_Ambient* no espaco de cor ativo
            float3 Ambiente(float3 n)
            {
                float3 a = lerp(unity_AmbientEquator.rgb, unity_AmbientSky.rgb, saturate(n.y));
                return lerp(a, unity_AmbientGround.rgb, saturate(-n.y));
            }

            Variantes Vert(Atributos v)
            {
                Variantes o = (Variantes)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float4 Frag(Variantes i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 col = i.color.rgb;
                float3 n = normalize(i.normalWS);
                float2 xz = i.positionWS.xz;

                if (_Mancha > 0.0)
                {
                    // a mancha puxa o MATIZ pro capim seco, nao so' escurece (escurecer lia como sujeira)
                    float m = Ruido(xz * _EscalaMancha);
                    col = lerp(col, col * float3(1.06, 0.88, 0.62), m * _Mancha * 2.0);
                }
                if (_Grao > 0.0)
                {
                    // duas amostras defasadas: uma so' desenha xadrez com a camera rente ao chao
                    float d = Ruido(xz * _EscalaGrao) * 0.65 + Ruido(xz * _EscalaGrao * 2.7 + 31.0) * 0.35;
                    col *= lerp(1.0 - _Grao, 1.0 + _Grao, d);
                }
                if (_ForcaTextura > 0.0)
                {
                    // CHAO: duas escalas do mesmo ladrilho, defasadas (a 200 m o ladrilho de 4,5 m vira xadrez)
                    float2 uv = xz * _EscalaTextura;
                    float g = SAMPLE_TEXTURE2D(_TexChao, sampler_TexChao, uv).r * 0.65
                            + SAMPLE_TEXTURE2D(_TexChao, sampler_TexChao, uv * 0.37 + 0.5).r * 0.35;
                    // ROCHA: triplanar — na encosta a projecao XZ estica em tiras; cada plano pesa pela normal
                    float3 w = n * n;
                    w = w * w;
                    w /= max(w.x + w.y + w.z, 1e-4);
                    float3 pr = i.positionWS * (_EscalaTextura * 1.7);
                    float r = SAMPLE_TEXTURE2D(_TexRocha, sampler_TexRocha, pr.zy).r * w.x
                            + SAMPLE_TEXTURE2D(_TexRocha, sampler_TexRocha, pr.xz).r * w.y
                            + SAMPLE_TEXTURE2D(_TexRocha, sampler_TexRocha, pr.xy).r * w.z;
                    // peso de rocha: o alfa do vertice (inclinacao AMPLA + altitude, a mesma conta da cor)
                    // e, fraco, a normal do pixel — a encosta curta que a conta ampla nao pega
                    float pesoRocha = saturate(max(i.color.a, (1.0 - smoothstep(0.6, 0.85, n.y)) * 0.7));
                    float t = lerp(g, r, pesoRocha);
                    // a textura foi importada LINEAR (ImportacaoArkana); o Godot a lia como sRGB. O quadrado
                    // devolve o contraste que os numeros do Godot (x1,35, forca 0,55) calibraram.
                    t *= t;
                    col *= lerp(1.0, t * 1.35, _ForcaTextura);
                }

                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light luz = GetMainLight(sc);
                float atten = luz.shadowAttenuation * luz.distanceAttenuation;
                // wrap: o terminador desliza pro lado escuro em vez de cortar em seco
                float d0 = saturate((dot(n, luz.direction) + _Wrap) / (1.0 + _Wrap)) * atten;
                // degrau macio: floor() da' a banda, smoothstep(frac) tira o serrilhado
                float passos = max(_Faixas - 1.0, 1.0);
                float s = d0 * passos;
                float faixa = saturate((floor(s) + smoothstep(0.0, _FaixaMacia, frac(s))) / passos);

                float3 luzTotal = faixa * luz.color
                                + (1.0 - faixa) * _Frio * _CorSombra.rgb    // a sombra ganha cor propria
                                + Ambiente(n);
                float3 cor = col * luzTotal;

                // rim dos dois lados: quente no sol, frio na sombra (somado fora do albedo, como no Godot)
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float fres = pow(1.0 - saturate(dot(n, V)), 3.0);
                float3 rc = lerp(_CorSombra.rgb, _CorRim.rgb, faixa);
                cor += fres * _Rim * rc * (0.3 + 0.7 * faixa);

                if (_Brilho > 0.0)
                {
                    // sheen de borda dura no DIFUSO: vende pedra sem ler cubemap por pixel
                    float3 h = SafeNormalize(luz.direction + V);
                    float sp = pow(max(dot(n, h), 0.0), _BrilhoDuro);
                    cor += smoothstep(0.3, 0.45, sp) * _Brilho * luz.color * atten;
                }

                // nevoa POR PIXEL (a do URP): o fator por vertice interpola errado em triangulo grande
                // (o mar de quads de 100 m chegava ao far com cor de mar, cortado contra o ceu)
                cor = MixFog(cor, InitializeInputDataFog(float4(i.positionWS, 1.0), i.fog));
                return float4(cor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Simple Lit"
}
