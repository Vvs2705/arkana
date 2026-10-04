using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Characters
{
    /// <summary>
    /// O MECANIM DOS MAGOS (BLOCO C, 04/10/2026): a biblioteca HUMANOIDE do Mixamo (Resources/mixamo-*.fbx, gerada pelo
    /// ControladorHumanoide) tocando no esqueleto da Meshy por um Avatar montado em runtime (EsqueletoHumano) — um clipe
    /// serve nos 20. O Mago delega para ca' quando ha' controller e o Avatar fecha; senao segue o legado (Animation) e,
    /// sem modelo, o procedural. A ANIMACAO NAO DECIDE NADA: tiro, dano, mana e recarga sao do relogio do Pawn; isto so'
    /// mostra o pedido. Camada BASE: a passada por velocidade NO CORPO (frente/tras/lados = strafe de verdade), pulo, ar,
    /// pouso, derrubado, nado, pegar. Camada TRONCO (mascara): magias e golpes por cima da passada, as pernas nao param.
    /// </summary>
    public sealed class MagoMecanim
    {
        public const string Controlador = "mixamo-mago";
        /// <summary>O ANIMATION VALIDATION SET (dado, 04/10): o mais compacto que pisa, a mediana, o maior/mais largo.</summary>
        public static readonly string[] ValidationSet = { "16-fizz", "05-corvomante", "18-basalto" };

        /// <summary>Velocidade de raiz (normalizada pelo humano) do clipe mais rapido em cada direcao — os do
        /// Logs/mixamo-clipes.txt (sprint 4,7; corrida para tras 3,1; para os lados ~3,5). Acima disto a Cadencia acelera.</summary>
        const float MaxFrente = 4.7f, MaxTras = 3.1f, MaxLado = 3.5f;
        /// <summary>Velocidade de raiz do rastejar (m/s normalizados): a cadencia do derrubado sai dela.</summary>
        const float Rastejo = 0.2f;

        static RuntimeAnimatorController _ctrl;
        static bool _procurou;
        static readonly Dictionary<string, Avatar> _avatares = new Dictionary<string, Avatar>();

        static readonly int PVelX = Animator.StringToHash("VelX"), PVelZ = Animator.StringToHash("VelZ"),
            PCad = Animator.StringToHash("Cadencia");

        public Animator Animador { get; }
        /// <summary>O estado PEDIDO na camada base (por nome) e o gesto do tronco ("" = camada apagada).</summary>
        public string Base { get; private set; } = "Locomocao";
        public string GestoAtual { get; private set; } = "";
        /// <summary>Velocidade do corpo no referencial DELE (x direita, z frente), m/s. O Pawn escreve por quadro.</summary>
        public Vector3 VelocidadeLocal;
        public float Cadencia { get; private set; } = 1f;

        float _velocidade, _gestoAte, _pesoTronco, _noEstado;

        MagoMecanim(Animator a) { Animador = a; }

        /// <summary>Teste do caminho de RESERVA (Animation legado): liga, cria o mago, desliga.</summary>
        public static bool ForcarLegado;

        public static RuntimeAnimatorController Ctrl()
        {
            if (!_procurou) { _ctrl = Resources.Load<RuntimeAnimatorController>(Controlador); _procurou = true; }
            return _ctrl;
        }

        /// <summary>Vale para este mago? (Balance.Anim.MecanimEmTodos, senao so' o Validation Set) — e existe o controller.</summary>
        public static bool Ligado(string slug) =>
            !ForcarLegado && !string.IsNullOrEmpty(slug) && (Balance.Anim.MecanimEmTodos || System.Array.IndexOf(ValidationSet, slug) >= 0) && Ctrl() != null;

        /// <summary>Animator + Avatar humano no `modelo` (a instancia do FBX da Meshy). O Avatar e' montado UMA vez por slug
        /// (mesma hierarquia nos bots do mesmo mago). Null = o rig nao fechou: o chamador segue no legado.</summary>
        public static MagoMecanim Montar(GameObject modelo, string slug)
        {
            if (modelo == null || Ctrl() == null) return null;
            if (!_avatares.TryGetValue(slug, out Avatar av) || av == null)
            {
                av = EsqueletoHumano.Construir(modelo, out _);
                if (av == null || !av.isValid || !av.isHuman) return null;
                av.name = "avatar-" + slug;
                _avatares[slug] = av;
            }
            Animator a = modelo.GetComponent<Animator>();
            if (a == null) a = modelo.AddComponent<Animator>();
            a.avatar = av;
            a.runtimeAnimatorController = Ctrl();
            a.applyRootMotion = false;   // quem anda e' a fisica do Pawn; o andar do clipe e' so' a passada
            a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            return new MagoMecanim(a);
        }

        // ------------------------------------------------------------------ pedidos

        /// <summary>O pedido do Pawn (o mesmo Clipe do legado). Laco igual ao atual nao reinicia.</summary>
        public void Play(Clipe c)
        {
            switch (c)
            {
                case Clipe.Cast: Gesto("Cast", Balance.Anim.CastInicioN, Balance.Anim.CastSeguraS); return;
                case Clipe.Pular: Ir("Pulo", 0.08f); return;
                case Clipe.Cair: case Clipe.Planar: Ir("Ar", 0.2f); return;
                case Clipe.Pousar: Ir("Pouso", 0.1f); return;
                case Clipe.Derrubado: if (Base != "Derrubado" && Base != "Rastejar") Ir("Derrubado", 0.15f); return;
                case Clipe.Nadar: case Clipe.NadarParado: Ir("Boiar", 0.25f); return;
                case Clipe.Pegar: Ir("Pegar", 0.1f, 0.18f); return;
                default:
                    // Idle, Run, AndarTras, AndeAgachado: UM estado — o blend 2D le' a velocidade no corpo
                    if (Base == "Derrubado" || Base == "Rastejar") Ir("Levantar", 0.15f, 0.35f);   // reerguido: levanta (rapido) e volta a passada
                    else Ir("Locomocao", Base == "Pouso" ? 0.2f : 0.15f);
                    return;
            }
        }

        /// <summary>Gestos so' do Mecanim (o legado toca o cast): "tatica", "suprema". False = nao e' gesto daqui.</summary>
        public bool Gesto(string nome)
        {
            switch (nome)
            {
                case "tatica": Gesto("Tatica", 0.15f, Balance.Anim.KitSeguraS); return true;
                case "suprema": Gesto("Suprema", 0.1f, Balance.Anim.KitSeguraS); return true;
                default: return false;
            }
        }

        /// <summary>GOLPE recebido, vindo de `deLocal` (direcao da fonte no referencial do corpo): tranco curto no tronco.
        /// Uma magia em curso nao e' interrompida (o braco do jogador manda).</summary>
        public void Golpe(Vector3 deLocal)
        {
            if (GestoAtual == "Cast" || GestoAtual == "Tatica" || GestoAtual == "Suprema") return;
            string g = Mathf.Abs(deLocal.x) > Mathf.Abs(deLocal.z) ? (deLocal.x > 0f ? "GolpeDir" : "GolpeEsq") : (deLocal.z >= 0f ? "GolpeFrente" : "GolpeTras");
            Gesto(g, 0.05f, Balance.Anim.GolpeSeguraS);
        }

        public void Velocidade(float ms) { _velocidade = float.IsNaN(ms) ? 0f : Mathf.Abs(ms); }

        void Ir(string estado, float blend, float inicioN = 0f)
        {
            if (estado == Base && estado != "Pegar") return;
            Base = estado;
            _noEstado = 0f;
            Animador.CrossFadeInFixedTime(estado, blend, 0, inicioN * Duracao(estado));
        }

        /// <summary>O gesto do tronco (a velocidade de cada um mora no estado do controller: ControladorHumanoide.Velocidade).</summary>
        void Gesto(string estado, float inicioN, float segura)
        {
            GestoAtual = estado;
            _gestoAte = segura;
            // reinicio SECO por gesto (fogo continuo reinicia o braco, como no legado); a camada sobe rapido
            Animador.Play(estado, 1, inicioN);
            Animador.SetLayerWeight(1, _pesoTronco = Mathf.Max(_pesoTronco, 0.6f));
        }

        float Duracao(string estado)
        {
            // a fracao inicial vira tempo pelo clipe do estado (CrossFadeInFixedTime pede segundos)
            foreach (AnimationClip c in Animador.runtimeAnimatorController.animationClips)
                if (c != null && c.name == ClipeDoEstado(estado)) return c.length;
            return 1f;
        }

        static string ClipeDoEstado(string estado)
        {
            switch (estado)
            {
                case "Pulo": return "standing-jump-running";
                case "Pouso": return "standing-land-to-standing-idle";
                case "Derrubado": return "derrubado";
                case "Levantar": return "levantar";
                case "Pegar": return "pegar";
                default: return "";
            }
        }

        // ------------------------------------------------------------------ o quadro

        /// <summary>
        /// A passada: velocidade no corpo / escala humana = a unidade dos clipes; o blend fica dentro do clipe mais rapido
        /// da direcao e o que passar vira CADENCIA (pernas mais rapidas, ate' CadenciaMax). Pulo que acaba no ar vira o
        /// laco do ar; derrubado vira o rastejar; o gesto do tronco solta a camada quando o tempo dele acaba.
        /// </summary>
        public void Tick(float dt)
        {
            if (!(dt > 0f) || Animador == null) return;
            _noEstado += dt;
            float escala = Mathf.Max(Animador.humanScale, 0.05f);
            Vector2 v = new Vector2(VelocidadeLocal.x, VelocidadeLocal.z) / escala;
            if (float.IsNaN(v.x) || float.IsNaN(v.y)) v = Vector2.zero;
            float mag = v.magnitude;
            float max = MaxNaDirecao(v);
            Cadencia = 1f;
            if (mag > max) { Cadencia = Mathf.Min(mag / max, Balance.Anim.CadenciaMax); v *= max / mag; }
            if (Base == "Rastejar") Cadencia = Mathf.Clamp(_velocidade / (Rastejo * escala), 0f, Balance.Anim.CadenciaMax);
            Animador.SetFloat(PVelX, v.x, 0.08f, dt);
            Animador.SetFloat(PVelZ, v.y, 0.08f, dt);
            Animador.SetFloat(PCad, Cadencia);

            // pulo terminado ainda no ar -> o laco do ar; derrubado deitado -> rastejar; levantar acabou -> passada
            AnimatorStateInfo st = Animador.GetCurrentAnimatorStateInfo(0);
            if (!Animador.IsInTransition(0) && _noEstado > 0.1f)
            {
                if (Base == "Pulo" && st.IsName("Pulo") && st.normalizedTime >= 0.95f) Ir("Ar", 0.25f);
                else if (Base == "Derrubado" && st.IsName("Derrubado") && st.normalizedTime >= 0.98f) Ir("Rastejar", 0.2f);
                else if ((Base == "Levantar" || Base == "Pouso" || Base == "Pegar") && st.IsName(Base) && st.normalizedTime >= 0.97f) Ir("Locomocao", 0.2f);
            }

            // a camada do tronco: sobe no gesto, desce quando ele acaba
            if (GestoAtual != "")
            {
                _gestoAte -= dt;
                if (_gestoAte <= 0f) GestoAtual = "";
            }
            float alvo = GestoAtual != "" ? 1f : 0f;
            _pesoTronco = Mathf.MoveTowards(_pesoTronco, alvo, dt / (alvo > _pesoTronco ? 0.06f : 0.25f));
            Animador.SetLayerWeight(1, _pesoTronco);
        }

        /// <summary>O clipe mais rapido na direcao `v` (frente: sprint; tras: corrida para tras; lados: corrida de lado).</summary>
        public static float MaxNaDirecao(Vector2 v)
        {
            float mag = v.magnitude;
            if (!(mag > 1e-4f)) return MaxLado;
            float cos = v.y / mag;
            return cos >= 0f ? Mathf.Lerp(MaxLado, MaxFrente, cos) : Mathf.Lerp(MaxLado, MaxTras, -cos);
        }

        public string Estado() => "mecanim base=" + Base + " tronco=" + (GestoAtual == "" ? "-" : GestoAtual) + " peso=" + _pesoTronco.ToString("F2")
            + " cad=" + Cadencia.ToString("F2") + " escala=" + (Animador != null ? Animador.humanScale.ToString("F2") : "?");
    }
}
