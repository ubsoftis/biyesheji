using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class stencilCollide : MonoBehaviour
{

    [Tooltip("被射线检测到时使用的 Layer")]
    public string activeLayer = "Raycastable";

    [Tooltip("隐藏时改成的 Layer（可选：留空则禁用 Collider）")]
    public string hiddenLayer = "";

    [Tooltip("如果 true，隐藏时直接关闭 Collider 而不是换层")]
    public bool disableColliderWhenHidden = false;

    Collider cachedCollider;
    int defaultLayer;
    int activeLayerIndex;
    int hiddenLayerIndex;

    void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        defaultLayer = gameObject.layer;

        activeLayerIndex = LayerMask.NameToLayer(
            string.IsNullOrEmpty(activeLayer) ? LayerMask.LayerToName(defaultLayer) : activeLayer);

        hiddenLayerIndex = string.IsNullOrEmpty(hiddenLayer)
            ? defaultLayer
            : LayerMask.NameToLayer(hiddenLayer);
    }

    /// <summary>
    /// 外部调用：Stencil 可见时传 true，遮挡时传 false
    /// </summary>
    public void SetStencilVisible(bool isVisible)
    {
        if (disableColliderWhenHidden)
        {
            cachedCollider.enabled = isVisible;
            if (isVisible)
                gameObject.layer = activeLayerIndex;
        }
        else
        {
            cachedCollider.enabled = true;
            gameObject.layer = isVisible ? activeLayerIndex : hiddenLayerIndex;
        }
    }
}
