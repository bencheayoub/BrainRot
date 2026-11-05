using UnityEngine;

public class DifficultySystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FruitSpawner spawner;
    [SerializeField] private ScrollingBackground background;

    [Header("Ramp")]
    [Tooltip("Seconds until the game reaches maximum difficulty")]
    [SerializeField] private float secondsToMaxDifficulty = 90f;
    [Tooltip("Shape of the ramp. Straight line = steady, curved = slow start, fast middle")]
    [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Background speed")]
    [SerializeField] private float easyScrollSpeed = 0.3f;
    [SerializeField] private float hardScrollSpeed = 0.8f;

    private float elapsed;

    // 0 = easy, 1 = hard. Other scripts can read this later.
    public float Difficulty { get; private set; }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / secondsToMaxDifficulty);
        Difficulty = difficultyCurve.Evaluate(t);

        if (spawner != null)
        {
            spawner.Difficulty = Difficulty;
        }

        if (background != null)
        {
            background.ScrollSpeed = Mathf.Lerp(easyScrollSpeed, hardScrollSpeed, Difficulty);
        }
    }

    // Called on restart (step 13)
    public void ResetDifficulty()
    {
        elapsed = 0f;
    }
}