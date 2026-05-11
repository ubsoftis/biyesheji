using UnityEngine;
using UnityEngine.UI;

public class ClickSignFadeQuit : MonoBehaviour
{
    [Header("拖入你全屏黑色遮罩Image")]
    public Image blackMask;
    public float fadeSpeed = 1f;

    private bool isFading = false;

    void Update()
    {
        // 修正：使用正确的变量名
        if (UILockSignManager.uiIsOpen)
            return;

        if (!isFading && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    isFading = true;
                }
            }
        }

        if (isFading && blackMask != null)
        {
            Color col = blackMask.color;
            col.a += fadeSpeed * Time.deltaTime;
            col.a = Mathf.Clamp01(col.a);
            blackMask.color = col;

            if (col.a >= 1f)
            {
                QuitGame();
            }
        }
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}