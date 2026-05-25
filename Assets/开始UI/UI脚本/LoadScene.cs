using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 通用场景加载：挂到按钮或任意物体，在 Inspector 配目标场景后调用 <see cref="LoadTargetScene"/>。
/// 场景名优先；名为空且 build index >= 0 时按索引加载。
/// 切关黑幕由 <see cref="GlobalSceneTransition"/> 统一处理（约 0.6 秒渐入渐出）。
/// </summary>
public class LoadScene : MonoBehaviour
{
    [Header("目标场景（二选一，场景名优先）")]
    [Tooltip("非空则 LoadScene(名称)。")]
    public string sceneName;

    [Tooltip("当 sceneName 为空且本值 >= 0 时，按 Build Settings 索引加载。")]
    public int sceneBuildIndex = -1;

    [Header("音效（可选，走 AudioManager Sfx）")]
    public AudioClip clickSfx;
    [Tooltip("Sfx 子标签，留空则仅用总 Sfx 音量")]
    public string sfxTag = "";
    [Range(0f, 2f)]
    public float sfxVolumeScale = 1f;

    [Header("交互")]
    [Tooltip("为 true 时，仅当本场景有 UILockSignManager 且 UI 打开时才阻止加载。")]
    public bool blockWhenUiLocked = true;

    bool _isLoading;

    void OnEnable()
    {
        _isLoading = false;
    }

    /// <summary>给 UI Button 的 OnClick 绑定。</summary>
    public void LoadTargetScene()
    {
        if (_isLoading)
            return;
        if (IsBlockedByUiLock())
            return;
        if (!HasValidTarget())
        {
            Debug.LogWarning($"[LoadScene] {name} 未配置有效的 sceneName 或 sceneBuildIndex。", this);
            return;
        }

        PlayClickSfxIfConfigured();
        _isLoading = true;
        LoadNow();
    }

    /// <summary>按场景名加载（可代码调用）。</summary>
    public void LoadByName(string name)
    {
        if (_isLoading || string.IsNullOrEmpty(name))
            return;
        sceneName = name;
        sceneBuildIndex = -1;
        LoadTargetScene();
    }

    /// <summary>按 Build Index 加载（可代码调用）。</summary>
    public void LoadByBuildIndex(int buildIndex)
    {
        if (_isLoading || buildIndex < 0)
            return;
        sceneName = string.Empty;
        sceneBuildIndex = buildIndex;
        LoadTargetScene();
    }

    /// <summary>重新加载当前场景。</summary>
    public void ReloadCurrentScene()
    {
        if (_isLoading)
            return;
        Scene current = SceneManager.GetActiveScene();
        sceneName = current.name;
        sceneBuildIndex = current.buildIndex;
        LoadTargetScene();
    }

    bool HasValidTarget()
    {
        return !string.IsNullOrEmpty(sceneName) || sceneBuildIndex >= 0;
    }

    bool IsBlockedByUiLock()
    {
        return blockWhenUiLocked
            && UILockSignManager.ExistsInActiveScene()
            && UILockSignManager.uiIsOpen;
    }

    void LoadNow()
    {
        if (!string.IsNullOrEmpty(sceneName))
            GlobalSceneTransition.LoadScene(sceneName);
        else
            GlobalSceneTransition.LoadSceneByBuildIndex(sceneBuildIndex);
    }

    void PlayClickSfxIfConfigured()
    {
        if (clickSfx == null || AudioManager.Instance == null)
            return;
        string tag = string.IsNullOrWhiteSpace(sfxTag) ? null : sfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clickSfx, tag, sfxVolumeScale);
    }
}
