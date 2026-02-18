using DScrollerGame.Utils.PropertyDrawers;
using UnityEngine;

namespace DScrollerGame.Player
{
    /// <summary>
    /// Manages the player's physical condition: Stamina and Carry-Weight.
    /// This component is PASSIVE — it never reads input or moves the character.
    /// The movement controller calls into its public API to query / consume resources.
    /// </summary>
    public class PlayerPhysicalState : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR – Stamina
        // ================================================================

        [Header("Stamina")]
        [Tooltip("Maximum stamina the player can have.")]
        [SerializeField] private float _maxStamina = 100f;

        [Tooltip("Stamina recovered per second while regenerating.")]
        [SerializeField] private float _staminaRegenRate = 15f;

        [Tooltip("Base stamina drained per second while sprinting (before weight multiplier).")]
        [SerializeField] private float _sprintStaminaDrainPerSecond = 20f;

        [Tooltip("Flat stamina cost of a single jump.")]
        [SerializeField] private float _jumpStaminaCost = 12f;

        [Tooltip("Seconds after last stamina use before regeneration begins.")]
        [SerializeField] private float _staminaRegenDelay = 1.5f;

        // ================================================================
        // INSPECTOR – Weight
        // ================================================================

        [Header("Weight / Carry Capacity")]
        [Tooltip("Maximum weight the player can carry.")]
        [SerializeField] private float _maxCarryWeight = 100f;

        [Tooltip("Movement speed multiplier vs weight%. X = weight% (0-1), Y = multiplier.")]
        [SerializeField] private AnimationCurve _baseMoveSpeedPenaltyCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);

        [Tooltip("Jump force multiplier vs weight%. X = weight% (0-1), Y = multiplier.")]
        [SerializeField] private AnimationCurve _baseJumpPenaltyCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);

        [Tooltip("Stamina drain multiplier vs weight%. X = weight% (0-1), Y = multiplier.")]
        [SerializeField] private AnimationCurve _staminaDrainMultiplierCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);

        // ================================================================
        // PRIVATE STATE – Stamina
        // ================================================================

        [ReadOnly, SerializeField] private float _currentStamina;
        private float _lastStaminaUseTime = Mathf.NegativeInfinity;

        // ================================================================
        // PRIVATE STATE – Weight
        // ================================================================

        private float _currentCarryWeight;
        private float _movementMultiplier = 1f;
        private float _jumpMultiplier = 1f;
        private float _staminaDrainMultiplier = 1f;

        // Minimum floor for MovementMultiplier so the player is never fully frozen.
        private const float MIN_MOVEMENT_MULTIPLIER = 0.1f;

        // ================================================================
        // PUBLIC READ-ONLY – Stamina
        // ================================================================

        /// <summary>Current stamina value (0 … MaxStamina).</summary>
        public float CurrentStamina => _currentStamina;

        /// <summary>Stamina as a 0-1 ratio.</summary>
        public float NormalizedStamina => _maxStamina > 0f
            ? Mathf.Clamp01(_currentStamina / _maxStamina)
            : 0f;

        /// <summary>True when there is enough stamina to keep sprinting this frame.</summary>
        public bool HasStaminaForSprint => _currentStamina > 0f;

        /// <summary>True when there is enough stamina to perform a jump.</summary>
        public bool HasStaminaForJump => _currentStamina >= _jumpStaminaCost;

        /// <summary>True when stamina is completely depleted.</summary>
        public bool IsExhausted => _currentStamina <= 0f;
        

        // ================================================================
        // PUBLIC READ-ONLY – Weight
        // ================================================================

        /// <summary>Current carry weight as a ratio of max (0-1, may reach 1).</summary>
        public float WeightPercent => _maxCarryWeight > 0f
            ? Mathf.Clamp01(_currentCarryWeight / _maxCarryWeight)
            : 0f;

        /// <summary>Multiplier applied to walk / sprint speed.</summary>
        public float MovementMultiplier => _movementMultiplier;

        /// <summary>Multiplier applied to jump force at jump start.</summary>
        public float JumpMultiplier => _jumpMultiplier;

        /// <summary>Multiplier applied to all stamina drain rates.</summary>
        public float StaminaDrainMultiplier => _staminaDrainMultiplier;

        /// <summary>True when carry weight equals or exceeds max capacity.</summary>
        public bool IsOverburdened => WeightPercent >= 1f;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            _currentStamina = _maxStamina;
            RecalculateWeightModifiers();
        }

        private void Update()
        {
            RegenerateStamina();
        }

        // ================================================================
        // STAMINA – Public Methods
        // ================================================================

        /// <summary>
        /// Called by the movement controller every frame the player is sprinting.
        /// Drains stamina based on sprint rate, delta time, and weight drain multiplier.
        /// </summary>
        /// <param name="deltaTime">Time.deltaTime passed from the caller.</param>
        public void ConsumeSprintStamina(float deltaTime)
        {
            float drain = _sprintStaminaDrainPerSecond * deltaTime * _staminaDrainMultiplier;
            _currentStamina = Mathf.Clamp(_currentStamina - drain, 0f, _maxStamina);
            _lastStaminaUseTime = Time.time;
        }

        /// <summary>
        /// Called by the movement controller once when a jump is initiated.
        /// Subtracts the flat jump cost.
        /// </summary>
        public void ConsumeJumpStamina()
        {
            _currentStamina = Mathf.Clamp(_currentStamina - _jumpStaminaCost, 0f, _maxStamina);
            _lastStaminaUseTime = Time.time;
        }

        // ================================================================
        // WEIGHT – Public Methods
        // ================================================================

        /// <summary>
        /// Increase current carry weight (e.g. picking up an item).
        /// Weight is clamped to [0 … MaxCarryWeight].
        /// </summary>
        public void AddWeight(float amount)
        {
            if (amount <= 0f) return;

            _currentCarryWeight = Mathf.Clamp(
                _currentCarryWeight + amount, 0f, _maxCarryWeight);

            RecalculateWeightModifiers();
        }

        /// <summary>
        /// Decrease current carry weight (e.g. dropping an item).
        /// Weight is clamped to [0 … MaxCarryWeight].
        /// </summary>
        public void RemoveWeight(float amount)
        {
            if (amount <= 0f) return;

            _currentCarryWeight = Mathf.Clamp(
                _currentCarryWeight - amount, 0f, _maxCarryWeight);

            RecalculateWeightModifiers();
        }

        // ================================================================
        // STAMINA – Internal
        // ================================================================

        /// <summary>
        /// Regenerates stamina when enough time has passed since the last use.
        /// Uses a timestamp comparison (NOT a decrementing timer).
        /// </summary>
        private void RegenerateStamina()
        {
            // Already full — nothing to do.
            //if (_currentStamina >= _maxStamina) return;
            if (_currentStamina >= _maxStamina)
{
    _currentStamina = _maxStamina;
    return;
}

            // Regen delay has not elapsed yet.
            if (Time.time < _lastStaminaUseTime + _staminaRegenDelay) return;

            _currentStamina = Mathf.Clamp(
                _currentStamina + _staminaRegenRate * Time.deltaTime,
                0f,
                _maxStamina);
        }

        // ================================================================
        // WEIGHT – Internal
        // ================================================================

        /// <summary>
        /// Evaluates all three weight penalty curves and caches the results.
        /// Called ONLY when carry weight actually changes — never per-frame.
        /// </summary>
        private void RecalculateWeightModifiers()
        {
            float pct = WeightPercent;

            _movementMultiplier = Mathf.Max(
                _baseMoveSpeedPenaltyCurve.Evaluate(pct),
                MIN_MOVEMENT_MULTIPLIER);

            _jumpMultiplier = _baseJumpPenaltyCurve.Evaluate(pct);

            _staminaDrainMultiplier = _staminaDrainMultiplierCurve.Evaluate(pct);
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxStamina = Mathf.Max(1f, _maxStamina);
            _staminaRegenRate = Mathf.Max(0f, _staminaRegenRate);
            _sprintStaminaDrainPerSecond = Mathf.Max(0f, _sprintStaminaDrainPerSecond);
            _jumpStaminaCost = Mathf.Max(0f, _jumpStaminaCost);
            _staminaRegenDelay = Mathf.Max(0f, _staminaRegenDelay);
            _maxCarryWeight = Mathf.Max(1f, _maxCarryWeight);
        }
