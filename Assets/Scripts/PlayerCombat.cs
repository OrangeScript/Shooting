
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float attackRadius = 0.9f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private Transform attackPoint;

    [SerializeField] private NetworkAnimator networkAnimator;
    private PlayerHealth health;
    private float nextLocalAttackTime;
    private float nextAttackTime;
    private float nextServerAttackTime;
    private float serverAttackExpireTime;
    private bool serverAttackPending;

    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
        networkAnimator = GetComponentInChildren<NetworkAnimator>();
    }

    public void TryAttack()
    {
        if (!IsSpawned || !IsOwner) return;
        if (health.IsDead) return;
        if (Time.time < nextLocalAttackTime) return;

        if (networkAnimator == null)
        {
            Debug.LogError("缺少 NetworkAnimator");
            return;
        }

        nextLocalAttackTime = Time.time + attackCooldown;

        // 服务器登记本次攻击，用于命中帧验证
        RequestAttackServerRpc();

        // 本地立即播放，并同步给其他网络实例
        networkAnimator.SetTrigger("Attack");
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = attackPoint.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRadius);
    }
 [ServerRpc]
    private void RequestAttackServerRpc()
    {
        if (health.IsDead) return;

        if (Time.time < nextServerAttackTime)
            return;

        // 一个攻击周期只允许一次命中
        if (serverAttackPending &&
            Time.time <= serverAttackExpireTime)
            return;

        nextServerAttackTime = Time.time + attackCooldown;
        serverAttackExpireTime = Time.time + 1.5f;

        serverAttackPending = true;
    }

    // 仍然由原本的 Animation Event 调用
    public void OnAttackHitFrame()
    {
        // 客户端只播放动画，服务器负责判定
        if (!IsSpawned || !IsServer) return;
        if (!serverAttackPending) return;

        if (Time.time > serverAttackExpireTime)
        {
            serverAttackPending = false;
            return;
        }

        // 只消费一次本次攻击
        serverAttackPending = false;

        if (health.IsDead || attackPoint == null)
            return;

        // 确保最新 Transform 反映到物理查询中
        Physics.SyncTransforms();

        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            attackRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        HashSet<PlayerHealth> damaged = new();

        foreach (Collider hit in hits)
        {
            PlayerHealth target =
                hit.GetComponentInParent<PlayerHealth>();

            if (target == null) continue;
            if (target == health) continue;
            if (target.IsDead) continue;
            if (!damaged.Add(target)) continue;
            int finalDamage = damage +
             (inventory != null ? inventory.BonusDamage : 0);

            target.TakeDamage(finalDamage);

            Debug.Log(
                $"Player {OwnerClientId} 命中 Player " +
                $"{target.OwnerClientId}，伤害 {finalDamage}"
            );
        }
    }
}
