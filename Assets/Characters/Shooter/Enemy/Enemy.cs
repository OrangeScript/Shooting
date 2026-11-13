
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

        gameObject.SetActive(true);

        // 恢复最初启用的碰撞体
        for (int i = 0; i < hitboxes.Length; i++)
        {
            if (hitboxes[i] != null)
                hitboxes[i].enabled = initialColliderStates[i];
        }

        enemyAnimation?.PlayWalk();
    }


   
    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
            return;

        currentHealth = Mathf.Max(
            0, currentHealth - damage
        );

        Debug.Log($"{name} HP: {currentHealth}");

        if (currentHealth == 0)
            Die();
    }
    

    private void Die()
    {
        IsAlive = false;

        foreach (Collider hitbox in hitboxes)
        {
            if (hitbox != null)
                hitbox.enabled = false;
        }

        enemyAnimation?.PlayDie();

        StartCoroutine(DeathAndRecycle());
    }

    private IEnumerator DeathAndRecycle()
    {
        yield return new WaitForSeconds(deathDuration);
        Recycle();
    }

    public void ReachEnd()
    {
        if (!IsAlive)
            return;

        Debug.Log($"{name} 到达终点");
        Recycle();
    }

    private void Recycle()
    {
        IsAlive = false;

        if (pool != null)
            pool.Release(this);
        else
            gameObject.SetActive(false);
    }
}
