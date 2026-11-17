using UnityEngine;

/// <summary>通用的每层基础属性效果；资产只保存配置，运行时按来源替换和撤销。</summary>
[CreateAssetMenu(menuName = "Items/Effects/Shooter Stats", fileName = "StatEffect")]
public sealed class ShooterStatEffectDefinition : ItemEffectDefinition
{
    [SerializeField] private ShooterStatModifiers perStack;

    public ShooterStatModifiers PerStack => perStack;

    public override IItemEffectRuntime CreateRuntime(ItemRuntimeContext context)
    {
        ShooterCombatStats stats = context.Owner.GetComponent<ShooterCombatStats>();
        if (stats == null)
        {
            Debug.LogError("基础属性道具需要角色挂载 ShooterCombatStats。", context.Owner);
            return null;
        }

        return new Runtime(stats, perStack);
    }

    private sealed class Runtime : IItemEffectRuntime
    {
        private readonly ShooterCombatStats stats;
        private readonly ShooterStatModifiers perStack;
        private readonly object source = new object();
        private bool disposed;

        public Runtime(ShooterCombatStats stats, ShooterStatModifiers perStack)
        {
            this.stats = stats;
            this.perStack = perStack;
        }

        public void SetStacks(int stacks)
        {
            if (disposed || stats == null)
                return;

            stats.SetModifiers(source, perStack.Scaled(Mathf.Max(0, stacks)));
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (stats != null)
                stats.RemoveModifiers(source);
        }
    }
}
