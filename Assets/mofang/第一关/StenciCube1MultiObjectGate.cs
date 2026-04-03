using UnityEngine;

/// <summary>
/// 专门负责 StenciCube1 的「自动控制多个物体」：根据 isVisible 对 objectsToActivate / objectsToDeactivate 做批量 SetActive。
/// 目的：让 StenciCube1 只做取样与判定；避免 storyGate 误影响其它逻辑。
/// </summary>
public class StenciCube1MultiObjectGate : MonoBehaviour
{
    [Tooltip("引用要取判定值的 StenciCube1（通常同一个物体上可自动获取）。")]
    public StenciCube1 sourceCube;

    [Header("触发显隐列表（按 allTargetsVisible 驱动）")]
    [Tooltip("allTargetsVisible=true 时：这些物体 SetActive(true)")]
    public GameObject[] objectsToActivate;

    [Tooltip("allTargetsVisible=true 时：这些物体 SetActive(false)；allTargetsVisible=false 时反向 SetActive(true)（受 Restore Deactivated 影响）")]
    public GameObject[] objectsToDeactivate;

    [Tooltip("当 allTargetsVisible=false 时：是否把 objectsToDeactivate 置为 SetActive(true)（实现反向）")]
    public bool restoreDeactivatedWhenNotAllVisible = true;

    bool _lastVisible;
    bool _hasAppliedOnce;

    void Awake()
    {
        if (sourceCube == null)
            sourceCube = GetComponent<StenciCube1>();
    }

    void Update()
    {
        if (sourceCube == null)
            return;

        // 按用户要求：只根据 allTargetsVisible 状态驱动批量显隐
        bool visible = sourceCube.allTargetsVisible;

        // 只有在可见状态变化时才更新，减少频繁 SetActive 带来的干扰
        if (!_hasAppliedOnce || visible != _lastVisible)
        {
            Apply(visible);
            _lastVisible = visible;
            _hasAppliedOnce = true;
        }
    }

    void Apply(bool visible)
    {
        // 兼容迁移：如果新脚本列表没填，就回退使用 sourceCube 上隐藏的列表
        GameObject[] activateList = (objectsToActivate != null && objectsToActivate.Length > 0) ? objectsToActivate : sourceCube.objectsToActivate;
        GameObject[] deactivateList = (objectsToDeactivate != null && objectsToDeactivate.Length > 0) ? objectsToDeactivate : sourceCube.objectsToDeactivate;
        bool restoreFlag = restoreDeactivatedWhenNotAllVisible;

        if (activateList != null)
        {
            for (int i = 0; i < activateList.Length; i++)
            {
                GameObject go = activateList[i];
                if (go != null) go.SetActive(visible);
            }
        }

        if (deactivateList != null)
        {
            for (int i = 0; i < deactivateList.Length; i++)
            {
                GameObject go = deactivateList[i];
                if (go == null) continue;

                // true：关闭 objectsToDeactivate；false：按恢复开关反向开启
                if (visible)
                    go.SetActive(false);
                else
                    go.SetActive(restoreFlag);
            }
        }
    }
}

