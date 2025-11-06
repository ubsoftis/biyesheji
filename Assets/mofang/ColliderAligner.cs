using UnityEngine;

/// <summary>
/// 碰撞体对齐工具类
/// 用于自动调整碰撞体的位置和大小，使其与实际的方块位置对齐
/// </summary>
public static class ColliderAligner
{
    /// <summary>
    /// 对齐单个碰撞体，使其与选中的方块对齐
    /// </summary>
    /// <param name="targetCollider">要调整的碰撞体对象</param>
    /// <param name="cubeRoot">魔方根节点</param>
    /// <param name="cubeLayerName">方块所在的层名称</param>
    /// <param name="colliderThickness">碰撞体厚度（沿法线方向）</param>
    /// <returns>是否成功对齐</returns>
    public static bool AlignCollider(GameObject targetCollider, Transform cubeRoot, string cubeLayerName = "Cube", float colliderThickness = 0.1f)
    {
        if (targetCollider == null)
        {
            Debug.LogError("[ColliderAligner] targetCollider 未设置");
            return false;
        }
        
        var boxCollider = targetCollider.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError($"[ColliderAligner] {targetCollider.name} 缺少 BoxCollider 组件");
            return false;
        }
        
        if (cubeRoot == null)
        {
            Debug.LogError("[ColliderAligner] cubeRoot 未设置");
            return false;
        }
        
        var targetTransform = targetCollider.transform;
        var forward = targetTransform.forward;
        var right = targetTransform.right;
        var up = targetTransform.up;
        
        // 使用当前碰撞体位置和大小来检测方块
        int layerMask = LayerMask.GetMask(cubeLayerName);
        Vector3 centerWorld = targetTransform.TransformPoint(boxCollider.center);
        Vector3 lossy = targetTransform.lossyScale;
        Vector3 scaleAbs = new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
        Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, scaleAbs);
        Quaternion orientation = targetTransform.rotation;
        
        Collider[] hitColliders = Physics.OverlapBox(centerWorld, halfExtents, orientation, layerMask);
        
        if (hitColliders.Length == 0)
        {
            Debug.LogWarning($"[ColliderAligner] {targetCollider.name} 未检测到任何方块，尝试扩大检测范围");
            // 扩大检测范围
            halfExtents *= 2f;
            hitColliders = Physics.OverlapBox(centerWorld, halfExtents, orientation, layerMask);
            if (hitColliders.Length == 0)
            {
                Debug.LogError($"[ColliderAligner] {targetCollider.name} 扩大范围后仍未检测到方块");
                return false;
            }
        }
        
        // 计算选中方块的几何中心和边界
        Vector3 sumPos = Vector3.zero;
        int count = 0;
        
        // 在碰撞体的局部坐标系下计算边界
        float minR = float.MaxValue, maxR = float.MinValue;
        float minU = float.MaxValue, maxU = float.MinValue;
        float minF = float.MaxValue, maxF = float.MinValue;
        
        foreach (var coll in hitColliders)
        {
            Bounds bounds = coll.bounds;
            sumPos += bounds.center;
            count++;
            
            // 检查包围盒的8个角点
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 p = c + new Vector3(e.x * sx, e.y * sy, e.z * sz);
                
                // 投影到碰撞体的 right/up/forward 轴上（相对于当前中心）
                float pr = Vector3.Dot(p - centerWorld, right);
                float pu = Vector3.Dot(p - centerWorld, up);
                float pf = Vector3.Dot(p - centerWorld, forward);
                
                if (pr < minR) minR = pr;
                if (pr > maxR) maxR = pr;
                if (pu < minU) minU = pu;
                if (pu > maxU) maxU = pu;
                if (pf < minF) minF = pf;
                if (pf > maxF) maxF = pf;
            }
        }
        
        if (count == 0) return false;
        
        // 计算实际几何中心
        Vector3 actualCenter = sumPos / count;
        
        // 计算尺寸（添加小量余量）
        float sizeR = Mathf.Max(0.01f, (maxR - minR) * 1.02f);
        float sizeU = Mathf.Max(0.01f, (maxU - minU) * 1.02f);
        float sizeF = Mathf.Max(colliderThickness, (maxF - minF) * 1.02f);
        
        // 计算中心偏移（在局部坐标系下）
        float centerR = (minR + maxR) * 0.5f;
        float centerU = (minU + maxU) * 0.5f;
        float centerF = (minF + maxF) * 0.5f;
        
        // 更新碰撞体位置（世界坐标）
        targetTransform.position = actualCenter;
        
        // 更新 BoxCollider 的局部中心和尺寸
        // 将偏移转换到局部坐标系
        Vector3 localOffset = right * centerR + up * centerU + forward * centerF;
        Vector3 localCenter = targetTransform.InverseTransformPoint(actualCenter + localOffset);
        
        boxCollider.center = localCenter;
        boxCollider.size = new Vector3(sizeR, sizeU, sizeF);
        
        Debug.Log($"[ColliderAligner] {targetCollider.name} 已对齐: 中心={actualCenter}, 尺寸=({sizeR:F3}, {sizeU:F3}, {sizeF:F3}), 选中{count}个方块");
        return true;
    }
    
    /// <summary>
    /// 对齐所有中间层碰撞体
    /// </summary>
    /// <param name="rotationRequire">魔方旋转控制器</param>
    public static void AlignAllMiddleColliders(mofanKeyRotationRequire rotationRequire)
    {
        if (rotationRequire == null)
        {
            Debug.LogError("[ColliderAligner] rotationRequire 未设置");
            return;
        }
        
        // 使用反射获取私有字段
        var middleRowField = typeof(mofanKeyRotationRequire).GetField("Coller_middleRow", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var middleColField = typeof(mofanKeyRotationRequire).GetField("Coller_middleCol", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var cubeRootField = typeof(mofanKeyRotationRequire).GetField("CubeRoot", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        Transform cubeRoot = cubeRootField != null ? (Transform)cubeRootField.GetValue(rotationRequire) : null;
        
        if (middleRowField != null)
        {
            var middleRow = (GameObject)middleRowField.GetValue(rotationRequire);
            if (middleRow != null)
            {
                AlignCollider(middleRow, cubeRoot);
            }
        }
        
        if (middleColField != null)
        {
            var middleCol = (GameObject)middleColField.GetValue(rotationRequire);
            if (middleCol != null)
            {
                AlignCollider(middleCol, cubeRoot);
            }
        }
    }
}

