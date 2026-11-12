using System;

internal enum CombatAttackType : byte
{
    None,
    Normal,
    SAttack
}

// 独立记录服务器认可的攻击周期，不依赖 Animator 或客户端命中请求。
internal sealed class AttackSequence
{
    private CombatAttackType attackType;
    private float startedAt;
    private float expiresAt;
    private int consumedHits;

    public float ReadyAt { get; private set; }

    public bool TryBegin(CombatAttackType type, float now, float cooldown,
        float duration, float eventGrace = 0.5f)
    {
        if (type != CombatAttackType.Normal && type != CombatAttackType.SAttack)
            return false;

        if (now < ReadyAt)
            return false;

        duration = Math.Max(0.01f, duration);
        attackType = type;
        startedAt = now;
        expiresAt = now + duration + Math.Max(0f, eventGrace);
        ReadyAt = now + Math.Max(Math.Max(0f, cooldown), duration);
        consumedHits = 0;
        return true;
    }

    public bool TryConsume(CombatAttackType type, int hitIndex, float now)
    {
        if (attackType == CombatAttackType.None || type != attackType ||
            now < startedAt || now > expiresAt)
            return false;

        int hitCount = type == CombatAttackType.SAttack ? 3 : 1;
        if (hitIndex < 1 || hitIndex > hitCount)
            return false;

        int hitBit = 1 << (hitIndex - 1);
        if ((consumedHits & hitBit) != 0)
            return false;

        // 即使这一段挥空，也不能通过重复动画事件再次结算。
        consumedHits |= hitBit;
        return true;
    }

    public void Reset()
    {
        attackType = CombatAttackType.None;
        startedAt = 0f;
        expiresAt = 0f;
        ReadyAt = 0f;
        consumedHits = 0;
    }
}
