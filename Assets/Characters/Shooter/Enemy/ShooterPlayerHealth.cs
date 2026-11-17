using System;
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
    public int PermanentMaxHealth { get; private set; }
    public float HealOnKillBaseFraction => healOnKill ? 1f : 0f;
    public float EffectiveKillHealFraction => Mathf.Clamp(HealOnKillBaseFraction +
        (combatStats != null ? combatStats.Totals.killHeal : 0f),
        0f, ShooterCombatStats.MaxKillHealFraction);

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private Animator animator;
    private ShooterCombatStats combatStats;
    private float killHealRemainder;
    private float regenerationRemainder;

    private void Awake()
    {
        combatStats = GetComponent<ShooterCombatStats>();
        PermanentMaxHealth = Mathf.Max(1, startingMaxHealth);
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

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        NotifyHealthChanged();

        if (IsDead)
        {
            Debug.Log("玩家死亡");
            if (animator != null)
                animator.SetTrigger("Die");
            Died?.Invoke();
        }
    }

    // 保留原来的击杀永久成长；道具上限与永久成长分开计算。
    public void GainMaxHealth(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        PermanentMaxHealth += amount;
        MaxHealth = CalculateMaxHealth();

        // 小比例击杀回血跨次累计，避免 3 血怪的 5% 被取整为零。
        killHealRemainder += amount * EffectiveKillHealFraction;
        int healing = Mathf.FloorToInt(killHealRemainder);
        killHealRemainder -= healing;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healing);
        NotifyHealthChanged();
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
        return Mathf.Max(1, PermanentMaxHealth +
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
