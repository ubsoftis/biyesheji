using UnityEngine;
using UnityEngine.SceneManagement;

public class UILockSignManager : MonoBehaviour
{
    [Header("拖入你的两个UI父对象")]
    public GameObject ui_SetUI;
    public GameObject ui_ChapterUI;

    public static bool uiIsOpen;

    static UILockSignManager _instance;

    /// <summary>当前已加载场景里是否存在 UI 锁管理器（避免静态值在切场景后误挡加载）。</summary>
    public static bool ExistsInActiveScene()
    {
        return _instance != null;
    }

    void Awake()
    {
        _instance = this;
        uiIsOpen = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshUiLockState();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            uiIsOpen = false;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_instance != this)
            return;
        uiIsOpen = false;
        RefreshUiLockState();
    }

    void Update()
    {
        RefreshUiLockState();
    }

    void RefreshUiLockState()
    {
        bool setOpen = ui_SetUI != null && ui_SetUI.activeSelf;
        bool chapterOpen = ui_ChapterUI != null && ui_ChapterUI.activeSelf;
        uiIsOpen = setOpen || chapterOpen;
    }
}