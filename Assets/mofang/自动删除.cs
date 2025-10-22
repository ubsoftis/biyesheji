using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 自动删除 : MonoBehaviour
{
    public float 删除时间;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        删除时间 -= Time.deltaTime;
        if (删除时间 <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}
