
public enum ShooterAttackSource
{
    PlayerBullet,
    DroneBullet,
    MeleeSweep,
    LightningBounce
}

public struct ShooterAttackContext
{
    // 谁拥有这次攻击：用于击杀奖励
    public ShooterPlayerHealth Owner;

    // 本次攻击最终伤害
    public int Damage;

    // 攻击来源
    public ShooterAttackSource Source;

    // 闪电效果
    public float LightningChance;
    public int MaxBounces;
    public float BounceRadius;
}
