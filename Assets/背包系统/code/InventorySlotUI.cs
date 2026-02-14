using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 背包格子的 UI 组件：显示图标、数量，支持悬停提示、点击事件。
/// 每个格子需单独挂在一个 UI 物体上，并设置 slotIndex 对应背包中的索引。
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("显示引用")]
    [Tooltip("用于显示物品图标的 Image 组件")]
    public Image itemIcon;
    [Tooltip("用于显示数量的 Text 组件（可为空，有则显示堆叠数）")]
    public Text amountText;
    [Tooltip("该格子对应的背包索引，需与 InventoryManager 中的格子顺序一致")]
    public int slotIndex;

    [Header("提示框（可选）")]
    [Tooltip("物品说明的提示框物体。不赋值则自动查找场景中名为 ItemTooltip 的对象")]
    public GameObject tooltipObject;
    [Tooltip("提示框跟随鼠标的偏移")]
    public Vector2 tooltipOffset = new Vector2(10f, -10f);

    [Header("点击行为")]
    [Tooltip("勾选时，点击格子会从背包移除 1 个该物品")]
    public bool clickToRemove = true;
    [Tooltip("点击格子时触发，参数为当前格子索引（可在 Inspector 中绑定其他逻辑）")]
    public UnityEvent<int> onSlotClicked;

    private InventoryManager inventory;
    private Text tooltipTextComponent;

    private void Start()
    {
        inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[InventorySlotUI] 未找到 InventoryManager.Instance，请确保场景中有挂载 InventoryManager 的对象。");
            return;
        }

        // 解析提示框
        if (tooltipObject != null)
        {
            tooltipTextComponent = tooltipObject.GetComponent<Text>();
            if (tooltipTextComponent == null)
                tooltipTextComponent = tooltipObject.GetComponentInChildren<Text>();
            if (tooltipObject.activeSelf)
                tooltipObject.SetActive(false);
        }
        else
        {
            GameObject findTooltip = GameObject.Find("ItemTooltip");
            if (findTooltip != null)
            {
                tooltipObject = findTooltip;
                tooltipTextComponent = findTooltip.GetComponent<Text>();
                if (tooltipTextComponent == null)
                    tooltipTextComponent = findTooltip.GetComponentInChildren<Text>();
                findTooltip.SetActive(false);
            }
        }

        UpdateSlotUI();
    }

    /// <summary>
    /// 根据当前背包数据刷新该格子的显示（图标、数量）。
    /// 背包增删物品后可由外部调用，或由 InventoryManager 统一刷新所有格子。
    /// </summary>
    public void UpdateSlotUI()
    {
        if (inventory == null || inventory.inventorySlots == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.inventorySlots.Count)
        {
            Debug.LogWarning($"[InventorySlotUI] slotIndex {slotIndex} 超出范围 0~{inventory.inventorySlots.Count - 1}");
            return;
        }

        InventorySlot slot = inventory.inventorySlots[slotIndex];

        if (itemIcon != null)
        {
            if (slot.item != null)
            {
                itemIcon.sprite = slot.item.icon;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.enabled = false;
            }
        }

        if (amountText != null)
        {
            if (slot.item != null && slot.item.isStackable && slot.amount > 1)
            {
                amountText.text = slot.amount.ToString();
                amountText.enabled = true;
            }
            else
            {
                amountText.enabled = false;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventory == null || inventory.inventorySlots == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.inventorySlots.Count) return;

        InventorySlot slot = inventory.inventorySlots[slotIndex];
        if (slot.item == null) return;

        if (tooltipObject != null && tooltipTextComponent != null)
        {
            tooltipTextComponent.text = $"<b>{slot.item.itemName}</b>\n{slot.item.description}";
            tooltipObject.SetActive(true);
            tooltipObject.transform.position = (Vector3)((Vector2)Input.mousePosition + tooltipOffset);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipObject != null)
            tooltipObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClick();
    }

    /// <summary>
    /// 格子被点击时调用。可挂在 Button 的 OnClick 上，或由 OnPointerClick 自动触发。
    /// </summary>
    public void OnSlotClick()
    {
        if (inventory == null || inventory.inventorySlots == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.inventorySlots.Count) return;

        InventorySlot slot = inventory.inventorySlots[slotIndex];
        if (slot.item == null) return;

        if (clickToRemove)
        {
            inventory.RemoveItem(slot.item, 1);
            UpdateSlotUI();
        }

        onSlotClicked?.Invoke(slotIndex);
    }
}
