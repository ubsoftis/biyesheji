using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 过场动画音频：播放时渐隐 BGM、渐显过场音轨；结束时反向恢复。
/// 挂在与 <see cref="Level1IntroVideo"/> / <see cref="LevelOutroVideo"/> 同一物体或子物体上，并在 Inspector 拖入过场 AudioClip。
/// BGM 默认从常驻 <see cref="AudioManager"/> 同物体或子物体上的 <see cref="BackgroundMusicPlayer"/> 自动解析，无需拖拽。
/// </summary>
[DisallowMultipleComponent]
public class CutsceneAudioController : MonoBehaviour
{
    [Header("过场音频")]
    public AudioClip cutsceneClip;
    [Tooltip("不填则自动在本物体上查找或添加 AudioSource")]
    public AudioSource cutsceneSource;
    [Tooltip("一般留空：自动从 DontDestroyOnLoad 的 AudioManager 同物体/子物体上找 BackgroundMusicPlayer，找不到再全场景查找。仅特殊结构时手动指定。")]
    public BackgroundMusicPlayer backgroundMusic;

    [Header("Mixer（可选：过场音走 Sfx 总线）")]
    public AudioMixerGroup cutsceneOutputMixerGroup;

    [Header("音量")]
    [Range(0f, 1f)]
    [Tooltip("过场片段最终音量 = 本系数 ×（走 Mixer 时为 1；否则为 AudioManager 的 SFX 有效音量）")]
    public float cutsceneVolumeScale = 1f;

    [Header("过场期间其它声音（常是「还有背景」的来源）")]
    [Tooltip("为 true：过场开始时把混音器 Ambient 通道临时拉到 0（不写入 PlayerPrefs），结束再恢复。")]
    public bool muteAmbientDuringCutscene = true;
    [Tooltip("为 true：过场开始时把 Sfx 总线临时拉到 0；会静音 UI 点击等，一般保持关闭。")]
    public bool muteSfxDuringCutscene = false;

    [Header("背景音乐彻底静音")]
    [Tooltip("为 true：BGM 渐隐结束后 Pause 音乐 AudioSource，避免 Mixer 仍漏一点声；过场结束会先 UnPause 再渐回。")]
    public bool pauseBackgroundMusicWhenFullyDucked = true;

    [Header("渐入渐出（秒，0 表示瞬间）")]
    public float bgmFadeOutDuration = 0.6f;
    public float cutsceneFadeInDuration = 0.4f;
    public float cutsceneFadeOutDuration = 0.5f;
    public float bgmFadeInDuration = 0.8f;

    float _cutsceneTargetVolume = 1f;
    bool _mutedAmbientForCutscene;
    bool _mutedSfxForCutscene;
    bool _pausedBgmForCutscene;

    public bool HasClip => cutsceneClip != null;

    void ApplyTemporaryMixSilence()
    {
        if (AudioManager.Instance == null)
            return;
        if (muteAmbientDuringCutscene)
        {
            AudioManager.Instance.ApplyAmbientToMixerWithoutSaving(0f);
            _mutedAmbientForCutscene = true;
        }

        if (muteSfxDuringCutscene)
        {
            AudioManager.Instance.ApplySfxToMixerWithoutSaving(0f);
            _mutedSfxForCutscene = true;
        }
    }

    void RestoreTemporaryMixSilence()
    {
        if (AudioManager.Instance == null)
        {
            _mutedAmbientForCutscene = false;
            _mutedSfxForCutscene = false;
            return;
        }

        if (_mutedAmbientForCutscene)
        {
            AudioManager.Instance.RestoreSavedAmbientToMixer();
            _mutedAmbientForCutscene = false;
        }

        if (_mutedSfxForCutscene)
        {
            AudioManager.Instance.RestoreSavedSfxToMixer();
            _mutedSfxForCutscene = false;
        }
    }

    void Awake()
    {
        EnsureCutsceneSource();
    }

    void EnsureCutsceneSource()
    {
        if (cutsceneSource != null)
            return;
        cutsceneSource = GetComponent<AudioSource>();
        if (cutsceneSource == null)
            cutsceneSource = gameObject.AddComponent<AudioSource>();
        cutsceneSource.playOnAwake = false;
        cutsceneSource.loop = false;
        cutsceneSource.spatialBlend = 0f;
    }

    /// <summary>
    /// BGM 通常与 <see cref="AudioManager"/> 同挂在常驻物体上；优先从该层级解析，避免依赖 Inspector 拖拽。
    /// </summary>
    BackgroundMusicPlayer ResolveBackgroundMusic()
    {
        if (backgroundMusic != null)
            return backgroundMusic;

        if (AudioManager.Instance != null)
        {
            var onRoot = AudioManager.Instance.GetComponent<BackgroundMusicPlayer>();
            if (onRoot != null)
                return onRoot;
            var inHierarchy = AudioManager.Instance.GetComponentInChildren<BackgroundMusicPlayer>(true);
            if (inHierarchy != null)
                return inHierarchy;
        }

        return FindFirstObjectByType<BackgroundMusicPlayer>();
    }