#endif


#if UNITY_EDITOR
private float _debugLogTimer;

private void LateUpdate()
{
    // Debug weight testing — press 1 to add 25, press 2 to remove 25
    if (UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame)
    {
        AddWeight(25f);
        Debug.Log($"[DEBUG] Added 25 weight → Current: {_currentCarryWeight:F1}");
    }

    if (UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame)
    {
        RemoveWeight(25f);
        Debug.Log($"[DEBUG] Removed 25 weight → Current: {_currentCarryWeight:F1}");
    }

    _debugLogTimer += Time.deltaTime;

    if (_debugLogTimer >= 1f)
    {
        _debugLogTimer = 0f;

        Debug.Log(
            $"[PlayerPhysicalState DEBUG]\n" +
            $"Stamina: {_currentStamina:F1} / {_maxStamina}\n" +
            $"Normalized Stamina: {NormalizedStamina:F2}\n" +
            $"Has Stamina For Sprint: {HasStaminaForSprint}\n" +
            $"Has Stamina For Jump: {HasStaminaForJump}\n" +
            $"Is Exhausted: {IsExhausted}\n\n" +
            $"Carry Weight: {_currentCarryWeight:F1} / {_maxCarryWeight}\n" +
            $"Weight %: {WeightPercent:F2}\n" +
            $"Is Overburdened: {IsOverburdened}\n\n" +
            $"Movement Multiplier: {_movementMultiplier:F2}\n" +
            $"Jump Multiplier: {_jumpMultiplier:F2}\n" +
            $"Effective Jump Force: (base × {_jumpMultiplier:F2})\n" +
            $"Stamina Drain Multiplier: {_staminaDrainMultiplier:F2}"
        );
    }
}
#endif
    }
}
