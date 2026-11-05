using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ScrollingBackground : MonoBehaviour
{
    [Header("Scrolling")]
    [Tooltip("Screens scrolled per second")]
    [SerializeField] private float scrollSpeed = 0.3f;

    [Header("Optional: your own tileable art")]
    [Tooltip("Leave empty to use the generated placeholder sky")]
    [SerializeField] private Texture2D customTexture;

    private Material mat;
    private float offsetY;

    // Other scripts (difficulty system) can change the speed later
    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = value;
    }

    private void Start()
    {
        Camera cam = Camera.main;

        // Stretch the quad to cover the whole camera view
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        transform.localScale = new Vector3(width, height, 1f);

        Texture2D tex = customTexture != null ? customTexture : GenerateSky();
        tex.wrapMode = TextureWrapMode.Repeat;

        mat = GetComponent<MeshRenderer>().material;
        mat.mainTexture = tex;
    }

    private void Update()
    {
        // Texture moves UP on screen, so the player seems to fall
        offsetY = Mathf.Repeat(offsetY - scrollSpeed * Time.deltaTime, 1f);
        mat.mainTextureOffset = new Vector2(0f, offsetY);
    }

    // Placeholder: soft blue sky with clouds, tiles vertically
    private Texture2D GenerateSky()
    {
        const int w = 256;
        const int h = 512;
        Color32[] px = new Color32[w * h];

        Color top = new Color(0.35f, 0.65f, 0.95f);
        Color bottom = new Color(0.55f, 0.82f, 1f);

        for (int y = 0; y < h; y++)
        {
            float t = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * y / h); // loops seamlessly
            Color c = Color.Lerp(top, bottom, t);
            for (int x = 0; x < w; x++)
                px[y * w + x] = c;
        }

        System.Random rng = new System.Random(7);
        for (int i = 0; i < 14; i++)
        {
            int cx = rng.Next(0, w);
            int cy = rng.Next(0, h);
            int r = rng.Next(20, 45);

            // Draw 3 times (y - h, y, y + h) so clouds wrap around the edge
            for (int k = -1; k <= 1; k++)
                DrawCloud(px, w, h, cx, cy + k * h, r);
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    private void DrawCloud(Color32[] px, int w, int h, int cx, int cy, int r)
    {
        for (int y = cy - r; y <= cy + r; y++)
        {
            if (y < 0 || y >= h) continue;
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= w) continue;
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                if (d >= 1f) continue;

                float a = (1f - d) * 0.6f;
                Color32 src = px[y * w + x];
                px[y * w + x] = new Color32(
                    (byte)Mathf.Lerp(src.r, 255, a),
                    (byte)Mathf.Lerp(src.g, 255, a),
                    (byte)Mathf.Lerp(src.b, 255, a),
                    255);
            }
        }
    }
}