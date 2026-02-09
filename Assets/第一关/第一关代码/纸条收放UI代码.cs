using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 可展开/收回的纸条UI控制脚本（无缩放版）
/// 挂载对象：纸条的根Canvas/Image对象
/// </summary>
public class 纸条收放UI代码 : MonoBehaviour
{
    [Header("核心配置")]
    [Tooltip("纸条收起时的位置（基于RectTransform的anchoredPosition）")]
    public Vector2 retractedPosition = new Vector2(800, 0); // 默认右侧收起位置
    [Tooltip("纸条展开时的位置")]
    public Vector2 expandedPosition = new Vector2(0, 0); // 默认左侧展开位置
    [Tooltip("移动动画时长（秒）")]
    public float moveDuration = 0.5f; // 可自定义移动速度

    [Header("增强动画配置")]
    [Tooltip("展开时的透明度（1=不透明）")]
    [Range(0f, 1f)] public float expandedAlpha = 1f;
    [Tooltip("收起时的透明度（0.8=轻微半透）")]
    [Range(0f, 1f)] public float retractedAlpha = 0.8f;

    [Header("组件引用")]
    [Tooltip("控制展开/收回的按钮")]
    public Button toggleButton;
    private RectTransform noteRect;
    private Image noteImage; // 控制纸条透明度

    // 状态变量
    private bool isExpanded = false; // 是否处于展开状态
    private float currentMoveTime = 0f; // 当前动画进度
    private Vector2 targetPosition; // 目标位置
    private float targetAlpha; // 目标透明度值

    void Start()
    {
        // 获取纸条的RectTransform组件
        noteRect = GetComponent<RectTransform>();
        if (noteRect == null)
        {
            Debug.LogError("纸条对象缺少RectTransform组件！");
            return;
        }

        // 获取Image组件（用于透明度动画）
        noteImage = GetComponent<Image>();
        if (noteImage == null)
        {
            Debug.LogWarning("纸条对象没有Image组件，透明度动画将失效！");
        }

        // 初始化位置、透明度为收起状态（去掉缩放初始化）
        noteRect.anchoredPosition = retractedPosition;
        if (noteImage != null)
        {
            Color tempColor = noteImage.color;
            tempColor.a = retractedAlpha;
            noteImage.color = tempColor;
        }

        // 初始化目标状态
        targetPosition = retractedPosition;
        targetAlpha = retractedAlpha;

        // 绑定按钮点击事件
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleNoteState);
        }
        else
        {
            Debug.LogError("未指定控制按钮！");
        }
    }

    void Update()
    {
        // 检查是否已到达所有目标状态（位置+透明度）
        if (IsTargetReached())
        {
            currentMoveTime = 0f;
            return;
        }

        // 累计动画时间，计算0-1的进度
        currentMoveTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentMoveTime / moveDuration);
        // 缓动曲线（先快后慢，动画更丝滑）
        float easeProgress = EaseOutCubic(progress);

        // 1. 位置平滑移动（缓动效果保留）
        noteRect.anchoredPosition = Vector2.Lerp(noteRect.anchoredPosition, targetPosition, easeProgress);

        // 2. 透明度动画（保留，去掉缩放动画）
        if (noteImage != null)
        {
            Color tempColor = noteImage.color;
            tempColor.a = Mathf.Lerp(tempColor.a, targetAlpha, easeProgress);
            noteImage.color = tempColor;
        }

        // 动画完成后强制对齐目标状态（避免浮点误差）
        if (progress >= 1f)
        {
            ResetToTargetState();
        }
    }

    /// <summary>
    /// 切换纸条的展开/收回状态
    /// </summary>
    public void ToggleNoteState()
    {
        isExpanded = !isExpanded;
        // 更新目标位置、透明度（去掉缩放目标）
        targetPosition = isExpanded ? expandedPosition : retractedPosition;
        targetAlpha = isExpanded ? expandedAlpha : retractedAlpha;
        currentMoveTime = 0f; // 重置动画进度
    }

    /// <summary>
    /// 检查是否到达所有目标状态
    /// </summary>
    private bool IsTargetReached()
    {
        bool posReached = noteRect.anchoredPosition == targetPosition;
        bool alphaReached = true;
        if (noteImage != null)
        {
            alphaReached = Mathf.Abs(noteImage.color.a - targetAlpha) < 0.01f;
        }
        return posReached && alphaReached;
    }

    /// <summary>
    /// 强制对齐目标状态（避免浮点误差）
    /// </summary>
    private void ResetToTargetState()
    {
        noteRect.anchoredPosition = targetPosition;
        if (noteImage != null)
        {
            Color tempColor = noteImage.color;
            tempColor.a = targetAlpha;
            noteImage.color = tempColor;
        }
    }

    /// <summary>
    /// 缓出曲线（让动画先快后慢，更自然）
    /// </summary>
    private float EaseOutCubic(float x)
    {
        return 1 - Mathf.Pow(1 - x, 3);
    }
}