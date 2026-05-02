using UnityEngine;

/// <summary>
/// 从 StenciCube1 精简而来：移除「固定 3 个采样点」相关判定与其后续联动，
/// 仅保留「9 个采样点任意命中」(anyOf9Visible) 的判定，并用其作为门控驱动后续逻辑。
/// </summary>
public class StenciCube1Only9Samples : MonoBehaviour
{
    [Header("相机与蒙版贴图")]
    public Camera mainCamera;
    public Camera maskCamera;
    public RenderTexture maskTexture;

    [Header("9 个采样点（任意命中即 true）")]
    [Tooltip("启用 9 点采样（任意一点命中即 true）。")]
    public bool enable9Samples = true;

    [Tooltip("9 个采样点的 Viewport 坐标(0-1)。建议按你 UI/格子位置填满 9 个。")]
    public Vector2[] sampleVp9 = new Vector2[]
    {
        new Vector2(0.35f, 0.70f),
        new Vector2(0.50f, 0.70f),
        new Vector2(0.65f, 0.70f),
        new Vector2(0.35f, 0.18f),
        new Vector2(0.50f, 0.18f),
        new Vector2(0.65f, 0.18f),
        new Vector2(0.35f, 0.45f),
        new Vector2(0.50f, 0.45f),
        new Vector2(0.65f, 0.45f),
    };

    [Tooltip("只读：9 点采样中是否任意一点命中目标颜色。")]
    public bool anyOf9Visible = false;

    [Header("判定颜色（面2 可见时的颜色）")]
    public Color targetColor = Color.red;
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;

    [Header("检测频率")]
    public int framesInterval = 1;

    [Header("对外兼容字段（用 9 点结果作为可见性）")]
    [Tooltip("当前这一帧是否满足：9 点采样任意命中。")]
    public bool isVisible = false;

    [Tooltip("兼容旧读法：等同于 isVisible。")]
    public bool allTargetsVisible = false;

    [Header("点击门控方式")]
    [Tooltip("为 true 时，用 isVisible 自动开关 Collider2D；为 false 时不再改 Collider。")]
    public bool controlColliderByVisibility = true;

    [Header("自动控制一个物体的显隐（由 9 点判定驱动）")]
    [Tooltip("当 isVisible=true 时 SetActive(true)，否则 SetActive(false)。不需要就留空。")]
    public GameObject objectToToggle;

    [Header("采样点十字显示（Game 视图）")]
    public bool showSampleCrossInGame = true;
    public Color sampleCrossColor = Color.yellow;
    public float sampleCrossHalfSizePixels = 12f;
    public float sampleCrossLineThickness = 2f;
    public float sampleCrossDepth = 2.0f;

    [Header("采样点十字显示（仅 Scene Gizmos）")]
    public bool showSampleCrossInScene = false;
    public float sampleCrossHalfSizeWorld = 0.15f;

    Collider2D _col;
    Texture2D _readTex;
    Texture2D _whiteGuiTex;
    int _frameCount;

    void Awake()
    {
        _col = GetComponent<Collider2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (maskCamera == null)
            Debug.LogError("[StenciCube1Only9Samples] 未指定 maskCamera。");

        if (maskTexture == null && maskCamera != null)
            maskTexture = maskCamera.targetTexture;

        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        EnsureWhiteGuiTexture();
    }

    void Update()
    {
        if (mainCamera == null || maskCamera == null || maskTexture == null || _readTex == null)
            return;

        _frameCount++;
        if (_frameCount % framesInterval != 0)
            return;

        bool any9Mask = false;
        if (enable9Samples && sampleVp9 != null && sampleVp9.Length > 0)
        {
            for (int i = 0; i < sampleVp9.Length; i++)
            {
                if (SampleIsTargetColor(sampleVp9[i]))
                {
                    any9Mask = true;
                    break;
                }
            }
        }

        anyOf9Visible = any9Mask;
        isVisible = any9Mask;
        allTargetsVisible = isVisible;

        if (controlColliderByVisibility && _col != null)
            _col.enabled = isVisible;

        if (objectToToggle != null)
            objectToToggle.SetActive(isVisible);
    }

    bool ColorsClose(Color a, Color b, float tol)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tol * tol;
    }

    bool SampleIsTargetColor(Vector2 vp01)
    {
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return false;

        int px = Mathf.Clamp(Mathf.RoundToInt(vp01.x * maskTexture.width), 0, maskTexture.width - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(vp01.y * maskTexture.height), 0, maskTexture.height - 1);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = maskTexture;
        _readTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        _readTex.Apply();
        RenderTexture.active = currentRT;

        Color c = _readTex.GetPixel(0, 0);
        return ColorsClose(c, targetColor, colorTolerance);
    }

    void OnGUI()
    {
        if (!showSampleCrossInGame)
            return;

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null)
            return;

        EnsureWhiteGuiTexture();
        float z = Mathf.Max(0.01f, sampleCrossDepth);

        if (sampleVp9 == null)
            return;
        for (int i = 0; i < sampleVp9.Length; i++)
            DrawCrossOnGameView(cam, sampleVp9[i], z);
    }

    void EnsureWhiteGuiTexture()
    {
        if (_whiteGuiTex != null)
            return;
        _whiteGuiTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _whiteGuiTex.SetPixel(0, 0, Color.white);
        _whiteGuiTex.Apply(false, true);
    }

    void DrawCrossOnGameView(Camera cam, Vector2 vp01, float zWorld)
    {
        Vector3 sp = cam.ViewportToScreenPoint(new Vector3(vp01.x, vp01.y, zWorld));
        if (sp.z < 0f)
            return;

        float guiX = sp.x;
        float guiY = Screen.height - sp.y;
        float half = Mathf.Max(1f, sampleCrossHalfSizePixels);
        float t = Mathf.Max(1f, sampleCrossLineThickness);

        GUI.color = sampleCrossColor;
        GUI.DrawTexture(new Rect(guiX - half, guiY - t * 0.5f, half * 2f, t), _whiteGuiTex);
        GUI.DrawTexture(new Rect(guiX - t * 0.5f, guiY - half, t, half * 2f), _whiteGuiTex);
        GUI.color = Color.white;
    }

    void OnDrawGizmos()
    {
        if (!showSampleCrossInScene)
            return;

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null)
            return;

        float depth = Mathf.Max(0.01f, sampleCrossDepth);
        Gizmos.color = sampleCrossColor;

        if (sampleVp9 == null)
            return;
        for (int i = 0; i < sampleVp9.Length; i++)
            DrawCrossAtViewport(cam, sampleVp9[i], depth, sampleCrossHalfSizeWorld);
    }

    void DrawCrossAtViewport(Camera cam, Vector2 vp01, float depth, float halfSize)
    {
        Vector3 world = cam.ViewportToWorldPoint(new Vector3(vp01.x, vp01.y, depth));
        Gizmos.DrawLine(world + Vector3.left * halfSize, world + Vector3.right * halfSize);
        Gizmos.DrawLine(world + Vector3.down * halfSize, world + Vector3.up * halfSize);
    }
}

