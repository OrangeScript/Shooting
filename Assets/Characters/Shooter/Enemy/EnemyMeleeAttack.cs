using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Enemy))]
public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("伤害")]
    [SerializeField] private int damage = 2;

    [Header("攻击范围")]
    [SerializeField] private float attackRangeX = 1.2f;
    [SerializeField] private float attackRangeZ = 2.5f;

    [Header("攻击时间")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDuration = 1f;
    [SerializeField] private float hitDelay = 0.4f;

    [Header("动画")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackState =
        "Base Layer.Attack";
    [SerializeField] private string walkState =
        "Base Layer.Walk";

    private Enemy enemy;
    private float nextAttackTime;

    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        IsAttacking = false;

        // 刚生成时先留出一点反应时间
        nextAttackTime = Time.time + 0.5f;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsAttacking = false;
    }

    public bool IsInRange(Transform player)
    {
        if (player == null)
            return false;

        Vector3 delta = player.position - transform.position;

        // 俯视/前进游戏：在 XZ 平面判断近战范围
        return Mathf.Abs(delta.x) <= attackRangeX &&
               Mathf.Abs(delta.z) <= attackRangeZ;
    }

    public bool TryAttack(Transform player)
    {
        if (!enemy.IsAlive ||
            IsAttacking ||
            Time.time < nextAttackTime ||
            !IsInRange(player))
            return false;

        // ShooterPlayerHealth playerHealth =
        //     player.GetComponent<ShooterPlayerHealth>();

        // if (playerHealth == null || playerHealth.IsDead)
        //     return false;

        StartCoroutine(AttackRoutine(player));
        return true;
    }

    private IEnumerator AttackRoutine(Transform player)
    {
        IsAttacking = true;

        float duration = Mathf.Max(0.01f, attackDuration);
        float actualHitDelay = Mathf.Clamp(
            hitDelay, 0f, duration
        );

        nextAttackTime =
            Time.time + Mathf.Max(duration, attackCooldown);

        if (animator != null)
            animator.Play(attackState, 0, 0f);

        // 攻击前摇：这时候玩家还没有受到伤害
        yield return new WaitForSeconds(actualHitDelay);

        // Boss 如果在前摇期间死亡，就取消攻击
        if (!enemy.IsAlive)
        {
            IsAttacking = false;
            yield break;
        }

        // 命中时重新检测位置：允许玩家躲避攻击
        if (IsInRange(player))
        {
            // ShooterPlayerHealth health =
            //     player.GetComponent<ShooterPlayerHealth>();

            // if (health != null && !health.IsDead)
            // {
            //     health.TakeDamage(damage);
            //     Debug.Log("MiniBoss 近战命中玩家！");
            // }
        }
        else
        {
            Debug.Log("MiniBoss 攻击落空！");
        }

        // 等待本次攻击动作结束
        yield return new WaitForSeconds(
            duration - actualHitDelay
        );

        IsAttacking = false;

        // 不允许死亡动画被 Walk 覆盖
        if (enemy.IsAlive && animator != null)
            animator.Play(walkState, 0, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(
                attackRangeX * 2f,
                1f,
                attackRangeZ * 2f
            )
        );
    }
}
