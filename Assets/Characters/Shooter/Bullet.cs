
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
    [SerializeField, Min(0.001f)] private float overlapRadius = 0.01f;

    private Collider[] overlapHits = new Collider[8];
    private bool inFlight;
    private static int lastPhysicsSyncFrame = -1;

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
        inFlight = true;

        gameObject.SetActive(true);

        // 清除上一次飞行留下的轨迹
        if (trail != null)
            trail.Clear();
    }

    private void LateUpdate()
    {
        if (!inFlight)
            return;

        // 敌人在 Update 中移动，等移动结束后同步；所有子弹每帧共用一次同步。
        if (lastPhysicsSyncFrame != Time.frameCount)
        {
            Physics.SyncTransforms();
            lastPhysicsSyncFrame = Time.frameCount;
        }

        // 射线无法检测起点已经在内部的 Collider，先处理贴身重叠。
        if (TryHitAtOrigin())
            return;

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

            Recycle();
            return;
        }
        transform.position += direction* distance;

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            Recycle();
        }
    }

    private bool TryHitAtOrigin()
    {
        while (true)
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                Mathf.Max(0.001f, overlapRadius),
                overlapHits,
                enemyMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Enemy enemy = overlapHits[i].GetComponentInParent<Enemy>();
                if (enemy == null || !enemy.IsAlive)
                    continue;

                enemy.TakeDamage(damage);
                Recycle();
                return true;
            }

            if (count < overlapHits.Length)
                return false;

            // 极拥挤时扩容，避免结果被截断后漏掉有效目标；平时复用数组。
            System.Array.Resize(ref overlapHits, overlapHits.Length * 2);
        }
    }

    private void Recycle()
    {
        if (!inFlight)
            return;

        inFlight = false;
        pool.Release(this);
    }
}
