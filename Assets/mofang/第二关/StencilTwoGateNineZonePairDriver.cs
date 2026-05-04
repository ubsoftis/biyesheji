using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 两段式驱动（与 <see cref="StencilActivateTwoOnClick"/> 同套 RT + 颜色判定）：
/// <b>第一段</b>：两个视口采样点门控成立后，须在门控成立时成功 <see cref="OnStencilClick"/> 一次，才允许显示第一段两物体。
/// 默认 <see cref="gateDrivePairStayActiveAfterFirstShow"/>：仅在首次「该显示」时 Active 一次，本脚本之后不会再设为 Inactive。
/// <b>第二段</b>：在「九区条件」（固定：<b>9 个点都不可见</b>，即 <c>nineZoneVisibleCount == 0</c>）与「联动」都满足时，且（可选）门控仍成立 →
/// 再将 <see cref="nineLinkDriveObject1"/>、<see cref="nineLinkDriveObject2"/> 设为 Active；否则关闭这一对。
/// 联动背包消耗品：拖入挂在 <see cref="InventoryManager"/> 上的 <see cref="InventoryPrerequisiteTracker"/>；留空则不限制此项。
/// 本组件 <see cref="DefaultExecutionOrder"/> 为 500：晚于默认 0 的魔方/动画，门控 RT 更贴近当前帧姿态；晚于 <see cref="InventoryPrerequisiteTracker"/>（-50）以便读到 <c>IsSatisfied</c>。
/// 九区可见数在 <c>LateUpdate</c> 内强制刷新 RT 后每帧重算，与第二段判定一致。
/// </summary>
[DefaultExecutionOrder(500)]
public class StencilTwoGateNineZonePairDriver : MonoBehaviour, IStencilClickable
{
    [Header("状态（只读）")]
    public bool clickGateVisible;
    public bool gateSample1Visible, gateSample2Visible;
    [Tooltip("每帧 LateUpdate 在 RT 刷新后重算；运行中在 Inspector 可见当前可见采样点数。")]
    public int nineZoneVisibleCount;
    [Tooltip("拖入 inventoryConsumablePrerequisiteTracker 时等于其 IsSatisfied；未拖入则为 true（只读）。")]
    public bool prerequisiteMet;
    [Tooltip("九区条件 + 联动 +（可选）门控 全部满足时为 true。")]
    public bool secondStageActive;
    [Tooltip("已在门控成立时收到过至少一次点击，第一段两物体才允许出现。")]
    public bool gateDrivePairUnlockedByClick;

    [Header("模板测试（三层 RT）")]
    public bool useThreeRTForVisibility = true;
    public CubeLayerRTPicker rtPicker;
    public bool leftmostDebugPreviewIsFrontLayer = true;

    [Header("第一段：2 个采样点门控 → 激活 2 个物体")]
    public Vector2 gateSampleVp1 = new Vector2(0.45f, 0.9f);
    public Vector2 gateSampleVp2 = new Vector2(0.55f, 0.9f);
    public bool gateRequireBothSamples = true;
    public GameObject gateDriveObject1;
    public GameObject gateDriveObject2;
    [Tooltip("为 true：门控成立后不会立刻显示第一段两物体，须点击（OnStencilClick）一次后才 Active。")]
    public bool requireClickToShowGateDrivePair = true;
    [Tooltip("在上一项为 true 时：为 true 则点到一次后第一段两物体保持 Active，即使门控随后变 false。")]
    public bool latchGateDrivePairAfterClick = true;
    [Tooltip(
        "为 true：第一段两物体仅在「从不显示变为显示」时由本脚本 Active 一次，之后不再改为 Inactive（避免门控抖动反复 SetActive）。\n" +
        "为 false：显示意图随条件变化，但仅在布尔值相对上一帧变化时 SetActive（与第二段沿触发一致）。"
    )]
    public bool gateDrivePairStayActiveAfterFirstShow = true;

    [Header("第二段：9 采样点 + 联动 → 再激活 2 个物体")]
    public Vector2[] nineZoneViewports = new Vector2[9];
    [Tooltip("为 true：第二段也要求第一段门控仍成立；为 false：仅看九区+联动。")]
    public bool secondStageAlsoRequiresGate = true;
    public GameObject nineLinkDriveObject1;
    public GameObject nineLinkDriveObject2;

    [Header("联动：背包消耗品")]
    [Tooltip("挂在 InventoryManager 同一物体上的 InventoryPrerequisiteTracker；留空则不检查背包消耗条件。")]
    public InventoryPrerequisiteTracker inventoryConsumablePrerequisiteTracker;

    [Header("判定颜色")]
    public Color targetColor = Color.red;
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;
    public float blackRgbThreshold = 0.02f;
    public float blackAlphaThreshold = 0.01f;
    public bool invertIsVisible;

    [Header("点击 Collider（可选）")]
    public Collider2D gateCollider2D;
    public bool disableColliderWhenGateClosed = true;

