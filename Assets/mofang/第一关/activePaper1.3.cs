using UnityEngine;
using NodeCanvas.Framework;

public class activePaper1_3 : MonoBehaviour
{
    private const string TriggerKey = "童话世界30s倒计时可以触发";
    private const string HearingBoostTriggerKey = "夜行世界听力增强可以触发";

    [Header("全局黑板")]
    public GlobalBlackboard gbb;

    [Header("童话世界第五面：30s倒计时脚本（受 TriggerKey 控制）")]
    public Countdown30s countdown30sScript;
    public bool canTriggerCountdown;

    [Header("夜行世界第一面：听力增强脚本（受 HearingBoostTriggerKey 控制）")]
    public NightWorldHearingBoostGate hearingBoostScript;
    public bool canTriggerHearingBoost;

    [Header("联动条件来源")]
    [Tooltip("读取 lessOrEqualHalf 与 hasShrimpChild")]
    public FishChildrenCheck fishChildrenCheck;

    [Tooltip("读取 allTargetsVisible（2 点同时出现）")]
    public StenciCube2 stenciCube2;

    [Tooltip("读取 singleSampleVisible，并在条件满足时开启它的 hitAndGone 判断门控")]
    public StencilCubePlant stencilCubePlant;

    [Header("满足条件后执行（2 个 Active，1 个 Deactive）")]
    public GameObject toActivate;
    public GameObject toActivate2;
    public GameObject toDeactivate;

    [Header("输出：条件是否满足（bool）")]
    [Tooltip("当所有联动条件都满足时为 true。")]
    public bool conditionsMet;

    [Tooltip("只执行一次（触发后不再重复触发）")]
    public bool triggerOnce = true;

    bool _triggered;

    void Update()
    {
        SyncTriggerStatesFromBlackboard();
        if (countdown30sScript != null) countdown30sScript.enabled = canTriggerCountdown;
        if (hearingBoostScript != null) hearingBoostScript.enabled = canTriggerHearingBoost;

        bool hasRefs = fishChildrenCheck != null && stenciCube2 != null && stencilCubePlant != null;
        conditionsMet =
            hasRefs &&
            fishChildrenCheck.lessOrEqualHalf &&
            fishChildrenCheck.hasShrimpChild &&
            stenciCube2.allTargetsVisible &&
            stencilCubePlant.singleSampleVisible;

        // 只有在条件满足时，才允许 Plant 开启 hitAndGone 判断
        if (stencilCubePlant != null)
            stencilCubePlant.enableHitAndGoneCheck = conditionsMet;

        if (_triggered && triggerOnce) return;
        if (!hasRefs) return;

        if (conditionsMet)
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

    void SyncTriggerStatesFromBlackboard()
    {
        if (gbb == null) return;
        canTriggerCountdown = gbb.GetVariableValue<bool>(TriggerKey);
        canTriggerHearingBoost = gbb.GetVariableValue<bool>(HearingBoostTriggerKey);
    }
}
