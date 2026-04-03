using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CountdownToGameOver : MonoBehaviour
{
    [Header("开关")]
    public bool enableCountdown = true;

    [Header("关卡进度门控（bool）")]
    [Tooltip("为 true 时才允许启动倒计时；你可以根据关卡进度在别的脚本里修改它。")]
    public bool countdownProgressGate = true;

    [Header("倒计时参数")]
    public float durationSeconds = 30f;
    [Tooltip("触发一次后是否不再触发第二次。")]
    public bool triggerOnce = true;
    [Tooltip("若倒计时已在跑，再次触发时是否重置为 30s 重新开始。")]
    public bool restartIfTriggeredAgain = false;

    [Header("触发条件（任选其一或多个）")]
    [Tooltip("任意格子判定成功：把 9 个 StencilCubePlant 都拖进来，只要任意一个 isVisible=true 就触发。")]
    public StencilCubePlant[] plants;

    public activePaper1_2 paper12;
    public activePaper1_3 paper13;

    [Header("只读：实时格子检测结果（bool）")]
    [Tooltip("plants 中是否存在任意一个 isVisible=true（忽略 null）。")]
    public bool anyPlantsVisible;
    [Tooltip("plants 中是否所有非 null 的格子都 isVisible=true；若没有任何非 null 格子则为 false。")]
    public bool allPlantsVisible;

    [Header("结束后进入死亡状态")]
    public GameOverController gameOverController;

    [Header("可选：显示倒计时（UGUI Text）")]
    public Text uiText;

    [Header("只读状态")]
    public bool isRunning;
    public float remainingSeconds;

    Coroutine _co;
    bool _finishedOnce;

    void Update()
    {
        RecalculatePlantsVisibility();

        if (!enableCountdown) return;
        if (!countdownProgressGate) return;
        if (triggerOnce && _finishedOnce) return;

        bool triggered = IsTriggered();
        if (!triggered) return;

        if (_co == null)
        {
            StartCountdown();
        }
        else if (restartIfTriggeredAgain)
        {
            StartCountdown();
        }
    }

    bool IsTriggered()
    {
        bool t = false;
        // 默认：任意格子命中就触发
        t |= anyPlantsVisible;
        if (paper12 != null)
            t |= paper12.conditionsMet;
        if (paper13 != null)
            t |= paper13.conditionsMet;
        return t;
    }

    void RecalculatePlantsVisibility()
    {
        bool any = false;
        bool all = true;
        bool hasAnyNonNull = false;

        if (plants != null)
        {
            for (int i = 0; i < plants.Length; i++)
            {
                var p = plants[i];
                if (p == null) continue;
                hasAnyNonNull = true;

                bool v = p.isVisible;
                if (v) any = true;
                if (!v) all = false;
            }
        }

        anyPlantsVisible = any;
        allPlantsVisible = hasAnyNonNull && all;
    }

    public void StartCountdown()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoCountdown());
    }

    public void StopCountdown()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
        isRunning = false;
        remainingSeconds = 0f;
        if (uiText != null) uiText.text = "";
    }

    IEnumerator CoCountdown()
    {
        isRunning = true;
        remainingSeconds = Mathf.Max(0f, durationSeconds);

        while (remainingSeconds > 0f)
        {
            if (uiText != null)
                uiText.text = Mathf.CeilToInt(remainingSeconds).ToString();

            remainingSeconds -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (uiText != null) uiText.text = "0";

        isRunning = false;
        _co = null;
        _finishedOnce = true;

        if (gameOverController != null)
            gameOverController.EnterGameOver();
    }
}

