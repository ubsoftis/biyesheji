using UnityEngine;
using UnityEngine.UI;
using NodeCanvas.Framework;

public class activePaper1_4 : MonoBehaviour
{
    [Header("条件2：听力增强按钮（Inspector 拖入 Button，点击一次后满足）")]
    public Button hearingBoostButton;

    [Header("联动条件来源")]
    [Tooltip("读取 hasShrimpChild，本关要求为 false")]
    public FishChildrenCheck fishChildrenCheck;

    [Tooltip("读取 singleSampleVisible：本关要求单独采样点为不可见")]
    public StencilCubePlant stencilCubePlant;

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
        bool hasRefs = fishChildrenCheck != null && stencilCubePlant != null;
        liveConditionsMet =
            hasRefs &&
            !fishChildrenCheck.hasShrimpChild &&
            !stencilCubePlant.singleSampleVisible &&
            _hearingBoostClickedOnce;
        conditionsMet = _triggered || liveConditionsMet;

        if (stencilCubePlant != null)
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
