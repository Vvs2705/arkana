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
        void OnPreprocessTexture()
        {
            var imp = (TextureImporter)assetImporter;
            string p = assetPath.Replace('\\', '/');

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
