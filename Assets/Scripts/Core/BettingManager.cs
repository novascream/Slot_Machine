using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotGame.Core;

public class BettingManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bettingPanel;
    [SerializeField] private TextMeshProUGUI balanceText;

    [Header("Handle Toggle Setup")]
    [Tooltip("The GameObject using slot-machine2.png (Handle Up)")]
    [SerializeField] private GameObject handleUpObject;

    [Tooltip("The GameObject using slot-machine3.png (Handle Down)")]
    [SerializeField] private GameObject handleDownObject;

    [Header("Game References")]
    [SerializeField] private SlotMachineManager slotManager;

    private int _currentBalance = 1000;

    void Start()
    {
        UpdateBalanceUI();

        // Ensure the menu is open at the start
        bettingPanel.SetActive(true);

        // Set the handle to the "Up" state initially
        handleUpObject.SetActive(true);
        handleDownObject.SetActive(false);
    }

    public void PlaceBet(int amount)
    {
        if (_currentBalance >= amount)
        {
            _currentBalance -= amount;
            UpdateBalanceUI();

            // Hide menu so player can't spam bets while reels are turning
            bettingPanel.SetActive(false);

            StartSpinSequence();
        }
        else
        {
            Debug.Log("<color=red>Insufficient Funds!</color>");
        }
    }

    private void StartSpinSequence()
    {
        // Visual "Pull": Hide Up, Show Down
        handleUpObject.SetActive(false);
        handleDownObject.SetActive(true);

        // Tell the machine to start the logic
        slotManager.RequestSpin();

        // Wait half a second, then "spring" the handle back up
        Invoke(nameof(ResetHandle), 0.5f);
    }

    private void ResetHandle()
    {
        // Spring back: Show Up, Hide Down
        handleUpObject.SetActive(true);
        handleDownObject.SetActive(false);
    }

    // This is called by the SlotMachineManager script when the last reel stops
    public void OnSpinComplete()
    {
        bettingPanel.SetActive(true);
    }

    // Updates the gold display
    private void UpdateBalanceUI()
    {
        if (balanceText != null)
        {
            balanceText.text = _currentBalance.ToString() + "G";
        }
    }

    // Called by SlotMachineManager when the player hits a jackpot
    public void AddWinnings(int amount)
    {
        _currentBalance += amount;
        UpdateBalanceUI();
        Debug.Log($"<color=green>Winnings Added: {amount}G. New Total: {_currentBalance}G</color>");
    }
}