using UnityEngine;

/// <summary>
/// 被 Stencil 射线点击脚本（2D/3D）识别的可点击对象接口。
/// 挂在任意需要通过 Stencil 点击的物体上，实现 OnStencilClick() 即可。
/// </summary>
public interface IStencilClickable
{
    void OnStencilClick();
}

/// <summary>
/// 挂在小方块（或 Pivot_）上，用于接收 2D/3D 射线点击事件。
/// 若同时实现了 IStencilClickable，会优先调用 OnStencilClick。
/// </summary>
public abstract class StencilCubeClickHandler : MonoBehaviour
{
    public abstract void OnCubeFaceClicked(GameObject cube, Vector3 hitPoint, Vector3 faceNormal);
}

