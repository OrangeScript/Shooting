using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyRushMovement : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 10f;

    private Enemy enemy;
    private ShooterPlayerHealth target;
    private bool stopped;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        enemy.Spawned += ResetForSpawn;
    }

    private void OnDisable()
    {
        enemy.Spawned -= ResetForSpawn;
    }

    private void ResetForSpawn()
    {
        stopped = false;

        if (target == null)
            target = FindObjectOfType<ShooterPlayerHealth>();
    }

    private void Update()
    {
        if (!enemy.IsAlive || stopped ||
            target == null || target.IsDead)
            return;

        Vector3 destination = target.transform.position;
        destination.y = transform.position.y;

        Vector3 direction = destination - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            speed * Time.deltaTime
        );
    }

    // 供Inspector事件或其他能力调用。
    public void StopMoving()
    {
        stopped = true;
    }
}
