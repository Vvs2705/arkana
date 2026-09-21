using NUnit.Framework;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// A sombra de jogo comercial (onda 5C): atlas 4096 em 4 cascatas e MACIA — no asset URP ATIVO. Sem isto a sombra do mago
    /// volta a sair em degraus (foto 24, 12/09), e o APK perde as variantes _SHADOWS_SOFT: o stripping segue o asset.
    /// 21/09, MEDIDO no Poco F4 (BancadaDeCortes): a macia ALTA custava ~10 ms e o SSAO ~12 ms por quadro — o chao rodava a
    /// 16 FPS. Macia BAIXA e sem SSAO: 29-34 FPS. O estilo toon quase nao usava o SSAO; a macia baixa segura os degraus.
    /// </summary>
    public class WorldSombraTests
    {
        [Test]
        public void AssetAtivo_Atlas4096_QuatroCascatas_MaciaBaixa_SemSsao()
        {
            // DEFEITO: o asset de 12/09 devolvia "2048px x1 dura sem-ssao" (a luz pedia Soft e o asset ignorava); o de 19/09,
            // "macia-high ssao", derrubava o chao a 16 FPS no aparelho
            Assert.AreEqual("4096px x4 macia-low sem-ssao", Sol.Estado(),
                "URP_Base / URP_Base_Renderer desmontados: resolucao, cascatas, sombra macia (baixa) ou SSAO (desligado)");
        }
    }
}
