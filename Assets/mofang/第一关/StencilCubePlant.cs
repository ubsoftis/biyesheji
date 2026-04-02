using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StencilCubePlant : MonoBehaviour
{
    [Header("相机与蒙版贴图")]
    public Camera mainCamera;          // 真正玩游戏看的相机（你现在的 Camera）
    public Camera maskCamera;          // 专门渲染蒙版的相机
    public RenderTexture maskTexture;  // maskCamera 的 TargetTexture，例如 64x64

    [Header("判定颜色（面2 可见时的颜色）")]
    public Color targetColor = Color.red;   // 面2 用的纯色
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;    // 允许一点点误差

    [Header("检测频率")]
    public int framesInterval = 1;          // 每几帧检测一次，1=每帧

    [Header("白块离开状态")]
    [Tooltip("当前这一帧，蒙版上该点是否是目标颜色（白块还在）")]
    public bool isVisible = false;

    [Header("hitAndGone 判断门控")]
    [Tooltip("为 true 时才会根据“上一帧可见、本帧不可见”去置 hitAndGone=true；为 false 时不进行该判断，并保持 hitAndGone=false。")]
    public bool enableHitAndGoneCheck = false;

    [Tooltip("是否已经：白块从当前视图中消失（上一帧可见，这一帧不可见）")]
    public bool hitAndGone = false;

    [Header("点击门控方式")]
    [Tooltip("为 true 时，用 isVisible 自动开关 Collider2D；为 false 时不再改 Collider，由点击射线逻辑自行读取 isVisible 做 gating。")]
    public bool controlColliderByVisibility = true;

    [Header("RT 三层遮挡判定（黑色为空）")]
    [Tooltip("为 true 时，用 CubeLayerRTPicker 的三张 RT（Back/Mid/Front）采样决定 isVisible。")]
    public bool useThreeRTForVisibility = true;
    [Tooltip("需要时可手动拖引用；不填会运行时自动 FindObjectOfType。")]
    public CubeLayerRTPicker rtPicker;
    [Tooltip("判定黑色为空的 RGB 阈值（RGB 最大值 <= 该值认为黑）")]
    public float blackRgbThreshold = 0.02f;
    [Tooltip("判定为空的 Alpha 阈值（alpha <= 该值认为黑）。")]
    public float blackAlphaThreshold = 0.01f;
    [Tooltip("若为 true，则 isVisible 反转（把黑当成可见/白当成不可见）。")]
    public bool invertIsVisible = false;

    Collider2D _col;
    Texture2D _readTex;
    int _frameCount;

    bool _lastVisible = false;   // 记录上一帧的可见状态

    void Awake()
    {
        _col = GetComponent<Collider2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (maskCamera == null)
        {
            Debug.LogError("[StencilMaskClickGate] 未指定 maskCamera。");
        }

        if (maskTexture == null && maskCamera != null)
        {
            maskTexture = maskCamera.targetTexture;
        }

        // 无论用不用 maskTexture，都需要一个 1x1 读像素纹理（用于 RT 采样）
        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
    }

    void Update()
    {
        if (_readTex == null)
            return;

        if (useThreeRTForVisibility)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (rtPicker == null)
                rtPicker = FindObjectOfType<CubeLayerRTPicker>();
            if (rtPicker == null)
                return;

            rtPicker.EnsureRTRenderedForSampling();
            var rtBack = rtPicker.RtBack;
            var rtMid = rtPicker.RtMid;
            var rtFront = rtPicker.RtFront;
            if (rtBack == null || rtMid == null || rtFront == null)
                return;

            _frameCount++;
            if (_frameCount % framesInterval != 0)
                return;

            Vector3 vpRT = mainCamera.WorldToViewportPoint(transform.position);
            if (vpRT.z <= 0f || vpRT.x < 0f || vpRT.x > 1f || vpRT.y < 0f || vpRT.y > 1f)
            {
                isVisible = false;
                if (controlColliderByVisibility && _col != null) _col.enabled = false;
                return;
            }

            int w = rtFront.width;
            int h = rtFront.height;
            int pxRt = Mathf.Clamp(Mathf.RoundToInt(vpRT.x * w), 0, w - 1);
            int pyRt = Mathf.Clamp(Mathf.RoundToInt(vpRT.y * h), 0, h - 1);

            Color cBack = SampleRT(rtBack, pxRt, pyRt);
            Color cMid = SampleRT(rtMid, pxRt, pyRt);
            Color cFront = SampleRT(rtFront, pxRt, pyRt);

            bool backEmpty = IsBlackEmpty(cBack);
            bool midEmpty = IsBlackEmpty(cMid);
            bool frontEmpty = IsBlackEmpty(cFront);

            bool foundNonEmpty = false;
            bool visibleRt = false;
            if (!backEmpty) { foundNonEmpty = true; visibleRt = true; }
            if (!midEmpty) { foundNonEmpty = true; visibleRt = true; }
            if (!frontEmpty) { foundNonEmpty = true; visibleRt = true; }

            visibleRt = invertIsVisible ? !visibleRt : visibleRt;
            isVisible = visibleRt;

            if (controlColliderByVisibility && _col != null)
                _col.enabled = isVisible;

            // “刚刚离开”：从可见 -> 不可见（受门控控制）
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
            return;
        }

        if (mainCamera == null || maskCamera == null || maskTexture == null || _readTex == null)
            return;

        _frameCount++;
        if (_frameCount % framesInterval != 0)
            return;

        // 1. 计算物体在主相机下的屏幕归一化坐标
        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        if (vp.z <= 0f)
        {
            // 在相机背后，肯定点不到
            if (controlColliderByVisibility && _col != null) _col.enabled = false;
            isVisible = false;
            return;
        }

        // 超出屏幕范围，也不用点
        if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
        {
            if (controlColliderByVisibility && _col != null) _col.enabled = false;
            isVisible = false;
            return;
        }

        // 2. 映射到蒙版贴图上的像素坐标
        int px = Mathf.Clamp(Mathf.RoundToInt(vp.x * maskTexture.width), 0, maskTexture.width - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(vp.y * maskTexture.height), 0, maskTexture.height - 1);

        // 3. 从 RenderTexture 读 1x1 像素
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = maskTexture;
        _readTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        _readTex.Apply();
        RenderTexture.active = currentRT;

        Color c = _readTex.GetPixel(0, 0);

        // 4. 比较颜色是否接近 targetColor
        bool visible = ColorsClose(c, targetColor, colorTolerance);
        isVisible = visible;

        // 控制 Collider：只有白块还在时才允许被射线打中
        if (controlColliderByVisibility && _col != null)
            _col.enabled = visible;

        // 5. 检测“刚刚离开”的那一帧：
        //    条件：上一帧可见，这一帧不可见
        if (enableHitAndGoneCheck)
        {
            if (_lastVisible && !visible)
                hitAndGone = true;
        }
        else
        {
            hitAndGone = false;
        }

        _lastVisible = visible;

        // 如需调试，可打开这一句：
        // Debug.Log($"[StencilCubePlant] vp={vp}, pixel=({px},{py}), color={c}, visible={visible}, hitAndGone={hitAndGone}");
    }

    bool ColorsClose(Color a, Color b, float tol)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tol * tol;
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
