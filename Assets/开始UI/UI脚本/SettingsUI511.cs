using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SettingsUI : MonoBehaviour
{
    [Header("内容面板")]
    public GameObject panelGameControls;
    public GameObject panelDisplaySettings;
    public GameObject panelAudioSettings;

    [Header("标签按钮")]
    public Button btnGameControls;
    public Button btnDisplaySettings;
    public Button btnAudioSettings;

    [Header("按钮高亮设置")]
    public Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);  // 普通状态（暗）
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);   // 悬停状态
    public Color selectedColor = Color.white;                      // 选中状态（亮）
    public float colorTransitionSpeed = 8f;                        // 颜色过渡速度

    [Header("退出按钮")]
    public Button btnClose;

    [Header("黑屏设置")]
    public Image blackScreen;
    [Range(0f, 1f)]
    public float blackScreenAlpha = 0.8f;
    public float blackScreenDuration = 0.5f;

    [Header("淡出设置")]
    public CanvasGroup panelCanvasGroup;
    public float fadeDuration = 0.3f;

    private int currentSelected = 0;
    private Image[] buttonImages;
    private Color[] targetColors;
    private bool[] isHovering;

    void Start()
    {
        // 初始化按钮图片数组
        buttonImages = new Image[3];
        buttonImages[0] = btnGameControls.GetComponent<Image>();
        buttonImages[1] = btnDisplaySettings.GetComponent<Image>();
        buttonImages[2] = btnAudioSettings.GetComponent<Image>();

        targetColors = new Color[3];
        isHovering = new bool[3];

        // 绑定点击事件
        btnGameControls.onClick.AddListener(() => SwitchPanel(0));
        btnDisplaySettings.onClick.AddListener(() => SwitchPanel(1));
        btnAudioSettings.onClick.AddListener(() => SwitchPanel(2));

        // 绑定悬停事件
        AddHoverEvents(btnGameControls, 0);
        AddHoverEvents(btnDisplaySettings, 1);
        AddHoverEvents(btnAudioSettings, 2);

        if (btnClose != null)
        {
            btnClose.onClick.AddListener(Close);
        }

        // 初始化黑屏
        if (blackScreen != null)
        {
            blackScreen.color = new Color(0, 0, 0, 0);
        }

        // 默认选中第一个
        SwitchPanel(0);
    }

    void Update()
    {
        // 平滑过渡按钮颜色
        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages[i] != null)
            {
                buttonImages[i].color = Color.Lerp(
                    buttonImages[i].color,
                    targetColors[i],
                    Time.deltaTime * colorTransitionSpeed
                );
            }
        }
    }

    void AddHoverEvents(Button btn, int index)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = btn.gameObject.AddComponent<EventTrigger>();
        }

        // 鼠标进入
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => OnButtonHoverEnter(index));
        trigger.triggers.Add(enterEntry);

        // 鼠标离开
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => OnButtonHoverExit(index));
        trigger.triggers.Add(exitEntry);
    }

    void OnButtonHoverEnter(int index)
    {
        isHovering[index] = true;
        UpdateButtonColors();
    }

    void OnButtonHoverExit(int index)
    {
        isHovering[index] = false;
        UpdateButtonColors();
    }

    void UpdateButtonColors()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i == currentSelected)
            {
                // 选中的按钮保持亮
                targetColors[i] = selectedColor;
            }
            else if (isHovering[i])
            {
                // 悬停的按钮中等亮
                targetColors[i] = hoverColor;
            }
            else
            {
                // 普通状态暗
                targetColors[i] = normalColor;
            }
        }
    }

    public void SwitchPanel(int index)
    {
        currentSelected = index;

        // 隐藏所有面板
        panelGameControls.SetActive(false);
        panelDisplaySettings.SetActive(false);
        panelAudioSettings.SetActive(false);

        // 显示选中的面板
        switch (index)
        {
            case 0:
                panelGameControls.SetActive(true);
                break;
            case 1:
                panelDisplaySettings.SetActive(true);
                break;
            case 2:
                panelAudioSettings.SetActive(true);
                break;
        }

        // 更新按钮颜色
        UpdateButtonColors();
    }

    public void Close()
    {
        StartCoroutine(CloseWithEffect());
    }

    IEnumerator CloseWithEffect()
    {
        // 黑屏渐入
        if (blackScreen != null)
        {
            float timer = 0f;
            while (timer < blackScreenDuration / 2)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0, blackScreenAlpha, timer / (blackScreenDuration / 2));
                blackScreen.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            yield return new WaitForSeconds(blackScreenDuration / 2);
        }

        // 面板淡出
        float fadeTimer = 0f;
        float startBlackAlpha = blackScreen != null ? blackScreen.color.a : 0;

        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float t = fadeTimer / fadeDuration;

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = Mathf.Lerp(1, 0, t);
            }

            if (blackScreen != null)
            {
                blackScreen.color = new Color(0, 0, 0, Mathf.Lerp(startBlackAlpha, 0, t));
            }

            yield return null;
        }

        gameObject.SetActive(false);

        // 重置状态
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1;
        }
        if (blackScreen != null)
        {
            blackScreen.color = new Color(0, 0, 0, 0);
        }
    }
}