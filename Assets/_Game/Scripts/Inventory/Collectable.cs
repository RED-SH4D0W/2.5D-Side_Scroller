using UnityEngine;
using UnityEngine.InputSystem;

namespace DScrollerGame.Inventory
{
    /// <summary>
    /// Placed on a pickup object in the scene.
    /// References an ItemData asset and handles the pickup flow.
    /// 
    /// SETUP:
    /// 1. Add a Collider (e.g., BoxCollider) to this GameObject and set it as Trigger.
    /// 2. Assign the ItemData ScriptableObject in the Inspector.
    /// 3. The player must have an Inventory component and a Rigidbody or CharacterController
    ///    (so OnTriggerEnter fires).
    /// 4. The player's GameObject must be on a layer / tag that this collectable can detect.
    ///    By default, it checks for the "Player" tag.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectable : MonoBehaviour, ICollectable
    {
        // ================================================================
        // INSPECTOR
        // ================================================================

        [Header("Item")]
        [Tooltip("The ItemData asset describing this pickup.")]
        [SerializeField] private ItemData _itemData;

        [Header("Pickup Settings")]
        [Tooltip("Tag used to identify the player.")]
        [SerializeField] private string _playerTag = "Player";

        [Tooltip("If true, the item is picked up automatically on contact. " +
                 "If false, pickup must be triggered manually.")]
        [SerializeField] private bool _autoPickup = true;

        [Header("Manual Pickup Input")]
        [Tooltip("Action reference for manual pickup (Interact).")]
        [SerializeField] private InputActionReference _pickupAction;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        private GameObject _playerInRange;

        // ================================================================
        // ICollectable
        // ================================================================

        /// <summary>The data asset describing this item.</summary>
        public ItemData ItemData => _itemData;

        /// <summary>
        /// Pick up this item: add it to the collector's inventory and remove
        /// this object from the scene.
        /// </summary>
        /// <param name="collector">The GameObject collecting this item (the player).</param>
        public void Collect(GameObject collector)
        {
            if (_itemData == null)
            {
                Debug.LogWarning($"[Collectable] {gameObject.name} has no ItemData assigned.");
                return;
            }

            PlayerInventory inventory = collector.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning($"[Collectable] {collector.name} has no Inventory component.");
                return;
            }

            inventory.AddItem(_itemData);
            Destroy(gameObject);
        }

        /// <summary>
        /// Convenience accessor — checks the ScriptableObject flag.
        /// </summary>
        public bool IsConsumable()
        {
            return _itemData != null && _itemData.IsConsumable;
        }

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void OnEnable()
        {
            if (_pickupAction != null)
                _pickupAction.action.Enable();
        }

        private void OnDisable()
        {
            if (_pickupAction != null)
                _pickupAction.action.Disable();
        }

        private void Update()
        {
            if (_autoPickup || _playerInRange == null) return;

            // Manual pickup via input action
            if (_pickupAction != null && _pickupAction.action.WasPressedThisFrame())
            {
                Collect(_playerInRange);
            }
        }

        // ================================================================
        // TRIGGER PICKUP
        // ================================================================

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_playerTag))
            {
                if (_autoPickup)
                {
                    Collect(other.gameObject);
                }
                else
                {
                    _playerInRange = other.gameObject;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_playerTag))
            {
                _playerInRange = null;
            }
        }

        // ================================================================
        // EDITOR SAFETY
        // ================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_itemData == null)
            {
                Debug.LogWarning($"[Collectable] {gameObject.name}: No ItemData assigned!", this);
            }
        }

        private void Reset()
        {
            // Auto-configure the collider as a trigger on first add.
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
#endif
    }
}
