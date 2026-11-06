using System;
using UnityEngine;

// The two audio switches (music and sound effects), saved on the device.
// Step 17 (sound) reads these: if (GameAudio.SfxOn) source.Play();
// Named GameAudio because UnityEngine.AudioSettings already exists.
public static class GameAudio
{
    private const string MusicKey = "FALL_MusicOn";
    private const string SfxKey = "FALL_SfxOn";

    // Fired whenever a switch changes, so music players can mute or unmute at once
    public static event Action Changed;

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static bool SfxOn
    {
        get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(SfxKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
