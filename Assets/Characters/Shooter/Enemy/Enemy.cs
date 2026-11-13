using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("基础属性")]
    [SerializeField] private bool isBoss = false;
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int contactDamage = 5;
    [SerializeField] private float moveSpeed = 2f;

    [Header("移动与碰撞")]
    [SerializeField] private float endZ = -1f;
    [SerializeField] private float contactHalfWidth = 0.8f;

    [Header("动画")]
    [SerializeField] private Animator animator;
    [SerializeField] private float deathDuration = 1.2f;

    private EnemyPool pool;
    private GameDirector director;
    private Rigidbody rb;
    private Collider hitbox;

    private int health;
    private bool alive;
    private bool dying;

    public bool IsBoss => isBoss;
    public int ContactDamage => contactDamage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hitbox = GetComponent<Collider>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    public void Activate(
        EnemyPool ownerPool,
        GameDirector game,
        Vector3 position,
        Quaternion rotation)
    {
        pool = ownerPool;
        director = game;

        transform.SetPositionAndRotation(position, rotation);
        rb.position = position;
        rb.rotation = rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        health = maxHealth;
        alive = true;
        dying = false;

        if (hitbox != null)
            hitbox.enabled = true;

        gameObject.SetActive(true);

        if (animator != null)
            animator.Play("Base Layer.Walk", 0, 0f);
    }

    private void FixedUpdate()
    {
        if (!alive || dying || director == null ||
            !director.IsRunning)
            return;

        // 所有敌人沿世界 Z- 方向向玩家推进
        Vector3 nextPosition =
            rb.position + Vector3.back *
            moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);

        if (nextPosition.z <= endZ)
        {
            // 经过玩家所在的横向范围，才视为撞到玩家
            bool touchedPlayer =
                Mathf.Abs(nextPosition.x - director.PlayerX)
                <= contactHalfWidth;

            ReachEnd(touchedPlayer);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!alive || dying)
            return;

        if (other.GetComponentInParent<GamerController>() != null)
            ReachEnd(true);
    }

    private void ReachEnd(bool touchedPlayer)
    {
        if (!alive)
            return;

        alive = false;

        if (touchedPlayer && director != null)
            director.OnEnemyReached(this);

        pool.Release(this);
    }

    public void TakeDamage(int damage)
    {
        if (!alive || dying || damage <= 0 ||
            director == null || !director.IsRunning)
            return;

        if (isBoss)
        {
            director.DamageBoss(damage);
            return;
        }

        health = Mathf.Max(0, health - damage);

        if (health == 0)
            Die();
    }

    // Boss HP 由 GameDirector 统一管理
    public void DieFromDirector()
    {
        if (alive && !dying)
            Die();
    }

    private void Die()
    {
        alive = false;
        dying = true;

        if (hitbox != null)
            hitbox.enabled = false;

        if (animator != null)
            animator.Play("Base Layer.Die", 0, 0f);

        if (!isBoss && director != null)
            director.OnEnemyDefeated();

        StartCoroutine(ReleaseAfterDeath());
    }

    private IEnumerator ReleaseAfterDeath()
    {
        yield return new WaitForSeconds(
            Mathf.Max(0.01f, deathDuration)
        );

        pool.Release(this);
    }
}
