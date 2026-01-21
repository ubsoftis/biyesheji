  using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class start : MonoBehaviour
{
    [Header("轴向开关")]
    [Tooltip("允许绕摄像机右轴旋转（垂直拖动）")]
    public bool allowX = true;
    [Tooltip("允许绕世界Y轴旋转（水平拖动）")]
    public bool allowY = true;

    [Header("拖拽旋转参数")]
    [Tooltip("鼠标拖动对旋转的灵敏度")]
    public float rotationSpeed = 200f;

    private bool dragging = false;
    private Vector3 lastMousePosition;

    private void OnMouseDown()
    {
        dragging = true;
        lastMousePosition = Input.mousePosition;
    }

    private void OnMouseDrag()
    {
        if (!dragging) return;

        Vector3 delta = Input.mousePosition - lastMousePosition;

        // 上下拖动让物体绕摄像机右轴转，左右拖动绕上轴转
        float rotX = delta.y * rotationSpeed * Time.deltaTime;
        float rotY = -delta.x * rotationSpeed * Time.deltaTime;

        if (Camera.main != null)
        {
            if (allowX)
                transform.Rotate(Camera.main.transform.right, rotX, Space.World);
            if (allowY)
                transform.Rotate(Vector3.up, rotY, Space.World);
        }
        else
        {
            if (allowX)
                transform.Rotate(Vector3.right, rotX, Space.World);
            if (allowY)
                transform.Rotate(Vector3.up, rotY, Space.World);
        }

        lastMousePosition = Input.mousePosition;
    }

    private void OnMouseUp()
    {
        dragging = false;
    }
}