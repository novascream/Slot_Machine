using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SlotGame.Data;

namespace SlotGame.Core
{
    /// <summary>
    /// The brain of the Slot Machine. Coordinates the reels, determines RNG outcomes,
    /// and communicates with the BettingManager, CroupierAI, and AudioSource.
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
        [SerializeField] private BettingManager bettingManager;
        [SerializeField] private CroupierAI croupier;

        [Header("Audio")]
        [Tooltip("The looping sound that plays while reels are spinning.")]
        [SerializeField] private AudioSource spinningSFX;

        private bool _isSpinningSequenceActive;

        /// <summary>
        /// Entry point for starting a spin. Triggered by BettingManager.
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

        /// <summary>
        /// Selects a symbol based on its 'winWeight' parameter defined in SymbolData.
        /// </summary>
        private SymbolData GetWeightedRandomSymbol()
        {
            int totalWeight = 0;
            foreach (var symbol in symbolPool)
            {
                totalWeight += symbol.winWeight;
            }

            int randomNumber = Random.Range(0, totalWeight);

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

            // Start spinning sound
            if (spinningSFX != null) spinningSFX.Play();

            // 1. Pre-determine weighted results (RNG Injection)
            SymbolData[] finalResults = new SymbolData[reels.Count];
            for (int i = 0; i < finalResults.Length; i++)
            {
                finalResults[i] = GetWeightedRandomSymbol();
            }

            // 2. Start reels in a staggered pattern
            foreach (var reel in reels)
            {
                reel.StartSpin();
                yield return new WaitForSeconds(staggeredStartDelay);
            }

            // 3. Wait for the minimum spin time
            yield return new WaitForSeconds(minimumSpinDuration);

            // 4. Stop reels one by one and inject the predetermined results
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].StopSpin(finalResults[i]);
                yield return new WaitForSeconds(staggeredStopDelay);
            }

            // 5. Evaluate the final results for a win or loss
            EvaluateWin(finalResults);

            // Stop spinning sound
            if (spinningSFX != null) spinningSFX.Stop();

            _isSpinningSequenceActive = false;

            // 6. Signal the BettingManager that the UI can be re-enabled
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

            // Simple win logic: All reels must match the first reel
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

                // Update Balance UI via BettingManager
                if (bettingManager != null) bettingManager.AddWinnings(payout);

                // Report the win to the Croupier for a reaction
                if (croupier != null) croupier.ReportSpinOutcome(true, payout);
            }
            else
            {
                Debug.Log("<color=grey>[LOSE]</color> No match.");

                // Report the loss to the Croupier
                if (croupier != null) croupier.ReportSpinOutcome(false);
            }
        }
    }
}