using UnityEngine;

/// <summary>
/// 挂在需要"从下往上浮入"的 Text 或任何 RectTransform 物体上。
/// 配合 CubeAnimationController 的制作人名单使用。
/// 挂上这个组件 = 该物体在所在Segment淡入时会上浮入场，并且会"先于"其他不上浮的Text出现。
/// 没挂这个组件 = 该物体只淡入，位置不动，并且会"晚于"上浮文字出现。
///
/// 自动在挂载的物体上添加一个 CanvasGroup（如果还没有），用于独立控制本物体的透明度，
/// 不受父级 Segment 的 CanvasGroup 影响（通过 ignoreParentGroups=true）。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RiseOnFadeIn : MonoBehaviour
{
    [Tooltip("上浮距离（像素）。0 表示使用 CubeAnimationController 里的全局值")]
    public float riseDistance = 0f;

    [Tooltip("是否使用全局淡入时间（CubeAnimationController.creditsFadeInTime）。\n关闭则使用下面的 customRiseDuration 单独控制本物体的上浮时长")]
    public bool useGlobalDuration = true;

    [Tooltip("自定义上浮时长（仅当 useGlobalDuration 关闭时生效）")]
    public float customRiseDuration = 0.8f;

    void Reset()
    {
        // Inspector里第一次添加组件时，自动配置CanvasGroup
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.ignoreParentGroups = true; // 不受Segment整体CanvasGroup影响，让它能独立控制透明度
            cg.alpha = 1f;
        }
    }
}