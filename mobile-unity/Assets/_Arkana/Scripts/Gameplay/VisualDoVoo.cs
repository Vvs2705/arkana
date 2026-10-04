using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Characters;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O VOO como espetaculo (o salto do onibus do Fortnite) — so' LEITURA, nada aqui decide jogo: a Queda e o Castelo
    /// seguem donos de tudo. Tres coisas:
    /// (1) CASTELO VIVO: circulo de runas girando sob a base e rochas em orbita lenta — FILHOS do castelo (somem com ele) —
    /// e o rastro por onde ele passa: esteira dourada e brasas caindo da base, de MUNDO (ficam para tras e apagam sozinhas
    /// quando o castelo vai embora), mais nuvens finas que ele atravessa.
    /// (2) QUEDA por ESTADO (Queda.Fase a cada quadro, de todo corpo da arena; nao ha' evento para perder): rastro fino nas
    /// maos e nos pes na cor do elemento do mago; no PLANEIO as maos viram FITAS e soltam brilho; e o VENTO passando pela
    /// camera do jogador, mais denso e mais rapido quanto maior a descida.
    /// (3) POUSO, na BORDA caindo/planando -> pousou: anel de poeira, onda no chao, faiscas e clarao nos pes.
    /// Um rastro por corpo criado no Start, sistemas de MUNDO com Emit em rajada: zero Instantiate por evento, zero lixo por
    /// quadro, zero luz dinamica. Nasce sob a arena (morre com ela). No treino (sem castelo) nao ha' voo: desliga.
    /// </summary>
    public sealed class VisualDoVoo : MonoBehaviour
    {
        // ------------------------------------------------------------------ KNOBS do castelo (por foto)
        /// <summary>Raio do circulo de runas como fracao da meia envergadura (35,8 m / 2): passa da base e aparece em volta
        /// dela visto do alto. m abaixo da base; graus/s do giro; HDR do dourado (acima de 1 o bloom acende).</summary>
        const float RunasRaio = 1.2f, RunasAbaixo = 2.5f, RunasGiro = 8f, BrilhoRunas = 2.2f;
        const int RochasN = 10;
        /// <summary>m do lado das rochas; graus/s da orbita (22 s de rota = meia volta: "devagar"); m acima da base.</summary>
        const float RochaMin = 1.3f, RochaMax = 3.6f, OrbitaGiro = 6f, RochasAcima = 9f;
        /// <summary>Brasas por segundo e o disco de onde caem (m de raio, m acima do fundo da base).</summary>
        const float BrasasPorSegundo = 80f, BrasaRaio = 8f, BrasaAcima = 5f;
        /// <summary>Esteira: m de largura na base, s de vida (x 34 m/s = ~80 m de rastro), alfa (aditivo sobre o HDR 1,8).</summary>
        const float EsteiraLargura = 15f, EsteiraS = 2.4f, EsteiraAlfa = 0.3f, EsteiraAcima = 5f;
        /// <summary>Nuvens finas por segundo (0 = sem nuvem) e onde nascem: m adiante na rota, m abaixo do eixo.</summary>
        const float NuvensPorSegundo = 5f, NuvemAdiante = 60f, NuvemAbaixo = 14f;

        // ------------------------------------------------------------------ KNOBS da queda
        /// <summary>Rastro fino (caindo): m de largura e s de vida (x 55 m/s = 22 m de risco). Fita (planando): idem.</summary>
        const float RastroFinoM = 0.07f, RastroFinoS = 0.4f, FitaM = 0.3f, FitaS = 0.9f;
        /// <summary>Brilhos por segundo POR MAO no planeio; alem disto (m) da camera nem nascem.</summary>
        const float BrilhosPorSegundo = 24f, BrilhoAlcance = 60f;
        /// <summary>Riscos de vento por segundo na terminal (escala com a DESCIDA: no planeio ~18%), velocidade deles como
        /// fracao da do corpo, comprimento do risco (s de velocidade), e a caixa: m adiante da camera, m no sentido do voo.</summary>
        const float VentoPorSegundo = 170f, VentoVel = 0.35f, VentoRisco = 0.14f, VentoAdiante = 9f, VentoAbaixo = 6f;

        // ------------------------------------------------------------------ KNOBS do pouso
        const int PoeiraN = 36, FaiscasN = 28;
        /// <summary>m do diametro final da onda no chao; alem disto (m) da camera o pouso de bot nao estala (so' conta).</summary>
        const float OndaM = 6.5f, AlcancePouso = 150f, BrilhoOnda = 1.6f;

        static readonly Color CorOuro = new Color(1f, 0.74f, 0.3f);
        static readonly Color CorBrasaA = new Color(1f, 0.84f, 0.42f), CorBrasaB = new Color(1f, 0.48f, 0.12f);
        static readonly Color CorVentoA = new Color(0.85f, 0.93f, 1f, 0.38f), CorVentoB = new Color(1f, 1f, 1f, 0.26f);
        static readonly Color CorNuvemA = new Color(1f, 0.96f, 0.9f, 0.16f), CorNuvemB = new Color(1f, 0.9f, 0.82f, 0.1f);
        static readonly Color CorPoeiraA = new Color(0.74f, 0.66f, 0.52f, 0.7f), CorPoeiraB = new Color(0.6f, 0.53f, 0.42f, 0.55f);
        /// <summary>Sem osso (mago procedural): o ponto no corpo, em metros locais do Pawn.</summary>
        static readonly Vector3 SemMaoE = new Vector3(-0.35f, 1.2f, 0f), SemMaoD = new Vector3(0.35f, 1.2f, 0f);
        static readonly Vector3 SemPeE = new Vector3(-0.12f, 0.1f, 0f), SemPeD = new Vector3(0.12f, 0.1f, 0f);

        static Material _mRunas, _mOnda, _mAlfa;
        static Mesh _quad;
        static int _idCor;
        static AnimationCurve _afina;

        Partida _partida;
        Castelo _castelo;
        MaterialPropertyBlock _mpb;
        bool _montouCastelo, _castEmitindo;
        float _baseY, _tempo;
        Renderer _rFora, _rDentro;
        Transform _orbita;
        readonly List<Transform> _rochas = new List<Transform>();
        readonly List<Vector3> _baseRocha = new List<Vector3>(), _giroRocha = new List<Vector3>();
        ParticleSystem _brasas, _nuvens, _vento, _brilhos, _poeira, _faiscas, _onda, _clarao;
        TrailRenderer _esteira;
        readonly List<Voo> _voos = new List<Voo>();
        ParticleSystem.EmitParams _ep;
        Vector3 _posAntes;
        bool _noArAntes;

        sealed class Voo
        {
            public Pawn P;
            public Transform MaoE, MaoD, PeE, PeD;   // null = sem osso: SemMao*/SemPe* no corpo
            public TrailRenderer RMaoE, RMaoD, RPeE, RPeD;
            public Color Cor;
            public string Fase;
            public bool Ativo, Fita;
            public float Brilho;
        }

        /// <summary>O castelo montou runas/rochas e segue no ceu (o teste e o diag da foto leem).</summary>
        public bool CasteloVivo => _montouCastelo && _castelo != null;
        /// <summary>A esteira e as brasas estao saindo do castelo AGORA.</summary>
        public bool RastroDoCastelo => _castEmitindo;
        public int Rochas => _rochas.Count;
        public int Brasas => _brasas != null ? _brasas.particleCount : 0;
        public int Vento => _vento != null ? _vento.particleCount : 0;
        /// <summary>Pousos estalados nesta partida (todo corpo) e onde foi o ultimo (na lamina, se foi no mar).</summary>
        public int Pousos { get; private set; }
        public Vector3 UltimoPouso { get; private set; }

        /// <summary>O rastro das maos deste corpo esta' riscando agora.</summary>
        public bool RastroAceso(Pawn p)
        {
            for (int i = 0; i < _voos.Count; i++) if (_voos[i].P == p) return _voos[i].Ativo && _voos[i].RMaoD.emitting;
            return false;
        }

        /// <summary>Nasce sob a arena (morre com ela). `castelo` null = treino: ninguem voa, o componente desliga.</summary>
        public static VisualDoVoo Criar(Transform arena, Partida partida, Castelo castelo)
        {
            var go = new GameObject("VisualDoVoo");
            if (arena != null) go.transform.SetParent(arena, false);
            var v = go.AddComponent<VisualDoVoo>();
            v._partida = partida;
            v._castelo = castelo;
            return v;
        }

        /// <summary>No Start (nao no Awake): o Criar ja' entregou castelo e partida, e todo corpo ja' esta' na arena.</summary>
        void Start()
        {
            Partida p = _partida ?? Partida.Atual;
            if (_castelo == null || p == null) { enabled = false; return; }
            _mpb = new MaterialPropertyBlock();
            if (_idCor == 0) _idCor = Shader.PropertyToID("_BaseColor");
            if (_mRunas == null)
            {
                _mRunas = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Aditivo, true, TexturaRunas());
                _mOnda = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, new Color(BrilhoOnda, BrilhoOnda, BrilhoOnda, 1f), MaterialVfx.Mistura.Aditivo, true, TexturaOnda());
                _mAlfa = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.PontoSuave());
                _quad = QuadXY();
                _afina = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            }
            MontarRastroDoCastelo();
            MontarVento();
            MontarPouso();
            for (int i = 0; i < p.Arena.Count; i++)
            {
                Pawn pw = p.Arena[i] as Pawn;
                if (pw != null) _voos.Add(NovoVoo(pw));
            }
        }

        void LateUpdate()
        {
            if (_brasas == null) return;   // o Start ainda nao montou
            float dt = Time.deltaTime;
            _tempo += dt;
            Camera cam = Camera.main;
            TickCastelo();
            TickVoos(dt, cam);
            TickVento(dt, cam);
        }

        // ------------------------------------------------------------------ castelo vivo

        void TickCastelo()
        {
            bool vivo = _castelo != null;
            // so' depois do Start do castelo (o modelo e' o primeiro filho): filho pendurado antes faria o castelo pular o
            // MontarVisual, que so' roda com childCount 0
            if (vivo && !_montouCastelo && _castelo.transform.childCount > 0) MontarCastelo();
            if (!_montouCastelo) return;
            if (vivo != _castEmitindo)
            {
                _castEmitindo = vivo;
                ParticleSystem.EmissionModule eb = _brasas.emission;
                eb.rateOverTime = vivo ? BrasasPorSegundo : 0f;
                ParticleSystem.EmissionModule en = _nuvens.emission;
                en.rateOverTime = vivo ? NuvensPorSegundo : 0f;
                _esteira.emitting = vivo;   // foi embora: o que ja' saiu apaga sozinho, nada some de estalo
            }
            if (!vivo) return;
            Transform c = _castelo.transform;
            Vector3 fundo = c.position + Vector3.up * _baseY;
            _brasas.transform.position = fundo + Vector3.up * BrasaAcima;
            _esteira.transform.position = fundo + Vector3.up * EsteiraAcima;
            _nuvens.transform.position = c.position + _castelo.Rota.Direcao * NuvemAdiante + Vector3.down * NuvemAbaixo;
            // runas: o quad XY deita (90 em X) e gira em Y; o de dentro contra, mais rapido; o dourado respira
            _rFora.transform.localRotation = Quaternion.Euler(90f, _tempo * RunasGiro, 0f);
            _rDentro.transform.localRotation = Quaternion.Euler(90f, -_tempo * RunasGiro * 1.7f, 0f);
            float k = BrilhoRunas * (0.85f + 0.15f * Mathf.Sin(_tempo * 2.8f));
            _mpb.Clear();
            _mpb.SetColor(_idCor, new Color(CorOuro.r * k, CorOuro.g * k, CorOuro.b * k, 1f));
            _rFora.SetPropertyBlock(_mpb);
            _rDentro.SetPropertyBlock(_mpb);
            _orbita.localRotation = Quaternion.Euler(0f, _tempo * OrbitaGiro, 0f);
            for (int i = 0; i < _rochas.Count; i++)
            {
                Vector3 b = _baseRocha[i];
                b.y += Mathf.Sin(_tempo * 0.7f + i * 1.9f) * 0.6f;   // cada uma boia no seu compasso
                _rochas[i].localPosition = b;
                _rochas[i].localRotation = Quaternion.Euler(_giroRocha[i] * _tempo);
            }
        }

        /// <summary>Mede a base NO MODELO (so' malhas: nada meu pendurado ainda) e pendura runas e rochas no castelo.</summary>
        void MontarCastelo()
        {
            _montouCastelo = true;
            Transform c = _castelo.transform;
            Bounds b = new Bounds(c.position, Vector3.zero);
            bool tem = false;
            foreach (Renderer r in c.GetComponentsInChildren<Renderer>())
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                if (!tem) { b = r.bounds; tem = true; } else b.Encapsulate(r.bounds);
            }
            // o castelo so' gira em Y: a altura dos limites de mundo e' a do modelo. Sem malha, a conta do Godot (52 m de altura)
            _baseY = Mathf.Clamp(tem ? b.min.y - c.position.y : -26f, -60f, -5f);
            float meia = Castelo.Envergadura * 0.5f;

            var runas = new GameObject("VooRunas").transform;
            runas.SetParent(c, false);
            runas.localPosition = new Vector3(0f, _baseY - RunasAbaixo, 0f);
            _rFora = Peca(runas, "Fora", _quad, _mRunas);
            _rFora.transform.localScale = Vector3.one * (meia * RunasRaio * 2f);
            _rDentro = Peca(runas, "Dentro", _quad, _mRunas);
            _rDentro.transform.localPosition = new Vector3(0f, -1.2f, 0f);
            _rDentro.transform.localScale = Vector3.one * (meia * RunasRaio * 1.1f);

            _orbita = new GameObject("VooRochas").transform;
            _orbita.SetParent(c, false);
            _orbita.localPosition = new Vector3(0f, _baseY + RochasAcima, 0f);
            Mesh malha;
            Material[] mats;
            MoldeDaRocha(out malha, out mats);
            Vector3 tm = malha.bounds.size;
            float lado = Mathf.Max(Mathf.Max(tm.x, tm.y), Mathf.Max(tm.z, 0.01f));
            var rng = new Sorteio(77);
            for (int i = 0; i < RochasN; i++)
            {
                float a = (i + rng.Faixa(-0.3f, 0.3f)) * Mathf.PI * 2f / RochasN;
                float d = meia * rng.Faixa(0.9f, 1.25f);
                var pos = new Vector3(Mathf.Cos(a) * d, rng.Faixa(-7f, 7f), Mathf.Sin(a) * d);
                float s = rng.Faixa(RochaMin, RochaMax) / lado;
                var go = new GameObject("Rocha", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(_orbita, false);
                go.transform.localPosition = pos;
                go.transform.localScale = new Vector3(s, s * rng.Faixa(0.6f, 0.9f), s);   // lasca, nao bola
                go.GetComponent<MeshFilter>().sharedMesh = malha;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                _rochas.Add(go.transform);
                _baseRocha.Add(pos);
                _giroRocha.Add(new Vector3(rng.Faixa(-18f, 18f), rng.Faixa(-30f, 30f), rng.Faixa(-18f, 18f)));
            }

            // os de MUNDO ja' no lugar: a esteira riscaria da origem da arena ate' o castelo
            Vector3 fundo = c.position + Vector3.up * _baseY;
            _brasas.transform.position = fundo + Vector3.up * BrasaAcima;
            _esteira.transform.position = fundo + Vector3.up * EsteiraAcima;
            _esteira.Clear();
            _nuvens.transform.rotation = Quaternion.LookRotation(-_castelo.Rota.Direcao, Vector3.up);   // o vento contra a proa
        }

        /// <summary>A rocha da Meshy dos pedregulhos (malha e material do molde, sem instanciar o prefab); sem ela, esfera.</summary>
        static void MoldeDaRocha(out Mesh malha, out Material[] mats)
        {
            GameObject g = Resources.Load<GameObject>(Vegetacao.RochaDoPedregulho);
            MeshFilter mf = g != null ? g.GetComponentInChildren<MeshFilter>(true) : null;
            MeshRenderer mr = g != null ? g.GetComponentInChildren<MeshRenderer>(true) : null;
            if (mf != null && mf.sharedMesh != null && mr != null) { malha = mf.sharedMesh; mats = mr.sharedMaterials; return; }
            malha = MalhaVfx.Primitiva(PrimitiveType.Sphere);   // ponytail: esfera amassada se a rocha da Meshy faltar
            mats = new[] { Ilha.MaterialPedra() };
        }

        void MontarRastroDoCastelo()
        {
            // BRASAS: chuva dourada que sai da base (+Z do emissor virado para BAIXO), fica no mundo e o castelo deixa para tras
            _brasas = ParticulaVfx.Novo(transform, "VooBrasas", CorBrasaA, CorBrasaB, 0f, new Vector2(2.4f, 4f),
                new Vector2(1.5f, 5f), new Vector2(0.45f, 1.1f), true, 0.15f, 360);
            _brasas.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ParticleSystem.MainModule mb = _brasas.main;
            mb.gravityModifier = 0.12f;
            ParticleSystem.ShapeModule sb = _brasas.shape;
            sb.shapeType = ParticleSystemShapeType.Cone;
            sb.angle = 30f;
            sb.radius = BrasaRaio;
            sb.radiusThickness = 1f;

            // NUVENS finas: fiapos em ALFA esticados no vento (Stretch com a velocidade contra a proa), que o castelo atravessa
            _nuvens = ParticulaVfx.Novo(transform, "VooNuvens", CorNuvemA, CorNuvemB, 0f, new Vector2(6f, 8f),
                new Vector2(1f, 3f), new Vector2(14f, 26f), true, 0f, 48);
            ParticleSystem.ShapeModule sn = _nuvens.shape;
            sn.scale = new Vector3(150f, 36f, 120f);   // o emissor olha o vento: x de lado, y de pe', z ao longo da rota
            var rn = _nuvens.GetComponent<ParticleSystemRenderer>();
            rn.sharedMaterial = _mAlfa;
            rn.renderMode = ParticleSystemRenderMode.Stretch;
            rn.lengthScale = 2.8f;
            rn.velocityScale = 0f;
            ParticleSystem.ColorOverLifetimeModule cn = _nuvens.colorOverLifetime;
            cn.color = new ParticleSystem.MinMaxGradient(Esvai(0.25f, 0.7f));   // entra e sai macio: nuvem nao pisca
            ParticleSystem.SizeOverLifetimeModule zn = _nuvens.sizeOverLifetime;
            zn.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.85f, 1f, 1.2f));

            // ESTEIRA: a faixa dourada larga que marca por onde o castelo passou (a faixa macia do DeLinha, HDR aditivo)
            var ge = new GameObject("VooEsteira");
            ge.transform.SetParent(transform, false);
            _esteira = ge.AddComponent<TrailRenderer>();
            _esteira.time = EsteiraS;
            _esteira.minVertexDistance = 1.5f;
            _esteira.widthMultiplier = EsteiraLargura;
            _esteira.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.15f);
            _esteira.numCapVertices = 2;
            _esteira.alignment = LineAlignment.View;
            _esteira.sharedMaterial = MaterialVfx.DeLinha();
            _esteira.shadowCastingMode = ShadowCastingMode.Off;
            _esteira.receiveShadows = false;
            _esteira.startColor = new Color(CorOuro.r, CorOuro.g, CorOuro.b, EsteiraAlfa);
            _esteira.endColor = new Color(CorBrasaB.r, CorBrasaB.g, CorBrasaB.b, 0f);
            _esteira.emitting = false;
            _brasas.Play();
            _nuvens.Play();
        }

        // ------------------------------------------------------------------ queda: rastro, fita e brilho

        Voo NovoVoo(Pawn p)
        {
            var v = new Voo { P = p, Fase = p.Queda.Fase };
            v.Cor = Projetil.Tint(IdentidadeMago.De(p.Slug).Elemento);
            Transform raiz = p.Visual != null ? p.Visual.transform : p.transform;
            // os 20 FBX da Meshy trazem LeftHand/RightHand/LeftFoot/RightFoot; uma passada so' por corpo, no Start
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (v.MaoE == null && n.EndsWith("LeftHand", StringComparison.Ordinal)) v.MaoE = t;
                else if (v.MaoD == null && n.EndsWith("RightHand", StringComparison.Ordinal)) v.MaoD = t;
                else if (v.PeE == null && n.EndsWith("LeftFoot", StringComparison.Ordinal)) v.PeE = t;
                else if (v.PeD == null && n.EndsWith("RightFoot", StringComparison.Ordinal)) v.PeD = t;
            }
            if (v.MaoD == null && p.Visual != null) v.MaoD = p.Visual.MaoDireita;   // o procedural so' expoe a direita
            v.RMaoE = Rastro("VooMaoE"); v.RMaoD = Rastro("VooMaoD");
            v.RPeE = Rastro("VooPeE"); v.RPeD = Rastro("VooPeD");
            Vestir(v.RMaoE, v.Cor, false); Vestir(v.RMaoD, v.Cor, false);
            Vestir(v.RPeE, v.Cor, false); Vestir(v.RPeD, v.Cor, false);
            return v;
        }

        TrailRenderer Rastro(string nome)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<TrailRenderer>();
            t.minVertexDistance = 0.3f;
            t.widthCurve = _afina;
            t.numCapVertices = 2;
            t.alignment = LineAlignment.View;
            t.sharedMaterial = MaterialVfx.DeLinha();
            t.shadowCastingMode = ShadowCastingMode.Off;
            t.receiveShadows = false;
            t.emitting = false;
            return t;
        }

        /// <summary>Fino (caindo) ou FITA (planando): largura, vida e a cabeca puxada para o branco (acende no bloom).</summary>
        static void Vestir(TrailRenderer t, Color cor, bool fita)
        {
            t.time = fita ? FitaS : RastroFinoS;
            t.widthMultiplier = fita ? FitaM : RastroFinoM;
            Color c = Color.Lerp(cor, Color.white, fita ? 0.45f : 0.25f);
            c.a = fita ? 1f : 0.85f;
            t.startColor = c;
            t.endColor = new Color(cor.r, cor.g, cor.b, 0f);
        }

        void TickVoos(float dt, Camera cam)
        {
            Vector3 olho = cam != null ? cam.transform.position : Vector3.zero;
            for (int i = 0; i < _voos.Count; i++)
            {
                Voo v = _voos[i];
                if (v.P == null) continue;
                string f = v.P.Queda.Fase;
                bool noAr = f == Queda.CAINDO || f == Queda.PLANANDO;
                if (noAr)
                {
                    Seguir(v);
                    if (!v.Ativo)
                    {
                        // saiu do portao: o risco comeca AQUI (Clear), nao de onde o objeto estava parado
                        v.Ativo = true;
                        v.RMaoE.Clear(); v.RMaoD.Clear(); v.RPeE.Clear(); v.RPeD.Clear();
                        v.RMaoE.emitting = v.RMaoD.emitting = true;
                        Modo(v, f == Queda.PLANANDO);
                    }
                    else if ((f == Queda.PLANANDO) != v.Fita) Modo(v, f == Queda.PLANANDO);
                    if (v.Fita && cam != null && (v.P.Pos - olho).sqrMagnitude < BrilhoAlcance * BrilhoAlcance) Brilhar(v, dt);
                }
                else if (v.Ativo)
                {
                    v.Ativo = false;   // o que ja' riscou apaga pela vida do rastro
                    v.RMaoE.emitting = v.RMaoD.emitting = v.RPeE.emitting = v.RPeD.emitting = false;
                }
                if (f == Queda.POUSOU && (v.Fase == Queda.CAINDO || v.Fase == Queda.PLANANDO)) Pousar(v, cam);
                v.Fase = f;
            }
        }

        /// <summary>No planeio as FITAS das maos sao a estrela: os pes param de riscar (quatro fitas liam como lula).</summary>
        static void Modo(Voo v, bool fita)
        {
            v.Fita = fita;
            Vestir(v.RMaoE, v.Cor, fita);
            Vestir(v.RMaoD, v.Cor, fita);
            v.RPeE.emitting = v.RPeD.emitting = !fita;
        }

        static void Seguir(Voo v)
        {
            v.RMaoE.transform.position = Ponto(v.MaoE, v.P, SemMaoE);
            v.RMaoD.transform.position = Ponto(v.MaoD, v.P, SemMaoD);
            v.RPeE.transform.position = Ponto(v.PeE, v.P, SemPeE);
            v.RPeD.transform.position = Ponto(v.PeD, v.P, SemPeD);
        }

        static Vector3 Ponto(Transform osso, Pawn p, Vector3 semOsso) => osso != null ? osso.position : p.transform.TransformPoint(semOsso);

        /// <summary>O leve brilho do planeio: motas na cor do elemento soltas das maos, que ficam no ar atras da fita.</summary>
        void Brilhar(Voo v, float dt)
        {
            v.Brilho = Mathf.Min(v.Brilho + dt * BrilhosPorSegundo, 4f);   // quadro longo nao despeja rajada
            Color32 c = Color.Lerp(v.Cor, Color.white, 0.35f);
            while (v.Brilho >= 1f)
            {
                v.Brilho -= 1f;
                Soltar(v.RMaoE.transform.position, c);
                Soltar(v.RMaoD.transform.position, c);
            }
        }

        void Soltar(Vector3 pos, Color32 c)
        {
            _ep.position = pos;
            _ep.startColor = c;
            _ep.velocity = UnityEngine.Random.insideUnitSphere * 0.7f;
            _brilhos.Emit(_ep, 1);
        }

        // ------------------------------------------------------------------ queda: o vento na camera

        void MontarVento()
        {
            // riscos de MUNDO numa caixa adiante e abaixo da camera, correndo CONTRA o voo: a camera cai por eles (paralaxe de
            // verdade) e o Stretch os estica pela velocidade — o "vento" do mergulho de todo battle royale
            _vento = ParticulaVfx.Novo(transform, "VooVento", CorVentoA, CorVentoB, 0f, new Vector2(0.18f, 0.32f),
                new Vector2(10f, 20f), new Vector2(0.03f, 0.07f), true, 0f, 96);
            ParticleSystem.ShapeModule sv = _vento.shape;
            sv.scale = new Vector3(18f, 12f, 10f);   // x de lado, y adiante da camera, z no sentido do voo
            var rv = _vento.GetComponent<ParticleSystemRenderer>();
            rv.renderMode = ParticleSystemRenderMode.Stretch;
            rv.velocityScale = VentoRisco;
            rv.lengthScale = 2f;
            ParticleSystem.SizeOverLifetimeModule zv = _vento.sizeOverLifetime;
            zv.enabled = false;   // risco nao afina: entra e sai pelo alfa
            ParticleSystem.ColorOverLifetimeModule cv = _vento.colorOverLifetime;
            cv.color = new ParticleSystem.MinMaxGradient(Esvai(0.2f, 0.6f));

            _brilhos = ParticulaVfx.Novo(transform, "VooBrilhos", Color.white, Color.white, 0f, new Vector2(0.35f, 0.7f),
                Vector2.zero, new Vector2(0.09f, 0.2f), true, 0f, 240);
            ParticleSystem.ShapeModule sh = _brilhos.shape;
            sh.enabled = false;   // nasce onde o Emit manda (a mao)
            _vento.Play();
            _brilhos.Play();
        }

        void TickVento(float dt, Camera cam)
        {
            Partida pt = _partida ?? Partida.Atual;
            Pawn eu = pt != null ? pt.Player as Pawn : null;
            bool noAr = eu != null && eu.Queda.NoAr && eu.Queda.Fase != Queda.NO_CASTELO;
            float taxa = 0f;
            if (noAr && _noArAntes && cam != null && dt > 0f)
            {
                Vector3 vel = (eu.Pos - _posAntes) / dt;   // o movimento de verdade do corpo (queda + deriva do joystick)
                float mv = vel.magnitude;
                if (mv > 1f)
                {
                    Vector3 dir = vel / mv;
                    Vector3 frente = cam.transform.forward;
                    frente.y = 0f;
                    frente = frente.sqrMagnitude > 1e-4f ? frente.normalized : Vector3.forward;
                    Vector3 dica = Mathf.Abs(Vector3.Dot(dir, frente)) > 0.95f ? Vector3.up : frente;
                    // a caixa fica ADIANTE da camera (debaixo dela a camera, que olha 31 graus para baixo, nao ve') e no
                    // sentido do voo; o +Z do emissor e' o ar correndo contra o corpo
                    _vento.transform.SetPositionAndRotation(cam.transform.position + frente * VentoAdiante + dir * VentoAbaixo,
                        Quaternion.LookRotation(-dir, dica));
                    taxa = VentoPorSegundo * Mathf.Clamp01(eu.Queda.Vy / Queda.VEL_QUEDA);
                    ParticleSystem.MainModule mv2 = _vento.main;
                    mv2.startSpeed = new ParticleSystem.MinMaxCurve(mv * VentoVel * 0.7f, mv * VentoVel * 1.3f);
                }
            }
            ParticleSystem.EmissionModule em = _vento.emission;
            em.rateOverTime = taxa;
            _noArAntes = noAr;
            if (eu != null) _posAntes = eu.Pos;
        }

        // ------------------------------------------------------------------ pouso

        void MontarPouso()
        {
            // POEIRA: anel deitado (o circulo do Novo e' o chao) que abre em nuvem de terra, em ALFA, freando no arrasto
            _poeira = ParticulaVfx.Novo(transform, "VooPoeira", CorPoeiraA, CorPoeiraB, 0f, new Vector2(0.6f, 1.1f),
                new Vector2(3.5f, 7f), new Vector2(0.6f, 1.2f), true, 0f, 160);
            ParticleSystem.MainModule mp = _poeira.main;
            mp.gravityModifier = -0.04f;
            ParticleSystem.ShapeModule sp = _poeira.shape;
            sp.shapeType = ParticleSystemShapeType.Circle;
            sp.radius = 0.35f;
            sp.radiusThickness = 0f;
            _poeira.GetComponent<ParticleSystemRenderer>().sharedMaterial = _mAlfa;
            ParticleSystem.SizeOverLifetimeModule zp = _poeira.sizeOverLifetime;
            zp.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.7f));   // a nuvem ABRE
            ParticleSystem.LimitVelocityOverLifetimeModule lp = _poeira.limitVelocityOverLifetime;
            lp.enabled = true;
            lp.limit = 100f;
            lp.dampen = 0f;
            lp.drag = 4f;   // 6 m/s param em ~1,5 m: o anel fica nos pes
            lp.multiplyDragByParticleSize = false;
            lp.multiplyDragByParticleVelocity = false;

            // FAISCAS: o estalo, riscos rapidos num cone para CIMA que voltam ao chao
            _faiscas = ParticulaVfx.Novo(transform, "VooFaiscas", Color.white, Color.white, 0f, new Vector2(0.35f, 0.7f),
                new Vector2(4f, 9f), new Vector2(0.05f, 0.11f), true, 0f, 200);
            ParticleSystem.MainModule mf = _faiscas.main;
            mf.gravityModifier = 1.1f;
            ParticleSystem.ShapeModule sf = _faiscas.shape;
            sf.shapeType = ParticleSystemShapeType.Cone;
            sf.angle = 55f;
            sf.radius = 0.3f;
            sf.radiusThickness = 1f;
            var rf = _faiscas.GetComponent<ParticleSystemRenderer>();
            rf.renderMode = ParticleSystemRenderMode.Stretch;
            rf.velocityScale = 0.04f;
            rf.lengthScale = 1f;

            // ONDA: UMA particula deitada (HorizontalBillboard) com a textura de aro, que abre rapido e apaga
            _onda = ParticulaVfx.Novo(transform, "VooOnda", Color.white, Color.white, 0f, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1f, 1f), true, 0f, 24);
            ParticleSystem.ShapeModule so = _onda.shape;
            so.enabled = false;
            var ro = _onda.GetComponent<ParticleSystemRenderer>();
            ro.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            ro.sharedMaterial = _mOnda;
            ParticleSystem.SizeOverLifetimeModule zo = _onda.sizeOverLifetime;
            zo.size = new ParticleSystem.MinMaxCurve(OndaM, new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(0.35f, 0.72f), new Keyframe(1f, 1f)));

            // CLARAO macio nos pes: UMA particula de ponto que abre e some
            _clarao = ParticulaVfx.Novo(transform, "VooClarao", Color.white, Color.white, 0f, new Vector2(0.16f, 0.16f),
                Vector2.zero, new Vector2(2.4f, 2.4f), true, 0f, 24);
            ParticleSystem.ShapeModule sc = _clarao.shape;
            sc.enabled = false;
            ParticleSystem.SizeOverLifetimeModule zc = _clarao.sizeOverLifetime;
            zc.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.2f));
            _poeira.Play(); _faiscas.Play(); _onda.Play(); _clarao.Play();
        }

        /// <summary>O estalo do pouso nos pes de `v`. No mar o estalo sobe para a lamina (o corpo pousou no fundo e sobe nadando).
        /// ponytail: anel sempre deitado reto; em encosta forte ele corta o chao — deitar na normal se a foto pedir.</summary>
        void Pousar(Voo v, Camera cam)
        {
            Vector3 pe = v.P.Pos;
            IRelevo r = Ilha.Atual != null ? Ilha.Atual.Chao : null;   // Chao: a ilha do Documento Mestre deixa o Relevo antigo nulo
            if (r != null) pe.y = Mathf.Max(pe.y, r.SuperficieDaAgua(pe.x, pe.z));
            Pousos++;
            UltimoPouso = pe;
            if (cam != null && (cam.transform.position - pe).sqrMagnitude > AlcancePouso * AlcancePouso) return;
            Color claro = Color.Lerp(v.Cor, Color.white, 0.5f);
            Emitir(_poeira, pe + Vector3.up * 0.12f, PoeiraN, CorPoeiraA, CorPoeiraB);
            Emitir(_faiscas, pe + Vector3.up * 0.1f, FaiscasN, claro, v.Cor);
            Emitir(_onda, pe + Vector3.up * 0.08f, 1, claro, claro);
            Emitir(_clarao, pe + Vector3.up * 0.5f, 1, claro, claro);
        }

        // ------------------------------------------------------------------ utilidades

        static void Emitir(ParticleSystem ps, Vector3 pos, int n, Color a, Color b)
        {
            ParticleSystem.MainModule m = ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(a, b);
            ps.transform.position = pos;   // MUNDO: as particulas velhas ficam onde nasceram
            ps.Emit(n);
        }

        /// <summary>Alfa que ENTRA ate' `cheio` e sai a partir de `sai` (fracoes da vida). Branco: a cor e' da particula.</summary>
        static Gradient Esvai(float cheio, float sai)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, cheio), new GradientAlphaKey(1f, sai), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>Filho com malha, sem colisor (a camera e a mira nao podem esbarrar em VFX) e sem sombra.</summary>
        static Renderer Peca(Transform pai, string nome, Mesh malha, Material mat)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        /// <summary>Quadrado 1x1 no plano XY COM uv e cor de vertice (o MalhaVfx.Quad nao tem uv: a textura sumiria).</summary>
        static Mesh QuadXY()
        {
            var m = new Mesh { name = "VfxQuadVoo" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            m.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>1 no eixo do traco, 0 a `meia` de distancia, perfil macio (nao serrilha no bilinear).</summary>
        static float Traco(float d, float meia)
        {
            float k = Mathf.Clamp01(1f - Mathf.Abs(d) / meia);
            return k * k * (3f - 2f * k);
        }

        /// <summary>
        /// O CIRCULO DE RUNAS, 256x256 gerado 1x (zero arquivo), com mip (visto rasante nao cintila): quatro aneis, a faixa de
        /// 32 glifos (cada setor sorteia haste, travessas, diagonal e anel por hash: le' como escrita, nao como padrao), o
        /// hexagrama inscrito e um fundo de luz que desce do centro. Branco: o dourado HDR vem do _BaseColor.
        /// </summary>
        static Texture2D TexturaRunas()
        {
            const int n = 256;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "RunasDoCastelo", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            var nx = new float[6];
            var ny = new float[6];
            for (int k = 0; k < 6; k++) { float a = (30f + 60f * k) * Mathf.Deg2Rad; nx[k] = Mathf.Cos(a); ny[k] = Mathf.Sin(a); }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 0f;
                    if (r < 1f)
                    {
                        a = 0.1f + 0.14f * (1f - r) * (1f - r);
                        a = Mathf.Max(a, Traco(r - 0.955f, 0.016f));
                        a = Mathf.Max(a, Traco(r - 0.9f, 0.01f));
                        a = Mathf.Max(a, Traco(r - 0.74f, 0.012f));
                        a = Mathf.Max(a, Traco(r - 0.4f, 0.01f));
                        if (r > 0.755f && r < 0.885f) a = Mathf.Max(a, Glifo(Mathf.Atan2(dy, dx), (r - 0.755f) / 0.13f));
                        if (r < 0.74f)   // cada lado do hexagrama e' uma corda: a reta dentro do circulo de 0,74 JA' e' o lado
                            for (int k = 0; k < 6; k++) a = Mathf.Max(a, Traco(dx * nx[k] + dy * ny[k] - 0.37f, 0.009f));
                        a *= Mathf.Clamp01((1f - r) / 0.02f);
                    }
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            t.SetPixels32(px);
            t.Apply(true, true);
            return t;
        }

        /// <summary>O glifo do setor em (angulo, v = 0..1 na faixa). Setor sem traco sorteado ganha a haste.</summary>
        static float Glifo(float ang, float v)
        {
            const int setores = 32;
            float s = (ang / (Mathf.PI * 2f) + 1f) * setores;
            float u = s - Mathf.Floor(s);
            if (u < 0.18f || u > 0.82f || v < 0.08f || v > 0.92f) return 0f;   // respiro entre glifos
            uint h = (uint)((int)s % setores) * 2654435761u;
            h ^= h >> 15;
            float uu = (u - 0.18f) / 0.64f;
            const float w = 0.1f;
            float g = 0f;
            if ((h & 1u) != 0u || (h & 14u) == 0u) g = Mathf.Max(g, Traco(uu - 0.5f, w));
            if ((h & 2u) != 0u) g = Mathf.Max(g, Traco(v - 0.25f, w * 1.3f));
            if ((h & 4u) != 0u) g = Mathf.Max(g, Traco(v - 0.75f, w * 1.3f));
            if ((h & 8u) != 0u) g = Mathf.Max(g, Traco(uu - v, w));
            if ((h & 16u) != 0u)
            {
                float cu = uu - 0.5f, cv = v - 0.5f;
                g = Mathf.Max(g, Traco(Mathf.Sqrt(cu * cu + cv * cv) - 0.22f, w));
            }
            return g;
        }

        /// <summary>A onda do pouso, 64x64 gerada 1x: aro macio perto da borda e um veu fraco que clareia para ele.</summary>
        static Texture2D TexturaOnda()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "OndaDoPouso", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = r < 1f ? Mathf.Max(Traco(r - 0.8f, 0.16f), 0.18f * r * r) * Mathf.Clamp01((1f - r) / 0.05f) : 0f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }
    }
}
