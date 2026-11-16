
using UnityEngine;

[CreateAssetMenu(
    menuName = "Items/Effects/Bonus Projectile Damage"
)]
public class BonusProjectileDamageEffect : ItemEffectDefinition
{
    [SerializeField] private int bonusPerStack = 5;

    public override IItemEffectRuntime CreateRuntime(
        ItemRuntimeContext context)
    {
        ShooterCombatStats stats =
            context.Owner.GetComponent<ShooterCombatStats>();

        if (stats == null)
        {
            Debug.LogError("缺少 ShooterCombatStats");
            return null;
        }

        return new Runtime(stats, bonusPerStack);
    }

    private sealed class Runtime : IItemEffectRuntime
    {
        private readonly ShooterCombatStats stats;
        private readonly int bonusPerStack;

        // 每个运行时效果实例都有独立的来源标识
        private readonly object source = new();

        public Runtime(
            ShooterCombatStats stats,
            int bonusPerStack)
        {
            this.stats = stats;
            this.bonusPerStack = bonusPerStack;
        }

        public void SetStacks(int stacks)
        {
            stats.SetDamageBonus(
                source,
                bonusPerStack * stacks
            );
        }

        public void Dispose()
        {
            stats.RemoveDamageBonus(source);
        }
    }
}
