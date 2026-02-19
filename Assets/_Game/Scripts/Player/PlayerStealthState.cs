using UnityEngine;

namespace DScrollerGame.Player
{
    /// <summary>
    /// Phase 3 — Stealth Output Component.
    /// Calculates and exposes normalized Noise (0–1) and Visibility (0–1) values
    /// based on the player's current movement and physical state.
    ///
    /// This component is PASSIVE:
    ///   • It never moves the character.
    ///   • It never consumes stamina.
    ///   • It never reads raw input.
    ///   • It only reads public state from PlayerMovementController and PlayerPhysicalState.
    /// </summary>
    public class PlayerStealthState : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR – Noise Settings
        // ================================================================

        [Header("Noise")]
        [Tooltip("Noise emitted while standing still on the ground.")]
        [SerializeField] private float _idleNoise = 0.05f;

        [Tooltip("Noise emitted while walking.")]
        [SerializeField] private float _walkNoise = 0.3f;

        [Tooltip("Noise emitted while sprinting.")]
        [SerializeField] private float _sprintNoise = 0.8f;

        [Tooltip("Noise emitted while crouching and moving.")]
        [SerializeField] private float _crouchNoise = 0.15f;

        [Tooltip("Noise spike when landing from a jump / fall.")]
        [SerializeField] private float _jumpLandNoise = 0.9f;

        [Tooltip("SmoothDamp time for noise transitions (seconds).")]
        [SerializeField] private float _noiseSmoothTime = 0.2f;

        [Tooltip("Maximum world-space noise radius at noise = 1.")]
        [SerializeField] private float _maxNoiseRadius = 15f;

        // ================================================================
        // INSPECTOR – Visibility Settings
        // ================================================================

        [Header("Visibility")]
        [Tooltip("Baseline visibility while idle.")]
        [SerializeField] private float _baseVisibility = 0.2f;

        [Tooltip("Additional visibility when the player is moving.")]
        [SerializeField] private float _moveVisibilityBonus = 0.2f;

        [Tooltip("Additional visibility when the player is sprinting.")]
        [SerializeField] private float _sprintVisibilityBonus = 0.4f;

        [Tooltip("Multiplier applied to total visibility while crouching (< 1 reduces visibility).")]
        [SerializeField] private float _crouchVisibilityMultiplier = 0.5f;

        [Tooltip("SmoothDamp time for visibility transitions (seconds).")]
        [SerializeField] private float _visibilitySmoothTime = 0.3f;

        // ================================================================
        // COMPONENT REFERENCES
        // ================================================================

        [Header("References (auto-resolved if left empty)")]
        [SerializeField] private PlayerMovementController _movement;
        [SerializeField] private PlayerPhysicalState _physicalState;

        // ================================================================
        // PUBLIC READ-ONLY OUTPUT
        // ================================================================

        /// <summary>Normalized noise level (0–1).</summary>
        public float CurrentNoise { get; private set; }

        /// <summary>Normalized visibility level (0–1).</summary>
        public float CurrentVisibility { get; private set; }

        /// <summary>World-space noise radius derived from CurrentNoise.</summary>
        public float CurrentNoiseRadius => CurrentNoise * _maxNoiseRadius;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        // SmoothDamp velocity trackers (required by Mathf.SmoothDamp).
        private float _noiseVelocity;
        private float _visibilityVelocity;

        // Landing detection — tracked internally by comparing grounded state each frame.
        private bool _wasGroundedLastFrame;
        private bool _justLanded;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            // Auto-resolve sibling references if not assigned in the Inspector.
            if (_movement == null)
                _movement = GetComponent<PlayerMovementController>();

