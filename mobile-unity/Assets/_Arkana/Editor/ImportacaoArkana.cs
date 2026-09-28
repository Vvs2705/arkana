using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Arkana.Characters;

namespace Arkana.EditorTools
{
    /// <summary>
    /// Regras de importacao por CAMINHO, para ninguem ter de lembrar de clicar no Inspector:
    /// - Resources/Retratos/NN.png -> Sprite (SelecaoPersonagem faz Resources.Load&lt;Sprite&gt;; textura crua devolve null).
    /// - Resources/detalhe-*.png   -> modulacao em espaco de mundo: Repeat (ladrilha sem costura), linear (e' multiplicador,
    ///   nao cor), mipmaps ligados (o chao a 200 m pisca sem eles).
    /// </summary>
    public sealed class ImportacaoArkana : AssetPostprocessor
    {
        /// <summary>
        /// Resources/magos/NN-slug.fbx (o elenco refeito no SITE da Meshy, 12/09): animacao LEGADA, que o Mago ja' toca por
        /// nome (Mago.MontarExterno, aliases do PoseMago) — sem Animator Controller asset para manter. Malha nao legivel
        /// (memoria), sem blendshape, materiais pela descricao do FBX (o URP gera Lit com a textura embutida).
        /// </summary>
        void OnPreprocessModel()
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/magos/")) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true;
            mi.importBlendShapes = false;
            mi.isReadable = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        // depois do preprocessador do URP (que faz o Lit): quem fala por ultimo sobre o material do mago somos nos
        public override int GetPostprocessOrder() => 100;
        // mudou a regra do material (ou do clipe)? sobe: sem isto o FBX ja' importado nao reimporta e a mudanca nao aparece
        public override uint GetVersion() => 3;

        /// <summary>
        /// PULO e ANDAR PARA TRAS (onda 15B): quem move o corpo e' a fisica do Pawn, entao o take NAO pode levar o Hips junto
        /// (medido no 01-pyra.fbx: o Regular_Jump sobe 0,43 m; o Walk_Backward anda 0,90 m por volta). O horizontal do Hips
        /// vira o do 1o quadro e o vertical nunca passa dele: agachar pode (o impulso e o amortecer do pouso ficam), subir e
        /// andar nao.
        /// </summary>
        void OnPostprocessAnimation(GameObject raiz, AnimationClip clip)
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/magos/")) return;
            Clipe? c = PoseMago.Alias(clip.name);
            if (c == Clipe.Pular || c == Clipe.AndarTras) LimparDeslocamento(raiz, clip);
        }

