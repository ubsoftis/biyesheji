using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Video;
using NodeCanvas.Framework;

public class activeAni : MonoBehaviour
{
    private const string CutsceneTriggerKey = "过场动画可以触发";

    [Header("条件1：点击入口（闭眼按钮等）")]
    [Tooltip("例如「主角闭眼」上的 Button：点击一次后 _clickedOnce=true。")]
    public Button heroCloseEyeButton;

    [Header("条件1：主角闭眼状态 UI/物体")]
    [Tooltip("未闭眼/未显示闭眼层时应为 inactive；activeInHierarchy=true 表示正在显示闭眼。")]
    public GameObject heroEyeClosedObject;

    [Header("条件1（可选）：主角本体")]
    [Tooltip("可选。若填写：主角 active 时也可满足状态（与「闭眼未显示」二选一）。")]
    public GameObject heroProtagonist;

    [Header("条件2：全局黑板")]
    public GlobalBlackboard gbb;

    [Header("过场")]
    [Tooltip("条件满足时 SetActive(true) 的根物体（如整段过场面板）。")]
    public GameObject cutsceneObjectToActivate;

    [Header("过场视频（推荐）")]
    [Tooltip("用于播放过场视频的 VideoPlayer。若填写则优先播放视频；未填写时才回退到 Animator。")]
    public VideoPlayer cutsceneVideoPlayer;

    [Tooltip("可选：直接指定 VideoClip（优先级高于 URL）。")]
    public VideoClip cutsceneVideoClip;

    [Tooltip("可选：视频 URL（本地/StreamingAssets/网络）。当未指定 VideoClip 时使用。")]
    public string cutsceneVideoUrl;

    [Tooltip("是否等待视频播放结束后再切换场景。")]
    public bool waitVideoToEnd = true;
    [Tooltip("播放时自动隐藏 Screen Space - Overlay 的 Canvas，避免挡住视频。")]
    public bool autoHideOverlayCanvasesWhileCutscene = true;

    [Header("过场音频")]
    public AudioClip cutsceneClip;
    [Tooltip("不填则自动在本物体上查找或添加 AudioSource")]
    public AudioSource cutsceneSource;
    [Tooltip("一般留空：自动从 DontDestroyOnLoad 的 AudioManager 同物体/子物体上找 BackgroundMusicPlayer。")]
    public BackgroundMusicPlayer backgroundMusic;

    [Header("Mixer（可选：过场音走 Sfx 总线）")]
    public AudioMixerGroup cutsceneOutputMixerGroup;

    [Header("音量")]
    [Range(0f, 1f)]
    [Tooltip("过场片段最终音量 = 本系数 ×（走 Mixer 时为 1；否则为 AudioManager 的 SFX 有效音量）")]
    public float cutsceneVolumeScale = 1f;

    [Header("过场期间其它声音")]
    [Tooltip("为 true：过场开始时把混音器 Ambient 通道临时拉到 0，结束再恢复。")]
    public bool muteAmbientDuringCutscene = true;
    [Tooltip("为 true：过场开始时把 Sfx 总线临时拉到 0；会静音 UI 点击等，一般保持关闭。")]
    public bool muteSfxDuringCutscene = false;

    [Header("背景音乐彻底静音")]
    [Tooltip("为 true：BGM 渐隐结束后 Pause 音乐 AudioSource；过场结束会先 UnPause 再渐回。")]
    public bool pauseBackgroundMusicWhenFullyDucked = true;

    [Header("渐入渐出（秒，0 表示瞬间）")]
    public float bgmFadeOutDuration = 0.6f;
    public float cutsceneFadeInDuration = 0.4f;
    public float cutsceneFadeOutDuration = 0.5f;
    public float bgmFadeInDuration = 0.8f;

    [Header("结束后切换关卡")]
    [Tooltip("要加载的场景名（确保已加入 Build Settings）。")]
    public string nextSceneName = "第二关场景";

    [Header("输出：条件是否满足（bool）")]
    public bool conditionsMet;

    [Tooltip("只执行一次（推荐）")]
    public bool triggerOnce = true;

    [Tooltip("为 false 时：不要求点击 heroCloseEyeButton，只要黑板允许且 stateOk 就触发（排障/特殊流程用）。")]
    public bool requireHeroClick = true;

    bool _clickedOnce;
    bool _triggered;
    bool _listening;
    readonly List<Canvas> _autoHiddenCanvases = new List<Canvas>();
    readonly List<bool> _autoHiddenPrevStates = new List<bool>();

    float _cutsceneTargetVolume = 1f;
    bool _mutedAmbientForCutscene;
    bool _mutedSfxForCutscene;
    bool _pausedBgmForCutscene;

    public bool HasCutsceneAudio => cutsceneClip != null;

    void Awake()
    {
        EnsureCutsceneSource();
    }

    void OnEnable()
    {
        RegisterButton();
    }

    void OnDisable()
    {
        UnregisterButton();
    }

    void Update()
    {
        bool canTriggerCutscene = gbb != null && gbb.GetVariableValue<bool>(CutsceneTriggerKey);
        bool closedLayerOff = heroEyeClosedObject == null || !heroEyeClosedObject.activeInHierarchy;
        bool protagonistOn = heroProtagonist != null && heroProtagonist.activeInHierarchy;
        bool stateOk = closedLayerOff || protagonistOn;

        bool clickOk = !requireHeroClick || _clickedOnce;
        conditionsMet = canTriggerCutscene && clickOk && stateOk;

        if (triggerOnce && _triggered) return;
        if (!conditionsMet) return;

        _triggered = true;
        ActivateCutsceneObjectIfNeeded();
        StartCoroutine(PlayAndLoad());
    }

