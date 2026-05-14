using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 背景音乐示例：循环播放一段 BGM，与 <see cref="AudioManager"/> 的「音乐」音量配合。
/// <list type="bullet">
/// <item>若指定 <see cref="musicOutputMixerGroup"/>（Mixer 里 Music 总线下的 Group），音量由 Mixer + UI 音乐滑条控制，本脚本将 <see cref="AudioSource.volume"/> 固定为 1。</item>
/// <item>若未指定 Mixer Group（没有混音器时），用 <see cref="AudioSource.volume"/> 跟随 <see cref="VolumeChannel.Music"/>。</item>
/// </list>
/// </summary>
[DisallowMultipleComponent]
public class BackgroundMusicPlayer : MonoBehaviour
{
    [Header("播放")]
    public AudioClip musicClip;
    [Tooltip("勾选则在进入场景后自动播放 musicClip")]
    public bool playOnStart = true;

    [Header("Mixer（推荐）")]
    [Tooltip("拖到 Audio Mixer 里「音乐」对应的 Audio Mixer Group")]
    public AudioMixerGroup musicOutputMixerGroup;

    [Header("淡入淡出（秒，0 表示不用）")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 0.5f;

    AudioSource _musicSource;
    Coroutine _fadeRoutine;

    void Awake()
    {
        _musicSource = GetComponent<AudioSource>();
        if (_musicSource == null)
            _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicSource.priority = 0;
    }

    void OnEnable()
    {
        AudioManager.VolumesReapplied += OnVolumesReapplied;
        ApplyMusicLinearVolumeToSource();
    }

    void OnDisable()
    {
        AudioManager.VolumesReapplied -= OnVolumesReapplied;
    }

    void Start()
    {
        if (playOnStart && musicClip != null)
            PlayMusic(musicClip);
    }

    void OnVolumesReapplied()
    {
        ApplyMusicLinearVolumeToSource();
    }

    /// <summary>有 Mixer Group 时音乐推子由 AudioManager 驱动；否则用 Music 通道线性值乘在 AudioSource 上。</summary>
    void ApplyMusicLinearVolumeToSource()
    {
        if (_musicSource == null)
            return;
        if (musicOutputMixerGroup != null)
        {
            _musicSource.outputAudioMixerGroup = musicOutputMixerGroup;
            _musicSource.volume = 1f;
        }
        else
        {
            float linear = AudioManager.Instance != null
                ? AudioManager.Instance.GetChannelLinear(VolumeChannel.Music)
                : 1f;
            _musicSource.volume = linear;
        }
    }

    /// <summary>播放并可选淡入；切换曲目时会先淡出当前再播新片段。</summary>
    public void PlayMusic(AudioClip clip, bool useFadeIn = true)
    {
        if (clip == null)
            return;
        musicClip = clip;
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (_musicSource.isPlaying && fadeOutDuration > 0.001f)
        {
            _fadeRoutine = StartCoroutine(FadeOutThenPlay(useFadeIn));
            return;
        }

        StartClip(useFadeIn);
    }

    public void StopMusic(bool useFadeOut = true)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
        if (!useFadeOut || fadeOutDuration <= 0.001f)
        {
            _musicSource.Stop();
            return;
        }
        _fadeRoutine = StartCoroutine(FadeOutAndStop());
    }

    void StartClip(bool useFadeIn)
    {
        ApplyMusicLinearVolumeToSource();
        _musicSource.clip = musicClip;
        _musicSource.time = 0f;
        if (useFadeIn && fadeInDuration > 0.001f && musicOutputMixerGroup == null)
        {
            float target = AudioManager.Instance != null
                ? AudioManager.Instance.GetChannelLinear(VolumeChannel.Music)
                : 1f;
            _musicSource.volume = 0f;
            _musicSource.Play();
            _fadeRoutine = StartCoroutine(FadeIn(target));
        }
        else if (useFadeIn && fadeInDuration > 0.001f && musicOutputMixerGroup != null)
        {
            _musicSource.volume = 0f;
            _musicSource.Play();
            _fadeRoutine = StartCoroutine(FadeIn(1f));
        }
        else
        {
            _musicSource.Play();
        }
    }

    IEnumerator FadeOutThenPlay(bool useFadeIn)
    {
        float t = 0f;
        float startVol = _musicSource.volume;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeOutDuration);
            yield return null;
        }
        _musicSource.Stop();
        _fadeRoutine = null;
        StartClip(useFadeIn);
    }

    IEnumerator FadeIn(float targetVolume)
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeInDuration);
            yield return null;
        }
        _musicSource.volume = targetVolume;
        _fadeRoutine = null;
    }

    IEnumerator FadeOutAndStop()
    {
        float t = 0f;
        float startVol = _musicSource.volume;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeOutDuration);
            yield return null;
        }
        _musicSource.Stop();
        _fadeRoutine = null;
    }
}
