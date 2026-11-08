using UnityEngine;

// Put this on each fruit / obstacle PREFAB (Fruit_Good, Fruit_Bad, Fruit_Golden, Obstacle_Spike).
// It swaps the placeholder sphere or cube for a 3D model.
// While the Model slot is empty, the placeholder stays, so nothing breaks.
public class FruitVisual : MonoBehaviour
{
    [Header("Model")]
    [Tooltip("Drag the 3D model (.fbx or prefab) here. Empty = keep the placeholder shape.")]
    [SerializeField] private GameObject model;

    [Tooltip("Drag the model's texture (PNG) here. Empty = keep the model's own material.")]
    [SerializeField] private Texture2D texture;

    [Tooltip("Color multiplier. White = no change. Use gold (1, 0.85, 0.2) to make a golden version of a normal fruit.")]
    [SerializeField] private Color tint = Color.white;

    [Header("Size and rotation")]
    [Tooltip("How tall it is in the game, in world units. The model is resized to this automatically.")]
    [SerializeField] private float targetHeight = 0.7f;

    [Tooltip("Extra size multiplier on top of the automatic resize")]
    [SerializeField] private float scale = 1f;

    [Tooltip("Rotation in degrees. Fruits spin while rising, so this only changes where they start.")]
    [SerializeField] private Vector3 rotation = Vector3.zero;

    [Header("Hitbox")]
    [Tooltip("Resize the catch area to match the model")]
    [SerializeField] private bool fitHitbox = true;

    [Tooltip("Smaller = easier to avoid / harder to catch. 0.8 feels fair.")]
    [Range(0.3f, 1.2f)] [SerializeField] private float hitboxScale = 0.8f;

    private void Awake()
    {
        if (model == null) return;

        // Hide the placeholder shape
        MeshRenderer own = GetComponent<MeshRenderer>();
        if (own != null) own.enabled = false;

        GameObject visual = Instantiate(model, transform);
        visual.name = "Visual_" + model.name;

        // Only the prefab's own trigger collider should take part in catching
        foreach (Collider c in visual.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);

        // Models exported from Blender often carry their own camera and light
        foreach (Camera cam in visual.GetComponentsInChildren<Camera>(true))
        {
            cam.enabled = false;
            if (cam.gameObject != visual) Destroy(cam.gameObject); else Destroy(cam);
        }
        foreach (Light l in visual.GetComponentsInChildren<Light>(true))
        {
            l.enabled = false;
            if (l.gameObject != visual) Destroy(l.gameObject); else Destroy(l);
        }
        foreach (AudioListener al in visual.GetComponentsInChildren<AudioListener>(true)) Destroy(al);

        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = visual.transform.localRotation * Quaternion.Euler(rotation);

        if (texture != null || tint != Color.white) ApplyMaterial(visual);

        if (targetHeight > 0f && TryGetBounds(visual, out Bounds fit) && fit.size.y > 0.0001f)
        {
            visual.transform.localScale = visual.transform.localScale * (targetHeight / fit.size.y);
        }
        visual.transform.localScale = visual.transform.localScale * scale;

        // Middle of the model on the middle of the prefab, so it spins around its center
        if (TryGetBounds(visual, out Bounds center))
        {
            visual.transform.position += transform.position - center.center;
        }

        if (fitHitbox) FitHitbox(visual);
    }

    private void FitHitbox(GameObject visual)
    {
        if (!TryGetBounds(visual, out Bounds b)) return;

        Vector3 s = transform.lossyScale;
        if (s.x < 0.0001f || s.y < 0.0001f || s.z < 0.0001f) return;

        Collider col = GetComponent<Collider>();

        if (col is SphereCollider sphere)
        {
            float worldRadius = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)) * hitboxScale;
            sphere.center = Vector3.zero;
            sphere.radius = worldRadius / Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        }
        else if (col is BoxCollider box)
        {
            box.center = Vector3.zero;
            box.size = new Vector3(b.size.x / s.x, b.size.y / s.y, b.size.z / s.z) * hitboxScale;
        }
    }

    private void ApplyMaterial(GameObject visual)
    {
        Material baseMat = null;
        if (texture != null)
        {
            // Borrow the project's default material, so it works with your render pipeline
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = temp.GetComponent<Renderer>().sharedMaterial;
            Destroy(temp);
            if (baseMat == null) return;
        }

        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;

            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat;
                if (texture != null)
                {
                    mat = new Material(baseMat);
                    mat.mainTexture = texture;
                }
                else
                {
                    if (mats[i] == null) continue;
                    mat = new Material(mats[i]); // keep the model's own look and tint it
                }

                mat.color = tint;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
                mats[i] = mat;
            }
            r.sharedMaterials = mats;
        }
    }

    private static bool TryGetBounds(GameObject visual, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }
}
