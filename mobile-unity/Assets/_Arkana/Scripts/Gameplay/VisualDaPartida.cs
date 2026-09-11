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
    }

    public static class BauVisual
    {
        public const string MODELO = "bau";
        /// <summary>m — maior lado do bau (Godot: 1,15 m de largura; a canalizacao nao muda com a arte).</summary>
        public const float TAMANHO = 1.15f;
        public const float RAIO_FEIXE = 1.2f;

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
        public const float ALTURA_ANEL = 1.2f, LARGURA_ANEL = 1.2f;
        public const int PONTOS_ANEL = 96;
        /// <summary>Repeticoes da textura na volta e na altura (inteiras: a costura em u=1 fecha sem rasgo).</summary>
        public const float REPETE_U = 24f, REPETE_V = 3f;

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
    }

    /// <summary>
    /// A CASCA que desenha a partida: loot no chao, Bau Celestial e a Tempestade — tudo lido da Partida no
    /// LateUpdate (estado depois do Tick), nada decidido aqui. Sem colisor em nada, sem sombra nos efeitos,
    /// material um por cor em cache estatico. Fiacao defensiva: modelo ausente vira primitiva, sem log.
    /// </summary>
    public sealed class VisualDaPartida : MonoBehaviour
    {
        const string SHADER_UNLIT = "Universal Render Pipeline/Unlit";
        const string SHADER_PARTICULA = "Universal Render Pipeline/Particles/Unlit";

        Partida _partida;
        readonly List<VisualLoot> _loots = new List<VisualLoot>();

        GameObject _parede;
        LineRenderer _anelZona;
        Vector3 _anelCentro;
        float _anelRaio = -1f;

        GameObject _bau;
        Transform _bauPivo, _marcaBau;
        TrailRenderer _rastro;
        LineRenderer _progresso;
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

        /// <summary>Um item no chao. Re-veste so' quando o item MUDA (troca no chao, lista que andou), nunca por frame.</summary>
        sealed class VisualLoot
        {
            readonly GameObject _raiz;
            readonly Transform _pivo;
            readonly MeshRenderer _marca, _coluna;
            readonly MeshFilter _marcaMf;
            readonly MeshRenderer[] _nucleos = new MeshRenderer[2];
            readonly Dictionary<string, GameObject> _modelos = new Dictionary<string, GameObject>();
            string _id;
            int _nEls = -1;   // -1: a primeira passada sempre veste
            Elemento _e0, _e1;
            bool _temPos;
            Vector3 _pos;

            public VisualLoot(Transform pai)
            {
                _raiz = new GameObject("Loot");
                _raiz.transform.SetParent(pai, false);
                _pivo = new GameObject("Pivo").transform;
                _pivo.SetParent(_raiz.transform, false);
                _marca = Malha(_raiz.transform, "Marca", null, null);
                _marca.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                _marca.transform.localScale = Vector3.one * LootVisual.RAIO_MARCA;
                _marcaMf = _marca.GetComponent<MeshFilter>();
                _coluna = Malha(_raiz.transform, "Coluna", MalhaColuna(), null);
                for (int k = 0; k < 2; k++)
                {
                    _nucleos[k] = MaterialMago.Primitivo(_pivo, "Nucleo" + k, PrimitiveType.Sphere, Vector3.zero,
                        Vector3.one * LootVisual.NUCLEO_M, null).GetComponent<MeshRenderer>();
                    SemSombra(_nucleos[k]);
                }
            }

            public void Esconder() => Ativo(_raiz, false);

            public void Mostrar(LootItem item, float t)
            {
                // campos crus, nao item.Elementos: aquele aloca array por chamada
                Elemento[] par = item.Par ?? Array.Empty<Elemento>();
                int n = par.Length > 0 ? Mathf.Min(par.Length, 2) : (item.ElementoDaLuva.HasValue ? 1 : 0);
                Elemento e0 = par.Length > 0 ? par[0] : item.ElementoDaLuva.GetValueOrDefault();
                Elemento e1 = par.Length > 1 ? par[1] : e0;
                if (item.ArmaId != _id || n != _nEls || e0 != _e0 || e1 != _e1) Vestir(item.ArmaId, n, e0, e1);
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
                Ativo(_raiz, true);
            }

            void Vestir(string id, int n, Elemento e0, Elemento e1)
            {
                _id = id; _nEls = n; _e0 = e0; _e1 = e1;
                // um modelo por armaId, criado uma vez e religado: pegar de maos nuas faz a lista andar
                string chave = id ?? "";
                GameObject modelo;
                if (!_modelos.TryGetValue(chave, out modelo) || modelo == null)
                {
                    modelo = Modelo(LootVisual.ModeloDe(id), _pivo, LootVisual.TAMANHO_LUVA, false);
                    if (modelo == null)
                    {
                        // sem o .glb: a luva procedural da mao, ampliada (o que se ve' e' o que se equipa)
                        modelo = LuvaVisual.Criar(_pivo, id, new[] { e0 });
                        modelo.transform.localScale = Vector3.one * 2f;
                    }
                    _modelos[chave] = modelo;
                }
                foreach (KeyValuePair<string, GameObject> kv in _modelos)
                    if (kv.Value != null) Ativo(kv.Value, kv.Value == modelo);

                Color cor = Arma.Cor(id);
                _marcaMf.sharedMesh = MalhaMarca(Arma.Forma(id));
                _marca.sharedMaterial = Opaco(cor);
                float h = LootVisual.AlturaDaColuna(Arma.Raridade(id));
                Ativo(_coluna.gameObject, h > 0f);
                if (h > 0f)
                {
                    _coluna.transform.localScale = new Vector3(LootVisual.RAIO_COLUNA, h, LootVisual.RAIO_COLUNA);
                    _coluna.sharedMaterial = Coluna(ComAlfa(cor, 0.45f));
                }
                for (int k = 0; k < 2; k++)
                {
                    bool liga = k < n;
                    Ativo(_nucleos[k].gameObject, liga);
                    if (!liga) continue;
                    // o ELEMENTO se le' no nucleo (cor ja' pelo filtro de daltonismo); manopla mostra o PAR
                    _nucleos[k].sharedMaterial = Opaco(Arkana.Menu.Estilo.CorElemento(k == 0 ? e0 : e1));
                    float x = n == 2 ? (k == 0 ? -0.1f : 0.1f) : 0f;
                    _nucleos[k].transform.localPosition = new Vector3(x, LootVisual.NUCLEO_Y, 0f);
                }
            }
        }

        // ---------------------------------------------------------------- bau celestial

        void MontarBau()
        {
            Color ouro = Arma.Cor(Arma.MANOPLA);   // a cor da LENDARIA, a mesma da manopla que esta' dentro
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
            _rastro = _bauPivo.gameObject.AddComponent<TrailRenderer>();
            _rastro.time = 1.2f;
            _rastro.startWidth = 0.9f;
            _rastro.endWidth = 0f;
            _rastro.minVertexDistance = 0.5f;
            _rastro.emitting = false;
            _rastro.sharedMaterial = Veu(ComAlfa(ouro, 0.7f));
            SemSombra(_rastro);
            // o feixe e' o farol: do anuncio ate' abrir, denuncia o ponto e quem esta' la' (Godot: fica aceso no pouso)
            MeshRenderer feixe = Malha(_bau.transform, "Feixe", MalhaColuna(), Coluna(ComAlfa(ouro, 0.35f)));
            feixe.transform.localScale = new Vector3(BauVisual.RAIO_FEIXE, BauCelestial.ALTURA_QUEDA, BauVisual.RAIO_FEIXE);
            // a MARCA tem o raio de abrir: mostra ONDE ficar para canalizar
            _marcaBau = Malha(_bau.transform, "Marca", MalhaMarca(48, 0.88f), Opaco(ouro)).transform;
            _marcaBau.localPosition = new Vector3(0f, 0.08f, 0f);
            _progresso = Linha(_bau.transform, "Progresso", Veu(ouro), 0.35f);
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
            Ativo(_bau, telegrafo);
            if (f != _faseVista)
            {
                // BORDA de fase: o estouro sai uma vez (ele mora fora de _bau para sobreviver ao sumico)
                Color ouro = Arma.Cor(Arma.MANOPLA);
                if (f == BauCelestial.Fase.Caindo) _rastro.Clear();
                else if (f == BauCelestial.Fase.Pousado) Estourar(bau.Pos + new Vector3(0f, 0.4f, 0f), ouro, 26);
                else if (f == BauCelestial.Fase.Aberto) Estourar(bau.Pos + new Vector3(0f, 1f, 0f), ouro, 40);
                _faseVista = f;
            }
            if (!telegrafo) return;

            float t01 = f == BauCelestial.Fase.Caindo ? 1f - bau.Restante / BauCelestial.QUEDA_S : 1f;
            _bauPivo.position = BauVisual.PosNaQueda(bau.Pos, t01);
            _bauPivo.localRotation = Quaternion.Euler(0f, t * 40f, 0f);   // giro lento: le' como objeto magico
            _rastro.emitting = f == BauCelestial.Fase.Caindo;
            _marcaBau.localScale = Vector3.one * (BauCelestial.RAIO_ABRIR * (1f + 0.06f * Mathf.Sin(t * 5f)));
            float p = f == BauCelestial.Fase.Pousado ? bau.Progresso : 0f;
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
            _parede.SetActive(false);
            _anelZona = Linha(transform, "ProximoCirculo", Veu(ComAlfa(ZonaVisual.COR, 0.75f)), ZonaVisual.LARGURA_ANEL);
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
                if (m.HasProperty("_BaseMap"))
                    m.SetTextureOffset("_BaseMap", new Vector2(Mathf.Repeat(t * 0.004f, 1f), Mathf.Repeat(-t * 0.05f, 1f)));
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
            r.sharedMaterial = MaterialParticula();
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

        // ---------------------------------------------------------------- fabrica (malha, material, textura)

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
        static GameObject Modelo(string nome, Transform pai, float tamanho, bool peNoChao)
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

        static Mesh _malhaParede, _malhaColuna;
        static readonly Dictionary<Vector2, Mesh> _marcas = new Dictionary<Vector2, Mesh>();

        static Mesh MalhaParede()
        {
            if (_malhaParede == null) _malhaParede = MalhaDaParede.Cilindro(MalhaDaParede.SEGMENTOS, MalhaDaParede.ALTURA).ParaMesh("ParedeDaZona");
            return _malhaParede;
        }

        /// <summary>Coluna de luz = o mesmo cilindro de 2 faces, 8 lados, altura 1 (a casca escala).</summary>
        static Mesh MalhaColuna()
        {
            if (_malhaColuna == null) _malhaColuna = MalhaDaParede.Cilindro(8, 1f).ParaMesh("Coluna");
            return _malhaColuna;
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

        /// <summary>Contorno chato de poligono regular (raio 1, vertice para +z), virado para cima.</summary>
        static Mesh MalhaMarca(int lados, float furo)
        {
            var chave = new Vector2(lados, furo);
            Mesh m;
            if (_marcas.TryGetValue(chave, out m) && m != null) return m;
            var b = new MalhaProc.Construtor();   // Tri() orienta pela normal pedida: sem briga de winding
            for (int i = 0; i < lados; i++)
            {
                float a0 = Mathf.PI * 0.5f + Mathf.PI * 2f * i / lados;
                float a1 = Mathf.PI * 0.5f + Mathf.PI * 2f * (i + 1) / lados;
                var o0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var o1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                b.Tri(o0, o1, o1 * furo, Color.white, Vector3.up);
                b.Tri(o0, o1 * furo, o0 * furo, Color.white, Vector3.up);
            }
            m = b.ParaMesh("Marca" + lados);
            _marcas[chave] = m;
            return m;
        }

        static readonly Dictionary<Color, Material> _opacos = new Dictionary<Color, Material>();
        static readonly Dictionary<Color, Material> _veus = new Dictionary<Color, Material>();
        static readonly Dictionary<Color, Material> _colunas = new Dictionary<Color, Material>();
        static Material _matParede, _matParticula;

        static Material Opaco(Color c) => DoCache(_opacos, c, false, null);
        static Material Veu(Color c) => DoCache(_veus, c, true, null);
        static Material Coluna(Color c) => DoCache(_colunas, c, true, TexturaDegrade());

        static Material DoCache(Dictionary<Color, Material> cache, Color cor, bool transparente, Texture tex)
        {
            Material m;
            if (cache.TryGetValue(cor, out m) && m != null) return m;
            m = NovoMaterial(SHADER_UNLIT, transparente);
            MaterialMago.Pintar(m, cor);
            if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            cache[cor] = m;
            return m;
        }

        /// <summary>Um so' no processo: o offset de UV rola por frame nele (a parede "anda").</summary>
        static Material MaterialParede()
        {
            if (_matParede != null) return _matParede;
            _matParede = NovoMaterial(SHADER_UNLIT, true);
            _matParede.name = "ParedeDaZona";
            MaterialMago.Pintar(_matParede, ZonaVisual.COR);   // alfa 1: quem da' a transparencia e' a textura
            if (_matParede.HasProperty("_BaseMap"))
            {
                _matParede.SetTexture("_BaseMap", TexturaTempestade());
                _matParede.SetTextureScale("_BaseMap", new Vector2(ZonaVisual.REPETE_U, ZonaVisual.REPETE_V));
            }
            return _matParede;
        }

        static Material MaterialParticula()
        {
            if (_matParticula != null) return _matParticula;
            _matParticula = NovoMaterial(SHADER_PARTICULA, true);
            _matParticula.name = "Estouro";
            MaterialMago.Pintar(_matParticula, Color.white);   // a cor vem do startColor (cor de vertice)
            if (_matParticula.HasProperty("_BaseMap")) _matParticula.SetTexture("_BaseMap", TexturaPonto());
            return _matParticula;
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

        static Texture2D _texDegrade, _texTempestade, _texPonto;

        /// <summary>Coluna: alfa cheio no pe', some no alto — le' como feixe, nao como cano.</summary>
        static Texture2D TexturaDegrade()
        {
            if (_texDegrade == null)
                _texDegrade = Textura(4, 32, TextureWrapMode.Clamp, (x, y) => { float v = 1f - y / 31f; return v * v; });
            return _texDegrade;
        }

        /// <summary>Estrias diagonais em periodos INTEIROS da textura: repete sem emenda quando rola.</summary>
        static Texture2D TexturaTempestade()
        {
            if (_texTempestade == null)
                _texTempestade = Textura(64, 64, TextureWrapMode.Repeat, (x, y) =>
                {
                    float s1 = 0.5f + 0.5f * Mathf.Sin((3f * x + y) / 64f * Mathf.PI * 2f);
                    float s2 = 0.5f + 0.5f * Mathf.Sin((x - 2f * y) / 64f * Mathf.PI * 2f);
                    return 0.12f + 0.3f * s1 * s1 * s1 + 0.1f * s2 * s2;
                });
            return _texTempestade;
        }

        /// <summary>Particula redonda e macia (o quadrado branco padrao le' como erro).</summary>
        static Texture2D TexturaPonto()
        {
            if (_texPonto == null)
                _texPonto = Textura(32, 32, TextureWrapMode.Clamp, (x, y) =>
                {
                    float dx = (x + 0.5f) / 16f - 1f, dy = (y + 0.5f) / 16f - 1f;
                    float v = 1f - Mathf.Sqrt(dx * dx + dy * dy);
                    return v > 0f ? v * v : 0f;
                });
            return _texPonto;
        }

        static Texture2D Textura(int w, int h, TextureWrapMode wrap, Func<int, int, float> alfa)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = wrap;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa(x, y)) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);   // sobe e solta a copia da CPU: memoria de celular
            return tex;
        }
    }
}
