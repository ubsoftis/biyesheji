using System.Text;
using UnityEngine;

public class ViewportSamplePicker : MonoBehaviour
{
    [Header("取点相机（用主相机即可）")]
    public Camera targetCamera;

    [Header("操作")]
    [Tooltip("按下该键切换取点开关")]
    public KeyCode toggleKey = KeyCode.P;

    [Tooltip("按下该键清空已记录点")]
    public KeyCode clearKey = KeyCode.C;

    [Tooltip("取点开启时，鼠标左键点击会记录点并输出")]
    public bool pickingEnabled = true;

    [Header("输出格式")]
    [Tooltip("输出时小数保留位数")]
    [Range(0, 6)]
    public int decimals = 3;

    readonly System.Collections.Generic.List<Vector2> _points = new System.Collections.Generic.List<Vector2>();

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            pickingEnabled = !pickingEnabled;
            Debug.Log($"[ViewportSamplePicker] pickingEnabled={pickingEnabled}", this);
        }

        if (Input.GetKeyDown(clearKey))
        {
            _points.Clear();
            Debug.Log("[ViewportSamplePicker] 已清空记录点。", this);
        }

        if (!pickingEnabled)
            return;

        if (targetCamera == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 vp = targetCamera.ScreenToViewportPoint(Input.mousePosition);
            Vector2 p = new Vector2(Mathf.Clamp01(vp.x), Mathf.Clamp01(vp.y));
            _points.Add(p);

            string vec2 = FormatVector2(p);
            Debug.Log($"[ViewportSamplePicker] 点{_points.Count}: {vec2}", this);

            string all = BuildAllPointsText();
            Debug.Log(all, this);

#if UNITY_EDITOR
            GUIUtility.systemCopyBuffer = vec2;
#endif
        }
    }

    string FormatVector2(Vector2 p)
    {
        string fmt = "F" + decimals;
        return $"new Vector2({p.x.ToString(fmt)}f, {p.y.ToString(fmt)}f)";
    }

    string BuildAllPointsText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[ViewportSamplePicker] 已记录点（可直接粘到 sampleVp 字段）:");
        for (int i = 0; i < _points.Count; i++)
        {
            sb.Append("  ");
            sb.Append(i + 1);
            sb.Append(": ");
            sb.AppendLine(FormatVector2(_points[i]));
        }
        return sb.ToString();
    }
}

