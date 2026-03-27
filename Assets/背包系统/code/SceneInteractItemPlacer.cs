using UnityEngine;

/// <summary>
/// 场景交互放置器：
/// - 左键点击场景中 tag 为「可互动」的物体
/// - 读取当前背包选中的 ItemSO
/// - 按目标物体上的配置，把对应预制体实例化到指定父节点下面，作为子级
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

        if (targetCamera == null) return;

        GameObject target = RaycastTarget(Input.mousePosition);
        if (target == null) return;
        if (!target.CompareTag(interactableTag)) return;

        // 目标物体上挂一个 ScenePlacementTarget，用来提供默认父节点
        var targetConfig = target.GetComponent<ScenePlacementTarget>();
        if (targetConfig == null)
        {
            Debug.LogWarning($"[SceneInteractItemPlacer] 目标 {target.name} 没有 ScenePlacementTarget 组件，无法放置物品。");
            return;
        }

        // 1. 决定父级（优先用 ScenePlacementTarget.defaultParent，其次用点击到的物体自身）
        Transform parent = targetConfig.defaultParent != null ? targetConfig.defaultParent : target.transform;

        // 2. 决定预制体：直接使用 ItemSO 上配置的 placedPrefab
        GameObject prefab = selectedItem.placedPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[SceneInteractItemPlacer] {selectedItem.itemName} 没有可用的放置预制体 (placedPrefab 未设置)。");
            return;
        }

        // 3. 实例化为子级，保持预制体自身局部 Transform
        GameObject instance = Object.Instantiate(prefab, parent, false);

        // 4. 成功后消耗背包物品
        if (consumeOnSuccess && !inv.TryConsumeSelectedItem(1))
            Debug.LogWarning("[SceneInteractItemPlacer] 放置成功，但消耗背包物品失败。");

        inv.RefreshAllSlots();
    }

    private GameObject RaycastTarget(Vector3 screenPosition)
    {
        Ray ray = targetCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit3D, rayDistance, interactLayerMask))
            return hit3D.collider.gameObject;

        Vector3 worldPos = targetCamera.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit2D = Physics2D.Raycast(worldPos, Vector2.zero, 0f, interactLayerMask);
        if (hit2D.collider != null)
            return hit2D.collider.gameObject;

        return null;
    }
}
