using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Enemy))]
public class EnemyProximityExplosion : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float triggerDistance = 1.4f;
    [SerializeField, Min(0.1f)] private float explosionRadius = 2f;
    [SerializeField, Min(0.05f)] private float fuseTime = 0.45f;
    [SerializeField, Min(1)] private int damage = 8;

    [SerializeField] private GameObject warningVisual;

    [Header("进入自爆预警时")]
    [SerializeField] private UnityEvent onArming = new UnityEvent();

    private Enemy enemy;
    private ShooterPlayerHealth target;

    private bool arming;
    private bool exploded;
    private float explodeAt;

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
        ShowWarning(false);
    }

    private void ResetForSpawn()
    {
        arming = false;
        exploded = false;
        ShowWarning(false);

        if (target == null)
            target = FindObjectOfType<ShooterPlayerHealth>();
    }

    private void Update()
    {
        if (!enemy.IsAlive || target == null || target.IsDead)
        {
            ShowWarning(false);
            return;
        }

        if (exploded)
            return;

        if (!arming)
        {
            if (FlatDistanceSquared() > triggerDistance * triggerDistance)
                return;

            arming = true;
            explodeAt = Time.time + fuseTime;

            ShowWarning(true);
            onArming.Invoke();
            return;
        }

        if (Time.time >= explodeAt)
            Explode();
    }

    private void Explode()
    {
        if (!enemy.IsAlive || exploded)
            return;

        exploded = true;
        ShowWarning(false);

        target.GetComponent<ExplosionVfxPool>()?.Play(
            transform.position,
            explosionRadius
        );

        if (!target.IsDead &&
            FlatDistanceSquared() <= explosionRadius * explosionRadius)
        {
            target.TakeDamage(damage);
        }

        enemy.Despawn();
    }

    private float FlatDistanceSquared()
    {
        Vector3 difference =
            target.transform.position - transform.position;

        difference.y = 0f;
        return difference.sqrMagnitude;
    }

    private void ShowWarning(bool visible)
    {
        if (warningVisual != null)
            warningVisual.SetActive(visible);
    }
}