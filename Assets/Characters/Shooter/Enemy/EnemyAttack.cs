using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float fireInterval = 2f;
    private EnemyAnimation animation;

    private Enemy enemy;
    private Transform player;
    private float timer;
    [SerializeField] private float attackRange = 5f;
    // Start is called before the first frame update
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        animation = GetComponent<EnemyAnimation>();
    }

    private void OnEnable()
    {
        // 每次从对象池取出，重置攻击冷却
        timer = Mathf.Max(0.1f, fireInterval);
    }

    private void Update()
    {
        if (!enemy.IsAlive)
            return;

        if (player == null)
        {
            GamerController controller =
                FindObjectOfType<GamerController>();

            if (controller == null)
                return;

            player = controller.transform;
        }

        if (Vector3.Distance(
            transform.position, player.position) > attackRange)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = Mathf.Max(0.1f, fireInterval);

        Fire();
    }

    private void Fire()
    {
        Vector3 targetPosition =
            player.position + Vector3.up * targetHeight;

        Vector3 direction =
            targetPosition - firePoint.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        EnemyProjectile projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );
        animation?.PlayAttack();
        projectile.Launch(direction);
    }
}
