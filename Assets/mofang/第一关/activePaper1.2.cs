using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class activePaper1_2 : MonoBehaviour
{
    [Header("条件：这两个物体都必须是 Active")]
    public GameObject conditionA;
    public GameObject conditionB;

    [Header("输出：条件是否满足（bool）")]
    [Tooltip("当且仅当 conditionA 与 conditionB 都处于 ActiveInHierarchy 时为 true。")]
    public bool conditionsMet;

    [Header("满足条件后执行")]
    public GameObject toActivate;
     public GameObject toActivate2;
    public GameObject toDeactivate;

    [Tooltip("只执行一次（推荐）")]
    public bool triggerOnce = true;

    bool _triggered;

    void Update()
    {
        bool hasConditions = conditionA != null && conditionB != null;
        conditionsMet = hasConditions && conditionA.activeInHierarchy && conditionB.activeInHierarchy;

        if (_triggered && triggerOnce) return;
        if (!hasConditions) return;
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
}