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
    public class BreakableLight : BreakableBase
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("Light Specifics")]
        [Tooltip("The Light component to disable on break. Auto-resolved if left empty.")]
        [SerializeField] private Light _lightComponent;

        [Tooltip("Optional: Renderer whose material will swap to a broken emissive material.")]
        [SerializeField] private Renderer _emissiveRenderer;

        [Tooltip("Optional: Material to apply when the light is broken (e.g., no emissive glow).")]
        [SerializeField] private Material _brokenMaterial;

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
        // BREAKABLE BASE IMPLEMENTATION
        // ================================================================

        protected override void OnBreak(Vector3 impactPoint)
        {
            // --- Disable actual Light component (REAL darkness) ---
            if (_lightComponent != null)
                _lightComponent.enabled = false;

            // --- Optional: swap to broken material (no emissive glow) ---
            if (_emissiveRenderer != null && _brokenMaterial != null)
                _emissiveRenderer.material = _brokenMaterial;
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }
#endif
    }
}
