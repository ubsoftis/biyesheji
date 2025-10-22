using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class 摄像机控制器 : MonoBehaviour
{
    public GameObject 目标对象;
    public float 距离;
    [Range(0.1f, 20f)] public float 鼠标滚轮灵敏度 = 4.0f;
    public float 最小距离 = 4f;
    public float 最大距离 = 10f;

    public float X速度;
    public float Y速度;
    private float x;
    private float y;
    float 当前X轴转动幅度 = 0;
    float 当前Y轴移动幅度 = 0;
    private float 鼠标X轴滑动的力度 = 0;
    private float 鼠标Y轴滑动的力度 = 0;

    public float Y方向最小角度限制 = 0;
    public float Y方向最大角度限制 = 0;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void LateUpdate()
    {
        if (Input.GetMouseButton(1))
        {
            鼠标X轴滑动的力度 = (Input.GetAxis("Mouse X") * X速度 - 鼠标X轴滑动的力度) * Time.deltaTime * 10;
            鼠标Y轴滑动的力度 = (Input.GetAxis("Mouse Y") * Y速度 - 鼠标Y轴滑动的力度) * Time.deltaTime * 10;
        }
        else
        {
            鼠标X轴滑动的力度 -= 鼠标X轴滑动的力度 * Time.deltaTime * 4;
            鼠标Y轴滑动的力度 -= 鼠标Y轴滑动的力度 * Time.deltaTime * 4;
        }

        当前X轴转动幅度 += (鼠标X轴滑动的力度 - 当前X轴转动幅度) * Time.deltaTime * 20;
        当前Y轴移动幅度 += (鼠标Y轴滑动的力度 - 当前Y轴移动幅度) * Time.deltaTime * 20;

        x += 当前X轴转动幅度;
        y -= 当前Y轴移动幅度;

        y = 角度限制(y, Y方向最小角度限制, Y方向最大角度限制);

        距离 -= Input.GetAxis("Mouse ScrollWheel") * 鼠标滚轮灵敏度;
        距离 = Mathf.Clamp(距离, 最小距离, 最大距离);


        Quaternion rotation = Quaternion.Euler(y, x, 0);
        transform.rotation = rotation;
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -距离) + 目标对象.transform.position;
        transform.position = position;

    }

    float 角度限制(float 角度, float 最小值, float 最大值)
    {
        if (角度 < -360)
            角度 += 360;
        if (角度 > 360)
            角度 -= 360;
        return Mathf.Clamp(角度, 最小值, 最大值);
    }
}
