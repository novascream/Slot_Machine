using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// Represents the data for a single slot symbol.
    /// Used for both display and payout calculations.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSymbol", menuName = "Slot Game/Symbol Data")]
    public class SymbolData : ScriptableObject
    {
        [Tooltip("Unique ID to identify the symbol in logic.")]
        public int SymbolID;

        [Tooltip("The visual sprite used on the reel.")]
        public Sprite SymbolSprite;

        [Tooltip("The reward multiplier or value for matching this symbol.")]
        public int PayoutValue;

        
        public int winWeight = 10; // Higher = more common
    }
}