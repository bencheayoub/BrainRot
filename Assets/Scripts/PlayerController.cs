using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(100)] // runs after the EventSystem, so it already knows what the pointer is over
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float edgePadding = 0.5f;

    private Camera cam;
    private bool pressStartedOnUI; // a press that began on a button never moves the player

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
        if (ptr != null)
        {
            if (ptr.press.wasPressedThisFrame) pressStartedOnUI = IsOverUI(ptr.position.ReadValue());
            if (!ptr.press.isPressed) pressStartedOnUI = false;
        }

        if (x == 0f && ptr != null && ptr.press.isPressed && !pressStartedOnUI)
        {
            Vector2 screenPos = ptr.position.ReadValue();
            x = PointerDirection(screenPos);
        }

        return x;
#else
        float x = Input.GetAxisRaw("Horizontal");

        if (Input.GetMouseButtonDown(0)) pressStartedOnUI = IsOverUI(Input.mousePosition);
        if (!Input.GetMouseButton(0)) pressStartedOnUI = false;

        if (x == 0f && Input.GetMouseButton(0) && !pressStartedOnUI)
        {
            x = PointerDirection(Input.mousePosition);
        }

        return x;
#endif
    }

    // True when the pointer is on the pause button or on any other clickable UI
    private static bool IsOverUI(Vector2 screenPos)
    {
        if (PauseMenuController.IsOverPauseButton(screenPos)) return true;
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // -1 / +1 for a press on the left / right half of the GAME area, 0 if it should be ignored
    // (on the pause button, or on the black bars)
    private float PointerDirection(Vector2 screenPos)
    {
        if (PauseMenuController.IsPaused) return 0f;
        if (PauseMenuController.IsOverPauseButton(screenPos)) return 0f;

        Rect area = cam != null ? cam.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
        if (!area.Contains(screenPos)) return 0f;

        return screenPos.x < area.center.x ? -1f : 1f;
    }
}
