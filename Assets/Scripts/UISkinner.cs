using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Applies the 2D art to UI that other scripts already build, WITHOUT editing those scripts.
// Put one UISkinner in the MainMenu scene (menu art) and one in the Game scene (game over art).
// It looks for UI by what it says:
//   buttons labelled PLAY / CHARACTERS / SETTINGS / QUIT  -> replaced by the button art
//   the PLAY screen                                       -> gets the background WITH the logo, the "FALL" title text is hidden
//   screens titled SETTINGS / CHOOSE YOUR HERO            -> get the background WITHOUT the logo
//   a text reading FINAL SCORE                            -> game over background is added, "GAME OVER" texts are hidden
// Flat full-screen colored panels inside a screen (the navy fill, the dark overlay) are switched off so the art is visible.
// Works with both normal UI Text and TextMeshPro. Anything it can't match is left unchanged.
// Runs its Start AFTER the menu scripts built their UI, so the art is applied before the first frame is drawn
[DefaultExecutionOrder(100)]
public class UISkinner : MonoBehaviour
{
    [Header("Main menu scene")]
    [Tooltip("Background WITH the logo, shown only on the main screen (the one with PLAY)")]
    [SerializeField] private Sprite menuBackground;
    [Tooltip("Background WITHOUT the logo, shown on the Settings and Choose Your Hero screens")]
    [SerializeField] private Sprite subScreenBackground;
    [SerializeField] private Sprite playButton;
    [SerializeField] private Sprite charactersButton;
    [SerializeField] private Sprite settingsButton;
    [SerializeField] private Sprite quitButton;

    [Header("Other buttons (any screen, either scene)")]
    [Tooltip("Buttons labelled BACK")]
    [SerializeField] private Sprite backButton;
    [Tooltip("The < and > arrows on the hero select screen")]
    [SerializeField] private Sprite leftArrowButton;
    [SerializeField] private Sprite rightArrowButton;
    [Tooltip("Buttons labelled RETRY or RESTART")]
    [SerializeField] private Sprite retryButton;
    [Tooltip("Buttons labelled MAIN MENU")]
    [SerializeField] private Sprite mainMenuButton;
    [Tooltip("Buttons labelled RESUME")]
    [SerializeField] private Sprite resumeButton;
    [Tooltip("The MUSIC button when music is ON / OFF (changes automatically when clicked)")]
    [SerializeField] private Sprite musicOnButton;
    [SerializeField] private Sprite musicOffButton;
    [Tooltip("The SOUND button when sound is ON / OFF")]
    [SerializeField] private Sprite soundOnButton;
    [SerializeField] private Sprite soundOffButton;

    [Header("Main menu buttons position")]
    [Tooltip("Moves the four menu buttons up (+) or down (-), in canvas units. Use a negative number if they overlap the logo.")]
    [SerializeField] private float menuButtonsOffsetY = 0f;

    [Tooltip("Hides the \"HERO: ...\" line at the bottom of the main menu")]
    [SerializeField] private bool hideHeroLabel = true;

    [Tooltip("Titles (in capitals) that mark the Settings / hero select screens")]
    [SerializeField] private string[] subScreenTitles = { "SETTINGS", "CHOOSE YOUR HERO" };

    [Header("Game scene")]
    [SerializeField] private Sprite gameOverBackground;
    [Tooltip("Optional: background for the pause screen (found by its PAUSED title)")]
    [SerializeField] private Sprite pauseBackground;

    private readonly HashSet<Button> done = new HashSet<Button>();
    private readonly HashSet<Graphic> subTitlesDone = new HashSet<Graphic>();
    private readonly HashSet<Transform> subPanelsDone = new HashSet<Transform>();
    private readonly List<Graphic> subTitles = new List<Graphic>();
    private readonly HashSet<Button> toggles = new HashSet<Button>();
    private readonly HashSet<Graphic> pauseTitlesDone = new HashSet<Graphic>();

