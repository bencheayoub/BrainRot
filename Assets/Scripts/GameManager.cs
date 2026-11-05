using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private int maxHearts = 3;

    [Header("References")]
    [SerializeField] private FruitSpawner spawner;

    public int Score { get; private set; }
    public int Hearts { get; private set; }
    public int MaxHearts => maxHearts;
    public bool IsGameOver { get; private set; }

    // The HUD, pop-ups and sounds will listen to these (steps 9, 11, 14, 17)
    public event Action<int> ScoreChanged;
    public event Action<int> HeartsChanged;
    public event Action<Vector3, int> ScorePopup; // world position, points gained or lost
    public event Action<Fruit> FruitCaught;
    public event Action GameOver;

    private void Awake()
    {
        Instance = this;
        Hearts = maxHearts;
    }

    // Called by the player when it touches a fruit or an obstacle
    public void CatchFruit(Fruit fruit)
    {
        if (IsGameOver) return;

        if (fruit.points != 0)
        {
            AddScore(fruit.points);
            ScorePopup?.Invoke(fruit.transform.position, fruit.points);
        }

        if (fruit.heartDamage > 0)
        {
            LoseHearts(fruit.heartDamage);
        }

        FruitCaught?.Invoke(fruit);
    }

    private void AddScore(int amount)
    {
        Score = Mathf.Max(0, Score + amount); // score never goes below 0
        ScoreChanged?.Invoke(Score);
    }

    private void LoseHearts(int amount)
    {
        Hearts = Mathf.Max(0, Hearts - amount);
        HeartsChanged?.Invoke(Hearts);

        if (Hearts == 0)
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        IsGameOver = true;

        if (spawner != null)
        {
            spawner.IsSpawning = false;
        }

        GameOver?.Invoke();
    }

    // ---- TEMPORARY DEBUG DISPLAY (replaced by the real HUD in step 11) ----
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 36;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(20, 20, 700, 60), "Score: " + Score + "    Hearts: " + Hearts, style);

        if (IsGameOver)
        {
            GUI.Label(new Rect(20, 80, 700, 60), "GAME OVER", style);
        }
    }
}