
using UnityEngine;

public class ItemFloating : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 60f;

    [Header("Floating")]
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 initialPosition;
    private float phase;

    private void Start()
    {
        initialPosition = transform.localPosition;

        // 给每个道具一个不同的初始相位
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        // 绕本地 Y 轴持续旋转
        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );

        // 在原始高度附近上下浮动
        float offset = Mathf.Sin(
            Time.time * floatSpeed + phase
        ) * floatHeight;

        transform.localPosition =
            initialPosition + Vector3.up * offset;
    }
}
