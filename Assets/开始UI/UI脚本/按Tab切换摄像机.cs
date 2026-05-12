using UnityEngine;

public class CameraSwitcher : MonoBehaviour  // ← 改这里
{
    public Camera camera1;
    public Camera camera2;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            camera1.enabled = !camera1.enabled;
            camera2.enabled = !camera2.enabled;
        }
    }
}