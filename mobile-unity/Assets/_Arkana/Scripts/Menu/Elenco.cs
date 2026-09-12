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
    /// que o cartao precisa (quem tem kit, ordem 01..20, abas por elemento, ficha, barras, habilidades). Os dados moram
    /// em Core.Kits (numeros, Implementado), na IdentidadeMago (elemento, altura) e em Textos.SelFichas (o resumo das
    /// fichas de design/personagens). Kit que ganha Implementado aparece sozinho: nada aqui lista quem tem kit.
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

        /// <summary>A aba TODOS; as outras abas sao (int)Elemento.</summary>
        public const int FiltroTodos = -1;

        /// <summary>O mago aparece na aba? TODOS mostra os 20; cada elemento, so' os afins dele (IdentidadeMago).</summary>
        public static bool NoFiltro(string slug, int filtro) => filtro == FiltroTodos || (int)IdentidadeMago.De(slug).Elemento == filtro;

        /// <summary>"FOGO", "ÁGUA"... (o nome que a HUD usa, com acento).</summary>
        public static string NomeDo(Elemento e)
        {
            string n;
            return Textos.HudElementos.TryGetValue(Elementos.Id(e), out n) ? n : Elementos.Nome(e);
        }

        public static string Titulo(string slug) => Ficha(slug)[0];
        public static string Papel(string slug) => Ficha(slug)[1];
        public static string AlcanceRotulo(string slug) => Ficha(slug)[2];

        /// <summary>0..1 na escala Textos.SelAlcances (Muito curto = 1/6 ... Muito longo = 1); fora da escala = 0.</summary>
        public static float Alcance(string slug) => (Array.IndexOf(Textos.SelAlcances, AlcanceRotulo(slug)) + 1f) / Textos.SelAlcances.Length;

        /// <summary>KNOB: a barra de PORTE vai do menor ao maior corpo do elenco (Pip 0,6 m, Basalto 2,3 m).</summary>
        public const float PorteMinM = 0.5f, PorteMaxM = 2.3f;

        /// <summary>0..1: o tamanho do alvo que o mago oferece (altura da ficha).</summary>
        public static float Porte(string slug) => Mathf.InverseLerp(PorteMinM, PorteMaxM, IdentidadeMago.De(slug).AlturaM);

        /// <summary>"1,78 m" (virgula PT-BR em qualquer cultura do aparelho).</summary>
        public static string Altura(string slug) => string.Format(Textos.SelAltura, Num(IdentidadeMago.De(slug).AlturaM, "0.00"));

        /// <summary>
        /// Tatica ou suprema no cartao: { nome, descricao, tempo }. A descricao troca cada `{chave}` pelo numero do bloco do
        /// Kits (o texto segue o balanceamento); o tempo e' a recarga/carga do kit, ou EM BREVE sem kit — nunca numero
        /// inventado. Le' Kits.De(slug).Implementado na hora: o kit que chegar aparece sem mexer aqui.
        /// </summary>
        public static string[] Habilidade(string slug, bool suprema)
        {
            string[] f = Ficha(slug);
            Kits.KitDef k = Kits.De(slug);
            string desc = f[suprema ? 6 : 4];
            var dados = suprema ? k.Suprema : k.Tatica;
            if (dados != null)
                foreach (var kv in dados) desc = desc.Replace("{" + kv.Key + "}", Num(kv.Value, "0.#"));
            string tempo = !k.Implementado ? Textos.SelEmBreve
                : string.Format(suprema ? Textos.SelCarga : Textos.SelRecarga, Num(suprema ? k.SupremaCarga : k.TaticaCd, "0.#"));
            return new[] { f[suprema ? 5 : 3], desc, tempo };
        }

        static readonly string[] SemFicha = { "", "", "", "", "", "", "" };

        /// <summary>A ficha de tela (Textos.SelFichas) ou vazia: slug fora do elenco nunca derruba o cartao.</summary>
        static string[] Ficha(string slug)
        {
            string[] f;
            return slug != null && Textos.SelFichas.TryGetValue(slug, out f) && f != null && f.Length >= SemFicha.Length ? f : SemFicha;
        }

        /// <summary>Numero com virgula (PT-BR) sem depender da cultura do aparelho.</summary>
        static string Num(float v, string formato) => v.ToString(formato, CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
