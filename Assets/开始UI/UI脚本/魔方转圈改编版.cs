using UnityEngine;

public class DragRotateSnap : MonoBehaviour
{
    [Header("轴向开关")]
    [Tooltip("允许绕摄像机右轴旋转（垂直拖动）")]
    public bool allowX = true;
    [Tooltip("允许绕世界Y轴旋转（水平拖动）")]
    public bool allowY = true;

    [Header("拖拽旋转参数")]
    public float rotationSpeed = 200f;

    [Header("90° 顺滑磁吸 设置")]
    public bool use90DegreeSnap = true;
    public float snapSpeed = 8f;

    [Header("自定义初始基准角度")]
    [Tooltip("磁吸以这个角度为基准，每次90°一格")]
    public float baseYAngle = 0f;

    private bool dragging = false;
    private Vector3 lastMousePosition;
    private bool isSnapping = false;
    private float targetYAngle;

    void Start()
    {
        // 游戏运行自动把物体初始角度设为你配置的基准角
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x,
            baseYAngle,
            transform.eulerAngles.z
        );
    }

    void Update()
    {
        // 右键按下
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
            {
                dragging = true;
                isSnapping = false;
                lastMousePosition = Input.mousePosition;
            }
        }

        // 拖拽旋转
        if (dragging && Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            float rotX = delta.y * rotationSpeed * Time.deltaTime;
            float rotY = -delta.x * rotationSpeed * Time.deltaTime;

            if (Camera.main != null)
            {
                if (allowX)
                    transform.Rotate(Camera.main.transform.right, rotX, Space.World);
                if (allowY)
                    transform.Rotate(Vector3.up, rotY, Space.World);
            }
            else
            {
                if (allowX)
                    transform.Rotate(Vector3.right, rotX, Space.World);
                if (allowY)
                    transform.Rotate(Vector3.up, rotY, Space.World);
            }

            lastMousePosition = Input.mousePosition;
        }

        // 鼠标松开 → 按基准角90°间隔吸附
        if (Input.GetMouseButtonUp(1))
        {
            dragging = false;

            if (use90DegreeSnap && allowY)
            {
                float currentY = transform.eulerAngles.y;
                // 以 baseYAngle 为基准，就近吸附到 90° 整数倍档位
                float diff = Mathf.DeltaAngle(baseYAngle, currentY);
                float snapStep = Mathf.Round(diff / 90f) * 90f;
                targetYAngle = baseYAngle + snapStep;

                isSnapping = true;
            }
        }

        // 平滑插值吸附
        if (isSnapping)
        {
            float currentY = transform.eulerAngles.y;
            float newY = Mathf.LerpAngle(currentY, targetYAngle, snapSpeed * Time.deltaTime);

            transform.eulerAngles = new Vector3(transform.eulerAngles.x, newY, transform.eulerAngles.z);

            if (Mathf.Abs(Mathf.DeltaAngle(currentY, targetYAngle)) < 0.1f)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, targetYAngle, transform.eulerAngles.z);
                isSnapping = false;
            }
        }
    }
}