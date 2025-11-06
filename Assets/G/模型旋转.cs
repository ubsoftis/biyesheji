using UnityEngine;

public class 模型旋转 : MonoBehaviour
{
    public float 旋转速度 = 100f;

    void OnMouseDrag()
    {
        float rotX = Input.GetAxis("Mouse X") * 旋转速度 * Mathf.Deg2Rad;
        float rotY = Input.GetAxis("Mouse Y") * 旋转速度 * Mathf.Deg2Rad;

        // 根据鼠标拖动方向旋转模型
        transform.Rotate(Vector3.up, -rotX, Space.World);
        transform.Rotate(Vector3.right, rotY, Space.World);
    }
}
