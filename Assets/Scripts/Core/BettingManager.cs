using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotGame.Core; // Necessary to access SlotMachineManager and CroupierAI

/// <summary>
/// Handles user betting, balance management, and the visual interaction of the slot machine handle.
/// Communicates with the SlotMachineManager to start spins and the CroupierAI to log events.
/// </summary>
public class BettingManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent panel containing the bet selection buttons (10G, 50G, 100G).")]
    [SerializeField] private GameObject bettingPanel;

    [Tooltip("The TextMeshPro object displaying the current gold balance.")]
    [SerializeField] private TextMeshProUGUI balanceText;

    [Header("Handle Toggle Setup")]
    [Tooltip("The GameObject representing the handle in the standard 'Up' position (slot-machine2.png).")]
    [SerializeField] private GameObject handleUpObject;

    [Tooltip("The GameObject representing the handle in the pulled 'Down' position (slot-machine3.png).")]
    [SerializeField] private GameObject handleDownObject;

    [Header("Game References")]
    [Tooltip("Reference to the main SlotMachineManager script responsible for spin logic.")]
    [SerializeField] private SlotMachineManager slotManager;

    [Tooltip("Reference to the CroupierAI script responsible for logging messages and stats.")]
    [SerializeField] private CroupierAI croupier; // Added link to the Croupier

    // Game State Data
    [Header("Player Data")]
    [SerializeField] private int _currentBalance = 1000;

    /// <summary>
    /// Initializes the UI, balance display, and handle state on game start.
    /// </summary>
    void Start()
    {
        UpdateBalanceUI();

        // Ensure the betting menu is open so the player can choose a bet
        bettingPanel.SetActive(true);

        // Ensure the handle starts in the standard Up state
        handleUpObject.SetActive(true);
        handleDownObject.SetActive(false);
    }

    /// <summary>
    /// Public function called by UI Buttons to place a bet.
    /// If funds are sufficient, it deducts gold, hides the menu, notifies the croupier, and starts the spin sequence.
    /// </summary>
    /// <param name="amount">The gold amount to bet.</param>
    public void PlaceBet(int amount)
    {
        if (_currentBalance >= amount)
        {
            // 1. Transaction: Deduct the gold
            _currentBalance -= amount;
            UpdateBalanceUI();

            // 2. UI: Hide the menu to prevent spam-clicking during the spin
            bettingPanel.SetActive(false);

            // 3. Croupier: Notify the log that a bet was placed (NEW)
            if (croupier != null)
            {
                croupier.ReportBetPlaced(amount);
            }

            // 4. Start the gameplay sequence
            StartSpinSequence();
        }
        else
        {
            // Visual/Audio feedback for insufficient funds would go here. For now, a simple log.
            Debug.Log("<color=red>[WALLET] Insufficient Funds!</color>");
        }
    }

    /// <summary>
    /// Handles the visual handle pull and requests the SlotMachineManager to begin the spin logic.
    /// </summary>
    private void StartSpinSequence()
    {
        // Visual "Pull": Swap visibility to the Down object
        handleUpObject.SetActive(false);
        handleDownObject.SetActive(true);

        // Logic: Request the actual spin to start
        if (slotManager != null)
        {
            slotManager.RequestSpin();
        }

        // Timer: After half a second (standard 'spring' action duration), return the handle up
        Invoke(nameof(ResetHandle), 0.5f);
    }

    /// <summary>
    /// Visually returns the handle to the standard Up state.
    /// </summary>
    private void ResetHandle()
    {
        // Visual "Spring Back": Swap visibility to the Up object
        handleUpObject.SetActive(true);
        handleDownObject.SetActive(false);
    }

    /// <summary>
    /// Public callback called by the SlotMachineManager script when the last reel has officially stopped.
    /// This re-enables the betting panel so the player can bet again.
    /// </summary>
    public void OnSpinComplete()
    {
        bettingPanel.SetActive(true);
    }

    /// <summary>
    /// Updates the TMP Balance text display to reflect the current gold amount.
    /// </summary>
    private void UpdateBalanceUI()
    {
        if (balanceText != null)
        {
            balanceText.text = "Wallet:- "+_currentBalance.ToString() + "G";
        }
    }

    /// <summary>
    /// Public function called by the SlotMachineManager when a win condition is met (e.g., Jackpot).
    /// Adds the winnings to the balance and updates the UI.
    /// </summary>
    /// <param name="amount">The jackpot payout amount.</param>
    public void AddWinnings(int amount)
    {
        _currentBalance += amount;
        UpdateBalanceUI();
        Debug.Log($"<color=cyan>[WINNINGS]</color> Added: {amount}G. New Total: {_currentBalance}G");
    }
}