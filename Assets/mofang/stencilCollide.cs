using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class stencilCollide : MonoBehaviour
{
    [Tooltip("被射线检测到时使用的 Layer（Stencils 透出时切回该层）")]
    public string activeLayer = "Raycastable";

    [Tooltip("隐藏时改成的 Layer（可选：留空则禁用 Collider）")]
    public string hiddenLayer = "";

    [Tooltip("如果 true，隐藏时直接关闭 Collider 而不是换层")]
    public bool disableColliderWhenHidden = false;

    Collider2D cachedCollider;
    int defaultLayer;
    int activeLayerIndex;
    int hiddenLayerIndex;

    void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
        defaultLayer = gameObject.layer; // 记录初始 Layer，方便缺省时回退

        activeLayerIndex = LayerMask.NameToLayer(
            string.IsNullOrEmpty(activeLayer) ? LayerMask.LayerToName(defaultLayer) : activeLayer);

        hiddenLayerIndex = string.IsNullOrEmpty(hiddenLayer)
            ? defaultLayer
            : LayerMask.NameToLayer(hiddenLayer);

        if (activeLayerIndex < 0)
        {
            Debug.LogError($"[stencilCollide] Active Layer '{activeLayer}' 未创建，请先在 Project Settings > Tags and Layers 中添加。");
            activeLayerIndex = defaultLayer;
        }

        if (!string.IsNullOrEmpty(hiddenLayer) && hiddenLayerIndex < 0)
        {
            Debug.LogError($"[stencilCollide] Hidden Layer '{hiddenLayer}' 未创建，将 fallback 到默认层。");
            hiddenLayerIndex = defaultLayer;
        }
    }

    /// <summary>
    /// 外部调用：Stencil 可见时传 true，遮挡时传 false
    /// </summary>
    public void SetStencilVisible(bool isVisible)
    {
        if (disableColliderWhenHidden)
        {
            // 隐藏时直接禁用 Collider，显示时恢复
            cachedCollider.enabled = isVisible;
            if (isVisible)
                gameObject.layer = activeLayerIndex;
        }
        else
        {
            // 始终保持 Collider 启用，仅通过 Layer 控制射线是否命中
            cachedCollider.enabled = true;
            gameObject.layer = isVisible ? activeLayerIndex : hiddenLayerIndex;
        }
    }
}
