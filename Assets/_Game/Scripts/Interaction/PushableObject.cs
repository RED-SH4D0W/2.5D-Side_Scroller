using DScrollerGame.Interfaces;
using DScrollerGame.Player;
using UnityEngine;

namespace DScrollerGame.Interaction
{
    /// <summary>
    /// Phase 4 — Pushable Object.
    ///
    /// A world object the player can push by toggling push mode (E key).
    /// Implements IInteractable (push toggle) and INoiseEmitter (world noise).
    ///
    /// MOVEMENT AUTHORITY:
    /// ───────────────────
    /// • PushableObject does NOT control player movement.
    /// • PlayerInteractionController remains the authority over player movement input.
    /// • PushableObject only reacts to physics forces and emits noise.
    ///
    /// PUSH STATE SAFETY:
    /// ──────────────────
    /// Push mode automatically disengages if:
    ///   • Player moves out of range (handled by PlayerInteractionController)
    ///   • Rigidbody becomes null (handled by PlayerInteractionController)
    ///   • E is pressed again (handled by PlayerInteractionController)
    ///
    /// ARCHITECTURE RULES:
    /// ───────────────────
    /// • No direct references to PlayerStealthState or PlayerPhysicalState.
    /// • All cross-system communication via interfaces.
    /// • Noise is WORLD-BASED — future Phase 5 routes through Noise Manager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PushableObject : MonoBehaviour, IInteractable, INoiseEmitter
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("Push Settings")]
        [Tooltip("Force applied to the object while being pushed.")]
        [SerializeField] private float _pushForce = 5f;

        [Tooltip("Normalized noise emitted while being pushed (0–1).")]
        [SerializeField] private float _pushNoise = 0.4f;

        [Tooltip("How often noise is emitted while pushed (seconds).")]
        [SerializeField] private float _noiseInterval = 0.5f;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        /// <summary>True while this object is being pushed by the player.</summary>
        private bool _isBeingPushed;

        /// <summary>Cached reference to the player driving the push.</summary>
        private PlayerInteractionController _pusher;

        /// <summary>Rigidbody on this object.</summary>
        private Rigidbody _rb;

        /// <summary>Timer for periodic noise emission while pushed.</summary>
        private float _noiseTimer;

        // ================================================================
        // PUBLIC — State
        // ================================================================

        /// <summary>True while this object is actively being pushed.</summary>
        public bool IsBeingPushed => _isBeingPushed;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            // Enforce 2.5D physics constraints.
            _rb.constraints = RigidbodyConstraints.FreezePositionZ
                            | RigidbodyConstraints.FreezeRotationX
                            | RigidbodyConstraints.FreezeRotationY
                            | RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            if (!_isBeingPushed || _pusher == null) return;

            // Determine push direction from the player's position relative to this object.
            //float direction = Mathf.Sign(transform.position.x - _pusher.transform.position.x);
            //Vector3 force = new Vector3(direction * _pushForce, 0f, 0f);
            
            float direction = Mathf.Sign(_pusher.transform.localScale.x);
            Vector3 force = new Vector3(direction * _pushForce, 0f, 0f);
            
            _rb.AddForce(force, ForceMode.Force);

            // Emit noise periodically while pushing.
            _noiseTimer += Time.fixedDeltaTime;

            if (_noiseTimer >= _noiseInterval)
            {
                _noiseTimer = 0f;
                EmitNoise(_pushNoise, transform.position);
            }
        }

        // ================================================================
        // IInteractable
        // ================================================================

        /// <summary>
        /// Toggles push mode on this object.
        /// Called by PlayerInteractionController when the player presses E.
        /// </summary>
        /// <param name="player">The interaction controller initiating the push.</param>
        public void Interact(PlayerInteractionController player)
        {
            if (_isBeingPushed)
            {
                // Disengage push.
                _isBeingPushed = false;
                _pusher = null;
                _noiseTimer = 0f;
            }
            else
            {
                // Engage push.
                _isBeingPushed = true;
                _pusher = player;
                _noiseTimer = 0f;
            }
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
                $"[PushableObject Noise] intensity={normalizedAmount:F2} " +
                $"position={position}"
            );
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            _pushForce = Mathf.Max(0f, _pushForce);
            _pushNoise = Mathf.Clamp01(_pushNoise);
            _noiseInterval = Mathf.Max(0.1f, _noiseInterval);
        }
#endif
    }
}
