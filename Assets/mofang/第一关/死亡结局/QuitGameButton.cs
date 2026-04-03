using UnityEngine;

public class QuitGameButton : MonoBehaviour
{
    // 绑定到 UI Button 的 OnClick()
    public void OnClickQuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("QuitGameButton: 编辑器模式下无法真正退出，打包后会退出游戏。");
#else
        Application.Quit();
#endif
    }
}
