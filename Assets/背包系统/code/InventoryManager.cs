using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

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

    [Header("选中状态（运行时）")]
    [Tooltip("当前选中的格子索引，-1 表示未选中")]
    public int selectedSlotIndex = -1;

    public event Action<int> OnSelectionChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
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

    public ItemSO GetSelectedItem()
    {
        if (inventorySlots == null) return null;
        if (selectedSlotIndex < 0 || selectedSlotIndex >= inventorySlots.Count) return null;

        InventorySlot slot = inventorySlots[selectedSlotIndex];
        if (slot == null || slot.IsEmpty) return null;
        return slot.item;
    }

    public bool SelectSlot(int slotIndex)
    {
        if (inventorySlots == null) return false;

        if (slotIndex < 0 || slotIndex >= inventorySlots.Count)
        {
            ClearSelection();
            return false;
        }

        InventorySlot slot = inventorySlots[slotIndex];
        if (slot == null || slot.IsEmpty)
        {
            ClearSelection();
            return false;
        }

        if (selectedSlotIndex == slotIndex) return true;
        selectedSlotIndex = slotIndex;
        OnSelectionChanged?.Invoke(selectedSlotIndex);
        return true;
    }

    public void ClearSelection()
    {
        if (selectedSlotIndex == -1) return;
        selectedSlotIndex = -1;
        OnSelectionChanged?.Invoke(selectedSlotIndex);
    }

    // ---------- 添加 ----------
    public bool AddItem(ItemSO item, int amount = 1)
    {
        EnsureInitialized();
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
            if (left <= 0) break;
            if (slot.item == item)
            {
                int remove = Mathf.Min(left, slot.amount);
                if (slot.RemoveAmount(remove)) left -= remove;
            }
        }
        bool success = left == 0;
        int removed = amount - left;

        if (selectedSlotIndex >= 0 && selectedSlotIndex < inventorySlots.Count)
        {
            var selectedSlot = inventorySlots[selectedSlotIndex];
            if (selectedSlot == null || selectedSlot.IsEmpty)
                ClearSelection();
        }

        if (removed > 0)
            RefreshAllSlots();

        return success;
    }

    public bool RemoveItemFromSlot(int slotIndex, int amount = 1)
    {
        if (inventorySlots == null || amount <= 0) return false;
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return false;

        InventorySlot slot = inventorySlots[slotIndex];
        if (slot == null || slot.IsEmpty) return false;

        bool ok = slot.RemoveAmount(amount);
        if (!ok) return false;

        if (slot.IsEmpty && selectedSlotIndex == slotIndex)
            ClearSelection();

        RefreshAllSlots();
        return true;
    }

    public bool TryConsumeSelectedItem(int amount = 1)
    {
        if (selectedSlotIndex < 0) return false;
        return RemoveItemFromSlot(selectedSlotIndex, amount);
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
