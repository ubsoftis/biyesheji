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

    [Header("联动：当 canTriggerHearingBoost=true 时，顺便打开纸条1.4脚本")]
    [Tooltip("拖入纸条1.4上的 activePaper1_4 组件（会在 canTriggerHearingBoost=true 时 enabled=true）。")]
    public activePaper1_4 paper1_4Script;

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
    [Tooltip("当前帧实时满足所有联动条件（未锁存）。")]
    public bool liveConditionsMet;
    [Tooltip("对话用：实时满足，或本段已触发过一次（避免触发后状态变化读回 false）。")]
    public bool conditionsMet;

    [Tooltip("只执行一次（触发后不再重复触发）")]
    public bool triggerOnce = true;

    bool _triggered;
    bool _toActivate2Fired;

    void Update()
    {
        SyncTriggerStatesFromBlackboard();
        if (countdown30sScript != null) countdown30sScript.enabled = canTriggerCountdown;
        if (hearingBoostScript != null) hearingBoostScript.enabled = canTriggerHearingBoost;
        if (canTriggerHearingBoost && paper1_4Script != null) paper1_4Script.enabled = true;

        bool hasRefs = fishChildrenCheck != null && stenciCube2 != null && stencilCubePlant != null;
        liveConditionsMet =
            hasRefs &&
            fishChildrenCheck.lessOrEqualHalf &&
            fishChildrenCheck.hasShrimpChild &&
            stenciCube2.allTargetsVisible &&
            stencilCubePlant.singleSampleVisible;
        conditionsMet = _triggered || liveConditionsMet;

        // 纸条 1.4 启用后由 1.4 独占 Plant 的 hitAndGone 门控，避免本脚本每帧写回 false 把 1.4 卡死
        bool paper14ControlsPlant = paper1_4Script != null && paper1_4Script.isActiveAndEnabled;
        if (stencilCubePlant != null && !paper14ControlsPlant)
            stencilCubePlant.enableHitAndGoneCheck = liveConditionsMet;

        if (_triggered && triggerOnce) return;
        if (!hasRefs) return;

        if (liveConditionsMet)
        {
            if (toActivate != null) toActivate.SetActive(true);
            if (toActivate2 != null && !_toActivate2Fired)
            {
                toActivate2.SetActive(true);
                _toActivate2Fired = true;
            }
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
