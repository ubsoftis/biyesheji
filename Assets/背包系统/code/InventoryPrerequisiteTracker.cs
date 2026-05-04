using UnityEngine;

/// <summary>
/// 挂在 <see cref="InventoryManager"/> 同一物体上，集中检测指定 <see cref="ItemSO"/>：
/// 是否曾出现在背包（峰值 ≥ 1）且之后至少被消耗过 1 个（数量相对峰值下降过，或相对上一帧下降）。
/// 满足后闩锁保持为 true。其它脚本（如关卡驱动）只读 <see cref="IsSatisfied"/> 即可。
/// 可选：再拖 <see cref="ItemSO"/>，当该物品在背包中数量从 0 变为 ≥1 时，将指定 <see cref="MonoBehaviour"/> 停用一次，或将指定 <see cref="GameObject"/> 激活一次（与 Tracked Consumable 同一引用方式）。
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(InventoryManager))]
public class InventoryPrerequisiteTracker : MonoBehaviour
{
    [Tooltip("要跟踪的消耗品（须与背包格子里的 ItemSO 为同一资源引用）")]
    public ItemSO trackedConsumable;

    [Header("状态（只读）")]
    public int currentCount;
    public int peakCount;
    [Tooltip("是否已满足「曾拥有且至少用过 1 个」")]
    public bool hasUsedOnce;

    [Header("监视另一物品进包时关脚本（可选）")]
    [Tooltip("与 Tracked Consumable 一样拖 ItemSO。当该物品数量从 0 变为 ≥1 时，将下方脚本 enabled=false（只执行一次）。留空则不做。")]
    public ItemSO watchItemWhenInInventory;
    [Tooltip("当 watchItemWhenInInventory 进包时，将此组件 enabled=false（只执行一次）。留空则忽略。")]
    public MonoBehaviour disableWhenWatchObjectAppears;
    [Tooltip("若开局背包里已有该物品（数量>0），视为已「进过包」，不会立刻关脚本；需先清空再获得才触发。")]
    public bool treatInitialItemInBackpackAsAlreadySeen = true;

    [Header("监视另一物品进包时激活物体（可选）")]
    [Tooltip("拖 ItemSO。当该物品数量从 0 变为 ≥1 时，将下方物体 SetActive(true)（只执行一次）。可与上一栏为不同物品。留空则不做。")]
    public ItemSO activateWhenItemInInventory;
    [Tooltip("当 activateWhenItemInInventory 进包时激活此物体。留空则忽略。")]
    public GameObject objectToActivateWhenItemAppears;
    [Tooltip("若开局背包里已有该物品（数量>0），视为已进过包，不会立刻激活；需先清空再获得才触发。")]
    public bool treatInitialItemForActivateAsAlreadySeen = true;

    [Header("调试")]
    public bool debugLog;

    InventoryManager _inventory;
    int _peakPrivate;
    bool _usedOnceLatched;
    int _prevFrameCount = int.MinValue;

    int _prevWatchItemCount;
    bool _watchItemBaselineSeeded;
    bool _watchItemAppearHandled;

    int _prevActivateItemCount;
    bool _activateItemBaselineSeeded;
    bool _activateItemAppearHandled;

    /// <summary>trackedConsumable 未设置时视为不启用限制（恒 true）。否则为闩锁已置位；闩锁成立当帧会立刻同步 <see cref="hasUsedOnce"/>。</summary>
    public bool IsSatisfied => trackedConsumable == null || _usedOnceLatched;

    void Awake()
    {
        _inventory = GetComponent<InventoryManager>();
        if (Application.isPlaying && trackedConsumable != null && hasUsedOnce && !_usedOnceLatched)
        {
            // 只读字段可能被保存进场景；私有关锁进 Play 会丢失。只要场景里记着「已用过」，就恢复闩锁与峰值下限，避免 IsSatisfied 与 Inspector 不一致。
            _usedOnceLatched = true;
            _peakPrivate = Mathf.Max(_peakPrivate, peakCount, 1);
        }
    }

    void Update()
    {
        Tick();
    }

    void LateUpdate()
    {
        Tick();
    }