    [Header("点击回调")]
    public UnityEvent onStencilClickWhenGateOpen;

    [Header("调试")]
    public bool debugLog;

    Texture2D _readTex;
    bool _gateDrivePairShowInitialized;
    bool _gateDrivePairPrevShowFirstPair;
    bool _nineLinkSecondStageInitialized;
    bool _nineLinkPrevSecondStageActive;

    void Awake()
    {
        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        if (nineZoneViewports == null || nineZoneViewports.Length != 9)
            nineZoneViewports = BuildDefaultNineGrid();
    }

    static Vector2[] BuildDefaultNineGrid()
    {
        var v = new Vector2[9];
        int k = 0;
        for (int gy = 0; gy < 3; gy++)
        for (int gx = 0; gx < 3; gx++)
        {
            float x = 0.35f + gx * 0.1f;
            float y = 0.25f + gy * 0.15f;
            v[k++] = new Vector2(x, y);
        }
        return v;
    }

    void Update()
    {
        if (useThreeRTForVisibility)
            ComputeGateFromTwoSamples();
        else
        {
            gateSample1Visible = gateSample2Visible = true;
            clickGateVisible = true;
        }

        if (requireClickToShowGateDrivePair && !clickGateVisible && !latchGateDrivePairAfterClick)
            gateDrivePairUnlockedByClick = false;

        bool showFirstPair;
        if (!requireClickToShowGateDrivePair)
            showFirstPair = clickGateVisible;
        else if (latchGateDrivePairAfterClick)
            showFirstPair = gateDrivePairUnlockedByClick;
        else
            showFirstPair = clickGateVisible && gateDrivePairUnlockedByClick;

        ApplyGateDrivePairOnShowEdge(showFirstPair);

        if (disableColliderWhenGateClosed && gateCollider2D != null)
            gateCollider2D.enabled = clickGateVisible;
    }

    void LateUpdate()
    {
        if (useThreeRTForVisibility)
        {
            if (rtPicker == null)
                rtPicker = FindObjectOfType<CubeLayerRTPicker>();
            if (rtPicker != null)
            {
                // 与 Update 里门控那次 Render 可能早于其它物体位移；九区计数放在帧末并强制再渲一次，保证与画面同步。
                rtPicker.InvalidateRTRenderFrameCache();
                nineZoneVisibleCount = CountNineZoneVisible();
            }
            else
                nineZoneVisibleCount = 0;
        }
        else
            nineZoneVisibleCount = -1;

        prerequisiteMet = CheckPrerequisiteMet();
        ApplySecondStagePairFromCurrentPrerequisite();
    }

    void ApplySecondStagePairFromCurrentPrerequisite()
    {
        bool nineOk = !useThreeRTForVisibility || NineZoneConditionMet();
        bool gateOkForSecond = !secondStageAlsoRequiresGate
                               || (clickGateVisible && (!requireClickToShowGateDrivePair || gateDrivePairUnlockedByClick));
        secondStageActive = gateOkForSecond && nineOk && prerequisiteMet;
        ApplyNineLinkDrivePairOnSecondStageEdge(secondStageActive);
    }

    static void SetPairActive(GameObject a, GameObject b, bool on)
    {
        if (a != null && a.activeSelf != on)
            a.SetActive(on);
        if (b != null && b.activeSelf != on)
            b.SetActive(on);
    }

    /// <summary>
    /// 第一段：<paramref name="showFirstPairNow"/> 仅在其相对上一帧变化时驱动 SetActive；
    /// 若 <see cref="gateDrivePairStayActiveAfterFirstShow"/> 为 true，则只做上升沿 Active，永不因本脚本再设为 Inactive。
    /// </summary>
    void ApplyGateDrivePairOnShowEdge(bool showFirstPairNow)
    {
        if (gateDrivePairStayActiveAfterFirstShow)
        {
            if (!_gateDrivePairShowInitialized)
            {
                if (showFirstPairNow)
                    SetPairActive(gateDriveObject1, gateDriveObject2, true);
                _gateDrivePairPrevShowFirstPair = showFirstPairNow;
                _gateDrivePairShowInitialized = true;
                return;
            }

            if (showFirstPairNow && !_gateDrivePairPrevShowFirstPair)
                SetPairActive(gateDriveObject1, gateDriveObject2, true);

            _gateDrivePairPrevShowFirstPair = showFirstPairNow;
            return;
        }

        if (!_gateDrivePairShowInitialized)
        {
            SetPairActive(gateDriveObject1, gateDriveObject2, showFirstPairNow);
            _gateDrivePairPrevShowFirstPair = showFirstPairNow;
            _gateDrivePairShowInitialized = true;
            return;
        }

        if (showFirstPairNow == _gateDrivePairPrevShowFirstPair)
            return;

        SetPairActive(gateDriveObject1, gateDriveObject2, showFirstPairNow);
        _gateDrivePairPrevShowFirstPair = showFirstPairNow;
    }

