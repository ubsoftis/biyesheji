using UnityEngine;

public class Click3DToToggleUI : MonoBehaviour
{
    [Header("点击3D物体要控制的UI面板")]
    public GameObject targetUI;

    [Header("设置")]
    public bool startHidden = true;
    public bool closeOtherUI = true;

    [Header("音效（可选，走 AudioManager 总 Sfx）")]
    [Tooltip("成功点开/关 UI 时播放；不拖则不播")]
    public AudioClip toggleClickSfx;
    [Range(0f, 2f)]
    public float sfxVolumeScale = 1f;

    bool isUIVisible = false;

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
            if (Camera.main == null)
                return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 允许 Collider 挂在子物体上（脚本在父级路牌上时常用）
                Transform hitT = hit.collider.transform;
                if (hitT == transform || hitT.IsChildOf(transform))
                    ToggleUI();
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

        if (toggleClickSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx2D(toggleClickSfx, null, sfxVolumeScale);
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