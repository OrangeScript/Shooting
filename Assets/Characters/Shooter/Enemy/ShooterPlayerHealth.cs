using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ShooterPlayerHealth : MonoBehaviour
{
    [Header("基础生命值")]
    [SerializeField, Min(1)] private int startingMaxHealth = 10;

    [Header("击杀成长")]
    [SerializeField] private bool healOnKill = false;

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    // 参数分别是：当前血量、最大血量
    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        MaxHealth = startingMaxHealth;
        CurrentHealth = MaxHealth;
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        NotifyHealthChanged();

        if (IsDead)
        {
            Debug.Log("玩家死亡");
            Died?.Invoke();
        }
    }

    // 击杀怪物后增加血量上限
    public void GainMaxHealth(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        MaxHealth += amount;

        if (healOnKill)
        {
            CurrentHealth = Mathf.Min(
                MaxHealth,
                CurrentHealth + amount
            );
        }

        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        Debug.Log($"Player HP: {CurrentHealth}/{MaxHealth}");
    }
}
