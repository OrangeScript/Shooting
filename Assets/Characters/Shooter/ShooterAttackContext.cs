

using UnityEngine;

public enum ShooterAttackSource
{
    PlayerBullet,
    DroneBullet,
    MeleeSweep,
    LightningBounce,
    DeathExplosion = 4,
    Unknown
}

public struct ShooterAttackContext
{
    // 谁拥有这次攻击：用于击杀奖励
    public ShooterPlayerHealth Owner;

    // 本次攻击最终伤害
    public int Damage;

    // 攻击来源
    public ShooterAttackSource Source;
}

public readonly struct EnemyKillContext
{
    public readonly Vector3 Position;
    public readonly int VictimMaxHealth;
    public readonly ShooterAttackContext Attack;

    public EnemyKillContext(
        Vector3 position,
        int victimMaxHealth,
        ShooterAttackContext attack)
    {
        Position = position;
        VictimMaxHealth = Mathf.Max(1, victimMaxHealth);
        Attack = attack;
    }
}
