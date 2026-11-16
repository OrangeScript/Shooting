using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoShooter : MonoBehaviour
{
     [Header("References")]
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private Transform firePoint;

    [Header("Settings")]
    [SerializeField] private float fireInterval = 0.2f;
    [SerializeField] private ParticleSystem muzzleFlash;


    private float nextFireTime;

    private ShooterPlayerHealth shooterHealth;

    [Header("基础子弹伤害")]
    [SerializeField, Min(1)]
    private int baseDamage = 1;

    private ShooterCombatStats combatStats;
    private ShooterCombatPipeline combatPipeline;

    private void Awake()
    {
        shooterHealth = GetComponent<ShooterPlayerHealth>();
        combatStats = GetComponent<ShooterCombatStats>();
        combatPipeline = GetComponent<ShooterCombatPipeline>();
    }

    private void Update()
    {
        if (shooterHealth != null && shooterHealth.IsDead)
            return;
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval);

        Fire();
    }

    private void Fire()
    {
        if (bulletPool == null || firePoint == null)
            return;

        ShooterAttackContext attack = new ShooterAttackContext
        {
            Owner = shooterHealth,
            Source = ShooterAttackSource.PlayerBullet,

            Damage = combatStats != null
                ? combatStats.CalculateDamage(baseDamage)
                : baseDamage
        };

        IProjectileHitAction[] actions =
            combatPipeline != null
                ? combatPipeline.Capture(attack.Source)
                : System.Array.Empty<IProjectileHitAction>();
        


        bulletPool.Spawn(
            firePoint.position,
            firePoint.rotation,
            attack,
            actions
        );
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            muzzleFlash.Play(true);
        }
    }
}
