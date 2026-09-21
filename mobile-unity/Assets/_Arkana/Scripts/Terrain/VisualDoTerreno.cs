using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Terrain
{
    /// <summary>A FORMA de cada estado (GDD §10: cor + FORMA, nunca so' cor — gelo e raio nao podem depender do matiz).</summary>
    public enum FormaCelula { Nenhuma, Quadrado, Mancha, Laje, Losango, Disco, Bloco }

    /// <summary>
    /// A LEITURA do terreno reativo (GDD §14: o jogador tem de LER o mapa mudando), PURA: cor e forma por estado,
    /// o teto de emissores de fogo (mobile), a escolha de quem ganha emissor, o estagio do muro por hp e o gelo que sustenta
    /// (a caixa do colisor, a regra do nadador, o tiro que pousa no gelo).
    /// </summary>
    public static class LeituraDoTerreno
    {
        /// <summary>Chamas COM particula ao mesmo tempo — KNOB de aparelho (Godot MAX_FIRE_FX). Alem dele a celula queima so' com o quad.</summary>
        public const int TetoDeFogo = 24;
        /// <summary>Emissores de faisca na agua eletrificada — KNOB de aparelho.</summary>
        public const int TetoDeFaiscas = 8;
        /// <summary>Instancias por chamada de instancing (limite classico do DrawMeshInstanced): lote maior sai em fatias.</summary>
        public const int PorLote = 1023;
        /// <summary>Sentinela "estado sem cor": estado novo que ninguem pintou sai MAGENTA e o teste reprova.</summary>
        public static readonly Color SemCor = new Color(1f, 0f, 1f, 1f);

        public static Color CorDoEstado(EstadoCelula s)
        {
            switch (s)
            {
                case EstadoCelula.Normal: return Color.clear;                               // nada a desenhar
                case EstadoCelula.Queimando: return new Color32(0xFF, 0x5A, 0x2A, 210);     // fogo (paleta GDD §10)
                case EstadoCelula.Carvao: return new Color(0.10f, 0.08f, 0.07f, 1f);        // a cobertura SUMIU
                case EstadoCelula.Congelado: return new Color(0.72f, 0.90f, 1f, 0.78f);     // gelo: placa clara sobre a agua
                case EstadoCelula.Eletrificado: return new Color32(0xF5, 0xD9, 0x0A, 230);  // raio (paleta §10): amarelo SOBRE agua azul
                case EstadoCelula.Lama: return new Color(0.38f, 0.26f, 0.14f, 1f);
                case EstadoCelula.Muro: return new Color(0.56f, 0.57f, 0.62f, 1f);          // pedra fria (tom de rocha da ilha)
            }
            return SemCor;
        }

        public static FormaCelula FormaDoEstado(EstadoCelula s)
        {
            switch (s)
            {
                case EstadoCelula.Queimando: return FormaCelula.Quadrado;    // brasa cobrindo a celula: tapete continuo
                case EstadoCelula.Carvao: return FormaCelula.Mancha;        // estrela irregular girada: queimado, nao pintado
                case EstadoCelula.Congelado: return FormaCelula.Laje;       // placa GROSSA: le-se como chao (rota)
                case EstadoCelula.Eletrificado: return FormaCelula.Losango; // trelica de losangos: padrao que nenhum outro tem
                case EstadoCelula.Lama: return FormaCelula.Disco;           // pocas redondas que se fundem
                case EstadoCelula.Muro: return FormaCelula.Bloco;           // pedra alta: cobertura
            }
            return FormaCelula.Nenhuma;
        }

        /// <summary>0 inteiro, 1 rachado, 2 em ruina (tercos do WallHp): o muro MOSTRA quanto aguenta (fresta = forma, escuro = cor).</summary>
        public static int EstagioDoMuro(float hp)
        {
            float f = hp / Balance.Terrain.WallHp;
            return f > 2f / 3f ? 0 : f > 1f / 3f ? 1 : 2;
        }

        /// <summary>s para o muro SAIR do chao (rapido: a cobertura ja' vale na logica) e para AFUNDAR depois de cair. KNOBs por foto.</summary>
        public const float SubidaDoMuro = 0.3f, QuedaDoMuro = 0.5f;

        /// <summary>
        /// Quanto do muro esta' FORA do chao (0..1), `idade` s depois de nascer e `caido` s depois de cair (negativo = de pe'):
        /// sobe rapido e assenta, cai devagar e despenca. Caiu no meio da subida: desce dali, nunca pula para cima.
        /// O colisor nao le' isto — a queda e' so' desenho.
        /// </summary>
        public static float FracaoDoMuro(float idade, float caido)
        {
            float s = Mathf.Clamp01(idade / SubidaDoMuro);
            s = 1f - (1f - s) * (1f - s);
            if (caido < 0f) return s;
            float q = Mathf.Clamp01(caido / QuedaDoMuro);
            return Mathf.Min(s, 1f - q * q);
        }

        // ------------------------------------------------------------------ gelo: a ROTA (GDD §14, "lago congela e vira caminho")

        /// <summary>m de gelo abaixo da lamina no COLISOR = o fundo do nado (Agua.PEITO): o nadador encosta na borda, nunca passa por baixo.</summary>
        public const float GrossuraDoGelo = Agua.PEITO;

        /// <summary>
        /// Base da laje DESENHADA em relacao a' lamina. Fresca (0,45 m), o topo fica 5 cm acima do colisor: o pe' pisa NO gelo
        /// (a -0,1 de antes o mago afundava 35 cm na placa). Derretendo, ela afina e o topo AFUNDA sob a lamina: a rota avisa
        /// que vai sumir. KNOB por foto (59-gelo-em-cima), vetavel.
        /// </summary>
        public static readonly float BaseDoGelo = -0.4f;   // readonly, nao const: o teste le' o valor do Arkana.dll

        /// <summary>O gelo AFINA no fim (derretendo) mas nunca encolhe a pegada: a rota que a logica da' e' a que se ve'.</summary>
        public static float EspessuraDoGelo(float restante) =>
            0.45f * Mathf.Lerp(0.35f, 1f, LeituraDosKits.EscalaPorRestante(restante, Balance.Terrain.FreezeDuration));

        /// <summary>
        /// O COLISOR da celula congelada: TOPO na lamina (onde a Agua mede o peito — em cima dele ninguem nada), a pegada da
        /// celula INTEIRA (vizinhas emendam sem fresta: piso continuo) e GrossuraDoGelo para baixo. Fundo raso so' enterra a caixa.
        /// </summary>
        public static Bounds CaixaDoGelo(Vector3 centro, float lamina, float lado) =>
            new Bounds(new Vector3(centro.x, lamina - GrossuraDoGelo * 0.5f, centro.z), new Vector3(lado, GrossuraDoGelo, lado));

        /// <summary>
        /// A REGRA DO NADADOR: quem tem o pe' abaixo do topo quando a celula SOLIDIFICA sobe para o topo, no mesmo x,z — ninguem
        /// fica preso dentro nem embaixo do gelo (sai do nado para o gelo, encharcado). Quem ja' esta' em cima, ou no ar, fica.
        /// </summary>
        public static Vector3 PorEmCima(Vector3 pe, Bounds caixa) =>
            pe.y < caixa.max.y ? new Vector3(pe.x, caixa.max.y, pe.z) : pe;

        /// <summary>
        /// O TIRO pousa no gelo como pousa no chao: celula congelada e o tiro na lamina ou abaixo. Por cima segue voando; agua
        /// LIQUIDA nao o para (quem para e' o fundo). O projetil nao tem fisica: quem pergunta e' o laco da Partida, como no muro.
        /// </summary>
        public static bool TiroNoGelo(TerrenoReativo t, Vector3 pos, float lamina) =>
            t != null && pos.y <= lamina && t.EstadoEm(pos) == EstadoCelula.Congelado;

        /// <summary>
        /// As celulas do `estado` que ganham emissor: as `teto` mais PERTO de `perto` (a camera), em ordem de distancia
        /// no chao. Sem alocar (insercao ordenada, O(n x teto)). Devolve quantas — nunca mais que `teto`.
        /// </summary>
        public static int EscolherEmissores(IReadOnlyList<CelulaVisivel> vis, EstadoCelula estado, Vector3 perto, int teto, List<CelulaVisivel> saida)
        {
            saida.Clear();
            if (vis == null || teto <= 0) return 0;
            for (int i = 0; i < vis.Count; i++)
            {
                CelulaVisivel c = vis[i];
                if (c == null || c.Estado != estado) continue;
                float d = Dist2(c.Centro, perto);
                if (saida.Count >= teto && d >= Dist2(saida[saida.Count - 1].Centro, perto)) continue;
                int j = saida.Count;
                while (j > 0 && Dist2(saida[j - 1].Centro, perto) > d) j--;
                saida.Insert(j, c);
                if (saida.Count > teto) saida.RemoveAt(teto);
            }
            return saida.Count;
        }

        static float Dist2(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }

    /// <summary>
    /// UM objeto por celula viva, com pool — o padrao do muro, PURO para o teste. `Pegar` a celula que esta' no estado NESTE
    /// quadro (`novo` = acabou de sair do pool ou de nascer: posicione); `Soltar` no fim do quadro devolve ao pool quem nao foi
    /// pego — a celula saiu de Visiveis (o gelo derreteu). O pool nunca passa do maior numero de celulas vivas juntas.
    /// </summary>
    public sealed class PoolDeCelulas<T>
    {
        readonly System.Func<T> _novo;
        readonly Dictionary<int, T> _vivos = new Dictionary<int, T>();
        readonly Dictionary<int, int> _visto = new Dictionary<int, int>();
        readonly Stack<T> _livres = new Stack<T>();
        readonly List<int> _soltar = new List<int>();

        public PoolDeCelulas(System.Func<T> novo) { _novo = novo; }

        public int Vivos => _vivos.Count;
        public int Livres => _livres.Count;

        public T Pegar(int idx, int quadro, out bool novo)
        {
            T t;
            novo = !_vivos.TryGetValue(idx, out t);
            if (novo) _vivos[idx] = t = _livres.Count > 0 ? _livres.Pop() : _novo();
            _visto[idx] = quadro;
            return t;
        }

        public void Soltar(int quadro, System.Action<T> aoSoltar)
        {
            _soltar.Clear();
            foreach (KeyValuePair<int, int> kv in _visto) if (kv.Value != quadro) _soltar.Add(kv.Key);
            for (int i = 0; i < _soltar.Count; i++)
            {
                T t = _vivos[_soltar[i]];
                _vivos.Remove(_soltar[i]);
                _visto.Remove(_soltar[i]);
                aoSoltar(t);
                _livres.Push(t);
            }
        }
    }

    /// <summary>
    /// PECA DA MESHY em Resources (.glb com o pe' em y=0): a malha e um URP Lit com a textura dela — fosco, sem metal e dos
    /// DOIS lados (o grampo do KitCenario.Domado: o decimado da Meshy tem triangulo com a volta trocada). UM material por
    /// .glb: todo muro e toda torreta dividem o mesmo (lote do SRP). False = falta o .glb, a malha ou o Lit: quem chama
    /// poe a primitiva (fiacao defensiva, como Ruinas e Vegetacao). Serve o muro daqui e as pecas dos kits (VisualDosKits).
    /// ponytail: um no' so', sem transformacao (as cinco pecas da onda 11 conferidas); peca com hierarquia pediria a
    /// matriz do no' junto.
    /// </summary>
    public static class PecaDaMeshy
    {
        static readonly Dictionary<string, KeyValuePair<Mesh, Material>> _pecas = new Dictionary<string, KeyValuePair<Mesh, Material>>();

        public static bool Carregar(string nome, out Mesh malha, out Material mat)
        {
            KeyValuePair<Mesh, Material> p;
            if (_pecas.TryGetValue(nome, out p) && p.Key != null && p.Value != null) { malha = p.Key; mat = p.Value; return true; }
            malha = null;
            mat = null;
            GameObject g = Resources.Load<GameObject>(nome);
            MeshFilter mf = g != null ? g.GetComponentInChildren<MeshFilter>(true) : null;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (mf == null || mf.sharedMesh == null || lit == null) return false;
            Renderer r = mf.GetComponent<Renderer>();
            Material o = r != null ? r.sharedMaterial : null;
            mat = new Material(lit) { name = nome + " (meshy)" };
            if (o != null) mat.SetTexture("_BaseMap", o.HasProperty("baseColorTexture") ? o.GetTexture("baseColorTexture") : o.mainTexture);
            mat.SetFloat("_Cull", 0f);          // 0 = Off: frente e verso
            mat.SetFloat("_Smoothness", 0.2f);  // pedra, cobre e madeira foscos: o 0,5 do Lit vira plastico
            mat.SetFloat("_Metallic", 0f);      // metal sem probe vira breu (a licao do mago preto)
            malha = mf.sharedMesh;
            _pecas[nome] = new KeyValuePair<Mesh, Material>(malha, mat);
            return true;
        }
    }

    /// <summary>
    /// DESENHA o terreno reativo (GDD §14) lendo `TerrenoReativo.Visiveis` a cada frame — nenhum estado de jogo aqui.
    /// Estados rasos (brasa, carvao, gelo, raio, lama) saem por INSTANCING: um lote por forma, em fatias de 1023 (o
    /// carvao e' permanente e acumula: centenas de celulas = poucas chamadas). Fogo e faisca em particula com TETO
    /// (LeituraDoTerreno.TetoDeFogo/TetoDeFaiscas), nos mais perto da camera. O muro e' objeto de verdade (pool): o muro
    /// de TERRA da Meshy (pedras empilhadas, frestas ambar; sem o .glb, a pedra facetada no material do mundo) + BoxCollider
    /// (nao se atravessa, como no Godot); SOBE do chao, racha pelo hp e AFUNDA ao cair. O TIRO quem barra e' a Partida.
    /// O GELO e' chao de verdade pelo mesmo padrao: um BoxCollider por celula congelada (pool), topo na lamina — o corpo anda
    /// por cima em vez de nadar por baixo; derreteu, o colisor sai e quem estava em cima cai na agua.
    /// Arvore queimada e' da Vegetacao (nao duplica). O TerrenoReativoBehaviour nasce um frame depois e muda no restart:
    /// le' Atual a cada frame.
    /// </summary>
    public sealed class VisualDoTerreno : MonoBehaviour
    {
        /// <summary>Fracao da celula que o desenho ocupa: a fresta entre muros vizinhos le' como blocos, nao parede lisa.</summary>
        const float CelulaVisual = 0.96f;
        /// <summary>Nome em Resources do muro de terra da Meshy (onda 11). Aparece em TODO tiro de terra: e' a peca mais vista.</summary>
        public const string MuroMeshy = "40-muro-terra";
        /// <summary>m que o muro da Meshy desce abaixo do chao (o topo fica em WallHeight): encosta nao mostra fresta no pe'.</summary>
        const float Enterra = 0.3f;
        const float PassoAnime = 1f / 12f;
        static readonly float[] Pisca = { 0.95f, 0.2f, 0.8f, 0.1f, 1f, 0.3f };   // raio: liga-desliga irregular
        static readonly float[] Flicker = { 1f, 0.85f, 0.95f, 0.8f };
        static readonly Bounds Mundo = new Bounds(Vector3.zero, Vector3.one * 5000f);

        static Material _mBrasa, _mCarvao, _mGelo, _mRaio, _mLama;
        static Mesh _mancha, _laje;
        static readonly Mesh[] _pedra = new Mesh[3];
        /// <summary>O Lit do muro da Meshy por estagio (0 = o da peca; 1 e 2 escurecidos): material trocado, sem PropertyBlock — o lote do SRP fica.</summary>
        static readonly Material[] _matMuro = new Material[3];
        static int _idCor;

        struct Apoio
        {
            public Vector3 Chao;
            public Quaternion Inclina;
            public float Agua;
        }

        sealed class MuroVivo
        {
            public GameObject Go;
            /// <summary>O que SOBE e AFUNDA: o colisor fica na raiz, onde a logica diz.</summary>
            public Transform Corpo;
            /// <summary>As duas paredes de costas da Meshy; sem o .glb, A = a pedra procedural e B = null.</summary>
            public Renderer A, B;
            public BoxCollider Bc;
            public int Estagio, Quadro;
            /// <summary>Time.time do nascimento e da queda (Caiu negativo = de pe').</summary>
            public float Nasceu, Caiu;
        }

        readonly Dictionary<int, Apoio> _apoio = new Dictionary<int, Apoio>();
        readonly Dictionary<int, MuroVivo> _muros = new Dictionary<int, MuroVivo>();
        readonly Stack<MuroVivo> _murosLivres = new Stack<MuroVivo>();
        /// <summary>Muros que a logica ja' derrubou e ainda estao afundando (sem colisor); acabou, voltam ao pool.</summary>
        readonly List<MuroVivo> _caindo = new List<MuroVivo>();
        readonly List<int> _soltar = new List<int>();
        readonly List<CelulaVisivel> _escolhidas = new List<CelulaVisivel>();
        /// <summary>O CHAO do gelo: um BoxCollider por celula congelada; derreteu, volta ao pool no mesmo quadro.</summary>
        PoolDeCelulas<BoxCollider> _gelos;
        static readonly Collider[] _dentro = new Collider[32];
        Lote _brasa, _carvao, _gelo, _raio, _lama;
        Emissores _fogo, _faiscas;
        TerrenoReativo _ultimo;
        int _quadro;
        float _cs;

        /// <summary>Nasce sob a arena (morre com ela).</summary>
        public static VisualDoTerreno Criar(Transform arena)
        {
            var go = new GameObject("VisualDoTerreno");
            if (arena != null) go.transform.SetParent(arena, false);
            return go.AddComponent<VisualDoTerreno>();
        }

        void Awake()
        {
            _cs = Balance.Terrain.CellSize;
            if (_idCor == 0) _idCor = Shader.PropertyToID("_BaseColor");
            if (_mBrasa == null)
            {
                _mBrasa = MaterialVfx.Solido(LeituraDoTerreno.CorDoEstado(EstadoCelula.Queimando), MaterialVfx.Mistura.Alfa);
                _mCarvao = MaterialVfx.Solido(LeituraDoTerreno.CorDoEstado(EstadoCelula.Carvao), MaterialVfx.Mistura.Opaco);
                _mGelo = MaterialVfx.Solido(LeituraDoTerreno.CorDoEstado(EstadoCelula.Congelado), MaterialVfx.Mistura.Alfa);
                _mRaio = MaterialVfx.Solido(LeituraDoTerreno.CorDoEstado(EstadoCelula.Eletrificado), MaterialVfx.Mistura.Aditivo);
                _mLama = MaterialVfx.Solido(LeituraDoTerreno.CorDoEstado(EstadoCelula.Lama), MaterialVfx.Mistura.Opaco);
            }
            _brasa = new Lote(MalhaVfx.Quad(), _mBrasa);
            _carvao = new Lote(Mancha(), _mCarvao);
            _gelo = new Lote(Laje(), _mGelo);
            _raio = new Lote(MalhaVfx.Quad(), _mRaio);
            _lama = new Lote(MalhaVfx.Disco(), _mLama);
            _fogo = new Emissores(NovoFogo);
            _faiscas = new Emissores(NovaFaisca);
            _gelos = new PoolDeCelulas<BoxCollider>(NovoGelo);
        }

        void LateUpdate()
        {
            _quadro++;
            TerrenoReativoBehaviour b = TerrenoReativoBehaviour.Atual;
            TerrenoReativo t = b != null ? b.Terreno : null;
            if (t != _ultimo) { _apoio.Clear(); _ultimo = t; }
            IReadOnlyList<CelulaVisivel> vis = t != null ? t.Visiveis : null;
            _brasa.N = 0; _carvao.N = 0; _gelo.N = 0; _raio.N = 0; _lama.N = 0;
            if (vis != null)
                for (int i = 0; i < vis.Count; i++) Classificar(vis[i]);
            SoltarMuros();
            _gelos.Soltar(_quadro, SoltarGelo);

            Camera cam = Camera.main;
            Vector3 olho = cam != null ? cam.transform.position : transform.position;
            LeituraDoTerreno.EscolherEmissores(vis, EstadoCelula.Queimando, olho, LeituraDoTerreno.TetoDeFogo, _escolhidas);
            _fogo.Atualizar(_escolhidas, this, false);
            LeituraDoTerreno.EscolherEmissores(vis, EstadoCelula.Eletrificado, olho, LeituraDoTerreno.TetoDeFaiscas, _escolhidas);
            _faiscas.Atualizar(_escolhidas, this, true);

            // UM material por estado: todas as brasas piscam juntas (e tudo bem: um set por passo e' mais barato que um por celula)
            int passo = (int)(Time.time / PassoAnime);
            Tingir(_mBrasa, EstadoCelula.Queimando, Flicker[passo % Flicker.Length]);
            Tingir(_mRaio, EstadoCelula.Eletrificado, Pisca[passo % Pisca.Length]);
            if (!SystemInfo.supportsInstancing) return;
            _brasa.Desenhar(); _carvao.Desenhar(); _gelo.Desenhar(); _raio.Desenhar(); _lama.Desenhar();
        }

        void Classificar(CelulaVisivel c)
        {
            if (c == null) return;
            switch (c.Estado)
            {
                case EstadoCelula.Queimando: _brasa.Add(Raso(c, 0.12f, 0f, _cs * CelulaVisual)); break;
                case EstadoCelula.Carvao: _carvao.Add(Raso(c, 0.1f, c.Semente % 360u, _cs * 0.9f)); break;
                // disco de raio 1: 0,56 x 3 m = pocas que se fundem com as vizinhas
                case EstadoCelula.Lama: _lama.Add(Raso(c, 0.11f, 0f, _cs * 0.56f)); break;
                // losango = quadrado a 45 graus com a DIAGONAL da celula: as pontas se tocam e a agua vira trelica
                case EstadoCelula.Eletrificado: _raio.Add(NaAgua(c, 0.06f, 45f, new Vector3(_cs * 0.7071f, 1f, _cs * 0.7071f))); break;
                case EstadoCelula.Congelado:
                    _gelo.Add(NaAgua(c, LeituraDoTerreno.BaseDoGelo, 0f, new Vector3(_cs * CelulaVisual, LeituraDoTerreno.EspessuraDoGelo(c.Restante), _cs * CelulaVisual)));
                    Solidificar(c);
                    break;
                case EstadoCelula.Muro: DesenharMuro(c); break;
            }
        }

        /// <summary>Plano rente ao chao, inclinado pela normal do relevo (encosta nao enterra meia brasa).</summary>
        Matrix4x4 Raso(CelulaVisivel c, float acima, float giro, float lado)
        {
            Apoio a = ApoioDe(c);
            return Matrix4x4.TRS(a.Chao + a.Inclina * (Vector3.up * acima), a.Inclina * Quaternion.Euler(0f, giro, 0f), new Vector3(lado, 1f, lado));
        }

        /// <summary>Na lamina d'agua (plana), nao no fundo do lago.</summary>
        Matrix4x4 NaAgua(CelulaVisivel c, float acima, float giro, Vector3 escala)
        {
            Apoio a = ApoioDe(c);
            return Matrix4x4.TRS(new Vector3(a.Chao.x, a.Agua + acima, a.Chao.z), Quaternion.Euler(0f, giro, 0f), escala);
        }

        /// <summary>Chao, inclinacao e agua da celula — o relevo nao muda: calcula 1x por celula (ruido por frame em 1000 celulas pesaria).</summary>
        Apoio ApoioDe(CelulaVisivel c)
        {
            Apoio a;
            if (_apoio.TryGetValue(c.Idx, out a)) return a;
            float x = c.Centro.x, z = c.Centro.z;
            float chao = Ilha.AlturaDoChao(x, z);
            float agua = Ilha.SuperficieDaAgua(x, z);
            Relevo r = Ilha.Atual != null ? Ilha.Atual.Relevo : null;
            a.Chao = new Vector3(x, chao, z);
            a.Inclina = Quaternion.FromToRotation(Vector3.up, r != null ? r.Normal(x, z) : Vector3.up);
            a.Agua = agua != Relevo.Seco ? agua : chao;
            _apoio[c.Idx] = a;
            return a;
        }

        static void Tingir(Material m, EstadoCelula s, float f)
        {
            if (m == null) return;
            Color c = LeituraDoTerreno.CorDoEstado(s);
            c.a *= f;
            m.SetColor(_idCor, c);
        }

        // ------------------------------------------------------------------ gelo (a rota)

        /// <summary>
        /// A celula congelada vira CHAO, no padrao do muro: BoxCollider na raiz de um objeto do pool, camada padrao (o
        /// CharacterController pisa; a Queda e o pouso o acham pelo ChaoComObstaculos). Posicionado INATIVO, entra na fisica ja'
        /// no lugar; quem estava dentro sobe (TirarDeDentro).
        /// </summary>
        void Solidificar(CelulaVisivel c)
        {
            bool novo;
            BoxCollider bc = _gelos.Pegar(c.Idx, _quadro, out novo);
            if (!novo) return;
            Apoio a = ApoioDe(c);
            Bounds caixa = LeituraDoTerreno.CaixaDoGelo(a.Chao, a.Agua, _cs);
            bc.transform.position = caixa.center;
            bc.size = caixa.size;
            bc.gameObject.SetActive(true);
            TirarDeDentro(caixa);
        }

        BoxCollider NovoGelo()
        {
            var go = new GameObject("Gelo");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            return go.AddComponent<BoxCollider>();
        }

        /// <summary>Derreteu: o chao sai JA' e quem estava em cima cai na agua (e nada, como sempre: a Agua mede pela lamina).</summary>
        static void SoltarGelo(BoxCollider bc) { bc.gameObject.SetActive(false); }

        /// <summary>
        /// Os corpos (CharacterController) dentro da caixa que acabou de solidificar sobem para o topo (LeituraDoTerreno.PorEmCima).
        /// ponytail: so' na BORDA do congelamento. Nadar CONTRA a borda do gelo nao sobe (degrau de 1,2 m > stepOffset 0,4): da
        /// agua para o gelo, so' pela margem. Subir pela borda pediria esta regra por contato, no Pawn.
        /// </summary>
        static void TirarDeDentro(Bounds caixa)
        {
            int n = Physics.OverlapBoxNonAlloc(caixa.center, caixa.extents, _dentro, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            bool mexeu = false;
            for (int i = 0; i < n; i++)
            {
                if (!(_dentro[i] is CharacterController)) continue;
                Transform t = _dentro[i].transform;
                Vector3 p = LeituraDoTerreno.PorEmCima(t.position, caixa);
                if (p == t.position) continue;
                t.position = p;
                mexeu = true;
            }
            if (mexeu) Physics.SyncTransforms();   // o CharacterController le' a pose nova no proximo Move (como no Teleportar)
        }

        // ------------------------------------------------------------------ muro

        void DesenharMuro(CelulaVisivel c)
        {
            MuroVivo m;
            if (!_muros.TryGetValue(c.Idx, out m))
            {
                m = _murosLivres.Count > 0 ? _murosLivres.Pop() : NovoMuro();
                // giro por Semente (907 + celula): vizinhos nao saem clonados, e duas montagens dao o MESMO muro
                m.Go.transform.SetPositionAndRotation(ApoioDe(c).Chao, Quaternion.Euler(0f, (c.Semente % 4u) * 90f, 0f));
                m.Estagio = -1;
                m.Nasceu = Time.time;
                m.Caiu = -1f;
                m.Bc.enabled = true;
                m.Go.SetActive(true);
                _muros[c.Idx] = m;
            }
            m.Quadro = _quadro;
            int e = LeituraDoTerreno.EstagioDoMuro(c.MuroHp);
            if (e != m.Estagio) { m.Estagio = e; Rachar(m, e); }
            Erguer(m);
        }

        void SoltarMuros()
        {
            _soltar.Clear();
            foreach (KeyValuePair<int, MuroVivo> kv in _muros) if (kv.Value.Quadro != _quadro) _soltar.Add(kv.Key);
            for (int i = 0; i < _soltar.Count; i++)
            {
                MuroVivo m = _muros[_soltar[i]];
                _muros.Remove(_soltar[i]);
                m.Bc.enabled = false;   // o colisor sai JA': muro caido nao segura ninguem (a queda e' so' desenho)
                m.Caiu = Time.time;
                _caindo.Add(m);
            }
            for (int i = _caindo.Count - 1; i >= 0; i--)
            {
                MuroVivo m = _caindo[i];
                if (Erguer(m) > 0f) continue;
                m.Go.SetActive(false);
                _murosLivres.Push(m);
                _caindo.RemoveAt(i);
            }
        }

        /// <summary>Sobe do chao ao nascer e afunda tombando ao cair (LeituraDoTerreno.FracaoDoMuro); de pe' e assentado, nao mexe.</summary>
        static float Erguer(MuroVivo m)
        {
            float idade = Time.time - m.Nasceu;
            if (m.Caiu < 0f && idade > LeituraDoTerreno.SubidaDoMuro + 0.2f) return 1f;   // o ultimo quadro da subida ja' gravou 1
            float f = LeituraDoTerreno.FracaoDoMuro(idade, m.Caiu < 0f ? -1f : Time.time - m.Caiu);
            m.Corpo.localPosition = Vector3.down * ((Balance.Terrain.WallHeight + Enterra) * (1f - f));
            m.Corpo.localRotation = m.Caiu < 0f ? Quaternion.identity : Quaternion.Euler(12f * (1f - f), 0f, 0f);   // desaba para um lado
            return f;
        }

        /// <summary>
        /// O muro MOSTRA quanto aguenta: rachado, a parede de tras abre uma FRESTA (tomba 4 graus para fora) e escurece; em
        /// ruina, as duas abrem, afundam um palmo e escurecem mais (a mesma leitura da pedra procedural: fresta = forma,
        /// escuro = cor). Sem o .glb, a pedra do estagio.
        /// </summary>
        static void Rachar(MuroVivo m, int e)
        {
            if (m.B == null) { m.A.GetComponent<MeshFilter>().sharedMesh = Pedra(e); return; }
            float ta = e == 2 ? 5f : 0f, tb = e == 0 ? 0f : e == 1 ? 4f : 8f;
            var pe = new Vector3(0f, -Enterra - (e == 2 ? 0.25f : 0f), 0f);
            m.A.transform.localRotation = Quaternion.Euler(-ta, 0f, 0f);
            m.B.transform.localRotation = Quaternion.Euler(tb, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f);
            m.A.transform.localPosition = pe;
            m.B.transform.localPosition = pe;
            m.A.sharedMaterial = _matMuro[e];
            m.B.sharedMaterial = _matMuro[e];
        }

        MuroVivo NovoMuro()
        {
            var go = new GameObject("Muro");
            go.transform.SetParent(transform, false);
            var corpo = new GameObject("Corpo").transform;
            corpo.SetParent(go.transform, false);
            var m = new MuroVivo { Go = go, Corpo = corpo, Estagio = -1, Caiu = -1f };
            float lado = _cs * CelulaVisual, h = Balance.Terrain.WallHeight;
            Mesh malha;
            Material mat;
            if (PecaDaMeshy.Carregar(MuroMeshy, out malha, out mat))
            {
                // ponytail: o muro da Meshy e' uma PAREDE (~1,25 m de fundo no lado -z; o pedrisco do +z fica por dentro). Duas
                // de COSTAS fecham a celula e cruzam os degraus do topo (a ponta baixa de uma cai atras da alta da outra): o
                // bloco de 3,5 m inteiro, pedra de verdade nas quatro faces. 2 x 1,5K tris por muro.
                Bounds b = malha.bounds;
                var esc = new Vector3(lado / Mathf.Max(b.size.x, 0.01f), (h + Enterra) / Mathf.Max(b.size.y, 0.01f),
                    lado * 0.5f / Mathf.Max(-b.min.z, 0.01f));
                if (_matMuro[0] != mat)
                {
                    _matMuro[0] = mat;
                    for (int e = 1; e < 3; e++)
                    {
                        float k = 1f - 0.2f * e;   // KNOB: 1 / 0,8 / 0,6 — o escuro diz quanto aguenta
                        _matMuro[e] = new Material(mat) { name = mat.name + " estagio " + e };
                        _matMuro[e].SetColor(_idCor, new Color(k, k * 0.97f, k * 0.95f, 1f));
                    }
                }
                m.A = Parede(corpo, malha, mat, esc);
                m.B = Parede(corpo, malha, mat, esc);
            }
            // a pedra PERTENCE ao mundo: o mesmo material da ilha (cor de vertice, luz e sombra)
            else m.A = Parede(corpo, Pedra(0), Ilha.MaterialPadrao(), Vector3.one);
            m.Bc = go.AddComponent<BoxCollider>();
            m.Bc.size = new Vector3(lado, h, lado);
            m.Bc.center = new Vector3(0f, h * 0.5f, 0f);
            return m;
        }

        /// <summary>Uma parede do muro (renderer sem colisor: quem segura e' o BoxCollider da raiz). Faz sombra, como a pedra fazia.</summary>
        static Renderer Parede(Transform pai, Mesh malha, Material mat, Vector3 esc)
        {
            var go = new GameObject("Parede");
            go.transform.SetParent(pai, false);
            go.transform.localScale = esc;
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return mr;
        }

        /// <summary>
        /// A pedra do muro, gerada 1x por estagio com Sorteio(WallSeed): o MESMO muro em qualquer montagem (contrato
        /// do 907). Base enterrada 0,4 m (encosta nao mostra fresta), topo quebrado em volta de WallHeight — nunca
        /// meio metro alem (acima disso o visual mente a cobertura). Inteira = 1 bloco; rachada = 2 blocos com FRESTA;
        /// em ruina = fresta larga, um lado mais baixo, mais escura.
        /// </summary>
        static Mesh Pedra(int estagio)
        {
            if (_pedra[estagio] != null) return _pedra[estagio];
            float meia = Balance.Terrain.CellSize * CelulaVisual * 0.5f, h = Balance.Terrain.WallHeight;
            var rng = new Sorteio(TerrenoReativo.WallSeed + estagio);
            var b = new MalhaProc.Construtor();
            float escuro = 0.18f * estagio, quebra = 0.25f + 0.12f * estagio;
            if (estagio == 0) Rocha(b, rng, new Vector2(-meia, -meia), new Vector2(meia, meia), h, quebra, escuro);
            else
            {
                float fresta = estagio == 1 ? 0.1f : 0.28f;
                float corte = rng.Faixa(-0.25f, 0.25f) * meia;   // a rachadura nao passa no meio exato
                Rocha(b, rng, new Vector2(-meia, -meia), new Vector2(corte - fresta * 0.5f, meia), h, quebra, escuro);
                Rocha(b, rng, new Vector2(corte + fresta * 0.5f, -meia), new Vector2(meia, meia), h - 0.15f * estagio, quebra, escuro);
            }
            return _pedra[estagio] = b.ParaMesh("MuroPedra" + estagio, QualitySettings.activeColorSpace == ColorSpace.Linear);
        }

        /// <summary>Bloco afunilado (pedra erguida, nao caixote) com topo em cume irregular e faces de tons sorteados.</summary>
        static void Rocha(MalhaProc.Construtor b, Sorteio rng, Vector2 min, Vector2 max, float h, float quebra, float escuro)
        {
            const float Base = -0.4f, Afina = 0.12f;
            Vector2 c = (min + max) * 0.5f;
            Vector2[] k = { new Vector2(min.x, min.y), new Vector2(max.x, min.y), new Vector2(max.x, max.y), new Vector2(min.x, max.y) };
            var lo = new Vector3[4];
            var hi = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector2 t = Vector2.Lerp(k[i], c, Afina);
                lo[i] = new Vector3(k[i].x, Base, k[i].y);
                hi[i] = new Vector3(t.x, h + rng.Faixa(-quebra, quebra * 0.6f), t.y);
            }
            Vector3 meio = new Vector3(c.x, h * 0.5f, c.y);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                Vector3 fora = (lo[i] + lo[j]) * 0.5f - meio;
                fora.y = 0f;
                b.Tri(lo[i], lo[j], hi[j], Tom(rng, escuro), fora);
                b.Tri(lo[i], hi[j], hi[i], Tom(rng, escuro), fora);
            }
            Vector3 cume = new Vector3(c.x, h + rng.Faixa(0f, quebra), c.y);
            for (int i = 0; i < 4; i++) b.Tri(hi[i], hi[(i + 1) % 4], cume, Color.Lerp(Tom(rng, escuro), Color.white, 0.1f), Vector3.up);
        }

        /// <summary>Rampa de rocha do Godot (fresta marrom-cinza -> crista cinza-fria), escurecida pelo dano.</summary>
        static Color Tom(Sorteio rng, float escuro) =>
            Relevo.Escurecer(Color.Lerp(new Color(0.33f, 0.30f, 0.28f), new Color(0.62f, 0.64f, 0.70f), rng.Float()), escuro);

        // ------------------------------------------------------------------ formas rasas

        /// <summary>Estrela irregular de 9 pontas (diametro 1): carvao le' como QUEIMADO, nao como tinta.</summary>
        static Mesh Mancha()
        {
            if (_mancha != null) return _mancha;
            const int n = 9;
            var rng = new Sorteio(TerrenoReativo.WallSeed + 11);
            var b = new MalhaProc.Construtor();
            var p = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * 2f * i / n;
                float r = 0.5f * (i % 2 == 0 ? rng.Faixa(0.85f, 1f) : rng.Faixa(0.55f, 0.75f));
                p[i] = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }
            for (int i = 0; i < n; i++) b.Tri(Vector3.zero, p[i], p[(i + 1) % n], Color.white, Vector3.up);
            return _mancha = b.ParaMesh("Carvao");
        }

        /// <summary>Caixa 1x1x1 com a base em y=0 (a laje de gelo cresce PARA CIMA da agua).</summary>
        static Mesh Laje()
        {
            if (_laje != null) return _laje;
            var b = new MalhaProc.Construtor();
            b.Caixa(new Vector3(0f, 0.5f, 0f), Vector3.one * 0.5f, Color.white);
            return _laje = b.ParaMesh("Gelo");
        }

        // ------------------------------------------------------------------ particulas

        ParticleSystem NovoFogo()
        {
            ParticleSystem ps = ParticulaVfx.Fogo(transform, "FogoCelula", 14f, true);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.scale = new Vector3(_cs * 0.6f, _cs * 0.6f, 0.3f);
            return ps;
        }

        ParticleSystem NovaFaisca()
        {
            ParticleSystem ps = ParticulaVfx.Novo(transform, "Faisca", Color.white, LeituraDoTerreno.CorDoEstado(EstadoCelula.Eletrificado), 20f,
                new Vector2(0.12f, 0.3f), new Vector2(2f, 5f), new Vector2(0.06f, 0.16f), true, 1f, 24);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.scale = new Vector3(_cs * 0.8f, _cs * 0.8f, 0.1f);
            return ps;
        }

        /// <summary>Um lote de instancing: uma forma, um material, as matrizes do frame.</summary>
        sealed class Lote
        {
            readonly Mesh _malha;
            readonly Material _mat;
            Matrix4x4[] _m = new Matrix4x4[64];
            public int N;

            public Lote(Mesh malha, Material mat) { _malha = malha; _mat = mat; }

            public void Add(Matrix4x4 m)
            {
                if (N == _m.Length) System.Array.Resize(ref _m, N * 2);
                _m[N++] = m;
            }

            public void Desenhar()
            {
                if (N == 0 || _mat == null || _malha == null) return;
                var rp = new RenderParams(_mat) { worldBounds = Mundo, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
                for (int i = 0; i < N; i += LeituraDoTerreno.PorLote)
                    Graphics.RenderMeshInstanced(rp, _malha, 0, _m, Mathf.Min(LeituraDoTerreno.PorLote, N - i), i);
            }
        }

        /// <summary>
        /// Um emissor por celula escolhida (a chave e' a celula: nada pula de lugar quando a lista muda). Quem sai da
        /// lista PARA de emitir e volta ao pool (a chama morre sozinha, sem piscar). O pool nunca passa do teto: so'
        /// cresce ate' o tamanho da maior lista escolhida.
        /// </summary>
        sealed class Emissores
        {
            readonly System.Func<ParticleSystem> _novo;
            readonly Dictionary<int, ParticleSystem> _porCelula = new Dictionary<int, ParticleSystem>();
            readonly Stack<ParticleSystem> _livres = new Stack<ParticleSystem>();
            readonly List<int> _soltar = new List<int>();

            public Emissores(System.Func<ParticleSystem> novo) { _novo = novo; }

            public void Atualizar(List<CelulaVisivel> escolhidas, VisualDoTerreno dono, bool naAgua)
            {
                _soltar.Clear();
                foreach (KeyValuePair<int, ParticleSystem> kv in _porCelula)
                    if (!Contem(escolhidas, kv.Key)) _soltar.Add(kv.Key);
                for (int i = 0; i < _soltar.Count; i++)
                {
                    ParticleSystem ps = _porCelula[_soltar[i]];
                    _porCelula.Remove(_soltar[i]);
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    _livres.Push(ps);
                }
                for (int i = 0; i < escolhidas.Count; i++)
                {
                    CelulaVisivel c = escolhidas[i];
                    if (_porCelula.ContainsKey(c.Idx)) continue;
                    ParticleSystem ps = _livres.Count > 0 ? _livres.Pop() : _novo();
                    Apoio a = dono.ApoioDe(c);
                    ps.transform.position = new Vector3(a.Chao.x, (naAgua ? a.Agua : a.Chao.y) + 0.15f, a.Chao.z);
                    ps.Play(true);
                    _porCelula[c.Idx] = ps;
                }
            }

            static bool Contem(List<CelulaVisivel> l, int idx)
            {
                for (int i = 0; i < l.Count; i++) if (l[i].Idx == idx) return true;
                return false;
            }
        }
    }
}
