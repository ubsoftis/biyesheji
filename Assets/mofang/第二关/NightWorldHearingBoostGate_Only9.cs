using UnityEngine;

/// <summary>
/// NightWorldHearingBoostGate 的 9 点采样版本：
/// 引用 StenciCube1Only9Samples.anyOf9Visible 来控制按钮显隐。
/// </summary>
public class NightWorldHearingBoostGate_Only9 : MonoBehaviour
{
    [Header("来源：夜行世界第一面的可见性脚本（仅 9 点采样）")]
    public StenciCube1Only9Samples stenciCube1Only9;

    [Header("当 anyOf9Visible=true 时要开启的按钮对象")]
    public GameObject buttonObject;

    void Awake()
    {
        ApplyButtonState(false);
    }

    void Update()
    {
        bool shouldShow = stenciCube1Only9 != null && stenciCube1Only9.anyOf9Visible;
        ApplyButtonState(shouldShow);
    }

    void ApplyButtonState(bool active)
    {
        if (buttonObject != null)
            buttonObject.SetActive(active);
    }
}

