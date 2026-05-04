using UnityEngine;

/// <summary>
/// 供其它逻辑在「成功」时通过 <see cref="UnityEngine.Events.UnityEvent"/> 调用：
/// 将已拖入的一个 <see cref="MonoBehaviour"/> 设为 <c>enabled = false</c>（多为别的物体上的脚本）。
/// </summary>
public class SuccessDisableOneScript : MonoBehaviour
{
    [Tooltip("要关掉的脚本（任意物体上拖入组件引用）。")]
    public MonoBehaviour targetScript;

    /// <summary>绑到 UnityEvent（无参数）时选此方法。</summary>
    public void DisableTarget()
    {
        if (targetScript != null)
            targetScript.enabled = false;
    }
}
