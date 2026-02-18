using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包数据处理中心：管理格子、添加/移除/查找。单例，挂场景里一个物体上。
/// 可选：配置 slotPrefab + gridParent 则在 Start 时自动生成格子 UI，并提供 RefreshAllSlots。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("背包数据")]
    [Tooltip("格子总数")]
    public int inventorySize = 20;
    [Tooltip("格子列表（运行时自动初始化，与 InventorySlotUI 的 slotIndex 对应）")]
    public List<InventorySlot> inventorySlots;

    [Header("可选：自动生成格子 UI")]
    [Tooltip("不填则需手动在场景里摆好格子并设 slotIndex")]
    public GameObject slotPrefab;
    public Transform gridParent;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
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

    private void Start()
    {
        if (slotPrefab != null && gridParent != null)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, gridParent);
                var slotUI = slotObj.GetComponent<InventorySlotUI>();
                if (slotUI != null) slotUI.slotIndex = i;
                var btn = slotObj.GetComponent<Button>();
                if (btn != null && slotUI != null) btn.onClick.AddListener(slotUI.OnSlotClick);
            }
        }
    }

    // ---------- 添加 ----------
    public bool AddItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0 || inventorySlots == null) return false;

        if (item.isStackable)
        {
            foreach (var slot in inventorySlots)
            {
                if (amount <= 0) return true;
                if (slot.item == item && slot.amount < item.maxStack)
                {
                    int add = Mathf.Min(amount, item.maxStack - slot.amount);
                    slot.AddAmount(add);
                    amount -= add;
                }
            }
        }

        int perSlot = item.isStackable ? item.maxStack : 1;
        foreach (var slot in inventorySlots)
        {
            if (amount <= 0) return true;
            if (slot.IsEmpty)
            {
                int put = Mathf.Min(amount, perSlot);
                slot.item = item;
                slot.amount = put;
                amount -= put;
            }
        }

        return amount == 0;
    }

    // ---------- 移除 ----------
    public bool RemoveItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0 || inventorySlots == null) return false;
        int left = amount;
        foreach (var slot in inventorySlots)
        {
            if (left <= 0) return true;
            if (slot.item == item)
            {
                int remove = Mathf.Min(left, slot.amount);
                if (slot.RemoveAmount(remove)) left -= remove;
            }
        }
        return left == 0;
    }

    // ---------- 查找 ----------
    public int GetItemCount(ItemSO item)
    {
        if (item == null || inventorySlots == null) return 0;
        int count = 0;
        foreach (var slot in inventorySlots)
            if (slot.item == item) count += slot.amount;
        return count;
    }

    public bool HasItem(ItemSO item) => GetItemCount(item) > 0;

    public int GetEmptySlotCount()
    {
        if (inventorySlots == null) return 0;
        int n = 0;
        foreach (var slot in inventorySlots)
            if (slot.IsEmpty) n++;
        return n;
    }

    /// <summary> 刷新所有格子显示（拾取/移除后调用；若未用 slotPrefab 则需外部用 FindObjectsOfType InventorySlotUI 刷新）。 </summary>
    public void RefreshAllSlots()
    {
        foreach (var slotUI in FindObjectsOfType<InventorySlotUI>(true))
            slotUI.UpdateSlotUI();
    }
}
