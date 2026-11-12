using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float attackRadius = 0.9f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private Transform attackPoint;

    [Header("SAttack")]
    [SerializeField, Min(0f)] private float sAttackCooldown = 4f;
    [Tooltip("每一段伤害 = (基础伤害 + 道具加成) × 此倍率。")]
    [SerializeField, Min(0f)] private float sAttackDamageMultiplier = 1f;
    [Tooltip("正常情况下自动读取带 SAttack_1 事件的动画长度；未找到时使用此时长。")]
    [SerializeField, Min(0.01f)] private float sAttackFallbackDuration = 4f;

    [SerializeField] private NetworkAnimator networkAnimator;

    private readonly AttackSequence serverAttack = new();
    private readonly HashSet<PlayerHealth> damagedTargets = new();
    private PlayerHealth health;
    private PlayerInventory inventory;
    private Animator animator;
    private AnimationClip normalAttackClip;
    private AnimationClip sAttackClip;
    private float nextLocalAttackTime;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
        if (networkAnimator == null)
            networkAnimator = GetComponentInChildren<NetworkAnimator>();

        animator = networkAnimator != null && networkAnimator.Animator != null
            ? networkAnimator.Animator
            : GetComponentInChildren<Animator>();
        CacheAttackClips();
    }

    public override void OnNetworkSpawn()
    {
        ResetAttackState();
    }

    public override void OnNetworkDespawn()
    {
        ResetAttackState();
    }

    private void ResetAttackState()
    {
        nextLocalAttackTime = 0f;
        serverAttack.Reset();
        damagedTargets.Clear();
    }

    private void CacheAttackClips()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            foreach (AnimationEvent animationEvent in clip.events)
            {
                if (animationEvent.functionName == nameof(PlayerAnimationEvents.OnThrowHit))
                    normalAttackClip = clip;
                else if (animationEvent.functionName == nameof(PlayerAnimationEvents.SAttack_1))
                    sAttackClip = clip;
            }
        }
    }

    public void TrySAttack()
    {
        TryStartAttack(CombatAttackType.SAttack);
    }

    public void TryAttack()
    {
        TryStartAttack(CombatAttackType.Normal);
    }

    private void TryStartAttack(CombatAttackType type)
    {
        if (!IsSpawned || !IsOwner || health == null || health.IsDead)
            return;

        if (Time.time < nextLocalAttackTime)
            return;

        if (networkAnimator == null || animator == null || attackPoint == null)
        {
            Debug.LogError("攻击需要 NetworkAnimator、Animator 和 Attack Point。", this);
            return;
        }

        if (animator.speed <= 0f)
            return;

        string trigger = type == CombatAttackType.SAttack ? "SAttack" : "Attack";
        if (!HasTrigger(trigger))
        {
            Debug.LogError($"Animator 缺少 Trigger 参数 {trigger}，请检查并保存 Animator Controller。", this);
            return;
        }

        float duration = GetAttackDuration(type);
        nextLocalAttackTime = Time.time + Mathf.Max(GetAttackCooldown(type), duration);

        // 服务器接受后才同步播放，拒绝的请求不会只播动作却没有伤害。
        RequestAttackServerRpc(type);
    }

    private bool HasTrigger(string trigger)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == trigger && parameter.type == AnimatorControllerParameterType.Trigger)
                return true;
        }
        return false;
    }

    private float GetAttackCooldown(CombatAttackType type)
    {
        return Mathf.Max(0f, type == CombatAttackType.SAttack ? sAttackCooldown : attackCooldown);
    }

    private float GetAttackDuration(CombatAttackType type)
    {
        AnimationClip clip = type == CombatAttackType.SAttack ? sAttackClip : normalAttackClip;
        float fallback = type == CombatAttackType.SAttack ? sAttackFallbackDuration : 1.5f;
        float duration = clip != null ? clip.length : Mathf.Max(0.01f, fallback);
        float playbackSpeed = animator != null ? Mathf.Max(0.01f, animator.speed) : 1f;

        // 留出过渡时间，避免上一段动画尚未退出就接受下一次攻击。
        return duration / playbackSpeed + 0.25f;
    }

    [ServerRpc]
    private void RequestAttackServerRpc(CombatAttackType type)
    {
        if (!IsSpawned || health == null || health.IsDead || attackPoint == null ||
            networkAnimator == null || animator == null || animator.speed <= 0f)
            return;

        string trigger = type == CombatAttackType.SAttack ? "SAttack" : "Attack";
        if (!HasTrigger(trigger))
            return;

        float duration = GetAttackDuration(type);
        if (!serverAttack.TryBegin(type, Time.time, GetAttackCooldown(type), duration))
        {
            UpdateAttackCooldownClientRpc(Mathf.Max(0.05f, serverAttack.ReadyAt - Time.time));
            return;
        }

        UpdateAttackCooldownClientRpc(serverAttack.ReadyAt - Time.time);
        networkAnimator.SetTrigger(trigger);
    }

    [ClientRpc]
    private void UpdateAttackCooldownClientRpc(float remainingCooldown)
    {
        if (IsSpawned && IsOwner)
            nextLocalAttackTime = Time.time + Mathf.Max(0f, remainingCooldown);
    }

    public void OnAttackHitFrame()
    {
        ResolveHit(CombatAttackType.Normal, 1, 1f);
    }

    public void OnSAttackHitFrame(int hitIndex)
    {
        ResolveHit(CombatAttackType.SAttack, hitIndex, sAttackDamageMultiplier);
    }

    private void ResolveHit(CombatAttackType type, int hitIndex, float damageMultiplier)
    {
        if (!IsSpawned || !IsServer || health == null || health.IsDead || attackPoint == null)
            return;

        if (!serverAttack.TryConsume(type, hitIndex, Time.time))
            return;

        int finalDamage = Mathf.RoundToInt(
            (damage + (inventory != null ? inventory.BonusDamage : 0)) * Mathf.Max(0f, damageMultiplier));
        if (finalDamage <= 0)
            return;

        Physics.SyncTransforms();
        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position, attackRadius, ~0, QueryTriggerInteraction.Collide);

        // 每一段分别去重；同一目标可以被后续段再次命中。
        damagedTargets.Clear();
        foreach (Collider hit in hits)
        {
            PlayerHealth target = hit.GetComponentInParent<PlayerHealth>();
            if (target == null || target == health || target.IsDead || !damagedTargets.Add(target))
                continue;

            target.TakeDamage(finalDamage);
            Debug.Log($"Player {OwnerClientId} {type} 第 {hitIndex} 段命中 Player {target.OwnerClientId}，伤害 {finalDamage}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
