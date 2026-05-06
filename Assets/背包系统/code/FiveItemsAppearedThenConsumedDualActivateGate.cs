using UnityEngine;

/// <summary>
/// 参照 <see cref="InventoryRequiredFiveItems"/> 的 5 个 <see cref="ItemSO"/> 配置方式（挂在 <see cref="InventoryManager"/> 同一物体上）：
/// 当五种物品都曾经在背包里出现过（任意时刻数量 &gt; 0），且<strong>当前</strong>五种在背包中的数量<strong>全部为 0</strong>（视为都已用掉）时，
/// 一次性将两个目标物体 <see cref="GameObject.SetActive"/>(true) 并闩锁，之后不再检测。
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(InventoryManager))]
public class FiveItemsAppearedThenConsumedDualActivateGate : MonoBehaviour
{
    [Tooltip("五个物品（拖入 ItemSO 资源）；每一种都必须「曾进过包」且「当前数量为 0」才会满足条件。")]
    public ItemSO[] requiredItems = new ItemSO[5];

    [Tooltip("满足条件时 SetActive(true)。可为空。")]
    public GameObject objectToActivateA;

    [Tooltip("满足条件时 SetActive(true)。可为空。")]
    public GameObject objectToActivateB;

    [Header("状态（只读）")]
    [Tooltip("每种物品是否曾在背包中数量 > 0（本局内）")]
    public bool[] debugEverHadPositive = new bool[5];

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
                "[FiveItemsAppearedThenConsumedDualActivateGate] 五种物品均已出现过且当前背包中数量均为 0，已激活 A/B。");
        }
    }

    bool AllSatisfied()
    {
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
