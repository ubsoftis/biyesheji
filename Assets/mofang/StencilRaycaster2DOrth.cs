 using UnityEngine;

/// <summary>
/// 示例射线脚本：只检测指定 Layer（例如与 stencilCollide 相同的 Active Layer）
/// </summary>

public class StencilRaycaster2DOrth : MonoBehaviour
{
     [Tooltip("屏幕坐标转世界时使用的摄像机；留空则取 Camera.main")]
    public Camera rayCamera;

    [Tooltip("可点击层（必须与 stencilCollide 的 activeLayer 一致）")]
    public string raycastLayer = "Raycastable";

    [Tooltip("正交相机世界坐标的 Z 平面（通常与 2D 场景平面一致）")]
    public float projectionPlaneZ = 0f;

    [Tooltip("是否点击到了物体")]
    public bool isHit = false;

    [Tooltip("当点击命中时激活的 UI 物体")]
    public GameObject uiObjectToActivate;

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

        if (!rayCamera.orthographic)
        {
            Debug.LogWarning("[StencilRaycaster2DOrth] 当前脚本仅支持正交相机，请将摄像机 Projection 设为 Orthographic。");
            return;
        }

        // 正交相机：所有射线方向平行，直接将屏幕坐标映射到世界平面
        float zDistance = Mathf.Abs(projectionPlaneZ - rayCamera.transform.position.z);
        Vector3 worldPoint = rayCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        worldPoint.z = projectionPlaneZ;

        Collider2D hit = Physics2D.OverlapPoint(worldPoint, layerMask);
        if (hit != null)
        {
            HandleHit(hit);
        }
        else
        {
            // 未命中时关闭 UI，但不改变 isHit 状态
            if (uiObjectToActivate != null)
            {
                uiObjectToActivate.SetActive(false);
            }
        }
    }

    void HandleHit(Collider2D collider)
    {
        isHit = true;
        Debug.Log($"[StencilRaycaster2D] 点击到 {collider.name}");
        
        // 激活 UI 物体
        if (uiObjectToActivate != null)
        {
            uiObjectToActivate.SetActive(true);
        }
        
        // 在这里触发你自己的逻辑，比如调用某个接口或发事件
        var clickable = collider.GetComponent<IStencilClickable>();
        if (clickable != null)
        {
            clickable.OnStencilClick();
        }
    }
}

