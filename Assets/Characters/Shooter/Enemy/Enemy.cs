
using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
   
    // [SerializeField] private float endZ = -1f;

    // [Header("动画")]
    // [SerializeField] private Animator animator;


    [Header("生命值")]
    [SerializeField] private int maxHealth = 3;

    [Header("死亡后回收时间")]
    [SerializeField] private float deathDuration = 1.2f;

    private int currentHealth;
    private EnemyPool pool;
    private EnemyAnimation enemyAnimation;

    private Collider[] hitboxes;
    private bool[] initialColliderStates;

    public bool IsAlive { get; private set; }
    [Header("接触伤害")]
    [SerializeField, Min(1)] private int damage = 1;
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public int CurrentHealth => currentHealth;

    [Header("头顶血量位置")]
    [SerializeField] private Transform healthAnchor;

    public Transform HealthAnchor => healthAnchor;
    // 每次从对象池正式出生后通知行为组件。
    public event Action Spawned;

    public bool IsInvulnerable { get; private set; }

    // 本项目隐身时同时关闭受击碰撞体。
    // 恢复时尊重碰撞体原本的 enabled 状态。
    public void SetInvulnerable(bool value)
    {
        IsInvulnerable = value;

        if (hitboxes == null)
            return;

        for (int i = 0; i < hitboxes.Length; i++)
        {
            if (hitboxes[i] == null)
                continue;

            hitboxes[i].enabled =
                IsAlive &&
                !value &&
                initialColliderStates[i];
        }
    }

    private void Awake()
    {
        enemyAnimation = GetComponent<EnemyAnimation>();

        hitboxes = GetComponentsInChildren<Collider>(true);
        initialColliderStates = new bool[hitboxes.Length];

        for (int i = 0; i < hitboxes.Length; i++)
            initialColliderStates[i] = hitboxes[i].enabled;
    }

    // 每次从对象池取出，都重新初始化
    public void Spawn(
        EnemyPool owner,
        Vector3 position,
        Quaternion rotation)
    {
        StopAllCoroutines();

        pool = owner;
        currentHealth = Mathf.Max(1, maxHealth);

        transform.SetPositionAndRotation(position, rotation);
        IsAlive = true;
        IsInvulnerable = false;

        gameObject.SetActive(true);

        // 恢复最初启用的碰撞体
        for (int i = 0; i < hitboxes.Length; i++)
        {
            if (hitboxes[i] != null)
                hitboxes[i].enabled = initialColliderStates[i];
        }

        enemyAnimation?.PlayWalk();
        EnemyHealthUI.Instance.Register(this);
        Spawned?.Invoke();
    }


   
    public void TakeDamage(int damage,ShooterPlayerHealth attacker = null)
    {

            TakeDamage(new ShooterAttackContext
        {
            Owner = attacker,
            Damage = damage,
            Source = ShooterAttackSource.Unknown
        });
    }

        public void TakeDamage(ShooterAttackContext attack)
    {
        if (!IsAlive || IsInvulnerable || attack.Damage <= 0)
            return;

        currentHealth = Mathf.Max(
            0,
            currentHealth - attack.Damage
        );

        Debug.Log($"{name} HP: {currentHealth}");

        if (currentHealth == 0)
            Die(attack);
    }
    

    private void Die(ShooterAttackContext attack)
    {
        if(!IsAlive) return;
        IsAlive = false;
        var kill = new EnemyKillContext(
            transform.position,
            MaxHealth,
            attack
        );
        

        foreach (Collider hitbox in hitboxes)
        {
            if (hitbox != null)
                hitbox.enabled = false;
        }

        enemyAnimation?.PlayDie();
        EnemyHealthUI.Instance?.Unregister(this);
        StartCoroutine(DeathAndRecycle());

        ShooterPlayerHealth killer = attack.Owner;
        if (killer != null){
        killer.GainMaxHealth(Mathf.Max(1, maxHealth));
        killer.GetComponent<ShooterCombatPipeline>()
            ?.ReportEnemyKilled(kill);
        }
    }

    private IEnumerator DeathAndRecycle()
    {
        yield return new WaitForSeconds(deathDuration);
        Recycle();
    }

    public void ReachEnd(ShooterPlayerHealth health)
    {
        if (!IsAlive)
            return;
        health.TakeDamage(damage);

        Debug.Log($"{name} 到达终点");
        Recycle();
    }

    public void Despawn()
    {
        if (!gameObject.activeSelf)
            return;

        StopAllCoroutines();
        Recycle();
    }

    private void Recycle()
    {
        IsAlive = false;
        EnemyHealthUI.Instance?.Unregister(this);

        if (pool != null)
            pool.Release(this);
        else
            gameObject.SetActive(false);
    }
}
