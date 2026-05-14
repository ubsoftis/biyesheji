using UnityEngine;
using UnityEngine.UI;

/// <summary>与 <see cref="Screen.fullScreenMode"/> / PlayerPrefs 的联动方式。</summary>
public enum SimpleDropdownScreenBind
{
    [InspectorName("全屏/窗口（联动屏显模式）")]
    [Tooltip("启动时按当前 Screen.fullScreenMode 刷新文案；点选项会切换模式并存档。")]
    FullScreenMode,
    [InspectorName("仅改下拉文案（分辨率等）")]
    [Tooltip("不按全屏模式同步「当前选中」文案，避免分辨率第二项被误显示；点选项只改显示文字。")]
    None
}

/// <summary>
/// 简易自定义下拉：箭头展开选项，点选后更新 <see cref="currentText"/>。
/// 可选：为每个 <see cref="options"/> 下标配置 <see cref="screenModeForEachOption"/>，用于全屏 / 窗口化并写入 PlayerPrefs。
/// </summary>
public class SimpleDropdown : MonoBehaviour
{
    [Header("=== 下拉设置 ===")]
    [InspectorName("屏幕联动（分辨率选「仅改下拉文案」）")]
    [Tooltip("全屏/窗口下拉选第一项；选分辨率的那组 UI 选第二项，否则启动时可能把第二档分辨率误显示成「当前」。")]
    public SimpleDropdownScreenBind screenBind = SimpleDropdownScreenBind.FullScreenMode;

    public Text currentText;      // 显示当前选择的文字
    public GameObject dropList;   // 下拉列表面板（默认隐藏）
    public Button arrowBtn;       // 箭头按钮
    public Button[] options;      // 选项按钮

    [Header("—— 显示模式：全屏 / 窗口化 ——")]
    [Tooltip("勾选后，选中选项时会切换 Screen.fullScreenMode；请把下面数组 Size 设为与 options 相同（一般为 2）")]
    public bool applyDisplayModeControl = true;
    [Tooltip("与 options 下标一一对应：Element0=第一个按钮(全屏)、Element1=第二个(窗口化)。留空或 Size=0 则不切换。")]
    public FullScreenMode[] screenModeForEachOption =
    {
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed
    };
    [Tooltip("PlayerPrefs 键，用于下次启动恢复显示模式")]
    public string displayModePlayerPrefsKey = "UiDisplay_FullScreenMode";

    [Header("音效（可选）")]
    public AudioClip listToggleSfx;
    [Tooltip("选中一项时播放；不填则用 listToggleSfx")]
    public AudioClip optionSelectSfx;
    [Tooltip("Sfx 子标签，留空仅用总 Sfx")]
    public string sfxTag = "";

    const int PrefsUnset = -9999;

    bool isOpen = false;

    void Start()
    {
        arrowBtn.onClick.AddListener(OnArrowClick);

        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            options[i].onClick.AddListener(() => OnSelectOption(index, GetOptionLabel(options[index])));
        }

        // 分辨率模式：禁止选第二项（禁用按钮 + 回调兜底）
        if (screenBind == SimpleDropdownScreenBind.None && options != null && options.Length > 1 && options[1] != null)
            options[1].interactable = false;

        if (HasScreenModeMapping())
            RestoreOrSyncDisplayMode();
    }

    static string GetOptionLabel(Button optionButton)
    {
        if (optionButton == null)
            return string.Empty;
        var t = optionButton.GetComponentInChildren<Text>();
        return t != null ? t.text : string.Empty;
    }

    bool HasScreenModeMapping()
    {
        if (screenBind != SimpleDropdownScreenBind.FullScreenMode)
            return false;
        if (!applyDisplayModeControl || options == null || options.Length == 0)
            return false;
        return screenModeForEachOption != null
            && screenModeForEachOption.Length > 0
            && screenModeForEachOption.Length == options.Length;
    }

    /// <summary>启动时：有存档则应用；再根据当前 <see cref="Screen.fullScreenMode"/> 同步显示文字。</summary>
    void RestoreOrSyncDisplayMode()
    {
        if (!string.IsNullOrEmpty(displayModePlayerPrefsKey) && PlayerPrefs.HasKey(displayModePlayerPrefsKey))
        {
            int raw = PlayerPrefs.GetInt(displayModePlayerPrefsKey, PrefsUnset);
            if (raw != PrefsUnset && System.Enum.IsDefined(typeof(FullScreenMode), raw))
                Screen.fullScreenMode = (FullScreenMode)raw;
        }

        SyncCurrentTextToScreenMode();
    }

    void SyncCurrentTextToScreenMode()
    {
        FullScreenMode mode = Screen.fullScreenMode;
        for (int i = 0; i < options.Length && i < screenModeForEachOption.Length; i++)
        {
            if (screenModeForEachOption[i] != mode)
                continue;
            string label = GetOptionLabel(options[i]);
            if (currentText != null && !string.IsNullOrEmpty(label))
                currentText.text = label;
            return;
        }
    }

    void OnArrowClick()
    {
        isOpen = !isOpen;
        dropList.SetActive(isOpen);

        arrowBtn.transform.rotation = Quaternion.Euler(0, 0, isOpen ? 180 : 0);

        PlaySfxIfConfigured(listToggleSfx);
    }

    void OnSelectOption(int optionIndex, string text)
    {
        if (screenBind == SimpleDropdownScreenBind.None && optionIndex == 1)
            return;

        var clip = optionSelectSfx != null ? optionSelectSfx : listToggleSfx;
        PlaySfxIfConfigured(clip);

        currentText.text = text;

        isOpen = false;
        dropList.SetActive(false);
        arrowBtn.transform.rotation = Quaternion.identity;

        ApplyScreenModeForOption(optionIndex);
    }

    void ApplyScreenModeForOption(int optionIndex)
    {
        if (!HasScreenModeMapping())
            return;
        if (optionIndex < 0 || optionIndex >= screenModeForEachOption.Length || optionIndex >= options.Length)
            return;

        FullScreenMode mode = screenModeForEachOption[optionIndex];
        Screen.fullScreenMode = mode;

        if (!string.IsNullOrEmpty(displayModePlayerPrefsKey))
        {
            PlayerPrefs.SetInt(displayModePlayerPrefsKey, (int)mode);
            PlayerPrefs.Save();
        }
    }

    void PlaySfxIfConfigured(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;
        string tag = string.IsNullOrWhiteSpace(sfxTag) ? null : sfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clip, tag);
    }
}
