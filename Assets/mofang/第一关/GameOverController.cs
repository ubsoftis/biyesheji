using UnityEngine;

public class GameOverController : MonoBehaviour
{
    [Header("UI（死亡面板）")]
    [Tooltip("死亡/失败面板（包含 Retry/退出 按钮）。进入死亡状态时会 SetActive(true)。")]
    public GameObject gameOverPanel;

    [Header("进入死亡状态时要禁用的东西")]
    [Tooltip("把“魔方旋转脚本、点击交互、对话脚本”等拖到这里，死亡时会 enabled=false。")]
    public MonoBehaviour[] behavioursToDisable;

    [Tooltip("把需要死亡时直接隐藏/禁用的物体拖到这里，死亡时会 SetActive(false)。")]
    public GameObject[] objectsToDisable;

    [Header("可选：暂停时间")]
    [Tooltip("为 true 时进入死亡状态会 Time.timeScale=0（UI 仍可点击）。")]
    public bool pauseTimeScaleOnGameOver = true;

    [Header("只读状态")]
    public bool isGameOver;

    public void EnterGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (pauseTimeScaleOnGameOver)
            Time.timeScale = 0f;

        if (behavioursToDisable != null)
        {
            for (int i = 0; i < behavioursToDisable.Length; i++)
            {
                var b = behavioursToDisable[i];
                if (b != null) b.enabled = false;
            }
        }

        if (objectsToDisable != null)
        {
            for (int i = 0; i < objectsToDisable.Length; i++)
            {
                var go = objectsToDisable[i];
                if (go != null) go.SetActive(false);
            }
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        GlobalSceneTransition.ReloadCurrentScene();
    }

    public void Quit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
