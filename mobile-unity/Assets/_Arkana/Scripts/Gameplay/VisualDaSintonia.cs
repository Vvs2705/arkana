using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    // ================================================================================ LOGICA PURA (EditMode roda sem cena)

    /// <summary>
    /// A ZONA DEITADA NO RELEVO (onda 18A). O disco chapado numa cota so' CORTAVA a ladeira: meia pizza de lava, meio disco de
    /// lama, e o ARCO BRANCO solto no chao era a onda de choque plana enterrada pela metade (HDR quase branco). Aqui toda
    /// zona e' uma GRADE POLAR — centro + aneis — com a altura do chao DESENHADO perguntada em CADA vertice, `Acima` m por
    /// cima. O vertice leva o dado do shader Arkana/SintoniaZona: COLOR.r = raio relativo (0..1), COLOR.a = alfa radial (a
    /// borda esmaece), UV = metros no plano (o ruido tem tamanho de mundo). A raiz fica em (cx, 0, cz): y do vertice e' de mundo.
    /// </summary>
    public static class ZonaNoRelevo
    {
        /// <summary>m acima do chao desenhado (o shader ainda puxa na profundidade: Offset -1).</summary>
        public const float Acima = 0.12f;
        /// <summary>Fracao do raio (a de fora) em que o alfa cai de 1 a 0.</summary>
        public const float Borda = 0.3f;
        /// <summary>m entre aneis: a dobra do chao (quad de 4,5 m) ganha 4 amostras.</summary>
        public const float PassoM = 1.1f;

        public static int Aneis(float raio) => Mathf.Clamp(Mathf.CeilToInt(raio / PassoM), 2, 14);
        public static int Gomos(float raio) => Mathf.Clamp(Mathf.CeilToInt(raio * 4.5f), 20, 56);
        public static int Vertices(int aneis, int gomos) => 1 + aneis * gomos;

        /// <summary>Alfa radial: 1 no miolo; na `Borda` de fora desce suave (smoothstep) ate' 0 no raio.</summary>
        public static float Fade(float rad)
        {
            float u = Mathf.Clamp01((rad - (1f - Borda)) / Borda);
            return 1f - u * u * (3f - 2f * u);
        }

        /// <summary>Indices: leque do centro ao 1o anel + faixas entre aneis (o ultimo gomo fecha no primeiro).</summary>
        public static int[] Triangulos(int aneis, int gomos)
        {
            var t = new int[3 * gomos * (2 * aneis - 1)];
            int k = 0;
            for (int g = 0; g < gomos; g++) { t[k++] = 0; t[k++] = 1 + (g + 1) % gomos; t[k++] = 1 + g; }
            for (int a = 1; a < aneis; a++)
            {
                int i0 = 1 + (a - 1) * gomos, i1 = 1 + a * gomos;
                for (int g = 0; g < gomos; g++)
                {
                    int g1 = (g + 1) % gomos;
                    t[k++] = i0 + g; t[k++] = i0 + g1; t[k++] = i1 + g;
                    t[k++] = i0 + g1; t[k++] = i1 + g1; t[k++] = i1 + g;
                }
            }
            return t;
        }

        /// <summary>
        /// Deita a grade em `centro` (so' x,z contam): x,z LOCAIS, y = max(chao(x,z), piso) + Acima perguntado NO PROPRIO
        /// vertice — nunca no centro. `piso` = lamina d'agua sob o centro (a zona boia no lago; seco = Relevo.Seco). So' a
        /// posicao depende do chao: `uv`/`cor` null = ja' escritos (reamostra do tornado que anda).
        /// </summary>
        public static void Amostrar(Vector3 centro, float raio, int aneis, int gomos, System.Func<float, float, float> chao, float piso,
            Vector3[] v, Vector2[] uv, Color32[] cor)
        {
            v[0] = new Vector3(0f, Mathf.Max(chao(centro.x, centro.z), piso) + Acima, 0f);
            if (uv != null) uv[0] = Vector2.zero;
            if (cor != null) cor[0] = new Color32(0, 0, 0, 255);
            int i = 1;
            for (int a = 1; a <= aneis; a++)
            {
                float rad = a / (float)aneis, r = rad * raio;
                byte br = (byte)Mathf.RoundToInt(rad * 255f), ba = (byte)Mathf.RoundToInt(Fade(rad) * 255f);
                for (int g = 0; g < gomos; g++, i++)
                {
                    float ang = g * (Mathf.PI * 2f / gomos);
                    float x = Mathf.Cos(ang) * r, z = Mathf.Sin(ang) * r;
                    v[i] = new Vector3(x, Mathf.Max(chao(centro.x + x, centro.z + z), piso) + Acima, z);
                    if (uv != null) uv[i] = new Vector2(x, z);
                    if (cor != null) cor[i] = new Color32(br, 0, 0, ba);
                }
            }
        }
    }

    /// <summary>
    /// O chao DESENHADO — a malha de Ilha.Quads com a divisao de triangulo da Ilha (a conta do Vegetacao.ChaoDesenhado e do
    /// GradeDoChao.AlturaNaMalha) — com MEMORIA dos nos. A Altura() exata passa ate' 0,3 m da malha nas dobras (a zona
    /// boiaria ou enterraria), e o ChaoDesenhado paga 4 Altura() por pergunta: uma zona de 600 vertices toca ~50 nos. A ilha
    /// nao muda na partida: cada no' e' calculado UMA vez (133^2 floats = 70 KB), o tornado reamostra de graca.
    /// </summary>
    public sealed class ChaoDesenhadoMemo
    {
        readonly Relevo _r;
        readonly int _q;
        readonly float[] _h;

        public ChaoDesenhadoMemo(Relevo r, int quads = Ilha.Quads)
        {
            _r = r;
            _q = Mathf.Max(1, quads);
            _h = new float[(_q + 1) * (_q + 1)];
            for (int i = 0; i < _h.Length; i++) _h[i] = float.NaN;
        }

        public Relevo Relevo => _r;

        float No(int ix, int iz)
        {
            int k = iz * (_q + 1) + ix;
            float h = _h[k];
            if (float.IsNaN(h)) _h[k] = h = _r.Altura(((float)ix / _q - 0.5f) * _r.Lado, ((float)iz / _q - 0.5f) * _r.Lado);
            return h;
        }

        public float Altura(float x, float z)
        {
            float fx = Mathf.Clamp((x / _r.Lado + 0.5f) * _q, 0f, _q - 1e-4f), fz = Mathf.Clamp((z / _r.Lado + 0.5f) * _q, 0f, _q - 1e-4f);
            int ix = (int)fx, iz = (int)fz;
            float u = fx - ix, v = fz - iz;
            float ha = No(ix, iz), hb = No(ix + 1, iz), hc = No(ix, iz + 1), hd = No(ix + 1, iz + 1);
            if (u + v <= 1f) return ha + (hb - ha) * u + (hc - ha) * v;
            return hd + (hc - hd) * (1f - u) + (hb - hd) * (1f - v);
        }
    }

    /// <summary>Pool por tipo, PURO: a casca diz como criar e como mostrar/esconder. Devolver duas vezes NAO empilha duas
    /// vezes — o mesmo item emprestado a dois efeitos e' como um anel fica preso na tela.</summary>
    public sealed class PoolVfx<T> where T : class
    {
        readonly System.Func<int, T> _novo;
        readonly System.Action<T, bool> _mostrar;
        readonly Dictionary<int, Stack<T>> _livres = new Dictionary<int, Stack<T>>();
        readonly HashSet<T> _fora = new HashSet<T>();

        public PoolVfx(System.Func<int, T> novo, System.Action<T, bool> mostrar) { _novo = novo; _mostrar = mostrar; }

        /// <summary>Quantos estao na tela agora (0 = nada sobrou).</summary>
        public int Emprestados => _fora.Count;
        public int Criados { get; private set; }

        public T Pegar(int tipo)
        {
            Stack<T> s;
            T it;
            if (_livres.TryGetValue(tipo, out s) && s.Count > 0) it = s.Pop();
            else { it = _novo(tipo); Criados++; }
            _fora.Add(it);
            _mostrar?.Invoke(it, true);
            return it;
        }

        /// <summary>Esconde e guarda. Falso = nulo ou ja' devolvido (nao faz nada).</summary>
        public bool Devolver(int tipo, T it)
        {
            if (it == null || !_fora.Remove(it)) return false;
            _mostrar?.Invoke(it, false);
            Stack<T> s;
            if (!_livres.TryGetValue(tipo, out s)) { s = new Stack<T>(); _livres[tipo] = s; }
            s.Push(it);
            return true;
        }
    }

    /// <summary>Um golpe na tela (o instante do combo) ou o que FICOU sumindo depois de acabar (Fonte != null).</summary>
    public sealed class GolpeDaSintonia<T> where T : class
    {
        public ComboSintonia Combo;
        public Vector3 Pos;
        public float Raio, Dur, Resta, Acc;
        public T Onda, Forma;
        public int TipoOnda, TipoForma;
        /// <summary>O que ficou e acabou: a casca o desenha pela Persistente, com o alfa descendo ate' sumir.</summary>
        public SintoniaEfeitos.Persistente Fonte;
        public float U => Dur > 0f ? Mathf.Clamp01(1f - Resta / Dur) : 1f;
    }

    /// <summary>
    /// O CICLO da casca, PURO: quem entra na tela e quem sai. Toda saida passa por aqui e devolve TUDO ao pool — o teste
    /// prova que depois do fim (golpe vencido, zona acabada, fim de partida) nao sobra nada desenhado.
    /// </summary>
    public static class CicloDaSintonia
    {
        /// <summary>s de o que ficou esmaecer depois de acabar (em vez de sumir de estalo).</summary>
        public const float SumirS = 0.45f;

        /// <summary>Desconta `dt`; o que venceu devolve onda E forma e sai da lista. Devolve quantos sairam.</summary>
        public static int Vencer<T>(List<GolpeDaSintonia<T>> golpes, float dt, PoolVfx<T> pool) where T : class
        {
            int n = 0;
            for (int i = golpes.Count - 1; i >= 0; i--)
            {
                GolpeDaSintonia<T> g = golpes[i];
                g.Resta -= dt;
                if (g.Resta > 0f) continue;
                pool.Devolver(g.TipoOnda, g.Onda);
                pool.Devolver(g.TipoForma, g.Forma);
                golpes.RemoveAt(i);
                n++;
            }
            return n;
        }

        /// <summary>
        /// O que ficou (SintoniaEfeitos.Ativos) x o que esta' desenhado. Novo vivo pega item do pool (tipo = combo). O que
        /// saiu — acabou, foi desfeito pelo counter, QUEBROU, ou a lista foi zerada no fim da partida (a Persistente nem sabe:
        /// o Restante dela continua > 0) — vira um golpe "sumindo" de SumirS com o proprio item (quebrou: 0 s, o estilhaco e'
        /// da casca). `mortos` fica com quem saiu neste quadro.
        /// </summary>
        public static void Sincronizar<T>(IReadOnlyList<SintoniaEfeitos.Persistente> ativos, Dictionary<SintoniaEfeitos.Persistente, T> vivos,
            List<SintoniaEfeitos.Persistente> mortos, List<GolpeDaSintonia<T>> golpes, PoolVfx<T> pool) where T : class
        {
            for (int i = 0; i < ativos.Count; i++)
            {
                SintoniaEfeitos.Persistente p = ativos[i];
                if (p.Restante > 0f && !vivos.ContainsKey(p)) vivos[p] = pool.Pegar((int)p.Combo);
            }
            mortos.Clear();
            foreach (KeyValuePair<SintoniaEfeitos.Persistente, T> kv in vivos)
                if (!(kv.Key.Restante > 0f) || !Contem(ativos, kv.Key)) mortos.Add(kv.Key);
            for (int i = 0; i < mortos.Count; i++)
            {
                SintoniaEfeitos.Persistente p = mortos[i];
                float dur = p.Quebrou ? 0f : SumirS;
                golpes.Add(new GolpeDaSintonia<T> { Combo = p.Combo, Pos = p.Pos, Raio = p.Raio, Dur = dur, Resta = dur, Forma = vivos[p], TipoForma = (int)p.Combo, Fonte = p });
                vivos.Remove(p);
            }
        }

        static bool Contem(IReadOnlyList<SintoniaEfeitos.Persistente> l, SintoniaEfeitos.Persistente p)
        {
            for (int i = 0; i < l.Count; i++) if (l[i] == p) return true;
            return false;
        }
    }

    // ============================================================================================================ A CASCA

    /// <summary>
    /// A SINTONIA NA TELA (GDD §9/§10) — so' LEITURA, nada aqui decide jogo: chega tudo pelo Bus e pela lista
    /// SintoniaEfeitos.Ativos. Acabamento da onda 18A (a folha 53 lia prototipo: disco chapado cortando a ladeira, esfera lisa,
    /// arco branco solto):
    /// CHAO — toda area e' malha DEITADA NO RELEVO (ZonaNoRelevo) com o shader Arkana/SintoniaZona: transparente, borda roida
    /// que esmaece, ruido rolando, brilho onde a fantasia pede. Nada de cupula: vapor, areia e nuvem sao BAFORADAS macias
    /// (billboard) nascidas no chao de cada ponto, que deixam ver o jogo.
    /// CANALIZACAO — fio de luz entre os dois nas DUAS cores + aviso no chao cuja borda pulsa e cujo miolo ENCHE; o "falhou"
    /// racha o anel em quatro e solta cacos.
    /// DISPARO, a forma de cada um: FUNIL de fogo girando (tornado) · METEOROS com rastro caindo numa POCA de crosta com veio
    /// incandescente (magma) · CLARAO branco-violeta + ONDA DE CHOQUE + faiscas rasantes (plasma) · NUVEM escaldante branca-
    /// alaranjada (vapor) · ESTRELA de raios rente ao chao + arcos que saltam sobre a agua azul (eletrocussao) · LAMA brilhante
    /// com ondas e bolhas (lamacal) · COLUNA de chuva sob tampa escura, poca com respingos (torrencial) · PAREDE de areia
    /// girando (areia) · CACHO de cristal da Meshy carregado de raio (minas) · NUVEM escura volumosa, raio no alvo e sombra
    /// (nuvem).
    /// Leve (Adreno 650): pool por tipo (PoolVfx), estouros em sistemas de MUNDO compartilhados (Emit com EmitParams: zero
    /// Instantiate), nada alocado por quadro, cor por MaterialPropertyBlock, teto de particula em todo sistema, zero luz
    /// dinamica. O chao reamostra so' quando o centro anda (memoria dos nos da malha). Passos de 12 fps (look de anime).
    /// O shader da zona e' ALU pura (zero textura, uma passada); o risco de GPU sao as BAFORADAS (overdraw alfa).
    /// ponytail: tetos de baforada (vapor 34, parede 50, nuvem 22) postos por olho, sem medida — o Poco F4 e' limitado por
    /// GPU (17-20 FPS, bancada 2d8510f). Subir um teto so' depois de a Sintonia entrar na bancada de cortes.
    /// </summary>
    public sealed class VisualDaSintonia : MonoBehaviour
    {
        /// <summary>O material-ASSET do chao dos combos (Resources): e' ele que leva o shader Arkana/SintoniaZona ao APK — shader
        /// pedido so' por codigo sai no "Strip Unused".</summary>
        public const string MaterialZona = "ArkanaSintoniaZona";
        public const string ShaderZona = "Arkana/SintoniaZona";
        /// <summary>O que a casca escreve no shader (o teste confere contra o asset: nome errado = zona que nao muda).</summary>
        public static readonly string[] PropsDaZona = { "_Cor", "_Brilho", "_Escala", "_Rolagem", "_Veio", "_Anel", "_Onda", "_Enche", "_Periodo" };

        /// <summary>m acima dos pes onde o fio sai do conjurador (o peito: a mao que conjurou).</summary>
        const float AlturaFio = 1.2f;
        /// <summary>m acima do ponto onde o fio dobra (o no da fusao se le' por cima da cabeca do alvo).</summary>
        const float AlturaNo = 2.2f;
        /// <summary>s do anel rachando no "falhou"; s que a canalizacao espera pelo desfecho depois de cheia.</summary>
        const float RachaS = 0.6f, EsperaS = 1f;
        /// <summary>m: a nuvem sobre o alvo, a tampa da chuva, de onde caem os meteoros.</summary>
        const float AlturaNuvem = 7f, AlturaTampa = 10f, AlturaMeteoro = 18f;
        /// <summary>s da onda de choque (o anel abre do centro ate' a borda da area).</summary>
        const float OndaS = 0.9f;
        /// <summary>m que o centro anda antes de reamostrar o chao (tornado, sombra da nuvem).</summary>
        const float ReamostraM = 0.35f;
        /// <summary>m de altura do cacho de cristal da mina.</summary>
        const float AlturaCristal = 1.8f;
        const float PassoAnime = 1f / 12f;
        static readonly float[] Flicker = { 1f, 0.8f, 0.95f, 0.75f, 0.9f };

        // pool: 0..9 = o que FICA de cada combo; 10+c = o chao do golpe (onda); 20+c = a forma do golpe; 30 = canalizacao
        const int ONDA = 10, FORMA = 20, CANAL = 30;
        const int RaiosEletro = 7, ArcosEletro = 3, PontosRaio = 7, PontosArco = 9;
        /// <summary>Teto de meteoros em voo esperando o estouro no chao.</summary>
        const int MaxImpactos = 48;

        static bool _ids;
        static int _idCor, _idBrilho, _idEscala, _idRolagem, _idVeio, _idAnel, _idOnda, _idEnche, _idPeriodo, _idBase, _idEmissao;
        static Material _mZona, _mPoeira, _mNuvem, _mFaceta, _mCristal;
        static bool _zonaPropria, _semGlb;
        static Mesh _funil, _cristalProc, _cristalGlb;
        static Bounds _caixaCristal;
        static Texture2D _texNuvem;
        static ChaoDesenhadoMemo _memo;
        static readonly System.Func<float, float, float> _chao = Chao;
        static readonly Dictionary<int, int[]> _tris = new Dictionary<int, int[]>();

        MaterialPropertyBlock _mpb;
        PoolVfx<Item> _pool;
        ParticleSystem _brasas, _faiscas, _poeira, _clarao, _meteoros;
        readonly List<Canal> _canais = new List<Canal>();
        readonly List<GolpeDaSintonia<Item>> _golpes = new List<GolpeDaSintonia<Item>>();
        readonly Dictionary<SintoniaEfeitos.Persistente, Item> _vivos = new Dictionary<SintoniaEfeitos.Persistente, Item>();
        readonly List<SintoniaEfeitos.Persistente> _mortos = new List<SintoniaEfeitos.Persistente>();
        readonly List<Zona> _zonas = new List<Zona>();
        readonly Impacto[] _impactos = new Impacto[MaxImpactos];
        int _nImpactos;
        uint _sorte = 2166136261u;

        struct Impacto { public Vector3 Pos; public float Em; }

        sealed class Item
        {
            public int Tipo;
            public GameObject Go;
            public Zona Chao;
            public Renderer R, R2;
            public Transform Pivo;
            public ParticleSystem Ps, Ps2, Ps3;
            public LineRenderer Linha;
            public LineRenderer[] Raios;
            public Vector3[] Pontos, Pontos2;
            public float Acc, Acc2, Aux;
        }

        /// <summary>A malha deitada de um item: arrays proprios (zero lixo por quadro); reamostra so' quando o centro anda.</summary>
        sealed class Zona
        {
            public MeshRenderer R;
            public Mesh M;
            public Vector3[] V;
            public Vector2[] Uv;
            public Color32[] C;
            public int Aneis, Gomos;
            public float Raio = -1f, Cx = float.NaN, Cz;
        }

        sealed class Canal
        {
            public ComboSintonia Combo;
            public IEntidade A, B;
            public Vector3 Ponto;
            public float Raio, Dur, Resta, Espera, Racha;
            public bool Falhou;
            public Color CorA, CorB;
            public Item It;
        }

        /// <summary>O visual de uma zona no shader (base sRGB; Brilho LINEAR e HDR — vai por SetVector, sem conversao de gama).</summary>
        struct Look
        {
            public Color Cor, Brilho;
            public float Escala, Periodo;
            public Vector4 Rolagem, Veio, Anel, Onda, Enche;
        }

        /// <summary>Nasce sob a arena (morre com ela). `partida` fica por simetria com o VisualDoImpacto: tudo chega pelo Bus e
        /// pela lista do SintoniaEfeitos.</summary>
        public static VisualDaSintonia Criar(Transform arena, Partida p)
        {
            var go = new GameObject("VisualDaSintonia");
            if (arena != null) go.transform.SetParent(arena, false);
            return go.AddComponent<VisualDaSintonia>();
        }

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            Ids();
            Materiais();
            _pool = new PoolVfx<Item>(Novo, Mostrar);
            // os ESTOUROS: sistemas de MUNDO parados (taxa 0) servem os 10 combos; cor, tamanho e velocidade no EmitParams
            _brasas = Estouro("SintoniaBrasas", new Vector2(0.6f, 1.2f), new Vector2(3f, 8f), new Vector2(0.2f, 0.5f), 0.5f, 260, null);
            Gravidade(_brasas, -0.3f);
            _faiscas = Estouro("SintoniaFaiscas", new Vector2(0.2f, 0.5f), new Vector2(8f, 18f), new Vector2(0.06f, 0.16f), 1f, 420, null);
            Esticar(_faiscas, 0.045f);
            Gravidade(_faiscas, 0.6f);
            _poeira = Estouro("SintoniaPoeira", new Vector2(0.9f, 1.8f), new Vector2(2f, 6f), new Vector2(1.2f, 2.6f), 0.6f, 220, _mPoeira);
            Crescer(_poeira, 0.6f, 1.6f);
            _clarao = Estouro("SintoniaClarao", new Vector2(0.2f, 0.3f), Vector2.zero, Vector2.one, 0f, 16, null);
            Crescer(_clarao, 0.7f, 1.4f);
            _meteoros = Estouro("SintoniaMeteoros", new Vector2(0.6f, 0.8f), Vector2.zero, new Vector2(0.9f, 1.4f), 0f, 64, null);
            Esticar(_meteoros, 0.07f);
            Rastro(_meteoros);
            _brasas.Play(); _faiscas.Play(); _poeira.Play(); _clarao.Play(); _meteoros.Play();
        }

        void OnEnable() { Ligar(); }
        void OnDisable() { Desligar(); }

        void OnDestroy()
        {
            Desligar();
            for (int i = 0; i < _zonas.Count; i++) if (_zonas[i].M != null) Destroy(_zonas[i].M);
            _canais.Clear(); _golpes.Clear(); _vivos.Clear(); _zonas.Clear();
        }

        void Ligar()
        {
            Desligar();   // idempotente
            Bus.SintoniaCanalizando += AoCanalizar;
            Bus.SintoniaDisparou += AoDisparar;
            Bus.SintoniaFalhou += AoFalhar;
        }

        void Desligar()
        {
            Bus.SintoniaCanalizando -= AoCanalizar;
            Bus.SintoniaDisparou -= AoDisparar;
            Bus.SintoniaFalhou -= AoFalhar;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            int passo = Passo();
            Canais(dt, passo);
            CicloDaSintonia.Sincronizar(SintoniaEfeitos.Ativos, _vivos, _mortos, _golpes, _pool);
            for (int i = 0; i < _mortos.Count; i++) if (_mortos[i].Quebrou) Estilhacar(_mortos[i]);
            foreach (KeyValuePair<SintoniaEfeitos.Persistente, Item> kv in _vivos)
                Desenhar(kv.Key, kv.Value, passo, LeituraDosKits.EscalaPorRestante(kv.Key.Restante, kv.Key.Duracao), dt);
            CicloDaSintonia.Vencer(_golpes, dt, _pool);
            Golpes(dt, passo);
            Impactos();
        }

        // ------------------------------------------------------------------ canalizacao

        void AoCanalizar(ComboSintonia c, IEntidade a, IEntidade b, Vector3 ponto, float duracao)
        {
            Elemento x, y;
            SintoniaEfeitos.Par(c, out x, out y);
            // ponytail: o evento nao diz o elemento de cada um — A leva a cor do 1o do par. As DUAS cores sempre aparecem.
            var k = new Canal
            {
                Combo = c, A = a, B = b, Ponto = ponto, Raio = Balance.Sintonia.Raio(c), Dur = Mathf.Max(duracao, 0.01f),
                Resta = Mathf.Max(duracao, 0.01f), CorA = Projetil.Tint(x), CorB = Projetil.Tint(y), It = _pool.Pegar(CANAL),
            };
            ParticleSystem.MainModule m = k.It.Ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(k.CorA, k.CorB);
            _canais.Add(k);
        }

        void AoFalhar(ComboSintonia c, IEntidade a, IEntidade b, Vector3 ponto)
        {
            Canal k = AcharCanal(a, b);
            if (k == null) return;
            k.Falhou = true;
            k.Racha = RachaS;
            k.It.Linha.enabled = false;
            k.It.Ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            // os CACOS: pedrisco cinza saltando do anel que racha
            Anelada(_faiscas, k.Ponto, k.Raio * 0.95f, 26, new Color(0.72f, 0.7f, 0.78f), 2.5f, 3f);
        }

        Canal AcharCanal(IEntidade a, IEntidade b)
        {
            for (int i = 0; i < _canais.Count; i++)
            {
                Canal k = _canais[i];
                if (!k.Falhou && ((k.A == a && k.B == b) || (k.A == b && k.B == a))) return k;
            }
            return null;
        }

        void Canais(float dt, int passo)
        {
            float f = Flicker[passo % Flicker.Length];
            for (int i = _canais.Count - 1; i >= 0; i--)
            {
                Canal k = _canais[i];
                Item it = k.It;
                Vector3 chao = NoChao(k.Ponto);
                it.Go.transform.SetPositionAndRotation(chao + Vector3.up * 0.12f, Quaternion.identity);
                Deitar(it.Chao, k.Ponto, k.Raio);
                Look z = Aviso(k.CorA, k.CorB);
                if (k.Falhou)
                {
                    k.Racha -= dt;
                    if (k.Racha <= 0f) { Devolver(it); _canais.RemoveAt(i); continue; }
                    // o anel RACHA em quatro (as fendas abrem), acinzenta, abre um pouco e apaga — deitado no relevo
                    float u = 1f - k.Racha / RachaS;
                    z.Cor.a = 0.3f * (1f - u);
                    z.Brilho = Color.Lerp(z.Brilho, new Color(0.45f, 0.43f, 0.5f), u);
                    z.Enche.x = 1f - k.Resta / k.Dur;   // o miolo para onde a canalizacao parou (o Resta congela no falhou)
                    z.Anel = new Vector4(0.94f + 0.06f * u, 0.035f, 1.3f * (1f - u) * f, 0.3f + 0.7f * u);
                    PintarZona(it.Chao.R, z, 1f);
                    continue;
                }
                k.Resta = Mathf.Max(k.Resta - dt, 0f);
                if (k.Resta <= 0f && (k.Espera += dt) > EsperaS) { Devolver(it); _canais.RemoveAt(i); continue; }
                float prog = 1f - k.Resta / k.Dur;
                it.Pontos[0] = PosDe(k.A, k.Ponto) + Vector3.up * AlturaFio;
                it.Pontos[1] = chao + Vector3.up * AlturaNo;
                it.Pontos[2] = PosDe(k.B, k.Ponto) + Vector3.up * AlturaFio;
                it.Linha.SetPositions(it.Pontos);
                it.Linha.widthMultiplier = 0.16f * f * (1f + prog);   // engrossa ate' fundir
                it.Linha.startColor = k.CorA;   // cor de vertice (32 bits): o brilho HDR e' do material DeLinha
                it.Linha.endColor = k.CorB;
                // o AVISO no chao: borda que PULSA (le-se de canto de olho) e miolo que ENCHE ate' a fusao
                z.Enche.x = prog;
                z.Anel = new Vector4(0.94f, 0.035f, (passo & 1) == 0 ? 1.4f : 0.85f, 0f);
                PintarZona(it.Chao.R, z, 1f);
                ParticleSystem.ShapeModule sh = it.Ps.shape;
                sh.radius = k.Raio;
                Taxa(it.Ps, 30f + 90f * prog);
            }
        }

        // ------------------------------------------------------------------ disparo

        void AoDisparar(DisparoSintonia d)
        {
            Canal k = AcharCanal(d.A, d.B);
            if (k != null) { Devolver(k.It); _canais.Remove(k); }
            ComboSintonia c = d.Combo;
            float r = Balance.Sintonia.Raio(c);
            Vector3 p = NoChao(d.Ponto);
            // CHAO proprio do golpe: so' quem nao deixa zona (a zona dos outros desenha a onda de choque nela mesma)
            bool onda = c == ComboSintonia.ExplosaoDePlasma || c == ComboSintonia.Eletrocussao || c == ComboSintonia.TempestadeTorrencial
                || c == ComboSintonia.CristaisCarregados || c == ComboSintonia.NuvemTempestuosa;
            bool forma = c == ComboSintonia.Eletrocussao || c == ComboSintonia.TempestadeTorrencial;
            if (onda || forma || c == ComboSintonia.ChuvaDeMagma)
            {
                var g = new GolpeDaSintonia<Item> { Combo = c, Pos = p, Raio = r, Dur = DuracaoDoGolpe(c) };
                g.Resta = g.Dur;
                if (onda) { g.TipoOnda = ONDA + (int)c; g.Onda = _pool.Pegar(g.TipoOnda); }
                if (forma) { g.TipoForma = FORMA + (int)c; g.Forma = _pool.Pegar(g.TipoForma); }
                _golpes.Add(g);
            }
            Color cc = CorDoCombo(c);
            Vector3 cima = Vector3.up;
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante:
                    Clarao(p + cima * 1.5f, 5f, new Color(1f, 0.6f, 0.2f), 0.25f);
                    Emitir(_brasas, p, 60, new Color(1f, 0.7f, 0.25f), r * 0.3f);
                    Anelada(_poeira, p, 1.5f, 16, new Color(0.62f, 0.55f, 0.45f, 0.7f), 6f, 1f);
                    break;
                case ComboSintonia.ChuvaDeMagma:
                    Clarao(p + cima * 2f, 6f, new Color(1f, 0.45f, 0.1f), 0.3f);
                    Emitir(_brasas, p, 40, new Color(1f, 0.45f, 0.1f), r * 0.5f);
                    break;
                case ComboSintonia.ExplosaoDePlasma:
                    // CLARAO branco-violeta (dois: o halo e o miolo) + faiscas em todo sentido + as RASANTES correndo no chao
                    Clarao(p + cima * 1.3f, 13f, new Color(0.85f, 0.7f, 1f), 0.26f);
                    Clarao(p + cima * 1.3f, 5.5f, Color.white, 0.34f);
                    Emitir(_faiscas, p + cima * 1.2f, 70, new Color(0.8f, 0.55f, 1f), 0.5f);
                    Emitir(_faiscas, p + cima, 50, Color.white, 0.4f);
                    Anelada(_faiscas, p, 0.6f, 32, new Color(0.9f, 0.78f, 1f), 16f, 3f);
                    break;
                case ComboSintonia.CortinaDeVapor:
                    Clarao(p + cima * 1.5f, 7f, new Color(1f, 0.85f, 0.7f), 0.3f);
                    Emitir(_poeira, p, 40, new Color(0.97f, 0.95f, 0.92f, 0.6f), r * 0.5f);
                    break;
                case ComboSintonia.Eletrocussao:
                    Clarao(p + cima, 7f, new Color(0.6f, 0.85f, 1f), 0.22f);
                    Emitir(_faiscas, p, 70, cc, r * 0.3f);
                    break;
                case ComboSintonia.Lamacal:
                    Emitir(_poeira, p, 36, new Color(0.42f, 0.31f, 0.2f, 0.9f), r * 0.5f);
                    Anelada(_poeira, p, 1f, 14, new Color(0.36f, 0.26f, 0.16f, 0.85f), 5f, 2f);
                    break;
                case ComboSintonia.TempestadeTorrencial:
                    Emitir(_poeira, p, 40, new Color(0.6f, 0.82f, 1f, 0.5f), r * 0.7f);
                    Anelada(_faiscas, p, r * 0.3f, 28, new Color(0.75f, 0.9f, 1f), 10f, 2f);
                    break;
                case ComboSintonia.TempestadeDeAreia:
                    Emitir(_poeira, p, 60, new Color(0.82f, 0.68f, 0.45f, 0.7f), r * 0.7f);
                    break;
                case ComboSintonia.CristaisCarregados:
                    Clarao(p + cima, 5f, new Color(1f, 0.9f, 0.4f), 0.22f);
                    Emitir(_faiscas, p, 50, cc, r * 0.4f);
                    break;
                case ComboSintonia.NuvemTempestuosa:
                    Emitir(_faiscas, p + cima, 40, cc, 0.5f);
                    break;
            }
        }

        /// <summary>s do golpe na tela (game feel, nao regra): plasma e eletrocussao duram o que a regra dura (A15); a chuva, o
        /// tempo de a tampa escurecer e escorrer; o magma, a chuva de meteoros; o resto, a onda de choque.</summary>
        static float DuracaoDoGolpe(ComboSintonia c)
        {
            switch (c)
            {
                case ComboSintonia.ExplosaoDePlasma: return Mathf.Max(Balance.Sintonia.Duracao(c), 0.7f);
                case ComboSintonia.Eletrocussao: return Mathf.Max(Balance.Sintonia.Duracao(c), 0.9f);
                case ComboSintonia.TempestadeTorrencial: return 2.5f;
                case ComboSintonia.ChuvaDeMagma: return 1.3f;
            }
            return OndaS;
        }

        void Golpes(float dt, int passo)
        {
            float f = Flicker[passo % Flicker.Length];
            for (int i = 0; i < _golpes.Count; i++)
            {
                GolpeDaSintonia<Item> g = _golpes[i];
                if (g.Fonte != null)
                {
                    // o que FICOU e acabou: o mesmo desenho, alfa descendo do piso ate' zero
                    if (g.Forma != null && g.Dur > 0f) Desenhar(g.Fonte, g.Forma, passo, LeituraDosKits.Piso * (g.Resta / g.Dur), dt);
                    continue;
                }
                float u = g.U;
                if (g.Onda != null) PintarOnda(g, u, f);
                switch (g.Combo)
                {
                    case ComboSintonia.ChuvaDeMagma:
                        if (u < 0.85f) for (int n = Quantos(ref g.Acc, 26f, dt); n > 0; n--) Meteoro(g.Pos, g.Raio);
                        break;
                    case ComboSintonia.Eletrocussao: Estrela(g, u, passo, f); break;
                    case ComboSintonia.TempestadeTorrencial: Chuva(g, u, dt); break;
                }
            }
        }

        /// <summary>O chao do golpe: onda de choque para todos; para plasma, eletrocussao e chuva tambem o chao do instante.</summary>
        void PintarOnda(GolpeDaSintonia<Item> g, float u, float f)
        {
            Deitar(g.Onda.Chao, g.Pos, g.Raio);
            Look k = ZonaDe(g.Combo);
            float tempo = u * g.Dur;
            float fim = 1f - Mathf.Clamp01((u - 0.7f) / 0.3f);   // o chao do instante apaga no ultimo 30%
            switch (g.Combo)
            {
                case ComboSintonia.ExplosaoDePlasma:
                    k.Cor.a *= 1f - u;
                    k.Brilho *= (1f - u) * f;
                    Choque(ref k, tempo, 0.5f, 2.2f);
                    break;
                case ComboSintonia.Eletrocussao:
                case ComboSintonia.TempestadeTorrencial:
                    k.Cor.a *= fim;
                    k.Brilho *= fim * (g.Combo == ComboSintonia.Eletrocussao ? f : 1f);
                    Choque(ref k, tempo, 0.5f, 1.6f);
                    break;
                default:
                    // so' a ONDA DE CHOQUE, na cor do combo (cristais: o anel das minas; nuvem: o estouro no alvo)
                    k.Cor.a = 0f;
                    k.Veio = Vector4.zero;
                    k.Onda = Vector4.zero;
                    k.Brilho = Lin(CorDoCombo(g.Combo)) * 2.2f;
                    Choque(ref k, tempo, OndaS, 1.4f);
                    break;
            }
            PintarZona(g.Onda.Chao.R, k, 1f);
        }

        /// <summary>A ONDA DE CHOQUE dentro de um Look: o anel abre rapido do centro e para na borda, apagando (em `s` s).</summary>
        static void Choque(ref Look k, float tempo, float s, float forca)
        {
            if (!(tempo < s)) return;
            float u = Mathf.Clamp01(tempo / s), sai = 1f - (1f - u) * (1f - u);
            k.Anel = new Vector4(Mathf.Lerp(0.08f, 0.96f, sai), 0.045f, forca * (1f - u), 0f);
        }

        /// <summary>ELETROCUSSAO: estrela de raios RENTE ao chao (cada ponto no relevo) + arcos que SALTAM entre pontos da poca;
        /// o desenho muda a cada passo de anime.</summary>
        void Estrela(GolpeDaSintonia<Item> g, float u, int passo, float f)
        {
            Item it = g.Forma;
            float vivo = 1f - Mathf.Clamp01((u - 0.7f) / 0.3f);
            for (int j = 0; j < it.Raios.Length; j++)
            {
                LineRenderer l = it.Raios[j];
                if (j < RaiosEletro)
                {
                    float a = (j * 360f / RaiosEletro + passo * 11f) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    float alcance = g.Raio * (0.75f + 0.25f * Flicker[(passo + j) % Flicker.Length]);
                    Ziguezague(it.Pontos, g.Pos, g.Pos + dir * alcance, 1f, passo * 7 + j * 13);
                    Rente(it.Pontos, 0.3f);
                    l.SetPositions(it.Pontos);
                    l.widthMultiplier = 0.3f * f;
                }
                else
                {
                    Arco(it.Pontos2, g.Pos, g.Raio, passo * 3 + j * 17);
                    l.SetPositions(it.Pontos2);
                    l.widthMultiplier = 0.2f * f;
                }
                Color k = new Color(0.85f, 0.97f, 1f, vivo);
                l.startColor = k;
                k.a *= 0.35f;
                l.endColor = k;
            }
            if (vivo > 0.3f && (passo & 3) == 0 && it.Aux != passo)
            {
                it.Aux = passo;   // faisca nas pontas: 1 rajada por passo (nao por quadro)
                Emitir(_faiscas, it.Pontos[it.Pontos.Length - 1], 6, CorDoCombo(g.Combo), 0.3f);
            }
        }

        /// <summary>TORRENCIAL: chuva densa caindo da TAMPA escura (baforadas no alto) + respingos saltando do chao.</summary>
        void Chuva(GolpeDaSintonia<Item> g, float u, float dt)
        {
            Item it = g.Forma;
            it.Go.transform.SetPositionAndRotation(g.Pos, Quaternion.identity);
            float chove = 1f - Mathf.Clamp01((u - 0.75f) / 0.25f);
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.scale = new Vector3(g.Raio * 1.5f, g.Raio * 1.5f, 0.1f);
            Taxa(it.Ps, 460f * chove);
            if (it.Aux < 0f)
            {
                it.Aux = 1f;
                for (int n = 0; n < 14; n++)
                {
                    float a = Sorte() * Mathf.PI * 2f, d = Mathf.Sqrt(Sorte()) * g.Raio * 0.95f;
                    var q = new Vector3(Mathf.Cos(a) * d, AlturaTampa + 0.5f + (Sorte() - 0.5f) * 1.6f, Mathf.Sin(a) * d);
                    Color cor = Color.Lerp(new Color(0.22f, 0.26f, 0.34f, 0.92f), new Color(0.36f, 0.42f, 0.52f, 0.88f), Sorte());
                    Sopro(it.Ps2, q, new Vector3(0f, 0.1f, 0f), 4.5f + 2.5f * Sorte(), cor, g.Dur * (0.95f + 0.2f * Sorte()));
                }
            }
            for (int n = Quantos(ref g.Acc, 70f * chove, dt); n > 0; n--)
            {
                Vector3 q = PontoNoChao(g.Pos, g.Raio * 0.95f) + Vector3.up * 0.05f;
                var ep = new ParticleSystem.EmitParams
                {
                    position = q, applyShapeToPosition = false, startColor = new Color(0.78f, 0.92f, 1f),
                    velocity = new Vector3((Sorte() - 0.5f) * 1.6f, 1.5f + 1.5f * Sorte(), (Sorte() - 0.5f) * 1.6f),
                    startSize = 0.05f + 0.04f * Sorte(), startLifetime = 0.25f + 0.15f * Sorte(),
                };
                _faiscas.Emit(ep, 1);
            }
        }

        /// <summary>Um METEORO: chega num ponto do disco (sqrt = uniforme na area) vindo do alto e de lado, com vida = o tempo de
        /// voo — morre no chao, e o estouro daquele ponto fica agendado.</summary>
        void Meteoro(Vector3 centro, float r)
        {
            float a = Sorte() * Mathf.PI * 2f, d = Mathf.Sqrt(Sorte()) * r * 0.9f;
            float x = centro.x + Mathf.Cos(a) * d, z = centro.z + Mathf.Sin(a) * d;
            var alvo = new Vector3(x, Chao(x, z), z);
            Vector3 dir = new Vector3(0.35f, -1f, 0.2f).normalized;
            const float vel = 28f;
            float voo = AlturaMeteoro / -dir.y;
            var ep = new ParticleSystem.EmitParams
            {
                position = alvo - dir * voo, velocity = dir * vel, startLifetime = voo / vel, startSize = 0.9f + 0.5f * Sorte(),
                startColor = Color.Lerp(new Color(1f, 0.85f, 0.35f), new Color(1f, 0.35f, 0.05f), Sorte()), applyShapeToPosition = false,
            };
            _meteoros.Emit(ep, 1);
            if (_nImpactos < MaxImpactos) _impactos[_nImpactos++] = new Impacto { Pos = alvo, Em = Time.time + voo / vel };
        }

        /// <summary>Os meteoros que chegaram: brasa, faisca e um clarao curto no ponto do chao.</summary>
        void Impactos()
        {
            float agora = Time.time;
            for (int i = _nImpactos - 1; i >= 0; i--)
            {
                if (_impactos[i].Em > agora) continue;
                Vector3 q = _impactos[i].Pos + Vector3.up * 0.2f;
                Emitir(_brasas, q, 8, new Color(1f, 0.55f, 0.15f), 0.4f);
                Emitir(_faiscas, q, 10, new Color(1f, 0.7f, 0.3f), 0.3f);
                Clarao(q + Vector3.up * 0.4f, 2.6f, new Color(1f, 0.55f, 0.2f), 0.18f);
                _impactos[i] = _impactos[--_nImpactos];
            }
        }

        // ------------------------------------------------------------------ o que fica

        void Desenhar(SintoniaEfeitos.Persistente p, Item it, int passo, float e, float dt)
        {
            float f = Flicker[passo % Flicker.Length];
            float idade = p.Duracao - p.Restante;
            Vector3 chao = NoChao(p.Pos);
            Transform t = it.Go.transform;
            float r = p.Raio;
            Look k = ZonaDe(p.Combo);
            switch (p.Combo)
            {
                case ComboSintonia.TornadoFlamejante:
                {
                    // chao CHAMUSCADO que anda com ele + a borda da SUCCAO pulsando + FUNIL de fogo (casca interna de fogo e
                    // externa de fumaca girando em sentidos opostos: o eixo curvo varre e o funil "danca")
                    t.SetPositionAndRotation(chao, Quaternion.identity);
                    Deitar(it.Chao, p.Pos, r);
                    k.Anel = new Vector4(0.92f, 0.04f, (passo & 1) == 0 ? 0.9f : 0.55f, 0f);
                    Choque(ref k, idade, OndaS, 1.4f);
                    PintarZona(it.Chao.R, k, e);
                    float nasce = Mathf.Clamp01(idade / 0.5f), larg = Mathf.Lerp(0.25f, 1f, nasce) * Mathf.Lerp(0.4f, 1f, e);
                    it.R.transform.localRotation = Quaternion.Euler(0f, passo * 27f, 0f);
                    it.R.transform.localScale = new Vector3(larg, Mathf.Lerp(0.3f, 1f, nasce), larg);
                    it.R2.transform.localRotation = Quaternion.Euler(0f, -passo * 17f, 0f);
                    it.R2.transform.localScale = new Vector3(larg * 1.35f, Mathf.Lerp(0.3f, 1.08f, nasce), larg * 1.35f);
                    Look fogo = Funil(true);
                    fogo.Brilho *= Mathf.Lerp(1f, f, 0.5f);
                    PintarZona(it.R, fogo, e);
                    PintarZona(it.R2, Funil(false), e);
                    Taxa(it.Ps, 110f * e);
                    Taxa(it.Ps2, 45f * e);
                    Taxa(it.Ps3, 70f * e);
                    break;
                }
                case ComboSintonia.ChuvaDeMagma:
                    // POCA de lava: crosta escura rachada com veio incandescente + brasas e fumaca nascendo de pontos do chao
                    t.SetPositionAndRotation(chao, Quaternion.identity);
                    Deitar(it.Chao, p.Pos, r);
                    k.Brilho *= Mathf.Lerp(1f, f, 0.35f);
                    Choque(ref k, idade, OndaS, 1.2f);
                    PintarZona(it.Chao.R, k, e);
                    for (int n = Quantos(ref it.Acc, r * r * 0.8f * e, dt); n > 0; n--)
                        Sopro(it.Ps, PontoNoChao(p.Pos, r * 0.9f) + Vector3.up * 0.1f,
                            new Vector3((Sorte() - 0.5f) * 0.6f, 1.5f + 2f * Sorte(), (Sorte() - 0.5f) * 0.6f), 0.12f + 0.2f * Sorte(),
                            Color.Lerp(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.4f, 0.08f), Sorte()), 0.8f + 0.8f * Sorte());
                    for (int n = Quantos(ref it.Acc2, 2.5f * e, dt); n > 0; n--)
                        Sopro(it.Ps2, PontoNoChao(p.Pos, r * 0.8f) + Vector3.up * 0.8f, new Vector3(0f, 0.8f + 0.6f * Sorte(), 0f),
                            2.5f + 1.5f * Sorte(), new Color(0.17f, 0.13f, 0.11f, 0.5f), 2.5f + Sorte());
                    break;
                case ComboSintonia.CortinaDeVapor:
                    // a NUVEM ESCALDANTE: baforadas grandes nascendo no chao de CADA ponto (segue a ladeira), subindo devagar;
                    // o chao embaixo e' vapor rente com o calor alaranjado. Translucida: le-se "area perigosa" e ve-se o jogo.
                    t.SetPositionAndRotation(chao, Quaternion.identity);
                    Deitar(it.Chao, p.Pos, r);
                    Choque(ref k, idade, OndaS, 2.2f);
                    PintarZona(it.Chao.R, k, e);
                    for (int n = it.Aux < 0f ? 20 : Quantos(ref it.Acc, 8f * e, dt); n > 0; n--)
                    {
                        Vector3 q = PontoNoChao(p.Pos, r * 0.92f);
                        q.y += 0.4f + 1.8f * Sorte();
                        Color cor = Color.Lerp(new Color(1f, 0.98f, 0.96f, 0.46f), new Color(1f, 0.76f, 0.55f, 0.42f), Sorte());
                        Sopro(it.Ps, q, new Vector3(0f, 0.25f + 0.35f * Sorte(), 0f), 3f + 2.2f * Sorte(), cor, 3.5f + 2f * Sorte());
                    }
                    it.Aux = 1f;
                    break;
                case ComboSintonia.Lamacal:
                    // LAMA escura e brilhante: ondas que correm para fora e bolhas estourando (shader) + borrifo de lama saltando
                    t.SetPositionAndRotation(chao, Quaternion.identity);
                    Deitar(it.Chao, p.Pos, r);
                    Choque(ref k, idade, OndaS, 5f);
                    PintarZona(it.Chao.R, k, e);
                    for (int n = Quantos(ref it.Acc, 7f * e, dt); n > 0; n--)
                        Sopro(it.Ps, PontoNoChao(p.Pos, r * 0.85f) + Vector3.up * 0.1f,
                            new Vector3((Sorte() - 0.5f) * 0.8f, 1.6f + 1.2f * Sorte(), (Sorte() - 0.5f) * 0.8f), 0.22f + 0.23f * Sorte(),
                            new Color(0.33f, 0.23f, 0.13f, 0.95f), 0.7f + 0.3f * Sorte());
                    break;
                case ComboSintonia.TempestadeDeAreia:
                {
                    // PAREDE de areia girando no raio (baforadas ocre, cada uma no chao do seu ponto) + graos riscando em volta
                    t.SetPositionAndRotation(chao, Quaternion.identity);
                    Deitar(it.Chao, p.Pos, r);
                    Choque(ref k, idade, OndaS, 5f);
                    PintarZona(it.Chao.R, k, e);
                    for (int n = it.Aux < 0f ? 28 : Quantos(ref it.Acc, 14f * e, dt); n > 0; n--)
                    {
                        Vector3 q = NoAnel(p.Pos, r * (0.82f + 0.25f * Sorte()), 0.3f + 2.6f * Sorte()) - chao;
                        Color cor = Color.Lerp(new Color(0.86f, 0.7f, 0.46f, 0.55f), new Color(0.7f, 0.53f, 0.32f, 0.5f), Sorte());
                        Sopro(it.Ps, q, new Vector3(0f, 0.35f, 0f), 2f + 1.4f * Sorte(), cor, 2.6f + 1.2f * Sorte());
                    }
                    it.Aux = 1f;
                    for (int n = Quantos(ref it.Acc2, 45f * e, dt); n > 0; n--)
                    {
                        Vector3 q = NoAnel(p.Pos, r * (0.5f + 0.6f * Sorte()), 0.2f + 3.2f * Sorte()) - chao;
                        Sopro(it.Ps2, q, new Vector3(0f, 0.2f, 0f), 0.12f + 0.1f * Sorte(), new Color(0.55f, 0.42f, 0.25f, 0.9f), 1.2f + 0.6f * Sorte());
                    }
                    break;
                }
                case ComboSintonia.CristaisCarregados:
                {
                    // o CACHO de cristal brota do chao (mola), inclinado para um lado, com a carga de raio estalando na emissao
                    t.SetPositionAndRotation(chao, Quaternion.identity);
                    Deitar(it.Chao, p.Pos, 1.9f);
                    int h = Mathf.Abs(Mathf.RoundToInt(p.Pos.x * 7f + p.Pos.z * 13f));
                    float fl = Flicker[(passo + h) % Flicker.Length];
                    k.Brilho *= fl;
                    k.Anel = new Vector4(0.85f, 0.07f, 0.6f + 0.4f * fl, 0f);
                    PintarZona(it.Chao.R, k, e);
                    float cresce = idade < 0.35f ? Mola(idade / 0.35f) : 1f;
                    float esc = cresce * Mathf.Lerp(0.5f, 1f, e) * (_cristalGlb != null ? AlturaCristal / Mathf.Max(_caixaCristal.size.y, 0.01f) : 1f);
                    it.Pivo.localRotation = Quaternion.Euler(6f + h % 10, h % 360, (h / 7) % 12 - 6f);
                    it.Pivo.localScale = Vector3.one * esc;
                    PintarCristal(it.R, new Color(1.6f, 1.2f, 0.2f) * (0.5f + 0.7f * fl));
                    Taxa(it.Ps, 12f * e);
                    break;
                }
                case ComboSintonia.NuvemTempestuosa:
                {
                    // NUVEM escura volumosa sobre o alvo (anda com ele) + SOMBRA no chao + o RAIO logo apos cada descarga, com
                    // clarao dentro da nuvem e estalo no alvo
                    Vector3 alto = new Vector3(p.Pos.x, Mathf.Max(p.Pos.y, chao.y) + AlturaNuvem, p.Pos.z);
                    t.SetPositionAndRotation(alto, Quaternion.identity);
                    if (it.Aux < 0f) { for (int n = 0; n < 14; n++) BaforadaDaNuvem(it.Ps); it.Aux = float.MaxValue; }
                    for (int n = Quantos(ref it.Acc, 5f * e, dt); n > 0; n--) BaforadaDaNuvem(it.Ps);
                    Deitar(it.Chao, p.Pos, SintoniaEfeitos.NuvemAlcanceM * 1.3f);
                    bool raio = p.DesdeADescarga < 0.25f;
                    if (raio)
                    {
                        k.Anel = new Vector4(Mathf.Lerp(0.1f, 0.95f, p.DesdeADescarga / 0.25f), 0.08f, 1.4f, 0f);
                        k.Veio.x = 0.9f;
                    }
                    PintarZona(it.Chao.R, k, e);
                    if (p.DesdeADescarga < it.Aux)
                    {
                        Clarao(alto, 8f, new Color(0.75f, 0.85f, 1f), 0.22f);
                        Clarao(p.Pos + Vector3.up, 3.5f, new Color(0.8f, 0.9f, 1f), 0.18f);
                        Emitir(_faiscas, p.Pos + Vector3.up * 0.5f, 30, CorDoCombo(p.Combo), 0.3f);
                    }
                    it.Aux = p.DesdeADescarga;
                    it.Linha.enabled = raio;
                    if (raio)
                    {
                        Ziguezague(it.Pontos, alto + Vector3.down * 0.6f, p.Pos + Vector3.up * 0.9f, 0.9f, passo * 5);
                        it.Linha.SetPositions(it.Pontos);
                        it.Linha.widthMultiplier = 0.45f * f;
                        it.Linha.startColor = Color.white;
                        it.Linha.endColor = CorDoCombo(p.Combo);
                    }
                    break;
                }
            }
        }

        /// <summary>Uma baforada da nuvem: elipsoide achatado (3,4 x 1,1), local (anda com o alvo); o miolo mais alto.</summary>
        void BaforadaDaNuvem(ParticleSystem ps)
        {
            float a = Sorte() * Mathf.PI * 2f, d = Mathf.Sqrt(Sorte());
            var q = new Vector3(Mathf.Cos(a) * d * 3.4f, (1f - d) * 0.8f + (Sorte() - 0.5f) * 0.8f, Mathf.Sin(a) * d * 3.4f);
            Color cor = Color.Lerp(new Color(0.2f, 0.21f, 0.28f, 0.92f), new Color(0.36f, 0.37f, 0.46f, 0.88f), Sorte());
            Sopro(ps, q, new Vector3((Sorte() - 0.5f) * 0.4f, 0.12f, (Sorte() - 0.5f) * 0.4f), 2.8f + 1.8f * Sorte(), cor, 2.6f + 1.2f * Sorte());
        }

        /// <summary>A MINA estourou: estilhaco de cristal + faisca, no mundo (sobrevive ao item voltar ao pool).</summary>
        void Estilhacar(SintoniaEfeitos.Persistente p)
        {
            Vector3 q = NoChao(p.Pos) + Vector3.up * 0.8f;
            Emitir(_faiscas, q, 45, CorDoCombo(p.Combo), 0.4f);
            Emitir(_brasas, q, 18, new Color(1f, 0.9f, 0.45f), 0.3f);
            Clarao(q, 3.5f, new Color(1f, 0.9f, 0.45f), 0.2f);
        }

        // ------------------------------------------------------------------ o visual de cada chao

        /// <summary>O chao de cada combo no shader (a base em sRGB; o brilho LINEAR e HDR: acima de 1,1 acende no bloom).</summary>
        static Look ZonaDe(ComboSintonia c)
        {
            var k = new Look { Escala = 0.35f, Enche = new Vector4(2f, 0.45f, 0f, 0f) };
            switch (c)
            {
                case ComboSintonia.ChuvaDeMagma:           // crosta escura rachada + veio incandescente + miolo que ferve
                    k.Cor = new Color(0.13f, 0.045f, 0.025f, 0.96f);
                    k.Brilho = new Color(4.2f, 1.15f, 0.12f);
                    k.Escala = 0.3f;
                    k.Rolagem = new Vector4(0.035f, 0.012f, -0.02f, 0.028f);
                    k.Veio = new Vector4(1f, 5f, 0.9f, 0.55f);
                    k.Onda = new Vector4(0f, 0f, 0f, 0.35f);
                    break;
                case ComboSintonia.Lamacal:                // lama escura e brilhante: reflexo nas cristas, ondas e bolhas
                    k.Cor = new Color(0.2f, 0.13f, 0.07f, 0.94f);
                    k.Brilho = new Color(0.32f, 0.24f, 0.15f);
                    k.Escala = 0.42f;
                    k.Rolagem = new Vector4(0.02f, 0.01f, -0.015f, 0.02f);
                    k.Veio = new Vector4(0.8f, 3f, 0.75f, 0f);
                    k.Onda = new Vector4(0.9f, 16f, 1.6f, 1.6f);
                    break;
                case ComboSintonia.CortinaDeVapor:         // chao escaldante: vapor rente e o calor alaranjado por baixo
                    k.Cor = new Color(0.92f, 0.86f, 0.8f, 0.32f);
                    k.Brilho = new Color(1.3f, 0.45f, 0.12f);
                    k.Escala = 0.3f;
                    k.Rolagem = new Vector4(0.12f, 0.05f, -0.09f, 0.07f);
                    k.Veio = new Vector4(0.35f, 3f, 0.9f, 0.25f);
                    k.Enche.y = 0.6f;
                    break;
                case ComboSintonia.TempestadeDeAreia:      // areia varrendo o chao
                    k.Cor = new Color(0.74f, 0.58f, 0.34f, 0.42f);
                    k.Brilho = new Color(0.35f, 0.26f, 0.12f);
                    k.Escala = 0.4f;
                    k.Rolagem = new Vector4(0.9f, 0.35f, 0.6f, 0.25f);
                    k.Veio = new Vector4(0.9f, 3f, 0.9f, 0f);
                    k.Enche.y = 0.6f;
                    break;
                case ComboSintonia.TornadoFlamejante:      // chao chamuscado com brasa correndo
                    k.Cor = new Color(0.16f, 0.08f, 0.05f, 0.5f);
                    k.Brilho = new Color(3f, 0.8f, 0.1f);
                    k.Escala = 0.45f;
                    k.Rolagem = new Vector4(0.35f, -0.25f, -0.3f, 0.3f);
                    k.Veio = new Vector4(0.7f, 7f, 0.8f, 0.2f);
                    k.Enche.y = 0.55f;
                    break;
                case ComboSintonia.Eletrocussao:           // agua azul com a corrente correndo (veio fino e rapido)
                    k.Cor = new Color(0.08f, 0.32f, 0.55f, 0.6f);
                    k.Brilho = new Color(1.6f, 3.2f, 4.5f);
                    k.Escala = 0.5f;
                    k.Rolagem = new Vector4(1.1f, -0.7f, -0.8f, 0.9f);
                    k.Veio = new Vector4(1.3f, 16f, 0.6f, 0f);
                    k.Onda = new Vector4(0f, 0f, 0f, 0.6f);
                    break;
                case ComboSintonia.TempestadeTorrencial:   // poca de chuva com respingos
                    k.Cor = new Color(0.25f, 0.42f, 0.6f, 0.45f);
                    k.Brilho = new Color(0.8f, 1.1f, 1.5f);
                    k.Escala = 0.6f;
                    k.Rolagem = new Vector4(0.06f, 0.35f, -0.05f, 0.28f);
                    k.Veio = new Vector4(0.3f, 4f, 0.5f, 0f);
                    k.Onda = new Vector4(0f, 0f, 0f, 1.8f);
                    break;
                case ComboSintonia.ExplosaoDePlasma:       // chao queimado roxo-branco (so' o instante do estouro)
                    k.Cor = new Color(0.22f, 0.08f, 0.3f, 0.45f);
                    k.Brilho = new Color(2.6f, 1.6f, 4.2f);
                    k.Escala = 0.55f;
                    k.Rolagem = new Vector4(0.4f, 0.2f, -0.3f, 0.25f);
                    k.Veio = new Vector4(1f, 6f, 0.7f, 0.4f);
                    break;
                case ComboSintonia.CristaisCarregados:     // carga amarela estalando em volta da mina
                    k.Cor = new Color(0.4f, 0.33f, 0.06f, 0.3f);
                    k.Brilho = new Color(4f, 3f, 0.4f);
                    k.Escala = 1.3f;
                    k.Rolagem = new Vector4(0.9f, -0.6f, -0.7f, 0.8f);
                    k.Veio = new Vector4(1.1f, 12f, 0.5f, 0f);
                    break;
                default:                                   // NuvemTempestuosa: a SOMBRA sob a nuvem
                    k.Cor = new Color(0.03f, 0.03f, 0.07f, 0.55f);
                    k.Brilho = new Color(1.5f, 2.4f, 4f);
                    k.Escala = 0.5f;
                    k.Rolagem = new Vector4(0.2f, 0.1f, -0.15f, 0.12f);
                    k.Veio = new Vector4(0f, 8f, 0.4f, 0f);
                    k.Enche.y = 0.5f;
                    break;
            }
            return k;
        }

        /// <summary>O funil: casca de FOGO (faixas subindo em espiral) ou de FUMACA (mais larga, girando ao contrario). O UV
        /// em volta fecha no periodo 6 do ruido: sem costura.</summary>
        static Look Funil(bool fogo)
        {
            var k = new Look { Escala = 1f, Periodo = 6f, Enche = new Vector4(2f, 0.6f, 0f, 0f) };
            if (fogo)
            {
                k.Cor = new Color(0.55f, 0.14f, 0.03f, 0.6f);
                k.Brilho = new Color(4f, 1.5f, 0.25f);
                k.Rolagem = new Vector4(1.4f, -1.8f, 0.9f, -1.3f);
                k.Veio = new Vector4(1f, 3.5f, 0.9f, 0.6f);
                k.Onda = new Vector4(0.7f, 12f, 6f, 0f);
            }
            else
            {
                k.Cor = new Color(0.18f, 0.1f, 0.08f, 0.45f);
                k.Brilho = new Color(1.4f, 0.4f, 0.08f);
                k.Rolagem = new Vector4(-0.8f, -1.1f, -0.5f, -0.9f);
                k.Veio = new Vector4(0.35f, 3f, 1f, 0.2f);
            }
            return k;
        }

        /// <summary>O aviso da canalizacao: miolo na cor de B, borda e veio na cor de A (as DUAS cores sempre).</summary>
        static Look Aviso(Color a, Color b)
        {
            return new Look
            {
                Cor = new Color(b.r, b.g, b.b, 0.32f), Brilho = Lin(a) * 1.8f, Escala = 0.6f,
                Rolagem = new Vector4(0.3f, 0.2f, -0.25f, 0.3f), Veio = new Vector4(0.5f, 8f, 0.5f, 0f), Enche = new Vector4(0f, 0.1f, 0f, 0f),
            };
        }

        void PintarZona(Renderer r, Look k, float alfa)
        {
            if (r == null) return;
            k.Cor.a *= alfa;
            _mpb.Clear();
            _mpb.SetColor(_idCor, k.Cor);
            _mpb.SetVector(_idBrilho, new Vector4(k.Brilho.r * alfa, k.Brilho.g * alfa, k.Brilho.b * alfa, 1f));
            _mpb.SetFloat(_idEscala, k.Escala);
            _mpb.SetVector(_idRolagem, k.Rolagem);
            _mpb.SetVector(_idVeio, k.Veio);
            _mpb.SetVector(_idAnel, k.Anel);
            _mpb.SetVector(_idOnda, k.Onda);
            _mpb.SetVector(_idEnche, k.Enche);
            _mpb.SetFloat(_idPeriodo, k.Periodo);
            if (!_zonaPropria) _mpb.SetColor(_idBase, k.Cor);   // o Unlit de reserva: cor chapada (ainda deitada no relevo)
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>A carga do cristal: emissao LINEAR (SetVector) no Lit da Meshy; no facetado de reserva, a cor base.</summary>
        void PintarCristal(Renderer r, Color emissao)
        {
            if (r == null) return;
            _mpb.Clear();
            if (_cristalGlb != null) _mpb.SetVector(_idEmissao, new Vector4(emissao.r, emissao.g, emissao.b, 1f));
            else _mpb.SetColor(_idBase, new Color(1f, 0.85f, 0.25f) * (0.8f + 0.4f * emissao.g));
            r.SetPropertyBlock(_mpb);
        }

        // ------------------------------------------------------------------ o chao deitado

        /// <summary>Deita (ou reamostra) a malha da zona em `centro`; so' refaz o que mudou. A raiz fica em (cx, 0, cz).</summary>
        void Deitar(Zona z, Vector3 centro, float raio)
        {
            float dx = centro.x - z.Cx, dz = centro.z - z.Cz;
            bool outraMalha = Mathf.Abs(raio - z.Raio) > 0.01f;
            if (outraMalha || !(dx * dx + dz * dz < ReamostraM * ReamostraM))
            {
                int an = ZonaNoRelevo.Aneis(raio), go = ZonaNoRelevo.Gomos(raio);
                bool topo = an != z.Aneis || go != z.Gomos;
                if (topo)
                {
                    int n = ZonaNoRelevo.Vertices(an, go);
                    z.V = new Vector3[n];
                    z.Uv = new Vector2[n];
                    z.C = new Color32[n];
                    z.Aneis = an;
                    z.Gomos = go;
                    z.M.Clear();
                }
                ZonaNoRelevo.Amostrar(centro, raio, an, go, _chao, Ilha.SuperficieDaAgua(centro.x, centro.z), z.V,
                    outraMalha ? z.Uv : null, outraMalha ? z.C : null);
                z.M.SetVertices(z.V);
                if (outraMalha) { z.M.SetUVs(0, z.Uv); z.M.SetColors(z.C); }
                if (topo) z.M.SetTriangles(Tris(an, go), 0);
                z.M.RecalculateBounds();
                z.Raio = raio;
                z.Cx = centro.x;
                z.Cz = centro.z;
            }
            z.R.transform.SetPositionAndRotation(new Vector3(z.Cx, 0f, z.Cz), Quaternion.identity);
        }

        static int[] Tris(int aneis, int gomos)
        {
            int chave = aneis * 1000 + gomos;
            int[] t;
            if (!_tris.TryGetValue(chave, out t)) { t = ZonaNoRelevo.Triangulos(aneis, gomos); _tris[chave] = t; }
            return t;
        }

        /// <summary>O chao DESENHADO em (x, z) (memoria dos nos da malha); sem ilha, plano em y = 0.</summary>
        static float Chao(float x, float z)
        {
            Relevo r = Ilha.Atual != null ? Ilha.Atual.Relevo : null;
            if (r == null) return 0f;
            if (_memo == null || _memo.Relevo != r) _memo = new ChaoDesenhadoMemo(r);
            return _memo.Altura(x, z);
        }

        static Vector3 NoChao(Vector3 p) => new Vector3(p.x, Chao(p.x, p.z), p.z);

        /// <summary>Um ponto sorteado no disco (uniforme na area), no chao dele.</summary>
        Vector3 PontoNoChao(Vector3 centro, float r)
        {
            float a = Sorte() * Mathf.PI * 2f, d = Mathf.Sqrt(Sorte()) * r;
            float x = centro.x + Mathf.Cos(a) * d, z = centro.z + Mathf.Sin(a) * d;
            return new Vector3(x, Chao(x, z), z);
        }

        /// <summary>Um ponto sorteado no anel de raio `r`, `acima` m sobre o chao DELE.</summary>
        Vector3 NoAnel(Vector3 centro, float r, float acima)
        {
            float a = Sorte() * Mathf.PI * 2f;
            float x = centro.x + Mathf.Cos(a) * r, z = centro.z + Mathf.Sin(a) * r;
            return new Vector3(x, Chao(x, z) + acima, z);
        }

        /// <summary>Cada ponto da linha rente ao chao dele (o raio da eletrocussao corre PELA ladeira, nao por baixo dela).</summary>
        static void Rente(Vector3[] s, float acima)
        {
            for (int i = 0; i < s.Length; i++) s[i].y = Chao(s[i].x, s[i].z) + acima;
        }

        /// <summary>Arco que SALTA de um ponto da poca a outro 2-5 m adiante, por cima (parabola de 1,2-2,4 m), tremido.</summary>
        static void Arco(Vector3[] s, Vector3 centro, float raio, int semente)
        {
            float a0 = Hash01(semente) * Mathf.PI * 2f, d0 = Mathf.Sqrt(Hash01(semente + 1)) * raio * 0.8f;
            float a1 = Hash01(semente + 2) * Mathf.PI * 2f, d1 = 2f + 3f * Hash01(semente + 3);
            float x0 = centro.x + Mathf.Cos(a0) * d0, z0 = centro.z + Mathf.Sin(a0) * d0;
            float x1 = x0 + Mathf.Cos(a1) * d1, z1 = z0 + Mathf.Sin(a1) * d1;
            float y0 = Chao(x0, z0) + 0.2f, y1 = Chao(x1, z1) + 0.2f, alto = 1.2f + 1.2f * Hash01(semente + 4);
            int n = s.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float j = i == 0 || i == n - 1 ? 0f : (Hash01(semente * 31 + i) - 0.5f) * 0.5f;
                s[i] = new Vector3(Mathf.Lerp(x0, x1, t) + j, Mathf.Lerp(y0, y1, t) + 4f * t * (1f - t) * alto + j, Mathf.Lerp(z0, z1, t) - j);
            }
        }

        // ------------------------------------------------------------------ pool

        void Devolver(Item it)
        {
            if (it != null) _pool.Devolver(it.Tipo, it);
        }

        void Mostrar(Item it, bool on)
        {
            if (it.Chao != null)
            {
                it.Chao.R.enabled = on;
                if (on) it.Chao.Cx = float.NaN;   // reusado em outro lugar: reamostra no 1o desenho
            }
            if (it.R != null) it.R.enabled = on;
            if (it.R2 != null) it.R2.enabled = on;
            if (it.Linha != null) it.Linha.enabled = on && it.Tipo != (int)ComboSintonia.NuvemTempestuosa;   // o raio da nuvem acende so' na descarga
            if (it.Raios != null) for (int i = 0; i < it.Raios.Length; i++) it.Raios[i].enabled = on;
            Tocar(it.Ps, on);
            Tocar(it.Ps2, on);
            Tocar(it.Ps3, on);
            it.Acc = it.Acc2 = 0f;
            it.Aux = -1f;   // "acabou de nascer": o 1o desenho enche a nuvem de uma vez
        }

        static void Tocar(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            if (on) ps.Play(true);
            else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);   // o que ja' saiu morre sozinho
        }

        Item Novo(int tipo)
        {
            var it = new Item { Tipo = tipo, Go = new GameObject("Sintonia_" + tipo) };
            Transform raiz = it.Go.transform;
            raiz.SetParent(transform, false);
            if (tipo == CANAL)
            {
                it.Linha = Linha(it.Go, 0.16f);
                it.Pontos = new Vector3[3];
                it.Linha.positionCount = 3;
                it.Chao = NovaZona(raiz);
                // motas SUBINDO do anel nas duas cores (a cor vem por canal): a fusao juntando forca
                it.Ps = ParticulaVfx.Novo(raiz, "Motas", Color.white, Color.white, 30f, new Vector2(0.5f, 1f),
                    new Vector2(1.5f, 3.5f), new Vector2(0.08f, 0.2f), true, 0.1f, 90);
                Anel(it.Ps, 0f);
            }
            else if (tipo >= ONDA && tipo < ONDA + 10) it.Chao = NovaZona(raiz);
            else if (tipo == FORMA + (int)ComboSintonia.Eletrocussao)
            {
                it.Pontos = new Vector3[PontosRaio];
                it.Pontos2 = new Vector3[PontosArco];
                it.Raios = new LineRenderer[RaiosEletro + ArcosEletro];
                for (int i = 0; i < it.Raios.Length; i++)
                {
                    var go = new GameObject(i < RaiosEletro ? "Raio" : "Arco");
                    go.transform.SetParent(raiz, false);
                    it.Raios[i] = Linha(go, 0.25f);
                    it.Raios[i].positionCount = i < RaiosEletro ? PontosRaio : PontosArco;
                }
            }
            else if (tipo == FORMA + (int)ComboSintonia.TempestadeTorrencial)
            {
                // a CHUVA: riscos azuis caindo da tampa (a caixa gira para +Z apontar para baixo); vida longa o bastante para
                // chegar ao pe' da ladeira (a gota que passa do chao some no depth)
                it.Ps = ParticulaVfx.Novo(raiz, "Chuva", new Color(0.75f, 0.9f, 1f), new Color(0.45f, 0.7f, 1f), 0f,
                    new Vector2(0.5f, 0.62f), new Vector2(20f, 26f), new Vector2(0.05f, 0.09f), true, 0f, 320);
                it.Ps.transform.localPosition = Vector3.up * AlturaTampa;
                it.Ps.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Esticar(it.Ps, 0.06f);
                it.Ps2 = Manual(raiz, "Tampa", _mNuvem, 16, false);
                FadeSuave(it.Ps2, 0.1f);
                Crescer(it.Ps2, 0.8f, 1.15f);
            }
            else NovoQueFica(it, raiz, (ComboSintonia)tipo);
            return it;
        }

        void NovoQueFica(Item it, Transform raiz, ComboSintonia c)
        {
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante:
                {
                    it.Chao = NovaZona(raiz);
                    it.R = Peca(raiz, MalhaDoFunil(), _mZona);
                    it.R2 = Peca(raiz, MalhaDoFunil(), _mZona);
                    // CHAMAS subindo girando no funil (cone estreito que abre)
                    it.Ps = ParticulaVfx.Novo(raiz, "Chamas", new Color(1f, 0.9f, 0.4f), new Color(1f, 0.35f, 0.08f), 110f,
                        new Vector2(0.9f, 1.4f), new Vector2(5f, 7.5f), new Vector2(0.6f, 1.3f), false, 0f, 180);
                    ParticleSystem.ShapeModule sf = it.Ps.shape;
                    sf.shapeType = ParticleSystemShapeType.Cone;
                    sf.angle = 18f;
                    sf.radius = 0.45f;
                    Girar(it.Ps, 5f);
                    Crescer(it.Ps, 0.5f, 1.8f);
                    // POEIRA varrida no pe'
                    it.Ps2 = ParticulaVfx.Novo(raiz, "Redemoinho", new Color(0.75f, 0.9f, 0.8f, 0.6f), new Color(0.6f, 0.52f, 0.42f, 0.5f), 45f,
                        new Vector2(0.6f, 1f), new Vector2(0.3f, 1f), new Vector2(0.8f, 1.6f), false, 0f, 70);
                    ParticleSystem.ShapeModule sr = it.Ps2.shape;
                    sr.shapeType = ParticleSystemShapeType.Circle;
                    sr.radius = 2.2f;
                    Girar(it.Ps2, 3.5f);
                    it.Ps2.GetComponent<ParticleSystemRenderer>().sharedMaterial = _mPoeira;
                    // BRASAS subindo em espiral ate' o topo do funil
                    it.Ps3 = ParticulaVfx.Novo(raiz, "Brasas", new Color(1f, 0.95f, 0.6f), new Color(1f, 0.5f, 0.1f), 70f,
                        new Vector2(1.2f, 1.8f), new Vector2(5f, 8f), new Vector2(0.07f, 0.16f), false, 0.1f, 140);
                    ParticleSystem.ShapeModule sb = it.Ps3.shape;
                    sb.shapeType = ParticleSystemShapeType.Cone;
                    sb.angle = 28f;
                    sb.radius = 0.8f;
                    Girar(it.Ps3, 3f);
                    break;
                }
                case ComboSintonia.ChuvaDeMagma:
                    it.Chao = NovaZona(raiz);
                    it.Ps = Manual(raiz, "Brasas", null, 140, true);
                    Gravidade(it.Ps, -0.15f);
                    it.Ps2 = Manual(raiz, "Fumaca", _mNuvem, 12, true);
                    Crescer(it.Ps2, 0.6f, 1.6f);
                    FadeSuave(it.Ps2, 0.2f);
                    break;
                case ComboSintonia.CortinaDeVapor:
                    it.Chao = NovaZona(raiz);
                    it.Ps = Manual(raiz, "Vapor", _mNuvem, 34, true);
                    Crescer(it.Ps, 0.7f, 1.35f);
                    FadeSuave(it.Ps, 0.2f);
                    break;
                case ComboSintonia.Lamacal:
                    it.Chao = NovaZona(raiz);
                    it.Ps = Manual(raiz, "Bolhas", _mPoeira, 40, true);
                    Gravidade(it.Ps, 1f);
                    break;
                case ComboSintonia.TempestadeDeAreia:
                    it.Chao = NovaZona(raiz);
                    it.Ps = Manual(raiz, "Parede", _mNuvem, 50, false);
                    GirarY(it.Ps, 0.9f);
                    Crescer(it.Ps, 0.7f, 1.3f);
                    FadeSuave(it.Ps, 0.2f);
                    it.Ps2 = Manual(raiz, "Graos", _mPoeira, 80, false);
                    GirarY(it.Ps2, 2.4f);
                    Esticar(it.Ps2, 0.12f);
                    break;
                case ComboSintonia.CristaisCarregados:
                {
                    it.Chao = NovaZona(raiz);
                    it.Pivo = new GameObject("Pivo").transform;
                    it.Pivo.SetParent(raiz, false);
                    Mesh glb = CristalGlb();
                    if (glb != null)
                    {
                        // o molde: base do cacho em y = 0, centro da pegada na origem do pivo (que gira e escala)
                        it.R = Peca(it.Pivo, glb, _mCristal);
                        it.R.transform.localPosition = -new Vector3(_caixaCristal.center.x, _caixaCristal.min.y, _caixaCristal.center.z);
                    }
                    else
                    {
                        it.R = Peca(it.Pivo, CristalProc(), _mFaceta);
                        it.R.transform.localScale = new Vector3(0.6f, AlturaCristal, 0.6f);
                    }
                    it.Ps = ParticulaVfx.Novo(raiz, "Carga", new Color(1f, 1f, 0.8f), new Color(1f, 0.85f, 0.25f), 12f,
                        new Vector2(0.1f, 0.25f), new Vector2(1f, 3f), new Vector2(0.05f, 0.12f), false, 1f, 24);
                    ParticleSystem.ShapeModule sk = it.Ps.shape;
                    sk.scale = new Vector3(0.7f, 0.7f, 1.6f);
                    it.Ps.transform.localPosition = Vector3.up * 0.9f;
                    break;
                }
                case ComboSintonia.NuvemTempestuosa:
                    it.Chao = NovaZona(raiz);   // a SOMBRA (a casca a poe no chao, fora da raiz que esta' no ceu)
                    it.Ps = Manual(raiz, "Nuvem", _mNuvem, 22, false);
                    FadeSuave(it.Ps, 0.08f);
                    Crescer(it.Ps, 0.85f, 1.2f);
                    it.Linha = Linha(it.Go, 0.45f);
                    it.Pontos = new Vector3[PontosRaio];
                    it.Linha.positionCount = PontosRaio;
                    break;
                // plasma, eletrocussao, torrencial: nao ficam (a forma deles e' do golpe)
            }
        }

        Zona NovaZona(Transform pai)
        {
            var go = new GameObject("Chao");
            go.transform.SetParent(pai, false);
            var z = new Zona { M = new Mesh { name = "ZonaSintonia" } };
            z.M.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = z.M;
            z.R = go.AddComponent<MeshRenderer>();
            z.R.sharedMaterial = _mZona;
            z.R.shadowCastingMode = ShadowCastingMode.Off;
            z.R.receiveShadows = false;
            _zonas.Add(z);
            return z;
        }

        // ------------------------------------------------------------------ fabrica

        static void Ids()
        {
            if (_ids) return;
            _ids = true;
            _idCor = Shader.PropertyToID("_Cor");
            _idBrilho = Shader.PropertyToID("_Brilho");
            _idEscala = Shader.PropertyToID("_Escala");
            _idRolagem = Shader.PropertyToID("_Rolagem");
            _idVeio = Shader.PropertyToID("_Veio");
            _idAnel = Shader.PropertyToID("_Anel");
            _idOnda = Shader.PropertyToID("_Onda");
            _idEnche = Shader.PropertyToID("_Enche");
            _idPeriodo = Shader.PropertyToID("_Periodo");
            _idBase = Shader.PropertyToID("_BaseColor");
            _idEmissao = Shader.PropertyToID("_EmissionColor");
        }

        static void Materiais()
        {
            if (_mZona == null)
            {
                Material a = Resources.Load<Material>(MaterialZona);
                _zonaPropria = a != null && a.shader != null && a.shader.isSupported;
                // sem o asset (ou GPU sem o shader): Unlit transparente de cor chapada — feio, mas ainda deitado no relevo
                _mZona = _zonaPropria ? a : MaterialVfx.Solido(Color.white, MaterialVfx.Mistura.Alfa, true);
            }
            if (_mPoeira == null) _mPoeira = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.PontoSuave());
            if (_mNuvem == null) _mNuvem = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, TexNuvem());
            if (_mFaceta == null) _mFaceta = MaterialVfx.CorDeVertice(MaterialVfx.Mistura.Opaco);   // o cristal de reserva: faceta pela cor de vertice
        }

        /// <summary>O cacho de cristal da Meshy (o mesmo .glb da mata) no Lit com emissao do molde-asset (a variante de
        /// emissao so' chega ao APK por ele); null = sem .glb/textura/Lit (fica o espinho facetado).</summary>
        static Mesh CristalGlb()
        {
            if (_cristalGlb != null || _semGlb) return _cristalGlb;
            Mesh m = Vegetacao.MalhaDoGlb(Mata.Glb[(int)Mata.Peca.Cristal], out Texture tex);
            Material mat = m != null && tex != null
                ? Vegetacao.MaterialDaMeshy(ref _mCristal, "SintoniaCristal", tex, new Color(1f, 0.95f, 0.78f), Vegetacao.MoldeBrilho) : null;
            if (mat == null) { _semGlb = true; return null; }
            _caixaCristal = m.bounds;
            return _cristalGlb = m;
        }

        /// <summary>Espinho de reserva (bipiramide de 6 lados, 1 de altura, pe' enterrado), facetado pela cor de vertice.</summary>
        static Mesh CristalProc()
        {
            if (_cristalProc != null) return _cristalProc;
            var b = new MalhaProc.Construtor();
            const int lados = 6;
            const float r = 0.5f, meio = 0.3f;
            Vector3 pe = new Vector3(0f, -0.2f, 0f), bico = new Vector3(0f, 1f, 0f);
            for (int i = 0; i < lados; i++)
            {
                float a0 = Mathf.PI * 2f * i / lados, a1 = Mathf.PI * 2f * (i + 1) / lados, am = (a0 + a1) * 0.5f;
                Vector3 v0 = new Vector3(Mathf.Cos(a0) * r, meio, Mathf.Sin(a0) * r), v1 = new Vector3(Mathf.Cos(a1) * r, meio, Mathf.Sin(a1) * r);
                Vector3 fora = new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am));
                float claro = i % 2 == 0 ? 1f : 0.72f;
                b.Tri(v0, bico, v1, new Color(claro, claro, claro), fora + Vector3.up * 0.4f);
                b.Tri(pe, v0, v1, new Color(0.45f, 0.45f, 0.45f), fora - Vector3.up * 0.6f);
            }
            return _cristalProc = b.ParaMesh("VfxCristal");
        }

        /// <summary>
        /// O FUNIL do tornado: casca de revolucao de 9 m, estreita no pe' (0,45 m) e aberta no alto (3,3 m), com o EIXO CURVO —
        /// o corpo girando varre a curva e o funil "danca" sem custar nada. COLOR.r = altura (as faixas do shader sobem por
        /// ela), COLOR.a = some no pe' e no topo (a ponta de cima vira labareda pelo ruido). UV em volta fecha no periodo 6.
        /// </summary>
        static Mesh MalhaDoFunil()
        {
            if (_funil != null) return _funil;
            const int an = 14, go = 20;
            const float H = 9f;
            int n1 = go + 1;
            var v = new Vector3[(an + 1) * n1];
            var uv = new Vector2[v.Length];
            var c = new Color32[v.Length];
            for (int a = 0; a <= an; a++)
            {
                float h = a / (float)an;
                float r = 0.45f + 2.85f * Mathf.Pow(h, 1.6f);
                float cx = Mathf.Sin(h * Mathf.PI * 1.1f) * 0.9f * h;
                byte fade = (byte)(255f * Mathf.Min(Mathf.Clamp01(h / 0.1f), 1f - Mathf.Clamp01((h - 0.72f) / 0.28f)));
                for (int g = 0; g <= go; g++)
                {
                    float ang = g * Mathf.PI * 2f / go;
                    int i = a * n1 + g;
                    v[i] = new Vector3(cx + Mathf.Cos(ang) * r, h * H, Mathf.Sin(ang) * r);
                    uv[i] = new Vector2(g / (float)go * 6f, h * 4f);
                    c[i] = new Color32((byte)(h * 255f), 0, 0, fade);
                }
            }
            var t = new int[an * go * 6];
            int k = 0;
            for (int a = 0; a < an; a++)
                for (int g = 0; g < go; g++)
                {
                    int i0 = a * n1 + g, i1 = i0 + n1;
                    t[k++] = i0; t[k++] = i1; t[k++] = i0 + 1;
                    t[k++] = i0 + 1; t[k++] = i1; t[k++] = i1 + 1;
                }
            var m = new Mesh { name = "VfxFunil" };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetColors(c);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return _funil = m;
        }

        /// <summary>Baforada 64x64 gerada 1x (zero arquivo): 7 bolhas gaussianas num cacho + LUZ DE CIMA (topo claro, base a
        /// 72%) — a particula de nuvem le' volume, nao disco. Borda com alfa 0 (sem quadrado).</summary>
        static Texture2D TexNuvem()
        {
            if (_texNuvem != null) return _texNuvem;
            const int n = 64;
            float[] b = { 0.5f, 0.45f, 0.3f, 0.32f, 0.5f, 0.2f, 0.68f, 0.52f, 0.21f, 0.42f, 0.66f, 0.2f, 0.6f, 0.68f, 0.18f, 0.28f, 0.36f, 0.15f, 0.72f, 0.34f, 0.15f };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n, s = 0f;
                    for (int i = 0; i < b.Length; i += 3)
                    {
                        float dx = (u - b[i]) / b[i + 2], dy = (v - b[i + 1]) / b[i + 2];
                        s += Mathf.Exp(-(dx * dx + dy * dy) * 1.6f);
                    }
                    float du = u - 0.5f, dv = v - 0.5f;
                    float borda = Mathf.Clamp01((0.5f - Mathf.Sqrt(du * du + dv * dv)) / 0.12f);
                    float a = Mathf.Clamp01(s * 0.9f) * borda;
                    byte l = (byte)(255f * Mathf.Lerp(0.72f, 1f, v));
                    px[y * n + x] = new Color32(l, l, l, (byte)(a * 255f));
                }
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "NuvemSuave", wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply(false, true);
            return _texNuvem = t;
        }

        /// <summary>Estouro PARADO (taxa 0) de MUNDO: serve varios combos, cor por EmitParams.</summary>
        ParticleSystem Estouro(string nome, Vector2 vida, Vector2 vel, Vector2 tam, float aleatorio, int max, Material mat)
        {
            ParticleSystem ps = ParticulaVfx.Novo(transform, nome, Color.white, Color.white, 0f, vida, vel, tam, true, aleatorio, max);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            if (mat != null) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        /// <summary>Emissor MANUAL (taxa 0; cada particula sai com posicao/velocidade/tamanho/cor/vida proprios) sem o giro de
        /// -90 do ParticulaVfx: local = mundo - raiz. Baforada alfa ordenada pela distancia (sem pipocar a ordem).</summary>
        static ParticleSystem Manual(Transform pai, string nome, Material mat, int max, bool mundo)
        {
            ParticleSystem ps = ParticulaVfx.Novo(pai, nome, Color.white, Color.white, 0f, Vector2.one, Vector2.zero, Vector2.one, mundo, 0f, max);
            ps.transform.localRotation = Quaternion.identity;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (mat != null) r.sharedMaterial = mat;
            r.sortMode = ParticleSystemSortMode.Distance;
            return ps;
        }

        /// <summary>Uma particula com tudo proprio (baforada, brasa, gota). Giro de ate' 20 graus: a luz de cima da textura fica em cima.</summary>
        void Sopro(ParticleSystem ps, Vector3 pos, Vector3 vel, float tam, Color cor, float vida)
        {
            var ep = new ParticleSystem.EmitParams
            {
                position = pos, velocity = vel, startSize = tam, startColor = cor, startLifetime = vida,
                rotation = (Sorte() - 0.5f) * 40f, applyShapeToPosition = false,
            };
            ps.Emit(ep, 1);
        }

        void Clarao(Vector3 pos, float tam, Color cor, float vida)
        {
            var ep = new ParticleSystem.EmitParams
            {
                position = pos, velocity = Vector3.zero, startSize = tam, startColor = cor, startLifetime = vida, applyShapeToPosition = false,
            };
            _clarao.Emit(ep, 1);
        }

        /// <summary>`n` particulas num anel de raio `raio` rente ao chao, voando PARA FORA (cacos, faiscas rasantes, respingo).</summary>
        void Anelada(ParticleSystem ps, Vector3 centro, float raio, int n, Color cor, float fora, float cima)
        {
            for (int k = 0; k < n; k++)
            {
                float a = (k + Sorte() * 0.6f) * (Mathf.PI * 2f / n);
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float x = centro.x + dir.x * raio, z = centro.z + dir.z * raio;
                var ep = new ParticleSystem.EmitParams
                {
                    position = new Vector3(x, Chao(x, z) + 0.3f, z), startColor = cor, applyShapeToPosition = false,
                    velocity = dir * (fora * (0.7f + 0.6f * Sorte())) + Vector3.up * (cima * Sorte()),
                };
                ps.Emit(ep, 1);
            }
        }

        /// <summary>Rajada de `n` em `pos` espalhada numa caixa de lado 2r (x,z) e cor `cor` (Color32: o brilho HDR vem do material
        /// aditivo). Zero alocacao (EmitParams e' struct).</summary>
        static void Emitir(ParticleSystem ps, Vector3 pos, int n, Color cor, float r)
        {
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.scale = new Vector3(Mathf.Max(r * 2f, 0.1f), Mathf.Max(r * 2f, 0.1f), 0.3f);
            var ep = new ParticleSystem.EmitParams { position = pos, applyShapeToPosition = true, startColor = cor };
            ps.Emit(ep, n);
        }

        /// <summary>Quantas particulas cabem neste quadro numa taxa por segundo (o resto fica para o proximo; teto 8 por quadro).</summary>
        static int Quantos(ref float acc, float taxa, float dt)
        {
            acc += Mathf.Max(taxa, 0f) * dt;
            int n = (int)acc;
            acc -= n;
            return Mathf.Min(n, 8);
        }

        /// <summary>Emite da BORDA de um disco (cone de angulo 0 = reto para cima); `espessura` 0 = so' o aro. O raio vem por quadro.</summary>
        static void Anel(ParticleSystem ps, float espessura)
        {
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 0f;
            sh.radiusThickness = espessura;
        }

        static void Taxa(ParticleSystem ps, float taxa)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTimeMultiplier = taxa;
        }

        /// <summary>Orbita em volta do +Z LOCAL (= o cima do mundo, pelo -90 do ParticulaVfx.Novo): o funil GIRA.</summary>
        static void Girar(ParticleSystem ps, float radPorS)
        {
            ParticleSystem.VelocityOverLifetimeModule v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.Local;
            v.x = v.y = v.z = new ParticleSystem.MinMaxCurve(0f);
            v.orbitalX = v.orbitalY = new ParticleSystem.MinMaxCurve(0f);
            v.orbitalZ = new ParticleSystem.MinMaxCurve(radPorS);
        }

        /// <summary>Orbita em volta do +Y local (o emissor Manual nao tem o giro de -90): a parede de areia GIRA.</summary>
        static void GirarY(ParticleSystem ps, float radPorS)
        {
            ParticleSystem.VelocityOverLifetimeModule v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.Local;
            v.x = v.y = v.z = new ParticleSystem.MinMaxCurve(0f);
            v.orbitalX = v.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
            v.orbitalY = new ParticleSystem.MinMaxCurve(radPorS);
        }

        static void Crescer(ParticleSystem ps, float de, float para)
        {
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, de, 1f, para));
        }

        /// <summary>Baforada que ENTRA suave (alfa 0 -> 1 em `entra` da vida), segura e sai: nuvem nao pipoca.</summary>
        static void FadeSuave(ParticleSystem ps, float entra)
        {
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, entra), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        static void Gravidade(ParticleSystem ps, float g)
        {
            ParticleSystem.MainModule m = ps.main;
            m.gravityModifier = g;
        }

        /// <summary>Risco pela velocidade: faisca, meteoro e gota voando leem como RISCO, nao como bolinha.</summary>
        static void Esticar(ParticleSystem ps, float escala)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = escala;
            r.lengthScale = 1f;
        }

        /// <summary>O RASTRO de fogo do meteoro (Trails do proprio sistema, material de faixa): some depois do impacto.</summary>
        static void Rastro(ParticleSystem ps)
        {
            ParticleSystem.TrailModule tr = ps.trails;
            tr.enabled = true;
            tr.lifetime = new ParticleSystem.MinMaxCurve(0.35f);
            tr.minVertexDistance = 0.5f;
            tr.dieWithParticles = false;
            tr.inheritParticleColor = true;
            tr.widthOverTrail = new ParticleSystem.MinMaxCurve(0.6f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorOverTrail = new ParticleSystem.MinMaxGradient(g);
            ps.GetComponent<ParticleSystemRenderer>().trailMaterial = MaterialVfx.DeLinha();
        }

        /// <summary>Filho com malha, sem colisor (a camera e a mira nao esbarram em VFX) e sem sombra.</summary>
        static Renderer Peca(Transform pai, Mesh malha, Material mat)
        {
            var go = new GameObject("Peca");
            go.transform.SetParent(pai, false);
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        /// <summary>Linha aditiva de MUNDO virada para a camera, com a faixa macia (MaterialVfx.DeLinha).</summary>
        static LineRenderer Linha(GameObject go, float largura)
        {
            var l = go.AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.widthMultiplier = largura;
            l.numCapVertices = 2;
            l.alignment = LineAlignment.View;
            l.sharedMaterial = MaterialVfx.DeLinha();
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            return l;
        }

        // ------------------------------------------------------------------ utilidades

        /// <summary>Raio quebrado de `a` a `b` em `saida.Length` pontos: desvio lateral alternado (deterministico pela
        /// semente = o mesmo desenho no mesmo passo), pontas exatas.</summary>
        static void Ziguezague(Vector3[] saida, Vector3 a, Vector3 b, float desvio, int semente)
        {
            int n = saida.Length;
            Vector3 d = b - a;
            Vector3 lado = Vector3.Cross(d, Vector3.up);
            if (lado.sqrMagnitude < 1e-6f) lado = Vector3.Cross(d, Vector3.right);
            lado = lado.normalized;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float s = (Hash01(semente * 73856093 ^ i * 19349663) * 2f - 1f) * desvio * Mathf.Sin(t * Mathf.PI);
                saida[i] = a + d * t + lado * s + Vector3.up * (s * 0.3f);
            }
            saida[0] = a;
            saida[n - 1] = b;
        }

        static float Hash01(int k)
        {
            uint h = (uint)k * 0x9E3779B1u;
            h ^= h >> 15; h *= 0x85EBCA77u; h ^= h >> 13;
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Sorteio da casca (LCG): sem alocacao, sem mexer no UnityEngine.Random do jogo.</summary>
        float Sorte()
        {
            _sorte = _sorte * 1664525u + 1013904223u;
            return (_sorte >> 8) * (1f / 16777216f);
        }

        /// <summary>Sai com MOLA: passa um pouco e volta (o cristal BROTA do chao).</summary>
        static float Mola(float u)
        {
            u = Mathf.Clamp01(u) - 1f;
            return 1f + u * u * (2.7f * u + 1.7f);
        }

        /// <summary>A cor de cada combo (paleta do Elements.luau do Roblox, PONTE A15) — a da onda de choque e do raio.</summary>
        static Color CorDoCombo(ComboSintonia c)
        {
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante: return new Color32(0xFF, 0x8A, 0x3D, 255);
                case ComboSintonia.ChuvaDeMagma: return new Color32(0xFF, 0x45, 0x00, 255);
                case ComboSintonia.ExplosaoDePlasma: return new Color32(0xD8, 0xB8, 0xFF, 255);   // branco-violeta (era ouro: lia so' "branco")
                case ComboSintonia.CortinaDeVapor: return new Color32(0xE8, 0xF4, 0xFF, 255);
                case ComboSintonia.Eletrocussao: return new Color32(0x9B, 0xE7, 0xFF, 255);
                case ComboSintonia.Lamacal: return new Color32(0x9B, 0x7A, 0x4E, 255);   // o #6B5433 do Roblox some no aditivo
                case ComboSintonia.TempestadeTorrencial: return new Color32(0x7F, 0xD4, 0xFF, 255);
                case ComboSintonia.TempestadeDeAreia: return new Color32(0xC9, 0xA9, 0x6A, 255);
                case ComboSintonia.CristaisCarregados: return new Color32(0xF5, 0xD9, 0x0A, 255);
                default: return new Color32(0xCF, 0xE8, 0xF5, 255);
            }
        }

        /// <summary>sRGB -> linear (o Brilho vai por SetVector, sem a conversao do SetColor).</summary>
        static Color Lin(Color c) => c.linear;

        static int Passo() => (int)(Time.time / PassoAnime);

        /// <summary>Onde o conjurador esta'; destruido (fim de arena) cai no ponto do combo.</summary>
        static Vector3 PosDe(IEntidade e, Vector3 reserva)
        {
            if (e == null) return reserva;
            Object o = e as Object;
            if (!ReferenceEquals(o, null) && o == null) return reserva;
            return e.Pos;
        }
    }
}
