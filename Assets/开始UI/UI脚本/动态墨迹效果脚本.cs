using UnityEngine;
using UnityEngine.UI;

public class InkFlowControl : MonoBehaviour
{
    [Header("流动速度")]
    public float flowSpeedX = 0.02f;
    public float flowSpeedY = 0.01f;

    [Header("透明度呼吸")]
    public bool enableBreathing = true;
    public float alphaMin = 0.6f;
    public float alphaMax = 1f;
    public float breathSpeed = 0.3f;

    private Material material;

    void Start()
    {
        // 获取材质实例（避免修改原材质）
        Image image = GetComponent<Image>();
        material = Instantiate(image.material);
        image.material = material;
    }

    void Update()
    {
        if (material == null) return;

        // 设置流动速度
        material.SetFloat("_FlowSpeedX", flowSpeedX);
        material.SetFloat("_FlowSpeedY", flowSpeedY);

        // 透明度呼吸效果
        if (enableBreathing)
        {
            float alphaNormalized = (Mathf.Sin(Time.time * breathSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(alphaMin, alphaMax, alphaNormalized);
            material.SetFloat("_Alpha", alpha);
        }
    }

    void OnDestroy()
    {
        // 清理材质
        if (material != null)
        {
            Destroy(material);
        }
    }
}