    /// <summary>
    /// 第二段两个物体仅在 <paramref name="secondStageActiveNow"/> 相对上一帧发生变化时改 Active，
    /// 避免条件持续不满足时每帧强制 SetActive(false)；关闭发生在「满足 → 不满足」这一帧（下降沿）。
    /// </summary>
    void ApplyNineLinkDrivePairOnSecondStageEdge(bool secondStageActiveNow)
    {
        if (!_nineLinkSecondStageInitialized)
        {
            SetPairActive(nineLinkDriveObject1, nineLinkDriveObject2, secondStageActiveNow);
            _nineLinkPrevSecondStageActive = secondStageActiveNow;
            _nineLinkSecondStageInitialized = true;
            return;
        }

        if (secondStageActiveNow == _nineLinkPrevSecondStageActive)
            return;

        SetPairActive(nineLinkDriveObject1, nineLinkDriveObject2, secondStageActiveNow);
        _nineLinkPrevSecondStageActive = secondStageActiveNow;
    }

    bool NineZoneConditionMet() => nineZoneVisibleCount == 0;

    bool CheckPrerequisiteMet()
    {
        if (inventoryConsumablePrerequisiteTracker != null)
            return inventoryConsumablePrerequisiteTracker.IsSatisfied;
        return true;
    }

    public void OnStencilClick()
    {
        if (useThreeRTForVisibility)
        {
            ComputeGateFromTwoSamples();
            if (!clickGateVisible)
            {
                if (debugLog)
                    Debug.Log($"[StencilTwoGateNineZonePairDriver] 点击被门控拦截 gate1={gateSample1Visible} gate2={gateSample2Visible}");
                return;
            }
        }

        if (requireClickToShowGateDrivePair && !gateDrivePairUnlockedByClick)
        {
            gateDrivePairUnlockedByClick = true;
            if (debugLog)
                Debug.Log("[StencilTwoGateNineZonePairDriver] 首次点击：解锁第一段两物体显示。");
        }

        onStencilClickWhenGateOpen?.Invoke();
    }

    void ComputeGateFromTwoSamples()
    {
        if (rtPicker == null)
            rtPicker = FindObjectOfType<CubeLayerRTPicker>();
        if (rtPicker == null)
        {
            gateSample1Visible = gateSample2Visible = true;
            clickGateVisible = true;
            return;
        }

        rtPicker.EnsureRTRenderedForSampling();
        var rtBack = rtPicker.RtBack;
        var rtMid = rtPicker.RtMid;
        var rtFront = rtPicker.RtFront;
        if (rtBack == null || rtMid == null || rtFront == null)
        {
            gateSample1Visible = gateSample2Visible = true;
            clickGateVisible = true;
            return;
        }

        if (leftmostDebugPreviewIsFrontLayer)
        {
            var tmp = rtBack;
            rtBack = rtFront;
            rtFront = tmp;
        }

        bool v1 = SampleIsVisibleByThreeRT(gateSampleVp1, rtBack, rtMid, rtFront);
        bool v2 = SampleIsVisibleByThreeRT(gateSampleVp2, rtBack, rtMid, rtFront);
        gateSample1Visible = v1;
        gateSample2Visible = v2;
        clickGateVisible = gateRequireBothSamples ? (v1 && v2) : (v1 || v2);
    }

    int CountNineZoneVisible()
    {
        if (nineZoneViewports == null || nineZoneViewports.Length != 9)
            return 0;

        if (rtPicker == null)
            rtPicker = FindObjectOfType<CubeLayerRTPicker>();
        if (rtPicker == null)
            return 0;

        rtPicker.EnsureRTRenderedForSampling();
        var rtBack = rtPicker.RtBack;
        var rtMid = rtPicker.RtMid;
        var rtFront = rtPicker.RtFront;
        if (rtBack == null || rtMid == null || rtFront == null)
            return 0;

        if (leftmostDebugPreviewIsFrontLayer)
        {
            var tmp = rtBack;
            rtBack = rtFront;
            rtFront = tmp;
        }

        int n = 0;
        for (int i = 0; i < 9; i++)
        {
            if (SampleIsVisibleByThreeRT(nineZoneViewports[i], rtBack, rtMid, rtFront))
                n++;
        }
        return n;
    }

    bool SampleIsVisibleByThreeRT(Vector2 vp01, RenderTexture rtBack, RenderTexture rtMid, RenderTexture rtFront)
    {
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return false;

        int w = rtFront.width;
        int h = rtFront.height;
        int px = Mathf.Clamp(Mathf.RoundToInt(vp01.x * w), 0, w - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(vp01.y * h), 0, h - 1);

        Color cFront = SampleRT(rtFront, px, py);
        bool frontEmpty = IsBlackEmpty(cFront);
        bool colorHit = ColorsClose(cFront, targetColor, colorTolerance);
        bool visibleRt = !frontEmpty && colorHit;
        return invertIsVisible ? !visibleRt : visibleRt;
    }

    Color SampleRT(RenderTexture rt, int px, int py)
    {
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
}