    void Tick()
    {
        if (trackedConsumable == null)
        {
            currentCount = 0;
            peakCount = 0;
            hasUsedOnce = false;
            _usedOnceLatched = false;
            _peakPrivate = 0;
            _prevFrameCount = int.MinValue;
            TickWatchItemDisableScript();
            TickActivateObjectWhenItemInInventory();
            return;
        }

        if (_inventory == null)
            _inventory = GetComponent<InventoryManager>();

        if (_inventory == null)
        {
            currentCount = 0;
            peakCount = _peakPrivate;
            hasUsedOnce = _usedOnceLatched;
            if (debugLog)
                Debug.LogWarning("[InventoryPrerequisiteTracker] 未找到 InventoryManager 组件。");
            TickWatchItemDisableScript();
            TickActivateObjectWhenItemInInventory();
            return;
        }

        int n = _inventory.GetItemCount(trackedConsumable);
        currentCount = n;

        if (n > _peakPrivate)
            _peakPrivate = n;
        peakCount = _peakPrivate;

        if (!_usedOnceLatched)
        {
            bool belowPeak = _peakPrivate >= 1 && n < _peakPrivate;
            bool droppedSinceLastProbe = _prevFrameCount != int.MinValue
                                         && _prevFrameCount >= 1
                                         && n < _prevFrameCount;

            if (belowPeak || droppedSinceLastProbe)
            {
                _usedOnceLatched = true;
                hasUsedOnce = true;
                if (debugLog)
                    Debug.Log($"[InventoryPrerequisiteTracker] 闩锁：当前={n} 峰值={_peakPrivate} 上次={_prevFrameCount}");
            }
        }

        _prevFrameCount = n;
        hasUsedOnce = _usedOnceLatched;

        TickWatchItemDisableScript();
        TickActivateObjectWhenItemInInventory();
    }

    void TickWatchItemDisableScript()
    {
        if (watchItemWhenInInventory == null || disableWhenWatchObjectAppears == null || _watchItemAppearHandled)
            return;

        if (_inventory == null)
            _inventory = GetComponent<InventoryManager>();
        if (_inventory == null)
            return;

        int c = _inventory.GetItemCount(watchItemWhenInInventory);

        if (!_watchItemBaselineSeeded)
        {
            _watchItemBaselineSeeded = true;
            _prevWatchItemCount = c;
            if (treatInitialItemInBackpackAsAlreadySeen && c > 0)
                _watchItemAppearHandled = true;
            if (_watchItemAppearHandled)
                return;
        }

        if (c > 0 && _prevWatchItemCount <= 0)
        {
            if (disableWhenWatchObjectAppears.enabled)
                disableWhenWatchObjectAppears.enabled = false;
            _watchItemAppearHandled = true;
            if (debugLog)
                Debug.Log(
                    $"[InventoryPrerequisiteTracker] 监视物品「{watchItemWhenInInventory.itemName}」已进入背包，已停用 {disableWhenWatchObjectAppears.GetType().Name}。");
        }

        _prevWatchItemCount = c;
    }

    void TickActivateObjectWhenItemInInventory()
    {
        if (activateWhenItemInInventory == null || objectToActivateWhenItemAppears == null || _activateItemAppearHandled)
            return;

        if (_inventory == null)
            _inventory = GetComponent<InventoryManager>();
        if (_inventory == null)
            return;

        int c = _inventory.GetItemCount(activateWhenItemInInventory);

        if (!_activateItemBaselineSeeded)
        {
            _activateItemBaselineSeeded = true;
            _prevActivateItemCount = c;
            if (treatInitialItemForActivateAsAlreadySeen && c > 0)
                _activateItemAppearHandled = true;
            if (_activateItemAppearHandled)
                return;
        }

        if (c > 0 && _prevActivateItemCount <= 0)
        {
            if (!objectToActivateWhenItemAppears.activeSelf)
                objectToActivateWhenItemAppears.SetActive(true);
            _activateItemAppearHandled = true;
            if (debugLog)
                Debug.Log(
                    $"[InventoryPrerequisiteTracker] 监视物品「{activateWhenItemInInventory.itemName}」已进入背包，已激活「{objectToActivateWhenItemAppears.name}」。");
        }

        _prevActivateItemCount = c;
    }
}
