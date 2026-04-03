using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NodeCanvas.Framework;

public class activeAni : MonoBehaviour
{
    private const string CutsceneTriggerKey = "过场动画可以触发";

    [Header("条件1：点击入口（闭眼按钮等）")]
    [Tooltip("例如「主角闭眼」上的 Button：点击一次后 _clickedOnce=true。")]
    public Button heroCloseEyeButton;

    [Header("条件1：主角闭眼状态 UI/物体")]
    [Tooltip("未闭眼/未显示闭眼层时应为 inactive；activeInHierarchy=true 表示正在显示闭眼。")]
    public GameObject heroEyeClosedObject;

    [Header("条件1（可选）：主角本体")]
    [Tooltip("可选。若填写：主角 active 时也可满足状态（与「闭眼未显示」二选一）。")]
    public GameObject heroProtagonist;

    [Header("条件2：全局黑板")]
    public GlobalBlackboard gbb;

    [Header("过场动画")]
    [Tooltip("条件满足时 SetActive(true) 的根物体（如整段过场面板）。不填则使用 cutsceneAnimator 所在物体。")]
    public GameObject cutsceneObjectToActivate;

    [Tooltip("播放过场的 Animator（可以是 UI 或场景物体的 Animator）。")]
    public Animator cutsceneAnimator;

    [Tooltip("Animator Trigger 参数名（触发播放）。")]
    public string playTriggerName = "Play";

    [Tooltip("用于等待动画结束的 Clip（优先使用它的 length）。可不填，改用 waitSeconds。")]
    public AnimationClip cutsceneClip;

    [Tooltip("当未提供 cutsceneClip 时使用的等待秒数。")]
    public float waitSeconds = 3f;

    [Tooltip("当未提供 cutsceneClip 时：尝试从 Animator 当前播放的 Clip 推断时长。")]
    public bool tryGetLengthFromAnimator = true;

    [Tooltip("最小等待时间（避免 0 秒导致直接跳关）。")]
    public float minWaitSeconds = 0.1f;

    [Header("结束后切换关卡")]
    [Tooltip("要加载的场景名（确保已加入 Build Settings）。")]
    public string nextSceneName = "第二关场景";

    [Header("输出：条件是否满足（bool）")]
    public bool conditionsMet;

    [Tooltip("只执行一次（推荐）")]
    public bool triggerOnce = true;

    [Tooltip("为 false 时：不要求点击 heroCloseEyeButton，只要黑板允许且 stateOk 就触发（排障/特殊流程用）。")]
    public bool requireHeroClick = true;

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
        // 「闭眼层未显示」：未引用或已隐藏；或「主角本体」已激活。与未引用闭眼物体时 stateOk 不应恒为 false。
        bool closedLayerOff = heroEyeClosedObject == null || !heroEyeClosedObject.activeInHierarchy;
        bool protagonistOn = heroProtagonist != null && heroProtagonist.activeInHierarchy;
        bool stateOk = closedLayerOff || protagonistOn;

        bool clickOk = !requireHeroClick || _clickedOnce;
        conditionsMet = canTriggerCutscene && clickOk && stateOk;

        if (triggerOnce && _triggered) return;
        if (!conditionsMet) return;

        _triggered = true;
        ActivateCutsceneObjectIfNeeded();
        StartCoroutine(PlayAndLoad());
    }

    void ActivateCutsceneObjectIfNeeded()
    {
        GameObject go = cutsceneObjectToActivate;
        if (go == null && cutsceneAnimator != null)
            go = cutsceneAnimator.gameObject;
        if (go != null && !go.activeSelf)
            go.SetActive(true);
    }

    IEnumerator PlayAndLoad()
    {
        if (cutsceneAnimator != null && !string.IsNullOrEmpty(playTriggerName))
        {
            if (!cutsceneAnimator.gameObject.activeInHierarchy)
                ActivateCutsceneObjectIfNeeded();

            if (cutsceneAnimator.runtimeAnimatorController != null)
            {
                cutsceneAnimator.ResetTrigger(playTriggerName);
                cutsceneAnimator.SetTrigger(playTriggerName);
            }
        }

        float t = 0f;
        if (cutsceneClip != null)
        {
            t = Mathf.Max(0f, cutsceneClip.length);
        }
        else
        {
            t = Mathf.Max(0f, waitSeconds);

            // 给 Animator 一帧时间进入状态，避免立刻取到空 clip
            if (tryGetLengthFromAnimator && cutsceneAnimator != null)
            {
                yield return null;
                var clips = cutsceneAnimator.GetCurrentAnimatorClipInfo(0);
                if (clips != null && clips.Length > 0 && clips[0].clip != null)
                {
                    t = Mathf.Max(t, clips[0].clip.length);
                }
            }
        }

        t = Mathf.Max(minWaitSeconds, t);
        yield return new WaitForSeconds(t);

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
