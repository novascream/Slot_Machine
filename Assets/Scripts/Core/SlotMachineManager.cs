using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SlotGame.Data;

namespace SlotGame.Core
{
    /// <summary>
    /// The brain of the Slot Machine. Coordinates the reels, determines RNG outcomes,
    /// and communicates with the BettingManager and CroupierAI.
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
        [SerializeField] private CroupierAI croupier; // Added link to the Croupier

        private bool _isSpinningSequenceActive;

        /// <summary>
        /// Entry point for starting a spin.
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
        /// Selects a symbol based on its 'winWeight' parameter.
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

            return symbolPool[0];
        }

        private IEnumerator SpinSequence()
        {
            _isSpinningSequenceActive = true;

            // 1. Pre-determine weighted results
            SymbolData[] finalResults = new SymbolData[reels.Count];
            for (int i = 0; i < finalResults.Length; i++)
            {
                finalResults[i] = GetWeightedRandomSymbol();
            }

            // 2. Start reels
            foreach (var reel in reels)
            {
                reel.StartSpin();
                yield return new WaitForSeconds(staggeredStartDelay);
            }

            yield return new WaitForSeconds(minimumSpinDuration);

            // 3. Stop reels and inject results
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].StopSpin(finalResults[i]);
                yield return new WaitForSeconds(staggeredStopDelay);
            }

            // 4. Evaluate win/loss
            EvaluateWin(finalResults);

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

                // Update UI Balance
                if (bettingManager != null) bettingManager.AddWinnings(payout);

                // Tell Croupier to celebrate
                if (croupier != null) croupier.ReportSpinOutcome(true, payout);
            }
            else
            {
                Debug.Log("<color=grey>[LOSE]</color> No match.");

                // Tell Croupier to comment on the loss/streak
                if (croupier != null) croupier.ReportSpinOutcome(false);
            }
        }
    }
}