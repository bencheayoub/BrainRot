using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Builds the pause button and the pause screen in code.
// Put this on an empty GameObject in the Game scene. Nothing else is needed.
public class PauseMenuController : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    // The pause button's rectangle, so the player can ignore presses on it
    private static RectTransform pauseButtonRect;

    // True when a screen position (mouse or finger) is on the visible pause button
    public static bool IsOverPauseButton(Vector2 screenPosition)
    {
        if (pauseButtonRect == null || !pauseButtonRect.gameObject.activeInHierarchy) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(pauseButtonRect, screenPosition, null);
    }

    [Header("Scenes")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Optional art (leave empty for placeholders)")]
    [SerializeField] private Sprite pauseIcon;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite panelBackground;

    [Header("Layout")]
    [SerializeField] private float pauseButtonSize = 90f;
    [SerializeField] private float margin = 40f;

    private readonly Color dimColor = new Color(0f, 0f, 0f, 0.7f);
    private readonly Color titleColor = new Color(1f, 0.85f, 0.2f);
    private readonly Color resumeColor = new Color(0.2f, 0.7f, 0.35f);
    private readonly Color buttonColor = new Color(0.25f, 0.4f, 0.85f);
    private readonly Color quitColor = new Color(0.75f, 0.25f, 0.25f);
    private readonly Vector2 buttonSize = new Vector2(640f, 150f);

    private Font font;
    private GameManager gm;
    private GameObject pauseButton;
    private GameObject pausePanel;

    private void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

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

        pauseButtonRect = null;

        // Safety: never leave the game frozen when this scene ends or Play stops
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Update()
    {
        if (!PausePressed()) return;

        if (IsPaused) Resume();
        else Pause();
    }

    // ---------------------------------------------------------------- actions

    private void Pause()
    {
        if (IsPaused) return;
        if (gm != null && gm.IsGameOver) return;

        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        pausePanel.SetActive(true);
        pauseButton.SetActive(false);
    }

    private void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        pausePanel.SetActive(false);
        pauseButton.SetActive(true);
    }

    private void Restart()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(menuSceneName);
    }

    private void OnGameOver()
    {
        pauseButton.SetActive(false);
    }

    private bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
    }

    // ------------------------------------------------------------- UI building

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("Pause Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // above the HUD (10)

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>(); // needed so buttons can be clicked

        // Pause button, top center (inside the phone safe area)
        RectTransform safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        safe.SetParent(canvasGO.transform, false);
        Rect sa = Screen.safeArea;
        safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        safe.offsetMin = Vector2.zero;
        safe.offsetMax = Vector2.zero;

        BuildPauseButton(safe);
        BuildPausePanel(canvasGO.transform);
        pausePanel.SetActive(false);
    }

    private void BuildPauseButton(Transform parent)
    {
        Button btn = CreateButton("PauseButton", parent, null, Vector2.zero,
            new Vector2(pauseButtonSize, pauseButtonSize), new Color(0f, 0f, 0f, 0.45f), false, Pause);
        pauseButton = btn.gameObject;

        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -margin);
        pauseButtonRect = rt;

        if (pauseIcon != null)
        {
            Image img = btn.GetComponent<Image>();
            img.sprite = pauseIcon;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        else
        {
            // Two white bars = the classic pause symbol
            float barW = pauseButtonSize * 0.16f;
            float barH = pauseButtonSize * 0.5f;
            CreateBar(btn.transform, -pauseButtonSize * 0.14f, barW, barH);
            CreateBar(btn.transform, pauseButtonSize * 0.14f, barW, barH);
        }
    }

    private void CreateBar(Transform parent, float x, float w, float h)
    {
        GameObject obj = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        Image img = obj.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    private void BuildPausePanel(Transform parent)
    {
        pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        pausePanel.transform.SetParent(parent, false);

        RectTransform rt = pausePanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image dim = pausePanel.GetComponent<Image>();
        if (panelBackground != null) { dim.sprite = panelBackground; dim.color = Color.white; }
        else dim.color = dimColor;
        dim.raycastTarget = true; // blocks clicks on anything behind the panel

        Transform p = pausePanel.transform;
        CreateText("Title", p, "PAUSED", 150, titleColor, new Vector2(0f, 450f), new Vector2(1000f, 250f));
        CreateButton("ResumeButton", p, "RESUME", new Vector2(0f, 120f), buttonSize, resumeColor, true, Resume);
        CreateButton("RestartButton", p, "RESTART", new Vector2(0f, -60f), buttonSize, buttonColor, true, Restart);
        CreateButton("MenuButton", p, "MAIN MENU", new Vector2(0f, -240f), buttonSize, quitColor, true, GoToMenu);

        Vector2 half = new Vector2(310f, 130f);
        CreateToggle("MUSIC", p, new Vector2(-165f, -430f), half,
            () => GameAudio.MusicOn, v => GameAudio.MusicOn = v);
        CreateToggle("SOUND", p, new Vector2(165f, -430f), half,
            () => GameAudio.SfxOn, v => GameAudio.SfxOn = v);
    }

    // A button that flips an ON/OFF setting and shows its state ("MUSIC: ON")
    private void CreateToggle(string title, Transform parent, Vector2 pos, Vector2 size,
                              System.Func<bool> get, System.Action<bool> set)
    {
        Button btn = CreateButton(title + "Toggle", parent, null, pos, size, resumeColor, true, () => { });
        Image img = btn.GetComponent<Image>();
        Text label = CreateText("Label", btn.transform, "", 46, Color.white, Vector2.zero, size);

        void Refresh()
        {
            bool on = get();
            label.text = title + ": " + (on ? "ON" : "OFF");
            if (buttonSprite != null) img.color = on ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            else img.color = on ? resumeColor : quitColor;
        }

        btn.onClick.AddListener(() => { set(!get()); Refresh(); });
        Refresh();
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
