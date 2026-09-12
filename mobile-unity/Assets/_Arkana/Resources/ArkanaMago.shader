// Arkana/Mago — toon de PERSONAGEM. Releitura do mobile-godot/godot/characters/mage_toon.gdshader:
//   * 4 degraus de borda SUAVE com wrap: leitura de toon em combate sem achatar o volume.
//   * rim modulado pela normal.y (luz do CEU: nasce no ombro, nao no pe') — o mago nunca some contra
//     o fundo, que e' exigencia de combate.
//   * specular Blinn-Phong pequeno; `_Metallic` so' decide se o brilho corta em degrau (metal de
//     desenho) ou fica macio (tecido/pele). NAO ha' reflexo de ambiente: metal de verdade sem probe
//     vira silhueta preta no mobile (a licao de 26/08) — por isso metallic/smoothness seguem domados
//     pelo MaterialMago e aqui so' dosam um brilho somado.
//   * emissao crua (_EmissionColor): a MARCA no peito e as gemas da luva.
// Propriedades com os nomes do URP Lit (_BaseColor, _EmissionColor, _Metallic, _Smoothness): o
// MaterialMago pinta, doma e tinge sem saber qual shader achou.
Shader "Arkana/Mago"
{
    Properties
    {
        [MainColor] _BaseColor("Cor", Color) = (0.8, 0.8, 0.8, 1)
        [HDR] _EmissionColor("Emissao", Color) = (0, 0, 0, 1)
        _CorRim("Cor do rim", Color) = (1, 1, 1, 1)
        _Rim("Rim", Range(0, 2)) = 0.3
        _PotenciaRim("Potencia do rim", Range(1, 8)) = 3
        _Metallic("Metal (domado)", Range(0, 1)) = 0.05
        _Smoothness("Lisura (domada)", Range(0, 1)) = 0.35
        _Wrap("Wrap (half-lambert)", Range(0, 0.8)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _EmissionColor;
            float4 _CorRim;
            float _Rim;
            float _PotenciaRim;
            float _Metallic;
            float _Smoothness;
            float _Wrap;
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

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variantes
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fog : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

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
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float4 Frag(Variantes i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                Light luz = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float atten = luz.shadowAttenuation * luz.distanceAttenuation;

                float w = saturate((dot(n, luz.direction) + _Wrap) / (1.0 + _Wrap));
                // 4 degraus de borda suave; somam 1,0 na luz cheia
                float faixa = 0.20
                            + 0.26 * smoothstep(0.10, 0.22, w)
                            + 0.30 * smoothstep(0.40, 0.52, w)
                            + 0.24 * smoothstep(0.70, 0.82, w);
                float3 base = _BaseColor.rgb;
                float3 cor = base * (faixa * atten * luz.color + Ambiente(n));

                // brilho somado: lisura da' o lobo, metal decide se corta em degrau
                float3 h = SafeNormalize(luz.direction + V);
                float potencia = lerp(8.0, 48.0, _Smoothness);
                float s = pow(max(dot(n, h), 0.0), potencia);
                s = lerp(s, smoothstep(0.25, 0.32, s), saturate(_Metallic * 5.0));
                cor += s * (0.03 + 0.3 * _Smoothness * _Smoothness) * atten * luz.color;

                // rim de CEU: forte no ombro (normal pra cima), fraco no pe'
                float fres = pow(1.0 - saturate(dot(n, V)), _PotenciaRim);
                float ceu = saturate(n.y * 0.4 + 0.62);
                cor += _EmissionColor.rgb + _CorRim.rgb * fres * _Rim * ceu;

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

    Fallback "Universal Render Pipeline/Lit"
}
