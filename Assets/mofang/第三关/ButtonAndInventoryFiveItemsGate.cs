using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 参照 <see cref="ButtonClickOnceActivate"/>：同时满足「监听按钮被点击」与「背包中已集齐五个指定 ItemSO」时，
/// 只执行一次：<see cref="objectToDeactivate"/> SetActive(false)，两个物体 SetActive(true)。
/// 五个物品的判定由同场景的 <see cref="InventoryRequiredFiveItems"/>（挂在 InventoryManager 上）完成。
/// </summary>
public class ButtonAndInventoryFiveItemsGate : MonoBehaviour
{
    [Tooltip("要检测的按钮；留空则尝试在本物体上 GetComponent<Button>()。")]
    public Button watchButton;

    [Tooltip("挂在 InventoryManager 同一物体上的 InventoryRequiredFiveItems；留空则场景中查找。")]
    public InventoryRequiredFiveItems inventoryCheck;

    [Tooltip("条件满足后 SetActive(false)。")]
    public GameObject objectToDeactivate;

    [Tooltip("条件满足后 SetActive(true)。")]
    public GameObject objectToActivate1;

    [Tooltip("条件满足后 SetActive(true)。")]
    public GameObject objectToActivate2;

    [Tooltip("触发后是否将按钮 interactable 设为 false。")]
    public bool disableButtonAfterTrigger = true;

    [Header("状态（只读）")]
    public bool buttonHasBeenClicked;
    public bool hasTriggered;

    bool _fired;
    bool _buttonClicked;

    void Awake()
    {
        if (watchButton == null)
            watchButton = GetComponent<Button>();

        if (inventoryCheck == null)
            inventoryCheck = FindObjectOfType<InventoryRequiredFiveItems>();
    }

    void OnEnable()
    {
        if (watchButton != null)
            watchButton.onClick.AddListener(OnButtonClicked);
    }

    void OnDisable()
    {
        if (watchButton != null)
            watchButton.onClick.RemoveListener(OnButtonClicked);
    }

    void LateUpdate()
    {
        TryTrigger();
    }

    void OnButtonClicked()
    {
        _buttonClicked = true;
        buttonHasBeenClicked = true;
        TryTrigger();
    }

    void TryTrigger()
    {
        if (_fired)
            return;
        if (!_buttonClicked)
            return;
        if (inventoryCheck == null || !inventoryCheck.HasAllFiveRequired())
            return;

        _fired = true;
        hasTriggered = true;

        if (objectToDeactivate != null && objectToDeactivate.activeSelf)
            objectToDeactivate.SetActive(false);

        if (objectToActivate1 != null && !objectToActivate1.activeSelf)
            objectToActivate1.SetActive(true);

        if (objectToActivate2 != null && !objectToActivate2.activeSelf)
            objectToActivate2.SetActive(true);

        if (disableButtonAfterTrigger && watchButton != null)
            watchButton.interactable = false;
    }
}
