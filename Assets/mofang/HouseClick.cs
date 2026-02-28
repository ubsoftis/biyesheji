using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseClick : MonoBehaviour, IStencilClickable
{
    public void OnStencilClick()
    {
        Debug.Log("通过 stencil 点击到了：" + gameObject.name);
        // 这里写你的逻辑：比如打开UI、触发剧情等
    }
}
