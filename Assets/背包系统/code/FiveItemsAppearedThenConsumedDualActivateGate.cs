using UnityEngine;

/// <summary>
/// 参照 <see cref="InventoryRequiredFiveItems"/> 的 5 个 <see cref="ItemSO"/> 配置方式（挂在 <see cref="InventoryManager"/> 同一物体上）：
/// 满足 <see cref="activateWhen"/> 条件后，一次性将两个目标物体 <see cref="GameObject.SetActive"/>(true) 并闩锁。
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(InventoryManager))]
public class FiveItemsAppearedThenConsumedDualActivateGate : MonoBehaviour
{
    public enum ActivateWhen
    {
        [Tooltip("五种物品当前都在背包里（每种数量 ≥ 1），与 InventoryRequiredFiveItems 一致")]
        AllFiveInInventory = 0,

        [Tooltip("五种物品都曾进过包，且当前背包中五种数量全部为 0（视为都已用掉）")]
        AllFiveEverHadThenEmpty = 1,
    }

    [Tooltip("何时触发激活 A/B")]
    public ActivateWhen activateWhen = ActivateWhen.AllFiveInInventory;

    [Tooltip("五个物品（拖入 ItemSO 资源）")]
    public ItemSO[] requiredItems = new ItemSO[5];

    [Tooltip("满足条件时 SetActive(true)。可为空。")]
    public GameObject objectToActivateA;

    [Tooltip("满足条件时 SetActive(true)。可为空。")]
    public GameObject objectToActivateB;

    [Header("状态（只读）")]
    [Tooltip("每种物品是否曾在背包中数量 > 0（本局内；仅 AllFiveEverHadThenEmpty 模式有意义）")]
    public bool[] debugEverHadPositive = new bool[5];

    [Tooltip("只读：当前背包中各指定物品数量")]
    public int[] debugCurrentCounts = new int[5];

    [Tooltip("是否已执行过激活（闩锁）")]
    public bool hasActivatedTargets;

    [Header("调试")]
    public bool debugLog;

    InventoryManager _inventory;
    readonly bool[] _everHadPositiveCount = new bool[5];
    bool _activatedLatched;

    void Awake()
    {
        _inventory = GetComponent<InventoryManager>();
    }

    void OnValidate()
    {
        if (requiredItems == null || requiredItems.Length != 5)
        {
            requiredItems = new ItemSO[5];
        }

        if (debugEverHadPositive == null || debugEverHadPositive.Length != 5)
        {
            debugEverHadPositive = new bool[5];
        }

        if (debugCurrentCounts == null || debugCurrentCounts.Length != 5)
        {
            debugCurrentCounts = new int[5];
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
        if (_activatedLatched)
        {
            return;
        }

        if (_inventory == null)
        {
            _inventory = GetComponent<InventoryManager>();
        }

        if (_inventory == null)
        {
            return;
        }

        if (requiredItems == null || requiredItems.Length != 5)
        {
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            if (requiredItems[i] == null)
            {
                return;
            }
        }

        for (int i = 0; i < 5; i++)
        {
            int n = _inventory.GetItemCount(requiredItems[i]);
            debugCurrentCounts[i] = n;
            if (n > 0)
            {
                _everHadPositiveCount[i] = true;
            }
        }

        SyncDebugArray();

        if (!AllSatisfied())
        {
            return;
        }

        if (objectToActivateA != null && !objectToActivateA.activeSelf)
        {
            objectToActivateA.SetActive(true);
        }

        if (objectToActivateB != null && !objectToActivateB.activeSelf)
        {
            objectToActivateB.SetActive(true);
        }

        _activatedLatched = true;
        hasActivatedTargets = true;

        if (debugLog)
        {
            Debug.Log(
                activateWhen == ActivateWhen.AllFiveInInventory
                    ? "[FiveItemsAppearedThenConsumedDualActivateGate] 五种物品当前均在背包中，已激活 A/B。"
                    : "[FiveItemsAppearedThenConsumedDualActivateGate] 五种物品均已出现过且当前背包中数量均为 0，已激活 A/B。");
        }
    }

    bool AllSatisfied()
    {
        if (activateWhen == ActivateWhen.AllFiveInInventory)
        {
            for (int i = 0; i < 5; i++)
            {
                if (_inventory.GetItemCount(requiredItems[i]) < 1)
                {
                    return false;
                }
            }

            return true;
        }

        for (int i = 0; i < 5; i++)
        {
            if (!_everHadPositiveCount[i])
            {
                return false;
            }

            if (_inventory.GetItemCount(requiredItems[i]) != 0)
            {
                return false;
            }
        }

        return true;
    }

    void SyncDebugArray()
    {
        for (int i = 0; i < 5; i++)
        {
            debugEverHadPositive[i] = _everHadPositiveCount[i];
        }
    }
}
