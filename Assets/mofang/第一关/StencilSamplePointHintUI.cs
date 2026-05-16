using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在已带有 StenciCube1 / StenciCube2 的物体上（不修改二者脚本）。
/// 以「视口采样点」形式布置 UI：与 StenciCube 的 Viewport + sampleCrossDepth 一致，可选每帧跟随；
/// 可选用两条 Image 画出与游戏里黄色十字同尺寸的采样十字；入口按钮也可摆在采样点上。
/// </summary>
public class StencilSamplePointHintUI : MonoBehaviour
{
    public enum DetectionMode
    {
        [Tooltip("StenciCube1：三个采样点同时命中（isVisible）")]
        Cube1_AllSamplesVisible = 0,
        [Tooltip("StenciCube2：两个采样点同时命中（isVisible）")]
        Cube2_AllSamplesVisible = 1,
        [Tooltip("Cube1 与 Cube2 同时满足（与 StenciCube1.linkedCube2 组合逻辑一致）")]
        Cube1AndCube2_BothVisible = 2
    }

    public enum ViewportAnchorSource
    {
        Manual = 0,
        Cube1_Sample1 = 1,
        Cube1_Sample2 = 2,
        Cube1_Sample3 = 3,
        Cube2_Sample1 = 4,
        Cube2_Sample2 = 5
    }

    [Header("引用（留空则从本物体自动 GetComponent）")]
    public StenciCube1 cube1;
    public StenciCube2 cube2;

    [Header("何时允许点击入口按钮")]
    public DetectionMode detectionMode = DetectionMode.Cube1_AllSamplesVisible;

    [Header("入口按钮")]
    [Tooltip("检测通过才可点；未通过时可隐藏整段按钮区域（见下方选项）")]
    public Button gateButton;
    [Tooltip("可选：整块按钮父物体，未通过检测时 SetActive(false)")]
    public GameObject gateButtonRoot;
    [Tooltip("为 true：检测未通过时隐藏 gateButtonRoot；为 false：保持显示但按钮不可点")]
    public bool hideGateRootWhenBlocked = true;

    [Header("点击后打开的 UI")]
    [Tooltip("初始建议设为 Inactive，第一次点击入口后 SetActive(true)")]
    public GameObject hintPanel;
    [Tooltip("做上下浮动的 RectTransform（一般是文字父物体）；可空则不做浮动")]
    public RectTransform floatingTarget;
    [Tooltip("可选：需第二次点击才显示的文字/容器；若留空则第一次点击就显示 hintPanel 内已有内容")]
    public GameObject optionalTextGroup;
    [Tooltip("与 optionalTextGroup 配套：点此按钮后才显示文字；留空则第一次点击即显示 optionalTextGroup（若挂了该物体）")]
    public Button optionalRevealTextButton;

    [Header("浮动动画（像素，沿 anchoredPosition.y）")]
    public float bobSpeed = 2.2f;
    public float bobAmplitudePixels = 10f;
    public bool useUnscaledTime = true;

    [Header("采样点形式（与 StenciCube 十字一致）")]
    [Tooltip("为 true：hintPanel / 入口 使用视口坐标 + sampleCrossDepth，锚点居中，像贴在采样十字中心")]
    public bool useSamplePointPresentation = true;
    [Tooltip("为 true：每帧刷新位置（分辨率或相机变化时仍对齐采样点）")]
    public bool followViewportEachFrame = true;
    [Tooltip("为 true：检测通过时把 gateButtonRoot 摆在当前锚定的采样点上（小圆按钮等）")]
    public bool positionGateButtonAtSample = true;
    public ViewportAnchorSource viewportAnchorSource = ViewportAnchorSource.Cube1_Sample1;
    [Tooltip("当 viewportAnchorSource = Manual 时使用")]
    public Vector2 manualViewport01 = new Vector2(0.5f, 0.5f);
    [Tooltip("相对采样点屏幕坐标的像素偏移（文字常在十字上方）")]
    public Vector2 screenOffsetPixels = new Vector2(0f, 48f);
    [Tooltip("入口按钮相对采样点的像素偏移（可与 hint 不同）")]
    public Vector2 gateScreenOffsetPixels = new Vector2(0f, 0f);
    [Tooltip("留空则用 cube1/cube2 的 mainCamera")]
    public Camera positionCamera;

    [Header("可选：UI 十字（两条 Image，与魔方 OnGUI 十字同尺寸）")]
    [Tooltip("横线（Image）；留空则只摆面板不画十字")]
    public RectTransform sampleCrossArmHorizontal;
    [Tooltip("竖线（Image）")]
    public RectTransform sampleCrossArmVertical;
    [Tooltip("未挂魔方时十字臂半长（像素）")]
    public float fallbackCrossHalfPixels = 12f;
    [Tooltip("未挂魔方时线粗（像素）")]
    public float fallbackCrossThickness = 2f;

    [Header("检测丢失时")]
    [Tooltip("为 true：检测不再满足时关闭 hintPanel 并重置“第二次点击才显示”的状态")]
    public bool closePanelWhenDetectionLost = true;

