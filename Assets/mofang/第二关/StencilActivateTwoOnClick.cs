using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 通过 Stencil 点击后：
/// 可选先用三层 RT + 颜色做模板测试（clickGateVisible），通过后再：
/// 可选在采样点做 3D Pick 魔方块；最后激活两个物体并将 clicked 置为 true。
/// </summary>
public class StencilActivateTwoOnClick : MonoBehaviour, IStencilClickable
{
    [Header("状态")]
    public bool clicked = false;

    [Tooltip("只读：两采样点中，只要有一个点通过三层 RT+颜色判定，则为 true。")]
    public bool anySampleVisible = false;

    [Tooltip("只读：采样点1是否通过三层 RT+颜色判定。")]
    public bool sample1Visible = false;
    [Tooltip("只读：采样点2是否通过三层 RT+颜色判定。")]
    public bool sample2Visible = false;
    [Tooltip("只读：按 requireBothSamples 得出的“点击门控可见”。")]
    public bool clickGateVisible = false;

    [Header("模板测试判定（三层 RT）")]
    [Tooltip("为 true 时：点击前会用三层 RT 判定是否通过模板测试；为 false 时不做判定。")]
    public bool useThreeRTForVisibility = true;

    [Tooltip("不填会运行时 FindObjectOfType。")]
    public CubeLayerRTPicker rtPicker;

    [Header("RT 前后关系（与 CubeLayerRTPicker 左下角预览对齐）")]
    [Tooltip(
        "为 true：把左下角调试预览里『最左边那张 RT』当成“最前层”参与颜色命中（等价于把 RtBack 与 RtFront 对调后再做可见性判定）。\n" +
        "为 false：与 StencilCubePlant 一致，用 RtPicker.RtFront 作为最前层。"
    )]
    public bool leftmostDebugPreviewIsFrontLayer = true;

    [Header("采样点（2 个，Viewport 0-1）")]
    [Tooltip("采样点1（Viewport 0-1）。")]
    public Vector2 sampleVp1 = new Vector2(0.45f, 0.9f);
    [Tooltip("采样点2（Viewport 0-1）。")]
    public Vector2 sampleVp2 = new Vector2(0.55f, 0.9f);
    [Tooltip("为 true 时：必须两个采样点都通过才算通过模板测试；为 false 时任意一个通过即可。")]
    public bool requireBothSamples = true;

    [Header("判定颜色（与 StencilCubePlant 一致）")]
    public Color targetColor = Color.red;
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;

    [Header("RT 三层遮挡判定（黑色为空，与 StencilCubePlant 一致）")]
    public float blackRgbThreshold = 0.02f;
    public float blackAlphaThreshold = 0.01f;
    public bool invertIsVisible = false;

    [Tooltip("为 true 时：点击被拦截会在 Console 打印原因（仅调试用）。")]
    public bool debugLog = false;

    [Header("采样点显示（Game 视图，可开关）")]
    [Tooltip("是否在 Game 视图叠加绘制采样点十字（运行时）。")]
    public bool showSampleCrossInGame = false;
    [Tooltip("十字颜色")]
    public Color sampleCrossColor = Color.yellow;
    [Tooltip("十字臂长（屏幕像素，一半）")]
    public float sampleCrossHalfSizePixels = 12f;
    [Tooltip("十字线粗细（像素）")]
    public float sampleCrossLineThickness = 2f;
    [Tooltip("ViewportToScreenPoint 用的相机前向距离（世界单位）")]
    public float sampleCrossDepth = 2.0f;

    public enum CubeDirectPickMode
    {
        None = 0,
        /// <summary>仍通过 StencilCubeRaycaster2D 命中本物体 Collider，再走 OnStencilClick。</summary>
        OnStencilRaycastHit = 1,

        /// <summary>在两个 sampleVp 的屏幕位置分别用 CubeLayerRTPicker 做『最前』3D Pick，命中魔方块则认为通过。</summary>
        GlobalMouseWhenGateVisible = 2,
    }

    [Header("可选：在两个采样点处直接 Pick 魔方块（不依赖点到 NPC Collider）")]
    [Tooltip(
        "None：仅当 StencilCubeRaycaster2D 等命中挂有本脚本的 Collider 时才会 OnStencilClick（不会在空白处生效）。\n" +
        "GlobalMouseWhenGateVisible：门控放行后任意左键都会尝试采样点 Pick（易误判，一般不用于「必须点到 NPC」）。\n" +
        "OnStencilRaycastHit：点到本物体后还要在两个采样点 Pick 魔方块才生效。"
    )]
    public CubeDirectPickMode cubeDirectPickMode = CubeDirectPickMode.None;

    [Tooltip("用于 Viewport -> ScreenPoint；留空则用 CubeLayerRTPicker.viewCamera，再退 Camera.main")]
    public Camera pickCamera;

    [Tooltip("Pick 命中 Collider 的 Layer 必须是这些层之一（需包含 CubeBack/CubeMid/CubeFront 或你场景的魔方块 Layer）")]
    public LayerMask cubePiecePickLayers;

