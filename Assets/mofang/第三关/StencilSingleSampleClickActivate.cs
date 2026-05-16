using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 单采样点 Stencil 门控 + Collider 点击（<see cref="StencilCubeRaycaster2D"/> 命中本物体）：
/// 门控通过后播放音效并 SetActive 指定物体；<see cref="completed"/> 为 true 后不再响应（一次性）。
/// </summary>
[DisallowMultipleComponent]
public class StencilSingleSampleClickActivate : MonoBehaviour, IStencilClickable
{
    [Header("状态")]
    [Tooltip("成功后为 true，不再响应点击")]
    public bool completed;

    [Tooltip("只读：单采样点是否通过三层 RT+颜色判定")]
    public bool singleSampleVisible;

    [Tooltip("只读：点击门控是否放行")]
    public bool clickGateVisible;

    [Header("模板测试（三层 RT，单采样点）")]
    [Tooltip("为 true：点击前用三层 RT 做颜色门控；为 false 时门控恒为通过")]
    public bool useThreeRTForVisibility = true;

    [Tooltip("不填则运行时 FindObjectOfType")]
    public CubeLayerRTPicker rtPicker;

    [Tooltip("与 StencilSingleSampleItemSpriteReplace 相同语义")]
    public bool leftmostDebugPreviewIsFrontLayer = true;

    [Tooltip("唯一采样点（Viewport 0~1）")]
    public Vector2 sampleVp = new Vector2(0.5f, 0.5f);

    public Color targetColor = Color.red;
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;

    public float blackRgbThreshold = 0.02f;
    public float blackAlphaThreshold = 0.01f;
    public bool invertIsVisible;

    [Header("点击成功后")]
    [Tooltip("留空则不播放")]
    public AudioClip clickSfx;

    [Tooltip("Sfx 子标签；与总 Sfx 相乘，可用 VolumeChannelSlider 单独调此标签。留空则仅用总 Sfx")]
    public string sfxTag = "";

    [Tooltip("仅此点击音效的音量（0~1），不影响其它 Sfx")]
    [Range(0f, 1f)]
    public float clickSfxVolume = 1f;

    [Tooltip("成功后 SetActive(true) 的物体")]
    public GameObject activateTarget;

    [Tooltip("为 true：成功后将本脚本所在 GameObject SetActive(false)")]
    public bool deactivateSelfOnSuccess;

    [Header("可选")]
    public UnityEvent onSuccess;

    [Tooltip("失败原因调试输出")]
    public bool debugLog;

    [Header("采样点十字（Game 视图）")]
    public bool showSampleCrossInGame;
    public Color sampleCrossColor = Color.yellow;
    public float sampleCrossHalfSizePixels = 12f;
    public float sampleCrossLineThickness = 2f;
    public float sampleCrossDepth = 2f;

    Texture2D _readTex;
    Texture2D _whiteGuiTex;

    void Awake()
    {
        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        EnsureWhiteGuiTexture();
    }

    void Update()
    {
        if (useThreeRTForVisibility)
            ComputeVisibilityStateByThreeRT();
        else
        {
            singleSampleVisible = true;
            clickGateVisible = true;
        }
    }

    public void OnStencilClick()
    {
        if (!TryPassClickGate())
            return;

        ApplySuccessEffects();
    }

    bool TryPassClickGate()
    {
        if (completed)
        {
            if (debugLog)
                Debug.Log($"[StencilSingleSampleClickActivate] 已完成，忽略 {name}");
            return false;
        }

        if (useThreeRTForVisibility)
        {
            ComputeVisibilityStateByThreeRT();
            if (!clickGateVisible)
            {
                if (debugLog)
                    Debug.Log($"[StencilSingleSampleClickActivate] 门控未通过 singleSampleVisible={singleSampleVisible}");
                return false;
            }
        }

        return true;
    }

