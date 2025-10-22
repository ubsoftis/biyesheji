using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SnakeController : MonoBehaviour
{
    public GameObject bodyPrefab;
    public GameObject head;
    // 蛇初始长度等相关变量（根据实际逻辑调整）
    private int length;
    // 方向向量定义
    private Vector3 up = new Vector3(0, 0, 1);
    private Vector3 down = new Vector3(0, 0, -1);
    private Vector3 left = new Vector3(-1, 0, 0);
    private Vector3 right = new Vector3(1, 0, 0);
    private Vector3 direction;
    // 计时器与阈值，控制蛇移动速度
    public float timer;
    public float threshold;

    private List<Vector3> positions = new List<Vector3>(); // 记录head和每个body的目标位置
    public List<GameObject> bodies = new List<GameObject>(); // 记录所有body对象

    // Start is called before the first frame update
    void Start()
    {
        length = 3;
        direction = up;
        timer = 0;
        positions.Clear();
        bodies.Clear();

        // 记录head初始位置
        positions.Add(head.transform.position);

        // 生成body
        for (int i = 0; i < length; i++)
        {
            GameObject body = Instantiate(bodyPrefab, transform);
            Vector3 pos = head.transform.position - direction * (i + 1);
            body.transform.position = pos;
            bodies.Add(body);
            positions.Add(pos);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(bodies.Count>=15){
            SceneManager.LoadScene(3);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            direction = up;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            direction = down;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            direction = left;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            direction = right;
        }

        if (timer > threshold)
        {
            // 计算head新位置
            Vector3 newHeadPos = head.transform.position + direction;
            positions.Insert(0, newHeadPos);
            positions.RemoveAt(positions.Count - 1);

            // head移动
            head.transform.position = positions[0];

            // 每个body跟随positions
            for (int n = 0; n < bodies.Count; n++)
            {
                bodies[n].transform.position = positions[n + 1];
            }

            timer = 0;
        }
        timer += Time.deltaTime;
        
    }

    // 吃到苹果时调用的方法，用于蛇身体增长等逻辑
    public void getApple()
    {
        GameObject body = Instantiate(bodyPrefab, transform);
        Vector3 lastPos = positions[positions.Count - 1];
        body.transform.position = lastPos;
        bodies.Add(body);
        positions.Add(lastPos);

        if (threshold > 0.1f)
        {
            threshold -= 0.05f;
        }
    }

    }


