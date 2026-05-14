using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在带 <see cref="Button"/> 的物体上：点击时播放短音效，走 <see cref="AudioManager"/> 的 Sfx（可选子标签，与 <see cref="VolumeChannelSlider"/> 的 sfxTag 对应）。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour
{
    [Header("音效")]
    public AudioClip clickClip;
    [Tooltip("与 Sfx 子标签滑条一致，例如 UI；留空则只受总 Sfx 控制")]
    public string sfxTag = "UI";
    [Range(0f, 2f)]
    public float volumeScale = 1f;

    [Header("绑定")]
    [Tooltip("勾选则自动监听本物体 Button 的 onClick；若你想只在 Inspector 里手动绑事件，可关掉并调用 PlayClick")]
    public bool autoHookButtonClick = true;

    Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (autoHookButtonClick && _button != null)
            _button.onClick.AddListener(PlayClick);
    }

    void OnDestroy()
    {
        if (_button != null && autoHookButtonClick)
            _button.onClick.RemoveListener(PlayClick);
    }

    /// <summary>可挂在 Button 的 OnClick() 列表里，或由其它脚本调用。</summary>
    public void PlayClick()
    {
        if (clickClip == null)
            return;
        if (AudioManager.Instance == null)
            return;
        string tag = string.IsNullOrWhiteSpace(sfxTag) ? null : sfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clickClip, tag, volumeScale);
    }
}
