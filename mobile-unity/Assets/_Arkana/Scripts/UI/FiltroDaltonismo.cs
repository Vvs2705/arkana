using UnityEngine;

namespace Arkana.UI
{
    /// <summary>
    /// Modo daltonismo (GDD §12) por REMAP DE COR na CPU: daltonizacao (simula o que o olho perde e devolve
    /// a diferenca nos canais que ele ainda enxerga). Aplica-se a toda cor que a UI pinta via Estilo.CorElemento
    /// e Hud — o passe de tela inteira ficaria em shader URP (custo e complexidade); ponytail: remap primeiro,
    /// passe de tela quando o aparelho pedir.
    /// Modos: 0 Nenhum, 1 Protanopia, 2 Deuteranopia, 3 Tritanopia (mesma ordem de Textos.CFG_DALTONISMOS).
    /// </summary>
    public static class FiltroDaltonismo
    {
        public static int Modo = 0;
        public static readonly string[] Nomes = { "Nenhum", "Protanopia", "Deuteranopia", "Tritanopia" };

        public static Color Aplicar(Color c) => Corrigir(c, Modo);

        /// <summary>Conta pura: cor de entrada -> cor daltonizada para o modo.</summary>
        public static Color Corrigir(Color c, int modo)
        {
            if (modo <= 0 || modo > 3) return c;
            Vector3 v = new Vector3(c.r, c.g, c.b);
            Vector3 sim;
            Vector3 corr;
            switch (modo)
            {
                case 1: // protanopia (sem cone L)
                    sim = Mul(v, 0.152286f, 1.052583f, -0.204868f, 0.114503f, 0.786281f, 0.099216f, -0.003882f, -0.048116f, 1.051998f);
                    break;
                case 2: // deuteranopia (sem cone M)
                    sim = Mul(v, 0.367322f, 0.860646f, -0.227968f, 0.280085f, 0.672501f, 0.047413f, -0.011820f, 0.042940f, 0.968881f);
                    break;
                default: // tritanopia (sem cone S)
                    sim = Mul(v, 1.255528f, -0.076749f, -0.178779f, -0.078411f, 0.930809f, 0.147602f, 0.004733f, 0.691367f, 0.303900f);
                    break;
            }
            Vector3 err = v - sim;
            // desvio: para onde jogar o erro (canais que o olho AINDA enxerga)
            if (modo == 3)
                corr = new Vector3(err.x + 0.7f * err.z, err.y + 0.7f * err.z, 0f);
            else
                corr = new Vector3(0f, 0.7f * err.x + err.y, 0.7f * err.x + err.z);
            Vector3 o = v + corr;
            return new Color(Mathf.Clamp01(o.x), Mathf.Clamp01(o.y), Mathf.Clamp01(o.z), c.a);
        }

        // Matriz 3x3 por LINHAS (r' = a*r + b*g + c*b ...).
        static Vector3 Mul(Vector3 v, float a, float b, float c, float d, float e, float f, float g, float h, float i)
        {
            return new Vector3(
                a * v.x + b * v.y + c * v.z,
                d * v.x + e * v.y + f * v.z,
                g * v.x + h * v.y + i * v.z);
        }
    }
}
