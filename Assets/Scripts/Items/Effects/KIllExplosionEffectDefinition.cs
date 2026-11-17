using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "KillExplosionEffect",
    menuName = "Items/Effects/Kill Explosion"
)]
public class KillExplosionEffectDefinition : ItemEffectDefinition
{
    [Header("范围")]
    [SerializeField, Min(0.1f)]
    private float radius = 3f;

    [SerializeField]
    private LayerMask enemyMask;

    [Header("伤害")]
    [SerializeField, Min(0)]
    private int baseDamage = 5;

    [Tooltip("额外伤害 = 死者最大血量 × 此系数")]
    [SerializeField, Range(0f, 1f)]
    private float victimHealthRatio = 0.2f;

    [Tooltip("每多持有一层，伤害增加的比例")]
    [SerializeField, Min(0f)]
    private float damagePerExtraStack = 0.5f;

    public override IItemEffectRuntime CreateRuntime(
        ItemRuntimeContext context)
    {
        if (context == null || context.Owner == null)
            return null;

        var pipeline =
            context.Owner.GetComponent<ShooterCombatPipeline>();

        var owner =
            context.Owner.GetComponent<ShooterPlayerHealth>();

        var visuals =
            context.Owner.GetComponent<ExplosionVfxPool>();

        if (pipeline == null || owner == null)
        {
            Debug.LogError(
                "击杀爆炸需要 ShooterCombatPipeline 和 ShooterPlayerHealth",
                context.Owner
            );
            return null;
        }

        if (visuals == null)
        {
            Debug.LogWarning(
                "缺少 ExplosionVfxPool：爆炸伤害有效，但没有特效",
                context.Owner
            );
        }

        return new Runtime(this, pipeline, owner, visuals);
    }

    private sealed class Runtime : IItemEffectRuntime
    {
        private readonly KillExplosionEffectDefinition definition;
        private readonly ShooterCombatPipeline pipeline;
        private readonly ShooterPlayerHealth owner;
        private readonly ExplosionVfxPool visuals;

        private Collider[] hits = new Collider[16];

        private readonly HashSet<Enemy> unique = new();
        private readonly List<Enemy> targets = new();

        private int stacks;
        private bool disposed;

        public Runtime(
            KillExplosionEffectDefinition definition,
            ShooterCombatPipeline pipeline,
            ShooterPlayerHealth owner,
            ExplosionVfxPool visuals)
        {
            this.definition = definition;
            this.pipeline = pipeline;
            this.owner = owner;
            this.visuals = visuals;

            pipeline.EnemyKilled += OnEnemyKilled;
        }

        public void SetStacks(int stacks)
        {
            this.stacks = Mathf.Max(0, stacks);
        }

        private void OnEnemyKilled(EnemyKillContext kill)
        {
            // 必须先过滤，再操作下面的复用缓存。
            // 本版严格禁止爆炸再次触发爆炸。
            if (disposed ||
                stacks <= 0 ||
                owner == null ||
                !owner.isActiveAndEnabled ||
                kill.Attack.Owner != owner ||
                kill.Attack.Source == ShooterAttackSource.DeathExplosion)
                return;

            float radius = Mathf.Max(0.1f, definition.radius);

            float multiplier =
                1f + Mathf.Max(0f, definition.damagePerExtraStack)
                * Mathf.Max(0, stacks - 1);

            int damage = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    (
                        Mathf.Max(0, definition.baseDamage)
                        + kill.VictimMaxHealth
                        * Mathf.Clamp01(definition.victimHealthRatio)
                    ) * multiplier
                )
            );

            CollectTargets(kill.Position, radius);

            if (visuals != null)
                visuals.Play(kill.Position, radius);

            var explosionAttack = new ShooterAttackContext
            {
                Owner = owner,
                Damage = damage,
                Source = ShooterAttackSource.DeathExplosion
            };

            // 先收集、去重，再统一伤害。
            foreach (Enemy enemy in targets)
            {
                if (enemy != null && enemy.IsAlive)
                    enemy.TakeDamage(explosionAttack);
            }
        }

        private void CollectTargets(Vector3 position, float radius)
        {
            targets.Clear();
            unique.Clear();

            // 项目关闭了自动同步，查询前同步 Transform。
            Physics.SyncTransforms();

            int count;

            while (true)
            {
                count = Physics.OverlapSphereNonAlloc(
                    position,
                    radius,
                    hits,
                    definition.enemyMask,
                    QueryTriggerInteraction.Collide
                );

                if (count < hits.Length)
                    break;

                // 返回数量等于容量，可能截断了结果，需要扩容重查。
                Array.Resize(ref hits, hits.Length * 2);
            }

            for (int i = 0; i < count; i++)
            {
                Enemy enemy =
                    hits[i].GetComponentInParent<Enemy>();

                if (enemy == null ||
                    !enemy.IsAlive ||
                    !unique.Add(enemy))
                    continue;

                targets.Add(enemy);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            stacks = 0;

            if (pipeline != null)
                pipeline.EnemyKilled -= OnEnemyKilled;
        }
    }
}