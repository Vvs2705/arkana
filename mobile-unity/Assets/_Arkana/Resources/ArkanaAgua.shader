// Arkana/Agua — mar, lago e alagado. Releitura do mobile-godot/godot/world/water.gdshader:
//   * ondas no VERTICE, com LOD por distancia (`detalhe`: 1 perto, 0 alem de 160 m).
//   * padrao SEM EIXO: value noise girado ~31 graus + 1/3 de marola. Dois senos cruzados davam grade
//     de piscina (medido no Godot); listra reta dava toalha de piquenique.
//   * cor rasa/funda POR VERTICE: COLOR.r = o quanto o fundo esta' raso ali (a Ilha grava pela
//     profundidade DE VERDADE: 0 no fundo, 1 na margem). E' o que faz o olho ler profundidade — e a
//     espuma e a transparencia nascem na margem real, entao o lago deixa de ser um disco de borda dura
//     visto do alto: na beira a agua fica rala e a areia molhada aparece. O mar grava 0.
//   * reflexo FALSO do ceu so' no rasante (pow 5) e espuma de crista + de margem.
//   * translucida perto, OPACA longe: a 12% de alfa a borda da malha do terreno aparecia atraves do
//     mar visto do alto (a "borda fantasma" do Godot). Sem depth texture, sem luz (le' como cartoon).
Shader "Arkana/Agua"
{
    Properties
    {
        _Rasa("Cor rasa", Color) = (0.16, 0.65, 1, 1)
        _Funda("Cor funda", Color) = (0.06, 0.30, 0.75, 1)
        _CeuTinta("Reflexo do ceu", Color) = (0.95, 0.70, 0.42, 1)
        _Onda("Altura da onda (m)", Float) = 0.1
        _Alfa("Alfa perto", Range(0, 1)) = 0.88
        _Espuma("Espuma de margem", Range(0, 1)) = 0
        _EscalaBanda("Escala do padrao", Float) = 1
        _Contraste("Contraste da banda", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Agua"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Rasa;
                float4 _Funda;
                float4 _CeuTinta;
                float _Onda;
                float _Alfa;
                float _Espuma;
                float _EscalaBanda;
                float _Contraste;
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
                float detalhe : TEXCOORD2;
                float fog : TEXCOORD3;
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

            // dominio GIRADO: value noise interpola numa grade, e grade alinhada ao mundo desenha losango
            float Marola(float2 p, float t)
            {
                float2 q = float2(p.x * 0.857 - p.y * 0.515, p.x * 0.515 + p.y * 0.857);
                float n = Ruido(q * 0.4 + float2(t * 0.07, t * 0.045));
                float swell = sin(p.x * 0.62 + p.y * 0.28 + t * 0.8) * 0.5 + 0.5;
                return saturate(n * 0.68 + swell * 0.32);
            }

            Variantes Vert(Atributos v)
            {
                Variantes o = (Variantes)0;
                float3 pw = TransformObjectToWorld(v.positionOS.xyz);
                float det = 1.0 - smoothstep(55.0, 160.0, distance(pw, GetCameraPositionWS()));
                float t = _Time.y;
                pw.y += (sin(t * 1.4 + pw.x * 0.35 + pw.z * 0.21) * _Onda
                       + cos(t * 0.9 + pw.z * 0.44) * _Onda * 0.6) * det;
                o.positionCS = TransformWorldToHClip(pw);
                o.positionWS = pw;
                o.anel = v.color.r;
                o.detalhe = det;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float4 Frag(Variantes i) : SV_Target
            {
                float t = _Time.y;
                float b = Marola(i.positionWS.xz * _EscalaBanda, t);
                // 3 tons em banda (o vocabulario do toon), smoothstep e nao step: sem serrilha rastejando
                float faixa = smoothstep(0.38, 0.52, b) * 0.55 + smoothstep(0.62, 0.78, b) * 0.45;
                faixa = lerp(0.5, faixa, i.detalhe * _Contraste);
                float margem = smoothstep(0.25, 1.0, i.anel) * _Espuma;
                float3 cor = lerp(_Funda.rgb, _Rasa.rgb, saturate(faixa * 0.62 + margem * 0.42));
                // reflexo falso, so' no rasante: a normal da lamina e' o UP
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float fr = pow(1.0 - saturate(V.y), 5.0);
                cor = lerp(cor, _CeuTinta.rgb, fr * 0.45);
                // espuma de crista (onde a onda esta' no pico) + faixa FINA de margem respirando
                float espuma = smoothstep(0.84, 0.96, b) * 0.5 * i.detalhe;
                float wob = sin(t * 1.5 + i.positionWS.x * 1.2 + i.positionWS.z * 0.9) * 0.04;
                espuma += smoothstep(0.965 + wob, 1.0 + wob, i.anel) * _Espuma;
                cor = lerp(cor, lerp(_Rasa.rgb, float3(0.95, 0.99, 1.0), 0.6), saturate(espuma) * 0.6);
                // nevoa POR PIXEL (a do URP): no mar de quads de 100 m, o fator por vertice interpolava
                // ate' o far e o mar chegava ao horizonte com cor de mar, cortado contra o ceu
                cor = MixFog(cor, InitializeInputDataFog(float4(i.positionWS, 1.0), i.fog));
                // na beira (pouca agua) a lamina fica rala: a margem se funde com a areia em vez de cortar
                float rala = 1.0 - 0.45 * smoothstep(0.6, 1.0, i.anel) * _Espuma;
                return float4(cor, lerp(1.0, _Alfa, i.detalhe) * rala);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
