
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("属性")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float endZ = -1f;

    [Header("动画")]
    [SerializeField] private Animator animator;
    [SerializeField] private float deathDuration = 1.2f;

    private EnemyPool pool;
    private Collider[] hitboxes;

    private int health;
    private bool alive;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        hitboxes = GetComponentsInChildren<Collider>(true);
    }

    // 每次从对象池取出，都重新初始化
    public void Spawn(
        EnemyPool owner,
        Vector3 position,
        Quaternion rotation)
    {
        StopAllCoroutines();

        pool = owner;
        health = maxHealth;
        alive = true;

        transform.SetPositionAndRotation(position, rotation);

        foreach (Collider c in hitboxes)
        {
            if (c != null)
                c.enabled = true;
        }

        gameObject.SetActive(true);

        if (animator != null)
            animator.Play("Base Layer.Walk", 0, 0f);
    }

    private void Update()
    {
        if (!alive)
            return;

        // 敌人朝玩家方向推进
        transform.position +=
            Vector3.back * moveSpeed * Time.deltaTime;

        // 到达终点，直接回收
        if (transform.position.z <= endZ)
        {
            Debug.Log("敌人到达防线");
            Recycle();
        }
    }

    public void TakeDamage(int damage)
    {
        if (!alive || damage <= 0)
            return;

        health = Mathf.Max(0, health - damage);

        Debug.Log($"敌人剩余 HP: {health}");

        if (health <= 0)
            Die();
    }

    private void Die()
    {
        alive = false;

        // 关闭碰撞体，避免死亡期间继续被命中
        foreach (Collider c in hitboxes)
        {
            if (c != null)
                c.enabled = false;
        }

        if (animator != null)
            animator.Play("Base Layer.Die", 0, 0f);

        Debug.Log("击杀一个敌人");

        StartCoroutine(WaitAndRecycle());
    }

    private IEnumerator WaitAndRecycle()
    {
        yield return new WaitForSeconds(deathDuration);
        Recycle();
    }

    private void Recycle()
    {
        alive = false;

        if (pool != null)
            pool.Release(this);
        else
            gameObject.SetActive(false);
    }
}
