using UnityEngine;

/// <summary>
/// 与 <see cref="FiveItemsAppearedThenConsumedDualActivateGate"/> 分离的「集齐流程」：
/// 当五种指定物品<strong>当前都在背包中</strong>（每种数量 ≥ 1）时，一次性激活目标并闩锁。
/// </summary>
[DefaultExecutionOrder(-49)]
[RequireComponent(typeof(InventoryManager))]
public class FiveItemsInInventoryActivateGate : MonoBehaviour
{
    [Tooltip("五个物品（拖入 ItemSO 资源）；须与 InventoryRequiredFiveItems / 消耗门控使用同一组。")]
    public ItemSO[] requiredItems = new ItemSO[5];

    [Tooltip("集齐五种时 SetActive(true)。第三关：对话提示UI 主角。")]
    public GameObject objectToActivate;

    [Tooltip("集齐五种时 SetActive(true)。第三关：医生。")]
    public GameObject objectToActivateOptional;

    [Header("状态（只读）")]
    public int[] debugCurrentCounts = new int[5];

    [Tooltip("是否已执行过激活（闩锁）")]
    public bool hasActivatedTargets;

    [Header("调试")]
    public bool debugLog;

    InventoryManager _inventory;
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

        if (_inventory == null || requiredItems == null || requiredItems.Length != 5)
        {
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            if (requiredItems[i] == null)
            {
                return;
            }

            debugCurrentCounts[i] = _inventory.GetItemCount(requiredItems[i]);
            if (debugCurrentCounts[i] < 1)
            {
                return;
            }
        }

        ActivateIfNeeded(objectToActivate);
        ActivateIfNeeded(objectToActivateOptional);

        _activatedLatched = true;
        hasActivatedTargets = true;

        if (debugLog)
        {
            Debug.Log("[FiveItemsInInventoryActivateGate] 五种物品当前均在背包中，已激活集齐目标。");
        }
    }

    static void ActivateIfNeeded(GameObject go)
    {
        if (go != null && !go.activeSelf)
        {
            go.SetActive(true);
        }
    }
}
