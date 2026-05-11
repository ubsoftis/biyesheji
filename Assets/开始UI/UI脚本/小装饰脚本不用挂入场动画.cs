using UnityEngine;
using UnityEngine.UI;

public class DecorationEffect : MonoBehaviour
{
    [Header("入场淡入")]
    public bool enableFadeIn = true;
    public float fadeInDuration = 1.5f;
    public float fadeInDelay = 0f;

    [Header("呼吸灯效果")]
    public bool enableBreathing = true;
    public float breathAlphaMin = 0.5f;
    public float breathAlphaMax = 1f;
    public float breathSpeed = 1f;

    [Header("缩放呼吸")]
    public bool enableScalePulse = false;
    public float scaleMin = 0.95f;
    public float scaleMax = 1.05f;
    public float scaleSpeed = 1f;

    [Header("轻微旋转")]
    public bool enableRotate = false;
    public float rotateAngle = 10f;
    public float rotateSpeed = 1f;

    [Header("随机偏移")]
    public bool useRandomOffset = true;

    private Graphic graphic;
    private RectTransform rectTransform;
    private Vector3 startScale;
    private Quaternion startRotation;
    private float timeOffset;

    // 淡入相关
    private float fadeInTimer = 0f;
    private bool fadeInComplete = false;

    void Start()
    {
        graphic = GetComponent<Graphic>();
        rectTransform = GetComponent<RectTransform>();
        startScale = rectTransform.localScale;
        startRotation = rectTransform.localRotation;

        if (useRandomOffset)
        {
            timeOffset = Random.Range(0f, 100f);
        }

        // 初始透明
        if (enableFadeIn && graphic != null)
        {
            Color c = graphic.color;
            c.a = 0f;
            graphic.color = c;
        }
    }

    void Update()
    {
        float time = Time.time + timeOffset;

        // 淡入
        float baseAlpha = 1f;
        if (enableFadeIn && !fadeInComplete)
        {
            fadeInTimer += Time.deltaTime;
            if (fadeInTimer >= fadeInDelay)
            {
                float progress = (fadeInTimer - fadeInDelay) / fadeInDuration;
                if (progress >= 1f)
                {
                    progress = 1f;
                    fadeInComplete = true;
                }
                baseAlpha = Mathf.SmoothStep(0f, 1f, progress);
            }
            else
            {
                baseAlpha = 0f;
            }
        }

        // 呼吸灯
        if (graphic != null)
        {
            float alpha = baseAlpha;
            if (enableBreathing && fadeInComplete)
            {
                float t = (Mathf.Sin(time * breathSpeed) + 1f) / 2f;
                alpha = Mathf.Lerp(breathAlphaMin, breathAlphaMax, t);
            }
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        // 缩放呼吸
        if (enableScalePulse)
        {
            float t = (Mathf.Sin(time * scaleSpeed) + 1f) / 2f;
            float scale = Mathf.Lerp(scaleMin, scaleMax, t);
            rectTransform.localScale = startScale * scale;
        }

        // 轻微旋转
        if (enableRotate)
        {
            float angle = Mathf.Sin(time * rotateSpeed) * rotateAngle;
            rectTransform.localRotation = startRotation * Quaternion.Euler(0, 0, angle);
        }
    }
}