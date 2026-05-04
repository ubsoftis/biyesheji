using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

/// <summary>
/// 单采样点版 Stencil 交互（参照 <see cref="StencilActivateTwoOnClick"/>）：
/// 通过三层 RT + 颜色门控后，在 <see cref="StencilCubeRaycaster2D"/> 命中本物体 Collider 时触发；
/// 要求背包当前选中格子的物品为指定的 <see cref="ItemSO"/>（与 <see cref="SceneInteractItemPlacer"/> 一样读 <see cref="InventoryManager.GetSelectedItem"/>，但走 Stencil 点击而非 Physics 放置）。
/// 成功后可选消耗选中物品，并修改「场景中另一物体」上的 <see cref="SpriteRenderer.sprite"/>（不是本交互物体上的图）；
/// 可选将本脚本所在 <see cref="GameObject"/> SetActive(false)。
/// <para>与 <see cref="ScenePlacementTarget"/> 无强绑定；若同一物体还要给 <see cref="SceneInteractItemPlacer"/> 用，请自行配置 Tag / Collider。</para>
/// </summary>
public class StencilSingleSampleItemSpriteReplace : MonoBehaviour, IStencilClickable
{
    public enum CubePickMode
    {
        None = 0,
        OnStencilRaycastHit = 1,
        GlobalMouseWhenGateVisible = 2,
    }

    [Header("状态")]
    [Tooltip("成功后为 true，不再响应")]
    public bool completed;

    [Tooltip("只读：单采样点是否通过三层 RT+颜色判定")]
    public bool singleSampleVisible;

    [Tooltip("只读：与 useThreeRTForVisibility 一致的门控结果")]
    public bool clickGateVisible;

    [Header("模板测试（三层 RT，单采样点）")]
    [Tooltip("为 true：点击前用三层 RT 做颜色门控；为 false 时门控恒为通过")]
    public bool useThreeRTForVisibility = true;

    [Tooltip("不填则运行时 FindObjectOfType")]
    public CubeLayerRTPicker rtPicker;

    [Tooltip("与 StencilActivateTwoOnClick.leftmostDebugPreviewIsFrontLayer 相同语义")]
    public bool leftmostDebugPreviewIsFrontLayer = true;

    [Tooltip("唯一采样点（Viewport 0~1）")]
    public Vector2 sampleVp = new Vector2(0.5f, 0.5f);

    public Color targetColor = Color.red;
    [Range(0f, 1f)]
    public float colorTolerance = 0.05f;

    public float blackRgbThreshold = 0.02f;
    public float blackAlphaThreshold = 0.01f;
    public bool invertIsVisible;

    [Header("可选：单点 Pick 魔方块")]
    [Tooltip("None：仅 Stencil 命中本物体即可（在通过门控与背包校验后）。OnStencilRaycastHit：命中后还要求采样点处 Pick 到魔方块 Collider。")]
    public CubePickMode cubePickMode = CubePickMode.None;

    [Tooltip("Viewport→Screen；留空则用 rtPicker.viewCamera / Camera.main")]
    public Camera pickCamera;

    [Tooltip("Pick 命中 Collider 的 Layer 须在此掩码内")]
    public LayerMask cubePiecePickLayers;

    [Tooltip("为 true：点击 UI 时不做 Global 模式下的 Pick")]
    public bool ignoreUIBlockingForDirectPick = true;

    [Tooltip("仅 ignoreUIBlocking 为 true 时生效；与 StencilCubeRaycaster2D.uiBlockingOnlyOnUiLayer 一致")]
    public bool directPickUiBlockingOnlyOnUiLayer;

    [Header("背包（与 SceneInteractItemPlacer 相同：当前选中格子）")]
    [Tooltip("必须与格子里的 ItemSO 引用一致")]
    public ItemSO requiredItem;

    [Tooltip("成功后是否从当前选中格消耗")]
    public bool consumeSelectedItemOnSuccess = true;

    [Min(1)]
    public int consumeAmount = 1;

    [Header("成功后：换「别的物体」上的图")]
    [Tooltip("拖场景里任意其他物体上的 SpriteRenderer（不要拖本脚本所在物体的图，除非你刻意要换自己）。")]
    [FormerlySerializedAs("spriteRendererToChange")]
    public SpriteRenderer spriteRendererOnOtherObject;

    [Tooltip("成功后将上述 SpriteRenderer 的 sprite 设为该图。")]
    public Sprite spriteAfterSuccess;

    [Header("成功后：隐藏自身")]
    [Tooltip("为 true：在换图与 onSuccess 之后，将本脚本所在的 GameObject SetActive(false)。")]
    public bool deactivateSelfOnSuccess = true;

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

        if (cubePiecePickLayers.value == 0)
            cubePiecePickLayers = LayerMask.GetMask("CubeBack", "CubeMid", "CubeFront");
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

        if (cubePickMode != CubePickMode.GlobalMouseWhenGateVisible)
            return;
        if (!Input.GetMouseButtonDown(0))
            return;

