using UnityEngine;

/// <summary>
/// 根据摄像机当前朝向自动切换哪一面允许被射线点击。
/// 把它挂在魔方管理器上，并为每个面配置 Transform 与 stencilCollide。
/// </summary>
public class RubiksStencilVisibility : MonoBehaviour
{
    [System.Serializable]
    public class FaceEntry
    {
        public string name;
        public Transform faceTransform;     // 面（或其挂在的碰撞体）Transform
        public stencilCollide colliderGate; // 对应的 stencilCollide
        [Range(0.5f, 1f)]
        public float threshold = 0.95f;     // 面朝向摄像机到达该阈值才判定为“前面”
    }

    [Tooltip("参考摄像机（留空默认 Camera.main）")]
    public Camera referenceCamera;

    [Tooltip("六个面的信息，顺序任意")]
    public FaceEntry[] faces;

    int currentFrontIndex = -1;

    void Awake()
    {
        if (referenceCamera == null)
        {
            referenceCamera = Camera.main;
        }
    }

    void Update()
    {
        if (referenceCamera == null || faces == null || faces.Length == 0)
            return;

        Vector3 camPos = referenceCamera.transform.position;
        int bestIndex = -1;
        float bestScore = -1f;

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            if (face.faceTransform == null || face.colliderGate == null)
                continue;

            Vector3 toCamera = (camPos - face.faceTransform.position).normalized;
            Vector3 normal = face.faceTransform.forward.normalized;
            float score = Vector3.Dot(normal, toCamera);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        if (bestIndex == -1)
            return;

        // 只有当最优面达到自身阈值时才算“前面”
        if (bestScore < faces[bestIndex].threshold)
            bestIndex = -1;

        if (bestIndex == currentFrontIndex)
            return;

        currentFrontIndex = bestIndex;
        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            if (face.colliderGate == null)
                continue;
            bool visible = (i == currentFrontIndex);
            face.colliderGate.SetStencilVisible(visible);
        }
    }
}

