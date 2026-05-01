using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StencilCubePlant : MonoBehaviour
{
    [Header("相机与蒙版贴图")]
    public Camera mainCamera;
    public Camera maskCamera;
    public RenderTexture maskTexture;

    [Header("主采样点（控制 isVisible）")]
    [Tooltip("Viewport 坐标(0-1)。主采样点命中即 isVisible=true。")]
    public Vector2 mainSampleVp = new Vector2(0.5f, 0.9f);

    [Header("单独采样点（独立判断）")]
    [Tooltip("单独一个采样点（Viewport 0-1），仅用于 singleSampleVisible，不覆盖 isVisible。")]
    public Vector2 singleSampleVp = new Vector2(0.5f, 0.5f);
    [Tooltip("只读：单独采样点是否命中目标。")]
    public bool singleSampleVisible = false;

    [Header("扩展：9 个采样点（任意命中即 true）")]
    [Tooltip("为 true 时：计算 anyOf9Visible。三层 RT 模式下仅对「屏幕左下角调试预览最左边那张」即 RtBack 做点采样（与 CubeLayerRTPicker 绘制顺序一致）。")]
    public bool enable9Samples = false;
    [Tooltip("9 个采样点的 Viewport 坐标(0-1)。建议按关卡检测位填写。")]
    public Vector2[] sampleVp9 = new Vector2[]
    {
        new Vector2(0.35f, 0.70f),
        new Vector2(0.50f, 0.70f),
        new Vector2(0.65f, 0.70f),
        new Vector2(0.35f, 0.18f),
        new Vector2(0.50f, 0.18f),
        new Vector2(0.65f, 0.18f),
        new Vector2(0.35f, 0.45f),
        new Vector2(0.50f, 0.45f),
        new Vector2(0.65f, 0.45f),
    };
    [Tooltip("只读：9 点采样中是否任意一点命中目标颜色（仅看 RtBack，与 CubeLayerRTPicker 调试预览最左侧那张一致）。")]
    public bool anyOf9Visible = false;

    [Header("判定颜色（面2 可见时的颜色）")]
    public Color targetColor = Color.red;
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;

    [Header("检测频率")]
    public int framesInterval = 1;

    [Header("白块离开状态")]
    [Tooltip("当前这一帧，主采样点是否命中（白块还在）")]
    public bool isVisible = false;

    [Header("hitAndGone 判断门控")]
    [Tooltip("为 true 时才会根据“上一帧可见、本帧不可见”去置 hitAndGone=true；为 false 时保持 hitAndGone=false。")]
    public bool enableHitAndGoneCheck = false;

    [Tooltip("是否已经：白块从当前视图中消失（上一帧可见，这一帧不可见）")]
    public bool hitAndGone = false;

    [Header("点击门控方式")]
    [Tooltip("为 true 时，用 isVisible 自动开关 Collider2D；为 false 时不再改 Collider。")]
    public bool controlColliderByVisibility = true;

    [Header("RT 三层遮挡判定（黑色为空）")]
    [Tooltip("为 true 时，用三张 RT 采样：前层非黑且颜色贴近 targetColor 时 isVisible/singleSampleVisible 才为 true。")]
    public bool useThreeRTForVisibility = true;
    [Tooltip("需要时可手动拖引用；不填会运行时自动 FindObjectOfType。")]
    public CubeLayerRTPicker rtPicker;
    [Tooltip("隐藏屏幕左下角 RT 调试预览（对应 CubeLayerRTPicker.showDebugRTPreviews）。")]
    public bool hideRTDebugPreview = true;
    [Tooltip("判定黑色为空的 RGB 阈值（RGB 最大值 <= 该值认为黑）。")]
    public float blackRgbThreshold = 0.02f;
    [Tooltip("判定为空的 Alpha 阈值（alpha <= 该值认为黑）。")]
    public float blackAlphaThreshold = 0.01f;
    [Tooltip("若为 true，则可见性反转。")]
    public bool invertIsVisible = false;

    Collider2D _col;
    Texture2D _readTex;
    int _frameCount;
    bool _lastVisible = false;

    void Awake()
    {
        _col = GetComponent<Collider2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (maskCamera == null)
            Debug.LogError("[StencilMaskClickGate] 未指定 maskCamera。");

        if (maskTexture == null && maskCamera != null)
            maskTexture = maskCamera.targetTexture;

        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
    }

    void Update()
    {
        if (_readTex == null)
            return;

        _frameCount++;
        if (_frameCount % framesInterval != 0)
            return;

        if (useThreeRTForVisibility)
        {
            UpdateByThreeRT();
            return;
        }

        UpdateByMaskTexture();
    }

    void UpdateByThreeRT()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (rtPicker == null)
            rtPicker = FindObjectOfType<CubeLayerRTPicker>();
        if (rtPicker == null)
            return;

        // 由本脚本统一控制左下角 RT 预览是否显示，避免调试图遮挡画面。
        rtPicker.showDebugRTPreviews = !hideRTDebugPreview;

        rtPicker.EnsureRTRenderedForSampling();
        var rtBack = rtPicker.RtBack;
        var rtMid = rtPicker.RtMid;
        var rtFront = rtPicker.RtFront;
        if (rtBack == null || rtMid == null || rtFront == null)
            return;

        isVisible = SampleIsVisibleByThreeRT(mainSampleVp, rtBack, rtMid, rtFront);

        // anyOf9Visible：只认 RtBack（与 showDebugRTPreviews 时屏幕最左侧那张一致），避免与中/前层混淆
        bool any9 = false;
        if (enable9Samples && sampleVp9 != null && sampleVp9.Length > 0)
        {
            for (int i = 0; i < sampleVp9.Length; i++)
            {
                if (SampleIsTargetColor(rtBack, sampleVp9[i]))
                {
                    any9 = true;
                    break;
                }
            }
        }
        anyOf9Visible = any9;

        singleSampleVisible = SampleIsVisibleByThreeRT(singleSampleVp, rtBack, rtMid, rtFront);
        ApplyVisibilityResults();
    }

    void UpdateByMaskTexture()
    {
        if (mainCamera == null || maskCamera == null || maskTexture == null)
            return;

        isVisible = SampleIsTargetColor(maskTexture, mainSampleVp);

        bool any9 = false;
        if (enable9Samples && sampleVp9 != null && sampleVp9.Length > 0)
        {
            for (int i = 0; i < sampleVp9.Length; i++)
            {
                if (SampleIsTargetColor(maskTexture, sampleVp9[i]))
                {
                    any9 = true;
                    break;
                }
            }
        }
        anyOf9Visible = any9;

        singleSampleVisible = SampleIsTargetColor(maskTexture, singleSampleVp);
        ApplyVisibilityResults();
    }

    void ApplyVisibilityResults()
    {
        if (controlColliderByVisibility && _col != null)
            _col.enabled = isVisible;

        if (enableHitAndGoneCheck)
        {
            if (_lastVisible && !isVisible)
                hitAndGone = true;
        }
        else
        {
            hitAndGone = false;
        }

        _lastVisible = isVisible;
    }

    bool ColorsClose(Color a, Color b, float tol)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tol * tol;
    }

    bool SampleIsTargetColor(RenderTexture rt, Vector2 vp01)
    {
        if (rt == null)
            return false;
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return false;

        int px = Mathf.Clamp(Mathf.RoundToInt(vp01.x * rt.width), 0, rt.width - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(vp01.y * rt.height), 0, rt.height - 1);
        Color c = SampleRT(rt, px, py);
        return ColorsClose(c, targetColor, colorTolerance);
    }

    bool SampleIsVisibleByThreeRT(Vector2 vp01, RenderTexture rtBack, RenderTexture rtMid, RenderTexture rtFront)
    {
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return false;
        if (rtBack == null || rtMid == null || rtFront == null)
            return false;

        int w = rtFront.width;
        int h = rtFront.height;
        int px = Mathf.Clamp(Mathf.RoundToInt(vp01.x * w), 0, w - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(vp01.y * h), 0, h - 1);

        Color cBack = SampleRT(rtBack, px, py);
        Color cMid = SampleRT(rtMid, px, py);
        Color cFront = SampleRT(rtFront, px, py);

        bool backEmpty = IsBlackEmpty(cBack);
        bool midEmpty = IsBlackEmpty(cMid);
        bool frontEmpty = IsBlackEmpty(cFront);

        // 前层有内容且该像素颜色与 targetColor 一致才算「点到目标面」；避免仅靠非黑当作可点。
        bool colorHit = ColorsClose(cFront, targetColor, colorTolerance);
        bool visibleRt = !frontEmpty && colorHit;
        return invertIsVisible ? !visibleRt : visibleRt;
    }

    Color SampleRT(RenderTexture rt, int px, int py)
    {
        var currentRT = RenderTexture.active;
        RenderTexture.active = rt;
        _readTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        _readTex.Apply();
        RenderTexture.active = currentRT;
        return _readTex.GetPixel(0, 0);
    }

    bool IsBlackEmpty(Color c)
    {
        float maxRgb = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        bool blackRgb = maxRgb <= blackRgbThreshold;
        bool blackAlpha = c.a <= blackAlphaThreshold;
        return blackRgb && blackAlpha;
    }
}
