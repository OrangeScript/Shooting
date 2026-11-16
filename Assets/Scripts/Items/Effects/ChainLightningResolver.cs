
using System.Collections.Generic;
using UnityEngine;

public static class ChainLightningResolver
{
    public static void Bounce(
        Enemy firstTarget,
        ShooterAttackContext attack,
        LayerMask enemyMask)
    {
        if (firstTarget == null || attack.MaxBounces <= 0)
            return;

        if (Random.value > attack.LightningChance)
            return;

        HashSet<Enemy> visited = new()
        {
            firstTarget
        };

        Enemy current = firstTarget;

        for (int i = 0; i < attack.MaxBounces; i++)
        {
            Collider[] candidates = Physics.OverlapSphere(
                current.transform.position,
                attack.BounceRadius,
                enemyMask,
                QueryTriggerInteraction.Collide
            );

            Enemy next = null;
            float bestDistance = float.MaxValue;

            foreach (Collider collider in candidates)
            {
                Enemy enemy =
                    collider.GetComponentInParent<Enemy>();

                if (enemy == null ||
                    !enemy.IsAlive ||
                    visited.Contains(enemy))
                    continue;

                float distance =
                    (enemy.transform.position -
                     current.transform.position).sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    next = enemy;
                }
            }

            if (next == null)
                break;

            visited.Add(next);

            // 每次弹射衰减 30% 伤害
            int damage = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    attack.Damage * Mathf.Pow(0.7f, i + 1)
                )
            );

            next.TakeDamage(damage, attack.Owner);

            // 后续可以在这里播放两只怪之间的闪电特效
            current = next;
        }
    }
}
