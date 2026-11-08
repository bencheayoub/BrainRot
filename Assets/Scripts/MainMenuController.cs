using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Builds the whole main menu in code: main panel, character selection and settings.
// Put this on an empty GameObject in the MainMenu scene.
//
// Character selection is now a carousel: ONE big card in the middle, with < and > arrows.
// The card shows a live, slowly spinning 3D preview of the character (drag the prefabs into
// "Character Prefabs"). Without a prefab it shows a letter placeholder, so nothing is empty.
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
    [Tooltip("Optional 2D picture per character. If one is set it is shown instead of the 3D preview.")]
    [SerializeField] private Sprite[] characterPreviews;

    [Header("3D previews (same order as CharacterSelection)")]
    [Tooltip("0 = Tung Tung Tung Sahur, 1 = Potato with Glasses, 2 = Potato with Moustache, 3 = Ballerina Cappuccina")]
    [SerializeField] private GameObject[] characterPrefabs;
    [Tooltip("Optional texture per character (same idea as in PlayerVisual). Empty = keep the model's own material")]
    [SerializeField] private Texture2D[] characterTextures;
    [Tooltip("Optional rotation per character, e.g. X = -90 if the model lies down, Y = 180 if it faces away")]
    [SerializeField] private Vector3[] previewRotations;
    [Tooltip("How tall the character appears in the preview (the camera frames about 2.4 units)")]
    [SerializeField] private float previewHeight = 1.6f;
    [Tooltip("Spin speed in degrees per second. 0 = no spin")]
    [SerializeField] private float previewSpin = 40f;

    [Header("Character screen art (all optional)")]
    [SerializeField] private Sprite leftArrowSprite;
    [SerializeField] private Sprite rightArrowSprite;
    [Tooltip("Card frame picture. Empty = flat colored card")]
    [SerializeField] private Sprite cardSprite;
    [Tooltip("Card frame when this hero is selected. Empty = the normal card gets a golden tint")]
    [SerializeField] private Sprite cardSelectedSprite;

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

    // Carousel
    private int viewIndex;               // the character currently shown on the card
    private Image cardImage;
    private Text cardName;
    private Text cardStatus;
    private Text cardCounter;
    private Text placeholderText;
    private Image spriteImage;
    private RawImage previewRaw;

    // 3D preview stage
    private GameObject stageRoot;
    private Transform previewPivot;
    private RenderTexture previewRT;
    private GameObject[] previewModels;

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

    private void Update()
    {
        if (previewPivot != null && previewSpin != 0f && characterPanel.activeInHierarchy)
            previewPivot.Rotate(0f, previewSpin * Time.unscaledDeltaTime, 0f, Space.World);
    }

    private void OnDestroy()
    {
        if (previewRT != null) { previewRT.Release(); Destroy(previewRT); }
        if (stageRoot != null) Destroy(stageRoot);
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

    private void OnCardClicked()
    {
        CharacterSelection.Index = viewIndex;
        RefreshHero();
    }

    private void OnArrow(int direction)
    {
        int count = CharacterSelection.Names.Length;
        viewIndex = (viewIndex + direction + count) % count;
        if (previewPivot != null) previewPivot.rotation = Quaternion.identity;
        RefreshHero();
    }

    private void ShowPanel(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        characterPanel.SetActive(panel == characterPanel);
        settingsPanel.SetActive(panel == settingsPanel);

        // When opening the hero screen, start on the character that is currently selected
        if (panel == characterPanel)
        {
            viewIndex = CharacterSelection.Index;
            RefreshHero();
        }
    }

    private void RefreshHero()
    {
        heroLabel.text = "HERO: " + CharacterSelection.Name;

        if (cardImage == null) return;

        int count = CharacterSelection.Names.Length;
        bool selected = viewIndex == CharacterSelection.Index;

        if (cardSprite != null)
        {
            bool hasSelectedArt = cardSelectedSprite != null;
            cardImage.sprite = (selected && hasSelectedArt) ? cardSelectedSprite : cardSprite;
            cardImage.preserveAspect = false;
            cardImage.color = (selected && !hasSelectedArt) ? new Color(1f, 0.85f, 0.45f) : Color.white;
        }
        else
        {
            cardImage.color = selected ? cardSelectedColor : cardColor;
        }
        cardName.text = CharacterSelection.Names[viewIndex];
        cardStatus.text = selected ? "SELECTED" : "TAP TO SELECT";
        cardCounter.text = (viewIndex + 1) + " / " + count;

        Sprite sprite = (characterPreviews != null && viewIndex < characterPreviews.Length)
            ? characterPreviews[viewIndex] : null;

        bool hasModel = previewModels != null && viewIndex < previewModels.Length && previewModels[viewIndex] != null;

        if (previewModels != null)
        {
            for (int i = 0; i < previewModels.Length; i++)
                if (previewModels[i] != null) previewModels[i].SetActive(i == viewIndex && sprite == null);
        }

        spriteImage.sprite = sprite;
        spriteImage.enabled = sprite != null;
        previewRaw.enabled = sprite == null && hasModel;
        placeholderText.enabled = sprite == null && !hasModel;
        placeholderText.text = CharacterSelection.Names[viewIndex].Substring(0, 1);
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
        // A browser game can't quit, so the QUIT button only exists in Editor and desktop builds
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            CreateButton("QuitButton", p, "QUIT", new Vector2(0f, -390f), buttonSize, quitColor, true, OnQuit);

        heroLabel = CreateText("HeroLabel", p, "", 44, Color.white, new Vector2(0f, -580f), new Vector2(1000f, 100f));
    }

    private void BuildCharacterPanel()
    {
        Transform p = characterPanel.transform;

        // Keep this exact text: UISkinner finds the screen by it
        CreateText("Title", p, "CHOOSE YOUR HERO", 90, titleColor, new Vector2(0f, 700f), new Vector2(1000f, 150f));

        BuildPreviewStage();
        viewIndex = CharacterSelection.Index;

        // One big card in the middle
        Button card = CreateButton("Card", p, null, new Vector2(0f, 120f), new Vector2(600f, 800f),
            cardColor, false, OnCardClicked);
        cardImage = card.GetComponent<Image>();

        CreateImage("PreviewBackdrop", card.transform, null, new Vector2(0f, 150f), new Vector2(520f, 520f))
            .color = new Color(0f, 0f, 0f, 0.25f);

        GameObject rawGO = new GameObject("Preview3D", typeof(RectTransform), typeof(RawImage));
        rawGO.transform.SetParent(card.transform, false);
        previewRaw = rawGO.GetComponent<RawImage>();
        previewRaw.texture = previewRT;
        previewRaw.raycastTarget = false;
        RectTransform rawRt = previewRaw.rectTransform;
        rawRt.anchorMin = rawRt.anchorMax = rawRt.pivot = new Vector2(0.5f, 0.5f);
        rawRt.sizeDelta = new Vector2(520f, 520f);
        rawRt.anchoredPosition = new Vector2(0f, 150f);

        spriteImage = CreateImage("PreviewSprite", card.transform, null, new Vector2(0f, 150f), new Vector2(520f, 520f));
        placeholderText = CreateText("Placeholder", card.transform, "?", 260, new Color(1f, 1f, 1f, 0.5f),
            new Vector2(0f, 150f), new Vector2(520f, 400f));

        cardName = CreateText("Name", card.transform, "", 56, Color.white, new Vector2(0f, -210f), new Vector2(540f, 150f));
        cardStatus = CreateText("Status", card.transform, "", 40, Color.white, new Vector2(0f, -330f), new Vector2(540f, 70f));

        // Arrows left and right of the card
        Button left = CreateButton("LeftArrow", p, "<", new Vector2(-420f, 120f), new Vector2(150f, 150f),
            buttonColor, false, () => OnArrow(-1));
        Button right = CreateButton("RightArrow", p, ">", new Vector2(420f, 120f), new Vector2(150f, 150f),
            buttonColor, false, () => OnArrow(1));
        ApplyArrowArt(left, leftArrowSprite);
        ApplyArrowArt(right, rightArrowSprite);

        cardCounter = CreateText("Counter", p, "", 44, Color.white, new Vector2(0f, -390f), new Vector2(400f, 70f));

        CreateButton("BackButton", p, "BACK", new Vector2(0f, -560f), buttonSize, buttonColor, true,
            () => ShowPanel(mainPanel));
    }

    private void ApplyArrowArt(Button btn, Sprite sprite)
    {
        if (sprite == null) return;

        Image img = btn.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = Color.white;

        Text label = btn.GetComponentInChildren<Text>(true);
        if (label != null) label.enabled = false; // the art already shows the arrow
    }

    // A tiny hidden "photo studio" far below the menu: a camera filming the spinning model
    // into a RenderTexture, which the card then shows.
    private void BuildPreviewStage()
    {
        stageRoot = new GameObject("CharacterPreviewStage");
        stageRoot.transform.position = new Vector3(0f, -1000f, 0f);

        previewPivot = new GameObject("Pivot").transform;
        previewPivot.SetParent(stageRoot.transform, false);

        previewRT = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);

        GameObject camGO = new GameObject("PreviewCamera");
        camGO.transform.SetParent(stageRoot.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 0f, -4.5f);
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;
        cam.depth = -10f;
        cam.targetTexture = previewRT;

        GameObject lightGO = new GameObject("PreviewLight");
        lightGO.transform.SetParent(stageRoot.transform, false);
        lightGO.transform.rotation = Quaternion.Euler(30f, -30f, 0f);
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;

        int count = CharacterSelection.Names.Length;
        previewModels = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            if (characterPrefabs == null || i >= characterPrefabs.Length || characterPrefabs[i] == null) continue;

            GameObject model = Instantiate(characterPrefabs[i], previewPivot);
            model.name = "Preview_" + CharacterSelection.Names[i];
            StripGameplayParts(model);

            Vector3 rot = (previewRotations != null && i < previewRotations.Length) ? previewRotations[i] : Vector3.zero;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = model.transform.localRotation * Quaternion.Euler(rot);

            if (characterTextures != null && i < characterTextures.Length && characterTextures[i] != null)
                ApplyTexture(model, characterTextures[i]);

            if (TryGetBounds(model, out Bounds b) && b.size.y > 0.0001f)
            {
                model.transform.localScale *= previewHeight / b.size.y;
                if (TryGetBounds(model, out b))
                    model.transform.position += previewPivot.position - b.center;
            }

            model.SetActive(false);
            previewModels[i] = model;
        }
    }

    private static void StripGameplayParts(GameObject model)
    {
        foreach (Collider c in model.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (Rigidbody rb in model.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (AudioListener al in model.GetComponentsInChildren<AudioListener>(true)) Destroy(al);
        foreach (Camera cam in model.GetComponentsInChildren<Camera>(true)) { cam.enabled = false; Destroy(cam); }
        foreach (Light l in model.GetComponentsInChildren<Light>(true)) { l.enabled = false; Destroy(l); }
    }

    private static bool TryGetBounds(GameObject model, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found;
    }

    private static void ApplyTexture(GameObject model, Texture2D texture)
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material baseMat = temp.GetComponent<Renderer>().sharedMaterial;
        Destroy(temp);
        if (baseMat == null) return;

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = new Material(baseMat);
                mat.mainTexture = texture;
                mat.color = Color.white;
                mats[i] = mat;
            }
            r.sharedMaterials = mats;
        }
    }

    private void BuildSettingsPanel()
    {
        Transform p = settingsPanel.transform;

        CreateText("Title", p, "SETTINGS", 110, titleColor, new Vector2(0f, 600f), new Vector2(1000f, 200f));
        CreateToggle("MUSIC", p, new Vector2(0f, 250f), buttonSize,
            () => GameAudio.MusicOn, v => GameAudio.MusicOn = v);
        CreateToggle("SOUND", p, new Vector2(0f, 50f), buttonSize,
            () => GameAudio.SfxOn, v => GameAudio.SfxOn = v);
        CreateButton("BackButton", p, "BACK", new Vector2(0f, -250f), buttonSize, buttonColor, true,
            () => ShowPanel(mainPanel));
    }

    // A button that flips an ON/OFF setting and shows its state ("MUSIC: ON")
    private void CreateToggle(string title, Transform parent, Vector2 pos, Vector2 size,
                              System.Func<bool> get, System.Action<bool> set)
    {
        Button btn = CreateButton(title + "Toggle", parent, null, pos, size, playColor, true, () => { });
        Image img = btn.GetComponent<Image>();
        Text label = CreateText("Label", btn.transform, "", 64, Color.white, Vector2.zero, size);

        void Refresh()
        {
            bool on = get();
            label.text = title + ": " + (on ? "ON" : "OFF");
            if (buttonSprite != null) img.color = on ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            else img.color = on ? playColor : quitColor;
        }

        btn.onClick.AddListener(() => { set(!get()); Refresh(); });
        Refresh();
    }

    // ----------------------------------------------------------------- helpers

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

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
        btn.onClick.AddListener(SoundManager.PlayClick); // click sound first, then the action
        btn.onClick.AddListener(onClick);

        if (!string.IsNullOrEmpty(label))
        {
            CreateText("Label", obj.transform, label, 64, Color.white, Vector2.zero, size);
        }

        return btn;
    }
}
