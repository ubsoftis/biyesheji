using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using UnityEngine;

/// <summary>
/// 对话中断时立绘归位。与对话树 MoveTowards 移动的 UI 框一致，并支持各角色上的 DialoguePortraitHome。
/// </summary>
public static class DialoguePortraitReset
{
    public const string DefaultHomeObjectName = "对话角色背景框";

    /// <summary>本项目中 boundGraphObjectReferences[3] 为 MoveTowards 的 overrideAgent（立绘 UI）。</summary>
    public const int BoundGraphMoveAgentIndex = 3;

    public static void ResetForController(DialogueTreeController controller, Transform sharedHomeAnchor = null)
    {
        if (controller == null)
            return;

        Transform home = ResolveHomeAnchor(sharedHomeAnchor);
        ResetBoundGraphMoveAgents(controller, home);

        DialogueTree tree = controller.behaviour;
        if (tree != null && tree.actorParameters != null)
        {
            for (int i = 0; i < tree.actorParameters.Count; i++)
                ResetActor(tree.actorParameters[i].actor, home);
        }

        ResetPortraitHomesInScene();
        ActivateInterruptDisplay(home, controller);
    }

    public static void ResetForTree(DialogueTree tree, Transform sharedHomeAnchor = null, GraphOwner controller = null)
    {
        Transform home = ResolveHomeAnchor(sharedHomeAnchor);

        if (tree != null && tree.actorParameters != null)
        {
            for (int i = 0; i < tree.actorParameters.Count; i++)
                ResetActor(tree.actorParameters[i].actor, home);
        }

        ResetPortraitHomesInScene();
        ActivateInterruptDisplay(home, controller);
    }

    public static void ResetAllInScene(Transform sharedHomeAnchor = null)
    {
        Transform home = ResolveHomeAnchor(sharedHomeAnchor);

        DialoguePortraitHome[] homes = Object.FindObjectsOfType<DialoguePortraitHome>(true);
        for (int i = 0; i < homes.Length; i++)
        {
            if (homes[i] != null)
                homes[i].SnapToHome();
        }

        DialogueTreeController[] controllers =
            Object.FindObjectsOfType<DialogueTreeController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
                ResetBoundGraphMoveAgents(controllers[i], home);
        }
    }

    public static void ResetActor(IDialogueActor actor, Transform homeAnchor)
    {
        if (actor == null)
            return;

        DialoguePortraitHome home = (actor as Component)?.GetComponent<DialoguePortraitHome>();
        if (home != null)
        {
            home.SnapToHome();
            return;
        }

        DialogueActor dialogueActor = actor as DialogueActor;
        if (dialogueActor == null || dialogueActor.portraitAnimSource == null)
            return;

        Transform portraitTransform = dialogueActor.portraitAnimSource.transform;
        if (portraitTransform is RectTransform)
            SnapToHome(portraitTransform, homeAnchor);
    }

    public static void SnapToHome(Transform portrait, Transform homeAnchor)
    {
        if (portrait == null || homeAnchor == null)
            return;

        if (portrait is RectTransform portraitRect && homeAnchor is RectTransform homeRect)
        {
            if (portraitRect.parent == homeRect.parent)
            {
                portraitRect.anchoredPosition = homeRect.anchoredPosition;
                return;
            }
        }

        Vector3 target = homeAnchor.position;
        portrait.position = new Vector3(target.x, target.y, portrait.position.z);
    }

    public static Transform ResolveHomeAnchor(Transform sharedHomeAnchor)
    {
        if (sharedHomeAnchor != null)
            return sharedHomeAnchor;

        return FindTransformByName(DefaultHomeObjectName);
    }

    /// <summary>
    /// 中断后显示立绘：激活归位锚点（对话角色背景框）及对话树里被 MoveTowards 移动的立绘 UI。
    /// </summary>
    public static void ActivateInterruptDisplay(Transform sharedHomeAnchor, GraphOwner controller)
    {
        Transform home = ResolveHomeAnchor(sharedHomeAnchor);
        if (home != null)
            home.gameObject.SetActive(true);

        if (controller == null)
            return;

        var refs = controller.boundGraphObjectReferences;
        if (refs == null || refs.Count <= BoundGraphMoveAgentIndex)
            return;

        if (refs[BoundGraphMoveAgentIndex] is Component portrait && portrait != null)
            portrait.gameObject.SetActive(true);
    }

    private static void ResetPortraitHomesInScene()
    {
        DialoguePortraitHome[] homes = Object.FindObjectsOfType<DialoguePortraitHome>(true);
        for (int i = 0; i < homes.Length; i++)
        {
            if (homes[i] != null)
                homes[i].SnapToHome();
        }
    }

    private static void ResetBoundGraphMoveAgents(GraphOwner controller, Transform home)
    {
        if (controller == null || home == null)
            return;

        var refs = controller.boundGraphObjectReferences;
        if (refs == null || refs.Count <= BoundGraphMoveAgentIndex)
            return;

        if (refs[BoundGraphMoveAgentIndex] is Transform agent && agent != home)
            SnapToHome(agent, home);
    }

    private static Transform FindTransformByName(string objectName)
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];
            if (candidate == null || candidate.name != objectName)
                continue;

            if (!candidate.gameObject.scene.IsValid())
                continue;

            return candidate;
        }

        return null;
    }
}
