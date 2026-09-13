// Arkana/Agua — mar, lago e alagado. Releitura do mobile-godot/godot/world/water.gdshader:
//   * ondas no VERTICE, com LOD por distancia (`detalhe`: 1 perto, 0 alem de 160 m).
//   * padrao SEM EIXO: value noise girado ~31 graus + 1/3 de marola. Dois senos cruzados davam grade
//     de piscina (medido no Godot); listra reta dava toalha de piquenique.
//   * NITIDEZ pela pegada do pixel (fwidth): o padrao, a crista e a ondulacao da margem cedem ao tom
//     medio quando o pixel ja' nao os resolve (mar rasante, lago visto da queda) — sem moire, sem serrilha.
//   * cor rasa/funda POR VERTICE: COLOR.r = o quanto o fundo esta' raso ali (a Ilha grava pela
//     profundidade DE VERDADE: 0 no fundo, 1 na margem). E' o que faz o olho ler profundidade — e a
//     espuma e a transparencia nascem na margem real, entao o lago deixa de ser um disco de borda dura
//     visto do alto: na beira a agua fica rala e a areia molhada aparece. O mar grava 0.
//   * reflexo FALSO do ceu: rasante = horizonte quente (pow 5); do alto = ceu alto do entardecer com
//     piso `_Reflexo`; brilho do sol pela luz principal; espuma de crista + de margem.
//   * translucida perto, OPACA longe: a 12% de alfa a borda da malha do terreno aparecia atraves do
//     mar visto do alto (a "borda fantasma" do Godot). Sem depth texture, sem luz (le' como cartoon).
//   * `_SemMar0/1`: discos (x, z, raio) onde a lamina nao desenha. So' o MAR usa: ele passa por baixo da
//     ilha inteira (y = 0) e o fundo do lago e a poca do alagado descem abaixo dele.
//   * COSTA (onda 13A, so' o mar): o mar e' uma grade que segue a camera, sem cor de vertice util — a costa vem de
//     `_Costa`, a profundidade do fundo que a Ilha assa UMA vez (Ilha.AssarCosta), lida por XZ de mundo. Faixa rasa
//     turquesa, espuma na linha d'agua que respira (a frente vai e volta) e uma linha rala ao largo entrando. Lago e
//     alagado ficam com a textura PRETA padrao (= mar fundo): neles cada termo novo da' zero.
Shader "Arkana/Agua"
{
    Properties
    {
        _Rasa("Cor rasa", Color) = (0.16, 0.65, 1, 1)
        _Funda("Cor funda", Color) = (0.06, 0.30, 0.75, 1)
        _CeuTinta("Reflexo do ceu (horizonte)", Color) = (0.95, 0.70, 0.42, 1)
        _CeuAlto("Reflexo do ceu (alto)", Color) = (0.52, 0.60, 0.88, 1)
        _Reflexo("Reflexo visto do alto", Range(0, 1)) = 0.1
        _Brilho("Brilho do sol", Range(0, 1)) = 0.8
        _BrilhoDuro("Aperto do brilho (longe)", Float) = 1500
        _Onda("Altura da onda (m)", Float) = 0.1
        _Alfa("Alfa perto", Range(0, 1)) = 0.88
        _Espuma("Espuma de margem", Range(0, 1)) = 0
        _Rala("Lamina rala na beira", Range(0, 1)) = 0.7
        _EscalaBanda("Escala do padrao", Float) = 1
        _Contraste("Contraste da banda", Range(0, 1)) = 1
        _SemMar0("Disco sem lamina 0 (x, z, raio)", Vector) = (0, 0, 0, 0)
        _SemMar1("Disco sem lamina 1 (x, z, raio)", Vector) = (0, 0, 0, 0)
        _Costa("Costa assada (R: profundidade; so' o mar)", 2D) = "black" {}
        _CostaRet("Costa: xz minimo, 1/lado, 1 + seco", Vector) = (0, 0, 0, 0)
        _Turquesa("Agua rasa da costa", Color) = (0.21, 0.76, 0.71, 1)
        _CostaRaso("Forca do raso da costa", Range(0, 1)) = 0.85
        _CostaEspuma("Espuma da costa", Range(0, 1)) = 0.9
        _CostaVel("Ondas por segundo na costa", Float) = 0.15
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
                float4 _CeuAlto;
                float _Reflexo;
                float _Brilho;
                float _BrilhoDuro;
                float _Onda;
                float _Alfa;
                float _Espuma;
                float _Rala;
                float _EscalaBanda;
                float _Contraste;
                float4 _SemMar0;
                float4 _SemMar1;
                float4 _CostaRet;
                float4 _Turquesa;
                float _CostaRaso;
                float _CostaEspuma;
                float _CostaVel;
            CBUFFER_END

            TEXTURE2D(_Costa);
            SAMPLER(sampler_Costa);

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

            // hash SEM seno (Hoskins): o de seno costurava em retas longe da origem (ver ArkanaCeu.shader)
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
                float3 pw = i.positionWS;
                float2 pb = pw.xz * _EscalaBanda;
                float b = Marola(pb, t);
                // NITIDEZ: quanto do padrao cabe num pixel (fwidth, em unidades do padrao; a celula do ruido tem 2,5).
                // Abaixo de ~6 px por celula o desenho vira moire e cede ao tom medio: o corte das bandas (0,14 de
                // largura) nunca chega a ficar mais fino que o pixel. `vivo` = perto E resolvido. KNOB: 0,12-0,4.
                float2 fw = fwidth(pb);
                float resolvido = 1.0 - smoothstep(0.12, 0.4, max(fw.x, fw.y));
                float vivo = i.detalhe * resolvido;
                // 3 tons em banda (o vocabulario do toon), smoothstep e nao step: sem serrilha rastejando
                float faixa = smoothstep(0.38, 0.52, b) * 0.55 + smoothstep(0.62, 0.78, b) * 0.45;
                faixa = lerp(0.5, faixa, vivo * _Contraste);
                float margem = smoothstep(0.25, 1.0, i.anel) * _Espuma;
                float3 cor = lerp(_Funda.rgb, _Rasa.rgb, saturate(faixa * 0.62 + margem * 0.42));

                // COSTA (so' o mar). q = profundidade / Ilha.CostaFundo: < 0 na terra, 0 na linha d'agua, 1 no mar fundo. A textura
                // guarda um pouco de terra acima da linha (_CostaRet.w = 1 + Ilha.CostaSeco): o bilinear cruza o zero na beira
                // DESENHADA. Textura preta (lago, alagado, fora do retangulo pelo clamp) = costa 0: o if pula e nada muda neles.
                // Custo: 1 amostra por pixel; a conta so' roda na faixa da costa (o mar fundo, quase todo o quadro, pula).
                float costa = SAMPLE_TEXTURE2D(_Costa, sampler_Costa, (pw.xz - _CostaRet.xy) * _CostaRet.z).r;
                float q = 1.0 - costa * _CostaRet.w;
                float px = max(fwidth(q), 1e-4);
                float espumaCosta = 0.0, ralaCosta = 1.0;
                // o if USA o px de proposito: assim a derivada fica antes dele (o fxc a afundava para dentro, e derivada em
                // fluxo divergente = pixel branco solto na borda de fora da faixa). Entra onde ha' costa no pixel OU no vizinho;
                // o mar fundo, o lago e o alagado (q = 1 constante: px = 1e-4) pulam.
                [branch] if (costa + px > 2e-4)
                {
                    // faixa rasa: turquesa cheio ate' ~8 m da beira, degrade ate' o fundo (~25 m); a marola continua por baixo
                    cor = lerp(cor, _Turquesa.rgb * (0.9 + 0.2 * faixa), (1.0 - smoothstep(0.35, 1.0, q)) * _CostaRaso);
                    // ESPUMA. Uma onda a cada 1/_CostaVel s, defasada ao longo da costa (a frente chega obliqua, ~63 m entre
                    // cristas) e, de perto, pela marola (frente irregular). (1) BEIRA: faixa branca da linha d'agua ate' `larg`,
                    // que respira — larga quando a onda chega (fase 0/1), estreita no refluxo (0,5): a espuma lambe a areia; a
                    // frente e' o mais branco. (2) LINHA rala ao largo: nasce a q 0,75 (~17 m), entra ate' 0,12 e se funde na
                    // beira; picotada pela marola. Largura minima de ~1 px (px) com a forca dividida pelo excesso: do alto nao
                    // cintila, rasante nao vira faixa. KNOB: 0,04/0,12 (largura/respiro da beira: ~0,8 a ~3,3 m), 0,75 (de onde
                    // a linha nasce), 0,03 (meia largura da linha), 0,013/0,009 (obliquidade da frente).
                    float fase = frac(t * _CostaVel + dot(pw.xz, float2(0.013, 0.009)) + b * 0.25 * resolvido);
                    float larg = (0.04 + 0.12 * (0.5 + 0.5 * cos(fase * 6.2832))) * lerp(1.0, 0.7 + 0.6 * b, resolvido);
                    float beira = (1.0 - smoothstep(larg * 0.7, larg + px, q)) * lerp(0.75, 1.0, smoothstep(larg * 0.3, larg * 0.7, q));
                    float qc = lerp(0.75, 0.12, fase);
                    float linha = (1.0 - smoothstep(0.03 - px, 0.03 + px, abs(q - qc))) * saturate(0.06 / px)
                                * smoothstep(0.0, 0.2, fase) * (1.0 - smoothstep(0.75, 1.0, fase))
                                * lerp(0.6, smoothstep(0.3, 0.62, b), resolvido);
                    espumaCosta = saturate(max(beira, linha * 0.7)) * _CostaEspuma;
                    // lamina rala na beira (q 0,3 ~ 6 m): a areia molhada aparece por baixo — a conta do lago, pelo q
                    ralaCosta = 1.0 - _Rala * (1.0 - smoothstep(0.0, 0.3, q));
                }

                // REFLEXO do ceu. Lamina plana: a direcao refletida sobe o quanto o olhar desce (R.y = V.y). Rasante =
                // o horizonte quente (a conta de antes: pow 5 ate' 0,45); do alto = o ceu alto do entardecer, com o piso
                // `_Reflexo`. A Fresnel de verdade do alto (~2%) nao se ve': o lago da queda nao tinha ceu nenhum.
                float3 V = GetWorldSpaceNormalizeViewDir(pw);
                float fr = lerp(_Reflexo, 0.45, pow(1.0 - saturate(V.y), 5.0));
                cor = lerp(cor, lerp(_CeuTinta.rgb, _CeuAlto.rgb, saturate(V.y * 1.8)), fr);
                // BRILHO do sol (a luz principal que o URP publica), puxando PARA o dourado (somar luz quente no azul
                // da' rosa acinzentado). Longe: o reflexo do disco do sol, apertado. Perto: lobo 30x mais largo e so' nas
                // cristas do ruido — o caminho de faiscas na direcao do sol. Longe o lobo largo pintava o lago inteiro.
                // ponytail: normal plana (sem mapa de normal); a crista do ruido faz o papel da agua agitada.
                float3 R = float3(-V.x, V.y, -V.z);
                float sol = pow(saturate(dot(R, _MainLightPosition.xyz)), _BrilhoDuro * lerp(1.0, 0.03, vivo)) * _Brilho;
                sol *= lerp(1.0, smoothstep(0.5, 0.8, b) * 2.0, vivo);
                cor = lerp(cor, _MainLightColor.rgb * float3(1.0, 0.75, 0.45), saturate(sol));

                // espuma de crista (onde a onda esta' no pico) + faixa de margem respirando. Perto: fina e ondulando;
                // longe a ondulacao (4 m, fora do LOD de antes) some e a faixa alarga para ~5 px e esmaece: sem a
                // escadinha branca que a borda do lago mostrava do alto.
                float espuma = smoothstep(0.84, 0.96, b) * 0.5 * vivo;
                float wob = sin(t * 1.5 + pw.x * 1.2 + pw.z * 0.9) * 0.04 * vivo;
                float largura = lerp(0.12, 0.035, vivo);
                espuma += smoothstep(1.0 - largura + wob, 1.0 + wob, i.anel) * _Espuma * lerp(0.5, 1.0, vivo);
                espuma = saturate(espuma);
                cor = lerp(cor, lerp(_Rasa.rgb, float3(0.95, 0.99, 1.0), 0.6), espuma * 0.6);
                // a espuma da costa vai por cima do reflexo e do brilho: espuma e' fosca
                cor = lerp(cor, float3(0.95, 0.99, 1.0), espumaCosta * 0.85);

                // nevoa POR PIXEL (a do URP): no mar de quads de 100 m, o fator por vertice interpolava
                // ate' o far e o mar chegava ao horizonte com cor de mar, cortado contra o ceu
                cor = MixFog(cor, InitializeInputDataFog(float4(pw, 1.0), i.fog));
                // na beira (pouca agua) a lamina fica rala: a margem se funde com a areia molhada em vez de cortar.
                // Perto a espuma devolve o corpo (a linha branca na areia); longe fica so' o degrade. Na costa do mar (ralaCosta),
                // a espuma da costa devolve o corpo de perto E do alto: e' ela que desenha a ilha vista da queda.
                float rala = (1.0 - _Rala * smoothstep(0.7, 1.0, i.anel) * _Espuma) * ralaCosta;
                float alfa = max(lerp(1.0, _Alfa, i.detalhe) * rala, max(espuma * 0.8 * vivo, espumaCosta * 0.9));
                // discos sem lamina (o mar sob o lago e o alagado): alfa 0, sem discard — discard custa o early-Z
                float2 d0 = pw.xz - _SemMar0.xy;
                float2 d1 = pw.xz - _SemMar1.xy;
                alfa *= step(_SemMar0.z * _SemMar0.z, dot(d0, d0)) * step(_SemMar1.z * _SemMar1.z, dot(d1, d1));
                return float4(cor, alfa);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
