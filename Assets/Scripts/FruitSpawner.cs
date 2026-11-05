using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        [Tooltip("Higher = spawns more often (relative to the other entries)")]
        [Range(0f, 100f)] public float weight = 10f;
    }

    [Header("What to spawn")]
    [SerializeField] private SpawnEntry[] entries;

    [Header("Timing")]
    [Tooltip("Seconds between spawns")]
    [SerializeField] private float spawnInterval = 1.2f;

    [Header("Fall speed")]
    [SerializeField] private float minFallSpeed = 2.5f;
    [SerializeField] private float maxFallSpeed = 4.5f;

    [Header("Position")]
    [SerializeField] private float edgePadding = 0.5f;
    [SerializeField] private float spawnHeightAboveScreen = 1f;

    // The difficulty system (step 10) and game over (step 8) use these
    public bool IsSpawning { get; set; } = true;
    public float SpawnInterval { get => spawnInterval; set => spawnInterval = value; }
    public float SpeedMultiplier { get; set; } = 1f;

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
            timer = spawnInterval;
        }
    }

    private void SpawnOne()
    {
        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        // Random X inside the screen edges, Y just above the top of the screen
        float halfWidth = cam.orthographicSize * cam.aspect;
        float limit = halfWidth - edgePadding;
        float x = Random.Range(-limit, limit);
        float y = cam.orthographicSize + spawnHeightAboveScreen;

        GameObject obj = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, transform);

        Fruit fruit = obj.GetComponent<Fruit>();
        if (fruit != null)
        {
            fruit.fallSpeed = Random.Range(minFallSpeed, maxFallSpeed) * SpeedMultiplier;
        }
    }

    // Weighted random pick
    private GameObject PickPrefab()
    {
        if (entries == null || entries.Length == 0) return null;

        float total = 0f;
        foreach (SpawnEntry e in entries)
        {
            if (e.prefab != null) total += e.weight;
        }
        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        foreach (SpawnEntry e in entries)
        {
            if (e.prefab == null) continue;
            if (roll < e.weight) return e.prefab;
            roll -= e.weight;
        }
        return null;
    }
}