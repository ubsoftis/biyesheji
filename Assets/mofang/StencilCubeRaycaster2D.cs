using System.Collections.Generic;
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

    [Tooltip("射线检测的 Layer。必须包含「可点击目标」所在的层（如奇怪的房子），否则正面时仍无法点到；若也点魔方小块则再勾选 Cube")]
    public LayerMask raycastLayer;

    [Tooltip("射线最长检测距离")]
    public float rayDistance = 100f;

    [Tooltip(
        "沿相机 forward、从相机位置量起的距离（世界单位），用于确定 OverlapPoint 所在的采样平面。\n" +
        "应对齐「主要内容所在深度」：例如相机在 z=-10、物体约在 z≈-2，则约填 8。\n" +
        "实际换算用 ScreenPointToRay 与该平面求交，相机有俯仰/滚动时比直接 ScreenToWorldPoint 稳。"
    )]
    public float planeDepth = 10f;

    [Tooltip("是否忽略 UI 阻挡（点击在 UI 上时不触发魔方点击）")]
    public bool ignoreUIBlocking = true;

    [Tooltip(
        "仅当 ignoreUIBlocking 为 true 时生效。\n" +
        "为 true：只有 GraphicRaycaster 结果里命中 Layer「UI」才算遮挡（全屏 RawImage 若在 Default 层则不再挡世界点击）。\n" +
        "为 false：与 EventSystem.IsPointerOverGameObject() 一致。"
    )]
    public bool uiBlockingOnlyOnUiLayer = false;

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
        if (ignoreUIBlocking && EventSystem.current != null)
        {
            if (uiBlockingOnlyOnUiLayer)
            {
                if (IsUiLayerBlockingScreenPoint(Input.mousePosition))
                    return;
            }
            else if (EventSystem.current.IsPointerOverGameObject())
                return;
        }

        CastRay(Input.mousePosition);
    }

    /// <summary>
    /// 与 StencilActivateTwoOnClick.IsDirectPickBlockedByUi（directPickUiBlockingOnlyOnUiLayer）语义一致。
    /// </summary>
    bool IsUiLayerBlockingScreenPoint(Vector2 screenPosition)
    {
        var ped = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>(8);
        EventSystem.current.RaycastAll(ped, results);
        if (results.Count == 0)
            return false;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
            return EventSystem.current.IsPointerOverGameObject();

        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            if (go != null && go.layer == uiLayer)
                return true;
        }

        return false;
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

        Vector2 point = ScreenPointToWorldXYOnViewPlane(screenPosition);

        // Physics2DSettings.m_AutoSyncTransforms==0 时，Transform 改动可能尚未同步到碰撞体。
        Physics2D.SyncTransforms();

        // 点击判定应以“鼠标所在点”为准，而不是从相机发射一条长射线。
        // 使用 OverlapPointAll：同一点可能叠着魔方与目标物体，优先响应可点击目标。
        var cols = Physics2D.OverlapPointAll(point, raycastLayer);
        if (cols == null || cols.Length == 0)
        {
            if (debugLog)
                Debug.Log($"[StencilCubeRaycaster2D] 未命中任何碰撞体（OverlapPointAll）。worldXY={point}, planeDepth={planeDepth}, layerMask={raycastLayer.value}");
            return;
        }

        Collider2D targetCol = null;
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null || !c.enabled) continue;
            if (c.GetComponent<IStencilClickable>() != null)
            {
                targetCol = c;
                break;
            }
            if (targetCol == null)
                targetCol = c;
        }

        if (targetCol != null)
            HandleHit(targetCol, point);
    }

    /// <summary>
    /// 将屏幕像素换算到世界 XY：过该像素作相机射线，与「过 camera.position + forward*planeDepth、法线为 forward」的平面求交。
    /// </summary>
    Vector2 ScreenPointToWorldXYOnViewPlane(Vector2 screenPosition)
    {
        Ray ray = rayCamera.ScreenPointToRay(screenPosition);
        Vector3 planeOrigin = rayCamera.transform.position + rayCamera.transform.forward * planeDepth;
        var plane = new Plane(rayCamera.transform.forward, planeOrigin);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            return new Vector2(hit.x, hit.y);
        }

        Vector3 fallback = rayCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, planeDepth));
        if (debugLog)
            Debug.LogWarning("[StencilCubeRaycaster2D] 射线与采样平面平行，已退回 ScreenToWorldPoint（请检查 planeDepth / 相机姿态）。");
        return new Vector2(fallback.x, fallback.y);
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

        // 若目标挂了 Stencil 可见性门控脚本，则以 isVisible 作为“是否允许响应点击”的条件。
        // 这样可以避免通过禁用 Collider 来做 gating，从而不影响其它拾取/RT 判断逻辑。
        var gate = col.GetComponent<StenciCube>();
        if (gate == null) gate = cubeRoot.GetComponent<StenciCube>();
        if (gate != null && !gate.isVisible)
        {
            if (debugLog)
                Debug.Log($"[StencilCubeRaycaster2D] 命中 {cubeRoot.name} 但 isVisible=false，已忽略点击。");
            return;
        }
        var gatePlant = col.GetComponent<StencilCubePlant>();
        if (gatePlant == null) gatePlant = cubeRoot.GetComponent<StencilCubePlant>();
        if (gatePlant != null && !gatePlant.isVisible)
        {
            if (debugLog)
                Debug.Log($"[StencilCubeRaycaster2D] 命中 {cubeRoot.name} 但 isVisible=false(Plant)，已忽略点击。");
            return;
        }

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
