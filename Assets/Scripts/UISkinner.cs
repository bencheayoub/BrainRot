using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Applies the 2D art to UI that other scripts already build, WITHOUT editing those scripts.
// Put one UISkinner in the MainMenu scene (menu art) and one in the Game scene (game over art).
// It looks for UI by what it says:
//   buttons labelled PLAY / CHARACTERS / SETTINGS / QUIT  -> replaced by the button art
//   a PLAY button exists                                  -> menu background is added, the "FALL" title text is hidden
//   a text reading FINAL SCORE exists                     -> game over background is added, "GAME OVER" texts are hidden
// Works with both normal UI Text and TextMeshPro. Anything it can't match is left unchanged.
public class UISkinner : MonoBehaviour
{
    [Header("Main menu scene")]
    [SerializeField] private Sprite menuBackground;
    [SerializeField] private Sprite playButton;
    [SerializeField] private Sprite charactersButton;
    [SerializeField] private Sprite settingsButton;
    [SerializeField] private Sprite quitButton;

    [Header("Game scene")]
    [SerializeField] private Sprite gameOverBackground;

    private readonly HashSet<Button> done = new HashSet<Button>();
    private bool menuBackgroundApplied;
    private bool gameOverBackgroundApplied;

    private IEnumerator Start()
    {
        // The other scripts build their UI in their own Start(), so scan repeatedly
        while (true)
        {
            Scan();
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private void Scan()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            if (done.Add(b)) SkinButton(b);
        }

        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Text t in texts) ProcessText(t, t.text);

        TMP_Text[] tmps = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text t in tmps) ProcessText(t, t.text);
    }

    // ---------------------------------------------------------------- buttons

    private void SkinButton(Button b)
    {
        Graphic labelGraphic = null;
        string label = "";

        Text t = b.GetComponentInChildren<Text>(true);
        if (t != null)
        {
            labelGraphic = t;
            label = t.text;
        }
        else
        {
            TMP_Text m = b.GetComponentInChildren<TMP_Text>(true);
            if (m != null)
            {
                labelGraphic = m;
                label = m.text;
            }
        }

        string s = Normalize(label);
        Sprite sprite = null;

        switch (s)
        {
            case "PLAY": sprite = playButton; break;
            case "CHARACTERS":
            case "CHARACTER":
            case "HEROES": sprite = charactersButton; break;
            case "SETTINGS": sprite = settingsButton; break;
            case "QUIT": sprite = quitButton; break;
        }

        if (s == "PLAY") ApplyMenuBackground(b);

        if (sprite == null) return;

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

    // ------------------------------------------------------------------ texts

    private void ProcessText(Graphic g, string raw)
    {
        string s = Normalize(raw);

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
    }

    // ------------------------------------------------------------ backgrounds

    private void ApplyMenuBackground(Button anyMenuButton)
    {
        if (menuBackgroundApplied || menuBackground == null) return;

        Canvas canvas = anyMenuButton.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        CreateBackground(canvas.transform, menuBackground, "ArtMenuBackground");
        menuBackgroundApplied = true;
    }

    private void ApplyGameOverBackground(Graphic finalScoreText)
    {
        Canvas canvas = finalScoreText.canvas;
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        // The top-level object of the game over screen (the one that gets shown and hidden)
        Transform top = finalScoreText.transform;
        while (top.parent != null && top.parent != canvas.transform) top = top.parent;
        if (top.parent == null) return;

        // Remove the dark overlay so the art isn't dimmed
        Image overlay = top.GetComponent<Image>();
        if (overlay != null) overlay.enabled = false;

        CreateBackground(top, gameOverBackground, "ArtGameOverBackground");

        // The art already says GAME OVER, so hide the text version inside this screen
        foreach (Graphic g in top.GetComponentsInChildren<Graphic>(true))
        {
            if (g is Text txt && Normalize(txt.text) == "GAME OVER") g.enabled = false;
            else if (g is TMP_Text tmp && Normalize(tmp.text) == "GAME OVER") g.enabled = false;
        }

        gameOverBackgroundApplied = true;
    }

    // Full-screen image that fills the screen without stretching (crops a little if the shape differs)
    private void CreateBackground(Transform parent, Sprite sprite, string objectName)
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
    }

    private static string Normalize(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim().ToUpperInvariant();
    }
}
