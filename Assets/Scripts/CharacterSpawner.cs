using UnityEngine;

// Lives in the Game scene. Puts the chosen character's model on the Player.
// The Player object keeps its collider and PlayerController. Only the look changes.
public class CharacterSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Tooltip("Element 0 = Tung Tung Tung Sahur, Element 1 = Potato")]
    [SerializeField] private GameObject[] characterPrefabs;

    [Header("Model adjustments")]
    [SerializeField] private float modelScale = 1f;
    [SerializeField] private Vector3 modelOffset = Vector3.zero;
    [Tooltip("Extra rotation in degrees, e.g. Y = 180 if the model faces away")]
    [SerializeField] private Vector3 modelRotation = Vector3.zero;

    public GameObject CurrentModel { get; private set; }

    private void Awake()
    {
        if (player == null)
        {
            Debug.LogWarning("CharacterSpawner: the Player field is empty.");
            return;
        }

        int index = CharacterSelection.Index;
        if (characterPrefabs == null || index >= characterPrefabs.Length || characterPrefabs[index] == null)
        {
            Debug.LogWarning("CharacterSpawner: no prefab assigned for character " + index +
                             ". Keeping the original Player model.");
            return;
        }

        // Remember the old visuals BEFORE adding the new model, then hide them
        Renderer[] oldRenderers = player.GetComponentsInChildren<Renderer>();

        GameObject prefab = characterPrefabs[index];
        CurrentModel = Instantiate(prefab, player);
        CurrentModel.name = prefab.name + " (Model)";

        Transform t = CurrentModel.transform;
        t.localPosition = modelOffset;
        t.localRotation = Quaternion.Euler(modelRotation) * t.localRotation;
        t.localScale *= modelScale;

        // The Player already has the collider and the controller.
        // Remove anything gameplay-related that came with the model prefab.
        foreach (Collider c in CurrentModel.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (Rigidbody r in CurrentModel.GetComponentsInChildren<Rigidbody>()) Destroy(r);
        foreach (PlayerController pc in CurrentModel.GetComponentsInChildren<PlayerController>()) Destroy(pc);

        foreach (Renderer r in oldRenderers) r.enabled = false;
    }
}