using System;

namespace Arkana.Core
{
    /// <summary>Os 5 elementos (GDD §10: cor + FORMA, nunca so' cor). A ordem e' a do carrossel.</summary>
    public enum Elemento { Fogo = 0, Agua = 1, Raio = 2, Terra = 3, Vento = 4 }

    public static class Elementos
    {
        public static readonly Elemento[] Todos = { Elemento.Fogo, Elemento.Agua, Elemento.Raio, Elemento.Terra, Elemento.Vento };

        /// <summary>Ids de texto herdados do Godot ("fire", "water"...). Servem a loot, textos e saves.</summary>
        public static string Id(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fogo: return "fire";
                case Elemento.Agua: return "water";
                case Elemento.Raio: return "lightning";
                case Elemento.Terra: return "earth";
                case Elemento.Vento: return "wind";
            }
            throw new ArgumentOutOfRangeException(nameof(e));
        }

        public static bool TryParse(string id, out Elemento e)
        {
            foreach (var x in Todos)
                if (Id(x) == id) { e = x; return true; }
            e = Elemento.Fogo;
            return false;
        }

        /// <summary>Nome em portugues, maiusculo, como a HUD mostra ("FOGO").</summary>
        public static string Nome(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fogo: return "FOGO";
                case Elemento.Agua: return "AGUA";
                case Elemento.Raio: return "RAIO";
                case Elemento.Terra: return "TERRA";
                case Elemento.Vento: return "VENTO";
            }
            return "?";
        }
    }
}
