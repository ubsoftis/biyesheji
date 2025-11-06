using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class mofanKeyRotationRequire : MonoBehaviour
{
    public GameObject Coller_up;
    public GameObject Coller_down;
    public GameObject Coller_left;
    public GameObject Coller_right;
    public GameObject Coller_middleRow;   // 水平方向中间层（介于上/下之间）
    public GameObject Coller_middleCol;   // 竖直方向中间列（介于左/右之间）

    // 选中高亮（外描边）
    public Color highlightColor = new Color(1f, 0.8f, 0.2f, 1f);
    public float highlightWidth = 0.02f;
    private List<LineRenderer> selectionOutlines = new List<LineRenderer>();
    
    // 描边固定尺寸（横排：长宽高）
    public Vector3 rowOutlineSize = new Vector3(1f, 1f, 0.1f);
    // 描边固定尺寸（竖排：长宽高）
    public Vector3 columnOutlineSize = new Vector3(1f, 1f, 0.1f);
    public Transform CubeRoot; // 旋转后归还子物体的稳定根节点

    public float duration = 0.1f; // 旋转持续时间
    private GameObject BoxColler;

    private Dictionary<string, GameObject> Dic_ObjMap = new Dictionary<string, GameObject>();
    public List<string> List_History = new List<string>();
    public List<string> List_Str = new List<string>();
    private bool isComplete = true;
    private bool isAuto = false;
    private int lastGroupedCount = 0;
    private Vector3 boxPrevPos;
    private Quaternion boxPrevRot;
    private Vector3 middleLayerOriginalPos; // 中间层的原始位置，用于防止累积偏移
    
    // 判断是否为中间层碰撞体
    private bool IsMiddleLayer(GameObject collider) => collider == Coller_middleRow || collider == Coller_middleCol;
    
    // 恢复子物体到根节点
    private void RestoreChildrenToRoot()
    {
        Transform restoreParent = CubeRoot != null ? CubeRoot : null;
        for (int i = BoxColler.transform.childCount - 1; i >= 0; i--)
        {
            BoxColler.transform.GetChild(i).SetParent(restoreParent, true);
        }
    }
    
    // 记录碰撞体位姿
    private void RecordColliderState()
    {
        boxPrevPos = BoxColler.transform.position;
        boxPrevRot = BoxColler.transform.rotation;
        if (IsMiddleLayer(BoxColler))
        {
            middleLayerOriginalPos = boxPrevPos;
        }
    }
    
    // 恢复碰撞体位姿
    private void RestoreColliderState()
    {
        BoxColler.transform.position = IsMiddleLayer(BoxColler) ? middleLayerOriginalPos : boxPrevPos;
        BoxColler.transform.rotation = boxPrevRot;
    }

    // 初始状态快照（用于一键重置）
    private List<Transform> cubeTransforms = new List<Transform>();
    private Dictionary<Transform, Transform> initParent = new Dictionary<Transform, Transform>();
    private Dictionary<Transform, Vector3> initLocalPos = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> initLocalRot = new Dictionary<Transform, Quaternion>();

    // 每个面的方向系数，便于纠正顺时针/逆时针差异（1 或 -1）
    public int dir_R = 1;
    public int dir_L = -1;
    public int dir_U = 1;
    public int dir_D = -1;
    



    public enum Axis { X, Y, Z }
    // 每个面的旋转轴配置（默认：R/L=X, U/D=Y, F/B=Z）
    public Axis axis_R = Axis.X;
    public Axis axis_L = Axis.X;
    public Axis axis_U = Axis.Y;
    public Axis axis_D = Axis.Y;
    

    public enum AxisSpace { Local, World }
    // 指定每个面的轴所处坐标系（你提到的：左右朝世界坐标YZ面转 => 用世界X轴）
    public AxisSpace axisSpace_R = AxisSpace.World;
    public AxisSpace axisSpace_L = AxisSpace.World;
    public AxisSpace axisSpace_U = AxisSpace.Local;
    public AxisSpace axisSpace_D = AxisSpace.Local;
    

    Vector3 GetAxisVector(Transform t, Axis a)
    {
        switch (a)
        {
            case Axis.X: return t.right;
            case Axis.Y: return t.up;
            default: return t.forward;
        }
    }

    Vector3 GetAxisVector(Transform t, Axis a, AxisSpace space)
    {
        if (space == AxisSpace.Local)
        {
            return GetAxisVector(t, a);
        }
        // World axes
        switch (a)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            default: return Vector3.forward;
        }
    }



    [ContextMenu("Auto Fit Face Colliders")]
    void AutoFitFaceColliders()
    {
        if (CubeRoot == null)
        {
            Debug.LogError("CubeRoot 未设置，无法自动匹配面碰撞体。");
            return;
        }
        AutoFitOneFace(Coller_up);
        AutoFitOneFace(Coller_down);
        AutoFitOneFace(Coller_left);
        AutoFitOneFace(Coller_right);
        
        Debug.Log("[AutoFit] 六个面的 BoxCollider 已自动匹配。");
    }

    [ContextMenu("对齐中间层碰撞体")]
    void AlignMiddleColliders()
    {
        if (CubeRoot == null)
        {
            Debug.LogError("CubeRoot 未设置，无法对齐中间层碰撞体。");
            return;
        }
        
        if (Coller_middleRow != null)
        {
            ColliderAligner.AlignCollider(Coller_middleRow, CubeRoot);
        }
        else
        {
            Debug.LogWarning("Coller_middleRow 未设置");
        }
        
        if (Coller_middleCol != null)
        {
            ColliderAligner.AlignCollider(Coller_middleCol, CubeRoot);
        }
        else
        {
            Debug.LogWarning("Coller_middleCol 未设置");
        }
        
        Debug.Log("[AlignMiddleColliders] 中间层碰撞体已对齐。");
    }

    void AutoFitOneFace(GameObject face)
    {
        if (face == null) return;
        var box = face.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = face.AddComponent<BoxCollider>();
        }

        // 收集所有小方块的渲染器
        var rends = CubeRoot.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;

        // 计算该面局部空间中每个块中心与厚度估计
        var faceTf = face.transform;
        float maxZ = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        List<Vector3> locals = new List<Vector3>(rends.Length);
        List<float> zThicknessSamples = new List<float>(rends.Length);
        Vector3 fwdAbs = new Vector3(Mathf.Abs(faceTf.forward.x), Mathf.Abs(faceTf.forward.y), Mathf.Abs(faceTf.forward.z));
        foreach (var r in rends)
        {
            if (r.gameObject.layer != LayerMask.NameToLayer("Cube")) continue;
            Vector3 cLocal = faceTf.InverseTransformPoint(r.bounds.center);
            locals.Add(cLocal);
            maxZ = Mathf.Max(maxZ, cLocal.z);
            minZ = Mathf.Min(minZ, cLocal.z);
            // 估计该块沿面法线方向的厚度（投影渲染包围盒尺寸到法线）
            float thick = Vector3.Dot(r.bounds.size, fwdAbs);
            if (thick > 0f) zThicknessSamples.Add(thick);
        }
        if (locals.Count == 0) return;

        // 决定选前层还是后层：看面位置相对整体中心的方向
        float dirSign = Mathf.Sign(Vector3.Dot(faceTf.forward, faceTf.position - CubeRoot.position));
        float targetZ = dirSign >= 0 ? maxZ : minZ;

        // 厚度估计：取样本的中位数并加一点裕量
        zThicknessSamples.Sort();
        float thickness = zThicknessSamples.Count > 0 ? zThicknessSamples[zThicknessSamples.Count / 2] : Mathf.Abs(maxZ - minZ) / 3f;
        thickness *= 1.1f;

        float eps = thickness * 0.6f;
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        int count = 0;
        foreach (var p in locals)
        {
            if (Mathf.Abs(p.z - targetZ) <= eps)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
                count++;
            }
        }
        if (count == 0)
        {
            Debug.LogWarning($"[AutoFit] 面 {face.name} 未匹配到层，请检查朝向/位置。");
            return;
        }

        // 设置 BoxCollider（在面局部坐标系下）
        Vector3 centerLocal = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, targetZ);
        Vector3 sizeLocal = new Vector3((maxX - minX) * 1.02f, (maxY - minY) * 1.02f, thickness);

        // 确保缩放为正，避免物理异常
        Vector3 s = faceTf.localScale;
        faceTf.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

        box.center = centerLocal;
        box.size = sizeLocal;
    }

    void Start()
    {
        Dic_ObjMap.Add("U", Coller_up);
        Dic_ObjMap.Add("D", Coller_down);
        Dic_ObjMap.Add("L", Coller_left);
        Dic_ObjMap.Add("R", Coller_right);
        
        List_Str.Add("R");
        List_Str.Add("R''");
        List_Str.Add("L");
        List_Str.Add("L''");
        
        List_Str.Add("D");
        List_Str.Add("D''");
        List_Str.Add("U");
        List_Str.Add("U''");
        

        // 记录初始姿态，便于“重置”按钮一键恢复
        CaptureInitialState();
    }

    void CaptureInitialState()
    {
        if (CubeRoot == null) return;
        cubeTransforms.Clear();
        initParent.Clear();
        initLocalPos.Clear();
        initLocalRot.Clear();
        var trs = CubeRoot.GetComponentsInChildren<Transform>(true);
        foreach (var t in trs)
        {
            // 只记录魔方块相关节点：Layer 为 Cube 的物体以及它们的父节点（如 Pivot_）
            bool isCubeLayer = t.gameObject.layer == LayerMask.NameToLayer("Cube");
            bool isPivot = t.name.StartsWith("Pivot_");
            if (!isCubeLayer && !isPivot) continue;
            cubeTransforms.Add(t);
            initParent[t] = t.parent;
            initLocalPos[t] = t.localPosition;
            initLocalRot[t] = t.localRotation;
        }
    }

    public void ResetCube()
    {
        // 归还到初始父子关系并还原局部位姿
        foreach (var t in cubeTransforms)
        {
            if (t == null) continue;
            Transform p;
            if (initParent.TryGetValue(t, out p))
            {
                t.SetParent(p, false);
            }
            Vector3 lp;
            if (initLocalPos.TryGetValue(t, out lp)) t.localPosition = lp;
            Quaternion lr;
            if (initLocalRot.TryGetValue(t, out lr)) t.localRotation = lr;
        }
        List_History.Clear();
        isAuto = false;
    }

    // 模式：1=横排模式，2=竖排模式
    private enum ControlMode { Row, Column }
    private ControlMode currentMode = ControlMode.Row;
    // 选择索引：横排模式选行(0=上 1=下)，竖排模式选列(0=左 1=右)
    private int selectionIndex = 0;

    void Update ()
    {
        if (Input.anyKeyDown) { KeyDown(); }
    }

    void KeyDown()
    {
        if (isComplete == false) return;

        // 模式切换：1=横排，2=竖排
        if (Input.GetKeyDown(KeyCode.Alpha1)) { currentMode = ControlMode.Row; UpdateSelectionHighlight(); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { currentMode = ControlMode.Column; UpdateSelectionHighlight(); }

        if (currentMode == ControlMode.Row)
        {
            // 选择 0=上 1=中 2=下
            if (Input.GetKeyDown(KeyCode.UpArrow)) { selectionIndex = Mathf.Max(0, selectionIndex - 1); UpdateSelectionHighlight(); }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { selectionIndex = Mathf.Min(2, selectionIndex + 1); UpdateSelectionHighlight(); }

            // 旋转：← 逆时针，→ 顺时针
            if (Input.GetKeyDown(KeyCode.LeftArrow)) RotateRow(selectionIndex, false);
            if (Input.GetKeyDown(KeyCode.RightArrow)) RotateRow(selectionIndex, true);
        }
        else
        {
            // 选择 0=左 1=中 2=右
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { selectionIndex = Mathf.Max(0, selectionIndex - 1); UpdateSelectionHighlight(); }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { selectionIndex = Mathf.Min(2, selectionIndex + 1); UpdateSelectionHighlight(); }

            // 旋转：↑ 顺时针（向上），↓ 逆时针（向下）
            if (Input.GetKeyDown(KeyCode.UpArrow)) RotateColumn(selectionIndex, true);
            if (Input.GetKeyDown(KeyCode.DownArrow)) RotateColumn(selectionIndex, false);
        }
    }

    void RotateRow(int rowIndex, bool clockwise)
    {
        GameObject pivot = rowIndex == 0 ? Coller_up : rowIndex == 1 ? Coller_middleRow : Coller_down;
        if (pivot == null) { Debug.LogWarning("[Row] 对应行缺少 Coller：请在 Inspector 赋值（上/中/下）。"); return; }
        RotateWithPivot(clockwise ? "U" : "U'", pivot);
        UpdateSelectionHighlight();
    }

    void RotateColumn(int colIndex, bool clockwise)
    {
        GameObject pivot = colIndex == 0 ? Coller_left : colIndex == 1 ? Coller_middleCol : Coller_right;
        if (pivot == null) { Debug.LogWarning("[Column] 对应列缺少 Coller：请在 Inspector 赋值（左/中/右）。"); return; }
        RotateWithPivot(clockwise ? "L" : "L'", pivot);
        UpdateSelectionHighlight();
    }


    public void ButtonClick(string buttonName)
    {
        isComplete=false;
        
    }
    void Check_Coller()
    {
        var box = BoxColler.GetComponent<BoxCollider>();
        if (box == null)
        {
            Debug.LogError("BoxColler 缺少 BoxCollider");
            return;
        }

        int layerMask = LayerMask.GetMask("Cube");

        // 使用有向包围盒，保证任意朝向下选中正确的一层
        Vector3 centerWorld = box.transform.TransformPoint(box.center);
        // 负缩放会让物理系统给出奇怪结果，这里取绝对缩放
        Vector3 lossy = box.transform.lossyScale;
        Vector3 scaleAbs = new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, scaleAbs);
        Quaternion orientation = box.transform.rotation;

        Collider[] hitColliders = Physics.OverlapBox(centerWorld, halfExtents, orientation, layerMask);
        if (lossy.x < 0f || lossy.y < 0f || lossy.z < 0f)
        {
            Debug.LogWarning($"[SelectLayer] 面 {BoxColler.name}: 检测到负缩放 {lossy}. 请优先把该面物体的Scale改为正数(建议1,1,1)，仅用BoxCollider.size来控制范围。");
        }
        if (hitColliders.Length == 0)
        {
            Debug.LogWarning($"[SelectLayer] 面 {BoxColler.name}: 选中0个方块。请检查该面的 BoxCollider 尺寸/位置 以及小方块是否在 Layer 'Cube'。");
            lastGroupedCount = 0;
        }
        if (hitColliders.Length > 0)
        {
            // 非中间层移动到几何中心，中间层保持位置不变
            if (!IsMiddleLayer(BoxColler))
            {
                BoxColler.transform.position = centerWorld;
            }

            // 为避免撕裂：如果方块被"Pivot_"父物体包裹，则以该父物体为单位分组
            var uniqueTransforms = new HashSet<Transform>();
            int grouped = 0;
            foreach (Collider collider in hitColliders)
            {
                Transform t = collider.transform;
                if (t.parent != null && t.parent.name.StartsWith("Pivot_"))
                {
                    t = t.parent;
                }
                if (t == BoxColler.transform) continue;
                if (!uniqueTransforms.Add(t)) continue;
                t.SetParent(BoxColler.transform, true);
                grouped++;
            }
            Debug.Log($"[SelectLayer] 面 {BoxColler.name}: 命中 {hitColliders.Length}，分组 {grouped} 个变换。");
            lastGroupedCount = grouped;
        }
    }
    
    public string RotateObject(string str)
    {
        string name = str.Substring(0, 1)[0].ToString();
        BoxColler = Dic_ObjMap[name];
        RecordColliderState();
        Check_Coller();
        if (lastGroupedCount != 9)
        {
            Debug.LogWarning($"[Rotate] 面 {name} 分组数量为 {lastGroupedCount}，应为 9。已跳过此次旋转，请调整该面 BoxCollider 覆盖整层。");
            RestoreChildrenToRoot();
            RestoreColliderState();
            return "";
        }
        StartCoroutine(SmoothRotate(str));
        return "";
    }

    // 直接用指定的枢轴进行旋转（供行/列控制调用）
    void RotateWithPivot(string cmd, GameObject pivot)
    {
        BoxColler = pivot;
        RecordColliderState();
        Check_Coller();
        if (lastGroupedCount != 9)
        {
            RestoreChildrenToRoot();
            RestoreColliderState();
            return;
        }
        StartCoroutine(SmoothRotate(cmd));
    }

    void UpdateSelectionHighlight()
    {
        GameObject pivot = null;
        if (currentMode == ControlMode.Row)
            pivot = selectionIndex == 0 ? Coller_up : selectionIndex == 1 ? Coller_middleRow : Coller_down;
        else
            pivot = selectionIndex == 0 ? Coller_left : selectionIndex == 1 ? Coller_middleCol : Coller_right;
        if (pivot == null) { ClearOutline(); return; }

        var box = pivot.GetComponent<BoxCollider>();
        if (box == null) { ClearOutline(); return; }

        // 根据模式选择固定尺寸
        Vector3 outlineSize = currentMode == ControlMode.Row ? rowOutlineSize : columnOutlineSize;
        
        // 使用 BoxCollider 的中心位置
        Vector3 centerWorld = box.transform.TransformPoint(box.center);
        
        // 使用 pivot 的局部轴方向
        Vector3 right = pivot.transform.right.normalized;
        Vector3 up = pivot.transform.up.normalized;
        Vector3 normal = pivot.transform.forward.normalized;
        
        // 使用固定尺寸计算描边
        float halfR = outlineSize.x * 0.5f;
        float halfU = outlineSize.y * 0.5f;
        float halfN = outlineSize.z * 0.5f;
        
        // 描边中心就是 BoxCollider 的中心
        Vector3 boxCenter = centerWorld;
        Vector3 r = right * halfR;
        Vector3 u = up * halfU;
        Vector3 n = normal * halfN;
        Vector3 c000 = boxCenter - r - u - n;
        Vector3 c100 = boxCenter + r - u - n;
        Vector3 c110 = boxCenter + r + u - n;
        Vector3 c010 = boxCenter - r + u - n;
        Vector3 c001 = boxCenter - r - u + n;
        Vector3 c101 = boxCenter + r - u + n;
        Vector3 c111 = boxCenter + r + u + n;
        Vector3 c011 = boxCenter - r + u + n;

        EnsureOutlines(6);
        // 前矩形(-n)
        SetLine(selectionOutlines[0], new[]{ c000, c100, c110, c010, c000 });
        // 后矩形(+n)
        SetLine(selectionOutlines[1], new[]{ c001, c101, c111, c011, c001 });
        // 四条连接边
        SetLine(selectionOutlines[2], new[]{ c000, c001 });
        SetLine(selectionOutlines[3], new[]{ c100, c101 });
        SetLine(selectionOutlines[4], new[]{ c110, c111 });
        SetLine(selectionOutlines[5], new[]{ c010, c011 });
    }

    void ClearOutline()
    {
        if (selectionOutlines.Count == 0) return;
        foreach (var lr in selectionOutlines)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        selectionOutlines.Clear();
    }

    void EnsureOutlines(int count)
    {
        // 创建/复用指定数量的 LineRenderer
        while (selectionOutlines.Count < count)
        {
            var go = new GameObject("SelectionOutlinePart");
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            selectionOutlines.Add(lr);
        }
        for (int i = 0; i < selectionOutlines.Count; i++)
        {
            var lr = selectionOutlines[i];
            lr.startWidth = highlightWidth;
            lr.endWidth = highlightWidth;
            lr.startColor = highlightColor;
            lr.endColor = highlightColor;
        }
    }

    void SetLine(LineRenderer lr, Vector3[] points)
    {
        lr.positionCount = points.Length;
        lr.SetPositions(points);
    }

    IEnumerator SmoothRotate (string str) 
    { 
        isComplete = false;
        float targetAngle = 90f;
        Quaternion startRotation = BoxColler.transform.rotation;
        targetAngle = str.Contains("'") ? -targetAngle : targetAngle;

        if (isAuto == true)
        {
            targetAngle = -targetAngle;
        }
        else
        {
            List_History.Add(str);
        }
        
        bool isMiddleLayer = IsMiddleLayer(BoxColler);
        
        // 使用面的自身轴作为旋转轴，并应用每面可配置的方向系数与轴选择
        Vector3 axis = Vector3.zero;
        if (str.StartsWith("R") || str.StartsWith("L"))
        {
            bool isR = str.StartsWith("R");
            axis = GetAxisVector(
                BoxColler.transform,
                isR ? axis_R : axis_L,
                isR ? axisSpace_R : axisSpace_L
            );
            int faceDir = str.StartsWith("R") ? dir_R : dir_L;
            targetAngle *= faceDir;
        }
        else if (str.StartsWith("U") || str.StartsWith("D"))
        {
            bool isU = str.StartsWith("U");
            axis = GetAxisVector(
                BoxColler.transform,
                isU ? axis_U : axis_D,
                isU ? axisSpace_U : axisSpace_D
            );
            int faceDir = str.StartsWith("U") ? dir_U : dir_D;
            targetAngle *= faceDir;
        }
        
        // 统一顺/逆时针的语义：
        // 约定"未带 ' 的字母"为观察该面的正向(面法线方向)时的顺时针。
        // 四元数正角度遵循右手定则 => 面法线方向观察时，顺时针应取负角；
        // 若该面的法线与旋转轴同向，则乘以 -1；反向则乘以 +1。
        if (axis != Vector3.zero)
        {
            float dot = Vector3.Dot(axis.normalized, BoxColler.transform.forward.normalized);
            float clockwiseFix = (dot >= 0f) ? -1f : 1f;
            targetAngle *= clockwiseFix;
        }
        
        Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, axis) * startRotation;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            BoxColler.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / duration);
            // 中间层碰撞体在旋转过程中必须保持位置固定，防止位移
            // 使用记录的原始位置，而不是 boxPrevPos，避免累积偏移
            if (isMiddleLayer)
            {
                BoxColler.transform.position = middleLayerOriginalPos;
            }
            yield return null;
        }
        BoxColler.transform.rotation = targetRotation;

        // 恢复碰撞体位置（防止 SetParent 操作导致位置变化）
        if (isMiddleLayer)
        {
            BoxColler.transform.position = middleLayerOriginalPos;
        }
        
        // 还原子物体到根节点
        RestoreChildrenToRoot();
        
        // 恢复碰撞体位姿
        RestoreColliderState();

        // 更新描边位置
        UpdateSelectionHighlight();

        isComplete = true;
    }

    //复原魔方
    public void AutoRotate()//自动复原
{
        isAuto = true;
        StartCoroutine(AutoRotate_IE());
    }

    IEnumerator AutoRotate_IE()//自动复原过程
    {
        duration = 0.1f;
        for (int i = List_History.Count; i> 0; i--)
        {
            string str = List_History [i - 1];
            RotateObject (str);
            yield return new WaitForSeconds (0.2f);
        }
        isAuto = false;
        List_History.Clear();
    }

    public void Disruption()//打乱魔方
    {
        isAuto = false;
        StartCoroutine(Disruption_IE());
    }

    IEnumerator Disruption_IE()//打乱过程
    {
        duration = 0.1f;
        for (int n = 0; n < 10; n++)
        {
            int num = Random.Range (0, List_Str.Count);
            string str = List_Str [num];
            RotateObject (str);
            yield return new WaitForSeconds (0.2f);
        }
    }
}