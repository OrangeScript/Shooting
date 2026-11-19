using System;
using System.Collections.Generic;
using UnityEngine;

// 百分比使用小数：0.15 表示 +15%；闪避使用概率百分点。
[Serializable]
public struct ShooterStatModifiers : IEquatable<ShooterStatModifiers>
{
    [Tooltip("每层增加的整数攻击力，允许负数；最终伤害最低为1。")]
    public int damage;
    [Tooltip("每层生命上限修正；上限变化会等量调整当前生命，存活时最低保留1点。")]
    public int maxHealth;
    [Tooltip("每层攻速加法修正：0.12为+12%，-0.15为-15%。")]
    public float attackSpeed;
    [Tooltip("每层主角子弹速度加法修正：0.2为+20%。")]
    public float projectileSpeed;
    [Tooltip("每层角色移速加法修正：0.1为+10%。")]
    public float moveSpeed;
    [Tooltip("每次击杀的回血量修正。0.05 表示累计 0.05 点生命。")]
    public float killHeal;
    [Tooltip("闪避概率修正：0.04为+4个百分点；总概率最高65%。")]
    public float dodge;
    [Tooltip("每层每秒恢复的HP，可为小数；小数会累计。")]
    public float regeneration;

    public ShooterStatModifiers Scaled(int stacks)
    {
        return new ShooterStatModifiers
        {
            damage = damage * stacks,
            maxHealth = maxHealth * stacks,
            attackSpeed = attackSpeed * stacks,
            projectileSpeed = projectileSpeed * stacks,
            moveSpeed = moveSpeed * stacks,
            killHeal = killHeal * stacks,
            dodge = dodge * stacks,
            regeneration = regeneration * stacks
        };
    }

    public static ShooterStatModifiers operator +(
        ShooterStatModifiers left, ShooterStatModifiers right)
    {
        return new ShooterStatModifiers
        {
            damage = left.damage + right.damage,
            maxHealth = left.maxHealth + right.maxHealth,
            attackSpeed = left.attackSpeed + right.attackSpeed,
            projectileSpeed = left.projectileSpeed + right.projectileSpeed,
            moveSpeed = left.moveSpeed + right.moveSpeed,
            killHeal = left.killHeal + right.killHeal,
            dodge = left.dodge + right.dodge,
            regeneration = left.regeneration + right.regeneration
        };
    }

    public bool Equals(ShooterStatModifiers other)
    {
        return damage == other.damage && maxHealth == other.maxHealth &&
            attackSpeed.Equals(other.attackSpeed) &&
            projectileSpeed.Equals(other.projectileSpeed) &&
            moveSpeed.Equals(other.moveSpeed) && killHeal.Equals(other.killHeal) &&
            dodge.Equals(other.dodge) && regeneration.Equals(other.regeneration);
    }
}

[DisallowMultipleComponent]
public class ShooterCombatStats : MonoBehaviour
{
    private readonly Dictionary<object, int> damageBonuses = new();
    private readonly Dictionary<object, ShooterStatModifiers> modifiers = new();

    public const float MinAttackSpeedMultiplier = 0.25f;
    public const float MaxAttackSpeedMultiplier = 4f;
    public const float MinProjectileSpeedMultiplier = 0.3f;
    public const float MaxProjectileSpeedMultiplier = 3f;
    public const float MinMoveSpeedMultiplier = 0.35f;
    public const float MaxMoveSpeedMultiplier = 2.5f;
    public const float MaxDodgeChance = 0.65f;
    public const float MaxKillHealPerEnemy = 1f;
    public const float MaxRegeneration = 5f;

    public ShooterStatModifiers Totals { get; private set; }
    public int DamageBonus => Totals.damage;
    public float AttackSpeedMultiplier => Mathf.Clamp(1f + Totals.attackSpeed,
        MinAttackSpeedMultiplier, MaxAttackSpeedMultiplier);
    public float ProjectileSpeedMultiplier => Mathf.Clamp(1f + Totals.projectileSpeed,
        MinProjectileSpeedMultiplier, MaxProjectileSpeedMultiplier);
    public float MoveSpeedMultiplier => Mathf.Clamp(1f + Totals.moveSpeed,
        MinMoveSpeedMultiplier, MaxMoveSpeedMultiplier);
    public float DodgeChance => Mathf.Clamp(Totals.dodge, 0f, MaxDodgeChance);
    public float KillHealPerEnemy => Mathf.Clamp(Totals.killHeal, 0f, MaxKillHealPerEnemy);
    public float Regeneration => Mathf.Clamp(Totals.regeneration, 0f, MaxRegeneration);

    public event Action Changed;

    public void SetModifiers(object source, ShooterStatModifiers value)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        modifiers[source] = value;
        Recalculate();
    }

    public void RemoveModifiers(object source)
    {
        if (source != null && modifiers.Remove(source))
            Recalculate();
    }

    // 兼容已有的强化子弹及宠物伤害继承。
    public void SetDamageBonus(object source, int amount)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        damageBonuses[source] = amount;
        Recalculate();
    }

    public void RemoveDamageBonus(object source)
    {
        if (source != null && damageBonuses.Remove(source))
            Recalculate();
    }

    public int CalculateDamage(int baseDamage, float inheritedBonus = 1f)
    {
        int bonus = Mathf.RoundToInt(DamageBonus * Mathf.Max(0f, inheritedBonus));
        return Mathf.Max(1, baseDamage + bonus);
    }

    private void Recalculate()
    {
        ShooterStatModifiers total = default;
        foreach (ShooterStatModifiers value in modifiers.Values)
            total += value;
        foreach (int value in damageBonuses.Values)
            total.damage += value;

        if (Totals.Equals(total))
            return;

        Totals = total;
        Changed?.Invoke();
    }
}
