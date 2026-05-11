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
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        ItemSO selectedItem = inv.GetSelectedItem();
        if (selectedItem == null) return;

        EnsureCamera();
        if (targetCamera == null) return;

        GameObject target = RaycastTarget(Input.mousePosition);
        if (target == null) return;
        if (!target.CompareTag(interactableTag)) return;

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

    private GameObject RaycastTarget(Vector3 screenPosition)
    {
        Ray ray = targetCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit3D, rayDistance, interactLayerMask))
            return hit3D.collider.gameObject;

        // 2D：不要用 ScreenToWorldPoint(mouse)（z 常为 0）+ 零长度 Raycast，会几乎永远点不中。
        // 使用与相机射线一致的 GetRayIntersection，可命中任意深度平面上的 BoxCollider2D / PolygonCollider2D。
        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, rayDistance, interactLayerMask);
        if (hit2D.collider != null)
            return hit2D.collider.gameObject;

        // 备用：正交相机下用 OverlapPoint（部分情况下 GetRayIntersection 与 Collider 深度组合仍可能漏检）
        Vector3 sp = screenPosition;
        if (targetCamera.orthographic)
            sp.z = Mathf.Abs(Vector3.Dot(targetCamera.transform.forward, targetCamera.transform.position));
        else
            sp.z = targetCamera.nearClipPlane;
        Vector3 world = targetCamera.ScreenToWorldPoint(sp);
        Collider2D overlap = Physics2D.OverlapPoint(world, interactLayerMask);
        if (overlap != null)
            return overlap.gameObject;

        return null;
    }
}
