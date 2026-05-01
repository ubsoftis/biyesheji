using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        // 「闭眼层未显示」：未引用或已隐藏；或「主角本体」已激活。与未引用闭眼物体时 stateOk 不应恒为 false。
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
        if (cutsceneVideoPlayer == null)
        {
            var from = cutsceneObjectToActivate;
            if (from != null) cutsceneVideoPlayer = from.GetComponentInChildren<VideoPlayer>(true);
        }

        if (cutsceneVideoPlayer != null && (cutsceneVideoClip != null || !string.IsNullOrEmpty(cutsceneVideoUrl) || cutsceneVideoPlayer.clip != null))
        {
            ActivateCutsceneObjectIfNeeded();

            // 避免被 VideoPlayer 自己 Play On Awake 干扰时序
            cutsceneVideoPlayer.playOnAwake = false;
            cutsceneVideoPlayer.isLooping = false;
            if (cutsceneVideoPlayer.renderMode != VideoRenderMode.CameraNearPlane &&
                cutsceneVideoPlayer.renderMode != VideoRenderMode.CameraFarPlane)
            {
                cutsceneVideoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            }
            if (cutsceneVideoPlayer.targetCamera == null)
            {
                cutsceneVideoPlayer.targetCamera = Camera.main;
            }
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

            // Prepare 有助于减少首帧黑屏/卡顿（某些平台上仍可能是异步）
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
        else
        {
            Debug.LogWarning("[activeAni] 未配置可播放视频，跳过过场视频并直接切场景。");
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
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
