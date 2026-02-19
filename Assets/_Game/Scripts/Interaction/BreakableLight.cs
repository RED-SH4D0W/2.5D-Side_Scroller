using DScrollerGame.Interfaces;
using UnityEngine;

namespace DScrollerGame.Interaction
{
    /// <summary>
    /// Phase 4 — Breakable Light.
    ///
    /// A world light that can be broken by thrown projectiles.
    /// Implements IBreakable (throw impact) and INoiseEmitter (world noise).
    ///
    /// IMPORTANT:
    /// ──────────
    /// • Disables the actual Unity Light component — this is REAL darkness, not cosmetic.
    /// • Future AI vision systems can detect darkness via the Light component's enabled state.
    /// • Guarded by an internal _isBroken flag to prevent duplicate breaks.
    /// • Slash (ICuttable) is NOT implemented — lights can only be broken by thrown objects.
    ///
    /// ARCHITECTURE RULES:
    /// ───────────────────
    /// • No direct references to PlayerStealthState or PlayerPhysicalState.
    /// • EmitNoise currently logs only — future Phase 5 will route through Noise Manager.
    /// • Noise is WORLD-BASED, not player-based.
    /// </summary>
    public class BreakableLight : MonoBehaviour, IBreakable, INoiseEmitter
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("Break Settings")]
        [Tooltip("Minimum normalized impact force (0–1) required to break this light.")]
        [SerializeField] private float _breakThreshold = 0.4f;

        [Tooltip("Normalized noise emitted when this light breaks (0–1).")]
        [SerializeField] private float _breakNoise = 0.6f;

        [Header("References")]
        [Tooltip("The Light component to disable on break. Auto-resolved if left empty.")]
        [SerializeField] private Light _lightComponent;

        [Tooltip("Optional: Renderer whose material will swap to a broken emissive material.")]
        [SerializeField] private Renderer _emissiveRenderer;

        [Tooltip("Optional: Material to apply when the light is broken (e.g., no emissive glow).")]
        [SerializeField] private Material _brokenMaterial;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        /// <summary>Idempotency guard — prevents multiple breaks.</summary>
        private bool _isBroken;

        // ================================================================
        // PUBLIC — State
        // ================================================================

        /// <summary>True after this light has been broken.</summary>
        public bool IsBroken => _isBroken;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            // Auto-resolve Light component if not assigned.
            if (_lightComponent == null)
                _lightComponent = GetComponent<Light>();
        }

        // ================================================================
        // IBreakable
        // ================================================================

        /// <summary>
        /// Called when a thrown object impacts this light.
        /// If impact force meets the threshold and the light is not already broken,
        /// the Light component is disabled and noise is emitted.
        /// </summary>
        /// <param name="impactForce">Normalized impact force (0–1).</param>
        /// <param name="impactPoint">World-space collision point.</param>
        public void Break(float impactForce, Vector3 impactPoint)
        {
            // --- Idempotency guard: cannot break twice ---
            if (_isBroken) return;

            // --- Threshold check ---
            if (impactForce < _breakThreshold) return;

            _isBroken = true;

            // --- Disable actual Light component (REAL darkness) ---
            if (_lightComponent != null)
                _lightComponent.enabled = false;

            // --- Optional: swap to broken material (no emissive glow) ---
            if (_emissiveRenderer != null && _brokenMaterial != null)
                _emissiveRenderer.material = _brokenMaterial;

            // --- Emit break noise into the world ---
            EmitNoise(_breakNoise, impactPoint);
        }

        // ================================================================
        // INoiseEmitter
        // ================================================================

        /// <summary>
        /// Emits a noise event at the specified world position.
        ///
        /// CURRENT: Debug.Log only.
        /// FUTURE (Phase 5): Route through centralized Noise Manager for AI hearing.
        /// No object should directly modify PlayerStealthState.
        /// </summary>
        /// <param name="normalizedAmount">Noise intensity (0–1).</param>
        /// <param name="position">World-space origin of the noise.</param>
        public void EmitNoise(float normalizedAmount, Vector3 position)
        {
            // FUTURE: NoiseManager.Instance.RegisterNoise(normalizedAmount, position);
            Debug.Log(
                $"[BreakableLight Noise] intensity={normalizedAmount:F2} " +
                $"position={position} broken={_isBroken}"
            );
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            _breakThreshold = Mathf.Clamp01(_breakThreshold);
            _breakNoise = Mathf.Clamp01(_breakNoise);
        }
#endif
    }
}
