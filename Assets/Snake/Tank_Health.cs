using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank_Health : MonoBehaviour {

    public int hp = 100;
    public GameObject shellPrefab;

    // Use this for initialization
    void Start () {

    }

    // Update is called once per frame
    void Update () {

    }

    public void TakeDamage() {
        if (hp <= 0) {
            return;
        }
        hp -= Random.Range(10, 20);
        if (hp <= 0) { // 收到伤害之后 血量为0 控制死亡效果
            GameObject.Instantiate (shellPrefab, transform.position + Vector3.up, transform.rotation);
            GameObject.Destroy (this.gameObject);
        }
    }
}