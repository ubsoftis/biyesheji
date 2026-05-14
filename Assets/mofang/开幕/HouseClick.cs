using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseClick : MonoBehaviour, IStencilClickable
{
    public bool isClicked = false;
    public GameObject activate;
    public GameObject activate2;
    public GameObject activate3;
    public void OnStencilClick()
    {
        Debug.Log("通过 stencil 点击到了：" + gameObject.name);
        isClicked = true;
        activate.SetActive(true);
        activate2.SetActive(true);
        activate3.SetActive(true);
    }
}
