using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arkana.World
{
    /// <summary>
    /// Sorteio deterministico (xorshift32). Existe para a ilha, a floresta e a rota do castelo
    /// nao dependerem do System.Random do runtime: mesmo seed, mesma ilha, em qualquer aparelho.
    /// </summary>
    public sealed class Sorteio
    {
        uint estado;

        public Sorteio(int seed)
        {
            // zero mata o xorshift; mistura fixa para seed 0 tambem render.
            estado = (uint)seed * 2654435761u + 0x9E3779B9u;
            if (estado == 0) estado = 0x1234567u;
        }

        public uint Proximo()
        {
            uint x = estado;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            estado = x;
            return x;
        }

        /// <summary>[0, 1)</summary>
        public float Float() => (Proximo() >> 8) * (1f / 16777216f);

        public float Faixa(float a, float b) => a + (b - a) * Float();

        /// <summary>[0, n)</summary>
        public int Int(int n) => n <= 0 ? 0 : (int)(Proximo() % (uint)n);
    }

    /// <summary>Um ponto de interesse: centro (XZ, metros) e raio como FRACAO do raio de terra.</summary>
    public sealed class Poi
    {
        public readonly string Nome;
        public readonly Vector2 Centro;
        /// <summary>Raio em metros = RaioFracao x RaioTerra. Metro cravado envelhece calado.</summary>
        public readonly float Raio;
        public readonly float RaioFracao;

        public Poi(string nome, Vector2 centro, float raioFracao, float raioTerra)
        {
            Nome = nome;
            Centro = centro;
            RaioFracao = raioFracao;
            Raio = raioFracao * raioTerra;
        }
    }

    public enum Bioma { Campina, Floresta, Ruinas, Alagado, Dunas, Praia, Rocha, Pico, Agua }

    /// <summary>
    /// A matematica da ilha, PURA (sem MonoBehaviour): relevo, agua, POIs, nascimentos, cor.
    /// Porte de mobile-godot/godot/world/Island.gd. TUDO na horizontal sai de `Escala`
    /// (1.0 = a ilha de 300 m; 2.0 = 600 m, raio de terra 264 m). A VERTICAL nao escala:
    /// dobrar a altura junto tornaria toda ladeira insubivel.
    ///
    /// ONDE DIFERE DO GODOT: o Godot usa FastNoiseLite (simplex suave, seed 7, freq 0.02).
    /// Aqui o ruido e' um Perlin proprio (Ruido), com a MESMA frequencia, os MESMOS offsets
    /// e as MESMAS amplitudes — a estrutura da ilha (POIs, plato, pico, dunas, fiorde,
    /// costa irregular) e' identica; a forma exata das colinas nao e'.
    /// </summary>
    public sealed class Relevo : Arkana.Gameplay.IRelevo
    {
        // ---- layout em METROS-BASE (a ilha de 300 m). O mundo e' isto x Escala. ----
        public const float BaseLado = 300f;
        public const float BaseRaioTerra = 132f;
        static readonly Vector2 BLago = new Vector2(75, 30);
        const float BLagoR = 24f;
        static readonly Vector2 BAlagado = new Vector2(-70, 60);
        const float BAlagadoR = 22f;
        const float BLagoDiscoR = 29f;
        const float BAlagadoDiscoR = 22f;
        static readonly Vector2 BFloresta = new Vector2(-60, -66);
        const float BFlorestaR = 40f;
        static readonly Vector2 BRuinas = new Vector2(63, -73);
        const float BRuinasR = 20f;
        static readonly Vector2 BPico = new Vector2(12, 84);
        const float BPicoR = 40f;
        const float BPicoTopo = 14f;
        static readonly Vector2 BDunas = new Vector2(10, -98);
        const float BDunasR = 32f;
        static readonly Vector2 BEnseadaA = new Vector2(-142, 30);
        static readonly Vector2 BEnseadaB = new Vector2(-84, -4);
        const float BEnseadaW = 16f;
        const float BValeR = 50f;

        // ---- vertical: NAO escala ----
        public const float AguaY = 0f;          // nivel do mar
        public const float PraiaY = 1.4f;       // abaixo disto nada de gameplay pousa
        public const float LagoY = 0.6f;
        public const float AlagadoY = 0.55f;
        public const float PicoH = 18f;
        const float PicoFace = 4.6f;            // para onde aponta a encosta subivel (rad)
        /// <summary>Resposta de SuperficieDaAgua onde nao ha' agua.</summary>
        public const float Seco = -1e9f;

        // ---- os 14 nascimentos, em metros-base (Island.tscn) ----
        static readonly Vector2[] BNascimentos =
        {
            new Vector2(-48, -54), new Vector2(-74, -80),   // floresta
            new Vector2(43, 30), new Vector2(75, 62),       // lago
            new Vector2(63, -73), new Vector2(54, -62),     // ruinas
            new Vector2(-52, 48), new Vector2(-86, 76),     // alagado
            new Vector2(12, 84), new Vector2(20, 90),       // pico
            new Vector2(10, -98), new Vector2(-10, -88),    // dunas
            new Vector2(0, 12), new Vector2(-14, -18),      // vale
        };
        /// <summary>Dois nascimentos mais perto que isto valem um so'.</summary>
        public const float SeparacaoNascimentos = 12f;

        // ---- paleta GDD §10 (sRGB; quem grava no vertice converte para linear) ----
        /// <summary>Grama da campina. O #58bd6d/#7ed687 de antes, sob o sol quente do entardecer, saia VERDE-LIMAO chapado (fotos 10
        /// e 28 de 12/09): mais fundo e mais frio aqui, e as manchas quente/fria sao do shader (_Chao). KNOB: por foto.</summary>
        public static readonly Color CorGrama = Hex(0x559e67);
        public static readonly Color CorGramaClara = Hex(0x76b87f);
        public static readonly Color CorAreia = Hex(0xdcc9a0);
        public static readonly Color CorRocha = Hex(0x8e97ad);
        /// <summary>Pedra SOLTA (rocha da vegetacao, rochedo do mar): da familia do basalto do kit da Meshy. O lilas do CorRocha e'
        /// cor de CHAO; no pedregulho facetado, ao lado da rocha texturizada, lia como bloco de gelo (foto 03 de 12/09). KNOB: por foto.</summary>
        public static readonly Color CorPedregulho = Hex(0x6a615a);
        /// <summary>Pedra LAVRADA das ruinas (colunas e muralha de Ruinas.cs): o cinza quente e gasto dos arcos e estatuas da Meshy
        /// plantados em volta. O CorPedra azulado, ao lado deles, lia como cano de PVC (foto 14 de 12/09). KNOB: por foto.</summary>
        public static readonly Color CorRuina = Hex(0x958d82);
        /// <summary>Chao da PRACA das ruinas (onda 9B): a MEDIA do calcamento que o shader desenha de perto (laje quente, junta escura,
        /// musgo, terra da laje que falta) — e' o que o vertice mostra no minimapa, sem o shader e na leitura aerea. O CorRocha lilas
        /// de antes era o disco cinza liso das fotos 04/09. KNOB: por foto, junto com os tons da praca no ArkanaToon.</summary>
        public static readonly Color CorPraca = Hex(0x928680);
        /// <summary>Fracao do raio das ruinas onde o calcamento do shader comeca a soltar em lingua (so' para FORA, ate' ~1,3). O disco
        /// de cor do vertice acaba ANTES (0,90): nunca sobra cinza sem pedra por cima. A Ilha grava no _Ruinas.w do terreno.</summary>
        public const float BordaDaPraca = 0.92f;
        public static readonly Color CorLama = Hex(0xa8763e);
        public static readonly Color CorTronco = Hex(0x8a7259);
        public static readonly Color CorFolhaA = Hex(0x3fa85c);
        public static readonly Color CorFolhaB = Hex(0x6fd177);
        public static readonly Color CorPedra = Hex(0xb3bccf);
        public static readonly Color CorGramaSeca = Hex(0x8ab26e);
        public static readonly Color CorGramaFunda = Hex(0x2f7a52);
        /// <summary>Chao sob a copa: mais escuro e musgoso que a campina (o #5f7742 de antes lia como a mesma grama, so' suja).</summary>
        public static readonly Color CorChaoDeMata = Hex(0x4a5c35);
        public static readonly Color CorMusgo = Hex(0x74a06a);
        public static readonly Color CorSeixo = Hex(0x8d8a84);
        /// <summary>Cume do pico: pedra clara QUENTE, nao neve. O #dfe3ee de antes (branco-lilas) saia liso e o Diretor leu
        /// placeholder (fotos 02/05/18 de 12/09). KNOB: por foto — com a leitura aerea (pico x ruinas) acima de 0,10.</summary>
        public static readonly Color CorPico = Hex(0xbdb6aa);
        public static readonly Color CorDuna = Hex(0xe8d6a4);

        public readonly float Escala;
        public readonly int Seed;
        public readonly float Lado;
        public float RaioTerra { get; private set; }
        public readonly Vector2 Lago, Alagado, Floresta, Ruinas, Pico, Dunas;
        public readonly float LagoR, AlagadoR, FlorestaR, RuinasR, PicoR, PicoTopo, DunasR;
        /// <summary>Raio da LAMINA d'agua (maior que a cava). Fonte unica para nado, gelo e raio.</summary>
        public readonly float LagoDiscoR, AlagadoDiscoR;
        public Poi[] Pois { get; private set; }
        public readonly Vector3[] Nascimentos;

        readonly float invEscala;
        readonly Ruido ruido;

        public Relevo(float escala = 2f, int seed = 7)
        {
            Escala = escala;
            invEscala = 1f / escala;
            Seed = seed;
            ruido = new Ruido(seed, 0.02f);
            Lado = BaseLado * escala;
            RaioTerra = BaseRaioTerra * escala;
            Lago = BLago * escala; LagoR = BLagoR * escala;
            Alagado = BAlagado * escala; AlagadoR = BAlagadoR * escala;
            Floresta = BFloresta * escala; FlorestaR = BFlorestaR * escala;
            Ruinas = BRuinas * escala; RuinasR = BRuinasR * escala;
            Pico = BPico * escala; PicoR = BPicoR * escala; PicoTopo = BPicoTopo * escala;
            Dunas = BDunas * escala; DunasR = BDunasR * escala;
            LagoDiscoR = BLagoDiscoR * escala;
            AlagadoDiscoR = BAlagadoDiscoR * escala;
            // Ordem do Godot (o loot cicla por indice): alfabetica, POI novo entra no FIM.
            // A enseada nao entra: e' silhueta, nao lugar de pousar.
            Pois = new[]
            {
                new Poi("alagado", Alagado, BAlagadoR / BaseRaioTerra, RaioTerra),
                new Poi("floresta", Floresta, BFlorestaR / BaseRaioTerra, RaioTerra),
                new Poi("lago", Lago, BLagoR / BaseRaioTerra, RaioTerra),
                new Poi("ruinas", Ruinas, BRuinasR / BaseRaioTerra, RaioTerra),
                new Poi("dunas", Dunas, BDunasR / BaseRaioTerra, RaioTerra),
                new Poi("pico", Pico, BPicoR / BaseRaioTerra, RaioTerra),
                new Poi("vale", Vector2.zero, BValeR / BaseRaioTerra, RaioTerra),
            };
            Nascimentos = MontarNascimentos();
        }

        // ------------------------------------------------------------------ relevo

        /// <summary>
        /// O RELEVO. Tudo aqui dentro e' em espaco-base: a escala entra na primeira linha e em
        /// nenhum outro lugar — um mapa 2x maior e' a MESMA ilha amostrada com metade da frequencia.
        /// A altura sai em metros de verdade (nao escala).
        /// </summary>
        public float Altura(float x, float z)
        {
            float px = x * invEscala;
            float pz = z * invEscala;
            float r = Mathf.Sqrt(px * px + pz * pz);
            // costa irregular: raio "efetivo" deformado por ruido (~+-13%) — a silhueta deixa de ser um disco
            float redge = r * (1f + 0.13f * ruido.Amostra(px * 1.2f + 700f, pz * 1.2f - 300f));
            float fall = 1f - Suave(87f, 137f, redge);
            float n = ruido.Amostra(px, pz) * 0.5f + 0.5f;
            float h = fall * (1.8f + 10.5f * n);                                   // colinas
            h += fall * ruido.Amostra(px * 4f + 300f, pz * 4f - 120f) * 0.5f;     // ondulacao curta (~12 m)
            float vale = 1f - Suave(7f, 50f, r);                                  // vale central
            h -= 2.8f * vale;
            if (vale > 0f) h = Mathf.Max(h, 1.05f);                                // vale nunca afunda no mar
            h = Lerp(h, -2.4f, 1f - Suave(BLagoR * 0.3f, BLagoR + 20f, Dist(px, pz, BLago)));
            float dm = Dist(px, pz, BAlagado);
            h = Lerp(h, 0.35f, 1f - Suave(BAlagadoR * 0.55f, BAlagadoR + 26f, dm));
            h -= 0.5f * (1f - Suave(5f, 13f, dm));                                 // poca central do alagado
            h = Lerp(h, 9f, 1f - Suave(BRuinasR * 0.6f, BRuinasR + 14f, Dist(px, pz, BRuinas)));

            // PICO: mesa com UMA encosta subivel (setor de ~120 graus apontado para PicoFace).
            // Guarda de custo: acima de PicoR*1.9 o peso ja' e' zero, a conta nem roda.
            float dpk = Dist(px, pz, BPico);
            if (dpk < BPicoR * 1.9f)
            {
                float ang = Mathf.Atan2(pz - BPico.y, px - BPico.x);
                float ramp = 1f + 0.9f * Mathf.Max(0f, Mathf.Cos(ang - PicoFace));
                float wpk = Suave(BPicoR * ramp, BPicoTopo * ramp, dpk);
                float crag = 1.9f - 0.9f * ramp;                                   // crista some na rampa
                h += wpk * (PicoH + crag * ruido.Amostra(px * 2f - 400f, pz * 2f + 260f) * 5.4f);
            }

            // DUNAS: cota quase plana + onda DIRECIONAL (listra transversal ao vento)
            float wdu = 1f - Suave(BDunasR * 0.45f, BDunasR + 18f, Dist(px, pz, BDunas));
            if (wdu > 0f)
            {
                float warp = ruido.Amostra(px * 0.8f + 150f, pz * 0.8f - 80f) * 2.6f;
                float crest = Mathf.Sin((px * 0.62f + pz * 0.78f) * 0.45f + warp) * 1.25f
                              + ruido.Amostra(px * 3f - 900f, pz * 3f + 400f) * 1.1f;
                h = Lerp(h, 2.4f + crest, wdu * 0.9f);
            }

            // ENSEADA: o mar entra ilha adentro (a cota desce, o plano do mar preenche)
            Vector2 meio = (BEnseadaA + BEnseadaB) * 0.5f;
            if (Dist(px, pz, meio) < 66f)
            {
                Vector2 q = MaisPertoNoSegmento(new Vector2(px, pz), BEnseadaA, BEnseadaB);
                float db = Dist(px, pz, q);
                h = Lerp(h, -3f, 1f - Suave(BEnseadaW * 0.35f, BEnseadaW + 15f, db));
            }

            h -= 3.2f * Suave(118f, 150f, redge);                                  // borda mergulha no mar
            return h;
        }

        /// <summary>
        /// A superficie d'agua em (x, z): a cota da lamina ou Seco. E' a pergunta que o NADO faz.
        /// Lago e brejo = os MESMOS discos que a Ilha desenha; mar = onde o terreno mergulha.
        /// </summary>
        public float SuperficieDaAgua(float x, float z)
        {
            if (Dist(x, z, Lago) <= LagoDiscoR) return LagoY;
            if (Dist(x, z, Alagado) <= AlagadoDiscoR) return AlagadoY;
            if (Altura(x, z) < AguaY) return AguaY;
            return Seco;
        }

        /// <summary>
        /// Chao SECO acima da praia. SEM teto de altura, de proposito: o teto de 8,5 m que loot,
        /// bau e zona cravavam proibia o plato das ruinas (9 m) e o topo do pico (~28 m) — os dois
        /// unicos POIs altos eram os dois onde nada podia nascer.
        /// </summary>
        public bool PodePousar(float x, float z)
        {
            return SuperficieDaAgua(x, z) == Seco && Altura(x, z) >= PraiaY;
        }

        /// <summary>Normal.y aproximada (mesmo esquema da malha).</summary>
        public float NormalY(float x, float z)
        {
            return Normal(x, z).y;
        }

        public Vector3 Normal(float x, float z)
        {
            const float e = 0.6f;
            Vector3 n = new Vector3(
                Altura(x - e, z) - Altura(x + e, z),
                2f * e,
                Altura(x, z - e) - Altura(x, z + e));
            return n.normalized;
        }

        /// <summary>
        /// Procura chao pousavel numa espiral de raio crescente a partir de `centro`. E' o que o loot,
        /// o bau e os nascimentos precisam: POI de agua (lago, alagado) tem o centro molhado e a
        /// beira seca. Deterministico. Devolve false se nao ha' chao seco em `raioMax`.
        /// </summary>
        public bool PontoPousavelPerto(Vector2 centro, float raioMax, out Vector3 ponto)
        {
            return PontoPousavelPerto(centro, raioMax, null, out ponto);
        }

        /// <summary>
        /// Chao pousavel DENTRO do POI. Um POI de agua (lago, alagado) tem o centro molhado e a beira
        /// seca a ate' ~2,2 raios da cava — busca cravada em 1 raio e' como o cajado do alagado
        /// deixou de nascer, calado, no mapa de escala 2 (medido em 27/08). Por isso a busca vai
        /// a 2,5 raios: a fracao e' do POI, e cresce com ele.
        /// </summary>
        public bool PontoPousavelNoPoi(Poi poi, out Vector3 ponto)
        {
            return PontoPousavelPerto(poi.Centro, poi.Raio * 2.5f, null, out ponto);
        }

        bool PontoPousavelPerto(Vector2 centro, float raioMax, Predicate<Vector2> extra, out Vector3 ponto)
        {
            const int passos = 600;
            for (int k = 0; k < passos; k++)
            {
                float r = raioMax * Mathf.Sqrt((float)k / passos);
                float a = k * 2.39996f;                                            // angulo aureo: espiral parelha
                Vector2 p = centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (!PodePousar(p.x, p.y)) continue;
                if (extra != null && !extra(p)) continue;
                ponto = new Vector3(p.x, Altura(p.x, p.y), p.y);
                return true;
            }
            ponto = Vector3.zero;
            return false;
        }

        Vector3[] MontarNascimentos()
        {
            // As coordenadas do Island.tscn sao metros-base contra os POIs base. Cada uma e' grudada
            // no chao pousavel mais perto (o Godot so' grudava em height(); aqui o contrato e' mais
            // duro: ninguem nasce na agua nem na praia) e afastada dos anteriores.
            var lista = new List<Vector3>(BNascimentos.Length);
            for (int i = 0; i < BNascimentos.Length; i++)
            {
                Vector2 alvo = BNascimentos[i] * Escala;
                Vector3 p;
                if (!PontoPousavelPerto(alvo, 40f * Escala, q => LongeDeTodos(q, lista), out p))
                    continue;
                lista.Add(p + Vector3.up * 0.3f);
            }
            return lista.ToArray();
        }

        static bool LongeDeTodos(Vector2 q, List<Vector3> lista)
        {
            for (int j = 0; j < lista.Count; j++)
            {
                float dx = lista[j].x - q.x, dz = lista[j].z - q.y;
                if (dx * dx + dz * dz < SeparacaoNascimentos * SeparacaoNascimentos) return false;
            }
            return true;
        }

        // --------------------------------------------------------------- cor / bioma

        struct Pesos
        {
            public float Grande, Fino, Floresta, Ruinas, Lama, Dunas, Rocha;
        }

        Pesos PesosEm(float x, float z, float h)
        {
            float bx = x * invEscala, bz = z * invEscala;
            Pesos w;
            w.Grande = ruido.Amostra(bx * 1.5f, bz * 1.5f) * 0.5f + 0.5f;           // mancha de regiao (~33 m base)
            w.Fino = ruido.Amostra(bx * 5.5f + 90f, bz * 5.5f - 40f) * 0.5f + 0.5f; // granulado (~9 m base)
            w.Floresta = 1f - Suave(FlorestaR * 0.3f, FlorestaR + 9f, Dist(x, z, Floresta));
            w.Ruinas = 1f - Suave(RuinasR * 0.85f, RuinasR + 6f, Dist(x, z, Ruinas));
            float wm = (1f - Suave(AlagadoR * 0.2f, AlagadoR + 13f, Dist(x, z, Alagado))) * Suave(5.6f, 0.2f, h);
            w.Lama = Mathf.Min(wm * 1.5f, 1f);
            w.Dunas = 1f - Suave(DunasR * 0.5f, DunasR + 14f, Dist(x, z, Dunas));
            // Rocha pela inclinacao AMPLA (queda de cota sobre ~9 m), nao pela normal do vertice:
            // a normal pula entre vizinhos e cor que pula e' degrau.
            float passo = 4.5f * Escala;
            float dh = Mathf.Max(Mathf.Abs(Altura(x + passo, z) - Altura(x - passo, z)),
                                 Mathf.Abs(Altura(x, z + passo) - Altura(x, z - passo))) / (2f * passo);
            w.Rocha = Mathf.Max(Suave(0.30f, 0.62f, dh), Suave(10.5f, 16f, h));
            return w;
        }

        /// <summary>
        /// COR DO CHAO (sRGB), por altura, umidade e POI, tudo em rampa — lugar nenhum tem
        /// fronteira dura. O alfa carrega o PESO DE ROCHA para o shader trocar a textura de
        /// detalhe (detalhe-chao -> detalhe-rocha) pela mesma conta que pintou o vertice.
        /// </summary>
        public Color Cor(float x, float z, float h) => Cor(x, z, h, out _);

        /// <summary>
        /// A COMPOSICAO do chao em (x, z) — o que o shader pinta de perto fora da pedra (_Chao, onda 7B). A Ilha grava no UV0:
        /// r = chao de MATA, g = AREIA (praia e duna), b = PISADO (terra batida e trilha: miolo dos nascimentos e anel das
        /// ruinas; o shader so' aplica na grama e na mata), a = GRAMA da campina. Sai da MESMA conta da cor: cada camada que
        /// pinta por cima cobre as de baixo na mesma fracao, entao r + g + a nunca passa de 1.
        /// </summary>
        public Color Solo(float x, float z, float h)
        {
            Cor(x, z, h, out Color s);
            return s;
        }

        /// <summary>A camada nova cobre `t` do que ja' estava pintado (b, o pisado, e' posicional: sai fora da conta).</summary>
        static void Cobrir(ref Color k, float t)
        {
            float f = 1f - t;
            k.r *= f; k.g *= f; k.a *= f;
        }

        /// <summary>O quanto o chao em (x, z) e' PISADO: o miolo de cada nascimento (onde se nasce e se luta) e o anel de chegada
        /// das ruinas. KNOB: os raios, por foto.</summary>
        float Gasto(float x, float z)
        {
            float g = 1f - Suave(RuinasR * 0.7f, RuinasR + 30f, Dist(x, z, Ruinas));
            for (int i = 0; i < Nascimentos.Length; i++)
                g = Mathf.Max(g, 1f - Suave(5f, 40f, Dist(x, z, new Vector2(Nascimentos[i].x, Nascimentos[i].z))));
            return g;
        }

        public Color Cor(float x, float z, float h, out Color solo)
        {
            Pesos w = PesosEm(x, z, h);
            float big = w.Grande, fine = w.Fino;
            var k = new Color(0f, 0f, 0f, 1f);   // a composicao: r mata, g areia, b pisado, a grama
            // 1) campina: verde por altitude + faixa seca + fundo escuro
            Color c = Color.Lerp(CorGramaClara, CorGrama, Mathf.Clamp01((h - 1f) / 10.5f));
            c = Color.Lerp(c, CorGramaSeca, Suave(0.50f, 0.72f, big) * 0.7f);
            c = Color.Lerp(c, CorGramaFunda, Suave(0.50f, 0.28f, big) * 0.6f);
            c = Color.Lerp(c, Escurecer(c, 0.14f), fine * 0.5f);
            // 2) floresta: chao de mata escuro e terroso. Na composicao conta a REGIAO inteira (a cor mistura ate' 76%)
            c = Color.Lerp(c, CorChaoDeMata, w.Floresta * (0.42f + 0.34f * fine));
            Cobrir(ref k, w.Floresta);
            k.r += w.Floresta;
            // 3) ruinas: a PRACA de pedra (onda 9B). O calcamento e' do shader (cor propria, _Ruinas); aqui, a media dele com o
            //    musgo da regiao, num disco que acaba DENTRO da borda do calcamento (0,70-0,90 do raio < BordaDaPraca). O bioma
            //    (w.Ruinas: grama, vegetacao, terreno reativo) nao muda.
            float wp = (1f - Suave(RuinasR * 0.70f, RuinasR * 0.90f, Dist(x, z, Ruinas))) * 0.95f;
            c = Color.Lerp(c, Color.Lerp(CorPraca, CorMusgo, 0.05f + 0.20f * fine), wp);
            Cobrir(ref k, wp);
            // 4) alagado: lama so' na cota baixa
            c = Color.Lerp(c, Escurecer(CorLama, 0.2f), w.Lama);
            Cobrir(ref k, w.Lama);
            // 4b) dunas: areal palido com capim ralo (menos capim que antes: o capim seco novo e' mais verde, e duna e' CLARA)
            c = Color.Lerp(c, Color.Lerp(CorDuna, CorGramaSeca, 0.08f + 0.2f * fine), w.Dunas * 0.92f);
            Cobrir(ref k, w.Dunas * 0.92f);
            k.g += w.Dunas * 0.92f;
            // 5) rocha por inclinacao ampla e por altitude, com veio
            float wrk = w.Rocha;
            c = Color.Lerp(c, CorRocha, wrk * 0.92f);
            Cobrir(ref k, wrk * 0.92f);
            if (wrk > 0.02f)
            {
                c = Color.Lerp(c, Escurecer(c, 0.46f), wrk * big * 0.70f);
                c = Color.Lerp(c, Escurecer(c, 0.28f), wrk * fine * 0.55f);
                c = Color.Lerp(c, CorPedra, wrk * Suave(0.46f, 0.88f, fine) * 0.42f);
                c = Color.Lerp(c, CorLama, wrk * Suave(0.62f, 0.20f, big) * 0.20f);
            }
            // 5b) cume do pico: so' na tampa. A faixa ACOMPANHA o topo da mesa (PicoH +
            // sope' de ~7 m): medido aqui, a mesa fica em ~25,7 m — com a faixa cravada em 24-28
            // o cume saia com 0,05 de diferenca das ruinas e os dois POIs viravam a mesma mancha
            // vistos de 200 m (o teste de leitura aerea barra em 0,10). A mancha de regiao puxa
            // parte da tampa para terra quente: variacao que se le' do alto. A de perto (pedra
            // gasta, liquen, fissura, estrato) e' do shader (_Chao), por cima desta.
            Color tampa = Color.Lerp(CorPico, CorLama, Suave(0.40f, 0.75f, fine) * 0.25f);
            float tt = Suave(PicoH + 4f, PicoH + 8f, h) * 0.78f;
            c = Color.Lerp(c, tampa, tt);
            Cobrir(ref k, tt);
            // 6) praia em dois degraus, limiar puxado por ruido; a lama (ao quadrado) suprime.
            //    O degrau 1 vai ao MEIO (55% areia): so' essa fracao entra como areia.
            float beach = 4.6f + 1.4f * ruido.Amostra(x * invEscala * 2.6f - 55f, z * invEscala * 2.6f + 210f);
            float dry = (1f - w.Lama) * (1f - w.Lama);
            Color meio = Color.Lerp(CorAreia, c, 0.45f);
            float p1 = Suave(beach, 0.9f, h) * dry, p2 = Suave(2.5f, 0.15f, h) * dry;
            c = Color.Lerp(c, meio, p1);
            Cobrir(ref k, p1 * 0.55f);
            k.g += p1 * 0.55f;
            c = Color.Lerp(c, CorAreia, p2);
            Cobrir(ref k, p2);
            k.g += p2;
            // 7) faixa molhada na linha d'agua (larga, do vertice; a borda NITIDA da areia molhada e' do shader, por pixel)
            c = Color.Lerp(c, Escurecer(c, 0.28f), Suave(1.7f, 0f, h));
            c.a = Mathf.Clamp01(wrk);
            k.b = Gasto(x, z);
            solo = k;
            return c;
        }

        /// <summary>
        /// AO DE VERTICE — o substituto barato do SSAO (que nao existe no mobile). Cavidade em
        /// duas escalas (crista 2,5 m e bacia 16 m, ambas x Escala) e encosta pela normal.
        /// Multiplica a cor em LINEAR. Roda so' no load.
        /// </summary>
        public float Ao(float x, float z, float h, float ny)
        {
            float e = 2.5f * Escala;
            float w = 16f * Escala;
            float perto = (Altura(x - e, z) + Altura(x + e, z) + Altura(x, z - e) + Altura(x, z + e)) * 0.25f - h;
            float largo = (Altura(x - w, z) + Altura(x + w, z) + Altura(x, z - w) + Altura(x, z + w)) * 0.25f - h;
            float cavidade = Mathf.Clamp(1f - Mathf.Max(perto, 0f) * 0.45f - Mathf.Max(largo, 0f) * 0.07f, 0.66f, 1f);
            return cavidade * Lerp(0.80f, 1f, Suave(0.70f, 0.97f, ny));
        }

        /// <summary>O bioma dominante em (x, z) — a mesma conta da cor, resumida num rotulo.</summary>
        public Bioma BiomaEm(float x, float z)
        {
            if (SuperficieDaAgua(x, z) != Seco) return Bioma.Agua;
            float h = Altura(x, z);
            Pesos w = PesosEm(x, z, h);
            if (h > PicoH + 4f) return Bioma.Pico;
            if (w.Rocha > 0.5f) return Bioma.Rocha;
            if (w.Lama > 0.5f) return Bioma.Alagado;
            if (h < PraiaY) return Bioma.Praia;
            if (w.Ruinas > 0.5f) return Bioma.Ruinas;
            if (w.Floresta > 0.5f) return Bioma.Floresta;
            if (w.Dunas > 0.5f) return Bioma.Dunas;
            return Bioma.Campina;
        }

        // ------------------------------------------------------------------ util

        /// <summary>smoothstep do Godot: aceita de > para (rampa invertida).</summary>
        public static float Suave(float de, float para, float x)
        {
            float t = Mathf.Clamp01((x - de) / (para - de));
            return t * t * (3f - 2f * t);
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        static float Dist(float x, float z, Vector2 p)
        {
            float dx = x - p.x, dz = z - p.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static Vector2 MaisPertoNoSegmento(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 <= 1e-6f) return a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return a + ab * t;
        }

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        /// <summary>Color.darkened do Godot.</summary>
        public static Color Escurecer(Color c, float f)
        {
            return new Color(c.r * (1f - f), c.g * (1f - f), c.b * (1f - f), c.a);
        }

        /// <summary>Color.lightened do Godot.</summary>
        public static Color Clarear(Color c, float f)
        {
            return new Color(c.r + (1f - c.r) * f, c.g + (1f - c.g) * f, c.b + (1f - c.b) * f, c.a);
        }
    }

    /// <summary>
    /// Perlin 2D deterministico, sem dependencia. Substitui o FastNoiseLite do Godot: mesma
    /// assinatura (sai em [-1, 1], frequencia aplicada dentro), forma diferente.
    /// </summary>
    public sealed class Ruido
    {
        readonly uint seed;
        readonly float freq;
        static readonly float[] Gx = { 1f, -1f, 0f, 0f, 0.70710678f, -0.70710678f, 0.70710678f, -0.70710678f };
        static readonly float[] Gz = { 0f, 0f, 1f, -1f, 0.70710678f, 0.70710678f, -0.70710678f, -0.70710678f };

        public Ruido(int seed, float frequencia)
        {
            this.seed = (uint)seed;
            freq = frequencia;
        }

        uint Hash(int ix, int iz)
        {
            unchecked
            {
                uint h = (uint)ix * 374761393u ^ (uint)iz * 668265263u ^ seed * 0x9E3779B1u;
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return h;
            }
        }

        float Grad(int ix, int iz, float dx, float dz)
        {
            int g = (int)(Hash(ix, iz) & 7u);
            return Gx[g] * dx + Gz[g] * dz;
        }

        public float Amostra(float x, float z)
        {
            x *= freq;
            z *= freq;
            int ix = Mathf.FloorToInt(x), iz = Mathf.FloorToInt(z);
            float fx = x - ix, fz = z - iz;
            float ux = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);          // quintico: C2, sem artefato de grade
            float uz = fz * fz * fz * (fz * (fz * 6f - 15f) + 10f);
            float a = Grad(ix, iz, fx, fz);
            float b = Grad(ix + 1, iz, fx - 1f, fz);
            float c = Grad(ix, iz + 1, fx, fz - 1f);
            float d = Grad(ix + 1, iz + 1, fx - 1f, fz - 1f);
            float v = Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uz);
            return Mathf.Clamp(v * 1.41421356f, -1f, 1f);                    // 2D perlin cabe em +-0.707
        }
    }
}
