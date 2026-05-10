using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// Attach to a prefab that has a TextMeshProUGUI component.
/// HUDManager spawns it; it animates itself then self-destructs.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private TextMeshProUGUI _text;

    // HP thresholds (health AFTER the hit)
    private static readonly Color ColourHealthy  = new Color(0.25f, 0.92f, 0.25f);   // green   100→75
    private static readonly Color ColourWarning  = new Color(1.00f, 0.75f, 0.00f);   // yellow  75→50
    private static readonly Color ColourDanger   = new Color(1.00f, 0.38f, 0.00f);   // orange  50→25
    private static readonly Color ColourCritical = new Color(0.95f, 0.10f, 0.10f);   // red     25→0

    void Awake() => _text = GetComponent<TextMeshProUGUI>();

    /// <summary>Called immediately after spawning.</summary>
    public void Initialise(int damage, int healthAfterHit)
    {
        _text.text  = $"-{damage}";
        _text.color = PickColour(healthAfterHit);
        StartCoroutine(Animate());
    }

    private static Color PickColour(int hp)
    {
        if (hp >= 75) return ColourHealthy;
        if (hp >= 50) return ColourWarning;
        if (hp >= 25) return ColourDanger;
        return ColourCritical;
    }

    private IEnumerator Animate()
    {
        float duration  = 1.2f;
        float elapsed   = 0f;

        Vector3 startPos   = transform.localPosition;
        Vector3 targetPos  = startPos + new Vector3(Random.Range(-18f, 18f), 80f, 0f);
        Color   startColor = _text.color;

        // Punch scale on spawn
        transform.localScale = Vector3.one * 1.4f;
        float scaleT = 0f;
        while (scaleT < 0.12f)
        {
            scaleT += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, scaleT / 0.12f);
            yield return null;
        }
        transform.localScale = Vector3.one;

        // Float up + fade out
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);

            // Only start fading in the second half
            float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
            _text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}