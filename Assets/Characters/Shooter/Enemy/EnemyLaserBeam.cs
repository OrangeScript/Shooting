using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyLaserBeam : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Transform emitter;

    [Header("光束")]
    [SerializeField, Min(0.1f)] private float length = 16f;
    [SerializeField, Min(0.01f)] private float width = 0.45f;

    [SerializeField] private LaserBeamVisual visual;
    [Header("伤害")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.05f)] private float damageInterval = 0.35f;

    [Header("可选：阻挡激光的墙体层")]
    [SerializeField] private LayerMask obstacleMask;

    private Enemy enemy;
    private readonly Collider[] hits = new Collider[8];

    private Vector3 start;
    private Vector3 end;
    private bool prepared;
    private bool firing;
    private float nextDamageAt;

    private bool Ready => emitter != null &&
                            visual != null &&
                            visual.isActiveAndEnabled;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (emitter == null || visual == null || playerMask.value == 0)
        {
            Debug.LogError("请配置Emitter、Visual和Player Mask。", this);
            enabled = false;
            return;
        }

        StopBeam();
    }

    private void OnEnable()
    {
        enemy.Spawned += StopBeam;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.Spawned -= StopBeam;

        StopBeam();
    }

    public void ShowWarning()
    {
        if (!Ready)
            return;

        firing = false;

        // 预警时固定本轮的起点、方向和终点。
        start = emitter.position;
        Vector3 direction = emitter.forward;
        float actualLength = length;

        Physics.SyncTransforms();

        if (obstacleMask.value != 0 &&
            Physics.SphereCast(
                start,
                width * 0.5f,
                direction,
                out RaycastHit hit,
                length,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            actualLength = hit.distance;
        }

        end = start + direction * actualLength;
        prepared = true;
        visual.ShowWarning(start, end, width);
    }

    public void StartFiring()
    {
        if (!Ready || !prepared)
            return;

        firing = true;
        nextDamageAt = 0f;

        visual.StartFiring();
    }

    public void StopBeam()
    {
        prepared = false;
        firing = false;

        if (visual != null)
        visual.Stop();
    }

    

    private void LateUpdate()
    {
        if (!enemy.IsAlive || !Ready)
        {
            StopBeam();
            return;
        }

        if (!firing)
            return;

        Physics.SyncTransforms();

        int count = Physics.OverlapCapsuleNonAlloc(
            start,
            end,
            width * 0.5f,
            hits,
            playerMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null)
                continue;

            ShooterPlayerHealth health =
                hits[i].GetComponentInParent<ShooterPlayerHealth>();

            if (health == null || health.IsDead)
                continue;

            if (Time.time >= nextDamageAt)
            {
                // 闪避成功也消耗本次伤害间隔。
                nextDamageAt = Time.time + damageInterval;
                health.TakeDamage(enemy.Stats.ScaleDamage(damage));
            }

            // 当前Game_01是单角色，多个碰撞体也只结算一次。
            break;
        }
    }
}
