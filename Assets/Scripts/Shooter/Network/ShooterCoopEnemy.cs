using System;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject), typeof(Enemy), typeof(EnemyForwardMovement))]
public sealed class ShooterCoopEnemy : NetworkBehaviour
{
    private readonly NetworkVariable<Vector2Int> healthState = new();
    private Enemy enemy;
    private EnemyForwardMovement movement;
    private EnemyAnimation animationDriver;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        movement = GetComponent<EnemyForwardMovement>();
        animationDriver = GetComponent<EnemyAnimation>();
    }

    public void Initialize(EnemyStats stats, Func<Vector3, ShooterPlayerHealth> selectTarget)
    {
        movement.SelectEndTarget = selectTarget;
        enemy.Spawn(null, transform.position, transform.rotation, stats: stats);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            enemy.Damaged += OnDamaged;
            enemy.Killed += OnKilled;
            enemy.Recycled += OnRecycled;
            PublishHealth();
            return;
        }

        movement.enabled = false;
        foreach (Collider hitbox in GetComponentsInChildren<Collider>(true))
            hitbox.enabled = false;
        healthState.OnValueChanged += OnReplicaChanged;
        OnReplicaChanged(default, healthState.Value);
    }

    private void OnReplicaChanged(Vector2Int previous, Vector2Int next)
    {
        if (next.y <= 0)
            return;

        enemy.ApplyReplicatedHealth(next.x, next.y);
        if (previous.y == 0 && next.x > 0)
            animationDriver?.PlayWalk();
        if (previous.x > next.x)
        {
            AudioManager.Play(SoundEffect.EnemyHit);
            if (next.x == 0)
            {
                animationDriver?.PlayDie();
                AudioManager.Play(SoundEffect.EnemyDeath);
            }
        }
    }

    private void OnDamaged(int _) => PublishHealth();
    private void OnKilled(EnemyKillContext _) => PublishHealth();

    private void PublishHealth()
    {
        healthState.Value = new Vector2Int(enemy.CurrentHealth, enemy.MaxHealth);
    }

    private void OnRecycled()
    {
        if (IsSpawned)
            NetworkObject.Despawn();
    }

    public override void OnNetworkDespawn()
    {
        enemy.Damaged -= OnDamaged;
        enemy.Killed -= OnKilled;
        enemy.Recycled -= OnRecycled;
        healthState.OnValueChanged -= OnReplicaChanged;
        EnemyHealthUI.Instance?.Unregister(enemy);
    }
}
