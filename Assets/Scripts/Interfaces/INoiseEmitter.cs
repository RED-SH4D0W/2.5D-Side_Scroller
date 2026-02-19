using UnityEngine;

namespace DScrollerGame.Interfaces
{
    /// <summary>
    /// Implemented by world objects that can emit noise into the environment.
    ///
    /// ARCHITECTURE RULE:
    /// ──────────────────
    /// All EmitNoise calls must eventually route through a centralized Noise Manager
    /// (planned for Phase 5). No object should directly modify PlayerStealthState.
    /// Noise is WORLD-BASED, not player-based.
    ///
    /// Current implementation: Debug.Log only.
    /// Future implementation: Forward to global NoiseManager for AI hearing.
    /// </summary>
    public interface INoiseEmitter
    {
        /// <summary>
        /// Emit a noise event at a world position.
        /// </summary>
        /// <param name="normalizedAmount">Noise intensity (0–1).</param>
        /// <param name="position">World-space origin of the noise.</param>
        void EmitNoise(float normalizedAmount, Vector3 position);
    }
}
