using DScrollerGame.Interfaces;
using DScrollerGame.Player;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [Tooltip("Extra friction while player is pushing to reduce sliding.")]
        [SerializeField] private float _pushDrag = 2f;

        [Tooltip("Normalized noise emitted while being pushed (0–1).")]
        [SerializeField] private float _pushNoise = 0.4f;

        [Tooltip("How often noise is emitted while pushed (seconds).")]
        [SerializeField] private float _noiseInterval = 0.5f;

        [Header("Requirements")]
        [Tooltip("Optional: Tag used to recognize the player. If empty, falls back to component lookup.")]
        [SerializeField] private string _playerTag = "Player";

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        /// <summary>True while this object is being pushed by the player.</summary>
        private bool _isBeingPushed;

        /// <summary>Cached reference to the player currently in contact (if any).</summary>
        private Transform _currentPusher; // player's transform while in contact

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

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void FixedUpdate()
        {
            if (!_isBeingPushed || _currentPusher == null)
                return;

            // SAFETY: Check if pusher is still within a reasonable distance (e.g. 1.5m) 
            // This is needed because CharacterControllers don't call OnCollisionExit.
            float distance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(_currentPusher.position.x, _currentPusher.position.y)
            );

            if (distance > 1.8f) // assuming object is around 1x1m and player is 2m tall
            {
                _isBeingPushed = false;
                _currentPusher = null;
                _noiseTimer = 0f;
                return;
            }

            // Determine push direction from the player's position relative to this object.
            float direction = Mathf.Sign(transform.position.x - _currentPusher.position.x);
            Vector3 force = new Vector3(direction * _pushForce, 0f, 0f);

            _rb.AddForce(force, ForceMode.Force);

            // Add some drag while being pushed to reduce endless sliding
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x * (1f / (1f + _pushDrag * Time.fixedDeltaTime)), _rb.linearVelocity.y, 0f);

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
        /// New interaction model (no PlayerInteractionController):
        /// - Player must be physically touching the object.
        /// - Interaction key must be held (or pressed) to push.
        /// This class reads the InputAction directly via _interactAction.
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            if (!IsPlayerCollider(collision.collider))
                return;

            // Update pusher reference before checking input
            _currentPusher = collision.collider.transform.root;

            if (!IsInteractionPressed())
            {
                // If key not pressed while touching, stop pushing.
                if (_isBeingPushed)
                {
                    _isBeingPushed = false;
                    _currentPusher = null;
                    _noiseTimer = 0f;
                }
                return;
            }

            // Begin/continue pushing while contact is maintained and key is down.
            _isBeingPushed = true;
        }

        private void OnCollisionExit(Collision collision)
        {
            if (_currentPusher == null) return;
            if (IsPlayerCollider(collision.collider))
            {
                _isBeingPushed = false;
                _currentPusher = null;
                _noiseTimer = 0f;
            }
        }

        // Support trigger-based detection (recommended when using CharacterController on the player)
        private void OnTriggerStay(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            // Update pusher reference before checking input
            _currentPusher = other.transform.root;

            if (!IsInteractionPressed())
            {
                if (_isBeingPushed)
                {
                    _isBeingPushed = false;
                    _currentPusher = null;
                    _noiseTimer = 0f;
                }
                return;
            }

            _isBeingPushed = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (_currentPusher == null) return;
            if (IsPlayerCollider(other))
            {
                _isBeingPushed = false;
                _currentPusher = null;
                _noiseTimer = 0f;
            }
        }

        private bool IsInteractionPressed()
        {
            if (_currentPusher == null) return false;

            var movement = _currentPusher.GetComponentInChildren<PlayerMovementController>();
            if (movement == null) return false;

            var action = movement.InteractAction;
            return action != null && action.action != null && action.action.IsPressed();
        }

        private bool IsPlayerCollider(Collider col)
        {
            if (col == null) return false;

            // 1) Tag match (fast path)
            if (!string.IsNullOrEmpty(_playerTag) && col.CompareTag(_playerTag))
                return true;

            // 2) Component presence fallback
            return col.GetComponentInParent<PlayerMovementController>() != null;
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

        /// <summary>
        /// External entry point for CharacterControllers to push this object.
        /// </summary>
        /// <param name="pusher">The transform of the object doing the pushing.</param>
        public void TryPush(Transform pusher)
        {
            _currentPusher = pusher;
            
            if (!IsInteractionPressed())
            {
                if (_isBeingPushed)
                {
                    _isBeingPushed = false;
                    _currentPusher = null;
                    _noiseTimer = 0f;
                }
                return;
            }

            _isBeingPushed = true;
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            _pushForce = Mathf.Max(0f, _pushForce);
            _pushDrag = Mathf.Max(0f, _pushDrag);
            _pushNoise = Mathf.Clamp01(_pushNoise);
            _noiseInterval = Mathf.Max(0.1f, _noiseInterval);
        }
#endif
    }
}
