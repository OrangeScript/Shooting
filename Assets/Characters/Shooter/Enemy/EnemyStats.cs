using UnityEngine;

// 波次在出生时提供倍数，各能力继续使用预制体上的基础值。
public readonly struct EnemyStats
{
    private readonly float healthMultiplier;
    private readonly float moveMultiplier;
    private readonly float damageMultiplier;
    private readonly float attackRateMultiplier;

    public EnemyStats(float health, float move, float damage, float attackRate)
    {
        healthMultiplier = health;
        moveMultiplier = move;
        damageMultiplier = damage;
        attackRateMultiplier = attackRate;
    }

    public int ScaleHealth(int baseHealth) =>
        Mathf.Max(1, Mathf.RoundToInt(baseHealth * Multiplier(healthMultiplier)));

    public float ScaleMoveSpeed(float baseSpeed) => baseSpeed * Multiplier(moveMultiplier);

    public int ScaleDamage(int baseDamage) => baseDamage <= 0
        ? 0
        : Mathf.Max(1, Mathf.RoundToInt(baseDamage * Multiplier(damageMultiplier)));

    public float ScaleCooldown(float seconds) => seconds / Multiplier(attackRateMultiplier);

    private static float Multiplier(float value) => value > 0f ? value : 1f;
}
