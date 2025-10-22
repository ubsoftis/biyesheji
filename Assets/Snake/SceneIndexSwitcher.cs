using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneIndexSwitcher : MonoBehaviour
{
    // 在 Inspector 面板里填写要跳转的场景索引
    public int sceneIndex = 1;

    // 按钮点击时调用此方法
    public void SwitchSceneByIndex()
    {
        SceneManager.LoadScene(sceneIndex);
    }
}