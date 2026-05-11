using UnityEngine;
using UnityEngine.UI;

public class SimpleDropdown : MonoBehaviour
{
    [Header("=== 下拉设置 ===")]
    public Text currentText;      // 显示当前选择的文字
    public GameObject dropList;   // 下拉列表面板（默认隐藏）
    public Button arrowBtn;       // 箭头按钮
    public Button[] options;      // 选项按钮

    private bool isOpen = false;

    void Start()
    {
        // 绑定箭头点击事件
        arrowBtn.onClick.AddListener(OnArrowClick);

        // 绑定所有选项点击事件
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            options[i].onClick.AddListener(() => OnSelectOption(options[index].GetComponentInChildren<Text>().text));
        }
    }

    // 点击箭头：展开/收起 + 箭头旋转动画
    void OnArrowClick()
    {
        isOpen = !isOpen;
        dropList.SetActive(isOpen);

        // 箭头旋转（展开朝下，收起朝上）
        arrowBtn.transform.rotation = Quaternion.Euler(0, 0, isOpen ? 180 : 0);
    }

    // 直接读取按钮上的文字，自动设置，代码完全不用改
    void OnSelectOption(string text)
    {
        currentText.text = text;

        // 自动关闭列表 + 箭头复位
        isOpen = false;
        dropList.SetActive(false);
        arrowBtn.transform.rotation = Quaternion.identity;
    }
}