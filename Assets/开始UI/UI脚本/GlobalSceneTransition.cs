using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 全局切关淡入淡出：DontDestroyOnLoad 后全项目共用。
/// 调用 <see cref="LoadScene"/> / <see cref="LoadSceneByBuildIndex"/> 会先渐隐黑屏再加载，新场景加载完成后自动渐显。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class GlobalSceneTransition : MonoBehaviour
{
    public static GlobalSceneTransition Instance { get; private set; }

    [Header("淡入淡出")]
    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 0.6f;

    [Tooltip("为 true 时使用 unscaledDeltaTime（Time.timeScale=0 时仍可过渡）。")]
    [SerializeField] private bool useUnscaledTime = true;

    [SerializeField] private Color fadeColor = Color.black;

    [Header("首场景")]
    [Tooltip("为 true 时，游戏第一次进入时从黑屏渐显（不先渐隐）。开始菜单若已有 OpeningFadeIn，请保持 false。")]
    [SerializeField] private bool fadeInOnFirstScene = false;

    [Header("加载")]
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

    Canvas _canvas;
    Image _fadeImage;
    bool _isTransitioning;
    static bool _hasShownFirstScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
            return;

        var go = new GameObject(nameof(GlobalSceneTransition));
        go.AddComponent<GlobalSceneTransition>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (!fadeInOnFirstScene || _hasShownFirstScene)
            return;

        _hasShownFirstScene = true;
        SetFadeAlpha(1f);
        StartCoroutine(FadeRoutine(1f, 0f));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isTransitioning)
            return;

        _isTransitioning = false;
        StartCoroutine(FadeRoutine(1f, 0f));
    }

    /// <summary>按场景名切关（渐隐 → 加载 → 渐显）。</summary>
    public static void LoadScene(string sceneName)
    {
        LoadScene(sceneName, false);
    }

    /// <summary>按 Build Settings 索引切关。</summary>
    public static void LoadSceneByBuildIndex(int buildIndex)
    {
        LoadSceneByBuildIndex(buildIndex, false);
    }

    /// <summary>重新加载当前场景（带淡入淡出）。</summary>
    public static void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        LoadScene(current.name);
    }

    /// <summary>屏幕已是黑屏时调用，跳过渐隐，仅加载并在新场景渐显。</summary>
    public static void LoadSceneFromBlack(string sceneName)
    {
        LoadScene(sceneName, true);
    }

    /// <summary>屏幕已是黑屏时调用，跳过渐隐，仅加载并在新场景渐显。</summary>
    public static void LoadSceneByBuildIndexFromBlack(int buildIndex)
    {
        LoadSceneByBuildIndex(buildIndex, true);
    }

    static void LoadScene(string sceneName, bool skipFadeOut)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[GlobalSceneTransition] 场景中未放置实例，将直接 LoadScene。");
            SceneManager.LoadScene(sceneName);
            return;
        }

        Instance.StartTransition(sceneName, -1, skipFadeOut);
    }

    static void LoadSceneByBuildIndex(int buildIndex, bool skipFadeOut)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[GlobalSceneTransition] 场景中未放置实例，将直接 LoadScene。");
            SceneManager.LoadScene(buildIndex);
            return;
        }

        Instance.StartTransition(null, buildIndex, skipFadeOut);
    }

    /// <summary>仅渐隐到黑（不加载场景）。</summary>
    public void FadeOut()
    {
        if (_isTransitioning)
            return;

        StartCoroutine(FadeRoutine(GetCurrentAlpha(), 1f));
    }

    /// <summary>仅从黑渐显（不加载场景）。</summary>
    public void FadeIn()
    {
        if (_isTransitioning)
            return;

        StartCoroutine(FadeRoutine(GetCurrentAlpha(), 0f));
    }

    /// <summary>立即设为全黑（用于本地黑幕动画结束后与全局过渡衔接）。</summary>
    public void SnapToBlack()
    {
        EnsureOverlay();
        SetFadeAlpha(1f);
        if (_fadeImage != null)
            _fadeImage.raycastTarget = true;
    }

    void StartTransition(string sceneName, int buildIndex, bool skipFadeOut)
    {
        if (_isTransitioning)
            return;

        if (string.IsNullOrEmpty(sceneName) && buildIndex < 0)
        {
            Debug.LogWarning("[GlobalSceneTransition] 未指定场景名或 buildIndex。", this);
            return;
        }

        StartCoroutine(TransitionRoutine(sceneName, buildIndex, skipFadeOut));
    }

    IEnumerator TransitionRoutine(string sceneName, int buildIndex, bool skipFadeOut)
    {
        _isTransitioning = true;
        EnsureOverlay();
        _fadeImage.raycastTarget = true;

        float currentAlpha = GetCurrentAlpha();
        if (skipFadeOut || currentAlpha >= 0.99f)
        {
            SetFadeAlpha(1f);
        }
        else
        {
            yield return FadeRoutine(currentAlpha, 1f);
        }

        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName, loadMode);
        else
            SceneManager.LoadScene(buildIndex, loadMode);
    }

    IEnumerator FadeRoutine(float fromAlpha, float toAlpha)
    {
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFadeAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetFadeAlpha(toAlpha);
        if (_fadeImage != null)
            _fadeImage.raycastTarget = toAlpha > 0.01f;
    }

    float GetCurrentAlpha()
    {
        return _fadeImage != null ? _fadeImage.color.a : 0f;
    }

    void SetFadeAlpha(float alpha)
    {
        if (_fadeImage == null)
            return;

        Color c = fadeColor;
        c.a = Mathf.Clamp01(alpha);
        _fadeImage.color = c;
    }

    void EnsureOverlay()
    {
        if (_fadeImage != null)
            return;

        GameObject canvasGo = new GameObject("GlobalTransitionCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = short.MaxValue;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        RectTransform rt = imageGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _fadeImage = imageGo.AddComponent<Image>();
        _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        _fadeImage.raycastTarget = false;
        _fadeImage.sprite = CreateSolidSprite();
    }

    static Sprite CreateSolidSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
