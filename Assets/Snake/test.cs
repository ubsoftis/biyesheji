using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class test : MonoBehaviour
{
    public GameObject cubegameObject;
    private void Update()
    {
        if (Input.GetKey(KeyCode.C))
        {
            cubegameObject.SetActive(true);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("11111111");
            cubegameObject.SetActive(false);
        }
    }
}
