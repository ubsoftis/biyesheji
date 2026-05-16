using UnityEngine;

public class StenciCube1 : MonoBehaviour
{
    [Header("相机与蒙版贴图")]
    public Camera mainCamera;          // 真正玩游戏看的相机（你现在的 Camera）
    public Camera maskCamera;          // 专门渲染蒙版的相机
    public RenderTexture maskTexture;  // maskCamera 的 TargetTexture，例如 64x64

    [Header("固定采样点（3 个检测目标）")]
    [Tooltip("Viewport 坐标(0-1)。把 3 个检测目标在屏幕上的位置填在这里")]
    public Vector2 sampleVp1 = new Vector2(0.4f, 0.9f);
    public Vector2 sampleVp2 = new Vector2(0.5f, 0.9f);
    public Vector2 sampleVp3 = new Vector2(0.6f, 0.9f);

    [Header("扩展：9 个采样点（任意命中即 true）")]
    [Tooltip("为 true 时：会额外计算 9 点采样的 anyOf9Visible（任意一点命中即 true）。不影响原先 3 点判定。")]
    public bool enable9Samples = false;
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
    public Color targetColor = Color.red;   // 面2 用的纯色
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;    // 允许一点点误差

    [Header("检测频率")]
    public int framesInterval = 1;          // 每几帧检测一次，1=每帧

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

    [Header("触发：3 个目标同时出现")]
    [Tooltip("当前这一帧是否满足：3 个检测目标同时命中目标颜色")]
    public bool allTargetsVisible = false;

    [Header("白块离开状态（与 StencilCubePlant 对齐）")]
    [Tooltip("当前这一帧，三个采样点是否同时命中目标颜色（白块还在）")]
    public bool isVisible = false;

    [Header("点击门控方式")]
    [Tooltip("为 true 时，用 isVisible 自动开关 Collider2D；为 false 时不再改 Collider。")]
    public bool controlColliderByVisibility = true;

    [Header("自动控制一个物体的显隐")]
    [Tooltip("当三个目标同时出现时将其 SetActive(true)，否则 SetActive(false)。如果不需要自动控制就留空。")]
    public GameObject objectToToggle;
    public GameObject objectToToggle2;
    public GameObject objectToToggle3;
    public GameObject objectToToggle4;
    public GameObject objectToDeactivateToggle5;
    public GameObject objectToDeactivateToggle6;

    [Header("联动判断：1号(3点) + 2号(2点)")]
    [Tooltip("拖入 StenciCube2 脚本，用于读取它的 allTargetsVisible。")]
    public StenciCube2 linkedCube2;
    [Tooltip("当 1号和2号都满足可见时激活这个物体。")]
    public GameObject objectToActivateWhenBothVisible;
    [Tooltip("为 true：条件不满足时自动关闭该物体；为 false：只在满足时打开，不自动关闭。")]
    public bool deactivateWhenNotBothVisible = true;

    // 这几个字段已由 `StenciCube1MultiObjectGate` 接管，用于在 Inspector 中统一配置。
    // 为了不破坏你现有场景序列化数据，这里先保留字段但隐藏。
    [HideInInspector]
    public GameObject[] objectsToActivate;

    [HideInInspector]
    public GameObject[] objectsToDeactivate;

    [HideInInspector]
    public bool restoreDeactivatedWhenNotAllVisible = true;

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
        {
            Debug.LogError("[StencilMaskClickGate] 未指定 maskCamera。");
        }

        if (maskTexture == null && maskCamera != null)
        {
            maskTexture = maskCamera.targetTexture;
        }

        // 与新版逻辑对齐：统一使用 RGBA32（兼容 alpha 判定场景）
        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        EnsureWhiteGuiTexture();
    }

    void OnEnable()
    {
        Debug.Log($"[StenciCube1] 已启用并开始运行：{gameObject.name}", this);
    }

    void Update()
    {
        if (mainCamera == null || maskCamera == null || maskTexture == null || _readTex == null)
            return;

        _frameCount++;
        if (_frameCount % framesInterval != 0)
            return;

        // 固定采样 3 个点：三个检测目标同时出现才算通过（保持原逻辑不变）
        bool v1 = SampleIsTargetColor(sampleVp1);
        bool v2 = SampleIsTargetColor(sampleVp2);
        bool v3 = SampleIsTargetColor(sampleVp3);
        bool visible3Mask = v1 && v2 && v3;

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

        isVisible = visible3Mask;
        allTargetsVisible = isVisible;

        if (controlColliderByVisibility && _col != null)
            _col.enabled = isVisible;

        if (objectToToggle != null)
            objectToToggle.SetActive(isVisible);
    if (objectToToggle2 != null)
                objectToToggle2.SetActive(isVisible);   
        if (objectToToggle3 != null)
            objectToToggle3.SetActive(isVisible);
        if (objectToToggle4 != null)
            objectToToggle4.SetActive(isVisible);
        if (objectToDeactivateToggle5 != null)
            objectToDeactivateToggle5.SetActive(false);
        if (objectToDeactivateToggle6 != null)
            objectToDeactivateToggle6.SetActive(false);
        bool bothVisible = isVisible && linkedCube2 != null && linkedCube2.allTargetsVisible;
        if (objectToActivateWhenBothVisible != null)
        {
            if (deactivateWhenNotBothVisible)
                objectToActivateWhenBothVisible.SetActive(bothVisible);
            else if (bothVisible)
                objectToActivateWhenBothVisible.SetActive(true);
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
        DrawCrossOnGameView(cam, sampleVp3, z);
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
        DrawCrossAtViewport(cam, sampleVp3, depth, sampleCrossHalfSizeWorld);
    }

    void DrawCrossAtViewport(Camera cam, Vector2 vp01, float depth, float halfSize)
    {
        Vector3 world = cam.ViewportToWorldPoint(new Vector3(vp01.x, vp01.y, depth));
        Gizmos.DrawLine(world + Vector3.left * halfSize, world + Vector3.right * halfSize);
        Gizmos.DrawLine(world + Vector3.down * halfSize, world + Vector3.up * halfSize);
    }
}
