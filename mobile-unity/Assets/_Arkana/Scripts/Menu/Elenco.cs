using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Menu
{
    /// <summary>
    /// A vitrine dos 20 magos: a MESMA grade da selecao (uma tela, dois lugares, zero divergencia) e as leituras puras
    /// que o menu precisa (quem tem kit, ordem 01..20). Os dados moram em Core.Kits; nada aqui e' inventado.
    /// Retratos: Resources/Retratos/NN (importar de mobile-godot/godot/menu/art/NN.png, 512 px, como Sprite).
    /// </summary>
    public static class Elenco
    {
        public static SelecaoPersonagem Criar(Transform pai) => SelecaoPersonagem.Criar(pai, true);

        /// <summary>Retrato do mago ou null (a tela cai no quadrado da cor do mago).</summary>
        public static Sprite Retrato(string slug) => SelecaoPersonagem.Retrato(slug);

        /// <summary>Slugs em ordem 01..20 — conta pura sobre o Kits.</summary>
        public static string[] Slugs() => Kits.Slugs ?? new string[0];

        public static List<string> ComKit()
        {
            var r = new List<string>();
            foreach (var s in Slugs()) { Kits.KitDef k; if (Kits.Magos.TryGetValue(s, out k) && k != null && k.Implementado) r.Add(s); }
            return r;
        }

        /// <summary>"Pyra" para "01-pyra"; slug fora do elenco devolve o proprio slug (nunca vazio na tela).</summary>
        public static string Nome(string slug)
        {
            Kits.KitDef k;
            if (!string.IsNullOrEmpty(slug) && Kits.Magos.TryGetValue(slug, out k) && k != null && !string.IsNullOrEmpty(k.Nome)) return k.Nome;
            return slug ?? "";
        }
    }
}
