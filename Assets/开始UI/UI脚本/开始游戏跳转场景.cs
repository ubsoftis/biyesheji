using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ClickSignLoadScene : MonoBehaviour
{
    [Header("拖入全屏黑色遮罩Image")]
    public Image blackMask;
    public int targetSceneIndex;
    public float fadeSpeed = 1f;

    [Header("音效（可选，走 AudioManager 总 Sfx）")]
    [Tooltip("点开始淡出时播一次；不拖则不播")]
    public AudioClip clickSfx;
    [Range(0f, 2f)]
    public float sfxVolumeScale = 1f;

    bool isFading = false;

    void Update()
    {
        if (UILockSignManager.uiIsOpen)
            return;

        if (!isFading && Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null)
                return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Transform hitT = hit.collider.transform;
                if (hitT == transform || hitT.IsChildOf(transform))
                {
                    isFading = true;
                    PlayClickSfx();
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
                SceneManager.LoadScene(targetSceneIndex);
        }
    }

    void PlayClickSfx()
    {
        if (clickSfx == null || AudioManager.Instance == null)
            return;
        AudioManager.Instance.PlaySfx2D(clickSfx, null, sfxVolumeScale);
    }
}
