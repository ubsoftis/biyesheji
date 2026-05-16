using UnityEngine;

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Consumable,   // 消耗品（如药水）
    Equipment,    // 装备（如武器、护甲）
    Material      // 材料（如矿石）
}

/// <summary>
/// 物品数据（ScriptableObject），用于在编辑器中创建物品资源
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemSO : UnityEngine.ScriptableObject
{
    public string itemName = "New Item";  // 物品名称
    public Sprite icon = null;            // 物品图标
    [Header("背包格子内图标")]
    [Tooltip("背包格子内图标 RectTransform 的宽高（sizeDelta，单位与 UI 一致）。\n(0,0)：宽高都用格子预制体默认；\n某一轴填 ≤0：该轴沿用预制体默认，另一轴用你填的像素，便于单独拉长或压扁以匹配图标长宽比。")]
    public Vector2 iconSlotSizeDelta;
    public ItemType itemType;             // 物品类型
    public int maxStack = 1;              // 最大堆叠数（如药水可叠99，装备只能叠1）
    public bool isStackable => maxStack > 1; // 是否可堆叠
    [TextArea]
    public string description = "";       // 物品描述

    [Header("场景放置（可选）")]
    [Tooltip("将该物品放入场景容器（例如鱼缸）时实例化的预制体")]
    public GameObject placedPrefab;
    [Tooltip("通过 SceneInteractItemPlacer 点到带 ScenePlacementTarget 的物体时，是否从背包扣除该物品（关闭则只生成预制体不扣数）")]
    public bool consumeFromInventoryWhenPlaced = true;

    [Header("音效（可选，不填则用 InventoryManager 上的默认音效）")]
    public AudioClip pickupSound;
    public AudioClip selectSound;
    public AudioClip placeSound;
}
