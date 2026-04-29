using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private TextMeshProUGUI _scoreText;

    [SerializeField] private Image _hitMarker;
    private float _hitMarkerTimer = 0f;
    private const float HIT_MARKER_DURATION = 0.1f;

    [SerializeField] private GameObject _endScreen;
    [SerializeField] private TextMeshProUGUI _endScreenText;
    void Awake() => Instance = this;

     void Start()
    {
        _hitMarker.enabled = false;
        _endScreen.SetActive(false);
    }

    public void ShowEndScreen(bool localPlayerWon)
    {
        // Show end screen UI with win/lose message
        Debug.Log(localPlayerWon ? "You Win!" : "You Lose!");

        if(localPlayerWon)
            // Play victory sound, show confetti, etc.
            Debug.Log("Playing victory effects...");
        else
            // Play defeat sound, show broken heart, etc.
            Debug.Log("Playing defeat effects...");
    }
    public void UpdateScore(int player1Kills, int player2Kills)
    {
        // Update score UI here
        Debug.Log($"Score Update - Player 1: {player1Kills} Kills, Player 2: {player2Kills} Kills");
    }
    public void UpdateHealth(int currentHealth)
    {
        // Update health bar UI here
        Debug.Log($"Updating HUD: HP {currentHealth}");
    }


    public void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        // Update ammo UI here
        Debug.Log($"Updating HUD: Ammo {currentAmmo}/{maxAmmo}");
    }

    public void ShowDeathScreen()
    {
        Debug.Log("[HUDManager] ShowDeathScreen called");
    }
    public void HideDeathScreen()
    {
        Debug.Log("[HUDManager] HideDeathScreen called");
    }

}
