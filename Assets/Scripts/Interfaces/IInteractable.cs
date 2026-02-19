using DScrollerGame.Player;

namespace DScrollerGame.Interfaces
{
    /// <summary>
    /// Implemented by any world object the player can interact with via the F key.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called once when the player presses the interact key while targeting this object.
        /// </summary>
        /// <param name="player">
        /// Reference to the interaction controller — used ONLY for positional queries
        /// (e.g., checking range). Must NOT access PlayerStealthState or PlayerPhysicalState.
        /// </param>
        void Interact(PlayerInteractionController player);
    }
}
