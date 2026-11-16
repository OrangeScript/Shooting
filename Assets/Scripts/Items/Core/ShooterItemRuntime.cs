
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ShooterInventory))]
public class ShooterItemRuntime : MonoBehaviour
{
    private ShooterInventory inventory;

    // 每种道具对应一组实际生效的效果实例
    private readonly Dictionary<int, List<IItemEffectRuntime>>
        activeEffects = new();

    private void Start()
    {
        inventory = GetComponent<ShooterInventory>();

        if (inventory.Core == null)
        {
            Debug.LogError("InventoryCore 未初始化", this);
            return;
        }

        inventory.Core.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null && inventory.Core != null)
            inventory.Core.Changed -= Refresh;

        foreach (var effects in activeEffects.Values)
        {
            foreach (var effect in effects)
                effect.Dispose();
        }

        activeEffects.Clear();
    }

    private void Refresh()
    {
        InventoryCore core = inventory.Core;

        HashSet<int> ownedIds = new(core.Items);

        // 道具被移除时，销毁其运行时效果
        List<int> removedIds = new();

        foreach (int id in activeEffects.Keys)
        {
            if (!ownedIds.Contains(id))
                removedIds.Add(id);
        }

        foreach (int id in removedIds)
        {
            foreach (var runtime in activeEffects[id])
                runtime.Dispose();

            activeEffects.Remove(id);
        }

        // 遍历当前拥有的道具
        foreach (int id in ownedIds)
        {
            ItemDefinition item = inventory.Database.GetItem(id);

            if (item == null)
                continue;

            if (!activeEffects.TryGetValue(id, out var runtimes))
            {
                runtimes = new List<IItemEffectRuntime>();

                var context = new ItemRuntimeContext(gameObject);

                CreateEffects(item.sharedEffects, context, runtimes);
                CreateEffects(item.shooterEffects, context, runtimes);

                activeEffects.Add(id, runtimes);
            }

            // 每次调用都必须能安全覆盖原来的层数
            int stacks = core.GetStacks(id);

            foreach (var runtime in runtimes)
                runtime.SetStacks(stacks);
        }
    }

    private void CreateEffects(
        List<ItemEffectDefinition> definitions,
        ItemRuntimeContext context,
        List<IItemEffectRuntime> output)
    {
        if (definitions == null)
            return;

        foreach (ItemEffectDefinition definition in definitions)
        {
            if (definition == null)
                continue;

            IItemEffectRuntime runtime =
                definition.CreateRuntime(context);

            if (runtime != null)
                output.Add(runtime);
        }
    }
}
