
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float hitRadius = 0.12f;
    [SerializeField] private int damage = 1;

    [Header("只检测玩家 Layer")]
    [SerializeField] private LayerMask playerMask;

    private Vector3 direction;
    private float remainingLifetime;
    private bool launched;

    public void Launch(Vector3 shootDirection)
    {
        direction = shootDirection.normalized;
        remainingLifetime = lifetime;
        launched = true;
    }

    private void Update()
    {
        if (!launched)
            return;

        float distance = speed * Time.deltaTime;

        // 使用 SphereCast，让敌方子弹具有一定命中半径
        if (Physics.SphereCast(
            transform.position,
            hitRadius,
            direction,
            out RaycastHit hit,
            distance,
            playerMask,
            QueryTriggerInteraction.Collide))
        {
            // ShooterPlayerHealth health =
            //     hit.collider.GetComponentInParent<ShooterPlayerHealth>();

            // if (health != null)
            //     health.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        transform.position += direction * distance;

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
            Destroy(gameObject);
    }
}
