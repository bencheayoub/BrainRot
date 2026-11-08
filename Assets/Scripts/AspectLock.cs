using UnityEngine;
using UnityEngine.SceneManagement;

// Forces the game to always keep ONE shape (aspect ratio), whatever the window / Game view / phone is.
// If the screen has a different shape, black bars are added at the sides or at the top and bottom.
//
// Nothing to attach: it starts by itself in every scene (MainMenu and Game).
// To change the shape, change the two numbers below (default = 1080 x 1920, same as the menu's canvas).
//
// How it works:
//  - every normal camera is shrunk to the allowed area (Camera.rect), re-checked EVERY frame
//  - every "Screen Space - Overlay" canvas is switched to "Screen Space - Camera", so the UI is shrunk the same way
//  - one extra black camera fills the bars
// Look in the Console for lines starting with "AspectLock:" to see what it did.
[DefaultExecutionOrder(-1000)]
public class AspectLock : MonoBehaviour
{
    // ===== CHANGE THESE TWO NUMBERS TO CHANGE THE FIXED SHAPE =====
    private const float TargetWidth = 1080f;
    private const float TargetHeight = 1920f;
    // ===============================================================

    private static AspectLock instance;

    private Camera barsCamera;
    private int lastW, lastH;
    private float nextCanvasScan;
    private bool forceCanvasScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStart()
    {
        if (instance != null) return;

        GameObject go = new GameObject("AspectLock");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AspectLock>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        // Black camera that fills the bars. It draws first, the game cameras draw on top of it.
        GameObject camGO = new GameObject("BarsCamera");
        camGO.transform.SetParent(transform, false);
        barsCamera = camGO.AddComponent<Camera>();
        barsCamera.clearFlags = CameraClearFlags.SolidColor;
        barsCamera.backgroundColor = Color.black;
        barsCamera.cullingMask = 0;
        barsCamera.depth = -100f;
        barsCamera.rect = new Rect(0f, 0f, 1f, 1f);

        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("AspectLock: started, fixed shape " + TargetWidth + " x " + TargetHeight);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        forceCanvasScan = true;
        Debug.Log("AspectLock: scene \"" + scene.name + "\" loaded, applying the fixed shape");
    }

    private void LateUpdate()
    {
        if (Screen.height <= 0) return;

        Camera main = ApplyToCameras(); // every frame, so nothing can undo it

        bool sizeChanged = Screen.width != lastW || Screen.height != lastH;
        if (sizeChanged || forceCanvasScan || Time.unscaledTime >= nextCanvasScan)
        {
            lastW = Screen.width;
            lastH = Screen.height;
            forceCanvasScan = false;
            nextCanvasScan = Time.unscaledTime + 0.1f;

            ApplyToCanvases(main);
        }
    }

    private static Rect AllowedArea()
    {
        float window = (float)Screen.width / Screen.height;
        float target = TargetWidth / TargetHeight;

        if (window > target)
        {
            float w = target / window; // window too wide: bars left and right
            return new Rect((1f - w) * 0.5f, 0f, w, 1f);
        }

        float h = window / target; // window too tall: bars top and bottom
        return new Rect(0f, (1f - h) * 0.5f, 1f, h);
    }

    private Camera ApplyToCameras()
    {
        Rect area = AllowedArea();
        Camera main = null;

        foreach (Camera cam in Camera.allCameras)
        {
            if (cam == barsCamera) continue;
            if (cam.targetTexture != null) continue; // e.g. the 3D hero preview camera
            if (cam.cameraType != CameraType.Game) continue;

            if (cam.rect != area) cam.rect = area;
            if (main == null || cam == Camera.main) main = cam;
        }

        return main;
    }

    private void ApplyToCanvases(Camera main)
    {
        if (main == null) return;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (!c.isRootCanvas) continue;

            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = main;
                c.planeDistance = main.nearClipPlane + 0.1f;
            }
            else if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == null)
            {
                c.worldCamera = main;
                c.planeDistance = main.nearClipPlane + 0.1f;
            }
        }
    }
}
