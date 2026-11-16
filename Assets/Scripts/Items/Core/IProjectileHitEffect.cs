
using UnityEngine;

// 道具运行时实现这个接口
public interface IProjectileHitEffect
{
    // 开火时创建本次攻击的效果快照
    // 不适用于该攻击来源时返回 null
    IProjectileHitAction Capture(
        ShooterAttackSource source
    );
}

// 子弹实际携带和执行的效果
public interface IProjectileHitAction
{
    void OnHit(
        ShooterAttackContext attack,
        Enemy target,
        LayerMask enemyMask
    );
}
