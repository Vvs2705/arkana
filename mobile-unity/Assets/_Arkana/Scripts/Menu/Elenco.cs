using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Arkana.Characters;
using Arkana.Core;

namespace Arkana.Menu
{
    /// <summary>
    /// A vitrine dos 20 magos: a MESMA grade da selecao (uma tela, dois lugares, zero divergencia) e as leituras puras
    /// que o menu precisa (quem tem kit, ordem 01..20, o texto do cartao). Os dados moram em Core.Kits e na
    /// IdentidadeMago; nada aqui e' inventado (a "Funcao" da ficha ainda nao tem dado: fica fora, nunca "a definir").
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

        /// <summary>"FOGO · Robusto · 1,78 m" (ficha da IdentidadeMago). Slug desconhecido cai no mago generico, nunca vazio.</summary>
        public static string Porte(string slug)
        {
            IdentidadeMago id = IdentidadeMago.De(slug);
            string el;
            if (!Textos.HudElementos.TryGetValue(Elementos.Id(id.Elemento), out el)) el = Elementos.Nome(id.Elemento);
            // ponytail: o nome do enum vai direto para a tela (ja' e' portugues); tabela em Textos quando o EN entrar
            return string.Format(Textos.SelPorte, el, id.Silhueta, Num(id.AlturaM, "0.00"));
        }

        /// <summary>O kit em duas linhas (recarga da tatica, carga da suprema) ou "KIT EM BREVE" + vazio para quem nao tem.</summary>
        public static string[] Kit(string slug)
        {
            Kits.KitDef k = Kits.De(slug);
            if (!k.Implementado) return new[] { Textos.SelKitEmBreve, "" };
            return new[] { string.Format(Textos.SelTatica, Num(k.TaticaCd, "0.#")), string.Format(Textos.SelSuprema, Num(k.SupremaCarga, "0.#")) };
        }

        /// <summary>Numero com virgula (PT-BR) sem depender da cultura do aparelho.</summary>
        static string Num(float v, string formato) => v.ToString(formato, CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
