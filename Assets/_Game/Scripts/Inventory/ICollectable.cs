using UnityEngine;

namespace DScrollerGame.Inventory
{
    /// <summary>
    /// Implemented by any world object that can be picked up by the player.
    /// This is a NEW interface — the existing IInteractable is NOT modified.
    /// </summary>
    public interface ICollectable
    {
        /// <summary>The data asset describing this item.</summary>
        ItemData ItemData { get; }

        /// <summary>
        /// Called when the collector picks up this item.
        /// The implementer should add the item to the collector's Inventory
        /// and then remove itself from the scene.
        /// </summary>
        /// <param name="collector">
        /// The GameObject that is collecting this item (typically the player).
        /// </param>
        void Collect(GameObject collector);
    }
}
