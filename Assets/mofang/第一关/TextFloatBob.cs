using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 每句必须是单独的 TextMeshProUGUI 或旧版 Text（各自写好一句文案），按顺序冒出 → 停留 → 渐隐。
/// 浮动作用在 <see cref="target"/>（默认本物体 RectTransform）上，子句会一起浮动。
/// </summary>
[DisallowMultipleComponent]
public class TextFloatBob : MonoBehaviour
{
    [Tooltip("留空则用本物体上的 RectTransform")]
    [SerializeField] RectTransform target;

    [Header("逐句 TMP — 每句拖一个 TextMeshProUGUI，顺序即播放顺序")]
    [SerializeField] TextMeshProUGUI[] sentenceTexts;

    [Header("逐句旧版 UI Text — 与 TMP 二选一或混用（手动数组时先 TMP 后 Text）")]
    [SerializeField] Text[] sentenceLegacyTexts;

    [Tooltip("上面数组都为空时，自动收集本物体下所有子级 TMP / Text（不含自己身上的）")]
    [SerializeField] bool autoCollectChildTmps = true;

    [Header("逐句浮现 / 渐隐")]
    [SerializeField] bool sentenceBySentence = true;
    [SerializeField] float popDuration = 0.6f;
    [SerializeField] float sentenceHoldSeconds = 0.8f;
    [SerializeField] float fadeOutDuration = 1.8f;
    [SerializeField] float gapBetweenSentences = 0.6f;
    [SerializeField] float popFromBelowPixels = 50f;
    [SerializeField] float popFromScale = 0.35f;

    [Header("上飘（旧句让位，避免与新句重叠）")]
    [Tooltip("所有句子都在同一位置冒出；留空则用第一句文本的位置")]
    [SerializeField] RectTransform displayAnchor;
    [SerializeField] float floatUpStepPixels = 58f;
    [SerializeField] float floatUpDuration = 0.45f;
    [Tooltip("渐隐期间额外向上飘的像素")]
    [SerializeField] float floatUpWhileFadePixels = 36f;

    [Header("上下浮动")]
    [SerializeField] float amplitudeYPixels = 10f;
    [SerializeField] float speedY = 2.2f;

    [Header("横向微动（可选）")]
    [SerializeField] float amplitudeXPixels = 0f;
    [SerializeField] float speedX = 1.6f;

    [Header("相位")]
    [SerializeField] float phaseOffsetRadians = 0f;

    [Header("时间轴")]
    [SerializeField] bool useUnscaledTime = true;
    [SerializeField] bool play = true;

    Vector2 _baseAnchored;
    bool _hasBase;
    Coroutine _sentenceRoutine;

    void Reset()
    {
        ApplyRecommendedDefaults();
        if (target == null)
            target = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        CaptureBasePosition();
    }

    void Start()
    {
        if (sentenceBySentence)
            BeginSentenceSequence();
    }

    void OnDisable()
    {
        if (_sentenceRoutine != null)
        {
            StopCoroutine(_sentenceRoutine);
            _sentenceRoutine = null;
        }
        ResetAllSentences();
    }

    void Update()
    {
        if (!play || target == null)
            return;

        if (!_hasBase)
            CaptureBasePosition();

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float dy = Mathf.Sin(t * speedY + phaseOffsetRadians) * amplitudeYPixels;
        float dx = Mathf.Sin(t * speedX + phaseOffsetRadians * 1.37f) * amplitudeXPixels;
        target.anchoredPosition = _baseAnchored + new Vector2(dx, dy);
    }

    public void RecaptureBasePosition()
    {
        _hasBase = false;
        CaptureBasePosition();
    }

    public void RestartSentenceSequence()
    {
        if (_sentenceRoutine != null)
        {
            StopCoroutine(_sentenceRoutine);
            _sentenceRoutine = null;
        }
        ResetAllSentences();
        BeginSentenceSequence();
    }

    void BeginSentenceSequence()
    {
        if (!sentenceBySentence)
            return;

        var lines = CollectSentenceLines();
        if (lines.Count == 0)
        {
            Debug.LogWarning("[TextFloatBob] 没有可用的句子文本。请拖入 TextMeshProUGUI 或旧版 Text，或放在子物体里并勾选 Auto Collect Child Tmps。", this);
            return;
        }

        if (_sentenceRoutine != null)
            StopCoroutine(_sentenceRoutine);
        _sentenceRoutine = StartCoroutine(SentenceSequence(lines));
    }