    void ActivateCutsceneObjectIfNeeded()
    {
        GameObject go = cutsceneObjectToActivate;
        if (go != null && !go.activeSelf)
            go.SetActive(true);
    }

    IEnumerator PlayAndLoad()
    {
        bool hasAudio = HasCutsceneAudio;
        if (hasAudio)
            yield return BeginCutsceneAudio();

        if (cutsceneVideoPlayer == null)
        {
            var from = cutsceneObjectToActivate;
            if (from != null) cutsceneVideoPlayer = from.GetComponentInChildren<VideoPlayer>(true);
        }

        bool hasVideo = cutsceneVideoPlayer != null &&
            (cutsceneVideoClip != null || !string.IsNullOrEmpty(cutsceneVideoUrl) || cutsceneVideoPlayer.clip != null);

        if (hasVideo)
        {
            ActivateCutsceneObjectIfNeeded();

            cutsceneVideoPlayer.playOnAwake = false;
            cutsceneVideoPlayer.isLooping = false;
            if (cutsceneVideoPlayer.renderMode != VideoRenderMode.CameraNearPlane &&
                cutsceneVideoPlayer.renderMode != VideoRenderMode.CameraFarPlane)
            {
                cutsceneVideoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            }
            if (cutsceneVideoPlayer.targetCamera == null)
                cutsceneVideoPlayer.targetCamera = Camera.main;
            if (cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraNearPlane ||
                cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraFarPlane)
                cutsceneVideoPlayer.targetCameraAlpha = 1f;

            AutoHideOverlayCanvases();

            if (cutsceneVideoClip != null)
            {
                cutsceneVideoPlayer.source = VideoSource.VideoClip;
                cutsceneVideoPlayer.clip = cutsceneVideoClip;
            }
            else if (!string.IsNullOrEmpty(cutsceneVideoUrl))
            {
                cutsceneVideoPlayer.source = VideoSource.Url;
                cutsceneVideoPlayer.url = NormalizeUrl(cutsceneVideoUrl);
            }

            bool finished = false;
            void OnLoopPointReached(VideoPlayer _) => finished = true;
            cutsceneVideoPlayer.loopPointReached += OnLoopPointReached;

            cutsceneVideoPlayer.Prepare();
            float prepareDeadline = Time.realtimeSinceStartup + 5f;
            while (!cutsceneVideoPlayer.isPrepared && Time.realtimeSinceStartup < prepareDeadline)
                yield return null;

            cutsceneVideoPlayer.Play();

            if (waitVideoToEnd)
            {
                while (!finished && (cutsceneVideoPlayer.isPlaying || cutsceneVideoPlayer.frame < (long)cutsceneVideoPlayer.frameCount - 1))
                    yield return null;
            }

            cutsceneVideoPlayer.loopPointReached -= OnLoopPointReached;
            RestoreOverlayCanvases();
            if (cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraNearPlane ||
                cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraFarPlane)
            {
                cutsceneVideoPlayer.targetCameraAlpha = 0f;
            }
            cutsceneVideoPlayer.Stop();
        }
        else if (!hasAudio)
        {
            Debug.LogWarning("[activeAni] 未配置可播放视频或过场音频，跳过过场并直接切场景。");
        }

        if (hasAudio)
            yield return EndCutsceneAudio();

        if (!string.IsNullOrEmpty(nextSceneName))
            GlobalSceneTransition.LoadScene(nextSceneName);
    }

    IEnumerator BeginCutsceneAudio()
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

    IEnumerator EndCutsceneAudio()
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

    static string NormalizeUrl(string urlOrFileName)
    {
        if (string.IsNullOrEmpty(urlOrFileName)) return urlOrFileName;
        if (urlOrFileName.Contains("://") || Path.IsPathRooted(urlOrFileName))
            return urlOrFileName;
        return Path.Combine(Application.streamingAssetsPath, urlOrFileName);
    }

    void AutoHideOverlayCanvases()
    {
        if (!autoHideOverlayCanvasesWhileCutscene) return;
        _autoHiddenCanvases.Clear();
        _autoHiddenPrevStates.Clear();

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            _autoHiddenCanvases.Add(c);
            _autoHiddenPrevStates.Add(c.gameObject.activeSelf);
            c.gameObject.SetActive(false);
        }
    }

    void RestoreOverlayCanvases()
    {
        for (int i = 0; i < _autoHiddenCanvases.Count; i++)
        {
            var c = _autoHiddenCanvases[i];
            if (c == null) continue;
            bool prev = i < _autoHiddenPrevStates.Count && _autoHiddenPrevStates[i];
            c.gameObject.SetActive(prev);
        }
        _autoHiddenCanvases.Clear();
        _autoHiddenPrevStates.Clear();
    }

    void RegisterButton()
    {
        if (_listening) return;
        if (heroCloseEyeButton == null) return;
        heroCloseEyeButton.onClick.AddListener(OnHeroCloseEyeClicked);
        _listening = true;
    }

    void UnregisterButton()
    {
        if (!_listening) return;
        if (heroCloseEyeButton == null) return;
        heroCloseEyeButton.onClick.RemoveListener(OnHeroCloseEyeClicked);
        _listening = false;
    }

    void OnHeroCloseEyeClicked()
    {
        _clickedOnce = true;
    }
}
