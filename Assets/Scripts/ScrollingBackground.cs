using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ScrollingBackground : MonoBehaviour
{
    [Header("Scrolling")]
    [Tooltip("Screens scrolled per second")]
    [SerializeField] private float scrollSpeed = 0.3f;

    [Header("Optional: your own tileable art")]
    [Tooltip("Leave empty to use the generated placeholder pit wall")]
    [SerializeField] private Texture2D customTexture;

    private Material mat;
    private float offsetY;

    // The difficulty system changes this over time
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

        Texture2D tex = customTexture != null ? customTexture : GeneratePit();
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

    // Placeholder: dark brick wall, darker at the left and right edges (like a shaft)
    private Texture2D GeneratePit()
    {
        const int w = 256;
        const int h = 512;
        const int brickH = 32;
        const int brickW = 64;

        Color32[] px = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            int row = y / brickH;
            int yIn = y % brickH;
            int rowOffset = (row % 2 == 0) ? 0 : brickW / 2;

            for (int x = 0; x < w; x++)
            {
                int xs = x + rowOffset;
                int col = xs / brickW;
                int xIn = xs % brickW;

                bool mortar = yIn < 3 || xIn < 3;
                float shade;
                if (mortar)
                {
                    shade = 0.12f;
                }
                else
                {
                    shade = Mathf.Lerp(0.30f, 0.48f, Hash(row, col));
                    if (yIn < 6) shade += 0.05f; // light edge on top of each brick
                }

                // Darker toward the left and right edges
                float d = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f;
                float t = Mathf.InverseLerp(0.5f, 1f, d);
                float edge = t * t * (3f - 2f * t);
                shade *= Mathf.Lerp(1.15f, 0.35f, edge);

                px[y * w + x] = new Color(
                    Mathf.Clamp01(shade * 1.15f),
                    Mathf.Clamp01(shade * 0.85f),
                    Mathf.Clamp01(shade * 0.65f));
            }
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // Stable pseudo-random number between 0 and 1
    private static float Hash(int a, int b)
    {
        float s = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }
}