    bool _panelOpen;
    Vector2 _floatBaseAnchored;
    RectTransform _hintPanelRt;
    RectTransform _gateRootRt;

    void Awake()
    {
        if (cube1 == null)
            cube1 = GetComponent<StenciCube1>();
        if (cube2 == null)
            cube2 = GetComponent<StenciCube2>();

        if (hintPanel != null)
            _hintPanelRt = hintPanel.GetComponent<RectTransform>();
        if (gateButtonRoot != null)
            _gateRootRt = gateButtonRoot.GetComponent<RectTransform>();

        if (gateButton != null)
            gateButton.onClick.AddListener(OnGateButtonClicked);
        if (optionalRevealTextButton != null)
            optionalRevealTextButton.onClick.AddListener(OnRevealTextClicked);

        if (floatingTarget != null)
            _floatBaseAnchored = floatingTarget.anchoredPosition;
    }

    void OnDestroy()
    {
        if (gateButton != null)
            gateButton.onClick.RemoveListener(OnGateButtonClicked);
        if (optionalRevealTextButton != null)
            optionalRevealTextButton.onClick.RemoveListener(OnRevealTextClicked);
    }

    void Update()
    {
        bool ok = IsDetectionPassed();

        if (gateButton != null)
            gateButton.interactable = ok;

        if (gateButtonRoot != null)
        {
            if (hideGateRootWhenBlocked)
                gateButtonRoot.SetActive(ok);
            else
                gateButtonRoot.SetActive(true);
        }

        if (!ok && closePanelWhenDetectionLost && _panelOpen)
        {
            if (hintPanel != null)
                hintPanel.SetActive(false);
            if (optionalTextGroup != null)
                optionalTextGroup.SetActive(false);
            _panelOpen = false;
        }

        if (_panelOpen && floatingTarget != null)
        {
            float t = useUnscaledTime ? Time.unscaledTime : Time.time;
            float dy = Mathf.Sin(t * bobSpeed) * bobAmplitudePixels;
            floatingTarget.anchoredPosition = _floatBaseAnchored + new Vector2(0f, dy);
        }

        if (!useSamplePointPresentation || !followViewportEachFrame)
            return;

        bool gateShown = gateButtonRoot != null && gateButtonRoot.activeSelf;
        if (positionGateButtonAtSample && ok && gateShown)
            PositionRectAtViewportSample(_gateRootRt, gateScreenOffsetPixels);

        if (_panelOpen && hintPanel != null && hintPanel.activeSelf)
            PositionRectAtViewportSample(_hintPanelRt, screenOffsetPixels);
    }

    bool IsDetectionPassed()
    {
        switch (detectionMode)
        {
            case DetectionMode.Cube1_AllSamplesVisible:
                return cube1 != null && cube1.isVisible;
            case DetectionMode.Cube2_AllSamplesVisible:
                return cube2 != null && cube2.isVisible;
            case DetectionMode.Cube1AndCube2_BothVisible:
                return cube1 != null && cube1.isVisible && cube2 != null && cube2.allTargetsVisible;
            default:
                return false;
        }
    }

    float GetSampleCrossDepth()
    {
        if (cube1 != null)
            return cube1.sampleCrossDepth;
        if (cube2 != null)
            return cube2.sampleCrossDepth;
        return 2f;
    }

    void GetCrossPixelStyle(out float halfPixels, out float thickness, out Color tint)
    {
        if (cube1 != null)
        {
            halfPixels = Mathf.Max(1f, cube1.sampleCrossHalfSizePixels);
            thickness = Mathf.Max(1f, cube1.sampleCrossLineThickness);
            tint = cube1.sampleCrossColor;
            return;
        }

        if (cube2 != null)
        {
            halfPixels = Mathf.Max(1f, cube2.sampleCrossHalfSizePixels);
            thickness = Mathf.Max(1f, cube2.sampleCrossLineThickness);
            tint = cube2.sampleCrossColor;
            return;
        }

        halfPixels = Mathf.Max(1f, fallbackCrossHalfPixels);
        thickness = Mathf.Max(1f, fallbackCrossThickness);
        tint = Color.yellow;
    }

    Vector2 GetAnchorViewport01()
    {
        switch (viewportAnchorSource)
        {
            case ViewportAnchorSource.Manual:
                return manualViewport01;
            case ViewportAnchorSource.Cube1_Sample1:
                return cube1 != null ? cube1.sampleVp1 : manualViewport01;
            case ViewportAnchorSource.Cube1_Sample2:
                return cube1 != null ? cube1.sampleVp2 : manualViewport01;
            case ViewportAnchorSource.Cube1_Sample3:
                return cube1 != null ? cube1.sampleVp3 : manualViewport01;
            case ViewportAnchorSource.Cube2_Sample1:
                return cube2 != null ? cube2.sampleVp1 : manualViewport01;
            case ViewportAnchorSource.Cube2_Sample2:
                return cube2 != null ? cube2.sampleVp2 : manualViewport01;
            default:
                return manualViewport01;
        }
    }

