using UnityEngine;

/// <summary>
/// 挂在 <see cref="InventoryManager"/> 同一物体上，监视指定 <see cref="ItemSO"/> 在背包中数量从 0 变为 ≥1，
/// 满足后一次性将两个目标 <see cref="GameObject.SetActive"/>(true)，并闩锁；其它脚本只读 <see cref="HasActivatedTargets"/> / <see cref="IsGateConsumed"/> 即可。
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(InventoryManager))]
public class ObjectAppearDualActivateGate : MonoBehaviour
{
    [Tooltip("要监视的 ItemSO（须与背包格子里的 ItemSO 为同一资源引用）。数量从 0 变为 ≥1 时触发一次。")]
    public ItemSO watchItem;

    [Tooltip("触发时 SetActive(true)。可为空。")]
    public GameObject objectToActivateA;

    [Tooltip("触发时 SetActive(true)。可为空。")]
    public GameObject objectToActivateB;

    [Tooltip("若开局背包里已有该物品（数量>0），视为已进过包，不触发激活；需先清空再获得才会触发。")]
    public bool treatInitiallyInInventoryAsAlreadySeen = true;

    [Header("状态（只读）")]
    [Tooltip("当前背包中该物品数量")]
    public int currentCount;

    [Tooltip("是否已对 A/B 执行过 SetActive(true)（本局内实际点过火）")]
    public bool hasActivatedTargets;

    [Tooltip("门控是否已结束（含：首帧已在背包且选择「视为已见过」导致的永久跳过）")]
    public bool gateConsumed;

    [Header("调试")]
    public bool debugLog;

    InventoryManager _inventory;
    bool _baselineSeeded;
    int _prevCount;
    bool _activatedLatched;
    bool _handledLatched;

    /// <summary>watchItem 未设置时视为无门控（恒 true）。否则为门控已消耗（已激活或已跳过）。</summary>
    public bool IsGateConsumed => watchItem == null || _handledLatched;

    /// <summary>与 <see cref="InventoryPrerequisiteTracker.IsSatisfied"/> 命名对齐：无监视或门控已消耗时为 true。</summary>
    public bool IsSatisfied => IsGateConsumed;

    /// <summary>与 <see cref="hasActivatedTargets"/> 对齐的只读别名。</summary>
    public bool HasActivatedTargets => _activatedLatched;

    void Awake()
    {
        _inventory = GetComponent<InventoryManager>();
        if (!Application.isPlaying || watchItem == null)
            return;

        if (hasActivatedTargets && !_activatedLatched)
            _activatedLatched = true;
        if (gateConsumed && !_handledLatched)
            _handledLatched = true;
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
        if (watchItem == null)
        {
            currentCount = 0;
            _baselineSeeded = false;
            _prevCount = 0;
            _activatedLatched = false;
            _handledLatched = false;
            hasActivatedTargets = false;
            gateConsumed = false;
            return;
        }

        if (_inventory == null)
            _inventory = GetComponent<InventoryManager>();

        if (_inventory == null)
        {
            if (debugLog)
                Debug.LogWarning("[ObjectAppearDualActivateGate] 未找到 InventoryManager 组件。");
            SyncPublicFields();
            return;
        }

        int n = _inventory.GetItemCount(watchItem);
        currentCount = n;

        if (!_baselineSeeded)
        {
            _baselineSeeded = true;
            _prevCount = n;
            if (treatInitiallyInInventoryAsAlreadySeen && n > 0)
            {
                _handledLatched = true;
                if (debugLog)
                    Debug.Log(
                        $"[ObjectAppearDualActivateGate] 开局背包已有「{watchItem.itemName}」，按设置视为已进过包，门控关闭（不会自动激活 A/B）。");
            }

            SyncPublicFields();
            return;
        }

        if (_handledLatched)
        {
            SyncPublicFields();
            return;
        }

        if (n > 0 && _prevCount <= 0)
        {
            if (objectToActivateA != null && !objectToActivateA.activeSelf)
                objectToActivateA.SetActive(true);
            if (objectToActivateB != null && !objectToActivateB.activeSelf)
                objectToActivateB.SetActive(true);

            _activatedLatched = true;
            _handledLatched = true;
            if (debugLog)
                Debug.Log(
                    $"[ObjectAppearDualActivateGate] 「{watchItem.itemName}」已进入背包，已执行一次性激活 A/B。");
        }

        _prevCount = n;
        SyncPublicFields();
    }

    void SyncPublicFields()
    {
        hasActivatedTargets = _activatedLatched;
        gateConsumed = _handledLatched;
    }
}
