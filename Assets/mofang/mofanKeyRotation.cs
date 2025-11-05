using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class mofanKeyRotation : MonoBehaviour
{
    public GameObject Coller_up;
    public GameObject Coller_down;
    public GameObject Coller_left;
    public GameObject Coller_right;
    public GameObject Coller_front;
    public GameObject Coller_back;
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
    public int dir_F = 1;
    public int dir_B = -1;



    public enum Axis { X, Y, Z }
    // 每个面的旋转轴配置（默认：R/L=X, U/D=Y, F/B=Z）
    public Axis axis_R = Axis.X;
    public Axis axis_L = Axis.X;
    public Axis axis_U = Axis.Y;
    public Axis axis_D = Axis.Y;
    public Axis axis_F = Axis.Z;
    public Axis axis_B = Axis.Z;

    public enum AxisSpace { Local, World }
    // 指定每个面的轴所处坐标系（你提到的：左右朝世界坐标YZ面转 => 用世界X轴）
    public AxisSpace axisSpace_R = AxisSpace.World;
    public AxisSpace axisSpace_L = AxisSpace.World;
    public AxisSpace axisSpace_U = AxisSpace.Local;
    public AxisSpace axisSpace_D = AxisSpace.Local;
    public AxisSpace axisSpace_F = AxisSpace.Local;
    public AxisSpace axisSpace_B = AxisSpace.Local;

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
        AutoFitOneFace(Coller_front);
        AutoFitOneFace(Coller_back);
        Debug.Log("[AutoFit] 六个面的 BoxCollider 已自动匹配。");
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
        Dic_ObjMap.Add("F", Coller_front);
        Dic_ObjMap.Add("B", Coller_back);
        List_Str.Add("R");
        List_Str.Add("R''");
        List_Str.Add("L");
        List_Str.Add("L''");
        List_Str.Add("F");
        List_Str.Add("F''");
        List_Str.Add("D");
        List_Str.Add("D''");
        List_Str.Add("U");
        List_Str.Add("U''");
        List_Str.Add("B");
        List_Str.Add("B''");

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

    void Update ()
    {
        if (Input.anyKeyDown)
        {
            KeyDown();
        }
    }

    void KeyDown()
    {
        string key =KeyCheck();
        if(key!=null)
        {
            RotateObject(key);
        }
    }

    string KeyCheck()
    {
        if(isComplete == false)// 如果前一次旋转还没完成，则退出
        {
            return null;
        }
        var keyMap = new Dictionary<KeyCode, string> //键盘按键映射
        {
            {KeyCode.W, "U"},
            {KeyCode.S, "D"},
            {KeyCode.A, "L"},
            {KeyCode.D, "R"},
            {KeyCode.Q, "F"},
            {KeyCode.E, "B"}
        };

        string result = null;
        foreach (var pair in keyMap)//空格反转
        {
            if (Input.GetKey(KeyCode.Space) && Input.GetKeyDown(pair.Key))
            {
                result = pair.Value + "\'";
                break;
            }
            if (Input.GetKeyDown(pair.Key))
            {
                result = pair.Value;
                break;
            }
        }
        return result;
    }

    public void ButtonClick(string buttonName)
    {
        isComplete=false;
        RotateObject(buttonName);
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
            // 将父物体移动到所选层的几何中心，确保旋转围绕中心进行
            BoxColler.transform.position = centerWorld;

            // 为避免撕裂：如果方块被“Pivot_”父物体包裹，则以该父物体为单位分组
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
        // 记录进入本次操作前的枢轴位姿，以便失败时恢复
        boxPrevPos = BoxColler.transform.position;
        boxPrevRot = BoxColler.transform.rotation;
        Check_Coller();
        if (lastGroupedCount != 9)
        {
            Debug.LogWarning($"[Rotate] 面 {name} 分组数量为 {lastGroupedCount}，应为 9。已跳过此次旋转，请调整该面 BoxCollider 覆盖整层。");
            // 还原：把临时分组的子物体放回稳定根节点，并恢复枢轴位置与旋转
            Transform restoreParent = CubeRoot != null ? CubeRoot : null;
            for (int i = BoxColler.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = BoxColler.transform.GetChild(i);
                child.SetParent(restoreParent, true);
            }
            BoxColler.transform.position = boxPrevPos;
            BoxColler.transform.rotation = boxPrevRot;
            return "";
        }
        StartCoroutine(SmoothRotate(str));
        return "";
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
        else if (str.StartsWith("F") || str.StartsWith("B"))
        {
            bool isF = str.StartsWith("F");
            axis = GetAxisVector(
                BoxColler.transform,
                isF ? axis_F : axis_B,
                isF ? axisSpace_F : axisSpace_B
            );
            int faceDir = str.StartsWith("F") ? dir_F : dir_B;
            targetAngle *= faceDir;
        }
        Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, axis) * startRotation;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            BoxColler.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / duration);
            yield return null;
        }
        BoxColler.transform.rotation = targetRotation;

        // 旋转完成：把子物体还原到稳定根节点，并重置临时枢轴的旋转，避免累积误差与错位
        Transform restoreParent = CubeRoot != null ? CubeRoot : null;
        for (int i = BoxColler.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = BoxColler.transform.GetChild(i);
            child.SetParent(restoreParent, true);
        }
        // 恢复枢轴到旋转前的位姿，避免碰撞体留在层中心
        BoxColler.transform.position = boxPrevPos;
        BoxColler.transform.rotation = boxPrevRot;

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