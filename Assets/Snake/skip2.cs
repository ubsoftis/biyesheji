using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class skip2 : MonoBehaviour
{
    // Start is called before the first frame update
    public int sceneIndex = 2; // 在Inspector里设置要切换到的场景索引

    void OnMouseDown()
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
