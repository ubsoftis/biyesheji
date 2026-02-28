using UnityEngine;

/// <summary>
/// 根据多个魔方面/小方块的朝向，控制一组“静止目标物体”是否可被点击。
/// 物体本身不移动，只是当对应的面转到正面对摄像机（可通过 stencil 显现）时，
/// 才启用它们的 Collider2D，让 2D 射线脚本可以点到。
/// 
/// 所有需要在不同关卡里配置的内容（目标物体、需要通过的面、需要几块面同时满足）
/// 全部通过 Inspector public 出来，每个关卡可以有不同的配置。
/// </summary>
public class StencilMultiFaceClickController : MonoBehaviour
{
    [System.Serializable]
    public class TargetEntry
    {
        [Tooltip("静止的、需要通过 stencil 显现并可被点击的物体")]
        public GameObject targetObject;

        [Tooltip("控制该物体的所有魔方面/小方块（会随魔方旋转的 Transform，例如某些 Coller_ 节点或 Pivot_）")]
        public Transform[] facePivots;

        [Range(0.5f, 1f)]
        [Tooltip("一块面被认为“正对摄像机”的点积阈值")]
        public float visibleThreshold = 0.9f;

        [Min(1)]
        [Tooltip("至少需要多少块面同时满足朝向条件：1=任意一个面满足即可，2=至少两块面同时满足，以此类推")]
        public int requiredVisibleCount = 1;
    }

    [Tooltip("用来判断朝向的摄像机；为空则自动 Camera.main")]
    public Camera referenceCamera;

    [Tooltip("可在 Inspector 中为每个关卡单独配置的“目标物体 + 控制它的面”列表")]
    public TargetEntry[] targets;

    [Tooltip("调试：是否定期打印每个条目的面朝向 dot 值")]
    public bool debugLog = false;

    void Awake()
    {
        if (referenceCamera == null)
            referenceCamera = Camera.main;
    }

    void Update()
    {
        if (referenceCamera == null || targets == null || targets.Length == 0)
            return;

        foreach (var entry in targets)
        {
            if (entry == null || entry.targetObject == null || entry.facePivots == null || entry.facePivots.Length == 0)
                continue;

            var col = entry.targetObject.GetComponent<Collider2D>();
            if (col == null)
                continue;

            int visibleCount = 0;

            foreach (var pivot in entry.facePivots)
            {
                if (pivot == null)
                    continue;

                Vector3 toCamera = (referenceCamera.transform.position - pivot.position).normalized;
                Vector3 normal = pivot.forward.normalized; // 如与你建模不一致，可改为 pivot.up / -pivot.forward 等
                float dot = Vector3.Dot(normal, toCamera);

                if (dot >= entry.visibleThreshold)
                    visibleCount++;

                if (debugLog && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[StencilMultiFaceClickController] Target={entry.targetObject.name}, Face={pivot.name}, dot={dot:F3}, threshold={entry.visibleThreshold}");
                }
            }

            bool canClick = visibleCount >= Mathf.Max(1, entry.requiredVisibleCount);
            col.enabled = canClick;
        }
    }
}

