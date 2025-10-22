using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class musicMAnager : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject); // 让这个物体切换场景时不被销毁
    }
}
