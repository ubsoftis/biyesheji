using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
/// <summary>
/// UI 文字：可选按 <see cref="RectTransform.anchoredPosition"/> 做正弦浮动；
/// 可选「逐句」浮现（带轻微弹出）并在一段时间后逐句渐隐。
/// 逐句模式需要同物体上有 <see cref="TextMeshProUGUI"/>，整段文案写在 TMP 的 Text 里即可。
/// </summary>
[DisallowMultipleComponent]
public class TextFloatBob : MonoBehaviour
{
    [Tooltip("留空则用本物体上的 RectTransform")]
    [SerializeField] RectTransform target;

    [Header("逐句浮现 / 渐隐")]
    [SerializeField] bool sentenceBySentence = true;
    [Tooltip("弹出时长（缩放 + 位移 + 透明度）")]
    [SerializeField] float popDuration = 0.4f;
    [Tooltip("每句保持完全不透明的时间，之后开始渐隐")]
    [SerializeField] float sentenceHoldSeconds = 2f;
    [Tooltip("每句渐隐时长")]
    [SerializeField] float fadeOutDuration = 1.25f;
    [Tooltip("两句依次开始浮现的间隔")]
    [SerializeField] float gapBetweenSentences = 0.2f;
    [SerializeField] float lineSpacingPixels = 4f;
    [Tooltip("冒出时从下方偏移的像素")]
    [SerializeField] float popFromBelowPixels = 18f;
    [Tooltip("冒出时从多少缩放到 1")]
    [SerializeField] float popFromScale = 0.72f;

    [Header("上下浮动")]
    [SerializeField] float amplitudeYPixels = 10f;
    [SerializeField] float speedY = 2.2f;

    [Header("横向微动（可选）")]
    [SerializeField] float amplitudeXPixels = 0f;
    [SerializeField] float speedX = 1.6f;

    [Header("相位")]
    [Tooltip("弧度偏移，多条文字可错开相位")]
    [SerializeField] float phaseOffsetRadians = 0f;

    [Header("时间轴")]
    [SerializeField] bool useUnscaledTime = true;

    [Tooltip("关闭后可在代码里改 play 再开")]
    [SerializeField] bool play = true;

    static readonly char[] SentenceEndChars =
    {
        '。', '！', '？', '…', '.', '!', '?', ';', '；', '\n', '\r'
    };

    Vector2 _baseAnchored;
    bool _hasBase;

    TextMeshProUGUI _sourceTmp;
    RectTransform _sentenceHolder;
    Coroutine _sentenceRoutine;