        bool uiBlock = ignoreUIBlockingForDirectPick && IsDirectPickBlockedByUi(Input.mousePosition);
        if (uiBlock)
            return;
        if (!useThreeRTForVisibility || !clickGateVisible)
            return;
        if (completed)
            return;

        if (!TryApplyStencilClickCore(requirePick: true))
            return;

        ApplySuccessEffects();
    }

    public void OnStencilClick()
    {
        if (cubePickMode == CubePickMode.GlobalMouseWhenGateVisible)
        {
            if (debugLog)
                Debug.Log("[StencilSingleSampleItemSpriteReplace] Global 模式在 Update 处理，OnStencilClick 忽略。");
            return;
        }

        bool requirePick = cubePickMode == CubePickMode.OnStencilRaycastHit;
        if (!TryApplyStencilClickCore(requirePick))
            return;

        ApplySuccessEffects();
    }

    /// <summary>门控、背包、可选 Pick；不修改 completed。</summary>
    bool TryApplyStencilClickCore(bool requirePick)
    {
        if (completed)
        {
            if (debugLog)
                Debug.Log($"[StencilSingleSampleItemSpriteReplace] 已完成，忽略 {name}");
            return false;
        }

        if (useThreeRTForVisibility)
        {
            ComputeVisibilityStateByThreeRT();
            if (!clickGateVisible)
            {
                if (debugLog)
                    Debug.Log($"[StencilSingleSampleItemSpriteReplace] 颜色门控未通过 singleSampleVisible={singleSampleVisible}");
                return false;
            }
        }

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            if (debugLog)
                Debug.LogWarning("[StencilSingleSampleItemSpriteReplace] 无 InventoryManager.Instance");
            return false;
        }

        if (requiredItem == null)
        {
            if (debugLog)
                Debug.LogWarning("[StencilSingleSampleItemSpriteReplace] requiredItem 未设置");
            return false;
        }

        ItemSO selected = inv.GetSelectedItem();
        if (selected != requiredItem)
        {
            if (debugLog)
                Debug.Log($"[StencilSingleSampleItemSpriteReplace] 选中物品不符：需要 {requiredItem.itemName}，当前 {(selected != null ? selected.itemName : "无")}");
            return false;
        }

        if (requirePick && !TryPickCubePieceAtSingleSample())
        {
            if (debugLog)
                Debug.Log("[StencilSingleSampleItemSpriteReplace] 单点 Pick 魔方块未通过");
            return false;
        }

        if (consumeSelectedItemOnSuccess)
        {
            if (!inv.TryConsumeSelectedItem(consumeAmount))
            {
                if (debugLog)
                    Debug.LogWarning("[StencilSingleSampleItemSpriteReplace] 消耗失败（数量或选中格）");
                return false;
            }

            inv.RefreshAllSlots();
        }

        return true;
    }

    void ApplySuccessEffects()
    {
        if (completed)
            return;

        completed = true;

        if (spriteRendererOnOtherObject != null && spriteAfterSuccess != null)
            spriteRendererOnOtherObject.sprite = spriteAfterSuccess;
        else if (debugLog && spriteRendererOnOtherObject == null)
            Debug.LogWarning("[StencilSingleSampleItemSpriteReplace] spriteRendererOnOtherObject 未设置，跳过换图");
        else if (debugLog && spriteAfterSuccess == null)
            Debug.LogWarning("[StencilSingleSampleItemSpriteReplace] spriteAfterSuccess 未设置，跳过换图");

        onSuccess?.Invoke();

        if (debugLog)
            Debug.Log($"[StencilSingleSampleItemSpriteReplace] 成功 {name}");

        if (deactivateSelfOnSuccess && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    bool TryPickCubePieceAtSingleSample()
    {
        if (rtPicker == null)
            rtPicker = FindObjectOfType<CubeLayerRTPicker>();
        if (rtPicker == null)
            return false;

        Camera cam = pickCamera != null ? pickCamera : (rtPicker.viewCamera != null ? rtPicker.viewCamera : Camera.main);
        if (cam == null)
            return false;

        Vector3 sp = cam.ViewportToScreenPoint(new Vector3(sampleVp.x, sampleVp.y, Mathf.Max(0.01f, sampleCrossDepth)));
        if (sp.z < 0f)
            return false;

        rtPicker.EnsureRTRenderedForSampling();
        if (!rtPicker.TryPickFrontmostScreen(sp, out RaycastHit hit))
            return false;
        if (hit.collider == null)
            return false;

        int layer = hit.collider.gameObject.layer;
        if (cubePiecePickLayers.value != 0 && ((1 << layer) & cubePiecePickLayers.value) == 0)
            return false;

        return true;
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

    bool IsDirectPickBlockedByUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        if (!directPickUiBlockingOnlyOnUiLayer)
            return EventSystem.current.IsPointerOverGameObject();

        var ped = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>(8);
        EventSystem.current.RaycastAll(ped, results);
        if (results.Count == 0)
            return false;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
            return EventSystem.current.IsPointerOverGameObject();

        for (int i = 0; i < results.Count; i++)
        {
            GameObject go = results[i].gameObject;
            if (go != null && go.layer == uiLayer)
                return true;
        }

        return false;
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
