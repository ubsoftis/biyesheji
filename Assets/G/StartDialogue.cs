using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NodeCanvas.DialogueTrees;


public class StartDialogue : MonoBehaviour
{
    DialogueTreeController dialogueTree;
    
    // Start is called before the first frame update
    void Start()
    {
        dialogueTree = GetComponent<DialogueTreeController>();
    }

    public void Talk()
    {
        dialogueTree.StartDialogue();

    }
    // Update is called once per frame

}