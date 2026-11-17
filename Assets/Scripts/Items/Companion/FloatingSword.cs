using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FloatingSword : CompanionAgent
{
    [Header("近战判定")]
    [SerializeField] private Transform attackPoint;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.75f;

    [Header("动画")]
    [SerializeField] private string idleState = "Base Layer.Idle";
    [SerializeField, Min(0.1f)] private float attackTimeout = 1.5f;

    private Animator animator;
    private int idleHash;
    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private bool swinging;
    private bool hitConsumed;
    private float timeoutAt;

    private Enemy swingTarget;
    private Collider swingCollider;
    private ShooterAttackContext attack;

    private IProjectileHitAction[] actions =
        System.Array.Empty<IProjectileHitAction>();

    protected override bool LockMovement => swinging;

    protected override void OnInitialize()
    {
        animator = GetComponent<Animator>();
        idleHash = Animator.StringToHash(idleState);

        if (attackPoint == null ||
            animator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                "FloatingSword需要AttackPoint和Animator Controller",
                this
            );

            enabled = false;
        }
    }

    protected override void TickAttack()
    {
        if (swinging)
        {
            // 缺少结束事件时取消攻击，不自动补伤害。
            if (Time.time >= timeoutAt)
                EndSwing();

            return;
        }

        if (!ReadyToAttack ||
            !animator.isActiveAndEnabled ||
            animator.IsInTransition(0) ||
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash
                != idleHash)
            return;

        swingTarget = Target;
        swingCollider = TargetCollider;

        attack = CreateAttack(ShooterAttackSource.SwordPet);
        actions = Manager.Pipeline.Capture(attack.Source);

        hitConsumed = false;
        swinging = true;
        timeoutAt = Time.time + attackTimeout;

        StartCooldown();
        animator.SetTrigger(AttackHash);
    }

    // 在挥剑动画的命中帧添加同名Animation Event。
    public void HitFrame()
    {
        if (!swinging || hitConsumed)
            return;

        // 先消费，重复事件或挥空都不能再次结算。
        hitConsumed = true;

        if (Manager == null || !Manager.CanAct ||
            swingTarget == null ||
            !swingTarget.isActiveAndEnabled ||
            !swingTarget.IsAlive ||
            swingCollider == null ||
            !swingCollider.enabled ||
            attackPoint == null)
            return;

        Physics.SyncTransforms();

        Vector3 nearest =
            swingCollider.ClosestPoint(attackPoint.position);

        if ((nearest - attackPoint.position).sqrMagnitude
            > hitRadius * hitRadius)
            return;

        Enemy hitTarget = swingTarget;

        hitTarget.TakeDamage(attack);

        // 致命命中也执行命中效果。
        foreach (var action in actions)
            action?.OnHit(attack, hitTarget, EnemyMask);
    }

    // 在挥剑动画末尾添加同名Animation Event。
    public void SwingFinished()
    {
        if (swinging)
            EndSwing();
    }

    private void EndSwing()
    {
        swinging = false;
        hitConsumed = true;
        swingTarget = null;
        swingCollider = null;
        actions = System.Array.Empty<IProjectileHitAction>();

        StartCooldown();

        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.ResetTrigger(AttackHash);
            animator.Play(idleHash, 0, 0f);
        }
    }

    protected override void OnTargetLost()
    {
        if (swinging)
            EndSwing();
    }

    protected override void OnAgentDisabled()
    {
        if (swinging)
            EndSwing();
    }
}