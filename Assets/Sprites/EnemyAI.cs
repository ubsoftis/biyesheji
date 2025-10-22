using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 5f;           // 移动速度
    public float angularSpeed = 30f;   // 旋转速度
    public float attackInterval = 1f;  // 攻击间隔
    public GameObject shellPrefab;     // 子弹预制体
    public Transform firePoint;        // 发射点

    private Rigidbody rigidbody;
    private Transform targetTank;
    private float lastAttackTime = 0f;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        // 查找场景中第一个带有"Tank"标签的物体作为目标
        GameObject playerTank = GameObject.FindGameObjectWithTag("Tank");
        if (playerTank != null)
            targetTank = playerTank.transform;
    }

    void FixedUpdate()
    {
        if (targetTank == null) return;

        // 计算方向
        Vector3 dir = (targetTank.position - transform.position).normalized;
        dir.y = 0; // 保持在水平面

        // 旋转朝向目标
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.fixedDeltaTime);

        // 前进
        rigidbody.velocity = transform.forward * speed;
    }

    void Update()
    {
        if (targetTank == null) return;

        // 攻击间隔判断
        if (Time.time - lastAttackTime > attackInterval)
        {
            lastAttackTime = Time.time;
            Attack();
        }
    }

    void Attack()
    {
        if (shellPrefab != null && firePoint != null)
        {
            GameObject go = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
            Rigidbody shellRb = go.GetComponent<Rigidbody>();
            if (shellRb != null)
            {
                shellRb.velocity = firePoint.forward * 10f; // 你可以调整子弹速度
            }
        }
    }
}