
using System;
using System.Collections.Generic;
using UnityEngine;

public class ShooterCombatPipeline : MonoBehaviour
{
    // 当前玩家激活的命中效果
    private readonly List<IProjectileHitEffect> hitEffects = new();

    public int RegisteredEffectCount => hitEffects.Count;
    public event Action<EnemyKillContext> EnemyKilled;

    public void ReportEnemyKilled(EnemyKillContext context)
    {
        EnemyKilled?.Invoke(context);
    }

    // 道具生效时注册
    public void Register(IProjectileHitEffect effect)
    {
        if (effect == null || hitEffects.Contains(effect))
            return;

        hitEffects.Add(effect);

        Debug.Log(
            $"注册命中效果: {effect.GetType().Name}"
        );
    }

    // 道具移除时注销
    public void Unregister(IProjectileHitEffect effect)
    {
        if (effect == null)
            return;

        hitEffects.Remove(effect);
    }

    // 开火时调用，获取本次攻击携带的效果
    public IProjectileHitAction[] Capture(
        ShooterAttackSource source)
    {
        if (hitEffects.Count == 0)
            return Array.Empty<IProjectileHitAction>();

        List<IProjectileHitAction> result = new();

        foreach (var effect in hitEffects)
        {
            IProjectileHitAction action =
                effect.Capture(source);

            if (action != null)
                result.Add(action);
        }

        return result.ToArray();
    }
}
