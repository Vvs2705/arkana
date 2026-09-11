namespace Arkana.Core
{
    /// <summary>
    /// PRODUTO UNICO de velocidade: base x terreno x status x postura. Ninguem escreve velocidade por fora.
    /// Cada fator tem PISO (nunca 0): a lama e' 0.55, a agua 0.4x — um fator zerado (ou NaN) travaria o mago
    /// no lugar sem ninguem saber de onde veio.
    /// </summary>
    public static class Velocidade
    {
        public const float Piso = 0.05f;

        public static float Produto(float baseMs, float terreno, float status, float postura)
        {
            return Fator(baseMs) * Fator(terreno) * Fator(status) * Fator(postura);
        }

        // `f > Piso` e' falso para NaN: a mesma guarda do dano (`!(x > 0)`), aqui virando piso em vez de 0.
        private static float Fator(float f) => f > Piso ? f : Piso;
    }
}
