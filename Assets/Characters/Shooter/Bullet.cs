
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private TrailRenderer trail;

    private BulletPool pool;
    private float remainingLifetime;

    // 每次发射都要重新初始化
    public void Launch(
        BulletPool ownerPool,
        Vector3 position,
        Quaternion rotation)
    {
        pool = ownerPool;

        transform.SetPositionAndRotation(position, rotation);

        // 重置生命周期
        remainingLifetime = lifetime;

        gameObject.SetActive(true);

        // 清除上一次飞行留下的轨迹
        if (trail != null)
            trail.Clear();
    }

    private void Update()
    {
        transform.position +=
            transform.forward * speed * Time.deltaTime;

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            pool.Release(this);
        }
    }
}
