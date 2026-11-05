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

    [Header("Rise speed")]
    [SerializeField] private float minRiseSpeed = 2.5f;
    [SerializeField] private float maxRiseSpeed = 4.5f;
    [Tooltip("Rise speed is multiplied by this at max difficulty")]
    [SerializeField] private float hardSpeedMultiplier = 1.8f;

    [Header("Patterns")]
    [Tooltip("Chance of spawning two objects at once, at max difficulty")]
    [Range(0f, 1f)] [SerializeField] private float hardDoubleSpawnChance = 0.35f;

    [Header("Position")]
    [SerializeField] private float edgePadding = 0.5f;
    [SerializeField] private float spawnHeightBelowScreen = 1f;

    // 0 = easy, 1 = hard. Set by DifficultySystem.
    private float difficulty;
    public float Difficulty
    {
        get => difficulty;
        set => difficulty = Mathf.Clamp01(value);
    }

    // Game over switches this off
    public bool IsSpawning { get; set; } = true;

    private Camera cam;
    private float timer;

    private void Start()
    {
        cam = Camera.main;
        timer = 0.5f;
    }

    private void Update()
    {
        if (!IsSpawning) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnOne();

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

        // Random X inside the pit, Y just BELOW the bottom of the screen
        float halfWidth = cam.orthographicSize * cam.aspect;
        float limit = halfWidth - edgePadding;
        float x = Random.Range(-limit, limit);
        float y = -(cam.orthographicSize + spawnHeightBelowScreen);

        GameObject obj = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, transform);

        Fruit fruit = obj.GetComponent<Fruit>();
        if (fruit != null)
        {
            float speedMultiplier = Mathf.Lerp(1f, hardSpeedMultiplier, difficulty);
            fruit.riseSpeed = Random.Range(minRiseSpeed, maxRiseSpeed) * speedMultiplier;
        }
    }

    private float GetWeight(SpawnEntry e)
    {
        return Mathf.Lerp(e.weightEasy, e.weightHard, difficulty);
    }

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