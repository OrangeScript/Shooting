
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private TrailRenderer trail;

    private BulletPool pool;
    private float remainingLifetime;

    [SerializeField] private int damage = 1;

    [Header("命中检测")]
    [SerializeField] private LayerMask enemyMask;


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

        float distance = speed * Time.deltaTime;
        Vector3 direction = transform.forward;

        // 只检测 Enemy Layer，包含 Trigger Collider
        if (Physics.Raycast(
            transform.position,
            direction,
            out RaycastHit hit,
            distance,
            enemyMask,
            QueryTriggerInteraction.Collide))
        {
            Enemy enemy =
                hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
                enemy.TakeDamage(damage);

            pool.Release(this);
            return;
        }
        transform.position += direction* distance;

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            pool.Release(this);
        }
    }
}
