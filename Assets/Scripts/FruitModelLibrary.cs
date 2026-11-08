using UnityEngine;

public enum SkinPickMode
{
    Random,   // a random model from the list each time
    InOrder   // apple, then banana, then the next one... and back to the start
}

// Lives in the Game scene (one object only).
// Every time a Fruit is created, Fruit.Start() asks this library for a 3D model
// that matches its type (Good, Bad, Golden or Obstacle) and puts it on the fruit.
// The fruit keeps its collider, score and movement. Only the look changes.
public class FruitModelLibrary : MonoBehaviour
{
    public static FruitModelLibrary Instance { get; private set; }

    [Header("Models for each type (drag prefabs from Assets/Prefabs/Unpacked)")]
    [SerializeField] private GameObject[] goodFruits;
    [SerializeField] private GameObject[] badFruits;
    [SerializeField] private GameObject[] goldenFruits;
    [SerializeField] private GameObject[] obstacles;

    [Header("How the model is chosen")]
    [SerializeField] private SkinPickMode pickMode = SkinPickMode.InOrder;

    [Header("Size of each type (1 = about 1 unit wide)")]
    [Tooltip("Change these to match the size of the collider on your fruit prefab")]
    [SerializeField] private float goodSize = 1f;
    [SerializeField] private float badSize = 1f;
    [SerializeField] private float goldenSize = 1f;
    [SerializeField] private float obstacleSize = 1f;

    [Header("Bad fruits look rotten (color tint)")]
    [SerializeField] private bool tintBadFruits = true;
    [SerializeField] private Color badTint = new Color(0.35f, 0.45f, 0.1f);
    [SerializeField, Range(0f, 1f)] private float badTintStrength = 0.6f;

    [Header("Golden fruits shine (color tint)")]
    [SerializeField] private bool tintGoldenFruits = true;
    [SerializeField] private Color goldenTint = new Color(1f, 0.82f, 0.1f);
    [SerializeField, Range(0f, 1f)] private float goldenTintStrength = 0.75f;

    // Next model to use for each type when the mode is InOrder
    private readonly int[] nextIndex = new int[4];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("FruitModelLibrary: there are two libraries in the scene. Delete one.");
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Called by Fruit.Start()
    public void ApplySkin(Fruit fruit)
    {
        GameObject[] list = ListFor(fruit.type);
        if (list == null || list.Length == 0) return; // nothing assigned: keep the old look

        GameObject prefab = Pick(fruit.type, list);
        if (prefab == null) return;

        // Remember the old visuals BEFORE adding the new model, then hide them.
        // (Particles and other effects on the fruit are left alone.)
        Renderer[] oldRenderers = fruit.GetComponentsInChildren<Renderer>();

        GameObject model = Instantiate(prefab, fruit.transform);
        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = prefab.transform.localRotation;
        model.transform.localScale = prefab.transform.localScale * SizeFor(fruit.type);

        // The Fruit already has the collider. Remove anything extra that came with the model.
        foreach (Collider c in model.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (Rigidbody r in model.GetComponentsInChildren<Rigidbody>()) Destroy(r);

        foreach (Renderer r in oldRenderers)
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer || r is SpriteRenderer)
            {
                r.enabled = false;
            }
        }

        if (fruit.type == FruitType.Bad && tintBadFruits)
        {
            Tint(model, badTint, badTintStrength);
        }
        else if (fruit.type == FruitType.Golden && tintGoldenFruits)
        {
            Tint(model, goldenTint, goldenTintStrength);
        }
    }

    private GameObject[] ListFor(FruitType type)
    {
        switch (type)
        {
            case FruitType.Good: return goodFruits;
            case FruitType.Bad: return badFruits;
            case FruitType.Golden: return goldenFruits;
            default: return obstacles;
        }
    }

    private float SizeFor(FruitType type)
    {
        switch (type)
        {
            case FruitType.Good: return goodSize;
            case FruitType.Bad: return badSize;
            case FruitType.Golden: return goldenSize;
            default: return obstacleSize;
        }
    }

    private GameObject Pick(FruitType type, GameObject[] list)
    {
        int start;
        if (pickMode == SkinPickMode.Random)
        {
            start = Random.Range(0, list.Length);
        }
        else
        {
            start = nextIndex[(int)type] % list.Length;
            nextIndex[(int)type] = (start + 1) % list.Length;
        }

        // Skip empty slots in the list
        for (int i = 0; i < list.Length; i++)
        {
            GameObject candidate = list[(start + i) % list.Length];
            if (candidate != null) return candidate;
        }
        return null;
    }

    // Mixes the model's colors with a tint, without creating new materials
    private static void Tint(GameObject model, Color tint, float strength)
    {
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;

                string prop = null;
                if (mats[i].HasProperty("_BaseColor")) prop = "_BaseColor";
                else if (mats[i].HasProperty("_Color")) prop = "_Color";
                if (prop == null) continue;

                Color mixed = Color.Lerp(mats[i].GetColor(prop), tint, strength);

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block, i);
                block.SetColor(prop, mixed);
                r.SetPropertyBlock(block, i);
            }
        }
    }
}