    void ApplySuccessEffects()
    {
        if (completed)
            return;

        completed = true;

        PlayClickSfxIfConfigured();

        if (activateTarget != null)
            activateTarget.SetActive(true);
        else if (debugLog)
            Debug.LogWarning("[StencilSingleSampleClickActivate] activateTarget 未设置");

        onSuccess?.Invoke();

        if (debugLog)
            Debug.Log($"[StencilSingleSampleClickActivate] 成功 {name}");

        if (deactivateSelfOnSuccess && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    void PlayClickSfxIfConfigured()
    {
        if (clickSfx == null || AudioManager.Instance == null)
            return;

        string tag = string.IsNullOrWhiteSpace(sfxTag) ? null : sfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clickSfx, tag, clickSfxVolume);
    }

    void ComputeVisibilityStateByThreeRT()
    {
        if (rtPicker == null)
            rtPicker = FindObjectOfType<CubeLayerRTPicker>();
        if (rtPicker == null)
        {
            singleSampleVisible = true;
            clickGateVisible = true;
            return;
        }

        rtPicker.EnsureRTRenderedForSampling();
        RenderTexture rtBack = rtPicker.RtBack;
        RenderTexture rtMid = rtPicker.RtMid;
        RenderTexture rtFront = rtPicker.RtFront;
        if (rtBack == null || rtMid == null || rtFront == null)
        {
            singleSampleVisible = true;
            clickGateVisible = true;
            return;
        }

        if (leftmostDebugPreviewIsFrontLayer)
        {
            RenderTexture tmp = rtBack;
            rtBack = rtFront;
            rtFront = tmp;
        }

        bool v = SampleIsVisibleByThreeRT(sampleVp, rtBack, rtMid, rtFront);
        singleSampleVisible = v;
        clickGateVisible = v;
    }

    bool SampleIsVisibleByThreeRT(Vector2 vp01, RenderTexture rtBack, RenderTexture rtMid, RenderTexture rtFront)
    {
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return false;

        int w = rtFront.width;
        int h = rtFront.height;
        int px = Mathf.Clamp(Mathf.RoundToInt(vp01.x * w), 0, w - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(vp01.y * h), 0, h - 1);

        Color cFront = SampleRT(rtFront, px, py);
        bool frontEmpty = IsBlackEmpty(cFront);
        bool colorHit = ColorsClose(cFront, targetColor, colorTolerance);
        bool visibleRt = !frontEmpty && colorHit;
        return invertIsVisible ? !visibleRt : visibleRt;
    }

    Color SampleRT(RenderTexture rt, int px, int py)
    {
        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;
        _readTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        _readTex.Apply();
        RenderTexture.active = currentRT;
        return _readTex.GetPixel(0, 0);
    }

    static bool ColorsClose(Color a, Color b, float tol)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return dr * dr + dg * dg + db * db <= tol * tol;
    }

    bool IsBlackEmpty(Color c)
    {
        float maxRgb = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        bool blackRgb = maxRgb <= blackRgbThreshold;
        bool blackAlpha = c.a <= blackAlphaThreshold;
        return blackRgb && blackAlpha;
    }

    void OnGUI()
    {
        if (!showSampleCrossInGame)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        EnsureWhiteGuiTexture();
        float z = Mathf.Max(0.01f, sampleCrossDepth);
        DrawCrossOnGameView(cam, sampleVp, z);
    }

    void EnsureWhiteGuiTexture()
    {
        if (_whiteGuiTex != null)
            return;
        _whiteGuiTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _whiteGuiTex.SetPixel(0, 0, Color.white);
        _whiteGuiTex.Apply(false, true);
    }

    void DrawCrossOnGameView(Camera cam, Vector2 vp01, float zWorld)
    {
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return;

        Vector3 sp = cam.ViewportToScreenPoint(new Vector3(vp01.x, vp01.y, zWorld));
        if (sp.z < 0f)
            return;

        float guiX = sp.x;
        float guiY = Screen.height - sp.y;
        float half = Mathf.Max(1f, sampleCrossHalfSizePixels);
        float t = Mathf.Max(1f, sampleCrossLineThickness);

        GUI.color = sampleCrossColor;
        GUI.DrawTexture(new Rect(guiX - half, guiY - t * 0.5f, half * 2f, t), _whiteGuiTex);
        GUI.DrawTexture(new Rect(guiX - t * 0.5f, guiY - half, t, half * 2f), _whiteGuiTex);
        GUI.color = Color.white;
    }
}
