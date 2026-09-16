using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Characters
{
    /// <summary>
    /// A LUVA como forma fisica na mao (DIRECAO.md §1: a arma arcana E' a luva). E' a MESMA luva da Meshy que gira no chao
    /// (o jogador reconhece o que pegou) + a RUNA acesa na cor do elemento sobre a gema (o que se le' de longe; a manopla
    /// acende as DUAS). Sem o .glb: primitivas com a silhueta por tier (GDD §10): comum = punho e runa; conjurador = nos de
    /// cristal nos dedos; manopla = punho longo e as duas gemas.
    ///
    /// SOQUETE (onda 15A): a luva nunca pendura direto no osso. O RightHand da Meshy herda a escala 100 da Armature do FBX e a
    /// luva de 0,1 m virava blocos de 10-17 m na frente da camera (diag 46 de 16/09). A raiz da luva e' um filho do osso com a
    /// escala que CANCELA a dele (lossy da raiz = escala do corpo) e com o QUADRO DA MAO DIREITA: origem no pulso,
    /// +Y = dedos, +Z = dorso, +X = polegar.
    /// </summary>
    public static class LuvaVisual
    {
        /// <summary>KNOB: tamanho da luva (maior lado, punho incluso) em antebracos. A parte da mao (1 - PULSO) sai ~1,15 mao
        /// (a mao mede ~0,75 antebraco). Pyra: antebraco 0,247 m -> luva 0,32 m.</summary>
        public const float K_ANTEBRACO = 1.3f;
        /// <summary>KNOB: teto da luva em m num mago de 1,80 (escala com a altura): nunca tampa a tela.</summary>
        public const float TETO_M = 0.35f;
        /// <summary>m — antebraco do corpo de referencia: o procedural nao tem cotovelo para medir.</summary>
        public const float ANTEBRACO_REF = 0.26f;
        /// <summary>KNOB: onde fica o pulso no .glb, em fracao da altura a partir da base (medido nos 3: o trecho mais estreito
        /// entre o punho e a palma, 0,30-0,40). E' o deslocamento: o pulso do .glb cai na origem do osso.</summary>
        public const float PULSO = 0.35f;
        /// <summary>KNOB: giro extra em graus em volta dos dedos, se a foto mostrar a palma do lado errado.</summary>
        public const float GIRO_DEDOS = 0f;
        /// <summary>A runa sobre a gema do .glb, em fracao da altura a partir do CENTRO do modelo (medido pixel a pixel na textura
        /// emissiva: gema a 0,56-0,62 da base, x ~0, casca do dorso a 0,10-0,13). A manopla tem as duas gemas lado a lado em x.</summary>
        const float RUNA_Y = 0.09f, RUNA_Z = 0.11f, RUNA_D = 0.07f;
        static readonly float[] GEMAS_X = { 0.02f, -0.09f };
        /// <summary>m — altura da luva de primitivas (punho comum + mao) no corpo de referencia; escala para caber no tamanho.</summary>
        const float ALTURA_PRIMITIVA = 0.23f;
        /// <summary>Pivo Mao do procedural: o braco pende em -Y (dedos para baixo) e o dorso olha +Z.</summary>
        static readonly Quaternion GiroProcedural = Quaternion.Euler(0f, 0f, 180f);
        static readonly Color Couro = new Color(0.23f, 0.20f, 0.19f);

        /// <summary>Devolve a raiz da luva (filha de mao). armaId desconhecido = tier 0 (luva comum); sem elementos = fogo.</summary>
        public static GameObject Criar(Transform mao, string armaId, Elemento[] elementos)
        {
            int tier = Mathf.Max(0, Arma.Tier(armaId));
            Elemento e0 = elementos != null && elementos.Length > 0 ? elementos[0] : Elemento.Fogo;
            Color cor = Projetil.Tint(e0);
            float brilho = tier == 0 ? 1.6f : tier == 1 ? 2.4f : 4.0f;

            // O CORPO: escala do rig (a do mago inteiro) e altura. Sem mago (o loot no chao) = mundo, 1,80.
            Mago mago = mao != null ? mao.GetComponentInParent<Mago>(true) : null;
            Transform rig = mago != null ? mago.transform.Find("Rig") : null;
            float corpo = rig != null ? rig.lossyScale.x : 1f;
            float altura = mago != null ? mago.Altura * mago.transform.lossyScale.y : IdentidadeMago.AlturaRef;
            bool osso = EhOsso(mao);
            float antebraco = osso ? Vector3.Distance(mao.position, mao.parent.position) : ANTEBRACO_REF * corpo;
            float h = Mathf.Min(K_ANTEBRACO * antebraco, TETO_M * altura / IdentidadeMago.AlturaRef) / corpo;   // unidades do corpo

            GameObject raiz = new GameObject("Luva " + (armaId ?? ""));
            Transform t = raiz.transform;
            t.SetParent(mao, false);
            t.localScale = Vector3.one * (corpo / (mao != null ? mao.lossyScale.x : 1f));   // CANCELA a escala do osso
            t.localRotation = osso ? GiroDoOsso(mago, mao) : mago != null ? GiroProcedural : Quaternion.identity;

            GameObject modelo = Modelo(armaId, t, h);
            if (modelo != null)
            {
                float yc = (0.5f - PULSO) * h;
                modelo.transform.localPosition = new Vector3(0f, yc, 0f);
                // a RUNA acesa em cima da gema: a cor do .glb e' fixa, a do elemento e' esta
                Vector3 pos = new Vector3(0f, yc + RUNA_Y * h, RUNA_Z * h);
                if (tier < 2)
                    MaterialMago.Primitivo(t, "Runa", PrimitiveType.Sphere, pos, Vector3.one * RUNA_D * h, MaterialMago.Novo(cor, brilho, 0f, 0.5f));
                else
                    for (int i = 0; i < 2; i++)
                        MaterialMago.Primitivo(t, "Gema" + i, PrimitiveType.Sphere, new Vector3(GEMAS_X[i] * h, pos.y, pos.z),
                            Vector3.one * RUNA_D * h, MaterialMago.Novo(Projetil.Tint(ElementoN(elementos, i, e0)), 5f, 0f, 0.55f));
                return raiz;
            }

            Transform p = MaterialMago.Pivo(t, "Primitivas", Vector3.zero);
            p.localScale = Vector3.one * (h / ALTURA_PRIMITIVA);
            Material escuro = MaterialMago.Novo(Color.Lerp(cor, Color.black, 0.45f), brilho * 0.3f, 0.15f, 0.5f);
            Material dorso = MaterialMago.Novo(Color.Lerp(Couro, cor, 0.25f), brilho * 0.4f, 0.05f, 0.4f);
            Material runa = MaterialMago.Novo(cor, brilho, 0f, 0.5f);

            // punho/cano para -Y (o antebraco): mais longo por tier (a manopla sobe pelo antebraco)
            float punho = 0.10f + 0.05f * tier;
            MaterialMago.Primitivo(p, "Punho", PrimitiveType.Cylinder, new Vector3(0f, -punho * 0.5f, 0f),
                new Vector3(0.13f + 0.02f * tier, punho * 0.5f, 0.13f + 0.02f * tier), escuro);
            // dorso da mao
            MaterialMago.Primitivo(p, "Dorso", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.01f),
                new Vector3(0.10f, 0.11f, 0.05f + 0.012f * tier), dorso);
            // 4 dedos para +Y; polegar em +X (mao direita)
            for (int i = 0; i < 4; i++)
                MaterialMago.Primitivo(p, "Dedo" + i, PrimitiveType.Cube, new Vector3(-0.03f + 0.02f * i, 0.10f, 0.01f),
                    new Vector3(0.018f, 0.055f, 0.02f), dorso);
            Transform pol = MaterialMago.Primitivo(p, "Polegar", PrimitiveType.Cube, new Vector3(0.055f, 0.04f, 0.015f),
                new Vector3(0.02f, 0.045f, 0.02f), dorso);
            pol.localRotation = Quaternion.Euler(0f, 0f, 30f);
            // A RUNA no dorso: a cor do elemento acesa — e' o que se le' de longe
            MaterialMago.Primitivo(p, "Runa", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.04f + 0.008f * tier),
                new Vector3(0.045f, 0.045f, 0.012f), runa).localRotation = Quaternion.Euler(0f, 0f, 45f);

            if (tier >= 1)
                for (int i = 0; i < 4; i++)   // nos de cristal do conjurador sobre os dedos
                    MaterialMago.Primitivo(p, "No" + i, PrimitiveType.Cube, new Vector3(-0.03f + 0.02f * i, 0.135f, 0.02f),
                        new Vector3(0.016f, 0.02f, 0.016f), runa);
            if (tier >= 2)
                for (int i = 0; i < 2; i++)   // as 2 gemas da manopla: cada uma na cor do SEU elemento
                {
                    Material gema = MaterialMago.Novo(Projetil.Tint(ElementoN(elementos, i, e0)), 5f, 0f, 0.55f);
                    MaterialMago.Primitivo(p, "Gema" + i, PrimitiveType.Sphere, new Vector3(-0.022f + 0.045f * i, -0.04f, 0.035f),
                        Vector3.one * 0.04f, gema);
                }
            return raiz;
        }

        /// <summary>
        /// O .glb da luva (Resources/luva-&lt;id&gt;, o mesmo do chao) CENTRADO em `pai`, maior lado `tamanho`, ja' no quadro da
        /// mao direita (+Y dedos, +Z dorso, +X polegar). Devolve o no' "Giro" (o modelo e' filho dele). Sem o .glb -> null.
        /// Os 3 da Meshy vem de pe' (dedos +Y) mas nao iguais (medido nas vistas de dorso e palma; o glTFast troca o sinal de
        /// X): a manopla ja' e' direita com o dorso em +Z; a VARINHA e' ESQUERDA (dorso +Z, polegar -X: espelho em X); o
        /// CAJADO tambem e' ESQUERDO, de dorso para -Z (polegar +X: espelho em Z, que ja' vira o dorso para +Z).
        /// </summary>
        public static GameObject Modelo(string armaId, Transform pai, float tamanho)
        {
            string nome = LootVisual.ModeloDe(armaId);
            if (Resources.Load<GameObject>(nome) == null) return null;
            string id = Arma.Dados(armaId).Id;
            Transform giro = MaterialMago.Pivo(pai, "Giro", Vector3.zero);
            // ponytail: espelho = escala negativa (o Unity inverte a volta do triangulo sozinho; o material ja' e' dois lados)
            giro.localScale = new Vector3(id == Arma.VARINHA ? -1f : 1f, 1f, id == Arma.CAJADO ? -1f : 1f);
            VisualDaPartida.Modelo(nome, giro, tamanho, false);
            return giro.gameObject;
        }

        /// <summary>O pai e' o antebraco (RightForeArm da Meshy/Mixamo): a mao e' osso de verdade, com escala e eixos do FBX.
        /// O pivo Mao do procedural pende do BracoD (sem cotovelo) e o MaoD de reserva do externo pende do Rig.</summary>
        static bool EhOsso(Transform mao) =>
            mao != null && mao.parent != null && mao.parent.name.ToLowerInvariant().Contains("arm");

        /// <summary>
        /// O quadro da mao no osso da Meshy, MEDIDO na pose de BIND do proprio FBX: dedos = +Y do osso (convencao do Blender:
        /// o RightHand_End fica em +Y nos 20 magos); polegar = o que olha para a FRENTE do mago no bind (T-pose de palma para
        /// baixo e braco caido de palma para dentro, os dois poem o polegar para a frente); dorso = polegar x dedos (mao
        /// direita). Nos 19 magos de T-pose isso da' polegar ~+Z e dorso ~-X do osso (medido no FBX, 16/09); o Vitalis
        /// (braco caido no bind) tem outro rolamento — por isso mede em vez de fixar.
        /// </summary>
        static Quaternion GiroDoOsso(Mago mago, Transform mao)
        {
            Vector3 frente = FrenteNoBind(mago, mao) ?? Vector3.forward;   // sem o bind: o medido nos 19
            frente.y = 0f;   // perpendicular aos dedos
            if (frente.sqrMagnitude < 1e-4f) frente = Vector3.forward;
            Vector3 dorso = Vector3.Cross(frente.normalized, Vector3.up);
            return Quaternion.LookRotation(dorso, Vector3.up) * Quaternion.Euler(0f, GIRO_DEDOS, 0f);
        }

        /// <summary>
        /// A frente do mago no espaco do osso, na POSE DE BIND (Mesh.bindposes). O prefab NAO serve: o importador guarda nele
        /// o 1o quadro de um clipe (a "pose congelada" de 12/09; o Unity mostra o joelho da Pyra dobrado 120 graus).
        /// A malha nao anima, entao a frente do mago no espaco dela e' fixa. Null sem skin com esse osso.
        /// </summary>
        static Vector3? FrenteNoBind(Mago mago, Transform osso)
        {
            if (mago == null) return null;
            foreach (SkinnedMeshRenderer smr in mago.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int i = System.Array.IndexOf(smr.bones, osso);
                if (i < 0 || smr.sharedMesh == null) continue;
                Matrix4x4[] bind;
                // ponytail: o FBX e' importado sem Read/Write; o bind fica na CPU (o skin usa), mas se um aparelho barrar a
                // leitura, cai no giro medido dos 19 (so' o Vitalis sai girado)
                try { bind = smr.sharedMesh.bindposes; }
                catch (System.Exception) { return null; }
                if (bind == null || i >= bind.Length) return null;
                return bind[i].MultiplyVector(smr.transform.InverseTransformDirection(mago.transform.forward));
            }
            return null;
        }

        static Elemento ElementoN(Elemento[] els, int i, Elemento reserva) => els != null && els.Length > i ? els[i] : reserva;
    }
}
