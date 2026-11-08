using UnityEngine;

public enum FruitType
{
    Good,
    Bad,
    Golden,
    Obstacle
}

public class Fruit : MonoBehaviour
{
    [Header("Gameplay data")]
    public FruitType type = FruitType.Good;
    [Tooltip("Score change when caught (can be negative)")]
    public int points = 10;
    [Tooltip("Hearts removed when caught")]
    public int heartDamage = 0;

    [Header("Motion")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Sway (side to side)")]
    [Tooltip("How far it drifts left and right, in units")]
    [SerializeField] private float swayAmount = 0.5f;
    [Tooltip("How fast it sways")]
    [SerializeField] private float swaySpeed = 1.5f;
    [Tooltip("Obstacles sway this much less, so they stay fair to dodge")]
    [SerializeField] private float obstacleSwayFactor = 0.4f;

    // The spawner sets this when it creates the object
    public float riseSpeed = 3f;

    private Vector3 spinAxis;
    private float spinSpeed;
    private Camera cam;

    private float startX;
    private float phase;
    private float freq;
    private float amplitude;
    private float xLimit;

    private void Start()
    {
        // Give this object its 3D model (apple, banana, bomb...) from the
        // FruitModelLibrary in the scene. If there is no library, the old look stays.
        if (FruitModelLibrary.Instance != null)
        {
            FruitModelLibrary.Instance.ApplySkin(this);
        }

        cam = Camera.main;

        // Each object spins around its own random axis, at a slightly random speed
        spinAxis = Random.onUnitSphere;
        spinSpeed = rotationSpeed * Random.Range(0.6f, 1.4f);

        // Each object gets its own sway
        startX = transform.position.x;
        phase = Random.Range(0f, Mathf.PI * 2f);
        freq = swaySpeed * Random.Range(0.7f, 1.3f);
        amplitude = swayAmount * Random.Range(0.5f, 1.2f);
        if (type == FruitType.Obstacle) amplitude *= obstacleSwayFactor;

        xLimit = cam.orthographicSize * cam.aspect - 0.4f;
    }

    private void Update()
    {
        Vector3 pos = transform.position;

        pos.y += riseSpeed * Time.deltaTime;

        float x = startX + Mathf.Sin(Time.time * freq + phase) * amplitude;
        pos.x = Mathf.Clamp(x, -xLimit, xLimit);

        transform.position = pos;

        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);

        // Remove once it is above the top of the screen
        float topLimit = cam.orthographicSize + 1f;
        if (pos.y > topLimit)
        {
            Destroy(gameObject);
        }
    }
}
