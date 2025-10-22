using UnityEngine;
using UnityEngine.SceneManagement;

public class Tank_Movement : MonoBehaviour {

    public float speed = 5f;      //坦克移动速度
    public float angularSpeed = 30f; //坦克旋转速度

    private Rigidbody rigidbody;

    private float surviveTime = 0f; // 新增：存活时间计时器

    // Use this for initialization
    void Start () {
        rigidbody = GetComponent<Rigidbody>();//获得刚体组件
    }

    void FixedUpdate() {
        float move = 0f;
        float rotate = 0f;

        // W/S 控制前后
        if (Input.GetKey(KeyCode.W))
            move = 1f;
        else if (Input.GetKey(KeyCode.S))
            move = -1f;

        // A/D 控制左右旋转
        if (Input.GetKey(KeyCode.A))
            rotate = -1f;
        else if (Input.GetKey(KeyCode.D))
            rotate = 1f;

        // 施加移动
        rigidbody.velocity = transform.forward * move * speed;

        // 施加旋转
        rigidbody.angularVelocity = transform.up * rotate * angularSpeed;

        // 新增：计时
        surviveTime += Time.fixedDeltaTime;
        if (surviveTime >= 10f) {
            SceneManager.LoadScene(4); // 切换到场景4（索引为4）
        }
    }
}