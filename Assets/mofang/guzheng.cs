using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class guzheng : MonoBehaviour
{
   [ContextMenu("Normalize Cubelet Pivots (wrap to center)")]
void NormalizeCubeletPivots()
{
    int cubeLayer = LayerMask.NameToLayer("Cube");
    var allColliders = GameObject.FindObjectsOfType<Collider>();
    foreach (var col in allColliders)
    {
        var go = col.gameObject;
        if (go.layer != cubeLayer) continue;

        // 已经处理过的跳过
        if (go.transform.parent != null && go.transform.parent.name.StartsWith("Pivot_")) continue;

        // 计算几何中心：优先使用网格本地包围盒中心（转到世界坐标），否则合并Renderer的AABB
        Vector3 center;
        var mf = go.GetComponent<MeshFilter>();
        var smr = go.GetComponent<SkinnedMeshRenderer>();
        if (mf != null && mf.sharedMesh != null)
        {
            center = go.transform.TransformPoint(mf.sharedMesh.bounds.center);
        }
        else if (smr != null && smr.sharedMesh != null)
        {
            center = go.transform.TransformPoint(smr.sharedMesh.bounds.center);
        }
        else
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) continue;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            center = b.center;
        }

        // 若该对象来自Prefab实例，需要先在Editor中解包后才能修改层级
#if UNITY_EDITOR
        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        if (prefabRoot != null)
        {
            PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        }
#endif

        // 创建新的父物体作为中心枢轴
        GameObject pivot = new GameObject("Pivot_" + go.name);
        pivot.layer = cubeLayer;
        pivot.transform.SetParent(go.transform.parent, true);
        pivot.transform.position = center;
        pivot.transform.rotation = go.transform.rotation;
        pivot.transform.localScale = Vector3.one;

        // 把原物体放到新枢轴下
        go.transform.SetParent(pivot.transform, true);
    }
}
}
