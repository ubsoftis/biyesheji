using UnityEngine;
using UnityEngine.Events;

public class StenciCube2 : MonoBehaviour
{
    [Header("相机与蒙版贴图")]
    public Camera mainCamera;
    public Camera maskCamera;
    public RenderTexture maskTexture;

    [Header("采样点（2 个检测目标，可自行填写位置）")]
    [Tooltip("Viewport 坐标(0-1)。你可以在 Inspector 自己改采样位置。")]
    public Vector2 sampleVp1 = new Vector2(0.45f, 0.9f);

    [Tooltip("Viewport 坐标(0-1)。你可以在 Inspector 自己改采样位置。")]
    public Vector2 sampleVp2 = new Vector2(0.55f, 0.9f);

    [Header("判定颜色")]
    public Color targetColor = Color.red;

    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;

    [Header("检测频率")]
    public int framesInterval = 1;

    [Header("采样点十字显示（Game 视图）")]
    [Tooltip("是否在 Game 视图叠加绘制采样点十字（运行时）")]
    public bool showSampleCrossInGame = true;
    [Tooltip("十字颜色")]
    public Color sampleCrossColor = Color.yellow;
    [Tooltip("十字臂长（屏幕像素，一半）")]
    public float sampleCrossHalfSizePixels = 12f;
    [Tooltip("十字线粗细（像素）")]
    public float sampleCrossLineThickness = 2f;
    [Tooltip("ViewportToScreenPoint 用的相机前向距离（世界单位）")]
    public float sampleCrossDepth = 2.0f;

    [Header("采样点十字显示（仅 Scene Gizmos）")]
    [Tooltip("是否在 Scene 视图显示 Gizmos 十字（Game 视图里看不到）")]
    public bool showSampleCrossInScene = false;
    [Tooltip("Scene Gizmos：十字半径（世界坐标）")]
    public float sampleCrossHalfSizeWorld = 0.15f;

    [Header("触发：2 个目标同时出现")]
    [Tooltip("当前这一帧是否满足：2 个检测点同时命中目标颜色")]
    public bool allTargetsVisible = false;

    [Header("白块离开状态（与 StencilCubePlant 对齐）")]
    [Tooltip("当前这一帧，两个采样点是否同时命中目标颜色（白块还在）")]
    public bool isVisible = false;

    [Tooltip("是否已经：白块从当前视图中消失（上一帧可见，这一帧不可见）")]
    public bool hitAndGone = false;

    [Header("点击门控方式")]
    [Tooltip("为 true 时，用 isVisible 自动开关 Collider2D；为 false 时不再改 Collider。")]
    public bool controlColliderByVisibility = true;

    [Header("自动控制一个物体的显隐")]
    [Tooltip("当两个目标同时出现时将其 SetActive(true)，否则 SetActive(false)。如果不需要自动控制就留空。")]
    public GameObject objectToToggle;

    [Tooltip("当 allTargetsVisible 从 false->true 时触发（只触发一次，直到再次变为 false）")]
    public UnityEvent onAllTargetsVisible;

    bool _allTargetsVisibleTriggered = false;
    bool _lastVisible = false;
    Collider2D _col;
    Texture2D _readTex;
    Texture2D _whiteGuiTex;
    int _frameCount;

    void Awake()
    {
        _col = GetComponent<Collider2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (maskTexture == null && maskCamera != null)
            maskTexture = maskCamera.targetTexture;

        if (maskCamera == null)
            Debug.LogError("[StenciCube2] 未指定 maskCamera。");

        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        EnsureWhiteGuiTexture();
    }

    void Update()
    {
        if (maskCamera == null || maskTexture == null || _readTex == null)
            return;

        _frameCount++;
        if (_frameCount % framesInterval != 0)
            return;

        bool v1 = SampleIsTargetColor(sampleVp1);
        bool v2 = SampleIsTargetColor(sampleVp2);
        isVisible = v1 && v2;
        allTargetsVisible = isVisible;

        if (controlColliderByVisibility && _col != null)
            _col.enabled = isVisible;

        if (_lastVisible && !isVisible)
            hitAndGone = true;
        _lastVisible = isVisible;

        if (objectToToggle != null)
            objectToToggle.SetActive(isVisible);

        if (isVisible && !_allTargetsVisibleTriggered)
        {
            _allTargetsVisibleTriggered = true;
            onAllTargetsVisible?.Invoke();
        }
        else if (!isVisible)
        {
            _allTargetsVisibleTriggered = false;
        }
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
        DrawCrossOnGameView(cam, sampleVp1, z);
        DrawCrossOnGameView(cam, sampleVp2, z);
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
        DrawCrossAtViewport(cam, sampleVp1, depth, sampleCrossHalfSizeWorld);
        DrawCrossAtViewport(cam, sampleVp2, depth, sampleCrossHalfSizeWorld);
    }

    void DrawCrossAtViewport(Camera cam, Vector2 vp01, float depth, float halfSize)
    {
        Vector3 world = cam.ViewportToWorldPoint(new Vector3(vp01.x, vp01.y, depth));
        Gizmos.DrawLine(world + Vector3.left * halfSize, world + Vector3.right * halfSize);
        Gizmos.DrawLine(world + Vector3.down * halfSize, world + Vector3.up * halfSize);
    }
}

