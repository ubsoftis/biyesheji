using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class raycastButton : MonoBehaviour
{
    [Header("射线检测参数")]
    [Tooltip("检测距离，超过此距离不检测")]
    public float maxDistance = 100f;
    
    [Tooltip("检测层级")]
    public LayerMask layerMask = -1;
    
    [Header("调试选项")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = false;
    
    private Camera mainCamera;

    void Start()
    {
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("未找到摄像机！请确保场景中有摄像机。");
            }
        }
    }

    void Update()
    {
        // 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            if (showDebugInfo)
                Debug.Log("检测到鼠标点击！");

            if (mainCamera == null)
            {
                Debug.LogError("摄像机未初始化！");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // 只检测第一个命中的物体
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, maxDistance, layerMask))
            {
                if (showDebugInfo)
                    Debug.Log("射线未命中任何物体！");
                return;
            }

            Collider col = hit.collider;
            if (col == null)
            {
                if (showDebugInfo)
                    Debug.Log("命中的物体没有 Collider！");
                return;
            }

            // 检查 Collider 是否被禁用
            if (!col.enabled)
            {
                if (showDebugInfo)
                    Debug.Log($"Collider 被禁用: {col.gameObject.name}");
                return;
            }

            // 检查物体是否被禁用
            if (!col.gameObject.activeInHierarchy)
            {
                if (showDebugInfo)
                    Debug.Log($"物体未激活: {col.gameObject.name}");
                return;
            }

            string hitTag = col.tag;

            if (showDebugInfo)
                Debug.Log($"命中物体: {col.gameObject.name}, Tag: {hitTag}");

            // 根据 tag 调用对应方法（并打印）
            switch (hitTag)
            {
                case "button1":
                    OnButton1Hit();
                    break;
                case "button2":
                    OnButton2Hit();
                    break;
                case "button3":
                    OnButton3Hit();
                    break;
                case "button4":
                    OnButton4Hit();
                    break;
                default:
                    if (showDebugInfo)
                        Debug.Log($"命中的物体 Tag 不是目标按钮: {hitTag}");
                    break;
            }
        }
    }

    // 命中 button1 的方法
    void OnButton1Hit()
    {
        Debug.Log("命中了 button1");
    }

    // 命中 button2 的方法
    void OnButton2Hit()
    {
        Debug.Log("命中了 button2");
    }

    // 命中 button3 的方法
    void OnButton3Hit()
    {
        Debug.Log("命中了 button3");
    }

    // 命中 button4 的方法
    void OnButton4Hit()
    {
        Debug.Log("命中了 button4");
    }
}
