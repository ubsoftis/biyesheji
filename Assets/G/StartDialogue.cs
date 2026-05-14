using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NodeCanvas.DialogueTrees;


public class StartDialogue : MonoBehaviour
{
    [SerializeField] DialogueTreeController dialogueTree;

    public void Talk()
    {
        if (dialogueTree == null)
        {
            dialogueTree = GetComponent<DialogueTreeController>()
                ?? GetComponentInParent<DialogueTreeController>()
                ?? GetComponentInChildren<DialogueTreeController>(true);
        }
        if (dialogueTree != null)
            dialogueTree.StartDialogue();
        else
            Debug.LogWarning("[StartDialogue] 未找到 DialogueTreeController，请在 Inspector 中拖入带 DialogueTreeController 的物体上的组件。");
    }
    // Update is called once per frame

}