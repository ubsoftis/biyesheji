using UnityEngine;
using UnityEngine.Events;

public class StenciCube2 : MonoBehaviour
{
    [Header("相机与蒙版贴图")]
    public Camera mainCamera;
    public Camera maskCamera;
    public RenderTexture maskTexture;

    [Header("采样点（2 个检测目标，可自行填写位置）")]
    [Tooltip("Viewport 坐标(0-1)。你可以在 Inspector 自己改采样位置。")]
    public Vector2 sampleVp1 = new Vector2(0.45f, 0.9f);

    [Tooltip("Viewport 坐标(0-1)。你可以在 Inspector 自己改采样位置。")]
    public Vector2 sampleVp2 = new Vector2(0.55f, 0.9f);

    [Header("判定颜色")]
    public Color targetColor = Color.red;

    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;

    [Header("检测频率")]
    public int framesInterval = 1;

    [Header("触发：2 个目标同时出现")]
    [Tooltip("当前这一帧是否满足：2 个检测点同时命中目标颜色")]
    public bool allTargetsVisible = false;

    [Header("白块离开状态（与 StencilCubePlant 对齐）")]
    [Tooltip("当前这一帧，两个采样点是否同时命中目标颜色（白块还在）")]
    public bool isVisible = false;

    [Tooltip("是否已经：白块从当前视图中消失（上一帧可见，这一帧不可见）")]
    public bool hitAndGone = false;

    [Header("点击门控方式")]
    [Tooltip("为 true 时，用 isVisible 自动开关 Collider2D；为 false 时不再改 Collider。")]
    public bool controlColliderByVisibility = true;

    [Header("自动控制一个物体的显隐")]
    [Tooltip("当两个目标同时出现时将其 SetActive(true)，否则 SetActive(false)。如果不需要自动控制就留空。")]
    public GameObject objectToToggle;

    [Tooltip("当 allTargetsVisible 从 false->true 时触发（只触发一次，直到再次变为 false）")]
    public UnityEvent onAllTargetsVisible;

    bool _allTargetsVisibleTriggered = false;
    bool _lastVisible = false;
    Collider2D _col;
    Texture2D _readTex;
    int _frameCount;

    void Awake()
    {
        _col = GetComponent<Collider2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (maskTexture == null && maskCamera != null)
            maskTexture = maskCamera.targetTexture;

        if (maskCamera == null)
            Debug.LogError("[StenciCube2] 未指定 maskCamera。");

        if (_readTex == null)
            _readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
    }

    void Update()
    {
        if (maskCamera == null || maskTexture == null || _readTex == null)
            return;

        _frameCount++;
        if (_frameCount % framesInterval != 0)
            return;

        bool v1 = SampleIsTargetColor(sampleVp1);
        bool v2 = SampleIsTargetColor(sampleVp2);
        isVisible = v1 && v2;
        allTargetsVisible = isVisible;

        if (controlColliderByVisibility && _col != null)
            _col.enabled = isVisible;

        if (_lastVisible && !isVisible)
            hitAndGone = true;
        _lastVisible = isVisible;

        if (objectToToggle != null)
            objectToToggle.SetActive(isVisible);

        if (isVisible && !_allTargetsVisibleTriggered)
        {
            _allTargetsVisibleTriggered = true;
            onAllTargetsVisible?.Invoke();
        }
        else if (!isVisible)
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

