using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Arkana.Core;

namespace Arkana.EditorTools
{
    /// <summary>
    /// O MECANIM DOS MAGOS (BLOCO C, 04/10/2026): gera `Resources/mixamo-mago.controller` e `Resources/mixamo-tronco.mask` a
    /// partir dos clipes `Resources/mixamo-*.fbx` (design/pipeline/MIXAMO.md). Maquina ENXUTA e SEM TRANSICOES: quem escolhe
    /// o estado e' o codigo (MagoMecanim, CrossFade por nome), como o legado sempre fez — nada de teia de setas.
    /// Camada BASE (corpo inteiro): Locomocao = Blend Tree 2D pela velocidade NO CORPO (VelX lado, VelZ frente), cada clipe
    /// na posicao da propria velocidade de raiz (a passada casa com o chao em qualquer direcao); a cadencia acima do mais
    /// rapido sai do parametro Cadencia. Camada TRONCO (mascara da cintura para cima): magias e golpes por cima da passada.
    /// Rodar de novo apaga e refaz (os ids internos mudam: commitar so' quando mudar de proposito).
    /// </summary>
    public static class ControladorHumanoide
    {
        public const string Pasta = "Assets/_Arkana/Resources/";
        public const string Controlador = Pasta + "mixamo-mago.controller";
        public const string Mascara = Pasta + "mixamo-tronco.mask";

        /// <summary>Estado da camada BASE -> clipe (nome do arquivo sem "mixamo-"). O MagoMecanim usa os MESMOS nomes.</summary>
        public static readonly string[,] Base =
        {
            { "Pulo", "standing-jump-running" }, { "Ar", "queda-no-ar" }, { "Pouso", "standing-land-to-standing-idle" },
            { "Derrubado", "derrubado" }, { "Rastejar", "rastejar" }, { "Levantar", "levantar" },
            { "Boiar", "boiar" }, { "Pegar", "pegar" },
            { "Vaultar", "pular-obstaculo-1-mao" }, { "Escalar", "escalar-beirada-agachar" },
        };

        /// <summary>Estado da camada TRONCO -> clipe.</summary>
        public static readonly string[,] Tronco =
        {
            { "Cast", "standing-1h-magic-attack-01" }, { "Tatica", "standing-2h-magic-attack-01" },
            { "Suprema", "standing-2h-magic-area-attack-01" },
            { "GolpeFrente", "standing-react-small-from-front" }, { "GolpeTras", "standing-react-small-from-back" },
            { "GolpeEsq", "standing-react-small-from-left" }, { "GolpeDir", "standing-react-small-from-right" },
        };

        /// <summary>Velocidade de cada estado (o gesto cabe no tempo do jogo; levantar e pegar nao seguram o corpo).</summary>
        public static float Velocidade(string estado)
        {
            switch (estado)
            {
                case "Cast": return Balance.Anim.CastVel;
                case "Tatica": return 1.5f;
                case "Suprema": return 1.3f;
                case "Levantar": return 2f;
                case "Pegar": return 2.5f;
                case "Derrubado": return 1.3f;
                case "Pouso": return 1.4f;
                // o clipe cabe no relogio do jogo (Balance.Move.EscaladaVaultS / EscaladaSubirS)
                case "Vaultar": return Clipe("pular-obstaculo-1-mao").length / Balance.Move.EscaladaVaultS;
                case "Escalar": return Clipe("escalar-beirada-agachar").length / Balance.Move.EscaladaSubirS;
                default: return estado.StartsWith("Golpe") ? 1.6f : 1f;
            }
        }

        /// <summary>A passada: (clipe, velocidade de reserva no corpo em m/s normalizados, direcao). A posicao no blend e' a
        /// velocidade de raiz MEDIDA no clipe; a reserva so' vale se o clipe vier parado no lugar.</summary>
        /// A PASSADA E' A GENERICA do Mixamo ("loc-*": Unarmed + Left/Right Strafe + Standard Sprint), nao a do pacote de mago:
        /// a do pacote anda com o braco direito jogado PARA TRAS (medido igual no X Bot de origem: retarget fiel), e na camera
        /// do jogo, atras do ombro, esse braco aparece ERGUIDO AO LADO DA CABECA parado e correndo (folha 61, 04/10). O pacote
        /// segue nas magias, golpes, pulo, derrubado. VETAVEL: trocar os nomes aqui e rodar o menu.
        static readonly (string clipe, float reserva, Vector2 dir)[] Passada =
        {
            ("loc-andar-frente", 1.4f, Vector2.up), ("loc-andar-tras", 1.2f, Vector2.down),
            ("loc-andar-esq", 1.3f, Vector2.left), ("loc-andar-dir", 1.3f, Vector2.right),
            ("loc-correr-frente", 3.6f, Vector2.up), ("loc-correr-tras", 3.0f, Vector2.down),
            ("loc-correr-esq", 3.2f, Vector2.left), ("loc-correr-dir", 3.2f, Vector2.right),
            ("loc-sprint", 5.6f, Vector2.up),
        };

