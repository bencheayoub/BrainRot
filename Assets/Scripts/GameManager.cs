using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string HighScoreKey = "FALL_HighScore";

    [Header("Rules")]
    [SerializeField] private int maxHearts = 3;

    [Header("References")]
    [SerializeField] private FruitSpawner spawner;

    public int Score { get; private set; }
    public int Hearts { get; private set; }
    public int MaxHearts => maxHearts;
    public bool IsGameOver { get; private set; }

    // Best score ever saved on this device, and whether this run just beat it
    public int HighScore { get; private set; }
    public bool IsNewHighScore { get; private set; }

    // The HUD, pop-ups and sounds listen to these
    public event Action<int> ScoreChanged;
    public event Action<int> HeartsChanged;
    public event Action<Vector3, int> ScorePopup; // world position, points gained or lost
    public event Action<Fruit> FruitCaught;
    public event Action GameOver;

    private void Awake()
    {
        Instance = this;
        Hearts = maxHearts;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
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

        // Save the high score before anyone listens to GameOver
        if (Score > HighScore)
        {
            HighScore = Score;
            IsNewHighScore = true;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        GameOver?.Invoke();
    }
}
