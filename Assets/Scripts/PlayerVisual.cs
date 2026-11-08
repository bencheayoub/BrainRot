using UnityEngine;

// Shows the character picked in the menu on the Player object.
// Put this on the Player in the Game scene.
// Element 0 = Tung Tung Tung Sahur, element 1 = Potato with Glasses (same order as CharacterSelection).
// While a model slot is empty, a placeholder shape is shown instead, so the game always works.
//
// New: the model is resized automatically to "Target Height", so a model exported at a huge or tiny
// scale no longer fills the screen. A texture can be dragged straight into the slot as well.
public class PlayerVisual : MonoBehaviour
{
    [System.Serializable]
    public class CharacterModel
    {
        public string label = "Character";

        [Tooltip("Drag the 3D model (.fbx or prefab) here. Empty = placeholder shape.")]
        public GameObject prefab;

        [Tooltip("Drag the model's texture (PNG) here. Empty = keep the model's own material.")]
        public Texture2D texture;

        [Tooltip("How tall the character is in the game (world units). The model is resized to this height automatically. 0 = do not resize.")]
        public float targetHeight = 1.6f;

        [Tooltip("Extra size multiplier on top of the automatic resize")]
        public float scale = 1f;

        [Tooltip("Rotation in degrees. Set this FIRST so the model stands upright (try X = -90 or 90 if it lies down, Y = 180 if it faces away).")]
        public Vector3 rotation = Vector3.zero;

        [Tooltip("Move the model relative to the Player")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("Shape shown while there is no model")]
        public PrimitiveType placeholderShape = PrimitiveType.Capsule;
        public Color placeholderColor = Color.white;
    }

    [SerializeField]
    private CharacterModel[] models =
    {
        new CharacterModel
        {
            label = "Tung Tung Tung Sahur",
            placeholderShape = PrimitiveType.Capsule,
            placeholderColor = new Color(0.55f, 0.35f, 0.15f),
            targetHeight = 1.6f
        },
        new CharacterModel
        {
            label = "Potato with Glasses",
            placeholderShape = PrimitiveType.Sphere,
            placeholderColor = new Color(0.85f, 0.70f, 0.40f),
            targetHeight = 1.2f
        }
    };

    private void Awake()
    {
        // Hide the Player's own capsule, the chosen character replaces it
        MeshRenderer own = GetComponent<MeshRenderer>();
        if (own != null) own.enabled = false;

        if (models == null || models.Length == 0) return;

        int index = Mathf.Clamp(CharacterSelection.Index, 0, models.Length - 1);
        CharacterModel m = models[index];

        GameObject visual;
        if (m.prefab != null)
        {
            visual = Instantiate(m.prefab, transform);
        }
        else
        {
            visual = GameObject.CreatePrimitive(m.placeholderShape);
            visual.transform.SetParent(transform, false);
            visual.GetComponent<Renderer>().material.color = m.placeholderColor;
        }

        // Only the Player's own trigger collider should take part in catching things
        foreach (Collider c in visual.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);

        // Models exported from Blender often carry their own camera and light.
        // They would take over the screen and blow out the lighting, so remove them.
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

        visual.name = "Visual_" + m.label;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = visual.transform.localRotation * Quaternion.Euler(m.rotation);

        if (m.texture != null) ApplyTexture(visual, m.texture);

        if (m.targetHeight > 0f) FitHeight(visual, m.targetHeight);
        visual.transform.localScale = visual.transform.localScale * m.scale;

        CenterOnPlayer(visual);
        visual.transform.localPosition += m.positionOffset;
    }

    // Resize so the character is exactly targetHeight tall in the game
    private void FitHeight(GameObject visual, float targetHeight)
    {
        if (!TryGetBounds(visual, out Bounds b) || b.size.y < 0.0001f) return;
        visual.transform.localScale = visual.transform.localScale * (targetHeight / b.size.y);
    }

    // Put the middle of the model on the middle of the Player
    private void CenterOnPlayer(GameObject visual)
    {
        if (!TryGetBounds(visual, out Bounds b)) return;
        visual.transform.position += transform.position - b.center;
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

    // Gives every part of the model a fresh material with this texture
    private static void ApplyTexture(GameObject visual, Texture2D texture)
    {
        // Borrow the project's default material, so it always works with your render pipeline
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material baseMat = temp.GetComponent<Renderer>().sharedMaterial;
        Destroy(temp);
        if (baseMat == null) return;

        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;

            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = new Material(baseMat);
                mat.mainTexture = texture;
                mat.color = Color.white;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
                mats[i] = mat;
            }
            r.sharedMaterials = mats;
        }
    }
}
