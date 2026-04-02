using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("任意目标出现（至少 1 个点命中）")]
    [Tooltip("当前这一帧是否满足：3 个检测点中至少有 1 个命中目标颜色")]
    public bool anyTargetVisible = false;

    [Header("任意目标出现 -> 激活物体（可开关）")]
    [Tooltip("为 true 时：只要 anyTargetVisible=true 就会控制 objectToToggleWhenAnyVisible 的显隐。")]
    public bool enableAnyTargetActivation = false;
    [Tooltip("anyTargetVisible 为 true 时 SetActive(true)，否则 SetActive(false)。")]
    public GameObject objectToToggleWhenAnyVisible;

    [Header("白块离开状态（与 StencilCubePlant 对齐）")]
    [Tooltip("当前这一帧，三个采样点是否同时命中目标颜色（白块还在）")]
    public bool isVisible = false;

    [Tooltip("是否已经：白块从当前视图中消失（上一帧可见，这一帧不可见）")]
    public bool hitAndGone = false;

    [Header("点击门控方式")]
    [Tooltip("为 true 时，用 isVisible 自动开关 Collider2D；为 false 时不再改 Collider。")]
    public bool controlColliderByVisibility = true;

    [Header("自动控制一个物体的显隐")]
    [Tooltip("当三个目标同时出现时将其 SetActive(true)，否则 SetActive(false)。如果不需要自动控制就留空。")]
    public GameObject objectToToggle;

    [Header("联动判断：1号(3点) + 2号(2点)")]
    [Tooltip("拖入 StenciCube2 脚本，用于读取它的 allTargetsVisible。")]
    public StenciCube2 linkedCube2;
    [Tooltip("当 1号和2号都满足可见时激活这个物体。")]
    public GameObject objectToActivateWhenBothVisible;
    [Tooltip("为 true：条件不满足时自动关闭该物体；为 false：只在满足时打开，不自动关闭。")]
    public bool deactivateWhenNotBothVisible = true;

    [Header("自动控制多个物体（出现时：3 个 Active，2 个 Deactive）")]
    [Tooltip("当 allTargetsVisible=true 时会 SetActive(true) 的物体（建议拖 3 个）。")]
    public GameObject[] objectsToActivate;

    [Tooltip("当 allTargetsVisible=true 时会 SetActive(false) 的物体（建议拖 2 个）。")]
    public GameObject[] objectsToDeactivate;

    [Header("触发门控（bool 为 true 才执行）")]
    [Tooltip("为 false 时，不会再根据 allTargetsVisible 执行 objectsToActivate/objectsToDeactivate 的显隐切换，也不会触发 onAllTargetsVisible。")]
    public bool gateByBool = true;

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

        // 固定采样 3 个点：三个检测目标同时出现才算通过
        bool v1 = SampleIsTargetColor(sampleVp1);
        bool v2 = SampleIsTargetColor(sampleVp2);
        bool v3 = SampleIsTargetColor(sampleVp3);
        bool visible = v1 && v2 && v3;
        anyTargetVisible = v1 || v2 || v3;
        isVisible = visible;
        allTargetsVisible = isVisible;

        if (controlColliderByVisibility && _col != null)
            _col.enabled = isVisible;

        if (_lastVisible && !isVisible)
            hitAndGone = true;
        _lastVisible = isVisible;

        if (objectToToggle != null)
            objectToToggle.SetActive(isVisible);

        if (enableAnyTargetActivation && objectToToggleWhenAnyVisible != null)
            objectToToggleWhenAnyVisible.SetActive(anyTargetVisible);

        bool bothVisible = isVisible && linkedCube2 != null && linkedCube2.allTargetsVisible;
        if (objectToActivateWhenBothVisible != null)
        {
            if (deactivateWhenNotBothVisible)
                objectToActivateWhenBothVisible.SetActive(bothVisible);
            else if (bothVisible)
                objectToActivateWhenBothVisible.SetActive(true);
        }

        // 批量显隐：出现时激活一组、关闭另一组；未出现时反过来
        if (gateByBool && objectsToActivate != null)
        {
            for (int i = 0; i < objectsToActivate.Length; i++)
            {
                GameObject go = objectsToActivate[i];
                if (go != null) go.SetActive(isVisible);
            }
        }

        if (gateByBool && objectsToDeactivate != null)
        {
            for (int i = 0; i < objectsToDeactivate.Length; i++)
            {
                GameObject go = objectsToDeactivate[i];
                if (go != null) go.SetActive(!isVisible);
            }
        }

        bool canFireAllTargetsVisible = gateByBool && isVisible;
        if (canFireAllTargetsVisible && !_allTargetsVisibleTriggered)
        {
            _allTargetsVisibleTriggered = true;
            onAllTargetsVisible?.Invoke();
        }
        else if (!canFireAllTargetsVisible)
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
