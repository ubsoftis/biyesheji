using UnityEngine;

public class UILockSignManager : MonoBehaviour
{
    [Header("拖入你的两个UI父对象")]
    public GameObject ui_SetUI;
    public GameObject ui_ChapterUI;

    public static bool uiIsOpen;

    void Update()
    {
        // 只要其中一个UI显示，就锁定路牌
        bool setOpen = ui_SetUI != null && ui_SetUI.activeSelf;
        bool chapterOpen = ui_ChapterUI != null && ui_ChapterUI.activeSelf;

        uiIsOpen = setOpen || chapterOpen;
    }
}