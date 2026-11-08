using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Flashes the screen so hits and bonuses FEEL like something:
//   losing a heart  -> red flash
//   golden fruit    -> gold flash
//   bad fruit       -> short brown flash
// Put this on the GameManager object. It builds its own overlay and listens to GameManager events.
public class HitFeedback : MonoBehaviour
{
    [Header("Colors (alpha = strength)")]
    [SerializeField] private Color hurtColor = new Color(1f, 0.10f, 0.10f, 0.50f);
    [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.20f, 0.30f);
    [SerializeField] private Color badColor = new Color(0.45f, 0.25f, 0.10f, 0.25f);

    [Header("Timing")]
    [SerializeField] private float flashDuration = 0.35f;

    private Image flash;
    private Coroutine running;

    private void Start()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("HitFeedback: no GameManager found in the scene.");
            return;
        }

        BuildOverlay();

        gm.HeartsChanged += OnHeartsChanged;
        gm.FruitCaught += OnFruitCaught;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.HeartsChanged -= OnHeartsChanged;
        GameManager.Instance.FruitCaught -= OnFruitCaught;
    }

    private void OnHeartsChanged(int hearts)
    {
        Flash(hurtColor);
    }

    private void OnFruitCaught(Fruit fruit)
    {
        if (fruit.type == FruitType.Golden) Flash(goldColor);
        else if (fruit.type == FruitType.Bad) Flash(badColor);
    }

    private void BuildOverlay()
    {
        // Sits below the HUD (which uses sorting order 10), so hearts and score stay clear
        GameObject canvasGO = new GameObject("Flash Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        GameObject imgGO = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        imgGO.transform.SetParent(canvasGO.transform, false);

        flash = imgGO.GetComponent<Image>();
        flash.raycastTarget = false;
        flash.color = Color.clear;

        RectTransform rt = flash.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Flash(Color color)
    {
        if (flash == null) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / flashDuration);
            flash.color = new Color(color.r, color.g, color.b, color.a * fade);
            yield return null;
        }

        flash.color = Color.clear;
        running = null;
    }
}
