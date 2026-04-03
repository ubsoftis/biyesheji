using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NodeCanvas.Framework;

public class activeAni : MonoBehaviour
{
    private const string CutsceneTriggerKey = "过场动画可以触发";

    [Header("条件1：点击主角闭眼（点击一次后满足点击条件）")]
    [Tooltip("如果你的“闭眼”是 UI Button，请拖入；点击一次后本条件的“点击部分”满足。")]
    public Button heroCloseEyeButton;

    [Header("条件1：主角闭眼物体（要求为不激活，即 active=false）")]
    [Tooltip("拖入“闭眼效果/闭眼图片/闭眼模型”等物体。activeInHierarchy=true 表示闭眼；本条件要求它为 false。")]
    public GameObject heroEyeClosedObject;

    [Header("条件2：全局黑板")]
    public GlobalBlackboard gbb;

    [Header("过场动画")]
    [Tooltip("播放过场的 Animator（可以是 UI 或场景物体的 Animator）。")]
    public Animator cutsceneAnimator;

    [Tooltip("Animator Trigger 参数名（触发播放）。")]
    public string playTriggerName = "Play";

    [Tooltip("用于等待动画结束的 Clip（优先使用它的 length）。可不填，改用 waitSeconds。")]
    public AnimationClip cutsceneClip;

    [Tooltip("当未提供 cutsceneClip 时使用的等待秒数。")]
    public float waitSeconds = 3f;

    [Header("结束后切换关卡")]
    [Tooltip("要加载的场景名（确保已加入 Build Settings）。")]
    public string nextSceneName = "第二关场景";

    [Header("输出：条件是否满足（bool）")]
    public bool conditionsMet;

    [Tooltip("只执行一次（推荐）")]
    public bool triggerOnce = true;

    bool _clickedOnce;
    bool _triggered;
    bool _listening;

    void OnEnable()
    {
        RegisterButton();
    }

    void OnDisable()
    {
        UnregisterButton();
    }

    void Update()
    {
        bool canTriggerCutscene = gbb != null && gbb.GetVariableValue<bool>(CutsceneTriggerKey);
        bool heroEyeClosed = heroEyeClosedObject != null && heroEyeClosedObject.activeInHierarchy;

        conditionsMet = canTriggerCutscene && _clickedOnce && heroEyeClosed == false;

        if (triggerOnce && _triggered) return;
        if (!conditionsMet) return;

        _triggered = true;
        StartCoroutine(PlayAndLoad());
    }

    IEnumerator PlayAndLoad()
    {
        if (cutsceneAnimator != null && !string.IsNullOrEmpty(playTriggerName))
        {
            cutsceneAnimator.ResetTrigger(playTriggerName);
            cutsceneAnimator.SetTrigger(playTriggerName);
        }

        float t = 0f;
        if (cutsceneClip != null) t = Mathf.Max(0f, cutsceneClip.length);
        else t = Mathf.Max(0f, waitSeconds);

        if (t > 0f) yield return new WaitForSeconds(t);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void RegisterButton()
    {
        if (_listening) return;
        if (heroCloseEyeButton == null) return;
        heroCloseEyeButton.onClick.AddListener(OnHeroCloseEyeClicked);
        _listening = true;
    }

    void UnregisterButton()
    {
        if (!_listening) return;
        if (heroCloseEyeButton == null) return;
        heroCloseEyeButton.onClick.RemoveListener(OnHeroCloseEyeClicked);
        _listening = false;
    }

    void OnHeroCloseEyeClicked()
    {
        _clickedOnce = true;
    }
}
