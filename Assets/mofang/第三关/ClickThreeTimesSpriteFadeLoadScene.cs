using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 挂在带 <see cref="Collider"/> / <see cref="Collider2D"/> 的可点击物体上（需 Main Camera 与 Physics 射线）：
/// 每点击一次替换本物体或指定 <see cref="SpriteRenderer"/> 的贴图；共三次；
/// 第三次换图后可换 BGM、播放过场视频，再渐隐黑屏并加载下一关（场景名优先，否则 build index）。
/// </summary>
public class ClickThreeTimesSpriteFadeLoadScene : MonoBehaviour
{
    [Header("换图目标")]
    [Tooltip("不填则在本物体上 GetComponent<SpriteRenderer>()。")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("长度须为 3：第 1/2/3 次点击依次替换为这些 Sprite。")]
    public Sprite[] spritesPerClick = new Sprite[3];

    [Header("点击")]
    [Tooltip("为 true：指针在 UI 上时不响应（需场景里有 EventSystem）。")]
    public bool blockWhenPointerOverUi = true;

    [Header("点击音效")]
    [Tooltip("每次有效点击换图时播放；留空则不播放")]
    public AudioClip clickSfx;

    [Tooltip("Sfx 子标签；与总 Sfx 相乘。留空则仅用总 Sfx")]
    public string clickSfxTag = "";

    [Tooltip("仅此点击音效的音量（0~1）")]
    [Range(0f, 1f)]
    public float clickSfxVolume = 1f;

    [Header("黑屏（二选一）")]
    [Tooltip("可选：全屏黑色 Image，初始 alpha 建议为 0；若赋值则用其做渐隐，否则用 OnGUI 绘制。")]
    public Image fullscreenBlackOverlay;

    [Tooltip("第三次换图完成后停留时长（秒），之后再开始黑屏渐隐；与「渐隐使用非缩放时间」一致选用缩放或非缩放计时。")]
    [Min(0f)]
    public float holdDurationAfterLastSprite = 0.75f;

    [Tooltip("第三次点击后黑屏渐隐时长（秒）。")]
    [Min(0.01f)]
    public float fadeOutDuration = 1f;

    [Tooltip("停留与渐隐是否使用非缩放时间（与 holdDurationAfterLastSprite、fadeOutDuration 一致）；避免 Time.timeScale=0 时卡住。")]
    public bool fadeUsesUnscaledTime = true;

    [Header("背景音乐（第三次点击后）")]
    [Tooltip("第三次换图完成后切换为此 BGM；留空则不换曲。")]
    public AudioClip backgroundMusicAfterThirdClick;

    [Tooltip("一般留空：自动从 AudioManager 上找 BackgroundMusicPlayer。")]
    public BackgroundMusicPlayer backgroundMusic;

    [Tooltip("换曲时是否淡入淡出（沿用 BackgroundMusicPlayer 的 fade 时长）。")]
    public bool backgroundMusicUseFade = true;

    [Tooltip("加载下一关前切换为此 BGM；留空则不换曲。")]
    public AudioClip backgroundMusicOnSceneLoad;

    [Tooltip("加载下一关换曲时是否淡入淡出。")]
    public bool backgroundMusicOnSceneLoadUseFade = true;

    [Header("过场视频（第三次点击后，黑屏前）")]
    [Tooltip("视频 Clip；与 videoUrl、VideoPlayer 上已有 clip 三选一。")]
    public VideoClip cutsceneVideoClip;

    [Tooltip("可填完整 URL，或仅文件名（从 StreamingAssets 读取）。")]
    public string cutsceneVideoUrl;

    [Tooltip("不填则在 cutsceneVideoRoot 或本物体上查找/添加 VideoPlayer。")]
    public VideoPlayer cutsceneVideoPlayer;

    [Tooltip("播放前激活的根物体（如黑底 UI）。不填则用本物体。")]
    public GameObject cutsceneVideoRootToActivate;

