using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FloatingSword : CompanionAgent
{
    [Header("近战判定：AttackPoint放在会随动画移动的剑刃中段")]
    [SerializeField] private Transform attackPoint;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.75f;

    [Header("动画")]
    [SerializeField] private string idleState = "Base Layer.Idle";
    [SerializeField] private string attackState = "Base Layer.Attack";
    [Tooltip("只处理无法进入攻击状态的配置错误，不中断正在播放的攻击。")]
    [SerializeField, Min(0.1f)] private float attackTimeout = 1.5f;

    [Header("剑光：手动配置TrailRenderer后拖入，可留空")]
    [SerializeField] private TrailRenderer swordTrail;

    private Animator animator;
    private int idleHash;
    private int attackStateHash;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private bool swinging;
    private bool enteredAttack;
    private bool windowStarted;
    private bool hitWindowOpen;
    private bool closeRequested;
    private bool finishRequested;
    private bool hasPreviousPoint;
    private float timeoutAt;
    private Vector3 previousPoint;

    private readonly HashSet<Enemy> hitEnemies = new();
    private Collider[] hitboxes = new Collider[16];
    private ShooterAttackContext attack;
    private IProjectileHitAction[] actions = System.Array.Empty<IProjectileHitAction>();

    protected override bool LockMovement => swinging;
    protected override bool LockTarget => swinging;

    protected override void OnInitialize()
    {
        animator = GetComponent<Animator>();
        idleHash = Animator.StringToHash(idleState);
        attackStateHash = Animator.StringToHash(attackState);
        if (swordTrail != null)
        {
            swordTrail.emitting = false;
            swordTrail.Clear();
        }

        if (attackPoint == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("FloatingSword需要AttackPoint和Animator Controller", this);
            enabled = false;
        }
    }

    protected override void TickAttack()
    {
        if (swinging)
        {
            if (!animator.isActiveAndEnabled)
            {
                EndSwing();
                return;
            }
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool transitioning = animator.IsInTransition(0);
            bool playingAttack = state.fullPathHash == attackStateHash ||
                (transitioning && animator.GetNextAnimatorStateInfo(0).fullPathHash == attackStateHash);

            if (playingAttack)
                enteredAttack = true;
            else if (enteredAttack && !transitioning && state.fullPathHash == idleHash)
                SwingFinished(); // 漏配结束事件时，等Animator实际回到Idle才释放锁定。
            else if (!enteredAttack && Time.time >= timeoutAt)
            {
                Debug.LogWarning("悬浮剑未进入Attack，请检查状态名称及Attack触发器连线。", this);
                EndSwing();
            }
            return;
        }

        if (!ReadyToAttack || !animator.isActiveAndEnabled || animator.IsInTransition(0) ||
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash != idleHash)
            return;

        Vector3 direction = Target.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);

        attack = CreateAttack(ShooterAttackSource.SwordPet);
        actions = Manager.Pipeline.Capture(attack.Source);
        hitEnemies.Clear();
        windowStarted = hitWindowOpen = closeRequested = finishRequested = false;
        hasPreviousPoint = enteredAttack = false;
        swinging = true;
        timeoutAt = Time.time + attackTimeout;

        StartCooldown();
        animator.SetTrigger(AttackHash);
    }

    // 动画事件：剑刃开始横扫时开启判定和剑光，每次挥砍只开启一次。
    public void HitFrame()
    {
        if (!swinging || windowStarted)
            return;
        windowStarted = hitWindowOpen = true;
        hasPreviousPoint = false;
        if (swordTrail != null)
        {
            swordTrail.Clear();
            swordTrail.emitting = true;
        }
        AudioManager.Play(SoundEffect.Sword);
    }

    // 动画事件：横扫结束。最后一次采样留到LateUpdate，读取本帧最终动画姿势。
    public void EndHitFrame()
    {
        if (!swinging)
            return;
        closeRequested = true;
        if (swordTrail != null)
            swordTrail.emitting = false;
    }

    // 动画事件：整段攻击动画结束，只释放状态，不强行切换Animator。
    public void SwingFinished()
    {
        if (!swinging)
            return;
        EndHitFrame();
        finishRequested = true;
    }

    private void LateUpdate()
    {
        if (!swinging)
            return;
        if (Manager == null || !Manager.CanAct || attackPoint == null)
        {
            EndSwing();
            return;
        }

        if (hitWindowOpen)
        {
            Vector3 point = attackPoint.position;
            Sweep(hasPreviousPoint ? previousPoint : point, point);
            previousPoint = point;
            hasPreviousPoint = true;
        }
        if (closeRequested)
            hitWindowOpen = false;
        if (finishRequested)
            EndSwing();
    }

    private void Sweep(Vector3 from, Vector3 to)
    {
        Physics.SyncTransforms();
        int count;
        do
        {
            count = Physics.OverlapCapsuleNonAlloc(from, to, Mathf.Max(0.01f, hitRadius),
                hitboxes, EnemyMask, QueryTriggerInteraction.Collide);
            if (count < hitboxes.Length)
                break;
            System.Array.Resize(ref hitboxes, hitboxes.Length * 2);
        } while (true);

        // 保留本次攻击快照，某个目标死亡触发其他效果时也不改变伤害来源。
        ShooterAttackContext swingAttack = attack;
        IProjectileHitAction[] swingActions = actions;
        for (int i = 0; i < count; i++)
        {
            if (!swinging || Manager == null || !Manager.CanAct)
                break;
            Collider hitbox = hitboxes[i];
            if (hitbox == null)
                continue;
            Enemy enemy = hitbox.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive || !hitEnemies.Add(enemy))
                continue;

            enemy.TakeDamage(swingAttack);
            foreach (IProjectileHitAction action in swingActions)
                action?.OnHit(swingAttack, enemy, EnemyMask);
        }
    }

    private void EndSwing()
    {
        swinging = hitWindowOpen = windowStarted = enteredAttack = false;
        closeRequested = finishRequested = hasPreviousPoint = false;
        hitEnemies.Clear();
        actions = System.Array.Empty<IProjectileHitAction>();
        if (swordTrail != null)
            swordTrail.emitting = false; // 正常收招保留残影，按Trail.Time自然消失。
        if (animator != null)
            animator.ResetTrigger(AttackHash);
        StartCooldown();
    }

    protected override void OnAgentDisabled()
    {
        EndSwing();
        if (swordTrail != null)
            swordTrail.Clear();
        // 组件被主动禁用才复位姿势；目标死亡、正常收招均不会强切Idle。
        if (animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled)
            animator.Play(idleHash, 0, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        Gizmos.color = hitWindowOpen ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, Mathf.Max(0.01f, hitRadius));
    }
}
