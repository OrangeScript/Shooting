using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{

    [Header("预制体")]
    [SerializeField] private Enemy enemyPrefab;

    [Header("对象池参数")]
    [SerializeField] private int initialSize = 20;
    [SerializeField] private int maxPoolSize = 150;


    private ObjectPool<Enemy> pool;
    public int InactiveCount =>  pool == null ? 0 : pool.CountInactive;

     private void Awake()
    {
        pool = new ObjectPool<Enemy>(
            createFunc: CreateEnemy,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyEnemy,
            collectionCheck: true,
            defaultCapacity: Mathf.Max(1, initialSize),
            maxSize: Mathf.Max(1, maxPoolSize)
        );

        // 真正预热 initialSize 个敌人
        List<Enemy> temporary = new();

        for (int i = 0; i < initialSize; i++)
            temporary.Add(pool.Get());

        foreach (Enemy enemy in temporary)
            pool.Release(enemy);
    }
    

    private Enemy CreateEnemy()
    {
        Enemy enemy = Instantiate(enemyPrefab, transform);
        enemy.gameObject.SetActive(false);
        return enemy;
    }

    private void OnGet(Enemy enemy)
    {
        // 暂不激活，等 Spawn 设置完位置和状态
    }

    private void OnRelease(Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
    }

    private void OnDestroyEnemy(Enemy enemy)
    {
        if (enemy != null)
            Destroy(enemy.gameObject);
    }

    public Enemy Spawn(
        Vector3 position,
        Quaternion rotation,
        GameDirector director)
    {
        Enemy enemy = pool.Get();

        enemy.Activate(this, director, position, rotation);

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
        if (pool != null)
            pool.Clear();
    }

    
}
