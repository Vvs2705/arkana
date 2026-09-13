// Arkana/Mago — toon de PERSONAGEM: os 20 magos da Meshy (textura pintada + normal) e o procedural de reserva.
// Releitura do mobile-godot/godot/characters/mage_toon.gdshader, com o que a foto de 12/09 pediu:
//   * FAIXA de sombra toon suave: um terminador macio e o lado escuro num tom so' (sol fraco + ambiente em 3 cores +
//     o preenchimento). Os 4 degraus de antes sobre a textura pintada da Meshy viravam cartaz.
//   * CONTORNO DE LUZ (onda 9A): de costas para o sol o mago virava silhueta escura sobre o chao claro e o ceu
//     dourado. Borda QUENTE do lado do sol, cheia no contraluz (a camera olhando para o sol); borda FRIA do lado
//     oposto (luz do ceu). Os dois nascem no ombro e somem na sola. Fino perto (discreto na camera de ombro, 4 m),
//     largo e cheio longe (le' a 20-30 m no celular). Somado em "tela": acende o escuro e nunca passa de 1 — nao acorda o
//     bloom (limiar 1,1). A tinta do bot colore o contorno; o cinza de morto o apaga; a piscada o cobre.
//   * specular Blinn-Phong pequeno; `_Metallic` so' decide se o brilho corta em degrau (metal de desenho) ou fica
//     macio. NAO ha' reflexo de ambiente: metal de verdade sem probe vira silhueta preta no mobile (a licao de 26/08).
//   * emissao = _EmissionColor x a PROPRIA textura: a marca do procedural (textura branca) e o preenchimento dos FBX
//     (0,16 x a cor, ImportacaoArkana). ponytail: mapa de emissao proprio so' quando um mago trouxer gema acesa.
// Nomes do URP Lit (_BaseMap, _BaseColor, _BumpMap, _EmissionColor, _Metallic, _Smoothness): o Mago troca o shader
// do material importado e a textura vem junto; a piscada (MaterialPropertyBlock do VisualDoImpacto) e a tinta
// (SetTint) seguem no _BaseColor. Sem keyword nenhuma: keyword ligada em runtime some no APK (licao do _NORMALMAP).
Shader "Arkana/Mago"
{
    Properties
    {
        [MainColor] _BaseColor("Cor", Color) = (0.8, 0.8, 0.8, 1)
        [MainTexture] [NoScaleOffset] _BaseMap("Textura", 2D) = "white" {}
        [Normal] [NoScaleOffset] _BumpMap("Normal", 2D) = "bump" {}
        [HDR] _EmissionColor("Emissao (x textura)", Color) = (0, 0, 0, 1)
        _Metallic("Metal (domado)", Range(0, 1)) = 0.05
        _Smoothness("Lisura (domada)", Range(0, 1)) = 0.35
        // KNOBs da faixa e do contorno: calibrar pela foto 36, nunca por teste (a previa em numpy da onda 9A partiu daqui).
        // Cores em sRGB, como toda cor de material no projeto Linear (chegam ao shader como 1/0,8/0,55 e 0,5/0,66/1).
        _Sombra("Sol no lado escuro", Range(0, 1)) = 0.25
        _Maciez("Maciez do terminador (N.L)", Range(0.01, 0.5)) = 0.1
        _CorQuente("Contorno quente (x cor do sol)", Color) = (1, 0.906, 0.767, 1)
        _Quente("Forca do quente", Range(0, 2)) = 0.8
        _CorFrio("Contorno frio (ceu)", Color) = (0.735, 0.832, 1, 1)
        _Frio("Forca do frio", Range(0, 1)) = 0.25
        _Perto("Contorno a 4 m (x)", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // O MESMO cbuffer em todos os passes: e' o que mantem o shader compativel com o SRP Batcher.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _EmissionColor;
            float _Metallic;
            float _Smoothness;
            float _Sombra;
            float _Maciez;
            float4 _CorQuente;
            float _Quente;
            float4 _CorFrio;
            float _Frio;
            float _Perto;
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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variantes
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;   // w: sinal da bitangente (espelho de UV e escala negativa)
                float2 uv : TEXCOORD3;
                float fog : TEXCOORD4;
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
                VertexNormalInputs nv = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = nv.normalWS;
                o.tangentWS = float4(nv.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uv = v.uv;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float4 Frag(Variantes i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                // normal da MALHA (contorno: segue a silhueta) e a do normal map (luz: dobra de pano, costura)
                float3 ng = normalize(i.normalWS);
                float3 bit = i.tangentWS.w * cross(ng, i.tangentWS.xyz);
                float3 nt = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));
                float3 n = normalize(TransformTangentToWorld(nt, float3x3(i.tangentWS.xyz, bit, ng)));
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                Light luz = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float atten = luz.shadowAttenuation * luz.distanceAttenuation;

                float3 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                float3 base = tex * _BaseColor.rgb;

                // FAIXA: terminador macio; a sombra projetada cai no MESMO tom do lado escuro (toon: um tom so')
                float banda = smoothstep(-_Maciez, _Maciez, dot(n, luz.direction)) * atten;
                float3 cor = base * (luz.color * lerp(_Sombra, 1.0, banda) + Ambiente(n)) + _EmissionColor.rgb * tex;

                // brilho somado: lisura da' o lobo, metal decide se corta em degrau
                float3 h = SafeNormalize(luz.direction + V);
                float potencia = lerp(8.0, 48.0, _Smoothness);
                float s = pow(max(dot(n, h), 0.0), potencia);
                s = lerp(s, smoothstep(0.25, 0.32, s), saturate(_Metallic * 5.0));
                cor += s * (0.03 + 0.3 * _Smoothness * _Smoothness) * banda * luz.color;

                // CONTORNO. `longe`: 0 ate' 4 m (o braco da camera de ombro e' 4,15), 1 a partir de 24 m. A faixa em N.V
                // engorda com a distancia: a 25 m o mago tem ~70 px de altura no Poco e a faixa de perto daria 1 px. KNOB
                // abs, nao saturate: a Meshy tem triangulo com a normal do avesso (4% do Corvus) e ele acendia INTEIRO
                float longe = saturate((distance(GetCameraPositionWS(), i.positionWS) - 4.0) / 20.0);
                float borda = 1.0 - smoothstep(lerp(0.08, 0.15, longe), lerp(0.42, 0.60, longe), abs(dot(ng, V)));
                // quanto a borda e' do SOL: o lado que o encara, e a silhueta inteira quando a camera olha para ele
                float quente = saturate(dot(ng, luz.direction) * 0.5 + dot(-V, luz.direction) * 0.5 + 0.25);
                float3 rim = quente * _Quente * _CorQuente.rgb * luz.color + (1.0 - quente) * _Frio * _CorFrio.rgb;
                // luz de CIMA (sol e ceu): nasce no ombro e some embaixo (a sola da bota acendia creme na previa)
                rim *= borda * lerp(_Perto, 1.0, longe) * saturate(ng.y * 0.6 + 0.8) * saturate(_BaseColor.rgb);
                cor += saturate(rim) * saturate(1.0 - cor);   // "tela": onde ja' e' claro, quase nada; nunca passa de 1

                // nevoa POR PIXEL (a do URP): o fator por vertice interpola errado em triangulo grande
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
