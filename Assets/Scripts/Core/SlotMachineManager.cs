using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SlotGame.Data;

namespace SlotGame.Core
{
    /// <summary>
    /// The high-level orchestrator for the slot machine. Handles user input,
    /// coordinates reel movement, manages the symbol pool, and evaluates results.
    /// </summary>
    public class SlotMachineManager : MonoBehaviour
    {
        [Header("Reel Configuration")]
        [Tooltip("References to the three independent reel columns.")]
        [SerializeField] private List<ReelController> reels;

        [Header("Timing Settings")]
        [SerializeField] private float staggeredStartDelay = 0.15f;
        [SerializeField] private float staggeredStopDelay = 0.5f;
        [SerializeField] private float minimumSpinDuration = 2.0f;

        [Header("Game Data")]
        [Tooltip("The list of all possible SymbolData ScriptableObjects (7, Cherry, Bell, Bar).")]
        [SerializeField] private List<SymbolData> symbolPool;

        private bool _isSpinningSequenceActive;

        /// <summary>
        /// Public entry point for the UI Spin Button.
        /// </summary>
        public void RequestSpin()
        {
            // Prevent overlapping spin requests
            if (_isSpinningSequenceActive) return;

            // Ensure the pool is populated to prevent runtime crashes
            if (symbolPool == null || symbolPool.Count == 0)
            {
                Debug.LogError("[SlotMachineManager] Symbol Pool is empty! Assign SymbolData assets in the Inspector.");
                return;
            }

            StartCoroutine(SpinSequence());
        }

        /// <summary>
        /// Coroutine managing the full lifecycle of a spin: Start -> Delay -> Stop -> Evaluate.
        /// </summary>
        private IEnumerator SpinSequence()
        {
            _isSpinningSequenceActive = true;

            // 1. Determine the landing results via RNG before the reels even stop
            SymbolData[] finalResults = new SymbolData[reels.Count];
            for (int i = 0; i < finalResults.Length; i++)
            {
                finalResults[i] = symbolPool[Random.Range(0, symbolPool.Count)];
            }

            // 2. Start all reels with a staggered delay
            foreach (var reel in reels)
            {
                reel.StartSpin();
                yield return new WaitForSeconds(staggeredStartDelay);
            }

            // 3. Maintain the spin for the minimum visual duration
            yield return new WaitForSeconds(minimumSpinDuration);

            // 4. Sequentially stop each reel and inject the pre-determined result
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].StopSpin(finalResults[i]);
                yield return new WaitForSeconds(staggeredStopDelay);
            }

            // 5. Final validation and Win Evaluation
            EvaluateWin(finalResults);

            _isSpinningSequenceActive = false;
        }

        /// <summary>
        /// Compares the landing symbols to determine if the player has won.
        /// </summary>
        /// <param name="results">The SymbolData that landed on each reel.</param>
        private void EvaluateWin(SymbolData[] results)
        {
            if (results.Length < 2) return;

            bool isWin = true;
            int firstSymbolID = results[0].SymbolID;

            // Simple Logic: All symbols must match the first one
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
                // Retrieve payout from the SymbolData ScriptableObject
                int payout = results[0].PayoutValue;
                Debug.Log($"<color=cyan>[WIN]</color> Jackpot! 3x {results[0].name} matched. Awarding: {payout}G");

                // Trigger Win UI or Particle Effects here
            }
            else
            {
                Debug.Log("<color=grey>[LOSE]</color> No match. Better luck next time.");
            }
        }
    }
}