using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// 结束过场：当本物体被 <c>SetActive(true)</c> 时开始播放视频（逻辑参照 <see cref="Level1IntroVideo"/>），
/// 播完后加载下一关场景；若未配置场景名则可在 Inspector 用 build index 兜底。
/// </summary>
public class LevelOutroVideo : MonoBehaviour
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

    [Header("播放期间禁用的对象（可选）")]
    public GameObject[] disableWhilePlaying;

    [Header("自动遮挡处理")]
    [Tooltip("播放期间自动隐藏所有 Screen Space - Overlay 的 Canvas，避免挡住视频。")]
    public bool autoHideOverlayCanvases = true;

    [Header("过场音效")]
    [Tooltip("不填则尝试从本物体 GetComponent<CutsceneAudioController>()")]
    public CutsceneAudioController cutsceneAudio;

    [Header("播完后")]
    [Tooltip("留空则使用 nextSceneBuildIndexIfNameEmpty（>=0 时）。")]
    public string nextSceneName;
    [Tooltip("当 nextSceneName 为空且本值 >= 0 时，使用 SceneManager.LoadScene(buildIndex)。")]
    public int nextSceneBuildIndexIfNameEmpty = -1;
    [Tooltip("为 true：播放结束后强制 Stop VideoPlayer，避免停在最后一帧继续覆盖画面。")]
    public bool stopVideoPlayerAfterFinish = true;
    [Tooltip("当 RenderMode 为 CameraNear/FarPlane 时，播放结束把 targetCameraAlpha 置 0，避免残留覆盖。")]
    public bool clearCameraPlaneAfterFinish = true;
    [Tooltip("即将切场景时一般不需要再隐藏过场根节点（切场景会卸载当前场景）。为 true 且未切场景时才会隐藏。")]
    public bool deactivateRootAfterFinishIfNoLoad = false;

    Coroutine _co;
    readonly List<Canvas> _autoHiddenCanvases = new List<Canvas>();
    readonly List<bool> _autoHiddenPrevStates = new List<bool>();

    void OnEnable()
    {
        if (_co != null)
            StopCoroutine(_co);
        _co = StartCoroutine(PlayRoutine());
    }

    void OnDisable()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
            CutscenePlaybackGate.Exit();
        }
    }

    IEnumerator PlayRoutine()
    {
        CutscenePlaybackGate.Enter();
        try
        {
            var root = videoRootToActivate != null ? videoRootToActivate : gameObject;
            if (root != null && !root.activeSelf)
                root.SetActive(true);

            if (disableWhilePlaying != null)
            {
                for (int i = 0; i < disableWhilePlaying.Length; i++)
                {
                    var go = disableWhilePlaying[i];
                    if (go != null)
                        go.SetActive(false);
                }
            }

            AutoHideOverlayCanvases(root);

            var audio = ResolveCutsceneAudio();
            if (audio != null && audio.HasClip)
                yield return audio.BeginCutsceneAudio();

            if (videoPlayer == null)
                videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
                videoPlayer = gameObject.AddComponent<VideoPlayer>();

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            if (videoPlayer.renderMode == VideoRenderMode.CameraNearPlane || videoPlayer.renderMode == VideoRenderMode.CameraFarPlane)
            {
                // 已是 Camera 模式则保留
            }
            else
            {
                videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            }

            if (videoPlayer.targetCamera == null)
                videoPlayer.targetCamera = Camera.main;

            if (videoPlayer.renderMode == VideoRenderMode.CameraNearPlane || videoPlayer.renderMode == VideoRenderMode.CameraFarPlane)
                videoPlayer.targetCameraAlpha = 1f;

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
                RestoreAfter(root, loadScene: false, deactivateRoot: deactivateRootAfterFinishIfNoLoad);
                _co = null;
                yield break;
            }

            bool finished = false;
            void OnLoopPointReached(VideoPlayer _) => finished = true;
            videoPlayer.loopPointReached += OnLoopPointReached;

            videoPlayer.Prepare();
            float prepareDeadline = Time.realtimeSinceStartup + 5f;
            while (!videoPlayer.isPrepared && Time.realtimeSinceStartup < prepareDeadline)
                yield return null;

            videoPlayer.Play();
            while (!finished && (videoPlayer.isPlaying || videoPlayer.frame < (long)videoPlayer.frameCount - 1))
                yield return null;

            videoPlayer.loopPointReached -= OnLoopPointReached;

            if (audio != null && audio.HasClip)
                yield return audio.EndCutsceneAudio();

            bool willLoad = !string.IsNullOrEmpty(nextSceneName) || nextSceneBuildIndexIfNameEmpty >= 0;
            RestoreAfter(root, loadScene: willLoad, deactivateRoot: deactivateRootAfterFinishIfNoLoad && !willLoad);

            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else if (nextSceneBuildIndexIfNameEmpty >= 0)
                SceneManager.LoadScene(nextSceneBuildIndexIfNameEmpty);
            else
                Debug.LogWarning("[LevelOutroVideo] 未配置 nextSceneName 或 nextSceneBuildIndexIfNameEmpty，播完后不会切场景。");

            _co = null;
        }
        finally
        {
            CutscenePlaybackGate.Exit();
        }
    }

    void RestoreAfter(GameObject root, bool loadScene, bool deactivateRoot)
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
                if (go != null)
                    go.SetActive(true);
            }
        }

        // 即将 LoadScene 时不要再 SetActive(false) 过场根节点，避免打断切场景前的收尾（且即将卸载）
        if (!loadScene && deactivateRoot && root != null)
            root.SetActive(false);
    }

    void AutoHideOverlayCanvases(GameObject root)
    {
        if (!autoHideOverlayCanvases)
            return;

        _autoHiddenCanvases.Clear();
        _autoHiddenPrevStates.Clear();

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c == null)
                continue;
            if (c.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;

            if (root != null && c.transform.IsChildOf(root.transform))
                continue;

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
            if (c == null)
                continue;
            bool prev = i < _autoHiddenPrevStates.Count && _autoHiddenPrevStates[i];
            c.gameObject.SetActive(prev);
        }

        _autoHiddenCanvases.Clear();
        _autoHiddenPrevStates.Clear();
    }

    CutsceneAudioController ResolveCutsceneAudio()
    {
        if (cutsceneAudio != null)
            return cutsceneAudio;
        return GetComponent<CutsceneAudioController>();
    }

    static string NormalizeUrl(string urlOrFileName)
    {
        if (urlOrFileName.Contains("://") || Path.IsPathRooted(urlOrFileName))
            return urlOrFileName;

        return Path.Combine(Application.streamingAssetsPath, urlOrFileName);
    }
}
