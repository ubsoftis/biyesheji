using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OpeningFadeIn : MonoBehaviour
{
    [Header("==== 黑屏设置 ====")]
    [Tooltip("黑屏覆盖层Image（全屏黑色，会被脚本自动控制）")]
    public Image blackOverlay;

    [Tooltip("开场全黑保持时间（秒）")]
    public float blackHoldTime = 0.5f;

    [Tooltip("黑屏淡出（从黑变透明）时间（秒）")]
    public float fadeOutTime = 2f;

    [Tooltip("淡出完成后多久禁用黑屏GameObject（0=不禁用）")]
    public float disableAfterSeconds = 0f;

    [Header("==== 自动播放 ====")]
    public bool playOnStart = true;

    void Start()
    {
        // 初始化：一开始全黑
        if (blackOverlay != null)
        {
            blackOverlay.gameObject.SetActive(true);
            Color c = blackOverlay.color;
            c.a = 1f;
            blackOverlay.color = c;
        }

        if (playOnStart)
            PlayFadeIn();
    }

    /// <summary>
    /// 开始播放开场黑屏淡出
    /// </summary>
    public void PlayFadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        if (blackOverlay == null)
        {
            Debug.LogWarning("[OpeningFadeIn] 没有指定 blackOverlay！");
            yield break;
        }

        // 保持全黑
        if (blackHoldTime > 0)
            yield return new WaitForSeconds(blackHoldTime);

        // 淡出（从黑到透明）
        Color startColor = blackOverlay.color;
        startColor.a = 1f;
        Color endColor = blackOverlay.color;
        endColor.a = 0f;

        float timer = 0f;
        while (timer < fadeOutTime)
        {
            float t = timer / fadeOutTime;
            blackOverlay.color = Color.Lerp(startColor, endColor, t);
            timer += Time.deltaTime;
            yield return null;
        }
        blackOverlay.color = endColor;

        // 完成后可选禁用
        if (disableAfterSeconds > 0)
        {
            yield return new WaitForSeconds(disableAfterSeconds);
            blackOverlay.gameObject.SetActive(false);
        }

        Debug.Log("✅ 开场黑屏淡出完成");
    }
}