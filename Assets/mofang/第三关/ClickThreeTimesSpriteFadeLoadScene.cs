using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 挂在带 <see cref="Collider"/> / <see cref="Collider2D"/> 的可点击物体上（需 Main Camera 与 Physics 射线）：
/// 每点击一次替换本物体或指定 <see cref="SpriteRenderer"/> 的贴图；共三次；
/// 第三次换图后先停留一小段时间展示最后一张图，再渐隐黑屏并加载下一关（场景名优先，否则 build index）。
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
            if (_fadeCo != null)
                StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeOutAndLoadRoutine());
        }
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

    void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (nextSceneBuildIndexIfNameEmpty >= 0)
        {
            SceneManager.LoadScene(nextSceneBuildIndexIfNameEmpty);
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
