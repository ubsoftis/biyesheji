using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 进入关卡时：先全黑 → 播放开场视频（关卡内容尚未显示）→ 视频结束后再显示关卡并渐亮。
/// </summary>
[DefaultExecutionOrder(-300)]
public class Level1IntroVideo : MonoBehaviour
{
    [Header("视频源（二选一：Clip 优先）")]
    public VideoClip videoClip;
    [Tooltip("可填完整 URL；也可以只填文件名（会自动从 StreamingAssets 读取）。")]
    public string videoUrl;

    [Header("显示/播放")]
    [Tooltip("不填则运行时自动 AddComponent<VideoPlayer>()。")]
    public VideoPlayer videoPlayer;

    [Tooltip("播放时激活的根物体（例如黑底/过场UI）。不填则默认使用当前 GameObject。")]
    public GameObject videoRootToActivate;

    [Header("进场隐藏关卡")]
    [Tooltip("为 true 时，开场视频播完之前禁用场景中除摄像机/VideoPlayer/本脚本以外的根物体。")]
    public bool hideSceneUntilIntroFinished = true;

    [Tooltip("可选：手动指定“关卡内容”根；留空则自动隐藏其它场景根物体。")]
    public GameObject[] gameplayRoots;

    [Header("播放期间禁用的对象（可选，额外）")]
    public GameObject[] disableWhilePlaying;

    [Header("自动遮挡处理")]
    [Tooltip("播放期间自动隐藏本场景内 Screen Space - Overlay 的 Canvas（不含 DontDestroyOnLoad 的全局黑幕）。")]
    public bool autoHideOverlayCanvases = true;

    [Header("过场音效")]
    [Tooltip("不填则尝试从本物体 GetComponent<CutsceneAudioController>()")]
    public CutsceneAudioController cutsceneAudio;

    [Header("行为")]
    public bool playOnStart = true;
    public bool deactivateRootAfter = true;
    [Tooltip("播放结束后强制 Stop VideoPlayer，避免停在最后一帧继续覆盖画面。")]
    public bool stopVideoPlayerAfterFinish = true;
    [Tooltip("当 RenderMode 为 CameraNear/FarPlane 时，播放结束把 targetCameraAlpha 置 0，避免残留覆盖。")]
    public bool clearCameraPlaneAfterFinish = true;

    Coroutine _co;
    readonly List<Canvas> _autoHiddenCanvases = new List<Canvas>();
    readonly List<bool> _autoHiddenPrevStates = new List<bool>();
    readonly List<GameObject> _hiddenRoots = new List<GameObject>();
    bool _holdsEntryBlack;
    Camera _mainCamera;
    CameraClearFlags _prevClearFlags;
    Color _prevBackgroundColor;

    void Awake()
    {
        if (!ShouldRunEntryIntro())
            return;

        _holdsEntryBlack = true;
        GlobalSceneTransition.SceneEntryBlackHold.Hold();
        GlobalSceneTransition.Instance?.SnapToBlack();

        if (hideSceneUntilIntroFinished)
            HideSceneContent();

        SetupCameraBlackBackground();
    }

    void Start()
    {
        if (!playOnStart) return;
        _co = StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        CutscenePlaybackGate.Enter();
        try
        {
            var root = videoRootToActivate != null ? videoRootToActivate : gameObject;
            if (root != null && !root.activeSelf) root.SetActive(true);

            DisableExtraObjectsWhilePlaying();
            AutoHideOverlayCanvases(root);

            var audio = ResolveCutsceneAudio();
            if (audio != null && audio.HasClip)
                yield return audio.BeginCutsceneAudio();

            if (videoPlayer == null) videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            if (videoPlayer.renderMode != VideoRenderMode.CameraNearPlane &&
                videoPlayer.renderMode != VideoRenderMode.CameraFarPlane)
            {
                videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            }

            if (videoPlayer.targetCamera == null)
                videoPlayer.targetCamera = Camera.main;

            if (videoPlayer.renderMode == VideoRenderMode.CameraNearPlane ||
                videoPlayer.renderMode == VideoRenderMode.CameraFarPlane)
            {
                videoPlayer.targetCameraAlpha = 1f;
            }

            if (videoClip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClip;
            }
            else if (!string.IsNullOrEmpty(videoUrl))
            {
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = NormalizeUrl(videoUrl);
            }
            else if (videoPlayer.clip == null && string.IsNullOrEmpty(videoPlayer.url))
            {
                if (audio != null && audio.HasClip)
                    yield return audio.EndCutsceneAudio();
                RevealSceneAfterIntro();
                RestoreAfter();
                yield break;
            }

            bool finished = false;
            void OnLoopPointReached(VideoPlayer _) => finished = true;
            videoPlayer.loopPointReached += OnLoopPointReached;

            videoPlayer.Prepare();
            float prepareDeadline = Time.realtimeSinceStartup + 8f;
            while (!videoPlayer.isPrepared && Time.realtimeSinceStartup < prepareDeadline)
                yield return null;

            videoPlayer.Play();
            yield return WaitForVideoFirstFrame();

            // 关卡仍隐藏、摄像机纯黑底；仅撤掉全局黑幕以露出视频。
            GlobalSceneTransition.Instance?.ClearOverlayImmediate();

            while (!finished && (videoPlayer.isPlaying || videoPlayer.frame < (long)videoPlayer.frameCount - 1))
                yield return null;

            videoPlayer.loopPointReached -= OnLoopPointReached;

            if (audio != null && audio.HasClip)
                yield return audio.EndCutsceneAudio();

            RevealSceneAfterIntro();
            RestoreAfter();
        }
        finally
        {
            if (_holdsEntryBlack)
            {
                _holdsEntryBlack = false;
                GlobalSceneTransition.SceneEntryBlackHold.Reset();
                GlobalSceneTransition.Instance?.RevealSceneAfterEntryIntro();
            }
            CutscenePlaybackGate.Exit();
        }
    }

