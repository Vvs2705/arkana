using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Characters
{
    /// <summary>
    /// A LUVA como forma fisica na mao (DIRECAO.md §1: a arma arcana E' a luva). COR do elemento +
    /// SILHUETA por tier (GDD §10, cor e forma): comum = punho e runa; conjurador = nos de cristal
    /// nos dedos; manopla = punho longo e as DUAS gemas dos dois elementos (o portador se denuncia).
    /// Medidas em metros do corpo de referencia — pendura no pivo Mao e herda a escala do rig.
    /// </summary>
    public static class LuvaVisual
    {
        static readonly Color Couro = new Color(0.23f, 0.20f, 0.19f);

        /// <summary>Devolve a raiz da luva (filha de mao). armaId desconhecido = tier 0; sem elementos = fogo.</summary>
        public static GameObject Criar(Transform mao, string armaId, Elemento[] elementos)
        {
            int tier = Mathf.Max(0, Arma.Tier(armaId));
            Elemento e0 = elementos != null && elementos.Length > 0 ? elementos[0] : Elemento.Fogo;
            Color cor = Projetil.Tint(e0);
            float brilho = tier == 0 ? 1.6f : tier == 1 ? 2.4f : 4.0f;

            GameObject raiz = new GameObject("Luva " + (armaId ?? ""));
            raiz.transform.SetParent(mao, false);
            Transform t = raiz.transform;

            Material escuro = MaterialMago.Novo(Color.Lerp(cor, Color.black, 0.45f), brilho * 0.3f, 0.15f, 0.5f);
            Material dorso = MaterialMago.Novo(Color.Lerp(Couro, cor, 0.25f), brilho * 0.4f, 0.05f, 0.4f);
            Material runa = MaterialMago.Novo(cor, brilho, 0f, 0.5f);

            // punho/cano: mais longo por tier (a manopla sobe pelo antebraco)
            float punho = 0.10f + 0.05f * tier;
            MaterialMago.Primitivo(t, "Punho", PrimitiveType.Cylinder, new Vector3(0f, punho * 0.5f, 0f),
                new Vector3(0.13f + 0.02f * tier, punho * 0.5f, 0.13f + 0.02f * tier), escuro);
            // dorso da mao
            MaterialMago.Primitivo(t, "Dorso", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.01f),
                new Vector3(0.10f, 0.11f, 0.05f + 0.012f * tier), dorso);
            // 4 dedos
            for (int i = 0; i < 4; i++)
                MaterialMago.Primitivo(t, "Dedo" + i, PrimitiveType.Cube, new Vector3(-0.03f + 0.02f * i, -0.10f, 0.01f),
                    new Vector3(0.018f, 0.055f, 0.02f), dorso);
            Transform pol = MaterialMago.Primitivo(t, "Polegar", PrimitiveType.Cube, new Vector3(0.055f, -0.04f, 0.015f),
                new Vector3(0.02f, 0.045f, 0.02f), dorso);
            pol.localRotation = Quaternion.Euler(0f, 0f, -30f);
            // A RUNA no dorso: a cor do elemento acesa — e' o que se le' de longe
            MaterialMago.Primitivo(t, "Runa", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.04f + 0.008f * tier),
                new Vector3(0.045f, 0.045f, 0.012f), runa).localRotation = Quaternion.Euler(0f, 0f, 45f);

            if (tier >= 1)
                for (int i = 0; i < 4; i++)   // nos de cristal do conjurador sobre os dedos
                    MaterialMago.Primitivo(t, "No" + i, PrimitiveType.Cube, new Vector3(-0.03f + 0.02f * i, -0.135f, 0.02f),
                        new Vector3(0.016f, 0.02f, 0.016f), runa);
            if (tier >= 2)
                for (int i = 0; i < 2; i++)   // as 2 gemas da manopla: cada uma na cor do SEU elemento
                {
                    Elemento ei = elementos != null && elementos.Length > i ? elementos[i] : e0;
                    Material gema = MaterialMago.Novo(Projetil.Tint(ei), 5f, 0f, 0.55f);
                    MaterialMago.Primitivo(t, "Gema" + i, PrimitiveType.Sphere, new Vector3(-0.022f + 0.045f * i, 0.04f, 0.035f),
                        Vector3.one * 0.04f, gema);
                }
            return raiz;
        }
    }
}
