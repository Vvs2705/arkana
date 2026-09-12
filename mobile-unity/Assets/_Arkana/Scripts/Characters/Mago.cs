using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Characters
{
    /// <summary>
    /// Materiais do personagem por codigo (zero asset). Arkana/Mago (toon de personagem: faixas + rim +
    /// emissao, em Resources/); sem ele, URP Lit; sem URP, Standard — calado, cadeia de reserva.
    /// METAL DOMADO (Godot 26/08): sem reflection probe o metal vira silhueta preta no mobile —
    /// metallic &lt;= 0.2 e smoothness &lt;= 0.55 em TUDO que veste um mago, inclusive o .glb importado.
    /// </summary>
    public static class MaterialMago
    {
        public const float MetallicMax = 0.2f, SmoothnessMax = 0.55f;
        static Shader _shader;

        static Shader Achar()
        {
            if (_shader == null)
            {
                Shader toon = Resources.Load<Shader>("ArkanaMago");
                if (toon == null) toon = UnityEngine.Shader.Find("Arkana/Mago");
                if (toon != null && toon.isSupported) _shader = toon;
            }
            if (_shader == null) _shader = UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null) _shader = UnityEngine.Shader.Find("Standard");
            return _shader;
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
        const float BlendIdle = 0.18f, BlendRun = 0.12f, BlendCast = 0.05f, BlendOutro = 0.15f;

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
        public Clipe ClipeAtual => _clipe;
        /// <summary>"procedural" ou "external:magos/&lt;slug&gt;".</summary>
        public string Fonte { get; private set; } = "procedural";

        /// <summary>O quadro em que a mao chega a frente. E' aqui que nasce o projetil.</summary>
        public event Action CastFired;

        // procedural
        Transform _rig, _raiz, _tronco, _bracoE, _bracoD, _pernaE, _pernaD, _maoDEsfera;
        readonly List<Material> _tint = new List<Material>();

        // externo (Animation legado: o .glb importado como Legacy traz os clipes por nome; sem Controller asset)
        Animation _anim;
        readonly Dictionary<Clipe, string> _clipesExternos = new Dictionary<Clipe, string>();

        // estado
        Clipe _clipe = Clipe.Idle;
        float _t, _ms, _blend = 1f, _blendDur = BlendIdle;
        bool _correndo, _castDisparado;
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
            // O importador traz tudo sem laco: run tocava tres passos e congelava (patinacao, 25/08).
            foreach (KeyValuePair<Clipe, string> kv in _clipesExternos)
                anim[kv.Value].wrapMode = PoseMago.Laco(kv.Key) ? WrapMode.Loop : WrapMode.Once;
            anim.playAutomatically = false;
            _anim = anim;

            // materiais proprios (o tint de um bot nao pode pintar o player) + metal domado
            Bounds b = new Bounds(inst.transform.position, Vector3.zero);
            bool temBounds = false;
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.materials;   // instancia
                foreach (Material m in mats) { MaterialMago.Domar(m); _tint.Add(m); }
                if (!temBounds) { b = r.bounds; temBounds = true; } else b.Encapsulate(r.bounds);
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

        public void Play(Clipe c)
        {
            // laco igual ao atual: nada (SetVelocidade chama todo frame; reiniciar aqui travaria o blend em t=0)
            if (PoseMago.Laco(c) && c == _clipe) return;
            Clipe pedido = c;
            if (_anim != null && !_clipesExternos.ContainsKey(c)) c = PoseMago.Substituto(c);
            _de = _aplicada;
            _blend = 0f;
            _blendDur = c == Clipe.Cast ? BlendCast : c == Clipe.Run ? BlendRun : c == Clipe.Idle ? BlendIdle : BlendOutro;
            _clipe = c;
            _t = 0f;
            // PEDIDO, nunca o substituto: "pegar" caindo em "cast" NAO pode virar tiro (achado de 26/08).
            _castDisparado = pedido != Clipe.Cast;
            if (_anim != null)
            {
                string real = _clipesExternos[c];
                _anim.Stop(real);   // play() com o clipe corrente nao reinicia: parar antes e' o que reinicia o cast
                _anim.Play(real);
            }
        }

        /// <summary>Velocidade real do pawn -> Idle/Run com histerese + cadencia da passada (lei da patinacao).</summary>
        public void SetVelocidade(float ms)
        {
            _ms = float.IsNaN(ms) ? 0f : Mathf.Abs(ms);
            _correndo = PoseMago.Correndo(_correndo, _ms);
            if (_clipe == Clipe.Idle || _clipe == Clipe.Run) Play(Locomocao());
        }

        Clipe Locomocao() => _correndo ? Clipe.Run : Clipe.Idle;

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
            float vel = _clipe == Clipe.Run ? PoseMago.EscalaDeCorrida(_ms) : 1f;
            _t += dt * vel;
            if (_anim != null && _clipe == Clipe.Run) _anim[_clipesExternos[Clipe.Run]].speed = vel;
            if (_anim != null && _clipe == Clipe.Cast) _t = _anim[_clipesExternos[Clipe.Cast]].time;   // o relogio e' o do clipe real

            if (!_castDisparado && _t >= PoseMago.CastFireT)
            {
                _castDisparado = true;
                CastFired?.Invoke();
            }
            // Disparo unico acabou: volta ao idle/run. E' o blend de volta que ABAIXA o braco (DIRECAO.md).
            bool acabou = _anim != null ? !_anim.IsPlaying(_clipesExternos[_clipe]) : _t >= PoseMago.Duracao(_clipe);
            if (!PoseMago.Laco(_clipe) && acabou) { Play(Locomocao()); return; }

            if (_anim != null) return;
            if (_blend < 1f) _blend = Mathf.Min(1f, _blend + dt / _blendDur);
            PoseCorpo alvo = PoseMago.Pose(_clipe, _t);
            _aplicada = _blend < 1f ? PoseMago.Lerp(_de, alvo, _blend) : alvo;
            Aplicar(_aplicada);
        }

        void Aplicar(PoseCorpo p)
        {
            if (_raiz == null) return;
            _raiz.localPosition = new Vector3(0f, p.BobY, 0f);
            _raiz.localRotation = Quaternion.Euler(p.Raiz);
            _tronco.localRotation = Quaternion.Euler(p.Tronco);
            Cabeca.localRotation = Quaternion.Euler(p.Cabeca);
            _bracoE.localRotation = Quaternion.Euler(p.BracoE);
            _bracoD.localRotation = Quaternion.Euler(p.BracoD);
            _pernaE.localRotation = Quaternion.Euler(p.PernaE);
            _pernaD.localRotation = Quaternion.Euler(p.PernaD);
            _maoDEsfera.localScale = Vector3.one * 0.11f * p.MaoD;
        }
    }
}
