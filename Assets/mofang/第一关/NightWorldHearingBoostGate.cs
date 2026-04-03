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

    void Update()
    {
        bool shouldShow = stenciCube1 != null && stenciCube1.anyOf9Visible;
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
