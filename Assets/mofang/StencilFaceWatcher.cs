using UnityEngine;

/// <summary>
/// 监控六个面枢轴（如 Coller_front/back/...），仅当写 Stencil=2 的那一面朝向摄像机时
/// 才允许 stencilCollide 参与射线检测。
/// </summary>
public class StencilFaceWatcher : MonoBehaviour
{
    [System.Serializable]
    public class FaceEntry
    {
        public string name;
        [Tooltip("对应面的枢轴（例如 Coller_front）")]
        public Transform facePivot;
        [Tooltip("如果该面写了 Stencil=2，请勾选")]
        public bool isStencilTarget;
        [Tooltip("面的法线方向：Forward=使用forward，Up=使用up，Down=使用-up，Back=使用-forward")]
        public NormalDirection normalDirection = NormalDirection.Forward;
        [Range(0.5f, 1f)]
        [Tooltip("显示阈值：点积值达到此值时显示（避免抖动）")]
        public float visibleThreshold = 0.97f;
        [Range(0.3f, 0.9f)]
        [Tooltip("隐藏阈值：点积值低于此值时隐藏（应小于显示阈值，用于避免抖动）")]
        public float hiddenThreshold = 0.4f;
    }
    
    public enum NormalDirection
    {
        Forward,   // 使用 forward
        Back,      // 使用 -forward
        Up,        // 使用 up（适用于顶部面）
        Down,      // 使用 -up（适用于底部面）
        Right,     // 使用 right
        Left       // 使用 -right
    }

    [Tooltip("六个面（对应 Coller_front/back 等）的信息")]
    public FaceEntry[] faces;

    [Tooltip("负责切换点击层的 stencilCollide 组件")]
    public stencilCollide colliderGate;

    [Tooltip("用来判断朝向的摄像机；空则默认 Camera.main")]
    public Camera referenceCamera;

    bool currentVisible;

    void Awake()
    {
        if (referenceCamera == null)
            referenceCamera = Camera.main;
    }

    void Start()
    {
        if (colliderGate != null)
        {
            currentVisible = false;
            colliderGate.SetStencilVisible(false);
        }
    }

    void Update()
    {
        if (referenceCamera == null || colliderGate == null || faces == null || faces.Length == 0)
            return;

        FaceEntry targetFace = null;
        float bestDotValue = -1f;
        Vector3 camPos = referenceCamera.transform.position;
        
        // 遍历所有面，找到最朝向摄像机的那个面
        foreach (var face in faces)
        {
            if (face == null || !face.isStencilTarget || face.facePivot == null)
                continue;

            Vector3 toCamera = (camPos - face.facePivot.position).normalized;
            // 根据 normalDirection 选择面的法线方向
            Vector3 normal;
            switch (face.normalDirection)
            {
                case NormalDirection.Forward:
                    normal = face.facePivot.forward.normalized;
                    break;
                case NormalDirection.Back:
                    normal = -face.facePivot.forward.normalized;
                    break;
                case NormalDirection.Up:
                    normal = face.facePivot.up.normalized;
                    break;
                case NormalDirection.Down:
                    normal = -face.facePivot.up.normalized;
                    break;
                case NormalDirection.Right:
                    normal = face.facePivot.right.normalized;
                    break;
                case NormalDirection.Left:
                    normal = -face.facePivot.right.normalized;
                    break;
                default:
                    normal = face.facePivot.forward.normalized;
                    break;
            }
            float dotValue = Vector3.Dot(normal, toCamera);
            
            // 调试信息：显示每个面的点积值
            if (Time.frameCount % 60 == 0) // 每60帧打印一次，避免刷屏
            {
                Debug.Log($"[StencilFaceWatcher] 面 '{face.name}': dotValue={dotValue:F3}, threshold={face.visibleThreshold}, normal={normal}, toCamera={toCamera}, direction={face.normalDirection}");
            }
            
            // 找到点积值最大的面（最朝向摄像机的面）
            if (dotValue > bestDotValue)
            {
                bestDotValue = dotValue;
                targetFace = face;
            }
        }

        if (targetFace == null)
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogWarning("[StencilFaceWatcher] 未找到任何有效的目标面（isStencilTarget=true）");
            }
            return;
        }

        // 确保隐藏阈值小于显示阈值
        float hideThreshold = Mathf.Min(targetFace.hiddenThreshold, targetFace.visibleThreshold - 0.05f);
        
        // 使用滞后机制避免抖动：显示和隐藏使用不同的阈值
        bool shouldBeVisible;
        if (currentVisible)
        {
            // 当前是可见状态：只有当点积值降到隐藏阈值以下时才隐藏
            shouldBeVisible = bestDotValue >= hideThreshold;
        }
        else
        {
            // 当前是隐藏状态：只有当点积值达到显示阈值时才显示
            shouldBeVisible = bestDotValue >= targetFace.visibleThreshold;
        }

        // 调试信息：显示当前状态
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[StencilFaceWatcher] 最佳面: '{targetFace.name}', dotValue={bestDotValue:F3}, showThreshold={targetFace.visibleThreshold}, hideThreshold={hideThreshold}, currentVisible={currentVisible}, shouldBeVisible={shouldBeVisible}");
        }

        if (shouldBeVisible == currentVisible)
            return;

        currentVisible = shouldBeVisible;
        colliderGate.SetStencilVisible(currentVisible);
        
        Debug.Log($"[StencilFaceWatcher] 状态改变: {currentVisible} (面: {targetFace.name}, dotValue: {bestDotValue:F3})");
    }
}
