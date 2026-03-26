using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StenciCube1 : MonoBehaviour
{
    [Header("相机与蒙版贴图")]
    public Camera mainCamera;          // 真正玩游戏看的相机（你现在的 Camera）
    public Camera maskCamera;          // 专门渲染蒙版的相机
    public RenderTexture maskTexture;  // maskCamera 的 TargetTexture，例如 64x64

    [Header("固定采样点（3 个检测目标）")]
    [Tooltip("Viewport 坐标(0-1)。把 3 个检测目标在屏幕上的位置填在这里")]
    public Vector2 sampleVp1 = new Vector2(0.4f, 0.9f);
    public Vector2 sampleVp2 = new Vector2(0.5f, 0.9f);
    public Vector2 sampleVp3 = new Vector2(0.6f, 0.9f);

    [Header("判定颜色（面2 可见时的颜色）")]
    public Color targetColor = Color.red;   // 面2 用的纯色
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;    // 允许一点点误差

    [Header("检测频率")]
    public int framesInterval = 1;          // 每几帧检测一次，1=每帧

    [Header("触发：3 个目标同时出现")]
    [Tooltip("当前这一帧是否满足：3 个检测目标同时命中目标颜色")]
    public bool allTargetsVisible = false;

    [Header("自动控制一个物体的显隐")]
    [Tooltip("当三个目标同时出现时将其 SetActive(true)，否则 SetActive(false)。如果不需要自动控制就留空。")]
    public GameObject objectToToggle;

    [Tooltip("当 allTargetsVisible 从 false->true 时触发（只触发一次，直到再次变为 false）")]
    public UnityEvent onAllTargetsVisible;

    bool _allTargetsVisibleTriggered = false;

    Texture2D _readTex;
    int _frameCount;

    void Awake()
    {
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

    void OnEnable()
    {
        Debug.Log($"[StenciCube1] 已启用并开始运行：{gameObject.name}", this);
    }

    void Update()
    {
        if (maskCamera == null || maskTexture == null || _readTex == null)
            return;

        _frameCount++;
        if (_frameCount % framesInterval != 0)
            return;

        // 固定采样 3 个点：三个检测目标同时出现才算通过
        bool v1 = SampleIsTargetColor(sampleVp1);
        bool v2 = SampleIsTargetColor(sampleVp2);
        bool v3 = SampleIsTargetColor(sampleVp3);
        bool visible = v1 && v2 && v3;
        allTargetsVisible = visible;

        if (objectToToggle != null)
            objectToToggle.SetActive(allTargetsVisible);

        if (allTargetsVisible && !_allTargetsVisibleTriggered)
        {
            _allTargetsVisibleTriggered = true;
            onAllTargetsVisible?.Invoke();
        }
        else if (!allTargetsVisible)
        {
            _allTargetsVisibleTriggered = false;
        }
    }

    bool ColorsClose(Color a, Color b, float tol)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tol * tol;
    }

    bool SampleIsTargetColor(Vector2 vp01)
    {
        if (vp01.x < 0f || vp01.x > 1f || vp01.y < 0f || vp01.y > 1f)
            return false;

        int px = Mathf.Clamp(Mathf.RoundToInt(vp01.x * maskTexture.width), 0, maskTexture.width - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(vp01.y * maskTexture.height), 0, maskTexture.height - 1);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = maskTexture;
        _readTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        _readTex.Apply();
        RenderTexture.active = currentRT;

        Color c = _readTex.GetPixel(0, 0);
        return ColorsClose(c, targetColor, colorTolerance);
    }
}