    [Tooltip("播放期间自动隐藏本场景 Screen Space Overlay Canvas，避免挡住视频。")]
    public bool autoHideOverlayCanvases = true;

    [Tooltip("播放视频期间 SetActive(false) 的对象（可选）。")]
    public GameObject[] disableWhilePlayingVideo;

    [Min(0.1f)]
    public float videoPrepareTimeoutSeconds = 5f;

    [Tooltip("视频播完后 Stop VideoPlayer，并清除 Camera 平面残留。")]
    public bool stopVideoPlayerAfterFinish = true;

    [Header("下一关（与 LevelOutroVideo 相同优先级）")]
    [Tooltip("非空则 LoadScene(名称)。")]
    public string nextSceneName;

    [Tooltip("当 nextSceneName 为空且本值 >= 0 时，按 build index 加载。")]
    public int nextSceneBuildIndexIfNameEmpty = -1;

    [Header("可选回调")]
    public UnityEngine.Events.UnityEvent<int> onAfterSpriteStep;

    [Header("状态（只读）")]
    [Tooltip("已完成第三次点击并开始过场，不再响应点击。")]
    public bool transitionStarted;

    /// <summary>已完成的点击次数 0~3。</summary>
    public int CompletedClickCount => _step;

    int _step;
    float _fadeAlpha;
    Texture2D _blackTex;
    Coroutine _fadeCo;
    readonly List<Canvas> _autoHiddenCanvases = new List<Canvas>();
    readonly List<bool> _autoHiddenPrevStates = new List<bool>();

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnMouseDown()
    {
        TryAdvanceClick();
    }

