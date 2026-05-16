using UnityEngine;
using UnityEngine.UI;

public class CanvasPanelManager : MonoBehaviour
{
    [Header("请把需要置顶显示的 Panel 拖到这里")]
    [SerializeField] private GameObject targetPanel;

    [Header("请把该 Panel 里的退出/关闭按钮拖到这里")]
    [SerializeField] private Button closeButton;

    [Header("置顶设置")]
    [SerializeField] private int topSortingOrder = 999;

    [Header("拖入需要显示在 Panel 之上的元素")]
    [SerializeField] private RectTransform elementToPlaceAbovePanel;

    // 内部变量：用来保存按钮最干净的母体备份
    private GameObject buttonPrefabBackup;
    private Transform buttonParent;
    private Vector3 buttonPosition;
    private Quaternion buttonRotation;
    private Vector3 buttonScale;

    private Canvas panelCanvas;
    private GraphicRaycaster panelRaycaster;

    private void Awake()
    {
        if (targetPanel == null || closeButton == null)
        {
            Debug.LogError($"[{gameObject.name}] 基础组件未拖拽完整！", gameObject);
            return;
        }

        // 动态检查或添加置顶组件
        panelCanvas = targetPanel.GetComponent<Canvas>();
        if (panelCanvas == null) panelCanvas = targetPanel.AddComponent<Canvas>();

        panelRaycaster = targetPanel.GetComponent<GraphicRaycaster>();
        if (panelRaycaster == null) panelRaycaster = targetPanel.AddComponent<GraphicRaycaster>();

        // 【核心准备】：在游戏刚启动、动画还没卡死的第一时间，把这个干净的按钮“复制一份”存进内存作为母体
        buttonParent = closeButton.transform.parent;
        buttonPosition = closeButton.transform.localPosition;
        buttonRotation = closeButton.transform.localRotation;
        buttonScale = closeButton.transform.localScale;

        // 复制并隐藏母体备份
        buttonPrefabBackup = Instantiate(closeButton.gameObject, this.transform);
        buttonPrefabBackup.name = "CloseButton_Backup_DoNotDelete";
        buttonPrefabBackup.SetActive(false);

        // 绑定初始按钮的事件
        closeButton.onClick.AddListener(HideTopPanel);

        // 初始化：默认关闭 Panel
        targetPanel.SetActive(false);
    }

    /// <summary>
    /// 打开面板
    /// </summary>
    public void OpenAndTriggerTopPanel()
    {
        if (targetPanel == null) return;

        // 1. 激活面板
        targetPanel.SetActive(true);

        // 2. 强行置顶
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = topSortingOrder;
        panelRaycaster.enabled = true;

        // 3. 处理外部元素置顶
        if (elementToPlaceAbovePanel != null)
        {
            MoveElementAbovePanel();
        }

        // 4. 【终极降维打击】：毁灭旧按钮，克隆全新按钮
        RecreateCleanCloseButton();
    }

    /// <summary>
    /// 【核心克隆逻辑】
    /// </summary>
    private void RecreateCleanCloseButton()
    {
        if (buttonPrefabBackup == null) return;

        // 1. 如果场上有旧的、卡死的按钮，直接无情毁灭
        if (closeButton != null)
        {
            Destroy(closeButton.gameObject);
        }

        // 2. 从内存里那尊完美的“神像备份”里克隆一具全新的身体
        GameObject newButtonObj = Instantiate(buttonPrefabBackup, buttonParent);

        // 3. 还原它的名字和位置属性
        newButtonObj.name = buttonPrefabBackup.name.Replace("_Backup_DoNotDelete", "");
        newButtonObj.transform.localPosition = buttonPosition;
        newButtonObj.transform.localRotation = buttonRotation;
        newButtonObj.transform.localScale = buttonScale;

        // 4. 重新让它显示出来
        newButtonObj.SetActive(true);

        // 5. 重新获取新按钮的 Button 组件并绑定点击退出事件
        closeButton = newButtonObj.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideTopPanel);
        }

        Debug.Log("【系统】已对退出键执行克隆转世，新动画已强制从第一帧加载！");
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    public void HideTopPanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }

    private void MoveElementAbovePanel()
    {
        RectTransform panelRect = targetPanel.GetComponent<RectTransform>();
        if (panelRect == null || elementToPlaceAbovePanel == null) return;

        elementToPlaceAbovePanel.SetParent(panelRect);
        elementToPlaceAbovePanel.SetAsLastSibling();
        elementToPlaceAbovePanel.gameObject.SetActive(true);
    }
}