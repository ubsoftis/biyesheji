using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseClick : MonoBehaviour, IStencilClickable
{
    public bool isClicked = false;
    public GameObject activate;
    public GameObject activate2;
    public GameObject activate3;

    [Header("音效（可选）")]
    public AudioClip clickSfx;
    [Tooltip("Sfx 子标签，留空则仅用总 Sfx 音量")]
    public string sfxTag = "";

    public void OnStencilClick()
    {
        Debug.Log("通过 stencil 点击到了：" + gameObject.name);
        PlayClickSfxIfConfigured();
        isClicked = true;
        activate.SetActive(true);
        activate2.SetActive(true);
        activate3.SetActive(true);
    }

    void PlayClickSfxIfConfigured()
    {
        if (clickSfx == null || AudioManager.Instance == null)
            return;
        string tag = string.IsNullOrWhiteSpace(sfxTag) ? null : sfxTag.Trim();
        AudioManager.Instance.PlaySfx2D(clickSfx, tag);
    }
}
