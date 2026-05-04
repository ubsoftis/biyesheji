using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在 CubeRoot 下创建 前/中/后 空物体（仅作层级组织），按当前相机视角深度把魔方块分到三个 Unity Layer，
/// 用三台相机分别渲染到三个 RenderTexture，点击时用「从后到前」迭代 RT / 射线，最终命中最靠前（离相机最近）的块。
///
/// 使用前请在 Edit > Project Settings > Tags and Layers 中新增三个 Layer，名称需与下面字段一致（默认 CubeBack / CubeMid / CubeFront）。
/// </summary>
[DisallowMultipleComponent]
public class CubeLayerRTPicker : MonoBehaviour
{
    [Header("与 mofanKeyRotation 的 CubeRoot 相同")]
    public Transform cubeRoot;

    [Header("用于划分前中后的相机（不填则用 Camera.main）")]
    public Camera viewCamera;

    [Header("魔方块原始 Layer 名（用于首次收集；分层后会改到下面三个 Layer）")]
    public string sourceCubeLayerName = "Cube";

    [Header("三个分层 Layer 名（须与 Project 中 Layer 一致）")]
    public string layerNameBack = "CubeBack";
    public string layerNameMid = "CubeMid";
    public string layerNameFront = "CubeFront";

    [Header("RT 尺寸")]
    public int rtWidth = 512;
    public int rtHeight = 512;

    [Header("分层用空物体名（挂在 cubeRoot 下）")]
    public string organizerName = "LayerOrganizer";
    public string nodeBackName = "Layer_Back";
    public string nodeMidName = "Layer_Middle";
    public string nodeFrontName = "Layer_Front";

    [Header("调试：在屏幕左下角显示三张 RT")]
    public bool showDebugRTPreviews = false;

    [Header("调试：左键点击时渲染 RT 并在 Console 打印最前命中")]
    public bool debugPickOnClick = false;

    [Header("RT 里参与 CubeMask（仅影响 RT，不永久改层）")]
    [Tooltip("CubeMask 对象所在的 Layer。用于在渲染 RT 之前，临时按前/中/后分组把它们切到 CubeBack/Mid/Front。")]
    public LayerMask cubeMaskLayerMask;

    [Tooltip("是否让 CubeMask 参与前/中/后 RT 渲染逻辑。")]
    public bool cubeMaskAffectsRT = true;

    Transform _organizer;
    Transform _nodeBack;
    Transform _nodeMid;
    Transform _nodeFront;

    readonly List<Transform> _cubieRoots = new List<Transform>();
    // 首次收集缓存：只对“属于原始 Cube 层的小块根节点”做分层，
    // 避免误把像 CubeMask 这类非 Cube 对象也一起改层。
    readonly List<Transform> _cubePieceRootsCache = new List<Transform>();
    // 每个小块根节点下，真正允许被改层的节点（仅初始化时 sourceCubeLayerName 层）。
    readonly Dictionary<Transform, List<GameObject>> _layerMutableNodesByRoot = new Dictionary<Transform, List<GameObject>>();

    // CubeMask 节点缓存：用于 RT 渲染前临时切层，再立刻恢复。
    readonly Dictionary<Transform, List<GameObject>> _cubeMaskNodesByRoot = new Dictionary<Transform, List<GameObject>>();
    readonly Dictionary<GameObject, int> _cubeMaskOriginalLayerByGO = new Dictionary<GameObject, int>();

    RenderTexture _rtBack;
    RenderTexture _rtMid;
    RenderTexture _rtFront;

    public RenderTexture RtBack => _rtBack;
    public RenderTexture RtMid => _rtMid;
    public RenderTexture RtFront => _rtFront;

    int _lastRTRenderFrame = -1;

    Camera _camBack;
    Camera _camMid;
    Camera _camFront;

    GameObject _rtCameraRoot;

