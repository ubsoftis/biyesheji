using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 「成功」回调用：关闭（禁用）两个 MonoBehaviour + 关闭 1～3 个 UI 根物体（<see cref="GameObject.SetActive"/> false）。
/// 与 <see cref="SuccessDisableOneScript"/>（只拆一个组件）二选一或组合挂在不同事件上均可。
/// </summary>
public class SuccessDestroyScriptAndUi : MonoBehaviour
{
    [Tooltip("要禁用的脚本 1。若从 Hierarchy 拖「整颗物体」到槽里，Unity 只会绑到该物体上第一个脚本；请改拖 Inspector 里具体脚本那一行的组件引用，或留空并用下方 scriptHost + 序号。")]
    public MonoBehaviour targetScript;

    [Tooltip("要禁用的脚本 2。同上；或留空并用 scriptHost + scriptIndexForSecond。")]
    public MonoBehaviour targetScript2;

    [Tooltip("当某个脚本槽不方便拖时：指定挂有多个脚本的物体，再用下面两个序号取该物体上的 MonoBehaviour（不含本组件 SuccessDestroyScriptAndUi）。")]
    public GameObject scriptHost;

    [Tooltip("scriptHost 上「第几个」MonoBehaviour（从 0 起），仅在 targetScript 为空时使用。")]
    public int scriptIndexForFirst = 0;

    [Tooltip("scriptHost 上「第几个」MonoBehaviour，仅在 targetScript2 为空时使用。")]
    public int scriptIndexForSecond = 1;

    [Tooltip("要关闭的 UI 根物体 1（SetActive false，例如倒计时文字父节点）。")]
    public GameObject uiRootToDestroy;

    [Tooltip("要关闭的 UI 根物体 2（例如底图 Image 父节点）。可不填。")]
    public GameObject uiRootToDestroy2;

    [Tooltip("要关闭的 UI 根物体 3。可不填。")]
    public GameObject uiRootToDestroy3;

    /// <summary>绑到 UnityEvent（无参数）时用此方法。</summary>
    public void DestroyScriptAndUi()
    {
        var a = ResolveScript(0);
        var b = ResolveScript(1);
        if (a != null)
        {
            a.enabled = false;
        }

        if (b != null)
        {
            b.enabled = false;
        }

        if (uiRootToDestroy != null)
        {
            uiRootToDestroy.SetActive(false);
        }

        if (uiRootToDestroy2 != null)
        {
            uiRootToDestroy2.SetActive(false);
        }

        if (uiRootToDestroy3 != null)
        {
            uiRootToDestroy3.SetActive(false);
        }
    }

    private MonoBehaviour ResolveScript(int slot)
    {
        if (slot == 0)
        {
            if (targetScript != null)
            {
                return targetScript;
            }
        }
        else if (targetScript2 != null)
        {
            return targetScript2;
        }

        if (scriptHost == null)
        {
            return null;
        }

        var list = GetMonoBehavioursExcludingSelf(scriptHost);
        var index = slot == 0 ? scriptIndexForFirst : scriptIndexForSecond;
        if (index < 0 || index >= list.Count)
        {
            return null;
        }

        return list[index];
    }

    private List<MonoBehaviour> GetMonoBehavioursExcludingSelf(GameObject host)
    {
        var list = new List<MonoBehaviour>();
        foreach (var mb in host.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb == this)
            {
                continue;
            }

            list.Add(mb);
        }

        return list;
    }
}
