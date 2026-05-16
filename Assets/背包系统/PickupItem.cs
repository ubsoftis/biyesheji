using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂到可拾取物体上。支持两种用法：
/// 1）挂在 UI（如 Image）上：直接点击即可拾取，需保证该 UI 的 Raycast Target 勾选。
/// 2）挂在场景物体上：在你的射线检测命中后调用 Pickup()。
/// </summary>
public class PickupItem : MonoBehaviour, IPointerClickHandler
{
    [Header("拾取物品")]
    public ItemSO item;
    public int amount = 1;

    [Header("调试")]
    public bool debugLog;

    private InventoryManager GetInventoryManager()
    {
        if (InventoryManager.Instance != null) return InventoryManager.Instance;

        InventoryManager found = FindObjectOfType<InventoryManager>(true);
        if (found != null)
            InventoryManager.Instance = found;

        return found;
    }

    /// <summary>
    /// UI 被点击时由 EventSystem 调用（apple 是 Canvas 下的 Image 时会走这里）。
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Pickup();
    }

    private void RefreshInventoryUI()
    {
        var manager = GetInventoryManager();
        if (manager != null)
        {
            manager.RefreshAllSlots();
            return;
        }

        // 兜底：包含未激活的格子（背包关闭时 bag 下的格子都是 inactive）
        foreach (var slotUI in FindObjectsOfType<InventorySlotUI>(true))
            slotUI.UpdateSlotUI();
    }

    /// <summary>
    /// 由外部（如你的射线检测脚本）在点击命中该物体时调用，执行拾取逻辑。
    /// </summary>
    public void Pickup()
    {
        if (debugLog) Debug.Log("[PickupItem] 被调用拾取");

        if (item == null)
        {
            Debug.LogWarning("[PickupItem] 未设置 item。");
            return;
        }
        var managerRef = GetInventoryManager();
        if (managerRef == null)
        {
            Debug.LogWarning("[PickupItem] 场景里没有 InventoryManager。");
            return;
        }

        bool isAdded = managerRef.AddItem(item, amount);
        if (isAdded)
        {
            if (debugLog) Debug.Log("[PickupItem] 拾取成功: " + item.itemName);
            managerRef.PlayPickupSound(item);
            RefreshInventoryUI();
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("[PickupItem] 背包已满。");
        }
    }
}
