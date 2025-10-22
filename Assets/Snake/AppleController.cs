using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppleController : MonoBehaviour
{
    public GameObject snake;

    public void OnTriggerEnter(Collider col)
    {
        // 只处理蛇头
        if (col.name == "head")
        {
            // 让蛇增长
            if (snake != null)
            {
                snake.GetComponent<SnakeController>().getApple();
            }
            else
            {
                Debug.LogError("AppleController的snake未赋值！");
            }
            // 随机生成新的苹果位置（确保在场地内）
            int x = Random.Range(-11, 11);
            int z = Random.Range(-8, 8);
            transform.position = new Vector3(x + 1f, 5, z + 1f);
        }
        // 检测是否是蛇身体克隆体触发碰撞（可根据实际需求完善逻辑）
        else if (col.name == "head(Clone)") 
        {
            snake.GetComponent<SnakeController>().getApple();
            // 随机生成新的苹果 x 坐标
            int x = Random.Range(11, -11); 
            // 随机生成新的苹果 z 坐标
            int z = Random.Range(8, -8); 
            // 设置苹果新位置
            transform.position = new Vector3(x + 1f, 5, z + 1f); 
        }
    }
}
