using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveHitandGone : MonoBehaviour
{
    public StenciCube cube;          // 拖你那个 StenciCube 上来
    public GameObject[] toActivate;  // 要激活的物体
    public GameObject[] toDeactivate; // 要关闭的物体
    public bool onlyOnce = true;     // 只执行一次

    bool _done = false;

    void Update()
    {
        if (_done && onlyOnce) return;
        if (cube == null) return;

        // 当 hitAndGone 变成 true 时激活物体
        if (cube.hitAndGone)
        {
            foreach (var go in toActivate)
            {
                if (go != null) go.SetActive(true);
            }
            foreach (var go in toDeactivate)
            {
                if (go != null) go.SetActive(false);
            }
            _done = true;
        }
    }
}
