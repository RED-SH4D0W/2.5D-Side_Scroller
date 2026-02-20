using System;
using System.Collections.Generic;
using DScrollerGame.Player;
using UnityEngine;

namespace DScrollerGame.Inventory
{
    /// <summary>
    /// Manages the player's collected items.
    /// Attach to the same GameObject as PlayerPhysicalState and PlayerHealth.
    /// Fires events so the UI can react without tight coupling.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("References (auto-resolved if left empty)")]
        [Tooltip("Reference to PlayerPhysicalState for weight management.")]
        [SerializeField] private PlayerPhysicalState _physicalState;

        [Tooltip("Reference to PlayerHealth for consumable effects.")]
        [SerializeField] private PlayerHealth _playerHealth;

        [Header("Starting Items")]
        [Tooltip("List of items in the inventory. Can be edited in the Inspector.")]
        [SerializeField] private List<ItemData> _items = new List<ItemData>();

        // ================================================================
        // EVENTS
        // ================================================================

        /// <summary>Fired when an item is added to the inventory.</summary>
        public event Action<ItemData> OnItemAdded;

        /// <summary>Fired when an item is removed from the inventory.</summary>
        public event Action<ItemData> OnItemRemoved;

        /// <summary>Fired when a consumable item is consumed (after effect applied).</summary>
        public event Action<ItemData> OnItemConsumed;

        // ================================================================
        // PUBLIC ACCESSORS
        // ================================================================

        /// <summary>Read-only view of all items currently held.</summary>
        public IReadOnlyList<ItemData> Items => _items;

        /// <summary>Number of items currently held.</summary>
        public int Count => _items.Count;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            // Auto-resolve sibling references if not assigned in the Inspector.
            if (_physicalState == null)
                _physicalState = GetComponent<PlayerPhysicalState>();

            if (_playerHealth == null)
                _playerHealth = GetComponent<PlayerHealth>();

            if (_physicalState == null)
                Debug.LogWarning($"[Inventory] No PlayerPhysicalState found on {gameObject.name}. " +
                                 "Weight updates will be skipped.");
        }

        // ================================================================
        // PUBLIC METHODS
        // ================================================================

        /// <summary>
        /// Add an item to the inventory and apply its weight.
        /// </summary>
        public void AddItem(ItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[Inventory] Attempted to add a null item.");
                return;
            }

            _items.Add(item);

            // Update carry weight.
            if (_physicalState != null && item.Weight > 0f)
                _physicalState.AddWeight(item.Weight);

            OnItemAdded?.Invoke(item);
        }

        /// <summary>
        /// Remove an item from the inventory and return its weight.
        /// Returns true if the item was found and removed.
        /// </summary>
        public bool RemoveItem(ItemData item)
        {
            if (item == null) return false;

            int index = _items.IndexOf(item);
            if (index < 0) return false;

            _items.RemoveAt(index);

            // Return carry weight.
            if (_physicalState != null && item.Weight > 0f)
                _physicalState.RemoveWeight(item.Weight);

            OnItemRemoved?.Invoke(item);
            return true;
        }

        /// <summary>
        /// Consume a consumable item: apply its effect, remove it, and return its weight.
        /// Returns true if the item was consumable and successfully consumed.
        /// </summary>
        public bool ConsumeItem(ItemData item)
        {
            if (item == null || !item.IsConsumable) return false;

            int index = _items.IndexOf(item);
            if (index < 0) return false;

            // ---- Apply consumable effect ----
            ApplyEffect(item);

            // ---- Remove from inventory ----
            _items.RemoveAt(index);

            // ---- Return carry weight ----
            if (_physicalState != null && item.Weight > 0f)
                _physicalState.RemoveWeight(item.Weight);

            OnItemConsumed?.Invoke(item);
            return true;
        }

        /// <summary>
        /// Check if the inventory contains a specific item.
        /// </summary>
        public bool HasItem(ItemData item) => item != null && _items.Contains(item);

        // ================================================================
        // PRIVATE – Effect Application
        // ================================================================

        /// <summary>
        /// Applies the consumable effect defined in the ItemData asset.
        /// </summary>
        private void ApplyEffect(ItemData item)
        {
            switch (item.EffectType)
            {
                case ConsumableEffectType.RestoreHealth:
                    if (_playerHealth != null)
                    {
                        _playerHealth.RestoreHealthPercent(item.EffectValue);
                    }
                    else
                    {
                        Debug.LogWarning("[Inventory] Cannot restore health — " +
                                         "no PlayerHealth component found.");
                    }
                    break;

                case ConsumableEffectType.RestoreStamina:
                    // PlayerPhysicalState doesn't expose a RestoreStamina() method
                    // (existing script is not modified). Log for now — extend later.
                    Debug.Log($"[Inventory] Stamina restoration by {item.EffectValue}% " +
                              "is not yet supported. Add a RestoreStamina() method " +
                              "to PlayerPhysicalState to enable this.");
                    break;

                case ConsumableEffectType.None:
                    // No effect — item is simply removed.
                    break;

                default:
                    Debug.LogWarning($"[Inventory] Unhandled effect type: {item.EffectType}");
                    break;
            }
        }
    }
}
