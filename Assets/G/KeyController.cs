using UnityEngine;
using UnityEngine.UI;

public class KeyController : MonoBehaviour
{
    public GameObject keyInfoPanel;   // 钥匙信息弹窗
    public GameObject keyModel;       // 钥匙3D模型
    public Button closeButton;        // 关闭键按钮

    private bool hasCollected = false;

    void Start()
    {
        keyInfoPanel.SetActive(false);
        keyModel.SetActive(false);
        closeButton.onClick.AddListener(ClosePanel);
    }

    public void OnKeyClicked()
    {
        // 打开钥匙信息弹窗与模型
        keyInfoPanel.SetActive(true);
        keyModel.SetActive(true);
    }

    void ClosePanel()
    {
        // 关闭弹窗与模型
        keyInfoPanel.SetActive(false);
        keyModel.SetActive(false);

        // 表示钥匙已被收集
        hasCollected = true;

        // 隐藏UI中的钥匙图片
        gameObject.SetActive(false);
    }

    public bool IsCollected()
    {
        return hasCollected;
    }
}