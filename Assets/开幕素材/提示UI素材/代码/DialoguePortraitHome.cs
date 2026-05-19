using NodeCanvas.DialogueTrees;
using UnityEngine;

/// <summary>
/// 挂在带 DialogueActor 的物体上，指定立绘动画物体中断后应回到哪里。
/// 与对话树里 MoveTowards →「对话角色背景框」的终点一致。
/// </summary>
[DisallowMultipleComponent]
public class DialoguePortraitHome : MonoBehaviour
{
    [Tooltip("立绘动画物体。不填则用同物体上 DialogueActor.portraitAnimSource。")]
    [SerializeField] private Transform portrait;

    [Tooltip("回归目标，拖场景里的「对话角色背景框」或该角色专用框。不填则 Awake 时记录立绘当前坐标。")]
    [SerializeField] private Transform homeAnchor;

    private Vector3 cachedWorldPosition;
    private bool hasCachedPosition;

    private void Awake()
    {
        CacheInitialPositionIfNeeded();
    }

    public Transform PortraitTransform => ResolvePortrait();

    public void SnapToHome()
    {
        Transform portraitTransform = ResolvePortrait();
        Transform anchor = homeAnchor != null ? homeAnchor : null;
        if (portraitTransform == null)
            return;

        if (anchor != null)
        {
            DialoguePortraitReset.SnapToHome(portraitTransform, anchor);
            return;
        }

        Vector3 target = ResolveHomeWorldPosition();
        portraitTransform.position = new Vector3(target.x, target.y, portraitTransform.position.z);
    }

    public Vector3 ResolveHomeWorldPosition()
    {
        if (homeAnchor != null)
            return homeAnchor.position;

        if (hasCachedPosition)
            return cachedWorldPosition;

        CacheInitialPositionIfNeeded();
        return cachedWorldPosition;
    }

    private Transform ResolvePortrait()
    {
        if (portrait != null)
            return portrait;

        DialogueActor actor = GetComponent<DialogueActor>();
        if (actor != null && actor.portraitAnimSource != null)
            return actor.portraitAnimSource.transform;

        return null;
    }

    private void CacheInitialPositionIfNeeded()
    {
        if (hasCachedPosition || homeAnchor != null)
            return;

        Transform portraitTransform = ResolvePortrait();
        if (portraitTransform == null)
            return;

        cachedWorldPosition = portraitTransform.position;
        hasCachedPosition = true;
    }
}
