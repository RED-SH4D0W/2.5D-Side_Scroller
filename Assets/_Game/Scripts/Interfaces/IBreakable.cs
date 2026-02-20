using UnityEngine;

namespace DScrollerGame.Interfaces
{
    /// <summary>
    /// Implemented by world objects that can be broken by thrown projectiles.
    /// Only throw impacts should trigger Break — slashing must NOT break objects.
    /// </summary>
    public interface IBreakable
    {
        /// <summary>
        /// The current amount of damage this object has taken.
        /// </summary>
        float CurrentDamage { get; }

        /// <summary>
        /// The maximum damage this object can take before breaking.
        /// </summary>
        float MaxDamage { get; }

        /// <summary>
        /// Called when a thrown object collides with this breakable.
        /// </summary>
        /// <param name="damage">The amount of damage to apply.</param>
        /// <param name="impactPoint">World-space point of collision.</param>
        void ApplyDamage(float damage, Vector3 impactPoint);

        /// <summary>
        /// Immediately breaks the object.
        /// </summary>
        /// <param name="impactPoint">World-space point of collision.</param>
        void Break(Vector3 impactPoint);
    }
}
