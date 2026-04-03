using UnityEngine;
using UnityEngine.UI;
using NodeCanvas.Framework;

/// <summary>纸条 1.4 与 StencilCubePlant 的衔接方式（单点与 9 点在三层 RT 下含义不同，勿混用）。</summary>
public enum Paper14StencilMode
{
    [Tooltip("要求 singleSampleVisible=false。三层 RT 下表示「单点处前层为空」；与 anyOf9Visible 无关。")]
    SingleSampleNotVisible = 0,
    [Tooltip("要求 anyOf9Visible=false（九宫格在 RtBack 上都不采到目标色）。适合「拼图里已经没有绿块」但单点仍可能为 true 的情况。")]
    NineSamplesNoTargetColor = 1,
}

public class activePaper1_4 : MonoBehaviour
{
    [Header("条件2：听力增强按钮（Inspector 拖入 Button，点击一次后满足）")]
    public Button hearingBoostButton;

    [Header("联动条件来源")]
    [Tooltip("读取 hasShrimpChild，本关要求为 false")]
    public FishChildrenCheck fishChildrenCheck;

    [Tooltip("读取 StencilCubePlant；具体用 single 还是 9 点由下方「Stencil 判定模式」决定")]
    public StencilCubePlant stencilCubePlant;

    [Tooltip("单点不可见 vs 九宫无目标色（三层 RT 下二者可能一真一假）。九宫「已无绿块」但单点仍为 true 时请选 NineSamplesNoTargetColor。")]
    public Paper14StencilMode stencilMode = Paper14StencilMode.SingleSampleNotVisible;

    [Header("满足条件后执行（2 个 Active，1 个 Deactive）")]
    public GameObject toActivate;
    public GameObject toActivate2;
    public GameObject toDeactivate;

    [Header("输出：条件是否满足（bool）")]
    [Tooltip("当前帧实时满足所有条件（未锁存）。")]
    public bool liveConditionsMet;
    [Tooltip("对话用：实时满足，或本段已触发过一次。")]
    public bool conditionsMet;

    [Tooltip("只执行一次（触发后不再重复触发）")]
    public bool triggerOnce = true;

    [Header("调试：子条件（运行时在 Play 下刷新；全为√时 liveConditionsMet 才为 true）")]
    [Tooltip("FishChildrenCheck、StencilCubePlant 均已赋值")]
    public bool debugRefsOk;
    [Tooltip("要求无虾：hasShrimpChild 为 false")]
    public bool debugNoShrimpOk;
    [Tooltip("当前 Stencil 模式下，这一关要求的「魔方/蒙版」子条件是否已满足")]
    public bool debugStencilOk;
    [Tooltip("听力增强按钮已点击过一次")]
    public bool debugHearingBoostClickedOk;

    [Header("调试：原始值（便于对照）")]
    [Tooltip("FishChildrenCheck.hasShrimpChild")]
    public bool debugRawHasShrimpChild;
    [Tooltip("StencilCubePlant.singleSampleVisible（单点；与 9 点不是一回事）")]
    public bool debugRawSingleSampleVisible;
    [Tooltip("StencilCubePlant.anyOf9Visible")]
    public bool debugRawAnyOf9Visible;

    [Header("调试：本组件状态（若为 false 则 Update 不执行，上面分项不会刷新）")]
    public bool debugGameObjectActive;
    public bool debugBehaviourEnabled;

    bool _triggered;
    bool _hearingBoostClickedOnce;
    bool _buttonListenerRegistered;

    void OnEnable()
    {
        RegisterHearingBoostListener();
    }

    void OnDisable()
    {
        UnregisterHearingBoostListener();
    }

    void Update()
    {
        debugGameObjectActive = gameObject.activeInHierarchy;
        debugBehaviourEnabled = enabled;

        bool hasRefs = fishChildrenCheck != null && stencilCubePlant != null;
        debugRefsOk = hasRefs;
        bool stencilOk = false;
        if (hasRefs)
        {
            debugRawHasShrimpChild = fishChildrenCheck.hasShrimpChild;
            debugRawSingleSampleVisible = stencilCubePlant.singleSampleVisible;
            debugRawAnyOf9Visible = stencilCubePlant.anyOf9Visible;
            debugNoShrimpOk = !fishChildrenCheck.hasShrimpChild;
            stencilOk = stencilMode == Paper14StencilMode.SingleSampleNotVisible
                ? !stencilCubePlant.singleSampleVisible
                : !stencilCubePlant.anyOf9Visible;
            debugStencilOk = stencilOk;
        }
        else
        {
            debugRawHasShrimpChild = false;
            debugRawSingleSampleVisible = false;
            debugRawAnyOf9Visible = false;
            debugNoShrimpOk = false;
            debugStencilOk = false;
        }
        debugHearingBoostClickedOk = _hearingBoostClickedOnce;

        liveConditionsMet =
            hasRefs &&
            !fishChildrenCheck.hasShrimpChild &&
            stencilOk &&
            _hearingBoostClickedOnce;
        conditionsMet = _triggered || liveConditionsMet;

        // 仅在本脚本启用且需要判定 hitAndGone 时接管；避免被 1.3 覆盖的条件在下一帧被清掉
        if (stencilCubePlant != null && isActiveAndEnabled)
            stencilCubePlant.enableHitAndGoneCheck = liveConditionsMet;

        if (_triggered && triggerOnce) return;
        if (!hasRefs) return;

        if (liveConditionsMet)
        {
            if (toActivate != null) toActivate.SetActive(true);
            if (toActivate2 != null) toActivate2.SetActive(true);
            if (toDeactivate != null) toDeactivate.SetActive(false);
            _triggered = true;
        }
        else
        {
            if (!triggerOnce) _triggered = false;
        }
    }

    void RegisterHearingBoostListener()
    {
        if (hearingBoostButton == null || _buttonListenerRegistered) return;
        hearingBoostButton.onClick.AddListener(OnHearingBoostClicked);
        _buttonListenerRegistered = true;
    }

    void UnregisterHearingBoostListener()
    {
        if (hearingBoostButton == null || !_buttonListenerRegistered) return;
        hearingBoostButton.onClick.RemoveListener(OnHearingBoostClicked);
        _buttonListenerRegistered = false;
    }

    void OnHearingBoostClicked()
    {
        _hearingBoostClickedOnce = true;
    }
}
