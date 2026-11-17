
using UnityEngine;

[CreateAssetMenu(
    menuName = "Items/Effects/Chain Lightning"
)]
public class ChainLightningEffectDefinition : ItemEffectDefinition
{
    [Header("Lightning")]
    [SerializeField, Range(0f, 1f)]
    private float chancePerStack = 1f;

    [SerializeField, Min(1)]
    private int baseBounces = 3;

    [SerializeField, Min(0.1f)]
    private float bounceRadius = 5f;

    [SerializeField, Range(0f, 1f)]
    private float damageRetention = 0.7f;

    [Header("Drone Inheritance")]
    [SerializeField, Range(0f, 1f)]
    private float droneChanceMultiplier = 0.35f;

    public override IItemEffectRuntime CreateRuntime(
        ItemRuntimeContext context)
    {
        return new Runtime(
            chancePerStack,
            baseBounces,
            bounceRadius,
            damageRetention,
            droneChanceMultiplier
        );
    }

    private sealed class Runtime :
        IItemEffectRuntime,
        IProjectileHitEffect
    {
        private readonly float chancePerStack;
        private readonly int baseBounces;
        private readonly float radius;
        private readonly float retention;
        private readonly float droneMultiplier;

        private int stacks;

        public Runtime(
            float chancePerStack,
            int baseBounces,
            float radius,
            float retention,
            float droneMultiplier)
        {
            this.chancePerStack = chancePerStack;
            this.baseBounces = baseBounces;
            this.radius = radius;
            this.retention = retention;
            this.droneMultiplier = droneMultiplier;
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

            // 不同攻击来源可以继承不同效果
            float multiplier;

            switch (source)
            {
                case ShooterAttackSource.PlayerBullet:
                    multiplier = 1f;
                    break;

                case ShooterAttackSource.DroneBullet:
                    multiplier = droneMultiplier;
                    break;

                default:
                    return null;
            }

            float chance = Mathf.Clamp01(
                chancePerStack * stacks * multiplier
            );

            int bounces =
                baseBounces + Mathf.Max(0, stacks - 1);

            // 每次开火生成独立的数值快照
            return new HitAction(
                chance,
                bounces,
                radius,
                retention
            );
        }

        public void Dispose()
        {
            stacks = 0;
        }
    }

    private sealed class HitAction : IProjectileHitAction
    {
        private readonly float chance;
        private readonly int bounces;
        private readonly float radius;
        private readonly float retention;

        public HitAction(
            float chance,
            int bounces,
            float radius,
            float retention)
        {
            this.chance = chance;
            this.bounces = bounces;
            this.radius = radius;
            this.retention = retention;
        }

        public void OnHit(
            ShooterAttackContext attack,
            Enemy target,
            LayerMask enemyMask)
        {
            ChainLightningResolver.Bounce(
                target,
                attack,
                enemyMask,
                chance,
                bounces,
                radius,
                retention
            );
        }
    }
}
