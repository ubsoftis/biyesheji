using UnityEngine;

/// <summary>
/// 挂在与 <see cref="InventoryManager"/> 同一物体上，在 Inspector 中拖入 5 个指定的 <see cref="ItemSO"/>，
/// 用于判断背包里是否已同时拥有这五种物品（每种至少 1 个）。
/// </summary>
[RequireComponent(typeof(InventoryManager))]
public class InventoryRequiredFiveItems : MonoBehaviour
{
    [Tooltip("必须全部存在于背包中的 5 个物品（拖入 ItemSO 资源）")]
    public ItemSO[] requiredItems = new ItemSO[5];

    InventoryManager _inv;

    void Awake()
    {
        _inv = GetComponent<InventoryManager>();
    }

    void OnValidate()
    {
        if (requiredItems == null || requiredItems.Length != 5)
            requiredItems = new ItemSO[5];
    }

    /// <summary>五种指定物品是否都已存在于背包中。</summary>
    public bool HasAllFiveRequired()
    {
        if (_inv == null) return false;
        if (requiredItems == null || requiredItems.Length != 5) return false;

        for (int i = 0; i < 5; i++)
        {
            if (requiredItems[i] == null) return false;
            if (!_inv.HasItem(requiredItems[i])) return false;
        }

        return true;
    }
}
