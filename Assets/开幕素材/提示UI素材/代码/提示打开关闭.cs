using NodeCanvas.DialogueTrees;
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

    [Header("对话中断（打开面板时）")]
    [Tooltip("指定要中断的对话控制器；不拖则中断当前正在播放的那条对话树。")]
    [SerializeField] private DialogueTreeController[] dialogueControllersToInterrupt;

    [Tooltip("立绘回归位置。拖 Hierarchy 里的「对话角色背景框」（与对话树 MoveTowards 终点相同）。不填则自动按名字查找。")]
    [SerializeField] private Transform dialoguePortraitHomeAnchor;

    [Tooltip("打开提示面板、且对话仍在进行中被打断时，额外 SetActive(true) 的对象。对话已跑完则不会激活。")]
    [SerializeField] private GameObject objectToActivateOnInterrupt;

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
        if (!EnsurePanelComponents())
            return;

        EnsureCloseButtonBackup();

        if (closeButton != null)
            closeButton.onClick.AddListener(HideTopPanel);

        targetPanel.SetActive(false);
    }

    /// <summary>确保 Panel 上的 Canvas / GraphicRaycaster 已就绪（Awake 未跑完时也可补救）。</summary>
    private bool EnsurePanelComponents()
    {
        if (targetPanel == null)
        {
            Debug.LogError($"[{gameObject.name}] targetPanel 未赋值！", gameObject);
            return false;
        }

        if (panelCanvas == null)
        {
            panelCanvas = targetPanel.GetComponent<Canvas>();
            if (panelCanvas == null)
                panelCanvas = targetPanel.AddComponent<Canvas>();
        }

        if (panelRaycaster == null)
        {
            panelRaycaster = targetPanel.GetComponent<GraphicRaycaster>();
            if (panelRaycaster == null)
                panelRaycaster = targetPanel.AddComponent<GraphicRaycaster>();
        }

        return panelCanvas != null && panelRaycaster != null;
    }

    /// <summary>备份关闭按钮，供克隆重置动画使用。</summary>
    private void EnsureCloseButtonBackup()
    {
        if (closeButton == null || buttonPrefabBackup != null)
            return;

        buttonParent = closeButton.transform.parent;
        buttonPosition = closeButton.transform.localPosition;
        buttonRotation = closeButton.transform.localRotation;
        buttonScale = closeButton.transform.localScale;

        buttonPrefabBackup = Instantiate(closeButton.gameObject, transform);
        buttonPrefabBackup.name = "CloseButton_Backup_DoNotDelete";
        buttonPrefabBackup.SetActive(false);
    }

    /// <summary>
    /// 打开面板
    /// </summary>
    public void OpenAndTriggerTopPanel()
    {
        if (!EnsurePanelComponents())
            return;

        bool didInterruptDialogue = ForceInterruptActiveDialogue();
        if (didInterruptDialogue)
            ActivateObjectsAfterDialogueInterrupt();
        EnsureCloseButtonBackup();

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

    /// <summary>
    /// 中断当前对话：先 Pause 停住字幕/Action 协程，再 Stop(false) 中止图（不当作成功跑完）。
    /// 不 SetActive 关闭 DialogueUGUI，保留事件订阅供之后重新对话。
    /// </summary>
    /// <returns>是否有正在运行的对话被中断（已跑完则返回 false）。</returns>
    private bool ForceInterruptActiveDialogue()
    {
        if (dialogueControllersToInterrupt != null && dialogueControllersToInterrupt.Length > 0)
        {
            bool interrupted = false;
            for (int i = 0; i < dialogueControllersToInterrupt.Length; i++)
                interrupted |= TryInterruptController(dialogueControllersToInterrupt[i], dialoguePortraitHomeAnchor);
            return interrupted;
        }

        DialogueTree current = DialogueTree.currentDialogue;
        if (current != null && current.isRunning)
        {
            DialogueTreeController owner = FindControllerForTree(current) ?? FindAnyRunningController();
            if (owner != null)
                return TryInterruptController(owner, dialoguePortraitHomeAnchor);

            PauseDialogueGraph(current);
            DialoguePortraitReset.ResetForTree(current, dialoguePortraitHomeAnchor, null);
            current.Stop(false);
            return true;
        }

        DialogueTreeController running = FindAnyRunningController();
        if (running != null)
            return TryInterruptController(running, dialoguePortraitHomeAnchor);

        return false;
    }

    private static bool TryInterruptController(DialogueTreeController controller, Transform portraitHomeAnchor)
    {
        if (controller == null || !controller.isRunning)
            return false;

        PauseDialogueGraph(controller);
        DialoguePortraitReset.ResetForController(controller, portraitHomeAnchor);
        controller.StopBehaviour(false);
        return true;
    }

    private void ActivateObjectsAfterDialogueInterrupt()
    {
        if (objectToActivateOnInterrupt != null)
            objectToActivateOnInterrupt.SetActive(true);
    }

    private static void PauseDialogueGraph(DialogueTreeController controller)
    {
        if (controller != null && !controller.isPaused)
            controller.PauseBehaviour();
    }

    private static void PauseDialogueGraph(DialogueTree tree)
    {
        if (tree != null && !tree.isPaused)
            tree.Pause();
    }

    private static DialogueTreeController FindControllerForTree(DialogueTree tree)
    {
        if (tree == null)
            return null;

        DialogueTreeController[] all =
            Object.FindObjectsOfType<DialogueTreeController>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].isRunning)
                continue;

            if (ReferenceEquals(all[i].graph, tree) || ReferenceEquals(all[i].behaviour, tree))
                return all[i];
        }

        return null;
    }

    private static DialogueTreeController FindAnyRunningController()
    {
        DialogueTreeController[] all =
            Object.FindObjectsOfType<DialogueTreeController>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].isRunning)
                return all[i];
        }

        return null;
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