using UnityEngine;
using NodeCanvas.DialogueTrees.UI.Examples;

/// <summary>
/// 把 NodeCanvas <see cref="DialogueUGUI"/> 的打字音接到 <see cref="AudioManager"/> 的 Sfx（NodeCanvas 程序集不能直接引用本脚本）。
/// 挂在已有 AudioManager 的同物体或任意常驻物体上即可。
/// </summary>
[DefaultExecutionOrder(-450)]
public sealed class DialogueTypingAudioBridge : MonoBehaviour
{
    static int _handlerRefCount;

    void Awake()
    {
        if (_handlerRefCount == 0)
            DialogueUGUI.TypingSoundPlayHandler += PlayThroughAudioManager;
        _handlerRefCount++;
    }

    void OnDestroy()
    {
        _handlerRefCount--;
        if (_handlerRefCount <= 0)
        {
            _handlerRefCount = 0;
            DialogueUGUI.TypingSoundPlayHandler -= PlayThroughAudioManager;
        }
    }

    static void PlayThroughAudioManager(AudioClip clip, float volumeScale)
    {
        if (clip == null)
            return;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx2D(clip, null, volumeScale);
            return;
        }
        // 单独打开某关、DontDestroy 的 AudioManager 尚未进域时：仍能听到预览（不经 Sfx Mixer）
        Vector3 p = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, p, Mathf.Clamp01(volumeScale));
    }
}