    float GetCutsceneTargetVolume()
    {
        if (cutsceneOutputMixerGroup != null)
            return 1f;
        if (AudioManager.Instance != null)
            return AudioManager.Instance.GetEffectiveSfxVolume();
        return 1f;
    }

    /// <summary>过场开始：并行压低 BGM、渐入过场音频。</summary>
    public IEnumerator BeginCutsceneAudio()
    {
        if (cutsceneClip == null)
            yield break;

        EnsureCutsceneSource();
        var bgm = ResolveBackgroundMusic();

        ApplyTemporaryMixSilence();

        _cutsceneTargetVolume = GetCutsceneTargetVolume() * Mathf.Clamp01(cutsceneVolumeScale);
        cutsceneSource.clip = cutsceneClip;
        cutsceneSource.time = 0f;
        cutsceneSource.volume = 0f;
        if (cutsceneOutputMixerGroup != null)
            cutsceneSource.outputAudioMixerGroup = cutsceneOutputMixerGroup;
        cutsceneSource.Play();

        float maxDur = Mathf.Max(bgmFadeOutDuration, cutsceneFadeInDuration);
        if (maxDur <= 0.001f)
        {
            if (bgm != null)
                yield return bgm.DuckForCutscene(0f);
            cutsceneSource.volume = _cutsceneTargetVolume;
            if (pauseBackgroundMusicWhenFullyDucked && bgm != null)
            {
                bgm.PauseForCutscene();
                _pausedBgmForCutscene = true;
            }
            yield break;
        }

        Coroutine duckCo = null;
        if (bgm != null)
            duckCo = StartCoroutine(bgm.DuckForCutscene(bgmFadeOutDuration));

        float t = 0f;
        while (t < maxDur)
        {
            t += Time.unscaledDeltaTime;
            float cutK = cutsceneFadeInDuration <= 0.001f
                ? 1f
                : Mathf.Clamp01(t / cutsceneFadeInDuration);
            cutsceneSource.volume = Mathf.Lerp(0f, _cutsceneTargetVolume, cutK);
            yield return null;
        }

        cutsceneSource.volume = _cutsceneTargetVolume;
        if (duckCo != null)
            yield return duckCo;

        if (pauseBackgroundMusicWhenFullyDucked && bgm != null)
        {
            bgm.PauseForCutscene();
            _pausedBgmForCutscene = true;
        }
    }

    /// <summary>过场结束：渐出过场音频、渐显 BGM。</summary>
    public IEnumerator EndCutsceneAudio()
    {
        if (cutsceneClip == null)
        {
            RestoreTemporaryMixSilence();
            yield break;
        }

        EnsureCutsceneSource();
        var bgm = ResolveBackgroundMusic();

        if (_pausedBgmForCutscene && bgm != null)
        {
            bgm.UnpauseForCutscene();
            _pausedBgmForCutscene = false;
        }

        float maxDur = Mathf.Max(cutsceneFadeOutDuration, bgmFadeInDuration);
        Coroutine unduckCo = null;
        if (bgm != null)
            unduckCo = StartCoroutine(bgm.UnduckAfterCutscene(bgmFadeInDuration));

        if (maxDur <= 0.001f || !cutsceneSource.isPlaying)
        {
            cutsceneSource.Stop();
            if (unduckCo != null)
                yield return unduckCo;
            RestoreTemporaryMixSilence();
            yield break;
        }

        float startVol = cutsceneSource.volume;
        float t = 0f;
        while (t < maxDur)
        {
            t += Time.unscaledDeltaTime;
            float cutK = cutsceneFadeOutDuration <= 0.001f
                ? 1f
                : Mathf.Clamp01(t / cutsceneFadeOutDuration);
            cutsceneSource.volume = Mathf.Lerp(startVol, 0f, cutK);
            yield return null;
        }

        cutsceneSource.Stop();
        cutsceneSource.volume = 0f;
        if (unduckCo != null)
            yield return unduckCo;

        RestoreTemporaryMixSilence();
    }

    /// <summary>即将切场景时立即停止过场音，避免淡出期间露出关卡画面。</summary>
    public void StopCutsceneImmediate()
    {
        if (cutsceneClip == null)
        {
            RestoreTemporaryMixSilence();
            return;
        }

        EnsureCutsceneSource();
        var bgm = ResolveBackgroundMusic();

        if (_pausedBgmForCutscene && bgm != null)
        {
            bgm.UnpauseForCutscene();
            _pausedBgmForCutscene = false;
        }

        if (cutsceneSource != null && cutsceneSource.isPlaying)
        {
            cutsceneSource.Stop();
            cutsceneSource.volume = 0f;
        }

        RestoreTemporaryMixSilence();
    }
}