    static int NameToLayerSafe(string name)
    {
        int i = LayerMask.NameToLayer(name);
        if (i < 0)
            Debug.LogWarning($"[CubeLayerRTPicker] 未找到 Layer「{name}」，请在 Tags and Layers 里添加。");
        return i;
    }

    void Awake()
    {
        if (cubeRoot == null)
            cubeRoot = transform;
        if (viewCamera == null)
            viewCamera = Camera.main;
    }

    void Start()
    {
        EnsureOrganizerNodes();
        CollectCubieRoots();
        _cubePieceRootsCache.Clear();
        _cubePieceRootsCache.AddRange(_cubieRoots);
        BuildMutableNodeCache();
        BuildCubeMaskNodeCache();
        RefreshLayerAssignment();
        EnsureRTResources();
    }

    void OnDestroy()
    {
        SafeDestroyRT(ref _rtBack);
        SafeDestroyRT(ref _rtMid);
        SafeDestroyRT(ref _rtFront);
        if (_rtCameraRoot != null)
            Destroy(_rtCameraRoot);
    }

    static void SafeDestroyRT(ref RenderTexture rt)
    {
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
            rt = null;
        }
    }

    /// <summary>旋转或重置后由 mofanKeyRotation 调用，更新前中后 Layer。</summary>
    public void RefreshLayerAssignment()
    {
        if (cubeRoot == null || viewCamera == null)
            return;

        if (_cubePieceRootsCache.Count == 0)
        {
            // 容错：如果缓存为空，退化为重新收集（但正常情况下 Start 已建立缓存）
            CollectCubieRoots();
            _cubePieceRootsCache.Clear();
            _cubePieceRootsCache.AddRange(_cubieRoots);
        }
        if (_cubePieceRootsCache.Count == 0)
            return;

        int layerBack = NameToLayerSafe(layerNameBack);
        int layerMid = NameToLayerSafe(layerNameMid);
        int layerFront = NameToLayerSafe(layerNameFront);
        if (layerBack < 0 || layerMid < 0 || layerFront < 0)
            return;

        var cam = viewCamera;
        var camTr = cam.transform;
        var scored = new List<(Transform tr, float key)>(_cubePieceRootsCache.Count);
        foreach (var tr in _cubePieceRootsCache)
        {
            if (tr == null) continue;
            Vector3 lp = camTr.InverseTransformPoint(tr.position);
            // 相机局部：Z 越小通常越靠近相机前方（Unity 相机朝 -Z 看）
            scored.Add((tr, lp.z));
        }
        scored.Sort((a, b) => a.key.CompareTo(b.key));

        int n = scored.Count;
        int iBackEnd = Mathf.Max(1, n / 3);
        int iMidEnd = Mathf.Max(iBackEnd + 1, (n * 2) / 3);

        for (int i = 0; i < n; i++)
        {
            int layer;
            if (i < iBackEnd)
                layer = layerBack;
            else if (i < iMidEnd)
                layer = layerMid;
            else
                layer = layerFront;

            SetLayerForRootCached(scored[i].tr, layer);
        }
    }

    void SetLayerForRootCached(Transform root, int layer)
    {
        if (root == null) return;

        List<GameObject> nodes;
        if (!_layerMutableNodesByRoot.TryGetValue(root, out nodes) || nodes == null || nodes.Count == 0)
        {
            // 兜底：若缓存不存在，至少只改根节点自身，不递归污染其他子节点。
            root.gameObject.layer = layer;
            return;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            var go = nodes[i];
            if (go != null)
                go.layer = layer;
        }
    }

    void BuildMutableNodeCache()
    {
        _layerMutableNodesByRoot.Clear();
        int src = LayerMask.NameToLayer(sourceCubeLayerName);
        bool hasSrcLayer = src >= 0;
        int maskValue = cubeMaskLayerMask.value;

        for (int i = 0; i < _cubePieceRootsCache.Count; i++)
        {
            var root = _cubePieceRootsCache[i];
            if (root == null) continue;

            // 优先：只缓存“原始 Cube 层”的渲染器
            // 兜底：如果场景之前被改过 layer，导致找不到 src，则退化缓存“非 CubeMask 的渲染器”
            var srcNodes = new List<GameObject>();
            var fallbackNodes = new List<GameObject>();

            var rends = root.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < rends.Length; j++)
            {
                var go = rends[j] != null ? rends[j].gameObject : null;
                if (go == null) continue;

                if (maskValue != 0 && (((1 << go.layer) & maskValue) != 0))
                {
                    // CubeMask 参与 RT 时也不应该被永久改层；这里直接排除在“可改层节点”之外。
                    continue;
                }

                fallbackNodes.Add(go);

                if (hasSrcLayer && go.layer == src)
                    srcNodes.Add(go);
            }

            List<GameObject> listToCache = null;
            if (srcNodes.Count > 0)
                listToCache = srcNodes;
            else if (fallbackNodes.Count > 0)
                listToCache = fallbackNodes;
            else
                listToCache = new List<GameObject> { root.gameObject }; // 极端兜底

            if (listToCache.Count > 0)
                _layerMutableNodesByRoot[root] = listToCache;
        }
    }

    void BuildCubeMaskNodeCache()
    {
        _cubeMaskNodesByRoot.Clear();
        _cubeMaskOriginalLayerByGO.Clear();

        if (!cubeMaskAffectsRT)
            return;
        if (cubeMaskLayerMask.value == 0)
            return;

        for (int i = 0; i < _cubePieceRootsCache.Count; i++)
        {
            var root = _cubePieceRootsCache[i];
            if (root == null) continue;

            var list = new List<GameObject>();
            // 用 Renderer 作为判断入口：CubeMask 通常会挂在渲染器对象上
            var rends = root.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < rends.Length; j++)
            {
                var go = rends[j].gameObject;
                if (go == null) continue;

                int goLayer = go.layer;
                if (((1 << goLayer) & cubeMaskLayerMask.value) == 0)
                    continue;

                list.Add(go);
                if (!_cubeMaskOriginalLayerByGO.ContainsKey(go))
                    _cubeMaskOriginalLayerByGO.Add(go, goLayer);
            }

            if (list.Count > 0)
                _cubeMaskNodesByRoot[root] = list;
        }
    }

    void CollectCubieRoots()
    {
        _cubieRoots.Clear();
        if (cubeRoot == null) return;
        EnsureOrganizerNodes();

        int src = LayerMask.NameToLayer(sourceCubeLayerName);
        bool hasSrcLayer = src >= 0;
        if (!hasSrcLayer)
            Debug.LogWarning($"[CubeLayerRTPicker] 未找到源层 `{sourceCubeLayerName}`，将退化为不做 Cube 层过滤。");

        // 优先：CubeRoot 下一级子物体 = 每个小块根（常见为 27 个 Pivot）
        foreach (Transform ch in cubeRoot)
        {
            if (ch.name == organizerName) continue;
            if (!hasSrcLayer)
            {
                _cubieRoots.Add(ch);
                continue;
            }

            if (IsCubePieceRoot(ch, src))
                _cubieRoots.Add(ch);
        }

        // 若只有一个总父节点包着全部块，则改用 Renderer 回退收集
        if (_cubieRoots.Count <= 1)
        {
            _cubieRoots.Clear();
            int lb = NameToLayerSafe(layerNameBack);
            int lm = NameToLayerSafe(layerNameMid);
            int lf = NameToLayerSafe(layerNameFront);

            // src layer 存在时：只收集真正属于 sourceCubeLayerName 的小块渲染器父节点；
            // src 不存在时：退化为按当前 CubeBack/Mid/Front 层回收集，保证不会直接失效。
            var seen = new HashSet<Transform>();
            var rends = cubeRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (_organizer != null && r.transform.IsChildOf(_organizer))
                    continue;
                Transform tr = r.transform;
                while (tr.parent != null && tr.parent != cubeRoot)
                    tr = tr.parent;
                if (tr == cubeRoot) continue;
                if (!seen.Add(tr)) continue;
                int l = r.gameObject.layer;
                if (hasSrcLayer)
                {
                    if (l == src) _cubieRoots.Add(tr);
                }
                else
                {
                    if (l == lb || l == lm || l == lf) _cubieRoots.Add(tr);
                }
            }
        }
    }

    bool IsCubePieceRoot(Transform root, int srcCubeLayerIndex)
    {
        if (root == null) return false;
        if (srcCubeLayerIndex < 0)
            return false;

        // pivot 本身就在 Cube 层
        if (root.gameObject.layer == srcCubeLayerIndex)
            return true;

        // pivot 未必在 Cube 层，但只要它下面有 Renderer 在 Cube 层，就算属于 cube 小块
        var rends = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            var go = rends[i].gameObject;
            if (go != null && go.layer == srcCubeLayerIndex)
                return true;
        }
        return false;
    }

    void EnsureOrganizerNodes()
    {
        if (cubeRoot == null) return;

        _organizer = cubeRoot.Find(organizerName);
        if (_organizer == null)
        {
            var go = new GameObject(organizerName);
            go.transform.SetParent(cubeRoot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            _organizer = go.transform;
        }

        _nodeBack = GetOrCreateChild(_organizer, nodeBackName);
        _nodeMid = GetOrCreateChild(_organizer, nodeMidName);
        _nodeFront = GetOrCreateChild(_organizer, nodeFrontName);
    }

    static Transform GetOrCreateChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    void EnsureRTResources()
    {
        if (viewCamera == null) return;

        SafeDestroyRT(ref _rtBack);
        SafeDestroyRT(ref _rtMid);
        SafeDestroyRT(ref _rtFront);

        _rtBack = NewRT();
        _rtMid = NewRT();
        _rtFront = NewRT();

        if (_rtCameraRoot != null)
            Destroy(_rtCameraRoot);
        _rtCameraRoot = new GameObject("RT_PreviewCameras");
        _rtCameraRoot.transform.SetParent(viewCamera.transform, false);

        _camBack = CreateLayerCamera("RT_Cam_Back", _rtBack, LayerMask.GetMask(layerNameBack));
        _camMid = CreateLayerCamera("RT_Cam_Mid", _rtMid, LayerMask.GetMask(layerNameMid));
        _camFront = CreateLayerCamera("RT_Cam_Front", _rtFront, LayerMask.GetMask(layerNameFront));

        _camBack.enabled = false;
        _camMid.enabled = false;
        _camFront.enabled = false;
    }

    RenderTexture NewRT()
    {
        var rt = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.ARGB32);
        rt.name = "CubeLayerRT";
        rt.Create();
        return rt;
    }

    Camera CreateLayerCamera(string goName, RenderTexture target, int cullingMask)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(_rtCameraRoot.transform, false);
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.cullingMask = cullingMask;
        cam.targetTexture = target;
        cam.nearClipPlane = viewCamera.nearClipPlane;
        cam.farClipPlane = viewCamera.farClipPlane;
        cam.fieldOfView = viewCamera.fieldOfView;
        cam.orthographic = viewCamera.orthographic;
        cam.orthographicSize = viewCamera.orthographicSize;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        return cam;
    }

    void LateUpdate()
    {
        SyncRTCamerasToMain();
        // OnGUI 只负责 DrawTexture，不会触发渲染；若不在此处刷新，预览会一直黑/空，
        // 除非别的脚本（如 StencilActivateTwoOnClick）在同一帧调用了 EnsureRTRenderedForSampling。
        if (showDebugRTPreviews && _camBack != null && viewCamera != null)
            RenderAllLayersToRT();
    }

    void Update()
    {
        if (!debugPickOnClick || viewCamera == null)
            return;
        if (!Input.GetMouseButtonDown(0))
            return;
        RenderAllLayersToRT();
        if (TryPickFrontmostScreen(Input.mousePosition, out RaycastHit hit))
            Debug.Log($"[CubeLayerRTPicker] 最前命中: {hit.collider.name}, dist={hit.distance:F3}");
        else
            Debug.Log("[CubeLayerRTPicker] 未命中（需 Collider 且 Layer 为 CubeBack/Mid/Front）");
    }

    void SyncRTCamerasToMain()
    {
        if (viewCamera == null) return;
        CopyCam(viewCamera, _camBack);
        CopyCam(viewCamera, _camMid);
        CopyCam(viewCamera, _camFront);
    }

    static void CopyCam(Camera from, Camera to)
    {
        if (from == null || to == null) return;
        to.transform.position = from.transform.position;
        to.transform.rotation = from.transform.rotation;
        to.nearClipPlane = from.nearClipPlane;
        to.farClipPlane = from.farClipPlane;
        to.fieldOfView = from.fieldOfView;
        to.orthographic = from.orthographic;
        to.orthographicSize = from.orthographicSize;
    }

    /// <summary>
    /// 将三层分别渲染到 RT（可用于调试贴图）。点击逻辑建议用 TryPickFrontmost。
    /// </summary>
    public void RenderAllLayersToRT()
    {
        if (_camBack == null || viewCamera == null) return;
        SyncRTCamerasToMain();

        // 有些情况下手动 Render 可能受 enabled 影响；为了保证三张 RT 都能渲染，
        // 渲染前临时开启，渲染后恢复原状态。
        bool prevBackEnabled = _camBack.enabled;
        bool prevMidEnabled = _camMid.enabled;
        bool prevFrontEnabled = _camFront.enabled;
        _camBack.enabled = true;
        _camMid.enabled = true;
        _camFront.enabled = true;

        // 让 CubeMask 按“前/中/后深度分组”参与 RT 渲染：只在渲染期间临时改层，渲染后立刻恢复。
        if (cubeMaskAffectsRT && cubeMaskLayerMask.value != 0 && _cubeMaskNodesByRoot.Count > 0)
        {
            int layerBack = NameToLayerSafe(layerNameBack);
            int layerMid = NameToLayerSafe(layerNameMid);
            int layerFront = NameToLayerSafe(layerNameFront);
            if (layerBack >= 0 && layerMid >= 0 && layerFront >= 0)
            {
                ApplyCubeMaskLayersForCurrentDepthPartition(layerBack, layerMid, layerFront);
                try
                {
                    _camBack.Render();
                    _camMid.Render();
                    _camFront.Render();
                }
                finally
                {
                    RestoreCubeMaskOriginalLayers();
                }
                // 恢复 enabled 状态
                _camBack.enabled = prevBackEnabled;
                _camMid.enabled = prevMidEnabled;
                _camFront.enabled = prevFrontEnabled;
                return;
            }
        }

        _camBack.Render();
        _camMid.Render();
        _camFront.Render();

        // 恢复 enabled 状态
        _camBack.enabled = prevBackEnabled;
        _camMid.enabled = prevMidEnabled;
        _camFront.enabled = prevFrontEnabled;
    }

    /// <summary>
    /// 给外部（比如 StenciCube）采样 RT 时用：保证同一帧只渲染一次，避免多物体重复 Render 导致卡顿。
    /// </summary>
    public void EnsureRTRenderedForSampling()
    {
        if (Time.frameCount == _lastRTRenderFrame)
            return;
        RenderAllLayersToRT();
        _lastRTRenderFrame = Time.frameCount;
    }

    /// <summary>
    /// 清除「本帧已渲染」标记，使下一次 <see cref="EnsureRTRenderedForSampling"/> 必定再次 <see cref="RenderAllLayersToRT"/>。
    /// 供帧末二次采样等与 <see cref="EnsureRTRenderedForSampling"/> 同帧但需更新 RT 的脚本使用。
    /// </summary>
    public void InvalidateRTRenderFrameCache()
    {
        _lastRTRenderFrame = -1;
    }

    void ApplyCubeMaskLayersForCurrentDepthPartition(int layerBack, int layerMid, int layerFront)
    {
        if (viewCamera == null)
            return;

        var camTr = viewCamera.transform;
        var scored = new List<(Transform tr, float key)>(_cubePieceRootsCache.Count);
        for (int i = 0; i < _cubePieceRootsCache.Count; i++)
        {
            var tr = _cubePieceRootsCache[i];
            if (tr == null) continue;
            Vector3 lp = camTr.InverseTransformPoint(tr.position);
            scored.Add((tr, lp.z));
        }

        scored.Sort((a, b) => a.key.CompareTo(b.key));

        int n = scored.Count;
        int iBackEnd = Mathf.Max(1, n / 3);
        int iMidEnd = Mathf.Max(iBackEnd + 1, (n * 2) / 3);

        for (int i = 0; i < n; i++)
        {
            Transform root = scored[i].tr;
            int layer = (i < iBackEnd) ? layerBack : (i < iMidEnd) ? layerMid : layerFront;

            if (root == null) continue;
            if (!_cubeMaskNodesByRoot.TryGetValue(root, out var nodes) || nodes == null)
                continue;

            for (int j = 0; j < nodes.Count; j++)
            {
                var go = nodes[j];
                if (go != null)
                    go.layer = layer;
            }
        }
    }

    void RestoreCubeMaskOriginalLayers()
    {
        if (_cubeMaskOriginalLayerByGO.Count == 0)
            return;

        foreach (var kv in _cubeMaskOriginalLayerByGO)
        {
            var go = kv.Key;
            if (go == null) continue;
            go.layer = kv.Value;
        }
    }

    /// <summary>
    /// 从后到前依次检测：对 Back / Mid / Front 三层做 Raycast，取距离相机最近的命中（物理上即最「前」的块）。
    /// </summary>
    public bool TryPickFrontmost(Ray ray, out RaycastHit bestHit, float maxDistance = 100f)
    {
        bestHit = default;
        int mask = LayerMask.GetMask(layerNameBack, layerNameMid, layerNameFront);
        var hits = Physics.RaycastAll(ray, maxDistance, mask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        float best = float.PositiveInfinity;
        int bestIdx = -1;
        for (int i = 0; i < hits.Length; i++)
        {
            float d = hits[i].distance;
            if (d < best)
            {
                best = d;
                bestIdx = i;
            }
        }
        bestHit = hits[bestIdx];
        return true;
    }

    public bool TryPickFrontmostScreen(Vector3 screenPosition, out RaycastHit hit)
    {
        hit = default;
        if (viewCamera == null) return false;
        Ray ray = viewCamera.ScreenPointToRay(screenPosition);
        return TryPickFrontmost(ray, out hit);
    }

    void OnGUI()
    {
        if (!showDebugRTPreviews || _rtBack == null) return;
        const int w = 120;
        const int h = 90;
        float y = Screen.height - h - 10;
        if (_rtBack != null && _rtBack.IsCreated())
            GUI.DrawTexture(new Rect(10, y, w, h), _rtBack, ScaleMode.ScaleToFit, false);
        if (_rtMid != null && _rtMid.IsCreated())
            GUI.DrawTexture(new Rect(20 + w, y, w, h), _rtMid, ScaleMode.ScaleToFit, false);
        if (_rtFront != null && _rtFront.IsCreated())
            GUI.DrawTexture(new Rect(30 + w * 2, y, w, h), _rtFront, ScaleMode.ScaleToFit, false);
    }

#if UNITY_EDITOR
    [ContextMenu("立即刷新分层")]
    void EditorRefresh()
    {
        Awake();
        Start();
        RefreshLayerAssignment();
    }
#endif
}
