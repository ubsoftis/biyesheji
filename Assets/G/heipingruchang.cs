using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 黑屏入场渐变脚本
/// 挂载到包含Image组件的全屏黑色图片对象上
/// </summary>
[RequireComponent(typeof(Image))]
public class BlackScreenFade : MonoBehaviour
{
    // 渐变时长（秒），可在Inspector面板调整
    [Header("渐变时长（秒）")]
    public float fadeDuration = 3f;

    // 黑色图片的Image组件引用
    private Image blackImage;
    // 渐变计时
    private float fadeTimer;

    void Start()
    {
        // 获取图片组件
        blackImage = GetComponent<Image>();
        // 初始状态：完全不透明
        blackImage.color = new Color(0, 0, 0, 1);
        // 确保对象初始是激活的
        gameObject.SetActive(true);
        // 初始化计时器
        fadeTimer = 0;
    }

    void Update()
    {
        // 如果还在渐变时长内
        if (fadeTimer < fadeDuration)
        {
            // 累计计时
            fadeTimer += Time.deltaTime;
            // 计算当前透明度（从1到0渐变）
            float alpha = 1 - (fadeTimer / fadeDuration);
            // 应用透明度（RGB保持黑色，只改Alpha）
            blackImage.color = new Color(0, 0, 0, alpha);

            // 当渐变完成时
            if (fadeTimer >= fadeDuration)
            {
                // 直接取消Inspector的勾选（SetActive(false)）
                gameObject.SetActive(false);
            }
        }
    }

    // 可选：手动重置黑屏（比如重新播放入场动画）
    public void ResetBlackScreen()
    {
        gameObject.SetActive(true);
        blackImage.color = new Color(0, 0, 0, 1);
        fadeTimer = 0;
    }
}