using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(
    fileName = "HitExplosionEffect",
    menuName = "Items/Effects/Hit Explosion"
)]
public class HitExplosionEffectDefinition : ItemEffectDefinition
{
    [Header("爆炸范围")]
    [SerializeField, Min(0.1f)]
    private float radius = 2.5f;

    [Header("爆炸伤害")]
    [Tooltip("爆炸伤害相对于本次子弹最终伤害的倍率")]
    [SerializeField, Min(0.01f)]
    private float damageMultiplier = 1f;

    [Tooltip("每多一层道具，额外增加多少伤害倍率")]
    [SerializeField, Min(0f)]
    private float extraMultiplierPerStack = 0.25f;

    [Tooltip("原目标没死时，是否也承受爆炸伤害")]
    [SerializeField]
    private bool damagePrimaryTarget = true;

    [Header("无人机继承")]
    [Tooltip("0表示无人机不继承这个效果")]
    [SerializeField, Range(0f, 1f)]
    private float droneMultiplier = 0.5f;

    public override IItemEffectRuntime CreateRuntime(
        ItemRuntimeContext context)
    {
        if (context == null || context.Owner == null)
            return null;

        var visuals =
            context.Owner.GetComponent<ExplosionVfxPool>();

        if (visuals == null)
        {
            Debug.LogWarning(
                "命中爆炸找不到玩家身上的 ExplosionVfxPool",
                context.Owner
            );
        }

        return new Runtime(this, visuals);
    }

    private sealed class Runtime :
        IItemEffectRuntime,
        IProjectileHitEffect
    {
        private readonly HitExplosionEffectDefinition definition;
        private readonly ExplosionVfxPool visuals;

        // 同一玩家的效果快照共享查询缓存，
        // 但快照不依赖Runtime的当前层数。
        private readonly QueryCache query = new();

        private int stacks;

        public Runtime(
            HitExplosionEffectDefinition definition,
            ExplosionVfxPool visuals)
        {
            this.definition = definition;
            this.visuals = visuals;
        }

        public void SetStacks(int stacks)
        {
            this.stacks = Mathf.Max(0, stacks);
        }

        public IProjectileHitAction Capture(
            ShooterAttackSource source)
        {
            if (stacks <= 0)
                return null;

            float inheritance;

            switch (source)
            {
                case ShooterAttackSource.PlayerBullet:
                    inheritance = 1f;
                    break;

                case ShooterAttackSource.DroneBullet:
                    inheritance = Mathf.Clamp01(
                        definition.droneMultiplier
                    );
                    break;

                case ShooterAttackSource.SwordPet:
                inheritance = Mathf.Clamp01(
                    definition.droneMultiplier
                );
                break;

                default:
                    return null;
            }

            if (inheritance <= 0f)
                return null;

            float multiplier =
                (
                    Mathf.Max(0.01f, definition.damageMultiplier)
                    + Mathf.Max(0f, definition.extraMultiplierPerStack)
                    * Mathf.Max(0, stacks - 1)
                ) * inheritance;

            // 保存本次开火的数值快照。
            return new HitAction(
                Mathf.Max(0.1f, definition.radius),
                multiplier,
                definition.damagePrimaryTarget,
                visuals,
                query
            );
        }

        public void Dispose()
        {
            // 不影响已经发射出去的HitAction。
            stacks = 0;
        }
    }

    private sealed class HitAction : IProjectileHitAction
    {
        private readonly float radius;
        private readonly float multiplier;
        private readonly bool damagePrimaryTarget;
        private readonly ExplosionVfxPool visuals;
        private readonly QueryCache query;

        public HitAction(
            float radius,
            float multiplier,
            bool damagePrimaryTarget,
            ExplosionVfxPool visuals,
            QueryCache query)
        {
            this.radius = radius;
            this.multiplier = multiplier;
            this.damagePrimaryTarget = damagePrimaryTarget;
            this.visuals = visuals;
            this.query = query;
        }

        public void OnHit(
            ShooterAttackContext attack,
            Enemy target,
            LayerMask enemyMask)
        {
            if (target == null)
                return;

            // 不检查target.IsAlive：
            // 子弹先扣血，致命命中时目标已经被标记死亡。
            Vector3 center = target.transform.position;

            List<Enemy> targets = ListPool<Enemy>.Get();
            HashSet<Enemy> unique = HashSetPool<Enemy>.Get();

            try
            {
                query.Collect(
                    center,
                    radius,
                    enemyMask,
                    damagePrimaryTarget ? null : target,
                    targets,
                    unique
                );

                // 即使周围没有存活目标，也照常播放爆炸。
                if (visuals != null)
                    visuals.Play(center, radius);

                var explosionAttack = new ShooterAttackContext
                {
                    Owner = attack.Owner,
                    Damage = Mathf.Max(
                        1,
                        Mathf.RoundToInt(attack.Damage * multiplier)
                    ),
                    Source = ShooterAttackSource.HitExplosion
                };

                foreach (Enemy enemy in targets)
                {
                    // 前面的伤害或其他效果可能已杀死它。
                    if (enemy != null && enemy.IsAlive)
                        enemy.TakeDamage(explosionAttack);
                }
            }
            finally
            {
                ListPool<Enemy>.Release(targets);
                HashSetPool<Enemy>.Release(unique);
            }
        }
    }

    private sealed class QueryCache
    {
        private Collider[] hits = new Collider[16];

        public void Collect(
            Vector3 center,
            float radius,
            LayerMask enemyMask,
            Enemy excluded,
            List<Enemy> targets,
            HashSet<Enemy> unique)
        {
            targets.Clear();
            unique.Clear();

            Physics.SyncTransforms();

            int count;

            while (true)
            {
                count = Physics.OverlapSphereNonAlloc(
                    center,
                    radius,
                    hits,
                    enemyMask,
                    QueryTriggerInteraction.Collide
                );

                if (count < hits.Length)
                    break;

                Array.Resize(ref hits, hits.Length * 2);
            }

            // 完整收集之后才执行伤害，
            // 避免伤害产生的其他效果干扰遍历。
            for (int i = 0; i < count; i++)
            {
                Enemy enemy =
                    hits[i].GetComponentInParent<Enemy>();

                if (enemy == null ||
                    !enemy.IsAlive ||
                    enemy == excluded ||
                    !unique.Add(enemy))
                    continue;

                targets.Add(enemy);
            }
        }
    }
}