using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 背包格子的 UI 组件：显示图标、数量，支持悬停提示、点击事件。
/// 每个格子需单独挂在一个 UI 物体上，并设置 slotIndex 对应背包中的索引。
/// </summary>
/// <remarks>
/// 与场景放置配合：先点击格子（<see cref="OnSlotClick"/> → <see cref="InventoryManager.SelectSlot"/>），
/// 再在<strong>非 UI 区域</strong>左键点击带 Tag「可互动」且挂 <see cref="ScenePlacementTarget"/> 的物体（如 NPC2），
/// 由 <see cref="SceneInteractItemPlacer"/> 消耗物品并刷新格子；若同物体上有 <see cref="UnityEngine.UI.Button"/>，请把 OnClick 绑到 <see cref="OnSlotClick"/>。
/// </remarks>
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
    [Tooltip("选中时图标颜色")]
    public Color selectedIconColor = new Color(1f, 1f, 0.6f, 1f);
    [Tooltip("未选中时图标颜色")]
    public Color normalIconColor = Color.white;
    [Tooltip("可选：用于高亮边框（选中显示，未选中隐藏）")]
    public GameObject selectedHighlightObject;
    [Tooltip("点击格子时触发，参数为当前格子索引（可在 Inspector 中绑定其他逻辑）")]
    public UnityEvent<int> onSlotClicked;

    private InventoryManager inventory;
    private Text tooltipTextComponent;
    private bool warnedHighlightBinding;
    private Button cachedButton;

    private void Awake()
    {
        cachedButton = GetComponent<Button>();
        EnsureValidVisualDefaults();
    }

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
        UpdateSelectionVisual();
    }

    private void OnValidate()
    {
        EnsureValidVisualDefaults();
    }

    private void OnEnable()
    {
        if (inventory == null) inventory = InventoryManager.Instance;
        if (inventory != null) inventory.OnSelectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnSelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(int _)
    {
        UpdateSelectionVisual();
    }

    /// <summary>
    /// 根据当前背包数据刷新该格子的显示（图标、数量）。
    /// 背包增删物品后可由外部调用，或由 InventoryManager 统一刷新所有格子。
    /// </summary>
    public void UpdateSlotUI()
    {
        if (inventory == null)
            inventory = InventoryManager.Instance;
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
                if (!itemIcon.gameObject.activeSelf)
                    itemIcon.gameObject.SetActive(true);
                itemIcon.sprite = slot.item.icon;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }
        }

        if (amountText != null)
        {
            // 显示所有可堆叠物品的数量（包括 1）
            if (slot.item != null && slot.item.isStackable && slot.amount > 0)
            {
                amountText.text = slot.amount.ToString();
                amountText.enabled = true;
            }
            else
            {
                // 非可堆叠物品或空格子隐藏数字
                amountText.enabled = false;
            }
        }

        UpdateSelectionVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventory == null)
            inventory = InventoryManager.Instance;
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
        // 若同物体上有 Button，OnClick 已经会调用 OnSlotClick，避免一次点击触发两次
        if (cachedButton != null)
            return;

        OnSlotClick();
    }

    /// <summary>
    /// 格子被点击时调用。可挂在 Button 的 OnClick 上，或由 OnPointerClick 自动触发。
    /// </summary>
    public void OnSlotClick()
    {
        if (inventory == null)
            inventory = InventoryManager.Instance;
        if (inventory == null || inventory.inventorySlots == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.inventorySlots.Count) return;

        InventorySlot slot = inventory.inventorySlots[slotIndex];
        bool hasItem = slot.item != null && !slot.IsEmpty;

        // 再次点击已选中格子 -> 取消选中
        if (inventory.selectedSlotIndex == slotIndex)
        {
            inventory.ClearSelection();
            UpdateSelectionVisual();
            onSlotClicked?.Invoke(slotIndex);
            return;
        }

        // 点击空格子 -> 取消当前选中
        if (!hasItem)
        {
            inventory.ClearSelection();
            UpdateSelectionVisual();
            onSlotClicked?.Invoke(slotIndex);
            return;
        }

        // 点击其它有物品格子 -> 切换选中
        inventory.SelectSlot(slotIndex);
        UpdateSelectionVisual();

        onSlotClicked?.Invoke(slotIndex);
    }

    private void UpdateSelectionVisual()
    {
        if (inventory == null)
            inventory = InventoryManager.Instance;
        if (inventory == null) return;

        bool selected = inventory.selectedSlotIndex == slotIndex;

        if (itemIcon != null)
            itemIcon.color = selected ? selectedIconColor : normalIconColor;

        if (selectedHighlightObject != null)
        {
            // 保护：若高亮对象误绑成 IconImage 本体，会导致未选中时图标被整块隐藏
            if (itemIcon != null && selectedHighlightObject == itemIcon.gameObject)
            {
                if (!warnedHighlightBinding)
                {
                    Debug.LogWarning("[InventorySlotUI] selectedHighlightObject 与 itemIcon 指向同一对象，已忽略显隐控制以避免图标消失。");
                    warnedHighlightBinding = true;
                }
            }
            else
            {
                selectedHighlightObject.SetActive(selected);
            }
        }
    }

    private void EnsureValidVisualDefaults()
    {
        // 兼容老预制体/老场景：新增颜色字段可能被序列化为全透明，导致图标看不见
        if (normalIconColor.a <= 0.001f)
            normalIconColor = Color.white;

        if (selectedIconColor.a <= 0.001f)
            selectedIconColor = new Color(1f, 1f, 0.6f, 1f);
    }
}
