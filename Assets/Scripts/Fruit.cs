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

    // The spawner sets this when it creates the fruit (step 7)
    public float fallSpeed = 3f;

    private Vector3 spinAxis;
    private float spinSpeed;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        // Each fruit spins around its own random axis, at a slightly random speed
        spinAxis = Random.onUnitSphere;
        spinSpeed = rotationSpeed * Random.Range(0.6f, 1.4f);
    }

    private void Update()
    {
        // Fall straight down
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // Rotate while falling
        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);

        // Remove once it is below the screen
        float bottomLimit = -(cam.orthographicSize + 1f);
        if (transform.position.y < bottomLimit)
        {
            Destroy(gameObject);
        }
    }
}