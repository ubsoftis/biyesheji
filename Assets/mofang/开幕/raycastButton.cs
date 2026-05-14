using UnityEngine;

public class raycastButton : MonoBehaviour
{
    static readonly string[] ButtonTags = { "button1", "button2", "button3", "button4" };

    [Header("射线检测参数")]
    [Tooltip("检测距离，超过此距离不检测")]
    public float maxDistance = 100f;

    [Tooltip("检测层级")]
    public LayerMask layerMask = -1;

    [Header("调试选项")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = false;

    [Header("音效（总 Sfx 总控；不走 Sfx 子标签）")]
    [Tooltip("所有射线按钮共用的点击音")]
    public AudioClip clickSfx;
    [Range(0f, 2f)]
    public float volumeScale = 1f;

    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            if (mainCamera == null)
                Debug.LogError("未找到摄像机！请确保场景中有摄像机。");
        }
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (showDebugInfo)
            Debug.Log("检测到鼠标点击！");

        if (mainCamera == null)
        {
            Debug.LogError("摄像机未初始化！");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        {
            if (showDebugInfo)
                Debug.Log("射线未命中任何物体！");
            return;
        }

        Collider col = hit.collider;
        if (col == null)
        {
            if (showDebugInfo)
                Debug.Log("命中的物体没有 Collider！");
            return;
        }

        if (!col.enabled)
        {
            if (showDebugInfo)
                Debug.Log($"Collider 被禁用: {col.gameObject.name}");
            return;
        }

        if (!col.gameObject.activeInHierarchy)
        {
            if (showDebugInfo)
                Debug.Log($"物体未激活: {col.gameObject.name}");
            return;
        }

        // Collider 往往在子物体，Tag 在父物体：从命中点向上找 button1~4
        string t = ResolveRaycastButtonTag(col);
        if (showDebugInfo)
            Debug.Log($"命中 Collider: {col.gameObject.name}, 解析到的按钮 Tag: {t}");

        switch (t)
        {
            case "button1":
                OnButton1Hit();
                break;
            case "button2":
                OnButton2Hit();
                break;
            case "button3":
                OnButton3Hit();
                break;
            case "button4":
                OnButton4Hit();
                break;
            default:
                if (showDebugInfo)
                    Debug.Log($"未识别为射线按钮（需要 Tag 为 button1~4，或在父级上）: {t}");
                break;
        }
    }

    /// <summary>从 Collider 所在节点一直查到根，匹配第一个带 button1~4 的 Tag。</summary>
    static string ResolveRaycastButtonTag(Collider col)
    {
        for (Transform tr = col.transform; tr != null; tr = tr.parent)
        {
            string tag = tr.tag;
            for (int i = 0; i < ButtonTags.Length; i++)
            {
                if (tag == ButtonTags[i])
                    return ButtonTags[i];
            }
        }
        return col.tag;
    }

    void PlayRaycastClick()
    {
        if (clickSfx == null)
        {
            Debug.LogWarning("[raycastButton] 未指定 clickSfx，无法播放。请在 Inspector 里拖入音效。");
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[raycastButton] 场景里没有可用的 AudioManager（Instance 为空）。请在与菜单/音量同一流程的场景里放一个带 AudioManager 的物体，并让它先进游戏（DontDestroyOnLoad）。");
            return;
        }

        float eff = AudioManager.Instance.GetEffectiveSfxVolume(null);
        if (eff < 0.001f)
            Debug.LogWarning("[raycastButton] 总 Sfx 音量为 0（或极低），听不到声音。请在音量界面把「音效 / Sfx」调高。");

        AudioManager.Instance.PlaySfx2D(clickSfx, null, volumeScale);
    }

    void OnButton1Hit()
    {
        Debug.Log("命中了 button1");
        PlayRaycastClick();
    }

    void OnButton2Hit()
    {
        Debug.Log("命中了 button2");
        PlayRaycastClick();
    }

    void OnButton3Hit()
    {
        Debug.Log("命中了 button3");
        PlayRaycastClick();
    }

    void OnButton4Hit()
    {
        Debug.Log("命中了 button4");
        PlayRaycastClick();
    }
}
