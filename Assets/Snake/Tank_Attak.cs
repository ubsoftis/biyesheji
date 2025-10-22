using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank_Attack : MonoBehaviour {

    public GameObject shellPrefab;     //子弹预制体
    public KeyCode fireKey = KeyCode.Space;//发射子弹键盘按键
    public float shellSpeed = 10;      //子弹发射速度

    private Transform firePoint;       //发射点的Transform组件

    // Use this for initialization
    void Start () {
        firePoint = transform.Find("FirePoint"); //找到发射点
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(fireKey)) { //空格键按下后
            GameObject go = GameObject.Instantiate(shellPrefab, firePoint.position, firePoint.rotation) as GameObject;//在发射点位置实例化子弹
            go.GetComponent<Rigidbody>().velocity = go.transform.forward*shellSpeed;                                   //获得子弹刚体组件设置速度
        }
    }
}