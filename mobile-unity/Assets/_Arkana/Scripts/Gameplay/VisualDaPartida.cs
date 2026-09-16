using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Core;
using Arkana.Characters;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>Malha em arrays puros: o teste EditMode confere a conta sem criar Mesh (sem GPU, sem cena).</summary>
    public sealed class MalhaDados
    {
        public Vector3[] Vertices;
        public Vector3[] Normais;
        public Vector2[] Uvs;
        public int[] Triangulos;

        public Mesh ParaMesh(string nome)
        {
            var m = new Mesh();
            m.name = nome;
            m.vertices = Vertices;
            m.normals = Normais;
            m.uv = Uvs;
            m.triangles = Triangulos;
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>
    /// A PAREDE da tempestade: cilindro ABERTO de raio 1 (a casca escala pelo raio da zona), pe' em y=0.
    /// DUAS faces na malha (normal para fora e para dentro, ordem invertida) em vez de Cull Off no material:
    /// quem esta' seguro ve' a parede por dentro, quem sangra ve' por fora, e o shader fica o Unlit de fabrica.
    /// A coluna `segmentos` repete a 0 com u=1: a textura rola sem rasgo na costura.
    /// </summary>
    public static class MalhaDaParede
    {
        public const int SEGMENTOS = 64;
        public const float ALTURA = 220f;

        public static MalhaDados Cilindro(int segmentos, float altura)
        {
            int n = Mathf.Max(3, segmentos);
            int colunas = n + 1;
            int porFace = colunas * 2;   // pe' e topo de cada coluna
            var d = new MalhaDados
            {
                Vertices = new Vector3[porFace * 2],
                Normais = new Vector3[porFace * 2],
                Uvs = new Vector2[porFace * 2],
                Triangulos = new int[n * 12],
            };
            for (int i = 0; i < colunas; i++)
            {
                // i % n: a ultima coluna cai EXATAMENTE em cima da primeira (cos/sin de 2pi nao voltam a 0 em float)
                float a = (i % n) * Mathf.PI * 2f / n;
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float u = (float)i / n;
                for (int f = 0; f < 2; f++)
                {
                    int v = f * porFace + i * 2;
                    Vector3 normal = f == 0 ? radial : -radial;
                    d.Vertices[v] = radial;
                    d.Vertices[v + 1] = radial + new Vector3(0f, altura, 0f);
                    d.Normais[v] = normal;
                    d.Normais[v + 1] = normal;
                    d.Uvs[v] = new Vector2(u, 0f);
                    d.Uvs[v + 1] = new Vector2(u, 1f);
                }
            }
            int t = 0;
            for (int i = 0; i < n; i++)
                for (int f = 0; f < 2; f++)
                {
                    int b0 = f * porFace + i * 2, t0 = b0 + 1, b1 = b0 + 2, t1 = b0 + 3;
                    // frente do Unity = o lado para onde aponta cross(b-a, c-a): fora gira num sentido, dentro no outro
                    if (f == 0) { Tri(d.Triangulos, ref t, b0, t0, b1); Tri(d.Triangulos, ref t, t0, t1, b1); }
                    else { Tri(d.Triangulos, ref t, b0, b1, t0); Tri(d.Triangulos, ref t, t0, b1, t1); }
                }
            return d;
        }

        static void Tri(int[] tri, ref int t, int a, int b, int c)
        {
            tri[t++] = a; tri[t++] = b; tri[t++] = c;
        }
    }

    /// <summary>
    /// A MARCA no chao em NEON: contorno de poligono regular (raio 1 = a borda de fora do contorno chapado antigo,
    /// vertice para +z), deitado e virado para cima. Tubo de luz no meio do contorno, brilho que esmaece para os dois
    /// lados e miolo fraco (a poca de luz sob a luva/o bau). O perfil mora no V da faixa macia (MaterialVfx.FaixaSuave:
    /// alfa 0 em v=0 e v=1, cheio em v=0,5) — o Unlit do URP pinta sem cor de vertice. So' raio RELATIVO: o poligono
    /// escalado continua lendo a forma (GDD §10: quem nao distingue cor le' losango/triangulo).
    /// </summary>
    public static class MalhaDaMarca
    {
        /// <summary>v do miolo: alfa (1 - |2v-1|)^2 = 0,13 da faixa — a poca aparece, o chao nao some.</summary>
        public const float MIOLO_V = 0.18f;
        /// <summary>O brilho se espalha por 2,5 meias-larguras do contorno, para dentro e para fora (3 borrava o losango).</summary>
        public const float ESPALHA = 2.5f;

        public static MalhaDados Neon(int lados, float furo)
        {
            int n = Mathf.Max(3, lados);
            float meio = (1f + furo) * 0.5f, meia = (1f - furo) * 0.5f;
            // centro + 3 aneis de n: (miolo, v fixo) -> (tubo, v=0,5) -> (fora, v=1)
            float r1 = Mathf.Max(meio - meia * ESPALHA, 0f), r2 = meio, r3 = meio + meia * ESPALHA;
            int vs = 1 + 3 * n;
            var d = new MalhaDados
            {
                Vertices = new Vector3[vs],
                Normais = new Vector3[vs],
                Uvs = new Vector2[vs],
                Triangulos = new int[n * 15],
            };
            d.Uvs[0] = new Vector2(0.5f, MIOLO_V);
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * 0.5f + Mathf.PI * 2f * i / n;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                d.Vertices[1 + i] = dir * r1;
                d.Vertices[1 + n + i] = dir * r2;
                d.Vertices[1 + 2 * n + i] = dir * r3;
                d.Uvs[1 + i] = new Vector2(0.5f, MIOLO_V);
                d.Uvs[1 + n + i] = new Vector2(0.5f, 0.5f);
                d.Uvs[1 + 2 * n + i] = new Vector2(0.5f, 1f);
            }
            for (int i = 0; i < vs; i++) d.Normais[i] = Vector3.up;
            int t = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                TriCima(d, ref t, 0, 1 + i, 1 + j);
                for (int k = 0; k < 2; k++)
                {
                    int a0 = 1 + k * n + i, a1 = 1 + k * n + j, b0 = a0 + n, b1 = a1 + n;
                    TriCima(d, ref t, a0, a1, b1);
                    TriCima(d, ref t, a0, b1, b0);
                }
            }
            return d;
        }

        /// <summary>Frente para CIMA (a mesma regra da parede: cross(b-a, c-a) aponta para a normal). Cull Back no material.</summary>
        static void TriCima(MalhaDados d, ref int t, int a, int b, int c)
        {
            Vector3[] v = d.Vertices;
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), Vector3.up) < 0f) { int x = b; b = c; c = x; }
            d.Triangulos[t++] = a; d.Triangulos[t++] = b; d.Triangulos[t++] = c;
        }
    }

    /// <summary>Leitura de BR (DIRECAO.md, GDD §10): a luva se ve' de longe pela raridade — cor + FORMA + coluna.</summary>
    public static class LootVisual
    {
        /// <summary>m — maior lado do modelo no chao (a Meshy entrega ~1,9 u: escala medida, nao chutada).</summary>
        public const float TAMANHO_LUVA = 0.5f;
        public const float GIRO_S = 6f, FLUTUA_M = 0.16f, FLUTUA_S = 2.6f;   // os KNOBs do Loot.gd
        public const float RAIO_MARCA = 0.7f, RAIO_COLUNA = 0.3f;
        public const float NUCLEO_M = 0.15f, NUCLEO_Y = 0.36f;
        /// <summary>m — altura da coluna de luz. KNOB: e' o que se ve' do planeio (90 m) para escolher o rumo.</summary>
        public const float COLUNA_RARO = 18f, COLUNA_LENDARIA = 30f;
        /// <summary>m — a faixa de luz (borda macia: le' ~metade, a largura do cano antigo) e o fio quente no meio dela.</summary>
        public const float LARGURA_FEIXE = RAIO_COLUNA * 5f, LARGURA_FIO = RAIO_COLUNA * 1.3f;
        /// <summary>m — raio do anel de faiscas que gira em volta da luva.</summary>
        public const float RAIO_AURA = 0.42f;
        /// <summary>Particulas/s na forca 1 (lendaria): a aura em volta da luva e as motas subindo na coluna.</summary>
        public const float AURA_S = 8f, MOTAS_S = 9f;
        /// <summary>Brilho HDR da marca na forca 1 (passa do limiar 1,1 do bloom); o pulso oscila entre 70% e 100% disto.
        /// KNOB por foto, como o MaterialVfx.BrilhoHdr.</summary>
        public const float BRILHO_MARCA = 1.4f, PULSO_S = 1.6f;
        /// <summary>O NUCLEO do elemento ACENDE (foto 02 de 12/09: a esfera chapada lia bolinha de plastico): cor HDR no
        /// Unlit (passa do limiar do bloom) + HALO de ponto macio aditivo em volta, de HALO_M m. KNOBs por foto.</summary>
        public const float BRILHO_NUCLEO = 1.6f, BRILHO_HALO = 1.2f, HALO_M = 0.55f;

        /// <summary>Nome em Resources. Id desconhecido cai na luva comum — o mesmo fallback de Arma.Dados.</summary>
        public static string ModeloDe(string armaId) => "luva-" + Arma.Dados(armaId).Id;

        /// <summary>So' raro para cima tem coluna: 12 varinhas com farol viram ruido e apagam o cajado.</summary>
        public static float AlturaDaColuna(string raridade)
        {
            switch (raridade)
            {
                case "raro": return COLUNA_RARO;
                case "lendaria": return COLUNA_LENDARIA;
                default: return 0f;
            }
        }

        /// <summary>Quanto a raridade ACENDE (marca, aura, motas): a cor e a forma dizem QUAL, isto diz QUANTO. O comum
        /// nao cai abaixo de 0,7: o neon e' aditivo, e na areia clara a cor palida do comum some.</summary>
        public static float Forca(string raridade)
        {
            switch (raridade)
            {
                case "raro": return 0.85f;
                case "lendaria": return 1f;
                default: return 0.7f;
            }
        }
    }

    public static class BauVisual
    {
        public const string MODELO = "bau";
        /// <summary>m — maior lado do bau (Godot: 1,15 m de largura; a canalizacao nao muda com a arte).</summary>
        public const float TAMANHO = 1.15f;
        public const float RAIO_FEIXE = 1.2f;
        /// <summary>Brilho HDR da marca de abrir; a canalizacao soma ate' +100% (quem canaliza ve' o chao acender).</summary>
        public const float BRILHO_MARCA = 1.3f;

        /// <summary>
        /// Onde o bau esta' na queda, t01 = 0 (anuncio) .. 1 (pouso). EASE-OUT cubico: desce rapido e ASSENTA
        /// devagar — queda acelerada esconderia o bau ate' o ultimo segundo, o oposto do que o evento quer.
        /// </summary>
        public static Vector3 PosNaQueda(Vector3 pouso, float t01)
        {
            float t = t01 > 0f ? (t01 < 1f ? t01 : 1f) : 0f;   // !(x > 0) barra NaN
            float resta = 1f - t;
            return pouso + new Vector3(0f, BauCelestial.ALTURA_QUEDA * resta * resta * resta, 0f);
        }
    }

    public static class ZonaVisual
    {
        /// <summary>Violeta arcano (paleta GDD §10: clima com COR, nunca falta de luz).</summary>
        public static readonly Color COR = new Color32(0xB0, 0x6C, 0xFF, 255);
        /// <summary>m — pe' da parede abaixo do fundo do mar: ela nunca "flutua" sobre um vale.</summary>
        public const float BASE_PAREDE = -20f;
        /// <summary>m — o anel do proximo circulo e' faixa macia aditiva (le' ~metade da largura: 1,8 ~ o 1,2 chapado antigo).</summary>
        public const float ALTURA_ANEL = 1.2f, LARGURA_ANEL = 1.8f;
        public const int PONTOS_ANEL = 96;
        /// <summary>Ladrilho da textura (px) e repeticoes na volta e na altura (inteiras: a costura em u=1 fecha sem rasgo).</summary>
        public const int TEX_U = 128, TEX_V = 64;
        public const float REPETE_U = 12f, REPETE_V = 3f;
        /// <summary>Cor da parede acima de 1 (o filete claro passa do limiar do bloom; a nevoa nao). KNOB por foto.</summary>
        public const float BRILHO_PAREDE = 1.25f;
        /// <summary>m — faixa acesa no PE' da parede (de BASE_PAREDE para cima, pico na metade: ~8 m, o chao da ilha
        /// vai de 0 a 18). E' a BORDA que se le' do alto e de longe, onde a tempestade morde a ilha.</summary>
        public const float ALTURA_PE = 56f, BRILHO_PE = 1.2f;

        /// <summary>
        /// O circulo que o anel do chao mostra: na ESPERA e' Plano[FaseAtual] (o avisado); no FECHA a fase ja'
        /// avancou e o alvo e' Plano[FaseAtual - 1] — o anel fica ate' a parede chegar (Zona.gd: some no _chegou).
        /// </summary>
        public static bool Proximo(Zona z, out Zona.Circulo c)
        {
            c = default(Zona.Circulo);
            if (z == null || z.Plano == null) return false;
            int i = z.EstadoAtual == Zona.Estado.Espera ? z.FaseAtual
                  : z.EstadoAtual == Zona.Estado.Fecha ? z.FaseAtual - 1
                  : -1;
            if (i < 0 || i >= z.Plano.Length) return false;
            c = z.Plano[i];
            return true;
        }

        /// <summary>
        /// Um texel do ladrilho da parede, PURO (o EditMode confere a costura sem GPU). Nada de listra diagonal: uma
        /// CORTINA de energia — filetes quase verticais que ondulam de leve (o offset os faz subir), pacotes de brilho
        /// correndo neles, chuva miuda de luz, correntes largas e nevoa violeta por tras. So' senos de frequencia
        /// INTEIRA em u e v: o ladrilho fecha sem emenda nos dois eixos. A cor mora aqui (nevoa = COR, filete quase
        /// branco); o material so' multiplica o brilho. Previa por foto: ondulacao forte lia papel de parede.
        /// </summary>
        public static Color Tempestade(int x, int y)
        {
            const float TAU = Mathf.PI * 2f;
            float u = (float)x / TEX_U, v = (float)y / TEX_V;
            float torce = 0.10f * Mathf.Sin(TAU * (v + 2f * u)) + 0.05f * Mathf.Sin(TAU * (3f * v - u));
            float f1 = Fio(5f * u + torce, 10f);                    // 5 filetes mestres por ladrilho
            float f2 = Fio(9f * u - 1.3f * torce + 0.37f, 12f);     // 9 finos, ondulando ao contrario
            float f3 = Fio(23f * u + 0.5f * torce + 0.11f, 4f);     // chuva miuda
            float p1 = Cubo(0.5f + 0.5f * Mathf.Sin(TAU * (2f * v + 3f * u)));
            float p2 = Cubo(0.5f + 0.5f * Mathf.Sin(TAU * (3f * v - 5f * u + 0.2f)));
            float p3 = 0.5f + 0.5f * Mathf.Sin(TAU * (4f * v + 7f * u));
            float largo = Cubo(0.5f + 0.5f * Mathf.Sin(TAU * (2f * u + 0.5f * torce)));
            float nevoa = 0.5f + 0.25f * Mathf.Sin(TAU * (u + 0.3f * Mathf.Sin(TAU * (2f * v + u))))
                               + 0.25f * Mathf.Sin(TAU * (3f * u - v + 0.25f * Mathf.Sin(TAU * 2f * v)));
            float mestre = f1 * (0.25f + 0.75f * p1);
            float luz = mestre + 0.7f * f2 * p2 + 0.2f * f3 * p3 + 0.25f * largo;
            Color c = Color.Lerp(COR * 0.8f, Color.Lerp(COR, Color.white, 0.6f), Mathf.Clamp01(luz));
            c.a = Mathf.Clamp01(0.08f + 0.16f * nevoa + 0.14f * largo + 0.6f * mestre + 0.4f * f2 * p2 + 0.1f * f3 * p3);
            return c;
        }

        /// <summary>Linha fina onde sin(pi w) zera: 1 no fio, cai rapido (p = dureza da borda).</summary>
        static float Fio(float w, float p) => Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(Mathf.PI * w)), p);

        static float Cubo(float x) => x * x * x;
    }

    /// <summary>
    /// A CASCA que desenha a partida: loot no chao, Bau Celestial e a Tempestade — tudo lido da Partida no
    /// LateUpdate (estado depois do Tick), nada decidido aqui. Sem colisor em nada, sem sombra nos efeitos,
    /// material um por cor em cache estatico. Fiacao defensiva: modelo ausente vira primitiva, sem log.
    /// A LUZ fala a lingua dos kits (VisualDosKits): faixa aditiva HDR de borda macia virada para a camera
    /// (MaterialVfx.DeLinha), particula de ponto macio (ParticulaVfx) e neon no chao — o cilindro translucido
    /// chapado da coluna lia "painel de plastico" (foto 02 de 12/09).
    /// </summary>
    public sealed class VisualDaPartida : MonoBehaviour
    {
        const string SHADER_UNLIT = "Universal Render Pipeline/Unlit";
        /// <summary>Pontos no comprimento do feixe: o degrade de cor vive nos VERTICES (2 pontos = degrade linear).</summary>
        const int PONTOS_FEIXE = 6;

        Partida _partida;
        readonly List<VisualLoot> _loots = new List<VisualLoot>();

        GameObject _parede;
        MeshRenderer _pe;
        LineRenderer _anelZona;
        Vector3 _anelCentro;
        float _anelRaio = -1f;

        GameObject _bau;
        Transform _bauPivo, _marcaBau;
        MeshRenderer _marcaBauR;
        TrailRenderer _rastro;
        LineRenderer _progresso, _feixeBau;
        ParticleSystem _faiscas, _auraBau, _canal;
        ParticleSystem[] _bauPs;
        Color _ouro;
        BauCelestial _bauVisto;
        BauCelestial.Fase _faseVista;

        ParticleSystem _estouro;

        public static VisualDaPartida Criar(Transform arena, Partida partida)
        {
            var go = new GameObject("VisualDaPartida");
            if (arena != null) go.transform.SetParent(arena, false);   // morre com a arena: restart nasce limpo
            VisualDaPartida v = go.AddComponent<VisualDaPartida>();
            v._partida = partida;
            return v;
        }

        void Awake()
        {
            if (_mpb == null) { _mpb = new MaterialPropertyBlock(); _idCor = Shader.PropertyToID("_BaseColor"); }
            MontarZona();
            MontarBau();
            MontarEstouro();
        }

        void LateUpdate()
        {
            if (_partida == null) return;
            float t = Time.time;   // timeScale 0 (pausa) congela tudo junto
            DesenharLoot(t);
            DesenharBau(t);
            DesenharZona(t);
        }

        // ---------------------------------------------------------------- loot

        void DesenharLoot(float t)
        {
            List<LootItem> itens = _partida.Loot != null ? _partida.Loot.Itens : null;
            int n = itens != null ? itens.Count : 0;
            while (_loots.Count < n) _loots.Add(new VisualLoot(transform));
            for (int i = 0; i < _loots.Count; i++)
            {
                if (i < n && itens[i] != null) _loots[i].Mostrar(itens[i], t);
                else _loots[i].Esconder();
            }
        }

        /// <summary>
        /// Um item no chao. Re-veste so' quando o item MUDA (troca no chao, lista que andou), nunca por frame; por frame
        /// so' o pulso (escala + cor por MaterialPropertyBlock, zero lixo). Custo por luva: marca + aura (<=16 faiscas);
        /// raro para cima soma 2 faixas e as motas (<=24).
        /// </summary>
        sealed class VisualLoot
        {
            readonly GameObject _raiz, _coluna;
            readonly Transform _pivo;
            readonly MeshRenderer _marca;
            readonly MeshFilter _marcaMf;
            readonly LineRenderer _feixe, _fio;
            readonly ParticleSystem _motas, _aura;
            readonly MeshRenderer[] _nucleos = new MeshRenderer[2];
            readonly ParticleSystem[] _halos = new ParticleSystem[2];
            readonly Dictionary<string, GameObject> _modelos = new Dictionary<string, GameObject>();
            string _id;
            int _nEls = -1;   // -1: a primeira passada sempre veste
            Elemento _e0, _e1;
            bool _temPos;
            Vector3 _pos;
            Color _corMarca;

            public VisualLoot(Transform pai)
            {
                _raiz = new GameObject("Loot");
                _raiz.transform.SetParent(pai, false);
                _pivo = new GameObject("Pivo").transform;
                _pivo.SetParent(_raiz.transform, false);
                _marca = Malha(_raiz.transform, "Marca", null, MaterialBrilho());
                _marca.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                _marcaMf = _marca.GetComponent<MeshFilter>();
                // a COLUNA: faixa larga na cor + fio quente quase branco no meio (o miolo que faz "luz", nao "vidro")
                // + motas subindo por dentro. LOCAL: o loot nao anda, os pontos se escrevem so' no Vestir.
                _coluna = new GameObject("Coluna");
                _coluna.transform.SetParent(_raiz.transform, false);
                _feixe = LinhaDeLuz(_coluna.transform, "Feixe", 0.55f);
                _fio = LinhaDeLuz(_coluna.transform, "Fio", 0.4f);
                _motas = ParticulaVfx.Novo(_coluna.transform, "Motas", Color.white, Color.white, 0f,
                    new Vector2(1.4f, 2.6f), new Vector2(2f, 5f), new Vector2(0.07f, 0.18f), false, 0.05f, 24);
                ParticleSystem.ShapeModule sm = _motas.shape;
                sm.scale = new Vector3(0.35f, 0.35f, 0.2f);
                // a AURA: faiscas girando em volta da luva e subindo por ela (o anel nasce um palmo abaixo)
                _aura = Orbita(_raiz.transform, "Aura", LootVisual.RAIO_AURA, 0.35f, 2.4f, 0f,
                    new Vector2(1.1f, 1.9f), new Vector2(0.05f, 0.13f), 16);
                _aura.transform.localPosition = new Vector3(0f, Loot.ALTURA - 0.35f, 0f);
                for (int k = 0; k < 2; k++)
                {
                    _nucleos[k] = MaterialMago.Primitivo(_pivo, "Nucleo" + k, PrimitiveType.Sphere, Vector3.zero,
                        Vector3.one * LootVisual.NUCLEO_M, null).GetComponent<MeshRenderer>();
                    SemSombra(_nucleos[k]);
                    _halos[k] = Halo(_nucleos[k].transform);   // filho: liga, desliga e anda com o nucleo
                }
            }

            public void Esconder() => Ativo(_raiz, false);

            public void Mostrar(LootItem item, float t)
            {
                bool acordou = !_raiz.activeSelf, vestiu = false;
                // campos crus, nao item.Elementos: aquele aloca array por chamada
                Elemento[] par = item.Par ?? Array.Empty<Elemento>();
                int n = par.Length > 0 ? Mathf.Min(par.Length, 2) : (item.ElementoDaLuva.HasValue ? 1 : 0);
                Elemento e0 = par.Length > 0 ? par[0] : item.ElementoDaLuva.GetValueOrDefault();
                Elemento e1 = par.Length > 1 ? par[1] : e0;
                if (item.ArmaId != _id || n != _nEls || e0 != _e0 || e1 != _e1) { Vestir(item.ArmaId, n, e0, e1); vestiu = true; }
                if (!_temPos || item.Pos != _pos)
                {
                    _temPos = true;
                    _pos = item.Pos;
                    // o chao da ILHA, nao o y do item: o treino poe a luva 0,4 m acima e o disco flutuaria
                    float chao = Ilha.Atual != null ? Ilha.AlturaDoChao(_pos.x, _pos.z) : _pos.y;
                    _raiz.transform.position = new Vector3(_pos.x, chao, _pos.z);
                }
                float fase = _pos.x * 0.37f + _pos.z * 0.61f;   // dessincroniza: 16 luvas subindo juntas parecem uma engrenagem so'
                float y = Loot.ALTURA + Mathf.Sin(t * (2f * Mathf.PI / LootVisual.FLUTUA_S) + fase) * LootVisual.FLUTUA_M;
                _pivo.localPosition = new Vector3(0f, y, 0f);
                _pivo.localRotation = Quaternion.Euler(0f, t * (360f / LootVisual.GIRO_S) + fase * Mathf.Rad2Deg, 0f);
                // o PULSO da marca: respira na escala e no brilho; o feixe respira junto (le' como luz viva)
                float pulso = 0.5f + 0.5f * Mathf.Sin(t * (2f * Mathf.PI / LootVisual.PULSO_S) + fase);
                _marca.transform.localScale = Vector3.one * (LootVisual.RAIO_MARCA * (1f + 0.06f * pulso));
                Pintar(_marca, Brilho(_corMarca, 0.7f + 0.3f * pulso));
                if (_coluna.activeSelf) _feixe.widthMultiplier = LootVisual.LARGURA_FEIXE * (1f + 0.08f * pulso);
                Ativo(_raiz, true);
                if (acordou || vestiu) { Tocar(_aura); Tocar(_motas); Tocar(_halos[0]); Tocar(_halos[1]); }
            }

            void Vestir(string id, int n, Elemento e0, Elemento e1)
            {
                _id = id; _nEls = n; _e0 = e0; _e1 = e1;
                // um modelo por armaId, criado uma vez e religado: pegar de maos nuas faz a lista andar
                string chave = id ?? "";
                GameObject modelo;
                if (!_modelos.TryGetValue(chave, out modelo) || modelo == null)
                {
                    // a MESMA luva que o Pawn veste (LuvaVisual.Modelo): de pe', no quadro da mao direita
                    modelo = LuvaVisual.Modelo(id, _pivo, LootVisual.TAMANHO_LUVA);
                    if (modelo == null)
                    {
                        // sem o .glb: a luva procedural da mao, ampliada (o que se ve' e' o que se equipa)
                        modelo = LuvaVisual.Criar(_pivo, id, new[] { e0 });
                        modelo.transform.localScale = Vector3.one * 1.5f;   // a da mao tem ~0,34 m; o .glb do chao, TAMANHO_LUVA
                    }
                    _modelos[chave] = modelo;
                }
                foreach (KeyValuePair<string, GameObject> kv in _modelos)
                    if (kv.Value != null) Ativo(kv.Value, kv.Value == modelo);

                Color cor = Arma.Cor(id);
                Color clara = Color.Lerp(cor, Color.white, 0.6f);
                string raridade = Arma.Raridade(id);
                float forca = LootVisual.Forca(raridade);
                _marcaMf.sharedMesh = MalhaMarca(Arma.Forma(id));
                _corMarca = Brilho(cor, LootVisual.BRILHO_MARCA * forca);
                ParticleSystem.MainModule ma = _aura.main;
                ma.startColor = new ParticleSystem.MinMaxGradient(cor, clara);
                Taxa(_aura, LootVisual.AURA_S * forca);
                float h = LootVisual.AlturaDaColuna(raridade);
                Ativo(_coluna, h > 0f);
                if (h > 0f)
                {
                    Acender(_feixe, h, LootVisual.LARGURA_FEIXE, cor, 0.7f, 1f);
                    Acender(_fio, h, LootVisual.LARGURA_FIO, Color.Lerp(cor, Color.white, 0.55f), 1f, 0.75f);
                    ParticleSystem.MainModule mm = _motas.main;
                    mm.startColor = new ParticleSystem.MinMaxGradient(cor, clara);
                    Taxa(_motas, LootVisual.MOTAS_S * forca);
                }
                for (int k = 0; k < 2; k++)
                {
                    bool liga = k < n;
                    Ativo(_nucleos[k].gameObject, liga);
                    if (!liga) continue;
                    // o ELEMENTO se le' no nucleo (cor ja' pelo filtro de daltonismo); manopla mostra o PAR. A MESMA cor, acesa:
                    // HDR no miolo e no halo (a particula viva nao rele' startColor: o halo pinta por MaterialPropertyBlock)
                    Color ce = Arkana.Menu.Estilo.CorElemento(k == 0 ? e0 : e1);
                    _nucleos[k].sharedMaterial = Opaco(Brilho(ce, LootVisual.BRILHO_NUCLEO));
                    Pintar(_halos[k].GetComponent<Renderer>(), Brilho(ce, LootVisual.BRILHO_HALO));
                    float x = n == 2 ? (k == 0 ? -0.1f : 0.1f) : 0f;
                    _nucleos[k].transform.localPosition = new Vector3(x, LootVisual.NUCLEO_Y, 0f);
                }
            }
        }

        // ---------------------------------------------------------------- bau celestial

        void MontarBau()
        {
            Color ouro = _ouro = Arma.Cor(Arma.MANOPLA);   // a cor da LENDARIA, a mesma da manopla que esta' dentro
            Color ouroClaro = Color.Lerp(ouro, Color.white, 0.6f);
            _bau = new GameObject("BauCelestial");
            _bau.transform.SetParent(transform, false);
            _bau.SetActive(false);
            _bauPivo = new GameObject("Pivo").transform;
            _bauPivo.SetParent(_bau.transform, false);
            if (Modelo(BauVisual.MODELO, _bauPivo, BauVisual.TAMANHO, true) == null)
            {
                // sem o .glb: caixa de madeira + cinta de ouro — le' "bau" a 50 m
                SemSombra(MaterialMago.Primitivo(_bauPivo, "Caixa", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f),
                    new Vector3(1.15f, 0.7f, 0.85f), Opaco(new Color32(0x3A, 0x2B, 0x1E, 255))).GetComponent<Renderer>());
                SemSombra(MaterialMago.Primitivo(_bauPivo, "Cinta", PrimitiveType.Cube, new Vector3(0f, 0.36f, 0f),
                    new Vector3(0.14f, 0.74f, 0.9f), Opaco(ouro)).GetComponent<Renderer>());
            }
            // COMETA na queda: rastro aditivo de borda macia + faiscas de MUNDO (ficam no ar por onde ele passou)
            _rastro = _bauPivo.gameObject.AddComponent<TrailRenderer>();
            _rastro.time = 1.2f;
            _rastro.startWidth = 1.5f;
            _rastro.endWidth = 0f;
            _rastro.minVertexDistance = 0.5f;
            _rastro.emitting = false;
            _rastro.sharedMaterial = MaterialVfx.DeLinha();
            _rastro.colorGradient = Degrade(ouro, 1f, 1f);
            SemSombra(_rastro);
            _faiscas = ParticulaVfx.Novo(_bauPivo, "Faiscas", ouroClaro, ouro, 0f,
                new Vector2(0.5f, 1.2f), new Vector2(0.5f, 2.5f), new Vector2(0.1f, 0.26f), true, 1f, 90);
            _faiscas.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            ParticleSystem.ShapeModule sf = _faiscas.shape;
            sf.scale = new Vector3(0.9f, 0.9f, 0.6f);
            // o FEIXE e' o farol: do anuncio ate' abrir, denuncia o ponto e quem esta' la' (Godot: fica aceso no pouso).
            // Mesma lingua da coluna do loot, maior: faixa + fio quente + motas subindo ate' o alto da queda.
            _feixeBau = LinhaDeLuz(_bau.transform, "Feixe", 0.6f);
            Acender(_feixeBau, BauCelestial.ALTURA_QUEDA, BauVisual.RAIO_FEIXE * 2f, ouro, 0.75f, 1f);
            Acender(LinhaDeLuz(_bau.transform, "Fio", 0.4f), BauCelestial.ALTURA_QUEDA, BauVisual.RAIO_FEIXE * 0.5f, ouroClaro, 1f, 0.7f);
            ParticleSystem motas = ParticulaVfx.Novo(_bau.transform, "Motas", ouroClaro, ouro, 14f,
                new Vector2(2f, 3.5f), new Vector2(4f, 11f), new Vector2(0.15f, 0.4f), false, 0.03f, 64);
            ParticleSystem.ShapeModule sm = motas.shape;
            sm.scale = new Vector3(1.2f, 1.2f, 0.3f);
            // AURA dourada do bau pousado (sobe girando em volta dele) e a CANALIZACAO (faiscas saem do anel de abrir e
            // espiralam para dentro do bau — o jogador ve' a energia sendo puxada). As duas crescem com o progresso.
            _auraBau = Orbita(_bau.transform, "Aura", 0.8f, 0.9f, 1.6f, 0f, new Vector2(1f, 1.8f), new Vector2(0.08f, 0.22f), 70);
            _auraBau.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            _canal = Orbita(_bau.transform, "Canal", BauCelestial.RAIO_ABRIR * 0.94f, 0.5f, 1.2f, -1.7f,
                new Vector2(1.2f, 1.4f), new Vector2(0.1f, 0.24f), 70);
            _canal.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            ParticleSystem.MainModule ma = _auraBau.main;
            ma.startColor = new ParticleSystem.MinMaxGradient(ouro, ouroClaro);
            ParticleSystem.MainModule mc = _canal.main;
            mc.startColor = new ParticleSystem.MinMaxGradient(ouro, ouroClaro);
            _bauPs = new[] { _faiscas, motas, _auraBau, _canal };
            // a MARCA tem o raio de abrir: mostra ONDE ficar para canalizar (neon dourado, o miolo acende o chao)
            _marcaBauR = Malha(_bau.transform, "Marca", MalhaMarca(48, 0.88f), MaterialBrilho());
            _marcaBau = _marcaBauR.transform;
            _marcaBau.localPosition = new Vector3(0f, 0.08f, 0f);
            _progresso = Linha(_bau.transform, "Progresso", MaterialVfx.DeLinha(), 0.5f);
            _progresso.startColor = ouro;
            _progresso.endColor = ouro;
            _progresso.loop = false;
        }

        void DesenharBau(float t)
        {
            BauCelestial bau = _partida.Bau;
            if (bau != _bauVisto)
            {
                _bauVisto = bau;
                _faseVista = BauCelestial.Fase.Esperando;
                if (bau != null) _bau.transform.position = bau.Pos;
            }
            if (bau == null) { Ativo(_bau, false); return; }   // treino: nao ha' bau

            BauCelestial.Fase f = bau.FaseAtual;
            bool telegrafo = f == BauCelestial.Fase.Caindo || f == BauCelestial.Fase.Pousado;
            bool acordou = telegrafo && !_bau.activeSelf;
            Ativo(_bau, telegrafo);
            if (acordou) for (int i = 0; i < _bauPs.Length; i++) Tocar(_bauPs[i]);
            if (f != _faseVista)
            {
                // BORDA de fase: o estouro sai uma vez (ele mora fora de _bau para sobreviver ao sumico)
                if (f == BauCelestial.Fase.Caindo) _rastro.Clear();
                else if (f == BauCelestial.Fase.Pousado) Estourar(bau.Pos + new Vector3(0f, 0.4f, 0f), _ouro, 26);
                else if (f == BauCelestial.Fase.Aberto) Estourar(bau.Pos + new Vector3(0f, 1f, 0f), _ouro, 40);
                _faseVista = f;
            }
            if (!telegrafo) return;

            bool caindo = f == BauCelestial.Fase.Caindo;
            float t01 = caindo ? 1f - bau.Restante / BauCelestial.QUEDA_S : 1f;
            _bauPivo.position = BauVisual.PosNaQueda(bau.Pos, t01);
            _bauPivo.localRotation = Quaternion.Euler(0f, t * 40f, 0f);   // giro lento: le' como objeto magico
            _rastro.emitting = caindo;
            float p = caindo ? 0f : bau.Progresso;
            Taxa(_faiscas, caindo ? 50f : 0f);
            Taxa(_auraBau, caindo ? 0f : 10f + 30f * p);
            Taxa(_canal, p > 0f ? 12f + 36f * p : 0f);
            float pulso = 0.5f + 0.5f * Mathf.Sin(t * 5f);
            _marcaBau.localScale = Vector3.one * (BauCelestial.RAIO_ABRIR * (1f + 0.06f * (2f * pulso - 1f)));
            Pintar(_marcaBauR, Brilho(_ouro, BauVisual.BRILHO_MARCA * (0.75f + 0.25f * pulso) * (1f + p)));
            _feixeBau.widthMultiplier = BauVisual.RAIO_FEIXE * 2f * (1f + 0.5f * p + 0.06f * pulso);
            Ativo(_progresso.gameObject, p > 0f);
            if (p > 0f) Arco(_progresso, bau.Pos + new Vector3(0f, 0.12f, 0f), BauCelestial.RAIO_ABRIR * 0.94f, p);
        }

        /// <summary>Anel de progresso 0..1 a partir do norte, no sentido do relogio (le' como ponteiro).</summary>
        static void Arco(LineRenderer lr, Vector3 centro, float r, float p)
        {
            float q = Mathf.Clamp01(p);
            int n = 2 + Mathf.RoundToInt(q * 46f);
            if (lr.positionCount != n) lr.positionCount = n;
            float volta = q * Mathf.PI * 2f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * 0.5f - volta * i / (n - 1);
                lr.SetPosition(i, centro + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
        }

        // ---------------------------------------------------------------- zona

        void MontarZona()
        {
            _parede = Malha(transform, "ParedeDaZona", MalhaParede(), MaterialParede()).gameObject;
            // o PE' aceso: a mesma casca, curta, filha da parede (herda a escala do raio: zero conta por frame)
            _pe = Malha(_parede.transform, "Pe", MalhaPe(), MaterialBrilho());
            _parede.SetActive(false);
            _anelZona = Linha(transform, "ProximoCirculo", MaterialVfx.DeLinha(), ZonaVisual.LARGURA_ANEL);
            _anelZona.startColor = ComAlfa(ZonaVisual.COR, 0.7f);
            _anelZona.endColor = ComAlfa(ZonaVisual.COR, 0.7f);
            _anelZona.loop = true;
            _anelZona.positionCount = ZonaVisual.PONTOS_ANEL;
            _anelZona.gameObject.SetActive(false);
        }

        void DesenharZona(float t)
        {
            Zona z = _partida.Zona;
            // Ativa, nao Ligada: nos 70 s de abertura a tempestade ainda nao existe (Zona.gd: parede invisivel ate' formar)
            bool ativa = z != null && z.Ativa;
            Ativo(_parede, ativa);
            if (ativa)
            {
                float r = Mathf.Max(z.Raio, 0.05f);   // a fase 5 fecha em ZERO: escala 0 e' transform degenerada
                _parede.transform.position = new Vector3(z.Centro.x, ZonaVisual.BASE_PAREDE, z.Centro.z);
                _parede.transform.localScale = new Vector3(r, 1f, r);
                Material m = MaterialParede();
                // os filetes SOBEM (~4 m/s) e a trama gira devagar: energia correndo, nao pintura parada
                if (m.HasProperty("_BaseMap"))
                    m.SetTextureOffset("_BaseMap", new Vector2(Mathf.Repeat(t * 0.004f, 1f), Mathf.Repeat(-t * 0.06f, 1f)));
                Pintar(_pe, ComAlfa(Brilho(ZonaVisual.COR, ZonaVisual.BRILHO_PE), 0.55f + 0.15f * Mathf.Sin(t * 1.3f)));
            }

            Zona.Circulo c;
            if (!ZonaVisual.Proximo(z, out c)) { Ativo(_anelZona.gameObject, false); return; }
            Ativo(_anelZona.gameObject, true);
            float ra = Mathf.Max(c.Raio, 1f);   // o circulo final e' um ponto: 1 m ainda mostra ONDE
            if (c.Centro == _anelCentro && ra == _anelRaio) return;   // refaz 1x por fase, nao por frame
            _anelCentro = c.Centro;
            _anelRaio = ra;
            // ponytail: o anel deita no relevo so' nos 96 pontos; entre eles pode cortar morro. Sem ZTest no Unlit do URP.
            for (int i = 0; i < ZonaVisual.PONTOS_ANEL; i++)
            {
                float a = i * Mathf.PI * 2f / ZonaVisual.PONTOS_ANEL;
                float x = c.Centro.x + Mathf.Cos(a) * ra, zz = c.Centro.z + Mathf.Sin(a) * ra;
                float y = Mathf.Max(Ilha.AlturaDoChao(x, zz), Ilha.SuperficieDaAgua(x, zz)) + ZonaVisual.ALTURA_ANEL;
                _anelZona.SetPosition(i, new Vector3(x, y, zz));
            }
        }

        // ---------------------------------------------------------------- estouro

        void MontarEstouro()
        {
            var go = new GameObject("Estouro");
            go.transform.SetParent(transform, false);
            go.SetActive(false);   // configura DESLIGADO: mexer no sistema tocando pode logar (e o PlayMode reprova log)
            _estouro = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _estouro.main;
            main.loop = true;          // container parado: nada nasce sozinho, Emit(n) e' o estouro
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            ParticleSystem.EmissionModule em = _estouro.emission;
            em.rateOverTime = 0f;
            ParticleSystem.ShapeModule sh = _estouro.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.5f;
            ParticleSystem.ColorOverLifetimeModule col = _estouro.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            if (r == null) r = go.AddComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = MaterialVfx.DeParticula();   // aditivo HDR: o estouro ACENDE no bloom
            SemSombra(r);
            go.SetActive(true);
        }

        void Estourar(Vector3 pos, Color cor, int n)
        {
            _estouro.transform.position = pos;   // espaco de MUNDO: as particulas velhas ficam onde nasceram
            ParticleSystem.MainModule main = _estouro.main;
            main.startColor = cor;
            _estouro.Emit(n);
        }

        // ---------------------------------------------------------------- fabrica (malha, material, textura, vfx)

        static void Ativo(GameObject go, bool on)
        {
            if (go.activeSelf != on) go.SetActive(on);
        }

        static void SemSombra(Renderer r)
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static Color ComAlfa(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>Cor acima de 1 (acende no bloom) com alfa 1: Color * k multiplicaria o alfa, e no aditivo sairia k^2.</summary>
        static Color Brilho(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        static MaterialPropertyBlock _mpb;
        static int _idCor;

        /// <summary>Cor por objeto sem material novo (o pulso por frame): um bloco so', zero lixo.</summary>
        static void Pintar(Renderer r, Color c)
        {
            _mpb.SetColor(_idCor, c);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Sistema de particula tocando; o que dormiu com o GameObject acorda aqui (so' nas bordas, nao por frame).</summary>
        static void Tocar(ParticleSystem ps)
        {
            if (ps.gameObject.activeInHierarchy && !ps.isPlaying) ps.Play(true);
        }

        static void Taxa(ParticleSystem ps, float porSegundo)
        {
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTimeMultiplier = porSegundo;
        }

        /// <summary>
        /// Emissor em ANEL deitado (raio r) cujas faiscas GIRAM em volta do centro e sobem devagar: aura de objeto magico.
        /// `radial` negativo puxa para o centro (a canalizacao). LOCAL: gira em volta do dono. Todas as curvas de
        /// velocidade em modo CONSTANTE, escritas uma a uma: modos misturados logam erro (e o PlayMode reprova log).
        /// </summary>
        static ParticleSystem Orbita(Transform pai, string nome, float r, float sobe, float gira, float radial,
            Vector2 vida, Vector2 tam, int max)
        {
            ParticleSystem ps = ParticulaVfx.Novo(pai, nome, Color.white, Color.white, 0f, vida, Vector2.zero, tam, false, 0f, max);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;   // plano XY do emissor = o chao (o Novo gira -90 em X)
            sh.radius = r;
            sh.radiusThickness = 0f;   // so' a borda do anel
            ParticleSystem.VelocityOverLifetimeModule v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.Local;
            v.x = 0f;
            v.y = 0f;
            v.z = sobe;   // z local = cima
            v.orbitalX = 0f;
            v.orbitalY = 0f;
            v.orbitalZ = gira;
            v.radial = radial;
            return ps;
        }

        /// <summary>
        /// HALO: UMA particula de ponto macio aditivo (DeParticula), imortal, no centro do pai — billboard de graca. A taxa
        /// com teto 1 repoe a particula em 0,1 s se o GameObject dormir e acordar. Escala LOCAL explicita: o pai (o nucleo)
        /// tem 0,15 de escala, e na Hierarchy o halo encolheria para dentro da esfera.
        /// </summary>
        static ParticleSystem Halo(Transform pai)
        {
            ParticleSystem ps = ParticulaVfx.Novo(pai, "Halo", Color.white, Color.white, 10f, new Vector2(1e5f, 1e5f),
                Vector2.zero, new Vector2(LootVisual.HALO_M, LootVisual.HALO_M), false, 0f, 1);
            ParticleSystem.MainModule m = ps.main;
            m.scalingMode = ParticleSystemScalingMode.Local;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = false;   // nasce no centro, nao na caixa de 1 m do Novo
            return ps;
        }

        /// <summary>
        /// Faixa de luz em pe' (farol): LineRenderer LOCAL virado para a camera, aditivo HDR de borda macia
        /// (MaterialVfx.DeLinha, a do feixe dos kits) — nao tem face, entao nao le' caixa de nenhum angulo. `afina` =
        /// largura no alto / no pe' (a perspectiva de feixe subindo para o ceu).
        /// </summary>
        static LineRenderer LinhaDeLuz(Transform pai, string nome, float afina)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.alignment = LineAlignment.View;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.positionCount = PONTOS_FEIXE;
            lr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, afina);
            lr.sharedMaterial = MaterialVfx.DeLinha();
            SemSombra(lr);
            return lr;
        }

        /// <summary>Ergue a faixa ate' `altura`: FORTE no pe', some no alto (zera em `someEm` do comprimento).</summary>
        static void Acender(LineRenderer lr, float altura, float largura, Color cor, float alfaPe, float someEm)
        {
            for (int i = 0; i < PONTOS_FEIXE; i++) lr.SetPosition(i, new Vector3(0f, altura * i / (PONTOS_FEIXE - 1), 0f));
            lr.widthMultiplier = largura;
            lr.colorGradient = Degrade(cor, alfaPe, someEm);
        }

        static Gradient _degrade;
        static readonly GradientColorKey[] _chavesCor = new GradientColorKey[2];
        static readonly GradientAlphaKey[] _chavesAlfa = new GradientAlphaKey[3];

        /// <summary>Degrade de alfa em curva (cheio, 40% a 30% do caminho, zero em `someEm`). Um Gradient de rascunho:
        /// o setter do LineRenderer/TrailRenderer COPIA, entao o mesmo objeto serve a todos.</summary>
        static Gradient Degrade(Color cor, float alfa0, float someEm)
        {
            if (_degrade == null) _degrade = new Gradient();
            _chavesCor[0] = new GradientColorKey(cor, 0f);
            _chavesCor[1] = new GradientColorKey(cor, 1f);
            _chavesAlfa[0] = new GradientAlphaKey(alfa0, 0f);
            _chavesAlfa[1] = new GradientAlphaKey(alfa0 * 0.4f, someEm * 0.3f);
            _chavesAlfa[2] = new GradientAlphaKey(0f, someEm);
            _degrade.SetKeys(_chavesCor, _chavesAlfa);
            return _degrade;
        }

        /// <summary>MeshFilter + MeshRenderer, SEM colisor (camera e mira atravessam o visual).</summary>
        static MeshRenderer Malha(Transform pai, string nome, Mesh mesh, Material mat)
        {
            var go = new GameObject(nome, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(pai, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            SemSombra(r);
            return r;
        }

        static LineRenderer Linha(Transform pai, string nome, Material mat, float largura)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = largura;
            lr.endWidth = largura;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.positionCount = 0;
            lr.sharedMaterial = mat;
            SemSombra(lr);
            return lr;
        }

        /// <summary>
        /// Instancia um .glb de Resources com o MAIOR lado em `tamanho` m (a Meshy normaliza tudo em ~1,9 u).
        /// Mede nos bounds na origem, depois pendura. `peNoChao` = base em y=0 (bau); senao centro no pivo (luva).
        /// Ausente -> null, sem log: quem chama poe a primitiva.
        /// </summary>
        public static GameObject Modelo(string nome, Transform pai, float tamanho, bool peNoChao)
        {
            if (string.IsNullOrEmpty(nome)) return null;
            GameObject prefab = Resources.Load<GameObject>(nome);
            if (prefab == null) return null;
            GameObject inst = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Renderer[] rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { Destroy(inst); return null; }
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            float lado = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float k = lado > 0.001f ? tamanho / lado : 1f;
            foreach (Collider c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
            inst.transform.SetParent(pai, false);
            inst.transform.localScale *= k;
            Vector3 centro = b.center * k;
            inst.transform.localPosition = peNoChao ? new Vector3(-centro.x, -b.min.y * k, -centro.z) : -centro;
            return inst;
        }

        static Mesh _malhaParede, _malhaPe;
        static readonly Dictionary<Vector2, Mesh> _marcas = new Dictionary<Vector2, Mesh>();

        static Mesh MalhaParede()
        {
            if (_malhaParede == null) _malhaParede = MalhaDaParede.Cilindro(MalhaDaParede.SEGMENTOS, MalhaDaParede.ALTURA).ParaMesh("ParedeDaZona");
            return _malhaParede;
        }

        /// <summary>O pe' aceso: a casca da parede, curta. O v (0 no pe', 1 no alto) cai na faixa macia: pico no meio.</summary>
        static Mesh MalhaPe()
        {
            if (_malhaPe == null) _malhaPe = MalhaDaParede.Cilindro(MalhaDaParede.SEGMENTOS, ZonaVisual.ALTURA_PE).ParaMesh("PeDaZona");
            return _malhaPe;
        }

        /// <summary>A FORMA da raridade no chao (GDD §10: quem nao distingue cor le' a forma).</summary>
        static Mesh MalhaMarca(string forma)
        {
            switch (forma)
            {
                case "losango": return MalhaMarca(4, 0.55f);
                case "triangulo": return MalhaMarca(3, 0.45f);
                default: return MalhaMarca(32, 0.76f);
            }
        }

        static Mesh MalhaMarca(int lados, float furo)
        {
            var chave = new Vector2(lados, furo);
            Mesh m;
            if (_marcas.TryGetValue(chave, out m) && m != null) return m;
            m = MalhaDaMarca.Neon(lados, furo).ParaMesh("Marca" + lados);
            _marcas[chave] = m;
            return m;
        }

        static readonly Dictionary<Color, Material> _opacos = new Dictionary<Color, Material>();
        static Material _matParede, _matBrilho;

        static Material Opaco(Color c)
        {
            Material m;
            if (_opacos.TryGetValue(c, out m) && m != null) return m;
            m = NovoMaterial(SHADER_UNLIT, false);
            MaterialMago.Pintar(m, c);
            _opacos[c] = m;
            return m;
        }

        /// <summary>A faixa macia ADITIVA no Unlit (le' o V da malha, sem cor de vertice): neon das marcas e pe' da parede.
        /// Um material para todos — a cor (e o pulso) vai por MaterialPropertyBlock.</summary>
        static Material MaterialBrilho()
        {
            if (_matBrilho == null)
            {
                _matBrilho = MaterialVfx.Novo(MaterialVfx.Unlit, Color.white, MaterialVfx.Mistura.Aditivo, false, MaterialVfx.FaixaSuave());
                if (_matBrilho != null) _matBrilho.name = "Brilho";
            }
            return _matBrilho;
        }

        /// <summary>Um so' no processo: o offset de UV rola por frame nele (a parede "anda").</summary>
        static Material MaterialParede()
        {
            if (_matParede != null) return _matParede;
            _matParede = NovoMaterial(SHADER_UNLIT, true);
            _matParede.name = "ParedeDaZona";
            // a COR mora na textura (nevoa violeta, filete claro); o material so' passa o brilho de 1 (HDR)
            float k = ZonaVisual.BRILHO_PAREDE;
            MaterialMago.Pintar(_matParede, new Color(k, k, k, 1f));
            if (_matParede.HasProperty("_BaseMap"))
            {
                _matParede.SetTexture("_BaseMap", TexturaTempestade());
                _matParede.SetTextureScale("_BaseMap", new Vector2(ZonaVisual.REPETE_U, ZonaVisual.REPETE_V));
            }
            return _matParede;
        }

        /// <summary>Os dois shaders estao em Build.ShadersDoCodigo (no aparelho o Shader.Find nao volta null).</summary>
        static Material NovoMaterial(string shader, bool transparente)
        {
            Shader s = Shader.Find(shader);
            if (s == null) s = Shader.Find(SHADER_UNLIT);
            if (s == null) s = Shader.Find("Unlit/Color");
            Material m = s != null ? new Material(s) : new Material(Ilha.MaterialPadrao());
            if (transparente) Transparente(m);
            return m;
        }

        /// <summary>
        /// Transparencia do URP 17 por codigo (Unlit e Particles/Unlit tem as mesmas chaves). O blend e' ESTADO de
        /// render lido destes floats, e o alfa sai por IsSurfaceTypeTransparent(_Surface) — float, nao keyword.
        /// A keyword so' desliga o SSAO no fragmento: mesmo que o build pode a variante, a transparencia vale.
        /// </summary>
        static void Transparente(Material m)
        {
            m.SetFloat("_Surface", 1f);   // Transparent
            m.SetFloat("_Blend", 0f);     // Alpha
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;   // 3000
        }

        static Texture2D _texTempestade;

        /// <summary>O ladrilho da parede (ZonaVisual.Tempestade) com MIPMAP: filete fino sem mip cintila a 200 m de distancia.
        /// 128x64 RGBA + mips = ~43 KB, gerado 1x.</summary>
        static Texture2D TexturaTempestade()
        {
            if (_texTempestade != null) return _texTempestade;
            const int w = ZonaVisual.TEX_U, h = ZonaVisual.TEX_V;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = "Tempestade",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = ZonaVisual.Tempestade(x, y);
            tex.SetPixels32(px);
            tex.Apply(true, true);   // gera os mips e solta a copia da CPU: memoria de celular
            return _texTempestade = tex;
        }
    }
}
