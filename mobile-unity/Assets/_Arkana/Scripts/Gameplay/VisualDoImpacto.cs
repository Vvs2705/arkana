using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O PESO DO ACERTO — so' LEITURA, nada aqui decide jogo (GDD §10: cor + forma). Tres coisas:
    /// (1) ESTOURO na lingua do elemento onde o tiro para: alvo, escudo e muro pelo Bus.TerrainHit (Projetil.Impacto emite
    /// em TODO acerto; o fio da Tessa e a poca da Pyra tambem; e o CHAO, que a Partida agora testa e para o tiro com
    /// Impacto — o estouro sobe para a superficie, porque o passo que cruzou o chao ja' esta' enterrado). (2) PISCADA do corpo atingido: _BaseColor HDR por MaterialPropertyBlock por um quadro de anime; o material do
    /// mago (tinta de bot, cinza de morto) nao e' tocado — soltar o bloco devolve tudo. (3) BOLHA quando o escudo absorve,
    /// na cor do NIVEL do escudo (a mesma da barra da HUD); quebrou, estoura maior e branca.
    /// Um sistema de MUNDO por variante serve todos os estouros ao mesmo tempo (as particulas velhas ficam onde nasceram):
    /// zero Instantiate por tiro, Emit(n) em rajada, zero lixo por quadro, zero luz dinamica. Nasce sob a arena (morre com
    /// ela). Os handlers do Bus nao lancam: excecao aqui cortaria os ouvintes seguintes (o TerrenoReativo e' um deles).
    /// </summary>
    public sealed class VisualDoImpacto : MonoBehaviour
    {
        /// <summary>s de corpo aceso: um quadro de anime (12 fps) — "pisca", nao acende.</summary>
        public const float PiscadaS = 0.08f;
        /// <summary>O DoT sai em migalhas por quadro (teto 3 por janela de 0,25 s) e deixaria o corpo em chamas aceso sem
        /// parar; o menor tiro que existe (vento na luva comum, no escudo: 5,6) passa. KNOB.</summary>
        public const float DanoMinimo = 3.5f;
        public const float BolhaS = 0.18f, BolhaQuebraS = 0.3f;
        /// <summary>m: diametro da bolha (o mago tem 1,8 m) e a altura do centro dela acima dos pes.</summary>
        const float BolhaM = 2.3f, CentroCorpo = 0.95f;
        /// <summary>m: estouro alem disto da camera nem nasce (faisca de 10 cm a 70 m e' um pixel).</summary>
        const float Alcance = 70f;
        /// <summary>m que o estouro anda para a camera: o acerto conta a 0,6 m do eixo do corpo (capsula + hitbox) e o
        /// proprio mago esconderia o miolo.</summary>
        const float Recuo = 0.35f;
        /// <summary>HDR da piscada e da bolha: acima de 1 passa do limiar do bloom e ACENDE. KNOB por foto.</summary>
        const float BrilhoPiscada = 2.2f, BrilhoBolha = 1.4f;
        /// <summary>Particulas por estouro, na ordem do enum Elemento (fogo, agua, raio, terra, vento); terra soma as pedrinhas.</summary>
        static readonly int[] Quantas = { 22, 26, 28, 14, 30 };
        const int Pedrinhas = 12;

        static Material _mPoeira, _mBolha;
        static Mesh _quadBolha;
        static int _idCor;

        MaterialPropertyBlock _mpb;
        readonly ParticleSystem[] _estouros = new ParticleSystem[5];
        ParticleSystem _pedrinhas, _clarao;
        Color[] _coresEscudo;
        readonly Dictionary<Pawn, Corpo> _corpos = new Dictionary<Pawn, Corpo>();
        readonly Bolha[] _bolhas = new Bolha[4];
        int _proxima;

        sealed class Corpo { public Renderer[] Rs; public float Resta; }
        sealed class Bolha { public Transform T; public Renderer R; public Pawn Alvo; public float Resta, Dur, Tam; public Color Cor; }

        /// <summary>Nasce sob a arena (morre com ela). `partida` fica na assinatura por simetria com o VisualDosKits: tudo chega pelo Bus.</summary>
        public static VisualDoImpacto Criar(Transform arena, Partida partida)
        {
            var go = new GameObject("VisualDoImpacto");
            if (arena != null) go.transform.SetParent(arena, false);
            var v = go.AddComponent<VisualDoImpacto>();
            return v;
        }

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (_idCor == 0) _idCor = Shader.PropertyToID("_BaseColor");
            if (_mPoeira == null)
            {
                _mPoeira = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.PontoSuave());
                _mBolha = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Aditivo, true, TexturaBolha());
                _quadBolha = QuadBolha();
            }
            // FOGO: brasas que SOBEM (gravidade negativa), ponto redondo
            _estouros[(int)Elemento.Fogo] = Variante("Brasas", new Color(1f, 0.62f, 0.2f), new Color(1f, 0.3f, 0.05f),
                new Vector2(0.35f, 0.75f), new Vector2(1.8f, 5f), new Vector2(0.11f, 0.27f), -0.45f, ParticleSystemShapeType.Sphere, 0f, 128, null);
            // AGUA: estilhaco claro — gotas em risco que espirram e caem
            _estouros[(int)Elemento.Agua] = Variante("Estilhaco", new Color(0.55f, 0.85f, 1f), new Color(0.2f, 0.55f, 1f),
                new Vector2(0.22f, 0.45f), new Vector2(3f, 7f), new Vector2(0.08f, 0.19f), 1.4f, ParticleSystemShapeType.Sphere, 0.035f, 128, null);
            // RAIO: faiscas RAPIDAS — riscos longos que morrem num piscar
            _estouros[(int)Elemento.Raio] = Variante("Faiscas", new Color(1f, 0.95f, 0.45f), new Color(1f, 0.78f, 0.1f),
                new Vector2(0.1f, 0.22f), new Vector2(7f, 14f), new Vector2(0.06f, 0.14f), 0.6f, ParticleSystemShapeType.Sphere, 0.03f, 144, null);
            // TERRA: poeira em ALFA (terra nao brilha) que abre devagar + pedrinhas que pulam e caem
            ParticleSystem poeira = Variante("Poeira", new Color(0.64f, 0.54f, 0.42f, 0.75f), new Color(0.46f, 0.37f, 0.27f, 0.6f),
                new Vector2(0.5f, 0.9f), new Vector2(0.6f, 1.8f), new Vector2(0.5f, 1.1f), -0.05f, ParticleSystemShapeType.Hemisphere, 0f, 80, _mPoeira);
            ParticleSystem.SizeOverLifetimeModule sp = poeira.sizeOverLifetime;
            sp.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.55f, 1f, 1.4f));   // a nuvem ABRE, nao encolhe
            _estouros[(int)Elemento.Terra] = poeira;
            _pedrinhas = Variante("Pedrinhas", new Color(0.36f, 0.26f, 0.17f), new Color(0.52f, 0.4f, 0.26f),
                new Vector2(0.45f, 0.7f), new Vector2(3f, 5.5f), new Vector2(0.1f, 0.2f), 2.2f, ParticleSystemShapeType.Hemisphere, 0f, 96, _mPoeira);
            // VENTO: ANEL deitado que abre em riscos (o circulo do emissor e' o chao: o Novo gira -90 em X)
            _estouros[(int)Elemento.Vento] = Variante("Anel", new Color(0.75f, 1f, 0.9f), new Color(0.4f, 0.9f, 0.7f),
                new Vector2(0.2f, 0.32f), new Vector2(5f, 7f), new Vector2(0.11f, 0.2f), 0f, ParticleSystemShapeType.Circle, 0.04f, 160, null);
            // CLARAO macio: UMA particula de ponto que abre e some; a cor (elemento puxado para o branco) sai por estouro
            _clarao = Variante("Clarao", Color.white, Color.white, new Vector2(0.14f, 0.14f), Vector2.zero, new Vector2(2f, 2f),
                0f, ParticleSystemShapeType.Sphere, 0f, 16, null);
            ParticleSystem.ShapeModule sc = _clarao.shape;
            sc.enabled = false;   // nasce no ponto, nao na esfera
            ParticleSystem.SizeOverLifetimeModule sz = _clarao.sizeOverLifetime;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.2f));

            _coresEscudo = new Color[Balance.Escudo.Cores.Length];
            for (int i = 0; i < _coresEscudo.Length; i++)
                if (!ColorUtility.TryParseHtmlString("#" + Balance.Escudo.Cores[i], out _coresEscudo[i])) _coresEscudo[i] = Color.white;
            for (int i = 0; i < _bolhas.Length; i++)
            {
                var go = new GameObject("Bolha", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = _quadBolha;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = _mBolha;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _bolhas[i] = new Bolha { T = go.transform, R = r };
            }
            // tudo configurado PARADO; so' agora toca (taxa 0: nada nasce sozinho, o Emit e' o estouro)
            for (int i = 0; i < _estouros.Length; i++) _estouros[i].Play();
            _pedrinhas.Play();
            _clarao.Play();
        }

        void OnEnable() { Bus.TerrainHit += AoAtingir; Bus.DamageApplied += AoDanar; }
        void OnDisable() { Bus.TerrainHit -= AoAtingir; Bus.DamageApplied -= AoDanar; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Apagar(dt);
            Bolhas(dt);
        }

        // ------------------------------------------------------------------ estouro

        /// <summary>O estouro de UM acerto em `pos`, na lingua do elemento. Publico: a foto (e quem mais quiser) chama direto.</summary>
        public void Estourar(Elemento el, Vector3 pos)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 d = cam.transform.position - pos;
                float m = d.magnitude;
                if (m > Alcance) return;
                if (m > 0.01f) pos += d * (Mathf.Min(Recuo, m * 0.5f) / m);
            }
            int i = Mathf.Clamp((int)el, 0, _estouros.Length - 1);
            Emitir(_estouros[i], pos, Quantas[i]);
            if (el == Elemento.Terra) Emitir(_pedrinhas, pos, Pedrinhas);
            ParticleSystem.MainModule mc = _clarao.main;
            mc.startColor = Color.Lerp(Projetil.Tint(el), Color.white, 0.2f);   // pouco branco: com o bloom, 0,45 lavava os cinco no mesmo branco (foto 25)
            Emitir(_clarao, pos, 1);
        }

        static void Emitir(ParticleSystem ps, Vector3 pos, int n)
        {
            ps.transform.position = pos;   // MUNDO: as particulas velhas ficam onde nasceram
            ps.Emit(n);
        }

        /// <summary>O tiro que o chao parou chega ate' um passo ENTERRADO (40 m/s a 60 fps = 0,66 m): sobe para a superficie.</summary>
        void AoAtingir(Elemento el, Vector3 pos, bool forte)
        {
            pos.y = Mathf.Max(pos.y, Ilha.AlturaDoChao(pos.x, pos.z));
            Estourar(el, pos);
        }

        // ------------------------------------------------------------------ corpo e escudo

        void AoDanar(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool escudo)
        {
            Pawn p = alvo as Pawn;
            if (p == null || !(dano >= DanoMinimo)) return;
            Vitalidade v = p.Vital;
            bool quebrou = v != null && v.Escudo <= 0f;
            if (escudo) Inflar(p, quebrou, v != null ? v.Nivel : 1);
            if (!escudo || quebrou) Piscar(p, el);   // escudo inteiro segurou tudo: so' a bolha; quebrou, a sobra pegou o corpo
        }

        void Piscar(Pawn p, Elemento el)
        {
            Corpo c;
            if (!_corpos.TryGetValue(p, out c)) { c = new Corpo { Rs = CorpoDe(p) }; _corpos[p] = c; }   // a busca aloca: 1x por corpo
            c.Resta = PiscadaS;
            Color k = Color.Lerp(Color.white, Projetil.Tint(el), 0.3f) * BrilhoPiscada;
            k.a = 1f;
            _mpb.Clear();
            _mpb.SetColor(_idCor, k);
            for (int i = 0; i < c.Rs.Length; i++) if (c.Rs[i] != null) c.Rs[i].SetPropertyBlock(_mpb);
        }

        /// <summary>As malhas do mago (e da luva que ja' estiver na mao na primeira piscada): particula e rastro ficam de fora.</summary>
        static Renderer[] CorpoDe(Pawn p)
        {
            Component raiz = p.Visual != null ? (Component)p.Visual : p;
            var saida = new List<Renderer>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                if (r is MeshRenderer || r is SkinnedMeshRenderer) saida.Add(r);
            return saida.ToArray();
        }

        void Apagar(float dt)
        {
            foreach (KeyValuePair<Pawn, Corpo> kv in _corpos)
            {
                Corpo c = kv.Value;
                if (c.Resta <= 0f) continue;
                c.Resta -= dt;
                if (c.Resta > 0f) continue;
                // null SOLTA o bloco: volta o material como estava (tinta de bot, cinza de morto)
                for (int i = 0; i < c.Rs.Length; i++) if (c.Rs[i] != null) c.Rs[i].SetPropertyBlock(null);
            }
        }

        void Inflar(Pawn p, bool quebrou, int nivel)
        {
            Bolha b = null;
            for (int i = 0; i < _bolhas.Length; i++)
                if (_bolhas[i].Alvo == p && _bolhas[i].Resta > 0f) { b = _bolhas[i]; break; }   // tiro seguido: a MESMA bolha recomeca
            // ponytail: 4 bolhas na tela; a 5a rouba a mais antiga (13 magos escudados apanhando no mesmo quadro nao acontece)
            if (b == null) { b = _bolhas[_proxima]; _proxima = (_proxima + 1) % _bolhas.Length; }
            b.Alvo = p;
            b.Dur = b.Resta = quebrou ? BolhaQuebraS : BolhaS;
            b.Tam = quebrou ? BolhaM * 1.35f : BolhaM;
            b.Cor = quebrou ? Color.white : _coresEscudo[Mathf.Clamp(nivel - 1, 0, _coresEscudo.Length - 1)];
            if (!b.T.gameObject.activeSelf) b.T.gameObject.SetActive(true);
            Pousar(b, Camera.main);   // ja' no lugar: um quadro com a bolha na origem da arena apareceria na foto
        }

        void Bolhas(float dt)
        {
            Camera cam = Camera.main;
            for (int i = 0; i < _bolhas.Length; i++)
            {
                Bolha b = _bolhas[i];
                if (b.Resta <= 0f) continue;
                b.Resta -= dt;
                if (b.Resta <= 0f || b.Alvo == null) { b.Resta = 0f; b.T.gameObject.SetActive(false); continue; }
                Pousar(b, cam);
            }
        }

        /// <summary>A bolha ACOMPANHA o corpo (particula de mundo ficaria para tras de quem corre), de frente para a camera (a
        /// esfera vista de qualquer lado e' um disco com aro) e na CALOTA da frente — no centro do corpo o peito do mago
        /// esconderia o miolo. Estufa rapido e some.</summary>
        void Pousar(Bolha b, Camera cam)
        {
            float f = 1f - b.Resta / b.Dur;   // 0 -> 1
            float estufa = 1f - (1f - f) * (1f - f);
            Vector3 centro = b.Alvo.Pos + Vector3.up * CentroCorpo;
            if (cam != null)
            {
                Vector3 d = cam.transform.position - centro;
                float m = d.magnitude;
                if (m > 0.01f) centro += d * (Mathf.Min(b.Tam * 0.4f, m * 0.5f) / m);
                b.T.rotation = cam.transform.rotation;
            }
            b.T.position = centro;
            b.T.localScale = Vector3.one * (b.Tam * (0.85f + 0.3f * estufa));
            float a = 1f - f * f;   // segura e some
            _mpb.Clear();
            _mpb.SetColor(_idCor, new Color(b.Cor.r * BrilhoBolha, b.Cor.g * BrilhoBolha, b.Cor.b * BrilhoBolha, a));
            b.R.SetPropertyBlock(_mpb);
        }

        // ------------------------------------------------------------------ fabrica

        /// <summary>Estouro PARADO (taxa 0) de MUNDO pelo ParticulaVfx.Novo. `risco` > 0 estica o ponto macio pela velocidade:
        /// faisca e gota voando leem como risco, nao como bolinha. O anel (Circle) e' so' a borda.</summary>
        ParticleSystem Variante(string nome, Color a, Color b, Vector2 vida, Vector2 vel, Vector2 tam, float gravidade,
            ParticleSystemShapeType forma, float risco, int max, Material mat)
        {
            ParticleSystem ps = ParticulaVfx.Novo(transform, nome, a, b, 0f, vida, vel, tam, true, 0f, max);
            ParticleSystem.MainModule m = ps.main;
            m.gravityModifier = gravidade;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = forma;
            bool anel = forma == ParticleSystemShapeType.Circle;
            sh.radius = anel ? 0.2f : 0.15f;
            sh.radiusThickness = anel ? 0f : 1f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (risco > 0f)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = risco;
                r.lengthScale = 1f;
            }
            if (mat != null) r.sharedMaterial = mat;
            return ps;
        }

        /// <summary>Bolha 64x64 gerada 1x (zero arquivo): ARO aceso + miolo quase vazio que clareia para a borda (o fresnel
        /// de mentira de uma esfera vista de frente). Branca: a cor e o esvaecer vem do _BaseColor.</summary>
        static Texture2D TexturaBolha()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Bolha", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float aro = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.12f);
                    float a = r < 1f ? Mathf.Max(aro * aro, 0.25f * r * r * r) : 0f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        /// <summary>Quadrado 1x1 no plano XY COM uv e cor de vertice (o MalhaVfx.Quad nao tem uv: a textura da bolha sumiria).</summary>
        static Mesh QuadBolha()
        {
            var m = new Mesh { name = "VfxBolha" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            m.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }
    }
}
