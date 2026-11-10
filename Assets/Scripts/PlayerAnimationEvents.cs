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
        Debug.Log("Throw 命中帧触发！");

        combat?.OnAttackHitFrame();
    }
}