    [Tooltip("为 true：两个采样点的 Pick 都必须成功；false：任意一个成功即可")]
    public bool directPickRequireBothSamples = true;

    [Tooltip("为 true：点击 UI 上不触发全局 Pick（与会话里 StencilCubeRaycaster2D 行为对齐）")]
    public bool ignoreUIBlockingForDirectPick = true;

    [Tooltip(
        "仅当 ignoreUIBlockingForDirectPick 为 true 时生效。\n" +
        "为 true：只有射线命中 Layer「UI」才算遮挡（适合全屏 Image 在 Default 层导致 Global 一直 UI遮挡）。\n" +
        "为 false：与 EventSystem.IsPointerOverGameObject() 一致。"
    )]
    public bool directPickUiBlockingOnlyOnUiLayer = false;

    [Header("点击后激活")]
    public GameObject activate;
    public GameObject activate2;

    [Header("可选回调")]
    public UnityEvent onClicked;

    Texture2D _readTex;
    Texture2D _whiteGuiTex;

    void Awake()
    {
        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        EnsureWhiteGuiTexture();

        if (cubePiecePickLayers.value == 0)
            cubePiecePickLayers = LayerMask.GetMask("CubeBack", "CubeMid", "CubeFront");
    }

    void Update()
    {
        if (useThreeRTForVisibility)
            ComputeVisibilityStateByThreeRT();
        else
        {
            sample1Visible = true;
            sample2Visible = true;
            anySampleVisible = true;
            clickGateVisible = true;
        }

        if (cubeDirectPickMode == CubeDirectPickMode.GlobalMouseWhenGateVisible
            && Input.GetMouseButtonDown(0))
        {
            bool uiBlock = ignoreUIBlockingForDirectPick
                           && IsDirectPickBlockedByUi(Input.mousePosition);
            if (debugLog)
                Debug.Log($"[StencilActivateTwoOnClick] Global 左键 Down，UI遮挡={uiBlock}" +
                          (uiBlock && directPickUiBlockingOnlyOnUiLayer == false
                              ? "（若误判请勾选 Direct Pick Ui Blocking Only On Ui Layer）"
                              : ""));
            if (!uiBlock)
            {
                if (!useThreeRTForVisibility || clickGateVisible)
                {
                    if (TryDirectPickCubePiecesAtSamples())
                        ApplyActivationEffects();
                    else if (debugLog)
                        Debug.Log("[StencilActivateTwoOnClick] Global：DirectPick 返回 false");
                }
                else if (debugLog)
                    Debug.Log("[StencilActivateTwoOnClick] Global：门控关闭 clickGateVisible=false");
            }
        }
    }

    /// <summary>
    /// Global DirectPick 专用的 UI 判定，与 StencilCubeRaycaster2D.uiBlockingOnlyOnUiLayer 语义一致。
    /// </summary>
    bool IsDirectPickBlockedByUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        if (!directPickUiBlockingOnlyOnUiLayer)
            return EventSystem.current.IsPointerOverGameObject();

