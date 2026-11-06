using System.Collections;
using UnityEngine;

// Shows a floating "+10" / "-5" / "+50" where the player caught something.
// Listens to GameManager.ScorePopup, so nothing else needs to call it.
public class ScorePopups : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("Size of the text in the world (raise it if the text is too small)")]
    [SerializeField] private float textSize = 0.08f;
    [SerializeField] private int fontSize = 64;
    [Tooltip("Pop-ups worth at least this many points use the bonus color")]
    [SerializeField] private int bonusThreshold = 50;

    [Header("Colors")]
    [SerializeField] private Color gainColor = new Color(0.30f, 1.00f, 0.35f);
    [SerializeField] private Color lossColor = new Color(1.00f, 0.30f, 0.30f);
    [SerializeField] private Color bonusColor = new Color(1.00f, 0.85f, 0.10f);

    [Header("Motion")]
    [SerializeField] private float riseDistance = 1.3f;
    [SerializeField] private float duration = 0.9f;
    [SerializeField] private float startHeightOffset = 0.6f;

    private Font font;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        // Built-in font, so no assets are needed
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ScorePopup += OnScorePopup;
        }
        else
        {
            Debug.LogWarning("ScorePopups: no GameManager found in the scene.");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ScorePopup -= OnScorePopup;
        }
    }

    private void OnScorePopup(Vector3 worldPos, int points)
    {
        StartCoroutine(Animate(worldPos, points));
    }

    private IEnumerator Animate(Vector3 worldPos, int points)
    {
        // Keep the text fully on screen, in front of everything (z = -1)
        float halfWidth = cam.orthographicSize * cam.aspect;
        Vector3 start = new Vector3(
            Mathf.Clamp(worldPos.x, -halfWidth + 0.7f, halfWidth - 0.7f),
            worldPos.y + startHeightOffset,
            -1f);

        GameObject go = new GameObject("ScorePopup");
        go.transform.position = start;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = points > 0 ? "+" + points : points.ToString();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = fontSize;
        tm.characterSize = textSize;
        tm.fontStyle = FontStyle.Bold;
        tm.font = font;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 100;

        Color baseColor = points >= bonusThreshold ? bonusColor : (points > 0 ? gainColor : lossColor);
        tm.color = baseColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Rise, fast at first and slowing down
            float ease = 1f - (1f - t) * (1f - t);
            go.transform.position = start + Vector3.up * (ease * riseDistance);

            // Quick pop in size, then settle
            float scale;
            if (t < 0.15f) scale = Mathf.Lerp(0.5f, 1.25f, t / 0.15f);
            else if (t < 0.35f) scale = Mathf.Lerp(1.25f, 1f, (t - 0.15f) / 0.2f);
            else scale = 1f;
            go.transform.localScale = Vector3.one * scale;

            // Fade out during the second half
            float alpha = 1f - Mathf.InverseLerp(0.5f, 1f, t);
            tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        Destroy(go);
    }
}
