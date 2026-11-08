using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Plays the music and the sound effects. Every slot is optional: an empty slot stays silent.
// Put this on an empty GameObject in BOTH the MainMenu scene and the Game scene.
// Respects the MUSIC and SOUND switches (GameAudio).
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Music (loops)")]
    [SerializeField] private AudioClip music;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;

    [Header("Sound effects")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip goodCatch;
    [SerializeField] private AudioClip badCatch;
    [SerializeField] private AudioClip goldenCatch;
    [SerializeField] private AudioClip obstacleHit;
    [SerializeField] private AudioClip gameOver;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("Buttons")]
    [Tooltip("Turn ON if some buttons make no click sound. Leave OFF if your menus already call SoundManager.PlayClick(), or you will hear it twice.")]
    [SerializeField] private bool autoClickAllButtons = false;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private GameManager gm;

    private readonly HashSet<Button> hookedButtons = new HashSet<Button>();
    private float scanTimer;

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
        sfxSource.ignoreListenerPause = true; // button clicks are heard while the game is paused
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
        if (!autoClickAllButtons) return;

        // Buttons can be created or shown later, so check a couple of times per second
        scanTimer -= Time.unscaledDeltaTime;
        if (scanTimer > 0f) return;
        scanTimer = 0.5f;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            if (hookedButtons.Add(b))
            {
                b.onClick.AddListener(PlayClick);
            }
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

    // ---------------------------------------------------------------- playing

    // Called by every button in the menus
    public static void PlayClick()
    {
        if (Instance != null) Instance.Play(Instance.buttonClick);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null || !GameAudio.SfxOn) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void OnFruitCaught(Fruit fruit)
    {
        switch (fruit.type)
        {
            case FruitType.Good: Play(goodCatch); break;
            case FruitType.Bad: Play(badCatch); break;
            case FruitType.Golden: Play(goldenCatch); break;
            case FruitType.Obstacle: Play(obstacleHit); break;
        }
    }

    private void OnGameOver()
    {
        musicSource.Stop();
        Play(gameOver);
    }
}