    private Button playButtonRef;
    private bool menuBackgroundApplied;
    private bool gameOverBackgroundApplied;

    private IEnumerator Start()
    {
        // The other scripts build their UI in their own Start(), so scan repeatedly.
        // For the first 1.5 seconds scan EVERY frame, so nothing flashes without art, then relax.
        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            Scan();

            if (Time.realtimeSinceStartup - startTime < 1.5f) yield return null;
            else yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private void Scan()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        List<Graphic> texts = new List<Graphic>();
        texts.AddRange(FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        texts.AddRange(FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        // 1. Find the PLAY button and the Settings / hero select titles
        if (playButtonRef == null)
        {
            foreach (Button b in buttons)
            {
                if (Normalize(LabelOf(b, out _)) == "PLAY") { playButtonRef = b; break; }
            }
        }

        foreach (Graphic g in texts)
        {
            string s = Normalize(ReadText(g));
            if (IsSubScreenTitle(s) && g.GetComponentInParent<Button>(true) == null && !subTitles.Contains(g))
            {
                subTitles.Add(g);
            }
        }

        // 2. Skin buttons and texts
        foreach (Button b in buttons)
        {
            if (done.Add(b)) SkinButton(b);
            else if (toggles.Contains(b)) RefreshToggle(b);
        }

        foreach (Graphic g in texts) ProcessText(g, ReadText(g));

        // 3. Backgrounds
        if (playButtonRef != null && !menuBackgroundApplied && menuBackground != null)
        {
            ApplyMenuBackground();
        }

        if (playButtonRef != null && subScreenBackground != null)
        {
            foreach (Graphic title in subTitles)
            {
                if (subTitlesDone.Add(title)) ApplySubScreenBackground(title);
            }
        }
    }

    // ---------------------------------------------------------------- buttons

    private void SkinButton(Button b)
    {
        string s = Normalize(LabelOf(b, out Graphic labelGraphic));
        Sprite sprite = SpriteForLabel(s, out bool isToggle);

        if (sprite == null) return;
        if (isToggle) toggles.Add(b);

        // Shift only the four main menu buttons (each button is only processed once)
        if (menuButtonsOffsetY != 0f && IsMainMenuLabel(s))
        {
            RectTransform brt = b.GetComponent<RectTransform>();
            if (brt != null) brt.anchoredPosition += new Vector2(0f, menuButtonsOffsetY);
        }

        Image img = b.targetGraphic as Image;
        if (img == null) img = b.GetComponent<Image>();
        if (img == null) return;

        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = Color.white;

        // The art already contains the word, so hide the text label
        if (labelGraphic != null) labelGraphic.enabled = false;
    }

    // MUSIC / SOUND buttons change their text (ON / OFF) when clicked, so their picture is updated every scan
    private void RefreshToggle(Button b)
    {
        string s = Normalize(LabelOf(b, out _));
        Sprite sprite = SpriteForLabel(s, out _);
        if (sprite == null) return;

        Image img = b.targetGraphic as Image;
        if (img == null) img = b.GetComponent<Image>();
        if (img == null) return;

        img.sprite = sprite;
        img.color = Color.white; // the menu script may tint the button green / red
    }

    private Sprite SpriteForLabel(string s, out bool isToggle)
    {
        isToggle = false;

        switch (s)
        {
            case "PLAY": return playButton;
            case "CHARACTERS":
            case "CHARACTER":
            case "HEROES": return charactersButton;
            case "SETTINGS": return settingsButton;
            case "QUIT": return quitButton;
            case "BACK": return backButton;
            case "<": return leftArrowButton;
            case ">": return rightArrowButton;
            case "RETRY":
            case "RESTART":
            case "TRY AGAIN":
            case "PLAY AGAIN": return retryButton;
            case "MAIN MENU":
            case "MENU": return mainMenuButton;
            case "RESUME":
            case "CONTINUE": return resumeButton;
        }

        if (s.StartsWith("MUSIC"))
        {
            isToggle = true;
            return s.EndsWith("OFF") ? musicOffButton : musicOnButton;
        }

        if (s.StartsWith("SOUND") || s.StartsWith("SFX"))
        {
            isToggle = true;
            return s.EndsWith("OFF") ? soundOffButton : soundOnButton;
        }

        return null;
    }

    private static bool IsMainMenuLabel(string s)
    {
        return s == "PLAY" || s == "CHARACTERS" || s == "CHARACTER" || s == "HEROES" ||
               s == "SETTINGS" || s == "QUIT";
    }

    private static string LabelOf(Button b, out Graphic labelGraphic)
    {
        labelGraphic = null;

        Text t = b.GetComponentInChildren<Text>(true);
        if (t != null)
        {
            labelGraphic = t;
            return t.text;
        }

        TMP_Text m = b.GetComponentInChildren<TMP_Text>(true);
        if (m != null)
        {
            labelGraphic = m;
            return m.text;
        }

        return "";
    }

    // ------------------------------------------------------------------ texts

    private void ProcessText(Graphic g, string raw)
    {
        string s = Normalize(raw);

        // The "HERO: Tung Tung Tung Sahur" line under the menu buttons
        if (hideHeroLabel && s.StartsWith("HERO:"))
        {
            g.enabled = false;
            return;
        }

        // Menu: the logo is part of the background art, so hide the text title
        if (s == "FALL" && menuBackground != null)
        {
            g.enabled = false;
            return;
        }

        if (s == "FINAL SCORE" && gameOverBackground != null && !gameOverBackgroundApplied)
        {
            ApplyGameOverBackground(g);
        }

        if (pauseBackground != null && (s == "PAUSED" || s == "PAUSE" || s == "GAME PAUSED") &&
            g.GetComponentInParent<Button>(true) == null && pauseTitlesDone.Add(g))
        {
            ApplyPauseBackground(g);
        }
    }

    private static string ReadText(Graphic g)
    {
        if (g is Text t) return t.text;
        if (g is TMP_Text m) return m.text;
        return "";
    }

    private bool IsSubScreenTitle(string s)
    {
        if (string.IsNullOrEmpty(s) || subScreenTitles == null) return false;
        foreach (string title in subScreenTitles)
        {
            if (Normalize(title) == s) return true;
        }
        return false;
    }

    // ------------------------------------------------------------ backgrounds

    private void ApplyMenuBackground()
    {
        // true = also find the canvas when a screen is hidden
        Canvas canvas = playButtonRef.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        // The biggest object that holds the PLAY button but none of the other screens' titles
        Transform panel = null;
        Transform t = playButtonRef.transform.parent;
        while (t != null && t != canvas.transform)
        {
            if (ContainsAnySubTitle(t)) break;
            panel = t;
            t = t.parent;
        }

        if (panel == null)
        {
            panel = TopLevelChild(playButtonRef.transform, canvas.transform);
            Debug.LogWarning("UISkinner: could not separate the main screen from the other screens, the logo background may show on all of them.");
        }
        if (panel == null || panel == playButtonRef.transform) panel = canvas.transform;

        Transform bg = CreateBackground(panel, menuBackground, "ArtMenuBackground");
        HideFlatBackdrops(panel, bg, canvas);

        menuBackgroundApplied = true;
        Debug.Log("UISkinner: menu background (with logo) applied to " + panel.name);
    }

    private void ApplySubScreenBackground(Graphic title)
    {
        Canvas canvas = title.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        // The biggest object that holds this title but not the PLAY button = this screen
        Transform panel = null;
        Transform t = title.transform;
        while (t != null && t != canvas.transform)
        {
            if (playButtonRef.transform.IsChildOf(t)) break;
            panel = t;
            t = t.parent;
        }

        if (panel == null || panel == title.transform)
        {
            Debug.LogWarning("UISkinner: could not find the screen that holds the title \"" + ReadText(title) + "\". Send me the main menu script.");
            return;
        }

        if (!subPanelsDone.Add(panel)) return;

        Transform bg = CreateBackground(panel, subScreenBackground, "ArtSubScreenBackground");
        HideFlatBackdrops(panel, bg, canvas);

        Debug.Log("UISkinner: sub-screen background (no logo) applied to " + panel.name);
    }

    private bool ContainsAnySubTitle(Transform t)
    {
        foreach (Graphic g in subTitles)
        {
            if (g != null && g.transform.IsChildOf(t)) return true;
        }
        return false;
    }

    private void ApplyGameOverBackground(Graphic finalScoreText)
    {
        // true = also find the canvas while the game over screen is still hidden
        Canvas canvas = finalScoreText.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        Transform top = TopLevelChild(finalScoreText.transform, canvas.transform);
        if (top == null) return;

        Transform bg = CreateBackground(top, gameOverBackground, "ArtGameOverBackground");
        HideFlatBackdrops(top, bg, canvas);

        // The art already says GAME OVER, so hide the text version inside this screen
        foreach (Graphic g in top.GetComponentsInChildren<Graphic>(true))
        {
            if (g is Text txt && Normalize(txt.text) == "GAME OVER") g.enabled = false;
            else if (g is TMP_Text tmp && Normalize(tmp.text) == "GAME OVER") g.enabled = false;
        }

        gameOverBackgroundApplied = true;
        Debug.Log("UISkinner: game over background applied");
    }

    private void ApplyPauseBackground(Graphic pauseTitle)
    {
        Canvas canvas = pauseTitle.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        Transform top = TopLevelChild(pauseTitle.transform, canvas.transform);
        if (top == null || top == pauseTitle.transform) return;

        Transform bg = CreateBackground(top, pauseBackground, "ArtPauseBackground");
        HideFlatBackdrops(top, bg, canvas);

        Debug.Log("UISkinner: pause background applied");
    }

    // Full-screen image that fills the screen without stretching (crops a little if the shape differs)
    private Transform CreateBackground(Transform parent, Sprite sprite, string objectName)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        obj.transform.SetParent(parent, false);
        obj.transform.SetAsFirstSibling();

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.preserveAspect = false;

        AspectRatioFitter fitter = obj.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

        return obj.transform;
    }

    // Switches off flat colored full-screen panels (navy fill, dark overlays) that would cover the art
    private void HideFlatBackdrops(Transform top, Transform keep, Canvas canvas)
    {
        Vector2 canvasSize = WorldSize((RectTransform)canvas.transform);

        foreach (Image img in top.GetComponentsInChildren<Image>(true))
        {
            if (img.transform == keep || img.transform.IsChildOf(keep)) continue;
            if (img.sprite != null) continue;                             // real art stays
            if (img.GetComponentInParent<Button>(true) != null) continue; // button faces stay

            RectTransform rt = img.rectTransform;

            bool stretched = rt.anchorMin.x <= 0.01f && rt.anchorMin.y <= 0.01f &&
                             rt.anchorMax.x >= 0.99f && rt.anchorMax.y >= 0.99f;

            Vector2 size = WorldSize(rt);
            bool coversScreen = size.x >= canvasSize.x * 0.95f && size.y >= canvasSize.y * 0.95f;

            if (stretched || coversScreen) img.enabled = false;
        }
    }

    private static Vector2 WorldSize(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        return new Vector2(Mathf.Abs(c[2].x - c[0].x), Mathf.Abs(c[2].y - c[0].y));
    }

    // The object directly under the canvas that contains t (the whole screen/panel it belongs to)
    private static Transform TopLevelChild(Transform t, Transform canvasTransform)
    {
        while (t != null && t.parent != canvasTransform) t = t.parent;
        return t;
    }

    private static string Normalize(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim().ToUpperInvariant();
    }
}