        public static AnimationClip Clipe(string nome)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Pasta + "mixamo-" + nome + ".fbx"))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
            return null;
        }

        [MenuItem("Arkana/Gerar controlador humanoide (Mixamo)")]
        public static void Gerar()
        {
            AssetDatabase.Refresh();
            var rel = new StringBuilder("ARKANA MIXAMO: clipe | s | laco | velocidade de raiz (x, z) | movimento humano\n");

            var mask = new AvatarMask { name = "mixamo-tronco" };
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            foreach (AvatarMaskBodyPart b in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm,
                         AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers,
                         AvatarMaskBodyPart.LeftHandIK, AvatarMaskBodyPart.RightHandIK })
                mask.SetHumanoidBodyPartActive(b, true);
            AssetDatabase.DeleteAsset(Mascara);
            AssetDatabase.CreateAsset(mask, Mascara);

            AssetDatabase.DeleteAsset(Controlador);
            AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(Controlador);
            ac.AddParameter("VelX", AnimatorControllerParameterType.Float);
            ac.AddParameter("VelZ", AnimatorControllerParameterType.Float);
            ac.AddParameter(new AnimatorControllerParameter { name = "Cadencia", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });

            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            var bt = new BlendTree
            {
                name = "Locomocao", blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "VelX", blendParameterY = "VelZ", useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(bt, ac);
            AnimationClip idle = Exigir("loc-parado", rel);
            bt.AddChild(idle, Vector2.zero);
            foreach (var p in Passada)
            {
                AnimationClip c = Exigir(p.clipe, rel);
                Vector2 v = new Vector2(c.averageSpeed.x, c.averageSpeed.z);
                // clipe parado no lugar (ou de raiz torta): a reserva na direcao do nome
                if (v.magnitude < 0.2f || Vector2.Dot(v.normalized, p.dir) < 0.7f) v = p.dir * p.reserva;
                bt.AddChild(c, v);
            }
            AnimatorState loc = sm.AddState("Locomocao");
            loc.motion = bt;
            loc.speedParameterActive = true;
            loc.speedParameter = "Cadencia";
            sm.defaultState = loc;
            for (int i = 0; i < Base.GetLength(0); i++)
            {
                AnimatorState s = sm.AddState(Base[i, 0]);
                s.motion = Exigir(Base[i, 1], rel);
                s.speed = Velocidade(Base[i, 0]);
                if (Base[i, 0] == "Rastejar") { s.speedParameterActive = true; s.speedParameter = "Cadencia"; }
            }

            var trc = new AnimatorStateMachine { name = "Tronco", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(trc, ac);
            trc.defaultState = trc.AddState("Vazio");
            for (int i = 0; i < Tronco.GetLength(0); i++)
            {
                AnimatorState s = trc.AddState(Tronco[i, 0]);
                s.motion = Exigir(Tronco[i, 1], rel);
                s.speed = Velocidade(Tronco[i, 0]);
            }
            // peso 0: o MagoMecanim acende a camada so' enquanto ha' gesto (estado vazio numa camada Override nao e' "nada")
            ac.AddLayer(new AnimatorControllerLayer
            {
                name = "Tronco", stateMachine = trc, avatarMask = mask, defaultWeight = 0f,
                blendingMode = AnimatorLayerBlendingMode.Override,
            });

            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/mixamo-clipes.txt", rel.ToString());
            Debug.Log("ARKANA MIXAMO: controlador gerado com " + bt.children.Length + " passadas, " + Base.GetLength(0) + " estados base, "
                + Tronco.GetLength(0) + " de tronco");
        }

        static AnimationClip Exigir(string nome, StringBuilder rel)
        {
            AnimationClip c = Clipe(nome);
            if (c == null) throw new System.Exception("ARKANA MIXAMO: falta o clipe mixamo-" + nome + ".fbx (importado como Humanoid?)");
            rel.AppendLine(nome + " | " + c.length.ToString("F2") + " | " + (c.isLooping ? "laco" : "uma vez") + " | ("
                + c.averageSpeed.x.ToString("F2") + ", " + c.averageSpeed.z.ToString("F2") + ") | " + c.isHumanMotion);
            return c;
        }
    }
}
