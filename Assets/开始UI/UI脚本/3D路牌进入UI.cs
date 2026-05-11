using UnityEngine;

public class Click3DToToggleUI : MonoBehaviour
{
    [Header("点击3D物体要控制的UI面板")]
    public GameObject targetUI;

    [Header("设置")]
    public bool startHidden = true;
    public bool closeOtherUI = true;

    private bool isUIVisible = false;

    void Start()
    {
        if (targetUI != null)
        {
            isUIVisible = !startHidden;
            targetUI.SetActive(isUIVisible);
        }
    }

    void Update()
    {
        // 修正：使用正确的变量名
        if (UILockSignManager.uiIsOpen)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    ToggleUI();
                }
            }
        }
    }

    void ToggleUI()
    {
        if (targetUI == null) return;

        if (closeOtherUI)
            CloseAllMenuUI();

        isUIVisible = !isUIVisible;
        targetUI.SetActive(isUIVisible);
    }

    void CloseAllMenuUI()
    {
        Click3DToToggleUI[] all = FindObjectsOfType<Click3DToToggleUI>();
        foreach (var item in all)
        {
            if (item.targetUI != null)
                item.targetUI.SetActive(false);
        }
    }
}