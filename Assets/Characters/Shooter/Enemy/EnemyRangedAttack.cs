
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyRangedAttack : MonoBehaviour
{
    [Header("射击")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private float attackRange = 20f;

    [Header("目标")]
    [SerializeField] private float targetHeight = 1f;
    private EnemyAnimation animation;

    private Enemy enemy;
    private Transform player;
    private float timer;

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
        if (!enemy.IsAlive ||
            projectilePrefab == null ||
            firePoint == null)
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
