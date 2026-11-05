using UnityEngine;
using UnityEngine.Serialization;

public class FruitSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        [Tooltip("Spawn weight at the start of the game (easy)")]
        [FormerlySerializedAs("weight")]
        [Range(0f, 100f)] public float weightEasy = 10f;
        [Tooltip("Spawn weight at max difficulty (hard)")]
        [Range(0f, 100f)] public float weightHard = 10f;
    }

    [Header("What to spawn")]
    [SerializeField] private SpawnEntry[] entries;

    [Header("Timing (seconds between spawns)")]
    [SerializeField] private float easyInterval = 1.3f;
    [SerializeField] private float hardInterval = 0.5f;

    [Header("Fall speed")]
    [SerializeField] private float minFallSpeed = 2.5f;
    [SerializeField] private float maxFallSpeed = 4.5f;
    [Tooltip("Fall speed is multiplied by this at max difficulty")]
    [SerializeField] private float hardSpeedMultiplier = 1.8f;

    [Header("Patterns")]
    [Tooltip("Chance of spawning two fruits at once, at max difficulty")]
    [Range(0f, 1f)] [SerializeField] private float hardDoubleSpawnChance = 0.35f;

    [Header("Position")]
    [SerializeField] private float edgePadding = 0.5f;
    [SerializeField] private float spawnHeightAboveScreen = 1f;

    // 0 = easy, 1 = hard. Set by DifficultySystem.
    private float difficulty;
    public float Difficulty
    {
        get => difficulty;
        set => difficulty = Mathf.Clamp01(value);
    }

    // Game over (step 8) switches this off
    public bool IsSpawning { get; set; } = true;

    private Camera cam;
    private float timer;

    private void Start()
    {
        cam = Camera.main;
        timer = 0.5f; // first fruit comes quickly
    }

    private void Update()
    {
        if (!IsSpawning) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnOne();

            // Pattern: sometimes drop a second fruit at the same time
            if (Random.value < Mathf.Lerp(0f, hardDoubleSpawnChance, difficulty))
            {
                SpawnOne();
            }

            float interval = Mathf.Lerp(easyInterval, hardInterval, difficulty);
            timer = interval * Random.Range(0.85f, 1.15f);
        }
    }

    private void SpawnOne()
    {
        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        float halfWidth = cam.orthographicSize * cam.aspect;
        float limit = halfWidth - edgePadding;
        float x = Random.Range(-limit, limit);
        float y = cam.orthographicSize + spawnHeightAboveScreen;

        GameObject obj = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, transform);

        Fruit fruit = obj.GetComponent<Fruit>();
        if (fruit != null)
        {
            float speedMultiplier = Mathf.Lerp(1f, hardSpeedMultiplier, difficulty);
            fruit.fallSpeed = Random.Range(minFallSpeed, maxFallSpeed) * speedMultiplier;
        }
    }

    private float GetWeight(SpawnEntry e)
    {
        return Mathf.Lerp(e.weightEasy, e.weightHard, difficulty);
    }

    // Weighted random pick, using the weights for the current difficulty
    private GameObject PickPrefab()
    {
        if (entries == null || entries.Length == 0) return null;

        float total = 0f;
        foreach (SpawnEntry e in entries)
        {
            if (e.prefab != null) total += GetWeight(e);
        }
        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        foreach (SpawnEntry e in entries)
        {
            if (e.prefab == null) continue;
            float w = GetWeight(e);
            if (roll < w) return e.prefab;
            roll -= w;
        }
        return null;
    }
}