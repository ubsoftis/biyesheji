using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadCurrentLevelButton : MonoBehaviour
{
    // 绑定到 UI Button 的 OnClick()
    public void OnClickReloadCurrentLevel()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
