using UnityEngine;

[CreateAssetMenu(
    fileName = "SummonCompanionEffect",
    menuName = "Items/Effects/Summon Companion"
)]
public class SummonEffectDefinition : ItemEffectDefinition
{
    [SerializeField] private CompanionAgent prefab;
    [SerializeField, Min(1)] private int countPerStack = 1;

    public override IItemEffectRuntime CreateRuntime(
        ItemRuntimeContext context)
    {
        if (context == null || context.Owner == null || prefab == null)
            return null;

        var manager =
            context.Owner.GetComponent<ShooterCompanionManager>();

        if (manager == null)
        {
            Debug.LogError("玩家缺少ShooterCompanionManager", context.Owner);
            return null;
        }

        return new Runtime(manager, prefab, countPerStack);
    }

    private sealed class Runtime : IItemEffectRuntime
    {
        private readonly ShooterCompanionManager manager;
        private readonly CompanionAgent prefab;
        private readonly int perStack;
        private readonly object source = new();

        private bool disposed;

        public Runtime(
            ShooterCompanionManager manager,
            CompanionAgent prefab,
            int perStack)
        {
            this.manager = manager;
            this.prefab = prefab;
            this.perStack = Mathf.Max(1, perStack);
        }

        public void SetStacks(int stacks)
        {
            if (!disposed && manager != null)
            {
                manager.SetSummons(
                    source,
                    prefab,
                    Mathf.Max(0, stacks) * perStack
                );
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (manager != null)
                manager.RemoveSummons(source);
        }
    }
}