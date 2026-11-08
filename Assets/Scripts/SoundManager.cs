using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Plays the music and the sound effects. Every slot is optional: an empty slot stays silent.
// Put this on an empty GameObject in BOTH the MainMenu scene and the Game scene.
// Respects the MUSIC and SOUND switches (GameAudio).
//
// Every sound slot is a LIST. With one clip it always plays that clip.
// With several clips it plays a different random one each time (never the same one twice in a row).
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Music (loops)")]
    [SerializeField] private AudioClip music;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;

    [Header("Button sounds")]
    [Tooltip("Normal button press")]
    [SerializeField] private AudioClip[] buttonClicks;
    [Tooltip("Mouse moves over a button")]
    [SerializeField] private AudioClip[] hoverSounds;
    [Tooltip("Play / Start button")]
    [SerializeField] private AudioClip[] startSounds;
    [Tooltip("Quit / Exit button")]
    [SerializeField] private AudioClip[] exitSounds;
    [Tooltip("Pause button")]
    [SerializeField] private AudioClip[] pauseSounds;

    [Header("Gameplay sounds")]
    [SerializeField] private AudioClip[] goodCatchSounds;
    [SerializeField] private AudioClip[] badCatchSounds;
    [SerializeField] private AudioClip[] goldenCatchSounds;
    [SerializeField] private AudioClip[] obstacleHitSounds;
    [SerializeField] private AudioClip[] gameOverSounds;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Tooltip("Hover sounds are quieter than clicks")]
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.5f;

    [Header("Automatic button sounds")]
    [Tooltip("Every button plays its sound by itself. It is safe if your menus already call SoundManager.PlayClick(): the sound is never played twice for one press.")]
    [SerializeField] private bool autoClickAllButtons = true;
    [Tooltip("Every button plays the hover sound when the mouse moves over it")]
    [SerializeField] private bool hoverOnAllButtons = true;

    [Header("Which button gets which sound")]
    [Tooltip("A button whose name or text contains one of these words plays the START sound")]
    [SerializeField] private string[] startWords = { "play", "start" };
    [Tooltip("... the EXIT sound")]
    [SerializeField] private string[] exitWords = { "quit", "exit" };
    [Tooltip("... the PAUSE sound")]
    [SerializeField] private string[] pauseWords = { "pause" };

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private GameManager gm;

    private readonly HashSet<Button> hookedButtons = new HashSet<Button>();
    private readonly Dictionary<AudioClip[], int> lastIndex = new Dictionary<AudioClip[], int>();
    private float scanTimer;
    private int lastPressFrame = -1;

    private void Awake()
    {
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.clip = music;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.ignoreListenerPause = true; // sounds are heard while the game is paused
    }

    private void Start()
    {
        gm = GameManager.Instance; // null in the main menu, that is fine
        if (gm != null)
        {
            gm.FruitCaught += OnFruitCaught;
            gm.GameOver += OnGameOver;
        }

        GameAudio.Changed += ApplySettings;
        ApplySettings();

        if (music != null) musicSource.Play();
    }

    private void Update()
    {
        if (!autoClickAllButtons && !hoverOnAllButtons) return;

        // Buttons can be created or shown later, so check a couple of times per second
        scanTimer -= Time.unscaledDeltaTime;
        if (scanTimer > 0f) return;
        scanTimer = 0.5f;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            if (!hookedButtons.Add(b)) continue;

            if (autoClickAllButtons) b.onClick.AddListener(PlayClick);
            if (hoverOnAllButtons) AddHover(b);
        }
    }

    private void OnDestroy()
    {
        if (gm != null)
        {
            gm.FruitCaught -= OnFruitCaught;
            gm.GameOver -= OnGameOver;
        }

        GameAudio.Changed -= ApplySettings;
        if (Instance == this) Instance = null;
    }

    // Music is muted (not stopped), so switching it back ON continues from the same spot
    private void ApplySettings()
    {
        musicSource.mute = !GameAudio.MusicOn;
    }

    // ---------------------------------------------------------------- buttons

    // Called by every button press. It looks at which button was pressed and plays
    // the START, EXIT or PAUSE sound for it, or the normal click sound for any other button.
    public static void PlayClick()
    {
        if (Instance != null) Instance.HandleButtonPress();
    }

    // Called when the mouse moves over a button
    public static void PlayHover()
    {
        if (Instance == null) return;
        Instance.Play(Instance.Pick(Instance.hoverSounds), Instance.hoverVolume);
    }

    private void HandleButtonPress()
    {
        // Several scripts can react to the same press: only play one sound per frame
        if (Time.frameCount == lastPressFrame) return;
        lastPressFrame = Time.frameCount;

        AudioClip[] list = buttonClicks;

        GameObject pressed = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (pressed != null)
        {
            string label = LabelOf(pressed);
            if (ContainsAny(label, pauseWords) && HasClip(pauseSounds)) list = pauseSounds;
            else if (ContainsAny(label, exitWords) && HasClip(exitSounds)) list = exitSounds;
            else if (ContainsAny(label, startWords) && HasClip(startSounds)) list = startSounds;
        }

        // Played by a separate object, so it is not cut off when the button changes the scene
        PlayDetached(Pick(list), sfxVolume);
    }

    private void AddHover(Button b)
    {
        EventTrigger trigger = b.GetComponent<EventTrigger>();
        if (trigger == null) trigger = b.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) =>
        {
            if (b != null && b.interactable) PlayHover();
        });
        trigger.triggers.Add(entry);
    }

    private static string LabelOf(GameObject go)
    {
        string label = go.name;

        TMP_Text tmp = go.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) label += " " + tmp.text;

        Text legacy = go.GetComponentInChildren<Text>(true);
        if (legacy != null) label += " " + legacy.text;

        return label.ToLowerInvariant();
    }

    private static bool ContainsAny(string label, string[] words)
    {
        if (words == null) return false;
        foreach (string w in words)
        {
            if (string.IsNullOrEmpty(w)) continue;
            if (label.Contains(w.ToLowerInvariant())) return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- playing

    private void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || !GameAudio.SfxOn) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    // Plays a clip on its own temporary object that survives a scene change
    private void PlayDetached(AudioClip clip, float volume)
    {
        if (clip == null || !GameAudio.SfxOn) return;

        GameObject go = new GameObject("UI Sound");
        DontDestroyOnLoad(go);

        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.ignoreListenerPause = true;
        src.spatialBlend = 0f;
        src.clip = clip;
        src.volume = volume;
        src.Play();

        Destroy(go, clip.length + 0.3f);
    }

    // Picks a random clip from the list, never the same one twice in a row
    private AudioClip Pick(AudioClip[] list)
    {
        if (list == null || list.Length == 0) return null;

        int valid = 0;
        foreach (AudioClip c in list) if (c != null) valid++;
        if (valid == 0) return null;

        int last;
        if (!lastIndex.TryGetValue(list, out last)) last = -1;

        int index;
        int guard = 0;
        do
        {
            index = Random.Range(0, list.Length);
            guard++;
        }
        while ((list[index] == null || (index == last && valid > 1)) && guard < 100);

        if (list[index] == null) return null;

        lastIndex[list] = index;
        return list[index];
    }

    private static bool HasClip(AudioClip[] list)
    {
        if (list == null) return false;
        foreach (AudioClip c in list) if (c != null) return true;
        return false;
    }

    // ---------------------------------------------------------------- game events

    private void OnFruitCaught(Fruit fruit)
    {
        switch (fruit.type)
        {
            case FruitType.Good: Play(Pick(goodCatchSounds)); break;
            case FruitType.Bad: Play(Pick(badCatchSounds)); break;
            case FruitType.Golden: Play(Pick(goldenCatchSounds)); break;
            case FruitType.Obstacle: Play(Pick(obstacleHitSounds)); break;
        }
    }

    private void OnGameOver()
    {
        musicSource.Stop();
        Play(Pick(gameOverSounds));
    }
}
