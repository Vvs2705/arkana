// Arkana/Ceu — skybox do entardecer arcano (RenderSettings.skybox). Releitura do sky.gdshader:
// gradiente horizonte->lilas->topo, DISCO do sol duro + halo largo dourado (a direcao vem da luz principal
// que o URP ja' publica em _MainLightPosition) e a FAIXA DO HORIZONTE puxada pra cor da nevoa: o mar que a
// nevoa engole encosta no ceu sem costura. Zero textura.
//
// ONDA 13B — as nuvens deixaram de ser mancha de ruido (blob bege de borda mole, foto 34/40) e viraram DUAS camadas:
//  1) BANDA DE CUMULOS no horizonte, em coordenada cilindrica (azimute x seno da elevacao): cada celula de 9 graus
//     tem um cumulo feito de 4 lobulos-circulo (corpo, torre, couve-flor em cima da torre, ombro), a silhueta e' o
//     MIN duro dos circulos (topo em couve-flor com vinco nitido) e a BASE e' cortada reta. O volume vem de uma
//     normal "inflada" da silhueta (media softmax dos gradientes dos lobulos), sombreada em 3 faixas toon, com a
//     BORDA DE LUZ dourada so' na silhueta do lado do sol (forte contra o sol: a borda de prata).
//  2) CIRROS no alto: plano projetado, esticado no vento, 4 ruidos (1 dobra + 3 oitavas), 2 cortes toon, fracos.
// A banda gira no azimute com o vento; o cirro anda no plano: as duas camadas andam diferente = profundidade.
// Previa numpy 1:1 deste Frag (sem Unity): rascunho onda13-ceu-out/novo.py.
Shader "Arkana/Ceu"
{
    Properties
    {
        _Topo("Topo", Color) = (0.13, 0.2, 0.46, 1)
        _Meio("Meio (lilas)", Color) = (0.76, 0.6, 0.76, 1)
        _Horizonte("Horizonte", Color) = (0.96, 0.73, 0.42, 1)
        _Nuvem("Nuvem (luz)", Color) = (1, 0.88, 0.76, 1)
        _NuvemMeio("Nuvem (meio-tom)", Color) = (0.9, 0.68, 0.72, 1)
        _NuvemSombra("Sombra da nuvem", Color) = (0.5, 0.42, 0.64, 1)
        _NuvemRim("Borda de luz da nuvem", Color) = (1, 0.8, 0.55, 1)
        _Cirro("Cirro", Color) = (0.95, 0.85, 0.92, 1)
        _NevoaNoHorizonte("Nevoa no horizonte", Range(0, 1)) = 0.6
        _NevoaNaNuvem("Nevoa na nuvem (fracao da faixa)", Range(0, 1)) = 0.5
        _Cobertura("Cobertura da banda", Range(0, 1)) = 0.62
        _AlturaBanda("Base da banda (seno da elevacao)", Range(0, 0.08)) = 0.012
        _TamanhoNuvem("Tamanho do cumulo", Range(0.4, 1)) = 1
        _Vento("Vento (rad/s)", Range(0, 0.02)) = 0.004
        _RimForca("Forca da borda de luz", Range(0, 2)) = 1
        _CirroForca("Forca do cirro", Range(0, 1)) = 0.2
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
                float4 _Meio;
                float4 _Horizonte;
                float4 _Nuvem;
                float4 _NuvemMeio;
                float4 _NuvemSombra;
                float4 _NuvemRim;
                float4 _Cirro;
                float _NevoaNoHorizonte;
                float _NevoaNaNuvem;
                float _Cobertura;
                float _AlturaBanda;
                float _TamanhoNuvem;
                float _Vento;
                float _RimForca;
                float _CirroForca;
            CBUFFER_END

            // KNOBs de forma (fixos: mexer so' com a previa do rascunho aberta)
            #define TAU 6.2831853
            #define CELULAS 40.0        // cumulos na volta inteira (9 graus cada). O lobulo alcanca <= 1,21 raio: com
                                        // _TamanhoNuvem <= 1 cabe nas 3 celulas lidas; mais que isso corta o cumulo em reta
            #define MACIEZ 0.015        // rad: quanto a normal de um lobulo escorre para o vizinho (0 = pedra, 0,04 = massa)
            #define BOJO 1.0            // 1 = cada lobulo sombreia como esfera; <1 chapa o miolo
            #define ALTO_AO 0.8         // fracao do raio local em que a base escura sobe
            #define CORTE1 0.40         // sombra -> meio-tom
            #define CORTE2 0.62         // meio-tom -> luz
            #define FAIXA_TOON 0.04     // largura da transicao das faixas (curta: toon, nao borrao)
            #define RIM_LARGURA 0.005   // rad (~6 px no Poco F4 a FOV 50)
            #define BORDA_AA 0.0016     // rad (~2 px): antisserrilhado da silhueta SEM derivada (a costura do atan2 estoura fwidth)
            #define CIRRO_ESCALA 20.0

            struct Atributos
            {
                float4 positionOS : POSITION;
            };

            struct Variantes
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            // Hash SEM seno (Hoskins). O frac(sin(x)*43758) de antes, com x na casa dos milhares, amplificava o erro de
            // float: o mesmo canto calculado pelas duas celulas vizinhas dava hashes diferentes e a nuvem saia
            // cortada em RETAS (as linhas da grade vistas em perspectiva — foto 12-elenco de 12/09).
            float H21(float2 p)
            {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float H11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float3 H31(float p)
            {
                float3 p3 = frac(p * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xxy + p3.yzz) * p3.zyx);
            }

            float Ruido(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(H21(i), H21(i + float2(1.0, 0.0)), f.x),
                            lerp(H21(i + float2(0.0, 1.0)), H21(i + float2(1.0, 1.0)), f.x), f.y);
            }

            // i mod m. O +0,5 importa: no GPU a divisao e' por reciproco e 40*(1/40) da' 0,99999 -> floor 0 -> a celula 40
            // nao vira 0 e a banda COSTURA no giro do azimute (atras do jogador, em -z).
            float Modp(float i, float m)
            {
                return i - m * floor((i + 0.5) / m);
            }

            // ruido de valor 1D que fecha a volta (m celulas em 2pi): onde ha' cumulo e de que tamanho
            float Env1(float x, float m, float semente)
            {
                float u = x * (m / TAU);
                float i = floor(u);
                float f = u - i;
                f = f * f * (3.0 - 2.0 * f);
                return lerp(H11(Modp(i, m) + semente), H11(Modp(i + 1.0, m) + semente), f);
            }

            // Um lobulo-circulo. sd: silhueta (min duro). acc = (soma dos pesos, gradiente x, gradiente y, raio), pesos
            // softmax exp(-sd/MACIEZ): a normal vem do lobulo em que o ponto esta' mais fundo e escorre no vinco.
            void Lobulo(float x, float y, float cosE, float cx, float cy, float r, inout float sd, inout float4 acc)
            {
                float2 v = float2((x - cx) * cosE, y - cy);
                float ln = length(v) + 1e-6;
                bool vivo = r > 1e-4;
                float sdi = vivo ? ln - r : 1.0;   // lobulo vazio nao deixa um ponto no centro
                sd = min(sd, sdi);
                float e = vivo ? exp(min(-sdi * (1.0 / MACIEZ), 30.0)) : 0.0;
                acc += e * float4(1.0, v / ln, r);
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
                float3 L = _MainLightPosition.xyz;

                // ceu: dourado ate' ~10 graus, lilas ate' ~25, azul-violeta no alto (o lerp direto dourado->azul dava bege)
                float g = sqrt(saturate(d.y));
                half3 col = lerp((half3)_Horizonte.rgb, (half3)_Meio.rgb, (half)smoothstep(0.2, 0.7, g));
                col = lerp(col, (half3)_Topo.rgb, (half)smoothstep(0.5, 1.0, g));

                float sund = max(dot(d, L), 0.0);
                float cosE = max(length(d.xz), 1e-4);
                // brasa do lado do sol, baixa (o horizonte embaixo do sol esquenta, o oposto fica lilas) + halo largo
                float az = saturate(dot(d.xz, L.xz) / (cosE * length(L.xz) + 1e-4));
                float g1 = 1.0 - g;
                col += half3(0.55, 0.28, 0.10) * (half)(az * az * az * g1 * g1 * 0.35 + pow(sund, 5.0) * 0.22);
                // sol: disco com borda de 2 px (em HDR estoura e o bloom pega) + halo
                col += half3(1.0, 0.9, 0.7) * (half)(smoothstep(0.99925, 0.99945, sund) * 2.5);
                col += half3(1.0, 0.75, 0.45) * (half)(pow(sund, 9.0) * 0.35 + pow(sund, 90.0) * 0.45);

                // ---- CIRROS (so' acima de ~8 graus: abaixo o fade e' zero)
                if (d.y > 0.14)
                {
                    float2 u = d.xz * (CIRRO_ESCALA / (d.y + 0.15));
                    float2 p = float2(dot(u, float2(0.8, 0.6)), dot(u, float2(-0.6, 0.8))) * float2(0.22, 2.4);   // esticado no vento
                    p.x += _Time.y * _Vento * 2.0;
                    float mancha = Ruido(p * float2(0.5, 0.2) + float2(5.0, 3.0));   // onde ha' cirro (em grupos) + a dobra da estria
                    p.y += mancha * 2.2;
                    float n = Ruido(p) * 0.55 + Ruido(p * 2.1 + float2(17.0, 11.0)) * 0.28 + Ruido(p * 4.3 + float2(31.0, 7.0)) * 0.17;
                    n *= smoothstep(0.42, 0.8, mancha) * 1.25;
                    float ac = (smoothstep(0.58, 0.61, n) * 0.6 + smoothstep(0.69, 0.71, n) * 0.4) * smoothstep(0.14, 0.4, d.y) * _CirroForca;
                    half3 cc = lerp((half3)_Cirro.rgb, (half3)_NuvemRim.rgb * 1.15, (half)(sund * sund * sund));
                    col = lerp(col, cc, (half)ac);
                }

                // faixa do horizonte na cor da nevoa: o mar nevoado encosta no ceu sem linha. ABAIXO do
                // horizonte nao existe "chao" de skybox: e' o mar alem do far, que a nevoa ja' engoliu —
                // do castelo (320 m) o mar acaba no far a ~18 graus abaixo da linha do horizonte.
                float faixa = (1.0 - smoothstep(0.0, 0.12, abs(d.y))) * _NevoaNoHorizonte;
                float abaixo = 1.0 - smoothstep(-0.03, 0.0, d.y);
                col = lerp(col, (half3)unity_FogColor.rgb, (half)max(faixa, abaixo));

                // ---- BANDA DE CUMULOS (ramo: o cumulo mais alto termina em base + 1,78 raio)
                float base = _AlturaBanda;
                float rmax = (TAU / CELULAS) * _TamanhoNuvem;
                if (d.y > -0.02 && d.y < base + 1.85 * rmax)
                {
                    float w = TAU / CELULAS;
                    float x = atan2(d.x, d.z) + frac(_Time.y * _Vento / TAU) * TAU;   // azimute + vento (fecha a volta)
                    float ci = floor(x / w);
                    float sd = 1.0;
                    float4 acc = 0.0;
                    [unroll]
                    for (int k = -1; k <= 1; k++)
                    {
                        float c = ci + k;
                        float im = Modp(c, CELULAS);
                        float3 h = H31(im + 0.37);
                        float3 hb = H31(im + 19.1);
                        float x0 = (c + 0.5 + 0.3 * (h.x - 0.5)) * w;
                        float s = smoothstep(1.0 - _Cobertura, 1.3 - _Cobertura, Env1(x0, 7.0, 1.7) * 0.6 + Env1(x0, 17.0, 5.3) * 0.4);
                        float r0 = rmax * saturate(s * 1.2 - 0.2) * (0.7 + 0.3 * h.y);
                        if (r0 > 1e-4)   // celula vazia (~40% delas): pula os 4 lobulos. ponytail: o ramo e' coerente (9 graus = ~400 px)
                        {
                            float y0 = base + r0 * (0.25 * h.z - 0.15);   // o equador do corpo fica ~na base: domo com canto, sem paredao
                            Lobulo(x, d.y, cosE, x0, y0, r0, sd, acc);                                   // corpo
                            float r1 = r0 * (0.5 + 0.15 * hb.x) * smoothstep(0.35, 0.8, s);
                            float x1 = x0 + (hb.y - 0.5) * r0 * 0.9;
                            float y1 = y0 + r0 * 0.8;
                            Lobulo(x, d.y, cosE, x1, y1, r1, sd, acc);                                   // torre
                            float a2 = 0.7 + 1.7 * hb.z;                                                 // 40..140 graus
                            Lobulo(x, d.y, cosE, x1 + cos(a2) * r1 * 0.8, y1 + sin(a2) * r1 * 0.8, r1 * 0.55, sd, acc);   // couve-flor
                            float a3 = (x1 > x0 ? 2.25 : 0.9) + (h.y - 0.5) * 0.4;                      // ombro: no alto, do outro lado
                            Lobulo(x, d.y, cosE, x0 + cos(a3) * r0 * 0.78, y0 + sin(a3) * r0 * 0.78, r0 * 0.42, sd, acc);
                        }
                    }

                    // normal "inflada": o gradiente medio (sem normalizar: no vinco |g|<1 e ela olha a camera, sem pinta)
                    // deitado na borda, de frente no miolo; chapada perto da base (sem o risco vertical do lado do lobulo)
                    float Si = 1.0 / max(acc.x, 1e-20);
                    float2 gr = acc.yz * Si;
                    float Rm = max(acc.w * Si, 1e-4);
                    float lat = saturate(1.0 + sd / (Rm * BOJO));
                    float fl = smoothstep(0.0, 1.0, saturate((d.y - base) / (Rm * 0.5)));
                    float2 nxy = gr * (lat * fl);
                    float nz = sqrt(saturate(1.0 - dot(nxy, nxy)));

                    // o sol no referencial local da banda: x = direita no azimute, y = cima local, z = para a camera
                    float Lx = (L.x * d.z - L.z * d.x) / cosE;
                    float Ly = cosE * L.y - d.y * (d.x * L.x + d.z * L.z) / cosE;
                    float Lz = -dot(d, L);
                    float contra = saturate(-Lz);
                    // o lobulo e' modelado como se o sol viesse um pouco da frente (legivel em qualquer azimute);
                    // contra o sol a nuvem e' silhueta: pouco modelado de lado, escurece, e so' a borda acende
                    float lfz = 0.35 + 0.5 * max(Lz, 0.0);
                    float2 ls = float2(Lx * (1.0 - 0.8 * contra), Ly * (1.0 - 0.5 * contra));
                    float ndl = dot(float3(nxy, nz), float3(ls, lfz)) * rsqrt(dot(ls, ls) + lfz * lfz);
                    // a base escura sobe no raio LOCAL (Rm): o cumulo pequeno tambem ganha topo aceso, nao vira seixo rosa
                    float luz = (ndl * 0.5 + 0.5) * (0.5 + 0.5 * saturate((d.y - base) / (Rm * ALTO_AO)));
                    half t1 = (half)smoothstep(CORTE1, CORTE1 + FAIXA_TOON, luz);
                    half t2 = (half)smoothstep(CORTE2, CORTE2 + FAIXA_TOON, luz);
                    half3 sombra = (half3)_NuvemSombra.rgb, meio = (half3)_NuvemMeio.rgb;
                    half3 cn = lerp(lerp(sombra, meio, t1), (half3)_Nuvem.rgb, t2);
                    // volume dentro da faixa: topo um pouco mais claro, borda do lobulo um tico mais escura
                    cn *= (half)((0.88 + 0.12 * saturate((d.y - base) / (Rm * 1.2))) * (0.94 + 0.06 * nz));
                    cn = lerp(cn, lerp(sombra, meio, 0.5), (half)(contra * 0.5));
                    // borda de luz: so' na silhueta, do lado do sol, forte contra o sol (a borda de prata)
                    float lado = saturate(dot(gr, float2(Lx, Ly)) / ((length(float2(Lx, Ly)) + 1e-4) * (length(gr) + 1e-6)));
                    float rim = saturate(1.0 + sd / RIM_LARGURA) * smoothstep(0.2, 0.6, lado) * (0.3 + 0.7 * contra)
                              * saturate((d.y - base) / 0.004) * _RimForca;
                    cn = lerp(cn, (half3)_NuvemRim.rgb * 1.3, (half)saturate(rim));
                    // a nuvem longe tambem pega a nevoa do horizonte, menos que o ceu (a base fica legivel)
                    cn = lerp(cn, (half3)unity_FogColor.rgb, (half)(faixa * _NevoaNaNuvem));
                    float an = saturate(0.5 - max(sd, base - d.y) / BORDA_AA);   // base cortada reta
                    col = lerp(col, cn, (half)an);
                }
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Skybox/Procedural"
}