            if (_physicalState == null)
                _physicalState = GetComponent<PlayerPhysicalState>();
        }

        private void Update()
        {
            DetectLanding();
            UpdateNoise();
            UpdateVisibility();
        }

        // ================================================================
        // LANDING DETECTION
        // ================================================================

        /// <summary>
        /// Detects the single frame the player transitions from airborne to grounded.
        /// This is used to trigger a noise spike on landing.
        /// </summary>
        private void DetectLanding()
        {
            bool grounded = _movement.IsGrounded;

            // A landing occurs when we are grounded this frame but were NOT grounded last frame.
            _justLanded = grounded && !_wasGroundedLastFrame;

            _wasGroundedLastFrame = grounded;
        }

        // ================================================================
        // NOISE LOGIC
        // ================================================================

        /// <summary>
        /// Calculates the target noise based on movement state, applies weight influence,
        /// handles landing spikes, and smooth-damps toward the target.
        /// </summary>
        private void UpdateNoise()
        {
            float targetNoise;

            // --- Airborne: silent ---
            if (!_movement.IsGrounded)
            {
                targetNoise = _idleNoise * 0.5f;;
            }
            else
            {
                // --- Grounded noise hierarchy (crouch > sprint > walk > idle) ---
                if (_movement.IsCrouching)
                    targetNoise = _crouchNoise;
                else if (_movement.IsSprinting)
                    targetNoise = _sprintNoise;
                else if (_movement.IsMoving)
                    targetNoise = _walkNoise;
                else
                    targetNoise = _idleNoise;

                // Weight influence — heavier loads produce more noise.
                targetNoise *= (1f + _physicalState.WeightPercent * 0.5f);
            }

            // --- Landing spike: immediately push noise toward the landing value ---
            if (_justLanded)
            {
                //CurrentNoise = _jumpLandNoise;
                CurrentNoise = Mathf.Max(CurrentNoise, _jumpLandNoise);
                _noiseVelocity = 0f;
            }

            // Smoothly interpolate toward the target.
            CurrentNoise = Mathf.SmoothDamp(
                CurrentNoise,
                targetNoise,
                ref _noiseVelocity,
                _noiseSmoothTime
            );

            // Final clamp.
            CurrentNoise = Mathf.Clamp01(CurrentNoise);
        }

        // ================================================================
        // VISIBILITY LOGIC
        // ================================================================

        /// <summary>
        /// Calculates target visibility from movement, sprinting, crouching,
        /// weight, and exhaustion, then smooth-damps toward it.
        /// </summary>
        private void UpdateVisibility()
        {
            float targetVisibility = _baseVisibility;

            // Movement increases visibility.
            if (_movement.IsMoving)
                targetVisibility += _moveVisibilityBonus;

            // Sprinting significantly increases visibility.
            if (_movement.IsSprinting)
                targetVisibility += _sprintVisibilityBonus;

            // Heavier loads make the player slightly more visible.
            targetVisibility += _physicalState.WeightPercent * 0.1f;

            // Exhaustion makes the player breathe heavier / more noticeable.
            if (_physicalState.IsExhausted)
                targetVisibility += 0.05f;

            // Crouching multiplies (reduces) the accumulated visibility.
            if (_movement.IsCrouching)
                targetVisibility *= _crouchVisibilityMultiplier;

            // Smoothly interpolate toward the target.
            CurrentVisibility = Mathf.SmoothDamp(
                CurrentVisibility,
                targetVisibility,
                ref _visibilityVelocity,
                _visibilitySmoothTime
            );

            // Final clamp.
            CurrentVisibility = Mathf.Clamp01(CurrentVisibility);
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            _idleNoise      = Mathf.Clamp01(_idleNoise);
            _walkNoise       = Mathf.Clamp01(_walkNoise);
            _sprintNoise     = Mathf.Clamp01(_sprintNoise);
            _crouchNoise     = Mathf.Clamp01(_crouchNoise);
            _jumpLandNoise   = Mathf.Clamp01(_jumpLandNoise);
            _noiseSmoothTime = Mathf.Max(0.01f, _noiseSmoothTime);
            _maxNoiseRadius  = Mathf.Max(0f, _maxNoiseRadius);

            _baseVisibility            = Mathf.Clamp01(_baseVisibility);
            _moveVisibilityBonus       = Mathf.Max(0f, _moveVisibilityBonus);
            _sprintVisibilityBonus     = Mathf.Max(0f, _sprintVisibilityBonus);
            _crouchVisibilityMultiplier = Mathf.Clamp01(_crouchVisibilityMultiplier);
            _visibilitySmoothTime      = Mathf.Max(0.01f, _visibilitySmoothTime);
        }
#endif

#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, CurrentNoiseRadius);
}
#endif

#if UNITY_EDITOR
private float _debugTimer;

private void LateUpdate()
{
    _debugTimer += Time.deltaTime;

    if (_debugTimer >= 0.5f)
    {
        _debugTimer = 0f;

        Debug.Log(
            $"[STEALTH DEBUG]\n" +
            $"Noise: {CurrentNoise:F2} | Radius: {CurrentNoiseRadius:F2}\n" +
            $"Visibility: {CurrentVisibility:F2}"
        );
    }
}
#endif
    }
}
