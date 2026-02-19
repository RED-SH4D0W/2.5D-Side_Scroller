using DScrollerGame.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace DScrollerGame.UI
{
    /// <summary>
    /// Individual slot in the inventory UI grid.
    /// Displays an item's icon and name, and handles click-to-consume.
    ///
    /// PREFAB SETUP:
    /// - Root: Button component (InventorySlotUI goes here)
    ///   └── IconImage  (Image, assign to _iconImage)
    ///   └── NameText   (TMPro or legacy Text, assign to _nameText)
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("UI Elements")]
        [Tooltip("Image component that displays the item icon.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Text component that displays the item name. " +
                 "(Use UnityEngine.UI.Text or wire up TMPro manually.)")]
        [SerializeField] private Text _nameText;

        [Tooltip("Button for click detection.")]
        [SerializeField] private Button _button;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        private ItemData _itemData;
        private PlayerInventory _inventory;

        // ================================================================
        // PUBLIC METHODS
        // ================================================================

        /// <summary>
        /// Initialize this slot with an item and its owning inventory.
        /// </summary>
        public void Setup(ItemData item, PlayerInventory inventory)
        {
            _itemData = item;
            _inventory = inventory;

            // Update visuals.
            if (_iconImage != null)
            {
                _iconImage.sprite = item.Icon;
                _iconImage.enabled = item.Icon != null;
            }

            if (_nameText != null)
                _nameText.text = item.ItemName;

            // Wire click handler.
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnSlotClicked);

                // Visually indicate non-consumable items.
                _button.interactable = item.IsConsumable;
            }
        }

        /// <summary>The item this slot represents.</summary>
        public ItemData ItemData => _itemData;

        // ================================================================
        // PRIVATE
        // ================================================================

        private void OnSlotClicked()
        {
            if (_itemData == null || _inventory == null) return;

            if (_itemData.IsConsumable)
            {
                bool consumed = _inventory.ConsumeItem(_itemData);
                if (consumed)
                {
                    // The InventoryUI will destroy this slot via the OnItemConsumed event.
                    // No need to self-destruct here.
                }
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveAllListeners();
        }
    }
}