        var ped = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>(8);
        EventSystem.current.RaycastAll(ped, results);
        if (results.Count == 0)
            return false;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
            return EventSystem.current.IsPointerOverGameObject();

        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            if (go != null && go.layer == uiLayer)
                return true;
        }

        return false;
    }

    void OnGUI()
    {
        if (!showSampleCrossInGame)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        EnsureWhiteGuiTexture();
        float z = Mathf.Max(0.01f, sampleCrossDepth);
        DrawCrossOnGameView(cam, sampleVp1, z);
        DrawCrossOnGameView(cam, sampleVp2, z);
    }

    public void OnStencilClick()
    {
        if (debugLog)
            Debug.Log($"[StencilActivateTwoOnClick] OnStencilClick 入口 directPickMode={cubeDirectPickMode}, useThreeRT={useThreeRTForVisibility}");

        if (cubeDirectPickMode == CubeDirectPickMode.GlobalMouseWhenGateVisible)
        {
            if (debugLog)
                Debug.Log("[StencilActivateTwoOnClick] GlobalMouseWhenGateVisible：不走 OnStencilClick；请看 Update 里 DirectPick 日志。");
            return;
        }

        if (useThreeRTForVisibility)
        {
            ComputeVisibilityStateByThreeRT();
            if (!clickGateVisible)
            {
                if (debugLog)
                    Debug.Log($"[StencilActivateTwoOnClick] 点击被拦截：sample1={sample1Visible}, sample2={sample2Visible}, requireBoth={requireBothSamples}, targetColor={targetColor}");
                return;
            }
        }

        if (cubeDirectPickMode == CubeDirectPickMode.OnStencilRaycastHit)
        {
            if (!TryDirectPickCubePiecesAtSamples())
            {
                if (debugLog)
                    Debug.Log("[StencilActivateTwoOnClick] OnStencilClick：采样点 Pick 未通过，已拦截。");
                return;
            }
        }

        if (debugLog)
            Debug.Log("[StencilActivateTwoOnClick] 门控与 Pick 已通过，即将 ApplyActivationEffects。");

        ApplyActivationEffects();
    }

    void ApplyActivationEffects()
    {
        if (clicked)
            return;

        clicked = true;

        if (activate != null) activate.SetActive(true);
        if (activate2 != null) activate2.SetActive(true);

        onClicked?.Invoke();
    }

    bool TryDirectPickCubePiecesAtSamples()
    {
        if (rtPicker == null)
            rtPicker = FindObjectOfType<CubeLayerRTPicker>();
        if (rtPicker == null)
            return false;

        Camera cam = pickCamera != null ? pickCamera : (rtPicker.viewCamera != null ? rtPicker.viewCamera : Camera.main);
        if (cam == null)
            return false;

        Vector3 sp1 = cam.ViewportToScreenPoint(new Vector3(sampleVp1.x, sampleVp1.y, Mathf.Max(0.01f, sampleCrossDepth)));
        Vector3 sp2 = cam.ViewportToScreenPoint(new Vector3(sampleVp2.x, sampleVp2.y, Mathf.Max(0.01f, sampleCrossDepth)));

        bool ok1 = TryPickCubePieceAtScreen(sp1);
        bool ok2 = TryPickCubePieceAtScreen(sp2);

        if (debugLog)
            Debug.Log($"[StencilActivateTwoOnClick] DirectPick ok1={ok1}, ok2={ok2}, requireBoth={directPickRequireBothSamples}");

        return directPickRequireBothSamples ? (ok1 && ok2) : (ok1 || ok2);
    }

    bool TryPickCubePieceAtScreen(Vector3 screenPoint)
    {
        if (rtPicker == null)
            return false;
        if (screenPoint.z < 0f)
            return false;

        rtPicker.EnsureRTRenderedForSampling();

        if (!rtPicker.TryPickFrontmostScreen(screenPoint, out RaycastHit hit))
            return false;

        if (hit.collider == null)
            return false;

        int layer = hit.collider.gameObject.layer;
        if (cubePiecePickLayers.value != 0 && ((1 << layer) & cubePiecePickLayers.value) == 0)
            return false;

        return true;
    }

    void ComputeVisibilityStateByThreeRT()
    {
        if (rtPicker == null)
            rtPicker = FindObjectOfType<CubeLayerRTPicker>();
        if (rtPicker == null)
        {
            sample1Visible = true;
            sample2Visible = true;
            anySampleVisible = true;
            clickGateVisible = true;
            return;
        }

        rtPicker.EnsureRTRenderedForSampling();
        var rtBack = rtPicker.RtBack;
        var rtMid = rtPicker.RtMid;
        var rtFront = rtPicker.RtFront;
        if (rtBack == null || rtMid == null || rtFront == null)
        {
            sample1Visible = true;
            sample2Visible = true;
            anySampleVisible = true;
            clickGateVisible = true;
            return;
        }

        if (leftmostDebugPreviewIsFrontLayer)
        {
            var tmp = rtBack;
            rtBack = rtFront;
            rtFront = tmp;
        }

        bool v1 = SampleIsVisibleByThreeRT(sampleVp1, rtBack, rtMid, rtFront);
        bool v2 = SampleIsVisibleByThreeRT(sampleVp2, rtBack, rtMid, rtFront);

        sample1Visible = v1;
        sample2Visible = v2;
        anySampleVisible = v1 || v2;
        clickGateVisible = requireBothSamples ? (v1 && v2) : (v1 || v2);
    }

    bool SampleIsVisibleByThreeRT(Vector2 vp01, RenderTexture rtBack, RenderTexture rtMid, RenderTexture rtFront)
    {
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return false;
        if (rtBack == null || rtMid == null || rtFront == null)
            return false;

        int w = rtFront.width;
        int h = rtFront.height;
        int px = Mathf.Clamp(Mathf.RoundToInt(vp01.x * w), 0, w - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(vp01.y * h), 0, h - 1);

        Color cBack = SampleRT(rtBack, px, py);
        Color cMid = SampleRT(rtMid, px, py);
        Color cFront = SampleRT(rtFront, px, py);

        bool backEmpty = IsBlackEmpty(cBack);
        bool midEmpty = IsBlackEmpty(cMid);
        bool frontEmpty = IsBlackEmpty(cFront);

        // 与 StencilCubePlant 一致：前层非空且颜色命中才算通过模板测试
        bool colorHit = ColorsClose(cFront, targetColor, colorTolerance);
        bool visibleRt = !frontEmpty && colorHit;
        return invertIsVisible ? !visibleRt : visibleRt;
    }

    Color SampleRT(RenderTexture rt, int px, int py)
    {
        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        var currentRT = RenderTexture.active;
        RenderTexture.active = rt;
        _readTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        _readTex.Apply();
        RenderTexture.active = currentRT;
        return _readTex.GetPixel(0, 0);
    }

    bool ColorsClose(Color a, Color b, float tol)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tol * tol;
    }

    bool IsBlackEmpty(Color c)
    {
        float maxRgb = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        bool blackRgb = maxRgb <= blackRgbThreshold;
        bool blackAlpha = c.a <= blackAlphaThreshold;
        return blackRgb && blackAlpha;
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
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return;

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
}