    /// <summary>若不用 OnMouseDown（例如由别的脚本转发），可调用此方法。</summary>
    public void TryAdvanceClick()
    {
        if (transitionStarted)
            return;

        if (blockWhenPointerOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[ClickThreeTimesSpriteFadeLoadScene] {name} 无 SpriteRenderer，忽略点击。");
            return;
        }

        if (spritesPerClick == null || spritesPerClick.Length != 3)
        {
            Debug.LogWarning($"[ClickThreeTimesSpriteFadeLoadScene] {name} spritesPerClick 必须长度为 3。");
            return;
        }

        if (_step >= 3)
            return;

        Sprite next = spritesPerClick[_step];
        if (next == null)
        {
            Debug.LogWarning($"[ClickThreeTimesSpriteFadeLoadScene] {name} spritesPerClick[{_step}] 为空，忽略本次点击。");
            return;
        }

        spriteRenderer.sprite = next;
        _step++;
        PlayClickSfxIfConfigured();
        onAfterSpriteStep?.Invoke(_step);

        if (_step >= 3)
        {
            transitionStarted = true;
            TrySwitchBackgroundMusicAfterThirdClick();
            if (_fadeCo != null)
                StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeOutAndLoadRoutine());
        }
    }

    void TrySwitchBackgroundMusicAfterThirdClick()
    {
        if (backgroundMusicAfterThirdClick == null)
            return;

        var bgm = ResolveBackgroundMusic();
        if (bgm == null)
        {
            Debug.LogWarning(
                $"[ClickThreeTimesSpriteFadeLoadScene] {name} 已配置 backgroundMusicAfterThirdClick，但未找到 BackgroundMusicPlayer。");
            return;
        }

        bgm.PlayMusic(backgroundMusicAfterThirdClick, backgroundMusicUseFade);
    }

    void TrySwitchBackgroundMusicOnSceneLoad()
    {
        if (backgroundMusicOnSceneLoad == null)
            return;

        var bgm = ResolveBackgroundMusic();
        if (bgm == null)
        {
            Debug.LogWarning(
                $"[ClickThreeTimesSpriteFadeLoadScene] {name} 已配置 backgroundMusicOnSceneLoad，但未找到 BackgroundMusicPlayer。");
            return;
        }

        bgm.PlayMusic(backgroundMusicOnSceneLoad, backgroundMusicOnSceneLoadUseFade);
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

    void PlayClickSfxIfConfigured()
    {
        if (clickSfx == null || AudioManager.Instance == null)
            return;

        string tag = string.IsNullOrWhiteSpace(clickSfxTag) ? null : clickSfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clickSfx, tag, clickSfxVolume);
    }

    IEnumerator FadeOutAndLoadRoutine()
    {
        float hold = Mathf.Max(0f, holdDurationAfterLastSprite);
        if (hold > 0f)
        {
            if (fadeUsesUnscaledTime)
                yield return new WaitForSecondsRealtime(hold);
            else
                yield return new WaitForSeconds(hold);
        }

        if (HasCutsceneVideo())
            yield return PlayCutsceneVideoRoutine();

        float dur = Mathf.Max(0.01f, fadeOutDuration);
        float t = 0f;

        if (fullscreenBlackOverlay != null)
        {
            fullscreenBlackOverlay.gameObject.SetActive(true);
            Color c = fullscreenBlackOverlay.color;
            while (t < dur)
            {
                t += fadeUsesUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float a = Mathf.Clamp01(t / dur);
                c.a = a;
                fullscreenBlackOverlay.color = c;
                yield return null;
            }

            c.a = 1f;
            fullscreenBlackOverlay.color = c;
        }
        else
        {
            EnsureBlackTexture();
            while (t < dur)
            {
                t += fadeUsesUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                _fadeAlpha = Mathf.Clamp01(t / dur);
                yield return null;
            }

            _fadeAlpha = 1f;
        }

        LoadNextScene();
        _fadeCo = null;
    }

    bool HasCutsceneVideo()
    {
        if (cutsceneVideoClip != null || !string.IsNullOrWhiteSpace(cutsceneVideoUrl))
            return true;

        var player = cutsceneVideoPlayer;
        if (player == null && cutsceneVideoRootToActivate != null)
            player = cutsceneVideoRootToActivate.GetComponentInChildren<VideoPlayer>(true);
        if (player == null)
            player = GetComponentInChildren<VideoPlayer>(true);

        return player != null && (player.clip != null || !string.IsNullOrEmpty(player.url));
    }

    IEnumerator PlayCutsceneVideoRoutine()
    {
        CutscenePlaybackGate.Enter();
        GameObject root = cutsceneVideoRootToActivate != null ? cutsceneVideoRootToActivate : gameObject;
        VideoPlayer player = null;
        try
        {
            if (root != null && !root.activeSelf)
                root.SetActive(true);

            SetObjectsActive(disableWhilePlayingVideo, false);
            AutoHideOverlayCanvases(root);

            player = ResolveCutsceneVideoPlayer(root);
            if (player == null)
                yield break;

            player.playOnAwake = false;
            player.isLooping = false;

            if (player.renderMode != VideoRenderMode.CameraNearPlane &&
                player.renderMode != VideoRenderMode.CameraFarPlane)
            {
                player.renderMode = VideoRenderMode.CameraNearPlane;
            }

            if (player.targetCamera == null)
                player.targetCamera = Camera.main;

            if (player.renderMode == VideoRenderMode.CameraNearPlane ||
                player.renderMode == VideoRenderMode.CameraFarPlane)
            {
                player.targetCameraAlpha = 1f;
            }

            if (cutsceneVideoClip != null)
            {
                player.source = VideoSource.VideoClip;
                player.clip = cutsceneVideoClip;
            }
            else if (!string.IsNullOrWhiteSpace(cutsceneVideoUrl))
            {
                player.source = VideoSource.Url;
                player.url = NormalizeVideoUrl(cutsceneVideoUrl);
            }

            bool finished = false;
            void OnLoopPointReached(VideoPlayer _) => finished = true;
            player.loopPointReached += OnLoopPointReached;

            player.Prepare();
            float prepareDeadline = Time.realtimeSinceStartup + videoPrepareTimeoutSeconds;
            while (!player.isPrepared && Time.realtimeSinceStartup < prepareDeadline)
                yield return null;

            if (!player.isPrepared)
            {
                Debug.LogWarning($"[ClickThreeTimesSpriteFadeLoadScene] {name} 视频 Prepare 超时，跳过播放。");
                player.loopPointReached -= OnLoopPointReached;
                yield break;
            }

            player.Play();
            while (!finished &&
                   (player.isPlaying || player.frame < (long)player.frameCount - 1))
            {
                yield return null;
            }

            player.loopPointReached -= OnLoopPointReached;
            CleanupVideoPlayer(player);
        }
        finally
        {
            RestoreOverlayCanvases();
            SetObjectsActive(disableWhilePlayingVideo, true);
            CutscenePlaybackGate.Exit();
        }
    }

    VideoPlayer ResolveCutsceneVideoPlayer(GameObject root)
    {
        if (cutsceneVideoPlayer != null)
            return cutsceneVideoPlayer;

        if (root != null)
        {
            var fromRoot = root.GetComponent<VideoPlayer>();
            if (fromRoot != null)
                return fromRoot;
            var inRoot = root.GetComponentInChildren<VideoPlayer>(true);
            if (inRoot != null)
                return inRoot;
        }

        var onSelf = GetComponent<VideoPlayer>();
        if (onSelf != null)
            return onSelf;

        return GetComponentInChildren<VideoPlayer>(true);
    }

    void CleanupVideoPlayer(VideoPlayer player)
    {
        if (player == null || !stopVideoPlayerAfterFinish)
            return;

        if (player.renderMode == VideoRenderMode.CameraNearPlane ||
            player.renderMode == VideoRenderMode.CameraFarPlane)
        {
            player.targetCameraAlpha = 0f;
        }

        player.Stop();
    }

    void AutoHideOverlayCanvases(GameObject root)
    {
        if (!autoHideOverlayCanvases)
            return;

        _autoHiddenCanvases.Clear();
        _autoHiddenPrevStates.Clear();

        var scene = gameObject.scene;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;
            if (!c.gameObject.scene.IsValid() || c.gameObject.scene != scene)
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

    static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    static string NormalizeVideoUrl(string urlOrFileName)
    {
        if (urlOrFileName.Contains("://") || Path.IsPathRooted(urlOrFileName))
            return urlOrFileName;

        return Path.Combine(Application.streamingAssetsPath, urlOrFileName);
    }

    void LoadNextScene()
    {
        TrySwitchBackgroundMusicOnSceneLoad();

        if (GlobalSceneTransition.Instance != null)
            GlobalSceneTransition.Instance.SnapToBlack();

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            GlobalSceneTransition.LoadSceneFromBlack(nextSceneName);
            return;
        }

        if (nextSceneBuildIndexIfNameEmpty >= 0)
        {
            GlobalSceneTransition.LoadSceneByBuildIndexFromBlack(nextSceneBuildIndexIfNameEmpty);
            return;
        }

        Debug.LogWarning(
            $"[ClickThreeTimesSpriteFadeLoadScene] {name} 未配置 nextSceneName 或有效的 nextSceneBuildIndexIfNameEmpty，无法跳转关卡。");
    }

    void EnsureBlackTexture()
    {
        if (_blackTex != null)
            return;
        _blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _blackTex.SetPixel(0, 0, Color.black);
        _blackTex.Apply(false, true);
    }

    void OnGUI()
    {
        if (fullscreenBlackOverlay != null || _fadeAlpha <= 0f)
            return;

        EnsureBlackTexture();
        GUI.color = new Color(0f, 0f, 0f, _fadeAlpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _blackTex);
        GUI.color = Color.white;
    }
}
