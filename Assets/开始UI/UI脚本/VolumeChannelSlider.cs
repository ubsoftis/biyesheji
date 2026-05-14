using UnityEngine;
using UnityEngine.UI;

public enum VolumeChannel
{
    Master,
    Music,
    Sfx,
    /// <summary>环境音（与 Sfx 分开：Mixer 里用独立 Group + 暴露参数）</summary>
    Ambient
}

/// <summary>
/// 分别挂在 Slider 上，Inspector 指定 VolumeChannel。
/// 当 channel 为 Sfx 且 <see cref="sfxTag"/> 非空时，该滑条只控制该子标签音量（与总 Sfx 相乘）；留空则控制总 Sfx。
/// 与常驻 <see cref="AudioManager"/> 同步；切关后管理器会触发 <see cref="AudioManager.VolumesReapplied"/> 刷新滑条。
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeChannelSlider : MonoBehaviour
{
    public VolumeChannel channel = VolumeChannel.Master;

    [Tooltip("仅当 channel 为 Sfx 时生效：填写子标签名（如 UI、战斗）；留空表示控制总 Sfx 音量")]
    public string sfxTag = "";

    Slider _slider;
    bool _internal;

    void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    void OnEnable()
    {
        if (_slider == null)
            _slider = GetComponent<Slider>();
        _slider.onValueChanged.AddListener(OnSliderChanged);
        AudioManager.VolumesReapplied += RefreshFromManager;
        RefreshFromManager();
    }

    void OnDisable()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
        AudioManager.VolumesReapplied -= RefreshFromManager;
    }

    void Start()
    {
        RefreshFromManager();
    }

    public void RefreshFromManager()
    {
        if (AudioManager.Instance == null || _slider == null)
            return;
        _internal = true;
        float v = channel == VolumeChannel.Sfx && !string.IsNullOrWhiteSpace(sfxTag)
            ? AudioManager.Instance.GetSfxTagLinear(sfxTag)
            : AudioManager.Instance.GetChannelLinear(channel);
        _slider.SetValueWithoutNotify(v);
        _internal = false;
    }

    void OnSliderChanged(float value)
    {
        if (_internal)
            return;
        if (AudioManager.Instance == null)
            return;
        if (channel == VolumeChannel.Sfx && !string.IsNullOrWhiteSpace(sfxTag))
            AudioManager.Instance.SetSfxTagLinear(sfxTag, value, savePrefs: true);
        else
            AudioManager.Instance.SetChannelLinear(channel, value, savePrefs: true);
    }
}
