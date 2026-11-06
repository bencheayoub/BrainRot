using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float edgePadding = 0.5f;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        // Keep the player on the gameplay plane (z = 0)
        Vector3 p = transform.position;
        p.z = 0f;
        transform.position = p;
    }

    private void Update()
    {
        // Player can't move after game over
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        float input = ReadHorizontal();

        Vector3 pos = transform.position;
        pos.x += input * moveSpeed * Time.deltaTime;

        // Screen boundaries, computed from the camera so it works on any aspect ratio
        float halfWidth = cam.orthographicSize * cam.aspect;
        float limit = halfWidth - edgePadding;
        pos.x = Mathf.Clamp(pos.x, -limit, limit);

        transform.position = pos;
    }

    // Runs when the player touches a fruit or obstacle (both have trigger colliders)
    private void OnTriggerEnter(Collider other)
    {
        Fruit fruit = other.GetComponent<Fruit>();
        if (fruit == null) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CatchFruit(fruit);
        }

        Destroy(fruit.gameObject);
    }

    // Returns -1 (left), 0 (none) or +1 (right)
    private float ReadHorizontal()
    {
#if ENABLE_INPUT_SYSTEM
        float x = 0f;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) x -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) x += 1f;
        }

        // Mouse or touch: hold the left or right half of the screen
        // (a press on the pause button does not count)
        Pointer ptr = Pointer.current;
        if (x == 0f && ptr != null && ptr.press.isPressed)
        {
            Vector2 screenPos = ptr.position.ReadValue();
            if (!PauseMenuController.IsOverPauseButton(screenPos))
            {
                x = screenPos.x < Screen.width * 0.5f ? -1f : 1f;
            }
        }

        return x;
#else
        float x = Input.GetAxisRaw("Horizontal");

        if (x == 0f && Input.GetMouseButton(0))
        {
            Vector2 screenPos = Input.mousePosition;
            if (!PauseMenuController.IsOverPauseButton(screenPos))
            {
                x = screenPos.x < Screen.width * 0.5f ? -1f : 1f;
            }
        }

        return x;
#endif
    }
}