    void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
        _sourceTmp = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        CaptureBasePosition();
        if (sentenceBySentence && _sourceTmp != null)
        {
            if (_sentenceRoutine != null)
                StopCoroutine(_sentenceRoutine);
            _sentenceRoutine = StartCoroutine(SentenceSequence());
        }
    }

    void OnDisable()
    {
        if (_sentenceRoutine != null)
        {
            StopCoroutine(_sentenceRoutine);
            _sentenceRoutine = null;
        }
        ClearSentenceChildren();
        if (_sourceTmp != null)
            _sourceTmp.enabled = true;
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

    /// <summary>
    /// 当外部改动了 anchoredPosition 后调用，重新以当前位置为「静止中心」。
    /// </summary>
    public void RecaptureBasePosition()
    {
        _hasBase = false;
        CaptureBasePosition();
    }

    void CaptureBasePosition()
    {
        if (target == null)
            return;
        _baseAnchored = target.anchoredPosition;
        _hasBase = true;
    }

    IEnumerator SentenceSequence()
    {
        string full = _sourceTmp.text;
        var sentences = SplitSentences(full);
        if (sentences.Count == 0)
            yield break;

        _sourceTmp.enabled = false;
        EnsureSentenceHolder();
        ClearSentenceChildren();
        yield return null;

        float lineWidth = Mathf.Max(1f, _sentenceHolder.rect.width);
        if (lineWidth < 2f)
            lineWidth = Mathf.Max(1f, _sourceTmp.rectTransform.rect.width);

        float yTop = 0f;

        foreach (var sentence in sentences)
        {
            var line = CreateSentenceLine(sentence, lineWidth);
            line.Tmp.ForceMeshUpdate(true);
            float h = line.Tmp.GetPreferredValues(line.Tmp.text, lineWidth, 0f).y;
            line.Rect.sizeDelta = new Vector2(lineWidth, Mathf.Max(line.Tmp.fontSize * 1.2f, h));
            var pos = new Vector2(0f, yTop);
            line.Rect.anchoredPosition = pos + new Vector2(0f, -popFromBelowPixels);
            yTop -= line.Rect.sizeDelta.y + lineSpacingPixels;

            var ready = new SentenceLine(line.Rect, line.Tmp, pos, line.TargetAlpha);
            StartCoroutine(PopIn(ready));
            StartCoroutine(FadeOutAfter(ready, sentenceHoldSeconds, fadeOutDuration));
            yield return Wait(gapBetweenSentences);
        }

        _sentenceRoutine = null;
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
            var c = line.Tmp.color;
            c.a = Mathf.LerpUnclamped(0f, line.TargetAlpha, e);
            line.Tmp.color = c;
            yield return null;
        }

        rt.anchoredPosition = endPos;
        rt.localScale = Vector3.one;
        var cf = line.Tmp.color;
        cf.a = line.TargetAlpha;
        line.Tmp.color = cf;
    }

    IEnumerator FadeOutAfter(SentenceLine line, float hold, float fade)
    {
        yield return Wait(hold);
        float fd = Mathf.Max(0.01f, fade);
        float startA = line.Tmp.color.a;
        float t = 0f;
        while (t < fd)
        {
            t += DeltaTime();
            float u = Mathf.Clamp01(t / fd);
            var c = line.Tmp.color;
            c.a = Mathf.LerpUnclamped(startA, 0f, u);
            line.Tmp.color = c;
            yield return null;
        }

        var end = line.Tmp.color;
        end.a = 0f;
        line.Tmp.color = end;
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

    void EnsureSentenceHolder()
    {
        if (_sentenceHolder != null)
            return;

        var holderGo = new GameObject("SentenceLines", typeof(RectTransform));
        _sentenceHolder = holderGo.GetComponent<RectTransform>();
        _sentenceHolder.SetParent(_sourceTmp.rectTransform, false);
        _sentenceHolder.anchorMin = Vector2.zero;
        _sentenceHolder.anchorMax = Vector2.one;
        _sentenceHolder.offsetMin = Vector2.zero;
        _sentenceHolder.offsetMax = Vector2.zero;
        _sentenceHolder.pivot = new Vector2(0.5f, 1f);
    }

    void ClearSentenceChildren()
    {
        if (_sentenceHolder == null)
            return;
        for (int i = _sentenceHolder.childCount - 1; i >= 0; i--)
            Destroy(_sentenceHolder.GetChild(i).gameObject);
    }

    SentenceLine CreateSentenceLine(string text, float lineWidth)
    {
        var go = new GameObject("Sentence", typeof(RectTransform));
        go.transform.SetParent(_sentenceHolder, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.localScale = Vector3.one * popFromScale;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        CopyTmpFromSource(tmp);
        tmp.text = text;
        tmp.raycastTarget = false;
        float targetA = tmp.color.a;
        var c0 = tmp.color;
        c0.a = 0f;
        tmp.color = c0;

        rt.sizeDelta = new Vector2(lineWidth, _sourceTmp.fontSize * 2f);
        return new SentenceLine(rt, tmp, Vector2.zero, targetA);
    }

    void CopyTmpFromSource(TextMeshProUGUI dst)
    {
        dst.font = _sourceTmp.font;
        dst.fontSharedMaterials = _sourceTmp.fontSharedMaterials;
        dst.fontSize = _sourceTmp.fontSize;
        dst.fontStyle = _sourceTmp.fontStyle;
        dst.color = _sourceTmp.color;
        dst.alignment = _sourceTmp.alignment;
        dst.characterSpacing = _sourceTmp.characterSpacing;
        dst.wordSpacing = _sourceTmp.wordSpacing;
        dst.lineSpacing = _sourceTmp.lineSpacing;
        dst.paragraphSpacing = _sourceTmp.paragraphSpacing;
        dst.margin = _sourceTmp.margin;
        dst.enableWordWrapping = _sourceTmp.enableWordWrapping;
        dst.overflowMode = _sourceTmp.overflowMode;
    }

    static List<string> SplitSentences(string raw)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(raw))
            return list;

        var sb = new StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
            char ch = raw[i];
            if (ch == '\r')
                continue;

            sb.Append(ch);
            if (IsSentenceEnd(ch) || ch == '\n')
            {
                string part = sb.ToString().Trim();
                if (part.Length > 0)
                    list.Add(part);
                sb.Clear();
            }
        }

        string tail = sb.ToString().Trim();
        if (tail.Length > 0)
            list.Add(tail);

        return list;
    }

    static bool IsSentenceEnd(char c)
    {
        for (int i = 0; i < SentenceEndChars.Length; i++)
        {
            if (SentenceEndChars[i] == c)
                return true;
        }
        return false;
    }

    sealed class SentenceLine
    {
        public readonly RectTransform Rect;
        public readonly TextMeshProUGUI Tmp;
        public readonly Vector2 EndAnchoredPosition;
        public readonly float TargetAlpha;

        public SentenceLine(RectTransform rect, TextMeshProUGUI tmp, Vector2 endAnchored, float targetAlpha)
        {
            Rect = rect;
            Tmp = tmp;
            EndAnchoredPosition = endAnchored;
            TargetAlpha = targetAlpha;
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
