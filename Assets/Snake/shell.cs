using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shell : MonoBehaviour
{
    void OnCollisionEnter(Collision collision) {
    Tank_Health tank = collision.gameObject.GetComponent<Tank_Health>();
    if (tank != null) {
        tank.TakeDamage();
    }
    Destroy(this.gameObject); // 子弹销毁
}
    
}
