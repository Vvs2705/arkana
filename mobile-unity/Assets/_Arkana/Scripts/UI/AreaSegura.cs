using UnityEngine;

namespace Arkana.UI
{
    /// <summary>
    /// Margens da area segura em px (notch, camera, barra de gestos). Toda margem de borda da HUD passa por aqui.
    /// Origem Unity: (0,0) e' o canto inferior esquerdo da tela.
    /// </summary>
    public struct Margens
    {
        public float Esq, Topo, Dir, Baixo;
        public Margens(float esq, float topo, float dir, float baixo) { Esq = esq; Topo = topo; Dir = dir; Baixo = baixo; }
    }

    public enum Canto { InfEsq, InfDir, SupEsq, SupDir }

    public static class AreaSegura
    {
        /// <summary>Conta pura: recorte seguro + tamanho da tela -> margens (nunca negativas).</summary>
        public static Margens Calcular(Rect safe, Vector2 tela)
        {
            return new Margens(
                Mathf.Max(safe.xMin, 0f),
                Mathf.Max(tela.y - safe.yMax, 0f),
                Mathf.Max(tela.x - safe.xMax, 0f),
                Mathf.Max(safe.yMin, 0f));
        }

        public static Margens Atual() => Calcular(Screen.safeArea, new Vector2(Screen.width, Screen.height));

        /// <summary>
        /// Ancora um RectTransform num canto, DENTRO da area segura, com tamanho e folga em px.
        /// O pai deve ser o Canvas de tela cheia (escala 1).
        /// </summary>
        public static void NoCanto(RectTransform rt, Canto canto, Vector2 tamanhoPx, Vector2 folgaPx, Margens m)
        {
            Vector2 a;
            Vector2 pos;
            switch (canto)
            {
                case Canto.InfEsq: a = new Vector2(0, 0); pos = new Vector2(m.Esq + folgaPx.x, m.Baixo + folgaPx.y); break;
                case Canto.InfDir: a = new Vector2(1, 0); pos = new Vector2(-(m.Dir + folgaPx.x), m.Baixo + folgaPx.y); break;
                case Canto.SupEsq: a = new Vector2(0, 1); pos = new Vector2(m.Esq + folgaPx.x, -(m.Topo + folgaPx.y)); break;
                default: a = new Vector2(1, 1); pos = new Vector2(-(m.Dir + folgaPx.x), -(m.Topo + folgaPx.y)); break;
            }
            rt.anchorMin = a; rt.anchorMax = a; rt.pivot = a;
            rt.sizeDelta = tamanhoPx;
            rt.anchoredPosition = pos;
        }

        /// <summary>Posiciona por retangulo absoluto em px de tela (origem inferior esquerda).</summary>
        public static void NoRect(RectTransform rt, Rect r)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(r.xMin, r.yMin);
            rt.sizeDelta = new Vector2(r.width, r.height);
        }

        /// <summary>Estica ate' as bordas do pai (tela cheia).</summary>
        public static void Esticar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        /// <summary>Estica ate' as bordas do pai, recuando as margens seguras + folga em px.</summary>
        public static void EsticarDentro(RectTransform rt, Margens m, float folgaPx)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(m.Esq + folgaPx, m.Baixo + folgaPx);
            rt.offsetMax = new Vector2(-(m.Dir + folgaPx), -(m.Topo + folgaPx));
        }
    }
}
