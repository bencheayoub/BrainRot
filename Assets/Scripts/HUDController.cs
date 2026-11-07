using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Builds the whole HUD in code: hearts (top-left), score + icon (top-right)
// and a temporary GAME OVER label. No Canvas needs to be created by hand.
// If your teammate's art is assigned in the Inspector it is used,
// otherwise simple generated shapes are shown.
public class HUDController : MonoBehaviour
{
    [Header("Optional art (leave empty for placeholders)")]
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;
    [SerializeField] private Sprite scoreIcon;

    [Header("Layout")]
    [SerializeField] private float heartSize = 90f;
    [SerializeField] private float heartSpacing = 10f;
    [SerializeField] private float margin = 40f;
    [SerializeField] private float scoreIconSize = 80f;
    [SerializeField] private int scoreFontSize = 64;

    private Image[] hearts;
    private Text scoreText;
    private Text gameOverText;

    private Sprite generatedHeart;
    private Sprite fullSprite;
    private Sprite emptySprite;
    private Color fullColor;
    private Color emptyColor;
    private Font font;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("HUDController: no GameManager found in the scene.");
            return;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        PrepareHeartArt();
        BuildUI();

        GameManager gm = GameManager.Instance;
        gm.ScoreChanged += OnScoreChanged;
        gm.HeartsChanged += OnHeartsChanged;
        gm.GameOver += OnGameOver;

        scoreText.text = gm.Score.ToString();
        UpdateHearts(gm.Hearts);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.ScoreChanged -= OnScoreChanged;
        GameManager.Instance.HeartsChanged -= OnHeartsChanged;
        GameManager.Instance.GameOver -= OnGameOver;
    }

    // ---------------------------------------------------------------- events

    private void OnScoreChanged(int score)
    {
        scoreText.text = score.ToString();
        StartCoroutine(Punch(scoreText.rectTransform));
    }

    private void OnHeartsChanged(int hearts)
    {
        UpdateHearts(hearts);

        // The heart that just became empty pops
        if (hearts >= 0 && hearts < this.hearts.Length)
        {
            StartCoroutine(Punch(this.hearts[hearts].rectTransform));
        }
    }

    private void OnGameOver()
    {
        //
    }

    private void UpdateHearts(int current)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            bool isFull = i < current;
            hearts[i].sprite = isFull ? fullSprite : emptySprite;
            hearts[i].color = isFull ? fullColor : emptyColor;
        }
    }

    // ------------------------------------------------------------- UI building

    private void BuildUI()
    {
        // Canvas that scales with the screen size
        GameObject canvasGO = new GameObject("HUD Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        // Keeps the HUD away from phone notches and rounded corners
        RectTransform safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        safe.SetParent(canvasGO.transform, false);
        Rect sa = Screen.safeArea;
        safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        safe.offsetMin = Vector2.zero;
        safe.offsetMax = Vector2.zero;

        // Hearts, top-left
        int max = GameManager.Instance.MaxHearts;
        hearts = new Image[max];
        for (int i = 0; i < max; i++)
        {
            Image img = CreateImage("Heart" + i, safe, heartSize);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(margin + heartSize * 0.5f + i * (heartSize + heartSpacing),
                                              -(margin + heartSize * 0.5f));
            hearts[i] = img;
        }

        // Score icon, top-right
        Image icon = CreateImage("ScoreIcon", safe, scoreIconSize);
        icon.sprite = scoreIcon != null ? scoreIcon : GenerateCoinSprite();
        icon.color = Color.white;
        RectTransform iconRt = icon.rectTransform;
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(1f, 1f);
        iconRt.pivot = new Vector2(1f, 1f);
        iconRt.anchoredPosition = new Vector2(-margin, -margin);

        // Score number, to the left of the icon
        scoreText = CreateText("ScoreText", safe, scoreFontSize, TextAnchor.MiddleRight, Color.white);
        RectTransform st = scoreText.rectTransform;
        st.anchorMin = st.anchorMax = new Vector2(1f, 1f);
        st.pivot = new Vector2(1f, 1f);
        st.sizeDelta = new Vector2(420f, scoreIconSize);
        st.anchoredPosition = new Vector2(-(margin + scoreIconSize + 15f), -margin);

        // Temporary GAME OVER label (replaced by the game over screen in step 14)
        gameOverText = CreateText("GameOverText", safe, 130, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.35f));
        gameOverText.text = "GAME OVER";
        RectTransform go = gameOverText.rectTransform;
        go.anchorMin = go.anchorMax = new Vector2(0.5f, 0.5f);
        go.pivot = new Vector2(0.5f, 0.5f);
        go.sizeDelta = new Vector2(1000f, 250f);
        go.anchoredPosition = Vector2.zero;
        gameOverText.gameObject.SetActive(false);
    }

    private Image CreateImage(string name, Transform parent, float size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        Image img = obj.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.rectTransform.sizeDelta = new Vector2(size, size);
        return img;
    }

    private Text CreateText(string name, Transform parent, int size, TextAnchor anchor, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        obj.transform.SetParent(parent, false);

        Text t = obj.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = anchor;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = obj.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    // ------------------------------------------------------------ placeholders

    private void PrepareHeartArt()
    {
        generatedHeart = GenerateHeartSprite();

        fullSprite = heartFull != null ? heartFull : generatedHeart;
        emptySprite = heartEmpty != null ? heartEmpty : generatedHeart;

        // Generated hearts are white shapes, so they get a tint. Real art is shown as is.
        fullColor = heartFull != null ? Color.white : new Color(1f, 0.2f, 0.3f);
        emptyColor = heartEmpty != null ? Color.white : new Color(0.15f, 0.15f, 0.15f, 0.8f);
    }

    private Sprite GenerateHeartSprite()
    {
        const int s = 128;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                // Map the pixel to the heart equation's range
                float fx = (x + 0.5f) / s * 2.6f - 1.3f;
                float fy = (y + 0.5f) / s * 2.6f - 1.25f;

                float a = fx * fx + fy * fy - 1f;
                float f = a * a * a - fx * fx * fy * fy * fy;

                px[y * s + x] = f <= 0f ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite GenerateCoinSprite()
    {
        const int s = 64;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f;
                float dy = (y + 0.5f) / s * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > 1f) px[y * s + x] = new Color32(0, 0, 0, 0);
                else if (d > 0.78f) px[y * s + x] = new Color32(204, 140, 15, 255);
                else px[y * s + x] = new Color32(255, 217, 26, 255);
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    // ------------------------------------------------------------------- juice

    // Quick size pop that settles back to normal
    private IEnumerator Punch(RectTransform rt)
    {
        const float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(1.4f, 1f, t);
            if (rt != null) rt.localScale = Vector3.one * scale;
            yield return null;
        }

        if (rt != null) rt.localScale = Vector3.one;
    }
}
