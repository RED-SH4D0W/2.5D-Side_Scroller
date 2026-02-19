namespace DScrollerGame.Interfaces
{
    /// <summary>
    /// Implemented by environmental objects that can be slashed / cut.
    /// Slash is environmental-only — it must NOT break lights or apply combat damage.
    /// </summary>
    public interface ICuttable
    {
        /// <summary>
        /// Called once when the player slashes this object (RMB).
        /// </summary>
        void Cut();
    }
}
