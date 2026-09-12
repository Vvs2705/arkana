using NUnit.Framework;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// A sombra de jogo comercial (onda 5C): atlas 4096 em 4 cascatas, macia, e o SSAO no renderer — no asset URP ATIVO.
    /// Sem isto a sombra do mago volta a sair em degraus (foto 24, 12/09), e o APK perde as variantes _SHADOWS_SOFT e o
    /// shader do SSAO: o stripping do build segue o asset, nao a luz nem o codigo.
    /// </summary>
    public class WorldSombraTests
    {
        [Test]
        public void AssetAtivo_Atlas4096_QuatroCascatas_Macia_ComSsao()
        {
            // DEFEITO: o asset de 12/09 devolvia "2048px x1 dura sem-ssao" (a luz pedia Soft e o asset ignorava)
            Assert.AreEqual("4096px x4 macia ssao", Sol.Estado(),
                "URP_Base / URP_Base_Renderer desmontados: resolucao, cascatas, sombra macia ou SSAO");
        }
    }
}
