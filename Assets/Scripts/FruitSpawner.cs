using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// Spawns everything that rises up the pit.
// Same fields as before (your Inspector values are kept), plus patterns:
//   - OBSTACLE WALL: a row of obstacles across the pit with one gap (a good or golden fruit waits in the gap)
//   - FRUIT LINE: a wavy line of good fruits, sometimes with a bad fruit hidden in it
// Patterns start once the difficulty passes "Pattern Start Difficulty" and get more frequent and tighter over time.
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
    [SerializeField] private float minRiseSpeed = 1.5f;
    [SerializeField] private float maxRiseSpeed = 3.2f;
    [Tooltip("Rise speed is multiplied by this at max difficulty")]
    [SerializeField] private float hardSpeedMultiplier = 1.8f;

    [Header("Random doubles")]
    [Tooltip("Chance of spawning two objects at once, at max difficulty")]
    [Range(0f, 1f)] [SerializeField] private float hardDoubleSpawnChance = 0.35f;

    [Header("Patterns (walls with a gap, fruit lines)")]
    [SerializeField] private bool usePatterns = true;
    [Tooltip("Difficulty (0 to 1) at which patterns begin")]
    [Range(0f, 1f)] [SerializeField] private float patternStartDifficulty = 0.1f;
    [Tooltip("Seconds between patterns at the start")]
    [SerializeField] private float patternEasyCooldown = 10f;
    [Tooltip("Seconds between patterns at max difficulty")]
    [SerializeField] private float patternHardCooldown = 4f;
    [Tooltip("Width of the gap in a wall, easy (bigger = easier)")]
    [SerializeField] private float gapWidthEasy = 3.2f;
    [Tooltip("Width of the gap in a wall, at max difficulty")]
    [SerializeField] private float gapWidthHard = 2.4f;
    [Tooltip("Distance between obstacles in a wall")]
    [SerializeField] private float wallSpacing = 0.9f;
    [Tooltip("How far the gap can jump between two walls in a row")]
    [SerializeField] private float maxGapShift = 3f;
    [Tooltip("Most walls in a row, at max difficulty")]
    [SerializeField] private int hardMaxWalls = 3;
    [Tooltip("Chance that the reward in a gap is golden")]
    [Range(0f, 1f)] [SerializeField] private float goldenInGapChance = 0.25f;

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
    private float patternCooldown;
    private float busyTimer;   // random spawns pause while a pattern is playing
    private float lastGapX;

    private float Limit => cam.orthographicSize * cam.aspect - edgePadding;
    private float SpawnY => -(cam.orthographicSize + spawnHeightBelowScreen);

    private void Start()
    {
        cam = Camera.main;
        timer = 0.5f;
        patternCooldown = patternEasyCooldown;
    }

    private void Update()
    {
        if (!IsSpawning) return;

        float dt = Time.deltaTime;

        if (busyTimer > 0f) busyTimer -= dt;

        if (usePatterns && difficulty >= patternStartDifficulty)
        {
            patternCooldown -= dt;
            if (patternCooldown <= 0f && busyTimer <= 0f)
            {
                StartPattern();
                patternCooldown = Mathf.Lerp(patternEasyCooldown, patternHardCooldown, difficulty)
                                  * Random.Range(0.8f, 1.2f);
            }
        }

        // No random spawns while a pattern is playing, so a wall's gap stays fair
        if (busyTimer > 0f) return;

        timer -= dt;
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

    // ----------------------------------------------------------- random spawns

    private void SpawnOne()
    {
        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        float x = Random.Range(-Limit, Limit);
        Spawn(prefab, x, SpawnY, RollSpeed());
    }

    private float RollSpeed()
    {
        return Random.Range(minRiseSpeed, maxRiseSpeed) * Mathf.Lerp(1f, hardSpeedMultiplier, difficulty);
    }

    private Fruit Spawn(GameObject prefab, float x, float y, float speed)
    {
        if (prefab == null) return null;

        GameObject obj = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, transform);
        Fruit fruit = obj.GetComponent<Fruit>();
        if (fruit != null) fruit.riseSpeed = speed;
        return fruit;
    }

    // --------------------------------------------------------------- patterns

    private void StartPattern()
    {
        GameObject obstacle = PrefabOfType(FruitType.Obstacle);
        GameObject good = PrefabOfType(FruitType.Good);

        // Walls become more likely as the game gets harder
        float wallWeight = obstacle != null ? 50f + 30f * difficulty : 0f;
        float lineWeight = good != null ? 40f - 15f * difficulty : 0f;
        float total = wallWeight + lineWeight;
        if (total <= 0f) return;

        if (Random.Range(0f, total) < wallWeight) StartCoroutine(WallSequence(obstacle));
        else StartCoroutine(FruitLine());
    }

    // One to several walls in a row, each with a gap that can jump sideways
    private IEnumerator WallSequence(GameObject obstacle)
    {
        int maxWalls = 1 + Mathf.RoundToInt((hardMaxWalls - 1) * difficulty);
        int count = Random.Range(1, maxWalls + 1);

        float speed = RollSpeed();
        float delay = 4f / speed; // about 4 units of space between walls

        busyTimer = count * delay + 1f;

        for (int i = 0; i < count; i++)
        {
            if (!IsSpawning) yield break;

            SpawnWall(obstacle, speed);
            if (i < count - 1) yield return new WaitForSeconds(delay);
        }
    }

    private void SpawnWall(GameObject obstacle, float speed)
    {
        float limit = Limit;
        float gapWidth = Mathf.Lerp(gapWidthEasy, gapWidthHard, difficulty);
        float half = gapWidth * 0.5f;

        float minX = -limit + half;
        float maxX = limit - half;
        if (minX > maxX) minX = maxX = 0f;

        float gapX = Mathf.Clamp(lastGapX + Random.Range(-maxGapShift, maxGapShift), minX, maxX);
        lastGapX = gapX;

        float y = SpawnY;

        for (float x = -limit; x <= limit + 0.01f; x += wallSpacing)
        {
            if (Mathf.Abs(x - gapX) < half) continue;
            Spawn(obstacle, x, y, speed);
        }

        // Reward waiting in the gap
        GameObject good = PrefabOfType(FruitType.Good);
        GameObject golden = PrefabOfType(FruitType.Golden);
        GameObject reward = (golden != null && Random.value < goldenInGapChance) ? golden : good;
        Spawn(reward, gapX, y, speed);
    }

    // A wavy line of good fruits, sometimes with a decoy bad fruit inside
    private IEnumerator FruitLine()
    {
        GameObject good = PrefabOfType(FruitType.Good);
        GameObject bad = PrefabOfType(FruitType.Bad);
        GameObject golden = PrefabOfType(FruitType.Golden);
        if (good == null) yield break;

        const int count = 5;
        float speed = RollSpeed();
        float step = 0.9f / speed;

        busyTimer = count * step + 1f;

        float limit = Limit;
        float center = Random.Range(-limit * 0.6f, limit * 0.6f);
        float amplitude = Mathf.Min(1.4f, limit * 0.5f);
        int decoy = (bad != null && Random.value < difficulty * 0.7f) ? Random.Range(1, count - 1) : -1;

        for (int i = 0; i < count; i++)
        {
            if (!IsSpawning) yield break;

            float x = Mathf.Clamp(center + Mathf.Sin(i * 1.1f) * amplitude, -limit, limit);

            GameObject prefab = good;
            if (i == decoy) prefab = bad;
            else if (i == count - 1 && golden != null && Random.value < 0.3f) prefab = golden;

            Spawn(prefab, x, SpawnY, speed);
            yield return new WaitForSeconds(step);
        }
    }

    // ---------------------------------------------------------------- helpers

    // Finds a prefab by what kind of object it is (looks at its Fruit component)
    private GameObject PrefabOfType(FruitType type)
    {
        if (entries == null) return null;

        foreach (SpawnEntry e in entries)
        {
            if (e.prefab == null) continue;
            Fruit f = e.prefab.GetComponent<Fruit>();
            if (f != null && f.type == type) return e.prefab;
        }
        return null;
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