    IEnumerator WaitForVideoFirstFrame()
    {
        float deadline = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.frame > 0)
                yield break;
            yield return null;
        }
    }

    void RevealSceneAfterIntro()
    {
        ShowSceneContent();
        RestoreCameraBackground();

        _holdsEntryBlack = false;
        GlobalSceneTransition.Instance?.RevealSceneAfterEntryIntro();
    }

    void HideSceneContent()
    {
        _hiddenRoots.Clear();

        if (gameplayRoots != null && gameplayRoots.Length > 0)
        {
            for (int i = 0; i < gameplayRoots.Length; i++)
            {
                var go = gameplayRoots[i];
                if (go == null || !go.activeSelf)
                    continue;
                _hiddenRoots.Add(go);
                go.SetActive(false);
            }
            return;
        }

        var introRoot = GetIntroRoot();
        var scene = gameObject.scene;
        if (!scene.IsValid())
            return;

        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i];
            if (root == null || !root.activeSelf)
                continue;
            if (IsEssentialIntroRoot(root, introRoot))
                continue;

            _hiddenRoots.Add(root);
            root.SetActive(false);
        }
    }

    void ShowSceneContent()
    {
        for (int i = 0; i < _hiddenRoots.Count; i++)
        {
            var go = _hiddenRoots[i];
            if (go != null)
                go.SetActive(true);
        }
        _hiddenRoots.Clear();
    }

    bool IsEssentialIntroRoot(GameObject root, GameObject introRoot)
    {
        if (introRoot != null && (root == introRoot || root.transform == introRoot.transform))
            return true;
        if (introRoot != null && introRoot.transform.IsChildOf(root.transform))
            return true;
        if (root.GetComponentInChildren<Level1IntroVideo>(true) != null)
            return true;
        if (root.GetComponentInChildren<VideoPlayer>(true) != null)
            return true;
        if (root.GetComponentInChildren<Camera>(true) != null)
            return true;
        return false;
    }

    GameObject GetIntroRoot()
    {
        return videoRootToActivate != null ? videoRootToActivate : gameObject;
    }

    void SetupCameraBlackBackground()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
            return;

        _prevClearFlags = _mainCamera.clearFlags;
        _prevBackgroundColor = _mainCamera.backgroundColor;
        _mainCamera.clearFlags = CameraClearFlags.SolidColor;
        _mainCamera.backgroundColor = Color.black;
    }

    void RestoreCameraBackground()
    {
        if (_mainCamera == null)
            return;

        _mainCamera.clearFlags = _prevClearFlags;
        _mainCamera.backgroundColor = _prevBackgroundColor;
        _mainCamera = null;
    }

    bool ShouldRunEntryIntro()
    {
        if (!playOnStart)
            return false;
        if (videoClip != null)
            return true;
        if (!string.IsNullOrEmpty(videoUrl))
            return true;
        if (videoPlayer != null && videoPlayer.clip != null)
            return true;
        return videoPlayer != null && !string.IsNullOrEmpty(videoPlayer.url);
    }

    void ReleaseEntryBlackHold()
    {
        _holdsEntryBlack = false;
        GlobalSceneTransition.SceneEntryBlackHold.Reset();
    }

    void DisableExtraObjectsWhilePlaying()
    {
        if (disableWhilePlaying == null)
            return;

        for (int i = 0; i < disableWhilePlaying.Length; i++)
        {
            var go = disableWhilePlaying[i];
            if (go != null)
                go.SetActive(false);
        }
    }

    CutsceneAudioController ResolveCutsceneAudio()
    {
        if (cutsceneAudio != null)
            return cutsceneAudio;
        return GetComponent<CutsceneAudioController>();
    }

    void RestoreAfter()
    {
        if (videoPlayer != null)
        {
            if (stopVideoPlayerAfterFinish)
                videoPlayer.Stop();

            if (clearCameraPlaneAfterFinish &&
                (videoPlayer.renderMode == VideoRenderMode.CameraNearPlane || videoPlayer.renderMode == VideoRenderMode.CameraFarPlane))
            {
                videoPlayer.targetCameraAlpha = 0f;
            }
        }

        RestoreOverlayCanvases();

        if (disableWhilePlaying != null)
        {
            for (int i = 0; i < disableWhilePlaying.Length; i++)
            {
                var go = disableWhilePlaying[i];
                if (go != null) go.SetActive(true);
            }
        }

        var root = videoRootToActivate != null ? videoRootToActivate : gameObject;
        if (deactivateRootAfter && root != null) root.SetActive(false);
    }

    void AutoHideOverlayCanvases(GameObject root)
    {
        if (!autoHideOverlayCanvases) return;

        _autoHiddenCanvases.Clear();
        _autoHiddenPrevStates.Clear();

        var scene = gameObject.scene;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c == null) continue;
            if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            // 不碰 DontDestroyOnLoad 的全局黑幕（GlobalSceneTransition）。
            if (!c.gameObject.scene.IsValid() || c.gameObject.scene != scene) continue;
            if (root != null && c.transform.IsChildOf(root.transform)) continue;

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

    static string NormalizeUrl(string urlOrFileName)
    {
        if (urlOrFileName.Contains("://") || Path.IsPathRooted(urlOrFileName))
            return urlOrFileName;

        return Path.Combine(Application.streamingAssetsPath, urlOrFileName);
    }
}
