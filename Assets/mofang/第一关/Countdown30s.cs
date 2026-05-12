using System.Collections;
using NodeCanvas.DialogueTrees.UI.Examples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Countdown30s : MonoBehaviour
{
    [Header("开关")]
    public bool enableCountdown = true;

    [Header("触发方式（按当前逻辑：anyOf9Visible）")]
    [Tooltip("当 anyOf9Visible=true 时开始倒计时。")]
    public StencilCubePlant stencilCubePlant;

    [Header("倒计时参数")]
    public float durationSeconds = 30f;
    public bool triggerOnce = true;
    public bool restartIfTriggeredAgain = false;

    [Header("可选：显示倒计时")]
    public Text uiText;

    [Header("可选：倒计时下方 Sprite")]
    [Tooltip("拖一个 UI Image（里面放你的 sprite），倒计时运行时显示，结束后隐藏。")]
    public Image bottomSpriteImage;

    [Header("倒计时结束时")]
    [Tooltip("进死亡结局前先关闭的对话 UI 根物体（例如 @DialogueUGUI）。留空则自动查找当前已加载场景里所有 DialogueUGUI。")]
    public GameObject[] dialogueUguiRootsToDisable;
    [Tooltip("倒计时结束后要激活的死亡结局对象。")]
    public GameObject deathEndingObject;
    [Tooltip("倒计时结束后要禁用的玩家控制脚本。")]
    public MonoBehaviour[] playerControlScriptsToDisable;
    [Tooltip("可选：倒计时结束后同时禁用整个玩家对象。")]
    public GameObject playerRootToDisable;

    [Header("禁止旋转与点击（死亡结局时）")]
    [Tooltip("鼠标右键拖视角的摄像机脚本，禁用后无法旋转视角。")]
    public 摄像机控制器 cameraController;
    [Tooltip("魔方按键旋转脚本，禁用后无法转魔方。")]
    public mofanKeyRotation cubeKeyRotation;
    [Tooltip("场景里对魔方/背后物体做 2D 点击检测的脚本，禁用后无法点穿到后面。")]
    public StencilCubeRaycaster2D stencilCubeRaycaster;
    [Tooltip("若还有其它点击/射线脚本，可一并拖进来。")]
    public MonoBehaviour[] extraInputScriptsToDisable;
    public UnityEvent onFinished;
    public bool isRunning;
    public float remainingSeconds;

    private Coroutine _co;
    private bool _finishedOnce;
    private bool _lastTrigger;

    void Awake()
    {
        SetUiVisible(false);
    }

    void OnDisable()
    {
        // 仅把组件关掉（例如 SuccessDisableOneScript / UnityEvent set_enabled）时，
        // Unity 会停协程，但不会走 StopCountdownAndHideUi，倒计时 UI 会一直留在场上。
        StopCountdownAndHideUi();
    }

    void Update()
    {
        if (!enableCountdown)
        {
            StopCountdownAndHideUi();
            return;
        }

        bool trigger = stencilCubePlant != null && stencilCubePlant.anyOf9Visible;
        if (!trigger)
        {
            StopCountdownAndHideUi();
            _lastTrigger = false;
            return;
        }

        bool risingEdge = trigger && !_lastTrigger;
        _lastTrigger = trigger;

        if (!risingEdge) return;
        if (triggerOnce && _finishedOnce) return;

        if (_co == null)
        {
            StartCountdown();
        }
        else if (restartIfTriggeredAgain)
        {
            StartCountdown();
        }
    }
    public void StartCountdown()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoCountdown());
    }

    void StopCountdownAndHideUi()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        isRunning = false;
        SetUiVisible(false);
    }

    IEnumerator CoCountdown()
    {
        isRunning = true;
        remainingSeconds = Mathf.Max(0f, durationSeconds);
        SetUiVisible(true);

        while (remainingSeconds > 0f)
        {
            if (uiText != null)
                uiText.text = FormatToMinuteSecond(remainingSeconds);
            remainingSeconds -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (uiText != null) uiText.text = "00:00";
        SetUiVisible(false);
        isRunning = false;
        _co = null;
        _finishedOnce = true;
        ApplyFailureOutcome();
        onFinished?.Invoke();
    }

    void SetUiVisible(bool visible)
    {
        if (uiText != null)
        {
            uiText.gameObject.SetActive(visible);
        }

        if (bottomSpriteImage != null)
        {
            bottomSpriteImage.gameObject.SetActive(visible);
        }
    }

    string FormatToMinuteSecond(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return minutes.ToString("00") + ":" + secs.ToString("00");
    }

    void ApplyFailureOutcome()
    {
        DisableDialogueUgui();

        if (deathEndingObject != null)
        {
            deathEndingObject.SetActive(true);
        }

        if (playerControlScriptsToDisable != null)
        {
            for (int i = 0; i < playerControlScriptsToDisable.Length; i++)
            {
                if (playerControlScriptsToDisable[i] != null)
                {
                    playerControlScriptsToDisable[i].enabled = false;
                }
            }
        }

        if (playerRootToDisable != null)
        {
            playerRootToDisable.SetActive(false);
        }

        if (cameraController != null) cameraController.enabled = false;
        if (cubeKeyRotation != null) cubeKeyRotation.enabled = false;
        if (stencilCubeRaycaster != null) stencilCubeRaycaster.enabled = false;

        if (extraInputScriptsToDisable != null)
        {
            for (int i = 0; i < extraInputScriptsToDisable.Length; i++)
            {
                if (extraInputScriptsToDisable[i] != null)
                    extraInputScriptsToDisable[i].enabled = false;
            }
        }
    }

    void DisableDialogueUgui()
    {
        if (dialogueUguiRootsToDisable != null && dialogueUguiRootsToDisable.Length > 0)
        {
            for (int i = 0; i < dialogueUguiRootsToDisable.Length; i++)
            {
                if (dialogueUguiRootsToDisable[i] != null)
                    dialogueUguiRootsToDisable[i].SetActive(false);
            }

            return;
        }

        DialogueUGUI[] all = Object.FindObjectsOfType<DialogueUGUI>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
                all[i].gameObject.SetActive(false);
        }
    }
}
