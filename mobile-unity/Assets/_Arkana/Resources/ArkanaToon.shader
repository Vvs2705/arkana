// Arkana/Toon — cel-shading HIBRIDO do mundo (terreno, arvores, rochas, ruinas). Releitura do
// mobile-godot/godot/world/toon.gdshader, nao traducao:
//   * cor de VERTICE e' a cor (bioma x AO ja' multiplicados pela Ilha); o alfa e' o PESO DE ROCHA.
//   * wrap antes de bandar (o lado escuro tem forma) + degrau MACIO (sem serrilha piscando no celular).
//   * sombra com COR FRIA (clima com cor, nunca falta de luz) e rim dos dois lados.
//   * textura de detalhe CINZA em espaco de MUNDO: tira a cara de plastico sem UV no terreno.
// Tudo que e' opcional (mancha, grao, textura, sheen, chao de pedra) e' branch em UNIFORM: coerente no
// warp, quem nao liga nao paga. Uniform, nao keyword: keyword ligada em runtime some no APK (o
// _NORMALMAP do mago ensinou). Ambiente pelas 3 cores que o URP escreve por camera (unity_Ambient*),
// nao pelo probe SH: nao depende de o probe ser recalculado em runtime.
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
        // CHAO DE PEDRA: so' o material do terreno liga (Ilha.MaterialTerreno). Tons em sRGB, MULTIPLICAM a cor.
        _Chao("Chao de pedra (so' terreno)", Float) = 0
        _EscalaManchaPedra("Escala da mancha de pedra (1/m)", Float) = 0.09
        // sobrios de proposito: o sol do entardecer ja' puxa tudo pro laranja (terra forte virava laranja, liquen virava limao)
        _TomGasta("Tom: pedra gasta", Color) = (0.74, 0.73, 0.76, 1)
        _TomTerra("Tom: terra quente", Color) = (0.97, 0.88, 0.78, 1)
        _TomLiquen("Tom: liquen", Color) = (0.74, 0.86, 0.66, 1)
        _Estrato("Estratos por metro (encosta)", Float) = 0.6
        _Fissura("Forca da fissura", Range(0, 1)) = 0.6
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
            float _Chao;
            float _EscalaManchaPedra;
            float4 _TomGasta;
            float4 _TomTerra;
            float4 _TomLiquen;
            float _Estrato;
            float _Fissura;
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

            // o mesmo Hoskins, com duas saidas: o ponto sorteado de cada celula (fissura e seixo)
            float2 H22(float2 p)
            {
                float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float Fbm2(float2 p)
            {
                return Ruido(p) * 0.65 + Ruido(p * 2.03 + 17.0) * 0.35;
            }

            // lerp(1, t, s) sem depender de promover escalar em intrinseca
            float3 Tom(float3 t, float s)
            {
                return 1.0 + (t - 1.0) * s;
            }

            // Voronoi F2-F1, em unidade de celula: ~0 na BORDA entre duas celulas, que e' onde a pedra racha
            float BordaDeCelula(float2 p)
            {
                float2 i = floor(p);
                float2 f = p - i;
                float d1 = 8.0;
                float d2 = 8.0;
                [unroll] for (int y = -1; y <= 1; y++)
                {
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        // ponto longe da quina da celula: sem lasca fina demais entre duas fissuras
                        float2 r = g + 0.15 + 0.7 * H22(i + g) - f;
                        float d = dot(r, r);
                        d2 = min(d2, max(d1, d));
                        d1 = min(d1, d);
                    }
                }
                return sqrt(d2) - sqrt(d1);
            }

            // PEDRA DO CHAO (_Chao): o planalto do pico e as encostas. Tudo e' TOM multiplicado sobre a cor do vertice:
            // o bioma continua mandando, e a transicao com grama e areia herda a rampa do vertice. Os tons do material sao
            // sRGB e o Unity os converte: multiplicar em linear = multiplicar em sRGB (a curva e' potencia), entao 0,7 no
            // material e' 0,7 na tela. Os escalares daqui sao LINEARES (0,4 linear ~ 0,66 na tela).
            // `pegada` = tamanho do pixel no chao (m): o detalhe fino some antes de caber em 2 pixels (vira chuvisco/moire').
            // `grao` = o ruido de 0,6-1,5 m que o bloco _Grao ja' pagou: dobra a fissura sem ruido novo.
            // Custo (fxc, passe com sombra: 447 -> 984 slots no total): +225 em toda pedra, +195 na pedra a menos de ~27 m
            // (Voronoi 3x3 + seixo), +80 na encosta. Grama e areia nao entram aqui (~ +20 la' fora: pegada e borda).
            float3 TomDaPedra(float3 p, float3 n, float pedra, float pegada, float grao)
            {
                float2 xz = p.xz;
                // 1) MANCHAS GRANDES (~5-11 m): um campo dobrado por ruido decide pedra gasta (baixo) x terra quente
                //    (alto); outro, menor, poe liquen — e mais liquen na lingua onde a pedra encontra a grama.
                float2 q = xz + (Ruido(xz * 0.045 + 3.1) - 0.5) * float2(10.0, -7.0);
                // borda da mancha roida pelo grao (sem ele a mancha e' borrao de aerografo); some com a pegada, junto do grao
                float roe = (grao - 0.5) * 0.16 * (1.0 - smoothstep(0.25, 0.9, pegada));
                float a = Fbm2(q * _EscalaManchaPedra) + roe;
                float gasta = 1.0 - smoothstep(0.30, 0.37, a);
                float terra = smoothstep(0.66, 0.72, a);
                // liquen em cacho de ~2 m, mais na pedra gasta (pedra velha e' que tem liquen; espalhado lia como gramado)
                float b = Ruido(q * (_EscalaManchaPedra * 5.0) + 41.0);
                float liquen = smoothstep(0.62, 0.72, b - roe) * (0.25 + 0.75 * (1.0 - smoothstep(0.30, 0.50, a)));
                // o liquen miudo pisca quando a celula cabe em 2-3 pixels: some com a pegada
                liquen *= 1.0 - smoothstep(0.4, 1.0, pegada);
                liquen = max(liquen, (1.0 - smoothstep(0.45, 0.85, pedra)) * 0.5) * (1.0 - terra);
                float3 tom = Tom(_TomGasta.rgb, gasta) * Tom(_TomTerra.rgb, terra) * Tom(_TomLiquen.rgb, liquen);
                // bem longe e rasante (so' antes da nevoa) ate' a mancha grande pisca: vira o tom MEDIO (~cobertura de cada uma)
                float3 medio = Tom(_TomGasta.rgb, 0.17) * Tom(_TomTerra.rgb, 0.10) * Tom(_TomLiquen.rgb, 0.10);
                tom = lerp(tom, medio, smoothstep(3.0, 8.0, pegada));

                // 2) ENCOSTA: a normal do pixel separa o planalto da encosta. Nesta ilha nenhum vertice passa de ~32 graus
                //    (ny >= 0,85, medido): o estrato vive entre 16 e 29 graus. Camadas horizontais de ~1,7 m, ondeadas
                //    (camada reta e' regua), tom proprio por camada, sulco escuro embaixo e quina clara em cima.
                float ingreme = 1.0 - smoothstep(0.875, 0.96, n.y);
                [branch] if (ingreme > 0.001)
                {
                    float onda = Ruido(xz * 0.07 + 11.0);
                    float ey = p.y * _Estrato + (onda - 0.5) * 1.6;
                    float camada = floor(ey);
                    float fy = ey - camada;
                    // a pegada mede o chao; na encosta a cota muda no maximo isso por pixel: sulco some antes da camada
                    float nitido = saturate(1.0 - pegada * _Estrato * 5.0);
                    float largo = saturate(1.0 - pegada * _Estrato * 1.5);
                    // camada escura-fria x clara-quente (so' o sulco lia como curva de nivel, nao como rocha em camadas)
                    float hc = saturate(H21(float2(camada, 13.7)) * 0.7 + onda * 0.3);
                    float3 tomCamada = lerp(float3(0.62, 0.62, 0.68), float3(1.14, 1.06, 0.94), hc);
                    float perfil = (1.0 - 0.61 * (1.0 - smoothstep(0.0, 0.18, fy)) * nitido)
                                 * (1.0 + 0.28 * smoothstep(0.78, 1.0, fy) * nitido);
                    tom *= Tom(Tom(tomCamada, largo) * perfil, ingreme);
                }

                // 3) DE PERTO: fissura (borda de Voronoi dobrada, em cacho: a rede inteira le' como piso de ladrilho) e
                //    seixo miudo. A linha e' filtrada pela pegada: alarga e clareia junto (a media fica), depois some
                //    (pegada 0,25 m = ~27 m do olho a 3 m de altura; alem disso a fissura ja' nao passava de 3%).
                //    _Fissura 0 pula o bloco inteiro (fissura E seixo): e' o corte de custo se o FPS cair no aparelho.
                float perto = saturate(1.0 - pegada * 4.0);
                [branch] if (perto * _Fissura > 0.0)
                {
                    float2 cf = xz * 0.5 + (grao - 0.5) * float2(0.5, -0.4);
                    // F2-F1 ~ 2x a distancia ate' a borda: 0,05 = fissura de ~5 cm (3-4 px a 5 m do olho; 2 px some a 395 ppi)
                    float e = BordaDeCelula(cf) / 0.5;
                    float larg = max(0.05, pegada * 1.5);
                    float linha = (1.0 - smoothstep(0.0, larg, e)) * (0.05 / larg);
                    // rede inteira = piso de ladrilho/lama seca (visto na previa): so' em cacho (campo do liquen) e em
                    // trecho (o grao corta a aresta em pedacos de ~1 m) — nenhum ruido novo
                    linha *= smoothstep(0.50, 0.66, b) * smoothstep(0.42, 0.60, grao) * pedra;   // x pedra: nada de racha no verde
                    tom *= 1.0 - _Fissura * linha * perto;
                    // seixo: ~1 em 8 celulas de 0,45 m ganha um ponto de 5-9 cm, escuro ou claro (o grao que se ve')
                    float2 sc = xz / 0.45;
                    float2 si = floor(sc);
                    float2 sh = H22(si + 101.0);
                    float rs = length(sc - si - (0.3 + 0.4 * sh));
                    float seixo = (1.0 - smoothstep(0.10, 0.20, rs)) * step(0.87, frac((sh.x + sh.y) * 7.31));
                    seixo *= saturate(1.0 - pegada * 16.0);
                    tom *= 1.0 + seixo * (frac(sh.x * 13.7) > 0.5 ? 0.28 : -0.54);
                }
                return tom;
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

                // CHAO DE PEDRA: quanto o pixel e' pedra e o tamanho dele no chao. A pegada (fwidth, m) e' a distancia
                // ja' corrigida pelo angulo rasante. Derivada so' aqui, em branch de UNIFORM: em branch por pixel e'
                // indefinida no celular. Quem nao liga (arvore, ruina) fica com pedra 0 e pegada 0: nada muda.
                float pedra = 0.0;
                float pegada = 0.0;
                float m = 0.0;
                if (_Mancha > 0.0) m = Ruido(xz * _EscalaMancha);
                if (_Chao > 0.0)
                {
                    pegada = length(fwidth(i.positionWS));
                    // borda pedra/grama quebrada pelo MESMO ruido da mancha (~5 m, sem pagar outro): a rocha entra na
                    // grama em lingua, nao em anel de cota. ponytail: _Mancha 0 deixa a borda lisa (m = 0)
                    pedra = smoothstep(0.25, 0.75, i.color.a + (m - 0.5) * 0.55);
                }

                if (_Mancha > 0.0)
                {
                    // a mancha puxa o MATIZ pro capim seco, nao so' escurece (escurecer lia como sujeira).
                    // Na pedra quem pinta mancha e' o TomDaPedra.
                    col = lerp(col, col * float3(1.06, 0.88, 0.62), m * _Mancha * 2.0 * (1.0 - pedra));
                }
                float grao = 0.5;
                if (_Grao > 0.0)
                {
                    // duas amostras defasadas: uma so' desenha xadrez com a camera rente ao chao. No chao, some com a
                    // pegada: oitava de 0,6-1,5 m a 60 m do olho e' chuvisco.
                    grao = Ruido(xz * _EscalaGrao) * 0.65 + Ruido(xz * _EscalaGrao * 2.7 + 31.0) * 0.35;
                    float g = _Grao * (1.0 - smoothstep(0.25, 0.9, pegada));
                    col *= lerp(1.0 - g, 1.0 + g, grao);
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
                // branch por pixel: grama e areia (pedra 0) nao pagam o Voronoi. Dentro, nada de textura nem derivada.
                [branch] if (pedra > 0.003)
                {
                    col *= Tom(TomDaPedra(i.positionWS, n, pedra, pegada, grao), pedra);
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