    List<SentenceLine> CollectSentenceLines()
    {
        var result = new List<SentenceLine>();

        if (HasAssignedSentences())
        {
            if (sentenceTexts != null)
            {
                for (int i = 0; i < sentenceTexts.Length; i++)
                {
                    var tmp = sentenceTexts[i];
                    if (tmp == null)
                        continue;
                    result.Add(WrapSentence(tmp.rectTransform));
                }
            }

            if (sentenceLegacyTexts != null)
            {
                for (int i = 0; i < sentenceLegacyTexts.Length; i++)
                {
                    var text = sentenceLegacyTexts[i];
                    if (text == null)
                        continue;
                    result.Add(WrapSentence(text.rectTransform));
                }
            }

            return result;
        }

        if (!autoCollectChildTmps)
            return result;

        var found = new List<(int siblingIndex, RectTransform rect)>();
        var selfGo = gameObject;

        var selfTmp = GetComponent<TextMeshProUGUI>();
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp == selfTmp)
                continue;
            found.Add((tmp.transform.GetSiblingIndex(), tmp.rectTransform));
        }

        var selfText = GetComponent<Text>();
        foreach (var text in GetComponentsInChildren<Text>(true))
        {
            if (text == selfText)
                continue;
            found.Add((text.transform.GetSiblingIndex(), text.rectTransform));
        }

        found.Sort((a, b) => a.siblingIndex.CompareTo(b.siblingIndex));
        for (int i = 0; i < found.Count; i++)
            result.Add(WrapSentence(found[i].rect));

        return result;
    }

    static bool HasAnyAssigned<T>(T[] array) where T : Object
    {
        if (array == null || array.Length == 0)
            return false;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null)
                return true;
        }
        return false;
    }

    bool HasAssignedSentences() =>
        HasAnyAssigned(sentenceTexts) || HasAnyAssigned(sentenceLegacyTexts);

    SentenceLine WrapSentence(RectTransform rt)
    {
        var group = rt.GetComponent<CanvasGroup>();
        if (group == null)
            group = rt.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        return new SentenceLine(rt, group);
    }

    void CaptureBasePosition()
    {
        if (target == null)
            return;
        _baseAnchored = target.anchoredPosition;
        _hasBase = true;
    }

    void ResetAllSentences()
    {
        var lines = CollectSentenceLines();
        for (int i = 0; i < lines.Count; i++)
            ResetLine(lines[i]);
    }

    void ResetLine(SentenceLine line)
    {
        StopLineMotion(line);
        line.Rect.anchoredPosition = line.BaseAnchoredPosition;
        line.Rect.localScale = Vector3.one;
        line.Group.alpha = 0f;
        line.Root.SetActive(true);
    }

    void HideAllSentences(List<SentenceLine> lines)
    {
        Vector2 spawn = GetSpawnPosition(lines);
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            line.BaseAnchoredPosition = line.Rect.anchoredPosition;
            line.SpawnPosition = spawn;
            line.Rect.localScale = Vector3.one * popFromScale;
            line.Group.alpha = 0f;
            line.Root.SetActive(true);
        }
    }

    Vector2 GetSpawnPosition(List<SentenceLine> lines)
    {
        if (displayAnchor != null)
            return displayAnchor.anchoredPosition;
        if (lines.Count > 0)
            return lines[0].BaseAnchoredPosition;
        return Vector2.zero;
    }

    IEnumerator SentenceSequence(List<SentenceLine> lines)
    {
        yield return null;
        HideAllSentences(lines);

        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
                FloatUpPreviousLines(lines, i);

            var line = lines[i];
            line.EndAnchoredPosition = line.SpawnPosition;
            line.Rect.anchoredPosition = line.EndAnchoredPosition + new Vector2(0f, -popFromBelowPixels);

            StartCoroutine(FadeOutAfter(line, sentenceHoldSeconds, fadeOutDuration));
            yield return PopIn(line);
            yield return Wait(gapBetweenSentences);
        }

        _sentenceRoutine = null;
    }

    void FloatUpPreviousLines(List<SentenceLine> lines, int beforeIndex)
    {
        for (int j = 0; j < beforeIndex; j++)
        {
            var prev = lines[j];
            if (!prev.Root.activeSelf || prev.Group.alpha <= 0.01f)
                continue;
            StartFloatUp(prev, floatUpStepPixels, floatUpDuration);
        }
    }

    IEnumerator PopIn(SentenceLine line)
    {
        float dur = Mathf.Max(0.01f, popDuration);
        float elapsed = 0f;
        var rt = line.Rect;
        Vector2 endPos = line.EndAnchoredPosition;
        Vector2 startPos = endPos + new Vector2(0f, -popFromBelowPixels);
        float z = popFromScale;

        while (elapsed < dur)
        {
            elapsed += DeltaTime();
            float u = Mathf.Clamp01(elapsed / dur);
            float e = 1f - Mathf.Pow(1f - u, 3f);
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, e);
            float s = Mathf.LerpUnclamped(z, 1f, e);
            rt.localScale = new Vector3(s, s, 1f);
            line.Group.alpha = Mathf.LerpUnclamped(0f, 1f, e);
            yield return null;
        }

        rt.anchoredPosition = endPos;
        rt.localScale = Vector3.one;
        line.Group.alpha = 1f;
    }

    IEnumerator FadeOutAfter(SentenceLine line, float hold, float fade)
    {
        yield return Wait(hold);
        float fd = Mathf.Max(0.01f, fade);
        StartFloatUp(line, floatUpWhileFadePixels, fd);

        float startA = line.Group.alpha;
        float t = 0f;
        while (t < fd)
        {
            t += DeltaTime();
            float u = Mathf.Clamp01(t / fd);
            line.Group.alpha = Mathf.LerpUnclamped(startA, 0f, u);
            yield return null;
        }

        line.Group.alpha = 0f;
        line.Root.SetActive(false);
    }

    void StartFloatUp(SentenceLine line, float deltaY, float duration)
    {
        if (line.MotionRoutine != null)
            StopCoroutine(line.MotionRoutine);
        line.MotionRoutine = StartCoroutine(FloatUpRoutine(line, deltaY, duration));
    }

    void StopLineMotion(SentenceLine line)
    {
        if (line.MotionRoutine != null)
        {
            StopCoroutine(line.MotionRoutine);
            line.MotionRoutine = null;
        }
    }

    IEnumerator FloatUpRoutine(SentenceLine line, float deltaY, float duration)
    {
        float dur = Mathf.Max(0.01f, duration);
        Vector2 from = line.Rect.anchoredPosition;
        Vector2 to = from + new Vector2(0f, deltaY);
        float t = 0f;
        while (t < dur)
        {
            t += DeltaTime();
            float u = Mathf.Clamp01(t / dur);
            float e = 1f - Mathf.Pow(1f - u, 2f);
            line.Rect.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }

        line.Rect.anchoredPosition = to;
        line.EndAnchoredPosition = to;
        line.MotionRoutine = null;
    }

    float DeltaTime() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f)
            yield break;
        float t = 0f;
        while (t < seconds)
        {
            t += DeltaTime();
            yield return null;
        }
    }

    void ApplyRecommendedDefaults()
    {
        sentenceBySentence = true;
        autoCollectChildTmps = true;
        popDuration = 0.6f;
        sentenceHoldSeconds = 0.8f;
        fadeOutDuration = 1.8f;
        gapBetweenSentences = 0.6f;
        popFromBelowPixels = 50f;
        popFromScale = 0.35f;
        floatUpStepPixels = 58f;
        floatUpDuration = 0.45f;
        floatUpWhileFadePixels = 36f;
    }

    sealed class SentenceLine
    {
        public readonly RectTransform Rect;
        public readonly GameObject Root;
        public readonly CanvasGroup Group;
        public Vector2 BaseAnchoredPosition;
        public Vector2 SpawnPosition;
        public Vector2 EndAnchoredPosition;
        public Coroutine MotionRoutine;

        public SentenceLine(RectTransform rect, CanvasGroup group)
        {
            Rect = rect;
            Root = rect.gameObject;
            Group = group;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
    }
#endif
}
