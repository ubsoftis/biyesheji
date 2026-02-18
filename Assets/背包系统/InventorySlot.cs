using UnityEngine;

/// <summary>
/// 背包中的一个格子数据：物品 + 数量。仅做数据结构，由 InventoryManager 管理。
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public ItemSO item;
    public int amount;

    public InventorySlot(ItemSO newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
    }

    public void AddAmount(int value)
    {
        if (value > 0) amount += value;
    }

    public bool RemoveAmount(int value)
    {
        if (value <= 0) return true;
        if (amount < value) return false;
        amount -= value;
        if (amount == 0) item = null;
        return true;
    }

    /// <summary> 是否为空。 </summary>
    public bool IsEmpty => item == null || amount <= 0;
}
