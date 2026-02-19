using System;
using DScrollerGame.Utils.PropertyDrawers;
using UnityEngine;

namespace DScrollerGame.Player
{
    /// <summary>
    /// Manages the player's health pool.
    /// Provides API for damage, healing, and percentage-based restoration
    /// (used by consumable items).
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("Health")]
        [Tooltip("Maximum health the player can have.")]
        [SerializeField] private float _maxHealth = 100f;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        [ReadOnly, SerializeField] private float _currentHealth;

        // ================================================================
        // EVENTS
        // ================================================================

        /// <summary>
        /// Fired whenever health changes. Args: (currentHealth, maxHealth).
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>Fired when health reaches zero.</summary>
        public event Action OnDeath;

        // ================================================================
        // PUBLIC READ-ONLY
        // ================================================================

        /// <summary>Current health value (0 … MaxHealth).</summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>Maximum health.</summary>
        public float MaxHealth => _maxHealth;

        /// <summary>Health as a 0-1 ratio.</summary>
        public float NormalizedHealth => _maxHealth > 0f
            ? Mathf.Clamp01(_currentHealth / _maxHealth)
            : 0f;

        /// <summary>True when health is zero or below.</summary>
        public bool IsDead => _currentHealth <= 0f;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        // ================================================================
        // PUBLIC METHODS
        // ================================================================

        /// <summary>
        /// Deal damage to the player.
        /// </summary>
        /// <param name="amount">Positive damage value.</param>
        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead) return;

            _currentHealth = Mathf.Clamp(_currentHealth - amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (IsDead)
            {
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// Heal a flat amount of health.
        /// </summary>
        /// <param name="amount">Positive heal value.</param>
        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead) return;

            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Restore a percentage of max health (e.g., 25 restores 25% of MaxHealth).
        /// Used by consumable items.
        /// </summary>
        /// <param name="percent">Percentage of max health to restore (0-100).</param>
        public void RestoreHealthPercent(float percent)
        {
            if (percent <= 0f) return;

            float healAmount = _maxHealth * (percent / 100f);
            Heal(healAmount);
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1f, _maxHealth);
        }
#endif
    }
}
