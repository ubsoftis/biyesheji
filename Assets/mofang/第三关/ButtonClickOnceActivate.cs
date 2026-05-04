using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 监听一个 <see cref="Button"/> 的点击：首次点击后将指定物体 <see cref="GameObject.SetActive"/>(true)，只执行一次。
/// 可挂在任意物体上，将「要听的按钮」与「要点亮的物体」拖入即可。
/// </summary>
public class ButtonClickOnceActivate : MonoBehaviour
{
    [Tooltip("要检测的按钮；留空则尝试在本物体上 GetComponent<Button>()。")]
    public Button watchButton;

    [Tooltip("首次点击后 SetActive(true)。可为空（仅闩锁/可选禁用按钮）。")]
    public GameObject objectToActivate;
   public GameObject objectToActivate2;
   public GameObject objectToActivate3;
   public GameObject objectToActivate4;
    public GameObject objectToActivate5;
   public GameObject objectToActivate6;
   public GameObject objectToDeactivate;
    [Tooltip("触发后是否将按钮 interactable 设为 false，防止重复点。")]
    public bool disableButtonAfterClick = true;

    [Header("状态（只读）")]
    public bool hasClicked;

    bool _fired;

    void Awake()
    {
        if (watchButton == null)
            watchButton = GetComponent<Button>();
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

    void OnButtonClicked()
    {
        if (_fired)
            return;

        _fired = true;
        hasClicked = true;

        if (objectToActivate != null && !objectToActivate.activeSelf)
            objectToActivate.SetActive(true);

        if (objectToActivate2 != null && !objectToActivate2.activeSelf)
            objectToActivate2.SetActive(true);

        if (objectToActivate3 != null && !objectToActivate3.activeSelf)
            objectToActivate3.SetActive(true);

        if (objectToActivate4 != null && !objectToActivate4.activeSelf)
            objectToActivate4.SetActive(true);

        if (objectToActivate5 != null && !objectToActivate5.activeSelf)
            objectToActivate5.SetActive(true);

        if (objectToActivate6 != null && !objectToActivate6.activeSelf)
            objectToActivate6.SetActive(true);

        if (objectToDeactivate != null && objectToDeactivate.activeSelf)
            objectToDeactivate.SetActive(false);

        if (disableButtonAfterClick && watchButton != null)
            watchButton.interactable = false;
    }
}
