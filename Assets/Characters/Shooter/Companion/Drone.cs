using UnityEngine;

public class Drone : CompanionAgent
{
    [SerializeField] private Transform firePoint;

    protected override void OnInitialize()
    {
        if (firePoint == null || Manager.Bullets == null)
        {
            Debug.LogError("Drone缺少FirePoint或BulletPool", this);
            enabled = false;
        }
    }

    protected override void TickAttack()
    {
        if (!ReadyToAttack)
            return;

        Vector3 direction = AimPosition - firePoint.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        var attack = CreateAttack(
            ShooterAttackSource.DroneBullet
        );

        var actions = Manager.Pipeline.Capture(attack.Source);

        Manager.Bullets.Spawn(
            firePoint.position,
            Quaternion.LookRotation(direction),
            attack,
            actions
        );

        StartCooldown();
    }
}