// Arkana/Nevoa — bruma baixa do alagado (discos translucidos). Releitura do mist.gdshader: 2 ondas de
// seno baratas deslizando, alfa que MORRE na borda (anel em COLOR.r, 0 no centro, 1 na margem) e cor
// clara-esverdeada — clima com COR, nunca falta de luz: a bruma nao escurece nada. Duas camadas em
// alturas diferentes leem como AR; uma so' le' como adesivo (Ilha monta as duas). Sem luz, sem sombra.
Shader "Arkana/Nevoa"
{
    Properties
    {
        _Cor("Cor (alfa = densidade)", Color) = (0.78, 0.93, 0.86, 0.13)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Nevoa"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Cor;
            CBUFFER_END

            struct Atributos
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Variantes
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float anel : TEXCOORD1;
                float fog : TEXCOORD2;
            };

            Variantes Vert(Atributos v)
            {
                Variantes o;
                float3 pw = TransformObjectToWorld(v.positionOS.xyz);
                pw.y += sin(_Time.y * 0.5 + pw.x * 0.3) * 0.06;
                o.positionCS = TransformWorldToHClip(pw);
                o.positionWS = pw;
                o.anel = v.color.r;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float4 Frag(Variantes i) : SV_Target
            {
                float t = _Time.y;
                float3 p = i.positionWS;
                float n = sin(p.x * 0.35 + t * 0.14) * sin(p.z * 0.28 - t * 0.1)
                        + 0.5 * sin(p.x * 0.9 - t * 0.07 + p.z * 0.6);
                float a = _Cor.a * (0.55 + 0.3 * n) * (1.0 - smoothstep(0.5, 1.0, i.anel));
                return float4(MixFog(_Cor.rgb, InitializeInputDataFog(float4(p, 1.0), i.fog)), saturate(a));
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
