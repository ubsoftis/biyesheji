using UnityEngine;
using UnityEngine.UI;

public class ChestController : MonoBehaviour
{
    public KeyController keyController;   // 引用钥匙控制脚本
    public GameObject chestInfoPanel;     // 宝箱弹窗
    public GameObject chestModel;         // 宝箱模型
    public GameObject needKeyPanel;       // “需要钥匙”提示弹窗
    public Button closeButton;            // 关闭键按钮

    void Start()
    {
        chestInfoPanel.SetActive(false);
        chestModel.SetActive(false);
        needKeyPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePanel);
    }

    public void OnChestClicked()
    {
        if (keyController.IsCollected())
        {
            // 有钥匙 → 打开宝箱弹窗与模型
            chestInfoPanel.SetActive(true);
            chestModel.SetActive(true);
        }
        else
        {
            // 没钥匙 → 提示需要钥匙
            needKeyPanel.SetActive(true);
        }
    }

    void ClosePanel()
    {
        chestInfoPanel.SetActive(false);
        chestModel.SetActive(false);
        needKeyPanel.SetActive(false);
    }
}