using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// 单例常驻：音量与 PlayerPrefs 的唯一入口。首场景放一个空物体挂上即可，DontDestroyOnLoad。
/// <see cref="VolumeChannelSlider"/> 读写本管理器；切关后 <see cref="OnSceneLoaded"/> 会再从磁盘读入并刷到 Mixer。
/// Sfx 支持子标签：<see cref="GetSfxTagLinear"/> / <see cref="SetSfxTagLinear"/>；播放时用 <see cref="GetEffectiveSfxVolume"/> 或 <see cref="PlaySfx2D"/>。
/// </summary>
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    /// <summary>切关并 <see cref="ReloadFromDiskAndApply"/> 之后触发，供 UI 刷新 Slider。</summary>
    public static event Action VolumesReapplied;

    [Header("可选：总线混音器（需在 Mixer 里暴露 4 个 Volume 参数名并填到下方）")]
    public AudioMixer audioMixer;

    [Tooltip("与 VolumeChannel 对应：Master, Music, Sfx, Ambient")]
    public string exposedParameterMaster = "MasterVolume";
    public string exposedParameterMusic = "MusicVolume";
    public string exposedParameterSfx = "SfxVolume";
    public string exposedParameterAmbient = "AmbientVolume";

    [Header("无 Mixer 时：仅 Master 可映射到 AudioListener.volume")]
    public bool mapMasterToAudioListenerWhenNoMixer = true;

    [Header("音效子标签（可选：预填便于在 Inspector 里看到；不填也可运行时由 Slider/代码创建）")]
    [Tooltip("仅用于在编辑器里展示/预注册标签名，运行时会与 PlayerPrefs 里已保存的标签合并")]
    public string[] editorSfxTagHints = Array.Empty<string>();

    [Tooltip("PlaySfx2D 使用的 AudioSource 的输出组；不填则用默认输出（若已在总线上可留空）")]
    public AudioMixerGroup sfxOneShotOutputMixerGroup;

    const string PpMaster = "AudioVol_Master";
    const string PpMusic = "AudioVol_Music";
    const string PpSfx = "AudioVol_Sfx";
    const string PpAmbient = "AudioVol_Ambient";
    const string PpSfxTagKeys = "AudioVol_SfxTagKeys";

    static string SfxTagPrefKey(string sanitizedTag) => "AudioVol_SfxTag_" + sanitizedTag;

    float _master = 1f;
    float _music = 1f;
    float _sfx = 1f;
    float _ambient = 1f;

    readonly Dictionary<string, float> _sfxTagLinear = new Dictionary<string, float>(StringComparer.Ordinal);
    AudioSource _sfxOneShotSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        MigrateLegacyUiPrefsToAmbient();
        EnsureSfxOneShotSource();
        ReloadFromDiskAndApply();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReloadFromDiskAndApply();
    }

    void EnsureSfxOneShotSource()
    {
        if (_sfxOneShotSource != null)
            return;

        // 必须与 BackgroundMusicPlayer 等使用的 BGM AudioSource 分开：同一物体上若共用一个
        // AudioSource，长循环 BGM 与 PlayOneShot 叠在一起容易听不到或表现异常。
        const string childName = "SfxOneShotPlayer";
        Transform child = transform.Find(childName);
        if (child == null)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        _sfxOneShotSource = child.GetComponent<AudioSource>();
        if (_sfxOneShotSource == null)
            _sfxOneShotSource = child.gameObject.AddComponent<AudioSource>();

        _sfxOneShotSource.playOnAwake = false;
        _sfxOneShotSource.loop = false;
        _sfxOneShotSource.spatialBlend = 0f;
        _sfxOneShotSource.volume = 1f;
        if (sfxOneShotOutputMixerGroup != null)
            _sfxOneShotSource.outputAudioMixerGroup = sfxOneShotOutputMixerGroup;
    }

    /// <summary>从 PlayerPrefs 读入内存并写到 Mixer / Listener（进关、切关后调用）。</summary>
    public void ReloadFromDiskAndApply()
    {
        _master = PlayerPrefs.GetFloat(PpMaster, 1f);
        _music = PlayerPrefs.GetFloat(PpMusic, 1f);
        _sfx = PlayerPrefs.GetFloat(PpSfx, 1f);
        _ambient = PlayerPrefs.GetFloat(PpAmbient, 1f);
        ReloadSfxTagsFromPrefs();
        PushAllToMixerFromCache();
        VolumesReapplied?.Invoke();
    }

    /// <summary>总 Sfx × 子标签（子标签为空则只乘总 Sfx）。用于给 AudioSource 或 PlayOneShot 算最终系数。</summary>
    public float GetEffectiveSfxVolume(string sfxTag = null)
    {
        float tagMul = string.IsNullOrEmpty(sfxTag) ? 1f : GetSfxTagLinear(sfxTag);
        return Mathf.Clamp01(_sfx * tagMul);
    }

    /// <summary>子标签独立音量 0~1（默认 1）。与总 <see cref="VolumeChannel.Sfx"/> 相乘。</summary>
    public float GetSfxTagLinear(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return 1f;
        string key = SanitizeSfxTagKey(tag);
        return _sfxTagLinear.TryGetValue(key, out float v) ? v : 1f;
    }

    public void SetSfxTagLinear(string tag, float linear01, bool savePrefs = true)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;
        string key = SanitizeSfxTagKey(tag);
        linear01 = Mathf.Clamp01(linear01);
        _sfxTagLinear[key] = linear01;

        if (savePrefs)
        {
            PlayerPrefs.SetFloat(SfxTagPrefKey(key), linear01);
            RegisterSfxTagKey(key);
            PlayerPrefs.Save();
        }

        VolumesReapplied?.Invoke();
    }

    /// <summary>在二维/ UI 等处播放短音效；volumeScale 为在有效 Sfx 音量之上的额外倍率。</summary>
    public void PlaySfx2D(AudioClip clip, string sfxTag = null, float volumeScale = 1f)
    {
        if (clip == null)
            return;
        EnsureSfxOneShotSource();
        if (sfxOneShotOutputMixerGroup != null)
            _sfxOneShotSource.outputAudioMixerGroup = sfxOneShotOutputMixerGroup;
        float v = Mathf.Clamp01(volumeScale * GetEffectiveSfxVolume(sfxTag));
        _sfxOneShotSource.PlayOneShot(clip, v);
    }

    public float GetChannelLinear(VolumeChannel ch)
    {
        switch (ch)
        {
            case VolumeChannel.Master: return _master;
            case VolumeChannel.Music: return _music;
            case VolumeChannel.Sfx: return _sfx;
            default: return _ambient;
        }
    }

    public void SetChannelLinear(VolumeChannel ch, float linear01, bool savePrefs = true)
    {
        linear01 = Mathf.Clamp01(linear01);
        switch (ch)
        {
            case VolumeChannel.Master: _master = linear01; break;
            case VolumeChannel.Music: _music = linear01; break;
            case VolumeChannel.Sfx: _sfx = linear01; break;
            default: _ambient = linear01; break;
        }

        if (savePrefs)
        {
            PlayerPrefs.SetFloat(PrefKey(ch), linear01);
            PlayerPrefs.Save();
        }

        ApplyOneToMixer(ch, linear01);
        VolumesReapplied?.Invoke();
    }

    void PushAllToMixerFromCache()
    {
        ApplyOneToMixer(VolumeChannel.Master, _master);
        ApplyOneToMixer(VolumeChannel.Music, _music);
        ApplyOneToMixer(VolumeChannel.Sfx, _sfx);
        ApplyOneToMixer(VolumeChannel.Ambient, _ambient);
    }

    void ApplyOneToMixer(VolumeChannel ch, float linear01)
    {
        string param = ExposedName(ch);
        if (audioMixer != null && !string.IsNullOrEmpty(param))
            audioMixer.SetFloat(param, LinearToDecibels(linear01));

        if (ch == VolumeChannel.Master && audioMixer == null && mapMasterToAudioListenerWhenNoMixer)
            AudioListener.volume = linear01;
    }

    static string PrefKey(VolumeChannel ch)
    {
        switch (ch)
        {
            case VolumeChannel.Master: return PpMaster;
            case VolumeChannel.Music: return PpMusic;
            case VolumeChannel.Sfx: return PpSfx;
            default: return PpAmbient;
        }
    }

    string ExposedName(VolumeChannel ch)
    {
        switch (ch)
        {
            case VolumeChannel.Master: return exposedParameterMaster;
            case VolumeChannel.Music: return exposedParameterMusic;
            case VolumeChannel.Sfx: return exposedParameterSfx;
            default: return exposedParameterAmbient;
        }
    }

    static float LinearToDecibels(float linear)
    {
        return linear > 0.0001f ? 20f * Mathf.Log10(linear) : -80f;
    }

    void MigrateLegacyUiPrefsToAmbient()
    {
        const string legacyUi = "AudioVol_Ui";
        if (PlayerPrefs.HasKey(legacyUi) && !PlayerPrefs.HasKey(PpAmbient))
        {
            PlayerPrefs.SetFloat(PpAmbient, PlayerPrefs.GetFloat(legacyUi, 1f));
            PlayerPrefs.Save();
        }
    }

    void ReloadSfxTagsFromPrefs()
    {
        _sfxTagLinear.Clear();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string k in LoadSfxTagKeyListFromPrefs())
            keys.Add(k);
        if (editorSfxTagHints != null)
        {
            foreach (string hint in editorSfxTagHints)
            {
                if (!string.IsNullOrWhiteSpace(hint))
                    keys.Add(SanitizeSfxTagKey(hint));
            }
        }

        foreach (string key in keys)
        {
            float def = 1f;
            _sfxTagLinear[key] = PlayerPrefs.GetFloat(SfxTagPrefKey(key), def);
        }
    }

    static string SanitizeSfxTagKey(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return string.Empty;
        var t = tag.Trim();
        var chars = t.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (!(char.IsLetterOrDigit(c) || c == '_'))
                chars[i] = '_';
        }
        return new string(chars);
    }

    void RegisterSfxTagKey(string sanitizedKey)
    {
        var list = LoadSfxTagKeyListFromPrefs();
        if (!list.Contains(sanitizedKey))
        {
            list.Add(sanitizedKey);
            SaveSfxTagKeyList(list);
        }
    }

    static List<string> LoadSfxTagKeyListFromPrefs()
    {
        var list = new List<string>();
        string raw = PlayerPrefs.GetString(PpSfxTagKeys, string.Empty);
        if (string.IsNullOrEmpty(raw))
            return list;
        string[] parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string p in parts)
        {
            if (!string.IsNullOrEmpty(p) && !list.Contains(p))
                list.Add(p);
        }
        return list;
    }

    static void SaveSfxTagKeyList(List<string> keys)
    {
        PlayerPrefs.SetString(PpSfxTagKeys, string.Join("|", keys));
    }
}
