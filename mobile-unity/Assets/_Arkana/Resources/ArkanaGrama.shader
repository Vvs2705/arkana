// Arkana/Grama — tufos, flores, juncos e seixos por GPU instancing (Graphics.RenderMeshInstanced).
// Releitura do mobile-godot/godot/world/grass.gdshader:
//   * vento no VERTICE pesado pela altura (o pe' fica cravado, a ponta balanca); fase pela posicao
//     da instancia, entao a campina ondula em vez de bater em unissono.
//   * COLAPSO por distancia 3D: alem do fade a lamina afunda e some do fill rate. A celula inteira ja'
//     e' cortada antes (Grama.cs, na horizontal); o colapso esconde a borda desse corte.
//   * NORMAL ACHATADA PRA CIMA: com a normal real as 3 faces do tufo pegavam 3 brilhos e o tufo lia
//     como OBJETO facetado; puxada pro UP ele recebe a luz do chao embaixo e vira TEXTURA do chao.
//   * gradiente base->ponta vem na cor de vertice; a tinta POR INSTANCIA (_Tinta) quebra a repeticao.
// Sem ShadowCaster de proposito: grama nao projeta sombra (orcamento), mas RECEBE (casa com o chao).
Shader "Arkana/Grama"
{
    Properties
    {
        _Vento("Balanco do vento (m)", Float) = 0.09
        _AlturaLamina("Altura da lamina (m)", Float) = 0.7
        _FadeInicio("Comeco do colapso (m)", Float) = 42
        _FadeFim("Fim do colapso (m)", Float) = 56
        _Achatar("Normal puxada pra cima", Range(0, 1)) = 0.8
        _Wrap("Wrap (half-lambert)", Range(0, 0.6)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // sem a matriz inversa por instancia: metade dos dados no buffer, mais tufos por draw call. A escala
            // do tufo NAO e' uniforme (y 0,75-1,3x), mas a normal ja' e' 80% puxada pro UP: o erro nao aparece.
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Vento;
                float _AlturaLamina;
                float _FadeInicio;
                float _FadeFim;
                float _Achatar;
                float _Wrap;
            CBUFFER_END

            // tinta POR INSTANCIA: o array vem no MaterialPropertyBlock de cada lote (SetVectorArray)
            UNITY_INSTANCING_BUFFER_START(PorTufo)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Tinta)
            UNITY_INSTANCING_BUFFER_END(PorTufo)

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

                float3 iw = TransformObjectToWorld(float3(0.0, 0.0, 0.0));   // posicao da instancia no mundo
                float d = distance(iw, GetCameraPositionWS());
                float3 p = v.positionOS.xyz;
                float t = saturate(p.y / max(_AlturaLamina, 0.01));
                p.y *= 1.0 - smoothstep(_FadeInicio, _FadeFim, d);
                float tempo = _Time.y;
                p.x += sin(tempo * 1.9 + iw.x * 0.8 + iw.z * 0.6) * _Vento * t;
                p.z += cos(tempo * 1.3 + iw.x * 0.5) * _Vento * 0.6 * t;

                float3 pw = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(pw);
                o.positionWS = pw;
                float3 nw = TransformObjectToWorldNormal(v.normalOS);
                o.normalWS = normalize(lerp(nw, float3(0.0, 1.0, 0.0), _Achatar));
                o.color = v.color * UNITY_ACCESS_INSTANCED_PROP(PorTufo, _Tinta);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float4 Frag(Variantes i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light luz = GetMainLight(sc);
                // distanceAttenuation fica de fora: e' 1 para o sol, e o desenho procedural nao garante unity_LightData
                float d = saturate((dot(n, luz.direction) + _Wrap) / (1.0 + _Wrap)) * luz.shadowAttenuation;
                float3 cor = i.color.rgb * (d * luz.color + Ambiente(n));
                // nevoa POR PIXEL (a do URP): o fator por vertice interpola errado em triangulo grande
                // (o mar de quads de 100 m chegava ao far com cor de mar, cortado contra o ceu)
                cor = MixFog(cor, InitializeInputDataFog(float4(i.positionWS, 1.0), i.fog));
                return float4(cor, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Simple Lit"
}
