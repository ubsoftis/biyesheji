using UnityEngine;

/// <summary>
/// 「成功」回调用：只 Destroy <b>一个</b>目标——拖入的 <see cref="MonoBehaviour"/> 组件。
/// 若还要删掉整块 UI，请用 <see cref="SuccessDestroyScriptAndUi"/>。
/// 用 Destroy 而不是 enabled=false，避免停更后逻辑/UI 仍挂在场上。
/// </summary>
public class SuccessDisableOneScript : MonoBehaviour
{
    [Tooltip("要移除的脚本（拖组件引用）。会 Destroy 该组件，不会删掉整个 GameObject。")]
    public MonoBehaviour targetScript;

    /// <summary>绑到 UnityEvent（无参数）时仍选此方法名即可。</summary>
    public void DisableTarget()
    {
        if (targetScript == null) return;
        Destroy(targetScript);
        targetScript = null;
    }
}
