using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseClick : MonoBehaviour, IStencilClickable
{
    public bool isClicked = false;
    public GameObject activate;
    public void OnStencilClick()
    {
        Debug.Log("通过 stencil 点击到了：" + gameObject.name);
        isClicked = true;
        activate.SetActive(true);
        // 这里写你的逻辑：比如打开UI、触发剧情等
    }
}
