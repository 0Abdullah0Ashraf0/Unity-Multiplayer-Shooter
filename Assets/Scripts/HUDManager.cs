using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [SerializeField] private GameObject _endScreen;
    [SerializeField] private GameObject _roundPanel;         // the panel itself
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _endScreenText;
    [SerializeField] private TextMeshProUGUI _roundText;     // shows "ROUND 2" or "NEXT ROUND IN"
    [SerializeField] private TextMeshProUGUI _countdownText; // shows the big countdown number

    [SerializeField] private Image _hitMarker;
    private float _hitMarkerTimer = 0f;
    private const float HIT_MARKER_DURATION = 0.1f;

    void Awake() => Instance = this;

     void Start()
    {
        _hitMarker.enabled = false;
        _endScreen.SetActive(false);
        _roundPanel.SetActive(false);

    }

    void Update()
    {
        // Hide hit marker after its short duration
        if (_hitMarker.enabled)
        {
            _hitMarkerTimer -= Time.deltaTime;
            if (_hitMarkerTimer <= 0f)
                _hitMarker.enabled = false;
        }
    }

    //Show End Screen
    public void ShowEndScreen(bool localPlayerWon)
    {
        _endScreen.SetActive(true);
        _endScreenText.text = localPlayerWon ? "YOU WIN!" : "YOU LOSE!";
    }

    public void UpdateScore(int player1Kills, int player2Kills)
    {
        // Update score UI here
        _scoreText.text = $"P1  {player1Kills}    P2  {player2Kills}";
    }
    
    public void UpdateHealth(int currentHealth)
    {
        // Update health bar UI here
        _healthText.text = $"HP  {currentHealth}";
    }


    public void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        // Update ammo UI here
        _ammoText.text = $"{currentAmmo}  /  {maxAmmo}";
    }
    
    public void ShowHitMarker()
    {
        _hitMarker.enabled = true;
        _hitMarkerTimer    = HIT_MARKER_DURATION;
    }

    // Shows "NEXT ROUND IN" with a big number counting down, then hides itself.
    public void StartCountdownUI(int seconds)
    {
        StopAllCoroutines();
        StartCoroutine(CountdownRoutine(seconds));
    }

    private IEnumerator CountdownRoutine(int seconds)
    {
        _roundPanel.SetActive(true);
        _roundText.text = "NEXT ROUND IN";
        _countdownText.gameObject.SetActive(true);
 
        for (int i = seconds; i > 0; i--)
        {
            _countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
 
        _countdownText.text = "GO!";
        yield return new WaitForSeconds(0.6f);
        _roundPanel.SetActive(false);
    }

    // Briefly shows "ROUND 2", "ROUND 3" etc. at the start of each new round.
    public void ShowRoundBanner(int roundNumber)
    {
        StopAllCoroutines();
        StartCoroutine(RoundBannerRoutine(roundNumber));
    }

    private IEnumerator RoundBannerRoutine(int roundNumber)
    {
        _roundPanel.SetActive(true);
        _roundText.text = $"ROUND  {roundNumber}";
        _countdownText.gameObject.SetActive(false);
        yield return new WaitForSeconds(2f);
        _roundPanel.SetActive(false);
    }
}
