
using UnityEngine;

public abstract class ItemEffectDefinition : ScriptableObject
{
    // 生成一个独立的运行时效果实例。
// 这里不保存当前玩家的状态。
    public abstract IItemEffectRuntime CreateRuntime(
        ItemRuntimeContext context
    );
}

public interface IItemEffectRuntime
{
    // 道具叠加数变化时调用
    void SetStacks(int stacks);

    // 道具被移除或离开该玩法时清理
    void Dispose();
}
