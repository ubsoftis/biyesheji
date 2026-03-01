using UnityEngine;

/// <summary>
/// 根据多个魔方面/小方块的朝向，控制一组“静止目标物体”是否可被点击。
/// 物体本身不移动，只是当对应的面转到正面对摄像机（可通过 stencil 显现）时，
/// 才启用它们的 Collider2D，让 2D 射线脚本可以点到。
///
/// 支持两种模式：整面（Coller）或物体前方方块（facePivots）。方块模式可检测「顶面」（开始向上的面）是否转到正对摄像机。
/// </summary>
[DefaultExecutionOrder(-100)] // 先于射线检测执行，确保点击时 Collider 状态已更新
public class StencilMultiFaceClickController : MonoBehaviour
{
    [Header("碰撞体引用（与 Mofan Key Rotation 一致）")]
    [Tooltip("上")]
    public Transform Coller_up;
    [Tooltip("下")]
    public Transform Coller_down;
    [Tooltip("左")]
    public Transform Coller_left;
    [Tooltip("右")]
    public Transform Coller_right;
    [Tooltip("前")]
    public Transform Coller_front;
    [Tooltip("后")]
    public Transform Coller_back;

    [System.Serializable]
    public class TargetEntry
    {
        [Tooltip("静止的、需要通过 stencil 显现并可被点击的物体")]
        public GameObject targetObject;

        [Tooltip("需要满足朝向的整面（勾选即表示该面正对摄像机时计入可点击条件）")]
        public bool useUp, useDown, useLeft, useRight, useFront, useBack;

        [Tooltip("可选：物体前方方块的 Pivot（如 Pivot_立方体.024）。需全部满足时 requiredVisibleCount 填数量")]
        public Transform[] facePivots;

        public enum FaceAxis { Up, Down, Forward, Back, Right, Left, Any }
        [Tooltip("检测哪个轴/面：Up=顶面, Forward=正面, Any=六面取最大；根据模型轴向在 Scene 中观察 Gizmo 后选择")]
        public FaceAxis facePivotsAxis = FaceAxis.Any;

        [Tooltip("若 dot 为负时物体已可见，勾选此项反转法线（仅当 Axis 非 Any 时生效）")]
        public bool facePivotsInvertNormal = false;

        [Range(0.5f, 1f)]
        [Tooltip("一块面被认为“正对摄像机”的点积阈值；0.8 更宽松，0.9 更严格")]
        public float visibleThreshold = 0.8f;

        [Min(1)]
        [Tooltip("至少需要多少块面同时满足：1=任意一个即可；若需「所有前方方块」都转过来，填 facePivots 的数量")]
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
            if (entry == null || entry.targetObject == null)
                continue;

            var col = entry.targetObject.GetComponent<Collider2D>();
            if (col == null)
                continue;

            int visibleCount = 0;

            // 根据勾选的面，使用对应的 Coller 引用进行朝向检测
            if (entry.useUp && Coller_up != null)
                visibleCount += CheckFaceVisible(Coller_up, entry.visibleThreshold, entry.targetObject.name, "上", TargetEntry.FaceAxis.Forward, false);
            if (entry.useDown && Coller_down != null)
                visibleCount += CheckFaceVisible(Coller_down, entry.visibleThreshold, entry.targetObject.name, "下", TargetEntry.FaceAxis.Forward, false);
            if (entry.useLeft && Coller_left != null)
                visibleCount += CheckFaceVisible(Coller_left, entry.visibleThreshold, entry.targetObject.name, "左", TargetEntry.FaceAxis.Forward, false);
            if (entry.useRight && Coller_right != null)
                visibleCount += CheckFaceVisible(Coller_right, entry.visibleThreshold, entry.targetObject.name, "右", TargetEntry.FaceAxis.Forward, false);
            if (entry.useFront && Coller_front != null)
                visibleCount += CheckFaceVisible(Coller_front, entry.visibleThreshold, entry.targetObject.name, "前", TargetEntry.FaceAxis.Forward, false);
            if (entry.useBack && Coller_back != null)
                visibleCount += CheckFaceVisible(Coller_back, entry.visibleThreshold, entry.targetObject.name, "后", TargetEntry.FaceAxis.Forward, false);

            // 物体前方方块的 Pivot
            if (entry.facePivots != null)
            {
                foreach (var pivot in entry.facePivots)
                {
                    if (pivot != null)
                    {
                        if (entry.facePivotsAxis == TargetEntry.FaceAxis.Any)
                            visibleCount += CheckAnyFaceVisible(pivot, entry.visibleThreshold, entry.targetObject.name, pivot.name);
                        else
                            visibleCount += CheckFaceVisible(pivot, entry.visibleThreshold, entry.targetObject.name, pivot.name, entry.facePivotsAxis, entry.facePivotsInvertNormal);
                    }
                }
            }

            bool canClick = visibleCount >= Mathf.Max(1, entry.requiredVisibleCount);
            col.enabled = canClick;
        }
    }

    int CheckFaceVisible(Transform pivot, float threshold, string targetName, string faceName, TargetEntry.FaceAxis axis, bool invertNormal)
    {
        Vector3 toCamera = (referenceCamera.transform.position - pivot.position).normalized;
        Vector3 normal = GetAxisNormal(pivot, axis);
        if (invertNormal) normal = -normal;
        float dot = Vector3.Dot(normal.normalized, toCamera);

        if (debugLog && Time.frameCount % 60 == 0)
        {
            string axisStr = axis.ToString() + (invertNormal ? "(反)" : "");
            Debug.Log($"[StencilMultiFaceClickController] Target={targetName}, Face={pivot.name} ({axisStr}), dot={dot:F3}, threshold={threshold}");
        }

        return dot >= threshold ? 1 : 0;
    }

    static Vector3 GetAxisNormal(Transform pivot, TargetEntry.FaceAxis axis)
    {
        switch (axis)
        {
            case TargetEntry.FaceAxis.Up:    return pivot.up;
            case TargetEntry.FaceAxis.Down:  return -pivot.up;
            case TargetEntry.FaceAxis.Forward: return pivot.forward;
            case TargetEntry.FaceAxis.Back:  return -pivot.forward;
            case TargetEntry.FaceAxis.Right: return pivot.right;
            case TargetEntry.FaceAxis.Left:  return -pivot.right;
            default: return pivot.up;
        }
    }

    /// <summary>
    /// 检测方块任意一面是否朝向摄像机（横向/纵向转动都适用）
    /// </summary>
    int CheckAnyFaceVisible(Transform pivot, float threshold, string targetName, string faceName)
    {
        Vector3 toCamera = (referenceCamera.transform.position - pivot.position).normalized;
        Vector3[] normals = { pivot.up, -pivot.up, pivot.forward, -pivot.forward, pivot.right, -pivot.right };

        float maxDot = float.NegativeInfinity;
        foreach (var n in normals)
        {
            float dot = Vector3.Dot(n.normalized, toCamera);
            if (dot > maxDot) maxDot = dot;
        }

        if (debugLog && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[StencilMultiFaceClickController] Target={targetName}, Face={pivot.name} (任意面), maxDot={maxDot:F3}, threshold={threshold}");
        }

        return maxDot >= threshold ? 1 : 0;
    }
}

