using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class camera : MonoBehaviour
{public Transform target;      // 需要跟随的目标
    public Vector3 offset = new Vector3(0, 10, -10); // 摄像机与目标的偏移
    public float smoothSpeed = 5f; // 跟随平滑速度

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 如果你想让摄像机始终看着目标，可以加上这一行
        transform.LookAt(target);
    }
   
}
