using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Characters
{
    /// <summary>
    /// Materiais do personagem por codigo (zero asset). Arkana/Mago (toon de personagem: faixa macia + contorno de
    /// luz + emissao, em Resources/); sem ele, URP Lit; sem URP, Standard — calado, cadeia de reserva.
    /// METAL DOMADO (Godot 26/08): sem reflection probe o metal vira silhueta preta no mobile —
    /// metallic &lt;= 0.2 e smoothness &lt;= 0.55 em TUDO que veste um mago, inclusive o .glb importado.
    /// </summary>
    public static class MaterialMago
    {
        public const float MetallicMax = 0.2f, SmoothnessMax = 0.55f;
        static Shader _shader, _toon;

        /// <summary>Arkana/Mago, se o aparelho roda; senao null.</summary>
        static Shader Toon()
        {
            if (_toon == null)
            {
                Shader t = Resources.Load<Shader>("ArkanaMago");
                if (t == null) t = UnityEngine.Shader.Find("Arkana/Mago");
                if (t != null && t.isSupported) _toon = t;
            }
            return _toon;
        }

        static Shader Achar()
        {
            if (_shader == null) _shader = Toon();
            if (_shader == null) _shader = UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null) _shader = UnityEngine.Shader.Find("Standard");
            return _shader;
        }

        /// <summary>
        /// O material do FBX (URP Lit que o ImportacaoArkana faz, com a textura da Meshy) passa para o toon com CONTORNO
        /// (onda 9A: de costas para o sol o mago era uma silhueta escura). O shader usa os nomes do Lit: textura, normal,
        /// cor e preenchimento vem junto na troca. Sem toon suportado, fica o Lit — calado.
        /// </summary>
        public static void Vestir(Material m)
        {
            Shader t = Toon();
            if (m != null && t != null) m.shader = t;
        }

        /// <summary>Null so' se nao ha' shader nenhum (build sem URP e sem Standard): o primitivo fica no rosa padrao, nunca lanca.</summary>
        public static Material Novo(Color cor, float emissao = 0f, float metallic = 0.05f, float smoothness = 0.35f)
        {
            Shader s = Achar();
            if (s == null) return null;
            Material m = new Material(s);
            Pintar(m, cor);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            Domar(m);
            if (emissao > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", cor * emissao);
            }
            return m;
        }

        /// <summary>_BaseColor (URP) e _Color (Standard): o que houver.</summary>
        public static void Pintar(Material m, Color cor)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", cor);
            if (m.HasProperty("_Color")) m.SetColor("_Color", cor);
        }

        public static void Domar(Material m)
        {
            if (m == null) return;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", Mathf.Min(m.GetFloat("_Metallic"), MetallicMax));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", Mathf.Min(m.GetFloat("_Smoothness"), SmoothnessMax));
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", Mathf.Min(m.GetFloat("_Glossiness"), SmoothnessMax));
        }

        /// <summary>Primitivo SEM colisor (o pawn tem o dele; 14 colisores por mago x 7 magos = fisica a toa).</summary>
        public static Transform Primitivo(Transform pai, string nome, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            g.name = nome;
            Collider c = g.GetComponent<Collider>();
            if (c != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(c); else UnityEngine.Object.DestroyImmediate(c); }
            g.transform.SetParent(pai, false);
            g.transform.localPosition = pos;
            g.transform.localScale = escala;
            if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
            return g.transform;
        }

        public static Transform Pivo(Transform pai, string nome, Vector3 pos)
        {
            GameObject g = new GameObject(nome);
            g.transform.SetParent(pai, false);
            g.transform.localPosition = pos;
            return g.transform;
        }
    }

    /// <summary>
    /// O MAGO na cena — fachada estavel para PAWN/CENA. Visual puro: sem fisica, sem input.
    /// Corpo procedural por primitivas animado por PoseMago a cada frame; se houver um modelo em
    /// Resources/magos/&lt;slug&gt; com os tres clipes obrigatorios, ele veste o modelo (aliases de clipe,
    /// laco, metal domado) — ausente ou incompleto, cai no procedural em silencio (fiacao defensiva).
    /// </summary>
    public sealed class Mago : MonoBehaviour
    {
        /// <summary>Crossfade por DESTINO (Godot BLEND): entrar no cast e' quase seco — o dedo tem que VER o gesto.</summary>
        // a decolagem do pulo tambem e' seca: o corpo ja' saiu do chao no quadro do botao
        const float BlendIdle = 0.18f, BlendRun = 0.12f, BlendCast = 0.05f, BlendOutro = 0.15f, BlendPulo = 0.08f;

        // ---- esqueleto de referencia (metros, corpo de 1,80 m, pes em y=0). A altura SAI daqui.
        const float QuadrilY = 0.92f, TroncoAlt = 0.60f, CabecaDiam = 0.28f;
        const float OmbroY = 0.50f, OmbroX = 0.26f, BracoComp = 0.62f, PernaX = 0.11f;
        public const float AlturaRef = QuadrilY + TroncoAlt + CabecaDiam;   // 1,80: o topo do cranio

        public IdentidadeMago Identidade { get; private set; }
        public string Slug => Identidade != null ? Identidade.Slug : "";
        /// <summary>Altura em metros, medida pelo esqueleto ja' escalado (pes em y=0).</summary>
        public float Altura { get; private set; }
        public Transform MaoDireita { get; private set; }
        public Transform Cabeca { get; private set; }
        /// <summary>O que toca (no externo pode ser o substituto do pedido).</summary>
        public Clipe ClipeAtual => _clipe;
        /// <summary>O que foi PEDIDO por ultimo.</summary>
        public Clipe ClipePedido => _pedido;
        /// <summary>"procedural" ou "external:magos/&lt;slug&gt;".</summary>
        public string Fonte { get; private set; } = "procedural";
        /// <summary>Graus que o TRONCO gira sobre as pernas, aplicados depois da animacao (o Pawn escreve por quadro:
        /// PoseMago.TorcaoDoTronco). Positivo = peito para a direita.</summary>
        public float Torcao { get; set; }
        /// <summary>s de voo previstos para o pulo que comeca (o Pawn escreve na decolagem): o ar do take cabe neles.</summary>
        public float VooS { get; set; } = 1f;
        /// <summary>(decolagem, pouso) em s dentro do take do pulo, medidos no proprio take; zero sem o take.</summary>
        public Vector2 FasesDoPulo { get; private set; }
        /// <summary>O gesto de conjurar esta' so' no tronco (pernas seguindo a passada)?</summary>
        public bool CastNoTronco => _castTronco;

        /// <summary>O quadro em que a mao chega a frente. E' aqui que nasce o projetil.</summary>
        public event Action CastFired;

        // procedural
        Transform _rig, _raiz, _tronco, _bracoE, _bracoD, _pernaE, _pernaD, _maoDEsfera;
        Vector3 _ombroE, _ombroD;
        readonly List<Material> _tint = new List<Material>();

        // externo (Animation legado: o .glb importado como Legacy traz os clipes por nome; sem Controller asset)
        Animation _anim;
        readonly Dictionary<Clipe, string> _clipesExternos = new Dictionary<Clipe, string>();
        /// o cast numa copia na camada 1, misturada so' da coluna para cima (null = sem coluna: cast de corpo inteiro)
        string _castTroncoNome;
        /// sem o Regular_Jump: uma copia da corrida, segura no passo no ar (o crossfade da corrida para ela e' de verdade)
        string _saltoReserva;
        /// a coluna a partir do Hips (Meshy: Spine02 > Spine01 > Spine): onde a torcao mora
        Transform[] _coluna = new Transform[0];
        Quaternion[] _colunaBase, _colunaEscrita;
        /// (decolagem, pouso) por take: medido uma vez por clipe, os bots do mesmo mago reaproveitam
        static readonly Dictionary<AnimationClip, Vector2> _fasesMedidas = new Dictionary<AnimationClip, Vector2>();

        // estado
        Clipe _clipe = Clipe.Idle, _pedido = Clipe.Idle;
        float _t, _ms, _blend = 1f, _blendDur = BlendIdle;
        bool _correndo, _castDisparado, _reverso, _castTronco, _segura;
        PoseCorpo _de, _aplicada;

        // ------------------------------------------------------------------ criacao

        public static Mago Criar(Transform pai, string slug)
        {
            GameObject g = new GameObject("Mago " + (slug ?? ""));
            g.transform.SetParent(pai, false);
            Mago m = g.AddComponent<Mago>();
            m.Montar(slug);
            return m;
        }

        /// <summary>Prefab em Resources/magos/&lt;slug&gt;. Ausente -> false, nunca lanca.</summary>
        public static bool TentarModeloExterno(string slug, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(slug)) return false;
            try { prefab = Resources.Load<GameObject>("magos/" + slug); }
            catch (Exception) { prefab = null; }
            return prefab != null;
        }

        void Montar(string slug)
        {
            Identidade = IdentidadeMago.De(slug);
            _rig = MaterialMago.Pivo(transform, "Rig", Vector3.zero);
            GameObject prefab;
            if (TentarModeloExterno(slug, out prefab) && MontarExterno(prefab)) Fonte = "external:magos/" + slug;
            else MontarProcedural();
            _aplicada = _de = PoseMago.Pose(Clipe.Idle, 0f);
            Aplicar(_aplicada);
        }

        void MontarProcedural()
        {
            IdentidadeMago id = Identidade;
            float bulk = IdentidadeMago.Largura(id.Silhueta);
            float escala = id.AlturaM / AlturaRef;
            _rig.localScale = Vector3.one * escala;            // UNIFORME: nao-uniforme cisalha filho rotacionado
            Altura = AlturaRef * escala;

            Material manto = MaterialMago.Novo(IdentidadeMago.Cor(id.CorPrimaria));
            Material debrum = MaterialMago.Novo(IdentidadeMago.Cor(id.CorSecundaria), 0f, 0.2f, 0.55f);
            Material pele = MaterialMago.Novo(new Color(0.88f, 0.73f, 0.55f), 0f, 0f, 0.3f);
            Material marca = MaterialMago.Novo(IdentidadeMago.Cor(id.CorMarca), 2.5f, 0f, 0.5f);
            _tint.Add(manto);

            _raiz = MaterialMago.Pivo(_rig, "Raiz", Vector3.zero);
            Transform quadril = MaterialMago.Pivo(_raiz, "Quadril", new Vector3(0f, QuadrilY + id.FlutuaM, 0f));

            _tronco = MaterialMago.Pivo(quadril, "Tronco", Vector3.zero);
            MaterialMago.Primitivo(_tronco, "Corpo", PrimitiveType.Capsule, new Vector3(0f, TroncoAlt * 0.5f, 0f),
                new Vector3(0.24f * bulk, TroncoAlt * 0.5f, 0.18f * bulk), manto);
            // tunica ate' meia coxa (as pernas ficam visiveis para a corrida ler); largura da raca mora AQUI
            MaterialMago.Primitivo(_tronco, "Manto", PrimitiveType.Cylinder, new Vector3(0f, -0.22f, 0f),
                new Vector3(0.30f * bulk, 0.22f, 0.26f * bulk), manto);
            MaterialMago.Primitivo(_tronco, "Cinto", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f),
                new Vector3(0.27f * bulk, 0.02f, 0.21f * bulk), debrum);
            // A MARCA: emissiva no peito — o que se le' a 20 m junto com a cor
            MaterialMago.Primitivo(_tronco, "Marca", PrimitiveType.Sphere, new Vector3(0f, 0.40f, 0.10f * bulk),
                Vector3.one * 0.07f, marca);

            Cabeca = MaterialMago.Pivo(_tronco, "Cabeca", new Vector3(0f, TroncoAlt, 0f));
            MaterialMago.Primitivo(Cabeca, "Cranio", PrimitiveType.Sphere, new Vector3(0f, CabecaDiam * 0.5f, 0f),
                Vector3.one * CabecaDiam, pele);
            // capuz/chapeu: aba + bico (silhueta de mago, mesmo em primitiva)
            MaterialMago.Primitivo(Cabeca, "Aba", PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f),
                new Vector3(0.24f * bulk, 0.012f, 0.24f * bulk), manto);
            MaterialMago.Primitivo(Cabeca, "Bico", PrimitiveType.Capsule, new Vector3(0f, 0.32f, -0.02f),
                new Vector3(0.11f, 0.12f, 0.11f), manto);
            MaterialMago.Primitivo(Cabeca, "OlhoE", PrimitiveType.Sphere, new Vector3(-0.045f, 0.15f, 0.12f), Vector3.one * 0.03f, marca);
            MaterialMago.Primitivo(Cabeca, "OlhoD", PrimitiveType.Sphere, new Vector3(0.045f, 0.15f, 0.12f), Vector3.one * 0.03f, marca);

            _bracoE = Braco(quadril, "BracoE", -OmbroX * bulk, manto, debrum, pele, out Transform maoE);
            _bracoD = Braco(quadril, "BracoD", OmbroX * bulk, manto, debrum, pele, out Transform maoD);
            _ombroE = _bracoE.localPosition;
            _ombroD = _bracoD.localPosition;
            MaoDireita = maoD;
            _maoDEsfera = maoD.GetChild(0);

            _pernaE = Perna(quadril, "PernaE", -PernaX * bulk, manto);
            _pernaD = Perna(quadril, "PernaD", PernaX * bulk, manto);
        }

        Transform Braco(Transform quadril, string nome, float x, Material manto, Material debrum, Material pele, out Transform mao)
        {
            Transform b = MaterialMago.Pivo(quadril, nome, new Vector3(x, OmbroY, 0f));
            MaterialMago.Primitivo(b, "Manga", PrimitiveType.Capsule, new Vector3(0f, -BracoComp * 0.5f, 0f),
                new Vector3(0.09f, BracoComp * 0.5f, 0.09f), manto);
            MaterialMago.Primitivo(b, "Punho", PrimitiveType.Cylinder, new Vector3(0f, -BracoComp + 0.06f, 0f),
                new Vector3(0.10f, 0.02f, 0.10f), debrum);
            // a mao e' um PIVO (escala 1): a luva pendura nele e herda so' a escala do rig
            mao = MaterialMago.Pivo(b, "Mao", new Vector3(0f, -BracoComp, 0f));
            MaterialMago.Primitivo(mao, "Palma", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.11f, pele);
            return b;
        }

        Transform Perna(Transform quadril, string nome, float x, Material manto)
        {
            Transform p = MaterialMago.Pivo(quadril, nome, new Vector3(x, 0f, 0f));
            MaterialMago.Primitivo(p, "Coxa", PrimitiveType.Capsule, new Vector3(0f, -QuadrilY * 0.5f, 0f),
                new Vector3(0.15f, QuadrilY * 0.5f, 0.15f), manto);   // capsula de altura QuadrilY: o pe' toca y=0
            return p;
        }

        /// <summary>
        /// Veste o prefab se ele tem Animation com idle/run/cast (por alias) e cast mais longo que o disparo.
        /// Convencao: glTF olha +Z e o Unity anda para +Z — NAO leva a meia-volta que o Godot precisava.
        /// </summary>
        bool MontarExterno(GameObject prefab)
        {
            GameObject inst = Instantiate(prefab, _rig, false);
            inst.name = "ExternalModel";
            Animation anim = inst.GetComponentInChildren<Animation>();
            if (anim == null) { Descartar(inst); return false; }

            Dictionary<string, string> porNorm = new Dictionary<string, string>();
            foreach (AnimationState st in anim) porNorm[PoseMago.Normaliza(st.name)] = st.name;
            foreach (Clipe c in PoseMago.Todos)
                foreach (string alias in PoseMago.AliasesDe(c))
                {
                    string real;
                    if (porNorm.TryGetValue(PoseMago.Normaliza(alias), out real)) { _clipesExternos[c] = real; break; }
                }
            if (!_clipesExternos.ContainsKey(Clipe.Idle) || !_clipesExternos.ContainsKey(Clipe.Run)
                || !_clipesExternos.ContainsKey(Clipe.Cast)
                || anim[_clipesExternos[Clipe.Cast]].length <= PoseMago.CastFireT)
            {
                Debug.LogWarning("Mago: modelo externo de '" + Slug + "' sem idle/run/cast utilizaveis; procedural.");
                _clipesExternos.Clear();
                Descartar(inst);
                return false;
            }
            // PULO EM FASES (onda 15B): os tempos saem do proprio take; a aterrissagem e' o MESMO take numa copia, para o
            // crossfade do ar segurado ao pouso ser mistura de verdade (um estado nao mistura consigo mesmo)
            Transform hips = Osso(inst.transform, "hips", null);
            string pulo;
            if (_clipesExternos.TryGetValue(Clipe.Pular, out pulo))
            {
                AnimationClip take = anim[pulo].clip;
                FasesDoPulo = MedirPulo(anim, take, hips);
                anim.AddClip(take, pulo + "#pouso");
                _clipesExternos[Clipe.Pousar] = pulo + "#pouso";
            }
            else
            {
                string run = _clipesExternos[Clipe.Run];
                _saltoReserva = run + "#salto";
                anim.AddClip(anim[run].clip, _saltoReserva);
                anim[_saltoReserva].wrapMode = WrapMode.ClampForever;
            }
            // ATIRAR CORRENDO: o cast numa copia na camada 1, so' da coluna para cima — as pernas seguem a camada 0
            _coluna = Coluna(hips);
            if (_coluna.Length > 0)
            {
                _colunaBase = new Quaternion[_coluna.Length];
                _colunaEscrita = new Quaternion[_coluna.Length];
                string cast = _clipesExternos[Clipe.Cast];
                _castTroncoNome = cast + "#tronco";
                anim.AddClip(anim[cast].clip, _castTroncoNome);
                AnimationState tronco = anim[_castTroncoNome];
                tronco.layer = 1;
                tronco.AddMixingTransform(_coluna[0], true);
                tronco.wrapMode = WrapMode.ClampForever;
            }
            // O importador traz tudo sem laco: run tocava tres passos e congelava (patinacao, 25/08).
            // Disparo unico em ClampForever: segura o ultimo quadro e o crossfade de volta parte DELE (em Once o estado
            // morria e o blend saia do nada). O fim do disparo se le' pelo tempo, no Tick. O ar do pulo tambem segura.
            foreach (KeyValuePair<Clipe, string> kv in _clipesExternos)
                anim[kv.Value].wrapMode = PoseMago.Laco(kv.Key) && kv.Key != Clipe.Pular ? WrapMode.Loop : WrapMode.ClampForever;
            anim.playAutomatically = false;
            _anim = anim;
            // o clipe inicial JA' e' Idle, e Play(Idle) sai cedo (laco igual ao atual): sem isto o externo ficava na pose
            // congelada do FBX (o elenco inteiro de braco erguido, foto de 12/09) ate' correr a primeira vez
            anim.Play(_clipesExternos[Clipe.Idle]);

            // materiais proprios (o tint de um bot nao pode pintar o player) + toon com contorno + metal domado
            Bounds b = new Bounds(inst.transform.position, Vector3.zero);
            bool temBounds = false;
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.materials;   // instancia
                foreach (Material m in mats) { MaterialMago.Vestir(m); MaterialMago.Domar(m); _tint.Add(m); }
                Bounds rb = LimitesDeRepouso(r);
                if (!temBounds) { b = rb; temBounds = true; } else b.Encapsulate(rb);
            }
            float alturaMalha = temBounds && b.size.y > 0.01f ? b.size.y : IdentidadeMago.AlturaRef;
            float escala = Identidade.AlturaM / alturaMalha;
            _rig.localScale = Vector3.one * escala;
            if (temBounds) inst.transform.localPosition = new Vector3(0f, -(b.min.y - inst.transform.position.y), 0f);  // pes em y=0
            Altura = Identidade.AlturaM;

            MaoDireita = Osso(inst.transform, "hand", "r") ?? MaterialMago.Pivo(_rig, "MaoD", new Vector3(0.25f, 0.55f * alturaMalha, 0.2f));
            Cabeca = Osso(inst.transform, "head", null) ?? MaterialMago.Pivo(_rig, "Cabeca", new Vector3(0f, 0.9f * alturaMalha, 0f));
            return true;
        }

        /// <summary>
        /// Tamanho REAL em repouso. O SkinnedMeshRenderer.bounds do FBX da Meshy vem ~100x maior (localBounds no espaco do
        /// Hips, que herda a escala 100 da Armature) e a Pyra saia com 1 cm (12/09). A malha de repouso levada pelo
        /// transform do proprio renderer e' o que o skinning desenha na pose de bind.
        /// ponytail: os bounds de culling seguem gigantes (o mago nunca e' cortado); corrigir localBounds se pesar.
        /// </summary>
        static Bounds LimitesDeRepouso(Renderer r)
        {
            var smr = r as SkinnedMeshRenderer;
            if (smr == null || smr.sharedMesh == null) return r.bounds;
            Bounds m = smr.sharedMesh.bounds;
            Matrix4x4 w = smr.transform.localToWorldMatrix;
            Bounds b = new Bounds(w.MultiplyPoint3x4(m.min), Vector3.zero);
            for (int i = 1; i < 8; i++)
                b.Encapsulate(w.MultiplyPoint3x4(new Vector3((i & 1) != 0 ? m.max.x : m.min.x,
                    (i & 2) != 0 ? m.max.y : m.min.y, (i & 4) != 0 ? m.max.z : m.min.z)));
            return b;
        }

        static Transform Osso(Transform raiz, string contem, string lado)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant();
                if (!n.Contains(contem)) continue;
                if (lado == null || n.Contains(lado + "_") || n.EndsWith(lado) || n.Contains("." + lado) || n.Contains("right")) return t;
            }
            return null;
        }

        /// <summary>A coluna a partir do Hips: o filho "spine" de cada osso, ate' 3 (Meshy: Spine02 > Spine01 > Spine).</summary>
        static Transform[] Coluna(Transform hips)
        {
            var r = new List<Transform>(3);
            for (Transform t = hips; t != null && r.Count < 3;)
            {
                Transform prox = null;
                foreach (Transform f in t)
                    if (f.name.ToLowerInvariant().Contains("spine")) { prox = f; break; }
                if (prox != null) r.Add(prox);
                t = prox;
            }
            return r.ToArray();
        }

        /// <summary>
        /// Decolagem e pouso do take, pela altura do Hips (PoseMago.FasesDoPulo) amostrada com SampleAnimation. Em RUNTIME,
        /// uma vez por clipe: vale para qualquer FBX que chegar, sem metadado a manter nem reimportacao, e custa ~115
        /// amostras de 28 ossos uma vez por mago. A pose do modelo volta exatamente como estava.
        /// </summary>
        static Vector2 MedirPulo(Animation anim, AnimationClip take, Transform hips)
        {
            if (take == null || hips == null) return Vector2.zero;
            Vector2 f;
            if (_fasesMedidas.TryGetValue(take, out f)) return f;
            GameObject raiz = anim.gameObject;
            Transform[] ts = raiz.GetComponentsInChildren<Transform>(true);
            var pos = new Vector3[ts.Length];
            var rot = new Quaternion[ts.Length];
            for (int i = 0; i < ts.Length; i++) { pos[i] = ts[i].localPosition; rot[i] = ts[i].localRotation; }
            const float passo = 1f / 60f;
            var h = new float[Mathf.Max(Mathf.CeilToInt(take.length / passo) + 1, 2)];
            for (int i = 0; i < h.Length; i++)
            {
                take.SampleAnimation(raiz, Mathf.Min(i * passo, take.length));
                h[i] = raiz.transform.InverseTransformPoint(hips.position).y;
            }
            for (int i = 0; i < ts.Length; i++) { ts[i].localPosition = pos[i]; ts[i].localRotation = rot[i]; }
            PoseMago.FasesDoPulo(h, passo, out float decolagem, out float pouso);
            f = new Vector2(decolagem, pouso);
            _fasesMedidas[take] = f;
            return f;
        }

        static void Descartar(GameObject g)
        {
            if (Application.isPlaying) Destroy(g); else DestroyImmediate(g);
        }

        // ------------------------------------------------------------------ contrato

        /// <summary>Todo nome do contrato/Mixamo/Meshy resolve no procedural; no externo, o que o modelo trouxe.</summary>
        public bool TemClipe(string nome)
        {
            Clipe? c = PoseMago.Alias(nome);
            if (c == null) return false;
            return _anim == null || _clipesExternos.ContainsKey(c.Value);
        }

        /// <summary>Cast REINICIA sempre (disparo continuo); laco igual ao atual nao faz nada. Desconhecido avisa e ignora.</summary>
        public void Play(string nome)
        {
            Clipe? c = PoseMago.Alias(nome);
            if (c == null) { Debug.LogWarning("Mago: clipe desconhecido '" + nome + "'"); return; }
            Play(c.Value);
        }

        /// <summary>
        /// ATIRAR CORRENDO (onda 15B): com o corpo em movimento (ou no pulo) o cast vai so' no TRONCO, numa camada acima da
        /// passada — as pernas nao congelam. Parado, o cast de corpo inteiro de sempre. Qualquer outro pedido encerra o cast
        /// do tronco (o gesto do Pawn acabou) e o braco desce no blend.
        /// </summary>
        public void Play(Clipe c)
        {
            if (c == Clipe.Cast && PodeCastNoTronco()) { IniciarCastTronco(0f, false); return; }
            if (_castTronco) EncerrarCastTronco();
            Tocar(c);
        }

        bool PodeCastNoTronco() => _castTroncoNome != null
            && (_correndo || _pedido == Clipe.Pular || _pedido == Clipe.Pousar || _pedido == Clipe.AndarTras);

        void IniciarCastTronco(float t, bool disparado)
        {
            AnimationState st = _anim[_castTroncoNome];
            st.time = t;
            st.speed = 1f;
            _anim.CrossFade(_castTroncoNome, BlendCast);   // StopSameLayer: so' a camada 1; a passada da 0 segue
            _castTronco = true;
            _castDisparado = disparado;
        }

        void EncerrarCastTronco()
        {
            _castTronco = false;
            _anim.Blend(_castTroncoNome, 0f, BlendOutro);
        }

        void Tocar(Clipe c)
        {
            // laco igual ao PEDIDO: nada (SetVelocidade chama todo frame; reiniciar aqui travaria o blend em t=0)
            if (PoseMago.Laco(c) && c == _pedido) return;
            Clipe pedido = c;
            if (_anim != null && !_clipesExternos.ContainsKey(c)) c = PoseMago.Substituto(c);
            if (_anim != null && !_clipesExternos.ContainsKey(c)) c = Clipe.Idle;   // nem o substituto (queda sem take): o Idle e' garantido
            _pedido = pedido;
            _de = _aplicada;
            _blend = 0f;
            _blendDur = c == Clipe.Cast ? BlendCast : c == Clipe.Run || c == Clipe.AndarTras ? BlendRun
                : c == Clipe.Idle ? BlendIdle : pedido == Clipe.Pular || pedido == Clipe.Pousar ? BlendPulo : BlendOutro;
            _clipe = c;
            _t = 0f;
            _reverso = pedido == Clipe.AndarTras && c == Clipe.Run;
            _segura = pedido == Clipe.Pular && c != Clipe.Pular && _saltoReserva != null;
            // PEDIDO, nunca o substituto: "pegar" caindo em "cast" NAO pode virar tiro (achado de 26/08). O cast do tronco
            // cuida do proprio disparo: a passada trocar por baixo dele nao o perde.
            if (pedido == Clipe.Cast) _castDisparado = false;
            else if (!_castTronco) _castDisparado = true;
            if (_anim == null) return;
            string real = Real(c);
            AnimationState st = _anim[real];
            float velocidade = 1f;   // o Tick cadencia a passada (e inverte a corrida de reserva)
            if (pedido == Clipe.Pular && c == Clipe.Pular)
            {
                // o jogo decola NA HORA: pula a agachada e estica o ar do take no voo previsto
                st.time = FasesDoPulo.x;
                velocidade = PoseMago.VelocidadeNoAr(FasesDoPulo.x, FasesDoPulo.y, VooS);
            }
            else if (_segura)
            {
                // sem o take: SEGURA o passo no ar da corrida o voo inteiro (nada de Idle subindo, nada de piscar)
                st.time = Balance.Anim.PuloReservaT;
                velocidade = 0f;
            }
            else if (c == Clipe.Pousar)
            {
                st.time = FasesDoPulo.y;
                velocidade = Balance.Anim.PousoVel;
            }
            // CROSSFADE com os mesmos tempos do procedural (a troca seca de clipe era o "pulo" do externo).
            // Disparo unico reinicia do zero (o cast continuo); laco que ja' esta' saindo nao volta ao comeco.
            else if (!PoseMago.Laco(c) || !_anim.IsPlaying(real)) st.time = 0f;
            st.speed = velocidade;
            _anim.CrossFade(real, _blendDur);
        }

        /// <summary>O estado legado que toca `c` agora (o salto de reserva e' a copia segura da corrida).</summary>
        string Real(Clipe c) => _segura ? _saltoReserva : _clipesExternos[c];

        /// <summary>Velocidade real do pawn -> Idle/Run com histerese + cadencia da passada (lei da patinacao).</summary>
        public void SetVelocidade(float ms)
        {
            _ms = float.IsNaN(ms) ? 0f : Mathf.Abs(ms);
            _correndo = PoseMago.Correndo(_correndo, _ms);
            if (_pedido == Clipe.Cast && _clipe == Clipe.Cast && _correndo && _castTroncoNome != null)
            {
                // saiu correndo no meio do gesto parado: as pernas voltam a andar, o braco segue no tronco sem perder o tiro
                float t = _anim[_clipesExternos[Clipe.Cast]].time;
                bool disparado = _castDisparado;
                Tocar(Locomocao());
                IniciarCastTronco(t, disparado);
                return;
            }
            // pelo PEDIDO: o salto de reserva toca a corrida, e a passada nao pode tira-lo do ar
            if (_pedido == Clipe.Idle || _pedido == Clipe.Run || _pedido == Clipe.AndarTras) Tocar(Locomocao());
        }

        /// <summary>Parado = Idle; correndo = Run, ou AndarTras se o ultimo pedido foi recuar (o Pawn troca pelo nome).</summary>
        Clipe Locomocao() => !_correndo ? Clipe.Idle : _pedido == Clipe.AndarTras ? Clipe.AndarTras : Clipe.Run;

        /// <summary>Recolore o manto (bots reusam o modelo). No externo, todos os materiais.</summary>
        public void SetTint(Color cor)
        {
            foreach (Material m in _tint) MaterialMago.Pintar(m, cor);
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>Avanca a animacao. Publico para teste e para quem quiser cadenciar por fora.</summary>
        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            AnimationState st = _anim != null ? _anim[Real(_clipe)] : null;
            float vel = 1f;
            if (_clipe == Clipe.Run) vel = PoseMago.EscalaDeCorrida(_ms);
            else if (_clipe == Clipe.AndarTras) vel = PoseMago.EscalaDeRecuo(_ms, st != null ? st.length : PoseMago.Duracao(Clipe.AndarTras));
            _t += dt * vel;
            if (st != null && !_segura)
            {
                if (_clipe == Clipe.Run || _clipe == Clipe.AndarTras) st.speed = _reverso ? -vel : vel;
                else if (_clipe == Clipe.Pular && st.time >= FasesDoPulo.y) { st.time = FasesDoPulo.y; st.speed = 0f; }   // segura a chegada ate' o chao
                else if (_clipe == Clipe.Cast) _t = st.time;   // o relogio e' o do clipe real
            }

            float relogio = _castTronco ? _anim[_castTroncoNome].time : _pedido == Clipe.Cast ? _t : -1f;
            if (!_castDisparado && relogio >= PoseMago.CastFireT)
            {
                _castDisparado = true;
                CastFired?.Invoke();
            }
            if (_castTronco && _anim[_castTroncoNome].time >= _anim[_castTroncoNome].length) EncerrarCastTronco();
            // Disparo unico acabou: volta ao idle/run. E' o blend de volta que ABAIXA o braco (DIRECAO.md).
            bool acabou = st != null ? st.time >= st.length : _t >= PoseMago.Duracao(_clipe);
            if (!PoseMago.Laco(_clipe) && acabou) { Tocar(Locomocao()); return; }

            if (_anim != null) return;
            if (_blend < 1f) _blend = Mathf.Min(1f, _blend + dt / _blendDur);
            PoseCorpo alvo = PoseMago.Pose(_clipe, _t);
            _aplicada = _blend < 1f ? PoseMago.Lerp(_de, alvo, _blend) : alvo;
            Aplicar(_aplicada);
        }

        /// <summary>
        /// A TORCAO do tronco (onda 15B) no externo, DEPOIS da animacao: giro sobre o alto do mago, um terco por osso da
        /// coluna. Se a animacao nao reescreveu o osso neste quadro (culling, pausa), parte da base guardada — nunca acumula.
        /// </summary>
        void LateUpdate()
        {
            if (_anim == null || _coluna.Length == 0) return;
            Vector3 cima = transform.up;
            float parte = Torcao / _coluna.Length;
            for (int i = 0; i < _coluna.Length; i++)
            {
                Transform o = _coluna[i];
                Quaternion q = o.localRotation;
                if (q == _colunaEscrita[i]) q = _colunaBase[i];
                _colunaBase[i] = q;
                o.localRotation = Quaternion.AngleAxis(parte, o.parent.InverseTransformDirection(cima)) * q;
                _colunaEscrita[i] = o.localRotation;
            }
        }

        /// <summary>Para o diag das fotos: o que foi pedido, o que toca, tempo/velocidade do estado e a torcao.</summary>
        public string Estado()
        {
            string s = "pedido=" + _pedido + " toca=" + _clipe;
            if (_anim != null)
            {
                AnimationState st = _anim[Real(_clipe)];
                s += " '" + st.name + "' t=" + st.time.ToString("F2") + " v=" + st.speed.ToString("F2");
                if (_castTronco) s += " castTronco t=" + _anim[_castTroncoNome].time.ToString("F2");
            }
            return s + " torcao=" + Torcao.ToString("F0");
        }

        void Aplicar(PoseCorpo p)
        {
            if (_raiz == null) return;
            // a torcao do procedural: tronco e ombros giram juntos sobre o quadril (as pernas penduram dele tambem)
            Quaternion tor = Quaternion.AngleAxis(Torcao, Vector3.up);
            _raiz.localPosition = new Vector3(0f, p.BobY, 0f);
            _raiz.localRotation = Quaternion.Euler(p.Raiz);
            _tronco.localRotation = tor * Quaternion.Euler(p.Tronco);
            Cabeca.localRotation = Quaternion.Euler(p.Cabeca);
            _bracoE.localPosition = tor * _ombroE;
            _bracoD.localPosition = tor * _ombroD;
            _bracoE.localRotation = tor * Quaternion.Euler(p.BracoE);
            _bracoD.localRotation = tor * Quaternion.Euler(p.BracoD);
            _pernaE.localRotation = Quaternion.Euler(p.PernaE);
            _pernaD.localRotation = Quaternion.Euler(p.PernaD);
            _maoDEsfera.localScale = Vector3.one * 0.11f * p.MaoD;
        }
    }
}
