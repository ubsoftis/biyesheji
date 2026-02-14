using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 背包格子（存储物品+数量）
[System.Serializable]
public class InventorySlot
{
    public ItemSO item;
    public int amount;

    // 初始化格子
    public InventorySlot(ItemSO newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
    }

    // 增加物品数量
    public void AddAmount(int value)
    {
        amount += value;
    }

    // 减少物品数量
    public bool RemoveAmount(int value)
    {
        if (amount - value < 0) return false;
        amount -= value;
        if (amount == 0) item = null; // 数量为0时清空物品
        return true;
    }
}

/// <summary>
/// 背包管理器：单例，负责添加/移除物品、查询背包状态。
/// 挂到场景里一个空物体上即可使用。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("背包设置")]
    [Tooltip("背包格子总数")]
    public int inventorySize = 20;
    [Tooltip("背包格子列表（运行时自动初始化）")]
    public List<InventorySlot> inventorySlots;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 可选：跨场景不销毁
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (inventorySlots == null || inventorySlots.Count == 0)
        {
            inventorySlots = new List<InventorySlot>();
            for (int i = 0; i < inventorySize; i++)
                inventorySlots.Add(new InventorySlot(null, 0));
        }
    }

    /// <summary> 添加物品到背包。返回 true 表示全部放入，false 表示背包满或部分放入。 </summary>
    public bool AddItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // 1. 可堆叠：先往已有该物品的格子里堆
        if (item.isStackable)
        {
            foreach (var slot in inventorySlots)
            {
                if (amount <= 0) return true;
                if (slot.item == item && slot.amount < item.maxStack)
                {
                    int addAmount = Mathf.Min(amount, item.maxStack - slot.amount);
                    slot.AddAmount(addAmount);
                    amount -= addAmount;
                }
            }
        }

        // 2. 剩余数量用空格子装（每个格子最多 maxStack，不可堆叠则 1 个/格）
        int perSlot = item.isStackable ? item.maxStack : 1;
        foreach (var slot in inventorySlots)
        {
            if (amount <= 0) return true;
            if (slot.item == null)
            {
                int put = Mathf.Min(amount, perSlot);
                slot.item = item;
                slot.amount = put;
                amount -= put;
            }
        }

        return amount == 0; // 若 amount 还有剩余说明背包满了
    }

    /// <summary> 从背包移除指定物品数量。返回 true 表示移除成功。 </summary>
    public bool RemoveItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        int left = amount;
        foreach (var slot in inventorySlots)
        {
            if (left <= 0) return true;
            if (slot.item == item)
            {
                int remove = Mathf.Min(left, slot.amount);
                if (slot.RemoveAmount(remove))
                {
                    left -= remove;
                }
            }
        }
        return left == 0;
    }

    /// <summary> 获取背包中该物品的总数量。 </summary>
    public int GetItemCount(ItemSO item)
    {
        if (item == null) return 0;
        int count = 0;
        foreach (var slot in inventorySlots)
            if (slot.item == item) count += slot.amount;
        return count;
    }

    /// <summary> 是否拥有该物品（至少 1 个）。 </summary>
    public bool HasItem(ItemSO item)
    {
        return GetItemCount(item) > 0;
    }

    /// <summary> 当前空格子数量。 </summary>
    public int GetEmptySlotCount()
    {
        int n = 0;
        foreach (var slot in inventorySlots)
            if (slot.item == null) n++;
        return n;
    }
}
