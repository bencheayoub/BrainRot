using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Builds the whole main menu in code: main panel, character selection and settings.
// Put this on an empty GameObject in the MainMenu scene. Nothing else is needed.
public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Text")]
    [SerializeField] private string gameTitle = "FALL";

    [Header("Optional art (leave empty for placeholders)")]
    [SerializeField] private Sprite background;
    [SerializeField] private Sprite logo;
    [SerializeField] private Sprite buttonSprite;
    [Tooltip("Element 0 = Tung Tung Tung Sahur, Element 1 = Potato")]
    [SerializeField] private Sprite[] characterPreviews;

    private readonly Color bgColor = new Color(0.07f, 0.09f, 0.2f);
    private readonly Color titleColor = new Color(1f, 0.85f, 0.2f);
    private readonly Color playColor = new Color(0.2f, 0.7f, 0.35f);
    private readonly Color buttonColor = new Color(0.25f, 0.4f, 0.85f);
    private readonly Color quitColor = new Color(0.75f, 0.25f, 0.25f);
    private readonly Color cardColor = new Color(0.2f, 0.24f, 0.4f);
    private readonly Color cardSelectedColor = new Color(0.95f, 0.7f, 0.15f);

    private readonly Vector2 buttonSize = new Vector2(640f, 150f);

    private Font font;
    private GameObject mainPanel;
    private GameObject characterPanel;
    private GameObject settingsPanel;
    private Text heroLabel;
    private Image[] cardImages;
    private Text[] cardStatus;

    private void Start()
    {
        Time.timeScale = 1f; // in case we came back from a paused game

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        EnsureEventSystem();
        BuildUI();
        RefreshHero();
        ShowPanel(mainPanel);
    }

    // ---------------------------------------------------------------- actions

    private void OnPlay()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnCardClicked(int index)
    {
        CharacterSelection.Index = index;
        RefreshHero();
    }

    private void ShowPanel(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        characterPanel.SetActive(panel == characterPanel);
        settingsPanel.SetActive(panel == settingsPanel);
    }

    private void RefreshHero()
    {
        heroLabel.text = "HERO: " + CharacterSelection.Name;

        for (int i = 0; i < cardImages.Length; i++)
        {
            bool selected = i == CharacterSelection.Index;
            cardImages[i].color = selected ? cardSelectedColor : cardColor;
            cardStatus[i].text = selected ? "SELECTED" : "TAP TO SELECT";
        }
    }

    // ------------------------------------------------------------- UI building

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("Menu Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>(); // needed so buttons can be clicked

        // Background
        GameObject bg = CreatePanel("Background", canvasGO.transform);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.raycastTarget = false;
        if (background != null) { bgImg.sprite = background; bgImg.color = Color.white; }
        else bgImg.color = bgColor;

        mainPanel = CreatePanel("MainPanel", canvasGO.transform);
        characterPanel = CreatePanel("CharacterPanel", canvasGO.transform);
        settingsPanel = CreatePanel("SettingsPanel", canvasGO.transform);

        BuildMainPanel();
        BuildCharacterPanel();
        BuildSettingsPanel();
    }

    private void BuildMainPanel()
    {
        Transform p = mainPanel.transform;

        if (logo != null)
            CreateImage("Logo", p, logo, new Vector2(0f, 560f), new Vector2(900f, 400f));
        else
            CreateText("Title", p, gameTitle, 230, titleColor, new Vector2(0f, 560f), new Vector2(1000f, 300f));

        CreateButton("PlayButton", p, "PLAY", new Vector2(0f, 150f), buttonSize, playColor, true, OnPlay);
        CreateButton("CharactersButton", p, "CHARACTERS", new Vector2(0f, -30f), buttonSize, buttonColor, true,
            () => ShowPanel(characterPanel));
        CreateButton("SettingsButton", p, "SETTINGS", new Vector2(0f, -210f), buttonSize, buttonColor, true,
            () => ShowPanel(settingsPanel));
        CreateButton("QuitButton", p, "QUIT", new Vector2(0f, -390f), buttonSize, quitColor, true, OnQuit);

        heroLabel = CreateText("HeroLabel", p, "", 44, Color.white, new Vector2(0f, -580f), new Vector2(1000f, 100f));
    }

    private void BuildCharacterPanel()
    {
        Transform p = characterPanel.transform;

        CreateText("Title", p, "CHOOSE YOUR HERO", 90, titleColor, new Vector2(0f, 700f), new Vector2(1000f, 150f));

        int count = CharacterSelection.Names.Length;
        cardImages = new Image[count];
        cardStatus = new Text[count];

        for (int i = 0; i < count; i++)
        {
            int index = i; // copy, so each button remembers its own number
            float x = (i - (count - 1) * 0.5f) * 500f;

            Button card = CreateButton("Card" + i, p, null, new Vector2(x, 120f), new Vector2(460f, 760f),
                cardColor, false, () => OnCardClicked(index));
            cardImages[i] = card.GetComponent<Image>();

            Sprite preview = (characterPreviews != null && i < characterPreviews.Length) ? characterPreviews[i] : null;
            Image previewImg = CreateImage("Preview", card.transform, preview, new Vector2(0f, 130f), new Vector2(380f, 380f));
            if (preview == null) previewImg.color = new Color(0f, 0f, 0f, 0.25f);

            CreateText("Name", card.transform, CharacterSelection.Names[i], 48, Color.white,
                new Vector2(0f, -170f), new Vector2(420f, 140f));
            cardStatus[i] = CreateText("Status", card.transform, "", 38, Color.white,
                new Vector2(0f, -300f), new Vector2(420f, 70f));
        }

        CreateButton("BackButton", p, "BACK", new Vector2(0f, -560f), buttonSize, buttonColor, true,
            () => ShowPanel(mainPanel));
    }

    private void BuildSettingsPanel()
    {
        Transform p = settingsPanel.transform;

        CreateText("Title", p, "SETTINGS", 110, titleColor, new Vector2(0f, 600f), new Vector2(1000f, 200f));
        CreateText("Info", p, "Music and sound effects\nswitches come in step 15.", 54, Color.white,
            new Vector2(0f, 150f), new Vector2(900f, 300f));
        CreateButton("BackButton", p, "BACK", new Vector2(0f, -300f), buttonSize, buttonColor, true,
            () => ShowPanel(mainPanel));
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

    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return obj;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return img;
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