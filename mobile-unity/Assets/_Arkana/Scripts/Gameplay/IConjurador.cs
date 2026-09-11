using System;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O MINIMO que o motor de kits (KitRunner) pede ao pawn que o carrega. Quem implementa: Gameplay.Pawn
    /// (raia PAWN); em teste, um fake. O kit NUNCA escreve velocidade, vida ou escudo por aqui: velocidade e'
    /// Efeitos.Lentificar (produto unico), dano e' Combat, escudo e' Vitalidade — o pawn so' entrega mira,
    /// chao, mana (que o kit LE' e nunca gasta — GDD §4.2) e os estados que so' o corpo resolve (intangivel).
    /// </summary>
    public interface IConjurador : IEntidade
    {
        /// <summary>A mana e' do ATAQUE. Tatica e suprema nao a tocam — o teste prova.</summary>
        float Mana { get; set; }
        /// <summary>Direcao de mira (camera no player, corpo no bot). Pode nao estar normalizada.</summary>
        Vector3 DirecaoDaMira { get; }
        /// <summary>Falso na queda: a carga da suprema NAO anda no ar (ordem do Diretor, 26/08).</summary>
        bool NoChao { get; }
        /// <summary>Estado que so' o corpo resolve: "intangivel" (mascara de colisao), "silencio" (aliado na Mare).</summary>
        void AplicarEstado(string nome, float dur);
        /// <summary>Entidades VIVAS a ate' `raio` m do ponto (o pawn inclusive). Null = ninguem por perto.</summary>
        Func<Vector3, float, IEntidade[]> AlvosNoRaio { get; }
    }
}
