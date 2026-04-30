using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Required for the Reset/Exit logic
using SlotGame.Core;

/// <summary>
/// Handles user betting, balance management, handle visuals, and game resetting.
/// </summary>
public class BettingManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bettingPanel;
    [SerializeField] private TextMeshProUGUI balanceText;

    [Header("Handle Toggle Setup")]
    [SerializeField] private GameObject handleUpObject;
    [SerializeField] private GameObject handleDownObject;

    [Header("Game References")]
    [SerializeField] private SlotMachineManager slotManager;
    [SerializeField] private CroupierAI croupier;

    [Header("Audio")]
    [Tooltip("The sound that plays when any bet button or exit button is clicked.")]
    [SerializeField] private AudioSource buttonClickSFX;

    [Header("Player Data")]
    [SerializeField] private int _currentBalance = 1000;

    void Start()
    {
        UpdateBalanceUI();
        bettingPanel.SetActive(true);
        handleUpObject.SetActive(true);
        handleDownObject.SetActive(false);
    }

    /// <summary>
    /// Deducts gold, plays audio, and starts the spin.
    /// </summary>
    public void PlaceBet(int amount)
    {
        if (_currentBalance >= amount)
        {
            PlayClickSound();

            _currentBalance -= amount;
            UpdateBalanceUI();
            bettingPanel.SetActive(false);

            if (croupier != null)
                croupier.ReportBetPlaced(amount);

            StartSpinSequence();
        }
        else
        {
            Debug.Log("<color=red>[WALLET] Insufficient Funds!</color>");
        }
    }

    /// <summary>
    /// Resets the game by reloading the current scene. 
    /// Hook this up to your 'Exit' button in the Inspector.
    /// </summary>
    public void ExitAndResetGame()
    {
        PlayClickSound();
        Debug.Log("Resetting Game...");
        
        // Reloads the currently active scene from index 0
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void StartSpinSequence()
    {
        handleUpObject.SetActive(false);
        handleDownObject.SetActive(true);

        if (slotManager != null)
            slotManager.RequestSpin();

        Invoke(nameof(ResetHandle), 0.5f);
    }

    private void ResetHandle()
    {
        handleUpObject.SetActive(true);
        handleDownObject.SetActive(false);
    }

    public void OnSpinComplete()
    {
        bettingPanel.SetActive(true);
    }

    private void UpdateBalanceUI()
    {
        if (balanceText != null)
        {
            balanceText.text = "Wallet:- " + _currentBalance.ToString() + "G";
        }
    }

    public void AddWinnings(int amount)
    {
        _currentBalance += amount;
        UpdateBalanceUI();
        Debug.Log($"<color=cyan>[WINNINGS]</color> Added: {amount}G. New Total: {_currentBalance}G");
    }

    /// <summary>
    /// Helper to play the click sound with a slight randomized pitch for variety.
    /// </summary>
    private void PlayClickSound()
    {
        if (buttonClickSFX != null)
        {
            // Adds a tiny bit of variation so the click doesn't get annoying
            buttonClickSFX.pitch = Random.Range(0.9f, 1.1f);
            buttonClickSFX.Play();
        }
    }
}