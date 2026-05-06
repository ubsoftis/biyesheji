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

    void OnDisable()
    {
        // 仅关掉组件时 Update 不再跑，按钮可能一直留在「显示」状态。
        ApplyButtonState(false);
    }

    void Update()
    {
        // StenciCube 被别的逻辑 Disable 后，anyOf9Visible 会停在最后一帧，常为 true；
        // 若不检查 isActiveAndEnabled，门控仍会一直把听力增强按钮打开。
        bool shouldShow = stenciCube1Only9 != null
            && stenciCube1Only9.isActiveAndEnabled
            && stenciCube1Only9.anyOf9Visible;
        ApplyButtonState(shouldShow);
    }

    void ApplyButtonState(bool active)
    {
        if (buttonObject != null)
            buttonObject.SetActive(active);
    }
}

