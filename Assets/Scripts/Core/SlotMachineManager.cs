using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SlotGame.Data;

namespace SlotGame.Core
{
    /// <summary>
    /// The brain of the Slot Machine. Coordinates the reels, determines RNG outcomes,
    /// and communicates with the BettingManager to handle the UI and money.
    /// </summary>
    public class SlotMachineManager : MonoBehaviour
    {
        [Header("Reel Configuration")]
        [SerializeField] private List<ReelController> reels;

        [Header("Timing Settings")]
        [SerializeField] private float staggeredStartDelay = 0.15f;
        [SerializeField] private float staggeredStopDelay = 0.5f;
        [SerializeField] private float minimumSpinDuration = 2.0f;

        [Header("Game Data")]
        [SerializeField] private List<SymbolData> symbolPool;

        [Header("External Managers")]
        [Tooltip("The manager handling bets, balance, and the betting menu visibility.")]
        [SerializeField] private BettingManager bettingManager;

        private bool _isSpinningSequenceActive;

        /// <summary>
        /// Public entry point called by the BettingManager after a bet is placed.
        /// </summary>
        public void RequestSpin()
        {
            if (_isSpinningSequenceActive) return;

            if (symbolPool == null || symbolPool.Count == 0)
            {
                Debug.LogError("[SlotMachineManager] Symbol Pool is empty!");
                return;
            }

            StartCoroutine(SpinSequence());
        }
        private SymbolData GetWeightedRandomSymbol()
        {
            // 1. Calculate the total weight of all symbols in the pool
            int totalWeight = 0;
            foreach (var symbol in symbolPool)
            {
                totalWeight += symbol.winWeight;
            }

            // 2. Pick a random number between 0 and the total weight
            int randomNumber = Random.Range(0, totalWeight);

            // 3. Iterate through the pool to find which symbol the random number "lands" on
            foreach (var symbol in symbolPool)
            {
                if (randomNumber < symbol.winWeight)
                {
                    return symbol;
                }
                randomNumber -= symbol.winWeight;
            }

            return symbolPool[0]; // Fallback
        }
        private IEnumerator SpinSequence()
        {
            _isSpinningSequenceActive = true;

            // 1. Pre-determine results (RNG Injection)
            SymbolData[] finalResults = new SymbolData[reels.Count];
            for (int i = 0; i < finalResults.Length; i++)
            {
                finalResults[i] = GetWeightedRandomSymbol(); ;
            }

            // 2. Start all reels (Staggered)
            foreach (var reel in reels)
            {
                reel.StartSpin();
                yield return new WaitForSeconds(staggeredStartDelay);
            }

            // 3. Keep spinning for the duration
            yield return new WaitForSeconds(minimumSpinDuration);

            // 4. Stop all reels (Staggered) and inject the target symbols
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].StopSpin(finalResults[i]);
                yield return new WaitForSeconds(staggeredStopDelay);
            }

            // 5. Evaluate the results for a win
            EvaluateWin(finalResults);

            // 6. Signal the BettingManager that the sequence is over
            _isSpinningSequenceActive = false;

            if (bettingManager != null)
            {
                bettingManager.OnSpinComplete();
            }
        }

        private void EvaluateWin(SymbolData[] results)
        {
            if (results.Length < 2) return;

            bool isWin = true;
            int firstSymbolID = results[0].SymbolID;

            // Check if all reel results match the first reel
            for (int i = 1; i < results.Length; i++)
            {
                if (results[i].SymbolID != firstSymbolID)
                {
                    isWin = false;
                    break;
                }
            }

            if (isWin)
            {
                int payout = results[0].PayoutValue;
                Debug.Log($"<color=cyan>[WIN]</color> {results[0].name} Match! Awarding: {payout}G");

                // Send the winnings to the BettingManager to update the Balance UI
                if (bettingManager != null)
                {
                    bettingManager.AddWinnings(payout);
                }
            }
            else
            {
                Debug.Log("<color=grey>[LOSE]</color> No match.");
            }
        }
    }
}