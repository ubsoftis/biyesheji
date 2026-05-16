using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 点击 <see cref="Button"/> 后激活一批物体；停留 <see cref="displayDuration"/> 秒后渐隐，
/// 再 <see cref="GameObject.SetActive"/>(false)。支持 CanvasGroup、SpriteRenderer、UI Graphic。
/// </summary>
public class ButtonActivateFadeOut : MonoBehaviour
{
    [Tooltip("要监听的按钮；留空则在本物体上 GetComponent<Button>()")]
    public Button watchButton;

    [Tooltip("点击后要显示、再淡出并关闭的物体（可含子物体上的 Sprite / UI）")]
    public GameObject[] targets;

    [Tooltip("激活后保持完全不透明的秒数，再开始淡出")]
    [Min(0f)]
    public float displayDuration = 2f;

    [Tooltip("淡出过程时长（秒）")]
    [Min(0.01f)]
    public float fadeOutDuration = 1f;

    [Tooltip("淡出是否使用 unscaledTime（不受 Time.timeScale 影响）")]
    public bool fadeUsesUnscaledTime;

    [Tooltip("首次触发后禁用按钮，防止重复点击")]
    public bool disableButtonAfterClick = true;

    [Tooltip("为 true 时只执行一轮（再次点击无效）")]
    public bool onlyOnce = true;

    [Header("状态（只读）")]
    public bool hasTriggered;

    bool _fired;
    Coroutine _routine;

    readonly List<FadeChannel> _channels = new List<FadeChannel>(32);

    struct FadeChannel
    {
        public CanvasGroup CanvasGroup;
        public SpriteRenderer SpriteRenderer;
        public Graphic Graphic;
        public float OriginalAlpha;
    }

    void Awake()
    {
        if (watchButton == null)
            watchButton = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (watchButton != null)
            watchButton.onClick.AddListener(OnButtonClicked);
    }

    void OnDisable()
    {
        if (watchButton != null)
            watchButton.onClick.RemoveListener(OnButtonClicked);
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    void OnButtonClicked()
    {
        if (onlyOnce && _fired)
            return;

        _fired = true;
        hasTriggered = true;

        if (disableButtonAfterClick && watchButton != null)
            watchButton.interactable = false;

        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(ShowHoldFadeHideRoutine());
    }

    IEnumerator ShowHoldFadeHideRoutine()
    {
        ActivateAndPrepareChannels();

        float hold = Mathf.Max(0f, displayDuration);
        if (hold > 0f)
        {
            if (fadeUsesUnscaledTime)
                yield return new WaitForSecondsRealtime(hold);
            else
                yield return new WaitForSeconds(hold);
        }

        float dur = Mathf.Max(0.01f, fadeOutDuration);
        float t = 0f;
        while (t < dur)
        {
            t += fadeUsesUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / dur);
            ApplyAlpha(k);
            yield return null;
        }

        ApplyAlpha(0f);
        DeactivateTargets();
        _routine = null;
    }

    void ActivateAndPrepareChannels()
    {
        _channels.Clear();
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            var go = targets[i];
            if (go == null)
                continue;

            go.SetActive(true);
            CollectChannelsFrom(go.transform);
        }

        ApplyAlpha(1f);
    }

    void CollectChannelsFrom(Transform root)
    {
        var groups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            var g = groups[i];
            if (g == null || AlreadyHasCanvasGroup(g))
                continue;
            _channels.Add(new FadeChannel
            {
                CanvasGroup = g,
                OriginalAlpha = g.alpha < 0.001f ? 1f : g.alpha
            });
        }

        var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            var sr = sprites[i];
            if (sr == null || AlreadyHasSprite(sr))
                continue;
            var c = sr.color;
            _channels.Add(new FadeChannel
            {
                SpriteRenderer = sr,
                OriginalAlpha = c.a < 0.001f ? 1f : c.a
            });
        }

        var graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var gr = graphics[i];
            if (gr == null || AlreadyHasGraphic(gr))
                continue;
            // 父级已有 CanvasGroup 时只淡出 Group，避免透明度被叠乘两次
            if (HasFadingCanvasGroupAncestor(gr.transform))
                continue;
            var c = gr.color;
            _channels.Add(new FadeChannel
            {
                Graphic = gr,
                OriginalAlpha = c.a < 0.001f ? 1f : c.a
            });
        }
    }

    bool HasFadingCanvasGroupAncestor(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            var cg = p.GetComponent<CanvasGroup>();
            if (cg != null && AlreadyHasCanvasGroup(cg))
                return true;
        }
        return false;
    }

    bool AlreadyHasCanvasGroup(CanvasGroup g)
    {
        for (int i = 0; i < _channels.Count; i++)
        {
            if (_channels[i].CanvasGroup == g)
                return true;
        }
        return false;
    }

    bool AlreadyHasSprite(SpriteRenderer sr)
    {
        for (int i = 0; i < _channels.Count; i++)
        {
            if (_channels[i].SpriteRenderer == sr)
                return true;
        }
        return false;
    }

    bool AlreadyHasGraphic(Graphic gr)
    {
        for (int i = 0; i < _channels.Count; i++)
        {
            if (_channels[i].Graphic == gr)
                return true;
        }
        return false;
    }

    void ApplyAlpha(float normalized)
    {
        float n = Mathf.Clamp01(normalized);
        for (int i = 0; i < _channels.Count; i++)
        {
            var ch = _channels[i];
            float a = ch.OriginalAlpha * n;
            if (ch.CanvasGroup != null)
                ch.CanvasGroup.alpha = a;
            if (ch.SpriteRenderer != null)
            {
                var c = ch.SpriteRenderer.color;
                c.a = a;
                ch.SpriteRenderer.color = c;
            }
            if (ch.Graphic != null)
            {
                var c = ch.Graphic.color;
                c.a = a;
                ch.Graphic.color = c;
            }
        }
    }

    void DeactivateTargets()
    {
        RestoreFullAlpha();
        if (targets == null)
            return;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(false);
        }
        _channels.Clear();
    }

    void RestoreFullAlpha()
    {
        ApplyAlpha(1f);
    }
}
