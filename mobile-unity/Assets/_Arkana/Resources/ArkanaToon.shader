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
        // CHAO VIVO (pedra da 5A + solo da 7B): so' o material do terreno liga (Ilha.MaterialTerreno). Tons em sRGB, MULTIPLICAM a cor.
        _Chao("Chao vivo (so' terreno)", Float) = 0
        _EscalaManchaPedra("Escala da mancha de pedra (1/m)", Float) = 0.09
        // sobrios de proposito: o sol do entardecer ja' puxa tudo pro laranja (terra forte virava laranja, liquen virava limao)
        _TomGasta("Tom: pedra gasta", Color) = (0.74, 0.73, 0.76, 1)
        _TomTerra("Tom: terra quente", Color) = (0.97, 0.88, 0.78, 1)
        _TomLiquen("Tom: liquen", Color) = (0.74, 0.86, 0.66, 1)
        _Estrato("Estratos por metro (encosta)", Float) = 0.6
        _Fissura("Forca da fissura", Range(0, 1)) = 0.6
        // SOLO VIVO (onda 7B, o mesmo _Chao): campina, mata, areia e chao pisado, pela composicao que a Ilha grava no UV0.
        // Tons em sRGB multiplicando a cor, como os da pedra (acima de 1 clareia). A terra batida e' COR (verde nao vira marrom
        // multiplicando).
        _TomFria("Tom: grama funda e fria", Color) = (0.72, 0.84, 0.90, 1)
        _TomQuente("Tom: grama clara e quente", Color) = (1.12, 1.06, 1.08, 1)
        _TomMata("Tom: chao de mata", Color) = (0.74, 0.76, 0.72, 1)
        _TomMusgo("Tom: musgo", Color) = (1.00, 1.14, 0.80, 1)
        _CorTerraBatida("Cor: terra batida", Color) = (0.55, 0.48, 0.40, 1)
        _TomMolhada("Tom: areia molhada", Color) = (0.80, 0.78, 0.80, 1)
        _Pintado("Pintado de perto (trevo, flor, folha, concha)", Range(0, 1)) = 1
        // PRACA DAS RUINAS (onda 9B, o mesmo _Chao): o calcamento do plato. _Ruinas = centro x, centro z, raio e a borda (fracao
        // do raio onde a lingua comeca) — a Ilha grava do Relevo (Ilha.PracaDasRuinas); raio 0 = sem praca. Cores em sRGB,
        // EXPLICITAS (a praca passa da borda do disco de cor do vertice, por cima do verde).
        _Ruinas("Praca das ruinas (x, z, raio, borda)", Vector) = (0, 0, 0, 0.92)
        _CorLaje("Cor: laje", Color) = (0.58, 0.565, 0.54, 1)
        _CorJunta("Cor: junta", Color) = (0.24, 0.21, 0.17, 1)
        _CorMusgoRuina("Cor: musgo da praca", Color) = (0.36, 0.48, 0.26, 1)
        _CorPoeira("Cor: poeira", Color) = (0.80, 0.74, 0.64, 1)
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
            float4 _TomFria;
            float4 _TomQuente;
            float4 _TomMata;
            float4 _TomMusgo;
            float4 _CorTerraBatida;
            float4 _TomMolhada;
            float _Pintado;
            float4 _Ruinas;
            float4 _CorLaje;
            float4 _CorJunta;
            float4 _CorMusgoRuina;
            float4 _CorPoeira;
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
                float4 solo : TEXCOORD0;   // so' o terreno grava (Relevo.Solo); malha sem UV le' o padrao e _Chao 0 nem olha
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variantes
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                float fog : TEXCOORD3;
                float4 solo : TEXCOORD4;
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

            // o mesmo value noise com o GRADIENTE analitico (por unidade de p): a trilha mede em METROS a distancia ate' a
            // isolinha. So' o limiar em |n - 0,5| fazia bolha de 20 m onde o ruido fica perto de 0,5 (sela) — virou clareira.
            float RuidoG(float2 p, out float2 g)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = H21(i), b = H21(i + float2(1.0, 0.0)), c = H21(i + float2(0.0, 1.0)), d = H21(i + float2(1.0, 1.0));
                float k = a - b - c + d;
                g = 6.0 * f * (1.0 - f) * float2(b - a + k * u.y, c - a + k * u.x);
                return a + (b - a) * u.x + (c - a) * u.y + k * u.x * u.y;
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

            // Voronoi F2-F1, em unidade de celula: ~0 na BORDA entre duas celulas, que e' onde a pedra racha. `centro` = o ponto
            // da celula mais perto (o miolo da laje da praca; a fissura nao usa e o compilador corta).
            float BordaDeCelula(float2 p, out float2 centro)
            {
                float2 i = floor(p);
                float2 f = p - i;
                float d1 = 8.0;
                float d2 = 8.0;
                centro = p;
                [unroll] for (int y = -1; y <= 1; y++)
                {
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        // ponto longe da quina da celula: sem lasca fina demais entre duas fissuras
                        float2 r = g + 0.15 + 0.7 * H22(i + g) - f;
                        float d = dot(r, r);
                        d2 = min(d2, max(d1, d));
                        centro = d < d1 ? p + r : centro;
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
                    float2 nada;
                    float e = BordaDeCelula(cf, nada) / 0.5;
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

            // PRACA DAS RUINAS (_Chao, onda 9B): o plato das ruinas era um disco cinza liso do alto e cinza chapado entre os arcos.
            // Agora: lajes irregulares de ~0,9 m (a rede de Voronoi da fissura, com o miolo de cada laje), junta escura, laje que
            // falta (terra e capim), musgo entrando pela junta e tomando laje na borda, poeira clara e um medalhao gasto no centro
            // (disco e anel claros, estrela de 8 raios, meio-fio). A cor e' EXPLICITA: o calcamento passa da borda do disco de cor
            // do vertice (Relevo: ate' 0,90 do raio) em lingua por cima do verde; a do vertice so' vale no minimapa e sem o shader
            // (Relevo.CorPraca e' a media disto). `borda` = onde a lingua comeca (em raios, >= _Ruinas.w: so' vai para fora);
            // `laje` = quanto o pixel e' praca. Longe (pegada > ~0,45 m) nada de laje: tom, junta, chanfro, musgo e buraco viram a
            // FRACAO media (sem moire'); de perto, cada laje sorteia (tres sorteios tirados do mesmo hash) contra essa fracao.
            // `v` = o ruido de 5 m do _Mancha lido no miolo da laje de perto e no pixel longe: a mesma mancha nas duas distancias.
            // ponytail: com _Mancha 0 (m = 0) a praca de longe perde buraco e musgo de mancha; o terreno liga 0,10 — se desligar, ler v aqui.
            // Custo (fxc, passe com sombra: 1552 -> 2032 slots no total): fora de 1,5 raio das ruinas so' o teste de distancia (~6);
            // perto delas +22 (a borda); o pixel da praca longe ~840 e perto ~1110 (o Voronoi 3x3 com o miolo + o ruido no miolo:
            // +271), acima da pedra perto da 5A (~985). _Fissura 0 corta o perto (a praca inteira fica na versao de longe).
            float3 PisoDasRuinas(float3 col, float2 xz, float pegada, float grao, float m, float a, float borda, out float laje)
            {
                float2 c = _Ruinas.xy;
                // _Fissura 0 (o corte de custo da 5A) tira TODO Voronoi do chao: a praca fica com a versao de longe
                float fino = _Fissura > 0.0 ? 1.0 - smoothstep(0.18, 0.45, pegada) : 0.0;
                float2 pc = xz;
                float v = m, h = 0.5, em = 0.45, tom = 1.0, jun = 0.12, bev = 0.95;   // as medias (longe)
                [branch] if (fino > 0.0)
                {
                    float2 miolo;
                    // a dobra do grao (+-15 cm) entorta a junta: laje assentada ha' seculos nao e' regua
                    float f21 = BordaDeCelula(xz / 0.9 + (grao - 0.5) * 0.35, miolo);
                    em = f21 * 0.45;                                        // metros ate' a junta (F2-F1 ~ 2x, celula de 0,9 m)
                    pc = miolo * 0.9;
                    v = Ruido(pc * _EscalaMancha);
                    h = H21(miolo + 71.0);
                    tom = lerp(0.80, 1.16, h);
                    float jl = max(0.03, pegada * 0.9);                     // meia junta de 3 cm: alarga e clareia com a pegada
                    jun = (1.0 - smoothstep(jl * 0.5, jl, em)) * (0.03 / jl);
                    bev = 1.0 - 0.22 * (1.0 - smoothstep(0.0, 0.16, em));   // quina gasta: a laje escurece na beira
                }
                v = lerp(m, v, fino);
                pc = lerp(xz, pc, fino);
                float2 oc = pc - c;
                float rm = length(oc);
                float d = rm / _Ruinas.z;
                // 1) BORDA: de perto decide por laje inteira (com sorteio: laje solta na grama, falha na beira); longe, rampa de ~4 m
                float sw = lerp(0.05, 0.004, fino);
                laje = 1.0 - smoothstep(-sw, sw, d + (h - 0.5) * 0.06 * fino - borda - 0.05);

                // 2) O TEMPO: musgo pela borda e pela mancha fria da campina (a baixo), laje que falta onde a mancha de 5 m sobe e
                //    na beira, poeira na mancha quente e no miolo pisado. O medalhao (0,23 do raio) fica limpo. KNOB: as fracoes.
                float med = 1.0 - smoothstep(0.21, 0.25, d);
                float musgoReg = saturate(smoothstep(0.55, 1.0, d + (0.5 - a) * 0.35 + (m - 0.5) * 0.25) * 0.85 + (0.42 - a) * 1.2) * (1.0 - med);
                float poeira = saturate(smoothstep(0.52, 0.70, a) * 0.6 + med * 0.35) * (1.0 - musgoReg);
                float pb = saturate((v - 0.58) * 0.75 + smoothstep(0.65, 1.1, d) * 0.2) * (1.0 - med);   // fracao de laje que falta
                float pm = saturate((v - 0.5) * 0.9 + musgoReg * 0.7) * (1.0 - med);                   // fracao de musgo
                float entra = 0.12 * pm;                                                                // o musgo que entra pela junta (m)
                float buraco = lerp(pb, step(1.0 - pb, frac(h * 13.7 + 0.31)), fino);
                // de perto: o fio que entra pela junta e a mancha do grao (~1 m) que cresce a partir dela — laje inteira verde lia azulejo
                float musgo = lerp(saturate(pm * 0.55 + entra * 3.5),
                                   max(1.0 - smoothstep(0.0, 0.03, em - entra + (grao - 0.5) * 0.06),
                                       smoothstep(0.66, 0.74, grao * 0.6 + pm * 0.5 - em * 0.6)), fino);

                // 3) MEDALHAO (8,5 m): disco e anel claros, estrela de 8 raios sobre fundo escuro — por laje de perto, gasto em trecho
                //    (o mesmo v), some longe. cos(8 ang) sem atan2: tres duplicacoes de angulo. O meio-fio e' curva: raio do PIXEL.
                float2 u = oc / max(rm, 1e-3);
                float c2 = u.x * u.x - u.y * u.y;
                float c4 = 2.0 * c2 * c2 - 1.0;
                float c8 = 2.0 * c4 * c4 - 1.0;
                float ea = lerp(0.35, 0.02, fino);
                float disco = 1.0 - smoothstep(2.1 - ea, 2.1 + ea, rm);
                float anel = smoothstep(4.0 - ea, 4.0 + ea, rm) * (1.0 - smoothstep(5.2 - ea, 5.2 + ea, rm));
                float estrela = smoothstep(2.1 - ea, 2.1 + ea, rm) * (1.0 - smoothstep(7.7 - ea, 7.7 + ea, rm)) * (1.0 - anel);
                float raio8 = smoothstep(0.2, 0.5, c8);   // raio de ~17 graus: cabe laje inteira
                float vis = (1.0 - smoothstep(0.6, 1.2, pegada)) * (1.0 - 0.45 * smoothstep(0.5, 0.8, v));
                float claro = (disco + anel + estrela * raio8) * vis;   // pedra clara: disco, anel e os raios
                float escuro = estrela * (1.0 - raio8) * vis;           // o campo da estrela, em pedra escura e fria
                tom = lerp(1.0, tom, 1.0 - 0.6 * med) * (1.0 + 0.30 * claro - 0.40 * escuro);   // laje do medalhao mais parelha
                float mf = abs(length(xz - c) - 8.3);
                float jmf = max(0.03, pegada * 0.9);
                float fio = (1.0 - smoothstep(0.25, 0.25 + jmf, mf)) * (1.0 - smoothstep(0.3, 0.6, pegada));
                tom = lerp(tom, 1.15, fio);
                bev = lerp(bev, 1.0, fio);
                jun = lerp(jun, (1.0 - smoothstep(jmf * 0.5, jmf, abs(mf - 0.25))) * (0.03 / jmf), fio);

                // 4) a COR: laje (tom x chanfro, matiz frio/quente por laje), poeira, musgo, junta (musgo e poeira entram nela) e o
                //    buraco (terra com capim)
                float3 verde = _CorMusgoRuina.rgb * (0.7 + 0.6 * grao);
                float3 p = _CorLaje.rgb * (tom * bev) * lerp(float3(0.95, 0.98, 1.05), float3(1.04, 1.0, 0.94), frac(h * 5.3))
                         * lerp(1.0, float3(0.90, 0.95, 1.08), escuro);
                p = lerp(p, _CorPoeira.rgb, poeira * (0.2 + 0.4 * grao));
                p = lerp(p, verde, musgo);
                float3 junta = lerp(_CorJunta.rgb, verde * 0.75, saturate(musgoReg * 1.2 + (v - 0.5) * 1.5));
                junta = lerp(junta, verde, musgo * saturate(pm * 2.0 - 0.6));   // musgo fechado cobre a junta: sem azulejo verde
                junta = lerp(junta, _CorPoeira.rgb * 0.75, poeira * 0.4);
                p = lerp(p, junta, saturate(jun));
                float3 terra = _CorTerraBatida.rgb * (0.62 + 0.3 * grao) * (0.7 + 0.3 * smoothstep(0.0, 0.15, em));   // afundada: a beira na sombra da laje
                p = lerp(p, lerp(terra, verde, smoothstep(0.55, 0.8, grao + musgoReg * 0.25)), buraco);
                return lerp(col, p, laje);
            }

            // SOLO VIVO (_Chao, onda 7B): o chao que NAO e' pedra — campina, borda e chao da mata, praia e duna, e o chao
            // pisado dos nascimentos e das ruinas. `k` = a composicao que a Ilha grava no UV0 (Relevo.Solo): r mata, g areia,
            // b pisado, a grama; `solo` = 1 - pedra. O contrato do TomDaPedra: tom MULTIPLICA a cor do vertice (o bioma segue
            // mandando e a transicao herda a rampa dele) e tudo passa pela `pegada` antes de virar chuvisco/moire'. So' a terra
            // batida TROCA a cor.
            // Custo (fxc, passe com sombra: 984 -> 1552 slots no total): +161 em todo pixel que nao e' pedra (a campina), +27 na
            // mata, +128 na areia, +107 no chao pisado, +142 a menos de ~15 m do olho (o pintado). O pior pixel (grama pisada
            // perto, ~880) fica abaixo da pedra perto da 5A (~985). _Pintado 0 e' o corte de custo.
            float3 SoloVivo(float3 col, float3 p, float4 k, float solo, float pegada, float grao, float m)
            {
                float2 xz = p.xz;
                k *= solo;
                float nitido = 1.0 - smoothstep(0.25, 0.9, pegada);   // o grao de ~1 m ainda cabe no pixel
                float roe = (grao - 0.5) * 0.16 * nitido;             // borda de mancha roida (sem ela e' aerografo)
                float longe = smoothstep(3.0, 8.0, pegada);           // bem longe e rasante, a mancha grande vira o tom medio

                // 1) CAMPINA: manchas de ~25 m, verde fundo e frio x claro e quente, dobradas por um ruido de ~50 m.
                //    O limao chapado era a cor do vertice sozinha sob o sol quente (fotos 10 e 28).
                float2 q = xz + (Ruido(xz * 0.02 + 7.3) - 0.5) * float2(18.0, -13.0);
                float a = Fbm2(q * 0.04) + roe;
                float fria = 1.0 - smoothstep(0.31, 0.44, a);
                float quente = smoothstep(0.57, 0.71, a);
                float3 tom = Tom(_TomFria.rgb, fria) * Tom(_TomQuente.rgb, quente);
                tom = lerp(tom, Tom(_TomFria.rgb, 0.3) * Tom(_TomQuente.rgb, 0.3), longe);
                float3 t = Tom(tom, k.a);
                // a PRACA das ruinas (onda 9B): a borda anda em lingua entre _Ruinas.w e w + 0,4 do raio, pela mancha de 25 m (a)
                // e a de 5 m (m), ja' pagas. `praca` < 0 = dentro da lingua; onde a praca e' certa o pisado nem roda. Alem de 1,5
                // raio (a lingua mais comprida + a rampa acaba em ~1,45) nao se paga nada; raio 0 (sem praca) tambem cai fora.
                float praca = 1.0, bordaPraca = 1.0;
                float2 op = xz - _Ruinas.xy;
                [branch] if (dot(op, op) < _Ruinas.z * _Ruinas.z * 2.25)
                {
                    bordaPraca = _Ruinas.w + 0.40 * smoothstep(0.25, 0.75, saturate(a * 1.4 - 0.2) * 0.55 + m * 0.45);
                    praca = length(op) / _Ruinas.z - bordaPraca;
                }

                // 2) MATA: a borda entra na campina em lingua (a mancha de 5 m quebra o anel da regiao); dentro, humus mais
                //    escuro x musgo claro, na mistura da mancha de 25 m com a de 5 m — nenhum ruido novo.
                //    ponytail: _Mancha 0 deixa m = 0 e a borda so' encolhe
                float mata = smoothstep(0.2, 0.75, k.r + (m - 0.5) * 0.5 * solo);
                [branch] if (mata > 0.001)
                {
                    float hm = a * 0.5 + m * 0.5 + roe;
                    float musgo = smoothstep(0.54, 0.64, hm);
                    float humus = 1.0 - smoothstep(0.36, 0.46, hm);
                    float3 tm = _TomMata.rgb * Tom(_TomMata.rgb, humus * 0.8) * Tom(_TomMusgo.rgb, musgo);
                    tm = lerp(tm, _TomMata.rgb * Tom(_TomMata.rgb, 0.2) * Tom(_TomMusgo.rgb, 0.25), longe);
                    t *= Tom(tm, mata);
                }

                // 3) AREIA: a borda molhada NITIDA pela cota do pixel (o vertice de 4,5 m so' dava degrade), o fio de sal logo
                //    acima dela e a duna mais clara; de perto, a ondinha de vento (~0,55 m) paralela a' crista das dunas.
                //    KNOB: a cota da molhada (0,5 m) e a largura do fio, por foto.
                [branch] if (k.g > 0.01)
                {
                    float lim = 0.5 + (Ruido(xz * 0.23 + 3.0) - 0.5) * 0.4;
                    float molhada = 1.0 - smoothstep(lim - 0.06, lim + 0.02, p.y);
                    float fio = (1.0 - smoothstep(0.02, 0.07, abs(p.y - lim - 0.09))) * saturate(1.0 - pegada * 4.0);
                    float seca = smoothstep(1.3, 2.8, p.y);
                    float w = frac(dot(xz, float2(0.62, 0.78)) * 1.8 + Ruido(xz * 0.45) * 1.6);
                    float ond = smoothstep(0.6, 0.9, abs(w - 0.5) * 2.0) * saturate(1.0 - pegada * 5.0) * seca;   // 4 px por onda
                    // + a mancha de 25 m da campina, fraca: areal inteiro de uma cor so' lia como chao de estudio
                    float3 ta = Tom(_TomMolhada.rgb, molhada) * (1.0 + 0.12 * seca - 0.18 * ond + 0.25 * fio + (a - 0.5) * 0.16);
                    t *= Tom(ta, k.g);
                }
                col *= t;

                // 4) PISADO: TRECHOS de terra batida de 2-5 m (a mancha de 5 m com o grao de 1 m) no miolo do nascimento e na
                //    chegada das ruinas, e a TRILHA: a isolinha 0,5 de um ruido de ~25 m = caminho sinuoso de ~1 m cheio, medido
                //    em metros pelo gradiente. As duas com borda de grama gasta (a mistura com o verde). Longe, viram a cobertura
                //    media (nada de fio piscando). So' na grama e na mata: areia, lama e pedra nao se pisam aqui.
                float chao = saturate(k.a + k.r);
                float terra = 0.0;
                [branch] if (k.b * chao > 0.01 && praca > -0.12)
                {
                    float n = m * 0.45 + lerp(0.5, grao, nitido) * 0.55;
                    float lb = 0.64 + (1.0 - k.b) * 0.6;   // o miolo abre ~1/5 do chao em trecho; a 15 m do nascimento, quase nada
                    float borda = smoothstep(0.08, 0.35, k.b);
                    float2 gt;
                    float tn = RuidoG(xz * 0.04 + 91.0, gt);
                    // metros ate' o eixo; o piso do gradiente (~1/3 do tipico) segura a sela, onde a conta explodiria
                    float dt = abs(tn - 0.5) / max(length(gt) * 0.04, 0.01) + (grao - 0.5) * 0.3 * nitido;
                    // longe os dois viram a cobertura MEDIA: a mancha de 5 m, amostrada a 5 m por pixel, virava clareira inteira
                    float vago = smoothstep(0.5, 2.0, pegada);
                    float bat = lerp(smoothstep(lb - 0.07, lb + 0.02, n), 0.12 * smoothstep(0.7, 1.0, k.b), vago);
                    float tri = lerp(1.0 - smoothstep(0.45, 1.1, dt), 0.06, vago) * borda;
                    terra = saturate(max(bat, tri)) * chao;
                    // sob a copa a terra e' escura (humus): a batida clara no meio da mata lia como clareira de areia
                    col = lerp(col, _CorTerraBatida.rgb * (0.85 + 0.3 * grao) * (1.0 - 0.45 * mata), terra * 0.92);
                }

                // 4b) PRACA DAS RUINAS por cima de tudo (a cor dela e' propria). Custo no comentario do PisoDasRuinas.
                float laje = 0.0;
                [branch] if (praca < 0.13)
                {
                    col = PisoDasRuinas(col, xz, pegada, grao, m, a, bordaPraca, laje);
                }

                // 5) DE PERTO, pintado a mao: trevo (tres folhas de ~4 cm a 120 graus) em cacho de ~3 m e flor miuda na grama,
                //    folha caida na mata, concha na areia — a MESMA celula de 0,4 m, a cor sai da composicao. Some a ~15 m do
                //    olho. _Pintado 0 pula o bloco inteiro: corte de custo se o FPS cair no aparelho. Na laje da praca, nada.
                float perto = saturate(1.0 - pegada * 10.0) * (1.0 - terra) * (1.0 - laje);
                [branch] if (perto * _Pintado > 0.0)
                {
                    float2 sc = xz / 0.4;
                    float2 si = floor(sc);
                    float2 sh = H22(si + 57.0);
                    float2 d = sc - si - (0.3 + 0.4 * sh);
                    // giro pelo hash, sem seno: um vetor sorteado normalizado e as duas rotacoes fixas de 120 graus
                    float2 o = normalize(sh.yx - 0.5 + 0.001) * 0.11;
                    float2 o2 = float2(-0.5 * o.x - 0.866 * o.y, 0.866 * o.x - 0.5 * o.y);
                    float2 o3 = float2(-0.5 * o.x + 0.866 * o.y, -0.866 * o.x - 0.5 * o.y);
                    float r2 = min(dot(d - o, d - o), min(dot(d - o2, d - o2), dot(d - o3, d - o3)));
                    float cacho = smoothstep(0.5, 0.68, Ruido(xz * 0.33 + 23.0));
                    float tem = step(frac((sh.x + sh.y) * 5.13), lerp(0.06, 0.85, cacho));
                    float trevo = (1.0 - smoothstep(0.009, 0.014, r2)) * tem * k.a * perto;
                    col *= 1.0 - trevo * float3(0.34, 0.20, 0.24);   // folha mais funda e mais fria que a grama em volta
                    // flor / folha caida / concha: um ponto de ~4 cm noutro canto da celula (mais no trevo e na mata)
                    float2 fh = H22(si + 211.0);
                    float2 fd = sc - si - (0.15 + 0.7 * fh);
                    float fr = dot(fd, fd);
                    float taxa = (0.05 + 0.22 * cacho) * k.a + 0.12 * k.r + 0.05 * k.g;
                    float ponto = (1.0 - smoothstep(0.007, 0.011, fr)) * step(frac(fh.x * 9.7 + fh.y), taxa) * perto;
                    // cores ja' LINEARES: branco, dourado arcano e rosa das petalas do Grama (#f5f0ff, #f0c75e, #ff8ab3)
                    float3 flor = fh.y < 0.5 ? float3(0.91, 0.87, 0.95) : (fh.x < 0.5 ? float3(0.87, 0.57, 0.11) : float3(0.95, 0.30, 0.45));
                    flor = lerp(flor, float3(0.95, 0.62, 0.08), 1.0 - smoothstep(0.0008, 0.0018, fr));   // miolo amarelo
                    float3 folha = fh.x < 0.5 ? float3(0.24, 0.12, 0.05) : float3(0.36, 0.22, 0.08);   // folha seca apagada: laranja vivo virava confete
                    float3 cp = k.r > k.a ? folha : flor;
                    cp = k.g > max(k.a, k.r) ? float3(0.90, 0.78, 0.70) : cp;
                    col = lerp(col, cp, ponto);
                }
                return col;
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
                o.solo = v.solo;
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
                // SOLO VIVO: tudo o que nao e' pedra (a pedra pura nao paga). Antes da textura: a terra batida tambem ganha o
                // detalhe. Custo no comentario do SoloVivo.
                [branch] if (_Chao > 0.0 && pedra < 0.997)
                {
                    col = SoloVivo(col, i.positionWS, i.solo, 1.0 - pedra, pegada, grao, m);
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
