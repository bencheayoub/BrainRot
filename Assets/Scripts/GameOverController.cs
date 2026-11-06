using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Builds the game over screen in code: final score, high score, Retry and Main Menu.
// Put this on an empty GameObject in the Game scene. Nothing else is needed.
public class GameOverController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Timing")]
    [Tooltip("Real seconds to wait after the last heart is lost, before the screen appears")]
    [SerializeField] private float showDelay = 0.6f;

    [Header("Optional art (leave empty for placeholders)")]
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite panelBackground;

    private readonly Color dimColor = new Color(0f, 0f, 0f, 0.75f);
    private readonly Color titleColor = new Color(0.95f, 0.3f, 0.3f);
    private readonly Color goldColor = new Color(1f, 0.85f, 0.2f);
    private readonly Color retryColor = new Color(0.2f, 0.7f, 0.35f);
    private readonly Color quitColor = new Color(0.75f, 0.25f, 0.25f);
    private readonly Vector2 buttonSize = new Vector2(640f, 150f);

    private Font font;
    private GameManager gm;
    private GameObject panel;
    private Text scoreText;
    private Text highScoreText;
    private RectTransform newHighRect;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        EnsureEventSystem();
        BuildUI();

        gm = GameManager.Instance;
        if (gm != null) gm.GameOver += OnGameOver;
    }

    private void OnDestroy()
    {
        if (gm != null) gm.GameOver -= OnGameOver;

        // Safety: never leave the game frozen when this scene ends or Play stops
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Small pulse on the "NEW HIGH SCORE" label (uses real time, the game is frozen)
        if (newHighRect != null && newHighRect.gameObject.activeInHierarchy)
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f;
            newHighRect.localScale = new Vector3(s, s, 1f);
        }
    }

    // ---------------------------------------------------------------- actions

    private void OnGameOver()
    {
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return new WaitForSecondsRealtime(showDelay);

        scoreText.text = gm.Score.ToString();
        highScoreText.text = "HIGH SCORE: " + gm.HighScore;
        newHighRect.gameObject.SetActive(gm.IsNewHighScore);

        panel.SetActive(true);
        Time.timeScale = 0f; // freezes falling, fruits and the background
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    // ------------------------------------------------------------- UI building

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("GameOver Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30; // above HUD (10) and pause (20)

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image dim = panel.GetComponent<Image>();
        if (panelBackground != null) { dim.sprite = panelBackground; dim.color = Color.white; }
        else dim.color = dimColor;
        dim.raycastTarget = true; // blocks clicks behind the panel

        Transform p = panel.transform;
        CreateText("Title", p, "GAME OVER", 140, titleColor, new Vector2(0f, 600f), new Vector2(1000f, 220f));
        CreateText("FinalLabel", p, "FINAL SCORE", 60, Color.white, new Vector2(0f, 380f), new Vector2(1000f, 100f));
        scoreText = CreateText("Score", p, "0", 220, goldColor, new Vector2(0f, 220f), new Vector2(1000f, 280f));
        highScoreText = CreateText("HighScore", p, "HIGH SCORE: 0", 56, Color.white,
            new Vector2(0f, 20f), new Vector2(1000f, 100f));
        newHighRect = CreateText("NewHigh", p, "NEW HIGH SCORE!", 70, goldColor,
            new Vector2(0f, -90f), new Vector2(1000f, 120f)).rectTransform;

        CreateButton("RetryButton", p, "RETRY", new Vector2(0f, -330f), buttonSize, retryColor, true, Retry);
        CreateButton("MenuButton", p, "MAIN MENU", new Vector2(0f, -510f), buttonSize, quitColor, true, GoToMenu);

        panel.SetActive(false);
    }

    // ----------------------------------------------------------------- helpers

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule module = es.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    private Text CreateText(string name, Transform parent, string content, int size, Color color,
                            Vector2 pos, Vector2 box)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        obj.transform.SetParent(parent, false);

        Text t = obj.GetComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = obj.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);

        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        return t;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2 size,
                                Color color, bool useArt, UnityAction onClick)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        Image img = obj.GetComponent<Image>();
        if (useArt && buttonSprite != null)
        {
            img.sprite = buttonSprite;
            img.color = Color.white;
        }
        else
        {
            img.color = color;
        }

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);

        if (!string.IsNullOrEmpty(label))
        {
            CreateText("Label", obj.transform, label, 64, Color.white, Vector2.zero, size);
        }

        return btn;
    }
}
