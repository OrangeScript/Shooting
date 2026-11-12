using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerCombat combat;

    private void Awake()
    {
        combat = GetComponentInParent<PlayerCombat>();
    }

    // Unity 动画事件会自动调用
    public void OnThrowHit()
    {

        combat?.OnAttackHitFrame();
    }

    public void SAttack_1()
    {
        combat?.OnSAttackHitFrame(1);
    }
    public void SAttack_2()
    {
        combat?.OnSAttackHitFrame(2);
    }
    public void SAttack_3()
    {
        combat?.OnSAttackHitFrame(3);
    }
}
