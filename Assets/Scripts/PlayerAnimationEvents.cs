using System.Collections;
using System.Collections.Generic;
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
        
        Debug.Log("Throw 命中帧1触发！");
    }
    public void SAttack_2()
    {
        
        Debug.Log("Throw 命中帧2触发！");
    }
    public void SAttack_3()
    {
        
        Debug.Log("Throw 命中帧3触发！");
    }
}
