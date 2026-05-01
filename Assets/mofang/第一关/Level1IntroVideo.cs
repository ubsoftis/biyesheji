using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 进入第一关时播放一段视频：播放期间可选择禁用指定物体，播完再恢复。
/// 挂到任意物体即可（推荐挂到“过场动画”面板/节点上）。
/// </summary>
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

    [Header("播放期间禁用的对象（可选）")]
    public GameObject[] disableWhilePlaying;

    [Header("自动遮挡处理")]
    [Tooltip("播放期间自动隐藏所有 Screen Space - Overlay 的 Canvas，避免挡住视频。")]
    public bool autoHideOverlayCanvases = true;

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

    void Start()
    {
        if (!playOnStart) return;
        _co = StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        var root = videoRootToActivate != null ? videoRootToActivate : gameObject;
        if (root != null && !root.activeSelf) root.SetActive(true);

        if (disableWhilePlaying != null)
        {
            for (int i = 0; i < disableWhilePlaying.Length; i++)
            {
                var go = disableWhilePlaying[i];
                if (go != null) go.SetActive(false);
            }
        }

        AutoHideOverlayCanvases(root);

        if (videoPlayer == null) videoPlayer = gameObject.GetComponent<VideoPlayer>();
        if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        // 默认渲染到主摄像机近裁剪面，避免你还要额外配 RawImage/RenderTexture
        if (videoPlayer.renderMode == VideoRenderMode.CameraNearPlane || videoPlayer.renderMode == VideoRenderMode.CameraFarPlane)
        {
            // 已经是 Camera 模式就尊重现有配置
        }
        else
        {
            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        }

        if (videoPlayer.targetCamera == null)
        {
            videoPlayer.targetCamera = Camera.main;
        }

        if (videoPlayer.renderMode == VideoRenderMode.CameraNearPlane || videoPlayer.renderMode == VideoRenderMode.CameraFarPlane)
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
            // 没有配置任何视频源：直接恢复
            RestoreAfter();
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

        RestoreAfter();
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

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c == null) continue;
            if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

            // 过场根节点下的 Canvas 不自动处理，避免误伤你的视频容器。
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
        // 已经是完整 URL（含协议）或绝对路径：直接用
        if (urlOrFileName.Contains("://") || Path.IsPathRooted(urlOrFileName))
            return urlOrFileName;

        // 只给了文件名/相对路径：默认从 StreamingAssets 读取
        return Path.Combine(Application.streamingAssetsPath, urlOrFileName);
    }
}

