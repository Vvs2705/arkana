using UnityEditor;
using UnityEngine;

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
        // mudou a regra do material? sobe: sem isto o FBX ja' importado nao reimporta e a mudanca nao aparece
        public override uint GetVersion() => 2;

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
