using UnityEngine;

/// <summary>
/// 一次性背包互动点：左键射线命中<strong>挂载本脚本的物体</strong>（可在子物体上放 Collider，会向上查找）时，
/// 用当前背包选中物品在配置的父节点下实例化 <see cref="ItemSO.placedPrefab"/>，消耗物品后隐藏或销毁本根物体。
/// </summary>
/// <remarks>
/// 若场景里仍有 <see cref="SceneInteractItemPlacer"/> 且 Tag 与这里相同，同一次点击可能<strong>重复放置</strong>。
/// 建议：一次性点使用独立 Tag（例如「可互动一次性」「可互动_用完消失」），并把全局放置器的 <c>interactableTag</c> 设为只匹配常驻互动点。
/// <c>requiredTag</c> 留空则不做 Tag 校验（仅当你已关掉会与它冲突的放置器时使用）。
/// </remarks>
[DisallowMultipleComponent]
public class OneUseBackpackInteractable : MonoBehaviour
{
    [Header("点击检测")]
    public Camera targetCamera;
    [Tooltip("挂在挂本脚本的根物体上；与 SceneInteractItemPlacer 错开可避免双次放置。留空=不校验 Tag（慎用）。")]
    public string requiredTag = "可互动一次性";
    public LayerMask interactLayerMask = ~0;
    public float rayDistance = 200f;

    [Header("物品条件")]
    [Tooltip("不填：当前选中任意带 placedPrefab 的物品均可；填了：必须是该 ItemSO")]
    public ItemSO onlyAcceptItem;

    [Header("放置")]
    [Tooltip("与 SceneInteractItemPlacer 一致：成功后是否从背包扣 1 个选中物品")]
    public bool consumeOnSuccess = true;

    [Header("用完后（本组件所在根物体）")]
    [Tooltip("为 true：Destroy(gameObject)；为 false：SetActive(false)")]
    public bool destroySelf = true;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryPlaceThenDispose();
    }

    public void TryPlaceThenDispose()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
            return;

        ItemSO selected = inv.GetSelectedItem();
        if (selected == null)
            return;

        if (onlyAcceptItem != null && onlyAcceptItem != selected)
            return;

        if (targetCamera == null)
            return;

        GameObject hitGo = RaycastRootGameObject(Input.mousePosition);
        if (hitGo == null)
            return;

        var selfOnHit = hitGo.GetComponentInParent<OneUseBackpackInteractable>();
        if (selfOnHit == null || selfOnHit != this)
            return;

        // Tag 挂在挂有本脚本的根物体上（子物体 Collider 命中时 hit 物体可能无 Tag）；requiredTag 为空则跳过
        if (!string.IsNullOrEmpty(requiredTag) && !gameObject.CompareTag(requiredTag))
            return;

        GameObject prefab = selected.placedPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[OneUseBackpackInteractable] {selected.itemName} 未设置 placedPrefab。");
            return;
        }

        Transform parent = ResolvePlaceParent();
        GameObject instance = Object.Instantiate(prefab, parent, false);

        bool consumeOk = !consumeOnSuccess || inv.TryConsumeSelectedItem(1);
        if (consumeOnSuccess && !consumeOk)
            Debug.LogWarning("[OneUseBackpackInteractable] 已生成实例，但消耗背包物品失败。");

        inv.RefreshAllSlots();

        if (!consumeOk)
            return;

        if (instance != null && instance.transform.IsChildOf(transform))
            instance.transform.SetParent(transform.parent, true);

        if (destroySelf)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    Transform ResolvePlaceParent()
    {
        // ScenePlacementTarget 可挂在父物体上
        var st = GetComponentInParent<ScenePlacementTarget>();
        if (st != null && st.defaultParent != null)
            return st.defaultParent;
        return transform;
    }

    GameObject RaycastRootGameObject(Vector3 screenPosition)
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
