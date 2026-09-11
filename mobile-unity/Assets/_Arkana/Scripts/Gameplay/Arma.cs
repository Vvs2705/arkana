using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>O que a arma multiplica do perfil do elemento. Mesmas chaves de Balance.PerfilElemento.</summary>
    public struct ArmaSpec
    {
        public float Dmg, ManaCost, FireRate, ProjectileSpeed, Range;
    }

    public sealed class ArmaDef
    {
        public string Id, Nome, Raridade;
        public float Dmg, FireRate, ProjectileSpeed, Range, ManaCost, SupremaBonus;
        public int Elementos, Runas;
    }

    /// <summary>
    /// ARMAS ARCANAS — os DADOS (GDD §16.2). personagem = habilidades; ELEMENTO = perfil (Balance);
    /// ARMA = TIER que MULTIPLICA o perfil. Multiplicador e nao numero absoluto: Balance e' o dono unico
    /// dos numeros, e a arma so' diz "quanto do perfil". A Luva Comum e' a linha de base (1.0).
    /// Ids herdados do Godot ("varinha"/"cajado"/"manopla") — viajam no Bus e nos saves.
    /// </summary>
    public static class Arma
    {
        public const string VARINHA = "varinha";
        public const string CAJADO = "cajado";
        public const string MANOPLA = "manopla";

        /// <summary>Ordem de tier (o loot decide upgrade por aqui).</summary>
        public static readonly string[] TIERS = { VARINHA, CAJADO, MANOPLA };

        public static readonly Dictionary<string, ArmaDef> ARMAS = new Dictionary<string, ArmaDef>
        {
            { VARINHA, new ArmaDef { Id = VARINHA, Nome = "Luva Comum", Raridade = "comum",
                Dmg = 1f, FireRate = 1f, ProjectileSpeed = 1f, Range = 1f, ManaCost = 1f,
                Elementos = 1, Runas = 1, SupremaBonus = 1f } },
            // "alcance e dano maiores, conjuracao mais lenta": o rifle da tabela, ganha por ALCANCE.
            { CAJADO, new ArmaDef { Id = CAJADO, Nome = "Luva de Conjurador", Raridade = "raro",
                Dmg = 1.55f, FireRate = 1.45f, ProjectileSpeed = 1.25f, Range = 1.7f, ManaCost = 1.3f,
                Elementos = 1, Runas = 2, SupremaBonus = 1.25f } },
            // LENDARIA, so' no Bau. Estalar de dedos: sem preparo; o limitador e' a MANA (1.45x), nunca o dano.
            { MANOPLA, new ArmaDef { Id = MANOPLA, Nome = "Manopla", Raridade = "lendaria",
                Dmg = 1.25f, FireRate = 0.80f, ProjectileSpeed = 1.15f, Range = 1.15f, ManaCost = 1.45f,
                Elementos = 2, Runas = 3, SupremaBonus = 1.1f } },
        };

        /// <summary>Pares FIXOS da manopla (GDD §16.2). O portador nao escolhe: a manopla que achou manda.</summary>
        public static readonly Elemento[][] PARES_MANOPLA =
        {
            new[] { Elemento.Fogo, Elemento.Vento },   // fogo alimentado por vento
            new[] { Elemento.Agua, Elemento.Raio },    // conducao: molha e eletrocuta
            new[] { Elemento.Terra, Elemento.Fogo },   // muro e brasa
        };

        /// <summary>RARIDADE = COR + FORMA (GDD §10): daltonico le' a forma. Feixe = altura do farol no chao.</summary>
        public static Color Cor(string armaId)
        {
            switch (Raridade(armaId))
            {
                case "raro": return new Color32(0x6C, 0x8C, 0xFF, 255);
                case "lendaria": return new Color32(0xFF, 0xC5, 0x3D, 255);
                default: return new Color32(0xBF, 0xD4, 0xE8, 255);
            }
        }

        public static string Forma(string armaId)
        {
            switch (Raridade(armaId))
            {
                case "raro": return "losango";
                case "lendaria": return "triangulo";
                default: return "circulo";
            }
        }

        public static float Feixe(string armaId)
        {
            switch (Raridade(armaId))
            {
                case "raro": return 2.6f;
                case "lendaria": return 3.6f;
                default: return 1.9f;
            }
        }

        public static bool Existe(string armaId) => armaId != null && ARMAS.ContainsKey(armaId);

        public static ArmaDef Dados(string armaId)
        {
            ArmaDef d;
            return Existe(armaId) && ARMAS.TryGetValue(armaId, out d) ? d : ARMAS[VARINHA];
        }

        /// <summary>Posicao na escada de poder. -1 = desconhecido (e maos nuas).</summary>
        public static int Tier(string armaId) => System.Array.IndexOf(TIERS, armaId);

        public static string Raridade(string armaId) => Dados(armaId).Raridade;

        /// <summary>O NUMERO QUE O DISPARO USA: perfil do elemento x tier da arma.</summary>
        public static ArmaSpec Spec(Elemento el, string armaId = VARINHA)
        {
            Balance.PerfilElemento b = Balance.Perfil(el);
            ArmaDef a = Dados(armaId);
            return new ArmaSpec
            {
                Dmg = b.Dmg * a.Dmg,
                ManaCost = b.ManaCost * a.ManaCost,
                FireRate = b.FireRate * a.FireRate,
                ProjectileSpeed = b.ProjectileSpeed * a.ProjectileSpeed,
                Range = b.Range * a.Range,
            };
        }
    }
}