        /// <summary>A regra do OnPostprocessAnimation num clipe qualquer (PoseMago.SemDeslocamento + tangentes monotonas).
        /// False = sem Hips ou sem curva de posicao dele (nada a fazer).</summary>
        public static bool LimparDeslocamento(GameObject raiz, AnimationClip clip)
        {
            Transform hips = null;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name.EndsWith("Hips", System.StringComparison.Ordinal)) { hips = t; break; }
            if (hips == null || hips.parent == null) return false;
            string caminho = AnimationUtility.CalculateTransformPath(hips, raiz.transform);
            var binds = new EditorCurveBinding[3];
            var curvas = new AnimationCurve[3];
            foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(clip))
            {
                if (b.path != caminho || b.type != typeof(Transform) || !b.propertyName.Contains("LocalPosition")) continue;
                int e = "xyz".IndexOf(b.propertyName[b.propertyName.Length - 1]);
                if (e < 0) continue;
                binds[e] = b;
                curvas[e] = AnimationUtility.GetEditorCurve(clip, b);
            }
            if (curvas[0] == null && curvas[1] == null && curvas[2] == null) return false;
            // a uniao das chaves dos tres eixos (a reducao de chaves pode ter deixado cada um com as suas)
            var tempos = new SortedSet<float>();
            foreach (AnimationCurve k in curvas)
                if (k != null) foreach (Keyframe q in k.keys) tempos.Add(q.time);
            var ts = new float[tempos.Count];
            tempos.CopyTo(ts);
            Vector3 rep = hips.localPosition;   // eixo sem curva = o do repouso
            var p = new Vector3[ts.Length];
            for (int i = 0; i < ts.Length; i++)
                p[i] = new Vector3(Em(curvas[0], ts[i], rep.x), Em(curvas[1], ts[i], rep.y), Em(curvas[2], ts[i], rep.z));
            // o alto do personagem no espaco do pai do Hips (a Armature da Meshy vem girada e com escala 100)
            Vector3[] limpo = PoseMago.SemDeslocamento(p, hips.parent.InverseTransformDirection(raiz.transform.up));
            string[] nomes = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };
            for (int e = 0; e < 3; e++)
            {
                var v = new float[ts.Length];
                for (int i = 0; i < ts.Length; i++) v[i] = limpo[i][e];
                float[] tg = PoseMago.Tangentes(ts, v);
                var ks = new Keyframe[ts.Length];
                for (int i = 0; i < ts.Length; i++) ks[i] = new Keyframe(ts[i], v[i], tg[i], tg[i]);
                EditorCurveBinding b = curvas[e] != null ? binds[e] : EditorCurveBinding.FloatCurve(caminho, typeof(Transform), nomes[e]);
                AnimationUtility.SetEditorCurve(clip, b, new AnimationCurve(ks));
            }
            return true;
        }

        static float Em(AnimationCurve c, float t, float semCurva) => c != null ? c.Evaluate(t) : semCurva;

        /// <summary>
        /// O FBX da Meshy vem SEM caminho de textura (as PNGs vem soltas no zip). Convencao: NN-slug-cor.png e
        /// NN-slug-normal.png ao lado do NN-slug.fbx. Ligado AQUI, na importacao, para o _NORMALMAP ficar gravado no
        /// material — keyword ligada em runtime some no APK (a variante e' descartada no build).
        /// </summary>
        void OnPreprocessMaterialDescription(UnityEditor.AssetImporters.MaterialDescription d, Material m, AnimationClip[] clips)
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/magos/")) return;
            string raiz = p.Substring(0, p.Length - System.IO.Path.GetExtension(p).Length);
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.3f);
            context.DependsOnArtifact(raiz + "-cor.png");
            context.DependsOnArtifact(raiz + "-normal.png");
            var cor = AssetDatabase.LoadAssetAtPath<Texture2D>(raiz + "-cor.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(raiz + "-normal.png");
            if (cor != null)
            {
                m.SetTexture("_BaseMap", cor);
                // PREENCHIMENTO: de frente para a camera o mago fica contra o sol e sai escuro (foto 12-elenco de 12/09).
                // Emissao = a propria cor x 0,16: le' como luz de rebote, nao como brilho (abaixo do limiar do bloom).
                // Keyword aqui, na importacao, pelo mesmo motivo do _NORMALMAP. KNOB: por foto.
                m.SetTexture("_EmissionMap", cor);
                m.SetColor("_EmissionColor", Color.white * 0.16f);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        }

        void OnPreprocessTexture()
        {
            var imp = (TextureImporter)assetImporter;
            string p = assetPath.Replace('\\', '/');

            if (p.Contains("/Resources/magos/"))
            {
                // 2K da Meshy -> 1K: 20 magos x 2 mapas num celular; o mago ocupa ~1/4 da tela na camera de ombro
                imp.maxTextureSize = 1024;
                imp.mipmapEnabled = true;
                if (p.EndsWith("-normal.png")) imp.textureType = TextureImporterType.NormalMap;
                return;
            }

            if (p.Contains("/Resources/Retratos/"))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.maxTextureSize = 512;
                return;
            }

            string nome = System.IO.Path.GetFileNameWithoutExtension(p);
            if (p.Contains("/Resources/") && nome.StartsWith("arq-"))
            {
                // as fotos da arquitetura do kit (IlhaMestre.Peca liga pelo material "arq_<nome>"): repetem em metros
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.mipmapEnabled = true;
                imp.maxTextureSize = 1024;
                if (nome.EndsWith("-normal")) imp.textureType = TextureImporterType.NormalMap;
                return;
            }
            if (p.Contains("/Resources/") && nome.StartsWith("detalhe-"))
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = false;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.mipmapEnabled = true;
                imp.filterMode = FilterMode.Trilinear;
            }
        }
    }
}
