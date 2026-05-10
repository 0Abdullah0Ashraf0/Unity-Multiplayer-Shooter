using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("End Screen")]
    [SerializeField] private GameObject        _endScreen;
    [SerializeField] private TextMeshProUGUI   _endScreenText;

    [Header("Health Bar")]
    [SerializeField] private Image             _healthBarFill;      // Image (type = Filled, Fill Method = Horizontal)
    [SerializeField] private Image             _healthBarDamage;    // second Image behind fill — lerps slowly for "damage drain" effect
    [SerializeField] private TextMeshProUGUI   _healthValueText;    // optional "75 HP" label
    [SerializeField] private int               _maxHealth = 100;
    private float _damageBarVel;                                     // used by SmoothDamp

    [Header("Ammo")]
    [SerializeField] private TextMeshProUGUI   _ammoText;

    [Header("Score")]
    [SerializeField] private TextMeshProUGUI   _scoreText;

    [Header("Damage Popups")]
    [SerializeField] private GameObject        _damagePopupPrefab;  // prefab with DamagePopup + TextMeshProUGUI
    [SerializeField] private Transform         _damagePopupContainer; // a UI panel anchored near the health bar

    [Header("Hit Marker")]
    [SerializeField] private Image             _hitMarker;
    private float _hitMarkerTimer;
    private const float HIT_MARKER_DURATION = 0.1f;

    [Header("Round Panel")]
    [SerializeField] private GameObject        _roundPanel;
    [SerializeField] private CanvasGroup       _roundPanelGroup;    // add a CanvasGroup component on the same GO
    [SerializeField] private TextMeshProUGUI   _roundText;          // "ROUND 2"  /  "NEXT ROUND IN"
    [SerializeField] private TextMeshProUGUI   _countdownText;      // big countdown digit / "GO!"

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    void Awake() => Instance = this;

    void Start()
    {
        _hitMarker.enabled = false;
        _endScreen.SetActive(false);
        _roundPanel.SetActive(false);

        SetHealthBar(1f, 1f);  // full bar on start
    }

    void Update()
    {
        // Fade hit-marker
        if (_hitMarker.enabled)
        {
            _hitMarkerTimer -= Time.deltaTime;
            if (_hitMarkerTimer <= 0f) _hitMarker.enabled = false;
        }

        // Lag the damage bar behind the green fill
        if (_healthBarDamage != null && _healthBarFill != null)
        {
            float target = _healthBarFill.fillAmount;
            _healthBarDamage.fillAmount = Mathf.SmoothDamp(
                _healthBarDamage.fillAmount, target, ref _damageBarVel, 0.4f);
        }
    }

    // ── Health ─────────────────────────────────────────────────────────────────

    /// <summary>Call from PlayerHealth.OnHealthChanged.</summary>
    public void UpdateHealth(int currentHealth)
    {
        float fraction = Mathf.Clamp01((float)currentHealth / _maxHealth);
        SetHealthBar(fraction, _healthBarDamage != null ? _healthBarDamage.fillAmount : fraction);

        if (_healthValueText != null)
            _healthValueText.text = $"{currentHealth}";

        // Tint bar red when low
        if (_healthBarFill != null)
            _healthBarFill.color = fraction > 0.35f
                ? Color.Lerp(new Color(1f, 0.6f, 0f), new Color(0.2f, 0.85f, 0.2f), (fraction - 0.35f) / 0.65f)
                : new Color(0.85f, 0.15f, 0.1f);
    }

    private void SetHealthBar(float fillFraction, float damageFraction)
    {
        if (_healthBarFill   != null) _healthBarFill.fillAmount   = fillFraction;
        if (_healthBarDamage != null) _healthBarDamage.fillAmount = damageFraction;
    }

    // ── Damage Popups ──────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a floating "-25" label coloured by remaining health.
    /// Called from PlayerHealth.OnHealthChanged (owner only).
    /// </summary>
    public void ShowDamageNumber(int damage, int healthAfterHit)
    {
        if (_damagePopupPrefab == null || _damagePopupContainer == null) return;

        GameObject popup = Instantiate(_damagePopupPrefab, _damagePopupContainer);

        // Slight random horizontal offset so repeated hits don't stack exactly
        popup.transform.localPosition = new Vector3(Random.Range(-30f, 30f), 0f, 0f);

        popup.GetComponent<DamagePopup>().Initialise(damage, healthAfterHit);
    }

    // ── Ammo ───────────────────────────────────────────────────────────────────

    public void UpdateAmmo(int currentAmmo, int maxAmmo)
        => _ammoText.text = $"{currentAmmo}  <size=70%><color=#aaaaaa>/  {maxAmmo}</color></size>";

    // ── Score ──────────────────────────────────────────────────────────────────

    public void UpdateScore(int p1, int p2)
        => _scoreText.text = $"{p1}  <color=#555555>|</color>  {p2}";

    // ── Hit Marker ─────────────────────────────────────────────────────────────

    public void ShowHitMarker()
    {
        _hitMarker.enabled = true;
        _hitMarkerTimer    = HIT_MARKER_DURATION;
    }

    // ── End Screen ─────────────────────────────────────────────────────────────

    public void ShowEndScreen(bool localPlayerWon)
    {
        _endScreen.SetActive(true);
        _endScreenText.text = localPlayerWon ? "YOU WIN!" : "YOU LOSE!";
    }

    // ── Round Banner ───────────────────────────────────────────────────────────

    /// <summary>Shows "ROUND 2" briefly at the start of a new round.</summary>
    public void ShowRoundBanner(int roundNumber)
    {
        StopAllCoroutines();
        StartCoroutine(RoundBannerRoutine(roundNumber));
    }

    private IEnumerator RoundBannerRoutine(int roundNumber)
    {
        _countdownText.gameObject.SetActive(false);
        _roundText.text = $"ROUND  <color=#F4C542>{roundNumber}</color>";

        yield return FadePanel(0f, 1f, 0.25f);   // fade in
        yield return new WaitForSeconds(1.8f);
        yield return FadePanel(1f, 0f, 0.35f);   // fade out
        _roundPanel.SetActive(false);
    }

    // ── Countdown UI ───────────────────────────────────────────────────────────

    /// <summary>Shows "NEXT ROUND IN  3 … 2 … 1 … GO!" then hides itself.</summary>
    public void StartCountdownUI(int seconds)
    {
        StopAllCoroutines();
        StartCoroutine(CountdownRoutine(seconds));
    }

    private IEnumerator CountdownRoutine(int seconds)
    {
        _roundText.text = "NEXT ROUND IN";
        _countdownText.gameObject.SetActive(true);

        yield return FadePanel(0f, 1f, 0.2f);    // fade in

        for (int i = seconds; i > 0; i--)
        {
            yield return PunchScale(_countdownText.transform, i.ToString(), 0.9f);
            yield return new WaitForSeconds(0.15f);
        }

        // "GO!"
        yield return PunchScale(_countdownText.transform, "<color=#F4C542>GO!</color>", 0.5f);
        yield return new WaitForSeconds(0.3f);

        yield return FadePanel(1f, 0f, 0.3f);    // fade out
        _roundPanel.SetActive(false);
    }

    // ── Panel Helpers ──────────────────────────────────────────────────────────

    private IEnumerator FadePanel(float from, float to, float duration)
    {
        _roundPanel.SetActive(true);
        if (_roundPanelGroup == null) { yield break; }

        float t = 0f;
        _roundPanelGroup.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            _roundPanelGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _roundPanelGroup.alpha = to;
    }

    /// <summary>Sets the text, punches the scale up then springs back.</summary>
    private IEnumerator PunchScale(Transform t, string newText, float holdSeconds)
    {
        _countdownText.text = newText;

        // Scale up
        float punch = 0f;
        while (punch < 0.15f)
        {
            punch += Time.deltaTime * 8f;
            t.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, punch / 0.15f);
            yield return null;
        }
        // Scale back
        float shrink = 0f;
        while (shrink < 0.2f)
        {
            shrink += Time.deltaTime * 8f;
            t.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, shrink / 0.2f);
            yield return null;
        }
        t.localScale = Vector3.one;
        yield return new WaitForSeconds(holdSeconds);
    }
}