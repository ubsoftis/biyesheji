using UnityEngine;

/// <summary>
/// 示例射线脚本：只检测指定 Layer（例如与 stencilCollide 相同的 Active Layer）
/// </summary>
public class StencilRaycaster2D : MonoBehaviour
{
    [Tooltip("屏幕坐标转世界时使用的摄像机；留空则取 Camera.main")]
    public Camera rayCamera;

    [Tooltip("可点击层（必须与 stencilCollide 的 activeLayer 一致）")]
    public string raycastLayer = "Raycastable";

    [Tooltip("射线最长检测距离")]
    public float rayDistance = 100f;

    int layerMask;

    void Awake()
    {
        rayCamera = rayCamera != null ? rayCamera : Camera.main;
        layerMask = LayerMask.GetMask(raycastLayer);

        if (layerMask == 0)
        {
            Debug.LogWarning($"[StencilRaycaster2D] LayerMask '{raycastLayer}' 不存在或未勾选，射线将无法命中目标。");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CastRay(Input.mousePosition);
        }
    }

    public void CastRay(Vector2 screenPosition)
    {
        if (rayCamera == null)
        {
            Debug.LogError("[StencilRaycaster2D] 未设置 Camera，无法投射射线。");
            return;
        }

        if (rayCamera.orthographic)
        {
            // 正交相机：需要指定 z 坐标（使用相机的 nearClipPlane 或 0）
            Vector3 worldPoint = rayCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, rayCamera.nearClipPlane));
            Vector2 origin = worldPoint;
            
            // 正交相机：直接投射一个"点"拾取（OverlapPoint）
            Collider2D hit = Physics2D.OverlapPoint(origin, layerMask);
            if (hit != null)
            {
                HandleHit(hit);
            }
        }
        else
        {
            // 透视相机：从摄像机发射射线
            Ray ray = rayCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, rayDistance, layerMask);

            if (hit.collider != null)
            {
                HandleHit(hit.collider);
            }
        }
    }

    void HandleHit(Collider2D collider)
    {
        Debug.Log($"[StencilRaycaster2D] 点击到 {collider.name}");
        // 在这里触发你自己的逻辑，比如调用某个接口或发事件
        var clickable = collider.GetComponent<IStencilClickable>();
        if (clickable != null)
        {
            clickable.OnStencilClick();
        }
    }
}

public interface IStencilClickable
{
    void OnStencilClick();
}

