using DScrollerGame.Interfaces;
using UnityEngine;

namespace DScrollerGame.Interaction
{
    /// <summary>
    /// Phase 4 — Throwable Object.
    ///
    /// Attached to any prefab spawned by the throw system.
    /// On collision, it detects IBreakable and/or INoiseEmitter on the hit object
    /// and delegates accordingly. Never references player scripts directly.
    ///
    /// PHYSICS RULES:
    /// ──────────────
    /// • Rigidbody must freeze Z position and Z rotation (2.5D constraint).
    /// • Impact force is normalized against a configurable max threshold, clamped 0–1.
    ///
    /// ARCHITECTURE RULES:
    /// ───────────────────
    /// • No direct references to PlayerStealthState or PlayerPhysicalState.
    /// • All communication via IBreakable / INoiseEmitter interfaces.
    /// • If the hit object implements neither interface, do nothing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableObject : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("Impact")]
        [Tooltip("Maximum expected impact force magnitude. Used to normalize collision force to 0–1.")]
        [SerializeField] private float _maxImpactForce = 20f;

        [Tooltip("Seconds after spawn before the object is automatically destroyed.")]
        [SerializeField] private float _lifetime = 10f;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Start()
        {
            // Enforce 2.5D Rigidbody constraints.
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationZ;

            // Auto-destroy to prevent world clutter.
            Destroy(gameObject, _lifetime);
        }

        // ================================================================
        // COLLISION
        // ================================================================

        /// <summary>
        /// On collision, computes normalized impact force and delegates to
        /// IBreakable / INoiseEmitter on the hit object (if implemented).
        /// </summary>
        
        
        // private void OnCollisionEnter(Collision collision)
        // {
        //     // --- Compute normalized impact force (0–1) ---
        //     float rawForce = collision.relativeVelocity.magnitude;
        //     float normalizedForce = Mathf.Clamp01(rawForce / _maxImpactForce);

        //     // Contact point for spatial info.
        //     Vector3 contactPoint = collision.contacts.Length > 0
        //         ? collision.contacts[0].point
        //         : transform.position;

        //     // --- Delegate to IBreakable if implemented ---
        //     IBreakable breakable = collision.collider.GetComponent<IBreakable>();

        //     if (breakable != null)
        //         breakable.Break(normalizedForce, contactPoint);

        //     // --- Delegate to INoiseEmitter if implemented ---
        //     INoiseEmitter noiseEmitter = collision.collider.GetComponent<INoiseEmitter>();

        //     if (noiseEmitter != null)
        //         noiseEmitter.EmitNoise(normalizedForce, contactPoint);
        // }

        private void OnCollisionEnter(Collision collision)
        {
            float impactForce = collision.relativeVelocity.magnitude;
            float normalized = Mathf.Clamp01(impactForce / _maxImpactForce);
            Vector3 impactPoint = collision.contacts[0].point;

            IBreakable breakable = collision.collider.GetComponent<IBreakable>();
            if (breakable != null)
                breakable.Break(normalized, impactPoint);

            INoiseEmitter noise = collision.collider.GetComponent<INoiseEmitter>();
            if (noise != null)
                noise.EmitNoise(normalized, impactPoint);

            Destroy(gameObject); // Destroy immediately on first impact
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxImpactForce = Mathf.Max(0.1f, _maxImpactForce);
            _lifetime = Mathf.Max(1f, _lifetime);
        }
#endif
    }
}
