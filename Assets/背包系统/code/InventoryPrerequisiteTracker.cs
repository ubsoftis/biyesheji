using UnityEngine;

/// <summary>
/// 挂在 <see cref="InventoryManager"/> 同一物体上，集中检测指定 <see cref="ItemSO"/>：
/// 是否曾出现在背包（峰值 ≥ 1）且之后至少被消耗过 1 个（数量相对峰值下降过，或相对上一帧下降）。
/// 满足后闩锁保持为 true。其它脚本（如关卡驱动）只读 <see cref="IsSatisfied"/> 即可。
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

    [Header("调试")]
    public bool debugLog;

    InventoryManager _inventory;
    int _peakPrivate;
    bool _usedOnceLatched;
    int _prevFrameCount = int.MinValue;

    /// <summary>trackedConsumable 未设置时视为不启用限制（恒 true）。否则为闩锁已置位；闩锁成立当帧会立刻同步 <see cref="hasUsedOnce"/>。</summary>
    public bool IsSatisfied => trackedConsumable == null || _usedOnceLatched;

    void Awake()
    {
        _inventory = GetComponent<InventoryManager>();
        if (!Application.isPlaying || trackedConsumable == null)
            return;

        // 只读字段可能被保存进场景；私有关锁进 Play 会丢失。只要场景里记着「已用过」，就恢复闩锁与峰值下限，避免 IsSatisfied 与 Inspector 不一致。
        if (hasUsedOnce && !_usedOnceLatched)
        {
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
    }
}