    Camera ResolvePositionCamera()
    {
        if (positionCamera != null)
            return positionCamera;
        if (cube1 != null && cube1.mainCamera != null)
            return cube1.mainCamera;
        if (cube2 != null && cube2.mainCamera != null)
            return cube2.mainCamera;
        return Camera.main;
    }

    /// <summary>
    /// 与 StenciCube.DrawCrossOnGameView 同源：Viewport + depth → 屏幕像素（左下原点）。
    /// </summary>
    Vector2 GetSampleScreenPoint(Camera cam, Vector2 vp01, float depthZ, Vector2 extraOffsetPixels)
    {
        if (cam == null)
            return extraOffsetPixels;
        float z = Mathf.Max(0.01f, depthZ);
        Vector3 sp = cam.ViewportToScreenPoint(new Vector3(vp01.x, vp01.y, z));
        if (sp.z < 0f)
            return Vector2.zero;
        return new Vector2(sp.x, sp.y) + extraOffsetPixels;
    }

    void ApplySamplePivot(RectTransform rt)
    {
        if (rt == null || !useSamplePointPresentation)
            return;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static bool TryScreenToCanvasLocal(RectTransform uiElement, Vector2 screenPoint, out Vector2 localPoint)
    {
        localPoint = default;
        Canvas root = uiElement.GetComponentInParent<Canvas>()?.rootCanvas;
        if (root == null)
            return false;
        var canvasRect = root.transform as RectTransform;
        Camera eventCam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCam, out localPoint);
    }

    void PositionRectAtViewportSample(RectTransform rt, Vector2 extraOffsetPixels)
    {
        if (rt == null)
            return;

        Camera cam = ResolvePositionCamera();
        if (cam == null)
            return;

        Vector2 vp = GetAnchorViewport01();
        Vector2 screen = GetSampleScreenPoint(cam, vp, GetSampleCrossDepth(), extraOffsetPixels);
        ApplySamplePivot(rt);

        if (!TryScreenToCanvasLocal(rt, screen, out Vector2 local))
            return;

        rt.anchoredPosition = local;

        if (hintPanel != null && rt == _hintPanelRt)
            SyncSampleCrossUiUnder(rt);
    }

    /// <summary>
    /// 横竖臂请在编辑器里挂在 hintPanel 子级（本方法只改尺寸/颜色，与 StenciCube 十字像素一致）。
    /// </summary>
    void SyncSampleCrossUiUnder(RectTransform parentRt)
    {
        if (parentRt == null)
            return;
        GetCrossPixelStyle(out float half, out float thick, out Color tint);

        if (sampleCrossArmHorizontal != null)
        {
            sampleCrossArmHorizontal.anchorMin = sampleCrossArmHorizontal.anchorMax = new Vector2(0.5f, 0.5f);
            sampleCrossArmHorizontal.pivot = new Vector2(0.5f, 0.5f);
            sampleCrossArmHorizontal.anchoredPosition = Vector2.zero;
            sampleCrossArmHorizontal.sizeDelta = new Vector2(half * 2f, thick);
            var img = sampleCrossArmHorizontal.GetComponent<Image>();
            if (img != null)
                img.color = tint;
        }

        if (sampleCrossArmVertical != null)
        {
            sampleCrossArmVertical.anchorMin = sampleCrossArmVertical.anchorMax = new Vector2(0.5f, 0.5f);
            sampleCrossArmVertical.pivot = new Vector2(0.5f, 0.5f);
            sampleCrossArmVertical.anchoredPosition = Vector2.zero;
            sampleCrossArmVertical.sizeDelta = new Vector2(thick, half * 2f);
            var img = sampleCrossArmVertical.GetComponent<Image>();
            if (img != null)
                img.color = tint;
        }
    }

    void OnGateButtonClicked()
    {
        if (!IsDetectionPassed())
            return;
        if (hintPanel == null || _hintPanelRt == null)
            return;

        hintPanel.SetActive(true);
        PositionRectAtViewportSample(_hintPanelRt, screenOffsetPixels);
        _panelOpen = true;

        if (floatingTarget != null)
            _floatBaseAnchored = floatingTarget.anchoredPosition;

        if (optionalTextGroup != null)
        {
            if (optionalRevealTextButton != null)
                optionalTextGroup.SetActive(false);
            else
                optionalTextGroup.SetActive(true);
        }
    }

    void OnRevealTextClicked()
    {
        if (!_panelOpen || optionalTextGroup == null)
            return;
        optionalTextGroup.SetActive(true);
        if (floatingTarget != null)
            _floatBaseAnchored = floatingTarget.anchoredPosition;
    }

    void Start()
    {
        if (useSamplePointPresentation && followViewportEachFrame && IsDetectionPassed() &&
            positionGateButtonAtSample && gateButtonRoot != null && gateButtonRoot.activeSelf)
            PositionRectAtViewportSample(_gateRootRt, gateScreenOffsetPixels);
    }
}
