using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CloseButton : MonoBehaviour
{
    [Header("要关闭的面板")]
    public GameObject panelToClose;

    [Header("黑屏设置")]
    public Image blackScreen;              // 黑屏图片（可选）
    [Range(0f, 1f)]
    public float blackScreenAlpha = 0.8f;  // 黑屏程度
    public float blackScreenDuration = 0.5f; // 黑屏持续时间

    [Header("淡出设置")]
    public CanvasGroup panelCanvasGroup;   // 面板的CanvasGroup（可选）
    public float fadeDuration = 0.3f;      // 淡出时间

    [Header("退出按钮")]
    public Button closeButton;

    [Header("音效（可选）")]
    public AudioClip clickSfx;
    [Tooltip("Sfx 子标签，留空仅用总 Sfx")]
    public string sfxTag = "";

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        // 初始化黑屏为透明
        if (blackScreen != null)
        {
            blackScreen.color = new Color(0, 0, 0, 0);
        }
    }

    public void Close()
    {
        PlaySfxIfConfigured(clickSfx);
        StartCoroutine(CloseWithEffect());
    }

    IEnumerator CloseWithEffect()
    {
        // 第一阶段：黑屏渐入
        if (blackScreen != null)
        {
            float timer = 0f;
            while (timer < blackScreenDuration / 2)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0, blackScreenAlpha, timer / (blackScreenDuration / 2));
                blackScreen.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            // 保持黑屏一小段时间
            yield return new WaitForSeconds(blackScreenDuration / 2);
        }

        // 第二阶段：面板淡出 + 黑屏淡出
        float fadeTimer = 0f;
        float startBlackAlpha = blackScreen != null ? blackScreen.color.a : 0;

        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float t = fadeTimer / fadeDuration;

            // 面板透明度降低
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = Mathf.Lerp(1, 0, t);
            }

            // 黑屏也跟着消失
            if (blackScreen != null)
            {
                blackScreen.color = new Color(0, 0, 0, Mathf.Lerp(startBlackAlpha, 0, t));
            }

            yield return null;
        }

        // 关闭面板
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }

        // 重置状态（下次打开时用）
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1;
        }
        if (blackScreen != null)
        {
            blackScreen.color = new Color(0, 0, 0, 0);
        }
    }

    void PlaySfxIfConfigured(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;
        string tag = string.IsNullOrWhiteSpace(sfxTag) ? null : sfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clip, tag);
    }
}