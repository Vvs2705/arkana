using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Characters
{
    /// <summary>O que o jogador le' a 20 m: porte (altura) e silhueta. Espelho de "Aparencia fisica" das fichas.</summary>
    public enum Silhueta { Esguio, Robusto, Baixo, Alto, Flutuante }

    /// <summary>
    /// IDENTIDADE VISUAL dos 20 magos — espelho de mobile-godot/godot/characters/mage_identity.gd,
    /// com os numeros tirados de design/personagens/NN-*.md ("## Paleta" e "## Aparencia fisica") — e o CORPO que a ficha
    /// fixa fora do kit (vida base: vale para player e bot, que nao tem passiva).
    /// DADOS, nao malhas: um corpo base, 20 fichas. Classe PURA (testa sem cena).
    /// </summary>
    public sealed class IdentidadeMago
    {
        public string Slug, Nome;
        /// <summary>Metros, da ficha. E' o que escala o esqueleto.</summary>
        public float AlturaM;
        /// <summary>Metros acima do chao para quem nunca pousa (Pip voa a ~1,2 m).</summary>
        public float FlutuaM;
        /// <summary>Vida com que o corpo nasce (Pawn.Montar), da ficha: a Pip tem 55 ("a mais fragil do elenco"); o resto, a
        /// regua (Balance.Player.Hp). O escudo nao muda: e' do nivel (GDD §5).</summary>
        public float VidaBase = Balance.Player.Hp;
        /// <summary>Hex sem '#'. Primaria = manto (o que SetTint sobrescreve); Marca = a que brilha.</summary>
        public string CorPrimaria, CorSecundaria, CorMarca;
        public Elemento Elemento;
        public Silhueta Silhueta;

        /// <summary>Altura do corpo base procedural (mage_identity.gd: REF_HEIGHT).</summary>
        public const float AlturaRef = 1.80f;

        static readonly Dictionary<string, IdentidadeMago> _tabela = new Dictionary<string, IdentidadeMago>();

        /// <summary>Ficha do slug. Desconhecido devolve o mago generico (azul-noite, 1,80 m): fiacao defensiva.</summary>
        public static IdentidadeMago De(string slug)
        {
            IdentidadeMago i;
            if (slug != null && _tabela.TryGetValue(slug, out i)) return i;
            return Nova(slug ?? "", "?", AlturaRef, "1B2440", "F0C75E", "F0C75E", Elemento.Fogo, Silhueta.Esguio);
        }

        public static IdentidadeMago[] Todas()
        {
            IdentidadeMago[] r = new IdentidadeMago[Kits.Slugs.Length];
            for (int k = 0; k < r.Length; k++) r[k] = De(Kits.Slugs[k]);
            return r;
        }

        /// <summary>"FF5A2A" ou "#FF5A2A" -> Color. Invalido devolve magenta (o erro aparece na tela, nunca em silencio).</summary>
        public static Color Cor(string hex)
        {
            if (!HexValido(hex)) return Color.magenta;
            string h = hex.TrimStart('#');
            return new Color(Canal(h, 0), Canal(h, 2), Canal(h, 4), 1f);
        }

        public static bool HexValido(string hex)
        {
            if (hex == null) return false;
            string h = hex.TrimStart('#');
            if (h.Length != 6) return false;
            foreach (char c in h) if (!System.Uri.IsHexDigit(c)) return false;
            return true;
        }

        static float Canal(string h, int i) => System.Convert.ToInt32(h.Substring(i, 2), 16) / 255f;

        /// <summary>
        /// LARGURA da silhueta (o "bulk" do Godot): vai no manto/tronco, nunca como escala nao-uniforme
        /// no rig — escala nao-uniforme no pai cisalha todo filho rotacionado (selftest.gd).
        /// </summary>
        public static float Largura(Silhueta s)
        {
            switch (s)
            {
                case Silhueta.Robusto: return 1.30f;
                case Silhueta.Baixo: return 1.12f;
                case Silhueta.Esguio: return 0.92f;
                case Silhueta.Flutuante: return 0.85f;
                default: return 1.0f;   // Alto: a altura ja' fala
            }
        }

        static IdentidadeMago Nova(string slug, string nome, float altura, string p, string s, string m, Elemento e, Silhueta sil, float flutua = 0f)
        {
            return new IdentidadeMago
            {
                Slug = slug, Nome = nome, AlturaM = altura, FlutuaM = flutua,
                CorPrimaria = p, CorSecundaria = s, CorMarca = m, Elemento = e, Silhueta = sil,
            };
        }

        static void Add(string slug, float altura, string p, string s, string m, Elemento e, Silhueta sil, float flutua = 0f,
            float vida = Balance.Player.Hp)
        {
            IdentidadeMago i = Nova(slug, Kits.De(slug).Nome, altura, p, s, m, e, sil, flutua);
            i.VidaBase = vida;
            _tabela[slug] = i;
        }

        static IdentidadeMago()
        {
            // slug, altura(ficha), manto, debrum, marca(emissiva), elemento afim, silhueta
            Add("01-pyra",         1.78f, "2B2E63", "A8763E", "FF5A2A", Elemento.Fogo,  Silhueta.Robusto);   // manopla esquerda 2x o braco
            Add("02-ceifadora",    1.82f, "1A1A22", "F0C75E", "8A5CF0", Elemento.Vento, Silhueta.Esguio);
            Add("03-veu",          1.58f, "2B2E63", "E8E6F0", "4A6FA5", Elemento.Vento, Silhueta.Baixo);
            Add("04-corvus",       1.85f, "6B5138", "C2452D", "8FE8C9", Elemento.Vento, Silhueta.Alto);      // curvado, mas alto
            Add("05-corvomante",   1.80f, "4A4A50", "2E8B57", "2E8B57", Elemento.Vento, Silhueta.Esguio);
            Add("06-olho-de-eter", 1.83f, "C24B8E", "F0C75E", "8A5CF0", Elemento.Vento, Silhueta.Esguio);
            Add("07-vitalis",      1.68f, "F5F2E8", "F0C75E", "2AA7FF", Elemento.Agua,  Silhueta.Baixo);
            Add("08-ilusionista",  1.76f, "6B3FA0", "F0C75E", "D8D8E0", Elemento.Raio,  Silhueta.Esguio);
            Add("09-vex",          1.90f, "5A8A3C", "A8763E", "B8D8C0", Elemento.Terra, Silhueta.Robusto);
            Add("10-tessa",        1.62f, "2B2E63", "B07030", "F5D90A", Elemento.Raio,  Silhueta.Baixo);
            Add("11-aelion",       1.95f, "2B2E63", "F0C75E", "8A5CF0", Elemento.Vento, Silhueta.Alto);
            Add("12-umbra",        1.70f, "3A3A46", "E8E6F0", "8A5CF0", Elemento.Vento, Silhueta.Esguio);
            Add("13-brok",         1.40f, "A8763E", "2AA7FF", "FF5A2A", Elemento.Terra, Silhueta.Robusto);   // "largo como uma porta"
            Add("14-gromm",        2.05f, "5C6B3C", "4A5A7A", "F5D90A", Elemento.Raio,  Silhueta.Alto);
            Add("15-maris",        1.80f, "2AA7FF", "EDE8E0", "2E8B74", Elemento.Agua,  Silhueta.Esguio);
            Add("16-fizz",         0.95f, "B07030", "2AA7FF", "F5D90A", Elemento.Raio,  Silhueta.Baixo);     // o menor do elenco
            Add("17-sylva",        1.75f, "3E7A3A", "E48AB0", "F0C75E", Elemento.Terra, Silhueta.Esguio);
            Add("18-basalto",      2.30f, "4A4A50", "F0C75E", "FF5A2A", Elemento.Terra, Silhueta.Alto);      // golem: o maior alvo do jogo
            Add("19-noctus",       1.88f, "8B1E2E", "E8E4EC", "8A5CF0", Elemento.Vento, Silhueta.Esguio);
            Add("20-pip",          0.60f, "2AA7FF", "8A5CF0", "F5D90A", Elemento.Raio,  Silhueta.Flutuante, 1.2f, 55f); // nunca pousa; 55 de vida
        }
    }
}
