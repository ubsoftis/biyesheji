using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ControlSwitcher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("操作界面")]
    public GameObject keyboardPanel;  // 键盘操作
    public GameObject gamepadPanel;   // 手柄操作

    [Header("切换按钮")]
    public Button switchButton;
    public Text buttonText;  // 按钮上的文字（可选）

    [Header("呼吸灯颜色设置")]
    public Color colorA = Color.gray;                    // 灰色
    public Color colorB = new Color(0.6f, 0, 0, 1);     // 深红色
    public float breathSpeed = 2f;                       // 呼吸速度

    [Header("悬停放大设置")]
    public float hoverScale = 1.2f;           // 悬停时放大倍数
    public float scaleSpeed = 8f;             // 放大/缩小速度

    [Header("音效（可选）")]
    public AudioClip clickSfx;
    [Tooltip("Sfx 子标签，留空仅用总 Sfx")]
    public string sfxTag = "";

    private bool isShowingKeyboard = true;
    private Image buttonImage;
    private RectTransform buttonRect;
    private Vector3 originalScale;
    private bool isHovering = false;

    void Start()
    {
        switchButton.onClick.AddListener(Switch);
        buttonImage = switchButton.GetComponent<Image>();
        buttonRect = switchButton.GetComponent<RectTransform>();
        originalScale = buttonRect.localScale;

        // 默认显示键盘
        ShowKeyboard();
    }

    void Update()
    {
        // 呼吸灯效果
        if (buttonImage != null)
        {
            float t = Mathf.PingPong(Time.time * breathSpeed, 1f);
            buttonImage.color = Color.Lerp(colorA, colorB, t);
        }

        // 悬停缩放效果
        if (buttonRect != null)
        {
            Vector3 targetScale = isHovering ? originalScale * hoverScale : originalScale;
            buttonRect.localScale = Vector3.Lerp(buttonRect.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    public void Switch()
    {
        PlaySfxIfConfigured(clickSfx);
        if (isShowingKeyboard)
        {
            ShowGamepad();
        }
        else
        {
            ShowKeyboard();
        }
    }

    void ShowKeyboard()
    {
        keyboardPanel.SetActive(true);
        gamepadPanel.SetActive(false);
        isShowingKeyboard = true;

        if (buttonText != null)
            buttonText.text = "切换到手柄";
    }

    void ShowGamepad()
    {
        keyboardPanel.SetActive(false);
        gamepadPanel.SetActive(true);
        isShowingKeyboard = false;

        if (buttonText != null)
            buttonText.text = "切换到键盘";
    }

    void PlaySfxIfConfigured(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;
        string tag = string.IsNullOrWhiteSpace(sfxTag) ? null : sfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clip, tag);
    }
}
