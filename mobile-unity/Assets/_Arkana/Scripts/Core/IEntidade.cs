using UnityEngine;

namespace Arkana.Core
{
    /// <summary>
    /// O minimo que o Bus, o Combat e a HUD precisam saber de um pawn (player ou bot)
    /// sem depender de MonoBehaviour. Quem implementa: Gameplay.Pawn. Em teste, um fake.
    /// </summary>
    public interface IEntidade
    {
        string Nome { get; }
        Vector3 Pos { get; }
        bool EhPlayer { get; }
        /// <summary>Estado de vida/escudo. Dono unico do dano: Core.Combat.</summary>
        Vitalidade Vital { get; }
    }
}
