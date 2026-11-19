using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class ShooterCoopPlayer : NetworkBehaviour
{
    private readonly NetworkVariable<float> horizontalInput = new(
        0f, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<Vector2Int> healthState = new();
    private GamerController movement;
    private ShooterPlayerHealth health;
    private AutoShooter shooter;
    private PlayerInput input;
    private BulletPool bullets;

    public ShooterPlayerHealth Health => health;

    private void Awake()
    {
        movement = GetComponent<GamerController>();
        health = GetComponent<ShooterPlayerHealth>();
        shooter = GetComponent<AutoShooter>();
        input = GetComponent<PlayerInput>();
        movement.enabled = false;
        shooter.enabled = false;
        health.enabled = false;
        input.enabled = false;
    }

    public void ConfigureServer(BulletPool pool, float centerX, float halfWidth)
    {
        bullets = pool;
        movement.SetLaneBounds(centerX, halfWidth);
        shooter.BindBulletPool(pool);
    }

    public override void OnNetworkSpawn()
    {
        ShooterCoopRun run = ShooterCoopRun.Active;
        if (IsServer)
        {
            health.HealthChanged += PublishHealth;
            shooter.ShotFired += PublishShot;
            PublishHealth(health.CurrentHealth, health.MaxHealth);
            movement.enabled = true;
            shooter.enabled = true;
            health.enabled = true;
        }
        else
        {
            bullets = run != null ? run.Bullets : null;
            GetComponent<CharacterController>().enabled = false;
            healthState.OnValueChanged += OnHealthStateChanged;
            ApplyHealth(healthState.Value, false);
        }

        if (IsOwner && IsClient)
        {
            movement.MoveInputChanged += SendInput;
            input.enabled = true;
            run?.BindLocalPlayer(this);
        }
    }

    private void SendInput(float value)
    {
        if (IsSpawned && IsOwner)
            horizontalInput.Value = value;
    }

    private void Update()
    {
        if (IsSpawned && IsServer)
            movement.SetHorizontalInput(horizontalInput.Value);
    }

    private void PublishHealth(int current, int maximum)
    {
        healthState.Value = new Vector2Int(current, maximum);
    }

    private void OnHealthStateChanged(Vector2Int previous, Vector2Int next)
    {
        ApplyHealth(next, IsOwner && previous.y > 0);
    }

    private void ApplyHealth(Vector2Int next, bool playFeedback)
    {
        if (next.y > 0)
            health.ApplyReplicatedHealth(next.x, next.y, playFeedback);
    }

    private void PublishShot(Vector3 position, Quaternion rotation, float speed)
    {
        ShowShotClientRpc(position, rotation, speed);
    }

    [ClientRpc]
    private void ShowShotClientRpc(Vector3 position, Quaternion rotation, float speed)
    {
        if (IsServer || bullets == null)
            return;

        shooter.PlayShotFeedback();
        bullets.Spawn(position, rotation, new ShooterAttackContext
        {
            ProjectileSpeedMultiplier = speed
        }, null, visualOnly: true);
    }

    public void StopPlaying()
    {
        input.enabled = false;
        movement.enabled = false;
        shooter.enabled = false;
        health.enabled = false;
    }

    public override void OnNetworkDespawn()
    {
        health.HealthChanged -= PublishHealth;
        healthState.OnValueChanged -= OnHealthStateChanged;
        shooter.ShotFired -= PublishShot;
        movement.MoveInputChanged -= SendInput;
        StopPlaying();
    }
}
