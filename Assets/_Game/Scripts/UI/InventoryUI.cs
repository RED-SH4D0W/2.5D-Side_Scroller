using System.Collections.Generic;
using DScrollerGame.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DScrollerGame.UI
{
    /// <summary>
    /// Controls the inventory panel UI.
    /// Subscribes to Inventory events and spawns / destroys slot prefabs.
    ///
    /// SCENE SETUP:
    /// 1. Create a Canvas → Panel ("InventoryPanel").
    /// 2. Inside the panel, add a layout group (e.g., VerticalLayoutGroup or
    ///    GridLayoutGroup) — this is the _slotContainer.
    /// 3. Create a slot prefab with an InventorySlotUI component (see that class).
    /// 4. Assign _inventory (Player's Inventory), _slotPrefab, and _slotContainer
    ///    in the Inspector.
    /// 5. Optionally assign _toggleKey to open/close the panel at runtime.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("References")]
        [Tooltip("The player's Inventory component.")]
        [SerializeField] private PlayerInventory _inventory;

        [Tooltip("Prefab for each inventory slot. Must have InventorySlotUI.")]
        [SerializeField] private InventorySlotUI _slotPrefab;

        [Tooltip("Parent transform for spawned slots (should have a LayoutGroup).")]
        [SerializeField] private Transform _slotContainer;

        [Header("Toggle")]
        [Tooltip("Action reference to toggle the inventory panel (e.g. 'I' key).")]
        [SerializeField] private InputActionReference _toggleAction;

        [Tooltip("Should the inventory panel start hidden?")]
        [SerializeField] private bool _startHidden = true;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        // Maps ItemData instances to their spawned slot GameObjects
        // so we can remove the right one on consume / drop.
        private readonly List<InventorySlotUI> _activeSlots = new List<InventorySlotUI>();

        private bool _isOpen;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            if (_inventory == null)
            {
                Debug.LogWarning("[InventoryUI] No Inventory reference assigned!", this);
            }

            if (_slotPrefab == null)
            {
                Debug.LogWarning("[InventoryUI] No slot prefab assigned!", this);
            }
        }

        private void OnEnable()
        {
            if (_inventory != null)
            {
                _inventory.OnItemAdded += HandleItemAdded;
                _inventory.OnItemRemoved += HandleItemRemoved;
                _inventory.OnItemConsumed += HandleItemRemoved; // same visual effect
            }

            if (_toggleAction != null)
                _toggleAction.action.Enable();
        }

        private void OnDisable()
        {
            if (_inventory != null)
            {
                _inventory.OnItemAdded -= HandleItemAdded;
                _inventory.OnItemRemoved -= HandleItemRemoved;
                _inventory.OnItemConsumed -= HandleItemRemoved;
            }

            if (_toggleAction != null)
                _toggleAction.action.Disable();
        }

        private void Start()
        {
            if (_startHidden)
            {
                SetPanelVisible(false);
            }
            else
            {
                SetPanelVisible(true);
                RebuildSlots();
            }
        }

        private void Update()
        {
            if (_toggleAction != null && _toggleAction.action.WasPressedThisFrame())
            {
                TogglePanel();
            }
        }

        // ================================================================
        // PUBLIC METHODS
        // ================================================================

        /// <summary>Toggle inventory panel visibility.</summary>
        public void TogglePanel()
        {
            _isOpen = !_isOpen;
            SetPanelVisible(_isOpen);

            if (_isOpen)
            {
                RebuildSlots();
            }
        }

        // ================================================================
        // PRIVATE – Panel Visibility
        // ================================================================

        private void SetPanelVisible(bool visible)
        {
            _isOpen = visible;

            // Hide/show the slot container (or the entire panel).
            if (_slotContainer != null)
                _slotContainer.gameObject.SetActive(visible);
        }

        // ================================================================
        // PRIVATE – Slot Management
        // ================================================================

        /// <summary>
        /// Clears all slots and rebuilds from the current inventory contents.
        /// Called when the panel is opened.
        /// </summary>
        private void RebuildSlots()
        {
            ClearSlots();

            if (_inventory == null || _slotPrefab == null || _slotContainer == null)
                return;

            foreach (ItemData item in _inventory.Items)
            {
                SpawnSlot(item);
            }
        }

        private void SpawnSlot(ItemData item)
        {
            if (_slotPrefab == null || _slotContainer == null) return;

            InventorySlotUI slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Setup(item, _inventory);
            _activeSlots.Add(slot);
        }

        private void ClearSlots()
        {
            foreach (InventorySlotUI slot in _activeSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            _activeSlots.Clear();
        }

        // ================================================================
        // PRIVATE – Event Handlers
        // ================================================================

        private void HandleItemAdded(ItemData item)
        {
            // Only add visually if the panel is currently open.
            if (_isOpen)
            {
                SpawnSlot(item);
            }
        }

        private void HandleItemRemoved(ItemData item)
        {
            // Find and remove the FIRST matching slot.
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                if (_activeSlots[i] != null && _activeSlots[i].ItemData == item)
                {
                    Destroy(_activeSlots[i].gameObject);
                    _activeSlots.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
