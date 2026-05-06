using UnityEngine;

public class NightWorldHearingBoostGate : MonoBehaviour
{
    [Header("来源：夜行世界第一面的可见性脚本")]
    public StenciCube1 stenciCube1;

    [Header("当 anyOf9Visible=true 时要开启的按钮对象")]
    public GameObject buttonObject;

    void Awake()
    {
        ApplyButtonState(false);
    }

    void OnDisable()
    {
        // 仅关掉组件时 Update 不再跑，按钮可能一直留在「显示」状态。
        ApplyButtonState(false);
    }

    void Update()
    {
        bool shouldShow = stenciCube1 != null
            && stenciCube1.isActiveAndEnabled
            && stenciCube1.anyOf9Visible;
        ApplyButtonState(shouldShow);
    }

    void ApplyButtonState(bool active)
    {
        if (buttonObject != null)
        {
            buttonObject.SetActive(active);
        }
    }
}
