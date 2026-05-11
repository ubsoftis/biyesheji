using UnityEngine;
using UnityEngine.UI;

public class BirdFloat : MonoBehaviour
{
    [Header("上下浮动")]
    public float floatDistance = 10f;
    public float floatSpeedMin = 0.3f;
    public float floatSpeedMax = 0.8f;

    [Header("左右摇摆")]
    public float swayDistance = 5f;
    public float swaySpeedMin = 0.2f;
    public float swaySpeedMax = 0.5f;

    [Header("微微旋转")]
    public float rotateAngle = 3f;
    public float rotateSpeedMin = 0.2f;
    public float rotateSpeedMax = 0.6f;

    [Header("透明度闪动（模拟模糊）")]
    public float alphaMin = 0.7f;
    public float alphaMax = 1f;
    public float alphaSpeedMin = 0.3f;
    public float alphaSpeedMax = 0.8f;

    [Header("开场淡入效果")]
    public float fadeInDuration = 2f;      // 淡入持续时间
    public float fadeInDelay = 0f;         // 延迟多久开始淡入

    private RectTransform rectTransform;
    private Image image;
    private Vector2 startPos;
    private Quaternion startRotation;

    // 随机速度
    private float floatSpeed;
    private float swaySpeed;
    private float rotateSpeed;
    private float alphaSpeed;

    // 随机起始偏移
    private float timeOffset;

    // 淡入相关
    private float fadeInTimer = 0f;
    private bool fadeInComplete = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        startPos = rectTransform.anchoredPosition;
        startRotation = rectTransform.localRotation;

        // 随机速度
        floatSpeed = Random.Range(floatSpeedMin, floatSpeedMax);
        swaySpeed = Random.Range(swaySpeedMin, swaySpeedMax);
        rotateSpeed = Random.Range(rotateSpeedMin, rotateSpeedMax);
        alphaSpeed = Random.Range(alphaSpeedMin, alphaSpeedMax);

        // 随机起始时间
        timeOffset = Random.Range(0f, 100f);

        // 随机延迟淡入（让每只鸟不同时出现）
        fadeInDelay += Random.Range(0f, 0.5f);

        // 初始透明度为0
        if (image != null)
        {
            Color color = image.color;
            color.a = 0f;
            image.color = color;
        }
    }

    void Update()
    {
        float time = Time.time + timeOffset;

        // 上下浮动
        float offsetY = Mathf.Sin(time * floatSpeed) * floatDistance;

        // 左右摇摆
        float offsetX = Mathf.Sin(time * swaySpeed) * swayDistance;

        // 应用位置
        rectTransform.anchoredPosition = startPos + new Vector2(offsetX, offsetY);

        // 微微旋转
        float angle = Mathf.Sin(time * rotateSpeed) * rotateAngle;
        rectTransform.localRotation = startRotation * Quaternion.Euler(0, 0, angle);

        // 处理透明度
        if (image != null)
        {
            float alpha;

            if (!fadeInComplete)
            {
                // 开场淡入
                alpha = HandleFadeIn();
            }
            else
            {
                // 淡入完成后，正常闪动
                float alphaNormalized = (Mathf.Sin(time * alphaSpeed) + 1f) / 2f;
                alpha = Mathf.Lerp(alphaMin, alphaMax, alphaNormalized);
            }

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    float HandleFadeIn()
    {
        fadeInTimer += Time.deltaTime;

        // 还在延迟中
        if (fadeInTimer < fadeInDelay)
        {
            return 0f;
        }

        // 计算淡入进度
        float fadeProgress = (fadeInTimer - fadeInDelay) / fadeInDuration;

        if (fadeProgress >= 1f)
        {
            fadeInComplete = true;
            return alphaMax;
        }

        // 平滑淡入（使用 SmoothStep 更丝滑）
        return Mathf.SmoothStep(0f, alphaMax, fadeProgress);
    }
}