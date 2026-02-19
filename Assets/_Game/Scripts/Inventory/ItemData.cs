using UnityEngine;

namespace DScrollerGame.Inventory
{
    /// <summary>
    /// The type of effect applied when a consumable item is used.
    /// </summary>
    public enum ConsumableEffectType
    {
        None,
        RestoreHealth,
        RestoreStamina
    }

    /// <summary>
    /// Immutable data asset for any collectible item.
    /// Create instances via: Right-click → Create → Game → Items → Item Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Game/Items/Item Data", order = 0)]
    public class ItemData : ScriptableObject
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("General")]
        [Tooltip("Display name shown in UI.")]
        [SerializeField] private string _itemName = "New Item";

        [Tooltip("Icon displayed in inventory slots.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Short description for tooltips / UI.")]
        [TextArea(2, 4)]
        [SerializeField] private string _description = "";

        [Header("Weight")]
        [Tooltip("Weight added to the player's carry load on pickup.")]
        [Min(0f)]
        [SerializeField] private float _weight = 1f;

        [Header("Consumable")]
        [Tooltip("Can the player consume (use) this item?")]
        [SerializeField] private bool _isConsumable;

        [Tooltip("What effect does consuming this item have?")]
        [SerializeField] private ConsumableEffectType _effectType = ConsumableEffectType.None;

        [Tooltip("Magnitude of the effect (e.g., 25 = restore 25% of max).")]
        [Min(0f)]
        [SerializeField] private float _effectValue;

        // ================================================================
        // PUBLIC ACCESSORS (read-only)
        // ================================================================

        /// <summary>Display name of the item.</summary>
        public string ItemName => _itemName;

        /// <summary>Inventory icon sprite.</summary>
        public Sprite Icon => _icon;

        /// <summary>Short item description.</summary>
        public string Description => _description;

        /// <summary>Weight added to the player's carry load.</summary>
        public float Weight => _weight;

        /// <summary>Whether this item can be consumed from the inventory.</summary>
        public bool IsConsumable => _isConsumable;

        /// <summary>The type of effect applied on consumption.</summary>
        public ConsumableEffectType EffectType => _effectType;

        /// <summary>Magnitude of the consumable effect (percentage of max).</summary>
        public float EffectValue => _effectValue;

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            _weight = Mathf.Max(0f, _weight);
            _effectValue = Mathf.Max(0f, _effectValue);

            // Auto-clear effect fields when not consumable.
            if (!_isConsumable)
            {
                _effectType = ConsumableEffectType.None;
                _effectValue = 0f;
            }
        }
#endif
    }
}
