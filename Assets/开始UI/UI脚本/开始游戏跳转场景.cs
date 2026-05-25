using UnityEngine;

public class ClickSignLoadScene : MonoBehaviour
{
    public int targetSceneIndex;

    [Header("音效（可选，走 AudioManager 总 Sfx）")]
    [Tooltip("点开始淡出时播一次；不拖则不播")]
    public AudioClip clickSfx;
    [Range(0f, 2f)]
    public float sfxVolumeScale = 1f;

    bool _isLoading;

    void OnEnable()
    {
        _isLoading = false;
    }

    void Update()
    {
        if (UILockSignManager.ExistsInActiveScene() && UILockSignManager.uiIsOpen)
            return;

        if (_isLoading || !Input.GetMouseButtonDown(0))
            return;

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Transform hitT = hit.collider.transform;
        if (hitT != transform && !hitT.IsChildOf(transform))
            return;

        _isLoading = true;
        PlayClickSfx();
        GlobalSceneTransition.LoadSceneByBuildIndex(targetSceneIndex);
    }

    void PlayClickSfx()
    {
        if (clickSfx == null || AudioManager.Instance == null)
            return;
        AudioManager.Instance.PlaySfx2D(clickSfx, null, sfxVolumeScale);
    }
}
