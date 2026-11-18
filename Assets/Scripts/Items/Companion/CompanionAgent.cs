using UnityEngine;

public abstract class CompanionAgent : MonoBehaviour
{
    [Header("移动与索敌")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField, Min(0.1f)] private float moveSpeed = 7f;
    [SerializeField, Min(0.1f)] private float stopDistance = 3f;
    [SerializeField, Min(1f)] private float turnSpeed = 540f;
    [SerializeField, Min(0.05f)] private float searchInterval = 0.2f;

    [Header("基础攻击")]
    [SerializeField, Min(0.1f)] private float attackRange = 6f;
    [SerializeField, Min(0.01f)] protected float attackInterval = 0.4f;
    [SerializeField, Min(1)] private int baseDamage = 2;
    [SerializeField, Range(0f, 1f)] private float damageInheritance = 0.5f;

    protected ShooterCompanionManager Manager { get; private set; }
    protected Enemy Target { get; private set; }
    protected Collider TargetCollider { get; private set; }
    protected LayerMask EnemyMask => enemyMask;

    protected virtual bool LockMovement => false;
    protected virtual bool LockTarget => false;

    private Collider[] searchHits = new Collider[16];
    private int slot;
    private int slotCount = 1;
    private float nextSearchTime;
    private float nextAttackTime;

    public void Initialize(ShooterCompanionManager manager)
    {
        Manager = manager;
        nextSearchTime = Time.time + Random.Range(0f, searchInterval);
        nextAttackTime = Time.time + Random.Range(0f, attackInterval);
        OnInitialize();
    }

    public void SetSlot(int index, int count)
    {
        slot = index;
        slotCount = Mathf.Max(1, count);
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnTargetLost() { }
    protected virtual void OnAgentDisabled() { }
    protected abstract void TickAttack();

    private void OnDisable()
    {
        OnAgentDisabled();
    }

    private void Update()
    {
        if (Manager == null || !Manager.CanAct)
            return;

        if (!LockTarget && !IsValidTarget(Target))
        {
            OnTargetLost();
            Target = null;
            TargetCollider = null;
        }

        if (!LockTarget && Target == null && Time.time >= nextSearchTime)
        {
            nextSearchTime =
                Time.time + Mathf.Max(0.05f, searchInterval);

            FindTarget();
        }

        Vector3 anchor =
            Manager.GetFollowPosition(slot, slotCount);

        if (!LockMovement)
        {
            Vector3 goal = anchor;

            if (Target != null)
            {
                Vector3 away = anchor - Target.transform.position;
                away.y = 0f;

                if (away.sqrMagnitude < 0.001f)
                    away = Vector3.back;

                goal = Target.transform.position
                    + away.normalized * stopDistance;
            }

            goal = Manager.ClampPosition(goal);

            transform.position = Manager.ClampPosition(
                Vector3.MoveTowards(
                    transform.position,
                    goal,
                    moveSpeed * Time.deltaTime
                )
            );

            Vector3 look = Target != null
                ? Target.transform.position - transform.position
                : goal - transform.position;

            look.y = 0f;

            if (look.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(look),
                    turnSpeed * Time.deltaTime
                );
            }
        }

        TickAttack();
    }

    protected bool ReadyToAttack =>
        Target != null &&
        Time.time >= nextAttackTime &&
        FlatSqrDistance(transform.position, Target.transform.position)
            <= attackRange * attackRange;

    protected void StartCooldown()
    {
        nextAttackTime =
            Time.time + Mathf.Max(0.01f, attackInterval);
    }

    protected ShooterAttackContext CreateAttack(
        ShooterAttackSource source)
    {
        return new ShooterAttackContext
        {
            Owner = Manager.Owner,
            Source = source,
            Damage = Manager.Stats.CalculateDamage(
                baseDamage,
                damageInheritance
            )
        };
    }

    protected Vector3 AimPosition =>
        TargetCollider != null && TargetCollider.enabled
            ? TargetCollider.bounds.center
            : Target.transform.position + Vector3.up;

    private bool IsValidTarget(Enemy enemy)
    {
        return enemy != null &&
            enemy.isActiveAndEnabled &&
            enemy.IsAlive &&
            FlatSqrDistance(
                enemy.transform.position,
                Manager.transform.position
            ) <= Manager.LeashRadius * Manager.LeashRadius;
    }

    private void FindTarget()
    {
        Physics.SyncTransforms();

        int count;

        while (true)
        {
            count = Physics.OverlapSphereNonAlloc(
                Manager.transform.position,
                Manager.LeashRadius,
                searchHits,
                enemyMask,
                QueryTriggerInteraction.Collide
            );

            if (count < searchHits.Length)
                break;

            System.Array.Resize(
                ref searchHits,
                searchHits.Length * 2
            );
        }

        float nearest = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider hit = searchHits[i];
            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (!IsValidTarget(enemy))
                continue;

            float distance = FlatSqrDistance(
                transform.position,
                enemy.transform.position
            );

            if (distance >= nearest)
                continue;

            nearest = distance;
            Target = enemy;
            TargetCollider = hit;
        }
    }

    private static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        Vector3 offset = a - b;
        offset.y = 0f;
        return offset.sqrMagnitude;
    }
}
