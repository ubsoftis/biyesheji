using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 从背包选中指定 <see cref="ItemSO"/> 后，用 <see cref="StencilCubeRaycaster2D"/> 点到挂有本脚本且带 Collider 的物体时：
/// SetActive 两个物体、关闭一个物体。可选联动 <see cref="StenciCube1"/>：仅当「9 点采样任意一点都不命中目标色」
/// （即 <c>anyOf9Visible == false</c>，需在 StenciCube1 上开启 enable9Samples）时才允许通过。
/// </summary>
public class InventoryStencilGateActivate : MonoBehaviour, IStencilClickable
{
    [Header("背包条件")]
    [Tooltip("玩家必须在背包里选中该物品（与格子里的 ItemSO 引用一致）")]
    public ItemSO requiredItem;

    [Tooltip("成功后是否从当前选中格子消耗数量")]
    public bool consumeSelectedItemOnSuccess = true;

    [Tooltip("消耗数量，默认可堆叠物品扣 1")]
    [Min(1)]
    public int consumeAmount = 1;

    [Header("联动：StenciCube1 九格采样")]
    [Tooltip("留空则不检查 9 点；拖入后按下方选项要求 anyOf9Visible")]
    public StenciCube1 linkedStenciCube1;

    [Tooltip(
        "为 true：必须满足 linkedStenciCube1.anyOf9Visible == false（九格都没有命中目标色）。\n" +
        "请确保 StenciCube1.enable9Samples 已开启，否则 anyOf9Visible 恒为 false。"
    )]
    public bool requireNoNineHit = true;

    [Header("点击后：激活 / 关闭")]
    public GameObject activateFirst;
    public GameObject activateSecond;
    [Tooltip("成功后 SetActive(false)")]
    public GameObject deactivateTarget;

    [Header("状态")]
    [Tooltip("成功后为 true，之后 OnStencilClick 不再执行逻辑")]
    public bool completed = false;

    [Header("可选")]
    public UnityEvent onSuccess;

    [Tooltip("失败时在 Console 打印原因（调试用）")]
    public bool debugLog = false;

    public void OnStencilClick()
    {
        if (completed)
        {
            if (debugLog)
                Debug.Log($"[InventoryStencilGateActivate] 已完成，忽略：{name}");
            return;
        }

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            if (debugLog)
                Debug.LogWarning("[InventoryStencilGateActivate] 场景中无 InventoryManager.Instance");
            return;
        }

        if (requiredItem == null)
        {
            if (debugLog)
                Debug.LogWarning("[InventoryStencilGateActivate] requiredItem 未设置");
            return;
        }

        ItemSO selected = inv.GetSelectedItem();
        if (selected != requiredItem)
        {
            if (debugLog)
                Debug.Log($"[InventoryStencilGateActivate] 背包选中物品不符：需要 {requiredItem.itemName}，当前 {(selected != null ? selected.itemName : "无")}");
            return;
        }

        if (linkedStenciCube1 != null && requireNoNineHit)
        {
            if (linkedStenciCube1.anyOf9Visible)
            {
                if (debugLog)
                    Debug.Log("[InventoryStencilGateActivate] 未通过：StenciCube1 的 9 点仍有命中（anyOf9Visible=true）");
                return;
            }
        }

        if (consumeSelectedItemOnSuccess)
        {
            if (!inv.TryConsumeSelectedItem(consumeAmount))
            {
                if (debugLog)
                    Debug.LogWarning("[InventoryStencilGateActivate] 消耗物品失败（数量不足或未选中）");
                return;
            }

            inv.RefreshAllSlots();
        }

        if (activateFirst != null) activateFirst.SetActive(true);
        if (activateSecond != null) activateSecond.SetActive(true);
        if (deactivateTarget != null) deactivateTarget.SetActive(false);

        completed = true;
        onSuccess?.Invoke();

        if (debugLog)
            Debug.Log($"[InventoryStencilGateActivate] 成功：{name}");
    }
}
