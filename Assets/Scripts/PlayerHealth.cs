
using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [Tooltip("可选；未指定时自动查找子物体中的 Animator（包含未激活对象）。")]
    [SerializeField] private Animator anim;

    private readonly NetworkVariable<int> currentHealth = new( 
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int CurrentHealth =>currentHealth.Value;
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;
    private int deathLayerIndex = -1;

    private void Awake()
    {
        if (anim == null)
            anim = GetComponentInChildren<Animator>(true);

        // 测试胶囊等没有动画的物体仍然可以使用生命值逻辑。
        if (anim != null)
            deathLayerIndex = anim.GetLayerIndex("DeathLayer");
    }

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;

        if (IsServer)
            currentHealth.Value = maxHealth;

        Debug.Log($"{name}: 初始 HP = {CurrentHealth}");
        ApplyDeathState();
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }


    public void TakeDamage(int damage)
    {
        if (!IsSpawned || !IsServer) return;
        if (IsDead || damage <= 0) return;

        currentHealth.Value = Mathf.Max(
            0,
            CurrentHealth - damage
        );
    }

    private void OnHealthChanged(int oldHP, int newHP)
    {
        Debug.Log(
            $"Player {OwnerClientId}: HP {oldHP} -> {newHP}"
        );

        if (newHP < oldHP && newHP > 0)
        {
            Debug.Log($"{name} 受到攻击！");

            if (anim != null)
                anim.SetTrigger("Hit");
        }

        if (oldHP > 0 && newHP <= 0)
        {
            Debug.Log($"{name} 死亡！");
            ApplyDeathState();
        }
    }

        private void ApplyDeathState()
        {
            if (anim == null)
                return;

            bool dead = IsDead;

            // 持久状态，不使用一次性 Trigger
            anim.SetBool("IsDead", dead);

            if (deathLayerIndex >= 0)
            {
                // 死亡时全身覆盖，活着时不影响正常动作
                anim.SetLayerWeight(
                    deathLayerIndex,
                    dead ? 1f : 0f
                );
            }
        }

}
