using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 魔方蒙版可点击检测器（2D 射线版）
/// 使用 Physics2D.Raycast 从摄像机发射射线，命中 2D 碰撞体。
/// 当魔方旋转后，背后的物体转到正面时，会随 2D 射线命中顺序被点击。
/// 适用于 2D 场景，需要目标物体挂有 Collider2D。
/// </summary>
public class StencilCubeRaycaster2D : MonoBehaviour
{
    [Tooltip("射线使用的摄像机；留空则取 Camera.main")]
    public Camera rayCamera;

    [Tooltip("射线检测的 Layer（通常为魔方小方块的 Cube 层）")]
    public LayerMask raycastLayer;

    [Tooltip("射线最长检测距离")]
    public float rayDistance = 100f;

    [Tooltip("屏幕点转世界点时使用的深度（相对相机）。透视相机下建议设为魔方到相机的距离，正交相机可保持 0")]
    public float planeDepth = 10f;

    [Tooltip("是否忽略 UI 阻挡（点击在 UI 上时不触发魔方点击）")]
    public bool ignoreUIBlocking = true;

    [Tooltip("是否每帧打印命中信息（调试用）")]
    public bool debugLog = false;

    [Tooltip("是否点击到了物体（供 NodeCanvas 等外部读取）")]
    public bool isHit = false;

    int _cubeLayer;
    bool _layerValid;

    void Awake()
    {
        rayCamera = rayCamera != null ? rayCamera : Camera.main;
        _cubeLayer = LayerMask.NameToLayer("Cube");
        _layerValid = (raycastLayer.value != 0);
        if (!_layerValid)
        {
            raycastLayer = 1 << _cubeLayer;
            if (_cubeLayer < 0)
                Debug.LogWarning("[StencilCubeRaycaster2D] 未找到 'Cube' 层，请在 Project Settings > Tags and Layers 中添加。");
        }
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        if (rayCamera == null)
            return;
        if (ignoreUIBlocking && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        CastRay(Input.mousePosition);
    }

    /// <summary>
    /// 从屏幕坐标投射 2D 射线并处理命中
    /// </summary>
    public void CastRay(Vector2 screenPosition)
    {
        if (rayCamera == null)
        {
            Debug.LogError("[StencilCubeRaycaster2D] 未设置 Camera，无法投射射线。");
            return;
        }

        Vector3 worldPos = rayCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, planeDepth));
        Vector2 origin = (Vector2)rayCamera.transform.position;
        Vector2 end = new Vector2(worldPos.x, worldPos.y);
        Vector2 dir = end - origin;

        RaycastHit2D hit;
        if (dir.sqrMagnitude < 0.0001f)
        {
            // 点击在相机正前方时用 OverlapPoint 检测该点上的碰撞体
            Collider2D col = Physics2D.OverlapPoint(end, raycastLayer);
            if (col != null)
            {
                HandleHit(col, end);
                return;
            }
            if (debugLog)
                Debug.Log("[StencilCubeRaycaster2D] 未命中任何魔方方块（OverlapPoint）。");
            return;
        }

        dir.Normalize();
        hit = Physics2D.Raycast(origin, dir, rayDistance, raycastLayer);

        if (!hit)
        {
            if (debugLog)
                Debug.Log("[StencilCubeRaycaster2D] 未命中任何魔方方块。");
            return;
        }

        HandleHit(hit);
    }

    void HandleHit(RaycastHit2D hit)
    {
        HandleHit(hit.collider, hit.point);
    }

    void HandleHit(Collider2D col, Vector2 point)
    {
        isHit = true;
        Transform hitTransform = col.transform;

        // 若命中对象在 Pivot_ 下，取其父物体作为“方块根”
        Transform cubeRoot = hitTransform;
        if (hitTransform.parent != null && hitTransform.parent.name.StartsWith("Pivot_"))
            cubeRoot = hitTransform.parent;

        GameObject hitObj = cubeRoot.gameObject;
        Vector3 hitPoint3 = new Vector3(point.x, point.y, 0f);

        if (debugLog)
            Debug.Log($"[StencilCubeRaycaster2D] 点击到 {hitObj.name}");

        var clickable = col.GetComponent<IStencilClickable>();
        if (clickable == null)
            clickable = cubeRoot.GetComponent<IStencilClickable>();

        if (clickable != null)
        {
            clickable.OnStencilClick();
        }
        else
        {
            var handler = cubeRoot.GetComponent<StencilCubeClickHandler>();
            if (handler != null)
                handler.OnCubeFaceClicked(hitObj, hitPoint3, Vector3.forward);
        }
    }
}
