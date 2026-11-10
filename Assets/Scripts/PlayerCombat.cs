
using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float attackRadius = 0.9f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private Transform attackPoint;

    [SerializeField] private Animator animator;
    
    private float nextAttackTime;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
            animator.SetTrigger("Attack");

        
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = attackPoint.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRadius);
    }

    internal void OnAttackHitFrame()
    {
        Vector3 attackCenter = attackPoint.position;

        Collider[] hits = Physics.OverlapSphere(
            attackCenter,
            attackRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        HashSet<PlayerHealth> damaged = new();

        foreach (Collider hit in hits)
        {
            PlayerHealth target =
                hit.GetComponentInParent<PlayerHealth>();

            if (target == null)
                continue;

            if (target.gameObject == gameObject)
                continue;

            if (!damaged.Add(target))
                continue;

            target.TakeDamage(damage);
        }
    }
}
