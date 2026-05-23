using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Animations;
using static UnityEditor.Experimental.GraphView.GraphView;

public class 魔方 : MonoBehaviour
{
    int 魔方LayerID;
    public GameObject 被点到的小正方体;
    Vector3 鼠标点到的位置;
    Vector3 鼠标移动的方向;
    Vector3 鼠标点击位置的法线;
    Axis 魔方旋转的轴向 = Axis.None;
    bool 是否是顺时针转动;
    bool 是否正在旋转 = true;

    /// <summary>
    /// 记录魔方的27个区域
    /// </summary>
    小方块[] 记录所有小方块数组;
    GameObject 旋转的点;
    List<小方块> 正在旋转的小正方体列表 = new List<小方块>();


    // Start is called before the first frame update
    void Start()
    {
        魔方LayerID = LayerMask.NameToLayer("mofang");
        记录所有小方块数组 = this.GetComponentsInChildren<小方块>();
        旋转的点 = new GameObject("旋转的点");
        旋转的点.transform.position = Vector3.zero;

    }
    private void 检测鼠标拖动()
    {
        
        if (Input.GetMouseButtonDown(0))
        {
            //从屏幕发出一条射线到鼠标的位置
            Ray 鼠标射线 = Camera.main.ScreenPointToRay(Input.mousePosition);

            //定义一个获取射线目标点物体的变量
            RaycastHit 射线命中的目标;
            Debug.DrawRay(鼠标射线.origin, 鼠标射线.direction * 100f, UnityEngine.Color.blue);

            //从刚才点击屏幕的射线的初始点，发射一条跟刚才射线方向一样的射线，并返回射线目标体的属性
            if (Physics.Raycast(鼠标射线.origin, 鼠标射线.direction, out 射线命中的目标, 100f, 1 << 魔方LayerID))
            {
                //鼠标点到的物体就是射线碰触到的目标物体
                被点到的小正方体 = 射线命中的目标.transform.gameObject;

                //鼠标点击的点就是射线碰触到物体的point
                鼠标点到的位置 = 射线命中的目标.point;

                //获得当前物体的法线
                鼠标点击位置的法线 = 射线命中的目标.normal;
            }
        }
        if (Input.GetMouseButton(0) && 被点到的小正方体 != null)
        {
            //从鼠标点击的位置发射一根射线
            Ray 鼠标拖动时候的射线 = Camera.main.ScreenPointToRay(Input.mousePosition);

            //定义一个获取射线目标点物体的变量
            RaycastHit 拖动射线命中的目标;
            //从刚才点击屏幕的射线的初始点，发射一条跟刚才射线方向一样的射线，并返回射线目标体的属性
            if (Physics.Raycast(鼠标拖动时候的射线.origin, 鼠标拖动时候的射线.direction, out 拖动射线命中的目标, 100f, 1 << 魔方LayerID))
            {
                鼠标移动的方向 = 拖动射线命中的目标.point - 鼠标点到的位置;
                Vector3 叉乘结果 = Vector3.Cross(鼠标点击位置的法线, 鼠标移动的方向).normalized;
                if (鼠标移动的方向.sqrMagnitude > 2f)
                {
                    开始旋转魔方(叉乘结果);
                    被点到的小正方体 = null;

                }

            }

        }

    }
    private void 开始旋转魔方(Vector3 叉乘结果)
    {
        魔方旋转的轴向 = Axis.None;
        float X轴角度 = Vector3.Angle(叉乘结果, transform.right);
        float Y轴角度 = Vector3.Angle(叉乘结果, transform.up);
        float Z轴角度 = Vector3.Angle(叉乘结果, -transform.forward);

        float 拖动角度 = 0;
        if (X轴角度 < 15 || X轴角度 > 165)
        {
            魔方旋转的轴向 = Axis.X;
            拖动角度 = X轴角度;
        }
        else if (Y轴角度 < 15 || Y轴角度 > 165)
        {
            魔方旋转的轴向 = Axis.Y;
            拖动角度 = Y轴角度;
        }
        else if (Z轴角度 < 15 || Z轴角度 > 165)
        {
            魔方旋转的轴向 = Axis.Z;
            拖动角度 = Z轴角度;
        }

        是否是顺时针转动 = 拖动角度 < 15;
        小方块 child = 被点到的小正方体.GetComponent<小方块>();
        if (child != null && 魔方旋转的轴向 != Axis.None)
        {
            switch (魔方旋转的轴向)
            {
                case Axis.X:
                    foreach (var item in 记录所有小方块数组)
                    {
                        if (item.相对于魔方中心的偏移量.x == child.相对于魔方中心的偏移量.x)
                        {
                            item.gameObject.transform.SetParent(旋转的点.transform);
                            正在旋转的小正方体列表.Add(item);
                        }
                    }
                    break;
                case Axis.Y:
                    foreach (var item in 记录所有小方块数组)
                    {
                        if (item.相对于魔方中心的偏移量.y == child.相对于魔方中心的偏移量.y)
                        {
                            item.gameObject.transform.SetParent(旋转的点.transform);
                            正在旋转的小正方体列表.Add(item);
                        }
                    }
                    break;
                case Axis.Z:
                    foreach (var item in 记录所有小方块数组)
                    {
                        if (item.相对于魔方中心的偏移量.z == child.相对于魔方中心的偏移量.z)
                        {
                            item.gameObject.transform.SetParent(旋转的点.transform);
                            正在旋转的小正方体列表.Add(item);
                        }
                    }
                    break;
            }
            是否正在旋转 = true;

        }
    }

    public Vector3 获取魔方旋转轴向的方向向量()
    {
        Vector3 方向向量 = Vector3.zero;
        switch (魔方旋转的轴向)
        {
            case Axis.X:
                方向向量 = transform.right;
                break;
            case Axis.Y:

                方向向量 = transform.up;
                break;
            case Axis.Z:
                方向向量 = -transform.forward;
                break;
        }
        return 方向向量;

    }

    private void FixedUpdate()
    {
        if (是否正在旋转)
        {
            Quaternion rotation = Quaternion.AngleAxis(是否是顺时针转动 ? 90f : -90f, 获取魔方旋转轴向的方向向量());
            Quaternion rot = Quaternion.Slerp(旋转的点.transform.rotation, rotation, 0.08f);
            旋转的点.transform.rotation = rot;
            if (Quaternion.Angle(旋转的点.transform.rotation, rotation) < 0.02f)
            {
                旋转的点.transform.rotation = rotation;

                foreach (var item in 正在旋转的小正方体列表)
                {
                    item.更新偏移量(魔方旋转的轴向, 是否是顺时针转动);
                    item.transform.SetParent(this.transform);
                }
                正在旋转的小正方体列表.Clear();
                旋转的点.transform.rotation = Quaternion.identity;
                是否正在旋转 = false;


            }


        }

    }
    // Update is called once per frame
    void Update()
    {
        if (!是否正在旋转)
        {
            检测鼠标拖动();

        }
    }
}
