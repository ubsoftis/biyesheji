using UnityEngine;

using NodeCanvas.Framework;

public class activePaper1_2 : MonoBehaviour

{

    private const string TriggerKey = "纸条1.2可以触发";

    public GlobalBlackboard gbb;
    private bool _hasTriggered;
    bool _toActivate2Fired;
  
    [Header("剧情可触发开关")]

    public bool canTrigger;


    [Header("条件：这两个物体都必须是 Active")]

    public GameObject conditionA;

    public GameObject conditionB;

    [Header("输出：条件是否满足（bool）")]
    [Tooltip("当且仅当 conditionA 与 conditionB 都处于 ActiveInHierarchy 时为 true（不含“已触发过”锁存）。")]
    public bool liveConditionsMet;

    [Tooltip("对话分支用：实时条件满足，或纸条流程已触发过一次（避免触发后关掉 condition 物体导致读回 false）。")]
    public bool conditionsMet;

    [Header("满足条件后：要 SetActive(true)")]

    public GameObject toActivate;
    public GameObject toActivate2;
    public GameObject toActivate3;
    public GameObject toDeactivate;

    [Tooltip("只执行一次（推荐）")]
    public bool triggerOnce = true;

    void Start()

    {

        SyncFromBlackboard();

    }



    void Update()

    {

        SyncFromBlackboard();
      
        bool hasConditions = conditionA != null && conditionB != null;

        liveConditionsMet = hasConditions && conditionA.activeInHierarchy && conditionB.activeInHierarchy;

        conditionsMet = _hasTriggered || liveConditionsMet;

        // 对话提示 UI：仅第一次满足 A/B 时打开一次，不随实时条件反复关开

        if (liveConditionsMet && !_toActivate2Fired)

        {

            if (toActivate2 != null) toActivate2.SetActive(true);

            if (toActivate3 != null) toActivate3.SetActive(true);

            if (toActivate2 != null || toActivate3 != null) _toActivate2Fired = true;

        }



        if (triggerOnce && _hasTriggered) return;

        if (!hasConditions) return;

        if (canTrigger && liveConditionsMet)

        {

            ActivateIfAssigned(toActivate);
             ActivateIfAssigned(toActivate2);

            
            if (toDeactivate != null) toDeactivate.SetActive(false);

    

            _hasTriggered = true;

        }            

        else

        {

            if (!triggerOnce)

            {

                canTrigger = false;

                SyncToBlackboard();

            }
       }

    }



    void SyncFromBlackboard()

    {

        if (gbb == null) return;

        canTrigger = gbb.GetVariableValue<bool>(TriggerKey);

    }



    void SyncToBlackboard()

    {

        if (gbb == null) return;

        gbb.SetVariableValue(TriggerKey, canTrigger);

    }



    static void ActivateIfAssigned(GameObject go)

    {

        if (go != null) go.SetActive(true);

    }

}

