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

    [Header("SAttack 前冲与击退")]
    [Tooltip("每段命中前的前冲距离，设为 0 关闭。")]
    [SerializeField, Min(0f)] private float sAttackLungeDistance = 0.45f;
    [SerializeField, Min(0.01f)] private float sAttackLungeDuration = 0.15f;
    [SerializeField, Min(0f)] private float sAttackKnockbackDistance = 0.4f;
    [SerializeField, Min(0f)] private float sAttackFinisherKnockbackDistance = 1f;
    [SerializeField, Min(0.01f)] private float sAttackKnockbackDuration = 0.18f;

    [SerializeField] private NetworkAnimator networkAnimator;

    private readonly AttackSequence serverAttack = new();
    private readonly HashSet<PlayerHealth> damagedTargets = new();
    private PlayerHealth health;
    private PlayerInventory inventory;
    private PlayerController movement;
    private Animator animator;
    private AnimationClip normalAttackClip;
    private AnimationClip sAttackClip;
    private float nextLocalAttackTime;
    private readonly float[] sAttackHitTimes = { -1f, -1f, -1f };
    private readonly List<AnimatorClipInfo> clipInfos = new();
    private bool lungePlanActive;
    private bool lungeAnimationStarted;
    private int completedLunges;
    private float lungePlanExpiresAt;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerController>();
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
        CancelAttackMovement();
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

        if (sAttackClip == null)
            return;

        foreach (AnimationEvent animationEvent in sAttackClip.events)
        {
            switch (animationEvent.functionName)
            {
                case nameof(PlayerAnimationEvents.SAttack_1): sAttackHitTimes[0] = animationEvent.time; break;
                case nameof(PlayerAnimationEvents.SAttack_2): sAttackHitTimes[1] = animationEvent.time; break;
                case nameof(PlayerAnimationEvents.SAttack_3): sAttackHitTimes[2] = animationEvent.time; break;
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

        if (movement != null && movement.IsKnockedBack)
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

        if (movement != null && movement.IsKnockedBack)
        {
            UpdateAttackCooldownClientRpc(Mathf.Max(0.1f, serverAttack.ReadyAt - Time.time), type, false);
            return;
        }

        float duration = GetAttackDuration(type);
        if (!serverAttack.TryBegin(type, Time.time, GetAttackCooldown(type), duration))
        {
            UpdateAttackCooldownClientRpc(Mathf.Max(0.05f, serverAttack.ReadyAt - Time.time), type, false);
            return;
        }

        UpdateAttackCooldownClientRpc(serverAttack.ReadyAt - Time.time, type, true);
        networkAnimator.SetTrigger(trigger);
    }

    [ClientRpc]
    private void UpdateAttackCooldownClientRpc(float remainingCooldown, CombatAttackType type, bool accepted)
    {
        if (!IsSpawned || !IsOwner)
            return;

        nextLocalAttackTime = Time.time + Mathf.Max(0f, remainingCooldown);
        if (!accepted)
            return;

        CancelAttackMovement();
        if (type == CombatAttackType.SAttack && sAttackClip != null && sAttackLungeDistance > 0f)
        {
            lungePlanActive = true;
            lungePlanExpiresAt = Time.time + remainingCooldown + 1f;
        }
    }

    // 由控制器在本帧唯一一次 Move 前调用，按已配置的命中事件提前前冲。
    internal void UpdateAttackMovement(float deltaTime)
    {
        if (!lungePlanActive)
            return;

        if (!IsSpawned || !IsOwner || health == null || health.IsDead || movement == null ||
            movement.IsKnockedBack || Time.time > lungePlanExpiresAt)
        {
            CancelAttackMovement();
            return;
        }

        if (!TryGetSAttackPlayback(out float clipTime, out float playbackRate))
        {
            if (lungeAnimationStarted)
                CancelAttackMovement();
            return;
        }

        lungeAnimationStarted = true;
        for (int stage = 0; stage < sAttackHitTimes.Length; stage++)
        {
            int bit = 1 << stage;
            if ((completedLunges & bit) != 0 || sAttackHitTimes[stage] < 0f)
                continue;

            float timeToHit = (sAttackHitTimes[stage] - clipTime) / playbackRate;
            if (timeToHit <= 0f)
            {
                completedLunges |= bit;
                continue;
            }
            float windowStart = stage > 0 ? Mathf.Max(0f, sAttackHitTimes[stage - 1]) : 0f;
            if (clipTime < windowStart)
                continue;
            float stageDuration = Mathf.Min(sAttackLungeDuration,
                Mathf.Max(0.01f, (sAttackHitTimes[stage] - windowStart) / playbackRate));
            if (timeToHit > stageDuration + Mathf.Max(0f, deltaTime))
                continue;

            completedLunges |= bit;
            movement.BeginAttackLunge(transform.forward, sAttackLungeDistance, Mathf.Max(0.01f, timeToHit));
        }
    }

    private bool TryGetSAttackPlayback(out float clipTime, out float playbackRate)
    {
        clipTime = 0f;
        playbackRate = 1f;
        if (animator == null || !animator.isActiveAndEnabled || !animator.isInitialized || sAttackClip == null)
            return false;

        AnimatorStateInfo state;
        if (animator.IsInTransition(0))
        {
            state = animator.GetNextAnimatorStateInfo(0);
            animator.GetNextAnimatorClipInfo(0, clipInfos);
        }
        else
        {
            state = animator.GetCurrentAnimatorStateInfo(0);
            animator.GetCurrentAnimatorClipInfo(0, clipInfos);
        }
        // 离开攻击状态时立即取消，不在退出混合期间继续前冲。
        if (!ContainsSAttackClip())
            return false;

        playbackRate = state.speed * state.speedMultiplier * animator.speed;
        clipTime = state.normalizedTime * sAttackClip.length;
        return playbackRate > 0f;
    }

    private bool ContainsSAttackClip()
    {
        foreach (AnimatorClipInfo info in clipInfos)
        {
            if (info.clip == sAttackClip && info.weight > 0f)
                return true;
        }
        return false;
    }

    private void CancelAttackMovement()
    {
        lungePlanActive = false;
        lungeAnimationStarted = false;
        completedLunges = 0;
        lungePlanExpiresAt = 0f;
        movement?.CancelAttackLunge();
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

        if (movement != null && movement.IsKnockedBack)
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
            if (type == CombatAttackType.SAttack)
            {
                Vector3 pushDirection = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
                if (pushDirection.sqrMagnitude < 0.001f)
                    pushDirection = transform.forward;

                float distance = hitIndex == 3 ? sAttackFinisherKnockbackDistance : sAttackKnockbackDistance;
                target.GetComponent<PlayerController>()?.ServerApplyKnockback(
                    pushDirection, distance, sAttackKnockbackDuration);
            }
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
