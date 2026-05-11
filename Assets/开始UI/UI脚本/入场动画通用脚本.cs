using UnityEngine;
using UnityEngine.UI;

public class EntranceEffect : MonoBehaviour
{
    [Header("基础设置")]
    public float duration = 1.5f;          // 动画持续时间
    public float delay = 0f;               // 延迟开始时间

    [Header("随机延迟（可选）")]
    public bool useRandomDelay = false;
    public float randomDelayMin = 0f;
    public float randomDelayMax = 1f;

    [Header("淡入效果")]
    public bool enableFadeIn = true;
    public float startAlpha = 0f;          // 起始透明度
    public float endAlpha = 1f;            // 最终透明度

    [Header("位置移动效果")]
    public bool enableMove = false;
    public Vector2 startOffset = Vector2.zero;  // 起始位置偏移（相对于最终位置）

    [Header("缩放效果")]
    public bool enableScale = false;
    public Vector3 startScale = Vector3.one;    // 起始缩放
    public Vector3 endScale = Vector3.one;      // 最终缩放

    [Header("动画曲线")]
    public AnimationCurveType curveType = AnimationCurveType.SmoothStep;

    public enum AnimationCurveType
    {
        Linear,         // 线性
        SmoothStep,     // 平滑（推荐）
        EaseIn,         // 慢进快出
        EaseOut,        // 快进慢出
        Bounce          // 弹跳
    }

    private RectTransform rectTransform;
    private Graphic graphic;
    private Vector2 finalPosition;
    private float timer = 0f;
    private float actualDelay;
    private bool animationComplete = false;
    private bool initialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (initialized) return;

        rectTransform = GetComponent<RectTransform>();
        graphic = GetComponent<Graphic>();

        // 记录最终位置
        finalPosition = rectTransform.anchoredPosition;

        // 计算实际延迟
        if (useRandomDelay)
        {
            actualDelay = delay + Random.Range(randomDelayMin, randomDelayMax);
        }
        else
        {
            actualDelay = delay;
        }

        // 设置初始状态
        SetInitialState();

        initialized = true;
    }

    void SetInitialState()
    {
        // 初始透明度
        if (enableFadeIn && graphic != null)
        {
            Color color = graphic.color;
            color.a = startAlpha;
            graphic.color = color;
        }

        // 初始位置
        if (enableMove)
        {
            rectTransform.anchoredPosition = finalPosition + startOffset;
        }

        // 初始缩放
        if (enableScale)
        {
            rectTransform.localScale = startScale;
        }
    }

    void Update()
    {
        if (animationComplete) return;

        timer += Time.deltaTime;

        // 还在延迟中
        if (timer < actualDelay)
        {
            return;
        }

        // 计算动画进度 (0 ~ 1)
        float progress = (timer - actualDelay) / duration;

        if (progress >= 1f)
        {
            // 动画完成
            progress = 1f;
            animationComplete = true;
        }

        // 应用曲线
        float curvedProgress = ApplyCurve(progress);

        // 应用淡入
        if (enableFadeIn && graphic != null)
        {
            float alpha = Mathf.Lerp(startAlpha, endAlpha, curvedProgress);
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        // 应用位置移动
        if (enableMove)
        {
            Vector2 currentPos = Vector2.Lerp(finalPosition + startOffset, finalPosition, curvedProgress);
            rectTransform.anchoredPosition = currentPos;
        }

        // 应用缩放
        if (enableScale)
        {
            Vector3 currentScale = Vector3.Lerp(startScale, endScale, curvedProgress);
            rectTransform.localScale = currentScale;
        }
    }

    float ApplyCurve(float t)
    {
        switch (curveType)
        {
            case AnimationCurveType.Linear:
                return t;

            case AnimationCurveType.SmoothStep:
                return t * t * (3f - 2f * t);

            case AnimationCurveType.EaseIn:
                return t * t;

            case AnimationCurveType.EaseOut:
                return 1f - (1f - t) * (1f - t);

            case AnimationCurveType.Bounce:
                if (t < 0.5f)
                {
                    return 2f * t * t;
                }
                else
                {
                    float overshoot = (t - 0.5f) * 2f;
                    return 1f + 0.1f * Mathf.Sin(overshoot * Mathf.PI * 3f) * (1f - overshoot);
                }

            default:
                return t;
        }
    }

    // ============ 公开方法 ============

    // 重新播放动画
    public void Replay()
    {
        timer = 0f;
        animationComplete = false;
        SetInitialState();
    }

    // 立即完成动画
    public void Complete()
    {
        animationComplete = true;

        if (enableFadeIn && graphic != null)
        {
            Color color = graphic.color;
            color.a = endAlpha;
            graphic.color = color;
        }

        if (enableMove)
        {
            rectTransform.anchoredPosition = finalPosition;
        }

        if (enableScale)
        {
            rectTransform.localScale = endScale;
        }
    }

    // 跳过动画（直接到最终状态）
    public void Skip()
    {
        Complete();
    }
}