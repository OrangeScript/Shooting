
using System.Collections.Generic;
using UnityEngine;

public static class ChainLightningResolver
{
    public static void Bounce(
        Enemy firstTarget,
        ShooterAttackContext attack,
        LayerMask enemyMask,
        float chance,
        int maxBounces,
        float radius,
        float damageRetention)
    {
        if (firstTarget == null || maxBounces <= 0)
            return;

        // 检查本次是否触发闪电
        if (Random.value >= Mathf.Clamp01(chance))
            return;

        // 防止一条闪电重复命中同一个敌人
        HashSet<Enemy> visited = new()
        {
            firstTarget
        };

        Vector3 from = firstTarget.transform.position;

        for (int i = 1; i <= maxBounces; i++)
        {
            Collider[] hits = Physics.OverlapSphere(
                from,
                radius,
                enemyMask,
                QueryTriggerInteraction.Collide
            );

            Enemy nearest = null;
            float bestDistance = float.PositiveInfinity;

            foreach (Collider hit in hits)
            {
                Enemy candidate =
                    hit.GetComponentInParent<Enemy>();

                if (candidate == null ||
                    !candidate.IsAlive ||
                    visited.Contains(candidate))
                    continue;

                float distance =
                    (candidate.transform.position - from)
                    .sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = candidate;
                }
            }

            if (nearest == null)
                break;

            // 保存位置，避免怪物死亡后产生引用问题
            Vector3 to = nearest.transform.position;
            visited.Add(nearest);
            LightningArcVFX.Instance?.Play(from, to);

            int damage = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    attack.Damage *
                    Mathf.Pow(damageRetention, i)
                )
            );

            // 可在 Scene 视图中看到蓝色连线
            Debug.DrawLine(
                from + Vector3.up,
                to + Vector3.up,
                Color.cyan,
                0.4f
            );

            nearest.TakeDamage(damage, attack.Owner);

            Debug.Log(
                $"[Lightning] 弹射第 {i} 次，" +
                $"目标 {nearest.name}，伤害 {damage}"
            );

            from = to;
        }
    }
}
