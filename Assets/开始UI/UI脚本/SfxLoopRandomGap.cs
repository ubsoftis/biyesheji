using System.Collections;
using UnityEngine;

/// <summary>
/// 归类为 Sfx：反复播放同一段音频，每次播完后随机等待若干秒再播下一次。
/// 优先走 <see cref="AudioManager.PlaySfx2D"/>；无单例时在本物体上 <see cref="AudioSource.PlayOneShot"/> 兜底。
/// </summary>
[DisallowMultipleComponent]
public class SfxLoopRandomGap : MonoBehaviour
{
    [Header("音频")]
    public AudioClip sfxClip;
    [Tooltip("走 AudioManager 时的 Sfx 子标签（可选）")]
    public string sfxTag;
    [Tooltip("在有效 Sfx 音量上的额外倍率")]
    [Range(0f, 2f)]
    public float volumeScale = 1f;

    [Header("随机间隔（秒）")]
    [Tooltip("两次播放之间的随机等待下限（不含片段本身时长时见下方「先等播完」）")]
    public float minGapSeconds = 2f;
    [Tooltip("两次播放之间的随机等待上限")]
    public float maxGapSeconds = 8f;

    [Header("时机")]
    [Tooltip("为 true：先等上一段播完（按 clip.length），再随机间隔；避免长音频叠音")]
    public bool waitClipLengthBeforeGap = true;
    [Tooltip("勾选则进入场景后开始循环")]
    public bool playOnStart = true;
    [Tooltip("勾选则每次启用物体时开始（与 playOnStart 可同时为 true，会防重复启动）")]
    public bool playOnEnable = false;

    [Header("时间缩放")]
    [Tooltip("为 true：间隔受 Time.timeScale 影响；为 false：用真实时间（暂停时仍计时）")]
    public bool useScaledTime = true;

    [Header("过场")]
    [Tooltip("Level1IntroVideo / LevelOutroVideo 协程运行期间不播放，结束后从下一轮再继续")]
    public bool muteDuringCutscene = true;

    AudioSource _fallbackSource;
    Coroutine _loopCo;

    void Start()
    {
        if (playOnStart)
            StartLoop();
    }

    void OnEnable()
    {
        if (playOnEnable)
            StartLoop();
    }

    void OnDisable()
    {
        StopLoop();
    }

    /// <summary>开始循环（已在跑则忽略）。</summary>
    public void StartLoop()
    {
        if (sfxClip == null || _loopCo != null)
            return;
        _loopCo = StartCoroutine(LoopRoutine());
    }

    /// <summary>停止循环。</summary>
    public void StopLoop()
    {
        if (_loopCo != null)
        {
            StopCoroutine(_loopCo);
            _loopCo = null;
        }
    }

    IEnumerator LoopRoutine()
    {
        float gapMin = Mathf.Min(minGapSeconds, maxGapSeconds);
        float gapMax = Mathf.Max(minGapSeconds, maxGapSeconds);

        while (enabled)
        {
            if (muteDuringCutscene && CutscenePlaybackGate.IsCutscenePlaying)
            {
                yield return null;
                continue;
            }

            PlayOnce();

            if (waitClipLengthBeforeGap && sfxClip != null && sfxClip.length > 0f)
            {
                if (useScaledTime)
                    yield return new WaitForSeconds(sfxClip.length);
                else
                    yield return new WaitForSecondsRealtime(sfxClip.length);
            }

            float gap = gapMax > gapMin ? Random.Range(gapMin, gapMax) : gapMin;
            if (useScaledTime)
                yield return new WaitForSeconds(gap);
            else
                yield return new WaitForSecondsRealtime(gap);
        }

        _loopCo = null;
    }

    void PlayOnce()
    {
        if (sfxClip == null)
            return;
        if (muteDuringCutscene && CutscenePlaybackGate.IsCutscenePlaying)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx2D(sfxClip, sfxTag, volumeScale);
            return;
        }

        if (_fallbackSource == null)
        {
            _fallbackSource = GetComponent<AudioSource>();
            if (_fallbackSource == null)
                _fallbackSource = gameObject.AddComponent<AudioSource>();
            _fallbackSource.playOnAwake = false;
            _fallbackSource.spatialBlend = 0f;
        }

        _fallbackSource.PlayOneShot(sfxClip, Mathf.Clamp01(volumeScale));
    }
}
