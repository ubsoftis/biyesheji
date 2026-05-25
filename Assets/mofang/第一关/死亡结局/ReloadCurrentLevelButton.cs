using UnityEngine;

public class ReloadCurrentLevelButton : MonoBehaviour
{
    // 绑定到 UI Button 的 OnClick()
    public void OnClickReloadCurrentLevel()
    {
        GlobalSceneTransition.ReloadCurrentScene();
    }
}
