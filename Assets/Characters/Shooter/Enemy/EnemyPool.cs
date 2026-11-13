
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private int initialSize = 15;
    [SerializeField] private int maxPoolSize = 100;

    private ObjectPool<Enemy> pool;

    public int ActiveCount =>
        pool == null ? 0 : pool.CountActive;

    public int InactiveCount =>
        pool == null ? 0 : pool.CountInactive;

    private void Awake()
    {
        pool = new ObjectPool<Enemy>(
            createFunc: CreateEnemy,
            actionOnGet: enemy => { },
            actionOnRelease: enemy =>
                enemy.gameObject.SetActive(false),
            actionOnDestroy: enemy =>
                Destroy(enemy.gameObject),
            collectionCheck: true,
            defaultCapacity: Mathf.Max(1, initialSize),
            maxSize: Mathf.Max(1, maxPoolSize)
        );

        // 提前创建一批敌人
        List<Enemy> prewarmed = new();

        for (int i = 0; i < initialSize; i++)
            prewarmed.Add(pool.Get());

        foreach (Enemy enemy in prewarmed)
            pool.Release(enemy);
    }

    private Enemy CreateEnemy()
    {
        Enemy enemy = Instantiate(enemyPrefab, transform);
        enemy.gameObject.SetActive(false);
        return enemy;
    }

    public Enemy Spawn(Vector3 position, Quaternion rotation)
    {
        Enemy enemy = pool.Get();
        enemy.Spawn(this, position, rotation);
        return enemy;
    }

    public void Release(Enemy enemy)
    {
        if (enemy == null || !enemy.gameObject.activeSelf)
            return;

        pool.Release(enemy);
    }

    private void OnDestroy()
    {
        pool?.Clear();
    }
}
