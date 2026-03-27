using UnityEngine;

/// <summary>
/// 挂在「可互动」的场景物体上，用来告诉 SceneInteractItemPlacer：
/// - 默认把实例化出来的预制体挂在哪个父节点下面
/// </summary>
public class ScenePlacementTarget : MonoBehaviour
{
    [Header("默认父节点")]
    [Tooltip("背包物体实例化后会放到这个 Transform 下面；不填则用当前物体自身")]
    public Transform defaultParent;
}

