using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EyeButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("眼球组件")]
    public RectTransform eyeWhite;      // 眼白 (整个Button)
    public RectTransform pupil;          // 瞳孔 (眼黑Image)

    [Header("眼白移动设置")]
    public float eyeMoveDistance = 30f;  // 眼白左右移动的距离
    public float eyeMoveSpeedMin = 1f;   // 眼白移动速度最小值
    public float eyeMoveSpeedMax = 3f;   // 眼白移动速度最大值

    [Header("瞳孔水平移动设置")]
    public float pupilMoveDistanceX = 15f; // 瞳孔水平移动距离
    public float pupilFollowSpeedMin = 3f; // 瞳孔跟随速度最小值
    public float pupilFollowSpeedMax = 7f; // 瞳孔跟随速度最大值
    [Range(0f, 1f)]
    public float rightMoveRatio = 0.5f;    // 往右移动的比例

    [Header("瞳孔上下移动设置")]
    public float pupilMoveDistanceY = 10f; // 瞳孔上下移动距离
    public float pupilMoveSpeedYMin = 2f;  // 瞳孔上下速度最小值
    public float pupilMoveSpeedYMax = 5f;  // 瞳孔上下速度最大值

    [Header("随机速度变化设置")]
    public float speedChangeInterval = 2f; // 多久换一次随机速度（秒）

    [Header("鼠标悬停设置")]
    public float hoverScale = 1.3f;       // 悬停时放大倍数
    public float scaleSpeed = 8f;         // 放大/缩小速度

    [Header("悬停提示文本")]
    public GameObject hoverText;          // 悬停显示的文本

    private Vector2 eyeStartPos;          // 眼白初始位置
    private Vector2 pupilStartPos;        // 瞳孔初始位置
    private bool isHovering = false;      // 鼠标是否悬停
    private float currentDirectionX = 0f; // 当前水平移动方向

    // 当前随机速度
    private float currentEyeSpeed;
    private float currentPupilFollowSpeed;
    private float currentPupilSpeedY;
    private float speedChangeTimer;

    void Start()
    {
        // 记录初始位置
        if (eyeWhite != null)
            eyeStartPos = eyeWhite.anchoredPosition;

        if (pupil != null)
            pupilStartPos = pupil.anchoredPosition;

        // 初始隐藏文本
        if (hoverText != null)
            hoverText.SetActive(false);

        // 初始化随机速度
        RandomizeSpeeds();
    }

    void Update()
    {
        // 定时更换随机速度
        speedChangeTimer += Time.deltaTime;
        if (speedChangeTimer >= speedChangeInterval)
        {
            speedChangeTimer = 0f;
            RandomizeSpeeds();
        }

        if (!isHovering)
        {
            // 眼白来回移动
            MoveEye();
            // 瞳孔跟随移动
            MovePupil();
        }

        // 处理缩放
        HandleScale();
    }

    void RandomizeSpeeds()
    {
        currentEyeSpeed = Random.Range(eyeMoveSpeedMin, eyeMoveSpeedMax);
        currentPupilFollowSpeed = Random.Range(pupilFollowSpeedMin, pupilFollowSpeedMax);
        currentPupilSpeedY = Random.Range(pupilMoveSpeedYMin, pupilMoveSpeedYMax);
    }

    void MoveEye()
    {
        if (eyeWhite == null) return;

        // 用 Sin 函数实现来回移动 (-1 到 1)
        float wave = Mathf.Sin(Time.time * currentEyeSpeed);
        currentDirectionX = wave; // 记录方向，给瞳孔用

        // 计算新位置
        float offsetX = wave * eyeMoveDistance;
        Vector2 targetPos = eyeStartPos + new Vector2(offsetX, 0);

        eyeWhite.anchoredPosition = targetPos;
    }

    void MovePupil()
    {
        if (pupil == null) return;

        // 瞳孔水平移动 (左边多，右边少)
        float offsetX;
        if (currentDirectionX < 0)
        {
            // 往左：正常距离
            offsetX = currentDirectionX * pupilMoveDistanceX;
        }
        else
        {
            // 往右：按比例缩小
            offsetX = currentDirectionX * pupilMoveDistanceX * rightMoveRatio;
        }

        // 瞳孔上下移动 (只往下，不往上)
        float waveY = -Mathf.Abs(Mathf.Sin(Time.time * currentPupilSpeedY));
        float offsetY = waveY * pupilMoveDistanceY;

        // 目标位置
        Vector2 targetPos = pupilStartPos + new Vector2(offsetX, offsetY);

        // 平滑移动
        pupil.anchoredPosition = Vector2.Lerp(
            pupil.anchoredPosition,
            targetPos,
            Time.deltaTime * currentPupilFollowSpeed
        );
    }

    void HandleScale()
    {
        if (eyeWhite == null) return;

        // 目标缩放
        float targetScale = isHovering ? hoverScale : 1f;
        Vector3 target = new Vector3(targetScale, targetScale, 1f);

        // 平滑缩放
        eyeWhite.localScale = Vector3.Lerp(
            eyeWhite.localScale,
            target,
            Time.deltaTime * scaleSpeed
        );
    }

    // 鼠标进入
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        // 显示文本
        if (hoverText != null)
            hoverText.SetActive(true);
    }

    // 鼠标离开
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        // 隐藏文本
        if (hoverText != null)
            hoverText.SetActive(false);
    }
}