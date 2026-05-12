using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 场景交互放置器：
/// - 先在 <see cref="InventorySlotUI"/> 里选中格子，再左键点击场景中 tag 为 <see cref="interactableTag"/> 且带 <see cref="ScenePlacementTarget"/> 的物体
/// - 读取当前背包选中的 ItemSO，实例化 <see cref="ItemSO.placedPrefab"/>，并按设置消耗背包
/// </summary>
public class SceneInteractItemPlacer : MonoBehaviour
{
    [Header("点击检测")]
    public Camera targetCamera;
    public string interactableTag = "可互动";
    public LayerMask interactLayerMask = ~0;
    public float rayDistance = 200f;

    [Header("放置设置")]
    [Tooltip("是否在成功放置后消耗背包中 1 个该物品")]
    public bool consumeOnSuccess = true;

    private void Awake()
    {
        EnsureCamera();
    }

    private void Start()
    {
        // 比 Awake 更晚，避免与别处的相机切换/主相机分配竞态
        EnsureCamera();
    }

    private void EnsureCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryPlaceSelectedItemByMouse();
    }

    public void TryPlaceSelectedItemByMouse()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        ItemSO selectedItem = inv.GetSelectedItem();
        if (selectedItem == null) return;

        if (IsPointerOverBlockingUiWhenPlacing(inv))
            return;

        EnsureCamera();
        if (targetCamera == null) return;

        GameObject target = RaycastTarget(Input.mousePosition);
        if (target == null) return;

        // Collider 可在子物体上，用 InParent 找 ScenePlacementTarget
        var targetConfig = target.GetComponentInParent<ScenePlacementTarget>();
        if (targetConfig == null)
        {
            Debug.LogWarning($"[SceneInteractItemPlacer] 目标 {target.name} 及其父级没有 ScenePlacementTarget，无法放置。请在 NPC 等物体上添加 ScenePlacementTarget。");
            return;
        }

        // 1. 决定父级（优先用 ScenePlacementTarget.defaultParent，其次用挂目标的物体自身）
        Transform parent = targetConfig.defaultParent != null ? targetConfig.defaultParent : targetConfig.transform;

        // 2. 决定预制体：直接使用 ItemSO 上配置的 placedPrefab
        GameObject prefab = selectedItem.placedPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[SceneInteractItemPlacer] {selectedItem.itemName} 没有可用的放置预制体 (placedPrefab 未设置)。");
            return;
        }

        // 3. 实例化为子级，保持预制体自身局部 Transform
        GameObject instance = Object.Instantiate(prefab, parent, false);

        // 4. 消耗：全局开关 + ItemSO 上的 per-item 开关
        bool doConsume = consumeOnSuccess && selectedItem.consumeFromInventoryWhenPlaced;
        if (doConsume && !inv.TryConsumeSelectedItem(1))
            Debug.LogWarning("[SceneInteractItemPlacer] 放置成功，但消耗背包物品失败（请确认已先点击格子选中物品）。");

        inv.RefreshAllSlots();
    }

    /// <summary>
    /// 仅在「已选中物品、即将做场景放置」时调用。
    /// 只拦截点在 <see cref="InventoryManager.gridParent"/>（格子容器）上的点击，避免其它 Canvas UI 挡住鱼缸放置。
    /// </summary>
    private bool IsPointerOverBlockingUiWhenPlacing(InventoryManager inv)
    {
        if (EventSystem.current == null)
            return false;

        if (inv.gridParent == null)
            return EventSystem.current.IsPointerOverGameObject();

        var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>(8);
        EventSystem.current.RaycastAll(ped, results);
        if (results.Count == 0)
            return false;

        Transform t = results[0].gameObject.transform;
        return t == inv.gridParent || t.IsChildOf(inv.gridParent);
    }

    /// <summary>
    /// 命中物体或其父级上存在 <see cref="ScenePlacementTarget"/>，且该组件所在物体带 <see cref="interactableTag"/>。
    /// 用于跳过鱼缸内已放置鱼等同层碰撞体（常为 Untagged）。
    /// </summary>
    private bool IsValidPlacementHit(GameObject hitGo)
    {
        if (hitGo == null)
            return false;
        ScenePlacementTarget st = hitGo.GetComponentInParent<ScenePlacementTarget>();
        if (st == null)
            return false;
        return st.gameObject.CompareTag(interactableTag);
    }

    private GameObject RaycastTarget(Vector3 screenPosition)
    {
        Ray ray = targetCamera.ScreenPointToRay(screenPosition);

        var candidates = new List<(float dist, GameObject go)>(12);

        foreach (RaycastHit h in Physics.RaycastAll(ray, rayDistance, interactLayerMask))
        {
            if (h.collider != null)
                candidates.Add((h.distance, h.collider.gameObject));
        }

        foreach (RaycastHit2D h in Physics2D.GetRayIntersectionAll(ray, rayDistance, interactLayerMask))
        {
            if (h.collider != null)
                candidates.Add((h.distance, h.collider.gameObject));
        }

        if (candidates.Count > 0)
        {
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
            for (int i = 0; i < candidates.Count; i++)
            {
                GameObject go = candidates[i].go;
                if (IsValidPlacementHit(go))
                    return go;
            }
        }

        // 备用：正交相机下用 OverlapPoint（部分情况下 GetRayIntersection 与 Collider 深度组合仍可能漏检）
        Vector3 sp = screenPosition;
        if (targetCamera.orthographic)
            sp.z = Mathf.Abs(Vector3.Dot(targetCamera.transform.forward, targetCamera.transform.position));
        else
            sp.z = targetCamera.nearClipPlane;
        Vector3 world = targetCamera.ScreenToWorldPoint(sp);
        Collider2D overlap = Physics2D.OverlapPoint(world, interactLayerMask);
        if (overlap != null && IsValidPlacementHit(overlap.gameObject))
            return overlap.gameObject;

        return null;
    }
}
