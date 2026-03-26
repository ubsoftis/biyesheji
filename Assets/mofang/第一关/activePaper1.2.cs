using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class activePaper1_2 : MonoBehaviour
{
    [Header("条件：这两个物体都必须是 Active")]
    public GameObject conditionA;
    public GameObject conditionB;

    [Header("满足条件后执行")]
    public GameObject toActivate;
    public GameObject toDeactivate;

    [Tooltip("只执行一次（推荐）")]
    public bool triggerOnce = true;

    bool _triggered;

    void Update()
    {
        if (_triggered && triggerOnce) return;
        if (conditionA == null || conditionB == null) return;
        if (conditionA.activeInHierarchy && conditionB.activeInHierarchy)
        {
            if (toActivate != null) toActivate.SetActive(true);
            if (toDeactivate != null) toDeactivate.SetActive(false);
            _triggered = true;
        }
        else
        {
            if (!triggerOnce) _triggered = false;
        }
    }
}