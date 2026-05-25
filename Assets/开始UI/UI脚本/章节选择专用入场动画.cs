using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ChapterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("场景跳转")]
    public int sceneIndex = 0;

    [Header("音效（可选）")]
    public AudioClip clickSfx;
    [Tooltip("Sfx 子标签，留空仅用总 Sfx")]
    public string sfxTag = "";

    [Header("文件夹图片组 - 关闭状态")]
    public List<Image> closedImages = new List<Image>();

    [Header("文件夹图片组 - 打开状态")]
    public List<Image> openImages = new List<Image>();

    [Header("始终显示的图片（如标题）")]
    public List<Graphic> alwaysShowGraphics = new List<Graphic>();

    [Header("切换效果设置")]
    public float switchDuration = 0.3f;
    public bool useFadeEffect = true;
    public bool useScaleEffect = true;
    public float hoverScale = 1.1f;

    [Header("颜色变化（可选）")]
    public bool useColorEffect = false;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.white;

    [Header("场景切换效果")]
    public Image screenFade;                   // 黑幕 Image
    public float fadeToBlackDuration = 0.8f;   // 渐黑时间
    public float stayBlackDuration = 0.3f;     // 黑屏停留时间
    public float fadeFromBlackDuration = 0.8f; // 渐亮时间

    [Header("入场动画")]
    public bool enableEntrance = true;
    public float entranceDuration = 1.5f;
    public float entranceDelay = 0f;
    public bool useRandomDelay = false;
    public float randomDelayMin = 0f;
    public float randomDelayMax = 0.5f;

    [Header("入场 - 淡入")]
    public bool entranceFadeIn = true;

    [Header("入场 - 位置移动")]
    public bool entranceMove = false;
    public Vector2 entranceStartOffset = Vector2.zero;

    [Header("入场 - 缩放")]
    public bool entranceScale = false;
    public Vector3 entranceStartScale = Vector3.one;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Vector2 finalPosition;
    private Coroutine switchCoroutine;
    private bool isHovering = false;
    private bool isTransitioning = false;

    // 入场动画相关
    private float entranceTimer = 0f;
    private float actualDelay;
    private bool entranceComplete = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        finalPosition = rectTransform.anchoredPosition;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 初始状态
        SetFolderState(false);

        // 初始化黑幕为透明
        if (screenFade != null)
        {
            Color c = screenFade.color;
            c.a = 0f;
            screenFade.color = c;
        }

        if (enableEntrance)
        {
            InitializeEntrance();
        }
    }

    void Update()
    {
        if (enableEntrance && !entranceComplete)
        {
            UpdateEntrance();
        }
    }

    #region 文件夹状态切换

    void SetFolderState(bool isOpen)
    {
        foreach (var img in closedImages)
        {
            if (img != null)
            {
                img.gameObject.SetActive(!isOpen);
            }
        }

        foreach (var img in openImages)
        {
            if (img != null)
            {
                img.gameObject.SetActive(isOpen);
            }
        }
    }

    #endregion

    #region 入场动画

    void InitializeEntrance()
    {
        if (useRandomDelay)
        {
            actualDelay = entranceDelay + Random.Range(randomDelayMin, randomDelayMax);
        }
        else
        {
            actualDelay = entranceDelay;
        }

        if (entranceFadeIn)
        {
            canvasGroup.alpha = 0f;
        }

        if (entranceMove)
        {
            rectTransform.anchoredPosition = finalPosition + entranceStartOffset;
        }

        if (entranceScale)
        {
            rectTransform.localScale = entranceStartScale;
            originalScale = Vector3.one;
        }
    }

    void UpdateEntrance()
    {
        entranceTimer += Time.deltaTime;

        if (entranceTimer < actualDelay) return;

        float progress = (entranceTimer - actualDelay) / entranceDuration;

        if (progress >= 1f)
        {
            progress = 1f;
            entranceComplete = true;
        }

        float t = progress * progress * (3f - 2f * progress);

        if (entranceFadeIn)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
        }

        if (entranceMove)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(finalPosition + entranceStartOffset, finalPosition, t);
        }

        if (entranceScale)
        {
            rectTransform.localScale = Vector3.Lerp(entranceStartScale, Vector3.one, t);
            originalScale = Vector3.one;
        }
    }

    #endregion

    #region 鼠标交互

    public void OnPointerEnter(PointerEventData eventData)
    {
        if ((!entranceComplete && enableEntrance) || isTransitioning) return;

        isHovering = true;

        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
        }
        switchCoroutine = StartCoroutine(SwitchToOpen());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isTransitioning) return;

        isHovering = false;

        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
        }
        switchCoroutine = StartCoroutine(SwitchToClose());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if ((!entranceComplete && enableEntrance) || isTransitioning) return;

        PlaySfxIfConfigured(clickSfx);
        StartCoroutine(LoadSceneWithFade());
    }

    #endregion

    #region 切换动画

    IEnumerator SwitchToOpen()
    {
        float timer = 0f;
        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = originalScale * hoverScale;

        if (useFadeEffect)
        {
            while (timer < switchDuration / 2)
            {
                timer += Time.deltaTime;
                float t = timer / (switchDuration / 2);

                foreach (var img in closedImages)
                {
                    if (img != null)
                    {
                        Color c = img.color;
                        c.a = Mathf.Lerp(1f, 0f, t);
                        img.color = c;
                    }
                }

                if (useScaleEffect)
                {
                    rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t * 0.5f);
                }

                yield return null;
            }
        }

        SetFolderState(true);

        foreach (var img in openImages)
        {
            if (img != null)
            {
                Color c = img.color;
                c.a = 0f;
                img.color = c;
            }
        }

        timer = 0f;
        while (timer < switchDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (switchDuration / 2);

            foreach (var img in openImages)
            {
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Lerp(0f, 1f, t);
                    if (useColorEffect)
                    {
                        c = Color.Lerp(normalColor, hoverColor, t);
                        c.a = Mathf.Lerp(0f, 1f, t);
                    }
                    img.color = c;
                }
            }

            if (useScaleEffect)
            {
                rectTransform.localScale = Vector3.Lerp(startScale, targetScale, 0.5f + t * 0.5f);
            }

            yield return null;
        }

        if (useScaleEffect)
        {
            rectTransform.localScale = targetScale;
        }
    }

    IEnumerator SwitchToClose()
    {
        float timer = 0f;
        Vector3 startScale = rectTransform.localScale;

        if (useFadeEffect)
        {
            while (timer < switchDuration / 2)
            {
                timer += Time.deltaTime;
                float t = timer / (switchDuration / 2);

                foreach (var img in openImages)
                {
                    if (img != null)
                    {
                        Color c = img.color;
                        c.a = Mathf.Lerp(1f, 0f, t);
                        img.color = c;
                    }
                }

                if (useScaleEffect)
                {
                    rectTransform.localScale = Vector3.Lerp(startScale, originalScale, t * 0.5f);
                }

                yield return null;
            }
        }

        SetFolderState(false);

        foreach (var img in closedImages)
        {
            if (img != null)
            {
                Color c = img.color;
                c.a = 0f;
                img.color = c;
            }
        }

        timer = 0f;
        while (timer < switchDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (switchDuration / 2);

            foreach (var img in closedImages)
            {
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Lerp(0f, 1f, t);
                    if (useColorEffect)
                    {
                        c = Color.Lerp(hoverColor, normalColor, t);
                        c.a = Mathf.Lerp(0f, 1f, t);
                    }
                    img.color = c;
                }
            }

            if (useScaleEffect)
            {
                rectTransform.localScale = Vector3.Lerp(startScale, originalScale, 0.5f + t * 0.5f);
            }

            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    #endregion

    #region 场景跳转（全局黑幕过渡）

    IEnumerator LoadSceneWithFade()
    {
        isTransitioning = true;

        // 点击反馈
        Vector3 clickScale = rectTransform.localScale * 0.9f;
        Vector3 currentScale = rectTransform.localScale;

        float timer = 0f;
        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(currentScale, clickScale, timer / 0.1f);
            yield return null;
        }

        timer = 0f;
        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(clickScale, currentScale, timer / 0.1f);
            yield return null;
        }

        GlobalSceneTransition.LoadSceneByBuildIndex(sceneIndex);
    }

    void PlaySfxIfConfigured(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;
        string tag = string.IsNullOrWhiteSpace(sfxTag) ? null : sfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clip, tag);
    }

    #endregion
}