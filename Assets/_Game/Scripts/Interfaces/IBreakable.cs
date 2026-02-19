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
        /// Called when a thrown object collides with this breakable.
        /// </summary>
        /// <param name="impactForce">
        /// Normalized impact force (0–1), derived from collision magnitude
        /// divided by a configurable maxImpactForce threshold, then clamped.
        /// </param>
        /// <param name="impactPoint">World-space point of collision.</param>
        void Break(float impactForce, Vector3 impactPoint);
    }
}
