using UnityEngine;

// Remembers which character the player picked.
// 0 = Tung Tung Tung Sahur, 1 = Potato with Glasses,
// 2 = Potato with Moustache, 3 = Ballerina Cappuccina.
// Nothing to attach: other scripts just read CharacterSelection.Index.
public static class CharacterSelection
{
    private const string Key = "SelectedCharacter";

    public static readonly string[] Names =
    {
        "Tung Tung Tung Sahur",
        "Potato with Glasses",
        "Potato with Moustache",
        "Ballerina Cappuccina"
    };

    public static int Index
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Names.Length - 1);
        set
        {
            PlayerPrefs.SetInt(Key, Mathf.Clamp(value, 0, Names.Length - 1));
            PlayerPrefs.Save();
        }
    }

    public static string Name => Names[Index];
}
