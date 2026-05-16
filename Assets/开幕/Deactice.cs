using NodeCanvas.DialogueTrees.UI.Examples;
using UnityEngine;

/// <summary>
/// 启用时关闭对话 UI（NodeCanvas 的 DialogueUGUI）。不要用 PixelCrushers 命名空间——本工程无该包。
/// </summary>
public class Deactice : MonoBehaviour
{
    [Tooltip("不拖则运行时自动查找场景中所有 DialogueUGUI（含未激活）。")]
    [SerializeField] private DialogueUGUI dialogueUGUI;

    private void OnEnable()
    {
        if (dialogueUGUI != null)
        {
            dialogueUGUI.gameObject.SetActive(false);
            return;
        }

        // 与 Countdown30s 一致：包含 inactive，避免 UI 默认隐藏时查不到
        DialogueUGUI[] all = Object.FindObjectsOfType<DialogueUGUI>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
                all[i].gameObject.SetActive(false);
        }
    }
}
