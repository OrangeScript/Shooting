using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ShooterPlayerHealth : MonoBehaviour
{
    [Header("基础生命值")]
    [SerializeField, Min(1)] private int startingMaxHealth = 10;

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public event Action<int, int> HealthChanged;
    public event Action Died;

    private Animator animator;
    private ShooterCombatStats combatStats;
    private float killHealRemainder;
    private float regenerationRemainder;

    private void Awake()
    {
        combatStats = GetComponent<ShooterCombatStats>();
        MaxHealth = CalculateMaxHealth();
        CurrentHealth = MaxHealth;
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (combatStats != null)
        {
            combatStats.Changed += OnStatsChanged;
            OnStatsChanged();
        }
    }

    private void OnDisable()
    {
        if (combatStats != null)
            combatStats.Changed -= OnStatsChanged;
    }

    private void Start()
    {
        // 兼容其他组件在 Awake 中注册属性来源的情况。
        OnStatsChanged();
        NotifyHealthChanged();
    }

    private void Update()
    {
        TickRegeneration(Time.deltaTime);
    }

    private void TickRegeneration(float deltaTime)
    {
        if (IsDead || combatStats == null || CurrentHealth >= MaxHealth)
        {
            // 满血时不储存一整管额外回血。
            regenerationRemainder = 0f;
            return;
        }

        regenerationRemainder += combatStats.Regeneration * Mathf.Max(0f, deltaTime);
        int amount = Mathf.FloorToInt(regenerationRemainder);
        if (amount <= 0)
            return;
        regenerationRemainder -= amount;
        Heal(amount);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
            return;

        float dodgeChance = combatStats != null ? combatStats.DodgeChance : 0f;
        if (dodgeChance > 0f && UnityEngine.Random.value < dodgeChance)
            return;

        ApplyDamage(damage);
    }

    public void Kill()
    {
        if (!IsDead)
            ApplyDamage(CurrentHealth);
    }

    private void ApplyDamage(int damage)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        NotifyHealthChanged();
        AudioManager.Play(IsDead ? SoundEffect.PlayerDeath : SoundEffect.PlayerHurt);

        if (!IsDead)
            return;

        Debug.Log("玩家死亡");
        if (animator != null)
            animator.SetTrigger("Die");

        Died?.Invoke();
    }

    public void OnEnemyKilled()
    {
        if (IsDead || CurrentHealth >= MaxHealth)
        {
            killHealRemainder = 0f;
            return;
        }

        // 道具提供每杀的回血量，小数跨次累计。敌人的最大生命不参与结算。
        killHealRemainder += combatStats != null ? combatStats.KillHealPerEnemy : 0f;
        int healing = Mathf.FloorToInt(killHealRemainder);
        killHealRemainder -= healing;
        Heal(healing);
        if (CurrentHealth >= MaxHealth)
            killHealRemainder = 0f;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead || CurrentHealth >= MaxHealth)
            return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        NotifyHealthChanged();
    }

    private int CalculateMaxHealth()
    {
        return Mathf.Max(1, startingMaxHealth +
            (combatStats != null ? combatStats.Totals.maxHealth : 0));
    }

    private void OnStatsChanged()
    {
        int previousMax = MaxHealth;
        MaxHealth = CalculateMaxHealth();
        if (MaxHealth == previousMax)
            return;

        // 只应用真正的上限差额；重复刷新同层数不会重复回血。
        // 负面上限道具不会直接击杀玩家，移除道具也不会复活。
        if (!IsDead)
            CurrentHealth = Mathf.Clamp(CurrentHealth + MaxHealth - previousMax,
                1, MaxHealth);
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
