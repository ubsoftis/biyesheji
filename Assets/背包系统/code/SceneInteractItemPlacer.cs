using UnityEngine;

public interface IItemPlacementReceiver
{
    bool TryPlaceItem(ItemSO item);
}

/// <summary>
/// 场景交互放置器：
/// 点击 tag 为可互动 的物体，将背包当前选中物品放入目标。
/// 目标物体需挂载实现 IItemPlacementReceiver 的组件。
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

        var receiver = target.GetComponent<IItemPlacementReceiver>();
        if (receiver == null)
        {
            Debug.LogWarning($"[SceneInteractItemPlacer] 目标 {target.name} 未实现 IItemPlacementReceiver，无法放入物品。");
            return;
        }

        bool placed = receiver.TryPlaceItem(selectedItem);
        if (!placed) return;

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
