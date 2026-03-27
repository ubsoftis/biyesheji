using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FishChildrenCheck : MonoBehaviour  
{
    [Header("要检测的父物体（不填就用当前物体）")]
    public Transform root;
    [Header("是否包含 root 自己（一般不需要）")]
    public bool includeRoot = false;

    [Header("总数来源")]
    [Tooltip("勾选后：totalCount 使用 fixedTotalCount（写定）；不再每帧重新统计总数。")]
    public bool useFixedTotalCount = true;
    [Min(0)]
    public int fixedTotalCount = 0;

    [Header("子物体检测（虾）")]
    [Tooltip("要检测的子物体名称，默认检测 \"虾(Clone)\"")]
    public string shrimpChildName = "虾(Clone)";
    [Tooltip("root 下（不含 root 自身）是否存在名称为 shrimpChildName 的子物体")]
    public bool hasShrimpChild;

    [Header("结果（只读）")]
    public int totalCount;
    public int activeCount;
    [Tooltip("activeCount <= totalCount/2")]
    public bool lessOrEqualHalf;

    [Header("可选：条件满足时触发一次")]
    public bool fireOnceWhenReached = false;
    public UnityEvent onReached;

    bool _hasFired;

    void Awake()
    {
        if (root == null) root = transform;
        InitializeTotalCount();
    }
    void Update()
    {
        RecalculateActiveAndCondition();
    }

    public void InitializeTotalCount()
    {
        if (root == null) { totalCount = 0; return; }

        if (useFixedTotalCount)
        {
            totalCount = Mathf.Max(0, fixedTotalCount);
            return;
        }

        totalCount = CountAllChildren(includeInactive: true);
    }

    public void RecalculateActiveAndCondition()
    {
        activeCount = 0;
        if (root == null) { lessOrEqualHalf = false; return; }

        activeCount = CountActiveChildren();
        hasShrimpChild = HasChildNamed(shrimpChildName);

        // “小于等于一半”：activeCount <= totalCount/2
        // 用整数避免浮点误差：activeCount * 2 <= totalCount
        lessOrEqualHalf = totalCount > 0 && (activeCount * 2) <= totalCount;

        if (lessOrEqualHalf && fireOnceWhenReached && !_hasFired)
        {
            _hasFired = true;
            onReached?.Invoke();
        }
    }

    int CountAllChildren(bool includeInactive)
    {
        int count = 0;
        var trs = root.GetComponentsInChildren<Transform>(includeInactive);
        for (int i = 0; i < trs.Length; i++)
        {
            if (!includeRoot && trs[i] == root) continue;
            count++;
        }
        return count;
    }

    bool HasChildNamed(string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName)) return false;

        var trs = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < trs.Length; i++)
        {
            if (!includeRoot && trs[i] == root) continue;
            if (trs[i].name == targetName)
                return true;
        }
        return false;
    }

    int CountActiveChildren()
    {
        int count = 0;
        var trs = root.GetComponentsInChildren<Transform>(true); // 取到全部，再用 activeInHierarchy 判定
        for (int i = 0; i < trs.Length; i++)
        {
            if (!includeRoot && trs[i] == root) continue;
            if (trs[i].gameObject.activeInHierarchy) count++;
        }
        return count;
    }

    // 你要的 bool 值可以直接读这个方法
    public bool IsLessOrEqualHalf()
    {
        return lessOrEqualHalf;
    }
}