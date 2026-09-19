using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// A SINTONIA NA TELA (GDD §9/§10) — so' LEITURA, nada aqui decide jogo: chega tudo pelo Bus e pela lista
    /// SintoniaEfeitos.Ativos. Tres momentos:
    /// CANALIZACAO — o telegrama que TODOS veem (e' a chance de interromper): fio de luz entre os dois conjuradores, passando
    /// pelo ponto, nas DUAS cores dos elementos + anel da area com o miolo que ENCHE em `duracao` (contagem sem numero).
    /// DISPARO — onda de choque na cor do combo + a FORMA propria de cada um dos 10, para reconhecer de longe no celular:
    /// FUNIL de fogo que anda (tornado) · METEOROS caindo numa POCA de lava (magma) · ESFERA que estoura (plasma) · CUPULA
    /// de vapor (cega de dentro) · ESTRELA de raios no chao (eletrocussao) · LAMA com ondas (lamacal) · COLUNA de chuva
    /// sob uma tampa escura (torrencial) · PAREDE de areia girando (areia) · ESPINHOS de cristal (minas) · NUVEM escura
    /// com sombra no alvo e raio que cai (nuvem).
    /// FALHOU — o anel RACHA em quatro, os cacos se abrem, caem e apagam.
    /// Leve: zero shader novo (MaterialVfx: URP Unlit/Particles), zero luz dinamica, pool por tipo, estouros de particula
    /// em QUATRO sistemas de mundo compartilhados (Emit com EmitParams: zero Instantiate), nada alocado por quadro; cor por
    /// MaterialPropertyBlock. Passos de 12 fps (look de anime). Nasce sob a arena; solta o Bus no OnDestroy.
    /// </summary>
    public sealed class VisualDaSintonia : MonoBehaviour
    {
        /// <summary>m acima dos pes onde o fio sai do conjurador (o peito: a mao que conjurou).</summary>
        const float AlturaFio = 1.2f;
        /// <summary>m acima do ponto onde o fio dobra (o no da fusao se le' por cima da cabeca do alvo).</summary>
        const float AlturaNo = 2.2f;
        /// <summary>s do anel rachando no "falhou".</summary>
        const float RachaS = 0.6f;
        /// <summary>s que a canalizacao espera pelo desfecho depois de cheia (evento perdido nao deixa anel preso na tela).</summary>
        const float EsperaS = 1f;
        /// <summary>m de altura da nuvem sobre o alvo e da tampa da chuva torrencial; de onde caem os meteoros.</summary>
        const float AlturaNuvem = 7f, AlturaTampa = 9f, AlturaMeteoro = 16f;
        const float PassoAnime = 1f / 12f;
        static readonly float[] Flicker = { 1f, 0.8f, 0.95f, 0.75f, 0.9f };

        // pool: 0..9 = o que FICA de cada combo; 10..19 = a forma do GOLPE (so' os instantaneos); 30/31 = onda/canal
        const int GOLPE = 10, ONDA = 30, CANAL = 31;
        const int RaiosEletro = 7, PontosRaio = 7;

        static int _idCor;
        static Material _mAlfa, _mAditivo, _mFaceta, _mPoeira;
        static Mesh _cristal, _arco;

        MaterialPropertyBlock _mpb;
        int _quadro;
        ParticleSystem _brasas, _faiscas, _poeira, _meteoros;
        readonly List<Canal> _canais = new List<Canal>();
        readonly List<Golpe> _golpes = new List<Golpe>();
        readonly Dictionary<SintoniaEfeitos.Persistente, Item> _vivos = new Dictionary<SintoniaEfeitos.Persistente, Item>();
        readonly List<SintoniaEfeitos.Persistente> _mortos = new List<SintoniaEfeitos.Persistente>();
        readonly Dictionary<int, Stack<Item>> _livres = new Dictionary<int, Stack<Item>>();

        sealed class Item
        {
            public int Tipo;
            public GameObject Go;
            public Renderer R, R2, R3;
            public ParticleSystem Ps, Ps2;
            public LineRenderer Linha;
            public LineRenderer[] Raios;
            public Renderer[] Arcos;
            public Vector3[] Pontos;
            public int Quadro;
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

        sealed class Golpe
        {
            public ComboSintonia Combo;
            public Vector3 Pos;
            public float Raio, Dur, Resta;
            public Item Onda, Forma;
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
            if (_idCor == 0) _idCor = Shader.PropertyToID("_BaseColor");
            if (_mAlfa == null)
            {
                _mAlfa = MaterialVfx.Solido(Color.white, MaterialVfx.Mistura.Alfa, true);        // dos dois lados: a camera entra na cupula
                _mAditivo = MaterialVfx.Solido(Color.white, MaterialVfx.Mistura.Aditivo, true);
                _mFaceta = MaterialVfx.CorDeVertice(MaterialVfx.Mistura.Opaco);                   // o cristal: faceta pela cor de vertice
                _mPoeira = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.PontoSuave());
            }
            // os ESTOUROS: quatro sistemas de MUNDO parados (taxa 0) servem os 10 combos; a cor vem no EmitParams
            _brasas = Estouro("SintoniaBrasas", new Vector2(0.6f, 1.2f), new Vector2(3f, 8f), new Vector2(0.2f, 0.5f), 0.5f, 220, null);
            Gravidade(_brasas, -0.3f);
            _faiscas = Estouro("SintoniaFaiscas", new Vector2(0.15f, 0.4f), new Vector2(8f, 18f), new Vector2(0.06f, 0.15f), 1f, 320, null);
            Esticar(_faiscas, 0.04f);
            _poeira = Estouro("SintoniaPoeira", new Vector2(0.9f, 1.8f), new Vector2(2f, 6f), new Vector2(1.2f, 2.6f), 0.6f, 220, _mPoeira);
            Crescer(_poeira, 0.6f, 1.6f);
            _meteoros = Estouro("SintoniaMeteoros", new Vector2(0.55f, 0.75f), new Vector2(22f, 30f), new Vector2(0.35f, 0.7f), 0.03f, 90, null);
            _meteoros.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // +Z da caixa = PARA BAIXO: caem
            Esticar(_meteoros, 0.08f);
            ParticleSystem.MainModule mm = _meteoros.main;
            mm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.35f), new Color(1f, 0.3f, 0.05f));
            _brasas.Play(); _faiscas.Play(); _poeira.Play(); _meteoros.Play();
        }

        void OnEnable() { Ligar(); }
        void OnDisable() { Desligar(); }

        void OnDestroy()
        {
            Desligar();
            _canais.Clear(); _golpes.Clear(); _vivos.Clear(); _livres.Clear();
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
            _quadro++;
            Canais(dt);
            Golpes(dt);
            Persistentes();
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
                Resta = Mathf.Max(duracao, 0.01f), CorA = Projetil.Tint(x), CorB = Projetil.Tint(y), It = Pegar(CANAL),
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
            k.It.R.enabled = k.It.R2.enabled = k.It.Linha.enabled = false;
            k.It.Ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            for (int i = 0; i < k.It.Arcos.Length; i++) k.It.Arcos[i].enabled = true;
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

        void Canais(float dt)
        {
            int passo = Passo();
            float f = Flicker[passo % Flicker.Length];
            for (int i = _canais.Count - 1; i >= 0; i--)
            {
                Canal k = _canais[i];
                Item it = k.It;
                Vector3 chao = NoChao(k.Ponto);
                it.Go.transform.position = chao + Vector3.up * 0.12f;
                if (k.Falhou)
                {
                    k.Racha -= dt;
                    if (k.Racha <= 0f) { Devolver(it); _canais.RemoveAt(i); continue; }
                    float u = 1f - k.Racha / RachaS;
                    Color cinza = Color.Lerp(k.CorA, new Color(0.45f, 0.42f, 0.5f), u);
                    for (int j = 0; j < it.Arcos.Length; j++)
                    {
                        Transform t = it.Arcos[j].transform;
                        Quaternion q = Quaternion.Euler(0f, j * 90f, 0f);
                        Vector3 fora = q * new Vector3(0.7071f, 0f, 0.7071f);
                        // os cacos se ABREM, caem e giram em passos (quebra de anime, nao lerp macio)
                        t.localPosition = fora * (k.Raio * 0.35f * u) + Vector3.down * (0.6f * u * u);
                        t.localRotation = q * Quaternion.Euler(Mathf.Floor(u * 6f) * 7f * (j % 2 == 0 ? 1f : -1f), 0f, 0f);
                        t.localScale = Vector3.one * k.Raio;
                        Pintar(it.Arcos[j], Hdr(cinza, 1.4f), (1f - u) * f);
                    }
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
                it.R.transform.localScale = Vector3.one * k.Raio;
                it.R2.transform.localScale = Vector3.one * Mathf.Max(k.Raio * prog, 0.05f);
                Pintar(it.R, Hdr(k.CorA, 1.5f), (passo & 1) == 0 ? 0.95f : 0.6f);   // a borda PULSA: le-se de canto de olho
                Pintar(it.R2, k.CorB, 0.4f);
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
            var g = new Golpe { Combo = c, Pos = p, Raio = r, Dur = DuracaoDoGolpe(c), Onda = Pegar(ONDA) };
            g.Resta = g.Dur;
            if (c == ComboSintonia.ExplosaoDePlasma || c == ComboSintonia.Eletrocussao || c == ComboSintonia.TempestadeTorrencial)
                g.Forma = Pegar(GOLPE + (int)c);
            _golpes.Add(g);
            Color cc = CorDoCombo(c);
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante: Emitir(_brasas, p, 50, new Color(1f, 0.7f, 0.25f), r * 0.3f); break;
                case ComboSintonia.ChuvaDeMagma:
                    ParticleSystem.ShapeModule sm = _meteoros.shape;
                    sm.scale = new Vector3(r * 1.6f, r * 1.6f, 0.1f);
                    var em = new ParticleSystem.EmitParams { position = p + Vector3.up * AlturaMeteoro, applyShapeToPosition = true };
                    _meteoros.Emit(em, 45);
                    Emitir(_brasas, p, 40, new Color(1f, 0.45f, 0.1f), r * 0.5f);
                    break;
                case ComboSintonia.ExplosaoDePlasma: Emitir(_faiscas, p + Vector3.up, 90, cc, 0.5f); break;
                case ComboSintonia.CortinaDeVapor: Emitir(_poeira, p, 50, new Color(0.95f, 0.97f, 1f, 0.7f), r * 0.6f); break;
                case ComboSintonia.Eletrocussao: Emitir(_faiscas, p, 60, cc, r * 0.3f); break;
                case ComboSintonia.Lamacal: Emitir(_poeira, p, 36, new Color(0.42f, 0.31f, 0.2f, 0.9f), r * 0.5f); break;
                case ComboSintonia.TempestadeTorrencial: Emitir(_poeira, p, 40, new Color(0.6f, 0.82f, 1f, 0.55f), r * 0.7f); break;
                case ComboSintonia.TempestadeDeAreia: Emitir(_poeira, p, 60, new Color(0.82f, 0.68f, 0.45f, 0.75f), r * 0.7f); break;
                case ComboSintonia.CristaisCarregados: Emitir(_faiscas, p, 50, cc, r * 0.4f); break;
                case ComboSintonia.NuvemTempestuosa: Emitir(_faiscas, p + Vector3.up, 40, cc, 0.5f); break;
            }
        }

        /// <summary>s do golpe na tela: a onda de todos; plasma e eletrocussao duram o que a regra dura (A15); a chuva, o
        /// tempo de a tampa escurecer e escorrer. Game feel, nao regra.</summary>
        static float DuracaoDoGolpe(ComboSintonia c)
        {
            switch (c)
            {
                case ComboSintonia.ExplosaoDePlasma: return Mathf.Max(Balance.Sintonia.Duracao(c), 0.5f);
                case ComboSintonia.Eletrocussao: return Mathf.Max(Balance.Sintonia.Duracao(c), 0.9f);
                case ComboSintonia.TempestadeTorrencial: return 1.8f;
            }
            return 0.9f;
        }

        void Golpes(float dt)
        {
            int passo = Passo();
            float f = Flicker[passo % Flicker.Length];
            for (int i = _golpes.Count - 1; i >= 0; i--)
            {
                Golpe g = _golpes[i];
                g.Resta -= dt;
                if (g.Resta <= 0f)
                {
                    Devolver(g.Onda);
                    if (g.Forma != null) Devolver(g.Forma);
                    _golpes.RemoveAt(i);
                    continue;
                }
                float u = 1f - g.Resta / g.Dur;
                float sai = 1f - (1f - Mathf.Min(u * 1.6f, 1f)) * (1f - Mathf.Min(u * 1.6f, 1f));   // abre rapido e para
                // ONDA DE CHOQUE: toda fusao — a area inteira que o combo pegou, na cor dele
                g.Onda.Go.transform.position = g.Pos + Vector3.up * 0.15f;
                g.Onda.R.transform.localScale = Vector3.one * (g.Raio * Mathf.Lerp(0.15f, 1f, sai));
                Pintar(g.Onda.R, Hdr(CorDoCombo(g.Combo), 1.8f), 1f - u);
                if (g.Forma == null) continue;
                Item it = g.Forma;
                it.Go.transform.position = g.Pos;
                switch (g.Combo)
                {
                    case ComboSintonia.ExplosaoDePlasma:
                        // ESFERA de plasma: estufa ate' passar do raio e some — branca no miolo, ouro na borda (bloom)
                        it.R.transform.localPosition = Vector3.up * 1f;
                        it.R.transform.localScale = Vector3.one * (g.Raio * 1.3f * sai);
                        Pintar(it.R, Hdr(CorDoCombo(g.Combo), 2.6f), (1f - u) * f);
                        it.R2.transform.localPosition = Vector3.up * 1f;
                        it.R2.transform.localScale = Vector3.one * (g.Raio * 0.7f * sai);
                        Pintar(it.R2, Hdr(Color.white, 2.2f), (1f - u) * (1f - u));
                        break;
                    case ComboSintonia.Eletrocussao:
                        // ESTRELA de raios no chao (a poca conduz) sobre um disco de agua; o risco muda a cada passo de anime
                        it.R.transform.localPosition = Vector3.up * 0.08f;
                        it.R.transform.localScale = Vector3.one * g.Raio;
                        Pintar(it.R, CorDoCombo(g.Combo), 0.35f * (1f - u) * f);
                        for (int j = 0; j < it.Raios.Length; j++)
                        {
                            LineRenderer l = it.Raios[j];
                            float a = (j * 360f / it.Raios.Length + passo * 11f) * Mathf.Deg2Rad;
                            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                            float alcance = g.Raio * (0.7f + 0.3f * Flicker[(passo + j) % Flicker.Length]);
                            Ziguezague(it.Pontos, g.Pos + Vector3.up * 0.2f, g.Pos + dir * alcance + Vector3.up * 0.2f, 0.9f, passo * 7 + j * 13);
                            l.SetPositions(it.Pontos);
                            l.widthMultiplier = 0.25f * f;
                            Color k = new Color(0.8f, 0.96f, 1f);
                            k.a = 1f - u * u;
                            l.startColor = k;
                            k.a *= 0.3f;
                            l.endColor = k;
                        }
                        break;
                    case ComboSintonia.TempestadeTorrencial:
                        // COLUNA DE CHUVA: tampa escura no alto + chuva densa caindo dela (a onda azul no chao e' o empurrao)
                        it.R.transform.localPosition = Vector3.up * AlturaTampa;
                        it.R.transform.localScale = Vector3.one * g.Raio;
                        Pintar(it.R, new Color(0.22f, 0.3f, 0.42f), 0.8f * (1f - u * u * u));
                        ParticleSystem.ShapeModule sh = it.Ps.shape;
                        sh.scale = new Vector3(g.Raio * 1.4f, g.Raio * 1.4f, 0.1f);
                        Taxa(it.Ps, 520f * (1f - u));
                        break;
                }
            }
        }

        // ------------------------------------------------------------------ o que fica

        void Persistentes()
        {
            IReadOnlyList<SintoniaEfeitos.Persistente> ativos = SintoniaEfeitos.Ativos;
            int passo = Passo();
            for (int i = 0; i < ativos.Count; i++)
            {
                SintoniaEfeitos.Persistente p = ativos[i];
                if (!(p.Restante > 0f)) continue;
                Item it;
                if (!_vivos.TryGetValue(p, out it)) { it = Pegar((int)p.Combo); _vivos[p] = it; }
                it.Quadro = _quadro;
                Desenhar(p, it, passo);
            }
            _mortos.Clear();
            foreach (KeyValuePair<SintoniaEfeitos.Persistente, Item> kv in _vivos) if (kv.Value.Quadro != _quadro) _mortos.Add(kv.Key);
            for (int i = 0; i < _mortos.Count; i++)
            {
                SintoniaEfeitos.Persistente p = _mortos[i];
                if (p.Quebrou)
                {
                    // a MINA estourou: estilhaco de cristal + faisca, no mundo (sobrevive ao item voltar ao pool)
                    Vector3 q = NoChao(p.Pos) + Vector3.up * 0.8f;
                    Emitir(_faiscas, q, 45, CorDoCombo(p.Combo), 0.4f);
                    Emitir(_brasas, q, 18, new Color(1f, 0.9f, 0.45f), 0.3f);
                }
                Devolver(_vivos[p]);
                _vivos.Remove(p);
            }
        }

        void Desenhar(SintoniaEfeitos.Persistente p, Item it, int passo)
        {
            float e = LeituraDosKits.EscalaPorRestante(p.Restante, p.Duracao);
            float f = Flicker[passo % Flicker.Length];
            Vector3 chao = NoChao(p.Pos);
            Transform t = it.Go.transform;
            float r = p.Raio;
            switch (p.Combo)
            {
                case ComboSintonia.TornadoFlamejante:
                    // FUNIL de fogo girando + redemoinho de poeira no pe' + o anel da SUCCAO (a area que puxa)
                    t.position = chao;
                    it.R.transform.localPosition = Vector3.up * 0.1f;
                    it.R.transform.localRotation = Quaternion.Euler(0f, passo * 15f, 0f);
                    it.R.transform.localScale = Vector3.one * r;
                    Pintar(it.R, CorDoCombo(p.Combo), 0.3f * e);
                    Taxa(it.Ps, 120f * e);
                    Taxa(it.Ps2, 45f * e);
                    break;
                case ComboSintonia.ChuvaDeMagma:
                    // POCA de lava: disco brilhando em passos + borda acesa + brasas subindo
                    t.position = chao + Vector3.up * 0.06f;
                    it.R.transform.localScale = Vector3.one * r;
                    Pintar(it.R, new Color(1f, 0.32f, 0.06f), 0.85f * e * f);
                    it.R2.transform.localScale = Vector3.one * r;
                    Pintar(it.R2, Hdr(new Color(1f, 0.7f, 0.2f), 1.8f), e);
                    Caixa(it.Ps, r * 1.4f, 0.1f);
                    Taxa(it.Ps, r * r * 1.1f * e);
                    break;
                case ComboSintonia.CortinaDeVapor:
                    // CUPULA branca (dos dois lados: quem esta' dentro ve' so' vapor — a cegueira e' esta) + nuvens subindo
                    t.position = chao;
                    it.R.transform.localScale = new Vector3(r * 2f, r * 1.2f, r * 2f);
                    Pintar(it.R, new Color(0.93f, 0.96f, 1f), 0.5f * e);
                    Caixa(it.Ps, r * 1.3f, r * 0.4f);
                    Taxa(it.Ps, 28f * e);
                    break;
                case ComboSintonia.Lamacal:
                    // LAMA fosca + ONDA que abre do centro em passos + bolhas estourando
                    t.position = chao + Vector3.up * 0.05f;
                    it.R.transform.localScale = Vector3.one * r;
                    Pintar(it.R, new Color(0.36f, 0.26f, 0.15f), 0.9f * e);
                    float onda = (passo % 18) / 18f;
                    it.R2.transform.localPosition = Vector3.up * 0.03f;
                    it.R2.transform.localScale = Vector3.one * (r * Mathf.Lerp(0.2f, 0.95f, onda));
                    Pintar(it.R2, new Color(0.62f, 0.5f, 0.34f), 0.8f * (1f - onda) * e);
                    Caixa(it.Ps, r * 1.3f, 0.05f);
                    Taxa(it.Ps, r * 3f * e);
                    break;
                case ComboSintonia.TempestadeDeAreia:
                    // PAREDE de areia girando no raio + cupula baixa de poeira (de dentro, a areia cega)
                    t.position = chao;
                    it.R.transform.localScale = new Vector3(r * 2f, r * 0.9f, r * 2f);
                    Pintar(it.R, new Color(0.8f, 0.66f, 0.42f), 0.38f * e);
                    ParticleSystem.ShapeModule sa = it.Ps.shape;
                    sa.radius = r;
                    Taxa(it.Ps, 110f * e);
                    break;
                case ComboSintonia.CristaisCarregados:
                    // ESPINHO de cristal cravado e tombado (cada um para um lado), pulsando carga + faisca estalando
                    t.position = chao;
                    int h = Mathf.Abs(Mathf.RoundToInt(p.Pos.x * 7f + p.Pos.z * 13f));
                    it.R.transform.localRotation = Quaternion.Euler(12f + h % 14, h % 360, 0f);
                    it.R.transform.localScale = new Vector3(0.6f, 1.9f * Mathf.Lerp(0.3f, 1f, e), 0.6f);
                    Pintar(it.R, Hdr(CorDoCombo(p.Combo), 1.3f + 0.5f * Flicker[(passo + h) % Flicker.Length]), 1f);
                    Taxa(it.Ps, 10f * e);
                    break;
                case ComboSintonia.NuvemTempestuosa:
                    // NUVEM escura sobre o alvo + SOMBRA no chao onde o raio cai + o RAIO logo apos cada descarga
                    Vector3 alto = p.Pos + Vector3.up * AlturaNuvem;
                    t.position = alto;
                    t.rotation = Quaternion.Euler(0f, passo * 6f, 0f);
                    it.R.transform.localScale = new Vector3(4.2f, 1.6f, 4.2f);
                    it.R2.transform.localScale = new Vector3(2.8f, 1.3f, 2.8f);
                    Pintar(it.R, new Color(0.24f, 0.26f, 0.34f), 0.92f * e);
                    Pintar(it.R2, new Color(0.34f, 0.36f, 0.46f), 0.92f * e);
                    it.R3.transform.position = chao + Vector3.up * 0.08f;
                    it.R3.transform.rotation = Quaternion.identity;
                    it.R3.transform.localScale = Vector3.one * SintoniaEfeitos.NuvemAlcanceM;
                    Pintar(it.R3, new Color(0.1f, 0.1f, 0.18f), 0.45f * e);
                    bool raio = p.DesdeADescarga < 0.25f;
                    it.Linha.enabled = raio;
                    if (raio)
                    {
                        Ziguezague(it.Pontos, alto + Vector3.down * 0.6f, p.Pos + Vector3.up * 0.9f, 0.7f, passo * 5);
                        it.Linha.SetPositions(it.Pontos);
                        it.Linha.widthMultiplier = 0.35f * f;
                        Color k = CorDoCombo(p.Combo);
                        it.Linha.startColor = k;
                        it.Linha.endColor = k;
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ pool

        Item Pegar(int tipo)
        {
            Stack<Item> s;
            Item it = _livres.TryGetValue(tipo, out s) && s.Count > 0 ? s.Pop() : Novo(tipo);
            Mostrar(it, true);
            return it;
        }

        void Devolver(Item it)
        {
            if (it == null) return;
            Mostrar(it, false);
            Stack<Item> s;
            if (!_livres.TryGetValue(it.Tipo, out s)) { s = new Stack<Item>(); _livres[it.Tipo] = s; }
            s.Push(it);
        }

        static void Mostrar(Item it, bool on)
        {
            if (it.R != null) it.R.enabled = on;
            if (it.R2 != null) it.R2.enabled = on;
            if (it.R3 != null) it.R3.enabled = on;
            if (it.Linha != null) it.Linha.enabled = on && it.Tipo != (int)ComboSintonia.NuvemTempestuosa;   // o raio da nuvem acende so' na descarga
            if (it.Raios != null) for (int i = 0; i < it.Raios.Length; i++) it.Raios[i].enabled = on;
            if (it.Arcos != null) for (int i = 0; i < it.Arcos.Length; i++) it.Arcos[i].enabled = false;   // so' o "falhou" liga
            Tocar(it.Ps, on);
            Tocar(it.Ps2, on);
        }

        static void Tocar(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            if (on) ps.Play(true);
            else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);   // a chama que ja' saiu morre sozinha
        }

        Item Novo(int tipo)
        {
            var it = new Item { Tipo = tipo, Go = new GameObject("Sintonia_" + tipo) };
            Transform raiz = it.Go.transform;
            raiz.SetParent(transform, false);
            switch (tipo)
            {
                case CANAL:
                    it.Linha = Linha(it.Go, 0.16f);
                    it.Pontos = new Vector3[3];
                    it.Linha.positionCount = 3;
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mAditivo);
                    it.R2 = Peca(raiz, MalhaVfx.Disco(), _mAlfa);
                    it.R.transform.localPosition = Vector3.up * 0.02f;   // a borda por cima do miolo
                    it.Arcos = new Renderer[4];
                    for (int i = 0; i < 4; i++) it.Arcos[i] = Peca(raiz, Arco(), _mAditivo);
                    // motas SUBINDO do anel nas duas cores (a cor vem por canal): a fusao juntando forca
                    it.Ps = ParticulaVfx.Novo(raiz, "Motas", Color.white, Color.white, 30f, new Vector2(0.5f, 1f),
                        new Vector2(1.5f, 3.5f), new Vector2(0.08f, 0.2f), true, 0.1f, 90);
                    Anel(it.Ps, 0f);
                    break;
                case ONDA:
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mAditivo);
                    break;
                case GOLPE + (int)ComboSintonia.ExplosaoDePlasma:
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mAditivo);
                    it.R2 = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mAditivo);
                    break;
                case GOLPE + (int)ComboSintonia.Eletrocussao:
                    it.R = Peca(raiz, MalhaVfx.Disco(), _mAditivo);
                    it.Pontos = new Vector3[PontosRaio];
                    it.Raios = new LineRenderer[RaiosEletro];
                    for (int i = 0; i < RaiosEletro; i++)
                    {
                        var go = new GameObject("Raio");
                        go.transform.SetParent(raiz, false);
                        it.Raios[i] = Linha(go, 0.25f);
                        it.Raios[i].positionCount = PontosRaio;
                    }
                    break;
                case GOLPE + (int)ComboSintonia.TempestadeTorrencial:
                    it.R = Peca(raiz, MalhaVfx.Disco(), _mAlfa);
                    // a CHUVA: riscos azuis caindo da tampa (a caixa gira para +Z apontar para baixo)
                    it.Ps = ParticulaVfx.Novo(raiz, "Chuva", new Color(0.75f, 0.9f, 1f), new Color(0.45f, 0.7f, 1f), 0f,
                        new Vector2(0.35f, 0.45f), new Vector2(20f, 26f), new Vector2(0.05f, 0.09f), true, 0f, 500);
                    it.Ps.transform.localPosition = Vector3.up * AlturaTampa;
                    it.Ps.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Esticar(it.Ps, 0.06f);
                    break;
                case (int)ComboSintonia.TornadoFlamejante:
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mAlfa);
                    // FUNIL: cone estreito que sobe girando e ABRE no alto (tamanho cresce com a vida)
                    it.Ps = ParticulaVfx.Novo(raiz, "Funil", new Color(1f, 0.88f, 0.35f), new Color(1f, 0.33f, 0.07f), 120f,
                        new Vector2(0.9f, 1.3f), new Vector2(5f, 7.5f), new Vector2(0.5f, 1.1f), false, 0f, 200);
                    ParticleSystem.ShapeModule sf = it.Ps.shape;
                    sf.shapeType = ParticleSystemShapeType.Cone;
                    sf.angle = 20f;
                    sf.radius = 0.5f;
                    Girar(it.Ps, 5f);
                    Crescer(it.Ps, 0.5f, 1.7f);
                    it.Ps2 = ParticulaVfx.Novo(raiz, "Redemoinho", new Color(0.75f, 0.95f, 0.85f, 0.6f), new Color(0.55f, 0.8f, 0.7f, 0.45f), 45f,
                        new Vector2(0.6f, 1f), new Vector2(0.3f, 1f), new Vector2(0.8f, 1.6f), false, 0f, 70);
                    ParticleSystem.ShapeModule sr = it.Ps2.shape;
                    sr.shapeType = ParticleSystemShapeType.Circle;
                    sr.radius = 2.2f;
                    Girar(it.Ps2, 3.5f);
                    it.Ps2.GetComponent<ParticleSystemRenderer>().sharedMaterial = _mPoeira;
                    break;
                case (int)ComboSintonia.ChuvaDeMagma:
                    it.R = Peca(raiz, MalhaVfx.Disco(), _mAlfa);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mAditivo);
                    it.R2.transform.localPosition = Vector3.up * 0.02f;
                    it.Ps = ParticulaVfx.Fogo(raiz, "Brasas", 40f, true);
                    ParticleSystem.MainModule mb = it.Ps.main;
                    mb.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
                    mb.maxParticles = 140;
                    break;
                case (int)ComboSintonia.CortinaDeVapor:
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mAlfa);
                    it.Ps = Nevoa(raiz, "Vapor", new Color(0.97f, 0.98f, 1f, 0.55f), new Color(0.85f, 0.9f, 0.95f, 0.4f), new Vector2(2.5f, 4.5f));
                    break;
                case (int)ComboSintonia.Lamacal:
                    it.R = Peca(raiz, MalhaVfx.Disco(), _mAlfa);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mAlfa);
                    it.Ps = ParticulaVfx.Novo(raiz, "Bolhas", new Color(0.55f, 0.42f, 0.28f, 0.9f), new Color(0.38f, 0.28f, 0.18f, 0.85f), 20f,
                        new Vector2(0.4f, 0.8f), new Vector2(0.6f, 1.6f), new Vector2(0.18f, 0.42f), false, 0.25f, 70);
                    Gravidade(it.Ps, 0.6f);
                    it.Ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _mPoeira;
                    break;
                case (int)ComboSintonia.TempestadeDeAreia:
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mAlfa);
                    it.Ps = Nevoa(raiz, "Areia", new Color(0.86f, 0.72f, 0.48f, 0.6f), new Color(0.7f, 0.56f, 0.36f, 0.5f), new Vector2(1.4f, 2.6f));
                    Anel(it.Ps, 0.15f);   // a PAREDE: so' a borda do disco
                    ParticleSystem.MainModule ma = it.Ps.main;
                    ma.maxParticles = 240;
                    Girar(it.Ps, 0.8f);
                    break;
                case (int)ComboSintonia.CristaisCarregados:
                    it.R = Peca(raiz, Cristal(), _mFaceta);
                    it.Ps = ParticulaVfx.Novo(raiz, "Carga", new Color(1f, 1f, 0.8f), new Color(1f, 0.85f, 0.25f), 10f,
                        new Vector2(0.1f, 0.25f), new Vector2(1f, 3f), new Vector2(0.05f, 0.12f), false, 1f, 20);
                    ParticleSystem.ShapeModule sk = it.Ps.shape;
                    sk.scale = new Vector3(0.5f, 0.5f, 1.6f);
                    it.Ps.transform.localPosition = Vector3.up * 0.8f;
                    break;
                case (int)ComboSintonia.NuvemTempestuosa:
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mAlfa);
                    it.R2 = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mAlfa);
                    it.R2.transform.localPosition = new Vector3(1.3f, 0.35f, 0.6f);
                    it.R3 = Peca(raiz, MalhaVfx.Disco(), _mAlfa);   // a SOMBRA no chao (mundo: a casca a poe fora da raiz)
                    it.Linha = Linha(it.Go, 0.35f);
                    it.Pontos = new Vector3[PontosRaio];
                    it.Linha.positionCount = PontosRaio;
                    break;
            }
            return it;
        }

        // ------------------------------------------------------------------ fabrica

        /// <summary>Estouro PARADO (taxa 0) de MUNDO: serve varios combos, cor por EmitParams.</summary>
        ParticleSystem Estouro(string nome, Vector2 vida, Vector2 vel, Vector2 tam, float aleatorio, int max, Material mat)
        {
            ParticleSystem ps = ParticulaVfx.Novo(transform, nome, Color.white, Color.white, 0f, vida, vel, tam, true, aleatorio, max);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            if (mat != null) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
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

        /// <summary>Nevoa em ALFA (vapor, areia): baforadas grandes que sobem devagar e abrem.</summary>
        ParticleSystem Nevoa(Transform raiz, string nome, Color a, Color b, Vector2 tam)
        {
            ParticleSystem ps = ParticulaVfx.Novo(raiz, nome, a, b, 28f, new Vector2(1.6f, 3f), new Vector2(0.3f, 1f), tam, false, 0.3f, 110);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _mPoeira;
            Crescer(ps, 0.6f, 1.5f);
            return ps;
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

        /// <summary>Caixa de emissao quadrada de lado `lado` no chao, `altura` na vertical (o Novo gira -90 em X).</summary>
        static void Caixa(ParticleSystem ps, float lado, float altura)
        {
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.scale = new Vector3(lado, lado, altura);
        }

        /// <summary>Orbita em volta do +Z LOCAL (= o cima do mundo, pelo -90 do Novo): o funil e a parede de areia GIRAM.</summary>
        static void Girar(ParticleSystem ps, float radPorS)
        {
            ParticleSystem.VelocityOverLifetimeModule v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.Local;
            v.x = v.y = v.z = new ParticleSystem.MinMaxCurve(0f);
            v.orbitalX = v.orbitalY = new ParticleSystem.MinMaxCurve(0f);
            v.orbitalZ = new ParticleSystem.MinMaxCurve(radPorS);
        }

        static void Crescer(ParticleSystem ps, float de, float para)
        {
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, de, 1f, para));
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

        /// <summary>
        /// Espinho de cristal (bipiramide de 6 lados, 1 de altura, pe' enterrado), gerado 1x. FACETA pela cor de vertice:
        /// faces de cima alternando claro/medio, de baixo escuras — a cor chapada lia "cone", nao cristal.
        /// </summary>
        static Mesh Cristal()
        {
            if (_cristal != null) return _cristal;
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
            return _cristal = b.ParaMesh("VfxCristal");
        }

        /// <summary>Um QUARTO do anel (raio 1, 10% de espessura): os cacos do "falhou". Gerado 1x.</summary>
        static Mesh Arco()
        {
            if (_arco != null) return _arco;
            const int lados = 10;
            const float r0 = 0.9f;
            var b = new MalhaProc.Construtor();
            for (int i = 0; i < lados; i++)
            {
                float a0 = Mathf.PI * 0.5f * i / lados, a1 = Mathf.PI * 0.5f * (i + 1) / lados;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                b.Tri(d0 * r0, d0, d1, Color.white, Vector3.up);
                b.Tri(d0 * r0, d1, d1 * r0, Color.white, Vector3.up);
            }
            return _arco = b.ParaMesh("VfxArco");
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
                uint h = (uint)(semente * 73856093) ^ (uint)(i * 19349663);
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                float s = ((h & 1023) / 1023f * 2f - 1f) * desvio * Mathf.Sin(t * Mathf.PI);
                saida[i] = a + d * t + lado * s + Vector3.up * (s * 0.3f);
            }
            saida[0] = a;
            saida[n - 1] = b;
        }

        /// <summary>A cor de cada combo (paleta do Elements.luau do Roblox, PONTE A15) — a da onda de choque, do plasma e do raio.</summary>
        static Color CorDoCombo(ComboSintonia c)
        {
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante: return new Color32(0xFF, 0x8A, 0x3D, 255);
                case ComboSintonia.ChuvaDeMagma: return new Color32(0xFF, 0x45, 0x00, 255);
                case ComboSintonia.ExplosaoDePlasma: return new Color32(0xFF, 0xD9, 0x8A, 255);
                case ComboSintonia.CortinaDeVapor: return new Color32(0xE8, 0xF4, 0xFF, 255);
                case ComboSintonia.Eletrocussao: return new Color32(0x9B, 0xE7, 0xFF, 255);
                case ComboSintonia.Lamacal: return new Color32(0x9B, 0x7A, 0x4E, 255);   // o #6B5433 do Roblox some no aditivo
                case ComboSintonia.TempestadeTorrencial: return new Color32(0x7F, 0xD4, 0xFF, 255);
                case ComboSintonia.TempestadeDeAreia: return new Color32(0xC9, 0xA9, 0x6A, 255);
                case ComboSintonia.CristaisCarregados: return new Color32(0xD9, 0xC4, 0x6A, 255);
                default: return new Color32(0xCF, 0xE8, 0xF5, 255);
            }
        }

        /// <summary>Cor por objeto sem material novo (MaterialPropertyBlock): o alfa multiplica.</summary>
        void Pintar(Renderer r, Color c, float alfa)
        {
            if (r == null) return;
            c.a *= alfa;
            _mpb.SetColor(_idCor, c);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Cor acima de 1 (acende no bloom) com o alfa intacto.</summary>
        static Color Hdr(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        static int Passo() => (int)(Time.time / PassoAnime);

        static Vector3 NoChao(Vector3 p) => new Vector3(p.x, Ilha.AlturaDoChao(p.x, p.z), p.z);

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
