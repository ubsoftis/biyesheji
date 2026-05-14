using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NodeCanvas.Framework;

public class ActiveHitandGone : MonoBehaviour
{
    public StenciCube cube;          // 拖你那个 StenciCube 上来

    [Header("全局黑板")]
    [Tooltip("拖场景里的 @GlobalBlackboard；CanGone 为黑板上的 bool 变量名。")]
    public GlobalBlackboard gbb;
    [Tooltip("黑板里 bool 变量的名字，默认 CanGone。")]
    public string canGoneVariableName = "CanGone";

    public GameObject[] toActivate;  // 要激活的物体
    public GameObject[] toDeactivate; // 要关闭的物体
    /// <summary>额外要激活的一个物体（与 toActivate 数组并行）。</summary>
    public GameObject toActivateExtra;
    public bool onlyOnce = true;     // 只执行一次

    bool _done = false;

    void Update()
    {
        if (_done && onlyOnce) return;
        if (cube == null) return;

        bool canGone = gbb != null && gbb.GetVariableValue<bool>(canGoneVariableName);

        // 当 hitAndGone 且黑板 CanGone 为 true 时激活物体
        if (cube.hitAndGone && canGone)
        {
            foreach (var go in toActivate)
            {
                if (go != null) go.SetActive(true);
            }
            if (toActivateExtra != null)
                toActivateExtra.SetActive(true);
            foreach (var go in toDeactivate)
            {
                if (go != null) go.SetActive(false);
            }
            _done = true;
        }
    }
}
