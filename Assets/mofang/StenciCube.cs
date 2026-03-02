using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StenciCube : MonoBehaviour
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

    [Header("点击 + 白块离开 组合状态")]
    [Tooltip("当前这一帧，蒙版上该点是否是目标颜色（白块还在）")]
    public bool isVisible = false;

    [Tooltip("是否已经：先被点击过（HouseClick.isClicked == true），然后白块从当前视图中消失")]
    public bool hitAndGone = false;

    [Tooltip("对应的奇怪房子（或其它可点击对象）的 HouseClick 脚本引用")]
    public HouseClick linkedClick;

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

        if (maskTexture != null)
        {
            _readTex = new Texture2D(1, 1, TextureFormat.RGB24, false);
        }
    }

    void Update()
    {
        if (mainCamera == null || maskCamera == null || maskTexture == null || _readTex == null)
            return;

        _frameCount++;
        if (_frameCount % framesInterval != 0)
            return;

        // 1. 计算房子在主相机下的屏幕归一化坐标
        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        if (vp.z <= 0f)
        {
            // 在相机背后，肯定点不到
            _col.enabled = false;
            isVisible = false;
            return;
        }

        // 超出屏幕范围，也不用点
        if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
        {
            _col.enabled = false;
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
        _col.enabled = visible;

        // 5. 检测“刚刚离开”的那一帧：
        //    条件：上一帧可见，这一帧不可见，并且对应的 HouseClick 已经被点击过
        if (_lastVisible && !visible && linkedClick != null && linkedClick.isClicked)
        {
            hitAndGone = true;
        }

        _lastVisible = visible;

        // 如需调试，可打开这一句：
        // Debug.Log($"[StenciCube] vp={vp}, pixel=({px},{py}), color={c}, visible={visible}, hitAndGone={hitAndGone}");
    }

    bool ColorsClose(Color a, Color b, float tol)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tol * tol;
    }
}
